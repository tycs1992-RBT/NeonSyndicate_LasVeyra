using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Lighting" (Order 47): the sun's elevation, apparent colour temperature and strength, the
    /// ambient mode and the ground under the camera, the reflection probe budget and the volumetrics status, with a
    /// toggle for each Phase 1 switch so the owner can A/B them in Unity without leaving Play mode. Registered by
    /// Atmosphere.Init through SkyLighting.
    /// </summary>
    public static class SkyLightingDebug
    {
        public static void Register()
        {
            DebugPages.Register(new MenuPage { Title = "Lighting", Order = 47, Draw = Draw });
        }

        private static void Draw(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 300f, bh = 40f, gap = 12f;
            UIKit.Text(new Rect(x, y, r.width, 28f), "LIGHTING (PHASE 1)", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            Atmosphere a = Game.Atmo;
            if (a == null)
            {
                UIKit.Text(new Rect(x, y, r.width, 22f), "No atmosphere.", 17, Color.white, TextAnchor.UpperLeft);
                return;
            }
            string sun = "Sun: elevation " + a.SunElevation.ToString("0.0") + " deg, about " + Mathf.RoundToInt(a.SunKelvin) + " K, intensity " + a.SunStrength.ToString("0.00")
                + "   moon " + a.MoonStrength.ToString("0.00") + "   haze " + a.Haze.ToString("0.0") + "   clock " + a.ClockText;
            UIKit.Text(new Rect(x, y, r.width, 22f), sun, 17, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            string amb = "Ambient: " + (a.SkyAmbientActive ? "sky dome (Skybox mode, intensity " + RenderSettings.ambientIntensity.ToString("0.00") + ")" : "Trilight")
                + "   dome " + (a.NeonSkyActive ? "NeonSky" : "Unity procedural") + "   environment refreshes " + a.EnvironmentStamp;
            UIKit.Text(new Rect(x, y, r.width, 22f), amb, 17, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            Color g = a.GroundAlbedo;
            string ground = "Ground under camera: " + a.GroundUnderCamera + "   albedo " + g.r.ToString("0.00") + " " + g.g.ToString("0.00") + " " + g.b.ToString("0.00")
                + "   bounce " + a.GroundRadiance.r.ToString("0.00") + " " + a.GroundRadiance.g.ToString("0.00") + " " + a.GroundRadiance.b.ToString("0.00");
            UIKit.Text(new Rect(x, y, r.width, 22f), ground, 17, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            Color sky = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor, gr = RenderSettings.ambientGroundColor;
            string tri = "Trilight now: sky " + Fmt(sky) + "   horizon " + Fmt(eq) + "   ground " + Fmt(gr) + "   fog " + Fmt(RenderSettings.fogColor) + " density " + (RenderSettings.fogDensity * 1000f).ToString("0.00");
            UIKit.Text(new Rect(x, y, r.width, 22f), tri, 16, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperLeft);
            y += 24f;
            string shadows = "Shadows: " + QualitySettings.shadowDistance.ToString("0") + " m, " + QualitySettings.shadowCascades + " cascades, splits "
                + Fmt3(QualitySettings.shadowCascade4Split) + ", resolution " + QualitySettings.shadowResolution + ", sun " + (a.Sun != null ? a.Sun.shadows.ToString() : "none");
            UIKit.Text(new Rect(x, y, r.width, 22f), shadows, 16, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperLeft);
            y += 24f;
            string probes = "Reflection probes: " + (ReflectionProbes.Instance != null ? ReflectionProbes.Instance.Describe() : "not created");
            UIKit.Text(new Rect(x, y, r.width - 20f, 110f), probes, 15, new Color(0.78f, 0.78f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 112f;
            UIKit.Text(new Rect(x, y, r.width, 22f), "Volumetrics: " + NeonPost.VolumetricStatus + "   Underheart lamp cones " + UnderheartLampShafts.Count + " (cap " + UnderheartLampShafts.Cap(Settings.Graphics) + " per cell)", 16, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperLeft);
            y += 30f;
            UIKit.Text(new Rect(x, y, r.width, 22f), "Switches (preset " + Settings.Graphics + "; Low gets none of this, Medium the dome and a camera probe):", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Physical sun: " + On(Settings.PhysicalSun), Settings.PhysicalSun, 17)) { Settings.PhysicalSun = !Settings.PhysicalSun; Changed(); }
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "NeonSky dome: " + On(Settings.ProceduralSky), Settings.ProceduralSky, 17)) { Settings.ProceduralSky = !Settings.ProceduralSky; Changed(); }
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Sky ambient (bounce): " + On(Settings.SkyAmbient), Settings.SkyAmbient, 17)) { Settings.SkyAmbient = !Settings.SkyAmbient; Changed(); }
            y += bh + 8f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Area reflections: " + On(Settings.AreaReflections), Settings.AreaReflections, 17)) { Settings.AreaReflections = !Settings.AreaReflections; Changed(); }
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Light shafts: " + On(Settings.VolumetricLight), Settings.VolumetricLight, 17)) { Settings.VolumetricLight = !Settings.VolumetricLight; Changed(); }
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Height fog: " + On(Settings.HeightFog), Settings.HeightFog, 17)) { Settings.HeightFog = !Settings.HeightFog; Changed(); }
            y += bh + 8f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Re-render environment and probes", false, 17)) { a.RequestEnvironmentRefresh(true); if (ReflectionProbes.Instance != null) ReflectionProbes.Instance.Invalidate(); }
        }

        private static void Changed()
        {
            Settings.Save();
            SkyLighting.OnGraphicsApplied();
        }

        private static string On(bool b) { return b ? "on" : "off"; }

        private static string Fmt(Color c) { return c.r.ToString("0.00") + "/" + c.g.ToString("0.00") + "/" + c.b.ToString("0.00"); }

        private static string Fmt3(Vector3 v) { return v.x.ToString("0.00") + "/" + v.y.ToString("0.00") + "/" + v.z.ToString("0.00"); }
    }
}
