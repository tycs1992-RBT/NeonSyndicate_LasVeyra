using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CrystalEncounters
    {
        /// <summary>
        /// The scoped monster-combat noise rule (WantedSystem asks it for gunfire, blast and fire noise that has no
        /// victim): true only while this contract's fight is live, the noise is inside its area, and the shot has
        /// provenance from the fight (it landed on or passed by an owned opponent, or the player just hurt one). Crimes
        /// with a victim (a civilian, a car, the police) never come here, so harming them still counts.
        /// </summary>
        public static bool ScopedNoise(Vector3 from, Vector3 to)
        {
            CrystalEncounters e = Game.Encounters;
            return e != null && e.MonsterNoise(from, to);
        }

        /// <summary>The instance side of ScopedNoise (see there).</summary>
        public bool MonsterNoise(Vector3 from, Vector3 to)
        {
            EncounterRun04D r = _run;
            if (r == null) return false;
            bool live = _state == EncounterState.Active || _state == EncounterState.Retreating;
            float nearImpact = float.MaxValue;
            float nearLine = float.MaxValue;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Defeated || a.Removed || a.Npc == null || a.Npc.IsDead) continue;
                Vector3 c = a.Npc.ChestPosition;
                nearImpact = Mathf.Min(nearImpact, Vector3.Distance(c, to));
                nearLine = Mathf.Min(nearLine, EncounterRules04D.SegmentDistance(from, to, c));
            }
            return EncounterRules04D.NoiseExempt(live, MathUtil.FlatDistance(from, r.Origin), nearImpact, nearLine, Time.time - r.LastOwnedHurtAt);
        }
    }
}
