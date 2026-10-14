using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a creature leaves the world when it dies (no ragdolls: creatures dissolve).</summary>
    public enum CreatureDeath
    {
        /// <summary>The body sinks while it bursts into crystal sparks in its glow colour.</summary>
        Crystal,
        /// <summary>The body comes apart in shadow smoke (Hollows, shades).</summary>
        Smoke,
        /// <summary>Golems fall to rock chunks and dust.</summary>
        Crumble,
        /// <summary>Wraiths shatter into glass shards.</summary>
        Shatter,
        /// <summary>A short boss death: the body shakes and flares before it bursts.</summary>
        BossBurst
    }

    /// <summary>Where an ambient creature group is placed (the ecology spawner turns this into a real point).</summary>
    public enum SpawnSpot
    {
        /// <summary>Open ground in the wilderness around the player.</summary>
        Open,
        /// <summary>Inside a park block.</summary>
        Park,
        /// <summary>An empty dirt lot or a warehouse yard (the city's construction ground).</summary>
        Construction,
        /// <summary>The sidewalks of the Grave Frequency.</summary>
        Sidewalk,
        /// <summary>The floor of the quarry.</summary>
        Quarry,
        /// <summary>The shore of Rimeglass Lake.</summary>
        FrostLake,
        /// <summary>The rim of Glass Row Canyon (the drake's roost).</summary>
        CanyonRim,
        /// <summary>The Inner World's crystal growths.</summary>
        Inner
    }

    /// <summary>What surrounds the player, as plain values the spawn table reads.</summary>
    public struct HabitatContext
    {
        public bool Inner;
        /// <summary>Inside the city (not wilderness).</summary>
        public bool City;
        public bool Park;
        public bool Construction;
        public bool Grave;
        public bool Frost;
        public bool FrostLake;
        public bool Badlands;
        public bool Quarry;
        public bool Canyon;
        /// <summary>Atmosphere.NightFactor: 0 full day, 1 full night.</summary>
        public float Night;
        public Weather Weather;

        public bool IsNight { get { return Night > 0.55f; } }
    }

    /// <summary>One weighted entry the ecology may spawn.</summary>
    public struct SpawnCandidate
    {
        public string Species;
        public float Weight;
        public int Min, Max;
        public SpawnSpot Spot;

        public SpawnCandidate(string species, float weight, int min, int max, SpawnSpot spot)
        {
            Species = species;
            Weight = weight;
            Min = min;
            Max = max;
            Spot = spot;
        }
    }

    /// <summary>What the territorial brain does this moment.</summary>
    public enum TerritorialStep
    {
        Calm,
        Warn,
        Attack
    }

    /// <summary>
    /// The ambient spawn table: which species live where, by place, time, weather and world, following the bestiary.
    /// Outer City streets only at night (parks: jackals; construction ground and the quarry: mites and shard golems;
    /// Grave Frequency streets: hollow shades); the Badlands and the Frostline day and night with more at night; the
    /// Inner World denser. Pure (tested offline).
    /// </summary>
    public static class CreatureEcology
    {
        /// <summary>Fills into with every species that may spawn here now (weights already include weather).</summary>
        public static void Candidates(HabitatContext c, List<SpawnCandidate> into)
        {
            into.Clear();
            bool night = c.IsNight;
            if (c.Inner)
            {
                into.Add(new SpawnCandidate("hollow", 3f, 2, 4, SpawnSpot.Inner));
                into.Add(new SpawnCandidate("hollow_shade", 2f, 1, 2, SpawnSpot.Inner));
                into.Add(new SpawnCandidate("mirror_wraith", 1.2f, 1, 1, SpawnSpot.Inner));
                into.Add(new SpawnCandidate("crystal_basilisk", 1f, 1, 1, SpawnSpot.Inner));
                into.Add(new SpawnCandidate("violet_wyvern", 1f, 1, 2, SpawnSpot.Inner));
            }
            else if (c.City)
            {
                if (!night) return;
                if (c.Park) into.Add(new SpawnCandidate("neon_jackal", 3f, 3, 5, SpawnSpot.Park));
                if (c.Construction)
                {
                    into.Add(new SpawnCandidate("glass_mite", 2.5f, 8, 16, SpawnSpot.Construction));
                    into.Add(new SpawnCandidate("shard_golem", 1.2f, 1, 1, SpawnSpot.Construction));
                }
                if (c.Grave) into.Add(new SpawnCandidate("hollow_shade", 2.5f, 1, 2, SpawnSpot.Sidewalk));
            }
            else if (c.Frost)
            {
                into.Add(new SpawnCandidate("frost_stalker", 2.5f, 1, 2, SpawnSpot.Open));
                into.Add(new SpawnCandidate("rime_griffin", c.FrostLake ? 2.5f : 0.5f, 1, 1, c.FrostLake ? SpawnSpot.FrostLake : SpawnSpot.Open));
                if (night) into.Add(new SpawnCandidate("snow_hollow", 2.5f, 2, 3, SpawnSpot.Open));
            }
            else if (c.Badlands)
            {
                if (!night) into.Add(new SpawnCandidate("sun_griffin", 2f, 1, 2, SpawnSpot.Open));
                into.Add(new SpawnCandidate("sand_golem", 1.5f, 1, 1, SpawnSpot.Open));
                into.Add(new SpawnCandidate("dune_wyrm", 0.8f, 1, 1, SpawnSpot.Open));
                if (c.Canyon) into.Add(new SpawnCandidate("quartz_drake", 1.5f, 1, 1, SpawnSpot.CanyonRim));
                if (night) into.Add(new SpawnCandidate("neon_jackal", 2f, 3, 5, SpawnSpot.Open));
                if (c.Quarry)
                {
                    if (night)
                    {
                        into.Add(new SpawnCandidate("glass_mite", 2.5f, 8, 16, SpawnSpot.Quarry));
                        into.Add(new SpawnCandidate("shard_golem", 1.5f, 1, 1, SpawnSpot.Quarry));
                    }
                    into.Add(new SpawnCandidate("sand_golem", 1f, 1, 1, SpawnSpot.Quarry));
                    into.Add(new SpawnCandidate("crystal_basilisk", 0.6f, 1, 1, SpawnSpot.Quarry));
                }
            }
            for (int i = 0; i < into.Count; i++)
            {
                SpawnCandidate s = into[i];
                s.Weight *= WeatherFactor(s.Species, c.Weather);
                into[i] = s;
            }
        }

        /// <summary>How much more or less often a species appears in a weather (flyers stay down in storms and sandstorms,
        /// stalkers and shades love fog, golems and the wyrm rise in sandstorms).</summary>
        public static float WeatherFactor(string species, Weather w)
        {
            bool flyer = species == "rime_griffin" || species == "sun_griffin" || species == "violet_wyvern" || species == "quartz_drake";
            switch (w)
            {
                case Weather.Rain:
                case Weather.Storm:
                    if (flyer) return 0.3f;
                    if (species == "hollow_shade" || species == "hollow") return 1.3f;
                    if (species == "frost_stalker") return 1.2f;
                    return 1f;
                case Weather.Fog:
                    if (species == "frost_stalker") return 1.5f;
                    if (species == "hollow_shade") return 1.6f;
                    if (species == "hollow" || species == "snow_hollow") return 1.3f;
                    return 1f;
                case Weather.Sandstorm:
                    if (flyer) return 0.2f;
                    if (species == "sand_golem") return 2f;
                    if (species == "dune_wyrm") return 1.5f;
                    return 1f;
                default:
                    return 1f;
            }
        }

        /// <summary>Picks one candidate with roll in [0, 1); false when nothing may spawn.</summary>
        public static bool Pick(List<SpawnCandidate> candidates, float roll, out SpawnCandidate pick)
        {
            pick = default(SpawnCandidate);
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++) total += Mathf.Max(0f, candidates[i].Weight);
            if (total <= 0f) return false;
            float r = Mathf.Clamp01(roll) * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                float w = Mathf.Max(0f, candidates[i].Weight);
                if (w <= 0f) continue;
                pick = candidates[i];
                r -= w;
                if (r < 0f) return true;
            }
            return pick.Species != null;
        }

        /// <summary>Seconds between ambient spawn attempts: busier at night in the wilderness and densest in the Inner World.</summary>
        public static float SpawnInterval(HabitatContext c)
        {
            if (c.Inner) return 7f;
            if (c.City) return c.IsNight ? 15f : 30f;
            return c.IsNight ? 9f : 14f;
        }

        /// <summary>Group size for a candidate at a roll in [0, 1); swarms are halved on the Low preset.</summary>
        public static int GroupSize(SpawnCandidate s, float roll, GraphicsPreset preset)
        {
            int n = s.Min + Mathf.FloorToInt(Mathf.Clamp01(roll) * (s.Max - s.Min + 1));
            n = Mathf.Clamp(n, s.Min, Mathf.Max(s.Min, s.Max));
            if (preset == GraphicsPreset.Low && n > 4) n = Mathf.Max(4, n / 2);
            return Mathf.Max(1, n);
        }

        /// <summary>The ambient limits by graphics preset: groups alive at once and bodies (see BodyCost).</summary>
        public static void Caps(GraphicsPreset preset, out int groups, out float bodies)
        {
            switch (preset)
            {
                case GraphicsPreset.Low: groups = 3; bodies = 8f; break;
                case GraphicsPreset.Medium: groups = 4; bodies = 12f; break;
                case GraphicsPreset.Ultra: groups = 7; bodies = 22f; break;
                default: groups = 6; bodies = 18f; break;
            }
        }

        /// <summary>How much of the body budget one creature uses: a mite is a quarter, great beasts and bosses two.</summary>
        public static float BodyCost(CreatureSpecies s)
        {
            if (s == null) return 1f;
            if (s.Id == "glass_mite") return 0.25f;
            if (s.GreatBeast || s.Boss) return 2f;
            return 1f;
        }

        /// <summary>At most this many of a species alive at once from the ambient spawner (great beasts are unique).</summary>
        public static int MaxAlive(CreatureSpecies s)
        {
            if (s == null) return 0;
            if (s.GreatBeast || s.Boss) return 1;
            if (s.Id == "glass_mite") return 32;
            return 8;
        }
    }

    /// <summary>Loot rolls from a species table (MaterialFind adds to every chance). Pure.</summary>
    public static class CreatureLoot
    {
        /// <summary>Rolls every entry once; adds (material, count) pairs to into. Elite kills roll each entry twice.</summary>
        public static void Roll(LootRoll[] table, float materialFind, bool elite, GRand r, List<KeyValuePair<string, int>> into)
        {
            if (table == null || r == null) return;
            int passes = elite ? 2 : 1;
            for (int p = 0; p < passes; p++)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    LootRoll l = table[i];
                    if (string.IsNullOrEmpty(l.Material)) continue;
                    float chance = Mathf.Clamp01(l.Chance + Mathf.Max(0f, materialFind));
                    if (r.Value() >= chance) continue;
                    int lo = Mathf.Max(1, l.Min), hi = Mathf.Max(lo, l.Max);
                    int n = r.Range(lo, hi + 1);
                    Add(into, l.Material, n);
                }
            }
        }

        private static void Add(List<KeyValuePair<string, int>> into, string id, int n)
        {
            for (int i = 0; i < into.Count; i++)
            {
                if (into[i].Key != id) continue;
                into[i] = new KeyValuePair<string, int>(id, into[i].Value + n);
                return;
            }
            into.Add(new KeyValuePair<string, int>(id, n));
        }
    }

    /// <summary>Perception, temperament, fleeing, leashing and the fairness rules of creature fights. Pure.</summary>
    public static class CreatureBrainLogic
    {
        /// <summary>
        /// How far a creature notices the player: the species' Sense times SkillMods.CreatureSense, halved while crouched
        /// and still (three quarters when only crouched or only still), a little further when sprinting or driving,
        /// shorter in fog and a little shorter at night for day hunters (nightCreature false).
        /// </summary>
        public static float NoticeRange(float sense, float senseMod, bool crouched, float playerSpeed, bool inVehicle, float night, bool nightCreature, bool fog)
        {
            float r = Mathf.Max(0f, sense) * Mathf.Clamp(senseMod, 0.1f, 3f);
            bool still = playerSpeed < 0.6f;
            if (inVehicle) r *= 1.3f;
            else if (crouched && still) r *= 0.5f;
            else if (crouched || still) r *= 0.75f;
            else if (playerSpeed > 5.5f) r *= 1.15f;
            if (fog) r *= 0.7f;
            if (!nightCreature) r *= Mathf.Lerp(1f, 0.75f, Mathf.Clamp01(night));
            return r;
        }

        /// <summary>
        /// True when a creature looking along its facing sees a target at distance and at angle degrees off its facing:
        /// full range inside the 240 degree front view, a third of it behind (hearing and smell).
        /// </summary>
        public static bool CanNotice(float distance, float angle, float range)
        {
            if (distance > range) return false;
            if (Mathf.Abs(angle) <= 120f) return true;
            return distance <= range * 0.35f;
        }

        /// <summary>How far a noise carries for a creature with the given sense (gunshots twice as far as the sense).</summary>
        public static float NoiseReach(ThreatKind kind, float sense)
        {
            switch (kind)
            {
                case ThreatKind.Gunshot: return sense * 2f;
                case ThreatKind.Explosion: return sense * 2.5f;
                case ThreatKind.Power: return sense * 1.2f;
                case ThreatKind.Violence: return sense * 0.8f;
                default: return 0f;
            }
        }

        /// <summary>Health fraction under which a prey-like species runs (0 = never); a pack that lost its leader breaks sooner.</summary>
        public static float FleeHealth(string speciesId, bool leaderLost)
        {
            switch (speciesId)
            {
                case "neon_jackal": return leaderLost ? 0.5f : 0.2f;
                case "rime_griffin":
                case "sun_griffin": return 0.18f;
                case "hollow_shade": return 0.15f;
                case "glass_mite": return leaderLost ? 0.6f : 0f;
                default: return 0f;
            }
        }

        /// <summary>True when an ambient creature has been drawn too far from home and should go back (and heal).</summary>
        public static bool ShouldLeash(float distanceFromHome, float leash, bool persistent)
        {
            if (persistent || leash <= 0f) return false;
            return distanceFromHome > leash;
        }

        /// <summary>
        /// A territorial creature's choice: calm outside the warning radius, a threat display inside it, and an attack once
        /// the intruder comes inside strikeRadius, stays longer than patience seconds, or has already hurt it.
        /// </summary>
        public static TerritorialStep Territorial(float distance, float warnRadius, float strikeRadius, float warnedFor, float patience, bool provoked)
        {
            if (provoked) return TerritorialStep.Attack;
            if (distance <= strikeRadius) return TerritorialStep.Attack;
            if (distance > warnRadius) return TerritorialStep.Calm;
            return warnedFor >= patience ? TerritorialStep.Attack : TerritorialStep.Warn;
        }

        /// <summary>
        /// Caps one hit so no creature attack takes more than a share of the player's health in one go (no instant
        /// deaths): 25 percent on Story, 35 on Balanced, 50 on Veteran, measured after the difficulty's damage scale.
        /// raw is the hit before the scale, damageTaken the difficulty's scale (CombatDirector.PlayerDamageTaken).
        /// </summary>
        public static float CapHit(float raw, float playerMaxHealth, int difficulty, float damageTaken)
        {
            float share = difficulty <= 0 ? 0.25f : (difficulty == 1 ? 0.35f : 0.5f);
            float capAfterScale = Mathf.Max(1f, playerMaxHealth) * share;
            float capRaw = capAfterScale / Mathf.Max(0.05f, damageTaken);
            return Mathf.Min(Mathf.Max(0f, raw), capRaw);
        }

        /// <summary>Damage multiplier for an element against a species: weakness 1.5, resistance 0.6.</summary>
        public static float ElementMultiplier(Element hit, Element weakness, Element resists)
        {
            if (hit == Element.None) return 1f;
            if (hit == weakness) return 1.5f;
            if (hit == resists) return 0.6f;
            return 1f;
        }

        /// <summary>Seconds a broken poise staggers a creature: longer for bigger bodies, shorter for bosses (they recover).</summary>
        public static float StaggerSeconds(float height, bool boss)
        {
            float s = Mathf.Clamp(0.8f + height * 0.25f, 0.8f, 2.4f);
            return boss ? s * 0.8f : s;
        }

        /// <summary>Degrees per second a creature turns: big bodies turn slowly, so a quick player can flank them.</summary>
        public static float TurnRate(float height)
        {
            return Mathf.Clamp(520f / Mathf.Sqrt(Mathf.Max(0.2f, height)), 90f, 720f);
        }

        /// <summary>Rest between attacks by difficulty (Story 0, Balanced 1, Veteran 2) at roll in [0, 1), times the species' tempo.</summary>
        public static float RestBetweenAttacks(int difficulty, float roll, float tempo)
        {
            float lo, hi;
            if (difficulty <= 0) { lo = 2.2f; hi = 3.6f; }
            else if (difficulty == 1) { lo = 1.3f; hi = 2.6f; }
            else { lo = 0.75f; hi = 1.6f; }
            return Mathf.Lerp(lo, hi, Mathf.Clamp01(roll)) * Mathf.Clamp(tempo, 0.3f, 3f);
        }
    }

    /// <summary>Flocking for Glass Mite swarms: separation, alignment, cohesion and the pull of the shared target. Pure.</summary>
    public static class SwarmMath
    {
        /// <summary>
        /// Desired velocity for one mite: keep separation from mates closer than spacing, match the swarm's velocity, stay
        /// near the swarm centre and stream toward the target (orbiting it at orbit metres when no bite is due).
        /// </summary>
        public static Vector3 Steer(Vector3 pos, Vector3[] mates, int count, Vector3 swarmCenter, Vector3 swarmVelocity, Vector3 target, float speed, float spacing, float orbit, float orbitPhase)
        {
            Vector3 sep = Vector3.zero;
            float s2 = spacing * spacing;
            for (int i = 0; i < count; i++)
            {
                Vector3 d = pos - mates[i];
                float m2 = d.sqrMagnitude;
                if (m2 < 1e-6f || m2 > s2) continue;
                sep += d / Mathf.Max(0.02f, m2);
            }
            Vector3 toCenter = swarmCenter - pos;
            Vector3 want = target;
            if (orbit > 0f)
            {
                float a = orbitPhase;
                want = target + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * orbit;
            }
            Vector3 toTarget = want - pos;
            float dist = toTarget.magnitude;
            Vector3 seek = dist > 1e-4f ? toTarget / dist * Mathf.Min(speed, dist * 3f) : Vector3.zero;
            Vector3 v = seek + sep * (spacing * 0.6f) + toCenter * 0.4f + swarmVelocity * 0.25f;
            v.y = Mathf.Clamp(v.y, -speed, speed);
            float mag = v.magnitude;
            return mag > speed ? v / mag * speed : v;
        }
    }

    /// <summary>Flight paths for winged creatures: circling, diving and the landing spot. Pure.</summary>
    public static class FlightMath
    {
        /// <summary>A point on a circle of radius around center at angle degrees, height metres above center.</summary>
        public static Vector3 OrbitPoint(Vector3 center, float radius, float angle, float height)
        {
            float a = angle * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }

        /// <summary>Degrees per second to travel the circle at speed (positive turns clockwise seen from above).</summary>
        public static float OrbitRate(float radius, float speed)
        {
            return speed / Mathf.Max(1f, radius) * Mathf.Rad2Deg;
        }

        /// <summary>Where a dive aims: where the target will be after lead seconds, at chest height.</summary>
        public static Vector3 DiveAim(Vector3 target, Vector3 targetVelocity, float lead)
        {
            Vector3 v = targetVelocity;
            v.y = 0f;
            return target + v * Mathf.Clamp(lead, 0f, 1.5f);
        }
    }
}
