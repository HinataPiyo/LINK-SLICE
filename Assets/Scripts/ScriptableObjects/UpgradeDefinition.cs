namespace Upgrade
{
    using UnityEngine;
    

    /// <summary>
    /// 個々のアップグレード要素のデータクラス。
    /// アップグレードの定義はこのクラスを継承して実装する。
    /// </summary>
    public abstract class UpgradeDefinition : ScriptableObject
    {
        public string upgradeName;     // アップグレードの名前
        public Sprite icon;           // アップグレードのアイコン

        public abstract string GetDescription();
    }
}