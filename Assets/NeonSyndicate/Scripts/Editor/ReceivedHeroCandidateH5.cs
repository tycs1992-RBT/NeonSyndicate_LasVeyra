#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Explicit, disabled preparation of received CC0 art. No gameplay prefab or save is touched.</summary>
    public sealed class ReceivedHeroCandidateH5 : EditorWindow
    {
        private const string Root="Assets/NeonSyndicate/Art/CandidatesH5/";
        private const string Textures="Assets/NeonSyndicate/Art/CandidatesH3/Textures/";
        [Serializable] private sealed class Manifest { public Entry[] models;public Entry[] files; }
        [Serializable] private sealed class Entry {public string output,derived_sha256,path,sha256;}
        [MenuItem("Neon Syndicate/04OH/Prepare normalized H5 reference (disabled)",true)]
        public static bool CanPrepare(){return !EditorApplication.isPlayingOrWillChangePlaymode;}
        [MenuItem("Neon Syndicate/04OH/Prepare normalized H5 reference (disabled)")]
        public static void Prepare()
        {
            GameObject copy=null;
            try
            {
                if(!CanPrepare())throw new InvalidOperationException("Prepare only in EditMode, never during gameplay.");
                Manifest original=JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/NeonSyndicate/Art/CandidatesH3/candidate.json"));
                if(original==null || original.files==null || original.files.Length!=17)throw new InvalidDataException("Original source lineage is incomplete.");
                foreach(Entry file in original.files)if(ImportedArtProof04OH.FileHash(file.path)!=file.sha256)throw new InvalidDataException("Original source or texture changed: "+file.path);
                Manifest manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"skin-lineage.json"));
                if(manifest==null || manifest.models==null || manifest.models.Length!=5)throw new InvalidDataException("Missing received-art manifest.");
                foreach(Entry file in manifest.models)
                    if(string.IsNullOrWhiteSpace(file.output) || !file.output.StartsWith(Root,StringComparison.Ordinal) || file.output.Contains("..") || ImportedArtProof04OH.FileHash(file.output)!=file.derived_sha256)throw new InvalidDataException("Received source changed: "+file.output+". Reinspect provenance and asset data.");
                if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prepared/HeroCandidateH5.prefab")!=null)throw new InvalidOperationException("A prepared candidate already exists. Review it before rebuilding; no existing candidate was overwritten.");
                if(!AssetDatabase.IsValidFolder(Root+"Prepared"))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Prepared");
                GameObject clothing=Model("TravelClothing"),head=Model("MaleBase");
                copy=new GameObject("Hero candidate H5, appearance approval pending");
                GameObject body=(GameObject)PrefabUtility.InstantiatePrefab(clothing);body.name="Body";body.transform.SetParent(copy.transform,false);DisableAnimation(body);
                Dictionary<string,Transform> bones=UniqueBones(body);
                Material cloth=MaterialFor("Cloth",new Color(.48f,.47f,.52f)),hands=MaterialFor("Hands",Color.white),skin=MaterialFor("Skin",Color.white),hair=MaterialFor("Hair",new Color(.45f,.38f,.34f)),eye=MaterialFor("Eye",Color.white);
                foreach(SkinnedMeshRenderer renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    ValidateSkin(renderer);
                    Material[] materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        string name=materials[i]!=null?materials[i].name.ToLowerInvariant():"";
                        materials[i]=name.Contains("skin") || name.Contains("regular") || name.Contains("base")?hands:cloth;
                    }
                    renderer.sharedMaterials=materials;
                }
                AddPart(copy,bones,head,"ReceivedHead",skin,eye,true);
                foreach(string part in new[]{"Beard","ShortHair","Brows"})AddPart(copy,bones,Model(part),part,hair,hair,false);
                string path=Root+"Prepared/HeroCandidateH5.prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)throw new InvalidOperationException("Prepared candidate already exists. Review it or choose a new source path before rebuilding; no reviewed asset was overwritten.");
                GameObject prefab=PrefabUtility.SaveAsPrefabAsset(copy,path);if(prefab==null)throw new InvalidOperationException("Candidate prefab creation failed.");
                AssetDatabase.SaveAssets();Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
                ImportedVisualWizard04OF.OpenDerivedH5(prefab);
                Log.Info("H5 candidate PREPARED for native inspection only. Owner appearance approval and registry activation remain false. Validate weights, face/coat identity, materials, 47 mapped bones, weapons, flight and fallback. No authored clips are present.");
            }
            catch(Exception ex){Log.Warn("H5 candidate preparation stopped safely: "+ex.Message);EditorUtility.DisplayDialog("Candidate remains disabled",ex.Message,"OK");}
            finally{if(copy!=null)DestroyImmediate(copy);}
        }
        private static GameObject Model(string name)
        {
            GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+name+".fbx");
            if(model==null)throw new FileNotFoundException("Wait for Unity to import the received model: "+name);
            foreach(Component component in model.GetComponentsInChildren<Component>(true))
            {
                if(component==null || !ImportedArt04OH.VisualComponent(component.GetType()))throw new InvalidDataException("Unexpected component before clone: "+name);
                Animator animator=component as Animator;if(animator!=null && animator.runtimeAnimatorController!=null)throw new InvalidDataException("A candidate must not carry an Animator controller.");
            }
            ModelImporter importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(model)) as ModelImporter;
            if(importer!=null && (importer.skinWeights!=ModelImporterSkinWeights.Custom || importer.maxBonesPerVertex!=8 || importer.minBoneWeight!=0 || !importer.isReadable))
            {
                importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=8;importer.minBoneWeight=0;importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
                model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+name+".fbx");
            }
            return model;
        }
        private static Dictionary<string,Transform> UniqueBones(GameObject body)
        {
            Dictionary<string,Transform> bones=new Dictionary<string,Transform>(StringComparer.Ordinal);
            foreach(Transform t in body.GetComponentsInChildren<Transform>(true))
            {if(bones.ContainsKey(t.name))throw new InvalidDataException("Ambiguous candidate transform: "+t.name);bones.Add(t.name,t);}
            foreach(string original in RigLayout.Names)if(!bones.ContainsKey(ImportedBindH3.CandidateBone(original)))throw new InvalidDataException("Candidate lacks original mapped bone: "+original);
            return bones;
        }
        private static void ValidateSkin(SkinnedMeshRenderer renderer)
        {
            if(renderer.sharedMesh==null || renderer.bones.Length==0 || renderer.sharedMesh.bindposes.Length!=renderer.bones.Length)throw new InvalidDataException("Incomplete native skin: "+renderer.name);
            SkinWeightsH5.Referenced(renderer.sharedMesh,renderer.bones.Length);renderer.quality=SkinQuality.Auto;
        }

        private static void DisableAnimation(GameObject model)
        {foreach(Animator animator in model.GetComponentsInChildren<Animator>(true)){animator.enabled=false;animator.applyRootMotion=false;}}
        private static Material MaterialFor(string role,Color tint)
        {
            string path=Root+"Prepared/"+role+".mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            Shader shader=Mats.StandardShader;if(shader==null)throw new InvalidOperationException("Original Lit shader unavailable.");
            material=new Material(shader){name="H5 candidate / "+role};
            Texture2D albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+role+"_Albedo.jpg");
            if(albedo==null)albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+role+"_Albedo.png");
            if(albedo==null)throw new FileNotFoundException("Missing candidate albedo: "+role);
            material.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",albedo);material.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",tint);
            Texture2D normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+role+"_Normal.png"),mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+role+"_Mask.png");
            if(normal!=null){material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");}
            if(mask!=null){material.SetFloat("_Metallic",1);material.SetTexture("_MetallicGlossMap",mask);material.SetTexture("_OcclusionMap",mask);material.EnableKeyword(RenderPath.IsURP?"_METALLICSPECGLOSSMAP":"_METALLICGLOSSMAP");material.EnableKeyword("_OCCLUSIONMAP");}
            else material.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",role=="Eye"?.55f:.22f);
            material.SetFloat(RenderPath.IsURP?"_Smoothness":"_GlossMapScale",1);
            if(role=="Hair")
            {
                material.SetOverrideTag("RenderType","TransparentCutout");material.SetFloat("_Mode",1);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.45f);material.SetFloat("_ZWrite",1);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
            }
            AssetDatabase.CreateAsset(material,path);return material;
        }
        private static void AddPart(GameObject root,Dictionary<string,Transform> destination,GameObject source,string label,Material main,Material eyes,bool headOnly)
        {
            GameObject part=(GameObject)PrefabUtility.InstantiatePrefab(source);part.name=label;part.transform.SetParent(root.transform,false);DisableAnimation(part);
            Transform sourceHead=null,sourceNeck=null;
            foreach(Transform t in part.GetComponentsInChildren<Transform>(true)){if(t.name=="Head")sourceHead=t;if(t.name=="neck_01")sourceNeck=t;}
            if(sourceHead==null || sourceNeck==null)throw new InvalidDataException("Received part has no head/neck reference: "+label);
            Matrix4x4 alignment=destination["Head"].localToWorldMatrix*sourceHead.worldToLocalMatrix;
            foreach(SkinnedMeshRenderer renderer in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh=Instantiate(renderer.sharedMesh);mesh.name="H5 prepared "+label+" / "+renderer.name;
                Vector3[] vertices=mesh.vertices,normals=mesh.normals;Vector4[] tangents=mesh.tangents;float cutoff=sourceNeck.position.y-.055f;
                if(headOnly)
                {
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        int[] original=mesh.GetTriangles(sub);List<int> kept=new List<int>();
                        for(int i=0;i<original.Length;i+=3)
                            if(renderer.transform.TransformPoint(vertices[original[i]]).y>=cutoff && renderer.transform.TransformPoint(vertices[original[i+1]]).y>=cutoff && renderer.transform.TransformPoint(vertices[original[i+2]]).y>=cutoff)
                            {kept.Add(original[i]);kept.Add(original[i+1]);kept.Add(original[i+2]);}
                        mesh.SetTriangles(kept.ToArray(),sub);
                    }
                }
                Matrix4x4 local=renderer.transform.worldToLocalMatrix*alignment*renderer.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i++){vertices[i]=local.MultiplyPoint3x4(vertices[i]);if(i<normals.Length)normals[i]=local.inverse.transpose.MultiplyVector(normals[i]).normalized;}
                for(int i=0;i<tangents.Length;i++){Vector3 tangent=local.MultiplyVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z)).normalized;tangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,tangents[i].w);}
                mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;
                Transform[] bones=renderer.bones;Matrix4x4[] bind=new Matrix4x4[bones.Length];
                for(int i=0;i<bones.Length;i++)
                {
                    Transform replacement;if(bones[i]==null || !destination.TryGetValue(bones[i].name,out replacement))throw new InvalidDataException("Part has an unmapped source bone: "+label);
                    bones[i]=replacement;bind[i]=replacement.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                }
                mesh.bindposes=bind;mesh.RecalculateBounds();renderer.bones=bones;renderer.rootBone=destination["root"];renderer.localBounds=mesh.bounds;
                SkinWeightsH5.Referenced(mesh,renderer.bones.Length);renderer.quality=SkinQuality.Auto;
                string meshPath=AssetDatabase.GenerateUniqueAssetPath(Root+"Prepared/"+label+"_"+CinematicAcceptance04OF.Label(renderer.name)+".asset");AssetDatabase.CreateAsset(mesh,meshPath);renderer.sharedMesh=mesh;
                Material[] materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){string name=materials[i]!=null?materials[i].name.ToLowerInvariant():"";materials[i]=headOnly && (name.Contains("eye") || name.Contains("cornea"))?eyes:headOnly && name.Contains("hair")?AssetDatabase.LoadAssetAtPath<Material>(Root+"Prepared/Hair.mat"):main;}renderer.sharedMaterials=materials;
            }
        }
    }
}
#endif
