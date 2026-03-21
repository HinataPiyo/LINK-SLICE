namespace Enemy.SpawnType
{
    using System.Collections;
    using UnityEngine;

    public class RandomSpawnPattern : IEnemySpawnPattern
    {
        public IEnumerator SpawnComposition(EnemySpawnController controller, EnemyWaveEntry.Composition composition, SpawnPatternContext context)
        {
            for (int i = 0; i < composition.spawnCount; i++)
            {
                controller.SpawnEnemy(composition.enemyType, controller.GetRandomSpawnPosition());
                yield return new WaitForSeconds(composition.spawnInterval);
            }
        }
    }
}