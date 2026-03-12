namespace Player.Link
{
    using System.Collections;
    using UnityEngine;
    
    public class LinkController : MonoBehaviour
    {
        [Header("生成設定")]
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform target;          // 接続先ターゲット
        [SerializeField] float targetDistance = 8f;     // ターゲットとの距離がこの値を超えたらリンクを切る
        [SerializeField] Transform spawnParent;     // Linkを生成する親オブジェクト（未設定ならこのオブジェクトの子として生成）

        Link spawnedLink;
        Coroutine breakRoutine;
        float targetDistanceSqr;

        void Awake()
        {
            targetDistanceSqr = targetDistance * targetDistance;        // 0除算防止
        }

        void Update()
        {
            // ターゲットがいなくなった、またはターゲットとの距離が遠すぎる場合はリンクを切る
            if (ShouldBreakLink())
            {
                StartBreakIfNeeded();       // すぐに切るのではなく、破棄演出を走らせてから切る
                return;
            }

            // ターゲットがいる場合はリンクをつなげる（すでに接続されている場合は何もしない）
            ResumeOrCreateLink();
        }

        /// <summary>
        /// ターゲットがいなくなった、またはターゲットとの距離が遠すぎるか
        /// </summary>
        /// <returns> ターゲットがいなくなった、またはターゲットとの距離が遠すぎる場合はtrue、それ以外はfalse </returns>
        bool ShouldBreakLink()
        {
            if (target == null) return true;

            // ターゲットとの距離が遠すぎるか
            Vector3 delta = target.position - transform.position;
            return delta.sqrMagnitude > targetDistanceSqr;
        }

        /// <summary>
        /// リンク切断を開始する（距離が遠すぎる場合など）。
        /// すぐに切るのではなく、破棄演出を走らせてから切る
        /// </summary>
        void StartBreakIfNeeded()
        {
            // 破棄演出は1本だけ走らせる
            if (spawnedLink == null || breakRoutine != null) return;

            breakRoutine = StartCoroutine(BreakAndDespawnCoroutine());      // 破棄演出を走らせてから切る
        }

        /// <summary>
        /// ターゲットがいる場合はリンクをつなげる（すでに接続されている場合は何もしない）。
        /// 破棄演出が走っている場合はキャンセルする。
        /// </summary>
        void ResumeOrCreateLink()
        {
            // 破棄演出が走っている場合はキャンセルする
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            // ターゲットがいない場合は何もしない
            if (spawnedLink == null)
            {
                SpawnLink();
                return;
            }

            // すでに接続されている場合は何もしない
            spawnedLink.SetTarget(target);
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
            // 破棄演出のコルーチンが走っている場合は停止する
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }
        }

        /// <summary>
        /// Linkプレハブを生成して、ターゲットを接続する
        /// </summary>
        public void SpawnLink()
        {
            if (linkPrefab == null)
            {
                Debug.LogWarning("Link prefabが未設定です", this);
                return;
            }

            if (spawnedLink != null) return;

            Transform parent = spawnParent != null ? spawnParent : transform;       // 親オブジェクトが指定されていない場合は自分の子として生成する
            spawnedLink = Instantiate(linkPrefab, transform.position, Quaternion.identity, parent);     // 生成したLinkにターゲットを設定する
            spawnedLink.SetTarget(target);
        }

        /// <summary>
        /// 生成済みのLinkを破棄する
        /// </summary>
        public void DespawnLink()
        {
            if (spawnedLink == null) return;
            
            Destroy(spawnedLink.gameObject);
            spawnedLink = null;
        }

        /// <summary>
        /// リンク切断を開始してから、破棄演出が終わるまで待ってからLinkを破棄するコルーチン
        /// </summary>
        IEnumerator BreakAndDespawnCoroutine()
        {
            spawnedLink.BeginBreak();       // リンク切断を開始する（幅0到達後にLinkBrokenを通知）

            // 破棄演出が終わるまで待つ
            while (spawnedLink != null && !spawnedLink.IsBreakFinished)
            {
                yield return null;
            }

            DespawnLink();          // Linkを破棄する
            breakRoutine = null;    // コルーチン終了後にnullを代入して、次の破棄演出が走るようにする
        }
    }
}