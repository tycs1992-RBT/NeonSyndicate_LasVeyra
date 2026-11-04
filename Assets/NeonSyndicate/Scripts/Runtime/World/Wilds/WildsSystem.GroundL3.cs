using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class WildsSystem
    {
        private const string GroundPreferenceL3 = "NSLV_redspire_ground_l3";
        private bool _readGroundL3, _selectedGroundL3, _compareGroundL3, _suspendedGroundL3;
        public bool SelectedGroundL3
        {
            get { if (!_readGroundL3) { _readGroundL3 = true; _selectedGroundL3 = PlayerPrefs.GetInt(GroundPreferenceL3, 0) == 1; } return _selectedGroundL3; }
        }
        public void ToggleGroundL3()
        {
            _selectedGroundL3 = !SelectedGroundL3;
            PlayerPrefs.SetInt(GroundPreferenceL3, _selectedGroundL3 ? 1 : 0); PlayerPrefs.Save(); RefreshGroundL3();
        }
        public void CompareGroundL3(bool legacy) { _compareGroundL3 = legacy; RefreshGroundL3(); }
        public void RefreshGroundL3() { LiveGroundMaterialsL3.Select(SelectedGroundL3, _compareGroundL3 || _suspendedGroundL3, Game.Layout != null && Game.Layout.GothicCivicH5); }
        public void SuspendGroundL3(bool suspend)
        { if (_suspendedGroundL3 == suspend) return; _suspendedGroundL3 = suspend; RefreshGroundL3(); }
        public string GroundStatusL3 { get { return LiveGroundMaterialsL3.Status; } }
    }
}
