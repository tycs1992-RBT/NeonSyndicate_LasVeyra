using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The in-game HUD: minimap with health/armor/crystal bars, money, wanted heat crystals, weapon and ammo, crosshair and
    /// hit markers, help prompts and notifications, subtitles, objectives, mission timers and counters, banners,
    /// FLATLINED/CUFFED/MISSION COMPLETE messages, district and vehicle name popups, speedometer, radio display, damage
    /// direction indicators, screen fades and the weapon wheel.
    /// </summary>
    public sealed partial class Hud : MonoBehaviour
    {
        private sealed class Note
        {
            public string Text;
            public float Time;
        }

        private sealed class Popup
        {
            public int Amount;
            public float Time;
        }

        public bool Hidden;
        public bool MinimapExpanded;
        public bool WheelOpen { get; private set; }

        private readonly List<Note> _notes = new List<Note>();
        private readonly List<Popup> _money = new List<Popup>();
        private string _subSpeaker;
        private string _subLine;
        private float _subTime;
        private string _bannerTitle;
        private string _bannerSub;
        private Color _bannerColor;
        private float _bannerTime;
        private float _bannerMax;
        private string _bigText;
        private string _bigSub;
        private Color _bigColor;
        private float _bigTime;
        private string _objective;
        private float _objectiveFlash;
        private float _timer = -1f;
        private string _timerLabel;
        private string _counterLabel;
        private int _counterA;
        private int _counterB;
        private string _progressLabel;
        private float _progress;
        private int _progressFrame = -1;
        private float _hitTime;
        private bool _hitKill;
        private float _damageAlpha;
        private Vector3 _damageFrom;
        private float _damageDirTime;
        private string _vehicleName;
        private float _vehicleTime;
        private string _districtName;
        private float _districtTime;
        private District _lastDistrict = (District)(-1);
        // 04E: the one-line district chip on foot (HudLayout04E.DistrictChipFoot), fitted once per district and width.
        private string _districtChip = "";
        private string _districtChipName = "";
        private string _districtChipText = "";
        private int _districtChipSize = 24;
        private float _districtChipFitW = -1f;
        // 04E: the player is in the Underheart this frame (the prompts then start under the heat row).
        private bool _underground;
        private float _fade;
        private float _fadeTarget;
        private float _fadeSpeed = 1f;
        private int _wheelSlot = -1;
        private Vector2 _wheelMouse;
        private float _fpsTimer;
        private int _fpsFrames;
        private float _fps;
        private bool _help;
        private bool _wheelReleaseSelect;

        // ------------------------------------------------------------------ api

        public void Notify(string text, float seconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (Note n in _notes) if (n.Text == text) { n.Time = Mathf.Max(n.Time, seconds); return; }
            _notes.Add(new Note { Text = text, Time = seconds });
            if (_notes.Count > 4) _notes.RemoveAt(0);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.35f);
        }

        public void Subtitle(string speaker, string line, float seconds)
        {
            if (!Settings.Subtitles && speaker != "Police Scanner") return;
            _subSpeaker = speaker;
            _subLine = line;
            _subTime = seconds;
        }

        public void ClearSubtitle()
        {
            _subTime = 0f;
        }

        public void Banner(string title, string sub, Color color, float seconds)
        {
            _bannerTitle = title;
            _bannerSub = sub;
            _bannerColor = color;
            _bannerTime = seconds;
            _bannerMax = seconds;
        }

        public void BigMessage(string text, string sub, Color color, float seconds)
        {
            _bigText = text;
            _bigSub = sub;
            _bigColor = color;
            _bigTime = seconds;
        }

        public void ClearBigMessage()
        {
            _bigTime = 0f;
        }

        public void SetObjective(string text)
        {
            if (_objective != text) _objectiveFlash = 1f;
            _objective = text;
        }

        public void ClearObjective()
        {
            _objective = null;
        }

        public void SetTimer(string label, float seconds)
        {
            _timerLabel = label;
            _timer = seconds;
        }

        public void ClearTimer()
        {
            _timer = -1f;
        }

        public void SetCounter(string label, int a, int b)
        {
            _counterLabel = label;
            _counterA = a;
            _counterB = b;
        }

        public void ClearCounter()
        {
            _counterLabel = null;
        }

        /// <summary>Shows a progress bar for this frame (call every frame while active).</summary>
        public void SetProgress(string label, float progress)
        {
            _progressLabel = label;
            _progress = progress;
            _progressFrame = Time.frameCount;
        }

        public void HitMarker(bool kill)
        {
            _hitTime = 0.25f;
            _hitKill = kill;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Beep, kill ? 0.35f : 0.18f, kill ? 0.8f : 2.2f);
        }

        public void DamageFlash(DamageInfo info)
        {
            _damageAlpha = Mathf.Clamp01(_damageAlpha + info.Amount / 40f + 0.15f);
            if (info.Source != null)
            {
                _damageFrom = info.Source.transform.position;
                _damageDirTime = 1.2f;
            }
        }

        public void MoneyPopup(int amount)
        {
            _money.Add(new Popup { Amount = amount, Time = 2.2f });
            if (_money.Count > 4) _money.RemoveAt(0);
        }

        public void ShowVehicleName(string name)
        {
            _vehicleName = name;
            _vehicleTime = 3.5f;
        }

        public void FadeTo(float alpha, float duration)
        {
            _fadeTarget = Mathf.Clamp01(alpha);
            _fadeSpeed = duration <= 0.001f ? 1000f : 1f / duration;
            if (duration <= 0.001f) _fade = _fadeTarget;
        }

        public float FadeAlpha { get { return _fade; } }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                _notes[i].Time -= dt;
                if (_notes[i].Time <= 0f) _notes.RemoveAt(i);
            }
            for (int i = _money.Count - 1; i >= 0; i--)
            {
                _money[i].Time -= dt;
                if (_money[i].Time <= 0f) _money.RemoveAt(i);
            }
            _subTime -= dt;
            _bannerTime -= dt;
            _bigTime -= dt;
            _hitTime -= dt;
            _damageDirTime -= dt;
            _vehicleTime -= dt;
            _districtTime -= dt;
            _objectiveFlash = Mathf.MoveTowards(_objectiveFlash, 0f, dt);
            _damageAlpha = Mathf.MoveTowards(_damageAlpha, 0f, dt * 0.8f);
            _fade = Mathf.MoveTowards(_fade, _fadeTarget, dt * _fadeSpeed);
            if (_timer > 0f && Game.IsPlaying) _timer = Mathf.Max(0f, _timer - Time.deltaTime);
            _fpsFrames++;
            _fpsTimer += dt;
            if (_fpsTimer >= 0.5f)
            {
                _fps = _fpsFrames / _fpsTimer;
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }
            if (GInput.Down(Keys.HideHud)) Hidden = !Hidden;
            if (GInput.Down(Keys.Help) && Game.IsPlaying) _help = !_help;
            if (Game.Player != null && Game.IsPlaying)
            {
                District d = CityLayout.DistrictAt(Game.PlayerPosition);
                WatchZones(d);
                if (d != _lastDistrict)
                {
                    _lastDistrict = d;
                    string name = CityLayout.DistrictName(d);
                    _districtName = name;
                    _districtTime = 4f;
                    GangDef g = Gangs.ForDistrict(d);
                    if (g != null) _districtName += "\n<size=20><color=#ffb060>" + g.Name + " turf</color></size>";
                    _districtChipName = "<b>" + name + "</b>";
                    _districtChip = _districtChipName + (g != null ? "   <size=17><color=#ffb060>" + g.Name + " turf</color></size>" : "");
                    _districtChipFitW = -1f;
                }
                _underground = WorldBounds.IsUnderground(Game.PlayerPosition);
            }
            else _underground = false;
            UpdateWheel();
            UpdateCombatHud(dt);
        }

        private void UpdateWheel()
        {
            bool canOpen = Game.IsPlaying && Game.Player != null && !Game.Player.IsDead && !GInput.Broken && (Game.Menus == null || !Game.Menus.IsOpen) && (Game.Phone == null || !Game.Phone.IsOpen);
            bool held = canOpen && GInput.Held(Keys.WeaponWheel);
            if (held && !WheelOpen)
            {
                WheelOpen = true;
                _wheelMouse = Vector2.zero;
                _wheelSlot = SlotIndexOf(Game.Player.Weapons.Current.Slot);
                GInput.GameplayBlocked = true;
                GameTime.Request("wheel", 0.2f);
            }
            else if (!held && WheelOpen)
            {
                WheelOpen = false;
                GInput.GameplayBlocked = false;
                GameTime.Release("wheel");
                if (_wheelSlot >= 0 && Game.Player != null) SelectWheelSlot(_wheelSlot);
            }
            if (WheelOpen)
            {
                Vector2 raw = GInput.RawMouseDelta;
                _wheelMouse += raw * 8f;
                if (_wheelMouse.magnitude > 60f) _wheelMouse = _wheelMouse.normalized * 60f;
                if (_wheelMouse.magnitude > 25f)
                {
                    float ang = Mathf.Atan2(_wheelMouse.x, _wheelMouse.y) * Mathf.Rad2Deg;
                    _wheelSlot = Mathf.RoundToInt(Mathf.Repeat(ang, 360f) / 45f) % 8;
                }
                float scroll = GInput.Scroll;
                if (Mathf.Abs(scroll) > 0.01f && _wheelSlot >= 0)
                {
                    // Scroll cycles weapons inside the highlighted slot.
                    WeaponSlot slot = WheelSlots[_wheelSlot];
                    Game.Player.Weapons.Inventory.SelectSlot(slot);
                }
            }
        }

        private static readonly WeaponSlot[] WheelSlots =
        {
            WeaponSlot.Handgun, WeaponSlot.SMG, WeaponSlot.Shotgun, WeaponSlot.Rifle, WeaponSlot.Sniper, WeaponSlot.Heavy, WeaponSlot.Throwable, WeaponSlot.Melee
        };

        private static int SlotIndexOf(WeaponSlot s)
        {
            if (s == WeaponSlot.Unarmed) s = WeaponSlot.Melee;
            for (int i = 0; i < WheelSlots.Length; i++) if (WheelSlots[i] == s) return i;
            return 7;
        }

        private void SelectWheelSlot(int i)
        {
            WeaponInventory inv = Game.Player.Weapons.Inventory;
            WeaponSlot slot = WheelSlots[i];
            if (Weapons.Get(inv.Current).Slot == slot || (slot == WeaponSlot.Melee && Weapons.Get(inv.Current).Slot == WeaponSlot.Unarmed)) return;
            WeaponId best = inv.BestInSlot(slot);
            if (best != WeaponId.Fists || slot == WeaponSlot.Melee) inv.Select(best);
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (Game.State == GameState.Loading || Game.State == GameState.Title) { DrawFade(); return; }
            GUI.depth = 10;
            UIKit.BeginScaled();
            float W = UIKit.Width, H = UIKit.Height;
            PlayerController p = Game.Player;
            bool show = !Hidden && p != null && (Game.Menus == null || !Game.Menus.IsOpen) && (Game.Cam == null || !Game.Cam.InCinematic);
            if (show && Game.IsPlaying)
            {
                DrawVignettes(W, H, p);
                DrawMinimap(W, H, p);
                DrawTopRight(W, H, p);
                DrawCrosshair(W, H, p);
                DrawPopups(W, H, p);
                DrawCombatHud(W, H, p);
                HudOverlays.DrawAll(W, H);
                DrawPrompts(W, H, p);
                if (WheelOpen) DrawWheel(W, H, p);
                if (_help) DrawHelp(W, H);
            }
            if (!Hidden) DrawMessages(W, H);
            if (Settings.ShowFps) UIKit.Text(new Rect(W - 140f, H - 30f, 130f, 24f), Mathf.RoundToInt(_fps) + " FPS", 18, Color.white, TextAnchor.MiddleRight);
            if (GInput.Broken)
            {
                UIKit.Rect(new Rect(W * 0.5f - 520f, 40f, 1040f, 110f), new Color(0.6f, 0f, 0f, 0.9f));
                UIKit.Text(new Rect(W * 0.5f - 500f, 50f, 1000f, 90f), GInput.Problem, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true, true);
            }
            DrawFade();
        }

        private void DrawFade()
        {
            if (_fade <= 0.001f) return;
            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            UIKit.Rect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, _fade));
            GUI.matrix = m;
        }

        private void DrawVignettes(float W, float H, PlayerController p)
        {
            float hp = p.Health.Health / Mathf.Max(1f, p.Health.MaxHealth);
            float low = hp < 0.3f ? (0.3f - hp) / 0.3f * (0.6f + Mathf.Sin(Time.unscaledTime * 6f) * 0.25f) : 0f;
            float a = Mathf.Max(_damageAlpha * 0.8f, low * 0.7f);
            if (a > 0.01f) UIKit.Tex(new Rect(0f, 0f, W, H), UIKit.Vignette, new Color(0.75f, 0f, 0f, a));
            if (Game.Abilities != null && Game.Abilities.ThirdEyeActive) UIKit.Tex(new Rect(0f, 0f, W, H), UIKit.Vignette, new Color(0.2f, 0.9f, 1f, 0.55f));
            DrawVeilVignette(W, H);
            if (WheelOpen) UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.35f));
            if (_damageDirTime > 0f && Game.Cam != null)
            {
                Vector3 to = MathUtil.Flat(_damageFrom - Game.Player.transform.position);
                float ang = MathUtil.SignedFlatAngle(Game.Cam.FlatForward, to);
                Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, c * UIKit.Scale);
                UIKit.Tex(new Rect(c.x - 40f, c.y - 240f, 80f, 60f), UIKit.Arrow, new Color(1f, 0.1f, 0.1f, Mathf.Clamp01(_damageDirTime)));
                GUI.matrix = m;
            }
        }

        private void DrawMinimap(float W, float H, PlayerController p)
        {
            float mw = 330f, mh = mw * 336f / 512f;
            float x = 36f, y = H - mh - 70f;
            UIKit.Rect(new Rect(x - 4f, y - 4f, mw + 8f, mh + 8f + 26f), new Color(0f, 0f, 0f, 0.65f));
            if (Game.Minimap != null && Game.Minimap.Texture != null) GUI.DrawTexture(new Rect(x, y, mw, mh), Game.Minimap.Texture);
            // North marker.
            if (Game.Cam != null)
            {
                float ang = -Game.Cam.Yaw * Mathf.Deg2Rad;
                Vector2 c = new Vector2(x + mw * 0.5f, y + mh * 0.5f);
                Vector2 n = c + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * (mh * 0.5f - 12f);
                n.x = Mathf.Clamp(n.x, x + 10f, x + mw - 10f);
                n.y = Mathf.Clamp(n.y, y + 10f, y + mh - 10f);
                UIKit.Tex(new Rect(n.x - 11f, n.y - 11f, 22f, 22f), UIKit.Circle, new Color(0f, 0f, 0f, 0.7f));
                UIKit.Text(new Rect(n.x - 11f, n.y - 12f, 22f, 22f), "N", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            }
            // Health, stamina and energy live in the top-left vitals; the selected power sits under the map.
            float by = y + mh + 6f;
            if (Game.Abilities != null && Game.Abilities.AnyUnlocked)
            {
                Spell s = Abilities.Spells[Game.Abilities.Selected];
                float cd = Game.Abilities.CooldownOf(Game.Abilities.Selected);
                string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.LB) + "+" + Pad.Tag(PadButton.RT) + " " : "[B] ";
                string txt = key + s.Name + (cd > 0f ? "  " + cd.ToString("0.0") + "s" : "");
                UIKit.Text(new Rect(x, by, mw, 20f), txt, 16, cd > 0f ? new Color(0.7f, 0.7f, 0.7f) : s.Color, TextAnchor.UpperLeft);
            }
            if (Game.Abilities != null && Game.Abilities.ThirdEyeUnlocked)
            {
                string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.LB) + "+" + Pad.Tag(PadButton.RB) + " " : "[Caps] ";
                UIKit.Text(new Rect(x, by, mw, 20f), key + "Third Eye", 16, Game.Abilities.ThirdEyeActive ? UIKit.Cyan : new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperRight);
            }
            // GPS distance.
            if (Game.Gps != null && Game.Gps.HasTarget)
            {
                float d = Game.Gps.DistanceToTarget;
                string ds = d > 1609f ? (d / 1609.34f).ToString("0.0") + " mi" : Mathf.RoundToInt(d * 3.28084f) + " ft";
                UIKit.Text(new Rect(x + 6f, y + 4f, mw - 12f, 22f), ds, 17, Game.Gps.HasMissionTarget ? UIKit.Gold : new Color(0.8f, 0.6f, 1f), TextAnchor.UpperRight, FontStyle.Bold);
            }
            // Time of day and weather.
            if (Game.Atmo != null) UIKit.Text(new Rect(x + 6f, y + 4f, 200f, 22f), Game.Atmo.ClockText, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private void DrawTopRight(float W, float H, PlayerController p)
        {
            float x = W - 40f;
            float y = 30f;
            // Wanted level as heat crystals.
            if (Game.Wanted != null)
            {
                int stars = Game.Wanted.Stars;
                UIKit.Stars(x, y, 38f, stars, Game.Wanted.Flashing, stars > 0 ? 1f : 0.6f);
                y += 46f;
            }
            // Money.
            if (Game.Economy != null)
            {
                string money = MathUtil.Money(Mathf.RoundToInt(Game.Economy.DisplayMoney));
                UIKit.Text(new Rect(x - 400f, y, 400f, 44f), money, 38, new Color(0.45f, 0.95f, 0.5f), TextAnchor.UpperRight, FontStyle.Bold);
                y += 46f;
                float py = y;
                foreach (Popup pop in _money)
                {
                    float a = Mathf.Clamp01(pop.Time);
                    string s = (pop.Amount >= 0 ? "+" : "-") + MathUtil.Money(Mathf.Abs(pop.Amount)).Replace("-", "");
                    UIKit.Text(new Rect(x - 400f, py, 400f, 30f), s, 26, pop.Amount >= 0 ? new Color(0.5f, 1f, 0.55f, a) : new Color(1f, 0.4f, 0.4f, a), TextAnchor.UpperRight, FontStyle.Bold);
                    py += 28f;
                }
                y = Mathf.Max(y, py);
            }
            // Weapon.
            WeaponController wc = p.Weapons;
            WeaponDef d = wc.Current;
            UIKit.Rect(new Rect(x - 300f, y + 4f, 300f, 64f), new Color(0f, 0f, 0f, 0.45f));
            UIKit.Text(new Rect(x - 290f, y + 8f, 280f, 28f), d.Name, 24, LootArmory.NameColor(d, Color.white), TextAnchor.UpperRight, FontStyle.Bold);
            string ammo;
            if (!d.UsesAmmo) ammo = LootArmory.HeatText(wc) ?? Weapons.SlotName(d.Slot);
            else if (d.ClipSize > 0) ammo = wc.Inventory.Clip[(int)d.Id] + " / " + wc.Inventory.Reserve(d.Id);
            else ammo = wc.Inventory.Ammo[(int)d.Ammo].ToString();
            if (wc.Reloading) ammo = "Reloading...";
            UIKit.Text(new Rect(x - 290f, y + 36f, 280f, 26f), ammo, 22, wc.Reloading ? UIKit.Gold : new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperRight);
            y += 76f;
            if (Game.CrystalCombat != null && !p.InVehicle) y += Game.CrystalCombat.DrawChargeHud(x - 300f, y, 300f);
            // Timer and counter.
            if (_timer >= 0f)
            {
                int t = Mathf.CeilToInt(_timer);
                string ts = (t / 60) + ":" + (t % 60).ToString("00");
                UIKit.Rect(new Rect(x - 300f, y, 300f, 42f), new Color(0f, 0f, 0f, 0.55f));
                UIKit.Text(new Rect(x - 290f, y + 6f, 140f, 30f), string.IsNullOrEmpty(_timerLabel) ? "TIME" : _timerLabel, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(x - 150f, y + 2f, 140f, 36f), ts, 32, _timer < 15f && (Mathf.FloorToInt(Time.unscaledTime * 2f) & 1) == 0 ? new Color(1f, 0.3f, 0.3f) : Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                y += 48f;
            }
            if (!string.IsNullOrEmpty(_counterLabel))
            {
                UIKit.Rect(new Rect(x - 300f, y, 300f, 42f), new Color(0f, 0f, 0f, 0.55f));
                UIKit.Text(new Rect(x - 290f, y + 6f, 190f, 30f), _counterLabel, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(x - 150f, y + 2f, 140f, 36f), _counterA + " / " + _counterB, 30, UIKit.Gold, TextAnchor.UpperRight, FontStyle.Bold);
            }
        }

        private void DrawCrosshair(float W, float H, PlayerController p)
        {
            if (p.InVehicle && !p.Aiming) return;
            if (Game.Cam != null && Game.Cam.ScopeActive)
            {
                DrawScope(W, H);
                return;
            }
            WeaponDef d = p.Weapons.Current;
            bool gun = d.IsGun || d.Fire == FireKind.Throw;
            if (!gun && !p.Aiming) return;
            if (!p.Aiming && !p.AimingDownSights) return;
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            bool enemy = p.AimTarget != null && (p.AimTarget.IsHostileTo() || (p.AimTarget.Team != null && p.AimTarget.Team.Team == NeonSyndicate.Team.Police));
            Color col = enemy ? new Color(1f, 0.25f, 0.25f) : (p.AimTarget != null ? new Color(1f, 0.85f, 0.4f) : Color.white);
            float spread = d.IsGun ? p.Weapons.CurrentSpread(p.AimingDownSights, p.FlatVelocity.magnitude, p.Crouched) : 0f;
            float gap = 4f + spread * 4.5f;
            UIKit.Tex(new Rect(c.x - 3f, c.y - 3f, 6f, 6f), UIKit.Circle, col);
            if (d.IsGun)
            {
                UIKit.Rect(new Rect(c.x - gap - 10f, c.y - 1f, 10f, 2f), col);
                UIKit.Rect(new Rect(c.x + gap, c.y - 1f, 10f, 2f), col);
                UIKit.Rect(new Rect(c.x - 1f, c.y - gap - 10f, 2f, 10f), col);
                UIKit.Rect(new Rect(c.x - 1f, c.y + gap, 2f, 10f), col);
            }
            if (_hitTime > 0f)
            {
                Color hc = _hitKill ? new Color(1f, 0.2f, 0.2f, Mathf.Clamp01(_hitTime * 4f)) : new Color(1f, 1f, 1f, Mathf.Clamp01(_hitTime * 4f));
                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(45f, c * UIKit.Scale);
                UIKit.Rect(new Rect(c.x - 16f, c.y - 1.5f, 10f, 3f), hc);
                UIKit.Rect(new Rect(c.x + 6f, c.y - 1.5f, 10f, 3f), hc);
                UIKit.Rect(new Rect(c.x - 1.5f, c.y - 16f, 3f, 10f), hc);
                UIKit.Rect(new Rect(c.x - 1.5f, c.y + 6f, 3f, 10f), hc);
                GUI.matrix = m;
            }
            if (p.AimTarget != null && !p.AimTarget.IsDead && enemy)
            {
                float hp = p.AimTarget.Health.Health / Mathf.Max(1f, p.AimTarget.Health.MaxHealth);
                UIKit.Bar(new Rect(c.x - 30f, c.y + 28f, 60f, 5f), hp, new Color(1f, 0.3f, 0.3f), new Color(0f, 0f, 0f, 0.6f));
            }
        }

        private void DrawScope(float W, float H)
        {
            float size = H * 0.92f;
            Rect r = new Rect(W * 0.5f - size * 0.5f, H * 0.5f - size * 0.5f, size, size);
            UIKit.Rect(new Rect(0f, 0f, r.x + 2f, H), Color.black);
            UIKit.Rect(new Rect(r.xMax - 2f, 0f, W - r.xMax + 2f, H), Color.black);
            UIKit.Tex(r, UIKit.Vignette, new Color(0f, 0f, 0f, 0.9f));
            UIKit.Tex(new Rect(r.x - size * 0.02f, r.y - size * 0.02f, size * 1.04f, size * 1.04f), UIKit.Ring, Color.black);
            UIKit.Rect(new Rect(r.x + size * 0.05f, H * 0.5f - 1f, size * 0.9f, 2f), new Color(0f, 0f, 0f, 0.85f));
            UIKit.Rect(new Rect(W * 0.5f - 1f, r.y + size * 0.05f, 2f, size * 0.9f), new Color(0f, 0f, 0f, 0.85f));
            UIKit.Tex(new Rect(W * 0.5f - 3f, H * 0.5f - 3f, 6f, 6f), UIKit.Circle, new Color(1f, 0.2f, 0.2f));
            UIKit.Text(new Rect(W * 0.5f + 20f, H * 0.5f + 20f, 300f, 30f), "Scroll to zoom", 18, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft);
        }

        private void DrawPopups(float W, float H, PlayerController p)
        {
            float x = W - 40f;
            float y = H - 60f;
            // Speedometer.
            if (p.InVehicle && Settings.ShowSpeedometer)
            {
                Vehicle v = p.CurrentVehicle;
                int mph = Mathf.RoundToInt(Mathf.Abs(v.ForwardSpeed) * 2.23694f);
                UIKit.Text(new Rect(x - 300f, y - 70f, 300f, 64f), mph.ToString(), 60, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                string gear = v.Gear < 0 ? "R" : (v.Speed < 0.5f ? "N" : v.Gear.ToString());
                UIKit.Text(new Rect(x - 300f, y - 8f, 300f, 26f), "MPH   " + gear + (v.NitroCharges > 0 ? "   NITRO x" + v.NitroCharges : ""), 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperRight, FontStyle.Bold);
                float h = v.Health / v.Def.Health;
                UIKit.Bar(new Rect(x - 180f, y + 18f, 180f, 6f), h, h < 0.3f ? new Color(1f, 0.3f, 0.2f) : new Color(0.9f, 0.9f, 0.9f), new Color(0f, 0f, 0f, 0.5f));
                y -= 110f;
            }
            if (_vehicleTime > 0f && !string.IsNullOrEmpty(_vehicleName))
            {
                float a = Mathf.Clamp01(_vehicleTime);
                UIKit.Text(new Rect(x - 600f, y - 40f, 600f, 40f), _vehicleName, 34, new Color(0.4f, 0.95f, 1f, a), TextAnchor.LowerRight, FontStyle.Bold);
                y -= 46f;
            }
            if (_districtTime > 0f && !string.IsNullOrEmpty(_districtName))
            {
                float a = Mathf.Clamp01(_districtTime);
                // Above the speedometer in a vehicle (as before). On foot (or with the speedometer off) the two large lines
                // used to print over the Lumen readout: one line in its own slot instead (HudLayout04E), and none while an
                // objective or a subtitle holds the bottom of the screen.
                if (p.InVehicle && Settings.ShowSpeedometer)
                    UIKit.Text(new Rect(x - 600f, y - 70f, 600f, 70f), _districtName, 34, new Color(1f, 1f, 1f, a), TextAnchor.LowerRight, FontStyle.Bold, true, true);
                else if (string.IsNullOrEmpty(_objective) && _subTime <= 0f)
                    DrawDistrictChip(W, H, a);
            }
            // Radio.
            if (Game.Radio != null && Game.Radio.ShowTimer > 0f && p.InVehicle && !string.IsNullOrEmpty(Game.Radio.NowPlaying))
            {
                float a = Mathf.Clamp01(Game.Radio.ShowTimer);
                UIKit.Text(new Rect(W * 0.5f - 500f, 30f, 1000f, 70f), Game.Radio.NowPlaying, 24, new Color(1f, 0.85f, 0.4f, a), TextAnchor.UpperCenter, FontStyle.Bold, true, true);
            }
            // Arrest bar.
            if (Game.Police != null && Game.Police.ArrestProgress > 0.01f)
            {
                UIKit.Text(new Rect(W * 0.5f - 200f, H * 0.62f - 34f, 400f, 30f), "BEING ARRESTED - MOVE!", 24, new Color(0.5f, 0.7f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Bar(new Rect(W * 0.5f - 150f, H * 0.62f, 300f, 10f), Game.Police.ArrestProgress, new Color(0.4f, 0.6f, 1f), new Color(0f, 0f, 0f, 0.6f));
            }
            if (_progressFrame >= Time.frameCount - 1 && !string.IsNullOrEmpty(_progressLabel))
            {
                UIKit.Text(new Rect(W * 0.5f - 300f, H * 0.66f - 34f, 600f, 30f), _progressLabel, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Bar(new Rect(W * 0.5f - 160f, H * 0.66f, 320f, 10f), _progress, UIKit.Gold, new Color(0f, 0f, 0f, 0.6f));
            }
        }

        /// <summary>
        /// 04E: the district name on foot as one right-aligned line in its HudLayout04E slot (the name, then the turf in
        /// small type). The font steps down for a long line, and the turf is dropped when even the smallest size is too wide.
        /// </summary>
        private void DrawDistrictChip(float W, float H, float a)
        {
            Rect r = HudLayout04E.DistrictChipFoot(W, H);
            if (_districtChipFitW != r.width)
            {
                _districtChipFitW = r.width;
                float avail = r.width - 16f;
                float full = UIKit.Measure(_districtChip, 24, FontStyle.Bold).x;
                _districtChipText = _districtChip;
                _districtChipSize = HudLayout04E.FitFontSize(full, avail, 24, 16);
                if (full * _districtChipSize / 24f > avail)
                {
                    _districtChipText = _districtChipName;
                    _districtChipSize = HudLayout04E.FitFontSize(UIKit.Measure(_districtChipName, 24, FontStyle.Bold).x, avail, 24, 16);
                }
            }
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.4f * a));
            UIKit.Text(new Rect(r.x + 8f, r.y, r.width - 16f, r.height), _districtChipText, _districtChipSize, new Color(1f, 1f, 1f, a), TextAnchor.MiddleRight, FontStyle.Bold, true, false);
        }

        private void DrawPrompts(float W, float H, PlayerController p)
        {
            List<string> lines = new List<string>();
            if (p.Focus != null && !p.InVehicle) lines.Add(GInput.Prompt(p.Focus.CurrentPrompt));
            else if (p.EnterCandidate != null && !p.InVehicle && !p.Entering)
            {
                Vehicle v = p.EnterCandidate;
                string verb = v.Driver != null && !v.Driver.IsPlayer ? "carjack the " : "enter the ";
                lines.Add(GInput.Prompt("Press F to " + verb + v.Def.Name));
            }
            foreach (Note n in _notes) lines.Add(GInput.Prompt(n.Text));
            // Contextual prompts sit low in the centre, clear of the vitals and the minimap. 04E: underground on foot they
            // start under the Underheart heat row, which owns the top of this column there (HudLayout04E.PromptsTop).
            float y = HudLayout04E.PromptsTop(H, _underground && !p.InVehicle);
            foreach (string line in lines)
            {
                Vector2 size = UIKit.Measure(line, 22);
                float w = Mathf.Min(760f, size.x + 40f);
                float h = Mathf.Max(44f, Mathf.Ceil(size.x / 720f) * 28f + 16f);
                Rect r = new Rect(W * 0.5f - w * 0.5f, y, w, h);
                UIKit.Rect(r, new Color(0f, 0f, 0f, 0.62f));
                UIKit.Rect(new Rect(r.x, r.y, 3f, r.height), new Color(0.6f, 0.45f, 1f, 0.9f));
                UIKit.Text(new Rect(r.x + 20f, r.y + 8f, w - 40f, h - 16f), line, 22, Color.white, TextAnchor.UpperCenter, FontStyle.Normal, false, true);
                y += h + 6f;
            }
        }

        private void DrawMessages(float W, float H)
        {
            // Big center messages (FLATLINED, CUFFED, MISSION COMPLETE).
            if (_bigTime > 0f && !string.IsNullOrEmpty(_bigText))
            {
                float a = Mathf.Clamp01(_bigTime * 1.5f);
                UIKit.Rect(new Rect(0f, H * 0.36f, W, 150f), new Color(0f, 0f, 0f, 0.55f * a));
                UIKit.Text(new Rect(0f, H * 0.36f + 10f, W, 90f), _bigText, 84, new Color(_bigColor.r, _bigColor.g, _bigColor.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                if (!string.IsNullOrEmpty(_bigSub)) UIKit.Text(new Rect(0f, H * 0.36f + 96f, W, 44f), _bigSub, 30, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            // Banners (achievements, unique stunts, chapter titles).
            if (_bannerTime > 0f && !string.IsNullOrEmpty(_bannerTitle))
            {
                float t = _bannerMax - _bannerTime;
                float a = Mathf.Clamp01(Mathf.Min(t * 3f, _bannerTime * 1.5f));
                float y = H * 0.18f;
                UIKit.Text(new Rect(0f, y, W, 60f), _bannerTitle, 50, new Color(_bannerColor.r, _bannerColor.g, _bannerColor.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                if (!string.IsNullOrEmpty(_bannerSub)) UIKit.Text(new Rect(W * 0.15f, y + 58f, W * 0.7f, 60f), _bannerSub, 26, new Color(1f, 1f, 1f, a), TextAnchor.UpperCenter, FontStyle.Normal, true, true);
            }
            if (!Game.IsPlaying && Game.State != GameState.Wasted && Game.State != GameState.Busted && Game.State != GameState.Cutscene) return;
            // Objective.
            float sy = H - 150f;
            if (_subTime > 0f && !string.IsNullOrEmpty(_subLine))
            {
                string speaker = string.IsNullOrEmpty(_subSpeaker) ? "" : "<color=#ffd24d>" + _subSpeaker + ":</color> ";
                UIKit.Text(new Rect(W * 0.15f, sy, W * 0.7f, 70f), speaker + _subLine, 28, Color.white, TextAnchor.UpperCenter, FontStyle.Bold, true, true);
                sy -= 48f;
            }
            if (!string.IsNullOrEmpty(_objective))
            {
                Color oc = Color.Lerp(Color.white, UIKit.Gold, _objectiveFlash);
                UIKit.Text(new Rect(W * 0.15f, sy - 10f, W * 0.7f, 50f), _objective, 28, oc, TextAnchor.LowerCenter, FontStyle.Bold, true, true);
            }
        }

        private void DrawWheel(float W, float H, PlayerController p)
        {
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            float radius = 250f;
            WeaponInventory inv = p.Weapons.Inventory;
            UIKit.Tex(new Rect(c.x - radius - 90f, c.y - radius - 90f, (radius + 90f) * 2f, (radius + 90f) * 2f), UIKit.Circle, new Color(0f, 0f, 0f, 0.55f));
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f * Mathf.Deg2Rad;
                Vector2 pos = c + new Vector2(Mathf.Sin(ang), -Mathf.Cos(ang)) * radius;
                WeaponSlot slot = WheelSlots[i];
                List<WeaponId> list = inv.InSlot(slot);
                WeaponId best = inv.BestInSlot(slot);
                bool has = list.Count > 0 && (slot == WeaponSlot.Melee || best != WeaponId.Fists);
                bool sel = i == _wheelSlot;
                Rect r = new Rect(pos.x - 95f, pos.y - 48f, 190f, 96f);
                UIKit.Rect(r, sel ? new Color(1f, 0.82f, 0.3f, 0.9f) : new Color(0.1f, 0.1f, 0.12f, has ? 0.85f : 0.45f));
                UIKit.Text(new Rect(r.x, r.y + 6f, r.width, 22f), Weapons.SlotName(slot), 18, sel ? Color.black : new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperCenter, FontStyle.Bold, !sel);
                if (has)
                {
                    WeaponId show = Weapons.Get(inv.Current).Slot == slot ? inv.Current : best;
                    WeaponDef d = Weapons.Get(show);
                    UIKit.Text(new Rect(r.x + 4f, r.y + 30f, r.width - 8f, 30f), d.Name, 20, sel ? Color.black : LootArmory.NameColor(d, Color.white), TextAnchor.UpperCenter, FontStyle.Bold, !sel, true);
                    if (d.UsesAmmo) UIKit.Text(new Rect(r.x, r.y + 66f, r.width, 24f), inv.TotalAmmo(show).ToString(), 18, sel ? Color.black : UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Normal, !sel);
                    if (list.Count > 1) UIKit.Text(new Rect(r.x, r.y + 66f, r.width - 8f, 24f), "+" + (list.Count - 1), 16, sel ? Color.black : Color.white, TextAnchor.UpperRight, FontStyle.Normal, !sel);
                }
                else UIKit.Text(new Rect(r.x, r.y + 40f, r.width, 30f), "Empty", 18, new Color(0.5f, 0.5f, 0.5f), TextAnchor.UpperCenter);
            }
            WeaponDef cur = p.Weapons.Current;
            UIKit.Text(new Rect(c.x - 200f, c.y - 40f, 400f, 40f), cur.Name, 30, LootArmory.NameColor(cur, Color.white), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(c.x - 220f, c.y, 440f, 60f), cur.Blurb, 18, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperCenter, FontStyle.Normal, true, true);
            UIKit.Text(new Rect(c.x - 300f, c.y + radius + 70f, 600f, 30f), "Move the mouse to pick a slot, scroll to cycle, release Tab to equip", 18, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperCenter);
        }

        private void DrawHelp(float W, float H)
        {
            Rect r = new Rect(W * 0.5f - 560f, H * 0.5f - 500f, 1120f, 1000f);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.86f));
            UIKit.Text(new Rect(r.x, r.y + 16f, r.width, 40f), "CONTROLS (F1 to close)", 34, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            string left =
                "<b>ON FOOT</b>\n" +
                "WASD  Move      Shift  Sprint      Alt  Walk\n" +
                "Space  Jump / climb      Ctrl  Crouch\n" +
                "Mouse  Look      RMB  Aim      LMB  Fire / punch\n" +
                "R  Reload      G  Quick throw      Tab  Weapon wheel\n" +
                "Mouse wheel / 1-8  Cycle weapons\n" +
                "1 / 2  Gadgets (hold to see where it lands, let go to deploy; pad: hold D-pad left / right)\n" +
                "E  Interact (tap: take a dropped gun, hold: equip it)      F  Enter / carjack vehicle\n" +
                "C  Swap shoulder      V  Camera view\n" +
                "X  Take cover (in cover: tap to leave, hold aimed at other cover to dash, Space vaults or climbs it)\n" +
                "Caps Lock / T  Third Eye (tap: slow motion, hold: shift through the Veil)\n" +
                "B  Cast power      [ ]  Choose power\n" +
                "Powers mark foes (a glowing ring): shoot a marked foe for a RESONANCE BURST. Gun hits refill crystal energy.\n\n" +
                "<b>TRAVEL POWERS</b>\n" +
                "Ctrl while sprinting  Surge (super speed)      Crouch, then hold and release Space  Super leap\n" +
                "Hold Space in the air  Glide      Double-tap Space in the air  Fly (Space up, Ctrl down, Shift fast)\n\n" +
                "<b>DRIVING</b>\n" +
                "W / S  Throttle / brake / reverse      A / D  Steer\n" +
                "Space  Handbrake      Shift  Nitro (if fitted)\n" +
                "E  Horn (tap = siren in emergency vehicles)\n" +
                "H  Headlights      C  Look behind      V  Camera\n" +
                ", .  Radio station      /  Radio off\n" +
                "RMB + LMB  Drive-by with pistols and SMGs\n" +
                "F  Exit (at speed: bail out)      J  Taxi / vigilante / paramedic jobs\n\n" +
                "<b>BONDS AND FUSION</b>\n" +
                "N  Companion: tap follow / wait, hold recall      U  Companion assist      K  Build overview\n" +
                "On your own bike: hold N to fuse or separate, U for the form ability      Pad: LB + D-pad Down / Up\n\n" +
                "<b>GENERAL</b>\n" +
                "M  Map (click to set a waypoint)      P  Phone\n" +
                "Esc  Pause menu      F2  Debug menu      F3  Hide HUD\n" +
                "Enter  Next line of dialogue      Backspace  Skip the conversation or scene\n" +
                "F12  Screenshot      Type cheat codes during play";
            UIKit.Text(new Rect(r.x + 50f, r.y + 76f, r.width - 100f, r.height - 90f), left, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
        }
    }
}
