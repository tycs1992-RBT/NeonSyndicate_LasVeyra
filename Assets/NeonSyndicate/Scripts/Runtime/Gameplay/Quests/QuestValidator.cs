using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Everything quest and event data may refer to: species, materials, factions, places and weather.</summary>
    public sealed class QuestRefs
    {
        public CityLayout Layout;
        public readonly HashSet<string> Species = new HashSet<string>();
        public readonly HashSet<string> Bosses = new HashSet<string>();
        public readonly HashSet<string> Materials = new HashSet<string>();
        public readonly HashSet<string> Factions = new HashSet<string>();
        public readonly HashSet<string> Guilds = new HashSet<string>();
        /// <summary>Factions with a street gang (they can field crews in brawls and as guards).</summary>
        public readonly HashSet<string> Syndicates = new HashSet<string>();
        public readonly HashSet<string> Sites = new HashSet<string>();
        public readonly HashSet<string> Pois = new HashSet<string>();
        public readonly HashSet<string> Weathers = new HashSet<string>();

        /// <summary>Builds the reference sets from the game's tables and a generated city (the default seed in tests).</summary>
        public static QuestRefs Build(CityLayout layout)
        {
            QuestRefs r = new QuestRefs { Layout = layout };
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                r.Species.Add(s.Id);
                if (s.Boss) r.Bosses.Add(s.Id);
            }
            foreach (MaterialDef m in MaterialDb.All) r.Materials.Add(m.Id);
            foreach (FactionDef f in FactionDb.All)
            {
                r.Factions.Add(f.Id);
                if (f.Kind == FactionKind.Guild) r.Guilds.Add(f.Id);
                if (f.GangId >= 0) r.Syndicates.Add(f.Id);
            }
            foreach (string w in Enum.GetNames(typeof(Weather))) r.Weathers.Add(w);
            if (layout != null)
            {
                foreach (SpecialSite s in layout.Sites) r.Sites.Add(s.Key);
                foreach (Poi p in layout.Pois) if (!string.IsNullOrEmpty(p.Name)) r.Pois.Add(p.Name);
            }
            return r;
        }
    }

    /// <summary>The result of a validation run: errors make the data unusable, warnings are worth a look.</summary>
    public sealed class QuestReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public int Quests;
        public int Phases;
        public int Steps;
        public int Contracts;
        public int Givers;
        public int Events;

        public bool Ok { get { return Errors.Count == 0; } }

        public string Summary()
        {
            return Quests + " quests (" + Phases + " phases, " + Steps + " steps), " + Contracts + " contract templates, " + Givers + " givers, " + Events
                + " event templates: " + Errors.Count + " errors, " + Warnings.Count + " warnings";
        }

        /// <summary>Every error and warning, one per line.</summary>
        public string Details()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string e in Errors) sb.Append("ERROR ").Append(e).Append('\n');
            foreach (string w in Warnings) sb.Append("warning ").Append(w).Append('\n');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Checks quest, contract and event data before it ships: the schema (only fields the step type or event kind
    /// reads, required fields present, values in range), every reference (species, materials, factions, sites, POIs,
    /// looks, props, phases, prerequisites), the shape (ids unique, no prerequisite cycles, every phase reachable and
    /// able to finish) and the rewards. Plain C# with no engine calls; run by the EditMode tests and the offline tools.
    /// </summary>
    public static class QuestValidator
    {
        private const int MaxObjective = 110;
        private const int MaxLine = 260;

        // ================================================================== entry points

        public static QuestReport ValidateAll(IList<KeyValuePair<string, QuestFile>> questFiles, IList<KeyValuePair<string, EventFile>> eventFiles, QuestRefs refs, QuestJsonTrace trace)
        {
            QuestReport rep = new QuestReport();
            if (questFiles != null) ValidateQuests(questFiles, refs, rep, trace);
            if (eventFiles != null) ValidateEvents(eventFiles, refs, rep, trace);
            return rep;
        }

        /// <summary>Validates every quest file together (ids and prerequisites are checked across files).</summary>
        public static void ValidateQuests(IList<KeyValuePair<string, QuestFile>> files, QuestRefs refs, QuestReport rep, QuestJsonTrace trace)
        {
            Dictionary<string, QuestDef> byId = new Dictionary<string, QuestDef>();
            Dictionary<string, string> fileOf = new Dictionary<string, string>();
            HashSet<string> giverGuilds = new HashSet<string>();
            HashSet<string> contractIds = new HashSet<string>();
            foreach (KeyValuePair<string, QuestFile> kv in files)
            {
                QuestFile f = kv.Value;
                string file = kv.Key;
                if (f == null) { rep.Errors.Add(file + ": the file could not be read"); continue; }
                QuestDefaults.Normalize(f);
                if (f.Format != 1) rep.Errors.Add(file + ": Format must be 1 (found " + f.Format + ")");
                foreach (GiverDef g in f.Givers)
                {
                    rep.Givers++;
                    string where = file + At(trace, g) + ": giver for '" + g.Guild + "'";
                    if (!refs.Factions.Contains(g.Guild)) rep.Errors.Add(where + ": unknown faction" + Hint(g.Guild, refs.Factions));
                    if (!giverGuilds.Add(g.Guild)) rep.Errors.Add(where + ": this guild already has a giver");
                    if (!NamedPlace(refs, g.At)) rep.Errors.Add(where + ": At '" + g.At + "' is not a POI name or site key" + Hint(g.At, AllPlaces(refs)));
                    CheckText(where, "Name", g.Name, rep, 40, true);
                    CheckText(where, "Board", g.Board, rep, 48, false);
                    CheckText(where, "Greeting", g.Greeting, rep, MaxLine, false);
                }
            }
            foreach (KeyValuePair<string, QuestFile> kv in files)
            {
                if (kv.Value == null) continue;
                foreach (QuestDef q in kv.Value.Quests)
                {
                    rep.Quests++;
                    string where = kv.Key + At(trace, q) + ": quest '" + q.Id + "'";
                    if (!IsId(q.Id)) rep.Errors.Add(where + ": Id must be lower case letters, digits and underscores");
                    else if (byId.ContainsKey(q.Id)) rep.Errors.Add(where + ": duplicate quest id (also in " + fileOf[q.Id] + ")");
                    else
                    {
                        byId[q.Id] = q;
                        fileOf[q.Id] = kv.Key;
                    }
                }
                foreach (ContractDef c in kv.Value.Contracts)
                {
                    if (!contractIds.Add(c.Id)) rep.Errors.Add(kv.Key + At(trace, c) + ": duplicate contract id '" + c.Id + "'");
                }
            }
            foreach (KeyValuePair<string, QuestFile> kv in files)
            {
                if (kv.Value == null) continue;
                foreach (QuestDef q in kv.Value.Quests) ValidateQuest(kv.Key, q, byId, giverGuilds, refs, rep, trace);
                foreach (ContractDef c in kv.Value.Contracts) ValidateContract(kv.Key, c, byId, giverGuilds, refs, rep, trace);
            }
            CheckPrerequisiteCycles(byId, fileOf, rep);
            foreach (string id in contractIds) if (byId.ContainsKey(id)) rep.Errors.Add("contract id '" + id + "' is also a quest id");
            CheckArcs(byId, rep);
        }

        // ================================================================== quests

        private static void ValidateQuest(string file, QuestDef q, Dictionary<string, QuestDef> byId, HashSet<string> givers, QuestRefs refs, QuestReport rep, QuestJsonTrace trace)
        {
            string where = file + At(trace, q) + ": quest '" + q.Id + "'";
            CheckText(where, "Title", q.Title, rep, 42, true);
            CheckText(where, "Summary", q.Summary, rep, 320, true);
            CheckText(where, "Done", q.Done, rep, 320, false);
            if (!refs.Factions.Contains(q.Arc)) rep.Errors.Add(where + ": Arc '" + q.Arc + "' is not a faction id" + Hint(q.Arc, refs.Factions));
            if (q.Order < 1) rep.Errors.Add(where + ": Order must be 1 or more (its place in the arc)");
            if (string.IsNullOrEmpty(q.Giver))
            {
                if (!givers.Contains(q.Arc)) rep.Errors.Add(where + ": no Giver set and the arc '" + q.Arc + "' has no giver in any file");
            }
            else if (!NamedPlace(refs, q.Giver)) rep.Errors.Add(where + ": Giver '" + q.Giver + "' is not a POI name or site key" + Hint(q.Giver, AllPlaces(refs)));
            CheckOffset(where, "GiverOffset", q.GiverOffsetX, q.GiverOffsetZ, 60f, rep);

            // Requirements.
            QuestRequirements r = q.Requires;
            foreach (string id in r.Quests)
            {
                if (id == q.Id) rep.Errors.Add(where + ": requires itself");
                else if (!byId.ContainsKey(id)) rep.Errors.Add(where + ": requires unknown quest '" + id + "'" + Hint(id, byId.Keys));
            }
            if (r.Story < 0 || r.Story > 12) rep.Errors.Add(where + ": Requires.Story must be 0 to 12 (story missions passed)");
            if (r.Rep != 0 || !string.IsNullOrEmpty(r.Faction))
            {
                if (!refs.Factions.Contains(r.Faction)) rep.Errors.Add(where + ": Requires.Faction '" + r.Faction + "' is not a faction id");
                if (r.Rep < -100 || r.Rep > 100) rep.Errors.Add(where + ": Requires.Rep must be -100 to 100");
            }
            if (!string.IsNullOrEmpty(r.Visited) && !refs.Sites.Contains(r.Visited)) rep.Errors.Add(where + ": Requires.Visited '" + r.Visited + "' is not a site key" + Hint(r.Visited, refs.Sites));

            // Phases.
            if (q.Phases.Count == 0) rep.Errors.Add(where + ": has no phases");
            HashSet<string> phaseIds = new HashSet<string>();
            for (int i = 0; i < q.Phases.Count; i++)
            {
                QuestPhase ph = q.Phases[i];
                string pw = where + ", phase " + (i + 1) + " '" + ph.Id + "'";
                if (!IsId(ph.Id)) rep.Errors.Add(pw + ": phase Id must be lower case letters, digits and underscores");
                else if (!phaseIds.Add(ph.Id)) rep.Errors.Add(pw + ": duplicate phase id");
                CheckText(pw, "Title", ph.Title, rep, 48, false);
            }
            for (int i = 0; i < q.Phases.Count; i++)
            {
                QuestPhase ph = q.Phases[i];
                string pw = where + ", phase '" + ph.Id + "'";
                rep.Phases++;
                if (!string.IsNullOrEmpty(ph.Next) && ph.Next != QuestDefaults.End && !phaseIds.Contains(ph.Next))
                    rep.Errors.Add(pw + ": Next '" + ph.Next + "' is not a phase id or END" + Hint(ph.Next, phaseIds));
                if (ph.Steps.Count == 0) rep.Errors.Add(pw + ": has no steps");
                for (int s = 0; s < ph.Steps.Count; s++)
                {
                    rep.Steps++;
                    QuestStep st = ph.Steps[s];
                    ValidateStep(pw + ", step " + (s + 1) + " (" + st.Type + ")" + At(trace, st), st, q, phaseIds, refs, rep);
                    if (st.Type == "Choice" && s != ph.Steps.Count - 1) rep.Warnings.Add(pw + ": steps after a Choice never run (a Choice ends its phase)");
                }
            }
            if (q.Phases.Count > 0)
            {
                bool[] reach = QuestLogic.Reachable(q);
                for (int i = 0; i < reach.Length; i++)
                {
                    if (!reach[i]) rep.Errors.Add(where + ": phase '" + q.Phases[i].Id + "' can never be reached");
                    else if (!QuestLogic.CanFinishFrom(q, i)) rep.Errors.Add(where + ": from phase '" + q.Phases[i].Id + "' the quest can never finish (loop with no way to END)");
                }
            }

            // Fail conditions.
            foreach (QuestFailCondition c in q.FailIf)
            {
                string fw = where + ", fail condition " + c.Type + At(trace, c);
                if (!QuestCatalog.Contains(QuestCatalog.FailTypes, c.Type)) { rep.Errors.Add(fw + ": unknown type" + Hint(c.Type, QuestCatalog.FailTypes)); continue; }
                foreach (string p in c.Phases) if (!phaseIds.Contains(p)) rep.Errors.Add(fw + ": phase '" + p + "' does not exist");
                CheckText(fw, "Message", c.Message, rep, 120, false);
                switch (c.Type)
                {
                    case "TimeLimit":
                        if (c.Seconds < 10f || c.Seconds > 3600f) rep.Errors.Add(fw + ": Seconds must be 10 to 3600");
                        break;
                    case "LeaveArea":
                        if (c.Radius < 10f || c.Radius > 800f) rep.Errors.Add(fw + ": Radius must be 10 to 800");
                        if (!PlaceOk(refs, c.At)) rep.Errors.Add(fw + ": At '" + c.At + "' is not a place");
                        if (c.Seconds < 0f || c.Seconds > 60f) rep.Errors.Add(fw + ": Seconds (grace) must be 0 to 60");
                        break;
                    case "LeaveVeil":
                        if (c.Seconds < 0f || c.Seconds > 60f) rep.Errors.Add(fw + ": Seconds (grace) must be 0 to 60");
                        break;
                    case "Wanted":
                        if (c.Stars < 0 || c.Stars > 5) rep.Errors.Add(fw + ": Stars must be 0 to 5");
                        break;
                    case "Hour":
                        if (c.HourMin < 0f || c.HourMin > 24f || c.HourMax < 0f || c.HourMax > 24f || c.HourMin == c.HourMax) rep.Errors.Add(fw + ": HourMin and HourMax must be different hours from 0 to 24");
                        break;
                }
            }
            CheckReward(where, q.Reward, refs, rep, 60000, 10000, 3, 40);
            if (q.Reward.Cash == 0 && q.Reward.Lumen == 0 && q.Reward.Materials.Count == 0 && q.Reward.Rep.Count == 0 && q.Reward.SkillPoints == 0)
                rep.Warnings.Add(where + ": gives no reward");
        }

        private static void ValidateStep(string w, QuestStep s, QuestDef q, HashSet<string> phaseIds, QuestRefs refs, QuestReport rep)
        {
            HashSet<string> allowed = QuestCatalog.StepFields(s.Type);
            if (allowed == null)
            {
                rep.Errors.Add(w + ": unknown step Type '" + s.Type + "'" + Hint(s.Type, QuestCatalog.StepTypes));
                return;
            }
            CheckUnusedFields(w, s, allowed, "step type " + s.Type, rep);
            foreach (string req in QuestCatalog.StepRequired(s.Type)) if (!IsSet(s, req)) rep.Errors.Add(w + ": " + req + " is required for " + s.Type + " steps");
            CheckText(w, "Objective", s.Objective, rep, MaxObjective, false);
            CheckText(w, "Text", s.Text, rep, 400, false);
            CheckText(w, "Prompt", s.Prompt, rep, 70, false);
            CheckText(w, "Name", s.Name, rep, 40, false);
            if (s.Veil != "" && s.Veil != "Inner" && s.Veil != "Outer") rep.Errors.Add(w + ": Veil must be empty, Inner or Outer");
            if (s.HoldVeil && s.Veil != "Inner") rep.Errors.Add(w + ": HoldVeil only makes sense with Veil Inner");
            if (!PlaceOk(refs, s.At)) rep.Errors.Add(w + ": At '" + s.At + "' is not a place (POI, site, Giver, Player, Previous, World or District:Name)" + Hint(s.At, AllPlaces(refs)));
            if (!PlaceOk(refs, s.To)) rep.Errors.Add(w + ": To '" + s.To + "' is not a place" + Hint(s.To, AllPlaces(refs)));
            if (s.At == "World") CheckWorldPoint(w, s.OffsetX, s.OffsetZ, rep);
            else CheckOffset(w, "Offset", s.OffsetX, s.OffsetZ, 400f, rep);
            if (s.To == "World") CheckWorldPoint(w, s.ToOffsetX, s.ToOffsetZ, rep);
            else CheckOffset(w, "ToOffset", s.ToOffsetX, s.ToOffsetZ, 400f, rep);
            Range(w, "Radius", s.Radius, 0f, 300f, rep);
            Range(w, "ToRadius", s.ToRadius, 0f, 60f, rep);
            Range(w, "HealthScale", s.HealthScale, 0f, 6f, rep);
            Range(w, "DamageScale", s.DamageScale, 0f, 4f, rep);
            if (!string.IsNullOrEmpty(s.Look) && !QuestCatalog.Contains(QuestCatalog.Looks, s.Look)) rep.Errors.Add(w + ": unknown Look '" + s.Look + "'" + Hint(s.Look, QuestCatalog.Looks));
            if (!string.IsNullOrEmpty(s.Prop) && !QuestCatalog.Contains(QuestCatalog.Props, s.Prop)) rep.Errors.Add(w + ": unknown Prop '" + s.Prop + "'" + Hint(s.Prop, QuestCatalog.Props));
            foreach (DialogueLine l in s.Lines)
            {
                CheckText(w, "Lines.Speaker", l.Speaker, rep, 40, true);
                CheckText(w, "Lines.Text", l.Text, rep, MaxLine, true);
            }
            bool bossStep = s.Type == "Boss";
            if (!string.IsNullOrEmpty(s.Species))
            {
                if (!refs.Species.Contains(s.Species)) rep.Errors.Add(w + ": unknown species '" + s.Species + "'" + Hint(s.Species, refs.Species));
                else if (bossStep && !refs.Bosses.Contains(s.Species)) rep.Errors.Add(w + ": '" + s.Species + "' is not a boss; use a Kill step");
                else if (!bossStep && refs.Bosses.Contains(s.Species)) rep.Errors.Add(w + ": '" + s.Species + "' is a boss; bosses are fought with a Boss step");
            }
            if (!string.IsNullOrEmpty(s.AddSpecies))
            {
                if (!refs.Species.Contains(s.AddSpecies)) rep.Errors.Add(w + ": unknown AddSpecies '" + s.AddSpecies + "'" + Hint(s.AddSpecies, refs.Species));
                else if (refs.Bosses.Contains(s.AddSpecies)) rep.Errors.Add(w + ": AddSpecies cannot be a boss");
            }
            if (!string.IsNullOrEmpty(s.Material) && !refs.Materials.Contains(s.Material)) rep.Errors.Add(w + ": unknown material '" + s.Material + "'" + Hint(s.Material, refs.Materials));
            if (!string.IsNullOrEmpty(s.Faction) && !refs.Factions.Contains(s.Faction)) rep.Errors.Add(w + ": unknown faction '" + s.Faction + "'" + Hint(s.Faction, refs.Factions));
            switch (s.Type)
            {
                case "Talk":
                    if (s.Lines.Count == 0) rep.Errors.Add(w + ": Talk needs at least one line");
                    if (!string.IsNullOrEmpty(s.Look) && string.IsNullOrEmpty(s.Name)) rep.Errors.Add(w + ": a Talk with a Look needs the person's Name");
                    break;
                case "Interact":
                    Range(w, "Count", s.Count, 0, 8, rep);
                    break;
                case "Kill":
                    Range(w, "Count", s.Count, 0, 40, rep);
                    Range(w, "MaxAlive", s.MaxAlive, 0, 12, rep);
                    break;
                case "Boss":
                    Range(w, "AddCount", s.AddCount, 0, 8, rep);
                    if (s.AddCount > 0 && string.IsNullOrEmpty(s.AddSpecies)) rep.Errors.Add(w + ": AddCount needs AddSpecies");
                    if (string.IsNullOrEmpty(s.At))
                    {
                        bool inner;
                        if (QuestCatalog.BossArena(s.Species, out inner) == "") rep.Errors.Add(w + ": this boss has no arena, set At");
                        else if (inner && s.Veil != "Inner") rep.Errors.Add(w + ": '" + s.Species + "' fights inside the Inner World: set Veil to Inner");
                    }
                    break;
                case "Collect":
                    Range(w, "Count", s.Count, 1, 50, rep);
                    Range(w, "Caches", s.Caches, 0, 12, rep);
                    break;
                case "Survive":
                    Range(w, "Seconds", s.Seconds, 5f, 600f, rep);
                    Range(w, "Count", s.Count, 0, 12, rep);
                    Range(w, "Waves", s.Waves, 0, 10, rep);
                    if (s.Count > 0 && string.IsNullOrEmpty(s.Species)) rep.Errors.Add(w + ": Count needs Species");
                    break;
                case "Defend":
                    Range(w, "Count", s.Count, 0, 12, rep);
                    Range(w, "Waves", s.Waves, 0, 8, rep);
                    Range(w, "Seconds", s.Seconds, 0f, 30f, rep);
                    Range(w, "Allies", s.Allies, 0, 6, rep);
                    break;
                case "Escort":
                    Range(w, "Count", s.Count, 0, 10, rep);
                    Range(w, "Waves", s.Waves, 0, 4, rep);
                    Range(w, "Health", s.Health, 0f, 3000f, rep);
                    if (s.Count > 0 && string.IsNullOrEmpty(s.Species)) rep.Errors.Add(w + ": Count needs Species (the ambush)");
                    if (string.IsNullOrEmpty(s.Look)) rep.Warnings.Add(w + ": no Look set, the escort will be a random civilian");
                    break;
                case "Hunt":
                    Range(w, "Clues", s.Clues, 0, 8, rep);
                    break;
                case "CloseRift":
                    Range(w, "Count", s.Count, 0, 5, rep);
                    Range(w, "Seconds", s.Seconds, 0f, 30f, rep);
                    Range(w, "MaxAlive", s.MaxAlive, 0, 10, rep);
                    Range(w, "Health", s.Health, 0f, 5000f, rep);
                    break;
                case "Scan":
                    Range(w, "Seconds", s.Seconds, 0f, 10f, rep);
                    break;
                case "Shift":
                    if (s.Veil != "") rep.Errors.Add(w + ": a Shift step moves through the Veil itself, leave Veil empty");
                    break;
                case "Wait":
                    Range(w, "Seconds", s.Seconds, 0.1f, 120f, rep);
                    break;
                case "Spawn":
                    Range(w, "Count", s.Count, 0, 20, rep);
                    break;
                case "Ally":
                    Range(w, "Health", s.Health, 0f, 3000f, rep);
                    break;
                case "Prop":
                    Range(w, "Count", s.Count, 0, 12, rep);
                    break;
                case "SetTime":
                    Range(w, "Hour", s.Hour, 0f, 24f, rep);
                    break;
                case "SetWeather":
                    if (!refs.Weathers.Contains(s.Weather)) rep.Errors.Add(w + ": unknown Weather '" + s.Weather + "'" + Hint(s.Weather, refs.Weathers));
                    break;
                case "Give":
                    Range(w, "Count", s.Count, 0, 30, rep);
                    Range(w, "Cash", s.Cash, 0, 20000, rep);
                    Range(w, "Lumen", s.Lumen, 0, 5000, rep);
                    if (s.Count > 0 && string.IsNullOrEmpty(s.Material)) rep.Errors.Add(w + ": Count needs Material");
                    if (!string.IsNullOrEmpty(s.Material) && s.Count <= 0) rep.Errors.Add(w + ": Material needs a Count");
                    if (s.Count == 0 && s.Cash == 0 && s.Lumen == 0 && string.IsNullOrEmpty(s.Gear)) rep.Errors.Add(w + ": Give gives nothing");
                    if (!string.IsNullOrEmpty(s.Gear) && !Enum.IsDefined(typeof(GearSlot), s.Gear)) rep.Errors.Add(w + ": unknown Gear slot '" + s.Gear + "'" + Hint(s.Gear, Enum.GetNames(typeof(GearSlot))));
                    if (!string.IsNullOrEmpty(s.Rarity) && !Enum.IsDefined(typeof(Rarity), s.Rarity)) rep.Errors.Add(w + ": unknown Rarity '" + s.Rarity + "'" + Hint(s.Rarity, Enum.GetNames(typeof(Rarity))));
                    if (!string.IsNullOrEmpty(s.Rarity) && string.IsNullOrEmpty(s.Gear)) rep.Errors.Add(w + ": Rarity needs a Gear slot");
                    break;
                case "GiveGun":
                {
                    Rarity gr = Rarity.Rare;
                    LootArchetype ga = LootArchetype.Pistol;
                    bool hasRarity = !string.IsNullOrEmpty(s.Rarity);
                    bool hasType = !string.IsNullOrEmpty(s.Archetype);
                    if (hasRarity && (!Enum.IsDefined(typeof(Rarity), s.Rarity) || !Enum.TryParse(s.Rarity, out gr))) { rep.Errors.Add(w + ": unknown Rarity '" + s.Rarity + "'" + Hint(s.Rarity, Enum.GetNames(typeof(Rarity)))); hasRarity = false; }
                    if (hasType && (!Enum.IsDefined(typeof(LootArchetype), s.Archetype) || !Enum.TryParse(s.Archetype, out ga))) { rep.Errors.Add(w + ": unknown Archetype '" + s.Archetype + "'" + Hint(s.Archetype, Enum.GetNames(typeof(LootArchetype)))); hasType = false; }
                    if (!string.IsNullOrEmpty(s.Unique))
                    {
                        int u = LootWorldRules.UniqueByName(s.Unique);
                        if (u < 0)
                        {
                            List<string> names = new List<string>();
                            foreach (LootUniqueDef d in LootUniques.All) names.Add(d.Name);
                            rep.Errors.Add(w + ": unknown Unique '" + s.Unique + "'" + Hint(s.Unique, names));
                        }
                        else
                        {
                            LootUniqueDef d = LootUniques.All[u];
                            if (hasRarity && gr != d.Rarity) rep.Errors.Add(w + ": Rarity '" + s.Rarity + "' does not match the unique '" + d.Name + "' (" + d.Rarity + "); leave Rarity out");
                            if (hasType && ga != d.Archetype) rep.Errors.Add(w + ": Archetype '" + s.Archetype + "' does not match the unique '" + d.Name + "' (" + d.Archetype + "); leave Archetype out");
                        }
                    }
                    break;
                }
                case "AddRep":
                    if (s.Amount == 0 || s.Amount < -50 || s.Amount > 50) rep.Errors.Add(w + ": Amount must be -50 to 50 and not 0");
                    break;
                case "Message":
                    Range(w, "Seconds", s.Seconds, 0f, 15f, rep);
                    break;
                case "Cinematic":
                    Range(w, "Seconds", s.Seconds, 0f, 20f, rep);
                    Range(w, "Height", s.Height, 0f, 60f, rep);
                    break;
                case "Choice":
                    if (s.Options.Count < 2 || s.Options.Count > 4) rep.Errors.Add(w + ": a Choice needs 2 to 4 Options");
                    foreach (ChoiceOption o in s.Options)
                    {
                        CheckText(w, "Options.Label", o.Label, rep, 16, true);
                        if (o.Phase != QuestDefaults.End && !phaseIds.Contains(o.Phase)) rep.Errors.Add(w + ": option '" + o.Label + "' leads to unknown phase '" + o.Phase + "'" + Hint(o.Phase, phaseIds));
                    }
                    break;
            }
        }

        // ================================================================== contracts

        private static void ValidateContract(string file, ContractDef c, Dictionary<string, QuestDef> quests, HashSet<string> givers, QuestRefs refs, QuestReport rep, QuestJsonTrace trace)
        {
            rep.Contracts++;
            int before = rep.Errors.Count;
            string w = file + At(trace, c) + ": contract '" + c.Id + "'";
            if (!IsId(c.Id)) rep.Errors.Add(w + ": Id must be lower case letters, digits and underscores");
            if (!refs.Factions.Contains(c.Guild)) rep.Errors.Add(w + ": unknown Guild '" + c.Guild + "'" + Hint(c.Guild, refs.Factions));
            else if (!givers.Contains(c.Guild)) rep.Errors.Add(w + ": the guild '" + c.Guild + "' has no giver, so it has no board");
            if (!QuestCatalog.Contains(QuestCatalog.ContractKinds, c.Kind)) rep.Errors.Add(w + ": unknown Kind '" + c.Kind + "'" + Hint(c.Kind, QuestCatalog.ContractKinds));
            CheckText(w, "Title", c.Title, rep, 48, true);
            CheckText(w, "Brief", c.Brief, rep, MaxLine, true);
            CheckTokens(w, "Title", c.Title, rep);
            CheckTokens(w, "Brief", c.Brief, rep);
            Range(w, "Weight", c.Weight, 0f, 10f, rep);
            Range(w, "MinRep", c.MinRep, -100, 100, rep);
            Range(w, "CountMin", c.CountMin, 0, 20, rep);
            Range(w, "CountMax", c.CountMax, 0, 20, rep);
            if (c.CountMax > 0 && c.CountMax < c.CountMin) rep.Errors.Add(w + ": CountMax is below CountMin");
            Range(w, "Seconds", c.Seconds, 0f, 1800f, rep);
            Range(w, "Cash", c.Cash, 0, 10000, rep);
            Range(w, "Lumen", c.Lumen, 0, 3000, rep);
            Range(w, "Rep", c.Rep, 0, 15, rep);
            foreach (MaterialAmount m in c.Materials)
            {
                if (!refs.Materials.Contains(m.Id)) rep.Errors.Add(w + ": unknown material '" + m.Id + "'" + Hint(m.Id, refs.Materials));
                Range(w, "Materials.Count", m.Count, 1, 10, rep);
            }
            if (!string.IsNullOrEmpty(c.Look) && !QuestCatalog.Contains(QuestCatalog.Looks, c.Look)) rep.Errors.Add(w + ": unknown Look '" + c.Look + "'" + Hint(c.Look, QuestCatalog.Looks));
            CheckText(w, "Name", c.Name, rep, 40, false);
            foreach (string sp in c.Species)
            {
                CreatureSpecies s = CreatureSpeciesDb.Get(sp);
                if (s == null) { rep.Errors.Add(w + ": unknown species '" + sp + "'" + Hint(sp, refs.Species)); continue; }
                if (s.Boss) rep.Errors.Add(w + ": bosses cannot be contract targets ('" + sp + "')");
                if (c.Kind == "Hunt" && QuestPlaces.HabitatFor(s, 0) == "") rep.Errors.Add(w + ": '" + sp + "' lives nowhere a contract can send the player");
            }
            if ((c.Kind == "Hunt" || c.Kind == "Rift") && c.Species.Count == 0) rep.Errors.Add(w + ": " + c.Kind + " contracts need at least one species");
            if (c.Kind == "Escort" || c.Kind == "Courier")
            {
                if (c.Destinations.Count == 0) rep.Errors.Add(w + ": " + c.Kind + " contracts need Destinations");
                foreach (string d in c.Destinations) if (!NamedPlace(refs, d)) rep.Errors.Add(w + ": destination '" + d + "' is not a POI name or site key" + Hint(d, AllPlaces(refs)));
            }
            if (c.Kind == "Escort" && string.IsNullOrEmpty(c.Look)) rep.Errors.Add(w + ": Escort contracts need a Look for the client");
            // Every template must be able to produce a real job on the default city.
            if (refs.Layout != null && rep.Errors.Count == before)
            {
                ContractOffer o = QuestLogic.MakeContract(c, refs.Layout, BoardOf(c.Guild, refs), "Giver", 1, 0);
                if (o == null) rep.Errors.Add(w + ": could not be placed on the map (no habitat point or destination)");
            }
        }

        /// <summary>Where a guild's board stands on the default city (the faction contact's spot), or the origin.</summary>
        private static Vector3 BoardOf(string guild, QuestRefs refs)
        {
            FactionDef f = FactionDb.Get(guild);
            Vector3 v;
            if (f != null && refs.Layout != null && QuestPlaces.GiverPosition(refs.Layout, f.Contact, 0f, 0f, out v)) return v;
            return Vector3.zero;
        }

        private static void CheckTokens(string w, string label, string text, QuestReport rep)
        {
            if (string.IsNullOrEmpty(text)) return;
            int i = 0;
            while ((i = text.IndexOf('{', i)) >= 0)
            {
                int j = text.IndexOf('}', i);
                if (j < 0) { rep.Errors.Add(w + ": " + label + " has an unclosed '{'"); return; }
                string tok = text.Substring(i, j - i + 1);
                if (!QuestCatalog.Contains(QuestCatalog.ContractTokens, tok)) rep.Errors.Add(w + ": " + label + " uses unknown token " + tok + " (use {species}, {count}, {place} or {dest})");
                i = j + 1;
            }
        }

        // ================================================================== cross checks

        private static void CheckPrerequisiteCycles(Dictionary<string, QuestDef> byId, Dictionary<string, string> fileOf, QuestReport rep)
        {
            Dictionary<string, int> state = new Dictionary<string, int>();
            foreach (string id in byId.Keys)
            {
                List<string> path = new List<string>();
                if (Cycle(id, byId, state, path))
                {
                    rep.Errors.Add(fileOf[id] + ": prerequisite cycle: " + string.Join(" -> ", path.ToArray()));
                    return;
                }
            }
        }

        /// <summary>Depth-first search: state 1 = on the current path, 2 = done.</summary>
        private static bool Cycle(string id, Dictionary<string, QuestDef> byId, Dictionary<string, int> state, List<string> path)
        {
            int s;
            if (state.TryGetValue(id, out s))
            {
                if (s == 1)
                {
                    path.Add(id);
                    return true;
                }
                return false;
            }
            QuestDef q;
            if (!byId.TryGetValue(id, out q)) return false;
            state[id] = 1;
            path.Add(id);
            foreach (string r in q.Requires.Quests)
            {
                if (r == id) continue;
                if (Cycle(r, byId, state, path)) return true;
            }
            path.RemoveAt(path.Count - 1);
            state[id] = 2;
            return false;
        }

        /// <summary>Arc sanity: unique orders, and reputation gates that the arc's own quests can actually meet.</summary>
        private static void CheckArcs(Dictionary<string, QuestDef> byId, QuestReport rep)
        {
            Dictionary<string, HashSet<int>> orders = new Dictionary<string, HashSet<int>>();
            foreach (QuestDef q in byId.Values)
            {
                HashSet<int> set;
                if (!orders.TryGetValue(q.Arc, out set)) orders[q.Arc] = set = new HashSet<int>();
                if (!set.Add(q.Order)) rep.Warnings.Add("quest '" + q.Id + "': another quest in arc '" + q.Arc + "' has Order " + q.Order);
                QuestRequirements r = q.Requires;
                if (string.IsNullOrEmpty(r.Faction) || r.Rep <= 0) continue;
                // Reputation available from every quest that must be done first (transitively).
                HashSet<string> before = new HashSet<string>();
                Stack<string> open = new Stack<string>(r.Quests);
                while (open.Count > 0)
                {
                    string id = open.Pop();
                    QuestDef p;
                    if (!before.Add(id) || !byId.TryGetValue(id, out p)) continue;
                    foreach (string x in p.Requires.Quests) open.Push(x);
                }
                int sum = 0;
                foreach (string id in before)
                {
                    QuestDef p;
                    if (!byId.TryGetValue(id, out p)) continue;
                    foreach (RepAmount a in p.Reward.Rep) if (a.Faction == r.Faction) sum += a.Amount;
                }
                if (sum < r.Rep) rep.Warnings.Add("quest '" + q.Id + "': needs " + r.Rep + " reputation with " + r.Faction + " but the quests before it give only " + sum + " (the rest must come from contracts)");
            }
        }

        // ================================================================== events

        /// <summary>Validates every world-event file together (ids are checked across files).</summary>
        public static void ValidateEvents(IList<KeyValuePair<string, EventFile>> files, QuestRefs refs, QuestReport rep, QuestJsonTrace trace)
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (KeyValuePair<string, EventFile> kv in files)
            {
                EventFile f = kv.Value;
                if (f == null) { rep.Errors.Add(kv.Key + ": the file could not be read"); continue; }
                QuestDefaults.Normalize(f);
                if (f.Format != 1) rep.Errors.Add(kv.Key + ": Format must be 1 (found " + f.Format + ")");
                foreach (EventTemplate t in f.Events)
                {
                    rep.Events++;
                    string w = kv.Key + At(trace, t) + ": event '" + t.Id + "' (" + t.Kind + ")";
                    if (!IsId(t.Id)) rep.Errors.Add(w + ": Id must be lower case letters, digits and underscores");
                    else if (!ids.Add(t.Id)) rep.Errors.Add(w + ": duplicate event id");
                    ValidateEvent(w, t, refs, rep);
                }
            }
        }

        private static void ValidateEvent(string w, EventTemplate t, QuestRefs refs, QuestReport rep)
        {
            HashSet<string> allowed = QuestCatalog.EventFields(t.Kind);
            if (allowed == null)
            {
                rep.Errors.Add(w + ": unknown Kind '" + t.Kind + "'" + Hint(t.Kind, QuestCatalog.EventKinds));
                return;
            }
            CheckUnusedFields(w, t, allowed, "event kind " + t.Kind, rep);
            foreach (string req in QuestCatalog.EventRequired(t.Kind)) if (!IsSet(t, req)) rep.Errors.Add(w + ": " + req + " is required for " + t.Kind + " events");
            CheckText(w, "Title", t.Title, rep, 36, true);
            CheckText(w, "Callout", t.Callout, rep, 140, true);
            CheckText(w, "Objective", t.Objective, rep, MaxObjective, false);
            CheckText(w, "Success", t.Success, rep, 160, false);
            CheckText(w, "Failure", t.Failure, rep, 160, false);
            CheckText(w, "Name", t.Name, rep, 40, false);
            foreach (DialogueLine l in t.Lines)
            {
                CheckText(w, "Lines.Speaker", l.Speaker, rep, 40, true);
                CheckText(w, "Lines.Text", l.Text, rep, MaxLine, true);
            }
            Range(w, "Weight", t.Weight, 0f, 10f, rep);
            foreach (string d in t.Districts)
            {
                District dd;
                if (d != "City" && d != "Wild" && d != "Any" && !QuestPlaces.TryDistrict(d, out dd)) rep.Errors.Add(w + ": unknown district '" + d + "' (use a District name, City, Wild or Any)");
            }
            if (t.Veil != "" && t.Veil != "Outer" && t.Veil != "Inner" && t.Veil != "Any") rep.Errors.Add(w + ": Veil must be empty, Outer, Inner or Any");
            Range(w, "HourMin", t.HourMin, 0f, 24f, rep);
            Range(w, "HourMax", t.HourMax, 0f, 24f, rep);
            foreach (string x in t.Weathers) if (!refs.Weathers.Contains(x)) rep.Errors.Add(w + ": unknown weather '" + x + "'" + Hint(x, refs.Weathers));
            Range(w, "Story", t.Story, 0, 12, rep);
            Range(w, "Cooldown", t.Cooldown, 0f, 7200f, rep);
            Range(w, "Duration", t.Duration, 0f, 900f, rep);
            if (string.IsNullOrEmpty(t.Anchor) || !(QuestCatalog.Contains(QuestCatalog.Anchors, t.Anchor) || t.Anchor.StartsWith(QuestPlaces.SitePrefix)))
                rep.Errors.Add(w + ": Anchor must be one of " + string.Join(", ", QuestCatalog.Anchors) + " or Site:Key");
            else if (t.Anchor.StartsWith(QuestPlaces.SitePrefix) && !refs.Sites.Contains(t.Anchor.Substring(QuestPlaces.SitePrefix.Length)))
                rep.Errors.Add(w + ": " + t.Anchor + " is not a site" + Hint(t.Anchor.Substring(QuestPlaces.SitePrefix.Length), refs.Sites));
            CheckEventSpecies(w, "Species", t.Species, refs, rep);
            CheckEventSpecies(w, "Species2", t.Species2, refs, rep);
            Range(w, "Count", t.Count, 0, 16, rep);
            Range(w, "Count2", t.Count2, 0, 16, rep);
            if (t.Count2 > 0 && string.IsNullOrEmpty(t.Species2)) rep.Errors.Add(w + ": Count2 needs Species2");
            Range(w, "HealthScale", t.HealthScale, 0f, 6f, rep);
            Range(w, "Civilians", t.Civilians, 0, 8, rep);
            Range(w, "Guards", t.Guards, 0, 8, rep);
            Range(w, "Points", t.Points, 0, 16, rep);
            Range(w, "Seconds", t.Seconds, 0f, 600f, rep);
            Range(w, "Health", t.Health, 0f, 5000f, rep);
            Range(w, "Amount", t.Amount, 0, 30, rep);
            if (!string.IsNullOrEmpty(t.Look) && !QuestCatalog.Contains(QuestCatalog.Looks, t.Look)) rep.Errors.Add(w + ": unknown Look '" + t.Look + "'" + Hint(t.Look, QuestCatalog.Looks));
            if (!string.IsNullOrEmpty(t.Prop) && !QuestCatalog.Contains(QuestCatalog.Props, t.Prop)) rep.Errors.Add(w + ": unknown Prop '" + t.Prop + "'" + Hint(t.Prop, QuestCatalog.Props));
            if (!string.IsNullOrEmpty(t.To) && !NamedPlace(refs, t.To)) rep.Errors.Add(w + ": To '" + t.To + "' is not a POI name or site key" + Hint(t.To, AllPlaces(refs)));
            if (!string.IsNullOrEmpty(t.FactionA) && !refs.Factions.Contains(t.FactionA)) rep.Errors.Add(w + ": unknown FactionA '" + t.FactionA + "'" + Hint(t.FactionA, refs.Factions));
            if (!string.IsNullOrEmpty(t.FactionB) && !refs.Factions.Contains(t.FactionB)) rep.Errors.Add(w + ": unknown FactionB '" + t.FactionB + "'" + Hint(t.FactionB, refs.Factions));
            if (t.Guards > 0 && !refs.Syndicates.Contains(t.FactionA)) rep.Errors.Add(w + ": Guards need FactionA to be a syndicate with a street crew (" + string.Join(", ", Sorted(refs.Syndicates)) + ")");
            if (t.Kind == "Fieldwork" || t.Kind == "Skirmish")
            {
                if (!t.ManualOnly) rep.Errors.Add(w + ": territory activities require ManualOnly");
                if (!TerritoryEventRules.ValidActivity(t.Kind, t.Activity)) rep.Errors.Add(w + ": unknown territory Activity '" + t.Activity + "'");
                if (t.Duration < 60f || t.Duration > 600f) rep.Errors.Add(w + ": territory Duration must be 60 to 600 seconds");
                if (t.Cooldown < 30f) rep.Errors.Add(w + ": territory Cooldown must be at least 30 seconds");
                if (t.Points < 1 || t.Points > 4) rep.Errors.Add(w + ": territory Points must be 1 to 4");
                if (t.Seconds < 1f || t.Seconds > 60f) rep.Errors.Add(w + ": territory Seconds must be 1 to 60");
                if (t.Kind == "Skirmish" && (t.Count < 1 || t.Count > 4)) rep.Errors.Add(w + ": territory combat Count must be 1 to 4");
                if (t.Activity == "Observe" && string.IsNullOrEmpty(t.Species)) rep.Errors.Add(w + ": Observe needs a real Species");
                if (t.Activity == "Aid" && (string.IsNullOrEmpty(t.Look) || string.IsNullOrEmpty(t.Name))) rep.Errors.Add(w + ": Aid needs Look and Name");
                if (t.Veil == "Inner" || t.Veil == "Any") rep.Errors.Add(w + ": territory activities are Outer only");
            }
            switch (t.Kind)
            {
                case "Brawl":
                    if (!refs.Syndicates.Contains(t.FactionA) || !refs.Syndicates.Contains(t.FactionB)) rep.Errors.Add(w + ": both brawl sides must be syndicates with street crews");
                    if (t.FactionA == t.FactionB) rep.Errors.Add(w + ": a brawl needs two different crews");
                    break;
                case "Loot":
                    if (string.IsNullOrEmpty(t.Species) && t.Guards == 0) rep.Warnings.Add(w + ": loot with no guards at all");
                    break;
                case "Pickup":
                case "Sabotage":
                    break;
                case "Escort":
                    if (t.Count > 0 && string.IsNullOrEmpty(t.Species)) rep.Errors.Add(w + ": Count needs Species (the ambush)");
                    break;
                case "Impostor":
                    if (t.Species != "mirror_wraith") rep.Warnings.Add(w + ": impostors are meant to be Mirror Wraiths");
                    break;
            }
            CheckReward(w, t.Reward, refs, rep, 10000, 3000, 1, 15);
        }

        private static void CheckEventSpecies(string w, string label, string id, QuestRefs refs, QuestReport rep)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!refs.Species.Contains(id)) rep.Errors.Add(w + ": unknown " + label + " '" + id + "'" + Hint(id, refs.Species));
            else if (refs.Bosses.Contains(id)) rep.Errors.Add(w + ": bosses never appear in world events ('" + id + "')");
        }

        // ================================================================== shared checks

        private static void CheckReward(string w, QuestReward r, QuestRefs refs, QuestReport rep, int maxCash, int maxLumen, int maxSkill, int maxRep)
        {
            Range(w, "Reward.Cash", r.Cash, 0, maxCash, rep);
            Range(w, "Reward.Lumen", r.Lumen, 0, maxLumen, rep);
            Range(w, "Reward.SkillPoints", r.SkillPoints, 0, maxSkill, rep);
            if (r.Materials.Count > 8) rep.Errors.Add(w + ": at most 8 material rewards");
            foreach (MaterialAmount m in r.Materials)
            {
                if (!refs.Materials.Contains(m.Id)) rep.Errors.Add(w + ": unknown reward material '" + m.Id + "'" + Hint(m.Id, refs.Materials));
                Range(w, "Reward.Materials.Count", m.Count, 1, 30, rep);
            }
            HashSet<string> seen = new HashSet<string>();
            foreach (RepAmount a in r.Rep)
            {
                if (!refs.Factions.Contains(a.Faction)) rep.Errors.Add(w + ": unknown reward faction '" + a.Faction + "'" + Hint(a.Faction, refs.Factions));
                if (!seen.Add(a.Faction)) rep.Errors.Add(w + ": faction '" + a.Faction + "' appears twice in the reward");
                if (a.Amount == 0 || a.Amount < -maxRep || a.Amount > maxRep) rep.Errors.Add(w + ": reputation Amount must be -" + maxRep + " to " + maxRep + " and not 0");
            }
        }

        /// <summary>
        /// Text rules for everything a player reads: no em or en dashes (house rule), no control characters, a length
        /// limit, and a warning for characters outside plain ASCII (the UI font may not have them).
        /// </summary>
        public static void CheckText(string w, string label, string text, QuestReport rep, int maxLen, bool required)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (required) rep.Errors.Add(w + ": " + label + " is required");
                return;
            }
            if (text.Length > maxLen) rep.Errors.Add(w + ": " + label + " is " + text.Length + " characters, the limit is " + maxLen);
            bool warned = false;
            foreach (char c in text)
            {
                if (c >= '\u2010' && c <= '\u2015')
                {
                    rep.Errors.Add(w + ": " + label + " contains a dash character (U+" + ((int)c).ToString("X4") + "); house rule: use commas, periods or the word 'to'");
                    return;
                }
                if (c < ' ' && c != '\n')
                {
                    rep.Errors.Add(w + ": " + label + " contains a control character");
                    return;
                }
                if (c > '~' && !warned)
                {
                    warned = true;
                    rep.Warnings.Add(w + ": " + label + " has a character outside plain ASCII ('" + c + "')");
                }
            }
            if (text != text.Trim()) rep.Warnings.Add(w + ": " + label + " starts or ends with spaces");
        }

        private static void CheckUnusedFields(string w, object o, HashSet<string> allowed, string what, QuestReport rep)
        {
            foreach (FieldInfo f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (allowed.Contains(f.Name)) continue;
                if (IsSet(o, f.Name)) rep.Errors.Add(w + ": field " + f.Name + " is not used by " + what + " (remove it, or check the type)");
            }
        }

        /// <summary>True when a field holds something other than its empty default (text, a non-zero number, true, a non-empty list).</summary>
        public static bool IsSet(object o, string field)
        {
            FieldInfo f = o.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return false;
            object v = f.GetValue(o);
            if (v == null) return false;
            if (v is string) return ((string)v).Length > 0;
            if (v is int) return (int)v != 0;
            if (v is float) return (float)v != 0f;
            if (v is bool) return (bool)v;
            if (v is IList) return ((IList)v).Count > 0;
            if (v is QuestReward)
            {
                QuestReward r = (QuestReward)v;
                return r.Cash != 0 || r.Lumen != 0 || r.SkillPoints != 0 || (r.Materials != null && r.Materials.Count > 0) || (r.Rep != null && r.Rep.Count > 0);
            }
            return true;
        }

        private static void Range(string w, string label, float v, float min, float max, QuestReport rep)
        {
            if (float.IsNaN(v) || v < min || v > max) rep.Errors.Add(w + ": " + label + " is " + v + ", it must be " + min + " to " + max);
        }

        private static void Range(string w, string label, int v, int min, int max, QuestReport rep)
        {
            if (v < min || v > max) rep.Errors.Add(w + ": " + label + " is " + v + ", it must be " + min + " to " + max);
        }

        private static void CheckOffset(string w, string label, float x, float z, float max, QuestReport rep)
        {
            if (Math.Abs(x) > max || Math.Abs(z) > max) rep.Errors.Add(w + ": " + label + " is more than " + max + " m from its place (use World with map coordinates for far points)");
        }

        private static void CheckWorldPoint(string w, float x, float z, QuestReport rep)
        {
            if (Math.Abs(x) > CityLayout.MapHalf - 20f || Math.Abs(z) > CityLayout.MapHalf - 20f) rep.Errors.Add(w + ": World point (" + x + ", " + z + ") is outside the map");
        }

        private static bool PlaceOk(QuestRefs refs, string at)
        {
            if (string.IsNullOrEmpty(at)) return true;
            if (QuestCatalog.Contains(QuestCatalog.PlaceTokens, at)) return true;
            if (at.StartsWith(QuestPlaces.DistrictPrefix))
            {
                District d;
                return QuestPlaces.TryDistrict(at.Substring(QuestPlaces.DistrictPrefix.Length), out d) && !CityLayout.IsWilderness(d);
            }
            return NamedPlace(refs, at);
        }

        private static bool NamedPlace(QuestRefs refs, string at)
        {
            if (string.IsNullOrEmpty(at)) return false;
            if (refs.Layout == null) return true;
            return refs.Sites.Contains(at) || refs.Pois.Contains(at);
        }

        private static IEnumerable<string> AllPlaces(QuestRefs refs)
        {
            foreach (string s in refs.Sites) yield return s;
            foreach (string p in refs.Pois) yield return p;
        }

        private static bool IsId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 48) return false;
            foreach (char c in id) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            return true;
        }

        private static string At(QuestJsonTrace trace, object o)
        {
            int line = trace != null ? trace.LineOf(o) : 0;
            return line > 0 ? " (line " + line + ")" : "";
        }

        /// <summary>" (did you mean 'x'?)" when a known name is close to the unknown one.</summary>
        private static string Hint(string bad, IEnumerable<string> known)
        {
            if (string.IsNullOrEmpty(bad) || known == null) return "";
            string best = null;
            int bestD = int.MaxValue;
            foreach (string k in known)
            {
                int d = string.Equals(k, bad, StringComparison.OrdinalIgnoreCase) ? 0 : QuestJson.Distance(k.ToLowerInvariant(), bad.ToLowerInvariant());
                if (d < bestD)
                {
                    bestD = d;
                    best = k;
                }
            }
            return best != null && bestD <= Math.Max(2, bad.Length / 4) ? " (did you mean '" + best + "'?)" : "";
        }

        private static List<string> Sorted(IEnumerable<string> s)
        {
            List<string> l = new List<string>(s);
            l.Sort(StringComparer.Ordinal);
            return l;
        }
    }
}
