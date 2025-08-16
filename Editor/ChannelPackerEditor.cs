using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerEditor : EditorWindow
    {
        private const string FILE_PICKER_ICON_PATH = "Packages/com.ameworks.channelpacker/Editor/Icons/FilePickerIcon.png";

        private const int MAX_RESOLUTION = 8192;
        private const int CHANNEL_COUNT = 4;
        private const float BASE_PADDING = 10.0f;
        private const float SMALL_PADDING = 4.0f;
        private const float WINDOW_WIDTH = 274 + BASE_PADDING * 2;
        private const float MIN_WINDOW_HEIGHT = 128 + BASE_PADDING * 2;

        private static readonly Color _backgroundColor = new (0.2f, 0.2f, 0.2f);
        
        private readonly ChannelPackerRTGenerator _renderTextureGenerator = new();
        
        // Data
        private readonly float[] _channelDefaultValues = new float[CHANNEL_COUNT];
        private readonly Texture2D[] _channelTextures = new Texture2D[CHANNEL_COUNT];
        private readonly ChannelMask[] _channelMasks = new ChannelMask[CHANNEL_COUNT];
        private readonly SamplingType[] _samplingTypes = new SamplingType[CHANNEL_COUNT];
        private readonly bool[] _channelInvertValues = new bool[CHANNEL_COUNT];
        private readonly float[] _channelScalers = new float[CHANNEL_COUNT];
        private readonly float[] _channelMin = new float[CHANNEL_COUNT];
        private readonly float[] _channelMax = new float[CHANNEL_COUNT];
        
        private ChannelMask _previewMasking = ChannelMask.R | ChannelMask.G | ChannelMask.B | ChannelMask.A;
        private Vector2Int _textureSize = new (128, 128);
        private RenderTexture _resultRT;
        private RenderTexture _previewResultRT;
        private bool _isRTDirty;
        
        private string _outputDirectory = string.Empty;
        private string _fileName = string.Empty;
        
        // Elements
        private readonly Label[] _channelTextureSizeLabels = new Label[CHANNEL_COUNT];
        private readonly Image[] _previewImages = new Image[CHANNEL_COUNT];
        private readonly VisualElement[] _noTextureGroups = new VisualElement[CHANNEL_COUNT];
        private readonly VisualElement[] _textureGroups = new VisualElement[CHANNEL_COUNT];
        private Image _previewResultImage;
        
        [MenuItem("Tools/Channel Packer")]
        public static void OpenWindow()
        {
            ChannelPackerEditor wnd = GetWindow<ChannelPackerEditor>();
            wnd.titleContent = new GUIContent("Channel Packer");
            wnd.minSize = new Vector2(WINDOW_WIDTH + 16, MIN_WINDOW_HEIGHT);
        }

        private void Update()
        {
            if (_isRTDirty)
            {
                _renderTextureGenerator.SetData(
                    _channelDefaultValues, 
                    _channelMasks,
                    _channelInvertValues, 
                    _channelScalers, 
                    _channelMin, 
                    _channelMax, 
                    _channelTextures,
                    _samplingTypes,
                    _previewMasking
                );
                _renderTextureGenerator.RegenerateRenderTexture(ref _resultRT, ref _previewResultRT, _textureSize, RenderTextureFormat.ARGB32);
                _previewResultImage.image = _previewResultRT;
                _isRTDirty = false;
            }
        }

        private void CreateGUI()
        {
            _renderTextureGenerator.Init();
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                _channelMasks[i] = ChannelMask.R;
                _channelMax[i] = 1.0f;
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
            mainElementsGroup.style.marginTop = 10;
            mainElementsGroup.style.marginLeft = BASE_PADDING;
            mainElementsGroup.style.marginRight = BASE_PADDING;
            mainElementsGroup.style.minWidth = 280;
            mainElementsGroup.style.minHeight = 64;
            mainElementsGroup.style.justifyContent = Justify.FlexStart;
            
            scrollView.Add(mainElementsGroup);
            
            AddChannelTextureElement(mainElementsGroup, 0);
            AddChannelTextureElement(mainElementsGroup, 1);
            AddChannelTextureElement(mainElementsGroup, 2);
            AddChannelTextureElement(mainElementsGroup, 3);

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
                _isRTDirty = true;
            });
            
            VisualElement directoryPickerGroup = new VisualElement();
            directoryPickerGroup.style.marginTop = BASE_PADDING * 2;
            directoryPickerGroup.style.flexDirection = FlexDirection.Row;
            directoryPickerGroup.style.minWidth = WINDOW_WIDTH - BASE_PADDING;
            directoryPickerGroup.style.justifyContent = Justify.FlexStart;
            directoryPickerGroup.style.flexGrow = 1;
            
            TextField outputDirectoryField = new TextField("Output Directory");
            outputDirectoryField.style.flexGrow = 1;
            outputDirectoryField.value = _outputDirectory;
            outputDirectoryField.RegisterValueChangedCallback(evt =>
            {
                _outputDirectory = evt.newValue;
            });
            outputDirectoryField.style.minWidth = WINDOW_WIDTH - BASE_PADDING * 3 - 25;
            
            Texture2D filePickerTexture = (Texture2D)EditorGUIUtility.Load(FILE_PICKER_ICON_PATH);
            Background filePickerBackground = Background.FromTexture2D(filePickerTexture);
            Button outputDirectoryButton = new Button(filePickerBackground,() =>
            {
                _outputDirectory = EditorUtility.OpenFolderPanel("Select a folder", _outputDirectory, "");
                if (!string.IsNullOrEmpty(_outputDirectory))
                {
                    outputDirectoryField.value = _outputDirectory;
                }
            });
            outputDirectoryButton.style.maxWidth = 25;
            directoryPickerGroup.Add(outputDirectoryField);
            directoryPickerGroup.Add(outputDirectoryButton);
            
            TextField fileNameField = new TextField("File Name");
            fileNameField.value = _fileName;
            fileNameField.RegisterValueChangedCallback(evt =>
            {
                _fileName = evt.newValue;
            });
            
            Button exportButton = new Button(() =>
            {
                _resultRT.TryExportToPNG(_textureSize, _outputDirectory, _fileName);
            });
            exportButton.text = "Export PNG";
            
            var previewResultImage = new Image 
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = {
                    width           = 256,
                    height          = 256,
                    marginTop       = BASE_PADDING,
                    alignSelf       = Align.Center,
                    backgroundColor = _backgroundColor
                },
            };
            _previewResultImage = previewResultImage;

            mainElementsGroup.Add(textureSizeField);
            mainElementsGroup.Add(previewFlagsField);
            mainElementsGroup.Add(previewResultImage);
            mainElementsGroup.Add(directoryPickerGroup);
            mainElementsGroup.Add(fileNameField);
            mainElementsGroup.Add(exportButton);
        }

        private void AddChannelTextureElement(VisualElement parent, int index)
        {
            var texture = _channelTextures[index];
                
            VisualElement topElement = new VisualElement();
            topElement.style.flexDirection = FlexDirection.Row;
            topElement.style.marginTop = BASE_PADDING;
            topElement.style.minWidth = WINDOW_WIDTH - BASE_PADDING;
            topElement.style.justifyContent = Justify.FlexStart;
            topElement.style.backgroundColor = _backgroundColor;
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
            noTextureGroup.SetDisplayOption(texture != null 
                ? ElementDisplayOption.Collapsed 
                : ElementDisplayOption.Visible);
            _noTextureGroups[index] = noTextureGroup;
            
            VisualElement textureGroup = new VisualElement();
            textureGroup.SetDisplayOption(texture != null 
                ? ElementDisplayOption.Visible 
                : ElementDisplayOption.Collapsed);
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
                    ? _backgroundColor 
                    : new Color(defaultValue, defaultValue, defaultValue);
                
                _noTextureGroups[index].SetDisplayOption(isTextureValid
                    ? ElementDisplayOption.Collapsed 
                    : ElementDisplayOption.Visible);
                _textureGroups[index].SetDisplayOption(isTextureValid
                    ? ElementDisplayOption.Visible 
                    : ElementDisplayOption.Collapsed);
                _channelTextureSizeLabels[index].SetDisplayOption(isTextureValid
                    ? ElementDisplayOption.Visible 
                    : ElementDisplayOption.Collapsed);

                _channelTextureSizeLabels[index].text = newTexture == null ? "" : $"{newTexture.width} x {newTexture.height}";
                _isRTDirty = true;
            });

            FloatField defaultValueField = new FloatField("No Texture Source");
            defaultValueField.value = _channelDefaultValues[index];
            defaultValueField.RegisterValueChangedCallback(evt => 
            {
                _channelDefaultValues[index] = evt.newValue;
                var defaultValue = evt.newValue;
                _previewImages[index].style.backgroundColor = new Color(defaultValue, defaultValue, defaultValue);
                _isRTDirty = true;
            });
            EnumField channelEnumField = new EnumField("Channel Mask", _channelMasks[index]);
            channelEnumField.RegisterValueChangedCallback(evt =>
            {
                _channelMasks[index] = (ChannelMask)evt.newValue;
                _isRTDirty = true;
            });
            Toggle invertValuesToggle = new Toggle("Invert");
            invertValuesToggle.RegisterValueChangedCallback(evt =>
            {
                _channelInvertValues[index] = evt.newValue;
                _isRTDirty = true;
            });
            FloatField channelScalerField = new FloatField("Scale");
            channelScalerField.value = _channelScalers[index];
            channelScalerField.RegisterValueChangedCallback(evt =>
            {
                _channelScalers[index] = evt.newValue;
                _isRTDirty = true;
            });
            FloatField channelMinField = new FloatField("Min");
            channelMinField.value = _channelMin[index];
            channelMinField.RegisterValueChangedCallback(evt =>
            {
                _channelMin[index] = evt.newValue;
                _isRTDirty = true;
            });
            FloatField channelMaxField = new FloatField("Max");
            channelMaxField.value = _channelMax[index];
            channelMaxField.RegisterValueChangedCallback(evt =>
            {
                _channelMax[index] = evt.newValue;
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
            textureGroup.Add(invertValuesToggle);
            textureGroup.Add(channelScalerField);
            textureGroup.Add(channelMinField);
            textureGroup.Add(channelMaxField);
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
            textureSizeLabel.SetDisplayOption(texture != null 
                ? ElementDisplayOption.Visible 
                : ElementDisplayOption.Collapsed);
            _channelTextureSizeLabels[index] = textureSizeLabel;
            verticalGroupLeft.Add(textureSizeLabel);
            
            VisualElement verticalGroupRight = new VisualElement();
            verticalGroupRight.style.flexDirection = FlexDirection.Column;
            verticalGroupRight.style.maxWidth = 64;
            verticalGroupRight.style.minHeight = 64;
            verticalGroupRight.style.justifyContent = Justify.FlexStart;
            
            verticalGroupRight.Add(previewImage);
            verticalGroupRight.Add(textureSizeLabel);

            topElement.Add(verticalGroupLeft);
            topElement.Add(verticalGroupRight);
            parent.Add(topElement);
        }
    }
}