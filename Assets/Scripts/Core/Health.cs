namespace Core
{
    using UnityEngine;
    using Common;

    public class Health : HealthBase
    {
        CoreController coreCtrl;

        protected override void Initialize()
        {
            coreCtrl = GetComponent<CoreController>();
        }

        /// <summary>
        /// ダメージを受けると体力が減り、体力が0以下になると死亡する。
        /// </summary>
        /// <param name="damage"></param>
        protected override void TakeDamageInternal(int damage)
        {
            if (!IsServer) return;
            if (IsDead) return;

            currentHealth.Value = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）

            if(CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }

        /// <summary>
        /// 死亡処理。基底クラスのDie()で全クライアントへ演出を配り、その後サーバーだけが NetworkObject を Despawn する。
        /// Netcode では Despawn 権限はサーバー専用なので、クライアント側から Destroy/Despawn させない。
        /// </summary>
        public override void Die()
        {
            if (!IsServer) return;
            base.Die();
        }

        /// <summary>
        /// 体力をアップグレードする処理。
        /// アップグレードの効果に応じて体力を増加させる。
        /// </summary>
        /// <param name="percent"> 体力増加の割合（例: 0.1f は10%増加）</param>
        public void UpgradeHealth(float percent)
        {
            if (!IsServer) return;
            int max = Mathf.RoundToInt(defaultMaxHealth * (1f + percent));     // 最大体力を増加させる
            currentHealth.Value = max;     // 現在の体力も最大体力に合わせて回復させる
            MaxHealth = max;     // 最大体力を更新する

            Debug.Log($"Health upgraded! New MaxHealth: {max}, CurrentHealth: {CurrentHealth}");
        }
    }
}