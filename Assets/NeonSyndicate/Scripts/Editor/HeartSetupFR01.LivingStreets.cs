#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    public static partial class HeartSetupFR01
    {
        private const string LivingArt="Assets/NeonSyndicate/Art/HeartFR02A",LivingPrepared="Assets/NeonSyndicate/Resources/HeartPreparedFR02A";
        [MenuItem("Neon Syndicate/Live World/Prepare Living Gothic Streets",false,3307)]
        public static void MenuLiving(){Prepare(true);PrepareLiving(true);}
        /// <summary>Independent generated catalog, shared original FR01 surfaces, no donor or source-metadata rewrite.</summary>
        public static void PrepareLiving(bool verbose)
        {
            if(EditorApplication.isPlaying)return;
            HeartCatalogFR01 catalog=AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(LivingPrepared+"/Catalog.asset");
            try
            {
                string json=File.ReadAllText(LivingArt+"/manifest.json");Manifest receipt=JsonUtility.FromJson<Manifest>(json);
                if(receipt==null || receipt.license!="CC0-1.0" || receipt.modules==null || receipt.modules.Length!=39 || receipt.textures==null || receipt.textures.Length!=15)throw new InvalidDataException("Living Streets source receipt incomplete.");
                foreach(Row r in receipt.modules)
                    if(r==null || string.IsNullOrEmpty(r.path) || !r.path.StartsWith(LivingArt+"/",StringComparison.Ordinal) || r.path.Contains("..") || !File.Exists(r.path) || new FileInfo(r.path).Length!=r.bytes || Hash(File.ReadAllBytes(r.path))!=r.sha256)throw new InvalidDataException("Living Streets source missing or changed: "+(r!=null?r.path:"missing receipt row")+". Hydrate LFS.");
                foreach(Row r in receipt.textures)Verify(r);
                HeartCatalogFR01 foundation=AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(Prepared+"/Catalog.asset");
                if(foundation==null || !foundation.Ready)throw new InvalidDataException("Prepare the original Gothic Heart surfaces first.");
                string stamp=PreparationStamp("FR02A material dependencies 2",json,receipt,foundation.Fingerprint);
                if(catalog!=null && catalog.Fingerprint==stamp && catalog.LivingReady && PreparedSurfacesReady(catalog))return;
                Ensure(LivingPrepared);Dictionary<string,Material> surfaces=new Dictionary<string,Material>();
                foreach(string name in new[]{"Masonry","CarvedTrim","SlateTimber","MossRoot","AmethystBronze"})
                {
                    Material m=AssetDatabase.LoadAssetAtPath<Material>(Prepared+"/"+name+".mat");
                    if(m==null || m.mainTexture==null || m.shader==null || !m.shader.isSupported || !m.HasProperty("_BumpMap") || m.GetTexture("_BumpMap")==null || !m.HasProperty("_MetallicGlossMap") || m.GetTexture("_MetallicGlossMap")==null)throw new InvalidDataException("Original shared surface or required map unavailable: "+name);
                    surfaces.Add(name,m);
                }
                Dictionary<string,List<Row>> groups=new Dictionary<string,List<Row>>();
                foreach(Row r in receipt.modules){if(!groups.ContainsKey(r.name))groups.Add(r.name,new List<Row>());groups[r.name].Add(r);}
                List<HeartModuleFR01> modules=new List<HeartModuleFR01>();
                foreach(KeyValuePair<string,List<Row>> group in groups)
                {
                    if(group.Value.Count!=3)throw new InvalidDataException("Three LODs required: "+group.Key);
                    group.Value.Sort((a,b)=>a.lod.CompareTo(b.lod));GameObject[] levels=new GameObject[3];Vector3 size=Vector3.zero,center=Vector3.zero;
                    for(int level=0;level<3;level++)
                    {
                        Row row=group.Value[level];if(row.lod!=level)throw new InvalidDataException("Duplicate LOD.");
                        ModelImporter importer=AssetImporter.GetAtPath(row.path)as ModelImporter;if(importer==null)throw new InvalidDataException("Wait for FBX import: "+row.path);
                        if(importer.importAnimation || importer.animationType!=ModelImporterAnimationType.None || !importer.isReadable || importer.importCameras || importer.importLights || importer.addCollider || importer.materialImportMode!=ModelImporterMaterialImportMode.ImportViaMaterialDescription || importer.materialLocation!=ModelImporterMaterialLocation.InPrefab)
                        {importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportViaMaterialDescription;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();}
                        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(row.path);if(source==null)throw new InvalidDataException("Missing imported model: "+row.path);
                        if(source.GetComponentsInChildren<MeshRenderer>(true).Length==0)throw new InvalidDataException("Living Streets model has no renderer: "+row.path);
                        // Inspect before Instantiate, so no unexpected Awake can execute during preparation.
                        foreach(Component c in source.GetComponentsInChildren<Component>(true))if(c==null || !(c is Transform || c is MeshFilter || c is MeshRenderer))throw new InvalidDataException("Nonpassive Living Streets model: "+row.path);
                        GameObject copy=UnityEngine.Object.Instantiate(source);
                        try
                        {
                            Bounds bounds=default(Bounds);bool found=false;
                            foreach(MeshFilter f in copy.GetComponentsInChildren<MeshFilter>(true))
                            {
                                Mesh mesh=f.sharedMesh;if(mesh==null || mesh.vertexCount<3 || mesh.uv.Length!=mesh.vertexCount || mesh.normals.Length!=mesh.vertexCount)throw new InvalidDataException("Missing mesh, UV or normals: "+row.path);
                                foreach(Vector3 p in mesh.bounds.GetCornersFR01()){Vector3 q=copy.transform.InverseTransformPoint(f.transform.TransformPoint(p));if(!found){bounds=new Bounds(q,Vector3.zero);found=true;}else bounds.Encapsulate(q);}
                            }
                            Vector3 allowed=HeartPlanFR01.LivingSize(group.Key);
                            if(!found || bounds.size.x>allowed.x || bounds.size.y>allowed.y || bounds.size.z>allowed.z || bounds.size.y<.01f)throw new InvalidDataException("Model exceeds its conservative reservation: "+group.Key);
                            foreach(MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                Material[] slots=renderer.sharedMaterials;if(slots.Length==0)throw new InvalidDataException("Living Streets material slots missing: "+row.path);for(int i=0;i<slots.Length;i++)
                                {string name=slots[i]!=null?slots[i].name:"";if(!surfaces.ContainsKey(name))throw new InvalidDataException("Unmapped original surface: "+name+" in "+row.path);slots[i]=surfaces[name];}
                                renderer.sharedMaterials=slots;
                            }
                            if(level==0){size=bounds.size;center=bounds.center;}
                            levels[level]=PrefabUtility.SaveAsPrefabAsset(copy,LivingPrepared+"/"+group.Key+"_LOD"+level+".prefab");
                            if(levels[level]==null)throw new InvalidDataException("Prepared prefab could not be saved: "+row.path);
                        }
                        finally{UnityEngine.Object.DestroyImmediate(copy);}
                    }
                    modules.Add(new HeartModuleFR01{Id=group.Key,Lods=levels,Size=size,Center=center});
                }
                if(catalog==null){catalog=ScriptableObject.CreateInstance<HeartCatalogFR01>();AssetDatabase.CreateAsset(catalog,LivingPrepared+"/Catalog.asset");}
                catalog.Modules=modules.ToArray();if(!PreparedSurfacesReady(catalog))throw new InvalidDataException("FR02A generated catalog contains an invalid mesh, renderer, shader or material map.");catalog.Fingerprint=PreparationStamp("FR02A material dependencies 2",json,receipt,foundation.Fingerprint);catalog.Reason="Living Streets source prepared, native review pending.";EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();if(verbose)Log.Info("FR02A "+catalog.Reason);
            }
            catch(Exception e){if(catalog!=null){catalog.Modules=null;catalog.Fingerprint="";catalog.Reason=e.Message;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();}Log.Warn("FR02A original world retained: "+e.Message);}
        }
    }
}
#endif
