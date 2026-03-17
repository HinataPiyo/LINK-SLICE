using UnityEngine;


[CreateAssetMenu(fileName = "AudioData", menuName = "Config/AudioData")]
public class AudioData : ScriptableObject
{
    [SerializeField] AudioEntry[] entries;
    public AudioEntry[] Entries => entries;

    /// <summary>
    /// タグに対応するAudioClipを取得する
    /// </summary>
    /// <param name="tag"> 取得したいAudioClipのタグ </param>
    public AudioClip GetAudioClip(string tag)
    {
        foreach (AudioEntry entry in entries)
        {
            if (entry.Tag == tag)
            {
                return entry.Clip;
            }
        }
        Debug.LogWarning($"AudioData: AudioClip with tag '{tag}' not found.");
        return null;
    }

    [System.Serializable]
    public class AudioEntry
    {
        public string Tag => tag;
        public AudioClip Clip => clip;

        [SerializeField] string tag;
        [SerializeField] AudioClip clip;
    }
}