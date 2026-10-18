using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What an animated part of a fused form does (FormPose turns the role and the live state into a pose).</summary>
    public enum FormPartRole
    {
        Static = 0,
        WingRoot = 1,
        WingMid = 2,
        WingTip = 3,
        TailVane = 4,
        TailFin = 5,
        Emitter = 6,
        Prow = 7,
        Plate = 8,
        Spine = 9,
        Fin = 10,
        Coil = 11
    }

    /// <summary>Submesh slots every form part uses (the runtime fills them from the form's palette).</summary>
    public static class FormSlots
    {
        public const int Shell = 0, Crystal = 1, Membrane = 2, Metal = 3, Glow = 4, Count = 5;
    }

    /// <summary>
    /// The proportions of one personal bike model in its model space (z forward, y up, origin on the ground under the
    /// middle), copied from the hover bike builder so the fused geometry fits the real hull, seat, bars and pegs. The
    /// seat point is checked against BikeModels.RiderSeatLocal by a test so the two never drift apart.
    /// </summary>
    public struct FusionFrame
    {
        public string Class;
        public float Zf, Zr, PodR, PodY, HullBottom, HalfW;
        public float SeatZ0, SeatZ1, SeatY, PillionZ0, PillionZ1, PillionY;
        public float TankZ1, TankTop, NoseZ, NoseTop, NoseBottom, TailZ, TailTop;
        public float BarY, BarZ, BarHalf, BarSweep, PegX, PegY, PegZ, ScreenTop;
        public bool TopBox;
        public float Length, Width;

        /// <summary>The frame of a chassis class (sport, cruiser or skiff).</summary>
        public static FusionFrame For(string chassisClass)
        {
            FusionFrame f = new FusionFrame();
            f.Class = GarageRules.IsClass(chassisClass) ? chassisClass : FusionIds.Sport;
            if (f.Class == FusionIds.Cruiser)
            {
                f.Length = 2.45f; f.Width = 0.95f;
                f.Zf = 0.86f; f.Zr = -0.8f; f.PodR = 0.23f; f.PodY = 0.32f; f.HullBottom = 0.36f; f.HalfW = 0.24f;
                f.SeatZ0 = -0.55f; f.SeatZ1 = 0f; f.SeatY = 0.76f; f.PillionZ0 = -0.9f; f.PillionZ1 = -0.55f; f.PillionY = 0.86f;
                f.TankZ1 = 0.5f; f.TankTop = 0.96f; f.NoseZ = 1.2f; f.NoseTop = 0.78f; f.NoseBottom = 0.48f; f.TailZ = -1.2f; f.TailTop = 0.86f;
                f.BarY = 1.12f; f.BarZ = 0.42f; f.BarHalf = 0.4f; f.BarSweep = 0.16f; f.PegX = 0.28f; f.PegY = 0.4f; f.PegZ = 0.25f; f.ScreenTop = 1.22f;
            }
            else if (f.Class == FusionIds.Skiff)
            {
                f.Length = 1.95f; f.Width = 0.72f;
                f.Zf = 0.62f; f.Zr = -0.6f; f.PodR = 0.17f; f.PodY = 0.29f; f.HullBottom = 0.4f; f.HalfW = 0.17f;
                f.SeatZ0 = -0.45f; f.SeatZ1 = 0f; f.SeatY = 0.85f; f.PillionZ0 = -0.76f; f.PillionZ1 = -0.45f; f.PillionY = 0.9f;
                f.TankZ1 = 0.36f; f.TankTop = 0.94f; f.NoseZ = 0.97f; f.NoseTop = 0.72f; f.NoseBottom = 0.48f; f.TailZ = -0.97f; f.TailTop = 0.92f;
                f.BarY = 1.03f; f.BarZ = 0.32f; f.BarHalf = 0.34f; f.BarSweep = 0.04f; f.PegX = 0.21f; f.PegY = 0.44f; f.PegZ = -0.05f; f.ScreenTop = 1.08f;
                f.TopBox = true;
            }
            else
            {
                f.Length = 2.15f; f.Width = 0.78f;
                f.Zf = 0.7f; f.Zr = -0.66f; f.PodR = 0.19f; f.PodY = 0.3f; f.HullBottom = 0.4f; f.HalfW = 0.19f;
                f.SeatZ0 = -0.52f; f.SeatZ1 = -0.02f; f.SeatY = 0.86f; f.PillionZ0 = -0.86f; f.PillionZ1 = -0.52f; f.PillionY = 0.93f;
                f.TankZ1 = 0.42f; f.TankTop = 0.98f; f.NoseZ = f.Length * 0.5f; f.NoseTop = 0.66f; f.NoseBottom = 0.46f; f.TailZ = -f.Length * 0.5f; f.TailTop = 0.98f;
                f.BarY = 1f; f.BarZ = 0.36f; f.BarHalf = 0.33f; f.BarSweep = 0f; f.PegX = 0.22f; f.PegY = 0.44f; f.PegZ = -0.06f; f.ScreenTop = 1.14f;
            }
            return f;
        }

        /// <summary>The rider's seat point (the same formula as BikeModels.RiderSeatLocal).</summary>
        public Vector3 SeatLocal { get { return new Vector3(0f, SeatY, Mathf.Lerp(SeatZ0, SeatZ1, 0.45f)); } }

        /// <summary>The headlight point on the nose.</summary>
        public Vector3 LampLocal { get { return new Vector3(0f, Mathf.Lerp(NoseBottom, NoseTop, 0.62f), NoseZ + 0.012f); } }

        /// <summary>The space the rider's torso, head and arms need above the seat; no fused part may enter it.</summary>
        public void RiderKeepout(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(-0.36f, SeatY - 0.05f, SeatZ0 - 0.12f);
            max = new Vector3(0.36f, SeatY + 1.05f, BarZ + 0.18f);
        }

        /// <summary>The space the rider's legs need between the seat and the pegs; no fused part may enter it.</summary>
        public void LegKeepout(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(-(PegX + 0.12f), PegY - 0.05f, Mathf.Min(PegZ, SeatZ0) - 0.1f);
            max = new Vector3(PegX + 0.12f, SeatY, Mathf.Max(PegZ, SeatZ1) + 0.2f);
        }

        /// <summary>The front of the leg space (fused parts on the fairing start ahead of it).</summary>
        public float LegFrontZ { get { return Mathf.Max(PegZ, SeatZ1) + 0.2f; } }

        /// <summary>The hull section at a z (bottom, top and half-width), interpolated over the bike builder's segments.</summary>
        public void HullAt(float z, out float bottom, out float top, out float half)
        {
            float w = HalfW;
            float fairEnd = NoseZ - 0.25f;
            float[,] seg =
            {
                { TailZ, PillionZ0, TailTop - 0.36f, PillionY - 0.38f, TailTop, PillionY - 0.03f, w * 0.5f, w * 0.78f },
                { PillionZ0, SeatZ0, PillionY - 0.38f, HullBottom + 0.05f, PillionY - 0.03f, SeatY - 0.03f, w * 0.78f, w * 0.95f },
                { SeatZ0, SeatZ1, HullBottom + 0.05f, HullBottom, SeatY - 0.03f, SeatY - 0.03f, w * 0.95f, w },
                { SeatZ1, TankZ1, HullBottom, HullBottom, SeatY, TankTop, w, w * 1.1f },
                { TankZ1, fairEnd, HullBottom, HullBottom + 0.03f, TankTop, NoseTop + 0.14f, w * 1.1f, w * 0.9f },
                { fairEnd, NoseZ, HullBottom + 0.03f, NoseBottom, NoseTop + 0.14f, NoseTop, w * 0.9f, w * 0.42f }
            };
            int n = seg.GetLength(0);
            int pick = z <= seg[0, 0] ? 0 : n - 1;
            for (int i = 0; i < n; i++) if (z >= seg[i, 0] && z <= seg[i, 1]) { pick = i; break; }
            float z0 = seg[pick, 0], z1 = seg[pick, 1];
            float t = z1 - z0 > 1e-4f ? Mathf.Clamp01((z - z0) / (z1 - z0)) : 0f;
            bottom = Mathf.Lerp(seg[pick, 2], seg[pick, 3], t);
            top = Mathf.Lerp(seg[pick, 4], seg[pick, 5], t);
            half = Mathf.Lerp(seg[pick, 6], seg[pick, 7], t);
        }

        /// <summary>The top of the courier top box (skiff only), else the tail top.</summary>
        public float TailStackTop { get { return TopBox ? TailTop + 0.32f : TailTop; } }
    }

    /// <summary>One part of a fused form: a mesh in its own frame, placed under a parent part, with an animation role.</summary>
    public sealed class FormPart
    {
        public string Name = "";
        /// <summary>Index of the parent part (-1 hangs it on the form root under the bike's lean pivot).</summary>
        public int Parent = -1;
        public Vector3 Position;
        public Quaternion Rotation = QMath.Identity;
        public FormPartRole Role;
        /// <summary>+1 right, -1 left, 0 centre (mirrored poses use it).</summary>
        public int Side;
        public int Index;
        public bool CastShadows = true;
        public MeshBuilder Mesh = new MeshBuilder(FormSlots.Count);
    }

    /// <summary>The live inputs the pose of a fused form follows (all 0..1 unless noted).</summary>
    public struct FormPoseInput
    {
        /// <summary>How far the wings are spread (0 folded along the hull, 1 fully spread).</summary>
        public float Spread;
        public float Glide;
        public float Brace;
        /// <summary>Steering, -1 left to 1 right.</summary>
        public float Steer;
        /// <summary>How erect the spines stand (ability ready).</summary>
        public float Flare;
        /// <summary>Tail vane pitch, -1 to 1.</summary>
        public float Vane;
        public float Pulse;
        /// <summary>Transition progress (0 just starting, 1 complete).</summary>
        public float Unfold;

        /// <summary>The parked pose after the transition (wings partly spread).</summary>
        public static FormPoseInput Parked
        {
            get { return new FormPoseInput { Spread = FormPose.IdleSpread, Unfold = 1f }; }
        }
    }

    /// <summary>
    /// Pure pose of every animated part: wings fold and spread (and lift while gliding), tail vanes pitch, the fin
    /// steers, the emitter pulses, the Kilnback prow pushes forward while bracing, spines flare, fins follow the
    /// steering. The runtime rig and the envelope tests use this one function, so what the tests check is what draws.
    /// </summary>
    public static class FormPose
    {
        /// <summary>Wing spread when parked or rolling slowly.</summary>
        public const float IdleSpread = 0.3f;
        /// <summary>The Kilnback prow's travel while bracing (m), and how far it sits back while the form grows.</summary>
        public const float ProwTravel = 0.22f, ProwRetract = 0.25f;

        /// <summary>The right wing root folded against the flank: span backward, top surface toward the hull, leading edge up.</summary>
        public static Quaternion FoldedRoot { get { return QMath.FromBasis(new Vector3(0f, 0f, -1f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f)); } }

        /// <summary>The right wing root spread: swept back 14 degrees with a little dihedral (more while gliding).</summary>
        public static Quaternion SpreadRoot(float glide)
        {
            return QMath.AY(14f) * QMath.AZ(6f + 8f * Mathf.Clamp01(glide));
        }

        /// <summary>Mirror of a rotation across the bike's centre plane (for the left side).</summary>
        public static Quaternion Mirror(Quaternion q) { return new Quaternion(q.x, -q.y, -q.z, q.w); }

        public static Vector3 Mirror(Vector3 v) { return new Vector3(-v.x, v.y, v.z); }

        /// <summary>The pose offset of a part: a move in its parent frame, a turn after its own rotation, a uniform scale.</summary>
        public static void Offset(FormPartRole role, int side, int index, FormPoseInput p, out Vector3 move, out Quaternion turn, out float scale)
        {
            move = Vector3.zero;
            turn = QMath.Identity;
            scale = 1f;
            float unfold = Mathf.Clamp01(p.Unfold);
            float closed = 1f - QMath.Smooth01(unfold);
            float spread = Mathf.Clamp01(Mathf.Max(p.Spread, p.Glide) * unfold);
            float fold = 1f - spread;
            switch (role)
            {
                case FormPartRole.WingRoot:
                {
                    Quaternion q = QMath.Slerp(FoldedRoot, SpreadRoot(p.Glide), QMath.Smooth01(spread));
                    turn = side < 0 ? Mirror(q) : q;
                    break;
                }
                case FormPartRole.WingMid:
                {
                    // The mid panel folds back over the root through the leading-edge side (upward when folded), so a
                    // half-folded wing never swings down toward the road.
                    float a = Mathf.Lerp(-172f, 6f, QMath.Smooth01(spread));
                    turn = QMath.AY(side < 0 ? -a : a);
                    move = new Vector3(0f, -0.03f * fold, 0f);
                    break;
                }
                case FormPartRole.WingTip:
                {
                    float a = Mathf.Lerp(172f, 10f, QMath.Smooth01(spread));
                    turn = QMath.AY(side < 0 ? -a : a);
                    move = new Vector3(0f, -0.03f * fold, 0f);
                    break;
                }
                case FormPartRole.TailVane:
                    turn = QMath.AX(12f * Mathf.Clamp(p.Vane, -1f, 1f));
                    break;
                case FormPartRole.TailFin:
                    turn = QMath.AY(-10f * Mathf.Clamp(p.Steer, -1f, 1f));
                    break;
                case FormPartRole.Emitter:
                    scale = (1f + 0.15f * Mathf.Clamp01(p.Pulse)) * Mathf.Lerp(0.2f, 1f, unfold);
                    break;
                case FormPartRole.Prow:
                    // Retracted onto the nose while the form grows, pushed forward while bracing.
                    move = new Vector3(0f, 0f, ProwTravel * Mathf.Clamp01(p.Brace) - ProwRetract * closed);
                    break;
                case FormPartRole.Plate:
                    // Plates rise out of the hull as the form grows and lift a little while bracing.
                    move = new Vector3(0f, 0.025f * Mathf.Clamp01(p.Brace) - 0.06f * closed, 0f);
                    break;
                case FormPartRole.Spine:
                    turn = QMath.AX(Mathf.Lerp(-12f, 22f, Mathf.Clamp01(p.Flare)) - 45f * closed);
                    break;
                case FormPartRole.Fin:
                {
                    float s = side < 0 ? -1f : 1f;
                    // Fins swing out from along the hull as the form grows, then follow the steering.
                    turn = QMath.AY(s * 50f * closed) * QMath.AY(s * -12f * Mathf.Clamp(p.Steer, -1f, 1f));
                    break;
                }
            }
        }


    }

    /// <summary>A complete fused form for one chassis class at one quality: its parts and how to evaluate them.</summary>
    public sealed class FormBlueprint
    {
        public string Form = "";
        public string ChassisClass = "";
        public bool Low;
        public FusionFrame Frame;
        public readonly List<FormPart> Parts = new List<FormPart>();
        /// <summary>Points the Coilrunner's discharge arc runs through, in form space ("" path for other forms).</summary>
        public readonly List<Vector3> ArcPath = new List<Vector3>();
        /// <summary>Local points the Ventwing glide trails leave from (wing tips are animated; these are the tip part indices).</summary>
        public readonly List<int> TrailParts = new List<int>();

        public int Add(FormPart part)
        {
            Parts.Add(part);
            return Parts.Count - 1;
        }

        public int VertexCount
        {
            get
            {
                int n = 0;
                foreach (FormPart p in Parts) n += p.Mesh.VertexCount;
                return n;
            }
        }

        /// <summary>Every vertex in form space for a pose (root scale included), appended to output.</summary>
        public void Vertices(FormPoseInput pose, List<Vector3> output)
        {
            int count = Parts.Count;
            Vector3[] pos = new Vector3[count];
            Quaternion[] rot = new Quaternion[count];
            float[] scl = new float[count];
            for (int i = 0; i < count; i++)
            {
                FormPart part = Parts[i];
                Vector3 move; Quaternion turn; float s;
                FormPose.Offset(part.Role, part.Side, part.Index, pose, out move, out turn, out s);
                Vector3 lp = part.Position + move;
                Quaternion lr = part.Rotation * turn;
                if (part.Parent >= 0 && part.Parent < i)
                {
                    int pi = part.Parent;
                    pos[i] = pos[pi] + rot[pi] * (lp * scl[pi]);
                    rot[i] = rot[pi] * lr;
                    scl[i] = scl[pi] * s;
                }
                else
                {
                    pos[i] = lp;
                    rot[i] = lr;
                    scl[i] = s;
                }
                List<Vector3> v = part.Mesh.Vertices;
                for (int k = 0; k < v.Count; k++) output.Add(pos[i] + rot[i] * (v[k] * scl[i]));
            }
        }

        /// <summary>
        /// The box the form can occupy, sampled over its poses. transitionOnly covers the fold-to-parked unfold the
        /// fusion check needs (every unfold stage); otherwise every pose (full spread, glide, brace, steering, flare) too.
        /// </summary>
        public void Envelope(bool transitionOnly, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            List<Vector3> pts = new List<Vector3>(VertexCount + 16);
            float spreadMax = transitionOnly ? FormPose.IdleSpread : 1f;
            int steps = 6;
            float[] unfolds = { 0f, 0.5f, 1f };
            for (int s = 0; s <= steps; s++)
            {
                float spread = spreadMax * s / steps;
                for (int g = 0; g < (transitionOnly ? 1 : 2); g++)
                {
                    for (int extreme = 0; extreme < (transitionOnly ? 3 : 5); extreme++)
                    {
                        float unfold = extreme < 3 ? unfolds[extreme] : 1f;
                        FormPoseInput p = new FormPoseInput { Spread = spread, Glide = g, Unfold = unfold, Pulse = 1f };
                        if (extreme == 3) { p.Brace = 1f; p.Steer = 1f; p.Flare = 1f; p.Vane = 1f; }
                        if (extreme == 4) { p.Steer = -1f; p.Vane = -1f; }
                        pts.Clear();
                        Vertices(p, pts);
                        foreach (Vector3 v in pts)
                        {
                            min = Vector3.Min(min, v);
                            max = Vector3.Max(max, v);
                        }
                    }
                }
            }
            if (min.x > max.x) { min = Vector3.zero; max = Vector3.zero; }
        }
    }

    /// <summary>
    /// Builds the three fused forms (Ventwing, Kilnback, Coilrunner) and the chassis trims as pure blueprints for a
    /// chassis class at High or Low detail. Low keeps every outline (wing span, hump, prow, spines) and drops facets and
    /// small repeated details. Engine-free: the runtime turns the parts into meshes, the tests check them offline.
    /// </summary>
    public static partial class FusionFormGeometry
    {
        public static FormBlueprint Build(string form, string chassisClass, bool low)
        {
            FormBlueprint bp = new FormBlueprint { Form = form ?? "", ChassisClass = chassisClass ?? FusionIds.Sport, Low = low };
            bp.Frame = FusionFrame.For(bp.ChassisClass);
            switch (form)
            {
                case FusionIds.Ventwing: BuildVentwing(bp); break;
                case FusionIds.Kilnback: BuildKilnback(bp); break;
                case FusionIds.Coilrunner: BuildCoilrunner(bp); break;
            }
            AscensionFairing04I(bp);
            return bp;
        }

        // ================================================================== mesh helpers (offline safe)

        private static FormPart NewPart(string name, Vector3 pos, FormPartRole role, int side, int parent)
        {
            FormPart p = new FormPart { Name = name, Position = pos, Role = role, Side = side, Parent = parent };
            p.Mesh.FixedUV = new Vector2(0.5f, 0.5f);
            return p;
        }

        /// <summary>A tapered faceted rod from a to b (radii at each end).</summary>
        private static void Rod(MeshBuilder b, int sub, Vector3 a, Vector3 c, float ra, float rb, int sides)
        {
            Vector3 d = c - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Quaternion rot = QMath.FromTo(Vector3.up, d / len);
            b.Cylinder(sub, a, ra, rb, len, sides, rot, true, UVMode.Fixed);
        }

        /// <summary>A crystal spike (a faceted cone) from a base to a tip.</summary>
        private static void Spike(MeshBuilder b, int sub, Vector3 baseCenter, Vector3 tip, float radius, int sides)
        {
            Rod(b, sub, baseCenter, tip, radius, radius * 0.06f, sides);
        }

        private static void Box(MeshBuilder b, int sub, Vector3 center, Vector3 size, Quaternion rot)
        {
            b.Box(sub, center, size, rot, UVMode.Fixed);
        }

        /// <summary>An eight-corner solid (bottom back-left, back-right, front-right, front-left, then the top in the same order).</summary>
        private static void Hexa(MeshBuilder b, int sub, Vector3[] c)
        {
            VehicleModels.Hexa(b, sub, c);
        }

        /// <summary>The mirror image of an eight-corner solid (left side), reordered so its faces still point outward.</summary>
        private static Vector3[] MirrorHexa(Vector3[] c)
        {
            Vector3[] m = new Vector3[8];
            int[] map = { 1, 0, 3, 2, 5, 4, 7, 6 };
            for (int i = 0; i < 8; i++) m[i] = FormPose.Mirror(c[map[i]]);
            return m;
        }

        private static void HexaSided(MeshBuilder b, int sub, Vector3[] rightCorners, int side)
        {
            Hexa(b, sub, side < 0 ? MirrorHexa(rightCorners) : rightCorners);
        }

        /// <summary>
        /// A thin tapered panel along +X from the hinge (span len), chord along Z with the leading edge forward of the
        /// hinge by lead times the chord, thickness th, swept back by sweep metres at the tip. side -1 builds the mirror.
        /// </summary>
        private static void Panel(MeshBuilder b, int sub, float len, float chordRoot, float chordTip, float th, float sweep, float lead, int side)
        {
            float h = th * 0.5f;
            float r0 = chordRoot * lead, r1 = -chordRoot * (1f - lead);
            float t0 = chordTip * lead - sweep, t1 = -chordTip * (1f - lead) - sweep;
            Vector3[] c =
            {
                new Vector3(0f, -h, r1), new Vector3(len, -h * 0.6f, t1), new Vector3(len, -h * 0.6f, t0), new Vector3(0f, -h, r0),
                new Vector3(0f, h, r1), new Vector3(len, h * 0.6f, t1), new Vector3(len, h * 0.6f, t0), new Vector3(0f, h, r0)
            };
            HexaSided(b, sub, c, side);
        }

        /// <summary>
        /// One half of a gable-shaped shell plate over the hull between z0 and z1: from a low outer edge (half-width,
        /// bottom) up to the ridge at the centre, thickness th. side -1 builds the mirror.
        /// </summary>
        private static void ShellHalf(MeshBuilder b, int sub, float z0, float z1, float outer0, float outer1, float low0, float low1, float ridge0, float ridge1, float th, int side)
        {
            Vector3[] c =
            {
                new Vector3(0f, ridge0 - th, z0), new Vector3(outer0, low0, z0), new Vector3(outer1, low1, z1), new Vector3(0f, ridge1 - th, z1),
                new Vector3(0f, ridge0, z0), new Vector3(outer0 + th * 0.7f, low0 + th * 0.4f, z0), new Vector3(outer1 + th * 0.7f, low1 + th * 0.4f, z1), new Vector3(0f, ridge1, z1)
            };
            // Corner order for Hexa: bottom back-left, back-right, front-right, front-left (left = toward the centre here).
            HexaSided(b, sub, c, side);
        }

        private static float Scale(FusionFrame f) { return f.Length / 2.15f; }
    }
}
