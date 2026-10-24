using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The H8S creature look on the existing body plans (CreatureLook.ObsidianH8S): crystal shard clusters in the
    /// region's family breaking out of the spine, shoulders, crown, forearms, haunches and tail (varied sizes and angles,
    /// each grown from a low basalt socket, in place of the old uniform combs and slabs), and anatomy that reads under
    /// the realism grade: scapular and haunch plates, nape and jaw plates, rib ridges and joint caps in dark chitin and
    /// basalt, with carved ward lines on the guardians. Everything is appended after the plan has built its body, so the
    /// skeleton, hit volumes, radius, height and the aim, head and mouth points are exactly those of the plain body;
    /// every new vertex rides the skin weight of the surface it grows from. CrystalClusterH8S plans the shards.
    /// Budget (added vertices, about): crowd 800, standard 2600, hero 4500; the wyrm and the drake a little more.
    /// </summary>
    internal sealed partial class CreatureSculptor
    {
        private readonly CrystalPrismH8S[] _prisms = new CrystalPrismH8S[12];
        private readonly List<Vector3> _cv = new List<Vector3>(512), _cn = new List<Vector3>(512);
        private readonly List<int> _ck = new List<int>(512), _ct = new List<int>(1024);
        /// <summary>The wyrm's body tube (BuildSerpent keeps it for the cluster pass).</summary>
        private CreatureTube _wyrmTube;

        /// <summary>Dim seam glow for veins, rims and tendril seams: an inner light in the H8S look, the species glow before it.</summary>
        private Vector2 SeamUV { get { return _k.ObsidianH8S ? Pal(ObsidianPaletteH8S.DimCell(_k.FamilyH8S)) : Pal(_k.Glow); } }

        private Vector2 BodyCell(int column) { return Pal(ObsidianPaletteH8S.BodyCell(column)); }

        private Vector2 CrystalShade(int shade) { return Pal(ObsidianPaletteH8S.CrystalCell(_k.FamilyH8S, shade)); }

        /// <summary>Shards in a full cluster for the detail level (hero clusters are the fullest).</summary>
        private int ShardsPerCluster { get { return _detail == 2 ? 5 : (_detail == 1 ? 3 : 2); } }

        /// <summary>Runs the pass for the plan (called once per build, after the plan's own geometry).</summary>
        private void ObsidianH8S()
        {
            if (!_k.ObsidianH8S) return;
            switch (_k.Plan)
            {
                case CreaturePlan.Serpent: ObsidianSerpentH8S(); break;
                case CreaturePlan.Wisp: ObsidianWispH8S(); break;
                case CreaturePlan.Mite: ObsidianMiteH8S(); break;
                default: ObsidianBeastH8S(); break;
            }
        }

        // ================================================================== clusters

        /// <summary>
        /// A shard cluster on a tube surface at (t, theta): the leader shard about size long, the rest fanning out over a
        /// disc of spread, all leaning by lean radians toward lower t (back along the body) or, with lean negative, toward
        /// higher t. Rides the tube's skin weight at that point.
        /// </summary>
        private void ClusterOnTube(CreatureTube tb, float t, float theta, float size, float spread, int shards, float lean, bool socket)
        {
            Vector3 p, n;
            float th = Wrap360(theta);
            tb.Frame(t, th, out p, out n);
            Vector3 along = Nrm(tb.Center(t + 0.01f) - tb.Center(t - 0.01f));
            Cluster(p, n, -along, size, spread, shards, lean, socket, tb.WeightAt(t, th));
        }

        /// <summary>A shard cluster at a surface point with normal n; rake is the surface direction the shards lean toward.</summary>
        private void Cluster(Vector3 p, Vector3 n, Vector3 rake, float size, float spread, int shards, float lean, bool socket, BoneWeight w)
        {
            Vector3 z = Nrm(n);
            if (z.sqrMagnitude < 0.5f) z = Vector3.up;
            Vector3 x = rake - z * Vector3.Dot(rake, z);
            if (x.sqrMagnitude < 1e-8f) x = Vector3.Cross(z, Mathf.Abs(z.y) < 0.9f ? Vector3.up : Vector3.right);
            x = Nrm(x);
            Vector3 y = Vector3.Cross(z, x);
            uint seed = (uint)(Rand() * 4294967000f) | 1u;
            int count = CrystalClusterH8S.Plan(seed, shards, size, spread, lean, _prisms);
            // The socket: a low mound of basalt the shards break out of, sunk a little into the hide.
            Vector3 origin = p - z * (size * 0.04f);
            if (socket)
            {
                _cv.Clear(); _cn.Clear(); _ck.Clear(); _ct.Clear();
                CrystalClusterH8S.Socket(seed ^ 0x5bd1e995u, 7, spread * 1.25f + size * 0.12f, size * 0.1f, _cv, _cn, _ck, _ct);
                Vector2 basalt = BodyCell(ObsidianPaletteH8S.Basalt);
                EmitClusterFaces(origin, x, y, z, basalt, basalt, basalt, w);
            }
            for (int i = 0; i < count; i++)
            {
                CrystalPrismH8S prism = CrystalClusterH8S.Place(_prisms[i], origin, x, y, z);
                _cv.Clear(); _cn.Clear(); _ck.Clear(); _ct.Clear();
                CrystalClusterH8S.Faces(prism, seed + (uint)(i * 7919), 0.14f, _cv, _cn, _ck, _ct);
                int shade = prism.Shade;
                Vector2 body = CrystalShade(shade);
                EmitClusterFaces(Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, body, CrystalShade(Mathf.Min(2, shade + 1)), body, w);
            }
        }

        /// <summary>Appends the scratch faces (kind 0 body, 1 tip, 2 socket) in the given frame with one palette cell per kind.</summary>
        private void EmitClusterFaces(Vector3 origin, Vector3 x, Vector3 y, Vector3 z, Vector2 bodyUV, Vector2 tipUV, Vector2 socketUV, BoneWeight w)
        {
            int first = _d.Vertices.Count;
            for (int i = 0; i < _cv.Count; i++)
            {
                Vector3 lp = _cv[i], ln = _cn[i];
                Vector3 mp = origin + x * lp.x + y * lp.y + z * lp.z;
                Vector3 mn = Nrm(x * ln.x + y * ln.y + z * ln.z);
                int kind = _ck[i];
                Vector2 uv = kind == 1 ? tipUV : (kind == 2 ? socketUV : bodyUV);
                AddVertex(mp, mn, uv, new Vector2(mp.x / _tile, mp.z / _tile), w);
            }
            for (int i = 0; i + 2 < _ct.Count; i += 3) Tri(first + _ct[i], first + _ct[i + 1], first + _ct[i + 2]);
        }

        // ================================================================== the beasts (quadruped and winged plans)

        private void ObsidianBeastH8S()
        {
            if (_bodyTube == null || _headTube == null) return;
            float rich = Mathf.Clamp01(_k.ClusterH8S);
            int shards = ShardsPerCluster;
            int small = Mathf.Max(2, shards - 1), least = Mathf.Max(2, shards - 2);
            bool socket = _detail > 0;
            bool crowd = _detail == 0;
            float H = _H, hl = _headLen;
            int hs = _k.HeadStyle;
            CreatureTube tb = _bodyTube;
            float tHip = _bodyT[2], tWither = _bodyT[7];
            // Spine: a few clusters along the back, biggest over the loin, never a comb.
            if (!_k.BackFreeH8S)
            {
                int spineN = Mathf.Clamp(Mathf.RoundToInt(1.5f + rich * 3.5f), 2, 5);
                if (crowd) spineN = Mathf.Min(spineN, 3);
                for (int i = 0; i < spineN; i++)
                {
                    float u = (i + 0.5f) / spineN;
                    float t = Mathf.Lerp(tWither - 0.02f, tHip + 0.02f, u);
                    float size = H * (0.16f + 0.1f * Mathf.Sin(u * Mathf.PI) + 0.06f * rich) * Rand(0.85f, 1.15f);
                    if (hs == 2) size *= 1.3f;
                    ClusterOnTube(tb, t, 90f + Rand(-10f, 10f), size, size * 0.5f, shards, 0.55f, socket);
                }
            }
            // Shoulders: over each shoulder blade, raked back and outward like folded wings of stone.
            for (int side = -1; side <= 1; side += 2)
            {
                float size = H * (hs == 1 ? 0.26f : 0.21f) * (0.9f + 0.3f * rich) * Rand(0.9f, 1.1f);
                ClusterOnTube(tb, tWither - 0.005f, side > 0 ? 46f : 134f, size, size * 0.5f, shards, 0.6f, socket);
            }
            // Haunches.
            if (!crowd)
                for (int side = -1; side <= 1; side += 2)
                {
                    float size = H * 0.14f * (0.9f + 0.3f * rich) * Rand(0.9f, 1.1f);
                    ClusterOnTube(tb, tHip + 0.012f, side > 0 ? 30f : 150f, size, size * 0.45f, small, 0.5f, socket);
                }
            // Crown: behind the brow. Lizards keep their crown horns and dragons their horns, so they get a small brow pair.
            if (hs == 2 || hs == 3)
            {
                for (int side = -1; side <= 1; side += 2)
                    ClusterOnTube(_headTube, 0.3f, side > 0 ? 58f : 122f, hl * 0.11f, hl * 0.05f, 2, 0.4f, false);
            }
            else if (hs == 0)
            {
                // Canids: a crystal mane down the nape instead of a crown.
                int mane = crowd ? 1 : 3;
                for (int i = 0; i < mane; i++)
                {
                    float t = Mathf.Lerp(0.42f, 0.8f, (i + 0.5f) / mane);
                    ClusterOnTube(_neckTube, t, 90f + Rand(-8f, 8f), H * 0.1f * Rand(0.85f, 1.2f), H * 0.045f, small, 0.5f, socket);
                }
                ClusterOnTube(_headTube, 0.16f, 90f, hl * 0.16f, hl * 0.07f, small, 0.6f, false);
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                    ClusterOnTube(_headTube, 0.14f, side > 0 ? 66f : 114f, hl * (hs == 4 ? 0.15f : 0.18f), hl * 0.07f, small, 0.6f, false);
            }
            // Forearms and thighs: a cluster on the outside of each upper limb (the shards ride the limb bones through the
            // leg tube's weights).
            for (int i = 0; i < _legTubes.Count && i < _legs.Count; i++)
            {
                LegPlan lp = _legs[i];
                CreatureTube lt = _legTubes[i];
                if (crowd && !lp.Front) continue;
                float tH = lt.CtrlT(2), tK = lt.CtrlT(3);
                float t = Mathf.Lerp(tH, tK, lp.Front ? 0.38f : 0.3f);
                float theta = lp.Side > 0 ? 345f : 195f;
                float size = H * (lp.Front ? 0.09f : 0.085f) * (0.9f + 0.3f * rich);
                Vector3 p, n;
                lt.Frame(t, theta, out p, out n);
                Vector3 up = Nrm(lt.Center(t - 0.02f) - lt.Center(t + 0.02f));
                Cluster(p, n, up, size, size * 0.4f, least, 0.45f, _detail == 2, lt.WeightAt(t, theta));
            }
            // Tail: shards along the tail root.
            if (!crowd)
            {
                if (_tailTube != null)
                {
                    int tailN = _k.TailStyle == 1 ? 1 : 2;
                    for (int i = 0; i < tailN; i++)
                    {
                        float t = Mathf.Lerp(0.2f, 0.55f, tailN == 1 ? 0f : (float)i / (tailN - 1));
                        ClusterOnTube(_tailTube, t, 90f + Rand(-12f, 12f), H * 0.075f * Rand(0.85f, 1.15f), H * 0.03f, least, -0.4f, false);
                    }
                }
                else if (_tailJoinT > 0f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float t = _tailJoinT * Mathf.Lerp(0.72f, 0.42f, i);
                        ClusterOnTube(tb, t, 90f + Rand(-10f, 10f), H * 0.1f * Rand(0.85f, 1.15f), H * 0.04f, small, -0.45f, socket);
                    }
                }
            }
            // Wings: shards at the elbows (and the wrists at hero), riding the wing bones. Wings made of glass stay clear.
            bool glassWings = _k.Membrane.y == ObsidianPaletteH8S.CrystalRow;
            if (!crowd && !glassWings)
                foreach (WingPlan w in _wings)
                {
                    Vector3 along = Nrm(w.E - w.S);
                    Cluster(w.E + Vector3.up * (w.Len * 0.02f), Vector3.up, -along, w.Len * 0.075f, w.Len * 0.03f, least, 0.5f, false, W2(w.Arm, w.Fore, 0.5f));
                    if (_detail == 2) Cluster(w.Wr + Vector3.up * (w.Len * 0.015f), Vector3.up, -along, w.Len * 0.05f, w.Len * 0.02f, 2, 0.5f, false, W1(w.Hand));
                }
            ObsidianAnatomyH8S();
        }

        /// <summary>Plates, ridges and joint caps in dark chitin and basalt, so the mass of the body reads at night.</summary>
        private void ObsidianAnatomyH8S()
        {
            if (_detail == 0) return;
            float H = _H, hl = _headLen;
            Vector2 plate = BodyCell(ObsidianPaletteH8S.ChitinPlate), dark = BodyCell(ObsidianPaletteH8S.ChitinDark);
            Vector2 basalt = BodyCell(ObsidianPaletteH8S.Basalt), stone = BodyCell(ObsidianPaletteH8S.WeatheredStone);
            CreatureTube tb = _bodyTube;
            float tHip = _bodyT[2], tWither = _bodyT[7], tWaist = _bodyT[4], tRibs = _bodyT[6];
            // Scapular and haunch scutes: fans of small overlapping plates over the shoulder blade and the hip, so the
            // masses read as armoured muscle rather than one slab, and a jaw plate behind the cheek that leaves the mouth free.
            for (int side = -1; side <= 1; side += 2)
            {
                ScuteFan(tb, tWither - 0.035f, side > 0 ? 30f : 150f, side, 3, H * 0.12f, H * 0.085f, H * 0.016f, 13f, 0.018f, plate, dark);
                ScuteFan(tb, tHip + 0.02f, side > 0 ? 14f : 166f, side, 2, H * 0.11f, H * 0.08f, H * 0.014f, 15f, 0.02f, plate, dark);
                QuartzScaleOnTube(_headTube, 0.3f, side > 0 ? 338f : 202f, hl * 0.12f, hl * 0.07f, hl * 0.012f, plate, dark);
            }
            // Nape plates down the top of the neck.
            if (_neckTube != null)
                for (int i = 0; i < 3; i++)
                {
                    float t = Mathf.Lerp(0.3f, 0.78f, (i + 0.5f) / 3f);
                    QuartzScaleOnTube(_neckTube, t, 90f, hl * 0.09f, hl * 0.08f, hl * 0.012f, plate, dark);
                }
            // Guardians without quartz plates get rows of dark chitin scutes down the back and along the flanks.
            if (_k.WardLinesH8S && _k.Plates == 0)
            {
                int rows = _detail == 2 ? 8 : 5;
                for (int i = 0; i < rows; i++)
                {
                    float u = (i + 0.5f) / rows;
                    float t = Mathf.Lerp(tHip + 0.03f, tWither - 0.03f, u);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        QuartzScaleOnTube(tb, t, 90f + side * 24f, H * 0.1f, H * 0.075f, H * 0.014f, plate, dark);
                        if (_detail == 2) QuartzScaleOnTube(tb, t + 0.012f, 90f + side * 56f, H * 0.085f, H * 0.065f, H * 0.012f, plate, dark);
                    }
                }
            }
            // Rib ridges: low arcs down each flank between the waist and the chest.
            int ribs = _detail == 2 ? 3 : 2;
            for (int i = 0; i < ribs; i++)
            {
                float tc = Mathf.Lerp(tWaist + 0.01f, tRibs + 0.02f, (i + 0.5f) / ribs);
                for (int side = -1; side <= 1; side += 2)
                {
                    int s = side;
                    EmitSurfaceStrip(tb, u => new Vector2(tc - 0.015f * u, s > 0 ? 40f - u * 80f : 140f + u * 80f), u => 0.014f * H * Mathf.Sin(Mathf.Clamp01(u * 1.1f) * Mathf.PI), 7, plate, 0.004f * H);
                }
            }
            // Joint caps: basalt knuckles at the knees (and the hocks at hero).
            foreach (LegPlan leg in _legs)
            {
                float r = H * 0.055f * Mathf.Lerp(1f, Mathf.Max(0.6f, _k.Bulk), 0.5f);
                EmitEllipsoid(leg.Knee, new Vector3(r * 1.1f, r * 1.3f, r * 1.1f), QMath.Identity, 3, 6, basalt, W2(leg.Upper, leg.Lower, 0.5f));
                if (_detail == 2) EmitEllipsoid(leg.Ankle, new Vector3(r * 0.75f, r * 0.9f, r * 0.75f), QMath.Identity, 3, 6, basalt, W2(leg.Lower, leg.Foot, 0.5f));
            }
            // Guardians: carved ward lines, a wave on each side of the neck and a meander along each flank.
            if (_k.WardLinesH8S && _neckTube != null)
                for (int side = -1; side <= 1; side += 2)
                {
                    int s = side;
                    EmitSurfaceStrip(_neckTube, u => new Vector2(0.35f + 0.3f * u, s > 0 ? 50f - 30f * Mathf.Sin(u * Mathf.PI) : 130f + 30f * Mathf.Sin(u * Mathf.PI)), u => 0.008f * H, 8, stone, 0.003f * H);
                    EmitSurfaceStrip(tb, u => new Vector2(Mathf.Lerp(tHip + 0.05f, tWither - 0.06f, u), s > 0 ? 62f + 8f * Mathf.Sin(u * Mathf.PI * 3f) : 118f - 8f * Mathf.Sin(u * Mathf.PI * 3f)), u => 0.009f * H, 12, stone, 0.003f * H);
                }
        }

        /// <summary>A fan of count overlapping scutes on a tube, each the next one a little further around (dTheta degrees,
        /// toward side) and along (dT) the body, every scute a little smaller than the one before.</summary>
        private void ScuteFan(CreatureTube tb, float t, float theta, int side, int count, float length, float width, float raised, float dTheta, float dT, Vector2 face, Vector2 edge)
        {
            for (int i = 0; i < count; i++)
            {
                float k = 1f - 0.12f * i;
                QuartzScaleOnTube(tb, t - dT * i, theta - side * dTheta * i, length * k, width * k, raised * k, face, edge);
            }
        }

        // ================================================================== the wyrm

        /// <summary>Clusters down the wyrm's back every third ring plate, larger toward the collar, and a collar crown that is
        /// a ring of clusters rather than a comb of spikes.</summary>
        private void ObsidianSerpentH8S()
        {
            if (_wSegs.Length == 0 || _wyrmTube == null) return;
            float R = _wR;
            int n = _wSegs.Length;
            float zTail = -(n * _wSeg + 0.55f * R), len = _wRim - zTail;
            int shards = ShardsPerCluster;
            bool socket = _detail > 0;
            int step = _detail == 0 ? 4 : 3;
            for (int i = 1; i < n - 2; i += step)
            {
                float z = -(i + 0.75f) * _wSeg;
                float t = (z - zTail) / len;
                float big = Mathf.Lerp(1.1f, 0.55f, (float)i / n);
                float size = R * 0.42f * big * Rand(0.85f, 1.15f);
                Vector3 p, nr;
                _wyrmTube.Frame(t, 90f + Rand(-14f, 14f), out p, out nr);
                Cluster(p, nr, Vector3.back, size, size * 0.5f, shards, 0.5f, socket, WyrmWeight(z));
                if (_detail == 2 && i % 2 == 1)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        _wyrmTube.Frame(t, 90f + side * 40f, out p, out nr);
                        Cluster(p, nr, Vector3.back, size * 0.55f, size * 0.25f, 2, 0.5f, false, WyrmWeight(z));
                    }
            }
            // The collar crown.
            int crown = Segs(6, 5, 4);
            for (int k = 0; k < crown; k++)
            {
                float th = 90f + (k - (crown - 1) * 0.5f) * (190f / crown) + Rand(-6f, 6f);
                Vector3 p, nr;
                _wyrmTube.Frame((-0.12f * R - zTail) / len, th, out p, out nr);
                float size = R * (0.62f - 0.25f * Mathf.Abs(th - 90f) / 100f) * Rand(0.85f, 1.15f);
                Cluster(p, nr, Vector3.back, size, size * 0.4f, Mathf.Max(2, shards - 1), 0.6f, socket, W1(_wHead));
            }
        }

        // ================================================================== the shade

        /// <summary>The shade carries a halo of veil-blue shards arcing over the core and two trailing behind it.</summary>
        private void ObsidianWispH8S()
        {
            if (_sk.Head < 0) return;
            Vector3 c = _bm[_sk.Head];
            float size = Mathf.Max(0.2f, _k.BodyLength * _s);
            float rc = 0.24f * size * Mathf.Clamp(_k.Bulk, 0.6f, 1.5f);
            int shards = Mathf.Max(2, ShardsPerCluster - 1);
            int crown = _detail == 0 ? 2 : 3;
            for (int i = 0; i < crown; i++)
            {
                float a = (i - (crown - 1) * 0.5f) * 0.9f;
                Vector3 dir = Nrm(new Vector3(Mathf.Sin(a) * 0.8f, 0.75f, -0.3f * Mathf.Cos(a)));
                Cluster(c + dir * (rc * 0.8f), dir, Vector3.back, rc * 0.7f * Rand(0.85f, 1.15f), rc * 0.22f, shards, 0.5f, false, W1(_sk.Head));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 dir = Nrm(new Vector3(side * 0.7f, -0.25f, -0.65f));
                Cluster(c + dir * (rc * 0.85f), dir, Vector3.back, rc * 0.5f, rc * 0.15f, 2, 0.3f, false, W1(_sk.Head));
            }
        }

        // ================================================================== the mite

        /// <summary>A finite mineral cluster on legs: the abdomen carries a small amethyst cluster leaning back.</summary>
        private void ObsidianMiteH8S()
        {
            if (_sk.Chest < 0) return;
            float len = Mathf.Max(0.01f, _k.BodyLength * _s);
            Vector3 c = _bm[_sk.Chest];
            Vector3 top = c + new Vector3(0f, 0.17f * len, -0.2f * len);
            Cluster(top, Nrm(new Vector3(0f, 1f, -0.35f)), Vector3.back, 0.3f * len, 0.1f * len, 3, 0.5f, false, W1(_sk.Chest));
        }
    }
}
