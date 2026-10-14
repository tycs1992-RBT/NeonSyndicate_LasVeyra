using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Forge screen: opens from a Forge station and, like the shops, pauses the game and takes the input (mouse,
    /// arrow keys, or the controller: D-pad or stick to move, A to choose and craft, B or Start to close, LB and RB to
    /// switch tabs). Tabs: GEAR (design a piece by slot, rarity, set and element), INFUSE (re-tune the Weapon Core),
    /// BREWS (timed effects), UPGRADES (permanent ranks), SALVAGE at Dante Rook's forge, WARDENS at the Frost Wardens
    /// outpost and ARMORY for loot guns (ForgeScreen.Armory.cs). A shrine's light forge shows only brews and infusions.
    /// </summary>
    public sealed partial class ForgeScreen : MonoBehaviour
    {
        private enum Tab { Gear, Infuse, Brews, Upgrades, Salvage, Wardens, Armory }

        private bool _open;
        private bool _closeRequested;
        private ForgeStation _station;
        private string _title = "THE FORGE";
        private Tab _tab = Tab.Gear;
        private readonly List<Tab> _tabs = new List<Tab>();
        private GearOrder _order = new GearOrder { Slot = GearSlot.Shell, Rarity = Rarity.Common, Set = -1, Element = Element.None, Heart = null };
        private string _recipe;
        private string _salvage;
        private int _salvagePage;
        private int _warden;
        private string _status = "";
        private float _statusUntil;
        private bool _statusGood;
        private readonly List<KeyValuePair<MaterialDef, int>> _held = new List<KeyValuePair<MaterialDef, int>>();

        private static readonly string[] SetShort = { "Revenant", "Warden", "Rift Walker" };

        public bool IsOpen { get { return _open; } }

        // ------------------------------------------------------------------ open / close

        public void Open(ForgeStation station, string title)
        {
            if (_open) return;
            if (Game.Menus != null && (Game.Menus.IsOpen || Game.Menus.TitleOpen)) return;
            if (Game.State != GameState.Playing) return;
            _open = true;
            _closeRequested = false;
            _station = station;
            _title = string.IsNullOrEmpty(title) ? "THE FORGE" : title;
            BuildTabs();
            if (!_tabs.Contains(_tab)) _tab = _tabs[0];
            FixOrder();
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            GameTime.SetPaused(true);
            Game.State = GameState.Paused;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneOpen, 0.5f);
        }

        /// <summary>Closes without touching input or time (new game, load, title: the session code resets those).</summary>
        public void CloseSilently()
        {
            _open = false;
            _closeRequested = false;
        }

        private void DoClose()
        {
            _open = false;
            _closeRequested = false;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            GameTime.SetPaused(false);
            if (Game.State == GameState.Paused) Game.State = GameState.Playing;
            if (Game.State == GameState.Playing) GInput.SetCursorLocked(true);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.45f);
        }

        private void BuildTabs()
        {
            _tabs.Clear();
            if (ForgeMath.StationForgesGear(_station)) _tabs.Add(Tab.Gear);
            _tabs.Add(Tab.Infuse);
            _tabs.Add(Tab.Brews);
            if (_station != ForgeStation.Shrine) _tabs.Add(Tab.Upgrades);
            if (_station == ForgeStation.Forgewright) _tabs.Add(Tab.Salvage);
            if (_station == ForgeStation.Outpost) _tabs.Add(Tab.Wardens);
            if (_station != ForgeStation.Shrine && Game.Loot != null) _tabs.Add(Tab.Armory);
        }

        private static string TabName(Tab t)
        {
            switch (t)
            {
                case Tab.Gear: return "GEAR";
                case Tab.Infuse: return "INFUSE";
                case Tab.Brews: return "BREWS";
                case Tab.Upgrades: return "UPGRADES";
                case Tab.Salvage: return "SALVAGE";
                case Tab.Armory: return "ARMORY";
                default: return "WARDENS";
            }
        }

        private void SelectTab(int i)
        {
            if (_tabs.Count == 0) return;
            _tab = _tabs[(i % _tabs.Count + _tabs.Count) % _tabs.Count];
            _recipe = null;
        }

        private void Status(string s, bool good)
        {
            _status = s ?? "";
            _statusGood = good;
            _statusUntil = Time.unscaledTime + 4f;
        }

        private void Update()
        {
            if (!_open) return;
            if (Game.State == GameState.Title || Game.State == GameState.Loading)
            {
                CloseSilently();
                return;
            }
            if (Act.Pause || GInput.UiBack)
            {
                // The release of B must not become a dodge roll once gameplay has the pad again.
                Pad.Consume(PadButton.East);
                _closeRequested = true;
            }
            if (Pad.Connected && _tabs.Count > 1)
            {
                int cur = Mathf.Max(0, _tabs.IndexOf(_tab));
                if (Pad.Down(PadButton.LB)) SelectTab(cur - 1);
                if (Pad.Down(PadButton.RB)) SelectTab(cur + 1);
            }
        }

        // Closing waits for LateUpdate, so the same Esc press cannot also open the pause menu this frame.
        private void LateUpdate()
        {
            if (_open && _closeRequested) DoClose();
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (!_open) return;
            try { Draw(); }
            catch (System.Exception e)
            {
                // A drawing bug must not leave the game stuck behind a broken screen.
                Log.Exception("Forge screen", e);
                _closeRequested = true;
            }
        }

        private void Draw()
        {
            GUI.depth = 0;
            UIKit.BeginScaledMenu();
            CraftingSystem c = Game.Crafting;
            float W = UIKit.Width, H = UIKit.Height;
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            float pw = Mathf.Min(1640f, W - 60f);
            Rect panel = new Rect((W - pw) * 0.5f, 46f, pw, H - 92f);
            UIKit.Rect(panel, UIKit.Panel);
            UIKit.Frame(panel, new Color(1f, 0.6f, 0.2f, 0.55f), 2f);
            if (c == null)
            {
                UIKit.Text(new Rect(panel.x + 30f, panel.y + 30f, panel.width - 60f, 40f), "The Forge is not available.", 26, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(panel.xMax - 230f, panel.yMax - 66f, 200f, 48f), "LEAVE", false, 22)) _closeRequested = true;
                return;
            }
            UIKit.Text(new Rect(panel.x + 30f, panel.y + 14f, panel.width * 0.6f, 50f), _title, 40, ProgressionUI.ForgeOrange, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(panel.xMax - 430f, panel.y + 18f, 400f, 40f), MathUtil.Money(Game.Economy != null ? Game.Economy.Money : 0), 32, new Color(0.45f, 0.95f, 0.5f), TextAnchor.UpperRight, FontStyle.Bold);
            FactionRank fr = c.ForgeRank;
            string standing = "Forgewrights: " + ProgressionUI.Colored(FactionRules.RankName(fr), FactionRules.RankColor(fr)) + " (" + (Game.Factions != null ? Game.Factions.Rep(CraftingSystem.ForgewrightsId) : 0) + ")" +
                              "   Forges up to " + ProgressionUI.Colored(GearSystem.RarityName(c.MaxRarity), GearSystem.RarityColor(c.MaxRarity)) + " gear" +
                              (c.FeeDiscount > 0.001f ? "   Fees " + ProgressionUI.Pct(c.FeeDiscount) + " off" : "");
            if (_station == ForgeStation.Shrine) standing += "   Shrine forge: brews and infusions only.";
            UIKit.Text(new Rect(panel.x + 30f, panel.y + 64f, panel.width - 60f, 26f), standing, 18, new Color(0.88f, 0.88f, 0.9f), TextAnchor.UpperLeft);
            float tx = panel.x + 30f;
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (UIKit.Button(new Rect(tx, panel.y + 98f, 190f, 46f), TabName(_tabs[i]), _tabs[i] == _tab, 20)) SelectTab(i);
                tx += 200f;
            }
            // The body stops above the status line (panel.yMax - 112), so FORGE IT, INFUSE and BREW never sit under it.
            Rect body = new Rect(panel.x + 30f, panel.y + 160f, panel.width - 60f, panel.height - 160f - 116f);
            float leftW = Mathf.Min(600f, body.width * 0.42f);
            Rect left = new Rect(body.x, body.y, leftW, body.height);
            Rect right = new Rect(left.xMax + 30f, body.y, body.xMax - left.xMax - 30f, body.height);
            UIKit.Rect(right, new Color(1f, 1f, 1f, 0.04f));
            switch (_tab)
            {
                case Tab.Gear: DrawGear(left, right, c); break;
                case Tab.Infuse: DrawRecipes(left, right, c, RecipeKind.Infusion); break;
                case Tab.Brews: DrawRecipes(left, right, c, RecipeKind.Brew); break;
                case Tab.Upgrades: DrawRecipes(left, right, c, RecipeKind.Upgrade); break;
                case Tab.Salvage: DrawSalvage(left, right, c); break;
                case Tab.Wardens: DrawWardens(left, right, c); break;
                case Tab.Armory: DrawArmory(left, right, c); break;
            }
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(panel.x + 30f, panel.yMax - 112f, panel.width - 300f, 50f), _status, 20, _statusGood ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            UIKit.Text(new Rect(panel.x + 30f, panel.yMax - 50f, panel.width - 300f, 26f), ProgressionUI.NavHint("choose and craft"), 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft);
            if (UIKit.Button(new Rect(panel.xMax - 230f, panel.yMax - 66f, 200f, 48f), "LEAVE", false, 22)) _closeRequested = true;
        }

        private static void Label(float x, float y, float w, string s)
        {
            UIKit.Text(new Rect(x, y, w, 24f), s, 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        /// <summary>Material lines (have / need) and the cash line. Returns the y below them.</summary>
        private static float DrawCost(float x, float y, float w, ForgeCost price, ForgeCost basePrice, IForgeWallet wallet)
        {
            if (price == null) return y;
            Label(x, y, w, "COST");
            y += 26f;
            foreach (KeyValuePair<string, int> kv in price.Materials)
            {
                MaterialDef m = MaterialDb.Get(kv.Key);
                int have = wallet != null ? wallet.Count(kv.Key) : 0;
                string name = m != null ? ProgressionUI.Colored(m.Name, m.RarityColor) : kv.Key;
                UIKit.Text(new Rect(x + 12f, y, w * 0.7f, 24f), name, 18, Color.white, TextAnchor.UpperLeft);
                UIKit.Text(new Rect(x, y, w - 12f, 24f), have + " / " + kv.Value, 18, have >= kv.Value ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperRight, FontStyle.Bold);
                y += 25f;
            }
            if (price.Cash > 0)
            {
                bool ok = wallet != null && wallet.Cash >= price.Cash;
                string was = basePrice != null && basePrice.Cash > price.Cash ? "  <color=#999999>(was " + MathUtil.Money(basePrice.Cash) + ")</color>" : "";
                UIKit.Text(new Rect(x + 12f, y, w * 0.7f, 24f), "Smith's fee" + was, 18, Color.white, TextAnchor.UpperLeft);
                UIKit.Text(new Rect(x, y, w - 12f, 24f), MathUtil.Money(price.Cash), 18, ok ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperRight, FontStyle.Bold);
                y += 25f;
            }
            return y + 8f;
        }

        // ------------------------------------------------------------------ gear

        private void FixOrder()
        {
            if (_order.Set >= 0 && !GearSystem.SetAllowed(_order.Slot, _order.Rarity)) _order.Set = -1;
            if (_order.Element != Element.None && !GearSystem.ElementAllowed(_order.Slot, _order.Rarity)) _order.Element = Element.None;
            if (_order.Element == Element.None && GearSystem.ElementRequired(_order.Slot)) _order.Element = Element.Volt;
            if (_order.Rarity == Rarity.Mythic && System.Array.IndexOf(ForgeDb.Hearts, _order.Heart) < 0)
            {
                _order.Heart = ForgeDb.Hearts[0];
                foreach (string h in ForgeDb.Hearts) if (Game.Materials != null && Game.Materials.Count(h) > 0) { _order.Heart = h; break; }
            }
        }

        private void DrawGear(Rect left, Rect right, CraftingSystem c)
        {
            float x = left.x, y = left.y, w = left.width;
            float bw3 = (w - 20f) / 3f;
            Label(x, y, w, "PIECE");
            y += 26f;
            for (int i = 0; i < GearSystem.SlotCount; i++)
            {
                GearSlot s = (GearSlot)i;
                Rect br = new Rect(x + (i % 3) * (bw3 + 10f), y + (i / 3) * 50f, bw3, 44f);
                if (UIKit.Button(br, GearSystem.SlotName(s), _order.Slot == s, 18)) { _order.Slot = s; FixOrder(); }
            }
            y += 108f;
            Label(x, y, w, "RARITY");
            y += 26f;
            for (int i = 0; i <= (int)Rarity.Mythic; i++)
            {
                Rarity r = (Rarity)i;
                bool open = r <= c.MaxRarity;
                string label = ProgressionUI.Colored(GearSystem.RarityName(r), GearSystem.RarityColor(r));
                if (!open) label = ProgressionUI.Gray(GearSystem.RarityName(r) + " (" + FactionRules.RankForRarity(r) + ")");
                Rect br = new Rect(x + (i % 3) * (bw3 + 10f), y + (i / 3) * 50f, bw3, 44f);
                if (UIKit.Button(br, label, _order.Rarity == r, 17))
                {
                    if (open) { _order.Rarity = r; FixOrder(); }
                    else Status(GearSystem.RarityName(r) + " gear needs Forgewrights " + FactionRules.RankForRarity(r) + ". Forge, salvage and do their jobs to raise it.", false);
                }
            }
            y += 108f;
            Label(x, y, w, "SET");
            y += 26f;
            bool setOk = GearSystem.SetAllowed(_order.Slot, _order.Rarity);
            float bw4 = (w - 30f) / 4f;
            // The Forge crafts the three classic sets; the Phase 6 build sets (GearSystem.SetNames 3 to 8) only drop.
            for (int i = -1; i < SetShort.Length; i++)
            {
                Rect br = new Rect(x + (i + 1) * (bw4 + 10f), y, bw4, 44f);
                bool enabled = i < 0 || setOk;
                if (ProgressionUI.Button(br, i < 0 ? "None" : SetShort[i], _order.Set == i, 16, enabled))
                {
                    if (enabled) _order.Set = i;
                    else Status("Sets come on Rare or better Visors, Shells, Gauntlets and Striders.", false);
                }
            }
            y += 58f;
            Label(x, y, w, "ELEMENT");
            y += 26f;
            bool elemOk = GearSystem.ElementAllowed(_order.Slot, _order.Rarity);
            bool elemReq = GearSystem.ElementRequired(_order.Slot);
            float bw5 = (w - 40f) / 5f;
            for (int i = 0; i <= (int)Element.Void; i++)
            {
                Element e = (Element)i;
                Rect br = new Rect(x + i * (bw5 + 10f), y, bw5, 44f);
                bool enabled = e == Element.None ? !elemReq : elemOk;
                string name = e == Element.None ? "None" : ProgressionUI.Colored(GearSystem.ElementName(e), GearSystem.ElementColor(e));
                if (!enabled) name = ProgressionUI.Gray(e == Element.None ? "None" : GearSystem.ElementName(e));
                if (UIKit.Button(br, name, _order.Element == e, 16))
                {
                    if (enabled) _order.Element = e;
                    else Status(e == Element.None ? "A Weapon Core always carries an element." : "Only Weapon Cores and Epic or better pieces carry an element.", false);
                }
            }
            y += 58f;
            if (_order.Rarity == Rarity.Mythic)
            {
                Label(x, y, w, "BOSS HEART");
                y += 26f;
                for (int i = 0; i < ForgeDb.Hearts.Length; i++)
                {
                    string h = ForgeDb.Hearts[i];
                    int have = Game.Materials != null ? Game.Materials.Count(h) : 0;
                    Rect br = new Rect(x + (i % 3) * (bw3 + 10f), y + (i / 3) * 50f, bw3, 44f);
                    string label = MaterialDb.NameOf(h) + " (" + have + ")";
                    if (UIKit.Button(br, have > 0 ? label : ProgressionUI.Gray(label), _order.Heart == h, 14)) _order.Heart = h;
                }
            }
            DrawGearPreview(right, c);
        }

        private void DrawGearPreview(Rect r, CraftingSystem c)
        {
            float x = r.x + 20f, w = r.width - 40f, y = r.y + 16f;
            Color rc = GearSystem.RarityColor(_order.Rarity);
            UIKit.Text(new Rect(x, y, w, 36f), GearSystem.RarityName(_order.Rarity) + " " + GearSystem.SlotName(_order.Slot), 30, rc, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            int affixes = 1 + Mathf.Min(4, (int)_order.Rarity) + (_order.Rarity == Rarity.Mythic ? 1 : 0);
            string about = affixes + " random stat" + (affixes == 1 ? "" : "s") + " roll when it is forged, at power " + (Game.Gear != null ? Game.Gear.CurrentPower + 1 : 1) + ".";
            if (_order.Element != Element.None) about += "\n" + ProgressionUI.Colored(GearSystem.ElementName(_order.Element), GearSystem.ElementColor(_order.Element)) + (_order.Slot == GearSlot.Core ? ": your melee hits carry it." : " element.");
            if (_order.Set >= 0) about += "\n" + GearSystem.SetNames[_order.Set] + " set. 2 pieces: " + GearSystem.SetBonus2[_order.Set] + ". 4 pieces: " + GearSystem.SetBonus4[_order.Set] + ".";
            UIKit.Text(new Rect(x, y, w, 110f), about, 17, new Color(0.88f, 0.88f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 116f;
            ForgeCost baseCost = ForgeMath.GearCost(_order);
            ForgeCost price = c.GearPrice(_order);
            y = DrawCost(x, y, w, price, baseCost, c.Wallet);
            string problem = c.GearProblem(_order, _station);
            if (problem != null) UIKit.Text(new Rect(x, y, w, 50f), problem, 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            Rect btn = new Rect(x, r.yMax - 70f, Mathf.Min(300f, w), 56f);
            if (ProgressionUI.Button(btn, "FORGE IT", false, 24, problem == null))
            {
                string msg;
                bool ok = c.ForgeGear(_order, _station, out msg);
                Status(msg, ok);
            }
        }

        // ------------------------------------------------------------------ recipes (infusions, brews, upgrades)

        private void DrawRecipes(Rect left, Rect right, CraftingSystem c, RecipeKind kind)
        {
            float x = left.x, y = left.y, w = left.width;
            RecipeDef selected = ForgeDb.Get(_recipe);
            if (selected != null && selected.Kind != kind) selected = null;
            foreach (RecipeDef r in ForgeDb.All)
            {
                if (r.Kind != kind) continue;
                if (selected == null) { selected = r; _recipe = r.Id; }
                bool known = c.Knows(r);
                bool here = ForgeMath.StationAllows(r, _station);
                string label = r.Name;
                if (kind == RecipeKind.Upgrade) label += "  " + c.UpgradeRank(r) + "/" + r.MaxRank;
                if (kind == RecipeKind.Brew && c.BuffLeft(r.Id) > 0f) label += "  " + CraftingSystem.Clock(c.BuffLeft(r.Id));
                if (kind == RecipeKind.Infusion) label = ProgressionUI.Colored(r.Name, GearSystem.ElementColor(r.Element));
                if (!known || !here) label = ProgressionUI.Gray(r.Name + (known ? " (other forge)" : " (locked)"));
                if (UIKit.Button(new Rect(x, y, w, 50f), label, selected == r, 19)) _recipe = r.Id;
                y += 58f;
            }
            if (kind == RecipeKind.Infusion)
            {
                GearItem core = c.EquippedCore;
                string coreText = core != null
                    ? "Equipped Weapon Core: " + ProgressionUI.Colored(core.Name, GearSystem.RarityColor(core.Rarity)) + (core.Element != Element.None ? ", " + ProgressionUI.Colored(GearSystem.ElementName(core.Element), GearSystem.ElementColor(core.Element)) : "")
                    : "No Weapon Core equipped. Forge one on the GEAR tab or equip one on the pause menu's GEAR tab.";
                UIKit.Text(new Rect(x, y + 8f, w, 70f), coreText, 17, new Color(0.88f, 0.88f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
            if (selected != null) DrawRecipeDetail(right, c, selected);
        }

        private void DrawRecipeDetail(Rect r, CraftingSystem c, RecipeDef rec)
        {
            float x = r.x + 20f, w = r.width - 40f, y = r.y + 16f;
            UIKit.Text(new Rect(x, y, w, 36f), rec.Name, 30, ProgressionUI.ForgeOrange, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            UIKit.Text(new Rect(x, y, w, 70f), rec.Text, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 74f;
            if (rec.Kind == RecipeKind.Upgrade)
            {
                int rank = c.UpgradeRank(rec);
                string ranks = "Rank " + rank + " of " + rec.MaxRank + ".";
                if (rank < rec.MaxRank)
                {
                    FactionDef f = FactionDb.Get(rec.RankFaction);
                    ranks += " Next rank needs " + (f != null ? f.Name : rec.RankFaction) + " " + rec.Steps[rank].Need + ".";
                }
                UIKit.Text(new Rect(x, y, w, 26f), ranks, 17, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft);
                y += 30f;
            }
            if (rec.Kind == RecipeKind.Brew)
            {
                float left = c.BuffLeft(rec.Id);
                UIKit.Text(new Rect(x, y, w, 26f), "Lasts " + CraftingSystem.Clock(rec.Seconds) + " of game time." + (left > 0f ? " Active: " + CraftingSystem.Clock(left) + " left (brewing again starts it over)." : ""), 17, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 30f;
            }
            ForgeCost basePrice = c.RecipeCost(rec);
            y = DrawCost(x, y, w, c.RecipePrice(rec), basePrice, c.Wallet);
            string problem = c.RecipeProblem(rec, _station);
            if (problem != null) UIKit.Text(new Rect(x, y, w, 50f), problem, 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            string verb = rec.Kind == RecipeKind.Infusion ? "INFUSE" : (rec.Kind == RecipeKind.Brew ? "BREW" : "FORGE");
            Rect btn = new Rect(x, r.yMax - 70f, Mathf.Min(300f, w), 56f);
            if (basePrice != null && ProgressionUI.Button(btn, verb, false, 24, problem == null))
            {
                string msg;
                bool ok = c.Craft(rec, _station, out msg);
                Status(msg, ok);
            }
        }

        // ------------------------------------------------------------------ salvage

        private void DrawSalvage(Rect left, Rect right, CraftingSystem c)
        {
            float x = left.x, y = left.y, w = left.width;
            _held.Clear();
            if (Game.Materials != null) _held.AddRange(Game.Materials.Held());
            if (_held.Count == 0)
            {
                UIKit.Text(new Rect(x, y, w, 120f), "Your satchel is empty. Creatures drop materials, and rifts leave shards behind when they close.", 19, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            const int perPage = 9;
            int pages = Mathf.Max(1, (_held.Count + perPage - 1) / perPage);
            _salvagePage = Mathf.Clamp(_salvagePage, 0, pages - 1);
            MaterialDef sel = null;
            int selCount = 0;
            for (int i = 0; i < _held.Count; i++)
            {
                if (_held[i].Key.Id == _salvage) { sel = _held[i].Key; selCount = _held[i].Value; }
            }
            if (sel == null) { sel = _held[_salvagePage * perPage].Key; selCount = _held[_salvagePage * perPage].Value; _salvage = sel.Id; }
            for (int i = _salvagePage * perPage; i < Mathf.Min(_held.Count, (_salvagePage + 1) * perPage); i++)
            {
                MaterialDef m = _held[i].Key;
                string label = ProgressionUI.Colored(m.Name, m.RarityColor) + "  x" + _held[i].Value + "   <color=#7dff8a>" + MathUtil.Money(c.SalvagePrice(m.Id)) + "</color>";
                if (UIKit.Button(new Rect(x, y, w, 46f), label, m == sel, 17)) _salvage = m.Id;
                y += 52f;
            }
            if (pages > 1)
            {
                float by = left.y + perPage * 52f + 4f;
                if (UIKit.Button(new Rect(x, by, w * 0.48f, 42f), "Prev page", false, 18)) _salvagePage = (_salvagePage + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + w * 0.52f, by, w * 0.48f, 42f), "Next page", false, 18)) _salvagePage = (_salvagePage + 1) % pages;
            }
            // Detail.
            float rx = right.x + 20f, rw = right.width - 40f, ry = right.y + 16f;
            UIKit.Text(new Rect(rx, ry, rw, 36f), sel.Name, 30, sel.RarityColor, TextAnchor.UpperLeft, FontStyle.Bold);
            ry += 40f;
            UIKit.Text(new Rect(rx, ry, rw, 26f), sel.RarityName + (sel.Element != Element.None ? ", " + GearSystem.ElementName(sel.Element) : "") + ". You carry " + selCount + ".", 17, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft);
            ry += 30f;
            UIKit.Text(new Rect(rx, ry, rw, 70f), sel.Description, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Italic, true, true);
            ry += 76f;
            int price = c.SalvagePrice(sel.Id);
            float bonus = SkillMods.SalvageBonus + FactionRules.SalvageRankBonus(c.ForgeRank);
            UIKit.Text(new Rect(rx, ry, rw, 50f), "Dante pays " + MathUtil.Money(price) + " each" + (bonus > 0.001f ? " (" + ProgressionUI.Pct(bonus) + " over the base " + MathUtil.Money(sel.Value) + ")" : "") + ". Every $500 sold earns a little Forgewrights reputation.", 17, new Color(0.85f, 0.95f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float by2 = right.yMax - 70f;
            float bw = Mathf.Min(220f, (rw - 20f) / 3f);
            if (UIKit.Button(new Rect(rx, by2, bw, 56f), "Sell 1", false, 20)) Sell(c, sel.Id, 1);
            if (UIKit.Button(new Rect(rx + bw + 10f, by2, bw, 56f), "Sell 5", false, 20)) Sell(c, sel.Id, 5);
            if (UIKit.Button(new Rect(rx + (bw + 10f) * 2f, by2, bw, 56f), "Sell all (" + MathUtil.Money(price * selCount) + ")", false, 16)) Sell(c, sel.Id, selCount);
        }

        private void Sell(CraftingSystem c, string id, int count)
        {
            string msg;
            bool ok = c.Sell(id, count, out msg);
            if (!string.IsNullOrEmpty(msg)) Status(msg, ok);
        }

        // ------------------------------------------------------------------ the Frost Wardens' shop

        private void DrawWardens(Rect left, Rect right, CraftingSystem c)
        {
            float x = left.x, y = left.y, w = left.width;
            FactionRank rank = c.WardenRank;
            if (!c.WardenShopOpen)
            {
                string text = "The Frost Wardens sell their cold-weather gear to friends. You are " + FactionRules.RankName(rank) + " (" + (Game.Factions != null ? Game.Factions.Rep(CraftingSystem.WardensId) : 0) + "); Friendly starts at 20.\n\n" +
                              "Hunt Frostline creatures, forge here at the outpost and craft Frost Linings to earn their trust.";
                UIKit.Text(new Rect(x, y, left.width + right.width, 160f), text, 19, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            Rarity rar = c.WardenRarity;
            int count = CraftingSystem.WardenSlots.Length;
            for (int i = 0; i <= count; i++)
            {
                string label = i < count
                    ? ProgressionUI.Colored("Warden " + GearSystem.SlotName(CraftingSystem.WardenSlots[i]), GearSystem.RarityColor(rar)) + "   " + MathUtil.Money(c.WardenPrice(CraftingSystem.WardenSlots[i]))
                    : "Frost Ward Tea   " + MathUtil.Money(c.WardenTeaCost);
                if (UIKit.Button(new Rect(x, y, w, 50f), label, _warden == i, 19)) _warden = i;
                y += 58f;
            }
            float rx = right.x + 20f, rw = right.width - 40f, ry = right.y + 16f;
            Rect btn = new Rect(rx, right.yMax - 70f, Mathf.Min(300f, rw), 56f);
            if (_warden >= count)
            {
                UIKit.Text(new Rect(rx, ry, rw, 36f), "Frost Ward Tea", 30, new Color(0.6f, 0.95f, 1f), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(rx, ry + 44f, rw, 80f), "The Wardens' own brew, no materials needed. For 8 minutes: take 25% less damage deep in the Frostline.", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (UIKit.Button(btn, "BUY " + MathUtil.Money(c.WardenTeaCost), false, 22))
                {
                    string msg;
                    bool ok = c.BuyWardenTea(out msg);
                    Status(msg, ok);
                }
                return;
            }
            GearSlot slot = CraftingSystem.WardenSlots[_warden];
            UIKit.Text(new Rect(rx, ry, rw, 36f), GearSystem.RarityName(rar) + " Warden " + GearSystem.SlotName(slot), 30, GearSystem.RarityColor(rar), TextAnchor.UpperLeft, FontStyle.Bold);
            ry += 40f;
            string about = "Cold-weather gear from the Wardens' storehouse: a " + GearSystem.SetNames[1] + " set piece" + (GearSystem.ElementAllowed(slot, rar) ? " carrying Frost" : "") + ".\n" +
                           "2 pieces: " + GearSystem.SetBonus2[1] + ". 4 pieces: " + GearSystem.SetBonus4[1] + ".\n" +
                           "Better pieces come with better standing (Rare at Friendly, Epic at Trusted, Legendary at Family; Frost from Epic up). Stats roll when you buy it.";
            UIKit.Text(new Rect(rx, ry, rw, 150f), about, 17, new Color(0.88f, 0.9f, 0.95f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            string problem = c.WardenProblem(slot);
            if (problem != null) UIKit.Text(new Rect(rx, ry + 156f, rw, 50f), problem, 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            if (ProgressionUI.Button(btn, "BUY " + MathUtil.Money(c.WardenPrice(slot)), false, 22, problem == null))
            {
                string msg;
                bool ok = c.BuyWarden(slot, out msg);
                Status(msg, ok);
            }
        }

        // ------------------------------------------------------------------ HUD

        private static string _brewLine = "";
        private static int _brewKey;
        private static bool _brewValid;

        /// <summary>Active brews on the HUD, under the Shard Tonic pips: "Hunter's Oil 4:31   Warding Salve 0:59".</summary>
        public static void DrawBrews(float W, float H)
        {
            CraftingSystem c = Game.Crafting;
            if (c == null || c.ActiveBuffCount == 0) return;
            // The line is rebuilt only when a brew starts, ends or its shown second ticks over.
            int key = c.BuffClockKey;
            if (!_brewValid || key != _brewKey)
            {
                _brewValid = true;
                _brewKey = key;
                _brewLine = BuildBrewLine(c);
            }
            if (_brewLine.Length == 0) return;
            UIKit.Text(new Rect(40f, 138f, 900f, 24f), _brewLine, 16, new Color(0.55f, 1f, 0.85f), TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private static string BuildBrewLine(CraftingSystem c)
        {
            string line = "";
            foreach (KeyValuePair<string, float> kv in c.ActiveBuffs)
            {
                RecipeDef r = ForgeDb.Get(kv.Key);
                if (r == null || kv.Value <= 0f) continue;
                line += (line.Length > 0 ? "     " : "") + r.Name + " " + CraftingSystem.Clock(kv.Value);
            }
            return line;
        }

        /// <summary>Clears the cached brew line (Enter Play Mode without domain reload).</summary>
        public static void ResetStatics()
        {
            _brewLine = "";
            _brewKey = 0;
            _brewValid = false;
        }
    }
}
