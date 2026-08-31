using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GSParser.Editor.Core
{
    [InitializeOnLoad]
    public static class GSParserService
    {
        private static readonly string ConfigPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "Editor", "Config", "GoogleSheetConfig.cfg"));

        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<GoogleSheetResponse> OnDataLoaded;
        public static event Action<int> OnParseCompleted;
        public static event Action<string> OnLog;
        public static event Action OnConfigChanged;

        public static GSParserConfig Config { get; private set; } = new();
        public static GoogleSheetResponse CachedData { get; private set; }
        public static bool IsConnected { get; private set; }

        static GSParserService()
        {
            LoadConfig();
            EditorApplication.delayCall += AutoReconnect;
        }

        private static void AutoReconnect()
        {
            EditorApplication.delayCall -= AutoReconnect;
            if (Config.Active.IsValid)
                _ = LoadSheetAsync();
        }

        // ── Config ────────────────────────────────────────────────────────────

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                Config = new GSParserConfig();
                SaveConfig();
                return;
            }
            try
            {
                Config = JsonConvert.DeserializeObject<GSParserConfig>(
                    File.ReadAllText(ConfigPath)) ?? new GSParserConfig();
            }
            catch (Exception e)
            {
                Config = new GSParserConfig();
                Log($"Config parse error: {e.Message}");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            }
            catch (Exception e) { Log($"Config save error: {e.Message}"); }
        }

        // ── Connections ───────────────────────────────────────────────────────

        public static void SetActiveConnection(int index)
        {
            if (index < 0 || index >= Config.connections.Count) return;
            Config.activeIndex = index;
            SaveConfig();
            Disconnect();
            OnConfigChanged?.Invoke();
        }

        public static void AddConnection(string label = "New Connection")
        {
            Config.connections.Add(new SheetConnection { label = label });
            Config.activeIndex = Config.connections.Count - 1;
            SaveConfig();
            OnConfigChanged?.Invoke();
        }

        public static void RemoveConnection(int index)
        {
            if (Config.connections.Count <= 1) return;
            Config.connections.RemoveAt(index);
            Config.activeIndex = Math.Clamp(Config.activeIndex, 0, Config.connections.Count - 1);
            SaveConfig();
            OnConfigChanged?.Invoke();
        }

        public static void UpdateActiveConnection(SheetConnection updated)
        {
            Config.connections[Config.activeIndex] = updated;
            SaveConfig();
        }

        // ── Presets ───────────────────────────────────────────────────────────

        public static void SetActivePreset(int index)
        {
            if (index < 0 || index >= Config.presets.Count) return;
            Config.activePresetIndex = index;
            SaveConfig();
            OnConfigChanged?.Invoke();
        }

        public static void AddPreset(string label = "New Preset")
        {
            Config.presets.Add(new ParserPreset { label = label });
            Config.activePresetIndex = Config.presets.Count - 1;
            SaveConfig();
            OnConfigChanged?.Invoke();
        }

        public static void RemovePreset(int index)
        {
            if (Config.presets.Count <= 1) return;
            Config.presets.RemoveAt(index);
            Config.activePresetIndex = Math.Clamp(Config.activePresetIndex, 0, Config.presets.Count - 1);
            SaveConfig();
            OnConfigChanged?.Invoke();
        }

        // ── Sheet loading ─────────────────────────────────────────────────────

        public static async Task LoadSheetAsync()
        {
            var conn = Config.Active;
            if (!conn.IsValid) { Log("Cannot load: credentials missing"); return; }

            Log($"Loading \"{conn.label}\"...");
            try
            {
                var response = await SheetFetcher.FetchAsync(conn);
                CachedData = response;
                IsConnected = true;
                Log($"Loaded: {response.Rows.Count} rows, {response.Headers.Count} columns");
                OnConnected?.Invoke();
                OnDataLoaded?.Invoke(CachedData);
            }
            catch (Exception e)
            {
                IsConnected = false;
                Log($"Load failed: {e.Message}");
            }
        }

        public static void Disconnect()
        {
            CachedData = null;
            IsConnected = false;
            OnDisconnected?.Invoke();
            Log("Disconnected");
        }

        // ── Parse ─────────────────────────────────────────────────────────────

        public static void Parse(Type targetType, string savePath)
        {
            if (CachedData == null) { Log("Cannot parse: no data loaded"); return; }
            try
            {
                var count = AssetManager.Run(targetType, savePath, CachedData);
                Log($"Parse complete: {count} assets processed");
                OnParseCompleted?.Invoke(count);
            }
            catch (Exception e) { Log($"Parse failed: {e.Message}"); }
        }

        // ── Log ───────────────────────────────────────────────────────────────

        private static void Log(string msg)
        {
            Debug.Log($"[GSParser] {msg}");
            OnLog?.Invoke(msg);
        }
    }
}