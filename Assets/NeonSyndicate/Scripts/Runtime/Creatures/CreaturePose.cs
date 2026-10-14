using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural creature animation as plain math (no engine calls), so the same code animates the game and the offline
    /// preview, on any thread. One instance per creature: fill CreaturePoseInput every frame, call Solve, then copy
    /// LocalRotations and LocalPositions onto the bone transforms (bind = identity rotations at the skeleton's offsets).
    /// The runtime moves the creature's transform; this is in-place motion plus foot IK.
    /// Legged plans walk, trot and gallop (or bound) by speed with the right footfall order; each paw is planted in the
    /// world during stance and reached with two-bone IK (ground from input.GroundHeight), lands where the body will be at
    /// mid-stance and lifts on an arc. The spine flexes, the body leans into turns, the head stays steady and looks at
    /// LookTarget, the tail lags and sways. Every action is an authored wind-up (a readable silhouette), a fast strike and
    /// a clear recovery that follow input.Windup and input.Strike. Flyers flap, glide, bank, hover, dive and flare to
    /// land; the wyrm's segments follow the head's trail (underground when Depth is set); the shade floats and lashes.
    /// Death collapses the body and holds the final pose.
    /// In flight the runtime carries the transform through the air (RootPosition is wherever the transform is);
    /// Altitude only shapes the pose (legs reach for the ground in the landing flare). The serpent's transform marks its
    /// head: the body trails behind along the path the head took.
    /// LocalPositions carry procedural offsets for the body root (height, lunges), the wyrm's head and every segment (they
    /// hang straight off its root), the shade's core and the mite's body; everything else keeps its bind offset.
    /// Split by plan: CreaturePose.Legged.cs (gaits, feet, trunk, legs, tail), CreaturePose.Actions.cs (attacks and
    /// reactions), CreaturePose.Flight.cs (wings and flight), CreaturePose.Serpent.cs, CreaturePose.Small.cs.
    /// </summary>
    public sealed partial class CreaturePose
    {
        /// <summary>Per bone, relative to bind (identity at rest).</summary>
        public readonly Quaternion[] LocalRotations;
        /// <summary>Per bone, bind offsets plus any procedural offset (the root carries the body's position).</summary>
        public readonly Vector3[] LocalPositions;
        /// <summary>Bit i is set on the frame leg i plants (footstep sounds and dust).</summary>
        public int FootPlantMask;
        /// <summary>The body this pose animates.</summary>
        public readonly CreatureBody Body;

        private const float G = 9.81f;
        private readonly CreatureSkeleton _sk;
        private readonly CreatureLook _look;
        private readonly CreaturePlan _plan;
        private readonly int _n;
        private readonly int[] _parent;
        private readonly Vector3[] _bindLocal;
        private readonly Vector3[] _bindModel;
        /// <summary>Working pose in model space (the creature transform's frame): rotations and positions per bone.</summary>
        private readonly Quaternion[] _rot;
        private readonly Vector3[] _pos;
        /// <summary>Local position used this frame (bind unless a procedural offset moved the bone).</summary>
        private readonly Vector3[] _local;

        // ---- common state
        private bool _init;
        private Vector3 _prevRoot;
        private float _prevYaw;
        private Vector3 _vel;
        private float _speed, _yawRate, _accel, _clock, _alert, _hurt, _climb;
        private Vector3 _hurtDir = Vector3.back;
        private float _deathHold;
        private Vector3 _lookDir = Vector3.forward;
        private bool _hasLook;
        /// <summary>Size of the creature (metres) used to scale speeds and amplitudes.</summary>
        private readonly float _size;

        public CreaturePose(CreatureBody body)
        {
            Body = body;
            _sk = body.Skeleton;
            _look = body.Look ?? new CreatureLook();
            _plan = _look.Plan;
            _n = _sk.Count;
            LocalRotations = new Quaternion[_n];
            LocalPositions = new Vector3[_n];
            _parent = new int[_n];
            _bindLocal = new Vector3[_n];
            _bindModel = new Vector3[_n];
            _rot = new Quaternion[_n];
            _pos = new Vector3[_n];
            _local = new Vector3[_n];
            for (int i = 0; i < _n; i++)
            {
                _parent[i] = _sk.Parents[i];
                _bindLocal[i] = _sk.LocalPositions[i];
                _bindModel[i] = _sk.ModelPosition(i);
                LocalRotations[i] = QMath.Identity;
                LocalPositions[i] = _bindLocal[i];
                _rot[i] = QMath.Identity;
                _pos[i] = _bindModel[i];
                _local[i] = _bindLocal[i];
            }
            _size = Mathf.Max(0.05f, Mathf.Max(body.Height, body.Radius));
            switch (_plan)
            {
                case CreaturePlan.Serpent: InitSerpent(); break;
                case CreaturePlan.Wisp: InitWisp(); break;
                case CreaturePlan.Mite: InitMite(); break;
                default: InitLegged(); break;
            }
        }

        /// <summary>Forget all history (planted feet, trails, springs): after spawning or a teleport.</summary>
        public void Reset(Vector3 rootPosition, float yaw)
        {
            _init = true;
            _prevRoot = rootPosition;
            _prevYaw = yaw;
            _vel = Vector3.zero;
            _speed = _yawRate = _accel = _climb = 0f;
            _hurt = 0f;
            _deathHold = 0f;
            for (int i = 0; i < _n; i++)
            {
                LocalRotations[i] = QMath.Identity;
                LocalPositions[i] = _bindLocal[i];
                _rot[i] = QMath.Identity;
                _pos[i] = _bindModel[i];
                _local[i] = _bindLocal[i];
            }
            switch (_plan)
            {
                case CreaturePlan.Serpent: ResetSerpent(rootPosition, yaw); break;
                case CreaturePlan.Wisp: ResetWisp(); break;
                case CreaturePlan.Mite: ResetMite(); break;
                default: ResetLegged(rootPosition, yaw); break;
            }
        }

        public void Solve(ref CreaturePoseInput input, float dt)
        {
            FootPlantMask = 0;
            if (float.IsNaN(dt) || dt <= 0f) dt = 1f / 60f;
            dt = Mathf.Clamp(dt, 0.0005f, 0.1f);
            Sanitize(ref input);
            if (!_init) Reset(input.RootPosition, input.Yaw);
            // A teleport (spawn, warp, respawn) must not drag planted feet or the trail across the map.
            Vector3 jump = input.RootPosition - _prevRoot;
            if (jump.sqrMagnitude > Sq(Mathf.Max(4f, _size * 3f))) Reset(input.RootPosition, input.Yaw);
            _clock += dt;
            Quaternion invYaw = QMath.AY(-input.Yaw);
            Vector3 vWorld = new Vector3(input.Velocity.x, 0f, input.Velocity.z);
            Vector3 vRoot = invYaw * vWorld;
            Vector3 prevVel = _vel;
            _vel = Vector3.Lerp(_vel, vRoot, QMath.Damp(14f, dt));
            _speed = _vel.magnitude;
            float yawDelta = Mathf.DeltaAngle(_prevYaw, input.Yaw);
            float rate = Mathf.Abs(input.YawRate) > 1e-3f ? input.YawRate : yawDelta / dt;
            _yawRate = Mathf.Lerp(_yawRate, Mathf.Clamp(rate, -540f, 540f), QMath.Damp(8f, dt));
            _accel = Mathf.Lerp(_accel, Mathf.Clamp((_vel.z - prevVel.z) / dt, -30f, 30f), QMath.Damp(6f, dt));
            _climb = Mathf.Lerp(_climb, Mathf.Clamp(input.Velocity.y, -40f, 40f), QMath.Damp(6f, dt));
            _alert = Mathf.MoveTowards(_alert, Mathf.Clamp01(input.Alert), dt * 2.5f);
            float hurtIn = Mathf.Clamp01(input.Hurt);
            if (hurtIn > _hurt) _hurt = hurtIn;
            else _hurt = Mathf.MoveTowards(_hurt, hurtIn, dt * 4f);
            if (input.HurtDirection.sqrMagnitude > 1e-6f)
            {
                Vector3 hd = invYaw * input.HurtDirection;
                hd.y = 0f;
                if (hd.sqrMagnitude > 1e-6f) _hurtDir = Nrm(hd);
            }
            _hasLook = input.HasLookTarget;
            if (_hasLook)
            {
                Vector3 d = invYaw * (input.LookTarget - input.RootPosition);
                if (d.sqrMagnitude > 1e-6f) _lookDir = d;
                else _hasLook = false;
            }
            switch (_plan)
            {
                case CreaturePlan.Serpent: SolveSerpent(ref input, dt); break;
                case CreaturePlan.Wisp: SolveWisp(ref input, dt); break;
                case CreaturePlan.Mite: SolveMite(ref input, dt); break;
                default: SolveLegged(ref input, dt); break;
            }
            WriteLocals();
            _prevRoot = input.RootPosition;
            _prevYaw = input.Yaw;
        }

        // ================================================================== input hygiene

        /// <summary>Replaces NaN and infinite input values (and absurd magnitudes) so no frame can poison the pose.</summary>
        private static void Sanitize(ref CreaturePoseInput p)
        {
            p.RootPosition = Fin(p.RootPosition, 1e6f);
            p.Velocity = Fin(p.Velocity, 200f);
            p.HurtDirection = Fin(p.HurtDirection, 1e3f);
            p.LookTarget = Fin(p.LookTarget, 1e6f);
            p.Yaw = Fin(p.Yaw, 1e6f);
            p.Yaw = Mathf.Repeat(p.Yaw, 360f);
            p.YawRate = Mathf.Clamp(Fin(p.YawRate, 1e4f), -2000f, 2000f);
            p.Altitude = Mathf.Clamp(Fin(p.Altitude, 1e5f), -100f, 1e4f);
            p.Depth = Mathf.Clamp(Fin(p.Depth, 1e4f), 0f, 100f);
            p.ActionT = Fin(p.ActionT, 1e3f);
            if (p.ActionT > 1f && p.Action != CreatureAction.Death) p.ActionT = 1f;
            p.Windup = Mathf.Clamp(Fin(p.Windup, 1f), 0.02f, 0.95f);
            p.Strike = Mathf.Clamp(Fin(p.Strike, 1f), p.Windup + 0.01f, 0.99f);
            p.Alert = Fin(p.Alert, 10f);
            p.Hurt = Fin(p.Hurt, 10f);
            p.Time = Fin(p.Time, 1e7f);
        }

        private static float Fin(float v, float lim)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
            return v > lim ? lim : (v < -lim ? -lim : v);
        }

        private static Vector3 Fin(Vector3 v, float lim) { return new Vector3(Fin(v.x, lim), Fin(v.y, lim), Fin(v.z, lim)); }

        // ================================================================== math helpers

        private static float Sq(float x) { return x * x; }
        private static float E3(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }
        /// <summary>Fast start, soft landing (strikes).</summary>
        private static float EOut(float u) { u = 1f - Mathf.Clamp01(u); return 1f - u * u * u; }
        private static float SS(float a, float b, float x) { return E3((x - a) / (b - a)); }
        private static float Frac(float x) { return x - Mathf.Floor(x); }

        private static Vector3 Nrm(Vector3 v)
        {
            float m = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return m > 1e-9f ? v / m : Vector3.zero;
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>A rotation frame with +Z along dir and +X toward xHint (re-orthogonalised).</summary>
        private static Quaternion Frame(Vector3 dir, Vector3 xHint)
        {
            Vector3 z = Nrm(dir);
            if (z.sqrMagnitude < 0.5f) return QMath.Identity;
            Vector3 x = xHint - z * Vector3.Dot(xHint, z);
            if (x.sqrMagnitude < 1e-10f)
            {
                x = Vector3.Cross(Vector3.up, z);
                if (x.sqrMagnitude < 1e-10f) x = Vector3.Cross(Vector3.forward, z);
            }
            x = Nrm(x);
            Vector3 y = Vector3.Cross(z, x);
            return QMath.FromBasis(x, y, z);
        }

        /// <summary>Two-bone IK: the middle joint for a chain root -> joint -> end reaching for target, bending toward pole.</summary>
        private static Vector3 SolveJoint(Vector3 root, Vector3 target, float l1, float l2, Vector3 pole, out Vector3 end)
        {
            Vector3 d = target - root;
            float dist = d.magnitude;
            Vector3 dir = dist > 1e-6f ? d / dist : Vector3.down;
            float reach = Mathf.Clamp(dist, Mathf.Abs(l1 - l2) + 1e-3f, (l1 + l2) * 0.9995f);
            end = root + dir * reach;
            float cosA = Mathf.Clamp((l1 * l1 + reach * reach - l2 * l2) / (2f * l1 * reach), -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));
            Vector3 p = pole - dir * Vector3.Dot(pole, dir);
            if (p.sqrMagnitude < 1e-10f) p = Vector3.Cross(dir, Vector3.right);
            if (p.sqrMagnitude < 1e-10f) p = Vector3.Cross(dir, Vector3.forward);
            p = Nrm(p);
            return root + (dir * cosA + p * sinA) * l1;
        }

        private static Quaternion Euler(float pitch, float yaw, float roll) { return QMath.AY(yaw) * QMath.AX(pitch) * QMath.AZ(roll); }

        // ================================================================== forward kinematics

        /// <summary>Sets a bone's model rotation and places it from its parent (parents must already be final).</summary>
        private void SetModel(int i, Quaternion modelRot)
        {
            int p = _parent[i];
            _rot[i] = QMath.Normalize(modelRot);
            _pos[i] = p >= 0 ? _pos[p] + _rot[p] * _local[i] : _local[i];
        }

        /// <summary>Sets a bone's rotation relative to its parent (identity keeps the bind orientation).</summary>
        private void SetLocal(int i, Quaternion localRot)
        {
            int p = _parent[i];
            SetModel(i, p >= 0 ? _rot[p] * localRot : localRot);
        }

        private void WriteLocals()
        {
            for (int i = 0; i < _n; i++)
            {
                int p = _parent[i];
                Quaternion q = p >= 0 ? QMath.Inverse(_rot[p]) * _rot[i] : _rot[i];
                q = QMath.Normalize(q);
                if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w)) q = QMath.Identity;
                LocalRotations[i] = q;
                Vector3 l = _local[i];
                if (float.IsNaN(l.x) || float.IsNaN(l.y) || float.IsNaN(l.z)) l = _bindLocal[i];
                LocalPositions[i] = l;
            }
        }

        /// <summary>Model-space position of bone i in the solved pose (for tests and the runtime's effects).</summary>
        public Vector3 ModelPosition(int bone) { return bone >= 0 && bone < _n ? _pos[bone] : Vector3.zero; }

        /// <summary>Model-space rotation of bone i in the solved pose.</summary>
        public Quaternion ModelRotation(int bone) { return bone >= 0 && bone < _n ? _rot[bone] : QMath.Identity; }

        private int FindBone(string name)
        {
            for (int i = 0; i < _n; i++) if (_sk.Names[i] == name) return i;
            return -1;
        }

        private int FirstChild(int bone)
        {
            for (int i = bone + 1; i < _n; i++) if (_parent[i] == bone) return i;
            return -1;
        }

        /// <summary>Ground height (world) at a world position; flat ground at fallback when the input has no probe.</summary>
        private static float Ground(ref CreaturePoseInput p, float x, float z, float fallback)
        {
            if (p.GroundHeight == null) return fallback;
            float g = p.GroundHeight(x, z);
            if (float.IsNaN(g) || float.IsInfinity(g)) return fallback;
            return Mathf.Clamp(g, fallback - 50f, fallback + 50f);
        }

        // ================================================================== action timing

        /// <summary>Phase progress through an action: wind-up (0..1), strike (0..1) and recovery (0..1).</summary>
        private struct Phases
        {
            public float Wind, Hit, Rec, T;
            /// <summary>Overall weight: rises over the first part of the wind-up and falls over the end of the recovery.</summary>
            public float W;
        }

        private static Phases PhasesOf(ref CreaturePoseInput p)
        {
            Phases ph;
            float t = Mathf.Clamp01(p.ActionT);
            float w = p.Windup, s = p.Strike;
            ph.T = t;
            ph.Wind = Mathf.Clamp01(t / w);
            ph.Hit = Mathf.Clamp01((t - w) / Mathf.Max(0.01f, s - w));
            ph.Rec = Mathf.Clamp01((t - s) / Mathf.Max(0.01f, 1f - s));
            ph.W = SS(0f, Mathf.Min(0.12f, w * 0.5f), t) * (1f - SS(0.82f, 1f, t));
            if (p.ActionT < 0f) { ph.W = 0f; ph.Wind = ph.Hit = ph.Rec = 0f; }
            return ph;
        }

        /// <summary>Wind-up key, then strike key, then back: the value of an animated channel through an action.</summary>
        private static float Key3(Phases ph, float wind, float hit, float rest = 0f)
        {
            if (ph.Rec > 0f) return Mathf.Lerp(hit, rest, E3(ph.Rec));
            if (ph.Hit > 0f) return Mathf.Lerp(wind, hit, EOut(ph.Hit));
            return Mathf.Lerp(rest, wind, E3(ph.Wind));
        }

        private static Vector3 Key3(Phases ph, Vector3 wind, Vector3 hit, Vector3 rest)
        {
            if (ph.Rec > 0f) return Vector3.Lerp(hit, rest, E3(ph.Rec));
            if (ph.Hit > 0f) return Vector3.Lerp(wind, hit, EOut(ph.Hit));
            return Vector3.Lerp(rest, wind, E3(ph.Wind));
        }
    }
}
