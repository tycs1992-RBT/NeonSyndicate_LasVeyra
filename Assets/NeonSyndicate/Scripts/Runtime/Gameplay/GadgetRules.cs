using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The six gadgets (tech skills). Each comes in two variants, so a gadget id is kind * 2 + variant.</summary>
    public enum GadgetKind
    {
        Turret = 0,
        Mine = 1,
        Drone = 2,
        Barrier = 3,
        Scanner = 4,
        Vapor = 5
    }

    /// <summary>What a gadget mod improves.</summary>
    public enum GadgetModStat
    {
        Cooldown = 0,
        Power = 1,
        Duration = 2,
        Radius = 3
    }

    /// <summary>The fixed numbers of one gadget variant (before level, mods and gear sets).</summary>
    public sealed class GadgetSpec
    {
        public int Id;
        public GadgetKind Kind;
        public int Variant;
        public string Name;
        public string VariantName;
        public string Blurb;
        /// <summary>Seconds between uses.</summary>
        public float Cooldown;
        /// <summary>Seconds the deployed gadget lasts (or how long a scanner mark stays).</summary>
        public float Duration;
        /// <summary>Damage per hit, healing per second, or damage per blast, depending on the gadget.</summary>
        public float Power;
        /// <summary>Seconds between hits or heal ticks (0 when it does not repeat).</summary>
        public float Interval;
        /// <summary>Blast, cloud or pulse radius in metres.</summary>
        public float Radius;
        /// <summary>How far the gadget reaches to hit or seek targets.</summary>
        public float Reach;
        /// <summary>How far from the player it can be thrown or placed (0: it works on the player).</summary>
        public float DeployRange;
        /// <summary>Hit points of a shield (0 for everything else).</summary>
        public float Shield;
        public Color Color;

        public bool SelfCast { get { return DeployRange <= 0f; } }
        public string FullName { get { return Name + " (" + VariantName + ")"; } }
    }

    /// <summary>One gadget mod: a dropped chip that improves whichever gadget sits in the slot it is socketed in.</summary>
    [Serializable]
    public sealed class GadgetModSave
    {
        public int Uid;
        public int Stat;
        public int Rarity;
        public float Value;
    }

    /// <summary>The gadget system's save block (append-only, like every block).</summary>
    [Serializable]
    public sealed class GadgetSave
    {
        /// <summary>Gadget id in each slot (kind * 2 + variant), -1 for an empty slot.</summary>
        public List<int> Slots = new List<int>();
        /// <summary>Uid of the mod socketed in each slot, 0 for none.</summary>
        public List<int> SlotMods = new List<int>();
        public List<GadgetModSave> Mods = new List<GadgetModSave>();
        public int NextModUid = 1;
        /// <summary>Set by the F2 page: every gadget unlocked regardless of level.</summary>
        public bool AllUnlocked;
        /// <summary>How many first-time gadget hints have been shown (they stop at GadgetRules.HintUses).</summary>
        public int HintsShown;
        /// <summary>Gadget ids whose unlock banner has already been shown.</summary>
        public List<int> Announced = new List<int>();
    }

    /// <summary>
    /// Tick-down timer for one gadget slot. Pure, so the cooldown maths is tested offline.
    /// </summary>
    public sealed class GadgetCooldown
    {
        public float Remaining { get; private set; }
        public float Total { get; private set; }

        public bool Ready { get { return Remaining <= 0f; } }

        /// <summary>0 when ready, 1 right after use.</summary>
        public float Fraction { get { return Total > 0f ? Mathf.Clamp01(Remaining / Total) : 0f; } }

        public void Start(float seconds)
        {
            Total = Mathf.Max(0f, seconds);
            Remaining = Total;
        }

        /// <summary>Advances time. Returns true on the tick the cooldown finishes.</summary>
        public bool Tick(float dt)
        {
            if (Remaining <= 0f) return false;
            Remaining -= Mathf.Max(0f, dt);
            if (Remaining > 0f) return false;
            Remaining = 0f;
            return true;
        }

        /// <summary>Takes seconds off (gear set effects). Returns true if that finished it.</summary>
        public bool Cut(float seconds)
        {
            if (Remaining <= 0f || seconds <= 0f) return false;
            Remaining = Mathf.Max(0f, Remaining - seconds);
            return Remaining <= 0f;
        }

        public void Clear()
        {
            Remaining = 0f;
            Total = 0f;
        }
    }

    /// <summary>
    /// Every gadget number in one place: the twelve variants, unlock levels, how level, mods and gear sets change
    /// cooldowns and power, mod rolls and drop chances. Engine-free, tested by GadgetTests.
    /// </summary>
    public static class GadgetRules
    {
        public const int KindCount = 6;
        public const int VariantCount = 2;
        public const int GadgetCount = KindCount * VariantCount;
        public const int SlotCount = 2;
        public const int ModCap = 30;
        /// <summary>First-time hints shown before they stop.</summary>
        public const int HintUses = 3;
        /// <summary>Mods and gear sets together can never cut a cooldown by more than this.</summary>
        public const float MaxCooldownCut = 0.5f;
        /// <summary>The Lance scanner's exposed targets take this much more damage.</summary>
        public const float ExposeBonus = 0.15f;
        /// <summary>The Suture drone's armor repair per second.</summary>
        public const float SutureArmorPerSecond = 2f;
        /// <summary>Half angle of the Kiln turret's flame cone and the Lance scanner's cone, in degrees.</summary>
        public const float KilnHalfAngle = 40f;
        public const float LanceHalfAngle = 22f;
        /// <summary>The Litter mine splits into this many small seekers.</summary>
        public const int LitterCount = 4;

        private static readonly GadgetSpec[] _specs = Build();

        public static GadgetSpec Get(int id)
        {
            return id >= 0 && id < _specs.Length ? _specs[id] : null;
        }

        public static GadgetSpec Get(GadgetKind kind, int variant)
        {
            return Get(IdOf(kind, variant));
        }

        public static int IdOf(GadgetKind kind, int variant)
        {
            return (int)kind * VariantCount + Mathf.Clamp(variant, 0, VariantCount - 1);
        }

        public static GadgetKind KindOf(int id) { return (GadgetKind)(Mathf.Clamp(id, 0, GadgetCount - 1) / VariantCount); }
        public static int VariantOf(int id) { return Mathf.Clamp(id, 0, GadgetCount - 1) % VariantCount; }

        private static GadgetSpec[] Build()
        {
            Color amber = new Color(1f, 0.72f, 0.25f);
            Color flame = new Color(1f, 0.42f, 0.12f);
            Color teal = new Color(0.25f, 1f, 0.8f);
            Color green = new Color(0.4f, 1f, 0.45f);
            Color blue = new Color(0.35f, 0.65f, 1f);
            Color violet = new Color(0.78f, 0.45f, 1f);
            GadgetSpec[] s =
            {
                new GadgetSpec { Kind = GadgetKind.Turret, Variant = 0, Name = "Spindle Turret", VariantName = "Needle", Color = amber,
                    Blurb = "A tripod turret that fires steady rifle bursts at the nearest enemy it can see.",
                    Cooldown = 40f, Duration = 20f, Power = 7f, Interval = 0.2f, Reach = 32f, DeployRange = 18f },
                new GadgetSpec { Kind = GadgetKind.Turret, Variant = 1, Name = "Spindle Turret", VariantName = "Kiln", Color = flame,
                    Blurb = "A squat turret that sweeps a short cone of flame and sets enemies burning.",
                    Cooldown = 38f, Duration = 14f, Power = 5f, Interval = 0.25f, Reach = 9f, DeployRange = 14f },
                new GadgetSpec { Kind = GadgetKind.Mine, Variant = 0, Name = "Skitter Mine", VariantName = "Hound", Color = flame,
                    Blurb = "A legged mine that scuttles to the nearest enemy and bursts.",
                    Cooldown = 22f, Duration = 12f, Power = 120f, Radius = 4f, Reach = 20f, DeployRange = 20f },
                new GadgetSpec { Kind = GadgetKind.Mine, Variant = 1, Name = "Skitter Mine", VariantName = "Litter", Color = amber,
                    Blurb = "Splits into four small seekers that each chase a different enemy.",
                    Cooldown = 26f, Duration = 12f, Power = 45f, Radius = 2.6f, Reach = 16f, DeployRange = 20f },
                new GadgetSpec { Kind = GadgetKind.Drone, Variant = 0, Name = "Moth Drone", VariantName = "Gadfly", Color = teal,
                    Blurb = "A hover drone that rides your shoulder and zaps enemies near you.",
                    Cooldown = 45f, Duration = 25f, Power = 9f, Interval = 0.55f, Reach = 24f },
                new GadgetSpec { Kind = GadgetKind.Drone, Variant = 1, Name = "Moth Drone", VariantName = "Suture", Color = green,
                    Blurb = "A hover drone that follows you and knits your health and armor back together.",
                    Cooldown = 50f, Duration = 18f, Power = 7f, Interval = 0.25f, Reach = 0f },
                new GadgetSpec { Kind = GadgetKind.Barrier, Variant = 0, Name = "Lattice Shield", VariantName = "Palisade", Color = blue,
                    Blurb = "A deployable wall of hard light. Enemy bullets stop on its face; yours pass out through the back.",
                    Cooldown = 35f, Duration = 30f, Shield = 450f, Radius = 1.6f, DeployRange = 10f },
                new GadgetSpec { Kind = GadgetKind.Barrier, Variant = 1, Name = "Lattice Shield", VariantName = "Buckler", Color = blue,
                    Blurb = "A handheld shield that faces where you aim and soaks enemy fire while you shoot.",
                    Cooldown = 28f, Duration = 12f, Shield = 260f, Radius = 0.8f },
                new GadgetSpec { Kind = GadgetKind.Scanner, Variant = 0, Name = "Chime Scanner", VariantName = "Ripple", Color = violet,
                    Blurb = "A pulse that marks every enemy around you through walls and shows their armor.",
                    Cooldown = 18f, Duration = 10f, Radius = 40f },
                new GadgetSpec { Kind = GadgetKind.Scanner, Variant = 1, Name = "Chime Scanner", VariantName = "Lance", Color = violet,
                    Blurb = "A long narrow pulse where you aim. Marked enemies are exposed and take more damage.",
                    Cooldown = 20f, Duration = 8f, Radius = 0f, Reach = 60f },
                new GadgetSpec { Kind = GadgetKind.Vapor, Variant = 0, Name = "Vapor Launcher", VariantName = "Balm", Color = green,
                    Blurb = "Lobs a canister that bursts into a healing cloud. Stand in it to recover.",
                    Cooldown = 28f, Duration = 9f, Power = 10f, Interval = 0.25f, Radius = 4.5f, DeployRange = 20f },
                new GadgetSpec { Kind = GadgetKind.Vapor, Variant = 1, Name = "Vapor Launcher", VariantName = "Scald", Color = flame,
                    Blurb = "Lobs a canister that bursts into a burning cloud that scorches anyone inside.",
                    Cooldown = 26f, Duration = 9f, Power = 4f, Interval = 0.5f, Radius = 4.5f, DeployRange = 20f }
            };
            for (int i = 0; i < s.Length; i++) s[i].Id = i;
            return s;
        }

        // ------------------------------------------------------------------ unlocks

        /// <summary>Player level that unlocks a variant: the first variants unlock one by one from level 1, the second from level 8.</summary>
        public static int UnlockLevel(int id)
        {
            int[] firstOrder = { 2, 3, 6, 4, 1, 5 };
            GadgetKind k = KindOf(id);
            int baseLevel = firstOrder[(int)k];
            return VariantOf(id) == 0 ? baseLevel : baseLevel + 7;
        }

        public static bool IsUnlocked(int id, int playerLevel, bool all)
        {
            if (id < 0 || id >= GadgetCount) return false;
            return all || playerLevel >= UnlockLevel(id);
        }

        // ------------------------------------------------------------------ effective numbers

        /// <summary>Gadget damage and healing grow with the player's level: x1 at level 1, about x2 at level 50.</summary>
        public static float LevelScale(int playerLevel)
        {
            return 1f + 0.02f * (Mathf.Clamp(playerLevel, 1, 50) - 1);
        }

        /// <summary>Seconds of cooldown after mods and gear sets (their cuts add, capped at MaxCooldownCut).</summary>
        public static float Cooldown(GadgetSpec s, float cut)
        {
            if (s == null) return 0f;
            return s.Cooldown * (1f - Mathf.Clamp(cut, 0f, MaxCooldownCut));
        }

        public static float Power(GadgetSpec s, int playerLevel, float bonus)
        {
            if (s == null) return 0f;
            return s.Power * LevelScale(playerLevel) * (1f + Mathf.Max(0f, bonus));
        }

        public static float Duration(GadgetSpec s, float bonus)
        {
            if (s == null) return 0f;
            return s.Duration * (1f + Mathf.Max(0f, bonus));
        }

        public static float Radius(GadgetSpec s, float bonus)
        {
            if (s == null) return 0f;
            return s.Radius * (1f + Mathf.Max(0f, bonus));
        }

        public static float ShieldHealth(GadgetSpec s, int playerLevel, float bonus)
        {
            if (s == null) return 0f;
            return s.Shield * LevelScale(playerLevel) * (1f + Mathf.Max(0f, bonus));
        }

        /// <summary>Blast damage at a distance: full in the inner 40 percent of the radius, down to 40 percent at the edge.</summary>
        public static float BlastFalloff(float distance, float radius)
        {
            if (radius <= 0f || distance > radius) return 0f;
            float t = Mathf.Clamp01((distance / radius - 0.4f) / 0.6f);
            return Mathf.Lerp(1f, 0.4f, t);
        }

        /// <summary>True when a point is inside a cone (apex, unit forward, half angle in degrees, reach).</summary>
        public static bool InCone(Vector3 apex, Vector3 forward, float halfAngle, float reach, Vector3 point)
        {
            Vector3 d = point - apex;
            float dist = d.magnitude;
            if (dist > reach) return false;
            if (dist < 0.01f) return true;
            float cos = Vector3.Dot(d / dist, forward.normalized);
            return cos >= Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        }

        // ------------------------------------------------------------------ mods

        public static string StatName(GadgetModStat s)
        {
            switch (s)
            {
                case GadgetModStat.Cooldown: return "Quickcharge";
                case GadgetModStat.Power: return "Overclock";
                case GadgetModStat.Duration: return "Longcell";
                default: return "Wideband";
            }
        }

        public static string StatText(GadgetModStat s, float v)
        {
            int pct = Mathf.RoundToInt(v * 100f);
            switch (s)
            {
                case GadgetModStat.Cooldown: return pct + "% shorter cooldown";
                case GadgetModStat.Power: return "+" + pct + "% damage and healing, +" + pct + "% shield";
                case GadgetModStat.Duration: return "+" + pct + "% duration";
                default: return "+" + pct + "% radius and reach";
            }
        }

        /// <summary>A mod's value from its rarity and a uniform roll in [0, 1).</summary>
        public static float RollModValue(GadgetModStat s, Rarity r, float roll)
        {
            int k = (int)r;
            float u = Mathf.Clamp01(roll);
            switch (s)
            {
                case GadgetModStat.Cooldown: return 0.04f + 0.03f * k + 0.02f * u;
                case GadgetModStat.Power: return 0.05f + 0.045f * k + 0.03f * u;
                case GadgetModStat.Duration: return 0.06f + 0.05f * k + 0.03f * u;
                default: return 0.05f + 0.035f * k + 0.02f * u;
            }
        }

        /// <summary>Chance a kill drops a gadget mod (veteran difficulty x1.25, capped at 1).</summary>
        public static float ModDropChance(LootSource s, bool veteran)
        {
            float c;
            switch (s)
            {
                case LootSource.Normal: c = 0.03f; break;
                case LootSource.Elite: c = 0.25f; break;
                case LootSource.Champion: c = 0.6f; break;
                case LootSource.Boss: c = 1f; break;
                case LootSource.Creature: c = 0.02f; break;
                case LootSource.EliteCreature: c = 0.15f; break;
                default: c = 0.4f; break;
            }
            return Mathf.Min(1f, c * (veteran ? 1.25f : 1f));
        }

        public static int ModSalvageValue(Rarity r)
        {
            int k = (int)r;
            return 15 + 25 * k * k;
        }

        /// <summary>
        /// The bonuses a socketed mod gives, as (cooldown cut, power, duration, radius) fractions. A null mod gives nothing.
        /// </summary>
        public static void ModBonuses(GadgetModSave mod, out float cooldownCut, out float power, out float duration, out float radius)
        {
            cooldownCut = power = duration = radius = 0f;
            if (mod == null) return;
            switch ((GadgetModStat)Mathf.Clamp(mod.Stat, 0, 3))
            {
                case GadgetModStat.Cooldown: cooldownCut = mod.Value; break;
                case GadgetModStat.Power: power = mod.Value; break;
                case GadgetModStat.Duration: duration = mod.Value; break;
                default: radius = mod.Value; break;
            }
        }

        /// <summary>
        /// The first-time hint for a slot's key ("Hold 1 to aim ..., let go to deploy"). hold: the key must be held to
        /// start at all (the controller's D-pad, where a tap switches weapons).
        /// </summary>
        public static string HintText(GadgetSpec s, string key, bool hold)
        {
            if (s == null) return "";
            if (s.SelfCast) return (hold ? "Hold " : "Press ") + key + " to use the " + s.FullName + ".";
            return "Hold " + key + " to aim the " + s.FullName + ", let go to deploy it.";
        }
    }
}
