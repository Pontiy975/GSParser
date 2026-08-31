using System;

namespace GSParser.Editor.Core
{
    /// <summary>
    /// Maps a field to a Google Sheet column.
    /// [Parse("column_name")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ParseAttribute : Attribute
    {
        public string Column { get; }
        public ParseAttribute(string column) => Column = column;
    }

    /// <summary>
    /// Marks the field as primary key for asset lookup/creation.
    /// Must also have [Parse] to specify which column to read from.
    /// [Parse("id"), PrimaryKey]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PrimaryKeyAttribute : Attribute { }

    /// <summary>
    /// Sets a default asset reference when a new SO is created.
    /// Finds a ScriptableObject in the project by file name and assigns it.
    /// [ParseDefault("DefaultBoardingMap")]
    /// public BoardingMap boardingMap;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DefaultRefAttribute : Attribute
    {
        public string AssetName { get; }
        public DefaultRefAttribute(string assetName) => AssetName = assetName;
    }

    /// <summary>
    /// Specifies which sheet column becomes the file name of a newly created asset.
    /// Read directly from the row, independent of any [Parse]-bound field.
    /// Takes priority over [PrimaryKey] for naming; falls back to it if the
    /// column is missing or empty for a given row.
    /// [AssetName("id")]
    /// public class AIUnitModel : ...
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetNameAttribute : Attribute
    {
        public string Column { get; }
        public AssetNameAttribute(string column) => Column = column;
    }
}