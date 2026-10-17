using System;
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
        // Appended by the AUDIO stream (spec 17: companion cues identify an unavailable command).
        CompanionRefuse,
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
    /// Synthesized audio for Underheart and Bonds 04E, with no audio files: 56 one-shot cues, 7 positional loops, an
    /// ambience bed and scattered details for each of the seven non-surface zones, and a two-layer encounter score.
    /// Everyone calls these; nobody synthesizes their own 04E clips.
    /// <para>One-shots play through AudioManager's shared pools (3D pool for Play, the pause-proof interface pool for
    /// Play2D) after a per-id rate limit, so one event never stacks once per collider hit. Loops are keyed: StartLoop
    /// returns a key (0 when nothing started), UpdateLoop moves it and sets its level and pitch, StopLoop fades it out
    /// without a click; many loops may exist and the eight most audible get real voices. SetZone requests an ambience
    /// zone; the zone the player is really in (the Underheart region, or depth) is checked every frame, the beds
    /// crossfade over about 1.5 s and the street ambience in AudioManager fades by SurfaceGain, which is exactly 1 on the
    /// surface so street audio never changes there. Every call is safe before the clips exist: the bank is synthesized
    /// on a worker thread from Init (boot) and uploaded a few clips per frame, and a one-shot needed earlier is made on
    /// the spot. Volumes follow Settings (SfxVolume for cues, loops and ambience, MusicVolume for the score).</para>
    /// </summary>
    public static partial class Underheart04ESounds
    {
        /// <summary>The ModuleStatus row of this module (the STATUS page).</summary>
        public const string ModuleId = "sounds_04e";
        private const string ModuleTitle = "Underheart sounds";

        /// <summary>Creates the sources and starts synthesizing the bank in the background (boot; also happens on first use).</summary>
        public static void Init()
        {
            EnsureReady();
        }

        /// <summary>Plays a 3D one-shot at a point. volume 0 to 1 scales the cue's own level; pitch 1 is normal.</summary>
        public static void Play(UhSfx id, Vector3 pos, float volume = 1f, float pitch = 1f)
        {
            try { PlayInternal(id, pos, true, volume, pitch); }
            catch (Exception e) { Log.Exception("Underheart sound " + id, e); }
        }

        /// <summary>Plays a one-shot without position (interface, the companion's voice in the player's ear). Plays while paused.</summary>
        public static void Play2D(UhSfx id, float volume = 1f, float pitch = 1f)
        {
            try { PlayInternal(id, Vector3.zero, false, volume, pitch); }
            catch (Exception e) { Log.Exception("Underheart sound " + id, e); }
        }

        /// <summary>Starts a loop and returns its key (0 when nothing started).</summary>
        public static int StartLoop(UhLoop loop, Vector3 pos, float volume = 1f)
        {
            try { return StartLoopInternal(loop, pos, null, volume); }
            catch (Exception e)
            {
                Log.Exception("Underheart loop " + loop, e);
                return 0;
            }
        }

        /// <summary>
        /// Starts a loop that follows a transform and stops by itself when the transform is destroyed (a fused bike, a
        /// boss). Returns its key (0 when nothing started). UpdateLoop with a position detaches it.
        /// </summary>
        public static int StartLoop(UhLoop loop, Transform follow, float volume = 1f)
        {
            if (follow == null) return 0;
            try { return StartLoopInternal(loop, follow.position, follow, volume); }
            catch (Exception e)
            {
                Log.Exception("Underheart loop " + loop, e);
                return 0;
            }
        }

        /// <summary>Moves a loop and sets its level (0 to 1 of the loop's own level) and pitch (glides, no zipper noise).</summary>
        public static void UpdateLoop(int key, Vector3 pos, float volume, float pitch)
        {
            try { UpdateLoopInternal(key, pos, volume, pitch); }
            catch (Exception e) { Log.Exception("Underheart loop update", e); }
        }

        /// <summary>Fades a loop out (about 80 ms, no click). The key is invalid afterwards; unknown keys are ignored.</summary>
        public static void StopLoop(int key)
        {
            try { StopLoopInternal(key, LoopStopFade); }
            catch (Exception e) { Log.Exception("Underheart loop stop", e); }
        }

        /// <summary>Stops every 04E loop and the encounter score (title, load, death, a system shutting down). The zone ambience stays.</summary>
        public static void StopAll()
        {
            try { StopAllInternal(); }
            catch (Exception e) { Log.Exception("Underheart sounds stop", e); }
        }

        /// <summary>
        /// Blends the ambience toward a zone over about 1.5 s (called by the Underheart world and venues). Repeating the
        /// same zone is a no-op. A Surface request never applies while the player is inside an Underheart region; on the
        /// street ServiceInterior holds while the player is inside a walk-in venue, and any other request there (or a
        /// ServiceInterior left behind after walking out) is dropped 1.5 s after it was made.
        /// </summary>
        public static void SetZone(UhZone zone)
        {
            int z = (int)zone;
            if (z < 0 || z >= UhAudioRules.ZoneCount || zone == _requested) return;
            _requested = zone;
            _requestedAt = Now();
        }

        /// <summary>SetZone for an Underheart region id (UnderheartIds; "" is the surface).</summary>
        public static void SetZoneForRegion(string regionId)
        {
            SetZone(UhAudioRules.ZoneForRegion(regionId));
        }

        /// <summary>The zone the ambience is playing (or blending toward) after the checks above.</summary>
        public static UhZone Zone { get { return _zone; } }

        /// <summary>The last zone a caller requested with SetZone.</summary>
        public static UhZone RequestedZone { get { return _requested; } }

        /// <summary>Street ambience level (city, wind, rain, thunder): exactly 1 on the surface, lower in interiors, 0 underground.</summary>
        public static float SurfaceGain { get { return _blend != null ? _blend.SurfaceGain : 1f; } }

        /// <summary>Current crossfade weight of a zone (the eight weights sum to 1).</summary>
        public static float ZoneWeight(UhZone zone)
        {
            if (_blend != null) return _blend.Weight(zone);
            return zone == UhZone.Surface ? 1f : 0f;
        }

        /// <summary>True once the sources exist (clips still being built are made on demand or start when ready).</summary>
        public static bool Ready { get { return _bank != null && _root != null && !_failed; } }

        /// <summary>True once every clip of the bank has been synthesized and uploaded (or failed and been reported).</summary>
        public static bool BankComplete { get { return _bank != null && _bank.Cursor >= _bank.Order.Length; } }

        /// <summary>Loops that are alive (keyed), audible or not.</summary>
        public static int LiveLoops { get { return _loops.Count; } }

        /// <summary>True while a loop key is alive.</summary>
        public static bool LoopAlive(int key)
        {
            return key != 0 && _loopsByKey.ContainsKey(key);
        }

        /// <summary>One-shots played and dropped by the rate limit this session (debug page and tests in a native session).</summary>
        public static int PlayedCount { get { return _played; } }
        public static int LimitedCount { get { return _limited; } }

        public static void ResetStatics()
        {
            if (_bank != null) _bank.Cancelled = true;
            _bank = null;
            _root = null;
            _failed = false;
            _pageRegistered = false;
            _voices = null;
            _beds = null;
            _bedLevel = null;
            _details = null;
            _detailNext = 0;
            _music = null;
            _musicLevel = 0f;
            _driveLevel = 0f;
            _musicPlaying = false;
            _loops.Clear();
            _loopsByKey.Clear();
            _nextKey = 1;
            _loopRefused = 0;
            _limiter.Reset();
            _blend = new UhAudioZoneBlend();
            _scheduler = new UhAudioDetailScheduler(52501);
            _requested = UhZone.Surface;
            _requestedAt = -1e9f;
            _zone = UhZone.Surface;
            _lastRegion = "";
            _regionChangedAt = -1e9f;
            _lastState = GameState.Loading;
            _playingSince = -1e9f;
            _debugZone = -1;
            ResetDebugStatics();
            _musicForceRelease = false;
            _picked = 0;
            for (int i = 0; i < _pick.Length; i++) _pick[i] = null;
            _played = 0;
            _limited = 0;
            _tickErrors = 0;
        }
    }
}
