using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// F2 > Builds 04E (Order 56): the ledger and ward state, station placement, and test grants of any reward under a
    /// "debug:" token (saved like any entitlement, so claims, rollback and reloads can be exercised in a native session).
    /// </summary>
    public sealed partial class BuildSystem
    {
        private int _debugReward;
        private int _debugSerial;
        private string _debugStatus = "";
        private bool _debugFitting;

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, bw = 250f, bh = 40f, gap = 10f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 30f), "BUILDS, WARD AND REWARD LEDGER (04E)", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            string state = "Suggestion " + (_preset.Length > 0 ? _preset : "none") + "   Ward " + (_wardUnlocked ? "entitled" : (WardAvailable ? "by level" : "locked")) +
                           "   Aspect " + WardRules04E.AspectTitle(_activeAspect) + " (" + _aspects.Count + " unlocked)   Kits " + _kitClaims.Count +
                           "   Ledger " + _ledger.Entries.Count + " (" + _ledger.PendingCount + " pending)   Stations " + FittingStationCount + " fitting, " + LedgerTerminalCount + " ledger";
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f), state, 16, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            string flags = "Unlocks: " + (_unlocks.Count > 0 ? string.Join(", ", _unlocks.ToArray()) : "none") + "   Key items: " + (_keyItems.Count > 0 ? string.Join(", ", _keyItems.ToArray()) : "none") +
                           "   Fracture focus uid " + FractureFocusUid + "   At fitting " + AtFitting;
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f), flags, 15, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 30f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            string id = RewardIds04E.All[Mathf.Clamp(_debugReward, 0, RewardIds04E.All.Length - 1)];
            if (UIKit.Button(new Rect(x, y, 60f, bh), "<", false, 18)) _debugReward = (_debugReward + RewardIds04E.All.Length - 1) % RewardIds04E.All.Length;
            RewardDef04E def = RewardCatalog04E.Get(id);
            UIKit.Text(new Rect(x + 70f, y, 420f, bh), id + "  (" + (def != null ? def.Title : "?") + ")", 17, UIKit.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(x + 500f, y, 60f, bh), ">", false, 18)) _debugReward = (_debugReward + 1) % RewardIds04E.All.Length;
            if (UIKit.Button(new Rect(x + 570f, y, 160f, bh), "Grant", false, 16))
            {
                string token = "debug:" + id + ":" + (++_debugSerial) + ":" + Time.frameCount;
                _debugStatus = _ledger.Grant(id, token, "Debug grant") ? "Granted " + id + " under " + token + ". Claim it on the BUILD page." : "Not granted.";
            }
            if (UIKit.Button(new Rect(x + 740f, y, 200f, bh), "Grant and claim", false, 16))
            {
                string token = "debug:" + id + ":" + (++_debugSerial) + ":" + Time.frameCount;
                string m;
                bool ok = _ledger.GrantAndClaim(id, token, "Debug grant", out m);
                _debugStatus = (ok ? "Claimed: " : "Pending: ") + m;
            }
            y += bh + gap;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Unlock the starter ward", false, 16))
            {
                UnlockStarterWard("Debug");
                _debugStatus = "Starter ward unlocked: the Buckler goes into a free gadget slot.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap), y, bw, bh), "Unlock all four aspects", false, 16))
            {
                foreach (string a in WardRules04E.Aspects) UnlockAspect(a, "Debug");
                _debugStatus = "All four ward aspects unlocked (switch them on the BUILD page).";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), _debugFitting ? "Fitting here: ON" : "Fitting station here", _debugFitting, 16))
            {
                _debugFitting = true;
                string why;
                if (!OpenFitting(Game.PlayerPosition, out why)) _debugStatus = why;
                else _debugStatus = "The BUILD page is open in fitting mode (as at a station).";
                _debugFitting = false;
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Open the reward ledger", false, 16)) OpenLedger();
            y += bh + gap + 6f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 60f), _debugStatus, 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 60f;
            if (Game.Gadgets != null)
                UIKit.Text(new Rect(x, y, r.width - 40f, 80f), Game.Gadgets.WardDebugLine(), 15, new Color(0.8f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
