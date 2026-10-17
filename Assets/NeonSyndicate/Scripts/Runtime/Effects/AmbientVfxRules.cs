using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The ambient particle families of the visual pass.</summary>
    public enum AmbientVfxKind
    {
        /// <summary>Slow bright specks hanging in a lit room or under a lamp cone.</summary>
        DustMotes,
        /// <summary>Faint warm ripples rising over lava and hot vents (an approximation; true refraction is a shader).</summary>
        HeatHaze,
        /// <summary>Orange sparks drifting up and dying.</summary>
        Embers,
        /// <summary>Fine snow specks glittering near the ground in the Frostline.</summary>
        SnowMotes,
        /// <summary>Thin steam breathing out of a street drain at night.</summary>
        Steam,
        /// <summary>Pale spores drifting in the Underheart gardens.</summary>
        Spores
    }

    /// <summary>
    /// The engine-free budget rules of the ambient VFX: how many systems may be alive per preset, how far they are
    /// culled, how many particles each family may hold, and which registered spots the runner should keep alive.
    /// </summary>
    public static class AmbientVfxRules
    {
        /// <summary>Most emitters alive at once (Low: none of the new ones).</summary>
        public static int MaxActive(GraphicsPreset g)
        {
            switch (g)
            {
                case GraphicsPreset.Low: return 0;
                case GraphicsPreset.Medium: return 6;
                case GraphicsPreset.High: return 14;
                default: return 22;
            }
        }

        /// <summary>Beyond this distance from the viewer a spot is never started, and a running one is stopped.</summary>
        public static float CullDistance(GraphicsPreset g)
        {
            switch (g)
            {
                case GraphicsPreset.Low: return 0f;
                case GraphicsPreset.Medium: return 38f;
                case GraphicsPreset.High: return 60f;
                default: return 85f;
            }
        }

        /// <summary>Particle cap of one emitter of the family on the preset.</summary>
        public static int ParticleCap(AmbientVfxKind k, GraphicsPreset g)
        {
            float scale = g >= GraphicsPreset.Ultra ? 1.6f : g >= GraphicsPreset.High ? 1f : 0.5f;
            int baseCap;
            switch (k)
            {
                case AmbientVfxKind.DustMotes: baseCap = 90; break;
                case AmbientVfxKind.HeatHaze: baseCap = 40; break;
                case AmbientVfxKind.Embers: baseCap = 70; break;
                case AmbientVfxKind.SnowMotes: baseCap = 160; break;
                case AmbientVfxKind.Steam: baseCap = 30; break;
                default: baseCap = 60; break;
            }
            return Mathf.Max(8, Mathf.RoundToInt(baseCap * scale));
        }

        /// <summary>Emission per second for one emitter of the family (the lifetime keeps it under the cap).</summary>
        public static float Rate(AmbientVfxKind k, GraphicsPreset g)
        {
            float scale = g >= GraphicsPreset.Ultra ? 1.5f : g >= GraphicsPreset.High ? 1f : 0.5f;
            switch (k)
            {
                case AmbientVfxKind.DustMotes: return 14f * scale;
                case AmbientVfxKind.HeatHaze: return 12f * scale;
                case AmbientVfxKind.Embers: return 16f * scale;
                case AmbientVfxKind.SnowMotes: return 40f * scale;
                case AmbientVfxKind.Steam: return 7f * scale;
                default: return 10f * scale;
            }
        }

        /// <summary>Families that only run on High and Ultra (Medium keeps the cheapest, most visible ones).</summary>
        public static bool FamilyOn(AmbientVfxKind k, GraphicsPreset g)
        {
            if (g <= GraphicsPreset.Low) return false;
            if (g == GraphicsPreset.Medium) return k == AmbientVfxKind.DustMotes || k == AmbientVfxKind.Embers || k == AmbientVfxKind.SnowMotes;
            return true;
        }

        /// <summary>Whole seconds a running spot keeps its slot before a nearer one may take it (no flip-flopping).</summary>
        public const float Hysteresis = 1.5f;

        /// <summary>
        /// Picks the indices of the spots to keep alive: the nearest ones inside the cull distance, at most maxActive,
        /// and never one whose family is off. Pure so it can be tested; distances are flat squared distances.
        /// </summary>
        public static void Select(IList<float> sqrDistances, IList<AmbientVfxKind> kinds, IList<bool> enabled, GraphicsPreset g, List<int> into)
        {
            into.Clear();
            int max = MaxActive(g);
            if (max <= 0) return;
            float cull = CullDistance(g);
            float cullSqr = cull * cull;
            for (int i = 0; i < sqrDistances.Count; i++)
            {
                if (!enabled[i] || !FamilyOn(kinds[i], g) || sqrDistances[i] > cullSqr) continue;
                // Insertion by distance, bounded by max.
                int at = into.Count;
                while (at > 0 && sqrDistances[into[at - 1]] > sqrDistances[i]) at--;
                if (at >= max) continue;
                into.Insert(at, i);
                if (into.Count > max) into.RemoveAt(into.Count - 1);
            }
        }
    }
}
