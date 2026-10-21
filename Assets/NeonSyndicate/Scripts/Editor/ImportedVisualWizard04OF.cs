#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Authors the existing visual registry. Never replaces the gameplay prefab or enables an unaudited row.</summary>
    public sealed class ImportedVisualWizard04OF : EditorWindow
    {
        [SerializeField] private GameObject _asset;
        [SerializeField] private string _identity="malik";
        [SerializeField] private float _scale=1;
        [SerializeField] private ImportedArtRecord04OH _proof=new ImportedArtRecord04OH();
        [SerializeField] private bool _humanSockets=true;
        [SerializeField] private bool _alignBindH3;
        [SerializeField] private string[] _names;
        [SerializeField] private string[] _paths;
        [SerializeField] private string[] _socketPaths=new string[10];
        [SerializeField] private string _report="ASSET REQUIRED: select a lawful rigged model or visual prefab from Project.";
        private Vector2 _scroll;
        private static string[] Roles {get{return new[]{"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"};}}
        [MenuItem("Neon Syndicate/04OF/Imported visual wizard")]
        public static void Open(){GetWindow<ImportedVisualWizard04OF>("04OF Visual Import");}
        private void OnEnable(){if(_proof==null)_proof=new ImportedArtRecord04OH();if(_names==null || _paths==null)HumanTemplate();}
        private void HumanTemplate(){_humanSockets=true;_names=(string[])RigLayout.Names.Clone();_paths=new string[_names.Length];}
        [MenuItem("Neon Syndicate/04OH/Imported visual proof wizard")]
        public static void OpenAscension(){Open();}
        [MenuItem("Neon Syndicate/04OH/Open original calibration fixture")]
        public static void OpenFixture()
        {
            ImportedVisualWizard04OF window=GetWindow<ImportedVisualWizard04OF>("04OH Import Proof");
            window.HumanTemplate();window._asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx");
            window._identity="diagnostic:calibration04oh";window._proof=new ImportedArtRecord04OH{AssetId="FIXTURE04OH",Creator="Project-authored calibration",LicenseId="CC0-1.0",RightsRecordId="fixture-license",CommercialUse=true,PublicSourceApproved=true,Diagnostic=true};
            window.Suggest();window.SuggestLocators();window._report+="\nDiagnostic fixture only, not a production hero. Audit after native import; keep every row disabled.";
        }
        public static void OpenReceivedH3(GameObject candidate)
        {
            ImportedVisualWizard04OF window=GetWindow<ImportedVisualWizard04OF>("04OH Received Candidate");window.HumanTemplate();window._asset=candidate;window._identity="malik";window._scale=1;window._alignBindH3=true;
            window._proof=new ImportedArtRecord04OH{AssetId="H3-RECEIVED-HUMANOID-01",Creator="Quaternius",SourceUrl="https://quaternius.com/packs/universalbasecharacters.html",LicenseId="CC0-1.0",RightsRecordId="h3-free-source-fdbf1804-c3468b18",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false,Diagnostic=false};
            Transform[] nodes=candidate.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<window._names.Length;i++)foreach(Transform node in nodes)
                if(node.name==ImportedBindH3.CandidateBone(window._names[i]) && node.IsChildOf(candidate.transform.Find("Body")))window._paths[i]=AnimationUtility.CalculateTransformPath(node,candidate.transform);
            window.SuggestLocators();window._report="RECEIVED CANDIDATE, NOT APPROVED MALIK. Inspect actual native face, clothing, weight normalization, bind axes, hand grip, facial channels and performance. No authored clips were supplied. Final canonical long coat/armor remains artist work. Audit and create a DISABLED row only; appearance approval is unchecked.";
        }
        private void OnGUI()
        {
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("ASSET REQUIRED until you supply a lawful rigged asset. This wizard makes a separate visual prefab and a disabled registry row. Original movement, colliders, health, weapons, saves and procedural animation remain authoritative. Arbitrary animation retargeting is not provided.",MessageType.Info);
            _asset=(GameObject)EditorGUILayout.ObjectField("Rigged model / visual prefab",_asset,typeof(GameObject),false);
            _identity=EditorGUILayout.TextField("Canonical identity",_identity);
            EditorGUILayout.LabelField("04OH asset proof, rights start unknown",EditorStyles.boldLabel);
            _proof.AssetId=EditorGUILayout.TextField("Production asset ID",_proof.AssetId);
            _proof.Creator=EditorGUILayout.TextField("Actual creator",_proof.Creator);
            _proof.SourceUrl=EditorGUILayout.TextField("Source URL / provenance",_proof.SourceUrl);
            _proof.LicenseId=EditorGUILayout.TextField("License identifier",_proof.LicenseId);
            _proof.RightsRecordId=EditorGUILayout.TextField("Private rights record ID",_proof.RightsRecordId);
            _proof.CommercialUse=EditorGUILayout.Toggle("Commercial game use verified",_proof.CommercialUse);
            _proof.PublicSourceApproved=EditorGUILayout.Toggle("Raw public redistribution allowed",_proof.PublicSourceApproved);
            _proof.OwnerApproved=EditorGUILayout.Toggle("Owner approved candidate appearance",_proof.OwnerApproved);
            _proof.Diagnostic=EditorGUILayout.Toggle("Diagnostic fixture, never activate",_proof.Diagnostic);
            EditorGUILayout.HelpBox("Record an opaque rights ID, never private receipts, account data or credentials. This does not purchase or approve art automatically. New rows stay disabled. Existing-pose adapter is the only implemented bone authority; supplied clips are inventoried, not played.",MessageType.Info);
            _scale=EditorGUILayout.FloatField("Visual scale",_scale);
            _alignBindH3=EditorGUILayout.Toggle("Opt-in humanoid bind-axis alignment",_alignBindH3);
            if(GUILayout.Button("Use detailed human rig (Malik / named NPC)"))HumanTemplate();
            if(GUILayout.Button("Read original rig from selected live NPC, creature or owned drake"))ReadLiveRig();
            if(GUILayout.Button("Suggest Avatar and exact-name bone matches"))Suggest();
            if(GUILayout.Button("Use mapped body points for visual-only locators"))SuggestLocators();
            EditorGUILayout.HelpBox("Review every row. Empty or duplicate targets refuse export. Source bones come from the real runtime rig, including fingers. A match by name is only a suggestion; inspect native grip and deformation.",MessageType.None);
            for(int i=0;i<_names.Length;i++)_paths[i]=BoneField(i+" "+_names[i],_paths[i]);
            EditorGUILayout.Space();EditorGUILayout.LabelField("Visual locators (do not replace authoritative weapon sockets)",EditorStyles.boldLabel);
            for(int i=0;i<Roles.Length;i++)_socketPaths[i]=BoneField(Roles[i],_socketPaths[i]);
            if(GUILayout.Button("Audit model, rig, materials, animation and LOD"))Audit();
            if(GUILayout.Button("Create disabled visual prefab + registry row"))Create();
            if(GUILayout.Button("Save current audit report"))
            {
                string path=EditorUtility.SaveFilePanel("Import audit",Directory.GetParent(Application.dataPath).FullName,"import04of.txt","txt");
                if(path!="")File.WriteAllText(path,_report);
            }
            EditorGUILayout.TextArea(_report,GUILayout.MinHeight(150));
            EditorGUILayout.EndScrollView();
        }
        private string BoneField(string title,string path)
        {
            EditorGUILayout.BeginHorizontal();
            Transform current=_asset!=null && !string.IsNullOrWhiteSpace(path)?_asset.transform.Find(path):null;
            Transform chosen=(Transform)EditorGUILayout.ObjectField(title,current,typeof(Transform),false);
            if(chosen!=current && chosen!=null && _asset!=null && chosen.IsChildOf(_asset.transform))path=AnimationUtility.CalculateTransformPath(chosen,_asset.transform);
            path=EditorGUILayout.TextField(path??"",GUILayout.MinWidth(100));EditorGUILayout.EndHorizontal();return path;
        }
        private void ReadLiveRig()
        {
            GameObject selected=Selection.activeGameObject;
            CharacterBase character=selected!=null?selected.GetComponentInParent<CharacterBase>():null;
            CompanionController companion=selected!=null?selected.GetComponentInParent<CompanionController>():null;
            Transform[] source=null;
            if(companion!=null && companion.Uid>0 && companion.Rig!=null){source=companion.Rig.Bones;_humanSockets=false;_identity="companion:"+companion.Archetype;}
            else if(character!=null)
            {
                _humanSockets=character.Rig!=null;Creature creature=character as Creature;NpcController npc=character as NpcController;
                source=character.Rig!=null?character.Rig.Bones:creature!=null && creature.CRig!=null?creature.CRig.Bones:null;
                _identity=character.IsPlayer?"malik":creature!=null && creature.Species!=null?"creature:"+creature.Species.Id:npc!=null && npc.CreatureSpecies!=null?"creature:"+npc.CreatureSpecies.Id:"npc:"+character.DisplayName;
            }
            if(source==null){_report="Select the actual live actor. A wild drake with no owned UID is not an eligible companion.";return;}
            _names=new string[source.Length];_paths=new string[source.Length];
            for(int i=0;i<source.Length;i++)_names[i]=source[i]!=null?source[i].name:"bone_"+i;
            _report="Original runtime bone order loaded. No ownership, save or actor was changed.";
        }
        private string Normalize(string name)
        {StringBuilder b=new StringBuilder();foreach(char c in name.ToLowerInvariant())if(char.IsLetterOrDigit(c))b.Append(c);return b.ToString();}
        private void Suggest()
        {
            if(_asset==null){_report="ASSET REQUIRED";return;}
            Transform[] targets=_asset.GetComponentsInChildren<Transform>(true);
            Dictionary<string,string> avatarNames=new Dictionary<string,string>();
            ModelImporter importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_asset)) as ModelImporter;
            if(importer!=null && importer.animationType==ModelImporterAnimationType.Human)
                foreach(HumanBone human in importer.humanDescription.human)avatarNames[Normalize(human.humanName)]=human.boneName;
            for(int i=0;i<_names.Length;i++)
            {
                string match="";int count=0;
                string targetName;
                if(!avatarNames.TryGetValue(Normalize(HumanKey(_names[i])),out targetName))targetName=_names[i];
                foreach(Transform t in targets)if(Normalize(t.name)==Normalize(targetName)){match=AnimationUtility.CalculateTransformPath(t,_asset.transform);count++;}
                if(count==1)_paths[i]=match;
            }
            _report="Avatar mapping or unique exact-name suggestions only. Review missing fingers, handedness, bind pose and sockets.";
        }
        private string HumanKey(string source)
        {
            string[] parts=source.Split('.');if(parts.Length==1)return source;
            string side=parts[parts.Length-1]=="L"?"Left":"Right";
            if(parts.Length==3)
            {
                string joint=parts[1]=="1"?"Proximal":parts[1]=="2"?"Intermediate":"Distal";
                return side+parts[0]+joint;
            }
            string bone=parts[0]=="Forearm"?"LowerArm":parts[0]=="Thigh"?"UpperLeg":parts[0]=="Shin"?"LowerLeg":parts[0];
            return side+bone;
        }
        private void SuggestLocators()
        {
            if(!_humanSockets || _paths.Length<RigLayout.BodyBoneCount){_report="For a nonhuman rig, assign head/chest/fx/synergy from the supplied model. These do not replace gameplay hitboxes.";return;}
            int[] anchors={RigLayout.HandR,RigLayout.HandL,RigLayout.HandL,RigLayout.HandR,RigLayout.HandR,RigLayout.HandR,RigLayout.Head,RigLayout.Chest,RigLayout.HandL,RigLayout.Chest};
            for(int i=0;i<anchors.Length;i++)_socketPaths[i]=_paths[anchors[i]];
            _report="Visual body anchors suggested. Muzzle/blade/foregrip authority stays on the original equipped weapon, not these approximate body points. Inspect real contact before enabling.";
        }
        private ImportedBone04OD[] Mapping()
        {ImportedBone04OD[] bones=new ImportedBone04OD[_names.Length];for(int i=0;i<bones.Length;i++)bones[i]=new ImportedBone04OD{SourceBone=i,TargetPath=_paths[i]};return bones;}
        private bool Audit()
        {
            StringBuilder report=new StringBuilder("04OF import audit, "+DateTime.UtcNow.ToString("o")+"\n");int errors=0;
            Action<string> error=message=>{errors++;report.AppendLine("ERROR: "+message);};
            if(_asset==null || !EditorUtility.IsPersistent(_asset)){_report="ASSET REQUIRED: choose a Project model or prefab asset.";return false;}
            string reason;if(!ImportedModelRegistry04OD.ValidMapping(_names.Length,Mapping(),_scale,out reason))error(reason);
            HashSet<Transform> mapped=new HashSet<Transform>();
            foreach(string path in _paths){Transform t=CinematicAcceptance04OF.RelativeBonePath(path)?_asset.transform.Find(path):null;if(t==null)error("Missing/unsafe target: "+path);else mapped.Add(t);}
            foreach(Component component in _asset.GetComponentsInChildren<Component>(true))
                if(component==null || !ImportedArt04OH.VisualComponent(component.GetType()))error("Unsupported visual component before instantiation: "+(component!=null?component.GetType().Name:"missing script"));
            if(_asset.GetComponentsInChildren<Collider>(true).Length>0 || _asset.GetComponentsInChildren<Rigidbody>(true).Length>0 || _asset.GetComponentsInChildren<MonoBehaviour>(true).Length>0)error("Visual must contain no collider, rigidbody or gameplay script. Author a visual-only copy of supplied art.");
            foreach(Transform node in _asset.GetComponentsInChildren<Transform>(true))
                if(!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale))error("Mirrored, collapsed or nonuniform scale at "+node.name+". Apply transforms in your 3D editor and re-export; runtime pose rotation cannot safely repair skin shear.");
            SkinnedMeshRenderer[] skins=_asset.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(skins.Length==0)error("No skinned mesh.");
            foreach(SkinnedMeshRenderer skin in skins)
            {
                if(skin.sharedMesh==null || skin.bones.Length==0 || skin.sharedMesh.bindposes.Length!=skin.bones.Length)error("Missing mesh, skin or bind pose count: "+skin.name);
                if(skin.localBounds.size.y*_scale<.1f || skin.localBounds.size.y*_scale>20)error("Implausible height: "+skin.name);
                bool[] referenced=null;
                try{if(_alignBindH3 && skin.sharedMesh!=null)referenced=ImportedBindH3.ReferencedBones(skin.sharedMesh.boneWeights,skin.bones.Length);}
                catch(Exception ex){error("Cannot inspect candidate weights: "+ex.Message+". Enable read/write and repair the skin before preparing it.");continue;}
                for(int boneIndex=0;boneIndex<skin.bones.Length;boneIndex++)
                {
                    if(referenced!=null && !referenced[boneIndex])continue;
                    Transform bone=skin.bones[boneIndex];
                    if(bone==null){error("Null weighted bone");continue;}
                    if(mapped.Contains(bone))continue;
                    bool ancestor=false;Transform parent=bone.parent;
                    while(parent!=null && parent!=_asset.transform){if(mapped.Contains(parent)){ancestor=true;break;}parent=parent.parent;}
                    if(!ancestor)error("Unmapped weighted bone has no mapped ancestor: "+bone.name);
                }
                foreach(Material material in skin.sharedMaterials)
                {
                    if(material==null || material.shader==null || !material.shader.isSupported)error("Missing/unsupported material: "+skin.name);
                    else report.AppendLine("Material: "+material.name+" / "+material.shader.name+" / normal map "+(material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap")!=null));
                }
                report.AppendLine("Skin: "+skin.name+" / vertices "+(skin.sharedMesh!=null?skin.sharedMesh.vertexCount:0)+" / bones "+skin.bones.Length);
            }
            bool human=_humanSockets;
            for(int i=0;i<Roles.Length;i++)
            {
                if(!human && i<6)continue;
                if(!CinematicAcceptance04OF.RelativeBonePath(_socketPaths[i]) || _asset.transform.Find(_socketPaths[i])==null)error("Missing locator: "+Roles[i]);
            }
            foreach(Animator animator in _asset.GetComponentsInChildren<Animator>(true))
            {
                if(animator.runtimeAnimatorController!=null)error("Remove the Animator controller before copying this visual. State-machine behaviours cannot run beside the original pose owner.");
                report.AppendLine("Animator: "+animator.name+" / valid avatar "+(animator.avatar!=null && animator.avatar.isValid)+". Adapter disables Animator; original procedural pose wins, no root motion.");
            }
            report.AppendLine("Animation clips available: "+AnimationUtility.GetAnimationClips(_asset).Length+" (not automatically retargeted or activated).");
            report.AppendLine("LOD groups: "+_asset.GetComponentsInChildren<LODGroup>(true).Length+". Inspect transitions and all shaders natively.");
            report.AppendLine("Export SHA-256: "+ImportedArtProof04OH.FileHash(AssetDatabase.GetAssetPath(_asset)));
            report.AppendLine("Rights/appearance approval: "+(_proof.CommercialUse && _proof.OwnerApproved?"declared by owner, retain proof":"PENDING")+". Diagnostic: "+_proof.Diagnostic);
            report.AppendLine("Errors: "+errors+". Native deformation, grip, facial channels, animation, LOD and performance NOT VERIFIED.");_report=report.ToString();return errors==0;
        }
        private void Create()
        {
            if(!Audit())return;
            if(string.IsNullOrWhiteSpace(_identity)){_report+="\nERROR: canonical identity required.";return;}
            const string path="Assets/NeonSyndicate/Resources/ImportedModels04OD.asset";
            ImportedModelRegistry04OD registry=AssetDatabase.LoadAssetAtPath<ImportedModelRegistry04OD>(path);
            if(registry==null){registry=CreateInstance<ImportedModelRegistry04OD>();AssetDatabase.CreateAsset(registry,path);}
            List<ImportedModel04OD> models=new List<ImportedModel04OD>(registry.Models??new ImportedModel04OD[0]);
            foreach(ImportedModel04OD existing in models)if(existing!=null && existing.Identity==_identity){_report+="\nREFUSED duplicate identity. Inspect the existing registry row before replacing supplied art.";return;}
            string destination=EditorUtility.SaveFilePanelInProject("Separate visual prefab","Visual04OF","prefab","Save supplied art as a visual-only prefab. Original gameplay objects remain unchanged.");
            if(destination=="")return;
            GameObject copy=Instantiate(_asset);GameObject prefab=null;
            try
            {
                foreach(Animator animator in copy.GetComponentsInChildren<Animator>(true)){animator.enabled=false;animator.applyRootMotion=false;}
                prefab=PrefabUtility.SaveAsPrefabAsset(copy,destination);
            }
            finally{DestroyImmediate(copy);}
            if(prefab==null){_report+="\nERROR: prefab creation failed.";return;}
            List<string> passive=new List<string>();HashSet<string> mapped=new HashSet<string>(_paths);
            foreach(SkinnedMeshRenderer skin in _asset.GetComponentsInChildren<SkinnedMeshRenderer>(true))foreach(Transform bone in skin.bones)
            {string bonePath=AnimationUtility.CalculateTransformPath(bone,_asset.transform);if(!mapped.Contains(bonePath) && !passive.Contains(bonePath))passive.Add(bonePath);}
            List<ImportedSocket04OD> sockets=new List<ImportedSocket04OD>();
            for(int i=0;i<Roles.Length;i++)if(!string.IsNullOrWhiteSpace(_socketPaths[i]))sockets.Add(new ImportedSocket04OD{Role=Roles[i],Path=_socketPaths[i]});
            ImportedArtRecord04OH proof=new ImportedArtRecord04OH{AssetId=_proof.AssetId,Creator=_proof.Creator,SourceUrl=_proof.SourceUrl,LicenseId=_proof.LicenseId,RightsRecordId=_proof.RightsRecordId,
                CommercialUse=_proof.CommercialUse,PublicSourceApproved=_proof.PublicSourceApproved,OwnerApproved=_proof.OwnerApproved,Diagnostic=_proof.Diagnostic};
            ImportedArtProof04OH.Stamp(proof,_asset,prefab);
            models.Add(new ImportedModel04OD{Identity=_identity,Enabled=false,Prefab=prefab,Scale=_scale,AlignBindAxesH3=_alignBindH3,Bones=Mapping(),Sockets=sockets.ToArray(),PassiveBonePaths=passive.ToArray(),ArtProof=proof});
            Undo.RecordObject(registry,"Add disabled visual adapter");registry.Models=models.ToArray();EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();Selection.activeObject=registry;
            _report+="\nCreated DISABLED row. Inspect native poses, material/LOD, equipment, Pause, death/fallback and companion eligibility before manually enabling registry and row. Audit is not native acceptance.";
        }
    }
}
#endif
