namespace Upgrade
{
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UI.Module;
    using UnityEngine;
    using UnityEngine.UI;

    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager I { get; private set; }
        const int UPGRADE_OPTION_COUNT = 3;    // アップグレードの選択肢の数
        [SerializeField] UpgradeModuleController uiCtrl;
        [SerializeField] UpgradeDatabase upgradeDatabase;
        readonly Dictionary<Type, Entry> activeEffects = new Dictionary<Type, Entry>();
        Entry[] currentEntries;

        public bool IsUpgraded { get; private set; } = false;    // アップグレードが適用されたかどうかを示すフラグ

        [Header("テスト")]
        [SerializeField] Button button;
        Core.Health coreHealth;

        public class Entry
        {
            public EffectBase effect;
            public UpgradeDefinition data;
            public int activeCount;
            public Action onClickAction;
        }

        public void ChangeUpgradeFlag(bool value) => IsUpgraded = value;

        public void Awake()
        {
            if(I == null) I = this;
            coreHealth = FindAnyObjectByType<Core.Health>();

            if (button != null) button.onClick.AddListener(ApplyCoreHealthUpgrade);
        }

        /// <summary>
        /// アップグレードUIを表示する処理をここに実装する
        /// activeEffectsの内容に基づいてUIを更新する
        /// </summary>
        public void OnShowUpgradeUI()
        {
            IsUpgraded = true;    // アップグレードUIが表示された時点でアップグレードが適用されたとみなす
            // まず表示するUpgrade内容を決める
            // UpgradeDatabaseからランダムで選出する
            UpgradeDefinition[] upgradeDefinitions = upgradeDatabase.GetRandomUpgradeDefinitions(UPGRADE_OPTION_COUNT);    // 3つのUpgradeをランダムに選出する
            List<Entry> entries = new List<Entry>();
            for (int i = 0; i < upgradeDefinitions.Length; i++)
            {
                Entry entry = new Entry();
                entry.data = upgradeDefinitions[i];
                entry.onClickAction = () => ApplyUpgrade(entry.data);   // クリックされたときにそのUpgradeを適用するアクションを設定する
                entries.Add(entry);
            }

            currentEntries = entries.ToArray();
            uiCtrl.ShowUpgradeSelection(currentEntries);   // UIを表示して、選出されたUpgradeの内容を渡す
        }

        public void ApplyUpgradeAt(int selectedIndex)
        {
            if (!IsUpgraded || currentEntries == null)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= currentEntries.Length)
            {
                Debug.LogWarning($"無効なアップグレード選択です: {selectedIndex}");
                return;
            }

            ApplyUpgrade(currentEntries[selectedIndex].data);
        }

        /// <summary>
        /// アップグレードを適用する処理をここに実装する
        /// dataの内容に基づいてアップグレードを適用する
        /// 例えば、CoreHealthUpのUpgradeDefinitionであれば、コアの体力を上げる処理を行う
        /// さらに、同じ型の効果がすでにアクティブな場合は、その効果のactiveCountを増やすなどの処理も行う
        /// そして、アップグレードが適用された後は、UIを更新して現在の効果の内容を反映させる
        /// </summary>
        void ApplyUpgrade(UpgradeDefinition data)
        {
            if (data == null) return;

            // dataの内容に基づいてアップグレードを適用する処理をここに実装する
            if (data is Data.CoreHealthUp) ApplyCoreHealthUpgrade();

            ChangeUpgradeFlag(false);   // アップグレードが適用された後、UpgradeManagerのフラグをfalseにする
            currentEntries = null;

            if (uiCtrl != null)
            {
                if (uiCtrl.IsSpawned && uiCtrl.IsServer)
                {
                    uiCtrl.HideUpgradeSelection();
                }
                else
                {
                    uiCtrl.m_SelectUpgradeElement.Hide();
                }
            }
        }

        public int GetUpgradeDefinitionIndex(UpgradeDefinition definition)
        {
            if (upgradeDatabase == null || definition == null)
            {
                return -1;
            }

            return upgradeDatabase.IndexOf(definition);
        }

        public SelectUpgradeElement.ViewData[] CreateViewData(int[] definitionIndices, int[] activeCounts)
        {
            if (upgradeDatabase == null || definitionIndices == null)
            {
                return Array.Empty<SelectUpgradeElement.ViewData>();
            }

            SelectUpgradeElement.ViewData[] viewData = new SelectUpgradeElement.ViewData[definitionIndices.Length];
            for (int i = 0; i < definitionIndices.Length; i++)
            {
                UpgradeDefinition definition = upgradeDatabase.GetUpgradeDefinitionAt(definitionIndices[i]);
                int activeCount = activeCounts != null && i < activeCounts.Length ? activeCounts[i] : 0;

                if (definition == null)
                {
                    Debug.LogWarning($"UpgradeDefinition が見つかりません: index={definitionIndices[i]}");
                    continue;
                }

                viewData[i] = new SelectUpgradeElement.ViewData(
                    definition.icon,
                    activeCount,
                    definition.upgradeName,
                    definition.GetDescription());
            }

            return viewData;
        }

        /// <summary>
        /// コアの体力を増加させるアップグレードを適用する処理をここに実装する
        /// </summary>
        void ApplyCoreHealthUpgrade()
        {
            Data.CoreHealthUp data = TryGetUpgradeData<Data.CoreHealthUp>();
            if (data == null) return;

            if (coreHealth == null)
            {
                Debug.LogWarning("Core.Health が見つかりません。");
                return;
            }

            AddActiveEffect(new CoreHealthUp(data));
            coreHealth.UpgradeHealth(TryGetUpgradeValue<CoreHealthUp>());
        }

        /// <summary>
        /// アクティブな効果を追加する処理をここに実装する
        /// </summary>
        public void AddActiveEffect(EffectBase effect)
        {
            if (effect == null) return;

            Type effectType = effect.GetType();

            // すでに同じ型の効果がアクティブな場合は、activeCountを増やして終了する
            if (activeEffects.TryGetValue(effectType, out Entry entry))
            {
                entry.activeCount++;
                return;
            }

            // そうでない場合は、新たにエントリーを作成して追加する
            activeEffects.Add(effectType, new Entry { effect = effect, activeCount = 1 });
        }

        /// <summary>
        /// 指定された型の効果の値を取得する処理をここに実装する
        /// </summary>
        /// <typeparam name="T"> 取得したい効果の型 </typeparam>
        /// <returns> 指定された型の効果の値 </returns>
        public float TryGetUpgradeValue<T>() where T : EffectBase
        {
            if (activeEffects.TryGetValue(typeof(T), out Entry entry)) 
                return entry.effect.GetUpgradeValue(entry.activeCount);     // 見つかった場合はその効果の値を返す
            return 0f;     // 見つからない場合は0を返す
        }

        /// <summary>
        /// 指定された型のUpgradeDefinitionを取得する処理をここに実装する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T TryGetUpgradeData<T>() where T : UpgradeDefinition
        {
            if (upgradeDatabase == null) return null;

            T data = upgradeDatabase.GetUpgradeDefinition<T>();     // UpgradeDatabaseから指定された型のUpgradeDefinitionを取得
            if(data != null) return data;     // 見つかった場合はそのデータを返す
            return null;
        }
    }
}