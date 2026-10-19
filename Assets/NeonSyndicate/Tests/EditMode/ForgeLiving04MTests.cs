using System.Collections.Generic;
using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    public sealed class ForgeLiving04MTests
    {
        [Test]
        public void EightCompleteTreatmentsHaveReachableCostsAppliedEffectsAndCorrectStations()
        {
            var ids=new HashSet<string>();foreach(RecipeDef existing in ForgeDb.All)Assert.IsTrue(ids.Add(existing.Id));
            Assert.AreEqual(8,ForgeTreatments04M.Ids.Length);
            foreach(string id in ForgeTreatments04M.Ids)
            {
                RecipeDef r=ForgeDb.Get(id);Assert.IsNotNull(r);Assert.AreEqual(RecipeKind.Brew,r.Kind);
                Assert.That(r.Seconds,Is.InRange(300,480));Assert.That(r.Cost.Cash,Is.InRange(90,150));
                Assert.AreEqual(2,r.Cost.Materials.Count);Assert.AreEqual(2,r.Effects.Length);Assert.That(r.Rules.Length,Is.GreaterThan(0));
                foreach(var material in r.Cost.Materials)
                {Assert.IsNotNull(MaterialDb.Get(material.Key));Assert.That(MaterialDb.Get(material.Key).Rarity,Is.LessThan(3));Assert.That(material.Value,Is.InRange(1,3));}
                Assert.IsFalse(ForgeMath.StationAllows(r,ForgeStation.Shrine));
                Assert.IsTrue(ForgeMath.StationAllows(r,ForgeStation.Outpost));
                Assert.AreEqual(!r.OutpostOnly,ForgeMath.StationAllows(r,ForgeStation.Forgewright));
            }
        }
        [Test]
        public void NewTreatmentReplacesItsGroupWithoutChangingLegacyBrewOrUnrelatedState()
        {
            var buffs=new Dictionary<string,float>{{"brew_warding_salve",120},{ForgeTreatments04M.Ids[0],90}};
            ForgeTreatments04M.Activate(buffs,ForgeTreatments04M.Ids[2],300);
            Assert.AreEqual(2,buffs.Count);Assert.AreEqual(120,buffs["brew_warding_salve"]);
            Assert.IsFalse(buffs.ContainsKey(ForgeTreatments04M.Ids[0]));Assert.AreEqual(300,buffs[ForgeTreatments04M.Ids[2]]);
            ForgeTreatments04M.Activate(buffs,"not_a_recipe",300);Assert.AreEqual(2,buffs.Count);
        }
        [Test]
        public void RestoredTreatmentGroupSelectsOneDeterministicallyAndKeepsOldBuffs()
        {
            var a=new Dictionary<string,float>{{ForgeTreatments04M.Ids[0],90},{ForgeTreatments04M.Ids[1],120},{"brew_frost_tea",80}};
            var b=new Dictionary<string,float>{{"brew_frost_tea",80},{ForgeTreatments04M.Ids[1],120},{ForgeTreatments04M.Ids[0],90}};
            ForgeTreatments04M.Normalize(a);ForgeTreatments04M.Normalize(b);
            Assert.AreEqual(2,a.Count);Assert.That(a,Is.EquivalentTo(b));Assert.AreEqual(120,a[ForgeTreatments04M.Ids[1]]);
            a[ForgeTreatments04M.Ids[2]]=float.PositiveInfinity;ForgeTreatments04M.Normalize(a);Assert.AreEqual(2,a.Count);
        }
        private static SkillModValues Effects(string id)
        {
            var values=new SkillModValues();foreach(SkillEffect e in ForgeDb.Get(id).Effects)
            {if(e.Multiply)values.Multiply(e.Stat,e.PerRank);else values.Add(e.Stat,e.PerRank);}values.Finish();return values;
        }
        [Test]
        public void OffensiveAndDefensiveBuildsHaveRealOpposingCosts()
        {
            var attack=Effects("brew_04m_kiln");var defense=Effects("brew_04m_canal");
            Assert.That(attack.Get(SkillStat.MeleeDamage),Is.GreaterThan(1));Assert.That(attack.Get(SkillStat.GuardCost),Is.GreaterThan(1));
            Assert.That(defense.Get(SkillStat.DamageTaken),Is.LessThan(1));Assert.That(defense.Get(SkillStat.MeleeDamage),Is.LessThan(1));
        }
        [Test]
        public void EveryTreatmentKeepsAnExplicitCostRatherThanUniversalBonusStacking()
        {
            foreach(string id in ForgeTreatments04M.Ids)
            {
                var v=Effects(id);bool benefit=false,drawback=false;
                foreach(SkillEffect e in ForgeDb.Get(id).Effects)
                {
                    bool lower=e.Stat==SkillStat.DamageTaken || e.Stat==SkillStat.GuardCost || e.Stat==SkillStat.DodgeCost || e.Stat==SkillStat.VeilDrain;
                    float actual=v.Get(e.Stat),baseline=SkillModValues.BaseOf(e.Stat);
                    benefit|=lower?actual<baseline:actual>baseline;drawback|=lower?actual>baseline:actual<baseline;
                }
                Assert.IsTrue(benefit,id);Assert.IsTrue(drawback,id);
            }
        }
        [Test]
        public void ThreeNewGuildJobsGeneratePhysicalObjectivesThroughExistingBoards()
        {
            var errors=new List<string>();var files=QuestTestData.Load<QuestFile>("Quests",errors,new QuestJsonTrace());Assert.That(errors,Is.Empty);
            var guilds=new HashSet<string>();int count=0;
            foreach(var file in files)foreach(ContractDef contract in file.Value.Contracts)
            {
                if(!contract.Id.StartsWith("c_04m_"))continue;
                count++;guilds.Add(contract.Guild);var faction=FactionDb.Get(contract.Guild);Assert.IsNotNull(faction);
                UnityEngine.Vector3 board;Assert.IsTrue(QuestPlaces.GiverPosition(TestData.City,faction.Contact,0,0,out board));
                var offer=QuestLogic.MakeContract(contract,TestData.City,board,"Guild board",77,0);Assert.IsNotNull(offer);
                Assert.That(offer.Quest.Phases[0].Steps.Count,Is.GreaterThanOrEqualTo(2));
                Assert.That(offer.Reward.Rep.Count,Is.GreaterThan(0));
                Assert.AreEqual(offer.Point,QuestLogic.MakeContract(contract,TestData.City,board,"Guild board",77,0).Point);
            }
            Assert.AreEqual(3,count);Assert.That(guilds,Is.EquivalentTo(new[]{"menders","frequency_order","forgewrights"}));
        }
    }
}
