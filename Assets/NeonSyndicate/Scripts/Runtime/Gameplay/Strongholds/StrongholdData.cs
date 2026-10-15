using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    public enum FortModule { Empty, Watchtower, Conservatory, Bulwark, FlightDais }

    [Serializable]
    public sealed class StrongholdCatalog
    {
        public string version;
        public StrongholdDefinition[] sites;
        public void Validate()
        {
            if (version != "02I" || sites == null || sites.Length != 3) throw new InvalidOperationException("Stronghold catalog is incompatible.");
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int homes = 0;
            foreach (StrongholdDefinition d in sites)
            {
                if (d == null || !SafeId(d.id) || !ids.Add(d.id) || string.IsNullOrEmpty(d.title) || d.title.Length > 60 || d.title.Contains("\n") || d.title.Contains("\r") ||
                    d.anchor == null || d.anchor.Length != 3 || d.theme < 0 || d.theme > 2 || d.guardSpecies == null || d.guardSpecies.Length > 3)
                    throw new InvalidOperationException("Stronghold definition is incomplete.");
                foreach (float f in d.anchor) if (float.IsNaN(f) || float.IsInfinity(f) || Math.Abs(f) > 1500f) throw new InvalidOperationException("Stronghold anchor is invalid.");
                if (d.home) { homes++; if (d.guardSpecies.Length != 0) throw new InvalidOperationException("The home must start peaceful."); }
                else if (d.guardSpecies.Length == 0 || float.IsNaN(d.healthScale) || float.IsInfinity(d.healthScale) || float.IsNaN(d.damageScale) || float.IsInfinity(d.damageScale) || d.healthScale < .1f || d.healthScale > 2f || d.damageScale < .1f || d.damageScale > 1f)
                    throw new InvalidOperationException("Rival defender configuration is invalid.");
                foreach (string species in d.guardSpecies) if (!SafeId(species)) throw new InvalidOperationException("Invalid defender species.");
            }
            if (homes != 1 || sites[0].id != "hearthspire" || !sites[0].home || sites[1].id != "galehook" || sites[2].id != "sunscar")
                throw new InvalidOperationException("Stronghold IDs and home identity are save contracts.");
        }
        private static bool SafeId(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 40) return false;
            foreach (char c in s) if (!(c >= 'a' && c <= 'z') && c != '_' && !(c >= '0' && c <= '9')) return false;
            return true;
        }
    }

    [Serializable]
    public sealed class StrongholdDefinition
    {
        public string id, title, description;
        public bool home;
        public int theme;
        public float[] anchor;
        public string[] guardSpecies;
        public float healthScale = 1f, damageScale = 1f;
    }

    [Serializable]
    public sealed class FortSlotSave
    {
        public int Module;
        public int Rotation;
    }

    [Serializable]
    public sealed class StrongholdSiteSave
    {
        public string Id;
        // Local prototype identity only. Never trust this field as multiplayer authority.
        public string Owner = "unclaimed";
        public bool Owned;
        public bool EchoInfused;
        public int Theme;
        public List<FortSlotSave> Slots = new List<FortSlotSave>();
    }

    [Serializable]
    public sealed class StrongholdSave
    {
        public int Version = 1;
        public List<StrongholdSiteSave> Sites = new List<StrongholdSiteSave>();
        public string TrackedId = "hearthspire";
    }
}
