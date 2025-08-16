using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerEditor : EditorWindow
    {
        private const string FILE_PICKER_ICON_PATH =
            "Packages/com.ameworks.channelpacker/Editor/Icons/FilePickerIcon.png";

        private const int CHANNEL_COUNT = 4;
        private const float BASE_PADDING = 10.0f;
        private const float SMALL_PADDING = 4.0f;
        private const float WINDOW_WIDTH = 274 + BASE_PADDING * 2;
        private const float MIN_WINDOW_HEIGHT = 128 + BASE_PADDING * 2;

        private readonly ChannelPackerGenerator _channelPackerGenerator = new();
        
        // Data
        private readonly float[] _channelDefaultValues = new float[CHANNEL_COUNT];
        private readonly Texture2D[] _channelTextures = new Texture2D[CHANNEL_COUNT];
        private readonly ChannelMask[] _channelMasks = new ChannelMask[CHANNEL_COUNT];
        private readonly bool[] _channelInvertValues = new bool[CHANNEL_COUNT];
        private readonly float[] _channelScalers = new float[CHANNEL_COUNT];
        private readonly float[] _channelMin = new float[CHANNEL_COUNT];
        private readonly float[] _channelMax = new float[CHANNEL_COUNT];

        private Vector2Int _textureSize = new (128, 128);
        private RenderTexture _resultRT;
        private bool _isRTDirty;
        
        private string _outputDirectory = string.Empty;
        private string _fileName = string.Empty;
        
        // Elements
        private readonly FloatField[] _channelDefaultValueFields = new FloatField[CHANNEL_COUNT];
        private readonly EnumField[] _channelEnumFields = new EnumField[CHANNEL_COUNT];
        private readonly Toggle[] _invertValuesToggles = new Toggle[CHANNEL_COUNT];
        private readonly FloatField[] _channelScalerFields = new FloatField[CHANNEL_COUNT];
        private readonly FloatField[] _channelMinFields = new FloatField[CHANNEL_COUNT];
        private readonly FloatField[] _channelMaxFields = new FloatField[CHANNEL_COUNT];
        private readonly Image[] _previewImages = new Image[CHANNEL_COUNT];
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
                _channelPackerGenerator.SetData(
                    _channelDefaultValues, 
                    _channelMasks,
                    _channelInvertValues, 
                    _channelScalers, 
                    _channelMin, 
                    _channelMax, 
                    _channelTextures
                );
                _channelPackerGenerator.UpdateRenderTexture(ref _resultRT, _textureSize, RenderTextureFormat.ARGB32);
                _previewResultImage.image = _resultRT;
                _isRTDirty = false;
            }
        }

        private void CreateGUI()
        {
            _channelPackerGenerator.Init();
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
                _textureSize.x = Mathf.Clamp(evt.newValue.x, 0, 8192);
                _textureSize.y = Mathf.Clamp(evt.newValue.y, 0, 8192);
                textureSizeField.value = _textureSize;
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
                _channelPackerGenerator.ExportToPNG(_resultRT, _textureSize, _outputDirectory, _fileName);
            });
            exportButton.text = "Export PNG";
            
            var previewResultImage = new Image 
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = 
                {
                    width = 256,
                    height = 256,
                    marginTop = BASE_PADDING,
                    alignSelf = Align.Center,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f)
                },
            };
            _previewResultImage = previewResultImage;

            mainElementsGroup.Add(textureSizeField);
            mainElementsGroup.Add(previewResultImage);
            mainElementsGroup.Add(directoryPickerGroup);
            mainElementsGroup.Add(fileNameField);
            mainElementsGroup.Add(exportButton);
        }

        private void AddChannelTextureElement(VisualElement parent, int index)
        {
            VisualElement topElement = new VisualElement();
            topElement.style.flexDirection = FlexDirection.Row;
            topElement.style.marginTop = BASE_PADDING;
            topElement.style.minWidth = WINDOW_WIDTH - BASE_PADDING;
            topElement.style.justifyContent = Justify.FlexStart;
            topElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            topElement.style.paddingTop = SMALL_PADDING;
            topElement.style.paddingBottom = SMALL_PADDING;
            topElement.style.paddingLeft = SMALL_PADDING;
            topElement.style.paddingRight = SMALL_PADDING;
            topElement.style.justifyContent = Justify.Center;
            
            VisualElement verticalGroup = new VisualElement();
            verticalGroup.style.flexDirection = FlexDirection.Column;
            verticalGroup.style.marginRight = BASE_PADDING;
            verticalGroup.style.minWidth = 200;
            verticalGroup.style.maxWidth = float.MaxValue;
            verticalGroup.style.minHeight = 64;
            verticalGroup.style.flexGrow = 1;
            verticalGroup.style.justifyContent = Justify.FlexStart;
            
            ObjectField textureField = new ObjectField();
            textureField.objectType = typeof(Texture2D);
            textureField.allowSceneObjects = false;
            textureField.value = _channelTextures[index];
            textureField.RegisterValueChangedCallback(evt =>
            {
                _channelTextures[index] = evt.newValue as Texture2D;
                _previewImages[index].image = _channelTextures[index];
                var defaultValue = _channelDefaultValues[index];
                _previewImages[index].style.backgroundColor = new Color(defaultValue, defaultValue, defaultValue);
                
                _channelDefaultValueFields[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Collapsed : ElementDisplayOption.Visible);
                
                _channelEnumFields[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
                _invertValuesToggles[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
                _channelScalerFields[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
                _channelMinFields[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
                _channelMaxFields[index].SetDisplayOption(evt.newValue != null 
                    ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
                
                _isRTDirty = true;
            });

            FloatField defaultValueField = new FloatField("No Texture Source");
            defaultValueField.SetDisplayOption(_channelTextures[index] == null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            defaultValueField.value = _channelDefaultValues[index];
            defaultValueField.RegisterValueChangedCallback(evt => 
            {
                _channelDefaultValues[index] = evt.newValue;
                var defaultValue = evt.newValue;
                _previewImages[index].style.backgroundColor = new Color(defaultValue, defaultValue, defaultValue);
                _isRTDirty = true;
            });
            _channelDefaultValueFields[index] = defaultValueField;

            EnumField channelEnumField = new EnumField("Channel Mask", _channelMasks[index]);
            channelEnumField.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            channelEnumField.RegisterValueChangedCallback(evt =>
            {
                _channelMasks[index] = (ChannelMask)evt.newValue;
                _isRTDirty = true;
            });
            _channelEnumFields[index] = channelEnumField;
            
            Toggle invertValuesToggle = new Toggle("Invert");
            invertValuesToggle.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            invertValuesToggle.RegisterValueChangedCallback(evt =>
            {
                _channelInvertValues[index] = evt.newValue;
                _isRTDirty = true;
            });
            _invertValuesToggles[index] = invertValuesToggle;

            FloatField channelScalerField = new FloatField("Scale");
            channelScalerField.value = _channelScalers[index];
            channelScalerField.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            channelScalerField.RegisterValueChangedCallback(evt =>
            {
                _channelScalers[index] = evt.newValue;
                _isRTDirty = true;
            });
            _channelScalerFields[index] = channelScalerField;
            FloatField channelMinField = new FloatField("Min");
            channelMinField.value = _channelMin[index];
            channelMinField.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            channelMinField.RegisterValueChangedCallback(evt =>
            {
                _channelMin[index] = evt.newValue;
                _isRTDirty = true;
            });
            _channelMinFields[index] = channelMinField;
            FloatField channelMaxField = new FloatField("Max");
            channelMaxField.value = _channelMax[index];
            channelMaxField.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            channelMaxField.RegisterValueChangedCallback(evt =>
            {
                _channelMax[index] = evt.newValue;
                _isRTDirty = true;
            });
            _channelMaxFields[index] = channelMaxField;

            verticalGroup.Add(textureField);
            verticalGroup.Add(channelEnumField);
            verticalGroup.Add(invertValuesToggle);
            verticalGroup.Add(channelScalerField);
            verticalGroup.Add(channelMinField);
            verticalGroup.Add(channelMaxField);
            verticalGroup.Add(defaultValueField);

            var defaultValue = _channelDefaultValues[index];
            Image previewImage = _previewImages[index] ?? new Image 
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = 
                {
                    width = 64,
                    height = 64,
                    backgroundColor = new Color(defaultValue, defaultValue, defaultValue),
                },
                image = _channelTextures[index]
            };
            _previewImages[index] = previewImage;
            
            topElement.Add(verticalGroup);
            topElement.Add(previewImage);
            parent.Add(topElement);
        }
    }
}