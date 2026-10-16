using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One-shot cues of Underheart and Bonds 04E (synthesized; append only).</summary>
    public enum UhSfx
    {
        UiAccept, UiTrack, UiClaim, UiDenied, UiCard,
        RescueClamp, ClampBreak, DrakeWarn, BondChime, CompanionFollow, CompanionWait, CompanionRecall, AssistReveal,
        AssistBurst, CompanionHurt, CompanionDowned, CareChime, GuardianBrace, SkimmerArc,
        FuseStart, FuseLock, SeparateStart, SeparateDone, FuseDenied, VentGlide, VentBurst, BraceRam, ArcDash,
        ShortcutOpen, LiftClank, ValveTurn, SteamBurst, LavaWarn, HeatTick, RestChime, GateOpen, ReceiverPing, ConduitPulse,
        BossTelegraph, BossPhase, BossVulnerable, BossDefeat, FurnaceRoar, BeamCharge, MachineStep, LureHum, PlateCrack,
        ScavengerChitter, BurrowerEmerge, GlasswingScreech, CasterCharge, SentinelScan, RepairPulse, CrabWarn, MothFlutter,
        Count
    }

    /// <summary>Looping sources (positional, keyed by the int StartLoop returns).</summary>
    public enum UhLoop
    {
        FormHum, Furnace, Steam, Lava, Receiver, BeamLane, LiftMotor, Count
    }

    /// <summary>Ambience zones the audio blends between (surface life, muffled interiors, the six underground moods).</summary>
    public enum UhZone
    {
        Surface, ServiceInterior, Refuge, Canals, Gardens, Workshops, Reliquary, Crown
    }

    /// <summary>
    /// Synthesized audio for 04E: one-shots, loops and the underground ambience zones. Safe to call before the clips
    /// exist (calls do nothing). FOUNDATION STUB: the AUDIO work stream replaces the bodies and keeps every signature.
    /// </summary>
    public static class Underheart04ESounds
    {
        public static void Init() { }
        public static void Play(UhSfx id, Vector3 pos, float volume = 1f, float pitch = 1f) { }
        public static void Play2D(UhSfx id, float volume = 1f, float pitch = 1f) { }
        /// <summary>Starts a loop and returns its key (0 when nothing started).</summary>
        public static int StartLoop(UhLoop loop, Vector3 pos, float volume = 1f) { return 0; }
        public static void UpdateLoop(int key, Vector3 pos, float volume, float pitch) { }
        public static void StopLoop(int key) { }
        public static void StopAll() { }
        /// <summary>Blends the ambience toward a zone over a second or two (called by the Underheart world and venues).</summary>
        public static void SetZone(UhZone zone) { }
        public static UhZone Zone { get { return UhZone.Surface; } }
        public static void ResetStatics() { }
    }
}
