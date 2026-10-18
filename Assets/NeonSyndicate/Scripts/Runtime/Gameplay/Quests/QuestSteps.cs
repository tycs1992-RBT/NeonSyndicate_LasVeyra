using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The quest step library: one coroutine per step type, built on the MissionContext primitives the story missions
    /// use (objective, GPS target, markers, blips, dialogue, cinematics). This file holds the dispatcher, the Veil gate
    /// and the travel, talk and world-action steps; the fights and the field steps live in the other partial files.
    /// </summary>
    public static partial class QuestSteps
    {
        private static readonly Color MarkerColor = new Color(1f, 0.85f, 0.2f);

        /// <summary>Runs one step: the Veil gate first (with the Veil held open when asked), then the step itself.</summary>
        public static IEnumerator Run(QuestRun r, QuestStep s)
        {
            if (s == null) yield break;
            // 04E: a step whose place is a token (a room, an Underheart anchor, a cast post) waits briefly for it to exist,
            // then fails the quest with the reason (restartable) rather than pointing somewhere wrong.
            if (UsesPlaceToken(s)) yield return TokenGate(r, s);
            if (Game.Missions == null || Game.Missions.Active != r.C) yield break;
            if (s.Veil == "Inner" || s.Veil == "Outer") yield return VeilGate(r, s, null);
            if (s.HoldVeil) r.HoldVeil(true);
            yield return Body(r, s);
            if (s.HoldVeil) r.HoldVeil(false);
        }

        private static IEnumerator Body(QuestRun r, QuestStep s)
        {
            switch (s.Type)
            {
                case "Courier": return Courier04G(r, s);
                case "GoTo": return GoTo(r, s);
                case "Talk": return Talk(r, s);
                case "Interact": return Interact(r, s);
                case "Kill": return Kill(r, s);
                case "Boss": return Boss(r, s);
                case "Collect": return Collect(r, s);
                case "Survive": return Survive(r, s);
                case "Defend": return Defend(r, s);
                case "Escort": return Escort(r, s);
                case "Hunt": return Hunt(r, s);
                case "CloseRift": return CloseRift(r, s);
                case "Scan": return Scan(r, s);
                case "Shift": return Shift(r, s);
                case "Wait": return r.C.Wait(Mathf.Max(0.1f, s.Seconds));
                case "Spawn": return SpawnAction(r, s);
                case "Ally": return Instant(() => AllyAction(r, s));
                case "Prop": return Instant(() => PropAction(r, s));
                case "SetTime": return Instant(() => { if (Game.Atmo != null) Game.Atmo.SetTime(Mathf.Repeat(s.Hour, 24f)); });
                case "SetWeather": return Instant(() => WeatherAction(s));
                case "Give": return Instant(() => GiveAction(r, s));
                case "GiveGun": return Instant(() => GiveGunAction(r, s));
                case "AddRep": return Instant(() => { if (Game.Factions != null) Game.Factions.AddRep(s.Faction, s.Amount, r.Quest.Title); });
                case "Message": return Instant(() => MessageAction(r, s));
                case "Cinematic": return Cinematic(r, s);
                case "Choice": return Choice(r, s);
                case "Venue": return Venue(r, s);
                // ---- Underheart and Bonds 04E (QuestSteps.Underheart*.cs)
                case "Build": return Build(r, s);
                case "Claim": return Instant(() => ClaimAction(r, s));
                case "Equip": return Equip(r, s);
                case "Rescue": return Rescue(r, s);
                case "BondRank": return Instant(() => BondRankAction(r, s));
                case "Care": return Care(r, s);
                case "Assist": return Assist(r, s);
                case "Fuse": return Fuse(r, s);
                case "Course": return Course(r, s);
                case "Park": return Park(r, s);
                case "Skirmish": return Skirmish(r, s);
                case "UBoss": return UBoss(r, s);
                case "Mechanism": return Mechanism(r, s);
                case "Shortcut": return Shortcut(r, s);
                case "Decide": return Decide(r, s);
                case "Branch": return Instant(() => BranchAction(r, s));
                case "Conduit": return Instant(() => ConduitAction(r, s));
                case "Flag": return Instant(() => { if (r.Book != null) r.Book.SetFlag(s.Key); });
                default:
                    Log.Warn("Quest " + r.Quest.Id + ": unknown step type '" + s.Type + "' skipped.");
                    return Nothing();
            }
        }

        private static IEnumerator Nothing()
        {
            yield break;
        }

        private static IEnumerator Instant(Action a)
        {
            a();
            yield break;
        }

        // ------------------------------------------------------------------ helpers

        private static string Obj(QuestStep s, string fallback)
        {
            return !string.IsNullOrEmpty(s.Objective) ? s.Objective : fallback;
        }

        private static string[] Pairs(List<DialogueLine> lines)
        {
            string[] p = new string[lines.Count * 2];
            for (int i = 0; i < lines.Count; i++)
            {
                p[i * 2] = lines[i].Speaker;
                p[i * 2 + 1] = lines[i].Text;
            }
            return p;
        }

        private static IEnumerator SayLines(QuestRun r, List<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0) yield break;
            yield return r.C.Talk(Pairs(lines));
        }

        private static Color Accent(QuestRun r)
        {
            return QuestWorld.FactionColor(r.Quest.Arc);
        }

        private static string PlaceName(Vector3 p)
        {
            return QuestPlaces.PlaceName(Game.Layout, p);
        }

        /// <summary>True (and the objective says so) while the player is on the wrong side of the Veil for this step.</summary>
        private static bool VeilBlocked(QuestRun r, QuestStep s, string resumeObjective)
        {
            if (Game.Inner == null) return false;
            bool want = s.Veil == "Inner";
            if (s.Veil != "Inner" && s.Veil != "Outer") return false;
            if (Game.Inner.IsInner == want)
            {
                if (r.C.ObjectiveText != resumeObjective) r.C.Objective(resumeObjective);
                return false;
            }
            string o = want ? "Shift back into the Inner World to continue (hold the Third Eye)." : "Leave the Inner World to continue (hold the Third Eye).";
            if (r.C.ObjectiveText != o) r.C.Objective(o);
            return true;
        }

        /// <summary>Waits until the player is on the step's side of the Veil. objective replaces the default "Shift into..." text (a Shift step's own Objective).</summary>
        private static IEnumerator VeilGate(QuestRun r, QuestStep s, string objective)
        {
            if (Game.Inner == null) yield break;
            bool want = s.Veil == "Inner";
            if (Game.Inner.IsInner == want) yield break;
            if (want && !InnerWorld.Unlocked)
            {
                // The guild's ritual carries the player across when the Veil is not theirs to open yet.
                Game.Inner.Shift(true, false);
                yield return r.C.Wait(1.3f);
                yield break;
            }
            if (!string.IsNullOrEmpty(objective)) r.C.Objective(objective);
            else r.C.Objective(want ? "Shift into the Inner World: hold the Third Eye (Caps Lock or T, LB + RB on a controller)." : "Shift back out of the Inner World: hold the Third Eye.");
            if (want && Game.Abilities != null && Game.Abilities.Energy < InnerWorld.EnterCost)
            {
                // Never strand a quest on an empty crystal bar: the guild tops it up.
                Game.Abilities.AddEnergy(InnerWorld.EnterCost + 10f - Game.Abilities.Energy);
                if (Game.Hud != null) Game.Hud.Notify("Your crystal bar hums back to life. Enough to open the Veil.", 4f);
            }
            while (Game.Inner != null && Game.Inner.IsInner != want) yield return null;
            yield return r.C.Wait(0.6f);
        }

        // ------------------------------------------------------------------ travel and talk

        private static IEnumerator GoTo(QuestRun r, QuestStep s)
        {
            Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
            float radius = s.Radius > 0f ? s.Radius : QuestDefaults.GoToRadius;
            if (r.LastExact)
            {
                yield return ExactGoTo(r, p, radius, Obj(s, "Go to " + CampaignPlacesRuntime04E.Label(s.At) + "."), s.OnFoot);
                yield break;
            }
            yield return r.C.GoTo(p, radius, Obj(s, "Go to " + PlaceName(p) + "."), s.OnFoot);
        }

        private static IEnumerator Talk(QuestRun r, QuestStep s)
        {
            if (CampaignRules04E.IsPlaceToken(s.At))
            {
                yield return TalkExact(r, s);
                yield break;
            }
            bool place = !string.IsNullOrEmpty(s.At) || !string.IsNullOrEmpty(s.Look);
            if (place)
            {
                Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
                string who = !string.IsNullOrEmpty(s.Name) ? s.Name : "them";
                Vector3 spot = p;
                if (!string.IsNullOrEmpty(s.Look))
                {
                    Vector3 stand = QuestWorld.OpenGround(p, r.Giver);
                    NpcController n = r.C.Civilian(stand, QuestWorld.YawToward(stand, Game.PlayerPosition), QuestWorld.Look(s.Look, new System.Random(r.Salt + r.Step)));
                    n.DisplayName = who;
                    spot = n.transform.position + n.transform.forward * 0.9f;
                }
                if (!string.IsNullOrEmpty(s.Prop))
                {
                    QuestProp prop = QuestProps.Build(s.Prop, QuestWorld.OpenGround(p + new Vector3(1.5f, 0f, 1f), p), 0f, r.C.M.transform, Accent(r));
                    r.C.Objects.Add(prop.gameObject);
                }
                yield return r.C.PressE(spot, !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : "Press E to talk to " + who, Obj(s, "Talk to " + who + "."));
            }
            yield return SayLines(r, s.Lines);
        }

        private static string DefaultPrompt(string prop)
        {
            switch (prop)
            {
                case "Brazier":
                case "Campfire": return "Press E to light it";
                case "Shrine": return "Press E to touch the shrine";
                case "Crate":
                case "Wreck": return "Press E to search it";
                case "Cache": return "Press E to open the cache";
                case "Satchel": return "Press E to take it";
                case "Altar": return "Press E to place your hand on the altar";
                case "Anvil":
                case "Forge": return "Press E to work the forge";
                case "Jammer": return "Press E to pull the wires";
                case "Board": return "Press E to read the notices";
                default: return "Press E to interact";
            }
        }

        private static IEnumerator Interact(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            bool exact = r.LastExact;
            int n = Mathf.Max(1, s.Count);
            float radius = s.Radius > 0f ? s.Radius : (n > 1 ? 6f : 0f);
            string prompt = !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : DefaultPrompt(s.Prop);
            string objective = Obj(s, n > 1 ? "Use all " + n + " marked spots." : "Use the marked spot.");
            bool lightUp = s.Prop == "Brazier" || s.Prop == "Campfire" || s.Prop == "Shrine";
            // Motes, feathers, crystals, caches and satchels are taken along: they vanish once used.
            bool pickUp = QuestCatalog.IsPickupProp(s.Prop);
            bool[] used = new bool[n];
            Interactable[] its = new Interactable[n];
            WorldMarker[] marks = new WorldMarker[n];
            Blip[] blips = new Blip[n];
            QuestProp[] props = new QuestProp[n];
            // Underground, the spots stay in the anchor's cell and apart from each other (QuestWorld.OpenGround).
            List<Vector3> taken = new List<Vector3>(n);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = n == 1 ? c : QuestWorld.OpenGround(QuestWorld.Around(c, radius, i, n, 0.3f), c, taken);
                taken.Add(p);
                if (!string.IsNullOrEmpty(s.Prop))
                {
                    props[i] = QuestProps.Build(s.Prop, p, QuestWorld.YawToward(p, c) + 180f, r.C.M.transform, Accent(r));
                    r.C.Objects.Add(props[i].gameObject);
                    if (lightUp) props[i].SetLit(false);
                }
                int k = i;
                its[i] = QuestPrompt("Quest interact", p, 2.4f, prompt, () => used[k] = true, r.C.M.transform);
                r.C.Objects.Add(its[i].gameObject);
                marks[i] = exact ? ExactMarker(r, p, MarkerColor, 1.1f) : r.C.Marker(p, MarkerColor, 1.1f);
                blips[i] = r.C.BlipAt(p, MarkerColor, objective);
            }
            r.C.Objective(objective);
            r.C.Target(c);
            int done = 0;
            bool[] handled = new bool[n];
            while (done < n)
            {
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] || handled[i]) continue;
                    handled[i] = true;
                    done++;
                    if (its[i] != null) UnityEngine.Object.Destroy(its[i].gameObject);
                    if (marks[i] != null) UnityEngine.Object.Destroy(marks[i].gameObject);
                    r.C.RemoveBlip(blips[i]);
                    if (props[i] != null && lightUp) props[i].SetLit(true);
                    if (props[i] != null && pickUp) props[i].gameObject.SetActive(false);
                    if (Game.Audio != null) Game.Audio.Play2D(lightUp ? SfxId.Whoosh : SfxId.ItemPickup, 0.6f, lightUp ? 1.4f : 1f);
                    if (!string.IsNullOrEmpty(s.Text) && Game.Hud != null) Game.Hud.Notify(s.Text, 3f);
                }
                if (n > 1 && Game.Hud != null) Game.Hud.SetCounter(!string.IsNullOrEmpty(s.Name) ? s.Name : "DONE", done, n);
                if (done < n)
                {
                    // Point the GPS at the nearest spot still to do.
                    float best = float.MaxValue;
                    for (int i = 0; i < n; i++)
                    {
                        if (used[i] || its[i] == null) continue;
                        float d = MathUtil.FlatDistance(its[i].transform.position, Game.PlayerPosition);
                        if (d < best)
                        {
                            best = d;
                            r.C.Target(its[i].transform.position);
                        }
                    }
                }
                yield return null;
            }
            if (n > 1 && Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
            yield return SayLines(r, s.Lines);
        }

        private static IEnumerator Shift(QuestRun r, QuestStep s)
        {
            if (Game.Inner == null) yield break;
            if (Game.Inner.IsInner == s.Inner) yield break;
            if (s.Force || (s.Inner && !InnerWorld.Unlocked))
            {
                Game.Inner.Shift(s.Inner, false);
                yield return r.C.Wait(InnerWorld.BlendSeconds + 0.2f);
                yield break;
            }
            QuestStep gate = new QuestStep { Veil = s.Inner ? "Inner" : "Outer" };
            yield return VeilGate(r, gate, s.Objective);
        }

        private static IEnumerator Cinematic(QuestRun r, QuestStep s)
        {
            Vector3 at = r.Place(s.At, s.OffsetX, s.OffsetZ);
            Vector3 look = string.IsNullOrEmpty(s.To) ? Game.PlayerPosition : r.PlaceTo(s.To, s.ToOffsetX, s.ToOffsetZ);
            float h = s.Height > 0f ? s.Height : QuestDefaults.CinematicHeight;
            float secs = s.Seconds > 0f ? s.Seconds : QuestDefaults.CinematicSeconds;
            if (Game.Player == null) yield break;
            yield return r.C.Cinematic(at + Vector3.up * h, look + Vector3.up * 1.4f, secs, s.Lines.Count > 0 ? Pairs(s.Lines) : null);
        }

        private static IEnumerator Choice(QuestRun r, QuestStep s)
        {
            if (s.Options.Count == 0) yield break;
            int choice = -1;
            // 04E: options with a When condition are offered only while it holds (a finished mechanism is not offered again).
            List<int> shown = HasConditions(s.Options) && r.Book != null ? CampaignRules04E.VisibleOptions(s.Options, r.Book.Facts()) : AllOptions(s.Options.Count);
            string[] labels = new string[shown.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = s.Options[shown[i]].Label.ToUpperInvariant();
            if (Game.Menus != null) Game.Menus.ShowChoice(!string.IsNullOrEmpty(s.Name) ? s.Name : r.Quest.Title.ToUpperInvariant(), s.Text, labels, i => choice = i);
            else choice = 0;
            while (choice < 0) yield return null;
            r.Jump = s.Options[shown[Mathf.Clamp(choice, 0, shown.Count - 1)]].Phase;
        }

        private static bool HasConditions(List<ChoiceOption> options)
        {
            foreach (ChoiceOption o in options) if (o != null && !string.IsNullOrEmpty(o.When)) return true;
            return false;
        }

        private static List<int> AllOptions(int n)
        {
            List<int> list = new List<int>(n);
            for (int i = 0; i < n; i++) list.Add(i);
            return list;
        }

        // ------------------------------------------------------------------ instant actions

        /// <summary>Creatures hunt the player at once only when the player is this close to where they appear.</summary>
        public const float SpawnHostileRange = 60f;

        /// <summary>
        /// The Spawn step: creatures waiting at a place (a courier ambush on the route). Spawned far from the player they
        /// keep to their temperament and fight when they notice the player, instead of running across the city (quest
        /// creatures never leash). A pack species arrives as one pack; others one a frame.
        /// </summary>
        private static IEnumerator SpawnAction(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            if (sp == null) yield break;
            int n = s.Count > 0 ? s.Count : Mathf.Max(1, sp.PackMin);
            float radius = s.Radius > 0f ? s.Radius : 8f;
            bool hostile = MathUtil.FlatDistance(Game.PlayerPosition, c) < SpawnHostileRange;
            string name = n == 1 ? s.Name : null;
            if (QuestLogic.SpawnsAsPack(sp))
            {
                r.SpawnGroup(s.Species, c, n, hostile, s.Elite, s.HealthScale, s.DamageScale, name);
                yield break;
            }
            for (int i = 0; i < n; i++)
            {
                Vector3 p = QuestWorld.Around(c, radius * 0.6f, i, n, r.Step * 0.7f);
                r.Spawn(s.Species, p, QuestWorld.YawToward(p, Game.PlayerPosition), hostile, s.Elite, s.HealthScale, s.DamageScale, name);
                if (i < n - 1) yield return null;
            }
        }

        private static void AllyAction(QuestRun r, QuestStep s)
        {
            Vector3 p = QuestWorld.OpenGround(r.Place(s.At, s.OffsetX, s.OffsetZ) + new Vector3(1.5f, 0f, 1.5f), Game.PlayerPosition);
            NpcController n = r.C.Ally(p, QuestWorld.Look(!string.IsNullOrEmpty(s.Look) ? s.Look : "Civilian", new System.Random(r.Salt + 71)), s.Name, s.Health > 0f ? s.Health : QuestDefaults.AllyHealth);
            n.Pacifist = s.Pacifist;
            n.SetState(NpcState.Follow);
            if (!s.Pacifist) n.Arm(WeaponId.VectorSidearm);
            r.Allies.Add(n);
        }

        private static void PropAction(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            int n = Mathf.Max(1, s.Count);
            float radius = s.Radius > 0f ? s.Radius : (n > 1 ? 4f : 0f);
            List<Vector3> taken = new List<Vector3>(n);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = n == 1 ? c : QuestWorld.OpenGround(QuestWorld.Around(c, radius, i, n, 0.9f), c, taken);
                taken.Add(p);
                QuestProp prop = QuestProps.Build(s.Prop, p, QuestWorld.YawToward(p, c) + 90f * i, r.C.M.transform, Accent(r));
                r.C.Objects.Add(prop.gameObject);
            }
        }

        private static void WeatherAction(QuestStep s)
        {
            if (Game.Atmo == null) return;
            Weather w;
            if (Enum.TryParse(s.Weather, out w)) Game.Atmo.SetWeather(w, false);
        }

        private static void GiveAction(QuestRun r, QuestStep s)
        {
            if (!string.IsNullOrEmpty(s.Material) && s.Count > 0 && Game.Materials != null) Game.Materials.Add(s.Material, s.Count, true);
            if (s.Cash > 0 && Game.Economy != null) Game.Economy.Earn(s.Cash, r.Quest.Title);
            if (s.Lumen > 0 && Game.Progress != null) Game.Progress.AddLumen(s.Lumen);
            if (!string.IsNullOrEmpty(s.Gear) && Game.Gear != null)
            {
                // A piece rolled by the gear system itself, at the player's current power.
                GearSlot slot;
                Rarity rarity;
                if (!Enum.TryParse(s.Gear, out slot)) return;
                if (string.IsNullOrEmpty(s.Rarity) || !Enum.TryParse(s.Rarity, out rarity)) rarity = Rarity.Rare;
                try
                {
                    GearItem it = Game.Gear.Generate(slot, rarity, Game.Gear.CurrentPower);
                    if (!Game.Gear.Add(it, true))
                    {
                        // A quest reward is never lost to a full bag: it goes in over the cap.
                        Game.Gear.Items.Add(it);
                        if (Game.Hud != null) Game.Hud.Notify(it.Name + " is in your gear bag (it was full, so it went in on top).", 5f);
                    }
                }
                catch (Exception e) { Log.Exception("Quest gear reward " + r.Quest.Id, e); }
            }
        }

        private static void MessageAction(QuestRun r, QuestStep s)
        {
            if (Game.Hud == null || string.IsNullOrEmpty(s.Text)) return;
            float secs = s.Seconds > 0f ? s.Seconds : QuestDefaults.MessageSeconds;
            if (s.Banner) Game.Hud.Banner(!string.IsNullOrEmpty(s.Name) ? s.Name.ToUpperInvariant() : r.Quest.Title.ToUpperInvariant(), s.Text, Accent(r), secs);
            else Game.Hud.Notify(s.Text, secs);
        }
    }
}
