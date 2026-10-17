using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The dressing layers of the city: the planned decals, clutter, roof gear and foliage cards, merged per 160 m
    /// chunk and per material like the rest of the city, but into one root per preset tier (Medium, High, Ultra) so a
    /// preset change only toggles roots. A tier that was not built at boot (the player raised the preset later) is
    /// built on demand from the same deterministic plan. Nothing here adds a collider.
    /// </summary>
    public sealed partial class CityBuilder
    {
        private DressingPlanner _dressing;
        private readonly Dictionary<GraphicsPreset, Transform> _dressRoots = new Dictionary<GraphicsPreset, Transform>();
        private readonly HashSet<GraphicsPreset> _dressBuilt = new HashSet<GraphicsPreset>();
        private readonly Dictionary<long, Dictionary<Material, MeshBuilder>> _dressChunks = new Dictionary<long, Dictionary<Material, MeshBuilder>>();
        private int _dressFlush;
        /// <summary>Items emitted per tier (diagnostics for the F2 page and the log).</summary>
        public readonly Dictionary<GraphicsPreset, int> DressingCounts = new Dictionary<GraphicsPreset, int>();

        /// <summary>Ground decals sit this far above the surface they mark (the hit-effect decals use the same).</summary>
        private const float DecalLift = 0.012f;

        /// <summary>The full plan (all tiers), made once per city.</summary>
        public DressingPlanner Dressing
        {
            get
            {
                if (_dressing == null)
                {
                    DressContext ctx = new DressContext();
                    ctx.SkipBuilding = SkipForDressing;
                    InhabitedCitySystem city = Game.Inhabited;
                    if (city != null)
                    {
                        ctx.InsideVenue = p => city.ContainsVenueFootprint(p);
                        foreach (VenueBinding04C v in city.Venues)
                        {
                            if (!v.Available) continue;
                            ctx.VenueDoors.Add(v.Entrance);
                            ctx.VenueDoors.Add(v.Outside);
                        }
                    }
                    _dressing = new DressingPlanner(L, ctx);
                    _dressing.Plan();
                    Log.Info("DRESSING_P4 planned " + _dressing.Items.Count + " items (decals, clutter, roof gear, foliage)");
                }
                return _dressing;
            }
        }

        /// <summary>Buildings whose facade is not the plain generated box: art-district fronts and walk-in venues.</summary>
        private static bool SkipForDressing(Building bld)
        {
            if (Game.Inhabited != null && Game.Inhabited.ReplacesBuilding(bld)) return true;
            try { return DistrictArchitecture.Contains(bld); }
            catch (System.Exception) { return false; }
        }

        /// <summary>True once the tier's geometry exists in the scene.</summary>
        public bool DressingBuilt(GraphicsPreset tier)
        {
            return _dressBuilt.Contains(tier);
        }

        /// <summary>Shows the tiers the preset includes and hides the rest (built tiers only).</summary>
        public void SetDressingVisible(GraphicsPreset preset)
        {
            foreach (KeyValuePair<GraphicsPreset, Transform> kv in _dressRoots)
            {
                if (kv.Value == null) continue;
                bool on = DressingRules.Shows(kv.Key, preset);
                if (kv.Value.gameObject.activeSelf != on) kv.Value.gameObject.SetActive(on);
            }
        }

        /// <summary>Builds the dressing of the presets up to and including the current one (boot path).</summary>
        private IEnumerator BuildDressing(System.Action<float, string> progress)
        {
            GraphicsPreset g = Settings.Graphics;
            if (!DressingRules.Enabled(g)) yield break;
            for (GraphicsPreset tier = GraphicsPreset.Medium; tier <= g; tier++)
            {
                if (progress != null) progress(0.79f, "Scattering street detail (" + tier + ")");
                yield return BuildDressingLayer(tier);
            }
            SetDressingVisible(g);
        }

        /// <summary>Builds one tier's geometry (idempotent). Yields every few thousand items.</summary>
        public IEnumerator BuildDressingLayer(GraphicsPreset tier)
        {
            if (tier == GraphicsPreset.Low || _dressBuilt.Contains(tier)) yield break;
            _dressBuilt.Add(tier);
            Transform root;
            if (!_dressRoots.TryGetValue(tier, out root) || root == null)
            {
                root = new GameObject("Dressing " + tier).transform;
                root.SetParent(_root, false);
                _dressRoots[tier] = root;
            }
            DressingPlanner plan = null;
            try { plan = Dressing; }
            catch (System.Exception e) { Log.Exception("Dressing plan", e); }
            if (plan == null) yield break;
            _dressChunks.Clear();
            int emitted = 0;
            int step = 0;
            List<DressItem> items = plan.Items;
            for (int i = 0; i < items.Count; i++)
            {
                DressItem it = items[i];
                if (it.Tier != tier) continue;
                try
                {
                    if (it.IsDecal) EmitDecal(it);
                    else if (it.IsFoliage) EmitFoliage(it);
                    else EmitClutter(it);
                    emitted++;
                }
                catch (System.Exception e)
                {
                    Log.Exception("Dressing item " + it.Kind, e);
                }
                if (++step % 2500 == 0) yield return null;
            }
            foreach (KeyValuePair<long, Dictionary<Material, MeshBuilder>> kv in _dressChunks)
            {
                foreach (KeyValuePair<Material, MeshBuilder> mk in kv.Value)
                {
                    try { EmitDressingMesh(root, kv.Key, mk.Key, mk.Value); }
                    catch (System.Exception e) { Log.Exception("Dressing mesh " + mk.Key.name, e); }
                }
            }
            _dressChunks.Clear();
            DressingCounts[tier] = emitted;
            Log.Info("DRESSING_P4 built tier " + tier + ": " + emitted + " items");
        }

        /// <summary>Dressing merges per 480 m cell (three city chunks): the pieces are small, so fewer, larger meshes win.</summary>
        private const float DressChunk = ChunkSize * 3f;

        private static long DressChunkKey(float x, float z)
        {
            int cx = Mathf.FloorToInt(x / DressChunk);
            int cz = Mathf.FloorToInt(z / DressChunk);
            return ((long)cx << 32) ^ (uint)cz;
        }

        private MeshBuilder DressGet(Vector3 at, Material mat, float tile)
        {
            long key = DressChunkKey(at.x, at.z);
            Dictionary<Material, MeshBuilder> d;
            if (!_dressChunks.TryGetValue(key, out d))
            {
                d = new Dictionary<Material, MeshBuilder>();
                _dressChunks[key] = d;
            }
            MeshBuilder b;
            if (!d.TryGetValue(mat, out b))
            {
                b = new MeshBuilder(1);
                d[mat] = b;
            }
            b.Tile = tile;
            return b;
        }

        private void EmitDressingMesh(Transform root, long key, Material mat, MeshBuilder b)
        {
            if (b.VertexCount == 0) return;
            Mesh mesh = b.ToMesh("Dressing " + key + " " + mat.name + " " + _dressFlush++);
            bool decal = IsDecalMaterial(mat);
            bool cards = mat == Mats.Weed;
            GameObject go = Shapes.MeshObject("Dressing " + mat.name + " " + key, mesh, new[] { mat }, root, !decal && !cards);
            go.isStatic = true;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.receiveShadows = true;
            mr.lightProbeUsage = LightProbeUsage.Off;
        }

        private static bool IsDecalMaterial(Material m)
        {
            return m == DecalMats.Grime || m == DecalMats.OilStain || m == DecalMats.DrainStain || m == DecalMats.Manhole
                || m == DecalMats.MarkingWear || m == DecalMats.PosterScrap || m == DecalMats.WaterStain || m == Mats.Skid || m == Mats.Scorch;
        }

        // ------------------------------------------------------------------ decals

        /// <summary>How far a ground decal of this kind floats above a road (above the 0.025 lane paint where it must cover it).</summary>
        private static float RoadLift(DressKind k)
        {
            switch (k)
            {
                case DressKind.MarkingWear: return MarkY + 0.01f;
                case DressKind.Skid: return MarkY + 0.006f;
                case DressKind.Manhole: return 0.018f;
                default: return MarkY + 0.004f;
            }
        }

        private void EmitDecal(DressItem it)
        {
            Material mat = DecalMats.For(it.Kind);
            if (mat == null) return;
            MeshBuilder mb = DressGet(it.Pos, mat, 1f);
            // A tiny per-item offset keeps overlapping decals from fighting for the same depth.
            float jitter = (it.Seed % 7) * 0.0012f;
            if (it.IsWall)
            {
                Vector3 n = it.Normal.sqrMagnitude > 0.01f ? it.Normal.normalized : Vector3.forward;
                Vector3 rgt = Vector3.Cross(Vector3.up, n).normalized;
                Vector3 c = it.Pos + n * (0.02f + jitter);
                float hw = it.Size.x * 0.5f, hh = it.Size.y * 0.5f;
                Vector3 bl = c + rgt * hw - Vector3.up * hh;
                Vector3 tl = c + rgt * hw + Vector3.up * hh;
                Vector3 tr = c - rgt * hw + Vector3.up * hh;
                Vector3 br = c - rgt * hw - Vector3.up * hh;
                Vector2 u0 = new Vector2(0f, 0f), u1 = new Vector2(1f, 1f);
                if (it.Kind == DressKind.PosterScrap)
                {
                    // Four scraps share one sheet: pick the quadrant by variant.
                    int q = Mathf.Abs(it.Variant) % 4;
                    u0 = new Vector2((q % 2) * 0.5f + 0.01f, (q / 2) * 0.5f + 0.01f);
                    u1 = u0 + new Vector2(0.48f, 0.48f);
                }
                mb.Quad(0, bl, tl, tr, br, n, new Vector2(u0.x, u0.y), new Vector2(u0.x, u1.y), new Vector2(u1.x, u1.y), new Vector2(u1.x, u0.y));
                return;
            }
            float lift = it.Pos.y < 0.1f ? RoadLift(it.Kind) : DecalLift;
            Vector3 p = it.Pos + Vector3.up * (lift + jitter);
            Vector3 fwd = MathUtil.DirFromYaw(it.Yaw);
            Vector3 right = MathUtil.DirFromYaw(it.Yaw + 90f);
            float hx = it.Size.x * 0.5f, hz = it.Size.y * 0.5f;
            Vector3 a = p - right * hx - fwd * hz;
            Vector3 b = p - right * hx + fwd * hz;
            Vector3 cc = p + right * hx + fwd * hz;
            Vector3 d = p + right * hx - fwd * hz;
            // Flip or mirror the sheet by seed so repeated stains do not read as copies.
            bool flipU = (it.Seed & 1) != 0, flipV = (it.Seed & 2) != 0 && it.Kind != DressKind.DrainStain;
            float ua = flipU ? 1f : 0f, ub = 1f - ua, va = flipV ? 1f : 0f, vb = 1f - va;
            mb.Quad(0, a, b, cc, d, Vector3.up, new Vector2(ua, va), new Vector2(ua, vb), new Vector2(ub, vb), new Vector2(ub, va));
        }

        // ------------------------------------------------------------------ foliage cards

        private void EmitFoliage(DressItem it)
        {
            bool dry = it.Kind == DressKind.ScrubTuft || it.Variant == 1;
            WeedCards(DressGet(it.Pos, Mats.Weed, 1f), it.Pos, it.Seed, it.Size.x, dry);
        }
    }
}
