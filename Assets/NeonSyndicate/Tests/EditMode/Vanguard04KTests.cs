using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class Vanguard04KTests
    {
        [Test]
        public void ChampionNeverShortensTheActiveWindowOrRemovesThePunishWindow()
        {
            AttackTiming normal=new AttackTiming{Windup=.3f,Active=.2f,Recovery=.4f};
            AttackTiming calm=VanguardRules04K.Timing(normal,1f),last=VanguardRules04K.Timing(normal,.4f);
            Assert.AreEqual(1.05f,calm.Windup);Assert.AreEqual(.85f,last.Windup);
            Assert.AreEqual(normal.Active,last.Active);Assert.GreaterOrEqual(last.Recovery,1.05f);
            Assert.AreEqual(1.35f,VanguardRules04K.ExposedMultiplier(2));
            Assert.AreEqual(1f,VanguardRules04K.ExposedMultiplier(0));
            Assert.IsFalse(VanguardRules04K.Authorized("uh05_04i_04_recovery",VanguardRules04K.Arena));
        }

        private sealed class Sink:IRewardSink04E
        {
            public bool Full=true;public int Applied;
            public bool Deliver(RewardEntrySave e,RewardDef04E d,RewardOffer04E o,out int uid,out string why)
            {uid=Full?0:701;why=Full?"full":"ok";if(!Full)Applied++;return !Full;}
        }

        [Test]
        public void FullBackpackThenReloadCannotLoseOrDuplicateTheEarnedCaster()
        {
            const string token="quest:uh05_04k_01_fractured_bell:caster";
            RewardLedgerCore04E ledger=new RewardLedgerCore04E();Sink sink=new Sink();string message;
            Assert.IsTrue(ledger.Grant(VanguardRules04K.Reward,token,"bell",4,8,""));
            Assert.AreEqual(RewardClaimResult04E.StillPending,ledger.TryClaim(token,0,sink,out message));
            RewardLedgerCore04E restored=new RewardLedgerCore04E();restored.Load(ledger.Entries);
            Assert.IsFalse(restored.Grant(VanguardRules04K.Reward,token,"bell",20,30,""));
            sink.Full=false;
            Assert.AreEqual(RewardClaimResult04E.Claimed,restored.TryClaim(token,0,sink,out message));
            Assert.AreEqual(RewardClaimResult04E.AlreadyClaimed,restored.TryClaim(token,0,sink,out message));
            Assert.AreEqual(1,sink.Applied);Assert.AreEqual(701,restored.Find(token).DeliveredUid);
            RewardOffer04E offer=RewardCatalog04E.Offers(VanguardRules04K.Reward,61724,4,8,"")[0];
            LootGun gun=offer.Parts[0].BuildGun();
            Assert.AreEqual(LootArchetype.CrystalCaster,gun.Archetype);
            Assert.Less(gun.Unique,17);Assert.AreEqual(Rarity.Epic,gun.Rarity);
        }

        [Test]
        public void ExpeditionDeclaresOneChampionAndNoReinforcementsOrBondGrants()
        {
            List<string> errors=new List<string>();QuestDef quest=null;
            foreach(var file in QuestTestData.Load<QuestFile>("Quests",errors,new QuestJsonTrace()))
                foreach(QuestDef q in file.Value.Quests) if(q.Id==VanguardRules04K.Quest) quest=q;
            Assert.IsEmpty(errors);Assert.NotNull(quest);
            int champions=0,claims=0,checkpoints=0;
            foreach(QuestPhase phase in quest.Phases)
            {
                if(phase.Checkpoint){checkpoints++;Assert.IsNotEmpty(phase.ResumeAt);}
                foreach(QuestStep s in phase.Steps)
                {
                    Assert.IsFalse(s.Type=="GiveGun"||s.Type=="Rescue"||s.Type=="BondRank"||s.Type=="Fuse");
                    if(s.Type=="Skirmish") Assert.AreEqual(0,s.Reinforce);
                    if(s.Type=="Skirmish" && s.At==VanguardRules04K.Arena)
                    {champions++;Assert.AreEqual(1,s.Roster.Count);Assert.AreEqual(1,s.Roster[0].Count);Assert.AreEqual("quartz_drake",s.Roster[0].Species);}
                    if(s.Type=="Claim"){claims++;Assert.AreEqual(VanguardRules04K.Reward,s.Reward);}
                }
            }
            Assert.AreEqual(1,champions);Assert.AreEqual(1,claims);Assert.GreaterOrEqual(checkpoints,5);
            QuestStep exit=quest.Phases[quest.Phases.Count-1].Steps[quest.Phases[quest.Phases.Count-1].Steps.Count-1];
            Assert.AreEqual("Venue:dispatch.exit",exit.At);
            foreach(string id in new[]{"brew_vanguard_edge_04k","brew_bell_ward_04k"})
            {RecipeDef r=ForgeDb.Get(id);Assert.Greater(r.Cost.Cash,0);Assert.Greater(r.Cost.Materials.Count,0);Assert.AreEqual(VanguardRules04K.Quest,r.Rules[0].Arg);}
        }

        [TestCase("dispatch")][TestCase("pressure")]
        public void NewInteriorGeometryRespectsTheOriginalOccupiedFloor(string id)
        {
            foreach(bool near in new[]{true,false})
            {
                MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1);CityMotionGeometry04K.Venue(id,a,b,near);
                Assert.That(a.VertexCount+b.VertexCount,Is.InRange(100,1300));
                foreach(MeshBuilder m in new[]{a,b})foreach(Vector3 v in m.Vertices)
                {
                    Assert.IsTrue(MathUtil.IsFinite(v));
                    if(Mathf.Abs(v.x)<7.3f)Assert.GreaterOrEqual(v.y,3.4f,"Door/stair/receiver routes stay clear");
                }
            }
        }
    }
}
