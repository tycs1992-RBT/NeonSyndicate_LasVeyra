using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the "what changed" banner after levelling up. Levels are raised in the shrine menu, where the HUD (and the
    /// old "+1 skill point" note, which expires on its own clock) is hidden, so the first level up of a visit keeps a
    /// picture of the player from before it, and the first HUD frame after the menu closes posts one banner through the
    /// existing Hud.Banner owner: "LEVEL 9" over "Vigor 6 → 7: max health 284 → 298. +1 skill point, 2 to spend in
    /// Pause > SKILLS." Several levels in one visit become one banner. Presentation only; nothing is saved.
    /// </summary>
    public static class LevelUpReadoutH8S
    {
        /// <summary>The HUD overlay key (registered on the first level up of a session).</summary>
        public const string OverlayKey = "progress.levelup_h8s";
        public const float BannerSeconds = 6f;

        private static LevelSnapshotH8S _start;
        private static bool _pending;

        public static void ResetStatics()
        {
            _start = new LevelSnapshotH8S();
            _pending = false;
        }

        /// <summary>True while a level up waits for the HUD to show its banner.</summary>
        public static bool Pending { get { return _pending; } }

        /// <summary>
        /// ProgressionHooks.OnLevelUp: Progression.LevelUp calls it after the level number rises and before the attribute
        /// and the player's stats change, so the first call of a visit captures the true "before" values.
        /// </summary>
        public static void Begin(int newLevel)
        {
            if (!_pending)
            {
                LevelSnapshotH8S s = Capture();
                if (!s.Valid) return;
                s.Level = Mathf.Max(1, newLevel - 1);
                _start = s;
                _pending = true;
            }
            HudOverlays.Register(OverlayKey, Draw);
        }

        /// <summary>The HUD overlay: runs only while the HUD shows (no menu open) and posts the banner once.</summary>
        private static void Draw(float w, float h)
        {
            if (!_pending) return;
            _pending = false;
            LevelSnapshotH8S end = Capture();
            if (Game.Hud == null || !LevelUpSummaryH8S.Rose(_start, end)) return;
            int points = Game.Skills != null ? Mathf.Max(0, Game.Skills.Available) : -1;
            Game.Hud.Banner(LevelUpSummaryH8S.Title(_start, end), LevelUpSummaryH8S.Body(_start, end, points), UIKit.Gold, BannerSeconds);
        }

        private static LevelSnapshotH8S Capture()
        {
            Progression pr = Game.Progress;
            if (pr == null) return new LevelSnapshotH8S();
            LevelSnapshotH8S s = new LevelSnapshotH8S
            {
                Valid = true,
                Level = pr.Level,
                Vigor = pr.Vigor,
                Endurance = pr.Endurance,
                Might = pr.Might,
                Focus = pr.Focus,
                Melee = pr.MeleeMultiplier,
                Gun = pr.GunMultiplier,
                Power = pr.PowerMultiplier
            };
            PlayerController p = Game.Player;
            if (p != null && p.Health != null) s.MaxHealth = p.Health.MaxHealth;
            if (p != null && p.Combat != null) s.MaxStamina = p.Combat.MaxStamina;
            if (Game.Abilities != null) s.MaxEnergy = Game.Abilities.MaxEnergy;
            return s;
        }
    }
}
