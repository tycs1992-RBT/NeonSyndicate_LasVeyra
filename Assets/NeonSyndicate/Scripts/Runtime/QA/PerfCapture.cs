using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The performance flythrough: flies the camera along PerfPath (about 80 seconds through seven districts) at 21:00
    /// in clear weather, with the frame-rate cap and vertical sync lifted and the player parked in the desert, and logs
    /// every frame: frame time, CPU main thread, render thread and GPU time where Unity reports them, and managed
    /// memory allocated per frame where measurable. Writes Application.persistentDataPath/Perf/yyyyMMdd_HHmmss.csv and a
    /// .txt summary (average, p50, p95, p99, max and the ten worst frames with where they happened).
    /// </summary>
    internal static class PerfCapture
    {
        /// <summary>Hour of the flythrough: night, with the street lights and neon on (the heavier case).</summary>
        public const float Hour = 21f;
        /// <summary>Seconds spent at the start pose before recording, so the first frames are not counted.</summary>
        public const float WarmUpSeconds = 3f;

        private struct Sample
        {
            public int Frame;
            public float Time;
            public float FrameMs;
            public float CpuMainMs;
            public float CpuRenderMs;
            public float GpuMs;
            public long GcBytes;
            public Vector3 Position;
        }

        private sealed class Recorders
        {
            public ProfilerRecorder MainThread;
            public ProfilerRecorder GcAlloc;
            public readonly FrameTiming[] Timing = new FrameTiming[1];
            public bool FrameTimingOn;

            public void Start()
            {
                try { MainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1); }
                catch (Exception) { }
                try { GcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1); }
                catch (Exception) { }
                try { FrameTimingOn = FrameTimingManager.IsFeatureEnabled(); }
                catch (Exception) { FrameTimingOn = false; }
            }

            public void Dispose()
            {
                if (MainThread.Valid) MainThread.Dispose();
                if (GcAlloc.Valid) GcAlloc.Dispose();
            }
        }

        public static IEnumerator Run(QaJob job)
        {
            yield return QaSession.WaitForPlayable(job);
            if (job.Stopping) yield break;
            QaSession session = new QaSession();
            job.Session = session;
            session.Begin(true, false);
            session.SetClock(Hour);
            session.Uncap();
            PerfPath path = PerfPath.CreateDefault();
            job.Runner.SetPose(path.PositionAt(0f), path.LookAtAt(0f), PerfPath.Fov);
            if (Game.Cam != null) Game.Cam.CutHistory();

            // Warm up at the start of the route: let the player's surroundings clear, lights move and shaders compile.
            float warm = Time.realtimeSinceStartup;
            while (!job.Stopping && (Time.realtimeSinceStartup - warm < WarmUpSeconds || ShadersCompiling()) && Time.realtimeSinceStartup - warm < WarmUpSeconds + 30f)
            {
                job.Progress = "warming up";
                // A menu or the phone opened meanwhile: the warm-up starts again once it is closed.
                if (QaSession.Interruption() != null) warm = Time.realtimeSinceStartup;
                yield return null;
            }
            if (job.Stopping) yield break;

            Recorders rec = new Recorders();
            rec.Start();
            job.Cleanup = rec.Dispose;
            List<Sample> samples = new List<Sample>(8192);
            float start = Time.realtimeSinceStartup;
            int frame = 0;
            float lastTime = 0f;
            Vector3 lastPos = path.PositionAt(0f);
            while (!job.Stopping)
            {
                float t = Time.realtimeSinceStartup - start;
                if (frame > 0)
                {
                    // Everything measured now describes the frame that just finished (the previous pose).
                    Sample s = new Sample { Frame = frame - 1, Time = lastTime, Position = lastPos };
                    s.FrameMs = Time.unscaledDeltaTime * 1000f;
                    s.CpuMainMs = rec.MainThread.Valid ? rec.MainThread.LastValue / 1000000f : -1f;
                    s.GcBytes = rec.GcAlloc.Valid ? rec.GcAlloc.LastValue : -1;
                    s.CpuRenderMs = -1f;
                    s.GpuMs = -1f;
                    if (rec.FrameTimingOn)
                    {
                        // Frame timings arrive a few frames late (the GPU finishes later); they are logged as they come.
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, rec.Timing) > 0)
                        {
                            FrameTiming ft = rec.Timing[0];
                            if (ft.cpuRenderThreadFrameTime > 0.0) s.CpuRenderMs = (float)ft.cpuRenderThreadFrameTime;
                            if (ft.gpuFrameTime > 0.0) s.GpuMs = (float)ft.gpuFrameTime;
                            if (s.CpuMainMs < 0f && ft.cpuMainThreadFrameTime > 0.0) s.CpuMainMs = (float)ft.cpuMainThreadFrameTime;
                        }
                    }
                    samples.Add(s);
                }
                if (t >= path.Duration) break;
                string blocked = QaSession.Interruption();
                if (blocked != null)
                {
                    job.Fail("The flythrough stopped because " + blocked + " (the timings would be wrong). Run it again without touching the controls.");
                    yield break;
                }
                Vector3 pos = path.PositionAt(t);
                job.Runner.SetPose(pos, path.LookAtAt(t), PerfPath.Fov);
                job.Progress = "flying, " + Mathf.FloorToInt(t) + " of " + Mathf.CeilToInt(path.Duration) + " s";
                lastTime = t;
                lastPos = pos;
                frame++;
                yield return null;
            }
            if (job.Stopping) yield break;

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string csv, txt;
            try
            {
                Directory.CreateDirectory(QaTools.PerfRoot);
                csv = Path.Combine(QaTools.PerfRoot, stamp + ".csv");
                txt = Path.Combine(QaTools.PerfRoot, stamp + ".txt");
                File.WriteAllText(csv, BuildCsv(samples));
                File.WriteAllText(txt, BuildSummary(samples, path, session, rec, stamp));
            }
            catch (Exception e)
            {
                job.Fail("The flythrough finished but its log could not be written: " + e.Message);
                yield break;
            }
            List<float> ms = FrameTimes(samples);
            job.Output = txt;
            job.Message = samples.Count + " frames, average " + F(PerfStats.Average(ms)) + " ms, p99 " + F(PerfStats.Percentile(ms, 99f)) + " ms. Saved " + txt;
        }

        private static bool ShadersCompiling()
        {
#if UNITY_EDITOR
            return UnityEditor.ShaderUtil.anythingCompiling;
#else
            return false;
#endif
        }

        // ------------------------------------------------------------------ files

        private static string F(float v)
        {
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Opt(float v)
        {
            return v >= 0f ? v.ToString("0.000", CultureInfo.InvariantCulture) : "";
        }

        private static List<float> FrameTimes(List<Sample> samples)
        {
            List<float> list = new List<float>(samples.Count);
            foreach (Sample s in samples) list.Add(s.FrameMs);
            return list;
        }

        private static string BuildCsv(List<Sample> samples)
        {
            StringBuilder sb = new StringBuilder(samples.Count * 80 + 128);
            sb.Append("frame,time_s,frame_ms,cpu_main_ms,cpu_render_ms,gpu_ms,gc_alloc_bytes,pos_x,pos_y,pos_z,district\n");
            CultureInfo inv = CultureInfo.InvariantCulture;
            foreach (Sample s in samples)
            {
                sb.Append(s.Frame.ToString(inv)).Append(',');
                sb.Append(s.Time.ToString("0.000", inv)).Append(',');
                sb.Append(s.FrameMs.ToString("0.000", inv)).Append(',');
                sb.Append(Opt(s.CpuMainMs)).Append(',');
                sb.Append(Opt(s.CpuRenderMs)).Append(',');
                sb.Append(Opt(s.GpuMs)).Append(',');
                sb.Append(s.GcBytes >= 0 ? s.GcBytes.ToString(inv) : "").Append(',');
                sb.Append(s.Position.x.ToString("0.0", inv)).Append(',');
                sb.Append(s.Position.y.ToString("0.0", inv)).Append(',');
                sb.Append(s.Position.z.ToString("0.0", inv)).Append(',');
                sb.Append(CityLayout.DistrictName(CityLayout.DistrictAt(s.Position))).Append('\n');
            }
            return sb.ToString();
        }

        private static void StatsLine(StringBuilder sb, string label, List<float> values)
        {
            if (values.Count == 0)
            {
                sb.Append(label).Append(": not reported by Unity on this setup\n");
                return;
            }
            sb.Append(label).Append(": avg ").Append(F(PerfStats.Average(values)))
                .Append("  p50 ").Append(F(PerfStats.Percentile(values, 50f)))
                .Append("  p95 ").Append(F(PerfStats.Percentile(values, 95f)))
                .Append("  p99 ").Append(F(PerfStats.Percentile(values, 99f)))
                .Append("  max ").Append(F(PerfStats.Max(values)))
                .Append(" ms (").Append(values.Count).Append(" frames)\n");
        }

        private static string BuildSummary(List<Sample> samples, PerfPath path, QaSession session, Recorders rec, string stamp)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            List<float> frame = FrameTimes(samples), main = new List<float>(), render = new List<float>(), gpu = new List<float>(), gc = new List<float>();
            foreach (Sample s in samples)
            {
                if (s.CpuMainMs >= 0f) main.Add(s.CpuMainMs);
                if (s.CpuRenderMs >= 0f) render.Add(s.CpuRenderMs);
                if (s.GpuMs >= 0f) gpu.Add(s.GpuMs);
                if (s.GcBytes >= 0) gc.Add(s.GcBytes);
            }
            StringBuilder sb = new StringBuilder(4096);
            sb.Append(Game.Title).Append(" perf flythrough ").Append(stamp).Append('\n');
            sb.Append("Unity ").Append(Application.unityVersion).Append(", ").Append(Application.platform)
                .Append(Application.isEditor ? " (numbers from the editor include editor overhead; a standalone build is faster)" : "").Append('\n');
            sb.Append("Render path ").Append(GoldenManifest.CurrentRenderPath()).Append(", GPU ").Append(SystemInfo.graphicsDeviceName)
                .Append(" (").Append(SystemInfo.graphicsDeviceType).Append("), CPU ").Append(SystemInfo.processorType)
                .Append(" x").Append(SystemInfo.processorCount).Append('\n');
            sb.Append("Screen ").Append(Screen.width).Append('x').Append(Screen.height).Append(", graphics preset ").Append(Settings.Graphics)
                .Append(", post effects ").Append(Settings.Bloom ? "on" : "off").Append(", AO ").Append(Settings.AmbientOcclusion ? "on" : "off")
                .Append(", motion blur ").Append(Settings.MotionBlur ? "on" : "off").Append(", traffic ").Append(Settings.TrafficDensity.ToString("0.00", inv))
                .Append(", people ").Append(Settings.PedDensity.ToString("0.00", inv)).Append('\n');
            sb.Append("Route ").Append(path.Length.ToString("0", inv)).Append(" m in ").Append(path.Duration.ToString("0.0", inv))
                .Append(" s at ").Append(GoldenViews.ClockStamp(Hour)).Append(", clear weather, frame-rate cap ")
                .Append(session.FrameRateUncapped ? "lifted" : "unchanged").Append(", player ").Append(session.PlayerParked ? "parked in the desert (no crowds or traffic along the route)" : "left in place").Append('\n');
            sb.Append('\n');
            float avg = PerfStats.Average(frame);
            float p99 = PerfStats.Percentile(frame, 99f);
            sb.Append("Frames: ").Append(samples.Count).Append(", average ").Append(avg > 0f ? (1000f / avg).ToString("0.0", inv) : "0").Append(" fps, 1% low ")
                .Append(p99 > 0f ? (1000f / p99).ToString("0.0", inv) : "0").Append(" fps, frames over 33.3 ms: ").Append(PerfStats.CountAbove(frame, 33.34f))
                .Append(", over 16.7 ms: ").Append(PerfStats.CountAbove(frame, 16.67f)).Append('\n');
            StatsLine(sb, "Frame time", frame);
            StatsLine(sb, "CPU main thread", main);
            StatsLine(sb, "CPU render thread", render);
            StatsLine(sb, "GPU", gpu);
            if (gc.Count > 0)
            {
                int allocFrames = 0;
                foreach (float b in gc) if (b > 0f) allocFrames++;
                sb.Append("Managed allocations: ").Append((PerfStats.Sum(gc) / 1024f).ToString("0.0", inv)).Append(" KB total, ")
                    .Append(PerfStats.Average(gc).ToString("0", inv)).Append(" bytes per frame on average, max ")
                    .Append(PerfStats.Max(gc).ToString("0", inv)).Append(" bytes, ").Append(allocFrames).Append(" frames allocated\n");
            }
            else sb.Append("Managed allocations: not measurable on this setup\n");
            if (!rec.FrameTimingOn) sb.Append("Note: GPU and render thread times need Frame Timing Stats (Project Settings > Player > Other Settings).\n");
            sb.Append('\n').Append("Worst 10 frames:\n");
            int[] worst = PerfStats.WorstIndices(frame, 10);
            foreach (int i in worst)
            {
                Sample s = samples[i];
                sb.Append("  ").Append(s.FrameMs.ToString("0.00", inv).PadLeft(8)).Append(" ms at ").Append(s.Time.ToString("0.00", inv)).Append(" s (frame ")
                    .Append(s.Frame).Append(") over ").Append(CityLayout.DistrictName(CityLayout.DistrictAt(s.Position)))
                    .Append(" at ").Append(s.Position.x.ToString("0", inv)).Append(", ").Append(s.Position.y.ToString("0", inv)).Append(", ").Append(s.Position.z.ToString("0", inv)).Append('\n');
            }
            return sb.ToString();
        }
    }
}
