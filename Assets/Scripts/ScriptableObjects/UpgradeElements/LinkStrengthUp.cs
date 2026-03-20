namespace Upgrade.Data
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "LinkStrengthUp", menuName = "UpgradeData/LinkStrengthUp", order = 0)]
    public class LinkStrengthUp : UpgradeDefinition
    {
        [Header("リンク強度を増加させる設定（%）")]
        [Range(0.1f, 2f)] public float strengthIncreasePercent = 1f;   // リンク強度を増加させる割合（例: 0.2なら20%増加）

        public override string GetDescription(int offeredLevel)
        {
            return $"リンクの強度が{strengthIncreasePercent * 100f}%増加する。";
        }

        /// <summary>
        /// リンクの強度を増加させる効果を適用する。
        /// </summary>
        /// <param name="context">
        /// アップグレードの適用に必要な参照をまとめたコンテキスト。
        /// LinkStrengthUp の場合は LinkRuntimeStats への参照を期待する。
        /// </param>
        /// <param name="offeredLevel">今回選ぶと到達するレベル。これをもとに増加量を計算する。</param>
        /// <returns>
        /// true を返すと、UpgradeManager がこのアップグレードを適用済みとして扱う。
        /// false を返すと、適用に失敗したと見なされる。
        /// </returns>
        public override bool Apply(UpgradeContext context, int offeredLevel)
        {
            if (context == null || context.LinkRuntimeStats == null)
            {
                Debug.LogWarning($"{nameof(LinkStrengthUp)} の適用先となる LinkRuntimeStats が見つかりません。");
                return false;
            }

            // 定義ごとに一意なキーで Modifier を上書きすることで、同じアップグレードを再取得した際も累積値を安全に再計算できる。
            // Attack は共有 RuntimeStats を直接読むため、既存 Link と新規 Link の両方へ同じ値が即時反映される。
            context.LinkRuntimeStats.SetStrengthPercentModifier(name, strengthIncreasePercent * offeredLevel);
            return true;
        }
    }
}