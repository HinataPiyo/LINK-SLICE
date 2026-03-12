namespace Player
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// プレイヤーを移動させるクラス
    /// </summary>
    public class Movement : MonoBehaviour
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
            // マウスが使える環境では毎フレーム現在位置を取得する
            if (Mouse.current != null)
            {
                pointerScreenPos = Mouse.current.position.ReadValue();
                hasPointerPos = true;
            }

            if (!hasPointerPos)
            {
                return;
            }

            // 画面座標のカーソル位置をプレイヤーと同じZ平面のワールド座標へ変換する
            float depth = transform.position.z - Camera.main.transform.position.z;
            Vector3 cursorWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, depth));
            cursorWorldPos.z = transform.position.z;

            // 目標に近いほど速度を落とす（停止距離以内なら完全停止）
            float distance = Vector2.Distance(transform.position, cursorWorldPos);
            if (distance <= stopDistance)
            {
                return;
            }

            // 目標からの距離に応じて速度を調整する
            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.0001f, slowDownRadius));
            float currentSpeed = moveSpeed * normalizedDistance;

            // プレイヤーを目標に向かって移動させる
            transform.position = Vector2.MoveTowards(transform.position, cursorWorldPos, currentSpeed * Time.deltaTime);
        }

    }
}