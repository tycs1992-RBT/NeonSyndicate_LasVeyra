using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Bounded shared materials on the established Built-in/URP bank. No new render pipeline.</summary>
    public static partial class GothicRealmMaterials04OB
    {
        private static readonly Dictionary<string,Material> Cache=new Dictionary<string,Material>();
        public static Material For(string role)
        {
            string key=role=="crystal"||role=="glass"?"amethyst":role=="teal"?"teal":role=="metal"||role=="trim"||role=="roof"?"metal":role=="timber"?"timber":role=="linen"||role=="cloth"?"cloth":role=="lamp"?"lamp":role=="paving"?"paving":"stone";
            Material material;if(Cache.TryGetValue(key,out material) && material!=null)return material;
            Material source=key=="timber"||key=="cloth"||key=="lamp"?ArtInhabitedCity04C.Surface(0,key=="cloth"?"linen":key):VisualRebirth04AAssets.Material(0,key=="amethyst"||key=="teal"?"crystal":key=="paving"?"stone":key);
            material=new Material(source);material.name="04O-B / "+key;
            Color color=key=="amethyst"?new Color(.32f,.12f,.52f):key=="teal"?new Color(.08f,.44f,.48f):key=="stone"?new Color(.23f,.24f,.29f):key=="metal"?new Color(.38f,.34f,.31f):key=="paving"?new Color(.38f,.4f,.43f):key=="cloth"?new Color(.43f,.32f,.27f):key=="lamp"?new Color(1f,.67f,.3f):new Color(.38f,.28f,.2f);
            if(material.HasProperty("_Color"))material.SetColor("_Color",color);
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            bool crystal=key=="amethyst"||key=="teal";
            if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",crystal?color*.22f:key=="lamp"?color*.8f:Color.black);
            if(crystal||key=="lamp")material.EnableKeyword("_EMISSION");else material.DisableKeyword("_EMISSION");
            float gloss=crystal?.62f:key=="metal"?.38f:key=="paving"?.25f:.18f;
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",gloss);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",gloss);
            ApplySurface04OB(material,key);
            Cache[key]=material;return material;
        }
        public static void ResetStatics(){foreach(Material material in Cache.Values)ArtInhabitedCity04C.Release(material);Cache.Clear();foreach(Texture2D texture in GeneratedSurfaces04OB)ArtInhabitedCity04C.Release(texture);GeneratedSurfaces04OB.Clear();}
    }
}
