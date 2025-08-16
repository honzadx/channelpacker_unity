using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerGenerator : IDisposable
    {
        private const string COMPUTE_SHADER_PATH =
            "Packages/com.ameworks.channelpacker/Editor/PackTexture.compute";
        
        private static readonly int _channelDataBufferShaderID = Shader.PropertyToID("channelDataBuffer");
        private static readonly int _inputRShaderID = Shader.PropertyToID("inputR");
        private static readonly int _inputGShaderID = Shader.PropertyToID("inputG");
        private static readonly int _inputBShaderID = Shader.PropertyToID("inputB");
        private static readonly int _inputAShaderID = Shader.PropertyToID("inputA");
        private static readonly int _resultShaderID = Shader.PropertyToID("result");
        
        private readonly ChannelData[] _channelDataArray = new ChannelData[4];

        private ComputeShader _computeShader;
        private int _mainKernelID;
        private ComputeBuffer _channelDataBuffer;
        private Texture2D[] _channelTextures;

        public void Init()
        {
            _computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(COMPUTE_SHADER_PATH);
            _mainKernelID = _computeShader.FindKernel("CSMain");
            _channelDataBuffer = new ComputeBuffer(4, sizeof(float) * 4 + sizeof(int) * 2);
        }
        
        public void Dispose()
        {
            Resources.UnloadAsset(_computeShader);
            _channelDataBuffer?.Dispose();
        }

        public void SetData(
            float[]         defaultValues, 
            ChannelMask[]   channelMasks, 
            bool[]          invertValues, 
            float[]         channelScalers, 
            float[]         channelMin, 
            float[]         channelMax, 
            Texture2D[]     channelTextures) 
        {
            for (int i = 0; i < _channelDataArray.Length; i++)
            {
                _channelDataArray[i] = new ChannelData
                {
                    mask = channelTextures[i] == null ? 0 : (int)channelMasks[i],
                    invertValue = invertValues[i] ? 1 : 0,
                    scaler = channelScalers[i],
                    min = channelMin[i],
                    max = channelMax[i],
                    defaultValue = defaultValues[i],
                };
            }
            _channelTextures = channelTextures;
            _channelDataBuffer.SetData(_channelDataArray);
        }
        
        public void UpdateRenderTexture(ref RenderTexture resultRT, Vector2Int size, RenderTextureFormat format)
        {
            if (size.x <= 0 || size.y <= 0)
                return;
            
            if (resultRT != null) 
                resultRT.Release();
            
            resultRT = new (size.x, size.y, 0, format);
            resultRT.enableRandomWrite = true;
            resultRT.Create();
            
            _computeShader.SetBuffer(_mainKernelID, _channelDataBufferShaderID, _channelDataBuffer);
            _computeShader.SetTexture(_mainKernelID, _inputRShaderID, _channelTextures[0] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputGShaderID, _channelTextures[1] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputBShaderID, _channelTextures[2] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputAShaderID, _channelTextures[3] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _resultShaderID, resultRT);
            _computeShader.Dispatch(_mainKernelID, size.x, size.y, 1);
        }
        
        public void ExportToPNG(RenderTexture sourceRT, Vector2Int size, string directory, string fileName)
        {
            if (!Directory.Exists(directory) || string.IsNullOrEmpty(fileName))
                return;
            
            if (size.x <= 0 || size.y <= 0)
                return;

            try
            {
                Texture2D resultTexture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
                var previousActiveRT = RenderTexture.active;

                RenderTexture.active = sourceRT;
                resultTexture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                resultTexture.Apply();
                byte[] bytes = resultTexture.EncodeToPNG();
                string path = Path.Combine(directory, $"{fileName}.png");
                File.WriteAllBytes(path, bytes);
                RenderTexture.active = previousActiveRT;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}