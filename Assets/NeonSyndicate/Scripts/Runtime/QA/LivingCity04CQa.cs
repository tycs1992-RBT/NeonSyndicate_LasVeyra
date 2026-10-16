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
    /// <summary>Actual Unity screenshots of registered entrances and rooms. A camera tour never proves player traversal.</summary>
    public static class LivingCity04CQa
    {
        public static bool FocusOverride { get; private set; }
        public static Vector3 FocusPoint { get; private set; }
        public static bool MomentBusy { get; private set; }
        public static void ResetStatics() { FocusOverride = false; FocusPoint = Vector3.zero; MomentBusy = false; }

        [Serializable] private sealed class VenueRecord
        {
            public string Id, Title, OriginalPoi, Status;
            public int BuildingSeed, Parts, Renderers, Colliders, NearTriangles, FarTriangles;
            public bool Bound, Available, Loaded, Occupied, Visited, TwoStoreys;
            public Vector3 Entrance, Outside, Service;
        }
        [Serializable] private sealed class Sample
        {
            public string Id, File, State, OccupiedVenue, Weather, ActiveQuest, QuestVenue, QuestAction;
            public Vector3 CameraPosition, CameraForward, PlayerPosition;
            public int Frames, GameObjects, Renderers, Lights, Npcs, Creatures, Residents;
            public float Hour, MeanFrameMs = -1f, P50FrameMs = -1f, P95FrameMs = -1f, P99FrameMs = -1f;
            public long UnityAllocatedBytes, ManagedBytes, DrawCalls = -1, SetPassCalls = -1, Triangles = -1, MainThreadNs = -1;
        }
        [Serializable] private sealed class Report
        {
            public string Evidence = "Actual Unity Game view PNGs from registered venue geometry. Camera tour only; no doorway traversal, service transaction or gameplay success is simulated.";
            public string Limitations = "Tour parks the player and temporarily moves the camera/clock. It is not entry/service/exit proof. Timings include capture overhead and frame caps. Unsupported profiler counters are -1. No comparative performance claim is made.";
            public string CapturedAtUtc, UnityVersion, Device, Cpu, RenderPath, Graphics, StateBefore, StateAfter;
            public int Seed, Width, Height, FrameCap, VSync;
            public bool EnabledAtBoot, ResidentsAtBoot, AmbienceAtBoot, RequestedEnabled, RequestedResidents, RequestedAmbience, CameraFocus;
            public List<VenueRecord> Venues = new List<VenueRecord>();
            public List<Sample> Samples = new List<Sample>();
        }
        private sealed class Counters
        {
            private ProfilerRecorder _draw, _passes, _triangles, _main;
            public void Start()
            {
                try { _draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1); } catch (Exception) { }
                try { _passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1); } catch (Exception) { }
                try { _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1); } catch (Exception) { }
                try { _main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1); } catch (Exception) { }
            }
            public void Read(Sample sample)
            {
                sample.DrawCalls = Read(_draw); sample.SetPassCalls = Read(_passes);
                sample.Triangles = Read(_triangles); sample.MainThreadNs = Read(_main);
            }
            private long Read(ProfilerRecorder value) { return value.Valid && value.Count > 0 ? value.LastValue : -1; }
            public void Dispose()
            {
                if (_draw.Valid) _draw.Dispose(); if (_passes.Valid) _passes.Dispose();
                if (_triangles.Valid) _triangles.Dispose(); if (_main.Valid) _main.Dispose();
            }
        }

        /// <summary>Checked before QaTools.Busy changes, because that flag can cancel existing encounters.</summary>
        internal static string TourProblem()
        {
            if (Game.Inhabited == null || Game.Player == null) return "Start a game and wait for city construction before the 04C tour.";
            if (Game.State != GameState.Playing && Game.State != GameState.Paused) return "Resume a loaded game before the 04C tour.";
            if ((Game.Crafting != null && Game.Crafting.ScreenOpen) || (Game.Menus != null && Game.Menus.ChoiceOpen))
                return "Close the Forge or conversation choice before the camera tour. Current Place Moment can record the open menu.";
            if (Game.Inner != null && Game.Inner.IsInner) return "Return from the Veil before the 04C tour.";
            if (Game.Player.InVehicle || Game.Player.IsDead || Game.Player.IsRagdolled || !Game.Player.IsGroundedPublic ||
                Game.Player.Climbing || Game.Player.TravelState != TravelMode.None)
                return "Stand safely on foot before the 04C camera tour.";
            if ((Game.Missions != null && Game.Missions.Active != null) || (Game.Quests != null && Game.Quests.Running != null) ||
                (Game.Territories != null && Game.Territories.HasActiveEncounter) ||
                (Game.Strongholds != null && Game.Strongholds.Book != null && Game.Strongholds.Book.ActiveRaid != null) ||
                (Game.Events != null && Game.Events.AnyEventActive) || Game.Inhabited.IsDepotEncounterActive ||
                CombatDirector.InCombat || (Game.Wanted != null && Game.Wanted.Stars > 0))
                return "Finish or cancel the active quest, encounter or pursuit before a camera tour. Current Place Moment records it without changing it.";
            return null;
        }

        public static List<GoldenView> BuildViews()
        {
            List<GoldenView> views = new List<GoldenView>();
            if (Game.Inhabited == null) return views;
            foreach (GoldenView view in VisualRebirth04AQa.BuildViews(Game.Layout))
                if (view.Id == "04a_core_street" || view.Id == "04a_city_aerial") views.Add(view);
            foreach (VenueBinding04C venue in Game.Inhabited.Venues)
            {
                if (venue.Building == null) continue;
                Vector3 right = MathUtil.RightOf(-venue.Outward);
                string prefix = "04c_" + venue.Definition.Id;
                views.Add(new GoldenView(prefix + "_entrance", venue.Definition.Title + " / actual entrance",
                    venue.Entrance + venue.Outward * 7f + right * 1.8f + Vector3.up * 1.7f,
                    venue.Entrance - venue.Outward * 2f + Vector3.up * 1.7f, 65f) { FollowCamera = true });
                if (!venue.Available) continue;
                Transform root = venue.View.Root;
                views.Add(new GoldenView(prefix + "_interior", venue.Definition.Title + " / ground floor",
                    root.TransformPoint(new Vector3(0f, 1.7f, 2.1f)), venue.Point("service") + Vector3.up * 1.2f, 68f) { FollowCamera = true });
                if (!venue.View.Composition.twoStoreys) continue;
                VenueNode04C first = null, landing = null;
                foreach (VenueNode04C node in venue.View.Composition.nodes)
                {
                    if (node.id == "stair_tread_0") first = node;
                    if (node.id == "upper_landing") landing = node;
                }
                if (first == null || landing == null) continue;
                Vector3 bottom = ArtInhabitedCity04C.Vector(first.position);
                Vector3 top = ArtInhabitedCity04C.Vector(landing.position) + Vector3.up * (landing.scale[1] * .5f);
                views.Add(new GoldenView(prefix + "_stairs", venue.Definition.Title + " / source stair route",
                    root.TransformPoint(new Vector3(bottom.x, 1.7f, bottom.z - 1.1f)), root.TransformPoint(top + Vector3.up), 66f) { FollowCamera = true });
                views.Add(new GoldenView(prefix + "_upper", venue.Definition.Title + " / upper landing",
                    root.TransformPoint(top + Vector3.up * 1.7f), root.TransformPoint(new Vector3(1.5f, top.y + 1.3f, 10f)), 67f) { FollowCamera = true });
            }
            return views;
        }

        private static string State()
        {
            return Game.Inhabited != null ? Game.Inhabited.Summary() : "No inhabited-city runtime.";
        }
        private static Report CreateReport(bool focus)
        {
            InhabitedCitySystem city = Game.Inhabited;
            Report report = new Report
            {
                CapturedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), UnityVersion = Application.unityVersion,
                Device = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")", Cpu = SystemInfo.processorType,
                RenderPath = GoldenManifest.CurrentRenderPath(), Graphics = Settings.Graphics.ToString(),
                Seed = Game.Layout != null ? Game.Layout.Seed : Game.Seed, FrameCap = Application.targetFrameRate,
                VSync = QualitySettings.vSyncCount, CameraFocus = focus, StateBefore = State(),
                EnabledAtBoot = city != null && city.EnabledAtBoot, RequestedEnabled = InhabitedCitySettings04C.Enabled,
                ResidentsAtBoot = city != null && city.ResidentsAtBoot, AmbienceAtBoot = city != null && city.AmbienceAtBoot,
                RequestedResidents = InhabitedCitySettings04C.Residents, RequestedAmbience = InhabitedCitySettings04C.Ambience
            };
            if (city == null) return report;
            foreach (VenueBinding04C venue in city.Venues)
                report.Venues.Add(new VenueRecord
                {
                    Id = venue.Definition.Id, Title = venue.Definition.Title, OriginalPoi = venue.Poi != null ? venue.Poi.Name : "",
                    Status = venue.Status, BuildingSeed = venue.Building != null ? venue.Building.Seed : 0,
                    Bound = venue.Building != null, Available = venue.Available, Loaded = venue.View != null,
                    Occupied = city.OccupiedId == venue.Definition.Id, Visited = city.Book != null && city.Book.Visited(venue.Definition.Id),
                    Entrance = venue.Entrance, Outside = venue.Outside, Service = venue.Point("service"),
                    TwoStoreys = venue.Available && venue.View.Composition.twoStoreys,
                    Parts = venue.Available ? venue.View.PartCount : 0, Renderers = venue.Available ? venue.View.RendererCount : 0,
                    Colliders = venue.Available ? venue.View.ColliderCount : 0, NearTriangles = venue.Available ? venue.View.NearTriangles : 0,
                    FarTriangles = venue.Available ? venue.View.FarTriangles : 0
                });
            return report;
        }
        private static Sample Measure(string id, string file, List<float> frames, Counters counters)
        {
            float sum = 0f; foreach (float frame in frames) sum += frame; frames.Sort();
            Sample sample = new Sample
            {
                Id = id, File = file, State = State(), OccupiedVenue = Game.Inhabited != null ? Game.Inhabited.OccupiedId : "",
                Weather = Game.Atmo != null ? Game.Atmo.Current.ToString() : "unavailable",
                ActiveQuest = Game.Quests != null && Game.Quests.Running != null ? Game.Quests.Running.Quest.Id : "",
                QuestVenue = Game.Quests != null && Game.Quests.Running != null ? Game.Quests.Running.CurrentVenue : "",
                QuestAction = Game.Quests != null && Game.Quests.Running != null ? Game.Quests.Running.CurrentAction : "",
                CameraPosition = Game.Cam.Cam.transform.position, CameraForward = Game.Cam.Cam.transform.forward,
                PlayerPosition = Game.PlayerPosition, Hour = Game.Atmo != null ? Game.Atmo.Hours : -1f, Frames = frames.Count,
                MeanFrameMs = frames.Count > 0 ? sum / frames.Count : -1f,
                P50FrameMs = Percentile(frames, .5f), P95FrameMs = Percentile(frames, .95f), P99FrameMs = Percentile(frames, .99f),
                UnityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), ManagedBytes = GC.GetTotalMemory(false),
                GameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>().Length,
                Renderers = UnityEngine.Object.FindObjectsOfType<Renderer>().Length, Lights = UnityEngine.Object.FindObjectsOfType<Light>().Length,
                Npcs = UnityEngine.Object.FindObjectsOfType<NpcController>().Length, Creatures = UnityEngine.Object.FindObjectsOfType<Creature>().Length,
                Residents = Game.Inhabited != null ? Game.Inhabited.ResidentCount : 0
            };
            if (counters != null) counters.Read(sample);
            return sample;
        }
        private static float Percentile(List<float> frames, float q)
        {
            return frames.Count == 0 ? -1f : frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * q) - 1, 0, frames.Count - 1)];
        }

        internal static IEnumerator Run(QaJob job)
        {
            yield return QaSession.WaitForPlayable(job);
            if (job.Stopping) yield break;
            string problem = TourProblem();
            if (problem != null) { job.Fail(problem); yield break; }
            List<GoldenView> views = BuildViews();
            if (views.Count == 0) { job.Fail("No bound venue cameras are available."); yield break; }
            Report report = CreateReport(true);
            QaSession session = new QaSession(); job.Session = session; session.Begin(true, true);
            Counters counters = new Counters(); counters.Start();
            job.Cleanup = delegate { FocusOverride = false; FocusPoint = Vector3.zero; counters.Dispose(); };
            string folder = GoldenShots.UniqueFolder(QaTools.GoldenRoot, "InhabitedCity04C_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            GoldenManifest manifest = GoldenShots.CreateManifest(session);
            manifest.Notes = "04C camera tour of actual bound entrances and source room/stair geometry. Player is parked. Not entry, service, collision or chapter acceptance. Read inhabited-city-look-tests.json. Disabled 04C retains matched entrance cameras for baseline comparisons.";
            List<float> frames = new List<float>(90);
            int done = 0;
            foreach (float hour in new[] { 12f, 22f, 14f })
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
                        FocusOverride = true; FocusPoint = view.Position;
                        session.PrepareView(view); session.SetClock(hour);
                        if (hour == 14f && Game.Atmo != null) Game.Atmo.SetWeather(Weather.Rain, true);
                        job.Progress = "04C camera " + (done + 1) + " / " + (views.Count * 3) + ": " + view.Id;
                        job.Runner.SetPose(view.Position, view.LookAt, view.Fov);
                        if (Game.Cam != null) Game.Cam.CutHistory();
                        yield return GoldenShots.Settle(job, 3f);
                        if (job.Stopping) yield break;
                        if (QaSession.Interruption() != null) continue;
                        frames.Clear();
                        for (int i = 0; i < 90 && !job.Stopping && QaSession.Interruption() == null; i++)
                        { yield return null; frames.Add(Time.unscaledDeltaTime * 1000f); }
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
                        report.Samples.Add(Measure(view.Id, file, frames, counters));
                        captured = true; done++;
                    }
                    if (job.Stopping) yield break;
                }
            }
            report.StateAfter = State(); manifest.PlayerParked = session.PlayerParked;
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            File.WriteAllText(Path.Combine(folder, "inhabited-city-look-tests.json"), JsonUtility.ToJson(report, true));
            job.Output = folder; job.Message = "Saved " + done + " actual 04C camera views and native counters to " + folder;
        }

        /// <summary>Current player or shop-menu frame, without QaTools.Busy or any scene/state changes.</summary>
        internal static bool StartMoment()
        {
            if (MomentBusy || LivingTerritories04BQa.MomentBusy || QaTools.Busy)
            { QaTools.InhabitedMomentFinished(false, "", "A capture is already in progress."); return false; }
            if ((Game.State != GameState.Playing && Game.State != GameState.Paused) || Game.Player == null || Game.Cam == null || Game.Root == null)
            { QaTools.InhabitedMomentFinished(false, "", "Load a game before capturing a current place moment."); return false; }
            MomentBusy = true;
            try { Game.Root.StartCoroutine(CaptureMoment()); return true; }
            catch (Exception e) { MomentBusy = false; QaTools.InhabitedMomentFinished(false, "", e.Message); return false; }
        }
        private static IEnumerator CaptureMoment()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                string folder = GoldenShots.UniqueFolder(QaTools.GoldenRoot, "PlaceMoment04C_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(folder);
                Report report = CreateReport(false);
                report.Evidence = "Actual current player Game view including any open service menu. Camera, clock, menu, combat and saved progress were not changed by the capture.";
                report.Limitations = "One real frame, not an uninterrupted entry/service/exit recording. Empty timing samples are -1. Record continuous gameplay separately.";
                int width, height;
                string error = GoldenShots.SaveScreen(Path.Combine(folder, "moment.png"), out width, out height);
                if (error != null) QaTools.InhabitedMomentFinished(false, "", error);
                else
                {
                    report.Width = width; report.Height = height; report.StateAfter = State();
                    report.Samples.Add(Measure("current_player_frame", "moment.png", new List<float>(), null));
                    File.WriteAllText(Path.Combine(folder, "place-moment.json"), JsonUtility.ToJson(report, true));
                    QaTools.InhabitedMomentFinished(true, folder, "Saved the actual player/service frame to " + folder);
                }
            }
            catch (Exception error) { QaTools.InhabitedMomentFinished(false, "", "Current frame failed: " + error.Message); }
            finally { MomentBusy = false; }
        }
    }
}
