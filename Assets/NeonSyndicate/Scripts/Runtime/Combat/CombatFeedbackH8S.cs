using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: weight and confirmation for the player's hits (owner: the floating damage number report, which sees every
    /// player hit on a character once it has resolved, numbers on or off). A kill plays a crisp crystal confirmation
    /// (a heavier one for elites, champions and Shard-Touched creatures; bosses keep their death sequence) that climbs in
    /// pitch through a streak, and kills or genuinely heavy hits from guns, foci, powers and blasts get a short impact
    /// freeze through CombatDirector.HitStop, the GameTime request owner (HitWeightRulesH8S decides; reduced effects
    /// shrink it). Unscaled time throughout, so a freeze can never lengthen its own spacing.
    /// </summary>
    public static class CombatFeedbackH8S
    {
        private static float _lastStopAt = -99f;
        private static float _lastKillAt = -99f;
        private static int _streak;
        private static int _kills;
        private static int _stops;

        /// <summary>Chained from CombatDirector.ResetStatics.</summary>
        public static void ResetStatics()
        {
            _lastStopAt = -99f;
            _lastKillAt = -99f;
            _streak = 0;
            _kills = 0;
            _stops = 0;
        }

        /// <summary>Kill confirmations played this session (debug page).</summary>
        public static int Kills { get { return _kills; } }

        /// <summary>Feedback freezes asked for this session (debug page).</summary>
        public static int Stops { get { return _stops; } }

        /// <summary>
        /// One of the player's hits on a character, after it resolved: health and armor are what it took, burst whether it
        /// was a Resonance Burst.
        /// </summary>
        public static void OnPlayerHit(CharacterBase target, DamageInfo info, float health, float armor, bool burst)
        {
            if (target == null || target.Health == null || !IsFoe(target)) return;
            bool killed = !target.Health.IsAlive;
            KillTierH8S tier = HitWeightRulesH8S.KillTier(killed, IsElite(target), ResonanceSystem.IsBossLike(target));
            float now = Time.unscaledTime;
            float share = (Mathf.Max(0f, health) + Mathf.Max(0f, armor)) / Mathf.Max(1f, target.Health.MaxHealth);
            bool reduced = Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects;
            // A companion's or fused vehicle's hit is credited to the player but never freezes the player's world.
            float stop = info.Origin == DamageOrigin.Player
                ? HitWeightRulesH8S.HitStop(info.Type, burst, share, info.Crit || info.Headshot, tier, reduced, now - _lastStopAt) : 0f;
            if (stop > 0f)
            {
                _lastStopAt = now;
                _stops++;
                CombatDirector.HitStop(stop);
            }
            if (tier == KillTierH8S.Normal || tier == KillTierH8S.Elite) ConfirmKill(target, tier, now);
        }

        private static void ConfirmKill(CharacterBase target, KillTierH8S tier, float now)
        {
            _streak = HitWeightRulesH8S.NextStreak(_streak, now - _lastKillAt);
            _lastKillAt = now;
            _kills++;
            bool heavy = tier == KillTierH8S.Elite;
            CombatSounds.Play(heavy ? CombatSounds.Clip.KillHeavy : CombatSounds.Clip.Kill, target.ChestPosition, heavy ? 0.7f : 0.5f,
                HitWeightRulesH8S.StreakPitch(_streak), 0f, 60f);
        }

        /// <summary>Only the player's foes get weight and confirmation (civilians and allies never do).</summary>
        private static bool IsFoe(CharacterBase target)
        {
            NpcController n = target as NpcController;
            if (n != null) return n.FoeOfPlayerH8S;
            Creature c = target as Creature;
            return c != null && c.FoeOfPlayerH8S;
        }

        /// <summary>Elites, champions and Shard-Touched creatures get the heavy confirmation.</summary>
        private static bool IsElite(CharacterBase target)
        {
            NpcController n = target as NpcController;
            if (n != null) return n.Elite || n.IsChampion;
            Creature c = target as Creature;
            return c != null && c.Elite;
        }
    }
}
