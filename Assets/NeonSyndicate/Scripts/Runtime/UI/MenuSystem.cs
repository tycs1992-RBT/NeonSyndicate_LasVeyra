using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum ShopKind
    {
        Guns,
        Garage,
        Clothing
    }

    /// <summary>
    /// Every full-screen menu: title screen, pause menu (map with waypoints, mission brief, stats and achievements,
    /// settings, controls, save/load), gun shop, Jax Calder Customs, clothing store, safehouse, casino slots, story
    /// choices and the F2 debug menu.
    /// </summary>
    public sealed class MenuSystem : MonoBehaviour
    {
        private enum MenuScreen { None, Title, Pause, Shop, Safehouse, Slots, Choice, Debug, Shrine }
        private enum Tab { Map, Brief, Gear, Stats, Settings, Controls, Save }

        private MenuScreen _screen = MenuScreen.None;
        private Tab _tab = Tab.Map;
        /// <summary>The selected extra page (skills, journal, bestiary), or null when a built-in tab is selected.</summary>
        private MenuPage _page;
        private struct TabEntry { public string Name; public Tab Core; public MenuPage Page; }
        private readonly List<TabEntry> _tabs = new List<TabEntry>();
        /// <summary>The tab list is rebuilt at most once per frame (Update and every OnGUI event ask for it).</summary>
        private int _tabsFrame = -1;
        /// <summary>Tab rows, width and font, measured again only when the screen width or the set of tabs changes.</summary>
        private TabLayout _tabLayout;
        private float _tabLayoutW = -1f;
        private int _tabLayoutKey;
        private ShopKind _shop;
        private string _shopName;
        /// <summary>Faction turf and skill discount at the open shop, worked out when it opened (see ProgressionHooks).</summary>
        private float _shopDiscount;
        private string _shopDiscountText = "";
        private WeaponSlot _gunCategory = WeaponSlot.Handgun;
        /// <summary>The gun shop's Loot guns tab (drawn by the loot armory) is open.</summary>
        private bool _lootShop;
        private string _choiceTitle;
        private string _choiceText;
        private string[] _choiceOptions;
        private Action<int> _choiceCallback;
        private ChoiceCardFit04E _choiceFit;
        private string _choiceFitFor;
        private float _choiceFitH = -1f;
        private Vector2 _choiceScroll;
        private Vector2 _scroll;
        private int _debugTab;
        private string _status;
        private float _statusTime;
        private bool _confirmNewGame;
        /// <summary>The pending New Game confirmation came from "Start after the prologue".</summary>
        private bool _confirmSkipsPrologue;
        private bool _titleSettings;
        private bool _titleLoad;
        private int _shrineId = -1;
        private int _shrineTab;
        private int _settingsPage;

        // Map state.
        private Vector2 _mapCenter;
        private float _mapZoom = 1f;
        private bool _mapDragging;
        private Vector2 _mapDragStart;
        private Vector2 _mapCenterStart;
        private bool _mapOnly;

        // Slots state.
        private readonly string[] _reelSymbols = { "7", "BAR", "LOTUS", "GEM", "BELL", "SKULL" };
        private readonly int[] _reels = new int[3];
        private float _spinTime;
        private string _slotResult = "";
        private int _slotBet = 100;

        public bool IsOpen { get { return _screen != MenuScreen.None && _screen != MenuScreen.Title; } }
        public bool ChoiceOpen { get { return _screen == MenuScreen.Choice; } }
        public bool TitleOpen { get { return _screen == MenuScreen.Title; } }

        // ------------------------------------------------------------------ open / close

        public void ShowTitle()
        {
            _screen = MenuScreen.Title;
            _confirmNewGame = false;
            _titleSettings = false;
            _titleLoad = false;
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
        }

        /// <summary>Hides every screen including the title (used when a game session starts or loads).</summary>
        public void HideAll()
        {
            _screen = MenuScreen.None;
            _mapOnly = false;
            _confirmNewGame = false;
            _titleSettings = false;
            _titleLoad = false;
            _choiceCallback = null;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            GameTime.SetPaused(false);
        }

        private void Open(MenuScreen s, bool pause)
        {
            _screen = s;
            _scroll = Vector2.zero;
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            if (pause) GameTime.SetPaused(true);
            if (Game.State == GameState.Playing) Game.State = GameState.Paused;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneOpen, 0.5f);
        }

        public void Close()
        {
            if (_screen == MenuScreen.Title) return;
            GearMenu.AtShrine = false;
            _screen = MenuScreen.None;
            _mapOnly = false;
            GInput.SuppressDialogueInput();
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            GameTime.SetPaused(false);
            if (Game.State == GameState.Paused) Game.State = GameState.Playing;
            if (Game.State == GameState.Playing || Game.State == GameState.Cutscene) GInput.SetCursorLocked(true);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.45f);
        }

        public void OpenPause(bool mapOnly)
        {
            Open(MenuScreen.Pause, true);
            _tab = mapOnly ? Tab.Map : Tab.Map;
            _page = null;
            _mapOnly = mapOnly;
            CenterMapOnPlayer();
        }

        /// <summary>Opens a visible system page through the existing pause flow, for physical ledger terminals.</summary>
        public bool OpenRegisteredPage(string title)
        {
            MenuPage target = null;
            foreach (MenuPage candidate in MenuPages.Pages)
                if (candidate.Title == title && candidate.IsVisible) { target = candidate; break; }
            if (target == null || (Game.State != GameState.Playing && Game.State != GameState.Paused)) return false;
            OpenPause(false);
            _page = target;
            _scroll = Vector2.zero;
            _tabsFrame = -1;
            return true;
        }

        public void OpenShop(ShopKind kind, string name)
        {
            _shop = kind;
            _shopName = name;
            _shopDiscount = ProgressionHooks.ShopDiscount(name);
            _shopDiscountText = ProgressionHooks.ShopDiscountText(name);
            if (kind == ShopKind.Garage && (Game.Player == null || !Game.Player.InVehicle))
            {
                if (Game.Hud != null) Game.Hud.Notify("Drive a vehicle into the garage to modify it.", 3f);
                return;
            }
            Open(MenuScreen.Shop, true);
        }

        public void OpenSafehouse(string name)
        {
            if (Game.Wanted != null && Game.Wanted.Stars > 0)
            {
                Game.Hud.Notify("You can't use the safehouse while the police are after you.", 3f);
                return;
            }
            _shopName = name;
            Open(MenuScreen.Safehouse, true);
        }

        /// <summary>Shrine menu after resting: level up, Shard Tonic, gear upgrades, fast travel.</summary>
        public void OpenShrine(int id)
        {
            _shrineId = id;
            _shrineTab = 0;
            Open(MenuScreen.Shrine, true);
        }

        public void OpenSlots(string name)
        {
            _shopName = name;
            _slotResult = "";
            Open(MenuScreen.Slots, true);
        }

        public void ShowChoice(string title, string text, string[] options, Action<int> callback)
        {
            _choiceTitle = title;
            _choiceText = text;
            _choiceOptions = options;
            _choiceCallback = callback;
            _choiceFitFor = null;
            _choiceScroll = Vector2.zero;
            Open(MenuScreen.Choice, true);
        }

        private void Status(string s)
        {
            _status = s;
            _statusTime = 3f;
        }

        // ------------------------------------------------------------------ input

        private void Update()
        {
            _statusTime -= Time.unscaledDeltaTime;
            if (_spinTime > 0f)
            {
                _spinTime -= Time.unscaledDeltaTime;
                for (int i = 0; i < 3; i++) if (_spinTime > i * 0.35f) _reels[i] = UnityEngine.Random.Range(0, _reelSymbols.Length);
                if (_spinTime <= 0f) ResolveSpin();
            }
            if (Game.State == GameState.Title || Game.State == GameState.Loading) return;
            bool esc = Act.Pause;
            bool map = Act.Map;
            if (_screen == MenuScreen.None)
            {
                // Keep the cinematic state while GameTime pauses it; Close then resumes the same scene.
                if (Game.State == GameState.Cutscene)
                {
                    if (esc && !GInput.GameplayBlocked) OpenPause(false);
                    return;
                }
                if (!Game.IsPlaying || (Game.Hud != null && Game.Hud.WheelOpen) || (Game.Phone != null && Game.Phone.IsOpen)) return;
                if (esc) OpenPause(false);
                else if (map) OpenPause(true);
                else if (Act.Gear) { OpenPause(false); _tab = Tab.Gear; _page = null; }
                else if (Act.BuildMenu) OpenRegisteredPage("BUILD");
                else if (GInput.Down(Keys.Debug)) Open(MenuScreen.Debug, true);
                return;
            }
            if (_screen == MenuScreen.Pause && Pad.Connected)
            {
                BuildTabs();
                int n = _tabs.Count;
                int cur = CurrentTab();
                if (Pad.Down(PadButton.LB)) SelectTab((cur + n - 1) % n);
                if (Pad.Down(PadButton.RB)) SelectTab((cur + 1) % n);
            }
            bool back = Pad.Down(PadButton.East) && _screen != MenuScreen.Choice;
            if (esc || back || (_screen == MenuScreen.Pause && _mapOnly && map) || (_screen == MenuScreen.Debug && GInput.Down(Keys.Debug)) || (_screen == MenuScreen.Pause && _tab == Tab.Gear && _page == null && Act.Gear))
            {
                if (_screen == MenuScreen.Choice) return;
                if (_screen == MenuScreen.Slots && _spinTime > 0f) return;
                Close();
            }
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (_screen == MenuScreen.None) return;
            GUI.depth = 0;
            UIKit.BeginScaledMenu();
            float W = UIKit.Width, H = UIKit.Height;
            switch (_screen)
            {
                case MenuScreen.Title: DrawTitle(W, H); break;
                case MenuScreen.Pause: DrawPause(W, H); break;
                case MenuScreen.Shop: DrawShop(W, H); break;
                case MenuScreen.Safehouse: DrawSafehouse(W, H); break;
                case MenuScreen.Slots: DrawSlots(W, H); break;
                case MenuScreen.Choice: DrawChoice(W, H); break;
                case MenuScreen.Debug: DrawDebug(W, H); break;
                case MenuScreen.Shrine: DrawShrine(W, H); break;
            }
            if (_statusTime > 0f && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(0f, H - 70f, W, 40f), _status, 26, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        // ------------------------------------------------------------------ title

        /// <summary>Starts a new story game, from the prologue or (afterPrologue) from mission 2 at Mara's clinic.</summary>
        private void StartStory(bool afterPrologue)
        {
            if (Game.Layout != null && Game.Layout.Revision04O != CityRevision04O.Legacy04N)
            {
                CityRebirthBoot04O.Request(CityRevision04O.Legacy04N,null,false,afterPrologue); return;
            }
            if (Game.Missions != null)
            {
                if (afterPrologue) Game.Missions.RequestPrologueSkip();
                else Game.Missions.CancelPrologueSkip();
            }
            Game.Root.NewGame();
        }

        private void DrawTitle(float W, float H)
        {
            UIKit.Tex(new Rect(0f, 0f, W, H), UIKit.Vignette, new Color(0f, 0f, 0f, 0.85f));
            UIKit.Text(new Rect(0f, H * 0.12f, W, 110f), "NEON SYNDICATE", 104, UIKit.Pink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(0f, H * 0.12f + 100f, W, 60f), "L A S   V E Y R A", 44, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(0f, H * 0.12f + 160f, W, 40f), "Malik Veyr wakes in the ruins of Crystal Manor. His daughter hears another world calling her name.", 22, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter);
            if (_titleSettings)
            {
                Rect r = new Rect(W * 0.5f - 520f, H * 0.36f, 1040f, H * 0.56f);
                UIKit.Rect(r, UIKit.Panel);
                DrawSettings(r);
                if (UIKit.Button(new Rect(W * 0.5f - 120f, r.yMax + 10f, 240f, 50f), "Back", false)) { Settings.Save(); _titleSettings = false; }
                return;
            }
            if (_titleLoad)
            {
                Rect r = new Rect(W * 0.5f - 520f, H * 0.38f, 1040f, 420f);
                UIKit.Rect(r, UIKit.Panel);
                DrawSlotsList(r, false);
                if (UIKit.Button(new Rect(W * 0.5f - 120f, r.yMax + 10f, 240f, 50f), "Back", false)) _titleLoad = false;
                return;
            }
            float bx = W * 0.5f - 180f;
            float by = H * 0.45f;
            bool hasSave = SaveSystem.AnyExists();
            bool anyBankHasSave = hasSave || SaveSystem.AnyExists(0) || SaveSystem.AnyExists(2) || SaveSystem.AnyExists(3) || SaveSystem.AnyExists(4) || SaveSystem.AnyExists(5);
            if (hasSave)
            {
                if (UIKit.Button(new Rect(bx, by, 360f, 58f), "Continue", true, 28)) { Game.Root.ContinueGame(); return; }
                by += 70f;
            }
            if (_confirmNewGame)
            {
                UIKit.Text(new Rect(0f, by, W, 40f), _confirmSkipsPrologue ? "Start a new story after the prologue? Your saves stay on disk." : "Start a new story? Your saves stay on disk.", 24, Color.white, TextAnchor.MiddleCenter);
                by += 50f;
                if (UIKit.Button(new Rect(bx, by, 170f, 54f), "Yes", false)) { StartStory(_confirmSkipsPrologue); return; }
                if (UIKit.Button(new Rect(bx + 190f, by, 170f, 54f), "No", false)) _confirmNewGame = false;
                by += 70f;
            }
            else
            {
                if (UIKit.Button(new Rect(bx, by, 360f, 58f), "New Game", !hasSave, 28))
                {
                    if (hasSave) { _confirmNewGame = true; _confirmSkipsPrologue = false; }
                    else { StartStory(false); return; }
                }
                by += 70f;
            }
            if (anyBankHasSave)
            {
                if (UIKit.Button(new Rect(bx, by, 360f, 58f), "Load Game", false, 28)) _titleLoad = true;
                by += 70f;
            }
            if (UIKit.Button(new Rect(bx + 390f, by, 390f, 58f), Game.Layout != null && Game.Layout.Revision04O == CityRevision04O.Crystalline04O ? "Legacy city Free Roam" : "Crystal city preview (04O)", false, 22))
            {
                CityRevision04O revision = Game.Layout != null && Game.Layout.Revision04O == CityRevision04O.Crystalline04O ? CityRevision04O.Legacy04N : CityRevision04O.Crystalline04O;
                CityRebirthBoot04O.Request(revision, null, true); return;
            }
            if (UIKit.Button(new Rect(bx + 390f, by + 75f, 390f, 58f), "Gothic realm preview (04O-B)", false, 22))
            { CityRebirthBoot04O.Request(CityRevision04O.GothicRealm04OB,null,true); return; }
            if (UIKit.Button(new Rect(bx + 390f, by + 150f, 390f, 58f), "Fractured wilds preview (04O-C)", false, 22))
            { CityRebirthBoot04O.Request(CityRevision04O.FracturedWilds04OC,null,true); return; }
            if (UIKit.Button(new Rect(bx + 390f, by + 225f, 188f, 58f), "Realms 04O-D", false, 20))
            { CityRebirthBoot04O.Request(CityRevision04O.CrystalRealms04OD,null,true); return; }
            if (UIKit.Button(new Rect(bx + 592f, by + 225f, 188f, 58f), "Unbound 04O-E", false, 20))
            { CityRebirthBoot04O.Request(CityRevision04O.CrystalUnbound04OE,null,true); return; }
            if (UIKit.Button(new Rect(bx + 390f, by + 293f, 390f, 58f), "Living Frontier 04O-G", false, 22))
            { CityRebirthBoot04O.Request(CityRevision04O.LivingFrontier04OG,null,true); return; }
            UIKit.Text(new Rect(bx + 390f, by + 357f, 390f, 80f), "Explicit previews have isolated saves. Original story and earlier layouts stay available. Native acceptance pending.", 17, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            if (UIKit.Button(new Rect(bx, by, 360f, 58f), "Free Roam (skip story)", false, 24))
            {
                if (Game.Missions != null) Game.Missions.CancelPrologueSkip();
                Game.Root.NewGame(true);
                return;
            }
            by += 70f;
            // The story from mission 2 at Mara's clinic, with everything the prologue gives and a hover bike.
            if (!_confirmNewGame && UIKit.Button(new Rect(bx, by, 360f, 58f), "Start After the Prologue", false, 24))
            {
                if (hasSave) { _confirmNewGame = true; _confirmSkipsPrologue = true; }
                else { StartStory(true); return; }
            }
            if (!_confirmNewGame) by += 70f;
            if (UIKit.Button(new Rect(bx, by, 360f, 58f), "Settings", false, 28)) _titleSettings = true;
            by += 70f;
            if (UIKit.Button(new Rect(bx, by, 360f, 58f), "Quit", false, 28)) Game.Root.Quit();
            UIKit.Text(new Rect(20f, H - 40f, W - 40f, 30f), Game.Title + "  " + Game.Version + "  |  Built procedurally in Unity  |  F1 in game for controls", 18, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
            if (GInput.Broken)
                UIKit.Text(new Rect(W * 0.1f, H * 0.32f, W * 0.8f, 90f), GInput.Problem, 22, new Color(1f, 0.4f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold, true, true);
        }

        // ------------------------------------------------------------------ pause

        private void DrawPause(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.55f));
            UIKit.Text(new Rect(60f, 30f, 800f, 60f), "NEON SYNDICATE: LAS VEYRA", 40, UIKit.Pink, TextAnchor.UpperLeft, FontStyle.Bold);
            BuildTabs();
            int current = CurrentTab();
            // Narrow screens shrink the labels to fit, then wrap the tabs onto two rows (UiFit.Tabs).
            LayoutTabs(W);
            TabLayout tl = _tabLayout;
            const float tabH = 46f, rowGap = 6f;
            for (int i = 0; i < _tabs.Count; i++)
            {
                Rect tr = new Rect(60f + (i % tl.PerRow) * (tl.Width + UiFit.TabGap), 90f + (i / tl.PerRow) * (tabH + rowGap), tl.Width, tabH);
                if (UIKit.Button(tr, _tabs[i].Name, current == i, tl.Font)) SelectTab(i);
            }
            float extra = (tl.Rows - 1) * (tabH + rowGap);
            if (Pad.Connected && Pad.LastUsed)
                UIKit.Text(new Rect(60f, 138f + extra, 600f, 22f), Pad.Tag(PadButton.LB) + " / " + Pad.Tag(PadButton.RB) + " switch tabs     " + Pad.Tag(PadButton.East) + " back", 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft);
            if (UIKit.Button(new Rect(W - 260f, 90f, 200f, 46f), "RESUME", false, 22)) { Close(); return; }
            Rect body = new Rect(60f, 150f + extra, W - 120f, H - 220f - extra);
            if (_page != null)
            {
                try { _page.Draw(body); }
                catch (Exception e) { Log.Exception("Menu page " + _page.Title, e); _page = null; }
            }
            else switch (_tab)
            {
                case Tab.Map: DrawMap(body); break;
                case Tab.Brief: DrawBrief(body); break;
                case Tab.Gear: UIKit.Rect(body, UIKit.Panel); GearMenu.Draw(body); break;
                case Tab.Stats: DrawStats(body); break;
                case Tab.Settings: UIKit.Rect(body, UIKit.Panel); DrawSettings(body); break;
                case Tab.Controls: UIKit.Rect(body, UIKit.Panel); DrawControls(body); break;
                case Tab.Save: UIKit.Rect(body, UIKit.Panel); DrawSlotsList(body, true); break;
            }
            if (UIKit.Button(new Rect(W - 470f, H - 60f, 200f, 44f), "Title Screen", false, 20)) { Game.Root.ReturnToTitle(); return; }
            if (UIKit.Button(new Rect(W - 260f, H - 60f, 200f, 44f), "Quit Game", false, 20)) Game.Root.Quit();
        }

        /// <summary>Built-in tabs plus the pages other systems registered (MenuPages), in display order. At most once per frame.</summary>
        private void BuildTabs()
        {
            if (_tabsFrame == Time.frameCount && _tabs.Count > 0) return;
            _tabsFrame = Time.frameCount;
            _tabs.Clear();
            AddCore("MAP", Tab.Map);
            AddCore("BRIEF", Tab.Brief);
            AddCore("GEAR", Tab.Gear);
            foreach (MenuPage p in MenuPages.Pages) if (p.Order < 100 && p.IsVisible) _tabs.Add(new TabEntry { Name = p.Title, Page = p });
            AddCore("STATS", Tab.Stats);
            AddCore("SETTINGS", Tab.Settings);
            AddCore("CONTROLS", Tab.Controls);
            AddCore("SAVE / LOAD", Tab.Save);
            foreach (MenuPage p in MenuPages.Pages) if (p.Order >= 100 && p.IsVisible) _tabs.Add(new TabEntry { Name = p.Title, Page = p });
            if (_page != null && !HasTabFor(_page)) _page = null;
        }

        private bool HasTabFor(MenuPage page)
        {
            for (int i = 0; i < _tabs.Count; i++) if (_tabs[i].Page == page) return true;
            return false;
        }

        /// <summary>Works out the tab rows, width and font (UiFit.Tabs, measuring the labels) when the width or the tabs changed.</summary>
        private void LayoutTabs(float W)
        {
            int key = _tabs.Count;
            for (int i = 0; i < _tabs.Count; i++) key = key * 31 + (_tabs[i].Name != null ? _tabs[i].Name.GetHashCode() : 0);
            if (Mathf.Abs(W - _tabLayoutW) < 0.5f && key == _tabLayoutKey && _tabLayout.PerRow > 0) return;
            _tabLayoutW = W;
            _tabLayoutKey = key;
            _tabLayout = UiFit.Tabs(_tabs.Count, W, WidestTabLabel);
        }

        private float WidestTabLabel(int size)
        {
            float w = 0f;
            for (int i = 0; i < _tabs.Count; i++) w = Mathf.Max(w, UIKit.Measure(_tabs[i].Name, size, FontStyle.Bold).x);
            return w;
        }

        private void AddCore(string name, Tab t)
        {
            _tabs.Add(new TabEntry { Name = name, Core = t });
        }

        private int CurrentTab()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_page != null ? _tabs[i].Page == _page : (_tabs[i].Page == null && _tabs[i].Core == _tab)) return i;
            }
            return 0;
        }

        private void SelectTab(int i)
        {
            if (i < 0 || i >= _tabs.Count) return;
            _page = _tabs[i].Page;
            if (_page == null) _tab = _tabs[i].Core;
            _scroll = Vector2.zero;
        }

        // ------------------------------------------------------------------ map

        private void CenterMapOnPlayer()
        {
            Vector2 uv = MapTexture.WorldToUV(Game.PlayerPosition);
            _mapCenter = uv;
            _mapZoom = 2.5f;
        }

        private void DrawMap(Rect area)
        {
            Event e = Event.current;
            UIKit.Rect(area, new Color(0.08f, 0.07f, 0.06f, 0.95f));
            Texture2D tex = Game.MapTexture;
            if (tex == null) return;
            // Map rectangle: keep the texture square, fit the height.
            float span = 1f / _mapZoom;
            float aspect = area.width / area.height;
            float spanU = span * aspect;
            float spanV = span;
            // When zoomed out further than the map is wide, keep it centred instead of pinning it to one side.
            _mapCenter.x = spanU >= 1.2f ? 0.5f : Mathf.Clamp(_mapCenter.x, spanU * 0.5f - 0.1f, 1.1f - spanU * 0.5f);
            _mapCenter.y = spanV >= 1.2f ? 0.5f : Mathf.Clamp(_mapCenter.y, spanV * 0.5f - 0.1f, 1.1f - spanV * 0.5f);
            Rect uvRect = new Rect(_mapCenter.x - spanU * 0.5f, _mapCenter.y - spanV * 0.5f, spanU, spanV);
            GUI.BeginGroup(area);
            Rect local = new Rect(0f, 0f, area.width, area.height);
            GUI.DrawTextureWithTexCoords(local, tex, uvRect);
            Func<Vector3, Vector2> toScreen = w =>
            {
                Vector2 uv = MapTexture.WorldToUV(w);
                return new Vector2((uv.x - uvRect.x) / uvRect.width * area.width, (1f - (uv.y - uvRect.y) / uvRect.height) * area.height);
            };
            Func<Vector2, Vector3> toWorld = s =>
            {
                Vector2 uv = new Vector2(uvRect.x + s.x / area.width * uvRect.width, uvRect.y + (1f - s.y / area.height) * uvRect.height);
                return MapTexture.UVToWorld(uv);
            };
            // Route.
            if (Game.Gps != null && Game.Gps.Route.Count >= 2)
            {
                Color rc = Game.Gps.HasMissionTarget ? UIKit.Gold : new Color(0.75f, 0.4f, 1f);
                for (int i = 1; i < Game.Gps.Route.Count; i++) Line(area.position, toScreen(Game.Gps.Route[i - 1]), toScreen(Game.Gps.Route[i]), rc, 5f);
            }
            // Blips.
            foreach (Blip b in Blip.All)
            {
                if (b == null || !b.ShowOnMap || !b.Visible) continue;
                if (b.Kind == BlipKind.Enemy || b.Kind == BlipKind.Police || b.Kind == BlipKind.Ally || b.Kind == BlipKind.Pickup) continue;
                Vector3 wp = b.Target != null ? b.Target.position : b.Position;
                Vector2 s = toScreen(wp);
                if (s.x < -20f || s.y < -20f || s.x > area.width + 20f || s.y > area.height + 20f) continue;
                float size = string.IsNullOrEmpty(b.Letter) ? 16f : 26f;
                UIKit.Tex(new Rect(s.x - size * 0.5f, s.y - size * 0.5f, size, size), UIKit.Circle, b.Color);
                if (!string.IsNullOrEmpty(b.Letter)) UIKit.Text(new Rect(s.x - size * 0.5f, s.y - size * 0.5f - 1f, size, size), b.Letter, 16, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, false);
                Rect hit = new Rect(s.x - 14f, s.y - 14f, 28f, 28f);
                if (hit.Contains(e.mousePosition) && !string.IsNullOrEmpty(b.Label))
                    UIKit.Text(new Rect(s.x + 16f, s.y - 14f, 400f, 28f), b.Label, 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            // Waypoint.
            if (Game.Gps != null && Game.Gps.HasWaypoint)
            {
                Vector2 s = toScreen(Game.Gps.Waypoint);
                UIKit.Tex(new Rect(s.x - 14f, s.y - 14f, 28f, 28f), UIKit.Ring, new Color(0.8f, 0.4f, 1f));
                UIKit.Tex(new Rect(s.x - 6f, s.y - 6f, 12f, 12f), UIKit.Circle, new Color(0.8f, 0.4f, 1f));
            }
            // 04E map layers (the Underheart layer, quest-giver cards). A click an overlay handles drops no waypoint.
            bool mapClick = e.type == EventType.MouseUp && _mapDragging && e.button == 0 && (e.mousePosition - _mapDragStart).magnitude < 5f;
            bool mapClickUsed = false;
            if (MapOverlays.Count > 0)
            {
                MapContext mctx = new MapContext { Local = local, Area = area, ToScreen = toScreen, ToWorld = toWorld, Mouse = e.mousePosition, Clicked = mapClick };
                MapOverlays.DrawAll(mctx);
                mapClickUsed = mctx.ClickUsed;
            }
            // Player arrow.
            if (Game.Player != null)
            {
                Vector2 s = toScreen(Game.PlayerPosition);
                float yaw = Game.Player.InVehicle ? Game.Player.CurrentVehicle.transform.eulerAngles.y : Game.Player.transform.eulerAngles.y;
                Matrix4x4 m = GUI.matrix;
                UIKit.RotateAround(area.position + s, yaw);
                UIKit.Tex(new Rect(s.x - 14f, s.y - 14f, 28f, 28f), UIKit.Arrow, Color.white);
                GUI.matrix = m;
            }
            // Hover info.
            if (local.Contains(e.mousePosition))
            {
                Vector3 w = toWorld(e.mousePosition);
                District d = CityLayout.DistrictAt(w);
                string street = "";
                if (Game.Layout != null)
                {
                    RoadEdge edge = Game.Layout.RoadAt(w, 3f);
                    if (edge != null && !string.IsNullOrEmpty(edge.Name)) street = edge.Name + ", ";
                }
                UIKit.Text(new Rect(14f, area.height - 40f, area.width - 28f, 30f), street + CityLayout.DistrictName(d), 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            // Input: drag, zoom, waypoint.
            if (e.type == EventType.ScrollWheel && local.Contains(e.mousePosition))
            {
                Vector3 before = toWorld(e.mousePosition);
                _mapZoom = Mathf.Clamp(_mapZoom * (e.delta.y > 0f ? 0.85f : 1.18f), 1f, 12f);
                Vector2 uvBefore = MapTexture.WorldToUV(before);
                float nSpanU = 1f / _mapZoom * aspect, nSpanV = 1f / _mapZoom;
                Vector2 frac = new Vector2(e.mousePosition.x / area.width, 1f - e.mousePosition.y / area.height);
                _mapCenter = new Vector2(uvBefore.x - (frac.x - 0.5f) * nSpanU, uvBefore.y - (frac.y - 0.5f) * nSpanV);
                e.Use();
            }
            // The "Center on me" button and the legend sit on top of the map; let them have their clicks.
            bool overUi = new Rect(area.width - 220f, 12f, 200f, 40f).Contains(e.mousePosition) || new Rect(10f, 10f, 340f, 330f).Contains(e.mousePosition);
            if (e.type == EventType.MouseDown && local.Contains(e.mousePosition) && !overUi)
            {
                if (e.button == 0)
                {
                    _mapDragging = true;
                    _mapDragStart = e.mousePosition;
                    _mapCenterStart = _mapCenter;
                }
                else if (e.button == 1 && Game.Gps != null)
                {
                    Game.Gps.ClearWaypoint();
                }
                e.Use();
            }
            if (e.type == EventType.MouseDrag && _mapDragging)
            {
                Vector2 delta = e.mousePosition - _mapDragStart;
                _mapCenter = _mapCenterStart - new Vector2(delta.x / area.width * uvRect.width, -delta.y / area.height * uvRect.height);
                e.Use();
            }
            if (e.type == EventType.MouseUp && _mapDragging)
            {
                _mapDragging = false;
                if ((e.mousePosition - _mapDragStart).magnitude < 5f && Game.Gps != null && !mapClickUsed) Game.Gps.SetWaypoint(toWorld(e.mousePosition));
                e.Use();
            }
            GUI.EndGroup();
            // Legend.
            float lx = area.x + 16f, ly = area.y + 16f;
            UIKit.Rect(new Rect(lx - 6f, ly - 6f, 340f, 330f), new Color(0f, 0f, 0f, 0.65f));
            string[] legend = { "Click: set waypoint", "Right click: clear waypoint", "Drag: pan   Wheel: zoom", "", "<color=#ffd24d>Yellow</color>: mission", "<color=#ff8030>G</color> Gun shop   <color=#ff5050>H</color> Hospital", "<color=#50e0ff>R</color> Respray   <color=#ffe060>J</color> Jax Customs", "<color=#60ff80>S</color> Safehouse   <color=#60ff80>$</color> Property", "<color=#ff70d0>T</color> Clothes   <color=#ffd040>7</color> Slots", "<color=#ffffff>F</color> Race   <color=#ff5530>!</color> Rampage   <color=#ff9a40>A</color> Forge", "<color=#60ff80>P</color> Ringing payphone   <color=#60ffb0>+</color> Clinic", "<color=#ffffff>Letters</color>: story contacts" };
            foreach (string l in legend)
            {
                UIKit.Text(new Rect(lx, ly, 330f, 26f), l, 18, Color.white, TextAnchor.UpperLeft);
                ly += 26f;
            }
            if (UIKit.Button(new Rect(area.xMax - 220f, area.y + 12f, 200f, 40f), "Center on me", false, 18)) CenterMapOnPlayer();
        }

        /// <summary>Draws a line inside a GUI group whose top-left corner is at origin (in scaled GUI coordinates).</summary>
        private static void Line(Vector2 origin, Vector2 a, Vector2 b, Color c, float width)
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

        // ------------------------------------------------------------------ brief and stats

        private void DrawBrief(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            MissionManager mm = Game.Missions;
            float y = r.y + 20f;
            UIKit.Text(new Rect(r.x + 30f, y, r.width - 60f, 40f), "STORY", 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 50f;
            if (mm != null)
            {
                string current = mm.Active != null ? "Current mission: " + mm.Active.Def.Title + "\n" + mm.Active.ObjectiveText : (mm.NextStory != null ? "Next: " + mm.NextStory.Title + " (" + mm.NextStory.Contact + ")\n" + mm.NextStory.Summary : "Story complete. Las Veyra is yours.");
                UIKit.Text(new Rect(r.x + 30f, y, r.width - 60f, 90f), current, 24, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 110f;
                for (int i = 0; i < mm.Story.Count; i++)
                {
                    MissionDef d = mm.Story[i];
                    bool done = i < mm.StoryProgress;
                    bool next = i == mm.StoryProgress;
                    string mark = done ? "<color=#60ff80>DONE</color>" : (next ? "<color=#ffd24d>NEXT</color>" : "<color=#777777>LOCKED</color>");
                    UIKit.Text(new Rect(r.x + 30f, y, 140f, 30f), mark, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                    UIKit.Text(new Rect(r.x + 160f, y, r.width - 200f, 30f), (i + 1) + ". " + d.Chapter + ": " + d.Title + "  <color=#aaaaaa>- " + d.Contact + "</color>", 20, done || next ? Color.white : new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft);
                    y += 30f;
                }
            }
            if (Game.Activities != null)
            {
                y += 16f;
                UIKit.Text(new Rect(r.x + 30f, y, r.width - 60f, 30f), "SIDE ACTIVITIES", 26, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
                y += 36f;
                UIKit.Text(new Rect(r.x + 30f, y, r.width - 60f, 200f), Game.Activities.Summary(), 20, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
        }

        private void DrawStats(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            Stats s = Game.Stats;
            if (s == null) return;
            UIKit.Text(new Rect(r.x + 30f, r.y + 20f, 600f, 40f), "COMPLETION " + s.CompletionPercent.ToString("0.0") + "%", 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float colW = (r.width - 90f) / 3f;
            float y0 = r.y + 80f;
            int idx = 0;
            for (int i = 0; i < (int)StatId.Count; i++)
            {
                StatId id = (StatId)i;
                int col = idx % 2;
                int row = idx / 2;
                float x = r.x + 30f + col * colW;
                float y = y0 + row * 30f;
                if (y > r.yMax - 40f) break;
                UIKit.Text(new Rect(x, y, colW * 0.62f, 28f), Stats.Label(id), 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft);
                UIKit.Text(new Rect(x + colW * 0.62f, y, colW * 0.35f, 28f), s.Format(id), 20, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                idx++;
            }
            float ax = r.x + 60f + colW * 2f;
            UIKit.Text(new Rect(ax, y0 - 40f, colW, 30f), "ACHIEVEMENTS " + s.Unlocked.Count + "/" + Stats.Achievements.Length, 22, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            float ay = y0;
            foreach (Achievement a in Stats.Achievements)
            {
                bool got = s.Unlocked.Contains(a.Id);
                UIKit.Text(new Rect(ax, ay, colW, 24f), (got ? "<color=#60ff80>[x]</color> " : "[ ] ") + a.Name, 18, got ? Color.white : new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(ax + 34f, ay + 20f, colW - 34f, 20f), a.Description, 15, new Color(0.65f, 0.65f, 0.65f), TextAnchor.UpperLeft);
                ay += 42f;
                if (ay > r.yMax - 40f) break;
            }
            if (Game.Collectibles != null)
            {
                Collectibles c = Game.Collectibles;
                string col = "Data chips " + c.Count(CollectibleKind.DataChip) + "/" + c.Total(CollectibleKind.DataChip) +
                             "    Crystal shards " + c.Count(CollectibleKind.CrystalShard) + "/" + c.Total(CollectibleKind.CrystalShard) +
                             "    Spell stones " + c.Count(CollectibleKind.SpellStone) + "/" + c.Total(CollectibleKind.SpellStone) +
                             "    Echo Relics " + c.Count(CollectibleKind.Relic) + "/" + c.Total(CollectibleKind.Relic) +
                             "    Stunt jumps " + c.Jumps.Count + "/" + (Game.Layout != null ? Game.Layout.StuntJumps.Count : 0);
                UIKit.Text(new Rect(r.x + 30f, r.yMax - 40f, r.width - 60f, 30f), col, 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            }
        }

        // ------------------------------------------------------------------ settings and controls

        private void DrawSettings(Rect r)
        {
            float x = r.x + 40f, y = r.y + 24f;
            string[] pages = { "General", "Combat and controller", "Graphics" };
            for (int i = 0; i < pages.Length; i++)
            {
                if (UIKit.Button(new Rect(x + i * 300f, y, 290f, 42f), pages[i], _settingsPage == i, 20)) _settingsPage = i;
            }
            y += 62f;
            float lw = 300f, sw = Mathf.Min(420f, r.width - lw - 140f);
            if (_settingsPage == 0)
            {
                Settings.MouseSensitivity = SettingSlider(ref y, x, lw, sw, "Mouse sensitivity", Settings.MouseSensitivity, 0.2f, 8f, Settings.MouseSensitivity.ToString("0.0"));
                Settings.FieldOfView = SettingSlider(ref y, x, lw, sw, "Field of view", Settings.FieldOfView, 45f, 90f, Mathf.RoundToInt(Settings.FieldOfView).ToString());
                Settings.MasterVolume = SettingSlider(ref y, x, lw, sw, "Master volume", Settings.MasterVolume, 0f, 1f, Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%");
                Settings.MusicVolume = SettingSlider(ref y, x, lw, sw, "Radio volume", Settings.MusicVolume, 0f, 1f, Mathf.RoundToInt(Settings.MusicVolume * 100f) + "%");
                Settings.SfxVolume = SettingSlider(ref y, x, lw, sw, "Effects volume", Settings.SfxVolume, 0f, 1f, Mathf.RoundToInt(Settings.SfxVolume * 100f) + "%");
                Settings.TrafficDensity = SettingSlider(ref y, x, lw, sw, "Traffic density", Settings.TrafficDensity, 0.25f, 1.6f, Mathf.RoundToInt(Settings.TrafficDensity * 100f) + "%");
                Settings.PedDensity = SettingSlider(ref y, x, lw, sw, "Pedestrian density", Settings.PedDensity, 0.25f, 1.6f, Mathf.RoundToInt(Settings.PedDensity * 100f) + "%");
                Settings.UiScale = SettingSlider(ref y, x, lw, sw, "HUD scale", Settings.UiScale, 0.75f, 1.5f, Mathf.RoundToInt(Settings.UiScale * 100f) + "%");
                Settings.SubtitleScale04L = SettingSlider(ref y, x, lw, sw, "Subtitle size", Settings.SubtitleScale04L, .9f, 1.4f, Mathf.RoundToInt(Settings.SubtitleScale04L * 100f) + "%");
                float tx = x;
                Settings.InvertY = Toggle(new Rect(tx, y, 230f, 40f), "Invert mouse Y", Settings.InvertY); tx += 240f;
                Settings.CameraShake = Toggle(new Rect(tx, y, 230f, 40f), "Camera shake", Settings.CameraShake); tx += 240f;
                Settings.Subtitles = Toggle(new Rect(tx, y, 230f, 40f), "Subtitles", Settings.Subtitles); tx += 240f;
                y += 50f;
                tx = x;
                Settings.ShowFps = Toggle(new Rect(tx, y, 230f, 40f), "Show FPS", Settings.ShowFps); tx += 240f;
                Settings.ShowSpeedometer = Toggle(new Rect(tx, y, 230f, 40f), "Speedometer", Settings.ShowSpeedometer); tx += 240f;
                Settings.DamageNumbers = Toggle(new Rect(tx, y, 230f, 40f), "Damage numbers", Settings.DamageNumbers); tx += 240f;
            }
            else if (_settingsPage == 1)
            {
                UIKit.Text(new Rect(x, y + 8f, lw, 30f), "Difficulty", 22, Color.white, TextAnchor.UpperLeft);
                for (int i = 0; i < 3; i++)
                {
                    DifficultyLevel dl = (DifficultyLevel)i;
                    if (UIKit.Button(new Rect(x + lw + i * 150f, y, 140f, 40f), Settings.DifficultyName(dl), Settings.Difficulty == dl, 20)) Settings.Difficulty = dl;
                }
                y += 50f;
                string blurb = Settings.Difficulty == DifficultyLevel.Story
                    ? "Story (welcoming default): gentler hits, shared attack limits, slower police response, five tonics and full health recovery."
                    : (Settings.Difficulty == DifficultyLevel.Balanced
                        ? "Balanced: moderate combat and police pressure, readable wind-ups, partial health recovery, four tonics, death leaves an echo."
                        : "Veteran: the full challenge. Faster wind-ups, more shooters, tight parries, three tonics, little regen.");
                UIKit.Text(new Rect(x, y, r.width - 80f, 60f), blurb, 18, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 64f;
                Settings.PadLookSpeed = SettingSlider(ref y, x, lw, sw, "Controller look speed", Settings.PadLookSpeed, 60f, 480f, Mathf.RoundToInt(Settings.PadLookSpeed).ToString());
                float tx = x;
                Settings.Vibration = Toggle(new Rect(tx, y, 260f, 40f), "Vibration", Settings.Vibration); tx += 270f;
                Settings.PadInvertY = Toggle(new Rect(tx, y, 260f, 40f), "Invert stick Y", Settings.PadInvertY); tx += 270f;
                Settings.AimAssist = Toggle(new Rect(tx, y, 260f, 40f), "Aim assist", Settings.AimAssist); tx += 270f;
                y += 56f;
                string pad = Pad.Connected ? "Controller: " + Pad.DeviceName + " (" + Pad.Family + ")" : "No controller detected. Plug one in or pair it over Bluetooth; it is picked up instantly.";
                UIKit.Text(new Rect(x, y, r.width - 80f, 60f), pad, 18, Pad.Connected ? UIKit.Cyan : new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            }
            else
            {
                UIKit.Text(new Rect(x, y + 8f, lw, 30f), "Graphics quality", 22, Color.white, TextAnchor.UpperLeft);
                string[] presets = { "Low", "Medium", "High", "Ultra" };
                for (int i = 0; i < 4; i++)
                {
                    if (UIKit.Button(new Rect(x + lw + i * 110f, y, 100f, 40f), presets[i], (int)Settings.Graphics == i, 20))
                    {
                        Settings.Graphics = (GraphicsPreset)i;
                        Settings.ApplyGraphics();
                    }
                }
                y += 54f;
                float tx = x;
                Settings.Bloom = Toggle(new Rect(tx, y, 260f, 40f), "Post effects", Settings.Bloom); tx += 270f;
                Settings.AmbientOcclusion = Toggle(new Rect(tx, y, 260f, 40f), "Ambient occlusion", Settings.AmbientOcclusion); tx += 270f;
                Settings.FilmGrain = Toggle(new Rect(tx, y, 260f, 40f), "Film grain", Settings.FilmGrain); tx += 270f;
                y += 50f;
                tx = x;
                Settings.MotionBlur = Toggle(new Rect(tx, y, 260f, 40f), "Motion blur", Settings.MotionBlur); tx += 270f;
                Settings.AutoExposure = Toggle(new Rect(tx, y, 260f, 40f), "Eye adaptation", Settings.AutoExposure); tx += 270f;
                Settings.TemporalAA = Toggle(new Rect(tx, y, 260f, 40f), "Temporal AA", Settings.TemporalAA); tx += 270f;
                y += 50f;
                tx = x;
                Settings.DepthOfField = Toggle(new Rect(tx, y, 260f, 40f), "Depth of field", Settings.DepthOfField); tx += 270f;
                Settings.ChromaticAberration = Toggle(new Rect(tx, y, 260f, 40f), "Chromatic aberration", Settings.ChromaticAberration); tx += 270f;
                y += 56f;
                float glw = 300f, gsw = Mathf.Min(420f, r.width - glw - 140f);
                Settings.Brightness = SettingSlider(ref y, x, glw, gsw, "Brightness", Settings.Brightness, -1f, 1f, (Settings.Brightness >= 0f ? "+" : "") + Settings.Brightness.ToString("0.0"));
                y += 8f;
                UIKit.Text(new Rect(x, y, r.width - 80f, 80f), "High and Ultra add temporal anti-aliasing, horizon-based ambient occlusion and motion blur on moving objects; Ultra adds 8x MSAA, long-range soft shadows and the widest bloom. Depth of field only runs on cutscenes, the title and the pause menu. Drop to Medium for more frames per second.", 18, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            }
            if (UIKit.Button(new Rect(r.xMax - 270f, r.yMax - 60f, 230f, 44f), "Save settings", false, 20)) { Settings.Save(); Status("Settings saved."); }
        }

        private float SettingSlider(ref float y, float x, float lw, float sw, string label, float value, float min, float max, string shown)
        {
            UIKit.Text(new Rect(x, y + 6f, lw, 30f), label, 22, Color.white, TextAnchor.UpperLeft);
            float v = UIKit.Slider(new Rect(x + lw, y + 4f, sw, 30f), value, min, max);
            UIKit.Text(new Rect(x + lw + sw + 20f, y + 6f, 120f, 30f), shown, 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 44f;
            return v;
        }

        private static bool Toggle(Rect r, string label, bool value)
        {
            if (UIKit.Button(r, label + (value ? ": ON" : ": OFF"), value, 20)) return !value;
            return value;
        }

        private void DrawControls(Rect r)
        {
            string text;
            if (Pad.Connected && Pad.LastUsed)
            {
                string S(PadButton b) => Pad.Glyph(b);
                text =
                    "<b>ON FOOT</b>  Left stick move (tilt to walk or run), " + S(PadButton.L3) + " crouch, right stick camera, " + S(PadButton.R3) + " lock on (flick the stick to switch targets). " + S(PadButton.South) + " jump, " + S(PadButton.East) + " tap to dodge roll, hold to sprint. " + S(PadButton.West) + " drink Shard Tonic, " + S(PadButton.North) + " interact or enter vehicles.\n\n" +
                    "<b>COMBAT</b>  " + S(PadButton.RB) + " light attack (chain combos), " + S(PadButton.RT) + " heavy attack (hold to charge) or fire a gun, " + S(PadButton.LT) + " aim, " + S(PadButton.LB) + " guard (tap right before a hit to parry, then light attack to riposte). " + S(PadButton.LB) + " + " + S(PadButton.RB) + " Third Eye (hold: shift through the Veil), " + S(PadButton.LB) + " + " + S(PadButton.RT) + " cast power, D-pad Up next power (hold: camera view), D-pad Left/Right weapons, D-pad Down reload (hold: throw).\n\n" +
                    "<b>DRIVING</b>  " + S(PadButton.RT) + " gas, " + S(PadButton.LT) + " brake and reverse, left stick steer, " + S(PadButton.RB) + " or " + S(PadButton.East) + " handbrake, " + S(PadButton.South) + " nitro, " + S(PadButton.L3) + " horn, " + S(PadButton.R3) + " look behind, " + S(PadButton.LB) + " aim a drive-by and " + S(PadButton.RB) + " fire, " + S(PadButton.LB) + " and " + S(PadButton.RB) + " pressed together Third Eye (hold: shift through the Veil), " + S(PadButton.West) + " power, D-pad Left/Right radio, D-pad Down lights, " + S(PadButton.North) + " exit.\n\n" +
                    "<b>GENERAL</b>  " + S(PadButton.Start) + " pause, " + S(PadButton.Select) + " map (hold: phone)" + (Pad.Family == PadFamily.PlayStation ? ", touchpad map" : "") + ". In menus: D-pad or stick to move, " + S(PadButton.South) + " select, " + S(PadButton.East) + " back, " + S(PadButton.LB) + "/" + S(PadButton.RB) + " tabs.";
            }
            else
            {
                text =
                    "<b>ON FOOT</b>  WASD move, Shift sprint (tap to dodge roll), Alt walk, Space jump/climb, Ctrl crouch, Mouse look, E interact, F enter or carjack, C swap shoulder, V camera view.\n\n" +
                    "<b>COMBAT</b>  LMB light attack (chain combos; hold for a charged heavy) or fire a gun, RMB guard with fists and melee (tap right before a hit to parry, then attack to riposte) or aim a gun, Middle mouse or Z lock on (swipe the mouse to switch targets), Q drink Shard Tonic, R reload, G quick throw, Tab weapon wheel, mouse wheel or 1-8 weapons.\n\n" +
                    "<b>POWERS</b>  Caps Lock or T: tap to toggle the Third Eye (bullet time), hold to shift through the Veil into the Inner World and back. B casts the selected power, [ and ] choose powers.\n\n" +
                    "<b>DRIVING</b>  W throttle, S brake and reverse, A/D steer, Space handbrake, Shift nitro, E horn, H headlights, C look behind, V camera, comma and period radio, slash radio off, RMB + LMB drive-by, F exit, J side jobs.\n\n" +
                    "<b>SHRINES AND GEAR</b>  Defeated enemies drop Lumen. Rest at a Resonance Shrine to heal, refill tonics, level up, upgrade gear and fast travel. Die and your Lumen waits where you fell. I opens the Gear screen.\n\n" +
                    "<b>GENERAL</b>  M map, P phone, Esc pause, F1 quick controls, F2 debug menu, F3 hide HUD, F12 screenshot. A controller works at any time.";
            }
            UIKit.Text(new Rect(r.x + 40f, r.y + 30f, r.width - 80f, r.height - 60f), text, 21, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
        }

        // ------------------------------------------------------------------ shrine

        private void DrawShrine(float W, float H)
        {
            Progression pr = Game.Progress;
            Shrine sh = pr != null ? pr.ShrineById(_shrineId) : null;
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0.02f, 0f, 0.05f, 0.72f));
            UIKit.Text(new Rect(60f, 30f, W - 120f, 60f), "RESONANCE SHRINE", 44, new Color(0.75f, 0.55f, 1f), TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(60f, 82f, W - 120f, 30f), sh != null ? sh.Name : "", 22, Color.white, TextAnchor.UpperLeft);
            if (pr == null) return;
            string[] tabs = { "LEVEL UP", "GEAR", "TRAVEL", "SKILLS" };
            for (int i = 0; i < tabs.Length; i++)
                if (UIKit.Button(new Rect(60f + i * 210f, 124f, 200f, 46f), tabs[i], _shrineTab == i, 22)) _shrineTab = i;
            if (UIKit.Button(new Rect(W - 260f, 124f, 200f, 46f), "LEAVE", false, 22)) { Close(); return; }
            UIKit.Text(new Rect(W - 700f, 40f, 640f, 40f), "Lumen  " + pr.Lumen.ToString("N0") + "      Level " + pr.Level, 30, new Color(0.85f, 0.7f, 1f), TextAnchor.UpperRight, FontStyle.Bold);
            Rect body = new Rect(60f, 186f, W - 120f, H - 250f);
            UIKit.Rect(body, UIKit.Panel);
            GearMenu.AtShrine = _shrineTab == 1;
            if (_shrineTab == 1) { GearMenu.Draw(body); return; }
            if (_shrineTab == 2) { DrawTravel(body, pr); return; }
            if (_shrineTab == 3) { SkillsPage.Draw(body, true); return; }
            float x = body.x + 40f, y = body.y + 30f;
            UIKit.Text(new Rect(x, y, body.width - 80f, 30f), "Next level costs " + pr.NextLevelCost.ToString("N0") + " Lumen. Each level raises one attribute.", 22, Color.white, TextAnchor.UpperLeft);
            y += 50f;
            for (int a = 0; a < 4; a++)
            {
                int v = pr.AttributeValue(a);
                UIKit.Text(new Rect(x, y + 10f, 260f, 30f), Progression.AttributeName(a) + "  " + v, 26, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(x + 270f, y + 13f, 520f, 30f), Progression.AttributeBlurb(a), 20, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                bool can = pr.CanLevel(a);
                if (UIKit.Button(new Rect(x + 820f, y, 220f, 50f), can ? "Raise (" + pr.NextLevelCost + ")" : (v >= Progression.AttributeCap ? "Maxed" : "Need Lumen"), false, 20) && can) pr.LevelUp(a);
                y += 66f;
            }
            y += 20f;
            int tu = pr.TonicUpgrades;
            string tonic = tu >= Progression.MaxTonicUpgrades ? "Shard Tonic fully expanded (" + (Game.Player != null && Game.Player.Combat != null ? Game.Player.Combat.MaxTonics : 0) + " charges)." : "Add a Shard Tonic charge (" + Progression.TonicUpgradeCost(tu) + " Lumen).";
            UIKit.Text(new Rect(x, y + 12f, 780f, 30f), tonic, 22, new Color(0.45f, 1f, 0.85f), TextAnchor.UpperLeft);
            if (tu < Progression.MaxTonicUpgrades && UIKit.Button(new Rect(x + 820f, y, 220f, 50f), "Expand tonic", false, 20))
            {
                if (!pr.BuyTonicUpgrade()) Status("Not enough Lumen.");
            }
        }

        private void DrawTravel(Rect body, Progression pr)
        {
            float x = body.x + 40f, y = body.y + 30f;
            UIKit.Text(new Rect(x, y, body.width - 80f, 30f), "Travel to any shrine you have awakened.", 22, Color.white, TextAnchor.UpperLeft);
            y += 50f;
            int col = 0;
            foreach (Shrine s in pr.Shrines)
            {
                if (!s.Lit) continue;
                Rect br = new Rect(x + col * 560f, y, 540f, 50f);
                if (UIKit.Button(br, s.Name, s.Id == _shrineId, 20) && s.Id != _shrineId) { pr.TravelTo(s.Id); return; }
                col++;
                if (col >= 2) { col = 0; y += 60f; }
                if (y > body.yMax - 60f) break;
            }
        }

        private string[] _slotInfo;
        private float _slotInfoTime = -99f;
        private int _slotBankRevision04OB = -1;

        /// <summary>Save descriptions are read from disk at most every couple of seconds, not on every GUI event.</summary>
        private void RefreshSlotInfo()
        {
            if (_slotInfo == null) _slotInfo = new string[SaveSystem.Slots + 1];
            for (int i = 0; i <= SaveSystem.Slots; i++) _slotInfo[i] = SaveSystem.Describe(i, _slotBankRevision04OB);
            _slotInfoTime = Time.unscaledTime;
        }

        private void DrawSlotsList(Rect r, bool allowSave)
        {
            if (_slotBankRevision04OB < 0) _slotBankRevision04OB = SaveSystem.ActiveBankRevision04OB;
            if (_slotInfo == null || Time.unscaledTime - _slotInfoTime > 2f) RefreshSlotInfo();
            float y = r.y + 24f;
            UIKit.Text(new Rect(r.x + 30f, y, r.width - 60f, 36f), allowSave ? "SAVE / LOAD" : "LOAD GAME", 30, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            string bankLabel = _slotBankRevision04OB == 6 ? "Living Frontier saves (change bank)" : _slotBankRevision04OB == 5 ? "Unbound realms saves (change bank)" : _slotBankRevision04OB == 4 ? "Crystal realms saves (change bank)" : _slotBankRevision04OB == 3 ? "Fractured wilds saves (change bank)" : _slotBankRevision04OB == 2 ? "Gothic realm saves (change bank)" : "Original / 04O saves (change bank)";
            if (UIKit.Button(new Rect(r.xMax - 510f, y, 480f, 36f), bankLabel, false, 18))
            {
                _slotBankRevision04OB = _slotBankRevision04OB == 0 ? 2 : _slotBankRevision04OB == 2 ? 3 : _slotBankRevision04OB == 3 ? 4 : _slotBankRevision04OB == 4 ? 5 : _slotBankRevision04OB == 5 ? 6 : 0;
                RefreshSlotInfo();
            }
            y += 54f;
            bool matchingBank = _slotBankRevision04OB == SaveSystem.ActiveBankRevision04OB;
            bool canSave = allowSave && matchingBank && Game.Missions != null && Game.Missions.Active == null && (Game.Wanted == null || Game.Wanted.Stars == 0) && Game.Player != null && !Game.Player.IsDead;
            for (int slot = 0; slot <= SaveSystem.Slots; slot++)
            {
                string info = _slotInfo[slot];
                string label = slot == 0 ? "Autosave" : "Slot " + slot;
                UIKit.Rect(new Rect(r.x + 30f, y, r.width - 60f, 64f), new Color(1f, 1f, 1f, 0.06f));
                UIKit.Text(new Rect(r.x + 46f, y + 8f, 160f, 30f), label, 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(r.x + 210f, y + 10f, r.width - 640f, 50f), info ?? "Empty", 20, info != null ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.5f, 0.5f, 0.5f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                if (info != null && UIKit.Button(new Rect(r.xMax - 410f, y + 10f, 180f, 44f), "Load", false, 20))
                {
                    if (Game.Root.LoadGame(slot, _slotBankRevision04OB)) { _titleLoad = false; return; }
                    Status("That save could not be loaded.");
                }
                if (slot > 0 && allowSave)
                {
                    if (UIKit.Button(new Rect(r.xMax - 220f, y + 10f, 180f, 44f), canSave ? "Save" : "Save (n/a)", false, 20))
                    {
                        if (!matchingBank) Status("Change to this world's save bank before saving. Older saves stay in their own bank.");
                        else if (!canSave) Status("You can save when you're not on a mission and not wanted.");
                        else
                        {
                            Status(SaveSystem.Save(slot) ? "Game saved to slot " + slot + "." : (SaveSystem.LastError ?? "Saving failed."));
                            RefreshSlotInfo();
                        }
                    }
                }
                y += 76f;
            }
            UIKit.Text(new Rect(r.x + 30f, r.yMax - 40f, r.width - 60f, 30f), "Saves are stored in " + Application.persistentDataPath, 16, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft);
        }

        // ------------------------------------------------------------------ shops

        private void DrawShop(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.5f));
            Rect r = new Rect(W * 0.5f - 700f, 80f, 1400f, H - 160f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Text(new Rect(r.x + 30f, r.y + 20f, 900f, 50f), _shopName, 40, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.xMax - 430f, r.y + 24f, 400f, 40f), MathUtil.Money(Game.Economy.Money), 34, new Color(0.45f, 0.95f, 0.5f), TextAnchor.UpperRight, FontStyle.Bold);
            if (!string.IsNullOrEmpty(_shopDiscountText)) UIKit.Text(new Rect(r.x + 30f, r.y + 64f, 1000f, 24f), _shopDiscountText, 18, new Color(0.45f, 1f, 0.55f), TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(r.xMax - 210f, r.yMax - 64f, 180f, 46f), "Leave", false, 22)) { Close(); return; }
            switch (_shop)
            {
                case ShopKind.Guns: DrawGunShop(r); break;
                case ShopKind.Garage: DrawGarage(r); break;
                case ShopKind.Clothing: DrawClothing(r); break;
            }
        }

        private void DrawGunShop(Rect r)
        {
            WeaponInventory inv = Game.Player.Weapons.Inventory;
            int rank = Game.Root != null && Game.Root.FreeRoam ? int.MaxValue : (Game.Missions != null ? Game.Missions.StoryProgress : 0);
            WeaponSlot[] cats = { WeaponSlot.Melee, WeaponSlot.Handgun, WeaponSlot.SMG, WeaponSlot.Shotgun, WeaponSlot.Rifle, WeaponSlot.Sniper, WeaponSlot.Heavy, WeaponSlot.Throwable };
            float cy = r.y + 90f;
            foreach (WeaponSlot c in cats)
            {
                if (UIKit.Button(new Rect(r.x + 30f, cy, 220f, 46f), Weapons.SlotName(c), !_lootShop && _gunCategory == c, 20))
                {
                    _gunCategory = c;
                    _lootShop = false;
                }
                cy += 54f;
            }
            if (Game.Loot != null)
            {
                if (UIKit.Button(new Rect(r.x + 30f, cy, 220f, 46f), "Loot guns", _lootShop, 20)) _lootShop = true;
                cy += 54f;
            }
            else _lootShop = false;
            if (UIKit.Button(new Rect(r.x + 30f, cy + 20f, 220f, 46f), "Body armor " + MathUtil.Money(Price(500)), false, 18))
            {
                if (Game.Player.Health.Armor >= Game.Player.Health.MaxArmor - 1f) Status("Your armor is already full.");
                else if (Game.Economy.Spend(Price(500), "Armor")) { Game.Player.Health.AddArmor(100f); Status("Body armor equipped."); }
            }
            if (_lootShop && Game.Loot != null)
            {
                Game.Loot.DrawShop(new Rect(r.x + 290f, r.y + 90f, r.width - 330f, r.height - 150f), Price, Status);
                return;
            }
            float x = r.x + 290f, y = r.y + 90f;
            foreach (WeaponDef d in Weapons.All)
            {
                if (d.Slot != _gunCategory || d.Id == WeaponId.Fists) continue;
                if (d.Price <= 0 && d.Slot != WeaponSlot.Throwable) continue;
                bool owned = inv.Has(d.Id);
                bool locked = d.UnlockRank > rank;
                UIKit.Rect(new Rect(x, y, r.width - 340f, 92f), new Color(1f, 1f, 1f, 0.06f));
                UIKit.Text(new Rect(x + 16f, y + 8f, 600f, 34f), d.Name + (owned ? "  <color=#60ff80>(owned)</color>" : ""), 26, locked ? new Color(0.55f, 0.55f, 0.55f) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                string stats = d.Blurb + (d.IsGun ? "   DMG " + Mathf.RoundToInt(d.Damage * Mathf.Max(1, d.Pellets)) + "  RATE " + Mathf.RoundToInt(60f / Mathf.Max(0.01f, d.Interval)) + "/min  CLIP " + d.ClipSize : "");
                UIKit.Text(new Rect(x + 16f, y + 44f, r.width - 760f, 40f), locked ? "Unlocks after story mission " + d.UnlockRank : stats, 18, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                float bx = r.xMax - 410f;
                if (!locked)
                {
                    if (!owned && d.Price > 0)
                    {
                        if (UIKit.Button(new Rect(bx, y + 22f, 170f, 48f), "Buy " + MathUtil.Money(Price(d.Price)), false, 18) && Game.Economy.Spend(Price(d.Price), "Weapon"))
                        {
                            inv.Give(d.Id, d.UsesAmmo ? Mathf.Max(d.ClipSize * 3, d.AmmoPerPurchase) : 0);
                            inv.Select(d.Id);
                            Status(d.Name + " purchased.");
                            if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f);
                        }
                    }
                    if (d.UsesAmmo && (owned || d.Price <= 0))
                    {
                        if (UIKit.Button(new Rect(bx + 180f, y + 22f, 170f, 48f), "Ammo " + MathUtil.Money(Price(d.AmmoPrice)), false, 18))
                        {
                            if (inv.Ammo[(int)d.Ammo] >= inv.MaxAmmoFor(d.Ammo)) Status("You can't carry more ammo for that.");
                            else if (Game.Economy.Spend(Price(d.AmmoPrice), "Ammo"))
                            {
                                if (!owned) inv.Give(d.Id, d.AmmoPerPurchase);
                                else inv.AddAmmo(d.Ammo, d.AmmoPerPurchase);
                                Status("+" + d.AmmoPerPurchase + " " + d.Ammo + " rounds.");
                            }
                        }
                    }
                }
                y += 100f;
                if (y > r.yMax - 100f) break;
            }
        }

        private void DrawGarage(Rect r)
        {
            PlayerController p = Game.Player;
            if (p == null || !p.InVehicle) { Close(); return; }
            Vehicle v = p.CurrentVehicle;
            float x = r.x + 40f, y = r.y + 90f;
            UIKit.Text(new Rect(x, y, 900f, 34f), v.Def.Name + "   Health " + Mathf.RoundToInt(v.Health / v.Def.Health * 100f) + "%   Top speed " + Mathf.RoundToInt(v.Def.TopSpeedMph * (1f + v.EngineUpgrade * 0.06f)) + " mph", 26, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 36f, 1200f, 30f), "Jax: \"Bring it in clean or bring it in on fire. I fix both.\"", 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft);
            y += 90f;
            if (GarageRow(ref y, x, "Repair and wash", v.Health >= v.Def.Health - 1f && !v.TirePopped[0] && !v.TirePopped[1] && !v.TirePopped[2] && !v.TirePopped[3] ? "Like new" : "", 300)) { v.Repair(); Status("Repaired."); }
            int[] enginePrices = { 5000, 12000, 25000 };
            if (v.EngineUpgrade < 3)
            {
                if (GarageRow(ref y, x, "Engine tune level " + (v.EngineUpgrade + 1) + " (more torque and top speed)", "", enginePrices[v.EngineUpgrade])) { v.EngineUpgrade++; Status("Engine upgraded."); }
            }
            else GarageRow(ref y, x, "Engine tune", "Maxed", 0);
            int[] armorPrices = { 4000, 10000, 20000 };
            if (v.ArmorUpgrade < 3)
            {
                if (GarageRow(ref y, x, "Armor plating level " + (v.ArmorUpgrade + 1) + " (crash and bullet resistance)", "", armorPrices[v.ArmorUpgrade])) { v.ArmorUpgrade++; Status("Armor fitted."); }
            }
            else GarageRow(ref y, x, "Armor plating", "Maxed", 0);
            if (GarageRow(ref y, x, "Bulletproof tires", v.BulletproofTires ? "Fitted" : "", 15000)) { v.BulletproofTires = true; Status("Bulletproof tires fitted."); }
            if (GarageRow(ref y, x, "Nitro (3 charges, Shift while driving)", v.NitroCharges >= 9 ? "Full" : "", 3000)) { v.NitroCharges += 3; Status("Nitro loaded."); }
            UIKit.Text(new Rect(x, y + 10f, 600f, 30f), "Paint (" + MathUtil.Money(Price(400)) + ")", 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 50f;
            for (int i = 0; i < Mats.CarPaints.Length; i++)
            {
                Rect sw = new Rect(x + (i % 8) * 70f, y + (i / 8) * 70f, 60f, 60f);
                UIKit.Rect(sw, Mats.CarPaints[i]);
                if (GUI.Button(sw, GUIContent.none, GUIStyle.none) && Game.Economy.Spend(Price(400), "Paint"))
                {
                    v.Respray(Mats.CarPaints[i]);
                    Status("Fresh paint.");
                }
            }
        }

        /// <summary>Shop price after faction and skill discounts (worked out when the shop opened).</summary>
        private int Price(int basePrice)
        {
            return ForgeMath.Discounted(basePrice, _shopDiscount);
        }

        private bool GarageRow(ref float y, float x, string label, string doneText, int price)
        {
            price = Price(price);
            UIKit.Rect(new Rect(x, y, 1100f, 58f), new Color(1f, 1f, 1f, 0.06f));
            UIKit.Text(new Rect(x + 16f, y + 14f, 760f, 30f), label, 22, Color.white, TextAnchor.UpperLeft);
            bool clicked = false;
            if (!string.IsNullOrEmpty(doneText) || price <= 0) UIKit.Text(new Rect(x + 880f, y + 14f, 200f, 30f), string.IsNullOrEmpty(doneText) ? "" : doneText, 22, new Color(0.5f, 1f, 0.6f), TextAnchor.UpperRight, FontStyle.Bold);
            else if (UIKit.Button(new Rect(x + 900f, y + 7f, 190f, 44f), MathUtil.Money(price), false, 20) && Game.Economy.Spend(price, "Garage")) clicked = true;
            y += 66f;
            return clicked;
        }

        private static readonly string[] OutfitNames = { "Courier Jacket", "Night Runner", "Crystal Coat", "Strip Suit", "Desert Drifter", "Neon Track", "Canal Hoodie", "Old Mile Classic" };
        private static readonly int[] OutfitPrices = { 150, 250, 900, 700, 200, 300, 180, 400 };

        public static Appearance Outfit(int i)
        {
            Appearance a = Appearance.Malik();
            switch (i)
            {
                case 1: a.ShirtRow = 3; a.Shirt = 0; a.JacketRow = 4; a.JacketColor = 1; a.Headwear = Headwear.Cap; a.HatRow = 3; a.Hat = 0; break;
                case 2: a.ShirtRow = 3; a.Shirt = 2; a.JacketRow = 4; a.JacketColor = 4; a.PantsRow = 3; a.Pants = 9; break;
                case 3: a.ShirtRow = 3; a.Shirt = 10; a.JacketRow = 3; a.JacketColor = 0; a.PantsRow = 3; a.Pants = 0; a.Glasses = true; break;
                case 4: a.ShirtRow = 1; a.Shirt = 5; a.JacketRow = 1; a.JacketColor = 11; a.Headwear = Headwear.Bandana; a.HatRow = 1; a.Hat = 3; break;
                case 5: a.ShirtRow = 4; a.Shirt = 0; a.Jacket = true; a.JacketRow = 4; a.JacketColor = 7; a.PantsRow = 1; a.Pants = 0; break;
                case 6: a.ShirtRow = 1; a.Shirt = 13; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 9; a.Headwear = Headwear.Beanie; a.HatRow = 1; a.Hat = 0; break;
                case 7: a.ShirtRow = 3; a.Shirt = 10; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 5; a.PantsRow = 1; a.Pants = 11; a.Headwear = Headwear.Cap; a.HatRow = 1; a.Hat = 11; break;
            }
            return a;
        }

        public int CurrentOutfit;

        private void DrawClothing(Rect r)
        {
            float x = r.x + 40f, y = r.y + 100f;
            for (int i = 0; i < OutfitNames.Length; i++)
            {
                UIKit.Rect(new Rect(x, y, 900f, 60f), new Color(1f, 1f, 1f, 0.06f));
                UIKit.Text(new Rect(x + 16f, y + 14f, 600f, 32f), OutfitNames[i] + (CurrentOutfit == i ? "  <color=#60ff80>(wearing)</color>" : ""), 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                if (CurrentOutfit != i && UIKit.Button(new Rect(x + 700f, y + 8f, 180f, 44f), MathUtil.Money(Price(OutfitPrices[i])), false, 20) && Game.Economy.Spend(Price(OutfitPrices[i]), "Clothes"))
                {
                    CurrentOutfit = i;
                    Game.Player.Rig.ChangeLook(Outfit(i));
                    Game.Player.Weapons.RefreshModel();
                    Status("Looking sharp.");
                }
                y += 68f;
            }
        }

        // ------------------------------------------------------------------ safehouse

        private void DrawSafehouse(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.5f));
            Rect r = new Rect(W * 0.5f - 560f, 120f, 1120f, H - 240f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Text(new Rect(r.x + 30f, r.y + 20f, 800f, 50f), _shopName, 40, new Color(0.4f, 1f, 0.55f), TextAnchor.UpperLeft, FontStyle.Bold);
            float x = r.x + 40f, y = r.y + 100f;
            if (UIKit.Button(new Rect(x, y, 330f, 56f), "Sleep 6 hours", false, 22))
            {
                if (Game.Atmo != null) Game.Atmo.AdvanceHours(6f);
                Game.Player.Health.Heal(Game.Player.Health.MaxHealth);
                if (Game.Abilities != null) Game.Abilities.AddEnergy(999f);
                Status("You wake up rested. It's " + (Game.Atmo != null ? Game.Atmo.ClockText : "later") + ".");
            }
            if (UIKit.Button(new Rect(x + 350f, y, 330f, 56f), "Wardrobe: default clothes", false, 22))
            {
                CurrentOutfit = 0;
                Game.Player.Rig.ChangeLook(Outfit(0));
                Game.Player.Weapons.RefreshModel();
            }
            if (UIKit.Button(new Rect(x + 700f, y, 330f, 56f), "Weather forecast", false, 22))
                Status("Forecast: " + (Game.Atmo != null ? Game.Atmo.Target.ToString() : "clear") + ".");
            y += 80f;
            Rect slots = new Rect(r.x + 20f, y, r.width - 40f, r.yMax - y - 80f);
            DrawSlotsList(slots, true);
            if (UIKit.Button(new Rect(r.xMax - 210f, r.yMax - 64f, 180f, 46f), "Leave", false, 22)) Close();
        }

        // ------------------------------------------------------------------ slots

        private void DrawSlots(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            Rect r = new Rect(W * 0.5f - 520f, 140f, 1040f, 700f);
            UIKit.Rect(r, new Color(0.15f, 0.02f, 0.12f, 0.95f));
            UIKit.Frame(r, UIKit.Pink, 4f);
            UIKit.Text(new Rect(r.x, r.y + 20f, r.width, 60f), _shopName, 46, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Text(new Rect(r.x, r.y + 80f, r.width, 30f), "Three 7s pay 50x. Three of a kind pay 12x. Any pair returns your bet.", 22, Color.white, TextAnchor.UpperCenter);
            for (int i = 0; i < 3; i++)
            {
                Rect reel = new Rect(r.x + 130f + i * 270f, r.y + 160f, 240f, 240f);
                UIKit.Rect(reel, new Color(0.95f, 0.92f, 0.85f));
                UIKit.Frame(reel, UIKit.Gold, 5f);
                string sym = _reelSymbols[_reels[i]];
                Color c = sym == "7" ? new Color(0.85f, 0.1f, 0.1f) : (sym == "SKULL" ? Color.black : new Color(0.25f, 0.1f, 0.5f));
                UIKit.Text(reel, sym, sym.Length > 2 ? 52 : 120, c, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            }
            UIKit.Text(new Rect(r.x, r.y + 420f, r.width, 50f), _slotResult, 36, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Text(new Rect(r.x, r.y + 480f, r.width, 40f), "Cash " + MathUtil.Money(Game.Economy.Money) + "     Bet " + MathUtil.Money(_slotBet), 26, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            if (_spinTime <= 0f)
            {
                if (UIKit.Button(new Rect(r.x + 120f, r.y + 540f, 180f, 60f), "Bet $100", _slotBet == 100, 22)) _slotBet = 100;
                if (UIKit.Button(new Rect(r.x + 320f, r.y + 540f, 180f, 60f), "Bet $500", _slotBet == 500, 22)) _slotBet = 500;
                if (UIKit.Button(new Rect(r.x + 520f, r.y + 540f, 200f, 60f), "SPIN", true, 28))
                {
                    if (Game.Economy.Spend(_slotBet, "Slots"))
                    {
                        _spinTime = 1.6f;
                        _slotResult = "";
                        if (Game.Audio != null) Game.Audio.Play2D(SfxId.SlotSpin, 0.8f);
                    }
                }
                if (UIKit.Button(new Rect(r.x + 740f, r.y + 540f, 180f, 60f), "Leave", false, 22)) Close();
            }
        }

        private void ResolveSpin()
        {
            int a = _reels[0], b = _reels[1], c = _reels[2];
            int mult = 0;
            if (a == b && b == c) mult = _reelSymbols[a] == "7" ? 50 : 12;
            else if (a == b || b == c || a == c) mult = 1;
            if (mult > 0)
            {
                int win = _slotBet * mult;
                Game.Economy.Earn(win, "Slots");
                if (Game.Stats != null) Game.Stats.Add(StatId.CasinoWinnings, win - _slotBet);
                _slotResult = mult >= 12 ? "JACKPOT! +" + MathUtil.Money(win) : "A pair. Your bet comes back.";
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.SlotWin, 0.9f);
            }
            else
            {
                if (Game.Stats != null) Game.Stats.Add(StatId.CasinoWinnings, -_slotBet);
                _slotResult = "No luck. The house thanks you.";
            }
        }

        // ------------------------------------------------------------------ choice

        /// <summary>
        /// The pausing choice card. The body is measured once per text (ChoiceCardLayout04E): a short body keeps the classic
        /// half-screen card at size 24; a long one grows the card, then uses a smaller size, and only then scrolls (mouse
        /// wheel, or the right stick on a pad), so it never runs under the buttons.
        /// </summary>
        private void DrawChoice(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.7f));
            if (!ReferenceEquals(_choiceFitFor, _choiceText) || _choiceFitH != H)
            {
                _choiceFitFor = _choiceText;
                _choiceFitH = H;
                string body = _choiceText ?? "";
                GUIContent content = new GUIContent(body);
                _choiceFit = ChoiceCardLayout04E.Fit(H, size => body.Length == 0 ? 0f : UIKit.Style(size, FontStyle.Normal, TextAnchor.UpperCenter, true).CalcHeight(content, ChoiceCardLayout04E.TextWidth));
                // Scrolling text wraps a little narrower (the scroll bar takes 24 px): measure it at that width.
                if (_choiceFit.Scroll) _choiceFit.TextHeight = UIKit.Style(_choiceFit.TextSize, FontStyle.Normal, TextAnchor.UpperCenter, true).CalcHeight(content, ChoiceCardLayout04E.TextWidth - 24f);
            }
            float cardH = _choiceFit.CardHeight;
            Rect r = new Rect(W * 0.5f - ChoiceCardLayout04E.Width * 0.5f, (H - cardH) * 0.5f, ChoiceCardLayout04E.Width, cardH);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Frame(r, UIKit.Cyan, 3f);
            UIKit.Text(new Rect(r.x, r.y + 30f, r.width, 60f), _choiceTitle, 48, UIKit.Cyan, TextAnchor.UpperCenter, FontStyle.Bold);
            Rect area = new Rect(r.x + ChoiceCardLayout04E.SidePad, r.y + ChoiceCardLayout04E.TitleBlock, ChoiceCardLayout04E.TextWidth, _choiceFit.AreaHeight);
            if (_choiceFit.Scroll)
            {
                Event ev = Event.current;
                if (ev != null && ev.type == EventType.Repaint && Pad.Connected && Mathf.Abs(Pad.RightStick.y) > 0.2f)
                    _choiceScroll.y = Mathf.Max(0f, _choiceScroll.y - Pad.RightStick.y * 700f * Time.unscaledDeltaTime);
                Rect view = new Rect(0f, 0f, area.width - 24f, _choiceFit.TextHeight + 8f);
                _choiceScroll = GUI.BeginScrollView(area, _choiceScroll, view, false, true);
                UIKit.Text(new Rect(0f, 0f, view.width, view.height), _choiceText, _choiceFit.TextSize, Color.white, TextAnchor.UpperCenter, FontStyle.Normal, true, true);
                GUI.EndScrollView();
            }
            else UIKit.Text(area, _choiceText, _choiceFit.TextSize, Color.white, TextAnchor.UpperCenter, FontStyle.Normal, true, true);
            if (_choiceOptions == null) return;
            float bw = (r.width - 120f - (_choiceOptions.Length - 1) * 30f) / _choiceOptions.Length;
            for (int i = 0; i < _choiceOptions.Length; i++)
            {
                Rect br = new Rect(r.x + 60f + i * (bw + 30f), r.yMax - 140f, bw, 90f);
                if (UIKit.Button(br, _choiceOptions[i], false, 26))
                {
                    Action<int> cb = _choiceCallback;
                    _choiceCallback = null;
                    _screen = MenuScreen.None;
                    Close();
                    if (cb != null)
                    {
                        try { cb(i); }
                        catch (Exception ex) { Log.Exception("Choice callback", ex); }
                    }
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ debug

        private void DrawDebug(float W, float H)
        {
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            Rect r = new Rect(60f, 60f, W - 120f, H - 120f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Text(new Rect(r.x + 30f, r.y + 16f, 900f, 44f), "DEBUG MENU (F2 to close)", 34, new Color(1f, 0.5f, 0.3f), TextAnchor.UpperLeft, FontStyle.Bold);
            string[] tabs = { "Player", "Teleport", "Vehicles", "World", "Missions" };
            int tabCount = tabs.Length + DebugPages.Pages.Count;
            float dw = Mathf.Min(180f, (r.width - 60f) / Mathf.Max(1, tabCount) - 10f);
            for (int i = 0; i < tabCount; i++)
            {
                string label = i < tabs.Length ? tabs[i] : DebugPages.Pages[i - tabs.Length].Title;
                if (UIKit.Button(new Rect(r.x + 30f + i * (dw + 10f), r.y + 70f, dw, 44f), label, _debugTab == i, 20)) _debugTab = i;
            }
            float x = r.x + 30f, y = r.y + 140f;
            if (_debugTab >= tabs.Length)
            {
                int pi = _debugTab - tabs.Length;
                if (pi < DebugPages.Pages.Count)
                {
                    MenuPage page = DebugPages.Pages[pi];
                    try { page.Draw(new Rect(x, y, r.width - 60f, r.height - 170f)); }
                    catch (Exception e) { Log.Exception("Debug page " + page.Title, e); _debugTab = 0; }
                }
                else _debugTab = 0;
                return;
            }
            Cheats ch = Game.Cheats;
            PlayerController p = Game.Player;
            switch (_debugTab)
            {
                case 0:
                {
                    if (UIKit.Button(new Rect(x, y, 300f, 50f), "Full health + armor", false, 20)) ch.Run("TOUGHSKIN");
                    if (UIKit.Button(new Rect(x + 320f, y, 300f, 50f), "All weapons + ammo", false, 20)) ch.Run("ARSENAL");
                    if (UIKit.Button(new Rect(x + 640f, y, 300f, 50f), "+$250,000", false, 20)) ch.Run("CASHFLOW");
                    y += 64f;
                    if (UIKit.Button(new Rect(x, y, 300f, 50f), "God mode: " + (Game.Root.GodMode ? "ON" : "OFF"), Game.Root.GodMode, 20)) ch.Run("UNDYING");
                    if (UIKit.Button(new Rect(x + 320f, y, 300f, 50f), "Unlock all powers", false, 20)) ch.Run("THIRDEYE");
                    if (UIKit.Button(new Rect(x + 640f, y, 300f, 50f), "Moon jump: " + (PlayerController.MoonJump ? "ON" : "OFF"), PlayerController.MoonJump, 20)) ch.Run("LOWGRAVITY");
                    y += 80f;
                    UIKit.Text(new Rect(x, y, 300f, 40f), "Wanted level", 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                    for (int s = 0; s <= 5; s++)
                        if (UIKit.Button(new Rect(x + 200f + s * 90f, y, 80f, 46f), s.ToString(), Game.Wanted != null && Game.Wanted.Stars == s, 22) && Game.Wanted != null) Game.Wanted.SetStars(s);
                    y += 64f;
                    if (UIKit.Button(new Rect(x, y, 300f, 50f), "Police ignore me: " + (Game.Wanted != null && Game.Wanted.Suppressed ? "ON" : "OFF"), Game.Wanted != null && Game.Wanted.Suppressed, 20)) ch.Run("LAWLESS");
                    break;
                }
                case 1:
                {
                    List<KeyValuePair<string, Vector3>> places = TeleportList();
                    int col = 0, row = 0;
                    foreach (KeyValuePair<string, Vector3> kv in places)
                    {
                        Rect br = new Rect(x + col * 360f, y + row * 50f, 350f, 44f);
                        if (br.yMax > r.yMax - 20f) break;
                        if (UIKit.Button(br, kv.Key, false, 17))
                        {
                            Close();
                            Game.Root.TeleportPlayer(kv.Value);
                            return;
                        }
                        col++;
                        if (col >= 4) { col = 0; row++; }
                    }
                    break;
                }
                case 2:
                {
                    int col = 0, row = 0;
                    foreach (VehicleDef d in VehicleDb.All)
                    {
                        Rect br = new Rect(x + col * 360f, y + row * 50f, 350f, 44f);
                        if (UIKit.Button(br, d.Name, false, 18))
                        {
                            Close();
                            ch.SpawnVehicle(d);
                            return;
                        }
                        col++;
                        if (col >= 4) { col = 0; row++; }
                    }
                    break;
                }
                case 3:
                {
                    float[] hours = { 6f, 9f, 12f, 17f, 19.5f, 22f, 1f };
                    UIKit.Text(new Rect(x, y, 200f, 40f), "Time", 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                    for (int i = 0; i < hours.Length; i++)
                        if (UIKit.Button(new Rect(x + 120f + i * 130f, y, 120f, 46f), MathUtil.Clock(hours[i]), false, 20) && Game.Atmo != null) Game.Atmo.SetTime(hours[i]);
                    y += 64f;
                    UIKit.Text(new Rect(x, y, 200f, 40f), "Weather", 24, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
                    string[] weathers = Enum.GetNames(typeof(Weather));
                    for (int i = 0; i < weathers.Length; i++)
                        if (UIKit.Button(new Rect(x + 120f + i * 170f, y, 160f, 46f), weathers[i], Game.Atmo != null && Game.Atmo.Target == (Weather)i, 20) && Game.Atmo != null) Game.Atmo.SetWeather((Weather)i, true);
                    y += 64f;
                    if (UIKit.Button(new Rect(x, y, 300f, 50f), "Freeze time: " + (Game.Atmo != null && Game.Atmo.Frozen ? "ON" : "OFF"), Game.Atmo != null && Game.Atmo.Frozen, 20) && Game.Atmo != null) Game.Atmo.Frozen = !Game.Atmo.Frozen;
                    if (UIKit.Button(new Rect(x + 320f, y, 300f, 50f), "Blow up nearby cars", false, 20)) ch.Run("DETONATE");
                    if (UIKit.Button(new Rect(x + 640f, y, 300f, 50f), "Clear traffic and peds", false, 20))
                    {
                        if (Game.Traffic != null) Game.Traffic.ClearAround(Game.PlayerPosition, 400f);
                        if (Game.Peds != null) Game.Peds.ClearAround(Game.PlayerPosition, 400f);
                    }
                    break;
                }
                case 4:
                {
                    MissionManager mm = Game.Missions;
                    if (mm == null) break;
                    int i = 0;
                    foreach (MissionDef d in mm.Story)
                    {
                        Rect br = new Rect(x + (i % 3) * 470f, y + (i / 3) * 56f, 460f, 48f);
                        if (UIKit.Button(br, (i + 1) + ". " + d.Title, i == mm.StoryProgress, 18))
                        {
                            Close();
                            mm.DebugStart(i);
                            return;
                        }
                        i++;
                    }
                    y += ((mm.Story.Count + 2) / 3) * 56f + 20f;
                    if (UIKit.Button(new Rect(x, y, 300f, 50f), "Pass current mission", false, 20) && mm.Active != null) { Close(); mm.Pass(); return; }
                    if (UIKit.Button(new Rect(x + 320f, y, 300f, 50f), "Abort current mission", false, 20) && mm.Active != null) { Close(); mm.Fail("Aborted"); return; }
                    break;
                }
            }
        }

        private List<KeyValuePair<string, Vector3>> TeleportList()
        {
            List<KeyValuePair<string, Vector3>> l = new List<KeyValuePair<string, Vector3>>();
            CityLayout L = Game.Layout;
            if (L == null) return l;
            HashSet<string> seen = new HashSet<string>();
            PoiType[] types = { PoiType.Safehouse, PoiType.Contact, PoiType.GunShop, PoiType.Hospital, PoiType.Respray, PoiType.Garage, PoiType.Casino, PoiType.Property, PoiType.Clothing, PoiType.Police };
            foreach (PoiType t in types)
                foreach (Poi p in L.Pois)
                    if (p.Type == t && seen.Add(p.Name)) l.Add(new KeyValuePair<string, Vector3>(p.Name, p.Position));
            foreach (SpecialSite s in L.Sites) l.Add(new KeyValuePair<string, Vector3>(s.Name, s.Position));
            return l;
        }
    }
}
