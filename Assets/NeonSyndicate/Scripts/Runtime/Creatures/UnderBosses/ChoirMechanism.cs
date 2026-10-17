using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One mechanism of Break the Extraction Choir (shared base of the valve defense, the reflector beam and the
    /// maintenance escort). It builds its props at an anchor (a frame turned by the anchor's yaw, every prop on a probed
    /// floor), holds the area with a local admission lease while it runs, brings only its declared waves (announced),
    /// and finishes Done (props stay in their solved state for a while, then tidy away) or Failed (death, abort, load,
    /// title: everything it owns goes). A loss inside the mechanism (a seized valve, a broken construct) resets the
    /// mechanism itself, never the quest.
    /// </summary>
    public abstract class ChoirMechanism : MonoBehaviour
    {
        public const float AftermathSeconds = 180f;
        /// <summary>The fight area attackers are bound to (inside the 32 m admission lease; waves arrive about 12 m out).</summary>
        public const float ArenaRadius = 28f;

        public string Id { get; private set; } = "";
        public MechanismHandle Handle { get; private set; }
        public Vector3 Anchor { get; private set; }
        public float Yaw { get; private set; }
        public bool Running { get; private set; }
        public bool Finished { get; private set; }

        protected UnderheartBosses Owner;
        protected UBossAddSquad Squad;
        private int _attempt;
        private float _doneAt;
        private bool _cleaned;
        private float _hitAt = -9f;
        private readonly List<int> _loops = new List<int>();

        protected static DifficultyLevel Diff { get { return Settings.Difficulty; } }
        protected string LeaseOwner { get { return "uboss04e:mech:" + Id; } }

        public bool Setup(UnderheartBosses owner, string id, MechanismHandle handle, Vector3 anchor, float yaw, out string reason)
        {
            reason = "";
            Owner = owner;
            Id = id;
            Handle = handle;
            Anchor = anchor;
            Yaw = yaw;
            transform.SetPositionAndRotation(anchor, Quaternion.Euler(0f, yaw, 0f));
            NewSquad();
            try
            {
                if (!Build(out reason)) return false;
            }
            catch (Exception e)
            {
                Log.Exception("Choir mechanism build " + id, e);
                reason = e.Message;
                return false;
            }
            Running = true;
            LocalQuiet.AcquireAdmission(LeaseOwner, anchor, 32f);
            Log.Info("UBOSS04E mechanism " + id + " ready at " + anchor.ToString("F1") + " (declared attackers per attempt " + ChoirRules04E.DeclaredAttackers(id) + ")");
            return true;
        }

        /// <summary>A fresh declared wave budget for a new attempt (the old attackers are dismissed first).</summary>
        protected void NewSquad()
        {
            if (Squad != null) Squad.Dismiss(true);
            _attempt++;
            int declared = ChoirRules04E.DeclaredAttackers(Id);
            Squad = new UBossAddSquad("uboss04e_mech_" + Id + "_" + _attempt, new AddLedger04E(declared, declared, ChoirRules04E.WaveCount(Id)));
            Squad.UseArena(Anchor, ArenaRadius);
        }

        protected abstract bool Build(out string reason);
        protected abstract void Tick(float dt, float now);

        /// <summary>0 to 1 for the HUD bar.</summary>
        public abstract float Progress01 { get; }

        /// <summary>A short status line under the bar ("" for none).</summary>
        public virtual string ExtraLine { get { return ""; } }

        public virtual void Targets(List<Vector3> into, List<string> labels)
        {
            into.Clear();
            labels.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float now = Time.time;
            if (!Running)
            {
                if (Finished && (now - _doneAt > AftermathSeconds || MathUtil.FlatDistance(Game.PlayerPosition, Anchor) > 120f)) Dispose();
                else if (Finished)
                {
                    try { Aftermath(dt, now); }
                    catch (Exception e) { Log.Exception("Choir mechanism aftermath " + Id, e); }
                }
                return;
            }
            if (Game.State == GameState.Title)
            {
                End(false, "The mechanism stopped when the game returned to the title.");
                return;
            }
            if (Game.State == GameState.Loading)
            {
                End(false, "The mechanism stopped while a save loaded.");
                return;
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted)
            {
                End(false, "You fell. " + ChoirRules04E.Title(Id) + " resets: return when you are ready.");
                return;
            }
            Squad.Tick();
            try { Tick(dt, now); }
            catch (Exception e)
            {
                Log.Exception("Choir mechanism " + Id, e);
                End(false, ChoirRules04E.Title(Id) + " stopped after an error. Send the Console error along.");
            }
        }

        /// <summary>After a success: props keep their solved look (a lit receptor, an open valve).</summary>
        protected virtual void Aftermath(float dt, float now) { }

        /// <summary>
        /// Ends the mechanism. done: Handle.Done, a quiet aftermath lease, props stay a while. Otherwise Handle.Failed with
        /// the reason, every attacker and prop goes now.
        /// </summary>
        public void End(bool done, string objective)
        {
            if (!Running) return;
            Running = false;
            Finished = true;
            _doneAt = Time.time;
            StopLoops();
            int dismissed = Squad != null ? Squad.Dismiss(done || (Game.State != GameState.Title && Game.State != GameState.Loading)) : 0;
            if (Squad != null) Squad.Ledger.Close();
            LocalQuiet.Release(LeaseOwner);
            if (Handle != null)
            {
                Handle.Objective = objective ?? "";
                if (done) Handle.Done = true;
                else Handle.Failed = true;
            }
            if (done)
            {
                LocalQuiet.AcquireAftermath(LeaseOwner + ":aftermath", Anchor, 40f);
                Underheart04ESounds.Play(UhSfx.BossDefeat, Anchor + Vector3.up * 2f, 0.8f, 1.2f);
                if (Game.Hud != null) Game.Hud.Banner(ChoirRules04E.Title(Id).ToUpper() + " DOWN", objective, new Color(1f, 0.85f, 0.4f), 4.5f);
            }
            Log.Info("UBOSS04E mechanism " + Id + (done ? " DONE" : " ended without success") + " (attempts " + _attempt + ", attackers dismissed " + dismissed + "): " + objective);
            try { OnEnded(done); }
            catch (Exception e) { Log.Exception("Choir mechanism end " + Id, e); }
            if (!done) Dispose();
        }

        protected virtual void OnEnded(bool done) { }

        public void Dispose()
        {
            if (Running)
            {
                Running = false;
                if (Handle != null && !Handle.Done)
                {
                    Handle.Failed = true;
                    Handle.Objective = "The mechanism was removed.";
                }
            }
            Finished = true;
            Cleanup();
            Destroy(gameObject);
        }

        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            StopLoops();
            if (Squad != null)
            {
                if (Running || (Handle != null && !Handle.Done)) Squad.Dismiss(false);
                Squad.Ledger.Close();
            }
            LocalQuiet.Release(LeaseOwner);
            try { OnCleanup(); }
            catch (Exception e) { Log.Exception("Choir mechanism cleanup " + Id, e); }
        }

        protected virtual void OnCleanup() { }

        private void OnDestroy()
        {
            if (Running && Handle != null && !Handle.Done)
            {
                Handle.Failed = true;
                Handle.Objective = "The mechanism was removed before it finished.";
            }
            Cleanup();
        }

        // ================================================================== helpers

        /// <summary>A point in the mechanism's frame (x right, z forward) on the floor near the anchor height.</summary>
        protected Vector3 Local(float x, float z)
        {
            Vector3 p = Anchor + Quaternion.Euler(0f, Yaw, 0f) * new Vector3(x, 0f, z);
            float y;
            p.y = UnderBossFx.Floor(new Vector3(p.x, Anchor.y, p.z), 2.2f, 5f, out y) && Mathf.Abs(y - Anchor.y) < 3f ? y : Anchor.y;
            return p;
        }

        /// <summary>Spawns a wave's declared roster ahead of where it should arrive (announced first). Returns how many came.</summary>
        protected int SpawnWave(int wave, Vector3 around, Vector3 from)
        {
            string[] roster = ChoirRules04E.WaveRoster(Id, wave);
            if (roster.Length == 0 || !Squad.Ledger.TryFireBatch(wave)) return 0;
            int n = 0;
            Vector3 away = MathUtil.Flat(around - Game.PlayerPosition);
            if (away.sqrMagnitude < 0.5f) away = MathUtil.DirFromYaw(Yaw);
            float baseYaw = MathUtil.YawOf(away);
            for (int i = 0; i < roster.Length; i++)
            {
                float a = baseYaw + (i - (roster.Length - 1) * 0.5f) * 40f;
                Vector3 want = around + MathUtil.DirFromYaw(a) * 12f;
                want.y = Anchor.y;
                Vector3 spot;
                if (!UBossAddSquad.Spot(want, from, Anchor.y, out spot)) continue;
                if (Squad.Spawn(roster[i], spot, MathUtil.YawOf(Game.PlayerPosition - spot), 1f, 1f) == null) continue;
                if (Game.Fx != null) Game.Fx.RockBurst(spot + Vector3.up, new Color(1f, 0.55f, 0.2f), 10, 1.2f);
                n++;
            }
            if (n > 0 && Game.Hud != null)
                Game.Hud.Banner("WAVE " + (wave + 1) + " OF " + ChoirRules04E.WaveCount(Id), n + " attackers incoming", new Color(1f, 0.6f, 0.3f), 3f);
            Log.Info("UBOSS04E mechanism " + Id + " wave " + (wave + 1) + ": " + n + " of " + roster.Length + " arrived (" + Squad.Ledger.Spawned + "/" + Squad.Ledger.Declared + " of this attempt's budget)");
            return n;
        }

        /// <summary>A mechanism hazard touching the player (the reflector beam): a small capped hit, at most once a second.</summary>
        protected void Sting(Vector3 from, float share)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Time.time - _hitAt < 1f) return;
            _hitAt = Time.time;
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                p.CurrentVehicle.ApplyDamage(10f * share, gameObject);
                return;
            }
            float raw = CreatureBrainLogic.CapHit(16f * share, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, p.ChestPosition - from, gameObject, DamageType.Power);
            info.Force = 1f;
            p.Health.TakeDamage(info);
        }

        protected int StartLoop(UhLoop loop, Vector3 pos, float volume)
        {
            int k = Underheart04ESounds.StartLoop(loop, pos, volume);
            if (k != 0) _loops.Add(k);
            return k;
        }

        private void StopLoops()
        {
            for (int i = 0; i < _loops.Count; i++) Underheart04ESounds.StopLoop(_loops[i]);
            _loops.Clear();
        }

        /// <summary>True when the player stands within r (flat) of p at about its height.</summary>
        protected static bool PlayerNear(Vector3 p, float r)
        {
            Vector3 pp = Game.PlayerPosition;
            return Mathf.Abs(pp.y - p.y) < 4f && MathUtil.FlatDistance(pp, p) <= r;
        }

        public int Attempt { get { return _attempt; } }
    }
}
