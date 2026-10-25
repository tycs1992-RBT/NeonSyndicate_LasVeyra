#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Called by the existing LiveWorld preparation lifecycle. Creates a build-ready, passive Redspire kit.</summary>
    public static class LiveWildsSetupL2
    {
        public const string Root="Assets/NeonSyndicate/Art/LiveWilds",Prepared="Assets/NeonSyndicate/Resources/LiveWildsPrepared";
        private const string Manifest="Docs/LiveWildL2/ASSET_MANIFEST_L2.json";
        [Serializable] private sealed class Receipt {public string license;public Entry[] files;}
        [Serializable] private sealed class Entry {public string path,sha256;public long bytes;}
        public static void ResetStatics() { }
        [MenuItem("Neon Syndicate/Live World/Prepare Redspire wild art",false,3304)]
        public static void Menu(){Prepare(true);}
        public static void Prepare(bool verbose)
        {
            if(EditorApplication.isPlaying)return;
            try
            {
                string fingerprint=Fingerprint();LiveWildsRegistryL2 prior=AssetDatabase.LoadAssetAtPath<LiveWildsRegistryL2>(Prepared+"/RedspireCatalog.asset");
                if(prior!=null && prior.Fingerprint==fingerprint && Complete(prior)){if(verbose)Log.Info("LIVE_WILDS "+prior.Status);return;}
                Receipt receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Manifest));
                if(receipt==null || receipt.license!="CC0-1.0" || receipt.files==null || receipt.files.Length<25)throw new InvalidDataException("Missing original-art receipt.");
                foreach(Entry e in receipt.files)
                {
                    if(e==null || !CinematicAcceptance04OF.RelativeBonePath(e.path) || !(e.path.StartsWith(Root+"/",StringComparison.Ordinal) || e.path.StartsWith("ArtSource/RedspireL2/",StringComparison.Ordinal)) || !File.Exists(e.path) || new FileInfo(e.path).Length!=e.bytes || Hash(e.path)!=e.sha256)throw new InvalidDataException("Asset receipt mismatch, pull real LFS bytes: "+(e!=null?e.path:"missing"));
                }
                Ensure(Prepared);List<LiveWildsKitL2> kits=new List<LiveWildsKitL2>();
                foreach(string kind in new[]{"Boulder","RockSpire","DeadTree"})
                {
                    LiveWildsKitL2 row=new LiveWildsKitL2{Kind=(WildPieceKind)Enum.Parse(typeof(WildPieceKind),kind)};kits.Add(row);
                    try
                    {
                        string folder=Root+"/"+kind,mat;string[] names;string reason;
                        if(!File.Exists(folder+"/CREDITS.txt") || !File.ReadAllText(folder+"/CREDITS.txt").Contains("CC0-1.0") || !LiveWildsRulesL2.Kit(File.ReadAllText(folder+"/kit.txt"),kind,out mat,out names,out reason))throw new InvalidDataException("Invalid kit grammar or credits.");
                        row.ExpectedVariants=names.Length;row.Material=Material(mat);List<LiveWildsVariantL2> variants=new List<LiveWildsVariantL2>();
                        foreach(string name in names)
                        {
                            LiveWildsVariantL2 variant=new LiveWildsVariantL2{Name=name,Lods=new Mesh[3]};
                            for(int lod=0;lod<3;lod++)variant.Lods[lod]=Model(folder+"/"+name+"_LOD"+lod+".fbx",name+"_LOD"+lod);
                            variants.Add(variant);
                        }
                        row.Variants=variants.ToArray();row.ReceiptVerified=true;row.Reason="Original candidate, native playtest pending.";
                    }
                    catch(Exception e){row.ReceiptVerified=false;row.Reason=e.Message;Log.Warn("LIVE_WILDS "+kind+" fallback: "+e.Message);}
                }
                LiveWildsRegistryL2 catalog=prior;
                if(catalog==null){catalog=ScriptableObject.CreateInstance<LiveWildsRegistryL2>();AssetDatabase.CreateAsset(catalog,Prepared+"/RedspireCatalog.asset");}
                catalog.Kits=kits.ToArray();catalog.Fingerprint=Fingerprint();catalog.Preparation="Candidate only; L1 and L2 native acceptance pending.";EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Log.Info("LIVE_WILDS "+catalog.Status+" Choose the Redspire kit in Pause > EXPLORE.");
            }
            catch(Exception e)
            {
                // Invalidate stale preparation when rights or source bytes change; never keep an old approved-looking kit.
                LiveWildsRegistryL2 stale=AssetDatabase.LoadAssetAtPath<LiveWildsRegistryL2>(Prepared+"/RedspireCatalog.asset");
                if(stale!=null){stale.Kits=new LiveWildsKitL2[0];stale.Preparation=e.Message;stale.Fingerprint="";EditorUtility.SetDirty(stale);AssetDatabase.SaveAssets();}
                Log.Warn("LIVE_WILDS procedural fallback: "+e.Message);
            }
        }
        private static bool Complete(LiveWildsRegistryL2 catalog)
        {
            if(catalog.Kits==null || catalog.Kits.Length!=3)return false;
            foreach(LiveWildsKitL2 k in catalog.Kits)
            {
                if(k==null || !k.ReceiptVerified || k.Material==null || k.Variants==null || k.Variants.Length<2)return false;
                foreach(LiveWildsVariantL2 v in k.Variants){if(v==null || v.Lods==null || v.Lods.Length!=3)return false;foreach(Mesh m in v.Lods)if(m==null)return false;}
            }
            return true;
        }
        private static Mesh Model(string path,string name)
        {
            ModelImporter importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new InvalidDataException("Native model import missing: "+path);
            if(!importer.isReadable || importer.importAnimation || importer.addCollider || importer.importCameras || importer.importLights || importer.animationType!=ModelImporterAnimationType.None)
            {importer.isReadable=true;importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.addCollider=false;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();}
            GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new InvalidDataException("Native FBX did not load.");
            foreach(Component c in model.GetComponentsInChildren<Component>(true))if(c==null || !(c is Transform || c is MeshFilter || c is MeshRenderer))throw new InvalidDataException("Unsupported model component: "+(c!=null?c.GetType().Name:"missing"));
            MeshFilter[] filters=model.GetComponentsInChildren<MeshFilter>(true);if(filters.Length!=1 || filters[0].sharedMesh==null)throw new InvalidDataException("Expected one passive source mesh.");
            Mesh original=filters[0].sharedMesh;if(!original.isReadable || original.subMeshCount!=1 || original.vertexCount<4 || original.GetIndexCount(0)/3>3000)throw new InvalidDataException("Mesh/material budget or readability failed.");
            Vector3[] points=original.vertices;Vector3 min=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),max=-min;
            // Include the imported root axis/unit conversion, just as an instantiated FBX does.
            Matrix4x4 matrix=filters[0].transform.localToWorldMatrix;
            for(int i=0;i<points.Length;i++){points[i]=matrix.MultiplyPoint3x4(points[i]);if(!MathUtil.IsFinite(points[i]))throw new InvalidDataException("Nonfinite source vertex.");min=Vector3.Min(min,points[i]);max=Vector3.Max(max,points[i]);}
            Vector3 size=max-min;if(size.x<.02f || size.y<.02f || size.z<.02f || Mathf.Max(size.x,Mathf.Max(size.y,size.z))>35)throw new InvalidDataException("Unexpected source metres or collapsed geometry.");
            Vector3 center=(min+max)*.5f;for(int i=0;i<points.Length;i++)points[i]=new Vector3((points[i].x-center.x)/size.x,(points[i].y-center.y)/size.y,(points[i].z-center.z)/size.z);
            if(original.uv==null || original.uv.Length!=points.Length)throw new InvalidDataException("Missing original UVs.");
            Mesh result=new Mesh{name=name};result.vertices=points;result.uv=original.uv;int[] triangles=original.triangles;if(matrix.determinant<0)for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}result.triangles=triangles;result.RecalculateNormals();result.RecalculateTangents();result.RecalculateBounds();
            string target=Prepared+"/"+name+".asset";Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(target);
            if(existing!=null){EditorUtility.CopySerialized(result,existing);UnityEngine.Object.DestroyImmediate(result);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(result,target);return result;
        }
        private static Material Material(string name)
        {
            string role=name=="RedspireBark"?"Bark":"Sandstone";Shader shader=Mats.StandardShader;if(shader==null)throw new InvalidDataException("Supported Lit shader missing.");
            string target=Prepared+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(target);bool created=m==null;
            if(created)m=new Material(shader){name=name};else m.shader=shader;
            m.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",Color.white);m.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",Texture(role,"BaseColor",false,true));m.SetTexture("_BumpMap",Texture(role,"Normal",true,false));m.SetFloat("_BumpScale",.55f);m.EnableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",Texture(role,"MetallicSmoothness",false,false));m.SetFloat("_Metallic",0);m.SetFloat(RenderPath.IsURP?"_Smoothness":"_GlossMapScale",.6f);m.EnableKeyword("_METALLICGLOSSMAP");m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");
            if(created)AssetDatabase.CreateAsset(m,target);else EditorUtility.SetDirty(m);return m;
        }
        private static Texture2D Texture(string role,string channel,bool normal,bool srgb)
        {
            string path=Root+"/Shared/"+role+"_"+channel+".png";TextureImporter t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)throw new InvalidDataException("Texture not imported: "+path);
            TextureImporterType type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(t.textureType!=type || t.sRGBTexture!=srgb || t.textureShape!=TextureImporterShape.Texture2D){t.textureType=type;t.sRGBTexture=srgb;t.textureShape=TextureImporterShape.Texture2D;t.SaveAndReimport();}
            Texture2D result=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(result==null)throw new InvalidDataException("Texture load failed.");return result;
        }
        private static void Ensure(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=path.Substring(0,path.LastIndexOf('/'));Ensure(parent);AssetDatabase.CreateFolder(parent,path.Substring(parent.Length+1));}
        private static string Fingerprint()
        {
            List<string> files=new List<string>(Directory.GetFiles(Root,"*",SearchOption.AllDirectories));files.Sort(StringComparer.Ordinal);StringBuilder s=new StringBuilder(RenderPath.IsURP?"l2-root-frame-v1|urp":"l2-root-frame-v1|builtin");s.Append(Hash(Manifest));
            foreach(string f in files)if(!f.EndsWith(".meta",StringComparison.Ordinal)){s.Append(f).Append(Hash(f));if(f.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))s.Append(AssetDatabase.GetAssetDependencyHash(f.Replace('\\','/')));}
            using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(s.ToString()))).Replace("-","").ToLowerInvariant();
        }
        private static string Hash(string file)
        {using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
