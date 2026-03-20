using PlayerSystem.Link;

namespace Upgrade
{
    /// <summary>
    /// 各アップグレードが必要とする実行対象をまとめて渡すためのコンテキスト。
    /// UpgradeDefinition が UpgradeManager の内部実装を知らずに適用処理だけへ集中できるようにする。
    /// </summary>
    public sealed class UpgradeContext
    {
        public Core.Health CoreHealth { get; }
        public LinkRuntimeStats LinkRuntimeStats { get; }

        public UpgradeContext(Core.Health coreHealth, LinkRuntimeStats linkRuntimeStats)
        {
            CoreHealth = coreHealth;
            LinkRuntimeStats = linkRuntimeStats;
        }
    }
}