namespace Common
{
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Netcode;
    using UI;
    using Common.Effect;

    public abstract class HealthBase : NetworkBehaviour, IDamageable
    {
        protected NetworkVariable<int> currentHealth = new NetworkVariable<int>();
        protected NetworkVariable<int> maxHealth = new NetworkVariable<int>();
        readonly Dictionary<string, int> maxHealthFlatModifiers = new Dictionary<string, int>();
        readonly Dictionary<string, float> maxHealthPercentModifiers = new Dictionary<string, float>();
        public event System.Action HealthStateChanged;
        protected int defaultMaxHealth;
        Die dieEffectPrefab;

        public int MaxHealth
        {
            get => IsSpawned ? maxHealth.Value : 1;
            protected set => maxHealth.Value = value;
        }
        public Vector2 GetPosition()
        {
            if(!IsSpawned)
            {
                return Vector2.zero;
            }
            return transform.position;
        }
        public int CurrentHealth => IsSpawned ? currentHealth.Value : MaxHealth;
        public bool IsDead { get; private set; } = false;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            currentHealth.OnValueChanged += OnHealthValueChanged;
            maxHealth.OnValueChanged += OnMaxHealthValueChanged;

            if (!IsServer) return;  // サーバーでなければ、以降の処理をスキップ

            // スポーン直後は基礎値と現在有効な Modifier をまとめて反映し、初期体力を最大値へ揃える。
            RecalculateMaxHealth(resetCurrentHealth: true);
        }

        public override void OnNetworkDespawn()
        {
            currentHealth.OnValueChanged -= OnHealthValueChanged;
            maxHealth.OnValueChanged -= OnMaxHealthValueChanged;
            base.OnNetworkDespawn();
        }

        void OnHealthValueChanged(int previousValue, int newValue)
        {
            HealthStateChanged?.Invoke();
        }

        void OnMaxHealthValueChanged(int previousValue, int newValue)
        {
            HealthStateChanged?.Invoke();
        }

        /// <summary>
        /// Awake で呼び出される初期化処理。
        /// HealthBase では特に何も行わないが、派生クラスで必要に応じてオーバーライドして使用する。
        /// </summary>
        protected void Initialize(int initialMaxHealth, Die dieEffectPrefab)
        {
            defaultMaxHealth = initialMaxHealth;
            this.dieEffectPrefab = dieEffectPrefab;
        }

        /// <summary>
        /// IDamageable インターフェースの TakeDamage メソッドの実装。
        /// </summary>
        protected abstract void TakeDamageInternal(int damage);

        public void ApplyDamage(int damage)
        {
            if (!IsServer) return;
            if (!IsSpawned) return;
            if (IsDead) return;

            TakeDamageInternal(damage);
        }

        /// <summary>
        /// 最大体力に対する固定値 Modifier を設定する。
        /// sourceId 単位で上書きすることで、同じ効果の再計算時に二重加算を防ぐ。
        /// </summary>
        public void SetMaxHealthFlatModifier(string sourceId, int value)
        {
            if (!IsServer || !IsSpawned) return;
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("最大体力の固定値 Modifier に空の sourceId は使用できません。");
                return;
            }

            maxHealthFlatModifiers[sourceId] = value;
            RecalculateMaxHealth();
        }

        /// <summary>
        /// 最大体力に対する割合 Modifier を設定する。
        /// 永続アップグレード、一時バフ、デバフを同じ仕組みで扱えるようにするための入口。
        /// </summary>
        public void SetMaxHealthPercentModifier(string sourceId, float value)
        {
            if (!IsServer || !IsSpawned) return;
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("最大体力の割合 Modifier に空の sourceId は使用できません。");
                return;
            }

            maxHealthPercentModifiers[sourceId] = value;
            RecalculateMaxHealth();
        }

        /// <summary>
        /// sourceId 指定の Modifier をまとめて解除する。
        /// 一時効果の終了やリセット処理を同じ API で扱えるようにしておく。
        /// </summary>
        public void RemoveMaxHealthModifier(string sourceId)
        {
            if (!IsServer || !IsSpawned) return;
            if (string.IsNullOrWhiteSpace(sourceId)) return;

            bool removedFlat = maxHealthFlatModifiers.Remove(sourceId);
            bool removedPercent = maxHealthPercentModifiers.Remove(sourceId);

            if (removedFlat || removedPercent)
            {
                RecalculateMaxHealth();
            }
        }

        /// <summary>
        /// 現在有効な Modifier から最大体力を再構築する。
        /// アップグレード側は最終値を直接書き換えず、Modifier を登録するだけにすると責務が明確になる。
        /// </summary>
        protected void RecalculateMaxHealth(bool resetCurrentHealth = false)
        {
            int recalculatedMaxHealth = CalculateMaxHealth();
            MaxHealth = recalculatedMaxHealth;

            if (resetCurrentHealth)
            {
                currentHealth.Value = MaxHealth;
                return;
            }

            // 既存挙動を大きく変えないため、現在体力は自動回復させず、最大値超過だけを丸める。
            if (currentHealth.Value > MaxHealth)
            {
                currentHealth.Value = MaxHealth;
            }
        }

        /// <summary>
        /// 最大体力の最終値計算。
        /// 計算式を1箇所へ集約しておくと、将来の体力関連アップグレード追加時にも変更点が限定される。
        /// </summary>
        protected virtual int CalculateMaxHealth()
        {
            int flatBonus = 0;
            foreach (int value in maxHealthFlatModifiers.Values)
            {
                flatBonus += value;
            }

            float percentBonus = 0f;
            foreach (float value in maxHealthPercentModifiers.Values)
            {
                percentBonus += value;
            }

            int baseHealthWithFlatBonus = Mathf.Max(1, defaultMaxHealth + flatBonus);
            return Mathf.Max(1, Mathf.RoundToInt(baseHealthWithFlatBonus * (1f + percentBonus)));
        }

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
            Destroy(gameObject);
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

        
        [ClientRpc]
        protected void ShowApplyDamageUIClientRpc(Vector3 position, int damage)
        {
            if (WorldCanvasManager.I == null)
            {
                return;
            }

            WorldCanvasManager.I.ShowApplyDamageUI(position, damage);
        }
    }
}