using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The FACTIONS page (pause menu tab) and the reputation toasts on the HUD. The page lists every faction with its
    /// colour, kind, rank and a small bar; the panel on the right shows the selected one in full: the rank bar with its
    /// bands, what they offer, what you have unlocked and what the next rank brings.
    /// </summary>
    public static class FactionsPage
    {
        private static string _selected;

        public static void ResetStatics()
        {
            _selected = null;
        }

        public static void Draw(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            FactionSystem fs = Game.Factions;
            if (fs == null)
            {
                UIKit.Text(new Rect(r.x + 40f, r.y + 40f, r.width - 80f, 40f), "Factions are not available.", 24, Color.white, TextAnchor.UpperLeft);
                return;
            }
            float pad = 20f;
            UIKit.Text(new Rect(r.x + pad, r.y + 10f, 600f, 40f), "FACTIONS", 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 220f, r.y + 20f, r.width - 240f, 24f), "Reputation runs from -100 (Hunted) to 100 (Family). Gaining with a faction costs half as much with its rival.", 16, new Color(0.82f, 0.82f, 0.85f), TextAnchor.UpperLeft);
            float listW = Mathf.Min(560f, r.width * 0.36f);
            Rect list = new Rect(r.x + pad, r.y + 60f, listW, r.height - 60f - pad);
            Rect detail = new Rect(list.xMax + pad, list.y, r.xMax - list.xMax - pad * 2f, list.height);
            float rowH = Mathf.Min(54f, list.height / FactionDb.All.Length);
            FactionDef hot = null;
            for (int i = 0; i < FactionDb.All.Length; i++)
            {
                FactionDef f = FactionDb.All[i];
                if (DrawRow(new Rect(list.x, list.y + i * rowH, list.width, rowH - 4f), f, fs)) hot = f;
            }
            FactionDef show = hot ?? FactionDb.Get(_selected) ?? FactionDb.All[0];
            DrawDetail(detail, show, fs);
        }

        private static string KindName(FactionKind k)
        {
            switch (k)
            {
                case FactionKind.Syndicate: return "Syndicate";
                case FactionKind.Guild: return "Guild";
                case FactionKind.Corporate: return "Company";
                default: return "Cult";
            }
        }

        private static bool DrawRow(Rect r, FactionDef f, FactionSystem fs)
        {
            bool hot, focused;
            if (ProgressionUI.Hotspot(r, out hot, out focused)) _selected = f.Id;
            bool selected = _selected == f.Id;
            int rep = fs.Rep(f.Id);
            FactionRank rank = FactionRules.RankOf(rep);
            UIKit.Rect(r, selected ? new Color(1f, 1f, 1f, 0.14f) : (hot ? new Color(1f, 1f, 1f, 0.09f) : new Color(0f, 0f, 0f, 0.45f)));
            UIKit.Rect(new Rect(r.x, r.y, 6f, r.height), f.Color);
            UIKit.Text(new Rect(r.x + 16f, r.y + 3f, r.width * 0.6f, 24f), f.Name, 19, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 16f, r.y + 25f, r.width * 0.5f, 20f), KindName(f.Kind), 14, new Color(0.7f, 0.7f, 0.72f), TextAnchor.UpperLeft);
            UIKit.Text(new Rect(r.x, r.y + 3f, r.width - 12f, 24f), FactionRules.RankName(rank) + "  " + rep, 18, FactionRules.RankColor(rank), TextAnchor.UpperRight, FontStyle.Bold);
            DrawTrack(new Rect(r.xMax - 172f, r.y + r.height - 15f, 160f, 6f), rep, f.Color, false);
            if (selected) UIKit.Frame(r, UIKit.Gold, 2f);
            if (focused) UIKit.Frame(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), UIKit.Cyan, 3f);
            return hot;
        }

        /// <summary>The -100..100 track with its rank bands, and a marker where the value sits.</summary>
        private static void DrawTrack(Rect r, int rep, Color color, bool labels)
        {
            for (int i = 0; i <= (int)FactionRank.Family; i++)
            {
                FactionRank k = (FactionRank)i;
                float a = FactionRules.TrackFraction(Mathf.Max(FactionRules.MinRep, FactionRules.RankMin(k) - (i == 0 ? 0 : 1)));
                float b = FactionRules.TrackFraction(FactionRules.RankMax(k));
                if (i == 0) a = 0f;
                Color c = FactionRules.RankColor(k);
                Rect band = new Rect(r.x + r.width * a, r.y, r.width * (b - a), r.height);
                UIKit.Rect(band, new Color(c.r, c.g, c.b, 0.28f));
                if (labels) UIKit.Text(new Rect(band.x, r.yMax + 4f, band.width, 18f), FactionRules.RankName(k), 13, c, TextAnchor.UpperCenter);
            }
            float t = FactionRules.TrackFraction(rep);
            float mid = FactionRules.TrackFraction(0);
            float x0 = r.x + r.width * Mathf.Min(mid, t), x1 = r.x + r.width * Mathf.Max(mid, t);
            UIKit.Rect(new Rect(x0, r.y + r.height * 0.25f, Mathf.Max(1f, x1 - x0), r.height * 0.5f), color);
            UIKit.Rect(new Rect(r.x + r.width * t - 2f, r.y - 3f, 4f, r.height + 6f), Color.white);
        }

        private static void DrawDetail(Rect r, FactionDef f, FactionSystem fs)
        {
            UIKit.Rect(r, new Color(1f, 1f, 1f, 0.04f));
            UIKit.Rect(new Rect(r.x, r.y, r.width, 5f), f.Color);
            float x = r.x + 24f, w = r.width - 48f, y = r.y + 16f;
            int rep = fs.Rep(f.Id);
            FactionRank rank = FactionRules.RankOf(rep);
            UIKit.Text(new Rect(x, y, w, 40f), f.Name, 34, f.Color, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 4f, w, 34f), FactionRules.RankName(rank).ToUpperInvariant() + "  " + rep, 28, FactionRules.RankColor(rank), TextAnchor.UpperRight, FontStyle.Bold);
            y += 44f;
            FactionDef rival = FactionDb.Get(f.Rival);
            string facts = KindName(f.Kind) + " of " + CityLayout.DistrictName(f.Home) + (rival != null ? ".   Rival: " + rival.Name : "") + (string.IsNullOrEmpty(f.Contact) ? "" : ".   Contact: " + f.Contact) + ".";
            UIKit.Text(new Rect(x, y, w, 24f), facts, 17, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 26f;
            UIKit.Text(new Rect(x, y, w, 26f), f.Blurb, 18, new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
            y += 38f;
            DrawTrack(new Rect(x, y, w, 16f), rep, f.Color, true);
            y += 44f;
            if (f.GangId >= 0 && f.GangId < Gangs.Heat.Length)
            {
                float heat = Gangs.Heat[f.GangId];
                string h = "Gang heat " + ProgressionUI.Pct(heat) + (heat > 0.6f ? "  (they attack on sight)" : "");
                UIKit.Text(new Rect(x, y, w, 22f), h, 16, heat > 0.6f ? ProgressionUI.Bad : new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Bold);
                y += 26f;
            }
            y = Section(x, y, w, "OFFERS", new List<string> { f.Offers }, new Color(0.9f, 0.9f, 0.9f));
            y = Section(x, y, w, "YOU HAVE NOW (" + FactionRules.RankName(rank) + ")", FactionRules.Perks(f, rank), FactionRules.RankColor(rank));
            if (rank < FactionRank.Family)
            {
                FactionRank next = FactionRules.Next(rank);
                int at = FactionRules.RankMin(next);
                y = Section(x, y, w, "NEXT: " + FactionRules.RankName(next).ToUpperInvariant() + " AT " + at + " (" + (at - rep) + " to go)", FactionRules.Perks(f, next), new Color(0.75f, 0.75f, 0.78f));
            }
            else UIKit.Text(new Rect(x, y, w, 24f), "You are family. Nothing more to earn here.", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 6f;
            if (y < r.yMax - 70f)
            {
                string how = HowToEarn(f);
                UIKit.Text(new Rect(x, r.yMax - 66f, w, 56f), how, 15, new Color(0.7f, 0.75f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
        }

        private static float Section(float x, float y, float w, string title, List<string> lines, Color color)
        {
            UIKit.Text(new Rect(x, y, w, 22f), title, 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            foreach (string l in lines)
            {
                UIKit.Text(new Rect(x + 12f, y, w - 12f, 44f), "- " + l, 17, color, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += UIKit.Measure(l, 17).x > w - 30f ? 42f : 23f;
            }
            return y + 8f;
        }

        private static string HowToEarn(FactionDef f)
        {
            switch (f.Id)
            {
                case "forgewrights": return "Earn it by forging (gear, infusions, upgrades) and by selling materials at Dante Rook's forge in Old Silver.";
                case "menders": return "Earn it by donating at Mara Solane's corner beside her street clinic in Sunken Row.";
                case "frost_wardens": return "Earn it by hunting Frostline creatures, forging at the outpost and buying the Wardens' gear.";
                case "frequency_order": return "Earn it by hunting in the Inner World and by offering rift shards to Iris Moon's people in Glass Heights.";
                case "hollow_cult": return "Killing Hollows angers the cult. Its favour comes only through its own dark work.";
                case "solane_holdings": return "Quests and jobs for the family move this one.";
                default:
                    FactionDef rival = FactionDb.Get(f.Rival);
                    return "Killing their members costs reputation (not in missions, and not once they are at war with you); taking down " + (rival != null ? rival.Name : "their rivals") + " earns a little. Jobs and quests do the rest. A grudge fades every hour, by at least " + FactionRules.DailyForgiveness + " a day and faster when it runs deep.";
            }
        }

        // ------------------------------------------------------------------ HUD toasts

        /// <summary>Reputation changes, top left under the vitals: "+3 Frost Wardens  (24)  killed a Frost Stalker".</summary>
        public static void DrawToasts(float W, float H)
        {
            FactionSystem fs = Game.Factions;
            if (fs == null || fs.Toasts.Count == 0) return;
            float x = 40f, y = 166f;
            IList<RepToast> toasts = fs.Toasts;
            for (int i = 0; i < toasts.Count; i++)
            {
                RepToast t = toasts[i];
                FactionDef f = FactionDb.Get(t.FactionId);
                if (f == null || t.Delta == 0) continue;
                // The text is built once per change, not on every OnGUI event.
                if (t.TextVersion != t.Version) BuildToastText(t, f);
                float a = Mathf.Clamp01(t.Time * 1.6f);
                Rect box = new Rect(x, y, 480f, t.RankChanged ? 50f : 32f);
                UIKit.Rect(box, new Color(0f, 0f, 0f, 0.62f * a));
                UIKit.Rect(new Rect(box.x, box.y, 5f, box.height), new Color(f.Color.r, f.Color.g, f.Color.b, a));
                Color dc = t.Delta > 0 ? ProgressionUI.Good : ProgressionUI.Bad;
                UIKit.Text(new Rect(box.x + 14f, box.y + 4f, 60f, 24f), t.DeltaText, 20, new Color(dc.r, dc.g, dc.b, a), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(box.x + 70f, box.y + 5f, box.width - 80f, 24f), t.LineText, 18, new Color(1f, 1f, 1f, a), TextAnchor.UpperLeft, FontStyle.Bold);
                if (t.RankChanged)
                {
                    Color rc = FactionRules.RankColor(FactionRules.RankOf(t.Now));
                    UIKit.Text(new Rect(box.x + 70f, box.y + 27f, box.width - 80f, 22f), t.RankText, 15, new Color(rc.r, rc.g, rc.b, a), TextAnchor.UpperLeft, FontStyle.Bold);
                }
                y += box.height + 4f;
            }
        }

        private static void BuildToastText(RepToast t, FactionDef f)
        {
            t.TextVersion = t.Version;
            t.DeltaText = (t.Delta > 0 ? "+" : "") + t.Delta;
            t.LineText = f.Name + "  <color=#bbbbbb>(" + t.Now + ")</color>" + (string.IsNullOrEmpty(t.Reason) ? "" : "  <size=15><color=#aaaaaa>" + t.Reason + "</color></size>");
            t.RankText = t.RankChanged ? "Now " + FactionRules.RankName(FactionRules.RankOf(t.Now)).ToUpperInvariant() + ". See Pause menu > FACTIONS." : null;
        }
    }
}
