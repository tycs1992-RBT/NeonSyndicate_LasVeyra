using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Crystal arsenal" (Order 53): grant each focus through the real entitlement path, fill the
    /// backpack or the slots to test pending claims, list every record, reset the offers, force a retroactive claim,
    /// complete calibration and show the placement audit.
    /// </summary>
    public sealed partial class CrystalArsenal
    {
        private string _debugStatus = "";
        private int _debugGrants;
        private int _debugScroll;

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, w = r.width - 40f;
            UIKit.Text(new Rect(x, y, w, 30f), "CRYSTAL ARSENAL", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            LootArmory la = Game.Loot;
            UIKit.Text(new Rect(x, y, w, 24f), "Records " + _entitlements.Count + " (pending " + PendingCount + ")   Calibration " + (_calibrationDone ? "done" : CrystalPracticeRules.Count(_calibrationHits) + "/3") +
                       "   Backpack " + (la != null ? la.Backpack.Count + "/" + LootArmory.BackpackSize : "n/a") + "   Practice noise suppressed: " + (CrystalPractice.SuppressNoise ? "yes" : "no"), 16, Color.white, TextAnchor.UpperLeft);
            y += 26f;
            UIKit.Text(new Rect(x, y, w, 44f), _placeReport, 14, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 46f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, w, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            UIKit.Text(new Rect(x, y, w, 22f), "Grant a focus through the entitlement path (backpack when there is room, else a pending claim)", 15, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            float bw = (w - 50f) / 6f;
            for (int i = 1; i <= CrystalFocusDb.UniqueCount; i++)
            {
                CrystalBehavior b = (CrystalBehavior)i;
                CrystalProfile p = CrystalFocusDb.Get(b);
                if (UIKit.Button(new Rect(x + (i - 1) * (bw + 10f), y, bw, 40f), p.Name, false, 15))
                {
                    _debugGrants++;
                    string token = CrystalEntitlementRules.DebugPrefix + p.Name.Replace(" ", "_").ToLowerInvariant() + ":" + _debugGrants;
                    bool ok = GrantInternal(token, CrystalFocusDb.UniqueOf(b), RewardLevel(LootSource.Normal), "Debug grant", false);
                    CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, token);
                    _debugStatus = ok ? p.Name + (CrystalEntitlementRules.IsGranted(e) ? " is in the backpack (uid " + e.Uid + ")." : " is a pending claim (backpack full).") : "Grant refused.";
                }
            }
            y += 48f;
            float cw = (w - 30f) / 4f;
            if (UIKit.Button(new Rect(x, y, cw, 40f), "Fill the backpack", false, 15) && la != null)
            {
                int n = 0;
                while (!la.BackpackFull && n < LootArmory.BackpackSize) { la.AddToBackpack(la.NewGun(Rarity.Common, LootSource.Normal), false); n++; }
                _debugStatus = "Added " + n + " Common guns; the backpack is full.";
            }
            if (UIKit.Button(new Rect(x + cw + 10f, y, cw, 40f), "Fill empty loot slots", false, 15) && la != null)
            {
                int n = 0;
                for (int s = la.FirstFreeSlot(); s >= 0; s = la.FirstFreeSlot()) { la.Equip(la.NewGun(Rarity.Common, LootSource.Normal), s, false); n++; }
                _debugStatus = "Filled " + n + " empty slots.";
            }
            if (UIKit.Button(new Rect(x + 2f * (cw + 10f), y, cw, 40f), "Reset offers", false, 15))
            {
                int n = RemoveTokens(t => t == CrystalEntitlementRules.StarterToken || t == CrystalEntitlementRules.CalibrationToken);
                _calibrationDone = false;
                for (int i = 0; i < _calibrationHits.Length; i++) _calibrationHits[i] = false;
                _hintsShown = 0;
                _hintShownThisSession = false;
                _debugStatus = "Removed " + n + " offer records and reset calibration (guns already taken stay in the armory).";
            }
            if (UIKit.Button(new Rect(x + 3f * (cw + 10f), y, cw, 40f), "Complete calibration", false, 15))
            {
                for (int i = 0; i < _calibrationHits.Length; i++) _calibrationHits[i] = true;
                _calibrationDone = true;
                _version++;
                _debugStatus = "Calibration marked complete: Prism Thread is open in the case.";
            }
            y += 48f;
            string[] bosses = CrystalEntitlementRules.SignatureBosses;
            for (int i = 0; i < bosses.Length; i++)
            {
                string boss = bosses[i];
                if (UIKit.Button(new Rect(x + i * (cw + 10f), y, cw, 40f), "Force retro: " + boss, false, 14))
                {
                    if (HasEntitlement(CrystalEntitlementRules.BossToken(boss))) _debugStatus = boss + " already has a record. Use Remove boss records first.";
                    else
                    {
                        System.Collections.Generic.List<int> uids = new System.Collections.Generic.List<int>();
                        System.Collections.Generic.List<int> uniques = new System.Collections.Generic.List<int>();
                        CollectOwned(uids, uniques);
                        CrystalEntitlementSave e = CrystalEntitlementRules.CreateRetro(_entitlements, boss, RewardLevel(LootSource.Boss), uids, uniques);
                        _version++;
                        _debugStatus = e != null ? "Retro claim " + e.Token + (CrystalEntitlementRules.IsGranted(e) ? " adopted uid " + e.Uid + "." : " is pending.") : "No retro claim made.";
                    }
                }
            }
            if (UIKit.Button(new Rect(x + 3f * (cw + 10f), y, cw, 40f), "Remove boss records", false, 14))
            {
                int n = RemoveTokens(t => t.StartsWith(CrystalEntitlementRules.BossPrefix, System.StringComparison.Ordinal));
                _debugStatus = "Removed " + n + " boss records (the defeat ledger and the guns are untouched).";
            }
            y += 48f;
            if (UIKit.Button(new Rect(x, y, cw, 40f), "Run reconcile now", false, 15))
            {
                RunReconcile();
                _debugStatus = "Reconcile and retro pass done (see the log).";
            }
            if (UIKit.Button(new Rect(x + cw + 10f, y, cw, 40f), "GPS to the case", false, 15)) TrackCase();
            if (UIKit.Button(new Rect(x + 2f * (cw + 10f), y, cw, 40f), "Open the case screen", false, 15) && _ui != null)
            {
                if (Game.Menus != null) Game.Menus.HideAll();
                _ui.Open(CrystalOfferUi.Mode.Case, null);
            }
            if (UIKit.Button(new Rect(x + 3f * (cw + 10f), y, cw, 40f), "Test reward card", false, 15))
            {
                CrystalEntitlementSave last = _entitlements.Count > 0 ? _entitlements[_entitlements.Count - 1] : null;
                if (last != null)
                {
                    QueueRewardCard(last.Token, null, Game.PlayerPosition, last.Token.StartsWith(CrystalEntitlementRules.BossPrefix, System.StringComparison.Ordinal));
                    _debugStatus = "Queued a card for " + last.Token + " (shows after the menu closes).";
                }
                else _debugStatus = "No record to show.";
            }
            y += 50f;
            if (!string.IsNullOrEmpty(_debugStatus))
            {
                UIKit.Text(new Rect(x, y, w, 24f), _debugStatus, 16, ProgressionUI.Good, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                y += 28f;
            }
            UIKit.Text(new Rect(x, y, w, 22f), "RECORDS (token, focus, status, uid, level, source)", 15, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            if (_entitlements.Count > 10)
            {
                if (UIKit.Button(new Rect(x + w - 230f, y - 4f, 110f, 30f), "Up", false, 14)) _debugScroll = Mathf.Max(0, _debugScroll - 5);
                if (UIKit.Button(new Rect(x + w - 110f, y - 4f, 110f, 30f), "Down", false, 14)) _debugScroll = Mathf.Min(_entitlements.Count - 1, _debugScroll + 5);
            }
            y += 26f;
            int shown = 0;
            for (int i = Mathf.Clamp(_debugScroll, 0, Mathf.Max(0, _entitlements.Count - 1)); i < _entitlements.Count && shown < 10 && y < r.yMax - 22f; i++, shown++)
            {
                CrystalEntitlementSave e = _entitlements[i];
                bool present = la != null && e.Uid > 0 && la.Find(e.Uid) != null;
                string line = e.Token + "   " + NameOfUnique(e.Unique) + "   " + (CrystalEntitlementRules.IsGranted(e) ? "Granted" : "Pending") + "   uid " + e.Uid +
                              (CrystalEntitlementRules.IsGranted(e) ? (present ? " (in armory)" : " (sold or scrapped)") : "") + "   lv " + e.Level + "   " + e.Source + (e.Retro ? "   retro" : "");
                UIKit.Text(new Rect(x, y, w, 20f), line, 14, CrystalEntitlementRules.IsGranted(e) ? Color.white : new Color(1f, 0.75f, 0.35f), TextAnchor.UpperLeft);
                y += 21f;
            }
            if (_entitlements.Count == 0) UIKit.Text(new Rect(x, y, w, 20f), "No records yet.", 14, Color.white, TextAnchor.UpperLeft);
        }
    }
}
