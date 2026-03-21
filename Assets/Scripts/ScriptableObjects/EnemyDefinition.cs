namespace Enemy
{
    using UnityEngine;
    public class EnemyDefinition : ScriptableObject
    {
        public EnemyType enemyType;
        public Common.Effect.Die dieEffectPrefab;
        public int strength;
        public float attackRate;
        public int maxHealth;
        public float moveSpeed;
        public float rangeRadius;
        public LayerMask rangeLayerMask;
        public float rayDistance;
        public LayerMask rayLayerMask;
        
        public float defaultSwarmFollowDistance = 0.6f;
    }
}