using System;

namespace NeonSyndicate
{
    /// <summary>The six underground combat roles of 04E (one per hostile species).</summary>
    public enum UnderRole04E
    {
        None = 0,
        /// <summary>Ember Scavenger: a small pack harasser that darts in when you look away, then runs off.</summary>
        Harasser = 1,
        /// <summary>Ash Burrower: armoured, tunnels under you and bursts up under a filling ring.</summary>
        Armoured = 2,
        /// <summary>Glasswing Ambusher: hangs from the ceiling, drops on whoever passes below, climbs back.</summary>
        Ambusher = 3,
        /// <summary>Furnace Caster: hangs back and lobs fire over cover onto a ring at your feet.</summary>
        Caster = 4,
        /// <summary>Extraction Sentinel: scans with a visible cone, locks on and fires a clamp; stamps up close.</summary>
        Sentinel = 5,
        /// <summary>Repair Construct: mends its allies with a beam, flees when you come for it.</summary>
        Support = 6
    }

    /// <summary>
    /// One role's design numbers: what it is, its signature move (tell, active and punish windows, cooldown and range),
    /// where it likes to stand, when it retreats, and how the player counters it. Plain data shared by the role drivers,
    /// the bestiary text, the docs and the EditMode tests.
    /// </summary>
    public sealed class UnderRoleDef04E
    {
        public string Species = "";
        public UnderRole04E Role;
        /// <summary>Short role name for the debug page and the docs ("Pack harasser").</summary>
        public string Title = "";
        /// <summary>The signature move's name.</summary>
        public string Signature = "";
        /// <summary>What the player sees and hears before the signature lands.</summary>
        public string Tell = "";
        /// <summary>Seconds of warning before the signature can hurt (the readable wind-up).</summary>
        public float TellSeconds;
        /// <summary>Seconds the signature's hit window lasts.</summary>
        public float ActiveSeconds;
        /// <summary>Seconds the role stands open afterwards (the punish window).</summary>
        public float PunishSeconds;
        public float CooldownMin, CooldownMax;
        /// <summary>Distance band to the target where the signature is used (metres).</summary>
        public float RangeMin, RangeMax;
        /// <summary>Distance band it tries to keep from the target between attacks (metres).</summary>
        public float PreferredMin, PreferredMax;
        /// <summary>Health fraction under which it retreats (1 means it always keeps away from a close player; 0 never).</summary>
        public float RetreatHealth;
        public float RetreatSeconds;
        /// <summary>How it retreats, in one line.</summary>
        public string Retreat = "";
        /// <summary>How the player beats it (at least two ways).</summary>
        public string[] Counters = new string[0];
    }

    /// <summary>
    /// The 04E role table (engine-free). Every hostile id in EnemyIds04E.Hostile has exactly one distinct role; the
    /// runtime drivers (CreatureRoleDriver04E and SentinelDriver04E) read these numbers so the docs, the tests and the
    /// game cannot drift apart.
    /// </summary>
    public static class UnderheartRoles04E
    {
        /// <summary>Signatures never wind up faster than this (seconds): every one is readable at player height.</summary>
        public const float MinTell = 0.6f;
        /// <summary>Every signature leaves at least this long open afterwards (seconds).</summary>
        public const float MinPunish = 0.6f;

        private static readonly UnderRoleDef04E[] _defs =
        {
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.EmberScavenger, Role = UnderRole04E.Harasser, Title = "Pack harasser", Signature = "Hit and run",
                Tell = "It crouches and chitters, its ember spines flare, then it darts in low; it holds back while you face it.",
                TellSeconds = 0.7f, ActiveSeconds = 0.35f, PunishSeconds = 0.9f, CooldownMin = 5f, CooldownMax = 7f,
                RangeMin = 2.5f, RangeMax = 7f, PreferredMin = 3.5f, PreferredMax = 6.5f, RetreatHealth = 0.3f, RetreatSeconds = 4f,
                Retreat = "After every strike it runs off a few metres to circle again; badly hurt, it breaks and runs, then comes back.",
                Counters = new[] { "Face the pack: they hesitate while you look at them.", "Shoot or hit a dart in its wind-up to break it.", "Keep a wall at your back; frost slows them." }
            },
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.AshBurrower, Role = UnderRole04E.Armoured, Title = "Armoured digger", Signature = "Burrow ambush",
                Tell = "It rattles its plates and digs in (an ash plume), a dust trail runs toward you, then an ash ring fills under your feet.",
                TellSeconds = 1.1f, ActiveSeconds = 0.3f, PunishSeconds = 1.6f, CooldownMin = 12f, CooldownMax = 16f,
                RangeMin = 5f, RangeMax = 18f, PreferredMin = 0f, PreferredMax = 3f, RetreatHealth = 0.25f, RetreatSeconds = 3f,
                Retreat = "Badly hurt, it dives into the floor once and surfaces far from you to recover.",
                Counters = new[] { "Step out of the ash ring before it fills.", "Punish the rear after it bursts up: its belly is open.", "Flank its frontal plates or break them; lightning hits hardest." }
            },
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.GlasswingAmbusher, Role = UnderRole04E.Ambusher, Title = "Ceiling ambusher", Signature = "Ceiling drop",
                Tell = "Glass dust trickles from the ceiling, a white ring marks where it will land and it screeches.",
                TellSeconds = 0.9f, ActiveSeconds = 0.4f, PunishSeconds = 0.8f, CooldownMin = 14f, CooldownMax = 18f,
                RangeMin = 0f, RangeMax = 6f, PreferredMin = 1f, PreferredMax = 3f, RetreatHealth = 0.3f, RetreatSeconds = 3f,
                Retreat = "Hurt, it climbs slowly back to the ceiling (open to fire); with cracked wings it can only run.",
                Counters = new[] { "Look up: the Third Eye lights it on the ceiling.", "Shoot it down before it drops: it lands stunned.", "Step out of the white ring; punish the landing." }
            },
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.FurnaceCaster, Role = UnderRole04E.Caster, Title = "Ranged caster", Signature = "Slag rain",
                Tell = "It plants its feet, the crucible on its back brightens and a red ring fills at your feet before slag falls into it.",
                TellSeconds = 1.4f, ActiveSeconds = 0.6f, PunishSeconds = 1.2f, CooldownMin = 9f, CooldownMax = 12f,
                RangeMin = 8f, RangeMax = 30f, PreferredMin = 10f, PreferredMax = 16f, RetreatHealth = 0.35f, RetreatSeconds = 3f,
                Retreat = "It backs away whenever you close in; badly hurt, it runs for a few seconds.",
                Counters = new[] { "Leave the red ring before it fills.", "Close the distance: frost or any melee hit during its charge puts the fire out.", "Break the crucible to stop its bolts." }
            },
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.ExtractionSentinel, Role = UnderRole04E.Sentinel, Title = "Extraction sentinel", Signature = "Scan and clamp",
                Tell = "It stops and sweeps an amber scan cone across the room; the cone turns red as it locks, then the clamp fires along it.",
                TellSeconds = 1.65f, ActiveSeconds = 0.3f, PunishSeconds = 1f, CooldownMin = 8f, CooldownMax = 11f,
                RangeMin = 5f, RangeMax = 20f, PreferredMin = 1.5f, PreferredMax = 3f, RetreatHealth = 0.3f, RetreatSeconds = 4f,
                Retreat = "Badly damaged, it backs to its post and braces its front plate (hits from the front are halved).",
                Counters = new[] { "Break line of sight or step out of the cone while it sweeps.", "Raise your ward toward it: the clamp turns aside.", "Shoot the scanner lens to blind it; flank the braced plate." }
            },
            new UnderRoleDef04E
            {
                Species = EnemyIds04E.RepairConstruct, Role = UnderRole04E.Support, Title = "Repair support", Signature = "Weld beam",
                Tell = "Its weld arm unfolds and a cyan beam links it to a wounded ally, whose wounds glow as they close.",
                TellSeconds = 0.6f, ActiveSeconds = 4f, PunishSeconds = 1.5f, CooldownMin = 2f, CooldownMax = 3f,
                RangeMin = 0f, RangeMax = 14f, PreferredMin = 6f, PreferredMax = 10f, RetreatHealth = 1f, RetreatSeconds = 2.5f,
                Retreat = "It keeps behind its allies and scuttles away whenever you come within a few metres.",
                Counters = new[] { "Kill it first: it cannot mend itself.", "Any hit breaks the beam for a moment.", "Lightning drops it fast." }
            }
        };

        /// <summary>Every role definition, in EnemyIds04E.Hostile order.</summary>
        public static UnderRoleDef04E[] All { get { return _defs; } }

        /// <summary>The role of a species id, or null when it has none (residents, surface species).</summary>
        public static UnderRoleDef04E For(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;
            for (int i = 0; i < _defs.Length; i++) if (_defs[i].Species == speciesId) return _defs[i];
            return null;
        }

        public static UnderRole04E RoleOf(string speciesId)
        {
            UnderRoleDef04E d = For(speciesId);
            return d != null ? d.Role : UnderRole04E.None;
        }

        /// <summary>True for the three harmless or territorial residents.</summary>
        public static bool IsResident(string speciesId)
        {
            return !string.IsNullOrEmpty(speciesId) && Array.IndexOf(EnemyIds04E.Residents, speciesId) >= 0;
        }

        /// <summary>True for any of the nine 04E Underheart species.</summary>
        public static bool IsUnderheart(string speciesId)
        {
            return For(speciesId) != null || IsResident(speciesId);
        }

        /// <summary>A cooldown inside the role's band for a roll in [0, 1).</summary>
        public static float Cooldown(UnderRoleDef04E d, float roll)
        {
            if (d == null) return 10f;
            float r = roll < 0f ? 0f : (roll > 1f ? 1f : roll);
            return d.CooldownMin + (d.CooldownMax - d.CooldownMin) * r;
        }

        /// <summary>True when a target at this distance is inside the signature's band.</summary>
        public static bool InSignatureRange(UnderRoleDef04E d, float distance)
        {
            return d != null && distance >= d.RangeMin && distance <= d.RangeMax;
        }

        /// <summary>
        /// The Harasser's courage rule: a scavenger in front of the player's view (within watchAngle degrees of where they
        /// look, closer than watchRange) holds its dart back unless the player has looked away for a moment.
        /// </summary>
        public static bool HarasserHesitates(float angleFromView, float distance, float watchAngle, float watchRange)
        {
            return angleFromView <= watchAngle && distance <= watchRange;
        }

        /// <summary>The Caster's spacing: -1 back away, +1 close in, 0 hold (distance to the target in metres).</summary>
        public static int CasterSpacing(UnderRoleDef04E d, float distance)
        {
            if (d == null) return 0;
            if (distance < d.PreferredMin) return -1;
            if (distance > d.PreferredMax) return 1;
            return 0;
        }

        /// <summary>The Support's flight rule: it backs off when the player is closer than this many metres.</summary>
        public const float SupportPanicRange = 6f;

        /// <summary>Mending: health per second restored to an ally (a fraction of the ally's max health), never to itself.</summary>
        public const float MendPerSecond = 0.06f;
        /// <summary>Allies under this health fraction are worth mending.</summary>
        public const float MendBelow = 0.85f;

        /// <summary>True when a support may mend this ally (never itself, never a dead or full-health body).</summary>
        public static bool MayMend(bool isSelf, bool allyDead, float allyHealthFraction, float distance, float range)
        {
            if (isSelf || allyDead) return false;
            if (allyHealthFraction >= MendBelow) return false;
            return distance <= range;
        }

        /// <summary>
        /// The Ambusher's drop trigger: the player passes under it (within triggerRadius flat metres and below it) or it
        /// was hurt, or it has waited the patience time since the fight began (it drops near the player anyway).
        /// </summary>
        public static bool AmbusherDrops(float flatDistance, float playerBelowBy, bool hurt, float engagedSeconds, float triggerRadius, float patience)
        {
            if (hurt) return true;
            if (playerBelowBy > 0.5f && flatDistance <= triggerRadius) return true;
            return engagedSeconds >= patience;
        }

        /// <summary>The Sentinel's lock test at the end of a sweep: inside the cone's half angle, within range, with a clear line.</summary>
        public static bool SentinelLocks(float angleOffAxis, float distance, float halfAngle, float range, bool lineClear)
        {
            return lineClear && angleOffAxis <= halfAngle && distance <= range;
        }

        /// <summary>The clamp travels along the lock line; it hits when the target is still within width of it on arrival.</summary>
        public static bool ClampHits(float lateralOffset, float width, bool lineClear)
        {
            return lineClear && lateralOffset <= width;
        }
    }
}
