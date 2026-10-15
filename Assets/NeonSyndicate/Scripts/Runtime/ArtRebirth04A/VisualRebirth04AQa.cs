using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace NeonSyndicate
{
    /// <summary>
    /// Real in-engine look tests using the existing QA runner's refusal, cancellation, player protection and restore
    /// contracts. Anchors follow successfully placed forts, including seeds where the desired catalog plot is busy.
    /// This tool produces no images until invoked in a running Unity game.
    /// </summary>
    public static class VisualRebirth04AQa
    {
        private static readonly Dictionary<string, Transform> Strongholds = new Dictionary<string, Transform>(StringComparer.Ordinal);
        public static void ResetStatics() { Strongholds.Clear(); }

        public static void RegisterStronghold(string id, Transform root)
        {
            if (root != null && (id == "hearthspire" || id == "galehook" || id == "sunscar")) Strongholds[id] = root;
        }

        [Serializable]
        private sealed class LookSample
        {
            public string View, Look, Weather, File;
            public float Hour;
            public Vector3 Position, LookAt;
            public int Frames;
            public float MeanFrameMilliseconds, P95FrameMilliseconds;
            public long UnityAllocatedBytes, ManagedBytes;
            public long DrawCalls = -1, SetPassCalls = -1, Triangles = -1;
            public int AccentLights, MoteFields, MovingOrnaments;
        }

        [Serializable]
        private sealed class LookManifest
        {
            public string Version = "Visual Rebirth 04A";
            public string Evidence = "Actual Unity Game view captures. Frame samples include the current frame cap/v-sync; they are not a GPU benchmark.";
            public string UnityVersion, RenderPath, Device, GraphicsPreset;
            public bool Enabled, City, Gardens, Strongholds, Atmosphere;
            public int Seed, VSync, FrameCap;
            public int Loaded04ASourceMeshes, Loaded04AMaterials;
            public string[] MissingStrongholds;
            public List<LookSample> Samples = new List<LookSample>();
        }

        private struct Look
        {
            public string Id;
            public float Hour;
            public Weather Weather;
            public Look(string id, float hour, Weather weather) { Id = id; Hour = hour; Weather = weather; }
        }

        private sealed class RenderCounters
        {
            public ProfilerRecorder DrawCalls, SetPassCalls, Triangles;
            public void Start()
            {
                // Unsupported platform counters remain unavailable (-1), never a fabricated zero.
                try { DrawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1); } catch (Exception) { }
                try { SetPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1); } catch (Exception) { }
                try { Triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1); } catch (Exception) { }
            }
            public void Dispose()
            {
                if (DrawCalls.Valid) DrawCalls.Dispose();
                if (SetPassCalls.Valid) SetPassCalls.Dispose();
                if (Triangles.Valid) Triangles.Dispose();
            }
        }

        private static readonly Look[] Looks =
        {
            new Look("day", 12f, Weather.Clear),
            new Look("golden_hour", 17.25f, Weather.Clear),
            new Look("dusk", 18.25f, Weather.Clear),
            new Look("night", 22f, Weather.Clear),
            new Look("rain", 15f, Weather.Rain)
        };

        /// <summary>Stable view IDs for before/after comparison. Missing forts are omitted and reported explicitly.</summary>
        public static List<GoldenView> BuildViews(CityLayout layout)
        {
            List<GoldenView> views = new List<GoldenView>();
            foreach (GoldenView view in GoldenViews.Build(layout))
                if (view.Id == "city_aerial" || view.Id == "core_street")
                {
                    GoldenView copy = view; copy.Id = "04a_" + copy.Id; copy.FollowCamera = true; views.Add(copy);
                }
            AddFortView(views, "hearthspire", "entry", "Hearthspire gate approach at player height",
                new Vector3(0f, 1.7f, -37f), new Vector3(0f, 7f, 2f), 60f);
            AddFortView(views, "hearthspire", "garden", "Hearthspire eastern living grove and decorative drake roost",
                new Vector3(21f, 1.7f, -5f), new Vector3(19f, 5f, 13f), 62f);
            AddFortView(views, "hearthspire", "upper_terrace", "Hearthspire north terrace and the courtyard below",
                new Vector3(-17f, 7.7f, 20f), new Vector3(0f, 5f, 2f), 62f);
            AddFortView(views, "hearthspire", "flight", "Hearthspire district as approached in flight",
                new Vector3(-63f, 48f, -66f), new Vector3(0f, 8f, 3f), 56f);
            AddFortView(views, "galehook", "entry", "Galehook living garden redoubt approach",
                new Vector3(-7f, 1.7f, -39f), new Vector3(0f, 7f, 6f), 60f);
            AddFortView(views, "sunscar", "entry", "Sunscar fractured extraction bastion approach",
                new Vector3(7f, 1.7f, -39f), new Vector3(0f, 7f, 6f), 60f);
            return views;
        }

        private static void AddFortView(List<GoldenView> views, string id, string suffix, string title, Vector3 position, Vector3 lookAt, float fov)
        {
            Transform root;
            if (!Strongholds.TryGetValue(id, out root) || root == null || !root.gameObject.activeInHierarchy) return;
            views.Add(new GoldenView("04a_" + id + "_" + suffix, title, root.TransformPoint(position), root.TransformPoint(lookAt), fov) { FollowCamera = true });
        }

        internal static IEnumerator Run(QaJob job)
        {
            yield return QaSession.WaitForPlayable(job);
            if (job.Stopping) yield break;
            if (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null)
            {
                job.Fail("Finish or cancel the active stronghold raid before starting a 04A look capture.");
                yield break;
            }
            QaSession session = new QaSession();
            job.Session = session;
            session.Begin(true, true);
            List<GoldenView> views = BuildViews(Game.Layout);
            string folder = GoldenShots.UniqueFolder(QaTools.GoldenRoot,
                "VisualRebirth04A_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            GoldenManifest golden = GoldenShots.CreateManifest(session);
            golden.Notes = "04A actual Unity look tests. Fixed view/clock/weather and current graphics options; HUD and grain suppressed. Shot IDs include weather/look. Fort anchors are the successful runtime placements. Decorative drakes are not combat AI. Read look-tests.json for captured settings and frame/memory measurements.";
            LookManifest report = CreateReport();
            RenderCounters counters = new RenderCounters();
            counters.Start();
            job.Cleanup = counters.Dispose;
            List<float> frameTimes = new List<float>(90);
            int done = 0, total = views.Count * Looks.Length;
            foreach (Look look in Looks)
            {
                golden.Hours.Add(look.Hour);
                foreach (GoldenView view in views)
                {
                    bool captured = false;
                    while (!captured && !job.Stopping)
                    {
                        while (QaSession.Interruption() != null && !job.Stopping)
                        {
                            job.Progress = "paused because " + QaSession.Interruption();
                            yield return null;
                        }
                        if (job.Stopping) yield break;
                        session.PrepareView(view);
                        session.SetClock(look.Hour);
                        Game.Atmo.SetWeather(look.Weather, true);
                        job.Progress = "04A look " + (done + 1) + " of " + total + ": " + view.Id + " / " + look.Id;
                        job.Runner.SetPose(view.Position, view.LookAt, view.Fov);
                        if (Game.Cam != null) Game.Cam.CutHistory();
                        yield return GoldenShots.Settle(job, GoldenShots.SettleSeconds + GoldenShots.ExtraAfterClockChange);
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        frameTimes.Clear();
                        for (int i = 0; i < 60 && !job.Stopping && QaSession.Interruption() == null; i++)
                        {
                            yield return null;
                            frameTimes.Add(Time.unscaledDeltaTime * 1000f);
                        }
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        yield return new WaitForEndOfFrame();
                        if (QaSession.Interruption() != null) continue;
                        string id = view.Id + "_" + look.Id;
                        string file = id + "_" + GoldenViews.ClockStamp(look.Hour) + ".png";
                        int width, height;
                        string error = GoldenShots.SaveScreen(Path.Combine(folder, file), out width, out height);
                        if (error != null) { job.Fail(error); yield break; }
                        golden.Width = width; golden.Height = height;
                        golden.Shots.Add(new GoldenShotEntry { Id = id, Title = view.Title + " / " + look.Id,
                            File = file, Hour = look.Hour, Position = view.Position, LookAt = view.LookAt, Fov = view.Fov,
                            District = CityLayout.DistrictName(CityLayout.DistrictAt(view.Position)), Width = width, Height = height });
                        report.Samples.Add(Sample(view, look, file, frameTimes, counters));
                        // Incremental manifests preserve completed evidence if the user cancels later in the route.
                        golden.PlayerParked = session.PlayerParked;
                        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(golden, true));
                        File.WriteAllText(Path.Combine(folder, "look-tests.json"), JsonUtility.ToJson(report, true));
                        captured = true; done++;
                    }
                }
            }
            job.Output = folder;
            job.Message = "Saved " + done + " actual Unity 04A look-test images and frame/memory samples to " + folder +
                (report.MissingStrongholds.Length > 0 ? ". Missing placed forts: " + string.Join(", ", report.MissingStrongholds) : "");
        }

        private static LookManifest CreateReport()
        {
            List<string> missing = new List<string>();
            foreach (string id in new[] { "hearthspire", "galehook", "sunscar" })
            {
                Transform root;
                if (!Strongholds.TryGetValue(id, out root) || root == null || !root.gameObject.activeInHierarchy) missing.Add(id);
            }
            return new LookManifest
            {
                UnityVersion = Application.unityVersion, RenderPath = GoldenManifest.CurrentRenderPath(),
                Device = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")",
                GraphicsPreset = Settings.Graphics.ToString(), Seed = Game.Layout != null ? Game.Layout.Seed : Game.Seed,
                Enabled = VisualRebirth04ASettings.Enabled, City = VisualRebirth04ASettings.City,
                Gardens = VisualRebirth04ASettings.Gardens, Strongholds = VisualRebirth04ASettings.Strongholds,
                Atmosphere = VisualRebirth04ASettings.Atmosphere, VSync = QualitySettings.vSyncCount,
                FrameCap = Application.targetFrameRate, MissingStrongholds = missing.ToArray(),
                Loaded04ASourceMeshes = VisualRebirth04AAssets.SourceMeshCount,
                Loaded04AMaterials = VisualRebirth04AAssets.MaterialCount
            };
        }

        private static LookSample Sample(GoldenView view, Look look, string file, List<float> frames, RenderCounters counters)
        {
            float sum = 0f;
            foreach (float frame in frames) sum += frame;
            frames.Sort();
            VisualRebirth04AAmbience ambience = VisualRebirth04AAmbience.Instance;
            return new LookSample
            {
                View = view.Id, Look = look.Id, Weather = look.Weather.ToString(), File = file,
                Hour = look.Hour, Position = view.Position, LookAt = view.LookAt, Frames = frames.Count,
                MeanFrameMilliseconds = frames.Count > 0 ? sum / frames.Count : 0f,
                P95FrameMilliseconds = frames.Count > 0 ? frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * .95f) - 1, 0, frames.Count - 1)] : 0f,
                UnityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), ManagedBytes = GC.GetTotalMemory(false),
                DrawCalls = counters.DrawCalls.Valid && counters.DrawCalls.Count > 0 ? counters.DrawCalls.LastValue : -1,
                SetPassCalls = counters.SetPassCalls.Valid && counters.SetPassCalls.Count > 0 ? counters.SetPassCalls.LastValue : -1,
                Triangles = counters.Triangles.Valid && counters.Triangles.Count > 0 ? counters.Triangles.LastValue : -1,
                AccentLights = ambience != null ? ambience.ActiveLights : 0,
                MoteFields = ambience != null ? ambience.ActiveFields : 0,
                MovingOrnaments = ambience != null ? ambience.ActiveMotions : 0
            };
        }
    }
}
