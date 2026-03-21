using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    int damage;
    float speed;
    LayerMask targetLayer;
    Vector3 direction;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector3 direction, float speed, int damage, LayerMask targetLayer)
    {
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.targetLayer = targetLayer;
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