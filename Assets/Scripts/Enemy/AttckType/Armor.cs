namespace Enemy
{
    using Common;
    using UnityEngine;

    /// <summary>
    /// 装甲のクラス。
    /// このスクリプトはコライダーがアタッチされているarmorオブジェクトにアタッチされる。
    /// </summary>
    public class Armor : HealthBase
    {
        [SerializeField] ArmorEnemyData armorData;
        bool isDamageable = true;     // ダメージを受けることができるかどうかのフラグ
        System.Action onRemoved;     // 装甲が破壊されたときに呼び出されるコールバック
        

        public void SetDamageable(bool value) => isDamageable = value;

        void Awake()
        {
            // 基底クラスのInitializeを呼び出して、体力と死亡エフェクトをセット
            Initialize(armorData.armorHealth, armorData.armorDieEffectPrefab);
        }

        public void RequireRemove(System.Action onRemoved)
        {
            this.onRemoved = onRemoved;     // 装甲が破壊されたときに呼び出されるコールバックをセット
        }

        protected override void TakeDamageInternal(int damage)
        {
            if (!IsServer) return;
            if (!isDamageable) return;
            if (IsDead) return;

            int health = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）
            currentHealth.Value = health;
            ShowApplyDamageUIClientRpc(transform.position, damage);     // 各クライアントでローカルにダメージUIを生成して表示する
            if (CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }

        public override void Die()
        {
            base.Die();
            onRemoved?.Invoke();     // 装甲が破壊されたときのコールバックを呼び出す
            Destroy(gameObject);     // ゲームオブジェクトを破壊する
        }
    }
}