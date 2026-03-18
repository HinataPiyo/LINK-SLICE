namespace Upgrade
{
    public class CoreHealthUp : EffectBase
    {
        readonly Data.CoreHealthUp data;

        public CoreHealthUp(Data.CoreHealthUp data)
        {
            this.data = data;
        }

        /// <summary>
        /// アクティブな効果の数に応じて体力増加の割合を計算して返す処理をここに実装する
        /// </summary>
        /// <param name="activeCount"> アクティブな効果の数 </param>
        public override float GetUpgradeValue(int activeCount)
        {
            if (data == null) return 0f;
            return data.healthIncreasePercent * activeCount;     // アクティブな効果の数に応じて体力増加の割合を計算して返す
        }
    }
}