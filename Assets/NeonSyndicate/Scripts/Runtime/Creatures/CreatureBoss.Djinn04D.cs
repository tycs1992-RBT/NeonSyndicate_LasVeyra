using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The upgraded Mirage Djinn, the fight that teaches the Mirage Choir. Its signature is the shard choir: a fan of
    /// glowing shards gathers round the real body (the real casting tell) while harmless decoy pillars shimmer elsewhere,
    /// then the shards leave one by one and steer toward the player at a limited turn rate, only for a short window and
    /// only while they can see the player (losing sight once ends the steering for good; they always collide with the
    /// world). Crossing the volley's lane or breaking sight beats it; afterwards the Djinn is spent for a moment. It
    /// shifts to a new legal lane every few moves and at two thirds health. At one third its one bounded illusion phase
    /// raises at most two mirages (no shadow, they pop at a touch, they never strike); only the registered real body can
    /// be defeated.
    /// </summary>
    public sealed partial class CreatureBoss
    {
        private sealed class Shard04D
        {
            public GameObject Go;
            public Vector3 Pos;
            public Vector3 Dir;
            public Vector3 Offset;
            public float Age;
            public float Life;
            public float LaunchAt;
            public float Trail;
            public bool Live;
            public bool Held;
            public bool Lost;
            public int Volley;
        }

        private const int MaxShards04D = 12;
        private static readonly Color ChoirColor04D = new Color(1f, 0.78f, 0.36f);
        private static readonly Color DecoyColor04D = new Color(1f, 0.7f, 0.35f);

        private readonly List<Shard04D> _shards = new List<Shard04D>();
        private readonly List<Beam04DLive> _decoys = new List<Beam04DLive>();
        private readonly int[] _volleyHits04D = new int[4];
        private int _volley04D;
        private float _choirTurn;
        private float _choirSteer;
        private float _choirLaunchEnd;
        private float _decoyPuff;
        private Vector3 _relocTo;
        private float _illusionUntil;
        private bool _illusionUsed;

        /// <summary>The Djinn's one illusion phase is running.</summary>
        private bool IllusionActive04D { get { return _illusionUntil > Time.time; } }

        private void EnterPhaseDjinn04D(int phase)
        {
            if (phase == 1)
            {
                Callout("The Djinn takes a new lane");
                _sinceRelocate = 3;
            }
            if (phase >= 2) StartIllusion04D();
        }

        // ================================================================== the shard choir

        private bool BeginChoir04D(float now)
        {
            float tele = BossTiming04D.Telegraph(BossMove04D.DjinnChoir, Diff04D);
            int n = BossProfiles04D.ChoirCount(Diff04D);
            _volley04D++;
            _volleyHits04D[_volley04D & 3] = 0;
            int made = 0;
            for (int i = 0; i < n; i++)
            {
                Shard04D s = TakeShard04D();
                if (s == null) break;
                float a = (i - (n - 1) * 0.5f) * 28f * Mathf.Deg2Rad;
                s.Offset = new Vector3(Mathf.Sin(a) * 1.5f, 0.9f + Mathf.Cos(a) * 0.45f, -0.35f);
                s.Held = true;
                s.Lost = false;
                s.Volley = _volley04D;
                s.LaunchAt = float.MaxValue;
                s.Pos = HeldPoint04D(s);
                PlaceShard04D(s, Npc.transform.forward);
                made++;
            }
            if (made == 0) return false;
            Cast(tele + 0.3f);
            _beatUntil = now + tele;
            _beat = Beat04D.Telegraph;
            SpawnDecoys04D(2);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, Npc.ChestPosition, 0.8f, 1.35f, 60f);
            return true;
        }

        private Vector3 HeldPoint04D(Shard04D s)
        {
            float sc = Mathf.Max(1f, Npc.Rig.Scale);
            Transform t = Npc.transform;
            float spin = Mathf.Sin(Time.time * 3f + s.Offset.x) * 0.12f;
            return Npc.ChestPosition + (MathUtil.RightOf(t.forward) * s.Offset.x + Vector3.up * (s.Offset.y + spin) + t.forward * s.Offset.z) * sc;
        }

        private static void PlaceShard04D(Shard04D s, Vector3 dir)
        {
            if (s.Go == null) return;
            s.Go.transform.SetPositionAndRotation(s.Pos, Quaternion.LookRotation(dir.sqrMagnitude > 1e-6f ? dir : Vector3.forward));
        }

        private void TickChoirTelegraph04D(float now)
        {
            for (int i = 0; i < _shards.Count; i++)
            {
                Shard04D s = _shards[i];
                if (!s.Live || !s.Held) continue;
                s.Pos = HeldPoint04D(s);
                PlaceShard04D(s, s.Pos - Npc.ChestPosition);
            }
        }

        /// <summary>
        /// The choir leaves: each shard a stagger apart, fanned round the line to the player (in three dimensions, so a
        /// flyer faces it too). The decoys fade without hurting anyone.
        /// </summary>
        private void CommitChoir04D(float now)
        {
            PlayerController p = Game.Player;
            DropDecoys04D(false);
            if (p == null || p.IsDead)
            {
                DropHeldShards04D();
                FinishMove04D(now);
                return;
            }
            Vector3 target = PlayerAimPoint04D(p);
            int n = 0;
            for (int i = 0; i < _shards.Count; i++) if (_shards[i].Live && _shards[i].Held && _shards[i].Volley == _volley04D) n++;
            int k = 0;
            for (int i = 0; i < _shards.Count; i++)
            {
                Shard04D s = _shards[i];
                if (!s.Live || !s.Held || s.Volley != _volley04D) continue;
                Vector3 to = target - s.Pos;
                float yaw = (k - (n - 1) * 0.5f) * 14f;
                Vector3 fan = QMath.AngleAxis(yaw, Vector3.up) * to;
                s.Dir = (fan.normalized + Vector3.up * 0.12f).normalized;
                s.LaunchAt = now + k * BossProfiles04D.ChoirStagger;
                k++;
            }
            _choirTurn = BossProfiles04D.ChoirTurnRate(Diff04D);
            _choirSteer = BossProfiles04D.ChoirSteerSeconds(Diff04D);
            _choirLaunchEnd = now + k * BossProfiles04D.ChoirStagger;
            _beat = Beat04D.Active;
        }

        /// <summary>The volley is spent once every shard of it has landed or faded (or 1.6 s after the last left).</summary>
        private bool ChoirLaunched04D(float now)
        {
            if (now < _choirLaunchEnd) return false;
            if (now >= _choirLaunchEnd + 1.6f) return true;
            for (int i = 0; i < _shards.Count; i++) if (_shards[i].Live && _shards[i].Volley == _volley04D) return false;
            return true;
        }

        private Shard04D TakeShard04D()
        {
            if (_ledger == null) return null;
            int live = 0;
            Shard04D free = null;
            for (int i = 0; i < _shards.Count; i++)
            {
                if (_shards[i].Live) live++;
                else if (free == null) free = _shards[i];
            }
            if (live >= MaxShards04D || !_ledger.Open(BossOwned04D.Bolt)) return null;
            if (free == null)
            {
                free = new Shard04D();
                _shards.Add(free);
            }
            free.Go = TakeShardBody04D();
            free.Live = true;
            free.Age = 0f;
            free.Life = BossProfiles04D.ChoirLife;
            free.Trail = 0f;
            return free;
        }

        private void FreeShard04D(Shard04D s)
        {
            if (!s.Live) return;
            s.Live = false;
            s.Held = false;
            FreeShardBody04D(s.Go);
            s.Go = null;
            if (_ledger != null) _ledger.Release(BossOwned04D.Bolt);
        }

        private void UpdateShards04D(float dt)
        {
            if (_shards.Count == 0) return;
            float now = Time.time;
            PlayerController p = Game.Player;
            bool alive = p != null && !p.IsDead;
            Vector3 target = alive ? PlayerAimPoint04D(p) : Vector3.zero;
            for (int i = 0; i < _shards.Count; i++)
            {
                Shard04D s = _shards[i];
                if (!s.Live) continue;
                if (s.Held)
                {
                    if (now < s.LaunchAt)
                    {
                        if (_beat != Beat04D.Telegraph)
                        {
                            s.Pos = HeldPoint04D(s);
                            PlaceShard04D(s, s.Dir);
                        }
                        continue;
                    }
                    s.Held = false;
                    s.Age = 0f;
                    // Long enough to reach a high flyer, never longer than ChoirMaxLife.
                    s.Life = BossProfiles04D.ChoirLifeFor(alive ? Vector3.Distance(s.Pos, target) : 0f);
                    if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Whoosh, s.Pos, 0.35f, 0.2f, 45f);
                }
                s.Age += dt;
                if (s.Age >= s.Life)
                {
                    BurstShard04D(s, s.Pos);
                    continue;
                }
                bool inWindow = s.Age <= _choirSteer;
                bool sees = true;
                if (inWindow && !s.Lost) sees = alive && !Physics.Linecast(s.Pos, target, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                s.Dir = BossLanes04D.SteerShard(s.Dir, target - s.Pos, _choirTurn, dt, sees, inWindow, ref s.Lost);
                Vector3 p1 = s.Pos + s.Dir * BossProfiles04D.ChoirSpeed * dt;
                if (alive && ShardHitsPlayer04D(p, s.Pos, p1))
                {
                    if (_volleyHits04D[s.Volley & 3] < BossProfiles04D.ChoirMaxHits(Diff04D) && HitPlayer04D(0.55f, s.Pos, DamageType.Bullet, DamageFlags.None, 2.5f))
                        _volleyHits04D[s.Volley & 3]++;
                    BurstShard04D(s, p1);
                    continue;
                }
                RaycastHit hit;
                if (Physics.Linecast(s.Pos, p1, out hit, Layers.WorldMask | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore))
                {
                    BurstShard04D(s, hit.point);
                    continue;
                }
                s.Pos = p1;
                PlaceShard04D(s, s.Dir);
                s.Trail -= dt;
                if (s.Trail <= 0f && Game.Fx != null)
                {
                    s.Trail = 0.05f;
                    Game.Fx.BreathPuff(p1, -s.Dir * 0.6f, ChoirColor04D, 0.35f, false);
                }
            }
        }

        /// <summary>This step of a shard's flight (a to b) meets the player's body (or the car they drive).</summary>
        private static bool ShardHitsPlayer04D(PlayerController p, Vector3 a, Vector3 b)
        {
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                Vector3 c = p.CurrentVehicle.transform.position + Vector3.up;
                return (b - c).sqrMagnitude < 2.4f * 2.4f;
            }
            Vector3 lo = p.transform.position + Vector3.up * 0.35f, hi = p.transform.position + Vector3.up * 1.6f;
            const float r = 0.62f;
            Vector3 mid = (a + b) * 0.5f;
            float t;
            return (MathUtil.ClosestPointOnSegment(lo, hi, b, out t) - b).sqrMagnitude < r * r || (MathUtil.ClosestPointOnSegment(lo, hi, mid, out t) - mid).sqrMagnitude < r * r;
        }

        private void BurstShard04D(Shard04D s, Vector3 at)
        {
            if (Game.Fx != null) Game.Fx.CrystalBurst(at, ChoirColor04D, 8, 0.5f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, at, 0.25f, 0.2f, 35f);
            FreeShard04D(s);
        }

        /// <summary>A choir broken in its wind-up: the shards still gathered round the Djinn crumble harmlessly.</summary>
        private void DropHeldShards04D()
        {
            for (int i = 0; i < _shards.Count; i++)
            {
                Shard04D s = _shards[i];
                if (!s.Live || !s.Held) continue;
                if (Game.Fx != null) Game.Fx.SandBurst(s.Pos, 0.4f, 4);
                FreeShard04D(s);
            }
        }

        private int ReleaseAllShards04D()
        {
            int n = 0;
            for (int i = 0; i < _shards.Count; i++)
            {
                if (!_shards[i].Live) continue;
                n++;
                FreeShard04D(_shards[i]);
            }
            return n;
        }

        private int LiveShards04D
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _shards.Count; i++) if (_shards[i].Live) n++;
                return n;
            }
        }

        // ================================================================== decoy tells

        /// <summary>Harmless shimmering sand pillars that copy the casting tell somewhere else (never a ground ring: rings always hurt).</summary>
        private void SpawnDecoys04D(int n)
        {
            if (Kind != HumanoidCreature.MirageDjinn) return;
            Vector3 me = Npc.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector3 spot;
                if (!AddSpot(me, 7f, 14f, Random.value * Mathf.PI * 2f, out spot)) continue;
                if (_ledger == null || !_ledger.Open(BossOwned04D.Decoy)) break;
                Beam04DLive b = AddBeam04D(spot, spot + Vector3.up * 4.2f, 0.55f, DecoyColor04D, 0.4f, _moveSerial);
                if (b == null)
                {
                    _ledger.Release(BossOwned04D.Decoy);
                    break;
                }
                _decoys.Add(b);
                if (Game.Fx != null) Game.Fx.SandBurst(spot + Vector3.up, 1.2f, 10);
            }
        }

        private void UpdateDecoys04D(float dt)
        {
            if (_decoys.Count == 0) return;
            float now = Time.time;
            for (int i = 0; i < _decoys.Count; i++)
            {
                Beam04DLive b = _decoys[i];
                if (b == null || b.Done || b.Fx == null) continue;
                b.Alpha = 0.3f + 0.18f * Mathf.Sin(now * 9f + i * 1.7f);
                SetBeam04D(b, b.A, b.B);
            }
            _decoyPuff -= dt;
            if (_decoyPuff <= 0f && Game.Fx != null)
            {
                _decoyPuff = 0.15f;
                Beam04DLive b = _decoys[Random.Range(0, _decoys.Count)];
                if (b != null && !b.Done) Game.Fx.SandBurst(b.A + Vector3.up * Random.Range(0.3f, 3.5f), 0.5f, 3);
            }
        }

        /// <summary>The decoy pillars fade (a harmless puff unless silent). Returns how many there were.</summary>
        private int DropDecoys04D(bool silent)
        {
            int n = _decoys.Count;
            for (int i = 0; i < _decoys.Count; i++)
            {
                Beam04DLive b = _decoys[i];
                if (b == null) continue;
                if (!silent && Game.Fx != null) Game.Fx.SandBurst(b.A + Vector3.up * 1.5f, 1f, 8);
                ReleaseBeam04D(b);
                if (_ledger != null) _ledger.Release(BossOwned04D.Decoy);
            }
            _decoys.Clear();
            return n;
        }

        // ================================================================== shifting lanes

        /// <summary>
        /// Picks a legal new lane (BossProfiles04D.RelocateValid, then ground at about its height, room for the body, a
        /// clear line to the player, not inside a building) and shimmers there first so the player sees where it goes.
        /// </summary>
        private bool BeginRelocate04D(float now)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return false;
            Vector3 me = Npc.transform.position;
            Vector3 pp = p.transform.position;
            bool found = false;
            for (int k = 0; k < 14 && !found; k++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Vector3 c = pp + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * Random.Range(12f, 24f);
                if (!BossProfiles04D.RelocateValid(c, pp, me, ArenaCenter, ArenaRadius)) continue;
                float g;
                if (!CreatureDirector.GroundAt(c, 6f, out g) || Mathf.Abs(g - me.y) > 5f) continue;
                c.y = g;
                if (Physics.CheckCapsule(c + Vector3.up * 1f, c + Vector3.up * 2.4f, 0.7f, AddBlockMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(c + Vector3.up * 1.4f, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (Game.Layout != null && Game.Layout.IsInsideBuilding(c, 0.4f)) continue;
                _relocTo = c;
                found = true;
            }
            if (!found) return false;
            float tele = BossTiming04D.Telegraph(BossMove04D.DjinnRelocate, Diff04D);
            Cast(tele + 0.2f);
            _beatUntil = now + tele;
            _beat = Beat04D.Telegraph;
            if (Game.Fx != null)
            {
                Game.Fx.SandBurst(_relocTo + Vector3.up, 1.6f, 16);
                Game.Fx.SandBurst(Npc.ChestPosition, 1.4f, 12);
            }
            return true;
        }

        private void CommitRelocate04D(float now)
        {
            bool clear = !Physics.CheckCapsule(_relocTo + Vector3.up * 1f, _relocTo + Vector3.up * 2.4f, 0.7f, AddBlockMask, QueryTriggerInteraction.Ignore);
            if (clear)
            {
                if (Game.Fx != null) Game.Fx.SandBurst(Npc.ChestPosition, 1.8f, 18);
                Vector3 look = Npc.Target != null ? MathUtil.Flat(Npc.Target.transform.position - _relocTo) : Npc.transform.forward;
                Npc.CreatureTeleport(_relocTo, look.sqrMagnitude > 0.01f ? MathUtil.YawOf(look) : Npc.transform.eulerAngles.y);
                if (Game.Fx != null) Game.Fx.SandBurst(Npc.ChestPosition, 1.8f, 18);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, _relocTo, 0.7f, 0.7f, 60f);
            }
            _beat = Beat04D.Idle;
            _glowUntil = 0f;
            Npc.CreatureBusyUntil = Mathf.Min(Npc.CreatureBusyUntil, now);
            _next04D = now + 0.9f;
        }

        // ================================================================== the illusion phase

        /// <summary>
        /// The one illusion phase: the fight's one reinforcement batch becomes at most two mirages wearing the Djinn's
        /// face, for a bounded time. They cast no shadow, mimic the casting glow, never strike and pop at a touch; the
        /// real Djinn shifts lanes among them. A mirage's death never counts as the boss's.
        /// </summary>
        private void StartIllusion04D()
        {
            if (_illusionUsed || Game.Creatures == null || _budget == null || _ledger == null) return;
            _illusionUsed = true;
            if (!_budget.TryReserveBatch()) return;
            float now = Time.time;
            _illusionUntil = now + BossTiming04D.IllusionSeconds(Diff04D);
            int n = _budget.Allowance(BossProfiles04D.MaxSupportActors, AliveSupports04D);
            int faceSeed = Npc.CreatureOptions != null ? Npc.CreatureOptions.Seed : 0;
            Vector3 me = Npc.transform.position;
            int made = 0;
            for (int i = 0; i < n; i++)
            {
                // Hero-detail mirages only from bodies built ahead (never a main-thread hero build mid-fight).
                if (!Game.Creatures.BossAddReady("mirage_djinn", faceSeed)) break;
                Vector3 p;
                if (!AddSpot(me, 6f, 9f, (i + Random.value * 0.5f) / Mathf.Max(1, n) * Mathf.PI * 2f, out p)) continue;
                if (!_ledger.Open(BossOwned04D.Illusion)) break;
                CreatureSpawnOptions o = AddOptions();
                o.Seed = faceSeed;
                o.HealthScale = 0.0005f;
                o.DamageScale = 0.05f;
                o.DisplayName = Npc.DisplayName;
                float yaw = Npc.Target != null ? MathUtil.YawOf(Npc.Target.transform.position - p) : Random.value * 360f;
                NpcController c = Game.Creatures.SpawnBossAdd("mirage_djinn", p, yaw, o) as NpcController;
                if (c == null)
                {
                    _ledger.Release(BossOwned04D.Illusion);
                    continue;
                }
                c.CreatureMirage = true;
                c.CreatureBusyUntil = _illusionUntil + 1f;
                if (c.Rig != null && c.Rig.Body != null) c.Rig.Body.shadowCastingMode = ShadowCastingMode.Off;
                if (c.CreatureKit != null) c.CreatureKit.ScriptedOnly = true;
                if (Game.Fx != null) Game.Fx.SandBurst(c.ChestPosition, 1.4f, 14);
                _copies.Add(c);
                made++;
            }
            _budget.NoteAdmitted(made);
            _sinceRelocate = 3;
            if (made == 0)
            {
                // No mirage body was ready (or no clear ground): the batch is spent and the phase passes without one.
                _illusionUntil = 0f;
                Log.Info("Boss 04D: mirage_djinn illusion phase had no mirage ready, skipped");
                return;
            }
            _copiesUntil = _illusionUntil;
            Callout("Mirages rise: the real Djinn casts a shadow");
            Log.Info("Boss 04D: mirage_djinn illusion phase, " + made + " mirages for " + BossTiming04D.IllusionSeconds(Diff04D).ToString("0") + " s");
        }

        private void UpdateIllusion04D(float now)
        {
            if (_illusionUntil <= 0f) return;
            int live = 0;
            for (int i = 0; i < _copies.Count; i++)
            {
                NpcController c = _copies[i];
                if (c == null || c.IsDead) continue;
                live++;
                // Mirages never swing, and they glow with the real tell to test the player's eye.
                c.CreatureBusyUntil = Mathf.Max(c.CreatureBusyUntil, now + 0.5f);
                if (c.Anim != null) c.Anim.Telegraph = _beat == Beat04D.Telegraph ? 0.75f + 0.25f * Mathf.Sin(now * 14f + i) : 0f;
            }
            if (now < _illusionUntil && live > 0) return;
            _illusionUntil = 0f;
            ClearCopies04D(true);
            Callout("The illusion fades");
            Log.Info("Boss 04D: mirage_djinn illusion phase over");
        }
    }
}
