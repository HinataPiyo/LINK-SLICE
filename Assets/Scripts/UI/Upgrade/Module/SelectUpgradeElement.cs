namespace UI.Module
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SelectUpgradeElement : MonoBehaviour, IUIModuleHandler
    {
        TemplateContainer[] uiElements;
        List<UpgradeElement> upgradeElements = new List<UpgradeElement>();

        VisualElement moduleRoot;

        public readonly struct ViewData
        {
            public readonly Sprite Icon;
            public readonly int Level;
            public readonly string Name;
            public readonly string Description;

            public ViewData(Sprite icon, int level, string name, string description)
            {
                Icon = icon;
                Level = level;
                Name = name;
                Description = description;
            }
        }

        public void Initialize(VisualElement moduleRoot)
        {
            this.moduleRoot = moduleRoot;
            upgradeElements.Clear();

            // ここで、moduleRootを使用してUI要素の初期化を行う
            uiElements = moduleRoot.Q<VisualElement>("elements").Query<TemplateContainer>().ToList().ToArray();
            for (int i = 0; i < uiElements.Length; i++)
            {
                uiElements[i].name = $"element{i}";
                UpgradeElement upgradeElement = new UpgradeElement(uiElements[i]);
                upgradeElements.Add(upgradeElement);
            }

            Hide();  // 初期状態ではUIを非表示にする
        }

        /// <summary>
        /// UIを非表示にする
        /// </summary>
        public void Hide()
        {
            if (moduleRoot == null)
            {
                return;
            }

            moduleRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// UIを表示する
        /// </summary>
        public void Show(ViewData[] entries, Action<int> onClick)
        {
            if (moduleRoot == null)
            {
                return;
            }

            moduleRoot.style.display = DisplayStyle.Flex;

            for (int i = 0; i < upgradeElements.Count; i++)
            {
                UpgradeElement upgradeElement = upgradeElements[i];
                if(i < entries.Length)
                {
                    ViewData entry = entries[i];
                    int selectedIndex = i;
                    upgradeElement.UpdateUI(entry.Icon, entry.Level, entry.Name, entry.Description, () => onClick?.Invoke(selectedIndex));
                }
                else
                {
                    upgradeElement.Hide();  // entriesの数より多いUI要素は非表示にする
                }
            }
        }

        public class UpgradeElement
        {
            TemplateContainer element;
            VisualElement icon;
            Label name;
            Label level;
            Label description;

            Button button;
            Action clickHandler;

            /// <summary>
            /// UpgradeElementクラスのコンストラクタ。
            /// TemplateContainerからUI要素を取得して初期化する。
            /// </summary>
            public UpgradeElement(TemplateContainer element)
            {
                this.element = element;
                icon = element.Q<VisualElement>("icon-value");
                name = element.Q<Label>("name-value");
                level = element.Q<Label>("level-value");
                description = element.Q<Label>("description-value");
                button = element.Q<Button>();
            }

            /// <summary>
            /// UpgradeElementのUIを更新するメソッド。
            /// </summary>
            public void UpdateUI(Sprite icon, int level, string name, string description, System.Action onClick)
            {
                Show();
                this.icon.style.backgroundImage = new StyleBackground(icon);
                this.name.text = name;
                this.level.text = $"レベル{level}";
                this.description.text = description;

                if (clickHandler != null)
                {
                    button.clicked -= clickHandler;
                }

                clickHandler = () => onClick?.Invoke();
                button.clicked += clickHandler;
            }

            /// <summary>
            /// UpgradeElementのUIを非表示にするメソッド。
            /// </summary>
            public void Hide()
            {
                element.style.display = DisplayStyle.None;
            }

            /// <summary>
            /// UpgradeElementのUIを表示するメソッド。
            /// </summary>
            public void Show()
            {
                element.style.display = DisplayStyle.Flex;
            }
        }
    }
}