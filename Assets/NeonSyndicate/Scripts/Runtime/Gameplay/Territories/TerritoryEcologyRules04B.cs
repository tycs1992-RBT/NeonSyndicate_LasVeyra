using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Authored ecology data. Positions are local to the surveyed habitat, never global spawn guesses.</summary>
    [Serializable]
    public sealed class TerritoryHabitat04B
    {
        public string version;
        public string id;
        public string title;
        public string region;
        public string role;
        public string careLabel;
        public bool care;
        public TerritoryHabitatSlot04B[] slots;
    }

    [Serializable]
    public sealed class TerritoryHabitatSlot04B
    {
        public string day;
        public string night;
        public string wet;
        public float[] position;
        public float yaw;
        public float clearance;
        public float roam;
        public bool aerial;
        public int minimumPreset;
    }

    /// <summary>Immutable care candidate. The territory ledger, not ecology, owns reward persistence.</summary>
    public struct TerritoryCare04B
    {
        public string Id;
        public string Habitat;
        public string Species;
        public string Label;
        public Vector3 Position;
    }

    /// <summary>Engine-free selection, caps, stable seeds and hysteresis shared by runtime and source tests.</summary>
    public static class TerritoryEcologyRules04B
    {
        public const float EnterDistance = 170f;
        public const float ExitDistance = 255f;
        public const float RetrySeconds = 75f;
        public const float CareDistance = 8f;
        public const int DirectorReserve = 4;

        public static void ResetStatics() { }

        public static int CreatureCap(int preset) { return preset <= 0 ? 3 : preset == 1 ? 5 : preset == 2 ? 8 : 10; }
        public static int StaffCap(int preset) { return preset <= 0 ? 2 : preset == 1 ? 4 : preset == 2 ? 6 : 8; }
        public static int StaffPerSite(int preset, bool hearth) { return hearth ? 1 : preset <= 0 ? 1 : preset == 1 ? 2 : 3; }
        public static int DirectorLimit(int preset) { return preset <= 0 ? 18 : preset == 1 ? 26 : preset == 2 ? 36 : 42; }
        public static bool CanAllocateCreature(int preset, int owned, int directorAlive)
        { return owned < CreatureCap(preset) && directorAlive < DirectorLimit(preset) - DirectorReserve; }
        public static bool ShouldLoad(bool loaded, float distance) { return loaded ? distance <= ExitDistance : distance < EnterDistance; }
        public static bool Night(float hours) { return hours < 6f || hours >= 19f; }
        public static bool SevereWeather(int weather) { return weather == (int)Weather.Storm || weather == (int)Weather.Sandstorm; }

        public static string Species(TerritoryHabitatSlot04B slot, float hours, int weather)
        {
            if (slot == null) return null;
            bool wet = weather == (int)Weather.Rain || weather == (int)Weather.Storm;
            return wet && !string.IsNullOrEmpty(slot.wet) ? slot.wet : Night(hours) ? slot.night : slot.day;
        }

        public static bool SlotEnabled(TerritoryHabitatSlot04B slot, int preset, int weather)
        {
            return slot != null && preset >= slot.minimumPreset && (!slot.aerial || !SevereWeather(weather));
        }

        public static int Seed(int worldSeed, string id, int index)
        {
            unchecked
            {
                uint h = 2166136261u ^ (uint)worldSeed;
                if (id != null) for (int i = 0; i < id.Length; i++) h = (h ^ id[i]) * 16777619u;
                h = (h ^ (uint)(index + 1)) * 16777619u;
                int result = (int)(h & 0x7fffffffu);
                return result == 0 ? 1 : result;
            }
        }

        public static bool Valid(TerritoryHabitat04B data, out string error)
        {
            error = null;
            if (data == null || data.version != "04B" || string.IsNullOrEmpty(data.id) || data.slots == null || data.slots.Length < 1 || data.slots.Length > 3)
            { error = "Invalid habitat header or slot count"; return false; }
            if (data.region != "amethyst" && data.region != "verdant" && data.region != "obsidian")
            { error = "Unknown territorial region"; return false; }
            for (int i = 0; i < data.slots.Length; i++)
            {
                TerritoryHabitatSlot04B slot = data.slots[i];
                if (slot == null || slot.position == null || slot.position.Length != 3 || !Finite(slot.yaw) ||
                    !Finite(slot.clearance) || slot.clearance < 0.4f || slot.clearance > 10f || !Finite(slot.roam) ||
                    slot.roam < 0f || slot.roam > (slot.aerial ? 9f : 3f) || slot.minimumPreset < 0 || slot.minimumPreset > 3)
                { error = "Invalid habitat slot envelope"; return false; }
                for (int j = 0; j < 3; j++) if (!Finite(slot.position[j]) || Math.Abs(slot.position[j]) > 24f)
                { error = "Habitat slot outside authored plot"; return false; }
                string[] ids = { slot.day, slot.night, slot.wet };
                foreach (string id in ids)
                {
                    CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                    if (sp == null || sp.Boss || sp.IsHumanoid || (slot.aerial && !sp.Flies))
                    { error = "Missing or incompatible habitat species " + id; return false; }
                }
            }
            return true;
        }

        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }

        public static Vector3 StaffOffset(int index, bool hearth)
        {
            return hearth ? Vector3.zero : index == 0 ? new Vector3(-9f, 0f, -3f) :
                index == 1 ? new Vector3(9f, 0f, -3f) : new Vector3(-7f, 0f, -4f);
        }

        public static float StaffYaw(int index) { return index == 1 ? 135f : 225f; }

        /// <summary>Existing body generator and palette. No imported or advertised bespoke animation clips.</summary>
        public static Appearance StaffLook(string region, int seed)
        {
            Appearance look = Appearance.Random(new System.Random(seed));
            look.SuitStyle = region == "obsidian" ? 3 : 1;
            look.Headwear = Headwear.None;
            look.Jacket = true;
            look.LongSleeves = true;
            look.JacketRow = look.ShirtRow = 1;
            look.JacketColor = region == "verdant" ? 6 : region == "obsidian" ? 0 : 8;
            look.Shirt = region == "verdant" ? 5 : region == "obsidian" ? 3 : 10;
            look.Glow = region == "verdant" ? 3 : region == "obsidian" ? 5 : 2;
            look.ShellStyle = region == "obsidian" ? 1 : 0;
            return look;
        }
    }
}
