namespace PlayerSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using PlayerSystem.Link;
    using Unity.Netcode;
    using UnityEngine;
    
    public class Attack : MonoBehaviour
    {
        [SerializeField] PlayerConfig playerConfig;
        LinkRuntimeStats runtimeStats;
        public readonly Dictionary<int, Coroutine> attackCooldownRoutines = new Dictionary<int, Coroutine>();
        public readonly HashSet<int> processedTargetIds = new HashSet<int>();
        public bool CanProcessAttack => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        /// <summary>
        /// LinkController から共有ランタイムステータスを注入する。
        /// 攻撃のたびに管理側へ問い合わせるのではなく、各 Link が同じ状態を直接読む構造にする。
        /// </summary>
        public void Initialize(LinkRuntimeStats linkRuntimeStats)
        {
            runtimeStats = linkRuntimeStats;
        }
        /// <summary>
        /// 攻撃処理。Healthコンポーネントを持つオブジェクトに対してダメージを与える
        /// </summary>
        public void OnAttack(IDamageable damageableTarget, int targetId)
        {
            if (!CanProcessAttack) return;
            if (damageableTarget == null) return;
            if (attackCooldownRoutines.ContainsKey(targetId)) return;     // この対象のクールダウン中は攻撃できない

            Coroutine cooldownRoutine = StartCoroutine(AttackCooldownRoutine(targetId, damageableTarget));
            attackCooldownRoutines[targetId] = cooldownRoutine;
        }

        /// <summary>
        /// 攻撃のクールダウン処理。攻撃対象ごとに独立して一定時間攻撃できない状態にする
        /// </summary>
        IEnumerator AttackCooldownRoutine(int targetId, IDamageable damageableTarget)
        {
            if (damageableTarget == null)
            {
                attackCooldownRoutines.Remove(targetId);
                yield break;
            }

            // RuntimeStats が存在する場合は、アップグレード反映後の最終攻撃力を優先して使用する。
            // フォールバックを残すことで、注入漏れ時にも最低限従来挙動を維持する。
            int damage = runtimeStats != null ? runtimeStats.CurrentStrength : playerConfig.Link.strength;
            float interval = runtimeStats != null ? runtimeStats.CurrentInterval : playerConfig.Link.attackIntarval;

            // 最初の攻撃はクールダウンを無視して即時実行
            if (!processedTargetIds.Contains(targetId))
            {
                processedTargetIds.Add(targetId);
                damageableTarget.ApplyDamage(damage);
                // クールダウンを待たずに即終了
                attackCooldownRoutines.Remove(targetId);
                yield break;
            }

            // 2回目以降はクールダウンを待つ
            damageableTarget.ApplyDamage(damage);
            yield return new WaitForSeconds(interval);
            attackCooldownRoutines.Remove(targetId);
        }

        public int GetDamageableTargetId(IDamageable damageableTarget)
        {
            Object damageableObject = damageableTarget as Object;
            if (damageableObject == null) return 0;

            return damageableObject.GetInstanceID();
        }

        void OnDisable()
        {
            // 攻撃のクールダウン中にオブジェクトが無効化された場合は、対象ごとのクールダウンをすべてキャンセルする
            foreach (Coroutine cooldownRoutine in attackCooldownRoutines.Values)
            {
                if (cooldownRoutine == null) continue;
                StopCoroutine(cooldownRoutine);
            }

            attackCooldownRoutines.Clear();
            processedTargetIds.Clear();
        }
    }
}