using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class FrontierContinuation04OGTests
    {
        [Test] public void BlockedForagersCannotWalkForeverAndStormsDoNotRestartTheReturnClock()
        {
            FrontierForage04OG cycle=new FrontierForage04OG();cycle.Tick(14,false,false,false,false);
            Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Return));
            for(int i=0;i<14;i++)cycle.Tick(1,false,false,false,true);
            Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(20,false,false,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(1,false,false,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Travel));
        }
        [Test] public void NearbyViolenceOverridesWatchingWithoutProvokingAnAttack()
        {
            FrontierForage04OG cycle=new FrontierForage04OG();cycle.Tick(1,false,false,true,false);
            Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Watch));
            cycle.Tick(1,false,false,true,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Return));
            cycle.Tick(1,false,true,false,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(20,false,true,false,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(0,true,true,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
        }
        [Test] public void MineralDestinationUsesAnActualNearbyDepositWithClearance()
        {
            Vector3 home=new Vector3(0,.15f,0),goal;
            List<WildScenery04OC> scenery=new List<WildScenery04OC>{null,
                new WildScenery04OC{Kind=0,Position=Vector3.right,Size=Vector3.one},
                new WildScenery04OC{Kind=1,Position=new Vector3(5,.15f,0),Size=new Vector3(2,3,2)},
                new WildScenery04OC{Kind=1,Position=new Vector3(100,.15f,0),Size=Vector3.one}};
            Assert.That(FrontierHabitat04OG.Deposit(home,scenery,out goal),Is.True);
            Assert.That(goal.x,Is.InRange(1.5f,3f));Assert.That(goal.y,Is.EqualTo(home.y));
            Assert.That(MathUtil.FlatDistance(goal,scenery[2].Position),Is.GreaterThan(2));
            scenery.RemoveAt(2);Assert.That(FrontierHabitat04OG.Deposit(home,scenery,out goal),Is.False);
            Assert.That(FrontierHabitat04OG.Deposit(home,null,out goal),Is.False);
        }
        [TestCase(1)] [TestCase(2)]
        public void PartnerBeatHasOneVisibleContactAndFiniteRecovery(int kind)
        {
            Assert.That(FrontierPerformance04OG.ArtBeat(kind,.85f).y,Is.EqualTo(1).Within(.0001f));
            Assert.That(FrontierPerformance04OG.ArtBeat(kind,.99f).z,Is.LessThan(.05f));
            Assert.That(FrontierPerformance04OG.ArtBeat(kind,1),Is.EqualTo(Vector3.zero));
            Assert.That(FrontierPerformance04OG.ArtBeat(kind,float.NaN),Is.EqualTo(Vector3.zero));
            Assert.That(FrontierPerformance04OG.ArtBeat(0,.85f),Is.EqualTo(Vector3.zero));
            for(int i=0;i<100;i++)
            {Vector3 beat=FrontierPerformance04OG.ArtBeat(kind,i/100f);Assert.That(beat.x,Is.InRange(0,1));Assert.That(beat.y,Is.InRange(0,1));Assert.That(beat.z,Is.InRange(0,1));}
        }
        [TestCase("ash_warden",1,false)] [TestCase("storm_skimmer",2,true)] [TestCase("storm_skimmer",2,false)]
        public void ActualCompanionSkeletonChangesDuringArtAndRecoversWithoutALock(string archetype,int kind,bool flying)
        {
            CreatureBody body=CompanionBodyFactory.BuildNow(archetype,37,1,"",0);
            CreaturePose art=new CreaturePose(body),reference=new CreaturePose(body);float maximum=0;
            for(int frame=0;frame<180;frame++)
            {
                CreaturePoseInput a=CreaturePoseInput.Idle(Vector3.zero,0);a.Flying=flying;a.Grounded=!flying;a.Altitude=flying?4:0;
                if(frame<120){a.Action=kind==1?CreatureAction.Charge:CreatureAction.Dive;a.ActionT=frame/120f;a.Windup=.72f;a.Strike=.85f;}
                CreaturePoseInput b=a;a.RegionalArt04OG=kind;
                art.Solve(ref a,1f/60f);reference.Solve(ref b,1f/60f);
                for(int bone=0;bone<art.LocalRotations.Length;bone++)
                {
                    Quaternion q=art.LocalRotations[bone];Assert.That(TestData.Finite(q),Is.True);
                    float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;Assert.That(norm,Is.EqualTo(1).Within(.015f));
                    Assert.That(MathUtil.IsFinite(art.LocalPositions[bone]),Is.True);
                    float delta=2*Mathf.Acos(Mathf.Clamp(Mathf.Abs(QMath.Dot(q,reference.LocalRotations[bone])),0,1))*Mathf.Rad2Deg;
                    if(frame<120)maximum=Mathf.Max(maximum,delta);
                }
            }
            Assert.That(maximum,Is.GreaterThan(.5f),"The real solved bones must move, not just a helper vector");
            for(int bone=0;bone<art.LocalRotations.Length;bone++)
            {float dot=Mathf.Abs(QMath.Dot(art.LocalRotations[bone],reference.LocalRotations[bone]));Assert.That(dot,Is.GreaterThan(.999f),"Action cancellation and recovery release the skeleton");}
        }
        [Test] public void RegionalSoundBedsAreDistinctBoundedAndLoopWithoutAClick()
        {
            float[] oath=FrontierSound04OG.Make(false),storm=FrontierSound04OG.Make(true);
            Assert.That(oath.Length,Is.EqualTo(AudioSynth.Rate*4));Assert.That(storm.Length,Is.EqualTo(oath.Length));
            double difference=0;
            foreach(float[] samples in new[]{oath,storm})
            {
                double power=0;float peak=0;
                foreach(float sample in samples){Assert.That(float.IsNaN(sample)||float.IsInfinity(sample),Is.False);peak=Mathf.Max(peak,Mathf.Abs(sample));power+=sample*sample;}
                Assert.That(peak,Is.InRange(.08f,.20f));Assert.That(Math.Sqrt(power/samples.Length),Is.InRange(.025,.1));
                Assert.That(Mathf.Abs(samples[0]-samples[samples.Length-1]),Is.LessThan(.012f));
            }
            for(int i=0;i<oath.Length;i++)difference+=Math.Abs(oath[i]-storm[i]);
            Assert.That(difference/oath.Length,Is.GreaterThan(.02));
        }
        [Test] public void RegionalAudioHasFinitePhysicalReachAndCanBeSilenced()
        {
            Assert.That(FrontierSound04OG.Gain(true,Vector3.zero,Vector3.zero),Is.EqualTo(1));
            Assert.That(FrontierSound04OG.Gain(true,Vector3.right*56,Vector3.zero),Is.EqualTo(.5f).Within(.001f));
            Assert.That(FrontierSound04OG.Gain(true,Vector3.up*71,Vector3.zero),Is.Zero);
            Assert.That(FrontierSound04OG.Gain(false,Vector3.zero,Vector3.zero),Is.Zero);
            Assert.That(FrontierSound04OG.Gain(true,new Vector3(float.NaN,0,0),Vector3.zero),Is.Zero);
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void FrontierMapGoalsKeepActualElevationAndOriginalPlaceIdentity(int seed)
        {
            Assert.That(FrontierLandmarks04OG.Build(null),Is.Empty);
            CityLayout layout=new CityLayout(seed);layout.Generate();
            Assert.That(FrontierLandmarks04OG.Build(layout),Is.Empty);
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            layout.ApplyProfile04O(CityRevision04O.LivingFrontier04OG,venues);
            FrontierLandmark04OG[] places=FrontierLandmarks04OG.Build(layout);Assert.That(places.Length,Is.EqualTo(5));
            Assert.That(places.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(5));
            Assert.That(places.Select(p=>p.Shape).Distinct().Count(),Is.EqualTo(3));
            foreach(FrontierLandmark04OG place in places)
            {Vector3 point;Assert.That(layout.ResolveRealmAnchor04OC("Realm:"+place.Id,out point),Is.True);Assert.That(place.Position,Is.EqualTo(point));Assert.That(layout.FindWalkRoute04O(layout.FindPoi("Veyra Lofts").Position,point).Count,Is.GreaterThan(1));}
            Assert.That(places[0].Position.y,Is.EqualTo(4.15f).Within(.001f));Assert.That(places[2].Position.y,Is.EqualTo(4.15f).Within(.001f));
        }
    }
}
