using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure event progression, budget and strict shipped-data contracts; no scene or native physics implied.</summary>
    public sealed class TerritoryEventTests
    {
        [TestCase(4, 0, false, 2)]
        [TestCase(4, 1, false, 3)]
        [TestCase(4, 2, false, 4)]
        [TestCase(4, 2, true, 2)]
        [TestCase(1, 2, false, 1)]
        [TestCase(99, 2, false, 4)]
        public void CombatBudgetRespectsDifficultyAndPreset(int authored, int difficulty, bool low, int expected)
        {
            Assert.That(TerritoryEventRules.CombatCount(authored, difficulty, low), Is.EqualTo(expected));
        }

        [Test]
        public void ObservationRequiresContinuousViewAndFreezesDuringPause()
        {
            float progress = TerritoryEventRules.Observe(2f, 0.5f, true, false);
            Assert.That(progress, Is.EqualTo(2.5f));
            Assert.That(TerritoryEventRules.Observe(progress, 200f, false, true), Is.EqualTo(progress));
            Assert.That(TerritoryEventRules.Observe(progress, 0.1f, false, false), Is.Zero);
            Assert.That(TerritoryEventRules.Observe(2f, -10f, true, false), Is.EqualTo(2f));
        }

        [Test]
        public void ResonanceRequiresOrderedNotesAndCanRecoverAfterAWrongNote()
        {
            int step = TerritoryEventRules.ResonanceStep(0, 0, 3);
            step = TerritoryEventRules.ResonanceStep(step, 1, 3);
            Assert.That(step, Is.EqualTo(2));
            step = TerritoryEventRules.ResonanceStep(step, 1, 3);
            Assert.That(step, Is.Zero);
            step = TerritoryEventRules.ResonanceStep(step, 0, 3);
            step = TerritoryEventRules.ResonanceStep(step, 1, 3);
            step = TerritoryEventRules.ResonanceStep(step, 2, 3);
            Assert.That(step, Is.EqualTo(3));
            Assert.That(TerritoryEventRules.ResonanceStep(step, 0, 3), Is.EqualTo(3), "completed sequences cannot be revoked by a late prompt");
        }

        [Test]
        public void CancelTransitionsNeverCountAsSuccess()
        {
            Assert.That(TerritoryEventRules.StopReason(false, false, false, 89f, 20f, 240f), Is.Null);
            Assert.That(TerritoryEventRules.StopReason(true, false, false, 0f, 20f, 240f), Does.Contain("fell"));
            Assert.That(TerritoryEventRules.StopReason(false, true, false, 0f, 20f, 240f), Does.Contain("pursuit"));
            Assert.That(TerritoryEventRules.StopReason(false, false, true, 0f, 20f, 240f), Does.Contain("Veil"));
            Assert.That(TerritoryEventRules.StopReason(false, false, false, 91f, 20f, 240f), Does.Contain("left"));
            Assert.That(TerritoryEventRules.StopReason(false, false, false, 0f, 240f, 240f), Does.Contain("closed"));
        }

        [Test]
        public void CorruptCooldownsAreFiniteAndFortificationIsCapped()
        {
            Assert.That(TerritoryEventRules.SavedCooldown(float.NaN), Is.Zero);
            Assert.That(TerritoryEventRules.SavedCooldown(float.PositiveInfinity), Is.Zero);
            Assert.That(TerritoryEventRules.SavedCooldown(-2f), Is.Zero);
            Assert.That(TerritoryEventRules.SavedCooldown(999999f), Is.EqualTo(7200f));
            Assert.That(TerritoryEventRules.DefenseHold(20f, 0), Is.EqualTo(20f));
            Assert.That(TerritoryEventRules.DefenseHold(20f, 1), Is.EqualTo(16f));
            Assert.That(TerritoryEventRules.DefenseHold(20f, 999), Is.EqualTo(12f));
            Assert.That(TerritoryEventRules.DefenseHealthScale(999), Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(TerritoryEventRules.DefenseHealthScale(-10), Is.EqualTo(1f));
        }

        [Test]
        public void StrictValidatorRejectsUnsafeOrUnknownTerritoryDefinitions()
        {
            EventTemplate invalid = new EventTemplate
            {
                Id = "invalid_04b_check", Title = "Invalid activity", Kind = "Skirmish", Activity = "InfiniteWave",
                ManualOnly = false, Anchor = "Any", Callout = "This invalid record must not ship.",
                Duration = 5f, Cooldown = 1f, Points = 8, Seconds = 0f, Count = 16, Species = "unknown_species"
            };
            QuestReport result = new QuestReport();
            EventFile file = new EventFile(); file.Events.Add(invalid);
            QuestValidator.ValidateEvents(new List<KeyValuePair<string, EventFile>> { new KeyValuePair<string, EventFile>("invalid.json", file) }, new QuestRefs(), result, null);
            string details = result.Details();
            Assert.That(details, Does.Contain("require ManualOnly"));
            Assert.That(details, Does.Contain("unknown territory Activity"));
            Assert.That(details, Does.Contain("territory combat Count"));
            Assert.That(details, Does.Contain("territory Duration"));
            Assert.That(details, Does.Contain("unknown Species"));
        }

        [Test]
        public void ShippedLivingActivitiesAreExplicitAndKeepDistinctLoops()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, EventFile>> files = QuestTestData.Load<EventFile>("Events", errors, new QuestJsonTrace());
            Assert.That(errors, Is.Empty);
            HashSet<string> activities = new HashSet<string>();
            int count = 0;
            foreach (KeyValuePair<string, EventFile> file in files)
                foreach (EventTemplate t in file.Value.Events)
                {
                    if (!t.Id.StartsWith("ev_04b_")) continue;
                    count++;
                    activities.Add(t.Activity);
                    Assert.That(t.ManualOnly, Is.True, t.Id);
                    Assert.That(TerritoryEventRules.ValidActivity(t.Kind, t.Activity), Is.True, t.Id);
                    Assert.That(EventSelector.WhyNot(t, new EventSituation(), null, null), Is.EqualTo("start at its territory terminal"));
                    Assert.That(t.Duration, Is.InRange(60f, 600f));
                    Assert.That(t.Count, Is.LessThanOrEqualTo(4));
                    Assert.That(t.Reward.SkillPoints, Is.Zero, "events do not create repeatable skill-point loops");
                }
            Assert.That(count, Is.EqualTo(6));
            Assert.That(activities, Is.EquivalentTo(new[] { "Gather", "Aid", "Observe", "Resonance", "Defense", "Stabilize" }));
        }
    }
}
