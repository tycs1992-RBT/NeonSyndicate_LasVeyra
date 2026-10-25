using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Owns generated meshes and scoped exclusions. Publishes a complete replacement before releasing old geometry.</summary>
    public sealed partial class WildsChunkL1 : MonoBehaviour
    {
        private WildsPlan _plan;
        private IList<int> _indices;
        private readonly Dictionary<object, HashSet<int>> _leases = new Dictionary<object, HashSet<int>>();
        private readonly HashSet<int> _excluded = new HashSet<int>();
        private Transform _geometry;

        public void Init(WildsPlan plan, IList<int> indices, WildsBuilder.Result result, bool authored = false)
        {
            _authoredL2 = authored;
            _plan = plan;
            _indices = indices;
            Rebuild(result);
        }

        /// <summary>Multiple structures cannot restore each other's exclusions. A failed rebuild rolls back the lease.</summary>
        public void SetLease(object owner, HashSet<int> indices)
        {
            if (owner == null) return;
            HashSet<int> local = new HashSet<int>();
            if (indices != null) foreach (int i in _indices) if (indices.Contains(i)) local.Add(i);
            HashSet<int> previous;
            bool hadLease = _leases.TryGetValue(owner, out previous);
            if (local.Count == 0 && !hadLease) return;
            if (local.Count == 0) _leases.Remove(owner); else _leases[owner] = local;
            HashSet<int> next = new HashSet<int>();
            foreach (HashSet<int> lease in _leases.Values) next.UnionWith(lease);
            if (_excluded.SetEquals(next)) return;
            HashSet<int> before = new HashSet<int>(_excluded);
            _excluded.Clear();
            _excluded.UnionWith(next);
            try { Rebuild(new WildsBuilder.Result()); }
            catch
            {
                _excluded.Clear();
                _excluded.UnionWith(before);
                if (hadLease) _leases[owner] = previous; else _leases.Remove(owner);
                throw;
            }
        }

        private static void Release(Transform geometry)
        {
            if (geometry == null) return;
            // Disable before deferred Destroy so a rejected probe never hits stale collision in this frame.
            geometry.gameObject.SetActive(false);
            HashSet<Mesh> meshes = new HashSet<Mesh>();
            foreach (MeshFilter filter in geometry.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
            foreach (MeshCollider collider in geometry.GetComponentsInChildren<MeshCollider>(true)) if (collider.sharedMesh != null) meshes.Add(collider.sharedMesh);
            foreach (Mesh mesh in meshes) Dispose(mesh);
            Dispose(geometry.gameObject);
        }

        private static void Dispose(Object item)
        {
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }

        private Transform Stage()
        {
            Transform stage = new GameObject("Generated geometry").transform;
            stage.gameObject.SetActive(false);
            stage.SetParent(transform, false);
            return stage;
        }

        private void Rebuild(WildsBuilder.Result result, bool collision = true)
        {
            Transform stage = Stage();
            WildsBuilder.Result built = new WildsBuilder.Result();
            bool authored = _authoredL2;
            try
            {
                try { WildsBuilder.RenderChunk(_plan, _indices, _excluded, stage, built, authored, collision); }
                catch (System.Exception e)
                {
                    if (!authored) throw;
                    Release(stage);
                    stage = Stage();
                    built = new WildsBuilder.Result();
                    authored = false;
                    WildsBuilder.RenderChunk(_plan, _indices, _excluded, stage, built, false, collision);
                    built.LastArtL2.Reason = "Native batch refused, original fallback: " + e.Message;
                    Log.Warn("LIVE_WILDS " + built.LastArtL2.Reason);
                }
            }
            catch { Release(stage); throw; }
            Transform previous = _geometry;
            // Appearance changes transfer the actual collider object and mesh. No recook, disable or physics gap.
            if (!collision && previous != null)
            {
                Transform solid = previous.Find("Collision");
                if (solid != null) solid.SetParent(stage, false);
            }
            _geometry = stage;
            _authoredL2 = authored;
            MetricsL2 = built.LastArtL2;
            stage.gameObject.SetActive(true);
            Release(previous);
            result.Renderers += built.Renderers;
            result.Vertices += built.Vertices;
            result.ColliderTriangles += built.ColliderTriangles;
            result.LastArtL2 = built.LastArtL2;
        }

        private void OnDestroy() { Release(_geometry); }
    }
}
