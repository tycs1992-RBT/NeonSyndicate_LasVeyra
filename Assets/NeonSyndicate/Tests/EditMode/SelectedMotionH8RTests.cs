using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class SelectedMotionH8RTests
    {
        private MalikDataH8 Load()
        {
            const string path="Assets/NeonSyndicate/Art/ProductionH8R/Animations/MalikFullBodyH8R.json";
            List<string> errors=new List<string>();MalikFullBodyH8Tests.Input input=QuestJson.Read<MalikFullBodyH8Tests.Input>(File.ReadAllText(path),path,errors,null);Assert.IsEmpty(errors);
            List<Vector3> bind=new List<Vector3>();foreach(MalikFullBodyH8Tests.Point point in input.bind)bind.Add(point.Value);
            List<MalikClipH8> clips=new List<MalikClipH8>();
            foreach(MalikFullBodyH8Tests.Clip clip in input.clips)
            {
                List<MalikFrameH8> frames=new List<MalikFrameH8>();
                foreach(MalikFullBodyH8Tests.Frame frame in clip.samples)
                {
                    List<Vector3> points=new List<Vector3>();List<Quaternion> rotations=new List<Quaternion>();
                    foreach(MalikFullBodyH8Tests.Point point in frame.p)points.Add(point.Value);
                    foreach(MalikFullBodyH8Tests.Rotation rotation in frame.q)rotations.Add(rotation.Value);
                    frames.Add(new MalikFrameH8{t=frame.t,p=points.ToArray(),q=rotations.ToArray()});
                }
                clips.Add(new MalikClipH8{name=clip.name,duration=clip.duration,loop=clip.loop,samples=frames.ToArray()});
            }
            return new MalikDataH8{schema=input.schema,assetId=input.assetId,sourceSha256=input.sourceSha256,space=input.space,rootMotion=input.rootMotion,events=input.events,bind=bind.ToArray(),clips=clips.ToArray()};
        }
        [Test] public void ActualSelectedBodyAndRetargetedGuardPassTheFixedMotionContract()
        {
            MalikDataH8 data=Load();string reason;Assert.IsTrue(new MalikMotionH8().Valid(data,out reason),reason);
            Assert.AreEqual(MalikReviewH8R.AssetId,data.assetId);Assert.AreEqual(15,data.clips.Length);
            MalikClipH8 guard=new MalikMotionH8().Find(data,"H8_Guard_Brace");Assert.IsNotNull(guard);Assert.AreEqual(38,guard.samples.Length);
        }
        [Test] public void RetargetedBraceUsesTheOriginalGuardAndCleansUpOnInterruption()
        {
            MalikDataH8 data=Load();HumanoidPose pose=new HumanoidPose(RigLayout.From(Appearance.Malik()));pose.SetReviewMotionH8(data);
            PoseInput input=PoseInput.Idle();input.Blade=true;input.Guard=1;pose.Solve(ref input,1f/60);
            Assert.AreEqual("H8_Guard_Brace",pose.ReviewClipH8);
            input.StaggerT=.1f;pose.Solve(ref input,1f/60);Assert.AreEqual("",pose.ReviewClipH8);
            input.StaggerT=-1;input.Guard=0;pose.Solve(ref input,1f/60);Assert.AreNotEqual("H8_Guard_Brace",pose.ReviewClipH8);
            pose.SetReviewMotionH8(null);pose.Solve(ref input,1f/60);Assert.AreEqual("",pose.ReviewClipH8);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void GuardNeverTakesOverAnAttackDodgeTravelPauseOrAim(int conflict)
        {
            MalikDataH8 data=Load();PoseInput input=PoseInput.Idle();input.Guard=1;
            if(conflict==0)input.AttackT=.2f;if(conflict==1)input.DodgeT=.2f;if(conflict==2)input.Travel=TravelPose.Fly;if(conflict==4)input.Aiming=true;
            Assert.AreEqual(-1,new MalikMotionH8().Select(ref input,conflict==3,data));
        }
        [Test] public void MissingRetargetedTrackAndUnapprovedProfileRemainRejected()
        {
            MalikDataH8 data=Load();MalikMotionH8 motion=new MalikMotionH8();string reason;motion.Find(data,"H8_Guard_Brace").name="H8_Unrelated";
            Assert.IsFalse(motion.Valid(data,out reason));data=Load();data.assetId="unknown-selected-human";Assert.IsFalse(motion.Valid(data,out reason));
        }
    }
}
