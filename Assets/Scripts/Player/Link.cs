namespace Player.Link
{
    using UnityEngine;

    /// <summary>
    /// プレイヤーとプレイヤーをつなぐ線を描画するクラス
    /// </summary>
    public class Link : MonoBehaviour
    {
        [SerializeField] float maxLineWidth = 0.15f;
        [SerializeField] float growDuration = 0.2f;
        [SerializeField] float shrinkDuration = 0.12f;
        [SerializeField] float maxAnimationDeltaTime = 1f / 60f;

        LineRenderer lineRenderer;
        Transform target;
        LinkEffect linkEffect;
        float currentLineWidth;
        float widthLerp01;
        bool isLinkActive;
        bool isBreaking;
        bool notifiedBroken;

        const float WidthEpsilon = 0.0001f;

        public bool IsBreakFinished => isBreaking && notifiedBroken;

        /// <summary>
        /// 外部から接続先ターゲットを設定する
        /// </summary>
        public void SetTarget(Transform newTarget) => target = newTarget;

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
            linkEffect.BeginFadeOut();      // 切断開始と同時にエフェクトのフェードアウトも始める
        }

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            linkEffect = GetComponentInChildren<LinkEffect>();

            currentLineWidth = 0f;
            widthLerp01 = 0f;

            lineRenderer.startWidth = 0f;
            lineRenderer.endWidth = 0f;
            lineRenderer.enabled = false;
        }

        void Update()
        {
            if (lineRenderer == null) return;

            // 破棄される直前まで、線の始点/終点は追従させ続ける
            if (target != null)
            {
                // 線の両端を自分とターゲットの位置にする
                Vector3 selfPos = transform.position;
                Vector3 targetPos = target.position;
                lineRenderer.SetPosition(0, selfPos);
                lineRenderer.SetPosition(1, targetPos);

                // 線と同じ幾何情報でエフェクトの見た目も毎フレーム同期する
                Vector2 center = (selfPos + targetPos) * 0.5f;
                float angle = Mathf.Atan2(targetPos.y - selfPos.y, targetPos.x - selfPos.x) * Mathf.Rad2Deg;
                float length = Vector2.Distance(selfPos, targetPos);
                linkEffect.UpdateVisual(center, angle, length);
            }

            // 接続先が存在し、かつ切断中でない間だけリンクを有効にする
            isLinkActive = target != null && !isBreaking;

            if (isLinkActive)
            {
                PlayLinkEffect();
            }

            // enabledの瞬間切り替えではなく、幅を補間して自然に表示/非表示する
            float safeDeltaTime = Mathf.Min(Time.deltaTime, maxAnimationDeltaTime);     // フレーム落ちなどで極端に大きなdeltaTimeが入るのを防止
            float duration = isLinkActive ? Mathf.Max(0.0001f, growDuration) : Mathf.Max(0.0001f, shrinkDuration);  // 0除算防止
            float step = safeDeltaTime / duration;          // 1秒間にstepが1以上になるように補正する（フレーム落ちしても数秒で完全に表示/非表示になるように）
            float targetLerp = isLinkActive ? 1f : 0f;      // 目標の幅に向かってwidthLerp01を補間する
            widthLerp01 = Mathf.MoveTowards(widthLerp01, targetLerp, step);     // 補間値から現在の線の幅を計算する
            currentLineWidth = maxLineWidth * widthLerp01;      // 線の幅を更新し、幅が十分に大きいときだけ線を表示する

            // 線の幅を更新し、幅が十分に大きいときだけ線を表示する
            lineRenderer.startWidth = currentLineWidth;
            lineRenderer.endWidth = currentLineWidth;
            lineRenderer.enabled = currentLineWidth > WidthEpsilon;

            // 切断開始後、線の幅が十分に小さくなり、かつエフェクトのフェードアウトが完了したタイミングで切断完了とみなす
            if (isBreaking && !notifiedBroken && currentLineWidth <= WidthEpsilon && linkEffect.IsFadeOutFinished)
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

            linkEffect.EnsurePlaying();
        }
    }
}