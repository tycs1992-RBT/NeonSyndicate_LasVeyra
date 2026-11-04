using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class CivicH5Tests
    {
        private static MeshBuilder Source(string model)
        {return AscensionArtH2.Decode(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Resources/AscensionH2/Meshes/"+model+"_low.bytes")));}
        private static CityLayout World(int seed,int revision,out List<VenueBinding04C> venues)
        {
            CityLayout world=new CityLayout(seed);world.Generate();venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O((CityRevision04O)revision,venues),Is.True);return world;
        }
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
        private sealed class Part
        {
            public string Name;public MeshBuilder Mesh;public List<int> Indices;public Vector3 Min,Max;
            public Part(MeshBuilder mesh,string name){Name=name;Mesh=mesh;Indices=((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mesh))[0];Min=new Vector3(mesh.Vertices.Min(v=>v.x),mesh.Vertices.Min(v=>v.y),mesh.Vertices.Min(v=>v.z));Max=new Vector3(mesh.Vertices.Max(v=>v.x),mesh.Vertices.Max(v=>v.y),mesh.Vertices.Max(v=>v.z));}
            public bool Blocks(Vector3 a,Vector3 b)
            {
                for(int k=0;k<3;k++)if(Mathf.Min(a[k],b[k])>Max[k]+.001f || Mathf.Max(a[k],b[k])<Min[k]-.001f)return false;
                for(int i=0;i<Indices.Count;i+=3)if(Hit(a,b,Mesh.Vertices[Indices[i]],Mesh.Vertices[Indices[i+1]],Mesh.Vertices[Indices[i+2]]))return true;return false;
            }
        }

        private static Part Piece(ArtPlacementH2 piece,Vector3 origin,float yaw)
        {
            MeshBuilder local=new MeshBuilder(1),mesh=new MeshBuilder(1);
            AscensionArtH2.Append(local,Source(piece.Model),piece.Position,piece.Yaw,piece.Scale);
            AscensionArtH2.Append(mesh,local,origin,yaw,Vector3.one);return new Part(mesh,piece.Model+" at "+mesh.Vertices[0]);
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void NewCourtAndFunctionalServiceDoorsStayClearWithoutMovingOldAnchors(int seed)
        {
            List<VenueBinding04C> before,venues;CityLayout old=World(seed,9,out before),world=World(seed,10,out venues);
            Assert.That(world.GothicHeartH3 && world.ArtRevolutionH2 && world.Frontier04OG,Is.True);
            Assert.That(old.GothicCivicH5,Is.False);Assert.That(world.GothicCivicH5,Is.True);Assert.That(world.CrystalShardSpots,Is.EqualTo(old.CrystalShardSpots));
            List<ArtPlacementH2> worldPieces=GothicHeartH3.CourtPlan(world);worldPieces.AddRange(GothicHeartH3.RegionalPlan(world));
            List<Part> parts=worldPieces.Where(p=>AscensionArtH2.HasCollision(p.Model)).Select(p=>Piece(p,Vector3.zero,0)).ToList();
            foreach(VenueBinding04C venue in venues)
            {
                VenueBinding04C previous=before.Single(v=>v.Definition.Id==venue.Definition.Id);
                Assert.That(venue.Origin,Is.EqualTo(previous.Origin));Assert.That(venue.Yaw,Is.EqualTo(previous.Yaw));
                Assert.That(world.FindWalkRoute04O(venues.Single(v=>v.Definition.Id=="lofts").Outside,venue.Outside).Count,Is.GreaterThan(1),venue.Definition.Id);
                bool civic=GothicCivicH5.Replaces(venue.Definition.Id);if(!GothicHeartH3.Replaces(venue.Definition.Id) && !civic)continue;
                List<Part> shell=(civic?GothicCivicH5.Plan(venue):GothicHeartH3.ServicePlan(venue)).Select(p=>Piece(p,venue.Origin,venue.Yaw)).ToList();
                MeshBuilder local=new MeshBuilder(1),structure=new MeshBuilder(1);if(civic)GothicCivicH5.Structure(local,venue);else GothicHeartH3.Structure(local,venue);structure.Append(local);for(int i=0;i<structure.VertexCount;i++){structure.Vertices[i]=venue.Origin+QMath.AY(venue.Yaw)*structure.Vertices[i];structure.Normals[i]=QMath.AY(venue.Yaw)*structure.Normals[i];}
                shell.Add(new Part(structure,"service foundations"));parts.AddRange(shell);
                // Same original service entry, full 1.9m body and 2.6m companion/camera-height corridor.
                for(int lane=-1;lane<=1;lane++)foreach(float height in new[]{.45f,1.2f,1.9f,2.6f})
                {
                    Vector3 a=InhabitedCityBindings04C.LocalToWorld(venue.Origin,venue.Yaw,new Vector3(lane*.65f,height,-5));
                    Vector3 b=InhabitedCityBindings04C.LocalToWorld(venue.Origin,venue.Yaw,new Vector3(lane*.65f,height,3));
                    foreach(Part part in shell)Assert.That(part.Blocks(a,b),Is.False,venue.Definition.Id+" entry "+height+" / "+part.Name);
                }
            }
            List<string> conflicts=new List<string>();
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>!r.Id.StartsWith("04oh/h3/")))
            for(int span=1;span<route.Points.Count;span++)
            {
                Vector3 a=route.Points[span-1],b=route.Points[span],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                foreach(float height in new[]{.45f,1.2f,1.9f,2.6f})for(int lane=-1;lane<=1;lane++)foreach(Part p in parts)
                    if(p.Blocks(a+Vector3.up*height+side*lane*.95f,b+Vector3.up*height+side*lane*.95f) && conflicts.Count<40)conflicts.Add(route.Id+" / "+height+" / "+lane+" / "+p.Name);
            }
            Assert.That(conflicts.Count,Is.Zero,string.Join("\n",conflicts));
            RealmRoute04OB porch=world.RealmRoutes04OB.Single(r=>r.Id=="04oh/h3/home-porch");
            Assert.That(world.FindWalkRoute04O(venues.Single(v=>v.Definition.Id=="lofts").Outside,porch.Points.Last()).Count,Is.GreaterThan(2),"Raised porch is a real admitted destination");
            foreach(Vector3 point in porch.Points)
                Assert.That(parts.Any(p=>p.Blocks(point+Vector3.up*.08f,point-Vector3.up*.05f)),Is.True,"Actual stair or landing must support navigation endpoint "+point);
            Assert.That(parts.Sum(p=>p.Mesh.VertexCount),Is.LessThan(800000));
            Assert.That(venues.Count(v=>GothicHeartH3.Replaces(v.Definition.Id)),Is.EqualTo(5));
            List<ArtPlacementH2> home=GothicHeartH3.ServicePlan(venues.Single(v=>v.Definition.Id=="lofts"));
            Assert.That(home.Any(p=>p.Model=="processional_stair"),Is.True);
            Assert.That(GothicHeartH3.ServicePlan(venues.Single(v=>v.Definition.Id=="forge")).Any(p=>p.Model=="forge_bay"),Is.True);
            foreach(string id in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-spring","Realm:storm-vein"})
            {Vector3 a,b;Assert.That(old.ResolveRealmAnchor04OC(id,out a),Is.True);Assert.That(world.ResolveRealmAnchor04OC(id,out b),Is.True);Assert.That(b,Is.EqualTo(a));}
        }
        [Test] public void OriginalSanctuaryGalleryHasSupportedWalkwayAndHeadroom()
        {
            List<VenueBinding04C> venues;World(61724,10,out venues);VenueBinding04C archive=venues.Single(v=>v.Definition.Id=="archive");
            MeshBuilder structure=new MeshBuilder(1);GothicCivicH5.Structure(structure,archive);
            List<Part> parts=GothicCivicH5.Plan(archive).Where(p=>AscensionArtH2.HasCollision(p.Model)).Select(p=>Piece(p,Vector3.zero,0)).ToList();parts.Add(new Part(structure,"original sanctuary interior"));
            foreach(float z in new[]{32f,34f,37f,40f,42f})
            {
                Vector3 point=new Vector3(6.2f,1.2f,z);Assert.That(parts.Any(p=>p.Blocks(point+Vector3.up*.08f,point-Vector3.up*.05f)),Is.True,"Original gallery floor support");
                foreach(Part part in parts)Assert.That(part.Blocks(point+Vector3.up*.1f,point+Vector3.up*2.6f),Is.False,"Gallery headroom / "+part.Name);
            }
        }
        [Test] public void NewCivicBankRetainsPriorBanksAndPhysicalCommissionEligibility()
        {
            Assert.That(SaveSystem.SlotFileName04OB(0,10),Is.EqualTo("lasveyra_civic_h5_save_0.json"));
            Assert.That(new ImportedModel04OD().PreserveInfluencesH5,Is.False);
            for(int revision=0;revision<10;revision++)Assert.That(SaveSystem.BankMatches04OC(revision,10),Is.False);
            foreach(int prior in new[]{6,8,9})Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{prior}},10),Is.True);
            Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{7}},10),Is.False);
            foreach(string id in new[]{"ev_04ob_threshold_watch","ev_04oc_azure_watch"})
            {string token=FracturedWildsCombat04OC.RewardToken(id,1,10);Assert.That(token,Is.EqualTo(FracturedWildsCombat04OC.RewardToken(id,9999,10)));Assert.That(FracturedWildsCombat04OC.PreviouslyPaid04OF(new[]{token},id,10),Is.True);}
            Assert.That(CinematicAcceptance04OF.ProfileWarning(10),Does.Contain("Revision 10"));
        }
    }
}
