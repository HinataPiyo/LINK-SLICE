namespace Enemy
{
    using UnityEngine;
    using Common;
    using UI;


    public class Health : HealthBase
    {
        protected override void Initialize()
        {
            
        }

        protected override void TakeDamageInternal(int damage)
        {
            if (!IsServer) return;
            if (IsDead) return;

            int health = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）
            WorldCanvasManager.I.ShowApplyDamageUI(transform.position, damage);     // ダメージを受けた位置にダメージ量を表示するUIを生成して表示する
            
            currentHealth.Value = health;
            if(CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }
        
        /// <summary>
        /// 敵の死亡処理。演出は基底クラスで全員に通知し、実体の削除はサーバーだけが行う。
        /// EnemySpawnController の管理リスト更新も権威側でのみ実施する。
        /// </summary>
        public override void Die()
        {
            if (!IsServer) return;

            EnemySpawnController.I.RemoveEnemy(gameObject);     // EnemySpawnControllerから敵を削除
            AudioManager.I.PlaySE("EnemyBreak");                // 敵の破壊音を再生
            base.Die();     // 基底クラスのDie()を呼び出して、死亡エフェクトを出す
        }
    }
}