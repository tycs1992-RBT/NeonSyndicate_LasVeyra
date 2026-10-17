using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 5 of the visual upgrade: the physical camera and the subtle motion that cannot touch play.
    /// The camera is a 36 by 24 mm full-frame sensor with its focal length computed from the same vertical field of
    /// view the game always used (CameraOptics), with the gate fitted vertically so an ultrawide monitor keeps the
    /// vertical view it has today. Every field of view write goes through SetFov. The post stack can read
    /// FocusDistance, DepthOfFieldView, CurrentFov and the camera's aperture; nothing here is written by other systems.
    /// Motion: a slow sway on the title flyover, a slow drift behind the pause menu (ramped in, dropped the instant play
    /// resumes) and a breathing hand-held wobble on cinematic shots that opted in through SetCinematic(.., breathe: true).
    /// All three are additive offsets applied after the camera has been placed and removed again before the next frame's
    /// logic runs, so no camera state, aim ray read by gameplay or QA golden shot (which never opts in) can see them.
    /// Settings.CameraShake off turns all three off as a reduced-motion switch.
    /// </summary>
    public sealed partial class PlayerCamera
    {
        /// <summary>Title flyover sway (also off when Settings.CameraShake is off).</summary>
        public bool TitleSway = true;
        /// <summary>Slow drift of the frozen world behind the pause menu (also off when Settings.CameraShake is off).</summary>
        public bool PauseDrift = true;

        /// <summary>Distance in metres from the camera to what the shot is about: the character or vehicle on foot and
        /// driving, the look point of a cinematic or the title flyover. Read-only, for depth of field in the post stack.</summary>
        public float FocusDistance { get; private set; }
        /// <summary>True while the title flyover camera is live.</summary>
        public bool IsTitleView { get; private set; }
        /// <summary>True while the game is paused behind a menu and the gameplay camera stands still.</summary>
        public bool IsPauseView { get; private set; }
        /// <summary>True for the views where the post stack may use depth of field: cinematics, the title and the pause menu.</summary>
        public bool DepthOfFieldView { get { return _cinematic || IsTitleView || IsPauseView; } }
        /// <summary>The vertical field of view last written to the camera, in degrees.</summary>
        public float CurrentFov { get { return _camFov; } }
        /// <summary>True while the current cinematic shot breathes (its caller opted in).</summary>
        public bool CinematicBreath { get { return _cinematic && _cineBreath; } }

        private const float PauseRampSeconds = 1.5f;
        private const float BreathRampSeconds = 1f;
        private const float BreathPosition = 0.015f;
        private const float BreathDegrees = 0.25f;
        private const float TitlePosition = 1.5f;
        private const float TitleDegrees = 0.6f;
        private const float PausePosition = 0.02f;
        private const float PauseDegrees = 0.3f;

        private float _camFov = 55f;
        private bool _cineBreath;
        private float _cineBreathT;
        private float _pauseT;
        private float _flyFocus = 200f;
        private float _lastAperture = -1f;
        private bool _offsetApplied;
        private Vector3 _basePos;
        private Quaternion _baseRot = Quaternion.identity;

        /// <summary>Scripted shot with an optional hand-held breathing (default off; QA captures use the three-argument form).</summary>
        public void SetCinematic(Vector3 position, Vector3 lookAt, float fov, bool breathe)
        {
            SetCinematic(position, lookAt, fov);
            _cineBreath = breathe;
        }

        // ------------------------------------------------------------------ physical camera

        /// <summary>Turns the camera physical with the game's sensor, gate fit and lens numbers, keeping its field of view.</summary>
        private void SetupPhysical(Camera cam)
        {
            cam.usePhysicalProperties = true;
            cam.sensorSize = new Vector2(CameraOptics.SensorWidth, CameraOptics.SensorHeight);
            // Vertical gate fit: the vertical field of view is the one the game sets, whatever the monitor's aspect.
            cam.gateFit = Camera.GateFitMode.Vertical;
            cam.lensShift = Vector2.zero;
            cam.aperture = CameraOptics.GameplayAperture;
            _lastAperture = CameraOptics.GameplayAperture;
            cam.shutterSpeed = CameraOptics.ShutterSeconds;
            cam.iso = CameraOptics.Iso;
            cam.bladeCount = CameraOptics.BladeCount;
            SetFov(Settings.FieldOfView);
        }

        /// <summary>The one place the field of view is written: the focal length is derived so the perspective is the
        /// same as the plain field of view used to give (the field of view itself is written too, for anything that reads it).</summary>
        private void SetFov(float verticalFov)
        {
            _camFov = verticalFov;
            if (Cam == null) return;
            Cam.fieldOfView = verticalFov;
            Cam.focalLength = CameraOptics.FocalLength(verticalFov, CameraOptics.SensorHeight);
        }

        /// <summary>Focus distance, aperture and the view flags for the post stack, once the camera is placed.</summary>
        private void UpdateOptics()
        {
            IsTitleView = !_cinematic && (Game.State == GameState.Title || Game.State == GameState.Loading || Game.Player == null);
            IsPauseView = !_cinematic && !IsTitleView && Game.State == GameState.Paused;
            float focus;
            if (_cinematic) focus = Vector3.Distance(transform.position, _cineLook);
            else if (IsTitleView) focus = _flyFocus;
            else if (IsFirstPerson) focus = 4f;
            else focus = Mathf.Max(0.3f, DistanceToPivot);
            FocusDistance = focus;
            if (Cam == null) return;
            Cam.focusDistance = focus;
            float aperture = DepthOfFieldView ? CameraOptics.CinematicAperture : CameraOptics.GameplayAperture;
            if (aperture != _lastAperture)
            {
                _lastAperture = aperture;
                Cam.aperture = aperture;
            }
        }

        // ------------------------------------------------------------------ additive motion

        /// <summary>Undoes last frame's additive offset so this frame's logic starts from the camera's own pose.</summary>
        private void RestoreBasePose()
        {
            if (!_offsetApplied) return;
            _offsetApplied = false;
            transform.SetPositionAndRotation(_basePos, _baseRot);
        }

        private bool MotionAllowed { get { return Settings.CameraShake; } }

        /// <summary>Adds a small position (metres) and rotation (degrees) offset in the camera's own frame.</summary>
        private void ApplyOffset(Vector3 localPos, Vector3 eulerDeg)
        {
            _basePos = transform.position;
            _baseRot = transform.rotation;
            _offsetApplied = true;
            Quaternion rot = _baseRot * Quaternion.Euler(eulerDeg);
            transform.SetPositionAndRotation(_basePos + _baseRot * localPos, rot);
        }

        /// <summary>Title flyover: a slow sway on top of the orbit.</summary>
        private void ApplyTitleSway()
        {
            _pauseT = 0f;
            if (!TitleSway || !MotionAllowed) return;
            Vector3 pos, rot;
            CameraOptics.Drift(Time.unscaledTime, 0f, out pos, out rot);
            ApplyOffset(pos * TitlePosition, rot * TitleDegrees);
        }

        /// <summary>Pause menu: the frozen world drifts slowly, eased in after the pause begins. The offset simply stops
        /// being applied the frame play resumes, so the gameplay camera is untouched from its first live frame.</summary>
        private void ApplyPauseDrift(float dt)
        {
            if (Game.State != GameState.Paused || !PauseDrift || !MotionAllowed)
            {
                _pauseT = 0f;
                return;
            }
            _pauseT += dt;
            float w = CameraOptics.RampIn(_pauseT, PauseRampSeconds);
            if (w <= 0f) return;
            Vector3 pos, rot;
            CameraOptics.Drift(Time.unscaledTime, 1f, out pos, out rot);
            ApplyOffset(pos * (PausePosition * w), rot * (PauseDegrees * w));
        }

        /// <summary>Cinematic shots that opted in: a breathing hand-held wobble, eased in from the cut.</summary>
        private void ApplyCinematicBreath(float dt)
        {
            _pauseT = 0f;
            if (!_cineBreath || !MotionAllowed)
            {
                _cineBreathT = 0f;
                return;
            }
            _cineBreathT += dt;
            float w = CameraOptics.RampIn(_cineBreathT, BreathRampSeconds);
            Vector3 pos, rot;
            CameraOptics.Breath(_cineBreathT, out pos, out rot);
            ApplyOffset(pos * (BreathPosition * w), rot * (BreathDegrees * w));
        }
    }
}
