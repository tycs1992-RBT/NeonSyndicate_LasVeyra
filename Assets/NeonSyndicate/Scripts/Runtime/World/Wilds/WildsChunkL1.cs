using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Owns generated chunk meshes and scoped exclusions. Rebuilds visuals and colliders together.</summary>
    public sealed partial class WildsChunkL1 : MonoBehaviour
    {
        private WildsPlan _plan;
        private IList<int> _indices;
        private readonly Dictionary<object, HashSet<int>> _leases = new Dictionary<object, HashSet<int>>();
        private readonly HashSet<int> _excluded = new HashSet<int>();
        private readonly List<Mesh> _owned = new List<Mesh>();
        private Transform _geometry;

        public void Init(WildsPlan plan, IList<int> indices, WildsBuilder.Result result, bool authored=false)
        {
            _authoredL2=authored;
            _plan = plan;
            _indices = indices;
            Rebuild(result);
        }

        /// <summary>One owner may clear several pieces. Multiple structures cannot restore each other's exclusions.</summary>
        public void SetLease(object owner, HashSet<int> indices)
        {
            HashSet<int> local = new HashSet<int>();
            if (indices != null) foreach (int i in _indices) if (indices.Contains(i)) local.Add(i);
            if (local.Count == 0 && !_leases.ContainsKey(owner)) return;
            if (local.Count == 0) _leases.Remove(owner); else _leases[owner] = local;
            HashSet<int> next = new HashSet<int>();
            foreach (HashSet<int> lease in _leases.Values) next.UnionWith(lease);
            if (_excluded.SetEquals(next)) return;
            _excluded.Clear();
            _excluded.UnionWith(next);
            Rebuild(new WildsBuilder.Result());
        }

        private void Release()
        {
            // Disable before deferred Destroy so rejected probes/Place again cannot hit stale collision this frame.
            if (_geometry != null) { _geometry.gameObject.SetActive(false); Destroy(_geometry.gameObject); }
            foreach (Mesh mesh in _owned) if (mesh != null) Destroy(mesh);
            _owned.Clear();
        }

        private void Rebuild(WildsBuilder.Result result, bool collision=true)
        {
            Release();
            _geometry = new GameObject("Generated geometry").transform;
            _geometry.SetParent(transform, false);
            try{WildsBuilder.RenderChunk(_plan, _indices, _excluded, _geometry, result,_authoredL2,collision);}
            catch(System.Exception e)
            {
                foreach(MeshFilter f in _geometry.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)Destroy(f.sharedMesh);
                foreach(MeshCollider c in _geometry.GetComponentsInChildren<MeshCollider>())if(c.sharedMesh!=null)Destroy(c.sharedMesh);
                _geometry.gameObject.SetActive(false);Destroy(_geometry.gameObject);_geometry=new GameObject("Procedural fallback geometry").transform;_geometry.SetParent(transform,false);_authoredL2=false;
                WildsBuilder.RenderChunk(_plan,_indices,_excluded,_geometry,result,false,collision);result.LastArtL2.Reason="Native batch refused, original fallback: "+e.Message;
            }
            MetricsL2=result.LastArtL2;
            foreach (MeshFilter filter in _geometry.GetComponentsInChildren<MeshFilter>()) if (filter.sharedMesh != null) _owned.Add(filter.sharedMesh);
            foreach (MeshCollider collider in _geometry.GetComponentsInChildren<MeshCollider>()) if (collider.sharedMesh != null) _owned.Add(collider.sharedMesh);
        }

        private void OnDestroy() { Release(); }
    }
}
