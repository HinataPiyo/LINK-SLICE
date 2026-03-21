namespace Enemy
{
    using UnityEngine;
    using Unity.Netcode;
    
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(GetTarget))]
    public class EnemyController : NetworkBehaviour
    {
        public Attack Attack { get; private set; }
        public Movement Movement { get; private set; }
        public GetTarget GetTarget { get; private set; }
        public Collider2D Col { get; private set; }

        void Awake()
        {
            Movement = GetComponent<Movement>();
            GetTarget = GetComponent<GetTarget>();
            Attack = GetComponent<Attack>();
            Col = GetComponent<Collider2D>();
        }

        void Update()
        {
            if(!IsServer) return;            // サーバーでなければ、以降の処理をスキップ
            
            if(Attack.IsAtatcking) return;      // 攻撃中であれば、以降の処理をスキップ

            GameObject target = GetTarget.GetTargetObject();    

            // ターゲットが存在する場合 かつ 攻撃していない場合
            if(target != null)
            {
                Attack.ChangeAttackFlag(true, target);     // 攻撃のフラグを変更（攻撃している状態に変更）
            }
            else    // ターゲットが存在しない場合
            {
                Movement.Move();        // 移動処理
            }
        }
    }
}