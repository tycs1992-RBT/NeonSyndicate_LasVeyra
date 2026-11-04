using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ForgeFreightTests
    {
        private static CityLayout World()
        {
            CityLayout world=new CityLayout(61724);world.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues));return world;
        }
        private static bool Level(ref ForgeFreightFrame frame){return true;}

        [Test] public void EndpointsUseTheActualHydrantBenchAndEastboundLane()
        {
            CityLayout world=World();ForgeFreightRun run=ForgeFreightRun.Find(world);
            Assert.NotNull(run);Assert.That(run.Lane.Length,Is.EqualTo(283.6f).Within(.02f));
            Assert.Greater(run.Length,130);Assert.Less(run.Length,132);
            Assert.That(run.Start().Animal.x,Is.EqualTo(run.Load.x).Within(.01f));
            Assert.That(run.Point(run.Length,out Vector3 end).x,Is.EqualTo(run.Receive.x).Within(.01f));Assert.Greater(end.x,.99f);
            world.Props.RemoveAll(p=>p.Kind==PropKind.Hydrant);Assert.IsNull(ForgeFreightRun.Find(world));
        }

        [Test] public void EveryArticulatedJointHasFiniteDaylightGeometry()
        {
            ForgeFreightModel model=new ForgeFreightModel();HashSet<int> joints=new HashSet<int>();int triangles=0;
            foreach(ForgeFreightModel.Piece part in model.Pieces)
            {
                joints.Add(part.Joint);MeshBuilder m=part.Mesh;
                IReadOnlyList<int> indices=m.Triangles(0);
                for(int i=0;i<indices.Count;i+=3)
                {
                    Vector3 a=m.Vertices[indices[i]],b=m.Vertices[indices[i+1]],c=m.Vertices[indices[i+2]];
                    Assert.True(MathUtil.IsFinite(a)&&MathUtil.IsFinite(b)&&MathUtil.IsFinite(c));
                    Assert.Greater(Vector3.Cross(b-a,c-a).sqrMagnitude,1e-15f);triangles++;
                }
            }
            Assert.AreEqual(ForgeFreightModel.Count,joints.Count);
            Assert.That(triangles,Is.InRange(4200,24000));
        }

        [Test] public void EachFootStaysPlantedInWorldSpaceThroughThePullout()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());float[] offsets={.25f,.75f,0,.5f};
            for(int leg=0;leg<4;leg++)for(int cycle=1;cycle<12;cycle++)
            {
                float start=(cycle-offsets[leg])*ForgeFreightRun.Stride;
                Vector3 a=run.WorldFoot(leg,start+.01f,1);
                for(float t=.05f;t<ForgeFreightRun.Duty;t+=.07f)
                    Assert.Less(Vector3.Distance(a,run.WorldFoot(leg,start+t*ForgeFreightRun.Stride,1)),.0002f);
                Assert.Greater(run.WorldFoot(leg,start+.82f*ForgeFreightRun.Stride,1).y,.34f);
                Assert.That(run.WorldFoot(leg,start+.82f*ForgeFreightRun.Stride,0).y,Is.EqualTo(.13f).Within(.001f));
            }
        }

        [Test] public void HitchedBendsKeepAllFourEnvelopeCornersInsideTheEastboundRoad()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());ForgeFreightFrame f=run.Start();float yaw=0;
            while(f.Distance<run.Length)
            {
                f=run.Advance(f,.18f);Assert.That(Vector3.Distance(f.Axle,f.Hitch),Is.EqualTo(ForgeFreightRun.Tongue).Within(.0001f));
                yaw=Mathf.Max(yaw,Vector3.Angle(f.Forward,f.WagonForward));
                foreach(ForgeFreightEnvelope z in new[]{ForgeFreightEnvelope.Animal(f),ForgeFreightEnvelope.Wagon(f)})
                for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
                {
                    Vector3 right=new Vector3(z.Forward.z,0,-z.Forward.x);
                    Vector3 p=z.Centre+i*z.Half.x*right+j*z.Half.z*z.Forward;
                    Assert.That(p.z,Is.InRange(-156.1f,-150f),"Keep the approached and opposite lane clear");
                }
            }
            Assert.That(yaw,Is.InRange(4f,12f));Assert.Greater(Vector3.Dot(f.Forward,f.WagonForward),.98f);
        }

        [Test] public void HarnessUnfoldsUnderLoadWithoutLosingEitherAttachment()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());
            ForgeFreightFrame frame=run.FrameAtX(-2);
            ForgeFreightModel.JointPose[] poses=new ForgeFreightModel.JointPose[ForgeFreightModel.Count];
            foreach(float effort in new[]{0f,.5f,1f})
            {
                ForgeFreightModel.Pose(run,frame,effort,0,poses);
                for(int i=0;i<2;i++)
                {
                    ForgeFreightModel.JointPose front=poses[ForgeFreightModel.Trace+i],rear=poses[ForgeFreightModel.TraceTail+i];
                    Vector3 join=front.Position+front.Rotation*Vector3.up*front.Scale.y;
                    Vector3 end=rear.Position+rear.Rotation*Vector3.up*rear.Scale.y;
                    Assert.Less((join-rear.Position).magnitude,.0003f);
                    Assert.That((Vector3.Lerp(front.Position,end,.53f)-join).y,Is.EqualTo(.19f*(1-effort)).Within(.0003f));
                }
            }
        }

        [Test] public void WheelsMeasureAxleTravelIncludingDifferentialRoll()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());ForgeFreightFrame f=run.Start();float distance=0,maxSplit=0;
            for(int i=0;i<500;i++)
            {
                ForgeFreightFrame next=run.Advance(f,.14f);distance+=Vector3.Distance(f.Axle,next.Axle);
                maxSplit=Mathf.Max(maxSplit,Mathf.Abs(next.LeftRoll-next.RightRoll));f=next;
            }
            Assert.That((f.LeftRoll+f.RightRoll)*.5f*ForgeFreightRun.WheelRadius,Is.EqualTo(distance).Within(.02f));
            Assert.Greater(maxSplit,.12f);Assert.Greater(f.LeftRoll,100);
        }

        [Test] public void AWholeTeamWaitCannotCreepTowardTheSameCrossing()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());Vector3 obstacle=run.Point(6.1f,out Vector3 forward)+Vector3.up*.9f;
            bool blocked=true;
            ForgeFreightDrive drive=new ForgeFreightDrive(run,run.Start(),f=>!blocked ||
                (!ForgeFreightEnvelope.Animal(f).Contains(obstacle,.35f,.9f) && !ForgeFreightEnvelope.Wagon(f).Contains(obstacle,.35f,.9f)),Level);
            for(int i=0;i<1200 && !drive.Waiting;i++)drive.Step(.02f,true);
            Assert.True(drive.Waiting);float stop=drive.Frame.Distance;
            for(int i=0;i<200;i++)drive.Step(.02f,true);
            Assert.AreEqual(stop,drive.Frame.Distance);Assert.AreEqual(0,drive.Effort);
            // A companion now beside the back wheel must still hold the complete team.
            obstacle=drive.Frame.Axle+Vector3.up*.9f;for(int i=0;i<90;i++)drive.Step(.02f,true);
            Assert.AreEqual(stop,drive.Frame.Distance);
            blocked=false;for(int i=0;i<31;i++)drive.Step(.02f,true);
            Assert.Greater(drive.Speed,0);Assert.Less(drive.Speed,.15f);
        }

        [Test] public void LoadingWaitsForObserverAndTheHaulFinishesWithoutAReverse()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());
            ForgeFreightDrive d=new ForgeFreightDrive(run,run.Start(),f=>true,Level);
            for(int i=0;i<150;i++)d.Step(.04f,false);Assert.AreEqual(0,d.Frame.Distance);
            for(int i=0;i<55;i++)d.Step(.04f,true);Assert.AreEqual(0,d.Frame.Distance);
            float old=0;
            for(int i=0;i<2300;i++){d.Step(.04f,false);Assert.GreaterOrEqual(d.Frame.Distance,old);old=d.Frame.Distance;}
            Assert.AreEqual(run.Length,d.Frame.Distance);Assert.AreEqual(0,d.Speed);
        }

        [Test] public void RepeatedSelectionKeepsExactlyOneSourceOrTeamIncludingOccupiedExit()
        {
            ForgeFreightLease pair=new ForgeFreightLease();
            bool van=true,shell=true,motor=true,team=false;int operations=0;
            System.Action<bool> source=on=>{van=on;shell=on;motor=on;operations++;};
            System.Action<bool> candidate=on=>{team=on;operations++;};
            Assert.False(pair.Select(true,false,false,source,candidate));Assert.AreEqual(0,operations);
            for(int pass=0;pass<3;pass++)
            {
                int start=operations;
                Assert.True(pair.Select(true,true,false,source,candidate));Assert.True(pair.Select(true,true,false,source,candidate));
                Assert.AreEqual(start+2,operations);Assert.True(team);Assert.False(van||shell||motor);
                Assert.False(pair.Select(false,true,true,source,candidate));Assert.True(team);Assert.False(van||shell||motor);
                Assert.True(pair.Select(false,true,false,source,candidate));Assert.True(pair.Select(false,true,false,source,candidate));
                Assert.AreEqual(start+4,operations);Assert.False(team);Assert.True(van&&shell&&motor);
            }
        }

        [Test] public void InterruptedSwitchReinstatesThePreviousOwner()
        {
            ForgeFreightLease pair=new ForgeFreightLease();bool van=true,team=false;
            Assert.Throws<System.InvalidOperationException>(()=>pair.Select(true,true,false,on=>van=on,on=>{
                team=on;if(on)throw new System.InvalidOperationException("candidate failed");
            }));
            Assert.False(pair.Active);Assert.True(van);Assert.False(team);
            pair.Select(true,true,false,on=>van=on,on=>team=on);
            Assert.Throws<System.InvalidOperationException>(()=>pair.Select(false,true,false,on=>{
                van=on;if(on)throw new System.InvalidOperationException("source failed");
            },on=>team=on));
            Assert.True(pair.Active);Assert.False(van);Assert.True(team);
        }

        [Test] public void MissingGroundHoldsWheelsAndPreservesTheLastSupportedPose()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());
            ForgeFreightDrive d=new ForgeFreightDrive(run,run.FrameAtX(12f),f=>true,(ref ForgeFreightFrame f)=>false);
            ForgeFreightFrame before=d.Frame;for(int i=0;i<100;i++)d.Step(.02f,true);
            Assert.True(d.Waiting);Assert.AreEqual(before.Distance,d.Frame.Distance);Assert.AreEqual(before.LeftRoll,d.Frame.LeftRoll);
            Assert.AreEqual(before.Axle,d.Frame.Axle);
        }

        [Test] public void ReacquiringAReleasedVanKeepsItsLaneStationAndCoupling()
        {
            ForgeFreightRun run=ForgeFreightRun.Find(World());
            foreach(float x in new[]{-9f,2f,29f,108f,119f})
            {
                ForgeFreightFrame f=run.FrameAtX(x);Assert.That(f.Animal.x,Is.EqualTo(x).Within(.004f));
                Assert.That((f.Hitch-f.Axle).magnitude,Is.EqualTo(ForgeFreightRun.Tongue).Within(.0002f));
                Assert.Less(MathUtil.FlatDistance(f.Animal,run.Lane.PointAt(run.Lane.Project(f.Animal))),1.47f);
            }
        }
    }
    /// <summary>Native physics/scene test. Run in Unity separately from the managed freight harness.</summary>
    [Category("NeedsEngine")]
    public sealed class ForgeFreightNativeTests
    {
        [Test] public void ARealRearColliderDefersRestorationOfBothCarRoots()
        {
            GameObject parent=new GameObject("freight fixture"),van=new GameObject("test van"),shell=new GameObject("detached shell"),
                walker=new GameObject("crossing occupant");
            try
            {
                van.transform.SetParent(parent.transform);shell.transform.SetParent(parent.transform);walker.transform.SetParent(parent.transform);
                Vehicle source=van.AddComponent<Vehicle>();van.AddComponent<BoxCollider>();AudioSource motor=van.AddComponent<AudioSource>();
                Rigidbody shellBody=shell.AddComponent<Rigidbody>();shellBody.isKinematic=true;shell.AddComponent<BoxCollider>();
                const System.Reflection.BindingFlags hidden=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(Vehicle).GetField("_shell",hidden).SetValue(source,shellBody);
                System.Type viewType=typeof(Game).Assembly.GetType("NeonSyndicate.ForgeFreightView",true);
                Component view=(Component)viewType.GetMethod("Prepare").Invoke(null,new object[]{parent.transform});
                System.Reflection.MethodInfo toggle=typeof(Vehicle).GetMethod("SetFreightSourceActive",hidden);
                System.Action<bool> showSource=on=>toggle.Invoke(source,new object[]{on});
                CityLayout world=new CityLayout(61724);world.Generate();
                List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
                world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues);ForgeFreightRun run=ForgeFreightRun.Find(world);
                ForgeFreightFrame f=run.Start();f.Animal.y=f.Axle.y=500;
                viewType.GetMethod("Show").Invoke(view,new object[]{run,f,0f,0f});
                ForgeFreightLease lease=new ForgeFreightLease();
                walker.AddComponent<CapsuleCollider>();int parts=view.GetComponentsInChildren<MeshRenderer>(true).Length;
                for(int pass=0;pass<2;pass++)
                {
                    Assert.True(lease.Select(true,true,false,showSource,view.gameObject.SetActive));
                    Assert.False(van.activeInHierarchy || shell.activeInHierarchy || motor.isActiveAndEnabled);
                    walker.transform.position=f.Axle+Vector3.up;Physics.SyncTransforms();
                    bool clear=(bool)viewType.GetMethod("EnvelopeFree").Invoke(view,new object[]{f,source});
                    Assert.False(clear);Assert.False(lease.Select(false,true,!clear,showSource,view.gameObject.SetActive));
                    walker.transform.position+=Vector3.forward*20;Physics.SyncTransforms();
                    clear=(bool)viewType.GetMethod("EnvelopeFree").Invoke(view,new object[]{f,source});Assert.True(clear);
                    Assert.True(lease.Select(false,true,!clear,showSource,view.gameObject.SetActive));
                    Assert.True(van.activeInHierarchy && shell.activeInHierarchy && motor.isActiveAndEnabled);
                    Assert.False(view.gameObject.activeInHierarchy);Assert.AreEqual(parts,view.GetComponentsInChildren<MeshRenderer>(true).Length);
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(parent);}
        }
    }

}
