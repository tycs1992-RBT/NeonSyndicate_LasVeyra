using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns a CityLayout into scene geometry. Static geometry is merged per 160 m chunk and per material so the
    /// whole city renders in a few hundred draw calls. Colliders are lightweight primitives on empty objects.
    /// </summary>
    public sealed partial class CityBuilder
    {
        public const float ChunkSize = 160f;
        public const float MarkY = 0.025f;

        private readonly CityLayout L;
        private readonly Transform _root;
        private readonly Transform _meshRoot;
        private readonly Transform _colliderRoot;
        private readonly Dictionary<long, Dictionary<Material, MeshBuilder>> _chunks = new Dictionary<long, Dictionary<Material, MeshBuilder>>();
        private readonly HashSet<Material> _noShadow = new HashSet<Material>();
        private readonly Dictionary<long, Transform> _colliderChunks = new Dictionary<long, Transform>();

        public readonly Dictionary<int, MeshRenderer> SignalRenderers = new Dictionary<int, MeshRenderer>();
        public readonly List<GameObject> SignObjects = new List<GameObject>();
        public Transform Root { get { return _root; } }

        public CityBuilder(CityLayout layout, Transform parent)
        {
            L = layout;
            GameObject r = new GameObject("Las Veyra City");
            r.transform.SetParent(parent, false);
            _root = r.transform;
            _meshRoot = new GameObject("Merged Geometry").transform;
            _meshRoot.SetParent(_root, false);
            _colliderRoot = new GameObject("Colliders").transform;
            _colliderRoot.SetParent(_root, false);
            _noShadow.Add(Mats.Asphalt);
            _noShadow.Add(Mats.Highway);
            _noShadow.Add(Mats.LaneWhite);
            _noShadow.Add(Mats.LaneYellow);
            _noShadow.Add(Mats.Sidewalk);
            _noShadow.Add(Mats.Grass);
            _noShadow.Add(Mats.Sand);
            _noShadow.Add(LiveGroundMaterialsL3.Floor);
            _noShadow.Add(Mats.Dirt);
            _noShadow.Add(Mats.LampHead);
            _noShadow.Add(Mats.Water);
            _noShadow.Add(Mats.Weed);
            _noShadow.Add(Mats.Snow);
            _noShadow.Add(Mats.SnowThin);
            _noShadow.Add(Mats.Ice);
        }

        // ------------------------------------------------------------------ chunk plumbing

        private static long ChunkKey(float x, float z)
        {
            int cx = Mathf.FloorToInt(x / ChunkSize);
            int cz = Mathf.FloorToInt(z / ChunkSize);
            return ((long)cx << 32) ^ (uint)cz;
        }

        public MeshBuilder Get(Vector3 at, Material mat, float tile, [System.Runtime.CompilerServices.CallerMemberName] string visualOwnerWR02 = "")
        {
            MeshBuilder surfaceWR02 = SurfaceBuilderWR02(at, mat, tile, visualOwnerWR02);
            if (surfaceWR02 != null) return surfaceWR02;
            if (_frostGroundL3 && mat == Mats.Sand) mat = LiveGroundMaterialsL3.Floor;
            long key = ChunkKey(at.x, at.z);
            Dictionary<Material, MeshBuilder> d;
            if (!_chunks.TryGetValue(key, out d))
            {
                d = new Dictionary<Material, MeshBuilder>();
                _chunks[key] = d;
            }
            MeshBuilder b;
            if (!d.TryGetValue(mat, out b) || b.VertexCount > 250000)
            {
                if (b != null)
                {
                    // Flush an oversized builder into its own object and start a fresh one.
                    Emit(key, mat, b, _flushCounter++);
                }
                b = new MeshBuilder(1);
                d[mat] = b;
            }
            b.Tile = tile;
            return b;
        }

        private int _flushCounter;

        private Transform ColliderParent(Vector3 at)
        {
            long key = ChunkKey(at.x, at.z);
            Transform t;
            if (!_colliderChunks.TryGetValue(key, out t))
            {
                t = new GameObject("Colliders " + key).transform;
                t.SetParent(_colliderRoot, false);
                _colliderChunks[key] = t;
            }
            return t;
        }

        public BoxCollider AddBoxCollider(Vector3 center, Vector3 size, Quaternion rotation)
        {
            GameObject go = new GameObject("c");
            go.transform.SetParent(ColliderParent(center), false);
            go.transform.position = center;
            go.transform.rotation = rotation;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            go.isStatic = true;
            return bc;
        }

        public CapsuleCollider AddCapsule(Vector3 bottom, float radius, float height)
        {
            GameObject go = new GameObject("c");
            go.transform.SetParent(ColliderParent(bottom), false);
            go.transform.position = bottom + Vector3.up * (height * 0.5f);
            CapsuleCollider cc = go.AddComponent<CapsuleCollider>();
            cc.radius = radius;
            cc.height = Mathf.Max(height, radius * 2f);
            go.isStatic = true;
            return cc;
        }

        private void Emit(long key, Material mat, MeshBuilder b, int index)
        {
            if (b.VertexCount == 0) return;
            Mesh mesh = b.ToMesh("Chunk " + key + " " + mat.name + " " + index);
            GameObject go = Shapes.MeshObject(mat.name + " " + key, mesh, new[] { mat }, _meshRoot, !_noShadow.Contains(mat));
            OwnedMeshes.Track(go,mesh);
            go.isStatic = true;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.receiveShadows = true;
            if (mat == Mats.Water) mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private IEnumerator EmitAll(Action<float, string> progress)
        {
            int total = 0;
            foreach (var kv in _chunks) total += kv.Value.Count;
            int done = 0;
            foreach (var kv in _chunks)
            {
                foreach (var mk in kv.Value)
                {
                    Emit(kv.Key, mk.Key, mk.Value, _flushCounter++);
                    done++;
                    if (done % 40 == 0)
                    {
                        if (progress != null) progress(0.9f + 0.1f * done / Mathf.Max(1, total), "Assembling city meshes");
                        yield return null;
                    }
                }
            }
            _chunks.Clear();
        }

        // ------------------------------------------------------------------ main build

        public IEnumerator Build(Action<float, string> progress)
        {
            ResetCoverage02E();
            ResetCoverage04A();
            if (progress != null) progress(0.02f, "Pouring desert ground");
            BuildGround();
            yield return null;

            if (progress != null) progress(0.06f, "Paving roads");
            for (int i = 0; i < L.Edges.Count; i++)
            {
                BuildRoadEdge(L.Edges[i]);
                if (i % 40 == 39) yield return null;
            }
            for (int i = 0; i < L.Nodes.Count; i++) BuildIntersection(L.Nodes[i]);
            yield return null;

            if (progress != null) progress(0.18f, "Laying sidewalks and lots");
            for (int i = 0; i < L.Blocks.Count; i++)
            {
                BuildBlock(L.Blocks[i]);
                if (i % 12 == 11)
                {
                    if (progress != null) progress(0.18f + 0.12f * i / L.Blocks.Count, "Laying sidewalks and lots");
                    yield return null;
                }
            }

            if (progress != null) progress(0.3f, "Raising buildings");
            int bcount = 0;
            for (int i = 0; i < L.Blocks.Count; i++)
            {
                foreach (Lot lot in L.Blocks[i].Lots)
                {
                    foreach (Building bld in lot.Buildings)
                    {
                        BuildBuilding(bld, lot);
                        bcount++;
                    }
                }
                if (i % 8 == 7)
                {
                    if (progress != null) progress(0.3f + 0.3f * i / L.Blocks.Count, "Raising buildings (" + bcount + ")");
                    yield return null;
                }
            }
            foreach (Lot lot in L.DesertLots)
            {
                if (lot.CustomBuilt)
                {
                    foreach (Building protectedBuilding in lot.Buildings) RecordBuilding04A(protectedBuilding, lot, false, false);
                    continue; // modelled by hand (the Frost Wardens outpost)
                }
                foreach (Building bld in lot.Buildings) BuildBuilding(bld, lot);
            }
            yield return null;

            if (progress != null) progress(0.62f, "Digging the Sunken Row canal");
            BuildCanal();
            yield return null;

            if (progress != null) progress(0.66f, "Placing street furniture");
            yield return BuildProps(progress);
            BuildPowerLines();
            yield return null;

            if (progress != null) progress(0.8f, "Wiring traffic signals");
            BuildSignals();
            yield return null;

            if (progress != null) progress(0.84f, "Lighting the Strip");
            BuildSignsAndSites();
            BuildMountains();
            BuildStuntRamps();
            yield return null;
            yield return BuildFrostline(progress);

            // 02F composes bounded imported landmarks on the approved world; no manual scene setup.
            yield return CrystalCity02FWorld.Build(L, _root);
            BuildSpine04O();
            BuildRealmLandscape04OB();
            BuildGothicHeartH3();
            BuildFracturedWilds04OC();
            SealSurfaceSourcesWR02();
            yield return EmitAll(progress);
            ReportCoverage02E();
            ReportCoverage04A();
            if (progress != null) progress(1f, "Las Veyra ready");
        }

        // ------------------------------------------------------------------ stunt ramps

        /// <summary>Concrete kicker ramps for the stunt jumps (desert and big city lots), with matching colliders.</summary>
        private void BuildStuntRamps()
        {
            foreach (StuntJumpDef j in L.StuntJumps)
            {
                const float w = 6f;
                Quaternion rot = Quaternion.Euler(0f, j.Yaw, 0f);
                Vector3 basePos = j.Position;
                Get(basePos, Mats.Concrete, 2f).Wedge(0, basePos, w, j.Length, j.Height, rot, UVMode.World);
                float pitch = Mathf.Atan2(j.Height, j.Length) * Mathf.Rad2Deg;
                float slope = Mathf.Sqrt(j.Length * j.Length + j.Height * j.Height);
                Quaternion tilt = rot * Quaternion.Euler(-pitch, 0f, 0f);
                // Yellow warning stripe along the lip, lying flush on the slope.
                Vector3 lip = basePos + rot * new Vector3(0f, j.Height, j.Length * 0.5f) + tilt * new Vector3(0f, -0.04f, -0.3f);
                Get(lip, Mats.LaneYellow, 1f).Box(0, lip, new Vector3(w, 0.1f, 0.6f), tilt, UVMode.Unit);
                AddBoxCollider(basePos + Vector3.up * (j.Height * 0.5f) + tilt * new Vector3(0f, -0.25f, 0f), new Vector3(w, 0.5f, slope), tilt);
                AddBoxCollider(basePos + rot * new Vector3(0f, j.Height * 0.5f, j.Length * 0.5f - 0.2f), new Vector3(w, j.Height, 0.4f), rot);
            }
        }

        // ------------------------------------------------------------------ ground

        private void BuildGround()
        {
            float H = CityLayout.MapHalf;
            float cx0 = -900f, cx1 = 900f;
            float cz0 = CityLayout.CanalZ - CityLayout.CanalHalfWidth;
            float cz1 = CityLayout.CanalZ + CityLayout.CanalHalfWidth;
            // Four visual ground slabs around the canal (desert sand, slightly below roads to avoid z-fighting).
            RectF[] parts =
            {
                new RectF(-H, -H, cx0, H),
                new RectF(cx1, -H, H, H),
                new RectF(cx0, -H, cx1, cz0),
                new RectF(cx0, cz1, cx1, H)
            };
            foreach (RectF r in parts)
            {
                // Visual: split into chunk-sized tiles so culling works.
                for (float x = r.X0; x < r.X1; x += ChunkSize)
                {
                    for (float z = r.Z0; z < r.Z1; z += ChunkSize)
                    {
                        float x1 = Mathf.Min(r.X1, x + ChunkSize);
                        float z1 = Mathf.Min(r.Z1, z + ChunkSize);
                        Vector3 c = new Vector3((x + x1) * 0.5f, 0f, (z + z1) * 0.5f);
                        // 04E: a tile under a surveyed ground opening (the Underheart stair or vehicle portal) is built
                        // around it, so the opening stays open; every other tile is untouched.
                        if (WorldApertures.Any && WorldApertures.Overlaps(new RectF(x, z, x1, z1)))
                        {
                            foreach (RectF piece in WorldApertures.Subtract(new RectF(x, z, x1, z1)))
                                Get(piece.Center(0f), LiveGroundMaterialsL3.Floor, 12f).FlatRect(0, piece.X0, piece.Z0, piece.X1, piece.Z1, -0.03f, UVMode.World);
                            continue;
                        }
                        // Tiles the Frostline reaches get sand, frost-dusted and snow pieces along the snow field.
                        if (!BuildFrostGroundL3(c, x, z, x1, z1)) Get(c, LiveGroundMaterialsL3.Floor, 12f).FlatRect(0, x, z, x1, z1, -0.03f, UVMode.World);
                    }
                }
                // 04E: the slab collider is split around any ground opening (one box when there is none).
                foreach (RectF piece in WorldApertures.Subtract(r))
                    AddBoxCollider(new Vector3(piece.CenterX, -1f, piece.CenterZ), new Vector3(piece.Width, 2f, piece.Depth), Quaternion.identity);
            }
            // Map boundary walls.
            float wallH = 120f;
            AddBoxCollider(new Vector3(0f, wallH * 0.5f, H + 5f), new Vector3(2f * H + 20f, wallH, 10f), Quaternion.identity);
            AddBoxCollider(new Vector3(0f, wallH * 0.5f, -H - 5f), new Vector3(2f * H + 20f, wallH, 10f), Quaternion.identity);
            AddBoxCollider(new Vector3(H + 5f, wallH * 0.5f, 0f), new Vector3(10f, wallH, 2f * H + 20f), Quaternion.identity);
            AddBoxCollider(new Vector3(-H - 5f, wallH * 0.5f, 0f), new Vector3(10f, wallH, 2f * H + 20f), Quaternion.identity);
        }

        // ------------------------------------------------------------------ roads

        private void EdgeSpan(RoadEdge e, out Vector3 start, out Vector3 end, out Vector3 dir)
        {
            RoadNode a = L.Nodes[e.A];
            RoadNode b = L.Nodes[e.B];
            dir = MathUtil.Flat(b.Pos - a.Pos).normalized;
            float sa = e.AlongX ? a.HalfX : a.HalfZ;
            float sb = e.AlongX ? b.HalfX : b.HalfZ;
            start = a.Pos + dir * sa;
            end = b.Pos - dir * sb;
        }

        private void BuildRoadEdge(RoadEdge e)
        {
            Vector3 start, end, dir;
            EdgeSpan(e, out start, out end, out dir);
            float len = Vector3.Distance(start, end);
            if (len < 0.5f) return;
            Vector3 right = MathUtil.RightOf(dir);
            float hw = e.Width * 0.5f;
            Material surface = e.Kind == RoadKind.Highway ? Mats.Highway : Mats.Asphalt;

            // Road surface in chunk-sized pieces.
            int pieces = Mathf.Max(1, Mathf.CeilToInt(len / ChunkSize * 2f));
            for (int i = 0; i < pieces; i++)
            {
                Vector3 p0 = Vector3.Lerp(start, end, i / (float)pieces);
                Vector3 p1 = Vector3.Lerp(start, end, (i + 1) / (float)pieces);
                Vector3 centre = (p0 + p1) * 0.5f;
                // A surface-only refinement on the demonstration street. Traffic, markings and road colliders
                // remain the original ones. Existing camera reflections supply the reflected city on High/Ultra.
                // 02C: Extend the original mineral street material through the
                // whole inner city, not just the demo avenue. No road mesh, collider,
                // lane path, speed limit or traffic rule changes.
                bool promenade = Mathf.Abs(centre.x) < CityLayout.CoreHalf &&
                    Mathf.Abs(centre.z) < CityLayout.CoreHalf &&
                    e.Kind != RoadKind.Highway && !e.HasBridge;
                // Extend the same material language to the retained home and clinic starting blocks.
                Vector3[] starterAnchors = DistrictArchitecture.StarterAnchors();
                if (e.Kind != RoadKind.Highway && !e.HasBridge)
                    for (int a = 0; a < starterAnchors.Length; a++)
                        if (MathUtil.FlatSqrDistance(centre, starterAnchors[a]) < 45f * 45f) promenade = true;
                if (promenade) promenade = DistrictArchitecture.Ready();
                Material stripSurface = promenade ? DistrictMaterials.Get("promenade") : surface;
                if (L.Revision04O == CityRevision04O.Crystalline04O && e.Kind == RoadKind.Street)
                    stripSurface = RebirthSurface04O(centre,"paving",Mats.Concrete);
                AddFlatStrip(stripSurface, p0, p1, right, -hw, hw, 0f, 6f);
            }

            // Markings.
            float crossA = CrossBandAt(L.Nodes[e.A]);
            float crossB = CrossBandAt(L.Nodes[e.B]);
            Vector3 mStart = start + dir * crossA;
            Vector3 mEnd = end - dir * crossB;
            float mLen = Vector3.Distance(mStart, mEnd);
            bool mineralLane04O = L.Revision04O == CityRevision04O.Crystalline04O && e.Kind == RoadKind.Street;
            if (mLen > 2f && mineralLane04O)
            {
                SolidLine(Mats.Trim,mStart,mEnd,right,hw-.65f,.12f);
                SolidLine(Mats.Trim,mStart,mEnd,right,-hw+.65f,.12f);
            }
            if (mLen > 2f && !mineralLane04O)
            {
                switch (e.Kind)
                {
                    case RoadKind.Street:
                        DashedLine(Mats.LaneYellow, mStart, mEnd, right, 0f, 0.14f, 3f, 5f);
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, e.LaneWidth, 0.12f);
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, -e.LaneWidth, 0.12f);
                        break;
                    case RoadKind.Avenue:
                        SolidLine(Mats.LaneYellow, mStart, mEnd, right, 0.3f, 0.12f);
                        SolidLine(Mats.LaneYellow, mStart, mEnd, right, -0.3f, 0.12f);
                        DashedLine(Mats.LaneWhite, mStart, mEnd, right, e.Median * 0.5f + e.LaneWidth, 0.12f, 3f, 6f);
                        DashedLine(Mats.LaneWhite, mStart, mEnd, right, -(e.Median * 0.5f + e.LaneWidth), 0.12f, 3f, 6f);
                        break;
                    case RoadKind.Boulevard:
                        BuildMedian(e, mStart, mEnd, right, e.Median * 0.5f - 0.2f, 0.2f, Mats.Grass);
                        for (int k = 1; k < e.LanesPerDir; k++)
                        {
                            float off = e.Median * 0.5f + k * e.LaneWidth;
                            DashedLine(Mats.LaneWhite, mStart, mEnd, right, off, 0.12f, 3f, 6f);
                            DashedLine(Mats.LaneWhite, mStart, mEnd, right, -off, 0.12f, 3f, 6f);
                        }
                        break;
                    case RoadKind.Highway:
                        BuildMedian(e, mStart, mEnd, right, 0.35f, 0.85f, Mats.Concrete);
                        for (int k = 1; k < e.LanesPerDir; k++)
                        {
                            float off = e.Median * 0.5f + k * e.LaneWidth;
                            DashedLine(Mats.LaneWhite, mStart, mEnd, right, off, 0.14f, 4f, 8f);
                            DashedLine(Mats.LaneWhite, mStart, mEnd, right, -off, 0.14f, 4f, 8f);
                        }
                        float edgeOff = e.Median * 0.5f + e.LanesPerDir * e.LaneWidth + 0.2f;
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, edgeOff, 0.16f);
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, -edgeOff, 0.16f);
                        SolidLine(Mats.LaneYellow, mStart, mEnd, right, e.Median * 0.5f + 0.15f, 0.14f);
                        SolidLine(Mats.LaneYellow, mStart, mEnd, right, -(e.Median * 0.5f + 0.15f), 0.14f);
                        break;
                    default:
                        DashedLine(Mats.LaneYellow, mStart, mEnd, right, 0f, 0.14f, 3f, 6f);
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, hw - 1.2f, 0.14f);
                        SolidLine(Mats.LaneWhite, mStart, mEnd, right, -(hw - 1.2f), 0.14f);
                        break;
                }
            }

            // Stop lines and crosswalks at core intersections.
            StopAndCrosswalk(e, L.Nodes[e.A], dir, true);
            StopAndCrosswalk(e, L.Nodes[e.B], -dir, false);

            if (e.HasBridge) BuildBridge(e);
        }

        private float CrossBandAt(RoadNode n)
        {
            bool inCore = Mathf.Abs(n.Pos.x) <= CityLayout.CoreHalf + 1f && Mathf.Abs(n.Pos.z) <= CityLayout.CoreHalf + 1f;
            return inCore && n.Edges.Count >= 2 ? CityLayout.SidewalkWidth + 0.6f : 0.5f;
        }

        /// <summary>dirAway points from the node into the edge.</summary>
        private void StopAndCrosswalk(RoadEdge e, RoadNode n, Vector3 dirAway, bool nodeIsA)
        {
            bool inCore = Mathf.Abs(n.Pos.x) <= CityLayout.CoreHalf + 1f && Mathf.Abs(n.Pos.z) <= CityLayout.CoreHalf + 1f;
            if (!inCore || n.Edges.Count < 2) return;
            float half = e.AlongX ? n.HalfX : n.HalfZ;
            Vector3 right = MathUtil.RightOf(dirAway);
            float hw = e.Width * 0.5f;
            // Crosswalk band from half to half + sidewalk width.
            Vector3 c0 = n.Pos + dirAway * (half + 0.4f);
            Vector3 c1 = n.Pos + dirAway * (half + CityLayout.SidewalkWidth - 0.2f);
            for (float s = -hw + 0.6f; s < hw - 0.4f; s += 1.25f)
            {
                AddFlatStrip(Mats.LaneWhite, c0, c1, right, s, s + 0.6f, 0.02f, 1f);
            }
            // Stop line: incoming traffic toward the node travels -dirAway; its lanes are on the right of -dirAway = left of dirAway.
            Vector3 s0 = n.Pos + dirAway * (half + CityLayout.SidewalkWidth + 0.4f);
            Vector3 s1 = s0 + dirAway * 0.45f;
            float laneSpan = e.Median * 0.5f + e.LanesPerDir * e.LaneWidth;
            AddFlatStrip(Mats.LaneWhite, s0, s1, right, -laneSpan, -e.Median * 0.5f, MarkY, 1f);
        }

        private void BuildMedian(RoadEdge e, Vector3 a, Vector3 b, Vector3 right, float halfWidth, float height, Material top)
        {
            Vector3 dir = (b - a).normalized;
            float len = Vector3.Distance(a, b);
            int pieces = Mathf.Max(1, Mathf.CeilToInt(len / 80f));
            for (int i = 0; i < pieces; i++)
            {
                Vector3 p0 = Vector3.Lerp(a, b, i / (float)pieces);
                Vector3 p1 = Vector3.Lerp(a, b, (i + 1) / (float)pieces);
                Vector3 mid = (p0 + p1) * 0.5f;
                float l = Vector3.Distance(p0, p1);
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                Vector3 size = new Vector3(halfWidth * 2f, height, l);
                Get(mid, Mats.Curb, 1f).Box(0, mid + Vector3.up * (height * 0.5f), size, rot, UVMode.World, 1 | 2 | 16 | 32);
                if (top == Mats.Grass && halfWidth > 0.6f)
                {
                    AddFlatStrip(top, p0, p1, right, -halfWidth + 0.15f, halfWidth - 0.15f, height + 0.005f, 4f);
                    AddFlatStrip(Mats.Curb, p0, p1, right, -halfWidth, -halfWidth + 0.15f, height + 0.005f, 1f);
                    AddFlatStrip(Mats.Curb, p0, p1, right, halfWidth - 0.15f, halfWidth, height + 0.005f, 1f);
                }
                else
                {
                    AddFlatStrip(top, p0, p1, right, -halfWidth, halfWidth, height + 0.005f, 2f);
                }
                if (height > 0.5f) AddBoxCollider(mid + Vector3.up * (height * 0.5f), size, rot);
            }
        }

        private void BuildBridge(RoadEdge e)
        {
            float x = L.Nodes[e.A].Pos.x;
            float hw = e.Width * 0.5f;
            float z0 = CityLayout.CanalZ - CityLayout.CanalBandHalf;
            float z1 = CityLayout.CanalZ + CityLayout.CanalBandHalf;
            float deckHalf = hw + CityLayout.SidewalkWidth;
            Vector3 center = new Vector3(x, -0.45f, CityLayout.CanalZ);
            // Deck body visible from the canal.
            Get(center, Mats.Concrete, 3f).Box(0, center, new Vector3(deckHalf * 2f, 0.9f, z1 - z0), Quaternion.identity, UVMode.World, 1 | 2 | 8);
            AddBoxCollider(new Vector3(x, -0.4f, CityLayout.CanalZ), new Vector3(deckHalf * 2f, 0.8f, z1 - z0), Quaternion.identity);
            // Bridge sidewalks and railings.
            for (int side = -1; side <= 1; side += 2)
            {
                float sx0 = x + side * hw;
                float sx1 = x + side * deckHalf;
                RectF walk = new RectF(Mathf.Min(sx0, sx1), z0, Mathf.Max(sx0, sx1), z1);
                Get(walk.Center(0f), Mats.Sidewalk, 4f).FlatRect(0, walk.X0, walk.Z0, walk.X1, walk.Z1, CityLayout.CurbHeight, UVMode.World);
                Get(walk.Center(0f), Mats.Curb, 1f).Box(0, new Vector3(walk.CenterX, CityLayout.CurbHeight * 0.5f, walk.CenterZ), new Vector3(walk.Width, CityLayout.CurbHeight, walk.Depth), Quaternion.identity, UVMode.World, side < 0 ? 1 : 2);
                AddBoxCollider(new Vector3(walk.CenterX, CityLayout.CurbHeight * 0.5f, walk.CenterZ), new Vector3(walk.Width, CityLayout.CurbHeight, walk.Depth), Quaternion.identity);
                float railX = x + side * (deckHalf - 0.15f);
                Vector3 railC = new Vector3(railX, CityLayout.CurbHeight + 0.55f, CityLayout.CanalZ);
                Get(railC, Mats.Metal, 1f).Box(0, railC + Vector3.up * 0.45f, new Vector3(0.12f, 0.12f, z1 - z0), Quaternion.identity, UVMode.Unit);
                for (float z = z0 + 1f; z < z1; z += 2.5f)
                {
                    Vector3 post = new Vector3(railX, CityLayout.CurbHeight + 0.5f, z);
                    Get(post, Mats.Metal, 1f).Box(0, post, new Vector3(0.1f, 1f, 0.1f), Quaternion.identity, UVMode.Unit);
                }
                AddBoxCollider(railC, new Vector3(0.2f, 1.1f, z1 - z0), Quaternion.identity);
            }
        }

        private void BuildIntersection(RoadNode n)
        {
            Material surface = Mathf.Abs(Mathf.Abs(n.Pos.x) - CityLayout.RingHalf) < 1f || Mathf.Abs(Mathf.Abs(n.Pos.z) - CityLayout.RingHalf) < 1f ? Mats.Highway : Mats.Asphalt;
            if (surface != Mats.Highway && Mathf.Abs(n.Pos.x) < CityLayout.CoreHalf && Mathf.Abs(n.Pos.z) < CityLayout.CoreHalf)
                surface = DistrictMaterials.Get("promenade");
            Get(n.Pos, surface, 6f).FlatRect(0, n.Pos.x - n.HalfX, n.Pos.z - n.HalfZ, n.Pos.x + n.HalfX, n.Pos.z + n.HalfZ, 0f, UVMode.World);
        }

        /// <summary>Flat strip along a segment, between lateral offsets o0 and o1 (relative to 'right').</summary>
        public void AddFlatStrip(Material mat, Vector3 p0, Vector3 p1, Vector3 right, float o0, float o1, float y, float tile, [System.Runtime.CompilerServices.CallerMemberName] string visualOwnerWR02 = "")
        {
            Vector3 a = p0 + right * o0;
            Vector3 b = p1 + right * o0;
            Vector3 c = p1 + right * o1;
            Vector3 d = p0 + right * o1;
            a.y = b.y = c.y = d.y = y;
            MeshBuilder mb = Get((p0 + p1) * 0.5f, mat, tile, visualOwnerWR02);
            // Ensure clockwise winding when viewed from above.
            Vector3 n = Vector3.Cross(b - a, d - a);
            Vector2 ua = new Vector2(a.x / tile, a.z / tile), ub = new Vector2(b.x / tile, b.z / tile), uc = new Vector2(c.x / tile, c.z / tile), ud = new Vector2(d.x / tile, d.z / tile);
            if (n.y > 0f) mb.Quad(0, a, b, c, d, Vector3.up, ua, ub, uc, ud);
            else mb.Quad(0, a, d, c, b, Vector3.up, ua, ud, uc, ub);
        }

        private void SolidLine(Material mat, Vector3 a, Vector3 b, Vector3 right, float offset, float width)
        {
            AddFlatStrip(mat, a, b, right, offset - width * 0.5f, offset + width * 0.5f, MarkY, 1f);
        }

        private void DashedLine(Material mat, Vector3 a, Vector3 b, Vector3 right, float offset, float width, float dash, float gap)
        {
            float len = Vector3.Distance(a, b);
            Vector3 dir = (b - a) / Mathf.Max(0.001f, len);
            for (float s = gap * 0.5f; s + dash < len; s += dash + gap)
            {
                AddFlatStrip(mat, a + dir * s, a + dir * (s + dash), right, offset - width * 0.5f, offset + width * 0.5f, MarkY, 1f);
            }
        }

        // ------------------------------------------------------------------ blocks

        private void BuildBlock(Block b)
        {
            if(BuildGothicBlock04OB(b))return;
            RectF c = b.Curb;
            RectF inner = b.Inner;
            float y = CityLayout.CurbHeight;
            Vector3 center = c.Center(0f);
            // Curb faces and platform collider.
            Get(center, Mats.Curb, 1f).Box(0, new Vector3(c.CenterX, y * 0.5f, c.CenterZ), new Vector3(c.Width, y, c.Depth), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            // 04E: the platform collider is split around any ground opening in the block (one box when there is none).
            foreach (RectF piece in WorldApertures.Subtract(c))
                AddBoxCollider(new Vector3(piece.CenterX, y * 0.5f - 0.25f, piece.CenterZ), new Vector3(piece.Width, y + 0.5f, piece.Depth), Quaternion.identity);
            // Sidewalk ring (split along long sides to keep chunk locality).
            SidewalkRect(new RectF(c.X0, c.Z0, c.X1, inner.Z0));
            SidewalkRect(new RectF(c.X0, inner.Z1, c.X1, c.Z1));
            SidewalkRect(new RectF(c.X0, inner.Z0, inner.X0, inner.Z1));
            SidewalkRect(new RectF(inner.X1, inner.Z0, c.X1, inner.Z1));
            // Lot surfaces.
            foreach (Lot lot in b.Lots)
            {
                Material m;
                switch (lot.Use)
                {
                    case LotUse.Park:
                    case LotUse.Cemetery:
                        m = Mats.Grass; break;
                    case LotUse.Parking:
                        m = Mats.Asphalt; break;
                    case LotUse.Warehouse:
                        m = b.District == District.AshUnderpass ? Mats.Dirt : Mats.Concrete; break;
                    default:
                        m = Mats.Concrete; break;
                }
                if (lot.Buildings.Count > 0 && lot.Buildings[0].RebirthFamily04O == (int)MassingFamily04O.FringeRuin) m = Mats.Dirt;
                TiledRect(m, lot.Area, y + 0.002f, m == Mats.Grass ? 6f : 4f);
                if (lot.Use == LotUse.Parking) ParkingLines(lot.Area, y);
                if (lot.Use == LotUse.Court) CourtLines(lot.Area, y);
                if (lot.Use == LotUse.Park && lot.Area.Area > 4000f) ParkPaths(lot.Area, y);
                if (lot.Use == LotUse.Warehouse && lot.PoiName == "Battery Rail Yard") RailYard(lot.Area, y);
            }
            BlockWeeds(b, y);
            // 02C: Visual-only crystal groves at selected existing park lots.
            // The park remains traversable; this adds no colliders or AI obstacles.
            CrystalGardenDressing.TryDress(b, _root);
        }

        private void SidewalkRect(RectF r)
        {
            if (!r.Valid) return;
            TiledRect(Surface02E(r.Center(0f), "paving", Mats.Sidewalk), r, CityLayout.CurbHeight + 0.003f, 4f);
        }

        private void TiledRect(Material m, RectF r, float y, float tile)
        {
            // 04E: street-level surfaces (lots, sidewalks, paths, pads) leave surveyed ground openings uncovered.
            if (WorldApertures.Any && WorldApertures.Overlaps(r))
            {
                foreach (RectF piece in WorldApertures.Subtract(r)) TiledRectPiece(m, piece, y, tile);
                return;
            }
            TiledRectPiece(m, r, y, tile);
        }

        private void TiledRectPiece(Material m, RectF r, float y, float tile)
        {
            for (float x = r.X0; x < r.X1 - 0.01f; x += ChunkSize)
            {
                for (float z = r.Z0; z < r.Z1 - 0.01f; z += ChunkSize)
                {
                    float x1 = Mathf.Min(r.X1, x + ChunkSize);
                    float z1 = Mathf.Min(r.Z1, z + ChunkSize);
                    Get(new Vector3((x + x1) * 0.5f, 0f, (z + z1) * 0.5f), m, tile).FlatRect(0, x, z, x1, z1, y, UVMode.World);
                }
            }
        }

        private void ParkingLines(RectF a, float y)
        {
            RectF r = a.Inset(3f);
            for (float x = r.X0 + 0.5f; x < r.X1 - 0.5f; x += 3.2f)
            {
                for (float z = r.Z0 + 2f; z < r.Z1 - 6f; z += 12f)
                {
                    Get(new Vector3(x, 0f, z), Mats.LaneWhite, 1f).FlatRect(0, x - 0.06f, z, x + 0.06f, z + 5f, y + 0.02f, UVMode.Unit);
                }
            }
        }

        private void CourtLines(RectF a, float y)
        {
            RectF r = a.Inset(2.5f);
            Material line = Mats.LaneWhite;
            Get(r.Center(0f), Mats.Solid(new Color(0.15f, 0.35f, 0.5f), 0.3f), 2f).FlatRect(0, r.X0, r.Z0, r.X1, r.Z1, y + 0.01f, UVMode.World);
            Get(r.Center(0f), line, 1f).FlatRect(0, r.X0, r.Z0, r.X1, r.Z0 + 0.1f, y + 0.02f, UVMode.Unit);
            Get(r.Center(0f), line, 1f).FlatRect(0, r.X0, r.Z1 - 0.1f, r.X1, r.Z1, y + 0.02f, UVMode.Unit);
            Get(r.Center(0f), line, 1f).FlatRect(0, r.X0, r.Z0, r.X0 + 0.1f, r.Z1, y + 0.02f, UVMode.Unit);
            Get(r.Center(0f), line, 1f).FlatRect(0, r.X1 - 0.1f, r.Z0, r.X1, r.Z1, y + 0.02f, UVMode.Unit);
            Get(r.Center(0f), line, 1f).FlatRect(0, r.X0, r.CenterZ - 0.05f, r.X1, r.CenterZ + 0.05f, y + 0.02f, UVMode.Unit);
        }

        private void ParkPaths(RectF a, float y)
        {
            float w = 3f;
            TiledRect(Mats.Concrete, new RectF(a.X0, a.CenterZ - w * 0.5f, a.X1, a.CenterZ + w * 0.5f), y + 0.01f, 3f);
            TiledRect(Mats.Concrete, new RectF(a.CenterX - w * 0.5f, a.Z0, a.CenterX + w * 0.5f, a.Z1), y + 0.012f, 3f);
        }

        private void RailYard(RectF a, float y)
        {
            float x0 = a.X1 - 34f;
            for (int t = 0; t < 4; t++)
            {
                float x = x0 + t * 7f;
                TiledRect(Mats.Dirt, new RectF(x - 1.6f, a.Z0 + 4f, x + 1.6f, a.Z1 - 4f), y + 0.01f, 3f);
                for (int r = -1; r <= 1; r += 2)
                {
                    Vector3 c = new Vector3(x + r * 0.72f, y + 0.08f, a.CenterZ);
                    Get(c, Mats.Metal, 1f).Box(0, c, new Vector3(0.08f, 0.14f, a.Depth - 8f), Quaternion.identity, UVMode.Unit);
                }
                // Parked freight cars.
                for (float z = a.Z0 + 12f; z < a.Z1 - 20f; z += 18f)
                {
                    if (MathUtil.Hash01(t, Mathf.RoundToInt(z)) < 0.45f) continue;
                    Vector3 car = new Vector3(x, y + 2.1f, z + 7f);
                    Material paint = Mats.CarPaint(Mats.CarPaints[MathUtil.Hash(t, Mathf.RoundToInt(z)) % Mats.CarPaints.Length]);
                    Get(car, paint, 1f).Box(0, car, new Vector3(3f, 3.2f, 14f), Quaternion.identity, UVMode.Unit);
                    Get(car, Mats.DarkMetal, 1f).Box(0, car + Vector3.down * 1.6f, new Vector3(2.4f, 0.6f, 13f), Quaternion.identity, UVMode.Unit);
                    AddBoxCollider(car, new Vector3(3f, 3.6f, 14f), Quaternion.identity);
                }
            }
        }

        // ------------------------------------------------------------------ buildings

        private void BuildBuilding(Building bld, Lot lot)
        {
            RectF fp = bld.Footprint;
            if (!fp.Valid) { RecordBuilding04A(bld, lot, false, false); return; }
            if (Game.Inhabited != null && Game.Inhabited.TryBuildVenue(bld, lot, _root))
            { RecordBuilding04A(bld, lot, true, true); return; }
            if (BuildGothicBuilding04OB(bld)) return;
            if (BuildRebirth04O(bld)) return;
            bool rebirth = PrepareBuilding04A(bld, lot);
            bool themed = PrepareBuilding02E(bld, lot) || rebirth;
            DistrictBuilding art = lot.CustomBuilt ? null : DistrictBuilding.TryBegin(bld, _root);
            float baseY = CityLayout.CurbHeight;
            Material facade = themed ? null : Mats.Facade(bld.Style);
            GRand r = new GRand(bld.Seed);
            float shopH = bld.GroundShops ? 4.4f : 0f;
            int tiers = Mathf.Clamp(bld.Tiers, 1, 3);
            float[] tierTop = new float[tiers];
            float[] tierInset = new float[tiers];
            for (int t = 0; t < tiers; t++)
            {
                float frac = tiers == 1 ? 1f : (t == tiers - 1 ? 1f : (t == 0 ? r.Range(0.45f, 0.65f) : r.Range(0.75f, 0.88f)));
                tierTop[t] = bld.Height * frac;
                tierInset[t] = t == 0 ? 0f : Mathf.Min(fp.Width, fp.Depth) * (0.1f * t + r.Range(0.02f, 0.06f));
            }
            // The optional art pass finishes before original physics is emitted. Any art failure retains
            // the original visual path for this building and cannot stop the rest of the city from booting.
            if (art != null)
            {
                try
                {
                    float artBottom = 0f;
                    for (int t = 0; t < tiers; t++)
                    {
                        RectF tf = fp.Inset(tierInset[t]);
                        if (!tf.Valid || tf.Width < 3f || tf.Depth < 3f) break;
                        art.AddTier(tf, baseY + artBottom, baseY + tierTop[t], t, t == tiers - 1);
                        artBottom = tierTop[t];
                    }
                    art.Complete();
                }
                catch (Exception error)
                {
                    art.Abort();
                    art = null;
                    Log.Exception("Optional district art (original building retained)", error);
                }
            }
            float bottom = 0f;
            for (int t = 0; t < tiers; t++)
            {
                RectF tf = fp.Inset(tierInset[t]);
                if (!tf.Valid || tf.Width < 3f || tf.Depth < 3f) break;
                float y0 = baseY + Mathf.Max(bottom, t == 0 ? shopH : bottom);
                float y1 = baseY + tierTop[t];
                if (art == null && themed)
                {
                    if (rebirth) BuildOrdinaryTier04A(bld, tf, baseY + bottom, y1, t);
                    else BuildOrdinaryTier02E(bld, tf, baseY + bottom, y1, t);
                }
                if (art == null && !themed)
                {
                    if (y1 - y0 > 0.5f)
                    {
                        Vector3 c = new Vector3(tf.CenterX, (y0 + y1) * 0.5f, tf.CenterZ);
                        Get(c, facade, 1f).Box(0, c, new Vector3(tf.Width, y1 - y0, tf.Depth), Quaternion.identity, UVMode.Facade, 1 | 2 | 16 | 32, y0 - baseY);
                    }
                    // Roof of this tier (visible ring around the next tier is fine: draw whole roof).
                    Get(tf.Center(0f), Mats.Roof, 4f).FlatRect(0, tf.X0, tf.Z0, tf.X1, tf.Z1, y1, UVMode.World);
                    // Parapet.
                    float ph = 0.9f, pt = 0.35f;
                    Material trim = Mats.Trim;
                    Get(tf.Center(0f), trim, 1f).Box(0, new Vector3(tf.CenterX, y1 + ph * 0.5f, tf.Z0 + pt * 0.5f), new Vector3(tf.Width, ph, pt), UVMode.Unit, 1 | 2 | 4 | 16 | 32);
                    Get(tf.Center(0f), trim, 1f).Box(0, new Vector3(tf.CenterX, y1 + ph * 0.5f, tf.Z1 - pt * 0.5f), new Vector3(tf.Width, ph, pt), UVMode.Unit, 1 | 2 | 4 | 16 | 32);
                    Get(tf.Center(0f), trim, 1f).Box(0, new Vector3(tf.X0 + pt * 0.5f, y1 + ph * 0.5f, tf.CenterZ), new Vector3(pt, ph, tf.Depth - pt * 2f), UVMode.Unit, 1 | 2 | 4 | 16 | 32);
                    Get(tf.Center(0f), trim, 1f).Box(0, new Vector3(tf.X1 - pt * 0.5f, y1 + ph * 0.5f, tf.CenterZ), new Vector3(pt, ph, tf.Depth - pt * 2f), UVMode.Unit, 1 | 2 | 4 | 16 | 32);
                }
                // Collider for this tier.
                float cy0 = t == 0 ? baseY : baseY + bottom;
                AddBoxCollider(new Vector3(tf.CenterX, (cy0 + y1) * 0.5f, tf.CenterZ), new Vector3(tf.Width, y1 - cy0, tf.Depth), Quaternion.identity);
                bottom = tierTop[t];
            }

            if (art != null)
            {
                _fantasyDetailed02E++;
                RecordBuilding04A(bld, lot, art.Uses04A, true);
                return;
            }
            if (themed)
            {
                // Preserve landing pads and existing roof equipment before adding perimeter ornament.
                BuildRoofProps(bld, fp.Inset(tierInset[tiers - 1]), baseY + tierTop[tiers - 1], r);
                if (rebirth) BuildOrdinaryRoof04A(bld, fp.Inset(tierInset[tiers - 1]), baseY + tierTop[tiers - 1]);
                else BuildOrdinaryRoof02E(bld, fp.Inset(tierInset[tiers - 1]), baseY + tierTop[tiers - 1]);
                RecordBuilding04A(bld, lot, rebirth, false);
                _fantasyLight02E++;
                return;
            }
            if (CrystalCity02ECoverage.CanTheme(bld, lot)) _fantasyFallback02E++;
            RecordBuilding04A(bld, lot, false, false);

            if (bld.GroundShops)
            {
                BuildStorefront(fp, baseY, shopH, r, bld);
            }

            // Corner neon strips for casinos and hotels.
            if (bld.Style == FacadeStyle.Casino)
            {
                Material neon = Mats.Neon(bld.NeonColor);
                float top = baseY + bld.Height;
                Vector3[] corners = { new Vector3(fp.X0, 0f, fp.Z0), new Vector3(fp.X1, 0f, fp.Z0), new Vector3(fp.X1, 0f, fp.Z1), new Vector3(fp.X0, 0f, fp.Z1) };
                foreach (Vector3 cc in corners)
                {
                    Vector3 c = new Vector3(cc.x, (baseY + shopH + top) * 0.5f, cc.z);
                    Get(c, neon, 1f).Box(0, c, new Vector3(0.45f, top - baseY - shopH, 0.45f), UVMode.Unit, 1 | 2 | 16 | 32);
                }
                Vector3 band = new Vector3(fp.CenterX, top - 1.2f, fp.CenterZ);
                Get(band, neon, 1f).Box(0, band, new Vector3(fp.Width + 0.3f, 0.6f, fp.Depth + 0.3f), UVMode.Unit, 1 | 2 | 16 | 32);
            }

            BuildRoofProps(bld, fp.Inset(tierInset[tiers - 1]), baseY + tierTop[tiers - 1], r);
        }

        private void BuildStorefront(RectF fp, float baseY, float shopH, GRand r, Building bld)
        {
            float inset = 0.06f;
            RectF s = fp.Inset(inset);
            Vector3 c = new Vector3(s.CenterX, baseY + shopH * 0.5f, s.CenterZ);
            Get(c, Mats.Storefront, shopH).Box(0, c, new Vector3(s.Width, shopH, s.Depth), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            // Slab above shops.
            Vector3 slab = new Vector3(fp.CenterX, baseY + shopH + 0.15f, fp.CenterZ);
            Get(slab, Mats.Trim, 1f).Box(0, slab, new Vector3(fp.Width + 0.5f, 0.3f, fp.Depth + 0.5f), UVMode.Unit);
            // Awnings on some buildings.
            if (r.Chance(0.4f) && bld.Style != FacadeStyle.OfficeGlass && bld.Style != FacadeStyle.DarkGlass)
            {
                Material awn = Mats.Solid(Mats.ClothColors[r.Range(0, Mats.ClothColors.Length)], 0.2f);
                float ay = baseY + shopH - 0.6f;
                Quaternion tilt = Quaternion.Euler(18f, 0f, 0f);
                for (int side = 0; side < 4; side++)
                {
                    if (r.Chance(0.4f)) continue;
                    Vector3 pos;
                    Quaternion rot;
                    float len;
                    switch (side)
                    {
                        case 0: pos = new Vector3(fp.CenterX, ay, fp.Z0 - 0.75f); rot = Quaternion.Euler(0f, 180f, 0f) * tilt; len = fp.Width * 0.8f; break;
                        case 1: pos = new Vector3(fp.CenterX, ay, fp.Z1 + 0.75f); rot = tilt; len = fp.Width * 0.8f; break;
                        case 2: pos = new Vector3(fp.X0 - 0.75f, ay, fp.CenterZ); rot = Quaternion.Euler(0f, -90f, 0f) * tilt; len = fp.Depth * 0.8f; break;
                        default: pos = new Vector3(fp.X1 + 0.75f, ay, fp.CenterZ); rot = Quaternion.Euler(0f, 90f, 0f) * tilt; len = fp.Depth * 0.8f; break;
                    }
                    Get(pos, awn, 1f).Box(0, pos, new Vector3(len, 0.08f, 1.6f), rot, UVMode.Unit);
                }
            }
        }

        private void BuildRoofProps(Building bld, RectF roof, float y, GRand r)
        {
            if (!roof.Valid || roof.Width < 4f || roof.Depth < 4f) return;
            switch (bld.Roof)
            {
                case RoofType.FlatWithProps:
                    {
                        int ac = r.Range(1, 4);
                        for (int i = 0; i < ac; i++)
                        {
                            Vector3 p = new Vector3(r.Range(roof.X0 + 1.5f, roof.X1 - 1.5f), y + 0.6f, r.Range(roof.Z0 + 1.5f, roof.Z1 - 1.5f));
                            Get(p, Surface02E(roof.Center(0f), "roof", Mats.Metal), 1f).Box(0, p, new Vector3(1.6f, 1.2f, 1.2f), UVMode.Unit);
                        }
                        if (r.Chance(0.4f))
                        {
                            Vector3 p = new Vector3(r.Range(roof.X0 + 2f, roof.X1 - 2f), y, r.Range(roof.Z0 + 2f, roof.Z1 - 2f));
                            Get(p, Surface02E(roof.Center(0f), "roof", Mats.Wood), 1f).Cylinder(0, p + Vector3.up * 2.2f, 1.3f, 1.3f, 2.6f, 10, Quaternion.identity, true, UVMode.World);
                            for (int l = 0; l < 4; l++)
                            {
                                Vector3 leg = p + new Vector3(l < 2 ? -0.9f : 0.9f, 1.1f, l % 2 == 0 ? -0.9f : 0.9f);
                                Get(leg, Surface02E(roof.Center(0f), "trim", Mats.DarkMetal), 1f).Box(0, leg, new Vector3(0.12f, 2.2f, 0.12f), UVMode.Unit);
                            }
                        }
                        if (r.Chance(0.5f))
                        {
                            Vector3 p = new Vector3(r.Range(roof.X0 + 2f, roof.X1 - 2f), y + 1.4f, r.Range(roof.Z0 + 2f, roof.Z1 - 2f));
                            Get(p, Surface02E(roof.Center(0f), "wall", Mats.Concrete), 2f).Box(0, p, new Vector3(3f, 2.8f, 3f), UVMode.World, 1 | 2 | 4 | 16 | 32);
                        }
                    }
                    break;
                case RoofType.Antenna:
                    {
                        Vector3 p = new Vector3(roof.CenterX, y, roof.CenterZ);
                        float h = Mathf.Clamp(bld.Height * 0.12f, 6f, 26f);
                        Get(p, Surface02E(roof.Center(0f), "trim", Mats.DarkMetal), 1f).Cylinder(0, p, 0.25f, 0.08f, h, 6, Quaternion.identity, false);
                        Vector3 tip = p + Vector3.up * h;
                        Get(tip, Mats.Emissive(new Color(1f, 0.1f, 0.05f), 3f), 1f).Box(0, tip, new Vector3(0.35f, 0.35f, 0.35f), UVMode.Unit);
                        Vector3 box = p + new Vector3(0f, 1.5f, 0f);
                        Get(box, Surface02E(roof.Center(0f), "wall", Mats.Concrete), 2f).Box(0, box, new Vector3(Mathf.Min(6f, roof.Width * 0.4f), 3f, Mathf.Min(6f, roof.Depth * 0.4f)), UVMode.World, 1 | 2 | 4 | 16 | 32);
                    }
                    break;
                case RoofType.Helipad:
                    {
                        float s = Mathf.Min(roof.Width, roof.Depth) * 0.7f;
                        Vector3 c = new Vector3(roof.CenterX, y + 0.25f, roof.CenterZ);
                        Get(c, Surface02E(roof.Center(0f), "trim", Mats.DarkMetal), 1f).Box(0, c, new Vector3(s, 0.5f, s), UVMode.Unit);
                        Get(c, Mats.LaneYellow, 1f).FlatRect(0, c.x - s * 0.4f, c.z - 0.4f, c.x + s * 0.4f, c.z + 0.4f, y + 0.52f, UVMode.Unit);
                        Get(c, Mats.LaneYellow, 1f).FlatRect(0, c.x - s * 0.4f, c.z - s * 0.3f, c.x - s * 0.25f, c.z + s * 0.3f, y + 0.52f, UVMode.Unit);
                        Get(c, Mats.LaneYellow, 1f).FlatRect(0, c.x + s * 0.25f, c.z - s * 0.3f, c.x + s * 0.4f, c.z + s * 0.3f, y + 0.52f, UVMode.Unit);
                        AddBoxCollider(c, new Vector3(s, 0.5f, s), Quaternion.identity);
                    }
                    break;
            }
        }
    }
}
