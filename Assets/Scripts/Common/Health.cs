namespace Common
{
    using Common.Effect;
    using UnityEngine;
    
    public class Health : MonoBehaviour
    {
        [SerializeField] int maxHealth = 1;
        [SerializeField] Die dieEffectPrefab;

        public int CurrentHealth { get; private set; }

        void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            Instantiate(dieEffectPrefab, transform.position, Quaternion.identity);     // 死亡エフェクトを出す
            // とりあえずオブジェクトを非アクティブにするだけ。必要に応じて死亡エフェクトを出すなどの処理を追加する
            gameObject.SetActive(false);
        }
    }
}