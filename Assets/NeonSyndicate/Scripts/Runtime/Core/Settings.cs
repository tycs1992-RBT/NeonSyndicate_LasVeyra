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
        public static float SubtitleScale04L = 1f;
        public static bool ShowFps;
        public static bool CrystalCompass04O = true;
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
        // Phase 1 lighting (Docs/VisualUpgrade/PHASE1_LIGHTING.md). Each is an off switch; the presets gate the cost.
        /// <summary>The sun's colour and strength from the atmosphere model (off: the hand-tuned warm and noon colours).</summary>
        public static bool PhysicalSun = true;
        /// <summary>The NeonSky dome (Medium and up; off: Unity's procedural skybox).</summary>
        public static bool ProceduralSky = true;
        /// <summary>Ambient light from the rendered sky dome and the ground under the camera (High and up; off: Trilight).</summary>
        public static bool SkyAmbient = true;
        /// <summary>Per-area realtime reflection probes (Medium and up).</summary>
        public static bool AreaReflections = true;
        /// <summary>Sun and moon light shafts in NeonPost (High and up).</summary>
        public static bool VolumetricLight = true;
        /// <summary>Height fog: denser air near the ground, a ground fog band at dawn and in the Frostline (High and up).</summary>
        public static bool HeightFog = true;
        /// <summary>Temporal anti-aliasing on High and Ultra (off: multisampling and a light sharpen as before).</summary>
        public static bool TemporalAA = true;
        /// <summary>Depth of field on the title flyover, cutscene cameras and the pause menu background (never in play).</summary>
        public static bool DepthOfField = true;
        /// <summary>Chromatic aberration: a barely visible lens fringe, plus the Veil and rift fringing. Off by default.</summary>
        public static bool ChromaticAberration;

        private const string Prefix = "NSLV_";

        public static void Load()
        {
            CrystalCompass04O = PlayerPrefs.GetInt(Prefix + "compass04o", 1) == 1;
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
            SubtitleScale04L = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "subtitle_size_04l", 1f), .9f, 1.4f);
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
            PhysicalSun = PlayerPrefs.GetInt(Prefix + "physun", 1) == 1;
            ProceduralSky = PlayerPrefs.GetInt(Prefix + "procsky", 1) == 1;
            SkyAmbient = PlayerPrefs.GetInt(Prefix + "skyamb", 1) == 1;
            AreaReflections = PlayerPrefs.GetInt(Prefix + "areaprobes", 1) == 1;
            VolumetricLight = PlayerPrefs.GetInt(Prefix + "shafts", 1) == 1;
            HeightFog = PlayerPrefs.GetInt(Prefix + "heightfog", 1) == 1;
            TemporalAA = PlayerPrefs.GetInt(Prefix + "taa", 1) == 1;
            DepthOfField = PlayerPrefs.GetInt(Prefix + "dof", 1) == 1;
            ChromaticAberration = PlayerPrefs.GetInt(Prefix + "chroma", 0) == 1;
            Clamp();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "compass04o", CrystalCompass04O ? 1 : 0);
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
            PlayerPrefs.SetFloat(Prefix + "subtitle_size_04l", SubtitleScale04L);
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
            PlayerPrefs.SetInt(Prefix + "physun", PhysicalSun ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "procsky", ProceduralSky ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "skyamb", SkyAmbient ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "areaprobes", AreaReflections ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "shafts", VolumetricLight ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "heightfog", HeightFog ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "taa", TemporalAA ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "dof", DepthOfField ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "chroma", ChromaticAberration ? 1 : 0);
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
            // Phase 1 lighting: shadow distance and cascade counts come from SkyMath (Low 45 m and 1 cascade, Medium 80 m
            // and 2, High 150 m and 4, Ultra 260 m and 4), with the splits tuned for a camera standing in the street.
            switch (Graphics)
            {
                case GraphicsPreset.Low:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.lodBias = 0.7f;
                    break;
                case GraphicsPreset.Medium:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.lodBias = 1f;
                    break;
                case GraphicsPreset.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.pixelLightCount = 8;
                    QualitySettings.lodBias = 1.5f;
                    break;
                default:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 8;
                    QualitySettings.pixelLightCount = 12;
                    QualitySettings.lodBias = 2f;
                    break;
            }
            float shadowDistance = SkyMath.ShadowDistance(Graphics);
            int cascades = SkyMath.ShadowCascades(Graphics);
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = cascades;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            float[] splits = SkyMath.CascadeSplits(shadowDistance, cascades);
            if (cascades == 4) QualitySettings.shadowCascade4Split = new Vector3(splits[0], splits[1], splits[2]);
            else if (cascades == 2) QualitySettings.shadowCascade2Split = splits[0];
            QualitySettings.shadowResolution = Graphics >= GraphicsPreset.High ? ShadowResolution.VeryHigh
                : (Graphics == GraphicsPreset.Medium ? ShadowResolution.High : ShadowResolution.Medium);
            QualitySettings.shadowNearPlaneOffset = 2f;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 144;
            // On URP the preset also drives the pipeline asset (no effect on the Built-in pipeline).
            RenderPath.OnGraphicsApplied();
            // Phase 1 lighting systems that cache preset choices (sun shadows, sky material, probes, lamp shafts).
            SkyLighting.OnGraphicsApplied();
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
