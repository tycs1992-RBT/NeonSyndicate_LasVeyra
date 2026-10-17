using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared materials and meshes for the fused forms and chassis trims: one five-slot palette per form or trim
    /// (shell, crystal, membrane, metal, glow), a hot variant of the crystal slot for the Kilnback brace, and one cached
    /// mesh per blueprint part (form, class, quality), so repeated fusions never rebuild or leak geometry.
    /// </summary>
    public static class FusionArt
    {
        private static readonly Dictionary<string, Material[]> _palettes = new Dictionary<string, Material[]>();
        private static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();
        private static readonly Dictionary<string, Material> _single = new Dictionary<string, Material>();
        private static readonly Dictionary<string, FormBlueprint> _blueprints = new Dictionary<string, FormBlueprint>();
        private static readonly Dictionary<string, Vector3[]> _envelopes = new Dictionary<string, Vector3[]>();

        public static void ResetStatics()
        {
            _palettes.Clear();
            _meshes.Clear();
            _single.Clear();
            _blueprints.Clear();
            _envelopes.Clear();
        }

        /// <summary>True when the low-detail variants should be used (Low graphics preset).</summary>
        public static bool UseLow { get { return Settings.Graphics == GraphicsPreset.Low; } }

        /// <summary>The cached blueprint of a form (or trim) for a class and quality.</summary>
        public static FormBlueprint Blueprint(string form, string chassisClass, bool low, bool trim)
        {
            string key = (trim ? "t|" : "f|") + form + "|" + chassisClass + "|" + (low ? "L" : "H");
            FormBlueprint bp;
            if (_blueprints.TryGetValue(key, out bp)) return bp;
            bp = trim ? FusionFormGeometry.BuildTrim(form, chassisClass, low) : FusionFormGeometry.Build(form, chassisClass, low);
            _blueprints[key] = bp;
            return bp;
        }

        /// <summary>The cached transition envelope (min, max in chassis space) of a form: what the fusion check needs.</summary>
        public static void TransitionEnvelope(string form, string chassisClass, bool low, out Vector3 min, out Vector3 max)
        {
            string key = form + "|" + chassisClass + "|" + (low ? "L" : "H");
            Vector3[] env;
            if (!_envelopes.TryGetValue(key, out env))
            {
                Vector3 a, b;
                Blueprint(form, chassisClass, low, false).Envelope(true, out a, out b);
                env = new[] { a, b };
                _envelopes[key] = env;
            }
            min = env[0];
            max = env[1];
        }

        /// <summary>The mesh of one blueprint part (built once, shared by every fusion of that form, class and quality).</summary>
        public static Mesh PartMesh(FormBlueprint bp, int index)
        {
            string key = bp.Form + "|" + bp.ChassisClass + "|" + (bp.Low ? "L" : "H") + "|" + index;
            Mesh m;
            if (_meshes.TryGetValue(key, out m) && m != null) return m;
            m = bp.Parts[index].Mesh.ToMesh("Fusion " + bp.Form + " " + bp.Parts[index].Name);
            _meshes[key] = m;
            return m;
        }

        /// <summary>The five-slot palette of a form or trim (Shell, Crystal, Membrane, Metal, Glow).</summary>
        public static Material[] Palette(string id)
        {
            Material[] p;
            if (_palettes.TryGetValue(id ?? "", out p) && p != null && p[0] != null) return p;
            switch (id)
            {
                case FusionIds.Ventwing:
                    p = Make(id, new Color(0.15f, 0.19f, 0.26f), 0.55f, 0.2f, new Color(0.62f, 0.92f, 1f), 1.6f, new Color(0.11f, 0.29f, 0.35f), 0.45f, new Color(0.85f, 0.97f, 1f), 3.2f);
                    break;
                case FusionIds.Kilnback:
                    p = Make(id, new Color(0.2f, 0.18f, 0.17f), 0.12f, 0f, new Color(1f, 0.45f, 0.12f), 1.8f, new Color(0.3f, 0.22f, 0.18f), 0.2f, new Color(1f, 0.62f, 0.25f), 3f);
                    p[FormSlots.Metal] = Mats.DarkMetal;
                    break;
                case FusionIds.Coilrunner:
                    p = Make(id, new Color(0.18f, 0.18f, 0.24f), 0.5f, 0.35f, new Color(0.58f, 0.52f, 1f), 1.9f, new Color(0.68f, 0.74f, 0.9f), 0.7f, new Color(0.88f, 0.86f, 1f), 3.4f);
                    break;
                case ChassisTrims.QuartzCrown:
                    p = Make(id, new Color(0.8f, 0.85f, 0.9f), 0.8f, 0.1f, new Color(0.9f, 0.95f, 1f), 1.2f, new Color(0.7f, 0.8f, 0.9f), 0.8f, new Color(0.95f, 0.98f, 1f), 2.2f);
                    break;
                case ChassisTrims.EmberFins:
                    p = Make(id, new Color(0.45f, 0.24f, 0.12f), 0.6f, 0.8f, new Color(1f, 0.5f, 0.15f), 1.4f, new Color(0.62f, 0.33f, 0.16f), 0.7f, new Color(1f, 0.6f, 0.25f), 2.4f);
                    break;
                case ChassisTrims.FrostPlates:
                    p = Make(id, new Color(0.7f, 0.82f, 0.9f), 0.9f, 0.15f, new Color(0.55f, 0.85f, 1f), 1f, new Color(0.62f, 0.8f, 0.92f), 0.9f, new Color(0.7f, 0.9f, 1f), 2f);
                    break;
                case ChassisTrims.CouplerGlow:
                    p = Make(id, new Color(0.16f, 0.17f, 0.2f), 0.6f, 0.7f, new Color(1f, 0.78f, 0.35f), 1.8f, new Color(0.3f, 0.32f, 0.36f), 0.6f, new Color(1f, 0.82f, 0.45f), 3f);
                    break;
                default:
                    p = Make(id ?? "", new Color(0.2f, 0.2f, 0.24f), 0.6f, 0.6f, new Color(0.5f, 0.45f, 1f), 1.6f, new Color(0.3f, 0.3f, 0.35f), 0.6f, new Color(0.7f, 0.6f, 1f), 2.6f);
                    break;
            }
            _palettes[id ?? ""] = p;
            return p;
        }

        /// <summary>The bright crystal slot used while the Kilnback braces (ember seams flare).</summary>
        public static Material HotCrystal
        {
            get
            {
                Material m;
                if (_single.TryGetValue("kiln_hot", out m) && m != null) return m;
                m = Mats.Standard("Kilnback Hot Seams", new Color(1f, 0.55f, 0.2f), 0.6f, 0f);
                Mats.SetEmission(m, new Color(1f, 0.5f, 0.15f) * 4.5f, null);
                _single["kiln_hot"] = m;
                return m;
            }
        }

        /// <summary>An additive line or trail material in a colour (arc and glide trails).</summary>
        public static Material Streak(Color c)
        {
            return Mats.Glow(c);
        }

        private static Material[] Make(string id, Color shell, float shellSmooth, float shellMetal, Color crystal, float crystalGlow, Color membrane, float membraneSmooth, Color glow, float glowStrength)
        {
            Material s = Mats.Standard("Fusion " + id + " Shell", shell, shellSmooth, shellMetal);
            Material c = Mats.Standard("Fusion " + id + " Crystal", crystal, 0.92f, 0.1f);
            Mats.SetEmission(c, crystal * crystalGlow * 0.6f, null);
            Material m = Mats.Standard("Fusion " + id + " Membrane", membrane, membraneSmooth, 0.05f);
            Mats.SetEmission(m, membrane * 0.12f, null);
            Material g = Mats.Standard("Fusion " + id + " Glow", glow, 0.7f, 0f);
            Mats.SetEmission(g, glow * glowStrength, null);
            return new[] { s, c, m, Mats.Chrome, g };
        }
    }
}
