namespace Enemy
{
    using System.Collections;
    using System.Collections.Generic;
    using Enemy.SpawnType;
    using Unity.Netcode;
    using UnityEngine;
    using Upgrade;

    public class EnemySpawnController : MonoBehaviour
    {
        public static EnemySpawnController I { get; private set; }     // シングルトンインスタンス
        [SerializeField] EnemyContainer enemyContainer;     // 敵のプレハブを管理するコンテナ
        [SerializeField] EnemySpawnConfig[] spawnConfigs;
        [SerializeField] float timeAfterWave = 3f;
        public List<GameObject> ActiveEnemies { get; private set; } = new List<GameObject>();     // 現在アクティブな敵のリスト
        const float SwarmSpacing = 0.6f;
        Dictionary<SpawnPositionPattern, IEnemySpawnPattern> spawnPatterns;

        void Awake()
        {
            if (I == null) I = this;

            spawnPatterns = new Dictionary<SpawnPositionPattern, IEnemySpawnPattern>
            {
                { SpawnPositionPattern.Random, new RandomSpawnPattern() },
                { SpawnPositionPattern.Grouped, new GroupedSpawnPattern() },
                { SpawnPositionPattern.Swarm, new SwarmSpawnPattern() },
            };
        }

        void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// 敵の生成ルーチン
        /// </summary>
        IEnumerator SpawnRoutine()
        {
            foreach (EnemySpawnConfig config in spawnConfigs)
            {
                foreach (EnemyWaveEntry entries in config.Entries)
                {
                    foreach (EnemyWaveEntry.Composition c in entries.compositions)
                    {
                        if (!spawnPatterns.TryGetValue(c.spawnPattern, out IEnemySpawnPattern spawnPattern))
                        {
                            Debug.LogError($"Spawn pattern {c.spawnPattern} is not registered.");
                            continue;
                        }

                        // パターンごとのstateを格納するContextを生成
                        var context = new SpawnPatternContext();
                        yield return spawnPattern.SpawnComposition(this, c, context);
                    }

                    yield return new WaitUntil(() => ActiveEnemies.Count == 0);     // 生成された敵が全て倒されるまで待機

                    yield return new WaitForSeconds(timeAfterWave);         // 次のウェーブまでの時間を待機
                }

                UpgradeManager.I.OnShowUpgradeUI();     // ウェーブとウェーブの間にアップグレードUIを表示する
                yield return new WaitWhile(() => UpgradeManager.I.IsUpgraded);     // プレイヤーがアップグレードを選択するまで待機
            }
        }

        /// <summary>
        /// 敵の生成処理
        /// </summary>
        public GameObject SpawnEnemy(EnemyType enemy, Vector3 spawnPos)
        {
            GameObject spawnedEnemy = Instantiate(enemyContainer.GetEnemyPrefab(enemy), spawnPos, Quaternion.identity);

            NetworkObject networkObject = spawnedEnemy.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !networkObject.IsSpawned)
            {
                networkObject.Spawn(true);
            }

            ActiveEnemies.Add(spawnedEnemy);
            return spawnedEnemy;
        }

        public Vector3 GetRandomSpawnPosition()
        {
            return EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Random);
        }

        public Vector3 GetGroupedSpawnPosition(Vector3 compositionAnchor)
        {
            return EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Grouped, compositionAnchor);
        }

        public Vector3 GetSwarmSpawnPosition(Transform swarmFollowTarget)
        {
            if (swarmFollowTarget == null)
            {
                return GetRandomSpawnPosition();
            }

            Vector2 followPosition = swarmFollowTarget.position;
            Vector2 awayFromCore = followPosition.sqrMagnitude > 0.0001f ? followPosition.normalized : Vector2.up;
            return followPosition + awayFromCore * SwarmSpacing;
        }

        public void ConfigureSwarmMovement(GameObject spawnedEnemy, Transform previousSwarmMember, bool isFirstInComposition)
        {
            Movement movement = spawnedEnemy.GetComponent<Movement>();
            if (movement == null)
            {
                return;
            }

            if (isFirstInComposition)
            {
                movement.SetupSwarmLeader();
                return;
            }

            movement.SetupSwarmFollower(previousSwarmMember, SwarmSpacing);
        }

        /// <summary>
        /// 敵が倒されたときに呼び出されるメソッド
        /// </summary>
        /// <param name="enemy">倒された敵のゲームオブジェクト</param>
        public void RemoveEnemy(GameObject enemy)
        {
            ActiveEnemies.Remove(enemy);     // 敵をリストから削除
            Debug.Log("Enemy removed. Remaining enemies: " + ActiveEnemies.Count);
        }
    }
}