using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmeWorks.ChromaPacker.Editor
{
    public class ChromaPackerEditor : EditorWindow
    {
        private const int MAX_RESOLUTION = 8192;
        private const int CHANNEL_COUNT = 4;
        private const float BASE_PADDING = 10.0f;
        private const float SMALL_PADDING = 4.0f;
        private const float WINDOW_WIDTH = 274 + BASE_PADDING * 2;
        private const float MIN_WINDOW_HEIGHT = 128 + BASE_PADDING * 2;

        private static readonly Color s_diffuseRed = new (1, 0.2f, 0.5f);
        private static readonly Color s_diffuseGreen = new (0.25f, 0.85f, 0.5f);
        private static readonly Color s_diffuseBlue = new (0.3f, 0.7f, 1f);
        private static readonly Color s_darkerGrey = new (0.2f, 0.2f, 0.2f);
        
        private static readonly int s_channelDataShaderID = Shader.PropertyToID("channelDataBuffer");
        private static readonly int s_inputRShaderID = Shader.PropertyToID("inputR");
        private static readonly int s_inputGShaderID = Shader.PropertyToID("inputG");
        private static readonly int s_inputBShaderID = Shader.PropertyToID("inputB");
        private static readonly int s_inputAShaderID = Shader.PropertyToID("inputA");
        private static readonly int s_mainTextureShaderID = Shader.PropertyToID("mainTexture");
        private static readonly int s_previewTextureShaderID = Shader.PropertyToID("previewTexture");
        private static readonly int s_previewMaskShaderID = Shader.PropertyToID("previewMask");

        [SerializeField] private ComputeShader m_chromaPackerCS;
        private int m_kernelRenderMainID;
        private int m_kernelRenderPreviewID;
        
        // Data
        private readonly ChannelData[] m_channelDataArray   = new ChannelData[CHANNEL_COUNT];
        private readonly Texture2D[] m_channelTextures      = new Texture2D[CHANNEL_COUNT];
        private readonly float[] m_channelDefaultValues     = new float[CHANNEL_COUNT];
        private readonly ChannelMask[] m_channelMasks       = new ChannelMask[CHANNEL_COUNT];
        private readonly SamplingType[] m_samplingTypes     = new SamplingType[CHANNEL_COUNT];
        private readonly bool[] m_channelInverts            = new bool[CHANNEL_COUNT];
        private readonly float[] m_channelScalers           = new float[CHANNEL_COUNT];
        private readonly Vector2[] m_channelClamps          = new Vector2[CHANNEL_COUNT];
        private readonly Vector2[] m_channelClips           = new Vector2[CHANNEL_COUNT];
        private readonly Vector2Int[] m_channelOffsets      = new Vector2Int[CHANNEL_COUNT];
        
        private ChannelMask m_previewMasking = ChannelMask.R | ChannelMask.G | ChannelMask.B | ChannelMask.A;
        private Vector2Int m_textureSize = new (128, 128);
        private RenderTexture m_resultRT;
        private RenderTexture m_previewResultRT;
        private bool m_isRTDirty;
        
        // Elements
        private readonly Label[] m_channelTextureSizeLabels = new Label[CHANNEL_COUNT];
        private readonly Image[] m_previewImages            = new Image[CHANNEL_COUNT];
        private readonly VisualElement[] m_noTextureGroups  = new VisualElement[CHANNEL_COUNT];
        private readonly VisualElement[] m_textureGroups    = new VisualElement[CHANNEL_COUNT];
        private Image m_previewResultImage;
        
        [MenuItem("Tools/Chroma Packer")]
        public static void OpenWindow()
        {
            ChromaPackerEditor wnd = GetWindow<ChromaPackerEditor>();
            wnd.titleContent = new GUIContent("Chroma Packer");
            wnd.minSize = new Vector2(WINDOW_WIDTH + 64, MIN_WINDOW_HEIGHT);
        }
        
#region Create GUI
        // Unity's CreateGUI is verbose...
        // Next time it's either UXML assets or OnGUI's immediate mode rendering
        private void CreateGUI()
        {
            m_kernelRenderMainID = m_chromaPackerCS.FindKernel("CSRenderMain");
            m_kernelRenderPreviewID = m_chromaPackerCS.FindKernel("CSRenderPreview");
            
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                m_channelDataArray[i] = new ChannelData();
                m_channelMasks[i] = ChannelMask.R;
                m_channelClamps[i] = new Vector2(0, 1);
                m_channelClips[i] = new Vector2(0, 1);
                m_channelScalers[i] = 1.0f;
                m_channelOffsets[i] = Vector2Int.zero;
            }
            
            VisualElement root = rootVisualElement;
            root.style.justifyContent = Justify.FlexStart;
            root.style.alignItems = Align.FlexStart;
            
            ScrollView scrollView = new ScrollView();
            scrollView.style.width = Length.Percent(100f);
            scrollView.style.height = Length.Percent(100f);
            root.Add(scrollView);
            
            VisualElement mainElementsGroup = new VisualElement();
            mainElementsGroup.SetMargin(BASE_PADDING, BASE_PADDING, BASE_PADDING, BASE_PADDING);
            mainElementsGroup.style.minWidth = WINDOW_WIDTH;
            mainElementsGroup.style.minHeight = 64;
            mainElementsGroup.style.flexDirection = FlexDirection.Column;
            mainElementsGroup.style.justifyContent = Justify.FlexStart;
            
            CreateGUIInputChannels(mainElementsGroup);
            CreateGUIOutput(mainElementsGroup);
            
            scrollView.Add(mainElementsGroup);
        }

        private void CreateGUIInputChannels(VisualElement parent)
        {
            AddChannelTextureElement(0);
            AddChannelTextureElement(1);
            AddChannelTextureElement(2);
            AddChannelTextureElement(3);
            
            void AddChannelTextureElement(int index)
            {
                var texture = m_channelTextures[index];
                
                VisualElement topElement = new VisualElement();
                topElement.SetPadding(SMALL_PADDING, SMALL_PADDING, SMALL_PADDING, SMALL_PADDING);
                topElement.style.marginTop = BASE_PADDING;
                topElement.style.minWidth = WINDOW_WIDTH - BASE_PADDING;
                topElement.style.backgroundColor = s_darkerGrey;
                topElement.style.flexDirection = FlexDirection.Row;
                topElement.style.justifyContent = Justify.Center;
                
                VisualElement verticalGroupLeft = new VisualElement();
                verticalGroupLeft.style.marginRight = BASE_PADDING;
                verticalGroupLeft.style.minWidth = 200;
                verticalGroupLeft.style.minHeight = 64;
                verticalGroupLeft.style.flexGrow = 1;
                verticalGroupLeft.style.flexDirection = FlexDirection.Column;
                verticalGroupLeft.style.justifyContent = Justify.FlexStart;
                
                VisualElement noTextureGroup = new VisualElement();
                noTextureGroup.style.flexDirection = FlexDirection.Column;
                noTextureGroup.SetVisibility(texture != null ? ElementVisibility.Collapsed : ElementVisibility.Visible);
                m_noTextureGroups[index] = noTextureGroup;
                
                VisualElement textureGroup = new VisualElement();
                textureGroup.SetVisibility(texture != null ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                textureGroup.style.flexDirection = FlexDirection.Column;
                m_textureGroups[index] = textureGroup;
                
                ObjectField textureField = new ObjectField();
                textureField.objectType = typeof(Texture2D);
                textureField.allowSceneObjects = false;
                textureField.value = texture;
                textureField.RegisterValueChangedCallback(evt =>
                {
                    var newTexture = evt.newValue as Texture2D;
                    var defaultValue = m_channelDefaultValues[index];
                    var isTextureValid = newTexture != null;
                    m_channelTextures[index] = newTexture;
                    m_previewImages[index].image = newTexture;
                    m_previewImages[index].style.backgroundColor = isTextureValid 
                        ? s_darkerGrey : ColorExtensions.NewGrayscale(defaultValue);
                    
                    m_noTextureGroups[index].SetVisibility(isTextureValid ? ElementVisibility.Collapsed : ElementVisibility.Visible);
                    m_textureGroups[index].SetVisibility(isTextureValid ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                    m_channelTextureSizeLabels[index].SetVisibility(isTextureValid ? ElementVisibility.Visible : ElementVisibility.Collapsed);
    
                    m_channelTextureSizeLabels[index].text = newTexture == null ? "" : $"{newTexture.width} x {newTexture.height}";
                    m_isRTDirty = true;
                });
    
                FloatField defaultValueField = new FloatField("No Texture Source");
                defaultValueField.value = m_channelDefaultValues[index];
                defaultValueField.RegisterValueChangedCallback(evt => 
                {
                    var defaultValue = evt.newValue;
                    m_channelDefaultValues[index] = defaultValue;
                    m_previewImages[index].style.backgroundColor = ColorExtensions.NewGrayscale(defaultValue);
                    m_isRTDirty = true;
                });
                EnumField channelEnumField = new EnumField("Channel Mask", m_channelMasks[index]);
                channelEnumField.RegisterValueChangedCallback(evt =>
                {
                    m_channelMasks[index] = (ChannelMask)evt.newValue;
                    m_isRTDirty = true;
                });
                Toggle invertToggle = new Toggle("Invert");
                invertToggle.RegisterValueChangedCallback(evt =>
                {
                    m_channelInverts[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                FloatField channelScalerField = new FloatField("Scale");
                channelScalerField.value = m_channelScalers[index];
                channelScalerField.RegisterValueChangedCallback(evt =>
                {
                    m_channelScalers[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                
                MinMaxSlider clampSlider = new MinMaxSlider("Clamp", m_channelClamps[index].x, m_channelClamps[index].y, 0, 1);
                clampSlider.RegisterValueChangedCallback(evt =>
                {
                    m_channelClamps[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                MinMaxSlider clipSlider = new MinMaxSlider("Clip", m_channelClips[index].x, m_channelClips[index].y, 0, 1);
                clipSlider.RegisterValueChangedCallback(evt =>
                {
                    m_channelClips[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                Vector2IntField offsetField = new Vector2IntField("Offset");
                offsetField.value = m_channelOffsets[index];
                offsetField.RegisterValueChangedCallback(evt =>
                {
                    m_channelOffsets[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                EnumField samplingTypeField = new EnumField("Sampling Type", m_samplingTypes[index]);
                samplingTypeField.RegisterValueChangedCallback(evt =>
                {
                    m_samplingTypes[index] = (SamplingType)evt.newValue;
                    m_isRTDirty = true;
                });
                
                textureGroup.Add(channelEnumField);
                textureGroup.Add(invertToggle);
                textureGroup.Add(channelScalerField);
                textureGroup.Add(clampSlider);
                textureGroup.Add(clipSlider);
                textureGroup.Add(offsetField);
                textureGroup.Add(samplingTypeField);
                
                noTextureGroup.Add(defaultValueField);
    
                Image previewImage = m_previewImages[index] ?? new Image 
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    style = {
                        alignSelf       = Align.Center,
                        width           = 64,
                        height          = 64,
                        backgroundColor = ColorExtensions.NewGrayscale(m_channelDefaultValues[index]),
                    },
                    image = texture
                };
                m_previewImages[index] = previewImage;
                
                Label textureSizeLabel = new Label(texture == null ? "" : $"{texture.width} x {texture.height}");
                textureSizeLabel.style.fontSize = 10;
                textureSizeLabel.style.alignSelf = Align.Center;
                textureSizeLabel.SetVisibility(texture != null ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                m_channelTextureSizeLabels[index] = textureSizeLabel;
                
                VisualElement verticalGroupRight = new VisualElement();
                verticalGroupRight.style.maxWidth = 64;
                verticalGroupRight.style.flexDirection = FlexDirection.Column;
                verticalGroupRight.style.justifyContent = Justify.FlexStart;
                
                VisualElement colorStripElement = new VisualElement();
                colorStripElement.style.marginBottom = SMALL_PADDING;
                colorStripElement.style.minHeight = 4;
                colorStripElement.style.width = 64;
                colorStripElement.style.backgroundColor = index switch
                {
                    1 => s_diffuseGreen,
                    2 => s_diffuseBlue,
                    3 => Color.white,
                    _ => s_diffuseRed
                };
                
                verticalGroupLeft.Add(textureField);
                verticalGroupLeft.Add(noTextureGroup);
                verticalGroupLeft.Add(textureGroup);
                verticalGroupLeft.Add(textureSizeLabel);
                
                verticalGroupRight.Add(colorStripElement);
                verticalGroupRight.Add(previewImage);
                verticalGroupRight.Add(textureSizeLabel);
    
                topElement.Add(verticalGroupLeft);
                topElement.Add(verticalGroupRight);

                parent.Add(topElement);
            }
        }
        
        private void CreateGUIOutput(VisualElement parent)
        {
            Vector2IntField textureSizeField = new Vector2IntField("Resolution");
            textureSizeField.value = m_textureSize;
            textureSizeField.style.marginTop = BASE_PADDING * 2;
            textureSizeField.RegisterValueChangedCallback(evt =>
            {
                var sanitizedResolution = new Vector2Int(
                    Mathf.Clamp(evt.newValue.x, 1, MAX_RESOLUTION),
                    Mathf.Clamp(evt.newValue.y, 1, MAX_RESOLUTION));

                textureSizeField.value = sanitizedResolution;
                m_textureSize = sanitizedResolution;
                m_isRTDirty = true;
            });

            EnumFlagsField previewFlagsField = new EnumFlagsField("Preview Filter", m_previewMasking);
            previewFlagsField.RegisterValueChangedCallback(evt =>
            {
                m_previewMasking = (ChannelMask)evt.newValue;
                m_previewResultImage.SetVisibility((int)m_previewMasking == 0 ? ElementVisibility.Collapsed : ElementVisibility.Visible);
                m_isRTDirty = true;
            });

            var previewResultImage = new Image 
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = {
                    width           = 256,
                    height          = 256,
                    marginTop       = BASE_PADDING,
                    alignSelf       = Align.Center,
                    backgroundColor = s_darkerGrey
                },
            };
            previewResultImage.SetVisibility((int)m_previewMasking == 0 ? ElementVisibility.Collapsed : ElementVisibility.Visible);
            m_previewResultImage = previewResultImage;
            
            Button exportButton = new Button(ExportPackedTexture);
            exportButton.style.marginTop = BASE_PADDING;
            exportButton.text = "Export Packed Texture";
            
            Button resetButton = new Button(ResetData);
            resetButton.text = "Reset Data";

            parent.Add(textureSizeField);
            parent.Add(previewFlagsField);
            parent.Add(previewResultImage);
            parent.Add(exportButton);
            parent.Add(resetButton);
        }
#endregion Create GUI
#region Update
        private void Update()
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (!m_isRTDirty)
                return;
            
            SetupBlitData();
            Blit();
            m_isRTDirty = false;
            
            void SetupBlitData()
            {
                for (int i = 0; i < m_channelDataArray.Length; i++)
                {
                    var texture = m_channelTextures[i];
                    var textureIsValid = texture != null;
                    m_channelDataArray[i] = new ChannelData
                    {
                        size            = textureIsValid        ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero,
                        mask            = textureIsValid        ? (int)m_channelMasks[i]                        : 0,
                        samplingType    = textureIsValid        ? (int)m_samplingTypes[i]                       : 0,
                        invert          = m_channelInverts[i]   ? 1                                             : 0,
                        scaler          = m_channelScalers[i],
                        clamp           = m_channelClamps[i],
                        clip            = m_channelClips[i],
                        offset          = m_channelOffsets[i],
                        defaultValue    = m_channelDefaultValues[i],
                    };
                }
                
                if (m_resultRT != null && (m_textureSize.x != m_resultRT.width || m_textureSize.y != m_resultRT.height))
                {
                    m_resultRT.Release();
                    m_resultRT = null;
                    
                    m_previewResultRT.Release();
                    m_previewResultRT = null;
                }

                if (m_resultRT == null && m_textureSize != Vector2Int.zero)
                {
                    m_resultRT = new (m_textureSize.x, m_textureSize.y, 0, RenderTextureFormat.ARGB32);
                    m_resultRT.enableRandomWrite = true;
                    m_resultRT.Create();
            
                    m_previewResultRT = new (m_textureSize.x, m_textureSize.y, 0, RenderTextureFormat.ARGB32);
                    m_previewResultRT.enableRandomWrite = true;
                    m_previewResultRT.Create();
                }
            }

            void Blit()
            {
                if (m_textureSize == Vector2Int.zero) 
                    return;

                var channelDataBuffer = new ComputeBuffer(4, sizeof(float) * 6 + sizeof(int) * 7);
                channelDataBuffer.SetData(m_channelDataArray);

                var threadCountX = Mathf.CeilToInt(m_textureSize.x / 32.0f);
                var threadCountY = Mathf.CeilToInt(m_textureSize.y / 32.0f);
            
                m_chromaPackerCS.SetBuffer(m_kernelRenderMainID, s_channelDataShaderID, channelDataBuffer);
                m_chromaPackerCS.SetTexture(m_kernelRenderMainID, s_inputRShaderID, m_channelTextures[0] ?? Texture2D.blackTexture);
                m_chromaPackerCS.SetTexture(m_kernelRenderMainID, s_inputGShaderID, m_channelTextures[1] ?? Texture2D.blackTexture);
                m_chromaPackerCS.SetTexture(m_kernelRenderMainID, s_inputBShaderID, m_channelTextures[2] ?? Texture2D.blackTexture);
                m_chromaPackerCS.SetTexture(m_kernelRenderMainID, s_inputAShaderID, m_channelTextures[3] ?? Texture2D.blackTexture);
                m_chromaPackerCS.SetTexture(m_kernelRenderMainID, s_mainTextureShaderID, m_resultRT);
                m_chromaPackerCS.SetTexture(m_kernelRenderPreviewID, s_mainTextureShaderID, m_resultRT);
                m_chromaPackerCS.SetTexture(m_kernelRenderPreviewID, s_previewTextureShaderID, m_previewResultRT);
                m_chromaPackerCS.SetInts(s_previewMaskShaderID, (int)m_previewMasking);
                
                m_chromaPackerCS.Dispatch(m_kernelRenderMainID, threadCountX, threadCountY, 1);
                m_chromaPackerCS.Dispatch(m_kernelRenderPreviewID, threadCountX, threadCountY, 1);

                channelDataBuffer.Release();

                m_previewResultImage.image = m_previewResultRT;
            }
        }
#endregion Update
#region Button Events
        private void ExportPackedTexture()
        {
            if (m_textureSize.x <= 0 || m_textureSize.y <= 0)
                return;
            
            var path = EditorUtility.SaveFilePanelInProject(
                title: "Export Packed Texture", 
                defaultName: "image", 
                extension: "png", 
                message: string.Empty);

            if (path.Length == 0)
                return;

            Texture2D resultTexture = new Texture2D(m_textureSize.x, m_textureSize.y, TextureFormat.ARGB32, false);
            var previousActiveRT = RenderTexture.active;
            try
            {
                RenderTexture.active = m_resultRT;
                resultTexture.ReadPixels(new Rect(0, 0, m_textureSize.x, m_textureSize.y), 0, 0);
                resultTexture.Apply();
                byte[] bytes = resultTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                RenderTexture.active = previousActiveRT;
                DestroyImmediate(resultTexture);
            }
        }

        private void ResetData()
        {
            // I'm lazy ^_^
            Close();
            OpenWindow();
        }
#endregion Button Events
    }
}