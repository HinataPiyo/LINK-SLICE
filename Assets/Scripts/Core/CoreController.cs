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
        /// ネットワークスポーン時の処理。
        /// HealthコンポーネントのHealthStateChangedイベントにCoreVisualUpdateメソッドを登録する。
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (health != null)
            {
                health.HealthStateChanged += CoreVisualUpdate;
            }
            if(!IsClient) return;       // クライアントでない場合は処理を行わない
            CoreVisualUpdate();         // ネットワークスポーン時にローカルのビジュアルを更新する
        }

        /// <summary>
        /// ネットワークデスポーン時の処理。
        /// HealthコンポーネントのHealthStateChangedイベントからCoreVisualUpdateメソッドを解除する。
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (health != null)
            {
                health.HealthStateChanged -= CoreVisualUpdate;
            }
            base.OnNetworkDespawn();
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
            if (energyCircleTransform == null) return;
            if (health == null) return;
            if (health.MaxHealth <= 0) return;   // 最大体力が0以下の場合はエネルギーの割合を計算できないため、処理を行わない

            float energyRatio = Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);   // 現在の体力と最大体力からエネルギーの割合を計算する
            Debug.Log($"CurrentHealth: {health.CurrentHealth}, MaxHealth: {health.MaxHealth}, EnergyRatio: {energyRatio}");
            energyCircleTransform.localScale = new Vector3(energyRatio, energyRatio, 1f);
        }

        /// <summary>
        /// コアのビジュアルを更新するためのRPC。サーバーから全クライアントに向けて呼び出される。
        /// </summary>
        [ClientRpc]
        public void CoreVisualUpdateClientRpc()
        {
            if (!IsSpawned) return;     // オブジェクトがスポーンされていない場合は処理を行わない
            if (!IsClient) return;      // クライアントでない場合は処理を行わない
            CoreVisualUpdate();
        }
    }
}
