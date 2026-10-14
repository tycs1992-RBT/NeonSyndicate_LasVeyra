using UnityEngine;
#if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
#endif

namespace NeonSyndicate
{
    public enum PadButton
    {
        South, East, West, North,
        LB, RB, LT, RT,
        L3, R3,
        DUp, DDown, DLeft, DRight,
        Start, Select, Touchpad,
        Count
    }

    public enum PadFamily { None, Xbox, PlayStation, Switch, Generic }

    /// <summary>
    /// Gamepad layer on top of Unity's Input System package (Xbox, DualSense, DualShock 4, Switch Pro and generic pads).
    /// Polled lazily once per frame. Adds what gameplay needs on top of raw buttons: taps versus holds, chords,
    /// rumble with decay, the DualSense light bar and button names that match the connected pad.
    /// Without the Input System package (or with Active Input Handling set to the old Input Manager only) every
    /// query returns false or zero, so the keyboard and mouse keep working unchanged.
    /// </summary>
    public static class Pad
    {
        /// <summary>Presses shorter than this are taps; longer ones become holds.</summary>
        public const float HoldTime = 0.3f;
        public const float TriggerThreshold = 0.35f;

        private const int N = (int)PadButton.Count;
        private static readonly bool[] _held = new bool[N];
        private static readonly bool[] _prev = new bool[N];
        private static readonly float[] _pressedAt = new float[N];
        private static readonly float[] _releasedAfter = new float[N];
        private static readonly bool[] _consumed = new bool[N];
        private static readonly bool[] _holdFired = new bool[N];
        private static int _frame = -1;
        private static float _rumbleLow, _rumbleHigh, _rumbleUntil, _rumbleSentLow = -1f, _rumbleSentHigh = -1f;
        private static float _lowDecay, _highDecay;
        private static Color _light = Color.black;
        private static Color _lightSent = new Color(-1f, -1f, -1f);
        private static float _lightSentTime;

        public static bool Connected { get; private set; }
        public static PadFamily Family { get; private set; }
        public static string DeviceName { get; private set; }
        /// <summary>True when the pad was the last thing the player touched (button prompts follow it).</summary>
        public static bool LastUsed { get; private set; }
        public static Vector2 LeftStick { get; private set; }
        public static Vector2 RightStick { get; private set; }
        public static float LeftTrigger { get; private set; }
        public static float RightTrigger { get; private set; }

        public static void ResetStatics()
        {
            for (int i = 0; i < N; i++)
            {
                _held[i] = _prev[i] = _consumed[i] = _holdFired[i] = false;
                _pressedAt[i] = 0f;
                _releasedAfter[i] = -1f;
            }
            _frame = -1;
            _rumbleLow = _rumbleHigh = 0f;
            _rumbleUntil = 0f;
            _rumbleSentLow = _rumbleSentHigh = -1f;
            _light = Color.black;
            _lightSent = new Color(-1f, -1f, -1f);
            Connected = false;
            LastUsed = false;
            Family = PadFamily.None;
            DeviceName = null;
            LeftStick = RightStick = Vector2.zero;
            LeftTrigger = RightTrigger = 0f;
        }

        /// <summary>Called by the keyboard and mouse side when they are used, so prompts switch back to keys.</summary>
        public static void NotifyKeyboardUsed()
        {
            LastUsed = false;
        }

        // ------------------------------------------------------------------ queries

        public static bool Held(PadButton b) { Poll(); return _held[(int)b]; }
        public static bool Down(PadButton b) { Poll(); return _held[(int)b] && !_prev[(int)b]; }
        public static bool Up(PadButton b) { Poll(); return !_held[(int)b] && _prev[(int)b]; }

        /// <summary>Seconds the button has been held (0 when up).</summary>
        public static float HeldTime(PadButton b)
        {
            Poll();
            return _held[(int)b] ? Time.unscaledTime - _pressedAt[(int)b] : 0f;
        }

        /// <summary>Released this frame after a short press that nothing else claimed.</summary>
        public static bool Tapped(PadButton b)
        {
            Poll();
            int i = (int)b;
            return Up(b) && !_consumed[i] && !_holdFired[i] && _releasedAfter[i] >= 0f && _releasedAfter[i] < HoldTime;
        }

        /// <summary>True on the single frame a press turns into a hold.</summary>
        public static bool HoldStarted(PadButton b)
        {
            Poll();
            int i = (int)b;
            if (!_held[i] || _consumed[i] || _holdFired[i]) return false;
            if (Time.unscaledTime - _pressedAt[i] < HoldTime) return false;
            _holdFired[i] = true;
            return true;
        }

        /// <summary>The current press of this button no longer counts as a tap or a hold (used by chords).</summary>
        public static void Consume(PadButton b)
        {
            Poll();
            _consumed[(int)b] = true;
        }

        public static bool AnyDown()
        {
            Poll();
            for (int i = 0; i < N; i++) if (_held[i] && !_prev[i]) return true;
            return false;
        }

        // ------------------------------------------------------------------ feedback

        /// <summary>Rumble: low = heavy motor (impacts), high = light motor (gunfire, sparks). Stacks with what is playing.</summary>
        public static void Rumble(float low, float high, float seconds)
        {
            if (!Settings.Vibration || !Connected) return;
            float now = Time.unscaledTime;
            if (now > _rumbleUntil)
            {
                _rumbleLow = 0f;
                _rumbleHigh = 0f;
            }
            _rumbleLow = Mathf.Clamp01(Mathf.Max(_rumbleLow, low));
            _rumbleHigh = Mathf.Clamp01(Mathf.Max(_rumbleHigh, high));
            _rumbleUntil = Mathf.Max(_rumbleUntil, now + Mathf.Max(0.02f, seconds));
            _lowDecay = _rumbleLow / Mathf.Max(0.05f, seconds);
            _highDecay = _rumbleHigh / Mathf.Max(0.05f, seconds);
        }

        public static void StopRumble()
        {
            _rumbleLow = _rumbleHigh = 0f;
            _rumbleUntil = 0f;
        }

        /// <summary>DualSense and DualShock 4 light bar colour (ignored by other pads).</summary>
        public static void SetLight(Color c)
        {
            _light = c;
        }

        // ------------------------------------------------------------------ names

        /// <summary>Short name of a button in the style of the connected pad (Cross on PlayStation, A on Xbox).</summary>
        public static string Glyph(PadButton b)
        {
            Poll();
            bool ps = Family == PadFamily.PlayStation;
            bool sw = Family == PadFamily.Switch;
            switch (b)
            {
                case PadButton.South: return ps ? "Cross" : (sw ? "B" : "A");
                case PadButton.East: return ps ? "Circle" : (sw ? "A" : "B");
                case PadButton.West: return ps ? "Square" : (sw ? "Y" : "X");
                case PadButton.North: return ps ? "Triangle" : (sw ? "X" : "Y");
                case PadButton.LB: return ps ? "L1" : (sw ? "L" : "LB");
                case PadButton.RB: return ps ? "R1" : (sw ? "R" : "RB");
                case PadButton.LT: return ps ? "L2" : (sw ? "ZL" : "LT");
                case PadButton.RT: return ps ? "R2" : (sw ? "ZR" : "RT");
                case PadButton.L3: return ps ? "L3" : "LS";
                case PadButton.R3: return ps ? "R3" : "RS";
                case PadButton.DUp: return "D-pad Up";
                case PadButton.DDown: return "D-pad Down";
                case PadButton.DLeft: return "D-pad Left";
                case PadButton.DRight: return "D-pad Right";
                case PadButton.Start: return ps ? "Options" : (sw ? "+" : "Menu");
                case PadButton.Select: return ps ? "Create" : (sw ? "-" : "View");
                case PadButton.Touchpad: return ps ? "Touchpad" : "View";
                default: return b.ToString();
            }
        }

        /// <summary>Glyph wrapped in brackets for prompts, e.g. "[Triangle]".</summary>
        public static string Tag(PadButton b)
        {
            return "[" + Glyph(b) + "]";
        }

        // ------------------------------------------------------------------ polling

        private static void Poll()
        {
            int f = Time.frameCount;
            if (f == _frame) return;
            _frame = f;
            for (int i = 0; i < N; i++) _prev[i] = _held[i];
            ReadDevice();
            float now = Time.unscaledTime;
            bool any = false;
            for (int i = 0; i < N; i++)
            {
                if (_held[i] && !_prev[i])
                {
                    _pressedAt[i] = now;
                    _consumed[i] = false;
                    _holdFired[i] = false;
                    _releasedAfter[i] = -1f;
                    any = true;
                }
                else if (!_held[i] && _prev[i])
                {
                    _releasedAfter[i] = now - _pressedAt[i];
                }
            }
            if (any || LeftStick.sqrMagnitude > 0.09f || RightStick.sqrMagnitude > 0.09f || LeftTrigger > 0.2f || RightTrigger > 0.2f)
                LastUsed = true;
            UpdateFeedback(now);
        }

#if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM
        private static Gamepad _sentTo;

        private static void ReadDevice()
        {
            Gamepad g = null;
            try { g = Gamepad.current; }
            catch (System.Exception) { g = null; }
            if (g == null || !g.added)
            {
                if (Connected)
                {
                    Connected = false;
                    LastUsed = false;
                }
                for (int i = 0; i < N; i++) _held[i] = false;
                LeftStick = RightStick = Vector2.zero;
                LeftTrigger = RightTrigger = 0f;
                return;
            }
            if (!Connected || DeviceName != g.displayName)
            {
                Connected = true;
                DeviceName = g.displayName;
                if (g is DualShockGamepad) Family = PadFamily.PlayStation;
                else if (g is SwitchProControllerHID) Family = PadFamily.Switch;
                else if (g.GetType().Name.Contains("XInput") || g.GetType().Name.Contains("Xbox")) Family = PadFamily.Xbox;
                else Family = PadFamily.Generic;
                _rumbleSentLow = _rumbleSentHigh = -1f;
                _lightSent = new Color(-1f, -1f, -1f);
                Log.Info("Controller connected: " + DeviceName + " (" + Family + ")");
            }
            LeftStick = Radial(g.leftStick.ReadValue());
            RightStick = Radial(g.rightStick.ReadValue());
            LeftTrigger = g.leftTrigger.ReadValue();
            RightTrigger = g.rightTrigger.ReadValue();
            _held[(int)PadButton.South] = g.buttonSouth.isPressed;
            _held[(int)PadButton.East] = g.buttonEast.isPressed;
            _held[(int)PadButton.West] = g.buttonWest.isPressed;
            _held[(int)PadButton.North] = g.buttonNorth.isPressed;
            _held[(int)PadButton.LB] = g.leftShoulder.isPressed;
            _held[(int)PadButton.RB] = g.rightShoulder.isPressed;
            _held[(int)PadButton.LT] = LeftTrigger > TriggerThreshold;
            _held[(int)PadButton.RT] = RightTrigger > TriggerThreshold;
            _held[(int)PadButton.L3] = g.leftStickButton.isPressed;
            _held[(int)PadButton.R3] = g.rightStickButton.isPressed;
            _held[(int)PadButton.DUp] = g.dpad.up.isPressed;
            _held[(int)PadButton.DDown] = g.dpad.down.isPressed;
            _held[(int)PadButton.DLeft] = g.dpad.left.isPressed;
            _held[(int)PadButton.DRight] = g.dpad.right.isPressed;
            _held[(int)PadButton.Start] = g.startButton.isPressed;
            _held[(int)PadButton.Select] = g.selectButton.isPressed;
            DualShockGamepad ds = g as DualShockGamepad;
            _held[(int)PadButton.Touchpad] = ds != null && ds.touchpadButton != null && ds.touchpadButton.isPressed;
        }

        private static void UpdateFeedback(float now)
        {
            Gamepad g = null;
            try { g = Gamepad.current; }
            catch (System.Exception) { g = null; }
            if (g == null || !Connected) return;
            if (g != _sentTo)
            {
                _sentTo = g;
                _rumbleSentLow = _rumbleSentHigh = -1f;
                _lightSent = new Color(-1f, -1f, -1f);
            }
            float dt = Time.unscaledDeltaTime;
            float low = 0f, high = 0f;
            bool paused = Time.timeScale <= 0.0001f && !Game.IsPlaying;
            if (now < _rumbleUntil && Settings.Vibration && !paused && Application.isFocused)
            {
                _rumbleLow = Mathf.Max(0f, _rumbleLow - _lowDecay * dt * 0.5f);
                _rumbleHigh = Mathf.Max(0f, _rumbleHigh - _highDecay * dt * 0.5f);
                low = _rumbleLow;
                high = _rumbleHigh;
            }
            if (Mathf.Abs(low - _rumbleSentLow) > 0.02f || Mathf.Abs(high - _rumbleSentHigh) > 0.02f)
            {
                _rumbleSentLow = low;
                _rumbleSentHigh = high;
                try { g.SetMotorSpeeds(low, high); }
                catch (System.Exception) { }
            }
            DualShockGamepad ds = g as DualShockGamepad;
            if (ds != null && now - _lightSentTime > 0.1f)
            {
                Color c = _light;
                if (Mathf.Abs(c.r - _lightSent.r) + Mathf.Abs(c.g - _lightSent.g) + Mathf.Abs(c.b - _lightSent.b) > 0.04f)
                {
                    _lightSent = c;
                    _lightSentTime = now;
                    try { ds.SetLightBarColor(c); }
                    catch (System.Exception) { }
                }
            }
        }

        /// <summary>Called when the game quits or loses focus so a pad never keeps buzzing.</summary>
        public static void Silence()
        {
            StopRumble();
            try
            {
                Gamepad g = Gamepad.current;
                if (g != null) g.SetMotorSpeeds(0f, 0f);
            }
            catch (System.Exception) { }
            _rumbleSentLow = _rumbleSentHigh = 0f;
        }
#else
        private static void ReadDevice()
        {
            Connected = false;
        }

        private static void UpdateFeedback(float now)
        {
        }

        public static void Silence()
        {
            StopRumble();
        }
#endif

        /// <summary>Rescales a stick that already had its dead zone removed so small deflections stay precise.</summary>
        private static Vector2 Radial(Vector2 v)
        {
            float m = v.magnitude;
            if (m < 0.001f) return Vector2.zero;
            if (m > 1f) return v / m;
            return v;
        }

        /// <summary>Response curve for camera sticks: fine control near the centre, full speed at the rim.</summary>
        public static Vector2 Curve(Vector2 v, float exponent)
        {
            float m = v.magnitude;
            if (m < 0.001f) return Vector2.zero;
            return v / m * Mathf.Pow(Mathf.Clamp01(m), exponent);
        }
    }
}
