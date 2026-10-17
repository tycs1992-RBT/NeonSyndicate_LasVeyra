using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// What the player's helpers can hit in a boss fight. A boss body is not a character, so companion assists and fused
    /// abilities (which look for characters) would never find it; they ask the bosses system instead, which reads this:
    /// the body's CharacterHealth (so the part, window and element rules of FilterDamage still apply) and the fight's live
    /// shootable targets (the Cinder Matron's lures, the Kiln Regent's furnace vents). The shootables are collected once,
    /// right after the arena props are built, so no query allocates or searches the scene.
    /// </summary>
    public abstract partial class UnderBossFight
    {
        private readonly List<UBossTarget> _shootables = new List<UBossTarget>();
        private readonly List<Vector3> _shootableLocal = new List<Vector3>();

        /// <summary>Remembers every UBossTarget under this fight and the local point helpers aim at (its visible centre).</summary>
        private void CollectShootables()
        {
            _shootables.Clear();
            _shootableLocal.Clear();
            UBossTarget[] all = GetComponentsInChildren<UBossTarget>(true);
            for (int i = 0; i < all.Length; i++)
            {
                UBossTarget t = all[i];
                if (t == null) continue;
                _shootables.Add(t);
                _shootableLocal.Add(t.transform.InverseTransformPoint(VisibleCenter(t)));
            }
        }

        private static Vector3 VisibleCenter(UBossTarget t)
        {
            Collider c = t.GetComponentInChildren<Collider>();
            if (c != null) return c.bounds.center;
            Renderer r = t.GetComponentInChildren<Renderer>();
            if (r != null) return r.bounds.center;
            return t.transform.position + Vector3.up;
        }

        /// <summary>True while companion and fused-vehicle hits can land: the fight is in its fighting state with a living body.</summary>
        public bool AcceptsAssists
        {
            get { return State == FightState.Fighting && Body != null && Health != null && Health.Health > 0f; }
        }

        /// <summary>The body's damage receiver (null before setup finished).</summary>
        public CharacterHealth BodyHealth { get { return Health; } }

        /// <summary>How many shootable targets the fight built (live or not).</summary>
        public int ShootableCount { get { return _shootables.Count; } }

        /// <summary>A shootable target and the world point helpers aim at; false when it is gone, broken or not armed.</summary>
        public bool ShootableAt(int index, out UBossTarget target, out Vector3 point)
        {
            target = null;
            point = Vector3.zero;
            if (index < 0 || index >= _shootables.Count) return false;
            UBossTarget t = _shootables[index];
            if (t == null || !t.IsAlive) return false;
            target = t;
            point = t.transform.TransformPoint(_shootableLocal[index]);
            return true;
        }

        /// <summary>The point on the body's enabled hit volumes nearest to p (BodyCenter when none is enabled).</summary>
        public Vector3 BodySurfaceNear(Vector3 p)
        {
            UBossPartTag t = PartAt(p);
            return t != null && t.Collider != null ? t.Collider.ClosestPoint(p) : BodyCenter;
        }
    }
}
