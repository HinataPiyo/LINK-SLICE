namespace Enemy
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "EnemyData_LongRange", menuName = "EnemyData/EnemyData_LongRange")]
    public class LongRangeEnemyData : EnemyDefinition
    {
        [Header("LongRange専用データ")]
        public float bulletSpeed;
        public GameObject bulletPrefab;
    }
}