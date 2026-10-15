using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Stable content identities; ownership and rewards use the existing save and material services.</summary>
    [Serializable] public sealed class TerritoryCatalog
    {
        public string version;
        public TerritoryDefinition[] sites;
        public void Validate()
        {
            if (version != "04B" || sites == null || sites.Length != 9) throw new InvalidOperationException("04B requires nine relay definitions.");
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int[] regions = new int[3];
            foreach (TerritoryDefinition d in sites)
            {
                if (d == null || !SafeId(d.id) || !ids.Add(d.id) || string.IsNullOrEmpty(d.title) || d.title.Length > 64 ||
                    d.title.Contains("\n") || d.title.Contains("\r") || d.anchor == null || d.anchor.Length != 3 ||
                    d.variant < 0 || d.variant > 2 || d.guards == null || d.guards.Length < 1 || d.guards.Length > 3 ||
                    d.rewardCount < 1 || d.rewardCount > 12 || !SafeId(d.rewardMaterial) ||
                    d.events == null || d.events.Length > 2)
                    throw new InvalidOperationException("Incomplete 04B relay definition.");
                int region = d.region == "amethyst" ? 0 : d.region == "verdant" ? 1 : d.region == "obsidian" ? 2 : -1;
                if (region < 0 || d.layout != "relay_" + d.region) throw new InvalidOperationException("Invalid territory region or layout.");
                regions[region]++;
                foreach (float f in d.anchor) if (float.IsNaN(f) || float.IsInfinity(f) || Math.Abs(f) > 1500f) throw new InvalidOperationException("Invalid relay anchor.");
                foreach (string id in d.guards) if (!SafeId(id)) throw new InvalidOperationException("Invalid relay defender.");
                foreach (string id in d.events) if (!SafeId(id)) throw new InvalidOperationException("Invalid relay activity.");
            }
            foreach (int count in regions) if (count != 3) throw new InvalidOperationException("Each region requires three relay identities.");
        }
        public static bool SafeId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64) return false;
            foreach (char c in value) if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }
        public static void ResetStatics() { }
    }
    [Serializable] public sealed class TerritoryDefinition
    {
        public string id, title, region, layout, description, resource, rewardMaterial;
        public float[] anchor;
        public string[] guards, events;
        public int variant, rewardCount;
        public bool hub;
    }
    [Serializable] public sealed class TerritorySiteSave
    {
        public string Id;
        public bool Owned, Discovered, RewardClaimed;
        public int Fortification;
        public float RestCooldown;
    }
    [Serializable] public sealed class TerritorySave
    {
        public int Version = 1;
        public List<TerritorySiteSave> Sites = new List<TerritorySiteSave>();
        public List<string> Claims = new List<string>();
        public string TrackedId = "";
    }
}
