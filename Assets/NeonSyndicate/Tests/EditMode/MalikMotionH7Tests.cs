using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class MalikMotionH7Tests
    {
        public sealed class Input {public int schema;public string assetId,space;public bool rootMotion,events;public List<string> bones;public List<ClipInput> clips;}
        public sealed class ClipInput {public string name;public float duration;public bool loop;public List<SampleInput> samples;}
        public sealed class SampleInput {public float t;public List<RotationInput> q;}
        public sealed class RotationInput {public float x,y,z,w;}
        [Test] public void ActualShippedAuthoredSamplesMeetTheSameRuntimeContract()
        {
            string path=Path.Combine(Directory.GetCurrentDirectory(),MalikReviewH7.Root+"Animations/MalikMotionH7.json");List<string> errors=new List<string>();
            Input input=QuestJson.Read<Input>(File.ReadAllText(path),path,errors,null);Assert.IsEmpty(errors);
            List<MalikMotionClipH7> clips=new List<MalikMotionClipH7>();
            foreach(ClipInput c in input.clips)
            {
                List<MalikMotionSampleH7> samples=new List<MalikMotionSampleH7>();
                foreach(SampleInput sample in c.samples)
                {List<Quaternion> q=new List<Quaternion>();foreach(RotationInput r in sample.q)q.Add(new Quaternion(r.x,r.y,r.z,r.w));samples.Add(new MalikMotionSampleH7{t=sample.t,q=q.ToArray()});}
                clips.Add(new MalikMotionClipH7{name=c.name,duration=c.duration,loop=c.loop,samples=samples.ToArray()});
            }
            MalikMotionDataH7 data=new MalikMotionDataH7{schema=input.schema,assetId=input.assetId,space=input.space,rootMotion=input.rootMotion,events=input.events,bones=input.bones.ToArray(),clips=clips.ToArray()};string reason;Assert.IsTrue(MalikMotionH7.Valid(data,out reason),reason);
            foreach(MalikMotionClipH7 clip in data.clips)for(int i=0;i<=100;i++)
            {Quaternion q=MalikMotionH7.Sample(clip,RigLayout.Chest,i/100f);Assert.AreEqual(1,QMath.Dot(q,q),.0001);Assert.AreEqual(QMath.Identity,MalikMotionH7.Sample(clip,RigLayout.HandR,i/100f));}
        }
        private static MalikMotionDataH7 Data()
        {
            string[] names={"H7_IdleBreath","H7_Walk","H7_Run","H7_SwordReady","H7_OathCut"};MalikMotionClipH7[] clips=new MalikMotionClipH7[5];
            for(int i=0;i<5;i++)clips[i]=new MalikMotionClipH7{name=names[i],duration=1,loop=i<4,samples=new[]{new MalikMotionSampleH7{t=0,q=new[]{QMath.Identity,QMath.Identity,QMath.Identity}},new MalikMotionSampleH7{t=.5f,q=new[]{QMath.AY(20),QMath.AX(4),QMath.AZ(3)}},new MalikMotionSampleH7{t=1,q=new[]{QMath.Identity,QMath.Identity,QMath.Identity}}}};
            return new MalikMotionDataH7{schema=1,space=MalikMotionH7.RotationSpace,assetId=MalikReviewH7.AssetId,bones=new[]{"Chest","Neck","Head"},clips=clips};
        }
        [Test] public void VerifiedDataPreservesExactlyThreePresentationBones(){string reason;Assert.IsTrue(MalikMotionH7.Valid(Data(),out reason));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void CorruptOrExpandedAuthorityCannotEnterReview(int defect)
        {
            MalikMotionDataH7 d=Data();
            if(defect==0)d.assetId=MalikReviewH6.AssetId;if(defect==1)d.schema=2;if(defect==2)d.rootMotion=true;if(defect==3)d.events=true;
            if(defect==4)d.bones[0]="Hand.R";if(defect==5)d.clips[0].name="Unknown";if(defect==6)d.clips[4].loop=true;if(defect==7)d.clips[0].duration=float.NaN;
            if(defect==8)d.clips[0].samples[1].t=0;if(defect==9)d.clips[0].samples[1].q[0]=new Quaternion(0,0,0,0);if(defect==10)d.clips[0].samples[1].q[0]=QMath.AY(100);if(defect==11)d.clips[0].samples[2].t=.9f;
            string reason;Assert.IsFalse(MalikMotionH7.Valid(d,out reason));Assert.IsNotEmpty(reason);
        }
        [TestCase(RigLayout.HandR)] [TestCase(RigLayout.HandL)] [TestCase(RigLayout.Hips)] [TestCase(RigLayout.FootL)] [TestCase(RigLayout.FootR)] [TestCase(30)]
        public void OriginalHandsFingersFeetAndRootAreNeverSampled(int semantic){Assert.AreEqual(QMath.Identity,MalikMotionH7.Sample(Data().clips[0],semantic,.5f));}
        [Test] public void SamplingUsesActualKeyTimingAndInterpolation()
        {
            Quaternion q=MalikMotionH7.Sample(Data().clips[0],RigLayout.Chest,.25f);Assert.Greater(Mathf.Abs(QMath.Dot(q,QMath.AY(10))),.99999f);
            Assert.AreEqual(QMath.Identity,MalikMotionH7.Sample(Data().clips[0],RigLayout.Head,-5));Assert.AreEqual(QMath.Identity,MalikMotionH7.Sample(Data().clips[0],RigLayout.Head,5));Assert.AreEqual(QMath.Identity,MalikMotionH7.Sample(Data().clips[0],RigLayout.Head,float.NaN));
        }
        [Test] public void OriginalWindupActiveAndRecoveryRetimingKeepsContactBoundaries()
        {
            Assert.AreEqual(0,MalikMotionH7.CutPhase(0,.3f,.2f),.00001);Assert.AreEqual(.36,MalikMotionH7.CutPhase(.3f,.3f,.2f),.00001);Assert.AreEqual(.55,MalikMotionH7.CutPhase(.5f,.3f,.2f),.00001);Assert.AreEqual(1,MalikMotionH7.CutPhase(1,.3f,.2f),.00001);
        }
        [TestCase(-1,.3f,.2f)] [TestCase(.5f,0,.2f)] [TestCase(.5f,.8f,.3f)] [TestCase(float.NaN,.3f,.2f)]
        public void CancelledOrInvalidOriginalPhaseDoesNotProduceAMotion(float phase,float windup,float active){Assert.AreEqual(-1,MalikMotionH7.CutPhase(phase,windup,active));}
        [Test] public void AccentCannotExceedAQuarterOfAuthoredRotation()
        {
            Quaternion source=QMath.AY(40);Assert.Greater(Mathf.Abs(QMath.Dot(MalikMotionH7.Accent(source,500),QMath.AY(10))),.99999f);Assert.AreEqual(QMath.Identity,MalikMotionH7.Accent(source,float.NaN));Assert.AreEqual(QMath.Identity,MalikMotionH7.Accent(source,-1));
        }
        [Test] public void ValidH7InspectionStillCannotEnterAPlayerBuildOrBecomeApproval()
        {
            ImportedBone04OD[] bones=new ImportedBone04OD[RigLayout.BoneCount];for(int i=0;i<bones.Length;i++)bones[i]=new ImportedBone04OD{SourceBone=i,TargetPath="Art/Root/bone"+i};
            string[] roles={"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"};ImportedSocket04OD[] sockets=new ImportedSocket04OD[roles.Length];for(int i=0;i<roles.Length;i++)sockets[i]=new ImportedSocket04OD{Role=roles[i],Path="Art/Root/socket_"+roles[i]};
            ImportedModel04OD model=new ImportedModel04OD{Identity="malik",Enabled=false,Bones=bones,Sockets=sockets,AlignBindAxesH3=true,PreserveInfluencesH5=true,ArtProof=new ImportedArtRecord04OH{AssetId=MalikReviewH7.AssetId,Creator="Recorded H7 lawful art",LicenseId="CC0-1.0",RightsRecordId="h7-lineage",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false,ExportAssetPath=MalikReviewH7.ExportPath,ExportSha256=new string('a',64),VisualDependencyHash=new string('b',32)}};
            string reason;Assert.IsTrue(MalikReviewH7.CanReview(model,true,out reason));Assert.IsFalse(MalikReviewH7.CanReview(model,false,out reason));Assert.IsFalse(ImportedArt04OH.CanActivate(model.ArtProof,model.AnimationAuthority,out reason));
            model.PreserveInfluencesH5=false;Assert.IsFalse(MalikReviewH7.CanReview(model,true,out reason));model.PreserveInfluencesH5=true;model.AlignBindAxesH3=false;Assert.IsFalse(MalikReviewH7.CanReview(model,true,out reason));
        }
        [Test] public void H7ReviewCannotAcceptH6OrActivateAnUnapprovedCandidate()
        {
            ImportedModel04OD model=new ImportedModel04OD{Identity="malik",Enabled=false,ArtProof=new ImportedArtRecord04OH{AssetId=MalikReviewH6.AssetId,ExportAssetPath=MalikReviewH6.ExportPath}};string reason;Assert.IsFalse(MalikReviewH7.CanReview(model,true,out reason));Assert.IsFalse(MalikReviewH7.CanReview(model,false,out reason));
            Assert.IsFalse(ImportedArt04OH.CanActivate(model.ArtProof,ImportedAnimationAuthority04OH.AuthoredClips,out reason));Assert.IsFalse(model.Enabled);
        }
    }
}
