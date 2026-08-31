using System;
using GSParser.Editor.Core;
using UnityEngine.UIElements;

namespace GSParser.Editor.Modules
{
    public class ConnectionModule : GSParserModule
    {
        private DropdownField _dropdown;
        private Button _addBtn;
        private Button _removeBtn;

        private TextField _labelField;
        private TextField _apikeyField;
        private TextField _sheetIdField;
        private TextField _sheetField;
        private TextField _rangeStartField;
        private TextField _rangeEndField;

        private Button _loadBtn;
        private Button _reconnectBtn;
        private Button _disconnectBtn;

        private Label _warningLabel;
        private Label _statusLabel;

        public ConnectionModule(VisualElement root) : base(root) { }

        public override void Initialize()
        {
            // All elements are direct children of ConnectionTab which is inside root
            var tab = Root.Q<VisualElement>("ConnectionTab");
            if (tab == null)
            {
                UnityEngine.Debug.LogError("[GSParser] ConnectionTab element not found in UXML");
                return;
            }

            _dropdown = tab.Q<DropdownField>("conn-dropdown");
            _addBtn = tab.Q<Button>("conn-add");
            _removeBtn = tab.Q<Button>("conn-remove");

            _labelField = tab.Q<TextField>("conn-label");
            _apikeyField = tab.Q<TextField>("conn-apikey");
            _sheetIdField = tab.Q<TextField>("conn-sheetid");
            _sheetField = tab.Q<TextField>("conn-sheet");
            _rangeStartField = tab.Q<TextField>("conn-range-start");
            _rangeEndField = tab.Q<TextField>("conn-range-end");

            _loadBtn = tab.Q<Button>("conn-load");
            _reconnectBtn = tab.Q<Button>("conn-reconnect");
            _disconnectBtn = tab.Q<Button>("conn-disconnect");

            _warningLabel = tab.Q<Label>("conn-warning");
            _statusLabel = tab.Q<Label>("conn-status");

            // Validate all refs
            if (!ValidateRefs()) return;

            // Connection list
            _addBtn.clicked += () => GSParserService.AddConnection();
            _removeBtn.clicked += () => GSParserService.RemoveConnection(GSParserService.Config.activeIndex);
            _dropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = _dropdown.choices.IndexOf(evt.newValue);
                if (idx >= 0) GSParserService.SetActiveConnection(idx);
            });

            // Fields — save on blur
            Blur(_labelField, v => GSParserService.Config.Active.label = v);
            Blur(_apikeyField, v => GSParserService.Config.Active.apiKey = v);
            Blur(_sheetIdField, v => GSParserService.Config.Active.spreadsheetID = v);
            Blur(_sheetField, v => GSParserService.Config.Active.sheetName = v);
            Blur(_rangeStartField, v => GSParserService.Config.Active.rangeStart = v);
            Blur(_rangeEndField, v => GSParserService.Config.Active.rangeEnd = v);

            // Action buttons
            _loadBtn.clicked += () => _ = GSParserService.LoadSheetAsync();
            _reconnectBtn.clicked += () => { GSParserService.Disconnect(); _ = GSParserService.LoadSheetAsync(); };
            _disconnectBtn.clicked += GSParserService.Disconnect;

            // Service events
            GSParserService.OnConnected += OnConnected;
            GSParserService.OnDisconnected += OnDisconnected;
            GSParserService.OnConfigChanged += Refresh;
            GSParserService.OnLog += SetStatus;

            Refresh();
            SyncConnectionState();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Blur(TextField field, Action<string> setter)
        {
            field.RegisterCallback<BlurEvent>(_ =>
            {
                setter(field.value);
                GSParserService.SaveConfig();
            });
        }

        private bool ValidateRefs()
        {
            var fields = new object[]
            {
                _dropdown, _addBtn, _removeBtn, _labelField, _apikeyField,
                _sheetIdField, _sheetField, _rangeStartField, _rangeEndField,
                _loadBtn, _reconnectBtn, _disconnectBtn, _warningLabel, _statusLabel
            };

            foreach (var f in fields)
            {
                if (f == null)
                {
                    UnityEngine.Debug.LogError("[GSParser] ConnectionModule: one or more UI elements not found. Check element names in UXML.");
                    return false;
                }
            }
            return true;
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            var cfg = GSParserService.Config;
            var labels = cfg.connections.ConvertAll(c => c.label);

            _dropdown.choices = labels;
            _dropdown.SetValueWithoutNotify(labels[cfg.activeIndex]);
            _removeBtn.SetEnabled(cfg.connections.Count > 1);

            var c = cfg.Active;
            _labelField.SetValueWithoutNotify(c.label);
            _apikeyField.SetValueWithoutNotify(c.apiKey);
            _sheetIdField.SetValueWithoutNotify(c.spreadsheetID);
            _sheetField.SetValueWithoutNotify(c.sheetName);
            _rangeStartField.SetValueWithoutNotify(c.rangeStart);
            _rangeEndField.SetValueWithoutNotify(c.rangeEnd);

            bool valid = c.IsValid;
            _warningLabel.style.display = valid
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            if (!valid) _warningLabel.text = "API Key and Spreadsheet ID are required.";

            SyncConnectionState();
        }

        private void SyncConnectionState()
        {
            bool connected = GSParserService.IsConnected;
            _loadBtn.SetEnabled(!connected);
            _reconnectBtn.SetEnabled(connected);
            _disconnectBtn.SetEnabled(connected);
        }

        private void OnConnected()
        {
            int rows = GSParserService.CachedData?.Rows.Count ?? 0;
            SetStatus($"Connected  -  {rows} rows");
            SyncConnectionState();
        }

        private void OnDisconnected()
        {
            SetStatus("Not connected");
            SyncConnectionState();
        }

        private void SetStatus(string msg) => _statusLabel.text = msg;

        // ── Cleanup ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            GSParserService.OnConnected -= OnConnected;
            GSParserService.OnDisconnected -= OnDisconnected;
            GSParserService.OnConfigChanged -= Refresh;
            GSParserService.OnLog -= SetStatus;
        }
    }
}