using System.Collections.Generic;

namespace GSParser
{
    public interface IGoogleSheetSerializable
    {
        public void ParseData(Dictionary<string, string> data);
    }
}