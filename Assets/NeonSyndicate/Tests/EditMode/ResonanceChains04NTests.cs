using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    public sealed class ResonanceChains04NTests
    {
        private static ChainRecipe04N Hit(ResonanceChains04N b,ChainAction04N a,int serial,float t,int target=7)
        {return b.Hit(a,serial,target,t,10,true,false);}
        [Test] public void ActualGunGunMeleeMatchesAndConsumesOnlyOnce()
        {
            var b=new ResonanceChains04N();Assert.IsNull(Hit(b,ChainAction04N.Gun,1,0));Assert.IsNull(Hit(b,ChainAction04N.Gun,2,.5f));
            Assert.AreEqual("twin_fang",Hit(b,ChainAction04N.Melee,3,1).Id);
            Assert.IsFalse(b.Consume(2,8));Assert.IsTrue(b.Consume(2,7));Assert.IsFalse(b.Consume(2,7));
        }
        [Test] public void PelletsTargetsAndBeamTicksCannotCountOneAttackTwice()
        {
            var b=new ResonanceChains04N();Hit(b,ChainAction04N.Gun,1,0);Hit(b,ChainAction04N.Gun,1,.1f,8);
            Assert.IsNull(Hit(b,ChainAction04N.Melee,2,.5f));Assert.IsFalse(b.Available(.6f));
        }
        [Test] public void WrongOrderAndExpiredIntervalsDoNotMatch()
        {
            var b=new ResonanceChains04N();Hit(b,ChainAction04N.Melee,1,0);Hit(b,ChainAction04N.Melee,2,.2f);
            Assert.IsNull(Hit(b,ChainAction04N.Gun,3,.5f));Hit(b,ChainAction04N.Gun,4,8);
            Assert.IsNull(Hit(b,ChainAction04N.Melee,5,9));
        }
        [Test] public void InvalidOrProtectedDamageNeverEntersTheChain()
        {
            var b=new ResonanceChains04N();Assert.IsNull(b.Hit(ChainAction04N.Gun,1,7,0,0,true,false));
            Assert.IsNull(b.Hit(ChainAction04N.Gun,2,7,0,20,false,false));Assert.IsNull(b.Hit(ChainAction04N.Gun,3,7,0,20,true,true));
            Assert.IsNull(Hit(b,ChainAction04N.Melee,4,1));Assert.IsFalse(b.Available(1));
        }
        [Test] public void ReadyExpiresAndEachLifeHasAFiniteChainBudget()
        {
            var b=new ResonanceChains04N();int serial=0;
            for(int n=0;n<3;n++)
            {float t=n*8;Hit(b,ChainAction04N.Gun,++serial,t);Hit(b,ChainAction04N.Gun,++serial,t+.5f);var r=Hit(b,ChainAction04N.Melee,++serial,t+1);Assert.AreEqual(n<2,r!=null);Assert.IsFalse(b.Available(t+7));}
        }
        [Test] public void EarnedFocusSnapshotSurvivesWeaponSwitchAndClearsOnReset()
        {
            var b=new ResonanceChains04N();Hit(b,ChainAction04N.Gun,1,0);Hit(b,ChainAction04N.Gun,2,.3f);
            b.Hit(ChainAction04N.Focus,3,7,.6f,20,true,false,CrystalBehavior.PrismThread,52);
            Assert.AreEqual("prism_volley",Hit(b,ChainAction04N.Gun,4,1).Id);Assert.AreEqual(52,b.FocusUid);
            b.Reset();Assert.AreEqual(0,b.FocusUid);Assert.IsFalse(b.Available(1));
        }
        [TestCase("quartz_guide")] [TestCase("ash_warden")] [TestCase("storm_skimmer")]
        public void SynergiesRequireEarnedRankBodyStateAndCooldown(string id)
        {
            Assert.IsTrue(ResonanceChains04N.PartnerEligible(id,BondIds.RankBonded,(int)CompanionState.Following,true,false,0));
            Assert.IsFalse(ResonanceChains04N.PartnerEligible(id,BondIds.RankStray,(int)CompanionState.Following,true,false,0));
            Assert.IsFalse(ResonanceChains04N.PartnerEligible(id,BondIds.RankBonded,(int)CompanionState.Downed,true,false,0));
            Assert.IsFalse(ResonanceChains04N.PartnerEligible(id,BondIds.RankBonded,(int)CompanionState.Following,false,false,0));
            Assert.IsFalse(ResonanceChains04N.PartnerEligible(id,BondIds.RankBonded,(int)CompanionState.Following,true,true,0));
            Assert.IsFalse(ResonanceChains04N.PartnerEligible(id,BondIds.RankBonded,(int)CompanionState.Following,true,false,1));
        }
        [Test] public void WildCreatureCannotBeUsedAsABondedPartner()
        {Assert.IsFalse(ResonanceChains04N.PartnerEligible("neon_jackal",9,(int)CompanionState.Following,true,false,0));}
        [Test] public void PromptPauseMissConfirmationAndCancellationAreTerminal()
        {
            var w=new SynergyWindow04N();w.Begin(false);w.Tick(.3f,true,true,false);Assert.AreEqual(0,w.Time);Assert.IsFalse(w.Confirmed);
            w.Tick(.3f,false,true,false);bool enhanced;Assert.IsTrue(w.Resolve(out enhanced));Assert.IsTrue(enhanced);Assert.IsFalse(w.Resolve(out enhanced));
            w.Begin(false);w.Tick(1.1f,false,true,false);Assert.IsTrue(w.Resolve(out enhanced));Assert.IsFalse(enhanced);
            w.Begin(true);w.Tick(1.1f,false,true,false);Assert.IsTrue(w.Resolve(out enhanced));Assert.IsTrue(enhanced);
            w.Begin(false);w.Tick(.3f,false,false,true);w.Cancel();Assert.IsFalse(w.Resolve(out enhanced));
        }
        [Test] public void ResonanceCostAndChainCreditUseOriginalBoundedReservoir()
        {
            var c=new ResonanceCharge04F();c.Restore(0);Assert.IsFalse(c.Spend(40));c.CreditChain04N(float.NaN);Assert.AreEqual(0,c.Value);
            for(int i=0;i<30;i++)c.CreditChain04N(100);Assert.AreEqual(100,c.Value);Assert.IsTrue(c.Spend(40));Assert.AreEqual(60,c.Value);
            c.Restore(0);Assert.IsFalse(c.Spend(40));
        }
        [Test] public void MasteryUsesFinitePerPartnerReceiptsAndNeverRaisesRankOrGrantsAFocus()
        {
            var r=new CompanionRecord{Uid=5,Archetype="quartz_guide",Rank=BondIds.RankHarmonized};
            Assert.IsFalse(ResonanceChains04N.Eclipse(r,CrystalBehavior.Shardwake));
            for(int i=0;i<3;i++){Assert.IsTrue(ResonanceChains04N.CreditMastery(r,"twin_fang"));Assert.IsTrue(ResonanceChains04N.CreditMastery(r,"prism_volley"));}
            Assert.IsFalse(ResonanceChains04N.CreditMastery(r,"twin_fang"));Assert.AreEqual(6,r.Milestones.Count);
            Assert.IsTrue(ResonanceChains04N.Eclipse(r,CrystalBehavior.Shardwake));Assert.IsFalse(ResonanceChains04N.Eclipse(r,CrystalBehavior.None));
            Assert.AreEqual(BondIds.RankHarmonized,r.Rank);Assert.IsFalse(ResonanceChains04N.Eclipse(new CompanionRecord(),CrystalBehavior.Shardwake));
        }
        [Test] public void PartnersHaveDifferentBoundedDamageAndBossAndMissFallbacks()
        {
            var amounts=new System.Collections.Generic.HashSet<float>();
            foreach(var id in new[]{"quartz_guide","ash_warden","storm_skimmer"})
            {float full=ResonanceChains04N.FinisherDamage(id,true,false,false);amounts.Add(full);
                Assert.That(ResonanceChains04N.FinisherDamage(id,false,false,false),Is.GreaterThan(0).And.LessThan(full));
                Assert.That(ResonanceChains04N.FinisherDamage(id,true,true,true),Is.LessThan(full));}
            Assert.AreEqual(3,amounts.Count);
        }
        [TestCase(GameState.Playing,true,true,false)]
        [TestCase(GameState.Cutscene,false,true,true)]
        [TestCase(GameState.Loading,true,true,true)]
        [TestCase(GameState.Playing,true,false,true)]
        public void SceneAndDeathCancellationPrecedeZeroDeltaPauseHandling(GameState state,bool paused,bool alive,bool cancel)
        {Assert.AreEqual(cancel,SynergyWindow04N.CancelForState(state,paused,alive));}
    }
}
