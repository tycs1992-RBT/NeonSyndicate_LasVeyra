using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The headpiece a live body wears, chosen from the person's Appearance, role and (for the cast) costume.</summary>
    public enum LiveHeadpiece
    {
        None = 0,
        /// <summary>A soft peaked cap in the person's hat colour (civilians, clerks, gang members in gang colours).</summary>
        Cap = 1,
        /// <summary>A Veil-navy officer's cap with a band, a dark peak and a bronze badge (the police).</summary>
        OfficerCap = 2,
        /// <summary>A knitted cap pulled over the ear tops with a folded cuff.</summary>
        Beanie = 3,
        /// <summary>A cloth tied over the crown with a knot and two tails at the nape (gang members).</summary>
        Bandana = 4,
        /// <summary>A deep hood with a cowl over the collar (gang members and mission enemies who would wear a beanie).</summary>
        Hood = 5,
        /// <summary>The closed sentinel helm: a steel shell over the whole head with a lit visor slit and a neck guard (the Ward Sentinels).</summary>
        Helm = 6,
        /// <summary>An open steel work helmet with a short peak (civilians and cast members whose look says Helmet).</summary>
        WorkHelm = 7,
        /// <summary>A tall-crowned hat with a wide brim (the cast's brim hat).</summary>
        BrimHat = 8,
        /// <summary>Goggles pushed up onto the forehead on a strap, lenses lit (the cast's goggles).</summary>
        Goggles = 9,
        /// <summary>A thin circlet with a lit stone at the brow (the cast's circlet).</summary>
        Circlet = 10,
        /// <summary>A dispatcher's headset: a band over the crown, ear cups and a lit microphone boom (the cast's headset).</summary>
        Headset = 11,
        /// <summary>Thin spectacles (people whose look says Glasses and who wear nothing else on the head).</summary>
        Glasses = 12
    }

    /// <summary>How a headpiece treats the hair under it.</summary>
    public enum LiveHairCover
    {
        /// <summary>Nothing on the head: the full style shows.</summary>
        None = 0,
        /// <summary>A cap, beanie or bandana: only the hair below the piece's edge shows (a long drape, the sides, the nape).</summary>
        Capped = 1,
        /// <summary>A closed helm or a hood: no hair shows.</summary>
        Hidden = 2
    }

    /// <summary>The beard a live body wears.</summary>
    public enum LiveBeard
    {
        None = 0,
        /// <summary>A few days' growth flat on the skin.</summary>
        Stubble = 1,
        /// <summary>A short full beard with a little volume at the chin.</summary>
        Short = 2
    }

    /// <summary>
    /// The colours of one person's hair, beard and headpiece on a live body, as tints of the greyscale strand textures
    /// and the headwear materials (LiveHairRules.Colors).
    /// </summary>
    public struct LiveHeadTint
    {
        /// <summary>The hair strands, and the tips (equal to Hair unless a gang member carries a dyed accent).</summary>
        public Color Hair, Tips;
        /// <summary>The beard shade.</summary>
        public Color Beard;
        /// <summary>The headpiece's cloth or leather, its metal and its trim, and the emission of lit trim (black for most pieces).</summary>
        public Color HeadCloth, HeadMetal, HeadTrim, HeadGlow;
    }

    /// <summary>
    /// Engine-free rules for hair, beards and headwear on live bodies (owner direction in chat 2026-10-23: swap NPCs and
    /// enemies for realistic bodies): the natural hair colour a person's palette index stands for, the rare dyed tips of
    /// gang members, which headpiece a role and look wear, how the headpiece covers the hair, who gets which beard, and
    /// the headpiece colours. Deterministic from the Appearance, the role and the gang, so a person keeps the same head
    /// every time a body is bound.
    /// </summary>
    public static class LiveHairRules
    {
        /// <summary>The believable natural hair colours, in sRGB: black, dark brown, brown, auburn, red, blond, ash, grey, white.</summary>
        public static readonly Color[] Naturals =
        {
            new Color(0.075f, 0.065f, 0.06f), new Color(0.21f, 0.13f, 0.08f), new Color(0.40f, 0.26f, 0.15f),
            new Color(0.47f, 0.22f, 0.12f), new Color(0.58f, 0.20f, 0.10f), new Color(0.80f, 0.66f, 0.40f),
            new Color(0.62f, 0.58f, 0.50f), new Color(0.56f, 0.56f, 0.57f), new Color(0.86f, 0.85f, 0.82f)
        };
        public const int Black = 0, DarkBrown = 1, Brown = 2, Auburn = 3, Red = 4, Blond = 5, Ash = 6, Grey = 7, White = 8;

        /// <summary>The strands are brighter than the palette colour because the strand texture averages below one.</summary>
        public const float StrandLift = 1.18f;
        /// <summary>The implant bit of Appearance that marks dyed hair tips (only gang members show it on a live body).</summary>
        public const int DyedTipsBit = 16;

        /// <summary>
        /// The natural colour index of a person: the hair palette column (0 black, 1 dark brown, 2 brown, 3 blond, 4 grey,
        /// 5 red) with a second shade chosen by the person's other indices, so six palette entries become nine naturals.
        /// </summary>
        public static int NaturalIndex(Appearance look)
        {
            int column = Wrap(look.Hair, 6);
            bool second = ((look.Shirt ^ look.Pants) & 1) != 0;
            switch (column)
            {
                case 0: return Black;
                case 1: return second ? Brown : DarkBrown;
                case 2: return second ? Auburn : Brown;
                case 3: return second ? Ash : Blond;
                case 4: return second ? White : Grey;
                default: return second ? Auburn : Red;
            }
        }

        /// <summary>True when a person's hair shows dyed tips: a gang member whose look carries the dyed-tips implant bit.</summary>
        public static bool DyedTips(NpcRole role, Appearance look)
        {
            return role == NpcRole.Gang && (look.Implant & DyedTipsBit) != 0;
        }

        /// <summary>
        /// The colours of a person's head. Hair is the natural colour lifted for the strand texture; the tips take the gang
        /// colour (brightened) only for a gang member with the dyed-tips bit; the beard is the hair a little darker; the
        /// headpiece follows the role: Veil navy and bronze for the officer's cap, near-black steel with a faint Veil-blue
        /// visor for the sentinel helm, the gang colour for gang cloth, the person's hat palette colour otherwise.
        /// </summary>
        public static LiveHeadTint Colors(NpcRole role, Appearance look, Color gangColor, bool hasGang, LiveHeadpiece piece)
        {
            LiveHeadTint t = new LiveHeadTint();
            Color natural = Naturals[NaturalIndex(look)];
            t.Hair = Opaque(natural * StrandLift);
            t.Tips = t.Hair;
            if (DyedTips(role, look))
            {
                Color gang = hasGang ? gangColor : new Color(0.62f, 0.4f, 0.76f);
                t.Tips = Opaque(Color.Lerp(gang, Color.white, 0.25f) * 1.3f);
            }
            t.Beard = Opaque(natural * (StrandLift * 0.82f));
            Color hat = CharacterPaletteData.Cloth[Wrap(look.Hat, CharacterPaletteData.Cloth.Length)];
            t.HeadCloth = Opaque(hat);
            t.HeadMetal = new Color(0.38f, 0.39f, 0.42f, 1f);
            t.HeadTrim = new Color(0.62f, 0.45f, 0.25f, 1f);
            t.HeadGlow = Color.black;
            switch (role)
            {
                case NpcRole.Police:
                    t.HeadCloth = new Color(0.11f, 0.16f, 0.27f, 1f);
                    t.HeadMetal = new Color(0.08f, 0.08f, 0.1f, 1f);
                    t.HeadTrim = new Color(0.72f, 0.56f, 0.3f, 1f);
                    break;
                case NpcRole.Swat:
                    t.HeadCloth = new Color(0.09f, 0.1f, 0.12f, 1f);
                    t.HeadMetal = new Color(0.1f, 0.11f, 0.13f, 1f);
                    t.HeadTrim = new Color(0.25f, 0.32f, 0.45f, 1f);
                    if (piece == LiveHeadpiece.Helm) t.HeadGlow = new Color(0.14f, 0.3f, 0.6f, 1f);
                    break;
                case NpcRole.Gang:
                    if (hasGang) t.HeadCloth = Opaque(Color.Lerp(gangColor, new Color(0.12f, 0.12f, 0.14f), 0.35f));
                    t.HeadTrim = Opaque(Color.Lerp(t.HeadTrim, hasGang ? gangColor : t.HeadTrim, 0.4f));
                    break;
                case NpcRole.Enemy:
                    t.HeadCloth = Opaque(Color.Lerp(hat, new Color(0.14f, 0.12f, 0.16f), 0.6f));
                    break;
            }
            if (piece == LiveHeadpiece.Hood) t.HeadCloth = Opaque(Color.Lerp(t.HeadCloth, new Color(0.1f, 0.1f, 0.12f), 0.5f));
            if (piece == LiveHeadpiece.Glasses) t.HeadMetal = new Color(0.12f, 0.12f, 0.13f, 1f);
            return t;
        }

        /// <summary>
        /// The colours of a named cast member's head from their procedural costume colours: cloth in the costume's main
        /// colour, trim in the accent, lit pieces (goggles, circlet, headset) glowing in the accent as the costume did.
        /// </summary>
        public static LiveHeadTint CastColors(Appearance look, Color main, Color accent, LiveHeadpiece piece)
        {
            LiveHeadTint t = Colors(NpcRole.Civilian, look, Color.black, false, piece);
            t.HeadCloth = Opaque(main);
            t.HeadTrim = Opaque(accent);
            t.HeadMetal = Opaque(Color.Lerp(main, new Color(0.3f, 0.3f, 0.32f), 0.6f));
            if (piece == LiveHeadpiece.Goggles || piece == LiveHeadpiece.Circlet || piece == LiveHeadpiece.Headset) t.HeadGlow = Opaque(accent * 1.5f);
            if (piece == LiveHeadpiece.Glasses) t.HeadMetal = new Color(0.12f, 0.12f, 0.13f, 1f);
            return t;
        }

        /// <summary>
        /// The headpiece a role and look wear on a live body. The police wear the officer's cap and the Sentinels the
        /// closed helm whatever their look says; a gang member or mission enemy who would wear a beanie wears a hood;
        /// a helmet on anyone else is the open work helmet; glasses count only when nothing else sits on the head.
        /// </summary>
        public static LiveHeadpiece Headpiece(NpcRole role, Appearance look)
        {
            if (role == NpcRole.Police) return LiveHeadpiece.OfficerCap;
            if (role == NpcRole.Swat) return LiveHeadpiece.Helm;
            bool rough = role == NpcRole.Gang || role == NpcRole.Enemy;
            switch (look.Headwear)
            {
                case Headwear.Cap: return LiveHeadpiece.Cap;
                case Headwear.Beanie: return rough ? LiveHeadpiece.Hood : LiveHeadpiece.Beanie;
                case Headwear.Helmet: return LiveHeadpiece.WorkHelm;
                case Headwear.PoliceCap: return LiveHeadpiece.OfficerCap;
                case Headwear.Bandana: return LiveHeadpiece.Bandana;
            }
            return look.Glasses ? LiveHeadpiece.Glasses : LiveHeadpiece.None;
        }

        /// <summary>
        /// The headpiece of a named cast member: the costume's head layer first (hood, brim hat, headset, goggles, circlet),
        /// then the look's headwear, then glasses.
        /// </summary>
        public static LiveHeadpiece CastHeadpiece(CostumeHead costume, Appearance look)
        {
            switch (costume)
            {
                case CostumeHead.Hood: return LiveHeadpiece.Hood;
                case CostumeHead.BrimHat: return LiveHeadpiece.BrimHat;
                case CostumeHead.Headset: return LiveHeadpiece.Headset;
                case CostumeHead.Goggles: return LiveHeadpiece.Goggles;
                case CostumeHead.Circlet: return LiveHeadpiece.Circlet;
            }
            return Headpiece(NpcRole.Civilian, look);
        }

        /// <summary>How a headpiece covers the hair: caps, beanies, bandanas, work helmets and brim hats leave the hair below their edge; helms and hoods hide it all.</summary>
        public static LiveHairCover Cover(LiveHeadpiece piece)
        {
            switch (piece)
            {
                case LiveHeadpiece.Cap:
                case LiveHeadpiece.OfficerCap:
                case LiveHeadpiece.Beanie:
                case LiveHeadpiece.Bandana:
                case LiveHeadpiece.WorkHelm:
                case LiveHeadpiece.BrimHat:
                    return LiveHairCover.Capped;
                case LiveHeadpiece.Helm:
                case LiveHeadpiece.Hood:
                    return LiveHairCover.Hidden;
                default:
                    return LiveHairCover.None;
            }
        }

        /// <summary>The beard a look wears: none for women and clean-shaven men, stubble or a short full beard by the look's other indices.</summary>
        public static LiveBeard Beard(Appearance look)
        {
            if (look.Female || !look.Beard) return LiveBeard.None;
            return ((look.Skin + look.Shoes) & 1) == 0 ? LiveBeard.Short : LiveBeard.Stubble;
        }

        /// <summary>The hair style a live body shows: the look's style, or none under a closed headpiece.</summary>
        public static HairStyle Style(Appearance look, LiveHairCover cover)
        {
            if (cover == LiveHairCover.Hidden) return HairStyle.Bald;
            return look.HairStyle;
        }

        /// <summary>True when a style keeps hair below a cap line (a cap over a bald head or a closed helm shows none).</summary>
        public static bool ShowsHair(HairStyle style, LiveHairCover cover)
        {
            return style != HairStyle.Bald && cover != LiveHairCover.Hidden;
        }

        private static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        private static int Wrap(int value, int count)
        {
            int m = value % count;
            return m < 0 ? m + count : m;
        }
    }
}
