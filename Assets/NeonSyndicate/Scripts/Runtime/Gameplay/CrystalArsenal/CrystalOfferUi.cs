using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal offer and claim screen (owner: arsenal). Opens at the resonance case (offers, pending rewards and the
    /// contract board), from a reward card or from a boss trophy (that reward first). Like the Forge it pauses the game
    /// and takes the input: mouse, arrow keys or the controller (D-pad or stick to move, A to choose, B or Start to
    /// close). Every choice is explicit: Equip to a free slot, Equip replacing a chosen slot (refused when the replaced
    /// gun would have nowhere to go, and never for a favourite), Send to backpack, Not now. Nothing is replaced silently.
    /// </summary>
    public sealed partial class CrystalOfferUi : MonoBehaviour
    {
        public enum Mode { Case, Claim }

        private const string BoardKey = "#board";

        private bool _open;
        private bool _closeRequested;
        private Mode _mode;
        private string _sel;
        private string _status = "";
        private float _statusUntil;
        private bool _statusGood;
        private readonly List<string> _rows = new List<string>();

        public bool IsOpen { get { return _open; } }
        /// <summary>A contract chosen on the board, started after the screen closes (it pauses the game while open).</summary>
        private string _pendingContract;

        // ------------------------------------------------------------------ open / close

        public void Open(Mode mode, string token)
        {
            if (_open) return;
            if (Game.Menus != null && (Game.Menus.IsOpen || Game.Menus.TitleOpen)) return;
            if (Game.Crafting != null && Game.Crafting.ScreenOpen) return;
            if (Game.State != GameState.Playing) return;
            _open = true;
            _closeRequested = false;
            _mode = mode;
            _sel = token;
            _status = "";
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            GameTime.SetPaused(true);
            Game.State = GameState.Paused;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneOpen, 0.5f);
            KeepStarterWhenFull();
        }

        /// <summary>
        /// Opening the case with every loot slot and the backpack full turns the starter into one pending claim at once,
        /// so the offer can never be lost; the player claims it later in ARSENAL, at the case or at the Forge.
        /// </summary>
        private void KeepStarterWhenFull()
        {
            CrystalArsenal a = Game.Arsenal;
            if (a == null || _mode != Mode.Case) return;
            string token = CrystalEntitlementRules.StarterToken;
            string reason;
            if (!a.OfferOpen(token, out reason)) return;
            if (!CrystalOfferRules.AllFull(a.CapacityFor(token))) return;
            string msg;
            if (a.Claim(token, CrystalClaimAction.KeepPending, -1, out msg))
            {
                _sel = token;
                Status("Your loot slots and backpack are full, so Shardwake is kept as a pending claim. Free a slot, then claim it here, in ARSENAL or at the Forge.", true);
            }
        }

        /// <summary>Closes without touching input or time (new game, load, title: the session code resets those).</summary>
        public void CloseSilently()
        {
            _open = false;
            _closeRequested = false;
            _pendingContract = null;
        }

        private void DoClose()
        {
            _open = false;
            _closeRequested = false;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            GameTime.SetPaused(false);
            if (Game.State == GameState.Paused) Game.State = GameState.Playing;
            if (Game.State == GameState.Playing) GInput.SetCursorLocked(true);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.45f);
            StartPendingContract();
        }

        /// <summary>A contract chosen on the board starts once the screen has closed and the game is playing again.</summary>
        private void StartPendingContract()
        {
            string id = _pendingContract;
            _pendingContract = null;
            if (string.IsNullOrEmpty(id) || Game.Encounters == null) return;
            string why;
            bool ok = false;
            try { ok = Game.Encounters.StartContract(id, out why); }
            catch (Exception e)
            {
                Log.Exception("Contract start", e);
                why = "The contract could not start.";
            }
            if (Game.Hud != null) Game.Hud.Notify(ok ? "Resonance Breach started. Follow the marker." : (string.IsNullOrEmpty(why) ? "The contract could not start." : why), 4f);
        }

        private void Status(string s, bool good)
        {
            _status = s ?? "";
            _statusGood = good;
            _statusUntil = Time.unscaledTime + 5f;
        }

        private void Update()
        {
            if (!_open) return;
            if (Game.State == GameState.Title || Game.State == GameState.Loading || Game.Arsenal == null)
            {
                CloseSilently();
                return;
            }
            if (Act.Pause || GInput.UiBack)
            {
                // The release of B must not become a dodge roll once gameplay has the pad again.
                Pad.Consume(PadButton.East);
                _closeRequested = true;
            }
        }

        // Closing waits for LateUpdate, so the same Esc press cannot also open the pause menu this frame.
        private void LateUpdate()
        {
            if (_open && _closeRequested) DoClose();
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (!_open) return;
            try { Draw(); }
            catch (Exception e)
            {
                // A drawing bug must not leave the game stuck behind a broken screen.
                Log.Exception("Crystal offer screen", e);
                _closeRequested = true;
            }
        }

        private void BuildRows(CrystalArsenal a)
        {
            _rows.Clear();
            if (_mode == Mode.Claim && !string.IsNullOrEmpty(_sel) && _sel != BoardKey) _rows.Add(_sel);
            if (_mode == Mode.Case)
            {
                AddRow(CrystalEntitlementRules.StarterToken);
                AddRow(CrystalEntitlementRules.CalibrationToken);
            }
            List<CrystalClaimView> views = a.Claims();
            for (int i = 0; i < views.Count; i++) if (views[i].Pending) AddRow(views[i].Token);
            if (_mode == Mode.Case) _rows.Add(BoardKey);
            if (!string.IsNullOrEmpty(_sel) && _rows.Contains(_sel)) return;
            // Open on the first thing that needs a choice (an open offer or a waiting reward), else the board.
            _sel = _rows.Count > 0 ? _rows[_rows.Count - 1] : BoardKey;
            for (int i = 0; i < _rows.Count; i++)
            {
                string t = _rows[i];
                if (t == BoardKey) continue;
                CrystalEntitlementSave e = CrystalEntitlementRules.Find(a.EntitlementList, t);
                string reason;
                if ((e == null && a.OfferOpen(t, out reason)) || CrystalEntitlementRules.IsPending(e))
                {
                    _sel = t;
                    break;
                }
            }
        }

        private void AddRow(string token)
        {
            if (!_rows.Contains(token)) _rows.Add(token);
        }

        private void Draw()
        {
            GUI.depth = 0;
            UIKit.BeginScaledMenu();
            CrystalArsenal a = Game.Arsenal;
            float W = UIKit.Width, H = UIKit.Height;
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            float pw = Mathf.Min(1560f, W - 60f);
            Rect panel = new Rect((W - pw) * 0.5f, 46f, pw, H - 92f);
            UIKit.Rect(panel, UIKit.Panel);
            UIKit.Frame(panel, new Color(0.35f, 0.85f, 1f, 0.55f), 2f);
            UIKit.Text(new Rect(panel.x + 30f, panel.y + 14f, panel.width - 60f, 50f), _mode == Mode.Case ? "RESONANCE CASE" : "CRYSTAL REWARD", 38, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(panel.x + 30f, panel.y + 62f, panel.width - 60f, 26f),
                _mode == Mode.Case ? "Veyra Crystalworks loan case: two compartments, a contract board, and any reward still waiting for room." : "Choose where it goes. Nothing in your armory is replaced unless you pick it.",
                17, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft);
            if (a == null)
            {
                UIKit.Text(new Rect(panel.x + 30f, panel.y + 110f, panel.width - 60f, 30f), "The crystal arsenal is not available.", 20, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(panel.xMax - 230f, panel.yMax - 66f, 200f, 48f), "CLOSE", false, 20)) _closeRequested = true;
                return;
            }
            BuildRows(a);
            Rect body = new Rect(panel.x + 30f, panel.y + 104f, panel.width - 60f, panel.height - 104f - 70f);
            float leftW = Mathf.Min(470f, body.width * 0.32f);
            Rect left = new Rect(body.x, body.y, leftW, body.height);
            Rect right = new Rect(left.xMax + 24f, body.y, body.xMax - left.xMax - 24f, body.height);
            DrawRows(a, left);
            UIKit.Rect(right, new Color(1f, 1f, 1f, 0.04f));
            Rect inner = new Rect(right.x + 14f, right.y + 12f, right.width - 28f, right.height - 24f);
            if (_sel == BoardKey) DrawBoard(a, inner);
            else DrawOffer(a, inner, _sel);
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(panel.x + 30f, panel.yMax - 58f, panel.width - 300f, 50f), _status, 18, _statusGood ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            if (UIKit.Button(new Rect(panel.xMax - 230f, panel.yMax - 62f, 200f, 48f), "CLOSE", false, 20)) _closeRequested = true;
        }

        private void DrawRows(CrystalArsenal a, Rect left)
        {
            float y = left.y;
            for (int i = 0; i < _rows.Count; i++)
            {
                string token = _rows[i];
                Rect row = new Rect(left.x, y, left.width, 62f);
                bool hot, focused;
                if (ProgressionUI.Hotspot(row, out hot, out focused)) _sel = token;
                bool sel = token == _sel;
                UIKit.Rect(row, new Color(1f, 1f, 1f, sel ? 0.14f : 0.05f));
                string title, sub;
                Color c;
                RowText(a, token, out title, out sub, out c);
                UIKit.Rect(new Rect(row.x, row.y, 5f, row.height), c);
                if (focused) UIKit.Frame(row, UIKit.Cyan, 3f);
                else if (sel) UIKit.Frame(row, UIKit.Gold, 2f);
                UIKit.Text(new Rect(row.x + 14f, row.y + 5f, row.width - 24f, 26f), title, 19, c, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(row.x + 14f, row.y + 33f, row.width - 24f, 24f), sub, 14, new Color(0.8f, 0.82f, 0.86f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 70f;
                if (y > left.yMax - 62f) break;
            }
        }

        private static void RowText(CrystalArsenal a, string token, out string title, out string sub, out Color c)
        {
            if (token == BoardKey)
            {
                title = "Contract board";
                sub = "Resonance Breach and the Foreman's trail";
                c = new Color(1f, 0.6f, 0.2f);
                return;
            }
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(a.EntitlementList, token);
            int unique = e != null ? e.Unique : (token == CrystalEntitlementRules.StarterToken ? CrystalFocusDb.ShardwakeUnique : CrystalFocusDb.PrismThreadUnique);
            LootUniqueDef u = LootUniques.Get(unique);
            string name = u != null ? u.Name : "Crystal focus";
            c = u != null ? GearSystem.RarityColor(u.Rarity) : Color.white;
            string prefix = token == CrystalEntitlementRules.StarterToken ? "Compartment 1: " : (token == CrystalEntitlementRules.CalibrationToken ? "Compartment 2: " : "");
            title = prefix + name;
            string reason;
            if (e == null) sub = a.OfferOpen(token, out reason) ? "Waiting for you" : "Locked: calibrate at the practice lane";
            else if (CrystalEntitlementRules.IsPending(e)) sub = "Reward waiting" + (e.Retro ? " (earlier victory)" : "") + ": " + e.Source;
            else sub = "Taken (" + e.Source + ")";
            if (e == null && !a.OfferOpen(token, out reason)) c = ProgressionUI.Dim;
        }

        private void DrawOffer(CrystalArsenal a, Rect r, string token)
        {
            LootGun g = a.PreviewFor(token);
            if (g == null)
            {
                UIKit.Text(new Rect(r.x, r.y, r.width, 40f), "Nothing to show here.", 20, Color.white, TextAnchor.UpperLeft);
                return;
            }
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(a.EntitlementList, token);
            string reason;
            bool locked = e == null && !a.OfferOpen(token, out reason);
            bool gone = e != null && CrystalEntitlementRules.IsGranted(e) && (Game.Loot == null || Game.Loot.Find(e.Uid) == null);
            string cmpLabel = "";
            LootArmory.CardStats cmp = Game.Loot != null ? Game.Loot.CompareFor(g, out cmpLabel) : new LootArmory.CardStats();
            float actionsH = 176f;
            Rect card = new Rect(r.x, r.y, r.width, r.height - actionsH);
            LootArmory.DrawCard(card, g, cmp, cmpLabel, 17);
            Rect act = new Rect(r.x, card.yMax + 10f, r.width, actionsH - 10f);
            if (locked)
            {
                UIKit.Text(new Rect(act.x, act.y, act.width, 60f), "Locked. Hit all three practice targets beside the case with any crystal focus to open the second compartment. Practice gives no rewards and is never reported as a crime.",
                    17, new Color(1f, 0.8f, 0.45f), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                return;
            }
            if (gone)
            {
                UIKit.Text(new Rect(act.x, act.y, act.width, 60f), "Taken earlier and later sold or scrapped. A reward is granted once per save.", 17, ProgressionUI.Dim, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                return;
            }
            bool close;
            DrawClaimActions(act, token, true, Status, out close);
            if (close) _closeRequested = true;
        }

        /// <summary>
        /// The choice buttons for one reward or offer (shared with the ARSENAL claim panel): Equip to a free slot, Replace
        /// slot 1 to 4 (with the slot's gun named under it), Send to or keep in the backpack, Keep as a pending claim when
        /// nothing has room, and Not now. A refused choice stays visible and explains itself when pressed.
        /// </summary>
        public static void DrawClaimActions(Rect r, string token, bool notNow, Action<string, bool> status, out bool close)
        {
            close = false;
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return;
            CrystalCapacity cap = a.CapacityFor(token);
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(a.EntitlementList, token);
            float bw = (r.width - 40f) / 5f;
            string why;
            bool okFree = a.CanClaim(token, CrystalClaimAction.EquipFree, -1, out why);
            string freeLabel = cap.InSlot >= 0 ? "IN SLOT " + (cap.InSlot + 1) : (cap.FreeSlot >= 0 ? "EQUIP TO SLOT " + (cap.FreeSlot + 1) : "NO FREE SLOT");
            if (ProgressionUI.Button(new Rect(r.x, r.y, bw * 2f + 10f, 46f), freeLabel, false, 17, okFree)) Choose(a, token, CrystalClaimAction.EquipFree, -1, status, ref close);
            bool inBp = cap.InBackpack;
            bool okBp = a.CanClaim(token, CrystalClaimAction.ToBackpack, -1, out why);
            if (ProgressionUI.Button(new Rect(r.x + 2f * (bw + 10f), r.y, bw * 2f + 10f, 46f), inBp ? "KEEP IN BACKPACK" : "SEND TO BACKPACK", false, 17, okBp))
            {
                if (inBp && okBp) { status("It stays in your backpack.", true); close = notNow; }
                else Choose(a, token, CrystalClaimAction.ToBackpack, -1, status, ref close);
            }
            bool allFull = e == null && CrystalOfferRules.AllFull(cap);
            if (allFull)
            {
                if (ProgressionUI.Button(new Rect(r.x + 4f * (bw + 10f), r.y, bw, 46f), "KEEP PENDING", false, 15, true)) Choose(a, token, CrystalClaimAction.KeepPending, -1, status, ref close);
            }
            else if (notNow && ProgressionUI.Button(new Rect(r.x + 4f * (bw + 10f), r.y, bw, 46f), e != null && CrystalEntitlementRules.IsGranted(e) ? "DONE" : "NOT NOW", false, 17, true))
            {
                close = true;
            }
            float y = r.y + 56f;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "Or replace a loot slot (its gun moves to the backpack):", 15, new Color(0.8f, 0.82f, 0.86f), TextAnchor.UpperLeft);
            y += 24f;
            int n = cap.SlotOccupied != null ? cap.SlotOccupied.Length : 0;
            float sw = (r.width - (n - 1) * 10f) / Mathf.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                bool ok = a.CanClaim(token, CrystalClaimAction.EquipReplace, i, out why);
                string label = cap.InSlot == i ? "IN SLOT " + (i + 1) : (cap.SlotOccupied[i] ? "REPLACE SLOT " + (i + 1) : "EQUIP SLOT " + (i + 1));
                Rect b = new Rect(r.x + i * (sw + 10f), y, sw, 42f);
                if (ProgressionUI.Button(b, label, false, 15, ok)) Choose(a, token, CrystalClaimAction.EquipReplace, i, status, ref close);
                string under = cap.SlotOccupied[i] ? (cap.SlotFavorite[i] ? "* " : "") + cap.SlotNames[i] : "empty";
                UIKit.Text(new Rect(b.x, b.yMax + 2f, b.width, 20f), under, 13, cap.SlotFavorite[i] ? UIKit.Gold : new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperCenter, FontStyle.Normal, true, true);
            }
        }

        private static void Choose(CrystalArsenal a, string token, CrystalClaimAction action, int slot, Action<string, bool> status, ref bool close)
        {
            string msg;
            bool ok = a.CanClaim(token, action, slot, out msg) && a.Claim(token, action, slot, out msg);
            if (status != null) status(msg, ok);
            if (Game.Audio != null && !ok) Game.Audio.Play2D(SfxId.UIBack, 0.4f);
        }
    }
}
