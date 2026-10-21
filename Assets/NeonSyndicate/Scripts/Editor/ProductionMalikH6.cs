#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate.Editor
{
    /// <summary>Native preparation and reversible inspection of the actual H6 art. No registry or saved state is enabled.</summary>
    public sealed partial class ProductionMalikH6 : EditorWindow
    {
        [SerializeField] private bool _h7,_authoredAccents;
        private string Root {get{return _h7?MalikReviewH7.Root:"Assets/NeonSyndicate/Art/ProductionH6/";}}
        private string Version {get{return _h7?"H7":"H6";}}
        private string Export {get{return _h7?MalikReviewH7.ExportPath:MalikReviewH6.ExportPath;}}
        private string Id {get{return _h7?MalikReviewH7.AssetId:MalikReviewH6.AssetId;}}
        private string Prepared {get{return Root+"Prepared/"+Version+"Review.prefab";}}
        [Serializable] private sealed class Receipt {public string assetId,creator,license;public bool commercialUse,publicSourceApproved,preserveInfluences;public Entry[] files;}
        [Serializable] private sealed class Entry {public string path,sha256;}
        [SerializeField] private GameObject _prepared;
        [SerializeField] private string _report="H6 candidate only. Preparation, gameplay and owner appearance acceptance are separate. All native checks are NOT RUN until you actually execute them.";
        private bool _qualityOwned;
        private SkinWeights _priorWeights;
        private Vector2 _scroll;
        [MenuItem("Neon Syndicate/04OH/H6 Malik production review")]
        public static void Open(){ProductionMalikH6 window=GetWindow<ProductionMalikH6>("Malik art review");window.SelectVersion(false);}
        [MenuItem("Neon Syndicate/04OH/H7 Living Malik review")]
        public static void OpenH7(){ProductionMalikH6 window=GetWindow<ProductionMalikH6>("Malik art review");window.SelectVersion(true);}
        private void SelectVersion(bool h7){Stop();_h7=h7;_prepared=AssetDatabase.LoadAssetAtPath<GameObject>(Prepared);_authoredAccents=false;_fullBodyH8=false;}
        private void OnEnable(){EditorApplication.playModeStateChanged+=PlayState;AssemblyReloadEvents.beforeAssemblyReload+=Stop;EditorApplication.projectChanged+=Stop;}
        private void OnDisable(){Stop();EditorApplication.playModeStateChanged-=PlayState;AssemblyReloadEvents.beforeAssemblyReload-=Stop;EditorApplication.projectChanged-=Stop;}
        private void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.ExitingEditMode)Stop();}
        private void OnGUI()
        {
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("Actual "+Version+" candidate, NOT APPROVED. Original Malik stays authoritative. This panel does not enable a registry, migrate saves or grant equipment. Authored accents require a separate opt-in. Start the existing Gothic Civic H5 world normally before gameplay review.",MessageType.Info);
            _prepared=(GameObject)EditorGUILayout.ObjectField("Prepared visual only",_prepared,typeof(GameObject),false);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if(GUILayout.Button("Prepare actual "+Version+" model and materials (disabled)"))Prepare();
            EditorGUILayout.HelpBox("Full skin weights are retained because four-weight pruning failed. Explicit Editor review temporarily selects Unlimited skin weights, then restores the prior live value on revert, death, travel, window close or reload. No project quality asset or player setting is saved. Check actual hardware and all presets before production approval.",MessageType.Warning);
            DrawFullBodyH8();
            if(_h7 && !_fullBodyH8)_authoredAccents=EditorGUILayout.Toggle("Review authored torso accents",_authoredAccents);
            if(_h7 && !_fullBodyH8)EditorGUILayout.HelpBox("Five genuine source actions exist. This optional review samples idle and right-cut chest/neck/head accents only. Hands, feet, equipment, damage and controls remain with the original pose owner. Full-body locomotion playback is NOT ACCEPTED.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _prepared==null))
                if(GUILayout.Button("Start reversible Editor gameplay review"))Begin();
            if(GUILayout.Button("Revert to original fallback"))Stop();
            using(new EditorGUI.DisabledScope(_h7 || _prepared==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Open original proof wizard, still disabled"))ImportedVisualWizard04OF.OpenProducedH6(_prepared);
            EditorGUILayout.TextArea(_report,GUILayout.MinHeight(170));
            EditorGUILayout.EndScrollView();
        }
        private Receipt ReadReceipt()
        {
            Receipt r=JsonUtility.FromJson<Receipt>(File.ReadAllText(Root+"candidate.json"));
            if(r==null || r.assetId!=Id || r.license!="CC0-1.0" || !r.commercialUse || !r.publicSourceApproved || r.files==null || r.files.Length<8)
                throw new InvalidDataException("Missing actual model rights and file receipt.");
            foreach(Entry e in r.files)
                if(e==null || !CinematicAcceptance04OF.RelativeBonePath(e.path) || !e.path.StartsWith(Root,StringComparison.Ordinal) || !ImportedArt04OH.Hex(e.sha256,64) || ImportedArtProof04OH.FileHash(e.path)!=e.sha256)
                    throw new InvalidDataException("Candidate source/texture fingerprint changed. Reinspect before review: "+(e!=null?e.path:"missing entry"));
            return r;
        }
        private void Prepare()
        {
            GameObject copy=null;
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepare only in EditMode.");
                Receipt r=ReadReceipt();
                if(_h7)PrepareMotion();
                _prepared=AssetDatabase.LoadAssetAtPath<GameObject>(Prepared);
                if(_prepared!=null){_report="A prepared copy already exists. It was not overwritten. Reinspect its current source/dependency proof before starting review.";return;}
                if(!AssetDatabase.IsValidFolder(Root+"Prepared"))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Prepared");
                ModelImporter importer=AssetImporter.GetAtPath(Export) as ModelImporter;
                if(importer==null)throw new InvalidDataException("Wait for the actual FBX import and inspect the first Console error.");
                importer.isReadable=true;importer.importAnimation=false;importer.optimizeGameObjects=false;importer.preserveHierarchy=true;
                importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=r.preserveInfluences?8:4;importer.minBoneWeight=0;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
                GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(Export);
                if(source==null)throw new InvalidDataException("Native FBX import did not return a model.");
                foreach(Component c in source.GetComponentsInChildren<Component>(true))
                {
                    if(c==null || !ImportedArt04OH.VisualComponent(c.GetType()))throw new InvalidDataException("Unexpected source component before clone.");
                    Animator animator=c as Animator;if(animator!=null && animator.runtimeAnimatorController!=null)throw new InvalidDataException("An Animator controller cannot enter the visual copy.");
                }
                copy=new GameObject(Version+" Malik candidate, appearance NOT APPROVED");
                GameObject art=new GameObject("Art");art.transform.SetParent(copy.transform,false);
                GameObject primary=(GameObject)PrefabUtility.InstantiatePrefab(source);primary.name="LOD0 source";primary.transform.SetParent(art.transform,false);
                // All exported LODs share one actual bind skeleton. Only the first skeleton is animated.
                Dictionary<string,Transform> sharedBones=new Dictionary<string,Transform>(StringComparer.Ordinal);
                foreach(SkinnedMeshRenderer skin in primary.GetComponentsInChildren<SkinnedMeshRenderer>(true))foreach(Transform bone in skin.bones)
                {
                    Transform existing;if(bone==null)throw new InvalidDataException("Missing primary LOD bone.");
                    if(sharedBones.TryGetValue(bone.name,out existing) && existing!=bone)throw new InvalidDataException("Ambiguous primary skin bone: "+bone.name);
                    sharedBones[bone.name]=bone;
                }
                foreach(Transform node in primary.GetComponentsInChildren<Transform>(true))if(!sharedBones.ContainsKey(node.name))sharedBones.Add(node.name,node);
                for(int level=1;level<3;level++)AttachLod(art,primary.transform,sharedBones,level,r.preserveInfluences);
                foreach(Animator animator in art.GetComponentsInChildren<Animator>(true)){animator.enabled=false;animator.applyRootMotion=false;}
                Orient(copy);
                Dictionary<string,Material> materials=new Dictionary<string,Material>(StringComparer.Ordinal);
                List<Renderer>[] lods={new List<Renderer>(),new List<Renderer>(),new List<Renderer>()};
                foreach(SkinnedMeshRenderer skin in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(skin.sharedMesh==null || skin.bones.Length==0 || skin.sharedMesh.bindposes.Length!=skin.bones.Length)throw new InvalidDataException("Incomplete actual native skin: "+skin.name);
                    SkinWeightsH5.Referenced(skin.sharedMesh,skin.bones.Length);skin.quality=SkinQuality.Auto;
                    Material[] targets=skin.sharedMaterials;
                    for(int i=0;i<targets.Length;i++)
                    {
                        string name=targets[i]!=null?targets[i].name.Replace('_',' ').Replace(" (Instance)","").Trim():"";
                        Material material;if(!materials.TryGetValue(name,out material)){material=Surface(name);materials.Add(name,material);}targets[i]=material;
                    }
                    skin.sharedMaterials=targets;skin.updateWhenOffscreen=false;
                    Vector3 center=skin.transform.InverseTransformPoint(copy.transform.TransformPoint(new Vector3(0,.95f,0)));
                    float scale=skin.transform.lossyScale.x;skin.localBounds=new Bounds(center,new Vector3(3.2f,3.2f,3.2f)/Mathf.Max(.0001f,scale));
                    int level=skin.name.StartsWith("LOD0",StringComparison.Ordinal)?0:skin.name.StartsWith("LOD1",StringComparison.Ordinal)?1:skin.name.StartsWith("LOD2",StringComparison.Ordinal)?2:-1;
                    if(level<0)throw new InvalidDataException("Missing actual LOD membership: "+skin.name);lods[level].Add(skin);
                }
                for(int i=0;i<3;i++)if(lods[i].Count<8)throw new InvalidDataException("Missing actual source LOD geometry.");
                LODGroup group=copy.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.55f,lods[0].ToArray()),new LOD(.22f,lods[1].ToArray()),new LOD(.025f,lods[2].ToArray())});group.localReferencePoint=new Vector3(0,.95f,0);group.size=1.9f;
                _prepared=PrefabUtility.SaveAsPrefabAsset(copy,Prepared);if(_prepared==null)throw new InvalidOperationException("Native prefab save failed.");
                AssetDatabase.SaveAssets();Selection.activeObject=_prepared;
                _report="Native preparation executed, candidate remains DISABLED and NOT APPROVED. Inspect actual face, materials, all 47 mappings, three LODs and weapon contact. Start ordinary gameplay, then request reversible review. Preparation alone is not a passed gameplay or appearance test.";
            }
            catch(Exception e){_report="Preparation refused safely: "+e.Message;Log.Warn(_report);}
            finally{if(copy!=null)DestroyImmediate(copy);}
        }
        private void PrepareMotion()
        {
            string path=Root+"Animations/MalikMotionH7.fbx";
            ModelImporter importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new InvalidDataException("Actual authored H7 motion FBX has not imported.");
            importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;importer.preserveHierarchy=true;
            ModelImporterClipAnimation[] settings=importer.defaultClipAnimations;
            foreach(ModelImporterClipAnimation setting in settings){setting.loopTime=setting.name.IndexOf("H7_OathCut",StringComparison.Ordinal)<0;setting.lockRootRotation=true;setting.lockRootHeightY=true;setting.lockRootPositionXZ=true;}
            if(settings.Length>0)importer.clipAnimations=settings;
            importer.SaveAndReimport();
            int clips=0;
            foreach(UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                AnimationClip clip=item as AnimationClip;
                if(clip==null || clip.name.StartsWith("__preview__",StringComparison.Ordinal))continue;
                if(clip.events.Length!=0)throw new InvalidDataException("Authored source cannot add animation events or root-motion authority.");
                clips++;
            }
            if(clips!=5)throw new InvalidDataException("Expected five actual imported H7 actions. Inspect native Animation tab; keep fallback.");
        }
        private void AttachLod(GameObject art,Transform primaryRoot,Dictionary<string,Transform> sharedBones,int level,bool preserve)
        {
            string path=Root+"Models/MalikVeyr"+Version+"_LOD"+level+".fbx";
            ModelImporter importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new InvalidDataException("Missing actual lower-detail source: "+path);
            importer.isReadable=true;importer.importAnimation=false;importer.optimizeGameObjects=false;importer.preserveHierarchy=true;
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=preserve?8:4;importer.minBoneWeight=0;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
            GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)throw new InvalidDataException("Lower-detail FBX did not import.");
            foreach(Component component in source.GetComponentsInChildren<Component>(true))
            {
                if(component==null || !ImportedArt04OH.VisualComponent(component.GetType()))throw new InvalidDataException("Unsupported lower-detail source component.");
                Animator animator=component as Animator;if(animator!=null && animator.runtimeAnimatorController!=null)throw new InvalidDataException("A lower-detail source cannot add animation authority.");
            }
            GameObject extra=(GameObject)PrefabUtility.InstantiatePrefab(source);extra.transform.SetParent(art.transform,false);
            try
            {
                SkinnedMeshRenderer[] skins=extra.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length!=13)throw new InvalidDataException("Expected thirteen real material batches in LOD "+level);
                foreach(SkinnedMeshRenderer skin in skins)
                {
                    Transform[] previous=skin.bones;Transform[] remapped=new Transform[previous.Length];
                    for(int i=0;i<previous.Length;i++)
                    {
                        if(previous[i]==null || !sharedBones.TryGetValue(previous[i].name,out remapped[i]))throw new InvalidDataException("LOD bone lacks original bind authority.");
                        if(Vector3.Distance(previous[i].position,remapped[i].position)>.0001f || Quaternion.Angle(previous[i].rotation,remapped[i].rotation)>.01f)throw new InvalidDataException("LOD bind frames differ; keep fallback.");
                    }
                    Transform root;if(skin.rootBone==extra.transform)root=primaryRoot;else if(skin.rootBone==null || !sharedBones.TryGetValue(skin.rootBone.name,out root))throw new InvalidDataException("LOD root is not shared.");
                    skin.bones=remapped;skin.rootBone=root;skin.transform.SetParent(art.transform,true);
                }
            }
            finally{DestroyImmediate(extra);}
        }
        private static Transform Named(GameObject root,string name)
        {
            Transform found=null;foreach(Transform node in root.GetComponentsInChildren<Transform>(true))if(node.name==name){if(found!=null)throw new InvalidDataException("Ambiguous model node: "+name);found=node;}
            if(found==null)throw new InvalidDataException("Missing actual source node: "+name);return found;
        }
        private static void Orient(GameObject root)
        {
            Transform front=Named(root,"review_forward"),head=Named(root,"Head"),left=Named(root,"hand_l"),right=Named(root,"hand_r");
            if(front.position.z<head.position.z)root.transform.Find("Art").localRotation=Quaternion.Euler(0,180,0);
            if(front.position.z<=head.position.z || left.position.x>=right.position.x)throw new InvalidDataException("Source handedness/forward contract does not match the original rig. Re-export applied axes; no mirroring was attempted.");
            float height=head.position.y-Mathf.Min(Named(root,"foot_l").position.y,Named(root,"foot_r").position.y);
            if(height<1.1f || height>2.5f)throw new InvalidDataException("Native source units/up axis do not match this adult candidate. Re-export; no world scaling was attempted.");
            foreach(Transform node in root.GetComponentsInChildren<Transform>(true))if(!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale))throw new InvalidDataException("Nonuniform or mirrored source transform: "+node.name);
        }
        private Texture2D Texture(string name,bool normal)
        {
            string path=Root+"Textures/"+name;TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new FileNotFoundException("Missing actual texture: "+path);
            TextureImporterType kind=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(importer.textureType!=kind || importer.sRGBTexture==normal){importer.textureType=kind;importer.sRGBTexture=!normal;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private Material Surface(string name)
        {
            string file=Root+"Prepared/"+name.Replace(' ','_')+".mat";Material existing=AssetDatabase.LoadAssetAtPath<Material>(file);if(existing!=null)return existing;
            Color color=Color.white;float metal=0,smooth=.25f;string albedo="",normal="";bool cutout=false,clear=false;Color emission=Color.black;
            switch(name)
            {
                case "H6 Skin":albedo="Skin_Albedo_Painted.png";normal="Skin_Normal.png";smooth=.45f;break;
                case "H6 Charcoal wool":albedo="Cloth_Albedo.png";normal="Cloth_Normal.png";smooth=.21f;break;
                case "H6 Worn leather":albedo="Leather_Albedo.png";normal="Leather_Normal.png";smooth=.24f;break;
                case "H6 Blackened steel":color=new Color(.025f,.03f,.038f);metal=.75f;smooth=.45f;break;
                case "H6 Aged bronze":color=new Color(.18f,.095f,.035f);metal=.78f;smooth=.53f;break;
                case "H6 Restrained amethyst":color=new Color(.16f,.035f,.28f);metal=.15f;smooth=.78f;emission=new Color(.12f,.012f,.25f)*.24f;break;
                case "H6 Brows":albedo="Brows_Albedo.png";cutout=true;smooth=.4f;break;
                case "H6 Lashes":albedo="Lashes_Albedo.png";cutout=true;smooth=.4f;break;
                case "H6 Eyes":albedo="Eyes_Albedo.png";smooth=.85f;break;
                case "H6 Sclera":color=new Color(.27f,.265f,.24f);smooth=.6f;break;
                case "H6 Pupils":color=new Color(.001f,.0007f,.0005f);smooth=.79f;break;
                case "H6 Cornea":color=new Color(.8f,.85f,.9f,.035f);smooth=.965f;clear=true;break;
                case "H6 Short groom":color=new Color(.008f,.005f,.003f);smooth=.25f;break;
                default:throw new InvalidDataException("Unrecorded actual model material: "+name);
            }
            Shader shader=Mats.StandardShader;if(shader==null)throw new InvalidOperationException("The original supported Lit shader is unavailable.");
            Material m=new Material(shader){name=name};m.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",color);m.SetFloat("_Metallic",metal);m.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",smooth);
            if(albedo!="")m.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",Texture(albedo,false));
            if(normal!=""){m.SetTexture("_BumpMap",Texture(normal,true));m.SetFloat("_BumpScale",name=="H6 Skin"?.35f:.65f);m.EnableKeyword("_NORMALMAP");}
            if(_h7 && name=="H6 Skin"){m.SetFloat("_BumpScale",.28f);m.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",.41f);}
            if(_h7 && name=="H6 Short groom")m.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",.17f);
            m.SetColor("_EmissionColor",emission);m.EnableKeyword("_EMISSION");
            if(cutout){m.SetFloat("_Mode",1);m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.3f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
            if(clear)
            {
                m.SetOverrideTag("RenderType","Transparent");m.SetFloat("_Mode",3);m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;
            }
            AssetDatabase.CreateAsset(m,file);return m;
        }
        private ImportedModel04OD ReviewRow(Receipt receipt)
        {
            if(_prepared==null)throw new InvalidDataException("Prepare the actual separate visual first.");
            ImportedBone04OD[] mapping=new ImportedBone04OD[RigLayout.BoneCount];HashSet<Transform> mapped=new HashSet<Transform>();
            for(int i=0;i<mapping.Length;i++){Transform t=Named(_prepared,ImportedBindH3.CandidateBone(RigLayout.Names[i]));mapping[i]=new ImportedBone04OD{SourceBone=i,TargetPath=AnimationUtility.CalculateTransformPath(t,_prepared.transform)};mapped.Add(t);}
            List<string> passive=new List<string>();foreach(SkinnedMeshRenderer skin in _prepared.GetComponentsInChildren<SkinnedMeshRenderer>(true))foreach(Transform bone in skin.bones)
            {if(bone==null)throw new InvalidDataException("Missing native weighted transform.");string path=AnimationUtility.CalculateTransformPath(bone,_prepared.transform);if(!mapped.Contains(bone) && !passive.Contains(path))passive.Add(path);}
            List<ImportedSocket04OD> sockets=new List<ImportedSocket04OD>();foreach(string role in new[]{"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"})
                sockets.Add(new ImportedSocket04OD{Role=role,Path=AnimationUtility.CalculateTransformPath(Named(_prepared,"socket_"+role),_prepared.transform)});
            ImportedArtRecord04OH proof=new ImportedArtRecord04OH{AssetId=receipt.assetId,Creator=receipt.creator,SourceUrl="https://static.makehumancommunity.org/about/license.html",LicenseId=receipt.license,RightsRecordId=_h7?"h7-cc0-asset-and-motion-lineage":"h6-cc0-asset-lineage",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false};
            ImportedArtProof04OH.Stamp(proof,AssetDatabase.LoadAssetAtPath<GameObject>(Export),_prepared);
            return new ImportedModel04OD{Identity="malik",Enabled=false,Prefab=_prepared,Scale=1,AlignBindAxesH3=true,PreserveInfluencesH5=receipt.preserveInfluences,Bones=mapping,PassiveBonePaths=passive.ToArray(),Sockets=sockets.ToArray(),ArtProof=proof,AnimationAuthority=ImportedAnimationAuthority04OH.ExistingPose};
        }
        private void Begin()
        {
            try
            {
                Stop();Receipt receipt=ReadReceipt();ImportedModel04OD row=ReviewRow(receipt);string reason;
                if(_h7? !MalikReviewH7.CanReview(row,true,out reason):!MalikReviewH6.CanReview(row,true,out reason))throw new InvalidDataException(reason);
                if(AssetDatabase.GetAssetPath(_prepared)!=Prepared)throw new InvalidDataException("Prepared prefab version does not match the selected candidate.");
                MalikMotionDataH7 motion=null;
                if(_h7 && _authoredAccents)
                {
                    TextAsset data=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Animations/MalikMotionH7.json");
                    if(data==null)throw new InvalidDataException("Actual H7 authored sample asset has not imported.");
                    motion=JsonUtility.FromJson<MalikMotionDataH7>(data.text);
                    if(!MalikMotionH7.Valid(motion,out reason))throw new InvalidDataException(reason);
                }
                // Explicit Editor-only live inspection scope. No project asset or persistent setting is written.
                if(row.PreserveInfluencesH5){_priorWeights=QualitySettings.skinWeights;QualitySettings.skinWeights=SkinWeights.Unlimited;_qualityOwned=true;}
                if(!ImportedVisual04OD.BeginReviewH6(Game.Player,row,RestoreQuality,out reason))throw new InvalidOperationException(reason);
                if(motion!=null && !ImportedVisual04OD.ReviewMotionH7(Game.Player,motion,out reason)){Stop();throw new InvalidOperationException(reason);}
                BeginFullBodyH8();
                _report="Asset "+row.ArtProof.AssetId+" / "+row.ArtProof.LicenseId+" / source SHA "+row.ArtProof.ExportSha256+" / visual dependencies "+row.ArtProof.VisualDependencyHash+" / original semantic bones "+row.Bones.Length+" / passive paths "+row.PassiveBonePaths.Length+" / pose authority "+row.AnimationAuthority+" / source "+row.ArtProof.ExportAssetPath+"\nREVIEW RUNNING in the real scene. Candidate is not production-enabled or owner-approved. Existing movement, Pause, weapons, camera, BONDS and Resonance still own gameplay. Exercise actual actions and record outcomes; no tests are auto-marked passed. Close this panel or Revert to restore fallback.";
            }
            catch(Exception e){Stop();_report="Review refused safely: "+e.Message;Log.Warn(_report);}
        }
        private void Stop(){if(EditorApplication.isPlaying)ImportedVisual04OD.EndReviewH6(Game.Player);RestoreQuality();}
        private void RestoreQuality()
        {
            if(!_qualityOwned)return;_qualityOwned=false;
            if(QualitySettings.skinWeights==SkinWeights.Unlimited)QualitySettings.skinWeights=_priorWeights;
        }
    }
}
#endif
