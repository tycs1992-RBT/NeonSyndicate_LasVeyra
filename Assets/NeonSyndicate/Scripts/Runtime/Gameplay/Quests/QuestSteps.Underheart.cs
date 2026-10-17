using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart and Bonds 04E quest steps for builds, rewards, the crystal focus, bonds and decisions: Build, Claim,
    /// Equip, Rescue, BondRank, Care, Assist, Decide, Branch and Conduit. Every step degrades gracefully: a missing system
    /// either skips an optional beat with a plain notice or fails the quest with the reason (restartable, never lost).
    /// </summary>
    public static partial class QuestSteps
    {
        private const string ClaimWhere = "Claim it on the BUILD page, at the Veyra Lofts ledger desk or at the Lantern Refuge ledger.";

        // ------------------------------------------------------------------ Build

        /// <summary>Waits for a build suggestion chosen at a fitting station (Wardkeeper, Conduit or Riftstrider; reversible later for free).</summary>
        private static IEnumerator Build(QuestRun r, QuestStep s)
        {
            Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            BuildSystem b = Game.Builds;
            if (b == null)
            {
                if (Game.Hud != null) Game.Hud.Notify("Build fittings are unavailable in this build (see STATUS). The quest continues without them.", 5f);
                yield break;
            }
            bool chosen = false, opened = false;
            Action<string> onChosen = preset => chosen = true;
            b.PresetChosen += onChosen;
            r.Cleanups.Add(() => { if (b != null) b.PresetChosen -= onChosen; });
            string objective = Obj(s, "Choose a build at the fitting worktable: Wardkeeper, Conduit or Riftstrider.");
            string prompt = !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : "Press E to open the fitting worktable";
            Interactable it = QuestPrompt("Quest fitting", p, 2.2f, prompt, () =>
            {
                opened = true;
                string why;
                if (Game.Builds != null && !Game.Builds.OpenFitting(p, out why) && Game.Hud != null) Game.Hud.Notify(why, 3.5f);
            }, r.C.M.transform);
            r.C.Objects.Add(it.gameObject);
            WorldMarker m = exact ? ExactMarker(r, p, MarkerColor, 1.1f) : r.C.Marker(p, MarkerColor, 1.1f);
            Blip bl = r.C.BlipAt(p, MarkerColor, objective);
            r.C.Objective(objective);
            r.C.Target(p);
            while (!chosen)
            {
                // A player who already has a build and closes the picker keeps it: the choice stays reversible at any fitting.
                if (opened && Game.IsPlaying && Game.Builds != null && !string.IsNullOrEmpty(Game.Builds.Preset)) break;
                yield return null;
            }
            b.PresetChosen -= onChosen;
            if (it != null) UnityEngine.Object.Destroy(it.gameObject);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            r.C.RemoveBlip(bl);
            r.C.ClearTarget();
            if (Game.Hud != null)
            {
                string preset = Game.Builds != null ? Game.Builds.Preset : "";
                Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : "Build set" + (preset.Length > 0 ? ": " + Cap(preset) : "") + ". Change it at any fitting station, free.", 5f);
            }
            yield return SayLines(r, s.Lines);
        }

        private static string Cap(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        // ------------------------------------------------------------------ Claim

        /// <summary>
        /// Grants a durable reward once per token and tries to claim it at once. Never blocks: a full inventory or a choice
        /// reward stays pending in the ledger (the HUD says where to claim), and a missing ledger queues the grant in the save.
        /// </summary>
        private static void ClaimAction(QuestRun r, QuestStep s)
        {
            string token = CampaignRules04E.ClaimToken(r.Quest.Id, s);
            string label = CampaignRules04E.RewardLabel(s.Reward);
            string source = r.Quest.Title;
            BuildSystem b = Game.Builds;
            if (b == null || b.Ledger == null)
            {
                if (r.Book != null) r.Book.QueueLedgerGrant(s.Reward, token, source);
                if (Game.Hud != null) Game.Hud.Notify("Reward recorded: " + label + ". It is delivered as soon as the reward ledger is running.", 5f);
                return;
            }
            if (b.Ledger.IsClaimed(token)) return;
            string msg = "";
            bool ok = false;
            try { ok = b.Ledger.GrantAndClaim(s.Reward, token, source, out msg); }
            catch (Exception e) { Log.Exception("Quest claim " + s.Reward, e); }
            // A ledger that could not record it (not running yet, or refused) gets it again later; nothing is lost.
            bool held = false;
            try { held = b.Ledger.Has(token); }
            catch (Exception e) { Log.Exception("Quest claim check " + s.Reward, e); }
            if (!held && r.Book != null) r.Book.QueueLedgerGrant(s.Reward, token, source);
            if (Game.Hud == null) return;
            if (ok) Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : (!string.IsNullOrEmpty(msg) ? msg : label + " claimed."), 5f);
            else Game.Hud.Notify((string.IsNullOrEmpty(msg) ? label + " is waiting for you." : msg) + " " + ClaimWhere, 6f);
        }

        // ------------------------------------------------------------------ Equip

        private static bool HoldingFocus()
        {
            return Game.Player != null && Game.Player.Weapons != null && WeaponController.BehaviorOf(Game.Player.Weapons.Current) != CrystalBehavior.None;
        }

        private static bool OwnsFocus()
        {
            LootArmory l = Game.Loot;
            if (l == null) return false;
            foreach (LootGun g in l.Slots) if (CrystalForgeRules.IsFocus(g)) return true;
            foreach (LootGun g in l.Backpack) if (CrystalForgeRules.IsFocus(g)) return true;
            return false;
        }

        /// <summary>
        /// A crystal focus in hand (the real 04D foci). Guides to the resonance case, never grants anything itself; with
        /// Calibrate it also waits for the 04D practice-lane calibration (done already counts at once).
        /// </summary>
        private static IEnumerator Equip(QuestRun r, QuestStep s)
        {
            string at = string.IsNullOrEmpty(s.At) ? CampaignRules04E.ArsenalCase : s.At;
            if (Game.Arsenal == null && !OwnsFocus())
            {
                if (Game.Hud != null) Game.Hud.Notify("The crystal arsenal is unavailable in this build (see STATUS), so the focus practice is skipped.", 5f);
                yield break;
            }
            // A focus that was claimed and later sold or scrapped is not handed out again: the practice is skipped honestly.
            if (!OwnsFocus() && Game.Arsenal != null && Game.Arsenal.HasEntitlement(CrystalEntitlementRules.StarterToken) && Game.Arsenal.PendingCount == 0)
            {
                if (Game.Hud != null) Game.Hud.Notify("You no longer carry a crystal focus (it was sold or scrapped), so the focus practice is skipped.", 5f);
                yield break;
            }
            WorldMarker m = null;
            Blip b = null;
            float refresh = 0f;
            while (!HoldingFocus())
            {
                refresh -= Time.deltaTime;
                if (refresh <= 0f)
                {
                    refresh = 0.5f;
                    bool owns = OwnsFocus();
                    string o = owns ? "Equip your crystal focus: open the weapon wheel (Tab, or hold LB) and pick it." : Obj(s, "Take a crystal focus from the gold resonance case by the Veyra Lofts door.");
                    if (r.C.ObjectiveText != o) r.C.Objective(o);
                    if (!owns)
                    {
                        Vector3 p;
                        float yaw;
                        string why;
                        if (m == null && CampaignPlacesRuntime04E.TryResolve(at, out p, out yaw, out why))
                        {
                            m = ExactMarker(r, p, MarkerColor, 1.2f);
                            b = r.C.BlipAt(p, MarkerColor, "Resonance case");
                            r.C.Target(p);
                        }
                    }
                    else if (m != null)
                    {
                        UnityEngine.Object.Destroy(m.gameObject);
                        m = null;
                        r.C.RemoveBlip(b);
                        b = null;
                        r.C.ClearTarget();
                    }
                }
                yield return null;
            }
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (Game.Hud != null) Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : "Crystal focus in hand: the HUD names it, and its heat bar sits by the crosshair.", 4.5f);
            if (!s.Calibrate || Game.Arsenal == null) yield break;
            if (Game.Arsenal.CalibrationDone)
            {
                if (Game.Hud != null) Game.Hud.Notify("Already calibrated: the case remembers, so nothing is granted twice.", 4f);
                yield break;
            }
            Vector3 lane;
            float lyaw;
            string lwhy;
            if (!Game.Arsenal.LanePlaced || !CampaignPlacesRuntime04E.TryResolve(CampaignRules04E.ArsenalLane, out lane, out lyaw, out lwhy))
            {
                if (Game.Hud != null) Game.Hud.Notify("The practice lane is not set up here, so calibration is skipped.", 4f);
                yield break;
            }
            WorldMarker lm = ExactMarker(r, lane, MarkerColor, 1.2f);
            Blip lb = r.C.BlipAt(lane, MarkerColor, "Practice lane");
            r.C.Objective("Step onto the practice lane and hit all three targets with the focus.");
            r.C.Target(lane);
            while (Game.Arsenal != null && !Game.Arsenal.CalibrationDone) yield return null;
            if (lm != null) UnityEngine.Object.Destroy(lm.gameObject);
            r.C.RemoveBlip(lb);
            r.C.ClearTarget();
        }

        // ------------------------------------------------------------------ Rescue, BondRank, Care

        /// <summary>The bonds rescue sequence (clamps, warning distance, tuning, naming), driven by Game.Bonds.BeginRescue.</summary>
        private static IEnumerator Rescue(QuestRun r, QuestStep s)
        {
            BondSystem bonds = Game.Bonds;
            string title = !string.IsNullOrEmpty(s.Name) ? s.Name : BondIds.Title(s.Bond);
            if (bonds == null)
            {
                FailQuest(r, "Bonded companions did not start (see STATUS). Restart this chapter once they are available.");
                yield break;
            }
            if (bonds.Owns(s.Bond))
            {
                if (Game.Hud != null) Game.Hud.Notify(BondIds.Title(s.Bond) + " is already bonded with you.", 3.5f);
                yield return SayLines(r, s.Lines);
                yield break;
            }
            Vector3 site = r.Place(s.At, s.OffsetX, s.OffsetZ);
            Vector3 ps;
            float yaw = 0f;
            string why;
            if (CampaignRules04E.IsPlaceToken(s.At)) CampaignPlacesRuntime04E.TryResolve(s.At, out ps, out yaw, out why);
            List<Vector3> clamps = new List<Vector3>();
            foreach (string p in s.Points)
            {
                Vector3 c;
                float cy;
                if (CampaignPlacesRuntime04E.TryResolve(p, out c, out cy, out why)) clamps.Add(c);
            }
            RescueHandle h = null;
            try { h = bonds.BeginRescue(s.Bond, site, yaw, clamps.Count, clamps.ToArray()); }
            catch (Exception e) { Log.Exception("Rescue " + s.Bond, e); }
            if (h == null || (h.Failed && !h.Done))
            {
                FailQuest(r, "The rescue could not begin: " + (h != null && !string.IsNullOrEmpty(h.Objective) ? h.Objective : "see the Console") + " Restart from the checkpoint.");
                yield break;
            }
            RescueHandle handle = h;
            r.Cleanups.Add(() => { if (!handle.Done && handle.Abort != null) handle.Abort(); });
            Blip b = r.C.BlipAt(site, MarkerColor, title);
            string fallback = Obj(s, "Free the " + title + ": release the clamps, keep your distance when it flares, then tune it calm.");
            float t = 0f;
            while (!h.Done)
            {
                if (h.Failed)
                {
                    r.C.RemoveBlip(b);
                    FailQuest(r, "The rescue stopped: " + (string.IsNullOrEmpty(h.Objective) ? "unknown reason" : h.Objective) + " Restart from the checkpoint.");
                    yield break;
                }
                string o = !string.IsNullOrEmpty(h.Objective) ? h.Objective : fallback;
                if (r.C.ObjectiveText != o) r.C.Objective(o);
                t -= Time.deltaTime;
                if (t <= 0f)
                {
                    t = 0.5f;
                    r.C.Target(h.Position != Vector3.zero ? h.Position : site);
                }
                yield return null;
            }
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (Game.Hud != null)
            {
                string name = h.Result != null && !string.IsNullOrEmpty(h.Result.Name) ? h.Result.Name : BondIds.DefaultName(s.Bond);
                Game.Hud.Banner("BONDED", name + " the " + BondIds.Title(s.Bond) + " chose to come with you.", CastColor, 4.5f);
            }
            yield return SayLines(r, s.Lines);
        }

        private static void BondRankAction(QuestRun r, QuestStep s)
        {
            if (Game.Bonds == null) return;
            CompanionRecord c = Game.Bonds.FindArchetype(s.Bond);
            if (c == null)
            {
                Log.Warn("Quest " + r.Quest.Id + ": BondRank for " + s.Bond + ", which is not on the roster.");
                return;
            }
            bool raised = false;
            try { raised = Game.Bonds.RaiseRank(c.Uid, s.Rank, r.Quest.Title); }
            catch (Exception e) { Log.Exception("Bond rank " + s.Bond, e); }
            if (!raised || Game.Hud == null) return;
            int rank = Mathf.Clamp(s.Rank, 0, BondIds.RankNames.Length - 1);
            Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : (string.IsNullOrEmpty(c.Name) ? BondIds.Title(s.Bond) : c.Name) + " is now " + BondIds.RankNames[rank] + ".", 5f);
        }

        /// <summary>
        /// A care or tuning moment with the active companion at a station (bounded by the bonds' own cooldown). The step's
        /// prompt carries the quest priority, so it wins over the always-on care station that stands at the same point; its
        /// press performs the real care through Game.Bonds.CareAt and completes the step only when the care happened (a
        /// refusal such as "not during a fight" or "no companion with you" is shown and the step keeps waiting). A player
        /// with no companion at all is never stuck: the step says so and the quest continues.
        /// </summary>
        private static IEnumerator Care(QuestRun r, QuestStep s)
        {
            Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            if (Game.Bonds == null)
            {
                if (Game.Hud != null) Game.Hud.Notify("Companion care is unavailable in this build (see STATUS); the quest continues.", 4f);
                yield break;
            }
            bool cared = false;
            string objective = Obj(s, "Tend your companion at the care station.");
            Interactable it = QuestPrompt("Quest care", p, 2.2f, "", () =>
            {
                BondSystem bonds = Game.Bonds;
                if (bonds == null || bonds.Roster.Count == 0)
                {
                    if (Game.Hud != null) Game.Hud.Notify("No companion to tend right now; the quest continues without the care moment.", 4f);
                    cared = true;
                    return;
                }
                string msg = "";
                bool ok = false;
                try { ok = bonds.CareAt(p, out msg); }
                catch (Exception e) { msg = ""; Log.Exception("Companion care", e); }
                if (Game.Hud != null && !string.IsNullOrEmpty(msg)) Game.Hud.Notify(msg, 4f);
                if (ok) cared = true;
            }, r.C.M.transform);
            it.DynamicPrompt = () =>
            {
                if (!string.IsNullOrEmpty(s.Prompt)) return s.Prompt;
                CompanionRecord a = Game.Bonds != null ? Game.Bonds.Active : null;
                return a != null ? "Press E to tend " + (string.IsNullOrEmpty(a.Name) ? BondIds.Title(a.Archetype) : a.Name) : "Press E to use the care station";
            };
            r.C.Objects.Add(it.gameObject);
            WorldMarker m = exact ? ExactMarker(r, p, MarkerColor, 1f) : r.C.Marker(p, MarkerColor, 1f);
            Blip b = r.C.BlipAt(p, MarkerColor, objective);
            r.C.Target(p);
            float t = 0f;
            while (!cared)
            {
                t -= Time.deltaTime;
                if (t <= 0f)
                {
                    t = 0.5f;
                    string o = Game.Bonds != null && Game.Bonds.Active == null && Game.Bonds.Roster.Count > 0
                        ? "Bring a companion along (BONDS page, or tap N to call it), then tend it at the care station." : objective;
                    if (r.C.ObjectiveText != o) r.C.Objective(o);
                }
                yield return null;
            }
            if (it != null) UnityEngine.Object.Destroy(it.gameObject);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (!string.IsNullOrEmpty(s.Text) && Game.Hud != null) Game.Hud.Notify(s.Text, 4f);
            yield return SayLines(r, s.Lines);
        }

        // ------------------------------------------------------------------ Assist

        /// <summary>
        /// The companion's exploration assist near a place, or the slower tool alternative (stand at the spot and work it by
        /// hand for Seconds). Both always work, so a resting or different companion never blocks the way.
        /// </summary>
        private static IEnumerator Assist(QuestRun r, QuestStep s)
        {
            Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            float radius = s.Radius > 0f ? s.Radius : 8f;
            float toolSeconds = s.Seconds > 0f ? s.Seconds : 8f;
            float start = Time.time;
            bool assisted = false, tool = false, toolActive = false;
            float toolT = 0f;
            BondSystem bonds = Game.Bonds;
            Action<Vector3> onAssist = point =>
            {
                if (MathUtil.FlatDistance(point, p) <= radius && Mathf.Abs(point.y - p.y) < 6f) assisted = true;
            };
            if (bonds != null)
            {
                bonds.ExplorationAssist += onAssist;
                r.Cleanups.Add(() => { if (bonds != null) bonds.ExplorationAssist -= onAssist; });
            }
            string objective = Obj(s, "Find the way: your companion's assist (U) near the marker, or work it out by hand.");
            Interactable it = QuestPrompt("Quest tool", p, 2.4f, !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : "Press E to work it out by hand",
                () => { toolActive = true; toolT = 0f; }, r.C.M.transform);
            it.Condition = () => !toolActive;
            r.C.Objects.Add(it.gameObject);
            WorldMarker m = exact ? ExactMarker(r, p, MarkerColor, Mathf.Min(radius, 3f)) : r.C.Marker(p, MarkerColor, Mathf.Min(radius, 3f));
            Blip b = r.C.BlipAt(p, MarkerColor, objective);
            r.C.Objective(objective);
            r.C.Target(p);
            while (!assisted && !tool)
            {
                if (bonds != null && bonds.LastExplorationAssistTime > start && MathUtil.FlatDistance(bonds.LastExplorationAssistPoint, p) <= radius
                    && Mathf.Abs(bonds.LastExplorationAssistPoint.y - p.y) < 6f) assisted = true;
                if (toolActive)
                {
                    if (AtExact(p, 3.2f))
                    {
                        toolT += Time.deltaTime;
                        if (Game.Hud != null) Game.Hud.SetProgress("Working it out by hand...", Mathf.Clamp01(toolT / toolSeconds));
                        if (toolT >= toolSeconds) tool = true;
                    }
                    else
                    {
                        toolActive = false;
                        if (Game.Hud != null) Game.Hud.Notify("Stay at the marker to finish by hand, or let your companion find it (U).", 3f);
                    }
                }
                yield return null;
            }
            if (bonds != null) bonds.ExplorationAssist -= onAssist;
            if (it != null) UnityEngine.Object.Destroy(it.gameObject);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
            if (Game.Hud != null)
            {
                CompanionRecord a = bonds != null ? bonds.Active : null;
                string who = a != null && !string.IsNullOrEmpty(a.Name) ? a.Name : "Your companion";
                Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : (assisted ? who + " found the way." : "Found the slow way. It works."), 4f);
            }
            yield return SayLines(r, s.Lines);
        }

        // ------------------------------------------------------------------ Decide, Branch, Conduit

        /// <summary>
        /// Records a campaign decision. Without Options it stores Value at once. With Options it asks (a pausing choice) and
        /// stores the picked option's Value, jumping to its Phase when the options lead to phases. A decision already made
        /// (a resumed checkpoint, a replay) stands: no menu, and the quest follows the earlier answer. A Reask decision (an
        /// approach such as fight or bypass) is asked again on every attempt and the new answer overwrites the old one, so
        /// a player who failed one way can try the other.
        /// </summary>
        private static IEnumerator Decide(QuestRun r, QuestStep s)
        {
            QuestBook book = r.Book;
            if (s.Options.Count == 0)
            {
                if (book != null) book.SetChoice(s.Key, s.Value, false);
                yield break;
            }
            bool jumps = QuestLogic.EndsPhase(s);
            string existing = book != null ? book.Choice(s.Key) : "";
            if (QuestRuntimeRules04E.KeepsEarlierChoice(s.Reask, existing))
            {
                foreach (ChoiceOption o in s.Options)
                {
                    if (o.Value != existing) continue;
                    if (Game.Hud != null) Game.Hud.Notify("Your earlier choice stands: " + o.Label + ".", 3.5f);
                    if (jumps) r.Jump = o.Phase;
                    yield break;
                }
            }
            List<int> shown = HasConditions(s.Options) && book != null ? CampaignRules04E.VisibleOptions(s.Options, book.Facts()) : AllOptions(s.Options.Count);
            string[] labels = new string[shown.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = s.Options[shown[i]].Label.ToUpperInvariant();
            int pick = -1;
            if (Game.Menus != null) Game.Menus.ShowChoice(!string.IsNullOrEmpty(s.Name) ? s.Name : r.Quest.Title.ToUpperInvariant(), s.Text, labels, i => pick = i);
            else pick = 0;
            while (pick < 0) yield return null;
            ChoiceOption chosen = s.Options[shown[Mathf.Clamp(pick, 0, shown.Count - 1)]];
            if (book != null) book.SetChoice(s.Key, chosen.Value, s.Reask);
            if (jumps) r.Jump = chosen.Phase;
        }

        /// <summary>Jumps to the first option whose condition holds (the validator makes the last one "else").</summary>
        private static void BranchAction(QuestRun r, QuestStep s)
        {
            if (s.Options.Count == 0) return;
            CampaignFacts f = r.Book != null ? r.Book.Facts() : new CampaignFacts();
            string target = CampaignRules04E.BranchTarget(s.Options, f);
            r.Jump = string.IsNullOrEmpty(target) ? s.Options[s.Options.Count - 1].Phase : target;
        }

        /// <summary>Lights or dims the arrival conduit (VENUES' ArrivalScene04E). A run that lights it dims it again when it ends.</summary>
        private static void ConduitAction(QuestRun r, QuestStep s)
        {
            try
            {
                ArrivalScene04E.SetConduitLit(s.Lit);
                if (s.Lit) r.Cleanups.Add(() => ArrivalScene04E.SetConduitLit(false));
            }
            catch (Exception e) { Log.Exception("Arrival conduit", e); }
        }
    }
}
