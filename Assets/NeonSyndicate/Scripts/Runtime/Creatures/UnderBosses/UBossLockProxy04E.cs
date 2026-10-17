using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: lets the existing lock-on (PlayerCombat), the pad aim assist (PlayerCamera), the lock-on reticle and the
    /// melee snap treat an Underheart boss like a character. The boss is not a Creature (its own controller on a
    /// procedural body), so the character scans never found it. This stand-in sits on a child of the boss body with no
    /// collider and is registered nowhere: its Health is the body's CharacterHealth (IsDead and the reticle's bar read
    /// the boss), BodyRadius and BodyHeight give the lock range, melee reach and camera framing, and its AimAnchor
    /// follows the best point to strike (UBossLockRules04E). Hits still land on the body's tagged colliders, never on
    /// this object, and CharacterHealth.Owner stays empty, so the boss's damage path is unchanged.
    /// </summary>
    public sealed class UBossLockProxy04E : CharacterBase
    {
        public UnderBossFight Fight;

        public override Vector3 Velocity { get { return Vector3.zero; } }

        protected override void OnBeforeRagdoll() { }

        protected override void OnAfterRagdoll() { }

        /// <summary>True while the boss can be locked: its fight is waiting or running and the body lives.</summary>
        public bool Lockable
        {
            get { return Fight != null && Fight.Running && Health != null && Health.IsAlive; }
        }

        /// <summary>The lockable stand-in of the running Underheart boss fight (null when there is none).</summary>
        public static UBossLockProxy04E Active
        {
            get
            {
                UBossLockProxy04E p = Game.UnderBosses != null ? Game.UnderBosses.LockProxy : null;
                return p != null && p.Lockable ? p : null;
            }
        }

        /// <summary>
        /// For PlayerCombat.FindLockTarget: the boss as a lock-on candidate with its score (lower wins), using the same
        /// view angle, side, range and line-of-sight rules as a creature. False when there is no lockable boss in reach.
        /// </summary>
        public static bool TryLockScore(Vector3 player, Vector3 head, Vector3 camForward, Vector3 camRight, Vector3 fromDir, CharacterBase from, int side, float range, out CharacterBase target, out float score)
        {
            target = null;
            score = float.MaxValue;
            UBossLockProxy04E b = Active;
            if (b == null || b == from) return false;
            Vector3 to = b.transform.position - player;
            float dist = Mathf.Max(0f, to.magnitude - b.BodyRadius);
            if (dist > range + Mathf.Min(10f, b.BodyRadius)) return false;
            Vector3 flat = MathUtil.Flat(to);
            float angle = Vector3.Angle(camForward, flat);
            if (side == 0 && angle > 80f && dist > 4f) return false;
            if (side != 0)
            {
                float s = Vector3.Dot(flat.normalized - fromDir, camRight);
                if (s * side < 0.05f) return false;
                angle = Vector3.Angle(fromDir, flat);
            }
            if (Physics.Linecast(head, b.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            target = b;
            score = UBossLockRules04E.Score(angle, dist);
            return true;
        }

        /// <summary>
        /// For PlayerCamera's pad aim assist: offers the boss's lock point (its open weak point, else its centre) as a
        /// candidate like a creature's chest. Updates best and bestAngle only when it is nearer the aim with a clear line.
        /// </summary>
        public static void ConsiderForAimAssist(Vector3 eye, Vector3 forward, CharacterBase locked, ref CharacterBase best, ref float bestAngle)
        {
            UBossLockProxy04E b = Active;
            if (b == null || b.Fight == null || b.Fight.State != UnderBossFight.FightState.Fighting) return;
            Vector3 to = b.ChestPosition - eye;
            float dist = to.magnitude;
            if (dist > UBossLockRules04E.AimAssistRange || dist < 1f) return;
            float ang = Vector3.Angle(forward, to);
            if (b == locked) ang *= 0.3f;
            if (ang >= bestAngle) return;
            if (Physics.Linecast(eye, b.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            bestAngle = ang;
            best = b;
        }
    }
}
