using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class AscensionEpisode04ITests
    {
        private static Dictionary<string,QuestDef> Chapters()
        {
            List<string> errors=new List<string>();
            List<KeyValuePair<string,QuestFile>> files=QuestTestData.Load<QuestFile>("Quests",errors,new QuestJsonTrace());
            Assert.IsEmpty(errors);
            Dictionary<string,QuestDef> chapters=new Dictionary<string,QuestDef>();
            foreach(KeyValuePair<string,QuestFile> f in files) foreach(QuestDef q in f.Value.Quests)
                if(q.Id.StartsWith("uh05_04i_",StringComparison.Ordinal)) chapters.Add(q.Id,q);
            return chapters;
        }
        [Test] public void FiveChaptersHaveConnectedPrerequisitesAndRealCheckpointedWork()
        {
            Dictionary<string,QuestDef> chapters=Chapters();Assert.AreEqual(5,chapters.Count);
            string[] ids={"uh05_04i_01_ledger","uh05_04i_02_passage","uh05_04i_03_habitat","uh05_04i_04_recovery","uh05_04i_05_ascension"};
            int work=0,checks=0;
            for(int i=0;i<ids.Length;i++)
            {
                QuestDef q=chapters[ids[i]];Assert.AreEqual("Side",q.Kind);
                Assert.AreEqual(i==0?0:1,q.Requires.Quests.Count);
                if(i>0) Assert.AreEqual(ids[i-1],q.Requires.Quests[0]);
                Assert.IsFalse(q.Phases[0].Checkpoint);
                foreach(QuestPhase phase in q.Phases)
                {
                    if(phase.Checkpoint){checks++;Assert.IsNotEmpty(phase.ResumeAt);}
                    foreach(QuestStep step in phase.Steps)
                    {if(step.Type!="Talk" && step.Type!="GoTo")work++;Assert.IsFalse(step.Type=="Give" || step.Type=="GiveGun");}
                }
                Assert.That(q.Reward.Cash,Is.InRange(100,200));
                Assert.IsTrue(GuidanceRules04F.IsInteriorContract(q.Id));
            }
            Assert.GreaterOrEqual(work,15);Assert.GreaterOrEqual(checks,12);
        }
        [Test] public void HandlerRequestCannotAuthorizeAnotherQuestOrIncident()
        {
            Assert.IsTrue(CityIncidentRules04I.Authorized("uh05_04i_02_passage","ev_04f_street_drake"));
            Assert.IsFalse(CityIncidentRules04I.Authorized("uh05_04g_01_courier","ev_04f_street_drake"));
            Assert.IsFalse(CityIncidentRules04I.Authorized("uh05_04i_02_passage","ev_04f_crystal_dispute"));
            Assert.IsFalse(CityIncidentRules04I.Authorized(null,null));
        }
        [Test] public void NewRecipesUseRealCostsAndExactCompletedChapterUnlocks()
        {
            foreach(string id in new[]{"brew_ascendant_edge_04i","brew_cityward_04i"})
            {
                RecipeDef recipe=ForgeDb.Get(id);Assert.NotNull(recipe);
                Assert.AreEqual(RecipeKind.Brew,recipe.Kind);Assert.AreEqual(480f,recipe.Seconds);
                Assert.GreaterOrEqual(recipe.Cost.Cash,140);Assert.Greater(recipe.Cost.Materials.Count,1);
                Assert.AreEqual(UnlockKind.QuestDone,recipe.Rules[0].Kind);
                Assert.IsTrue(Chapters().ContainsKey(recipe.Rules[0].Arg));
            }
        }
    }
}
