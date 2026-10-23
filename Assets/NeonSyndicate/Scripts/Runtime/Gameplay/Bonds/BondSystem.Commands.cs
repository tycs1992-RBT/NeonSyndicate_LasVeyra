using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The on-foot companion commands: tap N toggles follow and wait, hold N (0.6 s) recalls the companion to your
    /// side, U asks for its assist (a combat assist on a valid aimed or locked hostile, otherwise the exploration
    /// assist). The pad uses LB + D-pad Down and LB + D-pad Up. In a vehicle these buttons belong to fusion, so the
    /// bond system does not read them there. Every refusal says why and plays the unavailable cue.
    /// </summary>
    public sealed partial class BondSystem
    {
        private readonly HoldGesture _hold = new HoldGesture(0.6f);
        /// <summary>H8S: tap, hold and the optional double tap of the companion button turned into commands.</summary>
        private readonly CompanionCallGestureH8S _call = new CompanionCallGestureH8S();

        private struct Message
        {
            public string Text;
            public float Left;
            public bool Warn;
            /// <summary>H8S: a companion bark (subtitle of its call), drawn larger with the text size setting when it fits.</summary>
            public bool Bark;
            /// <summary>Measured widths (base and scaled font), filled once on the first GUI pass that draws the line.</summary>
            public bool Measured;
            public float Width;
            public float WidthScaled;
            public int ScaledFont;
        }

        private readonly List<Message> _messages = new List<Message>();

        /// <summary>Hold-N progress for the HUD (0 when not holding).</summary>
        public float HoldProgress { get { return _hold.Progress; } }

        private void ReadInput()
        {
            PlayerController pl = Game.Player;
            bool can = pl != null && !pl.IsDead && !pl.InVehicle && !GInput.GameplayBlocked && (_naming == null || !_naming.IsOpen);
            if (!can)
            {
                _hold.Reset();
                _call.Reset();
                return;
            }
            HoldGesture.Result g = _hold.Update(Act.CompanionDown, Act.CompanionHeld, Time.unscaledDeltaTime);
            CompanionCallGestureH8S.Command command = _call.Update(g, Settings.CompanionDoubleTapCallH8S, Time.unscaledDeltaTime);
            if (command == CompanionCallGestureH8S.Command.Toggle) ToggleFollowWait();
            else if (command == CompanionCallGestureH8S.Command.Recall) ManualRecall();
            if (Act.CompanionAssist)
            {if(_synergyRunning04N)_synergyPress04N=true;else RequestAssist();}
        }

        private void ToggleFollowWait()
        {
            CompanionRecord r = Active;
            if (r == null)
            {
                Refuse(_rescue != null ? AssistRefusal.Rescue : AssistRefusal.NoCompanion, null, 0f);
                return;
            }
            switch ((CompanionState)r.State)
            {
                case CompanionState.Downed: Refuse(AssistRefusal.Downed, r, 0f); return;
                case CompanionState.Resting: Refuse(AssistRefusal.Resting, r, 0f); return;
                case CompanionState.Fused: Refuse(AssistRefusal.Fused, r, 0f); return;
                case CompanionState.Assisting: Refuse(AssistRefusal.Busy, r, 0f); return;
            }
            if (_body == null)
            {
                Say(NameOf(r) + " is on its way.", 2f, false);
                return;
            }
            if (r.State == (int)CompanionState.Waiting)
            {
                r.State = (int)CompanionState.Following;
                _body.Mode = CompanionMode.Follow;
                _body.ClearBlock();
                Underheart04ESounds.Play(UhSfx.CompanionFollow, _body.ChestPosition, 0.8f, 1f);
                Say(NameOf(r) + " follows you.", 2f, false);
                NoteCommandH8S(false);
            }
            else
            {
                r.State = (int)CompanionState.Waiting;
                _body.WaitPoint = _body.transform.position;
                _body.Mode = CompanionMode.Wait;
                Underheart04ESounds.Play(UhSfx.CompanionWait, _body.ChestPosition, 0.8f, 1f);
                Say(NameOf(r) + " waits here. Tap N to call it on, hold N to call it to your side.", 3f, false);
                NoteCommandH8S(false);
            }
            ReportStatus();
        }

        private void ManualRecall()
        {
            CompanionRecord r = Active;
            if (r == null)
            {
                Refuse(AssistRefusal.NoCompanion, null, 0f);
                return;
            }
            switch ((CompanionState)r.State)
            {
                case CompanionState.Downed: Refuse(AssistRefusal.Downed, r, 0f); return;
                case CompanionState.Resting: Refuse(AssistRefusal.Resting, r, 0f); return;
                case CompanionState.Fused: Refuse(AssistRefusal.Fused, r, 0f); return;
            }
            if (_body == null)
            {
                Say(NameOf(r) + " is on its way.", 2f, false);
                return;
            }
            if (_body.RecallCooldownLeft > 0f)
            {
                Refuse(AssistRefusal.Cooldown, r, _body.RecallCooldownLeft);
                return;
            }
            EndAssist();
            EndBrace(false);
            string why;
            if (_body.TryRecall(true, out why))
            {
                r.State = (int)CompanionState.Following;
                _body.Mode = CompanionMode.Follow;
                Say(NameOf(r) + " comes to your side.", 2f, false);
                NoteCommandH8S(false);
                Bark(CompanionBarkH8S.Regrouping);
                ReportStatus();
            }
            else
            {
                PlayRefuse(true);
                Say(NameOf(r) + " cannot reach you here: " + why, 4f, true);
            }
        }

        private void Refuse(AssistRefusal why, CompanionRecord r, float cooldown)
        {
            string text = BondAssistRules.RefusalText(why, r != null ? NameOf(r) : null, cooldown);
            PlayRefuse(r != null);
            NoteCommandH8S(true);
            if (r == null && Game.Hud != null) Game.Hud.Notify(text, 3f);
            else Say(text, 3.5f, true);
        }

        /// <summary>
        /// The refusal cue: the companion's own refusal chirp (from its body when one is out, else in the ear), or the
        /// plain menu denial when there is no companion to refuse.
        /// </summary>
        private void PlayRefuse(bool fromCompanion)
        {
            if (!fromCompanion) Underheart04ESounds.Play2D(UhSfx.UiDenied, 0.55f, 1f);
            else if (_body != null && !_body.Hidden) Underheart04ESounds.Play(UhSfx.CompanionRefuse, _body.ChestPosition, 0.8f, 1f);
            else Underheart04ESounds.Play2D(UhSfx.CompanionRefuse, 0.6f, 1f);
        }

        // ================================================================== messages (HUD panel line)

        /// <summary>Shows a line on the companion panel for a few seconds (the newest replaces an older one of the same text).</summary>
        public void Say(string text, float seconds, bool warn)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int i = _messages.Count - 1; i >= 0; i--) if (_messages[i].Text == text) _messages.RemoveAt(i);
            _messages.Add(new Message { Text = text, Left = Mathf.Max(0.5f, seconds), Warn = warn });
            while (_messages.Count > 2) _messages.RemoveAt(0);
            if (Active == null && Game.Hud != null && _rescue == null) Game.Hud.Notify(text, seconds);
        }

        private void TickMessages(float udt)
        {
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                Message m = _messages[i];
                m.Left -= udt;
                if (m.Left <= 0f) _messages.RemoveAt(i);
                else _messages[i] = m;
            }
        }
    }
}
