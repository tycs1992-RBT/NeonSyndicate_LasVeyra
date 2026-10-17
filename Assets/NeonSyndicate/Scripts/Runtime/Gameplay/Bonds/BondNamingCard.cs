using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The naming card after a rescue: a short pausing card where the player types a name with the keyboard or keeps
    /// the name the creature already answers to (a pad keeps the default with A; Esc or B also keeps it). It waits
    /// until no other menu, phone or card is open, pauses like a menu and restores the game exactly on close. Also used
    /// to rename a companion from the BONDS page.
    /// </summary>
    public sealed class BondNamingCard : MonoBehaviour
    {
        private string _title = "";
        private string _body = "";
        private string _default = "";
        private string _text = "";
        private Action<string> _done;
        private bool _pending;
        private bool _open;
        private bool _focusSet;
        private bool _wasPaused;
        private GameState _stateBefore;
        private GUIStyle _field;

        public bool IsOpen { get { return _open; } }
        public bool IsPending { get { return _pending || _open; } }

        /// <summary>Queues the card (it opens as soon as nothing else owns the screen). done gets the chosen name.</summary>
        public void Request(string title, string body, string defaultName, string current, Action<string> done)
        {
            _title = title ?? "";
            _body = body ?? "";
            _default = string.IsNullOrEmpty(defaultName) ? "Friend" : defaultName;
            _text = current ?? "";
            _done = done;
            _pending = true;
        }

        /// <summary>Drops a queued or open card without a choice (the default is kept by the caller).</summary>
        public void Cancel()
        {
            if (_open) Close();
            _pending = false;
            _done = null;
        }

        private void Update()
        {
            if (_open)
            {
                if (Act.Pause || (Pad.Connected && Pad.Down(PadButton.East))) Finish(_default);
                return;
            }
            if (!_pending) return;
            if (Game.State != GameState.Playing) return;
            if (Game.Menus != null && (Game.Menus.IsOpen || Game.Menus.TitleOpen)) return;
            if (Game.Phone != null && Game.Phone.IsOpen) return;
            Open();
        }

        private void Open()
        {
            _pending = false;
            _open = true;
            _focusSet = false;
            _stateBefore = Game.State;
            _wasPaused = GameTime.Paused;
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            GameTime.SetPaused(true);
            if (Game.State == GameState.Playing) Game.State = GameState.Paused;
            Underheart04ESounds.Play2D(UhSfx.UiCard, 0.6f, 1f);
        }

        private void Close()
        {
            _open = false;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            if (!_wasPaused) GameTime.SetPaused(false);
            if (Game.State == GameState.Paused && _stateBefore == GameState.Playing) Game.State = GameState.Playing;
            if (Game.State == GameState.Playing) GInput.SetCursorLocked(true);
        }

        private void Finish(string name)
        {
            Action<string> cb = _done;
            _done = null;
            string final = BondRules.SanitizeName(name, _default);
            Close();
            Underheart04ESounds.Play2D(UhSfx.UiAccept, 0.7f, 1f);
            if (cb == null) return;
            try { cb(final); }
            catch (Exception e) { Log.Exception("Bond naming", e); }
        }

        private void OnGUI()
        {
            if (!_open) return;
            GUI.depth = -20;
            UIKit.BeginScaledMenu();
            float W = UIKit.Width, H = UIKit.Height;
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.55f));
            Rect r = new Rect(W * 0.5f - 460f, H * 0.5f - 210f, 920f, 420f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), UIKit.Cyan);
            UIKit.Text(new Rect(r.x + 36f, r.y + 24f, r.width - 72f, 44f), _title, 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 36f, r.y + 74f, r.width - 72f, 60f), _body, 20, new Color(0.88f, 0.9f, 0.94f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            Rect field = new Rect(r.x + 36f, r.y + 150f, r.width - 72f, 58f);
            bool padMode = Pad.Connected && Pad.LastUsed;
            if (_field == null)
            {
                _field = new GUIStyle(GUI.skin.textField);
                if (Mats.UIFont != null) _field.font = Mats.UIFont;
                _field.fontSize = 30;
                _field.alignment = TextAnchor.MiddleLeft;
                _field.padding = new RectOffset(14, 14, 6, 6);
            }
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                e.Use();
                Finish(string.IsNullOrEmpty(BondRules.SanitizeName(_text, "")) ? _default : _text);
                return;
            }
            GUI.SetNextControlName("bond_name_field");
            _text = GUI.TextField(field, _text ?? "", BondRules.MaxNameLength, _field);
            if (!_focusSet && !padMode)
            {
                GUI.FocusControl("bond_name_field");
                _focusSet = true;
            }
            string clean = BondRules.SanitizeName(_text, "");
            UIKit.Text(new Rect(r.x + 36f, field.yMax + 8f, r.width - 72f, 26f),
                padMode ? "Type a name with a keyboard, or press A to keep " + _default + "." : "Type a name (up to " + BondRules.MaxNameLength + " letters) and press Enter, or keep " + _default + ".",
                16, new Color(0.7f, 0.75f, 0.8f), TextAnchor.UpperLeft);
            float by = r.yMax - 92f;
            if (ProgressionUI.Button(new Rect(r.x + 36f, by, 410f, 58f), "Keep " + _default, padMode, 24, true))
            {
                Finish(_default);
                return;
            }
            bool can = clean.Length > 0;
            if (ProgressionUI.Button(new Rect(r.xMax - 446f, by, 410f, 58f), can ? "Name it " + clean : "Type a name first", false, 24, can))
            {
                if (can) Finish(clean);
            }
        }

        private void OnDestroy()
        {
            if (_open) Close();
        }
    }
}
