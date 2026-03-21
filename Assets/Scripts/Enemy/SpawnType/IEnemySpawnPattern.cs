namespace Enemy.SpawnType
{
    using System.Collections;

    public interface IEnemySpawnPattern
    {
        IEnumerator SpawnComposition(EnemySpawnController controller, EnemyWaveEntry.Composition composition, SpawnPatternContext context);
    }
}