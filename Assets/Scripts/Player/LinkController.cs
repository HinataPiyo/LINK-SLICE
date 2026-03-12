namespace Player.Link
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using Unity.Netcode;
    
    [DisallowMultipleComponent]
    public class LinkController : MonoBehaviour
    {
        static LinkController instance;

        [Header("生成設定")]
        [SerializeField] Link linkPrefab;
        [SerializeField] float targetDistance = 8f;     // ターゲットとの距離がこの値を超えたらリンクを切る
        [SerializeField] Transform spawnParent;     // Linkを生成する親オブジェクト（未設定ならこのオブジェクトの子として生成）

        readonly Dictionary<int, LinkRuntime> linkRuntimes = new Dictionary<int, LinkRuntime>();
        readonly List<Transform> players = new List<Transform>();
        readonly HashSet<int> activePlayerKeys = new HashSet<int>();

        float targetDistanceSqr;

        sealed class LinkRuntime
        {
            public Transform source;
            public Transform target;
            public Link spawnedLink;
            public Coroutine breakRoutine;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("LinkController はシーンに1つだけ配置してください", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            targetDistanceSqr = targetDistance * targetDistance;        // 0除算防止
        }

        void Update()
        {
            CollectPlayers(players);
            UpdateLinks();
        }

        /// <summary>
        /// プレイヤーを収集する。
        /// ネットワーク開始前はシーン内のMovementコンポーネントをプレイヤーとして収集し、ネットワーク開始後はNetworkManagerに接続されているクライアントのPlayerObjectをプレイヤーとして収集する。
        /// </summary>
        /// <param name="result"></param>
        void CollectPlayers(List<Transform> result)
        {
            result.Clear();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening)
            {
                foreach (NetworkClient client in networkManager.ConnectedClientsList)
                {
                    NetworkObject playerObject = client.PlayerObject;
                    // プレイヤーオブジェクトが存在しない、または非アクティブの場合はスキップする
                    if (playerObject == null || !playerObject.gameObject.activeInHierarchy) continue;

                    result.Add(playerObject.transform);
                }

                return;
            }

            // ネットワーク開始前のテスト用フォールバック
            Movement[] fallbackPlayers = FindObjectsByType<Movement>(FindObjectsSortMode.None);
            for (int i = 0; i < fallbackPlayers.Length; i++)
            {
                Movement movement = fallbackPlayers[i];
                if (movement == null || !movement.gameObject.activeInHierarchy) continue;

                result.Add(movement.transform);
            }
        }

        /// <summary>
        /// プレイヤーとターゲットの距離をチェックして、リンクを更新する
        /// </summary>
        void UpdateLinks()
        {
            activePlayerKeys.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                Transform source = players[i];
                if (source == null) continue;

                int sourceKey = source.GetInstanceID();
                activePlayerKeys.Add(sourceKey);

                if (!linkRuntimes.TryGetValue(sourceKey, out LinkRuntime runtime))
                {
                    runtime = new LinkRuntime { source = source };
                    linkRuntimes.Add(sourceKey, runtime);
                }
                else
                {
                    runtime.source = source;
                }

                runtime.target = FindNearestTarget(source, players);

                if (ShouldBreakLink(runtime))
                {
                    StartBreakIfNeeded(sourceKey, runtime);     // すぐに切るのではなく、破棄演出を走らせてから切る
                    continue;
                }

                ResumeOrCreateLink(runtime);
            }

            // すでにいないプレイヤーのリンクは破棄演出へ遷移させる
            List<int> keys = new List<int>(linkRuntimes.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int key = keys[i];
                if (activePlayerKeys.Contains(key)) continue;

                LinkRuntime runtime = linkRuntimes[key];
                runtime.source = null;
                runtime.target = null;

                if (runtime.spawnedLink == null)
                {
                    linkRuntimes.Remove(key);
                    continue;
                }

                StartBreakIfNeeded(key, runtime);
            }
        }

        /// <summary>
        /// sourceに対して、candidatesの中で最も近いTransformを返す。
        /// </summary>
        Transform FindNearestTarget(Transform source, List<Transform> candidates)
        {
            Transform nearest = null;
            float nearestSqr = float.MaxValue;

            Vector3 sourcePos = source.position;
            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];
                if (candidate == null || candidate == source) continue;

                float sqr = (candidate.position - sourcePos).sqrMagnitude;
                if (sqr >= nearestSqr) continue;

                nearest = candidate;
                nearestSqr = sqr;
            }

            return nearest;
        }

        /// <summary>
        /// runtimeのsourceとtargetの距離がtargetDistanceを超えているかどうかを返す。
        /// </summary>
        bool ShouldBreakLink(LinkRuntime runtime)
        {
            if (runtime.source == null || runtime.target == null) return true;

            Vector3 delta = runtime.target.position - runtime.source.position;
            return delta.sqrMagnitude > targetDistanceSqr;
        }

        /// <summary>
        /// runtimeのリンクを切る必要がある場合に、破棄演出を開始する。
        /// </summary>
        void StartBreakIfNeeded(int sourceKey, LinkRuntime runtime)
        {
            // 破棄演出は1本だけ走らせる
            if (runtime.spawnedLink == null || runtime.breakRoutine != null) return;

            runtime.breakRoutine = StartCoroutine(BreakAndDespawnCoroutine(sourceKey, runtime));
        }

        /// <summary>
        /// ターゲットがいる場合はリンクをつなげる（すでに接続されている場合は何もしない）。
        /// 破棄演出が走っている場合はキャンセルする。
        /// </summary>
        void ResumeOrCreateLink(LinkRuntime runtime)
        {
            // 破棄演出が走っている場合はキャンセルする
            if (runtime.breakRoutine != null)
            {
                StopCoroutine(runtime.breakRoutine);
                runtime.breakRoutine = null;
            }

            // ターゲットがいない場合は何もしない
            if (runtime.spawnedLink == null)
            {
                SpawnLink(runtime);
                return;
            }

            // すでに接続されている場合は何もしない
            runtime.spawnedLink.transform.position = runtime.source.position;
            runtime.spawnedLink.SetTarget(runtime.target);
        }

        /// <summary>
        /// インスペクターで値を変更したときに呼ばれる。
        /// targetDistanceが0以下にならないようにするための処理
        /// </summary>
        void OnValidate()
        {
            targetDistance = Mathf.Max(0f, targetDistance);
            targetDistanceSqr = targetDistance * targetDistance;
        }

        /// <summary>
        /// オブジェクトが破棄される直前に呼ばれる。
        /// 破棄演出のコルーチンが走っている場合は停止する
        /// </summary>
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            // 破棄演出のコルーチンが走っている場合は停止する
            foreach (LinkRuntime runtime in linkRuntimes.Values)
            {
                if (runtime.breakRoutine != null)
                {
                    StopCoroutine(runtime.breakRoutine);
                    runtime.breakRoutine = null;
                }

                if (runtime.spawnedLink != null)
                {
                    Destroy(runtime.spawnedLink.gameObject);
                    runtime.spawnedLink = null;
                }
            }

            linkRuntimes.Clear();
        }

        /// <summary>
        /// Linkプレハブを生成して、ターゲットを接続する
        /// </summary>
        void SpawnLink(LinkRuntime runtime)
        {
            if (linkPrefab == null)
            {
                Debug.LogWarning("Link prefabが未設定です", this);
                return;
            }

            if (runtime.spawnedLink != null || runtime.source == null || runtime.target == null) return;

            Transform parent = spawnParent != null ? spawnParent : transform;       // 親オブジェクトが指定されていない場合は管理オブジェクト配下に生成する
            runtime.spawnedLink = Instantiate(linkPrefab, runtime.source.position, Quaternion.identity, parent);
            runtime.spawnedLink.SetTarget(runtime.target);
        }

        /// <summary>
        /// 生成済みのLinkを破棄する
        /// </summary>
        void DespawnLink(LinkRuntime runtime)
        {
            if (runtime.spawnedLink == null) return;
            
            Destroy(runtime.spawnedLink.gameObject);
            runtime.spawnedLink = null;
        }

        /// <summary>
        /// リンク切断を開始してから、破棄演出が終わるまで待ってからLinkを破棄するコルーチン
        /// </summary>
        IEnumerator BreakAndDespawnCoroutine(int sourceKey, LinkRuntime runtime)
        {
            if (runtime.spawnedLink == null)
            {
                runtime.breakRoutine = null;
                yield break;
            }

            runtime.spawnedLink.BeginBreak();       // リンク切断を開始する（幅0到達後にLinkBrokenを通知）

            // 破棄演出が終わるまで待つ
            while (runtime.spawnedLink != null && !runtime.spawnedLink.IsBreakFinished)
            {
                if (runtime.source != null)
                {
                    runtime.spawnedLink.transform.position = runtime.source.position;
                }

                yield return null;
            }

            DespawnLink(runtime);       // Linkを破棄する
            runtime.breakRoutine = null;

            if (runtime.source == null)
            {
                linkRuntimes.Remove(sourceKey);
            }
        }
    }
}