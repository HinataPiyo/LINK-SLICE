namespace Enemy
{
    using UnityEngine;
    using Unity.Netcode;
    
    public abstract class Attack : NetworkBehaviour 
    {
        [SerializeField] protected EnemyDefinition enemyData;
        float elapsedTime = 0f;                      // 経過時間
        IDamageable damageableTarget;                      // ダメージを与える対象

        public bool IsAtatcking { get; private set; } = false;     // 攻撃中かどうかのフラグ

        /// <summary>
        /// 攻撃のフラグを変更するメソッド
        /// </summary>
        /// <param name="isAttacking">攻撃中かどうかのフラグ</param>
        /// <param name="target">ダメージを与える対象のゲームオブジェクト</param>
        public void ChangeAttackFlag(bool isAttacking, GameObject target)
        {
            if(target != null)
            {
                damageableTarget = target.GetComponent<IDamageable>();   // ターゲットからIDamageableコンポーネントを取得
                Reset();
            }
            
            IsAtatcking = isAttacking;
        }

        void Update()
        {
            UpdateOverridden();
            
            if(!IsAtatcking) return;                // 攻撃していない場合は、以降の処理をスキップ
            if(damageableTarget == null) return;    // ダメージを与える対象がいない場合は、以降の処理をスキップ

            elapsedTime += Time.deltaTime;          // 経過時間を更新
            if(elapsedTime >= enemyData.attackRate)           // 攻撃の頻度に達した場合
            {
                OnAction(damageableTarget);         // ターゲットに攻撃を行う
                Reset();
            }
        }

        protected virtual void UpdateOverridden() { }    // 派生クラスで必要に応じてUpdateをオーバーライドできるようにする
        public abstract void OnAction(IDamageable target);    // 攻撃のアクションを定義する抽象メソッド

        /// <summary>
        /// 攻撃の経過時間をリセットするメソッド
        /// </summary>
        void Reset()
        {
            elapsedTime = 0f;    // 経過時間をリセット
        }
    }
}