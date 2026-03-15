namespace Common
{
    using Common.Effect;
    using UnityEngine;
    using Unity.Netcode;

    public abstract class HealthBase : NetworkBehaviour, IDamageable
    {
        [SerializeField] protected int maxHealth = 1;
        [SerializeField] Die dieEffectPrefab;
        protected NetworkVariable<int> currentHealth = new NetworkVariable<int>();

        public int MaxHealth => maxHealth;
        public int CurrentHealth => IsSpawned ? currentHealth.Value : maxHealth;
        public bool IsDead { get; private set; } = false;

        void Awake()
        {
            Initialize();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer)
            {
                return;
            }

            currentHealth.Value = maxHealth;
        }

        /// <summary>
        /// Awake で呼び出される初期化処理。
        /// HealthBase では特に何も行わないが、派生クラスで必要に応じてオーバーライドして使用する。
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// IDamageable インターフェースの TakeDamage メソッドの実装。
        /// </summary>
        public abstract void TakeDamage(int damage);

        /// <summary>
        /// 死亡状態へ遷移する共通処理。
        /// ここでは死亡フラグの更新と死亡演出の通知だけを担当し、NetworkObject の Despawn は行わない。
        /// Despawn はサーバー権限でしか実行できないため、派生クラス側でサーバー判定付きで行う。
        /// </summary>
        public virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;

            PlayDieEffectClientRpc();

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                // ネットワーク生成された敵はサーバーから Despawn し、全クライアントに削除を伝播させる。
                NetworkObject.Despawn(true);
                return;
            }
        }

        /// <summary>
        /// 死亡エフェクトを再生するためのRPC。サーバーから全クライアントに向けて呼び出される。
        /// この RPC では見た目の再生だけを行い、NetworkObject の Despawn には触れない。
        /// 参加者クライアントで Despawn を呼ぶと「Only server can despawn objects」になるため。
        /// </summary>
        /// <param name="position"></param>
        [ClientRpc]
        void PlayDieEffectClientRpc()
        {
            PlayDieEffect();
        }

        /// <summary>
        /// 各端末ローカルで死亡エフェクトを生成する。
        /// エフェクト prefab 自体はネットワーク同期オブジェクトである必要はなく、各クライアントで同じ演出を個別生成すればよい。
        /// </summary>
        void PlayDieEffect()
        {
            if (dieEffectPrefab == null) return;
            Instantiate(dieEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}