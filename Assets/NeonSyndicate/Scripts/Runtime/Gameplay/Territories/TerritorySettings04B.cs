using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Independent diagnostic switches, captured at city boot. Disabling a layer never erases its save ledger.</summary>
    public static class TerritorySettings04B
    {
        private static bool _loaded;
        public static bool Enabled { get { Load(); return _enabled; } }
        public static bool Relays { get { Load(); return _relays; } }
        public static bool Ecology { get { Load(); return _ecology; } }
        public static bool Events { get { Load(); return _events; } }
        public static bool Ambience { get { Load(); return _ambience; } }
        private static bool _enabled, _relays, _ecology, _events, _ambience;
        private static void Load()
        {
            if (_loaded) return; _loaded = true;
            _enabled = Read("Enabled"); _relays = Read("Relays"); _ecology = Read("Ecology"); _events = Read("Events"); _ambience = Read("Ambience");
        }
        private static bool Read(string feature) { return PlayerPrefs.GetInt("NSLV_04B_" + feature, 1) != 0; }
        public static void Set(string feature, bool enabled)
        {
            if (feature != "Enabled" && feature != "Relays" && feature != "Ecology" && feature != "Events" && feature != "Ambience") return;
            PlayerPrefs.SetInt("NSLV_04B_" + feature, enabled ? 1 : 0); PlayerPrefs.Save(); ResetStatics();
        }
        public static void ResetStatics() { _loaded = false; }
    }
}
