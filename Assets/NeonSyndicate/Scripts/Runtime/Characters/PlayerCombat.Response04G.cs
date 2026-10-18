using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class PlayerCombat
    {
        public bool DefenseReady04H { get { return State==Phase.Free || State==Phase.Attacking && _move!=null &&
            CombatResponse04G.CanDefend(_move,_t,MeleeMoves.ClassOf(_moveWeapon)); } }

        private float _dodgeBufferedUntil04G = -1f;
        private Vector3 _dodgeWish04G;
        private bool _swingCued04G;
        private bool _propsResolved04H;

        private void ResetResponse04G()
        {
            _dodgeBufferedUntil04G = -1f;
            _dodgeWish04G = Vector3.zero;
            _swingCued04G = false;
            _chain = 0;
            _chainUntil = 0f;
            _rollEndedAt = -10f;
            _lmbDownAt = -1f;
            _guardPrev = false;
            _guardStart = -99f;
        }

        private void BufferDodge04G(Vector3 wish)
        {
            // A fresh dodge cannot cancel another dodge, hit reaction, or a drink before its committed sip.
            if (State != Phase.Free && State != Phase.Attacking && State != Phase.Drinking) return;
            _dodgeWish04G = wish;
            _dodgeBufferedUntil04G = Time.time + CombatResponse04G.DodgeBuffer;
        }

        private void AdvanceDodge04G()
        {
            if (_dodgeBufferedUntil04G < Time.time) return;
            TryDodge(_dodgeWish04G);
            if (State == Phase.Dodging) _dodgeBufferedUntil04G = -1f;
        }

        private void EndAttack04G()
        {
            State = Phase.Free;
            _move = null;
            _charging = false;
            _hasQueue = false;
            Motion = Vector3.zero;
            if (P.Anim != null) P.Anim.AttackT = -1f;
        }
    }
}
