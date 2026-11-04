using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Legged locomotion (quadrupeds, and winged creatures on the ground): gaits chosen by the Froude number (walk, trot,
    /// gallop; lizards trot), paws planted in the world through stance and landed where the body will be at mid-stance,
    /// the body held no higher than the planted legs can reach (so stance feet never skate), spine flex, turn lean, head
    /// stabilisation, look-at, a sprung tail and digitigrade four-bone leg IK.
    /// </summary>
    public sealed partial class CreaturePose
    {
        private sealed class Leg
        {
            public int Girdle = -1, Upper, Lower, Foot, Toe;
            public bool Front, Wing;
            public int Side;
            /// <summary>Gait role: 0 hind left, 1 fore left, 2 hind right, 3 fore right.</summary>
            public int Role;
            public float L1, L2, L3, PawH, Reach;
            public Vector3 Neutral, FootDir, BindHinge, ToeDir, AttachBind;
            public Quaternion InvUpper, InvLower, InvFoot, InvToe, InvGirdle;
            public Vector3 GirdleDir;
            // State (world space unless noted).
            public bool Swing, IdleStep, LandFixed;
            /// <summary>The swing started before the phase's lift-off because the leg ran out of reach.</summary>
            public bool Early;
            public float LiftPsi;
            public float U, PrevPsi = -1f, Since, Remaining, Offset;
            public Vector3 From, Plant, Paw, Land;
            public float Lift;
            // Action override: weight and model-space paw target, plus wrist or hock flex.
            public float OverW, OverFlex;
            public Vector3 Over;
            public bool Released, HasBase;
            /// <summary>Released legs: where the paw rests (model space), easing from where it lifted to under the body.</summary>
            public Vector3 Base;
            /// <summary>Highest the hip or shoulder may be this frame (model space) for this planted paw.</summary>
            public float MaxAttach;
            /// <summary>Where the limb solver wants the paw this frame (model space); the wings' own solver reads it.</summary>
            public Vector3 Target;
        }

        private Leg[] _legRig = new Leg[0];
        private int[] _neck = new int[0], _tail = new int[0];
        private int _root = -1, _pelvis = -1, _spine = -1, _chest = -1, _head = -1, _jaw = -1, _earL = -1, _earR = -1;
        private float _legLen = 0.5f, _trunkLen = 1f, _turnRadius = 0.4f, _rootH = 0.5f;
        /// <summary>How much higher the shoulders ride than at bind (a wyvern stands tall on its wing wrists).</summary>
        private float _frontLift;
        private bool _lizard, _canGallop;
        // Gait.
        private float _phase, _cycle = 0.8f, _duty = 0.68f, _wTrot, _wGallop;
        private bool _moving;
        // Body.
        private float _bodyY, _bodyPitch, _bodyRoll, _bend, _flex, _bodyX;
        private bool _bodyInit;
        // Look and head.
        private float _lookYaw, _lookPitch, _lookW;
        // Tail springs (yaw, pitch per bone).
        private float[] _tY = new float[0], _tP = new float[0], _tYV = new float[0], _tPV = new float[0];
        // Ears.
        private float _earPin, _earTw;
        private float _jawOpen;
        private float _airT;

        // Gait offsets per role (hind left, fore left, hind right, fore right), unwrapped so blends stay continuous.
        private static readonly float[] WalkOff = { 0f, 0.25f, 0.5f, 0.75f };
        private static readonly float[] TrotOff = { 0f, 0.5f, 0.5f, 1f };
        private static readonly float[] GallopOff = { 0f, 0.5f, 0.1f, 0.6f };

        // ================================================================== setup

        private void InitLegged()
        {
            _root = _sk.Root;
            _pelvis = _sk.Pelvis;
            _chest = _sk.Chest;
            _head = _sk.Head;
            _jaw = _sk.Jaw;
            _neck = _sk.Neck ?? new int[0];
            _tail = _sk.Tail ?? new int[0];
            _spine = _sk.Spine != null && _sk.Spine.Length > 1 ? _sk.Spine[1] : -1;
            _earL = FindBone("Ear.L");
            _earR = FindBone("Ear.R");
            _lizard = _look.Sprawl > 0.5f;
            _canGallop = !_lizard;
            int nl = _sk.Legs != null ? _sk.Legs.Length : 0;
            bool wingWalk = _plan == CreaturePlan.Winged && !_look.Forelegs && _sk.Wings != null && _sk.Wings.Length == 2;
            _legRig = new Leg[nl + (wingWalk ? 2 : 0)];
            float sumLen = 0f, sumR = 0f;
            int hind = 0;
            for (int i = 0; i < nl; i++)
            {
                LegChain c = _sk.Legs[i];
                Leg l = new Leg();
                l.Upper = c.Upper; l.Lower = c.Lower; l.Foot = c.Foot; l.Toe = c.Toe;
                l.Front = c.Front;
                l.Side = c.Side;
                int par = _parent[c.Upper];
                if (c.Front && par != _chest && par >= 0) l.Girdle = par;
                SetupLeg(l);
                _legRig[i] = l;
                if (!l.Front) { sumLen += l.AttachBind.y; hind++; }
                sumR += Flat(l.Neutral - _bindModel[_root]).magnitude;
            }
            if (wingWalk)
            {
                for (int k = 0; k < 2; k++)
                {
                    WingChain w = _sk.Wings[k];
                    Leg l = new Leg();
                    l.Wing = true;
                    l.Front = true;
                    l.Side = w.Side;
                    l.Upper = w.Arm; l.Lower = w.Fore; l.Foot = w.Hand; l.Toe = w.Hand;
                    SetupWingLeg(l);
                    _legRig[nl + k] = l;
                    sumR += Flat(l.Neutral - _bindModel[_root]).magnitude;
                }
            }
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                l.Role = (l.Side < 0 ? 0 : 2) + (l.Front ? 1 : 0);
            }
            _legLen = hind > 0 ? sumLen / hind : Mathf.Max(0.2f, _bindModel[_root].y);
            _turnRadius = _legRig.Length > 0 ? sumR / _legRig.Length : 0.3f;
            _rootH = _root >= 0 ? _bindModel[_root].y : _legLen;
            _trunkLen = _chest >= 0 && _pelvis >= 0 ? Mathf.Max(0.2f, _bindModel[_chest].z - _bindModel[_pelvis].z) : _legLen * 2f;
            int nt = _tail.Length;
            _tY = new float[nt]; _tP = new float[nt]; _tYV = new float[nt]; _tPV = new float[nt];
            if (_plan == CreaturePlan.Winged) InitWings();
            _frontLift = wingWalk ? 0.3f * _rootH : 0f;
        }

        private void SetupLeg(Leg l)
        {
            Vector3 a = _bindModel[l.Upper], k = _bindModel[l.Lower], w = _bindModel[l.Foot], p = _bindModel[l.Toe];
            l.L1 = (k - a).magnitude;
            l.L2 = (w - k).magnitude;
            l.L3 = (p - w).magnitude;
            l.PawH = p.y;
            l.Neutral = p;
            l.AttachBind = a;
            l.FootDir = Nrm(w - p);
            Vector3 h = Vector3.Cross(k - a, w - k);
            l.BindHinge = h.sqrMagnitude > 1e-10f ? Nrm(h) : Vector3.right;
            if (Vector3.Dot(l.BindHinge, Vector3.right) < 0f && Mathf.Abs(l.BindHinge.x) > 0.5f) { }
            l.ToeDir = Nrm(new Vector3(l.Side * 0.22f * Mathf.Clamp01(_look.Sprawl), 0f, 1f));
            l.InvUpper = QMath.Inverse(Frame(k - a, l.BindHinge));
            l.InvLower = QMath.Inverse(Frame(w - k, l.BindHinge));
            l.InvFoot = QMath.Inverse(Frame(p - w, l.BindHinge));
            l.InvToe = QMath.Inverse(Frame(l.ToeDir, l.BindHinge));
            if (l.Girdle >= 0)
            {
                l.GirdleDir = Nrm(a - _bindModel[l.Girdle]);
                l.InvGirdle = QMath.Inverse(Frame(l.GirdleDir, Vector3.right));
            }
            l.Reach = (l.L1 + l.L2) * 0.985f;
        }

        // ================================================================== reset

        private void ResetLegged(Vector3 root, float yaw)
        {
            Quaternion ry = QMath.AY(yaw);
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                Vector3 w = root + ry * new Vector3(l.Neutral.x, 0f, l.Neutral.z);
                w.y = root.y + l.PawH;
                l.Plant = l.Paw = l.From = l.Land = w;
                l.Swing = l.IdleStep = false;
                l.U = 0f;
                l.PrevPsi = -1f;
                l.Since = 1f;
                l.OverW = 0f;
                l.Released = false;
                l.Lift = 0f;
            }
            _moving = false;
            _bodyInit = false;
            _airT = 0f;
            for (int i = 0; i < _tY.Length; i++) _tY[i] = _tP[i] = _tYV[i] = _tPV[i] = 0f;
            _lookYaw = _lookPitch = _lookW = 0f;
            if (_plan == CreaturePlan.Winged) ResetWings();
        }

        // ================================================================== solve

        private void SolveLegged(ref CreaturePoseInput p, float dt)
        {
            if (_plan == CreaturePlan.Winged)
            {
                SolveWinged(ref p, dt);
                return;
            }
            SolveGround(ref p, dt, 1f);
        }

        /// <summary>
        /// The ground pose. groundW below 1 is a flyer that is taking off or landing: the body follows the flight blend and
        /// the feet stop planting.
        /// </summary>
        private void SolveGround(ref CreaturePoseInput p, float dt, float groundW)
        {
            Overlay o = new Overlay();
            o.Reset();
            bool dead = p.Action == CreatureAction.Death && p.ActionT >= 0f;
            Gait(ref p, dt);
            ActionsLegged(ref p, ref o, dt);
            Attention04L(ref p, ref o);
            StormProwlerPose04OF(ref p,ref o);
            RegionalArt04OG(ref p,ref o);
            GothicReadabilityH3(ref p,ref o);
            if (_alert > 0.001f) AlertPose(ref o, _alert);
            if (_hurt > 0.001f) HurtPose(ref o, _hurt);
            bool airborne = !p.Grounded;
            _airT = airborne ? _airT + dt : 0f;
            Feet(ref p, dt, ref o, airborne);
            Trunk(ref p, dt, ref o, groundW);
            NeckAndHead(ref p, dt, ref o);
            float drop = Limbs(ref p, dt, ref o, dead, false);
            if (drop > 0.002f && p.Grounded && !dead && groundW > 0.5f)
            {
                // A planted paw is just out of reach (the shoulder blade's swing, a steep slope): lower the body by the
                // shortfall and solve the legs again, so a planted paw does not skate. Bigger misses are a stepping
                // problem, left to the gait.
                SaveLocals();
                _bodyY -= drop;
                PlaceTrunk(ref o);
                RestoreLocals();
                Limbs(ref p, dt, ref o, dead, true);
            }
            FollowPlants(ref p);
            TailPose(ref p, dt, ref o);
            Face(ref p, dt, ref o);
            if (_plan == CreaturePlan.Winged) GroundWings(ref p, dt, ref o);
        }

        // ------------------------------------------------------------------ gait

        private void Gait(ref CreaturePoseInput p, float dt)
        {
            float L = Mathf.Max(0.05f, _legLen);
            float v = _speed;
            float spin = Mathf.Abs(_yawRate) * Mathf.Deg2Rad * _turnRadius;
            float vEff = Mathf.Max(v, spin);
            float sq = Mathf.Sqrt(L);
            if (!_moving && vEff > 0.16f * sq) _moving = true;
            else if (_moving && vEff < 0.07f * sq) _moving = false;
            float fr = vEff * vEff / (G * L);
            _wTrot = SS(0.3f, 0.55f, fr);
            _wGallop = _canGallop ? SS(1.8f, 2.7f, fr) : 0f;
            _duty = Mathf.Lerp(Mathf.Lerp(0.7f, 0.46f, _wTrot), 0.32f, _wGallop);
            float lambda = 2.3f * Mathf.Pow(Mathf.Max(0.015f, fr), 0.3f);
            float T = lambda * L / Mathf.Max(0.08f, vEff);
            float tMin = 0.24f * Mathf.Sqrt(L / 0.45f), tMax = 1.2f * Mathf.Sqrt(L / 0.45f);
            T = Mathf.Clamp(T, tMin, tMax);
            // A paw can only travel so far under the body while it is planted: past that the cadence rises instead.
            float maxTravel = L * Mathf.Lerp(0.72f, 1.1f, _wGallop) * (_lizard ? 0.85f : 1f);
            if (vEff * T * _duty > maxTravel) T = maxTravel / Mathf.Max(0.05f, vEff * _duty);
            _cycle = Mathf.Lerp(_cycle, T, QMath.Damp(6f, dt));
            bool settling = false;
            for (int i = 0; i < _legRig.Length; i++) if (_legRig[i].Swing && !_legRig[i].IdleStep) settling = true;
            if (_moving || settling) _phase = Frac(_phase + dt / Mathf.Max(0.12f, _cycle));
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                float off = Mathf.Lerp(Mathf.Lerp(WalkOff[l.Role], TrotOff[l.Role], _wTrot), GallopOff[l.Role], _wGallop);
                l.Offset = off;
            }
        }

        // ------------------------------------------------------------------ feet

        private void Feet(ref CreaturePoseInput p, float dt, ref Overlay o, bool airborne)
        {
            bool dead = p.Action == CreatureAction.Death && p.ActionT >= 0f;
            Quaternion ry = QMath.AY(p.Yaw);
            Vector3 root = p.RootPosition;
            Vector3 vW = ry * _vel;
            float omega = _yawRate * Mathf.Deg2Rad;
            float L = Mathf.Max(0.05f, _legLen);
            float T = Mathf.Max(0.12f, _cycle);
            float D = _duty;
            float stanceTime = D * T;
            int swinging = 0;
            for (int i = 0; i < _legRig.Length; i++) if (_legRig[i].Swing) swinging++;
            float narrow = Mathf.Lerp(1f, 0.72f, _wGallop) * Mathf.Lerp(1f, 0.92f, _wTrot);
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                float psi = Frac(_phase + l.Offset);
                l.Since += dt;
                // Where this paw belongs: under its hip or shoulder, predicted for when it lands.
                Vector3 nLocal = new Vector3(l.Neutral.x * (_lizard ? 1f : narrow), 0f, l.Neutral.z);
                // A wyvern's wing-feet stay off the ground until the wings have finished folding after a landing.
                bool free = airborne || l.OverW > 0.001f || dead || (l.Wing && _fly > 0.01f);
                if (free)
                {
                    // Off the ground: the limb solver poses it freely until it touches down again.
                    l.Released = true;
                    l.Swing = false;
                    l.IdleStep = false;
                    l.PrevPsi = psi;
                    continue;
                }
                if (l.Released)
                {
                    // Touchdown after a jump or an action: plant where the paw came down.
                    l.Released = false;
                    l.HasBase = false;
                    Vector3 g = l.Paw;
                    g.y = Mathf.Clamp(Ground(ref p, g.x, g.z, root.y), root.y - 0.9f * L, root.y + 0.9f * L) + l.PawH;
                    l.Plant = l.Paw = g;
                    l.Since = 0f;
                    FootPlantMask |= 1 << i;
                }
                Vector3 neutralNow = root + ry * nLocal;
                if (!l.Swing)
                {
                    l.Paw = l.Plant;
                    bool crossed = Crossed(l.PrevPsi, psi, D);
                    float drift = Flat(l.Plant - neutralNow).magnitude;
                    bool far = drift > 0.16f * L;
                    // Distance from where the hip or shoulder will be: past the reach, the paw must go now.
                    Vector3 attachW = root + ry * new Vector3(l.AttachBind.x, 0f, l.AttachBind.z);
                    float stretch = Flat(l.Plant - attachW).magnitude;
                    bool overstretched = stretch > 0.72f * (l.L1 + l.L2 + l.L3);
                    // Near the end of its stance a leg that is running out of reach lifts a little early instead of
                    // straightening (and dragging the body down after it).
                    bool early = false;
                    if (_moving && !crossed && psi < D && psi > D * 0.5f && _bodyInit)
                    {
                        Vector3 hip = root + ry * _pos[l.Upper];
                        Vector3 ankle = l.Plant + ry * (l.FootDir * l.L3);
                        early = (hip - ankle).sqrMagnitude > Sq(0.95f * l.Reach);
                    }
                    if (_moving && crossed && (drift > 0.02f * L || _speed > 0.05f)) StartSwing(l, psi, T, D, false);
                    else if (early) { StartSwing(l, psi, T, D, false); l.Early = true; l.LiftPsi = psi; }
                    else if (overstretched && swinging < 2) { StartSwing(l, psi, T, D, true); swinging++; }
                    else if (!_moving && far && swinging == 0 && l.Since > 0.12f) { StartSwing(l, psi, T, D, true); swinging++; }
                }
                if (l.Swing)
                {
                    float remaining;
                    float minSwing = 0.1f * Mathf.Sqrt(L / 0.45f);
                    if (l.IdleStep)
                    {
                        // Timed steps: settling and turning on the spot, and emergency steps for a paw left too far behind.
                        float dur = (_moving ? 0.17f : 0.22f) * Mathf.Sqrt(L / 0.45f);
                        l.U = Mathf.Min(1f, l.U + dt / dur);
                        remaining = (1f - l.U) * dur;
                    }
                    else
                    {
                        // Phase-driven swing: lands as the leg's phase comes round, but never faster than a real step.
                        remaining = psi >= D || (l.Early && psi >= l.LiftPsi) ? (1f - psi) * T : 0f;
                        float left = Mathf.Max(remaining, (1f - l.U) * minSwing);
                        float step = left > dt ? dt / left : 1f;
                        l.U = Mathf.Min(1f, l.U + (1f - l.U) * step);
                        remaining = left;
                    }
                    l.Remaining = remaining;
                    // Landing target: the neutral point where the body will be at touchdown, moved ahead by half the
                    // stance so the paw passes under the hip at mid-stance.
                    float yawTD = p.Yaw + Mathf.Clamp(_yawRate * remaining, -50f, 50f);
                    Vector3 rootTD = root + vW * remaining;
                    Vector3 rel = QMath.AY(yawTD) * nLocal;
                    Vector3 land = rootTD + rel;
                    if (!l.IdleStep && _moving)
                    {
                        Vector3 vN = vW + new Vector3(omega * rel.z, 0f, -omega * rel.x);
                        land += vN * (stanceTime * 0.5f);
                    }
                    // Never aim for ground the leg cannot reach (a ledge far below lands the paw at the reachable height).
                    land.y = Mathf.Clamp(Ground(ref p, land.x, land.z, root.y), root.y - 0.9f * L, root.y + 0.9f * L) + l.PawH;
                    // Late in the swing the landing spot is fixed: the paw comes straight down onto it.
                    if (l.LandFixed) land = l.Land;
                    else if (l.U > 0.72f) l.LandFixed = true;
                    l.Land = land;
                    // The paw peels up before it travels and has finished travelling before it sets down, so it never
                    // scuffs along the ground at lift-off or touchdown.
                    float h = E3((l.U - 0.07f) / 0.83f);
                    Vector3 pos = Vector3.Lerp(l.From, land, h);
                    float liftK = (l.Front ? 1.15f : 1f) * Mathf.Lerp(Mathf.Lerp(0.11f, 0.17f, _wTrot), 0.26f, _wGallop) * (_lizard ? 0.6f : 1f);
                    float lift = L * liftK * Mathf.Clamp01(Flat(land - l.From).magnitude / (0.25f * L) + 0.35f);
                    float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(l.U, 0.85f));
                    pos.y += lift * Mathf.Max(0f, arc);
                    l.Lift = lift * Mathf.Max(0f, arc);
                    l.Paw = pos;
                    if (l.U >= 1f)
                    {
                        l.Swing = false;
                        l.IdleStep = false;
                        l.Plant = land;
                        l.Paw = land;
                        l.Lift = 0f;
                        l.Since = 0f;
                        FootPlantMask |= 1 << i;
                    }
                }
                l.PrevPsi = psi;
            }
        }

        /// <summary>True when phase went from a to b (forward, wrapping) across the lift-off point d.</summary>
        private static bool Crossed(float a, float b, float d)
        {
            if (a < 0f) return false;
            if (b >= a) return a < d && d <= b;
            return d > a || d <= b;
        }

        private static void StartSwing(Leg l, float psi, float T, float D, bool idle)
        {
            l.Swing = true;
            l.IdleStep = idle;
            l.Early = false;
            l.LandFixed = false;
            l.U = 0f;
            l.From = l.Plant;
        }

        // ------------------------------------------------------------------ trunk

        private void Trunk(ref CreaturePoseInput p, float dt, ref Overlay o, float groundW)
        {
            float L = Mathf.Max(0.05f, _legLen);
            Quaternion invY = QMath.AY(-p.Yaw);
            float rootY = p.RootPosition.y;
            // How high the hips and shoulders may ride: every planted paw must stay within reach.
            float rearMax = float.MaxValue, frontMax = float.MaxValue;
            float groundRear = 0f, groundFront = 0f;
            int nr = 0, nf = 0;
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                Vector3 pm = invY * (l.Paw - p.RootPosition);
                float gy;
                if (l.Released) gy = Ground(ref p, l.Paw.x, l.Paw.z, rootY) - rootY;
                else gy = (l.Swing ? l.Land.y : l.Plant.y) - rootY - l.PawH;
                if (l.Front) { groundFront += gy; nf++; } else { groundRear += gy; nr++; }
                if (l.Swing || l.Released || l.OverW > 0.3f) continue;
                Vector3 w = pm + l.FootDir * l.L3;
                // The hip or shoulder where it was last frame, relative to the body root (pitch and bend included).
                Vector3 att = _bodyInit ? _pos[l.Upper] : l.AttachBind;
                float above = _bodyInit ? att.y - _pos[_root].y : l.AttachBind.y - _rootH;
                float dx = w.x - att.x, dz = w.z - att.z;
                float h2 = dx * dx + dz * dz;
                float maxLen = l.Reach;
                float vert = Mathf.Sqrt(Mathf.Max(maxLen * maxLen * 0.04f, maxLen * maxLen - h2));
                float lim = w.y + vert - above;
                l.MaxAttach = w.y + vert;
                if (l.Front) frontMax = Mathf.Min(frontMax, lim); else rearMax = Mathf.Min(rearMax, lim);
            }
            // No ground reading can carry the body further than the legs could ever reach.
            groundRear = Mathf.Clamp(nr > 0 ? groundRear / nr : 0f, -1.2f * L, 1.2f * L);
            groundFront = Mathf.Clamp(nf > 0 ? groundFront / nf : groundRear, -1.2f * L, 1.2f * L);
            // In the air the body rides with the transform (the runtime carries the jump), not the ground below.
            if (_airT > 0f)
            {
                float stay = 1f - SS(0f, 0.15f, _airT);
                groundRear *= stay;
                groundFront *= stay;
            }
            // Gait bob: two dips per stride at a walk or trot, a rocking gallop with a gathered suspension.
            float ph = _phase * Mathf.PI * 2f;
            float bob = -L * (0.012f * (1f - _wTrot) + 0.022f * _wTrot * (1f - _wGallop)) * (0.5f + 0.5f * Mathf.Cos(2f * ph)) * (_moving ? 1f : 0f);
            bob += L * 0.05f * _wGallop * Mathf.Sin(ph + 0.6f);
            float breathe = Mathf.Sin(_clock * 1.6f) * 0.004f * L * (1f - SS(0.2f, 1f, _speed));
            float crouch = o.Crouch * L * 0.3f;
            float baseRear = _rootH + groundRear + bob + breathe - crouch - o.CrouchRear * L * 0.25f;
            float baseFront = _rootH + _frontLift * (1f - o.Dead) + groundFront + bob + breathe - crouch - o.CrouchFront * L * 0.3f;
            if (_canGallop) { baseRear -= L * 0.03f * _wGallop * Mathf.Sin(ph); baseFront += L * 0.03f * _wGallop * Mathf.Sin(ph); }
            float rearY = Mathf.Min(baseRear, rearMax);
            float frontY = Mathf.Min(baseFront, frontMax);
            if (p.Action == CreatureAction.Death || _airT > 0.05f) { rearY = baseRear; frontY = baseFront; }
            float targetY = (rearY + frontY) * 0.5f + o.Root.y;
            float targetPitch = Mathf.Atan2(rearY - frontY, Mathf.Max(0.1f, _trunkLen)) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -35f, 35f) + o.Pitch;
            // Lean into turns, sway with the stride at a walk.
            float turnRoll = Mathf.Clamp(-_yawRate * Mathf.Deg2Rad * _speed * 4.5f, -16f, 16f);
            float sway = (1f - _wTrot) * 1.6f * Mathf.Sin(ph) * (_moving ? 1f : 0f);
            float targetRoll = turnRoll + sway + o.Roll;
            float targetBend = Mathf.Clamp(_yawRate * 0.07f, -16f, 16f) + (_lizard ? 9f * Mathf.Sin(ph) * (_moving ? 1f : 0f) : 0f) + o.Bend;
            float targetFlex = (_canGallop ? 9f * _wGallop * Mathf.Sin(ph + 0.3f) : 0f) + o.Arch;
            if (!_bodyInit)
            {
                _bodyY = targetY; _bodyPitch = targetPitch; _bodyRoll = targetRoll; _bend = targetBend; _flex = targetFlex;
                _bodyInit = true;
            }
            float k = QMath.Damp(o.Snap > 0f ? Mathf.Lerp(14f, 30f, o.Snap) : 14f, dt);
            _bodyY = Mathf.Lerp(_bodyY, targetY, k);
            // The reach limits are hard: never let smoothing lift the body above what planted legs allow.
            if (p.Grounded && groundW > 0.5f && p.Action != CreatureAction.Death)
            {
                float hard = Mathf.Min(rearMax, frontMax) + 0.5f * Mathf.Abs(rearMax - frontMax);
                if (rearMax < float.MaxValue * 0.5f && frontMax < float.MaxValue * 0.5f) _bodyY = Mathf.Min(_bodyY, hard + o.Root.y);
            }
            _bodyPitch = Mathf.Lerp(_bodyPitch, targetPitch, k);
            _bodyRoll = Mathf.Lerp(_bodyRoll, targetRoll, QMath.Damp(6f, dt));
            _bend = Mathf.Lerp(_bend, targetBend, QMath.Damp(7f, dt));
            _flex = Mathf.Lerp(_flex, targetFlex, QMath.Damp(12f, dt));
            _bodyX = Mathf.Lerp(_bodyX, o.Root.x + (1f - _wTrot) * 0.008f * L * Mathf.Sin(ph) * (_moving ? 1f : 0f), QMath.Damp(10f, dt));
            // Whatever the overlays asked for (rearing, lunging, rolling), a planted paw must stay within its leg's reach:
            // lower the body by the worst excess, measured on the final pitch and roll.
            if (p.Grounded && groundW > 0.5f && p.Action != CreatureAction.Death)
            {
                Quaternion tilt = Euler(_bodyPitch, o.Yaw, _bodyRoll);
                float excess = 0f;
                for (int i = 0; i < _legRig.Length; i++)
                {
                    Leg l = _legRig[i];
                    if (l.Swing || l.Released || l.OverW > 0.001f) continue;
                    Vector3 off = tilt * (l.AttachBind - _bindModel[_root]);
                    float y = _bodyY + off.y;
                    excess = Mathf.Max(excess, y - l.MaxAttach);
                }
                if (excess > 0f) _bodyY -= excess;
            }
            PlaceTrunk(ref o);
        }

        /// <summary>Places the root, pelvis, spine and chest from the body state (height, pitch, roll, bend, flex).</summary>
        private void PlaceTrunk(ref Overlay o)
        {
            // Whatever the ground does, the body stays within a leg's reach of the transform.
            _bodyY = Mathf.Clamp(_bodyY, _rootH - 1.2f * _legLen, _rootH + 1.5f * _legLen);
            Vector3 rootPos = new Vector3(_bindModel[_root].x + _bodyX, _bodyY, _bindModel[_root].z + o.Root.z);
            _local[_root] = rootPos;
            SetModel(_root, Euler(_bodyPitch, o.Yaw, _bodyRoll));
            // The back half bends one way and the front the other, about the middle of the trunk.
            if (_pelvis >= 0)
            {
                Quaternion q = Euler(-_flex * 0.5f, -_bend * 0.5f, 0f);
                _local[_pelvis] = q * _bindLocal[_pelvis];
                SetLocal(_pelvis, q);
            }
            if (_spine >= 0)
            {
                SetLocal(_spine, Euler(_flex * 0.25f, _bend * 0.3f, 0f));
                if (_chest >= 0) SetLocal(_chest, Euler(_flex * 0.25f + o.ChestPitch, _bend * 0.25f, -_bodyRoll * 0.15f));
            }
            else if (_chest >= 0) SetLocal(_chest, Euler(_flex * 0.5f + o.ChestPitch, _bend * 0.5f, 0f));
        }

        // ------------------------------------------------------------------ neck and head

        private void NeckAndHead(ref CreaturePoseInput p, float dt, ref Overlay o)
        {
            QuartzQuietGesture(ref p, ref o);
            int nn = _neck.Length;
            float ph = _phase * Mathf.PI * 2f;
            // Look-at: the target direction in the chest's frame, clamped, smoothed, spread down the neck.
            float wantYaw = 0f, wantPitch = 0f, wantW = 0f;
            if (_hasLook && _head >= 0)
            {
                Vector3 from = Attach(_head);
                Vector3 d = _lookDir - (from - Vector3.zero);
                Vector3 local = QMath.Inverse(_rot[_chest >= 0 ? _chest : _root]) * d;
                float hz = Mathf.Sqrt(local.x * local.x + local.z * local.z);
                wantYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -85f, 85f);
                wantPitch = Mathf.Clamp(-Mathf.Atan2(local.y, Mathf.Max(0.01f, hz)) * Mathf.Rad2Deg, -45f, 50f);
                if (local.z < 0f && Mathf.Abs(local.x) < 0.3f * hz) wantYaw = Mathf.Sign(local.x + 1e-4f) * 85f;
                wantW = o.LookW;
            }
            _lookW = Mathf.MoveTowards(_lookW, wantW, dt * 3f);
            _lookYaw = Mathf.Lerp(_lookYaw, wantYaw, QMath.Damp(7f, dt));
            _lookPitch = Mathf.Lerp(_lookPitch, wantPitch, QMath.Damp(7f, dt));
            float lookYaw = _lookYaw * _lookW, lookPitch = _lookPitch * _lookW;
            // Head nod with the forelegs at a walk and trot, a reaching head at the gallop.
            float nod = (_moving ? 1f : 0f) * (2.2f * (1f - _wGallop) * Mathf.Sin(2f * ph + 0.5f) - 6f * _wGallop);
            float idle = (1f - SS(0.1f, 0.8f, _speed)) * 2.5f * Mathf.Sin(_clock * 0.37f + 1.3f);
            float idleYaw = (1f - SS(0.1f, 0.8f, _speed)) * (6f * Mathf.Sin(_clock * 0.23f) + 3f * Mathf.Sin(_clock * 0.61f + 2f)) * (1f - _lookW);
            float neckPitch = o.NeckPitch + nod * 0.5f - _bodyPitch * 0.35f;
            float neckYaw = o.NeckYaw - _bend * 0.4f + idleYaw * 0.5f;
            for (int i = 0; i < nn; i++)
            {
                float f = 1f / nn;
                float yawPart = (lookYaw * 0.55f + neckYaw) * f;
                float pitchPart = (lookPitch * 0.45f + neckPitch) * f;
                SetLocal(_neck[i], Euler(pitchPart, yawPart, -_bodyRoll * 0.3f * f));
            }
            if (_head >= 0)
            {
                // Stabilise: cancel most of the body's pitch and roll so the head stays level while the body moves.
                float headPitch = o.HeadPitch + lookPitch * (nn > 0 ? 0.55f : 1f) + nod * 0.5f + idle;
                float headYaw = o.HeadYaw + lookYaw * (nn > 0 ? 0.45f : 1f) + idleYaw * 0.5f;
                Quaternion parentRot = _rot[_parent[_head]];
                Quaternion local = Euler(headPitch, headYaw, o.HeadRoll);
                Quaternion want = parentRot * local;
                // Remove the parent's roll and some pitch relative to the creature's heading.
                Vector3 fwd = want * Vector3.forward;
                Vector3 level = Nrm(new Vector3(fwd.x, fwd.y * 0.75f, fwd.z));
                Quaternion stab = QMath.LookRotation(level.sqrMagnitude > 0.5f ? level : Vector3.forward, Vector3.up) * QMath.AZ(o.HeadRoll);
                Quaternion head = QMath.Slerp(want, stab, 0.55f * (1f - o.Free));
                SetModel(_head, head);
            }
        }

        // ------------------------------------------------------------------ legs

        /// <summary>Solves every leg; returns how far the worst planted paw is from its plant (0 when all reach).</summary>
        private float Limbs(ref CreaturePoseInput p, float dt, ref Overlay o, bool dead, bool again)
        {
            Quaternion invY = QMath.AY(-p.Yaw);
            float worst = 0f;
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                Vector3 paw = invY * (l.Paw - p.RootPosition);
                Vector3 disp = _pos[_root] - _bindModel[_root];
                if (l.Released)
                {
                    // A released leg rests on a base that eases from where the paw lifted to its stance under the body, so
                    // whenever the action lets go the paw comes down in the right place without snapping.
                    if (!l.HasBase) { l.Base = paw; l.HasBase = true; }
                    float gy = Ground(ref p, p.RootPosition.x + (QMath.AY(p.Yaw) * l.Neutral).x, p.RootPosition.z + (QMath.AY(p.Yaw) * l.Neutral).z, p.RootPosition.y) - p.RootPosition.y;
                    gy = Mathf.Clamp(gy, -1.2f * _legLen, 1.2f * _legLen);
                    Vector3 stance = new Vector3(l.Neutral.x + disp.x, gy + l.PawH, l.Neutral.z + disp.z);
                    if (!again) l.Base = Vector3.Lerp(l.Base, stance, QMath.Damp(5f, dt));
                    paw = l.Base;
                    if (_airT > 0f) paw = Vector3.Lerp(paw, DanglePaw(l), SS(0f, 0.15f, _airT));
                }
                // Overrides are authored relative to the body, so they ride along with crouches and lunges.
                if (l.OverW > 0f)
                {
                    paw = Vector3.Lerp(paw, l.Over + disp, l.OverW);
                    // While an override blends in or out the paw lifts clear of the ground instead of scuffing along it.
                    paw.y += 0.1f * _legLen * 4f * l.OverW * (1f - l.OverW);
                }
                if (dead) paw = Vector3.Lerp(paw, DeadPaw(l), o.Dead);
                l.Target = paw;
                // Wing-feet are posed with the wings.
                if (l.Wing) continue;
                float swingU = l.Swing ? l.U : 0f;
                SolveLeg(l, paw, swingU, o);
                if (!l.Released && l.OverW < 0.01f && !l.Swing && (_pos[l.Toe] - paw).sqrMagnitude > 4e-6f)
                {
                    // How far the hip or shoulder must come down for the leg to reach its planted paw.
                    Vector3 d = Attach(l.Upper) - (paw + (_pos[l.Foot] - _pos[l.Toe]));
                    float r = (l.L1 + l.L2) * 0.998f;
                    float h2 = d.x * d.x + d.z * d.z;
                    if (h2 < r * r)
                    {
                        float need = d.y - Mathf.Sqrt(r * r - h2);
                        if (need > 0f && need < 0.08f * _legLen) worst = Mathf.Max(worst, need);
                    }
                }
            }
            return worst;
        }

        /// <summary>
        /// Where each paw really is after the leg solve: a planted paw that still could not be reached moves its plant
        /// (the last resort), and released or overridden paws are wherever the limb put them.
        /// </summary>
        private void FollowPlants(ref CreaturePoseInput p)
        {
            Quaternion ry = QMath.AY(p.Yaw);
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                if (l.Wing) continue;
                Vector3 actual = p.RootPosition + ry * _pos[l.Toe];
                if (!l.Released && l.OverW < 0.01f)
                {
                    if (!l.Swing && (actual - l.Paw).sqrMagnitude > 1e-6f)
                    {
                        Vector3 slide = actual - l.Plant;
                        slide.y = 0f;
                        l.Plant += slide;
                        l.Paw = l.Plant;
                    }
                }
                else l.Paw = actual;
            }
        }

        private Quaternion[] _savedLocal = new Quaternion[0];

        /// <summary>Remembers every bone's rotation relative to its parent (before the trunk moves).</summary>
        private void SaveLocals()
        {
            if (_savedLocal.Length != _n) _savedLocal = new Quaternion[_n];
            for (int i = 0; i < _n; i++)
            {
                int par = _parent[i];
                _savedLocal[i] = par >= 0 ? QMath.Inverse(_rot[par]) * _rot[i] : _rot[i];
            }
        }

        /// <summary>Re-places every bone below the trunk with its remembered local rotation.</summary>
        private void RestoreLocals()
        {
            for (int i = 0; i < _n; i++)
            {
                if (i == _root || i == _pelvis || i == _spine || i == _chest || _parent[i] < 0) continue;
                SetLocal(i, _savedLocal[i]);
            }
        }

        /// <summary>Where bone i sits given its parent's current transform (before i itself is solved this frame).</summary>
        private Vector3 Attach(int i)
        {
            int p = _parent[i];
            return p >= 0 ? _pos[p] + _rot[p] * _local[i] : _local[i];
        }

        /// <summary>Where a paw hangs when the leg is off the ground (model space): tucked under the body, a little forward.</summary>
        private Vector3 DanglePaw(Leg l)
        {
            Vector3 attach = Attach(l.Upper);
            float L = l.L1 + l.L2 + l.L3;
            Vector3 down = _rot[_root] * new Vector3(0f, -0.82f, l.Front ? 0.18f : -0.12f);
            return attach + Nrm(down) * (L * 0.8f);
        }

        /// <summary>A leg folded against the body of a creature lying on its side (model space).</summary>
        private Vector3 DeadPaw(Leg l)
        {
            Vector3 attach = Attach(l.Upper);
            float L = l.L1 + l.L2 + l.L3;
            Vector3 dir = _rot[_root] * Nrm(new Vector3(0f, -0.62f, l.Front ? 0.55f : -0.45f));
            return attach + dir * (L * 0.72f);
        }

        private void SolveLeg(Leg l, Vector3 paw, float swingU, Overlay o)
        {
            Quaternion body = _rot[_root];
            // The shoulder blade swings with the foreleg (it slides along the chest wall as the leg reaches and pushes).
            if (l.Girdle >= 0)
            {
                Vector3 rel = QMath.Inverse(_rot[_chest]) * (paw - _pos[l.Girdle]);
                float reach = Mathf.Clamp(rel.z / Mathf.Max(0.05f, l.L1 + l.L2), -0.8f, 0.8f);
                SetLocal(l.Girdle, QMath.AX(-reach * 22f));
            }
            Vector3 a = Attach(l.Upper);
            // Metatarsus or pastern: from the paw up to the hock or wrist, folding in the swing and under load.
            float L = l.L1 + l.L2 + l.L3;
            float compress = Mathf.Clamp01(1f - (a - paw).magnitude / Mathf.Max(0.05f, L * 0.92f));
            float fold = Mathf.Sin(Mathf.PI * Mathf.Clamp01(swingU));
            float flex = l.Front ? (fold * 62f + compress * 12f) : (fold * 34f - compress * 14f);
            flex += l.OverFlex;
            if (_lizard) flex *= 0.5f;
            Vector3 side = body * Vector3.right;
            Vector3 footDir = QMath.AngleAxis(flex, side) * (body * l.FootDir);
            Vector3 w = paw + footDir * l.L3;
            // Knees point forward, elbows back; a sprawling lizard bends them out to the side.
            Vector3 pole = l.Front ? body * new Vector3(0f, 0.15f, -1f) : body * new Vector3(0f, 0.15f, 1f);
            if (_lizard) pole = body * Nrm(new Vector3(l.Side * 1f, 0.55f, l.Front ? -0.3f : 0.3f));
            Vector3 end;
            Vector3 k = SolveJoint(a, w, l.L1, l.L2, pole, out end);
            Vector3 hinge = Vector3.Cross(k - a, end - k);
            if (hinge.sqrMagnitude < 1e-10f) hinge = body * l.BindHinge;
            hinge = Nrm(hinge);
            if (Vector3.Dot(hinge, body * l.BindHinge) < 0f) hinge = -hinge;
            SetModel(l.Upper, Frame(k - a, hinge) * l.InvUpper);
            SetModel(l.Lower, Frame(end - k, hinge) * l.InvLower);
            Vector3 footVec = paw - end;
            if (footVec.sqrMagnitude < 1e-8f) footVec = -footDir;
            SetModel(l.Foot, Frame(footVec, hinge) * l.InvFoot);
            // Toes: flat along the ground in stance, curled under in the swing.
            Vector3 toeFwd = body * l.ToeDir;
            toeFwd.y = 0f;
            toeFwd = toeFwd.sqrMagnitude > 1e-6f ? Nrm(toeFwd) : body * Vector3.forward;
            float curl = fold * (l.Front ? 55f : 40f) + o.ToeCurl;
            Vector3 toeDir = QMath.AngleAxis(curl, hinge) * toeFwd;
            SetModel(l.Toe, Frame(toeDir, hinge) * l.InvToe);
        }

        // ------------------------------------------------------------------ tail

        private void TailPose(ref CreaturePoseInput p, float dt, ref Overlay o)
        {
            int nt = _tail.Length;
            if (nt == 0) return;
            float ph = _phase * Mathf.PI * 2f;
            float moving = _moving ? 1f : 0f;
            float swayAmp = moving * Mathf.Lerp(Mathf.Lerp(7f, 4f, _wTrot), 2f, _wGallop) + (1f - moving) * 3f;
            float idlePhase = _clock * (_lizard ? 0.9f : 1.3f);
            for (int i = 0; i < nt; i++)
            {
                float u = nt > 1 ? (float)i / (nt - 1) : 0f;
                // A travelling wave down the tail, the stride's sway, and the lag outward when the body turns.
                float wave = Mathf.Sin((moving > 0f ? ph : idlePhase) - u * 2.2f) * swayAmp * (0.4f + u);
                float turn = -_yawRate * 0.022f * (0.6f + u) * (1f - o.TailFree);
                float yawT = wave + turn + o.TailYaw * (1f - u * 0.3f) + (_lizard ? -_bend * 0.4f : 0f);
                // Carriage: relaxed droop, lifted in the gallop for balance, stiffened when alert.
                float pitchT = o.TailPitch * (i == 0 ? 1f : 0.35f) + (_wGallop * 42f + _wTrot * (1f - _wGallop) * 14f) * (i == 0 ? 1f : 0.2f) + (i == 0 ? _bodyPitch * 0.5f + o.TailBase : 0f);
                pitchT += Mathf.Sin(idlePhase * 0.7f + u * 1.6f) * 2f * (1f - moving);
                float stiff = 1f / (1f + u * 1.4f);
                float kS = Sq(Mathf.PI * 2f * Mathf.Lerp(2.6f, 1.3f, u)) * (1f + o.TailSnap * 3f);
                float c = 2f * 0.62f * Mathf.Sqrt(kS);
                _tYV[i] += (kS * (yawT - _tY[i]) - c * _tYV[i]) * dt;
                _tPV[i] += (kS * (pitchT - _tP[i]) - c * _tPV[i]) * dt;
                // The parent's swing drags the child (a whip): add part of the previous bone's velocity.
                if (i > 0) { _tYV[i] += _tYV[i - 1] * 0.06f * stiff; }
                _tYV[i] = Mathf.Clamp(_tYV[i], -2500f, 2500f);
                _tPV[i] = Mathf.Clamp(_tPV[i], -2500f, 2500f);
                _tY[i] = Mathf.Clamp(_tY[i] + _tYV[i] * dt, -75f, 75f);
                _tP[i] = Mathf.Clamp(_tP[i] + _tPV[i] * dt, -70f, 70f);
                SetLocal(_tail[i], Euler(_tP[i], _tY[i], 0f));
                // The tail drags along the ground rather than through it: lift any segment that would dip below.
                Vector3 seg = i + 1 < nt ? _local[_tail[i + 1]] : _local[_tail[i]] * 0.8f;
                Vector3 next = _pos[_tail[i]] + _rot[_tail[i]] * seg;
                float floor = 0.03f * _legLen + (_airT > 0f ? -10f : 0f);
                if (next.y < floor)
                {
                    float len = Mathf.Max(1e-4f, seg.magnitude);
                    float lift = Mathf.Asin(Mathf.Clamp01((floor - next.y) / len)) * Mathf.Rad2Deg;
                    SetLocal(_tail[i], Euler(_tP[i] + lift, _tY[i], 0f));
                }
            }
        }

        // ------------------------------------------------------------------ face: jaw and ears

        private void Face(ref CreaturePoseInput p, float dt, ref Overlay o)
        {
            float pant = _wGallop * 0.25f * (0.5f + 0.5f * Mathf.Sin(_phase * Mathf.PI * 4f));
            float want = Mathf.Max(o.Jaw, pant);
            _jawOpen = Mathf.Lerp(_jawOpen, want, QMath.Damp(o.Jaw > _jawOpen ? 30f : 12f, dt));
            if (_jaw >= 0) SetLocal(_jaw, QMath.AX(_jawOpen * (_lizard ? 32f : 38f)));
            if (_earL >= 0 || _earR >= 0)
            {
                _earPin = Mathf.Lerp(_earPin, Mathf.Max(o.EarsBack, _alert * 0.6f), QMath.Damp(8f, dt));
                float tw = Mathf.Max(0f, Mathf.Sin(_clock * 2.3f) * Mathf.Sin(_clock * 0.71f)) ;
                _earTw = Mathf.Lerp(_earTw, tw > 0.8f ? 1f : 0f, QMath.Damp(14f, dt));
                if (_earL >= 0) SetLocal(_earL, Euler(-_earPin * 38f + _earTw * 8f, -_earPin * 12f - 4f * _earTw, _earPin * 14f));
                if (_earR >= 0) SetLocal(_earR, Euler(-_earPin * 38f, _earPin * 12f, -_earPin * 14f));
            }
        }
    }
}
