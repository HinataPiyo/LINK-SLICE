namespace Enemy
{
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;

    public class EnemySpawnController : MonoBehaviour
    {
        public static EnemySpawnController I { get; private set; }     // シングルトンインスタンス
        [SerializeField] EnemySpawnConfig[] spawnConfigs;
        public List<GameObject> ActiveEnemies { get; private set; } = new List<GameObject>();     // 現在アクティブな敵のリスト

        void Awake()
        {
            if(I == null) I = this;
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
                foreach (EnemyWaveEntry entry in config.Entries)
                {
                    for (int i = 0; i < entry.spawnCount; i++)
                    {
                        Spawn(entry, ActiveEnemies);       // 敵の生成
                        yield return new WaitForSeconds(entry.spawnInterval);
                    }

                    yield return new WaitUntil(() => ActiveEnemies.Count == 0);    // 生成された敵が全て倒されるまで待機
                }
            }
        }

        /// <summary>
        /// 敵の生成処理
        /// </summary>
        void Spawn(EnemyWaveEntry entry, List<GameObject> ActiveEnemies)
        {
            Vector3 spawnPos;
            if(ActiveEnemies.Count == 0)
            {
                // 最初の敵はランダムな位置に生成
                spawnPos = EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Random);
            }
            else
            {
                if(entry.spawnPattern == SpawnPositionPattern.Grouped)
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(entry.spawnPattern, ActiveEnemies[0].transform.position); // グループ化された位置の取得
                }
                else
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(entry.spawnPattern);       // 生成位置の取得
                }
            }
            
            GameObject enemy = Instantiate(entry.enemyPrefab, spawnPos, Quaternion.identity);

            NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            ActiveEnemies.Add(enemy);
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