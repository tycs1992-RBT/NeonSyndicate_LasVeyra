#if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define NS_KBM_NEW
#endif
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && NS_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace NeonSyndicate
{
    /// <summary>
    /// Central key map. Docs/CONTROLS.md mirrors this table exactly.
    /// </summary>
    public static class Keys
    {
        public const KeyCode Forward = KeyCode.W;
        public const KeyCode Back = KeyCode.S;
        public const KeyCode Left = KeyCode.A;
        public const KeyCode Right = KeyCode.D;
        public const KeyCode Sprint = KeyCode.LeftShift;
        public const KeyCode Walk = KeyCode.LeftAlt;
        public const KeyCode Jump = KeyCode.Space;
        public const KeyCode Crouch = KeyCode.LeftControl;
        public const KeyCode Reload = KeyCode.R;
        public const KeyCode Interact = KeyCode.E;
        public const KeyCode Vehicle = KeyCode.F;
        public const KeyCode WeaponWheel = KeyCode.Tab;
        public const KeyCode Throw = KeyCode.G;
        public const KeyCode Ability = KeyCode.CapsLock;
        public const KeyCode ThirdEye = KeyCode.T;
        public const KeyCode Power = KeyCode.B;
        public const KeyCode PowerNext = KeyCode.RightBracket;
        public const KeyCode PowerPrev = KeyCode.LeftBracket;
        public const KeyCode Heal = KeyCode.Q;
        public const KeyCode LockOn = KeyCode.Z;
        public const KeyCode Gear = KeyCode.I;
        public const KeyCode Camera = KeyCode.V;
        public const KeyCode Shoulder = KeyCode.C;
        /// <summary>Take cover, leave it, round a corner (tap) or dash to the aimed cover (hold). Docs/COVER.md.</summary>
        public const KeyCode Cover = KeyCode.X;
        public const KeyCode Phone = KeyCode.P;
        public const KeyCode Map = KeyCode.M;
        public const KeyCode Pause = KeyCode.Escape;
        public const KeyCode Handbrake = KeyCode.Space;
        public const KeyCode Boost = KeyCode.LeftShift;
        public const KeyCode Horn = KeyCode.E;
        public const KeyCode Headlights = KeyCode.H;
        public const KeyCode LookBehind = KeyCode.C;
        public const KeyCode RadioNext = KeyCode.Period;
        public const KeyCode RadioPrev = KeyCode.Comma;
        public const KeyCode RadioOff = KeyCode.Slash;
        public const KeyCode JobMode = KeyCode.J;
        public const KeyCode Help = KeyCode.F1;
        public const KeyCode Debug = KeyCode.F2;
        public const KeyCode HideHud = KeyCode.F3;
        public const KeyCode Screenshot = KeyCode.F12;
        /// <summary>Gadget slots 1 and 2 (the keypad's 1 and 2 work too). They pick no weapon while a gadget sits in the slot.</summary>
        public const KeyCode Gadget1 = KeyCode.Alpha1;
        public const KeyCode Gadget2 = KeyCode.Alpha2;
    }

    /// <summary>
    /// Input facade. Keyboard and mouse go through the classic Input Manager when it is enabled, or through the
    /// Input System package when the project is set to the new system only, so the game runs in every Active Input
    /// Handling mode. Gamepads always come from the Input System (see Pad). Gameplay code asks for actions (Act)
    /// that merge both, so every feature works on keyboard and controller.
    /// </summary>
    public static class GInput
    {
        public static bool Broken { get; private set; }
        public static string Problem { get; private set; }

        /// <summary>True while a menu, the phone, the map or the weapon wheel owns the mouse and keyboard.</summary>
        public static bool GameplayBlocked;

        private static bool _axisFallback;
        private static Vector3 _lastMousePos;
        private static int _scrollFrame = -1;
        private static float _scrollCache;
        private static int _pollFrame = -1;
        private static float _sprintDownAt = -1f;
        private static bool _sprintTap;
        private static float _lookRamp;
        private static float _rimTime;

        public static void ResetStatics()
        {
            Broken = false;
            Problem = null;
            GameplayBlocked = false;
            _mouseLatch = false;
            _axisFallback = false;
            _scrollFrame = -1;
            _pollFrame = -1;
            _sprintDownAt = -1f;
            _sprintTap = false;
            _lookRamp = 0f;
            _rimTime = 0f;
            _navHeldDir = 0;
            _navFrame = -1;
            Pad.ResetStatics();
#if NS_KBM_NEW
            _textHooked = null;
            _typed.Length = 0;
            _typedFrame = string.Empty;
#endif
        }

        private static void Fail(Exception e)
        {
            if (Broken) return;
            Broken = true;
            Problem = "Input is switched to the new Input System only and the Input System package is missing. In Unity open Edit > Project Settings > Player > Other Settings > Active Input Handling, choose 'Both', then let Unity restart.";
            Log.Error(Problem + " (" + e.Message + ")");
        }

        /// <summary>Once per frame: tap detection for keys with tap and hold meanings, and which device is in use.</summary>
        private static void Poll()
        {
            int f = Time.frameCount;
            if (f == _pollFrame) return;
            _pollFrame = f;
            _sprintTap = false;
            bool sh = RawHeld(Keys.Sprint);
            if (sh && _sprintDownAt < 0f) _sprintDownAt = Time.unscaledTime;
            else if (!sh && _sprintDownAt >= 0f)
            {
                _sprintTap = Time.unscaledTime - _sprintDownAt < 0.25f;
                _sprintDownAt = -1f;
            }
#if NS_KBM_NEW
            _typedFrame = _typed.ToString();
            _typed.Length = 0;
            HookText();
#endif
            if (RawAnyKeyDown() || RawMouseMoved()) Pad.NotifyKeyboardUsed();
        }

        // ------------------------------------------------------------------ keyboard and mouse backend

#if NS_KBM_NEW
        private static readonly Dictionary<KeyCode, Key> _keyMap = new Dictionary<KeyCode, Key>();
        private static readonly StringBuilder _typed = new StringBuilder();
        private static string _typedFrame = string.Empty;
        private static Keyboard _textHooked;

        private static void HookText()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || kb == _textHooked) return;
            if (_textHooked != null) _textHooked.onTextInput -= OnText;
            kb.onTextInput += OnText;
            _textHooked = kb;
        }

        private static void OnText(char c)
        {
            if (_typed.Length < 64) _typed.Append(c);
        }

        private static Key ToKey(KeyCode k)
        {
            Key key;
            if (_keyMap.TryGetValue(k, out key)) return key;
            switch (k)
            {
                case KeyCode.Return: key = Key.Enter; break;
                case KeyCode.KeypadEnter: key = Key.NumpadEnter; break;
                case KeyCode.LeftControl: key = Key.LeftCtrl; break;
                case KeyCode.RightControl: key = Key.RightCtrl; break;
                case KeyCode.LeftAlt: key = Key.LeftAlt; break;
                case KeyCode.RightAlt: key = Key.RightAlt; break;
                case KeyCode.LeftBracket: key = Key.LeftBracket; break;
                case KeyCode.RightBracket: key = Key.RightBracket; break;
                case KeyCode.BackQuote: key = Key.Backquote; break;
                default:
                {
                    string n = k.ToString();
                    if (n.StartsWith("Alpha")) n = "Digit" + n.Substring(5);
                    else if (n.StartsWith("Keypad")) n = "Numpad" + n.Substring(6);
                    if (!Enum.TryParse(n, out key)) key = Key.None;
                    break;
                }
            }
            _keyMap[k] = key;
            return key;
        }

        private static bool RawHeld(KeyCode k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key key = ToKey(k);
            return key != Key.None && kb[key].isPressed;
        }

        private static bool RawDown(KeyCode k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key key = ToKey(k);
            return key != Key.None && kb[key].wasPressedThisFrame;
        }

        private static bool RawUp(KeyCode k)
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            Key key = ToKey(k);
            return key != Key.None && kb[key].wasReleasedThisFrame;
        }

        private static UnityEngine.InputSystem.Controls.ButtonControl MouseButton(int b)
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return null;
            return b == 0 ? m.leftButton : b == 1 ? m.rightButton : m.middleButton;
        }

        private static bool RawMouse(int b) { UnityEngine.InputSystem.Controls.ButtonControl c = MouseButton(b); return c != null && c.isPressed; }
        private static bool RawMouseDown(int b) { UnityEngine.InputSystem.Controls.ButtonControl c = MouseButton(b); return c != null && c.wasPressedThisFrame; }
        private static bool RawMouseUp(int b) { UnityEngine.InputSystem.Controls.ButtonControl c = MouseButton(b); return c != null && c.wasReleasedThisFrame; }

        private static Vector3 RawMousePosition()
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return Vector3.zero;
            Vector2 p = m.position.ReadValue();
            return new Vector3(p.x, p.y, 0f);
        }

        private static Vector2 RawDelta()
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.1f;
        }

        private static float RawScroll()
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return 0f;
            float y = m.scroll.ReadValue().y;
            return Mathf.Abs(y) >= 10f ? y / 120f : y;
        }

        private static string RawTyped() { return _typedFrame; }

        private static bool RawAnyKeyDown()
        {
            Keyboard kb = Keyboard.current;
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame) || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame));
        }

        private static bool RawMouseMoved()
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            return m != null && m.delta.ReadValue().sqrMagnitude > 9f;
        }
#else
        private static bool RawHeld(KeyCode k)
        {
            if (Broken) return false;
            try { return Input.GetKey(k); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static bool RawDown(KeyCode k)
        {
            if (Broken) return false;
            try { return Input.GetKeyDown(k); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static bool RawUp(KeyCode k)
        {
            if (Broken) return false;
            try { return Input.GetKeyUp(k); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static bool RawMouse(int b)
        {
            if (Broken) return false;
            try { return Input.GetMouseButton(b); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static bool RawMouseDown(int b)
        {
            if (Broken) return false;
            try { return Input.GetMouseButtonDown(b); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static bool RawMouseUp(int b)
        {
            if (Broken) return false;
            try { return Input.GetMouseButtonUp(b); }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static Vector3 RawMousePosition()
        {
            if (Broken) return Vector3.zero;
            try { return Input.mousePosition; }
            catch (InvalidOperationException e) { Fail(e); return Vector3.zero; }
        }

        private static Vector2 RawDelta()
        {
            if (Broken) return Vector2.zero;
            if (!_axisFallback)
            {
                try
                {
                    return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                }
                catch (ArgumentException)
                {
                    _axisFallback = true;
                    Log.Warn("Mouse X/Y axes are missing from the Input Manager; using cursor-delta fallback.");
                }
                catch (InvalidOperationException e)
                {
                    Fail(e);
                    return Vector2.zero;
                }
            }
            Vector3 mp = RawMousePosition();
            Vector3 d = mp - _lastMousePos;
            _lastMousePos = mp;
            return new Vector2(d.x, d.y) * 0.1f;
        }

        private static float RawScroll()
        {
            if (Broken) return 0f;
            try { return Input.mouseScrollDelta.y; }
            catch (InvalidOperationException e) { Fail(e); return 0f; }
        }

        private static string RawTyped()
        {
            if (Broken) return string.Empty;
            try { return Input.inputString; }
            catch (InvalidOperationException e) { Fail(e); return string.Empty; }
        }

        private static bool RawAnyKeyDown()
        {
            if (Broken) return false;
            try { return Input.anyKeyDown; }
            catch (InvalidOperationException e) { Fail(e); return false; }
        }

        private static Vector3 _lastMovePos;

        private static bool RawMouseMoved()
        {
            Vector3 p = RawMousePosition();
            bool moved = (p - _lastMovePos).sqrMagnitude > 9f;
            _lastMovePos = p;
            return moved;
        }
#endif

        // ------------------------------------------------------------------ public keyboard and mouse api

        public static bool Held(KeyCode key) { Poll(); return RawHeld(key); }
        public static bool Down(KeyCode key) { Poll(); return RawDown(key); }
        public static bool Up(KeyCode key) { Poll(); return RawUp(key); }
        public static bool Mouse(int button) { Poll(); return RawMouse(button); }
        public static bool MouseDown(int button) { Poll(); return RawMouseDown(button); }
        public static bool MouseUp(int button) { Poll(); return RawMouseUp(button); }
        public static Vector3 MousePosition { get { return RawMousePosition(); } }
        public static string TypedText { get { Poll(); return RawTyped(); } }
        public static bool AnyKeyDown { get { Poll(); return RawAnyKeyDown() || Pad.AnyDown(); } }

        /// <summary>Raw mouse delta in "mouse units" (before sensitivity).</summary>
        public static Vector2 RawMouseDelta { get { return RawDelta(); } }

        /// <summary>
        /// Camera look in degrees for this frame: mouse delta with sensitivity, plus the right stick (response curve,
        /// rim acceleration) scaled by real time so slow motion never slows the camera.
        /// </summary>
        public static Vector2 Look
        {
            get
            {
                if (GameplayBlocked) return Vector2.zero;
                Vector2 raw = RawMouseDelta;
                float sens = Settings.MouseSensitivity;
                Vector2 look = new Vector2(raw.x * sens, raw.y * sens * (Settings.InvertY ? -1f : 1f));
                Vector2 stick = Pad.RightStick;
                if (stick.sqrMagnitude > 0.0001f)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    Vector2 c = Pad.Curve(stick, 1.7f);
                    if (stick.magnitude > 0.92f) _rimTime += dt;
                    else _rimTime = 0f;
                    _lookRamp = Mathf.MoveTowards(_lookRamp, _rimTime > 0.22f ? 1f : 0f, dt * (_rimTime > 0.22f ? 2.2f : 8f));
                    float speed = Settings.PadLookSpeed * (1f + _lookRamp * 0.65f) * PadLookScale;
                    look.x += c.x * speed * dt;
                    look.y += c.y * speed * 0.62f * dt * (Settings.PadInvertY ? -1f : 1f);
                }
                else
                {
                    _rimTime = 0f;
                    _lookRamp = 0f;
                }
                return look;
            }
        }

        /// <summary>Set by the camera each frame: aim friction slows the stick when the crosshair is on a target.</summary>
        public static float PadLookScale = 1f;

        public static float Scroll
        {
            get
            {
                if (_scrollFrame == Time.frameCount) return _scrollCache;
                _scrollFrame = Time.frameCount;
                _scrollCache = RawScroll();
                return _scrollCache;
            }
        }

        /// <summary>WASD, arrow keys or the left stick (analog). X = strafe, Y = forward.</summary>
        public static Vector2 Move
        {
            get
            {
                if (GameplayBlocked) return Vector2.zero;
                float x = 0f;
                float y = 0f;
                if (Held(Keys.Forward) || Held(KeyCode.UpArrow)) y += 1f;
                if (Held(Keys.Back) || Held(KeyCode.DownArrow)) y -= 1f;
                if (Held(Keys.Right) || Held(KeyCode.RightArrow)) x += 1f;
                if (Held(Keys.Left) || Held(KeyCode.LeftArrow)) x -= 1f;
                Vector2 v = new Vector2(x, y);
                if (v.sqrMagnitude > 1f) v.Normalize();
                Vector2 s = Pad.LeftStick;
                return s.sqrMagnitude > v.sqrMagnitude ? s : v;
            }
        }

        /// <summary>True for one frame when the sprint key was tapped (released quickly): the dodge roll.</summary>
        public static bool SprintTapped { get { Poll(); return _sprintTap; } }

        public static bool GameHeld(KeyCode key)
        {
            return !GameplayBlocked && Held(key);
        }

        public static bool GameDown(KeyCode key)
        {
            return !GameplayBlocked && Down(key);
        }

        private static bool _mouseLatch;

        /// <summary>
        /// Called when a menu or the phone closes from a mouse click: gameplay ignores the mouse buttons until they
        /// are released, so the click that pressed "Leave" doesn't also fire the gun.
        /// </summary>
        public static void LatchMouse()
        {
            _mouseLatch = true;
        }

        private static bool MouseFree()
        {
            if (!_mouseLatch) return true;
            if (!Mouse(0) && !Mouse(1) && !Pad.Held(PadButton.South) && !Pad.Held(PadButton.RT))
            {
                _mouseLatch = false;
                return true;
            }
            return false;
        }

        public static bool GameMouse(int button)
        {
            return !GameplayBlocked && MouseFree() && Mouse(button);
        }

        public static bool GameMouseDown(int button)
        {
            return !GameplayBlocked && MouseFree() && MouseDown(button);
        }

        public static bool Fire { get { return GameMouse(0) || Act.PadFireHeld; } }
        public static bool FireDown { get { return GameMouseDown(0) || Act.PadFirePressed; } }
        public static bool Aim { get { return GameMouse(1) || (!GameplayBlocked && Pad.Held(PadButton.LT)); } }

        /// <summary>Returns 1..9 for a number key pressed this frame, else 0.</summary>
        public static int NumberKeyDown()
        {
            if (GameplayBlocked) return 0;
            for (int i = 1; i <= 9; i++)
            {
                // Keys 1 and 2 belong to the gadget slots while a gadget sits in them (GadgetSystem).
                if (i <= 2 && Game.Gadgets != null && Game.Gadgets.ClaimsNumberKey(i)) continue;
                if (Down(KeyCode.Alpha0 + i) || Down(KeyCode.Keypad0 + i)) return i;
            }
            return 0;
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // ------------------------------------------------------------------ menu navigation

        private static int _navHeldDir;
        private static float _navNext;

        /// <summary>
        /// Menu direction pressed this frame (keyboard arrows, D-pad or a flick of the left stick, with key repeat):
        /// 0 none, 1 up, 2 down, 3 left, 4 right.
        /// </summary>
        public static int NavDirection()
        {
            if (_navFrame == Time.frameCount) return _navCache;
            _navFrame = Time.frameCount;
            _navCache = ComputeNav();
            return _navCache;
        }

        private static int _navFrame = -1;
        private static int _navCache;

        private static int ComputeNav()
        {
            int dir = 0;
            if (Down(KeyCode.UpArrow)) return 1;
            if (Down(KeyCode.DownArrow)) return 2;
            if (Down(KeyCode.LeftArrow)) return 3;
            if (Down(KeyCode.RightArrow)) return 4;
            if (!Pad.Connected) return 0;
            Vector2 s = Pad.LeftStick;
            int held = 0;
            if (Pad.Held(PadButton.DUp) || s.y > 0.6f) held = 1;
            else if (Pad.Held(PadButton.DDown) || s.y < -0.6f) held = 2;
            else if (Pad.Held(PadButton.DLeft) || s.x < -0.6f) held = 3;
            else if (Pad.Held(PadButton.DRight) || s.x > 0.6f) held = 4;
            float now = Time.unscaledTime;
            if (held == 0)
            {
                _navHeldDir = 0;
                return 0;
            }
            if (held != _navHeldDir)
            {
                _navHeldDir = held;
                _navNext = now + 0.38f;
                return held;
            }
            if (now >= _navNext)
            {
                _navNext = now + 0.11f;
                dir = held;
            }
            return dir;
        }

        public static bool UiConfirm { get { return Down(KeyCode.Return) || Down(KeyCode.KeypadEnter) || Pad.Down(PadButton.South); } }
        public static bool UiBack { get { return Down(KeyCode.Backspace) || Pad.Down(PadButton.East); } }

        /// <summary>
        /// Rewrites keyboard prompts ("Press E to ...") for the controller when it is the active device.
        /// </summary>
        public static string Prompt(string text)
        {
            if (string.IsNullOrEmpty(text) || !Pad.LastUsed || !Pad.Connected) return text;
            return text
                .Replace("Press Enter to retry", "Hold " + Pad.Tag(PadButton.South) + " to retry")
                .Replace("Press E ", "Press " + Pad.Tag(PadButton.North) + " ")
                .Replace("Press F ", "Press " + Pad.Tag(PadButton.North) + " ")
                .Replace("Press J ", "Hold " + Pad.Tag(PadButton.DUp) + " ")
                .Replace("Press Enter", "Press " + Pad.Tag(PadButton.South))
                .Replace("[B] ", Pad.Tag(PadButton.LB) + "+" + Pad.Tag(PadButton.RT) + " ")
                .Replace("[Caps] ", Pad.Tag(PadButton.LB) + "+" + Pad.Tag(PadButton.RB) + " ");
        }
    }

    /// <summary>
    /// Gameplay actions merged across keyboard, mouse and controller. Pad chords: holding guard (LB) turns the
    /// light attack button into the Third Eye and the heavy trigger into casting the selected power.
    /// </summary>
    public static class Act
    {
        private static bool Free { get { return !GInput.GameplayBlocked && !GInput.Broken; } }
        private static bool PadFree { get { return !GInput.GameplayBlocked && Pad.Connected; } }

        // ------------------------------------------------------------------ on foot: movement

        public static bool Jump { get { return Free && (GInput.Down(Keys.Jump) || (PadFree && Pad.Down(PadButton.South))); } }
        public static bool SprintHeld { get { return Free && (GInput.Held(Keys.Sprint) || (PadFree && Pad.HeldTime(PadButton.East) >= 0.18f)); } }
        public static bool Dodge { get { return Free && (GInput.SprintTapped || (PadFree && Pad.Tapped(PadButton.East))); } }
        public static bool Crouch { get { return Free && (GInput.Down(Keys.Crouch) || (PadFree && Pad.Down(PadButton.L3))); } }
        public static bool WalkHeld { get { return Free && GInput.Held(Keys.Walk); } }

        // ------------------------------------------------------------------ on foot: travel powers (Docs/TRAVEL.md)

        /// <summary>Jump is held: charges a Super Leap while crouched, glides in the air, rises while flying.</summary>
        public static bool JumpHeld { get { return Free && (GInput.Held(Keys.Jump) || (PadFree && Pad.Held(PadButton.South))); } }
        /// <summary>The crouch button pressed while sprinting switches Surge (super speed) on; pressed during Surge, off.</summary>
        public static bool SurgeToggle { get { return Crouch; } }
        /// <summary>Flying: rise (Jump held, Space or A / Cross).</summary>
        public static bool FlyUpHeld { get { return JumpHeld; } }
        /// <summary>Flying: sink (crouch held, Left Ctrl or L3).</summary>
        public static bool FlyDownHeld { get { return Free && (GInput.Held(Keys.Crouch) || (PadFree && Pad.Held(PadButton.L3))); } }
        /// <summary>Flying: fly fast (sprint held, Left Shift or B / Circle).</summary>
        public static bool FlyFastHeld { get { return SprintHeld; } }

        // ------------------------------------------------------------------ on foot: interaction

        public static bool InteractKey { get { return Free && GInput.Down(Keys.Interact); } }
        public static bool VehicleKey { get { return Free && GInput.Down(Keys.Vehicle); } }
        /// <summary>The pad's one context button: talk, use, open, or get into the nearest car.</summary>
        public static bool PadUse { get { return PadFree && Pad.Down(PadButton.North); } }

        // ------------------------------------------------------------------ on foot: combat

        public static bool LockOn { get { return Free && (GInput.Down(Keys.LockOn) || GInput.MouseDown(2) || (PadFree && Pad.Down(PadButton.R3) && !Pad.Held(PadButton.LT))); } }
        public static bool Heal { get { return Free && (GInput.Down(Keys.Heal) || (PadFree && Pad.Down(PadButton.West))); } }
        public static bool PadLight { get { return PadFree && Pad.Down(PadButton.RB) && !Pad.Held(PadButton.LB); } }
        public static bool PadHeavy { get { return PadFree && Pad.Down(PadButton.RT) && !Pad.Held(PadButton.LB); } }
        public static bool PadHeavyHeld { get { return PadFree && Pad.Held(PadButton.RT) && !Pad.Held(PadButton.LB); } }
        public static bool PadGuardHeld { get { return PadFree && Pad.Held(PadButton.LB); } }
        public static bool PadGuardPressed { get { return PadFree && Pad.Down(PadButton.LB); } }
        public static bool PadFireHeld { get { return PadFree && Pad.Held(PadButton.RT) && !Pad.Held(PadButton.LB); } }
        public static bool PadFirePressed { get { return PadFree && Pad.Down(PadButton.RT) && !Pad.Held(PadButton.LB); } }

        public static bool ThirdEye
        {
            get
            {
                if (!Free) return false;
                if (GInput.Down(Keys.Ability) || GInput.Down(Keys.ThirdEye)) return true;
                return PadFree && ((Pad.Down(PadButton.RB) && Pad.Held(PadButton.LB)) || (Pad.Down(PadButton.LB) && Pad.Held(PadButton.RB) && Pad.HeldTime(PadButton.RB) < 0.12f));
            }
        }

        public static bool CastPower
        {
            get
            {
                if (!Free) return false;
                if (GInput.Down(Keys.Power)) return true;
                return PadFree && Pad.Down(PadButton.RT) && Pad.Held(PadButton.LB);
            }
        }

        // ------------------------------------------------------------------ Third Eye: tap for bullet time, hold for the Veil

        /// <summary>Two bumper presses this close together (seconds) count as one chord.</summary>
        public const float ChordWindow = 0.12f;

        /// <summary>
        /// The Third Eye input went down this frame: Caps Lock or T, LB+RB on the pad on foot, and in vehicles the D-pad Up
        /// tap or both bumpers pressed together. holdable is false for presses that can only be taps (the D-pad tap, and
        /// Caps Lock where the platform reports its toggle instead of the key), which keep toggling the Third Eye at once.
        /// </summary>
        public static bool ThirdEyePressed(bool inVehicle, out bool holdable)
        {
            holdable = false;
            if (!Free) return false;
            if (GInput.Down(Keys.ThirdEye))
            {
                holdable = true;
                return true;
            }
            if (GInput.Down(Keys.Ability))
            {
                holdable = CapsHoldReliable;
                return true;
            }
            if (!PadFree) return false;
            if (inVehicle)
            {
                if (Pad.Tapped(PadButton.DUp)) return true;
                if (BumperChordDown)
                {
                    holdable = true;
                    return true;
                }
                return false;
            }
            if ((Pad.Down(PadButton.RB) && Pad.Held(PadButton.LB)) || (Pad.Down(PadButton.LB) && Pad.Held(PadButton.RB) && Pad.HeldTime(PadButton.RB) < ChordWindow))
            {
                holdable = true;
                return true;
            }
            return false;
        }

        /// <summary>The Third Eye input is still held: T, Caps Lock (where readable) or both bumpers.</summary>
        public static bool ThirdEyeHeld(bool inVehicle)
        {
            if (!Free) return false;
            if (GInput.Held(Keys.ThirdEye)) return true;
            if (CapsHoldReliable && GInput.Held(Keys.Ability)) return true;
            return PadFree && Pad.Held(PadButton.LB) && Pad.Held(PadButton.RB);
        }

        /// <summary>LB and RB went down together (within ChordWindow of each other) this frame.</summary>
        private static bool BumperChordDown
        {
            get
            {
                return (Pad.Down(PadButton.RB) && Pad.Held(PadButton.LB) && Pad.HeldTime(PadButton.LB) < ChordWindow)
                    || (Pad.Down(PadButton.LB) && Pad.Held(PadButton.RB) && Pad.HeldTime(PadButton.RB) < ChordWindow);
            }
        }

        /// <summary>
        /// In vehicles both bumpers pressed together are the Third Eye chord once the Third Eye is unlocked (and always
        /// inside the Veil, where the hold is the way back out), so they do not also fire a drive-by. Aiming with LB first
        /// and then pressing RB still shoots, exactly as before.
        /// </summary>
        private static bool VehicleEyeChordHeld
        {
            get
            {
                if (!PadFree || !Pad.Held(PadButton.LB) || !Pad.Held(PadButton.RB)) return false;
                if (Game.Abilities == null) return false;
                bool inVeil = Game.Inner != null && Game.Inner.IsInner;
                if (!Game.Abilities.ThirdEyeUnlocked && !inVeil) return false;
                return Mathf.Abs(Pad.HeldTime(PadButton.LB) - Pad.HeldTime(PadButton.RB)) < ChordWindow;
            }
        }

        /// <summary>
        /// Caps Lock's physical key state can be read everywhere except macOS on the legacy Input Manager, which reports
        /// the caps toggle instead. There Caps Lock stays a tap (bullet time) and T is the key to hold.
        /// </summary>
        private static bool CapsHoldReliable
        {
            get
            {
#if NS_KBM_NEW
                return true;
#else
                return Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor;
#endif
            }
        }

        public static bool NextPower { get { return Free && (GInput.Down(Keys.PowerNext) || (PadFree && Pad.Tapped(PadButton.DUp))); } }
        public static bool PrevPower { get { return Free && GInput.Down(Keys.PowerPrev); } }
        public static bool CameraView { get { return Free && (GInput.Down(Keys.Camera) || (PadFree && Pad.HoldStarted(PadButton.DUp))); } }
        public static bool Reload { get { return Free && (GInput.Down(Keys.Reload) || (PadFree && Pad.Tapped(PadButton.DDown))); } }
        public static bool Throw { get { return Free && (GInput.Down(Keys.Throw) || (PadFree && Pad.HoldStarted(PadButton.DDown))); } }
        // D-pad left and right: a tap switches weapons (decided on release), a hold uses the gadget in slot 1 or 2.
        public static bool WeaponNext { get { return Free && PadFree && Pad.Tapped(PadButton.DRight); } }
        public static bool WeaponPrev { get { return Free && PadFree && Pad.Tapped(PadButton.DLeft); } }

        // ------------------------------------------------------------------ gadgets (GadgetSystem)

        private static KeyCode GadgetKey(int slot) { return slot == 0 ? Keys.Gadget1 : Keys.Gadget2; }
        private static KeyCode GadgetPadKey(int slot) { return slot == 0 ? KeyCode.Keypad1 : KeyCode.Keypad2; }
        private static PadButton GadgetButton(int slot) { return slot == 0 ? PadButton.DLeft : PadButton.DRight; }

        /// <summary>Gadget key 1 or 2 (number row or keypad) pressed this frame.</summary>
        public static bool GadgetKeyDown(int slot) { return Free && (GInput.Down(GadgetKey(slot)) || GInput.Down(GadgetPadKey(slot))); }
        public static bool GadgetKeyHeld(int slot) { return Free && (GInput.Held(GadgetKey(slot)) || GInput.Held(GadgetPadKey(slot))); }
        /// <summary>True on the frame a D-pad left (slot 1) or right (slot 2) press becomes a hold. Call it only for a filled slot.</summary>
        public static bool GadgetPadHoldStarted(int slot) { return Free && PadFree && Pad.HoldStarted(GadgetButton(slot)); }
        public static bool GadgetPadHeld(int slot) { return Free && PadFree && Pad.Held(GadgetButton(slot)); }

        // ------------------------------------------------------------------ on foot: cover (Docs/COVER.md)

        /// <summary>The keyboard cover key (X) went down / is held.</summary>
        public static bool CoverKey { get { return Free && GInput.Down(Keys.Cover); } }
        public static bool CoverKeyHeld { get { return Free && GInput.Held(Keys.Cover); } }
        /// <summary>The pad's A (Cross) went down / is held: cover when a cover prompt shows or in cover, else jump.</summary>
        public static bool PadCoverDown { get { return PadFree && Pad.Down(PadButton.South); } }
        public static bool PadCoverHeld { get { return PadFree && Pad.Held(PadButton.South); } }
        /// <summary>The keyboard jump key alone (Space): vaults over low cover while in it.</summary>
        public static bool JumpKey { get { return Free && GInput.Down(Keys.Jump); } }

        public static bool Shoulder(bool aiming)
        {
            return Free && (GInput.Down(Keys.Shoulder) || (aiming && PadFree && Pad.Down(PadButton.R3)));
        }

        // ------------------------------------------------------------------ general

        public static bool Map { get { return GInput.Down(Keys.Map) || (Pad.Connected && (Pad.Tapped(PadButton.Select) || Pad.Down(PadButton.Touchpad))); } }
        public static bool Phone { get { return GInput.Down(Keys.Phone) || (Pad.Connected && Pad.HoldStarted(PadButton.Select)); } }
        public static bool Pause { get { return GInput.Down(Keys.Pause) || (Pad.Connected && Pad.Down(PadButton.Start)); } }
        public static bool Gear { get { return GInput.Down(Keys.Gear); } }

        // ------------------------------------------------------------------ driving

        /// <summary>Throttle -1..1: W/S or the triggers (RT gas, LT brake and reverse).</summary>
        public static float Throttle
        {
            get
            {
                if (!Free) return 0f;
                float t = 0f;
                if (GInput.Held(Keys.Forward) || GInput.Held(KeyCode.UpArrow)) t += 1f;
                if (GInput.Held(Keys.Back) || GInput.Held(KeyCode.DownArrow)) t -= 1f;
                if (PadFree)
                {
                    float p = Pad.RightTrigger - Pad.LeftTrigger;
                    if (Mathf.Abs(p) > Mathf.Abs(t)) t = p;
                }
                return Mathf.Clamp(t, -1f, 1f);
            }
        }

        /// <summary>Steering -1..1: A/D or the left stick with a gentle curve for small corrections.</summary>
        public static float Steer
        {
            get
            {
                if (!Free) return 0f;
                float s = 0f;
                if (GInput.Held(Keys.Right) || GInput.Held(KeyCode.RightArrow)) s += 1f;
                if (GInput.Held(Keys.Left) || GInput.Held(KeyCode.LeftArrow)) s -= 1f;
                if (PadFree)
                {
                    float x = Pad.LeftStick.x;
                    float p = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 1.35f);
                    if (Mathf.Abs(p) > Mathf.Abs(s)) s = p;
                }
                return Mathf.Clamp(s, -1f, 1f);
            }
        }

        public static bool SteerIsAnalog { get { return PadFree && Mathf.Abs(Pad.LeftStick.x) > 0.05f; } }

        public static bool DriveByAim { get { return Free && (GInput.GameMouse(1) || (PadFree && Pad.Held(PadButton.LB))); } }

        public static bool DriveByFireHeld { get { return Free && (GInput.GameMouse(0) || (PadFree && Pad.Held(PadButton.LB) && Pad.Held(PadButton.RB) && !VehicleEyeChordHeld)); } }

        public static bool DriveByFirePressed { get { return Free && (GInput.GameMouseDown(0) || (PadFree && Pad.Held(PadButton.LB) && Pad.Down(PadButton.RB) && !VehicleEyeChordHeld)); } }

        public static bool Handbrake
        {
            get
            {
                if (!Free) return false;
                if (GInput.Held(Keys.Handbrake)) return true;
                return PadFree && (Pad.Held(PadButton.East) || (Pad.Held(PadButton.RB) && !Pad.Held(PadButton.LB)));
            }
        }

        public static bool Boost { get { return Free && (GInput.Held(Keys.Boost) || (PadFree && Pad.Held(PadButton.South))); } }
        public static bool HornHeld { get { return Free && (GInput.Held(Keys.Horn) || (PadFree && Pad.Held(PadButton.L3))); } }
        public static bool HornReleased { get { return GInput.Up(Keys.Horn) || (Pad.Connected && Pad.Up(PadButton.L3)); } }
        public static bool Headlights { get { return Free && (GInput.Down(Keys.Headlights) || (PadFree && Pad.Tapped(PadButton.DDown))); } }
        public static bool LookBehind { get { return Free && (GInput.Held(Keys.LookBehind) || (PadFree && Pad.Held(PadButton.R3))); } }
        public static bool RadioNext { get { return Free && (GInput.Down(Keys.RadioNext) || (PadFree && Pad.Down(PadButton.DRight))); } }
        public static bool RadioPrev { get { return Free && (GInput.Down(Keys.RadioPrev) || (PadFree && Pad.Down(PadButton.DLeft))); } }
        public static bool RadioOff { get { return Free && (GInput.Down(Keys.RadioOff) || (PadFree && Pad.HoldStarted(PadButton.DDown))); } }
        public static bool JobMode { get { return Free && (GInput.Down(Keys.JobMode) || (PadFree && Pad.HoldStarted(PadButton.DUp))); } }
        public static bool ExitVehicle { get { return Free && (GInput.Down(Keys.Vehicle) || (PadFree && Pad.Down(PadButton.North))); } }
        public static bool VehiclePower { get { return Free && (GInput.Down(Keys.Power) || (PadFree && Pad.Down(PadButton.West))); } }
        public static bool VehicleThirdEye { get { return Free && (GInput.Down(Keys.Ability) || GInput.Down(Keys.ThirdEye) || (PadFree && Pad.Tapped(PadButton.DUp))); } }
    }

    /// <summary>
    /// Tells a tap from a hold on one input. Update it every frame with the press event, the held state and unscaled
    /// delta time: it returns Tap when the input is released before HoldSeconds, and Hold on the frame the threshold is
    /// reached (the release that follows is swallowed, so a hold never also counts as a tap). Pure logic.
    /// </summary>
    public sealed class HoldGesture
    {
        public enum Result
        {
            None,
            Tap,
            Hold
        }

        public readonly float HoldSeconds;
        /// <summary>Progress stays 0 for this long, so quick taps never flash a hold indicator.</summary>
        public readonly float ShowAfter;

        /// <summary>True from the press until the release.</summary>
        public bool Active { get; private set; }
        public float HeldFor { get; private set; }
        private bool _fired;

        public HoldGesture(float holdSeconds, float showAfter = 0.12f)
        {
            HoldSeconds = Mathf.Max(0.05f, holdSeconds);
            ShowAfter = Mathf.Clamp(showAfter, 0f, HoldSeconds * 0.9f);
        }

        /// <summary>0..1 toward the hold (0 while idle, before ShowAfter and after the hold fired).</summary>
        public float Progress
        {
            get
            {
                if (!Active || _fired || HeldFor < ShowAfter) return 0f;
                return Mathf.Clamp01((HeldFor - ShowAfter) / (HoldSeconds - ShowAfter));
            }
        }

        public Result Update(bool pressedThisFrame, bool held, float dt)
        {
            if (!Active)
            {
                if (!pressedThisFrame) return Result.None;
                Active = true;
                HeldFor = 0f;
                _fired = false;
            }
            else if (held) HeldFor += Mathf.Max(0f, dt);
            if (held)
            {
                if (!_fired && HeldFor >= HoldSeconds)
                {
                    _fired = true;
                    return Result.Hold;
                }
                return Result.None;
            }
            bool tap = !_fired;
            Reset();
            return tap ? Result.Tap : Result.None;
        }

        public void Reset()
        {
            Active = false;
            HeldFor = 0f;
            _fired = false;
        }
    }
}
