namespace Upgrade
{
    using UnityEngine;
    

    /// <summary>
    /// 個々のアップグレード要素のデータクラス。
    /// アップグレードの定義はこのクラスを継承して実装する。
    /// </summary>
    public class UpgradeDefinition : ScriptableObject
    {
        public string upgradeName;     // アップグレードの名前
        public string description;     // アップグレードの説明
        public Sprite icon;           // アップグレードのアイコン
    }
}