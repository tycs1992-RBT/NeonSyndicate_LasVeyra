using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class VisualSlice04OHTests
    {
        [Test] public void SliceEvidencePreservesBothEarlierChecklistsAndAppendsActualH1Observations()
        {
            string[] earlier=CinematicAcceptance04OF.Checklist04OH(),slice=CinematicAcceptance04OF.ChecklistSlice04OH();
            Assert.That(slice.Length,Is.EqualTo(32));for(int i=0;i<earlier.Length;i++)Assert.That(slice[i],Is.EqualTo(earlier[i]));
            Assert.That(CinematicAcceptance04OF.ProfileWarning(7),Does.Contain("Revision 7"));
        }
        private static CityLayout World(int seed,int revision)
        {
            CityLayout world=new CityLayout(seed);world.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O((CityRevision04O)revision,venues),Is.True);return world;
        }
        private static List<int> Triangles(MeshBuilder mesh)
        {return ((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mesh))[0];}
        private static bool Hit(Vector3 from,Vector3 to,Vector3 a,Vector3 b,Vector3 c)
        {
            // Broad-phase rejection is also necessary for almost parallel rays at large world coordinates.
            // Otherwise a tiny float determinant can invent a contact with a floor entirely below the segment.
            for(int axis=0;axis<3;axis++)
                if(Mathf.Min(from[axis],to[axis])>Mathf.Max(a[axis],Mathf.Max(b[axis],c[axis]))+.0001f ||
                    Mathf.Max(from[axis],to[axis])<Mathf.Min(a[axis],Mathf.Min(b[axis],c[axis]))-.0001f)return false;
            // Evaluate in double precision, including subtraction, to handle near-parallel faces in large-world coordinates.
            double dx=(double)to.x-from.x,dy=(double)to.y-from.y,dz=(double)to.z-from.z;
            double ax=(double)b.x-a.x,ay=(double)b.y-a.y,az=(double)b.z-a.z;
            double bx=(double)c.x-a.x,by=(double)c.y-a.y,bz=(double)c.z-a.z;
            double px=dy*bz-dz*by,py=dz*bx-dx*bz,pz=dx*by-dy*bx,det=ax*px+ay*py+az*pz;
            if(Math.Abs(det)<1e-10)return false;
            double tx=(double)from.x-a.x,ty=(double)from.y-a.y,tz=(double)from.z-a.z,inv=1/det;
            double u=(tx*px+ty*py+tz*pz)*inv;if(u<-.0001 || u>1.0001)return false;
            double qx=ty*az-tz*ay,qy=tz*ax-tx*az,qz=tx*ay-ty*ax;
            double v=(dx*qx+dy*qy+dz*qz)*inv;if(v<-.0001 || u+v>1.0001)return false;
            double distance=(bx*qx+by*qy+bz*qz)*inv;return distance>.0000001 && distance<.9999999;
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void NewStructuresHaveFloorsClearBodyAndCompanionRoutesAndKeepExistingAnchors(int seed)
        {
            CityLayout prior=World(seed,6),world=World(seed,7);
            Assert.That(prior.Ascension04OH,Is.False);Assert.That(world.Ascension04OH,Is.True);
            Assert.That(world.CreatureSpots,Is.EqualTo(prior.CreatureSpots));Assert.That(world.CrystalShardSpots,Is.EqualTo(prior.CrystalShardSpots));
            foreach(RealmRoute04OB route in prior.RealmRoutes04OB.Where(r=>!r.Id.StartsWith("neighbor-path/")))
                Assert.That(world.RealmRoutes04OB.Any(current=>current.Id==route.Id && current.Points.SequenceEqual(route.Points)),Is.True,"Preserve authored "+route.Id);
            Assert.That(prior.RealmRoutes04OB.Count-world.RealmRoutes04OB.Count,Is.EqualTo(world.RejectedNeighbors04OH));
            if(seed==1 || seed==2016)Assert.That(world.RejectedNeighbors04OH,Is.GreaterThan(0),"Reject demonstrated unsafe generated shortcuts");
            MeshBuilder mesh=new MeshBuilder(1);List<RealmRoute04OB> routes=world.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/")).ToList();
            foreach(RealmRoute04OB route in routes)AscensionGeometry04OH.Build(mesh,mesh,mesh,route);
            List<int> triangles=Triangles(mesh);Assert.That(mesh.Vertices.All(MathUtil.IsFinite),Is.True);
            Assert.That(mesh.VertexCount,Is.LessThan(90000),"Bound the whole new structural kit, not a frame-rate claim");
            foreach(RealmRoute04OB route in routes)for(int span=1;span<route.Points.Count;span++)
            {
                Vector3 a=route.Points[span-1],b=route.Points[span],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                for(int lane=-1;lane<=1;lane++)
                {
                    for(int step=0;step<=12;step++)
                    {
                        Vector3 at=Vector3.Lerp(a,b,step/12f)+side*lane*.95f;bool floor=false;
                        for(int t=0;t<triangles.Count;t+=3)
                        {
                            Vector3 x=mesh.Vertices[triangles[t]],y=mesh.Vertices[triangles[t+1]],z=mesh.Vertices[triangles[t+2]];
                            if(Vector3.Cross(y-x,z-x).y>.0001f && Hit(at+Vector3.up*.08f,at-Vector3.up*.03f,x,y,z)){floor=true;break;}
                        }
                        Assert.That(floor,Is.True,route.Id+" floor "+span+"/"+step+" lane "+lane);
                    }
                    foreach(float height in new[]{.45f,1.2f,1.9f,2.6f})
                        for(int t=0;t<triangles.Count;t+=3)
                            if(Hit(a+side*lane*.95f+Vector3.up*height,b+side*lane*.95f+Vector3.up*height,mesh.Vertices[triangles[t]],mesh.Vertices[triangles[t+1]],mesh.Vertices[triangles[t+2]]))
                                Assert.Fail(route.Id+" clearance "+span+" height "+height+" triangle "+t);
                }
            }
            // New meshes must also leave the inherited regional paths clear, including the access routes used by old quests.
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>!r.Id.StartsWith("04og/")))
                for(int span=1;span<route.Points.Count;span++)
                {
                    Vector3 from=route.Points[span-1]+Vector3.up*1.2f,to=route.Points[span]+Vector3.up*1.2f;
                    for(int t=0;t<triangles.Count;t+=3)
                        if(Hit(from,to,mesh.Vertices[triangles[t]],mesh.Vertices[triangles[t+1]],mesh.Vertices[triangles[t+2]]))Assert.Fail(route.Id+" inherited path intrusion triangle "+t+" from "+from+" to "+to+" vertices "+mesh.Vertices[triangles[t]]+" / "+mesh.Vertices[triangles[t+1]]+" / "+mesh.Vertices[triangles[t+2]]);
                }
            foreach(RealmRoute04OB route in routes.Where(r=>r.Id=="04og/oath-witness" || r.Id=="04og/storm-vein" || r.Id=="04og/oath-loop"))
            {
                int from=route.Id.EndsWith("loop")?1:0;Vector3 a=route.Points[from],b=route.Points[from+1],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector3 at=(a+b)*.5f+side*sign*3.6f;bool floor=false;
                    for(int t=0;t<triangles.Count;t+=3)
                    {
                        Vector3 x=mesh.Vertices[triangles[t]],y=mesh.Vertices[triangles[t+1]],z=mesh.Vertices[triangles[t+2]];
                        if(Vector3.Cross(y-x,z-x).y>.0001f && Hit(at+Vector3.up*.08f,at-Vector3.up*.03f,x,y,z))floor=true;
                        if(Hit(at+Vector3.up*.10f,at+Vector3.up*2.1f,x,y,z))Assert.Fail(route.Id+" adjacent bay headroom");
                    }
                    Assert.That(floor,Is.True,route.Id+" real adjacent exploration floor");
                }
            }
            foreach(string token in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"})
            {
                Vector3 before,after;Assert.That(prior.ResolveRealmAnchor04OC(token,out before),Is.True);Assert.That(world.ResolveRealmAnchor04OC(token,out after),Is.True);
                Assert.That(after,Is.EqualTo(before));Assert.That(world.FindWalkRoute04O(world.FindPoi("Veyra Lofts").Position,after).Count,Is.GreaterThan(1));
            }
        }
        [Test] public void ArtBankIsIsolatedAndQuestEligibilityDoesNotImportReceipts()
        {
            Assert.That(SaveSystem.SlotFileName04OB(2,7),Is.EqualTo("lasveyra_ascension04oh_save_2.json"));
            for(int revision=0;revision<=6;revision++)
            {Assert.That(SaveSystem.BankMatches04OC(revision,7),Is.False);Assert.That(SaveSystem.BankMatches04OC(7,revision),Is.False);}
            Assert.That(SaveSystem.BankMatches04OC(7,7),Is.True);Assert.That(new SaveData().WorldRevision04O,Is.Zero);
            QuestRequirements requirements=new QuestRequirements{WorldRevisions=new List<int>{6}};
            Assert.That(QuestLogic.WorldMatches(requirements,7),Is.True);Assert.That(QuestLogic.WorldMatches(requirements,5),Is.False);
            requirements.WorldRevisions=new List<int>{7};Assert.That(QuestLogic.WorldMatches(requirements,6),Is.False);
            Assert.That(CityRebirthRules04O.Known(99),Is.False);Assert.Throws<ArgumentOutOfRangeException>(()=>SaveSystem.SlotFileName04OB(0,99));
            Assert.That(WorldIdentity04OB.Profile((CityRevision04O)7),Does.Contain("Ascension"));
        }
        [TestCase(1)] [TestCase(2)] public void HeroSkinRetainsAllBonesExpressionsAndWeightedCoatFlexion(int detail)
        {
            Appearance hero=Appearance.Malik();RigLayout rig=RigLayout.From(hero);BodyData skin=BodyBuilder.Build(hero,rig,detail);
            Assert.That(rig.World.Length,Is.EqualTo(47));Assert.That(hero.HairStyle,Is.EqualTo(HairStyle.Undercut));Assert.That(hero.Beard,Is.True);
            Assert.That(skin.Vertices.Count,Is.LessThanOrEqualTo(detail==2?13500:8200));
            int coat=0;for(int i=0;i<skin.Vertices.Count;i++)
            {
                Vector3 v=skin.Vertices[i]/rig.Scale;BoneWeight w=skin.Weights[i];
                Assert.That(MathUtil.IsFinite(v),Is.True);Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.0001));
                if(v.y>.70f && v.y<.94f && v.z>.11f && ((w.boneIndex0==RigLayout.Hips && (w.boneIndex1==RigLayout.ThighL || w.boneIndex1==RigLayout.ThighR) && w.weight1>.15f) || (w.boneIndex1==RigLayout.Hips && (w.boneIndex0==RigLayout.ThighL || w.boneIndex0==RigLayout.ThighR) && w.weight0>.15f)))coat++;
            }
            Assert.That(coat,Is.GreaterThan(30),"The new split coat must really emit weighted front surfaces");
            foreach(Vector3[] shape in skin.FaceShapes04L.Values)Assert.That(shape.Length,Is.EqualTo(skin.Vertices.Count));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] public void ProwlerSculptPreservesRigAndActualAttackVolumes(int detail)
        {
            CreatureLook look=FracturedWildsCombat04OC.AzureLook(CreatureLooks.For("neon_jackal",41));
            CreatureBody enhanced=CreatureBuilder.Build(look,detail);look=look.Clone();look.StormProwler04OF=false;CreatureBody prior=CreatureBuilder.Build(look,detail);
            Assert.That(enhanced.Mesh.Vertices.Count,Is.GreaterThan(prior.Mesh.Vertices.Count+100));
            Assert.That(enhanced.Skeleton.Names,Is.EqualTo(prior.Skeleton.Names));
            Assert.That(enhanced.Radius,Is.EqualTo(prior.Radius));Assert.That(enhanced.Height,Is.EqualTo(prior.Height));
            foreach(Vector3 v in enhanced.Mesh.Vertices)Assert.That(MathUtil.IsFinite(v),Is.True);
            foreach(BoneWeight w in enhanced.Mesh.Weights)Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.0001));
            Assert.That(enhanced.Mesh.Vertices.Count,Is.LessThan(22000));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void GuidePresentationKeepsWildlifeSeparateAndPreservesOriginalCombatVolumes(int detail)
        {
            CreatureLook wild=CreatureLooks.For("quartz_drake",41),guide=CompanionLooks.For(BondIds.QuartzGuide,41,1);
            Assert.That(wild.AscensionGuide04OH,Is.False);Assert.That(guide.AscensionGuide04OH,Is.True);
            CreatureBody after=CreatureBuilder.Build(guide,detail);guide=guide.Clone();guide.AscensionGuide04OH=false;
            CreatureBody before=CreatureBuilder.Build(guide,detail);
            Assert.That(after.Mesh.Vertices.Count-before.Mesh.Vertices.Count,Is.InRange(100,1200));
            Assert.That(after.HitVolumes,Is.EqualTo(before.HitVolumes));Assert.That(after.Radius,Is.EqualTo(before.Radius));Assert.That(after.Height,Is.EqualTo(before.Height));
            Assert.That(after.Skeleton.Names,Is.EqualTo(before.Skeleton.Names));
        }
        [TestCase(1)] [TestCase(2)] public void EarnedRegionalPartnersHaveFiniteArtAndCancellationRecovery(int art)
        {
            CreatureBody body=CreatureBuilder.Build(CompanionLooks.For(art==1?BondIds.AshWarden:BondIds.StormSkimmer,41,1),1);CreaturePose pose=new CreaturePose(body);
            for(int frame=0;frame<160;frame++)
            {
                CreaturePoseInput input=CreaturePoseInput.Idle(Vector3.zero,0);input.RegionalArt04OG=frame<100?art:0;
                input.Action=frame<100?CreatureAction.Breath:CreatureAction.None;input.ActionT=frame<100?frame/100f:-1;
                input.Time=frame/60f;pose.Solve(ref input,1f/60);
                foreach(Vector3 p in pose.LocalPositions)Assert.That(MathUtil.IsFinite(p),Is.True);
                foreach(Quaternion q in pose.LocalRotations)Assert.That(float.IsNaN(q.x)||float.IsInfinity(q.x),Is.False);
            }
            Assert.That(FrontierPerformance04OG.ArtBeat(art,1),Is.EqualTo(Vector3.zero));
        }
        [Test] public void CastellanMantleIsARealOpenStructureWithPhaseAndPartBreakResponse()
        {
            MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1);RegionalBossPose04OF.Crest(stone,crystal,false,1);
            Assert.That(stone.VertexCount,Is.GreaterThan(100));Assert.That(crystal.VertexCount,Is.GreaterThan(60));
            Assert.That(stone.Vertices.Max(v=>v.y),Is.GreaterThan(.9f));Assert.That(stone.Vertices.Min(v=>v.x),Is.GreaterThan(.10f),"Leave original core and face exposed");
            float a=RegionalBossPose04OF.Spread(false,0,PartEffect.None,0,0),b=RegionalBossPose04OF.Spread(false,1,PartEffect.None,0,0),c=RegionalBossPose04OF.Spread(false,2,PartEffect.None,0,0);
            Assert.That(a,Is.LessThan(b));Assert.That(b,Is.LessThan(c));Assert.That(RegionalBossPose04OF.Spread(false,2,PartEffect.CoreExposed,0,0),Is.GreaterThan(c));
        }
    }
}
