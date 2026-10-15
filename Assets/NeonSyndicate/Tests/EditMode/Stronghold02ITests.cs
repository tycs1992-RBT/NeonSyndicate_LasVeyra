using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Local ownership and raid ledger contracts. Most cases exercise engine-free rules; asset tests need Unity.</summary>
    public sealed class Stronghold02ITests
    {
        private static StrongholdCatalog Catalog()
        {
            return new StrongholdCatalog{version="02I",sites=new[]{
                new StrongholdDefinition{id="hearthspire",title="Hearthspire Keep",home=true,anchor=new[]{1120f,0f,-740f},guardSpecies=new string[0]},
                new StrongholdDefinition{id="galehook",title="Galehook Redoubt",anchor=new[]{-1180f,0f,-710f},guardSpecies=new[]{"neon_jackal","neon_jackal"}},
                new StrongholdDefinition{id="sunscar",title="Sunscar Bastion",anchor=new[]{1190f,0f,450f},guardSpecies=new[]{"crystal_basilisk","neon_jackal"}}
            }};
        }
        private static void Win(StrongholdBook b,string id)
        {
            Assert.That(b.BeginRaid(id,2),Is.True);
            Assert.That(b.DefeatGuard(id,0),Is.True);Assert.That(b.DefeatGuard(id,1),Is.True);
            bool result=false;for(int i=0;i<50;i++)result=b.AdvanceCapture(id,.1f,true,true)||result;
            Assert.That(result,Is.True);
        }
        [Test] public void HomeIsClaimedOnceAndStartsWithoutBoosts()
        {
            StrongholdBook b=new StrongholdBook(Catalog());Assert.That(b.Tier,Is.EqualTo(0));Assert.That(b.ClaimHome(),Is.True);
            Assert.That(b.ClaimHome(),Is.False);Assert.That(b.FlightMultiplier,Is.EqualTo(1f));Assert.That(b.Tier,Is.EqualTo(1));
        }
        [Test] public void RivalRaidRequiresHomeAndMatchingRoster()
        {
            StrongholdBook b=new StrongholdBook(Catalog());Assert.That(b.BeginRaid("galehook",2),Is.False);b.ClaimHome();
            Assert.That(b.BeginRaid("galehook",1),Is.False);Assert.That(b.BeginRaid("hearthspire",2),Is.False);
            Assert.That(b.BeginRaid("unknown",2),Is.False);Assert.That(b.BeginRaid("galehook",2),Is.True);
            Assert.That(b.BeginRaid("sunscar",2),Is.False);
        }
        [Test] public void DuplicateOrForeignDeathsCannotClearWardens()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);
            Assert.That(b.DefeatGuard("sunscar",0),Is.False);Assert.That(b.DefeatGuard("galehook",3),Is.False);
            Assert.That(b.DefeatGuard("galehook",0),Is.True);Assert.That(b.DefeatGuard("galehook",0),Is.False);
            Assert.That(b.RemainingGuards,Is.EqualTo(1));Assert.That(b.AdvanceCapture("galehook",100f,true,true),Is.False);
        }
        [Test] public void CaptureNeedsFiveSafeSeconds()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);b.DefeatGuard("galehook",0);b.DefeatGuard("galehook",1);
            for(int i=0;i<49;i++)Assert.That(b.AdvanceCapture("galehook",.1f,true,true),Is.False);
            Assert.That(b.Status("galehook").Owned,Is.False);Assert.That(b.AdvanceCapture("galehook",.1f,true,true),Is.True);
        }
        [Test] public void DamageOrLeavingCoreResetsCapture()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);b.DefeatGuard("galehook",0);b.DefeatGuard("galehook",1);
            b.AdvanceCapture("galehook",.1f,true,true);b.AdvanceCapture("galehook",.1f,true,false);Assert.That(b.CaptureProgress,Is.Zero);
            b.AdvanceCapture("galehook",.1f,true,true);b.AdvanceCapture("galehook",.1f,false,true);Assert.That(b.CaptureProgress,Is.Zero);
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)] [TestCase(0f)]
        public void InvalidTimeCannotCapture(float delta)
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);b.DefeatGuard("galehook",0);b.DefeatGuard("galehook",1);
            Assert.That(b.AdvanceCapture("galehook",delta,true,true),Is.False);Assert.That(b.CaptureProgress,Is.Zero);
        }
        [Test] public void LargeDeltaIsCapped()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);b.DefeatGuard("galehook",0);b.DefeatGuard("galehook",1);
            b.AdvanceCapture("galehook",10000f,true,true);Assert.That(b.CaptureProgress,Is.EqualTo(.1f).Within(.0001f));
        }
        [Test] public void CapturesPayOnceAndInfusionIsRequired()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();Win(b,"galehook");
            Assert.That(b.Resonance,Is.Zero);Assert.That(b.PendingEchoes,Is.EqualTo(1));Assert.That(b.BeginRaid("galehook",2),Is.False);
            Assert.That(b.Infuse(),Is.EqualTo(1));Assert.That(b.FlightMultiplier,Is.EqualTo(1.04f).Within(.0001f));
            for(int i=0;i<100;i++)Assert.That(b.Infuse(),Is.Zero);
            Win(b,"sunscar");b.Infuse();Assert.That(b.Resonance,Is.EqualTo(2));Assert.That(b.FlightMultiplier,Is.EqualTo(1.08f).Within(.0001f));
            Assert.That(b.ClimbMultiplier,Is.EqualTo(1.06f).Within(.0001f));Assert.That(b.Tier,Is.EqualTo(3));
        }
        [Test] public void ConstructionIsOwnedBoundedAndLockedDuringRaids()
        {
            StrongholdBook b=new StrongholdBook(Catalog());Assert.That(b.Configure("hearthspire",0,1,0),Is.False);b.ClaimHome();
            Assert.That(b.Configure("galehook",0,1,0),Is.False);Assert.That(b.Configure("hearthspire",6,1,0),Is.False);
            Assert.That(b.Configure("hearthspire",0,5,0),Is.False);Assert.That(b.Configure("hearthspire",0,1,4),Is.False);
            for(int slot=0;slot<6;slot++)for(int type=0;type<5;type++)for(int rot=0;rot<4;rot++)
                Assert.That(b.Configure("hearthspire",slot,type,rot),Is.True);
            b.BeginRaid("galehook",2);Assert.That(b.Configure("hearthspire",0,1,0),Is.False);Assert.That(b.SetTheme("hearthspire",1),Is.False);
        }
        [Test] public void SaveIsDeepCopiedAndRestoresConstruction()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();Win(b,"galehook");b.Infuse();b.Configure("hearthspire",3,4,2);b.SetTheme("hearthspire",2);
            StrongholdSave save=b.Capture();b.Configure("hearthspire",3,0,0);
            StrongholdBook restored=new StrongholdBook(Catalog());restored.Restore(save);
            Assert.That(restored.Status("hearthspire").Slots[3].Module,Is.EqualTo(4));Assert.That(restored.Status("hearthspire").Theme,Is.EqualTo(2));
            Assert.That(restored.Resonance,Is.EqualTo(1));save.Sites[0].Slots[3].Module=0;Assert.That(restored.Status("hearthspire").Slots[3].Module,Is.EqualTo(4));
        }
        [Test] public void LoadingCannotCompleteAnInProgressRaid()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();b.BeginRaid("galehook",2);b.DefeatGuard("galehook",0);
            StrongholdSave save=b.Capture();b.Restore(save);Assert.That(b.ActiveRaid,Is.Null);Assert.That(b.Status("galehook").Owned,Is.False);
        }
        [Test] public void OldAndMalformedSaveResetsOrSanitizes()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.Restore(null);Assert.That(b.Tier,Is.Zero);
            StrongholdSave s=new StrongholdSave();s.Sites.Add(new StrongholdSiteSave{Id="sunscar",Owned=true,EchoInfused=true});
            s.Sites.Add(new StrongholdSiteSave{Id="invalid",Owned=true});b.Restore(s);Assert.That(b.Resonance,Is.Zero);
            s.Sites.Insert(0,new StrongholdSiteSave{Id="hearthspire",Owned=true,Theme=99,Slots=new List<FortSlotSave>{new FortSlotSave{Module=99,Rotation=-1}}});
            b.Restore(s);Assert.That(b.Status("hearthspire").Theme,Is.EqualTo(2));Assert.That(b.Status("hearthspire").Slots[0].Module,Is.EqualTo(4));
            Assert.That(b.Status("hearthspire").Slots[0].Rotation,Is.Zero);
        }
        [Test] public void BadCatalogsFailBeforeWorldCreation()
        {
            StrongholdCatalog c=Catalog();c.sites[1].healthScale=float.NaN;Assert.Throws<InvalidOperationException>(()=>c.Validate());
            c=Catalog();c.sites[0].guardSpecies=new[]{"neon_jackal"};Assert.Throws<InvalidOperationException>(()=>c.Validate());
            c=Catalog();c.sites[1].anchor[0]=float.PositiveInfinity;Assert.Throws<InvalidOperationException>(()=>c.Validate());
        }
        [Test] public void PlotCandidatesStayDeterministicAndSocketsInsideFort()
        {
            List<Vector3> a=StrongholdPlacement.Candidates(Catalog().sites[0]),b=StrongholdPlacement.Candidates(Catalog().sites[0]);
            Assert.That(a.Count,Is.EqualTo(169));Assert.That(new HashSet<Vector3>(a).Count,Is.EqualTo(a.Count));
            for(int i=0;i<a.Count;i++)Assert.That(a[i],Is.EqualTo(b[i]));
            for(int i=0;i<6;i++){Vector3 v=StrongholdPlacement.Socket(i);Assert.That(Mathf.Abs(v.x)+3.4f,Is.LessThan(16f));Assert.That(Mathf.Abs(v.z)+3.4f,Is.LessThan(16f));}
            Assert.That(StrongholdPlacement.RegionAllowed(Vector3.zero),Is.False);
        }
        [Test]
        [Category(TestData.NeedsEngine)]
        public void AllShippedBinaryMeshesAreReadableAndNonempty()
        {
            string[] names={"foundation","gate","gate_trim","tower","wall","dais","trellis","podium","heart","spire","banner","inlay","canopy"};
            using(StrongholdAssets assets=new StrongholdAssets())
                foreach(string name in names)for(int lod=0;lod<2;lod++)
                {
                    Mesh mesh=assets.Model(name,lod==1);Assert.That(mesh,Is.Not.Null);Assert.That(mesh.isReadable,Is.True);Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                    Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));Assert.That(assets.Model(name,lod==1),Is.SameAs(mesh));
                }
        }
        [Test]
        [Category(TestData.NeedsEngine)]
        public void NewBlockRoundTripsWithoutLosingEchoes()
        {
            StrongholdBook b=new StrongholdBook(Catalog());b.ClaimHome();Win(b,"sunscar");
            SaveData save=new SaveData{Strongholds=b.Capture()};SaveData loaded=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            b.Restore(loaded.Strongholds);Assert.That(b.PendingEchoes,Is.EqualTo(1));Assert.That(b.Infuse(),Is.EqualTo(1));
        }
    }
}
