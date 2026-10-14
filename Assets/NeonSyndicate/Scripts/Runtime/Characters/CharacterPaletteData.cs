using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 16x16 character palette as plain data: albedo, metallic, fabric-detail strength, smoothness and emission per
    /// cell. Tex builds the runtime textures from it and the offline character tests export it for the preview viewer,
    /// so both always agree. Rows: 0 skin, 1 cloth, 2 hair (8..13 beard and stubble shades), 3 special, 4 neon,
    /// 5 armour and tech surfaces, 6 eyes, 7 lips, 11 shadow, glass and spirit surfaces, 12 stone, ice and sand
    /// (11 and 12 are the humanoid creatures; see Spirit and Stone).
    /// </summary>
    public static class CharacterPaletteData
    {
        public const int Size = 16;
        public const int BeardShadeOffset = 8;

        public static readonly Color[] Skin =
        {
            new Color(0.98f, 0.82f, 0.70f), new Color(0.92f, 0.72f, 0.56f), new Color(0.80f, 0.58f, 0.42f),
            new Color(0.62f, 0.42f, 0.28f), new Color(0.45f, 0.29f, 0.18f), new Color(0.30f, 0.19f, 0.12f)
        };

        public static readonly Color[] Cloth =
        {
            new Color(0.10f, 0.10f, 0.12f), new Color(0.85f, 0.85f, 0.88f), new Color(0.15f, 0.22f, 0.45f), new Color(0.55f, 0.08f, 0.10f),
            new Color(0.20f, 0.40f, 0.22f), new Color(0.60f, 0.50f, 0.32f), new Color(0.35f, 0.35f, 0.38f), new Color(0.85f, 0.55f, 0.15f),
            new Color(0.45f, 0.20f, 0.55f), new Color(0.12f, 0.50f, 0.60f), new Color(0.75f, 0.30f, 0.45f), new Color(0.28f, 0.20f, 0.14f),
            new Color(0.95f, 0.85f, 0.25f), new Color(0.05f, 0.30f, 0.30f), new Color(0.65f, 0.65f, 0.70f), new Color(0.18f, 0.12f, 0.30f)
        };

        /// <summary>Cloth columns whose colour sits close to a skin tone. Bodysuits avoid them so a suit never reads as bare skin.</summary>
        public static readonly int[] SkinLikeCloth = { 5, 11 };

        /// <summary>Armour columns that read as skin (bone, copper). Bodysuits avoid them too.</summary>
        public static readonly int[] SkinLikeArmor = { 4, 11, 12 };

        /// <summary>Hair colours 0..5; beard and stubble shades sit at the same column + BeardShadeOffset.</summary>
        public static readonly Color[] Hair =
        {
            new Color(0.05f, 0.04f, 0.03f), new Color(0.25f, 0.15f, 0.08f), new Color(0.50f, 0.32f, 0.15f),
            new Color(0.85f, 0.70f, 0.40f), new Color(0.55f, 0.55f, 0.55f), new Color(0.60f, 0.12f, 0.08f)
        };

        private static readonly Color[] BeardShades =
        {
            new Color(0.04f, 0.033f, 0.028f), new Color(0.17f, 0.105f, 0.06f), new Color(0.33f, 0.21f, 0.1f),
            new Color(0.55f, 0.43f, 0.25f), new Color(0.4f, 0.4f, 0.41f), new Color(0.3f, 0.1f, 0.055f)
        };

        public static readonly Color[] Special =
        {
            new Color(0.05f, 0.05f, 0.05f), new Color(0.3f, 0.18f, 0.1f), new Color(0.9f, 0.9f, 0.9f), new Color(0.08f, 0.12f, 0.28f),
            new Color(0.95f, 0.8f, 0.1f), new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.9f, 0.9f), new Color(0.15f, 0.15f, 0.15f),
            new Color(0.6f, 0.45f, 0.2f), new Color(0.2f, 0.2f, 0.25f), new Color(1f, 1f, 1f), new Color(0.1f, 0.6f, 0.2f),
            new Color(0.5f, 0.1f, 0.6f), new Color(0.95f, 0.45f, 0.1f), new Color(0.7f, 0.7f, 0.75f), new Color(0.2f, 0.4f, 0.9f)
        };

        public static readonly Color[] Neon =
        {
            new Color(1f, 0.15f, 0.65f), new Color(0.1f, 0.95f, 1f), new Color(1f, 0.85f, 0.1f), new Color(0.3f, 1f, 0.35f),
            new Color(0.7f, 0.25f, 1f), new Color(1f, 0.25f, 0.15f), new Color(1f, 0.55f, 0.1f), new Color(0.25f, 0.45f, 1f)
        };

        public static readonly Color[] Armor =
        {
            new Color(0.22f, 0.23f, 0.26f), new Color(0.78f, 0.8f, 0.84f), new Color(0.05f, 0.05f, 0.06f), new Color(0.88f, 0.89f, 0.9f),
            new Color(0.9f, 0.7f, 0.3f), new Color(0.12f, 0.12f, 0.13f), new Color(0.5f, 0.04f, 0.06f), new Color(0.04f, 0.35f, 0.38f),
            new Color(0.28f, 0.08f, 0.45f), new Color(0.08f, 0.12f, 0.25f), new Color(0.16f, 0.18f, 0.18f), new Color(0.8f, 0.76f, 0.66f),
            new Color(0.72f, 0.42f, 0.28f), new Color(0.03f, 0.03f, 0.04f), new Color(0.82f, 0.9f, 0.95f), new Color(0.06f, 0.06f, 0.065f)
        };

        /// <summary>Natural irises 0..5, sclera 6, dark 7, lit (glowing) irises 8..15.</summary>
        public static readonly Color[] Eyes =
        {
            new Color(0.32f, 0.2f, 0.1f), new Color(0.45f, 0.35f, 0.16f), new Color(0.2f, 0.42f, 0.22f), new Color(0.22f, 0.38f, 0.6f),
            new Color(0.42f, 0.45f, 0.48f), new Color(0.62f, 0.42f, 0.12f), new Color(0.93f, 0.92f, 0.9f), new Color(0.05f, 0.03f, 0.03f),
            new Color(0.1f, 0.95f, 1f), new Color(1f, 0.2f, 0.75f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 1f, 0.4f),
            new Color(0.7f, 0.35f, 1f), new Color(1f, 0.15f, 0.1f), new Color(1f, 0.55f, 0.1f), new Color(0.3f, 0.5f, 1f)
        };

        private static readonly float[] SpecialMetal = { 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f, 0.9f, 0f };
        private static readonly float[] SpecialSmooth = { 0.4f, 0.48f, 0.3f, 0.25f, 0.8f, 0.35f, 0.5f, 0.4f, 0.3f, 0.3f, 0.35f, 0.3f, 0.3f, 0.3f, 0.7f, 0.3f };
        private static readonly float[] SpecialDetail = { 0.4f, 0.5f, 0.8f, 1f, 0f, 0.6f, 0.5f, 0.7f, 0.8f, 0.8f, 0.6f, 0.8f, 0.8f, 0.8f, 0f, 0.8f };
        private static readonly float[] ArmorMetal = { 0.85f, 1f, 0.2f, 0f, 1f, 0.35f, 0.1f, 0.1f, 0.1f, 0.3f, 0.2f, 0f, 1f, 0.2f, 0f, 0f };
        private static readonly float[] ArmorSmooth = { 0.62f, 0.92f, 0.3f, 0.78f, 0.8f, 0.6f, 0.85f, 0.85f, 0.85f, 0.55f, 0.4f, 0.5f, 0.65f, 0.95f, 0.8f, 0.25f };
        private static readonly float[] ArmorDetail = { 0.25f, 0f, 0.5f, 0.1f, 0.05f, 0.9f, 0.05f, 0.05f, 0.05f, 0.3f, 0.6f, 0.2f, 0.1f, 0f, 0.1f, 0.5f };

        /// <summary>
        /// Row 11, shadow, glass and spirit surfaces: 0 Hollow skin (smoky black with a sheen), 1 Hollow smoke tatters,
        /// 2 Hollow seam glow (violet), 3 Hollow eye glow, 4..6 wraith glass (deep, mid, pale), 7 wraith rim glow (cyan),
        /// 8 wraith rim glow (white, also wraith eyes), 9 Djinn skin (deep blue), 10 Djinn gold, 11 Djinn sigil glow
        /// (amber), 12 Djinn sand, 13 Djinn smoke, 14 Djinn eye glow, 15 claw obsidian.
        /// </summary>
        public static readonly Color[] Spirit =
        {
            new Color(0.03f, 0.024f, 0.045f), new Color(0.014f, 0.011f, 0.02f), new Color(0.5f, 0.16f, 1f), new Color(1f, 0.8f, 1f),
            new Color(0.012f, 0.018f, 0.026f), new Color(0.05f, 0.078f, 0.1f), new Color(0.13f, 0.18f, 0.22f), new Color(0.16f, 0.86f, 1f),
            new Color(0.86f, 0.96f, 1f), new Color(0.07f, 0.13f, 0.36f), new Color(0.95f, 0.72f, 0.3f), new Color(1f, 0.62f, 0.16f),
            new Color(0.74f, 0.6f, 0.4f), new Color(0.16f, 0.17f, 0.24f), new Color(1f, 0.9f, 0.55f), new Color(0.02f, 0.018f, 0.026f)
        };

        /// <summary>
        /// Row 12, stone, ice and sand: 0 golem stone (dark), 1 golem stone (light facets), 2 crystal (cyan, softly lit),
        /// 3 core glow (cyan white), 4 sandstone, 5 sandstone (dark strata), 6 amber glow, 7 glacier ice, 8 deep ice,
        /// 9 frost glow (pale blue), 10 snow and frost white, 11..13 golem inner glow seen between the plates (cyan, amber,
        /// blue), 14 hazard yellow, 15 hazard black.
        /// </summary>
        public static readonly Color[] Stone =
        {
            new Color(0.11f, 0.115f, 0.13f), new Color(0.21f, 0.215f, 0.235f), new Color(0.3f, 0.88f, 1f), new Color(0.72f, 1f, 1f),
            new Color(0.68f, 0.47f, 0.29f), new Color(0.46f, 0.3f, 0.18f), new Color(1f, 0.6f, 0.18f), new Color(0.62f, 0.8f, 0.92f),
            new Color(0.22f, 0.42f, 0.62f), new Color(0.6f, 0.9f, 1f), new Color(0.86f, 0.9f, 0.95f), new Color(0.02f, 0.05f, 0.06f),
            new Color(0.06f, 0.035f, 0.015f), new Color(0.025f, 0.045f, 0.075f), new Color(0.95f, 0.74f, 0.08f), new Color(0.04f, 0.04f, 0.045f)
        };

        private static readonly float[] SpiritMetal = { 0.2f, 0f, 0f, 0f, 0.9f, 0.85f, 0.75f, 0f, 0f, 0.15f, 1f, 0f, 0f, 0f, 0f, 0.4f };
        private static readonly float[] SpiritSmooth = { 0.5f, 0.14f, 0.85f, 0.9f, 0.97f, 0.95f, 0.93f, 0.9f, 0.9f, 0.55f, 0.82f, 0.85f, 0.2f, 0.3f, 0.9f, 0.88f };
        private static readonly float[] SpiritDetail = { 0.35f, 0.2f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.15f, 0.05f, 0f, 0.9f, 0.5f, 0f, 0f };
        private static readonly float[] StoneMetal = { 0.05f, 0.05f, 0.2f, 0f, 0f, 0f, 0f, 0.1f, 0.15f, 0f, 0f, 0f, 0f, 0f, 0.1f, 0.05f };
        private static readonly float[] StoneSmooth = { 0.32f, 0.3f, 0.95f, 0.9f, 0.22f, 0.2f, 0.85f, 0.9f, 0.92f, 0.85f, 0.45f, 0.4f, 0.4f, 0.4f, 0.5f, 0.45f };
        private static readonly float[] StoneDetail = { 0.9f, 0.9f, 0f, 0f, 0.9f, 0.9f, 0f, 0.3f, 0.2f, 0f, 0.6f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f };

        /// <summary>Emitted colour of the lit creature cells (rows 11 and 12); black where a cell does not glow.</summary>
        private static readonly Color[] SpiritGlow =
        {
            Color.black, Color.black, new Color(0.46f, 0.12f, 1f), new Color(1f, 0.72f, 1f),
            Color.black, Color.black, Color.black, new Color(0.02f, 0.44f, 0.62f),
            new Color(0.86f, 0.96f, 1f), Color.black, Color.black, new Color(1f, 0.6f, 0.14f),
            Color.black, Color.black, new Color(1f, 0.88f, 0.5f), Color.black
        };

        private static readonly Color[] StoneGlow =
        {
            Color.black, Color.black, new Color(0.1f, 0.55f, 0.68f), new Color(0.6f, 1f, 1f),
            Color.black, Color.black, new Color(1f, 0.55f, 0.12f), Color.black,
            Color.black, new Color(0.45f, 0.8f, 1f), Color.black, new Color(0.02f, 0.25f, 0.31f),
            new Color(0.34f, 0.2f, 0.04f), new Color(0.06f, 0.2f, 0.4f), Color.black, Color.black
        };

        private static readonly Color Unused = new Color(0.5f, 0.5f, 0.5f);

        public static bool IsSkinLikeCloth(int column)
        {
            return System.Array.IndexOf(SkinLikeCloth, column) >= 0;
        }

        public static bool IsSkinLikeArmor(int column)
        {
            return System.Array.IndexOf(SkinLikeArmor, column) >= 0;
        }

        /// <summary>Base colour of a cell (sRGB).</summary>
        public static Color Albedo(int row, int col)
        {
            switch (row)
            {
                case 0: return col < Skin.Length ? Skin[col] : Unused;
                case 1: return col < Cloth.Length ? Cloth[col] : Unused;
                case 2:
                    if (col < Hair.Length) return Hair[col];
                    if (col >= BeardShadeOffset && col - BeardShadeOffset < BeardShades.Length) return BeardShades[col - BeardShadeOffset];
                    return Unused;
                case 3: return col < Special.Length ? Special[col] : Unused;
                case 4: return col < Neon.Length ? Neon[col] : Unused;
                case 5: return col < Armor.Length ? Armor[col] : Unused;
                case 6: return col < Eyes.Length ? Eyes[col] : Unused;
                case 7:
                    if (col >= Skin.Length) return Unused;
                    Color sk = Skin[col];
                    return new Color(sk.r * 0.82f, sk.g * 0.58f, sk.b * 0.6f);
                case 8: case 9: case 10: return CreatureAlbedo(row, col);
                case 11: return col < Spirit.Length ? Spirit[col] : Unused;
                case 12: return col < Stone.Length ? Stone[col] : Unused;
                default: return Unused;
            }
        }

        /// <summary>R metallic, G fabric-detail strength, B unused, A smoothness (linear).</summary>
        public static Color MetalDetailSmooth(int row, int col)
        {
            switch (row)
            {
                case 0: return new Color(0f, 0.12f, 0f, 0.42f);
                case 1: return new Color(0f, 1f, 0f, 0.22f);
                case 2: return new Color(0f, 0.35f, 0f, 0.52f);
                case 3: return new Color(SpecialMetal[col], SpecialDetail[col], 0f, SpecialSmooth[col]);
                case 4: return new Color(0f, 0f, 0f, 0.85f);
                case 5: return new Color(ArmorMetal[col], ArmorDetail[col], 0f, ArmorSmooth[col]);
                case 6: return new Color(0f, 0f, 0f, 0.92f);
                case 7: return new Color(0f, 0.1f, 0f, 0.58f);
                case 8: case 9: case 10: return CreatureMetalDetailSmooth(row, col);
                case 11: return new Color(SpiritMetal[col], SpiritDetail[col], 0f, SpiritSmooth[col]);
                case 12: return new Color(StoneMetal[col], StoneDetail[col], 0f, StoneSmooth[col]);
                default: return new Color(0f, 0.5f, 0f, 0.3f);
            }
        }

        /// <summary>Emitted colour of a cell: the neon row, the lit irises and the creature glows (seams, sigils,
        /// crystals, cores and rim lines in rows 11 and 12) glow, everything else is black.</summary>
        public static Color Emission(int row, int col)
        {
            if (row == 4 && col < Neon.Length) return Neon[col];
            if (row == 6 && col >= 8 && col < Eyes.Length) return Eyes[col];
            if (row >= 8 && row <= 10) return CreatureEmission(row, col);
            if (row == 11 && col < SpiritGlow.Length) return SpiritGlow[col];
            if (row == 12 && col < StoneGlow.Length) return StoneGlow[col];
            return Color.black;
        }

        // ------------------------------------------------------------------ creatures (rows 8, 9 and 10)

        /// <summary>
        /// Row 8: creature hides, scales, feathers and membranes. 0..2 Neon Jackal (two night coats and the muzzle and
        /// belly), 3..4 Frost Stalker (ice pelt, frost markings), 5..6 Crystal Basilisk (obsidian scales, dusky belly),
        /// 7..8 Violet Wyvern (hide, membrane), 9..10 Rime Griffin (frost feathers, steel-blue lion body), 11..12 Sun
        /// Griffin (gold feathers, tawny lion body), 13..14 Quartz Drake (rose-umber hide, dusk-rose membrane),
        /// 15 Dune Wyrm armour.
        /// </summary>
        public static readonly Color[] CreatureHide =
        {
            new Color(0.085f, 0.095f, 0.13f), new Color(0.115f, 0.09f, 0.135f), new Color(0.23f, 0.25f, 0.31f), new Color(0.72f, 0.77f, 0.83f),
            new Color(0.47f, 0.55f, 0.67f), new Color(0.11f, 0.095f, 0.15f), new Color(0.45f, 0.4f, 0.41f), new Color(0.1f, 0.06f, 0.15f),
            new Color(0.3f, 0.09f, 0.44f), new Color(0.69f, 0.73f, 0.8f), new Color(0.42f, 0.52f, 0.66f), new Color(0.86f, 0.62f, 0.24f),
            new Color(0.6f, 0.4f, 0.2f), new Color(0.29f, 0.165f, 0.155f), new Color(0.56f, 0.25f, 0.29f), new Color(0.66f, 0.51f, 0.31f)
        };

        /// <summary>
        /// Row 9: crystals and hard parts. 0..9 crystal (cyan, magenta, ice, amethyst, snow quartz, amber, rose quartz,
        /// citrine, smoky quartz, clear quartz) with a faint inner light; 10 obsidian claws, 11 ivory teeth, 12 gold beak
        /// and talons, 13 steel-blue beak and talons, 14 dark horn and leather, 15 black glass (noses, pupils).
        /// </summary>
        public static readonly Color[] CreatureCrystal =
        {
            new Color(0.35f, 0.9f, 1f), new Color(0.95f, 0.36f, 0.85f), new Color(0.68f, 0.86f, 1f), new Color(0.62f, 0.36f, 0.96f),
            new Color(0.9f, 0.95f, 1f), new Color(1f, 0.64f, 0.2f), new Color(1f, 0.55f, 0.66f), new Color(1f, 0.8f, 0.3f),
            new Color(0.26f, 0.22f, 0.31f), new Color(0.86f, 0.86f, 0.9f), new Color(0.05f, 0.05f, 0.07f), new Color(0.88f, 0.84f, 0.74f),
            new Color(0.9f, 0.68f, 0.25f), new Color(0.45f, 0.55f, 0.68f), new Color(0.18f, 0.135f, 0.115f), new Color(0.02f, 0.02f, 0.03f)
        };

        /// <summary>
        /// Row 10: creature glow (eyes, veins, seams). 0 cyan, 1 magenta, 2 ice blue, 3 violet, 4 frost white, 5 amber,
        /// 6 rose, 7 gold, 8 ember (burning mouths), 9 ember flesh (a dim throat), 10 venom green, 11 blood red, 12 white-hot
        /// (eye cores), 13 shade smoke (near black with a faint violet light), 14 dim cyan, 15 dim violet.
        /// </summary>
        public static readonly Color[] CreatureGlow =
        {
            new Color(0.15f, 0.95f, 1f), new Color(1f, 0.2f, 0.8f), new Color(0.45f, 0.75f, 1f), new Color(0.72f, 0.32f, 1f),
            new Color(0.78f, 0.9f, 1f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.4f, 0.56f), new Color(1f, 0.82f, 0.3f),
            new Color(1f, 0.36f, 0.08f), new Color(0.55f, 0.12f, 0.05f), new Color(0.4f, 1f, 0.45f), new Color(1f, 0.12f, 0.12f),
            new Color(1f, 0.97f, 0.92f), new Color(0.045f, 0.03f, 0.065f), new Color(0.12f, 0.6f, 0.66f), new Color(0.4f, 0.18f, 0.6f)
        };

        // Hides: fur is matte with a little fabric detail (reads as fine hair), scales and armour are glossier with strong
        // detail (the diamond quilt reads as scales), membranes are thin leather.
        private static readonly float[] CreatureHideMetal = { 0f, 0f, 0f, 0f, 0f, 0.05f, 0f, 0.05f, 0f, 0f, 0f, 0f, 0f, 0.05f, 0f, 0.1f };
        private static readonly float[] CreatureHideDetail = { 0.3f, 0.3f, 0.25f, 0.3f, 0.3f, 0.85f, 0.6f, 0.7f, 0.25f, 0.35f, 0.3f, 0.35f, 0.3f, 0.75f, 0.25f, 0.7f };
        private static readonly float[] CreatureHideSmooth = { 0.24f, 0.24f, 0.22f, 0.26f, 0.24f, 0.6f, 0.42f, 0.52f, 0.44f, 0.3f, 0.26f, 0.32f, 0.24f, 0.55f, 0.44f, 0.38f };
        // Crystal: glassy and slightly metallic so facets catch the city lights; claws and beaks polished; black glass mirror-smooth.
        private static readonly float[] CreatureCrystalMetal = { 0.35f, 0.35f, 0.35f, 0.35f, 0.3f, 0.35f, 0.35f, 0.35f, 0.4f, 0.3f, 0.15f, 0.05f, 0.9f, 0.75f, 0f, 0.2f };
        private static readonly float[] CreatureCrystalSmooth = { 0.93f, 0.93f, 0.93f, 0.93f, 0.9f, 0.93f, 0.93f, 0.93f, 0.9f, 0.94f, 0.82f, 0.62f, 0.72f, 0.7f, 0.38f, 0.97f };
        /// <summary>How strongly each crystal cell glows from inside (fraction of its colour); hard parts do not glow.</summary>
        private static readonly float[] CreatureCrystalInnerLight = { 0.24f, 0.24f, 0.22f, 0.24f, 0.16f, 0.24f, 0.22f, 0.22f, 0.12f, 0.14f, 0f, 0f, 0f, 0f, 0f, 0f };
        /// <summary>Emission strength of each glow cell (dim cells are inner light, not lamps).</summary>
        private static readonly float[] CreatureGlowStrength = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.45f, 1f, 1f, 1.2f, 0.9f, 0.4f, 0.45f };

        private static Color CreatureAlbedo(int row, int col)
        {
            if (col < 0 || col >= Size) return Unused;
            if (row == 8) return CreatureHide[col];
            if (row == 9) return CreatureCrystal[col];
            return CreatureGlow[col];
        }

        private static Color CreatureMetalDetailSmooth(int row, int col)
        {
            if (col < 0 || col >= Size) return new Color(0f, 0.5f, 0f, 0.3f);
            if (row == 8) return new Color(CreatureHideMetal[col], CreatureHideDetail[col], 0f, CreatureHideSmooth[col]);
            if (row == 9) return new Color(CreatureCrystalMetal[col], 0f, 0f, CreatureCrystalSmooth[col]);
            // Glow cells are glassy, except the shade's smoke (matte, with a little detail) and the dim ember flesh.
            if (col == 13) return new Color(0f, 0.4f, 0f, 0.12f);
            if (col == 9) return new Color(0f, 0.3f, 0f, 0.45f);
            return new Color(0f, 0f, 0f, 0.85f);
        }

        private static Color CreatureEmission(int row, int col)
        {
            if (col < 0 || col >= Size || row == 8) return Color.black;
            if (row == 9)
            {
                float k = CreatureCrystalInnerLight[col];
                Color c = CreatureCrystal[col];
                return new Color(c.r * k, c.g * k, c.b * k);
            }
            if (col == 13) return new Color(0.105f, 0.042f, 0.175f);
            Color g = CreatureGlow[col];
            float s = CreatureGlowStrength[col];
            return new Color(Mathf.Min(1f, g.r * s), Mathf.Min(1f, g.g * s), Mathf.Min(1f, g.b * s));
        }
    }
}
