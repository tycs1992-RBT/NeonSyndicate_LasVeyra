using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Engine-free ledger and explicit capture state. No transient fight or channel is restored from a save.</summary>
    public sealed class TerritoryBook
    {
        public const float ChannelSeconds = 6f;
        public const float RestSeconds = 180f;
        private readonly Dictionary<string, TerritorySiteSave> _sites = new Dictionary<string, TerritorySiteSave>(StringComparer.Ordinal);
        private readonly HashSet<string> _claims = new HashSet<string>(StringComparer.Ordinal);
        private readonly TerritoryCatalog _catalog;
        private bool[] _defeated;
        private bool _armed;
        public string ActiveId { get; private set; }
        public string TrackedId { get; private set; }
        public float Channel { get; private set; }
        public bool ChannelArmed { get { return _armed; } }
        public int OwnedCount { get { int count = 0; foreach (TerritorySiteSave s in _sites.Values) if (s.Owned) count++; return count; } }
        public int Remaining { get { int n = 0; if (_defeated != null) foreach (bool dead in _defeated) if (!dead) n++; return n; } }
        public TerritoryBook(TerritoryCatalog catalog) { catalog.Validate(); _catalog = catalog; Restore(null); }
        public TerritorySiteSave Status(string id)
        {
            TerritorySiteSave state;
            return id != null && _sites.TryGetValue(id, out state) ? Copy(state) : null;
        }
        public bool Discover(string id)
        {
            TerritorySiteSave state;
            if (!_sites.TryGetValue(id ?? "", out state) || state.Discovered) return false;
            state.Discovered = true; return true;
        }
        public bool Track(string id)
        {
            if (!_sites.ContainsKey(id ?? "")) return false;
            TrackedId = id; return true;
        }
        public bool Begin(string id, int defenders)
        {
            TerritorySiteSave state;
            if (ActiveId != null || !_sites.TryGetValue(id ?? "", out state) || state.Owned || defenders < 1 || defenders > 3) return false;
            ActiveId = id; _defeated = new bool[defenders]; state.Discovered = true; _armed = false; Channel = 0f; return true;
        }
        /// <summary>Only the runtime's subscribed, live defender death callback may supply an index.</summary>
        public bool Defeat(string id, int index)
        {
            if (ActiveId != id || _defeated == null || index < 0 || index >= _defeated.Length || _defeated[index]) return false;
            _defeated[index] = true; return true;
        }
        public bool Arm(string id)
        {
            if (ActiveId == null || ActiveId != id || Remaining != 0) return false;
            _armed = true; Channel = 0f; return true;
        }
        public bool Advance(float dt, bool safe, bool inRange, bool paused)
        {
            if (paused || !Finite(dt) || dt <= 0f || ActiveId == null || !_armed || Remaining != 0) return false;
            if (!safe || !inRange) { _armed = false; Channel = 0f; return false; }
            Channel += Math.Min(dt, .25f);
            if (Channel < ChannelSeconds) return false;
            TerritorySiteSave state = _sites[ActiveId]; state.Owned = state.Discovered = true;
            Cancel(); return true;
        }
        public void Cancel() { ActiveId = null; _defeated = null; _armed = false; Channel = 0f; }
        public bool ClaimCapture(string id)
        {
            TerritorySiteSave state;
            if (!_sites.TryGetValue(id ?? "", out state) || !state.Owned || state.RewardClaimed) return false;
            state.RewardClaimed = true; return true;
        }
        public bool Claimed(string key) { return key != null && _claims.Contains(key); }
        public bool Claim(string key)
        {
            if (!ValidClaim(key) || _claims.Count >= 64) return false;
            return _claims.Add(key);
        }
        public bool Improve(string id)
        {
            TerritorySiteSave state;
            if (ActiveId != null || !_sites.TryGetValue(id ?? "", out state) || !state.Owned || state.Fortification >= 2) return false;
            state.Fortification++; return true;
        }
        public bool Rest(string id)
        {
            TerritorySiteSave state;
            if (ActiveId != null || !_sites.TryGetValue(id ?? "", out state) || !state.Owned || state.Fortification == 0 || state.RestCooldown > 0f) return false;
            state.RestCooldown = RestSeconds; return true;
        }
        public void Tick(float dt)
        {
            if (!Finite(dt) || dt <= 0f) return;
            foreach (TerritorySiteSave s in _sites.Values) s.RestCooldown = Math.Max(0f, s.RestCooldown - Math.Min(dt, 1f));
        }
        public TerritorySave Capture()
        {
            TerritorySave result = new TerritorySave { TrackedId = TrackedId };
            foreach (TerritoryDefinition d in _catalog.sites) result.Sites.Add(Copy(_sites[d.id]));
            result.Claims.AddRange(_claims); result.Claims.Sort(StringComparer.Ordinal); return result;
        }
        public void Restore(TerritorySave save)
        {
            Cancel(); _sites.Clear(); _claims.Clear(); TrackedId = "";
            foreach (TerritoryDefinition d in _catalog.sites) _sites.Add(d.id, new TerritorySiteSave { Id = d.id });
            if (save == null) return;
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            if (save.Sites != null) foreach (TerritorySiteSave value in save.Sites)
            {
                if (value == null || value.Id == null || !_sites.ContainsKey(value.Id) || !used.Add(value.Id)) continue;
                TerritorySiteSave state = Copy(value);
                state.Fortification = state.Owned ? Math.Max(0, Math.Min(2, state.Fortification)) : 0;
                state.Discovered |= state.Owned;
                state.RestCooldown = state.Owned && Finite(state.RestCooldown) ? Math.Max(0f, Math.Min(RestSeconds, state.RestCooldown)) : 0f;
                // Preserve reward-consumed bits even on malformed ownership, so corruption cannot regenerate rewards.
                _sites[state.Id] = state;
            }
            if (save.Claims != null) foreach (string key in save.Claims) if (_claims.Count < 64 && ValidClaim(key)) _claims.Add(key);
            if (_sites.ContainsKey(save.TrackedId ?? "")) TrackedId = save.TrackedId;
        }
        private static bool ValidClaim(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 100) return false;
            int colon = key.IndexOf(':');
            if (colon < 0 || !TerritoryCatalog.SafeId(key.Substring(colon + 1))) return false;
            string kind = key.Substring(0, colon); return kind == "care" || kind == "event" || kind == "resource";
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static TerritorySiteSave Copy(TerritorySiteSave s)
        { return new TerritorySiteSave { Id = s.Id, Owned = s.Owned, Discovered = s.Discovered, RewardClaimed = s.RewardClaimed, Fortification = s.Fortification, RestCooldown = s.RestCooldown }; }
    }
}
