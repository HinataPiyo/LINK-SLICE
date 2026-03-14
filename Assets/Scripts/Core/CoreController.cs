namespace Core
{
    using UnityEngine;

    public class CoreController : MonoBehaviour
    {
        [Header("コアが確保しているエネルギーの設定")]
        [SerializeField] float maxGenerateEnergy = 100f;    // 初期エネルギー
        [SerializeField] Transform energyCircleTransform;   // エネルギーの円のTransform

        [Header("四角が回転するための設定")]
        [SerializeField] float squareRotationSpeed = 10f;   // 回転速度
        [SerializeField] Transform squareTransform;         // 回転させる四角のTransform

        public float CurrentEnergy { get; private set; }

        void Awake()
        {
            CurrentEnergy = maxGenerateEnergy;    // 初期エネルギーを設定
            CoreVisualUpdate();                   // コアのビジュアルを初期化
        }

        void Update()
        {
            SquareRotationUpdate();
        }

        /// <summary>
        /// 四角の回転処理
        /// </summary>
        void SquareRotationUpdate()
        {
            squareTransform.Rotate(Vector3.forward, squareRotationSpeed * Time.deltaTime);   // 四角を回転させる
        }

        /// <summary>
        /// コアのビジュアルを更新する処理
        /// </summary>
        public void CoreVisualUpdate()
        {
            if(energyCircleTransform == null) return;
            // エネルギーの円のスケールをエネルギーの割合に応じて変える
            float energyRatio = Mathf.Clamp01(CurrentEnergy / maxGenerateEnergy);
            energyCircleTransform.localScale = new Vector3(energyRatio, energyRatio, 1f);
        }
    }
}