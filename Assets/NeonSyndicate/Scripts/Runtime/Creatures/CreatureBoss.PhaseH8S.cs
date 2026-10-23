using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S boss readability on top of the existing fight owners (BossPhaseRulesH8S holds the numbers). An upgraded boss
    /// opening a phase holds its own swings for the roar and drops rings that were scheduled but never shown, like the
    /// regional guardians already did; every boss's phase is announced with a banner and a deep bell; supports and
    /// mirror copies raised mid-fight wait a moment before their first swing; and boss rings draw their edge at the real
    /// reach of the burst in the tell colour (orange: cannot be blocked) over their elemental fill. Damage, phases,
    /// budgets and rewards are untouched; the Glacier Titan keeps its script and only gets the banner and the ring edge.
    /// </summary>
    public sealed partial class CreatureBoss
    {
        /// <summary>How far past a boss ring's radius its burst still hits the player's feet (BurstDamage).</summary>
        private const float RingSlackH8S = 0.3f;

        /// <summary>
        /// A phase opens on an upgraded fight: the boss stands and roars (no swing for BossPhaseRulesH8S.HoldSeconds),
        /// rings still waiting to appear are dropped, and the phase is announced.
        /// </summary>
        private void BeginPhaseBeatH8S()
        {
            float hold = BossPhaseRulesH8S.HoldSeconds(Settings.Difficulty);
            if (Npc != null) Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, Time.time + hold);
            int dropped = DropPendingHazardsH8S();
            AnnouncePhaseH8S();
            Log.Info("Boss H8S: " + (Species != null ? Species.Id : "boss") + " phase " + (Phase + 1) + " beat: swings held " + hold.ToString("0.0") + " s, " + dropped + " unshown rings dropped");
        }

        /// <summary>The phase banner and a deep bell (presentation only).</summary>
        private void AnnouncePhaseH8S()
        {
            if (Npc == null) return;
            CombatBannerH8S.ShowPhase(Npc.DisplayName, Phase, PhaseCount, null);
            CombatSounds.Play(CombatSounds.Clip.PhaseShift, Npc.HeadPosition, 0.85f, 1f, 0f, 250f);
        }

        /// <summary>Removes scheduled rings that never appeared (BossPhaseRulesH8S.DropsAtPhase); returns how many went.</summary>
        private int DropPendingHazardsH8S()
        {
            int n = 0;
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = _hazards[i];
                if (!BossPhaseRulesH8S.DropsAtPhase(h.Shown, h.Arrival)) continue;
                if (h.Ring != null) h.Ring.Release();
                _hazards.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>A support or copy raised mid-fight waits a moment before its first swing (BossPhaseRulesH8S.ArrivalGrace).</summary>
        private static void ArrivalGraceH8S(NpcController add)
        {
            if (add == null || add.IsDead) return;
            add.CreatureBusyUntil = Mathf.Max(add.CreatureBusyUntil, Time.time + BossPhaseRulesH8S.ArrivalGrace(Settings.Difficulty));
        }

        /// <summary>Shows a boss ring with its edge at the burst's real reach in the tell colour, over its elemental fill.</summary>
        private static GroundRing ShowHazardRingH8S(Vector3 center, float radius, float fill, Color theme, DamageFlags flags)
        {
            return GroundRing.ShowHit(center, radius + RingSlackH8S, fill, theme, CombatTellsH8S.ColorOf(CombatTellsH8S.SeverityOf(flags)));
        }
    }
}
