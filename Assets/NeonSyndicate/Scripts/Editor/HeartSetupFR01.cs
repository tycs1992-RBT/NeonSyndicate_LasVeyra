#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace NeonSyndicate.Editor
{
    /// <summary>Validated passive art preparation inside the existing LiveWorld lifecycle.</summary>
    public static class HeartSetupFR01
    {
        private const string Root="Assets/NeonSyndicate/Art/HeartFR01",Prepared="Assets/NeonSyndicate/Resources/HeartPreparedFR01";
        [Serializable] private sealed class Manifest { public string license="";public Row[] modules=Array.Empty<Row>();public Row[] textures=Array.Empty<Row>(); }
        [Serializable] private sealed class Row { public string name="",path="",sha256="";public int lod=0;public long bytes=0; }
        public static void ResetStatics() { }
        [MenuItem("Neon Syndicate/Live World/Prepare Gothic Heart Renaissance",false,3306)]
        public static void Menu(){Prepare(true);}
        public static void Prepare(bool verbose)
        {
            HeartCatalogFR01 catalog=AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(Prepared+"/Catalog.asset");
            try
            {
                if(EditorApplication.isPlaying)return;
                string json=File.ReadAllText(Root+"/manifest.json");Manifest receipt=JsonUtility.FromJson<Manifest>(json);
                if(receipt==null || receipt.license!="CC0-1.0" || receipt.modules==null || receipt.modules.Length!=33 || receipt.textures==null || receipt.textures.Length!=15)throw new InvalidDataException("FR01 source receipt missing or incomplete.");
                foreach(Row row in receipt.modules)Verify(row);foreach(Row row in receipt.textures)Verify(row);
                string fingerprint=Hash(System.Text.Encoding.UTF8.GetBytes("FR01E bounds and LOD schema 1\n"+json+RenderPath.IsURP));
                if(catalog!=null && catalog.Fingerprint==fingerprint && catalog.Ready)return;
                Ensure(Prepared);
                string[] names={"Masonry","CarvedTrim","SlateTimber","MossRoot","AmethystBronze"};Dictionary<string,Material> surfaces=new Dictionary<string,Material>();
                foreach(string name in names)
                {
                    string path=Prepared+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    Shader shader=Shader.Find(RenderPath.IsURP?"Universal Render Pipeline/Lit":"Standard");if(shader==null)throw new InvalidDataException("Supported lit shader absent.");
                    if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
                    Texture2D[] maps=new Texture2D[3];string[] channels={"BaseColor","Normal","MetallicSmoothness"};
                    for(int i=0;i<3;i++)
                    {
                        string p=Root+"/"+name+"_"+channels[i]+".png";TextureImporter t=AssetImporter.GetAtPath(p)as TextureImporter;
                        if(t==null)throw new InvalidDataException("Texture import unavailable: "+p);
                        bool normal=i==1;if(t.sRGBTexture!=(i==0) || t.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default) || t.textureShape!=TextureImporterShape.Texture2D || t.maxTextureSize!=1024)
                        {t.sRGBTexture=i==0;t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.maxTextureSize=1024;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.SaveAndReimport();}
                        maps[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(p);if(maps[i]==null)throw new InvalidDataException("Texture2D missing: "+p);
                    }
                    m.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",maps[0]);m.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",Color.white);
                    m.SetTexture("_BumpMap",maps[1]);m.EnableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",maps[2]);m.EnableKeyword(RenderPath.IsURP?"_METALLICSPECGLOSSMAP":"_METALLICGLOSSMAP");m.enableInstancing=true;EditorUtility.SetDirty(m);surfaces.Add(name,m);
                }
                Dictionary<string,List<Row>> groups=new Dictionary<string,List<Row>>();foreach(Row row in receipt.modules){if(!groups.ContainsKey(row.name))groups.Add(row.name,new List<Row>());groups[row.name].Add(row);}
                List<HeartModuleFR01> modules=new List<HeartModuleFR01>();
                foreach(KeyValuePair<string,List<Row>> pair in groups)
                {
                    pair.Value.Sort((a,b)=>a.lod.CompareTo(b.lod));GameObject[] lods=new GameObject[3];Vector3 size=Vector3.zero,center=Vector3.zero;
                    if(pair.Value.Count!=3)throw new InvalidDataException("Missing LOD: "+pair.Key);
                    for(int i=0;i<3;i++)
                    {
                        Row row=pair.Value[i];ModelImporter importer=AssetImporter.GetAtPath(row.path)as ModelImporter;if(importer==null)throw new InvalidDataException("FBX not imported.");
                        if(importer.animationType!=ModelImporterAnimationType.None || importer.importAnimation || importer.importCameras || importer.importLights || importer.materialLocation!=ModelImporterMaterialLocation.InPrefab || !importer.isReadable)
                        {importer.animationType=ModelImporterAnimationType.None;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportViaMaterialDescription;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();}
                        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(row.path);if(source==null)throw new InvalidDataException("FBX model absent.");
                        GameObject copy=UnityEngine.Object.Instantiate(source);
                        try
                        {
                            foreach(Component c in copy.GetComponentsInChildren<Component>(true))if(!(c is Transform || c is MeshFilter || c is MeshRenderer))throw new InvalidDataException("FR01 model is not passive.");
                            foreach(MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                Material[] slots=renderer.sharedMaterials;for(int n=0;n<slots.Length;n++){string key=slots[n]!=null?slots[n].name:"";if(!surfaces.ContainsKey(key))throw new InvalidDataException("Unknown material "+key);slots[n]=surfaces[key];}renderer.sharedMaterials=slots;
                            }
                            Bounds b=default(Bounds);bool found=false;foreach(MeshFilter f in copy.GetComponentsInChildren<MeshFilter>(true))
                            {
                                Mesh mesh=f.sharedMesh;if(mesh==null || mesh.vertexCount<3 || mesh.uv.Length!=mesh.vertexCount || mesh.normals.Length!=mesh.vertexCount)throw new InvalidDataException("Missing mesh, UV or normals.");
                                foreach(Vector3 p in mesh.bounds.GetCornersFR01()) {Vector3 q=copy.transform.InverseTransformPoint(f.transform.TransformPoint(p));if(!found){b=new Bounds(q,Vector3.zero);found=true;}else b.Encapsulate(q);}
                            }
                            if(!found || b.size.y<.001f)throw new InvalidDataException("Degenerate model bounds.");
                            if(i==0){size=b.size;center=b.center;}
                            lods[i]=PrefabUtility.SaveAsPrefabAsset(copy,Prepared+"/"+pair.Key+"_LOD"+i+".prefab");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(copy); }
                    }
                    modules.Add(new HeartModuleFR01{Id=pair.Key,Lods=lods,Size=size,Center=center});
                }
                // Reuse the delivered H8R tree mesh directly. The source and importer metadata are never edited here.
                GameObject elder=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonSyndicate/Art/WorldFoundationH8R/ShardwoodElder.fbx");
                if(elder!=null)
                {
                    GameObject copy=UnityEngine.Object.Instantiate(elder);
                    try
                    {
                        foreach(Component c in copy.GetComponentsInChildren<Component>(true))if(!(c is Transform || c is MeshFilter || c is MeshRenderer))UnityEngine.Object.DestroyImmediate(c);
                        foreach(MeshRenderer r in copy.GetComponentsInChildren<MeshRenderer>(true)){Material[] slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=surfaces[slots[i]!=null && slots[i].name.ToLowerInvariant().Contains("leaf")?"MossRoot":"SlateTimber"];r.sharedMaterials=slots;}
                        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(copy,Prepared+"/ReuseShardwoodElder.prefab");modules.Add(new HeartModuleFR01{Id="ReuseShardwoodElder",Lods=new[]{prefab},Size=Vector3.one});
                    }
                    finally { UnityEngine.Object.DestroyImmediate(copy); }
                }
                if(catalog==null){catalog=ScriptableObject.CreateInstance<HeartCatalogFR01>();AssetDatabase.CreateAsset(catalog,Prepared+"/Catalog.asset");}
                catalog.Modules=modules.ToArray();catalog.Fingerprint=fingerprint;catalog.Reason="FR01 original architecture prepared, native review pending.";EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();if(verbose)Log.Info("FR01 "+catalog.Reason);
            }
            catch(Exception e){if(catalog!=null){catalog.Modules=null;catalog.Fingerprint="";catalog.Reason=e.Message;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();}Log.Warn("FR01 original fallback: "+e.Message);}
        }
        private static void Verify(Row row)
        {if(row==null || !row.path.StartsWith(Root+"/",StringComparison.Ordinal) || row.path.Contains("..") || !File.Exists(row.path) || new FileInfo(row.path).Length!=row.bytes || Hash(File.ReadAllBytes(row.path))!=row.sha256)throw new InvalidDataException("FR01 source bytes missing or mismatched; hydrate LFS.");}
        private static string Hash(byte[] data){using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        private static void Ensure(string p){if(AssetDatabase.IsValidFolder(p))return;string parent=p.Substring(0,p.LastIndexOf('/'));Ensure(parent);AssetDatabase.CreateFolder(parent,p.Substring(parent.Length+1));}
        private static IEnumerable<Vector3> GetCornersFR01(this Bounds b)
        {for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return b.center+Vector3.Scale(b.extents,new Vector3(x,y,z));}
    }
}
#endif
