namespace PlayerSystem
{
    using Unity.Cinemachine;
    using Unity.Netcode;
    using UnityEngine;

    public class FollowCamera : MonoBehaviour
    {
        CinemachineCamera cinemachineCamera;
        public static readonly Vector2 LimitMoveAreaMin = new Vector2(-20f, -15f);
        public static readonly Vector2 LimitMoveAreaMax = new Vector2(20f, 15f);

        void Awake()
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        void Start()
        {
            TryAssignLocalPlayer();
            UpdateCameraTargetUi();
        }

        void LateUpdate()
        {
            if (cinemachineCamera == null)
            {
                return;
            }

            if (cinemachineCamera.Follow == null)
            {
                TryAssignLocalPlayer();
            }

            UpdateCameraTargetUi();
        }

        void TryAssignLocalPlayer()
        {
            if (cinemachineCamera == null)
            {
                return;
            }

            Movement[] players = FindObjectsByType<Movement>(FindObjectsSortMode.None);
            foreach (Movement player in players)
            {
                if (player == null || !player.IsSpawned || !player.IsOwner)
                {
                    continue;
                }

                cinemachineCamera.Follow = player.transform;
                UpdateCameraTargetUi();
                return;
            }

            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    cinemachineCamera.Follow = player.transform;
                    UpdateCameraTargetUi();
                }
            }
        }

        void UpdateCameraTargetUi()
        {
            if (TestUIManager.I == null)
            {
                return;
            }

            string targetName = cinemachineCamera != null && cinemachineCamera.Follow != null
                ? cinemachineCamera.Follow.name
                : "未設定";

            TestUIManager.I.SetCameraTargetInformation($"CameraTarget: {targetName}");
        }
    }
}