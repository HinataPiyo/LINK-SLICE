namespace Enemy.SpawnType
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// 群れのように、前の敵の位置を基準に次の敵を生成するパターン。
    /// 例: 1体目はランダム位置、2体目以降は前の敵の近くに生成し、全体として群れを形成する。
    /// context.PreviousSwarmMemberを利用して、前回生成した敵のTransformを保持・参照する。
    /// </summary>
    public class SwarmSpawnPattern : IEnemySpawnPattern
    {
        public IEnumerator SpawnComposition(EnemySpawnController controller, EnemyWaveEntry.Composition composition, SpawnPatternContext context)
        {
            // context.PreviousSwarmMemberを利用
            if (context.PreviousSwarmMember == null)
                context.PreviousSwarmMember = null;

            for (int i = 0; i < composition.spawnCount; i++)
            {
                bool isFirstInComposition = i == 0;
                Vector3 spawnPosition = isFirstInComposition
                    ? controller.GetRandomSpawnPosition()
                    : controller.GetSwarmSpawnPosition(context.PreviousSwarmMember);

                GameObject spawnedEnemy = controller.SpawnEnemy(composition.enemyType, spawnPosition);
                controller.ConfigureSwarmMovement(spawnedEnemy, context.PreviousSwarmMember, isFirstInComposition);

                context.PreviousSwarmMember = spawnedEnemy.transform;
                yield return null;
            }

            yield return new WaitForSeconds(composition.spawnInterval);
        }
    }
}