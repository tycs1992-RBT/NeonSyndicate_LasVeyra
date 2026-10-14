using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The travel powers' save block: the powers the story has unlocked (kept once earned), the debug unlock, and how
    /// often each power has been used (the button hints stop after the first few uses). Old saves have no block, so
    /// everything starts false and zero and the unlocks follow the save's story progress.
    /// </summary>
    [Serializable]
    public sealed class TravelSave
    {
        public bool LeapUnlocked;
        public bool FlightUnlocked;
        public bool DebugUnlocked;
        public int SurgeUses;
        public int LeapUses;
        public int GlideUses;
        public int FlightUses;
    }

    /// <summary>
    /// Travel powers (Game.Travel): super speed (Surge), the Super Leap and Glide, and Flight. The player's movement
    /// code (PlayerController.Travel.cs) asks this system what is unlocked and reports each use. This system watches the
    /// story progress (Leap and Glide after the first story mission, Flight after the second, everything in Free Roam),
    /// announces new powers, shows the first-use button hints, plays the wind and impact sounds, draws the travel HUD
    /// and the F2 Travel page, and saves with the game through IWorldSystem. Travel powers cost nothing to use.
    /// </summary>
    public sealed partial class TravelPowers : MonoBehaviour, IWorldSystem
    {
        /// <summary>Uses after which a power's button hint stops appearing.</summary>
        public const int HintUses = 3;
        /// <summary>Seconds between two showings of the same hint.</summary>
        public const float HintCooldown = 25f;

        private bool _savedLeap;
        private bool _savedFlight;
        private bool _debug;
        private readonly int[] _uses = new int[4];
        private readonly float[] _hintAt = { -999f, -999f, -999f, -999f };
        private bool _primed;
        private bool _leapNow;
        private bool _flightNow;
        private bool _announceLeap;
        private bool _announceFlight;
        private bool _announceFree;
        private float _announceDelay;
        private TravelAudio _audio;

        public bool SurgeUnlocked { get { return TravelMath.SurgeUnlocked(); } }
        public bool LeapUnlocked { get { return _leapNow; } }
        public bool GlideUnlocked { get { return _leapNow; } }
        public bool FlightUnlocked { get { return _flightNow; } }
        public bool DebugUnlocked { get { return _debug; } }
        /// <summary>The travel sounds (null if they failed to build).</summary>
        public TravelAudio Audio { get { return _audio; } }

        public void Init()
        {
            WorldSystems.Register(this);
            try
            {
                _audio = new TravelAudio();
                _audio.Init(transform);
            }
            catch (Exception e)
            {
                Log.Exception("Travel audio", e);
                _audio = null;
            }
            RegisterUi();
            ResetAll();
        }

        public void ResetAll()
        {
            _savedLeap = false;
            _savedFlight = false;
            _debug = false;
            for (int i = 0; i < _uses.Length; i++)
            {
                _uses[i] = 0;
                _hintAt[i] = -999f;
            }
            ClearLive();
        }

        public void Capture(SaveData d)
        {
            d.Travel = new TravelSave
            {
                LeapUnlocked = _savedLeap,
                FlightUnlocked = _savedFlight,
                DebugUnlocked = _debug,
                SurgeUses = _uses[(int)TravelPower.Surge],
                LeapUses = _uses[(int)TravelPower.Leap],
                GlideUses = _uses[(int)TravelPower.Glide],
                FlightUses = _uses[(int)TravelPower.Flight]
            };
        }

        public void Restore(SaveData d)
        {
            TravelSave s = d != null && d.Travel != null ? d.Travel : new TravelSave();
            int story = d != null ? d.StoryProgress : 0;
            // Old saves have no Travel block: the unlocks the story progress implies apply.
            _savedLeap = s.LeapUnlocked || story >= TravelMath.LeapStory;
            _savedFlight = s.FlightUnlocked || story >= TravelMath.FlightStory;
            _debug = s.DebugUnlocked;
            _uses[(int)TravelPower.Surge] = Mathf.Max(0, s.SurgeUses);
            _uses[(int)TravelPower.Leap] = Mathf.Max(0, s.LeapUses);
            _uses[(int)TravelPower.Glide] = Mathf.Max(0, s.GlideUses);
            _uses[(int)TravelPower.Flight] = Mathf.Max(0, s.FlightUses);
            for (int i = 0; i < _hintAt.Length; i++) _hintAt[i] = -999f;
            ClearLive();
        }

        /// <summary>Forgets the live unlock state; the next poll in play re-reads it silently (no announcement on load).</summary>
        private void ClearLive()
        {
            _primed = false;
            _leapNow = false;
            _flightNow = false;
            _announceLeap = false;
            _announceFlight = false;
            _announceFree = false;
        }

        // ------------------------------------------------------------------ per frame

        private void Update()
        {
            if (Game.IsPlaying)
            {
                if (!_primed || Time.frameCount % 10 == 0) Refresh(!_primed);
                UpdateAnnouncements(Time.unscaledDeltaTime);
            }
            PlayerController p = Game.Player;
            TravelMode mode = TravelMode.None;
            float speed = 0f;
            if (p != null && Game.IsPlaying && !p.IsDead && !p.IsRagdolled && !p.InVehicle)
            {
                mode = p.TravelState;
                speed = p.TravelSpeed;
            }
            if (_audio != null) _audio.Tick(mode, speed, Time.unscaledDeltaTime, Game.IsPlaying);
        }

        /// <summary>Re-reads the story progress and Free Roam. Only called in play, after loads and new games are applied.</summary>
        private void Refresh(bool silent)
        {
            int story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            bool free = Game.Root != null && Game.Root.FreeRoam;
            if (story >= TravelMath.LeapStory) _savedLeap = true;
            if (story >= TravelMath.FlightStory) _savedFlight = true;
            bool leap = TravelMath.LeapUnlocked(story, free, _savedLeap, _debug);
            bool flight = TravelMath.FlightUnlocked(story, free, _savedFlight, _debug);
            if (!silent && _primed)
            {
                if (leap && !_leapNow) { _announceLeap = true; _announceDelay = 2.5f; }
                if (flight && !_flightNow) { _announceFlight = true; _announceDelay = 2.5f; }
            }
            else if (!_primed && free && _uses[(int)TravelPower.Flight] == 0)
            {
                // Free Roam unlocks everything without a mission, so introduce the powers once, after its banner.
                _announceFree = true;
                _announceDelay = 9f;
            }
            _leapNow = leap;
            _flightNow = flight;
            _primed = true;
        }

        /// <summary>Announces a newly unlocked power once no mission or cutscene is running.</summary>
        private void UpdateAnnouncements(float dt)
        {
            if (!_announceLeap && !_announceFlight && !_announceFree) return;
            bool busy = (Game.Missions != null && Game.Missions.Active != null) || (Game.Cam != null && Game.Cam.InCinematic);
            if (busy) { _announceDelay = Mathf.Max(_announceDelay, 1.5f); return; }
            _announceDelay -= dt;
            if (_announceDelay > 0f || Game.Hud == null) return;
            if (_announceFree)
            {
                _announceFree = false;
                Game.Hud.Banner("TRAVEL POWERS", "Surge, Super Leap, Glide and Flight are all yours. Sprint, then press " + Key(PadButton.L3, "Ctrl") + " to Surge.", UIKit.Cyan, 6f);
                ForceHint(TravelPower.Surge);
                return;
            }
            if (_announceFlight)
            {
                _announceFlight = false;
                _announceDelay = 7f;
                Game.Hud.Banner("THE BOND DEEPENS: FLIGHT", "Protecting Luma made the Third Eye stronger. Double-tap " + Key(PadButton.South, "Space") + " in the air to fly.", UIKit.Cyan, 6f);
                ForceHint(TravelPower.Flight);
                return;
            }
            _announceLeap = false;
            _announceDelay = 7f;
            Game.Hud.Banner("THE BOND DEEPENS: LEAP AND GLIDE", "Protecting Luma made the Third Eye stronger. Crouch, then hold and release " + Key(PadButton.South, "Space") + ". Hold it in the air to glide.", UIKit.Cyan, 6f);
            ForceHint(TravelPower.Leap);
        }

        // ------------------------------------------------------------------ uses and hints

        /// <summary>Counts one use of a power (the hint for it stops after HintUses).</summary>
        public void NoteUse(TravelPower p)
        {
            int i = (int)p;
            if (_uses[i] < 100000) _uses[i]++;
        }

        /// <summary>How often a power has been used in this playthrough.</summary>
        public int Uses(TravelPower p)
        {
            return _uses[(int)p];
        }

        /// <summary>Shows a power's button hint, at most every HintCooldown seconds, until it has been used HintUses times.</summary>
        public void Hint(TravelPower p)
        {
            int i = (int)p;
            if (_uses[i] >= HintUses) return;
            if (Time.unscaledTime - _hintAt[i] < HintCooldown) return;
            ForceHint(p);
        }

        private void ForceHint(TravelPower p)
        {
            _hintAt[(int)p] = Time.unscaledTime;
            if (Game.Hud != null) Game.Hud.Notify(HintText(p), 6f);
        }

        /// <summary>
        /// The button hint for a power, in the names of the device used last (keyboard or controller). A button tag is
        /// never followed by a space, because the HUD's prompt rewriter turns "[B] " (the keyboard power key) into the
        /// controller's power chord, and a pad's B button would otherwise be caught by it.
        /// </summary>
        public static string HintText(TravelPower p)
        {
            string jump = Key(PadButton.South, "Space");
            switch (p)
            {
                case TravelPower.Surge:
                    return "SURGE (super speed): while sprinting, press " + Key(PadButton.L3, "Ctrl") + ". Press it again, or stop, to end it.";
                case TravelPower.Leap:
                    return "SUPER LEAP: crouch, then hold and let go of " + jump + ". Or jump during a Surge for a long leap.";
                case TravelPower.Glide:
                    return "GLIDE: in the air, hold " + jump + ". Steer with the stick or the camera; let go to drop.";
                default:
                    return "FLIGHT: in the air, double-tap " + jump + ". Rise: hold " + jump + ". Sink: hold " + Key(PadButton.L3, "Ctrl")
                        + ". Fast: hold " + Key(PadButton.East, "Shift") + ". Double-tap again to stop.";
            }
        }

        /// <summary>A button name for prompts: the controller's glyph when it was used last, otherwise the key in brackets.</summary>
        public static string Key(PadButton pad, string key)
        {
            return Pad.Connected && Pad.LastUsed ? Pad.Tag(pad) : "[" + key + "]";
        }

        // ------------------------------------------------------------------ sounds and debug

        /// <summary>Plays a travel one-shot if the sounds are available.</summary>
        public void PlaySound(TravelSounds.Clip c, float volume, float pitch)
        {
            if (_audio != null) _audio.Play(c, volume, pitch);
        }

        /// <summary>F2 Travel page: unlock everything (saved with the game), or go back to the story's unlocks.</summary>
        public void DebugUnlockAll(bool on)
        {
            _debug = on;
            if (!on)
            {
                _savedLeap = false;
                _savedFlight = false;
            }
            _primed = false;
        }

        /// <summary>F2 Travel page: show every first-use hint again.</summary>
        public void DebugResetHints()
        {
            for (int i = 0; i < _uses.Length; i++)
            {
                _uses[i] = 0;
                _hintAt[i] = -999f;
            }
        }
    }
}
