using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Takes over everything a QA capture needs to control and puts it all back afterwards: the camera (through
    /// PlayerCamera's cinematic mode), the clock and weather, the HUD, the FPS counter and film grain, the frame-rate
    /// cap, and the player, who is made invulnerable, frozen and (when on foot) parked out in the south-east desert so
    /// pedestrians, traffic and street props never wander into the shots. Nothing here touches the save game.
    /// </summary>
    internal sealed class QaSession
    {
        /// <summary>Where the player waits during a capture: open desert, far from every golden view and the flythrough.</summary>
        public static readonly Vector3 ParkingSpot = new Vector3(CityLayout.MapHalf - 160f, 0f, -(CityLayout.MapHalf - 160f));

        private bool _active;
        private bool _deterministic;
        private float _hours;
        private bool _frozen;
        private Weather _current, _target;
        private float _blend;
        private bool _hudHidden;
        private bool _showFps;
        private bool _filmGrain;
        private bool _controls;
        private bool _invulnerable;
        private float _camYaw, _camPitch;
        private bool _parked;
        private Vector3 _parkedFrom;
        private float _parkedYaw;
        private bool _uncapped;
        private int _targetFrameRate;
        private int _vSync;
        /// <summary>True while the capture itself has shifted into the Veil for a view.</summary>
        private bool _inVeil;

        /// <summary>True when the player was moved to the parking spot for this capture.</summary>
        public bool PlayerParked { get { return _parked; } }
        /// <summary>The player's own film grain setting (grain is switched off during golden shots so images compare cleanly).</summary>
        public bool FilmGrainSetting { get { return _filmGrain; } }
        /// <summary>True while film grain is held off for the capture.</summary>
        public bool GrainSuppressed { get { return _deterministic; } }
        public bool FrameRateUncapped { get { return _uncapped; } }

        // ------------------------------------------------------------------ getting ready

        /// <summary>
        /// Waits until a capture can start: the game has booted, the title screen has been left for Free Roam, and no
        /// menu, phone, fade or cutscene is in the way. Fails the job if a mission is running or nothing happens for
        /// four minutes.
        /// </summary>
        public static IEnumerator WaitForPlayable(QaJob job)
        {
            float deadline = Time.realtimeSinceStartup + 240f;
            float nextFreeRoamTry = 0f;
            while (true)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    job.Fail("The game did not reach free play within four minutes (still loading, or stuck on a screen).");
                    yield break;
                }
                GameRoot root = Game.Root;
                if (root == null)
                {
                    job.Progress = "waiting for the game to boot";
                    yield return null;
                    continue;
                }
                GameState s = Game.State;
                if (s == GameState.Title)
                {
                    job.Progress = "starting Free Roam from the title screen";
                    if (Time.realtimeSinceStartup >= nextFreeRoamTry)
                    {
                        nextFreeRoamTry = Time.realtimeSinceStartup + 5f;
                        root.NewGame(true);
                    }
                    yield return null;
                    continue;
                }
                if (s == GameState.Paused && Game.Menus != null && Game.Menus.IsOpen)
                {
                    // Started from the pause or debug menu: close it so it is not in the pictures.
                    Game.Menus.Close();
                    yield return null;
                    continue;
                }
                if (Game.Phone != null && Game.Phone.IsOpen) Game.Phone.Close();
                if (Game.Missions != null && Game.Missions.Active != null)
                {
                    job.Fail("A mission or activity is running. Finish or quit it, then start the capture again.");
                    yield break;
                }
                if (Game.Wanted != null && Game.Wanted.Stars > 0)
                {
                    job.Fail("The police are after you. Lose them first (or start from the title screen), then start the capture again.");
                    yield break;
                }
                bool ready = Interruption() == null && Game.Player != null && !Game.Player.IsDead
                    && Game.Cam != null && !Game.Cam.InCinematic && Game.Atmo != null;
                if (ready) yield break;
                job.Progress = "waiting for free play (state " + s + ")";
                yield return null;
            }
        }

        /// <summary>
        /// What is in the way of plain free play right now (a menu, the phone, a fade, or a WASTED, BUSTED or cutscene
        /// sequence), or null when nothing is. Captures check it so a menu opened by accident never ends up in the
        /// pictures or the timings.
        /// </summary>
        public static string Interruption()
        {
            GameState s = Game.State;
            if (s != GameState.Playing) return "the game left free play (" + s + ")";
            if (Game.Menus != null && (Game.Menus.IsOpen || Game.Menus.TitleOpen)) return "a menu is open";
            if (Game.Phone != null && Game.Phone.IsOpen) return "the phone is open";
            if (Game.Hud != null && Game.Hud.FadeAlpha >= 0.01f) return "the screen is fading";
            return null;
        }

        // ------------------------------------------------------------------ take over and restore

        /// <summary>
        /// Takes over the world. parkPlayer moves an on-foot player to the parking spot. deterministic switches film grain
        /// off (it is random every frame, so it would show up as noise in image comparisons). A capture always starts in
        /// the Outer City: a player inside the Veil is shifted out at once (and stays out afterwards).
        /// </summary>
        public void Begin(bool parkPlayer, bool deterministic)
        {
            if (_active) return;
            _active = true;
            _deterministic = deterministic;
            if (Game.Inner != null && Game.Inner.IsInner) Game.Inner.Shift(false, true);
            Atmosphere atmo = Game.Atmo;
            if (atmo != null)
            {
                _hours = atmo.Hours;
                _frozen = atmo.Frozen;
                _current = atmo.Current;
                _target = atmo.Target;
                _blend = atmo.WeatherBlend;
                atmo.Frozen = true;
            }
            if (Game.Hud != null) _hudHidden = Game.Hud.Hidden;
            _showFps = Settings.ShowFps;
            _filmGrain = Settings.FilmGrain;
            PlayerController p = Game.Player;
            if (p != null)
            {
                _controls = p.ControlsEnabled;
                p.ControlsEnabled = false;
                if (p.Health != null)
                {
                    _invulnerable = p.Health.Invulnerable;
                    p.Health.Invulnerable = true;
                }
                if (parkPlayer && !p.InVehicle && !p.IsDead && !p.IsRagdolled)
                {
                    _parkedFrom = p.transform.position;
                    _parkedYaw = p.transform.eulerAngles.y;
                    p.Teleport(GameRoot.FindGround(ParkingSpot) + Vector3.up * 0.05f, 0f);
                    _parked = true;
                }
            }
            if (Game.Cam != null)
            {
                _camYaw = Game.Cam.Yaw;
                _camPitch = Game.Cam.Pitch;
            }
            Enforce();
        }

        /// <summary>Called every frame while the capture runs: keeps the HUD, FPS counter, grain and clock where the capture wants them.</summary>
        public void Enforce()
        {
            if (!_active) return;
            if (Game.Hud != null) Game.Hud.Hidden = true;
            Settings.ShowFps = false;
            if (_deterministic) Settings.FilmGrain = false;
            if (Game.Atmo != null) Game.Atmo.Frozen = true;
        }

        /// <summary>
        /// Sets the world up for one golden view: inside the Veil or not (instant shifts, with the Veil's drain paused while
        /// the capture is inside), and whether the Frostline cold and the Veil's growths follow the camera.
        /// </summary>
        public void PrepareView(GoldenView view)
        {
            // Focus first, so the Veil's first growth scan (an instant shift runs one) is already around this camera.
            SetFocus(view.FollowCamera, view.Position);
            SetVeil(view.Inner);
        }

        private void SetVeil(bool inside)
        {
            InnerWorld iw = Game.Inner;
            if (iw == null) return;
            if (inside)
            {
                if (!iw.IsInner)
                {
                    iw.Shift(true, true);
                    _inVeil = true;
                }
                iw.DrainPaused = true;
            }
            else if (_inVeil)
            {
                // An instant shift out also clears DrainPaused.
                if (iw.IsInner) iw.Shift(false, true);
                _inVeil = false;
            }
        }

        private static void SetFocus(bool on, Vector3 at)
        {
            if (Game.Atmo != null)
            {
                Game.Atmo.FocusOverride = on;
                Game.Atmo.FocusPoint = at;
            }
            if (Game.Inner != null)
            {
                Game.Inner.FocusOverride = on;
                Game.Inner.FocusPoint = at;
            }
        }

        /// <summary>Clear weather at the given hour, applied at once (sun, sky, fog, ambient and reflections).</summary>
        public void SetClock(float hours)
        {
            Atmosphere atmo = Game.Atmo;
            if (atmo == null) return;
            atmo.SetWeather(Weather.Clear, true);
            atmo.SetTime(hours);
        }

        /// <summary>Lifts the frame-rate cap and vertical sync so the flythrough measures the real cost of each frame.</summary>
        public void Uncap()
        {
            if (_uncapped) return;
            _uncapped = true;
            _targetFrameRate = Application.targetFrameRate;
            _vSync = QualitySettings.vSyncCount;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        /// <summary>Puts everything back the way it was. Safe to call more than once and while Play mode is ending.</summary>
        public void End()
        {
            if (!_active) return;
            _active = false;
            // Back to the Outer City, with the weather and the Veil following the player again.
            SetVeil(false);
            SetFocus(false, Vector3.zero);
            // The player first, so the camera settles behind them where they started.
            PlayerController p = Game.Player;
            if (p != null)
            {
                if (_parked && !p.InVehicle) p.Teleport(_parkedFrom, _parkedYaw);
                p.ControlsEnabled = _controls;
                if (p.Health != null) p.Health.Invulnerable = _invulnerable;
            }
            _parked = false;
            PlayerCamera cam = Game.Cam;
            if (cam != null)
            {
                if (cam.InCinematic) cam.ClearCinematic();
                cam.Yaw = _camYaw;
                cam.Pitch = _camPitch;
                cam.CutHistory();
            }
            Atmosphere atmo = Game.Atmo;
            if (atmo != null)
            {
                atmo.Current = _current;
                atmo.Target = _target;
                atmo.WeatherBlend = _blend;
                atmo.Frozen = _frozen;
                atmo.SetTime(_hours);
            }
            if (Game.Hud != null) Game.Hud.Hidden = _hudHidden;
            Settings.ShowFps = _showFps;
            Settings.FilmGrain = _filmGrain;
            if (_uncapped)
            {
                Application.targetFrameRate = _targetFrameRate;
                QualitySettings.vSyncCount = _vSync;
                _uncapped = false;
            }
        }
    }
}
