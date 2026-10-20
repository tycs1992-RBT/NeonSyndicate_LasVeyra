using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The player's progress as the quest rules see it (filled by QuestBook in the game, by hand in tests).</summary>
    public sealed class QuestProgress
    {
        public readonly HashSet<string> Completed = new HashSet<string>();
        public readonly HashSet<string> Visited = new HashSet<string>();
        public int Story;
        public int WorldRevision;
        public bool FreeRoam;
        public bool ThirdEye;
        public bool InnerWorld;
        /// <summary>Reputation lookup (faction id to -100..100).</summary>
        public Func<string, int> Rep = id => 0;
    }

    /// <summary>A concrete job generated from a contract template for one board slot.</summary>
    public sealed class ContractOffer
    {
        public string Guild = "";
        public string TemplateId = "";
        public int Serial;
        public string Kind = "";
        public string Title = "";
        public string Brief = "";
        public string Species = "";
        public int Count;
        public string Place = "";
        public Vector3 Point;
        public string Destination = "";
        public bool Inner;
        public QuestReward Reward = new QuestReward();
        /// <summary>The runnable quest built for the offer (same step runtime as the guild quests).</summary>
        public QuestDef Quest;
    }

    /// <summary>
    /// Quest rules in plain C# (no engine calls): requirements, the phase graph, contract generation and reward
    /// scaling. Shared by the game, the validator, the EditMode tests and the offline harnesses.
    /// </summary>
    public static class QuestLogic
    {
        // ================================================================== requirements

        /// <summary>Null when the quest's requirements are met, otherwise a short reason ("Finish The Hunting Trial").</summary>
        public static bool WorldMatches(QuestRequirements requirements, int revision)
        {
            return requirements == null || ((requirements.WorldRevision == 0 || requirements.WorldRevision == revision) &&
                (requirements.WorldRevisions == null || requirements.WorldRevisions.Count == 0 || requirements.WorldRevisions.Contains(revision) || (revision==7 && requirements.WorldRevisions.Contains(6))));
        }

        public static string MissingRequirement(QuestDef q, QuestProgress p, Func<string, string> questTitle)
        {
            if (q == null || p == null) return "Unknown quest";
            QuestRequirements r = q.Requires ?? new QuestRequirements();
            if (!WorldMatches(r, p.WorldRevision))
                return r.WorldRevisions != null && r.WorldRevisions.Count > 0 ? "Needs a compatible realm preview" :
                    "Needs the " + WorldIdentity04OB.Profile((CityRevision04O)r.WorldRevision) + " preview";
            if (r.Story > 0 && !p.FreeRoam && p.Story < r.Story) return "Continue the story first";
            if (r.ThirdEye && !p.ThirdEye) return "Needs the Third Eye";
            if (r.InnerWorld && !p.InnerWorld) return "Needs the Veil opened";
            if (r.Quests != null)
            {
                foreach (string id in r.Quests)
                {
                    if (string.IsNullOrEmpty(id) || p.Completed.Contains(id)) continue;
                    string t = questTitle != null ? questTitle(id) : id;
                    return "Finish " + (string.IsNullOrEmpty(t) ? id : t);
                }
            }
            if (!string.IsNullOrEmpty(r.Visited) && !p.Visited.Contains(r.Visited)) return "Reach " + SiteLabel(r.Visited);
            if (!string.IsNullOrEmpty(r.Faction) && r.Rep != 0 && p.Rep(r.Faction) < r.Rep)
            {
                FactionDef f = FactionDb.Get(r.Faction);
                return "Needs " + r.Rep + " reputation with the " + (f != null ? f.Name : r.Faction);
            }
            return null;
        }

        private static string SiteLabel(string key)
        {
            switch (key)
            {
                case "WardenOutpost": return "the Frost Wardens Outpost";
                case "FrostLake": return "Rimeglass Lake";
                case "TitanHollow": return "Titan Hollow";
                case "CrystalManor": return "the Crystal Manor ruins";
                default: return key;
            }
        }

        // ================================================================== phases

        public static int PhaseIndex(QuestDef q, string id)
        {
            if (q == null || q.Phases == null || string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < q.Phases.Count; i++) if (q.Phases[i] != null && q.Phases[i].Id == id) return i;
            return -1;
        }

        /// <summary>
        /// The phase that follows phase index i: the jump target when a Choice picked one, else the phase's Next, else
        /// the following phase. -1 means the quest is complete; -2 means the target does not exist.
        /// </summary>
        public static int NextPhase(QuestDef q, int i, string jump)
        {
            string target = !string.IsNullOrEmpty(jump) ? jump : (i >= 0 && i < q.Phases.Count ? q.Phases[i].Next : "");
            if (string.IsNullOrEmpty(target)) return i + 1 < q.Phases.Count ? i + 1 : -1;
            if (target == QuestDefaults.End) return -1;
            int k = PhaseIndex(q, target);
            return k >= 0 ? k : -2;
        }

        /// <summary>
        /// True for a step that ends its phase by jumping: a Choice, a Branch (04E), or a Decide (04E) whose options all
        /// lead to a phase. A Decide whose options lead nowhere records the choice and lets the phase continue.
        /// </summary>
        public static bool EndsPhase(QuestStep s)
        {
            if (s == null) return false;
            if (s.Type == "Choice" || s.Type == "Branch") return true;
            if (s.Type != "Decide" || s.Options == null || s.Options.Count == 0) return false;
            foreach (ChoiceOption o in s.Options) if (o == null || string.IsNullOrEmpty(o.Phase)) return false;
            return true;
        }

        /// <summary>Every phase a phase can lead to (-1 for the end): its Choice, Branch or Decide targets when it has one, else its Next.</summary>
        public static List<int> Successors(QuestDef q, int i)
        {
            List<int> list = new List<int>();
            QuestPhase ph = q.Phases[i];
            bool choice = false;
            foreach (QuestStep s in ph.Steps)
            {
                if (!EndsPhase(s)) continue;
                choice = true;
                foreach (ChoiceOption o in s.Options)
                {
                    int k = o.Phase == QuestDefaults.End ? -1 : PhaseIndex(q, o.Phase);
                    if (!list.Contains(k)) list.Add(k);
                }
                // A Choice ends its phase, so the steps after it never run and the phase's own Next is never used.
                break;
            }
            if (!choice)
            {
                int n = NextPhase(q, i, null);
                if (!list.Contains(n)) list.Add(n);
            }
            return list;
        }

        /// <summary>Phases reachable from the first one.</summary>
        public static bool[] Reachable(QuestDef q)
        {
            bool[] seen = new bool[q.Phases.Count];
            if (q.Phases.Count == 0) return seen;
            Stack<int> open = new Stack<int>();
            open.Push(0);
            seen[0] = true;
            while (open.Count > 0)
            {
                int i = open.Pop();
                foreach (int k in Successors(q, i))
                {
                    if (k < 0 || seen[k]) continue;
                    seen[k] = true;
                    open.Push(k);
                }
            }
            return seen;
        }

        /// <summary>True when the end can be reached from phase i (so the quest cannot trap the player in a loop).</summary>
        public static bool CanFinishFrom(QuestDef q, int i)
        {
            bool[] seen = new bool[q.Phases.Count];
            Stack<int> open = new Stack<int>();
            open.Push(i);
            seen[i] = true;
            while (open.Count > 0)
            {
                int c = open.Pop();
                foreach (int k in Successors(q, c))
                {
                    if (k == -1) return true;
                    if (k < 0 || seen[k]) continue;
                    seen[k] = true;
                    open.Push(k);
                }
            }
            return false;
        }

        /// <summary>
        /// Steps along the longest route from the first phase to the end (for "step 3 of 9"). A branch the player does not
        /// take is not counted: QuestRun recounts with LongestFrom whenever a phase starts, so the last step reads "9 of 9".
        /// </summary>
        public static int CountSteps(QuestDef q)
        {
            return LongestFrom(q, 0);
        }

        /// <summary>Steps a phase actually runs: all of them, or up to and including its first Choice (a Choice ends its phase).</summary>
        public static int PhaseStepCount(QuestPhase p)
        {
            if (p == null || p.Steps == null) return 0;
            for (int i = 0; i < p.Steps.Count; i++) if (EndsPhase(p.Steps[i])) return i + 1;
            return p.Steps.Count;
        }

        /// <summary>
        /// The most steps any route from phase i to the end can still take, phase i included (each phase counted once per
        /// route, so a loop back is not counted twice). 0 for a missing phase.
        /// </summary>
        public static int LongestFrom(QuestDef q, int i)
        {
            if (q == null || q.Phases == null || i < 0 || i >= q.Phases.Count) return 0;
            return Longest(q, i, new bool[q.Phases.Count], 0);
        }

        private static int Longest(QuestDef q, int i, bool[] onRoute, int depth)
        {
            if (i < 0 || i >= q.Phases.Count || onRoute[i] || depth > 64) return 0;
            onRoute[i] = true;
            int best = 0;
            foreach (int k in Successors(q, i)) best = Mathf.Max(best, Longest(q, k, onRoute, depth + 1));
            onRoute[i] = false;
            return PhaseStepCount(q.Phases[i]) + best;
        }

        /// <summary>
        /// True for species that hunt in packs and are spawned together through Game.Creatures.SpawnGroup (one shared
        /// group, and one shared swarm body for Glass Mites); humanoid creatures and loners spawn one at a time.
        /// </summary>
        public static bool SpawnsAsPack(CreatureSpecies sp)
        {
            return sp != null && !sp.IsHumanoid && !sp.Boss && sp.PackMax > 1;
        }

        // ================================================================== rewards

        /// <summary>Contract pay per point of danger (Threat times count), on top of the template's base pay.</summary>
        public const float CashPerDanger = 160f;
        public const float LumenPerDanger = 20f;

        /// <summary>How dangerous one creature of a species is, roughly 0.1 (a mite) to 15 (a great beast).</summary>
        public static float Threat(CreatureSpecies sp)
        {
            if (sp == null) return 1f;
            float tier;
            switch (sp.Tier)
            {
                case CreatureTier.InnerWorld: tier = 1.25f; break;
                case CreatureTier.Frostline: tier = 1.45f; break;
                case CreatureTier.Badlands: tier = 1.55f; break;
                default: tier = 1f; break;
            }
            return Mathf.Pow(Mathf.Max(1f, sp.Health) / 100f, 0.6f) * Mathf.Pow(Mathf.Max(1f, sp.Damage) / 12f, 0.4f) * tier;
        }

        /// <summary>
        /// A contract's pay: the template's base plus a share for the danger of the job (threat times count), raised for
        /// long trips and, slowly, for loyalty (contracts already done for the guild). Always within sane bounds.
        /// </summary>
        public static QuestReward ScaleContractReward(ContractDef t, CreatureSpecies sp, int count, float distance, int done)
        {
            QuestReward r = new QuestReward();
            float danger = Threat(sp) * Mathf.Max(1, count);
            float trip = 1f + Mathf.Clamp(distance, 0f, 2500f) / 2500f * 0.5f;
            float loyal = 1f + 0.04f * Mathf.Clamp(done, 0, 10);
            float cash = (Mathf.Max(0, t.Cash) + CashPerDanger * danger) * trip * loyal;
            float lumen = (Mathf.Max(0, t.Lumen) + LumenPerDanger * danger) * trip * loyal;
            r.Cash = Mathf.Clamp(Mathf.RoundToInt(cash / 10f) * 10, 50, 12000);
            r.Lumen = Mathf.Clamp(Mathf.RoundToInt(lumen / 5f) * 5, 10, 2500);
            int rep = Mathf.Max(1, t.Rep) + (danger > 8f ? 1 : 0);
            r.Rep.Add(new RepAmount { Faction = t.Guild, Amount = Mathf.Clamp(rep, 1, 15) });
            if (t.Materials != null)
                foreach (MaterialAmount m in t.Materials)
                    if (m != null && MaterialDb.Get(m.Id) != null && m.Count > 0) r.Materials.Add(new MaterialAmount { Id = m.Id, Count = Mathf.Min(m.Count, 10) });
            if (sp != null && sp.Loot != null && sp.Loot.Length > 0 && t.Kind == "Hunt")
            {
                LootRoll roll = sp.Loot[0];
                if (MaterialDb.Get(roll.Material) != null)
                {
                    int n = Mathf.Clamp(Mathf.RoundToInt(count * Mathf.Max(0.25f, roll.Chance)), 1, 4);
                    AddMaterial(r, roll.Material, n);
                }
            }
            return r;
        }

        private static void AddMaterial(QuestReward r, string id, int n)
        {
            foreach (MaterialAmount m in r.Materials)
            {
                if (m.Id != id) continue;
                m.Count = Mathf.Min(10, m.Count + n);
                return;
            }
            r.Materials.Add(new MaterialAmount { Id = id, Count = n });
        }

        /// <summary>A one-line summary of a reward ("$1,200, 180 Lumen, 2 Rime Feather, +4 Frost Wardens").</summary>
        public static string RewardText(QuestReward r)
        {
            if (r == null) return "";
            StringBuilder sb = new StringBuilder();
            if (r.Cash > 0) Append(sb, MathUtil.Money(r.Cash));
            if (r.Lumen > 0) Append(sb, r.Lumen + " Lumen");
            if (r.SkillPoints > 0) Append(sb, r.SkillPoints + (r.SkillPoints == 1 ? " skill point" : " skill points"));
            if (r.Materials != null)
                foreach (MaterialAmount m in r.Materials)
                    if (m != null && m.Count > 0) Append(sb, m.Count + " " + MaterialDb.NameOf(m.Id));
            if (r.Rep != null)
                foreach (RepAmount a in r.Rep)
                {
                    if (a == null || a.Amount == 0) continue;
                    FactionDef f = FactionDb.Get(a.Faction);
                    Append(sb, (a.Amount > 0 ? "+" : "") + a.Amount + " " + (f != null ? f.Name : a.Faction));
                }
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string s)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(s);
        }

        /// <summary>Scales a reward (for example by how many civilians survived): cash, Lumen and material counts, never rep.</summary>
        public static QuestReward Scaled(QuestReward r, float factor)
        {
            QuestReward o = new QuestReward();
            if (r == null) return o;
            factor = Mathf.Clamp(factor, 0f, 4f);
            o.Cash = Mathf.RoundToInt(r.Cash * factor / 10f) * 10;
            o.Lumen = Mathf.RoundToInt(r.Lumen * factor / 5f) * 5;
            o.SkillPoints = factor >= 0.999f ? r.SkillPoints : 0;
            if (r.Materials != null)
                foreach (MaterialAmount m in r.Materials)
                {
                    if (m == null) continue;
                    int n = Mathf.RoundToInt(m.Count * factor);
                    if (n > 0) o.Materials.Add(new MaterialAmount { Id = m.Id, Count = n });
                }
            if (r.Rep != null)
                foreach (RepAmount a in r.Rep)
                    if (a != null && a.Amount != 0) o.Rep.Add(new RepAmount { Faction = a.Faction, Amount = a.Amount });
            return o;
        }

        // ================================================================== contracts

        /// <summary>Picks the template a board slot shows: weighted by Weight among the guild's templates the rep allows.</summary>
        public static ContractDef PickTemplate(IList<ContractDef> all, string guild, int rep, int seed)
        {
            float total = 0f;
            foreach (ContractDef t in all) if (t != null && t.Guild == guild && rep >= t.MinRep) total += t.Weight > 0f ? t.Weight : 1f;
            if (total <= 0f) return null;
            GRand r = new GRand(seed);
            float pick = r.Value() * total;
            ContractDef last = null;
            foreach (ContractDef t in all)
            {
                if (t == null || t.Guild != guild || rep < t.MinRep) continue;
                last = t;
                pick -= t.Weight > 0f ? t.Weight : 1f;
                if (pick < 0f) return t;
            }
            return last;
        }

        /// <summary>
        /// Builds a concrete, runnable contract from a template. board is where the guild posts it (the giver's spot);
        /// serial makes every posting different and repeatable. Returns null when the template cannot be placed.
        /// </summary>
        public static ContractOffer MakeContract(ContractDef t, CityLayout L, Vector3 board, string giverName, int serial, int done)
        {
            if (t == null || L == null) return null;
            int seed = MathUtil.Hash(serial * 31 + 7, Hash(t.Id));
            GRand r = new GRand(seed);
            ContractOffer o = new ContractOffer { Guild = t.Guild, TemplateId = t.Id, Serial = serial, Kind = t.Kind };
            CreatureSpecies sp = null;
            if (t.Species != null && t.Species.Count > 0)
            {
                sp = CreatureSpeciesDb.Get(t.Species[r.Range(0, t.Species.Count)]);
                if (sp == null) return null;
                o.Species = sp.Id;
            }
            int min = t.CountMin > 0 ? t.CountMin : (sp != null ? sp.PackMin : 1);
            int max = t.CountMax > 0 ? t.CountMax : (sp != null ? sp.PackMax : 1);
            if (max < min) max = min;
            o.Count = Mathf.Clamp(r.Range(min, max + 1), 1, 20);
            float distance;
            if (t.Kind == "Hunt" || t.Kind == "Rift")
            {
                string habitat = t.Kind == "Rift" ? Habitat.Inner : QuestPlaces.HabitatFor(sp, r.Range(0, 1000));
                if (string.IsNullOrEmpty(habitat)) return null;
                Vector3 p;
                string place;
                if (!QuestPlaces.HabitatPoint(L, habitat, seed ^ 0x5bd1e995, out p, out place)) return null;
                o.Point = p;
                o.Place = place;
                // Rifts tear open in the Outer City; Inner World species hunts happen inside the Veil.
                o.Inner = t.Kind == "Hunt" && habitat == Habitat.Inner;
                if (t.Kind == "Rift") o.Place = CityLayout.DistrictName(CityLayout.DistrictAt(p));
                distance = MathUtil.FlatDistance(board, p);
            }
            else
            {
                if (t.Destinations == null || t.Destinations.Count == 0) return null;
                // A destination inside a boss arena is passed over for the next one in the list (walking a client into
                // Titan Hollow would wake the Glacier Titan); with none left the template cannot be placed.
                int first = r.Range(0, t.Destinations.Count);
                string dest = null;
                Vector3 p = Vector3.zero;
                for (int k = 0; k < t.Destinations.Count && dest == null; k++)
                {
                    string d = t.Destinations[(first + k) % t.Destinations.Count];
                    Vector3 dp;
                    if (!QuestPlaces.Resolve(L, d, 0f, 0f, board, board, false, board, seed, out dp)) continue;
                    if (QuestPlaces.InBossArena(L, dp, QuestPlaces.ArenaMargin)) continue;
                    dest = d;
                    p = dp;
                }
                if (dest == null) return null;
                o.Destination = dest;
                o.Point = p;
                o.Place = QuestPlaces.DisplayName(L, dest, p);
                distance = MathUtil.FlatDistance(board, p);
            }
            o.Reward = ScaleContractReward(t, sp, t.Kind == "Hunt" || t.Kind == "Rift" ? o.Count : Mathf.Max(1, o.Count / 2), distance, done);
            o.Title = Fill(t.Title, o, sp);
            o.Brief = Fill(t.Brief, o, sp);
            o.Quest = BuildContractQuest(t, o, sp, giverName, board, L);
            return o;
        }

        private static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                if (s != null) foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        /// <summary>Replaces {species}, {count}, {place} and {dest} in a template line.</summary>
        public static string Fill(string text, ContractOffer o, CreatureSpecies sp)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string species = sp != null ? (o.Count == 1 ? sp.Name : sp.Plural) : "";
            return text.Replace("{species}", species).Replace("{count}", o.Count.ToString()).Replace("{place}", o.Place).Replace("{dest}", o.Place);
        }

        /// <summary>The contract as quest data: the same steps a guild quest would use.</summary>
        private static QuestDef BuildContractQuest(ContractDef t, ContractOffer o, CreatureSpecies sp, string giverName, Vector3 board, CityLayout L)
        {
            QuestDef q = new QuestDef
            {
                Id = "contract_" + t.Guild + "_" + o.Serial,
                Title = o.Title,
                Arc = t.Guild,
                Summary = o.Brief,
                IgnoreCrimes = true,
                Reward = o.Reward
            };
            QuestPhase ph = new QuestPhase { Id = "job", Title = o.Title };
            q.Phases.Add(ph);
            if (!string.IsNullOrEmpty(o.Brief))
            {
                QuestStep talk = new QuestStep { Type = "Talk" };
                talk.Lines.Add(new DialogueLine { Speaker = string.IsNullOrEmpty(giverName) ? "Contract board" : giverName, Text = o.Brief });
                ph.Steps.Add(talk);
            }
            string veil = o.Inner ? "Inner" : "";
            switch (t.Kind)
            {
                case "Hunt":
                    ph.Steps.Add(new QuestStep { Type = "GoTo", At = "World", OffsetX = o.Point.x, OffsetZ = o.Point.z, Radius = 45f, Objective = "Travel to " + o.Place + ".", Veil = veil });
                    ph.Steps.Add(new QuestStep { Type = "Kill", At = "World", OffsetX = o.Point.x, OffsetZ = o.Point.z, Radius = 22f, Species = o.Species, Count = o.Count, Veil = veil, HoldVeil = o.Inner });
                    break;
                case "Rift":
                    ph.Steps.Add(new QuestStep { Type = "GoTo", At = "World", OffsetX = o.Point.x, OffsetZ = o.Point.z, Radius = 40f, Objective = "Find the rift in " + o.Place + "." });
                    ph.Steps.Add(new QuestStep { Type = "CloseRift", At = "World", OffsetX = o.Point.x, OffsetZ = o.Point.z, Count = 1, Species = o.Species, MaxAlive = Mathf.Clamp(o.Count, 2, 6) });
                    break;
                case "Escort":
                {
                    QuestStep e = new QuestStep
                    {
                        Type = "Escort", At = "Giver", To = o.Destination, Name = string.IsNullOrEmpty(t.Name) ? "Client" : t.Name,
                        Look = string.IsNullOrEmpty(t.Look) ? "Civilian" : t.Look, Pacifist = true, Species = o.Species, Count = o.Species.Length > 0 ? o.Count : 0
                    };
                    ph.Steps.Add(e);
                    break;
                }
                case "Courier":
                {
                    ph.Steps.Add(new QuestStep { Type = "Interact", At = "Giver", Prop = "Satchel", Objective = "Pick up the package.", Prompt = "Press E to take the package" });
                    if (!string.IsNullOrEmpty(o.Species))
                    {
                        Vector3 mid = Vector3.Lerp(board, o.Point, 0.5f);
                        // In the city the halfway point is moved to the nearest sidewalk, so the ambush waits on the street.
                        if (L != null && !CityLayout.IsWilderness(CityLayout.DistrictAt(mid)))
                        {
                            int node = L.NearestWalkNode(mid, 120f);
                            if (node >= 0) mid = L.WalkNodes[node].Pos;
                        }
                        mid = QuestPlaces.PushOutOfArenas(L, mid);
                        ph.Steps.Add(new QuestStep { Type = "GoTo", At = o.Destination, Radius = 9f, Objective = "Deliver the package to " + o.Place + "." });
                        // The ambush waits on the way: it is spawned before the trip so the route crosses it.
                        ph.Steps.Insert(ph.Steps.Count - 1, new QuestStep { Type = "Spawn", At = "World", OffsetX = mid.x, OffsetZ = mid.z, Radius = 16f, Species = o.Species, Count = o.Count });
                    }
                    else ph.Steps.Add(new QuestStep { Type = "GoTo", At = o.Destination, Radius = 9f, Objective = "Deliver the package to " + o.Place + "." });
                    ph.Steps.Add(new QuestStep { Type = "Interact", At = o.Destination, Prop = "Crate", Objective = "Hand over the package.", Prompt = "Press E to deliver the package" });
                    if (t.Seconds > 0f) q.FailIf.Add(new QuestFailCondition { Type = "TimeLimit", Seconds = t.Seconds, Message = "The package went cold. The client walked." });
                    break;
                }
            }
            return q;
        }
    }
}
