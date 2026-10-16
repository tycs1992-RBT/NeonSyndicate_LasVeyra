using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Optional buildings are selected at city boot. Live floors are never removed by a settings toggle.</summary>
    public static class InhabitedCitySettings04C
    {
        public static bool Enabled { get { return PlayerPrefs.GetInt("NSLV_04C_Enabled", 1) != 0; } set { PlayerPrefs.SetInt("NSLV_04C_Enabled", value ? 1 : 0); } }
        public static bool Residents { get { return PlayerPrefs.GetInt("NSLV_04C_Residents", 1) != 0; } set { PlayerPrefs.SetInt("NSLV_04C_Residents", value ? 1 : 0); } }
        public static bool Ambience { get { return PlayerPrefs.GetInt("NSLV_04C_Ambience", 1) != 0; } set { PlayerPrefs.SetInt("NSLV_04C_Ambience", value ? 1 : 0); } }
        public static void ResetStatics() { }
    }
}
