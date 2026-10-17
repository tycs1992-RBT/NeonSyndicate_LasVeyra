using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What the live companion body is doing right now (the bond system sets it).</summary>
    public enum CompanionMode
    {
        /// <summary>Following the player's breadcrumb trail (direct when the way is clear and close).</summary>
        Follow = 0,
        /// <summary>Staying at its wait point, watching the player.</summary>
        Wait = 1,
        /// <summary>Moving to a goal the bond system gave it (an assist position, a relay, a mechanism).</summary>
        Goal = 2,
        /// <summary>Lying down after its health ran out (it rests, never dies).</summary>
        Downed = 3,
        /// <summary>Clamped at a rescue site: it struggles, warns and settles; it does not move.</summary>
        Rescue = 4
    }

    /// <summary>
    /// A bonded companion's live body: a creature rig built from its CreatureBody and driven by the shared pose solver
    /// (like the district drake exhibit), with its own movement brain. It is not a Creature: it never joins
    /// Creature.All, has no CharacterBase or health component, sits on the IgnoreRaycast layer with its colliders off
    /// (so bullets, blades, powers, lock-on and hostile AI never find it) and only becomes hittable during a rescue,
    /// where a hit resets the approach instead of hurting it. Movement lives in CompanionController.Motion.cs.
    /// </summary>
    public sealed partial class CompanionController : MonoBehaviour
    {
        public CreatureRig Rig { get; private set; }
        public CompanionProfile Profile { get; private set; }
        /// <summary>The companion record this body belongs to (0 while it is still being rescued).</summary>
        public int Uid;
        public string Archetype { get { return Profile != null ? Profile.Archetype : ""; } }
        /// <summary>The evolution stage and specialization the body was built for (a change rebuilds it).</summary>
        public int BuiltEvolution;
        public string BuiltSpec = "";
        public CompanionMode Mode = CompanionMode.Follow;
        public bool Hidden { get; private set; }
        public bool InRescueMode { get; private set; }

        private CreatureAction _action = CreatureAction.None;
        private float _actionT = -1f, _actionLen, _windup = 0.4f, _strike = 0.6f;
        private bool _actionHeld;
        private bool _strikeFired;
        private Action _onStrike;
        private bool _hasLook;
        private Vector3 _look;
        private float _hurt;
        private Vector3 _hurtDir = Vector3.back;
        private float _glowBoost;
        private float _alert;
        private float _clock;
        private float _animDt;
        private int _animFrame;
        private float _dissolve;
        private Func<float, float, float> _groundFn;

        /// <summary>Builds the live body at a point. The root sits on IgnoreRaycast with every collider disabled.</summary>
        public static CompanionController Create(CreatureBody body, CompanionProfile profile, Vector3 pos, float yaw, Transform parent)
        {
            GameObject go = new GameObject("Companion - " + (profile != null ? profile.Title : "Bond"));
            go.layer = Layers.IgnoreRaycast;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            CompanionController c = go.AddComponent<CompanionController>();
            try
            {
                c.Setup(body, profile, pos, yaw);
            }
            catch
            {
                Destroy(go);
                throw;
            }
            return c;
        }

        private void Setup(CreatureBody body, CompanionProfile profile, Vector3 pos, float yaw)
        {
            Profile = profile ?? CompanionArchetypes.QuartzGuide;
            Rig = CreatureRig.Build(gameObject, body, null, Settings.Graphics >= GraphicsPreset.High);
            Rig.SetCollidersEnabled(false);
            _groundFn = GroundAt;
            InitMotion(pos, yaw);
            CreaturePoseInput p = CreaturePoseInput.Idle(pos, yaw);
            p.GroundHeight = _groundFn;
            Rig.Pose.Reset(pos, yaw);
            Rig.Pose.Solve(ref p, 1f / 60f);
            Rig.ApplyPose();
        }

        private float GroundAt(float x, float z) { return _groundY; }

        // ================================================================== presentation

        /// <summary>The body's chest (aim and arc start), head and mouth in the world.</summary>
        public Vector3 ChestPosition { get { return Rig != null && Rig.AimPoint != null ? Rig.AimPoint.position : transform.position + Vector3.up * Profile.BodyHeight * 0.6f; } }
        public Vector3 HeadPosition { get { return Rig != null && Rig.HeadPoint != null ? Rig.HeadPoint.position : ChestPosition; } }
        public Vector3 MouthPosition { get { return Rig != null && Rig.MouthPoint != null ? Rig.MouthPoint.position : HeadPosition; } }

        /// <summary>Hides (fused, riding, retreated to rest) or shows the body. Hidden bodies do not move or animate.</summary>
        public void SetHidden(bool hidden)
        {
            Hidden = hidden;
            if (Rig != null && Rig.Renderer != null) Rig.Renderer.enabled = !hidden;
            if (!hidden)
            {
                _vel = Vector3.zero;
                _stuck.Reset();
            }
        }

        /// <summary>Rescue mode: the body becomes hittable (Characters layer, colliders on) so an attack can reset the
        /// approach; the hits never hurt it. Off again once it is bonded.</summary>
        public void SetRescueMode(bool on, Action onAttacked)
        {
            InRescueMode = on;
            Layers.SetRecursive(gameObject, on ? Layers.Characters : Layers.IgnoreRaycast);
            if (Rig != null) Rig.SetCollidersEnabled(on);
            CompanionHitReceiver r = GetComponent<CompanionHitReceiver>();
            if (on)
            {
                if (r == null) r = gameObject.AddComponent<CompanionHitReceiver>();
                r.Attacked = onAttacked;
            }
            else if (r != null)
            {
                r.Attacked = null;
                Destroy(r);
            }
        }

        /// <summary>Plays a one-shot action over seconds; onStrike runs once when the strike phase begins.</summary>
        public void PlayAction(CreatureAction action, float seconds, float windup, float strike, Action onStrike)
        {
            _action = action;
            _actionLen = Mathf.Max(0.1f, seconds);
            _actionT = 0f;
            _windup = Mathf.Clamp(windup, 0.05f, 0.9f);
            _strike = Mathf.Clamp(strike, _windup + 0.02f, 0.98f);
            _actionHeld = false;
            _strikeFired = false;
            _onStrike = onStrike;
        }

        /// <summary>Holds an action's pose at a fixed point (the brace stance, the downed collapse, a clamp struggle).</summary>
        public void HoldAction(CreatureAction action, float t, float windup, float strike)
        {
            _action = action;
            _actionT = Mathf.Clamp01(t);
            _windup = Mathf.Clamp(windup, 0.05f, 0.9f);
            _strike = Mathf.Clamp(strike, _windup + 0.02f, 0.98f);
            _actionHeld = true;
            _onStrike = null;
        }

        public void ClearAction()
        {
            _action = CreatureAction.None;
            _actionT = -1f;
            _actionHeld = false;
            _onStrike = null;
        }

        /// <summary>True while a one-shot action is still playing.</summary>
        public bool ActionPlaying { get { return _action != CreatureAction.None && !_actionHeld && _actionT >= 0f && _actionT < 1f; } }

        public void SetLook(Vector3 target)
        {
            _hasLook = true;
            _look = target;
        }

        public void ClearLook() { _hasLook = false; }

        /// <summary>A hit flash and flinch from a direction (heat, strain).</summary>
        public void Flinch(float amount, Vector3 fromDirection)
        {
            _hurt = Mathf.Max(_hurt, Mathf.Clamp01(amount));
            if (fromDirection.sqrMagnitude > 1e-4f) _hurtDir = fromDirection.normalized;
            if (Rig != null) Rig.HitFlash(Mathf.Clamp01(amount));
        }

        /// <summary>Extra seam and eye glow (assist ready, warning flare, tuning beat). Decays by itself.</summary>
        public void Glow(float boost) { _glowBoost = Mathf.Max(_glowBoost, boost); }

        /// <summary>0 relaxed to 1 alert (a fight nearby, a rescue warning).</summary>
        public void SetAlert(float a) { _alert = Mathf.Clamp01(a); }

        /// <summary>Shrink-and-fade toward its root (0 whole, 1 gone): the downed retreat and the return.</summary>
        public void SetDissolve(float k)
        {
            _dissolve = Mathf.Clamp01(k);
            if (Rig != null) Rig.SetDissolve(_dissolve);
        }

        // ================================================================== frame

        private void Update()
        {
            if (Rig == null || Hidden) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _clock += dt;
            TickAction(dt);
            TickMotion(dt);
            Animate(dt);
            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 2.5f);
            _glowBoost = Mathf.MoveTowards(_glowBoost, 0f, dt * 1.5f);
        }

        private void TickAction(float dt)
        {
            if (_action == CreatureAction.None || _actionHeld) return;
            _actionT += dt / _actionLen;
            if (!_strikeFired && _actionT >= _windup)
            {
                _strikeFired = true;
                Action cb = _onStrike;
                _onStrike = null;
                if (cb != null)
                {
                    try { cb(); }
                    catch (Exception e) { Log.Exception("Companion action", e); }
                }
            }
            if (_actionT >= 1f) ClearAction();
        }

        private void Animate(float dt)
        {
            if (Rig.Renderer == null) return;
            _animDt += dt;
            Transform cam = Game.Cam != null ? Game.Cam.transform : null;
            float camD = cam != null ? (cam.position - transform.position).magnitude : 0f;
            Rig.Renderer.enabled = camD < 160f;
            if (!Rig.Renderer.enabled) return;
            int every = camD < 30f ? 1 : (camD < 60f ? 2 : 4);
            if (!Rig.Renderer.isVisible) every = Mathf.Max(every, 6);
            _animFrame++;
            if (every > 1 && (_animFrame % every) != 0) return;
            float adt = Mathf.Min(_animDt, 0.1f);
            _animDt = 0f;
            CreaturePoseInput p = CreaturePoseInput.Idle(transform.position, _yaw);
            p.YawRate = _yawRate;
            p.Velocity = _vel;
            p.Grounded = !Flying;
            p.Flying = Flying;
            p.Altitude = Flying ? Mathf.Max(0f, transform.position.y - _groundY) : 0f;
            if (_action != CreatureAction.None && _actionT >= 0f)
            {
                p.Action = _action;
                p.ActionT = Mathf.Clamp01(_actionT);
                p.Windup = _windup;
                p.Strike = _strike;
            }
            p.Alert = _alert;
            p.Hurt = _hurt;
            p.HurtDirection = _hurtDir;
            if (_hasLook)
            {
                p.HasLookTarget = true;
                p.LookTarget = _look;
            }
            p.GroundHeight = _groundFn;
            p.Time = _clock;
            Rig.Pose.Solve(ref p, adt);
            Rig.ApplyPose();
            float breathe = 0.12f * Mathf.Sin(_clock * 1.7f);
            Rig.SetGlow(1f + breathe + _glowBoost + _alert * 0.25f);
        }

        /// <summary>Teleports the body (recall, summon, separation) and forgets planted feet so nothing drags across the map.</summary>
        public void Teleport(Vector3 pos, float yaw)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            _yaw = yaw;
            _vel = Vector3.zero;
            _lastPos = pos;
            _groundY = pos.y;
            Flying = false;
            _hopT = -1f;
            _stuck.Reset();
            _crumbId = 0;
            if (Rig != null) Rig.Pose.Reset(pos, yaw);
        }

        private void OnDestroy()
        {
            _onStrike = null;
        }
    }

    /// <summary>
    /// Present only during a rescue: lets bullets, blades and crystal shots find the trapped creature so an attack can
    /// reset the approach. It never takes damage, never dies and pays nothing (no kill, loot or reputation).
    /// </summary>
    public sealed class CompanionHitReceiver : MonoBehaviour, IDamageable
    {
        public Action Attacked;
        private float _last = -10f;

        public bool IsAlive { get { return true; } }

        public void TakeDamage(DamageInfo info)
        {
            if (!info.FromPlayer) return;
            if (Time.time - _last < 0.4f) return;
            _last = Time.time;
            Action a = Attacked;
            if (a == null) return;
            try { a(); }
            catch (Exception e) { Log.Exception("Rescue attacked", e); }
        }
    }
}
