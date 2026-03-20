namespace PlayerSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;
    
    public class Attack : MonoBehaviour
    {
        [SerializeField] PlayerConfig playerConfig;
        public readonly Dictionary<int, Coroutine> attackCooldownRoutines = new Dictionary<int, Coroutine>();
        public readonly HashSet<int> processedTargetIds = new HashSet<int>();
        public bool CanProcessAttack => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
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

            damageableTarget.ApplyDamage(playerConfig.Link.strength);     // ダメージを与える
            yield return new WaitForSeconds(playerConfig.Link.attackIntarval);
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