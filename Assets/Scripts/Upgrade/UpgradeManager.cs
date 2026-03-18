namespace Upgrade
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager I { get; private set; }

        [SerializeField] UpgradeDatabase upgradeDatabase;
        readonly Dictionary<Type, Entry> activeEffects = new Dictionary<Type, Entry>();

        [Header("テスト")]
        [SerializeField] Button button;
        Core.Health coreHealth;

        public class Entry
        {
            public EffectBase effect;
            public int activeCount;
        }

        public void Awake()
        {
            if(I == null) I = this;
            coreHealth = FindAnyObjectByType<Core.Health>();

            if (button != null) button.onClick.AddListener(ApplyCoreHealthUpgrade);
        }

        /// <summary>
        /// アップグレードを適用する処理をここに実装する
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
            if (activeEffects.TryGetValue(effectType, out Entry entry))
            {
                entry.activeCount++;
                return;
            }

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