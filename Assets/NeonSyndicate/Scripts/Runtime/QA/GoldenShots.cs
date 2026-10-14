using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>One saved golden screenshot, as listed in manifest.json.</summary>
    [Serializable]
    public sealed class GoldenShotEntry
    {
        public string Id;
        public string Title;
        public string File;
        public float Hour;
        public Vector3 Position;
        public Vector3 LookAt;
        public float Fov;
        public string District;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// manifest.json of a golden-shot folder: every view and the settings it was taken with, so two captures can be
    /// compared knowing whether the resolution, render path or graphics options differed.
    /// </summary>
    [Serializable]
    public sealed class GoldenManifest
    {
        public int FormatVersion = 1;
        public string Game;
        public string GameVersion;
        public string UnityVersion;
        public string Platform;
        public string RenderPath;
        public string GraphicsDevice;
        public string CapturedAt;
        public int Seed;
        public int Width;
        public int Height;
        public string GraphicsPreset;
        public bool PostEffects;
        public bool AmbientOcclusion;
        public bool MotionBlur;
        public bool AutoExposure;
        public bool FilmGrainSetting;
        public bool FilmGrainInShots;
        public float Brightness;
        public float FieldOfViewSetting;
        public int SettleFrames;
        public float SettleSeconds;
        public bool PlayerParked;
        public float InnerWorld;
        public float Frost;
        public float Rift;
        public string Notes;
        public List<float> Hours = new List<float>();
        public List<GoldenShotEntry> Shots = new List<GoldenShotEntry>();

        /// <summary>"Built-in" for the default pipeline, otherwise the render pipeline asset's type (for example the URP asset).</summary>
        public static string CurrentRenderPath()
        {
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            return rp == null ? "Built-in" : rp.GetType().Name;
        }
    }

    /// <summary>
    /// Golden screenshots: the fourteen fixed GoldenViews at noon and at 22:00 in clear weather with the HUD hidden, each
    /// taken after the frame has settled (streamed lights, the reflection probe, eye adaptation and, in the editor,
    /// shader compilation), written as PNGs to Application.persistentDataPath/GoldenShots/yyyyMMdd_HHmm with a
    /// manifest.json. The last two views show the Phase 3 looks: the Frostline (its cold follows the camera) and the
    /// Veil (the capture shifts into the Inner World for that view and back out). Compare two folders with
    /// Neon Syndicate > QA > Compare Golden Shots.
    /// </summary>
    internal static class GoldenShots
    {
        /// <summary>Frames to wait after moving the camera before taking a shot.</summary>
        public const int SettleFrames = 45;
        /// <summary>Real seconds to wait after moving the camera (street lights move every 0.35 s, the reflection probe every 2.5 s).</summary>
        public const float SettleSeconds = 3f;
        /// <summary>Extra wait for the first view after the clock changes (sky, ambient light and reflections update).</summary>
        public const float ExtraAfterClockChange = 2f;
        /// <summary>Longest extra wait for shaders that are still compiling in the editor.</summary>
        public const float ShaderWaitLimit = 30f;

        public static IEnumerator Run(QaJob job)
        {
            yield return QaSession.WaitForPlayable(job);
            if (job.Stopping) yield break;
            QaSession session = new QaSession();
            job.Session = session;
            session.Begin(true, true);
            List<GoldenView> views = GoldenViews.Build(Game.Layout);
            string folder;
            try
            {
                folder = UniqueFolder(QaTools.GoldenRoot, DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                job.Fail("Could not create the golden shots folder: " + e.Message);
                yield break;
            }
            GoldenManifest manifest = CreateManifest(session);
            int total = views.Count * GoldenViews.Hours.Length, done = 0;
            for (int h = 0; h < GoldenViews.Hours.Length; h++)
            {
                float hour = GoldenViews.Hours[h];
                manifest.Hours.Add(hour);
                session.SetClock(hour);
                for (int i = 0; i < views.Count; i++)
                {
                    GoldenView view = views[i];
                    string file = GoldenViews.FileName(view, hour);
                    int width = 0, height = 0;
                    // The first view after a clock change, and every zone view (and the one after it), waits longer: the
                    // Frostline cold eases in and out over about two seconds and the Veil's crystals grow in.
                    bool zoneChange = view.FollowCamera || view.Inner || (i > 0 && (views[i - 1].FollowCamera || views[i - 1].Inner));
                    bool clockChanged = i == 0 || zoneChange;
                    session.PrepareView(view);
                    // One shot, taken again from the start if a menu, the phone or a fade gets in the way.
                    while (true)
                    {
                        if (job.Stopping) yield break;
                        string blocked = QaSession.Interruption();
                        if (blocked != null)
                        {
                            yield return WaitUntilClear(job, blocked);
                            if (job.Stopping) yield break;
                            session.SetClock(hour);
                            session.PrepareView(view);
                            clockChanged = true;
                        }
                        job.Progress = "shot " + (done + 1) + " of " + total + ": " + view.Id + " at " + GoldenViews.ClockStamp(hour);
                        job.Runner.SetPose(view.Position, view.LookAt, view.Fov);
                        if (Game.Cam != null) Game.Cam.CutHistory();
                        yield return Settle(job, clockChanged ? SettleSeconds + ExtraAfterClockChange : SettleSeconds);
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        yield return new WaitForEndOfFrame();
                        if (QaSession.Interruption() != null) continue;
                        string error = SaveScreen(Path.Combine(folder, file), out width, out height);
                        if (error != null)
                        {
                            job.Fail(error);
                            yield break;
                        }
                        break;
                    }
                    manifest.Width = width;
                    manifest.Height = height;
                    manifest.Shots.Add(new GoldenShotEntry
                    {
                        Id = view.Id,
                        Title = view.Title,
                        File = file,
                        Hour = hour,
                        Position = view.Position,
                        LookAt = view.LookAt,
                        Fov = view.Fov,
                        District = CityLayout.DistrictName(CityLayout.DistrictAt(view.Position)),
                        Width = width,
                        Height = height
                    });
                    done++;
                }
            }
            manifest.PlayerParked = session.PlayerParked;
            try { File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true)); }
            catch (Exception e)
            {
                job.Fail("The screenshots were saved but manifest.json could not be written: " + e.Message);
                yield break;
            }
            job.Output = folder;
            job.Message = "Saved " + done + " screenshots (" + manifest.Width + "x" + manifest.Height + ") to " + folder;
        }

        /// <summary>Waits while something opened during the capture (for example the map) is in the way, then carries on.</summary>
        private static IEnumerator WaitUntilClear(QaJob job, string reason)
        {
            Log.Warn("[QA] Golden shots paused because " + reason + ". Close it to carry on (the shot is taken again).");
            while (!job.Stopping && QaSession.Interruption() != null)
            {
                job.Progress = "paused because " + QaSession.Interruption();
                yield return null;
            }
        }

        /// <summary>Waits for the frame to settle after a camera cut (and for shader compilation in the editor).</summary>
        private static IEnumerator Settle(QaJob job, float seconds)
        {
            float start = Time.realtimeSinceStartup;
            int frames = 0;
            while (!job.Stopping)
            {
                frames++;
                float elapsed = Time.realtimeSinceStartup - start;
                bool waitShaders = ShadersCompiling() && elapsed < seconds + ShaderWaitLimit;
                if (frames >= SettleFrames && elapsed >= seconds && !waitShaders) yield break;
                yield return null;
            }
        }

        private static bool ShadersCompiling()
        {
#if UNITY_EDITOR
            return UnityEditor.ShaderUtil.anythingCompiling;
#else
            return false;
#endif
        }

        /// <summary>Saves the finished frame as an opaque PNG. Returns an error message, or null on success.</summary>
        private static string SaveScreen(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            Texture2D shot = null, opaque = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
                if (shot == null) return "The screen could not be captured (is the Game view visible?).";
                width = shot.width;
                height = shot.height;
                // Copy into an RGB texture so the PNG never carries the frame's alpha (which can make viewers show holes).
                opaque = new Texture2D(width, height, TextureFormat.RGB24, false);
                opaque.SetPixels32(shot.GetPixels32());
                opaque.Apply(false);
                File.WriteAllBytes(path, opaque.EncodeToPNG());
                return null;
            }
            catch (Exception e)
            {
                return "Could not save " + path + ": " + e.Message;
            }
            finally
            {
                if (shot != null) UnityEngine.Object.Destroy(shot);
                if (opaque != null) UnityEngine.Object.Destroy(opaque);
            }
        }

        private static GoldenManifest CreateManifest(QaSession session)
        {
            GoldenManifest m = new GoldenManifest();
            m.Game = Game.Title;
            m.GameVersion = Game.Version;
            m.UnityVersion = Application.unityVersion;
            m.Platform = Application.platform.ToString();
            m.RenderPath = GoldenManifest.CurrentRenderPath();
            m.GraphicsDevice = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")";
            m.CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            m.Seed = Game.Layout != null ? Game.Layout.Seed : Game.Seed;
            m.GraphicsPreset = Settings.Graphics.ToString();
            m.PostEffects = Settings.Bloom;
            m.AmbientOcclusion = Settings.AmbientOcclusion;
            m.MotionBlur = Settings.MotionBlur;
            m.AutoExposure = Settings.AutoExposure;
            m.FilmGrainSetting = session.FilmGrainSetting;
            m.FilmGrainInShots = !session.GrainSuppressed && session.FilmGrainSetting;
            m.Brightness = Settings.Brightness;
            m.FieldOfViewSetting = Settings.FieldOfView;
            m.SettleFrames = SettleFrames;
            m.SettleSeconds = SettleSeconds;
            m.InnerWorld = PostGrade.InnerWorld;
            m.Frost = PostGrade.Frost;
            m.Rift = PostGrade.Rift;
            m.Notes = "Clear weather, HUD hidden, film grain off during the capture, player parked in the south-east desert so people, traffic and street props stay out of the views. frostline_outpost and veil_core_street measure the Frostline cold and the Veil at the camera; veil_core_street is taken inside the Veil.";
            return m;
        }

        private static string UniqueFolder(string root, string name)
        {
            string path = Path.Combine(root, name);
            for (int i = 2; Directory.Exists(path); i++) path = Path.Combine(root, name + "_" + i);
            return path;
        }
    }
}
