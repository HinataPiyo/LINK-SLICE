namespace Player
{
    using Unity.Cinemachine;
    using UnityEngine;

    public class FollowCamera : MonoBehaviour
    {
        CinemachineCamera cinemachineCamera;
        public static readonly Vector2 LimitMoveAreaMin = new Vector2(-20f, -15f);
        public static readonly Vector2 LimitMoveAreaMax = new Vector2(20f, 15f);

        void Start()
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Player タグのオブジェクトが見つかりません");
                return;
            }

            cinemachineCamera.Follow = player.transform;
        }
    }
}