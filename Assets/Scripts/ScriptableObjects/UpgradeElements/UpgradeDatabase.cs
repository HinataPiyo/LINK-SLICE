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

        /// <summary>
        /// アップグレードの定義をランダムに選出して返す処理をここに実装する
        /// </summary>
        public UpgradeDefinition[] GetRandomUpgradeDefinitions(int count)
        {
            List<UpgradeDefinition> shuffledUpgrades = new List<UpgradeDefinition>(upgrades);
            ShuffleList(shuffledUpgrades);    // アップグレードのリストをシャッフルする

            return shuffledUpgrades.GetRange(0, Mathf.Min(count, shuffledUpgrades.Count)).ToArray();   // シャッフルされたリストから指定された数だけ取得して返す
        }

        /// <summary>
        /// リストをシャッフルする処理をここに実装する
        /// </summary>
        /// <typeparam name="T">シャッフルするリストの要素の型</typeparam>
        /// <param name="list">シャッフルするリスト</param>
        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);   // 0からiまでのランダムなインデックスを生成する
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}