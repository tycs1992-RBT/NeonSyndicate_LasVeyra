using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The family a 04E one-shot belongs to (sets its default level, reach and repeat rules).</summary>
    public enum UhAudioCueKind
    {
        Ui,
        Companion,
        Fusion,
        World,
        Boss,
        Creature
    }

    /// <summary>
    /// How one 04E one-shot plays: its level, how close together two plays of the same id may be, how far it carries and
    /// how much its pitch varies. The rate limit is per id: a second play inside MinGap within Radius metres of an earlier
    /// play is dropped (one collider hit, one sound), plays farther apart count as separate events up to Burst per gap.
    /// </summary>
    public struct UhAudioCue
    {
        public UhAudioCueKind Kind;
        /// <summary>Base level 0 to 1 (the caller's volume and Settings.SfxVolume multiply it).</summary>
        public float Volume;
        /// <summary>Seconds that must pass before the same id plays again near the same place.</summary>
        public float MinGap;
        /// <summary>Plays farther apart than this (metres) are separate events. 0 means every play counts as the same place.</summary>
        public float Radius;
        /// <summary>The most separate plays of this id inside one MinGap (at least 1).</summary>
        public int Burst;
        /// <summary>Full-volume radius of the 3D source (metres).</summary>
        public float MinDistance;
        /// <summary>Distance beyond which the cue is inaudible and not played at all (metres).</summary>
        public float MaxDistance;
        /// <summary>Random pitch spread, plus or minus this fraction.</summary>
        public float PitchJitter;

        public UhAudioCue(UhAudioCueKind kind, float volume, float minGap, float radius, int burst, float minDistance, float maxDistance, float pitchJitter)
        {
            Kind = kind;
            Volume = volume;
            MinGap = minGap;
            Radius = radius;
            Burst = burst;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            PitchJitter = pitchJitter;
        }
    }

    /// <summary>Short ambience details scattered around the listener in each underground zone (synthesized, not frozen).</summary>
    public enum UhAudioDetail
    {
        PipeTick,
        RelayClick,
        Drip,
        Murmur,
        ValveClank,
        SteamPuff,
        WaterGurgle,
        CrystalCrackle,
        Chime,
        MetalTick,
        DistantHammer,
        ChainRattle,
        WindGust,
        ResonantTone,
        FurnaceThrob,
        DeepBoom,
        Count
    }

    /// <summary>The two encounter score layers: the skirmish pulse and the boss drive stacked on top of it.</summary>
    public enum UhAudioMusic
    {
        Pulse,
        Drive,
        Count
    }

    /// <summary>One kind of ambience detail in a zone: how often it happens, how loud and how far away.</summary>
    public struct UhAudioDetailSpec
    {
        public UhAudioDetail Detail;
        public float MinGap;
        public float MaxGap;
        public float Volume;
        public float PitchJitter;
        public float MinRange;
        public float MaxRange;

        public UhAudioDetailSpec(UhAudioDetail detail, float minGap, float maxGap, float volume, float pitchJitter, float minRange, float maxRange)
        {
            Detail = detail;
            MinGap = minGap;
            MaxGap = maxGap;
            Volume = volume;
            PitchJitter = pitchJitter;
            MinRange = minRange;
            MaxRange = maxRange;
        }
    }

    /// <summary>
    /// Pure rules of the 04E audio (engine-free, tested offline): the cue table, region and depth to zone mapping, the
    /// zone resolver that decides which ambience plays, the bed levels, how much street sound leaks into each zone, the
    /// detail tables, the loop table and the encounter score targets.
    /// </summary>
    public static class UhAudioRules
    {
        /// <summary>Number of UhZone values (Surface to Crown). UhZone is frozen without a Count member.</summary>
        public const int ZoneCount = 8;
        /// <summary>Seconds for a full crossfade from one zone to another.</summary>
        public const float ZoneFade = 1.5f;
        /// <summary>An underground zone requested while the player is plainly on the street is dropped after this long.</summary>
        public const float StaleZoneGrace = 1.5f;
        /// <summary>Below this height nothing but the Underheart exists (the street is 0, the ground slab ends at -2).</summary>
        public const float DeepY = -5.5f;
        /// <summary>At most this many non-UI 04E one-shots start inside one GlobalWindow.</summary>
        public const int GlobalCap = 10;
        public const float GlobalWindow = 0.1f;
        /// <summary>Encounter score fade in and release (seconds).</summary>
        public const float MusicAttack = 1.2f;
        public const float MusicRelease = 3.5f;

        /// <summary>The family of a cue (from the cue table).</summary>
        public static UhAudioCueKind KindOf(UhSfx id)
        {
            return Cue(id).Kind;
        }

        /// <summary>How a cue plays (level, repeat rule, reach). Every id in UhSfx has an entry.</summary>
        public static UhAudioCue Cue(UhSfx id)
        {
            switch (id)
            {
                // Interface (usually Play2D; still positional-safe).
                case UhSfx.UiAccept: return new UhAudioCue(UhAudioCueKind.Ui, 0.55f, 0.15f, 0f, 1, 2f, 30f, 0f);
                case UhSfx.UiTrack: return new UhAudioCue(UhAudioCueKind.Ui, 0.5f, 0.15f, 0f, 1, 2f, 30f, 0f);
                case UhSfx.UiClaim: return new UhAudioCue(UhAudioCueKind.Ui, 0.6f, 0.3f, 0f, 1, 2f, 30f, 0f);
                case UhSfx.UiDenied: return new UhAudioCue(UhAudioCueKind.Ui, 0.85f, 0.25f, 0f, 1, 2f, 30f, 0f);
                case UhSfx.UiCard: return new UhAudioCue(UhAudioCueKind.Ui, 0.6f, 0.15f, 0f, 1, 2f, 30f, 0f);
                // Rescue, bond and companion commands.
                case UhSfx.RescueClamp: return new UhAudioCue(UhAudioCueKind.Companion, 0.6f, 0.25f, 2f, 2, 2.5f, 45f, 0.04f);
                case UhSfx.ClampBreak: return new UhAudioCue(UhAudioCueKind.Companion, 0.85f, 0.3f, 2f, 3, 3f, 55f, 0.03f);
                case UhSfx.DrakeWarn: return new UhAudioCue(UhAudioCueKind.Companion, 0.65f, 1.2f, 0f, 1, 3f, 45f, 0.03f);
                case UhSfx.BondChime: return new UhAudioCue(UhAudioCueKind.Companion, 0.75f, 2f, 0f, 1, 4f, 60f, 0f);
                case UhSfx.CompanionFollow: return new UhAudioCue(UhAudioCueKind.Companion, 0.55f, 0.3f, 0f, 1, 2.5f, 40f, 0.03f);
                case UhSfx.CompanionWait: return new UhAudioCue(UhAudioCueKind.Companion, 0.55f, 0.3f, 0f, 1, 2.5f, 40f, 0.03f);
                case UhSfx.CompanionRecall: return new UhAudioCue(UhAudioCueKind.Companion, 0.55f, 0.6f, 0f, 1, 3f, 50f, 0.02f);
                case UhSfx.AssistReveal: return new UhAudioCue(UhAudioCueKind.Companion, 0.7f, 1f, 0f, 1, 4f, 60f, 0f);
                case UhSfx.AssistBurst: return new UhAudioCue(UhAudioCueKind.Companion, 1f, 0.25f, 4f, 2, 3f, 55f, 0.04f);
                case UhSfx.CompanionHurt: return new UhAudioCue(UhAudioCueKind.Companion, 0.55f, 0.5f, 0f, 1, 2.5f, 40f, 0.05f);
                case UhSfx.CompanionDowned: return new UhAudioCue(UhAudioCueKind.Companion, 0.7f, 3f, 0f, 1, 3f, 50f, 0f);
                case UhSfx.CareChime: return new UhAudioCue(UhAudioCueKind.Companion, 0.6f, 1f, 0f, 1, 2.5f, 35f, 0f);
                case UhSfx.GuardianBrace: return new UhAudioCue(UhAudioCueKind.Companion, 1f, 0.35f, 3f, 2, 3f, 50f, 0.04f);
                case UhSfx.SkimmerArc: return new UhAudioCue(UhAudioCueKind.Companion, 0.55f, 0.25f, 3f, 2, 3f, 50f, 0.05f);
                // Creature and vehicle fusion.
                case UhSfx.FuseStart: return new UhAudioCue(UhAudioCueKind.Fusion, 0.75f, 0.8f, 0f, 1, 4f, 70f, 0f);
                case UhSfx.FuseLock: return new UhAudioCue(UhAudioCueKind.Fusion, 1f, 0.8f, 0f, 1, 4f, 70f, 0f);
                case UhSfx.SeparateStart: return new UhAudioCue(UhAudioCueKind.Fusion, 0.7f, 0.8f, 0f, 1, 4f, 70f, 0f);
                case UhSfx.SeparateDone: return new UhAudioCue(UhAudioCueKind.Fusion, 0.7f, 0.8f, 0f, 1, 4f, 60f, 0f);
                case UhSfx.FuseDenied: return new UhAudioCue(UhAudioCueKind.Fusion, 1f, 0.4f, 0f, 1, 3f, 40f, 0f);
                case UhSfx.VentGlide: return new UhAudioCue(UhAudioCueKind.Fusion, 0.9f, 0.5f, 6f, 2, 4f, 70f, 0.03f);
                case UhSfx.VentBurst: return new UhAudioCue(UhAudioCueKind.Fusion, 0.95f, 0.3f, 6f, 2, 5f, 80f, 0.03f);
                case UhSfx.BraceRam: return new UhAudioCue(UhAudioCueKind.Fusion, 1f, 0.3f, 6f, 2, 6f, 90f, 0.03f);
                case UhSfx.ArcDash: return new UhAudioCue(UhAudioCueKind.Fusion, 0.6f, 0.3f, 6f, 2, 5f, 80f, 0.04f);
                // World mechanisms and hazards.
                case UhSfx.ShortcutOpen: return new UhAudioCue(UhAudioCueKind.World, 1f, 1.5f, 4f, 2, 6f, 90f, 0f);
                case UhSfx.LiftClank: return new UhAudioCue(UhAudioCueKind.World, 0.75f, 0.5f, 4f, 2, 4f, 70f, 0.03f);
                case UhSfx.ValveTurn: return new UhAudioCue(UhAudioCueKind.World, 0.7f, 0.6f, 3f, 2, 3f, 60f, 0.04f);
                case UhSfx.SteamBurst: return new UhAudioCue(UhAudioCueKind.World, 0.75f, 0.4f, 6f, 3, 4f, 70f, 0.05f);
                case UhSfx.LavaWarn: return new UhAudioCue(UhAudioCueKind.World, 0.9f, 1f, 4f, 2, 4f, 50f, 0f);
                case UhSfx.HeatTick: return new UhAudioCue(UhAudioCueKind.World, 0.6f, 0.35f, 3f, 1, 2f, 25f, 0.06f);
                case UhSfx.RestChime: return new UhAudioCue(UhAudioCueKind.World, 0.65f, 2f, 0f, 1, 3f, 40f, 0f);
                case UhSfx.GateOpen: return new UhAudioCue(UhAudioCueKind.World, 1f, 1f, 4f, 2, 5f, 80f, 0.02f);
                case UhSfx.ReceiverPing: return new UhAudioCue(UhAudioCueKind.World, 0.6f, 0.3f, 2f, 2, 3f, 45f, 0f);
                case UhSfx.ConduitPulse: return new UhAudioCue(UhAudioCueKind.World, 0.8f, 0.5f, 8f, 3, 4f, 60f, 0.03f);
                // Bosses.
                case UhSfx.BossTelegraph: return new UhAudioCue(UhAudioCueKind.Boss, 1f, 0.35f, 10f, 2, 12f, 150f, 0f);
                case UhSfx.BossPhase: return new UhAudioCue(UhAudioCueKind.Boss, 0.95f, 2f, 0f, 1, 15f, 180f, 0f);
                case UhSfx.BossVulnerable: return new UhAudioCue(UhAudioCueKind.Boss, 0.85f, 0.5f, 8f, 2, 10f, 140f, 0f);
                case UhSfx.BossDefeat: return new UhAudioCue(UhAudioCueKind.Boss, 1f, 5f, 0f, 1, 15f, 200f, 0f);
                case UhSfx.FurnaceRoar: return new UhAudioCue(UhAudioCueKind.Boss, 0.9f, 0.8f, 10f, 2, 12f, 150f, 0.03f);
                case UhSfx.BeamCharge: return new UhAudioCue(UhAudioCueKind.Boss, 0.8f, 0.4f, 8f, 3, 8f, 120f, 0.02f);
                case UhSfx.MachineStep: return new UhAudioCue(UhAudioCueKind.Boss, 0.9f, 0.14f, 3f, 2, 6f, 90f, 0.06f);
                case UhSfx.LureHum: return new UhAudioCue(UhAudioCueKind.Boss, 0.5f, 0.6f, 5f, 3, 4f, 60f, 0.02f);
                case UhSfx.PlateCrack: return new UhAudioCue(UhAudioCueKind.Boss, 0.9f, 0.12f, 1.5f, 3, 4f, 70f, 0.06f);
                // Underground creatures.
                case UhSfx.ScavengerChitter: return new UhAudioCue(UhAudioCueKind.Creature, 0.9f, 0.25f, 4f, 3, 3f, 55f, 0.08f);
                case UhSfx.BurrowerEmerge: return new UhAudioCue(UhAudioCueKind.Creature, 1f, 0.6f, 4f, 2, 5f, 80f, 0.05f);
                case UhSfx.GlasswingScreech: return new UhAudioCue(UhAudioCueKind.Creature, 0.6f, 0.5f, 5f, 2, 5f, 90f, 0.06f);
                case UhSfx.CasterCharge: return new UhAudioCue(UhAudioCueKind.Creature, 1f, 0.5f, 4f, 2, 4f, 70f, 0.04f);
                case UhSfx.SentinelScan: return new UhAudioCue(UhAudioCueKind.Creature, 0.5f, 0.8f, 4f, 2, 4f, 60f, 0.02f);
                case UhSfx.RepairPulse: return new UhAudioCue(UhAudioCueKind.Creature, 0.5f, 0.6f, 4f, 2, 3f, 55f, 0.04f);
                case UhSfx.CrabWarn: return new UhAudioCue(UhAudioCueKind.Creature, 1f, 0.6f, 3f, 2, 2.5f, 40f, 0.08f);
                case UhSfx.MothFlutter: return new UhAudioCue(UhAudioCueKind.Creature, 0.5f, 0.8f, 3f, 2, 2f, 25f, 0.1f);
                // Appended: the companion cannot do what was asked (target neutral, out of reach or hidden, ability cooling).
                case UhSfx.CompanionRefuse: return new UhAudioCue(UhAudioCueKind.Companion, 0.6f, 0.4f, 0f, 1, 2.5f, 40f, 0.02f);
                default: return new UhAudioCue(UhAudioCueKind.World, 0.5f, 0.3f, 0f, 1, 3f, 50f, 0f);
            }
        }

        /// <summary>The ambience zone of an Underheart region id ("" or unknown is the surface).</summary>
        public static UhZone ZoneForRegion(string regionId)
        {
            switch (regionId)
            {
                case UnderheartIds.ServiceLevel: return UhZone.ServiceInterior;
                case UnderheartIds.LanternRefuge: return UhZone.Refuge;
                case UnderheartIds.EmberCanals: return UhZone.Canals;
                case UnderheartIds.PyreGardens: return UhZone.Gardens;
                case UnderheartIds.CrucibleWorkshops: return UhZone.Workshops;
                case UnderheartIds.AshReliquary: return UhZone.Reliquary;
                case UnderheartIds.FoundryCrown: return UhZone.Crown;
                default: return UhZone.Surface;
            }
        }

        /// <summary>
        /// A fallback zone from depth alone (used only when the player is underground but no region was reported), from the
        /// planned floor depths: service level about -6, refuge -18, canals -26, gardens -40, workshops -55, reliquary -72,
        /// crown -95.
        /// </summary>
        public static UhZone ZoneForDepth(float y)
        {
            if (y > -12f) return UhZone.ServiceInterior;
            if (y > -22f) return UhZone.Refuge;
            if (y > -33f) return UhZone.Canals;
            if (y > -47f) return UhZone.Gardens;
            if (y > -63f) return UhZone.Workshops;
            if (y > -83f) return UhZone.Reliquary;
            return UhZone.Crown;
        }

        /// <summary>True for the six named underground regions (not the surface and not the muffled service interior).</summary>
        public static bool IsUnderground(UhZone z)
        {
            return z >= UhZone.Refuge && z <= UhZone.Crown;
        }

        /// <summary>How much of the street ambience (city, wind, rain, thunder) is heard in a zone.</summary>
        public static float SurfaceLeak(UhZone z)
        {
            switch (z)
            {
                case UhZone.Surface: return 1f;
                case UhZone.ServiceInterior: return 0.2f;
                default: return 0f;
            }
        }

        /// <summary>Level of a zone's ambience bed at full weight (Settings.SfxVolume multiplies it). The surface has no bed.</summary>
        public static float BedLevel(UhZone z)
        {
            switch (z)
            {
                case UhZone.ServiceInterior: return 0.36f;
                case UhZone.Refuge: return 0.42f;
                case UhZone.Canals: return 0.5f;
                case UhZone.Gardens: return 0.44f;
                case UhZone.Workshops: return 0.44f;
                case UhZone.Reliquary: return 0.46f;
                case UhZone.Crown: return 0.6f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Which zone the ambience should play. requested is the last SetZone call (requestedAt when it changed),
        /// playerRegion the Underheart region the player stands in ("" on the surface, regionChangedAt when it last
        /// changed), deep is true when the player is plainly underground (inside a cell or below DeepY), interior true
        /// while the player is inside a walk-in venue. Rules: outside a session it is always the surface; inside an
        /// Underheart region the newer of the explicit request and the region wins, except that a Surface request never
        /// applies underground; on the street a ServiceInterior request is kept while the player is inside a venue, and
        /// any non-surface request there is dropped StaleZoneGrace after it was made once the player is outside;
        /// underground with no region reported, the request applies, else a zone picked by depth.
        /// </summary>
        public static UhZone Resolve(UhZone requested, float requestedAt, string playerRegion, float regionChangedAt, bool deep, bool interior, float playerY, bool sessionActive, float now)
        {
            if (!sessionActive) return UhZone.Surface;
            int rq = (int)requested;
            if (rq < 0 || rq >= ZoneCount) requested = UhZone.Surface;
            UhZone auto = ZoneForRegion(playerRegion);
            if (auto != UhZone.Surface)
            {
                if (requested == UhZone.Surface) return auto;
                return requestedAt >= regionChangedAt ? requested : auto;
            }
            if (deep)
            {
                if (requested != UhZone.Surface) return requested;
                return ZoneForDepth(playerY);
            }
            if (requested == UhZone.Surface) return UhZone.Surface;
            if (requested == UhZone.ServiceInterior && interior) return requested;
            // A door threshold: honour the request briefly, then drop it if the player is plainly outside.
            return now - requestedAt < StaleZoneGrace ? requested : UhZone.Surface;
        }

        /// <summary>The details scattered around the listener in a zone (empty for the surface).</summary>
        public static UhAudioDetailSpec[] Details(UhZone z)
        {
            switch (z)
            {
                case UhZone.ServiceInterior: return ServiceDetails;
                case UhZone.Refuge: return RefugeDetails;
                case UhZone.Canals: return CanalDetails;
                case UhZone.Gardens: return GardenDetails;
                case UhZone.Workshops: return WorkshopDetails;
                case UhZone.Reliquary: return ReliquaryDetails;
                case UhZone.Crown: return CrownDetails;
                default: return NoDetails;
            }
        }

        private static readonly UhAudioDetailSpec[] NoDetails = new UhAudioDetailSpec[0];

        private static readonly UhAudioDetailSpec[] ServiceDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.PipeTick, 4f, 11f, 0.3f, 0.12f, 4f, 10f),
            new UhAudioDetailSpec(UhAudioDetail.RelayClick, 9f, 22f, 0.25f, 0.05f, 5f, 12f),
        };

        private static readonly UhAudioDetailSpec[] RefugeDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.Drip, 2.5f, 7f, 0.35f, 0.15f, 4f, 12f),
            new UhAudioDetailSpec(UhAudioDetail.Murmur, 10f, 24f, 0.28f, 0.08f, 7f, 15f),
        };

        private static readonly UhAudioDetailSpec[] CanalDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.ValveClank, 7f, 16f, 0.4f, 0.08f, 6f, 16f),
            new UhAudioDetailSpec(UhAudioDetail.SteamPuff, 4f, 10f, 0.35f, 0.1f, 5f, 14f),
            new UhAudioDetailSpec(UhAudioDetail.WaterGurgle, 5f, 12f, 0.32f, 0.12f, 4f, 12f),
            new UhAudioDetailSpec(UhAudioDetail.Drip, 4f, 10f, 0.25f, 0.15f, 4f, 10f),
        };

        private static readonly UhAudioDetailSpec[] GardenDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.CrystalCrackle, 3f, 8f, 0.32f, 0.12f, 4f, 12f),
            new UhAudioDetailSpec(UhAudioDetail.Chime, 6f, 14f, 0.3f, 0.06f, 6f, 16f),
        };

        private static readonly UhAudioDetailSpec[] WorkshopDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.MetalTick, 3f, 8f, 0.3f, 0.15f, 4f, 12f),
            new UhAudioDetailSpec(UhAudioDetail.DistantHammer, 9f, 20f, 0.4f, 0.06f, 12f, 22f),
            new UhAudioDetailSpec(UhAudioDetail.ChainRattle, 12f, 26f, 0.3f, 0.08f, 6f, 16f),
        };

        private static readonly UhAudioDetailSpec[] ReliquaryDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.WindGust, 7f, 16f, 0.38f, 0.1f, 6f, 16f),
            new UhAudioDetailSpec(UhAudioDetail.ResonantTone, 10f, 22f, 0.3f, 0.04f, 8f, 18f),
        };

        private static readonly UhAudioDetailSpec[] CrownDetails =
        {
            new UhAudioDetailSpec(UhAudioDetail.FurnaceThrob, 6f, 14f, 0.42f, 0.06f, 8f, 18f),
            new UhAudioDetailSpec(UhAudioDetail.DeepBoom, 12f, 28f, 0.45f, 0.08f, 14f, 24f),
        };

        /// <summary>Base level and 3D reach of a positional loop.</summary>
        public static void LoopSpec(UhLoop loop, out float volume, out float minDistance, out float maxDistance)
        {
            switch (loop)
            {
                case UhLoop.FormHum: volume = 0.45f; minDistance = 3f; maxDistance = 45f; return;
                case UhLoop.Furnace: volume = 0.7f; minDistance = 6f; maxDistance = 70f; return;
                case UhLoop.Steam: volume = 0.5f; minDistance = 3f; maxDistance = 35f; return;
                case UhLoop.Lava: volume = 0.55f; minDistance = 4f; maxDistance = 40f; return;
                case UhLoop.Receiver: volume = 0.45f; minDistance = 3f; maxDistance = 40f; return;
                case UhLoop.BeamLane: volume = 0.6f; minDistance = 4f; maxDistance = 60f; return;
                case UhLoop.LiftMotor: volume = 0.55f; minDistance = 3f; maxDistance = 45f; return;
                default: volume = 0.5f; minDistance = 3f; maxDistance = 40f; return;
            }
        }

        /// <summary>Approximate level of a logarithmic-rolloff source at a distance (1 inside minDistance, 0 beyond maxDistance).</summary>
        public static float Attenuation(float distance, float minDistance, float maxDistance)
        {
            if (distance >= maxDistance) return 0f;
            if (distance <= minDistance) return 1f;
            return minDistance / Mathf.Max(minDistance, distance);
        }

        /// <summary>
        /// The encounter score targets: the pulse plays during an Underheart skirmish or boss fight, the drive layer only in
        /// a boss fight. Never on the surface (no change to street audio).
        /// </summary>
        public static float MusicTarget(bool bossFight, bool skirmish, UhZone zone, out float drive)
        {
            drive = 0f;
            if (zone == UhZone.Surface) return 0f;
            if (bossFight)
            {
                drive = 1f;
                return 1f;
            }
            return skirmish ? 0.8f : 0f;
        }
    }

    /// <summary>
    /// The per-id rate limit for 04E one-shots (engine-free; the caller passes real time). A play is dropped when the same
    /// id was accepted less than MinGap ago within Radius metres (or anywhere, for 2D plays and Radius 0), or when Burst
    /// plays of the id are already inside the gap. Non-UI plays also share a global cap per GlobalWindow. So the same event
    /// never stacks once per collider hit, while separate events far apart still sound.
    /// </summary>
    public sealed class UhAudioRateLimiter
    {
        private const int Ring = 4;
        private readonly float[] _time;
        private readonly Vector3[] _pos;
        private readonly bool[] _positional;
        private readonly int[] _head;
        private readonly float[] _global = new float[UhAudioRules.GlobalCap];
        private int _globalHead;
        private readonly int _ids;

        public UhAudioRateLimiter(int ids)
        {
            _ids = Mathf.Max(1, ids);
            _time = new float[_ids * Ring];
            _pos = new Vector3[_ids * Ring];
            _positional = new bool[_ids * Ring];
            _head = new int[_ids];
            Reset();
        }

        /// <summary>Forgets every earlier play.</summary>
        public void Reset()
        {
            for (int i = 0; i < _time.Length; i++) _time[i] = -1e9f;
            for (int i = 0; i < _head.Length; i++) _head[i] = 0;
            for (int i = 0; i < _global.Length; i++) _global[i] = -1e9f;
            _globalHead = 0;
        }

        /// <summary>True when the play may start (and records it); false when it would stack on an earlier one.</summary>
        public bool TryAccept(int id, float now, Vector3 pos, bool positional, UhAudioCue cue)
        {
            if (id < 0 || id >= _ids) return false;
            float gap = Mathf.Max(0f, cue.MinGap);
            float r2 = cue.Radius * cue.Radius;
            int burst = Mathf.Clamp(cue.Burst, 1, Ring);
            int baseIndex = id * Ring;
            int recent = 0;
            for (int k = 0; k < Ring; k++)
            {
                int i = baseIndex + k;
                float dt = now - _time[i];
                // A time from the future (a reset clock) counts as long ago.
                if (dt < 0f || dt >= gap) continue;
                recent++;
                bool samePlace = !positional || !_positional[i] || cue.Radius <= 0f || (pos - _pos[i]).sqrMagnitude < r2;
                if (samePlace) return false;
            }
            if (recent >= burst) return false;
            bool countGlobal = cue.Kind != UhAudioCueKind.Ui;
            if (countGlobal)
            {
                float oldest = _global[_globalHead];
                float gdt = now - oldest;
                if (gdt >= 0f && gdt < UhAudioRules.GlobalWindow) return false;
                _global[_globalHead] = now;
                _globalHead = (_globalHead + 1) % _global.Length;
            }
            int slot = baseIndex + _head[id];
            _time[slot] = now;
            _pos[slot] = pos;
            _positional[slot] = positional;
            _head[id] = (_head[id] + 1) % Ring;
            return true;
        }
    }

    /// <summary>
    /// The ambience zone weights (engine-free). The eight weights always sum to 1: stepping toward a zone raises its weight
    /// linearly over the fade time and scales the others down in proportion, so a change of mind halfway through blends on
    /// from where it was. Gains are equal-power (the square root of the weight), which keeps two uncorrelated beds at a
    /// steady loudness through the crossfade.
    /// </summary>
    public sealed class UhAudioZoneBlend
    {
        private readonly float[] _w = new float[UhAudioRules.ZoneCount];

        public UhAudioZoneBlend()
        {
            Snap(UhZone.Surface);
        }

        /// <summary>The zone the weights are moving toward.</summary>
        public UhZone Target { get; private set; }

        /// <summary>True when the target zone has the whole weight.</summary>
        public bool Settled { get { return _w[(int)Target] >= 1f; } }

        public float Weight(UhZone z)
        {
            int i = (int)z;
            return i >= 0 && i < _w.Length ? _w[i] : 0f;
        }

        /// <summary>Sum of every weight (1 within float precision).</summary>
        public float Sum
        {
            get
            {
                float s = 0f;
                for (int i = 0; i < _w.Length; i++) s += _w[i];
                return s;
            }
        }

        /// <summary>Equal-power gain of a zone's bed.</summary>
        public float Gain(UhZone z)
        {
            return Mathf.Sqrt(Mathf.Clamp01(Weight(z)));
        }

        /// <summary>How loud the street ambience should be: exactly 1 when fully on the surface.</summary>
        public float SurfaceGain
        {
            get
            {
                if (_w[0] >= 1f) return 1f;
                float g = 0f;
                for (int i = 0; i < _w.Length; i++) g += _w[i] * UhAudioRules.SurfaceLeak((UhZone)i);
                return Mathf.Clamp01(g);
            }
        }

        /// <summary>The zone with the largest weight.</summary>
        public UhZone Dominant
        {
            get
            {
                int best = 0;
                for (int i = 1; i < _w.Length; i++) if (_w[i] > _w[best]) best = i;
                return (UhZone)best;
            }
        }

        /// <summary>Moves instantly to a zone (session start, load, title).</summary>
        public void Snap(UhZone z)
        {
            int t = (int)z;
            if (t < 0 || t >= _w.Length) t = 0;
            for (int i = 0; i < _w.Length; i++) _w[i] = 0f;
            _w[t] = 1f;
            Target = (UhZone)t;
        }

        /// <summary>Advances the blend toward target by dt seconds of a fade lasting fadeSeconds.</summary>
        public void Step(UhZone target, float dt, float fadeSeconds)
        {
            int t = (int)target;
            if (t < 0 || t >= _w.Length) return;
            Target = target;
            if (_w[t] >= 1f) return;
            if (fadeSeconds <= 0f)
            {
                Snap(target);
                return;
            }
            if (dt <= 0f || float.IsNaN(dt)) return;
            float wt = Mathf.Min(1f, _w[t] + dt / fadeSeconds);
            float others = 0f;
            for (int i = 0; i < _w.Length; i++) if (i != t) others += _w[i];
            float remain = 1f - wt;
            if (others <= 1e-6f || remain <= 1e-5f)
            {
                Snap(target);
                return;
            }
            float k = remain / others;
            for (int i = 0; i < _w.Length; i++)
            {
                if (i == t) continue;
                _w[i] *= k;
                if (_w[i] < 1e-4f) _w[i] = 0f;
            }
            _w[t] = wt;
            float s = Sum;
            if (s <= 1e-6f)
            {
                Snap(target);
                return;
            }
            for (int i = 0; i < _w.Length; i++) _w[i] /= s;
        }
    }

    /// <summary>
    /// Schedules the ambience details of the current zone (engine-free, seeded): each kind waits a random time between
    /// its MinGap and MaxGap, a zone change (or a gap of more than 2 s between calls) re-arms every kind with a random
    /// lead so they never all fire at once, and at most one detail is returned per call.
    /// </summary>
    public sealed class UhAudioDetailScheduler
    {
        private readonly float[] _next = new float[8];
        private uint _state;
        private UhZone _zone = UhZone.Surface;
        private bool _armed;
        private float _last = -1e9f;

        public UhAudioDetailScheduler(int seed)
        {
            _state = (uint)seed * 2654435761u + 0x9E3779B9u;
            if (_state == 0) _state = 1u;
        }

        /// <summary>Forgets the schedule (the next Tick re-arms).</summary>
        public void Reset()
        {
            _armed = false;
            _zone = UhZone.Surface;
        }

        /// <summary>A uniform value in [0,1) from the scheduler's own sequence (also used for placement).</summary>
        public float Next01()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Returns true (with the detail and a random placement) when a detail of the zone is due at now.</summary>
        public bool Tick(UhZone zone, float now, out UhAudioDetailSpec spec, out float azimuth01, out float range01, out float pitch01)
        {
            spec = default(UhAudioDetailSpec);
            azimuth01 = 0f;
            range01 = 0f;
            pitch01 = 0.5f;
            UhAudioDetailSpec[] list = UhAudioRules.Details(zone);
            if (list.Length == 0)
            {
                _zone = zone;
                _armed = false;
                return false;
            }
            // Re-arm on a zone change, a clock reset, or after a pause in ticking (so stale times never fire in a burst).
            if (!_armed || zone != _zone || now < _last - 1f || now - _last > 2f)
            {
                _zone = zone;
                _armed = true;
                for (int j = 0; j < list.Length && j < _next.Length; j++)
                    _next[j] = now + Mathf.Lerp(list[j].MinGap * 0.3f, list[j].MaxGap, Next01());
            }
            _last = now;
            for (int j = 0; j < list.Length && j < _next.Length; j++)
            {
                if (now < _next[j]) continue;
                _next[j] = now + Mathf.Lerp(list[j].MinGap, list[j].MaxGap, Next01());
                spec = list[j];
                azimuth01 = Next01();
                range01 = Next01();
                pitch01 = Next01();
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The index space of the 04E sound bank (one-shots, loops, the seven beds, the details and the two score layers), the
    /// build order (the cues the first route needs first) and the dispatch to the pure recipes, so the tests can check
    /// every clip the game can ever play.
    /// </summary>
    public static class UhAudioBank
    {
        public const int SfxCount = (int)UhSfx.Count;
        public const int LoopCount = (int)UhLoop.Count;
        public const int BedCount = UhAudioRules.ZoneCount - 1;
        public const int DetailCount = (int)UhAudioDetail.Count;
        public const int MusicCount = (int)UhAudioMusic.Count;
        public const int LoopBase = SfxCount;
        public const int BedBase = LoopBase + LoopCount;
        public const int DetailBase = BedBase + BedCount;
        public const int MusicBase = DetailBase + DetailCount;
        public const int Total = MusicBase + MusicCount;

        public static int Sfx(UhSfx id) { return (int)id; }
        public static int Loop(UhLoop id) { return LoopBase + (int)id; }
        /// <summary>The bed of a zone (-1 for the surface, which has none).</summary>
        public static int Bed(UhZone z) { return z == UhZone.Surface ? -1 : BedBase + (int)z - 1; }
        public static int Detail(UhAudioDetail d) { return DetailBase + (int)d; }
        public static int Music(UhAudioMusic m) { return MusicBase + (int)m; }

        /// <summary>True for clips that play looped (loops, beds and score layers): they must wrap without a click.</summary>
        public static bool IsLooping(int index)
        {
            return (index >= LoopBase && index < DetailBase) || (index >= MusicBase && index < Total);
        }

        public static bool IsBed(int index) { return index >= BedBase && index < DetailBase; }
        public static bool IsMusic(int index) { return index >= MusicBase && index < Total; }

        /// <summary>The peak every clip of this index is normalized to (nothing ever reaches full scale).</summary>
        public static float Peak(int index)
        {
            if (index < LoopBase) return Underheart04ESounds.OneShotPeak;
            if (index < BedBase) return Underheart04ESounds.LoopPeak;
            if (index < DetailBase) return Underheart04ESounds.BedPeak;
            if (index < MusicBase) return Underheart04ESounds.DetailPeak;
            return Underheart04ESounds.MusicPeak;
        }

        public static string Name(int index)
        {
            if (index < 0 || index >= Total) return "invalid";
            if (index < LoopBase) return ((UhSfx)index).ToString();
            if (index < BedBase) return "Loop " + (UhLoop)(index - LoopBase);
            if (index < DetailBase) return "Bed " + (UhZone)(index - BedBase + 1);
            if (index < MusicBase) return "Detail " + (UhAudioDetail)(index - DetailBase);
            return "Score " + (UhAudioMusic)(index - MusicBase);
        }

        /// <summary>Synthesizes one clip of the bank (pure maths, safe on a worker thread).</summary>
        public static float[] Make(int index)
        {
            if (index < 0 || index >= Total) return AudioSynth.Buf(0.05f);
            if (index < LoopBase) return Underheart04ESounds.Make((UhSfx)index);
            if (index < BedBase) return Underheart04ESounds.MakeLoop((UhLoop)(index - LoopBase));
            if (index < DetailBase) return Underheart04ESounds.MakeBed((UhZone)(index - BedBase + 1));
            if (index < MusicBase) return Underheart04ESounds.MakeDetail((UhAudioDetail)(index - DetailBase));
            return Underheart04ESounds.MakeMusic((UhAudioMusic)(index - MusicBase));
        }

        /// <summary>
        /// Every index once, in the order the bank is built: interface and companion cues first (the first minute of the
        /// route needs them), then world, fusion, boss and creature cues, the loops, the beds, the details and the score.
        /// </summary>
        public static int[] BuildOrder()
        {
            int[] order = new int[Total];
            int n = 0;
            UhAudioCueKind[] kinds = { UhAudioCueKind.Ui, UhAudioCueKind.Companion, UhAudioCueKind.World, UhAudioCueKind.Fusion, UhAudioCueKind.Boss, UhAudioCueKind.Creature };
            for (int k = 0; k < kinds.Length; k++)
                for (int i = 0; i < SfxCount; i++)
                    if (UhAudioRules.KindOf((UhSfx)i) == kinds[k]) order[n++] = i;
            for (int i = LoopBase; i < Total; i++) order[n++] = i;
            return order;
        }
    }
}
