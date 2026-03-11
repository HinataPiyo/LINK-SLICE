namespace Player.Link
{
    using UnityEngine;
    
    /// <summary>
    /// プレイヤーとプレイヤーをつなぐ線を描画するクラス
    /// </summary>
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lineRenderer;
        Transform target;
        [SerializeField] float maxLineWidth = 0.15f;
        [SerializeField] float growDuration = 0.2f;
        [SerializeField] float shrinkDuration = 0.12f;
        [SerializeField] float maxAnimationDeltaTime = 1f / 60f;

        LinkEffect linkEffect;
        bool isLinkActive;
        float currentLineWidth;
        float widthLerp01;
        bool isBreaking;
        bool notifiedBroken;

        const float WidthEpsilon = 0.0001f;

        public bool IsBreakFinished => isBreaking && notifiedBroken;

        /// <summary>
        /// 外部から接続先ターゲットを設定する
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (newTarget != null)
            {
                // 再接続時は切断状態を解除して表示復帰できるようにする
                isBreaking = false;
                notifiedBroken = false;
            }
        }

        /// <summary>
        /// リンク切断を開始する（幅0到達後にLinkBrokenを通知）
        /// </summary>
        public void BeginBreak()
        {
            if (isBreaking)
            {
                return;
            }

            isBreaking = true;
            if (linkEffect != null)
            {
                linkEffect.BeginFadeOut();
            }
        }

        void Awake()
        {
            linkEffect = GetComponentInChildren<LinkEffect>();
            currentLineWidth = 0f;
            widthLerp01 = 0f;

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = 0f;
                lineRenderer.endWidth = 0f;
                lineRenderer.enabled = false;
            }
        }

        void Update()
        {
            if (lineRenderer == null)
            {
                return;
            }

            // 破棄される直前まで、線の始点/終点は追従させ続ける
            if (target != null)
            {
                Vector3 selfPos = transform.position;
                Vector3 targetPos = target.position;
                lineRenderer.SetPosition(0, selfPos);
                lineRenderer.SetPosition(1, targetPos);

                if (linkEffect != null)
                {
                    // 線と同じ幾何情報でエフェクトの見た目も毎フレーム同期する
                    Vector2 center = (selfPos + targetPos) * 0.5f;
                    float angle = Mathf.Atan2(targetPos.y - selfPos.y, targetPos.x - selfPos.x) * Mathf.Rad2Deg;
                    float length = Vector2.Distance(selfPos, targetPos);
                    linkEffect.UpdateVisual(center, angle, length);
                }
            }

            // 接続先が存在し、かつ切断中でない間だけリンクを有効にする
            isLinkActive = target != null && !isBreaking;

            if (isLinkActive)
            {
                PlayLinkEffect();
            }

            // enabledの瞬間切り替えではなく、幅を補間して自然に表示/非表示する
            float safeDeltaTime = Mathf.Min(Time.deltaTime, maxAnimationDeltaTime);
            float duration = isLinkActive ? Mathf.Max(0.0001f, growDuration) : Mathf.Max(0.0001f, shrinkDuration);
            float step = safeDeltaTime / duration;
            float targetLerp = isLinkActive ? 1f : 0f;
            widthLerp01 = Mathf.MoveTowards(widthLerp01, targetLerp, step);
            currentLineWidth = maxLineWidth * widthLerp01;
            lineRenderer.startWidth = currentLineWidth;
            lineRenderer.endWidth = currentLineWidth;
            lineRenderer.enabled = currentLineWidth > WidthEpsilon;

            bool effectFinished = linkEffect == null || linkEffect.IsFadeOutFinished;
            if (isBreaking && !notifiedBroken && currentLineWidth <= WidthEpsilon && effectFinished)
            {
                notifiedBroken = true;
            }
        }

        /// <summary>
        /// 線のエフェクトを再生する
        /// </summary>
        public void PlayLinkEffect()
        {
            if (!isLinkActive) return;
            if (linkEffect == null) return;

            linkEffect.EnsurePlaying();
        }
    }
}