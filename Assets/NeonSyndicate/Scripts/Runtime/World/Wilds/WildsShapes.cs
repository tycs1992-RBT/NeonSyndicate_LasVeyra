using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The material slot of a wilds mesh (one submesh each; WildsBuilder maps them to materials).</summary>
    public enum WildMat
    {
        RedRock, PaleRock, DarkRock, GreyRock, FrostRock, Sand, Crystal, Bark, Leaf, DryGrass, Juniper, PalmLeaf, Bone, Stone,
        Adobe, Wood, Cloth, ClothRed, Metal, Rust, Water, Ice, Snow, Ember, Glow, Salt, Shadow, Dirt, DryTurf, GlassSand
    }

    /// <summary>
    /// The geometry of every piece of the wilds (WildsLayout, WildsSites): faceted noise boulders, layered buttes with
    /// strata, hoodoo spires with cap rocks, rock arches, smooth wind dunes, quartz crystals and clusters, shrubs, grass,
    /// dead trees, junipers, palms with drooping fronds, reeds, ponds with a shore, ribs and skulls, ruins, a lookout
    /// tower, tents, a campfire, a crashed hull, standing stones and glowing obelisks, geysers, salt pans, ice spires,
    /// wind turbines, adobe huts and a pier. Pure (MeshBuilder lists and QMath, no native calls), so the offline tools
    /// render exactly what the game builds. A solid piece also adds a simpler collision shape to the second builder.
    /// </summary>
    public static class WildsShapes
    {
        public static readonly int MatCount = System.Enum.GetValues(typeof(WildMat)).Length;

        /// <summary>Builds one piece into the visual builder (one submesh per WildMat) and, when solid, into the collider builder.</summary>
        public static void Build(WildPiece p, MeshBuilder m, MeshBuilder col)
        {
            GRand r = new GRand(p.Seed | 1);
            Quaternion q = QMath.AY(p.Yaw);
            Vector3 o = p.Position;
            Vector3 s = p.Size;
            MeshBuilder c = p.Solid ? col : null;
            WildMat rock = RockOf(p.Biome);
            switch (p.Kind)
            {
                case WildPieceKind.Boulder: Rock(m, c, o, s, q, r, rock, Mathf.Max(s.x, s.z) > 2.5f ? 1 : 0, 0.22f); break;
                case WildPieceKind.Swell: Dune(m, c, o, s, q, r, GroundOf(p.Biome), true); break;
                case WildPieceKind.GroundPatch: Disc(m, (int)GroundOf(p.Biome), o + Vector3.up * (0.012f + (p.Seed % 7) * 0.002f), s.x * 0.5f, s.z * 0.5f, q, r, 22, 0.3f); break;
                case WildPieceKind.SnowBoulder:
                    Rock(m, c, o, s, q, r, WildMat.FrostRock, Mathf.Max(s.x, s.z) > 2.5f ? 1 : 0, 0.2f);
                    Rock(m, null, o + Vector3.up * s.y * 0.32f, new Vector3(s.x * 0.86f, s.y * 0.42f, s.z * 0.86f), q, r, WildMat.Snow, 1, 0.1f);
                    break;
                case WildPieceKind.Pebbles:
                    for (int i = r.Range(3, 8); i > 0; i--)
                    {
                        float k = r.Range(0.15f, 0.45f) * s.x;
                        Rock(m, null, o + new Vector3(r.Range(-s.x, s.x), 0f, r.Range(-s.x, s.x)), new Vector3(k, k * 0.6f, k), QMath.AY(r.Range(0f, 360f)), r, rock, 0, 0.25f);
                    }
                    break;
                case WildPieceKind.RockSpire: Spire(m, c, o, s, q, r, rock); break;
                case WildPieceKind.Butte: Butte(m, c, o, s, q, r, p.Biome); break;
                case WildPieceKind.Arch: Arch(m, c, o, s, q, r, rock); break;
                case WildPieceKind.Dune: Dune(m, c, o, s, q, r, WildMat.Sand, false); break;
                case WildPieceKind.Crystal: Crystal(m, c, o, s.x * 0.5f, s.y, QMath.AY(p.Yaw) * QMath.AX(r.Range(-14f, 14f)), r); break;
                case WildPieceKind.CrystalCluster: Cluster(m, c, o, s, r); break;
                case WildPieceKind.IceSpire: IceSpire(m, c, o, s, q, r); break;
                case WildPieceKind.Shrub: Shrub(m, o, s, r); break;
                case WildPieceKind.GrassTuft: Grass(m, o, s, r); break;
                case WildPieceKind.DeadTree: DeadTree(m, c, o, s, r, p.Biome == WildBiome.FrostTaiga); break;
                case WildPieceKind.Juniper: Juniper(m, c, o, s, r); break;
                case WildPieceKind.Palm: Palm(m, c, o, s, q, r); break;
                case WildPieceKind.Reeds: Reeds(m, o, s, r); break;
                case WildPieceKind.Pond: Pond(m, o, s, q, r, p.Biome == WildBiome.FrostTaiga); break;
                case WildPieceKind.RibBone: Rib(m, c, o, s, q); break;
                case WildPieceKind.Skull: Skull(m, c, o, s, q); break;
                case WildPieceKind.RuinWall: RuinWall(m, c, o, s, q, r); break;
                case WildPieceKind.RuinColumn: Column(m, c, o, s, q, r); break;
                case WildPieceKind.RuinArchway: Gateway(m, c, o, s, q, p.Biome == WildBiome.FrostTaiga ? WildMat.Ice : WildMat.Stone); break;
                case WildPieceKind.Watchtower: Tower(m, c, o, s, q); break;
                case WildPieceKind.Tent: Tent(m, c, o, s, q, r.Range(0f, 1f) < 0.4f ? WildMat.ClothRed : WildMat.Cloth); break;
                case WildPieceKind.Campfire: Campfire(m, o, s, r); break;
                case WildPieceKind.WreckHull: Hull(m, c, o, s, q, r, p.Biome == WildBiome.FrostTaiga); break;
                case WildPieceKind.WreckFin: m.Box((int)WildMat.Rust, o + Vector3.up * (s.y * 0.35f), s, q * QMath.AZ(r.Range(-35f, 35f)), UVMode.World); if (c != null) c.Box(0, o + Vector3.up * (s.y * 0.35f), s, q, UVMode.Fixed); break;
                case WildPieceKind.Monolith: Monolith(m, c, o, s, q, r); break;
                case WildPieceKind.Obelisk: Obelisk(m, c, o, s, q); break;
                case WildPieceKind.Geyser: Geyser(m, c, o, s); break;
                case WildPieceKind.SaltPan: Disc(m, (int)WildMat.Salt, o + Vector3.up * 0.025f, s.x * 0.5f, s.z * 0.5f, q, r, 28, 0.18f); break;
                case WildPieceKind.Turbine: Turbine(m, c, o, s, q); break;
                case WildPieceKind.Hut: Hut(m, c, o, s, q, p.Biome == WildBiome.FrostTaiga ? WildMat.Wood : WildMat.Adobe); break;
                case WildPieceKind.Pier: Pier(m, c, o, s, q); break;
                case WildPieceKind.StoneRing:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f * Mathf.Deg2Rad;
                        Monolith(m, c, o + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * s.x * 0.5f, new Vector3(1.8f, s.y, 1f), QMath.AY(i * 45f), r);
                    }
                    break;
            }
        }

        /// <summary>The ground colour of each land's patches and swells.</summary>
        public static WildMat GroundOf(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands: return WildMat.Dirt;
                case WildBiome.DuneSea: return WildMat.Sand;
                case WildBiome.CrystalBarrens: return WildMat.GlassSand;
                case WildBiome.FrostTaiga: return WildMat.Snow;
                default: return WildMat.DryTurf;
            }
        }

        /// <summary>The rock colour of each land.</summary>
        public static WildMat RockOf(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands: return WildMat.RedRock;
                case WildBiome.DuneSea: return WildMat.PaleRock;
                case WildBiome.CrystalBarrens: return WildMat.DarkRock;
                case WildBiome.FrostTaiga: return WildMat.FrostRock;
                default: return WildMat.GreyRock;
            }
        }

        // ------------------------------------------------------------------ rocks

        private static readonly Vector3[] IcoV =
        {
            new Vector3(-1f, 1.618f, 0f), new Vector3(1f, 1.618f, 0f), new Vector3(-1f, -1.618f, 0f), new Vector3(1f, -1.618f, 0f),
            new Vector3(0f, -1f, 1.618f), new Vector3(0f, 1f, 1.618f), new Vector3(0f, -1f, -1.618f), new Vector3(0f, 1f, -1.618f),
            new Vector3(1.618f, 0f, -1f), new Vector3(1.618f, 0f, 1f), new Vector3(-1.618f, 0f, -1f), new Vector3(-1.618f, 0f, 1f)
        };
        private static readonly int[] IcoF =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        /// <summary>
        /// A faceted rock: an icosahedron (subdivided once for detail 1), each corner pushed in or out by its seed, sized
        /// to the box, flattened underneath and sunk a little so it sits in the ground.
        /// </summary>
        public static void Rock(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 size, Quaternion q, GRand r, WildMat mat, int detail, float rough)
        {
            RockInto(m, (int)mat, col, o, size, q, r, detail, rough);
        }

        /// <summary>The same faceted rock into any submesh (the city builder's mountain ring uses it).</summary>
        public static void RockInto(MeshBuilder m, int sub, MeshBuilder col, Vector3 o, Vector3 size, Quaternion q, GRand r, int detail, float rough)
        {
            System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<int> f = new System.Collections.Generic.List<int>();
            foreach (Vector3 p in IcoV) v.Add(p.normalized);
            f.AddRange(IcoF);
            for (int d = 0; d < detail; d++) Subdivide(v, f);
            int seed = (int)(r.NextUInt() & 0xffff);
            Vector3 half = size * 0.5f;
            Vector3[] w = new Vector3[v.Count];
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 n = v[i];
                float k = 1f + (WildsLayout.Hash01(Mathf.RoundToInt(n.x * 97f), Mathf.RoundToInt(n.y * 89f + n.z * 53f), seed) - 0.5f) * 2f * rough;
                Vector3 p = new Vector3(n.x * half.x, n.y * half.y, n.z * half.z) * k;
                if (p.y < -half.y * 0.55f) p.y = -half.y * 0.55f;
                w[i] = o + q * (p + Vector3.up * (half.y * 0.45f));
            }
            for (int t = 0; t < f.Count; t += 3)
            {
                Vector3 a = w[f[t]], b = w[f[t + 1]], c = w[f[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                // Front faces wind so that Cross(b - a, c - a) points out (MeshBuilder's rule).
                m.Triangle(sub, a, b, c, n, Uv(a, n), Uv(b, n), Uv(c, n));
                if (col != null) col.Triangle(0, a, b, c, n, Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }

        private static void Subdivide(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<int> f)
        {
            System.Collections.Generic.Dictionary<long, int> mid = new System.Collections.Generic.Dictionary<long, int>();
            System.Collections.Generic.List<int> nf = new System.Collections.Generic.List<int>();
            System.Func<int, int, int> M = (a, b) =>
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                int idx;
                if (mid.TryGetValue(key, out idx)) return idx;
                v.Add(((v[a] + v[b]) * 0.5f).normalized);
                idx = v.Count - 1;
                mid[key] = idx;
                return idx;
            };
            for (int t = 0; t < f.Count; t += 3)
            {
                int a = f[t], b = f[t + 1], c = f[t + 2];
                int ab = M(a, b), bc = M(b, c), ca = M(c, a);
                nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            f.Clear();
            f.AddRange(nf);
        }

        /// <summary>World UVs projected along the face's strongest axis (4 m per tile).</summary>
        private static Vector2 Uv(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z) * 0.25f;
            if (ax >= az) return new Vector2(p.z, p.y) * 0.25f;
            return new Vector2(p.x, p.y) * 0.25f;
        }

        /// <summary>
        /// A ring-stacked rock body: rings of noisy radius at rising heights joined by faceted quads, with a lid. Used by
        /// buttes (wide, few rings, strata colours), spires and arch legs.
        /// </summary>
        private static void Stack(MeshBuilder m, MeshBuilder col, Vector3 o, Quaternion q, float[] heights, float[] radiiX, float[] radiiZ, WildMat[] mats, int sides, GRand r, float rough, bool lid)
        {
            int rings = heights.Length;
            Vector3[,] pts = new Vector3[rings, sides];
            int seed = (int)(r.NextUInt() & 0xffff);
            for (int k = 0; k < rings; k++)
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    float n = 1f + (WildsLayout.Hash01(i, k, seed) - 0.5f) * 2f * rough;
                    pts[k, i] = o + q * new Vector3(Mathf.Cos(a) * radiiX[k] * n, heights[k], Mathf.Sin(a) * radiiZ[k] * n);
                }
            for (int k = 0; k < rings - 1; k++)
                for (int i = 0; i < sides; i++)
                {
                    int j = (i + 1) % sides;
                    Vector3 a = pts[k, i], b = pts[k, j], c = pts[k + 1, j], d = pts[k + 1, i];
                    Vector3 n = Vector3.Cross(d - a, b - a).normalized;
                    int sub = (int)mats[Mathf.Min(k, mats.Length - 1)];
                    m.Quad(sub, a, d, c, b, n, Uv(a, n), Uv(d, n), Uv(c, n), Uv(b, n));
                    if (col != null) col.Quad(0, a, d, c, b, n, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                }
            if (!lid) return;
            Vector3 top = Vector3.zero;
            for (int i = 0; i < sides; i++) top += pts[rings - 1, i];
            top /= sides;
            top += Vector3.up * (radiiX[rings - 1] * 0.06f);
            int lidSub = (int)mats[mats.Length - 1];
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = pts[rings - 1, i], b = pts[rings - 1, (i + 1) % sides];
                Vector3 n = Vector3.Cross(b - top, a - top).normalized;
                m.Triangle(lidSub, top, b, a, n, Uv(top, Vector3.up), Uv(b, Vector3.up), Uv(a, Vector3.up));
                if (col != null) col.Triangle(0, top, b, a, n, Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }

        private static void Spire(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r, WildMat rock)
        {
            // A hoodoo: a column that narrows in waves, a darker cap rock balanced on top.
            float h = s.y, w = s.x * 0.5f;
            float[] hs = { -0.3f, h * r.Range(0.12f, 0.24f), h * r.Range(0.32f, 0.44f), h * r.Range(0.5f, 0.6f), h * r.Range(0.66f, 0.76f), h * 0.84f };
            float[] rx = { w * 1.25f, w * r.Range(0.95f, 1.1f), w * r.Range(0.7f, 0.85f), w * r.Range(0.8f, 0.95f), w * r.Range(0.58f, 0.7f), w * 0.55f };
            // One pale band at most, at a random height, so spires do not read as striped.
            WildMat alt = rock == WildMat.RedRock ? WildMat.PaleRock : rock;
            int band = r.Range(0, 5);
            WildMat[] mats = { rock, rock, rock, rock, rock };
            if (r.Range(0f, 1f) < 0.6f) mats[band] = alt;
            Stack(m, col, o, q, hs, rx, rx, mats, 9, r, 0.16f, true);
            Rock(m, col, o + Vector3.up * (h * 0.8f), new Vector3(w * 2.3f, h * 0.2f, w * 2.1f), q * QMath.AZ(r.Range(-8f, 8f)), r, rock == WildMat.RedRock ? WildMat.DarkRock : rock, 1, 0.15f);
        }

        private static void Butte(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r, WildBiome biome)
        {
            // Three tiers stepping inward with strata bands, a flat top, and fallen talus round the foot.
            float h = s.y, wx = s.x * 0.5f, wz = s.z * 0.5f;
            WildMat a = RockOf(biome), b = biome == WildBiome.HoodooBadlands ? WildMat.PaleRock : (biome == WildBiome.FrostTaiga ? WildMat.Snow : WildMat.GreyRock);
            float[] hs = { -0.5f, h * 0.22f, h * 0.36f, h * 0.62f, h * 0.74f, h };
            float[] rx = { wx * 1.08f, wx, wx * 0.9f, wx * 0.86f, wx * 0.8f, wx * 0.76f };
            float[] rz = { wz * 1.08f, wz, wz * 0.9f, wz * 0.86f, wz * 0.8f, wz * 0.76f };
            WildMat lid = biome == WildBiome.FrostTaiga ? WildMat.Snow : a;
            Stack(m, col, o, q, hs, rx, rz, new[] { a, a, b, a, a, lid }, 14, r, 0.09f, true);
            for (int i = r.Range(6, 12); i > 0; i--)
            {
                float ang = r.Range(0f, Mathf.PI * 2f), k = r.Range(1.6f, 4.5f);
                Vector3 at = o + q * new Vector3(Mathf.Cos(ang) * wx * 1.15f, 0f, Mathf.Sin(ang) * wz * 1.15f);
                Rock(m, col, at, new Vector3(k, k * 0.7f, k * 0.9f), QMath.AY(r.Range(0f, 360f)), r, a, 0, 0.25f);
            }
        }

        private static void Arch(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r, WildMat rock)
        {
            // Two legs and a span of overlapping rocks along a flattened half circle.
            float span = s.x * 0.5f, h = s.y, t = s.z;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 foot = o + q * new Vector3(side * span * 0.82f, 0f, 0f);
                float[] hs = { -0.3f, h * 0.35f, h * 0.7f };
                float[] rr = { t * 0.75f, t * 0.6f, t * 0.55f };
                Stack(m, col, foot, q, hs, rr, rr, new[] { rock }, 8, r, 0.15f, false);
            }
            for (int i = 0; i <= 8; i++)
            {
                float a = Mathf.PI * i / 8f;
                Vector3 at = o + q * new Vector3(-Mathf.Cos(a) * span * 0.82f, h * 0.55f + Mathf.Sin(a) * h * 0.38f, 0f);
                Rock(m, col, at - Vector3.up * (t * 0.3f), new Vector3(t * 1.3f, t * 0.95f, t * 1.05f), q * QMath.AZ(r.Range(-20f, 20f)), r, rock, 1, 0.14f);
            }
        }

        // ------------------------------------------------------------------ sand

        private static void Dune(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r, WildMat mat, bool round)
        {
            // A smooth wind dune: gentle on the windward side (-x), a steeper slip face (+x), crest bent in plan. A round
            // swell is the same grid with an even dome profile, for gentle rises of the land itself.
            const int N = 12;
            float hx = s.x * 0.5f, hz = s.z * 0.5f, H = s.y;
            float bend = r.Range(-0.25f, 0.25f);
            Vector3[,] p = new Vector3[N + 1, N + 1];
            for (int i = 0; i <= N; i++)
                for (int j = 0; j <= N; j++)
                {
                    float u = i / (float)N * 2f - 1f, v = j / (float)N * 2f - 1f;
                    float y;
                    if (round)
                    {
                        float d2 = Mathf.Clamp01(u * u + v * v);
                        y = H * (1f - d2) * (1f - d2) - 0.08f;
                    }
                    else
                    {
                        float uc = u - bend * v * v;
                        float side = uc < 0.25f ? (uc + 1f) / 1.25f : (1f - uc) / 0.75f;
                        side = Mathf.Clamp01(side);
                        float along = Mathf.Clamp01(1f - v * v);
                        y = H * Mathf.Pow(side, uc < 0.25f ? 1.4f : 0.9f) * Mathf.Pow(along, 0.8f) - 0.06f;
                    }
                    p[i, j] = o + q * new Vector3(u * hx, y, v * hz);
                }
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    Vector3 a = p[i, j], b = p[i + 1, j], c = p[i + 1, j + 1], d = p[i, j + 1];
                    Vector3 na = DuneNormal(p, i, j, N), nb = DuneNormal(p, i + 1, j, N), nc = DuneNormal(p, i + 1, j + 1, N), nd = DuneNormal(p, i, j + 1, N);
                    m.QuadSmooth((int)mat, a, d, c, b, na, nd, nc, nb, Uv(a, Vector3.up), Uv(d, Vector3.up), Uv(c, Vector3.up), Uv(b, Vector3.up));
                    if (col != null) col.Quad(0, a, d, c, b, Vector3.up, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                }
        }

        private static Vector3 DuneNormal(Vector3[,] p, int i, int j, int n)
        {
            Vector3 dx = p[Mathf.Min(n, i + 1), j] - p[Mathf.Max(0, i - 1), j];
            Vector3 dz = p[i, Mathf.Min(n, j + 1)] - p[i, Mathf.Max(0, j - 1)];
            Vector3 nn = Vector3.Cross(dz, dx).normalized;
            return nn.y < 0f ? -nn : nn;
        }

        // ------------------------------------------------------------------ crystals and ice

        /// <summary>A six-sided quartz prism with a pointed tip, standing on o along the rotated up axis.</summary>
        public static void Crystal(MeshBuilder m, MeshBuilder col, Vector3 o, float radius, float height, Quaternion q, GRand r)
        {
            Prism(m, (int)WildMat.Crystal, o - q * Vector3.up * 0.3f, radius, height, 6, q, 0.26f);
            if (col != null) col.Cylinder(0, o, radius, radius * 0.6f, height * 0.8f, 6, q, true, UVMode.Fixed);
        }

        private static void Prism(MeshBuilder m, int sub, Vector3 o, float radius, float height, int sides, Quaternion q, float tipShare)
        {
            float body = height * (1f - tipShare);
            Vector3 up = q * Vector3.up;
            Vector3 tip = o + up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = q * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = q * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = o + d0 * radius, b1 = o + d1 * radius, t0 = b0 + up * body, t1 = b1 + up * body;
                Vector3 n = ((d0 + d1) * 0.5f).normalized;
                m.Quad(sub, b0, t0, t1, b1, n, new Vector2(i / (float)sides, 0f), new Vector2(i / (float)sides, 1f), new Vector2((i + 1f) / sides, 1f), new Vector2((i + 1f) / sides, 0f));
                Vector3 tn = Vector3.Cross(t1 - t0, tip - t0).normalized;
                if (Vector3.Dot(tn, n) < 0f) tn = -tn;
                m.Triangle(sub, t0, tip, t1, tn, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f));
            }
        }

        private static void Cluster(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, GRand r)
        {
            int n = r.Range(4, 9);
            for (int i = 0; i < n; i++)
            {
                float a = r.Range(0f, 360f), lean = i == 0 ? r.Range(0f, 8f) : r.Range(18f, 48f);
                float h = s.y * (i == 0 ? 1f : r.Range(0.35f, 0.8f));
                Quaternion q = QMath.AY(a) * QMath.AX(lean);
                Crystal(m, i == 0 ? col : null, o + QMath.AY(a) * Vector3.forward * r.Range(0f, s.x * 0.2f), Mathf.Max(0.12f, h * 0.13f), h, q, r);
            }
            Rock(m, null, o, new Vector3(s.x * 0.7f, s.y * 0.15f, s.x * 0.7f), QMath.AY(r.Range(0f, 360f)), r, WildMat.DarkRock, 0, 0.2f);
        }

        private static void IceSpire(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r)
        {
            int n = r.Range(1, 4);
            for (int i = 0; i < n; i++)
            {
                float h = s.y * (i == 0 ? 1f : r.Range(0.4f, 0.75f));
                Quaternion t = q * QMath.AY(r.Range(0f, 360f)) * QMath.AX(i == 0 ? r.Range(-6f, 6f) : r.Range(14f, 30f));
                Prism(m, (int)WildMat.Ice, o - Vector3.up * 0.4f, s.x * 0.5f * (i == 0 ? 1f : 0.6f), h, 5, t, 0.35f);
            }
            if (col != null) col.Cylinder(0, o, s.x * 0.45f, s.x * 0.2f, s.y * 0.85f, 5, q, true, UVMode.Fixed);
        }

        // ------------------------------------------------------------------ plants

        private static void Shrub(MeshBuilder m, Vector3 o, Vector3 s, GRand r)
        {
            int n = r.Range(2, 5);
            for (int i = 0; i < n; i++)
            {
                float k = s.x * r.Range(0.45f, 0.75f);
                Vector3 at = o + new Vector3(r.Range(-0.3f, 0.3f) * s.x, 0f, r.Range(-0.3f, 0.3f) * s.x);
                Rock(m, null, at, new Vector3(k, k * 0.75f, k), QMath.AY(r.Range(0f, 360f)), r, i % 3 == 2 ? WildMat.DryGrass : WildMat.Leaf, 0, 0.3f);
            }
        }

        private static void Grass(MeshBuilder m, Vector3 o, Vector3 s, GRand r)
        {
            int n = r.Range(6, 11);
            for (int i = 0; i < n; i++)
            {
                float a = r.Range(0f, Mathf.PI * 2f), lean = r.Range(0.15f, 0.5f), h = s.y * r.Range(0.6f, 1.2f), w = 0.05f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0f, dir.x) * w;
                Vector3 b = o + dir * r.Range(0f, s.x * 0.3f);
                Vector3 t = b + dir * (lean * h) + Vector3.up * h;
                DoubleTri(m, (int)WildMat.DryGrass, b - side, b + side, t);
            }
        }

        private static void DoubleTri(MeshBuilder m, int sub, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            m.Triangle(sub, a, c, b, -n, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f));
            m.Triangle(sub, a, b, c, n, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f));
        }

        private static void Branch(MeshBuilder m, MeshBuilder col, Vector3 from, Vector3 dir, float length, float radius, int depth, GRand r, WildMat mat)
        {
            Vector3 to = from + dir * length;
            m.Cylinder((int)mat, from, radius, radius * 0.62f, length, 5, QMath.FromTo(Vector3.up, dir), false, UVMode.World);
            if (col != null && depth == 0) col.Cylinder(0, from, radius, radius * 0.7f, length, 5, QMath.FromTo(Vector3.up, dir), false, UVMode.Fixed);
            if (depth >= 2 || radius < 0.03f) return;
            int kids = r.Range(2, 4);
            for (int i = 0; i < kids; i++)
            {
                Vector3 side = QMath.AY(r.Range(0f, 360f)) * Vector3.forward;
                Vector3 nd = (dir * 0.6f + side * r.Range(0.4f, 0.9f) + Vector3.up * r.Range(0.1f, 0.5f)).normalized;
                Branch(m, null, Vector3.Lerp(from, to, r.Range(0.55f, 0.95f)), nd, length * r.Range(0.45f, 0.7f), radius * 0.55f, depth + 1, r, mat);
            }
        }

        private static void DeadTree(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, GRand r, bool frost)
        {
            Vector3 dir = (Vector3.up + new Vector3(r.Range(-0.15f, 0.15f), 0f, r.Range(-0.15f, 0.15f))).normalized;
            Branch(m, col, o - Vector3.up * 0.2f, dir, s.y * 0.6f, s.y * 0.045f, 0, r, frost ? WildMat.FrostRock : WildMat.Bark);
        }

        private static void Juniper(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, GRand r)
        {
            Vector3 lean = new Vector3(r.Range(-0.3f, 0.3f), 1f, r.Range(-0.3f, 0.3f)).normalized;
            float trunk = s.y * 0.45f;
            m.Cylinder((int)WildMat.Bark, o - Vector3.up * 0.2f, s.y * 0.06f, s.y * 0.04f, trunk, 6, QMath.FromTo(Vector3.up, lean), false, UVMode.World);
            if (col != null) col.Cylinder(0, o, s.y * 0.06f, s.y * 0.05f, trunk, 5, QMath.FromTo(Vector3.up, lean), false, UVMode.Fixed);
            Vector3 top = o + lean * trunk;
            int blobs = r.Range(3, 6);
            for (int i = 0; i < blobs; i++)
            {
                float k = s.x * r.Range(0.4f, 0.65f);
                Vector3 at = top + new Vector3(r.Range(-0.35f, 0.35f) * s.x, r.Range(-0.1f, 0.35f) * s.y, r.Range(-0.35f, 0.35f) * s.x);
                Rock(m, null, at - Vector3.up * (k * 0.3f), new Vector3(k, k * 0.7f, k), QMath.AY(r.Range(0f, 360f)), r, WildMat.Juniper, 0, 0.25f);
            }
        }

        private static void Palm(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r)
        {
            // A trunk bending away from the water in eight segments, then nine fronds drooping from the crown.
            float h = s.y;
            Vector3 lean = q * Vector3.forward * r.Range(0.12f, 0.3f);
            Vector3 prev = o - Vector3.up * 0.2f;
            const int seg = 8;
            for (int i = 1; i <= seg; i++)
            {
                float t = i / (float)seg;
                Vector3 next = o + Vector3.up * (h * t) + lean * h * t * t;
                float rad = Mathf.Lerp(0.24f, 0.14f, t);
                Vector3 d = next - prev;
                m.Cylinder((int)WildMat.Bark, prev, rad * 1.05f, rad, d.magnitude, 7, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.World);
                if (col != null && i <= 3) col.Cylinder(0, prev, rad, rad, d.magnitude, 5, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.Fixed);
                prev = next;
            }
            int fronds = r.Range(8, 11);
            for (int f = 0; f < fronds; f++)
            {
                float a = f * Mathf.PI * 2f / fronds + r.Range(-0.2f, 0.2f);
                Vector3 out1 = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 side = new Vector3(-out1.z, 0f, out1.x);
                float len = h * r.Range(0.32f, 0.45f);
                Vector3 p0 = prev, n0 = prev;
                for (int k = 1; k <= 5; k++)
                {
                    float t = k / 5f;
                    Vector3 p1 = prev + out1 * (len * t) + Vector3.up * (len * (0.35f * t - 0.75f * t * t));
                    float w0 = 0.55f * Mathf.Sin(Mathf.PI * (t - 0.2f)), w1 = 0.55f * Mathf.Sin(Mathf.PI * t);
                    w0 = Mathf.Max(0.05f, w0);
                    w1 = Mathf.Max(0.02f, w1);
                    Vector3 a0 = p0 - side * w0, b0 = p0 + side * w0, a1 = p1 - side * w1, b1 = p1 + side * w1;
                    DoubleQuad(m, (int)WildMat.PalmLeaf, a0, a1, b1, b0);
                    p0 = p1;
                }
            }
        }

        private static void DoubleQuad(MeshBuilder m, int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, d - a).normalized;
            Vector2 u0 = new Vector2(0f, 0f), u1 = new Vector2(0f, 1f), u2 = new Vector2(1f, 1f), u3 = new Vector2(1f, 0f);
            m.Quad(sub, a, b, c, d, n, u0, u1, u2, u3);
            m.Quad(sub, a, d, c, b, -n, u0, u3, u2, u1);
        }

        private static void Reeds(MeshBuilder m, Vector3 o, Vector3 s, GRand r)
        {
            int n = r.Range(10, 18);
            for (int i = 0; i < n; i++)
            {
                Vector3 b = o + new Vector3(r.Range(-0.5f, 0.5f) * s.x, 0f, r.Range(-0.5f, 0.5f) * s.z);
                Vector3 d = (Vector3.up + new Vector3(r.Range(-0.2f, 0.2f), 0f, r.Range(-0.2f, 0.2f))).normalized;
                m.Cylinder((int)WildMat.Leaf, b, 0.025f, 0.01f, s.y * r.Range(0.7f, 1.3f), 3, QMath.FromTo(Vector3.up, d), false, UVMode.Fixed);
            }
        }

        // ------------------------------------------------------------------ water and flats

        private static void Disc(MeshBuilder m, int sub, Vector3 o, float rx, float rz, Quaternion q, GRand r, int sides, float rough)
        {
            int seed = (int)(r.NextUInt() & 0xffff);
            Vector3 prev = Vector3.zero, first = Vector3.zero;
            for (int i = 0; i <= sides; i++)
            {
                float a = (i % sides) * Mathf.PI * 2f / sides;
                float k = 1f + (WildsLayout.Hash01(i % sides, 3, seed) - 0.5f) * 2f * rough;
                Vector3 p = o + q * new Vector3(Mathf.Cos(a) * rx * k, 0f, Mathf.Sin(a) * rz * k);
                if (i > 0) m.Triangle(sub, o, p, prev, Vector3.up, Uv(o, Vector3.up), Uv(p, Vector3.up), Uv(prev, Vector3.up));
                else first = p;
                prev = p;
            }
        }

        private static void Pond(MeshBuilder m, Vector3 o, Vector3 s, Quaternion q, GRand r, bool warm)
        {
            GRand shore = new GRand((int)r.NextUInt());
            GRand water = new GRand((int)shore.NextUInt());
            Disc(m, (int)WildMat.Sand, o + Vector3.up * 0.02f, s.x * 0.62f, s.z * 0.62f, q, new GRand(7), 24, 0.12f);
            Disc(m, (int)WildMat.Water, o + Vector3.up * 0.06f, s.x * 0.5f, s.z * 0.5f, q, new GRand(7), 24, 0.12f);
            if (warm) for (int i = 0; i < 6; i++) Rock(m, null, o + q * new Vector3(water.Range(-0.4f, 0.4f) * s.x, 0f, water.Range(-0.4f, 0.4f) * s.z), new Vector3(0.6f, 0.3f, 0.6f), q, shore, WildMat.FrostRock, 0, 0.2f);
        }

        private static void Geyser(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s)
        {
            m.Cylinder((int)WildMat.Salt, o - Vector3.up * 0.05f, s.x * 0.5f, s.x * 0.12f, s.y, 12, QMath.Identity, false, UVMode.World);
            m.Cylinder((int)WildMat.Shadow, o + Vector3.up * (s.y - 0.02f), s.x * 0.12f, s.x * 0.12f, 0.03f, 10, QMath.Identity, true, UVMode.Fixed);
            if (col != null) col.Cylinder(0, o, s.x * 0.5f, s.x * 0.12f, s.y, 8, QMath.Identity, true, UVMode.Fixed);
        }

        // ------------------------------------------------------------------ bones

        private static void Rib(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            // Rises from the ground and arches over toward the spine line (local -z), tapering.
            const int seg = 9;
            Vector3 prev = o;
            for (int i = 1; i <= seg; i++)
            {
                float t = i / (float)seg, a = t * Mathf.PI * 0.62f;
                Vector3 next = o + q * new Vector3(0f, Mathf.Sin(a) * s.y, (1f - Mathf.Cos(a)) * -s.z * 0.55f);
                float rad = Mathf.Lerp(s.x * 0.5f, s.x * 0.2f, t);
                Vector3 d = next - prev;
                m.Cylinder((int)WildMat.Bone, prev, rad * 1.08f, rad, d.magnitude, 7, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.World);
                if (col != null && i <= 4) col.Cylinder(0, prev, rad, rad, d.magnitude, 5, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.Fixed);
                prev = next;
            }
        }

        private static void Skull(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            Vector3 c = o + Vector3.up * (s.y * 0.4f);
            m.Sphere((int)WildMat.Bone, c, s * 0.5f, 6, 10, q);
            m.Sphere((int)WildMat.Bone, c + q * new Vector3(0f, -s.y * 0.18f, s.z * 0.42f), new Vector3(s.x * 0.32f, s.y * 0.22f, s.z * 0.36f), 4, 8, q);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Sphere((int)WildMat.Shadow, c + q * new Vector3(side * s.x * 0.22f, s.y * 0.08f, s.z * 0.4f), new Vector3(s.x * 0.12f, s.y * 0.12f, s.z * 0.1f), 4, 8, q);
                Vector3 horn = c + q * new Vector3(side * s.x * 0.38f, s.y * 0.25f, s.z * 0.05f);
                Vector3 dir = q * new Vector3(side * 0.7f, 0.6f, -0.4f).normalized;
                m.Cylinder((int)WildMat.Bone, horn, s.x * 0.09f, s.x * 0.015f, s.y * 0.9f, 6, QMath.FromTo(Vector3.up, dir), false, UVMode.World);
            }
            if (col != null) col.Box(0, c, s * 0.9f, q, UVMode.Fixed);
        }

        // ------------------------------------------------------------------ ruins and camps

        private static void RuinWall(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(s.x / 1.4f));
            float w = s.x / n;
            for (int i = 0; i < n; i++)
            {
                if (i > 0 && i < n - 1 && r.Range(0f, 1f) < 0.15f) continue;
                float h = s.y * r.Range(0.35f, 1f);
                Vector3 c = o + q * new Vector3(-s.x * 0.5f + (i + 0.5f) * w, h * 0.5f - 0.1f, 0f);
                m.Box((int)WildMat.Stone, c, new Vector3(w * 1.02f, h, s.z), q, UVMode.World);
                if (col != null) col.Box(0, c, new Vector3(w, h, s.z), q, UVMode.Fixed);
            }
            for (int i = r.Range(1, 4); i > 0; i--)
                Rock(m, null, o + q * new Vector3(r.Range(-0.5f, 0.5f) * s.x, 0f, r.Range(0.8f, 1.6f) * (r.Range(0f, 1f) < 0.5f ? -1f : 1f)), new Vector3(0.7f, 0.4f, 0.5f), q, r, WildMat.Stone, 0, 0.1f);
        }

        private static void Column(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r)
        {
            float rad = s.x * 0.5f;
            m.Box((int)WildMat.Stone, o + Vector3.up * 0.2f, new Vector3(s.x * 1.4f, 0.4f, s.x * 1.4f), q, UVMode.World);
            float h = s.y;
            Quaternion tilt = q * QMath.AZ(r.Range(-4f, 4f));
            m.Cylinder((int)WildMat.Stone, o + Vector3.up * 0.4f, rad, rad * 0.9f, h - 0.4f, 10, tilt, true, UVMode.World);
            if (h > 4f) m.Box((int)WildMat.Stone, o + Vector3.up * h, new Vector3(s.x * 1.3f, 0.35f, s.x * 1.3f), tilt, UVMode.World);
            if (col != null) col.Box(0, o + Vector3.up * (h * 0.5f), new Vector3(s.x, h, s.x), q, UVMode.Fixed);
        }

        private static void Gateway(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, WildMat mat)
        {
            float pier = s.x * 0.18f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 c = o + q * new Vector3(side * (s.x * 0.5f - pier * 0.5f), s.y * 0.5f, 0f);
                m.Box((int)mat, c, new Vector3(pier, s.y, s.z), q, UVMode.World);
                if (col != null) col.Box(0, c, new Vector3(pier, s.y, s.z), q, UVMode.Fixed);
            }
            Vector3 lintel = o + q * new Vector3(0f, s.y * 0.88f, 0f);
            m.Box((int)mat, lintel, new Vector3(s.x * 1.05f, s.y * 0.24f, s.z * 1.1f), q, UVMode.World);
            if (col != null) col.Box(0, lintel, new Vector3(s.x, s.y * 0.24f, s.z), q, UVMode.Fixed);
            // The dark way in.
            m.Box((int)WildMat.Shadow, o + q * new Vector3(0f, s.y * 0.38f, s.z * 0.2f), new Vector3(s.x - pier * 2f, s.y * 0.76f, s.z * 0.6f), q, UVMode.Fixed);
        }

        private static void Tower(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            float h = s.y, half = s.x * 0.5f, deck = h * 0.72f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? -1f : 1f, sz = (i & 2) == 0 ? -1f : 1f;
                Vector3 bottom = o + q * new Vector3(sx * half, -0.3f, sz * half), top = o + q * new Vector3(sx * half * 0.6f, deck, sz * half * 0.6f);
                Vector3 d = top - bottom;
                m.Cylinder((int)WildMat.Wood, bottom, 0.22f, 0.18f, d.magnitude, 6, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.World);
                Vector3 post = top;
                m.Box((int)WildMat.Wood, post + Vector3.up * ((h - deck) * 0.5f), new Vector3(0.18f, h - deck, 0.18f), q, UVMode.World);
                if (col != null) col.Cylinder(0, bottom, 0.25f, 0.25f, d.magnitude, 5, QMath.FromTo(Vector3.up, d.normalized), false, UVMode.Fixed);
            }
            for (int k = 1; k <= 3; k++)
            {
                float y = deck * k / 4f, w = Mathf.Lerp(half, half * 0.6f, k / 4f) * 2f;
                m.Box((int)WildMat.Wood, o + q * new Vector3(0f, y, -w * 0.5f), new Vector3(w, 0.15f, 0.15f), q, UVMode.World);
                m.Box((int)WildMat.Wood, o + q * new Vector3(0f, y, w * 0.5f), new Vector3(w, 0.15f, 0.15f), q, UVMode.World);
            }
            float dw = half * 1.5f;
            m.Box((int)WildMat.Wood, o + Vector3.up * deck, new Vector3(dw * 1.15f, 0.25f, dw * 1.15f), q, UVMode.World);
            if (col != null) col.Box(0, o + Vector3.up * deck, new Vector3(dw * 1.15f, 0.25f, dw * 1.15f), q, UVMode.Fixed);
            for (int side = 0; side < 4; side++)
            {
                Quaternion t = q * QMath.AY(side * 90f);
                m.Box((int)WildMat.Wood, o + t * new Vector3(0f, deck + 0.6f, dw * 0.57f), new Vector3(dw * 1.15f, 0.1f, 0.1f), t, UVMode.World);
            }
            Vector3 roofBase = o + Vector3.up * h;
            m.Cylinder((int)WildMat.ClothRed, roofBase, dw * 0.95f, 0.05f, h * 0.18f, 4, q * QMath.AY(45f), true, UVMode.Unit);
            // Ladder on the front leg line.
            Vector3 l0 = o + q * new Vector3(0f, 0f, -half - 0.3f), l1 = o + q * new Vector3(0f, deck, -dw * 0.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = l0 + q * new Vector3(side * 0.3f, 0f, 0f), b = l1 + q * new Vector3(side * 0.3f, 0f, 0f);
                m.Cylinder((int)WildMat.Wood, a, 0.05f, 0.05f, (b - a).magnitude, 4, QMath.FromTo(Vector3.up, (b - a).normalized), false, UVMode.Fixed);
            }
            for (float t = 0.1f; t < 1f; t += 0.08f)
                m.Box((int)WildMat.Wood, Vector3.Lerp(l0, l1, t), new Vector3(0.65f, 0.05f, 0.05f), q, UVMode.Fixed);
        }

        private static void Tent(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, WildMat cloth)
        {
            float hx = s.x * 0.5f, hz = s.z * 0.5f, h = s.y;
            Vector3 a = o + q * new Vector3(-hx, 0f, -hz), b = o + q * new Vector3(hx, 0f, -hz), c = o + q * new Vector3(hx, 0f, hz), d = o + q * new Vector3(-hx, 0f, hz);
            Vector3 r0 = o + q * new Vector3(0f, h, -hz * 1.05f), r1 = o + q * new Vector3(0f, h, hz * 1.05f);
            DoubleQuad(m, (int)cloth, a, r0, r1, d);
            DoubleQuad(m, (int)cloth, b, c, r1, r0);
            Vector3 nb = q * Vector3.back;
            m.Triangle((int)WildMat.Shadow, a, r0, b, nb, Vector2.zero, Vector2.zero, Vector2.zero);
            m.Triangle((int)cloth, d, c, r1, -nb, Vector2.zero, Vector2.zero, Vector2.zero);
            m.Cylinder((int)WildMat.Wood, o + q * new Vector3(0f, 0f, -hz * 1.05f), 0.05f, 0.05f, h + 0.3f, 4, q, false, UVMode.Fixed);
            if (col != null) col.Box(0, o + Vector3.up * (h * 0.4f), new Vector3(s.x * 0.8f, h * 0.8f, s.z), q, UVMode.Fixed);
        }

        private static void Campfire(MeshBuilder m, Vector3 o, Vector3 s, GRand r)
        {
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                Rock(m, null, o + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * s.x * 0.45f, new Vector3(0.35f, 0.25f, 0.3f), QMath.AY(i * 40f), r, WildMat.GreyRock, 0, 0.2f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = QMath.AY(i * 90f + 20f) * new Vector3(0.6f, 0.5f, 0f).normalized;
                Vector3 from = o + QMath.AY(i * 90f + 20f) * new Vector3(-s.x * 0.3f, 0f, 0f);
                m.Cylinder((int)WildMat.Bark, from, 0.08f, 0.06f, s.x * 0.45f, 5, QMath.FromTo(Vector3.up, d), false, UVMode.Fixed);
            }
            Rock(m, null, o, new Vector3(s.x * 0.35f, s.y * 0.5f, s.x * 0.35f), QMath.Identity, r, WildMat.Ember, 0, 0.3f);
        }

        private static void Hull(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r, bool frozen)
        {
            // A long ship body, half buried and rolled on its side, with ribs where it broke and a fin standing up.
            Quaternion t = q * QMath.AZ(frozen ? 12f : 24f) * QMath.AX(frozen ? -4f : 7f);
            Vector3 c = o + Vector3.up * (s.y * 0.18f);
            WildMat skin = frozen ? WildMat.Wood : WildMat.Metal;
            m.Sphere((int)skin, c, new Vector3(s.x * 0.5f, s.y * 0.5f, s.z * 0.4f), 8, 16, t);
            m.Sphere((int)WildMat.Rust, c + t * new Vector3(0f, s.y * 0.05f, s.z * 0.02f), new Vector3(s.x * 0.47f, s.y * 0.38f, s.z * 0.36f), 6, 12, t);
            for (int i = 0; i < 6; i++)
            {
                float z = s.z * (0.36f + i * 0.04f);
                for (int k = 0; k < 7; k++)
                {
                    float a0 = Mathf.PI * (0.1f + k * 0.12f), a1 = Mathf.PI * (0.1f + (k + 1) * 0.12f);
                    Vector3 p0 = c + t * new Vector3(Mathf.Cos(a0) * s.x * 0.45f, Mathf.Sin(a0) * s.y * 0.45f, z);
                    Vector3 p1 = c + t * new Vector3(Mathf.Cos(a1) * s.x * 0.45f, Mathf.Sin(a1) * s.y * 0.45f, z);
                    m.Cylinder((int)WildMat.Rust, p0, 0.14f, 0.14f, (p1 - p0).magnitude, 5, QMath.FromTo(Vector3.up, (p1 - p0).normalized), false, UVMode.Fixed);
                }
            }
            m.Box((int)skin, c + t * new Vector3(0f, s.y * 0.55f, -s.z * 0.2f), new Vector3(0.5f, s.y * 0.5f, s.z * 0.25f), t, UVMode.World);
            if (col != null) col.Sphere(0, c, new Vector3(s.x * 0.5f, s.y * 0.5f, s.z * 0.4f), 5, 8, t);
        }

        // ------------------------------------------------------------------ stones, beacons, machines, huts

        private static void Monolith(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, GRand r)
        {
            Quaternion t = q * QMath.AX(r.Range(-5f, 5f)) * QMath.AZ(r.Range(-4f, 4f));
            m.Box((int)WildMat.Stone, o + t * new Vector3(0f, s.y * 0.35f, 0f), new Vector3(s.x, s.y * 0.75f, s.z), t, UVMode.World);
            m.Box((int)WildMat.Stone, o + t * new Vector3(0f, s.y * 0.85f, 0f), new Vector3(s.x * 0.82f, s.y * 0.3f, s.z * 0.85f), t, UVMode.World);
            if (col != null) col.Box(0, o + Vector3.up * (s.y * 0.5f), new Vector3(s.x, s.y, s.z), q, UVMode.Fixed);
        }

        private static void Obelisk(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            float w = s.x * 0.5f, h = s.y;
            m.Box((int)WildMat.DarkRock, o + Vector3.up * 0.3f, new Vector3(s.x * 1.8f, 0.6f, s.x * 1.8f), q, UVMode.World);
            m.Cylinder((int)WildMat.DarkRock, o + Vector3.up * 0.6f, w * 1.41f, w * 0.95f, h * 0.88f, 4, q * QMath.AY(45f), false, UVMode.World);
            m.Cylinder((int)WildMat.Glow, o + Vector3.up * (0.6f + h * 0.88f), w * 0.98f, 0.02f, h * 0.12f, 4, q * QMath.AY(45f), false, UVMode.Fixed);
            for (int k = 0; k < 3; k++)
            {
                float y = 0.6f + h * (0.25f + k * 0.2f);
                float rr = Mathf.Lerp(w * 1.41f, w * 0.95f, (y - 0.6f) / (h * 0.88f)) * 0.72f;
                m.Box((int)WildMat.Glow, o + Vector3.up * y, new Vector3(rr * 2.04f, 0.18f, rr * 2.04f), q, UVMode.Fixed);
            }
            if (col != null) col.Box(0, o + Vector3.up * (h * 0.5f), new Vector3(s.x * 1.2f, h, s.x * 1.2f), q, UVMode.Fixed);
        }

        private static void Turbine(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            float h = s.y, blade = s.x * 0.5f;
            m.Cylinder((int)WildMat.Metal, o, 0.55f, 0.28f, h, 10, QMath.Identity, false, UVMode.World);
            Vector3 hub = o + Vector3.up * h;
            m.Box((int)WildMat.Metal, hub + q * new Vector3(0f, 0f, -0.6f), new Vector3(1.1f, 1.1f, 2.6f), q, UVMode.World);
            Vector3 face = hub + q * new Vector3(0f, 0f, 0.8f);
            for (int i = 0; i < 3; i++)
            {
                Quaternion t = q * QMath.AZ(i * 120f + 15f);
                m.Box((int)WildMat.Stone, face + t * new Vector3(0f, blade * 0.5f, 0f), new Vector3(0.5f, blade, 0.12f), t, UVMode.Fixed);
            }
            if (col != null) col.Cylinder(0, o, 0.6f, 0.3f, h, 6, QMath.Identity, false, UVMode.Fixed);
        }

        private static void Hut(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q, WildMat walls)
        {
            float hx = s.x * 0.5f, hz = s.z * 0.5f, h = s.y, t = 0.4f, door = 1.4f;
            Box(m, col, walls, o, q, new Vector3(0f, h * 0.5f, hz - t * 0.5f), new Vector3(s.x, h, t));
            Box(m, col, walls, o, q, new Vector3(-hx + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, s.z));
            Box(m, col, walls, o, q, new Vector3(hx - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, s.z));
            float side = (s.x - door) * 0.5f;
            Box(m, col, walls, o, q, new Vector3(-hx + side * 0.5f, h * 0.5f, -hz + t * 0.5f), new Vector3(side, h, t));
            Box(m, col, walls, o, q, new Vector3(hx - side * 0.5f, h * 0.5f, -hz + t * 0.5f), new Vector3(side, h, t));
            Box(m, null, walls, o, q, new Vector3(0f, h - 0.4f, -hz + t * 0.5f), new Vector3(door, 0.8f, t));
            Box(m, col, walls, o, q, new Vector3(0f, h + 0.1f, 0f), new Vector3(s.x + 0.2f, 0.25f, s.z + 0.2f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = o + q * new Vector3(-hx + 0.6f + i * (s.x - 1.2f) / 3f, h - 0.25f, -hz - 0.5f);
                m.Cylinder((int)WildMat.Wood, a, 0.09f, 0.09f, s.z + 1f, 5, q * QMath.AX(90f), false, UVMode.Fixed);
            }
            m.Box((int)WildMat.Shadow, o + q * new Vector3(0f, h * 0.45f, 0f), new Vector3(s.x - 2f * t - 0.05f, h * 0.9f, s.z - 2f * t - 0.05f), q, UVMode.Fixed);
        }

        private static void Box(MeshBuilder m, MeshBuilder col, WildMat mat, Vector3 o, Quaternion q, Vector3 local, Vector3 size)
        {
            Vector3 c = o + q * local;
            m.Box((int)mat, c, size, q, UVMode.World);
            if (col != null) col.Box(0, c, size, q, UVMode.Fixed);
        }

        private static void Pier(MeshBuilder m, MeshBuilder col, Vector3 o, Vector3 s, Quaternion q)
        {
            int planks = Mathf.Max(3, Mathf.RoundToInt(s.z / 0.45f));
            for (int i = 0; i < planks; i++)
                m.Box((int)WildMat.Wood, o + q * new Vector3(0f, s.y, -s.z * 0.5f + (i + 0.5f) * s.z / planks), new Vector3(s.x, 0.08f, s.z / planks * 0.88f), q, UVMode.World);
            for (int i = 0; i < 4; i++)
                m.Cylinder((int)WildMat.Wood, o + q * new Vector3((i % 2 == 0 ? -1f : 1f) * s.x * 0.45f, -0.4f, (i < 2 ? -0.4f : 0.4f) * s.z), 0.1f, 0.1f, s.y + 0.6f, 5, QMath.Identity, false, UVMode.Fixed);
            if (col != null) col.Box(0, o + Vector3.up * s.y, new Vector3(s.x, 0.12f, s.z), q, UVMode.Fixed);
        }
    }
}
