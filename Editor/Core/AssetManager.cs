using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GSParser.Editor.Core
{
    public static class AssetManager
    {
        public static int Run(Type targetType, string savePath, GoogleSheetResponse data)
        {
            if (data == null || data.Rows.Count == 0)
                throw new InvalidOperationException("No sheet data.");

            if (!typeof(ScriptableObject).IsAssignableFrom(targetType))
                throw new InvalidOperationException($"{targetType.Name} is not a ScriptableObject.");

            var parseFields = ReflectionUtils.GetParseFields(targetType);
            var defaultFields = ReflectionUtils.GetDefaultFields(targetType);
            var primaryKey = ReflectionUtils.GetPrimaryKeyField(targetType);
            var assetNameColumn = ReflectionUtils.GetAssetNameColumn(targetType);
            var isSerializable = typeof(IGoogleSheetSerializable).IsAssignableFrom(targetType);

            if (parseFields.Length == 0 && !isSerializable)
                throw new InvalidOperationException(
                    $"{targetType.Name} has no [Parse] fields and does not implement IGoogleSheetSerializable.");

            if (!AssetDatabase.IsValidFolder(savePath))
                CreateFolderRecursive(savePath);

            var existing = LoadExisting(targetType, savePath);
            var headers = data.Headers;
            var processed = 0;

            foreach (var row in data.Rows)
            {
                var rowMap = BuildMap(headers, row);

                // Find or create asset
                ScriptableObject asset = null;
                bool isNew = false;

                if (primaryKey != null)
                {
                    var keyAttr = primaryKey.GetCustomAttribute<ParseAttribute>();
                    if (keyAttr != null &&
                        rowMap.TryGetValue(keyAttr.Column, out var keyValue) &&
                        !string.IsNullOrWhiteSpace(keyValue))
                    {
                        asset = FindByKey(existing, primaryKey, keyValue);
                        if (asset == null)
                        {
                            asset = ScriptableObject.CreateInstance(targetType);
                            isNew = true;
                        }
                    }
                    else continue;
                }
                else
                {
                    asset = ScriptableObject.CreateInstance(targetType);
                    isNew = true;
                }

                // [ParseDefault] — apply only on new assets
                if (isNew)
                    ApplyDefaults(asset, defaultFields);

                // [Parse] fields
                foreach (var field in parseFields)
                {
                    var attr = field.GetCustomAttribute<ParseAttribute>();

                    if (!rowMap.TryGetValue(attr.Column, out var raw) || string.IsNullOrWhiteSpace(raw))
                        continue;

                    try
                    {
                        field.SetValue(asset, ReflectionUtils.ConvertPrimitive(raw, field.FieldType));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GSParser] {targetType.Name}.{field.Name} <- \"{raw}\": {e.Message}");
                    }
                }

                // IGoogleSheetSerializable.ParseData for complex fields
                if (isSerializable)
                    ((IGoogleSheetSerializable)asset).ParseData(rowMap);

                if (isNew)
                {
                    var fileName = ResolveFileName(assetNameColumn, primaryKey, asset, rowMap);

                    var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                        Path.Combine(savePath, $"{fileName}.asset"));
                    AssetDatabase.CreateAsset(asset, assetPath);
                    existing.Add(asset);
                }

                EditorUtility.SetDirty(asset);
                processed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return processed;
        }

        // ── [AssetName] / [PrimaryKey] naming ────────────────────────────────

        private static string ResolveFileName(
            string assetNameColumn,
            FieldInfo primaryKey,
            ScriptableObject asset,
            Dictionary<string, string> rowMap)
        {
            if (!string.IsNullOrEmpty(assetNameColumn) &&
                rowMap.TryGetValue(assetNameColumn, out var raw) &&
                !string.IsNullOrWhiteSpace(raw))
            {
                return Sanitize(raw);
            }

            if (primaryKey != null)
            {
                var val = primaryKey.GetValue(asset)?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return Sanitize(val);
            }

            return "asset";
        }

        // ── [ParseDefault] ────────────────────────────────────────────────────

        private static void ApplyDefaults(ScriptableObject asset, (FieldInfo field, DefaultRefAttribute attr)[] defaults)
        {
            foreach (var (field, attr) in defaults)
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    Debug.LogWarning($"[GSParser] [ParseDefault] on \"{field.Name}\" ignored: field type is not a UnityEngine.Object");
                    continue;
                }

                var found = FindAssetByName(field.FieldType, attr.AssetName);
                if (found == null)
                {
                    Debug.LogWarning($"[GSParser] [ParseDefault] asset \"{attr.AssetName}\" of type {field.FieldType.Name} not found");
                    continue;
                }

                try { field.SetValue(asset, found); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GSParser] [ParseDefault] failed to set \"{field.Name}\": {e.Message}");
                }
            }
        }

        private static UnityEngine.Object FindAssetByName(Type type, string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:{type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != name) continue;
                var obj = AssetDatabase.LoadAssetAtPath(path, type);
                if (obj != null) return obj;
            }
            return null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<ScriptableObject> LoadExisting(Type type, string folder)
        {
            return AssetDatabase.FindAssets($"t:{type.Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath(p, type) as ScriptableObject)
                .Where(a => a != null)
                .ToList();
        }

        private static ScriptableObject FindByKey(List<ScriptableObject> assets, FieldInfo keyField, string key)
        {
            return assets.FirstOrDefault(a => keyField.GetValue(a)?.ToString() == key);
        }

        private static Dictionary<string, string> BuildMap(List<string> headers, List<string> row)
        {
            var map = new Dictionary<string, string>();
            for (var i = 0; i < headers.Count; i++)
                map[headers[i]] = i < row.Count ? row[i] : "";
            return map;
        }

        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
        }

        private static void CreateFolderRecursive(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}