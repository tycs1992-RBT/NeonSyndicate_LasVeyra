using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a resident slot decides its temperament: the region's rule (Default), or a campaign choice's override.</summary>
    public enum ResidentTemper04E
    {
        /// <summary>ResidentRules04E.Plan decides (a crab at a nest outside the refuges and study points is territorial).</summary>
        Default = 0,
        /// <summary>Calm until actually hurt, whatever the region says.</summary>
        Calm = 1,
        /// <summary>Warns, then defends its nest (never inside a refuge, where residents cannot be hurt).</summary>
        Territorial = 2
    }

    /// <summary>One authored group of residents: a species at a named Underheart anchor.</summary>
    public struct ResidentSlot04E
    {
        public string Region;
        public string Anchor;
        public string Species;
        public int Count;
        // ---- 04E choice ecology (ChoiceEcology04E); the authored slots keep the defaults.
        /// <summary>Temperament override (Default: the region's rule).</summary>
        public ResidentTemper04E Temper;
        /// <summary>Hatchlings that stay at their nest instead of following warmth (a returned clutch).</summary>
        public bool Sheltered;
        /// <summary>The campaign choice that placed or changed this slot ("matron_fate=contain"); "" for an authored slot.</summary>
        public string Source;

        public ResidentSlot04E(string region, string anchor, string species, int count)
        {
            Region = region;
            Anchor = anchor;
            Species = species;
            Count = count;
            Temper = ResidentTemper04E.Default;
            Sheltered = false;
            Source = "";
        }

        public ResidentSlot04E(string region, string anchor, string species, int count, ResidentTemper04E temper, bool sheltered, string source)
        {
            Region = region;
            Anchor = anchor;
            Species = species;
            Count = count;
            Temper = temper;
            Sheltered = sheltered;
            Source = source ?? "";
        }

        /// <summary>True for a slot a campaign choice placed or changed.</summary>
        public bool FromChoice { get { return !string.IsNullOrEmpty(Source); } }
    }

    /// <summary>How one resident body is spawned (the runtime turns this into CreatureSpawnOptions).</summary>
    public struct ResidentPlan04E
    {
        /// <summary>Calm until actually hurt (the territorial crab outside a refuge is not).</summary>
        public bool Calm;
        /// <summary>Damage cannot hurt or provoke it (every resident in a refuge, the venue nest's hatchlings).</summary>
        public bool Invulnerable;
        /// <summary>Gives no loot, Lumen or kill (no farming of residents).</summary>
        public bool NoRewards;
        /// <summary>A crab that may warn and defend its nest.</summary>
        public bool Territorial;
    }

    /// <summary>The kiln crab's warning as the player sees it.</summary>
    public enum CrabMood04E
    {
        Settled,
        Warning,
        Defending
    }

    /// <summary>
    /// Engine-free rules of the 04E underground residents: which regions are populated (the player's region and the ones
    /// next to it only), the authored slots per region, caps by graphics preset (halved while a finite encounter runs),
    /// the never-hostile refuge rule, the kiln crab's warn and settle reading, where an ember hatchling heads (warmth), and
    /// the cinder moth's drift around warm crystal.
    /// </summary>
    public static class ResidentRules04E
    {
        /// <summary>At most this many residents in one region.</summary>
        public const int MaxPerRegion = 6;
        /// <summary>A hatchling follows the player (its warmth) within this many metres (twice as far once it is being guided).</summary>
        public const float FollowRadius = 6f;
        /// <summary>Hatchlings keep this far from whoever they follow.</summary>
        public const float FollowSpacing = 1.4f;
        /// <summary>Moths scatter when the player comes this close.</summary>
        public const float MothScatterRadius = 2.6f;
        /// <summary>The crab warns this long before it attacks (the territorial brain's patience).</summary>
        public const float CrabPatience = 5f;
        /// <summary>A resident that died (a defending crab, say) leaves its slot empty this long before a new one settles.</summary>
        public const float RespawnSeconds = 180f;

        /// <summary>The authored resident slots, region by region (anchors from UnderheartIds.Anchors).</summary>
        public static readonly ResidentSlot04E[] Slots =
        {
            new ResidentSlot04E(UnderheartIds.ServiceLevel, "service.vent", EnemyIds04E.CinderMoth, 3),
            new ResidentSlot04E(UnderheartIds.LanternRefuge, "refuge.residents", EnemyIds04E.CinderMoth, 3),
            new ResidentSlot04E(UnderheartIds.LanternRefuge, "refuge.map_board", EnemyIds04E.CinderMoth, 2),
            new ResidentSlot04E(UnderheartIds.LanternRefuge, "refuge.sample_drop", EnemyIds04E.KilnCrab, 1),
            new ResidentSlot04E(UnderheartIds.EmberCanals, "canals.cool_pocket", EnemyIds04E.CinderMoth, 3),
            new ResidentSlot04E(UnderheartIds.EmberCanals, "canals.branch_cache", EnemyIds04E.KilnCrab, 1),
            new ResidentSlot04E(UnderheartIds.EmberCanals, "canals.rest", EnemyIds04E.EmberHatchling, 2),
            new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.study_a", EnemyIds04E.CinderMoth, 2),
            new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.study_b", EnemyIds04E.KilnCrab, 1),
            new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.study_c", EnemyIds04E.EmberHatchling, 2),
            new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.habitat", EnemyIds04E.KilnCrab, 1),
            new ResidentSlot04E(UnderheartIds.CrucibleWorkshops, "workshops.store_room", EnemyIds04E.KilnCrab, 1),
            new ResidentSlot04E(UnderheartIds.CrucibleWorkshops, "workshops.craft_point", EnemyIds04E.CinderMoth, 2),
            new ResidentSlot04E(UnderheartIds.AshReliquary, "reliquary.columns", EnemyIds04E.CinderMoth, 3),
            new ResidentSlot04E(UnderheartIds.AshReliquary, "reliquary.archive_hall", EnemyIds04E.CinderMoth, 2),
            new ResidentSlot04E(UnderheartIds.FoundryCrown, "crown.staging", EnemyIds04E.CinderMoth, 2)
        };

        /// <summary>The venue anchor of the Pressure House nest where the displaced hatchlings are first met.</summary>
        public const string VenueNestAnchor = "nest";
        public const int VenueNestHatchlings = 3;
        /// <summary>Where the hatchlings live once they have been guided home ("juveniles" = "guide").</summary>
        public const string GuidedHomeAnchor = "service.juveniles";

        /// <summary>Total residents alive at once by graphics preset (Low 0, Medium 1, High 2, Ultra 3); halved during a fight.</summary>
        public static int Cap(int preset, bool encounterRunning)
        {
            int c = preset <= 0 ? 6 : (preset == 1 ? 9 : (preset == 2 ? 12 : 16));
            return encounterRunning ? Mathf.Max(2, c / 2) : c;
        }

        /// <summary>Index of a region in the Underheart chain (-1 for the surface or an unknown id).</summary>
        public static int RegionIndex(string region)
        {
            return string.IsNullOrEmpty(region) ? -1 : Array.IndexOf(UnderheartIds.Regions, region);
        }

        /// <summary>
        /// The regions that carry residents now: the player's region and the regions next to it in the chain. On the
        /// surface nothing is populated, except the service level when the player is at the Pressure House entrance.
        /// </summary>
        public static void DesiredRegions(string playerRegion, bool nearSurfaceEntrance, List<string> into)
        {
            into.Clear();
            int i = RegionIndex(playerRegion);
            if (i < 0)
            {
                if (nearSurfaceEntrance) into.Add(UnderheartIds.ServiceLevel);
                return;
            }
            into.Add(UnderheartIds.Regions[i]);
            if (i > 0) into.Add(UnderheartIds.Regions[i - 1]);
            if (i + 1 < UnderheartIds.Regions.Length) into.Add(UnderheartIds.Regions[i + 1]);
        }

        /// <summary>True when two regions are the same or next to each other in the chain.</summary>
        public static bool CurrentOrAdjacent(string playerRegion, string region)
        {
            int a = RegionIndex(playerRegion), b = RegionIndex(region);
            if (a < 0 || b < 0) return false;
            return Mathf.Abs(a - b) <= 1;
        }

        /// <summary>A refuge never has a hostile resident (Lantern Refuge, and the Foundry Crown staging refuge).</summary>
        public static bool IsRefuge(string region, string anchor)
        {
            if (region == UnderheartIds.LanternRefuge) return true;
            return anchor == "crown.staging";
        }

        /// <summary>The Pyre Gardens study points: the residents there are feeding, not nesting, so even a crab stays calm
        /// while the player observes it (Gardens That Burn asks the player to study non-hostile creatures up close).</summary>
        public static bool IsObservationSite(string anchor)
        {
            return anchor != null && anchor.IndexOf(".study_", StringComparison.Ordinal) > 0;
        }

        /// <summary>The spawn plan of one resident. Refuge residents and the venue nest are calm and cannot be hurt; only
        /// a crab at a nest outside the refuges and the study points is territorial.</summary>
        public static ResidentPlan04E Plan(string species, string region, string anchor, bool inVenue)
        {
            ResidentPlan04E p = new ResidentPlan04E();
            p.NoRewards = true;
            bool refuge = inVenue || IsRefuge(region, anchor);
            bool crab = species == EnemyIds04E.KilnCrab;
            p.Territorial = crab && !refuge && !IsObservationSite(anchor);
            p.Calm = !p.Territorial;
            p.Invulnerable = refuge;
            return p;
        }

        /// <summary>
        /// A spawn plan with a slot's temperament override applied. Calm makes it calm until hurt; Territorial makes it
        /// warn and defend, except in a refuge (refuge residents cannot be hurt and are never hostile). Default keeps it.
        /// </summary>
        public static ResidentPlan04E ApplyTemper(ResidentPlan04E plan, ResidentTemper04E temper, bool refuge)
        {
            switch (temper)
            {
                case ResidentTemper04E.Calm:
                    plan.Territorial = false;
                    plan.Calm = true;
                    break;
                case ResidentTemper04E.Territorial:
                    if (refuge) break;
                    plan.Territorial = true;
                    plan.Calm = false;
                    break;
            }
            return plan;
        }

        /// <summary>
        /// The key of resident i of a slot: anchor, species, temperament and shelter, so a choice that changes any of them
        /// replaces the body (off camera) instead of leaving one spawned under the old plan.
        /// </summary>
        public static string SlotKey(ResidentSlot04E slot, int index)
        {
            return slot.Anchor + ":" + slot.Species + ":" + (int)slot.Temper + (slot.Sheltered ? "s" : "") + "#" + index;
        }

        /// <summary>
        /// The crab's mood from the brain's mode: a warning (the amber ring fills over the patience) settles when the
        /// intruder leaves, and only turns into a defence once the ring is full or the crab was hurt.
        /// </summary>
        public static CrabMood04E CrabMood(bool hunting, bool warning)
        {
            if (hunting) return CrabMood04E.Defending;
            return warning ? CrabMood04E.Warning : CrabMood04E.Settled;
        }

        /// <summary>Seconds left on the crab's warning ring (never negative).</summary>
        public static float CrabWarningLeft(float warnedFor)
        {
            return Mathf.Max(0f, CrabPatience - Mathf.Max(0f, warnedFor));
        }

        /// <summary>
        /// Where an ember hatchling heads: sheltered ones stay at their nest; otherwise they follow the player's warmth
        /// while the player is close (on the same layer and, in a venue, inside the room), keeping a small spacing; else home.
        /// slot spreads siblings around whoever they follow. Returns the goal; follow says whether it follows the player.
        /// </summary>
        public static Vector3 HatchlingGoal(Vector3 self, Vector3 home, Vector3 player, bool playerSameSpace, bool sheltered, bool guided, int slot, out bool follow)
        {
            follow = false;
            if (sheltered) return home + SlotOffset(slot, 0.6f);
            float radius = guided ? FollowRadius * 2f : FollowRadius;
            float dx = player.x - self.x, dz = player.z - self.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            float dh = player.x - home.x, dzh = player.z - home.z;
            float fromHome = Mathf.Sqrt(dh * dh + dzh * dzh);
            // They never trail the player further than a room away from their nest unless they are being guided.
            float tether = guided ? 40f : 9f;
            if (playerSameSpace && d <= radius && fromHome <= tether)
            {
                follow = true;
                Vector3 back = d > 0.01f ? new Vector3(-dx / d, 0f, -dz / d) : Vector3.back;
                return new Vector3(player.x, self.y, player.z) + back * FollowSpacing + SlotOffset(slot, 0.7f);
            }
            return home + SlotOffset(slot, 0.6f);
        }

        /// <summary>A small fixed offset per sibling (golden angle) so a clutch never stacks on one point.</summary>
        public static Vector3 SlotOffset(int slot, float radius)
        {
            float a = slot * 2.39996f;
            float r = radius * (0.6f + 0.4f * ((slot % 3) / 2f));
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        /// <summary>
        /// A cinder moth's drift around its warm crystal at time t: a slow, slightly lopsided loop between 0.6 and 1.8 m out
        /// and 0.8 to 2.2 m up, phase-shifted per moth. Returns the offset from the warm point.
        /// </summary>
        public static Vector3 MothDrift(float t, int seed)
        {
            float ph = (seed & 1023) * 0.0061f * Mathf.PI * 2f;
            float speed = 0.35f + (seed & 7) * 0.035f;
            float a = t * speed + ph;
            float r = 1.2f + 0.6f * Mathf.Sin(t * 0.21f + ph * 1.7f);
            float h = 1.5f + 0.7f * Mathf.Sin(t * 0.47f + ph * 2.3f);
            return new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a * 1.3f) * r * 0.8f);
        }

        /// <summary>A deterministic placement offset around an anchor for resident i (tries spread outward).</summary>
        public static Vector3 PlacementOffset(string anchor, int index, int attempt)
        {
            int h = 17;
            if (anchor != null) for (int i = 0; i < anchor.Length; i++) h = h * 31 + anchor[i];
            float a = ((h & 0xffff) / 65535f) * 360f + index * 137.508f + attempt * 61f;
            float r = 1.1f + 0.45f * index + 0.5f * attempt;
            float rad = a * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * r, 0f, Mathf.Cos(rad) * r);
        }
    }
}
