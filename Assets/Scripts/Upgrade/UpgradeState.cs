namespace Upgrade
{
    /// <summary>
    /// 1つのアップグレード定義に対する進行状況を表す実行時データ。
    /// Definition と Level を分けて持つことで、表示情報は ScriptableObject、状態はランタイム側に責務を分離する。
    /// </summary>
    public sealed class UpgradeState
    {
        public UpgradeDefinition Definition { get; }

        /// <summary>
        /// 現在の取得レベル。
        /// 0 は未取得、1 以上が取得済みレベルを表す。
        /// </summary>
        public int Level { get; private set; }

        public UpgradeState(UpgradeDefinition definition, int initialLevel = 0)
        {
            Definition = definition;
            Level = initialLevel;
        }

        /// <summary>
        /// アップグレード選択確定時にレベルを1つ進める。
        /// </summary>
        public void LevelUp()
        {
            Level++;
        }
    }
}