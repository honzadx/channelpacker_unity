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

        [SerializeField] private ChromaPackerRTGenerator m_renderTextureGenerator;
        
        // Data
        private readonly float[] m_channelDefaultValues     = new float[CHANNEL_COUNT];
        private readonly Texture2D[] m_channelTextures      = new Texture2D[CHANNEL_COUNT];
        private readonly ChannelMask[] m_channelMasks       = new ChannelMask[CHANNEL_COUNT];
        private readonly SamplingType[] m_samplingTypes     = new SamplingType[CHANNEL_COUNT];
        private readonly bool[] m_channelInverts            = new bool[CHANNEL_COUNT];
        private readonly float[] m_channelScalers           = new float[CHANNEL_COUNT];
        private readonly Vector2[] m_channelClamp           = new Vector2[CHANNEL_COUNT];
        private readonly Vector2[] m_channelClip            = new Vector2[CHANNEL_COUNT];
        
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

        private void Update()
        {
            UpdatePreviewIfDirty();
        }

        private void UpdatePreviewIfDirty()
        {
            if (!m_isRTDirty)
                return;
            
            m_renderTextureGenerator.SetData(
                m_channelDefaultValues, 
                m_channelMasks,
                m_channelInverts, 
                m_channelScalers, 
                m_channelClamp, 
                m_channelClip, 
                m_channelTextures,
                m_samplingTypes,
                m_previewMasking);
            
            m_renderTextureGenerator.RegenerateRenderTextures(
                ref m_resultRT, 
                ref m_previewResultRT, 
                m_textureSize, 
                RenderTextureFormat.ARGB32);
            
            m_previewResultImage.image = m_previewResultRT;
            m_isRTDirty = false;
        }
        
        private void CreateGUI()
        {
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                m_channelMasks[i] = ChannelMask.R;
                m_channelClamp[i] = new Vector2(0, 1);
                m_channelClip[i] = new Vector2(0, 1);
                m_channelScalers[i] = 1.0f;
            }
            
            VisualElement root = rootVisualElement;
            root.style.justifyContent = Justify.FlexStart;
            root.style.alignItems = Align.FlexStart;
            
            ScrollView scrollView = new ScrollView();
            scrollView.style.width = Length.Percent(100f);
            scrollView.style.height = Length.Percent(100f);
            root.Add(scrollView);
            
            VisualElement mainElementsGroup = new VisualElement();
            mainElementsGroup.style.flexDirection = FlexDirection.Column;
            mainElementsGroup.style.marginTop = BASE_PADDING;
            mainElementsGroup.style.marginLeft = BASE_PADDING;
            mainElementsGroup.style.marginRight = BASE_PADDING;
            mainElementsGroup.style.marginBottom = BASE_PADDING;
            mainElementsGroup.style.minWidth = WINDOW_WIDTH;
            mainElementsGroup.style.minHeight = 64;
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
                topElement.style.marginTop = BASE_PADDING;
                topElement.style.flexDirection = FlexDirection.Row;
                topElement.style.minWidth = WINDOW_WIDTH - BASE_PADDING;
                topElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                topElement.style.paddingTop = SMALL_PADDING;
                topElement.style.paddingBottom = SMALL_PADDING;
                topElement.style.paddingLeft = SMALL_PADDING;
                topElement.style.paddingRight = SMALL_PADDING;
                topElement.style.justifyContent = Justify.Center;
                
                VisualElement verticalGroupLeft = new VisualElement();
                verticalGroupLeft.style.flexDirection = FlexDirection.Column;
                verticalGroupLeft.style.marginRight = BASE_PADDING;
                verticalGroupLeft.style.minWidth = 200;
                verticalGroupLeft.style.maxWidth = float.MaxValue;
                verticalGroupLeft.style.minHeight = 64;
                verticalGroupLeft.style.flexGrow = 1;
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
                        ? new Color(0.2f, 0.2f, 0.2f) 
                        : new Color(defaultValue, defaultValue, defaultValue);
                    
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
                    m_previewImages[index].style.backgroundColor = new Color(defaultValue, defaultValue, defaultValue);
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
                
                MinMaxSlider clampSlider = new MinMaxSlider("Clamp", m_channelClamp[index].x, m_channelClamp[index].y, 0, 1);
                clampSlider.RegisterValueChangedCallback(evt =>
                {
                    m_channelClamp[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                MinMaxSlider clipSlider = new MinMaxSlider("Clip", m_channelClip[index].x, m_channelClip[index].y, 0, 1);
                clipSlider.RegisterValueChangedCallback(evt =>
                {
                    m_channelClip[index] = evt.newValue;
                    m_isRTDirty = true;
                });
                EnumField samplingTypeField = new EnumField("Sampling Type", m_samplingTypes[index]);
                samplingTypeField.RegisterValueChangedCallback(evt =>
                {
                    m_samplingTypes[index] = (SamplingType)evt.newValue;
                    m_isRTDirty = true;
                });
    
                verticalGroupLeft.Add(textureField);
                verticalGroupLeft.Add(noTextureGroup);
                verticalGroupLeft.Add(textureGroup);
                
                textureGroup.Add(channelEnumField);
                textureGroup.Add(invertToggle);
                textureGroup.Add(channelScalerField);
                textureGroup.Add(clampSlider);
                textureGroup.Add(clipSlider);
                textureGroup.Add(samplingTypeField);
                
                noTextureGroup.Add(defaultValueField);
    
                var defaultValue = m_channelDefaultValues[index];
                Image previewImage = m_previewImages[index] ?? new Image 
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    style = {
                        alignSelf       = Align.Center,
                        width           = 64,
                        height          = 64,
                        backgroundColor = new Color(defaultValue, defaultValue, defaultValue),
                    },
                    image = texture
                };
                m_previewImages[index] = previewImage;
                
                Label textureSizeLabel = new Label(texture == null ? "" : $"{texture.width} x {texture.height}");
                textureSizeLabel.style.fontSize = 10;
                textureSizeLabel.style.alignSelf = Align.Center;
                textureSizeLabel.SetVisibility(texture != null ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                m_channelTextureSizeLabels[index] = textureSizeLabel;
                verticalGroupLeft.Add(textureSizeLabel);
                
                VisualElement verticalGroupRight = new VisualElement();
                verticalGroupRight.style.flexDirection = FlexDirection.Column;
                verticalGroupRight.style.maxWidth = 64;
                verticalGroupRight.style.minHeight = 64;
                verticalGroupRight.style.justifyContent = Justify.FlexStart;
                
                VisualElement colorStripElement = new VisualElement();
                colorStripElement.style.minHeight = 4;
                colorStripElement.style.minWidth = 64;
                colorStripElement.style.maxWidth = 64;
                colorStripElement.style.marginBottom = 4;
                colorStripElement.style.backgroundColor = index switch
                {
                    1 => new Color(0.6f, 1, 0.1f),
                    2 => new Color(0.1f, 0.7f, 0.9f),
                    3 => Color.white,
                    _ => new Color(1, 0.2f, 0.4f),
                };
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
                    Mathf.Clamp(evt.newValue.x, 0, MAX_RESOLUTION),
                    Mathf.Clamp(evt.newValue.y, 0, MAX_RESOLUTION));

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
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f)
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

        private void ExportPackedTexture()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                title: "Export Packed Texture", 
                defaultName: "image", 
                extension: "png", 
                message: string.Empty);
            m_resultRT.TryExportToPNG(m_textureSize, path);
        }

        private void ResetData()
        {
            // I'm lazy ^_^
            Close();
            OpenWindow();
        }
    }
}