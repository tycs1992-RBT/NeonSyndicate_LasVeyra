using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: the Resonance loop on the four Underheart bosses (the Valve Guardian, the Cinder Matron, the Vaultbreaker and
    /// the Kiln Regent). A boss body is its own controller, not a character, so marks live on its lock-on stand-in
    /// (UBossLockProxy04E, a character registered nowhere whose health is the body's): the same mark book, the same
    /// ring, shell and label, the boss mark time (4.5 s) and the boss burst multiplier (x1.75). The fight's damage
    /// filter asks UnderBossBurstState for every hit on the body and applies the multiplier itself (the body's health
    /// has no character owner, so it never applies one), then its Damaged hook calls OnUnderBossHit: the burst (splash on
    /// the adds, magazine refill, the gold marker, BurstFired), crystal energy for gun and melee hits, and the marks a
    /// plain hit leaves (ResonanceRules.UnderBossMarkForHit). Glass Sight and Sunbolt reach the boss through PrimeAround.
    /// A mark never staggers, knocks down, burns or stuns a boss.
    /// </summary>
    public sealed partial class ResonanceSystem
    {
        /// <summary>True for the lock-on stand-in of an Underheart boss.</summary>
        public static bool IsUnderBoss(CharacterBase c)
        {
            return c is UBossLockProxy04E;
        }

        /// <summary>True while an Underheart boss's stand-in can carry a mark: its fight is in its fighting state with a living body.</summary>
        private static bool UnderBossMarkable(CharacterBase c)
        {
            UBossLockProxy04E p = c as UBossLockProxy04E;
            return p != null && p.Fight != null && p.Fight.AcceptsAssists && !p.IsDead;
        }

        /// <summary>
        /// Adds the running Underheart boss to a list of foes around center when its nearest hit volume lies within radius
        /// (a big body counts when its flank is in reach). PrimeAround sorts and filters the list as usual.
        /// </summary>
        private static void AddUnderBossCandidate(Vector3 center, float radius, List<CharacterBase> into)
        {
            UBossLockProxy04E p = UBossLockProxy04E.Active;
            if (p == null || !UnderBossMarkable(p) || into.Contains(p)) return;
            Vector3 s = p.Fight.BodySurfaceNear(center);
            if (!MathUtil.IsFinite(s) || (s - center).sqrMagnitude > radius * radius) return;
            into.Add(p);
        }

        /// <summary>
        /// The mark a hit on an Underheart boss body sets off (None when unmarked or the hit is not one of the player's gun
        /// rounds), read from the boss's stand-in. The caller (the fight's damage filter) applies
        /// ResonanceRules.BurstDamageMultiplier(true) and reports the hit back through OnUnderBossHit.
        /// </summary>
        public static ResonanceState UnderBossBurstState(UBossLockProxy04E proxy, DamageInfo info)
        {
            if (proxy == null || proxy.IsDead || !ResonanceRules.Detonates(info.Type, info.WeaponId, info.FromPlayer)) return ResonanceState.None;
            return _book.StateOf(proxy.GetInstanceID(), Time.time);
        }

        /// <summary>
        /// A hit of the player's side landed on an Underheart boss body (after the fight's filter scaled it): a burst is
        /// consumed and plays out, gun and melee hits refill crystal energy (capped per second like any foe), and a
        /// companion's Glass Lance or a power striking the body leaves its mark. applied is the damage the body took.
        /// </summary>
        public static void OnUnderBossHit(UBossLockProxy04E proxy, DamageInfo info, ResonanceState burst, float applied, bool killed)
        {
            if (proxy == null || !info.FromPlayer) return;
            if (burst != ResonanceState.None) OnBurst(proxy, info, burst, Mathf.Max(0f, applied));
            if (!killed)
            {
                string companion = "";
                if (info.Origin == DamageOrigin.Companion && Game.Bonds != null && Game.Bonds.Active != null) companion = Game.Bonds.Active.Archetype ?? "";
                ResonanceState mark = ResonanceRules.UnderBossMarkForHit(info.Type, info.WeaponId, info.Origin, companion);
                if (mark != ResonanceState.None) Prime(proxy, mark);
            }
            if (applied > 0f) OnPlayerHit(proxy, info, killed);
        }
    }
}
