namespace UI
{
    using UnityEngine;

    public class WorldCanvasManager : MonoBehaviour
    {
        public static WorldCanvasManager I { get; private set; }

        [SerializeField] ApplyDamageUI applyDamageUIPrefab;

        void Awake()
        {
            if (I == null) I = this;
        }

        /// <summary>
        /// ダメージを受けた位置にダメージ量を表示するUIを生成して表示する
        /// </summary>
        /// <param name="position"></param>
        /// <param name="damageAmount"></param>
        public void ShowApplyDamageUI(Vector2 position, int damageAmount)
        {
            if (applyDamageUIPrefab == null)
            {
                Debug.LogWarning("ApplyDamageUI prefabが未設定です", this);
                return;
            }

            Instantiate(applyDamageUIPrefab, position, Quaternion.identity, transform).ShowDamage(damageAmount);
        }

    }
}