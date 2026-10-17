using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Underheart" (Order 60): status and build stats, the player's cell and heat, region teleports,
    /// a paged teleport list of every named anchor, shortcut toggles, discover-all, a progress reset and a descent window
    /// override (early, late, none or follow the quest). For testing only; nothing here is reachable in normal play.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private string _debugStatus = "";
        private int _debugAnchorPage;

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, cw = r.width - 40f;
            UIKit.Text(new Rect(x, y, cw, 30f), "UNDERHEART", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            UIKit.Text(new Rect(x, y, cw, 44f), Status, 16, Ready ? Color.white : new Color(1f, 0.6f, 0.4f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 44f;
            if (_layout == null)
            {
                UIKit.Text(new Rect(x, y, cw, 24f), "No layout (the Underheart is disabled in this session).", 17, Color.white, TextAnchor.UpperLeft);
                return;
            }
            string stats = "Variant " + _layout.Variant + " (" + UnderheartLayout.VariantName(_layout.Variant) + ")   cells " + _layout.Cells.Count + "   doorways " + _layout.Portals.Count +
                           "   anchors " + _layout.Anchors.Count + "   shortcuts open " + _shortcuts.Count + "/" + _layout.Shortcuts.Count + "   discovered " + _discovered.Count + "/7";
            UIKit.Text(new Rect(x, y, cw, 22f), stats, 16, Color.white, TextAnchor.UpperLeft);
            y += 22f;
            if (_builder != null)
            {
                string build = "Built rooms " + _builder.CellsBuilt + "   colliders " + _builder.ColliderCount + "   renderers " + _builder.RendererCount + "   triangles " + _builder.TriangleCount +
                               "   lights on " + _lightsOn + "/" + _builder.Lights.Count + "   props " + _builder.PropCount + "   prepare " + _prepareMs + " ms   build " + _buildMs + " ms" +
                               (_builder.Problems.Count > 0 ? "   problems " + _builder.Problems.Count + " (" + _builder.Problems[0] + ")" : "");
                UIKit.Text(new Rect(x, y, cw, 22f), build, 16, _builder.Problems.Count > 0 ? new Color(1f, 0.7f, 0.4f) : Color.white, TextAnchor.UpperLeft);
                y += 22f;
            }
            string portal = _layout.Portal != null ? "Vehicle portal: " + _layout.Portal.LotText + ", tunnel " + Mathf.RoundToInt(_layout.Portal.AccessLength) + " m" : "Vehicle portal skipped: " + PortalReason;
            UIKit.Text(new Rect(x, y, cw, 22f), portal + "   stair " + (StairOpen ? "open" : "closed") + "   streaming " + string.Join(", ", new System.Collections.Generic.List<string>(_activeGroups).ToArray()), 16, Color.white, TextAnchor.UpperLeft);
            y += 22f;
            PlayerController p = Game.Player;
            if (p == null)
            {
                UIKit.Text(new Rect(x, y + 6f, cw, 24f), "Start a game to use the teleports.", 17, Color.white, TextAnchor.UpperLeft);
                return;
            }
            string cell = _playerCell >= 0 ? _layout.Cells[_playerCell].Id : "(street)";
            UIKit.Text(new Rect(x, y, cw, 22f), "You: " + (PlayerRegion.Length > 0 ? UnderheartIds.RegionTitle(PlayerRegion) : "the surface") + "   cell " + cell + "   y " + Game.PlayerPosition.y.ToString("0.0") +
                       "   heat " + _heatNow.ToString("0") + "/s   cooled " + _cooled.ToString("0.0") + " s   return point " + (_restAnchor.Length > 0 ? _restAnchor : "(none)") +
                       "   atmosphere " + (Game.Atmo != null ? Game.Atmo.UndergroundAmount.ToString("0.00") : "-"), 16, UIKit.Cyan, TextAnchor.UpperLeft);
            y += 30f;
            float bw = 228f, bh = 38f, gap = 8f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Discover every region", false, 15))
            {
                foreach (string id in UnderheartIds.Regions) if (!_discovered.Contains(id)) _discovered.Add(id);
                _debugStatus = "Every region is discovered (map layer and rest points).";
            }
            if (UIKit.Button(new Rect(x + (bw + gap), y, bw, bh), "Open every shortcut", false, 15))
            {
                foreach (UhShortcut s in _layout.Shortcuts) UnlockShortcut(s.Id, false);
                _debugStatus = "Every shortcut is open.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Close every shortcut", false, 15))
            {
                foreach (UhShortcut s in _layout.Shortcuts) CloseShortcut(s.Id);
                _debugStatus = "Every shortcut is closed again.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Reset Underheart progress", false, 15))
            {
                ResetAll();
                _debugStatus = "Shortcuts, discoveries, cleared rooms and the return point were reset.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 4f, y, bw, bh), "Go to the Pressure House", false, 15))
            {
                if (SurfaceEntrance.sqrMagnitude > 1f) { DebugTeleport(GameRoot.FindGround(new Vector3(SurfaceEntrance.x, 2f, SurfaceEntrance.z)), 0f); _debugStatus = "At the Pressure House door."; }
                else _debugStatus = "The Pressure House door is unknown.";
            }
            y += bh + gap;
            // The descent window (uh05_13): normally it follows the running quest phase; these force one to check the
            // reliquary's heat and light without playing to Act IV.
            UIKit.Text(new Rect(x, y + 8f, 520f, 22f), "Descent window: " + UnderheartWindowRules.Title(DescentWindow) + (DescentWindowForced ? " (forced)" : " (follows the quest)"), 16, UIKit.Cyan, TextAnchor.UpperLeft);
            if (UIKit.Button(new Rect(x + 530f, y, 190f, bh - 4f), "Follow the quest", !DescentWindowForced, 15)) { ForceDescentWindow(null); _debugStatus = "The descent window follows the running quest again."; }
            if (UIKit.Button(new Rect(x + 728f, y, 150f, bh - 4f), "Force early", DescentWindowForced && DescentWindow == UnderheartWindowRules.Early, 15)) { ForceDescentWindow(UnderheartWindowRules.Early); _debugStatus = "Forced the early window: cool heat columns, lamps out in the Ash Reliquary."; }
            if (UIKit.Button(new Rect(x + 886f, y, 150f, bh - 4f), "Force late", DescentWindowForced && DescentWindow == UnderheartWindowRules.Late, 15)) { ForceDescentWindow(UnderheartWindowRules.Late); _debugStatus = "Forced the late window: hot heat columns, lit columns in the Ash Reliquary."; }
            if (UIKit.Button(new Rect(x + 1044f, y, 150f, bh - 4f), "Force none", DescentWindowForced && DescentWindow == UnderheartWindowRules.None, 15)) { ForceDescentWindow(UnderheartWindowRules.None); _debugStatus = "Forced no window: the reliquary at its normal heat and light."; }
            y += bh + 2f;
            UIKit.Text(new Rect(x, y, cw, 22f), "Regions (go to the entry):", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            for (int i = 0; i < UnderheartIds.Regions.Length; i++)
            {
                string region = UnderheartIds.Regions[i];
                Rect b = new Rect(x + (i % 4) * (bw + gap + 60f), y + (i / 4) * (bh + gap), bw + 60f, bh);
                if (UIKit.Button(b, (_discovered.Contains(region) ? "" : "(new) ") + UnderheartIds.RegionTitle(region), PlayerRegion == region, 15))
                    _debugStatus = GoToAnchor(EntryAnchorOf(region));
            }
            y += (bh + gap) * 2f;
            UIKit.Text(new Rect(x, y, cw, 22f), "Shortcuts (click to toggle):", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            for (int i = 0; i < _layout.Shortcuts.Count; i++)
            {
                UhShortcut s = _layout.Shortcuts[i];
                bool open = ShortcutOpen(s.Id);
                if (UIKit.Button(new Rect(x + i * (bw + gap + 60f), y, bw + 60f, bh), s.Title + (open ? ": OPEN" : ": closed"), open, 15))
                {
                    if (open) CloseShortcut(s.Id);
                    else UnlockShortcut(s.Id, true);
                    _debugStatus = s.Title + (open ? " closed." : " opened.");
                }
            }
            y += bh + gap + 4f;
            const int perPage = 15;
            int pages = Mathf.Max(1, Mathf.CeilToInt(_layout.Anchors.Count / (float)perPage));
            _debugAnchorPage = Mathf.Clamp(_debugAnchorPage, 0, pages - 1);
            UIKit.Text(new Rect(x, y + 8f, 420f, 22f), "Anchors, page " + (_debugAnchorPage + 1) + " of " + pages + " (click to go):", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(x + 430f, y, 120f, bh - 4f), "Previous", false, 15)) _debugAnchorPage = (_debugAnchorPage + pages - 1) % pages;
            if (UIKit.Button(new Rect(x + 560f, y, 120f, bh - 4f), "Next", false, 15)) _debugAnchorPage = (_debugAnchorPage + 1) % pages;
            y += bh + 2f;
            for (int i = 0; i < perPage; i++)
            {
                int idx = _debugAnchorPage * perPage + i;
                if (idx >= _layout.Anchors.Count) break;
                UhAnchor a = _layout.Anchors[idx];
                Rect b = new Rect(x + (i % 5) * (bw + gap + 30f), y + (i / 5) * (bh - 4f + gap), bw + 30f, bh - 4f);
                if (UIKit.Button(b, a.Id, false, 14)) _debugStatus = GoToAnchor(a.Id);
            }
            y += 3f * (bh - 4f + gap) + 4f;
            if (_debugStatus.Length > 0) UIKit.Text(new Rect(x, y, cw, 24f), _debugStatus, 17, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private string GoToAnchor(string anchorId)
        {
            Vector3 pos;
            float yaw;
            if (!TryAnchor(anchorId, out pos, out yaw)) return "Unknown anchor " + anchorId + ".";
            if (!Ready) return "The Underheart is not built in this session.";
            DebugTeleport(pos + Vector3.up * 0.05f, yaw);
            return "At " + anchorId + " (" + UnderheartRules.FloorLabel(UnderheartIds.RegionOfAnchor(anchorId).Length > 0 ? UnderheartIds.RegionOfAnchor(anchorId) : UnderheartIds.EmberCanals, pos.y) + ").";
        }

        private void DebugTeleport(Vector3 pos, float yaw)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            p.Teleport(pos, yaw);
            if (Game.Cam != null)
            {
                Game.Cam.Yaw = yaw;
                Game.Cam.SnapBehindPlayer();
            }
            _snapAtmosphere = true;
        }
    }
}
