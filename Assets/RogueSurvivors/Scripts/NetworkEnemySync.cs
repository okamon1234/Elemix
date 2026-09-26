using UnityEngine;
#if ROGUE_FUSION
using Fusion;
#endif
namespace RogueSurvivors
{
    public sealed class NetworkEnemySync :
#if ROGUE_FUSION
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        public bool IsNetworked
        {
            get {
#if ROGUE_FUSION
                return Object && Object.IsValid;
#else
                return false;
#endif
            }
        }
        public bool IsAuthority
        {
            get {
#if ROGUE_FUSION
                return IsNetworked ? Object.HasStateAuthority : !NetworkManager.InRoom;
#else
                return true;
#endif
            }
        }
        public void RequestDamage(float amount, PlayerHealth source, CombatElement element = CombatElement.None)
        {
#if ROGUE_FUSION
            if (!IsNetworked || !source || !source.IsLocal) return;
            var playerObject = source.GetComponent<NetworkObject>();
            if (playerObject && playerObject.IsValid && playerObject.HasStateAuthority) RPC_RequestDamage(amount, playerObject.Id, (int)element);
#endif
        }
        public void GiveCrystal(PlayerHealth player)
        {
#if ROGUE_FUSION
            if (IsNetworked && IsAuthority && player && player.GetComponent<NetworkObject>()) RPC_Crystal(player.GetComponent<NetworkObject>().Id);
#endif
        }
        public void BroadcastReaction(int element, int spread = 0)
        {
#if ROGUE_FUSION
            if (IsNetworked && IsAuthority) RPC_Reaction(element, spread);
#endif
        }
        public void BroadcastVolley(float angle, int count, float speed, float damage)
        {
#if ROGUE_FUSION
            if (IsNetworked && IsAuthority) RPC_Volley(angle, count, speed, damage);
#endif
        }
        public void BroadcastAttack(int attack, Vector2 origin, Vector2 target, int phase, float damage, int mask)
        {
#if ROGUE_FUSION
            if (IsNetworked && IsAuthority) RPC_Attack(attack, origin, target, phase, damage, mask);
#endif
        }
        public void AnnounceDefeat() => SetResult(1);
        public void AnnounceFailure() => SetResult(2);
        void SetResult(int result)
        {
#if ROGUE_FUSION
            if (IsNetworked && IsAuthority && Result == 0) Result = result;
#endif
        }
#if ROGUE_FUSION
        [Networked] public float HealthValue { get; set; }
        [Networked] public float MaximumHealth { get; set; }
        [Networked] public int StateValue { get; set; }
        [Networked] public float RemainingTime { get; set; }
        [Networked] public Vector2 DashHeading { get; set; }
        [Networked] public int AttackIndex { get; set; }
        [Networked] public int Result { get; set; }
        [Networked] public int TeamLevel { get; set; }
        [Networked] public float DamageScale { get; set; }
        [Networked] public float AttackRate { get; set; }
        [Networked] public Vector4 PartHealth { get; set; }
        [Networked] public float PartMaximum { get; set; }
        [Networked] public int PartBreakOrder { get; set; }
        [Networked] public Vector4 BreakRewardState { get; set; }
        [Networked] public Vector3 BloomState { get; set; }
        [Networked] public NetworkId BloomSource { get; set; }
        [Networked] public int ElementAura { get; set; }
        [Networked] public float ElementRemaining { get; set; }
        [Networked] public int BossType { get; set; }
        [Networked] public int EncounterPhase { get; set; }
        [Networked] public int PlannedAttack { get; set; }
        [Networked] public Vector2 AimPoint { get; set; }
        [Networked] public int ChargesLeft { get; set; }
        EnemyHealth health;
        BossAI ai;
        bool wasAuthority;
        public override void Spawned()
        {
            health = GetComponent<EnemyHealth>(); ai = GetComponent<BossAI>();
            GetComponent<NetworkTransform>().DisableSharedModeInterpolation = true;
            wasAuthority = IsAuthority;
            if (IsAuthority)
            {
                ai.ConfigureDifficulty(FindObjectsByType<PlayerStats>(FindObjectsSortMode.None));
                var difficulty = GetComponent<BossDifficulty>();
                TeamLevel = difficulty.TeamLevel; DamageScale = difficulty.DamageScale; AttackRate = difficulty.AttackRate;
                HealthValue = health.Current; MaximumHealth = health.maximum;
                var parts = GetComponent<BossParts>(); PartHealth = parts.Health; PartMaximum = parts.Maximum; PartBreakOrder = parts.BreakOrder; BreakRewardState=GetComponent<BossBreakReward>().Capture();
                var bloom = GetComponent<ReactionDamage>();
                if(bloom) { BloomState = bloom.CaptureBloom(); var owner=bloom.BloomOwner ? bloom.BloomOwner.GetComponent<NetworkObject>() : null; BloomSource=owner && owner.IsValid?owner.Id:default; }
                var reaction = GetComponent<ElementReaction>(); ElementAura = (int)reaction.Aura; ElementRemaining = reaction.Remaining;
                BossType = (int)ai.Kind; EncounterPhase = ai.Phase; PlannedAttack = (int)ai.PendingAttack; AimPoint = ai.AimPoint; ChargesLeft = ai.ChargesLeft;
                StateValue = (int)ai.State; RemainingTime = ai.Remaining;
            }
        }
        public override void FixedUpdateNetwork()
        {
            if (!health) return;
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = IsAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.interpolation = IsAuthority ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
            body.useFullKinematicContacts = true;
            if (IsAuthority && !wasAuthority)
            {
                health.SetSynchronizedHealth(HealthValue, MaximumHealth);
                GetComponent<BossParts>().Restore(PartHealth, PartMaximum, PartBreakOrder);
                GetComponent<BossBreakReward>().Restore(BreakRewardState);
                var bloom = GetComponent<ReactionDamage>(); if(!bloom) bloom=gameObject.AddComponent<ReactionDamage>();
                bloom.RestoreBloom(BloomState,Runner.TryFindObject(BloomSource,out var bloomOwner)?bloomOwner.GetComponent<PlayerHealth>():null);
                GetComponent<ElementReaction>().Restore(ElementAura, ElementRemaining);
                ai.RestoreEncounter(BossType, EncounterPhase, PlannedAttack, AimPoint, ChargesLeft);
                ai.RestoreState(StateValue, RemainingTime, DashHeading, AttackIndex);
            }
            if (IsAuthority)
            {
                HealthValue = health.Current; MaximumHealth = health.maximum;
                var parts = GetComponent<BossParts>(); PartHealth = parts.Health; PartMaximum = parts.Maximum; PartBreakOrder = parts.BreakOrder; BreakRewardState=GetComponent<BossBreakReward>().Capture();
                var bloom = GetComponent<ReactionDamage>();
                if(bloom) { BloomState = bloom.CaptureBloom(); var owner=bloom.BloomOwner ? bloom.BloomOwner.GetComponent<NetworkObject>() : null; BloomSource=owner && owner.IsValid?owner.Id:default; }
                var reaction = GetComponent<ElementReaction>(); ElementAura = (int)reaction.Aura; ElementRemaining = reaction.Remaining;
                BossType = (int)ai.Kind; EncounterPhase = ai.Phase; PlannedAttack = (int)ai.PendingAttack; AimPoint = ai.AimPoint; ChargesLeft = ai.ChargesLeft;
                StateValue = (int)ai.State; RemainingTime = ai.Remaining; DashHeading = ai.Heading; AttackIndex = ai.AttackIndex;
            }
            wasAuthority = IsAuthority;
        }
        public override void Render()
        {
            if (!health) return;
            GetComponent<BossDifficulty>().ApplyNetwork(TeamLevel, DamageScale, AttackRate);
            if (!IsAuthority)
            {
                health.SetSynchronizedHealth(HealthValue, MaximumHealth);
                GetComponent<BossParts>().Restore(PartHealth, PartMaximum, PartBreakOrder);
                GetComponent<BossBreakReward>().Restore(BreakRewardState);
                var bloom = GetComponent<ReactionDamage>(); if(!bloom) bloom=gameObject.AddComponent<ReactionDamage>();
                bloom.RestoreBloom(BloomState,Runner.TryFindObject(BloomSource,out var bloomOwner)?bloomOwner.GetComponent<PlayerHealth>():null);
                GetComponent<ElementReaction>().Restore(ElementAura, ElementRemaining);
                ai.RestoreEncounter(BossType, EncounterPhase, PlannedAttack, AimPoint, ChargesLeft);
                ai.RestoreState(StateValue, RemainingTime, DashHeading, AttackIndex);
            }
            if (Result != 0 && GameManager.Instance) GameManager.Instance.Finish(Result == 1);
        }
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_RequestDamage(float amount, NetworkId sourceId, int element, RpcInfo info = default)
        {
            if (!GameManager.Instance.IsPlaying || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            if (!Runner.TryFindObject(sourceId, out var source)) return;
            if (Runner.GameMode == GameMode.Single ? !source.HasStateAuthority : source.StateAuthority != info.Source) return;
            var player = source.GetComponent<PlayerHealth>();
            if (!player || !player.Alive || Vector3.Distance(source.transform.position, transform.position) > 25) return;
            float maximum = 240 * player.GetComponent<PlayerStats>().DamageMultiplier;
            if (element < 0 || element > (int)CombatElement.Earth) return;
            health.ReceiveHit(Mathf.Clamp(amount, 0, maximum), Vector2.zero, player, (CombatElement)element);
            PartHealth = GetComponent<BossParts>().Health;
            HealthValue = health.Current;
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_Attack(int attack, Vector2 origin, Vector2 target, int phase, float damage, int mask)
            => GetComponent<BossAttackDirector>().Execute((BossAttack)attack, origin, target, phase, damage, mask);
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_Crystal(NetworkId playerId)
        {
            if (Runner.TryFindObject(playerId, out var obj)) { var player = obj.GetComponent<PlayerHealth>(); if (player) CrystalPickup.Spawn((Vector2)transform.position+((Vector2)player.transform.position-(Vector2)transform.position).normalized*1.9f,player); }
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_Reaction(int element, int spread) => GetComponent<ElementReaction>().Show(element, (CombatElement)spread);
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_Volley(float angle, int count, float speed, float damage) => GetComponent<BossAI>().SpawnVolley(angle, count, speed, damage);
#endif
    }
}
