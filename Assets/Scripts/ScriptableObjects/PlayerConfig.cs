using PlayerSystem.Link;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "PlayerConfig", order = 0)]
public class PlayerConfig : ScriptableObject
{
    [Header("コアの設定")]
    [SerializeField] CoreEntry core;

    [Header("リンクの設定")]
    [SerializeField] LinkEntry link;

    public CoreEntry Core => core;
    public LinkEntry Link => link;

    [System.Serializable]
    public class CoreEntry
    {
        public int maxHealth = 100;
    }

    [System.Serializable]
    public class LinkEntry
    {
        [Header("基本設定")]
        public Link linkPrefab;
        public int strength = 1;
        public float distance = 8f;
        public float attackIntarval = 0.5f;
        public LayerMask targetLayer;

        [Header("エフェクトの設定")]
        public float maxLineWidth = 0.15f;
        public float growDuration = 0.2f;
        public float shrinkDuration = 0.12f;
        public float maxAnimationDeltaTime = 1f / 60f;
    }
}