using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Background generation of the procedural PBR surfaces. The pure parts of a surface (SurfaceHeight fields, the
    /// SurfaceMaps shade and normal texels, the SurfaceColor albedo texels, gloss variation and occlusion) touch no
    /// Unity object, so they run on worker threads the way BodyFactory builds bodies; only the Texture2D upload stays
    /// on the main thread. The first request queues every kind (large ones first) on a few low-priority background
    /// threads. A request for a kind no worker has started yet is computed inline by the caller, so the main thread
    /// never waits for the whole queue, only for a kind that is already half done. Results are deterministic whatever
    /// the thread, and a generation counter drops the work of a previous session after Tex.ResetStatics.
    /// </summary>
    public static partial class Tex
    {
        /// <summary>The pure results for one kind at one size, before any texture exists.</summary>
        public sealed class SurfaceSet
        {
            public SurfaceKind Kind;
            public int Size;
            public SurfaceFields Fields;
            public SurfaceShade Shade;
            public Color32[] NormalPixels;
            public Color32[] AlbedoPixels;
            public float[] GlossVariation;
            public float[] Occlusion;
            /// <summary>Set when the computation threw; the caller then falls back to the plain material.</summary>
            public string Error;
        }

        private sealed class SurfaceJob
        {
            public SurfaceKind Kind;
            public int Size;
            public int Generation;
            public bool Started;
            public bool Done;
            public SurfaceSet Result;
            /// <summary>Realtime of the last request (large sets are released some time after it, see TrimStale).</summary>
            public float LastAccess;
        }

        /// <summary>Seconds after its last request that a large kind's retained fields are released (recomputed if asked again).</summary>
        public const float SurfaceRetainSeconds = 60f;

        private static readonly object _jobLock = new object();
        private static readonly Dictionary<SurfaceKind, SurfaceJob> _jobs = new Dictionary<SurfaceKind, SurfaceJob>();
        private static readonly List<SurfaceJob> _queue = new List<SurfaceJob>();
        private static readonly List<Thread> _workers = new List<Thread>();
        private static int _generation;
        private static int _jobsInline;
        private static int _jobsOnWorkers;
        private static int _jobsTrimmed;
        private static float _msOnMainThread;

        /// <summary>How many kinds the main thread had to compute itself and how many the workers finished (diagnostics).</summary>
        public static int SurfaceJobsInline { get { return _jobsInline; } }
        public static int SurfaceJobsOnWorkers { get { return _jobsOnWorkers; } }
        /// <summary>How many large kinds had their retained data released after SurfaceRetainSeconds (diagnostics).</summary>
        public static int SurfaceJobsTrimmed { get { return _jobsTrimmed; } }
        /// <summary>Milliseconds the main thread spent computing or waiting for surfaces (diagnostics).</summary>
        public static float SurfaceMsOnMainThread { get { return _msOnMainThread; } }

        private static void ResetJobStatics()
        {
            lock (_jobLock)
            {
                _generation++;
                _jobs.Clear();
                _queue.Clear();
                _workers.Clear();
                Monitor.PulseAll(_jobLock);
            }
            _jobsInline = 0;
            _jobsOnWorkers = 0;
            _jobsTrimmed = 0;
            _msOnMainThread = 0f;
            _sizeCap = 0;
        }

        /// <summary>The pure computation of one surface kind at one size (safe on any thread).</summary>
        public static SurfaceSet ComputeSurface(SurfaceKind kind, int size)
        {
            SurfaceSet set = new SurfaceSet { Kind = kind, Size = size };
            try
            {
                SurfaceFields f = SurfaceHeight.Build(kind, size);
                SurfaceShade shade = SurfaceMaps.Shade(f.Height, size, SurfaceColor.ShadeGain(kind));
                set.Fields = f;
                set.Shade = shade;
                set.NormalPixels = SurfaceMaps.Normal(f.Height, size, SurfaceColor.NormalStrength(kind, size));
                set.AlbedoPixels = SurfaceColor.Albedo(kind, f, shade, 0);
                set.GlossVariation = SurfaceColor.GlossVariation(kind, f, shade);
                float strength = SurfaceColor.OcclusionStrength(kind);
                float[] occ = new float[f.Length];
                for (int i = 0; i < occ.Length; i++) occ[i] = SurfaceMaps.Occlusion(shade.Cavity[i], strength);
                set.Occlusion = occ;
            }
            catch (System.Exception e)
            {
                set.Error = e.GetType().Name + ": " + e.Message;
            }
            return set;
        }

        /// <summary>Queues every kind for the workers (once per session); sizes come from the graphics preset.</summary>
        private static void EnsureSurfaceQueue()
        {
            lock (_jobLock)
            {
                if (_jobs.Count > 0) return;
                int gen = _generation;
                List<SurfaceJob> large = new List<SurfaceJob>();
                List<SurfaceJob> small = new List<SurfaceJob>();
                foreach (SurfaceKind kind in SurfaceKinds)
                {
                    SurfaceJob job = new SurfaceJob { Kind = kind, Size = PbrSize(kind), Generation = gen };
                    _jobs[kind] = job;
                    if (SurfaceHeight.IsLarge(kind)) large.Add(job);
                    else small.Add(job);
                }
                _queue.AddRange(large);
                _queue.AddRange(small);
                int threads = Mathf.Clamp(SystemInfo.processorCount - 2, 1, 6);
                for (int i = 0; i < threads; i++)
                {
                    Thread t = new Thread(WorkerLoop) { IsBackground = true, Name = "NeonSyndicate surfaces " + i, Priority = System.Threading.ThreadPriority.BelowNormal };
                    _workers.Add(t);
                    t.Start(gen);
                }
            }
        }

        private static readonly SurfaceKind[] SurfaceKinds = (SurfaceKind[])System.Enum.GetValues(typeof(SurfaceKind));

        private static void WorkerLoop(object state)
        {
            int gen = (int)state;
            while (true)
            {
                SurfaceJob job = null;
                lock (_jobLock)
                {
                    if (gen != _generation) return;
                    for (int i = 0; i < _queue.Count; i++)
                    {
                        if (_queue[i].Started) continue;
                        job = _queue[i];
                        job.Started = true;
                        break;
                    }
                    if (job == null) return;
                }
                SurfaceSet result = ComputeSurface(job.Kind, job.Size);
                lock (_jobLock)
                {
                    job.Result = result;
                    job.Done = true;
                    if (gen == _generation) _jobsOnWorkers++;
                    Monitor.PulseAll(_jobLock);
                }
            }
        }

        /// <summary>
        /// The surface set of a kind: from the worker that computed it, or computed here when no worker has started it
        /// (the main thread then does one kind's work, never the queue's). Blocks only on a kind already in progress.
        /// </summary>
        public static SurfaceSet Surface(SurfaceKind kind)
        {
            float t0 = Time.realtimeSinceStartup;
            EnsureSurfaceQueue();
            SurfaceJob job;
            bool inline = false;
            lock (_jobLock)
            {
                job = _jobs[kind];
                job.LastAccess = t0;
                if (!job.Started)
                {
                    job.Started = true;
                    inline = true;
                }
            }
            if (inline)
            {
                SurfaceSet result = ComputeSurface(job.Kind, job.Size);
                lock (_jobLock)
                {
                    job.Result = result;
                    job.Done = true;
                    _jobsInline++;
                    Monitor.PulseAll(_jobLock);
                }
            }
            else
            {
                // A worker has it: wait for it. If nothing arrives in ten seconds (a stalled thread) compute it here.
                bool stalled = false;
                lock (_jobLock)
                {
                    int waited = 0;
                    while (!job.Done && !stalled)
                    {
                        Monitor.Wait(_jobLock, 50);
                        waited++;
                        if (waited > 200 && !job.Done) stalled = true;
                    }
                }
                if (stalled)
                {
                    Log.Warn("[Tex] Surface " + kind + " stalled on its worker; computing it on the main thread.");
                    SurfaceSet result = ComputeSurface(job.Kind, job.Size);
                    lock (_jobLock)
                    {
                        if (!job.Done)
                        {
                            job.Result = result;
                            job.Done = true;
                            _jobsInline++;
                        }
                    }
                }
            }
            _msOnMainThread += (Time.realtimeSinceStartup - t0) * 1000f;
            if (job.Result != null && job.Result.Error != null) Log.Warn("[Tex] Surface " + kind + " failed: " + job.Result.Error);
            TrimStale(t0);
            return job.Result;
        }

        /// <summary>
        /// Releases the retained fields, shade and pixel arrays of large kinds not requested for SurfaceRetainSeconds
        /// (about 10 MB each at 512, kept so later palettes and mask variants cost nothing during the boot). A released
        /// kind is recomputed inline if a material asks for it again. Called on the main thread from Surface.
        /// </summary>
        public static void TrimStale(float now)
        {
            lock (_jobLock)
            {
                foreach (SurfaceJob job in _jobs.Values)
                {
                    if (!job.Done || job.Result == null || !SurfaceHeight.IsLarge(job.Kind)) continue;
                    if (now - job.LastAccess < SurfaceRetainSeconds) continue;
                    job.Result = null;
                    job.Done = false;
                    job.Started = false;
                    _jobsTrimmed++;
                }
            }
        }
    }
}
