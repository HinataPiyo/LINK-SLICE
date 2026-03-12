namespace Common.Effect
{
    using UnityEngine;
    
    public class Die : MonoBehaviour
    {
        [SerializeField] ParticleSystem ps;
        [SerializeField] Material setMaterial;

        void Awake()
        {
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = setMaterial;
        }

        void Update()
        {
            if (!ps.IsAlive(true))      // パーティクルシステムが完全に死んでいたら
            Destroy(gameObject);        // 全てのパーティクルシステムが死んでいたらオブジェクトを破壊する
        }
    }
}