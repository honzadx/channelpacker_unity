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

        [SerializeField] private ChromaPackerRTGenerator _renderTextureGenerator;
        
        // Data
        private readonly float[] _channelDefaultValues = new float[CHANNEL_COUNT];
        private readonly Texture2D[] _channelTextures = new Texture2D[CHANNEL_COUNT];
        private readonly ChannelMask[] _channelMasks = new ChannelMask[CHANNEL_COUNT];
        private readonly SamplingType[] _samplingTypes = new SamplingType[CHANNEL_COUNT];
        private readonly bool[] _channelInverts = new bool[CHANNEL_COUNT];
        private readonly float[] _channelScalers = new float[CHANNEL_COUNT];
        private readonly Vector2[] _channelClamp = new Vector2[CHANNEL_COUNT];
        private readonly Vector2[] _channelClip = new Vector2[CHANNEL_COUNT];
        
        private ChannelMask _previewMasking = ChannelMask.R | ChannelMask.G | ChannelMask.B | ChannelMask.A;
        private Vector2Int _textureSize = new (128, 128);
        private RenderTexture _resultRT;
        private RenderTexture _previewResultRT;
        private bool _isRTDirty;
        
        // Elements
        private readonly Label[] _channelTextureSizeLabels = new Label[CHANNEL_COUNT];
        private readonly Image[] _previewImages = new Image[CHANNEL_COUNT];
        private readonly VisualElement[] _noTextureGroups = new VisualElement[CHANNEL_COUNT];
        private readonly VisualElement[] _textureGroups = new VisualElement[CHANNEL_COUNT];
        private Image _previewResultImage;
        
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
            if (!_isRTDirty)
                return;
            
            _renderTextureGenerator.SetData(
                _channelDefaultValues, 
                _channelMasks,
                _channelInverts, 
                _channelScalers, 
                _channelClamp, 
                _channelClip, 
                _channelTextures,
                _samplingTypes,
                _previewMasking);
            
            _renderTextureGenerator.RegenerateRenderTextures(
                ref _resultRT, 
                ref _previewResultRT, 
                _textureSize, 
                RenderTextureFormat.ARGB32);
            
            _previewResultImage.image = _previewResultRT;
            _isRTDirty = false;
        }
        
        private void CreateGUI()
        {
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                _channelMasks[i] = ChannelMask.R;
                _channelClamp[i] = new Vector2(0, 1);
                _channelClip[i] = new Vector2(0, 1);
                _channelScalers[i] = 1.0f;
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
                var texture = _channelTextures[index];
                
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
                _noTextureGroups[index] = noTextureGroup;
                
                VisualElement textureGroup = new VisualElement();
                textureGroup.SetVisibility(texture != null ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                textureGroup.style.flexDirection = FlexDirection.Column;
                _textureGroups[index] = textureGroup;
                
                ObjectField textureField = new ObjectField();
                textureField.objectType = typeof(Texture2D);
                textureField.allowSceneObjects = false;
                textureField.value = texture;
                textureField.RegisterValueChangedCallback(evt =>
                {
                    var newTexture = evt.newValue as Texture2D;
                    var defaultValue = _channelDefaultValues[index];
                    var isTextureValid = newTexture != null;
                    _channelTextures[index] = newTexture;
                    _previewImages[index].image = newTexture;
                    _previewImages[index].style.backgroundColor = isTextureValid 
                        ? new Color(0.2f, 0.2f, 0.2f) 
                        : new Color(defaultValue, defaultValue, defaultValue);
                    
                    _noTextureGroups[index].SetVisibility(isTextureValid ? ElementVisibility.Collapsed : ElementVisibility.Visible);
                    _textureGroups[index].SetVisibility(isTextureValid ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                    _channelTextureSizeLabels[index].SetVisibility(isTextureValid ? ElementVisibility.Visible : ElementVisibility.Collapsed);
    
                    _channelTextureSizeLabels[index].text = newTexture == null ? "" : $"{newTexture.width} x {newTexture.height}";
                    _isRTDirty = true;
                });
    
                FloatField defaultValueField = new FloatField("No Texture Source");
                defaultValueField.value = _channelDefaultValues[index];
                defaultValueField.RegisterValueChangedCallback(evt => 
                {
                    var defaultValue = evt.newValue;
                    _channelDefaultValues[index] = defaultValue;
                    _previewImages[index].style.backgroundColor = new Color(defaultValue, defaultValue, defaultValue);
                    _isRTDirty = true;
                });
                EnumField channelEnumField = new EnumField("Channel Mask", _channelMasks[index]);
                channelEnumField.RegisterValueChangedCallback(evt =>
                {
                    _channelMasks[index] = (ChannelMask)evt.newValue;
                    _isRTDirty = true;
                });
                Toggle invertToggle = new Toggle("Invert");
                invertToggle.RegisterValueChangedCallback(evt =>
                {
                    _channelInverts[index] = evt.newValue;
                    _isRTDirty = true;
                });
                FloatField channelScalerField = new FloatField("Scale");
                channelScalerField.value = _channelScalers[index];
                channelScalerField.RegisterValueChangedCallback(evt =>
                {
                    _channelScalers[index] = evt.newValue;
                    _isRTDirty = true;
                });
                
                MinMaxSlider clampSlider = new MinMaxSlider("Clamp", _channelClamp[index].x, _channelClamp[index].y, 0, 1);
                clampSlider.RegisterValueChangedCallback(evt =>
                {
                    _channelClamp[index] = evt.newValue;
                    _isRTDirty = true;
                });
                MinMaxSlider clipSlider = new MinMaxSlider("Clip", _channelClip[index].x, _channelClip[index].y, 0, 1);
                clipSlider.RegisterValueChangedCallback(evt =>
                {
                    _channelClip[index] = evt.newValue;
                    _isRTDirty = true;
                });
                EnumField samplingTypeField = new EnumField("Sampling Type", _samplingTypes[index]);
                samplingTypeField.RegisterValueChangedCallback(evt =>
                {
                    _samplingTypes[index] = (SamplingType)evt.newValue;
                    _isRTDirty = true;
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
    
                var defaultValue = _channelDefaultValues[index];
                Image previewImage = _previewImages[index] ?? new Image 
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
                _previewImages[index] = previewImage;
                
                Label textureSizeLabel = new Label(texture == null ? "" : $"{texture.width} x {texture.height}");
                textureSizeLabel.style.fontSize = 10;
                textureSizeLabel.style.alignSelf = Align.Center;
                textureSizeLabel.SetVisibility(texture != null ? ElementVisibility.Visible : ElementVisibility.Collapsed);
                _channelTextureSizeLabels[index] = textureSizeLabel;
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
            textureSizeField.value = _textureSize;
            textureSizeField.style.marginTop = BASE_PADDING * 2;
            textureSizeField.RegisterValueChangedCallback(evt =>
            {
                var sanitizedResolution = new Vector2Int(
                    Mathf.Clamp(evt.newValue.x, 0, MAX_RESOLUTION),
                    Mathf.Clamp(evt.newValue.y, 0, MAX_RESOLUTION));

                textureSizeField.value = sanitizedResolution;
                _textureSize = sanitizedResolution;
                _isRTDirty = true;
            });

            EnumFlagsField previewFlagsField = new EnumFlagsField("Preview Filter", _previewMasking);
            previewFlagsField.RegisterValueChangedCallback(evt =>
            {
                _previewMasking = (ChannelMask)evt.newValue;
                _previewResultImage.SetVisibility((int)_previewMasking == 0 ? ElementVisibility.Collapsed : ElementVisibility.Visible);
                _isRTDirty = true;
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
            previewResultImage.SetVisibility((int)_previewMasking == 0 ? ElementVisibility.Collapsed : ElementVisibility.Visible);
            _previewResultImage = previewResultImage;
            
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
            _resultRT.TryExportToPNG(_textureSize, path);
        }

        private void ResetData()
        {
            // I'm lazy ^_^
            Close();
            OpenWindow();
        }
    }
}