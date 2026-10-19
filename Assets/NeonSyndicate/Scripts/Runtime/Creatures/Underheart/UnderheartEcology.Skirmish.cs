using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One running finite encounter: its spec, handle, arena, ledger, owned foes, reinforcement and lease.</summary>
    internal sealed class SkirmishRun04E
    {
        public int Serial;
        public SkirmishSpec Spec;
        public SkirmishHandle Handle;
        public SkirmishArena04E Arena;
        public SkirmishLedger04E Ledger;
        public string Tag = "";
        public string LeaseOwner = "";
        public string AftermathOwner = "";
        public string Title = "";
        public readonly List<string> Roster = new List<string>();
        public readonly List<bool> Elite = new List<bool>();
        public readonly List<Vector3> Posts = new List<Vector3>();
        public readonly List<int> RosterTokens = new List<int>();
        public readonly List<SkirmishActor04E> Actors = new List<SkirmishActor04E>();
        public int NextRoster;
        /// <summary>0 preparing (spawning the roster), 1 active, 2 ending (foes withdrawing after an abort), 3 finished.</summary>
        public int Phase;
        public float HealthScale = 1f, DamageScale = 1f;
        public bool InnerAtStart;
        public bool Engaged;
        public float AwaySeconds;
        public int ReinforceIndex;
        public int ReinforceToken;
        public bool ReinforceScheduled;
        public float ReinforceIn;
        public float NextReinforceAllowed;
        public Vector3 ReinforcePoint;
        public readonly OwnedRing04E ReinforceRing = new OwnedRing04E();
        public SkirmishEnd04E End;
        public float WithdrawT;
    }

    /// <summary>One owned foe: its body, death hook and how it left (confirmed dead or lost).</summary>
    internal sealed class SkirmishActor04E
    {
        public string Species = "";
        public CharacterBase Body;
        public CharacterHealth Health;
        public Action<DamageInfo> OnDied;
        public bool Dead;
        public bool Lost;
        public bool Reinforcement;
        public Vector3 Post;
        public float SpawnedAt;
    }

    public sealed partial class UnderheartEcology
    {
        private static readonly Color EncounterColor = new Color(1f, 0.62f, 0.25f);
        private static readonly Color ReinforceColor = new Color(1f, 0.45f, 0.2f, 1f);

        private readonly List<SkirmishRun04E> _runs = new List<SkirmishRun04E>();
        private readonly List<string> _speciesScratch = new List<string>();
        private readonly List<bool> _eliteScratch = new List<bool>();
        private int _serial;

        // ================================================================== starting

        private SkirmishHandle StartRun(SkirmishSpec spec, out string reason)
        {
            if (spec == null) return Refuse("No encounter was described.", out reason);
            if (Game.Creatures == null) return Refuse("The creature system is not running.", out reason);
            int reinforcements;
            if (!SkirmishRules04E.Expand(spec.Roster, spec.Reinforcements, KnownFoe, _speciesScratch, _eliteScratch, out reinforcements, out reason))
                return Refuse(reason, out reason);
            if (!MathUtil.IsFinite(spec.Center)) return Refuse("The encounter has no place.", out reason);
            string room = spec.RoomId ?? "";
            // The same owner asking again for its running room gets that run (a quest step restarting never doubles it).
            for (int i = 0; i < _runs.Count; i++)
            {
                SkirmishRun04E r = _runs[i];
                if (r.Phase <= 1 && !string.IsNullOrEmpty(room) && r.Spec.RoomId == room && r.Spec.Owner == spec.Owner)
                {
                    reason = null;
                    return r.Handle;
                }
            }
            if (!string.IsNullOrEmpty(room) && !spec.AllowCleared && RoomCleared(room))
            {
                reason = null;
                Note("room " + room + " is already cleared this run: reported done, nothing spawns");
                return new SkirmishHandle { Done = true, Status = "This room is already cleared.", Total = 0 };
            }
            float radius = SkirmishRules04E.ClampRadius(spec.Radius);
            int running = 0;
            for (int i = 0; i < _runs.Count; i++)
            {
                SkirmishRun04E r = _runs[i];
                if (r.Phase > 1) continue;
                running++;
                if (SkirmishRules04E.ArenasOverlap(spec.Center, radius, r.Arena.Center, r.Arena.Radius))
                    return Refuse("Another fight already owns this room (" + r.Title + ").", out reason);
            }
            if (running >= SkirmishRules04E.MaxConcurrent) return Refuse("Two fights are already running underground.", out reason);
            if (Game.Creatures.ActiveBoss != null) return Refuse("A boss fight is running.", out reason);
            if (Game.UnderBosses != null && Game.UnderBosses.FightActive && !OwnedByBosses(spec.Owner)) return Refuse("A guardian fight owns the area.", out reason);
            string foreign = ForeignAdmission(spec.Center);
            if (foreign != null) return Refuse("Another encounter owns this area (" + foreign + ").", out reason);

            SkirmishRun04E run = new SkirmishRun04E();
            run.Serial = ++_serial;
            run.Spec = spec;
            run.Tag = "uh04e_sk_" + run.Serial;
            run.LeaseOwner = LeasePrefix + "skirmish:" + run.Serial;
            run.AftermathOwner = LeasePrefix + "aftermath:" + (string.IsNullOrEmpty(room) ? run.Serial.ToString() : room);
            run.Roster.AddRange(_speciesScratch);
            run.Elite.AddRange(_eliteScratch);
            run.HealthScale = SkirmishRules04E.ClampScale(spec.HealthScale);
            run.DamageScale = SkirmishRules04E.ClampScale(spec.DamageScale);
            run.InnerAtStart = Game.Inner != null && Game.Inner.IsInner;
            run.Ledger = new SkirmishLedger04E(run.Roster.Count, reinforcements, SkirmishRules04E.AliveCap((int)Settings.Difficulty));
            for (int i = 0; i < run.Roster.Count; i++) run.RosterTokens.Add(run.Ledger.ReserveRoster());
            run.Arena = new SkirmishArena04E { Serial = run.Serial, Tag = run.Tag, Center = spec.Center, Radius = radius };
            run.Title = !string.IsNullOrEmpty(spec.Title) ? spec.Title : TitleFor(run.Roster);
            if (spec.Points != null && spec.Points.Count > 0)
            {
                for (int i = 0; i < run.Roster.Count + reinforcements; i++) run.Posts.Add(spec.Points[i % spec.Points.Count]);
            }
            else SkirmishRules04E.ArenaPoints(spec.Center, radius, run.Roster.Count + reinforcements, PostAngle(room, run.Serial), run.Posts);
            run.Handle = new SkirmishHandle { Total = run.Ledger.DeclaredTotal, Status = "Foes are taking position." };
            SkirmishRun04E captured = run;
            run.Handle.Abort = () => AbortRun(captured, SkirmishEnd04E.AbortedByOwner, false);
            _runs.Add(run);
            bool fresh = LocalQuiet.AcquireAdmission(run.LeaseOwner, spec.Center, radius + SkirmishRules04E.AdmissionPad);
            ClearResidentsIn(spec.Center, radius + 4f);
            Note("#" + run.Serial + " " + run.Title + " for " + (string.IsNullOrEmpty(spec.Owner) ? "?" : spec.Owner) + (string.IsNullOrEmpty(room) ? "" : " room " + room)
                + ": " + run.Roster.Count + " foes, up to " + reinforcements + " reinforcement(s), radius " + radius.ToString("0") + " m, admission lease "
                + (fresh ? "taken" : "refreshed") + " | " + run.Ledger);
            if (Game.Hud != null && NearPlayer(run, 80f))
                Game.Hud.Banner(run.Title.ToUpperInvariant(), run.Roster.Count + (run.Roster.Count == 1 ? " foe" : " foes")
                    + (reinforcements > 0 ? ", up to " + reinforcements + " announced reinforcement" + (reinforcements == 1 ? "" : "s") : "") + ". Clear the room.", EncounterColor, 4f);
            reason = null;
            return run.Handle;
        }

        private static SkirmishHandle Refuse(string why, out string reason)
        {
            reason = why;
            return new SkirmishHandle { Failed = true, Status = why };
        }

        private static bool KnownFoe(string id)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(id);
            return sp != null && !sp.Boss;
        }

        private static bool OwnedByBosses(string owner)
        {
            return !string.IsNullOrEmpty(owner) && (owner.StartsWith("boss", StringComparison.Ordinal) || owner.StartsWith("underboss", StringComparison.Ordinal));
        }

        private static bool RoomCleared(string room)
        {
            UnderheartWorld u = Game.Underheart;
            if (u == null) return false;
            try { return u.IsCleared(room); }
            catch (Exception e)
            {
                Log.Exception("Underheart IsCleared", e);
                return false;
            }
        }

        /// <summary>The owner of another system's running admission lease over a point, or null (aftermaths never refuse).</summary>
        private static string ForeignAdmission(Vector3 p)
        {
            IList<QuietLease> leases = LocalQuiet.Leases;
            for (int i = 0; i < leases.Count; i++)
            {
                QuietLease l = leases[i];
                if (l == null || l.Kind != QuietKind.Admission || l.Owner == null) continue;
                if (l.Owner.StartsWith(LeasePrefix, StringComparison.Ordinal)) continue;
                if (l.Covers(p)) return l.Owner;
            }
            return null;
        }

        private static string TitleFor(List<string> roster)
        {
            if (roster.Count == 0) return "Encounter";
            CreatureSpecies sp = CreatureSpeciesDb.Get(roster[0]);
            bool mixed = false;
            for (int i = 1; i < roster.Count; i++) if (roster[i] != roster[0]) mixed = true;
            string name = sp != null ? (roster.Count > 1 ? sp.Plural : sp.Name) : roster[0];
            return mixed ? name + " and company" : name;
        }

        private static float PostAngle(string room, int serial)
        {
            int h = 7;
            if (!string.IsNullOrEmpty(room)) for (int i = 0; i < room.Length; i++) h = h * 31 + room[i];
            else h = serial * 7919;
            return (h & 0xffff) / 65535f * 360f;
        }

        private static bool NearPlayer(SkirmishRun04E r, float extra)
        {
            if (Game.Player == null) return false;
            Vector3 pp = Game.PlayerPosition;
            return WorldBounds.SameLayer(pp, r.Arena.Center) && MathUtil.FlatDistance(pp, r.Arena.Center) <= r.Arena.Radius + extra;
        }

        // ================================================================== frame

        private void TickRun(SkirmishRun04E r, float dt, float active)
        {
            switch (r.Phase)
            {
                case 0: TickPreparing(r); break;
                case 1: TickActive(r, dt, active); break;
                case 2: TickEnding(r, dt); break;
            }
        }

        /// <summary>Two roster bodies per frame at their posts; a body that cannot be made ends the encounter (no victory).</summary>
        private void TickPreparing(SkirmishRun04E r)
        {
            for (int k = 0; k < 2 && r.NextRoster < r.RosterTokens.Count; k++)
            {
                int index = r.NextRoster++;
                int token = r.RosterTokens[index];
                if (!r.Ledger.Admit(token))
                {
                    AbortRun(r, SkirmishEnd04E.SpawnFailed, true);
                    return;
                }
                SkirmishActor04E a = SpawnActor(r, r.Roster[index], r.Elite[index], r.Posts[index % r.Posts.Count], index, false);
                if (a == null)
                {
                    r.Ledger.AdmitFailed(token);
                    Note("#" + r.Serial + " the " + r.Roster[index] + " could not be created");
                    AbortRun(r, SkirmishEnd04E.SpawnFailed, true);
                    return;
                }
            }
            if (r.NextRoster >= r.RosterTokens.Count)
            {
                r.Phase = 1;
                r.Handle.Status = "Enter the room: " + r.Ledger.DeclaredTotal + " foes.";
                Note("#" + r.Serial + " the roster is in place | " + r.Ledger);
            }
        }

        private SkirmishActor04E SpawnActor(SkirmishRun04E r, string species, bool elite, Vector3 post, int index, bool reinforcement)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null) return null;
            Vector3 p = PlacePost(r, post);
            Vector3 face = MathUtil.Flat(r.Arena.Center - p);
            float yaw = face.sqrMagnitude > 0.01f ? MathUtil.YawOf(face) : (index * 67f) % 360f;
            CreatureSpawnOptions o = new CreatureSpawnOptions
            {
                Tag = r.Tag,
                NoRewards = r.Spec.NoRewards04K && r.Spec.Owner=="quest:"+VanguardRules04K.Quest,
                Vanguard04K = r.Spec.Vanguard04K && species=="quartz_drake" && r.Spec.Owner=="quest:"+VanguardRules04K.Quest,
                DisplayName = r.Spec.Vanguard04K ? "The Fractured Bell" : null,
                Hostile = true,
                Persistent = true,
                Elite = elite,
                HealthScale = r.Spec.Vanguard04K ? .12f : r.HealthScale,
                DamageScale = r.DamageScale,
                Seed = SeedFor(r.Serial, index, reinforcement)
            };
            CharacterBase body = cd.Spawn(species, p, yaw, o);
            if (body == null || body.Health == null)
            {
                if (body != null) Destroy(body.gameObject);
                return null;
            }
            SkirmishActor04E a = new SkirmishActor04E { Species = species, Body = body, Health = body.Health, Reinforcement = reinforcement, Post = body.transform.position, SpawnedAt = Time.time };
            UnderheartRoleAttach04E.Bind(body, r.Arena, a.Post);
            SkirmishRun04E run = r;
            a.OnDied = info => CountDeath(run, a);
            body.Health.Died += a.OnDied;
            r.Actors.Add(a);
            r.Arena.Members.Add(body);
            return a;
        }

        /// <summary>A post on its own floor (short probe), off damaging heat; the arena centre when the post has no floor.</summary>
        private static Vector3 PlacePost(SkirmishRun04E r, Vector3 post)
        {
            float y;
            if (CreatureDirector.GroundAt(post, 1.2f, out y) && Mathf.Abs(y - r.Arena.Center.y) < 3f)
            {
                Vector3 p = new Vector3(post.x, y, post.z);
                float heat;
                if (Game.Underheart == null || !Game.Underheart.HazardAt(p, out heat)) return p;
            }
            return r.Arena.Center;
        }

        private static int SeedFor(int serial, int index, bool reinforcement)
        {
            int h = serial * 104729 + index * 7919 + (reinforcement ? 31337 : 0);
            return (h & 0x7fffffff) | 1;
        }

        private void TickActive(SkirmishRun04E r, float dt, float active)
        {
            Bookkeep(r);
            if (r.Ledger.VictoryReady)
            {
                Victory(r);
                return;
            }
            PlayerController p = Game.Player;
            Vector3 pp = Game.PlayerPosition;
            bool inside = p != null && !p.IsDead && SkirmishRules04E.PlayerEngaged(pp, r.Arena.Center, r.Arena.Radius);
            // Engaged once the player steps into the room, or kills a foe from outside it (a long shot still counts), so the
            // announced reinforcements and the leave-area rule apply either way.
            if ((inside || r.Ledger.Dead > 0) && !r.Engaged)
            {
                r.Engaged = true;
                Note("#" + r.Serial + " the player engaged" + (inside ? "" : " (from outside the room)"));
            }
            r.Arena.PlayerEngaged = inside;
            if (r.Engaged) r.Arena.EngagedSeconds += active;
            bool watch = Game.State == GameState.Playing || Game.State == GameState.Wasted || Game.State == GameState.Busted;
            if (watch)
            {
                bool away = p == null || SkirmishRules04E.PlayerAway(pp, r.Arena.Center, r.Arena.Radius);
                r.AwaySeconds = away ? r.AwaySeconds + active : 0f;
                SkirmishRules04E.WatchFacts f = new SkirmishRules04E.WatchFacts();
                f.PlayerDown = p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted;
                f.InVeil = (Game.Inner != null && Game.Inner.IsInner) != r.InnerAtStart;
                f.Unwinnable = r.Ledger.Unwinnable;
                f.AnyFailedRoster = r.Ledger.FailedRoster > 0;
                f.Engaged = r.Engaged;
                f.AwaySeconds = r.AwaySeconds;
                SkirmishEnd04E end = SkirmishRules04E.Check(f);
                if (end != SkirmishEnd04E.None)
                {
                    AbortRun(r, end, true);
                    return;
                }
            }
            TickReinforcement(r, dt, active);
            if (r.Ledger.VictoryReady)
            {
                Victory(r);
                return;
            }
            UpdateHandle(r);
        }

        /// <summary>Bodies that vanished without dying (never a kill), deaths the event missed, bodies that never got built.</summary>
        private void Bookkeep(SkirmishRun04E r)
        {
            for (int i = 0; i < r.Actors.Count; i++)
            {
                SkirmishActor04E a = r.Actors[i];
                if (a.Dead || a.Lost) continue;
                if (a.Body == null)
                {
                    MarkLost(r, a, "was removed without a death (not a defeat)");
                    continue;
                }
                if (a.Body.IsDead)
                {
                    CountDeath(r, a);
                    continue;
                }
                Creature c = a.Body as Creature;
                if (c != null && !c.Ready && Time.time - a.SpawnedAt > SkirmishRules04E.BuildTimeout)
                {
                    MarkLost(r, a, "never finished building");
                    c.Despawn(false);
                }
            }
            r.Arena.Members.RemoveAll(b => b == null || b.IsDead);
        }

        private void MarkLost(SkirmishRun04E r, SkirmishActor04E a, string why)
        {
            if (a.Dead || a.Lost) return;
            a.Lost = true;
            r.Ledger.MarkLost();
            Detach(a);
            Note("#" + r.Serial + " the " + a.Species + " " + why + " | " + r.Ledger);
        }

        /// <summary>A confirmed death (health reached zero) counts exactly once, whoever caused it.</summary>
        private void CountDeath(SkirmishRun04E r, SkirmishActor04E a)
        {
            if (a.Dead || a.Lost) return;
            a.Dead = true;
            Detach(a);
            if (r.Phase > 1) return;
            r.Ledger.MarkDead();
            r.Arena.Members.Remove(a.Body);
            if (Game.Hud != null && NearPlayer(r, 30f))
                Game.Hud.CombatText(r.Ledger.Dead + " of " + r.Ledger.DeclaredTotal + " down", EncounterColor);
        }

        private static void Detach(SkirmishActor04E a)
        {
            if (a.Health != null && a.OnDied != null) a.Health.Died -= a.OnDied;
            a.OnDied = null;
        }

        // ================================================================== reinforcements

        private void TickReinforcement(SkirmishRun04E r, float dt, float active)
        {
            SkirmishLedger04E L = r.Ledger;
            if (!r.ReinforceScheduled && r.Engaged && Time.time >= r.NextReinforceAllowed
                && SkirmishRules04E.ReinforcementDue(L.RosterTotal, L.RosterSpawned, L.Dead, L.Alive, L.ReinforcementsOutstanding) && L.CanReserveReinforcement)
            {
                int token = L.ReserveReinforcement();
                if (token > 0)
                {
                    string species = SkirmishRules04E.ReinforcementSpecies(r.Roster, r.ReinforceIndex);
                    Vector3 point;
                    if (!ReinforcePoint(r, out point))
                    {
                        L.Withdraw(token);
                        Note("#" + r.Serial + " a reinforcement had no safe arrival point and was withdrawn | " + L);
                        if (Game.Hud != null && NearPlayer(r, 40f)) Game.Hud.Notify("The reinforcement could not get through.", 3f);
                    }
                    else
                    {
                        r.ReinforceScheduled = true;
                        r.ReinforceToken = token;
                        r.ReinforceIn = SkirmishRules04E.ReinforceDelay;
                        r.ReinforcePoint = point;
                        r.ReinforceRing.Show(point, 2.4f, SkirmishRules04E.ReinforceDelay, ReinforceColor);
                        CreatureSpecies sp = CreatureSpeciesDb.Get(species);
                        Note("#" + r.Serial + " reinforcement reserved (token " + token + ", " + species + ") arriving in " + SkirmishRules04E.ReinforceDelay.ToString("0") + " s | " + L);
                        if (Game.Hud != null && NearPlayer(r, 40f))
                            Game.Hud.Banner("REINFORCEMENT", (sp != null ? "A " + sp.Name : "A foe") + " is coming (the glowing ring). " + L.ReinforcementsOutstanding + " more possible.", ReinforceColor, 3f);
                        Underheart04ESounds.Play(UhSfx.BossTelegraph, point, 0.6f, 1.3f);
                    }
                }
            }
            if (!r.ReinforceScheduled) return;
            r.ReinforceIn -= active;
            if (r.ReinforceIn > 0f) return;
            r.ReinforceScheduled = false;
            r.NextReinforceAllowed = Time.time + 6f;
            ReleaseRing(r, true);
            if (!L.Admit(r.ReinforceToken))
            {
                Note("#" + r.Serial + " reinforcement token " + r.ReinforceToken + " was already used or cancelled: nothing spawns");
                return;
            }
            string kind = SkirmishRules04E.ReinforcementSpecies(r.Roster, r.ReinforceIndex);
            SkirmishActor04E a = SpawnActor(r, kind, false, r.ReinforcePoint, r.Roster.Count + r.ReinforceIndex, true);
            r.ReinforceIndex++;
            if (a == null)
            {
                L.AdmitFailed(r.ReinforceToken);
                Note("#" + r.Serial + " the reinforcement body could not be created (withdrawn) | " + L);
                if (Game.Hud != null && NearPlayer(r, 40f)) Game.Hud.Notify("The reinforcement could not get through.", 3f);
                return;
            }
            if (Game.Fx != null) Game.Fx.CrystalBurst(r.ReinforcePoint + Vector3.up, ReinforceColor, 16, 1.2f);
            Note("#" + r.Serial + " the reinforcement arrived | " + L);
        }

        /// <summary>A validated arrival point: the free post farthest from the player (never within 6 m of them).</summary>
        private static bool ReinforcePoint(SkirmishRun04E r, out Vector3 point)
        {
            point = r.Arena.Center;
            Vector3 pp = Game.PlayerPosition;
            float best = -1f;
            bool found = false;
            for (int i = 0; i < r.Posts.Count; i++)
            {
                Vector3 p = PlacePost(r, r.Posts[i]);
                float d = MathUtil.FlatDistance(p, pp);
                if (d < 6f || d <= best) continue;
                best = d;
                point = p;
                found = true;
            }
            if (!found)
            {
                Vector3 c = PlacePost(r, r.Arena.Center);
                if (MathUtil.FlatDistance(c, pp) >= 6f)
                {
                    point = c;
                    found = true;
                }
            }
            return found;
        }

        private static void ReleaseRing(SkirmishRun04E r, bool burst)
        {
            if (burst) r.ReinforceRing.Burst();
            else r.ReinforceRing.Release();
        }

        private static void UpdateHandle(SkirmishRun04E r)
        {
            SkirmishLedger04E L = r.Ledger;
            r.Handle.Alive = L.Alive;
            r.Handle.Total = L.DeclaredTotal;
            r.Handle.Defeated = L.Dead;
            if (!r.Engaged) r.Handle.Status = "Enter the room: " + L.DeclaredTotal + " foes.";
            else r.Handle.Status = L.Dead + " of " + L.DeclaredTotal + " down"
                + (r.ReinforceScheduled ? ", reinforcement in " + Mathf.CeilToInt(Mathf.Max(0f, r.ReinforceIn)) + " s"
                : (L.ReinforcementsOutstanding > 0 ? ", " + L.ReinforcementsOutstanding + " reinforcement" + (L.ReinforcementsOutstanding == 1 ? "" : "s") + " possible" : "")) + ".";
        }

        // ================================================================== victory and abort

        /// <summary>
        /// A real victory: every spawned foe confirmed dead. Reservations are released, owned threats stop, the admission
        /// lease becomes the quiet aftermath, the room is marked cleared for this run, and the handle reports Done.
        /// </summary>
        private void Victory(SkirmishRun04E r)
        {
            if (r.Phase > 1) return;
            r.Phase = 3;
            r.End = SkirmishEnd04E.Victory;
            int cancelled = r.Ledger.CancelAll();
            r.Arena.Ending = true;
            StopOwnedThreats(r);
            bool released = LocalQuiet.Release(r.LeaseOwner);
            bool fresh = LocalQuiet.AcquireAftermath(r.AftermathOwner, r.Arena.Center, r.Arena.Radius);
            string room = r.Spec.RoomId ?? "";
            if (!string.IsNullOrEmpty(room) && Game.Underheart != null)
            {
                try { Game.Underheart.MarkCleared(room); }
                catch (Exception e) { Log.Exception("Underheart MarkCleared", e); }
            }
            for (int i = 0; i < r.Actors.Count; i++) Detach(r.Actors[i]);
            r.Handle.Alive = 0;
            r.Handle.Total = r.Ledger.DeclaredTotal;
            r.Handle.Defeated = r.Ledger.Dead;
            r.Handle.Status = SkirmishRules04E.EndLine(SkirmishEnd04E.Victory) + ".";
            r.Handle.Failed = false;
            r.Handle.Done = true;
            Note("#" + r.Serial + " VICTORY: " + r.Ledger + "; " + cancelled + " reservation(s) released; admission lease " + (released ? "released" : "was already gone")
                + "; quiet aftermath " + (fresh ? "started" : "refreshed") + (string.IsNullOrEmpty(room) ? "" : "; room " + room + " marked cleared"));
            if (NearPlayer(r, 60f))
            {
                if (Game.Hud != null) Game.Hud.Banner("ROOM CLEARED", "Every foe confirmed down. The area stays quiet for a while.", EncounterColor, 4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionPassed, 0.45f, 1.1f);
            }
        }

        /// <summary>
        /// Ends an encounter without a victory: reservations are released, owned threats stop, the foes withdraw to their
        /// posts and are removed off camera (never counted as kills); the admission lease goes when the last one is gone.
        /// </summary>
        private void AbortRun(SkirmishRun04E r, SkirmishEnd04E why, bool notify)
        {
            if (r.Phase > 1) return;
            r.Phase = 2;
            r.End = why;
            r.WithdrawT = 0f;
            int cancelled = r.Ledger.CancelAll();
            r.Arena.Ending = true;
            StopOwnedThreats(r);
            for (int i = 0; i < r.Actors.Count; i++)
            {
                SkirmishActor04E a = r.Actors[i];
                if (a.Dead || a.Lost || a.Body == null || a.Body.IsDead) continue;
                UnderheartRoleAttach04E.Withdraw(a.Body);
            }
            r.Handle.Done = false;
            r.Handle.Failed = true;
            r.Handle.Alive = r.Ledger.Alive;
            r.Handle.Status = SkirmishRules04E.EndLine(why) + ". Nothing was counted as a victory.";
            Note("#" + r.Serial + " ABORTED: " + SkirmishRules04E.EndLine(why) + "; " + cancelled + " reservation(s) released, no victory | " + r.Ledger);
            if (notify && Game.Hud != null && why != SkirmishEnd04E.SessionReset && why != SkirmishEnd04E.AbortedByOwner)
                Game.Hud.Notify("The fight underground ended: " + SkirmishRules04E.EndLine(why).ToLowerInvariant() + ". Nothing was counted.", 4f);
        }

        private void TickEnding(SkirmishRun04E r, float dt)
        {
            r.WithdrawT += dt;
            bool pending = false;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                SkirmishActor04E a = r.Actors[i];
                if (a.Dead || a.Lost) continue;
                if (a.Body == null || a.Body.IsDead)
                {
                    a.Lost = a.Body == null;
                    a.Dead = a.Body != null;
                    Detach(a);
                    continue;
                }
                bool seen = SeenByCamera(a.Body.ChestPosition);
                if (!seen || r.WithdrawT >= SkirmishRules04E.WithdrawSeconds)
                {
                    RemoveActor(a, seen);
                    continue;
                }
                pending = true;
            }
            if (pending && r.WithdrawT < SkirmishRules04E.WithdrawSeconds + 2f) return;
            FinishAbort(r);
        }

        private void FinishAbort(SkirmishRun04E r)
        {
            for (int i = 0; i < r.Actors.Count; i++)
            {
                SkirmishActor04E a = r.Actors[i];
                if (!a.Dead && !a.Lost) RemoveActor(a, false);
                Detach(a);
            }
            bool released = LocalQuiet.Release(r.LeaseOwner);
            ReleaseRing(r, false);
            r.Phase = 3;
            Note("#" + r.Serial + " every owned foe is gone; admission lease " + (released ? "released" : "was already gone"));
        }

        /// <summary>Ends a run right now from any phase (reset, shutdown, a fault): no victory, every body removed.</summary>
        private void EndRunNow(SkirmishRun04E r, SkirmishEnd04E why)
        {
            if (r.Phase <= 1) AbortRun(r, why, false);
            if (r.Phase == 2) FinishAbort(r);
            if (r.Phase < 3)
            {
                LocalQuiet.Release(r.LeaseOwner);
                r.Phase = 3;
            }
        }

        /// <summary>Removes a body now (no death, no rewards, never a defeat); a puff when the camera can see it.</summary>
        private static void RemoveActor(SkirmishActor04E a, bool puff)
        {
            if (a.Dead || a.Lost) return;
            a.Lost = true;
            Detach(a);
            CharacterBase b = a.Body;
            if (b == null) return;
            UnderheartRoleAttach04E.StopThreats(b);
            Creature c = b as Creature;
            if (c != null)
            {
                c.Despawn(puff);
                return;
            }
            if (puff && Game.Fx != null && b.Health != null) Game.Fx.RockBurst(b.ChestPosition, b.Health.SurfaceTint, 8, 0.8f);
            Destroy(b.gameObject);
        }

        /// <summary>Stops every owned threat: role signatures, beams, scans, scheduled slag and the arrival ring.</summary>
        private static void StopOwnedThreats(SkirmishRun04E r)
        {
            ReleaseRing(r, false);
            r.ReinforceScheduled = false;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                SkirmishActor04E a = r.Actors[i];
                if (a.Body != null) UnderheartRoleAttach04E.StopThreats(a.Body);
            }
        }

        private static bool SeenByCamera(Vector3 p)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(p);
            if (v.z <= 0f || v.x < -0.15f || v.x > 1.15f || v.y < -0.15f || v.y > 1.15f) return false;
            return !Physics.Linecast(cam.transform.position, p, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }
    }
}
