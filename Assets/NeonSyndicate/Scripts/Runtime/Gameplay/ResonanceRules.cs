using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The four Resonance marks a crystal power leaves on what it hits. Each one comes from the Veil's crystal and
    /// decides what a Resonance Burst does to the foes around the target when a gun detonates it.
    /// </summary>
    public enum ResonanceState
    {
        None = 0,
        /// <summary>Fire powers (Ember Chain, Sunbolt): the burst throws burning crystal on everyone close.</summary>
        Kindled = 1,
        /// <summary>Lightning powers (Static Lance, Roc Call): the burst arcs on to two or three more foes.</summary>
        Stormlit = 2,
        /// <summary>Gravity powers (Brass Gravity Well): the burst is a shockwave that throws nearby foes down.</summary>
        Unmoored = 3,
        /// <summary>Glass Sight: a crystal shell grows over the foe and shatters into shards that stagger the rest.</summary>
        Glassbound = 4
    }

    /// <summary>
    /// Every number of the Resonance loop in one engine-free place (tested offline): how much crystal energy gun and
    /// melee hits give back, the per-second cap, which power leaves which mark, how long a mark lasts, the burst
    /// multipliers, the splash of each mark and the magazine refill.
    /// Guns charge powers, powers prime enemies, guns detonate primed enemies.
    /// </summary>
    public static class ResonanceRules
    {
        // ------------------------------------------------------------------ guns charge powers

        /// <summary>Crystal energy per second of continuous body hits, whatever the gun (fire rate is divided out).</summary>
        public const float ChargePerSecond = 4.5f;
        /// <summary>Weak points, criticals and headshots charge this much more.</summary>
        public const float CritChargeFactor = 1.8f;
        /// <summary>Extra energy for a gun kill (the takedown energy every kill gives comes on top).</summary>
        public const float GunKillBonus = 2f;
        /// <summary>Energy per landed melee hit.</summary>
        public const float MeleeCharge = 2.5f;
        /// <summary>Most energy gun and melee hits may give in any one second (a rolling bucket).</summary>
        public const float MaxChargePerSecond = 9f;
        /// <summary>Shortest and longest shot interval the charge maths uses (seconds).</summary>
        public const float MinShotSeconds = 0.03f;
        public const float MaxShotSeconds = 1.6f;

        // ------------------------------------------------------------------ powers prime enemies

        /// <summary>Seconds a mark stays on a foe.</summary>
        public const float MarkSeconds = 6f;
        /// <summary>Bosses and great beasts shake a mark off sooner.</summary>
        public const float BossMarkSeconds = 4.5f;
        /// <summary>Glass Sight marks this many hostiles at most (the nearest in front of you).</summary>
        public const int GlassSightMarks = 4;
        /// <summary>Glass Sight marks hostiles this close (metres).</summary>
        public const float GlassSightRange = 25f;

        // ------------------------------------------------------------------ guns detonate primed enemies

        /// <summary>Damage multiplier on the gun hit that sets off a burst.</summary>
        public const float BurstMultiplier = 2.5f;
        /// <summary>The same against bosses and great beasts.</summary>
        public const float BossBurstMultiplier = 1.75f;
        /// <summary>A burst hit on a breakable creature part deals this much part damage (instead of the burst multiplier).</summary>
        public const float BurstPartMultiplier = 2f;
        /// <summary>The splash never uses more than this much of the burst hit (a charged sniper shot on a boss).</summary>
        public const float SplashCap = 500f;

        /// <summary>The mark a power leaves, by its id in Abilities.Spells (None for powers that do not prime).</summary>
        public static ResonanceState StateForSpell(string spellId)
        {
            switch (spellId)
            {
                case "EMBER_CHAIN":
                case "SUNBOLT":
                case "SHARD_GAZE":
                    return ResonanceState.Kindled;
                case "STATIC_LANCE":
                case "ROOF_ROC_CALL":
                    return ResonanceState.Stormlit;
                case "BRASS_GRAVITY":
                    return ResonanceState.Unmoored;
                case "GLASS_SIGHT":
                    return ResonanceState.Glassbound;
                default:
                    return ResonanceState.None;
            }
        }

        public static string StateName(ResonanceState s)
        {
            switch (s)
            {
                case ResonanceState.Kindled: return "Kindled";
                case ResonanceState.Stormlit: return "Stormlit";
                case ResonanceState.Unmoored: return "Unmoored";
                case ResonanceState.Glassbound: return "Glassbound";
                default: return "";
            }
        }

        /// <summary>The glow colour of a mark (ring, shell, splash and the HUD).</summary>
        public static Color StateColor(ResonanceState s)
        {
            switch (s)
            {
                case ResonanceState.Kindled: return new Color(1f, 0.5f, 0.15f);
                case ResonanceState.Stormlit: return new Color(0.6f, 0.55f, 1f);
                case ResonanceState.Unmoored: return new Color(0.95f, 0.75f, 0.35f);
                case ResonanceState.Glassbound: return new Color(0.45f, 0.92f, 1f);
                default: return Color.white;
            }
        }

        public static float MarkDuration(bool boss)
        {
            return boss ? BossMarkSeconds : MarkSeconds;
        }

        public static float BurstDamageMultiplier(bool boss)
        {
            return boss ? BossBurstMultiplier : BurstMultiplier;
        }

        /// <summary>
        /// True when a hit may set off a Resonance Burst: only the player's own gun rounds (a weapon id and bullet damage).
        /// Melee, explosions, powers, companion and fused-vehicle hits never detonate a mark.
        /// </summary>
        public static bool Detonates(DamageType type, int weaponId, bool fromPlayer)
        {
            return fromPlayer && type == DamageType.Bullet && weaponId >= 0;
        }

        // ------------------------------------------------------------------ 04E: the Underheart bosses

        /// <summary>
        /// 04E: the mark a hit that landed on an Underheart boss body leaves (None for most hits). A boss body is not a
        /// character, so the powers and the companion that mark characters reach it only as a plain hit; this reads the
        /// hit instead. The quartz drakeling's Glass Lance (a companion power hit while the guide is the active companion)
        /// leaves Glassbound, as it does on any foe. A player's power that strikes the body directly without a weapon
        /// (today only Static Lance does: its bolt lands on whatever it hits) leaves Stormlit, the lance's own mark.
        /// A mark is only a mark: it never staggers, knocks down or stuns a boss.
        /// </summary>
        public static ResonanceState UnderBossMarkForHit(DamageType type, int weaponId, DamageOrigin origin, string activeCompanion)
        {
            if (type != DamageType.Power || weaponId >= 0) return ResonanceState.None;
            if (origin == DamageOrigin.Companion) return activeCompanion == BondIds.QuartzGuide ? ResonanceState.Glassbound : ResonanceState.None;
            if (origin == DamageOrigin.Player) return ResonanceState.Stormlit;
            return ResonanceState.None;
        }

        /// <summary>
        /// 04E: the radius of the mark ring under an Underheart boss (metres). The body can reach 8 m, and the ring a
        /// character gets (1.6 times its radius) would then cover the arena, so a boss ring stays between 1.2 and 4.5 m.
        /// </summary>
        public static float UnderBossMarkRadius(float bodyRadius)
        {
            if (float.IsNaN(bodyRadius)) return 1.2f;
            return Mathf.Clamp(bodyRadius * 0.8f + 0.5f, 1.2f, 4.5f);
        }

        /// <summary>
        /// Energy one gun hit gives: ChargePerSecond spread over the gun's shot interval and its pellets, so a fast SMG
        /// and a slow sniper rifle charge about the same per second of good aim. Criticals give more, kills a bonus.
        /// </summary>
        public static float GunCharge(float shotSeconds, int pellets, bool crit, bool kill)
        {
            float interval = Mathf.Clamp(shotSeconds, MinShotSeconds, MaxShotSeconds);
            float e = ChargePerSecond * interval / Mathf.Max(1, pellets);
            if (crit) e *= CritChargeFactor;
            if (kill) e += GunKillBonus;
            return e;
        }

        /// <summary>
        /// The seconds one shot of a gun takes on average: its interval, faster while a spin-up trait is spun, plus the
        /// hold of a charge-shot gun. Burst guns fire three rounds per three intervals, so the interval stands.
        /// </summary>
        public static float ShotSeconds(float interval, bool spinUp, bool chargeShot, float chargeSeconds)
        {
            float s = interval * (spinUp ? 0.8f : 1f);
            if (chargeShot) s += chargeSeconds * 0.75f;
            return Mathf.Clamp(s, MinShotSeconds, MaxShotSeconds);
        }

        /// <summary>Rounds a burst moves from the reserve into the magazine (fills it when the reserve allows).</summary>
        public static int RefillRounds(int clipSize, int clip, int reserve)
        {
            if (clipSize <= 0) return 0;
            return Mathf.Max(0, Mathf.Min(clipSize - Mathf.Max(0, clip), Mathf.Max(0, reserve)));
        }

        // ------------------------------------------------------------------ splash per mark

        /// <summary>Radius of the burst's splash around the target (metres). Stormlit is the chain's jump range.</summary>
        public static float SplashRadius(ResonanceState s)
        {
            switch (s)
            {
                case ResonanceState.Kindled: return 4.5f;
                case ResonanceState.Stormlit: return 9f;
                case ResonanceState.Unmoored: return 5f;
                case ResonanceState.Glassbound: return 4f;
                default: return 0f;
            }
        }

        /// <summary>Share of the burst hit each splashed foe takes.</summary>
        public static float SplashShare(ResonanceState s)
        {
            switch (s)
            {
                case ResonanceState.Kindled: return 0.35f;
                case ResonanceState.Stormlit: return 0.4f;
                case ResonanceState.Unmoored: return 0.3f;
                case ResonanceState.Glassbound: return 0.45f;
                default: return 0f;
            }
        }

        /// <summary>How many foes the Stormlit chain jumps to.</summary>
        public const int StormJumps = 3;

        /// <summary>Splash damage per foe from a burst hit of burstAmount (after the burst multiplier), capped.</summary>
        public static float SplashDamage(ResonanceState s, float burstAmount)
        {
            return Mathf.Min(Mathf.Max(0f, burstAmount), SplashCap) * SplashShare(s);
        }
    }

    /// <summary>
    /// A rolling cap on gained energy: holds up to Capacity and refills at Capacity per second, so gains in any one
    /// second never add up to much more than Capacity. Engine-free (the caller passes the time).
    /// </summary>
    public sealed class ChargeBucket
    {
        public readonly float Capacity;
        private float _level;
        private float _at;

        public ChargeBucket(float capacity)
        {
            Capacity = Mathf.Max(0f, capacity);
            _level = Capacity;
            _at = 0f;
        }

        public float Level { get { return _level; } }

        /// <summary>Takes up to want from the bucket at time now; returns what was granted.</summary>
        public float Take(float want, float now)
        {
            Refill(now);
            float g = Mathf.Clamp(want, 0f, _level);
            _level -= g;
            return g;
        }

        private void Refill(float now)
        {
            float dt = now - _at;
            _at = now;
            if (dt < 0f) dt = 0f;
            _level = Mathf.Min(Capacity, _level + dt * Capacity);
        }

        public void Reset()
        {
            _level = Capacity;
            _at = 0f;
        }
    }

    /// <summary>
    /// Which foes carry a Resonance mark and until when, keyed by an integer id (the runtime uses instance ids).
    /// Priming again replaces the mark and restarts its timer; a burst consumes it. Engine-free.
    /// </summary>
    public sealed class ResonanceMarkBook
    {
        private struct Mark
        {
            public ResonanceState State;
            public float Until;
            public float Since;
        }

        private readonly Dictionary<int, Mark> _marks = new Dictionary<int, Mark>();
        private readonly List<int> _scratch = new List<int>();

        public int Count { get { return _marks.Count; } }

        public void Prime(int id, ResonanceState s, float now, float seconds)
        {
            if (s == ResonanceState.None || seconds <= 0f) return;
            _marks[id] = new Mark { State = s, Until = now + seconds, Since = now };
        }

        /// <summary>The live mark on id at time now (None when unmarked or expired).</summary>
        public ResonanceState StateOf(int id, float now)
        {
            Mark m;
            if (!_marks.TryGetValue(id, out m) || now >= m.Until) return ResonanceState.None;
            return m.State;
        }

        /// <summary>0 to 1 of the mark's time left (1 just primed), 0 when unmarked.</summary>
        public float Remaining(int id, float now)
        {
            Mark m;
            if (!_marks.TryGetValue(id, out m) || now >= m.Until) return 0f;
            float total = Mathf.Max(0.01f, m.Until - m.Since);
            return Mathf.Clamp01((m.Until - now) / total);
        }

        /// <summary>Removes a live mark and reports its state; false when there was none.</summary>
        public bool Consume(int id, float now, out ResonanceState state)
        {
            state = StateOf(id, now);
            if (state == ResonanceState.None)
            {
                _marks.Remove(id);
                return false;
            }
            _marks.Remove(id);
            return true;
        }

        public void Remove(int id)
        {
            _marks.Remove(id);
        }

        /// <summary>Drops expired marks; their ids are added to removed (when given).</summary>
        public void Prune(float now, List<int> removed)
        {
            _scratch.Clear();
            foreach (KeyValuePair<int, Mark> kv in _marks) if (now >= kv.Value.Until) _scratch.Add(kv.Key);
            for (int i = 0; i < _scratch.Count; i++)
            {
                _marks.Remove(_scratch[i]);
                if (removed != null) removed.Add(_scratch[i]);
            }
        }

        /// <summary>Every marked id (live or not yet pruned) into into.</summary>
        public void Ids(List<int> into)
        {
            foreach (KeyValuePair<int, Mark> kv in _marks) into.Add(kv.Key);
        }

        public void Clear()
        {
            _marks.Clear();
        }
    }
}
