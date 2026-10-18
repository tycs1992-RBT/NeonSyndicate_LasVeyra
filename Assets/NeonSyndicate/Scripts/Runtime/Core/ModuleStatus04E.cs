using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>What a module reported about itself at boot or later. Runtime registration, never a planned catalog.</summary>
    public enum ModuleState
    {
        NotStarted = 0,
        Loaded = 1,
        Partial = 2,
        Disabled = 3,
        Failed = 4
    }

    /// <summary>One row of the in-game build and loaded-content check.</summary>
    public sealed class ModuleRecord
    {
        public string Id = "";
        public string Title = "";
        public ModuleState State;
        /// <summary>Plain-language reason (why it failed, why it is partial or disabled), empty when loaded cleanly.</summary>
        public string Detail = "";
        /// <summary>The next safe thing the player can do when the module is not fully available.</summary>
        public string NextAction = "";
        /// <summary>Installed items this module placed at runtime (entrances, sites, quests loaded), -1 when not counted.</summary>
        public int Count = -1;
        public string CountLabel = "";
    }

    /// <summary>
    /// The runtime registry behind the STATUS page (Underheart and Bonds 04E). Boot steps report Loaded or Failed; systems
    /// add Partial or Disabled with a reason and count what they actually installed (entrances placed, quests loaded),
    /// so the page never shows a planned catalog entry as delivered. Engine-free.
    /// </summary>
    public static class ModuleStatus
    {
        /// <summary>The module label shown on the STATUS page and the session chip.</summary>
        public const string Label = "Crystal Combat and Living City 04F";

        private static readonly List<ModuleRecord> _all = new List<ModuleRecord>();

        public static IList<ModuleRecord> All { get { return _all; } }

        public static void ResetStatics()
        {
            _all.Clear();
        }

        public static ModuleRecord Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _all.Count; i++) if (_all[i].Id == id) return _all[i];
            return null;
        }

        private static ModuleRecord Ensure(string id, string title)
        {
            ModuleRecord r = Find(id);
            if (r == null)
            {
                r = new ModuleRecord { Id = id ?? "", Title = string.IsNullOrEmpty(title) ? (id ?? "") : title };
                _all.Add(r);
            }
            else if (!string.IsNullOrEmpty(title)) r.Title = title;
            return r;
        }

        /// <summary>Records a module's state. A later report replaces the earlier one (Failed is never downgraded to Loaded).</summary>
        public static void Report(string id, string title, ModuleState state, string detail, string nextAction = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            ModuleRecord r = Ensure(id, title);
            if (r.State == ModuleState.Failed && state == ModuleState.Loaded) return;
            r.State = state;
            r.Detail = detail ?? "";
            r.NextAction = nextAction ?? "";
        }

        /// <summary>Records how many items a module actually installed (for example "interior entrances", 4).</summary>
        public static void SetCount(string id, int count, string label)
        {
            if (string.IsNullOrEmpty(id)) return;
            ModuleRecord r = Ensure(id, null);
            r.Count = Math.Max(-1, count);
            r.CountLabel = label ?? "";
        }

        public static bool IsLoaded(string id)
        {
            ModuleRecord r = Find(id);
            return r != null && (r.State == ModuleState.Loaded || r.State == ModuleState.Partial);
        }
    }
}
