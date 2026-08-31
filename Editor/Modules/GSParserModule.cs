using UnityEngine.UIElements;

namespace GSParser.Editor
{
    /// <summary>
    /// Базовий клас для всіх вкладок плагіна.
    /// Кожен модуль знає тільки свій шматок UI — не тримає даних,
    /// не референсить інші модулі. Комунікація через GSParserService events.
    /// </summary>
    public abstract class GSParserModule
    {
        protected readonly VisualElement Root;

        protected GSParserModule(VisualElement root)
        {
            Root = root;
        }

        /// <summary>Викликається один раз при створенні вікна. Тут — Q(), RegisterCallback().</summary>
        public abstract void Initialize();

        /// <summary>Викликається коли вкладка стає видимою.</summary>
        public virtual void OnShow() { }

        /// <summary>Викликається коли вкладка ховається.</summary>
        public virtual void OnHide() { }

        /// <summary>Shortcut для Root.Q — шукає елемент по імені всередині свого root.</summary>
        protected T Q<T>(string name) where T : VisualElement => Root.Q<T>(name);
    }
}