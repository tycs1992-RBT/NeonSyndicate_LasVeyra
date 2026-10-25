using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns a WildsPlan into scene geometry: the map is cut into 160 m chunks, each with one mesh for its landforms and
    /// named places (drawn far, casting shadows), one for the small scatter (grass, shrubs, pebbles, small rocks; culled
    /// at a distance by a LOD group, no shadows) and one collision mesh for every solid piece. Only the material slots a
    /// chunk uses become submeshes, so a chunk costs a handful of draw calls. Everything sits on the World layer, so
    /// cameras, vehicles, people and creatures treat it like the rest of the ground.
    /// </summary>
    public static class WildsBuilder
    {
        public const float ChunkSize = 160f;
        /// <summary>Pieces below this size (metres, the larger of width and depth) count as small scatter.</summary>
        public const float SmallSize = 4f;
        /// <summary>Screen height below which a chunk's scatter, and its landforms, stop drawing.</summary>
        public const float SmallCull = 0.045f, BigCull = 0.006f;

        public sealed class Result
        {
            public int Chunks, Renderers, Vertices, ColliderTriangles;
            public readonly List<WildsChunkL1> LiveChunks = new List<WildsChunkL1>();
        }

        private sealed class Chunk
        {
            public MeshBuilder Big, Small, Collider;
        }

        /// <summary>True for a piece that belongs to the small scatter of a chunk.</summary>
        public static bool IsSmall(WildPiece p)
        {
            return p.Site < 0 && p.Kind != WildPieceKind.Swell && p.Kind != WildPieceKind.Dune && p.Kind != WildPieceKind.GroundPatch && Mathf.Max(p.Size.x, p.Size.z) < SmallSize && p.Size.y < SmallSize * 1.5f;
        }

        public static Result Build(WildsPlan plan, Transform parent)
        {
            Result result = new Result();
            Dictionary<long, List<int>> chunks = new Dictionary<long, List<int>>();
            for (int i = 0; i < plan.Pieces.Count; i++)
            {
                WildPiece p = plan.Pieces[i];
                int cx = Mathf.FloorToInt(p.Position.x / ChunkSize), cz = Mathf.FloorToInt(p.Position.z / ChunkSize);
                long key = ((long)cx << 32) ^ (uint)cz;
                List<int> indices;
                if (!chunks.TryGetValue(key, out indices)) { indices = new List<int>(); chunks.Add(key, indices); }
                indices.Add(i);
            }
            foreach (KeyValuePair<long, List<int>> kv in chunks)
            {
                int cx = (int)(kv.Key >> 32), cz = (int)(kv.Key & 0xffffffff);
                GameObject root = new GameObject("Wilds chunk " + cx + "," + cz);
                root.transform.SetParent(parent, false);
                root.isStatic = true;
                WildsChunkL1 chunk = root.AddComponent<WildsChunkL1>();
                chunk.Init(plan, kv.Value, result);
                result.LiveChunks.Add(chunk);
                result.Chunks++;
            }
            return result;
        }

        /// <summary>Rebuild a single chunk from original pieces, omitting only scoped landmark exclusions.</summary>
        public static void RenderChunk(WildsPlan plan, IList<int> indices, HashSet<int> excluded, Transform root, Result result)
        {
            Chunk c = new Chunk { Big = new MeshBuilder(WildsShapes.MatCount), Small = new MeshBuilder(WildsShapes.MatCount), Collider = new MeshBuilder(1) };
            foreach (int index in indices)
            {
                if (excluded.Contains(index)) continue;
                WildPiece p = plan.Pieces[index];
                try { WildsShapes.Build(p, IsSmall(p) ? c.Small : c.Big, c.Collider); }
                catch (System.Exception e) { Log.Exception("Wilds piece " + p.Kind, e); }
            }
            Cull(Emit(root, "Landforms", c.Big, true, result), BigCull);
            Cull(Emit(root, "Scatter", c.Small, false, result), SmallCull);
            if (c.Collider.VertexCount > 0)
            {
                GameObject col = new GameObject("Collision");
                col.transform.SetParent(root, false);
                col.layer = Layers.World;
                col.isStatic = true;
                col.AddComponent<MeshCollider>().sharedMesh = Pack(c.Collider, "Wilds collision", out int[] _);
                result.ColliderTriangles += c.Collider.Triangles(0).Count / 3;
            }
        }

        private static Renderer Emit(Transform parent, string name, MeshBuilder b, bool shadows, Result result)
        {
            if (b.VertexCount == 0) return null;
            int[] slots;
            Mesh mesh = Pack(b, "Wilds " + name, out slots);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = Layers.World;
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            Material[] mats = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++) mats[i] = WildsMaterials.Get((WildMat)slots[i]);
            r.sharedMaterials = mats;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            result.Renderers++;
            result.Vertices += b.VertexCount;
            return r;
        }

        /// <summary>A mesh holding only the used submeshes; slots lists the WildMat of each, in order.</summary>
        private static Mesh Pack(MeshBuilder b, string name, out int[] slots)
        {
            List<int> used = new List<int>();
            for (int i = 0; i < b.SubmeshCount; i++) if (b.HasGeometry(i)) used.Add(i);
            Mesh mesh = new Mesh();
            mesh.name = name;
            if (b.VertexCount > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(b.Vertices);
            mesh.SetNormals(b.Normals);
            mesh.SetUVs(0, b.UVs);
            mesh.subMeshCount = used.Count;
            List<int> tris = new List<int>();
            for (int k = 0; k < used.Count; k++)
            {
                tris.Clear();
                tris.AddRange(b.Triangles(used[k]));
                mesh.SetTriangles(tris, k, false);
            }
            mesh.RecalculateBounds();
            slots = used.ToArray();
            return mesh;
        }

        private static void Cull(Renderer r, float height)
        {
            if (r == null) return;
            LODGroup g = r.gameObject.AddComponent<LODGroup>();
            g.SetLODs(new[] { new LOD(height, new[] { r }) });
            g.RecalculateBounds();
        }
    }
}
