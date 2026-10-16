using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Only finite local activity/discovery facts live here. QuestBook owns chapter completion and rewards.</summary>
    public sealed class InhabitedCityBook
    {
        private readonly HashSet<string> _known;
        private readonly HashSet<string> _visited = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _actions = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _records = new HashSet<string>(StringComparer.Ordinal);
        public string ResumeVenue { get; private set; } = "";
        public bool IntroSeen { get; set; }
        public int VisitedCount { get { return _visited.Count; } }
        public InhabitedCityBook(IEnumerable<string> ids) { _known = new HashSet<string>(ids, StringComparer.Ordinal); }
        public bool Visit(string id) { return id != null && _known.Contains(id) && _visited.Add(id); }
        public bool Visited(string id) { return id != null && _visited.Contains(id); }
        public bool HasAction(string key) { return key != null && _actions.Contains(key); }
        public bool HasReadRecord(string id) { return id != null && _records.Contains(id); }
        public bool ReadRecord(string id)
        {
            return (id == "record_origins" || id == "record_route" || id == "record_frequency") && _records.Add(id);
        }
        public bool CompleteAction(string key)
        {
            return ValidKey(key) && _actions.Count < 64 && _actions.Add(key);
        }
        private bool ValidKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 100) return false;
            int colon = key.IndexOf(':');
            return colon > 0 && _known.Contains(key.Substring(0, colon)) &&
                QuietSignal04C.ValidAction(key.Substring(0, colon), key.Substring(colon + 1));
        }
        public void SetResume(string id) { ResumeVenue = id != null && _known.Contains(id) ? id : ""; }
        public InhabitedCitySave Capture()
        {
            InhabitedCitySave save = new InhabitedCitySave { ResumeVenue = ResumeVenue, IntroSeen = IntroSeen };
            save.Visited.AddRange(_visited); save.Visited.Sort(StringComparer.Ordinal);
            save.Actions.AddRange(_actions); save.Actions.Sort(StringComparer.Ordinal);
            save.ReadRecords.AddRange(_records); save.ReadRecords.Sort(StringComparer.Ordinal);
            return save;
        }
        public void Restore(InhabitedCitySave save)
        {
            _visited.Clear(); _actions.Clear(); _records.Clear(); ResumeVenue = ""; IntroSeen = false;
            if (save == null) return;
            if (save.Visited != null) foreach (string id in save.Visited) Visit(id);
            if (save.Actions != null) foreach (string key in save.Actions) CompleteAction(key);
            if (save.ReadRecords != null) foreach (string id in save.ReadRecords) ReadRecord(id);
            SetResume(save.ResumeVenue); IntroSeen = save.IntroSeen;
        }
    }
}
