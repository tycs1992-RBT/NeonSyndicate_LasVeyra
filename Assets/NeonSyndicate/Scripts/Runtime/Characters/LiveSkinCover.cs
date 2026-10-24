using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a material is for the skin cover pass: skin that may be hidden, clothing that covers, or neither.</summary>
    public enum LiveCoverRole { Other, Skin, Cloth }

    /// <summary>
    /// Which skin triangles of a live body lie under its clothing (owner report 2026-10-24: "their backs come through their
    /// shirts"). The live pose puts every bone where the procedural rig has it, so a coat weighted to a few bones and the
    /// body under it weighted to many no longer move together, and the body pokes out. A skin vertex is covered when a short
    /// line along its normal, from a little inside the skin to a few centimetres outside, meets a clothing triangle; a skin
    /// triangle is hidden only when all three of its vertices are covered and none is protected (the head, the neck and the
    /// hands stay, so collars and cuffs never open a gap). Everything is measured in the bind pose, in one space for both
    /// meshes. Pure data in and out, no engine calls; the Editor preparation applies the result.
    /// </summary>
    public static class LiveSkinCover
    {
        /// <summary>How far outside the skin a garment may sit and still cover it (the study coats stand five to eleven centimetres off the back).</summary>
        public const float Reach = 0.14f;
        /// <summary>How far inside the skin a garment may sit (where the body already shows through at rest).</summary>
        public const float Inside = 0.02f;
        /// <summary>The grid cell size used to find nearby clothing triangles.</summary>
        public const float Cell = 0.05f;

        /// <summary>
        /// One flag per skin triangle (skinTriangles.Length / 3): true when the triangle is covered and may be removed.
        /// protectedVertices may be null; a protected vertex (head, neck, hands) keeps every triangle it belongs to.
        /// </summary>
        public static bool[] CoveredTriangles(IList<Vector3> skinVertices, IList<Vector3> skinNormals, IList<int> skinTriangles, IList<bool> protectedVertices,
            IList<Vector3> clothVertices, IList<int> clothTriangles)
        {
            int count = skinTriangles != null ? skinTriangles.Count / 3 : 0;
            bool[] result = new bool[count];
            if (count == 0 || skinVertices == null || skinNormals == null || skinNormals.Count != skinVertices.Count) return result;
            if (clothVertices == null || clothTriangles == null || clothTriangles.Count < 3) return result;
            bool[] covered = CoveredVertices(skinVertices, skinNormals, clothVertices, clothTriangles);
            for (int t = 0; t < count; t++)
            {
                bool all = true;
                for (int k = 0; k < 3 && all; k++)
                {
                    int v = skinTriangles[t * 3 + k];
                    if (v < 0 || v >= covered.Length || !covered[v] || (protectedVertices != null && v < protectedVertices.Count && protectedVertices[v])) all = false;
                }
                result[t] = all;
            }
            return result;
        }

        /// <summary>One flag per skin vertex: true when a clothing triangle crosses the line along its normal from Inside below to Reach above.</summary>
        public static bool[] CoveredVertices(IList<Vector3> skinVertices, IList<Vector3> skinNormals, IList<Vector3> clothVertices, IList<int> clothTriangles)
        {
            bool[] covered = new bool[skinVertices.Count];
            Dictionary<long, List<int>> grid = BuildGrid(clothVertices, clothTriangles);
            for (int v = 0; v < skinVertices.Count; v++)
            {
                Vector3 n = skinNormals[v];
                float length = n.magnitude;
                if (length < 1e-6f || float.IsNaN(length)) continue;
                n /= length;
                Vector3 a = skinVertices[v] - n * Inside, b = skinVertices[v] + n * Reach;
                covered[v] = SegmentHits(a, b, grid, clothVertices, clothTriangles);
            }
            return covered;
        }

        /// <summary>
        /// The cover role of a material key (LiveCastFolderRules.MaterialKey: lowercase letters and digits): skin and body are
        /// skin; hair, brows, lashes, beards, eyes, teeth, tongue, nails, masks, glass and lenses are neither; anything
        /// else (a suit, coat, shirt, trousers, boots) is clothing.
        /// </summary>
        public static LiveCoverRole RoleOf(string key)
        {
            string k = key ?? "";
            if (k.Length == 0) return LiveCoverRole.Other;
            if (Has(k, "hair", "brow", "lash", "groom", "beard", "eye", "iris", "pupil", "sclera", "cornea", "teeth", "tooth", "tongue", "nail", "mask", "glass", "lens", "tear", "mouth"))
                return LiveCoverRole.Other;
            if (Has(k, "skin", "body", "face")) return LiveCoverRole.Skin;
            return LiveCoverRole.Cloth;
        }

        /// <summary>
        /// True for the bones whose skin always stays: the head and the neck (they turn inside the collar) and the hands and
        /// fingers (a hand hanging beside a coat would otherwise count as covered by it).
        /// </summary>
        public static bool ProtectedBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return false;
            if (boneName == "Head" || boneName == "neck_01") return true;
            foreach (string hand in HandBones) if (boneName.StartsWith(hand, System.StringComparison.Ordinal)) return true;
            return false;
        }

        private static readonly string[] HandBones = { "hand_", "thumb_", "index_", "middle_", "ring_", "pinky_" };

        private static bool Has(string key, params string[] words)
        {
            foreach (string w in words) if (key.IndexOf(w, System.StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>The triangle list without the flagged triangles (the kept ones in their original order).</summary>
        public static int[] Keep(IList<int> triangles, IList<bool> remove)
        {
            List<int> kept = new List<int>(triangles.Count);
            for (int t = 0; t * 3 + 2 < triangles.Count; t++)
            {
                if (remove != null && t < remove.Count && remove[t]) continue;
                kept.Add(triangles[t * 3]);
                kept.Add(triangles[t * 3 + 1]);
                kept.Add(triangles[t * 3 + 2]);
            }
            return kept.ToArray();
        }

        private static long Key(int x, int y, int z)
        {
            return ((long)(x & 0x1FFFFF) << 42) | ((long)(y & 0x1FFFFF) << 21) | (long)(z & 0x1FFFFF);
        }

        private static int CellOf(float value)
        {
            return Mathf.FloorToInt(value / Cell);
        }

        /// <summary>Every clothing triangle listed in each grid cell its bounding box touches.</summary>
        private static Dictionary<long, List<int>> BuildGrid(IList<Vector3> vertices, IList<int> triangles)
        {
            Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
            for (int t = 0; t * 3 + 2 < triangles.Count; t++)
            {
                Vector3 a = vertices[triangles[t * 3]], b = vertices[triangles[t * 3 + 1]], c = vertices[triangles[t * 3 + 2]];
                Vector3 min = Vector3.Min(a, Vector3.Min(b, c)), max = Vector3.Max(a, Vector3.Max(b, c));
                for (int x = CellOf(min.x); x <= CellOf(max.x); x++)
                    for (int y = CellOf(min.y); y <= CellOf(max.y); y++)
                        for (int z = CellOf(min.z); z <= CellOf(max.z); z++)
                        {
                            long key = Key(x, y, z);
                            List<int> list;
                            if (!grid.TryGetValue(key, out list)) { list = new List<int>(); grid.Add(key, list); }
                            list.Add(t);
                        }
            }
            return grid;
        }

        private static bool SegmentHits(Vector3 a, Vector3 b, Dictionary<long, List<int>> grid, IList<Vector3> vertices, IList<int> triangles)
        {
            Vector3 min = Vector3.Min(a, b), max = Vector3.Max(a, b);
            for (int x = CellOf(min.x); x <= CellOf(max.x); x++)
                for (int y = CellOf(min.y); y <= CellOf(max.y); y++)
                    for (int z = CellOf(min.z); z <= CellOf(max.z); z++)
                    {
                        List<int> list;
                        if (!grid.TryGetValue(Key(x, y, z), out list)) continue;
                        foreach (int t in list)
                            if (SegmentTriangle(a, b, vertices[triangles[t * 3]], vertices[triangles[t * 3 + 1]], vertices[triangles[t * 3 + 2]])) return true;
                    }
            return false;
        }

        /// <summary>True when the segment from a to b crosses the triangle (either side facing).</summary>
        public static bool SegmentTriangle(Vector3 a, Vector3 b, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 dir = b - a;
            Vector3 e1 = p1 - p0, e2 = p2 - p0;
            Vector3 h = Vector3.Cross(dir, e2);
            float det = Vector3.Dot(e1, h);
            if (Mathf.Abs(det) < 1e-12f) return false;
            float inv = 1f / det;
            Vector3 s = a - p0;
            float u = Vector3.Dot(s, h) * inv;
            if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(s, e1);
            float w = Vector3.Dot(dir, q) * inv;
            if (w < 0f || u + w > 1f) return false;
            float along = Vector3.Dot(e2, q) * inv;
            return along >= 0f && along <= 1f;
        }
    }
}
