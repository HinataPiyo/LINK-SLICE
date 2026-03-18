namespace Upgrade
{
    /// <summary>
    /// アップグレードの効果の基底クラス。
    /// アップグレードの効果はこのクラスを継承して実装する。
    /// </summary>
    public abstract class EffectBase
    {
        public virtual void ApplyEffect()
        {
            // 効果を適用する処理をここに実装する
        }

        public abstract float GetUpgradeValue(int activeCount);
    }
}