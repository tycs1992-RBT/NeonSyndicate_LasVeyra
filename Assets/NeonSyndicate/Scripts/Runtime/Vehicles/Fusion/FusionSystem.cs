using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The single authoritative fusion state (Docs/Underheart04E/FUSION.md).</summary>
    public enum FusionState
    {
        Separate = 0,
        FusionRequested = 1,
        Validating = 2,
        TransitionIn = 3,
        Fused = 4,
        SeparationRequested = 5,
        TransitionOut = 6,
        Recovery = 7
    }

    /// <summary>A handling course for the quest Course step: ordered checkpoints and the ability uses it asks for.</summary>
    public sealed class FusionCourse
    {
        public string Id = "";
        public Vector3[] Points = new Vector3[0];
        public float Radius = 5f;
        public int AbilityUses;
        public string Text = "";
        /// <summary>The checkpoint where the course asks for a hard stop (-1 none).</summary>
        public int BrakeAt = -1;
        /// <summary>The checkpoint where the course suggests the form ability (-1 none).</summary>
        public int AbilityAt = -1;
    }

    /// <summary>
    /// Game.Fusion: joins the active bonded companion with the player's owned personal chassis into a fused form and
    /// back, with validation, swept clearance, rollback on any failed step and the no-refill invariants; owns the form
    /// handling, abilities and HUD. One FusionMachine holds the only state; the live world is reached through two
    /// adapters (the companion through Game.Bonds, the form geometry on the bike). Not saved as a frame: a save or load
    /// always resumes separate (the garage keeps the meters and cooldown).
    /// </summary>
    public sealed partial class FusionSystem : MonoBehaviour, IWorldSystem
    {
        public event Action<string> Fused;
        public event Action Separated;
        public event Action<string> AbilityUsed;

        private readonly FusionMachine _machine = new FusionMachine();
        private readonly HoldGesture _hold = new HoldGesture(FusionRules.HoldSeconds);
        private Vehicle _vehicle;
        private FusionFormView _view;
        private bool _announcedFused;
        private bool _firstFusion, _calibrates;
        private bool _gliding;
        private float _airTime;
        private float _tightTimer;
        private string _toast = "";
        private float _toastUntil;
        private BondsCompanionPort _companionPort;
        private FormViewPort _formPort;
        private bool _statusReported;

        public void Init()
        {
            WorldSystems.Register(this);
            _companionPort = new BondsCompanionPort();
            _formPort = new FormViewPort(this);
            HudOverlays.Register("fusion_04e", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Fusion", Order = 64, Draw = DrawDebug });
            PlayerDamageHooks.Register("fusion_rider_04e", 30, RiderDamage);
            if (Game.Inner != null) Game.Inner.Shifted += OnVeilShifted;
            ReportStatus();
        }

        private void OnDestroy()
        {
            if (Game.Inner != null) Game.Inner.Shifted -= OnVeilShifted;
            HudOverlays.Unregister("fusion_04e");
            PlayerDamageHooks.Unregister("fusion_rider_04e");
        }

        public FusionState State { get { return _machine.State; } }
        public bool IsFused { get { return State == FusionState.Fused; } }
        /// <summary>The active form id ("" while separate).</summary>
        public string ActiveForm { get { return FusionRules.Engaged(State) ? _machine.Form : ""; } }
        /// <summary>Form ability uses since the last fusion (the Course step counts them).</summary>
        public int AbilityUses { get { return _machine.AbilityUses; } }
        /// <summary>The last refusal or forced-separation reason ("" after a success).</summary>
        public string LastReason { get { return _machine.LastReason; } }
        /// <summary>Seconds before the next fusion may start.</summary>
        public float CooldownLeft { get { return _machine.Cooldown; } }
        /// <summary>The chassis carrying the form while engaged (null when separate).</summary>
        public Vehicle Chassis { get { return FusionRules.Engaged(State) ? _vehicle : null; } }
        /// <summary>The Ventwing is gliding right now.</summary>
        public bool Gliding { get { return IsFused && _gliding; } }

        /// <summary>True when this system currently has a form on that bike (the garage leaves its handling alone).</summary>
        public bool Owns(Vehicle v) { return v != null && v == _vehicle && FusionRules.Engaged(State); }

        /// <summary>The meters of a form (heat, cooldown, charges): they change with time and use only.</summary>
        public FormMeters MetersOf(string form) { return _machine.Meters(form); }

        // ================================================================== frame loop

        private void Update()
        {
            try { Tick(Time.deltaTime); }
            catch (Exception e)
            {
                Log.Exception("Fusion tick", e);
                try { Recover("The coupler faulted (" + e.Message + ")."); }
                catch (Exception e2) { Log.Exception("Fusion recovery", e2); }
            }
        }

        private void Tick(float dt)
        {
            if (!Game.IsPlaying)
            {
                _hold.Reset();
                return;
            }
            if (!_statusReported)
            {
                _statusReported = true;
                ReportStatus();
            }
            if (_bayOpen && (GInput.UiBack || GInput.Down(Keys.CompanionAssist))) CloseBayCard();
            PlayerController p = Game.Player;
            Vehicle current = p != null ? p.CurrentVehicle : null;
            if (FusionRules.Engaged(State))
            {
                string why = WatchFailure(p, current, dt);
                if (why != null) Recover(why);
            }
            bool airborne = _vehicle != null && FusionRules.Engaged(State) && _vehicle.GroundedWheels == 0;
            _airTime = airborne ? _airTime + dt : 0f;
            FusionEvent ev = _machine.Tick(dt, airborne, _gliding && IsFused);
            if (ev == FusionEvent.BecameFused) OnBecameFused();
            else if (ev == FusionEvent.BecameSeparate) OnBecameSeparate(true, "");
            if (p != null && current != null && p.Seat == 0 && !p.IsDead && current.Def != null && current.Def.IsBike && !_bayOpen) HandleInput(p, current, dt);
            else _hold.Reset();
            if (IsFused) UpdateFused(dt);
            else if (_gliding) _gliding = false;
            if (_toastUntil > 0f && Time.unscaledTime > _toastUntil) { _toast = ""; _toastUntil = 0f; }
        }

        private void LateUpdate()
        {
            // Hold the chassis still while the form unfolds or folds (set after the rider's controls for the next physics step).
            if (_vehicle != null && (State == FusionState.TransitionIn || State == FusionState.TransitionOut))
            {
                _vehicle.Throttle = 0f;
                _vehicle.SteerInput = 0f;
                _vehicle.Handbrake = true;
                _vehicle.BoostHeld = false;
            }
        }

        /// <summary>The deterministic reasons an engaged fusion must end now (null while everything holds).</summary>
        private string WatchFailure(PlayerController p, Vehicle current, float dt)
        {
            if (_vehicle == null) return "The chassis was lost.";
            if (_vehicle.IsDestroyed) return "The chassis was wrecked.";
            if (p == null || p.IsDead) return "You went down.";
            if (current != _vehicle || p.Seat != 0) return "You left the chassis.";
            if (_vehicle.OccupantCount() > 1) return "A passenger climbed on; the frame has one seat.";
            if (Game.Inner != null && Game.Inner.IsInner) return "The Veil scrambles the bond.";
            if (State == FusionState.Fused && Game.Underheart != null && WorldBounds.IsUnderground(_vehicle.transform.position))
            {
                float headroom;
                bool ok = Game.Underheart.VehicleSpaceAt(_vehicle.transform.position, out headroom);
                _tightTimer = ok ? 0f : _tightTimer + dt;
                if (_tightTimer > 1.5f) return "Too tight down here for the fused frame.";
            }
            else _tightTimer = 0f;
            return null;
        }

        private void HandleInput(PlayerController p, Vehicle v, float dt)
        {
            HoldGesture.Result r = _hold.Update(Act.CompanionDown, Act.CompanionHeld, Time.unscaledDeltaTime);
            if (r == HoldGesture.Result.Hold)
            {
                string reason;
                if (State == FusionState.Separate)
                {
                    if (!RequestFuse(out reason)) Toast(reason, true);
                }
                else if (State == FusionState.Fused)
                {
                    if (!RequestSeparate(out reason)) Toast(reason, true);
                }
            }
            else if (r == HoldGesture.Result.Tap)
            {
                // A tap explains what holding would do (the hold is the command, so a tap never fuses by accident).
                if (State == FusionState.Fused) Toast("Hold " + KeyN + " to separate. " + FusionRules.Ability(_machine.Form).Hint.Replace("U", KeyU), false);
                else if (State == FusionState.Separate)
                {
                    string reason;
                    Toast(CanFuse(out reason) ? "Hold " + KeyN + " to fuse with " + CompanionName() + "." : reason, false);
                }
            }
            if (Act.CompanionAssist)
            {
                if (IsFused) UseAbility();
                else if (State == FusionState.Separate && ParkedInBay(v)) OpenBayCard();
                else if (State == FusionState.Separate) Toast(Game.Garage != null && Game.Garage.IsPersonal(v) ? "Fuse first to use a form ability." : "Jax's bay services open when you park inside his bay.", false);
            }
        }

        // ================================================================== transitions

        private void OnBecameFused()
        {
            _announcedFused = true;
            string form = _machine.Form;
            ApplyHandling(0f);
            if (_view != null)
            {
                _view.Show(1f);
                _view.StartHum();
            }
            PersonalGarage g = Game.Garage;
            if (g != null)
            {
                if (_calibrates) g.UnlockForm(form, "calibrated at Jax's bay");
                if (form == FusionIds.Ventwing && !FusionRules.IsProfile(g.Profile)) g.SetProfile(g.ActiveProfile);
            }
            Underheart04ESounds.Play(UhSfx.FuseLock, _vehicle != null ? _vehicle.transform.position : Game.PlayerPosition, 1f, 1f);
            if (Game.Cam != null) Game.Cam.Shake(0.12f);
            string title = FusionIds.Title(form).ToUpperInvariant();
            string hint = FusionRules.Ability(form).Hint.Replace("U", KeyU) + " Hold " + KeyN + " to separate.";
            if (_firstFusion) hint = "First fusion with " + CompanionName() + ". " + hint;
            else if (_calibrates) hint = FusionIds.Title(form) + " calibrated. " + hint;
            if (Game.Hud != null) Game.Hud.Banner(title, hint, FormColor(form), 5f);
            Log.Info("FUSION_04E fused form=" + form + " chassis=" + (_vehicle != null ? _vehicle.ChassisUid : 0) + " companion=" + _machine.CompanionUid + (_firstFusion ? " first" : "") + (_calibrates ? " calibrated" : ""));
            RaiseFused(form);
            ReportStatus();
        }

        private void OnBecameSeparate(bool voluntary, string reason)
        {
            Vehicle v = _vehicle;
            _vehicle = null;
            _view = null;
            _gliding = false;
            if (v != null && !v.IsDestroyed)
            {
                ChassisRecord r = Game.Garage != null ? Game.Garage.RecordOf(v) : null;
                v.FusionHandling = FusionRules.TuneHandling(r != null ? r.Tune : "");
            }
            else if (v != null) v.FusionHandling = FusedHandling.None;
            bool announce = _announcedFused;
            _announcedFused = false;
            if (voluntary)
            {
                Underheart04ESounds.Play(UhSfx.SeparateDone, v != null ? v.transform.position : Game.PlayerPosition, 1f, 1f);
                Toast(CompanionName() + " steps off. The coupler cools for " + Mathf.CeilToInt(FusionRules.CooldownAfterSeparate) + " s.", false);
            }
            else if (!string.IsNullOrEmpty(reason)) Toast(CompanionName() + " separated: " + reason, true);
            Log.Info("FUSION_04E separated " + (voluntary ? "voluntary" : "recovery (" + reason + ")"));
            if (announce) RaiseSeparated();
            ReportStatus();
        }

        /// <summary>
        /// Ends an engaged fusion now and always lands Separate: the form is removed, the companion comes back at the
        /// best verified spot near the chassis (or near the rider) with exactly the state it had when it fused.
        /// </summary>
        private void Recover(string reason)
        {
            if (!FusionRules.Engaged(State)) return;
            _companionPort.PrepareSpot(FindCompanionSpot(_vehicle != null && !_vehicle.IsDestroyed ? _vehicle.transform : null));
            bool fused = _announcedFused;
            _machine.Recover(reason);
            OnBecameSeparate(false, reason);
            if (!fused) Toast("Fusion cancelled: " + reason, true);
        }

        /// <summary>
        /// A forced, safe separation for scripted moments (the Kiln Regent's resonance disruption, a scene that needs the
        /// rider on foot): the same deterministic recovery as a crash. False when not fused or transitioning.
        /// </summary>
        public bool ForceSeparate(string reason)
        {
            if (!FusionRules.Engaged(State)) return false;
            Recover(string.IsNullOrEmpty(reason) ? "The resonance broke." : reason);
            return true;
        }

        private void OnVeilShifted(bool toInner)
        {
            if (toInner && FusionRules.Engaged(State)) Recover("The Veil scrambles the bond.");
        }

        // ================================================================== world system (session state only)

        /// <summary>New game, load and title: drop the live fusion without touching the destroyed world (always separate).</summary>
        public void ResetAll()
        {
            if (FusionRules.Engaged(State) && Game.Bonds != null)
            {
                try { Game.Bonds.SetFused(_machine.CompanionUid, false); }
                catch (Exception e) { Log.Exception("Fusion reset companion", e); }
            }
            if (_view != null) Destroy(_view.gameObject);
            if (_vehicle != null) _vehicle.FusionHandling = FusedHandling.None;
            _machine.ResetSession();
            _vehicle = null;
            _view = null;
            _announcedFused = false;
            _gliding = false;
            _hold.Reset();
            _toast = "";
            _toastUntil = 0f;
            // The session is being rebuilt behind a fade: drop the card without touching the input block GameRoot owns.
            _bayOpen = false;
            _statusReported = false;
            ReportStatus();
        }

        /// <summary>The garage saves the meters (no fused frame is ever saved).</summary>
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

        /// <summary>Writes every form's meters and the fusion cooldown into the garage block.</summary>
        public void CaptureMeters(GarageSave s)
        {
            if (s == null) return;
            s.Meters.Clear();
            foreach (System.Collections.Generic.KeyValuePair<string, FormMeters> kv in _machine.AllMeters)
                s.Meters.Add(new FormMeterSave { Form = kv.Key, Heat = kv.Value.Heat, Cooldown = kv.Value.Cooldown, Charges = kv.Value.Charges, Recharge = kv.Value.Recharge, Overheated = kv.Value.Overheated });
            s.FusionCooldown = _machine.Cooldown;
        }

        /// <summary>Restores the meters and cooldown from a loaded garage block.</summary>
        public void RestoreMeters(GarageSave s)
        {
            if (s == null) return;
            foreach (FormMeterSave m in s.Meters)
            {
                if (m == null || !FusionIds.Has(m.Form)) continue;
                _machine.SetMeters(m.Form, new FormMeters { Heat = m.Heat, Cooldown = m.Cooldown, Charges = m.Charges, Recharge = m.Recharge, Overheated = m.Overheated });
            }
            _machine.RestoreCooldown(s.FusionCooldown);
        }

        // ================================================================== helpers

        private void Toast(string text, bool denied)
        {
            if (string.IsNullOrEmpty(text)) return;
            _toast = text;
            _toastUntil = Time.unscaledTime + 4f;
            if (denied) Underheart04ESounds.Play2D(UhSfx.FuseDenied, 0.8f, 1f);
        }

        private string CompanionName()
        {
            CompanionRecord c = Game.Bonds != null ? Game.Bonds.Active : null;
            if (c == null) return "your companion";
            return string.IsNullOrEmpty(c.Name) ? BondIds.DefaultName(c.Archetype) : c.Name;
        }

        /// <summary>The key or chord names for the fuse and ability commands on the device in use.</summary>
        private static string KeyN { get { return Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.LB) + " + " + Pad.Tag(PadButton.DDown) : "N"; } }
        private static string KeyU { get { return Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.LB) + " + " + Pad.Tag(PadButton.DUp) : "U"; } }

        public static Color FormColor(string form)
        {
            switch (form)
            {
                case FusionIds.Ventwing: return new Color(0.55f, 0.92f, 1f);
                case FusionIds.Kilnback: return new Color(1f, 0.55f, 0.2f);
                case FusionIds.Coilrunner: return new Color(0.7f, 0.62f, 1f);
                default: return UIKit.Cyan;
            }
        }

        /// <summary>Reports the real fusion availability to the STATUS page (what works now and why not).</summary>
        public void ReportStatus()
        {
            PersonalGarage g = Game.Garage;
            string bay;
            Vector3 c; float r;
            bool hasBay = TryBay(out c, out r);
            bay = hasBay ? (_baySource == "anchor" ? "Jax's bay from the garage layout anchor." : (_baySource == "frame" ? "Jax's bay computed from the garage venue frame (the layout has no bay anchor in this build)." : "Jax's bay placed outside the garage door (the walk-in garage did not load).")) : "Jax's bay unavailable (no garage).";
            if (g == null)
            {
                ModuleStatus.Report("fusion", "Creature-vehicle fusion", ModuleState.Disabled, "The personal garage did not load, so no chassis can couple. " + bay, "Restart the session; the rest of the game is unaffected.");
                return;
            }
            if (Game.Bonds == null)
            {
                ModuleStatus.Report("fusion", "Creature-vehicle fusion", ModuleState.Partial, "Bonded companions did not load; fusion needs an Attuned companion. " + bay, "Bonds must load for fusion.");
                return;
            }
            string state = IsFused ? "Fused as " + FusionIds.Title(_machine.Form) + "." : (g.FusionUnlocked ? "Fusion unlocked." : "Fusion locked until Jax's first fusion (Two Hearts, One Chassis).");
            ModuleStatus.Report("fusion", "Creature-vehicle fusion", g.FusionUnlocked || IsFused ? ModuleState.Loaded : ModuleState.Partial, state + " Forms built: Ventwing, Kilnback, Coilrunner (3 forms x 3 bike classes). " + bay,
                g.FusionUnlocked ? "" : "Bond and attune a companion, then visit Jax Calder's garage.");
            int unlocked = 0;
            foreach (string f in FusionIds.Forms) if (g.FormUnlocked(f)) unlocked++;
            ModuleStatus.SetCount("fusion", unlocked, "forms calibrated");
        }

        private void RaiseFused(string form)
        {
            if (Fused == null) return;
            foreach (Delegate h in Fused.GetInvocationList())
            {
                try { ((Action<string>)h)(form); }
                catch (Exception e) { Log.Exception("Fused listener", e); }
            }
        }

        private void RaiseSeparated()
        {
            if (Separated == null) return;
            foreach (Delegate h in Separated.GetInvocationList())
            {
                try { ((Action)h)(); }
                catch (Exception e) { Log.Exception("Separated listener", e); }
            }
        }

        private void RaiseAbilityUsed(string form)
        {
            if (AbilityUsed == null) return;
            foreach (Delegate h in AbilityUsed.GetInvocationList())
            {
                try { ((Action<string>)h)(form); }
                catch (Exception e) { Log.Exception("AbilityUsed listener", e); }
            }
        }
    }

    /// <summary>Reset hub for every static the FUSION work stream adds (garage and fusion).</summary>
    public static class FusionStatics
    {
        public static void ResetStatics()
        {
            FusionArt.ResetStatics();
            FusionSystem.ResetStaticLists();
        }
    }
}
