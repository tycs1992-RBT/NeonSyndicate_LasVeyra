using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum StatId
    {
        TimePlayed,
        DistanceOnFoot,
        DistanceDriven,
        ShotsFired,
        Kills,
        CiviliansKilled,
        CopsKilled,
        GangKills,
        Headshots,
        VehiclesStolen,
        VehiclesDestroyed,
        HelicoptersDowned,
        PropsSmashed,
        Bailouts,
        StuntJumps,
        MissionsPassed,
        MissionsFailed,
        Deaths,
        Busts,
        MaxWantedLevel,
        TimesEvaded,
        MoneyEarned,
        MoneySpent,
        RacesWon,
        Rampages,
        ContractsDone,
        TaxiFares,
        VigilanteKills,
        StoresRobbed,
        DataChips,
        CrystalShards,
        SpellStones,
        CreatureSightings,
        PropertiesOwned,
        CarsResprayed,
        CasinoWinnings,
        TopSpeedMph,
        LongestJump,
        PatientsSaved,
        Count
    }

    public sealed class Achievement
    {
        public string Id;
        public string Name;
        public string Description;
        public Func<Stats, bool> Condition;
    }

    /// <summary>Career statistics and achievements (shown in the pause menu, saved with the game).</summary>
    public sealed class Stats : MonoBehaviour
    {
        public readonly float[] Values = new float[(int)StatId.Count];
        public readonly HashSet<string> Unlocked = new HashSet<string>();
        private float _checkTimer;
        private Vector3 _lastPos;
        private bool _hasLast;

        public static readonly Achievement[] Achievements =
        {
            new Achievement { Id = "welcome", Name = "Welcome to Las Veyra", Description = "Complete the first mission.", Condition = s => s.Get(StatId.MissionsPassed) >= 1 },
            new Achievement { Id = "story", Name = "Sunrise Debt Paid", Description = "Finish the story.", Condition = s => s.Missions >= 12 },
            new Achievement { Id = "thief", Name = "Five-Finger Discount", Description = "Steal 25 vehicles.", Condition = s => s.Get(StatId.VehiclesStolen) >= 25 },
            new Achievement { Id = "wrecker", Name = "Scrapyard Regular", Description = "Destroy 50 vehicles.", Condition = s => s.Get(StatId.VehiclesDestroyed) >= 50 },
            new Achievement { Id = "five", Name = "Maximum Response", Description = "Reach a five star wanted level.", Condition = s => s.Get(StatId.MaxWantedLevel) >= 5 },
            new Achievement { Id = "ghost", Name = "Ghost Lanes", Description = "Lose the police 10 times.", Condition = s => s.Get(StatId.TimesEvaded) >= 10 },
            new Achievement { Id = "marksman", Name = "Glass Saint Marksman", Description = "Land 100 headshots.", Condition = s => s.Get(StatId.Headshots) >= 100 },
            new Achievement { Id = "air", Name = "Grounded", Description = "Bring down a police helicopter.", Condition = s => s.Get(StatId.HelicoptersDowned) >= 1 },
            new Achievement { Id = "stunts", Name = "Skyrail Dreamer", Description = "Complete 10 unique stunt jumps.", Condition = s => s.Get(StatId.StuntJumps) >= 10 },
            new Achievement { Id = "chips", Name = "Data Hoarder", Description = "Collect all 50 data chips.", Condition = s => s.Get(StatId.DataChips) >= 50 },
            new Achievement { Id = "shards", Name = "Crystal Cartographer", Description = "Collect all 20 crystal shards.", Condition = s => s.Get(StatId.CrystalShards) >= 20 },
            new Achievement { Id = "stones", Name = "Spellbound", Description = "Find all 10 spell stones.", Condition = s => s.Get(StatId.SpellStones) >= 10 },
            new Achievement { Id = "relics", Name = "Echo Keeper", Description = "Attune to all 10 Echo Relics.", Condition = s => s.Get(StatId.CreatureSightings) >= 10 },
            new Achievement { Id = "tycoon", Name = "Night Market Tycoon", Description = "Own every property.", Condition = s => s.Get(StatId.PropertiesOwned) >= 10 },
            new Achievement { Id = "racer", Name = "Midnight Torque", Description = "Win every street race.", Condition = s => s.Get(StatId.RacesWon) >= 4 },
            new Achievement { Id = "taxi", Name = "Exact Change", Description = "Complete 20 taxi fares.", Condition = s => s.Get(StatId.TaxiFares) >= 20 },
            new Achievement { Id = "rich", Name = "Crownless Millionaire", Description = "Earn $1,000,000 in total.", Condition = s => s.Get(StatId.MoneyEarned) >= 1000000f },
            new Achievement { Id = "speed", Name = "Faster Than The Signal", Description = "Reach 150 mph in a vehicle.", Condition = s => s.Get(StatId.TopSpeedMph) >= 150f },
            new Achievement { Id = "props", Name = "Hydrant Hunter", Description = "Smash 200 street props.", Condition = s => s.Get(StatId.PropsSmashed) >= 200 },
            new Achievement { Id = "rampage", Name = "Rampage Royalty", Description = "Complete 3 rampages.", Condition = s => s.Get(StatId.Rampages) >= 3 },
        };

        public int Missions { get { return Game.Missions != null ? Game.Missions.StoryProgress : 0; } }

        public float Get(StatId id)
        {
            return Values[(int)id];
        }

        public void Add(StatId id, float amount)
        {
            Values[(int)id] += amount;
        }

        public void Max(StatId id, float value)
        {
            if (value > Values[(int)id]) Values[(int)id] = value;
        }

        public void Set(StatId id, float value)
        {
            Values[(int)id] = value;
        }

        public static string Label(StatId id)
        {
            switch (id)
            {
                case StatId.TimePlayed: return "Time played";
                case StatId.DistanceOnFoot: return "Distance on foot";
                case StatId.DistanceDriven: return "Distance driven";
                case StatId.ShotsFired: return "Shots fired";
                case StatId.CiviliansKilled: return "Civilians killed";
                case StatId.CopsKilled: return "Police killed";
                case StatId.GangKills: return "Gang members killed";
                case StatId.VehiclesStolen: return "Vehicles stolen";
                case StatId.VehiclesDestroyed: return "Vehicles destroyed";
                case StatId.HelicoptersDowned: return "Helicopters downed";
                case StatId.PropsSmashed: return "Street props smashed";
                case StatId.StuntJumps: return "Unique stunt jumps";
                case StatId.MissionsPassed: return "Missions passed";
                case StatId.MissionsFailed: return "Missions failed";
                case StatId.MaxWantedLevel: return "Highest wanted level";
                case StatId.TimesEvaded: return "Police evaded";
                case StatId.MoneyEarned: return "Money earned";
                case StatId.MoneySpent: return "Money spent";
                case StatId.RacesWon: return "Races won";
                case StatId.ContractsDone: return "Payphone contracts";
                case StatId.TaxiFares: return "Taxi fares";
                case StatId.VigilanteKills: return "Vigilante takedowns";
                case StatId.StoresRobbed: return "Stores robbed";
                case StatId.DataChips: return "Data chips";
                case StatId.CrystalShards: return "Crystal shards";
                case StatId.SpellStones: return "Spell stones";
                case StatId.CreatureSightings: return "Echo Relics";
                case StatId.PropertiesOwned: return "Properties owned";
                case StatId.CarsResprayed: return "Resprays";
                case StatId.CasinoWinnings: return "Casino winnings";
                case StatId.TopSpeedMph: return "Top speed (mph)";
                case StatId.LongestJump: return "Longest jump (m)";
                case StatId.PatientsSaved: return "Patients saved";
                case StatId.Rampages: return "Rampages completed";
                default: return id.ToString();
            }
        }

        public string Format(StatId id)
        {
            float v = Get(id);
            switch (id)
            {
                case StatId.TimePlayed:
                    int h = (int)(v / 3600f), m = (int)(v % 3600f / 60f);
                    return h + "h " + m.ToString("00") + "m";
                case StatId.DistanceOnFoot:
                case StatId.DistanceDriven:
                    return (v / 1609.34f).ToString("0.0") + " mi";
                case StatId.MoneyEarned:
                case StatId.MoneySpent:
                case StatId.CasinoWinnings:
                    return MathUtil.Money(Mathf.RoundToInt(v));
                case StatId.LongestJump:
                case StatId.TopSpeedMph:
                    return v.ToString("0");
                default:
                    return Mathf.RoundToInt(v).ToString("N0");
            }
        }

        public float CompletionPercent
        {
            get
            {
                float story = Mathf.Clamp01(Missions / 12f) * 40f;
                float chips = Get(StatId.DataChips) / 50f * 8f;
                float shards = Get(StatId.CrystalShards) / 20f * 8f;
                float stones = Get(StatId.SpellStones) / 10f * 8f;
                float creatures = Get(StatId.CreatureSightings) / 10f * 6f;
                float stunts = Mathf.Min(1f, Get(StatId.StuntJumps) / Mathf.Max(1f, Game.Layout != null ? Game.Layout.StuntJumps.Count : 12f)) * 8f;
                float props = Get(StatId.PropertiesOwned) / 10f * 8f;
                float races = Mathf.Min(1f, Get(StatId.RacesWon) / 4f) * 6f;
                float rampages = Mathf.Min(1f, Get(StatId.Rampages) / 3f) * 4f;
                float contracts = Mathf.Min(1f, Get(StatId.ContractsDone) / 6f) * 4f;
                return Mathf.Clamp(story + chips + shards + stones + creatures + stunts + props + races + rampages + contracts, 0f, 100f);
            }
        }

        private void Update()
        {
            if (!Game.IsPlaying) return;
            float dt = Time.deltaTime;
            Add(StatId.TimePlayed, Time.unscaledDeltaTime);
            if (Game.Player != null)
            {
                Vector3 p = Game.PlayerPosition;
                if (_hasLast)
                {
                    float d = MathUtil.FlatDistance(p, _lastPos);
                    if (d < 50f) Add(Game.Player.InVehicle ? StatId.DistanceDriven : StatId.DistanceOnFoot, d);
                }
                _lastPos = p;
                _hasLast = true;
                if (Game.Player.InVehicle) Max(StatId.TopSpeedMph, Game.Player.CurrentVehicle.Speed * 2.23694f);
            }
            _checkTimer -= dt;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 1f;
                CheckAchievements();
            }
        }

        public void CheckAchievements()
        {
            foreach (Achievement a in Achievements)
            {
                if (Unlocked.Contains(a.Id)) continue;
                bool ok;
                try { ok = a.Condition(this); }
                catch (Exception) { ok = false; }
                if (!ok) continue;
                Unlocked.Add(a.Id);
                if (Game.Hud != null) Game.Hud.Banner("ACHIEVEMENT UNLOCKED", a.Name + " - " + a.Description, new Color(1f, 0.85f, 0.3f), 4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionPassed, 0.5f, 1.3f);
            }
        }
    }
}
