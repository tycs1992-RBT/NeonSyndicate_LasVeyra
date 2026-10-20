using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class ArtRevolutionH2Tests
    {
        private static string Asset(string model,bool low){return Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Resources/AscensionH2/Meshes/"+model+(low?"_low":"_high")+".bytes");}
        private static MeshBuilder Read(string model,bool low){return AscensionArtH2.Decode(File.ReadAllBytes(Asset(model,low)));}
        private static CityLayout World(int seed,int revision)
        {
            CityLayout world=new CityLayout(seed);world.Generate();
            Assert.That(world.ApplyProfile04O((CityRevision04O)revision,InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog())),Is.True);return world;
        }
        private static List<int> Triangles(MeshBuilder mesh){return ((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mesh))[0];}
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
        [TestCase(false)] [TestCase(true)]
        public void ActualAuthoredFilesHaveRealUVGeometryFiniteNormalsAndBoundedDetail(bool low)
        {
            Assert.That(AscensionArtH2.Models.Distinct().Count(),Is.EqualTo(19));int total=0;
            foreach(string model in AscensionArtH2.Models)
            {
                MeshBuilder mesh=Read(model,low);total+=mesh.VertexCount;
                Assert.That(mesh.VertexCount,Is.GreaterThan(30),model);Assert.That(mesh.UVs.Distinct().Count(),Is.GreaterThan(10),model);
                Assert.That(mesh.Vertices.All(MathUtil.IsFinite),Is.True);Assert.That(mesh.Normals.All(n=>n.sqrMagnitude>.99f && n.sqrMagnitude<1.01f),Is.True);
                Assert.That(mesh.VertexCount,Is.LessThanOrEqualTo(Read(model,false).VertexCount));
                for(int i=0;i<mesh.VertexCount;i+=3)
                    Assert.That(Vector3.Dot(Vector3.Cross(mesh.Vertices[i+1]-mesh.Vertices[i],mesh.Vertices[i+2]-mesh.Vertices[i]).normalized,mesh.Normals[i]),Is.GreaterThan(.98f),model+" winding");
            }
            Assert.That(total,Is.LessThan(130000));
        }
        [Test] public void DecoderRejectsCorruptOrOversizedStreamsWithoutCreatingUnityObjects()
        {
            byte[] data=File.ReadAllBytes(Asset("portal_bay",true));Assert.Throws<InvalidDataException>(()=>AscensionArtH2.Decode(null));
            data[0]=0;Assert.Throws<InvalidDataException>(()=>AscensionArtH2.Decode(data));
            data=File.ReadAllBytes(Asset("portal_bay",true));Array.Copy(BitConverter.GetBytes(90003),0,data,4,4);Assert.Throws<InvalidDataException>(()=>AscensionArtH2.Decode(data));
            data=File.ReadAllBytes(Asset("portal_bay",true));Array.Copy(BitConverter.GetBytes(float.NaN),0,data,8,4);Assert.Throws<InvalidDataException>(()=>AscensionArtH2.Decode(data));
        }
        private sealed class Part
        {
            public string Name;public MeshBuilder Mesh;public Vector3 Min,Max;
            public Part(MeshBuilder mesh,string name){Name=name;Mesh=mesh;Min=new Vector3(mesh.Vertices.Min(v=>v.x),mesh.Vertices.Min(v=>v.y),mesh.Vertices.Min(v=>v.z));Max=new Vector3(mesh.Vertices.Max(v=>v.x),mesh.Vertices.Max(v=>v.y),mesh.Vertices.Max(v=>v.z));}
            public bool Blocks(Vector3 a,Vector3 b)
            {
                for(int k=0;k<3;k++)if(Mathf.Min(a[k],b[k])>Max[k]+.001f || Mathf.Max(a[k],b[k])<Min[k]-.001f)return false;
                for(int i=0;i<Mesh.VertexCount;i+=3)if(Hit(a,b,Mesh.Vertices[i],Mesh.Vertices[i+1],Mesh.Vertices[i+2]))return true;return false;
            }
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void RealAssetCollisionsLeaveGroundedRoutesSideBaysCaveJunctionsAndOldAnchorsClear(int seed)
        {
            CityLayout old=World(seed,7),world=World(seed,8);Assert.That(world.ArtRevolutionH2,Is.True);Assert.That(old.ArtRevolutionH2,Is.False);
            Assert.That(world.RealmRoutes04OB.Select(r=>r.Id),Is.EqualTo(old.RealmRoutes04OB.Select(r=>r.Id)));
            Assert.That(world.CreatureSpots,Is.EqualTo(old.CreatureSpots));Assert.That(world.CrystalShardSpots,Is.EqualTo(old.CrystalShardSpots));
            Dictionary<string,MeshBuilder> sources=new Dictionary<string,MeshBuilder>();List<Part> parts=new List<Part>();int count=0;
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/") && !r.Id.Contains("access")))
            {
                MeshBuilder floor=new MeshBuilder(1);AscensionCompositionH2.Ground(floor,route);List<int> triangles=Triangles(floor);
                foreach(ArtPlacementH2 piece in AscensionCompositionH2.Plan(route))
                {
                    count++;if(!AscensionArtH2.HasCollision(piece.Model))continue;MeshBuilder source;
                    if(!sources.TryGetValue(piece.Model,out source)){source=Read(piece.Model,true);sources.Add(piece.Model,source);}
                    MeshBuilder mesh=new MeshBuilder(1);AscensionArtH2.Append(mesh,source,piece.Position,piece.Yaw,piece.Scale);parts.Add(new Part(mesh,piece.Model+" at "+piece.Position));
                }
                for(int span=1;span<route.Points.Count;span++)
                {
                    Vector3 a=route.Points[span-1],b=route.Points[span],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                    for(int lane=-1;lane<=1;lane++)for(int step=0;step<=12;step++)
                    {
                        Vector3 at=Vector3.Lerp(a,b,step/12f)+side*lane*.95f;bool grounded=false;
                        for(int i=0;i<triangles.Count;i+=3)if(Hit(at+Vector3.up*.08f,at-Vector3.up*.04f,floor.Vertices[triangles[i]],floor.Vertices[triangles[i+1]],floor.Vertices[triangles[i+2]])){grounded=true;break;}
                        Assert.That(grounded,Is.True,route.Id+" real floor");
                    }
                }
            }
            Assert.That(count,Is.InRange(120,350));Assert.That(parts.Sum(p=>p.Mesh.VertexCount),Is.LessThan(230000),"Bounded merged static collision, not FPS acceptance");
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)
                for(int span=1;span<route.Points.Count;span++)
                {
                    Vector3 a=route.Points[span-1],b=route.Points[span],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                    foreach(float height in new[]{.45f,1.2f,1.9f,2.6f})for(int lane=-1;lane<=1;lane++)
                        foreach(Part part in parts)Assert.That(part.Blocks(a+Vector3.up*height+side*lane*.95f,b+Vector3.up*height+side*lane*.95f),Is.False,seed+" "+route.Id+" span "+span+" height "+height+" lane "+lane+" "+part.Name);
                }
            foreach(string token in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"})
            {
                Vector3 before,after;Assert.That(old.ResolveRealmAnchor04OC(token,out before),Is.True);Assert.That(world.ResolveRealmAnchor04OC(token,out after),Is.True);Assert.That(after,Is.EqualTo(before));
                Assert.That(world.FindWalkRoute04O(world.FindPoi("Veyra Lofts").Position,after).Count,Is.GreaterThan(1));
            }
        }
        [Test] public void RevisionEightCannotLoadAnyOlderBankOrGrantAnotherProfileSpecificQuest()
        {
            Assert.That(CityRebirthRules04O.Known(8),Is.True);Assert.That(CityRebirthRules04O.Known(99),Is.False);
            Assert.That(SaveSystem.SlotFileName04OB(2,8),Is.EqualTo("lasveyra_art_h2_save_2.json"));
            for(int revision=0;revision<8;revision++){Assert.That(SaveSystem.BankMatches04OC(revision,8),Is.False);Assert.That(SaveSystem.BankMatches04OC(8,revision),Is.False);}
            Assert.That(FracturedWildsCombat04OC.PreviouslyPaid04OF(new[]{"ev_04oc_azure_watch:first-win-04oc"},"ev_04oc_azure_watch",8),Is.True);
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);
            Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{6}},8),Is.True);
            Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{7}},8),Is.False);
            Assert.That(FracturedWildsCombat04OC.RewardToken("ev_04oc_azure_watch",7,8),Is.EqualTo(FracturedWildsCombat04OC.RewardToken("ev_04oc_azure_watch",999,8)));
        }
        [Test] public void ArticulatedFixedBossMeshMirrorsWithCorrectWindingAndKeepsCoreOpen()
        {
            MeshBuilder right=Read("castellan_mantle",false),left=AscensionArtH2.MirrorX(right);
            Assert.That(right.Vertices.Min(v=>v.x),Is.GreaterThan(.10f));Assert.That(left.Vertices.Max(v=>v.x),Is.LessThan(-.10f));
            for(int i=0;i<left.VertexCount;i+=3)Assert.That(Vector3.Dot(Vector3.Cross(left.Vertices[i+1]-left.Vertices[i],left.Vertices[i+2]-left.Vertices[i]).normalized,left.Normals[i]),Is.GreaterThan(.99f));
            Assert.That(AscensionArtH2.HasCollision("castellan_mantle"),Is.False);Assert.That(AscensionArtH2.HasCollision("fern_colony"),Is.False);Assert.That(AscensionArtH2.HasCollision("ribbed_vault"),Is.True);
        }
        [Test] public void H2PhysicalCommissionUsesOriginalRunnerFiniteBodiesAndNoUnearnedPowers()
        {
            string path=Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Resources/Quests/ascension_h2_living_lens.json");
            List<string> errors=new List<string>();QuestFile file=QuestJson.Read<QuestFile>(File.ReadAllText(path),path,errors,null);Assert.That(errors,Is.Empty);
            QuestDef quest=file.Quests.Single();Assert.That(quest.Id,Is.EqualTo("fg_04oh_h2_living_lens"));Assert.That(quest.VenueGiver,Is.EqualTo("forge"));
            Assert.That(quest.Requires.WorldRevisions,Is.EqualTo(new[]{8}));Assert.That(quest.NoAmbient && quest.IgnoreCrimes,Is.True);
            List<QuestStep> steps=quest.Phases.SelectMany(p=>p.Steps).ToList();
            Assert.That(steps.Count(p=>p.Type=="Interact"),Is.EqualTo(3));Assert.That(steps.Where(p=>p.Type=="Kill").Sum(p=>p.Count),Is.EqualTo(4));
            Assert.That(steps.Where(p=>p.Type=="Kill").All(p=>p.MaxAlive<=2 && p.Count<=2),Is.True);
            Assert.That(steps.Any(p=>p.Type=="GiveGun" || p.Type=="Boss" || p.Type=="Bond"),Is.False);
            Assert.That(quest.Reward.Cash,Is.EqualTo(90));
        }
        [Test] public void H2ChecklistPreservesAllEarlierObservations()
        {
            string[] old=CinematicAcceptance04OF.ChecklistSlice04OH(),current=CinematicAcceptance04OF.ChecklistArtH2();
            Assert.That(current.Length,Is.EqualTo(old.Length+5));for(int i=0;i<old.Length;i++)Assert.That(current[i],Is.EqualTo(old[i]));
            Assert.That(CinematicAcceptance04OF.ProfileWarning(8),Does.Contain("Revision 8"));
        }
    }
}
