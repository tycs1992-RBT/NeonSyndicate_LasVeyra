using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The quest and world-event save blocks are append-only like SaveData, and (inside Unity) the game's own loader,
    /// JsonUtility, must read the shipped data files exactly as the validator's QuestJson reader does.
    /// </summary>
    public sealed class QuestSaveTests
    {
        /// <summary>Every QuestSave field that has shipped. Append new fields here; never edit or remove a line.</summary>
        private static readonly string[] QuestSaveFields =
        {
            "Completed:List<String>", "Active:String", "Visited:List<String>", "UnclaimedSkillPoints:Int32", "BoardGuilds:List<String>",
            "BoardSerials:List<Int32>", "ContractsDone:List<Int32>", "NextSerial:Int32", "BoardDay:Int32"
        };

        /// <summary>Every EventSave field that has shipped. Append new fields here; never edit or remove a line.</summary>
        private static readonly string[] EventSaveFields =
        {
            "SeenIds:List<String>", "SeenCounts:List<Int32>", "RecentIds:List<String>", "RecentTitles:List<String>", "RecentOutcomes:List<Int32>",
            "RecentDays:List<Int32>", "RecentHours:List<Single>", "RecentPlaces:List<String>",
            "TerritoryCooldownKeys:List<String>", "TerritoryCooldownSeconds:List<Single>",
            "CrystalRewardTokens04F:List<String>", "CrystalCooldownIds04F:List<String>", "CrystalCooldownSeconds04F:List<Single>"
        };

        [Test]
        public void SaveBlocksAreOnlyEverAppended()
        {
            CheckFields(typeof(QuestSave), QuestSaveFields);
            CheckFields(typeof(EventSave), EventSaveFields);
            Assert.That(Attribute.IsDefined(typeof(QuestSave), typeof(SerializableAttribute)), Is.True);
            Assert.That(Attribute.IsDefined(typeof(EventSave), typeof(SerializableAttribute)), Is.True);
        }

        [Test]
        public void NewSaveBlocksStartEmpty()
        {
            QuestSave q = new QuestSave();
            Assert.That(q.Completed, Is.Not.Null.And.Empty);
            Assert.That(q.Visited, Is.Not.Null.And.Empty);
            Assert.That(q.BoardSerials, Is.Not.Null.And.Empty);
            EventSave e = new EventSave();
            Assert.That(e.RecentIds, Is.Not.Null.And.Empty);
            Assert.That(e.RecentHours, Is.Not.Null.And.Empty);
            Assert.That(e.TerritoryCooldownKeys, Is.Not.Null.And.Empty);
            Assert.That(e.TerritoryCooldownSeconds, Is.Not.Null.And.Empty);
            Assert.That(e.CrystalRewardTokens04F, Is.Not.Null.And.Empty);
            Assert.That(e.CrystalCooldownIds04F, Is.Not.Null.And.Empty);
            Assert.That(e.CrystalCooldownSeconds04F, Is.Not.Null.And.Empty);
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void QuestAndEventSavesSurviveJsonUtility()
        {
            QuestSave q = new QuestSave { Active = "fw_griffin_lake", UnclaimedSkillPoints = 3, NextSerial = 41, BoardDay = 7 };
            q.Completed.Add("fw_hunting_trial");
            q.Visited.Add("WardenOutpost");
            q.BoardGuilds.Add("menders");
            q.BoardSerials.Add(12);
            q.ContractsDone.Add(5);
            QuestSave qb = JsonUtility.FromJson<QuestSave>(JsonUtility.ToJson(q));
            Assert.That(qb.Active, Is.EqualTo("fw_griffin_lake"));
            Assert.That(qb.Completed, Is.EqualTo(q.Completed));
            Assert.That(qb.Visited, Is.EqualTo(q.Visited));
            Assert.That(qb.BoardSerials, Is.EqualTo(q.BoardSerials));
            Assert.That(qb.UnclaimedSkillPoints, Is.EqualTo(3));
            Assert.That(qb.NextSerial, Is.EqualTo(41));
            EventSave e = new EventSave();
            e.SeenIds.Add("ev_lumen_storm");
            e.SeenCounts.Add(2);
            e.RecentIds.Add("ev_lumen_storm");
            e.RecentTitles.Add("Lumen Storm");
            e.RecentOutcomes.Add(1);
            e.RecentDays.Add(4);
            e.RecentHours.Add(22.5f);
            e.RecentPlaces.Add("Sunken Row");
            e.TerritoryCooldownKeys.Add("amethyst_meridian:ev_04b_ancient_resonance");
            e.TerritoryCooldownSeconds.Add(123.5f);
            EventSave eb = JsonUtility.FromJson<EventSave>(JsonUtility.ToJson(e));
            Assert.That(eb.TerritoryCooldownKeys, Is.EqualTo(e.TerritoryCooldownKeys));
            Assert.That(eb.TerritoryCooldownSeconds, Is.EqualTo(e.TerritoryCooldownSeconds));
            EventSave oldEvents = JsonUtility.FromJson<EventSave>("{\"SeenIds\":[\"ev_lumen_storm\"],\"SeenCounts\":[2]}");
            Assert.That(oldEvents.SeenIds[0], Is.EqualTo("ev_lumen_storm"));
            Assert.That(eb.RecentTitles, Is.EqualTo(e.RecentTitles));
            Assert.That(eb.RecentHours, Is.EqualTo(e.RecentHours));
            // A save from before the quest system: the block is missing and loads empty.
            QuestSave old = JsonUtility.FromJson<QuestSave>("{\"Completed\":[\"x\"]}");
            Assert.That(old.Visited, Is.Not.Null.And.Empty);
            Assert.That(old.NextSerial, Is.EqualTo(0), "0 means the board serials start at 1");
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void TerritoryEventCooldownRestoreHandlesMissingNullAndMalformedValues()
        {
            GameObject host = new GameObject("Event save migration test");
            try
            {
                WorldEvents events = host.AddComponent<WorldEvents>();
                events.Restore(new SaveData { Events = null });
                SaveData captured = new SaveData();
                events.Capture(captured);
                Assert.That(captured.Events.TerritoryCooldownKeys, Is.Empty);
                events.Restore(new SaveData { Events = new EventSave { TerritoryCooldownKeys = null, TerritoryCooldownSeconds = null } });
                events.Capture(captured);
                Assert.That(captured.Events.TerritoryCooldownKeys, Is.Empty);
                EventSave malformed = new EventSave
                {
                    TerritoryCooldownKeys = new List<string> { "relay:event", "bad:event", "unpaired:event" },
                    TerritoryCooldownSeconds = new List<float> { 999999f, float.NaN }
                };
                events.Restore(new SaveData { Events = malformed });
                Assert.That(events.TerritoryCooldown("event", "relay"), Is.EqualTo(7200f));
                Assert.That(events.TerritoryCooldown("event", "bad"), Is.Zero);
                Assert.That(events.TerritoryCooldown("event", "unpaired"), Is.Zero);
                events.Capture(captured);
                Assert.That(captured.Events.TerritoryCooldownKeys.Count, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void JsonUtilityReadsTheShippedDataLikeTheValidator()
        {
            string root = QuestTestData.ResourcesDir;
            Assert.That(root, Is.Not.Null);
            int files = 0;
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Quests"), "*.json"))
            {
                string text = File.ReadAllText(path);
                QuestFile game = JsonUtility.FromJson<QuestFile>(text);
                QuestDefaults.Normalize(game);
                QuestFile checker = QuestJson.Read<QuestFile>(text, Path.GetFileName(path), null, null);
                QuestDefaults.Normalize(checker);
                Assert.That(JsonUtility.ToJson(game), Is.EqualTo(JsonUtility.ToJson(checker)), path);
                files++;
            }
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Events"), "*.json"))
            {
                string text = File.ReadAllText(path);
                EventFile game = JsonUtility.FromJson<EventFile>(text);
                QuestDefaults.Normalize(game);
                EventFile checker = QuestJson.Read<EventFile>(text, Path.GetFileName(path), null, null);
                QuestDefaults.Normalize(checker);
                Assert.That(JsonUtility.ToJson(game), Is.EqualTo(JsonUtility.ToJson(checker)), path);
                files++;
            }
            Assert.That(files, Is.GreaterThanOrEqualTo(6));
        }

        private static void CheckFields(Type type, string[] frozen)
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = TypeName(f.FieldType);
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string have;
                Assert.That(now.TryGetValue(parts[0], out have), Is.True, type.Name + "." + parts[0] + " was renamed or removed. Saves are JSON: only append new fields.");
                Assert.That(have, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type. Add a new field instead.");
            }
            Assert.That(now.Count, Is.EqualTo(frozen.Length), type.Name + " has a field missing from the freeze list in QuestSaveTests: add it there.");
        }

        private static string TypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return "List<" + t.GetGenericArguments()[0].Name + ">";
            return t.Name;
        }
    }
}
