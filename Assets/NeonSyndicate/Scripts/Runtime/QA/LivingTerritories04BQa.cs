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
    /// <summary>Native capture route using successful runtime placements and real streamed actors. Never changes ownership.</summary>
    public static class LivingTerritories04BQa
    {
        private sealed class Anchor
        {
            public string Id, Title, Kind;
            public Transform Root;
        }
        private static readonly Dictionary<string, Anchor> Anchors = new Dictionary<string, Anchor>(StringComparer.Ordinal);
        public static bool FocusOverride { get; private set; }
        public static bool MomentBusy { get; private set; }
        public static Vector3 FocusPoint { get; private set; }
        /// <summary>Registered by the runtime to refuse a camera tour while an encounter is in progress.</summary>
        public static Func<bool> EncounterActive;
        /// <summary>Optional read-only state summary supplied by the territory ledger.</summary>
        public static Func<string> StateSummary;
        public static Func<string> RuntimeFeatureSummary;

        public static void ResetStatics()
        {
            Anchors.Clear(); FocusOverride = false; MomentBusy = false; FocusPoint = Vector3.zero;
            EncounterActive = null; StateSummary = null; RuntimeFeatureSummary = null;
        }
        public static void RegisterSite(string id, string title, string kind, Transform root)
        {
            if (string.IsNullOrEmpty(id) || root == null) return;
            Anchors[id] = new Anchor { Id = id, Title = title, Kind = kind, Root = root };
        }
        public static void UnregisterSite(string id)
        {
            if (!string.IsNullOrEmpty(id)) Anchors.Remove(id);
        }

        [Serializable] private sealed class SiteRecord
        {
            public string Id, Title, Kind;
            public Vector3 ActualPosition;
            public bool HasLedgerEntry, Owned, Discovered, RewardClaimed;
            public int Fortification;
            public float RecoveryCooldownSeconds;
        }
        [Serializable] private sealed class FrameSample
        {
            public string View, File, State;
            public float Hour, MeanFrameMs, P50FrameMs, P95FrameMs, P99FrameMs;
            public Vector3 CameraPosition, CameraForward;
            public int Frames, LoadedGameObjects, Renderers, LiveCreatures, LiveNpcs;
            public long UnityAllocatedBytes, ManagedBytes, DrawCalls = -1, SetPassCalls = -1, Triangles = -1, MainThreadNanoseconds = -1;
            public double GpuFrameMs = -1;
        }
        [Serializable] private sealed class Report
        {
            public string Evidence = "Actual Unity Game view PNGs. Camera tour uses live placement and camera-focused ambient streaming. No capture, reward, ownership or combat outcome is simulated.";
            public string Limitations = "Frame timings include v-sync/frame cap and capture overhead. Unsupported counters are -1. A camera tour is not gameplay acceptance or a comparative GPU benchmark. Use current-moment captures for real player capture/defense flows.";
            public string CapturedAtUtc, UnityVersion, Device, Processor, RenderPath, GraphicsPreset;
            public string StateBefore, StateAfter, ActiveRuntimeFeatures;
            public bool RequestedEnabled, RequestedRelays, RequestedEcology, RequestedEvents, RequestedAmbience;
            public string ActiveRelayId, OriginalFortRaidId, TrackedRelayId;
            public bool ChannelArmed, TerritoryEventActive;
            public int RemainingDefenders;
            public float CaptureChannelSeconds;
            public int Seed, Width, Height, FrameCap, VSync, SystemMemoryMb, GraphicsMemoryMb;
            public bool CameraFocusedAmbientStreaming;
            public List<SiteRecord> Sites = new List<SiteRecord>();
            public List<FrameSample> Samples = new List<FrameSample>();
        }
        private sealed class Counters
        {
            private ProfilerRecorder _draw, _passes, _triangles, _main;
            private readonly FrameTiming[] _gpu = new FrameTiming[1];
            public void Start()
            {
                try { _draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1); } catch (Exception) { }
                try { _passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1); } catch (Exception) { }
                try { _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1); } catch (Exception) { }
                try { _main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1); } catch (Exception) { }
            }
            public void RequestFrame()
            {
                try { FrameTimingManager.CaptureFrameTimings(); } catch (Exception) { }
            }
            public void Read(FrameSample sample)
            {
                sample.DrawCalls = Value(_draw); sample.SetPassCalls = Value(_passes); sample.Triangles = Value(_triangles);
                sample.MainThreadNanoseconds = Value(_main);
                try
                {
                    if (FrameTimingManager.GetLatestTimings(1, _gpu) > 0 && _gpu[0].gpuFrameTime > 0d)
                        sample.GpuFrameMs = _gpu[0].gpuFrameTime;
                }
                catch (Exception) { }
            }
            private static long Value(ProfilerRecorder counter) { return counter.Valid && counter.Count > 0 ? counter.LastValue : -1; }
            public void Dispose()
            {
                if (_draw.Valid) _draw.Dispose(); if (_passes.Valid) _passes.Dispose();
                if (_triangles.Valid) _triangles.Dispose(); if (_main.Valid) _main.Dispose();
            }
        }

        /// <summary>Matched 04A city/home views plus views based on the successfully placed 04B sites.</summary>
        public static List<GoldenView> BuildViews(CityLayout layout)
        {
            List<GoldenView> views = new List<GoldenView>();
            foreach (GoldenView view in VisualRebirth04AQa.BuildViews(layout))
                if (view.Id == "04a_city_aerial" || view.Id == "04a_core_street" || view.Id == "04a_hearthspire_entry" || view.Id == "04a_hearthspire_flight") views.Add(view);
            List<string> ids = new List<string>(Anchors.Keys); ids.Sort(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                Anchor site = Anchors[id];
                if (site.Root == null || !site.Root.gameObject.activeInHierarchy) continue;
                Add(views, site, "ground", new Vector3(-18f, 1.7f, -28f), new Vector3(0f, 4f, 2f), 62f);
                if (site.Kind == "hub" || site.Kind == "habitat" || site.Kind == "home")
                    Add(views, site, "canopy", new Vector3(-42f, 30f, -43f), new Vector3(0f, 6f, 2f), 57f);
            }
            return views;
        }
        private static void Add(List<GoldenView> views, Anchor anchor, string suffix, Vector3 position, Vector3 lookAt, float fov)
        {
            views.Add(new GoldenView("04b_" + anchor.Id + "_" + suffix, anchor.Title + " / " + suffix,
                anchor.Root.TransformPoint(position), anchor.Root.TransformPoint(lookAt), fov) { FollowCamera = true });
        }
        private static string ReadState()
        {
            try { return StateSummary != null ? StateSummary() : "Territory runtime state unavailable."; }
            catch (Exception e) { return "State read failed: " + e.Message; }
        }
        private static Report CreateReport(bool focus)
        {
            Report report = new Report
            {
                CapturedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), UnityVersion = Application.unityVersion,
                Device = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")", Processor = SystemInfo.processorType,
                SystemMemoryMb = SystemInfo.systemMemorySize, GraphicsMemoryMb = SystemInfo.graphicsMemorySize,
                RenderPath = GoldenManifest.CurrentRenderPath(), GraphicsPreset = Settings.Graphics.ToString(),
                Seed = Game.Layout != null ? Game.Layout.Seed : Game.Seed, FrameCap = Application.targetFrameRate,
                VSync = QualitySettings.vSyncCount, CameraFocusedAmbientStreaming = focus, StateBefore = ReadState(),
                ActiveRuntimeFeatures = RuntimeFeatureSummary != null ? RuntimeFeatureSummary() : "Runtime flags unavailable.",
                RequestedEnabled = TerritorySettings04B.Enabled, RequestedRelays = TerritorySettings04B.Relays,
                RequestedEcology = TerritorySettings04B.Ecology, RequestedEvents = TerritorySettings04B.Events, RequestedAmbience = TerritorySettings04B.Ambience
            };
            TerritoryBook book = Game.Territories != null ? Game.Territories.Book : null;
            report.ActiveRelayId = book != null ? book.ActiveId : null;
            report.TrackedRelayId = book != null ? book.TrackedId : null;
            report.ChannelArmed = book != null && book.ChannelArmed;
            report.RemainingDefenders = book != null ? book.Remaining : 0;
            report.CaptureChannelSeconds = book != null ? book.Channel : 0f;
            report.OriginalFortRaidId = Game.Strongholds != null && Game.Strongholds.Book != null ? Game.Strongholds.Book.ActiveRaid : null;
            report.TerritoryEventActive = Game.Events != null && Game.Events.TerritoryEventActive;
            List<string> ids = new List<string>(Anchors.Keys); ids.Sort(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                Anchor site = Anchors[id];
                if (site.Root == null || !site.Root.gameObject.activeInHierarchy) continue;
                TerritorySiteSave state = book != null ? book.Status(id) : null;
                report.Sites.Add(new SiteRecord { Id = id, Title = site.Title, Kind = site.Kind, ActualPosition = site.Root.position,
                    HasLedgerEntry = state != null, Owned = state != null && state.Owned, Discovered = state != null && state.Discovered,
                    RewardClaimed = state != null && state.RewardClaimed, Fortification = state != null ? state.Fortification : 0,
                    RecoveryCooldownSeconds = state != null ? state.RestCooldown : 0f });
            }
            return report;
        }
        private static FrameSample Measure(string view, string file, float hour, List<float> frames, Counters counters)
        {
            float sum = 0f; foreach (float value in frames) sum += value;
            frames.Sort();
            FrameSample sample = new FrameSample
            {
                View = view, File = file, Hour = hour, State = ReadState(), Frames = frames.Count,
                CameraPosition = Game.Cam != null && Game.Cam.Cam != null ? Game.Cam.Cam.transform.position : Vector3.zero,
                CameraForward = Game.Cam != null && Game.Cam.Cam != null ? Game.Cam.Cam.transform.forward : Vector3.zero,
                MeanFrameMs = frames.Count > 0 ? sum / frames.Count : -1f,
                P50FrameMs = Percentile(frames, .5f), P95FrameMs = Percentile(frames, .95f), P99FrameMs = Percentile(frames, .99f),
                UnityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), ManagedBytes = GC.GetTotalMemory(false),
                LoadedGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>().Length,
                Renderers = UnityEngine.Object.FindObjectsOfType<Renderer>().Length,
                LiveCreatures = UnityEngine.Object.FindObjectsOfType<Creature>().Length,
                LiveNpcs = UnityEngine.Object.FindObjectsOfType<NpcController>().Length
            };
            if (counters != null) counters.Read(sample);
            return sample;
        }
        private static float Percentile(List<float> frames, float q)
        {
            return frames.Count > 0 ? frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * q) - 1, 0, frames.Count - 1)] : -1f;
        }

        internal static IEnumerator Run(QaJob job)
        {
            yield return QaSession.WaitForPlayable(job);
            if (job.Stopping) yield break;
            if ((EncounterActive != null && EncounterActive()) ||
                (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null))
            {
                job.Fail("Finish or cancel the active encounter before a camera tour. Use Capture Current Territory Moment to record actual combat/capture without changing it."); yield break;
            }
            bool hasLiveSite = false;
            foreach (Anchor anchor in Anchors.Values)
                if (anchor.Root != null && anchor.Root.gameObject.activeInHierarchy) { hasLiveSite = true; break; }
            if (!hasLiveSite) { job.Fail("No live 04B site is registered. Enable 04B, restart Play Mode and inspect placement logs first."); yield break; }
            QaSession session = new QaSession(); job.Session = session;
            session.Begin(true, true);
            Counters counters = new Counters(); counters.Start();
            job.Cleanup = delegate { FocusOverride = false; FocusPoint = Vector3.zero; counters.Dispose(); };
            List<GoldenView> views = BuildViews(Game.Layout);
            string folder = GoldenShots.UniqueFolder(QaTools.GoldenRoot, "LivingTerritories04B_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            GoldenManifest manifest = GoldenShots.CreateManifest(session);
            manifest.Notes = "04B native camera tour. Actual placed sites and real ambient actors. Ambient streaming follows the camera; player is protected and parked. Captures do not trigger encounters or change territory ownership. Matched 04A view IDs retained. Read territory-look-tests.json.";
            Report report = CreateReport(true);
            List<float> frames = new List<float>(120);
            float[] hours = { 12f, 22f };
            int done = 0;
            foreach (float hour in hours)
            {
                manifest.Hours.Add(hour);
                foreach (GoldenView view in views)
                {
                    bool captured = false;
                    while (!captured && !job.Stopping)
                    {
                        while (QaSession.Interruption() != null && !job.Stopping)
                        { job.Progress = "paused because " + QaSession.Interruption(); yield return null; }
                        if (job.Stopping) yield break;
                        FocusOverride = true; FocusPoint = view.LookAt;
                        session.PrepareView(view); session.SetClock(hour);
                        job.Progress = "04B view " + (done + 1) + " / " + (views.Count * hours.Length) + ": " + view.Id;
                        job.Runner.SetPose(view.Position, view.LookAt, view.Fov);
                        if (Game.Cam != null) Game.Cam.CutHistory();
                        yield return GoldenShots.Settle(job, 8f);
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        frames.Clear();
                        for (int i = 0; i < 120 && !job.Stopping && QaSession.Interruption() == null; i++)
                        { counters.RequestFrame(); yield return null; frames.Add(Time.unscaledDeltaTime * 1000f); }
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        yield return new WaitForEndOfFrame();
                        if (QaSession.Interruption() != null) continue;
                        string file = GoldenViews.FileName(view, hour);
                        int width, height;
                        string error = GoldenShots.SaveScreen(Path.Combine(folder, file), out width, out height);
                        if (error != null) { job.Fail(error); yield break; }
                        manifest.Width = report.Width = width; manifest.Height = report.Height = height;
                        manifest.Shots.Add(new GoldenShotEntry { Id = view.Id, Title = view.Title, File = file, Hour = hour,
                            Position = view.Position, LookAt = view.LookAt, Fov = view.Fov, Width = width, Height = height,
                            District = CityLayout.DistrictName(CityLayout.DistrictAt(view.Position)) });
                        report.Samples.Add(Measure(view.Id, file, hour, frames, counters));
                        captured = true; done++;
                    }
                    if (job.Stopping) yield break;
                }
            }
            report.StateAfter = ReadState(); manifest.PlayerParked = session.PlayerParked;
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            File.WriteAllText(Path.Combine(folder, "territory-look-tests.json"), JsonUtility.ToJson(report, true));
            job.Output = folder; job.Message = "Saved " + done + " real 04B views and runtime measurements to " + folder;
        }

        /// <summary>One real player frame. Never sets QaTools.Busy, which would cancel original stronghold raids.</summary>
        internal static bool StartMoment()
        {
            if (MomentBusy || QaTools.Busy)
            { QaTools.MomentFinished(false, "", "A capture is already in progress."); return false; }
            if (Game.State != GameState.Playing || Game.Player == null || Game.Cam == null || Game.Root == null)
            { QaTools.MomentFinished(false, "", "Resume gameplay before capturing a current territory moment."); return false; }
            MomentBusy = true;
            try { Game.Root.StartCoroutine(CaptureMoment()); return true; }
            catch (Exception e)
            {
                MomentBusy = false;
                QaTools.MomentFinished(false, "", "Current-frame capture could not start: " + e.Message);
                return false;
            }
        }
        private static IEnumerator CaptureMoment()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                string folder = GoldenShots.UniqueFolder(QaTools.GoldenRoot, "TerritoryMoment04B_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(folder);
                Report report = CreateReport(false);
                report.Evidence = "Actual current player Game view including HUD. No camera, clock, health, ownership or encounter state was changed by this capture.";
                int width, height;
                string error = GoldenShots.SaveScreen(Path.Combine(folder, "moment.png"), out width, out height);
                if (error != null) QaTools.MomentFinished(false, "", error);
                else
                {
                    report.Width = width; report.Height = height; report.StateAfter = ReadState();
                    report.Samples.Add(Measure("current_player_frame", "moment.png", Game.Atmo != null ? Game.Atmo.Hours : -1f, new List<float>(), null));
                    File.WriteAllText(Path.Combine(folder, "territory-moment.json"), JsonUtility.ToJson(report, true));
                    QaTools.MomentFinished(true, folder, "Saved actual current player frame to " + folder);
                }
            }
            catch (Exception e) { QaTools.MomentFinished(false, "", "Current-frame capture failed: " + e.Message); }
            finally { MomentBusy = false; }
        }
    }
}
