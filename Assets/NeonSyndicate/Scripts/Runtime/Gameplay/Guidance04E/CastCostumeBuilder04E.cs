using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds a cast member's costume layers (CastCostume04E) as small procedural meshes parented to the rig's bones:
    /// headgear on the head, a torso layer on the chest, a skirt, bag or belt on the hips and the held tool in the left
    /// hand (the right hand stays free for the greeting wave and pointing). Sizes follow the body builder's head and
    /// torso proportions times the rig scale, so pieces sit on the body rather than floating or cutting through the face.
    /// Each piece owns its mesh (destroyed with the person); materials come from the shared cache. Runtime only.
    /// </summary>
    public static class CastCostumeBuilder04E
    {
        private const int Main = 0, Trim = 1, Glow = 2;

        /// <summary>Adds every costume layer to the rig. A layer that fails is skipped; the person stays.</summary>
        public static void Dress(HumanoidRig rig, CastCostume04E c)
        {
            if (rig == null || rig.Head == null) return;
            float s = rig.Scale;
            Material[] mats =
            {
                Mats.Solid(c.Main, 0.3f),
                Mats.Solid(c.Accent, 0.55f),
                Mats.Emissive(c.Accent, 1.5f)
            };
            MeshBuilder b = new MeshBuilder(3);
            BuildHead(b, c.Head, s);
            Attach(rig, rig.Head, "Costume Head 04E", b, mats, c.Head == CostumeHead.Hood || c.Head == CostumeHead.BrimHat);
            b = new MeshBuilder(3);
            BuildTorso(b, c.Torso, s);
            Attach(rig, rig.Chest, "Costume Torso 04E", b, mats, c.Torso == CostumeTorso.Cloak || c.Torso == CostumeTorso.Mantle);
            b = new MeshBuilder(3);
            BuildHips(b, c.Torso, c.Waist, s);
            Attach(rig, rig.Hips, "Costume Waist 04E", b, mats, c.Torso == CostumeTorso.Apron);
            b = new MeshBuilder(3);
            BuildTool(b, c.Tool, s);
            Attach(rig, rig.HandL, "Costume Tool 04E", b, mats, c.Tool == HeldTool.Staff);
        }

        private static void Attach(HumanoidRig rig, Transform bone, string name, MeshBuilder b, Material[] mats, bool shadows)
        {
            if (bone == null || b.VertexCount == 0) return;
            GameObject go = new GameObject(name);
            go.layer = rig.gameObject.layer;
            go.transform.SetParent(bone, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = OwnedMeshes.Track(go, b.ToMesh(name));
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        // ================================================================== head (head bone space)

        private static void BuildHead(MeshBuilder b, CostumeHead head, float s)
        {
            Quaternion id = Quaternion.identity;
            switch (head)
            {
                case CostumeHead.Hood:
                    // A deep hood: an ellipsoid shell over the crown and back of the head, open at the face.
                    Shell(b, Main, new Vector3(0f, 0.085f, -0.006f) * s, new Vector3(0.12f, 0.162f, 0.134f) * s, 118f);
                    break;
                case CostumeHead.BrimHat:
                    b.Cylinder(Main, new Vector3(0f, 0.158f, 0.012f) * s, 0.235f * s, 0.23f * s, 0.012f * s, 18, id, true);
                    b.Cylinder(Main, new Vector3(0f, 0.165f, 0.012f) * s, 0.099f * s, 0.088f * s, 0.118f * s, 14, id, true);
                    b.Cylinder(Trim, new Vector3(0f, 0.168f, 0.012f) * s, 0.101f * s, 0.099f * s, 0.024f * s, 14, id, false);
                    break;
                case CostumeHead.Headset:
                    for (int i = 0; i < 6; i++)
                    {
                        float a0 = Mathf.PI * i / 6f, a1 = Mathf.PI * (i + 1) / 6f;
                        Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.105f, 0.08f + Mathf.Sin(a0) * 0.155f, -0.005f) * s;
                        Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.105f, 0.08f + Mathf.Sin(a1) * 0.155f, -0.005f) * s;
                        Bar(b, Trim, p0, p1, 0.018f * s, 0.022f * s);
                    }
                    for (int side = -1; side <= 1; side += 2)
                        b.Cylinder(Main, new Vector3(side * 0.088f, 0.072f, 0.004f) * s, 0.036f * s, 0.034f * s, 0.032f * s, 10, QMath.AZ(-side * 90f), true);
                    Bar(b, Main, new Vector3(-0.11f, 0.06f, 0.02f) * s, new Vector3(-0.045f, 0.012f, 0.112f) * s, 0.01f * s, 0.01f * s);
                    b.Box(Glow, new Vector3(-0.04f, 0.01f, 0.116f) * s, new Vector3(0.022f, 0.016f, 0.016f) * s, UVMode.Unit);
                    break;
                case CostumeHead.Goggles:
                    Band(b, Trim, new Vector3(0f, 0.14f, 0.012f) * s, 0.099f * s, 0.113f * s, 0.03f * s, 16);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        b.Cylinder(Main, new Vector3(side * 0.037f, 0.157f, 0.098f) * s, 0.031f * s, 0.029f * s, 0.026f * s, 10, QMath.AX(90f), false);
                        b.Cylinder(Glow, new Vector3(side * 0.037f, 0.157f, 0.122f) * s, 0.026f * s, 0.026f * s, 0.004f * s, 10, QMath.AX(90f), true);
                    }
                    break;
                case CostumeHead.Circlet:
                    Band(b, Trim, new Vector3(0f, 0.145f, 0.012f) * s, 0.093f * s, 0.107f * s, 0.014f * s, 16);
                    b.Sphere(Glow, new Vector3(0f, 0.152f, 0.12f) * s, new Vector3(0.016f, 0.02f, 0.012f) * s, 4, 8, Quaternion.identity);
                    break;
            }
        }

        // ================================================================== torso (chest bone space)

        private static void BuildTorso(MeshBuilder b, CostumeTorso torso, float s)
        {
            Quaternion id = Quaternion.identity;
            switch (torso)
            {
                case CostumeTorso.Scarf:
                    b.Cylinder(Main, new Vector3(0f, 0.19f, 0.006f) * s, 0.108f * s, 0.092f * s, 0.075f * s, 12, id, true);
                    b.Box(Main, new Vector3(0.07f, 0.075f, 0.128f) * s, new Vector3(0.07f, 0.24f, 0.024f) * s, QMath.AZ(-8f), UVMode.Unit);
                    b.Box(Trim, new Vector3(0.078f, -0.04f, 0.13f) * s, new Vector3(0.072f, 0.02f, 0.026f) * s, QMath.AZ(-8f), UVMode.Unit);
                    break;
                case CostumeTorso.Mantle:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        b.Box(Main, new Vector3(side * 0.15f, 0.215f, 0f) * s, new Vector3(0.17f, 0.045f, 0.23f) * s, QMath.AZ(-side * 22f), UVMode.Unit);
                        b.Box(Trim, new Vector3(side * 0.225f, 0.183f, 0f) * s, new Vector3(0.02f, 0.05f, 0.235f) * s, QMath.AZ(-side * 22f), UVMode.Unit);
                    }
                    b.Cylinder(Main, new Vector3(0f, 0.18f, 0f) * s, 0.125f * s, 0.11f * s, 0.06f * s, 12, id, true);
                    break;
                case CostumeTorso.Cloak:
                    b.Box(Main, new Vector3(0f, -0.14f, -0.155f) * s, new Vector3(0.44f, 0.76f, 0.03f) * s, QMath.AX(7f), UVMode.Unit);
                    for (int side = -1; side <= 1; side += 2)
                        b.Box(Main, new Vector3(side * 0.13f, 0.215f, -0.03f) * s, new Vector3(0.16f, 0.04f, 0.2f) * s, QMath.AZ(-side * 18f), UVMode.Unit);
                    b.Box(Trim, new Vector3(0f, 0.23f, -0.12f) * s, new Vector3(0.3f, 0.03f, 0.04f) * s, UVMode.Unit);
                    break;
                case CostumeTorso.Satchel:
                    Bar(b, Trim, new Vector3(0.14f, 0.2f, 0.07f) * s, new Vector3(-0.15f, -0.28f, 0.124f) * s, 0.04f * s, 0.012f * s);
                    Bar(b, Trim, new Vector3(0.14f, 0.2f, -0.07f) * s, new Vector3(-0.15f, -0.28f, -0.11f) * s, 0.04f * s, 0.012f * s);
                    break;
                case CostumeTorso.Sash:
                    Bar(b, Trim, new Vector3(-0.14f, 0.2f, 0.075f) * s, new Vector3(0.15f, -0.27f, 0.125f) * s, 0.075f * s, 0.012f * s);
                    Bar(b, Trim, new Vector3(-0.14f, 0.2f, -0.07f) * s, new Vector3(0.15f, -0.27f, -0.11f) * s, 0.075f * s, 0.012f * s);
                    b.Sphere(Glow, new Vector3(-0.01f, -0.04f, 0.128f) * s, new Vector3(0.02f, 0.02f, 0.01f) * s, 4, 8, Quaternion.identity);
                    break;
                case CostumeTorso.Apron:
                    b.Box(Main, new Vector3(0f, 0.08f, 0.124f) * s, new Vector3(0.25f, 0.22f, 0.012f) * s, UVMode.Unit);
                    for (int side = -1; side <= 1; side += 2)
                        Bar(b, Main, new Vector3(side * 0.1f, 0.19f, 0.12f) * s, new Vector3(side * 0.09f, 0.24f, -0.04f) * s, 0.02f * s, 0.01f * s);
                    break;
            }
        }

        // ================================================================== hips (hips bone space)

        private static void BuildHips(MeshBuilder b, CostumeTorso torso, CostumeWaist waist, float s)
        {
            if (torso == CostumeTorso.Apron)
                b.Box(Main, new Vector3(0f, -0.19f, 0.135f) * s, new Vector3(0.35f, 0.44f, 0.014f) * s, QMath.AX(-6f), UVMode.Unit);
            if (torso == CostumeTorso.Satchel)
            {
                b.Box(Main, new Vector3(-0.2f, -0.05f, 0.03f) * s, new Vector3(0.075f, 0.17f, 0.25f) * s, UVMode.Unit);
                b.Box(Trim, new Vector3(-0.24f, -0.02f, 0.03f) * s, new Vector3(0.012f, 0.06f, 0.08f) * s, UVMode.Unit);
            }
            switch (waist)
            {
                case CostumeWaist.ToolBelt:
                    Band(b, Trim, new Vector3(0f, 0.035f, 0.005f) * s, 0.168f * s, 0.122f * s, 0.05f * s, 14);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        b.Box(Main, new Vector3(side * 0.165f, -0.015f, 0.05f) * s, new Vector3(0.06f, 0.1f, 0.09f) * s, UVMode.Unit);
                        b.Box(Main, new Vector3(side * 0.12f, -0.02f, -0.11f) * s, new Vector3(0.07f, 0.09f, 0.05f) * s, UVMode.Unit);
                    }
                    break;
                case CostumeWaist.Pouches:
                    Band(b, Trim, new Vector3(0f, 0.03f, 0.005f) * s, 0.164f * s, 0.12f * s, 0.035f * s, 14);
                    for (int side = -1; side <= 1; side += 2)
                        b.Box(Main, new Vector3(side * 0.09f, -0.012f, 0.122f) * s, new Vector3(0.07f, 0.075f, 0.04f) * s, UVMode.Unit);
                    break;
            }
        }

        // ================================================================== held tool (left hand bone space, hanging)

        private static void BuildTool(MeshBuilder b, HeldTool tool, float s)
        {
            Quaternion id = Quaternion.identity;
            switch (tool)
            {
                case HeldTool.Clipboard:
                    b.Box(Main, new Vector3(0.012f, -0.2f, 0.05f) * s, new Vector3(0.012f, 0.31f, 0.23f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0.012f, -0.06f, 0.05f) * s, new Vector3(0.022f, 0.03f, 0.08f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0.004f, -0.22f, 0.05f) * s, new Vector3(0.004f, 0.22f, 0.18f) * s, UVMode.Unit);
                    break;
                case HeldTool.Tablet:
                    b.Box(Main, new Vector3(0.006f, -0.16f, 0.045f) * s, new Vector3(0.012f, 0.25f, 0.17f) * s, UVMode.Unit);
                    b.Box(Glow, new Vector3(-0.002f, -0.16f, 0.045f) * s, new Vector3(0.004f, 0.22f, 0.145f) * s, UVMode.Unit);
                    break;
                case HeldTool.Wrench:
                    b.Box(Main, new Vector3(0f, -0.15f, 0.02f) * s, new Vector3(0.022f, 0.27f, 0.032f) * s, UVMode.Unit);
                    b.Box(Main, new Vector3(0f, -0.3f, 0.035f) * s, new Vector3(0.026f, 0.055f, 0.1f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0f, -0.06f, 0.02f) * s, new Vector3(0.026f, 0.07f, 0.036f) * s, UVMode.Unit);
                    break;
                case HeldTool.Lantern:
                    b.Box(Trim, new Vector3(0f, -0.11f, 0f) * s, new Vector3(0.012f, 0.07f, 0.08f) * s, UVMode.Unit);
                    b.Cylinder(Main, new Vector3(0f, -0.33f, 0f) * s, 0.058f * s, 0.05f * s, 0.03f * s, 10, id, true);
                    b.Cylinder(Glow, new Vector3(0f, -0.3f, 0f) * s, 0.046f * s, 0.046f * s, 0.12f * s, 10, id, false);
                    b.Cylinder(Main, new Vector3(0f, -0.18f, 0f) * s, 0.054f * s, 0.03f * s, 0.035f * s, 10, id, true);
                    break;
                case HeldTool.Ledger:
                    b.Box(Main, new Vector3(0.012f, -0.17f, 0.05f) * s, new Vector3(0.05f, 0.27f, 0.2f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0.012f, -0.17f, -0.052f) * s, new Vector3(0.054f, 0.27f, 0.012f) * s, UVMode.Unit);
                    break;
                case HeldTool.Staff:
                    b.Cylinder(Main, new Vector3(0f, -0.82f, 0.03f) * s, 0.018f * s, 0.016f * s, 1.52f * s, 8, id, true);
                    b.Sphere(Glow, new Vector3(0f, 0.74f, 0.03f) * s, new Vector3(0.04f, 0.075f, 0.04f) * s, 4, 6, Quaternion.identity);
                    b.Cylinder(Trim, new Vector3(0f, 0.66f, 0.03f) * s, 0.026f * s, 0.022f * s, 0.03f * s, 8, id, true);
                    break;
                case HeldTool.Medkit:
                    b.Box(Main, new Vector3(0f, -0.21f, 0.02f) * s, new Vector3(0.08f, 0.16f, 0.25f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0f, -0.11f, 0.02f) * s, new Vector3(0.02f, 0.035f, 0.1f) * s, UVMode.Unit);
                    b.Box(Glow, new Vector3(-0.042f, -0.21f, 0.02f) * s, new Vector3(0.006f, 0.08f, 0.026f) * s, UVMode.Unit);
                    b.Box(Glow, new Vector3(-0.042f, -0.21f, 0.02f) * s, new Vector3(0.006f, 0.026f, 0.08f) * s, UVMode.Unit);
                    break;
                case HeldTool.Hammer:
                    b.Box(Main, new Vector3(0f, -0.17f, 0.02f) * s, new Vector3(0.026f, 0.36f, 0.026f) * s, UVMode.Unit);
                    b.Box(Trim, new Vector3(0f, -0.35f, 0.035f) * s, new Vector3(0.05f, 0.066f, 0.16f) * s, UVMode.Unit);
                    break;
                case HeldTool.DataSlate:
                    b.Box(Main, new Vector3(0.004f, -0.13f, 0.05f) * s, new Vector3(0.01f, 0.19f, 0.13f) * s, UVMode.Unit);
                    b.Box(Glow, new Vector3(-0.003f, -0.13f, 0.05f) * s, new Vector3(0.004f, 0.16f, 0.105f) * s, UVMode.Unit);
                    break;
            }
        }

        // ================================================================== helpers

        /// <summary>A rectangular bar from one point to another (width across, depth thickness).</summary>
        private static void Bar(MeshBuilder b, int sub, Vector3 from, Vector3 to, float width, float depth)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 1e-5f) return;
            Quaternion rot = QMath.FromTo(Vector3.up, d / len);
            b.Box(sub, (from + to) * 0.5f, new Vector3(width, len, depth), rot, UVMode.Unit);
        }

        /// <summary>An elliptical band (both sides) around the vertical axis, like a belt or a headband.</summary>
        private static void Band(MeshBuilder b, int sub, Vector3 center, float rx, float rz, float height, int segments)
        {
            Vector2 uv = Vector2.zero;
            Vector3 up = new Vector3(0f, height * 0.5f, 0f);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0) * rx, 0f, Mathf.Sin(a0) * rz);
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * rx, 0f, Mathf.Sin(a1) * rz);
                Vector3 n = new Vector3(Mathf.Cos((a0 + a1) * 0.5f), 0f, Mathf.Sin((a0 + a1) * 0.5f));
                // Outer face, then the inner face with the opposite winding.
                b.Quad(sub, p0 - up, p0 + up, p1 + up, p1 - up, n, uv, uv, uv, uv);
                b.Quad(sub, p1 - up, p1 + up, p0 + up, p0 - up, -n, uv, uv, uv, uv);
            }
        }

        /// <summary>
        /// An ellipsoid shell from the crown down to polarMax degrees, leaving the face open (the front 60 degrees below
        /// the brow), with both sides drawn so the hood's lining shows around the face.
        /// </summary>
        private static void Shell(MeshBuilder b, int sub, Vector3 center, Vector3 radii, float polarMax)
        {
            const int rings = 9, segs = 16;
            Vector2 uv = Vector2.zero;
            for (int r = 0; r < rings; r++)
            {
                float t0 = polarMax * Mathf.Deg2Rad * r / rings, t1 = polarMax * Mathf.Deg2Rad * (r + 1) / rings;
                for (int k = 0; k < segs; k++)
                {
                    float p0 = Mathf.PI * 2f * k / segs, p1 = Mathf.PI * 2f * (k + 1) / segs;
                    float pm = (p0 + p1) * 0.5f, tm = (t0 + t1) * 0.5f;
                    // Azimuth measured from the face (+z): skip the face opening.
                    float fromFront = Mathf.Abs(Mathf.DeltaAngle(0f, 90f - pm * Mathf.Rad2Deg));
                    if (fromFront < 60f && tm * Mathf.Rad2Deg > 40f) continue;
                    Vector3 a = center + Vector3.Scale(Dir(t0, p0), radii);
                    Vector3 bb = center + Vector3.Scale(Dir(t0, p1), radii);
                    Vector3 c = center + Vector3.Scale(Dir(t1, p1), radii);
                    Vector3 d = center + Vector3.Scale(Dir(t1, p0), radii);
                    Vector3 n = Dir(tm, pm);
                    b.Quad(sub, a, bb, c, d, n, uv, uv, uv, uv);
                    b.Quad(sub, a, d, c, bb, -n, uv, uv, uv, uv);
                }
            }
        }

        private static Vector3 Dir(float polar, float azimuth)
        {
            return new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
        }
    }
}
