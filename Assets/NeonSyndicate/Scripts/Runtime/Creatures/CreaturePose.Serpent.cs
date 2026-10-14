using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The wyrm. Its transform marks the head; the head weaves gently from side to side as it travels, and that path is
    /// recorded as a trail in the world (one sample every quarter segment). Every segment sits on the trail at its own
    /// distance behind the head, so the body slides through the exact curves the head carved, like a real serpent. The
    /// trail is three-dimensional: Depth (and the burrow plunge) sink the head below the surface and the body follows it
    /// down, segments posed underground; at shallow depth the path rolls up and down so arches of the body break the
    /// sand. Rearing (bites, roars, the eruption) lifts the front of the body off the trail on a smooth curve from a
    /// point on the ground to the raised head, pulling the rest of the body forward along the trail by the length the
    /// curve uses. The maw's five petals open about their hinges.
    /// FootPlantMask bit 0 marks the frame a bite, slam, eruption or plunge strikes the ground (dust and camera shake).
    /// </summary>
    public sealed partial class CreaturePose
    {
        private int[] _wSegs = new int[0];
        private int[] _wPetals = new int[0];
        private Vector3[] _wPetalAxis = new Vector3[0];
        private int _wHeadBone = -1;
        private float _wSegLen = 0.8f, _wR = 0.7f, _wLen = 14f, _wDs = 0.2f;
        // Trail ring buffer (world positions of the head's centre path), newest at _wNewest.
        private Vector3[] _wTrail = new Vector3[0];
        private int _wNewest, _wCount;
        private float _wTravel, _wWeave, _wOpen, _wLift, _wReach, _wPitch, _wYaw, _wShake, _wDepth;
        private Vector3[] _wPos = new Vector3[0];
        private Vector3[] _wCurve = new Vector3[17];
        private float[] _wCurveS = new float[17];
        private float _wPrevPhase = -1f;
        private bool _wFresh;

        private void InitSerpent()
        {
            _root = _sk.Root;
            _wHeadBone = _sk.Head;
            int n = _sk.Spine != null ? _sk.Spine.Length : 0;
            _wSegs = new int[n];
            for (int i = 0; i < n; i++) _wSegs[i] = _sk.Spine[i];
            int petals = 0;
            for (int i = 0; i < _n; i++) if (_parent[i] == _wHeadBone && _sk.Names[i].StartsWith("Maw", StringComparison.Ordinal)) petals++;
            _wPetals = new int[petals];
            _wPetalAxis = new Vector3[petals];
            int k = 0;
            Vector3 headBind = _wHeadBone >= 0 ? _bindModel[_wHeadBone] : Vector3.zero;
            for (int i = 0; i < _n; i++)
            {
                if (_parent[i] != _wHeadBone || !_sk.Names[i].StartsWith("Maw", StringComparison.Ordinal)) continue;
                _wPetals[k] = i;
                // Each petal opens by turning its tip from forward toward its own outward direction.
                Vector3 radial = _bindModel[i] - headBind;
                radial.z = 0f;
                radial = radial.sqrMagnitude > 1e-8f ? Nrm(radial) : Vector3.up;
                Vector3 axis = Vector3.Cross(Vector3.forward, radial);
                _wPetalAxis[k] = axis.sqrMagnitude > 1e-8f ? Nrm(axis) : Vector3.right;
                k++;
            }
            _wR = headBind.y > 0.01f ? headBind.y : 0.7f;
            _wSegLen = n > 1 ? Mathf.Max(0.05f, _bindModel[_wSegs[0]].z - _bindModel[_wSegs[1]].z) : 0.8f;
            if (n > 0) _wSegLen = Mathf.Max(0.05f, -_bindModel[_wSegs[0]].z);
            _wLen = n * _wSegLen + _wR;
            _wDs = _wSegLen * 0.25f;
            int cap = Mathf.CeilToInt(_wLen * 1.9f / _wDs) + 8;
            _wTrail = new Vector3[cap];
            _wPos = new Vector3[n + 1];
        }

        private void ResetSerpent(Vector3 root, float yaw)
        {
            Quaternion ry = QMath.AY(yaw);
            Vector3 head = root + Vector3.up * _wR;
            Vector3 back = ry * Vector3.back;
            _wCount = Mathf.Min(_wTrail.Length, Mathf.CeilToInt(_wLen * 1.8f / _wDs) + 2);
            // Oldest first, ending at the head: a straight body lying behind the head.
            for (int i = 0; i < _wCount; i++) _wTrail[i] = head + back * (_wDs * (_wCount - 1 - i));
            _wNewest = _wCount - 1;
            _wTravel = 0f;
            _wWeave = 0f;
            _wOpen = 0f;
            _wLift = _wReach = _wPitch = _wYaw = _wShake = 0f;
            _wDepth = 0f;
            _wPrevPhase = -1f;
            _wFresh = true;
        }

        /// <summary>Trail sample i steps back from the newest (0 = newest), clamped to the oldest.</summary>
        private Vector3 TrailAt(int back)
        {
            if (_wCount == 0) return Vector3.zero;
            back = Mathf.Clamp(back, 0, _wCount - 1);
            int idx = _wNewest - back;
            if (idx < 0) idx += _wTrail.Length;
            return _wTrail[idx];
        }

        /// <summary>World point on the trail at arc distance s behind the head point.</summary>
        private Vector3 TrailPoint(Vector3 head, float s)
        {
            Vector3 first = TrailAt(0);
            float d0 = (head - first).magnitude;
            if (s <= d0) return d0 > 1e-6f ? Vector3.Lerp(head, first, s / d0) : head;
            float x = (s - d0) / _wDs;
            int i = Mathf.FloorToInt(x);
            if (i >= _wCount - 1)
            {
                // Past the oldest sample: carry on in a straight line.
                Vector3 a = TrailAt(_wCount - 2), b = TrailAt(_wCount - 1);
                Vector3 dir = b - a;
                return b + (dir.sqrMagnitude > 1e-10f ? Nrm(dir) : Vector3.back) * ((x - (_wCount - 1)) * _wDs);
            }
            return Vector3.Lerp(TrailAt(i), TrailAt(i + 1), x - i);
        }

        private void PushTrail(Vector3 head)
        {
            Vector3 last = TrailAt(0);
            Vector3 d = head - last;
            float dist = d.magnitude;
            int guard = 0;
            while (dist >= _wDs && guard++ < 512)
            {
                last += d / dist * _wDs;
                _wNewest = (_wNewest + 1) % _wTrail.Length;
                _wTrail[_wNewest] = last;
                if (_wCount < _wTrail.Length) _wCount++;
                d = head - last;
                dist = d.magnitude;
            }
        }

        private void SolveSerpent(ref CreaturePoseInput p, float dt)
        {
            int n = _wSegs.Length;
            float L = _wLen, R = _wR;
            Quaternion ry = QMath.AY(p.Yaw), invY = QMath.AY(-p.Yaw);
            bool dead = p.Action == CreatureAction.Death && p.ActionT >= 0f;
            float speed = _speed;
            // ---- actions: lift (head above the trail), reach (forward), head pitch and yaw, maw opening, plunge depth
            float lift = 0f, reach = 0f, pitch = 0f, yaw = 0f, open = 0.06f + 0.04f * Mathf.Sin(_clock * 1.3f), plunge = 0f, shake = 0f, tailSwing = 0f;
            float idleRise = 0.04f * R * Mathf.Sin(_clock * 0.9f);
            lift += idleRise + _alert * 0.06f * L;
            open = Mathf.Max(open, _alert * 0.2f);
            int impact = 0;
            if (p.ActionT >= 0f && p.Action != CreatureAction.None)
            {
                Phases ph = PhasesOf(ref p);
                float W = ph.W;
                switch (p.Action)
                {
                    case CreatureAction.Bite:
                    case CreatureAction.Pounce:
                    case CreatureAction.Claw:
                    {
                        float big = p.Action == CreatureAction.Pounce ? 1.4f : 1f;
                        lift += W * Key3(ph, 0.2f * L, 0.02f * L) * big;
                        reach += W * Key3(ph, -0.06f * L, 0.2f * L) * big;
                        pitch += W * Key3(ph, -24f, 30f);
                        open = Mathf.Max(open, W * Key3(ph, 0.65f, ph.Rec > 0f ? 0.1f : 1f, 0.06f));
                        if (ph.Hit >= 1f && ph.Rec <= 0.02f) impact = 1;
                        break;
                    }
                    case CreatureAction.Roar:
                    case CreatureAction.Howl:
                    case CreatureAction.Breath:
                    case CreatureAction.Spit:
                    {
                        lift += W * Key3(ph, 0.24f * L, 0.3f * L);
                        reach += W * Key3(ph, -0.04f * L, 0.03f * L);
                        pitch += W * Key3(ph, -30f, p.Action == CreatureAction.Roar || p.Action == CreatureAction.Howl ? -12f : 10f);
                        open = Mathf.Max(open, W * Key3(ph, 0.5f, 1f, 0.06f));
                        if (ph.Hit > 0f && ph.Rec < 1f) shake = W * (1f - ph.Rec) * (p.Action == CreatureAction.Roar ? 1f : 0.3f);
                        break;
                    }
                    case CreatureAction.Slam:
                    case CreatureAction.Charge:
                    {
                        lift += W * Key3(ph, 0.28f * L, -0.03f * L);
                        reach += W * Key3(ph, 0f, 0.16f * L);
                        pitch += W * Key3(ph, -20f, 40f);
                        open = Mathf.Max(open, W * Key3(ph, 0.3f, 0.2f));
                        if (ph.Hit >= 1f && ph.Rec <= 0.02f) impact = 1;
                        break;
                    }
                    case CreatureAction.Erupt:
                    {
                        // From underground: the head bursts up out of the sand and arcs forward to the surface.
                        float up = ph.Rec > 0f ? Mathf.Lerp(0.36f * L, 0f, E3(ph.Rec)) : (ph.Hit > 0f ? Mathf.Lerp(-2f * R, 0.36f * L, EOut(ph.Hit)) : Mathf.Lerp(0f, -2f * R, E3(ph.Wind)));
                        lift += up;
                        reach += ph.Rec * 0.18f * L * (1f - ph.Rec * 0.5f);
                        pitch += ph.Hit > 0f && ph.Rec <= 0f ? -55f * (1f - ph.Hit * 0.3f) : Mathf.Lerp(-38f, 20f, ph.Rec);
                        open = Mathf.Max(open, ph.Hit > 0f ? 1f - 0.8f * ph.Rec : 0.2f);
                        if (ph.Hit > 0.05f && ph.Hit < 0.2f) impact = 1;
                        break;
                    }
                    case CreatureAction.Burrow:
                    {
                        // Rear up, then dive head first; the plunge sinks the trail so the body pours in after the head.
                        lift += W * Key3(ph, 0.18f * L, -0.05f * L);
                        reach += W * Key3(ph, -0.02f * L, 0.12f * L);
                        pitch += W * Key3(ph, -25f, 70f);
                        plunge = ph.Hit > 0f ? 3.2f * R * E3(ph.Hit) : 0f;
                        if (ph.Rec > 0f) plunge = 3.2f * R;
                        open = Mathf.Max(open, W * 0.3f);
                        if (ph.Hit > 0.3f && ph.Hit < 0.45f) impact = 1;
                        break;
                    }
                    case CreatureAction.TailSwipe:
                    {
                        tailSwing = W * Key3(ph, -0.8f, 1f);
                        yaw += W * Key3(ph, 15f, -20f);
                        break;
                    }
                    case CreatureAction.Stagger:
                    {
                        lift += W * 0.05f * L * Mathf.Sin(ph.T * Mathf.PI);
                        yaw += W * 28f * Mathf.Sin(ph.T * 18f) * (1f - ph.T);
                        pitch += W * 15f * Mathf.Sin(ph.T * 11f);
                        open = Mathf.Max(open, W * 0.5f);
                        break;
                    }
                    case CreatureAction.Death:
                    {
                        float t = Mathf.Clamp01(p.ActionT);
                        // A last rise and shudder, then the body crashes down and lies still, the maw slack.
                        lift += t < 0.35f ? 0.22f * L * E3(t / 0.35f) : 0.22f * L * (1f - E3((t - 0.35f) / 0.4f)) - 0.02f * L * E3((t - 0.6f) / 0.4f);
                        pitch += t < 0.35f ? -35f * E3(t / 0.35f) : Mathf.Lerp(-35f, 18f, E3((t - 0.35f) / 0.4f));
                        yaw += 40f * E3((t - 0.4f) / 0.5f);
                        open = 0.55f + 0.2f * Mathf.Sin(t * 30f) * (1f - t);
                        if (t > 0.72f && t < 0.76f) impact = 1;
                        break;
                    }
                }
            }
            if (impact != 0 && _wPrevPhase >= 0f) FootPlantMask |= 1;
            _wPrevPhase = p.ActionT;
            if (_hurt > 0.001f)
            {
                Vector3 hd = _hurtDir;
                yaw += -hd.x * 22f * _hurt;
                lift += 0.03f * L * _hurt;
                pitch += -10f * _hurt;
            }
            // ---- the head's path: weave with travel, depth (with the plunge), arches at shallow depth
            _wTravel += speed * dt;
            float weaveTarget = R * 0.75f * SS(0.15f, 2.5f, speed) * (dead ? 0f : 1f);
            _wWeave = Mathf.Lerp(_wWeave, weaveTarget, QMath.Damp(1.5f, dt));
            float lambda = 0.42f * L;
            float phase = _wTravel / lambda * Mathf.PI * 2f;
            float depthIn = dead ? 0f : p.Depth;
            float depth = depthIn + plunge;
            _wDepth = Mathf.Lerp(_wDepth, depth, QMath.Damp(dead ? 1.2f : 6f, dt));
            float arch = 0f;
            if (_wDepth > 0.2f * R && speed > 0.2f) arch = 1.1f * R * Mathf.Sin(_wTravel / (0.5f * L) * Mathf.PI * 2f) * SS(0.2f * R, 1.2f * R, _wDepth) * (1f - SS(4f * R, 7f * R, _wDepth));
            if (_wFresh)
            {
                // Spawned or teleported below the surface: the whole body starts down there with the head.
                _wDepth = depth;
                for (int i = 0; i < _wCount; i++) _wTrail[i].y -= depth;
                _wFresh = false;
            }
            Vector3 groundHead = p.RootPosition;
            Vector3 headPath = groundHead + ry * new Vector3(_wWeave * Mathf.Sin(phase), R - _wDepth + arch, 0f);
            PushTrail(headPath);
            // ---- smoothing the action channels a little keeps blends between actions soft
            float k = QMath.Damp(18f, dt);
            _wLift = Mathf.Lerp(_wLift, lift, k);
            _wReach = Mathf.Lerp(_wReach, reach, k);
            _wPitch = Mathf.Lerp(_wPitch, pitch, k);
            _wYaw = Mathf.Lerp(_wYaw, yaw, k);
            _wShake = Mathf.Lerp(_wShake, shake, QMath.Damp(8f, dt));
            _wOpen = Mathf.Lerp(_wOpen, Mathf.Clamp01(open), QMath.Damp(open > _wOpen ? 22f : 10f, dt));
            // ---- head direction: along the trail, turned toward the look target
            Vector3 tangent = headPath - TrailPoint(headPath, _wSegLen);
            if (tangent.sqrMagnitude < 1e-8f) tangent = ry * Vector3.forward;
            Vector3 flatT = Flat(tangent);
            float trailYaw = flatT.sqrMagnitude > 1e-8f ? Mathf.Atan2(flatT.x, flatT.z) * Mathf.Rad2Deg : p.Yaw;
            float lookYaw = 0f;
            if (_hasLook)
            {
                Vector3 ld = _lookDir;
                float want = Mathf.Atan2(ld.x, ld.z) * Mathf.Rad2Deg;
                lookYaw = Mathf.Clamp(Mathf.DeltaAngle(0f, want), -50f, 50f) * (dead ? 0f : 1f);
            }
            _lookYaw = Mathf.Lerp(_lookYaw, lookYaw, QMath.Damp(4f, dt));
            float shakeYaw = _wShake * 14f * Mathf.Sin(_clock * 26f);
            float headYaw = trailYaw + Mathf.DeltaAngle(trailYaw, p.Yaw) * 0.3f + _lookYaw * (0.5f + 0.5f * SS(0f, 0.15f * L, _wLift)) + _wYaw + shakeYaw;
            float flatPitch = -Mathf.Atan2(tangent.y, Mathf.Max(1e-4f, flatT.magnitude)) * Mathf.Rad2Deg;
            float headPitch = Mathf.Lerp(flatPitch, 0f, SS(0f, 0.1f * L, _wLift)) + _wPitch;
            Quaternion headRotW = QMath.AY(headYaw) * QMath.AX(Mathf.Clamp(headPitch, -85f, 85f));
            Vector3 headFwdW = headRotW * Vector3.forward;
            // ---- the raised front: a curve from an anchor on the trail up to the head
            Vector3 headW = headPath + ry * new Vector3(0f, _wLift, _wReach);
            // On the surface the head never digs into the ground (it does when it means to: burrowing or below ground).
            if (_wDepth < 0.05f * R && plunge <= 0f && p.Action != CreatureAction.Erupt)
            {
                float gy = Ground(ref p, headW.x, headW.z, p.RootPosition.y) + 0.72f * R;
                if (headW.y < gy) headW.y = gy;
            }
            float rise = Mathf.Abs(_wLift) + Mathf.Abs(_wReach);
            int nc = _wCurve.Length;
            float curveLen = 0f;
            float anchorS = 0f;
            if (rise > 0.01f * R)
            {
                anchorS = Mathf.Clamp(1.5f * Mathf.Abs(_wLift) + 0.8f * Mathf.Abs(_wReach) + _wSegLen, _wSegLen, 0.7f * L);
                Vector3 A = TrailPoint(headPath, anchorS);
                Vector3 A2 = TrailPoint(headPath, anchorS + _wSegLen);
                Vector3 t0 = A - A2;
                t0 = t0.sqrMagnitude > 1e-8f ? Nrm(t0) : headFwdW;
                float chord = (headW - A).magnitude;
                Vector3 m0 = t0 * (chord * 1.1f), m1 = headFwdW * (chord * 0.9f);
                // Sampled from the head (u = 0) back to the anchor (u = 1).
                for (int i = 0; i < nc; i++)
                {
                    float u = 1f - (float)i / (nc - 1);
                    float u2 = u * u, u3 = u2 * u;
                    float h00 = 2f * u3 - 3f * u2 + 1f, h10 = u3 - 2f * u2 + u, h01 = -2f * u3 + 3f * u2, h11 = u3 - u2;
                    _wCurve[i] = A * h00 + m0 * h10 + headW * h01 + m1 * h11;
                    _wCurveS[i] = i == 0 ? 0f : _wCurveS[i - 1] + (_wCurve[i] - _wCurve[i - 1]).magnitude;
                }
                curveLen = _wCurveS[nc - 1];
            }
            // ---- segments: on the curve while it lasts, then on the trail
            _wPos[0] = headW;
            float tailStart = n * 0.6f;
            for (int i = 1; i <= n; i++)
            {
                float b = i * _wSegLen;
                Vector3 w;
                if (b < curveLen)
                {
                    int j = 1;
                    while (j < nc - 1 && _wCurveS[j] < b) j++;
                    float s0 = _wCurveS[j - 1], s1 = _wCurveS[j];
                    w = Vector3.Lerp(_wCurve[j - 1], _wCurve[j], s1 > s0 ? (b - s0) / (s1 - s0) : 0f);
                }
                else w = TrailPoint(headPath, anchorS + (b - curveLen));
                // A tail swipe sweeps the back third sideways.
                if (tailSwing != 0f && i > tailStart)
                {
                    float f = (i - tailStart) / (n - tailStart);
                    w += ry * new Vector3(tailSwing * 0.18f * L * f * f, 0f, 0f);
                }
                // Lying dead the body settles onto the ground in a loose curl.
                if (dead)
                {
                    float settle = E3((p.ActionT - 0.5f) / 0.5f);
                    float gy = Ground(ref p, w.x, w.z, p.RootPosition.y) + R * 0.92f;
                    w.y = Mathf.Lerp(w.y, gy, settle * SS(0f, 0.3f * L, b));
                    w += ry * new Vector3(Mathf.Sin(b / L * 5f) * 0.05f * L * settle, 0f, 0f);
                }
                _wPos[i] = w;
            }
            // ---- write the bones (model space of the transform)
            _local[_root] = Vector3.zero;
            SetModel(_root, QMath.Identity);
            if (_wHeadBone >= 0)
            {
                _local[_wHeadBone] = invY * (headW - p.RootPosition);
                SetModel(_wHeadBone, invY * headRotW);
            }
            for (int i = 0; i < _wPetals.Length; i++) SetLocal(_wPetals[i], QMath.AngleAxis(_wOpen * 98f * (0.92f + 0.08f * Mathf.Sin(i * 2.1f + _clock * 3f)), _wPetalAxis[i]));
            Vector3 prevUp = Vector3.up;
            for (int i = 1; i <= n; i++)
            {
                int bone = _wSegs[i - 1];
                Vector3 fwd = _wPos[i - 1] - _wPos[i];
                if (i < n) fwd = Vector3.Lerp(fwd, _wPos[i] - _wPos[i + 1], 0.35f);
                Vector3 f = invY * fwd;
                Vector3 up = Mathf.Abs(Vector3.Dot(Nrm(f), Vector3.up)) > 0.96f ? prevUp : Vector3.up;
                Quaternion q = f.sqrMagnitude > 1e-10f ? QMath.LookRotation(f, up) : QMath.Identity;
                prevUp = q * Vector3.up;
                _local[bone] = invY * (_wPos[i] - p.RootPosition);
                SetModel(bone, q);
            }
        }
    }
}
