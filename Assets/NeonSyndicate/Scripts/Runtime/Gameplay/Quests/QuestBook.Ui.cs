using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// QuestBook, part three: the JOURNAL pause-menu page (active quest, available quests with waypoints, completed
    /// quests by arc, the contract boards, recent world events), the quest tracker line under the HUD objective and
    /// the Quests debug tab. Every control is a UIKit button, so the mouse, the arrow keys and a controller all work.
    /// </summary>
    public sealed partial class QuestBook
    {
        private enum JournalTab { Active, Available, Completed, Contracts, Events }

        private static readonly string[] JournalTabNames = { "ACTIVE", "AVAILABLE", "COMPLETED", "CONTRACTS", "WORLD EVENTS" };
        private const int PerPage = 8;

        private JournalTab _tab = JournalTab.Active;
        private int _page;
        private string _selQuest = "";
        private string _selArc = "";
        private int _selBoard;
        private int _debugPage;
        private string _debugStatus = "";

        private void RegisterUi()
        {
            MenuPages.Register(new MenuPage { Title = "JOURNAL", Order = 20, Draw = DrawJournal, Visible = () => Game.Player != null });
            DebugPages.Register(new MenuPage { Title = "Quests", Order = 30, Draw = DrawDebug });
            HudOverlays.Register("quest_tracker", DrawTracker);
        }

        private static string Hex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        // ================================================================== 04E campaign presentation

        /// <summary>The arc name shown for a quest: "Underheart and Bonds" for the 04E campaign, else the guild's name.</summary>
        private static string ArcName(QuestDef q)
        {
            if (QuestMeta04E.IsCampaign(q)) return "Underheart and Bonds";
            return QuestWorld.FactionName(q != null ? q.Arc : "");
        }

        private static string ArcName(string arc)
        {
            return arc == QuestIds04E.Campaign ? "Underheart and Bonds" : QuestWorld.FactionName(arc);
        }

        private static Color ArcColor(QuestDef q)
        {
            if (QuestMeta04E.IsCampaign(q)) return CampaignColor;
            return QuestWorld.FactionColor(q != null ? q.Arc : "");
        }

        private static Color ArcColor(string arc)
        {
            return arc == QuestIds04E.Campaign ? CampaignColor : QuestWorld.FactionColor(arc);
        }

        private static readonly Color CampaignColor = new Color(1f, 0.62f, 0.28f);

        /// <summary>The giver state shared with the overhead marker and the map (None when guidance is not running).</summary>
        private static GiverState CampaignState(QuestDef q, out string reason)
        {
            reason = "";
            if (Game.Campaign == null || !QuestMeta04E.IsCampaign(q)) return GiverState.None;
            return Game.Campaign.StateFor(q, out reason);
        }

        /// <summary>The detail panel for an Underheart quest: kind, act, difficulty, the shared giver state, where (with
        /// floor and entrance), risk and rewards, and the Track and Resume buttons. Returns the y below it.</summary>
        private void DrawCampaignDetail(Rect r, QuestDef q)
        {
            Color c = CampaignColor;
            float y = r.y;
            UIKit.Rect(new Rect(r.x, y, r.width, 4f), c);
            y += 12f;
            UIKit.Text(new Rect(r.x, y, r.width, 36f), q.Title, 30, c, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 42f;
            int act = QuestMeta04E.Act(q);
            int difficulty = QuestMeta04E.Difficulty(q);
            string kind = QuestMeta04E.Kind(q);
            UIKit.Text(new Rect(r.x, y, r.width, 26f), "Underheart and Bonds   " + (kind.Length > 0 ? kind + " quest" : "Quest") + (act > 0 ? ", Act " + act : "")
                + "   Suggested difficulty " + difficulty + " of 5 (" + QuestMeta04E.DifficultyName(difficulty) + ")", 19, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 32f;
            string reason;
            GiverState state = CampaignState(q, out reason);
            Color sc = GuidanceRules04E.ColorOf(state);
            MarkerShape04E shape = GuidanceRules04E.ShapeOf(state);
            GuidanceGlyphs04E.Draw(Vector2.zero, new Vector2(r.x + 13f, y + 13f), shape == MarkerShape04E.None ? MarkerShape04E.DoneDot : shape, sc, 24f);
            UIKit.Text(new Rect(r.x + 34f, y, r.width - 34f, 50f), GuidanceRules04E.LabelOf(state) + (string.IsNullOrEmpty(reason) ? "" : ": " + reason), 19, sc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 52f;
            OfferView04E v = new OfferView04E { Purpose = q.Summary };
            if (Game.Campaign != null) Game.Campaign.DescribeQuest(q, out v);
            UIKit.Text(new Rect(r.x, y, r.width, 26f), "From " + (Game.Campaign != null ? Game.Campaign.GiverPlace(q) : GiverName(q)), 19, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 32f;
            v.Note = "";
            v.Giver = "";
            UIKit.Text(new Rect(r.x, y, r.width, 230f), GuidanceRules04E.OfferText(v), 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            y += 236f;
            if (_completed.Contains(q.Id))
            {
                UIKit.Text(new Rect(r.x, y, r.width, 60f), "<color=#60ff80>Completed.</color> " + q.Done, 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                return;
            }
            bool running = _run != null && _run.Quest == q;
            if (Game.Campaign != null && UIKit.Button(new Rect(r.x, y, 260f, 48f), running ? "Track objective" : "Track", false, 20))
            {
                if (running && Game.Gps != null && Game.Gps.HasMissionTarget) Waypoint(Game.Gps.MissionTarget, "the objective");
                else Game.Campaign.TrackLead(q.Id);
            }
            bool canResume = !running && q.Id == _active && QuestMeta04E.ResumeSupported && QuestMeta04E.CanResume(q);
            if (canResume && UIKit.Button(new Rect(r.x + 280f, y, 320f, 48f), "Resume from checkpoint", false, 19))
            {
                // Resumed on the next frame after the menu has closed (never under an open menu).
                _pendingResume = q;
                if (Game.Menus != null) Game.Menus.Close();
            }
        }

        /// <summary>A checkpoint resume chosen in the journal, run by LateUpdate once the menu is closed.</summary>
        private QuestDef _pendingResume;

        private void RunPendingResume()
        {
            if (_pendingResume == null || !Game.IsPlaying || (Game.Menus != null && Game.Menus.IsOpen)) return;
            QuestDef q = _pendingResume;
            _pendingResume = null;
            if (!CanStartNow(true)) return;
            if (!QuestMeta04E.Resume(q) && Game.Hud != null) Game.Hud.Notify("The checkpoint cannot resume right now. Talk to " + GiverName(q) + " instead.", 4f);
        }

        // ================================================================== HUD tracker

        private QuestRun _trackerRun;
        private string _trackerProgress;
        private string _trackerLine = "";

        private void DrawTracker(float W, float H)
        {
            QuestRun r = _run;
            if (r == null || Game.Missions == null || Game.Missions.Active == null || Game.Missions.Active != r.C) return;
            // The line is rebuilt only when the run or its progress text changes (ProgressText itself is cached).
            string progress = r.ProgressText();
            if (r != _trackerRun || !ReferenceEquals(progress, _trackerProgress))
            {
                _trackerRun = r;
                _trackerProgress = progress;
                Color c = ArcColor(r.Quest);
                string kind = r.Contract ? " CONTRACT" : "";
                _trackerLine = "<color=" + Hex(c) + "><b>" + ArcName(r.Quest).ToUpperInvariant() + kind + "</b></color>   " + r.Quest.Title + (progress.Length > 0 ? "   <color=#c8c8c8>" + progress + "</color>" : "");
            }
            UIKit.Text(new Rect(W * 0.15f, H - 76f, W * 0.7f, 26f), _trackerLine, 19, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter);
        }

        // ================================================================== journal

        private void DrawJournal(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            float x = r.x + 24f, y = r.y + 16f;
            float tw = Mathf.Min(250f, (r.width - 48f) / JournalTabNames.Length - 10f);
            for (int i = 0; i < JournalTabNames.Length; i++)
            {
                if (UIKit.Button(new Rect(x + i * (tw + 10f), y, tw, 44f), JournalTabNames[i], (int)_tab == i, 19))
                {
                    _tab = (JournalTab)i;
                    _page = 0;
                }
            }
            if (UnclaimedSkillPoints > 0)
                UIKit.Text(new Rect(r.xMax - 420f, y + 52f, 396f, 24f), UnclaimedSkillPoints + " skill points banked for the skill trees", 17, UIKit.Gold, TextAnchor.UpperRight);
            Rect body = new Rect(r.x + 24f, y + 78f, r.width - 48f, r.yMax - y - 94f);
            switch (_tab)
            {
                case JournalTab.Active: DrawActive(body); break;
                case JournalTab.Available: DrawAvailable(body); break;
                case JournalTab.Completed: DrawCompleted(body); break;
                case JournalTab.Contracts: DrawContracts(body); break;
                case JournalTab.Events: DrawEvents(body); break;
            }
        }

        private void Waypoint(Vector3 p, string what)
        {
            if (Game.Gps == null) return;
            Game.Gps.SetWaypoint(p);
            if (Game.Hud != null) Game.Hud.Notify("Waypoint set: " + what + ".", 2.5f);
        }

        private static string Where(Vector3 p)
        {
            return QuestPlaces.PlaceName(Game.Layout, p) + ", " + Mathf.RoundToInt(MathUtil.FlatDistance(Game.PlayerPosition, p)) + " m " + QuestWorld.Direction(Game.PlayerPosition, p);
        }

        private void DrawActive(Rect r)
        {
            QuestRun run = _run;
            float y = r.y;
            if (run != null)
            {
                QuestDef q = run.Quest;
                Color c = ArcColor(q);
                UIKit.Text(new Rect(r.x, y, r.width, 40f), q.Title, 32, c, TextAnchor.UpperLeft, FontStyle.Bold);
                y += 44f;
                UIKit.Text(new Rect(r.x, y, r.width, 26f), ArcName(q) + (run.Contract ? " contract" : " quest") + "   " + run.ProgressText(), 20, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                y += 40f;
                UIKit.Text(new Rect(r.x, y, r.width, 90f), "<b>Now:</b> " + (string.IsNullOrEmpty(run.C.ObjectiveText) ? "..." : run.C.ObjectiveText), 24, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 96f;
                UIKit.Text(new Rect(r.x, y, r.width * 0.7f, 120f), q.Summary, 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 128f;
                UIKit.Text(new Rect(r.x, y, r.width, 26f), "Reward: " + QuestLogic.RewardText(q.Reward), 20, UIKit.Gold, TextAnchor.UpperLeft);
                y += 44f;
                if (Game.Gps != null && Game.Gps.HasMissionTarget && UIKit.Button(new Rect(r.x, y, 320f, 48f), "Waypoint on the objective", false, 19)) Waypoint(Game.Gps.MissionTarget, "the objective");
                if (UIKit.Button(new Rect(r.x + 340f, y, 260f, 48f), run.Contract ? "Abandon contract" : "Abandon quest", false, 19)) Abandon();
                return;
            }
            QuestDef pending = Find(_active);
            if (pending != null && QuestMeta04E.IsCampaign(pending))
            {
                // A campaign quest: the shared detail panel with Track (and Resume from a checkpoint when it has one).
                DrawCampaignDetail(new Rect(r.x, y, r.width * 0.75f, r.height), pending);
                if (UIKit.Button(new Rect(r.x + r.width * 0.75f - 260f, r.yMax - 56f, 260f, 48f), "Abandon quest", false, 19)) Abandon();
                return;
            }
            if (pending != null)
            {
                Vector3 start = StartPosition(pending);
                UIKit.Text(new Rect(r.x, y, r.width, 40f), pending.Title, 32, QuestWorld.FactionColor(pending.Arc), TextAnchor.UpperLeft, FontStyle.Bold);
                y += 48f;
                string fail = string.IsNullOrEmpty(_lastFail) ? "" : "\nLast attempt: " + _lastFail;
                UIKit.Text(new Rect(r.x, y, r.width * 0.75f, 110f), "Accepted but not finished. Return to " + GiverName(pending) + " (" + Where(start) + ") to start it again from the beginning." + fail, 22, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 120f;
                UIKit.Text(new Rect(r.x, y, r.width * 0.75f, 110f), pending.Summary, 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 118f;
                if (UIKit.Button(new Rect(r.x, y, 320f, 48f), "Waypoint to " + GiverName(pending), false, 18)) Waypoint(start, GiverName(pending));
                if (UIKit.Button(new Rect(r.x + 340f, y, 260f, 48f), "Abandon quest", false, 19)) Abandon();
                return;
            }
            UIKit.Text(new Rect(r.x, y, r.width * 0.8f, 120f), "No quest in progress.\n\nGuild givers glow in their guild's colour and show a Q on the map when they have work. The AVAILABLE tab lists them all, with a waypoint button for each.", 22, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
        }

        /// <summary>A paged column of buttons; returns the index clicked, or -1.</summary>
        private int PagedList(Rect r, List<string> labels, List<bool> selected, ref int page)
        {
            int pages = Mathf.Max(1, (labels.Count + PerPage - 1) / PerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            int clicked = -1;
            float y = r.y;
            for (int i = page * PerPage; i < Mathf.Min(labels.Count, (page + 1) * PerPage); i++)
            {
                if (UIKit.Button(new Rect(r.x, y, r.width, 56f), labels[i], selected[i], 18)) clicked = i;
                y += 62f;
            }
            if (pages > 1)
            {
                float by = r.y + PerPage * 62f + 4f;
                if (UIKit.Button(new Rect(r.x, by, r.width * 0.48f, 42f), "Prev page", false, 18)) page = (page + pages - 1) % pages;
                if (UIKit.Button(new Rect(r.x + r.width * 0.52f, by, r.width * 0.48f, 42f), "Next page", false, 18)) page = (page + 1) % pages;
            }
            return clicked;
        }

        private void DrawAvailable(Rect r)
        {
            QuestProgress p = Progress();
            List<QuestDef> list = new List<QuestDef>();
            List<string> labels = new List<string>();
            List<bool> sel = new List<bool>();
            foreach (QuestDef q in Quests)
            {
                bool open = WhyLocked(q, p) == null;
                // The next locked quest of each arc is listed too (greyed), so the player knows what comes next.
                bool nextLocked = !open && !_completed.Contains(q.Id) && FirstUnfinishedOfArc(q.Arc) == q;
                if (!open && !nextLocked && q.Id != _active) continue;
                list.Add(q);
                string tag = q.Id == _active ? "  <size=14>[restart]</size>" : (open ? "" : "  <size=14><color=#999999>[locked]</color></size>");
                if (QuestMeta04E.IsCampaign(q))
                {
                    // Room on the left for the giver-state glyph (drawn after the list, the same shape as the marker).
                    string reason;
                    GiverState gs = CampaignState(q, out reason);
                    labels.Add("      <color=" + Hex(CampaignColor) + ">" + (QuestMeta04E.IsMain(q) ? "Main" : "Side") + "</color>  " + q.Title
                        + "  <size=14><color=" + Hex(GuidanceRules04E.ColorOf(gs)) + ">" + GuidanceRules04E.LabelOf(gs) + "</color></size>");
                }
                else labels.Add("<color=" + Hex(QuestWorld.FactionColor(q.Arc)) + ">" + QuestWorld.FactionName(q.Arc) + "</color>  " + q.Title + tag);
                sel.Add(q.Id == _selQuest);
            }
            Rect left = new Rect(r.x, r.y, Mathf.Min(640f, r.width * 0.42f), r.height);
            if (list.Count == 0)
            {
                UIKit.Text(new Rect(r.x, r.y, r.width * 0.8f, 100f), "Nobody has work for you yet. Guild arcs open after the first story mission, once the Third Eye is yours (the Frost Wardens also want you to reach their outpost in the Frostline).", 21, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                return;
            }
            int clicked = PagedList(left, labels, sel, ref _page);
            if (clicked >= 0) _selQuest = list[clicked].Id;
            DrawCampaignGlyphs(left, list);
            QuestDef cur = Find(_selQuest);
            if (cur == null || !list.Contains(cur)) cur = list[Mathf.Clamp(_page * PerPage, 0, list.Count - 1)];
            DrawQuestDetail(new Rect(left.xMax + 30f, r.y, r.xMax - left.xMax - 30f, r.height), cur, p);
        }

        private QuestDef FirstUnfinishedOfArc(string arc)
        {
            foreach (QuestDef q in Quests) if (q.Arc == arc && !_completed.Contains(q.Id)) return q;
            return null;
        }

        /// <summary>The giver-state glyph at the left of each campaign row on the visible page of the AVAILABLE list.</summary>
        private void DrawCampaignGlyphs(Rect left, List<QuestDef> list)
        {
            int pages = Mathf.Max(1, (list.Count + PerPage - 1) / PerPage);
            int page = Mathf.Clamp(_page, 0, pages - 1);
            float y = left.y;
            for (int i = page * PerPage; i < Mathf.Min(list.Count, (page + 1) * PerPage); i++)
            {
                if (QuestMeta04E.IsCampaign(list[i]))
                {
                    string reason;
                    GiverState gs = CampaignState(list[i], out reason);
                    MarkerShape04E shape = GuidanceRules04E.ShapeOf(gs);
                    GuidanceGlyphs04E.Draw(Vector2.zero, new Vector2(left.x + 24f, y + 28f), shape == MarkerShape04E.None ? MarkerShape04E.DoneDot : shape, GuidanceRules04E.ColorOf(gs), 24f);
                }
                y += 62f;
            }
        }

        private void DrawQuestDetail(Rect r, QuestDef q, QuestProgress p)
        {
            if (QuestMeta04E.IsCampaign(q))
            {
                DrawCampaignDetail(r, q);
                return;
            }
            Color c = QuestWorld.FactionColor(q.Arc);
            float y = r.y;
            UIKit.Rect(new Rect(r.x, y, r.width, 4f), c);
            y += 12f;
            UIKit.Text(new Rect(r.x, y, r.width, 36f), q.Title, 30, c, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 42f;
            Vector3 start = StartPosition(q);
            UIKit.Text(new Rect(r.x, y, r.width, 26f), QuestWorld.FactionName(q.Arc) + "   From " + GiverName(q) + ", " + Where(start), 19, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 38f;
            UIKit.Text(new Rect(r.x, y, r.width, 140f), q.Summary, 21, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            y += 150f;
            string why = _completed.Contains(q.Id) ? null : WhyLocked(q, p);
            if (_completed.Contains(q.Id)) UIKit.Text(new Rect(r.x, y, r.width, 80f), "<color=#60ff80>Completed.</color> " + q.Done, 20, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            else if (why != null) UIKit.Text(new Rect(r.x, y, r.width, 30f), "Locked: " + why + ".", 20, new Color(1f, 0.6f, 0.5f), TextAnchor.UpperLeft);
            else UIKit.Text(new Rect(r.x, y, r.width, 30f), q.Id == _active ? "Accepted: walk up to the giver to start it again." : "Walk up to the giver and press E (Y on a controller) to accept.", 20, new Color(0.6f, 1f, 0.7f), TextAnchor.UpperLeft);
            y += 44f;
            UIKit.Text(new Rect(r.x, y, r.width, 60f), "Reward: " + QuestLogic.RewardText(q.Reward), 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            y += 70f;
            if (!_completed.Contains(q.Id) && UIKit.Button(new Rect(r.x, y, 300f, 48f), "Set waypoint", false, 20)) Waypoint(start, GiverName(q));
        }

        private void DrawCompleted(Rect r)
        {
            List<string> arcs = new List<string>();
            foreach (QuestDef q in Quests) if (!arcs.Contains(q.Arc)) arcs.Add(q.Arc);
            if (arcs.Count == 0) return;
            if (!arcs.Contains(_selArc)) _selArc = arcs[0];
            float y = r.y;
            float lw = Mathf.Min(420f, r.width * 0.3f);
            foreach (string arc in arcs)
            {
                int done = 0, total = 0;
                foreach (QuestDef q in Quests) if (q.Arc == arc) { total++; if (_completed.Contains(q.Id)) done++; }
                string label = "<color=" + Hex(ArcColor(arc)) + ">" + ArcName(arc) + "</color>   " + done + " / " + total;
                if (UIKit.Button(new Rect(r.x, y, lw, 56f), label, _selArc == arc, 19)) _selArc = arc;
                y += 64f;
            }
            float x = r.x + lw + 30f, w = r.xMax - x;
            y = r.y;
            FactionDef f = FactionDb.Get(_selArc);
            if (f != null)
            {
                int rep = Game.Factions != null ? Game.Factions.Rep(f.Id) : 0;
                UIKit.Text(new Rect(x, y, w, 34f), f.Name.ToUpperInvariant(), 28, f.Color, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(x, y + 4f, w, 30f), "Reputation " + rep + "   Contracts done " + ContractsDone(f.Id), 19, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperRight);
                y += 40f;
                UIKit.Text(new Rect(x, y, w, 30f), f.Blurb, 19, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 40f;
            }
            QuestProgress p = Progress();
            foreach (QuestDef q in Quests)
            {
                if (q.Arc != _selArc) continue;
                bool done = _completed.Contains(q.Id);
                string why = done ? null : WhyLocked(q, p);
                string status = done ? "<color=#60ff80>DONE</color>" : (q.Id == _active ? "<color=#ffd24d>ACCEPTED</color>" : (why == null ? "<color=#ffd24d>AVAILABLE</color>" : "<color=#888888>LOCKED</color>"));
                UIKit.Text(new Rect(x, y, w, 28f), status + "   <b>" + q.Order + ". " + q.Title + "</b>", 21, Color.white, TextAnchor.UpperLeft);
                y += 28f;
                string sub = done ? q.Done : (why != null ? why + "." : q.Summary);
                UIKit.Text(new Rect(x + 24f, y, w - 24f, 50f), sub, 17, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 52f;
                if (y > r.yMax - 40f) break;
            }
        }

        private void DrawContracts(Rect r)
        {
            if (_spots.Count == 0)
            {
                UIKit.Text(new Rect(r.x, r.y, r.width, 40f), "No guild boards.", 22, Color.white, TextAnchor.UpperLeft);
                return;
            }
            QuestProgress p = Progress();
            float lw = Mathf.Min(460f, r.width * 0.32f);
            float y = r.y;
            _selBoard = Mathf.Clamp(_selBoard, 0, _spots.Count - 1);
            for (int i = 0; i < _spots.Count; i++)
            {
                GiverSpot s = _spots[i];
                bool open = ArcOpen(s.Def.Guild, p);
                string label = "<color=" + Hex(QuestWorld.FactionColor(s.Def.Guild)) + ">" + QuestWorld.FactionName(s.Def.Guild) + "</color>" + (open ? "   " + CountOffers(s) + " posted" : "   <color=#888888>closed</color>");
                if (UIKit.Button(new Rect(r.x, y, lw, 56f), label, _selBoard == i, 19)) _selBoard = i;
                y += 64f;
            }
            GiverSpot cur = _spots[_selBoard];
            float x = r.x + lw + 30f, w = r.xMax - x;
            y = r.y;
            UIKit.Text(new Rect(x, y, w, 34f), BoardTitle(cur), 28, QuestWorld.FactionColor(cur.Def.Guild), TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            if (!ArcOpen(cur.Def.Guild, p))
            {
                UIKit.Text(new Rect(x, y, w, 60f), "The board opens with the guild's first quest.", 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                return;
            }
            EnsurePlaced(cur);
            UIKit.Text(new Rect(x, y, w, 26f), "Posted by " + cur.Def.Name + " at " + Where(cur.BoardPos) + ". Read the board in person to take a job.", 18, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            y += 50f;
            EnsureOffers(cur);
            for (int k = 0; k < BoardSlots; k++)
            {
                ContractOffer o = cur.Offers[k];
                if (o == null) continue;
                UIKit.Text(new Rect(x, y, w, 28f), "<b>" + o.Title + "</b>   <color=#aaaaaa>" + o.Kind + ", " + o.Place + "</color>", 21, Color.white, TextAnchor.UpperLeft);
                y += 28f;
                UIKit.Text(new Rect(x + 20f, y, w - 20f, 48f), o.Brief, 17, new Color(0.78f, 0.78f, 0.78f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 48f;
                UIKit.Text(new Rect(x + 20f, y, w - 20f, 24f), QuestLogic.RewardText(o.Reward), 17, UIKit.Gold, TextAnchor.UpperLeft);
                y += 34f;
            }
            if (UIKit.Button(new Rect(x, Mathf.Min(y + 6f, r.yMax - 50f), 300f, 46f), "Waypoint to the board", false, 19)) Waypoint(cur.BoardPos, BoardTitle(cur));
        }

        private void DrawEvents(Rect r)
        {
            WorldEvents ev = Game.Events;
            float y = r.y;
            if (ev == null)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 30f), "World events are not running.", 22, Color.white, TextAnchor.UpperLeft);
                return;
            }
            UIKit.Text(new Rect(r.x, y, r.width, 30f), "HAPPENING NOW", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            int shown = 0;
            foreach (EventInstance e in ev.Active)
            {
                if (e == null || e.Done) continue;
                UIKit.Text(new Rect(r.x, y + 10f, r.width - 360f, 28f), "<b>" + e.T.Title + "</b>   " + Where(e.FocusPos), 20, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(r.xMax - 320f, y, 300f, 46f), "Set waypoint", false, 19)) Waypoint(e.FocusPos, e.T.Title);
                y += 54f;
                shown++;
            }
            if (shown == 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 28f), "Nothing nearby right now. Events appear around you as you explore (a small callout and an E on the map).", 19, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 40f;
            }
            y += 16f;
            UIKit.Text(new Rect(r.x, y, r.width, 30f), "RECENT", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            IList<EventRecord> recent = ev.Recent;
            if (recent.Count == 0) UIKit.Text(new Rect(r.x, y, r.width, 28f), "No events yet.", 19, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft);
            for (int i = recent.Count - 1; i >= 0 && y < r.yMax - 30f; i--)
            {
                EventRecord rec = recent[i];
                string outcome = rec.Outcome == EventOutcome.Success ? "<color=#60ff80>handled</color>" : (rec.Outcome == EventOutcome.Failed ? "<color=#ff6060>failed</color>" : "<color=#999999>missed</color>");
                UIKit.Text(new Rect(r.x, y, r.width, 26f), "Day " + rec.Day + " " + MathUtil.Clock(rec.Hours) + "   <b>" + rec.Title + "</b>   " + outcome + "   <color=#aaaaaa>" + rec.Place + "</color>", 19, Color.white, TextAnchor.UpperLeft);
                y += 30f;
            }
        }

        // ================================================================== debug

        private void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            if (UIKit.Button(new Rect(x, y, 260f, 44f), "Abort running quest", false, 18) && _run != null && Game.Missions != null && Game.Missions.Active == _run.C) Game.Missions.Fail("Aborted from the debug menu.");
            if (UIKit.Button(new Rect(x + 270f, y, 260f, 44f), "Reset all quests", false, 18))
            {
                _completed.Clear();
                _active = "";
                _announced.Clear();
                _debugStatus = "Every quest is undone.";
            }
            if (UIKit.Button(new Rect(x + 540f, y, 260f, 44f), "Mark all sites visited", false, 18))
            {
                foreach (SpecialSite s in Game.Layout.Sites) _visited.Add(s.Key);
                _debugStatus = "Every site counts as visited (the Frost Wardens open).";
            }
            if (UIKit.Button(new Rect(x + 810f, y, 260f, 44f), "Post new contracts", false, 18))
            {
                foreach (GiverSpot s in _spots) for (int k = 0; k < BoardSlots; k++) { s.Slots[k] = 0; s.Offers[k] = null; }
                _debugStatus = "Every board posted new contracts.";
            }
            if (UIKit.Button(new Rect(x + 1080f, y, 260f, 44f), "Check quest data", false, 18)) _debugStatus = CheckDataInGame();
            y += 52f;
            UIKit.Text(new Rect(x, y, r.width, 26f), _debugStatus + (LoadProblems.Count > 0 ? "   Load problems: " + LoadProblems.Count + " (see the Console)" : ""), 17, UIKit.Gold, TextAnchor.UpperLeft);
            y += 32f;
            const int rows = 9;
            int pages = Mathf.Max(1, (Quests.Count + rows - 1) / rows);
            _debugPage = Mathf.Clamp(_debugPage, 0, pages - 1);
            for (int i = _debugPage * rows; i < Mathf.Min(Quests.Count, (_debugPage + 1) * rows); i++)
            {
                QuestDef q = Quests[i];
                bool done = _completed.Contains(q.Id);
                UIKit.Text(new Rect(x, y + 10f, 760f, 30f), "<color=" + Hex(QuestWorld.FactionColor(q.Arc)) + ">" + QuestWorld.FactionName(q.Arc) + "</color>  " + q.Order + ". " + q.Title + (done ? "  <color=#60ff80>done</color>" : (q.Id == _active ? "  <color=#ffd24d>accepted</color>" : "")), 18, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x + 780f, y, 170f, 42f), "Start", false, 17))
                {
                    if (Game.Menus != null) Game.Menus.Close();
                    if (Game.Missions != null && Game.Missions.Active != null) Game.Missions.Fail("Restarted from the debug menu.");
                    if (Game.Root != null) Game.Root.TeleportPlayer(StartPosition(q));
                    _pendingStart = null;
                    _debugStart = q;
                    return;
                }
                if (UIKit.Button(new Rect(x + 960f, y, 170f, 42f), "Complete", false, 17) && !done)
                {
                    if (_run != null && _run.Quest == q && Game.Missions != null && Game.Missions.Active == _run.C) Game.Missions.Pass();
                    else
                    {
                        QuestWorld.Grant(q.Reward, q.Title, true);
                        _completed.Add(q.Id);
                        if (_active == q.Id) _active = "";
                        RaiseCompleted(q.Id);
                    }
                    _debugStatus = "Completed " + q.Title + ".";
                }
                if (UIKit.Button(new Rect(x + 1140f, y, 170f, 42f), "Reset", false, 17))
                {
                    _completed.Remove(q.Id);
                    _announced.Remove(q.Id);
                    if (_active == q.Id) _active = "";
                    _debugStatus = "Reset " + q.Title + ".";
                }
                y += 48f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, y + 6f, 200f, 42f), "Prev page", false, 18)) _debugPage = (_debugPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + 210f, y + 6f, 200f, 42f), "Next page", false, 18)) _debugPage = (_debugPage + 1) % pages;
                UIKit.Text(new Rect(x + 430f, y + 14f, 300f, 30f), "Page " + (_debugPage + 1) + " of " + pages, 18, Color.white, TextAnchor.UpperLeft);
            }
        }

        private QuestDef _debugStart;

        /// <summary>Runs the full validator on the shipped files inside the game (same checks as the EditMode test).</summary>
        private string CheckDataInGame()
        {
            try
            {
                List<string> errors = new List<string>();
                QuestJsonTrace trace = new QuestJsonTrace();
                List<KeyValuePair<string, QuestFile>> qf = new List<KeyValuePair<string, QuestFile>>();
                foreach (TextAsset ta in Resources.LoadAll<TextAsset>("Quests")) qf.Add(new KeyValuePair<string, QuestFile>(ta.name + ".json", QuestJson.Read<QuestFile>(ta.text, ta.name + ".json", errors, trace)));
                List<KeyValuePair<string, EventFile>> ef = new List<KeyValuePair<string, EventFile>>();
                foreach (TextAsset ta in Resources.LoadAll<TextAsset>("Events")) ef.Add(new KeyValuePair<string, EventFile>(ta.name + ".json", QuestJson.Read<EventFile>(ta.text, ta.name + ".json", errors, trace)));
                QuestReport rep = QuestValidator.ValidateAll(qf, ef, QuestRefs.Build(Game.Layout), trace);
                rep.Errors.InsertRange(0, errors);
                string details = rep.Details();
                if (details.Length > 0) Log.Warn("Quest data check:\n" + details);
                return rep.Summary() + (rep.Ok ? "" : " (details in the Console)");
            }
            catch (Exception e)
            {
                Log.Exception("Quest data check", e);
                return "The check failed: " + e.Message;
            }
        }

        /// <summary>Debug starts wait one frame after the menu closes and the teleport lands. The 04E offer card's back
        /// button is read here too, after the menu's own Update, so Esc never reopens the pause menu in the same frame.</summary>
        private void LateUpdate()
        {
            CancelCampaignOfferOnBack();
            RunPendingResume();
            if (_debugStart == null || !Game.IsPlaying) return;
            QuestDef q = _debugStart;
            _debugStart = null;
            StartQuest(q, true);
        }
    }
}
