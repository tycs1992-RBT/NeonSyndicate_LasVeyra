using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The live state of one quest run: where it is, what it spawned, and what it must undo when it ends.</summary>
    public sealed class QuestRun
    {
        public readonly QuestBook Book;
        public readonly QuestDef Quest;
        public readonly MissionContext C;
        public readonly bool Contract;
        /// <summary>Creature tag for everything this run spawns (Game.Creatures.AliveWithTag and DespawnTag use it).</summary>
        public readonly string Tag;
        public readonly Vector3 Giver;
        public readonly int Salt;
        /// <summary>Steps on the route the quest is taking: steps started so far plus the longest way on from the current phase.</summary>
        public int TotalSteps { get; private set; }
        /// <summary>The boss a Boss step of this run fought ("" when none): Release ends its fight and clears its summons.</summary>
        public string BossId = "";
        public int Phase = -1;
        public int Step = -1;
        /// <summary>Steps started so far (for "step 3 of 9").</summary>
        public int StepNumber;
        public string PhaseTitle = "";
        /// <summary>The only venue action this run currently authorizes. Cleared on every ending.</summary>
        public string CurrentVenue { get; internal set; } = "";
        public string CurrentAction { get; internal set; } = "";
        /// <summary>Set by a Choice step: the phase to go to next.</summary>
        public string Jump;
        public Vector3 Last;
        public bool HasLast;
        /// <summary>The creature the last Scan step read; a Kill step of the same species right after fights it first.</summary>
        public CharacterBase Scanned;
        public string ScannedSpecies = "";
        public readonly List<CharacterBase> Creatures = new List<CharacterBase>();
        public readonly List<NpcController> Allies = new List<NpcController>();
        public readonly List<NpcController> StandIns = new List<NpcController>();
        // ---- Underheart and Bonds 04E
        /// <summary>True when the last place resolved (Place) was a 04E token: an exact 3D point that must not be ground-snapped.</summary>
        public bool LastExact;
        /// <summary>The phase the run starts at (a resumed checkpoint; 0 from the beginning).</summary>
        public int StartPhase;
        /// <summary>Undo actions for what 04E steps started (rescues, fights, mechanisms, the lit conduit), run once by Release.</summary>
        public readonly List<Action> Cleanups = new List<Action>();
        private bool _veilHeld;
        private bool _ambientOff;
        private bool _timeLimitOn;

        public QuestRun(QuestBook book, QuestDef quest, MissionContext c, bool contract, Vector3 giver)
        {
            Book = book;
            Quest = quest;
            C = c;
            Contract = contract;
            Giver = giver;
            Tag = (contract ? "" : "quest_") + quest.Id;
            // Masked rather than Abs: Abs of int.MinValue overflows.
            Salt = Hash(quest.Id) & 0x7fffffff;
            TotalSteps = QuestLogic.CountSteps(quest);
        }

        private static int Hash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
        }

        public CityLayout L { get { return Game.Layout; } }

        /// <summary>
        /// Resolves a step place (At plus offset), remembers it as "Previous", and returns the ground point. A 04E place token
        /// (Venue:, Under:, Cast:, Arsenal:) returns its exact live point instead (LastExact), never snapped from above.
        /// </summary>
        public Vector3 Place(string at, float ox, float oz)
        {
            Vector3 p;
            if (CampaignRules04E.IsPlaceToken(at))
            {
                LastExact = true;
                p = ExactPoint(at, ox, oz);
                Last = p;
                HasLast = true;
                return p;
            }
            LastExact = false;
            if (!QuestPlaces.Resolve(L, at, ox, oz, Giver, Last, HasLast, Game.PlayerPosition, Salt + Step * 31 + Phase * 7, out p)) p = HasLast ? Last : Giver;
            Last = p;
            HasLast = true;
            return QuestWorld.Ground(p);
        }

        /// <summary>Resolves a second place (To) without changing "Previous" (exact for 04E place tokens).</summary>
        public Vector3 PlaceTo(string at, float ox, float oz)
        {
            Vector3 p;
            if (CampaignRules04E.IsPlaceToken(at)) return ExactPoint(at, ox, oz);
            if (!QuestPlaces.Resolve(L, at, ox, oz, Giver, Last, HasLast, Game.PlayerPosition, Salt + Step * 37 + 5, out p)) p = HasLast ? Last : Giver;
            return QuestWorld.Ground(p);
        }

        /// <summary>A token's exact point (with an offset, a short probe finds the floor there). A miss falls back to the last place and warns.</summary>
        private Vector3 ExactPoint(string at, float ox, float oz)
        {
            Vector3 p;
            float yaw;
            string why;
            if (!CampaignPlacesRuntime04E.TryResolve(at, out p, out yaw, out why))
            {
                Log.Warn("Quest " + Quest.Id + ": " + why + " Using the last known place.");
                return HasLast ? Last : Giver;
            }
            if (ox == 0f && oz == 0f) return p;
            Vector3 q = p + new Vector3(ox, 0f, oz);
            RaycastHit hit;
            if (Physics.Raycast(q + Vector3.up * 1.2f, Vector3.down, out hit, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return q;
        }

        /// <summary>Uses a map point found another way (a boss arena) as the step's place: remembered as "Previous", returned on the ground.</summary>
        public Vector3 PlaceAt(Vector3 p)
        {
            Last = p;
            HasLast = true;
            return QuestWorld.Ground(p);
        }

        /// <summary>A phase starts: the step total becomes the steps done so far plus the longest way on from this phase.</summary>
        public void PlanFrom(int phase)
        {
            TotalSteps = StepNumber + QuestLogic.LongestFrom(Quest, phase);
        }

        private CreatureSpawnOptions Options(bool hostile, bool elite, float healthScale, float damageScale, string name)
        {
            return new CreatureSpawnOptions
            {
                Tag = Tag,
                Hostile = hostile,
                Persistent = true,
                Elite = elite,
                HealthScale = healthScale > 0f ? healthScale : 1f,
                DamageScale = damageScale > 0f ? damageScale : 1f,
                DisplayName = name,
                Seed = Salt + Creatures.Count * 977
            };
        }

        /// <summary>Stand-ins made by a spawn join the mission's NPCs so the mission cleans them up.</summary>
        private void AdoptStandIns(int before)
        {
            for (int i = before; i < StandIns.Count; i++) if (StandIns[i] != null && !C.Npcs.Contains(StandIns[i])) C.Npcs.Add(StandIns[i]);
        }

        /// <summary>Spawns a tagged creature for this run (stand-ins join the mission's NPCs so the mission cleans them up).</summary>
        public CharacterBase Spawn(string species, Vector3 pos, float yaw, bool hostile, bool elite, float healthScale, float damageScale, string name)
        {
            CreatureSpawnOptions o = Options(hostile, elite, healthScale, damageScale, name);
            int before = StandIns.Count;
            CharacterBase c = QuestWorld.SpawnCreature(species, QuestWorld.OpenGround(pos, Last), yaw, o, C.M.transform, StandIns);
            AdoptStandIns(before);
            if (c != null) Creatures.Add(c);
            return c;
        }

        /// <summary>A pack of a pack species around a point, as one group (QuestLogic.SpawnsAsPack), tagged for this run.</summary>
        public List<CharacterBase> SpawnGroup(string species, Vector3 center, int count, bool hostile, bool elite, float healthScale, float damageScale, string name)
        {
            CreatureSpawnOptions o = Options(hostile, elite, healthScale, damageScale, name);
            int before = StandIns.Count;
            List<CharacterBase> list = QuestWorld.SpawnPack(species, QuestWorld.OpenGround(center, Last), count, o, C.M.transform, StandIns);
            AdoptStandIns(before);
            Creatures.AddRange(list);
            return list;
        }

        /// <summary>
        /// The quest's boss through the creature director's own boss fight (phases, ring attacks, summons, the boss bar),
        /// tagged for this run; the stand-in from Spawn while the director cannot. directed tells which one it got.
        /// </summary>
        public CharacterBase SpawnBoss(string species, Vector3 pos, float yaw, float healthScale, float damageScale, string name, out bool directed)
        {
            BossId = species;
            CreatureSpawnOptions o = new CreatureSpawnOptions
            {
                Tag = Tag,
                Hostile = true,
                Persistent = true,
                HealthScale = healthScale > 0f ? healthScale : 1f,
                DamageScale = damageScale > 0f ? damageScale : 1f,
                DisplayName = name
            };
            CharacterBase b = QuestWorld.SpawnBoss(species, QuestWorld.OpenGround(pos, Last), yaw, o);
            directed = b != null;
            if (b != null)
            {
                Creatures.Add(b);
                return b;
            }
            b = Spawn(species, pos, yaw, true, false, healthScale, damageScale, name);
            // A director that routes plain spawns of a boss to its boss fight still counts as running the fight.
            directed = b != null && Game.Creatures != null && Game.Creatures.ActiveBoss == b;
            return b;
        }

        /// <summary>Holds the Veil open (no crystal-energy drain) until Release or the step ends.</summary>
        public void HoldVeil(bool on)
        {
            if (Game.Inner == null) return;
            if (on && !_veilHeld)
            {
                _veilHeld = true;
                Game.Inner.DrainPaused = true;
            }
            else if (!on && _veilHeld)
            {
                _veilHeld = false;
                Game.Inner.DrainPaused = false;
            }
        }

        public void AmbientOff()
        {
            if (Game.Creatures == null || _ambientOff) return;
            _ambientOff = true;
            Game.Creatures.AmbientEnabled = false;
        }

        public bool TimeLimitOn { get { return _timeLimitOn; } set { _timeLimitOn = value; } }

        /// <summary>Undoes what the run changed in the world (called by the mission cleanup on every ending).</summary>
        public void Release()
        {
            CurrentVenue = "";
            CurrentAction = "";
            for (int i = Cleanups.Count - 1; i >= 0; i--)
            {
                try { Cleanups[i](); }
                catch (Exception e) { Log.Exception("Quest cleanup " + Quest.Id, e); }
            }
            Cleanups.Clear();
            HoldVeil(false);
            if (_ambientOff && Game.Creatures != null) Game.Creatures.AmbientEnabled = true;
            _ambientOff = false;
            EndBoss();
            QuestWorld.Despawn(Tag, null, false);
            if (Game.Hud != null) Game.Hud.ClearCounter();
        }

        /// <summary>A boss still standing when the quest ends leaves with its fight, and its summons (tagged for the boss, not the quest) go too.</summary>
        private void EndBoss()
        {
            if (string.IsNullOrEmpty(BossId) || Game.Creatures == null) return;
            try
            {
                CharacterBase b = Game.Creatures.ActiveBoss;
                if (b != null && CreatureDirector.TagOf(b) == Tag) Game.Creatures.EndBossFight();
                Game.Creatures.DespawnTag(CreatureDirector.BossAddsTag(BossId));
            }
            catch (Exception e) { Log.Exception("Quest boss cleanup " + BossId, e); }
        }

        public bool AnyAllyDead()
        {
            foreach (NpcController n in Allies) if (n == null || n.IsDead) return true;
            return false;
        }

        public bool InPhases(List<string> phases)
        {
            if (phases == null || phases.Count == 0) return true;
            if (Phase < 0 || Phase >= Quest.Phases.Count) return false;
            return phases.Contains(Quest.Phases[Phase].Id);
        }

        private string _progress = "";
        private int _progressStep = -1;
        private int _progressTotal = -1;
        private string _progressPhase;

        /// <summary>"Phase title" or "step 3 of 9" for the HUD tracker and the journal (rebuilt only when it changes).</summary>
        public string ProgressText()
        {
            if (_progressStep == StepNumber && _progressTotal == TotalSteps && ReferenceEquals(_progressPhase, PhaseTitle)) return _progress;
            _progressStep = StepNumber;
            _progressTotal = TotalSteps;
            _progressPhase = PhaseTitle;
            string s = !string.IsNullOrEmpty(PhaseTitle) ? PhaseTitle : "";
            string steps = TotalSteps > 1 ? "step " + Mathf.Clamp(StepNumber, 1, TotalSteps) + " of " + TotalSteps : "";
            if (s.Length > 0 && steps.Length > 0) _progress = s + "  |  " + steps;
            else _progress = s.Length > 0 ? s : steps;
            return _progress;
        }
    }

    /// <summary>
    /// Runs quest data as a MissionManager script: the phases in order (or as Next and Choice steps send them), each
    /// step with its Veil gate, and the quest's fail conditions on top of the mission's own (death, arrest).
    /// Any exception fails the quest through the mission runner instead of breaking the game.
    /// </summary>
    public static class QuestRunner
    {
        public static IEnumerator Run(QuestRun r)
        {
            QuestDef q = r.Quest;
            if (q.IgnoreCrimes) r.C.M.CrimesIgnored = true;
            if (q.NoAmbient) r.AmbientOff();
            InstallFailConditions(r);
            // A checkpointed campaign quest resumes at its committed phase (04E); everything else starts at the top.
            int pi = q.Phases.Count > 0 ? Mathf.Clamp(r.StartPhase, 0, q.Phases.Count - 1) : 0;
            if (pi > 0)
            {
                if (Game.Hud != null) Game.Hud.Notify("Resuming at the checkpoint: " + (string.IsNullOrEmpty(q.Phases[pi].Title) ? q.Phases[pi].Id : q.Phases[pi].Title) + ".", 4f);
                Log.Info("Quest " + q.Id + ": resuming at checkpoint phase '" + q.Phases[pi].Id + "'.");
            }
            int guard = 0;
            while (pi >= 0 && pi < q.Phases.Count && guard++ < 256)
            {
                QuestPhase ph = q.Phases[pi];
                r.Phase = pi;
                r.PhaseTitle = ph.Title;
                r.Jump = null;
                r.PlanFrom(pi);
                PhaseTimeLimit(r);
                if (ph.Checkpoint && r.Book != null) r.Book.CommitCheckpoint04E(r, pi);
                for (int si = 0; si < ph.Steps.Count; si++)
                {
                    r.Step = si;
                    r.StepNumber++;
                    yield return QuestSteps.Run(r, ph.Steps[si]);
                    if (r.Jump != null) break;
                }
                int next = QuestLogic.NextPhase(q, pi, r.Jump);
                if (next == -2)
                {
                    Log.Warn("Quest " + q.Id + ": phase '" + (r.Jump ?? ph.Next) + "' does not exist; ending the quest.");
                    next = -1;
                }
                pi = next;
            }
            r.C.ClearTarget();
            r.C.ClearTimer();
        }

        // ------------------------------------------------------------------ fail conditions

        private static void InstallFailConditions(QuestRun r)
        {
            foreach (QuestFailCondition f in r.Quest.FailIf)
            {
                QuestFailCondition fc = f;
                string msg = !string.IsNullOrEmpty(fc.Message) ? fc.Message : DefaultFail(fc.Type);
                switch (fc.Type)
                {
                    case "AllyDies":
                        r.C.FailIf(() => r.InPhases(fc.Phases) && r.AnyAllyDead(), msg);
                        break;
                    case "TimeLimit":
                        if (fc.Phases.Count == 0)
                        {
                            r.C.Timer(fc.Seconds, "TIME", msg);
                            r.TimeLimitOn = true;
                        }
                        break;
                    case "LeaveArea":
                    {
                        Vector3 center = Vector3.zero;
                        bool resolved = false;
                        float outside = 0f;
                        float grace = fc.Seconds > 0f ? fc.Seconds : QuestDefaults.LeaveGrace;
                        r.C.FailIf(() =>
                        {
                            if (!r.InPhases(fc.Phases)) { outside = 0f; return false; }
                            if (!resolved)
                            {
                                Vector3 p;
                                if (!QuestPlaces.Resolve(r.L, fc.At, fc.OffsetX, fc.OffsetZ, r.Giver, r.Last, r.HasLast, Game.PlayerPosition, r.Salt, out p)) p = r.Giver;
                                center = p;
                                resolved = true;
                            }
                            if (MathUtil.FlatDistance(Game.PlayerPosition, center) > fc.Radius) outside += Time.deltaTime;
                            else outside = 0f;
                            if (outside > 0f && Game.Hud != null) Game.Hud.SetProgress("Get back to the area!", 1f - outside / grace);
                            return outside > grace;
                        }, msg);
                        break;
                    }
                    case "LeaveVeil":
                    {
                        float outside = 0f;
                        float grace = fc.Seconds > 0f ? fc.Seconds : 6f;
                        r.C.FailIf(() =>
                        {
                            if (!r.InPhases(fc.Phases) || Game.Inner == null) { outside = 0f; return false; }
                            if (!Game.Inner.IsInner) outside += Time.deltaTime;
                            else outside = 0f;
                            if (outside > 0f && Game.Hud != null) Game.Hud.SetProgress("Shift back into the Inner World!", 1f - outside / grace);
                            return outside > grace;
                        }, msg);
                        break;
                    }
                    case "Wanted":
                        r.C.FailIf(() => r.InPhases(fc.Phases) && Game.Wanted != null && Game.Wanted.Stars >= Mathf.Max(1, fc.Stars), msg);
                        break;
                    case "Hour":
                        r.C.FailIf(() => r.InPhases(fc.Phases) && Game.Atmo != null && EventSelector.HourInWindow(Game.Atmo.Hours, fc.HourMin, fc.HourMax), msg);
                        break;
                    case "Vehicle":
                        r.C.FailIf(() => r.InPhases(fc.Phases) && Game.Player != null && Game.Player.InVehicle, msg);
                        break;
                }
            }
        }

        /// <summary>Phase-scoped time limits start on the first listed phase and stop on the first phase not listed.</summary>
        private static void PhaseTimeLimit(QuestRun r)
        {
            foreach (QuestFailCondition fc in r.Quest.FailIf)
            {
                if (fc.Type != "TimeLimit" || fc.Phases.Count == 0) continue;
                bool inScope = r.InPhases(fc.Phases);
                if (inScope && !r.TimeLimitOn)
                {
                    r.C.Timer(fc.Seconds, "TIME", !string.IsNullOrEmpty(fc.Message) ? fc.Message : DefaultFail(fc.Type));
                    r.TimeLimitOn = true;
                }
                else if (!inScope && r.TimeLimitOn)
                {
                    r.C.ClearTimer();
                    r.TimeLimitOn = false;
                }
            }
        }

        private static string DefaultFail(string type)
        {
            switch (type)
            {
                case "AllyDies": return "Someone you were protecting fell.";
                case "TimeLimit": return "You ran out of time.";
                case "LeaveArea": return "You left the area.";
                case "LeaveVeil": return "The Veil closed behind you.";
                case "Wanted": return "The police got involved.";
                case "Hour": return "The hour has passed.";
                case "Vehicle": return "This had to be done on foot.";
                default: return "The quest failed.";
            }
        }
    }
}
