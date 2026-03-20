namespace PlayerSystem.Link
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// シーン内の全 Link が共有して参照するランタイム用ステータス。
    /// ScriptableObject の PlayerConfig を直接書き換えず、アップグレードや一時バフの結果だけをここへ集約する。
    /// </summary>
    public class LinkRuntimeStats : MonoBehaviour
    {
        readonly Dictionary<string, int> strengthFlatModifiers = new Dictionary<string, int>();
        readonly Dictionary<string, float> strengthPercentModifiers = new Dictionary<string, float>();

        int baseStrength = 1;

        /// <summary>
        /// 現在有効な Modifier を反映した最終攻撃力。
        /// Attack 側はこの値だけを参照すればよく、LinkController を毎回経由する必要がない。
        /// </summary>
        public int CurrentStrength { get; private set; } = 1;

        /// <summary>
        /// PlayerConfig 由来の基礎値で初期化する。
        /// Scene 読み込み直後や再設定時の入口を1箇所にまとめるためのメソッド。
        /// </summary>
        public void Initialize(PlayerConfig playerConfig)
        {
            if (playerConfig == null)
            {
                Debug.LogWarning("LinkRuntimeStats を初期化できません。PlayerConfig が未設定です。");
                return;
            }

            baseStrength = Mathf.Max(1, playerConfig.Link.strength);
            RecalculateStrength();
        }

        /// <summary>
        /// 攻撃力への固定値 Modifier を sourceId 単位で設定する。
        /// 同一アップグレードの再取得時に二重加算ではなく上書き再計算できるようにしている。
        /// </summary>
        public void SetStrengthFlatModifier(string sourceId, int value)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("攻撃力固定値 Modifier に空の sourceId は使用できません。");
                return;
            }

            strengthFlatModifiers[sourceId] = value;
            RecalculateStrength();
        }

        /// <summary>
        /// 攻撃力への割合 Modifier を sourceId 単位で設定する。
        /// 永続アップグレードと一時バフを同じ仕組みで扱えるようにするための入口。
        /// </summary>
        public void SetStrengthPercentModifier(string sourceId, float value)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("攻撃力割合 Modifier に空の sourceId は使用できません。");
                return;
            }

            strengthPercentModifiers[sourceId] = value;
            RecalculateStrength();
        }

        /// <summary>
        /// 指定 sourceId の Modifier を解除する。
        /// 効果切れやデバッグリセット時に同じ API で扱えるようにしている。
        /// </summary>
        public void RemoveStrengthModifier(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            bool removedFlat = strengthFlatModifiers.Remove(sourceId);
            bool removedPercent = strengthPercentModifiers.Remove(sourceId);
            if (removedFlat || removedPercent)
            {
                RecalculateStrength();
            }
        }

        /// <summary>
        /// 基礎値と全 Modifier から最終攻撃力を再計算する。
        /// 計算ロジックを1箇所へ寄せておくことで、将来の攻撃力関連アップグレード追加時も変更点を限定する。
        /// </summary>
        void RecalculateStrength()
        {
            int flatBonus = 0;
            foreach (int value in strengthFlatModifiers.Values)
            {
                flatBonus += value;
            }

            float percentBonus = 0f;
            foreach (float value in strengthPercentModifiers.Values)
            {
                percentBonus += value;
            }

            int strengthWithFlatBonus = Mathf.Max(1, baseStrength + flatBonus);
            CurrentStrength = Mathf.Max(1, Mathf.RoundToInt(strengthWithFlatBonus * (1f + percentBonus)));
        }
    }
}