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
        // 04C append-only venue discoveries and finite activities. Inside saves resume at a verified exterior entrance.
        public InhabitedCitySave InhabitedCity = new InhabitedCitySave();
        // 04D append-only crystal arsenal ledger: offers, durable reward entitlements and pending claims.
        public CrystalArsenalSave CrystalArsenal = new CrystalArsenalSave();
        // 04E Underheart and Bonds: append-only blocks. Old saves load them empty: no reward, bond or shortcut is granted.
        public UnderheartSave Underheart = new UnderheartSave();
        public GuidanceSave Guidance04E = new GuidanceSave();
        public CampaignSave Campaign04E = new CampaignSave();
        public BondSave Bonds = new BondSave();
        public GarageSave Garage = new GarageSave();
        public BuildSave Builds = new BuildSave();
        // 04E (appended): the last safehouse chosen on the surface. An Underheart rest point never replaces it, so a save
        // made on the surface after a rest underground stores this, and an underground save keeps it for later surface
        // saves. Old saves have none (false): they use their own surface safehouse, else the home spawn.
        public bool HasSurfaceSafehouse;
        public float SurfaceSafeX, SurfaceSafeY, SurfaceSafeZ;
        // 04F: missing block starts with full charge, no companion or quest unlock. Owned weapon ids stay intact.
        public CrystalCombatSave04F CrystalCombat = new CrystalCombatSave04F();
        public int WorldRevision04O;
    }

    /// <summary>JSON save games (autosave plus three slots) in Application.persistentDataPath.</summary>
    public static class SaveSystem
    {
        public const int Slots = 3;

        // Revision 0 and 1 keep every established filename. Revision 2 has an isolated bank,
        // including autosave and its backup. Reading another bank never copies or migrates it.
        public static int ActiveBankRevision04OB
        {
            get { return Game.Layout == null ? 0 : BankForRevision04OC((int)Game.Layout.Revision04O); }
        }

        public static int BankForRevision04OC(int revision)
        {
            if (!CityRebirthRules04O.Known(revision)) throw new ArgumentOutOfRangeException(nameof(revision));
            return revision >= 2 ? revision : 0;
        }

        public static string SlotFileName04OB(int slot, int revision)
        {
            if (slot < 0 || slot > Slots) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!CityRebirthRules04O.Known(revision)) throw new ArgumentOutOfRangeException(nameof(revision));
            return (revision == 6 ? "lasveyra_frontier04og_save_" : revision == 5 ? "lasveyra_unbound04oe_save_" : revision == 4 ? "lasveyra_realms04od_save_" : revision == 3 ? "lasveyra_wilds04oc_save_" : revision == 2 ? "lasveyra_realm04ob_save_" : "lasveyra_save_") + slot + ".json";
        }

        public static string PathFor(int slot) { return PathFor(slot, ActiveBankRevision04OB); }

        public static string PathFor(int slot, int revision)
        {
            return Path.Combine(Application.persistentDataPath, SlotFileName04OB(slot, revision));
        }

        public static bool Exists(int slot) { return Exists(slot, ActiveBankRevision04OB); }

        public static bool Exists(int slot, int revision)
        {
            // A slot whose main file was lost mid-install still counts while its verified backup survives.
            return SaveFileTransaction.AnyExists(PathFor(slot, revision));
        }

        /// <summary>Deletes a slot completely: the save, its verified backup and any half-written temporary file.</summary>
        public static void Delete(int slot)
        {
            string p = PathFor(slot);
            string[] files = { p, SaveFileTransaction.BackupPath(p), SaveFileTransaction.TempPath(p) };
            foreach (string f in files)
            {
                try { if (File.Exists(f)) File.Delete(f); }
                catch (Exception e) { Log.Exception("Delete save " + slot, e); }
            }
        }

        /// <summary>Why the last Save call failed (null after a success); menus show it instead of a false success.</summary>
        public static string LastError { get; private set; }

        public static void ResetStatics()
        {
            LastError = null;
        }

        /// <summary>A save text is usable when JsonUtility reads it into a SaveData with a version.</summary>
        public static bool BankMatches04OC(int storedRevision, int selectedRevision)
        {
            return CityRebirthRules04O.Known(storedRevision) && CityRebirthRules04O.Known(selectedRevision) &&
                BankForRevision04OC(storedRevision) == BankForRevision04OC(selectedRevision);
        }

        private static bool ValidSave(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            SaveData d = JsonUtility.FromJson<SaveData>(json);
            return d != null && d.Version >= 1;
        }

        public static bool AnyExists() { return AnyExists(ActiveBankRevision04OB); }

        public static bool AnyExists(int revision)
        {
            for (int i = 0; i <= Slots; i++) if (Exists(i, revision)) return true;
            return false;
        }

        public static int MostRecentSlot()
        {
            int best = -1;
            DateTime bestTime = DateTime.MinValue;
            for (int i = 0; i <= Slots; i++)
            {
                if (!Exists(i)) continue;
                string path = File.Exists(PathFor(i)) ? PathFor(i) : SaveFileTransaction.BackupPath(PathFor(i));
                DateTime t = File.GetLastWriteTimeUtc(path);
                if (t > bestTime) { bestTime = t; best = i; }
            }
            return best;
        }

        public static string Describe(int slot) { return Describe(slot, ActiveBankRevision04OB); }

        public static string Describe(int slot, int revision)
        {
            SaveData d = Read(slot, revision);
            if (d == null) return null;
            string mission = Game.Missions != null && d.StoryProgress < Game.Missions.Story.Count ? Game.Missions.Story[Mathf.Clamp(d.StoryProgress, 0, Game.Missions.Story.Count - 1)].Title : "Story complete";
            if (d.FreeRoam) mission = "Free roam";
            return d.SavedAt + "   " + MathUtil.Money(d.Money) + "   " + d.Completion.ToString("0") + "% complete\nNext: " + mission + "   Day " + d.Day + " " + MathUtil.Clock(d.Hours);
        }

        public static SaveData Read(int slot) { return Read(slot, ActiveBankRevision04OB); }

        public static SaveData Read(int slot, int revision)
        {
            try
            {
                string p = PathFor(slot, revision);
                bool fromBackup;
                string json = SaveFileTransaction.ReadValidated(p, text =>
                {
                    if (!ValidSave(text)) return false;
                    SaveData candidate = JsonUtility.FromJson<SaveData>(text);
                    return BankMatches04OC(candidate.WorldRevision04O, revision);
                }, out fromBackup);
                if (json == null) return null;
                if (fromBackup) Log.Warn("Save " + slot + ": the main file was unusable, loaded its verified backup.");
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
                if (d == null)
                {
                    LastError = "Nothing to save yet.";
                    return false;
                }
                string json = JsonUtility.ToJson(d, true);
                string error;
                // Write, flush, read back and validate a temporary file, then install it keeping the old save as a backup.
                SaveFileTransaction.Result r = SaveFileTransaction.Write(PathFor(slot, d.WorldRevision04O), json, ValidSave, out error);
                if (r != SaveFileTransaction.Result.Ok)
                {
                    LastError = "The save could not be written (" + r + "). Your previous save is still on disk.";
                    Log.Error("Save " + slot + " failed: " + r + " " + error);
                    return false;
                }
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = "The save could not be written. Your previous save is still on disk.";
                Log.Exception("Save " + slot, e);
                return false;
            }
        }

        public static SaveData Capture()
        {
            PlayerController p = Game.Player;
            if (p == null) return null;
            SaveData d = new SaveData();
            d.WorldRevision04O = Game.Layout != null ? (int)Game.Layout.Revision04O : 0;
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
            GameRoot root = Game.Root;
            if (root != null)
            {
                // 04E: an underground rest point is the resume point for saves made underground only. The same test as the
                // Underheart block's saved region, so the two always agree about where the save was made.
                Vector3 last = root.LastSafehouse;
                bool below = WorldBounds.IsUnderground(Game.PlayerPosition);
                Vector3 surface = !below && SafehouseRules04E.Usable(last) && !SafehouseRules04E.IsSurface(last) ? root.SurfaceSafehouseOrHome() : root.LastSurfaceSafehouse;
                bool atSafehouse;
                pos = SafehouseRules04E.SavePosition(pos, below, last, surface, out atSafehouse);
                d.AtSafehouse = atSafehouse;
                Vector3 keep = root.LastSurfaceSafehouse;
                if (SafehouseRules04E.Usable(keep) && SafehouseRules04E.IsSurface(keep))
                {
                    d.HasSurfaceSafehouse = true;
                    d.SurfaceSafeX = keep.x;
                    d.SurfaceSafeY = keep.y;
                    d.SurfaceSafeZ = keep.z;
                }
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
