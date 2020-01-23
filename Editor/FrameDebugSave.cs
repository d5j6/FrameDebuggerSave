﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Text;

namespace UTJ.FrameDebugSave
{
    public class FrameDebugSave : EditorWindow
    {
        private FrameInfoCrawler crawler;
        private FrameInfoCrawler.CaptureFlag captureFlag;

        private ReflectionCache reflectionCache;

        [MenuItem("Tools/FrameDebuggerSave")]
        public static void CreateWindow()
        {
            EditorWindow.GetWindow<FrameDebugSave>();
        }

        private void OnEnable()
        {
        }

        private void OnGUI()
        {
            captureFlag = (FrameInfoCrawler.CaptureFlag)EditorGUILayout.EnumFlagsField("CaptureFlag", captureFlag);
            if (GUILayout.Button("Capture via FrameDebugger"))
            {
                Execute();
            }
        }

        public void Execute()
        {
            if( this.reflectionCache == null)
            {
                this.reflectionCache = new ReflectionCache();
            }

            var frameDebuggeUtil = reflectionCache.GetTypeObject("UnityEditorInternal.FrameDebuggerUtility");

            // show FrameDebuggerWindow
            var frameDebuggerWindow = reflectionCache.GetTypeObject("UnityEditor.FrameDebuggerWindow");
            object windowObj = frameDebuggerWindow.CallMethod<object>("ShowFrameDebuggerWindow", null, null);
            frameDebuggerWindow.CallMethod<object>("EnableIfNeeded", windowObj, null);
            if (crawler == null)
            {
                crawler = new FrameInfoCrawler(this.reflectionCache);
            }
            crawler.Request(this.captureFlag, EndCrawler);
        }




        private void EndCrawler()
        {
            string dirPath = crawler.saveDirectory;
            // directory
            SaveFrameDebuggerEventsCsv(dirPath);
            SaveDetailJsonData(dirPath);
            EditorUtility.DisplayDialog("Saved", dirPath, "ok");
            crawler = null;
        }

        private void SaveFrameDebuggerEventsCsv(string dirPath)
        {
            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("frameEventIndex");
            csvStringGenerator.AppendColumn("type");

            csvStringGenerator.AppendColumn("rtName").
                AppendColumn("rtWidth").AppendColumn("rtHeight").
                AppendColumn("rtCount").AppendColumn("rtHasDepthTexture");

            csvStringGenerator.AppendColumn("vertexCount").
                AppendColumn("indexCount").
                AppendColumn("instanceCount").
                AppendColumn("drawCallCount").
                AppendColumn("shaderName").
                AppendColumn("passName").
                AppendColumn("passLightMode").
                AppendColumn("subShaderIndex").
                AppendColumn("shaderPassIndex").
                AppendColumn("shaderKeywords").
                AppendColumn("componentInstanceID").
                AppendColumn("meshInstanceID").
                AppendColumn("meshSubset");

            csvStringGenerator.AppendColumn("batchBreakCause");
            csvStringGenerator.NextRow();


            for (int i = 0; i < crawler.frameDebuggerEventDataList.Count; ++i)
            {
                var evtData = crawler.frameDebuggerEventDataList[i];
                var evt = crawler.frameDebuggerEventList[i];

                csvStringGenerator.AppendColumn(evtData.frameEventIndex);
                csvStringGenerator.AppendColumn(evt.type.ToString());

                csvStringGenerator.AppendColumn(evtData.rtName).
                    AppendColumn(evtData.rtWidth).AppendColumn(evtData.rtHeight).
                    AppendColumn(evtData.rtCount).AppendColumn(evtData.rtHasDepthTexture);

                csvStringGenerator.AppendColumn(evtData.vertexCount).
                    AppendColumn(evtData.indexCount).
                    AppendColumn(evtData.instanceCount).
                    AppendColumn(evtData.drawCallCount).
                    AppendColumn(evtData.shaderName).
                    AppendColumn(evtData.passName).
                    AppendColumn(evtData.passLightMode).
                    AppendColumn(evtData.subShaderIndex).
                    AppendColumn(evtData.shaderPassIndex).
                    AppendColumn(evtData.shaderKeywords).
                    AppendColumn(evtData.componentInstanceID).
                    AppendColumn(evtData.meshInstanceID).
                    AppendColumn(evtData.meshSubset);
                csvStringGenerator.AppendColumn(evtData.batchBreakCauseStr);
                csvStringGenerator.NextRow();
            }
            File.WriteAllText(Path.Combine(dirPath, "events.csv"), csvStringGenerator.ToString());
        }

        private void SaveDetailJsonData(string dirPath)
        {
            JsonStringGenerator jsonStringGenerator = new JsonStringGenerator();
            try
            {
                using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
                {
                    jsonStringGenerator.AddObjectValue("captureFlag", (int)captureFlag);
                    using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "frames"))
                    {
                        for (int i = 0; i < crawler.frameDebuggerEventDataList.Count; ++i)
                        {
                            var evt = crawler.frameDebuggerEventList[i];
                            var evtData = crawler.frameDebuggerEventDataList[i];
                            AppendFrameEvent(jsonStringGenerator, evt, evtData);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            File.WriteAllText(Path.Combine(dirPath, "detail.json"), jsonStringGenerator.ToString());
        }

        private void AppendFrameEvent(JsonStringGenerator jsonStringGenerator,
            FrameInfoCrawler.FrameDebuggerEvent evt,
            FrameInfoCrawler.FrameDebuggerEventData evtData)
        {
            using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
            {
                jsonStringGenerator.AddObjectValue("frameEventIndex",evtData.frameEventIndex);
                jsonStringGenerator.AddObjectValue("type",evt.type.ToString());
                AppendRenderingInfo(jsonStringGenerator, evt, evtData);
                AppendRenderTargetInfo(jsonStringGenerator, evtData);
                AppendShaderInfo(jsonStringGenerator, evtData);
            }
        }
        private void AppendRenderingInfo(JsonStringGenerator jsonStringGenerator,
            FrameInfoCrawler.FrameDebuggerEvent evt,
            FrameInfoCrawler.FrameDebuggerEventData evtData)
        {
            using (new JsonStringGenerator.ObjectScopeWithName(jsonStringGenerator, "rendering"))
            {
                jsonStringGenerator.AddObjectValue("vertexCount", evtData.vertexCount).
                    AddObjectValue("indexCount", evtData.indexCount).
                    AddObjectValue("instanceCount", evtData.instanceCount).
                    AddObjectValue("drawCallCount", evtData.drawCallCount).
                    AddObjectValue("componentInstanceID", evtData.componentInstanceID).
                    AddObjectValue("meshInstanceID", evtData.meshInstanceID).
                    AddObjectValue("meshSubset", evtData.meshSubset).
                    AddObjectValue("batchBreakCauseStr", evtData.batchBreakCauseStr);
            }
        }

        private void AppendRenderTargetInfo(JsonStringGenerator jsonStringGenerator,
            FrameInfoCrawler.FrameDebuggerEventData evtData)
        {
            using (new JsonStringGenerator.ObjectScopeWithName(jsonStringGenerator, "renderTarget"))
            {
                jsonStringGenerator.AddObjectValue("rtName",evtData.rtName).
                    AddObjectValue("rtWidth", evtData.rtWidth).AddObjectValue("rtHeight", evtData.rtHeight).
                    AddObjectValue("rtCount",evtData.rtCount).AddObjectValue("rtHasDepthTexture",evtData.rtHasDepthTexture);
            }
        }


        private void AppendShaderInfo(JsonStringGenerator jsonStringGenerator,
            FrameInfoCrawler.FrameDebuggerEventData evtData)
        {

            using (new JsonStringGenerator.ObjectScopeWithName(jsonStringGenerator, "shaderInfo"))
            {
                jsonStringGenerator.AddObjectValue("shaderName", evtData.shaderName);
                jsonStringGenerator.AddObjectValue("subShaderIndex", evtData.subShaderIndex);
                jsonStringGenerator.AddObjectValue("shaderPassIndex", evtData.shaderPassIndex);
                jsonStringGenerator.AddObjectValue("passName", evtData.passName);
                jsonStringGenerator.AddObjectValue("passLightMode", evtData.passLightMode);
                jsonStringGenerator.AddObjectValue("shaderKeywords", evtData.shaderKeywords);

                AppendShaderParam(jsonStringGenerator, evtData.convertedProperties);
            }
        }

        private void AppendShaderParam(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            using (new JsonStringGenerator.ObjectScopeWithName(jsonStringGenerator, "params"))
            {
                AppendShaderParamTextures(jsonStringGenerator, shaderParams);
                AppendShaderParamFloats(jsonStringGenerator, shaderParams);
                AppendShaderParamVectors(jsonStringGenerator, shaderParams);
                AppendShaderParamMatricies(jsonStringGenerator, shaderParams);
                
            }
        }
        private void AppendShaderParamTextures(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            if (shaderParams.convertedTextures == null) { return; }

            using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "textures"))
            {
                foreach (var textureParam in shaderParams.convertedTextures)
                {
                    using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
                    {
                        var val = textureParam.value as Texture;
                        jsonStringGenerator.AddObjectValue("name", textureParam.textureName);
                        if (val != null)
                        {
                            jsonStringGenerator.AddObjectValue("format", val.graphicsFormat.ToString());
                        }
                    }
                }
            }
        }
        private void AppendShaderParamFloats(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            if (shaderParams.convertedFloats == null) { return; }

            using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "floats"))
            {
                foreach (var floatParam in shaderParams.convertedFloats)
                {
                    using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
                    {
                        jsonStringGenerator.AddObjectValue(floatParam.name, (float)floatParam.value);
                    }
                }
            }
        }
        private void AppendShaderParamVectors(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            if (shaderParams.convertedVectors == null) { return; }

            using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "vectors"))
            {
                foreach (var vectorParam in shaderParams.convertedVectors)
                {
                    using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
                    {
                        var val = (Vector4)vectorParam.value;
                        jsonStringGenerator.AddObjectVector(vectorParam.name, ref val);
                    }
                }

            }
        }
        private void AppendShaderParamMatricies(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            if (shaderParams.convertedMatricies == null) { return; }
            using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "matricies"))
            {
                foreach (var matrixParam in shaderParams.convertedMatricies)
                {
                    using (new JsonStringGenerator.ObjectScope(jsonStringGenerator))
                    {
                        var val = (Matrix4x4)matrixParam.value;
                        jsonStringGenerator.AddObjectMatrix(matrixParam.name, ref val);
                    }
                }
            }
        }
        private void AppendShaderParamBuffers(JsonStringGenerator jsonStringGenerator, FrameInfoCrawler.ShaderProperties shaderParams)
        {
            if (shaderParams.convertedBuffers == null) { return; }
            using (new JsonStringGenerator.ObjectArrayValueScope(jsonStringGenerator, "buffers"))
            {
            }
        }

    }
}