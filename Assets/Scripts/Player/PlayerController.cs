namespace PlayerSystem
{
    using Unity.Netcode;
    using Unity.VisualScripting;
    using UnityEngine;

    /// <summary>
    /// Player操作用の明示的なエントリポイント。
    /// 実際の移動処理は Movement に集約する。
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        // Camera mainCamera;
        
        // // カメラ制御は個人で行う
        // void OnEnable()
        // {
        //     mainCamera = Camera.main;
        // }

        // void Update()
        // {
        //     if(IsServer) return;        // サーバーはカメラを動かさない
        //     mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
        // }
    }
}