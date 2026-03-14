namespace PlayerSystem
{
    using UnityEngine;
    using Unity.Netcode;

    public class Movement : NetworkBehaviour
    {
        [SerializeField] float moveSpeed = 5f;

        void Update()
        {
            //! if (!IsOwner) return;        // 自分のプレイヤーオブジェクトでなければ、以降の処理をスキップ

            if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPosition))
            {
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, mouseWorldPosition, moveSpeed * Time.deltaTime);
            transform.position = new Vector3(Mathf.Clamp(nextPosition.x, FollowCamera.LimitMoveAreaMin.x, FollowCamera.LimitMoveAreaMax.x),
                                             Mathf.Clamp(nextPosition.y, FollowCamera.LimitMoveAreaMin.y, FollowCamera.LimitMoveAreaMax.y),
                                             nextPosition.z);
        }

        /// <summary>
        /// マウスのスクリーン座標をワールド座標に変換する。失敗した場合はfalseを返す。
        /// </summary>
        /// <param name="mouseWorldPosition">変換されたワールド座標</param>
        bool TryGetMouseWorldPosition(out Vector3 mouseWorldPosition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mouseWorldPosition = transform.position;
                return false;
            }

            Plane movementPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!movementPlane.Raycast(mouseRay, out float enter))
            {
                mouseWorldPosition = transform.position;
                return false;
            }

            mouseWorldPosition = mouseRay.GetPoint(enter);
            mouseWorldPosition.z = transform.position.z;
            return true;
        }
    }
}