using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityDiscoverySystem
    {
        private DiscoverySiteView SiteFor(string id)
        {
            foreach (DiscoverySiteView site in _sites) if (site.Definition.id == id) return site;
            return null;
        }
        private void DrawLocalHint(float w, float h)
        {
            if (this == null || !FreeExploration || Game.Player.InVehicle) return;
            DiscoverySiteView nearest = null; float distance = 9f * 9f;
            foreach (DiscoverySiteView site in _sites)
            {
                float d = MathUtil.FlatSqrDistance(Game.PlayerPosition, site.Points[0]);
                if (d >= distance || Mathf.Abs(Game.PlayerPosition.y - site.Points[0].y) > 2f) continue;
                distance = d; nearest = site;
            }
            if (nearest == null) return;
            DiscoveryEntrySave state;
            if (!_snapshots.TryGetValue(nearest.Definition.id, out state)) return;
            Rect panel = new Rect(w - 450f, h - 320f, 420f, 112f);
            UIKit.Rect(panel, UIKit.Panel);
            UIKit.Rect(new Rect(panel.x, panel.y, 3f, panel.height), state.Solved ? UIKit.Gold : UIKit.Cyan);
            UIKit.Text(new Rect(panel.x + 14f, panel.y + 7f, panel.width - 28f, 26f), "CRYSTAL DISCOVERY  " + _book.ClaimedCount + "/3", 20, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            string status = !state.Read ? "Read the MEMORY CORE to learn the sequence." :
                (state.Solved ? (state.RewardMask == 3 ? "Memory restored. The core retains its story." : "Sequence complete. Return to the core for your reward.") :
                _book.OrderText(nearest.Definition.id) + "   (" + state.Step + "/3)");
            string safety = SafetyReason();
            UIKit.Text(new Rect(panel.x + 14f, panel.y + 39f, panel.width - 28f, 62f), safety ?? status,
                19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
        private void DrawPage(Rect body)
        {
            if (this == null || !_ready || _book == null) return;
            if (string.IsNullOrEmpty(_selected)) _selected = _catalog.sites[0].id;
            UIKit.Text(new Rect(body.x + 22f, body.y + 12f, body.width - 44f, 45f), "THE CITY REMEMBERS  |  " + _book.ClaimedCount + "/3 memories", 31, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(body.x + 22f, body.y + 62f, body.width - 44f, 55f),
                "Visit a D on the map. Read its core, touch the runes in order, then collect the memory reward. No combat is required.",
                21, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float left = Mathf.Min(470f, body.width * .39f), row = 112f;
            for (int i = 0; i < _catalog.sites.Length; i++)
            {
                DiscoveryDefinition d = _catalog.sites[i]; DiscoveryEntrySave s = _book.Status(d.id);
                Rect card = new Rect(body.x + 22f, body.y + 131f + row * i, left - 15f, 99f);
                if (UIKit.Button(card, d.title + "\n" + StateText(s), _selected == d.id, 22)) _selected = d.id;
            }
            Rect detail = new Rect(body.x + left + 26f, body.y + 132f, body.width - left - 50f, body.height - 175f);
            DiscoveryDefinition selected = _book.Definition(_selected); if (selected == null) return;
            DiscoveryEntrySave entry = _book.Status(_selected); DiscoverySiteView view = SiteFor(_selected);
            UIKit.Rect(detail, UIKit.PanelLight);
            float x = detail.x + 18f, width = detail.width - 36f;
            UIKit.Text(new Rect(x, detail.y + 12f, width, 50f), selected.title, 27, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            string place;
            if (!_placementNotes.TryGetValue(_selected, out place)) place = "Shrine placement begins after free exploration starts.";
            if (view != null && Game.Player != null) place = "Ground shrine: " + Vector3.Distance(Game.PlayerPosition, view.Points[0]).ToString("0") + " m away. No flight or rooftop landing needed.";
            UIKit.Text(new Rect(x, detail.y + 75f, width, 65f), place, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (view != null && UIKit.Button(new Rect(x, detail.y + 145f, width, 46f), "Track shrine on map / set waypoint", _book.TrackedId == _selected, 22))
            {
                _book.Track(_selected);
                if (Game.Gps != null) Game.Gps.SetWaypoint(view.Points[0]);
                Note("Waypoint set to the ground shrine. Active mission routes retain priority.", 4f);
            }
            string lore = !entry.Read ? "The inscription is waiting at the MEMORY CORE. You can discover all three shrines in any order." :
                (entry.Solved ? selected.memory : selected.inscription);
            UIKit.Text(new Rect(x, detail.y + 210f, width, 167f), lore, 21, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (entry.Read) UIKit.Text(new Rect(x, detail.y + 392f, width, 32f), "Sequence: " + _book.OrderText(_selected), 23, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            string reward = "Reward: " + selected.lumen + " Lumen + " + selected.amount + " " + MaterialDb.NameOf(selected.material) +
                ". Claim at the core. Finish all three for one Prism Core at any core.";
            UIKit.Text(new Rect(x, detail.y + 443f, width, 95f), reward, 20, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            UIKit.Text(new Rect(body.x + 22f, body.yMax - 47f, body.width - 44f, 37f),
                (_book.CircuitClaimed ? "CIRCUIT COMPLETE. Prism Core received. " : "Progress is stored in each game save. ") + "Use SAVE / LOAD before leaving the game.",
                20, UIKit.Gold, TextAnchor.MiddleLeft);
        }
        private static string StateText(DiscoveryEntrySave s)
        {
            if (s == null || !s.Seen) return "Undiscovered";
            if (s.RewardMask == 3) return "Memory restored";
            if (s.Solved) return "Return to core for reward";
            if (s.Read) return "Sequence " + s.Step + "/3";
            return "Read the inscription";
        }
    }
}
