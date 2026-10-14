using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The armour pieces a human enemy can wear.</summary>
    public enum ArmorPiece
    {
        /// <summary>No armour where the hit landed (legs, a broken plate's spot, an unarmoured head).</summary>
        None = -1,
        Chest = 0,
        Helmet = 1,
        ShoulderL = 2,
        ShoulderR = 3
    }

    /// <summary>What a character wears: a total armour pool split over the pieces that are present.</summary>
    public struct ArmorLoadout
    {
        public float Pool;
        public bool Chest, Helmet, Shoulders;

        public bool Any { get { return Pool > 0.5f && (Chest || Helmet || Shoulders); } }

        public bool Has(ArmorPiece p)
        {
            switch (p)
            {
                case ArmorPiece.Chest: return Chest;
                case ArmorPiece.Helmet: return Helmet;
                case ArmorPiece.ShoulderL:
                case ArmorPiece.ShoulderR: return Shoulders;
                default: return false;
            }
        }
    }

    /// <summary>How one hit splits between an armour plate and health.</summary>
    public struct ArmorSplit
    {
        public float Armor;
        public float Health;
    }

    /// <summary>
    /// Enemy armour plates, all the numbers in one engine-free place (tested offline). Human enemies wear a chest plate,
    /// a helmet and shoulder guards with one shared armour pool split between the pieces present. Bullets strip the
    /// plate they hit first (the rest of a hit that breaks it goes on to health), legs are never armoured, a headshot on
    /// a helmet only does a small bonus until the helmet is off, explosions spread over every plate, melee is half
    /// stopped, fire and crystal powers pass through. Lightning rounds (Volt) eat armour faster.
    /// </summary>
    public static class ArmorRules
    {
        public const int PieceCount = 4;
        /// <summary>Headshot multiplier while the helmet still holds (the full one applies once it is broken).</summary>
        public const float HelmetedHeadshot = 1.25f;
        /// <summary>Share of an explosion the plates take (spread over all of them); the rest reaches health.</summary>
        public const float ExplosionAbsorb = 0.75f;
        /// <summary>Share of a melee blow the struck plate takes.</summary>
        public const float MeleeAbsorb = 0.5f;
        /// <summary>Lightning rounds deal this much more damage to armour.</summary>
        public const float VoltVsArmor = 1.5f;
        /// <summary>Share of the pool each piece holds (normalised over the pieces present).</summary>
        public const float ChestShare = 0.5f, HelmetShare = 0.25f, ShoulderShare = 0.125f;
        /// <summary>Body heights (in rig scale units above the feet) of the hips and of the shoulder line.</summary>
        public const float HipLine = 0.95f, ShoulderLine = 1.3f;
        /// <summary>Hits further than this to the side of the spine (rig scale units) above the shoulder line hit a shoulder guard.</summary>
        public const float ShoulderSide = 0.17f;

        /// <summary>
        /// The armour an NPC wears by role and tier. roll (0 to 1, seeded per NPC) decides which gang members wear a
        /// vest. Elites add shoulder guards and double the pool, champions wear everything at three times it (the
        /// Armored affix half again). Difficulty: Story 0.7, Balanced 1, Veteran 1.25.
        /// </summary>
        public static ArmorLoadout LoadoutFor(NpcRole role, bool elite, bool champion, bool armoredAffix, int difficulty, float roll)
        {
            ArmorLoadout l = new ArmorLoadout();
            switch (role)
            {
                case NpcRole.Police: l.Pool = 60f; l.Chest = true; l.Helmet = true; break;
                case NpcRole.Swat: l.Pool = 160f; l.Chest = true; l.Helmet = true; l.Shoulders = true; break;
                case NpcRole.Enemy: l.Pool = 70f; l.Chest = true; l.Helmet = true; break;
                case NpcRole.Gang:
                    if (roll < 0.35f || elite || champion) { l.Pool = 45f; l.Chest = true; l.Helmet = roll < 0.12f; }
                    break;
                default: return l;
            }
            if (elite || champion)
            {
                if (l.Pool <= 0f) l.Pool = 45f;
                l.Chest = true;
                l.Helmet = true;
                l.Shoulders = true;
                l.Pool *= champion ? 3f : 2f;
                if (champion && armoredAffix) l.Pool *= 1.5f;
            }
            l.Pool *= DifficultyScale(difficulty);
            return l;
        }

        public static float DifficultyScale(int difficulty)
        {
            return difficulty <= 0 ? 0.7f : (difficulty == 1 ? 1f : 1.25f);
        }

        /// <summary>The armour points a piece holds out of the loadout's pool (0 when the piece is not worn).</summary>
        public static float PieceMax(ArmorLoadout l, ArmorPiece p)
        {
            if (!l.Has(p)) return 0f;
            float total = (l.Chest ? ChestShare : 0f) + (l.Helmet ? HelmetShare : 0f) + (l.Shoulders ? ShoulderShare * 2f : 0f);
            if (total <= 0f) return 0f;
            float share = p == ArmorPiece.Chest ? ChestShare : (p == ArmorPiece.Helmet ? HelmetShare : ShoulderShare);
            return l.Pool * share / total;
        }

        /// <summary>
        /// Which piece a bullet struck. height is the hit's height above the feet and side its distance to the right of
        /// the spine (negative to the left), both divided by the rig scale. Headshots go to the helmet; below the hips
        /// is never armoured.
        /// </summary>
        public static ArmorPiece Locate(bool headshot, float height, float side)
        {
            if (headshot) return ArmorPiece.Helmet;
            if (height < HipLine) return ArmorPiece.None;
            if (height > ShoulderLine && Mathf.Abs(side) > ShoulderSide) return side < 0f ? ArmorPiece.ShoulderL : ArmorPiece.ShoulderR;
            return ArmorPiece.Chest;
        }

        /// <summary>
        /// A direct hit of amount on a plate holding plateHp: the plate takes it first (times armorMul, Volt rounds eat
        /// armour faster) and only what is left after breaking it reaches health. absorb is the share the plate stops
        /// at all (1 for bullets, MeleeAbsorb for blows); the rest goes straight through.
        /// </summary>
        public static ArmorSplit Direct(float amount, float plateHp, float armorMul, float absorb)
        {
            ArmorSplit s = new ArmorSplit();
            amount = Mathf.Max(0f, amount);
            if (plateHp <= 0f)
            {
                s.Health = amount;
                return s;
            }
            float mul = Mathf.Max(0.01f, armorMul);
            float stopped = amount * Mathf.Clamp01(absorb);
            s.Health = amount - stopped;
            float want = stopped * mul;
            if (want <= plateHp) s.Armor = want;
            else
            {
                s.Armor = plateHp;
                s.Health += (want - plateHp) / mul;
            }
            return s;
        }

        /// <summary>Multiplier on a bullet's damage against armour by its element.</summary>
        public static float ElementVsArmor(Element e)
        {
            return e == Element.Volt ? VoltVsArmor : 1f;
        }

        /// <summary>True when this kind of damage is stopped by plates at all.</summary>
        public static bool Stops(DamageType t)
        {
            return t == DamageType.Bullet || t == DamageType.Explosion || t == DamageType.Melee;
        }

        /// <summary>
        /// An explosion over every intact plate: the plates take ExplosionAbsorb of it between them, in proportion to
        /// what each holds (never more than it holds); the rest, and whatever they could not hold, reaches health.
        /// hp is read, armorOut receives the damage per plate. Returns the health damage.
        /// </summary>
        public static float Spread(float amount, float[] hp, float[] armorOut)
        {
            amount = Mathf.Max(0f, amount);
            float total = 0f;
            for (int i = 0; i < hp.Length; i++)
            {
                armorOut[i] = 0f;
                total += Mathf.Max(0f, hp[i]);
            }
            if (total <= 0f) return amount;
            float stopped = amount * ExplosionAbsorb;
            float health = amount - stopped;
            float taken = 0f;
            for (int i = 0; i < hp.Length; i++)
            {
                float h = Mathf.Max(0f, hp[i]);
                if (h <= 0f) continue;
                float a = Mathf.Min(h, stopped * h / total);
                armorOut[i] = a;
                taken += a;
            }
            return health + Mathf.Max(0f, stopped - taken);
        }

        /// <summary>Headshot multiplier given the character's own one and whether a helmet still holds.</summary>
        public static float HeadshotMultiplier(float full, bool helmetIntact)
        {
            return helmetIntact ? Mathf.Min(full, HelmetedHeadshot) : full;
        }
    }
}
