namespace Enemy
{
    using Common.Effect;
    using UnityEngine;

    [CreateAssetMenu(fileName = "EnemyData_Armor", menuName = "EnemyData/EnemyData_Armor")]
    public class ArmorEnemyData : EnemyDefinition
    {
        [Header("Armor専用データ")]
        public int armorHealth;              // 装甲の耐久値
        public Die armorDieEffectPrefab;    // 装甲の死亡エフェクト
    
    }
}