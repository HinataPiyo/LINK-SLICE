namespace Core
{
    using System.Runtime.Serialization.Json;
    using Unity.Netcode;
    using Unity.VisualScripting;
    using UnityEngine;


    public class CoreController : NetworkBehaviour
    {
        [Header("コアが確保しているエネルギーの設定")]
        [SerializeField] Transform energyCircleTransform;   // エネルギーの円のTransform

        [Header("四角が回転するための設定")]
        [SerializeField] float squareRotationSpeed = 10f;   // 回転速度
        [SerializeField] Transform squareTransform;         // 回転させる四角のTransform

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
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
        void CoreVisualUpdate()
        {
            if (energyCircleTransform == null) return;

            float energyRatio = (float)health.CurrentHealth / health.MaxHealth;     // エネルギーの割合を計算
            Debug.Log($"CurrentHealth: {health.CurrentHealth}, MaxHealth: {health.MaxHealth}, EnergyRatio: {energyRatio}");
            energyCircleTransform.localScale = new Vector3(energyRatio, energyRatio, 1f);
        }

        /// <summary>
        /// コアのビジュアルを更新するためのRPC。サーバーから全クライアントに向けて呼び出される。
        /// </summary>
        [ClientRpc]
        public void CoreVisualUpdateClientRpc()
        {
            if (!IsSpawned) return;  // オブジェクトがスポーンされていない場合は処理を行わない
            if (!IsClient) return;    // クライアントでない場合は処理を行わない
            CoreVisualUpdate();
        }
    }
}
