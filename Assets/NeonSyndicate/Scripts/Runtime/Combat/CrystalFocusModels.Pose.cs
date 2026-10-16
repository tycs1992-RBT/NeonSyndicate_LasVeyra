using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a crystal model is used as, which changes how its rig behaves.</summary>
    public enum CrystalRigRole
    {
        /// <summary>A held focus, pickup or display copy (fed by the weapons runtime when held).</summary>
        Held = 0,
        /// <summary>A projectile body (spinning disc, tumbling orb).</summary>
        Shot = 1,
        /// <summary>The Mirrorwake reflective focus marker.</summary>
        Marker = 2,
        /// <summary>The resonance case with two lidded compartments.</summary>
        Case = 3,
        /// <summary>A practice target post (flashes when hit).</summary>
        Target = 4
    }

    /// <summary>The rest pose and motion data of one moving part, shared by every instance of a model.</summary>
    public sealed class CrystalRigPart
    {
        public CrystalPartRole Role;
        public int Index;
        public Vector3 RestPos;
        public Quaternion RestRot = QMath.Identity;
        public Vector3 Axis = Vector3.right;
        public Vector3 Aux;
    }

    /// <summary>
    /// The smoothed animation inputs a crystal model is posed from. The rig integrates these from its public fields every
    /// frame; the offline harness and the tests fill them by hand to preview or check a state.
    /// </summary>
    public struct CrystalRigState
    {
        /// <summary>Animation seconds (gameplay time, so pause freezes it).</summary>
        public float Time;
        public float Heat;
        public float Charge;
        /// <summary>Firing blend 0 to 1 (the beam opening and closing).</summary>
        public float Fire;
        /// <summary>Discharge kick: 1 on a shot, decaying to 0.</summary>
        public float Kick;
        /// <summary>Overheat blend 0 to 1.</summary>
        public float Vent;
        /// <summary>Gyre Crescent: the disc is away (the cradle shows empty).</summary>
        public bool Away;
        /// <summary>Gyre Crescent: 1 the moment the disc is back, decaying (the recovery effect).</summary>
        public float Recover;
        /// <summary>Mirage Choir: 1 at a volley, decaying linearly to 0 (seeds fan out and return).</summary>
        public float Fan;
        public int Locks;
        /// <summary>Mirrorwake: planted focus blend (one facet leaves the fan).</summary>
        public float Planted;
        /// <summary>Accumulated spin of cores, lenses, discs and prisms, in degrees.</summary>
        public float Spin;
        /// <summary>Accumulated orbit of pebbles, in degrees.</summary>
        public float Orbit;
        /// <summary>A full-charge flare: 1 when the charge completes, decaying.</summary>
        public float Ready;
        /// <summary>Case lids, 0 shut to 1 open.</summary>
        public float Open0, Open1;
        /// <summary>Practice target hit flash, 1 decaying to 0.</summary>
        public float Flash;
        /// <summary>Scale-in of a freshly placed marker, 0 to 1.</summary>
        public float Spawn;
    }

    /// <summary>
    /// The pure half of CrystalFocusRig: where each moving part sits for a given state, how fast things spin and how
    /// bright the active facets glow. Engine-free (QMath), so the offline harness renders charged, firing and empty
    /// states with exactly the game's maths, and the EditMode tests check them.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>Seconds a Mirage Choir volley takes to fan out and return.</summary>
        public const float FanSeconds = 0.7f;
        /// <summary>Seconds of the Gyre Crescent recovery effect after the disc comes back.</summary>
        public const float RecoverSeconds = 0.35f;

        /// <summary>Degrees per second the spinning part of a model turns in this state.</summary>
        public static float SpinRate(CrystalBehavior b, CrystalRigRole role, ref CrystalRigState s)
        {
            if (role == CrystalRigRole.Shot) return b == CrystalBehavior.GyreCrescent ? 1080f : 140f;
            if (role != CrystalRigRole.Held) return 25f;
            switch (b)
            {
                case CrystalBehavior.PrismThread: return 8f + 110f * (1f - s.Fire);
                case CrystalBehavior.GyreCrescent: return 160f + 1100f * s.Recover;
                case CrystalBehavior.Sunderheart: return 24f + 90f * s.Charge + 60f * s.Vent;
                case CrystalBehavior.Mirrorwake: return 18f + 260f * s.Charge + 200f * s.Kick;
                default: return 40f + 520f * s.Charge + 160f * s.Vent;
            }
        }

        /// <summary>The Mirage Choir fan shape: 0 at rest, out quickly to 1, then back to rest.</summary>
        public static float FanShape(float fan)
        {
            if (fan <= 0f) return 0f;
            float e = 1f - Mathf.Clamp01(fan);
            return e < 0.25f ? e / 0.25f : 1f - (e - 0.25f) / 0.75f;
        }

        /// <summary>How converged the Mirrorwake facets are: the charge, or the snap after a pulse settling out.</summary>
        public static float Convergence(ref CrystalRigState s)
        {
            return Mathf.Clamp01(Mathf.Max(s.Charge, s.Kick));
        }

        /// <summary>Poses one moving part: local position, rotation and uniform scale on its parent.</summary>
        public static void PosePart(CrystalBehavior b, CrystalRigRole role, CrystalRigPart p, ref CrystalRigState s, out Vector3 pos, out Quaternion rot, out float scale)
        {
            pos = p.RestPos;
            rot = p.RestRot;
            scale = 1f;
            float t = s.Time;
            switch (p.Role)
            {
                case CrystalPartRole.Core:
                    if (role == CrystalRigRole.Shot)
                    {
                        rot = p.RestRot * QMath.AY(s.Spin) * QMath.AX(s.Spin * 0.7f);
                    }
                    else if (b == CrystalBehavior.Sunderheart)
                    {
                        pos = p.RestPos + new Vector3(0f, Mathf.Sin(t * 1.3f) * 0.0025f, -0.016f * s.Kick);
                        rot = p.RestRot * QMath.AY(s.Spin) * QMath.AX(s.Spin * 0.63f);
                        scale = 1f - 0.14f * s.Charge + 0.12f * s.Kick;
                    }
                    else
                    {
                        float jitter = s.Vent * Mathf.Sin(t * 53f) * 0.0006f;
                        pos = p.RestPos + new Vector3(jitter, Mathf.Sin(t * 2.1f) * 0.0014f, -0.006f * s.Charge - 0.01f * s.Kick);
                        rot = p.RestRot * QMath.AZ(s.Spin);
                        scale = 1f + 0.08f * s.Charge + 0.05f * s.Kick;
                    }
                    break;
                case CrystalPartRole.Rib:
                    {
                        float a = 14f * s.Charge + 8f * s.Kick + 9f * s.Vent + 1.2f * Mathf.Sin(t * 1.7f + p.Index * 2.1f);
                        rot = p.RestRot * QMath.AngleAxis(a, p.Axis);
                    }
                    break;
                case CrystalPartRole.Tine:
                    rot = p.RestRot * QMath.AngleAxis(3.2f * s.Fire + 6f * s.Vent + 1.5f * s.Kick, p.Axis);
                    break;
                case CrystalPartRole.Lens:
                    {
                        float calm = 1f - s.Fire;
                        pos = p.RestPos + new Vector3(0f, 0.0015f * Mathf.Sin(t * 2.3f) * calm, 0.006f * s.Fire);
                        rot = p.RestRot * QMath.AZ(s.Spin) * QMath.AX(6f * Mathf.Sin(t * 1.9f) * calm);
                        scale = 1f + 0.08f * s.Fire;
                    }
                    break;
                case CrystalPartRole.Disc:
                    rot = p.RestRot * QMath.AngleAxis(s.Spin, p.Axis);
                    if (role == CrystalRigRole.Held)
                        scale = s.Away ? 0f : 1f - 0.6f * s.Recover * s.Recover;
                    break;
                case CrystalPartRole.Pebble:
                    {
                        Vector3 off = p.RestPos - p.Aux;
                        float r0 = off.magnitude;
                        float a = (s.Orbit + p.Index * 120f) * Mathf.Deg2Rad;
                        Vector3 dir = Nrm(new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.8f, Mathf.Sin(a) * 0.35f));
                        float k = role == CrystalRigRole.Held ? 1f - 0.4f * s.Charge + 0.5f * s.Kick : 1f;
                        pos = p.Aux + dir * (r0 * k);
                        rot = p.RestRot * QMath.AY(s.Spin * 1.3f + p.Index * 40f) * QMath.AX(s.Spin * 0.9f);
                    }
                    break;
                case CrystalPartRole.Jaw:
                    rot = p.RestRot * QMath.AngleAxis(6f * s.Kick + 4f * s.Vent + 2f * s.Charge, p.Axis);
                    break;
                case CrystalPartRole.Seed:
                    {
                        float shape = FanShape(s.Fan);
                        float lift = p.Index < s.Locks ? 1f : 0f;
                        float a = 26f * shape + 6f * lift;
                        rot = p.RestRot * QMath.AngleAxis(a, p.Axis);
                        Vector3 fwd = p.RestRot * Vector3.forward;
                        pos = p.RestPos + fwd * (0.028f * shape + 0.008f * lift) + p.Aux * (0.006f * lift) + new Vector3(0f, 0.0012f * Mathf.Sin(t * 2f + p.Index * 1.3f), 0f);
                    }
                    break;
                case CrystalPartRole.Facet:
                    if (role == CrystalRigRole.Marker)
                    {
                        rot = p.RestRot * QMath.AZ(t * 22f);
                        pos = p.RestPos + new Vector3(0f, 0.012f * Mathf.Sin(t * 2.1f), 0f);
                        scale = Mathf.Clamp01(s.Spawn);
                    }
                    else
                    {
                        float c = Convergence(ref s);
                        float wob = 5f * Mathf.Sin(t * 0.9f + p.Index * 1.3f) * (1f - c);
                        Quaternion turn = QMath.AZ(wob);
                        Vector3 off = turn * (p.RestPos - p.Aux);
                        pos = p.Aux + off * (1f - 0.58f * c) + new Vector3(0f, 0f, 0.012f * c);
                        rot = turn * p.RestRot * QMath.AngleAxis(50f * c, p.Axis);
                        if (p.Index == 0) scale = 1f - Mathf.Clamp01(s.Planted);
                    }
                    break;
                case CrystalPartRole.Prism:
                    rot = p.RestRot * QMath.AZ(s.Spin);
                    break;
                case CrystalPartRole.Lid:
                    {
                        float open = p.Index == 0 ? s.Open0 : s.Open1;
                        rot = p.RestRot * QMath.AngleAxis(100f * Smooth01(open), p.Axis);
                    }
                    break;
                case CrystalPartRole.Ring:
                    rot = p.RestRot * QMath.AX(s.Flash * 7f * Mathf.Sin(t * 38f));
                    scale = 1f + 0.04f * s.Flash;
                    break;
            }
        }

        /// <summary>
        /// How bright an emissive slot glows (a multiplier on its base emission) and how far its colour shifts toward the
        /// vent colour. Active facets warm with heat and flicker hot while venting; cores swell with charge and kick.
        /// </summary>
        public static float GlowOf(CrystalBehavior b, CrystalRigRole role, CrystalRigPart p, bool core, ref CrystalRigState s, out float ventMix)
        {
            ventMix = 0f;
            float t = s.Time;
            switch (role)
            {
                case CrystalRigRole.Shot:
                    return 1f;
                case CrystalRigRole.Marker:
                    return 1f + 0.35f * Mathf.Sin(t * 3f);
                case CrystalRigRole.Case:
                    return 0.85f + 0.15f * Mathf.Sin(t * 1.5f + p.Index);
                case CrystalRigRole.Target:
                    return (core ? 1f + 4f * s.Flash : 1f + 3f * s.Flash) * (0.9f + 0.1f * Mathf.Sin(t * 2f));
            }
            float g = core
                ? 0.8f + 0.35f * s.Heat + 1.3f * s.Charge + 0.9f * s.Fire + 1.6f * s.Kick + 1.5f * s.Ready
                : 0.55f + 0.75f * s.Heat + 0.4f * s.Fire;
            if (s.Vent > 0f)
            {
                float flick = 0.5f + 0.5f * Mathf.Sin(t * 14f);
                g *= 1f + s.Vent * (0.3f + 0.9f * flick);
                ventMix = s.Vent * (0.55f + 0.45f * flick);
            }
            switch (p.Role)
            {
                case CrystalPartRole.Seed:
                    {
                        if (p.Index < s.Locks) g *= 2.2f;
                        if (s.Fan > 0f)
                        {
                            float e = (1f - s.Fan) * FanSeconds;
                            float k = Mathf.Clamp01((e - 0.2f - p.Index * 0.06f) / 0.12f);
                            g *= Mathf.Lerp(0.25f, 1f, k);
                        }
                    }
                    break;
                case CrystalPartRole.Disc:
                    g *= 1f + 2.5f * s.Recover;
                    break;
                case CrystalPartRole.Body:
                case CrystalPartRole.Horn:
                    // The empty Gyre Crescent cradle pulses while it waits for the disc.
                    if (b == CrystalBehavior.GyreCrescent && s.Away) g *= 1.2f + 0.8f * Mathf.Sin(t * 8f);
                    break;
                case CrystalPartRole.Facet:
                    g *= 1f + 1.4f * Convergence(ref s);
                    break;
                case CrystalPartRole.Prism:
                    g *= 1f + 1.5f * s.Charge + 1.2f * s.Kick;
                    break;
                case CrystalPartRole.Lens:
                    g *= 1f + 1.2f * s.Fire;
                    break;
            }
            return g;
        }

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
