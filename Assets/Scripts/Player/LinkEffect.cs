namespace PlayerSystem.Link
{
    using UnityEngine;
    
    public class LinkEffect : MonoBehaviour
    {
        [SerializeField] float fadeOutDuration = 0.2f;      // フェードアウトにかかる時間（秒）

        ParticleSystem ps;
        float baseRateOverTimeMultiplier = 1f;      // フェードアウト前の放出量を保存しておき、フェードアウト中はここから0に向かって減らしていく
        float fadeTimer;
        bool isFadingOut;

        public bool IsFadeOutFinished { get; private set; } = true;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();

            // エフェクトの放出量の初期値を保存しておく（Inspectorで設定した値）
            var emission = ps.emission;
            baseRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
        }

        void Update()
        {
            if (!isFadingOut) return;       // フェードアウト中でなければ何もしない

            // 放出量を徐々に下げ、演出の余韻を残したまま消す
            fadeTimer += Time.deltaTime;
            float duration = Mathf.Max(0.0001f, fadeOutDuration);       // 0除算防止
            float t = Mathf.Clamp01(fadeTimer / duration);              // フェードアウトの進行度（0から1）

            // 放出量を補間して更新する
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(baseRateOverTimeMultiplier, 0f, t);        // フェードアウト開始前の放出量から0に向かって線形補間する

            // フェードアウトの進行度が1に達したら完全に消えたとみなす
            if (t >= 1f)
            {
                isFadingOut = false;
                IsFadeOutFinished = true;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);     // 全ての粒子が消えるまで待ってから停止する
            }
        }

        /// <summary>
        /// エフェクトを再生する
        /// </summary>
        public void EnsurePlaying()
        {
            // フェードアウト中に再生が要求された場合は、フェードアウトをキャンセルして放出量を元に戻す
            if (isFadingOut) isFadingOut = false;

            IsFadeOutFinished = false;

            // 放出量を元に戻す
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = baseRateOverTimeMultiplier;

            if (!ps.isPlaying) ps.Play();
        }

        /// <summary>
        /// 再生状態に関係なく、エフェクトの位置・角度・サイズを更新する
        /// </summary>
        public void UpdateVisual(Vector2 center, float angle, float length)
        {
            // エフェクトの位置と回転を更新する
            transform.position = center;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // エフェクトのサイズを更新する（エフェクトの向きによってはscale.xをlengthにする必要がある）
            var shape = ps.shape;
            shape.scale = new Vector3(length, shape.scale.y, shape.scale.z);
        }

        /// <summary>
        /// 放出量を下げてフェードアウトを開始する
        /// </summary>
        public void BeginFadeOut()
        {
            // すでにフェードアウト中なら何もしない
            if (isFadingOut) return;

            isFadingOut = true;
            fadeTimer = 0f;
            IsFadeOutFinished = false;
        }
    }
}