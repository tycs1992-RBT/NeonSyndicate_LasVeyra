using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The collider a machine part carries.</summary>
    public enum MachineCollider
    {
        None = 0,
        /// <summary>ColliderSize is the box size.</summary>
        Box = 1,
        /// <summary>ColliderSize.x is the radius.</summary>
        Sphere = 2,
        /// <summary>ColliderSize.x radius, .y height, .z the axis (0 x, 1 y, 2 z).</summary>
        Capsule = 3
    }

    /// <summary>One articulated part of a procedural machine: a pivot under its parent, geometry in its own frame, a hit volume.</summary>
    public sealed class MachinePartSpec
    {
        public string Name = "";
        /// <summary>Index of the parent part (-1: the body root).</summary>
        public int Parent = -1;
        /// <summary>Pivot position in the parent's frame (metres, +z forward, +y up).</summary>
        public Vector3 Pivot;
        /// <summary>Geometry around the pivot, one submesh per machine material (UnderBossFx.Sub*).</summary>
        public MeshBuilder Mesh = new MeshBuilder(UnderBossFx.SubCount);
        public MachineCollider Collider;
        public Vector3 ColliderCenter;
        public Vector3 ColliderSize;
        public UBossPart Hit;
        public int HitIndex;
        /// <summary>A dedicated glowing part whose emission the fight animates (valve wheel, seams, core, crown, regulator).</summary>
        public bool Glow;
    }

    /// <summary>A whole procedural machine: its parts in build order (parents before children) and its rough size.</summary>
    public sealed class MachineSpec
    {
        public readonly List<MachinePartSpec> Parts = new List<MachinePartSpec>();
        public float Height;
        public float Radius;

        public int Find(string name)
        {
            for (int i = 0; i < Parts.Count; i++) if (Parts[i].Name == name) return i;
            return -1;
        }

        public MachinePartSpec Part(string name)
        {
            int i = Find(name);
            return i >= 0 ? Parts[i] : null;
        }

        /// <summary>Triangles across every part (budgets and tests).</summary>
        public int VertexCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Parts.Count; i++) n += Parts[i].Mesh.VertexCount;
                return n;
            }
        }
    }

    /// <summary>
    /// The Underheart bosses' machines and arena props as plain geometry (pure: MeshBuilder lists and QMath rotations,
    /// no engine calls, so tests can check them offline): the Valve Guardian (a squat boiler with a valve wheel on its
    /// back), the Vaultbreaker (a six-limbed furnace walker with front plates, flank seams and a core hatch), the Kiln
    /// Regent (a pedestal colossus with a caged regulator and an emitter crown), the furnaces, lures, cooled plates and
    /// the Choir's props. Silhouettes read without colour: drum and wheel, low walker with hammer arms, towering crown.
    /// </summary>
    public static class UnderBossShapes04E
    {
        private const int Iron = UnderBossFx.SubIron, Brass = UnderBossFx.SubBrass, Heat = UnderBossFx.SubHeat;
        private const int Dark = UnderBossFx.SubDark, Core = UnderBossFx.SubCore, Cool = UnderBossFx.SubCoolant;

        // ================================================================== kit

        private static MachinePartSpec Part(MachineSpec s, string name, int parent, Vector3 pivot)
        {
            MachinePartSpec p = new MachinePartSpec();
            p.Name = name;
            p.Parent = parent;
            p.Pivot = pivot;
            p.Mesh.FixedUV = new Vector2(0.5f, 0.5f);
            s.Parts.Add(p);
            return p;
        }

        private static void Box(MachinePartSpec p, int sub, Vector3 c, Vector3 size)
        {
            p.Mesh.Box(sub, c, size, QMath.Identity, UVMode.Unit);
        }

        private static void Box(MachinePartSpec p, int sub, Vector3 c, Vector3 size, Quaternion r)
        {
            p.Mesh.Box(sub, c, size, r, UVMode.Unit);
        }

        private static void Cyl(MachinePartSpec p, int sub, Vector3 baseCenter, float r0, float r1, float h, int sides, Quaternion rot)
        {
            p.Mesh.Cylinder(sub, baseCenter, r0, r1, h, sides, rot, true, UVMode.Unit);
        }

        private static void Ball(MachinePartSpec p, int sub, Vector3 c, Vector3 radii)
        {
            p.Mesh.Sphere(sub, c, radii, 7, 10, QMath.Identity);
        }

        /// <summary>A pipe between two points (a cylinder turned onto the segment).</summary>
        private static void Pipe(MachinePartSpec p, int sub, Vector3 a, Vector3 b, float r, int sides)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            p.Mesh.Cylinder(sub, a, r, r, len, sides, QMath.FromTo(Vector3.up, d / len), true, UVMode.Unit);
        }

        /// <summary>A ring of small boxes (rims, bands seen edge-on) of radius around c in the plane of rot's x and z axes.</summary>
        private static void Ring(MachinePartSpec p, int sub, Vector3 c, float radius, float thick, int segments, Quaternion rot)
        {
            float seg = 2f * Mathf.PI * radius / segments * 1.08f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                Quaternion local = QMath.AY(a);
                Vector3 off = rot * (local * new Vector3(0f, 0f, radius));
                p.Mesh.Box(sub, c + off, new Vector3(seg, thick, thick), rot * local, UVMode.Unit);
            }
        }

        /// <summary>Rivets: count small brass studs around a circle of radius about c (plane of rot's x and z).</summary>
        private static void Rivets(MachinePartSpec p, Vector3 c, float radius, int count, Quaternion rot)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * 360f / count;
                Vector3 off = rot * (QMath.AY(a) * new Vector3(0f, 0f, radius));
                p.Mesh.Box(Brass, c + off, new Vector3(0.07f, 0.07f, 0.07f), rot * QMath.AY(a), UVMode.Unit);
            }
        }

        private static void HitBox(MachinePartSpec p, Vector3 c, Vector3 size, UBossPart hit, int index)
        {
            p.Collider = MachineCollider.Box;
            p.ColliderCenter = c;
            p.ColliderSize = size;
            p.Hit = hit;
            p.HitIndex = index;
        }

        private static void HitSphere(MachinePartSpec p, Vector3 c, float r, UBossPart hit, int index)
        {
            p.Collider = MachineCollider.Sphere;
            p.ColliderCenter = c;
            p.ColliderSize = new Vector3(r, 0f, 0f);
            p.Hit = hit;
            p.HitIndex = index;
        }

        private static void HitCapsule(MachinePartSpec p, Vector3 c, float r, float h, int axis, UBossPart hit, int index)
        {
            p.Collider = MachineCollider.Capsule;
            p.ColliderCenter = c;
            p.ColliderSize = new Vector3(r, h, axis);
            p.Hit = hit;
            p.HitIndex = index;
        }

        // ================================================================== Valve Guardian

        /// <summary>
        /// The Valve Guardian (about 3.7 m): an upright boiler drum on four stub legs, brass bands and a pressure gauge
        /// on its face, a steam nozzle below the gauge, two pipe arms ending in clamps, twin stacks, and the valve wheel
        /// on its back (its weak point: it spins and glows while it vents after a steam jet).
        /// </summary>
        public static MachineSpec ValveGuardian()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec hull = Part(s, "Hull", -1, new Vector3(0f, 0.95f, 0f));
            Cyl(hull, Iron, Vector3.zero, 1.15f, 1.15f, 1.7f, 16, QMath.Identity);
            Ball(hull, Iron, new Vector3(0f, 1.7f, 0f), new Vector3(1.15f, 0.5f, 1.15f));
            for (int i = 0; i < 3; i++) Cyl(hull, Brass, new Vector3(0f, 0.2f + i * 0.6f, 0f), 1.2f, 1.2f, 0.12f, 16, QMath.Identity);
            for (int i = 0; i < 3; i++) Rivets(hull, new Vector3(0f, 0.26f + i * 0.6f, 0f), 1.23f, 18, QMath.Identity);
            Cyl(hull, Brass, new Vector3(0f, 1.25f, 1.08f), 0.38f, 0.38f, 0.12f, 14, QMath.AX(90f));
            Cyl(hull, Heat, new Vector3(0f, 1.25f, 1.2f), 0.29f, 0.29f, 0.02f, 14, QMath.AX(90f));
            Box(hull, Dark, new Vector3(0.07f, 1.3f, 1.23f), new Vector3(0.05f, 0.22f, 0.02f), QMath.AZ(-35f));
            Cyl(hull, Brass, new Vector3(0f, 0.6f, 1.05f), 0.3f, 0.2f, 0.75f, 12, QMath.AX(90f));
            Cyl(hull, Heat, new Vector3(0f, 0.6f, 1.78f), 0.21f, 0.21f, 0.05f, 12, QMath.AX(90f));
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Pipe(hull, Iron, new Vector3(sd * 0.45f, 1.85f, -0.3f), new Vector3(sd * 0.45f, 2.75f, -0.35f), 0.16f, 10);
                Cyl(hull, Heat, new Vector3(sd * 0.45f, 2.75f, -0.35f), 0.17f, 0.2f, 0.08f, 10, QMath.Identity);
                Cyl(hull, Dark, new Vector3(sd * 1.12f, 1.3f, 0.2f), 0.3f, 0.3f, 0.18f, 10, QMath.AZ(-sd * 90f));
            }
            Cyl(hull, Iron, new Vector3(0f, 1.05f, -1.08f), 0.36f, 0.36f, 0.25f, 12, QMath.AX(-90f));
            HitBox(hull, new Vector3(0f, 0.9f, 0f), new Vector3(2.3f, 2.4f, 2.3f), UBossPart.Body, 0);

            MachinePartSpec valve = Part(s, "Valve", 0, new Vector3(0f, 1.05f, -1.42f));
            valve.Glow = true;
            Ring(valve, Core, Vector3.zero, 0.7f, 0.1f, 14, QMath.AX(90f));
            for (int i = 0; i < 6; i++)
            {
                Quaternion r = QMath.AZ(i * 60f);
                Box(valve, Brass, r * new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.66f, 0.06f), r);
            }
            Ball(valve, Core, Vector3.zero, new Vector3(0.2f, 0.2f, 0.14f));
            HitSphere(valve, Vector3.zero, 0.75f, UBossPart.Weak, 0);

            for (int sd = -1; sd <= 1; sd += 2)
            {
                string side = sd < 0 ? "L" : "R";
                MachinePartSpec arm = Part(s, "Arm" + side, 0, new Vector3(sd * 1.22f, 1.3f, 0.2f));
                Ball(arm, Brass, Vector3.zero, new Vector3(0.24f, 0.24f, 0.24f));
                Vector3 elbow = new Vector3(sd * 0.2f, -0.6f, 0.55f);
                Pipe(arm, Iron, Vector3.zero, elbow, 0.17f, 10);
                Ball(arm, Brass, elbow, new Vector3(0.21f, 0.21f, 0.21f));
                HitCapsule(arm, elbow * 0.5f, 0.24f, 0.95f, 1, UBossPart.Limb, 0);
                MachinePartSpec fore = Part(s, "Fore" + side, s.Find("Arm" + side), elbow);
                Pipe(fore, Iron, Vector3.zero, new Vector3(0f, -0.1f, 0.75f), 0.14f, 10);
                Box(fore, Dark, new Vector3(0.13f, -0.1f, 0.92f), new Vector3(0.08f, 0.14f, 0.38f), QMath.AY(-12f));
                Box(fore, Dark, new Vector3(-0.13f, -0.1f, 0.92f), new Vector3(0.08f, 0.14f, 0.38f), QMath.AY(12f));
                Cyl(fore, Heat, new Vector3(0f, -0.1f, 0.74f), 0.15f, 0.15f, 0.05f, 10, QMath.AX(90f));
                HitCapsule(fore, new Vector3(0f, -0.08f, 0.5f), 0.22f, 1f, 2, UBossPart.Limb, 0);
            }
            string[] legs = { "LegFL", "LegFR", "LegBL", "LegBR" };
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 0.8f;
                float z = i < 2 ? 0.55f : -0.55f;
                MachinePartSpec leg = Part(s, legs[i], -1, new Vector3(x, 0.95f, z));
                Ball(leg, Brass, Vector3.zero, new Vector3(0.24f, 0.24f, 0.24f));
                Box(leg, Iron, new Vector3(0f, -0.45f, 0f), new Vector3(0.34f, 0.9f, 0.34f));
                Box(leg, Dark, new Vector3(0f, -0.88f, 0.06f), new Vector3(0.56f, 0.14f, 0.72f));
            }
            s.Height = 3.75f;
            s.Radius = 1.4f;
            return s;
        }

        // ================================================================== Vaultbreaker

        /// <summary>
        /// The Vaultbreaker (about 5 m tall, 7 m long): a furnace hull on four jointed legs with two hammer arms in front,
        /// a sensor head, sloped front plates over the chest, a core behind sliding hatch doors under them, glowing
        /// seams along both flanks (and one across the back that wakes in phase two), exhaust stacks and a rear grill.
        /// </summary>
        public static MachineSpec Vaultbreaker()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec hull = Part(s, "Hull", -1, new Vector3(0f, 2.6f, 0f));
            Box(hull, Iron, Vector3.zero, new Vector3(3f, 2f, 4.4f));
            Box(hull, Dark, new Vector3(0f, 1.08f, -0.2f), new Vector3(2.6f, 0.2f, 3.6f));
            Box(hull, Dark, new Vector3(0f, -1.15f, 0f), new Vector3(2f, 0.3f, 3.6f));
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Box(hull, Dark, new Vector3(sd * 1.56f, -0.72f, 0f), new Vector3(0.12f, 0.6f, 4f));
                Box(hull, Dark, new Vector3(sd * 1.56f, 0.72f, 0f), new Vector3(0.12f, 0.42f, 4f));
                Box(hull, Brass, new Vector3(sd * 1.5f, 1.02f, 0f), new Vector3(0.09f, 0.09f, 4.4f));
                for (int k = 0; k < 2; k++)
                {
                    Pipe(hull, Iron, new Vector3(sd * 0.6f, 1f, -1.6f + k * 0.7f), new Vector3(sd * 0.6f, 2.3f, -1.75f + k * 0.7f), 0.2f, 10);
                    Cyl(hull, Heat, new Vector3(sd * 0.6f, 2.3f, -1.75f + k * 0.7f), 0.21f, 0.25f, 0.1f, 10, QMath.Identity);
                }
            }
            Box(hull, Heat, new Vector3(0f, 0.1f, -2.22f), new Vector3(1.8f, 1f, 0.05f));
            for (int k = 0; k < 6; k++) Box(hull, Dark, new Vector3(-0.75f + k * 0.3f, 0.1f, -2.25f), new Vector3(0.07f, 1.05f, 0.06f));
            Box(hull, Dark, new Vector3(0f, -0.55f, 1.95f), new Vector3(1.7f, 1f, 0.6f));
            HitBox(hull, new Vector3(0f, 0.1f, -0.2f), new Vector3(3f, 2.2f, 4f), UBossPart.Body, 0);
            const int hullIndex = 0;

            MachinePartSpec head = Part(s, "Head", hullIndex, new Vector3(0f, 1f, 2f));
            Box(head, Iron, new Vector3(0f, 0.25f, 0.1f), new Vector3(1.1f, 0.55f, 0.8f));
            Box(head, Heat, new Vector3(0f, 0.32f, 0.51f), new Vector3(0.9f, 0.12f, 0.04f));
            Ball(head, Core, new Vector3(-0.3f, 0.12f, 0.52f), new Vector3(0.09f, 0.09f, 0.05f));
            Ball(head, Core, new Vector3(0.3f, 0.12f, 0.52f), new Vector3(0.09f, 0.09f, 0.05f));
            Ball(head, Core, new Vector3(0f, 0.1f, 0.55f), new Vector3(0.11f, 0.11f, 0.06f));
            Pipe(head, Brass, new Vector3(-0.4f, 0.5f, -0.1f), new Vector3(-0.55f, 1.1f, -0.3f), 0.035f, 6);
            Pipe(head, Brass, new Vector3(0.4f, 0.5f, -0.1f), new Vector3(0.55f, 1f, -0.3f), 0.035f, 6);
            HitBox(head, new Vector3(0f, 0.25f, 0.1f), new Vector3(1.15f, 0.6f, 0.85f), UBossPart.Body, 0);

            MachinePartSpec plates = Part(s, "FrontPlates", hullIndex, new Vector3(0f, 0.35f, 2.2f));
            Box(plates, Dark, new Vector3(0f, 0.15f, 0.12f), new Vector3(3.4f, 1.1f, 0.35f), QMath.AX(-14f));
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Box(plates, Dark, new Vector3(sd * 1.62f, 0f, -0.35f), new Vector3(0.3f, 1.4f, 1f), QMath.AY(sd * 20f));
                for (int k = 0; k < 4; k++) Box(plates, Brass, new Vector3(sd * (0.4f + k * 0.35f), 0.55f, 0.18f), new Vector3(0.08f, 0.08f, 0.08f));
            }
            HitBox(plates, new Vector3(0f, 0.15f, 0.15f), new Vector3(3.5f, 1.2f, 0.6f), UBossPart.FrontPlate, 0);

            MachinePartSpec core = Part(s, "Core", hullIndex, new Vector3(0f, -0.55f, 1.85f));
            core.Glow = true;
            Ball(core, Core, Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f));
            Ring(core, Brass, Vector3.zero, 0.5f, 0.07f, 12, QMath.AX(90f));
            HitSphere(core, Vector3.zero, 0.5f, UBossPart.Core, 0);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                MachinePartSpec door = Part(s, sd < 0 ? "HatchL" : "HatchR", hullIndex, new Vector3(sd * 0.42f, -0.55f, 2.42f));
                Box(door, Dark, Vector3.zero, new Vector3(0.84f, 0.74f, 0.14f));
                Box(door, Brass, new Vector3(-sd * 0.36f, 0f, 0.08f), new Vector3(0.06f, 0.7f, 0.04f));
                HitBox(door, Vector3.zero, new Vector3(0.86f, 0.76f, 0.2f), UBossPart.FrontPlate, 0);
            }
            for (int sd = -1; sd <= 1; sd += 2)
            {
                MachinePartSpec seam = Part(s, sd < 0 ? "SeamL" : "SeamR", hullIndex, new Vector3(sd * 1.53f, 0.05f, 0f));
                seam.Glow = true;
                Box(seam, Core, Vector3.zero, new Vector3(0.06f, 0.28f, 3.2f));
                for (int k = 0; k < 7; k++) Box(seam, Dark, new Vector3(sd * 0.02f, 0f, -1.5f + k * 0.5f), new Vector3(0.08f, 0.36f, 0.06f));
                HitBox(seam, Vector3.zero, new Vector3(0.32f, 0.5f, 3.3f), UBossPart.Seam, sd < 0 ? 0 : 1);
            }
            MachinePartSpec rear = Part(s, "SeamRear", hullIndex, new Vector3(0f, 0.75f, -2.23f));
            rear.Glow = true;
            Box(rear, Core, Vector3.zero, new Vector3(2.2f, 0.26f, 0.06f));
            HitBox(rear, Vector3.zero, new Vector3(2.3f, 0.5f, 0.32f), UBossPart.Seam, 2);

            string[] legs = { "FL", "FR", "BL", "BR" };
            for (int i = 0; i < 4; i++)
            {
                float sd = i % 2 == 0 ? -1f : 1f;
                float z = i < 2 ? 1.5f : -1.5f;
                MachinePartSpec hip = Part(s, "Thigh" + legs[i], hullIndex, new Vector3(sd * 1.5f, -0.6f, z));
                Ball(hip, Brass, Vector3.zero, new Vector3(0.36f, 0.36f, 0.36f));
                Box(hip, Iron, new Vector3(sd * 0.5f, -0.25f, 0f), new Vector3(1.15f, 0.45f, 0.5f), QMath.AZ(sd * 26.5f));
                int hipIndex = s.Parts.Count - 1;
                MachinePartSpec shin = Part(s, "Shin" + legs[i], hipIndex, new Vector3(sd * 1f, -0.5f, 0f));
                Ball(shin, Brass, Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f));
                Box(shin, Iron, new Vector3(0f, -0.7f, 0f), new Vector3(0.45f, 1.4f, 0.45f));
                Box(shin, Dark, new Vector3(0f, -1.38f, 0.15f), new Vector3(0.8f, 0.22f, 1f));
                Box(shin, Heat, new Vector3(0f, -0.4f, 0.24f), new Vector3(0.2f, 0.5f, 0.03f));
                HitCapsule(shin, new Vector3(0f, -0.7f, 0f), 0.3f, 1.5f, 1, UBossPart.Limb, i);
            }
            for (int sd = -1; sd <= 1; sd += 2)
            {
                string side = sd < 0 ? "L" : "R";
                MachinePartSpec arm = Part(s, "Arm" + side, hullIndex, new Vector3(sd * 1.75f, 0.5f, 1.5f));
                Ball(arm, Brass, Vector3.zero, new Vector3(0.46f, 0.46f, 0.46f));
                Box(arm, Iron, new Vector3(0f, -0.6f, 0.4f), new Vector3(0.55f, 1.4f, 0.55f), QMath.AX(-34f));
                HitCapsule(arm, new Vector3(0f, -0.6f, 0.4f), 0.35f, 1.5f, 1, UBossPart.Limb, 0);
                int armIndex = s.Parts.Count - 1;
                MachinePartSpec fore = Part(s, "Fore" + side, armIndex, new Vector3(0f, -1.2f, 0.8f));
                Ball(fore, Brass, Vector3.zero, new Vector3(0.36f, 0.36f, 0.36f));
                Box(fore, Iron, new Vector3(0f, -0.1f, 0.7f), new Vector3(0.5f, 0.5f, 1.4f));
                Box(fore, Dark, new Vector3(0f, -0.2f, 1.5f), new Vector3(0.95f, 0.95f, 1.1f));
                Box(fore, Heat, new Vector3(0f, -0.2f, 2.06f), new Vector3(0.75f, 0.12f, 0.03f));
                for (int k = -1; k <= 1; k++) Box(fore, Brass, new Vector3(k * 0.3f, 0.32f, 1.5f), new Vector3(0.12f, 0.18f, 0.5f));
                HitBox(fore, new Vector3(0f, -0.2f, 1.2f), new Vector3(0.95f, 0.95f, 1.8f), UBossPart.Limb, 0);
            }
            s.Height = 4.9f;
            s.Radius = 3.4f;
            return s;
        }

        // ================================================================== Kiln Regent

        /// <summary>
        /// The Kiln Regent (about 8 m): an octagonal furnace pedestal, a turning torso with a caged regulator in its
        /// chest, pauldrons and twin chimneys, a masked head with a slit of light under an emitter crown of six prongs,
        /// and two long arms ending in kiln hammers. The cage lifts in the finale to bare the regulator.
        /// </summary>
        public static MachineSpec KilnRegent()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec ped = Part(s, "Pedestal", -1, Vector3.zero);
            Cyl(ped, Iron, Vector3.zero, 3.6f, 3.4f, 0.9f, 8, QMath.AY(22.5f));
            Cyl(ped, Dark, new Vector3(0f, 0.9f, 0f), 3f, 2.7f, 0.7f, 8, QMath.AY(22.5f));
            Cyl(ped, Brass, new Vector3(0f, 0.84f, 0f), 3.66f, 3.66f, 0.12f, 8, QMath.AY(22.5f));
            for (int i = 0; i < 8; i++)
            {
                Quaternion r = QMath.AY(i * 45f);
                Box(ped, Heat, r * new Vector3(0f, 1.25f, 2.86f), new Vector3(1.2f, 0.25f, 0.05f), r);
            }
            HitBox(ped, new Vector3(0f, 0.8f, 0f), new Vector3(6.2f, 1.6f, 6.2f), UBossPart.Body, 0);

            MachinePartSpec torso = Part(s, "Torso", -1, new Vector3(0f, 1.6f, 0f));
            Cyl(torso, Iron, Vector3.zero, 1.7f, 2f, 1.2f, 12, QMath.Identity);
            Box(torso, Iron, new Vector3(0f, 2.2f, 0f), new Vector3(4f, 2.6f, 2.3f));
            Box(torso, Dark, new Vector3(0f, 3.6f, 0f), new Vector3(4.4f, 0.3f, 2.5f));
            Box(torso, Heat, new Vector3(0f, 1f, 1.02f), new Vector3(2.4f, 0.5f, 0.05f));
            Box(torso, Dark, new Vector3(0f, 2.2f, 1.16f), new Vector3(2.4f, 2.3f, 0.12f));
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Box(torso, Dark, new Vector3(sd * 2.35f, 3.1f, 0f), new Vector3(1.1f, 0.9f, 1.6f), QMath.AZ(-sd * 15f));
                Box(torso, Brass, new Vector3(sd * 2.02f, 2.2f, 0f), new Vector3(0.08f, 2.6f, 2.32f));
                Pipe(torso, Iron, new Vector3(sd * 0.9f, 3.5f, -0.9f), new Vector3(sd * 0.95f, 5.7f, -1.05f), 0.32f, 10);
                Cyl(torso, Heat, new Vector3(sd * 0.95f, 5.7f, -1.05f), 0.33f, 0.4f, 0.14f, 10, QMath.Identity);
            }
            HitBox(torso, new Vector3(0f, 2.2f, 0f), new Vector3(4f, 2.6f, 2.3f), UBossPart.Body, 0);
            const int torsoIndex = 1;

            MachinePartSpec reg = Part(s, "Regulator", torsoIndex, new Vector3(0f, 2.2f, 1.2f));
            reg.Glow = true;
            Ball(reg, Core, Vector3.zero, new Vector3(0.85f, 0.85f, 0.6f));
            Ring(reg, Brass, Vector3.zero, 0.95f, 0.09f, 16, QMath.AX(90f));
            Ring(reg, Brass, Vector3.zero, 0.7f, 0.06f, 12, QMath.Identity);
            HitSphere(reg, Vector3.zero, 0.9f, UBossPart.Regulator, 0);

            MachinePartSpec cage = Part(s, "Cage", torsoIndex, new Vector3(0f, 2.2f, 1.95f));
            for (int k = 0; k < 7; k++) Box(cage, Dark, new Vector3(-0.9f + k * 0.3f, 0f, 0f), new Vector3(0.12f, 2f, 0.14f));
            Box(cage, Brass, new Vector3(0f, 1f, 0f), new Vector3(2.1f, 0.14f, 0.18f));
            Box(cage, Brass, new Vector3(0f, -1f, 0f), new Vector3(2.1f, 0.14f, 0.18f));
            HitBox(cage, Vector3.zero, new Vector3(2.1f, 2.1f, 0.32f), UBossPart.Body, 0);

            MachinePartSpec head = Part(s, "Head", torsoIndex, new Vector3(0f, 3.9f, 0.1f));
            Cyl(head, Brass, Vector3.zero, 0.85f, 0.75f, 0.25f, 12, QMath.Identity);
            Box(head, Iron, new Vector3(0f, 0.6f, 0f), new Vector3(1.3f, 1.1f, 1.2f));
            Box(head, Dark, new Vector3(0f, 0.2f, 0.45f), new Vector3(1.1f, 0.35f, 0.4f));
            Box(head, Heat, new Vector3(0f, 0.7f, 0.61f), new Vector3(0.9f, 0.1f, 0.04f));
            HitBox(head, new Vector3(0f, 0.6f, 0f), new Vector3(1.35f, 1.15f, 1.25f), UBossPart.Body, 0);
            int headIndex = s.Parts.Count - 1;

            MachinePartSpec crown = Part(s, "Crown", headIndex, new Vector3(0f, 1.15f, 0f));
            crown.Glow = true;
            Ring(crown, Brass, Vector3.zero, 0.75f, 0.12f, 12, QMath.Identity);
            for (int i = 0; i < 6; i++)
            {
                Quaternion r = QMath.AY(i * 60f);
                Vector3 b = r * new Vector3(0f, 0f, 0.7f);
                Box(crown, Iron, b + r * new Vector3(0f, 0.35f, 0.08f), new Vector3(0.14f, 0.7f, 0.14f), r * QMath.AX(14f));
                Ball(crown, Core, b + r * new Vector3(0f, 0.72f, 0.17f), new Vector3(0.12f, 0.12f, 0.12f));
            }
            HitSphere(crown, new Vector3(0f, 0.35f, 0f), 0.95f, UBossPart.Crown, 0);

            for (int sd = -1; sd <= 1; sd += 2)
            {
                string side = sd < 0 ? "L" : "R";
                MachinePartSpec arm = Part(s, "Arm" + side, torsoIndex, new Vector3(sd * 2.5f, 3f, 0f));
                Ball(arm, Brass, Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f));
                Box(arm, Iron, new Vector3(0f, -1.1f, 0f), new Vector3(0.75f, 2.2f, 0.75f));
                HitCapsule(arm, new Vector3(0f, -1.1f, 0f), 0.45f, 2.3f, 1, UBossPart.Limb, 0);
                int armIndex = s.Parts.Count - 1;
                MachinePartSpec fore = Part(s, "Fore" + side, armIndex, new Vector3(0f, -2.2f, 0f));
                Ball(fore, Brass, Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f));
                Box(fore, Iron, new Vector3(0f, -0.9f, 0.25f), new Vector3(0.7f, 1.8f, 0.7f), QMath.AX(15f));
                Box(fore, Dark, new Vector3(0f, -2.1f, 0.5f), new Vector3(1.3f, 1.1f, 1.5f));
                Box(fore, Heat, new Vector3(0f, -2.66f, 0.5f), new Vector3(1.1f, 0.04f, 1.2f));
                HitBox(fore, new Vector3(0f, -1.5f, 0.4f), new Vector3(1.3f, 2.6f, 1.5f), UBossPart.Limb, 0);
            }
            s.Height = 8f;
            s.Radius = 3.6f;
            return s;
        }

        // ================================================================== arena props

        /// <summary>A furnace pillar for the Regent's arena (4.6 m): banded drum, a grill facing +z and a vent valve (the shootable part).</summary>
        public static MachineSpec Furnace()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, Vector3.zero);
            Cyl(body, Iron, Vector3.zero, 1.25f, 1.1f, 4.2f, 10, QMath.Identity);
            Cyl(body, Dark, new Vector3(0f, 4.2f, 0f), 0.7f, 0.55f, 0.5f, 10, QMath.Identity);
            for (int i = 0; i < 3; i++) Cyl(body, Brass, new Vector3(0f, 0.5f + i * 1.5f, 0f), 1.3f, 1.3f, 0.12f, 10, QMath.Identity);
            HitBox(body, new Vector3(0f, 2.2f, 0f), new Vector3(2.3f, 4.4f, 2.3f), UBossPart.Body, 0);
            MachinePartSpec grill = Part(s, "Grill", 0, new Vector3(0f, 2.6f, 1.15f));
            grill.Glow = true;
            Box(grill, Heat, Vector3.zero, new Vector3(1.2f, 1.4f, 0.05f));
            for (int k = 0; k < 5; k++) Box(grill, Dark, new Vector3(-0.5f + k * 0.25f, 0f, 0.04f), new Vector3(0.06f, 1.45f, 0.06f));
            MachinePartSpec vent = Part(s, "Vent", 0, new Vector3(0f, 1.25f, 1.2f));
            vent.Glow = true;
            Ring(vent, Brass, Vector3.zero, 0.42f, 0.08f, 10, QMath.AX(90f));
            Cyl(vent, Core, new Vector3(0f, 0f, -0.05f), 0.36f, 0.36f, 0.1f, 12, QMath.AX(90f));
            HitBox(vent, new Vector3(0f, 0f, 0.05f), new Vector3(1f, 1f, 0.4f), UBossPart.Weak, 0);
            s.Height = 4.7f;
            s.Radius = 1.3f;
            return s;
        }

        /// <summary>An extraction lure (3 m): a tripod mast holding a burning crystal in a three-prong claw with coil rings.</summary>
        public static MachineSpec Lure()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                Quaternion r = QMath.AY(i * 120f);
                Pipe(body, Dark, r * new Vector3(0f, 0f, 0.7f), new Vector3(0f, 1f, 0f), 0.06f, 6);
            }
            Pipe(body, Iron, new Vector3(0f, 0.9f, 0f), new Vector3(0f, 2.4f, 0f), 0.09f, 8);
            for (int i = 0; i < 2; i++) Ring(body, Brass, new Vector3(0f, 1.5f + i * 0.4f, 0f), 0.26f, 0.06f, 10, QMath.Identity);
            for (int i = 0; i < 3; i++)
            {
                Quaternion r = QMath.AY(i * 120f);
                Box(body, Dark, r * new Vector3(0f, 2.7f, 0.25f), new Vector3(0.08f, 0.6f, 0.08f), r * QMath.AX(-25f));
            }
            HitBox(body, new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 2.6f, 0.5f), UBossPart.Body, 0);
            MachinePartSpec crystal = Part(s, "Crystal", 0, new Vector3(0f, 2.75f, 0f));
            crystal.Glow = true;
            Ball(crystal, Core, Vector3.zero, new Vector3(0.3f, 0.42f, 0.3f));
            HitSphere(crystal, Vector3.zero, 0.55f, UBossPart.Weak, 0);
            s.Height = 3.2f;
            s.Radius = 0.8f;
            return s;
        }

        /// <summary>A cooled crystal plate on the floor (radius in metres): a frosted disc with a brass rim and crystal spikes.</summary>
        public static MachineSpec CooledPlate(float radius)
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec plate = Part(s, "Plate", -1, Vector3.zero);
            plate.Glow = true;
            Cyl(plate, Cool, Vector3.zero, radius, radius * 0.97f, 0.1f, 14, QMath.Identity);
            Ring(plate, Brass, new Vector3(0f, 0.08f, 0f), radius + 0.05f, 0.1f, 18, QMath.Identity);
            for (int i = 0; i < 6; i++)
            {
                Quaternion r = QMath.AY(i * 60f + 15f);
                Box(plate, Cool, r * new Vector3(0f, 0.25f, radius * 0.82f), new Vector3(0.14f, 0.4f, 0.14f), r * QMath.AX(18f));
            }
            s.Height = 0.5f;
            s.Radius = radius;
            return s;
        }

        /// <summary>The Choir's small maintenance construct (1 m): a four-legged box with a lamp and a coolant tank.</summary>
        public static MachineSpec EscortConstruct()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, new Vector3(0f, 0.55f, 0f));
            Box(body, Iron, Vector3.zero, new Vector3(0.8f, 0.45f, 1f));
            Box(body, Dark, new Vector3(0f, 0.26f, -0.1f), new Vector3(0.7f, 0.08f, 0.8f));
            Cyl(body, Cool, new Vector3(0f, 0.25f, -0.25f), 0.2f, 0.2f, 0.4f, 10, QMath.AZ(90f));
            Ball(body, Core, new Vector3(0f, 0.2f, 0.52f), new Vector3(0.14f, 0.14f, 0.1f));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.36f : 0.36f;
                float z = i < 2 ? 0.38f : -0.38f;
                Pipe(body, Dark, new Vector3(x, -0.15f, z), new Vector3(x * 1.5f, -0.55f, z * 1.2f), 0.05f, 6);
            }
            HitBox(body, Vector3.zero, new Vector3(0.9f, 0.6f, 1.1f), UBossPart.Body, 0);
            s.Height = 1f;
            s.Radius = 0.6f;
            return s;
        }

        /// <summary>The Choir valve (2.8 m): a base block with pipes and a gauge, and the great wheel (spins as it opens).</summary>
        public static MachineSpec ValveMechanism()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, Vector3.zero);
            Box(body, Iron, new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 2f));
            Box(body, Dark, new Vector3(0f, 1.05f, 0f), new Vector3(1.6f, 0.1f, 1.6f));
            Pipe(body, Iron, new Vector3(-0.7f, 0.6f, -0.5f), new Vector3(-0.7f, 2.2f, -0.5f), 0.18f, 10);
            Pipe(body, Iron, new Vector3(0.7f, 0.6f, -0.5f), new Vector3(0.7f, 2.2f, -0.5f), 0.18f, 10);
            Pipe(body, Iron, new Vector3(-0.7f, 2.2f, -0.5f), new Vector3(0.7f, 2.2f, -0.5f), 0.18f, 10);
            Cyl(body, Brass, new Vector3(0f, 1.6f, 0.15f), 0.3f, 0.3f, 0.5f, 10, QMath.AX(90f));
            HitBox(body, new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 2f), UBossPart.Body, 0);
            MachinePartSpec wheel = Part(s, "Wheel", 0, new Vector3(0f, 1.6f, 0.7f));
            wheel.Glow = true;
            Ring(wheel, Brass, Vector3.zero, 1f, 0.12f, 18, QMath.AX(90f));
            for (int i = 0; i < 8; i++)
            {
                Quaternion r = QMath.AZ(i * 45f);
                Box(wheel, Iron, r * new Vector3(0f, 0.5f, 0f), new Vector3(0.09f, 0.95f, 0.07f), r);
            }
            Ball(wheel, Core, Vector3.zero, new Vector3(0.24f, 0.24f, 0.16f));
            s.Height = 2.8f;
            s.Radius = 1.2f;
            return s;
        }

        /// <summary>A reflector stand: a base and post, and the turning mirror panel (crystal glass framed in brass).</summary>
        public static MachineSpec ReflectorStand()
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, Vector3.zero);
            Cyl(body, Iron, Vector3.zero, 0.6f, 0.5f, 0.4f, 10, QMath.Identity);
            Pipe(body, Dark, new Vector3(0f, 0.4f, 0f), new Vector3(0f, 1.1f, 0f), 0.08f, 8);
            HitBox(body, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 1.2f, 0.9f), UBossPart.Body, 0);
            MachinePartSpec panel = Part(s, "Panel", 0, new Vector3(0f, 1.6f, 0f));
            panel.Glow = true;
            Box(panel, Cool, Vector3.zero, new Vector3(1.7f, 1.1f, 0.08f));
            Box(panel, Brass, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.1f, 0.12f));
            Box(panel, Brass, new Vector3(0f, -0.6f, 0f), new Vector3(1.8f, 0.1f, 0.12f));
            Box(panel, Brass, new Vector3(-0.88f, 0f, 0f), new Vector3(0.1f, 1.3f, 0.12f));
            Box(panel, Brass, new Vector3(0.88f, 0f, 0f), new Vector3(0.1f, 1.3f, 0.12f));
            s.Height = 2.2f;
            s.Radius = 0.9f;
            return s;
        }

        /// <summary>The beam emitter (a pillar with a lens facing +z) or the receptor (a cage round a crystal that lights up).</summary>
        public static MachineSpec BeamPost(bool receptor)
        {
            MachineSpec s = new MachineSpec();
            MachinePartSpec body = Part(s, "Body", -1, Vector3.zero);
            Box(body, Iron, new Vector3(0f, 0.8f, 0f), new Vector3(1.1f, 1.6f, 1.1f));
            Box(body, Brass, new Vector3(0f, 1.62f, 0f), new Vector3(1.2f, 0.08f, 1.2f));
            if (receptor)
            {
                for (int i = 0; i < 6; i++)
                {
                    Quaternion r = QMath.AY(i * 60f);
                    Box(body, Dark, r * new Vector3(0f, 2.2f, 0.45f), new Vector3(0.08f, 1.1f, 0.08f), r);
                }
            }
            else Cyl(body, Dark, new Vector3(0f, 1.6f, 0.2f), 0.32f, 0.32f, 0.5f, 10, QMath.AX(90f));
            HitBox(body, new Vector3(0f, 0.8f, 0f), new Vector3(1.1f, 1.6f, 1.1f), UBossPart.Body, 0);
            MachinePartSpec lens = Part(s, "Lens", 0, receptor ? new Vector3(0f, 2.2f, 0f) : new Vector3(0f, 1.6f, 0.72f));
            lens.Glow = true;
            Ball(lens, Core, Vector3.zero, receptor ? new Vector3(0.35f, 0.45f, 0.35f) : new Vector3(0.26f, 0.26f, 0.12f));
            s.Height = receptor ? 2.8f : 2f;
            s.Radius = 0.7f;
            return s;
        }
    }
}
