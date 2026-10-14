using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Original Quartz Drake detail pass. Broad pearl scutes, dark bevels and amethyst accents give the existing
    /// animal a readable hierarchy of large and small forms. Every piece joins its existing single skinned mesh.
    /// No bones, hit volumes, weak points, gameplay dimensions, materials or runtime objects are added.
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        private void QuartzRegalia()
        {
            if (_headTube == null || _neckTube == null || _bodyTube == null) return;
            // Crowd keeps the signature cheek fins. Medium and hero add scales; their source UVs and weights stay
            // on the same palette and bones as the original mesh, so the existing skin renderer and LOD path work.
            QuartzCrown();
            if (_detail == 0) return;
            Vector2 pearl = Pal(_k.Crystal), hide = Pal(_k.Hide), ivory = Pal(_k.Belly);
            int rows = Segs(13, 7, 0);
            float t0 = _bodyT[2], t1 = _bodyT[7];
            // Staggered flank shingles avoid the existing central spines and primary quartz armour.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int lane = 0; lane < Segs(4, 2, 0); lane++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        float u = (row + 0.5f + (lane % 2) * 0.35f) / rows;
                        float t = Mathf.Lerp(t0, t1, Mathf.Min(0.99f, u));
                        float theta = side > 0 ? 14f - lane * 15f : 166f + lane * 15f;
                        float size = _H * (0.12f + 0.025f * Mathf.Sin(u * Mathf.PI));
                        QuartzScaleOnTube(_bodyTube, t, theta, size, size * 0.7f, 0.016f * _H,
                            (row + lane) % 5 == 0 ? pearl : hide, hide);
                    }
                }
            }
            // A series of broad throat plates catches rim light and gives the long neck surface depth.
            int throat = Segs(12, 7, 0);
            for (int row = 0; row < throat; row++)
            {
                float u = (row + 0.5f) / throat;
                float t = Mathf.Lerp(0.13f, 0.85f, u);
                float width = Mathf.Lerp(0.20f, 0.095f, u) * _H;
                QuartzScaleOnTube(_neckTube, t, 270f, 0.085f * _H, width, 0.016f * _H, ivory, hide);
                for (int side = -1; side <= 1; side += 2)
                    QuartzScaleOnTube(_neckTube, t, side > 0 ? 25f : 155f, 0.095f * _H,
                        0.065f * _H, 0.014f * _H, row % 3 == 0 ? pearl : hide, hide);
            }
            // Small forehead shields sit behind the brow gem so its existing critical-hit cue remains exposed.
            for (int side = -1; side <= 1; side += 2)
                for (int row = 0; row < Segs(5, 3, 0); row++)
                    QuartzScaleOnTube(_headTube, 0.38f + row * 0.075f, side > 0 ? 52f : 128f,
                        _headLen * 0.065f, _headLen * 0.048f, _headLen * 0.009f, pearl, hide);
            // Tail scutes shrink with the anatomy instead of creating a second tail silhouette.
            if (_tailJoinT > 0f)
                for (int row = 0; row < Segs(12, 6, 0); row++)
                {
                    float u = (row + 0.5f) / Segs(12, 6, 0);
                    float t = Mathf.Lerp(_tailJoinT * 0.3f, _tailJoinT * 0.9f, u);
                    float size = _H * Mathf.Lerp(0.055f, 0.11f, u);
                    for (int side = -1; side <= 1; side += 2)
                        QuartzScaleOnTube(_bodyTube, t, side > 0 ? 24f : 156f, size, size * 0.65f,
                            0.012f * _H, pearl, hide);
                }
            foreach (WingPlan wing in _wings) QuartzWingArmour(wing);
        }

        /// <summary>New silhouette accents only. The brow gem, jaw silhouette and primary horns remain visible.</summary>
        private void QuartzCrown()
        {
            Vector2 quartz = Pal(_k.Crystal), hide = Pal(_k.Hide);
            int fins = Segs(3, 2, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int f = 0; f < fins; f++)
                {
                    float t = 0.15f + f * 0.085f;
                    float theta = side > 0 ? 8f : 172f;
                    Vector3 p, n;
                    _headTube.Frame(t, theta, out p, out n);
                    float length = _headLen * (0.22f - 0.035f * f);
                    Vector3 dir = Nrm(n * 0.65f - _headFwd * 0.65f + _headUp * 0.24f);
                    EmitCrystal(p - n * (_headLen * 0.008f), dir, _headUp, length, length * 0.2f,
                        5, 0.5f, quartz, W1(_head), 0.03f, 0.52f);
                    if (_detail > 0)
                        QuartzScaleOnTube(_headTube, t, theta, length * 0.28f, length * 0.25f,
                            length * 0.09f, hide, quartz);
                }
            }
        }

        private void QuartzWingArmour(WingPlan w)
        {
            Vector2 quartz = Pal(_k.Crystal), hide = Pal(_k.Hide), glow = Pal(_k.Glow);
            int count = Segs(7, 4, 0);
            for (int i = 0; i < count; i++)
            {
                float u = (i + 0.5f) / count;
                Vector3 along = Nrm(w.Wr - w.E);
                Vector3 p = Vector3.Lerp(w.E, w.Wr, u) + Vector3.up * (w.Len * Mathf.Lerp(0.027f, 0.016f, u));
                QuartzScale(p, Vector3.up, along, w.Len * 0.043f, w.Len * Mathf.Lerp(0.033f, 0.02f, u),
                    w.Len * 0.009f, quartz, hide, W1(w.Fore));
            }
            // Wrist caps and paired finger-root crystals remain compact during the original folding motion.
            for (int f = 0; f < w.Fingers.Length; f++)
            {
                Vector3 dir = Nrm(w.Tips[f] - w.Hd);
                Vector3 p = w.Hd + dir * (0.10f * w.Len);
                EmitCrystal(p, Nrm(Vector3.up * 0.85f + dir * 0.4f), dir, w.Len * 0.055f,
                    w.Len * 0.018f, 5, 0.4f, quartz, W1(w.Fingers[f]), 0.03f, 0.8f);
                if (_detail == 2)
                    QuartzScale(p + Vector3.up * (0.006f * w.Len), Vector3.up, dir,
                        w.Len * 0.024f, w.Len * 0.009f, w.Len * 0.003f, glow, quartz, W1(w.Fingers[f]));
            }
        }

        /// <summary>A bevelled, six-sided scute following the existing surface's normal and deformation weights.</summary>
        private void QuartzScaleOnTube(CreatureTube tube, float t, float theta, float length, float width,
            float raised, Vector2 face, Vector2 edge)
        {
            Vector3 p, n;
            tube.Frame(t, Wrap360(theta), out p, out n);
            Vector3 along = Nrm(tube.Center(Mathf.Min(1f, t + 0.015f)) - tube.Center(Mathf.Max(0f, t - 0.015f)));
            QuartzScale(p + n * (raised * 0.1f), n, along, length, width, raised,
                face, edge, tube.WeightAt(t, Wrap360(theta)));
        }

        /// <summary>42 vertices, 18 triangles. The buried rim and raised centre produce a clean readable bevel.</summary>
        private void QuartzScale(Vector3 centre, Vector3 normal, Vector3 along, float length, float width,
            float raised, Vector2 face, Vector2 edge, BoneWeight weight)
        {
            Vector3 n = Nrm(normal);
            Vector3 tangent = Nrm(along - n * Vector3.Dot(along, n));
            if (tangent.sqrMagnitude < 0.5f) tangent = Nrm(Vector3.Cross(n, Vector3.right));
            Vector3 across = Nrm(Vector3.Cross(n, tangent));
            Vector3 crown = centre + n * raised - tangent * (length * 0.12f);
            for (int corner = 0; corner < 6; corner++)
            {
                float a = corner * Mathf.PI / 3f, b = (corner + 1) * Mathf.PI / 3f;
                Vector3 ra = tangent * (Mathf.Cos(a) * length) + across * (Mathf.Sin(a) * width);
                Vector3 rb = tangent * (Mathf.Cos(b) * length) + across * (Mathf.Sin(b) * width);
                Vector3 va = centre + ra, vb = centre + rb;
                Vector3 topN = Nrm(Vector3.Cross(vb - va, crown - va));
                if (Vector3.Dot(topN, n) < 0f) topN = -topN;
                int ca = AddVertex(va, topN, face, new Vector2(0f, 0f), weight);
                int cb = AddVertex(vb, topN, face, new Vector2(1f, 0f), weight);
                int cc = AddVertex(crown, topN, face, new Vector2(0.5f, 1f), weight);
                Tri(ca, cb, cc);
                Vector3 skirtN = Nrm(ra + rb + n * (raised * 0.3f));
                Vector3 buriedA = centre + ra * 1.06f - n * (raised * 0.45f);
                Vector3 buriedB = centre + rb * 1.06f - n * (raised * 0.45f);
                int sa = AddVertex(va, skirtN, edge, Vector2.zero, weight);
                int sb = AddVertex(vb, skirtN, edge, Vector2.right, weight);
                int sc = AddVertex(buriedB, skirtN, edge, Vector2.one, weight);
                int sd = AddVertex(buriedA, skirtN, edge, Vector2.up, weight);
                Quad(sa, sb, sc, sd);
            }
        }
    }
}
