using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    /// <summary>The curated H8S motion set (schema 2): exact data contract, landing after real air time, the quiet
    /// listening loop for a named cast member and the H8S review contract. Offline maths only; no native Unity.</summary>
    public sealed class CuratedMotionH8STests
    {
        public const string CuratedPath="Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json";
        public const string SelectedPath="Assets/NeonSyndicate/Art/ProductionH8R/Animations/MalikFullBodyH8R.json";
        private const float Dt=1f/60f;

        /// <summary>Reads a delivered motion file with the strict project JSON reader (no JsonUtility, so it runs offline).</summary>
        public static MalikDataH8 Load(string path)
        {
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

        // ------------------------------------------------------------------ data contract

        [Test] public void DeliveredCuratedSetPassesTheSchemaTwoContract()
        {
            MalikDataH8 data=Load(CuratedPath);string reason;MalikMotionH8 motion=new MalikMotionH8();
            Assert.IsTrue(motion.Valid(data,out reason),reason);
            Assert.AreEqual(2,data.schema);Assert.AreEqual(MalikReviewH8S.AssetId,data.assetId);Assert.AreEqual(17,data.clips.Length);
            Assert.AreEqual(MalikMotionH8.CuratedClipsH8S(),System.Array.ConvertAll(data.clips,c=>c.name));
            MalikClipH8 landing=motion.Find(data,LandingTriggerH8S.HeavyClip),listening=motion.Find(data,QuietMotionH8S.ClipName);
            Assert.AreEqual(39,landing.samples.Length);Assert.IsFalse(landing.loop);Assert.AreEqual(1.2667,landing.duration,.001);
            Assert.AreEqual(76,listening.samples.Length);Assert.IsTrue(listening.loop);Assert.AreEqual(2.5,listening.duration,.0001);
            int total=0;foreach(MalikClipH8 clip in data.clips)total+=clip.samples.Length;Assert.AreEqual(701,total);
        }
        [Test] public void SchemaOneSelectedDataStillPassesUnchanged()
        {string reason;MalikDataH8 data=Load(SelectedPath);Assert.AreEqual(1,data.schema);Assert.IsTrue(new MalikMotionH8().Valid(data,out reason),reason);}

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)] [TestCase(16)] [TestCase(17)]
        public void WrongIdsMalformedTracksEventsAndRootMotionFailClosed(int defect)
        {
            MalikDataH8 data=Load(CuratedPath);MalikMotionH8 motion=new MalikMotionH8();
            MalikClipH8 landing=motion.Find(data,LandingTriggerH8S.HeavyClip),listening=motion.Find(data,QuietMotionH8S.ClipName);
            if(defect==0)data.assetId=MalikReviewH7.AssetId;
            if(defect==1)data.rootMotion=true;
            if(defect==2)data.events=true;
            if(defect==3)data.schema=3;
            if(defect==4)landing.name="H8_Landing_Other";
            if(defect==5){MalikClipH8 swap=data.clips[14];data.clips[14]=data.clips[15];data.clips[15]=swap;}
            if(defect==6)listening.loop=false;
            if(defect==7)landing.loop=true;
            if(defect==8){List<MalikClipH8> fewer=new List<MalikClipH8>(data.clips);fewer.RemoveAt(16);data.clips=fewer.ToArray();}
            if(defect==9)landing.samples[5].t+=.01f;
            if(defect==10)landing.samples[20].q[6]=QMath.AX(170f)*landing.samples[20].q[6];
            if(defect==11)listening.samples[10].p[7]+=new Vector3(0,.5f,0);
            if(defect==12)landing.samples[3].p=new Vector3[16];
            if(defect==13)
            {
                // A longer landing with a matching 30 Hz track: only the delivered-duration rule can reject it.
                List<MalikFrameH8> frames=new List<MalikFrameH8>(landing.samples);MalikFrameH8 last=frames[frames.Count-1];
                frames.Add(new MalikFrameH8{t=frames.Count/30f,p=(Vector3[])last.p.Clone(),q=(Quaternion[])last.q.Clone()});
                landing.samples=frames.ToArray();landing.duration=frames[frames.Count-1].t;
            }
            if(defect==14)data.sourceSha256="not-a-recorded-source";
            if(defect==15)
            {
                // Root translation hidden inside the tracks: a smooth sideways drift, continuous but out of the hip bound.
                MalikClipH8 idle=data.clips[0];for(int k=0;k<idle.samples.Length;k++)for(int i=0;i<17;i++)idle.samples[k].p[i]+=new Vector3(.006f*k,0,0);
            }
            if(defect==16)data.bind[13]+=new Vector3(0,.05f,0);
            if(defect==17)data.clips[1].samples[10].q[9]=QMath.AX(60f)*data.clips[1].samples[10].q[9];
            string reason;Assert.IsFalse(motion.Valid(data,out reason));Assert.IsNotEmpty(reason);
        }
        [Test] public void CuratedRotationsInterpolateOnTheShortArcAndLoopsClose()
        {
            MalikDataH8 data=Load(CuratedPath);MalikMotionH8 motion=new MalikMotionH8();Vector3[] points=new Vector3[17];Quaternion[] rotations=new Quaternion[17];
            foreach(MalikClipH8 clip in data.clips)
            {
                for(int k=1;k<clip.samples.Length;k++)for(int i=0;i<17;i++)
                {
                    // Halfway between two samples the slerp must stay between them, never swing the long way round.
                    Assert.IsTrue(motion.Sample(clip,(k-.5f)/(clip.samples.Length-1),points,rotations));
                    float total=Angle(clip.samples[k-1].q[i],clip.samples[k].q[i]);
                    Assert.LessOrEqual(Angle(clip.samples[k-1].q[i],rotations[i]),total*.5f+.75f,clip.name+" joint "+i);
                }
                if(!clip.loop)continue;
                Assert.IsTrue(motion.Sample(clip,0,points,rotations));Vector3[] first=(Vector3[])points.Clone();
                Assert.IsTrue(motion.Sample(clip,1,points,rotations));
                for(int i=0;i<17;i++)Assert.Less((first[i]-points[i]).sqrMagnitude,1e-6f,clip.name);
            }
        }
        private static float Angle(Quaternion a,Quaternion b){return 2f*Mathf.Acos(Mathf.Clamp01(Mathf.Abs(QMath.Dot(a,b))))*Mathf.Rad2Deg;}

        // ------------------------------------------------------------------ landing detector

        private static void Fly(LandingTriggerH8S trigger,float seconds,bool unsafeState=false)
        {for(float t=0;t<seconds;t+=Dt)trigger.Step(false,unsafeState,Dt,.7f,LandingTriggerH8S.HeavyPlayLength(1.2667f));}
        private static void Ground(LandingTriggerH8S trigger,float seconds,bool unsafeState=false)
        {for(float t=0;t<seconds;t+=Dt)trigger.Step(true,unsafeState,Dt,.7f,LandingTriggerH8S.HeavyPlayLength(1.2667f));}

        [Test] public void ShortHopsKeepTheOriginalDipOnly()
        {LandingTriggerH8S trigger=new LandingTriggerH8S();Fly(trigger,.25f);Ground(trigger,Dt);Assert.IsFalse(trigger.Active);}
        [Test] public void RealAirTimeThenTouchdownPlaysAFiniteSmallLanding()
        {
            LandingTriggerH8S trigger=new LandingTriggerH8S();Fly(trigger,.5f);Assert.IsFalse(trigger.Active);
            Ground(trigger,Dt);Assert.IsTrue(trigger.Active);Assert.IsFalse(trigger.Heavy);
            Assert.Less(trigger.Weight(0),.25f,"blends in over a finite window");
            Ground(trigger,.2f);Assert.Greater(trigger.Weight(0),.4f);
            Ground(trigger,.6f);Assert.IsFalse(trigger.Active,"finite length");Assert.AreEqual(0,trigger.Weight(0));
        }
        [Test] public void LongFallsPlayTheHeavyLandingFromItsContactFrame()
        {
            LandingTriggerH8S trigger=new LandingTriggerH8S();Fly(trigger,1f);Ground(trigger,Dt);
            Assert.IsTrue(trigger.Heavy);Assert.AreEqual(LandingTriggerH8S.HeavyContactTime/1.2667f,trigger.Phase(1.2667f),.02f);
            Ground(trigger,LandingTriggerH8S.HeavyPlayLength(1.2667f)+.05f);Assert.IsFalse(trigger.Active);
        }
        [TestCase(true)] [TestCase(false)]
        public void UnsafeFlightNeverLandsAndUnsafeStatesCancelAtOnce(bool duringFlight)
        {
            LandingTriggerH8S trigger=new LandingTriggerH8S();
            if(duringFlight){Fly(trigger,.3f);Fly(trigger,Dt,true);Fly(trigger,.5f);Ground(trigger,Dt);Assert.IsFalse(trigger.Active);return;}
            Fly(trigger,.6f);Ground(trigger,Dt);Assert.IsTrue(trigger.Active);Ground(trigger,Dt,true);Assert.IsFalse(trigger.Active);Assert.AreEqual(0,trigger.Weight(0));
        }
        [Test] public void TakingOffAgainCancelsAndMovingLightensTheLanding()
        {
            LandingTriggerH8S trigger=new LandingTriggerH8S();Fly(trigger,.6f);Ground(trigger,.15f);
            Assert.Greater(trigger.Weight(0),trigger.Weight(6f));Assert.Greater(trigger.Weight(6f),0f);
            Fly(trigger,Dt);Assert.IsFalse(trigger.Active);
        }
        [Test] public void MissingClipsFallBackAndBadTimestepsAreHarmless()
        {
            LandingTriggerH8S trigger=new LandingTriggerH8S();
            for(float t=0;t<.5f;t+=Dt)trigger.Step(false,false,Dt,0,1f);trigger.Step(true,false,Dt,0,1f);Assert.IsTrue(trigger.Heavy,"small missing: heavy");
            trigger.Reset();for(float t=0;t<1f;t+=Dt)trigger.Step(false,false,Dt,.7f,0);trigger.Step(true,false,Dt,.7f,0);Assert.IsTrue(trigger.Active);Assert.IsFalse(trigger.Heavy,"heavy missing: small");
            trigger.Reset();for(float t=0;t<1f;t+=Dt)trigger.Step(false,false,Dt,0,0);trigger.Step(true,false,Dt,0,0);Assert.IsFalse(trigger.Active,"both missing: original dip only");
            trigger.Reset();trigger.Step(false,false,float.NaN,.7f,1f);trigger.Step(true,false,float.PositiveInfinity,.7f,1f);Assert.IsFalse(trigger.Active);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        public void TravelCombatGuardDodgeReloadAimAndBlocksAreUnsafe(int state)
        {
            PoseInput p=PoseInput.Idle();
            if(state==0)p.Travel=TravelPose.Fly;if(state==1)p.TravelBlend=.5f;if(state==2)p.AttackT=.2f;if(state==3)p.Guard=1;if(state==4)p.DodgeT=.1f;
            if(state==5)p.Reload=.3f;if(state==6)p.Aiming=true;if(state==7)p.StaggerT=.1f;if(state==9)p.Ready=1f;
            Assert.IsTrue(LandingTriggerH8S.Unsafe(ref p,state==8));
            PoseInput idle=PoseInput.Idle();Assert.IsFalse(LandingTriggerH8S.Unsafe(ref idle,false));
        }

        // ------------------------------------------------------------------ landing through the original pose owner

        private static HumanoidPose MalikPose(MalikDataH8 data){HumanoidPose pose=new HumanoidPose(RigLayout.From(Appearance.Malik()));pose.SetReviewMotionH8(data);return pose;}
        private static string Jump(HumanoidPose pose,float air,out float lowestHips,float after=.3f)
        {
            PoseInput p=PoseInput.Idle();for(int i=0;i<30;i++)pose.Solve(ref p,Dt);
            p.Grounded=false;for(float t=0;t<air;t+=Dt)pose.Solve(ref p,Dt);
            p.Grounded=true;string clip="";lowestHips=float.MaxValue;
            for(float t=0;t<after;t+=Dt){pose.Solve(ref p,Dt);if(pose.ReviewClipH8.Contains("Landing"))clip=pose.ReviewClipH8;lowestHips=Mathf.Min(lowestHips,pose.HipsPosition.y);}
            return clip;
        }
        [Test] public void SmallAndHeavyLandingsPlayThroughTheOriginalSolver()
        {
            float low;
            HumanoidPose pose=MalikPose(Load(CuratedPath));Assert.AreEqual(LandingTriggerH8S.SmallClip,Jump(pose,.5f,out low));
            pose=MalikPose(Load(CuratedPath));PoseInput p=PoseInput.Idle();for(int i=0;i<60;i++)pose.Solve(ref p,Dt);float standing=pose.HipsPosition.y;
            Assert.AreEqual(LandingTriggerH8S.HeavyClip,Jump(pose,1f,out low,.45f));
            Assert.Less(low,standing-.1f,"the heavy landing becomes a real crouch with the feet on the ground");
            for(int i=0;i<90;i++)pose.Solve(ref p,Dt);Assert.IsFalse(pose.LandingActiveH8S);Assert.AreEqual(standing,pose.HipsPosition.y,.03f,"recovers fully");
        }
        [Test] public void SchemaOneDataAndTheReviewSwitchKeepTheOriginalLanding()
        {
            float low;Assert.AreEqual("",Jump(MalikPose(Load(SelectedPath)),1f,out low));
            HumanoidPose pose=MalikPose(Load(CuratedPath));pose.ReviewLandingH8S=false;Assert.AreEqual("",Jump(pose,1f,out low));
        }
        [Test] public void ADodgeOrDeathBlockTakesOverALandingImmediately()
        {
            HumanoidPose pose=MalikPose(Load(CuratedPath));PoseInput p=PoseInput.Idle();for(int i=0;i<30;i++)pose.Solve(ref p,Dt);
            p.Grounded=false;for(float t=0;t<1f;t+=Dt)pose.Solve(ref p,Dt);p.Grounded=true;pose.Solve(ref p,Dt);pose.Solve(ref p,Dt);
            Assert.IsTrue(pose.LandingActiveH8S);
            p.DodgeT=.1f;pose.Solve(ref p,Dt);Assert.IsFalse(pose.LandingActiveH8S);Assert.AreEqual("H8_Dodge",pose.ReviewClipH8);
            p.DodgeT=-1;pose.Solve(ref p,Dt);Assert.IsFalse(pose.LandingActiveH8S,"an interrupted landing never resumes");
        }
        [Test] public void ReviewBlockStopsALandingAndGuardStillUsesTheBrace()
        {
            HumanoidPose pose=MalikPose(Load(CuratedPath));PoseInput p=PoseInput.Idle();for(int i=0;i<30;i++)pose.Solve(ref p,Dt);
            p.Grounded=false;for(float t=0;t<1f;t+=Dt)pose.Solve(ref p,Dt);p.Grounded=true;pose.Solve(ref p,Dt);Assert.IsTrue(pose.LandingActiveH8S);
            pose.ReviewBlockedH8=true;pose.Solve(ref p,Dt);Assert.IsFalse(pose.LandingActiveH8S);Assert.AreEqual("",pose.ReviewClipH8);
            pose.ReviewBlockedH8=false;p.Blade=true;p.Guard=1;pose.Solve(ref p,Dt);Assert.AreEqual("H8_Guard_Brace",pose.ReviewClipH8);
        }

        // ------------------------------------------------------------------ quiet listening loop

        private static HumanoidPose CastPose(){return new HumanoidPose(RigLayout.From(Appearance.Malik()));}
        [Test] public void OnlyTheReviewedNamedCastMemberIsEligible()
        {Assert.IsTrue(QuietMotionH8S.Eligible(CastIds04E.Dante));Assert.IsFalse(QuietMotionH8S.Eligible(CastIds04E.Nessa));Assert.IsFalse(QuietMotionH8S.Eligible(""));Assert.IsFalse(QuietMotionH8S.Eligible(null));}
        [Test] public void ListeningLoopBlendsInAtRestAndYieldsToWalkingAndOwners()
        {
            HumanoidPose pose=CastPose();Assert.IsTrue(pose.SetQuietMotionH8S(Load(CuratedPath),false));PoseInput p=PoseInput.Idle();
            for(int i=0;i<60;i++)pose.Solve(ref p,Dt);
            Assert.AreEqual(QuietMotionH8S.ClipName,pose.QuietClipH8S);Assert.Greater(pose.QuietWeightH8S,.9f);
            p.Velocity=new Vector3(0,0,1.2f);for(int i=0;i<15;i++)pose.Solve(ref p,Dt);Assert.AreEqual(0,pose.QuietWeightH8S,"walking takes over within a quarter second");Assert.AreEqual("",pose.QuietClipH8S);
            p.Velocity=Vector3.zero;for(int i=0;i<60;i++)pose.Solve(ref p,Dt);Assert.Greater(pose.QuietWeightH8S,.9f);
            pose.QuietBlockedH8S=true;for(int i=0;i<15;i++)pose.Solve(ref p,Dt);Assert.AreEqual(0,pose.QuietWeightH8S,"gesture or directed dialogue takes over");
            pose.QuietBlockedH8S=false;Assert.IsTrue(pose.SetQuietMotionH8S(null,false));pose.Solve(ref p,Dt);Assert.AreEqual("",pose.QuietClipH8S);
        }
        [Test] public void HeadLookKeepsTheHeadAndAHeldToolKeepsTheArms()
        {
            MalikDataH8 data=Load(CuratedPath);PoseInput p=PoseInput.Idle();
            HumanoidPose plain=CastPose(),yield=CastPose(),free=CastPose(),tool=CastPose();
            yield.SetQuietMotionH8S(data,false);yield.QuietHeadYieldH8S=true;free.SetQuietMotionH8S(data,false);tool.SetQuietMotionH8S(data,true);
            for(int i=0;i<90;i++){plain.Solve(ref p,Dt);yield.Solve(ref p,Dt);free.Solve(ref p,Dt);tool.Solve(ref p,Dt);}
            Assert.Less(Angle(plain.Local[RigLayout.Head],yield.Local[RigLayout.Head]),.5f,"head-look owns the head");
            Assert.Greater(Angle(plain.Local[RigLayout.Head],free.Local[RigLayout.Head]),1f,"the loop owns the head when nobody looks");
            Assert.Greater(Angle(plain.Local[RigLayout.ForearmR],free.Local[RigLayout.ForearmR]),10f,"free hands fold");
            Assert.Less(Angle(plain.Local[RigLayout.ForearmR],tool.Local[RigLayout.ForearmR]),Angle(plain.Local[RigLayout.ForearmR],free.Local[RigLayout.ForearmR])*.5f,"a held tool keeps the original arms");
        }
        [Test] public void DataWithoutTheListeningLoopIsRefused()
        {HumanoidPose pose=CastPose();Assert.IsFalse(pose.SetQuietMotionH8S(Load(SelectedPath),false));PoseInput p=PoseInput.Idle();pose.Solve(ref p,Dt);Assert.AreEqual("",pose.QuietClipH8S);}
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void QuietLoopIsBlockedByMovementAirSeatsCombatAndPanic(int state)
        {
            PoseInput p=PoseInput.Idle();
            if(state==0)p.Velocity=new Vector3(.5f,0,0);if(state==1)p.Grounded=false;if(state==2)p.Sit=1;if(state==3)p.AttackT=.1f;if(state==4)p.Panic=1;if(state==5)p.Ready=1;
            Assert.IsTrue(QuietMotionH8S.Blocked(ref p,false));PoseInput idle=PoseInput.Idle();Assert.IsFalse(QuietMotionH8S.Blocked(ref idle,false));Assert.IsTrue(QuietMotionH8S.Blocked(ref idle,true));
        }

        // ------------------------------------------------------------------ review findings: facing, teleport, contact, head ease

        private static float HipYaw(Vector3[] p){Vector3 line=p[14]-p[11];return Mathf.Atan2(-line.z,line.x)*Mathf.Rad2Deg;}
        [Test] public void SuppliedClipsAreTurnedToFaceForwardWhileMaliksOwnClipsAreUntouched()
        {
            MalikDataH8 data=Load(CuratedPath);MalikMotionH8 motion=new MalikMotionH8();Vector3[] points=new Vector3[17];Quaternion[] rotations=new Quaternion[17];
            MalikClipH8 listening=motion.Find(data,QuietMotionH8S.ClipName);
            Assert.IsTrue(motion.Sample(listening,.5f,points,rotations));Assert.AreEqual(13.3,HipYaw(points),1,"delivered listening loop faces off forward");
            MalikMotionH8.TurnSample(MalikMotionH8.FacingCorrection(listening),points,rotations);Assert.AreEqual(0,HipYaw(points),.5,"corrected");
            MalikClipH8 landing=motion.Find(data,LandingTriggerH8S.HeavyClip);
            for(int k=12;k<=38;k+=2)
            {
                Assert.IsTrue(motion.Sample(landing,k/38f,points,rotations));MalikMotionH8.TurnSample(MalikMotionH8.FacingCorrection(landing),points,rotations);
                Assert.Less(Mathf.Abs(HipYaw(points)),6f,"played part of the heavy landing faces forward within its own motion");
            }
            foreach(MalikClipH8 clip in data.clips)if(clip.name.StartsWith("H7_",System.StringComparison.Ordinal) || clip.name==LandingTriggerH8S.SmallClip)
                Assert.AreEqual(QMath.Identity,MalikMotionH8.FacingCorrection(clip),clip.name);
        }
        [Test] public void ATeleportForgetsTheFlight()
        {
            HumanoidPose pose=MalikPose(Load(CuratedPath));PoseInput p=PoseInput.Idle();for(int i=0;i<30;i++)pose.Solve(ref p,Dt);
            p.Grounded=false;for(float t=0;t<1f;t+=Dt)pose.Solve(ref p,Dt);
            p.RootPosition=new Vector3(120,0,40);p.Grounded=true;pose.Solve(ref p,Dt);pose.Solve(ref p,Dt);
            Assert.IsFalse(pose.LandingActiveH8S,"a fall never lands at a respawn or restart point");
        }
        [Test] public void TheHeavyTouchdownFrameIsReviewableWithinItsRange()
        {
            Assert.AreEqual(LandingTriggerH8S.MinContactTime,LandingTriggerH8S.ClampContact(0f));Assert.AreEqual(LandingTriggerH8S.MaxContactTime,LandingTriggerH8S.ClampContact(5f));
            Assert.AreEqual(LandingTriggerH8S.HeavyContactTime,LandingTriggerH8S.ClampContact(float.NaN));
            Assert.Less(LandingTriggerH8S.HeavyPlayLength(1.2667f,.8f),LandingTriggerH8S.HeavyPlayLength(1.2667f,.4f));
            float low,lowLate;
            HumanoidPose early=MalikPose(Load(CuratedPath));Jump(early,1f,out low,.45f);
            HumanoidPose late=MalikPose(Load(CuratedPath));late.LandingContactH8S=.8f;Assert.AreEqual(LandingTriggerH8S.HeavyClip,Jump(late,1f,out lowLate,.45f));
            Assert.Greater(lowLate,low+.05f,"a late touchdown frame gives a lighter landing than the default crouch");
        }
        [Test] public void HeadLookTakesTheHeadBackOverAFewFramesNotInOne()
        {
            MalikDataH8 data=Load(CuratedPath);PoseInput p=PoseInput.Idle();HumanoidPose pose=CastPose();pose.SetQuietMotionH8S(data,false);
            for(int i=0;i<90;i++)pose.Solve(ref p,Dt);
            pose.QuietHeadYieldH8S=true;pose.Solve(ref p,Dt);
            Assert.Greater(pose.QuietHeadYieldWeightH8S,0f);Assert.Less(pose.QuietHeadYieldWeightH8S,.2f,"eased, not switched");
            for(int i=0;i<30;i++)pose.Solve(ref p,Dt);Assert.AreEqual(1,pose.QuietHeadYieldWeightH8S,.001);
        }
        [Test] public void TheQuietOwnerRevalidatesDataItIsHanded()
        {
            MalikDataH8 data=Load(CuratedPath);data.clips[15].samples[3].p=new Vector3[3];HumanoidPose pose=CastPose();
            Assert.IsFalse(pose.SetQuietMotionH8S(data,false));PoseInput p=PoseInput.Idle();Assert.DoesNotThrow(()=>pose.Solve(ref p,Dt));Assert.AreEqual("",pose.QuietClipH8S);
        }

        // ------------------------------------------------------------------ review contract

        private static ImportedModel04OD Candidate(string exportPath)
        {
            ImportedBone04OD[] bones=new ImportedBone04OD[RigLayout.BoneCount];for(int i=0;i<bones.Length;i++)bones[i]=new ImportedBone04OD{SourceBone=i,TargetPath="Art/Root/bone"+i};
            string[] roles={"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"};ImportedSocket04OD[] sockets=new ImportedSocket04OD[roles.Length];for(int i=0;i<roles.Length;i++)sockets[i]=new ImportedSocket04OD{Role=roles[i],Path="Art/Root/socket_"+roles[i]};
            return new ImportedModel04OD{Identity="malik",Enabled=false,Bones=bones,Sockets=sockets,AlignBindAxesH3=true,PreserveInfluencesH5=true,ArtProof=new ImportedArtRecord04OH{AssetId=MalikReviewH8S.AssetId,Creator="Recorded legal source and original project costume",LicenseId="CC0-1.0",RightsRecordId="actual-file-lineage",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false,ExportAssetPath=exportPath,ExportSha256=new string('a',64),VisualDependencyHash=new string('b',32)}};
        }
        [Test] public void TheExactExportPathChoosesTheSelectedHumanContract()
        {
            string reason;
            Assert.IsTrue(MalikReviewH8S.CanReviewSelected(Candidate(MalikReviewH8S.ExportPath),true,out reason),reason);
            Assert.IsTrue(MalikReviewH8S.CanReviewSelected(Candidate(MalikReviewH8R.ExportPath),true,out reason),reason);
            Assert.IsFalse(MalikReviewH8S.CanReviewSelected(Candidate("Assets/NeonSyndicate/Art/Elsewhere/MalikVeyr.fbx"),true,out reason));
            Assert.IsFalse(MalikReviewH8S.CanReview(Candidate(MalikReviewH8R.ExportPath),true,out reason),"H8S contract refuses the H8R export");
            Assert.IsFalse(MalikReviewH8S.CanReviewSelected(Candidate(MalikReviewH8S.ExportPath),false,out reason),"never a player-build override");
            ImportedModel04OD enabled=Candidate(MalikReviewH8S.ExportPath);enabled.Enabled=true;Assert.IsFalse(MalikReviewH8S.CanReviewSelected(enabled,true,out reason));
            ImportedModel04OD pruned=Candidate(MalikReviewH8S.ExportPath);pruned.PreserveInfluencesH5=false;Assert.IsFalse(MalikReviewH8S.CanReviewSelected(pruned,true,out reason));
        }
        [Test] public void DeliveredCandidateReceiptMatchesTheContract()
        {
            string text=File.ReadAllText(MalikReviewH8S.Root+"candidate.json");
            StringAssert.Contains("\"assetId\": \""+MalikReviewH8S.AssetId+"\"",text);
            StringAssert.Contains("\"sourceFbx\": \""+MalikReviewH8S.ExportPath+"\"",text);
            StringAssert.Contains("\"authoringSource\": \""+MalikReviewH8S.SourcePath+"\"",text);
            StringAssert.Contains("\"ownerApproved\": false",text);StringAssert.Contains("\"enabled\": false",text);
            Assert.IsTrue(File.Exists(MalikReviewH8S.MotionPath));
        }
    }
}
