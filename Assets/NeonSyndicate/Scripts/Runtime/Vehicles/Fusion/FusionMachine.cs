using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>The companion side of a fusion (the live adapter calls Game.Bonds; tests use a fake).</summary>
    public interface IFusionCompanionPort
    {
        CompanionSnapshot Snapshot(int uid);
        /// <summary>Hides (true) or restores (false) the free companion body through its own lifecycle.</summary>
        void SetFused(int uid, bool fused);
        void Restore(CompanionSnapshot snapshot);
    }

    /// <summary>The fused representation on the chassis (the live adapter builds real geometry; tests use a fake).</summary>
    public interface IFusionFormPort
    {
        /// <summary>Builds the new representation hidden and checks it fits. False (with a reason) leaves nothing behind
        /// once Destroy runs.</summary>
        bool Build(string form, out string error);
        /// <summary>Shows the built representation at an unfold progress (0 folded and hidden, 1 complete).</summary>
        void Show(float unfold01);
        /// <summary>Removes the representation (safe to call when nothing was built).</summary>
        void Destroy();
    }

    /// <summary>What a machine tick changed.</summary>
    public enum FusionEvent
    {
        None = 0,
        BecameFused = 1,
        BecameSeparate = 2
    }

    /// <summary>
    /// The single authoritative fusion state with its commit, rollback and recovery rules (engine-free). The new
    /// representation is built before anything changes; the companion is snapshotted and hidden only after the build
    /// succeeded; any failure before Fused rolls back without spending the cooldown; a forced recovery always ends in
    /// Separate with the companion restored from its snapshot. Form meters and the fusion cooldown change only with
    /// time and use: fusing and separating never refill them.
    /// </summary>
    public sealed class FusionMachine
    {
        private readonly Dictionary<string, FormMeters> _meters = new Dictionary<string, FormMeters>();

        public FusionState State { get; private set; }
        /// <summary>The form while engaged ("" when separate).</summary>
        public string Form { get; private set; } = "";
        public int CompanionUid { get; private set; }
        /// <summary>The companion's state at the moment it was hidden, returned exactly on separation.</summary>
        public CompanionSnapshot Carry { get; private set; }
        public bool HasCarry { get; private set; }
        /// <summary>Seconds before the next fusion may start.</summary>
        public float Cooldown { get; private set; }
        /// <summary>Seconds into the current transition.</summary>
        public float Phase { get; private set; }
        /// <summary>Form ability uses since the last successful fusion.</summary>
        public int AbilityUses { get; private set; }
        public string LastReason { get; private set; } = "";
        /// <summary>Every state the machine has entered, oldest first (tests read it; capped).</summary>
        public readonly List<FusionState> Trail = new List<FusionState>();

        private IFusionFormPort _form;
        private IFusionCompanionPort _companion;
        private bool _companionHidden;

        public FusionMachine()
        {
            State = FusionState.Separate;
        }

        public bool IsFused { get { return State == FusionState.Fused; } }

        private void Enter(FusionState next)
        {
            if (!FusionRules.IsLegal(State, next)) throw new InvalidOperationException("Illegal fusion transition " + State + " -> " + next);
            State = next;
            Trail.Add(next);
            if (Trail.Count > 64) Trail.RemoveAt(0);
        }

        // ------------------------------------------------------------------ meters

        public FormMeters Meters(string form)
        {
            FormMeters m;
            if (_meters.TryGetValue(form ?? "", out m)) return m;
            return FormMeters.Fresh(FusionRules.Ability(form));
        }

        public void SetMeters(string form, FormMeters m)
        {
            if (string.IsNullOrEmpty(form)) return;
            _meters[form] = m;
        }

        public IEnumerable<KeyValuePair<string, FormMeters>> AllMeters { get { return _meters; } }

        /// <summary>Restores durable cooldown state from a save (never more generous than what was saved).</summary>
        public void RestoreCooldown(float seconds)
        {
            Cooldown = Math.Max(0f, Math.Min(FusionRules.CooldownAfterRecovery, seconds));
        }

        /// <summary>Spends one use of the active form's ability. False when it can't fire (meters unchanged).</summary>
        public bool TryUseAbility(out string reason)
        {
            reason = "";
            if (State != FusionState.Fused) { reason = "You are not fused."; return false; }
            FormAbilitySpec spec = FusionRules.Ability(Form);
            FormMeters m = Meters(Form);
            reason = FusionRules.WhyNotUse(m, spec);
            if (reason.Length > 0) return false;
            FusionRules.TryUse(ref m, spec);
            SetMeters(Form, m);
            AbilityUses++;
            return true;
        }

        // ------------------------------------------------------------------ fusion

        /// <summary>
        /// Starts a fusion from a validation verdict. Builds the representation first; only then snapshots and hides the
        /// companion. Any failure restores the old state and costs nothing. True when the transition began.
        /// </summary>
        public bool RequestFuse(FusionVerdict verdict, int companionUid, IFusionFormPort form, IFusionCompanionPort companion, out string reason)
        {
            reason = "";
            if (State != FusionState.Separate)
            {
                reason = State == FusionState.Fused ? "You are already fused." : "The coupler is still settling.";
                LastReason = reason;
                return false;
            }
            Enter(FusionState.FusionRequested);
            Enter(FusionState.Validating);
            if (!verdict.Ok)
            {
                reason = verdict.Message;
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            if (Cooldown > 0.01f)
            {
                reason = "The coupler is cooling down.";
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            if (form == null || companion == null)
            {
                reason = "Fusion is unavailable right now.";
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            string error = "";
            bool built;
            try { built = form.Build(verdict.Form, out error); }
            catch (Exception e)
            {
                built = false;
                error = "The form could not be built (" + e.Message + ").";
            }
            if (!built)
            {
                SafeDestroy(form);
                reason = string.IsNullOrEmpty(error) ? "The form could not be built." : error;
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            CompanionSnapshot snap;
            try { snap = companion.Snapshot(companionUid); }
            catch (Exception e)
            {
                SafeDestroy(form);
                reason = "Your companion could not be read (" + e.Message + ").";
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            try
            {
                companion.SetFused(companionUid, true);
            }
            catch (Exception e)
            {
                // Roll back: the body may or may not have been hidden; restore it either way.
                SafeDestroy(form);
                try { companion.SetFused(companionUid, false); } catch (Exception) { }
                try { companion.Restore(snap); } catch (Exception) { }
                reason = "Your companion could not join the chassis (" + e.Message + ").";
                Enter(FusionState.Separate);
                LastReason = reason;
                return false;
            }
            _form = form;
            _companion = companion;
            _companionHidden = true;
            Carry = snap;
            HasCarry = true;
            CompanionUid = companionUid;
            Form = verdict.Form;
            Phase = 0f;
            AbilityUses = 0;
            Enter(FusionState.TransitionIn);
            SafeShow(0f);
            LastReason = "";
            return true;
        }

        /// <summary>Starts a voluntary separation once the runtime has a verified spot for the companion.</summary>
        public bool RequestSeparate(FusionVerdict verdict, out string reason)
        {
            reason = "";
            if (State != FusionState.Fused)
            {
                reason = State == FusionState.Separate ? "You are not fused." : "The coupler is still settling.";
                LastReason = reason;
                return false;
            }
            Enter(FusionState.SeparationRequested);
            if (!verdict.Ok)
            {
                reason = verdict.Message;
                Enter(FusionState.Fused);
                LastReason = reason;
                return false;
            }
            Phase = 0f;
            Enter(FusionState.TransitionOut);
            LastReason = "";
            return true;
        }

        /// <summary>
        /// Advances transitions and meters by time. Meters of every form cool and recharge whether fused or not, so a
        /// toggle can never refill them faster than time does.
        /// </summary>
        public FusionEvent Tick(float dt, bool airborne, bool sustaining)
        {
            if (dt < 0f) dt = 0f;
            Cooldown = Math.Max(0f, Cooldown - dt);
            List<string> keys = new List<string>(_meters.Keys);
            foreach (string k in keys)
                _meters[k] = FusionRules.Step(_meters[k], FusionRules.Ability(k), dt, airborne && k == Form, sustaining && k == Form && State == FusionState.Fused);
            switch (State)
            {
                case FusionState.TransitionIn:
                    Phase += dt;
                    SafeShow(Math.Min(1f, Phase / FusionRules.TransitionInSeconds));
                    if (Phase >= FusionRules.TransitionInSeconds)
                    {
                        Enter(FusionState.Fused);
                        if (!_meters.ContainsKey(Form)) _meters[Form] = FormMeters.Fresh(FusionRules.Ability(Form));
                        return FusionEvent.BecameFused;
                    }
                    break;
                case FusionState.TransitionOut:
                    Phase += dt;
                    SafeShow(Math.Max(0f, 1f - Phase / FusionRules.TransitionOutSeconds));
                    if (Phase >= FusionRules.TransitionOutSeconds)
                    {
                        Finish(FusionState.Separate, FusionRules.CooldownAfterSeparate);
                        return FusionEvent.BecameSeparate;
                    }
                    break;
            }
            return FusionEvent.None;
        }

        /// <summary>
        /// A failure while the form was still unfolding (the rider bailed out, the chassis was lost): roll back to the
        /// old state with the companion restored and no cooldown spent.
        /// </summary>
        public bool Abort(string reason)
        {
            if (State != FusionState.TransitionIn) return false;
            LastReason = reason ?? "";
            Enter(FusionState.Recovery);
            Finish(FusionState.Separate, 0f);
            return true;
        }

        /// <summary>
        /// A forced, deterministic end of an engaged fusion (crash, death, dismount, chassis lost, boss disruption,
        /// load): always ends Separate with the companion restored from its snapshot. A fusion that never finished
        /// unfolding costs nothing; otherwise the recovery cooldown applies. False when already separate.
        /// </summary>
        public bool Recover(string reason)
        {
            if (State == FusionState.TransitionIn) return Abort(reason);
            if (!FusionRules.Engaged(State)) return false;
            LastReason = reason ?? "";
            Enter(FusionState.Recovery);
            Finish(FusionState.Separate, FusionRules.CooldownAfterRecovery);
            return true;
        }

        /// <summary>Drops every live reference without calling the ports (a session reset destroys the world first).</summary>
        public void ResetSession()
        {
            State = FusionState.Separate;
            Form = "";
            CompanionUid = 0;
            HasCarry = false;
            Carry = default(CompanionSnapshot);
            Cooldown = 0f;
            Phase = 0f;
            AbilityUses = 0;
            LastReason = "";
            _form = null;
            _companion = null;
            _companionHidden = false;
            _meters.Clear();
            Trail.Clear();
        }

        private void Finish(FusionState end, float cooldown)
        {
            SafeDestroy(_form);
            if (_companion != null && _companionHidden)
            {
                try { _companion.SetFused(CompanionUid, false); }
                catch (Exception) { }
                if (HasCarry)
                {
                    try { _companion.Restore(Carry); }
                    catch (Exception) { }
                }
            }
            _companionHidden = false;
            _form = null;
            _companion = null;
            HasCarry = false;
            Form = "";
            Phase = 0f;
            Cooldown = Math.Max(Cooldown, cooldown);
            Enter(end);
        }

        private void SafeShow(float unfold)
        {
            if (_form == null) return;
            try { _form.Show(unfold); }
            catch (Exception) { }
        }

        private static void SafeDestroy(IFusionFormPort form)
        {
            if (form == null) return;
            try { form.Destroy(); }
            catch (Exception) { }
        }
    }
}
