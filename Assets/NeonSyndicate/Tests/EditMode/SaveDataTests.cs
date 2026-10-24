using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Save games are SaveData written with JsonUtility. A round trip must keep every field, old saves must load with
    /// sensible defaults, and fields may only ever be appended: renaming or removing one silently loses players' data.
    /// </summary>
    public sealed class SaveDataTests
    {
        /// <summary>Every SaveData field that has shipped, with its type. Add new fields here when you append them; never edit or remove a line.</summary>
        private static readonly string[] SaveDataFields =
        {
            "Version:Int32", "SavedAt:String", "Hours:Single", "Day:Int32", "Weather:Int32", "Money:Int32", "Health:Single", "Armor:Single",
            "Weapons:List<Int32>", "Clips:List<Int32>", "Ammo:List<Int32>", "CurrentWeapon:Int32", "Stats:List<Single>", "Achievements:List<String>",
            "StoryProgress:Int32", "Flags:List<String>", "Law:String", "Chips:List<Int32>", "Shards:List<Int32>", "Stones:List<Int32>",
            "Relics:List<Int32>", "Jumps:List<Int32>", "Properties:List<String>", "Activities:List<String>", "Energy:Single", "MaxEnergy:Single",
            "ThirdEye:Boolean", "Spells:List<Boolean>", "SelectedSpell:Int32", "GangHeat:List<Single>", "Outfit:Int32", "PosX:Single", "PosY:Single",
            "PosZ:Single", "Yaw:Single", "Completion:Single", "FreeRoam:Boolean", "AtSafehouse:Boolean", "Lumen:Int32", "Level:Int32",
            "Attributes:List<Int32>", "LastShrine:Int32", "LitShrines:List<Int32>", "EchoActive:Boolean", "EchoX:Single", "EchoY:Single",
            "EchoZ:Single", "EchoLumen:Int32", "Tonics:Int32", "LumenEarned:Int64", "Gear:List<GearSave>", "Equipped:List<Int32>", "Difficulty:Int32",
            "Creatures:CreatureSave", "Materials:MaterialSave", "Factions:FactionSave", "Quests:QuestSave", "Events:EventSave",
            "Crafting:CraftSave", "Skills:SkillSave", "Loot:LootSave", "Travel:TravelSave",
            "LootWorld:LootWorldSave",
            "Gadgets:GadgetSave",
            "Discovery:DiscoverySave",
            "Strongholds:StrongholdSave",
            "Territories:TerritorySave",
            "InhabitedCity:InhabitedCitySave",
            "CrystalArsenal:CrystalArsenalSave",
            "Underheart:UnderheartSave", "Guidance04E:GuidanceSave", "Campaign04E:CampaignSave", "Bonds:BondSave", "Garage:GarageSave",
            "Builds:BuildSave", "HasSurfaceSafehouse:Boolean", "SurfaceSafeX:Single", "SurfaceSafeY:Single", "SurfaceSafeZ:Single", "CrystalCombat:CrystalCombatSave04F", "WorldRevision04O:Int32",
            "Interiors:InteriorSave"
        };

        private static readonly string[] GadgetSaveFields =
        {
            "Slots:List<Int32>", "SlotMods:List<Int32>", "Mods:List<GadgetModSave>", "NextModUid:Int32", "AllUnlocked:Boolean", "HintsShown:Int32",
            "Announced:List<Int32>"
        };

        private static readonly string[] GadgetModSaveFields = { "Uid:Int32", "Stat:Int32", "Rarity:Int32", "Value:Single" };

        private static readonly string[] GearSaveFields =
        {
            "Uid:Int32", "Slot:Int32", "Rarity:Int32", "Power:Int32", "Upgrade:Int32", "Set:Int32", "Element:Int32", "Seed:Int32", "Name:String",
            "AffixTypes:List<Int32>", "AffixValues:List<Single>"
        };

        [Test]
        public void FieldsAreOnlyEverAppended()
        {
            CheckFields(typeof(SaveData), SaveDataFields);
            CheckFields(typeof(GearSave), GearSaveFields);
            CheckFields(typeof(GadgetSave), GadgetSaveFields);
            CheckFields(typeof(GadgetModSave), GadgetModSaveFields);
            Assert.That(Attribute.IsDefined(typeof(SaveData), typeof(SerializableAttribute)), Is.True, "SaveData stays [Serializable]");
            Assert.That(Attribute.IsDefined(typeof(GearSave), typeof(SerializableAttribute)), Is.True, "GearSave stays [Serializable]");
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void JsonRoundTripKeepsEveryField()
        {
            SaveData d = Sample();
            string json = JsonUtility.ToJson(d, true);
            SaveData back = JsonUtility.FromJson<SaveData>(json);
            Assert.That(back, Is.Not.Null);
            AssertSame(d, back, "SaveData");
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void OldSavesLoadWithDefaults()
        {
            // A save from before Phase 2: none of the progression, gear or difficulty fields exist yet.
            SaveData d = JsonUtility.FromJson<SaveData>("{\"Version\":1,\"SavedAt\":\"2026-01-01 12:00\",\"Money\":4200,\"Hours\":20.5}");
            Assert.That(d.Money, Is.EqualTo(4200));
            Assert.That(d.Hours, Is.EqualTo(20.5f));
            Assert.That(d.Level, Is.EqualTo(1), "Level defaults to 1");
            Assert.That(d.LastShrine, Is.EqualTo(-1), "no shrine yet");
            Assert.That(d.Tonics, Is.EqualTo(-1), "tonics refill on load");
            Assert.That(d.Difficulty, Is.EqualTo(-1), "difficulty keeps the current setting");
            Assert.That(d.Gear, Is.Not.Null.And.Empty, "no gear");
            Assert.That(d.Weapons, Is.Not.Null.And.Empty, "no weapons");
            Assert.That(d.Law, Is.Not.Null, "law text");
        }

        // ------------------------------------------------------------------ helpers

        private static void CheckFields(Type type, string[] frozen)
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = TypeName(f.FieldType);
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string type2;
                Assert.That(now.TryGetValue(parts[0], out type2), Is.True,
                    type.Name + "." + parts[0] + " was renamed or removed. Saves are JSON: only append new fields, or old saves lose this data.");
                Assert.That(type2, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type. Add a new field instead.");
            }
        }

        private static string TypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return "List<" + t.GetGenericArguments()[0].Name + ">";
            return t.Name;
        }

        /// <summary>A save with a distinct, non-default value in every field (lists included).</summary>
        private static SaveData Sample()
        {
            SaveData d = new SaveData();
            int n = 3;
            foreach (FieldInfo f in typeof(SaveData).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                n++;
                f.SetValue(d, ValueFor(f.FieldType, n));
            }
            return d;
        }

        private static object ValueFor(Type t, int n)
        {
            if (t == typeof(int)) return 1000 + n;
            if (t == typeof(long)) return 5000000000L + n;
            if (t == typeof(float)) return n + 0.25f;
            if (t == typeof(bool)) return n % 2 == 0;
            if (t == typeof(string)) return "value " + n;
            if (t == typeof(List<int>)) return new List<int> { n, n + 1, -n };
            if (t == typeof(List<float>)) return new List<float> { n + 0.5f, -n, 0.125f };
            if (t == typeof(List<string>)) return new List<string> { "a" + n, "b" + n };
            if (t == typeof(List<bool>)) return new List<bool> { true, false, true };
            if (t == typeof(List<GearSave>))
            {
                GearSave g = new GearSave();
                int k = n;
                foreach (FieldInfo f in typeof(GearSave).GetFields(BindingFlags.Public | BindingFlags.Instance)) f.SetValue(g, ValueFor(f.FieldType, ++k));
                return new List<GearSave> { g, new GearSave { Uid = 7, Name = "second", AffixTypes = new List<int> { 2 }, AffixValues = new List<float> { 0.5f } } };
            }
            // A list of [Serializable] blocks inside a save block (generated guns, for example): two filled entries.
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type e = t.GetGenericArguments()[0];
                if (e.IsClass && Attribute.IsDefined(e, typeof(SerializableAttribute)) && e.GetConstructor(Type.EmptyTypes) != null)
                {
                    IList list = (IList)Activator.CreateInstance(t);
                    list.Add(ValueFor(e, n * 3));
                    list.Add(ValueFor(e, n * 3 + 1));
                    return list;
                }
            }
            // A [Serializable] save block (the Phase 3 systems write one each): fill every field the same way.
            if (t.IsClass && Attribute.IsDefined(t, typeof(SerializableAttribute)) && t.GetConstructor(Type.EmptyTypes) != null)
            {
                object block = Activator.CreateInstance(t);
                int k = n * 10;
                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance)) f.SetValue(block, ValueFor(f.FieldType, ++k));
                return block;
            }
            Assert.Fail("SaveDataTests has no sample value for type " + t + "; add one to ValueFor.");
            return null;
        }

        private static bool IsBlock(object o)
        {
            return o != null && !(o is string) && !(o is IList) && o.GetType().IsClass && Attribute.IsDefined(o.GetType(), typeof(SerializableAttribute));
        }

        private static void AssertSame(object a, object b, string path)
        {
            Type t = a.GetType();
            foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object va = f.GetValue(a), vb = f.GetValue(b);
                string p = path + "." + f.Name;
                if (va is IList)
                {
                    IList la = (IList)va, lb = vb as IList;
                    Assert.That(lb, Is.Not.Null, p);
                    Assert.That(lb.Count, Is.EqualTo(la.Count), p + " count");
                    for (int i = 0; i < la.Count; i++)
                    {
                        if (IsBlock(la[i])) AssertSame(la[i], lb[i], p + "[" + i + "]");
                        else Assert.That(lb[i], Is.EqualTo(la[i]), p + "[" + i + "]");
                    }
                }
                else if (IsBlock(va)) AssertSame(va, vb, p);
                else Assert.That(vb, Is.EqualTo(va), p);
            }
        }
    }
}
