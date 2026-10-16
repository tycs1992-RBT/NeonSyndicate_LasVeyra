using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Veil: the Inner World is a violet-crimson mirror of Las Veyra that the player enters with a Veil shift (hold the
    /// Third Eye for 0.7 s). Inside, the sky turns red-violet under a crimson sun, people and moving traffic are gone,
    /// street lights burn violet, crystal growths spring up around the player and islands of rock and crystal float far off.
    /// The Veil feeds on crystal energy and throws the player back out when it runs dry.
    /// Gameplay API: IsInner, Blend, Shift, Shifted, CrystalSpotsNear and the static Unlocked.
    /// </summary>
    public sealed partial class InnerWorld : MonoBehaviour
    {
        public enum State
        {
            Outer,
            Inner
        }

        /// <summary>Seconds a shift takes to blend the world across.</summary>
        public const float BlendSeconds = 1.2f;
        /// <summary>Crystal energy a player shift into the Veil costs.</summary>
        public const float EnterCost = 25f;
        /// <summary>Crystal energy drained per second while inside.</summary>
        public const float DrainPerSecond = 2f;
        /// <summary>How long the Third Eye input must be held to shift (a shorter press is the usual Third Eye tap).</summary>
        public const float HoldSeconds = 0.7f;

        public State Current { get; private set; }
        public bool IsInner { get { return Current == State.Inner; } }
        /// <summary>0 in the Outer City, 1 fully inside; eased over BlendSeconds. Atmosphere and the colour grade follow it.</summary>
        public float Blend { get; private set; }
        /// <summary>1 at the moment of a shift, fading to 0 within about half a second (HUD flash).</summary>
        public float Pulse { get; private set; }
        /// <summary>When true the Veil does not drain crystal energy (for scripted Inner World scenes). Cleared on reset.</summary>
        public bool DrainPaused;
        /// <summary>QA captures: when true the crystal growths and spires gather around FocusPoint (the camera), not the player.</summary>
        public bool FocusOverride;
        public Vector3 FocusPoint;
        /// <summary>Raised after every shift starts: true when entering the Inner World, false when leaving it.</summary>
        public event Action<bool> Shifted;

        /// <summary>The Veil opens together with the Third Eye (same flag as Abilities.ThirdEyeUnlocked).</summary>
        public static bool Unlocked
        {
            get { return Game.Abilities != null && Game.Abilities.ThirdEyeUnlocked; }
            set { if (Game.Abilities != null) Game.Abilities.ThirdEyeUnlocked = value; }
        }

        /// <summary>What a player shift into the Veil costs right now: EnterCost with the skill, guild and brew discounts (SkillMods.VeilShiftCost).</summary>
        public static float ShiftCost { get { return EnterCost * SkillMods.VeilShiftCost; } }

        /// <summary>
        /// Crystal energy lost per real second inside the Veil right now: the Veil's own drain (with SkillMods.VeilDrain,
        /// nothing while DrainPaused) plus the Third Eye's bullet time when it is open. The HUD's VEIL tag divides by this.
        /// </summary>
        public float DrainNow(Abilities ab)
        {
            return DrainRate(DrainPaused, SkillMods.VeilDrain, ab != null && ab.ThirdEyeActive ? ab.ThirdEyeDrain : 0f);
        }

        /// <summary>Pure: the Veil's drain (unless paused) times its skill multiplier, plus any other drain at the same time.</summary>
        public static float DrainRate(bool paused, float veilDrainMultiplier, float otherDrain)
        {
            return (paused ? 0f : DrainPerSecond * Mathf.Max(0f, veilDrainMultiplier)) + Mathf.Max(0f, otherDrain);
        }

        private float _t;
        private bool _suppressing;
        private bool _pedsWereSuppressed;
        private bool _trafficWasSuppressed;
        private float _slowTimer;
        private float _lastPlayerShift = -99f;

        public void Init()
        {
            Current = State.Outer;
            BuildVisuals();
        }

        // ------------------------------------------------------------------ shifting

        /// <summary>
        /// Moves the player's world to the Inner World (toInner) or back to the Outer City. instant skips the 1.2 s blend
        /// and the distortion pulse (loading, resets). Costs nothing: the energy price belongs to PlayerShift.
        /// </summary>
        public void Shift(bool toInner, bool instant)
        {
            ShiftInternal(toInner, instant, !instant);
        }

        /// <summary>
        /// The player's own Veil shift (the Third Eye hold): entering costs ShiftCost crystal energy, leaving is free.
        /// Returns false when the shift can't happen (locked, not enough energy, or a shift just happened).
        /// </summary>
        public bool PlayerShift()
        {
            if (!IsInner && Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.IsOperational && Game.Inhabited.Inside)
            {
                if (Game.Hud != null) Game.Hud.Notify("Step outside before opening the Veil.", 2.5f);
                return false;
            }
            if (!IsInner && WorldBounds.IsUnderground(Game.PlayerPosition))
            {
                if (Game.Hud != null) Game.Hud.Notify("The Veil will not open this far underground. Return to the surface first.", 3f);
                return false;
            }
            if (Time.unscaledTime - _lastPlayerShift < 1f) return false;
            Abilities ab = Game.Abilities;
            if (!IsInner)
            {
                if (ab == null || !ab.ThirdEyeUnlocked) return false;
                // Skill trees, the Frequency Order and Forge brews make the shift cheaper (SkillMods.VeilShiftCost).
                float cost = ShiftCost;
                if (ab.Energy < cost)
                {
                    if (Game.Hud != null) Game.Hud.Notify("The Veil won't open: you need " + Mathf.CeilToInt(cost) + " crystal energy.", 2.5f);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.6f);
                    return false;
                }
                ab.Energy -= cost;
            }
            _lastPlayerShift = Time.unscaledTime;
            ShiftInternal(!IsInner, false, true);
            return true;
        }

        private void ShiftInternal(bool toInner, bool instant, bool pulse)
        {
            State target = toInner ? State.Inner : State.Outer;
            if (instant)
            {
                _t = toInner ? 1f : 0f;
                Blend = _t;
                PostGrade.InnerWorld = Blend;
                if (!toInner)
                {
                    Pulse = 0f;
                    DrainPaused = false;
                    if (_slowTimer > 0f)
                    {
                        _slowTimer = 0f;
                        GameTime.Release("veil");
                    }
                }
            }
            if (Current == target)
            {
                if (instant) UpdateVisuals(0f);
                return;
            }
            Current = target;
            if (toInner) EnterWorld();
            else ExitWorld();
            if (pulse) PlayPulse(toInner);
            if (instant) UpdateVisuals(0f);
            RaiseShifted(toInner);
        }

        private void RaiseShifted(bool toInner)
        {
            if (Shifted == null) return;
            // One broken listener (creature or mission code) must not stop the others or the shift itself.
            foreach (Delegate d in Shifted.GetInvocationList())
            {
                try { ((Action<bool>)d)(toInner); }
                catch (Exception e) { Log.Exception("InnerWorld.Shifted listener", e); }
            }
        }

        /// <summary>People and moving traffic leave with the Outer City; the spawners pause until the player returns.</summary>
        private void EnterWorld()
        {
            if (!_suppressing)
            {
                _pedsWereSuppressed = Game.Peds != null && Game.Peds.Suppressed;
                _trafficWasSuppressed = Game.Traffic != null && Game.Traffic.Suppressed;
                _suppressing = true;
            }
            if (Game.Peds != null)
            {
                Game.Peds.Suppressed = true;
                VanishPeople();
            }
            if (Game.Traffic != null)
            {
                Game.Traffic.Suppressed = true;
                Game.Traffic.ClearDrivenTraffic();
            }
            _anchor = Game.PlayerPosition;
        }

        /// <summary>The spawners get back exactly the state they had (missions may have paused them on purpose).</summary>
        private void ExitWorld()
        {
            if (!_suppressing) return;
            if (Game.Peds != null) Game.Peds.Suppressed = _pedsWereSuppressed;
            if (Game.Traffic != null) Game.Traffic.Suppressed = _trafficWasSuppressed;
            _suppressing = false;
        }

        /// <summary>
        /// Removes ordinary pedestrians and gang crews on foot. Anyone already after the player stays (the Veil is no free
        /// escape), and so do police, clerks, mission characters and companions.
        /// </summary>
        private static void VanishPeople()
        {
            for (int i = NpcController.All.Count - 1; i >= 0; i--)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.Persistent || n.InVehicle) continue;
                if (n.Role != NpcRole.Civilian && n.Role != NpcRole.Gang) continue;
                if (n.Target != null && n.Target.IsPlayer) continue;
                Destroy(n.gameObject);
            }
        }

        private void PlayPulse(bool toInner)
        {
            Pulse = 1f;
            Vector3 p = Game.PlayerPosition + Vector3.up * 1.5f;
            if (Game.Fx != null) Game.Fx.Flash(p, toInner ? new Color(0.75f, 0.2f, 1f) : new Color(0.55f, 0.75f, 1f), 7f, 40f, 0.7f);
            if (Game.Cam != null) Game.Cam.Shake(0.4f);
            GameTime.Request("veil", 0.3f);
            _slowTimer = 0.45f;
            if (Game.Audio != null)
            {
                Game.Audio.Play2D(SfxId.Whoosh, 0.9f, toInner ? 0.55f : 0.8f);
                Game.Audio.Play2D(SfxId.CrystalChime, 0.55f, toInner ? 0.7f : 1.15f);
            }
            Pad.Rumble(0.6f, 0.45f, 0.35f);
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t = Mathf.MoveTowards(_t, IsInner ? 1f : 0f, dt / BlendSeconds);
            Blend = _t * _t * (3f - 2f * _t);
            PostGrade.InnerWorld = Blend;
            if (Pulse > 0f) Pulse = Mathf.Max(0f, Pulse - dt * 1.8f);
            if (_slowTimer > 0f)
            {
                _slowTimer -= dt;
                if (_slowTimer <= 0f) GameTime.Release("veil");
            }
            if (IsInner) KeepInner(dt);
            UpdateVisuals(dt);
        }

        private void KeepInner(float dt)
        {
            // A mission ending switches the spawners back on; keep them paused and remember that for the return.
            if (Game.Peds != null && !Game.Peds.Suppressed)
            {
                _pedsWereSuppressed = false;
                Game.Peds.Suppressed = true;
            }
            if (Game.Traffic != null && !Game.Traffic.Suppressed)
            {
                _trafficWasSuppressed = false;
                Game.Traffic.Suppressed = true;
            }
            // Death, arrest or the title screen bring the player home.
            PlayerController p = Game.Player;
            if (Game.State == GameState.Wasted || Game.State == GameState.Busted || Game.State == GameState.Title || (p != null && p.IsDead))
            {
                ShiftInternal(false, false, false);
                return;
            }
            Abilities ab = Game.Abilities;
            if (ab == null || p == null || !Game.IsPlaying || DrainPaused) return;
            // The Veil's own share only: the Third Eye takes its drain in Abilities (DrainNow adds both for the HUD).
            ab.Energy = Mathf.Max(0f, ab.Energy - dt * DrainRate(false, SkillMods.VeilDrain, 0f));
            if (ab.Energy <= 0f) PullOut(p);
        }

        /// <summary>Out of energy: the Veil throws the player back with a stagger.</summary>
        private void PullOut(PlayerController p)
        {
            ShiftInternal(false, false, true);
            if (Game.Hud != null) Game.Hud.Notify("Your crystal energy ran dry. The Veil throws you back into the city.", 3.5f);
            if (!p.InVehicle && !p.IsDead && !p.IsRagdolled && p.Health != null)
            {
                // A zero-damage, unblockable shove: the combat system plays its flinch.
                DamageInfo info = DamageInfo.Make(0f, p.ChestPosition, -p.transform.forward, null, DamageType.Melee);
                info.Force = 7f;
                info.Flags = DamageFlags.Unblockable;
                p.Health.TakeDamage(info);
            }
            else if (Game.Cam != null) Game.Cam.Shake(0.5f);
        }

        // ------------------------------------------------------------------ gameplay queries

        /// <summary>
        /// Fills into (cleared first) with the Inner World crystal growth positions within radius of center: the same
        /// deterministic spots the visuals use, available whether or not the player is inside. Good spawn and loot points.
        /// The radius is capped at 300 m.
        /// </summary>
        public void CrystalSpotsNear(Vector3 center, float radius, List<Vector3> into)
        {
            if (into == null) return;
            into.Clear();
            if (Game.Layout == null) return;
            radius = Mathf.Clamp(radius, 0f, 300f);
            float cell = CityLayout.VeilCellSize;
            int x0 = Mathf.FloorToInt((center.x - radius) / cell), x1 = Mathf.FloorToInt((center.x + radius) / cell);
            int z0 = Mathf.FloorToInt((center.z - radius) / cell), z1 = Mathf.FloorToInt((center.z + radius) / cell);
            float r2 = radius * radius;
            for (int cx = x0; cx <= x1; cx++)
            {
                for (int cz = z0; cz <= z1; cz++)
                {
                    VeilCrystalSpot s;
                    if (!CrystalCell(cx, cz, out s)) continue;
                    if (MathUtil.FlatSqrDistance(s.Position, center) <= r2) into.Add(s.Position);
                }
            }
        }
    }
}
