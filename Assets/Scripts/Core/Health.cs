namespace Core
{
    using UnityEngine;
    using Common;
    
    public class Health : HealthBase
    {
        CoreController coreCtrl;

        protected override void Initialize()
        {
            base.Initialize();
            coreCtrl = GetComponent<CoreController>();
        }

        /// <summary>
        /// ダメージを受けると体力が減り、体力が0以下になると死亡する。
        /// </summary>
        /// <param name="damage"></param>
        public override void TakeDamage(int damage)
        {
            if(IsDead) return;     // すでに死亡している場合はダメージを受けない
            
            currentHealth = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）

            coreCtrl.CoreVisualUpdate();     // ダメージを受けたときの処理を呼び出す

            if(CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }

        /// <summary>
        /// 死亡処理。基底クラスのDie()を呼び出して死亡エフェクトを出し、その後ゲームオブジェクトを破壊する。
        /// </summary>
        public override void Die()
        {
            base.Die();     // 基底クラスのDie()を呼び出して、死亡エフェクトを出す
            Destroy(gameObject);     // ゲームオブジェクトを破壊
        }
    }
}