using System;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds companion bodies (the creature look, the shared creature builder and the companion sculpt pass, all pure
    /// C#) on a worker thread so a summon, a rescue or an evolution never stalls a frame; the main thread then only
    /// uploads the mesh. Without worker threads (WebGL, one core) it builds on the spot.
    /// </summary>
    public static class CompanionBodyFactory
    {
        /// <summary>One body order. Poll Done (or Failed) from the main thread.</summary>
        public sealed class Job
        {
            public string Archetype = "";
            public int Seed;
            public int Evolution;
            public string Spec = "";
            public int Detail = 1;
            public CreatureBody Body;
            public string Error = "";
            private int _state;

            public bool Done { get { return Volatile.Read(ref _state) == 1; } }
            public bool Failed { get { return Volatile.Read(ref _state) == 2; } }
            public bool Finished { get { return Volatile.Read(ref _state) != 0; } }
            /// <summary>Set by the owner when the result is no longer wanted (the body is dropped when it arrives).</summary>
            public bool Cancelled;

            internal void Complete(CreatureBody b) { Body = b; Volatile.Write(ref _state, 1); }
            internal void Fail(string e) { Error = e ?? "build failed"; Volatile.Write(ref _state, 2); }
        }

        private static int _threads = -1;

        public static void ResetStatics() { _threads = -1; }

        private static bool UseThreads
        {
            get
            {
                if (_threads < 0) _threads = Application.platform != RuntimePlatform.WebGLPlayer && SystemInfo.processorCount > 1 ? 1 : 0;
                return _threads == 1;
            }
        }

        /// <summary>The whole body build: look, creature builder, companion sculpt. Pure (any thread).</summary>
        public static CreatureBody BuildNow(string archetype, int seed, int evolution, string spec, int detail)
        {
            CreatureLook look = CompanionLooks.For(archetype, seed, evolution);
            CreatureBody body = CreatureBuilder.Build(look, detail);
            CompanionSculpt.Apply(body, archetype, evolution, spec, detail);
            return body;
        }

        /// <summary>Orders a body. It is built on a worker thread when one is available, otherwise right away.</summary>
        public static Job Request(string archetype, int seed, int evolution, string spec, int detail)
        {
            Job j = new Job { Archetype = archetype ?? "", Seed = seed, Evolution = evolution, Spec = spec ?? "", Detail = detail };
            if (!UseThreads)
            {
                Run(j);
                return j;
            }
            try
            {
                if (!ThreadPool.QueueUserWorkItem(s => Run((Job)s), j)) Run(j);
            }
            catch (Exception)
            {
                Run(j);
            }
            return j;
        }

        private static void Run(Job j)
        {
            try { j.Complete(BuildNow(j.Archetype, j.Seed, j.Evolution, j.Spec, j.Detail)); }
            catch (Exception e) { j.Fail(e.GetType().Name + ": " + e.Message); }
        }
    }
}
