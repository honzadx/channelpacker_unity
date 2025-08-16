using UnityEditor;
using UnityEngine;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerRTGenerator
    {
        private const string COMPUTE_SHADER_PATH = "Packages/com.ameworks.channelpacker/Editor/PackTexture.compute";
        
        private static readonly int _channelDataBufferShaderID = Shader.PropertyToID("channelDataBuffer");
        private static readonly int _inputRShaderID = Shader.PropertyToID("inputR");
        private static readonly int _inputGShaderID = Shader.PropertyToID("inputG");
        private static readonly int _inputBShaderID = Shader.PropertyToID("inputB");
        private static readonly int _inputAShaderID = Shader.PropertyToID("inputA");
        private static readonly int _resultShaderID = Shader.PropertyToID("result");
        
        private readonly ChannelData[] _channelDatas = new ChannelData[4];

        private Texture2D[] _channelTextures;
        private ComputeShader _computeShader;
        private int _mainKernelID;
        
        public void Init()
        {
            _computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(COMPUTE_SHADER_PATH);
            _mainKernelID = _computeShader.FindKernel("CSMain");
        }
        
        public void SetData(
            float[]         defaultValues, 
            ChannelMask[]   channelMasks, 
            bool[]          invertValues, 
            float[]         channelScalers, 
            float[]         channelMin, 
            float[]         channelMax, 
            Texture2D[]     channelTextures,
            SamplingType[]  samplingTypes)
        {
            for (int i = 0; i < _channelDatas.Length; i++)
            {
                var texture = channelTextures[i];
                var textureIsValid = texture != null;
                _channelDatas[i] = new ChannelData
                {
                    mask            = !textureIsValid ? 0 : (int)channelMasks[i],
                    width           = !textureIsValid ? 0 : texture.width,
                    height          = !textureIsValid ? 0 : texture.height,
                    samplingType    = !textureIsValid ? 0 : (int)samplingTypes[i],
                    invertValue     = invertValues[i] ? 1 : 0,
                    scaler          = channelScalers[i],
                    min             = channelMin[i],
                    max             = channelMax[i],
                    defaultValue    = defaultValues[i],
                };
            }
            _channelTextures = channelTextures;
        }
        
        public void RegenerateRenderTexture(ref RenderTexture resultRT, Vector2Int size, RenderTextureFormat format)
        {
            if (size.x <= 0 || size.y <= 0)
                return;

            if (resultRT != null) 
                resultRT.Release();
            
            resultRT = new (size.x, size.y, 0, format);
            resultRT.enableRandomWrite = true;
            resultRT.Create();
            
            var channelDataBuffer = new ComputeBuffer(4, sizeof(float) * 4 + sizeof(int) * 5);
            channelDataBuffer.SetData(_channelDatas);
            
            _computeShader.SetBuffer(_mainKernelID, _channelDataBufferShaderID, channelDataBuffer);
            _computeShader.SetTexture(_mainKernelID, _inputRShaderID, _channelTextures[0] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputGShaderID, _channelTextures[1] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputBShaderID, _channelTextures[2] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _inputAShaderID, _channelTextures[3] ?? Texture2D.blackTexture);
            _computeShader.SetTexture(_mainKernelID, _resultShaderID, resultRT);
            _computeShader.Dispatch(_mainKernelID, size.x, size.y, 1);
            
            channelDataBuffer.Release();
        }
    }
}