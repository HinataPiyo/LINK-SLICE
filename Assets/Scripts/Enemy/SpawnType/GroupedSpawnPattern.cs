namespace Enemy.SpawnType
{
    using System.Collections;
    using UnityEngine;

    public class GroupedSpawnPattern : IEnemySpawnPattern
    {
        public IEnumerator SpawnComposition(EnemySpawnController controller, EnemyWaveEntry.Composition composition, SpawnPatternContext context)
        {
            // context.GroupAnchorを利用
            if (context.GroupAnchor == null)
                context.GroupAnchor = null;

            for (int i = 0; i < composition.spawnCount; i++)
            {
                bool isFirstInComposition = i == 0;
                Vector3 spawnPosition = isFirstInComposition
                    ? controller.GetRandomSpawnPosition()
                    : controller.GetGroupedSpawnPosition(context.GroupAnchor.Value);

                GameObject spawnedEnemy = controller.SpawnEnemy(composition.enemyType, spawnPosition);
                if (isFirstInComposition)
                {
                    context.GroupAnchor = spawnedEnemy.transform.position;
                }

                yield return null;
            }

            yield return new WaitForSeconds(composition.spawnInterval);
        }
    }
}