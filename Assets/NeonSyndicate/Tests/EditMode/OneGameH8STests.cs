using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// One unified game (owner decision 2026-10-23): every title button plays world revision 10, Continue and the default
    /// Load list read bank 10, and every older bank stays reachable from Load Game without any save being changed.
    /// </summary>
    public sealed class OneGameH8STests
    {
        [Test]
        public void TheTitlePlaysTheNewestKnownWorld()
        {
            Assert.That((int)OneGameH8S.World, Is.EqualTo(10));
            Assert.That(CityRebirthRules04O.Known((int)OneGameH8S.World), Is.True);
            Assert.That(CityRebirthRules04O.Known((int)OneGameH8S.World + 1), Is.False, "no world revision 11 exists");
            Assert.That(OneGameH8S.Bank, Is.EqualTo(SaveSystem.BankForRevision04OC((int)OneGameH8S.World)));
            Assert.That(SaveSystem.SlotFileName04OB(0, OneGameH8S.Bank), Is.EqualTo("lasveyra_civic_h5_save_0.json"));
            foreach (CityRevision04O revision in System.Enum.GetValues(typeof(CityRevision04O)))
                Assert.That(OneGameH8S.NeedsReload(revision), Is.EqualTo(revision != CityRevision04O.GothicCivicH5), revision.ToString());
        }

        [Test]
        public void LoadGameReachesEveryOlderBankExactlyOnceAndComesBack()
        {
            List<int> seen = new List<int>();
            int bank = OneGameH8S.Bank;
            for (int i = 0; i < 10; i++)
            {
                seen.Add(bank);
                bank = OneGameH8S.NextBank(bank);
            }
            Assert.That(bank, Is.EqualTo(OneGameH8S.Bank), "the cycle returns to the current world");
            Assert.That(seen, Is.EqualTo(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 0 }));
            for (int revision = 0; revision <= 10; revision++)
                Assert.That(seen, Does.Contain(SaveSystem.BankForRevision04OC(revision)), "a save of revision " + revision + " is listed somewhere");
            Assert.That(OneGameH8S.NextBank(1), Is.EqualTo(OneGameH8S.Bank), "revision 1 shares bank 0; 1 is not a bank");
            Assert.That(OneGameH8S.NextBank(-1), Is.EqualTo(OneGameH8S.Bank));
        }

        [Test]
        public void BankLabelsAreClearAndPlayerFacing()
        {
            Assert.That(OneGameH8S.IsOlderBank(OneGameH8S.Bank), Is.False);
            Assert.That(OneGameH8S.BankButton(OneGameH8S.Bank), Is.EqualTo("Show older saves"));
            HashSet<string> names = new HashSet<string>();
            for (int bank = OneGameH8S.NextBank(OneGameH8S.Bank); bank != OneGameH8S.Bank; bank = OneGameH8S.NextBank(bank))
            {
                Assert.That(OneGameH8S.IsOlderBank(bank), Is.True, "bank " + bank);
                Assert.That(OneGameH8S.BankButton(bank), Does.StartWith("Older saves: "), "bank " + bank);
                Assert.That(OneGameH8S.WorldName(bank), Is.Not.EqualTo("Unknown world"), "bank " + bank);
                Assert.That(names.Add(OneGameH8S.WorldName(bank)), Is.True, "distinct world names");
            }
            Assert.That(OneGameH8S.OlderHint, Does.Contain("unchanged"));
        }

        [Test]
        public void TheTitleShowsNoPreviewGridOrPhaseFooter()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Scripts/Runtime/UI/MenuSystem.cs");
            string menu = File.ReadAllText(path);
            Assert.That(menu, Does.Contain("private static readonly bool WorldPreviewGridH8S = false;"), "the preview grid stays off for players");
            Assert.That(menu, Does.Contain("if (WorldPreviewGridH8S)"), "the grid is behind the flag");
            Assert.That(menu, Does.Not.Contain("Game.Version + \"  |  Built procedurally"), "the footer no longer names an old phase");
            Assert.That(menu, Does.Contain("Request(OneGameH8S.World,null,false,afterPrologue)"), "the story starts in the unified world");
        }
    }
}
