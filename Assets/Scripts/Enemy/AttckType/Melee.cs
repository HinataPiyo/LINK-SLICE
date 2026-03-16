namespace Enemy
{
    /// <summary>
    /// 近接攻撃を行うクラス。Attackクラスを継承し、OnActionメソッドで具体的な攻撃内容を定義する。
    /// </summary>
    public class Melee : Attack
    {

        /// <summary>
        /// 攻撃のアクションを定義する抽象メソッドを実装
        /// 近接攻撃の具体的な内容はここで定義される。
        /// </summary>
        public override void OnAction(IDamageable target)
        {
            target.ApplyDamage(strength);      // ダメージを与える
        }
    }
}