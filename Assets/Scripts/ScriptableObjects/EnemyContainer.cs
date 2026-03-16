using UnityEngine.U2D.IK;

namespace Enemy
{

    using UnityEngine;

    [CreateAssetMenu(fileName = "EnemyContainer", menuName = "Config/EnemyContainer")]
    public class EnemyContainer : ScriptableObject
    {
        [SerializeField] Entry[] enemiesPrefabs;

        public GameObject GetEnemyPrefab(EnemyType type)
        {
            // 指定されたEnemyTypeに対応する敵のプレハブを検索して返す
            foreach (var entry in enemiesPrefabs)
            {
                if (entry.enemyType == type)
                    return entry.enemyPrefab;
            }

            Debug.LogError($"Enemy prefab for type {type} not found!");
            return null;
        }

        [System.Serializable]
        public class Entry
        {
            public EnemyType enemyType;
            public GameObject enemyPrefab;
        }
    }
}