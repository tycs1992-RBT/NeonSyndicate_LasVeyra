using UnityEngine;

namespace NeonSyndicate
{
    public enum GraphicsPreset
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>How hard fights are: enemy damage, accuracy, how many attack at once, and healing.</summary>
    public enum DifficultyLevel
    {
        Story = 0,
        Balanced = 1,
        Veteran = 2
    }

    /// <summary>Player settings persisted with PlayerPrefs.</summary>
    public static class Settings
    {
        public static float MouseSensitivity = 2.0f;
        public static bool InvertY;
        public static float FieldOfView = 55f;
        public static float MasterVolume = 0.85f;
        public static float MusicVolume = 0.55f;
        public static float SfxVolume = 0.9f;
        public static GraphicsPreset Graphics = GraphicsPreset.High;
        public static float TrafficDensity = 1f;
        public static float PedDensity = 1f;
        public static bool CameraShake = true;
        public static bool Subtitles = true;
        public static bool ShowFps;
        public static bool ShowSpeedometer = true;
        public static bool Bloom = true;
        public static float UiScale = 1f;
        public static DifficultyLevel Difficulty = DifficultyLevel.Story;
        public static bool Vibration = true;
        public static float PadLookSpeed = 210f;
        public static bool PadInvertY;
        public static bool AimAssist = true;
        public static bool FilmGrain = true;
        public static bool AmbientOcclusion = true;
        public static bool MotionBlur = true;
        public static bool AutoExposure = true;
        /// <summary>Picture brightness in stops (-1 darker, +1 brighter), applied before tone mapping.</summary>
        public static float Brightness;
        /// <summary>Floating damage numbers over enemies the player hits (DamageNumbers).</summary>
        public static bool DamageNumbers = true;

        private const string Prefix = "NSLV_";

        public static void Load()
        {
            MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "sens", 2.0f);
            InvertY = PlayerPrefs.GetInt(Prefix + "invy", 0) == 1;
            FieldOfView = PlayerPrefs.GetFloat(Prefix + "fov", 55f);
            if (Mathf.Approximately(FieldOfView, 62f)) FieldOfView = 55f; // old default -> new tighter framing
            MasterVolume = PlayerPrefs.GetFloat(Prefix + "vol", 0.85f);
            MusicVolume = PlayerPrefs.GetFloat(Prefix + "music", 0.55f);
            SfxVolume = PlayerPrefs.GetFloat(Prefix + "sfx", 0.9f);
            Graphics = (GraphicsPreset)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "gfx", (int)GraphicsPreset.High), 0, 3);
            TrafficDensity = PlayerPrefs.GetFloat(Prefix + "traffic", 1f);
            PedDensity = PlayerPrefs.GetFloat(Prefix + "peds", 1f);
            CameraShake = PlayerPrefs.GetInt(Prefix + "shake", 1) == 1;
            Subtitles = PlayerPrefs.GetInt(Prefix + "subs", 1) == 1;
            ShowFps = PlayerPrefs.GetInt(Prefix + "fps", 0) == 1;
            ShowSpeedometer = PlayerPrefs.GetInt(Prefix + "speedo", 1) == 1;
            Bloom = PlayerPrefs.GetInt(Prefix + "bloom", 1) == 1;
            UiScale = PlayerPrefs.GetFloat(Prefix + "ui", 1f);
            // Existing choices are kept. New settings start with the welcoming exploration balance.
            Difficulty = (DifficultyLevel)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "diff", (int)DifficultyLevel.Story), 0, 2);
            Vibration = PlayerPrefs.GetInt(Prefix + "rumble", 1) == 1;
            PadLookSpeed = PlayerPrefs.GetFloat(Prefix + "padlook", 210f);
            PadInvertY = PlayerPrefs.GetInt(Prefix + "padinvy", 0) == 1;
            AimAssist = PlayerPrefs.GetInt(Prefix + "assist", 1) == 1;
            FilmGrain = PlayerPrefs.GetInt(Prefix + "grain", 1) == 1;
            AmbientOcclusion = PlayerPrefs.GetInt(Prefix + "ao", 1) == 1;
            MotionBlur = PlayerPrefs.GetInt(Prefix + "mblur", 1) == 1;
            AutoExposure = PlayerPrefs.GetInt(Prefix + "autoexp", 1) == 1;
            Brightness = PlayerPrefs.GetFloat(Prefix + "bright", 0f);
            DamageNumbers = PlayerPrefs.GetInt(Prefix + "dmgnum", 1) == 1;
            Clamp();
        }

        public static void Save()
        {
            Clamp();
            PlayerPrefs.SetFloat(Prefix + "sens", MouseSensitivity);
            PlayerPrefs.SetInt(Prefix + "invy", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "fov", FieldOfView);
            PlayerPrefs.SetFloat(Prefix + "vol", MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "music", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "sfx", SfxVolume);
            PlayerPrefs.SetInt(Prefix + "gfx", (int)Graphics);
            PlayerPrefs.SetFloat(Prefix + "traffic", TrafficDensity);
            PlayerPrefs.SetFloat(Prefix + "peds", PedDensity);
            PlayerPrefs.SetInt(Prefix + "shake", CameraShake ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "subs", Subtitles ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fps", ShowFps ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "speedo", ShowSpeedometer ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "bloom", Bloom ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "ui", UiScale);
            PlayerPrefs.SetInt(Prefix + "diff", (int)Difficulty);
            PlayerPrefs.SetInt(Prefix + "rumble", Vibration ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "padlook", PadLookSpeed);
            PlayerPrefs.SetInt(Prefix + "padinvy", PadInvertY ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "assist", AimAssist ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "grain", FilmGrain ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ao", AmbientOcclusion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "mblur", MotionBlur ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "autoexp", AutoExposure ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "bright", Brightness);
            PlayerPrefs.SetInt(Prefix + "dmgnum", DamageNumbers ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void Clamp()
        {
            MouseSensitivity = Mathf.Clamp(MouseSensitivity, 0.2f, 8f);
            FieldOfView = Mathf.Clamp(FieldOfView, 45f, 90f);
            MasterVolume = Mathf.Clamp01(MasterVolume);
            Brightness = Mathf.Clamp(Brightness, -1f, 1f);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            TrafficDensity = Mathf.Clamp(TrafficDensity, 0.25f, 1.6f);
            PedDensity = Mathf.Clamp(PedDensity, 0.25f, 1.6f);
            UiScale = Mathf.Clamp(UiScale, 0.75f, 1.5f);
            PadLookSpeed = Mathf.Clamp(PadLookSpeed, 60f, 480f);
        }

        public static string DifficultyName(DifficultyLevel d)
        {
            switch (d)
            {
                case DifficultyLevel.Story: return "Story";
                case DifficultyLevel.Veteran: return "Veteran";
                default: return "Balanced";
            }
        }

        /// <summary>Applies the graphics preset to Unity quality settings (and, on URP, to the active URP asset).</summary>
        public static void ApplyGraphics()
        {
            switch (Graphics)
            {
                case GraphicsPreset.Low:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 45f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.lodBias = 0.7f;
                    break;
                case GraphicsPreset.Medium:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 80f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.lodBias = 1f;
                    break;
                case GraphicsPreset.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 130f;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.pixelLightCount = 8;
                    QualitySettings.lodBias = 1.5f;
                    break;
                default:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 190f;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.antiAliasing = 8;
                    QualitySettings.pixelLightCount = 12;
                    QualitySettings.lodBias = 2f;
                    break;
            }
            QualitySettings.shadowResolution = Graphics >= GraphicsPreset.High ? ShadowResolution.VeryHigh : ShadowResolution.Medium;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 144;
            // On URP the preset also drives the pipeline asset (no effect on the Built-in pipeline).
            RenderPath.OnGraphicsApplied();
        }

        /// <summary>View distance in meters for the current preset.</summary>
        public static float ViewDistance
        {
            get
            {
                switch (Graphics)
                {
                    case GraphicsPreset.Low: return 700f;
                    case GraphicsPreset.Medium: return 1000f;
                    case GraphicsPreset.High: return 1500f;
                    default: return 2200f;
                }
            }
        }

        public static int MaxRealLights
        {
            get
            {
                switch (Graphics)
                {
                    case GraphicsPreset.Low: return 4;
                    case GraphicsPreset.Medium: return 10;
                    case GraphicsPreset.High: return 18;
                    default: return 28;
                }
            }
        }
    }
}
