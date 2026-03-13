namespace Enemy
{
    using System.Collections.Generic;
    using PlayerSystem;
    using UnityEngine;
    public enum SpawnPositionPattern { Random, Grouped }

    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Config/EnemySpawnConfig")]
    public class EnemySpawnConfig : ScriptableObject
    {
        [Header("1ウェーブあたりの敵の生成設定")]
        [SerializeField] List<EnemyWaveEntry> entries = new List<EnemyWaveEntry>();    // 敵の生成設定リスト
        public IReadOnlyList<EnemyWaveEntry> Entries => entries;    // 外部からの読み取り専用プロパティ

        public static Vector2 GetSpawnPosition(SpawnPositionPattern pattern, Vector2 centerPos = default)
        {
            switch (pattern)
            {
                case SpawnPositionPattern.Random:
                    Vector2 min = FollowCamera.LimitMoveAreaMin;
                    Vector2 max = FollowCamera.LimitMoveAreaMax;

                    // カメラ移動範囲の外周リングからランダムに選ぶ
                    const float outsideMin = 2f;   // 範囲境界から最低これだけ外側
                    const float outsideMax = 4f;   // 範囲境界から最大これだけ外側

                    int side = Random.Range(0, 4); // 0:左 1:右 2:下 3:上
                    switch (side)
                    {
                        case 0:
                            return new Vector2(
                                Random.Range(min.x - outsideMax, min.x - outsideMin),
                                Random.Range(min.y - outsideMax, max.y + outsideMax));
                        case 1:
                            return new Vector2(
                                Random.Range(max.x + outsideMin, max.x + outsideMax),
                                Random.Range(min.y - outsideMax, max.y + outsideMax));
                        case 2:
                            return new Vector2(
                                Random.Range(min.x - outsideMax, max.x + outsideMax),
                                Random.Range(min.y - outsideMax, min.y - outsideMin));
                        default:
                            return new Vector2(
                                Random.Range(min.x - outsideMax, max.x + outsideMax),
                                Random.Range(max.y + outsideMin, max.y + outsideMax));
                    }
                case SpawnPositionPattern.Grouped:
                    Vector2 group = new Vector2(Random.Range(centerPos.x - 2f, centerPos.x + 2f), Random.Range(centerPos.y - 2f, centerPos.y + 2f));    // グループ化された位置
                    return group; // グループ化された位置（例: 中央）
                default:
                    return Vector2.zero;
            }
        }
    }

    [System.Serializable]
    public class EnemyWaveEntry
    {
        public GameObject enemyPrefab;
        public SpawnPositionPattern spawnPattern;    // 生成位置のパターン
        [Range(1, 10)] public int spawnCount;                              // 生成数
        [Tooltip("個々の敵の生成間隔"), Range(0.1f, 3f)] public float spawnInterval;      // 生成間隔
    }
}