using System.Collections.Generic;
using GSParser.Editor.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSParser.Editor.Modules
{
    public class SheetViewerModule : GSParserModule
    {
        private VisualElement _tableContainer;
        private Label _infoLabel;
        private Button _refreshBtn;
        private MultiColumnListView _listView;

        private List<List<string>> _rows;
        private List<string> _headers;

        public SheetViewerModule(VisualElement root) : base(root) { }

        public override void Initialize()
        {
            var tab = Root.Q<VisualElement>("ViewerTab");
            if (tab == null)
            {
                UnityEngine.Debug.LogError("[GSParser] ViewerTab not found");
                return;
            }

            _tableContainer = tab.Q<VisualElement>("viewer-table-container");
            _infoLabel = tab.Q<Label>("viewer-info");
            _refreshBtn = tab.Q<Button>("viewer-refresh");

            _refreshBtn.clicked += () => _ = GSParserService.LoadSheetAsync();

            GSParserService.OnDataLoaded += SetData;
            GSParserService.OnDisconnected += Clear;

            // If data already loaded before window opened
            if (GSParserService.CachedData != null)
                SetData(GSParserService.CachedData);
        }

        // ── Data ──────────────────────────────────────────────────────────────

        public void SetData(GoogleSheetResponse response)
        {
            _headers = response.Headers;
            _rows = response.Rows;

            _infoLabel.text = $"{_rows.Count} rows  x  {_headers.Count} columns";

            RebuildTable();
        }

        private void Clear()
        {
            _headers = null;
            _rows = null;
            _infoLabel.text = "";
            _tableContainer.Clear();
            _listView = null;
        }

        // ── Table ─────────────────────────────────────────────────────────────

        private void RebuildTable()
        {
            _tableContainer.Clear();

            if (_headers == null || _headers.Count == 0) return;

            var columns = new Columns();
            for (int i = 0; i < _headers.Count; i++)
            {
                int captured = i;
                columns.Add(new Column
                {
                    name = $"col-{i}",
                    title = _headers[i],
                    width = 120,
                    resizable = true,
                    sortable = true,
                    makeCell = () => new Label { style = { paddingLeft = 4, paddingRight = 4 } },
                    bindCell = (el, rowIndex) =>
                    {
                        var label = (Label)el;
                        var row = _rows[rowIndex];
                        label.text = captured < row.Count ? row[captured] : "";
                    }
                });
            }

            _listView = new MultiColumnListView(columns)
            {
                itemsSource = _rows,
                fixedItemHeight = 22,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                sortingEnabled = true,
                style = { flexGrow = 1 }
            };

            _listView.columnSortingChanged += OnSortChanged;

            _tableContainer.Add(_listView);
        }

        private void OnSortChanged()
        {
            if (_listView == null || _rows == null) return;

            var descs = _listView.sortedColumns;
            var sorted = new List<List<string>>(_rows);

            foreach (var desc in descs)
            {
                int colIndex = _headers.IndexOf(desc.column.title);
                if (colIndex < 0) continue;

                sorted.Sort((a, b) =>
                {
                    string va = colIndex < a.Count ? a[colIndex] : "";
                    string vb = colIndex < b.Count ? b[colIndex] : "";

                    // Try numeric sort
                    if (float.TryParse(va, out float fa) && float.TryParse(vb, out float fb))
                        return desc.direction == SortDirection.Ascending
                            ? fa.CompareTo(fb)
                            : fb.CompareTo(fa);

                    return desc.direction == SortDirection.Ascending
                        ? string.Compare(va, vb, System.StringComparison.Ordinal)
                        : string.Compare(vb, va, System.StringComparison.Ordinal);
                });

                break; // single column sort
            }

            _listView.itemsSource = sorted;
            _listView.RefreshItems();
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            GSParserService.OnDataLoaded -= SetData;
            GSParserService.OnDisconnected -= Clear;
        }
    }
}