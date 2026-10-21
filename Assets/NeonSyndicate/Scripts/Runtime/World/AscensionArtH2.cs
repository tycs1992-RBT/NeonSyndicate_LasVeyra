using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Original fixed art streams through the existing material and city chunk owners. No gameplay authority.</summary>
    public static class AscensionArtH2
    {
        private const string Root="AscensionH2/";
        public static readonly string[] Models={"portal_bay","flying_buttress","ribbed_vault","cluster_column","slate_roof","reliquary_door","tracery_wall","gallery_rail","processional_stair","mineral_pier","broken_apse","forge_bay","basalt_strata","root_causeway","cavern_shell","fern_colony","crystal_seam","castellan_mantle","record_lens"};
        private static readonly Dictionary<string,MeshBuilder> Sources=new Dictionary<string,MeshBuilder>(StringComparer.Ordinal);
        private static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>(StringComparer.Ordinal);
        private static bool _ready,_failed;
        public static void ResetStatics()
        {
            foreach(Material material in Materials.Values)
                if(material!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);}
            Sources.Clear();Materials.Clear();_ready=false;_failed=false;
        }
        public static string Role(string model)
        {
            switch(model)
            {
                case "slate_roof":return "slate";
                case "mineral_pier":return "amethyst";
                case "basalt_strata":case "cavern_shell":return "basalt";
                case "root_causeway":return "root";
                case "fern_colony":return "leaf";
                case "crystal_seam":return "azure";
                case "record_lens":case "castellan_mantle":return "iron";
                default:return "masonry";
            }
        }
        public static bool HasCollision(string model){return model!="fern_colony" && model!="record_lens" && model!="castellan_mantle";}
        /// <summary>Engine-independent CMH1 validation shared by runtime and real-file regression tests.</summary>
        public static MeshBuilder Decode(byte[] data)
        {
            if(data==null || data.Length<8 || data[0]!='C' || data[1]!='M' || data[2]!='H' || data[3]!='1')throw new InvalidDataException("H2 mesh header invalid.");
            int count=BitConverter.ToInt32(data,4);
            if(count<3 || count>90000 || count%3!=0 || data.LongLength!=8L+count*32L)throw new InvalidDataException("H2 mesh length invalid.");
            MeshBuilder mesh=new MeshBuilder(1);
            using(BinaryReader reader=new BinaryReader(new MemoryStream(data,false)))
            {
                reader.BaseStream.Position=8;
                for(int i=0;i<count;i+=3)
                {
                    Vector3[] p=new Vector3[3],n=new Vector3[3];Vector2[] uv=new Vector2[3];
                    for(int j=0;j<3;j++)
                    {
                        p[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                        n[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[j]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                        if(!MathUtil.IsFinite(p[j]) || !MathUtil.IsFinite(n[j]) || n[j].sqrMagnitude<.95f || n[j].sqrMagnitude>1.05f || !MathUtil.IsFinite(new Vector3(uv[j].x,uv[j].y,0)))throw new InvalidDataException("H2 invalid mesh value.");
                    }
                    if(Vector3.Cross(p[1]-p[0],p[2]-p[0]).sqrMagnitude<1e-12f)throw new InvalidDataException("H2 degenerate triangle.");
                    int start=mesh.VertexCount;mesh.Triangle(0,p[0],p[1],p[2],n[0],uv[0],uv[1],uv[2]);mesh.Normals[start+1]=n[1];mesh.Normals[start+2]=n[2];
                }
            }
            return mesh;
        }
        public static MeshBuilder Source(string model,bool low)
        {
            if(Array.IndexOf(Models,model)<0)throw new ArgumentException("Unknown H2 model.",nameof(model));
            string key=model+(low?"_low":"_high");MeshBuilder result;
            if(Sources.TryGetValue(key,out result))return result;
            TextAsset asset=Resources.Load<TextAsset>(Root+"Meshes/"+key);
            if(asset==null)throw new FileNotFoundException("Missing H2 mesh "+key);
            result=Decode(asset.bytes);Sources.Add(key,result);return result;
        }
        public static void Append(MeshBuilder target,MeshBuilder source,Vector3 at,float yaw,Vector3 scale)
        {
            if(!MathUtil.IsFinite(at) || !MathUtil.IsFinite(scale) || scale.x<=0 || scale.y<=0 || scale.z<=0)throw new ArgumentOutOfRangeException(nameof(scale));
            Quaternion rotation=QMath.AY(yaw);
            for(int i=0;i<source.VertexCount;i+=3)
            {
                Vector3[] p=new Vector3[3],n=new Vector3[3];
                for(int j=0;j<3;j++)
                {
                    Vector3 v=source.Vertices[i+j],normal=source.Normals[i+j];
                    p[j]=at+rotation*Vector3.Scale(v,scale);n[j]=(rotation*new Vector3(normal.x/scale.x,normal.y/scale.y,normal.z/scale.z)).normalized;
                }
                int start=target.VertexCount;target.Triangle(0,p[0],p[1],p[2],n[0],source.UVs[i],source.UVs[i+1],source.UVs[i+2]);target.Normals[start+1]=n[1];target.Normals[start+2]=n[2];
            }
        }
        public static MeshBuilder MirrorX(MeshBuilder source)
        {
            MeshBuilder result=new MeshBuilder(1);
            for(int i=0;i<source.VertexCount;i+=3)
            {
                int[] order={i,i+2,i+1};Vector3[] p=new Vector3[3],n=new Vector3[3];
                for(int j=0;j<3;j++){p[j]=source.Vertices[order[j]];p[j].x=-p[j].x;n[j]=source.Normals[order[j]];n[j].x=-n[j].x;}
                int start=result.VertexCount;result.Triangle(0,p[0],p[1],p[2],n[0],source.UVs[order[0]],source.UVs[order[1]],source.UVs[order[2]]);result.Normals[start+1]=n[1];result.Normals[start+2]=n[2];
            }
            return result;
        }
        /// <summary>Replaces only the visual of the original physical investigation prop, never its interaction or receipt.</summary>
        public static bool RegionalRecord(QuestProp prop,Vector3 ground,string kind)
        {
            CityLayout layout=Game.Layout;
            if(layout==null || !layout.ArtRevolutionH2 || kind!="Crystal")return false;
            bool oath=(ground-layout.OathSeal04OG).sqrMagnitude<.25f || (ground-layout.OathWitness04OG).sqrMagnitude<.25f || (layout.GothicHeartH3 && (ground-layout.HearthLensH3).sqrMagnitude<.25f);
            bool storm=(ground-layout.StormSpring04OG).sqrMagnitude<.25f || (ground-layout.StormVein04OG).sqrMagnitude<.25f;
            if((!oath && !storm) || !Ready())return false;
            GameObject lens=new GameObject("H2 physical mineral record lens");lens.layer=Layers.IgnoreRaycast;lens.transform.SetParent(prop.transform,false);
            lens.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(lens,Source("record_lens",false).ToMesh(lens.name));
            lens.AddComponent<MeshRenderer>().sharedMaterial=Surface("iron");
            GameObject mineral=new GameObject("H2 mineral trace");mineral.layer=Layers.IgnoreRaycast;mineral.transform.SetParent(lens.transform,false);
            mineral.transform.localPosition=new Vector3(0,1.12f,0);mineral.transform.localScale=new Vector3(.22f,.13f,.22f);
            mineral.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(mineral,Source("mineral_pier",true).ToMesh(mineral.name));
            mineral.AddComponent<MeshRenderer>().sharedMaterial=Surface(oath?"amethyst":"azure");return true;
        }
        private static Texture2D Texture(string role,string kind)
        {
            Texture2D value=Resources.Load<Texture2D>(Root+"Textures/"+role+"_"+kind);
            if(value==null)throw new FileNotFoundException("Missing H2 texture "+role+"_"+kind);return value;
        }
        public static Material Surface(string role)
        {
            bool heart=Game.Layout!=null && Game.Layout.GothicHeartH3;
            bool civic=Game.Layout!=null && Game.Layout.GothicCivicH5;
            string key=role+(civic?"/H5":heart?"/H3":"");
            Material result;if(Materials.TryGetValue(key,out result))return result;
            Shader shader=Mats.StandardShader;if(shader==null)throw new InvalidOperationException("Existing Lit shader missing.");
            Texture2D albedo=Texture(role,"Albedo"),normal=Texture(role,"Normal"),mask=Texture(role,"Mask");
            result=new Material(shader){name="Ascension H2 / "+role,enableInstancing=true};
            result.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",albedo);result.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",Color.white);
            result.SetTexture("_BumpMap",normal);result.SetFloat("_BumpScale",.75f);result.EnableKeyword("_NORMALMAP");
            result.SetTexture("_MetallicGlossMap",mask);result.SetTexture("_OcclusionMap",mask);result.SetFloat("_OcclusionStrength",.8f);
            result.SetFloat("_Metallic",role=="iron"?.75f:0);result.SetFloat(RenderPath.IsURP?"_Smoothness":"_GlossMapScale",1);
            result.EnableKeyword(RenderPath.IsURP?"_METALLICSPECGLOSSMAP":"_METALLICGLOSSMAP");result.EnableKeyword("_OCCLUSIONMAP");
            if(role=="amethyst" || role=="azure")
            {
                result.SetTexture("_EmissionMap",albedo);result.SetColor("_EmissionColor",role=="azure"?new Color(.07f,.23f,.29f):new Color(.20f,.07f,.29f));result.EnableKeyword("_EMISSION");
            }
            if(heart && (role=="masonry" || role=="basalt"))
            {
                // Lawful CC0 material pixels, kept separate from every old revision's source assets.
                Texture2D actualAlbedo=Resources.Load<Texture2D>("AscensionH3/Textures/"+role+"_Albedo");
                Texture2D actualNormal=Resources.Load<Texture2D>("AscensionH3/Textures/"+role+"_Normal");
                Texture2D actualMask=Resources.Load<Texture2D>("AscensionH3/Textures/"+role+"_Mask");
                if(actualAlbedo!=null && actualNormal!=null && actualMask!=null)
                {
                    result.name="Gothic Heart H3 / "+role;
                    result.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",actualAlbedo);
                    result.SetTexture("_BumpMap",actualNormal);result.SetTexture("_MetallicGlossMap",actualMask);result.SetTexture("_OcclusionMap",actualMask);
                    result.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",role=="masonry"?new Color(.52f,.48f,.55f):new Color(.72f,.82f,.8f));
                    result.SetFloat("_BumpScale",.65f);
                }
                else Log.Warn("H3 material maps missing; retaining the original H2 material for "+role);
            }
            if(civic && role=="iron")
            {
                Texture2D a=Resources.Load<Texture2D>("AscensionH5/Textures/iron_Albedo"),n=Resources.Load<Texture2D>("AscensionH5/Textures/iron_Normal"),m=Resources.Load<Texture2D>("AscensionH5/Textures/iron_Mask");
                if(a!=null && n!=null && m!=null)
                {
                    result.name="Gothic Civic H5 / weathered iron";result.SetTexture(RenderPath.IsURP?"_BaseMap":"_MainTex",a);
                    result.SetTexture("_BumpMap",n);result.SetTexture("_MetallicGlossMap",m);result.SetTexture("_OcclusionMap",m);result.SetFloat("_Metallic",1);
                }
                else Log.Warn("H5 metal maps unavailable; retaining original iron.");
            }
            Materials.Add(key,result);return result;
        }
        /// <summary>Resolve the whole candidate before suppressing any fallback. One warning, no broken empty corridor.</summary>
        public static bool Ready()
        {
            if(_ready)return true;if(_failed)return false;
            try{foreach(string model in Models){Source(model,false);Source(model,true);Surface(Role(model));}_ready=true;return true;}
            catch(Exception ex){_failed=true;Log.Warn("H2 art unavailable, keeping H1 structures: "+ex.Message);return false;}
        }
    }
}
