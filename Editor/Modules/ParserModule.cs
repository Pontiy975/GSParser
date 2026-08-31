using System;
using System.Linq;
using GSParser.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSParser.Editor.Modules
{
    public class ParserModule : GSParserModule
    {
        // Preset
        private DropdownField _presetDropdown;
        private Button _presetAddBtn;
        private Button _presetRemoveBtn;
        private TextField _presetLabelField;

        // Target
        private DropdownField _typeDropdown;

        // Output
        private Label _savePathLabel;
        private Button _browseBtn;

        // Actions
        private Button _parseBtn;
        private Label _statusLabel;
        private Label _warningLabel;

        private Type[] _parsableTypes = Array.Empty<Type>();
        private Type _selectedType;
        private bool _initialized;

        public ParserModule(VisualElement root) : base(root) { }

        public override void Initialize()
        {
            var tab = Root.Q<VisualElement>("ParserTab");
            if (tab == null) { Debug.LogError("[GSParser] ParserTab not found"); return; }

            _presetDropdown = tab.Q<DropdownField>("parser-preset-select");
            _presetAddBtn = tab.Q<Button>("parser-preset-add");
            _presetRemoveBtn = tab.Q<Button>("parser-preset-remove");
            _presetLabelField = tab.Q<TextField>("parser-preset-label");

            _typeDropdown = tab.Q<DropdownField>("parser-target-type");

            _savePathLabel = tab.Q<Label>("parser-save-path-label");
            _browseBtn = tab.Q<Button>("parser-browse-path");

            _parseBtn = tab.Q<Button>("parser-parse");
            _statusLabel = tab.Q<Label>("parser-status");
            _warningLabel = tab.Q<Label>("parser-warning");

            if (_presetDropdown == null || _typeDropdown == null ||
                _savePathLabel == null || _parseBtn == null)
            {
                Debug.LogError("[GSParser] ParserModule: required UI elements not found");
                return;
            }

            _initialized = true;

            // Preset list
            _presetAddBtn.clicked += () => GSParserService.AddPreset();
            _presetRemoveBtn.clicked += () => GSParserService.RemovePreset(GSParserService.Config.activePresetIndex);

            _presetDropdown.RegisterValueChangedCallback(evt =>
            {
                var idx = _presetDropdown.choices.IndexOf(evt.newValue);
                if (idx >= 0) GSParserService.SetActivePreset(idx);
            });

            _presetLabelField.RegisterCallback<BlurEvent>(_ =>
            {
                GSParserService.Config.ActivePreset.label = _presetLabelField.value;
                GSParserService.SaveConfig();
                RefreshPresetDropdown();
            });

            // Type
            _typeDropdown.RegisterValueChangedCallback(evt =>
            {
                var idx = _typeDropdown.choices.IndexOf(evt.newValue);
                _selectedType = idx >= 0 && idx < _parsableTypes.Length ? _parsableTypes[idx] : null;
                GSParserService.Config.ActivePreset.targetTypeName = _selectedType?.AssemblyQualifiedName ?? "";
                GSParserService.SaveConfig();
                RefreshParseBtn();
            });

            // Browse
            _browseBtn.clicked += OnBrowse;

            // Parse
            _parseBtn.clicked += OnParse;

            // Service events
            GSParserService.OnDataLoaded += _ => RefreshParseBtn();
            GSParserService.OnDisconnected += RefreshParseBtn;
            GSParserService.OnConfigChanged += Refresh;
            GSParserService.OnParseCompleted += count => SetStatus($"Done: {count} assets processed", "status--connected");

            Refresh();
        }

        public override void OnShow()
        {
            if (_initialized) Refresh();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (!_initialized) return;
            RefreshPresetDropdown();
            RefreshPresetFields();
            RefreshTypeDropdown();
            RefreshSavePath();
            RefreshParseBtn();
        }

        private void RefreshPresetDropdown()
        {
            var labels = GSParserService.Config.presets.Select(p => p.label).ToList();
            _presetDropdown.choices = labels;
            _presetDropdown.SetValueWithoutNotify(labels[GSParserService.Config.activePresetIndex]);
            _presetRemoveBtn.SetEnabled(GSParserService.Config.presets.Count > 1);
        }

        private void RefreshPresetFields()
        {
            _presetLabelField.SetValueWithoutNotify(GSParserService.Config.ActivePreset.label);
        }

        private void RefreshTypeDropdown()
        {
            _parsableTypes = ReflectionUtils.GetParsableTypes();
            var names = _parsableTypes.Select(t => t.Name).ToList();
            _typeDropdown.choices = names;

            var saved = GSParserService.Config.ActivePreset.targetTypeName;
            _selectedType = string.IsNullOrEmpty(saved)
                ? null
                : _parsableTypes.FirstOrDefault(t => t.AssemblyQualifiedName == saved);

            if (_selectedType == null && _parsableTypes.Length > 0)
            {
                _selectedType = _parsableTypes[0];
                GSParserService.Config.ActivePreset.targetTypeName = _selectedType.AssemblyQualifiedName;
                GSParserService.SaveConfig();
            }

            _typeDropdown.SetValueWithoutNotify(_selectedType?.Name ?? "");
        }

        private void RefreshSavePath()
        {
            _savePathLabel.text = GSParserService.Config.ActivePreset.savePath;
        }

        private void RefreshParseBtn()
        {
            if (!_initialized) return;
            bool canParse = GSParserService.IsConnected && _selectedType != null;
            _parseBtn.SetEnabled(canParse);

            if (!GSParserService.IsConnected)
                SetStatus("Load a sheet first", "status--idle");
            else if (_selectedType == null)
                SetStatus("Select a target type", "status--idle");
            else
                SetStatus($"Ready  ·  {_selectedType.Name}", "status--connected");
        }

        // ── Actions ───────────────────────────────────────────────────────────

        private void OnBrowse()
        {
            var path = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;

            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path[Application.dataPath.Length..];

            GSParserService.Config.ActivePreset.savePath = path;
            GSParserService.SaveConfig();
            _savePathLabel.text = path;
        }

        private void OnParse()
        {
            if (_selectedType == null) { SetStatus("Select a target type", "status--error"); return; }

            var savePath = GSParserService.Config.ActivePreset.savePath;
            if (string.IsNullOrWhiteSpace(savePath)) { SetStatus("Set save path first", "status--error"); return; }

            SetStatus("Parsing...", "status--loading");
            GSParserService.Parse(_selectedType, savePath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetStatus(string msg, string cssClass = "")
        {
            if (_statusLabel == null) return;
            _statusLabel.text = msg;
            _statusLabel.RemoveFromClassList("status--idle");
            _statusLabel.RemoveFromClassList("status--connected");
            _statusLabel.RemoveFromClassList("status--loading");
            _statusLabel.RemoveFromClassList("status--error");
            if (!string.IsNullOrEmpty(cssClass))
                _statusLabel.AddToClassList(cssClass);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (!_initialized) return;
            GSParserService.OnDataLoaded -= _ => RefreshParseBtn();
            GSParserService.OnDisconnected -= RefreshParseBtn;
            GSParserService.OnConfigChanged -= Refresh;
        }
    }
}