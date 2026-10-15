#if UNITY_EDITOR
using UnityEditor;

namespace NeonSyndicate.EditorTools
{
    /// <summary>Independent 04B switches. Changes apply on the next city load and leave 04A switches untouched.</summary>
    public static class LivingTerritories04BEditor
    {
        private const string Root = "Neon Syndicate/World/Living Territories 04B/";
        [MenuItem(Root + "Enable living territories", false, 1)] private static void All() { Toggle("Enabled", TerritorySettings04B.Enabled); }
        [MenuItem(Root + "Relay encounters", false, 2)] private static void Relays() { Toggle("Relays", TerritorySettings04B.Relays); }
        [MenuItem(Root + "Staff and creature ecology", false, 3)] private static void Ecology() { Toggle("Ecology", TerritorySettings04B.Ecology); }
        [MenuItem(Root + "Territory events", false, 4)] private static void Events() { Toggle("Events", TerritorySettings04B.Events); }
        [MenuItem(Root + "Territory ambience", false, 5)] private static void Ambience() { Toggle("Ambience", TerritorySettings04B.Ambience); }
        [MenuItem(Root + "Enable living territories", true)] private static bool AllCheck() { Menu.SetChecked(Root + "Enable living territories", TerritorySettings04B.Enabled); return true; }
        [MenuItem(Root + "Relay encounters", true)] private static bool RelaysCheck() { Menu.SetChecked(Root + "Relay encounters", TerritorySettings04B.Relays); return true; }
        [MenuItem(Root + "Staff and creature ecology", true)] private static bool EcologyCheck() { Menu.SetChecked(Root + "Staff and creature ecology", TerritorySettings04B.Ecology); return true; }
        [MenuItem(Root + "Territory events", true)] private static bool EventsCheck() { Menu.SetChecked(Root + "Territory events", TerritorySettings04B.Events); return true; }
        [MenuItem(Root + "Territory ambience", true)] private static bool AmbienceCheck() { Menu.SetChecked(Root + "Territory ambience", TerritorySettings04B.Ambience); return true; }
        private static void Toggle(string feature, bool current)
        {
            TerritorySettings04B.Set(feature, !current);
            Log.Info("Living Territories 04B / " + feature + " next city load = " + !current + ". Stop and restart Play Mode. Saved progress and 04A switches are retained.");
        }
    }
}
#endif
