namespace Player.Link
{
    using UnityEngine;
    
    public class LinkEffect : MonoBehaviour
    {
        [SerializeField] float fadeOutDuration = 0.2f;

        ParticleSystem ps;
        float baseRateOverTimeMultiplier = 1f;
        float fadeTimer;
        bool isFadingOut;

        public bool IsFadeOutFinished { get; private set; } = true;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();

            if (ps != null)
            {
                var emission = ps.emission;
                baseRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
            }
        }

        void Update()
        {
            if (ps == null || !isFadingOut)
            {
                return;
            }

            // 放出量を徐々に下げ、演出の余韻を残したまま消す
            fadeTimer += Time.deltaTime;
            float duration = Mathf.Max(0.0001f, fadeOutDuration);
            float t = Mathf.Clamp01(fadeTimer / duration);

            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(baseRateOverTimeMultiplier, 0f, t);

            if (t >= 1f)
            {
                isFadingOut = false;
                IsFadeOutFinished = true;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// エフェクトを再生する
        /// </summary>
        public void EnsurePlaying()
        {
            if (ps == null)
            {
                return;
            }

            if (isFadingOut)
            {
                isFadingOut = false;
            }

            IsFadeOutFinished = false;
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = baseRateOverTimeMultiplier;

            if (!ps.isPlaying)
            {
                ps.Play();
            }
        }

        /// <summary>
        /// 再生状態に関係なく、エフェクトの位置・角度・サイズを更新する
        /// </summary>
        public void UpdateVisual(Vector2 center, float angle, float length)
        {
            transform.position = center;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (ps == null)
            {
                return;
            }

            var shape = ps.shape;
            shape.scale = new Vector3(length, shape.scale.y, shape.scale.z);
        }

        /// <summary>
        /// 放出量を下げてフェードアウトを開始する
        /// </summary>
        public void BeginFadeOut()
        {
            if (ps == null)
            {
                IsFadeOutFinished = true;
                return;
            }

            if (isFadingOut)
            {
                return;
            }

            isFadingOut = true;
            fadeTimer = 0f;
            IsFadeOutFinished = false;
        }
    }
}