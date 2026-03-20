namespace Upgrade
{
    using UnityEngine;
    

    /// <summary>
    /// 個々のアップグレード要素のデータクラス。
    /// アップグレードの定義はこのクラスを継承して実装する。
    /// </summary>
    public abstract class UpgradeDefinition : ScriptableObject
    {
        public string upgradeName;     // アップグレードの名前
        public Sprite icon;           // アップグレードのアイコン

        [Min(1)]
        public int maxLevel = 10;     // アップグレードの最大レベル。これを超えるとさらに提示されなくなる。

        /// <summary>
        /// 現在レベルから次に提示してよいかを判定する。
        /// 選出条件を各定義側へ寄せることで、UpgradeManager に型分岐を増やさずに済む。
        /// </summary>
        /// <param name="context">アップグレードの適用に必要な参照をまとめたコンテキスト</param>
        /// <param name="currentLevel">現在のレベル</param>
        public virtual bool CanOffer(UpgradeContext context, int currentLevel)
        {
            // デフォルトの選出条件は「現在レベルが最大レベル未満」であること。
            // 必要に応じて個別の定義でオーバーライドして複雑な条件を実装できる。
            return currentLevel < maxLevel;
        }

        /// <summary>
        /// UI に表示する説明文を返す。
        /// offeredLevel は「今回選ぶと到達するレベル」を表す。
        /// </summary>
        public abstract string GetDescription(int offeredLevel);

        /// <summary>
        /// 実際の効果適用を各アップグレード定義側に持たせる。
        /// これにより、UpgradeManager は状態管理と選出だけに責務を限定できる。
        /// </summary>
        public abstract bool Apply(UpgradeContext context, int offeredLevel);
    }
}