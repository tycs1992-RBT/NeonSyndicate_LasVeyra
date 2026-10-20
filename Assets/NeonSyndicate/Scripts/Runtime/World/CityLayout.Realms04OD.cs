using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Explicit additive profile. No original service, receipt or revision-3 endpoint moves.</summary>
    public sealed partial class CityLayout
    {
        public bool Realms04OD { get { return Revision04O==CityRevision04O.CrystalRealms04OD || Unbound04OE; } }
        public Vector3 CastellanCourt04OD {get;private set;}=new Vector3(-210f,CurbHeight,28f);
        public Vector3 SovereignCourt04OD {get;private set;}=new Vector3(-270f,CurbHeight,158f);
        private Vector3 SafeCourt04OD(Vector3 original,bool azure)
        {
            for(int step=0;step<=96;step+=8)for(int direction=0;direction<(step==0?1:8);direction++)
            {
                Vector3 candidate=original+MathUtil.DirFromYaw(direction*45f)*step;
                Vector3 branch=candidate+Vector3.forward*(azure?18f:-18f);
                if(IsInsideBuilding(candidate,20f) || candidate.x> -160f || !ClearPassage04O(candidate,branch,1.8f))continue;
                return candidate;
            }
            throw new System.InvalidOperationException("04OD cannot admit a safe regional court for seed "+Seed+". Earlier banks remain unchanged.");
        }
        /// <summary>Bounded physical detours around protected original owners. Geometry and nav consume these same points.</summary>
        private Vector3[] RegionalPath04OD(Vector3 from,Vector3 to)
        {
            if(ClearPassage04O(from,to,2.2f))return new[]{from,to};
            const float cell=4f;float x0=Mathf.Min(from.x,to.x)-64f,z0=Mathf.Min(from.z,to.z)-64f;
            int width=Mathf.CeilToInt((Mathf.Abs(from.x-to.x)+128f)/cell)+1;
            int depth=Mathf.CeilToInt((Mathf.Abs(from.z-to.z)+128f)/cell)+1;
            System.Func<int,Vector3> point=id=>new Vector3(x0+id%width*cell,CurbHeight,z0+id/width*cell);
            int start=Mathf.RoundToInt((from.z-z0)/cell)*width+Mathf.RoundToInt((from.x-x0)/cell);
            int goal=Mathf.RoundToInt((to.z-z0)/cell)*width+Mathf.RoundToInt((to.x-x0)/cell);
            Dictionary<int,float> costs=new Dictionary<int,float>();Dictionary<int,int> previous=new Dictionary<int,int>();
            SortedSet<System.Tuple<float,int>> open=new SortedSet<System.Tuple<float,int>>();HashSet<int> closed=new HashSet<int>();
            costs[start]=0;open.Add(System.Tuple.Create(Vector3.Distance(from,to),start));int expanded=0;
            while(open.Count>0 && expanded++<12000)
            {
                System.Tuple<float,int> first=open.Min;open.Remove(first);int current=first.Item2;if(!closed.Add(current))continue;
                if(current==goal)
                {
                    List<Vector3> path=new List<Vector3>{to};int cursor=goal;
                    while(cursor!=start){path.Add(point(cursor));cursor=previous[cursor];}path.Add(point(start));path.Add(from);path.Reverse();
                    // Simplify only when the full swept clearance is legal, preserving real detour length.
                    for(int i=0;i<path.Count-2;i++)for(int j=path.Count-1;j>i+1;j--)
                        if(ClearPassage04O(path[i],path[j],2.2f)){path.RemoveRange(i+1,j-i-1);break;}
                    return path.ToArray();
                }
                int cx=current%width,cz=current/width;
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
                {
                    if(dx==0 && dz==0)continue;int nx=cx+dx,nz=cz+dz;if(nx<0 || nz<0 || nx>=width || nz>=depth)continue;
                    int next=nz*width+nx;if(closed.Contains(next) || !ClearPassage04O(point(current),point(next),2.2f))continue;
                    float cost=costs[current]+(dx==0 || dz==0?cell:cell*1.41421356f),old;
                    if(costs.TryGetValue(next,out old) && old<=cost)continue;
                    costs[next]=cost;previous[next]=current;open.Add(System.Tuple.Create(cost+Vector3.Distance(point(next),to),next));
                }
            }
            throw new System.InvalidOperationException("04OD bounded regional path failed for seed "+Seed+"; protected geometry was retained.");
        }
        private bool ApplyCrystalRealms04OD(IReadOnlyList<VenueBinding04C> venues)
        {
            if(!ApplyFracturedWilds04OC(venues))return false;
            Vector3 oldCastellan=RealmChallenge04OC,oldSovereign=AzureGlade04OC;
            CastellanCourt04OD=SafeCourt04OD(oldCastellan,false);SovereignCourt04OD=SafeCourt04OD(oldSovereign,true);
            Revision04O=CityRevision04O.CrystalRealms04OD;
            RealmRoutes04OB.RemoveAll(r=>r.Id.StartsWith("neighbor-path/") || r.Id=="amethyst-challenge" || r.Id=="azure-expedition" || r.Id=="azure-return");
            AddRealmRoute04OB("amethyst-challenge",7f,RegionalPath04OD(RealmThreshold04OB,CastellanCourt04OD));
            AddRealmRoute04OB("azure-expedition",6f,RegionalPath04OD(CastellanCourt04OD,SovereignCourt04OD));
            AddRealmRoute04OB("azure-return",5f,RegionalPath04OD(SovereignCourt04OD,RealmGrotto04OB+Vector3.forward*11f));
            AddRealmRoute04OB("dominion-gallery",5f,RealmChallenge04OC,RealmChallenge04OC+Vector3.back*18f);
            AddRealmRoute04OB("storm-shrine",5f,AzureGlade04OC,AzureGlade04OC+Vector3.forward*18f);
            // New routes participate in terrain clearance and nav before any scene mesh is generated.
            WildScenery04OC.RemoveAll(s=>IntersectsWildRoute04OC(new RectF(s.Position.x-s.Size.x*.55f,s.Position.z-s.Size.z*.55f,s.Position.x+s.Size.x*.55f,s.Position.z+s.Size.z*.55f),1.8f));
            BuildSpatialHashes();ReauthorWalks04OB(venues);BuildSpatialHashes();return true;
        }
    }
}
