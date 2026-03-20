namespace Enemy
{
    using System.Collections;
    using System.Collections.Generic;
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

        void Awake()
        {
            if (I == null) I = this;
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
                        SpawnPositionPattern pattern = c.spawnPattern;

                        for (int i = 0; i < c.spawnCount; i++)
                        {
                            // 敵の生成パターンがグループだった場合
                            if (pattern == SpawnPositionPattern.Grouped)
                            {
                                Spawn(c.enemyType, pattern);
                                yield return null;
                            }
                            else if (pattern == SpawnPositionPattern.Random)     // 敵の生成パターンがランダムだった場合
                            {
                                Spawn(c.enemyType, pattern);
                                yield return new WaitForSeconds(c.spawnInterval);     // 個々の敵の生成間隔を待機
                            }
                        }
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
        void Spawn(EnemyType enemy, SpawnPositionPattern pattern)
        {
            Vector3 spawnPos;
            if (ActiveEnemies.Count == 0)        // 最初の敵を生成するときはランダムな位置に生成
            {
                //! 最初の敵はランダムな位置に生成(ランダム固定)
                spawnPos = EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Random);
            }
            else        // 2体目以降
            {
                // グループ化された位置を取得
                if (pattern == SpawnPositionPattern.Grouped)
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(pattern, ActiveEnemies[0].transform.position); // グループ化された位置の取得
                }
                else        // ランダムな位置を取得
                {
                    spawnPos = EnemySpawnConfig.GetSpawnPosition(pattern);       // 生成位置の取得
                }
            }

            GameObject spawnedEnemy = Instantiate(enemyContainer.GetEnemyPrefab(enemy), spawnPos, Quaternion.identity);

            NetworkObject networkObject = spawnedEnemy.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !networkObject.IsSpawned)
            {
                networkObject.Spawn(true);
            }

            ActiveEnemies.Add(spawnedEnemy);
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