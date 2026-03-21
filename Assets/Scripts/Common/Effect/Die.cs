namespace Common.Effect
{
    using UnityEngine;
    
    public class Die : MonoBehaviour
    {
        [SerializeField] ParticleSystem ps;

        void Update()
        {
            if (!ps.IsAlive(true))      // パーティクルシステムが完全に死んでいたら
            Destroy(gameObject);        // 全てのパーティクルシステムが死んでいたらオブジェクトを破壊する
        }
    }
}