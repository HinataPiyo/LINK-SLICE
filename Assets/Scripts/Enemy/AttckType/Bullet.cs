using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [SerializeField] LayerMask targetLayer;
    int damage;
    float speed;
    Vector3 direction;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector3 direction, float speed, int damage)
    {
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // TargetLayerに応じて攻撃判定を行う
        if (((1 << col.gameObject.layer) & targetLayer) != 0 && col.TryGetComponent(out IDamageable damageableTarget))
        {
            damageableTarget.ApplyDamage(damage);
            Destroy(gameObject);
        }
    }
}