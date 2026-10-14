using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Immediate-mode UI toolkit shared by the HUD and menus: resolution-independent scaling (1080p reference),
    /// cached styles, procedural icon textures (stars, circles, arrows, rings) and drawing helpers.
    /// </summary>
    public static class UIKit
    {
        public const float RefHeight = 1080f;
        private static Texture2D _white;
        private static Texture2D _circle;
        private static Texture2D _ring;
        private static Texture2D _star;
        private static Texture2D _starOutline;
        private static Texture2D _arrow;
        private static Texture2D _vignette;
        private static Texture2D _gradient;
        private static readonly Dictionary<long, GUIStyle> _styles = new Dictionary<long, GUIStyle>();
        private static GUIStyle _shadowStyle;

        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public static readonly Color Cyan = new Color(0.3f, 0.95f, 1f);
        public static readonly Color Pink = new Color(1f, 0.3f, 0.7f);
        public static readonly Color Health = new Color(0.32f, 0.85f, 0.38f);
        public static readonly Color Armor = new Color(0.3f, 0.6f, 1f);
        public static readonly Color Energy = new Color(1f, 0.8f, 0.2f);
        public static readonly Color Panel = new Color(0.02f, 0.03f, 0.05f, 0.78f);
        public static readonly Color PanelLight = new Color(0.1f, 0.12f, 0.16f, 0.9f);

        public static void ResetStatics()
        {
            _white = null;
            _circle = null;
            _ring = null;
            _star = null;
            _starOutline = null;
            _arrow = null;
            _vignette = null;
            _gradient = null;
            _styles.Clear();
            _shadowStyle = null;
            PadNav.ResetStatics();
        }

        private static bool _menuScale;

        /// <summary>UI scale for the current OnGUI: the HUD scale setting applies to the HUD and phone, not to full-screen menus.</summary>
        public static float Scale { get { return Mathf.Max(0.4f, Screen.height / RefHeight * (_menuScale ? 1f : Settings.UiScale)); } }
        public static float Width { get { return Screen.width / Scale; } }
        public static float Height { get { return Screen.height / Scale; } }

        public static void BeginScaled()
        {
            _menuScale = false;
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        }

        /// <summary>Like BeginScaled but ignores the HUD scale setting (menus must fit the screen at any setting).</summary>
        public static void BeginScaledMenu()
        {
            _menuScale = true;
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        }

        /// <summary>Rotates following GUI drawing around a point given in unscaled-group GUI coordinates (safe inside GUI groups).</summary>
        public static void RotateAround(Vector2 guiPivot, float degrees)
        {
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(guiPivot, Quaternion.Euler(0f, 0f, degrees), Vector3.one) * Matrix4x4.TRS(-guiPivot, Quaternion.identity, Vector3.one);
        }

        public static Vector2 MouseVirtual
        {
            get
            {
                Vector2 m = Event.current != null ? Event.current.mousePosition : Vector2.zero;
                return m;
            }
        }

        // ------------------------------------------------------------------ textures

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _white.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
                    _white.Apply();
                }
                return _white;
            }
        }

        private static Texture2D Make(int size, System.Func<float, float, float> alpha)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)Mathf.Clamp(alpha(u, v) * 255f, 0f, 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        public static Texture2D Circle
        {
            get
            {
                if (_circle == null) _circle = Make(64, (u, v) => Mathf.Clamp01((1f - Mathf.Sqrt(u * u + v * v)) * 24f));
                return _circle;
            }
        }

        public static Texture2D Ring
        {
            get
            {
                if (_ring == null) _ring = Make(64, (u, v) => { float d = Mathf.Sqrt(u * u + v * v); return Mathf.Clamp01((0.12f - Mathf.Abs(d - 0.85f)) * 30f); });
                return _ring;
            }
        }

        /// <summary>Heat crystal (the wanted level): a tall six-sided gem with a faceted centre line.</summary>
        private static float StarAlpha(float u, float v, bool outline)
        {
            float y = -v;
            float w;
            if (y > 0.3f) w = Mathf.Lerp(0.46f, 0f, (y - 0.3f) / 0.65f);
            else if (y < -0.3f) w = Mathf.Lerp(0.46f, 0f, (-0.3f - y) / 0.65f);
            else w = 0.46f;
            float d = w - Mathf.Abs(u);
            float inside = Mathf.Clamp01(d * 30f) * (Mathf.Abs(y) < 0.95f ? 1f : 0f);
            if (outline) return Mathf.Clamp01((0.08f - Mathf.Abs(d - 0.05f)) * 30f) * (Mathf.Abs(y) < 0.97f ? 1f : 0f);
            // Facets: a darker centre ridge and a bright upper-left face.
            float ridge = 1f - 0.25f * Mathf.Clamp01(1f - Mathf.Abs(u) * 18f);
            float face = u < 0f && y > -0.1f ? 1f : 0.82f;
            return inside * ridge * face;
        }

        public static Texture2D Star
        {
            get
            {
                if (_star == null) _star = Make(64, (u, v) => StarAlpha(u, v, false));
                return _star;
            }
        }

        public static Texture2D StarOutline
        {
            get
            {
                if (_starOutline == null) _starOutline = Make(64, (u, v) => StarAlpha(u, v, true));
                return _starOutline;
            }
        }

        /// <summary>Arrow pointing up (+V in texture space is up on screen).</summary>
        public static Texture2D Arrow
        {
            get
            {
                if (_arrow == null)
                {
                    _arrow = Make(64, (u, v) =>
                    {
                        float y = v;
                        if (y < -0.85f || y > 0.9f) return 0f;
                        float halfW = Mathf.Lerp(0.75f, 0f, (y + 0.85f) / 1.75f);
                        float notch = y < -0.4f ? Mathf.Lerp(0f, 0.45f, (-0.4f - y) / 0.45f) : 0f;
                        float ax = Mathf.Abs(u);
                        if (ax > halfW) return 0f;
                        if (y < -0.4f && ax < notch) return 0f;
                        return Mathf.Clamp01((halfW - ax) * 20f);
                    });
                }
                return _arrow;
            }
        }

        public static Texture2D Vignette
        {
            get
            {
                if (_vignette == null) _vignette = Make(128, (u, v) => Mathf.Clamp01(Mathf.Pow(Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) * 1.05f, 3f) * 1.2f + Mathf.Pow(Mathf.Sqrt(u * u + v * v) * 0.72f, 4f)));
                return _vignette;
            }
        }

        /// <summary>Vertical gradient: opaque at the bottom, transparent at the top.</summary>
        public static Texture2D Gradient
        {
            get
            {
                if (_gradient == null) _gradient = Make(32, (u, v) => Mathf.Clamp01((1f - (v + 1f) * 0.5f)));
                return _gradient;
            }
        }

        // ------------------------------------------------------------------ styles

        public static GUIStyle Style(int size, FontStyle fs, TextAnchor anchor, bool wrap = false)
        {
            long key = size * 1000L + (int)fs * 100 + (int)anchor * 2 + (wrap ? 1 : 0);
            GUIStyle s;
            if (_styles.TryGetValue(key, out s) && s != null) return s;
            s = new GUIStyle(GUI.skin != null ? GUI.skin.label : GUIStyle.none);
            if (Mats.UIFont != null) s.font = Mats.UIFont;
            s.fontSize = size;
            s.fontStyle = fs;
            s.alignment = anchor;
            s.wordWrap = wrap;
            s.richText = true;
            s.clipping = TextClipping.Overflow;
            s.normal.textColor = Color.white;
            s.padding = new RectOffset(0, 0, 0, 0);
            s.margin = new RectOffset(0, 0, 0, 0);
            _styles[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ drawing

        public static void Rect(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }

        public static void Tex(Rect r, Texture t, Color c)
        {
            if (t == null) return;
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t);
            GUI.color = old;
        }

        public static void Frame(Rect r, Color c, float thickness)
        {
            Rect(new Rect(r.x, r.y, r.width, thickness), c);
            Rect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            Rect(new Rect(r.x, r.y, thickness, r.height), c);
            Rect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

        public static void Text(Rect r, string text, int size, Color c, TextAnchor anchor, FontStyle fs = FontStyle.Normal, bool shadow = true, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUIStyle st = Style(size, fs, anchor, wrap);
            Color old = GUI.color;
            if (shadow)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.85f * c.a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, st);
            }
            GUI.color = c;
            GUI.Label(r, text, st);
            GUI.color = old;
        }

        public static Vector2 Measure(string text, int size, FontStyle fs = FontStyle.Normal)
        {
            return Style(size, fs, TextAnchor.UpperLeft).CalcSize(new GUIContent(text));
        }

        public static void Bar(Rect r, float fill, Color fg, Color bg)
        {
            Rect(r, bg);
            Rect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), fg);
        }

        public static void Stars(float x, float y, float size, int stars, bool flashing, float alpha)
        {
            for (int i = 0; i < 5; i++)
            {
                Rect r = new Rect(x + (4 - i) * -(size + 4f) - size, y, size, size);
                bool on = i < stars;
                if (on)
                {
                    float a = flashing ? (Mathf.PingPong(Time.unscaledTime * 3f, 1f) * 0.6f + 0.25f) : 1f;
                    Tex(r, Star, new Color(1f, 0.55f, 0.95f, a * alpha));
                }
                else Tex(r, StarOutline, new Color(0.85f, 0.75f, 1f, 0.35f * alpha));
            }
        }

        /// <summary>Clickable text button. Returns true when clicked. Highlighted when hovered, selected or focused by the pad.</summary>
        public static bool Button(Rect r, string label, bool selected, int size = 26)
        {
            Event e = Event.current;
            bool focused = PadNav.Register(r, false);
            bool hover = r.Contains(e.mousePosition) || focused;
            Rect(r, selected ? new Color(1f, 0.82f, 0.3f, 0.95f) : (hover ? new Color(1f, 1f, 1f, 0.22f) : new Color(0f, 0f, 0f, 0.55f)));
            if (focused) Frame(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), Cyan, 3f);
            Text(r, label, size, selected ? Color.black : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, !selected);
            if ((e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) || (focused && PadNav.TakeClick()))
            {
                if (e.type == EventType.MouseDown) e.Use();
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIClick, 0.5f);
                return true;
            }
            return false;
        }

        /// <summary>Horizontal slider drawn with the toolkit. Returns the new value. The pad nudges it with left/right.</summary>
        public static float Slider(Rect r, float value, float min, float max)
        {
            Event e = Event.current;
            bool focused = PadNav.Register(r, true);
            Rect track = new Rect(r.x, r.y + r.height * 0.4f, r.width, r.height * 0.2f);
            Rect(track, new Color(1f, 1f, 1f, focused ? 0.35f : 0.2f));
            float t = Mathf.InverseLerp(min, max, value);
            Rect(new Rect(track.x, track.y, track.width * t, track.height), Gold);
            Rect knob = new Rect(track.x + track.width * t - 8f, r.y, 16f, r.height);
            Rect(knob, focused ? Cyan : Color.white);
            if (focused)
            {
                Frame(new Rect(r.x - 6f, r.y - 3f, r.width + 12f, r.height + 6f), Cyan, 2f);
                int step = PadNav.TakeSliderStep();
                if (step != 0) value = Mathf.Clamp(value + step * (max - min) * 0.05f, min, max);
            }
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && new Rect(r.x - 10f, r.y, r.width + 20f, r.height).Contains(e.mousePosition))
            {
                float nt = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                value = Mathf.Lerp(min, max, nt);
                e.Use();
            }
            return value;
        }

        public static string KeyName(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.LeftShift: return "Shift";
                case KeyCode.LeftControl: return "Ctrl";
                case KeyCode.LeftAlt: return "Alt";
                case KeyCode.Space: return "Space";
                case KeyCode.CapsLock: return "Caps Lock";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Tab: return "Tab";
                case KeyCode.LeftBracket: return "[";
                case KeyCode.RightBracket: return "]";
                case KeyCode.Period: return ".";
                case KeyCode.Comma: return ",";
                case KeyCode.Slash: return "/";
                default: return k.ToString();
            }
        }
    }
    /// <summary>
    /// Controller and arrow-key navigation for the immediate-mode menus. Every button and slider registers its
    /// screen rectangle while it is drawn; the D-pad or left stick moves the focus to the nearest control in that
    /// direction, A/Cross (or Enter) clicks it, left/right nudges a focused slider.
    /// </summary>
    public static class PadNav
    {
        private struct Item
        {
            public Rect Screen;
            public bool Slider;
        }

        private static readonly List<Item> _last = new List<Item>();
        private static readonly List<Item> _cur = new List<Item>();
        private static int _frame = -1;
        private static Vector2 _focus = new Vector2(-9999f, -9999f);
        private static Rect _focusRect;
        private static bool _hasFocus;
        private static bool _click;
        private static int _sliderStep;
        private static bool _active;
        private static float _activeUntil;

        public static void ResetStatics()
        {
            _last.Clear();
            _cur.Clear();
            _frame = -1;
            _hasFocus = false;
            _click = false;
            _sliderStep = 0;
            _active = false;
            _focus = new Vector2(-9999f, -9999f);
        }

        /// <summary>True while the pad or the arrow keys drive the menus (the mouse takes over when it moves).</summary>
        public static bool Active { get { return _active; } }

        private static void BeginFrame()
        {
            int f = Time.frameCount;
            if (f == _frame) return;
            _frame = f;
            _last.Clear();
            _last.AddRange(_cur);
            _cur.Clear();
            _click = false;
            _sliderStep = 0;
            int dir = GInput.NavDirection();
            bool confirm = GInput.Down(KeyCode.Return) || GInput.Down(KeyCode.KeypadEnter) || Pad.Down(PadButton.South);
            if (dir != 0 || confirm || (Pad.Connected && Pad.LastUsed && Pad.AnyDown())) { _active = true; _activeUntil = Time.unscaledTime + 30f; }
            if (_active && (!Pad.LastUsed && GInput.RawMouseDelta.sqrMagnitude > 4f || Time.unscaledTime > _activeUntil)) _active = false;
            if (_last.Count == 0) { _hasFocus = false; return; }
            // Keep the focus on the control nearest to where it was last frame (menus redraw every frame).
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < _last.Count; i++)
            {
                float d = (_last[i].Screen.center - _focus).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            if (!_hasFocus || bd > 400f * 400f)
            {
                // Nothing near the old focus (a new screen opened): start at the top-left control.
                best = 0;
                float score = float.MaxValue;
                for (int i = 0; i < _last.Count; i++)
                {
                    float sc = _last[i].Screen.y * 2f + _last[i].Screen.x;
                    if (sc < score) { score = sc; best = i; }
                }
            }
            Item cur = _last[best];
            if (!_active) { _focus = cur.Screen.center; _focusRect = cur.Screen; _hasFocus = true; return; }
            if (dir != 0)
            {
                if (cur.Slider && (dir == 3 || dir == 4)) _sliderStep = dir == 3 ? -1 : 1;
                else
                {
                    Vector2 want = dir == 1 ? Vector2.down : dir == 2 ? Vector2.up : dir == 3 ? Vector2.left : Vector2.right;
                    int pick = -1;
                    float ps = float.MaxValue;
                    for (int i = 0; i < _last.Count; i++)
                    {
                        if (i == best) continue;
                        Vector2 d = _last[i].Screen.center - cur.Screen.center;
                        float along = Vector2.Dot(d, want);
                        if (along < 4f) continue;
                        float side = Mathf.Abs(want.x != 0f ? d.y : d.x);
                        float sc = along + side * 2.2f;
                        if (sc < ps) { ps = sc; pick = i; }
                    }
                    if (pick >= 0)
                    {
                        best = pick;
                        cur = _last[pick];
                        if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIHover, 0.35f);
                    }
                }
            }
            _focus = cur.Screen.center;
            _focusRect = cur.Screen;
            _hasFocus = true;
            _click = confirm;
        }

        /// <summary>Registers a control drawn this frame; returns true when it has the focus.</summary>
        public static bool Register(Rect guiRect, bool slider)
        {
            BeginFrame();
            Rect sr = ToScreen(guiRect);
            if (Event.current != null && Event.current.type == EventType.Repaint) _cur.Add(new Item { Screen = sr, Slider = slider });
            if (!_active || !_hasFocus) return false;
            return (sr.center - _focusRect.center).sqrMagnitude < 9f && Mathf.Abs(sr.width - _focusRect.width) < 3f;
        }

        public static bool TakeClick()
        {
            if (!_click) return false;
            _click = false;
            return true;
        }

        public static int TakeSliderStep()
        {
            int s = _sliderStep;
            _sliderStep = 0;
            return s;
        }

        private static Rect ToScreen(Rect r)
        {
            Matrix4x4 m = GUI.matrix;
            Vector3 a = m.MultiplyPoint3x4(new Vector3(r.xMin, r.yMin, 0f));
            Vector3 b = m.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
