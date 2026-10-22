using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class MalikFullBodyH8Tests
    {
        public sealed class Input {public int schema;public string assetId,sourceSha256,space;public bool rootMotion,events;public List<Point> bind;public List<Clip> clips;}
        public sealed class Point {public float x,y,z;public Vector3 Value {get{return new Vector3(x,y,z);}}}
        public sealed class Rotation {public float x,y,z,w;public Quaternion Value {get{return new Quaternion(x,y,z,w);}}}
        public sealed class Clip {public string name;public float duration;public bool loop;public List<Frame> samples;}
        public sealed class Frame {public float t;public List<Point> p;public List<Rotation> q;}
        private MalikDataH8 Load()
        {
            const string path="Assets/NeonSyndicate/Art/ProductionH8/Animations/MalikFullBodyH8.json";
            List<string> errors=new List<string>();Input input=QuestJson.Read<Input>(File.ReadAllText(path),path,errors,null);Assert.IsEmpty(errors);
            List<Vector3> bind=new List<Vector3>();foreach(Point p in input.bind)bind.Add(p.Value);List<MalikClipH8> clips=new List<MalikClipH8>();
            foreach(Clip c in input.clips)
            {
                List<MalikFrameH8> frames=new List<MalikFrameH8>();
                foreach(Frame f in c.samples){List<Vector3> points=new List<Vector3>();List<Quaternion> rotations=new List<Quaternion>();foreach(Point p in f.p)points.Add(p.Value);foreach(Rotation q in f.q)rotations.Add(q.Value);frames.Add(new MalikFrameH8{t=f.t,p=points.ToArray(),q=rotations.ToArray()});}
                clips.Add(new MalikClipH8{name=c.name,duration=c.duration,loop=c.loop,samples=frames.ToArray()});
            }
            return new MalikDataH8{schema=input.schema,assetId=input.assetId,sourceSha256=input.sourceSha256,space=input.space,rootMotion=input.rootMotion,events=input.events,bind=bind.ToArray(),clips=clips.ToArray()};
        }
        [Test] public void RealAuthoredFullBodyTracksPassTheRuntimeContract()
        {string reason;Assert.IsTrue(new MalikMotionH8().Valid(Load(),out reason),reason);}
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void RootEventsMalformedTracksAndUnboundedPosesFailClosed(int defect)
        {
            MalikDataH8 data=Load();
            if(defect==0)data.rootMotion=true;if(defect==1)data.events=true;if(defect==2)data.schema=2;if(defect==3)data.assetId="unearned";
            if(defect==4)data.clips[0].samples[1].t=0;if(defect==5)data.clips[0].samples[1].p[0]=new Vector3(0,20,0);
            if(defect==6)data.clips[0].samples[1].q[0]=new Quaternion(0,0,0,0);if(defect==7)data.bind[0].x=float.NaN;
            if(defect==8)data.clips[0].samples[1].p[0].x+=.3f;if(defect==9)data.clips[0].samples[0].p[0].x+=.01f;
            if(defect==10)data.clips[1].name=data.clips[0].name;if(defect==11)data.clips[0].duration=float.PositiveInfinity;
            string reason;Assert.IsFalse(new MalikMotionH8().Valid(data,out reason));Assert.IsNotEmpty(reason);
        }
        [Test] public void AllRealClipsSampleFiniteUnitRotationsAndRetainOriginalSegmentLengths()
        {
            MalikDataH8 data=Load();MalikMotionH8 sampler=new MalikMotionH8();RigLayout rig=RigLayout.From(Appearance.Malik());Vector3[] points=new Vector3[17],retarget=new Vector3[17];Quaternion[] rotations=new Quaternion[17];
            foreach(MalikClipH8 clip in data.clips)for(int step=0;step<=30;step++)
            {
                Assert.IsTrue(sampler.Sample(clip,step/30f,points,rotations));sampler.Retarget(data,rig,points,retarget);
                for(int i=1;i<17;i++){Assert.AreEqual(rig.Local[i].magnitude,(retarget[i]-retarget[RigLayout.Parent[i]]).magnitude,.00001);Assert.AreEqual(1,QMath.Dot(rotations[i],rotations[i]),.0001);}
            }
        }
        [Test] public void OriginalAttackWindowsDriveTheAuthoredSwordContact()
        {
            MalikMotionH8 sampler=new MalikMotionH8();PoseInput p=PoseInput.Idle();p.Blade=true;p.AttackT=.3f;p.AttackAnim=MeleeAnim.SlashRight;
            Assert.AreEqual(4,sampler.Select(ref p,false));Assert.AreEqual(.36,MalikMotionH7.CutPhase(p.AttackT,.3f,.2f),.00001);
            p.AttackMirror=true;Assert.AreEqual(-1,sampler.Select(ref p,false));p.AttackMirror=false;p.PairedBlades=true;Assert.AreEqual(-1,sampler.Select(ref p,false));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void UnsupportedOrInterruptedActionsRetainOriginalAuthority(int state)
        {
            PoseInput p=PoseInput.Idle();if(state==0)p.DodgeT=.2f;if(state==1)p.StaggerT=.1f;if(state==2)p.Aiming=true;if(state==3)p.Guard=1;if(state==4)p.CrystalCast04G=1;if(state==5)p.Ride=RidePose.Bike;if(state==6)p.Grounded=false;if(state==7)p.Crouch=1;if(state==8)p.Cover=1;if(state==9)p.Reload=.2f;if(state==10)p.RegionalSynergyKind04OF=1;
            Assert.AreEqual(-1,new MalikMotionH8().Select(ref p,state==11));
        }
        [Test] public void AuthoredLocomotionAndCutActuallyReachTheOriginalIkSolver()
        {
            MalikDataH8 data=Load();RigLayout rig=RigLayout.From(Appearance.Malik());HumanoidPose original=new HumanoidPose(rig),review=new HumanoidPose(rig);review.SetReviewMotionH8(data);
            PoseInput p=PoseInput.Idle();p.Velocity=Vector3.forward*2;
            for(int i=0;i<45;i++){p.RootPosition+=p.Velocity/60;original.Solve(ref p,1f/60);review.Solve(ref p,1f/60);}
            Assert.AreEqual("H7_Walk",review.ReviewClipH8);Assert.Less(Mathf.Abs(QMath.Dot(original.Local[RigLayout.UpperArmR],review.Local[RigLayout.UpperArmR])),.9999);
            p.Blade=true;p.AttackAnim=MeleeAnim.SlashRight;p.AttackT=.42f;review.Solve(ref p,1f/60);Assert.AreEqual("H7_OathCut",review.ReviewClipH8);
            p.StaggerT=.1f;review.Solve(ref p,1f/60);Assert.AreEqual("",review.ReviewClipH8);
            review.SetReviewMotionH8(null);Assert.AreEqual("",review.ReviewClipH8);p.StaggerT=-1;review.Solve(ref p,1f/60);Assert.AreEqual("",review.ReviewClipH8);
        }
        [Test] public void MissingTracksAndNanPhaseNeverYieldAPlaybackPose()
        {MalikMotionH8 sampler=new MalikMotionH8();Assert.IsNull(sampler.Find(null,"missing"));Assert.IsFalse(sampler.Sample(Load().clips[0],float.NaN,new Vector3[17],new Quaternion[17]));}
        [TestCase("H8_Dodge")] [TestCase("H8_CrystalCast")] [TestCase("H8_FireAim")] [TestCase("H8_AegisLink")]
        public void AdditionalAuthoredCoverageReachesTheExistingPoseOwner(string action)
        {
            MalikDataH8 data=Load();HumanoidPose pose=new HumanoidPose(RigLayout.From(Appearance.Malik()));pose.SetReviewMotionH8(data);PoseInput p=PoseInput.Idle();
            if(action=="H8_Dodge")p.DodgeT=.45f;if(action=="H8_CrystalCast")p.CrystalCast04G=.8f;if(action=="H8_FireAim"){p.Aiming=true;p.Hold=HoldType.Pistol;}if(action=="H8_AegisLink"){p.RegionalSynergyKind04OF=1;p.RegionalSynergyT04OF=.85f;p.CrystalCast04G=1;}
            pose.Solve(ref p,1f/60);Assert.AreEqual(action,pose.ReviewClipH8);pose.ReviewBlockedH8=true;pose.Solve(ref p,1f/60);Assert.AreEqual("",pose.ReviewClipH8);
        }
        [Test] public void ExistingWeaponRecoilSamplesTheRealAuthoredClipOnlyInSafeStates()
        {
            MalikDataH8 data=Load();MalikMotionH8 sampler=new MalikMotionH8();
            Assert.IsNotNull(sampler.Find(data,"H8_FireRecoil"));
            PoseInput p=PoseInput.Idle();p.Hold=HoldType.Pistol;p.Aiming=true;p.Recoil=12f;
            int selected=sampler.Select(ref p,false,data);
            Assert.GreaterOrEqual(selected,5);Assert.AreEqual("H8_FireRecoil",data.clips[selected].name);
            HumanoidPose pose=new HumanoidPose(RigLayout.From(Appearance.Malik()));pose.SetReviewMotionH8(data);
            pose.Solve(ref p,1f/60);Assert.AreEqual("H8_FireRecoil",pose.ReviewClipH8);
            p.Recoil=0;pose.Solve(ref p,1f/60);Assert.AreEqual("H8_FireAim",pose.ReviewClipH8);
            p.Recoil=12;pose.Solve(ref p,1f/60);Assert.AreEqual("H8_FireRecoil",pose.ReviewClipH8);
            p.Reload=.2f;Assert.AreEqual(-1,sampler.Select(ref p,false,data));
            pose.Solve(ref p,1f/60);Assert.AreEqual("",pose.ReviewClipH8);
            p.Reload=-1;p.DodgeT=.3f;selected=sampler.Select(ref p,false,data);
            Assert.AreEqual("H8_Dodge",data.clips[selected].name);
            p.DodgeT=-1;pose.SetReviewMotionH8(null);pose.Solve(ref p,1f/60);Assert.AreEqual("",pose.ReviewClipH8);
        }
        [Test] public void RecoilCannotBorrowBladedOrUnarmedOwnership()
        {
            MalikDataH8 data=Load();MalikMotionH8 sampler=new MalikMotionH8();
            PoseInput p=PoseInput.Idle();p.Recoil=20;p.Blade=true;p.Hold=HoldType.Pistol;
            Assert.AreNotEqual("H8_FireRecoil",data.clips[sampler.Select(ref p,false,data)].name);
            p.Blade=false;p.Hold=HoldType.None;
            Assert.AreNotEqual("H8_FireRecoil",data.clips[sampler.Select(ref p,false,data)].name);
        }
        [Test] public void FourWeightRejectionAndOriginalBanksStayUnchanged()
        {
            Assert.AreEqual("lasveyra_civic_h5_save_0.json",SaveSystem.SlotFileName04OB(0,10));
            string root="Assets/NeonSyndicate/Art/ProductionH7/";Assert.IsTrue(File.ReadAllText(root+"candidate.json").Contains("\"preserveInfluences\": true"));
        }
    }
}
