namespace Enemy
{
    using UnityEngine;
    
    public class GetTarget : MonoBehaviour
    {
        // 進んでる方向にRayを飛ばし接触したTargetを取得する
        [SerializeField] float rayDistance = 1f;        // Rayの距離
        [SerializeField] float rangeRadius = 3f;       // ターゲットを取得する範囲の半径
        [SerializeField] LayerMask targetLayerMask;     // ターゲットのレイヤーマスク
        [SerializeField] LayerMask rangeLayerMask;      // ターゲットを取得する範囲のレイヤーマスク

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

        /// <summary>
        /// Rayを飛ばすのではなく、指定した距離内にいるターゲットを取得する
        /// </summary>
        public Collider2D[] GetOnRangeTarget()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, rangeRadius, rangeLayerMask);
            return colliders;
        }

        /// <summary>
        /// ターゲットが一定距離に存在するかどうかを判定する
        /// </summary>
        public bool IsTargetOnRange()
        {
            return GetOnRangeTarget().Length > 0;
        }

        void OnDrawGizmos()
        {
            // ターゲットを取得する範囲を可視化
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, rangeRadius);
        }
    }
}