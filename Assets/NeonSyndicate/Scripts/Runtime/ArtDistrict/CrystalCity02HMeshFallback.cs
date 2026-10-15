using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Decode a vetted, binary copy of an existing 02F model when the Unity OBJ
    /// importer produces an empty or non-readable mesh. Nothing is downloaded.
    /// The binary payload stores the same positions, normals and UVs as the
    /// original approved 02F Wavefront model. No art or collider is added here.
    /// </summary>
    public static class CrystalCity02HMeshFallback
    {
        private const string ResourceRoot = "CrystalCity02F/MeshFallback/";
        private const int MaximumVertices = 300000;
        private static readonly Dictionary<string, GameObject> Sources =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Mesh> Meshes =
            new Dictionary<string, Mesh>(StringComparer.Ordinal);

        public static int SourceCount { get { return Sources.Count; } }

        public static MeshFilter[] Load(string name)
        {
            GameObject owner;
            if (Sources.TryGetValue(name, out owner) && owner != null)
                return owner.GetComponentsInChildren<MeshFilter>(true);

            TextAsset bytes = Resources.Load<TextAsset>(ResourceRoot + name);
            if (bytes == null)
                throw new InvalidDataException("02H mesh fallback is missing: " + name + ".bytes");

            Mesh mesh = Decode(bytes.bytes, name);
            GameObject root = new GameObject("02H source mesh / " + name);
            root.hideFlags = HideFlags.HideAndDontSave;
            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            Sources.Add(name, root);
            Meshes.Add(name, mesh);
            return new[] { filter };
        }

        /// <summary>Deterministic decoder, also exercised by the EditMode tests.</summary>
        public static Mesh Decode(byte[] data, string name)
        {
            if (data == null || data.Length < 8)
                throw new InvalidDataException("02H mesh is missing or truncated: " + name);
            if (data[0] != (byte)'C' || data[1] != (byte)'M' ||
                data[2] != (byte)'H' || data[3] != (byte)'1')
                throw new InvalidDataException("02H mesh has an unknown format: " + name);

            int count = BitConverter.ToInt32(data, 4);
            if (count < 3 || count > MaximumVertices || count % 3 != 0 ||
                data.LongLength != 8L + count * 32L)
                throw new InvalidDataException("02H mesh has invalid vertex data: " + name);

            List<Vector3> positions = new List<Vector3>(count);
            List<Vector3> normals = new List<Vector3>(count);
            List<Vector2> uvs = new List<Vector2>(count);
            int[] triangles = new int[count];
            using (MemoryStream stream = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position = 8L;
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    Vector3 n = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    Vector2 uv = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    if (!Valid(p.x) || !Valid(p.y) || !Valid(p.z) ||
                        !Valid(n.x) || !Valid(n.y) || !Valid(n.z) ||
                        !Valid(uv.x) || !Valid(uv.y) || n.sqrMagnitude < 0.2f)
                        throw new InvalidDataException("02H mesh has nonfinite or invalid data: " + name);
                    positions.Add(p);
                    normals.Add(n.normalized);
                    uvs.Add(uv);
                    triangles[i] = i;
                }
            }

            Mesh mesh = new Mesh { name = "Crystal City 02H fallback / " + name };
            mesh.indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, false);
            mesh.RecalculateBounds();
            // Keep CPU data readable so the approved 02F material batching can combine it.
            return mesh;
        }

        private static bool Valid(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static void ResetStatics()
        {
            foreach (GameObject root in Sources.Values) Release(root);
            foreach (Mesh mesh in Meshes.Values) Release(mesh);
            Sources.Clear();
            Meshes.Clear();
        }

        private static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
