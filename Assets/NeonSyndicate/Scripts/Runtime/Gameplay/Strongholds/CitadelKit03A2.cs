using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Per-fort ownership of new meshes, surface clones and procedural maps. Existing materials are never mutated.</summary>
    public sealed class CitadelKit03A2 : IDisposable
    {
        private readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> _maps = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public Mesh Model(string name, bool low)
        {
            string key=name+(low?"_low":"_high");Mesh mesh;
            if(_meshes.TryGetValue(key,out mesh)&&mesh!=null)return mesh;
            mesh=CitadelGeometry03A2.Build(name,low);_meshes[key]=mesh;return mesh;
        }
        public Material Surface(int style,string role,StrongholdAssets original)
        {
            string key=style+"/"+role;Material material;
            if(_materials.TryGetValue(key,out material)&&material!=null)return material;
            string sourceRole=role=="trim"?"stone":(role=="bloom"?"crystal":role);
            Material source=original.Surface(Mathf.Clamp(style,0,2),sourceRole);
            if(source==null)throw new InvalidOperationException("Citadel base surface is missing: "+role);
            material=new Material(source){name="Citadel 03A2 / "+key,enableInstancing=true};
            // Clear inherited glowing window/leaf patterns and metallic maps before setting this surface.
            material.DisableKeyword("_EMISSION");SetColor(material,"_EmissionColor",Color.black);
            SetTexture(material,"_EmissionMap",null);SetTexture(material,"_MetallicGlossMap",null);SetTexture(material,"_OcclusionMap",null);
            material.DisableKeyword("_METALLICGLOSSMAP");material.DisableKeyword("_METALLICSPECGLOSSMAP");material.DisableKeyword("_OCCLUSIONMAP");
            Color tint=Tint(style,role);
            SetColor(material,"_Color",tint);SetColor(material,"_BaseColor",tint);
            bool metal=role=="gilt",stone=role=="stone"||role=="trim";
            float gloss = metal ? .56f : (stone ? .25f : .36f);
            SetFloat(material,"_Metallic",metal ? .7f : 0f);SetFloat(material,"_Glossiness",gloss);SetFloat(material,"_Smoothness",gloss);
            SetFloat(material,"_GlossMapScale",gloss);
            Texture2D albedo=Map(stone?"stone":(role=="cloth"?"cloth":((role=="leaf"||role=="bark")?"leaf":"mineral")));
            SetTexture(material,"_MainTex",albedo);SetTexture(material,"_BaseMap",albedo);
            Vector2 repeat=role=="cloth"?Vector2.one:(stone?new Vector2(.55f,.55f):Vector2.one);
            SetScale(material,"_MainTex",repeat);SetScale(material,"_BaseMap",repeat);
            SetTexture(material,"_BumpMap",stone?Map("normal"):null);
            SetScale(material,"_BumpMap",repeat);
            if(stone){material.EnableKeyword("_NORMALMAP");SetFloat(material,"_BumpScale",.42f);}
            else material.DisableKeyword("_NORMALMAP");
            // Phase 2: citadel stone gets the shared plaster-pore detail maps on High and up.
            if(stone&&Mats.DetailEnabled)Mats.ApplyDetail(material,SurfaceKind.Pores,6f,.4f);
            if(role=="crystal"||role=="bloom")
            {
                material.EnableKeyword("_EMISSION");SetColor(material,"_EmissionColor",tint*(role == "bloom" ? .28f : .20f));
                SetTexture(material,"_EmissionMap",Map("mineral"));
            }
            material.renderQueue=2000;material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            _materials.Add(key,material);return material;
        }
        public static Color Tint(int style,string role)
        {
            if(role=="stone")return style==3?new Color(.21f,.23f,.27f):(style==2?new Color(.48f,.56f,.48f):(style==1?new Color(.86f,.79f,.61f):new Color(.59f,.63f,.76f)));
            if(role=="trim")return style==3?new Color(.43f,.36f,.31f):(style==2?new Color(.72f,.76f,.57f):new Color(.91f,.88f,.77f));
            if(role=="gilt")return style==3?new Color(.67f,.38f,.17f):(style==2?new Color(.66f,.69f,.37f):new Color(.84f,.70f,.38f));
            if(role=="bark")return new Color(.32f,.25f,.19f);
            if(role=="leaf")return style==3?new Color(.23f,.28f,.24f):(style==1?new Color(.37f,.49f,.24f):new Color(.21f,.46f,.34f));
            if(role=="cloth")return style==3?new Color(.38f,.12f,.09f):(style==2?new Color(.13f,.39f,.29f):(style==1?new Color(.58f,.36f,.14f):new Color(.30f,.20f,.49f)));
            return style==3?new Color(1f,.32f,.07f):(style==2?new Color(.19f,.80f,.56f):(style==1?new Color(1f,.73f,.34f):new Color(.47f,.57f,1f)));
        }
        private Texture2D Map(string kind)
        {
            Texture2D texture;if(_maps.TryGetValue(kind,out texture)&&texture!=null)return texture;
            const int n=256;Color32[] pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=x/(float)n,v=y/(float)n;Color c;
                if(kind=="normal")
                {
                    float dx=Height((x+1)%n,y,n)-Height((x+n-1)%n,y,n);
                    float dy=Height(x,(y+1)%n,n)-Height(x,(y+n-1)%n,n);
                    Vector3 normal=new Vector3(-dx*2.2f,-dy*2.2f,1f).normalized;
                    c=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1f);
                }
                else if(kind=="stone")
                {
                    float h=Height(x,y,n),grain=.93f+.07f*Noise(x,y);
                    c=new Color(h*grain,h*grain*.99f,h*grain*.97f,1f);
                }
                else if(kind=="cloth")
                {
                    // Woven cloth and a readable diamond/branch sigil, not a repeating neon grid.
                    float px=Math.Abs(u-.5f),py=Math.Abs(v-.5f);
                    bool border=u<.045f||u>.955f||v<.035f||v>.965f;
                    bool emblem=Math.Abs(px+py*.65f-.22f)<.017f||(px<.018f&&v>.24f&&v<.75f);
                    float light=(border || emblem) ? .97f : (.60f+.09f*((x+y)%2)+.035f*Noise(x,y));
                    c=new Color(light,light,light,1f);
                }
                else if(kind=="leaf")
                {
                    float vein=(float)Math.Pow(Math.Abs(Math.Cos((u+.12f*Math.Sin(v*15f))*22f)),12.0);
                    float light=.65f+.18f*vein+.10f*Noise(x,y);c=new Color(light,light,light,1f);
                }
                else
                {
                    float bands=.74f+.17f*(float)Math.Sin(u*10f+v*7f)+.05f*Noise(x,y);
                    c=new Color(bands,bands,bands,1f);
                }
                pixels[y*n+x]=c;
            }
            texture=new Texture2D(n,n,TextureFormat.RGBA32,true,kind=="normal")
            {name="Citadel 03A2 / "+kind,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=2};
            texture.SetPixels32(pixels);texture.Apply(true,true);_maps[kind]=texture;return texture;
        }
        private static float Height(int x,int y,int size)
        {
            float u=x/(float)size,v=y/(float)size;
            int row=(int)Math.Floor(v*8f);float bx=Repeat(u*4f+(row%2)*.5f),by=Repeat(v*8f);
            float edge=Math.Min(Math.Min(bx,1f-bx),Math.Min(by,1f-by));
            float bevel=Math.Min(1f,edge/.055f);
            return .59f+.28f*bevel+.035f*Noise(x,y);
        }
        private static float Repeat(float f){return f-(float)Math.Floor(f);}
        private static float Noise(int x,int y)
        {
            unchecked{uint h=(uint)(x*374761393+y*668265263+1297);h=(h^(h>>13))*1274126177u;return (h&65535u)/65535f;}
        }
        private static void SetColor(Material m,string key,Color c){if(m.HasProperty(key))m.SetColor(key,c);}
        private static void SetFloat(Material m,string key,float f){if(m.HasProperty(key))m.SetFloat(key,f);}
        private static void SetTexture(Material m,string key,Texture t){if(m.HasProperty(key))m.SetTexture(key,t);}
        private static void SetScale(Material m,string key,Vector2 v){if(m.HasProperty(key))m.SetTextureScale(key,v);}
        public void Dispose()
        {
            foreach(Mesh mesh in _meshes.Values)Release(mesh);
            foreach(Material material in _materials.Values)Release(material);
            foreach(Texture2D texture in _maps.Values)Release(texture);
            _meshes.Clear();_materials.Clear();_maps.Clear();
        }
        private static void Release(UnityEngine.Object obj)
        {if(obj!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}}
    }
}
