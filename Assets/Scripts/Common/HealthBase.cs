namespace Common
{
    using Common.Effect;
    using UnityEngine;
    
    public abstract class HealthBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected int maxHealth = 1;
        [SerializeField] Die dieEffectPrefab;
        protected int currentHealth;

        public int CurrentHealth => currentHealth;
        public bool IsDead { get; private set; } = false;

        void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            currentHealth = maxHealth;
        }

        public abstract void TakeDamage(int damage);

        public virtual void Die()
        {
            if(dieEffectPrefab == null || IsDead) return;
            IsDead = true;
            Instantiate(dieEffectPrefab, transform.position, Quaternion.identity);     // 死亡エフェクトを出す
        }
    }
}