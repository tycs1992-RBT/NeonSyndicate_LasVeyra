using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the givers: which campaign quests each cast member hands out ("Cast:id" givers), the facts the
    /// one pure decision (GuidanceRules04E.StateOf) turns into a giver state, what talking to them does, their prompt,
    /// and the offer card's content. The overhead marker, the map card, the journal and the offer all read this.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private readonly Dictionary<string, List<QuestDef>> _questsByCast = new Dictionary<string, List<QuestDef>>();
        private readonly List<string> _claims = new List<string>();
        private float _giverTimer;

        /// <summary>Steps that finish instantly: a quest whose remaining steps are all of these is at its hand-in.</summary>
        private static readonly string[] InstantSteps =
        {
            "Spawn", "Ally", "Prop", "SetTime", "SetWeather", "Give", "GiveGun", "AddRep", "Message", "Cinematic", "Claim", "Decide",
            "Branch", "Conduit", "BondRank", "Shortcut"
        };

        private void InitGivers()
        {
            IndexQuests();
        }

        /// <summary>Groups the campaign quests by their "Cast:id" giver: main quests first, then side quests, each in the
        /// quest book's order.</summary>
        private void IndexQuests()
        {
            _questsByCast.Clear();
            QuestBook book = Game.Quests;
            if (book == null) return;
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (QuestDef q in book.Quests)
                {
                    string cast = QuestMeta04E.CastGiver(q);
                    if (cast.Length == 0 || Slot(cast) == null) continue;
                    if (QuestMeta04E.IsMain(q) != (pass == 0)) continue;
                    List<QuestDef> list;
                    if (!_questsByCast.TryGetValue(cast, out list))
                    {
                        list = new List<QuestDef>();
                        _questsByCast[cast] = list;
                    }
                    list.Add(q);
                }
            }
        }

        private List<QuestDef> QuestsOf(string castId)
        {
            List<QuestDef> list;
            return castId != null && _questsByCast.TryGetValue(castId, out list) ? list : null;
        }

        // ================================================================== facts and state

        private void TickGivers(float dt)
        {
            _giverTimer -= dt;
            if (_giverTimer > 0f) return;
            _giverTimer = 0.5f;
            QuestBook book = Game.Quests;
            QuestProgress p = book != null ? book.Progress() : null;
            Vector3 player = Game.PlayerPosition;
            foreach (CastSlot s in _slots)
            {
                QuestDef offer;
                GiverFacts04E f = FactsFor(s, p, out offer);
                string reason;
                s.State = GuidanceRules04E.StateOf(f, out reason);
                s.StateReason = reason;
                s.Offer = offer;
                s.OfferResume = f.OfferIsResume;
                if (s.Talk != null) s.Talk.Priority = f.HasOffer ? Interactable.QuestPriority - 1 : 0;
                if (s.Marker != null)
                {
                    s.Marker.SetPresentation(s.Def.Name, offer != null ? offer.Title : "", s.Terminal != null);
                    s.Marker.Set(s.State, GuidanceRules04E.LayerDistance(player, s.Post));
                }
            }
        }

        /// <summary>The facts for one giver (also decides whether an on-demand person is needed in person).</summary>
        private GiverFacts04E FactsFor(CastSlot s, QuestProgress p, out QuestDef offer)
        {
            GiverFacts04E f = new GiverFacts04E();
            offer = null;
            QuestBook book = Game.Quests;
            List<QuestDef> list = QuestsOf(s.Def.Id);
            s.Needed = s.Reservations.Count > 0;
            if (book == null || list == null || list.Count == 0) return f;
            f.HasQuests = true;
            QuestRun run = book.Running;
            bool allDone = true;
            foreach (QuestDef q in list) if (!book.IsDone(q.Id)) { allDone = false; break; }
            f.AllDone = allDone;
            if (run != null && !run.Contract && run.Quest != null)
            {
                QuestStep step = CurrentStep(run);
                if (step != null && StepTargets(step, s.Def.Id))
                {
                    f.RunningTargetsGiver = true;
                    f.RunningIsFinalStep = AtFinalStep(run);
                    s.Needed = true;
                }
                if (list.Contains(run.Quest)) s.Needed = true;
            }
            QuestDef accepted = book.Find(book.ActiveId);
            if (accepted != null && list.Contains(accepted) && !book.IsDone(accepted.Id) && (run == null || run.Quest != accepted))
            {
                offer = accepted;
                f.OfferIsResume = true;
            }
            // A player who chose CONTRACT should receive that eligible job from Tamsin even while she
            // also offers the next main chapter. The marker, nameplate and OnTalk all use this same offer.
            if (offer == null)
                offer = GuidanceRules04F.TrackedOffer(list, _leadQuest, book.IsDone,
                    q => (run == null || run.Quest != q) && book.WhyLocked(q, p) == null);
            string locked = "";
            if (offer == null)
            {
                foreach (QuestDef q in list)
                {
                    if (book.IsDone(q.Id) || (run != null && run.Quest == q)) continue;
                    string why = book.WhyLocked(q, p);
                    if (why == null)
                    {
                        offer = q;
                        break;
                    }
                    if (locked.Length == 0) locked = "\"" + q.Title + "\" opens later: " + why;
                }
            }
            f.LockedReason = locked;
            f.HasOffer = offer != null;
            f.OfferIsMain = offer != null && QuestMeta04E.IsMain(offer);
            if (offer != null)
            {
                s.Needed = true;
                f.BlockedReason = BlockedReason(s);
            }
            return f;
        }

        /// <summary>The state of one quest's giver with respect to that quest (journal and StateFor), decided by the
        /// same pure rule as the giver markers.</summary>
        private GiverState StateForQuest(QuestDef q, out string reason)
        {
            reason = "";
            QuestBook book = Game.Quests;
            if (q == null || book == null) return GiverState.None;
            CastSlot s = Slot(QuestMeta04E.CastGiver(q));
            GiverFacts04E f = new GiverFacts04E { HasQuests = true, AllDone = book.IsDone(q.Id) };
            QuestRun run = book.Running;
            if (run != null && run.Quest == q && !run.Contract)
            {
                f.RunningTargetsGiver = true;
                QuestStep step = CurrentStep(run);
                f.RunningIsFinalStep = s != null && step != null && StepTargets(step, s.Def.Id) && AtFinalStep(run);
            }
            else if (!f.AllDone)
            {
                bool accepted = book.ActiveId == q.Id;
                string why = accepted ? null : book.WhyLocked(q, null);
                if (why == null)
                {
                    f.HasOffer = true;
                    f.OfferIsMain = QuestMeta04E.IsMain(q);
                    f.OfferIsResume = accepted;
                    f.BlockedReason = s != null ? BlockedReason(s) : CommonBlockedReason();
                }
                else f.LockedReason = why;
            }
            return GuidanceRules04E.StateOf(f, out reason);
        }

        /// <summary>Why an offered quest cannot start right now ("" when it can).</summary>
        private string BlockedReason(CastSlot s)
        {
            string common = CommonBlockedReason();
            if (common.Length > 0) return common;
            string gate = GateReason(s);
            if (gate.Length > 0) return gate;
            return "";
        }

        private static string CommonBlockedReason()
        {
            if (Game.Missions != null && Game.Missions.Active != null)
                return "finish \"" + (Game.Missions.Active.Def != null ? Game.Missions.Active.Def.Title : "the current mission") + "\" first";
            if (Game.Wanted != null && Game.Wanted.Stars > 0) return "resolve the pursuit first";
            bool encounter = (Game.Territories != null && Game.Territories.HasActiveEncounter)
                || (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null)
                || (Game.Events != null && Game.Events.AnyEventActive);
            if (encounter) return "finish the active encounter first";
            return "";
        }

        private static QuestStep CurrentStep(QuestRun run)
        {
            if (run == null || run.Quest == null || run.Quest.Phases == null) return null;
            if (run.Phase < 0 || run.Phase >= run.Quest.Phases.Count) return null;
            QuestPhase ph = run.Quest.Phases[run.Phase];
            if (ph == null || ph.Steps == null || run.Step < 0 || run.Step >= ph.Steps.Count) return null;
            return ph.Steps[run.Step];
        }

        private static bool StepTargets(QuestStep step, string castId)
        {
            string token = CastRoster04E.Token(castId);
            return step.At == token || step.To == token;
        }

        /// <summary>True when the running quest is in its last phase and every step after the current one is instant.</summary>
        private static bool AtFinalStep(QuestRun run)
        {
            List<QuestPhase> phases = run.Quest.Phases;
            QuestPhase ph = phases[run.Phase];
            bool lastPhase = run.Phase == phases.Count - 1 || ph.Next == QuestDefaults.End;
            if (!lastPhase) return false;
            for (int i = run.Step + 1; i < ph.Steps.Count; i++)
            {
                QuestStep st = ph.Steps[i];
                if (st != null && Array.IndexOf(InstantSteps, st.Type) < 0) return false;
            }
            return true;
        }

        // ================================================================== journal and map helpers

        /// <summary>The state a quest's giver shows now and why (for Unavailable and No work), as the journal draws it.</summary>
        public GiverState StateFor(QuestDef q, out string reason)
        {
            return StateForQuest(q, out reason);
        }

        /// <summary>The offer card's content for a quest (giver, purpose, where with floor and entrance, risk, rewards).</summary>
        public bool DescribeQuest(QuestDef q, out OfferView04E view)
        {
            view = new OfferView04E();
            if (q == null) return false;
            view = OfferView(Slot(QuestMeta04E.CastGiver(q)), q);
            return true;
        }

        /// <summary>"Nessa Vale at the Veyra Lofts door (12 m east)", or the giver's name when the post is not bound.</summary>
        public string GiverPlace(QuestDef q)
        {
            return q == null ? "" : GiverWhere(q);
        }

        /// <summary>The live state of a cast member's marker (tests and debugging read it).</summary>
        public GiverState CastState(string castId)
        {
            CastSlot s = Slot(castId);
            return s != null ? s.State : GiverState.None;
        }

        // ================================================================== talking

        private void OnTalk(CastSlot s)
        {
            if (s.Actor != null) Face(s, QuestWorld.YawToward(s.Actor.transform.position, Game.PlayerPosition), true);
            switch (s.State)
            {
                case GiverState.AvailableMain:
                case GiverState.AvailableSide:
                    if (s.Offer != null && Game.Quests != null) Game.Quests.OfferCampaign(s.Offer, OfferView(s, s.Offer));
                    break;
                case GiverState.Unavailable:
                    Notify(s.Def.Name + ": not right now, " + s.StateReason + ".", 4f);
                    break;
                case GiverState.ActiveTarget:
                case GiverState.TurnIn:
                    break;
                default:
                    // 04E: after the Kiln Regent, Ilse runs the refuge trade stall (CampaignDirector.Refuge).
                    if (TryOpenRefugeStall(s)) break;
                    string greeting = GreetingFor(s.Def);
                    if (Game.Quests != null && Game.Quests.IsDone("uh05_04g_01_courier") &&
                        (s.Def.Id == CastIds04E.Dispatcher || s.Def.Id == CastIds04E.Maintainer))
                        greeting = "The reserve you carried is holding steady. The block remembers who kept its lamps lit.";
                    if (Game.Hud != null && !string.IsNullOrEmpty(greeting) && s.Actor != null) Game.Hud.Subtitle(s.Def.Name, greeting, 3.5f);
                    string why = s.State == GiverState.Completed ? "Thanks for everything. Nothing new for now." :
                        (string.IsNullOrEmpty(s.StateReason) ? "No work right now." : "No work right now. " + s.StateReason + ".");
                    Notify((s.Terminal != null ? s.Def.Name + " (message terminal): " : s.Def.Name + ": ") + why, 4f);
                    break;
            }
        }

        private string TalkPrompt(CastSlot s)
        {
            string who = s.Terminal != null ? "the message terminal for " + s.Def.Name + " (not present)" : s.Def.Name;
            switch (s.State)
            {
                case GiverState.AvailableMain:
                case GiverState.AvailableSide:
                    string kind = s.State == GiverState.AvailableMain ? "main quest" : "side quest";
                    string title = s.Offer != null ? s.Offer.Title : "";
                    string resume = s.OfferResume ? (s.Offer != null && QuestMeta04E.CanResume(s.Offer) ? ", resume" : ", restart") : "";
                    return "Press E to talk to " + who + ": " + title + " (" + kind + resume + ")";
                case GiverState.Unavailable:
                    return s.Def.Name + " is unavailable: " + s.StateReason;
                default:
                    if (RunsRefugeStall(s)) return "Press E to talk to " + who + " and trade at the " + RefugeTrade04E.StallName;
                    return "Press E to talk to " + who;
            }
        }

        /// <summary>The prompt hides while the quest's own step owns the conversation, and during other missions unless it
        /// explains why the person is unavailable (it never steals a mission's E press).</summary>
        private bool TalkVisible(CastSlot s)
        {
            if (s.State == GiverState.ActiveTarget || s.State == GiverState.TurnIn) return false;
            bool mission = Game.Missions != null && Game.Missions.Active != null;
            return !mission || s.State == GiverState.Unavailable;
        }

        private static void Notify(string text, float seconds)
        {
            // Key names follow the active device ("Press E" becomes the pad's interact button).
            if (Game.Hud != null) Game.Hud.Notify(GInput.Prompt(text), seconds);
        }

        // ================================================================== offer and card content

        /// <summary>What the offer card and the map card say about a quest.</summary>
        private OfferView04E OfferView(CastSlot s, QuestDef q)
        {
            OfferView04E v = new OfferView04E();
            v.Giver = s == null ? QuestGiverName(q) : (s.Terminal != null ? s.Def.Name + " (message terminal; " + s.Def.Name + " is not present)" : s.Def.Name + ", " + s.Def.Role);
            v.Title = q.Title;
            v.Kind = QuestMeta04E.Kind(q);
            v.Purpose = q.Summary;
            string dest = QuestMeta04E.FirstDestination(q);
            v.Location = DestinationName(dest, s);
            v.Floor = QuestMeta04E.Floor(q, AnchorHeight);
            v.Entrance = QuestMeta04E.Entrance(q, VenueTitle);
            v.Difficulty = QuestMeta04E.Difficulty(q);
            v.Risk = GuidanceRules04E.RiskText(v.Difficulty, QuestMeta04E.HasCombat(q), QuestMeta04E.Region(q).Length > 0);
            v.Rewards = RewardsText(q);
            Vector3 pos;
            bool known = DestinationPoint(dest, out pos);
            if (!known && s != null && s.PostBound)
            {
                pos = s.Post;
                known = true;
            }
            if (known) v.Distance = GuidanceRules04E.DistanceText(MathUtil.FlatDistance(Game.PlayerPosition, pos), QuestWorld.Direction(Game.PlayerPosition, pos));
            if (s != null && s.OfferResume && q == s.Offer)
                v.Note = QuestMeta04E.CanResume(q) ? "Resumes at your last checkpoint; finished steps and rewards are kept." : "Accepted earlier: it starts again from the beginning.";
            return v;
        }

        private static string QuestGiverName(QuestDef q)
        {
            string cast = QuestMeta04E.CastGiver(q);
            if (cast.Length > 0) return CastIds04E.Name(cast);
            return Game.Quests != null ? Game.Quests.GiverName(q) : "";
        }

        /// <summary>The quest's money and Lumen rewards plus the durable claims it pays.</summary>
        private string RewardsText(QuestDef q)
        {
            StringBuilder sb = new StringBuilder(QuestLogic.RewardText(q.Reward));
            QuestMeta04E.Claims(q, _claims);
            foreach (string id in _claims)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(GuidanceRules04E.RewardTitle(id));
            }
            return sb.ToString();
        }

        /// <summary>A readable name for a place token ("Cinder Dispatch", "Lantern Refuge"), or the giver's own post.</summary>
        private string DestinationName(string token, CastSlot giver)
        {
            string venue = QuestMeta04E.VenueOfToken(token);
            if (venue.Length > 0) return VenueTitle(venue);
            string region = QuestMeta04E.RegionOfToken(token);
            if (region.Length > 0) return UnderheartIds.RegionTitle(region);
            return giver != null && giver.PostBound ? giver.PostName : "";
        }

        /// <summary>The live position of a place token (venue anchor, Underheart anchor or cast post).</summary>
        private bool DestinationPoint(string token, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (string.IsNullOrEmpty(token)) return false;
            string venue = QuestMeta04E.VenueOfToken(token);
            if (venue.Length > 0)
            {
                VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(venue) : null;
                if (v == null || v.Building == null) return false;
                string anchor = QuestMeta04E.AnchorOfToken(token);
                pos = v.Available && VenueHasAnchor(v, anchor) ? v.Point(anchor) : v.Entrance;
                return true;
            }
            if (token.StartsWith("Under:", StringComparison.Ordinal))
            {
                float yaw;
                return Game.Underheart != null && Game.Underheart.Ready && Game.Underheart.TryAnchor(token.Substring(6), out pos, out yaw);
            }
            string cast = CastRoster04E.IdFromToken(token);
            if (cast.Length > 0)
            {
                float yaw;
                return CastPost(cast, out pos, out yaw);
            }
            return false;
        }

        /// <summary>Height of a venue anchor above its venue floor (upper floor detection for the offer's floor line).</summary>
        private static float AnchorHeight(string token)
        {
            string venue = QuestMeta04E.VenueOfToken(token);
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(venue) : null;
            string anchor = QuestMeta04E.AnchorOfToken(token);
            if (v == null || !v.Available || !VenueHasAnchor(v, anchor)) return 0f;
            return v.View.LocalAnchor(anchor).y;
        }
    }
}
