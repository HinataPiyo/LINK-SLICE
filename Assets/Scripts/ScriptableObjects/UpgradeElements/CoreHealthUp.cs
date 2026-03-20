namespace Upgrade.Data
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "CoreHealthUp", menuName = "UpgradeData/CoreHealthUp")]
    public class CoreHealthUp : UpgradeDefinition
    {
        [Header("コアの体力を増加させる設定（%）")]
        [Range(0.1f, 2f)] public float healthIncreasePercent = 1f;   // コアの体力を増加させる割合（例: 0.2なら20%増加）

        public override string GetDescription(int offeredLevel)
        {
            return $"コアの最大体力が{healthIncreasePercent * 100f}%増加する。";
        }

        /// <summary>
        /// コアの体力を増加させる効果を適用する。
        /// </summary>
        /// <param name="context">
        /// アップグレードの適用に必要な参照をまとめたコンテキスト。
        /// CoreHealthUp の場合は Core.Health への参照を期待する。
        /// </param>
        /// <param name="offeredLevel">今回選ぶと到達するレベル。これをもとに増加量を計算する。</param>
        /// <returns>
        /// true を返すと、UpgradeManager がこのアップグレードを適用済みとして扱う。
        /// false を返すと、適用に失敗したと見なされる。
        /// </returns>
        public override bool Apply(UpgradeContext context, int offeredLevel)
        {
            if (context == null || context.CoreHealth == null)
            {
                Debug.LogWarning($"{nameof(CoreHealthUp)} の適用先となる Core.Health が見つかりません。");
                return false;
            }

            // 定義ごとに一意なキーで Modifier を上書きすることで、同じアップグレードを再取得した際も累積値を安全に再計算できる。
            context.CoreHealth.SetMaxHealthPercentModifier(name, healthIncreasePercent * offeredLevel);
            return true;
        }
    }
}