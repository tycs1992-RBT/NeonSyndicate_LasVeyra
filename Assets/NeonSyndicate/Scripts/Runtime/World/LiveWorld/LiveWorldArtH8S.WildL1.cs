using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Opt-in solid landmarks share the existing live-world placement and lifetime owner.</summary>
    public sealed partial class LiveWorldArtH8S
    {
        private readonly List<object> _landmarkLeases = new List<object>();
        private object _pendingLandmark;
        private readonly Dictionary<string, Vector3> _landmarkApproaches = new Dictionary<string, Vector3>(StringComparer.Ordinal);

        public bool LandmarkApproach(string name, out Vector3 at) { return _landmarkApproaches.TryGetValue(name, out at); }

        private void RestoreLandmarks()
        {
            RestorePendingLandmark();
            if (Game.Wilds != null) foreach (object lease in _landmarkLeases) Game.Wilds.RestoreLandmark(lease);
            _landmarkLeases.Clear();
            _landmarkApproaches.Clear();
        }

        private void RestorePendingLandmark()
        {
            if (_pendingLandmark != null && Game.Wilds != null) Game.Wilds.RestoreLandmark(_pendingLandmark);
            _pendingLandmark = null;
        }

        /// <summary>Keep structural physics during visual comparisons so the player never falls through steps.</summary>
        private static void SetCopyVisibility(GameObject holder, bool hidden)
        {
            if (holder.GetComponentInChildren<Collider>(true) != null)
            {
                holder.SetActive(true);
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = hidden || LiveWildRulesL1.IsProxy(renderer.name);
            }
            else holder.SetActive(!hidden);
        }

        private string AddStructureCollision(GameObject holder, Piece piece)
        {
            if (piece.Spec.Collision == LiveWorldCollisionL1.None) return "none";
            if (holder.GetComponentInParent<Rigidbody>() != null) return "refused: structural art must have no rigidbody ancestor";
            holder.isStatic = true;
            if (piece.Spec.Collision == LiveWorldCollisionL1.Box) { AddBoundsBox(holder, piece); return "box"; }
            List<MeshFilter> all = new List<MeshFilter>(), proxies = new List<MeshFilter>();
            foreach (MeshFilter filter in holder.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                if (LiveWildRulesL1.IsProxy(filter.name)) proxies.Add(filter); else all.Add(filter);
            }
            List<MeshFilter> chosen = proxies.Count > 0 ? proxies : all;
            long triangles = 0;
            bool valid = chosen.Count > 0 && chosen.Count <= LiveWildRulesL1.MaxCollisionParts;
            foreach (MeshFilter filter in chosen)
            {
                Mesh mesh = filter.sharedMesh;
                valid &= mesh.isReadable;
                for (int i = 0; i < mesh.subMeshCount; i++) triangles += mesh.GetIndexCount(i) / 3;
            }
            if (!valid || triangles > LiveWildRulesL1.MaxCollisionTriangles)
            {
                AddBoundsBox(holder, piece);
                Log.Warn("LIVE_WORLD " + piece.Entry.Name + ": collision proxy missing, unreadable or above budget; using bounded box. Export low-poly _Collision meshes and prepare again.");
                return "box fallback (repair _Collision meshes)";
            }
            foreach (MeshFilter filter in chosen)
            {
                filter.gameObject.layer = Layers.World;
                filter.gameObject.isStatic = true;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.convex = false;
                collider.sharedMesh = filter.sharedMesh;
            }
            return "mesh, " + (proxies.Count > 0 ? "authored proxies" : "bounded render fallback") + ", " + triangles + " triangles";
        }

        private static void AddBoundsBox(GameObject holder, Piece piece)
        {
            holder.layer = Layers.World;
            BoxCollider box = holder.AddComponent<BoxCollider>();
            box.center = (piece.Min + piece.Max) * 0.5f;
            box.size = piece.Max - piece.Min;
        }
    }
}
