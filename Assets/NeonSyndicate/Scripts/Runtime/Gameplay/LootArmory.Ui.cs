using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The ARSENAL pause tab (four equipped loot slots, a 30-gun backpack in rarity frames, a detail card, equip, favorite,
    /// compare and scrap, with full mouse and controller navigation) and the gun card drawing shared by the pickup card,
    /// the gun shop and the Forge's ARMORY tab.
    /// </summary>
    public sealed partial class LootArmory
    {
        /// <summary>The numbers a card shows and compares (works for loot guns and the fixed arsenal alike).</summary>
        public struct CardStats
        {
            public bool Valid;
            public bool Heat;
            public float Dps, Damage, Rate, Mag, Reload, Accuracy, Range, Crit;
            /// <summary>04D: a crystal focus (its rows come from CrystalFocusDb, never per-tick numbers).</summary>
            public bool Focus;
            public bool RegionalMelee;
            /// <summary>04D: the sustained beam (Damage is per second, Mag is seconds of beam before an overheat).</summary>
            public bool Beam;
            /// <summary>Damage per second over a whole magazine and reload, or a full heat cycle and vent.</summary>
            public float SustainedDps;
        }

        public static CardStats StatsOf(LootGun g)
        {
            if (g == null) return new CardStats();
            if (RegionalArsenal04OE.IsWeapon(g)) return RegionalArsenal04OE.Card(g);
            if (CrystalFocusDb.BehaviorOfUnique(g.Unique) == CrystalBehavior.None)
                return ConvertedStats04F(LootGenerator.ToWeaponDef(g, WeaponId.Loot1));
            LootStats s = g.Stats;
            bool heat = s.HeatPerShot > 0f;
            CrystalCardLines fl = CrystalCardRules.Lines(g);
            if (fl.Valid)
            {
                // A crystal focus: honest numbers from its profile. The beam shows damage per second and seconds of beam,
                // never the 0.1 s tick it is integrated on.
                return new CardStats
                {
                    Valid = true,
                    Heat = true,
                    Focus = true,
                    Beam = fl.Beam,
                    Dps = fl.SustainedDps,
                    SustainedDps = fl.SustainedDps,
                    Damage = fl.Primary,
                    Rate = fl.Beam ? 0f : 60f / Mathf.Max(0.04f, s.Interval),
                    Mag = fl.BeforeOverheat,
                    Reload = s.Reload,
                    Accuracy = 100f / (1f + 2f * s.SpreadAim),
                    Range = s.Range,
                    Crit = s.CritChance * 100f
                };
            }
            return new CardStats
            {
                Valid = true,
                Heat = heat,
                Dps = LootGenerator.BurstDps(g, true),
                SustainedDps = LootGenerator.SustainedDps(g),
                Damage = s.Damage * Mathf.Max(1, s.Pellets),
                Rate = 60f / Mathf.Max(0.04f, LootGenerator.CycleTime(g)) * (g.HasTrait(LootTrait.BurstFire) ? 3f : 1f),
                Mag = heat ? Mathf.Ceil(1f / s.HeatPerShot) : s.Clip,
                Reload = s.Reload,
                Accuracy = 100f / (1f + 2f * s.SpreadAim),
                Range = s.Range,
                Crit = s.CritChance * 100f
            };
        }

        public static CardStats StatsOf(WeaponDef d)
        {
            if (d != null && RegionalArsenal04OE.IsWeapon(d.Loot)) return RegionalArsenal04OE.Card(d.Loot);
            if (d == null || !d.IsGun) return new CardStats();
            if (d.Loot != null) return StatsOf(d.Loot);
            if (WeaponController.BehaviorOf(d) != CrystalBehavior.None) return ConvertedStats04F(d);
            float pull = d.Damage * Mathf.Max(1, d.Pellets);
            float cycle = Mathf.Max(0.02f, d.Interval);
            return new CardStats
            {
                Valid = true,
                Dps = pull / cycle,
                SustainedDps = d.ClipSize > 0 ? pull * d.ClipSize / (d.ClipSize * cycle + Mathf.Max(0f, d.ReloadTime)) : pull / cycle,
                Damage = d.Damage * Mathf.Max(1, d.Pellets),
                Rate = 60f / Mathf.Max(0.02f, d.Interval),
                Mag = d.ClipSize,
                Reload = d.ReloadTime,
                Accuracy = 100f / (1f + 2f * d.SpreadAim),
                Range = d.Range,
                Crit = 0f
            };
        }

        /// <summary>What a gun is compared with: the gun in hand, else the best loot gun of the same archetype.</summary>
        public CardStats CompareFor(LootGun g, out string label)
        {
            label = "";
            PlayerController p = Game.Player;
            if (p != null && p.Weapons.Current.IsGun && p.Weapons.Current.Loot != g)
            {
                label = p.Weapons.Current.Name;
                NoteCompareH8S(g, p.Weapons.Current.Loot);
                return StatsOf(p.Weapons.Current);
            }
            LootGun best = null;
            float bestDps = -1f;
            for (int i = 0; i < Slots.Length; i++) Consider(Slots[i], g, ref best, ref bestDps);
            foreach (LootGun b in Backpack) Consider(b, g, ref best, ref bestDps);
            if (best == null) return new CardStats();
            label = best.Name;
            NoteCompareH8S(g, best);
            return StatsOf(best);
        }

        private static void Consider(LootGun c, LootGun g, ref LootGun best, ref float bestDps)
        {
            if (c == null || c == g || c.Archetype != g.Archetype) return;
            float d = StatsOf(c).SustainedDps;
            if (d > bestDps)
            {
                bestDps = d;
                best = c;
            }
        }

        private static void StatRow(ref float y, float x, float w, string label, float v, float cmp, bool hasCmp, bool higherBetter, string fmt, string unit, int size)
        {
            UIKit.Text(new Rect(x, y, w * 0.5f, size + 6f), label, size, new Color(0.78f, 0.78f, 0.82f), TextAnchor.UpperLeft);
            string val = v.ToString(fmt) + unit;
            if (hasCmp)
            {
                float diff = v - cmp;
                float rel = Mathf.Abs(diff) / Mathf.Max(0.001f, Mathf.Abs(cmp));
                if (rel > 0.005f)
                {
                    bool better = higherBetter ? diff > 0f : diff < 0f;
                    string sign = diff > 0f ? "+" : "-";
                    val += "  <color=#" + ProgressionUI.Hex(better ? ProgressionUI.Good : ProgressionUI.Bad) + ">" + sign + Mathf.Abs(diff).ToString(fmt) + "</color>";
                }
            }
            UIKit.Text(new Rect(x + w * 0.42f, y, w * 0.58f, size + 6f), val, size, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            y += size + 5f;
        }

        /// <summary>The stat rows of a card with green and red differences against cmp. Returns the y below them.</summary>
        public static float DrawStatRows(float x, float y, float w, CardStats s, CardStats cmp, int size)
        {
            bool c = cmp.Valid;
            if (s.RegionalMelee)
            {
                StatRow(ref y,x,w,"Base blade damage",s.Damage,cmp.Damage,c && cmp.RegionalMelee,true,"0","",size);
                StatRow(ref y,x,w,"Heavy stamina cost",s.Mag,cmp.Mag,c && cmp.RegionalMelee,false,"0","",size);
                StatRow(ref y,x,w,"Heavy poise multiplier",s.Crit,cmp.Crit,c && cmp.RegionalMelee,true,"0.0","",size);
                StatRow(ref y,x,w,"Heavy recovery",s.Reload,cmp.Reload,c && cmp.RegionalMelee,false,"0.00"," s",size);
                return y;
            }
            if (s.Focus) return DrawFocusRows(x, y, w, s, cmp, size);
            StatRow(ref y, x, w, "Damage per second", s.Dps, cmp.Dps, c, true, "0", "", size);
            StatRow(ref y, x, w, "Damage", s.Damage, cmp.Damage, c, true, "0", "", size);
            StatRow(ref y, x, w, "Fire rate", s.Rate, cmp.Rate, c, true, "0", "/min", size);
            StatRow(ref y, x, w, s.Heat ? "Shots before overheating" : "Magazine", s.Mag, cmp.Mag, c && s.Heat == cmp.Heat, true, "0", "", size);
            StatRow(ref y, x, w, s.Heat ? "Vent time" : "Reload", s.Reload, cmp.Reload, c && s.Heat == cmp.Heat, false, "0.0", " s", size);
            StatRow(ref y, x, w, "Accuracy", s.Accuracy, cmp.Accuracy, c, true, "0", "", size);
            StatRow(ref y, x, w, "Range", s.Range, cmp.Range, c, true, "0", " m", size);
            StatRow(ref y, x, w, "Critical chance", s.Crit, cmp.Crit, c, true, "0", "%", size);
            return y;
        }

        /// <summary>
        /// 04D crystal focus rows: sustained damage per second over a full heat cycle (compared on the same basis for any
        /// gun), the hit or the beam's damage per second, the fire rate (not for the beam), shots or beam seconds before an
        /// overheat, vent time and range. Never a per-tick figure for the beam.
        /// </summary>
        private static float DrawFocusRows(float x, float y, float w, CardStats s, CardStats cmp, int size)
        {
            bool c = cmp.Valid;
            bool same = c && cmp.Focus && cmp.Beam == s.Beam;
            StatRow(ref y, x, w, "Heat-cycle damage per second", s.SustainedDps, cmp.SustainedDps, c, true, "0", "", size);
            StatRow(ref y, x, w, s.Beam ? "Beam damage per second" : "Hit damage", s.Damage, cmp.Damage, same || (c && !s.Beam && !cmp.Beam), true, "0", "", size);
            if (!s.Beam) StatRow(ref y, x, w, "Fire rate", s.Rate, cmp.Rate, c && !cmp.Beam, true, "0", "/min", size);
            StatRow(ref y, x, w, s.Beam ? "Beam seconds before overheating" : "Shots before overheating", s.Mag, cmp.Mag, same, true, s.Beam ? "0.0" : "0", s.Beam ? " s" : "", size);
            StatRow(ref y, x, w, "Vent time", s.Reload, cmp.Reload, c && cmp.Heat, false, "0.0", " s", size);
            StatRow(ref y, x, w, "Range", s.Range, cmp.Range, c, true, "0", " m", size);
            if (!s.Beam) StatRow(ref y, x, w, "Critical chance", s.Crit, cmp.Crit, c, true, "0", "%", size);
            return y;
        }

        /// <summary>The rarity, archetype and level line ("Epic Assault Rifle, level 12").</summary>
        public static string KindLine(LootGun g)
        {
            CrystalProfile p = CrystalFocusDb.Get(CrystalCombatPolicy04F.LootBehavior(g));
            return GearSystem.RarityName(g.Rarity) + " " + (p != null ? p.Name + " form" : "Crystal focus") + ", level " + g.Level;
        }

        /// <summary>
        /// A full gun card: name in its rarity colour, kind and maker, stats with differences against cmp, the element,
        /// the traits and the flavor line. Returns the y below the card's content.
        /// </summary>
        public static float DrawCard(Rect r, LootGun g, CardStats cmp, string cmpLabel, int size)
        {
            Color rc = GearSystem.RarityColor(g.Rarity);
            UIKit.Rect(r, new Color(0.03f, 0.04f, 0.06f, 0.92f));
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), rc);
            UIKit.Frame(r, new Color(rc.r, rc.g, rc.b, 0.6f), 2f);
            float x = r.x + 14f, w = r.width - 28f, y = r.y + 10f;
            UIKit.Text(new Rect(x, y, w, size + 14f), (g.Favorite ? "* " : "") + g.Name, size + 6, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += size + 12f;
            UIKit.Text(new Rect(x, y, w, size + 6f), KindLine(g) + "   " + LootGenerator.MakerFull[(int)g.Maker], size - 2, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += size + 8f;
            if (cmp.Valid && !string.IsNullOrEmpty(cmpLabel))
            {
                // H8S: the upgrade call (arrow, UPGRADE / ABOUT THE SAME / WEAKER, what it was compared with, item scores).
                float vy = Game.Loot != null ? Game.Loot.DrawVerdictH8S(x, y, w, g, cmp, cmpLabel, size) : -1f;
                if (vy >= 0f) y = vy;
                else
                {
                    UIKit.Text(new Rect(x, y, w, size + 4f), "Compared with " + cmpLabel, size - 3, new Color(0.65f, 0.65f, 0.7f), TextAnchor.UpperLeft);
                    y += size + 2f;
                }
            }
            y = DrawStatRows(x, y, w, StatsOf(g), cmp, size);
            if (RegionalArsenal04OE.IsWeapon(g))
            {
                string source = CrystalArsenal.SourceOf(g) ?? "Unverified provenance";
                UIKit.Text(new Rect(x,y+2f,w,size*4.5f),RegionalArsenal04OE.Description(g.Unique)+" Permanent UID " + g.Uid + ". " + source + ". Forge ascension and level upgrades persist; treatments remain temporary.",size-2,UIKit.Cyan,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
                return y+size*4.7f;
            }
            if (g.Element != Element.None)
            {
                Color ec = GearSystem.ElementColor(g.Element);
                UIKit.Text(new Rect(x, y + 2f, w, size + 6f), GearSystem.ElementName(g.Element) + " element: " + Mathf.RoundToInt(g.Stats.ElementChance * 100f) + "% per hit, power " + g.Stats.ElementPower.ToString("0.0"), size - 1, ec, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                y += size + 6f;
            }
            CrystalBehavior form = CrystalCombatPolicy04F.LootBehavior(g);
            CrystalProfile profile = CrystalFocusDb.Get(form);
            if (profile != null)
            {
                string charge = "Resonance: " + CrystalCombatPolicy04F.ShotCost(form, false).ToString("0.#") + (form == CrystalBehavior.PrismThread ? " per 0.1 s. " : " per shot. ") + "Melee or charging cradles refill it.";
                UIKit.Text(new Rect(x, y + 2f, w, size * 2.4f), charge, size - 2, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += size * 2.6f;
            }
            bool originalSignature = CrystalFocusDb.BehaviorOfUnique(g.Unique) != CrystalBehavior.None;
            if (!originalSignature && profile != null)
            {
                UIKit.Text(new Rect(x, y + 2f, w, size * 3.2f), profile.FireLine + " Trigger-changing traits are dormant in this form; damage, element and handling bonuses remain.", size - 2, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return y + size * 3.4f;
            }
            for (int i = 0; i < g.Traits.Count; i++)
            {
                string t = "<color=#" + ProgressionUI.Hex(UIKit.Gold) + ">" + LootGenerator.TraitName(g.Traits[i]) + ":</color> " + LootGenerator.TraitText(g.Traits[i], g.Archetype);
                float th = UIKit.Measure(t, size - 2).x > w ? (size - 2) * 2.4f : size + 4f;
                UIKit.Text(new Rect(x, y + 2f, w, th), t, size - 2, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += th + 2f;
            }
            if (CrystalForgeRules.IsFocus(g)) return DrawFocusLines(x, y, w, g, size);
            if (g.IsUnique)
            {
                UIKit.Text(new Rect(x, y + 4f, w, size * 2.4f), "\"" + g.Flavor + "\"", size - 2, new Color(0.9f, 0.85f, 0.7f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
                y += size * 2.4f;
            }
            return y;
        }

        /// <summary>
        /// 04D focus card lines: how it fires, the hit pattern, heat, range, its deliberate limitation and where it came
        /// from ("Foreman signature", "Resonance case"), plus the granted-once note. All from CrystalFocusDb.
        /// </summary>
        private static float DrawFocusLines(float x, float y, float w, LootGun g, int size)
        {
            CrystalCardLines fl = CrystalCardRules.Lines(g);
            if (!fl.Valid) return y;
            int ts = size - 3;
            y = FocusLine(x, y, w, "<color=#" + ProgressionUI.Hex(UIKit.Cyan) + ">How it fires:</color> " + fl.FireLine, ts, Color.white);
            y = FocusLine(x, y, w, fl.HitLine + ". " + fl.RangeLine + ".", ts, new Color(0.85f, 0.88f, 0.92f));
            y = FocusLine(x, y, w, "<color=#" + ProgressionUI.Hex(UIKit.Gold) + ">Heat:</color> " + fl.HeatLine, ts, Color.white);
            y = FocusLine(x, y, w, "<color=#" + ProgressionUI.Hex(ProgressionUI.Bad) + ">Limitation:</color> " + fl.Limitation, ts, Color.white);
            string source = CrystalArsenal.SourceOf(g);
            string srcLine = "Source: " + (string.IsNullOrEmpty(source) ? "unrecorded (a test or debug copy)" : source) + ". Takes no traits; Forge element, level and rarity keep its behavior.";
            y = FocusLine(x, y, w, srcLine, ts, new Color(0.9f, 0.85f, 0.7f));
            return y;
        }

        private static float FocusLine(float x, float y, float w, string text, int size, Color c)
        {
            float lines = Mathf.Max(1f, Mathf.Ceil(UIKit.Measure(text, size).x / Mathf.Max(60f, w)));
            float h = lines * (size + 5f);
            UIKit.Text(new Rect(x, y + 2f, w, h), text, size, c, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            return y + h + 2f;
        }

        // ------------------------------------------------------------------ the ARSENAL tab

        private int _selUid;
        /// <summary>04D: the crystal claim shown on the right instead of a gun card (null: none).</summary>
        private string _claimToken;
        private int _sellArmedUid;
        private float _sellArmedUntil;
        private int _scrapArmedUid;
        private float _scrapArmedUntil;
        private int _compareSlot = -1;
        private string _status = "";
        private float _statusUntil;
        private bool _statusGood;

        private void Status(string s, bool good)
        {
            _status = s ?? "";
            _statusGood = good;
            _statusUntil = Time.unscaledTime + 3.5f;
        }

        /// <summary>One gun cell: rarity frame, name, level, favorite star; clicking (or A on the pad) selects it.</summary>
        private void DrawCell(Rect c, LootGun g, string label, LootGun sel)
        {
            bool hot, focused;
            bool clicked = ProgressionUI.Hotspot(c, out hot, out focused);
            UIKit.Rect(c, g != null ? new Color(0.06f, 0.07f, 0.1f, 0.95f) : new Color(0f, 0f, 0f, 0.4f));
            if (g != null)
            {
                Color rc = GearSystem.RarityColor(g.Rarity);
                UIKit.Frame(c, rc, g.Rarity >= Rarity.Epic ? 3f : 2f);
                UIKit.Rect(new Rect(c.x, c.yMax - 5f, c.width, 5f), new Color(rc.r, rc.g, rc.b, 0.5f));
                UIKit.Text(new Rect(c.x + 6f, c.y + (label != null ? 20f : 4f), c.width - 12f, c.height - 26f), g.Name, 14, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(c.x + 6f, c.yMax - 22f, c.width - 12f, 16f), CellLineH8S(g), 12, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft);
                if (g.Favorite) UIKit.Tex(new Rect(c.xMax - 18f, c.y + 3f, 15f, 15f), UIKit.Star, UIKit.Gold);
                DrawCellMarkH8S(c, g, label);
            }
            else UIKit.Frame(c, new Color(1f, 1f, 1f, 0.1f), 1f);
            if (label != null) UIKit.Text(new Rect(c.x + 6f, c.y + 2f, c.width - 12f, 18f), label, 13, new Color(0.7f, 0.7f, 0.75f), TextAnchor.UpperLeft, FontStyle.Bold);
            if (g != null && sel == g) UIKit.Frame(new Rect(c.x - 3f, c.y - 3f, c.width + 6f, c.height + 6f), UIKit.Gold, 2f);
            if (focused) UIKit.Frame(new Rect(c.x - 5f, c.y - 5f, c.width + 10f, c.height + 10f), UIKit.Cyan, 3f);
            else if (hot) UIKit.Frame(new Rect(c.x - 4f, c.y - 4f, c.width + 8f, c.height + 8f), new Color(1f, 1f, 1f, 0.5f), 2f);
            if (clicked && g != null)
            {
                _selUid = g.Uid;
                _claimToken = null;
            }
        }

        private void DrawArsenal(Rect r)
        {
            LootGun sel = Find(_selUid);
            float leftW = Mathf.Min(820f, r.width * 0.55f);
            Rect left = new Rect(r.x, r.y, leftW, r.height);
            Rect right = new Rect(left.xMax + 24f, r.y, r.xMax - left.xMax - 24f, r.height);
            UIKit.Text(new Rect(left.x, left.y, left.width, 26f), "EQUIPPED LOOT SLOTS", 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float cw = (left.width - 30f) / 4f;
            for (int i = 0; i < Slots.Length; i++) DrawCell(new Rect(left.x + i * (cw + 10f), left.y + 30f, cw, 92f), Slots[i], "SLOT " + (i + 1), sel);
            float top = left.y + 140f;
            UIKit.Text(new Rect(left.x, top, left.width, 26f), "BACKPACK  " + Backpack.Count + " / " + BackpackSize, 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            // Phase 5 loot world tally (chests opened, satchels found, keys held, bounties), right-aligned on the same row.
            if (Game.LootWorld != null) Game.LootWorld.DrawArsenalTally(new Rect(left.x + 200f, top + 2f, left.width - 200f, 24f));
            const int cols = 6;
            float gw = (left.width - (cols - 1) * 8f) / cols;
            float gh = Mathf.Min(74f, (left.yMax - top - 40f) / 5f - 8f);
            for (int k = 0; k < BackpackSize; k++)
            {
                Rect c = new Rect(left.x + (k % cols) * (gw + 8f), top + 30f + (k / cols) * (gh + 8f), gw, gh);
                DrawCell(c, k < Backpack.Count ? Backpack[k] : null, null, sel);
            }

            // 04D: pending crystal rewards and open offers sit above the card, with their own claim panel.
            right = DrawCrystalClaims(right);
            if (_claimToken != null && DrawClaimPanel(right)) return;
            if (sel == null)
            {
                string help = "Loot guns drop from enemies in light beams: the taller the beam, the rarer the gun.\n\n" +
                              GInput.Prompt("Press E near a gun to take it to the backpack; hold it to equip it at once.") + "\n\n" +
                              "Pick a gun here to see its card, equip it to one of the four loot slots, mark it as a favorite (favorites cannot be scrapped or sold) or scrap it for materials and weapon cores.\n\n" +
                              "Sell guns at any gun shop (Loot guns tab). Upgrade them at the Forge's ARMORY tab.";
                UIKit.Text(new Rect(right.x + 10f, right.y + 10f, right.width - 20f, right.height - 20f), help, 18, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                DrawStatus(right);
                return;
            }
            string cmpLabel;
            CardStats cmp = ArsenalCompare(sel, out cmpLabel);
            Rect card = new Rect(right.x, right.y, right.width, right.height - 190f);
            DrawCard(card, sel, cmp, cmpLabel, 18);
            float by = card.yMax + 10f;
            float bw = (right.width - 30f) / 4f;
            int at = SlotOf(sel);
            for (int i = 0; i < Slots.Length; i++)
            {
                string label = at == i ? "IN SLOT " + (i + 1) : "EQUIP " + (i + 1);
                if (UIKit.Button(new Rect(right.x + i * (bw + 10f), by, bw, 44f), label, at == i, 16) && at != i)
                {
                    Equip(sel, i, false);
                    Status(sel.Name + " is in loot slot " + (i + 1) + ".", true);
                }
            }
            by += 52f;
            if (at >= 0)
            {
                if (UIKit.Button(new Rect(right.x, by, bw, 44f), "UNEQUIP", false, 16))
                {
                    if (Unequip(at)) Status(sel.Name + " moved to the backpack.", true);
                    else Status("The backpack is full.", false);
                }
            }
            if (UIKit.Button(new Rect(right.x + (bw + 10f), by, bw, 44f), sel.Favorite ? "UNFAVORITE" : "FAVORITE", sel.Favorite, 16)) ToggleFavorite(sel);
            string cmpName = _compareSlot < 0 ? "AUTO" : "SLOT " + (_compareSlot + 1);
            if (UIKit.Button(new Rect(right.x + 2f * (bw + 10f), by, bw, 44f), "COMPARE: " + cmpName, false, 14)) _compareSlot = _compareSlot >= Slots.Length - 1 ? -1 : _compareSlot + 1;
            bool armed = _scrapArmedUid == sel.Uid && Time.unscaledTime < _scrapArmedUntil;
            if (UIKit.Button(new Rect(right.x + 3f * (bw + 10f), by, bw, 44f), armed ? "CONFIRM" : "SCRAP", armed, 16))
            {
                if (sel.Favorite) Status("Favorites cannot be scrapped.", false);
                else if (!armed)
                {
                    _scrapArmedUid = sel.Uid;
                    _scrapArmedUntil = Time.unscaledTime + 3f;
                    Status("Press CONFIRM to scrap " + sel.Name + "." + (CrystalForgeRules.NeedsGrantedOnceConfirm(sel) ? " " + CrystalForgeRules.GrantedOnce : ""), false);
                }
                else
                {
                    string n = sel.Name;
                    if (Scrap(sel)) Status("Scrapped " + n + ".", true);
                    _scrapArmedUid = 0;
                    _selUid = 0;
                    return;
                }
            }
            by += 54f;
            ForgeCost y = LootForgeMath.ScrapYield(sel);
            string gives = "";
            foreach (KeyValuePair<string, int> kv in y.Materials) gives += (gives.Length > 0 ? ", " : "") + kv.Value + " " + MaterialDb.NameOf(kv.Key);
            UIKit.Text(new Rect(right.x, by, right.width, 44f), "Sells for " + MathUtil.Money(LootEconomy.SellValue(sel)) + " at any gun shop.  Scrapping gives " + gives + ".", 15, new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            DrawStatus(right);
        }

        private CardStats ArsenalCompare(LootGun sel, out string label)
        {
            label = "";
            if (_compareSlot >= 0)
            {
                LootGun c = Slots[_compareSlot];
                if (c == null || c == sel) return new CardStats();
                label = c.Name + " (slot " + (_compareSlot + 1) + ")";
                NoteCompareH8S(sel, c);
                return StatsOf(c);
            }
            // Auto: the equipped gun of the same archetype, else the gun in hand.
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && Slots[i] != sel && Slots[i].Archetype == sel.Archetype)
                {
                    label = Slots[i].Name + " (slot " + (i + 1) + ")";
                    NoteCompareH8S(sel, Slots[i]);
                    return StatsOf(Slots[i]);
                }
            }
            PlayerController p = Game.Player;
            if (p != null && p.Weapons.Current.IsGun && p.Weapons.Current.Loot != sel)
            {
                label = p.Weapons.Current.Name;
                NoteCompareH8S(sel, p.Weapons.Current.Loot);
                return StatsOf(p.Weapons.Current);
            }
            return new CardStats();
        }

        // ------------------------------------------------------------------ 04D crystal claims

        /// <summary>
        /// The "Crystal claims" strip at the top of the ARSENAL card column: every pending reward ("Reward waiting: free a
        /// slot or collect at the case or the Forge") and every open offer, each with a VIEW button. Returns the space left.
        /// </summary>
        private Rect DrawCrystalClaims(Rect right)
        {
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return right;
            List<CrystalClaimView> views = a.Claims();
            int shown = 0;
            for (int i = 0; i < views.Count; i++) if (!views[i].Locked) shown++;
            if (shown == 0)
            {
                if (_claimToken != null)
                {
                    LootGun done = a.HasEntitlement(_claimToken) ? a.PreviewFor(_claimToken) : null;
                    if (done != null && done.Uid > 0 && Find(done.Uid) != null) _selUid = done.Uid;
                }
                _claimToken = null;
                return right;
            }
            int rows = Mathf.Min(3, shown);
            float h = 34f + rows * 40f;
            Rect box = new Rect(right.x, right.y, right.width, h);
            UIKit.Rect(box, new Color(0.08f, 0.06f, 0.02f, 0.85f));
            UIKit.Frame(box, new Color(1f, 0.82f, 0.3f, 0.7f), 2f);
            int pending = a.PendingCount;
            UIKit.Text(new Rect(box.x + 12f, box.y + 6f, box.width - 24f, 24f), "CRYSTAL CLAIMS" + (pending > 0 ? "   Reward waiting: free a slot or collect at the case or the Forge." : ""), 15, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            float y = box.y + 34f;
            int drawn = 0;
            bool selectedStillListed = false;
            for (int i = 0; i < views.Count; i++)
            {
                CrystalClaimView v = views[i];
                if (v.Locked) continue;
                if (v.Token == _claimToken) selectedStillListed = true;
                if (drawn >= rows) continue;
                drawn++;
                LootUniqueDef u = LootUniques.Get(v.Unique);
                Color rc = u != null ? GearSystem.RarityColor(u.Rarity) : Color.white;
                UIKit.Text(new Rect(box.x + 12f, y + 2f, box.width - 160f, 22f), v.Title + "  <color=#cfcfd6>" + (v.Pending ? (v.Retro ? "earlier victory, " : "") + v.Source : v.Status) + "</color>", 15, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                if (UIKit.Button(new Rect(box.xMax - 136f, y, 124f, 34f), _claimToken == v.Token ? "SHOWN" : "VIEW", _claimToken == v.Token, 15))
                {
                    _claimToken = v.Token;
                    _selUid = 0;
                }
                y += 40f;
            }
            if (shown > rows) UIKit.Text(new Rect(box.x + 12f, box.yMax - 4f, box.width - 24f, 18f), "and " + (shown - rows) + " more at the resonance case or the Forge", 12, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft);
            if (_claimToken != null && !selectedStillListed)
            {
                // A claim just completed: show the gun itself, where it now sits.
                LootGun done = a.HasEntitlement(_claimToken) ? a.PreviewFor(_claimToken) : null;
                if (done != null && done.Uid > 0 && Find(done.Uid) != null) _selUid = done.Uid;
                _claimToken = null;
            }
            return new Rect(right.x, right.y + h + (shown > rows ? 24f : 10f), right.width, right.height - h - (shown > rows ? 24f : 10f));
        }

        /// <summary>The selected crystal claim: its full card (compared with your gun) and the explicit choices. True when drawn.</summary>
        private bool DrawClaimPanel(Rect right)
        {
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return false;
            LootGun g = a.PreviewFor(_claimToken);
            if (g == null)
            {
                _claimToken = null;
                return false;
            }
            string cmpLabel;
            CardStats cmp = CompareFor(g, out cmpLabel);
            Rect card = new Rect(right.x, right.y, right.width, right.height - 200f);
            DrawCard(card, g, cmp, cmpLabel, 17);
            Rect act = new Rect(right.x, card.yMax + 10f, right.width, 150f);
            string reason;
            bool offer = !a.HasEntitlement(_claimToken);
            if (offer && !a.OfferOpen(_claimToken, out reason))
            {
                UIKit.Text(new Rect(act.x, act.y, act.width, 60f), reason, 17, new Color(1f, 0.8f, 0.45f), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            }
            else if (offer && a.OfferNeedsCase)
            {
                UIKit.Text(new Rect(act.x, act.y, act.width, 50f), "Take it at the resonance case beside the Veyra Lofts arrival (A on the map). The case also holds the contract board.", 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (UIKit.Button(new Rect(act.x, act.y + 58f, 260f, 44f), "TRACK THE CASE", false, 17)) a.TrackCase();
            }
            else
            {
                bool close;
                CrystalOfferUi.DrawClaimActions(act, _claimToken, false, Status, out close);
            }
            DrawStatus(right);
            return true;
        }

        /// <summary>
        /// Selling a crystal focus at a gun shop asks twice ("granted once"); other guns sell on the first press. Returns
        /// true when the sale may go ahead now.
        /// </summary>
        public bool ConfirmGrantedOnce(LootGun g, System.Action<string> status)
        {
            if (g == null || !CrystalForgeRules.NeedsGrantedOnceConfirm(g)) return true;
            if (_sellArmedUid == g.Uid && Time.unscaledTime < _sellArmedUntil)
            {
                _sellArmedUid = 0;
                return true;
            }
            _sellArmedUid = g.Uid;
            _sellArmedUntil = Time.unscaledTime + 3f;
            if (status != null) status("Sell " + g.Name + "? " + CrystalForgeRules.GrantedOnce + " Press Sell again to confirm.");
            return false;
        }

        private void DrawStatus(Rect right)
        {
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(right.x, right.yMax - 28f, right.width, 26f), _status, 17, _statusGood ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
        }
    }
}
