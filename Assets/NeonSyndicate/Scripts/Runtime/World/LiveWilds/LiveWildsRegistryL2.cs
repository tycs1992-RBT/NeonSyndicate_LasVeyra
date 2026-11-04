using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class LiveWildsVariantL2 {public string Name;public Mesh[] Lods;}
    [Serializable] public sealed class LiveWildsKitL2 {public WildPieceKind Kind;public Material Material;public LiveWildsVariantL2[] Variants;public bool ReceiptVerified;public int ExpectedVariants;public string Reason;}
    /// <summary>Editor-prepared passive meshes and shared materials included in a build. No filesystem hot reload.</summary>
    public sealed class LiveWildsRegistryL2 : ScriptableObject
    {
        public LiveWildsKitL2[] Kits;
        public string Preparation,Fingerprint;
        private static LiveWildsRegistryL2 _loaded;
        private static bool _tried;
        public static void ResetStatics(){_loaded=null;_tried=false;LiveWildsRulesL2.ResetStatics();}
        public static LiveWildsRegistryL2 Loaded
        {
            get
            {
                if(!_tried){_tried=true;_loaded=Resources.Load<LiveWildsRegistryL2>("LiveWildsPrepared/RedspireCatalog");if(_loaded!=null)_loaded.Check();}
                return _loaded;
            }
        }
        private void Check()
        {
            if(Kits==null)return;
            HashSet<WildPieceKind> seen=new HashSet<WildPieceKind>();
            foreach(LiveWildsKitL2 kit in Kits)
            {
                if(kit==null)continue;
                if(!seen.Add(kit.Kind) || !kit.ReceiptVerified || LiveWildsRulesL2.Slot(kit.Kind)<0 || kit.Material==null || kit.Variants==null || kit.Variants.Length<2 || kit.Variants.Length>8){kit.Reason="Incomplete or duplicate prepared kit, using procedural fallback.";kit.ReceiptVerified=false;continue;}
                foreach(LiveWildsVariantL2 v in kit.Variants)
                {
                    if(v==null || v.Lods==null || v.Lods.Length!=3){kit.ReceiptVerified=false;break;}
                    int previous=int.MaxValue;
                    foreach(Mesh mesh in v.Lods)
                    {
                        if(mesh==null || !mesh.isReadable || mesh.subMeshCount!=1 || mesh.vertexCount==0 || mesh.GetIndexCount(0)/3>LiveWildsRulesL2.MaxTriangles || mesh.GetIndexCount(0)/3>=previous){kit.ReceiptVerified=false;break;}
                        previous=(int)mesh.GetIndexCount(0)/3;
                        foreach(Vector3 p in mesh.vertices)if(!MathUtil.IsFinite(p) || Mathf.Abs(p.x)>.501f || Mathf.Abs(p.y)>.501f || Mathf.Abs(p.z)>.501f){kit.ReceiptVerified=false;break;}
                    }
                }
                if(!kit.ReceiptVerified)kit.Reason="Invalid native mesh or LOD, using procedural fallback.";
            }
        }
        public LiveWildsKitL2 Find(WildPieceKind kind)
        {if(Kits!=null)foreach(LiveWildsKitL2 k in Kits)if(k!=null && k.Kind==kind && k.ReceiptVerified)return k;return null;}
        /// <summary>Retain each refused family independently so a partial import is visible in F2.</summary>
        public string Detail(WildPieceKind kind)
        {
            if(Kits!=null)foreach(LiveWildsKitL2 k in Kits)if(k!=null && k.Kind==kind)
                return kind+": "+(k.ReceiptVerified && k.Variants!=null?k.Variants.Length+" variants, 3 LODs":"procedural fallback")+". "+k.Reason;
            return kind+": no prepared entry, procedural fallback.";
        }
        public string Status
        {
            get
            {
                int good=0,bad=0,variants=0,rejected=0;string reason="";
                if(Kits!=null)foreach(LiveWildsKitL2 k in Kits)
                {
                    if(k!=null && k.ReceiptVerified){good++;variants+=k.Variants!=null?k.Variants.Length:0;}
                    else{bad++;if(k!=null){rejected+=k.ExpectedVariants;if(reason=="")reason=k.Reason;}}
                }
                return good+" prepared kits / "+variants+" valid variants, "+bad+" refused kits / "+rejected+" refused variants. "+(reason!=""?reason+". ":"")+Preparation;
            }
        }
    }
    public sealed class LiveWildsMetricsL2
    {
        public int Eligible,Excluded,Boulders,Spires,Trees,Batches,Vertices;
        public string Reason="";
        public int Replaced {get{return Boulders+Spires+Trees;}}
        public int Fallback {get{return Eligible-Replaced;}}
        public void Add(LiveWildsMetricsL2 other)
        {if(other==null)return;Eligible+=other.Eligible;Excluded+=other.Excluded;Boulders+=other.Boulders;Spires+=other.Spires;Trees+=other.Trees;Batches+=other.Batches;Vertices+=other.Vertices;if(Reason=="" && other.Reason!="")Reason=other.Reason;}
    }
}
