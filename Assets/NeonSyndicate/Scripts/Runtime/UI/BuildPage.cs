using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The BUILD pause page (K, Order 4): one overview of the build that reuses the existing inventories and commands.
    /// Tabs: OVERVIEW (focus and the four loot slots, gear per slot with compare against the same slot, ward, companion,
    /// chassis, a stats preview from the same calculations the game uses, set and element lines, the next affordable
    /// upgrade and a schematic of the visible changes), SUGGESTIONS (the three reversible build suggestions: preview
    /// anywhere, apply and revert at a fitting station), WARD (aspects, the Fracture Lattice, cosmetics, key items),
    /// REWARDS (pending claims with deterministic offers and compare, history) and LOADOUTS (two saved setups).
    /// Every control is a UIKit button, so mouse, keyboard and controller all work.
    /// </summary>
    public static class BuildPage
    {
        public const int TabOverview = 0, TabSuggestions = 1, TabWard = 2, TabRewards = 3, TabLoadouts = 4;
        private static readonly string[] TabNames = { "OVERVIEW", "SUGGESTIONS", "WARD", "REWARDS", "LOADOUTS" };

        private static int _tab;
        private static int _slot = -1;
        private static string _previewPreset = "";
        private static List<PresetLine04E> _lines = new List<PresetLine04E>();
        private static bool _linesAreResult;
        private static string _message = "";
        private static string _rewardToken = "";
        private static int _offer = -1;
        private static int _gearPage;
        private static readonly List<GearItem> _sameSlot = new List<GearItem>();
        private static readonly List<RewardEntrySave> _pending = new List<RewardEntrySave>();

        private static readonly Color Dim = new Color(0.72f, 0.74f, 0.78f);
        private static readonly Color Good = new Color(0.43f, 1f, 0.54f);
        private static readonly Color Bad = new Color(1f, 0.48f, 0.42f);

        public static void ResetStatics()
        {
            _tab = 0;
            _slot = -1;
            _previewPreset = "";
            _lines = new List<PresetLine04E>();
            _linesAreResult = false;
            _message = "";
            _rewardToken = "";
            _offer = -1;
            _gearPage = 0;
            _sameSlot.Clear();
            _pending.Clear();
        }

        /// <summary>Selects the tab the page opens on (fitting stations open SUGGESTIONS, ledgers REWARDS).</summary>
        public static void ShowTab(int tab)
        {
            _tab = Mathf.Clamp(tab, 0, TabNames.Length - 1);
            _message = "";
        }

        public static void Draw(Rect r)
        {
            BuildSystem b = Game.Builds;
            if (b == null || Game.Player == null)
            {
                UIKit.Text(new Rect(r.x + 40f, r.y + 40f, r.width - 80f, 40f), "Builds are not available.", 24, Color.white, TextAnchor.UpperLeft);
                return;
            }
            float pad = 22f;
            float x = r.x + pad, y = r.y + 12f;
            float tw = Mathf.Min(220f, (r.width - pad * 2f - 40f) / TabNames.Length);
            for (int i = 0; i < TabNames.Length; i++)
            {
                string label = TabNames[i];
                if (i == TabRewards && b.Ledger.PendingCount > 0) label += " (" + b.Ledger.PendingCount + ")";
                if (UIKit.Button(new Rect(x + i * (tw + 10f), y, tw, 40f), label, _tab == i, 17))
                {
                    _tab = i;
                    _message = "";
                }
            }
            string where = b.AtFitting ? "AT A FITTING STATION: suggestions can be applied and reverted here." : "Suggestions are applied at a fitting station (Cinder Dispatch worktable, Lantern Refuge bench).";
            UIKit.Text(new Rect(x + TabNames.Length * (tw + 10f) + 10f, y, r.xMax - pad - (x + TabNames.Length * (tw + 10f) + 10f), 40f), where, 14, b.AtFitting ? UIKit.Gold : Dim, TextAnchor.MiddleRight, FontStyle.Bold, true, true);
            Rect body = new Rect(r.x + pad, y + 54f, r.width - pad * 2f, r.yMax - y - 54f - pad);
            switch (_tab)
            {
                case TabSuggestions: DrawSuggestions(body, b); break;
                case TabWard: DrawWard(body, b); break;
                case TabRewards: DrawRewards(body, b); break;
                case TabLoadouts: DrawLoadouts(body, b); break;
                default: DrawOverview(body, b); break;
            }
            if (!string.IsNullOrEmpty(_message))
                UIKit.Text(new Rect(body.x, r.yMax - 34f, body.width, 30f), _message, 17, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true, true);
        }

        // ================================================================== OVERVIEW

        private static void DrawOverview(Rect r, BuildSystem b)
        {
            float colW = (r.width - 40f) / 3f;
            Rect left = new Rect(r.x, r.y, colW, r.height);
            Rect mid = new Rect(left.xMax + 20f, r.y, colW, r.height);
            Rect right = new Rect(mid.xMax + 20f, r.y, colW, r.height);
            float y = DrawFocus(left, b);
            DrawGearSlots(new Rect(left.x, y, left.width, left.yMax - y));
            if (_slot >= 0) DrawCompare(mid);
            else DrawSystems(mid, b);
            DrawStats(right, b);
        }

        private static float DrawFocus(Rect r, BuildSystem b)
        {
            float y = r.y;
            string preset = b.Preset.Length > 0 ? BuildPresetRules04E.Get(b.Preset).Title : "none chosen yet";
            UIKit.Text(new Rect(r.x, y, r.width, 26f), "SUGGESTION: " + preset.ToUpperInvariant(), 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "CRYSTAL FOCUS AND LOOT SLOTS", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            LootArmory la = Game.Loot;
            int inHand = la != null ? la.CurrentSlot() : -1;
            int fitted = b.FractureFocusUid;
            bool anyFocus = false;
            for (int i = 0; la != null && i < la.Slots.Length; i++)
            {
                LootGun g = la.Slots[i];
                string line = (i + 1) + ": ";
                if (g == null) line += "<color=#888888>empty</color>";
                else
                {
                    bool focus = CrystalFocusDb.BehaviorOfUnique(g.Unique) != CrystalBehavior.None;
                    anyFocus |= focus;
                    line += "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(g.Rarity)) + ">" + g.Name + "</color>" + (focus ? "  <size=12>FOCUS</size>" : "") +
                            (g.Uid == fitted && fitted > 0 ? "  <size=12>FRACTURE</size>" : "") + (i == inHand ? "  <size=12>[in hand]</size>" : "");
                }
                UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 21f), line, 15, Color.white, TextAnchor.UpperLeft);
                y += 21f;
            }
            if (!anyFocus)
            {
                bool owned = b.OwnedFoci().Count > 0;
                UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 40f), owned ? "A crystal focus waits in your backpack: equip it in ARSENAL." : "No crystal focus yet: claim Shardwake at the resonance case beside the Veyra Lofts arrival (A on the map).",
                    13, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 38f;
            }
            return y + 8f;
        }

        private static void DrawGearSlots(Rect r)
        {
            GearSystem g = Game.Gear;
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "GEAR  <size=13>(pick a slot to compare)</size>", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            if (g == null) return;
            float h = Mathf.Min(44f, (r.yMax - y) / GearSystem.SlotCount - 4f);
            for (int s = 0; s < GearSystem.SlotCount; s++)
            {
                GearItem it = g.Equipped[s];
                string label = GearSystem.SlotName((GearSlot)s) + ": " + (it != null ? "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(it.Rarity)) + ">" + it.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : "") +
                               "</color> <size=12>(" + GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity) + ")</size>" : "<color=#888888>empty</color>");
                if (UIKit.Button(new Rect(r.x, y, r.width, h), label, _slot == s, 15))
                {
                    _slot = _slot == s ? -1 : s;
                    _gearPage = 0;
                }
                y += h + 4f;
            }
        }

        private static void DrawCompare(Rect r)
        {
            GearSystem g = Game.Gear;
            if (g == null) return;
            GearSlot slot = (GearSlot)_slot;
            GearItem cur = g.Equipped[_slot];
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, r.width, 24f), GearSystem.SlotName(slot).ToUpperInvariant() + ": COMPARE IN THIS SLOT", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(r.xMax - 90f, y - 2f, 90f, 30f), "Back", false, 14)) _slot = -1;
            y += 28f;
            UIKit.Text(new Rect(r.x, y, r.width, 60f), cur != null ? "Worn: " + cur.Name + " (score " + GearScoreRules.GearScore(cur.Power, cur.Upgrade, cur.Rarity) + ")\n" + Affixes(cur) : "Nothing worn in this slot.", 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 64f;
            _sameSlot.Clear();
            foreach (GearItem it in g.Items) if (it.Slot == slot && it != cur) _sameSlot.Add(it);
            _sameSlot.Sort((a, c) => GearScoreRules.GearScore(c.Power, c.Upgrade, c.Rarity).CompareTo(GearScoreRules.GearScore(a.Power, a.Upgrade, a.Rarity)));
            if (_sameSlot.Count == 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 40f), "No other " + GearSystem.SlotName(slot) + " owned.", 14, Dim, TextAnchor.UpperLeft);
                return;
            }
            const int per = 4;
            int pages = Mathf.Max(1, (_sameSlot.Count + per - 1) / per);
            _gearPage = Mathf.Clamp(_gearPage, 0, pages - 1);
            for (int i = _gearPage * per; i < Mathf.Min(_sameSlot.Count, (_gearPage + 1) * per); i++)
            {
                GearItem it = _sameSlot[i];
                int sc = GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity);
                int cs = cur != null ? GearScoreRules.GearScore(cur.Power, cur.Upgrade, cur.Rarity) : 0;
                string head = "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(it.Rarity)) + ">" + it.Name + "</color>  score " + sc + (cur != null ? (sc >= cs ? " <color=#6dff8a>(+" + (sc - cs) + ")</color>" : " <color=#ff7a6a>(" + (sc - cs) + ")</color>") : "");
                UIKit.Text(new Rect(r.x, y, r.width - 96f, 20f), head, 14, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(r.xMax - 90f, y, 90f, 28f), "Equip", false, 14))
                {
                    g.Equip(it);
                    _message = it.Name + " equipped in " + GearSystem.SlotName(slot) + ".";
                }
                y += 22f;
                string delta = Deltas(it, cur);
                UIKit.Text(new Rect(r.x + 8f, y, r.width - 104f, 54f), delta, 12, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 58f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(r.x, y, r.width * 0.48f, 32f), "Prev", false, 14)) _gearPage = (_gearPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(r.x + r.width * 0.52f, y, r.width * 0.48f, 32f), "Next", false, 14)) _gearPage = (_gearPage + 1) % pages;
            }
        }

        private static string Affixes(GearItem it)
        {
            List<string> l = new List<string>();
            foreach (Affix a in it.Affixes) l.Add(GearSystem.AffixText(a.Type, a.Value * it.UpgradeScale));
            return string.Join(", ", l.ToArray());
        }

        /// <summary>Per-affix differences against the worn piece, plus the combined movement and parry result from the same rules the game uses.</summary>
        private static string Deltas(GearItem it, GearItem cur)
        {
            List<string> l = new List<string>();
            for (int t = 0; t < (int)AffixType.Count; t++)
            {
                AffixType at = (AffixType)t;
                float a = it.Get(at), c = cur != null ? cur.Get(at) : 0f;
                if (Mathf.Abs(a - c) < 0.0005f) continue;
                string txt = GearSystem.AffixText(at, Mathf.Abs(a - c));
                l.Add((a > c ? "<color=#6dff8a>+ " : "<color=#ff7a6a>- ") + txt.TrimStart('+') + "</color>");
            }
            GearSystem g = Game.Gear;
            if (g != null)
            {
                float mv = g.Total(AffixType.MoveSpeed) - (cur != null ? cur.Get(AffixType.MoveSpeed) : 0f) + it.Get(AffixType.MoveSpeed);
                float nowMv = BuildStatRules04E.MoveSpeedScale(g.Total(AffixType.MoveSpeed), false), newMv = BuildStatRules04E.MoveSpeedScale(mv, false);
                if (Mathf.Abs(newMv - nowMv) > 0.0005f) l.Add("move x" + nowMv.ToString("0.00") + " to x" + newMv.ToString("0.00"));
                bool frost = Game.Builds != null && Game.Builds.FrostglassActive;
                float pr = g.Total(AffixType.ParryWindow) - (cur != null ? cur.Get(AffixType.ParryWindow) : 0f) + it.Get(AffixType.ParryWindow);
                float nowP = BuildStatRules04E.ParryBonus(g.Total(AffixType.ParryWindow), frost), newP = BuildStatRules04E.ParryBonus(pr, frost);
                if (Mathf.Abs(newP - nowP) > 0.0005f) l.Add("parry +" + Mathf.RoundToInt(nowP * 1000f) + " to +" + Mathf.RoundToInt(newP * 1000f) + " ms (cap " + Mathf.RoundToInt(BuildStatRules04E.MaxParryBonus * 1000f) + ")");
            }
            if (it.Set >= 0) l.Add("set: " + GearSystem.SetNames[it.Set]);
            if (it.Element != Element.None) l.Add("element: " + GearSystem.ElementName(it.Element));
            return l.Count > 0 ? string.Join("   ", l.ToArray()) : "Same numbers as the worn piece.";
        }

        private static void DrawSystems(Rect r, BuildSystem b)
        {
            float y = r.y;
            GadgetSystem gs = Game.Gadgets;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "WARD AND GADGETS", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            string ward;
            if (gs == null) ward = "Gadgets are not available.";
            else
            {
                WardState04E st = gs.WardStateNow;
                ward = "Lattice Shield (Buckler): " + WardRules04E.StateLabel(st) +
                       (st == WardState04E.Locked ? ". Claim the starter ward (A Knock Beneath Home), or reach level " + GadgetRules.UnlockLevel(WardRules04E.BucklerId) + "." :
                        ". Capacity " + Mathf.RoundToInt(gs.WardCapacityMax) + (gs.WardIsStarterVariant ? " (starter: full at level " + GadgetRules.UnlockLevel(WardRules04E.BucklerId) + ")" : "") +
                        (gs.WardSlot >= 0 ? ", gadget slot " + (gs.WardSlot + 1) : ", not in a gadget slot (GADGETS tab)") + ".") +
                       "\nAspect: " + WardRules04E.AspectTitle(b.ActiveAspect) + (b.UnlockedAspects.Count > 0 ? " (" + b.UnlockedAspects.Count + " of 4 unlocked)" : " (none unlocked yet)");
                for (int s = 0; s < gs.Slots.Length; s++)
                {
                    GadgetSpec spec = gs.SpecIn(s);
                    ward += "\nGadget slot " + (s + 1) + ": " + (spec != null ? spec.FullName : "empty");
                }
            }
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 120f), ward, 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 124f;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "COMPANION", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            string comp = "No companion yet: Bondkeeper's Refuge helps rescued creatures.";
            CompanionRecord c = Game.Bonds != null ? Game.Bonds.Active : null;
            if (c != null)
                comp = c.Name + ", " + BondIds.Title(c.Archetype) + ": " + BondIds.RankNames[Mathf.Clamp(c.Rank, 0, BondIds.RankNames.Length - 1)] + (c.Evolution > 0 ? ", evolution " + c.Evolution : "") +
                       ". Assist: " + AssistLine(c.Archetype) + ".";
            else if (Game.Bonds != null && Game.Bonds.Roster.Count > 0) comp = Game.Bonds.Roster.Count + " bonded, none active. Pick one at a care station or on the BONDS tab.";
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 60f), comp, 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 62f;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "CHASSIS AND FUSION", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            string ch = "The personal garage is not available.";
            PersonalGarage pg = Game.Garage;
            if (pg != null)
            {
                ChassisRecord rec = pg.Personal;
                ch = rec != null ? "Personal chassis: " + rec.Class + " (uid " + rec.Uid + ")" + (string.IsNullOrEmpty(rec.Trim) ? "" : ", trim " + rec.Trim) : "No personal chassis registered yet (Jax's bay, or Phone > Bring my bike).";
                ch += "\nFusion: " + (pg.FusionUnlocked ? "unlocked" : "locked (Two Hearts, One Chassis)");
                List<string> forms = new List<string>();
                foreach (string f in FusionIds.Forms) if (pg.FormUnlocked(f)) forms.Add(FusionIds.Title(f));
                if (forms.Count > 0) ch += ". Forms: " + string.Join(", ", forms.ToArray());
            }
            if (b.HasKeyItem(RewardCatalog04E.KeyCoupler)) ch += "\nKey item: crystal coupler (Jax fits it at the bay).";
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 80f), ch, 14, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        private static string AssistLine(string archetype)
        {
            switch (archetype)
            {
                case BondIds.QuartzGuide: return "reveals a nearby route; marked-target burst";
                case BondIds.AshWarden: return "braces a mechanism; directional protection";
                case BondIds.StormSkimmer: return "bridges a relay; a short disruption";
                default: return "none";
            }
        }

        private static void DrawStats(Rect r, BuildSystem b)
        {
            PlayerController p = Game.Player;
            GearSystem g = Game.Gear;
            GadgetSystem gs = Game.Gadgets;
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "WHAT YOUR BUILD DOES NOW", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            if (g == null || p == null) return;
            bool coolant = Game.Crafting != null && Game.Crafting.BuffLeft(RewardCatalog04E.RecipeCoolant) > 0f;
            bool mantle = b.EmberMantleActive && gs != null && gs.HeatBufferValue > 0f;
            string s =
                "Health " + Mathf.RoundToInt(p.Health.MaxHealth) + "   damage taken x" + (g.ArmorMultiplier * CombatDirector.PlayerDamageTaken).ToString("0.00") + "\n" +
                "Bullets x" + g.DamageTakenMultiplier(DamageType.Bullet).ToString("0.00") + "   melee x" + g.DamageTakenMultiplier(DamageType.Melee).ToString("0.00") + "   blasts x" + g.DamageTakenMultiplier(DamageType.Explosion).ToString("0.00") +
                "   heat x" + BuildStatRules04E.HeatTakenWithMantle(coolant, mantle).ToString("0.00") + (coolant ? " (Coolant Draught)" : "") + "\n" +
                "Movement x" + g.MoveSpeedMultiplier.ToString("0.00") + "   parry +" + Mathf.RoundToInt(g.ParryBonus * 1000f) + " ms   dodge +" + Mathf.RoundToInt(g.DodgeBonus * 1000f) + " ms\n" +
                "Guns x" + (g.GunMultiplier * (Game.Progress != null ? Game.Progress.GunMultiplier : 1f)).ToString("0.00") + "   powers x" + g.PowerMultiplier.ToString("0.00") + "   melee x" + g.MeleeMultiplierPreview.ToString("0.00");
            if (gs != null && gs.IsUnlocked(WardRules04E.BucklerId)) s += "\nWard capacity " + Mathf.RoundToInt(gs.WardCapacityMax) + (b.KilnCrown ? " (Kiln Crown)" : "");
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 116f), s, 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 116f;
            List<string> lines = new List<string>();
            Element e = g.WeaponElement;
            if (e != Element.None) lines.Add("Element " + GearSystem.ElementName(e) + ": " + ElementLine(e));
            if (b.ActiveAspect.Length > 0) lines.Add(WardRules04E.AspectTitle(b.ActiveAspect) + ": " + WardRules04E.AspectLine(b.ActiveAspect));
            List<int> pieces = new List<int>();
            for (int i = 0; i < GearSystem.SetNames.Length; i++) pieces.Add(g.SetPieces(i));
            lines.AddRange(GearBuildRules.ActiveLines(pieces, GearSystem.SetNames, GearSystem.SetBonus2, GearSystem.SetBonus3, GearSystem.SetBonus4));
            if (b.FractureFocusUid > 0) lines.Add(FractureRules04E.Line(Rarity.Rare));
            if (lines.Count == 0) lines.Add("No element, aspect or set bonus is active yet.");
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 120f), "<size=12>" + string.Join("\n", lines.ToArray()) + "</size>", 12, new Color(0.62f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 124f;
            UIKit.Text(new Rect(r.x, y, r.width, 22f), "NEXT UPGRADE", 15, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            UIKit.Text(new Rect(r.x + 6f, y, r.width - 6f, 40f), NextUpgrade(g), 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 44f;
            DrawSchematic(new Rect(r.x, y, r.width, Mathf.Max(0f, r.yMax - y)), b, g);
        }

        private static string ElementLine(Element e)
        {
            switch (e)
            {
                case Element.Volt: return "melee hits can arc lightning into another enemy.";
                case Element.Ember: return "melee hits can set enemies burning.";
                case Element.Frost: return "melee hits can shatter an enemy's poise.";
                case Element.Void: return "melee hits can drain health back to you.";
                default: return "";
            }
        }

        /// <summary>The cheapest shrine upgrade among worn pieces, with the real Lumen cost and why it is blocked.</summary>
        private static string NextUpgrade(GearSystem g)
        {
            GearItem best = null;
            int cost = int.MaxValue;
            for (int s = 0; s < GearSystem.SlotCount; s++)
            {
                GearItem it = g.Equipped[s];
                if (it == null || it.Upgrade >= GearSystem.MaxUpgrade) continue;
                int c = GearSystem.UpgradeCost(it);
                if (c < cost)
                {
                    cost = c;
                    best = it;
                }
            }
            if (best == null) return "Every worn piece is fully upgraded (or nothing is worn).";
            int lumen = Game.Progress != null ? Game.Progress.Lumen : 0;
            string blocked = lumen < cost ? "Blocked: " + (cost - lumen).ToString("N0") + " Lumen short." : "Affordable: upgrade it at a Resonance Shrine (rest, then Gear).";
            return best.Name + " +" + best.Upgrade + " to +" + (best.Upgrade + 1) + ": " + cost.ToString("N0") + " Lumen (you have " + lumen.ToString("N0") + "). " + blocked;
        }

        /// <summary>A small schematic of the visible changes: armour pieces by rarity colour, the core element and the ward silhouette.</summary>
        private static void DrawSchematic(Rect r, BuildSystem b, GearSystem g)
        {
            if (r.height < 120f) return;
            float cx = r.x + 70f, top = r.y + 8f;
            UIKit.Text(new Rect(r.x, top - 4f, r.width, 18f), "SCHEMATIC (what changes on you)", 12, Dim, TextAnchor.UpperLeft, FontStyle.Bold);
            top += 16f;
            Color Piece(GearSlot s) { GearItem it = g.Equipped[(int)s]; return it != null ? GearSystem.RarityColor(it.Rarity) : new Color(0.25f, 0.25f, 0.28f); }
            UIKit.Tex(new Rect(cx - 14f, top, 28f, 28f), UIKit.Circle, Piece(GearSlot.Visor));
            UIKit.Rect(new Rect(cx - 22f, top + 32f, 44f, 50f), Piece(GearSlot.Shell));
            UIKit.Rect(new Rect(cx - 36f, top + 34f, 12f, 40f), Piece(GearSlot.Gauntlets));
            UIKit.Rect(new Rect(cx + 24f, top + 34f, 12f, 40f), Piece(GearSlot.Gauntlets));
            UIKit.Rect(new Rect(cx - 18f, top + 86f, 14f, 36f), Piece(GearSlot.Striders));
            UIKit.Rect(new Rect(cx + 4f, top + 86f, 14f, 36f), Piece(GearSlot.Striders));
            GearItem core = g.Equipped[(int)GearSlot.Core];
            UIKit.Tex(new Rect(cx - 6f, top + 50f, 12f, 12f), UIKit.Circle, core != null && core.Element != Element.None ? GearSystem.ElementColor(core.Element) : Piece(GearSlot.Core));
            GadgetSystem gs = Game.Gadgets;
            if (gs == null || !gs.IsUnlocked(WardRules04E.BucklerId)) return;
            Color wc = GadgetSystem.WardColor(b.ActiveAspect);
            float wx = cx + 70f, wy = top + 22f;
            switch (b.ActiveAspect)
            {
                case WardRules04E.VoidScreen:
                    UIKit.Tex(new Rect(wx, wy, 70f, 70f), UIKit.Ring, wc);
                    UIKit.Tex(new Rect(wx + 14f, wy + 14f, 42f, 42f), UIKit.Circle, new Color(0.08f, 0.03f, 0.14f));
                    break;
                case WardRules04E.Frostglass:
                    UIKit.Frame(new Rect(wx, wy, 52f, 76f), wc, 3f);
                    UIKit.Tex(new Rect(wx + 14f, wy + 26f, 24f, 24f), UIKit.Star, wc);
                    break;
                case WardRules04E.VoltLattice:
                    UIKit.Frame(new Rect(wx, wy, 52f, 76f), wc, 3f);
                    for (int i = 1; i < 3; i++) UIKit.Rect(new Rect(wx, wy + i * 25f, 52f, 2f), wc);
                    for (int i = 1; i < 3; i++) UIKit.Rect(new Rect(wx + i * 17f, wy, 2f, 76f), wc);
                    break;
                case WardRules04E.EmberMantle:
                    UIKit.Frame(new Rect(wx, wy, 52f, 76f), wc, 3f);
                    for (int i = 0; i < 3; i++) UIKit.Rect(new Rect(wx + 4f, wy + 34f + i * 13f, 44f, 9f), new Color(0.4f, 0.17f, 0.08f));
                    break;
                default:
                    UIKit.Frame(new Rect(wx, wy, 52f, 76f), wc, 3f);
                    UIKit.Rect(new Rect(wx + 25f, wy, 2f, 76f), wc);
                    break;
            }
            if (b.KilnCrown) for (int i = 0; i < 5; i++) UIKit.Rect(new Rect(wx + 4f + i * 10f, wy - 10f, 4f, 9f), new Color(1f, 0.62f, 0.22f));
            UIKit.Text(new Rect(wx - 10f, wy + 80f, 140f, 18f), "ward: " + WardRules04E.AspectTitle(b.ActiveAspect), 11, wc, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        // ================================================================== SUGGESTIONS

        private static void DrawSuggestions(Rect r, BuildSystem b)
        {
            float colW = (r.width - 40f) / 3f;
            float cardH = Mathf.Min(300f, r.height * 0.5f);
            PresetDef04E[] all = BuildPresetRules04E.All;
            for (int i = 0; i < all.Length; i++)
            {
                PresetDef04E p = all[i];
                Rect c = new Rect(r.x + i * (colW + 20f), r.y, colW, cardH);
                bool chosen = b.Preset == p.Id;
                UIKit.Rect(c, chosen ? new Color(p.Color.r * 0.25f, p.Color.g * 0.25f, p.Color.b * 0.25f, 0.85f) : UIKit.PanelLight);
                UIKit.Rect(new Rect(c.x, c.y, c.width, 4f), p.Color);
                UIKit.Text(new Rect(c.x + 12f, c.y + 10f, c.width - 24f, 30f), p.Title.ToUpperInvariant() + (chosen ? "  <size=13>(current)</size>" : ""), 22, p.Color, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(c.x + 12f, c.y + 44f, c.width - 24f, 64f), p.Tools, 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                UIKit.Text(new Rect(c.x + 12f, c.y + 108f, c.width - 24f, 80f), p.Tradeoff, 12, Dim, TextAnchor.UpperLeft, FontStyle.Italic, true, true);
                UIKit.Text(new Rect(c.x + 12f, c.y + 188f, c.width - 24f, 22f), b.KitClaimed(p.Id) ? "Starter kit: already given (once per save)." : "Starter kit: waiting (given once, the first time).", 12, b.KitClaimed(p.Id) ? Dim : UIKit.Gold, TextAnchor.UpperLeft);
                float by = c.yMax - 50f;
                float bw = (c.width - 36f) * 0.5f;
                if (UIKit.Button(new Rect(c.x + 12f, by, bw, 40f), "Preview", _previewPreset == p.Id && !_linesAreResult, 16))
                {
                    _previewPreset = p.Id;
                    _lines = b.PreviewPreset(p.Id);
                    _linesAreResult = false;
                    _message = "";
                }
                bool can = b.AtFitting;
                if (UIKit.Button(new Rect(c.x + 24f + bw, by, bw, 40f), can ? "Apply" : "Apply at a station", false, 15))
                {
                    if (!can) _message = "Apply suggestions at a fitting station: the Cinder Dispatch worktable or the Lantern Refuge fitting bench. Preview works anywhere.";
                    else
                    {
                        List<PresetLine04E> res;
                        string msg;
                        if (b.ApplyPreset(p.Id, out res, out msg))
                        {
                            _lines = res;
                            _linesAreResult = true;
                            _previewPreset = p.Id;
                        }
                        _message = msg;
                    }
                }
            }
            float y = r.y + cardH + 14f;
            if (b.AtFitting)
            {
                if (UIKit.Button(new Rect(r.x, y, 300f, 36f), b.CanRevert ? "Revert to my previous setup (free)" : "Nothing to revert", false, 15) && b.CanRevert)
                {
                    string msg;
                    b.RevertPreset(out msg);
                    _message = msg;
                    _lines.Clear();
                }
                y += 44f;
            }
            if (_lines.Count == 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 60f), "Pick Preview to see exactly what a suggestion would change. Suggestions only move items you own: they never duplicate an item and never swap a piece for one with a lower gear score. All styles can be mixed through play.", 14, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            PresetDef04E shown = BuildPresetRules04E.Get(_previewPreset);
            UIKit.Text(new Rect(r.x, y, r.width, 22f), (_linesAreResult ? "APPLIED: " : "PREVIEW: ") + (shown != null ? shown.Title.ToUpperInvariant() : ""), 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            foreach (PresetLine04E l in _lines)
            {
                if (y > r.yMax - 40f) break;
                UIKit.Text(new Rect(r.x + 8f, y, r.width - 8f, 34f), (l.Change ? "<color=#6dff8a>+</color> " : "<color=#888888>=</color> ") + l.Text, 13, l.Change ? Color.white : Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 34f;
            }
        }

        // ================================================================== WARD

        private static void DrawWard(Rect r, BuildSystem b)
        {
            GadgetSystem gs = Game.Gadgets;
            float colW = (r.width - 20f) * 0.5f;
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, colW, 24f), "WARD ASPECT  <size=13>(free to switch out of combat, never while raised)</size>", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            string[] ids = { "", WardRules04E.EmberMantle, WardRules04E.Frostglass, WardRules04E.VoltLattice, WardRules04E.VoidScreen };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                bool open = id.Length == 0 || b.AspectUnlocked(id);
                bool active = b.ActiveAspect == id;
                string label = (open ? "" : "<color=#777777>") + WardRules04E.AspectTitle(id) + (open ? "" : "  <size=12>locked</size></color>") + (active ? "  <size=12>[active]</size>" : "");
                if (UIKit.Button(new Rect(r.x, y, colW * 0.42f, 40f), label, active, 15))
                {
                    if (!open) _message = WardRules04E.AspectTitle(id) + " is a reward you have not earned yet.";
                    else
                    {
                        string why;
                        b.SetActiveAspect(id, out why);
                        _message = why;
                    }
                }
                UIKit.Text(new Rect(r.x + colW * 0.42f + 10f, y, colW * 0.58f - 10f, 46f), "<size=12>" + WardRules04E.AspectLine(id) + "</size>", 12, open ? Color.white : Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 52f;
            }
            y += 6f;
            string state = gs == null ? "Gadgets are not available." :
                "State: " + WardRules04E.StateLabel(gs.WardStateNow) + ".  Capacity " + Mathf.RoundToInt(gs.WardCapacityMax) + (gs.WardIsStarterVariant ? " (starter variant, full at level " + GadgetRules.UnlockLevel(WardRules04E.BucklerId) + ")" : "") +
                ".  It covers about " + Mathf.RoundToInt(WardRules04E.HalfAngle) + " degrees each side of your aim; blasts by angle. Breaks when empty and recovers on the gadget cooldown (" + GadgetRules.Get(WardRules04E.BucklerId).Cooldown.ToString("0") + " s base).";
            UIKit.Text(new Rect(r.x, y, colW, 80f), state, 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float rx = r.x + colW + 20f;
            float ry = r.y;
            UIKit.Text(new Rect(rx, ry, colW, 24f), "FRACTURE LATTICE", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            ry += 28f;
            if (!b.FractureOwned) UIKit.Text(new Rect(rx, ry, colW, 40f), "Not yours yet: the Vaultbreaker's first-win reward. It changes how one crystal focus fires.", 13, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            else
            {
                UIKit.Text(new Rect(rx, ry, colW, 40f), FractureRules04E.Line(Rarity.Rare), 12, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                ry += 42f;
                List<FocusCandidate04E> foci = b.OwnedFoci();
                if (foci.Count == 0) UIKit.Text(new Rect(rx, ry, colW, 24f), "You own no crystal focus to fit it to.", 13, Dim, TextAnchor.UpperLeft);
                for (int i = 0; i < foci.Count && i < 5; i++)
                {
                    LootGun g = Game.Loot != null ? Game.Loot.Find(foci[i].Uid) : null;
                    if (g == null) continue;
                    bool on = b.FractureFocusUid == g.Uid;
                    if (UIKit.Button(new Rect(rx, ry, colW, 34f), (on ? "Fitted: " : "Fit to ") + g.Name, on, 14) && !on)
                    {
                        string why;
                        b.FitFracture(g.Uid, out why);
                        _message = why;
                    }
                    ry += 38f;
                }
            }
            ry += 14f;
            UIKit.Text(new Rect(rx, ry, colW, 24f), "LOOKS AND KEY ITEMS  <size=12>(no combat power)</size>", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            ry += 28f;
            if (b.HasUnlock(RewardCatalog04E.UnlockWardVisual))
            {
                if (UIKit.Button(new Rect(rx, ry, colW, 34f), "Mender's Halo on the ward: " + (b.WardVisualOn ? "ON" : "OFF"), b.WardVisualOn, 14)) b.SetWardVisual(!b.WardVisualOn);
                ry += 40f;
            }
            string extra = (b.KilnCrown ? "Kiln Crown: ward capacity +" + Mathf.RoundToInt(BuildStatRules04E.CrownCapacityBonus * 100f) + "% and a molten crown.\n" : "") +
                           (b.HasKeyItem(RewardCatalog04E.KeyCoupler) ? "Crystal coupler: Jax fits it at the garage bay.\n" : "") +
                           (b.HasUnlock(RewardCatalog04E.UnlockSurvey) ? "Survey notes: rest points and shortcuts of discovered regions on the map.\n" : "") +
                           (b.HasUnlock(RewardCatalog04E.UnlockHomeDisplay) ? "Home trophy case: lit beside your home reward ledger.\n" : "");
            float cool = Game.Crafting != null ? Game.Crafting.BuffLeft(RewardCatalog04E.RecipeCoolant) : 0f;
            if (cool > 0f) extra += "Coolant Draught active: " + CraftingSystem.Clock(cool) + " left.\n";
            UIKit.Text(new Rect(rx, ry, colW, 120f), extra.Length > 0 ? extra : "Nothing here yet: ward looks, the coupler and survey notes come from campaign rewards.", 13, extra.Length > 0 ? Color.white : Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        // ================================================================== REWARDS

        private static void DrawRewards(Rect r, BuildSystem b)
        {
            float leftW = Mathf.Min(420f, r.width * 0.32f);
            Rect left = new Rect(r.x, r.y, leftW, r.height);
            Rect right = new Rect(left.xMax + 20f, r.y, r.xMax - left.xMax - 20f, r.height);
            _pending.Clear();
            foreach (RewardEntrySave e in b.Ledger.Entries) if (e != null && e.Status == RewardLedgerCore04E.Pending) _pending.Add(e);
            float y = left.y;
            UIKit.Text(new Rect(left.x, y, left.width, 24f), "WAITING (" + _pending.Count + ")", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 28f;
            if (_pending.Count == 0)
                UIKit.Text(new Rect(left.x, y, left.width, 60f), "Nothing waiting. Rewards from quests and first wins land here when they need a choice or room; they never expire.", 13, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            bool selectedStill = false;
            for (int i = 0; i < _pending.Count && y < left.yMax - 200f; i++)
            {
                RewardEntrySave e = _pending[i];
                RewardDef04E def = RewardCatalog04E.Get(e.Reward);
                bool sel = _rewardToken == e.Token;
                selectedStill |= sel;
                string label = (def != null ? def.Title : e.Reward) + "\n<size=12>" + (string.IsNullOrEmpty(e.Source) ? "Reward" : e.Source) + (def != null && def.IsChoice ? ", choose 1 of " + def.OfferCount : "") + "</size>";
                if (UIKit.Button(new Rect(left.x, y, left.width, 50f), label, sel, 15))
                {
                    _rewardToken = e.Token;
                    _offer = def != null && !def.IsChoice ? 0 : -1;
                    _message = "";
                    selectedStill = true;
                }
                y += 56f;
            }
            if (!selectedStill && _pending.Count > 0 && _rewardToken.Length > 0 && b.Ledger.IsPending(_rewardToken) == false) _rewardToken = "";
            y = Mathf.Max(y + 10f, left.yMax - 190f);
            UIKit.Text(new Rect(left.x, y, left.width, 22f), "CLAIMED", 14, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            int shown = 0;
            for (int i = b.Ledger.Entries.Count - 1; i >= 0 && shown < 6; i--)
            {
                RewardEntrySave e = b.Ledger.Entries[i];
                if (e == null || e.Status != RewardLedgerCore04E.Claimed) continue;
                UIKit.Text(new Rect(left.x + 6f, y, left.width - 6f, 20f), "<size=12>" + (string.IsNullOrEmpty(e.Delivered) ? e.Reward : e.Delivered) + "</size>", 12, Dim, TextAnchor.UpperLeft);
                y += 22f;
                shown++;
            }
            DrawOffers(right, b);
        }

        private static void DrawOffers(Rect r, BuildSystem b)
        {
            RewardEntrySave e = b.Ledger.Find(_rewardToken);
            if (e == null || e.Status != RewardLedgerCore04E.Pending)
            {
                UIKit.Text(new Rect(r.x, r.y, r.width, 60f), "Pick a waiting reward to see what it holds. Offers are fixed when a reward is earned: reopening, reloading or leaving never rerolls them.", 14, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            RewardDef04E def = RewardCatalog04E.Get(e.Reward);
            List<RewardOffer04E> offers = RewardCatalog04E.Offers(e);
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, r.width, 26f), def != null ? def.Title.ToUpperInvariant() : e.Reward, 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 28f;
            if (def != null)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 40f), def.Blurb + "  <color=#8aa4ad>(" + def.PowerBand + ")</color>", 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 44f;
            }
            int n = offers.Count;
            float cw = n > 0 ? (r.width - (n - 1) * 14f) / n : r.width;
            float ch = Mathf.Max(160f, r.height - (y - r.y) - 110f);
            for (int i = 0; i < n; i++)
            {
                RewardOffer04E o = offers[i];
                Rect c = new Rect(r.x + i * (cw + 14f), y, cw, ch);
                bool sel = _offer == i;
                UIKit.Rect(c, sel ? new Color(0.18f, 0.16f, 0.08f, 0.92f) : UIKit.PanelLight);
                if (sel) UIKit.Frame(c, UIKit.Gold, 2f);
                UIKit.Text(new Rect(c.x + 10f, c.y + 8f, c.width - 20f, 44f), o.Title, 15, sel ? UIKit.Gold : Color.white, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                string detail = o.Detail;
                if (o.CompareSlot >= 0 && Game.Gear != null)
                {
                    GearItem cur = Game.Gear.Equipped[o.CompareSlot];
                    detail += "\n<color=#8aa4ad>Worn " + GearSystem.SlotName((GearSlot)o.CompareSlot) + ": " + (cur != null ? cur.Name + " (score " + GearScoreRules.GearScore(cur.Power, cur.Upgrade, cur.Rarity) + ")" : "nothing") + "</color>";
                }
                if (o.SalvageLumen > 0 || o.SellCash > 0)
                {
                    string worth = o.SalvageLumen > 0 ? "salvage " + o.SalvageLumen.ToString("N0") + " Lumen" : "";
                    if (o.SellCash > 0) worth += (worth.Length > 0 ? ", " : "") + "sells for $" + o.SellCash.ToString("N0");
                    detail += "\n<color=#8aa4ad>Worth: " + worth + "</color>";
                }
                UIKit.Text(new Rect(c.x + 10f, c.y + 54f, c.width - 20f, c.height - 100f), "<size=12>" + detail + "</size>", 12, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (UIKit.Button(new Rect(c.x + 10f, c.yMax - 44f, c.width - 20f, 36f), n > 1 ? (sel ? "Chosen" : "Choose this") : "Selected", sel, 15)) _offer = i;
            }
            y += ch + 14f;
            string claimLabel = _offer >= 0 ? "Claim " + (n > 1 ? "my choice" : "it") + " now" : "Choose an offer first";
            if (UIKit.Button(new Rect(r.x, y, Mathf.Min(360f, r.width), 46f), claimLabel, false, 18))
            {
                if (_offer < 0) _message = "Pick one of the offers first; the choice is final once claimed.";
                else
                {
                    string msg;
                    bool ok = b.Ledger.TryClaim(e.Token, _offer, out msg);
                    _message = msg;
                    if (ok)
                    {
                        _rewardToken = "";
                        _offer = -1;
                    }
                }
            }
            UIKit.Text(new Rect(r.x + 380f, y, r.width - 380f, 46f), "Nothing is spent or lost if a claim cannot be delivered: it stays here until there is room.", 12, Dim, TextAnchor.MiddleLeft, FontStyle.Normal, true, true);
        }

        // ================================================================== LOADOUTS

        private static void DrawLoadouts(Rect r, BuildSystem b)
        {
            float y = r.y;
            UIKit.Text(new Rect(r.x, y, r.width, 24f), "LOADOUTS  <size=13>(save your whole setup; switch out of combat; only items you still own are moved)</size>", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            for (int i = 0; i < 2; i++)
            {
                bool open = i < b.LoadoutSlots;
                Loadout04E l = b.LoadoutAt(i);
                Rect c = new Rect(r.x, y, r.width, 110f);
                UIKit.Rect(c, UIKit.PanelLight);
                string head = "LOADOUT " + (i + 1) + (open ? "" : "  <size=13>locked: What We Choose to Keep unlocks it</size>");
                UIKit.Text(new Rect(c.x + 12f, c.y + 8f, c.width - 24f, 26f), head, 18, open ? Color.white : Dim, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(c.x + 12f, c.y + 36f, c.width - 420f, 70f), "<size=12>" + Describe(l) + "</size>", 12, Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (open)
                {
                    if (UIKit.Button(new Rect(c.xMax - 400f, c.y + 34f, 190f, 40f), "Save current setup", false, 15))
                    {
                        string msg;
                        b.SaveLoadout(i, out msg);
                        _message = msg;
                    }
                    if (UIKit.Button(new Rect(c.xMax - 200f, c.y + 34f, 190f, 40f), "Equip", false, 15))
                    {
                        string msg;
                        b.EquipLoadout(i, out msg);
                        _message = msg;
                    }
                }
                y += 124f;
            }
        }

        private static string Describe(Loadout04E l)
        {
            if (l == null || l.IsEmpty) return "Empty.";
            List<string> parts = new List<string>();
            int gear = 0;
            foreach (int u in l.Gear) if (u > 0) gear++;
            parts.Add(gear + " gear piece" + (gear == 1 ? "" : "s"));
            int guns = 0;
            foreach (int u in l.Loot) if (u > 0) guns++;
            parts.Add(guns + " loot gun" + (guns == 1 ? "" : "s"));
            List<string> gad = new List<string>();
            foreach (int id in l.Gadgets) { GadgetSpec s = GadgetRules.Get(id); if (s != null) gad.Add(s.FullName); }
            if (gad.Count > 0) parts.Add("gadgets " + string.Join(" and ", gad.ToArray()));
            if (l.Aspect.Length > 0) parts.Add("aspect " + WardRules04E.AspectTitle(l.Aspect));
            if (l.Preset.Length > 0) parts.Add("saved as " + BuildPresetRules04E.Get(l.Preset).Title);
            return string.Join(", ", parts.ToArray()) + ".";
        }
    }
}
