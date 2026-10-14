using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Creature bodies are built on worker threads (CreatureBuilder and BodyBuilder are pure C#), so a spawn never
    /// stalls a frame: the ecology orders its bodies before it places a group, and code spawns of non-humanoids wait
    /// hidden until theirs arrives (a few frames). The main thread only uploads the finished mesh. A Glass Mite swarm
    /// shares one body and one mesh (three variants kept for the session). Without worker threads (WebGL) bodies build
    /// on the spot.
    /// </summary>
    public sealed partial class CreatureDirector
    {
        /// <summary>One body being built (or built) for a creature or a planned group member.</summary>
        private sealed class BodyJob
        {
            public bool Humanoid;
            public string SpeciesId;
            /// <summary>The look seed the body was built from (copies of a boss share it).</summary>
            public int Seed;
            public CreatureLook Look;
            public Appearance HLook;
            public int Detail;
            /// <summary>The swarm's body: many creatures share it and its mesh.</summary>
            public bool Shared;
            /// <summary>A non-shared body already handed to its creature.</summary>
            public bool Claimed;
            public CreatureBody Body;
            public BodyData HData;
            public Mesh SharedMesh;
            public bool Started;
            public bool Done;
            public bool Failed;
            public bool Cancelled;
            public readonly List<Creature> Waiting = new List<Creature>();
        }

        private const int MaxInFlight = 3;
        private const int SwarmVariants = 3;
        private const int MaterializePerFrame = 2;

        private readonly object _gate = new object();
        private readonly List<BodyJob> _jobs = new List<BodyJob>();
        private readonly Dictionary<int, BodyJob> _swarmBodies = new Dictionary<int, BodyJob>();
        private int _inFlight;
        private bool _threads = true;

        private void InitBodies()
        {
            _threads = Application.platform != RuntimePlatform.WebGLPlayer && SystemInfo.processorCount > 1;
        }

        /// <summary>Build detail for a species: heroes for bosses and great beasts, crowd for mites, the ambient level otherwise.</summary>
        private static int DetailFor(CreatureSpecies sp)
        {
            if (sp.Boss) return 2;
            if (sp.Id == "glass_mite") return 0;
            if (sp.GreatBeast) return Settings.Graphics >= GraphicsPreset.High ? 2 : 1;
            return BodyFactory.AmbientDetail;
        }

        /// <summary>The look for a non-humanoid of a species (elites a little bigger and brighter).</summary>
        private static CreatureLook LookFor(CreatureSpecies sp, int seed, bool elite)
        {
            CreatureLook look = CreatureLooks.For(sp.LookId, seed);
            if (elite)
            {
                look = look.Clone();
                look.Scale *= 1.12f;
                look.GlowStrength *= 1.6f;
            }
            return look;
        }

        /// <summary>Orders a body for a future spawn (the ecology's groups); spawn with it once Done.</summary>
        private BodyJob RequestBody(CreatureSpecies sp, int seed, bool elite)
        {
            BodyJob j = new BodyJob();
            j.SpeciesId = sp.Id;
            j.Seed = seed;
            if (sp.IsHumanoid)
            {
                j.Humanoid = true;
                j.HLook = HumanoidLook(sp, seed);
                j.Detail = HumanoidDetail(sp);
            }
            else
            {
                j.Look = LookFor(sp, seed, elite);
                j.Detail = DetailFor(sp);
            }
            Enqueue(j);
            return j;
        }

        /// <summary>The shared body of a mite swarm (built once per variant, then reused for the session).</summary>
        private BodyJob SwarmBody(int seed)
        {
            int variant = Mathf.Abs(seed != 0 ? seed : _rng.Next()) % SwarmVariants;
            BodyJob j;
            if (_swarmBodies.TryGetValue(variant, out j) && !j.Failed) return j;
            CreatureSpecies sp = CreatureSpeciesDb.Get("glass_mite");
            j = new BodyJob();
            j.SpeciesId = "glass_mite";
            j.Shared = true;
            j.Look = CreatureLooks.For(sp != null ? sp.LookId : "glass_mite", 7001 + variant * 31);
            j.Detail = 0;
            _swarmBodies[variant] = j;
            Enqueue(j);
            return j;
        }

        /// <summary>A non-humanoid spawned without a body waits (hidden) for one; a shared swarm job just adds it to its list.</summary>
        private void WaitForBody(Creature c, CreatureSpecies sp, int seed, bool elite, BodyJob shared)
        {
            if (shared != null && !shared.Failed && !shared.Humanoid && !shared.Cancelled && (shared.Shared || (!shared.Claimed && shared.Waiting.Count == 0)))
            {
                // The swarm's body, or a body ordered ahead that is still building: wait for that one.
                shared.Waiting.Add(c);
                if (!_jobs.Contains(shared)) _jobs.Add(shared);
                return;
            }
            BodyJob j = new BodyJob();
            j.SpeciesId = sp.Id;
            j.Look = LookFor(sp, seed, elite);
            j.Detail = DetailFor(sp);
            j.Waiting.Add(c);
            Enqueue(j);
        }

        private void Enqueue(BodyJob j)
        {
            _jobs.Add(j);
            StartJobs();
        }

        /// <summary>Starts queued builds while worker slots are free (or builds right here without worker threads).</summary>
        private void StartJobs()
        {
            for (int i = 0; i < _jobs.Count; i++)
            {
                BodyJob j = _jobs[i];
                if (j.Started) continue;
                if (!_threads)
                {
                    j.Started = true;
                    Build(j);
                    continue;
                }
                lock (_gate)
                {
                    if (_inFlight >= MaxInFlight) return;
                    _inFlight++;
                }
                j.Started = true;
                bool queued = false;
                try { queued = ThreadPool.QueueUserWorkItem(Work, j); }
                catch (Exception) { queued = false; }
                if (!queued)
                {
                    lock (_gate) _inFlight = Mathf.Max(0, _inFlight - 1);
                    _threads = false;
                    Build(j);
                }
            }
        }

        private void Work(object state)
        {
            BodyJob j = (BodyJob)state;
            Build(j);
            lock (_gate) _inFlight = Mathf.Max(0, _inFlight - 1);
        }

        /// <summary>The pure build (any thread).</summary>
        private void Build(BodyJob j)
        {
            CreatureBody body = null;
            BodyData data = null;
            bool failed = false;
            try
            {
                if (j.Humanoid) data = BodyBuilder.Build(j.HLook, RigLayout.From(j.HLook), j.Detail);
                else body = CreatureBuilder.Build(j.Look, j.Detail);
            }
            catch (Exception)
            {
                failed = true;
            }
            lock (_gate)
            {
                j.Body = body;
                j.HData = data;
                j.Failed = failed || (j.Humanoid ? data == null : body == null);
                j.Done = true;
            }
        }

        private bool IsDone(BodyJob j)
        {
            lock (_gate) return j.Done;
        }

        /// <summary>Hands finished bodies to the creatures waiting for them (a couple of mesh uploads per frame at most).</summary>
        private void PollBodies()
        {
            if (_jobs.Count == 0) return;
            StartJobs();
            int budget = MaterializePerFrame;
            for (int i = 0; i < _jobs.Count && budget > 0; i++)
            {
                BodyJob j = _jobs[i];
                if (!IsDone(j)) continue;
                if (j.Waiting.Count == 0 && !j.Shared)
                {
                    // An ordered body (the ecology picks it up itself) or a cancelled one: nothing to hand out here.
                    if (j.Cancelled || j.Claimed) { _jobs.RemoveAt(i); i--; }
                    continue;
                }
                _jobs.RemoveAt(i);
                i--;
                if (j.Failed)
                {
                    if (j.Shared) _swarmBodies.Remove(KeyOf(j));
                    for (int k = 0; k < j.Waiting.Count; k++)
                    {
                        Creature c = j.Waiting[k];
                        if (c != null) { Log.Warn("Creatures: body build failed for " + j.SpeciesId); c.Despawn(false); }
                    }
                    j.Waiting.Clear();
                    continue;
                }
                if (j.Humanoid) continue;
                if (j.Shared && j.SharedMesh == null) j.SharedMesh = CreatureRig.MakeMesh(j.Body);
                for (int k = 0; k < j.Waiting.Count; k++)
                {
                    Creature c = j.Waiting[k];
                    if (c == null || c.IsDead) continue;
                    try { c.Materialize(j.Body, j.Shared ? j.SharedMesh : null); }
                    catch (Exception e)
                    {
                        Log.Exception("Creature materialize " + j.SpeciesId, e);
                        c.Despawn(false);
                    }
                }
                if (!j.Shared) j.Claimed = true;
                j.Waiting.Clear();
                budget--;
            }
        }

        private int KeyOf(BodyJob j)
        {
            foreach (KeyValuePair<int, BodyJob> kv in _swarmBodies) if (kv.Value == j) return kv.Key;
            return -1;
        }

        /// <summary>A body ordered ahead is ready to spawn with (the shared swarm mesh is uploaded on first use).</summary>
        private bool BodyReady(BodyJob j)
        {
            if (j == null || !IsDone(j)) return false;
            if (j.Failed) return true;
            if (j.Shared && j.SharedMesh == null && j.Body != null) j.SharedMesh = CreatureRig.MakeMesh(j.Body);
            return true;
        }

        /// <summary>
        /// Forgets every build in flight (their results are dropped). The swarm cache stays usable: a shared swarm body
        /// only loses its waiting list, so a swarm spawned while it is still building waits for it instead of ordering a
        /// body per mite.
        /// </summary>
        private void CancelBodyJobs()
        {
            for (int i = 0; i < _jobs.Count; i++)
            {
                if (!_jobs[i].Shared) _jobs[i].Cancelled = true;
                _jobs[i].Waiting.Clear();
            }
            _jobs.RemoveAll(j => !j.Shared);
        }

        private void DestroyBodyCaches()
        {
            foreach (KeyValuePair<int, BodyJob> kv in _swarmBodies)
                if (kv.Value.SharedMesh != null) Destroy(kv.Value.SharedMesh);
            _swarmBodies.Clear();
            _jobs.Clear();
        }

        /// <summary>Bodies still building (debug page).</summary>
        public int BodiesBuilding
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _jobs.Count; i++) if (!IsDone(_jobs[i])) n++;
                return n;
            }
        }
    }
}
