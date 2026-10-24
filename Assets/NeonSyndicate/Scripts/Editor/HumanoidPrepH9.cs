#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Shared Editor preparation steps for imported humanoids on the H9 skeleton, used by the Dante living cast review
    /// (LivingCastH8S) and the live character generator (LiveCastSetup): source component inspection before cloning,
    /// complete-weight import settings, one shared skeleton across the LODs (lower LOD skins rebound to the LOD0 bones by
    /// name with matching bind frames), orientation without mirroring and unambiguous node lookups by name.
    /// </summary>
    public static class HumanoidPrepH9
    {
        /// <summary>Import limit: no pruning at import. The complete-weight check (SkinWeightsH5) then refuses over eight.</summary>
        public const int NoPruning = 255;

        /// <summary>The only node with this name below root; throws when it is missing or ambiguous.</summary>
        public static Transform Named(GameObject root, string name)
        {
            Transform found = FindNamed(root, name);
            if (found == null) throw new InvalidDataException("Missing actual source node: " + name);
            return found;
        }

        /// <summary>The only node with this name below root, or null when there is none; throws when it is ambiguous.</summary>
        public static Transform FindNamed(GameObject root, string name)
        {
            Transform found = null;
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == name)
                {
                    if (found != null) throw new InvalidDataException("Ambiguous model node: " + name);
                    found = node;
                }
            return found;
        }

        /// <summary>Every transform below root by name. Names must be unique, so a bone can be found without guessing.</summary>
        public static Dictionary<string, Transform> UniqueNodes(Transform root)
        {
            Dictionary<string, Transform> nodes = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node == root) continue;
                if (nodes.ContainsKey(node.name)) throw new InvalidDataException("Ambiguous LOD0 node name: " + node.name);
                nodes.Add(node.name, node);
            }
            return nodes;
        }

        /// <summary>Refuses a source with anything but visual components or with an Animator controller, before cloning.</summary>
        public static void CheckSourceComponents(GameObject source, string path)
        {
            foreach (Component component in source.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !ImportedArt04OH.VisualComponent(component.GetType()))
                    throw new InvalidDataException("Unexpected component " + (component != null ? component.GetType().Name + " on " + component.name : "(missing script)") + " in " + path + " before cloning.");
                Animator animator = component as Animator;
                if (animator != null && animator.runtimeAnimatorController != null) throw new InvalidDataException("An Animator controller cannot enter the visual copy: " + path);
            }
        }

        /// <summary>
        /// Rebinds one lower LOD's skins to the LOD0 skeleton by bone name, refusing any missing bone or bind mismatch, and
        /// moves them under art. expectedSkins below zero accepts any count. Returns the moved skins.
        /// </summary>
        public static List<SkinnedMeshRenderer> AttachLod(Transform art, Transform primaryRoot, Dictionary<string, Transform> nodes, GameObject source, int level, int expectedSkins)
        {
            List<SkinnedMeshRenderer> moved = new List<SkinnedMeshRenderer>();
            GameObject extra = UnityEngine.Object.Instantiate(source, art, false);
            try
            {
                SkinnedMeshRenderer[] skins = extra.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (expectedSkins >= 0 && skins.Length != expectedSkins) throw new InvalidDataException("LOD" + level + " has " + skins.Length + " skins; the receipt records " + expectedSkins + ".");
                if (skins.Length == 0) throw new InvalidDataException("LOD" + level + " has no skinned mesh.");
                foreach (SkinnedMeshRenderer skin in skins)
                {
                    Transform[] previous = skin.bones;
                    Transform[] remapped = new Transform[previous.Length];
                    for (int i = 0; i < previous.Length; i++)
                    {
                        Transform shared;
                        if (previous[i] == null || !nodes.TryGetValue(previous[i].name, out shared))
                            throw new InvalidDataException("LOD" + level + " bone has no LOD0 counterpart: " + (previous[i] != null ? previous[i].name : "missing") + ".");
                        if (Vector3.Distance(previous[i].position, shared.position) > .0001f || Quaternion.Angle(previous[i].rotation, shared.rotation) > .01f)
                            throw new InvalidDataException("LOD" + level + " bind frame differs from LOD0 at " + shared.name + ".");
                        remapped[i] = shared;
                    }
                    Transform root;
                    if (skin.rootBone == extra.transform) root = primaryRoot;
                    else if (skin.rootBone == null || !nodes.TryGetValue(skin.rootBone.name, out root)) throw new InvalidDataException("LOD" + level + " root bone is not shared.");
                    skin.bones = remapped;
                    skin.rootBone = root;
                    skin.transform.SetParent(art, true);
                    moved.Add(skin);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(extra);
            }
            return moved;
        }

        /// <summary>The original rig faces +Z with its left hand at -X, in metres. Corrects a backward export from the
        /// review_forward marker, never mirrors, and refuses wrong units or nonuniform transforms.</summary>
        public static void Orient(GameObject root)
        {
            Transform front = Named(root, "review_forward"), head = Named(root, "Head"), left = Named(root, "hand_l"), right = Named(root, "hand_r");
            if (front.position.z < head.position.z) root.transform.Find("Art").localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (front.position.z <= head.position.z || left.position.x >= right.position.x)
                throw new InvalidDataException("Source forward or handedness does not match the original rig (forward +Z, left hand at -X). Re-export with applied axes; no mirroring was attempted.");
            CheckUnits(root, head);
        }

        /// <summary>
        /// Orient for a dropped-in character: the review_forward marker when the export kept it, otherwise toes in front of
        /// the ankles, otherwise a spread left hand at -X (LiveCastFolderRules.Facing). Never mirrors.
        /// </summary>
        public static void OrientAny(GameObject root)
        {
            Transform head = Named(root, "Head"), left = Named(root, "hand_l"), right = Named(root, "hand_r"), ankle = Named(root, "foot_l");
            Transform front = FindNamed(root, "review_forward"), toe = FindNamed(root, "ball_l");
            LiveFacing facing = LiveCastFolderRules.Facing(front != null, front != null ? front.position.z : 0f, head.position.z,
                toe != null, toe != null ? toe.position.z : 0f, ankle.position.z, left.position.x, right.position.x);
            if (facing == LiveFacing.Unknown)
                throw new InvalidDataException("Cannot tell which way the character faces. Keep the review_forward empty from MalikVeyrH9.blend in the export (tick Empty under Object Types).");
            if (facing == LiveFacing.TurnAround) root.transform.Find("Art").localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (left.position.x >= right.position.x)
                throw new InvalidDataException("The left hand is not on the character's left after import (a mirrored export). Use Forward -Z and Up Y in the FBX export; no mirroring was attempted.");
            CheckUnits(root, head);
        }

        private static void CheckUnits(GameObject root, Transform head)
        {
            float height = head.position.y - Mathf.Min(Named(root, "foot_l").position.y, Named(root, "foot_r").position.y);
            if (height < 1.1f || height > 2.5f) throw new InvalidDataException("Native source units or up axis do not match an adult candidate. Re-export; no world scaling was attempted.");
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale)) throw new InvalidDataException("Nonuniform or mirrored source transform: " + node.name);
        }

        /// <summary>The LOD level of a prepared skin from its "LOD0_" style name prefix, or -1.</summary>
        public static int LodLevel(string name)
        {
            for (int level = 0; level < 3; level++) if (name.StartsWith("LOD" + level + "_", StringComparison.Ordinal)) return level;
            return -1;
        }

        /// <summary>
        /// Readable, complete weights (no pruning beyond maxInfluences), no clips, plain hierarchy, embedded named materials,
        /// a Generic rig, no cameras or lights. The delivered FBX files carry a .meta with no import settings, which Unity
        /// imports with its legacy defaults (a Legacy rig adds an Animation component, which the source check refuses, and
        /// external material copies), so every setting the preparation relies on is stated here (owner report 2026-10-24).
        /// </summary>
        public static bool ModelReady(ModelImporter m, int maxInfluences)
        {
            return m.isReadable && !m.importAnimation && !m.optimizeGameObjects && m.preserveHierarchy && m.skinWeights == ModelImporterSkinWeights.Custom
                && m.maxBonesPerVertex == maxInfluences && m.minBoneWeight == 0f && m.importTangents == ModelImporterTangents.CalculateMikk
                && m.materialImportMode == ModelImporterMaterialImportMode.ImportViaMaterialDescription && m.materialLocation == ModelImporterMaterialLocation.InPrefab
                && m.animationType == ModelImporterAnimationType.Generic && !m.importCameras && !m.importLights;
        }

        /// <summary>Applies ModelReady's settings and re-imports, only when a value actually changes.</summary>
        public static void ConfigureModel(ModelImporter importer, int maxInfluences)
        {
            if (ModelReady(importer, maxInfluences)) return;
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = maxInfluences;
            importer.minBoneWeight = 0f;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importCameras = false;
            importer.importLights = false;
            // A Generic rig gives at most an Animator without a controller (allowed and switched off); Legacy adds an Animation.
            importer.animationType = ModelImporterAnimationType.Generic;
            // Embedded materials keep the FBX material names the preparation reads; the prepared copy replaces them.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// A base colour map is an sRGB default 2D texture; a normal map is a linear 2D normal map. The shape is stated
        /// because the delivered .meta files carry no settings and Unity imported those images as cube maps (owner report
        /// 2026-10-24: "Unity has not imported ...T_Eye_Brown.png as a texture").
        /// </summary>
        public static bool TextureReady(TextureImporter t, bool normal)
        {
            return t.textureType == (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) && t.sRGBTexture == !normal
                && t.textureShape == TextureImporterShape.Texture2D;
        }

        /// <summary>Applies TextureReady's settings and re-imports, only when a value actually changes.</summary>
        public static void ConfigureTexture(TextureImporter t, bool normal)
        {
            if (TextureReady(t, normal)) return;
            t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.sRGBTexture = !normal;
            t.textureShape = TextureImporterShape.Texture2D;
            t.SaveAndReimport();
        }

        /// <summary>The 2D texture at path, importing it once more synchronously when the first load finds nothing.</summary>
        public static Texture2D LoadTexture(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureShape != TextureImporterShape.Texture2D)
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.SaveAndReimport();
            }
            else AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif
