#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, the crowd merge. A study arrives as about thirty skinned pieces (six coat fasteners, each a renderer of
    /// its own); twenty people would cost six hundred skinned renderers. Pieces that share the same material slots are
    /// merged into one skinned mesh per group, armour kept apart from plain pieces so plain roles can hide it, which leaves
    /// about seven renderers per person. Vertices move into the Art space, every piece's weights are remapped onto one bone
    /// table (every transform below the skeleton root, so all merged meshes share it) with the complete influences kept and
    /// sorted as Unity requires, and each bone's bind pose follows the first piece that references it (a second piece must
    /// agree). The merged meshes are saved beside the prefab. Any doubt throws before a source piece is touched, and the
    /// caller then keeps the separate pieces.
    /// </summary>
    public static partial class LiveCastSetup
    {
        private const int MaxInfluencesPerVertex = 8;

        private static List<SkinnedMeshRenderer> MergeCrowdSkins(Transform art, Transform skeleton, List<SkinnedMeshRenderer> skins, bool[] armour, CrowdStudy study, out bool[] armourOut)
        {
            Transform[] union = skeleton.GetComponentsInChildren<Transform>(true);
            Dictionary<Transform, int> index = new Dictionary<Transform, int>();
            for (int i = 0; i < union.Length; i++) index[union[i]] = i;
            List<string> keys = new List<string>();
            List<List<int>> groups = new List<List<int>>();
            for (int s = 0; s < skins.Count; s++)
            {
                Material[] slots = skins[s].sharedMaterials;
                string key = "";
                for (int i = 0; i < slots.Length; i++) key += (slots[i] != null ? slots[i].name : "none") + "|";
                key += armour[s] ? "armour" : "plain";
                int at = keys.IndexOf(key);
                if (at < 0)
                {
                    keys.Add(key);
                    groups.Add(new List<int>());
                    at = keys.Count - 1;
                }
                groups[at].Add(s);
            }
            List<GameObject> created = new List<GameObject>();
            List<string> assets = new List<string>();
            List<SkinnedMeshRenderer> result = new List<SkinnedMeshRenderer>();
            List<bool> flags = new List<bool>();
            try
            {
                BoneWeight1[] scratch = new BoneWeight1[MaxInfluencesPerVertex];
                for (int g = 0; g < groups.Count; g++)
                {
                    List<int> members = groups[g];
                    bool isArmour = armour[members[0]];
                    Material[] materials = skins[members[0]].sharedMaterials;
                    string first = materials.Length > 0 && materials[0] != null ? materials[0].name : "piece";
                    GameObject go = new GameObject("Merged " + first + (isArmour ? " armour" : ""));
                    go.transform.SetParent(art, false);
                    created.Add(go);
                    Matrix4x4 toMerged = go.transform.worldToLocalMatrix;
                    List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
                    List<Vector4> tangents = new List<Vector4>();
                    List<Vector2> uvs = new List<Vector2>();
                    List<byte> perVertex = new List<byte>();
                    List<BoneWeight1> weights = new List<BoneWeight1>();
                    List<int>[] triangles = new List<int>[materials.Length];
                    for (int sub = 0; sub < triangles.Length; sub++) triangles[sub] = new List<int>();
                    Matrix4x4[] bind = new Matrix4x4[union.Length];
                    bool[] bound = new bool[union.Length];
                    bool allTangents = true;
                    int offset = 0;
                    foreach (int s in members)
                    {
                        SkinnedMeshRenderer skin = skins[s];
                        Mesh mesh = skin.sharedMesh;
                        if (mesh == null || !mesh.isReadable) throw new InvalidOperationException(skin.name + " has no readable mesh.");
                        if (mesh.subMeshCount != materials.Length) throw new InvalidOperationException(skin.name + " has a different number of material slots than its group.");
                        Matrix4x4 m = toMerged * skin.transform.localToWorldMatrix;
                        if (m.determinant <= 0f) throw new InvalidOperationException(skin.name + " is mirrored.");
                        Matrix4x4 inverse = m.inverse;
                        Vector3[] v = mesh.vertices;
                        Vector3[] n = mesh.normals;
                        Vector4[] t = mesh.tangents;
                        Vector2[] uv = mesh.uv;
                        if (n.Length != v.Length) throw new InvalidOperationException(skin.name + " has no normals.");
                        if (uv.Length != v.Length) throw new InvalidOperationException(skin.name + " has no texture coordinates.");
                        if (t.Length != v.Length) allTangents = false;
                        for (int i = 0; i < v.Length; i++)
                        {
                            vertices.Add(m.MultiplyPoint3x4(v[i]));
                            normals.Add(m.MultiplyVector(n[i]).normalized);
                            uvs.Add(uv[i]);
                            if (t.Length == v.Length)
                            {
                                Vector3 dir = m.MultiplyVector(new Vector3(t[i].x, t[i].y, t[i].z)).normalized;
                                tangents.Add(new Vector4(dir.x, dir.y, dir.z, t[i].w < 0f ? -1f : 1f));
                            }
                        }
                        Transform[] sourceBones = skin.bones;
                        Matrix4x4[] sourceBind = mesh.bindposes;
                        if (sourceBind.Length != sourceBones.Length) throw new InvalidOperationException(skin.name + " bind poses do not match its bones.");
                        int[] remap = new int[sourceBones.Length];
                        for (int b = 0; b < sourceBones.Length; b++)
                            if (sourceBones[b] == null || !index.TryGetValue(sourceBones[b], out remap[b]))
                                throw new InvalidOperationException(skin.name + " is weighted to a bone outside the skeleton.");
                        bool[] referenced = new bool[sourceBones.Length];
                        // Unity owns these read-only views; never dispose them.
                        NativeArray<byte> counts = mesh.GetBonesPerVertex();
                        NativeArray<BoneWeight1> all = mesh.GetAllBoneWeights();
                        if (counts.Length != v.Length) throw new InvalidOperationException(skin.name + " has no complete skin weights.");
                        int cursor = 0;
                        for (int i = 0; i < v.Length; i++)
                        {
                            int c = counts[i];
                            if (c < 1 || c > MaxInfluencesPerVertex || cursor + c > all.Length) throw new InvalidOperationException(skin.name + " has a vertex with an invalid influence count.");
                            for (int j = 0; j < c; j++)
                            {
                                BoneWeight1 w = all[cursor + j];
                                if (w.boneIndex < 0 || w.boneIndex >= remap.Length) throw new InvalidOperationException(skin.name + " references a bone outside its table.");
                                referenced[w.boneIndex] = true;
                                w.boneIndex = remap[w.boneIndex];
                                scratch[j] = w;
                            }
                            // Unity wants each vertex's influences from the heaviest down.
                            for (int j = 1; j < c; j++)
                            {
                                BoneWeight1 w = scratch[j];
                                int k = j - 1;
                                while (k >= 0 && scratch[k].weight < w.weight)
                                {
                                    scratch[k + 1] = scratch[k];
                                    k--;
                                }
                                scratch[k + 1] = w;
                            }
                            for (int j = 0; j < c; j++) weights.Add(scratch[j]);
                            perVertex.Add((byte)c);
                            cursor += c;
                        }
                        if (cursor != all.Length) throw new InvalidOperationException(skin.name + " has unowned trailing influences.");
                        for (int b = 0; b < sourceBones.Length; b++)
                        {
                            if (!referenced[b]) continue;
                            // The merged vertex is m times the source vertex, so the bind pose absorbs the inverse.
                            Matrix4x4 pose = sourceBind[b] * inverse;
                            int u = remap[b];
                            if (!bound[u])
                            {
                                bind[u] = pose;
                                bound[u] = true;
                            }
                            else if (!SameMatrix(bind[u], pose)) throw new InvalidOperationException(skin.name + " was bound in a different pose than another piece at " + union[u].name + ".");
                        }
                        for (int sub = 0; sub < materials.Length; sub++)
                        {
                            int[] tri = mesh.GetTriangles(sub);
                            List<int> target = triangles[sub];
                            for (int i = 0; i < tri.Length; i++) target.Add(tri[i] + offset);
                        }
                        offset += v.Length;
                    }
                    for (int u = 0; u < union.Length; u++)
                        if (!bound[u]) bind[u] = union[u].worldToLocalMatrix * go.transform.localToWorldMatrix;
                    Mesh merged = new Mesh();
                    merged.name = study.Role + " " + go.name;
                    if (vertices.Count > 65535) merged.indexFormat = IndexFormat.UInt32;
                    merged.SetVertices(vertices);
                    merged.SetNormals(normals);
                    merged.SetUVs(0, uvs);
                    if (allTangents && tangents.Count == vertices.Count) merged.SetTangents(tangents);
                    merged.subMeshCount = materials.Length;
                    for (int sub = 0; sub < materials.Length; sub++) merged.SetTriangles(triangles[sub], sub, true);
                    NativeArray<byte> perVertexArray = new NativeArray<byte>(perVertex.ToArray(), Allocator.Temp);
                    NativeArray<BoneWeight1> weightArray = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp);
                    try { merged.SetBoneWeights(perVertexArray, weightArray); }
                    finally
                    {
                        perVertexArray.Dispose();
                        weightArray.Dispose();
                    }
                    merged.bindposes = bind;
                    if (!(allTangents && tangents.Count == vertices.Count)) merged.RecalculateTangents();
                    merged.RecalculateBounds();
                    // The complete-weight check the live contract runs: a merged mesh must pass it before it replaces anything.
                    bool[] check = SkinWeightsH5.Referenced(merged, union.Length);
                    if (check.Length != union.Length || merged.vertexCount != offset) throw new InvalidOperationException("the merged mesh " + go.name + " did not keep every vertex and weight.");
                    string asset = study.Prepared + "/" + study.Role + "_" + LiveCastFolderRules.SafeName(go.name) + ".asset";
                    AssetDatabase.CreateAsset(merged, asset);
                    assets.Add(asset);
                    SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = merged;
                    renderer.bones = union;
                    renderer.rootBone = skeleton;
                    renderer.sharedMaterials = materials;
                    result.Add(renderer);
                    flags.Add(isArmour);
                }
            }
            catch (Exception)
            {
                foreach (GameObject go in created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                foreach (string asset in assets) AssetDatabase.DeleteAsset(asset);
                throw;
            }
            // Every group merged: only now do the source pieces go.
            foreach (SkinnedMeshRenderer skin in skins)
            {
                GameObject owner = skin.gameObject;
                UnityEngine.Object.DestroyImmediate(skin);
                if (owner.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(owner);
            }
            armourOut = flags.ToArray();
            return result;
        }

        private static bool SameMatrix(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
                if (Mathf.Abs(a[i] - b[i]) > 0.001f) return false;
            return true;
        }
    }
}
#endif
