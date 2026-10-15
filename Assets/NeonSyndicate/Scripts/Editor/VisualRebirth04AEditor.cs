#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>Independent visual rollback switches. Gameplay settings and save data are untouched.</summary>
    public static class VisualRebirth04AEditor
    {
        private const string MenuRoot = "Neon Syndicate/Graphics/Visual Rebirth 04A/";
        [MenuItem(MenuRoot + "Enable overhaul", false, 1)] private static void All() { Toggle("Enabled", VisualRebirth04ASettings.Enabled); }
        [MenuItem(MenuRoot + "City architecture", false, 2)] private static void City() { Toggle("City", VisualRebirth04ASettings.City); }
        [MenuItem(MenuRoot + "Stronghold architecture", false, 3)] private static void Forts() { Toggle("Strongholds", VisualRebirth04ASettings.Strongholds); }
        [MenuItem(MenuRoot + "Gardens and vegetation", false, 4)] private static void Gardens() { Toggle("Gardens", VisualRebirth04ASettings.Gardens); }
        [MenuItem(MenuRoot + "Atmosphere and ambience", false, 5)] private static void Atmosphere() { Toggle("Atmosphere", VisualRebirth04ASettings.Atmosphere); }
        [MenuItem(MenuRoot + "Enable overhaul", true)] private static bool CheckAll() { Menu.SetChecked(MenuRoot + "Enable overhaul", VisualRebirth04ASettings.Enabled); return true; }
        [MenuItem(MenuRoot + "City architecture", true)] private static bool CheckCity() { Menu.SetChecked(MenuRoot + "City architecture", VisualRebirth04ASettings.City); return true; }
        [MenuItem(MenuRoot + "Stronghold architecture", true)] private static bool CheckForts() { Menu.SetChecked(MenuRoot + "Stronghold architecture", VisualRebirth04ASettings.Strongholds); return true; }
        [MenuItem(MenuRoot + "Gardens and vegetation", true)] private static bool CheckGardens() { Menu.SetChecked(MenuRoot + "Gardens and vegetation", VisualRebirth04ASettings.Gardens); return true; }
        [MenuItem(MenuRoot + "Atmosphere and ambience", true)] private static bool CheckAtmosphere() { Menu.SetChecked(MenuRoot + "Atmosphere and ambience", VisualRebirth04ASettings.Atmosphere); return true; }
        private static void Toggle(string feature, bool current)
        {
            VisualRebirth04ASettings.Set(feature, !current);
            Log.Info("Visual Rebirth 04A / " + feature + " = " + !current + ". Stop and restart Play Mode to rebuild geometry. Saved games are unchanged.");
        }
    }
}
#endif
