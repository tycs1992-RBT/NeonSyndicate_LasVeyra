using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The shade and the mite. The shade's core floats and bobs, leans into its motion and turns to look; each tendril is
    /// a chain of particles in the world, pulled toward its rest shape (stiff at the root, loose at the tip) and kept at
    /// its bone lengths, so tendrils trail behind when it moves, swing out on turns and whip after a lash. Death drops
    /// the core and lets the tendrils fall slack across the ground. The mite hovers with buzzing wings or skitters with a
    /// quick bob, lunges to bite and flips onto its back when it dies.
    /// </summary>
    public sealed partial class CreaturePose
    {
        // ---- shade
        private int _hCore = -1;
        private int[][] _hBones = new int[0][];
        private Vector3[][] _hBindDir = new Vector3[0][];
        private float[][] _hLen = new float[0][];
        private Vector3[][] _hX = new Vector3[0][], _hXP = new Vector3[0][];
        private Vector3[] _hRest = new Vector3[8];
        private float _hRc = 0.3f, _hSize = 1.2f;
        private float _hPitch, _hRoll, _hYaw;
        private Vector3 _hOff;
        private bool _hInit;
        private int _hLashA = -1, _hLashB = -1;

        private void InitWisp()
        {
            _root = _sk.Root;
            _hCore = _sk.Head >= 0 ? _sk.Head : (_sk.Chest >= 0 ? _sk.Chest : _sk.Root);
            int nt = _sk.Tendrils != null ? _sk.Tendrils.Length : 0;
            _hBones = new int[nt][];
            _hBindDir = new Vector3[nt][];
            _hLen = new float[nt][];
            _hX = new Vector3[nt][];
            _hXP = new Vector3[nt][];
            float bestA = -9f, bestB = -9f;
            float size = 0f;
            for (int k = 0; k < nt; k++)
            {
                // Follow the chain from its first bone through each bone's only child.
                int count = 0;
                for (int b = _sk.Tendrils[k]; b >= 0; b = FirstChild(b)) count++;
                int[] chain = new int[count];
                int i = 0;
                for (int b = _sk.Tendrils[k]; b >= 0 && i < count; b = FirstChild(b)) chain[i++] = b;
                _hBones[k] = chain;
                _hBindDir[k] = new Vector3[count];
                _hLen[k] = new float[count];
                for (int j = 0; j < count; j++)
                {
                    Vector3 d = j + 1 < count ? _bindModel[chain[j + 1]] - _bindModel[chain[j]] : _bindModel[chain[j]] - _bindModel[chain[Mathf.Max(0, j - 1)]];
                    float len = d.magnitude;
                    if (j + 1 >= count) len *= 0.9f;
                    _hLen[k][j] = Mathf.Max(1e-4f, len);
                    _hBindDir[k][j] = len > 1e-6f ? d / d.magnitude : Vector3.down;
                    size += len;
                }
                _hX[k] = new Vector3[count + 1];
                _hXP[k] = new Vector3[count + 1];
                // The two tendrils hanging furthest forward do the lashing.
                float z = _bindModel[chain[count - 1]].z;
                if (z > bestA) { bestB = bestA; _hLashB = _hLashA; bestA = z; _hLashA = k; }
                else if (z > bestB) { bestB = z; _hLashB = k; }
            }
            _hSize = Mathf.Max(0.2f, nt > 0 ? size / nt : 1f);
            _hRc = Mathf.Max(0.05f, _bindModel[_hCore].y * 0.2f);
            if (_hRest.Length < 16) _hRest = new Vector3[16];
            _hInit = false;
        }

        private void ResetWisp()
        {
            _hInit = false;
            _hPitch = _hRoll = _hYaw = 0f;
            _hOff = Vector3.zero;
        }

        private void SolveWisp(ref CreaturePoseInput p, float dt)
        {
            Quaternion ry = QMath.AY(p.Yaw), invY = QMath.AY(-p.Yaw);
            bool dead = p.Action == CreatureAction.Death && p.ActionT >= 0f;
            float size = _hSize;
            float hc = _bindModel[_hCore].y;
            // ---- core motion
            float bob = Mathf.Sin(_clock * 1.7f) * 0.045f * size + Mathf.Sin(_clock * 0.63f + 1f) * 0.02f * size;
            Vector3 off = new Vector3(Mathf.Sin(_clock * 0.9f) * 0.015f * size, bob + _alert * 0.06f * size, 0f);
            float pitch = Mathf.Clamp(_vel.z * 7f, -12f, 26f) + Mathf.Sin(_clock * 1.1f) * 2f;
            float roll = Mathf.Clamp(-_yawRate * 0.05f - _vel.x * 6f, -22f, 22f);
            float yaw = 0f;
            float spread = 1f + 0.3f * _alert, lift = 0f, swing = 0f, stiffBoost = 0f, gravity = 0.25f;
            Vector3 lashTarget = Vector3.zero;
            float lashW = 0f;
            Vector3 lashShape = Vector3.zero;
            if (_hasLook && !dead)
            {
                float want = Mathf.Atan2(_lookDir.x, _lookDir.z) * Mathf.Rad2Deg;
                yaw = Mathf.Clamp(want, -70f, 70f);
            }
            if (p.ActionT >= 0f && p.Action != CreatureAction.None)
            {
                Phases ph = PhasesOf(ref p);
                float W = ph.W;
                switch (p.Action)
                {
                    case CreatureAction.Claw:
                    case CreatureAction.TailSwipe:
                    {
                        // Tendril lash: the front tendrils coil up behind the core, then crack forward at the target.
                        lashW = W;
                        lashShape = Key3(ph, new Vector3(0.15f, 0.75f, -0.45f), new Vector3(0.05f, -0.15f, 1.15f), Vector3.zero);
                        stiffBoost = 22f * W;
                        pitch += W * Key3(ph, -12f, 14f);
                        off.z += W * Key3(ph, -0.05f, 0.1f) * size;
                        if (p.Action == CreatureAction.TailSwipe) swing = W * Key3(ph, -40f, 70f);
                        break;
                    }
                    case CreatureAction.Spit:
                    case CreatureAction.Breath:
                    case CreatureAction.Bite:
                    case CreatureAction.Pounce:
                    case CreatureAction.Charge:
                    case CreatureAction.Dive:
                    {
                        float lunge = p.Action == CreatureAction.Spit || p.Action == CreatureAction.Breath ? 0.16f : 0.42f;
                        off.z += W * Key3(ph, -0.12f, lunge) * size;
                        off.y += W * Key3(ph, 0.06f, -0.03f) * size;
                        pitch += W * Key3(ph, -24f, 20f);
                        spread += W * Key3(ph, 0.2f, 0.7f);
                        break;
                    }
                    case CreatureAction.Roar:
                    case CreatureAction.Howl:
                    case CreatureAction.Slam:
                    {
                        lift = W * Key3(ph, 0.08f, 0.14f) * size;
                        pitch += W * Key3(ph, -18f, -8f) + W * 6f * Mathf.Sin(_clock * 40f) * (ph.Hit > 0f && ph.Rec < 0.8f ? 1f : 0f);
                        spread += W * Key3(ph, 0.4f, 1.1f);
                        if (p.Action == CreatureAction.Slam) off.y += W * Key3(ph, 0.15f, -0.25f) * size;
                        break;
                    }
                    case CreatureAction.Stagger:
                    {
                        pitch += W * 22f * Mathf.Sin(ph.T * 17f);
                        roll += W * 28f * Mathf.Sin(ph.T * 13f + 1f);
                        off.x += W * 0.08f * size * Mathf.Sin(ph.T * 9f);
                        stiffBoost = -3f * W;
                        break;
                    }
                    case CreatureAction.Death:
                    {
                        float t = Mathf.Clamp01(p.ActionT);
                        // Drops out of the air, bounces once and settles on the ground, tipped over.
                        float fall = t < 0.45f ? E3(t / 0.45f) : 1f - 0.08f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.45f) / 0.2f));
                        off = new Vector3(0f, -(hc - _hRc * 0.9f) * fall, 0.05f * size * fall);
                        pitch = 25f * fall;
                        roll = 35f * E3((t - 0.3f) / 0.5f);
                        yaw = 0f;
                        gravity = 4f;
                        break;
                    }
                }
            }
            if (_hurt > 0.001f)
            {
                off += -_hurtDir * (0.12f * size * _hurt);
                pitch += -14f * _hurt;
            }
            off.y += lift;
            float k = QMath.Damp(dead ? 30f : 10f, dt);
            _hOff = Vector3.Lerp(_hOff, off, k);
            _hPitch = Mathf.Lerp(_hPitch, pitch, k);
            _hRoll = Mathf.Lerp(_hRoll, roll, k);
            _hYaw = Mathf.Lerp(_hYaw, yaw, QMath.Damp(5f, dt));
            Vector3 corePos = _bindModel[_hCore] + _hOff;
            Quaternion coreRot = Euler(_hPitch, _hYaw, _hRoll);
            _local[_root] = Vector3.zero;
            SetModel(_root, QMath.Identity);
            _local[_hCore] = corePos;
            SetModel(_hCore, coreRot);
            // ---- tendrils
            Vector3 rootW = p.RootPosition;
            float drag = dead ? 3f : 2.2f;
            for (int t = 0; t < _hBones.Length; t++)
            {
                int[] ch = _hBones[t];
                int m = ch.Length;
                Vector3[] X = _hX[t], XP = _hXP[t];
                bool lashing = lashW > 0.001f && (t == _hLashA || t == _hLashB);
                // Rest shape in the world: the bind chain carried by the core, spread and swung by actions.
                Vector3 anchor = _bindModel[ch[0]] - _bindModel[_hCore];
                Vector3 radialOut = Flat(anchor);
                radialOut = radialOut.sqrMagnitude > 1e-8f ? Nrm(radialOut) : Vector3.forward;
                for (int j = 0; j <= m; j++)
                {
                    Vector3 bind = j < m ? _bindModel[ch[j]] : _bindModel[ch[m - 1]] + _hBindDir[t][m - 1] * _hLen[t][m - 1];
                    Vector3 rel = bind - _bindModel[ch[0]];
                    float u = (float)j / m;
                    rel += radialOut * ((spread - 1f) * 0.35f * size * u);
                    rel.y += (spread - 1f) * 0.12f * size * u;
                    // A slow idle drift so the tendrils are never still.
                    rel += new Vector3(Mathf.Sin(_clock * 0.8f + t * 1.7f + u * 2f), 0f, Mathf.Cos(_clock * 0.7f + t * 2.3f + u * 1.5f)) * (0.05f * size * u);
                    if (swing != 0f) rel = QMath.AY(swing * u) * rel;
                    Vector3 model = corePos + coreRot * anchor + (lashing ? Vector3.Lerp(coreRot * rel, coreRot * (lashShape * (size * u)), lashW) : coreRot * rel);
                    _hRest[j] = rootW + ry * model;
                }
                if (!_hInit)
                {
                    for (int j = 0; j <= m; j++) { X[j] = _hRest[j]; XP[j] = _hRest[j]; }
                }
                X[0] = _hRest[0];
                XP[0] = _hRest[0];
                for (int j = 1; j <= m; j++)
                {
                    float u = (float)j / m;
                    Vector3 v = (X[j] - XP[j]) * Mathf.Max(0f, 1f - drag * dt);
                    XP[j] = X[j];
                    X[j] += v + Vector3.down * (gravity * 9.81f * dt * dt * (dead ? 1f : 0.1f));
                    float stiff = dead ? 0f : Mathf.Max(0f, Mathf.Lerp(16f, 3.5f, u) + (lashing ? stiffBoost : Mathf.Min(0f, stiffBoost)));
                    X[j] = Vector3.Lerp(X[j], _hRest[j], QMath.Damp(stiff, dt));
                }
                for (int iter = 0; iter < 2; iter++)
                    for (int j = 1; j <= m; j++)
                    {
                        Vector3 d = X[j] - X[j - 1];
                        float len = d.magnitude;
                        float want = _hLen[t][j - 1];
                        X[j] = len > 1e-6f ? X[j - 1] + d * (want / len) : X[j - 1] + ry * (coreRot * _hBindDir[t][j - 1]) * want;
                        float g = Ground(ref p, X[j].x, X[j].z, rootW.y) + 0.012f * size;
                        if (X[j].y < g) X[j].y = g;
                    }
                // Bones point from each particle to the next.
                for (int j = 0; j < m; j++)
                {
                    Vector3 dir = invY * (X[j + 1] - X[j]);
                    Quaternion q = dir.sqrMagnitude > 1e-12f ? QMath.FromTo(_hBindDir[t][j], dir) : coreRot;
                    SetModel(ch[j], q);
                }
            }
            _hInit = true;
        }

        // ---- mite
        private int _mBody = -1, _mWingL = -1, _mWingR = -1;
        private float _mLen = 0.09f;
        private Vector3 _mOff;
        private float _mPitch, _mRoll, _mYaw, _mFly;

        private void InitMite()
        {
            _root = _sk.Root;
            _mBody = _sk.Chest >= 0 ? _sk.Chest : (_sk.Head >= 0 ? _sk.Head : _sk.Root);
            _mWingL = FindBone("Wing.L");
            _mWingR = FindBone("Wing.R");
            _mLen = Mathf.Max(0.01f, (_look.BodyLength > 0f ? _look.BodyLength : 0.09f) * Mathf.Max(0.05f, _look.Scale));
        }

        private void ResetMite()
        {
            _mOff = Vector3.zero;
            _mPitch = _mRoll = _mYaw = 0f;
            _mFly = 0f;
        }

        private void SolveMite(ref CreaturePoseInput p, float dt)
        {
            float len = _mLen;
            bool dead = p.Action == CreatureAction.Death && p.ActionT >= 0f;
            _mFly = Mathf.MoveTowards(_mFly, p.Flying && !dead ? 1f : 0f, dt * 4f);
            float t = _clock;
            // Hover: a bob and a nervous jitter. Skitter: a quick step bob that follows the speed.
            float jitter = (Mathf.Sin(t * 23f) * 0.6f + Mathf.Sin(t * 37f + 1f) * 0.4f);
            Vector3 hover = new Vector3(jitter * 0.04f * len, Mathf.Sin(t * 21f) * 0.06f * len + Mathf.Sin(t * 5.3f) * 0.08f * len, Mathf.Sin(t * 29f + 2f) * 0.03f * len);
            float stepRate = 14f + _speed / len * 1.2f;
            float skit = Mathf.Abs(Mathf.Sin(t * stepRate)) * 0.05f * len * SS(0.02f, 0.2f, _speed);
            Vector3 off = Vector3.Lerp(new Vector3(0f, skit, 0f), hover, _mFly);
            float pitch = Mathf.Clamp(_accel * 0.8f, -15f, 15f) + Mathf.Lerp(0f, Mathf.Clamp(_vel.z / len * 0.6f, -10f, 20f), _mFly);
            float roll = Mathf.Clamp(-_yawRate * 0.03f, -25f, 25f) + _mFly * jitter * 4f;
            float yaw = Mathf.Sin(t * 9f) * 3f * SS(0.02f, 0.2f, _speed);
            // A mite turns its whole body toward whatever it is watching.
            if (_hasLook && !dead) yaw += Mathf.Clamp(Mathf.Atan2(_lookDir.x, _lookDir.z) * Mathf.Rad2Deg, -35f, 35f);
            if (p.ActionT >= 0f && p.Action != CreatureAction.None)
            {
                Phases ph = PhasesOf(ref p);
                float W = ph.W;
                switch (p.Action)
                {
                    case CreatureAction.Death:
                    {
                        float d = Mathf.Clamp01(p.ActionT / 0.35f);
                        // Falls and flips onto its back, legs up.
                        off = new Vector3(0f, Mathf.Lerp(off.y, 0.25f * len - _bindModel[_mBody].y + 0.12f * len, d), 0f);
                        roll = 180f * E3(d);
                        pitch = 10f * d;
                        yaw = 0f;
                        break;
                    }
                    case CreatureAction.Stagger:
                        roll += W * 40f * Mathf.Sin(ph.T * 25f);
                        yaw += W * 60f * Mathf.Sin(ph.T * 9f);
                        break;
                    default:
                        // Every attack is a lunge: rear back, dart forward and down, pull back.
                        off.z += W * Key3(ph, -0.15f, 0.6f) * len;
                        off.y += W * Key3(ph, 0.12f, -0.05f) * len;
                        pitch += W * Key3(ph, -25f, 20f);
                        break;
                }
            }
            if (_hurt > 0.001f) { off += -_hurtDir * (0.3f * len * _hurt); roll += 30f * _hurt; }
            float k = QMath.Damp(dead ? 25f : 20f, dt);
            _mOff = Vector3.Lerp(_mOff, off, k);
            _mPitch = Mathf.Lerp(_mPitch, pitch, k);
            _mRoll = Mathf.Lerp(_mRoll, roll, k);
            _mYaw = Mathf.Lerp(_mYaw, yaw, k);
            _local[_root] = Vector3.zero;
            SetModel(_root, QMath.Identity);
            _local[_mBody] = _bindModel[_mBody] + _mOff;
            SetModel(_mBody, Euler(_mPitch, _mYaw, _mRoll));
            // Wings swing out to the sides and buzz in flight, and lie back over the abdomen on the ground.
            float buzz = _mFly * (dead ? 0f : 1f);
            float beat = buzz * (20f + 40f * Mathf.Sin(t * Mathf.PI * 2f * 31f));
            if (_mWingL >= 0) SetLocal(_mWingL, QMath.AZ(-beat) * QMath.AY(65f * buzz));
            if (_mWingR >= 0) SetLocal(_mWingR, QMath.AZ(beat) * QMath.AY(-65f * buzz));
        }
    }
}
