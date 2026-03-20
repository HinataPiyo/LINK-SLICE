namespace Upgrade.Data
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "CoreHealthUp", menuName = "UpgradeData/CoreHealthUp")]
    public class CoreHealthUp : UpgradeDefinition
    {
        [Header("コアの体力を増加させる設定（%）")]
        [Range(0.1f, 2f)] public float healthIncreasePercent = 1f;   // コアの体力を増加させる割合（例: 0.2なら20%増加）

        public override string GetDescription()
        {
            return $"コアの体力が{healthIncreasePercent * 100f}%増加する。";
        }
    }
}