namespace Enemy
{
    using UnityEngine;
    using Unity.Netcode;
    
    [RequireComponent(typeof(Attack))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(GetTarget))]
    public class EnemyController : NetworkBehaviour
    {
        Attack attack;
        Movement movement;
        GetTarget getTarget;

        void Awake()
        {
            movement = GetComponent<Movement>();
            getTarget = GetComponent<GetTarget>();
            attack = GetComponent<Attack>();
        }

        void Update()
        {
            if(!IsServer) return;            // サーバーでなければ、以降の処理をスキップ
            
            if(attack.IsAtatcking) return;      // 攻撃中であれば、以降の処理をスキップ
            GameObject target = getTarget.GetTargetObject();    

            // ターゲットが存在する場合 かつ 攻撃していない場合
            if(target != null)
            {
                attack.ChangeAttackFlag(true, target);     // 攻撃のフラグを変更（攻撃している状態に変更）
            }
            else    // ターゲットが存在しない場合
            {
                movement.Move();        // 移動処理
            }
        }
    }
}