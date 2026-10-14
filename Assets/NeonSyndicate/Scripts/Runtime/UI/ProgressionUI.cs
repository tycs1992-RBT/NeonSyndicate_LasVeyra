using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Drawing helpers shared by the SKILLS and FACTIONS pages and the Forge screen: a clickable area that takes part
    /// in controller navigation exactly like UIKit.Button (so the D-pad, the stick, the arrow keys and the mouse all
    /// work), straight lines, and small text helpers.
    /// </summary>
    public static class ProgressionUI
    {
        public static readonly Color ForgeOrange = new Color(1f, 0.6f, 0.2f);
        public static readonly Color Good = new Color(0.45f, 1f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.45f, 0.4f);
        public static readonly Color Dim = new Color(0.6f, 0.6f, 0.62f);

        /// <summary>
        /// An invisible button: registers with the pad navigation, returns true when clicked (mouse) or confirmed (pad,
        /// Enter); hot is true while the mouse is over it or the pad focus is on it. The caller draws the visuals.
        /// </summary>
        public static bool Hotspot(Rect r, out bool hot, out bool focused)
        {
            Event e = Event.current;
            focused = PadNav.Register(r, false);
            hot = focused || (e != null && r.Contains(e.mousePosition));
            if (e != null && ((e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) || (focused && PadNav.TakeClick())))
            {
                if (e.type == EventType.MouseDown) e.Use();
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIClick, 0.5f);
                return true;
            }
            return false;
        }

        /// <summary>A UIKit button that looks greyed out when not enabled (it still reports clicks, so the caller can explain why).</summary>
        public static bool Button(Rect r, string label, bool selected, int size, bool enabled)
        {
            return UIKit.Button(r, enabled ? label : Gray(label), selected, size);
        }

        /// <summary>A straight line between two points in UIKit coordinates.</summary>
        public static void Line(Vector2 a, Vector2 b, Color c, float width)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            if (Mathf.Abs(d.x) < 0.01f)
            {
                UIKit.Rect(new Rect(a.x - width * 0.5f, Mathf.Min(a.y, b.y), width, Mathf.Abs(d.y)), c);
                return;
            }
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 m = GUI.matrix;
            UIKit.RotateAround(a, ang);
            UIKit.Rect(new Rect(a.x, a.y - width * 0.5f, len, width), c);
            GUI.matrix = m;
        }

        public static string Gray(string s)
        {
            return "<color=#808080>" + s + "</color>";
        }

        public static string Hex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }

        public static string Colored(string s, Color c)
        {
            return "<color=#" + Hex(c) + ">" + s + "</color>";
        }

        public static string Pct(float f)
        {
            return Mathf.RoundToInt(f * 100f) + "%";
        }

        /// <summary>A short hint about the controls for a menu, matching the device in use.</summary>
        public static string NavHint(string confirmWhat)
        {
            if (Pad.Connected && Pad.LastUsed)
                return "D-pad or stick: move     " + Pad.Tag(PadButton.South) + ": " + confirmWhat + "     " + Pad.Tag(PadButton.East) + ": back     " + Pad.Tag(PadButton.LB) + " / " + Pad.Tag(PadButton.RB) + ": tabs";
            return "Mouse or arrow keys: choose     Click or Enter: " + confirmWhat + "     Esc: back";
        }
    }
}
