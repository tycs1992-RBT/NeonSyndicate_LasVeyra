using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Care and growth: care stations (the treatment bench at Bondkeeper's Refuge and the caretaker at Lantern Refuge,
    /// placed once those places exist), CareAt for the quest Care step and the stations (heals, revives a downed
    /// companion, raises a Rescued companion to Bonded, gives trust only off cooldown), crystal tuning bought with real
    /// materials (previewed, spent once, refunded if applying fails), the Harmonized specialization (free to change
    /// out of combat), renaming and rank-up presentation.
    /// </summary>
    public sealed partial class BondSystem
    {
        private sealed class CareStation
        {
            public string Id;
            public Vector3 Position;
            public Interactable Prompt;
        }

        private readonly List<CareStation> _careStations = new List<CareStation>();
        private float _stationScan;
        private int _stationScans;
        /// <summary>A purchase was made in a menu: save once play resumes (materials and bonds are in the same save).</summary>
        private bool _autosaveWanted;

        private void TickAutosave()
        {
            if (!_autosaveWanted || Game.State != GameState.Playing || Game.Root == null) return;
            _autosaveWanted = false;
            Game.Root.Autosave();
        }

        private void TickCareStations()
        {
            if (_careStations.Count >= 2 || _stationScans > 90) return;
            _stationScan -= Time.unscaledDeltaTime;
            if (_stationScan > 0f) return;
            _stationScan = 2f;
            _stationScans++;
            if (FindStation("bondkeeper") == null && Game.Inhabited != null)
            {
                VenueBinding04C v = Game.Inhabited.FindVenue(VenueIds04E.Bondkeeper);
                if (v != null && v.Available && v.View != null)
                {
                    Vector3 p = v.View.WorldPoint("care_station");
                    if ((p - v.View.transform.position).sqrMagnitude > 0.04f) AddStation("bondkeeper", p, "Bondkeeper's Refuge treatment bench");
                }
            }
            if (FindStation("refuge") == null && Game.Underheart != null && Game.Underheart.Ready)
            {
                Vector3 p;
                float yaw;
                if (Game.Underheart.TryAnchor("refuge.caretaker", out p, out yaw)) AddStation("refuge", p + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 1.2f, "Lantern Refuge care corner");
            }
            if (_careStations.Count >= 2 || _stationScans > 90) ReportStatus();
        }

        private CareStation FindStation(string id)
        {
            for (int i = 0; i < _careStations.Count; i++) if (_careStations[i].Id == id) return _careStations[i];
            return null;
        }

        private void AddStation(string id, Vector3 pos, string title)
        {
            GameObject go = new GameObject("Care Station - " + title);
            go.layer = Layers.IgnoreRaycast;
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            Interactable it = go.AddComponent<Interactable>();
            it.Radius = 2.4f;
            it.HeightTolerance = 2.4f;
            it.Condition = () => Active != null && Game.Player != null && !Game.Player.InVehicle;
            it.DynamicPrompt = () => CarePrompt();
            Vector3 at = pos;
            it.Use = () => UseStation(at);
            _careStations.Add(new CareStation { Id = id, Position = pos, Prompt = it });
            ReportStatus();
        }

        /// <summary>
        /// A care station: with one companion it is cared for at once; with more, a short pausing choice offers care for
        /// the active one or switching to another bonded companion (a care station is a safe place to swap).
        /// </summary>
        private void UseStation(Vector3 station)
        {
            CompanionRecord r = Active;
            int others = 0;
            for (int i = 0; i < _data.Companions.Count; i++)
                if (_data.Companions[i] != r && BondIds.Has(_data.Companions[i].Archetype)) others++;
            if (others == 0 || Game.Menus == null)
            {
                string msg;
                CareAt(station, out msg);
                if (!string.IsNullOrEmpty(msg)) Say(msg, 4f, false);
                return;
            }
            List<string> options = new List<string>();
            List<int> uids = new List<int>();
            options.Add(r != null ? "Care for " + NameOf(r) : "Care");
            uids.Add(0);
            System.Text.StringBuilder who = new System.Text.StringBuilder();
            for (int i = 0; i < _data.Companions.Count; i++)
            {
                CompanionRecord c = _data.Companions[i];
                if (c == r || !BondIds.Has(c.Archetype)) continue;
                options.Add("Bring " + NameOf(c));
                uids.Add(c.Uid);
                who.Append(' ').Append(NameOf(c)).Append(" (").Append(BondIds.Title(c.Archetype)).Append(", ").Append(BondRules.RankName(c.Rank)).Append(").");
            }
            options.Add("Leave");
            uids.Add(-1);
            string current = r != null ? NameOf(r) : "your companion";
            Game.Menus.ShowChoice("Care station", "Care gives trust once every few minutes. Or send " + current + " home to rest and bring another companion:" + who, options.ToArray(), pick =>
            {
                if (pick < 0 || pick >= uids.Count || uids[pick] < 0) return;
                string msg;
                if (uids[pick] == 0) CareAt(station, out msg);
                else SetActive(uids[pick], out msg);
                if (!string.IsNullOrEmpty(msg)) Say(msg, 4f, false);
            });
        }

        private string CarePrompt()
        {
            CompanionRecord r = Active;
            if (r == null) return "";
            string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.North) : "E";
            string n = NameOf(r);
            if (r.State == (int)CompanionState.Downed) return "Press " + key + " to bring " + n + " back on its feet";
            if (r.CareCooldown > 0f) return "Press " + key + " to care for " + n + " (content: trust again in " + BondRules.Clock(r.CareCooldown) + ")";
            if (r.Rank == BondIds.RankStray) return "Press " + key + " to care for " + n + " and seal the bond";
            return "Press " + key + " to care for " + n;
        }

        /// <summary>A care or tuning interaction with the active companion at a station; bounded by a cooldown.</summary>
        public bool CareAt(Vector3 station, out string message)
        {
            message = "";
            CompanionRecord r = Active;
            PlayerController pl = Game.Player;
            if (r == null) { message = "You have no bonded companion with you."; return false; }
            if (pl == null) return false;
            if (pl.InVehicle) { message = "Step off your vehicle first."; return false; }
            if (r.State == (int)CompanionState.Fused) { message = NameOf(r) + " is fused with your chassis. Separate first."; return false; }
            if (CombatDirector.InCombat) { message = "Not during a fight."; return false; }
            if ((pl.transform.position - station).magnitude > 6f) { message = "Stand at the care station."; return false; }
            int before = r.Rank;
            bool wasDowned = r.State == (int)CompanionState.Downed;
            CareOutcome o = BondRules.Care(r, NameOf(r));
            if (!o.Performed)
            {
                message = o.Message;
                return false;
            }
            if (r.State == (int)CompanionState.Resting) r.State = (int)CompanionState.Following;
            if (wasDowned)
            {
                // The downed body may still be lying there or already gone home: either way it comes back whole.
                DespawnBody();
                _revealT = 0f;
            }
            else if (_body != null)
            {
                if ((_body.transform.position - station).magnitude > 8f || _body.Hidden)
                {
                    string why;
                    _body.TryRecall(true, out why);
                }
                EndAssist();
                _body.PlayAction(CreatureAction.Howl, 1.4f, 0.35f, 0.7f, null);
                _body.SetLook(pl.HeadPosition);
                _body.Glow(1.5f);
                if (Game.Fx != null) Game.Fx.CrystalBurst(_body.ChestPosition, ProfileOf(r).Accent, 12, 0.45f);
            }
            Underheart04ESounds.Play(UhSfx.CareChime, station + Vector3.up, 1f, 1f);
            message = o.Message;
            if (r.Rank != before) OnRankChanged(r, before);
            ReportStatus();
            return true;
        }

        private void OnRankChanged(CompanionRecord r, int before)
        {
            if (r == null || r.Rank == before) return;
            string n = NameOf(r);
            string unlock = BondRules.RankUnlock(r.Rank);
            if (r.Rank == BondIds.RankHarmonized && string.IsNullOrEmpty(r.Specialization)) unlock += " Open the BONDS page to choose.";
            if (Game.Hud != null) Game.Hud.Banner(n.ToUpperInvariant() + " IS " + BondRules.RankName(r.Rank).ToUpperInvariant(), unlock, ProfileOf(r).Accent, 4.5f);
            Underheart04ESounds.Play2D(UhSfx.BondChime, 0.9f, 1f);
            ReportStatus();
        }

        // ================================================================== crystal tuning

        /// <summary>True when every material for the next tuning level is in the satchel.</summary>
        public bool CanAffordTuning(CompanionRecord r)
        {
            if (r == null || r.Tuning >= BondRules.MaxTuning || Game.Materials == null) return false;
            BondCost[] cost = BondRules.TuningCost(r.Archetype, r.Tuning + 1);
            if (cost.Length == 0) return false;
            for (int i = 0; i < cost.Length; i++) if (!Game.Materials.Has(cost[i].Material, cost[i].Count)) return false;
            return true;
        }

        /// <summary>
        /// Buys the next crystal tuning level: the previewed materials are spent once, then the level and its trust are
        /// applied; if applying fails the materials are put back. Out of combat only.
        /// </summary>
        public bool TryTune(int uid, out string message)
        {
            message = "";
            CompanionRecord r = Find(uid);
            if (r == null || !BondIds.Has(r.Archetype)) { message = "That companion is not in your roster."; return false; }
            if (r.Tuning >= BondRules.MaxTuning) { message = NameOf(r) + " is fully tuned."; return false; }
            if (CombatDirector.InCombat) { message = "Not during a fight."; return false; }
            if (Game.Materials == null) { message = "The material satchel is not available."; return false; }
            BondCost[] cost = BondRules.TuningCost(r.Archetype, r.Tuning + 1);
            List<KeyValuePair<string, int>> spend = new List<KeyValuePair<string, int>>(cost.Length);
            for (int i = 0; i < cost.Length; i++) spend.Add(new KeyValuePair<string, int>(cost[i].Material, cost[i].Count));
            if (!Game.Materials.Spend(spend))
            {
                message = "Not enough materials: " + CostText(cost, true) + ".";
                return false;
            }
            int beforeRank = r.Rank, beforeTune = r.Tuning, beforeTrust = r.Trust;
            try
            {
                r.Tuning = beforeTune + 1;
                BondRules.AddTrust(r, BondRules.TrustTuning);
                r.Health01 = Mathf.Min(1f, r.Health01 + 0.1f);
            }
            catch (System.Exception e)
            {
                r.Tuning = beforeTune;
                r.Trust = beforeTrust;
                r.Rank = beforeRank;
                for (int i = 0; i < spend.Count; i++) Game.Materials.Add(spend[i].Key, spend[i].Value, false);
                Log.Exception("Bonds tuning", e);
                message = "Tuning failed; your materials were returned.";
                return false;
            }
            message = NameOf(r) + " is tuned to level " + r.Tuning + ". " + BondRules.TuningEffect(r.Archetype, r.Tuning);
            if (_body != null && Active == r)
            {
                _body.Glow(2f);
                if (Game.Fx != null) Game.Fx.CrystalBurst(_body.ChestPosition, ProfileOf(r).Accent, 16, 0.5f);
            }
            Underheart04ESounds.Play2D(UhSfx.CareChime, 0.9f, 1.15f);
            if (r.Rank != beforeRank) OnRankChanged(r, beforeRank);
            _autosaveWanted = true;
            ReportStatus();
            return true;
        }

        /// <summary>The cost as text, with "have / need" counts when withHave is set.</summary>
        public static string CostText(BondCost[] cost, bool withHave)
        {
            if (cost == null || cost.Length == 0) return "nothing";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < cost.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(cost[i].Count).Append(' ').Append(MaterialDb.NameOf(cost[i].Material));
                if (withHave && Game.Materials != null) sb.Append(" (have ").Append(Game.Materials.Count(cost[i].Material)).Append(')');
            }
            return sb.ToString();
        }

        // ================================================================== specialization, names

        /// <summary>Chooses (or changes, free of charge out of combat) the Harmonized specialization.</summary>
        public bool SetSpecialization(int uid, string spec, out string message)
        {
            message = "";
            CompanionRecord r = Find(uid);
            if (r == null) { message = "That companion is not in your roster."; return false; }
            if (!BondRules.CanSpecialize(r.Rank)) { message = NameOf(r) + " can specialize at Harmonized."; return false; }
            if (!BondRules.IsSpecOf(r.Archetype, spec)) { message = "That specialization does not fit " + NameOf(r) + "."; return false; }
            if (CombatDirector.InCombat) { message = "Not during a fight."; return false; }
            if (r.State == (int)CompanionState.Fused) { message = NameOf(r) + " is fused. Separate first."; return false; }
            if (r.Specialization == spec) { message = NameOf(r) + " already follows " + BondRules.SpecTitle(spec) + "."; return true; }
            r.Specialization = spec;
            message = NameOf(r) + " now follows " + BondRules.SpecTitle(spec) + ". " + BondRules.SpecText(spec);
            Underheart04ESounds.Play2D(UhSfx.BondChime, 0.7f, 1.1f);
            ReportStatus();
            return true;
        }

        /// <summary>Renames a companion (sanitized; an empty name keeps the default).</summary>
        public bool Rename(int uid, string name)
        {
            CompanionRecord r = Find(uid);
            if (r == null) return false;
            r.Name = BondRules.SanitizeName(name, BondIds.DefaultName(r.Archetype));
            if (_body != null && _body.Uid == uid) _body.DisplayName = r.Name;
            ReportStatus();
            return true;
        }

        /// <summary>Opens the naming card to rename a companion (keyboard text, or keep the current name).</summary>
        public void RequestRename(int uid)
        {
            CompanionRecord r = Find(uid);
            if (r == null || _naming == null) return;
            int id = uid;
            string current = NameOf(r);
            _naming.Request("Rename " + current, "A new name for your " + BondIds.Title(r.Archetype).ToLowerInvariant() + ". It keeps every bond, rank and memory.", current, "", n => Rename(id, n));
        }

        /// <summary>
        /// A cosmetic evolution stage granted by a reward (for example a bond evolution from the reward ledger). Changes
        /// looks only (never stats), is never lowered and rebuilds the body when visible. Stage 2 also means Awakened when
        /// awaken is set. True when anything changed.
        /// </summary>
        public bool ApplyEvolution(int uid, int stage, bool awaken, string source)
        {
            CompanionRecord r = Find(uid);
            if (r == null || !BondIds.Has(r.Archetype)) return false;
            int before = r.Rank;
            bool changed = false;
            int s = Mathf.Clamp(stage, 0, 2);
            if (s > r.Evolution)
            {
                r.Evolution = s;
                changed = true;
            }
            if (awaken && BondRules.RaiseRank(r, BondIds.RankAwakened)) changed = true;
            if (!changed) return false;
            Log.Info("Bonds: " + NameOf(r) + " evolved (stage " + r.Evolution + ") from " + (source ?? "a reward") + ".");
            if (r.Rank != before) OnRankChanged(r, before);
            else if (Game.Hud != null) Game.Hud.Banner(NameOf(r).ToUpperInvariant() + " EVOLVES", "A new look earned through the bond. It changes appearance only.", ProfileOf(r).Accent, 4f);
            ReportStatus();
            return true;
        }
    }
}
