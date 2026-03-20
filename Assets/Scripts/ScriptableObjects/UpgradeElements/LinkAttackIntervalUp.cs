namespace Upgrade.Data
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "LinkAttackIntervalUp", menuName = "UpgradeData/LinkAttackIntervalUp")]
    public class LinkAttackIntervalUp : UpgradeDefinition
    {
        [Header("リンク攻撃間隔を増加させる設定（%）")]
        [SerializeField, Range(0.1f, 1f)] float intervalPercentBonus = 0.1f;   // リンク攻撃間隔を増加させる割合（例: 0.2なら20%増加）

        public override string GetDescription(int offeredLevel)
        {
            return $"リンクの攻撃間隔が{intervalPercentBonus * 100f}%増加する。";
        }

        /// <summary>
        /// リンクの攻撃間隔を増加させる効果を適用する。
        /// </summary> 
        /// <param name="context">
        /// アップグレードの適用に必要な参照をまとめたコンテキスト。
        /// LinkAttackIntervalUp の場合は LinkRuntimeStats への参照を期待する。
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
                Debug.LogWarning($"{nameof(LinkAttackIntervalUp)} の適用先となる LinkRuntimeStats が見つかりません。");
                return false;
            }

            // 定義ごとに一意なキーで Modifier を上書きすることで、同じアップグレードを再取得した際も累積値を安全に再計算できる。
            // Attack は共有 RuntimeStats を直接読むため、既存 Link と新規 Link の両方へ同じ値が即時反映される。
            context.LinkRuntimeStats.SetIntervalPercentModifier(name, intervalPercentBonus * offeredLevel);
            return true;
        }
    }
}