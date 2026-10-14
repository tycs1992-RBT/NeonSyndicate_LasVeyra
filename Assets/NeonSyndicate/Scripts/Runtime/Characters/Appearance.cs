namespace NeonSyndicate
{
    public enum HairStyle { Bald, Short, Long, Mohawk, Bun, Undercut, Braids }
    public enum Headwear { None, Cap, Beanie, Helmet, PoliceCap, Bandana }

    /// <summary>
    /// How a person (or a humanoid creature, see Creature) looks. Colors are palette indices (see Tex.CharacterPalette). Rows: 0 skin, 1 cloth, 2 hair,
    /// 3 special, 4 neon, 5 armour and tech surfaces, 6 eyes, 7 lips, 11 and 12 creature surfaces (see CharacterPaletteData). Las Veyra is a city of enhanced humans: most people
    /// wear techwear with glowing seams, many carry visible implants (temple lines, lit eyes, jaw circuits).
    /// Pure data with no engine calls, so it is shared by the runtime and the offline character tests.
    /// </summary>
    public struct Appearance
    {
        public int Skin;
        public int ShirtRow, Shirt;
        public int PantsRow, Pants;
        public int ShoesRow, Shoes;
        public int Hair;
        public HairStyle HairStyle;
        public Headwear Headwear;
        public int HatRow, Hat;
        public bool Jacket;
        public int JacketRow, JacketColor;
        public bool LongSleeves;
        public bool Glasses;
        public bool Beard;
        public bool Female;
        public float Height;
        public float Bulk;

        /// <summary>0 street clothes, 1 techwear (panels and seams), 2 bodysuit, 3 tactical armour.</summary>
        public int SuitStyle;
        /// <summary>0 none, otherwise neon palette column + 1 for seams, implants and visor light.</summary>
        public int Glow;
        /// <summary>Implant bits: 1 temple line, 2 lit eyes, 4 jaw circuit, 8 forearm light strip, 16 neon-dyed hair tips.
        /// Creatures other than wraiths use it as a shape-variation seed instead (wraiths keep their source's implants).</summary>
        public int Implant;
        /// <summary>Iris palette column in row 6 (0..7 natural, 8..15 lit).</summary>
        public int EyeColor;
        /// <summary>Gear overlays: 0 none. Visor 1 band, 2 full face. Shell 1 chest plate, 2 plus shoulders, 3 full carapace.</summary>
        public int VisorStyle, ShellStyle, GauntletStyle, StriderStyle;
        /// <summary>Armour plate colour (row 5).</summary>
        public int ArmorColor;

        /// <summary>A humanoid creature on the people rig (Hollows, wraiths, the Djinn, golems), or None for a person.
        /// Build creature looks with the factories below (Hollow, SnowHollow, MirrorWraith, MirageDjinn, Golem, WraithQueen).</summary>
        public HumanoidCreature Creature;
        /// <summary>
        /// Creature size relative to a person of Height 1 (about 1.85 m): the rig, the body and the pose all scale with
        /// Height times this. 0 counts as 1 (C# 9 structs cannot initialise fields, so the factories always set it).
        /// </summary>
        public float CreatureScale;

        /// <summary>Field-by-field equality (used to match a spawn with the body built for it ahead of time).</summary>
        public bool SameAs(Appearance o)
        {
            return Skin == o.Skin && ShirtRow == o.ShirtRow && Shirt == o.Shirt && PantsRow == o.PantsRow && Pants == o.Pants
                && ShoesRow == o.ShoesRow && Shoes == o.Shoes && Hair == o.Hair && HairStyle == o.HairStyle && Headwear == o.Headwear
                && HatRow == o.HatRow && Hat == o.Hat && Jacket == o.Jacket && JacketRow == o.JacketRow && JacketColor == o.JacketColor
                && LongSleeves == o.LongSleeves && Glasses == o.Glasses && Beard == o.Beard && Female == o.Female
                && Height == o.Height && Bulk == o.Bulk && SuitStyle == o.SuitStyle && Glow == o.Glow && Implant == o.Implant
                && EyeColor == o.EyeColor && VisorStyle == o.VisorStyle && ShellStyle == o.ShellStyle && GauntletStyle == o.GauntletStyle
                && StriderStyle == o.StriderStyle && ArmorColor == o.ArmorColor
                && Creature == o.Creature && CreatureScale == o.CreatureScale;
        }

        public static Appearance Random(System.Random r)
        {
            Appearance a = new Appearance();
            a.Skin = r.Next(0, 6);
            a.ShirtRow = 1; a.Shirt = r.Next(0, 16);
            a.PantsRow = 1; a.Pants = r.Next(0, 16);
            a.ShoesRow = 3; a.Shoes = r.Next(0, 3);
            a.Hair = r.Next(0, 6);
            a.Female = r.NextDouble() < 0.45;
            if (a.Female)
            {
                double h = r.NextDouble();
                a.HairStyle = h < 0.5 ? HairStyle.Long : (h < 0.72 ? HairStyle.Bun : (h < 0.88 ? HairStyle.Braids : HairStyle.Undercut));
            }
            else
            {
                double h = r.NextDouble();
                a.HairStyle = h < 0.12 ? HairStyle.Bald : (h < 0.55 ? HairStyle.Short : (h < 0.7 ? HairStyle.Undercut : (h < 0.82 ? HairStyle.Mohawk : (h < 0.92 ? HairStyle.Braids : HairStyle.Long))));
            }
            a.Headwear = r.NextDouble() < 0.14 ? (r.NextDouble() < 0.6 ? Headwear.Cap : Headwear.Beanie) : Headwear.None;
            a.HatRow = 1; a.Hat = r.Next(0, 16);
            a.Jacket = r.NextDouble() < 0.38;
            a.JacketRow = 1; a.JacketColor = r.Next(0, 16);
            a.LongSleeves = a.Jacket || r.NextDouble() < 0.4;
            a.Glasses = r.NextDouble() < 0.12;
            a.Beard = !a.Female && r.NextDouble() < 0.25;
            a.Height = (float)(0.93 + r.NextDouble() * 0.14);
            a.Bulk = (float)(0.85 + r.NextDouble() * 0.35);
            // Enhanced-human city: techwear and implants are everyday.
            double s = r.NextDouble();
            a.SuitStyle = s < 0.42 ? 0 : (s < 0.82 ? 1 : 2);
            a.Glow = a.SuitStyle > 0 || r.NextDouble() < 0.35 ? 1 + r.Next(0, 8) : 0;
            a.Implant = r.NextDouble() < 0.55 ? (1 << r.Next(0, 4)) | (r.NextDouble() < 0.25 ? 2 : 0) : 0;
            if (r.NextDouble() < 0.22) a.Implant |= 16;
            a.EyeColor = (a.Implant & 2) != 0 ? 8 + r.Next(0, 8) : r.Next(0, 6);
            if (a.SuitStyle == 2)
            {
                // A bodysuit in a skin-like colour reads as bare skin, so those colours are swapped out.
                a.ShirtRow = r.NextDouble() < 0.5 ? 5 : 1;
                if (a.ShirtRow == 5)
                {
                    a.Shirt = r.Next(0, 6);
                    if (CharacterPaletteData.IsSkinLikeArmor(a.Shirt)) a.Shirt = 0;
                }
                else
                {
                    a.Shirt = r.Next(0, 16);
                    if (CharacterPaletteData.IsSkinLikeCloth(a.Shirt)) a.Shirt = (a.Shirt + 3) % 16;
                }
                a.PantsRow = a.ShirtRow;
                a.Pants = a.Shirt;
            }
            a.ArmorColor = r.Next(0, 6);
            return a;
        }

        public static Appearance Police()
        {
            return Police(new System.Random(UnityEngine.Random.Range(0, int.MaxValue)));
        }

        public static Appearance Police(System.Random r)
        {
            Appearance a = new Appearance();
            a.Skin = r.Next(0, 6);
            a.ShirtRow = 3; a.Shirt = 3;
            a.PantsRow = 3; a.Pants = 3;
            a.ShoesRow = 3; a.Shoes = 0;
            a.Hair = r.Next(0, 5);
            a.HairStyle = HairStyle.Short;
            a.Headwear = Headwear.PoliceCap;
            a.HatRow = 3; a.Hat = 3;
            a.LongSleeves = r.NextDouble() < 0.5;
            a.Height = 0.97f + (float)r.NextDouble() * 0.1f;
            a.Bulk = 1f + (float)r.NextDouble() * 0.2f;
            a.Female = r.NextDouble() < 0.25;
            if (a.Female) a.HairStyle = HairStyle.Bun;
            a.SuitStyle = 3;
            a.Glow = 8;
            a.ShellStyle = 1;
            a.ArmorColor = 9;
            a.VisorStyle = r.NextDouble() < 0.3 ? 1 : 0;
            a.EyeColor = r.Next(0, 6);
            return a;
        }

        public static Appearance Swat()
        {
            return Swat(new System.Random(UnityEngine.Random.Range(0, int.MaxValue)));
        }

        public static Appearance Swat(System.Random r)
        {
            Appearance a = Police(r);
            a.ShirtRow = 3; a.Shirt = 7;
            a.PantsRow = 3; a.Pants = 7;
            a.Headwear = Headwear.Helmet;
            a.HatRow = 3; a.Hat = 7;
            a.Jacket = true;
            a.JacketRow = 3; a.JacketColor = 9;
            a.LongSleeves = true;
            a.Bulk = 1.25f;
            a.Female = false;
            a.ShellStyle = 3;
            a.GauntletStyle = 1;
            a.StriderStyle = 1;
            a.VisorStyle = 2;
            a.ArmorColor = 10;
            a.Glow = 6;
            return a;
        }

        public static Appearance Malik()
        {
            Appearance a = new Appearance();
            a.Skin = 3;
            a.ShirtRow = 1; a.Shirt = 13;
            a.PantsRow = 1; a.Pants = 0;
            a.ShoesRow = 3; a.Shoes = 1;
            a.Hair = 0;
            a.HairStyle = HairStyle.Undercut;
            a.Jacket = true;
            a.JacketRow = 1; a.JacketColor = 11;
            a.LongSleeves = true;
            a.Beard = true;
            a.Height = 1.03f;
            a.Bulk = 1.1f;
            a.SuitStyle = 1;
            a.Glow = 2;
            a.Implant = 1 | 8;
            a.EyeColor = 1;
            a.ArmorColor = 0;
            return a;
        }

        public static Appearance Luma()
        {
            Appearance a = new Appearance();
            a.Skin = 3;
            a.ShirtRow = 4; a.Shirt = 1;
            a.PantsRow = 1; a.Pants = 2;
            a.ShoesRow = 3; a.Shoes = 2;
            a.Hair = 0;
            a.HairStyle = HairStyle.Bun;
            a.Female = true;
            a.Jacket = false;
            a.Height = 0.72f;
            a.Bulk = 0.85f;
            a.Glow = 5;
            a.EyeColor = 12;
            a.Implant = 2;
            return a;
        }

        // ================================================================== humanoid creatures

        /// <summary>The fields every creature factory shares: the kind, its size and a shape-variation seed.</summary>
        private static Appearance CreatureLook(HumanoidCreature kind, float scale, System.Random r)
        {
            Appearance a = new Appearance();
            a.Creature = kind;
            a.CreatureScale = scale;
            a.Height = 1f;
            a.Bulk = 1f;
            a.Implant = r.Next(1, 1 << 20);
            return a;
        }

        /// <summary>A Hollow: a tall, gaunt shadow stalker from the Inner World, about 2.2 m, with a smoky black body,
        /// thin violet seams, a mask-like face with two burning slit eyes and smoke tatters trailing from its limbs.</summary>
        public static Appearance Hollow(System.Random r)
        {
            Appearance a = CreatureLook(HumanoidCreature.Hollow, 1.12f, r);
            a.Height = 0.96f + (float)r.NextDouble() * 0.1f;
            a.Bulk = 0.78f + (float)r.NextDouble() * 0.12f;
            return a;
        }

        /// <summary>A Snow Hollow: a Hollow that froze in the Frostline, frost white with cracked pale-blue ice plates,
        /// icicles instead of smoke and blue light in its seams.</summary>
        public static Appearance SnowHollow(System.Random r)
        {
            Appearance a = CreatureLook(HumanoidCreature.SnowHollow, 1.14f, r);
            a.Height = 0.97f + (float)r.NextDouble() * 0.08f;
            a.Bulk = 0.84f + (float)r.NextDouble() * 0.1f;
            return a;
        }

        /// <summary>
        /// A Mirror Wraith copying someone (usually the player): the source's body shape and clothing silhouette in dark
        /// glass, with cyan and white rim lines, a cracked-mirror chest and glowing eyes. Same size as the source.
        /// </summary>
        public static Appearance MirrorWraith(Appearance source)
        {
            Appearance a = source;
            a.Creature = HumanoidCreature.MirrorWraith;
            a.CreatureScale = 1f;
            return a;
        }

        /// <summary>The Mirage Djinn, the Badlands apex: about 3.5 m tall as it hovers, a broad bare deep-blue torso with
        /// glowing amber sigils, a gold crown and bracers, and a body that dissolves below the waist into a swirling
        /// sand-and-smoke tail. The pose solver floats it automatically (RigLayout.Floating).</summary>
        public static Appearance MirageDjinn(System.Random r)
        {
            Appearance a = CreatureLook(HumanoidCreature.MirageDjinn, 1.6f, r);
            a.Height = 0.98f + (float)r.NextDouble() * 0.04f;
            a.Bulk = 1.22f + (float)r.NextDouble() * 0.12f;
            // The face is a person's face repainted: lit gold irises (no pupils), no beard (it gets a braid instead).
            a.EyeColor = 10;
            return a;
        }

        /// <summary>
        /// A golem: ShardGolem (about 2.4 m, dark stone and cyan crystal), SandGolem (about 2.4 m, wind-worn sandstone and
        /// amber light), GlacierTitan (the Frostline boss, about 4.5 m of blue ice slabs) or GolemForeman (a 3 m Shard
        /// Golem boss with a crystal crown and hazard-striped plates). Any other kind returns that creature's own look.
        /// </summary>
        public static Appearance Golem(HumanoidCreature kind, System.Random r)
        {
            switch (kind)
            {
                case HumanoidCreature.ShardGolem:
                case HumanoidCreature.SandGolem:
                {
                    Appearance a = CreatureLook(kind, 1.45f, r);
                    a.Height = 0.95f + (float)r.NextDouble() * 0.1f;
                    a.Bulk = 1.22f + (float)r.NextDouble() * 0.16f;
                    return a;
                }
                case HumanoidCreature.GlacierTitan:
                {
                    Appearance a = CreatureLook(kind, 2.55f, r);
                    a.Bulk = 1.4f;
                    return a;
                }
                case HumanoidCreature.GolemForeman:
                {
                    Appearance a = CreatureLook(kind, 1.82f, r);
                    a.Bulk = 1.4f;
                    return a;
                }
                case HumanoidCreature.None: return Golem(HumanoidCreature.ShardGolem, r);
                default: return ForCreature(kind, r);
            }
        }

        /// <summary>The Wraith Queen boss: a crowned Mirror Wraith about 2.6 m tall whose gown is a skirt of long glass shards.</summary>
        public static Appearance WraithQueen(System.Random r)
        {
            Appearance a = new Appearance();
            a.Skin = r.Next(0, 6);
            a.Female = true;
            a.HairStyle = HairStyle.Long;
            a.Hair = r.Next(0, 6);
            a.ShirtRow = 5; a.Shirt = 13;
            a.PantsRow = 5; a.Pants = 13;
            a.ShoesRow = 3; a.Shoes = 0;
            a.SuitStyle = 2;
            a.Glow = 2;
            a.Implant = 2;
            a.EyeColor = 8;
            a.Height = 1f;
            a.Bulk = 0.86f;
            a.Creature = HumanoidCreature.WraithQueen;
            a.CreatureScale = 1.3f;
            return a;
        }

        /// <summary>A random look for any humanoid creature kind (a Mirror Wraith copies a random person). None gives a person.</summary>
        public static Appearance ForCreature(HumanoidCreature kind, System.Random r)
        {
            switch (kind)
            {
                case HumanoidCreature.Hollow: return Hollow(r);
                case HumanoidCreature.SnowHollow: return SnowHollow(r);
                case HumanoidCreature.MirrorWraith: return MirrorWraith(Random(r));
                case HumanoidCreature.MirageDjinn: return MirageDjinn(r);
                case HumanoidCreature.WraithQueen: return WraithQueen(r);
                case HumanoidCreature.ShardGolem:
                case HumanoidCreature.SandGolem:
                case HumanoidCreature.GlacierTitan:
                case HumanoidCreature.GolemForeman: return Golem(kind, r);
                default: return Random(r);
            }
        }
    }
}
