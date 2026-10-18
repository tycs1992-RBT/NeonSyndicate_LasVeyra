using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Original 04F modular tailoring and anatomical detail. These are authored silhouette profiles and bevelled
    /// closed plates in the existing skinned mesh, sharing the palette, rig, material and worker-thread build path.
    /// No extra renderers, lights or primitives are spawned. Saved wardrobe choices remain authoritative.
    /// </summary>
    public sealed partial class BodyBuilder
    {
        private void BuildFingers04F(int side, Vector2 uv, float hs)
        {
            for (int finger = 0; finger < RigLayout.FingerCount; finger++)
            {
                int root = RigLayout.Finger(side, finger, 0);
                Vector3 a = _L.World[root];
                Vector3 dir = finger == 4 ? Loft.Nrm(_L.Local[root + 1]) : Vector3.down;
                float length = RigLayout.FingerLength(finger) * hs;
                float radius = (finger == 4 ? 0.0115f : finger == 3 ? 0.0082f : 0.0094f) * hs;
                Vector3 u = finger == 4 ? Loft.Nrm(Vector3.Cross(dir, Vector3.forward)) : Vector3.right;
                Vector3 v = Loft.Nrm(Vector3.Cross(dir, u));
                float[] profile =
                {
                    0f, radius, radius, 0f, 0f, 2f,
                    0.22f, radius * 1.02f, radius * 0.98f, 0f, 0f, 2f,
                    0.44f, radius * 0.94f, radius * 0.9f, 0f, 0f, 2.1f,
                    0.62f, radius * 0.86f, radius * 0.83f, 0f, 0f, 2f,
                    0.76f, radius * 0.84f, radius * 0.78f, 0f, 0f, 2.1f,
                    0.94f, radius * 0.72f, radius * 0.66f, 0f, 0f, 2f,
                    1f, radius * 0.28f, radius * 0.32f, 0f, 0f, 2f
                };
                Loft digit = new Loft(a, a + dir * length, u, v, profile);
                digit.Weight = (t, th) => t < 0.35f ? W1(root) : t < 0.52f ?
                    W2(root, root + 1, SS(0.35f, 0.52f, t)) : t < 0.68f ? W1(root + 1) :
                    W2(root + 1, root + 2, SS(0.68f, 0.84f, t));
                EmitLoft(digit, 0f, 1f, Segs(9, 6, 4), Segs(8, 6, 5), t => uv, false, true);
                // Flat nail or articulated glove tip, deliberately smaller than the distal phalanx.
                if (_detail == 2)
                    EmitPatch(digit, 0.79f, 0.94f, 235f, 305f, 1, 3, 0.0008f * hs,
                        0.0005f * hs, Gloves ? ArmorUV : Pal(0, _look.Skin), uv);
            }
        }

        private void BuildFaceAnatomy04F()
        {
            if (_detail == 0 || _head == null || _look.VisorStyle == 2) return;
            float span = _headY1 - _headY0;
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                // Almond-shaped lower lid and supra-orbital fold. They follow the sculpted head surface.
                int samples = Segs(10, 6, 4);
                for (int i = 0; i < samples; i++)
                {
                    float u0 = (float)i / samples, u1 = (float)(i + 1) / samples;
                    float th0 = 90f - sign * Mathf.Lerp(9f, 30f, u0);
                    float th1 = 90f - sign * Mathf.Lerp(9f, 30f, u1);
                    float y0 = 0.031f - Mathf.Sin(u0 * Mathf.PI) * 0.009f;
                    float y1 = 0.031f - Mathf.Sin(u1 * Mathf.PI) * 0.009f;
                    FaceRibbon04F(th0, HY(y0) * _s, th1, HY(y1) * _s,
                        0.0022f * _s, 0.0025f * _s, SkinUV);
                    if (_detail == 2)
                        FaceRibbon04F(th0, HY(y0 + 0.023f) * _s, th1, HY(y1 + 0.023f) * _s,
                            0.0012f * _s, 0.0018f * _s, BeardUV);
                }
                // Nostril recesses and philtrum ridges add readable features at dialogue distance.
                float th = 90f + sign * 8.5f;
                EmitPatch(_head, (HY(-0.0485f) * _s - _headY0) / span,
                    (HY(-0.0445f) * _s - _headY0) / span, th - 2.2f, th + 2.2f,
                    1, 3, 0.0008f * _s, 0f, BeardUV, BeardUV);
                FaceRibbon04F(90f + sign * 2.3f, HY(-0.052f) * _s,
                    90f + sign * 3.1f, HY(-0.060f) * _s, 0.001f * _s, 0.0011f * _s, SkinUV);
            }
            // A dedicated recessed mouth seam keeps the lips distinct from skin under flat daylight.
            EmitPatch(_head, (HY(-0.070f) * _s - _headY0) / span,
                (HY(-0.0685f) * _s - _headY0) / span, 80.5f, 99.5f, 1,
                Segs(12, 7, 4), 0.001f * _s, 0f, BeardUV, BeardUV);
        }

        private void FaceRibbon04F(float th0, float y0, float th1, float y1, float width, float offset, Vector2 uv)
        {
            float t0 = HeadT(y0), t1 = HeadT(y1);
            Vector3 n0 = _head.Normal(t0, th0), n1 = _head.Normal(t1, th1);
            Vector3 a = _head.Point(t0, th0, offset), b = _head.Point(t1, th1, offset);
            int i0 = AddVertex(a + Vector3.up * width, n0, uv, Vector2.zero, W1(RigLayout.Head));
            int i1 = AddVertex(b + Vector3.up * width, n1, uv, Vector2.right, W1(RigLayout.Head));
            int i2 = AddVertex(b - Vector3.up * width, n1, uv, Vector2.one, W1(RigLayout.Head));
            int i3 = AddVertex(a - Vector3.up * width, n0, uv, Vector2.up, W1(RigLayout.Head));
            Quad(i0, i1, i2, i3);
        }

        private void BuildCrystalTailoring04F()
        {
            if (_detail == 0 || _torso == null) return;
            float s = _s;
            // Layered lapels are cut as six-sided closed forms, with a recessed textile centre and metal edge.
            if (_look.Jacket && _look.SuitStyle != 3)
            {
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    Vector3 c = new Vector3(sign * 0.085f, 1.37f, 0.132f) * s;
                    Plate04F(c, Vector3.forward, Vector3.up, 0.046f * s, 0.145f * s,
                        0.012f * s, JacketUV, TrimUV, W1(RigLayout.Chest), sign * 0.22f);
                    // Three overlapping lower skirt panels hang from the pelvis, clear of thigh articulation.
                    Plate04F(new Vector3(sign * 0.158f, 0.96f, -0.04f) * s,
                        new Vector3(sign * 0.7f, 0f, -0.7f), Vector3.up, 0.055f * s, 0.125f * s,
                        0.01f * s, JacketUV, DarkTechUV, W1(RigLayout.Hips), sign * 0.2f);
                }
                Plate04F(new Vector3(0f, 1.07f, -0.128f) * s, Vector3.back, Vector3.up,
                    0.11f * s, 0.17f * s, 0.008f * s, JacketUV, TrimUV, W1(RigLayout.Spine), 0f);
            }
            if (_look.SuitStyle == 3 || _look.ShellStyle >= 2)
            {
                Vector2 armor = ArmorUV, metal = Pal(5, 2);
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    int upper = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
                    int thigh = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
                    // Swept, overlapping shoulder lames create an enforcement silhouette, with no floating spheres.
                    int lames = _look.ShellStyle >= 3 ? 3 : 2;
                    for (int layer = 0; layer < lames; layer++)
                        Plate04F(_L.World[upper] + new Vector3(sign * (0.014f + layer * 0.022f),
                            0.035f - layer * 0.045f, 0f) * s, new Vector3(sign * 0.6f, 0.8f, 0f),
                            Vector3.forward, (0.075f - layer * 0.006f) * s, 0.091f * s,
                            0.025f * s, armor, metal, W1(upper), -sign * 0.15f);
                    // Articulated hip tassets leave a centre gap for running, stairs and seated riding.
                    Plate04F(_L.World[thigh] + new Vector3(sign * 0.014f, -0.055f, 0.098f) * s,
                        Vector3.forward, Vector3.up, 0.062f * s, 0.12f * s,
                        0.018f * s, armor, metal, W1(thigh), sign * 0.15f);
                    Plate04F(new Vector3(sign * 0.095f, 1.36f, 0.151f) * s,
                        new Vector3(sign * 0.18f, 0f, 1f), Vector3.up, 0.076f * s, 0.125f * s,
                        0.028f * s, armor, metal, W1(RigLayout.Chest), -sign * 0.15f);
                }
                // Solid raised gorget and sternum socket. Only the small inset uses the emissive palette.
                Plate04F(new Vector3(0f, 1.49f, 0.105f) * s, Vector3.forward, Vector3.up,
                    0.088f * s, 0.048f * s, 0.022f * s, armor, metal, W1(RigLayout.Chest), 0f);
                Plate04F(new Vector3(0f, 1.34f, 0.181f) * s, Vector3.forward, Vector3.up,
                    0.025f * s, 0.042f * s, 0.012f * s, HasGlow ? GlowUV : armor,
                    metal, W1(RigLayout.Chest), 0f);
            }
        }

        /// <summary>A six-point forged plate with a bevelled ridge and closed sides, 60 vertices and 24 triangles.</summary>
        private void Plate04F(Vector3 centre, Vector3 normal, Vector3 up, float width, float length,
            float depth, Vector2 face, Vector2 edge, BoneWeight weight, float sweep)
        {
            Vector3 n = Loft.Nrm(normal);
            Vector3 v = Loft.Nrm(up - n * Vector3.Dot(up, n));
            Vector3 u = Loft.Nrm(Vector3.Cross(v, n));
            Vector3[] rim =
            {
                centre + v * length * 1.08f + u * width * sweep,
                centre + v * length * 0.55f + u * width,
                centre - v * length * 0.65f + u * width * 0.8f,
                centre - v * length,
                centre - v * length * 0.65f - u * width * 0.8f,
                centre + v * length * 0.55f - u * width
            };
            Vector3 crown = centre + n * depth;
            for (int i = 0; i < rim.Length; i++)
            {
                Vector3 a = rim[i], b = rim[(i + 1) % rim.Length];
                Vector3 top = Loft.Nrm(Vector3.Cross(b - a, crown - a));
                if (Vector3.Dot(top, n) < 0f) top = -top;
                int t0 = AddVertex(a, top, face, Vector2.zero, weight);
                int t1 = AddVertex(b, top, face, Vector2.right, weight);
                int t2 = AddVertex(crown, top, face, Vector2.one, weight);
                Tri(t0, t1, t2);
                Vector3 outward = Loft.Nrm((a + b) * 0.5f - centre);
                int s0 = AddVertex(a, outward, edge, Vector2.zero, weight);
                int s1 = AddVertex(a - n * depth * 0.45f, outward, edge, Vector2.up, weight);
                int s2 = AddVertex(b - n * depth * 0.45f, outward, edge, Vector2.one, weight);
                int s3 = AddVertex(b, outward, edge, Vector2.right, weight);
                Quad(s0, s1, s2, s3);
                int k0 = AddVertex(a - n * depth * 0.45f, -n, edge, Vector2.zero, weight);
                int k1 = AddVertex(centre - n * depth * 0.45f, -n, edge, Vector2.one, weight);
                int k2 = AddVertex(b - n * depth * 0.45f, -n, edge, Vector2.right, weight);
                Tri(k0, k1, k2);
            }
        }
    }
}
