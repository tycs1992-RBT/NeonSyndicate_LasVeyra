using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Draws the giver-state glyphs in the immediate-mode UI (pause map, HUD, journal, STATUS) with the same shapes as
    /// the overhead markers: diamond, ring, down arrow, check, bar and the small completed dot. origin is the GUI group's
    /// screen offset (zero outside a group) so rotated parts pivot correctly inside the map's group.
    /// </summary>
    public static class GuidanceGlyphs04E
    {
        public static void Draw(Vector2 origin, Vector2 c, MarkerShape04E shape, Color color, float size)
        {
            if (shape == MarkerShape04E.None) return;
            float h = size * 0.5f;
            UIKit.Tex(new Rect(c.x - h - 3f, c.y - h - 3f, size + 6f, size + 6f), UIKit.Circle, new Color(0f, 0f, 0f, 0.55f * color.a));
            Matrix4x4 m = GUI.matrix;
            switch (shape)
            {
                case MarkerShape04E.Diamond:
                {
                    float q = size * 0.34f;
                    UIKit.RotateAround(origin + c, 45f);
                    UIKit.Rect(new Rect(c.x - q, c.y - q, q * 2f, q * 2f), color);
                    UIKit.Rect(new Rect(c.x - q * 0.35f, c.y - q * 0.35f, q * 0.7f, q * 0.7f), new Color(1f, 1f, 1f, 0.55f * color.a));
                    GUI.matrix = m;
                    break;
                }
                case MarkerShape04E.Ring:
                    UIKit.Tex(new Rect(c.x - h * 0.85f, c.y - h * 0.85f, size * 0.85f, size * 0.85f), UIKit.Ring, color);
                    break;
                case MarkerShape04E.Arrow:
                    UIKit.RotateAround(origin + c, 180f);
                    UIKit.Tex(new Rect(c.x - h * 0.8f, c.y - h * 0.8f, size * 0.8f, size * 0.8f), UIKit.Arrow, color);
                    GUI.matrix = m;
                    break;
                case MarkerShape04E.Check:
                    Line(origin, new Vector2(c.x - h * 0.55f, c.y), new Vector2(c.x - h * 0.15f, c.y + h * 0.45f), color, size * 0.16f);
                    Line(origin, new Vector2(c.x - h * 0.2f, c.y + h * 0.45f), new Vector2(c.x + h * 0.6f, c.y - h * 0.45f), color, size * 0.16f);
                    break;
                case MarkerShape04E.Bar:
                    UIKit.Rect(new Rect(c.x - h * 0.7f, c.y - size * 0.09f, size * 0.7f, size * 0.18f), color);
                    break;
                case MarkerShape04E.DoneDot:
                    UIKit.Tex(new Rect(c.x - h * 0.6f, c.y - h * 0.6f, size * 0.6f, size * 0.6f), UIKit.Ring, color);
                    Line(origin, new Vector2(c.x - h * 0.3f, c.y), new Vector2(c.x - h * 0.08f, c.y + h * 0.22f), color, size * 0.09f);
                    Line(origin, new Vector2(c.x - h * 0.1f, c.y + h * 0.22f), new Vector2(c.x + h * 0.3f, c.y - h * 0.22f), color, size * 0.09f);
                    break;
            }
        }

        /// <summary>A screen-edge arrow at c pointing along dir (GUI space, y down).</summary>
        public static void EdgeArrow(Vector2 c, Vector2 dir, Color color, float size)
        {
            float angle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            Matrix4x4 m = GUI.matrix;
            UIKit.Tex(new Rect(c.x - size * 0.62f, c.y - size * 0.62f, size * 1.24f, size * 1.24f), UIKit.Circle, new Color(0f, 0f, 0f, 0.5f));
            UIKit.RotateAround(c, angle);
            UIKit.Tex(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), UIKit.Arrow, color);
            GUI.matrix = m;
        }

        /// <summary>A vertical hint arrow (up or down) at c.</summary>
        public static void UpDown(Vector2 c, int vertical, Color color, float size)
        {
            if (vertical == 0) return;
            Matrix4x4 m = GUI.matrix;
            if (vertical < 0) UIKit.RotateAround(c, 180f);
            UIKit.Tex(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), UIKit.Arrow, color);
            GUI.matrix = m;
        }

        public static void Line(Vector2 origin, Vector2 a, Vector2 b, Color c, float width)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 m = GUI.matrix;
            UIKit.RotateAround(origin + a, ang);
            UIKit.Rect(new Rect(a.x, a.y - width * 0.5f, len, width), c);
            GUI.matrix = m;
        }
    }
}
