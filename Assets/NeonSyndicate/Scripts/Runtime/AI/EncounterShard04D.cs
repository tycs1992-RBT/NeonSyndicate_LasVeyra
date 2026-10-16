using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The refracted shard an encounter opponent throws: a pooled glowing crystal that flies straight, is swept against
    /// the world, cars and people every frame (creatures, the thrower among them, let it pass) and bursts on the first
    /// thing it meets. Every shard remembers the encounter that owns it, so a victory or an abort cancels the ones in
    /// flight; at most MaxActive fly at once and a refused shot is simply not thrown (never a silent damage change).
    /// One shared mesh and one material for the session; release resets every field.
    /// </summary>
    public sealed class EncounterShard04D : MonoBehaviour
    {
        public const int MaxActive = 12;
        private const float Life = 2.6f;
        private const float Radius = 0.12f;
        private static readonly Color ShardColor = new Color(0.7f, 0.95f, 1f);
        private static readonly List<EncounterShard04D> _free = new List<EncounterShard04D>();
        private static readonly List<EncounterShard04D> _active = new List<EncounterShard04D>();
        private static readonly RaycastHit[] _hits = new RaycastHit[8];
        private static Transform _root;
        private static Material _mat;

        private Vector3 _vel;
        private float _damage;
        private float _life;
        private GameObject _source;
        private int _owner;
        private float _trail;

        /// <summary>Shards in flight right now (debug page).</summary>
        public static int ActiveCount { get { return _active.Count; } }

        public static void ResetStatics()
        {
            _free.Clear();
            _active.Clear();
            _root = null;
            _mat = null;
        }

        private static int ShotMask { get { return Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Characters); } }

        /// <summary>Throws one shard for an encounter (owner = its run serial). False when the active cap refuses it.</summary>
        public static bool Fire(Vector3 from, Vector3 dir, float speed, float damage, GameObject source, int owner)
        {
            if (_active.Count >= MaxActive || dir.sqrMagnitude < 1e-6f || !MathUtil.IsFinite(from)) return false;
            EncounterShard04D s = null;
            while (_free.Count > 0 && s == null)
            {
                s = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
            }
            if (s == null) s = Create();
            Vector3 d = dir.normalized;
            s.transform.SetPositionAndRotation(from, Quaternion.LookRotation(d));
            s._vel = d * Mathf.Max(1f, speed);
            s._damage = Mathf.Max(0f, damage);
            s._life = Life;
            s._source = source;
            s._owner = owner;
            s._trail = 0f;
            s.gameObject.SetActive(true);
            _active.Add(s);
            return true;
        }

        /// <summary>Cancels every shard in flight that belongs to one encounter (victory, abort, reset). Returns how many.</summary>
        public static int CancelOwner(int owner)
        {
            int n = 0;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EncounterShard04D s = _active[i];
                if (s == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                if (s._owner != owner) continue;
                if (Game.Fx != null) Game.Fx.CrystalBurst(s.transform.position, ShardColor, 5, 0.4f);
                s.Release();
                n++;
            }
            return n;
        }

        /// <summary>Cancels every shard (new game, load, title, shutdown).</summary>
        public static void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null) _active[i].Release();
                else _active.RemoveAt(i);
            }
            _active.Clear();
        }

        private static EncounterShard04D Create()
        {
            if (_root == null) _root = new GameObject("Encounter Shards 04D").transform;
            if (_mat == null) _mat = Mats.Emissive(ShardColor, 2.6f);
            GameObject go = Shapes.MeshObject("Refracted Shard", Shapes.CubeMesh, new[] { _mat }, _root, false);
            go.layer = Layers.IgnoreRaycast;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.6f);
            return go.AddComponent<EncounterShard04D>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _life -= dt;
            Vector3 p0 = transform.position;
            Vector3 p1 = p0 + _vel * dt;
            if (_life <= 0f)
            {
                Burst(p1);
                return;
            }
            PlayerController p = Game.Player;
            if (p != null && !p.IsDead && HitsPlayer(p, p0, p1))
            {
                HitPlayer(p);
                Burst(p1);
                return;
            }
            if (Stopped(p0, p1)) return;
            transform.position = p1;
            _trail -= dt;
            if (_trail <= 0f && Game.Fx != null)
            {
                _trail = 0.05f;
                Game.Fx.BreathPuff(p1, -_vel * 0.03f, ShardColor, 0.35f, false);
            }
        }

        private bool HitsPlayer(PlayerController p, Vector3 a, Vector3 b)
        {
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                Vector3 c = p.CurrentVehicle.transform.position + Vector3.up;
                return (b - c).sqrMagnitude < 2.4f * 2.4f;
            }
            Vector3 lo = p.transform.position + Vector3.up * 0.35f;
            Vector3 hi = p.transform.position + Vector3.up * 1.6f;
            float r = 0.4f + Radius;
            Vector3 mid = (a + b) * 0.5f;
            float t;
            return (MathUtil.ClosestPointOnSegment(lo, hi, b, out t) - b).sqrMagnitude < r * r || (MathUtil.ClosestPointOnSegment(lo, hi, mid, out t) - mid).sqrMagnitude < r * r;
        }

        /// <summary>The nearest wall, car or person on this frame's path (creatures and the thrower are skipped).</summary>
        private bool Stopped(Vector3 a, Vector3 b)
        {
            Vector3 seg = b - a;
            float len = seg.magnitude;
            if (len < 1e-5f) return false;
            int n = Physics.RaycastNonAlloc(a, seg / len, _hits, len, ShotMask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDist = float.MaxValue;
            CharacterBase body = null;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null || h.distance >= bestDist) continue;
                CharacterBase c = h.collider.GetComponentInParent<CharacterBase>();
                if (c != null && (c.IsDead || c.gameObject == _source || (c.Team != null && c.Team.Team == Team.Creature))) continue;
                best = i;
                bestDist = h.distance;
                body = c;
            }
            if (best < 0) return false;
            RaycastHit hit = _hits[best];
            if (body != null)
            {
                PlayerController p = body as PlayerController;
                if (p != null) HitPlayer(p);
                else HitBody(body, hit.point);
            }
            else
            {
                Vehicle v = hit.collider.GetComponentInParent<Vehicle>();
                if (v != null && !v.IsDestroyed) v.ApplyDamage(_damage * 1.5f, _source);
            }
            Burst(hit.point);
            return true;
        }

        private void HitPlayer(PlayerController p)
        {
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                p.CurrentVehicle.ApplyDamage(_damage * 1.5f, _source);
                return;
            }
            DamageInfo info = DamageInfo.Make(_damage, p.ChestPosition, _vel, _source, DamageType.Bullet);
            info.Force = 3f;
            p.Health.TakeDamage(info);
        }

        private void HitBody(CharacterBase c, Vector3 at)
        {
            if (c.InVehicle && c.CurrentVehicle != null)
            {
                c.CurrentVehicle.ApplyDamage(_damage * 1.5f, _source);
                return;
            }
            if (c.Health == null) return;
            DamageInfo info = DamageInfo.Make(_damage, at, _vel, _source, DamageType.Bullet);
            info.Force = 3f;
            c.Health.TakeDamage(info);
        }

        private void Burst(Vector3 at)
        {
            if (Game.Fx != null) Game.Fx.CrystalBurst(at, ShardColor, 10, 0.7f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, at, 0.35f, 0.15f, 35f);
            Release();
        }

        private void Release()
        {
            _vel = Vector3.zero;
            _damage = 0f;
            _life = 0f;
            _source = null;
            _owner = 0;
            _trail = 0f;
            if (this != null && gameObject != null) gameObject.SetActive(false);
            _active.Remove(this);
            if (!_free.Contains(this)) _free.Add(this);
        }

        private void OnDestroy()
        {
            _active.Remove(this);
            _free.Remove(this);
        }
    }
}
