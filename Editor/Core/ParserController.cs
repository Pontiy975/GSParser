using GSParser.Editor.Core;
using GSParser.Editor.Modules;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GSParser.Editor
{
    public class ParserController
    {
        private readonly VisualElement _root;

        private ConnectionModule _connectionModule;
        private SheetViewerModule _viewerModule;
        private ParserModule _parserModule;

        private readonly Dictionary<string, string> _tabMap = new()
        {
            { "btn-tab-connection", "ConnectionTab" },
            { "btn-tab-viewer",     "ViewerTab"     },
            { "btn-tab-parser",     "ParserTab"     },
            { "btn-tab-log",        "LogTab"        },
        };

        private string _activeTab = "btn-tab-connection";

        public ParserController(VisualElement root)
        {
            _root = root;
        }

        public void Initialize()
        {
            _connectionModule = new ConnectionModule(_root);
            _connectionModule.Initialize();

            _viewerModule = new SheetViewerModule(_root);
            _viewerModule.Initialize();

            _parserModule = new ParserModule(_root);
            _parserModule.Initialize();

            GSParserService.OnDataLoaded += _ =>
            {
                // Auto-switch to viewer when data loads
                SwitchTab("btn-tab-viewer");
            };

            foreach (var (btnName, _) in _tabMap)
            {
                var btn = _root.Q<Button>(btnName);
                if (btn == null) continue;
                var captured = btnName;
                btn.clicked += () => SwitchTab(captured);
            }

            SwitchTab(_activeTab);
        }

        private void SwitchTab(string btnName)
        {
            _activeTab = btnName;

            foreach (var (btn, panel) in _tabMap)
            {
                _root.Q<Button>(btn)?.EnableInClassList("tab-btn--active", btn == btnName);

                var panelEl = _root.Q<VisualElement>(panel);
                if (panelEl != null)
                    panelEl.style.display = btn == btnName ? DisplayStyle.Flex : DisplayStyle.None;
            }

            switch (btnName)
            {
                case "btn-tab-connection": _connectionModule?.OnShow(); break;
                case "btn-tab-viewer": _viewerModule?.OnShow(); break;
                case "btn-tab-parser": _parserModule?.OnShow(); break;
            }
        }

        public void Dispose()
        {
            _connectionModule?.Dispose();
            _viewerModule?.Dispose();
            _parserModule?.Dispose();
        }
    }
}