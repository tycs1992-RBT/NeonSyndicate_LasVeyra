using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HumanoidAnimator
    {
        private object _performanceOwner04L;
        private SagaEmotion04L _performanceMood04L;
        private float _performanceStart04L, _performanceUntil04L, _performanceWeight04L;
        private bool _performanceSpeaking04L, _performanceChild04L;
        private Vector3 _performanceLook04L;

        public void Direct04L(object owner, SagaEmotion04L mood, bool speaking, bool child, Vector3 look, float seconds)
        {
            _performanceOwner04L = owner;
            _performanceMood04L = mood;
            _performanceSpeaking04L = speaking;
            _performanceChild04L = child;
            _performanceLook04L = look;
            _performanceStart04L = Time.time;
            _performanceUntil04L = Time.time + seconds;
        }
        public void Release04L(object owner)
        {
            if (!ReferenceEquals(owner, _performanceOwner04L)) return;
            _performanceOwner04L = null;
            _performanceUntil04L = 0f;
        }
        private void Performance04L(float dt)
        {
            bool busy = HandsUp || Cower || Phone || Aiming || AttackT >= 0f || MeleeT >= 0f || ThrowT >= 0f
                || Guard > .1f || ParcelCarry04G || CrystalCast04G > .01f || DrinkT >= 0f || ParryT >= 0f
                || Ride != RidePose.None || SitWeight > .1f || Travel != TravelPose.None || Cover > .1f
                || DodgeT >= 0f || StaggerT >= 0f || !Grounded || Velocity.sqrMagnitude > .6f;
            bool active = _performanceOwner04L != null && Time.time < _performanceUntil04L;
            _performanceWeight04L = Mathf.MoveTowards(_performanceWeight04L, active && !busy ? 1f : 0f, dt * 4f);
            if (Rig == null) return;
            float blinkPhase = Mathf.Repeat(Time.time + GetInstanceID() % 17 * .17f, 4.7f);
            float blink = Mathf.Clamp01(1f - Mathf.Abs(blinkPhase - .12f) / .10f);
            Rig.Face04L(Frozen ? 0f : blink,
                _performanceMood04L == SagaEmotion04L.Concern || _performanceMood04L == SagaEmotion04L.Grief ? _performanceWeight04L * .7f : 0f,
                _performanceMood04L == SagaEmotion04L.Warmth || _performanceMood04L == SagaEmotion04L.Wonder ? _performanceWeight04L * .6f : 0f);
            if (busy || _performanceWeight04L < .001f) return;
            Vector3 chest, head, upper, fore;
            SagaDirection04L.Acting(_performanceMood04L, Time.time - _performanceStart04L,
                _performanceSpeaking04L, _performanceChild04L, out chest, out head, out upper, out fore);
            float w = _performanceWeight04L;
            Rig.Chest.localRotation *= QMath.Slerp(QMath.Identity, QMath.Euler(chest.x, chest.y, chest.z), w);
            float yaw, pitch;
            if (CastGestures04E.Look(QMath.Inverse(Rig.Chest.rotation) * (_performanceLook04L - Rig.Head.position), out yaw, out pitch))
            {
                head.y += yaw * .45f; head.x += pitch * .45f;
            }
            Rig.Head.localRotation *= QMath.Slerp(QMath.Identity, QMath.Euler(head.x, head.y, head.z), w);
            if (_inspectW < .05f && _pointW < .05f && _greetT < 0f && !HoldBlade && Hold == HoldType.None)
            {
                Blend(RigLayout.UpperArmR, QMath.Euler(upper.x, upper.y, upper.z), w * .8f);
                Blend(RigLayout.ForearmR, QMath.Euler(fore.x, fore.y, fore.z), w * .8f);
            }
        }
    }
}
