namespace Common
{

    using UnityEngine;

    /// <summary>
    /// オーディオ管理クラス
    /// シングルトン化していますが、シーン遷移引継ぎは考慮していません。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }     // シングルトンインスタンス
        [SerializeField] AudioData audioData;

        [SerializeField] AudioSource se_source;     // 効果音用のAudioSource
        [SerializeField] AudioSource bgm_source;    // BGM用のAudioSource

        void Awake()
        {
            if (I == null) I = this;
        }

        /// <summary>
        /// タグに対応するSEを再生する
        /// </summary>
        public bool PlaySE(string tag)
        {
            AudioClip clip = audioData.GetAudioClip(tag);
            if (clip != null)
            {
                se_source.PlayOneShot(clip);
                return true;
            }
            else
            {
                Debug.LogWarning($"AudioManager: Failed to play SE with tag '{tag}'.");
                return false;
            }
        }

        /// <summary>
        /// タグに対応するBGMを再生する
        /// </summary>
        /// <param name="tag"> 取得したいBGMのタグ </param>
        /// <param name="loop"> BGMをループ再生するかどうか </param>
        public void PlayBGM(string tag, bool loop = true)
        {
            AudioClip clip = audioData.GetAudioClip(tag);
            if (clip != null)
            {
                bgm_source.clip = clip;
                bgm_source.loop = loop;
                bgm_source.Play();
            }
            else
            {
                Debug.LogWarning($"AudioManager: Failed to play BGM with tag '{tag}'.");
            }
        }
    }

}