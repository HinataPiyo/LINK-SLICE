namespace Enemy
{
    using UnityEngine;
    
    public class Movement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;


        void Update()
        {
            // とりあえず原点に向かって動かすだけ。必要に応じてプレイヤーを追いかけるなどの処理を追加する
            transform.position = Vector2.MoveTowards(transform.position, Vector2.zero, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector2.zero - (Vector2)transform.position);     // 原点の方を向く
        }
    }
}