using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Explicit additive profile. No original service, receipt or revision-3 endpoint moves.</summary>
    public sealed partial class CityLayout
    {
        public bool Realms04OD { get { return Revision04O==CityRevision04O.CrystalRealms04OD; } }
        public Vector3 CastellanCourt04OD {get;private set;}=new Vector3(-210f,CurbHeight,28f);
        public Vector3 SovereignCourt04OD {get;private set;}=new Vector3(-270f,CurbHeight,158f);
        private Vector3 SafeCourt04OD(Vector3 original,bool azure)
        {
            Vector3 before=azure?new Vector3(-270,CurbHeight,102):new Vector3(-158,CurbHeight,28);
            Vector3 after=azure?new Vector3(-168,CurbHeight,150):new Vector3(-250,CurbHeight,62);
            for(int step=0;step<=96;step+=8)for(int direction=0;direction<(step==0?1:8);direction++)
            {
                Vector3 candidate=original+MathUtil.DirFromYaw(direction*45f)*step;
                Vector3 branch=candidate+Vector3.forward*(azure?18f:-18f);
                if(IsInsideBuilding(candidate,20f) || !ClearPassage04O(before,candidate,1.8f) || !ClearPassage04O(candidate,after,1.8f) || !ClearPassage04O(candidate,branch,1.8f))continue;
                return candidate;
            }
            throw new System.InvalidOperationException("04OD cannot admit a safe regional court for seed "+Seed+". Earlier banks remain unchanged.");
        }
        private bool ApplyCrystalRealms04OD(IReadOnlyList<VenueBinding04C> venues)
        {
            if(!ApplyFracturedWilds04OC(venues))return false;
            Vector3 oldCastellan=RealmChallenge04OC,oldSovereign=AzureGlade04OC;
            CastellanCourt04OD=SafeCourt04OD(oldCastellan,false);SovereignCourt04OD=SafeCourt04OD(oldSovereign,true);
            Revision04O=CityRevision04O.CrystalRealms04OD;
            foreach(RealmRoute04OB route in RealmRoutes04OB)for(int i=0;i<route.Points.Count;i++)
            {
                if(MathUtil.FlatDistance(route.Points[i],oldCastellan)<.1f)route.Points[i]=CastellanCourt04OD;
                if(MathUtil.FlatDistance(route.Points[i],oldSovereign)<.1f)route.Points[i]=SovereignCourt04OD;
            }
            AddRealmRoute04OB("dominion-gallery",5f,RealmChallenge04OC,RealmChallenge04OC+Vector3.back*18f);
            AddRealmRoute04OB("storm-shrine",5f,AzureGlade04OC,AzureGlade04OC+Vector3.forward*18f);
            // New routes participate in terrain clearance and nav before any scene mesh is generated.
            WildScenery04OC.RemoveAll(s=>IntersectsWildRoute04OC(new RectF(s.Position.x-s.Size.x*.55f,s.Position.z-s.Size.z*.55f,s.Position.x+s.Size.x*.55f,s.Position.z+s.Size.z*.55f),1.8f));
            BuildSpatialHashes();ReauthorWalks04OB(venues);BuildSpatialHashes();return true;
        }
    }
}
