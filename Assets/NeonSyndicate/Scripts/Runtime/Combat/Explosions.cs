using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum ExplosionKind
    {
        Grenade,
        Rocket,
        Vehicle,
        Barrel,
        GasPump,
        Power
    }

    /// <summary>Explosions with falloff damage, physics impulses, ragdolls, chain reactions, camera shake and crime reports.</summary>
    public sealed class Explosions : MonoBehaviour
    {
        private readonly Collider[] _buf = new Collider[192];
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private readonly HashSet<Rigidbody> _hitBodies = new HashSet<Rigidbody>();

        private struct Pending
        {
            public Vector3 Pos;
            public float Radius;
            public float Damage;
            public GameObject Source;
            public ExplosionKind Kind;
        }

        private readonly Queue<Pending> _chain = new Queue<Pending>();
        private bool _busy;

        /// <summary>
        /// Explosions triggered while another one is being resolved (barrels, cars) are queued and resolved after it,
        /// because they share the overlap buffer and hit sets.
        /// </summary>
        public void Explode(Vector3 pos, float radius, float damage, GameObject source, ExplosionKind kind)
        {
            _chain.Enqueue(new Pending { Pos = pos, Radius = radius, Damage = damage, Source = source, Kind = kind });
            if (_busy) return;
            _busy = true;
            try
            {
                int guard = 0;
                while (_chain.Count > 0 && guard++ < 64)
                {
                    Pending p = _chain.Dequeue();
                    Resolve(p.Pos, p.Radius, p.Damage, p.Source, p.Kind);
                }
            }
            finally
            {
                // Even if one blast throws, never leave stale blasts queued for the next explosion.
                _chain.Clear();
                _busy = false;
            }
        }

        private void Resolve(Vector3 pos, float radius, float damage, GameObject source, ExplosionKind kind)
        {
            if (Game.Fx != null) Game.Fx.ExplosionFx(pos, radius);
            if (Game.Audio != null) Game.Audio.PlayAt(radius > 5f ? SfxId.Explosion : SfxId.ExplosionSmall, pos, 1f, Random.Range(0.85f, 1.05f), 400f);
            if (Game.Cam != null)
            {
                float d = Vector3.Distance(Game.Cam.transform.position, pos);
                Game.Cam.Shake(Mathf.Clamp01(1.2f - d / (radius * 7f)));
            }
            Threats.Report(pos, radius * 14f, source, ThreatKind.Explosion);
            if (source != null && Game.Player != null && source == Game.Player.gameObject && Game.Wanted != null)
                Game.Wanted.ReportCrime(CrimeType.Explosion, pos, null);

            _hitTargets.Clear();
            _hitBodies.Clear();
            int n = Physics.OverlapSphereNonAlloc(pos, radius * 1.6f, _buf, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            // Copy the results: damage callbacks can run physics queries of their own.
            Collider[] hits = new Collider[n];
            System.Array.Copy(_buf, hits, n);
            for (int i = 0; i < n; i++)
            {
                Collider c = hits[i];
                if (c == null) continue;
                Vector3 closest = c.ClosestPoint(pos);
                float dist = Vector3.Distance(closest, pos);
                float fall = Mathf.Clamp01(1f - dist / (radius * 1.6f));
                if (fall <= 0f) continue;
                Rigidbody rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic && _hitBodies.Add(rb))
                {
                    float vel = Mathf.Lerp(2f, 11f, fall) * (kind == ExplosionKind.Power ? 0.6f : 1f);
                    rb.AddExplosionForce(vel, pos, radius * 1.6f, 1.4f, ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere * vel * 0.6f, ForceMode.VelocityChange);
                }
                IDamageable dmg = c.GetComponentInParent<IDamageable>();
                if (dmg != null && _hitTargets.Add(dmg))
                {
                    Vector3 center = c.bounds.center;
                    float los = 1f;
                    RaycastHit hit;
                    if (Physics.Linecast(pos + Vector3.up * 0.3f, center, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider != c) los = 0.3f;
                    }
                    float amount = damage * Mathf.Pow(fall, 0.8f) * los;
                    DamageInfo info = DamageInfo.Make(amount, center, (center - pos), source, DamageType.Explosion);
                    info.Force = Mathf.Lerp(3f, 16f, fall) * los;
                    dmg.TakeDamage(info);
                }
            }
        }
    }
}
