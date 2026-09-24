using UnityEngine;
#if ROGUE_FUSION
using Fusion;
#endif
namespace RogueSurvivors
{
    public sealed class NetworkPlayerSync :
#if ROGUE_FUSION
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        public bool IsLocalAuthority
        {
            get {
#if ROGUE_FUSION
                return Object && Object.IsValid && Object.HasStateAuthority;
#else
                return true;
#endif
            }
        }
        public void BroadcastShot(Vector2 direction, int pierce)
        {
#if ROGUE_FUSION
            if (IsLocalAuthority) RPC_ShowShot(direction, pierce);
#endif
        }
        public void PublishBuild()
        {
#if ROGUE_FUSION
            if (!IsLocalAuthority) return;
            string json = JsonUtility.ToJson(GetComponent<PlayerStats>().Capture().NetworkCopy());
            WriteBuild(json); appliedBuild = json; HealthValue = GetComponent<PlayerHealth>().Current;
#endif
        }
        public void BroadcastEffect(int kind, Vector2 start, Vector2 end)
        {
#if ROGUE_FUSION
            if (IsLocalAuthority) RPC_WeaponEffect(kind, start, end);
#endif
        }
#if ROGUE_FUSION
        [Networked] public NetworkString<_512> BuildJson { get; set; }
        [Networked] public NetworkString<_512> BuildTail { get; set; }
        string ReadBuild() => BuildJson.ToString() + BuildTail.ToString();
        void WriteBuild(string json)
        {
            if (json.Length > 1024) throw new System.InvalidOperationException("Build exceeds network capacity");
            BuildJson = json.Substring(0, Mathf.Min(512, json.Length));
            BuildTail = json.Length > 512 ? json.Substring(512) : "";
        }
        [Networked] public NetworkBool BarrierValue { get; set; }
        [Networked] public float HealthValue { get; set; }
        [Networked] public NetworkBool Moving { get; set; }
        [Networked] public NetworkBool FacingLeft { get; set; }
        PlayerHealth health;
        string appliedBuild;
        PlayerDataData initialBuild;
        public bool BuildReady => !string.IsNullOrEmpty(appliedBuild);
        public void ConfigureBuild(PlayerDataData build)
        {
            initialBuild = build;
            WriteBuild(JsonUtility.ToJson(build.NetworkCopy())); HealthValue = build.maxHealth;
        }
        public override void Spawned()
        {
            health = GetComponent<PlayerHealth>();
            health.IsLocal = Object.HasStateAuthority;
            GetComponent<NetworkTransform>().DisableSharedModeInterpolation = true;
            if (health.IsLocal && initialBuild != null) {
                GetComponent<PlayerStats>().Restore(initialBuild); appliedBuild = ReadBuild(); initialBuild = null;
            } else ApplyBuild();
            GetComponent<PlayerMovement>().enabled = health.IsLocal;
            GetComponentInChildren<PlayerCollector>(true).gameObject.SetActive(health.IsLocal);
            if (health.IsLocal) SceneBootstrap.BindLocal(health, false);
            else
            {
                var body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.None;
                body.useFullKinematicContacts = true;
            }
            if (Object.HasStateAuthority) Runner.SetPlayerObject(Runner.LocalPlayer, Object);
        }
        void ApplyBuild()
        {
            string json = ReadBuild();
            if (string.IsNullOrEmpty(json) || json == appliedBuild) return;
            var build = JsonUtility.FromJson<PlayerDataData>(json);
            if (build == null || !build.IsValid()) return;
            GetComponent<PlayerStats>().Restore(build);
            appliedBuild = json;
        }
        public override void FixedUpdateNetwork()
        {
            if (!health) return;
            if (Object.HasStateAuthority)
            {
                HealthValue = health.Current; BarrierValue = health.HasBarrier;
                Moving = GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > .01f;
                FacingLeft = GetComponentInChildren<SpriteRenderer>().flipX;
            }
        }
        public override void Render()
        {
            if (!health) return;
            ApplyBuild();
            if (!Object.HasStateAuthority)
            {
                health.SetRemoteHealth(HealthValue); health.SetRemoteBarrier(BarrierValue);
                GetComponentInChildren<Animator>().SetBool("IsMoving", Moving);
                GetComponentInChildren<SpriteRenderer>().flipX = FacingLeft;
            }
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        void RPC_WeaponEffect(int kind, Vector2 start, Vector2 end)
        {
            if (kind >= 1000 && kind < 2000) { int code=kind-1000; StaffCast.Create((CombatElement)(code/100),start,end,(code%100)/2,0,null,(code%2)==1); return; }
            if (kind == 0) GetComponent<FireballWeapon>()?.SpawnAt(start, (end - start).normalized, true);
            if (kind == 1) CombatVisuals.Lightning(start, end);
            if (kind == 2) CombatVisuals.Spear(start, end);
            if (kind >= 30 && kind <= 36) PhysicalWeapon.Show(kind - 30, start, end);
            if (kind >= 11 && kind <= 19) ElementWeapon.Show((CombatElement)(kind - 10), start, end);
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        void RPC_ShowShot(Vector2 direction, int pierce)
        {
            var prefab = GetComponent<ProjectileWeapon>().bulletPrefab;
            Instantiate(prefab, transform.position, Quaternion.identity).Launch(direction, 0, 12, pierce, false, null, true);
        }
#endif
    }
}
