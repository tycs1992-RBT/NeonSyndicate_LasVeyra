using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// WorldEvents, part two: the HUD callout when an event appears (its title, one line of flavour, distance and
    /// direction), short status lines for events close by, and the Events tab of the F2 debug menu (director switch,
    /// end all, spawn now, reset cooldowns, cross the Veil and a Fire button for every template). Every control is a
    /// UIKit button, so the mouse, the arrow keys and a controller all work.
    /// </summary>
    public sealed partial class WorldEvents
    {
        private const int DebugRows = 9;
        private const float CalloutSeconds = 8f;
        private const float StatusRange = 140f;

        private int _debugPage;
        private string _debugStatus = "";

        private void RegisterUi()
        {
            HudOverlays.Register("world_events", DrawCallout);
            DebugPages.Register(new MenuPage { Title = "Events", Order = 31, Draw = DrawDebug });
        }

        private static string Hex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        private static string Bearing(Vector3 p)
        {
            return Mathf.RoundToInt(MathUtil.FlatDistance(Game.PlayerPosition, p)) + " m " + QuestWorld.Direction(Game.PlayerPosition, p);
        }

        // ================================================================== HUD

        private EventInstance _calloutFor;
        private string _calloutBearing = "";
        private float _calloutBearingAt;

        /// <summary>The callout panel at the top centre for a new event, then one status line per nearby event (lines rebuilt a few times a second).</summary>
        private void DrawCallout(float W, float H)
        {
            float y = 106f;
            EventInstance c = _callout;
            float now = Time.unscaledTime;
            float left = _calloutUntil - now;
            // 04E: a held event (the player is underground) says nothing until it resumes.
            if (c != null && !c.Done && !c.Held && left > 0f)
            {
                float a = Mathf.Clamp01(Mathf.Min((CalloutSeconds - left) * 4f, left * 1.5f));
                Color k = KindColor(c.T, c.Inner);
                float w = 680f;
                if (c != _calloutFor || now >= _calloutBearingAt)
                {
                    _calloutFor = c;
                    _calloutBearingAt = now + EventInstance.StatusInterval;
                    _calloutBearing = Bearing(c.FocusPos);
                }
                Rect r = new Rect(W * 0.5f - w * 0.5f, y, w, 86f);
                UIKit.Rect(r, new Color(0f, 0f, 0f, 0.62f * a));
                UIKit.Rect(new Rect(r.x, r.y, 4f, r.height), new Color(k.r, k.g, k.b, 0.95f * a));
                UIKit.Text(new Rect(r.x + 20f, r.y + 6f, w - 40f, 20f), "WORLD EVENT", 15, new Color(k.r, k.g, k.b, a), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(r.x + 20f, r.y + 6f, w - 40f, 20f), _calloutBearing, 15, new Color(0.85f, 0.85f, 0.85f, a), TextAnchor.UpperRight);
                UIKit.Text(new Rect(r.x + 20f, r.y + 24f, w - 40f, 30f), c.T.Title, 25, new Color(1f, 1f, 1f, a), TextAnchor.UpperLeft, FontStyle.Bold);
                string line = string.IsNullOrEmpty(c.T.Callout) ? c.T.Objective : c.T.Callout;
                UIKit.Text(new Rect(r.x + 20f, r.y + 55f, w - 40f, 26f), line, 17, new Color(0.88f, 0.88f, 0.88f, a), TextAnchor.UpperLeft);
                y += 94f;
            }
            int shown = 0;
            foreach (EventInstance e in _active)
            {
                if (e == null || e.Done || e.Held || shown >= MaxActive) continue;
                if (!e.Engaged && e.DistToFocus > StatusRange) continue;
                if (e == c && left > 0f) continue;
                if (e.HudLine == null || now >= e.HudAt || !ReferenceEquals(e.HudStatus, e.Status))
                {
                    e.HudAt = now + EventInstance.StatusInterval;
                    e.HudStatus = e.Status;
                    string what = string.IsNullOrEmpty(e.Status) ? e.T.Objective : e.Status;
                    e.HudLine = "<color=" + Hex(KindColor(e.T, e.Inner)) + "><b>" + e.T.Title + "</b></color>   " + what + "   <color=#bbbbbb>" + Bearing(e.FocusPos) + "</color>";
                    e.HudWidth = Mathf.Clamp(UIKit.Measure(e.HudLine, 18).x + 40f, 320f, 900f);
                }
                float w = e.HudWidth;
                UIKit.Rect(new Rect(W * 0.5f - w * 0.5f, y, w, 32f), new Color(0f, 0f, 0f, 0.5f));
                UIKit.Text(new Rect(W * 0.5f - w * 0.5f + 16f, y + 5f, w - 32f, 24f), e.HudLine, 18, Color.white, TextAnchor.UpperCenter);
                y += 38f;
                shown++;
            }
        }

        // ================================================================== debug

        private void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            if (UIKit.Button(new Rect(x, y, 250f, 44f), "Director: " + (DirectorEnabled ? "ON" : "OFF"), DirectorEnabled, 18))
            {
                DirectorEnabled = !DirectorEnabled;
                _debugStatus = DirectorEnabled ? "The director picks events on its own again." : "The director is off; only the Fire buttons start events.";
            }
            if (UIKit.Button(new Rect(x + 260f, y, 250f, 44f), "End all events", false, 18))
            {
                EndAll(true);
                _debugStatus = "Every event was ended and cleaned up.";
            }
            if (UIKit.Button(new Rect(x + 520f, y, 250f, 44f), "Spawn random now", false, 18))
            {
                DirectorEnabled = true;
                _nextSpawn = _clock;
                _debugStatus = "The director tries a random event as soon as the menu closes.";
            }
            if (UIKit.Button(new Rect(x + 780f, y, 250f, 44f), "Reset cooldowns", false, 18))
            {
                _lastFired.Clear();
                _debugStatus = "Every template may appear again.";
            }
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            if (Game.Inner != null && UIKit.Button(new Rect(x + 1040f, y, 250f, 44f), inner ? "Leave the Inner World" : "Enter the Inner World", false, 18))
            {
                Game.Inner.Shift(!inner, true);
                _debugStatus = inner ? "Back in the Outer City." : "Inside the Inner World.";
            }
            y += 52f;
            int running = CountRunning();
            int held = HeldCount;
            string info = "Templates " + Templates.Count + "   Running " + running + " / " + MaxActive + (held > 0 ? " (" + held + " held below the street)" : "") + "   Next random try in " + Mathf.CeilToInt(NextSpawnIn) + " s" + (LoadProblems.Count > 0 ? "   Load problems: " + LoadProblems.Count + " (see the Console)" : "");
            UIKit.Text(new Rect(x, y, r.width, 24f), info, 17, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 26f;
            UIKit.Text(new Rect(x, y, r.width, 24f), Blocked() ? "A mission, cutscene or cinematic is running: events wait until it ends." : _debugStatus, 17, UIKit.Gold, TextAnchor.UpperLeft);
            y += 30f;
            int pages = Mathf.Max(1, (Templates.Count + DebugRows - 1) / DebugRows);
            _debugPage = Mathf.Clamp(_debugPage, 0, pages - 1);
            HashSet<string> act = new HashSet<string>();
            foreach (EventInstance e in _active) if (!e.Done) act.Add(e.T.Id);
            EventSituation s = Situation();
            for (int i = _debugPage * DebugRows; i < Mathf.Min(Templates.Count, (_debugPage + 1) * DebugRows); i++)
            {
                EventTemplate t = Templates[i];
                string why = EventSelector.WhyNot(t, s, LastFired, act);
                string state = why == null ? "<color=#60ff80>could appear now</color>" : "<color=#999999>" + why + "</color>";
                string veil = t.Veil == "Inner" ? "  <color=#c080ff>Inner</color>" : (t.Veil == "Any" ? "  <color=#c080ff>either side</color>" : "");
                UIKit.Text(new Rect(x, y + 2f, 900f, 24f), "<color=" + Hex(KindColor(t, t.Veil == "Inner")) + ">" + t.Kind + "</color>  <b>" + t.Title + "</b>" + veil + "   seen " + SeenCount(t.Id), 18, Color.white, TextAnchor.UpperLeft);
                UIKit.Text(new Rect(x + 24f, y + 24f, 880f, 20f), t.Id + "   " + state, 15, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x + 920f, y, 200f, 44f), "Fire", false, 17)) FireFromDebug(t);
                y += 50f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, y + 6f, 200f, 42f), "Prev page", false, 18)) _debugPage = (_debugPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + 210f, y + 6f, 200f, 42f), "Next page", false, 18)) _debugPage = (_debugPage + 1) % pages;
                UIKit.Text(new Rect(x + 430f, y + 14f, 300f, 30f), "Page " + (_debugPage + 1) + " of " + pages, 18, Color.white, TextAnchor.UpperLeft);
            }
        }

        /// <summary>
        /// Starts one template near the player as soon as the menu closes (20 to 90 m away, district rules ignored).
        /// The player crosses the Veil first when the template lives on the other side.
        /// </summary>
        private void FireFromDebug(EventTemplate t)
        {
            if (Blocked())
            {
                _debugStatus = "End the mission or cutscene first.";
                return;
            }
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            if (Game.Inner != null && !EventSelector.VeilAllowed(t, inner)) Game.Inner.Shift(!inner, true);
            _debugFire = t;
            _debugStatus = "Firing " + t.Title + ".";
            if (Game.Menus != null) Game.Menus.Close();
        }
    }
}
