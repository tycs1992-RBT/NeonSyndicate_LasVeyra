using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Both creature rigs must honor the same exhibition/practice reward contract.</summary>
    public sealed class CreatureNoRewardsTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        [Category(TestData.NeedsEngine)]
        public void RewardlessBodiesDoNotEmitQuestCountingKills(bool humanoid, bool noRewards)
        {
            GameObject directorRoot = new GameObject("NoRewards director test");
            GameObject bodyRoot = new GameObject("NoRewards body test");
            directorRoot.SetActive(false);
            bodyRoot.SetActive(false);
            try
            {
                CreatureDirector director = directorRoot.AddComponent<CreatureDirector>();
                CreatureSpawnOptions options = new CreatureSpawnOptions { NoRewards = noRewards };
                CharacterBase body;
                if (humanoid)
                {
                    NpcController npc = bodyRoot.AddComponent<NpcController>();
                    npc.CreatureOptions = options;
                    body = npc;
                }
                else
                {
                    Creature creature = bodyRoot.AddComponent<Creature>();
                    creature.Options = options;
                    body = creature;
                }
                int notices = 0;
                director.Killed += delegate(CreatureKill notice) { notices++; Assert.That(notice.Body, Is.SameAs(body)); };
                // Null species and a non-player cause avoid touching any live game economy or progression.
                director.ReportDeath(body, null, false, default(DamageInfo));
                Assert.That(notices, Is.EqualTo(noRewards ? 0 : 1));
            }
            finally
            {
                Object.DestroyImmediate(bodyRoot);
                Object.DestroyImmediate(directorRoot);
            }
        }
    }
}
