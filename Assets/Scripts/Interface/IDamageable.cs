using UnityEngine;

public interface IDamageable
{
    Vector2 GetPosition();
    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    /// <param name="damage">受けるダメージの量</param>
    void ApplyDamage(int damage);
    void Die();
}