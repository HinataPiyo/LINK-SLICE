namespace PlayerSystem
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Unity.Netcode;

    /// <summary>
    /// プレイヤーを移動させるクラス
    /// </summary>
    public class Movement : NetworkBehaviour
    {
        [SerializeField] float moveSpeed = 10f;
        [SerializeField] float slowDownRadius = 5f;
        [SerializeField] float stopDistance = 0.03f;
        Vector2 pointerScreenPos;
        bool hasPointerPos;


        /// <summary>
        /// PlayerInput + Send Messages の場合、Action名 Move に対応してこの関数が呼ばれる
        /// </summary>
        /// <param name="value"></param>
        public void OnMove(InputValue value)
        {
            pointerScreenPos = value.Get<Vector2>();
            hasPointerPos = true;
        }

        void Update()
        {
            // 自分の操作するプレイヤー以外は処理しない
            if (!IsOwner) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // マウスが使える環境では毎フレーム現在位置を取得する
            if (Mouse.current != null)
            {
                pointerScreenPos = Mouse.current.position.ReadValue();
                hasPointerPos = true;
            }

            if (!hasPointerPos) return;

            // 画面座標のカーソル位置をプレイヤーと同じZ平面のワールド座標へ変換する
            float depth = transform.position.z - mainCamera.transform.position.z;
            Vector3 cursorWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, depth));
            cursorWorldPos.z = transform.position.z;
            cursorWorldPos.x = Mathf.Clamp(cursorWorldPos.x, FollowCamera.LimitMoveAreaMin.x, FollowCamera.LimitMoveAreaMax.x);
            cursorWorldPos.y = Mathf.Clamp(cursorWorldPos.y, FollowCamera.LimitMoveAreaMin.y, FollowCamera.LimitMoveAreaMax.y);

            // 目標に近いほど速度を落とす（停止距離以内なら完全停止）
            float distance = Vector2.Distance(transform.position, cursorWorldPos);
            if (distance <= stopDistance)
            {
                return;
            }

            // 目標からの距離に応じて速度を調整する
            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.0001f, slowDownRadius));
            float currentSpeed = moveSpeed * normalizedDistance;

            // サーバー権威のTransformでも動くように、移動適用はサーバー側で行う
            if (IsServer)
            {
                ApplyMove(cursorWorldPos, currentSpeed, Time.deltaTime);
                return;
            }

            RequestMoveServerRpc(cursorWorldPos, currentSpeed);
        }

        [ServerRpc]
        void RequestMoveServerRpc(Vector3 cursorWorldPos, float currentSpeed)
        {
            ApplyMove(cursorWorldPos, currentSpeed, Time.deltaTime);
        }

        void ApplyMove(Vector3 targetPosition, float currentSpeed, float deltaTime)
        {
            Vector3 nextPosition = Vector2.MoveTowards(transform.position, targetPosition, currentSpeed * deltaTime);
            // 目標座標と同じ境界値で最終位置も制限し、二重Clampの不一致を防ぐ。
            nextPosition.x = Mathf.Clamp(nextPosition.x, FollowCamera.LimitMoveAreaMin.x, FollowCamera.LimitMoveAreaMax.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, FollowCamera.LimitMoveAreaMin.y, FollowCamera.LimitMoveAreaMax.y);
            nextPosition.z = transform.position.z;
            transform.position = nextPosition;
        }
    }
}