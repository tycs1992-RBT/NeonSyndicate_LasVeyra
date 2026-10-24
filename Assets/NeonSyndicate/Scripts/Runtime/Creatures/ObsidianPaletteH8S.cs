using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal families of the owner's reference pack: which region a creature belongs to decides the colour of the
    /// shards that break out of its body and of its eyes. Azure for root predators and storm prowlers, Amethyst for ward
    /// guardians and mites, Veil blue violet for shades and spirits, Ember only for fire creatures and boss variants,
    /// and Rime, the Frostline's pale, iced end of the Veil blue.
    /// </summary>
    public enum CrystalFamilyH8S
    {
        Azure = 0,
        Amethyst = 1,
        Veil = 2,
        Ember = 3,
        Rime = 4
    }

    /// <summary>
    /// Palette rows 13, 14 and 15 of the shared character palette: the H8S creature look (dark obsidian, basalt and
    /// chitin bodies, regional crystal in three shades per family, and the glow cells for eyes, cores and accents).
    /// Rows 8 to 10 (the original creature cells) stay exactly as they were, so the companions, the drake's private
    /// surface and every saved look keep their colours. Plain data, read by CharacterPaletteData and the builder; the
    /// offline preview exports the same numbers. Colours are sRGB in the project's gamma working space.
    /// </summary>
    public static class ObsidianPaletteH8S
    {
        /// <summary>Row 13: the bodies.</summary>
        public const int BodyRow = 13;
        /// <summary>Row 14: the crystal shades, three per family (deep, mid, light), plus the obsidian horn cell.</summary>
        public const int CrystalRow = 14;
        /// <summary>Row 15: the glow cells (eyes and cores per family, dim seams, the ember throat) and the mouth darks.</summary>
        public const int GlowRow = 15;

        // ------------------------------------------------------------------ row 13: bodies

        public const int Obsidian = 0, Basalt = 1, ChitinDark = 2, ChitinPlate = 3, WeatheredStone = 4, UnderbellyDark = 5,
            AshHide = 6, RimeObsidian = 7, VeilObsidian = 8, AzureObsidian = 9, EmberChitin = 10, AmethystObsidian = 11,
            MembraneDark = 12, MembraneLit = 13, FeatherDark = 14, FeatherPale = 15;

        /// <summary>Body albedo (sRGB): volcanic glass, basalt and chitin darks from the Gothic Heart palette, lifted toward each region.</summary>
        public static readonly Color[] Body =
        {
            new Color(0.08f, 0.09f, 0.11f), new Color(0.2f, 0.22f, 0.25f), new Color(0.1f, 0.085f, 0.08f), new Color(0.18f, 0.16f, 0.145f),
            new Color(0.4f, 0.357f, 0.306f), new Color(0.12f, 0.125f, 0.14f), new Color(0.2f, 0.2f, 0.22f), new Color(0.3f, 0.36f, 0.44f),
            new Color(0.12f, 0.1f, 0.18f), new Color(0.08f, 0.135f, 0.15f), new Color(0.16f, 0.09f, 0.07f), new Color(0.15f, 0.105f, 0.175f),
            new Color(0.1f, 0.1f, 0.13f), new Color(0.16f, 0.14f, 0.2f), new Color(0.13f, 0.14f, 0.17f), new Color(0.3f, 0.32f, 0.36f)
        };

        private static readonly float[] BodyMetal = { 0.1f, 0.05f, 0.08f, 0.08f, 0f, 0f, 0f, 0.05f, 0.1f, 0.1f, 0.08f, 0.1f, 0f, 0f, 0f, 0f };
        /// <summary>Detail-normal strength per body cell: plates and scales strong, membranes and feathers light.</summary>
        private static readonly float[] BodyDetail = { 0.9f, 0.95f, 0.85f, 0.8f, 0.95f, 0.6f, 0.8f, 0.8f, 0.85f, 0.85f, 0.85f, 0.85f, 0.3f, 0.3f, 0.45f, 0.45f };
        private static readonly float[] BodySmooth = { 0.58f, 0.3f, 0.52f, 0.5f, 0.25f, 0.3f, 0.32f, 0.5f, 0.56f, 0.56f, 0.48f, 0.56f, 0.42f, 0.4f, 0.3f, 0.3f };

        // ------------------------------------------------------------------ row 14: crystal

        /// <summary>Shades within a family (column offset).</summary>
        public const int Deep = 0, Mid = 1, Light = 2;
        /// <summary>Column 15: dark obsidian horn and claw.</summary>
        public const int ObsidianHorn = 15;

        /// <summary>Crystal albedo: three shades per family in family order, then the horn cell.</summary>
        public static readonly Color[] Crystal =
        {
            Hex(0x17, 0x8d, 0x98), Hex(0x46, 0xc4, 0xc8), Hex(0x9b, 0xdb, 0xd3),
            Hex(0x64, 0x3f, 0x83), Hex(0x9d, 0x67, 0xc1), Hex(0xc6, 0xa2, 0xe4),
            Hex(0x39, 0x4e, 0x74), Hex(0x7f, 0x86, 0xbb), Hex(0xc0, 0xb9, 0xde),
            Hex(0x3a, 0x0d, 0x0a), Hex(0x7a, 0x1d, 0x12), Hex(0xc4, 0x40, 0x2a),
            Hex(0x6f, 0x8d, 0xb0), Hex(0xa9, 0xc6, 0xe2), Hex(0xe2, 0xf0, 0xfb),
            new Color(0.09f, 0.085f, 0.1f)
        };

        /// <summary>Inner light per shade (fraction of the albedo that emits): deep shards barely, tips the most. The legacy crystal row used 0.24 everywhere.</summary>
        public static readonly float[] CrystalInnerLight = { 0.05f, 0.09f, 0.16f };
        public const float CrystalMetal = 0.25f, CrystalSmooth = 0.93f, HornMetal = 0.2f, HornSmooth = 0.85f;

        // ------------------------------------------------------------------ row 15: glow and mouth darks

        /// <summary>Column of a family's eye and core glow (0..4), its dim seam glow (6..10).</summary>
        public const int GlowOffset = 0, DimOffset = 6;
        public const int WhiteCore = 5, EmberThroat = 11, MouthDark = 12, PupilGlass = 13, TelegraphAmber = 14;

        /// <summary>Glow albedo: the lit colour of eyes and cores per family, the white-hot core, dim seams, the ember throat, then the darks.</summary>
        public static readonly Color[] Glow =
        {
            new Color(0.42f, 0.92f, 0.92f), new Color(0.72f, 0.45f, 0.96f), new Color(0.6f, 0.64f, 0.98f), new Color(1f, 0.42f, 0.14f), new Color(0.86f, 0.94f, 1f),
            new Color(1f, 0.97f, 0.92f),
            new Color(0.2f, 0.5f, 0.52f), new Color(0.38f, 0.22f, 0.52f), new Color(0.3f, 0.32f, 0.54f), new Color(0.55f, 0.2f, 0.07f), new Color(0.48f, 0.54f, 0.6f),
            new Color(0.6f, 0.14f, 0.05f), new Color(0.07f, 0.035f, 0.04f), new Color(0.015f, 0.015f, 0.02f), new Color(1f, 0.75f, 0.3f), new Color(0.5f, 0.5f, 0.5f)
        };

        /// <summary>Emission strength per glow cell (fraction of the albedo): full for eyes and cores, low for seams, none for the darks.</summary>
        public static readonly float[] GlowStrength = { 1f, 1f, 1f, 1f, 1f, 1.1f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.5f, 0f, 0f, 1f, 0f };

        // ------------------------------------------------------------------ cell lookups (palette coordinates: x column, y row)

        public static Vector2Int BodyCell(int column) { return new Vector2Int(Mathf.Clamp(column, 0, 15), BodyRow); }

        /// <summary>The crystal cell of a family at a shade (Deep, Mid or Light).</summary>
        public static Vector2Int CrystalCell(CrystalFamilyH8S family, int shade)
        {
            return new Vector2Int((int)family * 3 + Mathf.Clamp(shade, 0, 2), CrystalRow);
        }

        public static Vector2Int HornCell { get { return new Vector2Int(ObsidianHorn, CrystalRow); } }

        /// <summary>The eye and core glow of a family.</summary>
        public static Vector2Int GlowCell(CrystalFamilyH8S family) { return new Vector2Int(GlowOffset + (int)family, GlowRow); }

        /// <summary>The dim seam glow of a family (veins, inner seams: an inner light, never a lamp).</summary>
        public static Vector2Int DimCell(CrystalFamilyH8S family) { return new Vector2Int(DimOffset + (int)family, GlowRow); }

        public static Vector2Int WhiteCoreCell { get { return new Vector2Int(WhiteCore, GlowRow); } }
        public static Vector2Int EmberThroatCell { get { return new Vector2Int(EmberThroat, GlowRow); } }
        public static Vector2Int MouthDarkCell { get { return new Vector2Int(MouthDark, GlowRow); } }
        public static Vector2Int PupilCell { get { return new Vector2Int(PupilGlass, GlowRow); } }

        /// <summary>The regional body hide of a family (the obsidian that took the region's stain).</summary>
        public static int RegionalBody(CrystalFamilyH8S family)
        {
            switch (family)
            {
                case CrystalFamilyH8S.Azure: return AzureObsidian;
                case CrystalFamilyH8S.Amethyst: return AmethystObsidian;
                case CrystalFamilyH8S.Veil: return VeilObsidian;
                case CrystalFamilyH8S.Ember: return EmberChitin;
                default: return RimeObsidian;
            }
        }

        // ------------------------------------------------------------------ the palette rows (CharacterPaletteData calls these)

        /// <summary>True for the three H8S rows.</summary>
        public static bool Owns(int row) { return row >= BodyRow && row <= GlowRow; }

        public static Color Albedo(int row, int col)
        {
            if (col < 0 || col >= 16) return new Color(0.5f, 0.5f, 0.5f);
            if (row == BodyRow) return Body[col];
            if (row == CrystalRow) return Crystal[col];
            return Glow[col];
        }

        /// <summary>R metallic, G detail strength, B unused, A smoothness.</summary>
        public static Color MetalDetailSmooth(int row, int col)
        {
            if (col < 0 || col >= 16) return new Color(0f, 0.5f, 0f, 0.3f);
            if (row == BodyRow) return new Color(BodyMetal[col], BodyDetail[col], 0f, BodySmooth[col]);
            if (row == CrystalRow)
            {
                if (col == ObsidianHorn) return new Color(HornMetal, 0f, 0f, HornSmooth);
                return new Color(CrystalMetal, 0f, 0f, CrystalSmooth);
            }
            // Glow cells are glassy; the mouth dark is matte flesh.
            if (col == MouthDark) return new Color(0f, 0.4f, 0f, 0.2f);
            if (col == PupilGlass) return new Color(0.1f, 0f, 0f, 0.95f);
            return new Color(0f, 0f, 0f, 0.85f);
        }

        public static Color Emission(int row, int col)
        {
            if (col < 0 || col >= 16 || row == BodyRow) return Color.black;
            if (row == CrystalRow)
            {
                if (col == ObsidianHorn) return Color.black;
                float k = CrystalInnerLight[col % 3];
                Color c = Crystal[col];
                return new Color(c.r * k, c.g * k, c.b * k);
            }
            float s = GlowStrength[col];
            if (s <= 0f) return Color.black;
            Color g = Glow[col];
            return new Color(Mathf.Min(1f, g.r * s), Mathf.Min(1f, g.g * s), Mathf.Min(1f, g.b * s));
        }

        private static Color Hex(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
