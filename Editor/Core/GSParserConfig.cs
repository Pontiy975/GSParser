using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace GSParser.Editor.Core
{
    [Serializable]
    public class SheetConnection
    {
        public string label = "New Connection";
        public string apiKey = "";
        public string spreadsheetID = "";
        public string sheetName = "Sheet1";
        public string rangeStart = "A1";
        public string rangeEnd = "Z100";

        public string BuildURL()
        {
            var range = $"{sheetName}!{rangeStart}:{rangeEnd}";
            return $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetID}/values/{UnityWebRequest.EscapeURL(range)}?key={apiKey}";
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(spreadsheetID);
    }

    [Serializable]
    public class ParserPreset
    {
        public string label = "New Preset";
        public string targetTypeName = "";
        public string savePath = "Assets/Data";
    }

    [Serializable]
    public class GSParserConfig
    {
        public int activeIndex = 0;
        public List<SheetConnection> connections = new() { new SheetConnection() };

        public int activePresetIndex = 0;
        public List<ParserPreset> presets = new() { new ParserPreset() };

        public SheetConnection Active
        {
            get
            {
                if (connections == null || connections.Count == 0)
                    connections = new() { new SheetConnection() };
                activeIndex = Math.Clamp(activeIndex, 0, connections.Count - 1);
                return connections[activeIndex];
            }
        }

        public ParserPreset ActivePreset
        {
            get
            {
                if (presets == null || presets.Count == 0)
                    presets = new() { new ParserPreset() };
                activePresetIndex = Math.Clamp(activePresetIndex, 0, presets.Count - 1);
                return presets[activePresetIndex];
            }
        }
    }
}