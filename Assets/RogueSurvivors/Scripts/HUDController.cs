using UnityEngine;
using UnityEngine.UI;
namespace RogueSurvivors
{
    public sealed class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }
        public LevelUpUI LevelUI { get; private set; }
        PlayerHealth player;
        Image hp, xp, bossHp;
        Text healthText, levelText, timer, kills, weapons, toast, bossLabel, partsLabel, recoveryLabel;
        float toastUntil;
        GameObject result;
        void OnEnable() => Instance = this;
        void Awake()
        {
            Instance = this; LevelUI = gameObject.AddComponent<LevelUpUI>();
            gameObject.AddComponent<BossMinimap>();
            recoveryLabel = UIFactory.Label("回復", transform, Vector2.up, new Vector2(24,-154), new Vector2(330,54), "", 15, new Color(.4f,1,.65f));
            var panel = UIFactory.Panel("Status", transform, Vector2.up, new Vector2(24, -24), new Vector2(320, 124), UIFactory.Ink);
            healthText = UIFactory.Label("Health", panel.transform, Vector2.up, new Vector2(18, -10), new Vector2(280, 28), "HP", 17);
            hp = UIFactory.Bar("HealthBar", panel.transform, new Vector2(18, -42), new Vector2(284, 12), new Color(1, .3f, .45f));
            levelText = UIFactory.Label("Level", panel.transform, Vector2.up, new Vector2(18, -64), new Vector2(280, 24), "レベル 1", 17, UIFactory.Cyan);
            xp = UIFactory.Bar("ExperienceBar", panel.transform, new Vector2(18, -98), new Vector2(284, 8), UIFactory.Cyan);
            timer = UIFactory.Label("Timer", transform, new Vector2(.5f, 1), new Vector2(0, -22), new Vector2(250, 48), "03:00", 36, null, TextAnchor.MiddleCenter);
            kills = UIFactory.Label("Kills", transform, Vector2.one, new Vector2(-30, -26), new Vector2(290, 40), "", 22, UIFactory.Cyan, TextAnchor.MiddleRight);
            weapons = UIFactory.Label("Weapons", transform, Vector2.zero, new Vector2(30, 28), new Vector2(790, 88), "", 15);
            UIFactory.Label("Controls", transform, Vector2.right, new Vector2(-30, 26), new Vector2(420, 56), "WASD／矢印キー／左スティックで移動\n攻撃は自動・Escでメニュー", 16, new Color(.6f, .68f, .78f), TextAnchor.MiddleRight);
            toast = UIFactory.Label("Toast", transform, new Vector2(.5f, .5f), new Vector2(0, 145), new Vector2(1000, 70), "", 25, UIFactory.Cyan, TextAnchor.MiddleCenter);
            bossLabel = UIFactory.Label("BossName", transform, new Vector2(.5f, 1), new Vector2(0, -86), new Vector2(600, 28), "", 17, new Color(1, .45f, .65f), TextAnchor.MiddleCenter);
            var bossPanel = UIFactory.Rect("BossBarPosition", transform, new Vector2(.5f, 1), new Vector2(0, -120), new Vector2(440, 12));
            bossHp = UIFactory.Bar("BossHP", bossPanel, Vector2.zero, new Vector2(440, 10), new Color(1, .3f, .55f));
            bossPanel.gameObject.SetActive(false);
            partsLabel = UIFactory.Label("部位", transform, new Vector2(.5f, 1), new Vector2(0, -145), new Vector2(760, 50), "", 17, Color.yellow, TextAnchor.MiddleCenter);
        }
        public void Bind(PlayerHealth target) => player = target;
        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            float time = gm.Mode == RunMode.Solo ? Mathf.Max(0, gm.soloDuration - gm.Elapsed) : gm.Elapsed;
            timer.text = string.Format("{0:00}:{1:00}", (int)time / 60, (int)time % 60);
            kills.text = gm.Mode == RunMode.Solo ? "撃破数　" + gm.Kills : "ボス決戦";
            if (player)
            {
                var stats = player.GetComponent<PlayerStats>();
                UIFactory.SetBar(hp, player.Current / player.Maximum, 284);
                UIFactory.SetBar(xp, (float)stats.Experience / stats.RequiredExperience, 284);
                healthText.text = "HP　" + Mathf.CeilToInt(player.Current) + " / " + player.Maximum + (player.HasBarrier ? "　結晶◆1" : "") + (player.Shield > 0 ? "　盾 " + Mathf.CeilToInt(player.Shield) : "");
                var recovery = player.GetComponent<BossRecovery>();
                recoveryLabel.text = (stats.Regeneration > 0 ? "自動回復 " + stats.Regeneration.ToString("0.00") + "/秒（被弾後5秒休止）" : "") +
                    (gm.Mode == RunMode.Boss && recovery ? "\n" + recovery.Status : "");
                levelText.text = "レベル " + stats.Level + "   •   " + stats.Experience + " / " + stats.RequiredExperience + " XP";
                var names = new System.Collections.Generic.List<string>();
                foreach (var weapon in stats.GetComponents<WeaponBase>()) if (weapon.Level > 0 && !PhysicalEvolution.IsPartner(weapon)) names.Add((PhysicalEvolution.Active(weapon)?"◆":"")+PhysicalEvolution.Name(weapon) + " " + PhysicalEvolution.DisplayLevel(weapon));
                string weaponLines = names.Count > 3 ? string.Join("　｜　", names.GetRange(0, 3)) + "\n" + string.Join("　｜　", names.GetRange(3, names.Count - 3)) : string.Join("　｜　", names);
                weapons.text = "武器 " + stats.WeaponCount + "/6（属性 " + stats.ElementWeaponCount + "/4）　" + weaponLines +
                    "\n攻撃倍率　" + stats.DamageMultiplier.ToString("0.0") + "x　　移動速度　" + stats.MoveSpeed.ToString("0.0");
            }
            EnemyHealth boss = null;
            foreach (var enemy in EnemyHealth.Active) if (enemy && enemy.IsBoss) { boss = enemy; break; }
            bossHp.transform.parent.parent.gameObject.SetActive(boss != null);
            if (boss)
            {
                UIFactory.SetBar(bossHp, boss.Current / boss.maximum, 440);
                var difficulty = boss.GetComponent<BossDifficulty>();
                var ai = boss.GetComponent<BossAI>();
                bossLabel.text = (ai ? ai.DisplayName : "ボス") + "　合計Lv " + (difficulty ? difficulty.TeamLevel : 1) + "　" + (ai ? ai.ActionLabel : "");
            }
            else bossLabel.text = "";
            partsLabel.text = boss && boss.GetComponent<BossParts>() ? boss.GetComponent<BossParts>().Status() : "";
            if (Time.unscaledTime > toastUntil) toast.text = "";
        }
        public void Toast(string message) { toast.text = message; toastUntil = Time.unscaledTime + 4; }
        public void ShowResult(bool won)
        {
            LevelUI.Hide();
            if (result) return;
            result = UIFactory.Panel("Result", transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(610, 320), UIFactory.Ink).gameObject;
            UIFactory.Label("Heading", result.transform, new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(550, 60), won ? "ボス討伐成功！" : "冒険終了", 38, won ? UIFactory.Cyan : new Color(1, .4f, .5f), TextAnchor.MiddleCenter);
            UIFactory.Label("Summary", result.transform, new Vector2(.5f, 1), new Vector2(0, -112), new Vector2(550, 70),
                "レベル " + (player ? player.GetComponent<PlayerStats>().Level : 1) + "  •  " + GameManager.Instance.Kills + "体撃破\n" + (won ? "仲間とともにボスを倒した。" : "次は違う武器や能力で挑戦してみよう。"), 21, null, TextAnchor.MiddleCenter);
            UIFactory.Button("Retry", result.transform, new Vector2(.5f, 0), new Vector2(0, 38), new Vector2(330, 58), NetworkManager.InRoom ? "部屋から退出" : "もう一度出撃", () => GameManager.Instance.Retry());
            UIFactory.Button("Home", result.transform, new Vector2(1, 1), new Vector2(-16, -12), new Vector2(112, 36), "ホーム", () => GameManager.Instance.ReturnHome());
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
