using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the pause MAP layer: every campaign giver with work, an objective or a reason to wait drawn with
    /// its state glyph (people under the city are tagged "below"); clicking a glyph, or choosing a person in the LEADS
    /// list (reachable with the gamepad), opens a card with name, task, Main or Side, suggested difficulty, rewards,
    /// floor or region and entrance, with TRACK and CLOSE. The route's stair or descent points are labeled too. A click
    /// the layer handles never drops a waypoint.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private CastSlot _selected;
        private readonly List<CastSlot> _mapList = new List<CastSlot>();

        private void InitUi()
        {
            MapOverlays.Register("guidance_04e", 20, DrawMapLayer);
            HudOverlays.Register("guidance_04e", DrawHud);
            MenuPages.Register(new MenuPage { Title = "STATUS", Order = 105, Draw = DrawStatus, Visible = () => Game.Player != null });
        }

        private void DrawMapLayer(MapContext ctx)
        {
            Event e = Event.current;
            _mapList.Clear();
            CastSlot hover = null;
            for (int i = 0; i < _slots.Count; i++)
            {
                CastSlot s = _slots[i];
                if (!s.PostBound) continue;
                if (!GuidanceRules04E.MapVisible(s.State) && s != _selected) continue;
                _mapList.Add(s);
                Vector2 sp = ctx.ToScreen(s.Post);
                if (sp.x < -20f || sp.y < -20f || sp.x > ctx.Local.width + 20f || sp.y > ctx.Local.height + 20f) continue;
                MarkerShape04E shape = GuidanceRules04E.ShapeOf(s.State);
                if (shape == MarkerShape04E.None) shape = MarkerShape04E.DoneDot;
                float size = s == _selected ? 34f : 26f;
                GuidanceGlyphs04E.Draw(ctx.Area.position, sp, shape, GuidanceRules04E.ColorOf(s.State), size);
                if (s.Post.y < WorldBounds.LayerSplitY)
                    UIKit.Text(new Rect(sp.x - 40f, sp.y + 15f, 80f, 18f), "below", 14, new Color(1f, 0.75f, 0.45f), TextAnchor.UpperCenter, FontStyle.Bold);
                bool over = (ctx.Mouse - sp).sqrMagnitude < 16f * 16f;
                if (over) hover = s;
                if (over && ctx.Clicked && !ctx.ClickUsed)
                {
                    _selected = s;
                    ctx.ClickUsed = true;
                }
            }
            if (hover != null)
            {
                Vector2 hp = ctx.ToScreen(hover.Post);
                string label = hover.Def.Name + ": " + GuidanceRules04E.LabelOf(hover.State) + (hover.Offer != null ? " (" + hover.Offer.Title + ")" : "");
                UIKit.Text(new Rect(hp.x + 20f, hp.y - 14f, 560f, 28f), label, 19, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            DrawRouteTransitions(ctx);
            DrawLeadsPanel(ctx, e);
            if (_selected != null) DrawGiverCard(ctx, e, _selected);
        }

        /// <summary>Labels where the route changes floor: stairs up or down, the descent into the Underheart.</summary>
        private static void DrawRouteTransitions(MapContext ctx)
        {
            Gps gps = Game.Gps;
            if (gps == null || gps.Route.Count < 2) return;
            int shown = 0;
            for (int i = 1; i < gps.Route.Count && shown < 3; i++)
            {
                float dy = gps.Route[i].y - gps.Route[i - 1].y;
                if (Mathf.Abs(dy) < 2.5f) continue;
                Vector2 sp = ctx.ToScreen(gps.Route[i - 1]);
                int v = dy > 0f ? 1 : -1;
                UIKit.Tex(new Rect(sp.x - 13f, sp.y - 13f, 26f, 26f), UIKit.Circle, new Color(0f, 0f, 0f, 0.65f));
                Matrix4x4 m = GUI.matrix;
                if (v < 0) UIKit.RotateAround(ctx.Area.position + sp, 180f);
                UIKit.Tex(new Rect(sp.x - 9f, sp.y - 9f, 18f, 18f), UIKit.Arrow, UIKit.Gold);
                GUI.matrix = m;
                UIKit.Text(new Rect(sp.x - 60f, sp.y + 14f, 120f, 18f), v < 0 ? "DOWN HERE" : "UP HERE", 14, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
                shown++;
            }
        }

        /// <summary>The LEADS list: one button per giver with something to say, so a gamepad can pick a card too.</summary>
        private void DrawLeadsPanel(MapContext ctx, Event e)
        {
            int n = 0;
            foreach (CastSlot s in _mapList) if (s.State != GiverState.Completed && s.State != GiverState.NoWork) n++;
            if (n == 0) return;
            n = Mathf.Min(n, 6);
            float w = 450f, rowH = 46f;
            Rect panel = new Rect(ctx.Local.width - w - 20f, ctx.Local.height - 60f - n * (rowH + 4f) - 34f, w, n * (rowH + 4f) + 40f);
            UIKit.Rect(panel, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Text(new Rect(panel.x + 12f, panel.y + 6f, w - 24f, 26f), "LEADS (" + ModuleStatus.Label + ")", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = panel.y + 34f;
            int drawn = 0;
            foreach (CastSlot s in _mapList)
            {
                if (s.State == GiverState.Completed || s.State == GiverState.NoWork) continue;
                if (drawn++ >= 6) break;
                Rect br = new Rect(panel.x + 8f, y, w - 16f, rowH);
                string text = "      " + s.Def.Name + "  <size=15><color=#cccccc>" + GuidanceRules04E.LabelOf(s.State) + "</color></size>";
                if (UIKit.Button(br, text, s == _selected, 18)) _selected = s;
                GuidanceGlyphs04E.Draw(ctx.Area.position, new Vector2(br.x + 22f, br.y + rowH * 0.5f), GuidanceRules04E.ShapeOf(s.State), GuidanceRules04E.ColorOf(s.State), 22f);
                y += rowH + 4f;
            }
            if (ctx.Clicked && panel.Contains(ctx.Mouse)) ctx.ClickUsed = true;
            if (e.type == EventType.MouseDown && panel.Contains(e.mousePosition)) e.Use();
        }

        private void DrawGiverCard(MapContext ctx, Event e, CastSlot s)
        {
            QuestDef q = CardQuest(s);
            Rect r = new Rect(ctx.Local.width - 490f, 64f, 470f, 440f);
            UIKit.Rect(r, UIKit.Panel);
            Color c = GuidanceRules04E.ColorOf(s.State);
            UIKit.Frame(r, c, 2f);
            float x = r.x + 18f, y = r.y + 14f, w = r.width - 36f;
            UIKit.Text(new Rect(x, y, w, 32f), s.Def.Name, 28, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            UIKit.Text(new Rect(x, y, w, 22f), s.Def.Role + (s.Terminal != null ? " (message terminal; not present)" : ""), 17, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft);
            y += 30f;
            MarkerShape04E shape = GuidanceRules04E.ShapeOf(s.State);
            GuidanceGlyphs04E.Draw(ctx.Area.position, new Vector2(x + 13f, y + 13f), shape == MarkerShape04E.None ? MarkerShape04E.DoneDot : shape, c, 24f);
            string stateLine = GuidanceRules04E.LabelOf(s.State) + (string.IsNullOrEmpty(s.StateReason) ? "" : ": " + s.StateReason);
            UIKit.Text(new Rect(x + 34f, y, w - 34f, 48f), stateLine, 18, c, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 52f;
            if (q != null)
            {
                OfferView04E v = OfferView(s, q);
                StringBuilder sb = new StringBuilder();
                sb.Append("<b>").Append(q.Title).Append("</b>\n");
                sb.Append(string.IsNullOrEmpty(v.Kind) ? "Quest" : v.Kind + " quest").Append(", suggested difficulty ").Append(v.Difficulty).Append(" of 5 (")
                  .Append(QuestMeta04E.DifficultyName(v.Difficulty)).Append(")\n");
                sb.Append("Where: ").Append(GuidanceRules04E.WhereLine(string.IsNullOrEmpty(v.Location) ? s.PostName : v.Location, v.Floor)).Append('\n');
                if (!string.IsNullOrEmpty(v.Entrance)) sb.Append("Entrance: ").Append(v.Entrance).Append('\n');
                sb.Append("Rewards: ").Append(string.IsNullOrEmpty(v.Rewards) ? "see the journal" : v.Rewards);
                UIKit.Text(new Rect(x, y, w, 200f), sb.ToString(), 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
            else UIKit.Text(new Rect(x, y, w, 60f), "Found at " + s.PostName + ".", 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float by = r.yMax - 58f;
            if (UIKit.Button(new Rect(x, by, 200f, 44f), "TRACK", false, 20)) TrackFromCard(s, q);
            if (UIKit.Button(new Rect(r.xMax - 18f - 160f, by, 160f, 44f), "CLOSE", false, 20)) _selected = null;
            if (ctx.Clicked && r.Contains(ctx.Mouse)) ctx.ClickUsed = true;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) e.Use();
        }

        /// <summary>The quest a giver's card describes: the running one at this giver, the offer, else the next locked one.</summary>
        private QuestDef CardQuest(CastSlot s)
        {
            QuestBook book = Game.Quests;
            if (book == null) return null;
            List<QuestDef> list = QuestsOf(s.Def.Id);
            if (list == null) return null;
            if (book.Running != null && list.Contains(book.Running.Quest)) return book.Running.Quest;
            if (s.Offer != null) return s.Offer;
            foreach (QuestDef q in list) if (!book.IsDone(q.Id)) return q;
            return list.Count > 0 ? list[list.Count - 1] : null;
        }

        private void TrackFromCard(CastSlot s, QuestDef q)
        {
            QuestBook book = Game.Quests;
            if (q != null && book != null && !book.IsDone(q.Id) && (book.ActiveId == q.Id || book.WhyLocked(q, null) == null))
            {
                TrackQuestLead(q.Id, true);
                return;
            }
            if (s.PostBound) TrackPlace(s.Post, s.Def.Name + " at " + s.PostName);
        }
    }
}
