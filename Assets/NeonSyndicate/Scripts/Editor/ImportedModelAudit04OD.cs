#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Read-only import audit. No scale, rig, material or GUID is changed by this command.</summary>
    public static class ImportedModelAudit04OD
    {
        [MenuItem("Neon Syndicate/04OD/Audit imported models")]
        public static void Audit()
        {
            int checkedModels=0,errors=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/NeonSyndicate/Art/Imported"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);ModelImporter importer=AssetImporter.GetAtPath(path) as ModelImporter;
                if(importer==null)continue;checkedModels++;
                GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(model==null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==0){Debug.LogError("04OD missing rigged mesh: "+path);errors++;continue;}
                if(importer.animationType==ModelImporterAnimationType.Human && (model.GetComponent<Animator>()==null || model.GetComponent<Animator>().avatar==null || !model.GetComponent<Animator>().avatar.isValid))
                {Debug.LogError("04OD invalid humanoid avatar: "+path);errors++;}
                foreach(SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(skin.sharedMesh==null || skin.bones.Length==0 || skin.localBounds.size.y<.1f || skin.localBounds.size.y>20f){Debug.LogError("04OD invalid scale or skin: "+path);errors++;}
                    foreach(Material material in skin.sharedMaterials)if(material==null || material.shader==null || !material.shader.isSupported){Debug.LogError("04OD missing or unsupported material: "+path);errors++;}
                }
            }
            Debug.Log("04OD import audit: "+checkedModels+" models, "+errors+" errors. Zero models means ASSET REQUIRED, not a passed character import.");
        }
        [MenuItem("Neon Syndicate/04OD/Smoke test selected visual fallback")]
        public static void Smoke()
        {
            if(!EditorApplication.isPlaying || Selection.activeGameObject==null){Debug.LogWarning("04OD: in Play Mode select Malik or a live creature; the existing scene is required.");return;}
            CharacterBase owner=Selection.activeGameObject.GetComponentInParent<CharacterBase>();
            if(owner==null){Debug.LogWarning("04OD: selection has no original character owner.");return;}
            ImportedVisual04OD.Attach(owner);Debug.Log("04OD requested adapter smoke test for "+owner.DisplayName+". Inspect renderer, sockets, strike contact, ragdoll fallback and Console; this command alone is not acceptance.");
        }
    }
}
#endif
