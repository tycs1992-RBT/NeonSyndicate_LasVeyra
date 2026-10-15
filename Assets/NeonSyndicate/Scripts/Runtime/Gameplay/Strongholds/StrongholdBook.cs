using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Engine-free ownership, construction and one-time crystal ledger. Raid state is deliberately transient.</summary>
    public sealed class StrongholdBook
    {
        public const int SlotCount = 6;
        public const float CaptureSeconds = 5f;
        private readonly StrongholdCatalog _catalog;
        private readonly Dictionary<string, StrongholdSiteSave> _sites = new Dictionary<string, StrongholdSiteSave>(StringComparer.Ordinal);
        private int _expected, _defeated;
        public string ActiveRaid { get; private set; }
        public string TrackedId { get; private set; }
        public float CaptureProgress { get; private set; }
        public int RemainingGuards { get { return _expected - BitCount(_defeated); } }
        public int Resonance
        {
            get { int n = 0; foreach (StrongholdDefinition d in _catalog.sites) if (!d.home && _sites[d.id].Owned && _sites[d.id].EchoInfused) n++; return Math.Min(2,n); }
        }
        public int PendingEchoes
        {
            get { int n = 0; foreach (StrongholdDefinition d in _catalog.sites) if (!d.home && _sites[d.id].Owned && !_sites[d.id].EchoInfused) n++; return n; }
        }
        public int Tier { get { return _sites["hearthspire"].Owned ? 1 + Resonance : 0; } }
        public float FlightMultiplier { get { return 1f + Resonance * .04f; } }
        public float ClimbMultiplier { get { return 1f + Resonance * .03f; } }
        public StrongholdBook(StrongholdCatalog catalog) { catalog.Validate(); _catalog = catalog; Reset(); }
        public void Reset()
        {
            _sites.Clear();
            foreach (StrongholdDefinition d in _catalog.sites)
            {
                StrongholdSiteSave s = new StrongholdSiteSave { Id = d.id, Theme = d.theme, Owner = d.home ? "unclaimed" : "npc_" + d.id };
                for (int i = 0; i < SlotCount; i++) s.Slots.Add(new FortSlotSave { Module = (i % 2 == 0) ? 1 : 3, Rotation = 0 });
                _sites.Add(d.id,s);
            }
            TrackedId = "hearthspire"; CancelRaid();
        }
        public StrongholdDefinition Definition(string id)
        {
            foreach (StrongholdDefinition d in _catalog.sites) if (d.id == id) return d;
            return null;
        }
        public StrongholdSiteSave Status(string id)
        {
            StrongholdSiteSave s; return id != null && _sites.TryGetValue(id,out s) ? Copy(s) : null;
        }
        public bool Track(string id) { if (Definition(id) == null) return false; TrackedId = id; return true; }
        public bool ClaimHome()
        {
            if (_sites["hearthspire"].Owned || ActiveRaid != null) return false;
            _sites["hearthspire"].Owned = true; _sites["hearthspire"].Owner = "local_malik"; return true;
        }
        public bool SetTheme(string id, int theme)
        {
            StrongholdSiteSave s;
            if (id == null || !_sites.TryGetValue(id,out s) || !s.Owned || ActiveRaid != null || theme < 0 || theme > 2) return false;
            s.Theme = theme; return true;
        }
        public bool Configure(string id, int slot, int module, int rotation)
        {
            StrongholdSiteSave s;
            if (id == null || !_sites.TryGetValue(id,out s) || !s.Owned || ActiveRaid != null || slot < 0 || slot >= SlotCount || module < 0 || module > 4 || rotation < 0 || rotation > 3) return false;
            s.Slots[slot] = new FortSlotSave { Module = module, Rotation = rotation }; return true;
        }
        public bool BeginRaid(string id, int guards)
        {
            StrongholdDefinition d = Definition(id);
            if (d == null || d.home || !_sites["hearthspire"].Owned || _sites[id].Owned || ActiveRaid != null || guards != d.guardSpecies.Length || guards < 1 || guards > 3) return false;
            ActiveRaid = id; _expected = guards; _defeated = 0; CaptureProgress = 0f; return true;
        }
        public bool DefeatGuard(string id, int index)
        {
            if (ActiveRaid == null || ActiveRaid != id || index < 0 || index >= _expected || (_defeated & (1 << index)) != 0) return false;
            _defeated |= 1 << index; return true;
        }
        public bool AdvanceCapture(string id, float dt, bool atCore, bool safe)
        {
            if (ActiveRaid == null || ActiveRaid != id || RemainingGuards != 0 || !atCore || !safe || float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f)
            { if (ActiveRaid == id) CaptureProgress = 0f; return false; }
            CaptureProgress += Math.Min(.1f,dt);
            if (CaptureProgress + .0001f < CaptureSeconds) return false;
            StrongholdSiteSave s = _sites[id]; s.Owned = true; s.Owner = "local_malik";
            CancelRaid(); return true;
        }
        public void InterruptCapture() { CaptureProgress = 0f; }
        public void CancelRaid() { ActiveRaid = null; _expected = _defeated = 0; CaptureProgress = 0f; }
        public int Infuse()
        {
            if (ActiveRaid != null || !_sites["hearthspire"].Owned) return 0;
            int n = 0;
            foreach (StrongholdDefinition d in _catalog.sites)
                if (!d.home && _sites[d.id].Owned && !_sites[d.id].EchoInfused) { _sites[d.id].EchoInfused = true; n++; }
            return n;
        }
        public StrongholdSave Capture()
        {
            StrongholdSave s = new StrongholdSave { TrackedId = TrackedId };
            foreach (StrongholdDefinition d in _catalog.sites) s.Sites.Add(Copy(_sites[d.id]));
            return s;
        }
        public void Restore(StrongholdSave save)
        {
            Reset();
            if (save == null || save.Version != 1 || save.Sites == null) return;
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            foreach (StrongholdSiteSave s in save.Sites)
            {
                if (s == null || s.Id == null || !used.Add(s.Id) || Definition(s.Id) == null) continue;
                StrongholdSiteSave dest = _sites[s.Id]; dest.Owned = s.Owned;
                dest.Owner = dest.Owned ? "local_malik" : (s.Id == "hearthspire" ? "unclaimed" : "npc_" + s.Id);
                dest.EchoInfused = dest.Owned && s.Id != "hearthspire" && s.EchoInfused;
                dest.Theme = Math.Max(0, Math.Min(2,s.Theme));
                if (s.Slots == null) continue;
                for (int i = 0; i < Math.Min(SlotCount,s.Slots.Count); i++)
                {
                    FortSlotSave slot = s.Slots[i]; if (slot == null) continue;
                    dest.Slots[i] = new FortSlotSave { Module = Math.Max(0,Math.Min(4,slot.Module)), Rotation = Math.Max(0,Math.Min(3,slot.Rotation)) };
                }
            }
            if (!_sites["hearthspire"].Owned) foreach (StrongholdSiteSave s in _sites.Values) if (s.Id != "hearthspire") { s.Owned = false; s.EchoInfused = false; s.Owner = "npc_" + s.Id; }
            Track(save.TrackedId);
        }
        private static StrongholdSiteSave Copy(StrongholdSiteSave s)
        {
            StrongholdSiteSave c = new StrongholdSiteSave { Id = s.Id, Owner = s.Owner, Owned = s.Owned, EchoInfused = s.EchoInfused, Theme = s.Theme };
            foreach (FortSlotSave slot in s.Slots) c.Slots.Add(new FortSlotSave { Module = slot.Module, Rotation = slot.Rotation });
            return c;
        }
        private static int BitCount(int n) { int c = 0; while (n != 0) { c += n & 1; n >>= 1; } return c; }
    }
}
