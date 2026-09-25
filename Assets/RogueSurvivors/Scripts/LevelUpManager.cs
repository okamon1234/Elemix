using System.Collections.Generic;
using UnityEngine;
namespace RogueSurvivors
{
    public sealed class LevelUpManager : MonoBehaviour
    {
        PlayerStats stats;
        int pending;
        List<UpgradeOption> offered;
        void OnEnable()
        {
            if (!stats) stats = GetComponent<PlayerStats>();
            if (stats) { stats.LeveledUp -= Queue; stats.LeveledUp += Queue; }
        }
        void OnDisable() { if (stats) stats.LeveledUp -= Queue; }
        public void Bind(PlayerStats player)
        {
            if (stats) stats.LeveledUp -= Queue;
            stats = player; stats.LeveledUp -= Queue; stats.LeveledUp += Queue;
        }
        void Queue() { pending++; }
        void Update()
        {
            if (offered != null && offered.Count > 0 && stats && HUDController.Instance && !HUDController.Instance.LevelUI.HasSelectionCallback)
                HUDController.Instance.LevelUI.Show(offered, Choose, stats.Level);
            if (pending > 0 && offered == null && stats && GameManager.Instance && HUDController.Instance && GameManager.Instance.IsPlaying) Open();
        }
        void Open()
        {
            var pool = new List<UpgradeOption>();
            foreach (var definition in WeaponCatalog.All) {
                var weapon = WeaponCatalog.Get(stats, definition.Kind);
                if (weapon && PhysicalEvolution.DisplayLevel(weapon) < 8 && stats.CanAcquire(weapon)) pool.Add(new UpgradeOption(definition.Kind, PhysicalEvolution.Name(weapon) + " Lv." + PhysicalEvolution.DisplayLevel(weapon) + " → " + (PhysicalEvolution.DisplayLevel(weapon)+1), WeaponCatalog.UpgradeDescription(weapon) + (PhysicalEvolution.Active(weapon)?"\n合体武器は1枠／素材の通常攻撃は停止":PhysicalEvolution.Hint(definition.Id))));
            }
            pool.Add(new UpgradeOption(UpgradeKind.Power, "攻撃力アップ", "攻撃力を基礎値の20%分強化\nすべての武器に有効"));
            pool.Add(new UpgradeOption(UpgradeKind.Speed, "移動速度アップ", "移動速度＋0.4\n敵の群れから抜け出しやすくなる"));
            pool.Add(new UpgradeOption(UpgradeKind.Health, "体力アップ", "最大HP＋20\n同時にHPを20回復"));
            pool.Add(new UpgradeOption(UpgradeKind.Pickup, "取得範囲アップ", "取得範囲＋0.6\n遠くの経験値を集めやすくなる"));
            if (stats.Regeneration < 2) pool.Add(new UpgradeOption(UpgradeKind.Regeneration, "自動回復", "5秒間ダメージを受けないと毎秒HP回復\n回復量＋0.35／秒・上限2／秒"));
            offered = new List<UpgradeOption>();
            var owned = pool.FindAll(option => { var weapon = WeaponCatalog.Get(stats, option.Kind); return weapon && weapon.Level > 0; });
            if (owned.Count > 0) { var option = owned[Random.Range(0, owned.Count)]; offered.Add(option); pool.Remove(option); }
            while (offered.Count < 3) {
                float total = 0;
                foreach (var option in pool) { var weapon = WeaponCatalog.Get(stats, option.Kind); total += weapon ? OfferWeight(weapon) : 1.5f; }
                float roll = Random.value * total; int index = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++) { var weapon = WeaponCatalog.Get(stats, pool[i].Kind); roll -= weapon ? OfferWeight(weapon) : 1.5f; if (roll <= 0) { index = i; break; } }
                offered.Add(pool[index]); pool.RemoveAt(index);
            }
            GameManager.Instance.ChoosingUpgrade = true;
            if (!NetworkManager.InRoom) Time.timeScale = 0;
            HUDController.Instance.LevelUI.Show(offered, Choose, stats.Level);
            EffectsService.Instance?.Play("Level");
        }
        float OfferWeight(WeaponBase weapon) => WeaponCatalog.OfferWeight(stats.CharacterId, weapon.Id) * (weapon.Level > 0 ? 1.35f : 1) * PhysicalEvolution.PartnerWeight(stats,weapon.Id);
        public void Choose(int index)
        {
            if (offered == null || index < 0 || index >= offered.Count || !GameManager.Instance.IsPlaying) return;
            stats.RecordUpgrade(offered[index].Kind); offered[index].Apply(stats); offered = null; pending--;
            HUDController.Instance.LevelUI.Hide();
            if (pending == 0) { GameManager.Instance.ChoosingUpgrade = false; Time.timeScale = 1; }
        }
        public void CancelChoices()
        {
            pending = 0; offered = null; HUDController.Instance?.LevelUI.Hide();
            if (GameManager.Instance) GameManager.Instance.ChoosingUpgrade = false;
            Time.timeScale = 1;
        }
        void OnDestroy()
        {
            if (stats) stats.LeveledUp -= Queue;
            if (GameManager.Instance) GameManager.Instance.ChoosingUpgrade = false;
            Time.timeScale = 1;
        }
    }
}
