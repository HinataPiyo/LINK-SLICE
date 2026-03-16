namespace Enemy
{
    using UnityEngine;
    
    public class GetTarget : MonoBehaviour
    {
        // 進んでる方向にRayを飛ばし接触したTargetを取得する
        [SerializeField] float rayDistance = 1f;        // Rayの距離
        [SerializeField] LayerMask targetLayerMask;     // ターゲットのレイヤーマスク

        /// <summary>
        /// ターゲット取得する
        /// </summary>
        public GameObject GetTargetObject()
        {
            Vector2 direction = Vector2.zero - (Vector2)transform.position;     // 原点の方を向くベクトル
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, rayDistance, targetLayerMask);
            bool isHit = hit.collider != null;     // Rayが何かに当たったかどうかのフラグ
            Debug.DrawRay(transform.position, direction.normalized * rayDistance, isHit ? Color.red : Color.green);    // Rayを可視化（当たった場合は赤、当たらなかった場合は緑）
            if (isHit) return hit.collider.gameObject;
            return null;
        }
    }
}