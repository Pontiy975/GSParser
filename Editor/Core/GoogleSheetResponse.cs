using System.Collections.Generic;

namespace GSParser.Editor.Core
{
    public class GoogleSheetResponse
    {
        public List<List<string>> values;

        public List<string> Headers => values != null && values.Count > 0 ? values[0] : new List<string>();

        public List<List<string>> Rows => values != null && values.Count > 1 ? values.GetRange(1, values.Count - 1) : new List<List<string>>();
    }
}