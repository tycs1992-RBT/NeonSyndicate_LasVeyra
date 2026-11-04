using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// NpcController, children of the city. A child always walks beside a grown-up (FamilyRules) and can never be harmed,
    /// grabbed or targeted: damage is refused (CharacterHealth.Invulnerable), knockdowns and ragdolls are refused
    /// (CharacterBase.NeverHarmed), it is a pacifist civilian that the targeting already skips (WeaponController
    /// .CrystalProtected), and the grab and stealth takedowns leave it out. When its grown-up is gone it slips away
    /// out of sight.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>True for a child of the city (spawned with MakeChild).</summary>
        public bool Child { get; private set; }

        /// <summary>The grown-up a child walks with, or null once they are gone.</summary>
        public NpcController Guardian { get; private set; }

        /// <summary>Turns this person into a child walking with a grown-up: protected from every harm, never armed, never a witness.</summary>
        public void MakeChild(NpcController guardian)
        {
            Child = true;
            Guardian = guardian;
            NeverHarmed = true;
            Pacifist = true;
            IgnoreThreats = true;
            Hostile = false;
            Courage = 0f;
            DropsLoot = false;
            CashDrop = 0;
            if (Health != null) Health.Invulnerable = true;
            SetState(NpcState.Scripted);
        }

        /// <summary>
        /// A child's step: walk to the spot beside the grown-up at their pace, run when behind, rejoin unseen when far.
        /// Without a grown-up the child leaves once nobody is looking. False when this is not a child.
        /// </summary>
        private bool UpdateChild(float dt, out float speed)
        {
            speed = 0f;
            if (!Child) return false;
            NpcController g = Guardian;
            if (g == null || g.IsDead || !g.isActiveAndEnabled)
            {
                Guardian = null;
                if (!LiveCastVisual.Seen(this)) Destroy(gameObject);
                return true;
            }
            Vector3 forward = MathUtil.Flat(g.transform.forward);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 spot = FamilyRules.Spot(g.transform.position, forward, right);
            float d = MathUtil.FlatDistance(transform.position, spot);
            if (d > FamilyRules.Rejoin && !LiveCastVisual.Seen(this))
            {
                transform.position = spot;
                return true;
            }
            _goal = spot;
            _hasGoal = true;
            speed = FamilyRules.Speed(d, MathUtil.Flat(g._vel).magnitude, WalkSpeed, RunSpeed);
            if (speed <= 0f) FaceTowards(g.transform.position + forward * 4f, dt);
            return true;
        }
    }
}
