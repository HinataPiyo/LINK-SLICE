namespace Core
{
    using UnityEngine;
    using Common;
    using Unity.Netcode;

    public class Health : HealthBase
    {
        CoreController coreCtrl;

        protected override void Initialize()
        {
            coreCtrl = GetComponent<CoreController>();
            coreCtrl.CoreVisualUpdate();
        }

        /// <summary>
        /// ダメージを受けると体力が減り、体力が0以下になると死亡する。
        /// </summary>
        /// <param name="damage"></param>
        public override void TakeDamage(int damage)
        {
            currentHealth.Value = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）

            coreCtrl.CoreVisualUpdate();     // ダメージを受けたときの処理を呼び出す
            Debug.Log($"CurrentHealth: {CurrentHealth}, MaxHealth: {MaxHealth}");     // デバッグ用に現在の体力と最大体力をログに出す

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
    }
}