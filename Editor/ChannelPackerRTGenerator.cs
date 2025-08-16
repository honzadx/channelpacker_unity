using UnityEditor;
using UnityEngine;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerRTGenerator
    {
        private const string PACK_TEXTURE_COMPUTE_SHADER_PATH 
            = "Packages/com.ameworks.channelpacker/Editor/PackTexture.compute";
        private const string MASKING_PREVIEW_FILTER_COMPUTE_SHADER_PATH 
            = "Packages/com.ameworks.channelpacker/Editor/MaskingPreviewFilter.compute";
        private const int MAIN_KERNEL_ID = 0;
        
        private static readonly int _channelDataBufferShaderID = Shader.PropertyToID("channelDataBuffer");
        private static readonly int _inputShaderID = Shader.PropertyToID("input");
        private static readonly int _maskShaderID = Shader.PropertyToID("mask");
        private static readonly int _inputRShaderID = Shader.PropertyToID("inputR");
        private static readonly int _inputGShaderID = Shader.PropertyToID("inputG");
        private static readonly int _inputBShaderID = Shader.PropertyToID("inputB");
        private static readonly int _inputAShaderID = Shader.PropertyToID("inputA");
        private static readonly int _resultShaderID = Shader.PropertyToID("result");
        
        private readonly ChannelData[] _channelDatas = new ChannelData[4];

        private Texture2D[] _channelTextures;
        private ComputeShader _packTextureCS;
        private ComputeShader _maskingPreviewFilterCS;
        private ChannelMask _previewMasking;
        
        public void Init()
        {
            _maskingPreviewFilterCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(MASKING_PREVIEW_FILTER_COMPUTE_SHADER_PATH);
            _packTextureCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(PACK_TEXTURE_COMPUTE_SHADER_PATH);
        }
        
        public void SetData(
            float[]         defaultValues, 
            ChannelMask[]   channelMasks, 
            bool[]          inverts, 
            float[]         channelScalers, 
            float[]         channelMin, 
            float[]         channelMax, 
            Texture2D[]     channelTextures,
            SamplingType[]  samplingTypes,
            ChannelMask     previewMasking)
        {
            for (int i = 0; i < _channelDatas.Length; i++)
            {
                var texture = channelTextures[i];
                var textureIsValid = texture != null;
                _channelDatas[i] = new ChannelData
                {
                    mask            = textureIsValid    ? (int)channelMasks[i]     : 0,
                    width           = textureIsValid    ? texture.width            : 0,
                    height          = textureIsValid    ? texture.height           : 0,
                    samplingType    = textureIsValid    ? (int)samplingTypes[i]    : 0,
                    invert          = inverts[i]        ? 1                        : 0,
                    scaler          = channelScalers[i],
                    min             = channelMin[i],
                    max             = channelMax[i],
                    defaultValue    = defaultValues[i],
                };
            }
            _channelTextures = channelTextures;
            _previewMasking = previewMasking;
        }
        
        public void RegenerateRenderTextures(ref RenderTexture resultRT, ref RenderTexture previewResultRT, Vector2Int size, RenderTextureFormat format)
        {
            if (size.x <= 0 || size.y <= 0)
                return;

            if (resultRT != null) 
                resultRT.Release();
            
            if (previewResultRT != null)
                previewResultRT.Release();
            
            resultRT = new (size.x, size.y, 0, format);
            resultRT.enableRandomWrite = true;
            resultRT.Create();
            
            previewResultRT = new (size.x, size.y, 0, format);
            previewResultRT.enableRandomWrite = true;
            previewResultRT.Create();
            
            var channelDataBuffer = new ComputeBuffer(4, sizeof(float) * 4 + sizeof(int) * 5);
            channelDataBuffer.SetData(_channelDatas);
            
            _packTextureCS.SetBuffer(MAIN_KERNEL_ID, _channelDataBufferShaderID, channelDataBuffer);
            _packTextureCS.SetTexture(MAIN_KERNEL_ID, _inputRShaderID, _channelTextures[0] ?? Texture2D.blackTexture);
            _packTextureCS.SetTexture(MAIN_KERNEL_ID, _inputGShaderID, _channelTextures[1] ?? Texture2D.blackTexture);
            _packTextureCS.SetTexture(MAIN_KERNEL_ID, _inputBShaderID, _channelTextures[2] ?? Texture2D.blackTexture);
            _packTextureCS.SetTexture(MAIN_KERNEL_ID, _inputAShaderID, _channelTextures[3] ?? Texture2D.blackTexture);
            _packTextureCS.SetTexture(MAIN_KERNEL_ID, _resultShaderID, resultRT);
            _packTextureCS.Dispatch(MAIN_KERNEL_ID, size.x, size.y, 1);
            
            _maskingPreviewFilterCS.SetVector(_maskShaderID, _previewMasking.ToVector4());
            _maskingPreviewFilterCS.SetTexture(MAIN_KERNEL_ID, _inputShaderID, resultRT);
            _maskingPreviewFilterCS.SetTexture(MAIN_KERNEL_ID, _resultShaderID, previewResultRT);
            _maskingPreviewFilterCS.Dispatch(MAIN_KERNEL_ID, size.x, size.y, 1);
            
            channelDataBuffer.Release();
        }
    }
}