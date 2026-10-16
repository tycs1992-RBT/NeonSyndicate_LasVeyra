using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The six crystal fire behaviors of the 04D arsenal. Numbers are saved nowhere: a gun's behavior comes from its
    /// stable unique index (LootGun.Unique), so it survives Refresh, Clone, the Forge and save and load. Append only.
    /// </summary>
    public enum CrystalBehavior
    {
        None = 0,
        /// <summary>Discrete swept physical shards; held trigger charges a three-shard fan.</summary>
        Shardwake = 1,
        /// <summary>A true sustained beam with time-based damage, heat and procs; aiming concentrates it.</summary>
        PrismThread = 2,
        /// <summary>A bankable returning disc (two banks at most, one outbound and one return hit per actor).</summary>
        GyreCrescent = 3,
        /// <summary>The Foreman's signature: a slow orb whose contained gravity pulse ends in a mineral rupture.</summary>
        Sunderheart = 4,
        /// <summary>The Djinn's signature: a fan of guided shards with aim-designated locks.</summary>
        MirageChoir = 5,
        /// <summary>The Queen's signature: a charged precision pulse with one planted reflective focus and one redirect.</summary>
        Mirrorwake = 6
    }

    /// <summary>
    /// One crystal focus's authored tuning. Damage values are multiples of the reference damage B
    /// (CrystalFocusDb.ReferenceDamage). These are the 04D starting proposals, not live-tested balance.
    /// </summary>
    public sealed class CrystalProfile
    {
        public CrystalBehavior Behavior;
        public string Name;
        /// <summary>The rarity the unique is authored at; the table below is exact at this rarity.</summary>
        public Rarity Authored;
        /// <summary>Primary hit in B (per shot, per launch or per volley shard); for the beam, B per second.</summary>
        public float Primary;
        /// <summary>Secondary hit in B: fan shard, disc return, gravity splash, redirected pulse share.</summary>
        public float Secondary;
        /// <summary>Seconds between shots, launches or volleys (the beam's proc tick for the beam).</summary>
        public float Interval;
        /// <summary>Longest legal path in metres (the disc's whole travel budget).</summary>
        public float Range;
        /// <summary>Projectile speed in m/s (0 for the beam and the pulse).</summary>
        public float Speed;
        /// <summary>Heat per shot (per second for the beam), 1 overheats.</summary>
        public float Heat;
        /// <summary>Heat of the charged or concentrated variation.</summary>
        public float ChargedHeat;
        /// <summary>Seconds a forced vent takes.</summary>
        public float VentSeconds;
        /// <summary>Seconds to a full charge (0 when the family has no charge).</summary>
        public float ChargeSeconds;
        /// <summary>Area radius in metres (gravity pulse), or the guided shards' lock cone in degrees.</summary>
        public float Radius;
        /// <summary>Most unique victims one shot may damage.</summary>
        public int MaxVictims;
        /// <summary>Shards per volley or fan, or banks for the disc.</summary>
        public int Count;
        /// <summary>One line for the item card: how it fires.</summary>
        public string FireLine;
        /// <summary>One line for the item card: its deliberate limitation.</summary>
        public string Limitation;
        /// <summary>One actionable line explaining its heat.</summary>
        public string HeatLine;
        /// <summary>Boss id whose first defeat grants it (empty for the three introductory foci).</summary>
        public string SignatureBoss = "";
    }

    /// <summary>
    /// Engine-free crystal arsenal data: the six profiles, the reference damage B, the append-only unique indices and
    /// the boss signature mapping. Every gameplay system asks this class, so the numbers live in one place.
    /// </summary>
    public static class CrystalFocusDb
    {
        // Appended after the 17 legacy uniques (indices 0 to 16 stay exactly as they were).
        public const int ShardwakeUnique = 17;
        public const int PrismThreadUnique = 18;
        public const int GyreCrescentUnique = 19;
        public const int SunderheartUnique = 20;
        public const int MirageChoirUnique = 21;
        public const int MirrorwakeUnique = 22;
        public const int FirstUnique = ShardwakeUnique;
        public const int UniqueCount = 6;

        /// <summary>Normalized heat drains this fast once the trigger has been quiet for CoolDelay seconds.</summary>
        public const float PassiveCooling = 0.25f;
        public const float CoolDelay = 0.35f;
        /// <summary>The beam's proc and damage tick (damage, heat and on-hit effects are integrated on this grid).</summary>
        public const float BeamTick = 0.1f;
        /// <summary>Most beam time integrated in one frame; a hitch beyond this is discarded, never paid out as a burst.</summary>
        public const float BeamMaxStep = 0.1f;
        /// <summary>Seconds of maintained aimed contact for the beam's full concentration ramp.</summary>
        public const float BeamRampSeconds = 1.5f;
        public const float BeamRampMax = 1.25f;
        /// <summary>Active authored projectile bodies (normal and Low graphics); refused shots fail visibly, never silently.</summary>
        public const int MaxProjectiles = 64;
        public const int MaxProjectilesLow = 32;
        /// <summary>Seconds a planted Mirrorwake focus lasts.</summary>
        public const float FocusLifetime = 8f;
        /// <summary>Metres from a planted focus to a redirect target.</summary>
        public const float RedirectRange = 30f;
        /// <summary>Seconds of aim per Mirage Choir lock, and the most locks.</summary>
        public const float LockSeconds = 0.35f;
        public const int MaxLocks = 3;
        /// <summary>Guided shard turn rate in degrees per second.</summary>
        public const float ChoirTurnRate = 180f;
        /// <summary>Seconds between Resonance vents of one focus (stops a burst, vent, burst loop).</summary>
        public const float ResonanceVentCooldown = 4f;

        private static readonly CrystalProfile[] _profiles = Build();

        public static void ResetStatics() { }

        private static CrystalProfile[] Build()
        {
            CrystalProfile[] p = new CrystalProfile[7];
            p[0] = null;
            p[1] = new CrystalProfile
            {
                Behavior = CrystalBehavior.Shardwake, Name = "Shardwake", Authored = Rarity.Rare,
                Primary = 1.0f, Secondary = 0.55f, Interval = 0.25f, Range = 55f, Speed = 95f, Heat = 0.09f, ChargedHeat = 0.2f,
                VentSeconds = 1.6f, ChargeSeconds = 0.8f, Radius = 0f, MaxVictims = 1, Count = 3,
                FireLine = "Fires traveling crystal shards (lead moving targets). Keep holding after a shot to charge a three-shard fan.",
                Limitation = "Small impact; every shard must reach its target.",
                HeatLine = "Each shard adds a little heat. Stop firing to cool, or press reload to vent."
            };
            p[2] = new CrystalProfile
            {
                Behavior = CrystalBehavior.PrismThread, Name = "Prism Thread", Authored = Rarity.Rare,
                Primary = 3.6f, Secondary = 0f, Interval = BeamTick, Range = 42f, Speed = 0f, Heat = 0.28f, ChargedHeat = 0.28f,
                VentSeconds = 1.9f, ChargeSeconds = 0f, Radius = 0f, MaxVictims = 1, Count = 0,
                FireLine = "Hold fire for a continuous beam that stops at the first wall. Aim to narrow it; maintained aimed contact ramps damage.",
                Limitation = "Needs unbroken sight; the ramp resets when contact breaks.",
                HeatLine = "Heat climbs while the beam is on. Release before it overheats, or press reload to vent."
            };
            p[3] = new CrystalProfile
            {
                Behavior = CrystalBehavior.GyreCrescent, Name = "Gyre Crescent", Authored = Rarity.Epic,
                Primary = 1.3f, Secondary = 0.65f, Interval = 1f / 0.7f, Range = 32f, Speed = 30f, Heat = 0.26f, ChargedHeat = 0.26f,
                VentSeconds = 2.0f, ChargeSeconds = 0f, Radius = 0f, MaxVictims = 3, Count = 2,
                FireLine = "Throws a crystal disc that banks off walls up to twice, then returns. Press fire again to recall it early.",
                Limitation = "At most three foes per disc; a blocked return quenches the disc.",
                HeatLine = "Each throw adds heat. The cradle cools while the disc is out."
            };
            p[4] = new CrystalProfile
            {
                Behavior = CrystalBehavior.Sunderheart, Name = "Sunderheart", Authored = Rarity.Legendary,
                Primary = 3.2f, Secondary = 1.4f, Interval = 1.0f, Range = 38f, Speed = 24f, Heat = 0.35f, ChargedHeat = 0.5f,
                VentSeconds = 2.2f, ChargeSeconds = 0.9f, Radius = 3.5f, MaxVictims = 6, Count = 0,
                FireLine = "Launches a slow gravity orb: a short contained pull, then a mineral rupture. Keep holding after a launch to concentrate the next burst.",
                Limitation = "Slow orb, small area; walls and floors stop the pulse.",
                HeatLine = "Heavy heat per orb, more when concentrated. Press reload to vent.",
                SignatureBoss = "golem_foreman"
            };
            p[5] = new CrystalProfile
            {
                Behavior = CrystalBehavior.MirageChoir, Name = "Mirage Choir", Authored = Rarity.Legendary,
                Primary = 0.45f, Secondary = 0f, Interval = 0.9f, Range = 48f, Speed = 42f, Heat = 0.24f, ChargedHeat = 0.24f,
                VentSeconds = 1.8f, ChargeSeconds = 0f, Radius = 18f, MaxVictims = 3, Count = 5,
                FireLine = "Fires a fan of five guided shards. Hold aim over visible foes to lock up to three first.",
                Limitation = "Locks need sight; a lost lock flies straight and can hit cover.",
                HeatLine = "Each volley adds heat. Press reload to vent.",
                SignatureBoss = "mirage_djinn"
            };
            p[6] = new CrystalProfile
            {
                Behavior = CrystalBehavior.Mirrorwake, Name = "Mirrorwake", Authored = Rarity.Legendary,
                Primary = 3.0f, Secondary = 1.0f, Interval = 1.1f, Range = 60f, Speed = 0f, Heat = 0.32f, ChargedHeat = 0.32f,
                VentSeconds = 2.0f, ChargeSeconds = 0.6f, Radius = 0f, MaxVictims = 1, Count = 1,
                FireLine = "Hold fire to charge a precision pulse. A pulse aimed at a bare surface plants a mirror focus; the next pulse aimed at the focus bends to a visible foe.",
                Limitation = "One focus, one redirect; both beam legs must be clear.",
                HeatLine = "Each pulse adds heat. Press reload to vent.",
                SignatureBoss = "wraith_queen"
            };
            return p;
        }

        public static CrystalProfile Get(CrystalBehavior b)
        {
            int i = (int)b;
            return i > 0 && i < _profiles.Length ? _profiles[i] : null;
        }

        /// <summary>The behavior a saved unique index carries (None for every legacy or generated gun).</summary>
        public static CrystalBehavior BehaviorOfUnique(int unique)
        {
            int k = unique - FirstUnique;
            return k >= 0 && k < UniqueCount ? (CrystalBehavior)(k + 1) : CrystalBehavior.None;
        }

        public static int UniqueOf(CrystalBehavior b)
        {
            int i = (int)b;
            return i >= 1 && i <= UniqueCount ? FirstUnique + i - 1 : -1;
        }

        /// <summary>The signature unique a boss's first defeat grants, or -1 (the Titan and every other creature: none).</summary>
        public static int SignatureFor(string bossId)
        {
            switch (bossId)
            {
                case "golem_foreman": return SunderheartUnique;
                case "mirage_djinn": return MirageChoirUnique;
                case "wraith_queen": return MirrorwakeUnique;
                default: return -1;
            }
        }

        /// <summary>
        /// The reference damage B: the unmodified body hit of a level-matched Uncommon crystal caster with neutral maker
        /// and part modifiers (LootGenerator base 18 x Uncommon power 1.0 x level power). 18.0 at level 1.
        /// </summary>
        public static float ReferenceDamage(int level)
        {
            return 18f * 1f * (1f + 0.008f * (Mathf.Clamp(level, 1, 50) - 1));
        }

        private static readonly float[] RarityPower = { 0.85f, 1f, 1.1f, 1.2f, 1.32f, 1.45f };

        /// <summary>Rarity scale relative to the authored rarity: exact at the authored rarity, higher after Forge ascension.</summary>
        public static float RarityScale(CrystalProfile p, Rarity r)
        {
            if (p == null) return 1f;
            int a = Mathf.Clamp((int)p.Authored, 0, 5);
            int c = Mathf.Clamp((int)r, 0, 5);
            return RarityPower[c] / RarityPower[a];
        }

        /// <summary>The primary hit (B per second for the beam) of a focus at a rarity and item level.</summary>
        public static float PrimaryDamage(CrystalProfile p, Rarity r, int level)
        {
            return p == null ? 0f : p.Primary * ReferenceDamage(level) * RarityScale(p, r);
        }

        public static float SecondaryDamage(CrystalProfile p, Rarity r, int level)
        {
            return p == null ? 0f : p.Secondary * ReferenceDamage(level) * RarityScale(p, r);
        }

        /// <summary>
        /// Damage per second over a full heat cycle (shots until overheat, then the vent). For the beam this is honest
        /// sustained damage, never a per-bullet figure. Ramp, misses and movement are excluded and named in the card.
        /// </summary>
        public static float SustainedDps(CrystalProfile p, Rarity r, int level)
        {
            if (p == null) return 0f;
            float primary = PrimaryDamage(p, r, level);
            if (p.Behavior == CrystalBehavior.PrismThread)
            {
                float onSeconds = 1f / Mathf.Max(0.01f, p.Heat);
                return primary * onSeconds / (onSeconds + p.VentSeconds);
            }
            float perShot = primary * Mathf.Max(1, p.Behavior == CrystalBehavior.MirageChoir ? p.Count : 1);
            if (p.Behavior == CrystalBehavior.GyreCrescent) perShot += SecondaryDamage(p, r, level);
            if (p.Behavior == CrystalBehavior.Sunderheart) perShot += SecondaryDamage(p, r, level);
            float shots = Mathf.Ceil(1f / Mathf.Max(0.01f, p.Heat));
            return shots * perShot / (shots * p.Interval + p.VentSeconds);
        }

        /// <summary>
        /// Traits a crystal focus may carry: none. Its behavior is its signature; the legacy traits (ricochet, split shot,
        /// charge shot and the rest) would double-dispatch the trigger, so Refresh strips them and the Forge refuses them.
        /// </summary>
        public static bool TraitAllowed(CrystalBehavior b, LootTrait t)
        {
            return b == CrystalBehavior.None;
        }
    }
}
