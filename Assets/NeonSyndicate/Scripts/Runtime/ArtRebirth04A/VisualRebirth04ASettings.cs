using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Independent optional art switches. No save-game fields or gameplay options are changed.</summary>
    public static class VisualRebirth04ASettings
    {
        public const string Version = "Visual Rebirth 04A";
        private static readonly Dictionary<string, bool> Flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        public static bool Enabled { get { return Read("Enabled"); } }
        public static bool City { get { return Read("City"); } }
        public static bool Gardens { get { return Read("Gardens"); } }
        public static bool Strongholds { get { return Read("Strongholds"); } }
        public static bool Atmosphere { get { return Read("Atmosphere"); } }

        private static bool Read(string feature)
        {
            bool value;
            if (!Flags.TryGetValue(feature, out value))
            {
                value = PlayerPrefs.GetInt("NSLV_04A" + feature, 1) != 0;
                Flags.Add(feature, value);
            }
            return value;
        }

        /// <summary>Geometry switches take effect on the next world generation. Atmosphere changes immediately.</summary>
        public static void Set(string feature, bool enabled)
        {
            if (feature != "Enabled" && feature != "City" && feature != "Gardens" &&
                feature != "Strongholds" && feature != "Atmosphere")
                throw new ArgumentException("Unknown 04A art feature: " + feature, "feature");
            Flags[feature] = enabled;
            PlayerPrefs.SetInt("NSLV_04A" + feature, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetStatics() { Flags.Clear(); }
    }
}
