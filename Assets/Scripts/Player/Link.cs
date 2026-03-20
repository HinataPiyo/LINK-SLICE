namespace PlayerSystem.Link
{
    using System.Collections;
    using System.Collections.Generic;

    using Common;
    using Unity.Netcode;
    using UnityEngine;

    /// <summary>
    /// プレイヤーとプレイヤーをつなぐ線を描画するクラス
    /// </summary>
    public class Link : NetworkBehaviour
    {
        [SerializeField] PlayerConfig playerConfig;

        LineRenderer lineRenderer;
        Transform target;
        LinkEffect linkEffect;
        float currentLineWidth;
        float widthLerp01;
        bool isLinkActive;
        bool isBreaking;
        bool notifiedBroken;

        const float WidthEpsilon = 0.0001f;

        public bool IsBreakFinished => isBreaking && notifiedBroken;
        readonly Dictionary<int, Coroutine> attackCooldownRoutines = new Dictionary<int, Coroutine>();
        readonly HashSet<int> processedTargetIds = new HashSet<int>();

        bool CanProcessAttack => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        /// <summary>
        /// 外部から接続先ターゲットを設定する
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (newTarget != null && isBreaking)
            {
                CancelBreak();
            }
        }

        /// <summary>
        /// リンク切断を開始する（幅0到達後にLinkBrokenを通知）
        /// </summary>
        public void BeginBreak()
        {
            if (isBreaking)
            {
                return;
            }

            isBreaking = true;
            notifiedBroken = false;
            linkEffect.BeginFadeOut();      // 切断開始と同時にエフェクトのフェードアウトも始める
        }

        /// <summary>
        /// 切断開始後、線の幅が十分に小さくなり
        /// かつエフェクトのフェードアウトが完了したタイミングで切断完了とみなす
        /// </summary>
        void CancelBreak()
        {
            isBreaking = false;
            notifiedBroken = false;
            linkEffect.EnsurePlaying();
        }

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            linkEffect = GetComponentInChildren<LinkEffect>();

            currentLineWidth = 0f;
            widthLerp01 = 0f;

            lineRenderer.startWidth = 0f;
            lineRenderer.endWidth = 0f;
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        void Update()
        {
            if (lineRenderer == null) return;

            isLinkActive = target != null && !isBreaking;

            // enabledの瞬間切り替えではなく、幅を補間して自然に表示/非表示する
            float safeDeltaTime = Mathf.Min(Time.deltaTime, playerConfig.Link.maxAnimationDeltaTime);     // フレーム落ちなどで極端に大きなdeltaTimeが入るのを防止
            float duration = isLinkActive ? Mathf.Max(0.0001f, playerConfig.Link.growDuration) : Mathf.Max(0.0001f, playerConfig.Link.shrinkDuration);  // 0除算防止
            float step = safeDeltaTime / duration;          // 1秒間にstepが1以上になるように補正する（フレーム落ちしても数秒で完全に表示/非表示になるように）
            float targetLerp = isLinkActive ? 1f : 0f;      // 目標の幅に向かってwidthLerp01を補間する
            widthLerp01 = Mathf.MoveTowards(widthLerp01, targetLerp, step);     // 補間値から現在の線の幅を計算する
            currentLineWidth = playerConfig.Link.maxLineWidth * widthLerp01;      // 線の幅を更新し、幅が十分に大きいときだけ線を表示する

            // 接続先が存在し、かつ切断中でない間だけリンクを有効にする
            if (target != null)
            {
                Vector3 selfPos = transform.position;
                Vector3 targetPos = target.position;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, selfPos);
                lineRenderer.SetPosition(1, targetPos);

                Vector2 center = (selfPos + targetPos) * 0.5f;
                float angle = Mathf.Atan2(targetPos.y - selfPos.y, targetPos.x - selfPos.x) * Mathf.Rad2Deg;
                float length = Vector2.Distance(selfPos, targetPos);
                linkEffect.UpdateVisual(center, angle, length);

                if (CanProcessAttack)
                {
                    GetHitCollider(center, angle, length);      // 毎フレーム、リンクの見た目の太さに合わせた帯状の当たり判定を更新する
                }
            }
            else
            {
                lineRenderer.positionCount = 0;
            }

            if (isLinkActive)
            {
                PlayLinkEffect();       // 接続中は常にエフェクトを再生する
            }

            // 線の幅を更新し、幅が十分に大きいときだけ線を表示する
            lineRenderer.startWidth = currentLineWidth;
            lineRenderer.endWidth = currentLineWidth;
            lineRenderer.enabled = currentLineWidth > WidthEpsilon;

            // 切断開始後、線の幅が十分に小さくなり、かつエフェクトのフェードアウトが完了したタイミングで切断完了とみなす
            if (isBreaking && !notifiedBroken && currentLineWidth <= WidthEpsilon && linkEffect.IsFadeOutFinished)
            {
                notifiedBroken = true;
            }
        }

        /// <summary>
        /// 線のエフェクトを再生する
        /// </summary>
        public void PlayLinkEffect()
        {
            if (!isLinkActive) return;

            linkEffect.EnsurePlaying();
        }

        /// <summary>
        /// 敵を攻撃するための当たり判定を取得する
        /// </summary>
        /// <param name="center">攻撃判定の中心座標</param>
        /// <param name="angle"> 攻撃の角度（度数法）</param>
        /// <param name="distance"> 攻撃の距離 </param>
        public void GetHitCollider(Vector2 center, float angle, float distance)
        {
            if (!TryGetHitBox(center, angle, distance, out Vector2 hitSize)) return;

            RaycastHit2D[] hits = Physics2D.BoxCastAll(center, hitSize, angle, Vector2.zero, 0f, playerConfig.Link.targetLayer);

            Vector2 right = Quaternion.Euler(0f, 0f, angle) * Vector2.right * (hitSize.x * 0.5f);
            Vector2 up = Quaternion.Euler(0f, 0f, angle) * Vector2.up * (hitSize.y * 0.5f);
            Debug.DrawLine(center - right - up, center + right - up, Color.red, 0.1f);
            Debug.DrawLine(center + right - up, center + right + up, Color.red, 0.1f);
            Debug.DrawLine(center + right + up, center - right + up, Color.red, 0.1f);
            Debug.DrawLine(center - right + up, center - right - up, Color.red, 0.1f);

            processedTargetIds.Clear();

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null) continue;
                if (!hit.collider.TryGetComponent(out IDamageable damageableTarget)) continue;

                int targetId = GetDamageableTargetId(damageableTarget);
                if (targetId == 0) continue;
                if (!processedTargetIds.Add(targetId)) continue;

                OnAttack(damageableTarget, targetId);
            }
        }

#region Debug
    

        void OnDrawGizmosSelected()
        {
            if (!TryGetGizmoHitBox(out Vector2 center, out float angle, out Vector2 hitSize)) return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
            Gizmos.DrawCube(Vector3.zero, hitSize);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
            Gizmos.DrawWireCube(Vector3.zero, hitSize);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        bool TryGetGizmoHitBox(out Vector2 center, out float angle, out Vector2 hitSize)
        {
            center = default;
            angle = 0f;
            hitSize = default;

            if (target != null)
            {
                Vector2 selfPos = transform.position;
                Vector2 targetPos = target.position;
                center = (selfPos + targetPos) * 0.5f;
                angle = Mathf.Atan2(targetPos.y - selfPos.y, targetPos.x - selfPos.x) * Mathf.Rad2Deg;
                float distance = Vector2.Distance(selfPos, targetPos);
                return TryGetHitBox(center, angle, distance, out hitSize);
            }

            LineRenderer currentLineRenderer = lineRenderer != null ? lineRenderer : GetComponent<LineRenderer>();
            if (currentLineRenderer == null || currentLineRenderer.positionCount < 2) return false;

            Vector2 start = currentLineRenderer.GetPosition(0);
            Vector2 end = currentLineRenderer.GetPosition(1);
            center = (start + end) * 0.5f;
            angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float lineDistance = Vector2.Distance(start, end);

            return TryGetHitBox(center, angle, lineDistance, out hitSize);
        }

        bool TryGetHitBox(Vector2 center, float angle, float distance, out Vector2 hitSize)
        {
            hitSize = default;

            if (playerConfig == null) return false;

            float visibleWidth = Application.isPlaying ? currentLineWidth : playerConfig.Link.maxLineWidth;
            float hitThickness = Mathf.Max(visibleWidth, WidthEpsilon);
            if (distance <= 0f || hitThickness <= WidthEpsilon) return false;

            hitSize = new Vector2(distance + hitThickness, hitThickness);
            return true;
        }
        
#endregion
        /// <summary>
        /// 攻撃処理。Healthコンポーネントを持つオブジェクトに対してダメージを与える
        /// </summary>
        void OnAttack(IDamageable damageableTarget, int targetId)
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

        int GetDamageableTargetId(IDamageable damageableTarget)
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