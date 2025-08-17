using UnityEngine;

namespace AmeWorks.ChromaPacker.Editor
{
    public class ChromaPackerRTGenerator : ScriptableObject
    {
        private static readonly int s_channelDataBufferShaderID = Shader.PropertyToID("channelDataBuffer");
        private static readonly int s_inputShaderID = Shader.PropertyToID("input");
        private static readonly int s_maskShaderID = Shader.PropertyToID("mask");
        private static readonly int s_inputRShaderID = Shader.PropertyToID("inputR");
        private static readonly int s_inputGShaderID = Shader.PropertyToID("inputG");
        private static readonly int s_inputBShaderID = Shader.PropertyToID("inputB");
        private static readonly int s_inputAShaderID = Shader.PropertyToID("inputA");
        private static readonly int s_resultShaderID = Shader.PropertyToID("result");
        
        [SerializeField] private ComputeShader m_packTextureCS;
        [SerializeField] private ComputeShader m_maskingPreviewFilterCS;
        
        private readonly ChannelData[] m_channelDatas = new ChannelData[4];
        private Texture2D[] m_channelTextures;
        private ChannelMask m_previewMasking;
        
        public void SetData(
            float[]         defaultValues, 
            ChannelMask[]   channelMasks, 
            bool[]          channelInverts, 
            float[]         channelScalers, 
            Vector2[]       channelClamps, 
            Vector2[]       channelClips,
            Vector2Int[]    channelOffsets,
            Texture2D[]     channelTextures,
            SamplingType[]  samplingTypes,
            ChannelMask     previewMasking)
        {
            for (int i = 0; i < m_channelDatas.Length; i++)
            {
                var texture = channelTextures[i];
                var textureIsValid = texture != null;
                m_channelDatas[i] = new ChannelData
                {
                    size            = textureIsValid        ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero,
                    mask            = textureIsValid        ? (int)channelMasks[i]                          : 0,
                    samplingType    = textureIsValid        ? (int)samplingTypes[i]                         : 0,
                    invert          = channelInverts[i]     ? 1                                             : 0,
                    scaler          = channelScalers[i],
                    clamp           = channelClamps[i],
                    clip            = channelClips[i],
                    offset          = channelOffsets[i],
                    defaultValue    = defaultValues[i],
                };
            }
            m_channelTextures = channelTextures;
            m_previewMasking = previewMasking;
        }
        
        public void RegenerateRenderTextures(
            ref RenderTexture resultRT, 
            ref RenderTexture previewResultRT, 
            Vector2Int size, 
            RenderTextureFormat format)
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
            
            var channelDataBuffer = new ComputeBuffer(4, sizeof(float) * 6 + sizeof(int) * 7);
            channelDataBuffer.SetData(m_channelDatas);
            
            m_packTextureCS.SetBuffer(0, s_channelDataBufferShaderID, channelDataBuffer);
            m_packTextureCS.SetTexture(0, s_inputRShaderID, m_channelTextures[0] ?? Texture2D.blackTexture);
            m_packTextureCS.SetTexture(0, s_inputGShaderID, m_channelTextures[1] ?? Texture2D.blackTexture);
            m_packTextureCS.SetTexture(0, s_inputBShaderID, m_channelTextures[2] ?? Texture2D.blackTexture);
            m_packTextureCS.SetTexture(0, s_inputAShaderID, m_channelTextures[3] ?? Texture2D.blackTexture);
            m_packTextureCS.SetTexture(0, s_resultShaderID, resultRT);
            m_packTextureCS.Dispatch(0, size.x, size.y, 1);
            
            channelDataBuffer.Release();

            m_maskingPreviewFilterCS.SetVector(s_maskShaderID, m_previewMasking.ToVector4());
            m_maskingPreviewFilterCS.SetTexture(0, s_inputShaderID, resultRT);
            m_maskingPreviewFilterCS.SetTexture(0, s_resultShaderID, previewResultRT);
            m_maskingPreviewFilterCS.Dispatch(0, size.x, size.y, 1);
        }
    }
}