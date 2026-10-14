using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The travel powers' screens: the small HUD indicator (power name, speed, altitude in flight, the leap charge),
    /// shown only while a power is active, and the F2 debug page "Travel" (unlock everything, back to the story's
    /// unlocks, show the hints again, live numbers).
    /// </summary>
    public sealed partial class TravelPowers
    {
        private void RegisterUi()
        {
            HudOverlays.Register("travel", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Travel", Order = 46, Draw = DrawDebug });
        }

        /// <summary>Bottom centre, below the objective line: name, speed and a bar (speed, or the leap charge).</summary>
        private void DrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || p.InVehicle || p.IsRagdolled) return;
            bool charging = p.LeapCharging;
            TravelMode m = p.TravelState;
            if (m == TravelMode.None && !charging) return;
            float speed = p.TravelSpeed;
            string name;
            float fill;
            Color col = UIKit.Cyan;
            if (charging)
            {
                name = "SUPER LEAP";
                fill = p.LeapCharge;
                col = UIKit.Gold;
            }
            else if (m == TravelMode.Surge)
            {
                name = "SURGE";
                fill = speed / TravelMath.SurgeTopSpeed;
            }
            else if (m == TravelMode.Flight)
            {
                name = "FLIGHT";
                fill = speed / TravelMath.FlyFast;
                col = new Color(0.75f, 0.6f, 1f);
            }
            else
            {
                name = p.Gliding ? "GLIDE" : (p.Velocity.y > 0f ? "LEAP" : "AIR");
                fill = speed / TravelMath.SurgeTopSpeed;
            }
            string right = charging
                ? Mathf.RoundToInt(TravelMath.LeapHeight(fill) * 3.28084f) + " ft leap"
                : Mathf.RoundToInt(speed * 2.23694f) + " mph";
            if (m == TravelMode.Flight && !charging) right += "   ALT " + Mathf.RoundToInt(Mathf.Max(0f, p.transform.position.y) * 3.28084f) + " ft";
            float bw = 330f, bh = 46f;
            Rect r = new Rect(w * 0.5f - bw * 0.5f, h - 86f, bw, bh);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.55f));
            UIKit.Rect(new Rect(r.x, r.y, 3f, r.height), col);
            UIKit.Text(new Rect(r.x + 14f, r.y + 4f, 150f, 26f), name, 20, col, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 110f, r.y + 4f, bw - 124f, 26f), right, 20, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            UIKit.Bar(new Rect(r.x + 14f, r.y + 32f, bw - 28f, 6f), Mathf.Clamp01(fill), col, new Color(1f, 1f, 1f, 0.15f));
        }

        private void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 300f, bh = 46f, gap = 20f;
            UIKit.Text(new Rect(x, y, r.width, 28f), "TRAVEL POWERS", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            string state = "Surge: on     Super Leap and Glide: " + (LeapUnlocked ? "on" : "locked") + "     Flight: " + (FlightUnlocked ? "on" : "locked")
                + (_debug ? "     (debug unlock)" : "");
            UIKit.Text(new Rect(x, y, r.width, 26f), state, 18, Color.white, TextAnchor.UpperLeft);
            y += 32f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Unlock all travel powers", _debug, 18)) DebugUnlockAll(true);
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Back to story unlocks", false, 18)) DebugUnlockAll(false);
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Show the button hints again", false, 18)) DebugResetHints();
            y += bh + 18f;
            PlayerController p = Game.Player;
            if (p != null)
            {
                string live = "Now: " + p.TravelState + (p.Gliding ? " (gliding)" : "") + "   speed " + p.TravelSpeed.ToString("0.0") + " m/s ("
                    + Mathf.RoundToInt(p.TravelSpeed * 2.23694f) + " mph)   height " + p.transform.position.y.ToString("0") + " m";
                UIKit.Text(new Rect(x, y, r.width, 26f), live, 18, new Color(0.8f, 0.9f, 1f), TextAnchor.UpperLeft);
                y += 28f;
            }
            string uses = "Uses: Surge " + _uses[(int)TravelPower.Surge] + ", Super Leap " + _uses[(int)TravelPower.Leap] + ", Glide " + _uses[(int)TravelPower.Glide]
                + ", Flight " + _uses[(int)TravelPower.Flight] + "   (button hints show for the first " + HintUses + " uses)";
            UIKit.Text(new Rect(x, y, r.width, 26f), uses, 18, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft);
            y += 34f;
            string rules = "Story unlocks: Surge from the start, Super Leap and Glide after story mission " + TravelMath.LeapStory + ", Flight after story mission "
                + TravelMath.FlightStory + ". Free Roam has everything. Jog " + TravelMath.JogSpeed + " m/s, sprint " + TravelMath.SprintSpeed + " m/s, Surge "
                + TravelMath.SurgeTopSpeed + " m/s, flight " + TravelMath.FlyCruise + " to " + TravelMath.FlyFast + " m/s, ceiling " + TravelMath.SoftCeiling + " m.";
            UIKit.Text(new Rect(x, y, r.width - 20f, 80f), rules, 17, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
