namespace PlayerSystem.Link
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    
    [DisallowMultipleComponent]
    public class LinkController : MonoBehaviour
    {
        static LinkController instance;

        [Header("生成設定")]
        [SerializeField] PlayerConfig playerConfig;
        [SerializeField] Transform spawnParent;     // Linkを生成する親オブジェクト（未設定ならこのオブジェクトの子として生成）

        readonly Dictionary<long, LinkRuntime> linkRuntimes = new Dictionary<long, LinkRuntime>();
        readonly List<Transform> players = new List<Transform>();
        readonly HashSet<long> activePairKeys = new HashSet<long>();

        float targetDistanceSqr;
        float targetDistance;

        sealed class LinkRuntime
        {
            public Transform source;
            public Transform target;
            public Link spawnedLink;
            public Coroutine breakRoutine;
            public bool pendingRemoval;
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
            targetDistance = playerConfig.Link.distance;
            targetDistanceSqr = targetDistance * targetDistance;        // 0除算防止
        }

        void Update()
        {
            CollectPlayers(players);
            UpdateLinks();
        }

        /// <summary>
        /// シーン内のMovementコンポーネントをプレイヤーとして収集する。
        /// </summary>
        /// <param name="result"></param>
        void CollectPlayers(List<Transform> result)
        {
            result.Clear();
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
            activePairKeys.Clear();

            for (int i = 0; i < players.Count; i++)
            {
                Transform source = players[i];
                if (source == null) continue;

                for (int j = i + 1; j < players.Count; j++)
                {
                    Transform target = players[j];
                    if (target == null) continue;
                    if (!IsWithinLinkRange(source, target)) continue;

                    long pairKey = CreatePairKey(source, target);
                    activePairKeys.Add(pairKey);

                    if (!linkRuntimes.TryGetValue(pairKey, out LinkRuntime runtime))
                    {
                        runtime = new LinkRuntime();
                        linkRuntimes.Add(pairKey, runtime);
                    }

                    runtime.source = source;
                    runtime.target = target;
                    runtime.pendingRemoval = false;
                    ResumeOrCreateLink(runtime);
                }
            }

            List<long> pairKeys = new List<long>(linkRuntimes.Keys);
            for (int i = 0; i < pairKeys.Count; i++)
            {
                long pairKey = pairKeys[i];
                if (activePairKeys.Contains(pairKey)) continue;

                LinkRuntime runtime = linkRuntimes[pairKey];
                UpdateBreakingLinkTransform(runtime);
                runtime.pendingRemoval = true;
                StartBreakIfNeeded(pairKey, runtime);
            }
        }

        /// <summary>
        /// 2つのTransformがリンク可能な距離にあるかどうか
        /// </summary>
        /// <param name="source"> リンクの発信元</param>
        /// <param name="target"> リンクの接続先</param>
        /// <returns> リンク可能な距離にある場合はtrue、そうでない場合はfalse</returns>
        bool IsWithinLinkRange(Transform source, Transform target)
        {
            Vector3 delta = target.position - source.position;
            return delta.sqrMagnitude <= targetDistanceSqr;
        }

        /// <summary>
        /// 2つのTransformからペアキーを作成する。
        /// ペアキーは2つのTransformの組み合わせを一意に識別するための値で、順序に依存しない。
        /// </summary>
        /// <param name="first"> リンクの一方</param>
        /// <param name="second"> リンクのもう一方</param>
        /// <returns> ペアキー</returns>
        long CreatePairKey(Transform first, Transform second)
        {
            int firstId = first.GetInstanceID();
            int secondId = second.GetInstanceID();
            int minId = Mathf.Min(firstId, secondId);
            int maxId = Mathf.Max(firstId, secondId);

            return ((long)(uint)minId << 32) | (uint)maxId;
        }

        /// <summary>
        /// リンクを切る必要がある場合に、破棄演出を開始する。
        /// </summary>
        void StartBreakIfNeeded(long pairKey, LinkRuntime runtime)
        {
            // 破棄演出は1本だけ走らせる
            if (runtime.spawnedLink == null || runtime.breakRoutine != null) return;

            runtime.breakRoutine = StartCoroutine(BreakAndDespawnCoroutine(pairKey, runtime));
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

            if (runtime.spawnedLink == null)
            {
                SpawnLink(runtime);
            }

            UpdateLinkTransform(runtime);
        }

        void UpdateLinkTransform(LinkRuntime runtime)
        {
            if (runtime.spawnedLink == null || runtime.source == null || runtime.target == null) return;

            runtime.spawnedLink.transform.position = runtime.source.position;
            runtime.spawnedLink.SetTarget(runtime.target);
        }

        void UpdateBreakingLinkTransform(LinkRuntime runtime)
        {
            if (runtime.spawnedLink == null || runtime.source == null) return;

            runtime.spawnedLink.transform.position = runtime.source.position;
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
            if (playerConfig.Link.linkPrefab == null)
            {
                Debug.LogWarning("Link prefabが未設定です", this);
                return;
            }

            if (runtime.spawnedLink != null || runtime.source == null || runtime.target == null) return;

            Transform parent = spawnParent != null ? spawnParent : transform;       // 親オブジェクトが指定されていない場合は管理オブジェクト配下に生成する
            runtime.spawnedLink = Instantiate(playerConfig.Link.linkPrefab, runtime.source.position, Quaternion.identity, parent);
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
        IEnumerator BreakAndDespawnCoroutine(long pairKey, LinkRuntime runtime)
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
                yield return null;
            }

            DespawnLink(runtime);       // Linkを破棄する
            runtime.breakRoutine = null;

            if (runtime.pendingRemoval)
            {
                runtime.source = null;
                runtime.target = null;
                linkRuntimes.Remove(pairKey);
            }
        }
    }
}