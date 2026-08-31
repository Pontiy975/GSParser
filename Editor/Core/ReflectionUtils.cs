using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GSParser.Editor.Core
{
    public static class ReflectionUtils
    {
        private const BindingFlags FieldFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(FieldFlags))
                {
                    if (seen.Add(f.Name))
                        yield return f;
                }
            }
        }

        public static Type[] GetParsableTypes()
        {
            return TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(t => !t.IsGenericTypeDefinition)
                .Where(t =>
                    typeof(IGoogleSheetSerializable).IsAssignableFrom(t) ||
                    GetAllFields(t).Any(f => f.GetCustomAttribute<ParseAttribute>() != null))
                .OrderBy(t => t.Name)
                .ToArray();
        }

        public static Type ResolveType(string assemblyQualifiedName)
        {
            return string.IsNullOrWhiteSpace(assemblyQualifiedName)
                ? null
                : Type.GetType(assemblyQualifiedName);
        }
        
        public static string GetAssetNameColumn(Type type)
        {
            return type.GetCustomAttribute<AssetNameAttribute>()?.Column;
        }

        public static FieldInfo[] GetParseFields(Type type)
        {
            return GetAllFields(type)
                .Where(f => f.GetCustomAttribute<ParseAttribute>() != null)
                .ToArray();
        }

        public static FieldInfo GetPrimaryKeyField(Type type)
        {
            return GetAllFields(type)
                .FirstOrDefault(f => f.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        }

        public static (FieldInfo field, DefaultRefAttribute attr)[] GetDefaultFields(Type type)
        {
            return GetAllFields(type)
                .Select(f => (field: f, attr: f.GetCustomAttribute<DefaultRefAttribute>()))
                .Where(x => x.attr != null)
                .ToArray();
        }

        public static FieldInfo FindField(Type type, string name)
        {
            return GetAllFields(type).FirstOrDefault(f => f.Name == name);
        }

        public static object ConvertPrimitive(string raw, Type fieldType)
        {
            if (fieldType == typeof(string))
                return raw ?? "";

            if (string.IsNullOrWhiteSpace(raw))
                return fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;

            var ci = CultureInfo.InvariantCulture;

            if (fieldType.IsEnum) return Enum.Parse(fieldType, raw, ignoreCase: true);
            if (fieldType == typeof(int)) return int.Parse(raw.Replace(",", "."), ci);
            if (fieldType == typeof(long)) return long.Parse(raw, ci);
            if (fieldType == typeof(float)) return float.Parse(raw.Replace(",", "."), ci);
            if (fieldType == typeof(double)) return double.Parse(raw.Replace(",", "."), ci);
            if (fieldType == typeof(bool)) return raw.Trim().ToLowerInvariant() is "1" or "true" or "yes";

            if (fieldType == typeof(Vector2)) return ParseVector2(raw);
            if (fieldType == typeof(Vector3)) return ParseVector3(raw);
            if (fieldType == typeof(Vector4)) return ParseVector4(raw);
            if (fieldType == typeof(Vector2Int)) return ParseVector2Int(raw);
            if (fieldType == typeof(Vector3Int)) return ParseVector3Int(raw);
            if (fieldType == typeof(Color)) return ParseColor(raw);
            if (fieldType == typeof(Color32)) return (Color32)ParseColor(raw);

            throw new NotSupportedException($"Unsupported type: {fieldType.Name}");
        }

        private static float[] Split(string raw, int n)
        {
            var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != n) throw new FormatException($"Expected {n} components in \"{raw}\"");
            return parts.Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
        }

        private static Vector2 ParseVector2(string r) { var c = Split(r, 2); return new Vector2(c[0], c[1]); }
        private static Vector3 ParseVector3(string r) { var c = Split(r, 3); return new Vector3(c[0], c[1], c[2]); }
        private static Vector4 ParseVector4(string r) { var c = Split(r, 4); return new Vector4(c[0], c[1], c[2], c[3]); }
        private static Vector2Int ParseVector2Int(string r) { var c = Split(r, 2); return new Vector2Int((int)c[0], (int)c[1]); }
        private static Vector3Int ParseVector3Int(string r) { var c = Split(r, 3); return new Vector3Int((int)c[0], (int)c[1], (int)c[2]); }

        private static Color ParseColor(string raw)
        {
            raw = raw.Trim();
            if (raw.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(raw, out var c)) return c;
                throw new FormatException($"Invalid hex color: \"{raw}\"");
            }
            var p = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length != 3 && p.Length != 4) throw new FormatException($"Expected 3-4 color components in \"{raw}\"");
            float r2 = float.Parse(p[0].Trim(), CultureInfo.InvariantCulture);
            float g = float.Parse(p[1].Trim(), CultureInfo.InvariantCulture);
            float b = float.Parse(p[2].Trim(), CultureInfo.InvariantCulture);
            float a = p.Length == 4 ? float.Parse(p[3].Trim(), CultureInfo.InvariantCulture) : 1f;
            return new Color(r2, g, b, a);
        }
    }
}