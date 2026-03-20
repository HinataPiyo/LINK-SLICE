namespace Upgrade
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Config/UpgradeDatabase")]
    public class UpgradeDatabase : ScriptableObject
    {
        [SerializeField] List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        /// <summary>
        /// 指定された型の UpgradeDefinition を取得する。
        /// </summary>
        public T GetUpgradeDefinition<T>() where T : UpgradeDefinition
        {
            // 指定された型のUpgradeDefinitionを検索して返す
            return upgrades.Find(upgrade => upgrade is T) as T;
        }

        /// <summary>
        /// 指定された UpgradeDefinition のインデックスを取得する。
        /// </summary>
        /// <param name="definition">インデックスを取得したい UpgradeDefinition</param>
        public int IndexOf(UpgradeDefinition definition)
        {
            return upgrades.IndexOf(definition);
        }

        /// <summary>
        /// 指定されたインデックスの UpgradeDefinition を取得する。
        /// </summary>
        /// <param name="index">取得したい UpgradeDefinition のインデックス</param>
        public UpgradeDefinition GetUpgradeDefinitionAt(int index)
        {
            if (index < 0 || index >= upgrades.Count)
            {
                return null;
            }

            return upgrades[index];
        }
    }
}