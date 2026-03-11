namespace Player.Link
{
    using System.Collections;
    using UnityEngine;
    
    public class LinkController : MonoBehaviour
    {
        [Header("生成設定")]
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform target;
        [SerializeField] float targetDistance = 5f;
        [SerializeField] Transform spawnParent;

        Link spawnedLink;
        Coroutine breakRoutine;
        float targetDistanceSqr;

        void Awake()
        {
            targetDistanceSqr = targetDistance * targetDistance;
        }

        void Update()
        {
            if (ShouldBreakLink())
            {
                StartBreakIfNeeded();
                return;
            }

            ResumeOrCreateLink();
        }

        bool ShouldBreakLink()
        {
            if (target == null)
            {
                return true;
            }

            Vector3 delta = target.position - transform.position;
            return delta.sqrMagnitude > targetDistanceSqr;
        }

        void StartBreakIfNeeded()
        {
            // 破棄演出は1本だけ走らせる
            if (spawnedLink == null || breakRoutine != null)
            {
                return;
            }

            breakRoutine = StartCoroutine(BreakAndDespawnCoroutine());
        }

        void ResumeOrCreateLink()
        {
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            if (spawnedLink == null)
            {
                SpawnLink();
                return;
            }

            spawnedLink.SetTarget(target);
        }

        void OnValidate()
        {
            targetDistance = Mathf.Max(0f, targetDistance);
            targetDistanceSqr = targetDistance * targetDistance;
        }

        void OnDestroy()
        {
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

            if (spawnedLink != null)
            {
                return;
            }

            Transform parent = spawnParent != null ? spawnParent : transform;
            spawnedLink = Instantiate(linkPrefab, transform.position, Quaternion.identity, parent);
            spawnedLink.SetTarget(target);
        }

        /// <summary>
        /// 生成済みのLinkを破棄する
        /// </summary>
        public void DespawnLink()
        {
            if (spawnedLink == null)
            {
                return;
            }
            
            Destroy(spawnedLink.gameObject);
            spawnedLink = null;
        }

        IEnumerator BreakAndDespawnCoroutine()
        {
            spawnedLink.BeginBreak();

            while (spawnedLink != null && !spawnedLink.IsBreakFinished)
            {
                yield return null;
            }

            DespawnLink();
            breakRoutine = null;
        }
    }
}