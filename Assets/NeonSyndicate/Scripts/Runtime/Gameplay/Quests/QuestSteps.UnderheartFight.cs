using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart and Bonds 04E fights: Skirmish (a finite declared encounter run by Game.Ecology04E, with a bounded
    /// creature-director fallback), UBoss (Game.UnderBosses), the Choir Mechanism (its loss resets the mechanism, not the
    /// quest) and Shortcut. A fight that ends without a confirmed victory never counts as won.
    /// </summary>
    public static partial class QuestSteps
    {
        /// <summary>Refusals (an arena still owned, the cap full) tolerated before a Skirmish falls back to the creature director.</summary>
        private const int SkirmishRetries = 5;

        // ------------------------------------------------------------------ Skirmish

        private static IEnumerator Skirmish(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            float radius = s.Radius > 0f ? s.Radius : 16f;
            string room = s.Room;
            if (!string.IsNullOrEmpty(room) && Game.Underheart != null && Game.Underheart.IsCleared(room))
            {
                if (Game.Hud != null) Game.Hud.Notify("This room is quiet: you cleared it earlier this run.", 3.5f);
                yield break;
            }
            string objective = Obj(s, "Defeat the declared group. Nothing else will come.");
            // Reach the arena first (the encounter starts when you arrive, not across the map).
            if (!AtExact(c, radius + 18f))
            {
                WorldMarker tm = exact ? ExactMarker(r, c, EnemyColor, 2f) : r.C.Marker(c, EnemyColor, 2f);
                Blip tb = r.C.BlipAt(c, EnemyColor, "Encounter");
                r.C.Objective("Go to the encounter at " + (exact ? CampaignPlacesRuntime04E.Label(s.At) : PlaceName(c)) + ".");
                r.C.Target(c);
                while (!AtExact(c, radius + 18f)) yield return null;
                if (tm != null) UnityEngine.Object.Destroy(tm.gameObject);
                r.C.RemoveBlip(tb);
                r.C.ClearTarget();
            }
            yield return SayLines(r, s.Lines);
            SkirmishHandle h = null;
            string why = "";
            if (Game.Ecology04E != null)
            {
                SkirmishSpec spec = new SkirmishSpec
                {
                    Owner = "quest:" + r.Quest.Id, RoomId = room ?? "", Center = c, Radius = radius, Reinforcements = Mathf.Clamp(s.Reinforce, 0, 4),
                    HealthScale = s.HealthScale > 0f ? s.HealthScale : 1f, DamageScale = s.DamageScale > 0f ? s.DamageScale : 1f
                };
                foreach (RosterEntry e in s.Roster) if (e != null && !string.IsNullOrEmpty(e.Species)) spec.Roster.Add(new SkirmishEntry { Species = e.Species, Count = Mathf.Max(1, e.Count), Elite = e.Elite });
                for (int attempt = 0; attempt < SkirmishRetries; attempt++)
                {
                    try { h = Game.Ecology04E.BeginSkirmish(spec, out why); }
                    catch (Exception ex) { Log.Exception("Skirmish " + r.Quest.Id, ex); h = null; why = "the encounter could not start"; }
                    if (h != null && !h.Failed) break;
                    h = null;
                    string o = "Waiting for the arena: " + why;
                    if (r.C.ObjectiveText != o) r.C.Objective(o);
                    yield return r.C.Wait(2f);
                }
            }
            if (h == null)
            {
                Log.Warn("Quest " + r.Quest.Id + ": the finite encounter runtime refused (" + why + "); using the creature director fallback.");
                yield return SkirmishFallback(r, s, c, radius, objective);
                if (Game.Missions == null || Game.Missions.Active != r.C) yield break;
            }
            else
            {
                SkirmishHandle handle = h;
                r.Cleanups.Add(() => { if (!handle.Done && handle.Abort != null) handle.Abort(); });
                r.C.Objective(objective);
                float t = 0f;
                while (!h.Done)
                {
                    if (h.Failed)
                    {
                        if (Game.Hud != null) Game.Hud.ClearCounter();
                        FailQuest(r, "The encounter was called off: " + (string.IsNullOrEmpty(h.Status) ? "unknown reason" : h.Status) + " Restart from the checkpoint.");
                        yield break;
                    }
                    if (Game.Hud != null && h.Total > 0) Game.Hud.SetCounter("DEFEATED", Mathf.Clamp(h.Total - h.Alive, 0, h.Total), h.Total);
                    t -= Time.deltaTime;
                    if (t <= 0f)
                    {
                        t = 0.5f;
                        r.C.Target(c);
                    }
                    yield return null;
                }
                if (Game.Hud != null) Game.Hud.ClearCounter();
            }
            r.C.ClearTarget();
            if (!string.IsNullOrEmpty(room) && Game.Underheart != null)
            {
                try { Game.Underheart.MarkCleared(room); }
                catch (Exception e) { Log.Exception("Mark cleared " + room, e); }
            }
            if (Game.Hud != null) Game.Hud.Notify("Quiet again. The room stays clear for this run.", 3.5f);
        }

        /// <summary>
        /// The bounded fallback when the finite encounter runtime is missing: the declared roster through the creature director
        /// (tagged for this quest, cleaned up with it), then the declared reinforcements, announced. Never more than declared.
        /// </summary>
        private static IEnumerator SkirmishFallback(QuestRun r, QuestStep s, Vector3 c, float radius, string objective)
        {
            List<CharacterBase> live = new List<CharacterBase>();
            List<Blip> blips = new List<Blip>();
            System.Random rand = new System.Random(r.Salt + r.Step * 19);
            int total = 0;
            foreach (RosterEntry e in s.Roster)
            {
                if (e == null) continue;
                CreatureSpecies sp = CreatureSpeciesDb.Get(e.Species);
                if (sp == null) continue;
                int n = Mathf.Clamp(e.Count, 1, 8);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = QuestWorld.InRing(c, radius * 0.25f, radius * 0.7f, rand);
                    CharacterBase b = r.Spawn(e.Species, p, QuestWorld.YawToward(p, Game.PlayerPosition), true, e.Elite, s.HealthScale, s.DamageScale, null);
                    if (b != null)
                    {
                        live.Add(b);
                        blips.Add(EnemyBlip(r, b));
                        total++;
                    }
                    yield return null;
                }
            }
            r.C.Objective(objective);
            bool reinforced = s.Reinforce <= 0;
            while (true)
            {
                int alive = QuestWorld.CountAlive(live);
                if (Game.Hud != null && total > 0) Game.Hud.SetCounter("DEFEATED", total - alive, total);
                if (alive == 0)
                {
                    if (reinforced) break;
                    reinforced = true;
                    string sp = s.Roster.Count > 0 ? s.Roster[0].Species : "";
                    if (Game.Hud != null) Game.Hud.Banner("REINFORCEMENTS", s.Reinforce + " more, and that is all of them.", EnemyColor, 3f);
                    for (int i = 0; i < s.Reinforce && !string.IsNullOrEmpty(sp); i++)
                    {
                        Vector3 p = QuestWorld.InRing(c, radius * 0.5f, radius, rand);
                        CharacterBase b = r.Spawn(sp, p, QuestWorld.YawToward(p, Game.PlayerPosition), true, false, s.HealthScale, s.DamageScale, null);
                        if (b != null)
                        {
                            live.Add(b);
                            blips.Add(EnemyBlip(r, b));
                            total++;
                        }
                        yield return null;
                    }
                    continue;
                }
                TargetNearest(r, live, c);
                yield return null;
            }
            foreach (Blip b in blips) r.C.RemoveBlip(b);
            if (Game.Hud != null) Game.Hud.ClearCounter();
        }

        // ------------------------------------------------------------------ UBoss

        private static IEnumerator UBoss(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            float yaw = 0f;
            Vector3 ignore;
            string why;
            if (CampaignRules04E.IsPlaceToken(s.At)) CampaignPlacesRuntime04E.TryResolve(s.At, out ignore, out yaw, out why);
            float radius = s.Radius > 0f ? s.Radius : 22f;
            string title = !string.IsNullOrEmpty(s.Name) ? s.Name : UnderBossIds.Title(s.Boss);
            UnderheartBosses bosses = Game.UnderBosses;
            if (bosses == null)
            {
                FailQuest(r, "The Underheart bosses did not start (see STATUS). Your progress is kept; restart from the checkpoint later.");
                yield break;
            }
            if (bosses.EverDefeated(s.Boss))
            {
                if (Game.Hud != null) Game.Hud.Notify(title + " has already fallen. Its arena is quiet.", 4f);
                yield break;
            }
            if (!AtExact(c, radius + 10f))
            {
                WorldMarker tm = exact ? ExactMarker(r, c, EnemyColor, 3f) : r.C.Marker(c, EnemyColor, 3f);
                Blip tb = r.C.BlipAt(c, EnemyColor, title);
                r.C.Objective("Go to " + title + "'s arena. Its boundary glows once you cross it.");
                r.C.Target(c);
                while (!AtExact(c, radius + 10f)) yield return null;
                if (tm != null) UnityEngine.Object.Destroy(tm.gameObject);
                r.C.RemoveBlip(tb);
            }
            yield return SayLines(r, s.Lines);
            BossFightHandle h = null;
            why = "";
            try { h = bosses.Begin(s.Boss, c, yaw, radius, out why); }
            catch (Exception e) { Log.Exception("Underheart boss " + s.Boss, e); why = "the fight could not start (see the Console)"; }
            if (h == null || (h.Failed && !h.Done))
            {
                FailQuest(r, title + " could not be fought: " + (string.IsNullOrEmpty(why) ? (h != null ? h.Status : "unknown reason") : why) + " Restart from the checkpoint.");
                yield break;
            }
            BossFightHandle handle = h;
            r.Cleanups.Add(() => { if (!handle.Done && handle.Abort != null) handle.Abort(); });
            r.C.Objective(Obj(s, "Defeat " + title + "."));
            float t = 0f;
            while (!h.Done)
            {
                if (h.Failed)
                {
                    FailQuest(r, title + ": " + (string.IsNullOrEmpty(h.Status) ? "the fight ended." : h.Status) + " Restart from the checkpoint; your rewards so far are kept.");
                    yield break;
                }
                t -= Time.deltaTime;
                if (t <= 0f)
                {
                    t = 0.5f;
                    r.C.Target(c);
                }
                yield return null;
            }
            r.C.ClearTarget();
            yield return r.C.Wait(1.5f);
        }

        // ------------------------------------------------------------------ Mechanism

        private static IEnumerator Mechanism(QuestRun r, QuestStep s)
        {
            string flag = CampaignRules04E.MechanismFlag(s.Mechanism);
            if (r.Book != null && r.Book.Flag(flag))
            {
                if (Game.Hud != null) Game.Hud.Notify("That mechanism is already silent.", 3f);
                yield break;
            }
            UnderheartBosses bosses = Game.UnderBosses;
            if (bosses == null)
            {
                FailQuest(r, "The Choir mechanisms need the Underheart boss framework, which did not start (see STATUS).");
                yield break;
            }
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            float yaw = 0f;
            Vector3 ignore;
            string why;
            if (CampaignRules04E.IsPlaceToken(s.At)) CampaignPlacesRuntime04E.TryResolve(s.At, out ignore, out yaw, out why);
            if (!AtExact(c, 12f))
            {
                WorldMarker tm = ExactMarker(r, c, MarkerColor, 2f);
                Blip tb = r.C.BlipAt(c, MarkerColor, "Mechanism");
                r.C.Objective("Go to the mechanism.");
                r.C.Target(c);
                while (!AtExact(c, 12f)) yield return null;
                if (tm != null) UnityEngine.Object.Destroy(tm.gameObject);
                r.C.RemoveBlip(tb);
                r.C.ClearTarget();
            }
            yield return SayLines(r, s.Lines);
            MechanismHandle h = null;
            why = "";
            try { h = bosses.BeginMechanism(s.Mechanism, c, yaw, out why); }
            catch (Exception e) { Log.Exception("Mechanism " + s.Mechanism, e); why = "it could not start (see the Console)"; }
            if (h == null || (h.Failed && !h.Done))
            {
                FailQuest(r, "The mechanism could not start: " + why + " Restart from the checkpoint; finished mechanisms stay finished.");
                yield break;
            }
            MechanismHandle current = h;
            r.Cleanups.Add(() => { if (current != null && !current.Done && current.Abort != null) current.Abort(); });
            int resets = 0;
            while (!current.Done)
            {
                if (current.Failed)
                {
                    // Losing a mechanism resets the mechanism, never the quest.
                    resets++;
                    if (Game.Hud != null) Game.Hud.Notify("The mechanism reset. It starts again in a moment.", 3f);
                    yield return r.C.Wait(3f);
                    MechanismHandle next = null;
                    try { next = bosses.BeginMechanism(s.Mechanism, c, yaw, out why); }
                    catch (Exception e) { Log.Exception("Mechanism restart " + s.Mechanism, e); }
                    if (next == null || (next.Failed && !next.Done))
                    {
                        FailQuest(r, "The mechanism could not restart: " + why + " Restart from the checkpoint.");
                        yield break;
                    }
                    current = next;
                    continue;
                }
                string o = !string.IsNullOrEmpty(current.Objective) ? current.Objective : "Work the mechanism.";
                if (r.C.ObjectiveText != o) r.C.Objective(o);
                yield return null;
            }
            if (r.Book != null) r.Book.SetFlag(flag);
            if (Game.Hud != null) Game.Hud.Notify("Mechanism silenced" + (resets > 0 ? " (after " + resets + " reset" + (resets == 1 ? "" : "s") + ")" : "") + ". It stays silent.", 4f);
        }

        // ------------------------------------------------------------------ Shortcut

        private static IEnumerator Shortcut(QuestRun r, QuestStep s)
        {
            UnderheartWorld u = Game.Underheart;
            if (u == null)
            {
                if (Game.Hud != null) Game.Hud.Notify("Shortcuts are unavailable in this build; the quest continues.", 3.5f);
                yield break;
            }
            if (u.ShortcutOpen(s.Shortcut)) yield break;
            if (!string.IsNullOrEmpty(s.At))
            {
                Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
                string prompt = !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : "Press E to open the shortcut";
                if (r.LastExact) yield return ExactPressE(r, p, prompt, Obj(s, "Open the return shortcut."), null, true);
                else yield return r.C.PressE(p, prompt, Obj(s, "Open the return shortcut."));
            }
            bool opened = false;
            try { opened = u.UnlockShortcut(s.Shortcut, true); }
            catch (Exception e) { Log.Exception("Shortcut " + s.Shortcut, e); }
            if (Game.Hud != null) Game.Hud.Notify(!string.IsNullOrEmpty(s.Text) ? s.Text : (opened ? "Shortcut open. It stays open." : "The shortcut would not move; the long way still works."), 4f);
        }
    }
}
