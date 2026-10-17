using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the session: the one-time update card for an eligible loaded save, the opening lead tracked
    /// once for a new Free Roam session (never accepted for the player), the tracked lead that follows a quest's giver,
    /// and the "what next" card after a campaign quest (up to three choices that only track, shown out of combat after
    /// the completion banner). Pausing cards close with Esc, B or their buttons.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private float _sessionStart = -100f;
        private bool _sessionActive;
        private bool _sessionLoaded;
        private bool _sessionFreeRoam;
        private float _openingAt = -1f;
        private float _updateCardAt = -1f;
        private float _retrackAt = -1f;
        private readonly List<string> _nextQueue = new List<string>();
        private readonly HashSet<string> _nextShown = new HashSet<string>();
        private float _nextQueuedAt;
        private bool _cardOpen;
        private float _leadTimer;
        private string _leadQuest = "";
        private string _leadPlaceLabel = "";

        /// <summary>Seconds since the current session started (the session chip reads it).</summary>
        public float SessionAge { get { return _sessionActive ? Time.unscaledTime - _sessionStart : -1f; } }

        private void ResetSessionState()
        {
            _sessionActive = false;
            _sessionStart = -100f;
            _openingAt = -1f;
            _updateCardAt = -1f;
            _retrackAt = -1f;
            _nextQueue.Clear();
            _nextShown.Clear();
            _cardOpen = false;
            _leadQuest = "";
            _leadPlaceLabel = "";
            _selected = null;
        }

        private void BeginSession(bool loaded, bool freeRoam)
        {
            _sessionStart = Time.unscaledTime;
            _sessionActive = true;
            _sessionLoaded = loaded;
            _sessionFreeRoam = freeRoam;
            _nextQueue.Clear();
            _nextShown.Clear();
            IndexQuests();
            float now = Time.unscaledTime;
            // A session that starts with 04E never needs the "now installed" card; it gets the opening lead instead.
            if (!loaded) _save.UpdateCardSeen = true;
            if (GuidanceRules04E.AutoTrackOpening(loaded, freeRoam, _save.OpeningLeadOffered)) _openingAt = now + 2.5f;
            if (GuidanceRules04E.UpdateCardEligible(loaded, _save.UpdateCardSeen)) _updateCardAt = now + 3.5f;
            else if (loaded && !string.IsNullOrEmpty(_save.TrackedLead)) _retrackAt = now + 1f;
            ReportModule();
        }

        private static bool MissionFree()
        {
            return Game.Missions == null || Game.Missions.Active == null;
        }

        private void TickSession()
        {
            if (_cardOpen && (Game.Menus == null || !Game.Menus.ChoiceOpen)) _cardOpen = false;
            if (!_sessionActive || !Game.IsPlaying) return;
            if ((Game.Menus != null && Game.Menus.IsOpen) || (Game.Phone != null && Game.Phone.IsOpen)) return;
            float now = Time.unscaledTime;
            if (_retrackAt > 0f && now >= _retrackAt)
            {
                _retrackAt = -1f;
                TrackQuestLead(_save.TrackedLead, false);
            }
            if (_openingAt > 0f && now >= _openingAt && MissionFree())
            {
                _openingAt = -1f;
                AutoTrackOpening();
            }
            if (_updateCardAt > 0f && now >= _updateCardAt && MissionFree() && !CombatDirector.InCombat)
            {
                _updateCardAt = -1f;
                ShowUpdateCard();
                return;
            }
            if (_nextQueue.Count > 0 && GuidanceRules04E.NextChoicesReady(now - _nextQueuedAt, !MissionFree(),
                    CombatDirector.InCombat || (Game.Wanted != null && Game.Wanted.Stars > 0), false, true))
            {
                string id = _nextQueue[0];
                _nextQueue.RemoveAt(0);
                OpenNextChoices(id);
            }
        }

        /// <summary>Esc or B closes one of the guidance cards (the menu's own choice screen ignores both).</summary>
        private void LateUpdate()
        {
            if (!_cardOpen || Game.Menus == null || !Game.Menus.ChoiceOpen) return;
            if (Act.Pause || (Pad.Connected && Pad.Down(PadButton.East)))
            {
                _cardOpen = false;
                Game.Menus.Close();
            }
        }

        // ================================================================== leads

        /// <summary>The first unfinished main quest in campaign order ("" when every one is done or none is loaded).</summary>
        private static string MainLeadQuest()
        {
            QuestBook book = Game.Quests;
            if (book == null) return "";
            foreach (string id in QuestIds04E.Main)
            {
                QuestDef q = book.Find(id);
                if (q != null && !book.IsDone(id)) return id;
            }
            return "";
        }

        private void AutoTrackOpening()
        {
            _save.OpeningLeadOffered = true;
            string id = MainLeadQuest();
            QuestBook book = Game.Quests;
            QuestDef q = book != null ? book.Find(id) : null;
            if (q == null)
            {
                Notify(ModuleStatus.Label + ": the opening lead is not available (Pause > STATUS says why).", 6f);
                return;
            }
            if (!TrackQuestLead(id, false)) return;
            CastSlot s = Slot(QuestMeta04E.CastGiver(q));
            string where = s != null && s.PostBound ? s.PostName : "the map";
            string behind = "";
            if (s != null && s.PostBound && Game.Cam != null)
            {
                Vector3 to = MathUtil.Flat(s.Post - Game.PlayerPosition);
                if (to.sqrMagnitude > 1f && Vector3.Dot(to.normalized, Game.Cam.FlatForward) < -0.3f) behind = " (behind you)";
            }
            Notify("New lead: " + QuestGiverName(q) + " at " + where + behind + ". Follow the route. Press E when you reach them.", 8f);
        }

        /// <summary>Tracks a quest's giver as the lead. announce: the player asked (a notice, and a manual waypoint gives way).</summary>
        private bool TrackQuestLead(string questId, bool announce)
        {
            QuestBook book = Game.Quests;
            if (book == null || string.IsNullOrEmpty(questId)) return false;
            QuestDef q = book.Find(questId);
            if (q == null)
            {
                if (announce) Notify("That lead is not loaded in this build.", 4f);
                return false;
            }
            if (book.IsDone(q.Id))
            {
                string next = MainLeadQuest();
                if (next.Length == 0)
                {
                    if (announce) Notify("Every main quest of " + ModuleStatus.Label + " is complete.", 4f);
                    ClearLeadRoute();
                    _save.TrackedLead = "";
                    return false;
                }
                q = book.Find(next);
            }
            bool active = book.ActiveId == q.Id || (book.Running != null && book.Running.Quest == q);
            string why = active ? null : book.WhyLocked(q, null);
            if (why != null)
            {
                if (announce) Notify("\"" + q.Title + "\" opens later: " + why + ".", 5f);
                return false;
            }
            _save.TrackedLead = q.Id;
            Vector3 pos;
            string label;
            if (!LeadTarget(q, out pos, out label))
            {
                if (announce) Notify(QuestGiverName(q) + " cannot be reached right now. Pause > STATUS says why.", 5f);
                return false;
            }
            _leadQuest = q.Id;
            _leadPlaceLabel = "";
            if (Game.Gps != null)
            {
                if (announce) Game.Gps.ClearWaypoint();
                Game.Gps.SetLead(pos, label, "quest:" + q.Id);
            }
            if (announce)
                Notify("Lead tracked: " + label + "." + (MissionFree() ? " Follow the route. Press E when you reach them." : " Your current mission keeps the route until it ends."), 5f);
            return true;
        }

        /// <summary>Tracks a place (an upgrade station) for this session only.</summary>
        private void TrackPlace(Vector3 pos, string label)
        {
            _leadQuest = "";
            _leadPlaceLabel = label;
            _save.TrackedLead = "";
            if (Game.Gps != null)
            {
                Game.Gps.ClearWaypoint();
                Game.Gps.SetLead(pos, label, "place");
            }
            Notify("Lead tracked: " + label + ".", 4f);
        }

        /// <summary>Where a quest's lead points: its cast giver's post, or the legacy giver position.</summary>
        private bool LeadTarget(QuestDef q, out Vector3 pos, out string label)
        {
            pos = Vector3.zero;
            label = QuestGiverName(q) + ": " + q.Title;
            string cast = QuestMeta04E.CastGiver(q);
            if (cast.Length > 0)
            {
                float yaw;
                return CastPost(cast, out pos, out yaw);
            }
            if (Game.Quests == null) return false;
            pos = Game.Quests.StartPosition(q);
            return true;
        }

        private void TickLead(float dt)
        {
            _leadTimer -= dt;
            if (_leadTimer > 0f) return;
            _leadTimer = 1f;
            if (_leadQuest.Length == 0 || Game.Gps == null || Game.Quests == null) return;
            QuestDef q = Game.Quests.Find(_leadQuest);
            if (q == null || Game.Quests.IsDone(q.Id))
            {
                ClearLeadRoute();
                return;
            }
            Vector3 pos;
            string label;
            if (LeadTarget(q, out pos, out label)) Game.Gps.SetLead(pos, label, "quest:" + q.Id);
        }

        private void ClearLeadRoute()
        {
            _leadQuest = "";
            _leadPlaceLabel = "";
            if (Game.Gps != null) Game.Gps.ClearLead();
        }

        /// <summary>The quest the HUD lead line belongs to ("" for a place lead or none).</summary>
        public string LeadQuestId { get { return _leadQuest; } }

        // ================================================================== update card

        /// <summary>The installed destinations in priority order, each with whether it is really in the world and why not.</summary>
        private List<Destination04E> InstalledDestinations()
        {
            List<Destination04E> list = new List<Destination04E>();
            CastSlot nessa = Slot(CastIds04E.Nessa);
            if (nessa != null)
            {
                EnsureBound(nessa);
                string gate = GateReason(nessa);
                list.Add(new Destination04E
                {
                    Name = nessa.Def.Name + " at " + (nessa.PostBound ? nessa.PostName : "the Veyra Lofts"),
                    Detail = "the opening lead" + (nessa.Terminal != null ? " (message terminal standing in)" : "") + Away(nessa.PostBound, nessa.Post),
                    Installed = nessa.PostBound && gate.Length == 0,
                    Reason = gate
                });
            }
            AddVenue(list, VenueIds04E.Dispatch, "walk-in office with Ion's receiver and the fitting worktable");
            VenueBinding04C pressure = Game.Inhabited != null ? Game.Inhabited.FindVenue(VenueIds04E.Pressure) : null;
            bool under = Game.Underheart != null && Game.Underheart.Ready;
            AddVenue(list, VenueIds04E.Pressure, under ? "utility building with a real stair down to the Underheart" : "utility building (the stair below is not built)");
            AddVenue(list, VenueIds04E.Bondkeeper, "creature care room and training yard");
            AddVenue(list, VenueIds04E.Office, "faction records office");
            list.Add(new Destination04E
            {
                Name = "Lantern Refuge, under the city",
                Detail = "safe underground hall reached by the Pressure House stair",
                Installed = under && pressure != null && pressure.Available,
                Reason = Game.Underheart == null ? "the Underheart did not start" : (!under ? Game.Underheart.Status : "")
            });
            Vector3 bay;
            float radius;
            bool hasBay = Game.Fusion != null && Game.Fusion.TryBay(out bay, out radius);
            list.Add(new Destination04E { Name = "Jax's garage bay", Detail = "creature-vehicle fusion service", Installed = hasBay, Reason = hasBay ? "" : "the fusion bay is not available" });
            return list;
        }

        private void AddVenue(List<Destination04E> list, string id, string detail)
        {
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(id) : null;
            bool ok = v != null && v.Available;
            list.Add(new Destination04E
            {
                Name = VenueTitle(id),
                Detail = detail + (ok ? Away(true, v.Entrance) : ""),
                Installed = ok,
                Reason = v == null ? "not in this build" : (ok ? "" : v.Status)
            });
        }

        private static string Away(bool known, Vector3 p)
        {
            if (!known || Game.Player == null) return "";
            return " (" + GuidanceRules04E.DistanceText(MathUtil.FlatDistance(Game.PlayerPosition, p), QuestWorld.Direction(Game.PlayerPosition, p)) + ")";
        }

        private void ShowUpdateCard()
        {
            _save.UpdateCardSeen = true;
            if (Game.Menus == null) return;
            List<Destination04E> pick = GuidanceRules04E.PickInstalled(InstalledDestinations(), 3);
            QuestBook book = Game.Quests;
            string lead = MainLeadQuest();
            QuestDef q = book != null ? book.Find(lead) : null;
            string why = q == null ? "the campaign quests are not loaded" : (book.ActiveId == q.Id ? null : book.WhyLocked(q, null));
            bool canTrack = q != null && why == null;
            string note = canTrack ? "Opening lead: " + QuestGiverName(q) + ", \"" + q.Title + "\". Tracking it never starts it."
                : "The opening lead opens later: " + why + ".";
            string text = GuidanceRules04E.UpdateCardText(pick, 3, note);
            _cardOpen = true;
            string leadId = canTrack ? q.Id : "";
            Game.Menus.ShowChoice(ModuleStatus.Label.ToUpperInvariant() + " IS INSTALLED", text,
                canTrack ? new[] { "TRACK OPENING LEAD", "DISMISS" } : new[] { "DISMISS" }, i =>
                {
                    _cardOpen = false;
                    if (canTrack && i == 0) TrackQuestLead(leadId, true);
                });
        }

        // ================================================================== next choices

        private void OnQuestCompleted(string questId)
        {
            QuestDef q = Game.Quests != null ? Game.Quests.Find(questId) : null;
            if (_save.TrackedLead == questId) _save.TrackedLead = "";
            if (_leadQuest == questId) ClearLeadRoute();
            if (q != null && QuestMeta04E.IsCampaign(q)) QueueNextChoices(questId);
        }

        private void QueueNextChoices(string questId)
        {
            if (string.IsNullOrEmpty(questId) || _nextShown.Contains(questId) || _nextQueue.Contains(questId)) return;
            _nextQueue.Add(questId);
            _nextQueuedAt = Time.unscaledTime;
        }

        private void OpenNextChoices(string completedId)
        {
            _nextShown.Add(completedId);
            if (Game.Menus == null || Game.Quests == null) return;
            List<NextOption04E> options = GuidanceRules04E.PickNext(MainOption(), SideOption(), UpgradeOption());
            if (options.Count == 0) return;
            QuestDef done = Game.Quests.Find(completedId);
            StringBuilder sb = new StringBuilder();
            sb.Append("What next? Choosing only tracks it; nothing starts until you talk to someone.\n");
            List<string> buttons = new List<string>();
            foreach (NextOption04E o in options)
            {
                sb.Append("\n<b>").Append(o.Button).Append(":</b> ").Append(o.Detail);
                buttons.Add(o.Button);
            }
            buttons.Add("LATER");
            _cardOpen = true;
            Game.Menus.ShowChoice("QUEST COMPLETE: " + (done != null ? done.Title.ToUpperInvariant() : completedId), sb.ToString(), buttons.ToArray(), i =>
            {
                _cardOpen = false;
                if (i < 0 || i >= options.Count) return;
                NextOption04E o = options[i];
                if (!string.IsNullOrEmpty(o.QuestId)) TrackQuestLead(o.QuestId, true);
                else if (o.HasTarget) TrackPlace(o.Target, o.Detail);
                else Notify(o.Detail, 5f);
            });
        }

        private NextOption04E? MainOption()
        {
            QuestBook book = Game.Quests;
            string id = MainLeadQuest();
            QuestDef q = book.Find(id);
            if (q == null || book.WhyLocked(q, null) != null) return null;
            return new NextOption04E { Kind = 0, Button = "CONTINUE MAIN", QuestId = q.Id, Detail = "\"" + q.Title + "\" with " + GiverWhere(q) };
        }

        private NextOption04E? SideOption()
        {
            QuestBook book = Game.Quests;
            QuestDef best = null;
            float bestD = float.MaxValue;
            foreach (string id in QuestIds04E.Side)
            {
                QuestDef q = book.Find(id);
                if (q == null || book.IsDone(id) || book.WhyLocked(q, null) != null) continue;
                CastSlot s = Slot(QuestMeta04E.CastGiver(q));
                if (s == null) continue;
                EnsureBound(s);
                if (!s.PostBound) continue;
                float d = GuidanceRules04E.LayerDistance(Game.PlayerPosition, s.Post);
                if (d < bestD)
                {
                    bestD = d;
                    best = q;
                }
            }
            if (best == null) return null;
            return new NextOption04E { Kind = 1, Button = "SIDE QUEST", QuestId = best.Id, Detail = "\"" + best.Title + "\" with " + GiverWhere(best) };
        }

        private NextOption04E? UpgradeOption()
        {
            BuildSystem b = Game.Builds;
            Vector3 p;
            if (b != null && b.Ledger != null && b.Ledger.PendingCount > 0)
            {
                bool home = VenuePoint("lofts", "desk", out p);
                Vector3 refuge = Vector3.zero;
                float yaw;
                bool under = Game.Underheart != null && Game.Underheart.Ready && Game.Underheart.TryAnchor("refuge.ledger", out refuge, out yaw);
                bool useRefuge = under && (!home || GuidanceRules04E.LayerDistance(Game.PlayerPosition, refuge) < GuidanceRules04E.LayerDistance(Game.PlayerPosition, p));
                string where = useRefuge ? "the Lantern Refuge ledger" : (home ? "your desk upstairs at the Veyra Lofts" : "a ledger terminal or the BUILD page (K)");
                return new NextOption04E
                {
                    Kind = 2, Button = "UPGRADE", Detail = "Claim " + b.Ledger.PendingCount + " pending reward(s) at " + where,
                    Target = useRefuge ? refuge : p, HasTarget = useRefuge || home
                };
            }
            if (Game.Bonds != null && Game.Bonds.Active != null && VenuePoint(VenueIds04E.Bondkeeper, "care_station", out p))
                return new NextOption04E { Kind = 2, Button = "UPGRADE", Detail = "Care and training for " + Game.Bonds.Active.Name + " at Bondkeeper's Refuge", Target = p, HasTarget = true };
            if (b != null && string.IsNullOrEmpty(b.Preset) && VenuePoint(VenueIds04E.Dispatch, "fitting", out p))
                return new NextOption04E { Kind = 2, Button = "UPGRADE", Detail = "Choose a build at the Cinder Dispatch fitting worktable", Target = p, HasTarget = true };
            return new NextOption04E { Kind = 2, Button = "UPGRADE", Detail = "Open the BUILD page (K, or from the pause menu) to compare your gear, ward and companion", HasTarget = false };
        }

        private string GiverWhere(QuestDef q)
        {
            CastSlot s = Slot(QuestMeta04E.CastGiver(q));
            if (s == null || !s.PostBound) return QuestGiverName(q);
            return s.Def.Name + " at " + s.PostName + Away(true, s.Post);
        }

        private static bool VenuePoint(string venueId, string anchor, out Vector3 p)
        {
            p = Vector3.zero;
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(venueId) : null;
            if (v == null || !v.Available || !VenueHasAnchor(v, anchor)) return false;
            p = v.Point(anchor);
            return true;
        }
    }
}
