using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The heat book of the crystal foci (engine-free, tested offline). Heat belongs to a focus by its stable inventory
    /// uid, never to the slot or to the weapon controller, so swapping slots or weapons can never reset an overheat.
    /// Every focus cools on one gameplay clock (Tick receives the scaled frame time, zero while paused): passive cooling
    /// after a short quiet delay, a forced vent after an overheat, a shorter manual vent on reload, and a Resonance vent
    /// at most once per cooldown per uid so a beam can never burst, vent and burst again on the same contact.
    /// </summary>
    public sealed class CrystalHeat
    {
        /// <summary>The heat state of one focus.</summary>
        public sealed class Entry
        {
            public int Uid;
            /// <summary>Normalized heat 0 to 1 (1 overheats it).</summary>
            public float Heat;
            /// <summary>True while a forced or manual vent runs (the focus cannot fire).</summary>
            public bool Venting;
            /// <summary>True when the vent was forced by an overheat (false for a manual vent).</summary>
            public bool Forced;
            public float VentLeft;
            public float VentTotal = 1f;
            /// <summary>Heat when the vent began (it drains from here to zero over VentTotal).</summary>
            public float VentStartHeat = 1f;
            /// <summary>Clock time of the last heat added (passive cooling waits CoolDelay after it).</summary>
            public float LastHeatAt = -999f;
            /// <summary>Clock time of the last Resonance vent (at most one per ResonanceCooldown).</summary>
            public float LastResonanceVentAt = -999f;
        }

        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        private readonly List<int> _drop = new List<int>();

        /// <summary>Heat drained per second once the focus has been quiet for CoolDelay seconds.</summary>
        public float PassiveCooling = CrystalFocusDb.PassiveCooling;
        public float CoolDelay = CrystalFocusDb.CoolDelay;
        public float ResonanceCooldown = CrystalFocusDb.ResonanceVentCooldown;
        /// <summary>A manual vent needs at least this much heat (pressing reload on a cool focus does nothing).</summary>
        public const float ManualVentMinimum = 0.05f;

        /// <summary>The gameplay clock in seconds (only Tick advances it, so pause and hitches are explicit).</summary>
        public float Now { get; private set; }

        public int Count { get { return _entries.Count; } }

        /// <summary>Advances the clock and cools every focus (the one cooling loop). Bad or negative frame times count as zero.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            dt = Mathf.Min(dt, 1f);
            Now += dt;
            _drop.Clear();
            foreach (KeyValuePair<int, Entry> kv in _entries)
            {
                Entry e = kv.Value;
                if (e.Venting)
                {
                    e.VentLeft -= dt;
                    e.Heat = Mathf.Clamp01(e.VentStartHeat * e.VentLeft / Mathf.Max(0.01f, e.VentTotal));
                    if (e.VentLeft <= 0f)
                    {
                        e.Venting = false;
                        e.Forced = false;
                        e.VentLeft = 0f;
                        e.Heat = 0f;
                    }
                }
                else if (e.Heat > 0f && Now - e.LastHeatAt >= CoolDelay)
                {
                    e.Heat = Mathf.Max(0f, e.Heat - PassiveCooling * dt);
                }
                // A cool, idle entry is forgotten unless its Resonance vent cooldown is still running.
                if (!e.Venting && e.Heat <= 0f && Now - e.LastResonanceVentAt >= ResonanceCooldown) _drop.Add(kv.Key);
            }
            for (int i = 0; i < _drop.Count; i++) _entries.Remove(_drop[i]);
            _drop.Clear();
        }

        private Entry Get(int uid, bool create)
        {
            Entry e;
            if (_entries.TryGetValue(uid, out e)) return e;
            if (!create) return null;
            e = new Entry { Uid = uid };
            _entries[uid] = e;
            return e;
        }

        public float HeatOf(int uid)
        {
            Entry e = Get(uid, false);
            return e != null ? e.Heat : 0f;
        }

        public bool IsVenting(int uid)
        {
            Entry e = Get(uid, false);
            return e != null && e.Venting;
        }

        /// <summary>H8S: seconds left of the vent running on this focus (0 when none), for the HUD countdown.</summary>
        public float VentLeftOf(int uid)
        {
            Entry e = Get(uid, false);
            return e != null && e.Venting ? Mathf.Max(0f, e.VentLeft) : 0f;
        }

        /// <summary>True when the focus may fire now (not venting).</summary>
        public bool CanFire(int uid)
        {
            return !IsVenting(uid);
        }

        /// <summary>
        /// Adds heat for one shot event (or one beam tick). Returns true when this addition overheated the focus, which
        /// starts a forced vent of ventSeconds. Adding to a venting focus does nothing (it cannot fire).
        /// </summary>
        public bool Add(int uid, float amount, float ventSeconds)
        {
            if (!(amount > 0f)) return false;
            Entry e = Get(uid, true);
            if (e.Venting) return false;
            e.LastHeatAt = Now;
            e.Heat += amount;
            if (e.Heat < 1f) return false;
            e.Heat = 1f;
            e.Venting = true;
            e.Forced = true;
            e.VentTotal = Mathf.Max(0.3f, ventSeconds);
            e.VentLeft = e.VentTotal;
            e.VentStartHeat = 1f;
            return true;
        }

        /// <summary>
        /// Reload pressed: vents early. The vent is shorter the cooler the focus (ventSeconds scaled by the heat, at least
        /// 40 percent). Returns false when there is nothing to vent or a vent already runs.
        /// </summary>
        public bool StartManualVent(int uid, float ventSeconds)
        {
            Entry e = Get(uid, false);
            if (e == null || e.Venting || e.Heat <= ManualVentMinimum) return false;
            e.Venting = true;
            e.Forced = false;
            e.VentTotal = Mathf.Max(0.3f, ventSeconds * Mathf.Clamp(e.Heat, 0.4f, 1f));
            e.VentLeft = e.VentTotal;
            e.VentStartHeat = Mathf.Clamp01(e.Heat);
            return true;
        }

        /// <summary>
        /// A Resonance Burst's reward: sheds all heat and ends any vent, at most once per ResonanceCooldown per uid.
        /// Returns false (and changes nothing) when the focus is already cool or the cooldown still runs.
        /// </summary>
        public bool ResonanceVent(int uid)
        {
            Entry e = Get(uid, false);
            if (e == null) return false;
            if (!e.Venting && e.Heat <= 0.01f) return false;
            if (Now - e.LastResonanceVentAt < ResonanceCooldown) return false;
            e.LastResonanceVentAt = Now;
            e.Heat = 0f;
            e.Venting = false;
            e.Forced = false;
            e.VentLeft = 0f;
            return true;
        }

        /// <summary>Seconds until the next Resonance vent is allowed for this focus (0 when allowed).</summary>
        public float ResonanceCooldownLeft(int uid)
        {
            Entry e = Get(uid, false);
            return e == null ? 0f : Mathf.Max(0f, ResonanceCooldown - (Now - e.LastResonanceVentAt));
        }

        /// <summary>Forgets every focus's heat (new game, load, title). The clock keeps running.</summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>Copies every live entry (debug page).</summary>
        public void CopyEntries(List<Entry> into)
        {
            if (into == null) return;
            into.Clear();
            foreach (KeyValuePair<int, Entry> kv in _entries) into.Add(kv.Value);
        }
    }
}
