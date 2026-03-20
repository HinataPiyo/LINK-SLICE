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
        bool hasSubmittedSelection;

        public bool IsInitialized => moduleRoot != null && upgradeElements.Count > 0;

        /// <summary>
        /// Upgradeの選択肢を表示するためのデータ構造。
        /// </summary>
        public readonly struct ViewData
        {
            public readonly Sprite Icon;
            public readonly int Level;
            public readonly string Name;
            public readonly string Description;

            /// <summary>
            /// ViewDataのコンストラクタ。
            /// </summary>
            public ViewData(Sprite icon, int level, string name, string description)
            {
                Icon = icon;
                Level = level;
                Name = name;
                Description = description;
            }
        }

        /// <summary>
        /// IUIModuleHandlerの実装。UpgradeModuleControllerから呼び出される。
        /// </summary>
        public void Initialize(VisualElement moduleRoot)
        {
            this.moduleRoot = moduleRoot;
            upgradeElements.Clear();

            // ここで、moduleRootを使用してUI要素の初期化を行う
            uiElements = moduleRoot.Q<VisualElement>("elements").Query<TemplateContainer>().ToList().ToArray();
            for (int i = 0; i < uiElements.Length; i++)
            {
                uiElements[i].name = $"element{i}";     // UI要素に一意の名前を設定する
                UpgradeElement upgradeElement = new UpgradeElement(uiElements[i]);      // UpgradeElementのインスタンスを作成してリストに追加する
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
            hasSubmittedSelection = false;
        }

        /// <summary>
        /// UIを表示する
        /// </summary>
        public void Show(ViewData[] entries, int totalPlayerCount, Action<int> onClick)
        {
            moduleRoot.style.display = DisplayStyle.Flex;
            hasSubmittedSelection = false;

            for (int i = 0; i < upgradeElements.Count; i++)
            {
                UpgradeElement upgradeElement = upgradeElements[i];
                if(i < entries.Length)
                {
                    ViewData entry = entries[i];
                    int selectedIndex = i;
                    upgradeElement.UpdateUI(entry.Icon, entry.Level, entry.Name, entry.Description, () =>
                    {
                        if (hasSubmittedSelection) return;

                        hasSubmittedSelection = true;
                        SetInteractable(false);
                        onClick?.Invoke(selectedIndex);
                    });
                    upgradeElement.UpdateSelectedPlayerCount(0, totalPlayerCount);
                }
                else
                {
                    upgradeElement.Hide();  // entriesの数より多いUI要素は非表示にする
                }
            }
        }

        public void UpdateSelectedPlayerCounts(int[] counts, int totalPlayerCount)
        {
            for (int i = 0; i < upgradeElements.Count; i++)
            {
                if (i >= counts.Length)
                {
                    upgradeElements[i].UpdateSelectedPlayerCount(0, totalPlayerCount);
                    continue;
                }

                upgradeElements[i].UpdateSelectedPlayerCount(counts[i], totalPlayerCount);
            }
        }

        public void ShowSelectedResult(int selectedIndex)
        {
            hasSubmittedSelection = true;
            SetInteractable(false);

            for (int i = 0; i < upgradeElements.Count; i++)
            {
                if (i == selectedIndex)
                {
                    upgradeElements[i].Show();
                    continue;
                }

                upgradeElements[i].Hide();
            }
        }

        void SetInteractable(bool isInteractable)
        {
            for (int i = 0; i < upgradeElements.Count; i++)
            {
                upgradeElements[i].SetInteractable(isInteractable);
            }
        }

        public class UpgradeElement
        {
            TemplateContainer element;
            VisualElement icon;
            Label name;
            Label level;
            Label description;
            Label selectedPlayerCount;

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
                selectedPlayerCount = element.Q<Label>("selected-count-value");
                button = element.Q<Button>();
                UpdateSelectedPlayerCount(0, 0);     // 初期状態では選択中プレイヤー数を0人にする
            }

            /// <summary>
            /// UpgradeElementのUIを更新するメソッド。
            /// </summary>
            public void UpdateUI(Sprite icon, int level, string name, string description, Action onClick)
            {
                Show();
                SetInteractable(true);
                this.icon.style.backgroundImage = new StyleBackground(icon);
                this.name.text = name;
                this.level.text = $"レベル{level}";
                this.description.text = description;

                if (clickHandler != null) button.clicked -= clickHandler;       // 既存のクリックハンドラーがある場合は削除する

                clickHandler = () => onClick?.Invoke();     // 新しいクリックハンドラーを設定する
                button.clicked += clickHandler;             // ボタンのクリックイベントにハンドラーを追加する
            }

            /// <summary>
            /// UpgradeElementの選択中プレイヤー数を更新するメソッド。
            /// </summary>
            /// <param name="count">選択中のプレイヤー数</param>
            public void UpdateSelectedPlayerCount(int count, int totalPlayerCount)
            {
                if (totalPlayerCount > 0)
                {
                    selectedPlayerCount.text = $"選択中: {count}/{totalPlayerCount}人";
                    return;
                }

                selectedPlayerCount.text = $"選択中: {count}人";
            }

            public void SetInteractable(bool isInteractable)
            {
                button?.SetEnabled(isInteractable);
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