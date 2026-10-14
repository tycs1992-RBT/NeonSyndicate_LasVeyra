using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Inner World's visible changes: crystal growths that spring up on sidewalks, lots and open ground within
    /// CrystalRadius of the player (pooled, one per fixed world cell, so they never move as the player does), a few great
    /// spires far across the city, and floating islands of rock and crystal hanging in the violet haze.
    /// </summary>
    public sealed partial class InnerWorld
    {
        /// <summary>Crystal growths appear within this distance of the player, in metres.</summary>
        public const float CrystalRadius = 80f;
        private const float SpireRadius = 720f;
        private const int IslandCount = 6;
        private const float GrowSeconds = 0.55f;
        private const float SpireGrowSeconds = 1.6f;

        /// <summary>A spire cell's cached answer from CityLayout.VeilSpireAt (Has is false for an empty cell).</summary>
        private struct SpireCell
        {
            public bool Has;
            public Vector3 Position;
            public float Height;
            public int Variant;
        }

        private sealed class Growth
        {
            public GameObject Go;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Vector3 Position;
            /// <summary>Uniform scale when fully grown (growths: size factor; spires: height in metres).</summary>
            public float Scale;
            public float Grow;
            public bool Leaving;
        }

        private Transform _visualRoot;
        private Transform _skyRig;
        private Mesh[] _clusterMeshes;
        private Mesh[] _spireMeshes;
        private Mesh[] _islandMeshes;
        private readonly List<Transform> _islands = new List<Transform>();
        private readonly List<float> _islandBaseY = new List<float>();
        private readonly List<float> _islandYaw = new List<float>();
        private readonly Dictionary<long, Growth> _growths = new Dictionary<long, Growth>();
        private readonly Dictionary<long, Growth> _spires = new Dictionary<long, Growth>();
        private readonly Stack<Growth> _freeGrowths = new Stack<Growth>();
        private readonly Stack<Growth> _freeSpires = new Stack<Growth>();
        private readonly Dictionary<long, VeilCrystalSpot> _cellSpots = new Dictionary<long, VeilCrystalSpot>();
        private readonly HashSet<long> _emptyCells = new HashSet<long>();
        private readonly Dictionary<long, SpireCell> _spireCells = new Dictionary<long, SpireCell>();
        private readonly List<long> _released = new List<long>();
        private float _scanTimer;
        private Vector3 _anchor;
        private bool _visualsShown;

        private static long CellKey(int cx, int cz)
        {
            return ((long)cx << 32) ^ (uint)cz;
        }

        /// <summary>CityLayout.VeilCrystalAt with a cache (a cell's answer never changes for a given city).</summary>
        private bool CrystalCell(int cx, int cz, out VeilCrystalSpot spot)
        {
            long k = CellKey(cx, cz);
            if (_cellSpots.TryGetValue(k, out spot)) return true;
            if (_emptyCells.Contains(k)) return false;
            CityLayout L = Game.Layout;
            if (L == null) return false;
            if (_cellSpots.Count + _emptyCells.Count > 60000)
            {
                _cellSpots.Clear();
                _emptyCells.Clear();
            }
            if (L.VeilCrystalAt(cx, cz, out spot))
            {
                _cellSpots[k] = spot;
                return true;
            }
            _emptyCells.Add(k);
            return false;
        }

        /// <summary>CityLayout.VeilSpireAt with a cache (the spire grid is coarse, so this stays small).</summary>
        private bool SpireAt(int cx, int cz, out SpireCell spire)
        {
            long k = CellKey(cx, cz);
            if (_spireCells.TryGetValue(k, out spire)) return spire.Has;
            CityLayout L = Game.Layout;
            if (L == null) return false;
            spire.Has = L.VeilSpireAt(cx, cz, out spire.Position, out spire.Height, out spire.Variant);
            _spireCells[k] = spire;
            return spire.Has;
        }

        // ------------------------------------------------------------------ setup

        private void BuildVisuals()
        {
            _visualRoot = new GameObject("Veil Growths").transform;
            _visualRoot.SetParent(transform, false);
            _clusterMeshes = new Mesh[CityLayout.VeilCrystalVariants];
            for (int v = 0; v < _clusterMeshes.Length; v++) _clusterMeshes[v] = BuildClusterMesh(v);
            _spireMeshes = new Mesh[3];
            for (int v = 0; v < _spireMeshes.Length; v++) _spireMeshes[v] = BuildSpireMesh(v);
            _islandMeshes = new Mesh[3];
            for (int v = 0; v < _islandMeshes.Length; v++) _islandMeshes[v] = BuildIslandMesh(v);
            _skyRig = new GameObject("Veil Sky").transform;
            _skyRig.SetParent(transform, false);
            for (int i = 0; i < IslandCount; i++)
            {
                Material crystal = i % 2 == 0 ? Mats.InnerCrystal : Mats.InnerCrystalCrimson;
                GameObject go = Shapes.MeshObject("Floating Island " + i, _islandMeshes[i % _islandMeshes.Length], new[] { Mats.InnerRock, crystal }, _skyRig, false);
                go.GetComponent<MeshRenderer>().receiveShadows = false;
                go.transform.localScale = Vector3.one * (14f + (i * 7 % 17));
                _islands.Add(go.transform);
                _islandBaseY.Add(85f + (i * 29 % 75));
                _islandYaw.Add(i * 53f);
            }
            _skyRig.gameObject.SetActive(false);
        }

        /// <summary>A cluster of three to seven violet crystals leaning out from one root (about 1 m wide, up to 3.4 m tall).</summary>
        private static Mesh BuildClusterMesh(int variant)
        {
            MeshBuilder mb = new MeshBuilder(1);
            int n = 3 + variant % 4 + (variant >= 4 ? 1 : 0);
            for (int i = 0; i < n; i++)
            {
                float a = (variant * 61f + i * 137.5f) * Mathf.Deg2Rad;
                float off = i == 0 ? 0f : 0.25f + MathUtil.Hash01(variant, i) * 0.55f;
                float h = i == 0 ? 2.2f + MathUtil.Hash01(variant, 50) * 1.2f : 0.7f + MathUtil.Hash01(variant, i + 20) * 1.5f;
                float r = Mathf.Lerp(0.16f, 0.36f, h / 3.4f);
                float tilt = i == 0 ? MathUtil.Hash01(variant, 60) * 10f : 16f + MathUtil.Hash01(variant, i + 40) * 22f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Quaternion rot = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, dir)) * Quaternion.Euler(0f, i * 31f, 0f);
                ZoneShapes.Crystal(mb, dir * off + Vector3.down * 0.12f, r, h, rot);
            }
            return mb.ToMesh("Veil Crystal Cluster " + variant);
        }

        /// <summary>A great spire at unit height: one tall shard with a ring of smaller shards around its foot.</summary>
        private static Mesh BuildSpireMesh(int variant)
        {
            MeshBuilder mb = new MeshBuilder(1);
            ZoneShapes.Crystal(mb, Vector3.down * 0.02f, 0.085f + variant * 0.012f, 1f, Quaternion.Euler(2f + variant, variant * 20f, 0f));
            int n = 4 + variant;
            for (int i = 0; i < n; i++)
            {
                float a = (i * 360f / n + variant * 15f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = 0.25f + MathUtil.Hash01(variant * 9 + i, 71) * 0.25f;
                float tilt = 18f + MathUtil.Hash01(variant * 9 + i, 73) * 14f;
                ZoneShapes.Crystal(mb, dir * 0.08f + Vector3.down * 0.01f, 0.035f + h * 0.03f, h, Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, dir)));
            }
            return mb.ToMesh("Veil Spire " + variant);
        }

        /// <summary>A floating island at unit radius: an inverted rock cone with hanging spikes, crystals on top and below.</summary>
        private static Mesh BuildIslandMesh(int variant)
        {
            MeshBuilder mb = new MeshBuilder(2);
            mb.Cylinder(0, new Vector3(0f, -1.4f, 0f), 0.12f, 1f, 1.4f, 9, Quaternion.Euler(0f, variant * 13f, 0f), true, UVMode.Unit);
            mb.Sphere(0, new Vector3(0.1f, 0f, -0.05f), new Vector3(0.82f, 0.2f, 0.76f), 3, 9, Quaternion.Euler(0f, variant * 40f, 0f));
            for (int i = 0; i < 3 + variant; i++)
            {
                float a = (i * 2.4f + variant) % (Mathf.PI * 2f);
                Vector3 c = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.35f + 0.2f * MathUtil.Hash01(variant, i + 5)) + Vector3.down * 0.45f;
                // Rotated upside down: the frustum hangs below the rock.
                mb.Cylinder(0, c, 0.3f, 0f, 0.8f + MathUtil.Hash01(variant, i + 9) * 0.7f, 7, Quaternion.Euler(180f, i * 40f, 0f), false, UVMode.Unit);
            }
            int crystals = 4 + variant * 2;
            for (int i = 0; i < crystals; i++)
            {
                float a = i * 2.39996f + variant;
                float r = Mathf.Sqrt((i + 0.5f) / crystals) * 0.62f;
                Vector3 b = new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = 0.3f + MathUtil.Hash01(variant * 13 + i, 81) * 0.6f;
                ZoneShapes.Crystal(mb, b, 0.05f + h * 0.06f, h, Quaternion.AngleAxis(r * 30f, Vector3.Cross(Vector3.up, dir)), 1);
            }
            for (int i = 0; i < 2 + variant; i++)
            {
                float a = i * 1.9f + variant * 0.7f;
                Vector3 b = new Vector3(Mathf.Cos(a) * 0.3f, -0.6f, Mathf.Sin(a) * 0.3f);
                ZoneShapes.Crystal(mb, b, 0.06f, 0.5f + 0.2f * i, Quaternion.Euler(180f, a * Mathf.Rad2Deg, 0f), 1);
            }
            return mb.ToMesh("Veil Island " + variant);
        }

        private Growth NewGrowth(string name)
        {
            GameObject go = Shapes.MeshObject(name, null, new[] { Mats.InnerCrystal }, _visualRoot, false);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.receiveShadows = false;
            go.SetActive(false);
            return new Growth { Go = go, Filter = go.GetComponent<MeshFilter>(), Renderer = mr };
        }

        // ------------------------------------------------------------------ frame

        private void UpdateVisuals(float dt)
        {
            if (_visualRoot == null) return;
            if (Blend <= 0.001f && !IsInner)
            {
                if (_visualsShown) HideAll();
                return;
            }
            if (!_visualsShown) ShowAll();
            Vector3 focus = FocusOverride ? FocusPoint : Game.PlayerPosition;
            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 0.2f;
                ScanGrowths(focus);
                ScanSpires(focus);
            }
            Animate(_growths, _freeGrowths, focus, dt, CrystalRadius, GrowSeconds);
            Animate(_spires, _freeSpires, focus, dt, SpireRadius, SpireGrowSeconds);
            UpdateSky();
        }

        private void ShowAll()
        {
            _visualsShown = true;
            _scanTimer = 0f;
            float dist = Mathf.Clamp(Settings.ViewDistance * 0.5f, 330f, 520f);
            for (int i = 0; i < _islands.Count; i++)
            {
                float a = (i * 360f / _islands.Count + 23f + (i * 37 % 20)) * Mathf.Deg2Rad;
                float d = dist * (0.88f + 0.1f * (i % 3));
                _islands[i].localPosition = new Vector3(Mathf.Sin(a) * d, _islandBaseY[i], Mathf.Cos(a) * d);
            }
            _skyRig.gameObject.SetActive(true);
        }

        private void HideAll()
        {
            _visualsShown = false;
            foreach (KeyValuePair<long, Growth> kv in _growths)
            {
                kv.Value.Go.SetActive(false);
                _freeGrowths.Push(kv.Value);
            }
            _growths.Clear();
            foreach (KeyValuePair<long, Growth> kv in _spires)
            {
                kv.Value.Go.SetActive(false);
                _freeSpires.Push(kv.Value);
            }
            _spires.Clear();
            if (_skyRig != null) _skyRig.gameObject.SetActive(false);
        }

        private void ScanGrowths(Vector3 focus)
        {
            float far = (CrystalRadius + 8f) * (CrystalRadius + 8f);
            foreach (KeyValuePair<long, Growth> kv in _growths)
                if (!IsInner || MathUtil.FlatSqrDistance(kv.Value.Position, focus) > far) kv.Value.Leaving = true;
            if (!IsInner) return;
            float cell = CityLayout.VeilCellSize;
            int r = Mathf.CeilToInt(CrystalRadius / cell);
            int fx = Mathf.FloorToInt(focus.x / cell), fz = Mathf.FloorToInt(focus.z / cell);
            float r2 = CrystalRadius * CrystalRadius;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    VeilCrystalSpot s;
                    if (!CrystalCell(fx + dx, fz + dz, out s)) continue;
                    if (MathUtil.FlatSqrDistance(s.Position, focus) > r2) continue;
                    long k = CellKey(fx + dx, fz + dz);
                    Growth g;
                    if (_growths.TryGetValue(k, out g))
                    {
                        g.Leaving = false;
                        continue;
                    }
                    g = _freeGrowths.Count > 0 ? _freeGrowths.Pop() : NewGrowth("Veil Crystal");
                    g.Position = s.Position;
                    g.Scale = s.Scale;
                    g.Grow = 0f;
                    g.Leaving = false;
                    g.Filter.sharedMesh = _clusterMeshes[Mathf.Clamp(s.Variant, 0, _clusterMeshes.Length - 1)];
                    g.Renderer.sharedMaterial = s.Crimson ? Mats.InnerCrystalCrimson : Mats.InnerCrystal;
                    g.Go.transform.SetPositionAndRotation(s.Position, Quaternion.Euler(0f, s.Yaw, 0f));
                    g.Go.transform.localScale = Vector3.zero;
                    g.Go.SetActive(true);
                    _growths[k] = g;
                }
            }
        }

        private void ScanSpires(Vector3 focus)
        {
            float far = (SpireRadius + 40f) * (SpireRadius + 40f);
            foreach (KeyValuePair<long, Growth> kv in _spires)
                if (!IsInner || MathUtil.FlatSqrDistance(kv.Value.Position, focus) > far) kv.Value.Leaving = true;
            if (!IsInner || Game.Layout == null) return;
            float cell = CityLayout.VeilSpireCellSize;
            int r = Mathf.CeilToInt(SpireRadius / cell);
            int fx = Mathf.FloorToInt(focus.x / cell), fz = Mathf.FloorToInt(focus.z / cell);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    long k = CellKey(fx + dx, fz + dz);
                    Growth g;
                    if (_spires.TryGetValue(k, out g))
                    {
                        if (MathUtil.FlatSqrDistance(g.Position, focus) <= SpireRadius * SpireRadius) g.Leaving = false;
                        continue;
                    }
                    SpireCell sc;
                    if (!SpireAt(fx + dx, fz + dz, out sc)) continue;
                    // Not right on top of the player: spires rise in the distance.
                    float d2 = MathUtil.FlatSqrDistance(sc.Position, focus);
                    if (d2 > SpireRadius * SpireRadius || d2 < 90f * 90f) continue;
                    g = _freeSpires.Count > 0 ? _freeSpires.Pop() : NewGrowth("Veil Spire");
                    g.Position = sc.Position;
                    g.Scale = sc.Height;
                    g.Grow = 0f;
                    g.Leaving = false;
                    g.Filter.sharedMesh = _spireMeshes[Mathf.Clamp(sc.Variant, 0, _spireMeshes.Length - 1)];
                    g.Renderer.sharedMaterial = (k & 1) == 0 ? Mats.InnerCrystal : Mats.InnerCrystalCrimson;
                    g.Go.transform.SetPositionAndRotation(sc.Position, Quaternion.Euler(0f, (k & 255) * 1.4f, 0f));
                    g.Go.transform.localScale = Vector3.zero;
                    g.Go.SetActive(true);
                    _spires[k] = g;
                }
            }
        }

        /// <summary>Grows new crystals with a springy overshoot, shrinks leaving ones, fades them toward the edge of their ring.</summary>
        private void Animate(Dictionary<long, Growth> set, Stack<Growth> free, Vector3 focus, float dt, float radius, float growSeconds)
        {
            _released.Clear();
            foreach (KeyValuePair<long, Growth> kv in set)
            {
                Growth g = kv.Value;
                bool leaving = g.Leaving || !IsInner;
                g.Grow = Mathf.MoveTowards(g.Grow, leaving ? 0f : 1f, dt / (leaving ? growSeconds * 0.6f : growSeconds));
                if (leaving && g.Grow <= 0f)
                {
                    _released.Add(kv.Key);
                    continue;
                }
                float d = MathUtil.FlatDistance(g.Position, focus);
                float edge = Mathf.Clamp01((radius - d) / (radius * 0.15f));
                float s = EaseOutBack(g.Grow) * g.Scale * edge * Blend;
                g.Go.transform.localScale = new Vector3(s, s, s);
            }
            for (int i = 0; i < _released.Count; i++)
            {
                Growth g = set[_released[i]];
                set.Remove(_released[i]);
                g.Go.SetActive(false);
                free.Push(g);
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>The islands hang far off and barely move with the camera; they rise into place as the Veil opens.</summary>
        private void UpdateSky()
        {
            if (_skyRig == null) return;
            Vector3 c = Game.Cam != null ? Game.Cam.transform.position : Game.PlayerPosition;
            _skyRig.position = new Vector3(c.x * 0.92f + _anchor.x * 0.08f, 0f, c.z * 0.92f + _anchor.z * 0.08f);
            float rise = (1f - Blend) * 160f;
            float t = Time.time;
            for (int i = 0; i < _islands.Count; i++)
            {
                Transform isl = _islands[i];
                Vector3 lp = isl.localPosition;
                lp.y = _islandBaseY[i] - rise + Mathf.Sin(t * 0.21f + i * 1.7f) * 2.5f;
                isl.localPosition = lp;
                isl.localRotation = Quaternion.Euler(0f, _islandYaw[i] + t * (1.2f + i * 0.3f), 0f);
            }
        }
    }
}
