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
                        Vector3? compositionAnchor = null;  // グループ生成の基準位置を保持する変数

                        for (int i = 0; i < c.spawnCount; i++)
                        {
                            bool isFirstInComposition = i == 0;

                            // 敵の生成パターンがグループだった場合
                            if (pattern == SpawnPositionPattern.Grouped)
                            {
                                GameObject spawnedEnemy = Spawn(c.enemyType, pattern, isFirstInComposition, compositionAnchor);
                                if (isFirstInComposition)
                                {
                                    compositionAnchor = spawnedEnemy.transform.position;
                                }

                                yield return null;
                            }
                            else if (pattern == SpawnPositionPattern.Random)     // 敵の生成パターンがランダムだった場合
                            {
                                Spawn(c.enemyType, pattern, isFirstInComposition, compositionAnchor);
                                yield return new WaitForSeconds(c.spawnInterval);     // 個々の敵の生成間隔を待機
                            }
                        }

                        // グループ生成の場合は、Composition全体の生成後に待機する
                        if(pattern == SpawnPositionPattern.Grouped)
                            yield return new WaitForSeconds(c.spawnInterval);     // Composition全体の生成後の時間を待機
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
        GameObject Spawn(EnemyType enemy, SpawnPositionPattern pattern, bool isFirstInComposition, Vector3? compositionAnchor)
        {
            Vector3 spawnPos;
            if (isFirstInComposition)        // 各Compositionの最初の敵はランダムな位置に生成
            {
                spawnPos = EnemySpawnConfig.GetSpawnPosition(SpawnPositionPattern.Random);
            }
            else if (pattern == SpawnPositionPattern.Grouped && compositionAnchor.HasValue)
            {
                spawnPos = EnemySpawnConfig.GetSpawnPosition(pattern, compositionAnchor.Value); // Composition先頭を基準にグループ生成
            }
            else
            {
                spawnPos = EnemySpawnConfig.GetSpawnPosition(pattern);       // 生成位置の取得
            }

            GameObject spawnedEnemy = Instantiate(enemyContainer.GetEnemyPrefab(enemy), spawnPos, Quaternion.identity);

            NetworkObject networkObject = spawnedEnemy.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !networkObject.IsSpawned)
            {
                networkObject.Spawn(true);
            }

            ActiveEnemies.Add(spawnedEnemy);
            return spawnedEnemy;
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