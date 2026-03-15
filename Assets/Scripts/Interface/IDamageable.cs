using Unity.Netcode;
public interface IDamageable
{
    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    /// <param name="damage">受けるダメージの量</param>
    void TakeDamage(int damage);
    void Die();
}