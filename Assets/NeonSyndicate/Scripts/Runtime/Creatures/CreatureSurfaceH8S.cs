using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The creature materials of the H8S look: the shared character material (same shader, palette, emission, hit flash
    /// and glow properties on either render pipeline) with the fabric weave in its detail slot replaced by one of the
    /// CreatureHideH8S relief families (chitin plates, scales, cracked basalt) on the second UV set, so hides and plates
    /// catch the key light and read at night while crystal facets stay flat (their palette cells carry no detail). One
    /// material per family is shared by every creature that wears it; the rim light follows the shared material's
    /// day and night rim. Anything that fails falls back to the shared character material.
    /// </summary>
    public static class CreatureSurfaceH8S
    {
        private static readonly Material[] _materials = new Material[CreatureHideH8S.FamilyCount];
        private static readonly Texture2D[] _normals = new Texture2D[CreatureHideH8S.FamilyCount];
        private static Color _rimSent = new Color(-1f, 0f, 0f, 0f);
        private static int _rimFrame = -1;
        private static readonly int RimId = Shader.PropertyToID("_RimColor");
        private static readonly int DetailId = Shader.PropertyToID("_DetailTex");
        private static readonly int DetailStrengthId = Shader.PropertyToID("_DetailStrength");
        private static readonly int EmissionBoostId = Shader.PropertyToID("_EmissionBoost");

        /// <summary>Relief texture size by graphics preset (Low keeps a small map; one tile spans a few plates, so 256 is plenty).</summary>
        public static int TextureSize { get { return Settings.Graphics == GraphicsPreset.Low ? 128 : 256; } }

        /// <summary>How much the relief shows relative to the people's fabric detail (the per cell strength multiplies this).</summary>
        public const float DetailStrength = 1.05f;

        /// <summary>Creature rim light relative to the people's (obsidian stays dark at its edges).</summary>
        public const float RimScale = 0.7f;

        /// <summary>
        /// Emission boost of the creature materials (the people's is 2.4, tuned for neon implants): the crystal's inner
        /// light and the eyes stay readable under the realism grade without turning the shards into lamps.
        /// </summary>
        public const float EmissionBoost = 2f;

        public static void ResetStatics()
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                Release(_materials[i]);
                Release(_normals[i]);
                _materials[i] = null;
                _normals[i] = null;
            }
            _rimSent = new Color(-1f, 0f, 0f, 0f);
            _rimFrame = -1;
        }

        /// <summary>The material for a built body: its relief family's material, or the shared character material.</summary>
        public static Material For(CreatureLook look)
        {
            return Get(look != null && look.ObsidianH8S ? look.SurfaceH8S : CreatureHideH8S.None);
        }

        /// <summary>The shared material of a relief family (CreatureHideH8S); family 0 or any failure gives the character material.</summary>
        public static Material Get(int family)
        {
            Material basis = Mats.CharacterMaterial;
            if (family <= CreatureHideH8S.None || family >= CreatureHideH8S.FamilyCount || basis == null) return basis;
            if (_materials[family] != null) return _materials[family];
            try
            {
                Material m = new Material(basis) { name = "Creature " + Name(family) + " (H8S)" };
                Texture2D relief = Normal(family);
                if (relief != null && m.HasProperty(DetailId)) m.SetTexture(DetailId, relief);
                if (m.HasProperty(DetailStrengthId)) m.SetFloat(DetailStrengthId, DetailStrength);
                if (m.HasProperty(EmissionBoostId)) m.SetFloat(EmissionBoostId, EmissionBoost);
                if (m.HasProperty(RimId))
                {
                    Color rim = basis.GetColor(RimId);
                    rim.a *= RimScale;
                    m.SetColor(RimId, rim);
                }
                _materials[family] = m;
                return m;
            }
            catch (Exception e)
            {
                Log.Exception("Creature surface " + family, e);
                return basis;
            }
        }

        /// <summary>
        /// Once a frame (CreatureRig calls it from LateUpdate): copies the shared material's rim light, which NeonPost
        /// retunes by time of day, onto the creature materials, scaled down.
        /// </summary>
        public static void SyncRim()
        {
            if (_rimFrame == Time.frameCount) return;
            _rimFrame = Time.frameCount;
            Material basis = Mats.CharacterMaterial;
            if (basis == null || !basis.HasProperty(RimId)) return;
            Color rim = basis.GetColor(RimId);
            if (Mathf.Abs(rim.r - _rimSent.r) + Mathf.Abs(rim.g - _rimSent.g) + Mathf.Abs(rim.b - _rimSent.b) + Mathf.Abs(rim.a - _rimSent.a) < 0.005f) return;
            _rimSent = rim;
            rim.a *= RimScale;
            for (int i = 1; i < _materials.Length; i++) if (_materials[i] != null) _materials[i].SetColor(RimId, rim);
        }

        /// <summary>The RGB relief normal texture of a family (generated once, mipmapped, repeating).</summary>
        public static Texture2D Normal(int family)
        {
            if (family <= CreatureHideH8S.None || family >= CreatureHideH8S.FamilyCount) return null;
            if (_normals[family] != null) return _normals[family];
            try
            {
                int n = TextureSize;
                Color32[] px = CreatureHideH8S.Normal(family, n);
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true, true);
                t.name = "Creature " + Name(family) + " relief (H8S)";
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Trilinear;
                t.anisoLevel = 4;
                t.SetPixels32(px);
                t.Apply(true, true);
                _normals[family] = t;
                return t;
            }
            catch (Exception e)
            {
                Log.Exception("Creature relief " + family, e);
                return null;
            }
        }

        private static string Name(int family)
        {
            switch (family)
            {
                case CreatureHideH8S.ChitinPlates: return "chitin";
                case CreatureHideH8S.Scales: return "scales";
                case CreatureHideH8S.Basalt: return "basalt";
                default: return "hide";
            }
        }

        private static void Release(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
