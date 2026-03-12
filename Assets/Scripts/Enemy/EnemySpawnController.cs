namespace Enemy
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class EnemySpawnController : MonoBehaviour
    {
        [SerializeField] EnemySpawnConfig[] spawnConfigs;

        void Start()
        {
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
                    List<GameObject> spawnedEnemies = new List<GameObject>(); // 生成された敵のリスト
                    for (int i = 0; i < entry.spawnCount; i++)
                    {
                        Spawn(entry, spawnedEnemies);       // 敵の生成
                        yield return new WaitForSeconds(entry.spawnInterval);
                    }
                }
            }
        }

        /// <summary>
        /// 敵の生成処理
        /// </summary>
        void Spawn(EnemyWaveEntry entry, List<GameObject> spawnedEnemies)
        {
            Vector3 spawnPos;
            if(spawnedEnemies.Count == 0)
            {
                // 最初の敵はランダムな位置に生成
                spawnPos = EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Random);
            }
            else
            {
                if(entry.spawnPattern == SpawnPositionPattern.Grouped)
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(entry.spawnPattern, spawnedEnemies[0].transform.position); // グループ化された位置の取得
                }
                else
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(entry.spawnPattern);       // 生成位置の取得
                }
            }
            
            GameObject enemy = Instantiate(entry.enemyPrefab, spawnPos, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }
    }
}