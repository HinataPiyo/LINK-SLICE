namespace Enemy.SpawnType
{
    /// <summary>
    /// SpawnTypeごとのパターン固有stateを格納するための共有コンテキスト。
    /// 必要に応じて各パターンで継承・拡張して利用する。
    /// </summary>
    public sealed class SpawnPatternContext
    {
        // 例: Grouped/Swarmで使うアンカーやリーダー参照など
        public UnityEngine.Vector3? GroupAnchor { get; set; }
        public UnityEngine.Transform PreviousSwarmMember { get; set; }
        // 必要に応じて追加
    }
}