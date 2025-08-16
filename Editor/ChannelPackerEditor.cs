using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AmeWorks.ChannelPacker.Editor
{
    public class ChannelPackerEditor : EditorWindow
    {
        private const string FILE_PICKER_ICON_PATH =
            "Packages/com.ameworks.channelpacker/Assets/Icons/FilePickerIcon.png";

        private const int CHANNEL_COUNT = 4;
        private const float PADDING = 10;
        private const float WINDOW_WIDTH = 274 + PADDING * 2;
        private const float MIN_WINDOW_HEIGHT = 128 + PADDING * 2;

        private readonly ChannelPackerGenerator _channelPackerGenerator = new();
        
        // Data
        private readonly float[] _channelDefaultValues = new float[CHANNEL_COUNT];
        private readonly Texture2D[] _channelTextures = new Texture2D[CHANNEL_COUNT];
        private readonly ChannelMask[] _channelMasks = new ChannelMask[CHANNEL_COUNT];
        private readonly bool[] _channelInvertValues = new bool[CHANNEL_COUNT];
        private readonly float[] _channelScalers = new float[CHANNEL_COUNT];
        private readonly float[] _channelMin = new float[CHANNEL_COUNT];
        private readonly float[] _channelMax = new float[CHANNEL_COUNT];

        private Vector2Int _resultRTSize = new (128, 128);
        private RenderTextureFormat _resultRTFormat = RenderTextureFormat.ARGB32;
        private TextureFormat _resultTextureFormat = TextureFormat.ARGB32;
        private RenderTexture _resultRT;
        private bool _isRTDirty;
        
        private string _outputDirectory = string.Empty;
        private string _fileName = string.Empty;
        
        // Elements
        private readonly FloatField[] _channelDefaultValueFields = new FloatField[CHANNEL_COUNT];
        private readonly EnumField[] _channelEnumFields = new EnumField[CHANNEL_COUNT];
        private readonly Toggle[] _invertChannelToggles = new Toggle[CHANNEL_COUNT];
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
            wnd.maxSize = new Vector2(WINDOW_WIDTH + 16, float.MaxValue);
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
                _channelPackerGenerator.UpdateRenderTexture(ref _resultRT, _resultRTSize, _resultRTFormat);
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
            mainElementsGroup.style.marginLeft = PADDING;
            mainElementsGroup.style.minWidth = 280;
            mainElementsGroup.style.maxWidth = 280;
            mainElementsGroup.style.minHeight = 64;
            mainElementsGroup.style.justifyContent = Justify.FlexStart;
            
            scrollView.Add(mainElementsGroup);
            
            AddChannelTextureElement(mainElementsGroup, 0);
            AddChannelTextureElement(mainElementsGroup, 1);
            AddChannelTextureElement(mainElementsGroup, 2);
            AddChannelTextureElement(mainElementsGroup, 3);

            Vector2IntField textureSizeField = new Vector2IntField("Resolution");
            textureSizeField.value = _resultRTSize;
            textureSizeField.style.marginTop = PADDING * 2;
            textureSizeField.RegisterValueChangedCallback(evt =>
            {
                _resultRTSize = evt.newValue;
                _isRTDirty = true;
            });
            EnumField renderTextureFormatField = new EnumField("Render Texture Format", _resultRTFormat);
            renderTextureFormatField.RegisterValueChangedCallback(evt =>
            {
                _resultRTFormat = (RenderTextureFormat)evt.newValue;
                _isRTDirty = true;
            });
            
            VisualElement directoryPickerGroup = new VisualElement();
            directoryPickerGroup.style.marginTop = PADDING * 2;
            directoryPickerGroup.style.flexDirection = FlexDirection.Row;
            directoryPickerGroup.style.minWidth = WINDOW_WIDTH - PADDING;
            directoryPickerGroup.style.justifyContent = Justify.FlexStart;
            TextField outputDirectoryField = new TextField("Output Directory");
            outputDirectoryField.value = _outputDirectory;
            outputDirectoryField.RegisterValueChangedCallback(evt =>
            {
                _outputDirectory = evt.newValue;
            });
            outputDirectoryField.style.minWidth = WINDOW_WIDTH - PADDING * 3 - 25;
            outputDirectoryField.style.maxWidth = WINDOW_WIDTH - PADDING * 3 - 25;
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

            EnumField textureFormatField = new EnumField("Texture Format", _resultTextureFormat);
            textureFormatField.RegisterValueChangedCallback(evt =>
            {
                _resultTextureFormat = (TextureFormat)evt.newValue;
            });
            
            Button exportButton = new Button(() =>
            {
                var path = Path.Combine(_outputDirectory, $"{_fileName}.png");
                if (!Directory.Exists(_outputDirectory) || string.IsNullOrEmpty(_fileName))
                    return;
                _channelPackerGenerator.ExportToPNG(_resultRT, _resultRTSize, _resultTextureFormat, path);
            });
            exportButton.text = "Export";
            
            _previewResultImage = new Image 
            {
                scaleMode = ScaleMode.ScaleToFit,
                style = 
                {
                    width = 256,
                    height = 256,
                    marginLeft = 12,
                    marginTop = 10,
                    alignSelf = Align.FlexStart,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f)
                },
            };

            mainElementsGroup.Add(textureSizeField);
            mainElementsGroup.Add(renderTextureFormatField);
            mainElementsGroup.Add(_previewResultImage);
            
            mainElementsGroup.Add(directoryPickerGroup);
            mainElementsGroup.Add(fileNameField);
            mainElementsGroup.Add(textureFormatField);
            mainElementsGroup.Add(exportButton);
        }

        private void AddChannelTextureElement(VisualElement parent, int index)
        {
            VisualElement topElement = new VisualElement();
            topElement.style.flexDirection = FlexDirection.Row;
            topElement.style.marginTop = PADDING;
            topElement.style.minWidth = WINDOW_WIDTH - PADDING;
            topElement.style.justifyContent = Justify.FlexStart;
            
            VisualElement verticalGroup = new VisualElement();
            verticalGroup.style.flexDirection = FlexDirection.Column;
            verticalGroup.style.marginRight = PADDING;
            verticalGroup.style.minWidth = 200;
            verticalGroup.style.maxWidth = 200;
            verticalGroup.style.minHeight = 64;
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
                _invertChannelToggles[index].SetDisplayOption(evt.newValue != null 
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
            
            Toggle invertChannelToggle = new Toggle("Invert");
            invertChannelToggle.SetDisplayOption(_channelTextures[index] != null 
                ? ElementDisplayOption.Visible : ElementDisplayOption.Collapsed);
            invertChannelToggle.RegisterValueChangedCallback(evt =>
            {
                _channelInvertValues[index] = evt.newValue;
                _isRTDirty = true;
            });
            _invertChannelToggles[index] = invertChannelToggle;

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
            verticalGroup.Add(invertChannelToggle);
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
                    backgroundColor = new Color(defaultValue, defaultValue, defaultValue)
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