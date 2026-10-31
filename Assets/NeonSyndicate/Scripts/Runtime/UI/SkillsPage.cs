using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The SKILLS page (pause menu tab, and the SKILLS tab of the shrine menu where the trees can be reset): the Body,
    /// Mind and Shard trees side by side, six tiers of five nodes with their links, ranks and costs, a details panel
    /// for the node under the mouse or the controller focus, and a LEARN button. Clicking (or pressing A on) a node
    /// selects it; doing it again on the selected node learns it.
    /// </summary>
    public static partial class SkillsPage
    {
        private static string _selected;
        private static string _status = "";
        private static float _statusUntil;
        private static bool _statusGood;
        private static float _confirmRespecUntil;

        // Text fitted to the current layout (narrow screens such as 5:4), measured again only when the sizes change.
        private static int[] _nameFont;
        private static float _fitNodeW = -1f;
        private static float _fitColW = -1f;
        private static readonly int[] _blurbFont = new int[3];
        private static readonly bool[] _blurbWrap = new bool[3];
        private static string _titleFor;
        private static float _titleW = -1f;
        private static int _titleFont = 26;

        private static readonly Color[] TreeColors = { new Color(1f, 0.48f, 0.36f), new Color(0.35f, 0.85f, 1f), new Color(0.72f, 0.48f, 1f) };
        private static readonly Color Signature = new Color(1f, 0.35f, 0.75f);

        public static void ResetStatics()
        {
            _selected = null;
            _status = "";
            _statusUntil = 0f;
            _confirmRespecUntil = 0f;
            _nameFont = null;
            _fitNodeW = -1f;
            _fitColW = -1f;
            _titleFor = null;
            _titleW = -1f;
        }

        private static void Status(string s, bool good)
        {
            _status = s;
            _statusGood = good;
            _statusUntil = Time.unscaledTime + 3.5f;
        }

        public static void Draw(Rect r, bool atShrine)
        {
            UIKit.Rect(r, UIKit.Panel);
            SkillTree st = Game.Skills;
            if (st == null)
            {
                UIKit.Text(new Rect(r.x + 40f, r.y + 40f, r.width - 80f, 40f), "The skill trees are not available.", 24, Color.white, TextAnchor.UpperLeft);
                return;
            }
            SkillBook book = st.Book;
            // Narrow screens (5:4, small windows) get tighter margins so the nodes keep room for their names.
            bool narrow = r.width < 1500f;
            float pad = narrow ? 12f : 20f;
            // Header: points, and the reset (at a shrine) or where to reset.
            UIKit.Text(new Rect(r.x + pad, r.y + 10f, 600f, 40f), "SKILL POINTS  " + Mathf.Max(0, book.Available), 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            string earned = "Level " + book.Level + ": one point per level after the first" + (book.Granted > 0 ? ", plus " + book.Granted + " from quests" : "") + ". Spent " + book.Spent + ".";
            UIKit.Text(new Rect(r.x + pad, r.y + 48f, r.width * 0.6f, 24f), earned, 17, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            DrawRespec(new Rect(r.xMax - pad - 420f, r.y + 12f, 420f, 50f), st, atShrine);

            float detailsW = Mathf.Clamp(r.width * 0.24f, narrow ? 280f : 300f, 430f);
            Rect trees = new Rect(r.x + pad, r.y + 82f, r.width - pad * 3f - detailsW, r.height - 82f - pad);
            Rect details = new Rect(trees.xMax + pad, trees.y, detailsW, trees.height);
            float colW = trees.width / 3f;
            float colGap = narrow ? 8f : 14f;
            SkillNode hot = null;
            for (int t = 0; t < 3; t++)
            {
                Rect col = new Rect(trees.x + t * colW, trees.y, colW - colGap, trees.height);
                SkillNode h = DrawTree(col, (SkillTreeId)t, book, st);
                if (h != null) hot = h;
            }
            DrawDetails(details, hot ?? SkillDb.Get(_selected), book, st);
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(r.x + pad, r.yMax - 30f, trees.width, 26f), _status, 19, _statusGood ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        private static void DrawRespec(Rect r, SkillTree st, bool atShrine)
        {
            int cost = st.RespecCost;
            if (!atShrine)
            {
                UIKit.Text(r, "Reset the trees at any Resonance Shrine (" + cost.ToString("N0") + " Lumen).", 16, new Color(0.8f, 0.7f, 1f), TextAnchor.MiddleRight, FontStyle.Normal, true, true);
                return;
            }
            bool confirming = Time.unscaledTime < _confirmRespecUntil;
            string label = confirming ? "CONFIRM RESET (" + cost.ToString("N0") + " Lumen)" : "RESET ALL TREES (" + cost.ToString("N0") + " Lumen)";
            if (!UIKit.Button(r, label, confirming, 18)) return;
            Progression pr = Game.Progress;
            if (st.Book.Spent <= 0) { Status("Nothing to reset yet.", false); return; }
            if (pr == null || pr.Lumen < cost) { Status("Resetting costs " + cost.ToString("N0") + " Lumen.", false); return; }
            if (!confirming)
            {
                _confirmRespecUntil = Time.unscaledTime + 4f;
                Status("Press again to reset every tree. All points come back.", true);
                return;
            }
            _confirmRespecUntil = 0f;
            if (st.Respec()) Status("Every tree reset. " + st.Book.Available + " points to spend.", true);
            else Status("The trees could not be reset.", false);
        }

        /// <summary>Draws one tree column; returns the node under the mouse or the pad focus, if any.</summary>
        private static SkillNode DrawTree(Rect col, SkillTreeId tree, SkillBook book, SkillTree st)
        {
            Color tc = TreeColors[(int)tree];
            float laneW = col.width / SkillDb.Lanes;
            float nodeW = Mathf.Min(laneW - (laneW < 70f ? 4f : 8f), 110f);
            FitTexts(nodeW, col.width);
            int ti = (int)tree;
            UIKit.Rect(new Rect(col.x, col.y, col.width, 3f), tc);
            UIKit.Text(new Rect(col.x, col.y + 6f, col.width, 30f), SkillDb.TreeName(tree).ToUpperInvariant(), 24, tc, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(col.x, col.y + 10f, col.width, 24f), book.SpentIn(tree) + " spent", 16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperRight);
            // The blurb shrinks to fit one line, or wraps onto two on narrow screens (the grid then starts lower).
            UIKit.Text(new Rect(col.x, col.y + 36f, col.width, _blurbWrap[ti] ? 32f : 22f), SkillDb.TreeBlurb(tree), _blurbFont[ti], new Color(0.75f, 0.75f, 0.78f), TextAnchor.UpperLeft, FontStyle.Normal, true, _blurbWrap[ti]);
            float head = _blurbWrap[ti] ? 70f : 62f;
            float top = col.y + head;
            float gridH = col.height - head;
            float rowH = gridH / SkillDb.Tiers;
            float nodeH = Mathf.Clamp(rowH - 24f, 34f, 62f);
            // Tier bands: tiers that are still locked are dimmed and say what they need.
            for (int tier = 0; tier < SkillDb.Tiers; tier++)
            {
                int need = SkillBook.TierNeed(tier);
                bool open = book.SpentBelow(tree, tier) >= need;
                Rect band = new Rect(col.x, top + tier * rowH + 2f, col.width, rowH - 4f);
                UIKit.Rect(band, open ? new Color(1f, 1f, 1f, 0.025f) : new Color(0f, 0f, 0f, 0.35f));
                if (!open) UIKit.Text(new Rect(band.x + 2f, band.y, band.width, 16f), "needs " + need + " points above", 12, new Color(0.7f, 0.7f, 0.7f, 0.9f), TextAnchor.UpperLeft);
            }
            // Links (drawn first, under the nodes).
            foreach (SkillNode n in SkillDb.All)
            {
                if (n.Tree != tree) continue;
                Rect nr = NodeRect(n, col, top, rowH, laneW, nodeW, nodeH);
                foreach (string pid in n.Prereqs)
                {
                    SkillNode p = SkillDb.Get(pid);
                    if (p == null || p.Tree != tree) continue;
                    Rect pr = NodeRect(p, col, top, rowH, laneW, nodeW, nodeH);
                    bool lit = book.Has(p.Id);
                    Color lc = lit && book.Has(n.Id) ? tc : (lit ? new Color(tc.r, tc.g, tc.b, 0.55f) : new Color(1f, 1f, 1f, 0.12f));
                    ProgressionUI.Line(new Vector2(pr.center.x, pr.yMax), new Vector2(nr.center.x, nr.y), lc, lit ? 3f : 2f);
                }
            }
            SkillNode hot = null;
            for (int i = 0; i < SkillDb.All.Length; i++)
            {
                SkillNode n = SkillDb.All[i];
                if (n.Tree != tree) continue;
                if (DrawNode(n, NodeRect(n, col, top, rowH, laneW, nodeW, nodeH), book, st, tc, _nameFont[i])) hot = n;
            }
            return hot;
        }

        /// <summary>
        /// Fits every node name (each word inside its node, 13 down to 9 points) and the tree blurbs (one line from 14 down
        /// to 11 points, else two lines) to the current sizes. Measures only when the sizes change.
        /// </summary>
        private static void FitTexts(float nodeW, float colW)
        {
            SkillNode[] all = SkillDb.All;
            if (_nameFont != null && _nameFont.Length == all.Length && Mathf.Abs(nodeW - _fitNodeW) < 0.5f && Mathf.Abs(colW - _fitColW) < 0.5f) return;
            _fitNodeW = nodeW;
            _fitColW = colW;
            _nameFont = new int[all.Length];
            float room = nodeW - 6f;
            int max = nodeW < 80f ? 12 : 13;
            for (int i = 0; i < all.Length; i++)
            {
                string[] words = (all[i].Name ?? "").Split(' ');
                _nameFont[i] = UiFit.LargestFont(max, 9, room, s => WidestWord(words, s));
            }
            for (int t = 0; t < _blurbFont.Length; t++)
            {
                string blurb = SkillDb.TreeBlurb((SkillTreeId)t);
                int f = UiFit.LargestFont(14, 11, colW, s => UIKit.Measure(blurb, s).x);
                bool fits = UIKit.Measure(blurb, f).x <= colW;
                _blurbWrap[t] = !fits;
                _blurbFont[t] = fits ? f : 12;
            }
        }

        private static float WidestWord(string[] words, int size)
        {
            float w = 0f;
            foreach (string word in words) if (word.Length > 0) w = Mathf.Max(w, UIKit.Measure(word, size, FontStyle.Bold).x);
            return w;
        }

        /// <summary>The details title's font: 26 points, smaller when the name would wrap in a narrow panel (cached per node and width).</summary>
        private static int TitleFont(SkillNode n, float w)
        {
            if (n.Id != _titleFor || Mathf.Abs(w - _titleW) > 0.5f)
            {
                _titleFor = n.Id;
                _titleW = w;
                string name = n.Name ?? "";
                _titleFont = UiFit.LargestFont(26, 18, w, s => UIKit.Measure(name, s, FontStyle.Bold).x);
            }
            return _titleFont;
        }

        private static Rect NodeRect(SkillNode n, Rect col, float top, float rowH, float laneW, float nodeW, float nodeH)
        {
            float cx = col.x + laneW * (n.Lane + 0.5f);
            float cy = top + rowH * (n.Tier + 0.5f) + 6f;
            return new Rect(cx - nodeW * 0.5f, cy - nodeH * 0.5f, nodeW, nodeH);
        }

        /// <summary>One node: its look follows learned, ready or locked; returns true while it is hot. nameFont fits its name.</summary>
        private static bool DrawNode(SkillNode n, Rect r, SkillBook book, SkillTree st, Color tc, int nameFont)
        {
            bool hot, focused;
            bool clicked = ProgressionUI.Hotspot(r, out hot, out focused);
            int rank = book.Rank(n.Id);
            bool maxed = rank >= n.MaxRank;
            LearnCheck check = book.Check(n.Id);
            bool ready = check.Ok;
            bool selected = _selected == n.Id;
            Color fill;
            Color text;
            if (rank > 0)
            {
                fill = new Color(tc.r, tc.g, tc.b, maxed ? 0.9f : 0.6f);
                text = Color.black;
            }
            else if (ready)
            {
                fill = new Color(0.08f, 0.1f, 0.14f, 0.95f);
                text = Color.white;
            }
            else
            {
                fill = new Color(0.05f, 0.05f, 0.06f, 0.9f);
                text = new Color(0.55f, 0.55f, 0.58f);
            }
            UIKit.Rect(r, fill);
            if (ready) UIKit.Frame(r, tc, 2f);
            else if (rank == 0) UIKit.Frame(r, new Color(1f, 1f, 1f, 0.12f), 1f);
            if (n.Signature) UIKit.Tex(new Rect(r.xMax - 15f, r.y + 2f, 13f, 13f), UIKit.Star, Signature);
            UIKit.Text(new Rect(r.x + 3f, r.y + 2f, r.width - 6f, r.height - 16f), n.Name, nameFont, text, TextAnchor.MiddleCenter, FontStyle.Bold, rank == 0, true);
            string ranks = rank + "/" + n.MaxRank + (n.Cost > 1 ? "  (" + n.Cost + ")" : "");
            UIKit.Text(new Rect(r.x, r.yMax - 16f, r.width, 15f), ranks, 12, rank > 0 ? new Color(0f, 0f, 0f, 0.85f) : new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperCenter, FontStyle.Normal, false);
            if (selected) UIKit.Frame(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), UIKit.Gold, 2f);
            if (focused) UIKit.Frame(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), UIKit.Cyan, 3f);
            else if (hot) UIKit.Frame(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), new Color(1f, 1f, 1f, 0.6f), 2f);
            if (clicked)
            {
                if (!selected) _selected = n.Id;
                else TryLearn(n, st);
            }
            return hot;
        }

        private static void TryLearn(SkillNode n, SkillTree st)
        {
            LearnCheck c = st.Book.Check(n.Id);
            if (!c.Ok)
            {
                Status(n.Name + ": " + c.Detail, false);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.4f);
                return;
            }
            // H8S: say what the rank changed, worked out just before learning it.
            string summary = LearnSummaryBeforeH8S(n, st.Book);
            if (st.Learn(n.Id)) Status(LearnedTextH8S(n, st.Book.Rank(n.Id), summary), true);
        }

        private static void DrawDetails(Rect r, SkillNode n, SkillBook book, SkillTree st)
        {
            UIKit.Rect(r, new Color(1f, 1f, 1f, 0.04f));
            float x = r.x + 16f, w = r.width - 32f, y = r.y + 14f;
            if (n == null)
            {
                string help = "Pick a skill to see what it does.\n\n" +
                              "Every level gives one skill point; quests can give more. A tier opens once enough points are spent in the tiers above it, and each skill needs the one linked above it.\n\n" +
                              "Star-marked skills are signatures: they change how you play.\n\n" +
                              (Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.South) + " selects a skill, " + Pad.Tag(PadButton.South) + " again learns it." : "Click a skill to select it, click it again (or LEARN) to learn it.");
                UIKit.Text(new Rect(x, y, w, r.height - 28f), help, 17, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                DrawBonuses(new Rect(x, r.yMax - 230f, w, 220f));
                return;
            }
            Color tc = TreeColors[(int)n.Tree];
            int rank = book.Rank(n.Id);
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), tc);
            UIKit.Text(new Rect(x, y, w, 34f), n.Name, TitleFont(n, w), tc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 36f;
            string sub = SkillDb.TreeName(n.Tree) + ", tier " + (n.Tier + 1) + (n.Signature ? ", signature" : "") + "\nRank " + rank + " of " + n.MaxRank + ", " + n.Cost + " point" + (n.Cost == 1 ? "" : "s") + " per rank";
            UIKit.Text(new Rect(x, y, w, 46f), sub, 16, n.Signature ? Signature : new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 52f;
            UIKit.Text(new Rect(x, y, w, 110f), n.Text, 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 116f;
            // H8S: this skill's share now and at the next rank, and the total in effect (room kept for LEARN and the bonuses).
            y = DrawRankPreviewH8S(x, y, w, n, book, r.yMax - 362f);
            if (n.Prereqs.Length > 0)
            {
                List<string> names = new List<string>();
                foreach (string p in n.Prereqs)
                {
                    SkillNode pn = SkillDb.Get(p);
                    if (pn != null) names.Add((book.Has(p) ? "" : "<color=#ff7a6a>") + pn.Name + (book.Has(p) ? "" : "</color>"));
                }
                UIKit.Text(new Rect(x, y, w, 24f), "Needs: " + string.Join(", ", names.ToArray()), 16, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 28f;
            }
            LearnCheck c = book.Check(n.Id);
            string state = rank >= n.MaxRank ? "Fully learned." : (c.Ok ? "Ready to learn." : c.Detail);
            UIKit.Text(new Rect(x, y, w, 48f), state, 17, rank >= n.MaxRank ? tc : (c.Ok ? ProgressionUI.Good : ProgressionUI.Bad), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 52f;
            if (rank < n.MaxRank)
            {
                string label = "LEARN (" + n.Cost + " point" + (n.Cost == 1 ? "" : "s") + ")";
                if (ProgressionUI.Button(new Rect(x, y, Mathf.Min(260f, w), 52f), label, false, 20, c.Ok))
                {
                    _selected = n.Id;
                    TryLearn(n, st);
                }
            }
            DrawBonuses(new Rect(x, r.yMax - 230f, w, 220f));
        }

        private struct BonusLine
        {
            public SkillStat Stat;
            public string Label;
            public bool Flat;
        }

        private static readonly BonusLine[] Lines =
        {
            new BonusLine { Stat = SkillStat.MeleeDamage, Label = "Melee damage" },
            new BonusLine { Stat = SkillStat.GunDamage, Label = "Gun damage" },
            new BonusLine { Stat = SkillStat.PowerDamage, Label = "Power damage" },
            new BonusLine { Stat = SkillStat.CreatureDamage, Label = "Damage to creatures" },
            new BonusLine { Stat = SkillStat.CritChance, Label = "Critical chance", Flat = true },
            new BonusLine { Stat = SkillStat.DamageTaken, Label = "Damage taken" },
            new BonusLine { Stat = SkillStat.HealthMax, Label = "Max health", Flat = true },
            new BonusLine { Stat = SkillStat.StaminaMax, Label = "Max stamina", Flat = true },
            new BonusLine { Stat = SkillStat.EnergyMax, Label = "Max energy", Flat = true },
            new BonusLine { Stat = SkillStat.TonicCharges, Label = "Tonic charges", Flat = true },
            new BonusLine { Stat = SkillStat.VeilDrain, Label = "Veil drain" },
            new BonusLine { Stat = SkillStat.Discount, Label = "Discount", Flat = true }
        };

        /// <summary>The bonuses in effect right now (skills, Forge upgrades, brews and guild perks together).</summary>
        private static void DrawBonuses(Rect r)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 22f), "IN EFFECT NOW (skills, Forge, guilds)", 14, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = r.y + 22f;
            int shown = 0;
            foreach (BonusLine l in Lines)
            {
                float v = SkillMods.Get(l.Stat);
                float b = SkillModValues.BaseOf(l.Stat);
                if (Mathf.Abs(v - b) < 0.001f) continue;
                string val;
                if (l.Stat == SkillStat.CritChance || l.Stat == SkillStat.Discount) val = "+" + ProgressionUI.Pct(v);
                else if (l.Flat) val = "+" + Mathf.RoundToInt(v);
                else val = (v > b ? "+" : "-") + ProgressionUI.Pct(Mathf.Abs(v - b));
                UIKit.Text(new Rect(r.x, y, r.width * 0.65f, 20f), l.Label, 15, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                UIKit.Text(new Rect(r.x, y, r.width, 20f), val, 15, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                y += 19f;
                if (++shown >= 10) break;
            }
            if (shown == 0) UIKit.Text(new Rect(r.x, y, r.width, 20f), "Nothing yet.", 15, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperLeft);
        }
    }
}
