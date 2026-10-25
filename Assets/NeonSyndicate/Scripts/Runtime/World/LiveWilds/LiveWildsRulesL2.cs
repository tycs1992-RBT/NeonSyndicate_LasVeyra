using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Pure admission and deterministic selection. The original plan and collision remain authoritative.</summary>
    public static class LiveWildsRulesL2
    {
        public const int MaxVariants=8,MaxTriangles=3000;
        public static void ResetStatics() { }
        public static int Slot(WildPieceKind kind)
        {return kind==WildPieceKind.Boulder?0:kind==WildPieceKind.RockSpire?1:kind==WildPieceKind.DeadTree?2:-1;}
        public static bool Eligible(WildPiece p)
        {
            return p.Site<0 && p.Biome==WildBiome.HoodooBadlands && Slot(p.Kind)>=0 && MathUtil.IsFinite(p.Position) && MathUtil.IsFinite(p.Size) && p.Size.x>0 && p.Size.y>0 && p.Size.z>0 && !float.IsNaN(p.Yaw) && !float.IsInfinity(p.Yaw);
        }
        public static int Variant(WildPiece p,int count)
        {
            if(!Eligible(p) || count<1 || count>MaxVariants)return -1;
            unchecked {uint h=(uint)p.Seed ^ ((uint)p.Kind+1)*0x9e3779b9u ^ ((uint)p.Biome+1)*0x85ebca6bu;h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;h^=h>>16;return (int)(h%(uint)count);}
        }
        public static bool Frame(WildPiece p,IList<Vector3> legacy,out Vector3 center,out Vector3 size)
        {
            center=Vector3.zero;size=Vector3.zero;if(!Eligible(p) || legacy==null || legacy.Count==0)return false;
            Vector3 min=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),max=-min;Quaternion q=QMath.AY(-p.Yaw);
            foreach(Vector3 point in legacy){Vector3 local=q*(point-p.Position);if(!MathUtil.IsFinite(local))return false;min=Vector3.Min(min,local);max=Vector3.Max(max,local);}
            size=max-min;center=p.Position+QMath.AY(p.Yaw)*((min+max)*.5f);
            return size.x>.01f && size.y>.01f && size.z>.01f;
        }
        public static bool Kit(string text,string kind,out string material,out string[] variants,out string reason)
        {
            material="";variants=null;reason="Kit requires version 1, Redspire, original collision and two to eight named variants.";
            bool version=false,biome=false,collision=false;List<string> names=new List<string>();
            foreach(string raw in (text??"").Split('\n'))
            {
                string line=raw.Trim();if(line.Length==0 || line.StartsWith("#",StringComparison.Ordinal))continue;
                string[] words=line.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(words.Length!=2)return false;
                if(words[0]=="version" && words[1]=="1" && !version)version=true;
                else if(words[0]=="biome" && words[1]=="HoodooBadlands" && !biome)biome=true;
                else if(words[0]=="collision" && words[1]=="original" && !collision)collision=true;
                else if(words[0]=="material" && material=="" && (words[1]=="RedspireSandstone" || words[1]=="RedspireBark"))material=words[1];
                else if(words[0]=="variant" && words[1].StartsWith(kind+"_",StringComparison.Ordinal) && !names.Contains(words[1]) && SafeName(words[1]))names.Add(words[1]);
                else return false;
            }
            if(!version || !biome || !collision || material=="" || names.Count<2 || names.Count>MaxVariants)return false;
            variants=names.ToArray();reason="";return true;
        }
        /// <summary>Find a source-plan viewpoint beside an existing mixed Redspire cluster. Excluded pieces cannot own it.</summary>
        public static bool Walk(WildsPlan plan,bool[] installed,HashSet<int> excluded,out Vector3 at,out string label)
        {
            at=Vector3.zero;label="No safe mixed Redspire cluster.";if(plan==null || installed==null || installed.Length!=3)return false;int best=-1;
            foreach(WildSite site in plan.Sites)
            {
                if(site.Biome!=WildBiome.HoodooBadlands || (site.Kind!=WildSiteKind.HoodooMaze && site.Kind!=WildSiteKind.Watchtower))continue;
                for(int i=0;i<plan.Pieces.Count;i++)
                {
                    WildPiece p=plan.Pieces[i];if(!Eligible(p) || !installed[Slot(p.Kind)] || excluded!=null && excluded.Contains(i) || MathUtil.FlatSqrDistance(p.Position,site.Center)>240*240)continue;
                    Vector3 min,max;if(!LiveWildRulesL1.PieceBounds(p,out min,out max))continue;Vector3 view=new Vector3(max.x+7,p.Position.y,max.z+7);
                    if(!WildsLayout.InWilds(view.x,view.z))continue;bool safe=true;int score=0,mask=0;
                    for(int j=0;j<plan.Pieces.Count;j++)
                    {
                        WildPiece other=plan.Pieces[j];if(excluded!=null && excluded.Contains(j))continue;
                        if(Eligible(other) && installed[Slot(other.Kind)] && MathUtil.FlatSqrDistance(other.Position,p.Position)<90*90){score+=other.Kind==WildPieceKind.RockSpire?3:1;mask|=1<<Slot(other.Kind);}
                        if(!other.Solid || MathUtil.FlatSqrDistance(other.Position,view)>Mathf.Pow(Mathf.Max(other.Size.x,other.Size.z)*3+6,2))continue;
                        Vector3 a,b;if(LiveWildRulesL1.PieceBounds(other,out a,out b) && view.x>a.x-2 && view.x<b.x+2 && view.z>a.z-2 && view.z<b.z+2){safe=false;break;}
                    }
                    if(safe && score>best && mask==7){best=score;at=view;label="Redspire art walk near "+site.Name;}
                }
            }
            return best>=0;
        }
        private static bool SafeName(string name)
        {foreach(char c in name)if(!char.IsLetterOrDigit(c) && c!='_')return false;return true;}
    }
}
