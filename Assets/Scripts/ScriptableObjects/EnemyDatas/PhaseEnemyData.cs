namespace Enemy
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "EnemyData_Phase", menuName = "EnemyData/EnemyData_Phase")]
    public class PhaseEnemyData : EnemyDefinition
    {
        [Header("Phase専用データ")]
        public float moveSpeedInPhase;
        public float moveSpeedOutPhase;
    }
}