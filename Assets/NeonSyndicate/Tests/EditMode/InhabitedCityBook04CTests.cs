using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Persistent venue facts are independent of native actors, room loading and quest rewards.</summary>
    public sealed class InhabitedCityBook04CTests
    {
        private static InhabitedCityBook NewBook() { return new InhabitedCityBook(QuietSignal04C.Venues); }

        [Test]
        public void SaveIdentityIsAppendOnly()
        {
            HashSet<string> fields = new HashSet<string>();
            foreach (FieldInfo field in typeof(InhabitedCitySave).GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields.Add(field.Name + ":" + field.FieldType.Name);
            Assert.That(fields, Is.EquivalentTo(new[] { "Version:Int32", "Visited:List`1", "Actions:List`1", "ReadRecords:List`1", "ResumeVenue:String", "IntroSeen:Boolean" }));
            Assert.That(typeof(InhabitedCitySave).GetField("Visited").FieldType, Is.EqualTo(typeof(List<string>)));
            Assert.That(typeof(InhabitedCitySave).GetField("Actions").FieldType, Is.EqualTo(typeof(List<string>)));
            Assert.That(typeof(InhabitedCitySave).GetField("ReadRecords").FieldType, Is.EqualTo(typeof(List<string>)));
        }

        [Test]
        public void OldOrNullSaveStartsWithNoClaimsAndNoResume()
        {
            InhabitedCityBook book = NewBook();
            book.Visit("lofts"); book.CompleteAction("lofts:request"); book.SetResume("clinic"); book.IntroSeen = true;
            book.Restore(null);
            Assert.That(book.VisitedCount, Is.Zero); Assert.That(book.Capture().Actions, Is.Empty);
            Assert.That(book.ResumeVenue, Is.Empty); Assert.That(book.IntroSeen, Is.False);
            book.Restore(new InhabitedCitySave { Visited = null, Actions = null, ResumeVenue = null });
            Assert.That(book.VisitedCount, Is.Zero); Assert.That(book.Capture().Actions, Is.Empty); Assert.That(book.ResumeVenue, Is.Empty);
        }

        [Test]
        public void DiscoveryAcceptsOnlyKnownIdentityAndOnlyOnce()
        {
            InhabitedCityBook book = NewBook();
            Assert.That(book.Visit(null), Is.False); Assert.That(book.Visit("unknown"), Is.False);
            Assert.That(book.Visit("LOFTS"), Is.False); Assert.That(book.Visit("lofts"), Is.True);
            Assert.That(book.Visit("lofts"), Is.False); Assert.That(book.VisitedCount, Is.EqualTo(1));
            Assert.That(book.Visited("lofts"), Is.True); Assert.That(book.Visited(null), Is.False);
        }

        [Test]
        public void ActivityClaimIsFiniteAcrossReloadAndDuplicateUse()
        {
            InhabitedCityBook original = NewBook();
            Assert.That(original.CompleteAction("clinic:inspect_supplies"), Is.True);
            Assert.That(original.CompleteAction("clinic:inspect_supplies"), Is.False);
            InhabitedCityBook restored = NewBook(); restored.Restore(original.Capture());
            Assert.That(restored.CompleteAction("clinic:inspect_supplies"), Is.False);
            Assert.That(restored.HasAction("clinic:inspect_supplies"), Is.True);
            Assert.That(restored.Capture().Actions, Has.Count.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown:request")]
        [TestCase("lofts:unknown")]
        [TestCase("lofts:request:again")]
        [TestCase(":request")]
        public void InvalidActionCannotBecomePersistentProgress(string value)
        {
            InhabitedCityBook book = NewBook(); Assert.That(book.CompleteAction(value), Is.False);
            Assert.That(book.Capture().Actions, Is.Empty);
        }

        [Test]
        public void CorruptSaveNormalizesUnknownDuplicateAndOversizedEntries()
        {
            InhabitedCityBook book = NewBook();
            InhabitedCitySave save = new InhabitedCitySave { Version = -123, ResumeVenue = "unloaded_room", IntroSeen = true,
                Visited = new List<string> { null, "", "unknown", "clinic", "clinic" },
                Actions = new List<string> { null, "", "clinic:inspect_supplies", "clinic:inspect_supplies", "clinic:invented", new string('a', 200) } };
            book.Restore(save);
            Assert.That(book.Capture().Version, Is.EqualTo(1)); Assert.That(book.VisitedCount, Is.EqualTo(1));
            Assert.That(book.Capture().Actions, Is.EqualTo(new[] { "clinic:inspect_supplies" }));
            Assert.That(book.ResumeVenue, Is.Empty); Assert.That(book.IntroSeen, Is.True);
        }

        [Test]
        public void CaptureAndRestoreNeverShareMutableLists()
        {
            InhabitedCityBook book = NewBook(); book.Visit("clinic"); book.CompleteAction("clinic:inspect_supplies"); book.SetResume("clinic");
            InhabitedCitySave captured = book.Capture(); captured.Visited.Clear(); captured.Actions.Clear(); captured.ResumeVenue = "lofts";
            Assert.That(book.Visited("clinic"), Is.True); Assert.That(book.HasAction("clinic:inspect_supplies"), Is.True);
            Assert.That(book.ResumeVenue, Is.EqualTo("clinic"));
            InhabitedCitySave source = book.Capture(); InhabitedCityBook restored = NewBook(); restored.Restore(source);
            source.Visited.Clear(); source.Actions.Clear(); source.ResumeVenue = "lofts";
            Assert.That(restored.Visited("clinic"), Is.True); Assert.That(restored.HasAction("clinic:inspect_supplies"), Is.True);
            Assert.That(restored.ResumeVenue, Is.EqualTo("clinic"));
        }

        [Test]
        public void SnapshotOrderAndAllowedIdentitySetAreStable()
        {
            List<string> known = new List<string>(QuietSignal04C.Venues); InhabitedCityBook book = new InhabitedCityBook(known);
            known.Clear(); book.Visit("lofts"); book.Visit("archive"); book.Visit("clinic");
            book.CompleteAction("lofts:request"); book.CompleteAction("archive:record_origins");
            Assert.That(book.Capture().Visited, Is.EqualTo(new[] { "archive", "clinic", "lofts" }));
            Assert.That(book.Capture().Actions, Is.EqualTo(new[] { "archive:record_origins", "lofts:request" }));
            book.SetResume("garage"); Assert.That(book.ResumeVenue, Is.EqualTo("garage"));
            book.SetResume("arbitrary_off_map_position"); Assert.That(book.ResumeVenue, Is.Empty);
        }
        [Test]
        public void ReadingARecordDoesNotCompleteAQuestActionOrGrantClaims()
        {
            InhabitedCityBook book = NewBook();
            Assert.That(book.ReadRecord("record_origins"), Is.True); Assert.That(book.ReadRecord("record_origins"), Is.False);
            Assert.That(book.HasReadRecord("record_origins"), Is.True);
            Assert.That(book.HasAction("archive:record_origins"), Is.False); Assert.That(book.Capture().Actions, Is.Empty);
            Assert.That(book.CompleteAction("archive:record_origins"), Is.True, "A record read before the chapter must not skip its later actual quest step.");
            Assert.That(book.ReadRecord("record_frequency"), Is.True); Assert.That(book.ReadRecord("record_route"), Is.True);
            Assert.That(book.ReadRecord("unknown"), Is.False); Assert.That(book.ReadRecord(null), Is.False);
        }
        [Test]
        public void RecordRestoreFiltersCorruptionAndCopiesItsOwnState()
        {
            InhabitedCitySave source = new InhabitedCitySave { ReadRecords = new List<string> { null, "", "origins", "unknown", "record_route", "record_route" } };
            InhabitedCityBook book = NewBook(); book.Restore(source); source.ReadRecords.Clear();
            Assert.That(book.Capture().ReadRecords, Is.EqualTo(new[] { "record_route" }));
            InhabitedCitySave captured = book.Capture(); captured.ReadRecords.Clear();
            Assert.That(book.HasReadRecord("record_route"), Is.True);
            book.Restore(new InhabitedCitySave { ReadRecords = null }); Assert.That(book.Capture().ReadRecords, Is.Empty);
        }
    }
}
