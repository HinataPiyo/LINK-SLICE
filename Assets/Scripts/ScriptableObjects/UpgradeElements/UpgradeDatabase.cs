namespace Upgrade
{
    using System.Collections.Generic;
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Config/UpgradeDatabase")]
    public class UpgradeDatabase : ScriptableObject
    {
        [SerializeField] List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

        public T GetUpgradeDefinition<T>() where T : UpgradeDefinition
        {
            return upgrades.Find(upgrade => upgrade is T) as T;     // 指定された型のUpgradeDefinitionを検索して返す
        }
    }
}