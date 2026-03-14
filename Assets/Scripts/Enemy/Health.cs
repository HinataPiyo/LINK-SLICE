namespace Enemy
{
    using UnityEngine;
    using Common;
    
    public class Health : HealthBase
    {

        protected override void Initialize()
        {
            
        }

        public override void TakeDamage(int damage)
        {
            if(IsDead) return;     // すでに死亡している場合はダメージを受けない
            
            currentHealth = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）
            if(CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }

        public override void Die()
        {
            base.Die();     // 基底クラスのDie()を呼び出して、死亡エフェクトを出す
            EnemySpawnController.I.RemoveEnemy(gameObject);     // EnemySpawnControllerから敵を削除
            Destroy(gameObject);     // ゲームオブジェクトを破壊
        }
    }
}