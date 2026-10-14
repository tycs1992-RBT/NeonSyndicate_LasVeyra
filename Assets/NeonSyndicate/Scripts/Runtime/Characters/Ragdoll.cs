using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns a HumanoidRig into a physics ragdoll (rigidbodies, colliders and character joints on the real bones),
    /// and can restore the character so knocked-down people get back up.
    /// </summary>
    public sealed class Ragdoll : MonoBehaviour
    {
        public bool Active { get; private set; }
        public float ActiveTime { get; private set; }
        private HumanoidRig _rig;
        private readonly List<Rigidbody> _bodies = new List<Rigidbody>();
        private readonly List<Collider> _colliders = new List<Collider>();
        private readonly List<CharacterJoint> _joints = new List<CharacterJoint>();
        private Rigidbody _hips;
        private bool _frozen;
        private float _stillTime;

        public Rigidbody HipsBody { get { return _hips; } }

        public void Init(HumanoidRig rig)
        {
            _rig = rig;
        }

        public void Activate(Vector3 impulse, Vector3 point)
        {
            if (Active || _rig == null) return;
            Active = true;
            ActiveTime = 0f;
            _frozen = false;
            _stillTime = 0f;
            float s = _rig.Scale;
            _hips = AddBody(_rig.Hips, 12f, null, BoxCol(_rig.Hips, new Vector3(0f, 0f, 0f), new Vector3(0.32f, 0.24f, 0.22f) * s), Vector3.right, -15f, 15f, 10f);
            Rigidbody chest = AddBody(_rig.Chest, 14f, _hips, BoxCol(_rig.Chest, new Vector3(0f, 0.08f * s, 0f), new Vector3(0.38f, 0.42f, 0.23f) * s), Vector3.right, -35f, 20f, 18f);
            Rigidbody head = AddBody(_rig.Head, 4.5f, chest, SphereCol(_rig.Head, new Vector3(0f, 0.11f * s, 0f), 0.13f * s), Vector3.right, -40f, 40f, 32f);
            Rigidbody uaL = AddBody(_rig.UpperArmL, 2.2f, chest, CapsCol(_rig.UpperArmL, 0.29f * s, 0.06f * s), Vector3.right, -140f, 50f, 70f);
            Rigidbody faL = AddBody(_rig.ForearmL, 1.6f, uaL, CapsCol(_rig.ForearmL, 0.3f * s, 0.05f * s), Vector3.right, -130f, 2f, 8f);
            Rigidbody uaR = AddBody(_rig.UpperArmR, 2.2f, chest, CapsCol(_rig.UpperArmR, 0.29f * s, 0.06f * s), Vector3.right, -140f, 50f, 70f);
            Rigidbody faR = AddBody(_rig.ForearmR, 1.6f, uaR, CapsCol(_rig.ForearmR, 0.3f * s, 0.05f * s), Vector3.right, -130f, 2f, 8f);
            Rigidbody thL = AddBody(_rig.ThighL, 6.5f, _hips, CapsCol(_rig.ThighL, 0.43f * s, 0.08f * s), Vector3.right, -95f, 30f, 25f);
            Rigidbody shL = AddBody(_rig.ShinL, 4f, thL, CapsCol(_rig.ShinL, 0.46f * s, 0.065f * s), Vector3.right, -2f, 125f, 5f);
            Rigidbody thR = AddBody(_rig.ThighR, 6.5f, _hips, CapsCol(_rig.ThighR, 0.43f * s, 0.08f * s), Vector3.right, -95f, 30f, 25f);
            Rigidbody shR = AddBody(_rig.ShinR, 4f, thR, CapsCol(_rig.ShinR, 0.46f * s, 0.065f * s), Vector3.right, -2f, 125f, 5f);
            for (int i = 0; i < _colliders.Count; i++)
                for (int j = i + 1; j < _colliders.Count; j++)
                    Physics.IgnoreCollision(_colliders[i], _colliders[j], true);
            // Inherit current velocity, then apply the hit.
            foreach (Rigidbody rb in _bodies) rb.linearVelocity = _inheritVelocity;
            if (impulse.sqrMagnitude > 0.0001f)
            {
                Rigidbody target = NearestBody(point);
                if (target != null) target.AddForce(impulse * 0.55f, ForceMode.VelocityChange);
                _hips.AddForce(impulse * 0.45f, ForceMode.VelocityChange);
                chest.AddForce(impulse * 0.45f, ForceMode.VelocityChange);
            }
            if (head == null || faL == null || faR == null || shL == null || shR == null) return;
        }

        private Vector3 _inheritVelocity;

        public void SetInheritedVelocity(Vector3 v)
        {
            _inheritVelocity = v;
        }

        private Rigidbody NearestBody(Vector3 p)
        {
            Rigidbody best = null;
            float bd = float.MaxValue;
            foreach (Rigidbody rb in _bodies)
            {
                float d = (rb.worldCenterOfMass - p).sqrMagnitude;
                if (d < bd) { bd = d; best = rb; }
            }
            return best;
        }

        private Collider BoxCol(Transform bone, Vector3 center, Vector3 size)
        {
            BoxCollider c = bone.gameObject.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
            return c;
        }

        private Collider SphereCol(Transform bone, Vector3 center, float r)
        {
            SphereCollider c = bone.gameObject.AddComponent<SphereCollider>();
            c.center = center;
            c.radius = r;
            return c;
        }

        private Collider CapsCol(Transform bone, float length, float radius)
        {
            CapsuleCollider c = bone.gameObject.AddComponent<CapsuleCollider>();
            c.direction = 1;
            c.center = new Vector3(0f, -length * 0.5f, 0f);
            c.height = length + radius * 2f;
            c.radius = radius;
            return c;
        }

        private Rigidbody AddBody(Transform bone, float mass, Rigidbody parent, Collider col, Vector3 axis, float lowTwist, float highTwist, float swing)
        {
            bone.gameObject.layer = Layers.Ragdoll;
            Rigidbody rb = bone.gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.8f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.solverIterations = 12;
            rb.solverVelocityIterations = 4;
            rb.maxDepenetrationVelocity = 3f;
            _bodies.Add(rb);
            _colliders.Add(col);
            if (parent != null)
            {
                CharacterJoint j = bone.gameObject.AddComponent<CharacterJoint>();
                j.connectedBody = parent;
                j.axis = axis;
                j.swingAxis = Vector3.forward;
                SoftJointLimit low = new SoftJointLimit(); low.limit = lowTwist;
                SoftJointLimit high = new SoftJointLimit(); high.limit = highTwist;
                SoftJointLimit s1 = new SoftJointLimit(); s1.limit = swing;
                SoftJointLimit s2 = new SoftJointLimit(); s2.limit = swing;
                j.lowTwistLimit = low;
                j.highTwistLimit = high;
                j.swing1Limit = s1;
                j.swing2Limit = s2;
                j.enableProjection = true;
                j.projectionDistance = 0.08f;
                j.projectionAngle = 25f;
                j.enablePreprocessing = false;
                _joints.Add(j);
            }
            return rb;
        }

        private void FixedUpdate()
        {
            if (!Active || _frozen || _hips == null) return;
            ActiveTime += Time.fixedDeltaTime;
            if (_hips.linearVelocity.sqrMagnitude < 0.04f) _stillTime += Time.fixedDeltaTime; else _stillTime = 0f;
            if (!MathUtil.IsFinite(_hips.position) || _hips.position.y < -60f)
            {
                // Physics blew up or fell out of the world: freeze in place rather than spam errors.
                Freeze();
            }
        }

        public bool IsSettled { get { return Active && (_stillTime > 0.8f || _frozen); } }

        /// <summary>Makes all bodies kinematic (corpse at rest, saves CPU).</summary>
        public void Freeze()
        {
            if (_frozen) return;
            _frozen = true;
            foreach (Rigidbody rb in _bodies) if (rb != null) rb.isKinematic = true;
        }

        public Vector3 HipsPosition { get { return _hips != null ? _hips.position : _rig.Hips.position; } }

        /// <summary>
        /// Removes the physics and returns control to the animator. The root is placed under the hips and turned the
        /// way the body will stand up; the current pose is kept so the animator blends smoothly into standing.
        /// </summary>
        public void Deactivate()
        {
            if (!Active) return;
            Vector3 hipsPos = HipsPosition;
            Quaternion hipsRot = _rig.Hips.rotation;
            Vector3 up = _rig.Hips.up;
            Vector3 fwd = _rig.Hips.forward;
            for (int i = _joints.Count - 1; i >= 0; i--) if (_joints[i] != null) DestroyImmediate(_joints[i]);
            for (int i = _bodies.Count - 1; i >= 0; i--) if (_bodies[i] != null) DestroyImmediate(_bodies[i]);
            for (int i = _colliders.Count - 1; i >= 0; i--) if (_colliders[i] != null) DestroyImmediate(_colliders[i]);
            _joints.Clear();
            _bodies.Clear();
            _colliders.Clear();
            _hips = null;
            Active = false;
            Vector3 ground = hipsPos;
            RaycastHit hit;
            if (Physics.Raycast(hipsPos + Vector3.up * 0.5f, Vector3.down, out hit, 4f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) ground = hit.point;
            else ground.y -= _rig.HipsHeight;
            // Face-down bodies push up facing where the head was; face-up bodies sit up facing their feet.
            Vector3 facing;
            if (Mathf.Abs(up.y) < 0.7f)
            {
                facing = MathUtil.Flat(up);
                if (fwd.y > 0f) facing = -facing;
            }
            else
            {
                facing = MathUtil.Flat(fwd);
            }
            if (facing.sqrMagnitude < 0.01f) facing = MathUtil.Flat(transform.forward);
            if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
            transform.position = ground + Vector3.up * 0.02f;
            transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            // Keep the lying pose (world space) so the get-up blends instead of popping.
            _rig.Hips.position = hipsPos;
            _rig.Hips.rotation = hipsRot;
            for (int i = 1; i < _rig.Bones.Length; i++)
            {
                if (_rig.Bones[i] == null) continue;
                _rig.Bones[i].localPosition = _rig.BindPositions[i];
                _rig.Bones[i].localScale = Vector3.one;
            }
            HumanoidAnimator anim = GetComponent<HumanoidAnimator>();
            if (anim != null) anim.SyncAfterRagdoll();
            else _rig.ResetPose();
        }
    }
}
