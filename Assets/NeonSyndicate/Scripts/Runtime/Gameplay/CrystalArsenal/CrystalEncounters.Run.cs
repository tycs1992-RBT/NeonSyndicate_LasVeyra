using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One open breach: its identity, place, budget, owned opponents, reservations, timers and leases.</summary>
    internal sealed class EncounterRun04D
    {
        public int Serial;
        public string Tag;
        public string LeaseOwner;
        public Vector3 Origin;
        public Vector3 Approach;
        public readonly List<Vector3> Anchors = new List<Vector3>();
        public EncounterBudget Budget;
        public readonly List<EncounterActor04D> Actors = new List<EncounterActor04D>();
        public readonly List<int> InitialTokens = new List<int>();
        public DifficultyLevel Difficulty;
        public bool InnerAtStart;
        public bool Engaged;
        public float EngagedSeconds;
        public float ApproachSeconds;
        public int InitialSpawned;
        public bool ReinforceScheduled;
        public bool ReinforceSpawned;
        public int ReinforceToken;
        public float ReinforceIn;
        public Vector3 ReinforcePoint;
        public GroundRing ReinforceRing;
        public float ReinforceRingAge;
        public float RetreatSeconds;
        public EncounterAbortReason AbortReason;
        public float LastOwnedHurtAt = -99f;
        /// <summary>An opponent vanished without dying: the run can no longer be won and ends at the next check.</summary>
        public bool ActorLost;
        public Blip Blip;
        public bool WaypointSet;
    }

    /// <summary>One owned opponent: its role, body, death and hurt hooks, and how it left (defeated or removed).</summary>
    internal sealed class EncounterActor04D
    {
        public EncounterRoleKind04D Role;
        public NpcController Npc;
        public CharacterHealth Health;
        public EncounterRole04D Overlay;
        public Action<DamageInfo> OnDied;
        public Action<DamageInfo> OnHurt;
        public Vector3 Anchor;
        public bool Defeated;
        public bool Removed;
        public bool ByPlayer;
        public float WithdrawSeconds;
    }

    public sealed partial class CrystalEncounters
    {
        private static readonly Color BreachColor = new Color(0.45f, 0.95f, 1f);
        private static readonly Color ReinforceColor = new Color(0.4f, 1f, 0.85f, 1f);

        // ================================================================== opening

        /// <summary>Preparing: reserves the opening wave, takes the area's admission lease and marks the way there.</summary>
        private bool BeginRun()
        {
            EncounterSite04D site = _site;
            if (site == null || !site.Valid || site.Anchors.Count < 5) return false;
            EncounterRun04D r = new EncounterRun04D();
            r.Serial = ++_serial;
            r.Tag = "enc04d_" + ResonanceBreach + "_" + r.Serial;
            r.LeaseOwner = LeasePrefix + ResonanceBreach + ":" + r.Serial;
            r.Origin = site.Center;
            r.Approach = site.Approach;
            r.Anchors.AddRange(site.Anchors);
            r.Difficulty = Settings.Difficulty;
            r.Budget = new EncounterBudget(EncounterRules04D.TotalOpponents, EncounterRules04D.AliveCap(r.Difficulty));
            r.InnerAtStart = Game.Inner != null && Game.Inner.IsInner;
            for (int i = 0; i < EncounterRules04D.InitialWave; i++)
            {
                int token = r.Budget.Reserve();
                if (token == 0) break;
                r.InitialTokens.Add(token);
            }
            if (r.InitialTokens.Count == 0) return false;
            _run = r;
            if (!SetState(EncounterState.Preparing, "started by the player"))
            {
                _run = null;
                return false;
            }
            bool fresh = LocalQuiet.AcquireAdmission(r.LeaseOwner, r.Origin, EncounterRules04D.AbortRadius);
            Note("admission lease " + (fresh ? "taken" : "refreshed") + ": " + r.LeaseOwner + " radius " + EncounterRules04D.AbortRadius.ToString("0")
                + " m at " + r.Origin + ", difficulty " + r.Difficulty + ", alive cap " + r.Budget.AliveCap);
            r.Blip = Blip.Add(BlipKind.Activity, null, r.Origin, BreachColor, "!", Title, true);
            if (r.Blip != null) r.Blip.Size = 1.25f;
            if (Game.Gps != null && !Game.Gps.HasMissionTarget)
            {
                Game.Gps.SetWaypoint(r.Origin);
                r.WaypointSet = true;
            }
            if (Game.Hud != null)
                Game.Hud.Banner("RESONANCE BREACH", "A crystal breach opened " + DistanceLine(r.Origin) + ". Four opponents, one reinforcement. Leave the area and it closes.", BreachColor, 5f);
            return true;
        }

        // ================================================================== frame

        private void TickRun(EncounterRun04D r, float dt, float active)
        {
            switch (_state)
            {
                case EncounterState.Preparing: TickPreparing(r); break;
                case EncounterState.Active: TickActive(r, dt, active); break;
                case EncounterState.Retreating: TickRetreat(r, dt); break;
                default:
                    // Victory and Cleared finish in one call; any other state here means the run is stale.
                    if (!EncounterRules04D.IsRunning(_state)) EndRunImmediately(EncounterAbortReason.Disabled, false);
                    break;
            }
        }

        /// <summary>One opening body per frame; a failed body aborts and cleans up whatever already exists.</summary>
        private void TickPreparing(EncounterRun04D r)
        {
            if (r.InitialSpawned >= r.InitialTokens.Count)
            {
                SetState(EncounterState.Active, "the opening wave is in place");
                return;
            }
            int index = r.InitialSpawned;
            int token = r.InitialTokens[index];
            if (!r.Budget.Admit(token))
            {
                Note("opening reservation " + token + " could not be admitted");
                AbortNow(r, EncounterAbortReason.SpawnFailed);
                return;
            }
            EncounterRoleKind04D role = EncounterRules04D.RoleAt(index);
            EncounterActor04D a = SpawnRole(r, role, r.Anchors[index]);
            if (a == null)
            {
                r.Budget.AdmitFailed(token);
                Note("the " + role + " body could not be created");
                AbortNow(r, EncounterAbortReason.SpawnFailed);
                return;
            }
            r.InitialSpawned++;
            if (r.InitialSpawned >= r.InitialTokens.Count) SetState(EncounterState.Active, "the opening wave is in place");
        }

        private void TickActive(EncounterRun04D r, float dt, float active)
        {
            PlayerController p = Game.Player;
            if (Bookkeep(r)) r.ActorLost = true;
            // Every authored opponent died: that is the victory, whatever else happened in the same frame.
            if (r.Budget.VictoryReady)
            {
                Victory(r, false);
                return;
            }
            // Paused, cutscenes and menus freeze the contract; death and arrest end it.
            bool watch = Game.State == GameState.Playing || Game.State == GameState.Wasted || Game.State == GameState.Busted;
            if (watch)
            {
                if (!r.Engaged) r.ApproachSeconds += active;
                else r.EngagedSeconds += active;
                EncounterRules04D.WatchFacts f = new EncounterRules04D.WatchFacts();
                f.PlayerDown = p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted;
                f.VeilChanged = (Game.Inner != null && Game.Inner.IsInner) != r.InnerAtStart;
                f.MissionActive = Game.Missions != null && Game.Missions.Active != null;
                f.OtherOwner = OtherCombatOwner();
                f.ActorLost = r.ActorLost;
                f.Engaged = r.Engaged;
                f.PlayerDistanceFromOrigin = p != null ? MathUtil.FlatDistance(p.transform.position, r.Origin) : 0f;
                f.ApproachSeconds = r.ApproachSeconds;
                EncounterAbortReason why = EncounterRules04D.Check(f);
                if (why != EncounterAbortReason.None)
                {
                    Abort(r, why, true);
                    return;
                }
                if (!r.Engaged && p != null && f.PlayerDistanceFromOrigin <= EncounterRules04D.EngageRadius)
                {
                    r.Engaged = true;
                    Note("the player arrived (" + f.PlayerDistanceFromOrigin.ToString("0") + " m from the origin)");
                    if (Game.Hud != null) Game.Hud.Notify("Resonance Breach: break the warden's guard after its slam, keep the refractor moving, and watch for the reinforcement.", 5f);
                }
            }
            if (r.Engaged) TickReinforcement(r, dt, active);
            if (_state == EncounterState.Active && r.Budget.VictoryReady) Victory(r, false);
        }

        /// <summary>Finds opponents that vanished without a death (never a kill) and deaths the event missed. True when one was lost.</summary>
        private bool Bookkeep(EncounterRun04D r)
        {
            bool lost = false;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Defeated || a.Removed) continue;
                if (a.Npc == null)
                {
                    a.Removed = true;
                    r.Budget.MarkRemoved();
                    Detach(a);
                    Note("the " + a.Role + " was removed without a death (not a defeat)");
                    lost = true;
                    continue;
                }
                if (a.Npc.IsDead) CountDeath(r, a, false);
            }
            return lost;
        }

        // ================================================================== the reinforcement

        private void TickReinforcement(EncounterRun04D r, float dt, float active)
        {
            if (!r.ReinforceScheduled && EncounterRules04D.ReinforcementDue(false, r.Engaged, r.Budget.Defeated, r.EngagedSeconds) && r.Budget.CanReserve)
            {
                // The reservation is taken before anything is scheduled; the arrival can only consume it once.
                int token = r.Budget.Reserve();
                if (token > 0)
                {
                    r.ReinforceScheduled = true;
                    r.ReinforceToken = token;
                    r.ReinforceIn = EncounterRules04D.ReinforceDelay;
                    r.ReinforcePoint = PickReinforcePoint(r);
                    r.ReinforceRing = GroundRing.Show(r.ReinforcePoint, 2.4f, EncounterRules04D.ReinforceDelay, ReinforceColor);
                    r.ReinforceRingAge = 0f;
                    Note("reinforcement reserved (token " + token + "), arriving in " + EncounterRules04D.ReinforceDelay.ToString("0") + " s at " + r.ReinforcePoint);
                    if (Game.Hud != null) Game.Hud.Banner("REINFORCEMENT", "A resonator is coming (the glowing ring). Its link shields an ally: break line of sight or take it down.", ReinforceColor, 4f);
                }
            }
            if (!r.ReinforceScheduled || r.ReinforceSpawned) return;
            r.ReinforceRingAge += dt;
            r.ReinforceIn -= active;
            if (r.ReinforceIn > 0f) return;
            r.ReinforceSpawned = true;
            ReleaseRing(r, true);
            if (!r.Budget.Admit(r.ReinforceToken))
            {
                // Never spawn twice; and a run whose authored total can no longer be met must end, not hang.
                Note("reinforcement token " + r.ReinforceToken + " was already used or cancelled: nothing spawns");
                Abort(r, EncounterAbortReason.SpawnFailed, true);
                return;
            }
            EncounterActor04D a = SpawnRole(r, EncounterRoleKind04D.Resonator, r.ReinforcePoint);
            if (a == null)
            {
                r.Budget.AdmitFailed(r.ReinforceToken);
                Note("the reinforcement body could not be created");
                Abort(r, EncounterAbortReason.SpawnFailed, true);
                return;
            }
            if (Game.Fx != null) Game.Fx.ShadowBurst(r.ReinforcePoint + Vector3.up, ReinforceColor, 16, 1f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, r.ReinforcePoint, 0.7f, 0.7f, 60f);
        }

        /// <summary>The reinforcement arrival point farther from the player (the anchors were validated with the site).</summary>
        private static Vector3 PickReinforcePoint(EncounterRun04D r)
        {
            Vector3 pp = Game.PlayerPosition;
            Vector3 a = r.Anchors[3];
            Vector3 b = r.Anchors[4];
            return MathUtil.FlatSqrDistance(a, pp) >= MathUtil.FlatSqrDistance(b, pp) ? a : b;
        }

        /// <summary>Releases or bursts the arrival marker only while it is surely still ours (rings are pooled).</summary>
        private static void ReleaseRing(EncounterRun04D r, bool burst)
        {
            GroundRing g = r.ReinforceRing;
            r.ReinforceRing = null;
            if (g == null || r.ReinforceRingAge > EncounterRules04D.ReinforceDelay + 1.5f) return;
            if (burst) g.Burst();
            else g.Release();
        }

        // ================================================================== bodies

        /// <summary>Spawns one role on its existing species with the encounter overlay; null when the body could not be made.</summary>
        private EncounterActor04D SpawnRole(EncounterRun04D r, EncounterRoleKind04D role, Vector3 anchor)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null) return null;
            CreatureSpawnOptions o = new CreatureSpawnOptions
            {
                Tag = r.Tag,
                Hostile = false,
                Persistent = true,
                HealthScale = EncounterRules04D.HealthScale(role, r.Difficulty),
                DamageScale = EncounterRules04D.DamageScale(r.Difficulty),
                DisplayName = EncounterRules04D.DisplayName(role),
                Seed = SeedFor(r.Serial, role)
            };
            string species = EncounterRules04D.SpeciesFor(role);
            float yaw = MathUtil.YawOf(r.Approach);
            CharacterBase body = cd.Spawn(species, anchor, yaw, o);
            NpcController npc = body as NpcController;
            if (npc == null || npc.Health == null)
            {
                if (body != null) Destroy(body.gameObject);
                return null;
            }
            // The overlay owns perception and hearing; the species' own temperament and pack alerts stand aside.
            npc.IgnoreThreats = true;
            npc.CreatureProvoked = true;
            CreatureAbilityKit kit = npc.CreatureKit;
            if (kit != null)
            {
                kit.ScriptedOnly = true;
                if (role == EncounterRoleKind04D.Refractor) kit.HasGuard = false;
                if (role == EncounterRoleKind04D.Warden) kit.HasGuard = r.Difficulty != DifficultyLevel.Story;
            }
            if (role == EncounterRoleKind04D.Resonator && npc.Anim != null) npc.Anim.EliteGlow = 0.7f;
            EncounterActor04D a = new EncounterActor04D { Role = role, Npc = npc, Health = npc.Health, Anchor = npc.transform.position };
            a.OnDied = info => OnActorDied(r, a, info);
            a.OnHurt = info => OnActorHurt(r, info);
            npc.Health.Died += a.OnDied;
            npc.Health.Damaged += a.OnHurt;
            EncounterRole04D overlay = npc.gameObject.AddComponent<EncounterRole04D>();
            overlay.Init(npc, role, a.Anchor, r.Origin, r.Serial, r.Difficulty);
            // The reinforcement arrives into a fight its allies called it to: it searches where the player was, then
            // still has to see them before it commits.
            if (role == EncounterRoleKind04D.Resonator && Game.Player != null) overlay.AlertFrom(Game.PlayerPosition);
            a.Overlay = overlay;
            r.Actors.Add(a);
            Note("spawned the " + role + " (" + species + ") at " + a.Anchor + ", health " + npc.Health.MaxHealth.ToString("0")
                + " (scale " + o.HealthScale.ToString("0.00") + "), damage scale " + o.DamageScale.ToString("0.00"));
            return a;
        }

        private static int SeedFor(int serial, EncounterRoleKind04D role)
        {
            int h = 1913;
            for (int i = 0; i < ResonanceBreach.Length; i++) h = h * 31 + ResonanceBreach[i];
            h = h * 31 + (int)role * 7919 + serial * 104729;
            return (h & 0x7fffffff) | 1;
        }

        private void OnActorDied(EncounterRun04D r, EncounterActor04D a, DamageInfo info)
        {
            if (_run != r) return;
            CountDeath(r, a, info.FromPlayer || (info.Source != null && Game.Player != null && Game.Player.InVehicle && Game.Player.CurrentVehicle != null && info.Source == Game.Player.CurrentVehicle.gameObject));
        }

        private void OnActorHurt(EncounterRun04D r, DamageInfo info)
        {
            if (_run == r && info.FromPlayer) r.LastOwnedHurtAt = Time.time;
        }

        /// <summary>A death (health reached zero) counts exactly once, whoever caused it; only deaths count toward victory.</summary>
        private void CountDeath(EncounterRun04D r, EncounterActor04D a, bool byPlayer)
        {
            if (a.Defeated || a.Removed) return;
            a.Defeated = true;
            a.ByPlayer = byPlayer;
            r.Budget.MarkDefeated();
            Note("the " + a.Role + " fell" + (byPlayer ? " to the player" : " (not by the player)"));
            if (_state == EncounterState.Active && Game.Hud != null)
                Game.Hud.CombatText(EncounterRules04D.DisplayName(a.Role) + " down (" + r.Budget.Defeated + " of " + r.Budget.Total + ")", BreachColor);
        }

        private static void Detach(EncounterActor04D a)
        {
            if (a.Health != null)
            {
                if (a.OnDied != null) a.Health.Died -= a.OnDied;
                if (a.OnHurt != null) a.Health.Damaged -= a.OnHurt;
            }
            a.OnDied = null;
            a.OnHurt = null;
        }

        /// <summary>Removes a body now (no death, no rewards, never a defeat); an optional puff when the camera can see it.</summary>
        private void RemoveActor(EncounterRun04D r, EncounterActor04D a, bool puff)
        {
            if (a.Defeated || a.Removed) return;
            a.Removed = true;
            r.Budget.MarkRemoved();
            Detach(a);
            NpcController n = a.Npc;
            if (n == null) return;
            if (a.Overlay != null) a.Overlay.StopPatterns();
            if (puff && Game.Fx != null && !n.IsDead && n.Health != null)
            {
                if (n.Health.Surface == ImpactSurface.Shadow) Game.Fx.ShadowBurst(n.ChestPosition, n.Health.SurfaceTint, 10, 1f);
                else if (n.Health.Surface == ImpactSurface.Glass) Game.Fx.GlassBurst(n.ChestPosition, n.Health.SurfaceTint, 14);
                else Game.Fx.RockBurst(n.ChestPosition, n.Health.SurfaceTint, 8, 0.8f);
            }
            Destroy(n.gameObject);
        }

        /// <summary>True when the camera could see a point (inside the view and not behind a wall).</summary>
        private static bool SeenByCamera(Vector3 p)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(p);
            if (v.z <= 0f || v.x < -0.15f || v.x > 1.15f || v.y < -0.15f || v.y > 1.15f) return false;
            return !Physics.Linecast(cam.transform.position, p, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Stops every owned threat: wind-ups, links, aim lines, in-flight shards and the arrival marker.</summary>
        private void StopOwnedThreats(EncounterRun04D r)
        {
            ReleaseRing(r, false);
            int shards = EncounterShard04D.CancelOwner(r.Serial);
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Overlay != null) a.Overlay.StopPatterns();
            }
            if (shards > 0) Note(shards + " owned shard(s) in flight cancelled");
        }

        private void ClearMarkers(EncounterRun04D r)
        {
            if (r.Blip != null) Blip.Remove(r.Blip);
            r.Blip = null;
            if (r.WaypointSet && Game.Gps != null && Game.Gps.HasWaypoint && MathUtil.FlatDistance(Game.Gps.Waypoint, r.Origin) < 1f) Game.Gps.ClearWaypoint();
            r.WaypointSet = false;
        }

        // ================================================================== victory

        /// <summary>
        /// A real victory: admissions stop, owned threats are cancelled, the reward is granted once through the arsenal,
        /// the admission lease becomes the quiet aftermath, and the contract is Cleared. forced: the debug button (it kills
        /// the remaining opponents for real first and is logged as forced).
        /// </summary>
        private void Victory(EncounterRun04D r, bool forced)
        {
            if (_run != r || _state != EncounterState.Active) return;
            if (forced) ForceKillRemaining(r);
            if (!SetState(EncounterState.Victory, forced ? "forced from the debug menu" : "every opponent defeated")) return;
            int cancelled = r.Budget.CancelAll();
            if (cancelled > 0) Note(cancelled + " pending reservation(s) released at victory");
            StopOwnedThreats(r);
            string reward = GrantReward();
            bool released = LocalQuiet.Release(r.LeaseOwner);
            bool fresh = LocalQuiet.AcquireAftermath(AftermathOwner, r.Origin, LocalQuiet.QuietRadius);
            Note("admission lease " + (released ? "released" : "was already gone") + "; quiet aftermath " + (fresh ? "started" : "refreshed")
                + ": radius " + LocalQuiet.QuietRadius.ToString("0") + " m, " + LocalQuiet.QuietSeconds.ToString("0") + " s of play, then leave "
                + (LocalQuiet.QuietRadius + LocalQuiet.DepartMargin).ToString("0") + " m; reward: " + reward);
            ClearMarkers(r);
            for (int i = 0; i < r.Actors.Count; i++) Detach(r.Actors[i]);
            _lastResult = reward;
            if (Game.Hud != null) Game.Hud.Banner("BREACH SEALED", reward + ". The area stays quiet for a while.", BreachColor, 5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionPassed, 0.5f, 1.15f);
            _run = null;
            SetState(EncounterState.Cleared, "reward handled and the quiet aftermath set");
        }

        /// <summary>The first victory grants the Gyre Crescent entitlement (the arsenal keeps it pending when the backpack is full).</summary>
        private string GrantReward()
        {
            CrystalArsenal ars = Game.Arsenal;
            if (ars == null)
            {
                Log.Warn("ENC04D reward " + RewardToken + " could not be recorded: no arsenal system");
                return "The reward could not be recorded (the arsenal is unavailable)";
            }
            bool had = false;
            bool granted = false;
            try
            {
                had = ars.HasEntitlement(RewardToken);
                granted = ars.GrantReward(RewardToken, CrystalFocusDb.GyreCrescentUnique, RewardSource);
            }
            catch (Exception e) { Log.Exception("Encounter reward", e); }
            bool now = false;
            try { now = ars.HasEntitlement(RewardToken); }
            catch (Exception e) { Log.Exception("Encounter entitlement query", e); }
            Note("reward " + RewardToken + " unique " + CrystalFocusDb.GyreCrescentUnique + ": granted " + granted + ", held before " + had + ", held now " + now);
            if (granted) return "Gyre Crescent earned (see ARSENAL)";
            if (had) return "Replay complete, no new reward";
            return "The reward could not be recorded";
        }

        private void ForceKillRemaining(EncounterRun04D r)
        {
            GameObject src = Game.Player != null ? Game.Player.gameObject : null;
            r.Budget.CancelAll();
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Defeated || a.Removed || a.Npc == null || a.Npc.IsDead) continue;
                IDamageFilter filter = a.Npc.Health.Filter;
                a.Npc.Health.Filter = null;
                a.Npc.Health.Kill(src, DamageType.Explosion);
                if (a.Npc != null && a.Npc.Health != null) a.Npc.Health.Filter = filter;
                CountDeath(r, a, true);
            }
        }

        // ================================================================== abort

        /// <summary>
        /// An encounter ends without a reward: reservations are released, owned threats stop, the opponents withdraw and are
        /// removed off camera (never counted as kills). The admission lease is released when the last one is gone.
        /// </summary>
        private void Abort(EncounterRun04D r, EncounterAbortReason reason, bool notify)
        {
            if (_run != r || r.AbortReason != EncounterAbortReason.None) return;
            r.AbortReason = reason;
            if (!SetState(EncounterState.Retreating, EncounterRules04D.AbortLine(reason))) return;
            int cancelled = r.Budget.CancelAll();
            StopOwnedThreats(r);
            ClearMarkers(r);
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Defeated || a.Removed || a.Npc == null || a.Npc.IsDead || a.Overlay == null) continue;
                Vector3 away = MathUtil.Flat(a.Npc.transform.position - pp);
                if (away.sqrMagnitude < 0.01f) away = MathUtil.Flat(a.Npc.transform.position - r.Origin);
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                a.Overlay.Withdraw(a.Npc.transform.position + away.normalized * 25f);
            }
            Note("aborted: " + EncounterRules04D.AbortLine(reason) + "; " + cancelled + " reservation(s) released, no reward");
            if (notify && Game.Hud != null && reason != EncounterAbortReason.SessionReset)
                Game.Hud.Notify("Resonance Breach failed: " + EncounterRules04D.AbortLine(reason) + ". No reward. You can try again in " + EncounterRules04D.RetrySeconds.ToString("0") + " s.", 5f);
        }

        /// <summary>Aborts and removes every owned body at once (a failed opening, a reset, a shutdown).</summary>
        private void AbortNow(EncounterRun04D r, EncounterAbortReason reason)
        {
            Abort(r, reason, true);
            if (_run != r) return;
            for (int i = 0; i < r.Actors.Count; i++) RemoveActor(r, r.Actors[i], false);
            FinishAbort(r);
        }

        private void TickRetreat(EncounterRun04D r, float dt)
        {
            bool pending = false;
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < r.Actors.Count; i++)
            {
                EncounterActor04D a = r.Actors[i];
                if (a.Defeated || a.Removed) continue;
                if (a.Npc == null)
                {
                    a.Removed = true;
                    r.Budget.MarkRemoved();
                    Detach(a);
                    continue;
                }
                if (a.Npc.IsDead)
                {
                    CountDeath(r, a, false);
                    continue;
                }
                a.WithdrawSeconds += dt;
                bool seen = SeenByCamera(a.Npc.ChestPosition);
                bool far = MathUtil.FlatDistance(a.Npc.transform.position, pp) > 160f;
                if (!seen || far || a.WithdrawSeconds >= EncounterRules04D.WithdrawLimit)
                {
                    RemoveActor(r, a, seen && !far);
                    continue;
                }
                pending = true;
            }
            r.RetreatSeconds += dt;
            if (!pending || r.RetreatSeconds > EncounterRules04D.WithdrawLimit + 5f)
            {
                for (int i = 0; i < r.Actors.Count; i++) RemoveActor(r, r.Actors[i], true);
                FinishAbort(r);
            }
        }

        /// <summary>The last owned body is gone: the admission lease is released (exactly once) and the retry clock starts.</summary>
        private void FinishAbort(EncounterRun04D r)
        {
            if (_run != r) return;
            bool released = LocalQuiet.Release(r.LeaseOwner);
            for (int i = 0; i < r.Actors.Count; i++) Detach(r.Actors[i]);
            _lastAbort = r.AbortReason;
            _lastResult = "";
            Note("every owned opponent is gone; admission lease " + (released ? "released" : "was already gone"));
            _run = null;
            SetState(EncounterState.Aborted, EncounterRules04D.AbortLine(r.AbortReason));
            _retryWait = EncounterRules04D.RetrySeconds;
        }

        /// <summary>Ends any open run right now, without a reward, from whatever state it is in.</summary>
        private void EndRunImmediately(EncounterAbortReason reason, bool notify)
        {
            EncounterRun04D r = _run;
            if (r == null) return;
            if (_state == EncounterState.Preparing || _state == EncounterState.Active) Abort(r, reason, notify);
            if (_run != r) return;
            if (_state != EncounterState.Retreating)
            {
                // Victory or a stale state: drop the run without inventing a transition.
                StopOwnedThreats(r);
                ClearMarkers(r);
                for (int i = 0; i < r.Actors.Count; i++) RemoveActor(r, r.Actors[i], false);
                LocalQuiet.Release(r.LeaseOwner);
                _run = null;
                return;
            }
            for (int i = 0; i < r.Actors.Count; i++) RemoveActor(r, r.Actors[i], false);
            FinishAbort(r);
        }

        // ================================================================== debug hooks

        /// <summary>Debug: declares victory now (the remaining opponents die for real first). Logged as forced.</summary>
        public bool DebugForceVictory()
        {
            EncounterRun04D r = _run;
            if (r == null || _state != EncounterState.Active) return false;
            Victory(r, true);
            return true;
        }

        /// <summary>Debug: aborts the open breach (no reward).</summary>
        public bool DebugForceAbort()
        {
            EncounterRun04D r = _run;
            if (r == null) return false;
            if (_state == EncounterState.Preparing) AbortNow(r, EncounterAbortReason.DebugForced);
            else if (_state == EncounterState.Active) Abort(r, EncounterAbortReason.DebugForced, true);
            return true;
        }
    }
}
