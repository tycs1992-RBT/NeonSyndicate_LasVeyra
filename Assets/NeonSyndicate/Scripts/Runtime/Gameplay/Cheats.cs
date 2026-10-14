using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Classic typed cheat codes (type the word while playing) and the actions behind the F2 debug menu.
    /// Codes never contain M or P: those keys open the map and the phone, which would swallow the rest of the code.
    /// The full list is in Docs/CHEATS.md.
    /// </summary>
    public sealed class Cheats : MonoBehaviour
    {
        private readonly StringBuilder _typed = new StringBuilder();
        private bool _slowmo;

        public static readonly string[] Codes =
        {
            "TOUGHSKIN", "CASHFLOW", "ARSENAL", "CLEANSLATE", "HEATWAVE", "DETONATE", "SLOWDOWN", "NIGHTOWL", "SUNRISE",
            "THUNDER", "CLEARSKY", "LOWGRAVITY", "THIRDEYE", "UNDYING", "LAWLESS", "VEKTOR", "FALCON", "SQUADCAR",
            "BIGRIG", "TANKY", "TAXIRIDE", "BUSRIDE", "DUSTDEVIL"
        };

        private void Update()
        {
            if (!Game.IsPlaying || GInput.Broken) return;
            if (Game.Phone != null && Game.Phone.IsOpen) return;
            string s = GInput.TypedText;
            if (string.IsNullOrEmpty(s)) return;
            foreach (char ch in s)
            {
                if (!char.IsLetter(ch)) continue;
                _typed.Append(char.ToUpperInvariant(ch));
            }
            if (_typed.Length > 24) _typed.Remove(0, _typed.Length - 24);
            string buf = _typed.ToString();
            foreach (string code in Codes)
            {
                if (buf.EndsWith(code))
                {
                    _typed.Length = 0;
                    Run(code);
                    break;
                }
            }
        }

        public void Run(string code)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            string msg = "Cheat activated";
            switch (code)
            {
                case "TOUGHSKIN":
                    p.Health.Revive(p.Health.MaxHealth);
                    p.Health.AddArmor(p.Health.MaxArmor);
                    msg = "Health and armor restored";
                    break;
                case "CASHFLOW":
                    Game.Economy.Earn(250000, "Cheat");
                    msg = "+$250,000";
                    break;
                case "ARSENAL":
                    p.Weapons.Inventory.GiveAll();
                    p.Weapons.RefreshModel();
                    msg = "Full arsenal";
                    break;
                case "CLEANSLATE":
                    if (Game.Wanted != null) Game.Wanted.ClearAll();
                    msg = "Wanted level cleared";
                    break;
                case "HEATWAVE":
                    if (Game.Wanted != null) Game.Wanted.SetStars(Mathf.Min(5, Game.Wanted.Stars + 2));
                    msg = "Wanted level raised";
                    break;
                case "DETONATE":
                    foreach (Vehicle v in Vehicle.All.ToArray())
                        if (v != null && !v.HasPlayer && !v.MissionCritical && MathUtil.FlatDistance(v.transform.position, p.transform.position) < 70f) v.Explode(null);
                    msg = "Boom town";
                    break;
                case "SLOWDOWN":
                    _slowmo = !GameTime.Has("cheat");
                    if (_slowmo) GameTime.Request("cheat", 0.5f); else GameTime.Release("cheat");
                    msg = _slowmo ? "Slow motion on" : "Slow motion off";
                    break;
                case "NIGHTOWL":
                    if (Game.Atmo != null) Game.Atmo.SetTime(23f);
                    msg = "Midnight in Las Veyra";
                    break;
                case "SUNRISE":
                    if (Game.Atmo != null) Game.Atmo.SetTime(6f);
                    msg = "Sunrise";
                    break;
                case "THUNDER":
                    if (Game.Atmo != null) Game.Atmo.SetWeather(Weather.Storm, false);
                    msg = "Storm rolling in";
                    break;
                case "DUSTDEVIL":
                    if (Game.Atmo != null) Game.Atmo.SetWeather(Weather.Sandstorm, false);
                    msg = "Sandstorm";
                    break;
                case "CLEARSKY":
                    if (Game.Atmo != null) Game.Atmo.SetWeather(Weather.Clear, false);
                    msg = "Clear skies";
                    break;
                case "LOWGRAVITY":
                    PlayerController.MoonJump = !PlayerController.MoonJump;
                    msg = PlayerController.MoonJump ? "Moon jump on" : "Moon jump off";
                    break;
                case "THIRDEYE":
                    if (Game.Abilities != null)
                    {
                        Game.Abilities.ThirdEyeUnlocked = true;
                        for (int i = 0; i < Abilities.Spells.Length; i++) Game.Abilities.Unlock(i);
                        Game.Abilities.AddMaxEnergy(100f);
                        Game.Abilities.Selected = Abilities.SunboltIndex;
                    }
                    msg = "All crystal powers unlocked";
                    break;
                case "UNDYING":
                    Game.Root.GodMode = !Game.Root.GodMode;
                    p.Health.Invulnerable = Game.Root.GodMode;
                    msg = Game.Root.GodMode ? "God mode on" : "God mode off";
                    break;
                case "LAWLESS":
                    if (Game.Wanted != null)
                    {
                        Game.Wanted.Suppressed = !Game.Wanted.Suppressed;
                        if (Game.Wanted.Suppressed) Game.Wanted.ClearAll();
                        msg = Game.Wanted.Suppressed ? "Police will ignore you" : "Police back on duty";
                    }
                    break;
                case "VEKTOR": SpawnVehicle(VehicleDb.Find("Bishop Vektor")); break;
                case "FALCON": SpawnVehicle(VehicleDb.Find("Torque Falcon")); break;
                case "SQUADCAR": SpawnVehicle(VehicleDb.Find("Metro Response Cruiser")); break;
                case "BIGRIG": SpawnVehicle(VehicleDb.Find("Vanta Sentinel Box Truck")); break;
                case "TANKY": SpawnVehicle(VehicleDb.Find("Anvil Sentinel Armored Truck")); break;
                case "TAXIRIDE": SpawnVehicle(VehicleDb.Find("Vanta Metro Cab")); break;
                case "BUSRIDE": SpawnVehicle(VehicleDb.Find("Metro Transit Coach")); break;
            }
            if (Game.Hud != null) Game.Hud.Notify(msg + " (" + code + ")", 2.5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.6f);
            if (Game.Stats != null) Game.Stats.Add(StatId.TimePlayed, 0f);
        }

        /// <summary>Spawns a vehicle a few meters in front of the player (on the nearest road if possible).</summary>
        public Vehicle SpawnVehicle(VehicleDef def)
        {
            PlayerController p = Game.Player;
            if (def == null || p == null) return null;
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            Vector3 pos = p.transform.position + fwd * (def.Length * 0.5f + 4f);
            if (p.InVehicle) pos = p.CurrentVehicle.transform.position + p.CurrentVehicle.transform.right * (def.Width + 3f);
            pos = TrafficManager.GroundAt(pos);
            Vehicle v = Game.Traffic != null ? Game.Traffic.SpawnParkedAt(def, pos, MathUtil.YawOf(fwd) + 90f, null) : Vehicle.Spawn(def, pos, 0f, null, true);
            v.Parked = false;
            v.Locked = false;
            // A delivered car is already yours: no theft stat, and a delivered cruiser brings no stars.
            v.StolenByPlayer = true;
            if (Game.Hud != null) Game.Hud.Notify(def.Name + " delivered.", 2f);
            return v;
        }
    }
}
