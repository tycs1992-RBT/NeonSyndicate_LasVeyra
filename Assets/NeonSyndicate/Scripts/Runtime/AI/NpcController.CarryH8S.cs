using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Being lifted and thrown by Malik (PlayerController.Grab.cs): a carried person is held (no thinking, no moving),
    /// loses its capsule so it never shoves the carrier, rides where the carrier puts it with legs dangling, and is let
    /// go as a ragdoll with the throw's velocity through the ordinary knockdown, so landings, getting up, fleeing and
    /// fighting back all work as they always did.
    /// </summary>
    public sealed partial class NpcController
    {
        private bool _carriedH8S;

        /// <summary>Carried by Malik right now.</summary>
        public bool CarriedH8S { get { return _carriedH8S; } }

        /// <summary>Lifted: held indefinitely, capsule off, any attack dropped.</summary>
        public void BeginCarryH8S()
        {
            _carriedH8S = true;
            _heldUntil = Time.time + 9999f;
            CancelAttack();
            CombatDirector.ReleaseShotToken(this);
            _hasGoal = false;
            _vel = Vector3.zero;
            if (_cap != null) _cap.enabled = false;
            Anim.Velocity = Vector3.zero;
            Anim.Aiming = false;
            Anim.Grounded = false;
            Anim.Panic = 1f;
            StartleH8S(Game.PlayerPosition);
        }

        /// <summary>Where the carrier holds the body this frame.</summary>
        public void CarryToH8S(Vector3 position, float yaw)
        {
            if (!_carriedH8S) return;
            _yaw = yaw;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Anim.Velocity = Vector3.zero;
            Anim.VerticalVelocity = 0f;
            Anim.Grounded = false;
        }

        /// <summary>Let go (thrown or set down): capsule back, a ragdoll with this velocity for at least minSeconds.</summary>
        public void EndCarryH8S(Vector3 velocity, float minSeconds)
        {
            if (!_carriedH8S) return;
            _carriedH8S = false;
            _heldUntil = 0f;
            if (_cap != null && !IsDead) _cap.enabled = true;
            Anim.Grounded = true;
            Anim.Panic = 0f;
            if (!IsDead) KnockdownWithVelocity(Vector3.zero, velocity, ChestPosition, minSeconds);
        }
    }
}
