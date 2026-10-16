using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The five material colours of one crystal model, as plain data (the offline harness renders with the same numbers):
    /// matte structure, metal, passive crystal, active facets and the core, plus the vent colour the active facets shift
    /// toward while the focus is overheated. Structure and metal never glow; emission lives on active facets and cores.
    /// </summary>
    public sealed class CrystalPalette
    {
        public Color Structure;
        public float StructureSmooth;
        public Color Metal;
        public float MetalSmooth;
        public float Metallic;
        public Color Crystal;
        /// <summary>Faint emission on passive crystal so it reads as mineral glass, not plastic.</summary>
        public float CrystalGlow;
        public Color Active;
        public float ActiveGlow;
        public Color Core;
        public float CoreGlow;
        /// <summary>The hot colour active facets shift toward while venting.</summary>
        public Color Vent;
        /// <summary>The passive crystal slot is see-through glass (the resonance case panes).</summary>
        public bool CrystalTransparent;
        /// <summary>A short key for material caching.</summary>
        public string Key;
    }

    /// <summary>The palettes of the six foci, their shots and the world props.</summary>
    public static partial class CrystalFocusModels
    {
        private static readonly CrystalPalette[] _palettes = BuildPalettes();
        private static readonly CrystalPalette _propPalette = new CrystalPalette
        {
            Key = "prop",
            Structure = new Color(0.46f, 0.44f, 0.47f), StructureSmooth = 0.12f,
            Metal = new Color(0.74f, 0.58f, 0.3f), MetalSmooth = 0.55f, Metallic = 0.9f,
            Crystal = new Color(0.62f, 0.82f, 0.95f, 0.32f), CrystalGlow = 0.05f, CrystalTransparent = true,
            Active = new Color(0.45f, 0.85f, 1f), ActiveGlow = 2.2f,
            Core = new Color(0.8f, 0.95f, 1f), CoreGlow = 3f,
            Vent = new Color(1f, 0.6f, 0.35f)
        };

        private static readonly CrystalPalette _targetPalette = new CrystalPalette
        {
            Key = "target",
            Structure = new Color(0.46f, 0.44f, 0.47f), StructureSmooth = 0.12f,
            Metal = new Color(0.74f, 0.58f, 0.3f), MetalSmooth = 0.55f, Metallic = 0.9f,
            Crystal = new Color(0.55f, 0.8f, 0.95f), CrystalGlow = 0.3f,
            Active = new Color(0.45f, 0.85f, 1f), ActiveGlow = 2.2f,
            Core = new Color(0.85f, 0.97f, 1f), CoreGlow = 3f,
            Vent = new Color(1f, 0.6f, 0.35f)
        };

        private static CrystalPalette[] BuildPalettes()
        {
            CrystalPalette[] p = new CrystalPalette[7];
            // None falls back to the Shardwake look.
            p[1] = new CrystalPalette
            {
                Key = "shardwake",
                Structure = new Color(0.6f, 0.57f, 0.64f), StructureSmooth = 0.22f,
                Metal = new Color(0.76f, 0.77f, 0.8f), MetalSmooth = 0.7f, Metallic = 0.9f,
                Crystal = new Color(0.68f, 0.55f, 0.98f), CrystalGlow = 0.35f,
                Active = new Color(0.55f, 0.74f, 1f), ActiveGlow = 2.2f,
                Core = new Color(0.78f, 0.9f, 1f), CoreGlow = 3.2f,
                Vent = new Color(1f, 0.62f, 0.4f)
            };
            p[0] = p[1];
            p[2] = new CrystalPalette
            {
                Key = "prism",
                Structure = new Color(0.86f, 0.85f, 0.8f), StructureSmooth = 0.55f,
                Metal = new Color(0.78f, 0.8f, 0.84f), MetalSmooth = 0.8f, Metallic = 1f,
                Crystal = new Color(0.58f, 0.92f, 1f), CrystalGlow = 0.4f,
                Active = new Color(0.45f, 1f, 0.94f), ActiveGlow = 2.4f,
                Core = new Color(0.86f, 1f, 1f), CoreGlow = 3.5f,
                Vent = new Color(1f, 0.7f, 0.45f)
            };
            p[3] = new CrystalPalette
            {
                Key = "gyre",
                Structure = new Color(0.16f, 0.19f, 0.22f), StructureSmooth = 0.3f,
                Metal = new Color(0.72f, 0.42f, 0.25f), MetalSmooth = 0.6f, Metallic = 0.9f,
                Crystal = new Color(0.2f, 0.82f, 0.72f), CrystalGlow = 0.45f,
                Active = new Color(0.25f, 1f, 0.8f), ActiveGlow = 2.4f,
                Core = new Color(0.62f, 1f, 0.9f), CoreGlow = 3f,
                Vent = new Color(1f, 0.55f, 0.3f)
            };
            p[4] = new CrystalPalette
            {
                Key = "sunder",
                Structure = new Color(0.25f, 0.23f, 0.22f), StructureSmooth = 0.1f,
                Metal = new Color(0.78f, 0.6f, 0.3f), MetalSmooth = 0.55f, Metallic = 0.9f,
                Crystal = new Color(0.95f, 0.55f, 0.2f), CrystalGlow = 0.45f,
                Active = new Color(1f, 0.52f, 0.14f), ActiveGlow = 2.4f,
                Core = new Color(1f, 0.7f, 0.3f), CoreGlow = 3.4f,
                Vent = new Color(1f, 0.85f, 0.6f)
            };
            p[5] = new CrystalPalette
            {
                Key = "choir",
                Structure = new Color(0.8f, 0.7f, 0.55f), StructureSmooth = 0.25f,
                Metal = new Color(0.95f, 0.75f, 0.36f), MetalSmooth = 0.72f, Metallic = 1f,
                Crystal = new Color(0.94f, 0.45f, 0.8f), CrystalGlow = 0.4f,
                Active = new Color(1f, 0.42f, 0.86f), ActiveGlow = 2.4f,
                Core = new Color(1f, 0.78f, 0.95f), CoreGlow = 3.2f,
                Vent = new Color(1f, 0.7f, 0.45f)
            };
            p[6] = new CrystalPalette
            {
                Key = "mirror",
                Structure = new Color(0.07f, 0.07f, 0.09f), StructureSmooth = 0.62f,
                Metal = new Color(0.82f, 0.83f, 0.86f), MetalSmooth = 0.94f, Metallic = 1f,
                Crystal = new Color(0.3f, 0.24f, 0.46f), CrystalGlow = 0.5f,
                Active = new Color(0.76f, 0.7f, 1f), ActiveGlow = 2.6f,
                Core = new Color(0.9f, 0.86f, 1f), CoreGlow = 3.5f,
                Vent = new Color(1f, 0.62f, 0.5f)
            };
            return p;
        }

        /// <summary>The palette of a focus (None gets Shardwake's).</summary>
        public static CrystalPalette PaletteOf(CrystalBehavior b)
        {
            int i = (int)b;
            return i >= 0 && i < _palettes.Length ? _palettes[i] : _palettes[1];
        }

        /// <summary>The palette a shot body uses: the family that fires it.</summary>
        public static CrystalPalette PaletteOfShot(CrystalShotKind k)
        {
            return PaletteOf(BehaviorOfShot(k));
        }

        /// <summary>The family that fires a shot kind.</summary>
        public static CrystalBehavior BehaviorOfShot(CrystalShotKind k)
        {
            switch (k)
            {
                case CrystalShotKind.Disc: return CrystalBehavior.GyreCrescent;
                case CrystalShotKind.Orb: return CrystalBehavior.Sunderheart;
                case CrystalShotKind.ChoirShard: return CrystalBehavior.MirageChoir;
                default: return CrystalBehavior.Shardwake;
            }
        }

        /// <summary>The palette of the resonance case (its passive crystal slot is see-through glass).</summary>
        public static CrystalPalette PropPalette { get { return _propPalette; } }

        /// <summary>The palette of the practice targets (solid crystal post, glowing ring).</summary>
        public static CrystalPalette TargetPalette { get { return _targetPalette; } }
    }
}
