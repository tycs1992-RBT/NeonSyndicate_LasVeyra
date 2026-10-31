#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, skin under clothing (owner report 2026-10-24: "their backs come through their shirts"). Before a
    /// crowd body or a drop-in is saved, the skin triangles that lie under its own garments in the bind pose
    /// (LiveSkinCover) are removed from a copy of the skin mesh, saved beside the prefab, so a body weighted to many bones
    /// cannot show through a coat weighted to a few. Skin on the head, the neck and the hands is always kept, so a collar or
    /// a cuff never opens a gap. Armour that hides on plain roles never counts as cover. The delivered FBX files are not touched.
    /// </summary>
    public static partial class LiveCastSetup
    {
        /// <summary>What a skinned piece is for the cover pass.</summary>
        internal enum CoverPart { Other, Skin, Cloth }

        /// <summary>
        /// Removes the covered skin triangles of one level of a body. part says what each piece is; folder and prefix name the
        /// saved mesh copies. Returns how many triangles were removed (0 leaves every mesh as it was).
        /// </summary>
        internal static int CoverSkin(IList<SkinnedMeshRenderer> skins, Func<SkinnedMeshRenderer, CoverPart> part, string folder, string prefix)
        {
            List<Vector3> clothVertices = new List<Vector3>();
            List<int> clothTriangles = new List<int>();
            List<SkinnedMeshRenderer> skinPieces = new List<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skin in skins)
            {
                if (skin == null || skin.sharedMesh == null || !skin.sharedMesh.isReadable) continue;
                CoverPart kind = part(skin);
                if (kind == CoverPart.Skin) skinPieces.Add(skin);
                else if (kind == CoverPart.Cloth) AddWorld(skin, clothVertices, clothTriangles);
            }
            if (skinPieces.Count == 0 || clothTriangles.Count == 0) return 0;
            int removed = 0;
            foreach (SkinnedMeshRenderer skin in skinPieces)
            {
                Mesh mesh = skin.sharedMesh;
                Matrix4x4 world = skin.transform.localToWorldMatrix;
                Vector3[] vertices = mesh.vertices, normals = mesh.normals;
                if (normals.Length != vertices.Length) continue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = world.MultiplyPoint3x4(vertices[i]);
                    normals[i] = world.MultiplyVector(normals[i]);
                }
                bool[] keep = HeadAndNeck(skin, vertices.Length);
                int[][] kept = new int[mesh.subMeshCount][];
                int cut = 0;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    int[] triangles = mesh.GetTriangles(sub);
                    bool[] covered = LiveSkinCover.CoveredTriangles(vertices, normals, triangles, keep, clothVertices, clothTriangles);
                    foreach (bool c in covered) if (c) cut++;
                    kept[sub] = LiveSkinCover.Keep(triangles, covered);
                }
                if (cut == 0) continue;
                Mesh copy = UnityEngine.Object.Instantiate(mesh);
                copy.name = mesh.name + " covered";
                for (int sub = 0; sub < kept.Length; sub++) copy.SetTriangles(kept[sub], sub, true);
                EnsureFolder(folder);
                AssetDatabase.CreateAsset(copy, folder + "/" + prefix + "_" + LiveCastFolderRules.SafeName(skin.name) + "_Covered.asset");
                skin.sharedMesh = copy;
                removed += cut;
            }
            return removed;
        }

        /// <summary>The cover role of a drop-in piece from its imported material names (any skin slot makes it skin).</summary>
        internal static CoverPart DropInCoverPart(SkinnedMeshRenderer skin, bool crowd)
        {
            CoverPart result = CoverPart.Other;
            foreach (Material m in skin.sharedMaterials)
            {
                LiveCoverRole role = LiveSkinCover.RoleOf(LiveCastFolderRules.MaterialKey(m != null ? m.name : ""));
                if (role == LiveCoverRole.Skin) return CoverPart.Skin;
                if (role == LiveCoverRole.Cloth) result = CoverPart.Cloth;
            }
            // A crowd drop-in hides armour on plain roles, so armour never covers its skin.
            if (result == CoverPart.Cloth && crowd && LiveCrowdRules.ArmourPart(skin.name, LiveCrowdSurface.Unknown)) return CoverPart.Other;
            return result;
        }

        /// <summary>Every vertex whose heaviest bone is the head, the neck or a hand (those triangles always stay).</summary>
        private static bool[] HeadAndNeck(SkinnedMeshRenderer skin, int count)
        {
            bool[] keep = new bool[count];
            Transform[] bones = skin.bones;
            NativeArray<byte> perVertex = skin.sharedMesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = skin.sharedMesh.GetAllBoneWeights();
            if (perVertex.Length != count) return keep;
            int cursor = 0;
            for (int v = 0; v < count; v++)
            {
                int best = -1;
                float heaviest = -1f;
                for (int j = 0; j < perVertex[v] && cursor + j < weights.Length; j++)
                    if (weights[cursor + j].weight > heaviest) { heaviest = weights[cursor + j].weight; best = weights[cursor + j].boneIndex; }
                cursor += perVertex[v];
                if (best >= 0 && best < bones.Length && bones[best] != null) keep[v] = LiveSkinCover.ProtectedBone(bones[best].name);
            }
            return keep;
        }

        private static void AddWorld(SkinnedMeshRenderer skin, List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = skin.sharedMesh;
            Matrix4x4 world = skin.transform.localToWorldMatrix;
            int start = vertices.Count;
            foreach (Vector3 v in mesh.vertices) vertices.Add(world.MultiplyPoint3x4(v));
            foreach (int index in mesh.triangles) triangles.Add(start + index);
        }
    }
}
#endif
