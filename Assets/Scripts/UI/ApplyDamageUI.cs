namespace UI
{
    using TMPro;
    using UnityEngine;

    public class ApplyDamageUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI text;

        public void ShowDamage(int damageAmount)
        {
            text.text = damageAmount.ToString("F0");        // 小数点以下を表示しない整数形式でダメージ量を表示する
        }
    }
}