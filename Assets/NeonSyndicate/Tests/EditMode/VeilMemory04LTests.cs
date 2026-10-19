using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class VeilMemory04LTests
    {
        [Test]
        public void InvestigationHasPhysicalEvidenceFiniteWitnessEarnedCareAndARealExit()
        {
            var errors=new List<string>();QuestDef quest=null;
            foreach(var file in QuestTestData.Load<QuestFile>("Quests",errors,new QuestJsonTrace()))
                foreach(QuestDef q in file.Value.Quests)if(q.Id==VeilMemoryRules04L.Quest)quest=q;
            Assert.IsEmpty(errors);Assert.IsNotNull(quest);Assert.AreEqual("Side",quest.Kind);
            int fight=0,carry=0,assist=0,care=0,interact=0,checkpoint=0;
            foreach(QuestPhase ph in quest.Phases)
            {
                if(ph.Checkpoint){checkpoint++;Assert.IsNotEmpty(ph.ResumeAt);}
                foreach(QuestStep s in ph.Steps)
                {
                    Assert.IsFalse(s.Type=="GiveGun"||s.Type=="Rescue"||s.Type=="BondRank"||s.Type=="Fuse"||s.Type=="Give");
                    if(s.Type=="Skirmish")
                    {
                        fight++;Assert.IsTrue(VeilMemoryRules04L.Authorized(quest.Id,s.At));
                        Assert.AreEqual(0,s.Reinforce);int count=0;foreach(var r in s.Roster)count+=r.Count;Assert.AreEqual(4,count);
                    }
                    if(s.Type=="Courier"){carry++;Assert.AreNotEqual(s.At,s.To);}
                    if(s.Type=="Assist")assist++;if(s.Type=="Care")care++;if(s.Type=="Interact")interact++;
                }
            }
            Assert.AreEqual(1,fight);Assert.AreEqual(2,carry);Assert.AreEqual(1,assist);Assert.AreEqual(1,care);
            Assert.That(interact,Is.GreaterThanOrEqualTo(5));Assert.That(checkpoint,Is.GreaterThanOrEqualTo(5));
            var last=quest.Phases[quest.Phases.Count-1].Steps;Assert.AreEqual("GoTo",last[last.Count-1].Type);
            Assert.AreEqual("Venue:dispatch.exit",last[last.Count-1].At);
            Assert.That(quest.Reward.Cash,Is.InRange(100,240));Assert.That(quest.Reward.Lumen,Is.InRange(20,80));
            Assert.IsFalse(VeilMemoryRules04L.Authorized(VanguardRules04K.Quest,VeilMemoryRules04L.Arena));
            Assert.IsFalse(VeilMemoryRules04L.Owns("ambient"));
        }
    }
}
