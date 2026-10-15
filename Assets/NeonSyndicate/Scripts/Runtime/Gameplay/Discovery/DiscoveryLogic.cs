using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class DiscoveryCatalog
    {
        public string version;
        public DiscoveryDefinition[] sites;
        public string circuitMaterial;
        public int circuitAmount;

        public void Validate()
        {
            if (version != "02G" || sites == null || sites.Length != 3 ||
                circuitMaterial != "core_prism" || circuitAmount != 1)
                throw new InvalidOperationException("02G discovery catalog is incompatible.");
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> landmarks = new HashSet<string>(StringComparer.Ordinal);
            foreach (DiscoveryDefinition d in sites)
            {
                if (d == null || !SafeId(d.id) || !ids.Add(d.id) || string.IsNullOrEmpty(d.title) ||
                    d.title.Length > 70 || string.IsNullOrEmpty(d.inscription) || d.inscription.Length > 700 ||
                    string.IsNullOrEmpty(d.memory) || d.memory.Length > 900 ||
                    d.order == null || d.order.Length != 3 || !landmarks.Add(d.landmark))
                    throw new InvalidOperationException("02G discovery definition is incomplete.");
                if (d.landmark != "aster_observatory" && d.landmark != "dawn_atrium" && d.landmark != "moonroot_garden")
                    throw new InvalidOperationException("02G discovery landmark is unknown.");
                int mask = 0;
                foreach (int rune in d.order)
                {
                    if (rune < 0 || rune > 2 || (mask & (1 << rune)) != 0)
                        throw new InvalidOperationException("02G rune order must contain Sun, Root and Star once each.");
                    mask |= 1 << rune;
                }
                if (!SafeId(d.material) || d.amount < 1 || d.amount > 5 || d.lumen < 1 || d.lumen > 300)
                    throw new InvalidOperationException("02G reward is outside the authored bounds.");
            }
        }
        private static bool SafeId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id) if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
            return true;
        }
    }

    [Serializable]
    public sealed class DiscoveryDefinition
    {
        public string id, title, landmark, inscription, memory, material;
        public int[] order;
        public int amount, lumen;
    }

    [Serializable]
    public sealed class DiscoveryEntrySave
    {
        public string Id;
        public bool Seen;
        public bool Read;
        public int Step;
        public bool Solved;
        /// <summary>Bit zero is Lumen; bit one is material. Reserved before external reward callbacks.</summary>
        public int RewardMask;
        public int Mistakes;
    }

    [Serializable]
    public sealed class DiscoverySave
    {
        public int Version = 1;
        public List<DiscoveryEntrySave> Entries = new List<DiscoveryEntrySave>();
        public bool CircuitClaimed;
        public string TrackedId = "";
    }

    public enum DiscoveryResult { Unavailable, NeedInscription, Correct, Incorrect, Solved, AlreadySolved }
    public enum DiscoveryCredit { Lumen, Material, CircuitMaterial }

    /// <summary>
    /// Engine-free puzzle and reward ledger. Does not read Input, Time, Unity objects or disk.
    /// Site IDs are authored, not derived from generated object names or runtime array order.
    /// </summary>
    public sealed class DiscoveryBook
    {
        private readonly DiscoveryCatalog _catalog;
        private readonly Dictionary<string, DiscoveryEntrySave> _entries = new Dictionary<string, DiscoveryEntrySave>(StringComparer.Ordinal);
        private bool _delivering;
        public bool CircuitClaimed { get; private set; }
        public string TrackedId { get; private set; }
        public int ClaimedCount
        {
            get { int n = 0; foreach (DiscoveryEntrySave s in _entries.Values) if (s.RewardMask == 3 && s.Solved) n++; return n; }
        }
        public int Total { get { return _catalog.sites.Length; } }
        public DiscoveryBook(DiscoveryCatalog catalog) { catalog.Validate(); _catalog = catalog; Reset(); }
        public void Reset()
        {
            _entries.Clear();
            foreach (DiscoveryDefinition def in _catalog.sites) _entries.Add(def.id, new DiscoveryEntrySave { Id = def.id });
            CircuitClaimed = false; TrackedId = ""; _delivering = false;
        }
        public DiscoveryDefinition Definition(string id)
        {
            foreach (DiscoveryDefinition d in _catalog.sites) if (d.id == id) return d;
            return null;
        }
        private DiscoveryEntrySave Entry(string id)
        {
            DiscoveryEntrySave s;
            return id != null && _entries.TryGetValue(id, out s) ? s : null;
        }
        public DiscoveryEntrySave Status(string id)
        {
            DiscoveryEntrySave s = Entry(id);
            return s == null ? null : Copy(s);
        }
        public bool Discover(string id)
        {
            DiscoveryEntrySave s = Entry(id);
            if (s == null || s.Seen) return false;
            s.Seen = true; return true;
        }
        public bool Read(string id)
        {
            DiscoveryEntrySave s = Entry(id);
            if (s == null) return false;
            s.Seen = true; s.Read = true; TrackedId = id; return true;
        }
        public void Track(string id) { TrackedId = Entry(id) != null ? id : ""; }
        public DiscoveryResult Touch(string id, int rune)
        {
            DiscoveryEntrySave s = Entry(id); DiscoveryDefinition d = Definition(id);
            if (s == null || d == null || rune < 0 || rune > 2) return DiscoveryResult.Unavailable;
            if (s.Solved) return DiscoveryResult.AlreadySolved;
            if (!s.Read) return DiscoveryResult.NeedInscription;
            if (d.order[s.Step] != rune)
            {
                s.Step = 0; s.Mistakes = Math.Min(s.Mistakes, 999998) + 1;
                return DiscoveryResult.Incorrect;
            }
            s.Step++;
            if (s.Step < d.order.Length) return DiscoveryResult.Correct;
            s.Solved = true; return DiscoveryResult.Solved;
        }
        /// <summary>Returns true only if all authored credits are settled. Full inventory leaves its credit pending.</summary>
        public bool Claim(string id, Func<DiscoveryCredit, string, int, bool> apply)
        {
            DiscoveryEntrySave s = Entry(id); DiscoveryDefinition d = Definition(id);
            if (s == null || d == null || !s.Solved || apply == null || _delivering) return false;
            _delivering = true;
            try
            {
                Credit(s, 1, DiscoveryCredit.Lumen, "", d.lumen, apply);
                Credit(s, 2, DiscoveryCredit.Material, d.material, d.amount, apply);
                return s.RewardMask == 3;
            }
            finally { _delivering = false; }
        }
        private static void Credit(DiscoveryEntrySave s, int bit, DiscoveryCredit kind, string id, int amount,
            Func<DiscoveryCredit, string, int, bool> apply)
        {
            if ((s.RewardMask & bit) != 0) return;
            // Reservation happens BEFORE events in the existing inventory can trigger a save or re-entrant use.
            s.RewardMask |= bit;
            bool applied = false;
            try { applied = apply(kind, id, amount); }
            finally { if (!applied) s.RewardMask &= ~bit; }
        }
        public bool ClaimCircuit(Func<DiscoveryCredit, string, int, bool> apply)
        {
            if (_delivering || CircuitClaimed || ClaimedCount != Total || apply == null) return false;
            _delivering = true; CircuitClaimed = true;
            bool applied = false;
            try { applied = apply(DiscoveryCredit.CircuitMaterial, _catalog.circuitMaterial, _catalog.circuitAmount); return applied; }
            finally { if (!applied) CircuitClaimed = false; _delivering = false; }
        }
        public DiscoverySave Capture()
        {
            DiscoverySave save = new DiscoverySave { CircuitClaimed = CircuitClaimed, TrackedId = TrackedId };
            foreach (DiscoveryDefinition d in _catalog.sites) save.Entries.Add(Copy(_entries[d.id]));
            return save;
        }
        public void Restore(DiscoverySave save)
        {
            Reset();
            if (save == null || save.Entries == null) return;
            // Unknown future IDs are ignored. Duplicate IDs merge monotonically, preventing reward resets.
            foreach (DiscoveryEntrySave read in save.Entries)
            {
                if (read == null) continue;
                DiscoveryEntrySave s = Entry(read.Id);
                if (s == null) continue;
                s.Seen |= read.Seen; s.Read |= read.Read;
                s.Solved |= read.Solved || (read.RewardMask & 3) != 0;
                s.RewardMask |= read.RewardMask & 3;
                s.Mistakes = Math.Max(s.Mistakes, Math.Max(0, Math.Min(read.Mistakes, 999999)));
                s.Step = Math.Max(s.Step, Math.Max(0, Math.Min(read.Step, 2)));
                if (s.Solved) { s.Seen = s.Read = true; s.Step = 3; }
                else if (s.Read) s.Seen = true;
                else s.Step = 0;
            }
            CircuitClaimed = save.CircuitClaimed && ClaimedCount == Total;
            Track(save.TrackedId);
        }
        private static DiscoveryEntrySave Copy(DiscoveryEntrySave s)
        {
            return new DiscoveryEntrySave { Id = s.Id, Seen = s.Seen, Read = s.Read, Step = s.Step,
                Solved = s.Solved, RewardMask = s.RewardMask, Mistakes = s.Mistakes };
        }
        public static string RuneName(int rune)
        {
            switch (rune) { case 0: return "SUN"; case 1: return "ROOT"; case 2: return "STAR"; default: return "?"; }
        }
        public string OrderText(string id)
        {
            DiscoveryDefinition d = Definition(id);
            return d == null ? "" : RuneName(d.order[0]) + " > " + RuneName(d.order[1]) + " > " + RuneName(d.order[2]);
        }
    }
}
