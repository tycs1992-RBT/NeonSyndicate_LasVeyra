using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds character bodies ahead of time on worker threads, so spawning a pedestrian, a driver or an officer never
    /// stalls a frame. Spawners ask for a look (CivilianLook, PoliceLook, SwatLook) and get one whose mesh data is
    /// already built; HumanoidRig then takes that data instead of building it on the main thread. Looks that were not
    /// stocked (the player, story characters) still build on the spot. BodyBuilder is pure C#, so this is thread safe.
    /// </summary>
    public static class BodyFactory
    {
        public enum Kind { Civilian = 0, Police = 1, Swat = 2 }

        private sealed class Job
        {
            public Appearance Look;
            public int Detail;
            public Kind Kind;
            public BodyData Data;
            public bool Done;
            public float HandedAt;
        }

        private static readonly object Gate = new object();
        private static readonly List<Job> Stock = new List<Job>();
        private static readonly List<Job> Handed = new List<Job>();
        private static readonly int[] Target = { 10, 4, 3 };
        private static System.Random _rand = new System.Random(20240611);
        private static int _inFlight;
        private const int MaxInFlight = 2;
        private const int MaxHanded = 24;
        private static bool _threadsChecked, _threads;

        /// <summary>Bodies built ahead of time and taken by spawns (for the debug overlay and tests).</summary>
        public static int Hits { get; private set; }
        /// <summary>Spawns that had to build on the main thread because the stock was empty.</summary>
        public static int Misses { get; private set; }

        private static bool Threads
        {
            get
            {
                if (!_threadsChecked)
                {
                    _threadsChecked = true;
                    _threads = Application.platform != RuntimePlatform.WebGLPlayer && SystemInfo.processorCount > 1;
                }
                return _threads;
            }
        }

        /// <summary>Detail for ambient characters: crowd meshes on the Low preset, standard otherwise.</summary>
        public static int AmbientDetail { get { return Settings.Graphics == GraphicsPreset.Low ? 0 : 1; } }

        public static Appearance CivilianLook(System.Random rand)
        {
            Appearance look;
            if (TakeStocked(Kind.Civilian, out look)) return look;
            return Appearance.Random(rand);
        }

        public static Appearance PoliceLook()
        {
            Appearance look;
            if (TakeStocked(Kind.Police, out look)) return look;
            return Appearance.Police(NextRandom());
        }

        public static Appearance SwatLook()
        {
            Appearance look;
            if (TakeStocked(Kind.Swat, out look)) return look;
            return Appearance.Swat(NextRandom());
        }

        /// <summary>Mesh data built ahead of time for exactly this look and detail, or null to build now.</summary>
        public static BodyData TakePrebuilt(Appearance look, int detail)
        {
            lock (Gate)
            {
                for (int i = 0; i < Handed.Count; i++)
                {
                    Job j = Handed[i];
                    if (j.Detail != detail || !j.Look.SameAs(look)) continue;
                    Handed.RemoveAt(i);
                    Hits++;
                    return j.Data;
                }
            }
            Misses++;
            return null;
        }

        /// <summary>
        /// Hands in mesh data built elsewhere on a worker thread (the creature director builds its humanoid creatures
        /// ahead), so the next HumanoidRig built for exactly this look and detail takes it instead of building now.
        /// </summary>
        public static void Offer(Appearance look, int detail, BodyData data)
        {
            if (data == null) return;
            lock (Gate)
            {
                Job j = new Job();
                j.Look = look;
                j.Detail = detail;
                j.Data = data;
                j.Done = true;
                j.HandedAt = Time.unscaledTime;
                Handed.Add(j);
                if (Handed.Count > MaxHanded) Handed.RemoveAt(0);
            }
        }

        /// <summary>Keeps the stock topped up. Call once per frame from the main thread.</summary>
        public static void Tick()
        {
            if (!Threads) return;
            int detail = AmbientDetail;
            lock (Gate)
            {
                // Drop handed-out looks nobody built a body for (a spawn that was cancelled).
                float now = Time.unscaledTime;
                for (int i = Handed.Count - 1; i >= 0; i--)
                    if (now - Handed[i].HandedAt > 20f) Handed.RemoveAt(i);
                if (_inFlight >= MaxInFlight) return;
                for (int k = 0; k < Target.Length && _inFlight < MaxInFlight; k++)
                {
                    int have = 0;
                    for (int i = 0; i < Stock.Count; i++) if (Stock[i].Kind == (Kind)k) have++;
                    if (have >= Target[k]) continue;
                    Job job = new Job();
                    job.Kind = (Kind)k;
                    job.Detail = detail;
                    job.Look = MakeLook((Kind)k);
                    Stock.Add(job);
                    _inFlight++;
                    if (!ThreadPool.QueueUserWorkItem(Work, job))
                    {
                        _inFlight--;
                        Stock.Remove(job);
                        _threads = false;
                        return;
                    }
                }
            }
        }

        private static void Work(object state)
        {
            Job job = (Job)state;
            BodyData data = null;
            try
            {
                data = BodyBuilder.Build(job.Look, RigLayout.From(job.Look), job.Detail);
            }
            catch (Exception)
            {
                data = null;
            }
            lock (Gate)
            {
                _inFlight = Mathf.Max(0, _inFlight - 1);
                if (data == null)
                {
                    Stock.Remove(job);
                    return;
                }
                job.Data = data;
                job.Done = true;
            }
        }

        private static bool TakeStocked(Kind kind, out Appearance look)
        {
            lock (Gate)
            {
                for (int i = 0; i < Stock.Count; i++)
                {
                    Job j = Stock[i];
                    if (j.Kind != kind || !j.Done) continue;
                    Stock.RemoveAt(i);
                    j.HandedAt = Time.unscaledTime;
                    Handed.Add(j);
                    if (Handed.Count > MaxHanded) Handed.RemoveAt(0);
                    look = j.Look;
                    return true;
                }
            }
            look = default(Appearance);
            return false;
        }

        private static System.Random NextRandom()
        {
            return new System.Random(_rand.Next());
        }

        private static Appearance MakeLook(Kind kind)
        {
            System.Random r = NextRandom();
            switch (kind)
            {
                case Kind.Police: return Appearance.Police(r);
                case Kind.Swat: return Appearance.Swat(r);
                default: return Appearance.Random(r);
            }
        }

        public static void ResetStatics()
        {
            lock (Gate)
            {
                Stock.Clear();
                Handed.Clear();
                _inFlight = 0;
            }
            _rand = new System.Random(20240611);
            _threadsChecked = false;
            Hits = 0;
            Misses = 0;
        }
    }
}
