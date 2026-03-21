namespace Enemy
{
    using UnityEngine;

    /// <summary>
    /// Playerが近くにいたら攻撃が通らない敵（一定距離離れると攻撃可能）
    /// </summary>
    public class Phase : Melee
    {
        [SerializeField] PhaseEnemyData phaseData;
        SpriteRenderer spriteRenderer;
        EnemyController ctrl;
        bool isPhase = false;

        void Awake()
        {
            ctrl = GetComponent<EnemyController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// ターゲットが一定距離に存在するかどうかを判定し、フェーズの切り替えを行う
        /// </summary>
        protected override void UpdateOverridden()
        {
            // ターゲットが一定距離に存在する場合
            if (ctrl.GetTarget.IsTargetOnRange())
            {
                // 一回だけの処理にしたいため
                isPhase = true;
                ChangePhase();
            }
            else
            {
                isPhase = false;
                ChangePhase();
            }
        }

        /// <summary>
        /// フェーズの切り替え
        /// </summary>
        void ChangePhase()
        {
            if (isPhase)
            {
                // フェーズに入る前の処理
                ctrl.Movement.SetMoveSpeed(phaseData.moveSpeedInPhase);
                gameObject.layer = LayerMask.NameToLayer(GlobalCommon.LAYER_INVISIBLE);    // レイヤーを変更して攻撃が通らないようにする
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);    // 半透明にする
            }
            else
            {
                // フェーズに入る前の処理
                ctrl.Movement.SetMoveSpeed(phaseData.moveSpeedOutPhase);
                gameObject.layer = LayerMask.NameToLayer(GlobalCommon.LAYER_ENEMY);    // レイヤーを変更して攻撃が通らないようにする
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f);    // 元の色に戻す
            }
        }
    }
}