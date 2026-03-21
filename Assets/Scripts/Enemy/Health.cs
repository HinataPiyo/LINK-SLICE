namespace Enemy
{
    using Unity.Netcode;
    using UnityEngine;
    using Common;
    using UI;
    using System.Collections.Generic;


    public class Health : HealthBase
    {
        [SerializeField] EnemyDefinition enemyData;
        Stack<Armor> armors = new Stack<Armor>();     // 装甲を格納するスタック。装甲は階層順にスタックされる（子オブジェクトの順番）

        void Awake()
        {
            Initialize(enemyData.maxHealth, enemyData.dieEffectPrefab);
            ArmorInitialize();
        }

        protected override void TakeDamageInternal(int damage)
        {
            if (!IsServer) return;
            if (armors.Count > 0) return;    // 装甲が残っている場合はダメージを受けない
            if (IsDead) return;

            int health = Mathf.Max(CurrentHealth - damage, 0);     // ダメージを受けて体力を減らす（0未満にならないようにする）
            ShowApplyDamageUIClientRpc(transform.position, damage);     // 各クライアントでローカルにダメージUIを生成して表示する

            currentHealth.Value = health;
            if (CurrentHealth <= 0f)       // 体力が0以下になった場合
            {
                Die();      // 死亡処理
            }
        }

        /// <summary>
        /// 敵の死亡処理。演出は基底クラスで全員に通知し、実体の削除はサーバーだけが行う。
        /// EnemySpawnController の管理リスト更新も権威側でのみ実施する。
        /// </summary>
        public override void Die()
        {
            if (!IsServer) return;

            EnemySpawnController.I.RemoveEnemy(gameObject);     // EnemySpawnControllerから敵を削除
            AudioManager.I.PlaySE("EnemyBreak");                // 敵の破壊音を再生
            base.Die();     // 基底クラスのDie()を呼び出して、死亡エフェクトを出す
        }

        /// <summary>
        /// 敵の装甲を初期化するメソッド。
        /// 子オブジェクトにアタッチされているArmorコンポーネントを階層順にスタックに積み、最初の装甲だけダメージを受けるようにする。
        /// </summary>
        void ArmorInitialize()
        {
            Armor[] armors = GetComponentsInChildren<Armor>();     // 子オブジェクトにアタッチされているArmorコンポーネントを全て取得して配列に格納
            if (armors.Length > 0)
            {
                System.Array.Sort(armors, (a, b) =>
                {
                    // 配列を子オブジェクトの階層順にソート
                    return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
                });

                // ソートされた配列の順番でスタックに積む
                foreach (Armor armor in armors)
                {
                    armor.SetDamageable(false);     // 最初は装甲はダメージを受けないようにする
                    this.armors.Push(armor);        // ソートされた配列の順番でスタックに積む
                    armor.RequireRemove(() =>
                    {
                        this.armors.Pop();           // 装甲が破壊されたときにスタックから取り出す
                        if (this.armors.Count > 0) this.armors.Peek().SetDamageable(true);      // 次の装甲をダメージを受けるようにする
                    });
                }

                this.armors.Peek().SetDamageable(true);      // 最初の装甲だけダメージを受けるようにする
            }
        }
    }
}