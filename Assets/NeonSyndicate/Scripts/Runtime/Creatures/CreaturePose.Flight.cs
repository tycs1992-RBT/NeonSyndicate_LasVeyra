using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Wings and flight. On the ground the wings fold along the flanks (a wyvern walks on its folded wings' wrists); in
    /// the air they beat with a clear downstroke (extended, sweeping down and forward) and upstroke (elbow and wrist
    /// flexed, flicking up and back), glide with a slight dihedral, bank into turns, hover with fast flat beats, tuck
    /// for a dive and flare wide to land with the legs reaching for the ground. Take-off starts with one big downstroke.
    /// The ground and flight poses are blended over a short transition.
    /// </summary>
    public sealed partial class CreaturePose
    {
        private sealed class WingRig
        {
            public int Arm, Fore, Hand, Side;
            public int[] Fingers = new int[0];
            /// <summary>Bind fan angle of each finger relative to the hand (degrees about the vertical).</summary>
            public float[] FingerAngle = new float[0];
            public float Length;
        }

        private struct WingPose
        {
            public float Raise, Sweep, Twist, Elbow, Wrist, Spread, Curl;

            public static WingPose Lerp(WingPose a, WingPose b, float t)
            {
                WingPose r;
                r.Raise = Mathf.Lerp(a.Raise, b.Raise, t);
                r.Sweep = Mathf.Lerp(a.Sweep, b.Sweep, t);
                r.Twist = Mathf.Lerp(a.Twist, b.Twist, t);
                r.Elbow = Mathf.Lerp(a.Elbow, b.Elbow, t);
                r.Wrist = Mathf.Lerp(a.Wrist, b.Wrist, t);
                r.Spread = Mathf.Lerp(a.Spread, b.Spread, t);
                r.Curl = Mathf.Lerp(a.Curl, b.Curl, t);
                return r;
            }
        }

        private WingRig[] _wingRig = new WingRig[0];
        private float _fly, _flap, _flapRate, _beat, _flyPitch, _flyRoll, _airTime, _flare;
        private bool _wasFlying;
        private bool _wingWalker;
        private float _wingSpan = 2f;
        // Ground and flight locals for the transition blend.
        private Quaternion[] _blendRot = new Quaternion[0];
        private Vector3[] _blendPos = new Vector3[0];

        /// <summary>Wings folded at rest (feathered and membrane wings fold the same way).</summary>
        private static readonly WingPose Folded = new WingPose { Raise = -78f, Sweep = 96f, Twist = 0f, Elbow = 150f, Wrist = 158f, Spread = 0f, Curl = 0f };
        private static readonly WingPose Glide = new WingPose { Raise = 6f, Sweep = 8f, Twist = -2f, Elbow = 4f, Wrist = 6f, Spread = 1f, Curl = 0f };

        private void InitWings()
        {
            _wingWalker = !_look.Forelegs;
            int nw = _sk.Wings != null ? _sk.Wings.Length : 0;
            _wingRig = new WingRig[nw];
            for (int i = 0; i < nw; i++)
            {
                WingChain c = _sk.Wings[i];
                WingRig w = new WingRig { Arm = c.Arm, Fore = c.Fore, Hand = c.Hand, Side = c.Side };
                int count = 0;
                for (int b = 0; b < _n; b++) if (_parent[b] == c.Hand) count++;
                w.Fingers = new int[count];
                w.FingerAngle = new float[count];
                // The fan bones (membrane fingers, or the carriers of the primaries) in the builder's order, leading
                // first; their bind angles come from the builder's tables.
                float[] fan = CreatureSculptor.IsFeathered(_look) ? CreatureSculptor.FeatherFan : CreatureSculptor.MembraneFan;
                int k = 0;
                for (int b = 0; b < _n; b++)
                {
                    if (_parent[b] != c.Hand) continue;
                    w.Fingers[k] = b;
                    w.FingerAngle[k] = k < fan.Length ? -fan[k] : 0f;
                    k++;
                }
                w.Length = (_bindModel[c.Hand] - _bindModel[c.Arm]).magnitude / 0.62f;
                _wingRig[i] = w;
                _wingSpan = Mathf.Max(_wingSpan, w.Length * 2f);
            }
            _blendRot = new Quaternion[_n];
            _blendPos = new Vector3[_n];
            _flapRate = 3.3f / Mathf.Sqrt(Mathf.Max(0.5f, _wingSpan * 0.5f));
        }

        private void ResetWings()
        {
            _fly = 0f;
            _flap = 0f;
            _wasFlying = false;
            _airTime = 0f;
            _flare = 0f;
            _flyPitch = _flyRoll = 0f;
        }

        /// <summary>A wyvern's wing used as a foreleg: arm and forearm reach for the wrist's plant.</summary>
        private void SetupWingLeg(Leg l)
        {
            Vector3 a = _bindModel[l.Upper], e = _bindModel[l.Lower], w = _bindModel[l.Foot];
            l.L1 = (e - a).magnitude;
            l.L2 = (w - e).magnitude;
            l.L3 = 0f;
            l.PawH = 0.02f * _bindModel[_sk.Root].y + 0.01f;
            float reach = (l.L1 + l.L2) * 0.62f;
            l.AttachBind = a;
            l.Neutral = new Vector3(a.x + l.Side * reach * 0.24f, l.PawH, a.z + reach * 0.62f);
            l.FootDir = Vector3.up;
            Vector3 h = Vector3.Cross(e - a, w - e);
            l.BindHinge = h.sqrMagnitude > 1e-10f ? Nrm(h) : Vector3.up;
            l.InvUpper = QMath.Inverse(Frame(e - a, l.BindHinge));
            l.InvLower = QMath.Inverse(Frame(w - e, l.BindHinge));
            l.Reach = (l.L1 + l.L2) * 0.97f;
        }

        // ================================================================== solve

        private void SolveWinged(ref CreaturePoseInput p, float dt)
        {
            bool flying = p.Flying;
            if (flying && !_wasFlying) _airTime = 0f;
            _wasFlying = flying;
            _airTime = flying ? _airTime + dt : 0f;
            float target = flying ? 1f : 0f;
            // Take-off is quick (one leap), landing a little slower (the wings close after touchdown).
            _fly = Mathf.MoveTowards(_fly, target, dt * (flying ? 2.4f : 1.8f));
            if (_fly <= 0.001f)
            {
                SolveGround(ref p, dt, 1f);
                return;
            }
            Quaternion ry = QMath.AY(p.Yaw);
            if (_fly >= 0.999f)
            {
                SolveFlight(ref p, dt);
                // Every foot is off the ground; the ground solver plants them again where they touch down.
                for (int i = 0; i < _legRig.Length; i++)
                {
                    Leg l = _legRig[i];
                    l.Released = true;
                    l.Swing = l.IdleStep = false;
                    l.HasBase = false;
                    l.Paw = p.RootPosition + ry * _pos[l.Toe];
                }
                return;
            }
            // Transition: solve both and blend the local pose.
            SolveGround(ref p, dt, 1f - _fly);
            for (int i = 0; i < _n; i++)
            {
                int par = _parent[i];
                _blendRot[i] = par >= 0 ? QMath.Inverse(_rot[par]) * _rot[i] : _rot[i];
                _blendPos[i] = _local[i];
            }
            SolveFlight(ref p, dt);
            float k = E3(_fly);
            for (int i = 0; i < _n; i++)
            {
                int par = _parent[i];
                Quaternion fl = par >= 0 ? QMath.Inverse(_rot[par]) * _rot[i] : _rot[i];
                Quaternion q = QMath.Slerp(_blendRot[i], fl, k);
                _local[i] = Vector3.Lerp(_blendPos[i], _local[i], k);
                SetLocal(i, q);
            }
            // Feet already on the ground stay exactly where they are: solve those legs again on the blended body.
            Overlay o = new Overlay();
            o.Reset();
            Quaternion invY = QMath.AY(-p.Yaw);
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                if (l.Wing || l.Released || l.OverW > 0.01f)
                {
                    l.Paw = p.RootPosition + ry * _pos[l.Toe];
                    continue;
                }
                SolveLeg(l, invY * (l.Paw - p.RootPosition), l.Swing ? l.U : 0f, o);
                Vector3 actual = p.RootPosition + ry * _pos[l.Toe];
                if (!l.Swing && (actual - l.Paw).sqrMagnitude > 1e-6f)
                {
                    Vector3 slide = actual - l.Plant;
                    slide.y = 0f;
                    l.Plant += slide;
                    l.Paw = l.Plant;
                }
            }
        }

        // ------------------------------------------------------------------ flight

        private void SolveFlight(ref CreaturePoseInput p, float dt)
        {
            Overlay o = new Overlay();
            o.Reset();
            ClearLegOverrides(dt);
            // The ground gait fades out (the tail and head stop following it).
            float fade = QMath.Damp(3f, dt);
            _wTrot = Mathf.Lerp(_wTrot, 0f, fade);
            _wGallop = Mathf.Lerp(_wGallop, 0f, fade);
            _moving = false;
            float L = Mathf.Max(0.05f, _legLen);
            float speedH = _speed;
            float vy = _climb;
            float size = Mathf.Max(0.5f, _wingSpan * 0.5f);
            // How the creature is flying: hovering when slow, diving when dropping fast, flaring close to the ground.
            float hover = 1f - SS(0.6f * size, 2.2f * size, speedH);
            float climb = Mathf.Clamp(vy / Mathf.Max(2f, speedH + 2f), -1f, 1f);
            float diveIn = p.Action == CreatureAction.Dive && p.ActionT >= 0f ? PhasesOf(ref p).W : 0f;
            float flareH = Mathf.Max(1.2f, 2.2f * L);
            float landing = p.Altitude < flareH && vy < 0.5f && _airTime > 0.6f ? 1f - SS(0.15f * flareH, flareH, p.Altitude) : 0f;
            _flare = Mathf.Lerp(_flare, landing, QMath.Damp(5f, dt));
            // Gliding when fast and not climbing; beating otherwise. Take-off beats hard.
            float takeoff = 1f - SS(0.6f, 1.6f, _airTime);
            float glide = SS(1.8f * size, 3.2f * size, speedH) * (1f - SS(0.1f, 0.4f, climb)) * (1f - takeoff) * (1f - _flare);
            float rate = _flapRate * (1f + 0.55f * hover + 0.3f * Mathf.Max(0f, climb) + 0.6f * takeoff + 0.5f * _flare) * (1f - 0.85f * glide);
            _flap = Frac(_flap + dt * rate);
            if (!_init || _airTime < dt * 1.5f) _flap = 0.62f;
            _beat = Mathf.Lerp(_beat, 1f - glide, QMath.Damp(4f, dt));
            // Body attitude.
            float pitch = Mathf.Lerp(-3f, 4f, SS(0f, 1f, speedH / (3f * size))) - climb * 24f;
            pitch = Mathf.Lerp(pitch, -32f, hover * 0.85f);
            pitch = Mathf.Lerp(pitch, -38f, _flare);
            pitch = Mathf.Lerp(pitch, 55f, diveIn);
            float roll = Mathf.Clamp(-_yawRate * Mathf.Deg2Rad * Mathf.Max(2f, speedH) * 7f, -55f, 55f) * (1f - hover * 0.6f);
            _flyPitch = Mathf.Lerp(_flyPitch, pitch, QMath.Damp(4f, dt));
            _flyRoll = Mathf.Lerp(_flyRoll, roll, QMath.Damp(3.5f, dt));
            // Beat-driven bob: the body lifts on the downstroke.
            float phase = _flap * Mathf.PI * 2f;
            float bob = -Mathf.Sin(phase) * 0.06f * size * _beat * (1f - diveIn);
            ActionsFlying(ref p, ref o, dt, L);
            RegionalArt04OG(ref p,ref o);
            if (_hurt > 0.001f) HurtPose(ref o, _hurt);
            Vector3 rootPos = _bindModel[_root] + new Vector3(o.Root.x, bob + o.Root.y, o.Root.z);
            _local[_root] = rootPos;
            SetModel(_root, Euler(_flyPitch + o.Pitch, o.Yaw, _flyRoll + o.Roll));
            float flex = Mathf.Sin(phase + 0.8f) * 3f * _beat;
            if (_pelvis >= 0)
            {
                Quaternion q = Euler(-flex * 0.5f + o.Arch * 0.5f, -_yawRate * 0.02f, 0f);
                _local[_pelvis] = q * _bindLocal[_pelvis];
                SetLocal(_pelvis, q);
            }
            if (_spine >= 0) SetLocal(_spine, Euler(flex * 0.25f, _yawRate * 0.01f, 0f));
            if (_chest >= 0) SetLocal(_chest, Euler(flex * 0.25f + o.ChestPitch, _yawRate * 0.01f, -_flyRoll * 0.1f));
            // The neck stretches out ahead (less when hovering) and keeps the head level against the body's pitch.
            o.NeckPitch += Mathf.Lerp(20f, 8f, hover) - _flyPitch * 0.45f + diveIn * -10f;
            o.HeadPitch += -Mathf.Lerp(14f, 5f, hover) - _flyPitch * 0.25f;
            _bodyPitch = _flyPitch;
            _bodyRoll = _flyRoll;
            _bend = 0f;
            NeckAndHead(ref p, dt, ref o);
            FlightLegs(ref p, dt, ref o, L);
            // Tail streams out behind and works as a rudder.
            // It stays in line with the body in a dive and lifts toward level when the body rears up to hover.
            o.TailBase += 4f - _flyPitch * 0.5f - Mathf.Min(0f, _flyPitch) * 0.4f;
            o.TailYaw += -_yawRate * 0.05f;
            TailPose(ref p, dt, ref o);
            Face(ref p, dt, ref o);
            // Wings.
            WingPose beatPose = Beat(_flap, hover, takeoff);
            WingPose wp = WingPose.Lerp(beatPose, Glide, glide);
            if (_flare > 0.01f) wp = WingPose.Lerp(wp, Flare(_flap), _flare);
            if (diveIn > 0.01f) wp = WingPose.Lerp(wp, new WingPose { Raise = -18f, Sweep = 70f, Twist = 0f, Elbow = 125f, Wrist = 140f, Spread = 0f }, diveIn);
            if (o.WingBeat > 0f) wp = WingPose.Lerp(wp, Beat(_flap, 0.5f, 0.5f), o.WingBeat);
            for (int i = 0; i < _wingRig.Length; i++)
            {
                WingRig w = _wingRig[i];
                // Banking: the inside wing drops its lift, the outside wing beats a little harder.
                WingPose side = wp;
                float bankSide = Mathf.Clamp(_flyRoll / 55f, -1f, 1f) * w.Side;
                side.Raise += bankSide * 8f;
                side.Twist += bankSide * 6f;
                QuartzWingMotion(ref p, w, ref side, glide);
                ApplyWing(w, side);
            }
        }

        /// <summary>The wing beat at phase u: downstroke over the first 55 percent, upstroke for the rest.</summary>
        private static WingPose Beat(float u, float hover, float takeoff)
        {
            WingPose w;
            float amp = 1f + 0.25f * takeoff;
            if (u < 0.55f)
            {
                float d = E3(u / 0.55f);
                w.Raise = Mathf.Lerp(58f, -42f, d) * amp;
                w.Sweep = Mathf.Lerp(-4f, -14f, Mathf.Sin(d * Mathf.PI)) + 4f * d;
                w.Twist = 12f * Mathf.Sin(d * Mathf.PI);
                w.Elbow = 6f;
                w.Wrist = 4f;
                w.Spread = 1f;
            }
            else
            {
                float d = E3((u - 0.55f) / 0.45f);
                float mid = Mathf.Sin(d * Mathf.PI);
                w.Raise = Mathf.Lerp(-42f, 58f, d) * amp;
                w.Sweep = Mathf.Lerp(0f, -4f, d) + 26f * mid;
                w.Twist = -14f * mid;
                w.Elbow = 6f + 42f * mid;
                w.Wrist = 4f + 62f * mid;
                w.Spread = 1f - 0.4f * mid;
            }
            w.Curl = 0f;
            // Hovering moves the stroke into a flatter, faster figure (the wings sweep forward and back more).
            if (hover > 0f)
            {
                w.Raise *= Mathf.Lerp(1f, 0.65f, hover);
                w.Sweep += hover * (u < 0.55f ? -22f * Mathf.Sin(u / 0.55f * Mathf.PI) : 26f * Mathf.Sin((u - 0.55f) / 0.45f * Mathf.PI));
                w.Twist *= 1f + hover;
            }
            return w;
        }

        /// <summary>Landing flare: wings spread wide and forward, cupped against the air, with short braking beats.</summary>
        private static WingPose Flare(float u)
        {
            float s = Mathf.Sin(u * Mathf.PI * 2f);
            return new WingPose { Raise = 30f + 14f * s, Sweep = -26f, Twist = -28f, Elbow = 12f, Wrist = 10f, Spread = 1f, Curl = 0.3f };
        }

        private void ApplyWing(WingRig w, WingPose p)
        {
            float s = w.Side;
            SetLocal(w.Arm, QMath.AZ(s * p.Raise) * QMath.AY(s * p.Sweep) * QMath.AX(p.Twist * 0.4f));
            SetLocal(w.Fore, QMath.AY(-s * p.Elbow) * QMath.AX(p.Twist * 0.3f));
            SetLocal(w.Hand, QMath.AY(s * p.Wrist) * QMath.AX(p.Twist * 0.3f));
            for (int f = 0; f < w.Fingers.Length; f++)
            {
                // Closing the fan turns every finger toward the hand's line; curl bends them down a little.
                float close = -w.FingerAngle[f] * (1f - Mathf.Clamp01(p.Spread));
                SetLocal(w.Fingers[f], QMath.AY(s * close) * QMath.AX(p.Curl * 10f * (f + 1)));
            }
        }

        // ------------------------------------------------------------------ legs in the air

        private void FlightLegs(ref CreaturePoseInput p, float dt, ref Overlay o, float L)
        {
            Quaternion body = _rot[_root];
            float ground = p.Altitude;
            for (int i = 0; i < _legRig.Length; i++)
            {
                Leg l = _legRig[i];
                if (l.Wing) continue;
                Vector3 attach = Attach(l.Upper);
                float len = l.L1 + l.L2 + l.L3;
                // Tucked: hind legs drawn up and back under the tail, forelegs folded under the chest.
                Vector3 tuck = l.Front ? new Vector3(0f, -0.5f, 0.2f) : new Vector3(0f, -0.32f, -0.9f);
                Vector3 paw = attach + body * Nrm(tuck) * (len * (l.Front ? 0.6f : 0.66f));
                // Landing: the legs reach down for the ground beneath the body.
                if (_flare > 0.01f)
                {
                    Vector3 reach = new Vector3(l.Neutral.x, l.PawH - Mathf.Max(0f, ground) + (_pos[_root].y - _rootH), l.Neutral.z + (l.Front ? 0.1f : 0.05f) * L);
                    Vector3 toward = reach - attach;
                    if (toward.magnitude > len * 0.98f) reach = attach + Nrm(toward) * (len * 0.98f);
                    paw = Vector3.Lerp(paw, reach, _flare);
                }
                if (l.OverW > 0f) paw = Vector3.Lerp(paw, l.Over + (_pos[_root] - _bindModel[_root]), l.OverW);
                SolveLeg(l, paw, 0.6f * (1f - _flare), o);
            }
            // A wyvern's wing-legs fly as wings: nothing to solve here.
        }

        // ------------------------------------------------------------------ on the ground

        /// <summary>Wings on the ground: folded (or half spread for threats and death); a wyvern walks on the wrists.</summary>
        private void GroundWings(ref CreaturePoseInput p, float dt, ref Overlay o)
        {
            float breathe = Mathf.Sin(_clock * 1.6f) * 2f;
            WingPose fold = Folded;
            fold.Raise += breathe;
            WingPose threat = new WingPose { Raise = 28f, Sweep = 18f, Twist = -6f, Elbow = 30f, Wrist = 35f, Spread = 0.85f, Curl = 0f };
            WingPose wp = WingPose.Lerp(fold, threat, Mathf.Clamp01(o.WingSpread));
            Quaternion invY = QMath.AY(-p.Yaw);
            for (int i = 0; i < _wingRig.Length; i++)
            {
                WingRig w = _wingRig[i];
                Leg wl = null;
                for (int k = 0; k < _legRig.Length; k++) if (_legRig[k].Wing && _legRig[k].Upper == w.Arm) wl = _legRig[k];
                if (wl != null && o.WingSpread < 0.5f && o.Dead < 0.5f)
                {
                    // Walking on the folded wing: the wrist is the foot.
                    Vector3 paw = wl.Target;
                    Vector3 a = Attach(wl.Upper);
                    Quaternion body = _rot[_root];
                    Vector3 pole = body * Nrm(new Vector3(wl.Side * 0.4f, 0.85f, -0.75f));
                    Vector3 end;
                    Vector3 e = SolveJoint(a, paw, wl.L1, wl.L2, pole, out end);
                    Vector3 hinge = Vector3.Cross(e - a, end - e);
                    if (hinge.sqrMagnitude < 1e-10f) hinge = body * wl.BindHinge;
                    hinge = Nrm(hinge);
                    if (Vector3.Dot(hinge, body * wl.BindHinge) < 0f) hinge = -hinge;
                    SetModel(wl.Upper, Frame(e - a, hinge) * wl.InvUpper);
                    SetModel(wl.Lower, Frame(end - e, hinge) * wl.InvLower);
                    // Hand and fingers fold back up along the forearm, the closed membrane facing outward.
                    float s = w.Side;
                    Vector3 handDir = Nrm(Nrm(e - end) + body * new Vector3(s * 0.15f, 0f, -0.35f));
                    Vector3 outward = body * new Vector3(s, 0f, 0f);
                    Vector3 xImg = handDir * s;
                    Vector3 yImg = Nrm(outward - handDir * Vector3.Dot(outward, handDir));
                    if (yImg.sqrMagnitude < 0.5f) yImg = body * Vector3.up;
                    SetModel(w.Hand, QMath.FromBasis(xImg, yImg, Vector3.Cross(xImg, yImg)));
                    for (int f = 0; f < w.Fingers.Length; f++) SetLocal(w.Fingers[f], QMath.AY(-s * w.FingerAngle[f]));
                    if (!wl.Released && wl.OverW < 0.01f && !wl.Swing)
                    {
                        Vector3 actual = p.RootPosition + QMath.AY(p.Yaw) * _pos[w.Hand];
                        Vector3 slide = actual - wl.Plant;
                        slide.y = 0f;
                        if (slide.sqrMagnitude > 1e-8f) { wl.Plant += slide; wl.Paw = wl.Plant; }
                    }
                    else wl.Paw = p.RootPosition + QMath.AY(p.Yaw) * _pos[w.Hand];
                    continue;
                }
                WingPose use = wp;
                // Dead: the wings go slack in a loose fold, the upper one draped over the fallen body.
                if (o.Dead > 0f) use = WingPose.Lerp(wp, new WingPose { Raise = -52f, Sweep = 72f, Twist = 0f, Elbow = 105f, Wrist = 115f, Spread = 0.3f }, o.Dead);
                QuartzWingMotion(ref p, w, ref use, 0f);
                ApplyWing(w, use);
                if (wl != null) wl.Paw = p.RootPosition + QMath.AY(p.Yaw) * _pos[w.Hand];
            }
        }

        // ------------------------------------------------------------------ actions in the air

        private void ActionsFlying(ref CreaturePoseInput p, ref Overlay o, float dt, float L)
        {
            if (p.ActionT < 0f) return;
            Phases ph = PhasesOf(ref p);
            float W = ph.W;
            switch (p.Action)
            {
                case CreatureAction.Claw:
                {
                    // Talons: the hind legs swing forward and open, the body pitches back to rake.
                    o.Pitch += W * Key3(ph, -10f, -28f);
                    o.WingBeat = Mathf.Max(o.WingBeat, W);
                    for (int i = 0; i < _legRig.Length; i++)
                    {
                        Leg l = _legRig[i];
                        if (l.Wing) continue;
                        Vector3 n = l.Neutral;
                        Vector3 cocked = n + new Vector3(0f, 0.25f * L, (l.Front ? 0.2f : -0.1f) * L);
                        Vector3 strike = n + new Vector3(0f, -0.05f * L, (l.Front ? 0.75f : 0.6f) * L);
                        OverrideLeg(l, W, Key3(ph, cocked, strike, n), Key3(ph, 40f, -30f));
                    }
                    o.ToeCurl = W * Key3(ph, 40f, -30f);
                    o.Jaw = Mathf.Max(o.Jaw, W * 0.4f);
                    break;
                }
                case CreatureAction.Breath:
                case CreatureAction.Spit:
                    Breath(ref o, ph, L);
                    o.Pitch += W * Key3(ph, -12f, 6f);
                    break;
                case CreatureAction.Roar:
                    Roar(ref o, ph, L, false);
                    o.WingBeat = Mathf.Max(o.WingBeat, W * 0.8f);
                    break;
                case CreatureAction.Bite:
                    Bite(ref o, ph, L);
                    break;
                case CreatureAction.Stagger:
                    StaggerPose(ref o, ph, L);
                    o.Roll += Mathf.Sin(ph.T * 20f) * 25f * Mathf.Sin(Mathf.PI * ph.T);
                    break;
                case CreatureAction.Death:
                    // Falling out of the sky: wings crumple, body tumbles nose down.
                    o.Pitch += 40f * E3(Mathf.Clamp01(p.ActionT / 0.5f));
                    o.Roll += 60f * E3(Mathf.Clamp01(p.ActionT / 0.6f));
                    o.Free = 1f;
                    o.LookW = 0f;
                    break;
            }
        }
    }
}
