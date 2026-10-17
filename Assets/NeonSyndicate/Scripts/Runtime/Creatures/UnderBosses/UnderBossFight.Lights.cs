using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// UnderBossFight, 04E light budget: every realtime light a fight creates goes through KeyLight, which keeps it
    /// within BossLightBudget04E (one shadowless Auto-mode light per fight). A refused light returns null and the fight
    /// relies on its emissive materials (every caller null-checks).
    /// </summary>
    public abstract partial class UnderBossFight
    {
        private int _lightsCreated;

        /// <summary>Realtime lights this fight created (the F2 page and the log show it).</summary>
        public int LightsCreated { get { return _lightsCreated; } }

        /// <summary>The fight's one key light (shadowless, Auto render mode), or null once the budget is spent.</summary>
        protected Light KeyLight(Transform parent, Vector3 local, Color color, float range, float intensity)
        {
            if (parent == null) return null;
            if (!BossLightBudget04E.MayCreate(_lightsCreated))
            {
                Log.Warn("UBOSS04E " + (Profile != null ? Profile.Id : "boss") + ": light budget spent (" + BossLightBudget04E.MaxFightLights + " per fight); the glow stays emissive only.");
                return null;
            }
            Light l = UnderBossMachine.Glow(parent, local, color, range, intensity);
            if (l == null) return null;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
            _lightsCreated++;
            return l;
        }
    }
}
