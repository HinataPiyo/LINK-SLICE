namespace UI.Module
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Upgrade;

    public class SelectUpgradeElement : MonoBehaviour, IUIModuleHandler
    {
        TemplateContainer[] uiElements;
        List<UpgradeElement> upgradeElements = new List<UpgradeElement>();

        VisualElement moduleRoot;
        public void Initialize(VisualElement moduleRoot)
        {
            this.moduleRoot = moduleRoot;

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
            moduleRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// UIを表示する
        /// </summary>
        public void Show(UpgradeManager.Entry[] entries)
        {
            moduleRoot.style.display = DisplayStyle.Flex;

            // entriesの内容に基づいてUIを更新する処理をここに実装する
            for (int i = 0; i < upgradeElements.Count; i++)
            {
                UpgradeElement upgradeElement = upgradeElements[i];
                if(i < entries.Length)
                {
                    UpgradeManager.Entry entry = entries[i];
                    // entryの内容をupgradeElementのUIに反映する処理をここに実装する
                    upgradeElement.UpdateUI(entry.data.icon, entry.activeCount, entry.data.upgradeName, entry.data.GetDescription(), entry.onClickAction);
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
                button.clicked += () => onClick?.Invoke();
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