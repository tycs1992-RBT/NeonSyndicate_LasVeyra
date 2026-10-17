using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E: the Underheart bosses with the shared combat feedback. Lock-on picks the part that takes the most damage now
    /// (UBossLockRules04E), floating damage numbers reach hits on things that are not characters (DamageNumbers.ReportAt),
    /// and each fight stays within one shadowless realtime light (BossLightBudget04E), checked in the rule and in the
    /// fight sources. Engine-free.
    /// </summary>
    public sealed class BossFeedback04ETests
    {
        private static UBossPart Best(string boss, UBossDamageState s, params UBossPart[] present)
        {
            return UBossLockRules04E.BestKind(boss, new List<UBossPart>(present), s);
        }

        // ================================================================== lock point

        [Test]
        public void TheLockPointGoesToTheStandingWeakPoint()
        {
            UBossDamageState calm = new UBossDamageState();
            Assert.That(Best(UnderBossIds.ValveGuardian, calm, UBossPart.Body, UBossPart.Weak, UBossPart.Limb), Is.EqualTo(UBossPart.Weak), "the valve wheel");
            Assert.That(Best(UnderBossIds.CinderMatron, calm, UBossPart.Body, UBossPart.Shell, UBossPart.Weak, UBossPart.Limb), Is.EqualTo(UBossPart.Weak), "the ember heart");
            Assert.That(Best(UnderBossIds.Vaultbreaker, calm, UBossPart.Body, UBossPart.FrontPlate, UBossPart.Seam, UBossPart.Core, UBossPart.Limb), Is.EqualTo(UBossPart.Seam), "flank it: the seams");
        }

        [Test]
        public void AnOpenWindowOrCoreMovesTheLockPoint()
        {
            UBossDamageState core = new UBossDamageState { CoreOpen = true };
            Assert.That(Best(UnderBossIds.Vaultbreaker, core, UBossPart.Body, UBossPart.Seam, UBossPart.Core), Is.EqualTo(UBossPart.Core));
            UBossDamageState regulator = new UBossDamageState { CoreOpen = true };
            Assert.That(Best(UnderBossIds.KilnRegent, regulator, UBossPart.Body, UBossPart.Crown, UBossPart.Regulator), Is.EqualTo(UBossPart.Regulator), "the exposed regulator");
            UBossDamageState crownWindow = new UBossDamageState { WindowOpen = true, WindowFocus = UBossPart.Crown, WindowMul = 2.2f };
            Assert.That(Best(UnderBossIds.KilnRegent, crownWindow, UBossPart.Body, UBossPart.Crown, UBossPart.Regulator), Is.EqualTo(UBossPart.Crown), "the venting crown");
        }

        [Test]
        public void TheBodyWinsTiesAndAMissingPartIsNeverChosen()
        {
            UBossDamageState calm = new UBossDamageState();
            Assert.That(Best(UnderBossIds.ValveGuardian, calm), Is.EqualTo(UBossPart.Body));
            Assert.That(Best(UnderBossIds.ValveGuardian, calm, UBossPart.Body, UBossPart.Limb), Is.EqualTo(UBossPart.Body), "a limb is worse than the hull");
            Assert.That(UBossLockRules04E.BestKind(UnderBossIds.ValveGuardian, null, calm), Is.EqualTo(UBossPart.Body));
            Assert.That(Best(UnderBossIds.Vaultbreaker, new UBossDamageState(), UBossPart.Body, UBossPart.FrontPlate), Is.EqualTo(UBossPart.Body), "never the front armour");
        }

        [Test]
        public void TheCurrentLockPointIsKeptUnlessAnotherIsClearlyNearerTheAim()
        {
            Assert.That(UBossLockRules04E.KeepCurrent(12f, 5f), Is.True);
            Assert.That(UBossLockRules04E.KeepCurrent(15f, 5f), Is.True);
            Assert.That(UBossLockRules04E.KeepCurrent(15.5f, 5f), Is.False);
            Assert.That(UBossLockRules04E.Radius(3.4f), Is.EqualTo(3.4f).Within(1e-4f));
            Assert.That(UBossLockRules04E.Radius(40f), Is.EqualTo(8f), "a huge reach is kept sensible");
            Assert.That(UBossLockRules04E.Radius(float.NaN), Is.EqualTo(1f));
            Assert.That(UBossLockRules04E.Height(0.2f), Is.EqualTo(1f));
            // Scored like an engaged creature in PlayerCombat.FindLockTarget (angle * 0.6 + distance * 1.2 - 28).
            Assert.That(UBossLockRules04E.Score(20f, 5f), Is.EqualTo(20f * 0.6f + 5f * 1.2f - 28f).Within(1e-4f));
        }

        // ================================================================== damage numbers

        [Test]
        public void DamageNumbersReachHitsOnThingsThatAreNotCharacters()
        {
            bool was = Settings.DamageNumbers;
            DamageNumbers.ResetStatics();
            try
            {
                Settings.DamageNumbers = true;
                GameObject none = null;
                DamageInfo plain = DamageInfo.Make(40f, new Vector3(0f, 2f, 0f), Vector3.forward, none, DamageType.Bullet);
                DamageNumbers.ReportAt(101, new Vector3(0f, 2.3f, 0f), plain, 40f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(1));
                DamageNumbers.ReportAt(101, new Vector3(0f, 2.3f, 0f), plain, 25f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(1), "fast hits on the same part merge into one climbing number");
                DamageInfo weak = plain;
                weak.Crit = true;
                DamageNumbers.ReportAt(101, new Vector3(0f, 2.3f, 0f), weak, 60f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(2), "a weak point hit is its own yellow number");
                DamageInfo companion = plain;
                companion.Origin = DamageOrigin.Companion;
                DamageNumbers.ReportAt(202, new Vector3(3f, 2.3f, 0f), companion, 18f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(3), "companion and fused hits show too (another part, another number)");
                DamageNumbers.ReportAt(303, Vector3.zero, plain, 0f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(3), "nothing applied, nothing shown");
                Settings.DamageNumbers = false;
                DamageNumbers.ReportAt(404, Vector3.zero, plain, 30f, false);
                Assert.That(DamageNumbers.Count, Is.EqualTo(3), "the setting turns them off");
            }
            finally
            {
                Settings.DamageNumbers = was;
                DamageNumbers.ResetStatics();
            }
        }

        // ================================================================== light budget

        [Test]
        public void AFightAddsAtMostOneShadowlessLight()
        {
            Assert.That(BossLightBudget04E.MaxFightLights, Is.EqualTo(1));
            Assert.That(BossLightBudget04E.MaxShadowCastingLights, Is.EqualTo(0));
            Assert.That(BossLightBudget04E.MayCreate(0), Is.True);
            Assert.That(BossLightBudget04E.MayCreate(1), Is.False);
            Assert.That(BossLightBudget04E.ArenaTotal(false), Is.EqualTo(9), "the region's 8 practical lights and the key light");
            Assert.That(BossLightBudget04E.ArenaTotal(true), Is.EqualTo(6), "Low: 5 and the key light");
        }

        private static string BossFolder()
        {
            string resources = QuestTestData.ResourcesDir;
            if (resources == null) return null;
            string dir = Path.Combine(Path.GetDirectoryName(resources), "Scripts", "Runtime", "Creatures", "UnderBosses");
            return Directory.Exists(dir) ? dir : null;
        }

        [Test]
        public void EveryFightLightGoesThroughTheBudgetedKeyLight()
        {
            string dir = BossFolder();
            Assert.That(dir, Is.Not.Null, "the boss sources were found");
            List<string> problems = new List<string>();
            int fights = 0;
            foreach (string path in Directory.GetFiles(dir, "*.cs"))
            {
                string name = Path.GetFileName(path);
                string src = File.ReadAllText(path);
                if (src.Contains("AddComponent<Light>") && name != "UnderBossMachine.cs") problems.Add(name + " adds a Light directly");
                if (src.Contains("UnderBossMachine.Glow(") && name != "UnderBossFight.Lights.cs") problems.Add(name + " calls Glow directly (use KeyLight)");
                if (Regex.IsMatch(src, @"LightShadows\.(Hard|Soft)")) problems.Add(name + " makes a shadow-casting light");
                if (name.EndsWith("Fight.cs") && name != "UnderBossFight.cs")
                {
                    fights++;
                    int calls = Regex.Matches(src, @"\bKeyLight\(").Count;
                    if (calls > BossLightBudget04E.MaxFightLights) problems.Add(name + " asks for " + calls + " key lights");
                }
            }
            Assert.That(fights, Is.EqualTo(4), "the Valve Guardian, the Cinder Matron, the Vaultbreaker and the Kiln Regent");
            string machine = File.ReadAllText(Path.Combine(dir, "UnderBossMachine.cs"));
            Assert.That(machine, Does.Contain("LightShadows.None"), "the one place that makes a light makes it shadowless");
            Assert.That(problems, Is.Empty, string.Join("\n", problems.ToArray()));
        }
    }
}
