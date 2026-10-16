using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// QuestBook, part two: the guild givers in the world (a glowing marker, a Q blip and a talk prompt while they have
    /// a quest to offer) and the contract board beside each giver (three repeatable contracts, a C blip, refreshed
    /// every in-game day and whenever one is finished). Everything hides while any mission runs.
    /// </summary>
    public sealed partial class QuestBook
    {
        public const int BoardSlots = 3;

        /// <summary>One guild's spot in the world: its giver and its contract board.</summary>
        public sealed class GiverSpot
        {
            public GiverDef Def;
            public bool Placed;
            public Vector3 Pos;
            public Vector3 BoardPos;
            public QuestDef Offer;
            public bool BoardOpen;
            public WorldMarker Marker;
            public Interactable Talk;
            public Blip Blip;
            public QuestProp Board;
            public Interactable BoardUse;
            public Blip BoardBlip;
            public readonly int[] Slots = new int[BoardSlots];
            public readonly ContractOffer[] Offers = new ContractOffer[BoardSlots];
            public int Done;
            /// <summary>Set for a quest with its own Giver place: the spot offers only that quest and has no board.</summary>
            public QuestDef Only;
        }

        private readonly List<GiverSpot> _spots = new List<GiverSpot>();
        /// <summary>Spots for quests that name their own Giver place (kept apart from the guild boards).</summary>
        private readonly List<GiverSpot> _questSpots = new List<GiverSpot>();
        private Transform _markerRoot;
        private int _nextSerial = 1;
        private int _boardDay = -1;

        /// <summary>Every guild giver and board (the journal lists them).</summary>
        public IList<GiverSpot> Spots { get { return _spots; } }

        private void InitBoards()
        {
            _spots.Clear();
            _questSpots.Clear();
            foreach (GiverDef g in Givers) _spots.Add(new GiverSpot { Def = g });
            foreach (QuestDef q in Quests)
            {
                if (string.IsNullOrEmpty(q.Giver) || !string.IsNullOrEmpty(q.VenueGiver)) continue;
                GiverDef own = new GiverDef { Guild = q.Arc, At = q.Giver, OffsetX = q.GiverOffsetX, OffsetZ = q.GiverOffsetZ, Name = GiverName(q) };
                _questSpots.Add(new GiverSpot { Def = own, Only = q });
            }
            GameObject root = new GameObject("Quest Givers");
            root.transform.SetParent(transform, false);
            _markerRoot = root.transform;
        }

        public GiverSpot SpotFor(string guild)
        {
            foreach (GiverSpot s in _spots) if (s.Def.Guild == guild) return s;
            return null;
        }

        // ------------------------------------------------------------------ markers

        private void EnsurePlaced(GiverSpot s)
        {
            if (s.Placed) return;
            Vector3 p;
            if (!QuestPlaces.GiverPosition(Game.Layout, s.Def.At, s.Def.OffsetX, s.Def.OffsetZ, out p)) return;
            Vector3 anchor = p;
            Poi poi = Game.Layout.FindPoi(s.Def.At);
            if (poi != null) anchor = poi.Position;
            s.Pos = QuestWorld.OpenGround(p, anchor);
            Vector3 side = poi != null && poi.Facing.sqrMagnitude > 0.01f ? MathUtil.RightOf(poi.Facing.normalized) : Vector3.right;
            s.BoardPos = QuestWorld.OpenGround(s.Pos + side * 3f, s.Pos);
            s.Placed = true;
            Color c = QuestWorld.FactionColor(s.Def.Guild);
            GiverSpot spot = s;
            s.Marker = WorldMarker.Create(s.Pos, c, 1.3f, _markerRoot, true);
            s.Talk = Interactable.Create("Quest giver " + s.Def.Guild, s.Pos, 2.2f, "", () => { if (spot.Offer != null) Offer(spot.Offer); }, _markerRoot);
            s.Talk.DynamicPrompt = () => spot.Offer != null ? "Press E to talk to " + spot.Def.Name + ": " + spot.Offer.Title : "";
            s.Talk.Condition = () => spot.Offer != null;
            s.Blip = Blip.Add(BlipKind.Contact, null, s.Pos, c, "Q", "", true);
            s.Blip.Size = 1.3f;
            if (s.Only != null) return;
            s.Board = QuestProps.Build("Board", s.BoardPos, QuestWorld.YawToward(s.BoardPos, s.Pos) + 90f, _markerRoot, c);
            s.BoardUse = Interactable.Create("Contract board " + s.Def.Guild, s.BoardPos, 2f, "Press E to read the " + BoardTitle(s), () => ShowBoard(spot), _markerRoot);
            s.BoardUse.Condition = () => spot.BoardOpen;
            s.BoardBlip = Blip.Add(BlipKind.Activity, null, s.BoardPos, c, "C", BoardTitle(s), false);
            s.Board.gameObject.SetActive(false);
        }

        public static string BoardTitle(GiverSpot s)
        {
            return !string.IsNullOrEmpty(s.Def.Board) ? s.Def.Board : QuestWorld.FactionName(s.Def.Guild) + " contract board";
        }

        private void HideMarkers()
        {
            foreach (GiverSpot s in _questSpots)
            {
                if (s.Marker != null) s.Marker.gameObject.SetActive(false);
                if (s.Blip != null) s.Blip.Visible = false;
                s.Offer = null;
            }
            foreach (GiverSpot s in _spots)
            {
                if (s.Marker != null) s.Marker.gameObject.SetActive(false);
                if (s.Blip != null) s.Blip.Visible = false;
                if (s.BoardBlip != null) s.BoardBlip.Visible = false;
                s.Offer = null;
                s.BoardOpen = false;
            }
        }

        /// <summary>Shows each giver and board when it has something and no mission is running.</summary>
        private void RefreshMarkers()
        {
            bool busy = Game.Missions != null && Game.Missions.Active != null;
            QuestProgress p = Progress();
            CheckBoardDay();
            foreach (GiverSpot s in _spots)
            {
                EnsurePlaced(s);
                if (!s.Placed) continue;
                bool open = ArcOpen(s.Def.Guild, p);
                s.Offer = busy ? null : OfferFor(s.Def.Guild, p);
                bool hasTemplates = false;
                foreach (ContractDef t in ContractTemplates) if (t.Guild == s.Def.Guild) { hasTemplates = true; break; }
                s.BoardOpen = open && hasTemplates && !busy;
                if (s.BoardOpen) EnsureOffers(s);
                if (s.Marker != null) s.Marker.gameObject.SetActive(s.Offer != null);
                if (s.Blip != null)
                {
                    s.Blip.Visible = s.Offer != null;
                    if (s.Offer != null) s.Blip.Label = QuestWorld.FactionName(s.Def.Guild) + ": " + s.Offer.Title + " (" + s.Def.Name + ")" + (s.Offer.Id == _active ? " [restart]" : "");
                }
                if (s.Board != null) s.Board.gameObject.SetActive(open && hasTemplates);
                if (s.BoardBlip != null)
                {
                    // One blip per guild spot: the quest giver's Q wins when both have something.
                    s.BoardBlip.Visible = s.BoardOpen && s.Offer == null;
                    s.BoardBlip.Label = BoardTitle(s) + " (" + CountOffers(s) + " contracts)";
                }
            }
            foreach (GiverSpot s in _questSpots)
            {
                EnsurePlaced(s);
                if (!s.Placed) continue;
                QuestDef q = s.Only;
                bool offered = !busy && !_completed.Contains(q.Id) && (q.Id == _active || WhyLocked(q, p) == null);
                s.Offer = offered ? q : null;
                if (s.Marker != null) s.Marker.gameObject.SetActive(offered);
                if (s.Blip != null)
                {
                    s.Blip.Visible = offered;
                    if (offered) s.Blip.Label = QuestWorld.FactionName(q.Arc) + ": " + q.Title + " (" + s.Def.Name + ")" + (q.Id == _active ? " [restart]" : "");
                }
            }
        }

        private static int CountOffers(GiverSpot s)
        {
            int n = 0;
            for (int i = 0; i < BoardSlots; i++) if (s.Offers[i] != null) n++;
            return n;
        }

        // ------------------------------------------------------------------ boards

        /// <summary>Fills empty board slots with fresh contracts (deterministic per serial).</summary>
        private void EnsureOffers(GiverSpot s)
        {
            int rep = Game.Factions != null ? Game.Factions.Rep(s.Def.Guild) : 0;
            for (int k = 0; k < BoardSlots; k++)
            {
                if (s.Offers[k] != null && s.Offers[k].Serial == s.Slots[k]) continue;
                s.Offers[k] = null;
                for (int attempt = 0; attempt < 4 && s.Offers[k] == null; attempt++)
                {
                    if (s.Slots[k] == 0 || attempt > 0) s.Slots[k] = _nextSerial++;
                    int seed = MathUtil.Hash(s.Slots[k], s.Def.Guild.Length * 131 + 17);
                    ContractDef t = QuestLogic.PickTemplate(ContractTemplates, s.Def.Guild, rep, seed);
                    if (t == null) break;
                    try { s.Offers[k] = QuestLogic.MakeContract(t, Game.Layout, s.Pos, s.Def.Name, s.Slots[k], s.Done); }
                    catch (Exception e) { Log.Exception("Contract " + t.Id, e); }
                }
            }
        }

        /// <summary>A new in-game day posts new contracts on every board (the running one keeps its slot).</summary>
        private void CheckBoardDay()
        {
            if (Game.Atmo == null) return;
            int day = Game.Atmo.Day;
            if (_boardDay == day) return;
            bool first = _boardDay < 0;
            _boardDay = day;
            if (first) return;
            foreach (GiverSpot s in _spots)
            {
                for (int k = 0; k < BoardSlots; k++)
                {
                    if (_run != null && _run.Contract && s.Offers[k] != null && s.Offers[k].Quest == _run.Quest) continue;
                    s.Slots[k] = 0;
                    s.Offers[k] = null;
                }
            }
        }

        /// <summary>The pausing board screen: the three postings with their pay, and a button to take one.</summary>
        public void ShowBoard(GiverSpot s)
        {
            if (s == null || !CanStartNow(true)) return;
            EnsureOffers(s);
            StringBuilder sb = new StringBuilder();
            List<string> labels = new List<string>();
            List<ContractOffer> picks = new List<ContractOffer>();
            if (!string.IsNullOrEmpty(s.Def.Greeting)) sb.Append(s.Def.Name).Append(": \"").Append(s.Def.Greeting).Append("\"\n\n");
            for (int k = 0; k < BoardSlots; k++)
            {
                ContractOffer o = s.Offers[k];
                if (o == null) continue;
                picks.Add(o);
                labels.Add("TAKE " + picks.Count);
                sb.Append(picks.Count).Append(". <b>").Append(o.Title).Append("</b>  ").Append(QuestLogic.RewardText(o.Reward)).Append('\n');
            }
            if (picks.Count == 0) sb.Append("Nothing posted right now. Come back tomorrow.");
            labels.Add("LEAVE");
            if (Game.Menus == null) return;
            Game.Menus.ShowChoice(BoardTitle(s).ToUpperInvariant(), sb.ToString(), labels.ToArray(), i =>
            {
                if (i >= 0 && i < picks.Count) _pendingContract = picks[i];
            });
        }

        /// <summary>Starts a posted contract through the MissionManager.</summary>
        public bool StartContract(ContractOffer o)
        {
            if (o == null || o.Quest == null || !CanStartNow(true)) return false;
            MissionDef def = BuildMission(o.Quest, true);
            Game.Missions.StartMission(def);
            return true;
        }

        /// <summary>A finished contract leaves the board and a fresh one takes its slot.</summary>
        private void ContractFinished(QuestDef q)
        {
            GiverSpot s = SpotFor(q.Arc);
            if (s == null) return;
            s.Done++;
            for (int k = 0; k < BoardSlots; k++)
            {
                if (s.Offers[k] == null || s.Offers[k].Quest != q) continue;
                s.Offers[k] = null;
                s.Slots[k] = _nextSerial++;
            }
        }

        public int ContractsDone(string guild)
        {
            GiverSpot s = SpotFor(guild);
            return s != null ? s.Done : 0;
        }

        // ------------------------------------------------------------------ saves

        private void ResetBoards()
        {
            _nextSerial = 1;
            _boardDay = -1;
            foreach (GiverSpot s in _spots)
            {
                s.Done = 0;
                for (int k = 0; k < BoardSlots; k++)
                {
                    s.Slots[k] = 0;
                    s.Offers[k] = null;
                }
            }
        }

        private void CaptureBoards(QuestSave save)
        {
            save.NextSerial = _nextSerial;
            save.BoardDay = _boardDay;
            foreach (GiverSpot s in _spots)
            {
                save.BoardGuilds.Add(s.Def.Guild);
                for (int k = 0; k < BoardSlots; k++) save.BoardSerials.Add(s.Slots[k]);
                save.ContractsDone.Add(s.Done);
            }
        }

        private void RestoreBoards(QuestSave save)
        {
            _nextSerial = Mathf.Max(1, save.NextSerial);
            _boardDay = save.BoardDay > 0 ? save.BoardDay : -1;
            if (save.BoardGuilds == null) return;
            for (int g = 0; g < save.BoardGuilds.Count; g++)
            {
                GiverSpot s = SpotFor(save.BoardGuilds[g]);
                if (s == null) continue;
                if (save.ContractsDone != null && g < save.ContractsDone.Count) s.Done = Mathf.Max(0, save.ContractsDone[g]);
                for (int k = 0; k < BoardSlots; k++)
                {
                    int i = g * BoardSlots + k;
                    s.Slots[k] = save.BoardSerials != null && i < save.BoardSerials.Count ? Mathf.Max(0, save.BoardSerials[i]) : 0;
                    s.Offers[k] = null;
                    if (s.Slots[k] >= _nextSerial) _nextSerial = s.Slots[k] + 1;
                }
            }
        }
    }
}
