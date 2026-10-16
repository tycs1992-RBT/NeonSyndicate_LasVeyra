using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>A modifier on a hit about to land on the player. Return false to cancel the hit entirely.</summary>
    public delegate bool PlayerDamageModifier(ref DamageInfo info);

    /// <summary>
    /// Ordered modifiers that run inside the player's damage filter after dodge, guard and parry and before gear and skill
    /// resistances (04E: the ward's directional capacity and aspects, the Ashshell Warden's brace). One shared path, so a
    /// hit is never absorbed twice. A modifier that throws is logged and removed.
    /// </summary>
    public static class PlayerDamageHooks
    {
        private sealed class Entry
        {
            public string Key;
            public int Order;
            public PlayerDamageModifier Modify;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        public static void ResetStatics() { _entries.Clear(); }

        public static void Register(string key, int order, PlayerDamageModifier modify)
        {
            if (string.IsNullOrEmpty(key) || modify == null) return;
            _entries.RemoveAll(e => e.Key == key);
            _entries.Add(new Entry { Key = key, Order = order, Modify = modify });
            _entries.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public static void Unregister(string key) { _entries.RemoveAll(e => e.Key == key); }

        public static int Count { get { return _entries.Count; } }

        /// <summary>Runs every modifier in order. False as soon as one cancels the hit.</summary>
        public static bool Apply(ref DamageInfo info)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                bool keep;
                try { keep = e.Modify(ref info); }
                catch (Exception ex)
                {
                    Log.Exception("Player damage hook " + e.Key, ex);
                    _entries.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!keep) return false;
            }
            return true;
        }
    }
}
