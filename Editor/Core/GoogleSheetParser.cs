using GSParser.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSParser.Editor
{
    public class GoogleSheetsParser : EditorWindow
    {
        private ParserController _controller;

        [MenuItem("Tools/GSParser")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<GoogleSheetsParser>();
            wnd.titleContent = new GUIContent("GSParser");
            wnd.minSize = new Vector2(400, 500);
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            var guids = AssetDatabase.FindAssets("GSParserWindow t:VisualTreeAsset");
            if (guids.Length == 0)
            {
                rootVisualElement.Add(new Label("[GSParser] GSParserWindow.uxml not found"));
                return;
            }

            VisualTreeAsset uxml = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (asset?.Instantiate().Q<VisualElement>("ParserTab") != null)
                {
                    uxml = asset;
                    break;
                }
            }

            if (uxml == null)
            {
                rootVisualElement.Add(new Label("[GSParser] GSParserWindow.uxml not found"));
                return;
            }

            var container = uxml.Instantiate();
            container.style.flexGrow = 1;
            rootVisualElement.Add(container);

            _controller?.Dispose();
            _controller = new ParserController(container);
            _controller.Initialize();
        }

        // Called after domain reload while window is open — rebuilds UI and resubscribes events
        private void OnEnable()
        {
            // CreateGUI is called automatically after OnEnable on domain reload
            // Nothing extra needed here
        }

        private void OnDestroy()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}