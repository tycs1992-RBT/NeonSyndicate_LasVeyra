using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public string SavedAt;
        public float Hours;
        public int Day;
        public int Weather;
        public int Money;
        public float Health;
        public float Armor;
        public List<int> Weapons = new List<int>();
        public List<int> Clips = new List<int>();
        public List<int> Ammo = new List<int>();
        public int CurrentWeapon;
        public List<float> Stats = new List<float>();
        public List<string> Achievements = new List<string>();
        public int StoryProgress;
        public List<string> Flags = new List<string>();
        public string Law = "";
        public List<int> Chips = new List<int>();
        public List<int> Shards = new List<int>();
        public List<int> Stones = new List<int>();
        public List<int> Relics = new List<int>();
        public List<int> Jumps = new List<int>();
        public List<string> Properties = new List<string>();
        public List<string> Activities = new List<string>();
        public float Energy;
        public float MaxEnergy;
        public bool ThirdEye;
        public List<bool> Spells = new List<bool>();
        public int SelectedSpell;
        public List<float> GangHeat = new List<float>();
        public int Outfit;
        public float PosX, PosY, PosZ, Yaw;
        public float Completion;
        public bool FreeRoam;
        /// <summary>True when the saved position is the last safehouse used (so later saves keep returning there).</summary>
        public bool AtSafehouse;
        // Phase 2: Lumen, levels, shrines, the death echo and mythic gear.
        public int Lumen;
        public int Level = 1;
        public List<int> Attributes = new List<int>();
        public int LastShrine = -1;
        public List<int> LitShrines = new List<int>();
        public bool EchoActive;
        public float EchoX, EchoY, EchoZ;
        public int EchoLumen;
        public int Tonics = -1;
        public long LumenEarned;
        public List<GearSave> Gear = new List<GearSave>();
        public List<int> Equipped = new List<int>();
        public int Difficulty = -1;
        // Phase 3: the mythic layer. Each system writes its own block (see IWorldSystem); old saves keep the defaults.
        public CreatureSave Creatures = new CreatureSave();
        public MaterialSave Materials = new MaterialSave();
        public FactionSave Factions = new FactionSave();
        public QuestSave Quests = new QuestSave();
        public EventSave Events = new EventSave();
        public CraftSave Crafting = new CraftSave();
        public SkillSave Skills = new SkillSave();
        // Phase 4: loot guns and travel powers.
        public LootSave Loot = new LootSave();
        public TravelSave Travel = new TravelSave();
        // Phase 5: the loot world (chests, satchels, keys, the Collector, bounties).
        public LootWorldSave LootWorld = new LootWorldSave();
        // Phase 6: gadgets (slots, unlocks, mods, hints).
        public GadgetSave Gadgets = new GadgetSave();
        // 02G: optional discovery activity state. Old saves have no block; Restore handles null.
        public DiscoverySave Discovery = new DiscoverySave();
        // 02I append-only local fort ledger. Old saves start with unclaimed forts.
        public StrongholdSave Strongholds = new StrongholdSave();
        // 04B append-only territory ledger. Prior saves default to available relays and unclaimed finite rewards.
        public TerritorySave Territories = new TerritorySave();
    }

    /// <summary>JSON save games (autosave plus three slots) in Application.persistentDataPath.</summary>
    public static class SaveSystem
    {
        public const int Slots = 3;

        public static string PathFor(int slot)
        {
            return Path.Combine(Application.persistentDataPath, "lasveyra_save_" + slot + ".json");
        }

        public static bool Exists(int slot)
        {
            try { return File.Exists(PathFor(slot)); }
            catch (Exception) { return false; }
        }

        public static bool AnyExists()
        {
            for (int i = 0; i <= Slots; i++) if (Exists(i)) return true;
            return false;
        }

        public static int MostRecentSlot()
        {
            int best = -1;
            DateTime bestTime = DateTime.MinValue;
            for (int i = 0; i <= Slots; i++)
            {
                if (!Exists(i)) continue;
                DateTime t = File.GetLastWriteTimeUtc(PathFor(i));
                if (t > bestTime) { bestTime = t; best = i; }
            }
            return best;
        }

        public static string Describe(int slot)
        {
            SaveData d = Read(slot);
            if (d == null) return null;
            string mission = Game.Missions != null && d.StoryProgress < Game.Missions.Story.Count ? Game.Missions.Story[Mathf.Clamp(d.StoryProgress, 0, Game.Missions.Story.Count - 1)].Title : "Story complete";
            if (d.FreeRoam) mission = "Free roam";
            return d.SavedAt + "   " + MathUtil.Money(d.Money) + "   " + d.Completion.ToString("0") + "% complete\nNext: " + mission + "   Day " + d.Day + " " + MathUtil.Clock(d.Hours);
        }

        public static SaveData Read(int slot)
        {
            try
            {
                string p = PathFor(slot);
                if (!File.Exists(p)) return null;
                string json = File.ReadAllText(p);
                SaveData d = JsonUtility.FromJson<SaveData>(json);
                return d;
            }
            catch (Exception e)
            {
                Log.Exception("Read save " + slot, e);
                return null;
            }
        }

        public static bool Save(int slot)
        {
            try
            {
                SaveData d = Capture();
                if (d == null) return false;
                string json = JsonUtility.ToJson(d, true);
                string p = PathFor(slot);
                string tmp = p + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(p)) File.Delete(p);
                File.Move(tmp, p);
                return true;
            }
            catch (Exception e)
            {
                Log.Exception("Save " + slot, e);
                return false;
            }
        }

        public static SaveData Capture()
        {
            PlayerController p = Game.Player;
            if (p == null) return null;
            SaveData d = new SaveData();
            d.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            if (Game.Atmo != null)
            {
                d.Hours = Game.Atmo.Hours;
                d.Day = Game.Atmo.Day;
                d.Weather = (int)Game.Atmo.Target;
            }
            if (Game.Economy != null)
            {
                d.Money = Game.Economy.Money;
                d.Properties.AddRange(Game.Economy.OwnedProperties);
            }
            d.Health = p.Health.Health;
            d.Armor = p.Health.Armor;
            WeaponInventory inv = p.Weapons.Inventory;
            foreach (WeaponId w in inv.Owned) d.Weapons.Add((int)w);
            d.Clips.AddRange(inv.Clip);
            d.Ammo.AddRange(inv.Ammo);
            d.CurrentWeapon = (int)inv.Current;
            if (Game.Stats != null)
            {
                d.Stats.AddRange(Game.Stats.Values);
                d.Achievements.AddRange(Game.Stats.Unlocked);
                d.Completion = Game.Stats.CompletionPercent;
            }
            if (Game.Missions != null)
            {
                d.StoryProgress = Game.Missions.StoryProgress;
                d.Flags.AddRange(Game.Missions.Flags);
            }
            if (Game.Collectibles != null)
            {
                d.Chips.AddRange(Game.Collectibles.Chips);
                d.Shards.AddRange(Game.Collectibles.Shards);
                d.Stones.AddRange(Game.Collectibles.Stones);
                d.Relics.AddRange(Game.Collectibles.Relics);
                d.Jumps.AddRange(Game.Collectibles.Jumps);
            }
            if (Game.Activities != null) d.Activities.AddRange(Game.Activities.Completed);
            if (Game.Abilities != null)
            {
                d.Energy = Game.Abilities.Energy;
                d.MaxEnergy = Game.Abilities.BaseMaxEnergy;
                d.ThirdEye = Game.Abilities.ThirdEyeUnlocked;
                d.Spells.AddRange(Game.Abilities.Unlocked);
                d.SelectedSpell = Game.Abilities.Selected;
                d.Law = Game.Abilities.Law;
            }
            d.GangHeat.AddRange(Gangs.Heat);
            d.Outfit = Game.Menus != null ? Game.Menus.CurrentOutfit : 0;
            Vector3 pos = p.InVehicle ? p.CurrentVehicle.transform.position : p.transform.position;
            if (Game.Root != null && Game.Root.LastSafehouse != Vector3.zero)
            {
                pos = Game.Root.LastSafehouse;
                d.AtSafehouse = true;
            }
            d.PosX = pos.x;
            d.PosY = pos.y;
            d.PosZ = pos.z;
            d.Yaw = p.transform.eulerAngles.y;
            d.FreeRoam = Game.Root != null && Game.Root.FreeRoam;
            if (Game.Progress != null) Game.Progress.Capture(d);
            if (Game.Gear != null) Game.Gear.Capture(d);
            WorldSystems.CaptureAll(d);
            d.Difficulty = (int)Settings.Difficulty;
            return d;
        }
    }
}
