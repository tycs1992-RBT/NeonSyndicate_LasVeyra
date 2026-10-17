using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E audio runtime: the background-built sound bank, one-shot playback through AudioManager's pools, the
    /// sources this module owns (loop voices, zone beds, detail voices, the score), and the per-frame tick driven by
    /// Underheart04ESoundsDriver in real time.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        /// <summary>
        /// One session's sound bank. A worker thread synthesizes every clip (pure maths) in build order into Pending;
        /// the main thread turns finished buffers into AudioClips a few per frame. ResetStatics cancels and drops it, so a
        /// worker still running from an earlier session only ever writes into its own orphaned bank.
        /// </summary>
        private sealed class Bank
        {
            public readonly float[][] Pending = new float[UhAudioBank.Total][];
            public readonly bool[] WorkerFailed = new bool[UhAudioBank.Total];
            public readonly string[] WorkerError = new string[UhAudioBank.Total];
            /// <summary>Set (under Lock) once the main thread has a clip, so the worker skips it.</summary>
            public readonly bool[] Done = new bool[UhAudioBank.Total];
            public readonly AudioClip[] Clips = new AudioClip[UhAudioBank.Total];
            public readonly bool[] Failed = new bool[UhAudioBank.Total];
            public readonly int[] Order = UhAudioBank.BuildOrder();
            public readonly object Lock = new object();
            public volatile bool Cancelled;
            public volatile bool WorkerDone;
            public bool WorkerStarted;
            public int Cursor;
            /// <summary>Real time of the last clip the worker delivered (or of the start): a stalled worker falls back to the main thread.</summary>
            public float LastProgress;
            public int Uploaded;
            public int FailedCount;
            public bool Reported;
        }

        private const int LoopVoices = 8;
        private const int DetailVoices = 3;
        private const int MaxLoops = 48;
        private const float LoopStopFade = 0.08f;
        /// <summary>Samples uploaded per frame at most (a 6 s bed is about 132 000).</summary>
        private const int UploadBudget = 200000;

        private static Bank _bank;
        private static GameObject _root;
        private static bool _failed;
        private static bool _pageRegistered;
        private static AudioSource[] _beds;
        private static float[] _bedLevel;
        private static AudioSource[] _details;
        private static int _detailNext;
        private static AudioSource[] _music;
        private static float _musicLevel;
        private static float _driveLevel;
        private static bool _musicPlaying;
        private static readonly UhAudioRateLimiter _limiter = new UhAudioRateLimiter(UhAudioBank.SfxCount);
        private static UhAudioZoneBlend _blend = new UhAudioZoneBlend();
        private static UhAudioDetailScheduler _scheduler = new UhAudioDetailScheduler(52501);
        private static UhZone _requested = UhZone.Surface;
        private static float _requestedAt = -1e9f;
        private static UhZone _zone = UhZone.Surface;
        private static string _lastRegion = "";
        private static float _regionChangedAt = -1e9f;
        private static GameState _lastState = GameState.Loading;
        private static float _playingSince = -1e9f;
        /// <summary>Debug page override: -1 follows the game, otherwise forces that zone.</summary>
        private static int _debugZone = -1;
        private static int _played;
        private static int _limited;
        private static int _tickErrors;

        private static float Now()
        {
            return Time.unscaledTime;
        }

        /// <summary>Creates the bank and the sources if missing (also rebuilds sources whose object was destroyed).</summary>
        private static bool EnsureReady()
        {
            if (_failed) return false;
            try
            {
                if (_bank == null)
                {
                    _bank = new Bank();
                    _bank.LastProgress = Now();
                    StartWorker(_bank);
                    ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Loaded, "Synthesizing " + UhAudioBank.Total + " sounds in the background.");
                    ModuleStatus.SetCount(ModuleId, 0, "of " + UhAudioBank.Total + " sounds ready");
                }
                if (_root == null) BuildSources();
                if (!_pageRegistered)
                {
                    _pageRegistered = true;
                    DebugPages.Register(new MenuPage { Title = "Audio 04E", Order = 61, Draw = DrawDebugPage });
                }
                return true;
            }
            catch (Exception e)
            {
                _failed = true;
                Log.Exception("Underheart sounds", e);
                ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Failed, "The 04E sound sources could not be created: " + e.Message,
                    "Everything else still plays; 04E cues stay silent. Send the Console error along.");
                return false;
            }
        }

        private static void BuildSources()
        {
            _root = new GameObject("Underheart Sounds 04E");
            if (Game.Audio != null) _root.transform.SetParent(Game.Audio.transform, false);
            _root.AddComponent<Underheart04ESoundsDriver>();
            _voices = new LoopVoice[LoopVoices];
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource s = NewSource("Loop " + i, true, 1f);
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0f;
                _voices[i] = new LoopVoice { Src = s };
            }
            _beds = new AudioSource[UhAudioRules.ZoneCount];
            _bedLevel = new float[UhAudioRules.ZoneCount];
            for (int z = 1; z < _beds.Length; z++) _beds[z] = NewSource("Bed " + (UhZone)z, true, 0f);
            _details = new AudioSource[DetailVoices];
            for (int i = 0; i < _details.Length; i++)
            {
                AudioSource s = NewSource("Detail " + i, false, 0.85f);
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = 3f;
                s.maxDistance = 45f;
                s.dopplerLevel = 0f;
                _details[i] = s;
            }
            _music = new AudioSource[(int)UhAudioMusic.Count];
            for (int i = 0; i < _music.Length; i++) _music[i] = NewSource("Score " + (UhAudioMusic)i, true, 0f);
            _detailNext = 0;
            _musicPlaying = false;
            _musicLevel = 0f;
            _driveLevel = 0f;
            // Voices whose loops were alive on the old object start again on the new ones.
            for (int i = 0; i < _loops.Count; i++) _loops[i].Voice = -1;
        }

        private static AudioSource NewSource(string name, bool loop, float spatial)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = spatial;
            s.volume = 0f;
            return s;
        }

        // ------------------------------------------------------------------ bank

        private static void StartWorker(Bank bank)
        {
            try
            {
                bank.WorkerStarted = ThreadPool.QueueUserWorkItem(_ => RunWorker(bank));
            }
            catch (Exception e)
            {
                bank.WorkerStarted = false;
                Log.Exception("Underheart sound worker (building on the main thread instead)", e);
            }
        }

        /// <summary>Worker thread: synthesizes every clip in build order. Pure maths only; never touches Unity objects or Log.</summary>
        private static void RunWorker(Bank bank)
        {
            try
            {
                for (int k = 0; k < bank.Order.Length; k++)
                {
                    if (bank.Cancelled) break;
                    int i = bank.Order[k];
                    lock (bank.Lock)
                    {
                        if (bank.Done[i]) continue;
                    }
                    float[] data = null;
                    string error = null;
                    try { data = UhAudioBank.Make(i); }
                    catch (Exception e) { error = e.GetType().Name + ": " + e.Message; }
                    lock (bank.Lock)
                    {
                        if (bank.Done[i]) continue;
                        if (data != null) bank.Pending[i] = data;
                        else
                        {
                            bank.WorkerFailed[i] = true;
                            bank.WorkerError[i] = error ?? "no data";
                        }
                    }
                }
            }
            finally
            {
                bank.WorkerDone = true;
            }
        }

        /// <summary>
        /// The clip of a bank index, uploading a finished buffer now if the worker has one. synthNow allows making a missing
        /// clip on the spot (one-shots and loops: a few milliseconds); beds and the score wait for the worker instead.
        /// </summary>
        private static AudioClip ClipAt(int index, bool synthNow)
        {
            Bank bank = _bank;
            if (bank == null || index < 0 || index >= UhAudioBank.Total) return null;
            if (bank.Clips[index] != null) return bank.Clips[index];
            if (bank.Failed[index]) return null;
            float[] data;
            bool workerFailed;
            string error;
            lock (bank.Lock)
            {
                data = bank.Pending[index];
                bank.Pending[index] = null;
                workerFailed = bank.WorkerFailed[index];
                error = bank.WorkerError[index];
            }
            if (data == null && workerFailed)
            {
                MarkFailed(bank, index, error);
                return null;
            }
            if (data == null)
            {
                if (!synthNow) return null;
                try { data = UhAudioBank.Make(index); }
                catch (Exception e)
                {
                    MarkFailed(bank, index, e.Message);
                    return null;
                }
            }
            Upload(bank, index, data);
            return bank.Clips[index];
        }

        private static void Upload(Bank bank, int index, float[] data)
        {
            try
            {
                AudioClip clip = AudioClip.Create("Underheart 04E " + UhAudioBank.Name(index), data.Length, 1, AudioSynth.Rate, false);
                if (!clip.SetData(data, 0)) throw new InvalidOperationException("sample upload failed");
                bank.Clips[index] = clip;
                bank.Uploaded++;
                lock (bank.Lock)
                {
                    bank.Done[index] = true;
                    bank.Pending[index] = null;
                }
            }
            catch (Exception e)
            {
                MarkFailed(bank, index, e.Message);
            }
        }

        private static void MarkFailed(Bank bank, int index, string error)
        {
            if (bank.Failed[index]) return;
            bank.Failed[index] = true;
            bank.FailedCount++;
            lock (bank.Lock)
            {
                bank.Done[index] = true;
                bank.Pending[index] = null;
            }
            Log.Warn("Underheart 04E sound " + UhAudioBank.Name(index) + " could not be made and stays silent: " + (error ?? "unknown"));
        }

        /// <summary>Uploads finished clips in build order within the per-frame budget; without a worker, makes one clip per frame here.</summary>
        private static void PumpBank(Bank bank)
        {
            if (bank.Reported) return;
            int before = bank.Uploaded;
            int budget = UploadBudget;
            int uploads = 0;
            while (bank.Cursor < bank.Order.Length && uploads < 4 && budget > 0)
            {
                int i = bank.Order[bank.Cursor];
                if (bank.Clips[i] != null || bank.Failed[i])
                {
                    lock (bank.Lock) bank.Pending[i] = null;
                    bank.Cursor++;
                    continue;
                }
                float[] data;
                bool workerFailed;
                string error;
                lock (bank.Lock)
                {
                    data = bank.Pending[i];
                    bank.Pending[i] = null;
                    workerFailed = bank.WorkerFailed[i];
                    error = bank.WorkerError[i];
                }
                if (data != null) bank.LastProgress = Now();
                if (data == null && !workerFailed)
                {
                    // The worker builds in the same order: wait for it while it runs (unless it has stalled for 4 s).
                    if (bank.WorkerStarted && !bank.WorkerDone && Now() - bank.LastProgress < 4f) break;
                    try { data = UhAudioBank.Make(i); }
                    catch (Exception e) { error = e.Message; }
                    uploads = 4;
                }
                if (data == null)
                {
                    MarkFailed(bank, i, error);
                    bank.Cursor++;
                    continue;
                }
                Upload(bank, i, data);
                budget -= data.Length;
                uploads++;
                bank.Cursor++;
            }
            if (bank.Uploaded != before) ModuleStatus.SetCount(ModuleId, bank.Uploaded, "of " + UhAudioBank.Total + " sounds ready");
            if (bank.Cursor >= bank.Order.Length)
            {
                bank.Reported = true;
                if (bank.FailedCount == 0) ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Loaded, "");
                else ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Partial, bank.FailedCount + " of " + UhAudioBank.Total + " sounds could not be made and stay silent.",
                    "Everything else plays. Send the Console warnings along.");
                ModuleStatus.SetCount(ModuleId, bank.Uploaded, "sounds synthesized");
            }
        }

        // ------------------------------------------------------------------ one-shots

        private static Transform Listener()
        {
            return Game.Cam != null ? Game.Cam.transform : null;
        }

        private static void PlayInternal(UhSfx id, Vector3 pos, bool positional, float volume, float pitch)
        {
            int ii = (int)id;
            if (ii < 0 || ii >= UhAudioBank.SfxCount) return;
            if (!EnsureReady()) return;
            AudioManager am = Game.Audio;
            if (am == null || !am.Ready) return;
            UhAudioCue cue = UhAudioRules.Cue(id);
            if (positional)
            {
                if (!MathUtil.IsFinite(pos)) return;
                Transform l = Listener();
                // Out of earshot: no voice taken and no rate-limit slot used.
                if (l != null && (l.position - pos).sqrMagnitude > cue.MaxDistance * cue.MaxDistance * 1.21f) return;
            }
            if (!_limiter.TryAccept(ii, Now(), pos, positional, cue))
            {
                _limited++;
                return;
            }
            AudioClip clip = ClipAt(UhAudioBank.Sfx(id), true);
            if (clip == null) return;
            float vol = cue.Volume * Mathf.Clamp01(float.IsNaN(volume) ? 1f : volume);
            float p = Mathf.Clamp(float.IsNaN(pitch) ? 1f : pitch, 0.25f, 3f);
            if (cue.PitchJitter > 0f) p *= 1f + UnityEngine.Random.Range(-cue.PitchJitter, cue.PitchJitter);
            if (positional) am.PlayClipAt(clip, pos, vol, p, cue.MinDistance, cue.MaxDistance);
            else am.PlayClip2D(clip, vol, p);
            _played++;
        }

        // ------------------------------------------------------------------ tick

        /// <summary>Called every frame by the driver (real time, clamped).</summary>
        internal static void Tick(float dt)
        {
            Bank bank = _bank;
            if (bank == null || _root == null) return;
            try { PumpBank(bank); }
            catch (Exception e) { TickError("bank", e); }
            float now = Now();
            try { TrackSession(now); }
            catch (Exception e) { TickError("session", e); }
            try { TickZone(now, dt); }
            catch (Exception e) { TickError("zone", e); }
            try { TickLoops(dt); }
            catch (Exception e) { TickError("loops", e); }
            try { TickMusic(dt); }
            catch (Exception e) { TickError("score", e); }
        }

        private static void TickError(string stage, Exception e)
        {
            _tickErrors++;
            Log.Exception("Underheart sounds " + stage, e);
        }

        /// <summary>
        /// Session edges: entering Loading (new game, load, title) stops every loop and the score at once and snaps the
        /// ambience to the surface, so nothing from the last session keeps playing; the first zone change in the first
        /// second of play snaps instead of fading (an underground load starts in its own ambience).
        /// </summary>
        private static void TrackSession(float now)
        {
            GameState s = Game.State;
            if (s == _lastState) return;
            if (s == GameState.Loading || s == GameState.Title)
            {
                StopAllInternal(0.03f);
                _blend.Snap(UhZone.Surface);
                _zone = UhZone.Surface;
                _requested = UhZone.Surface;
                _requestedAt = -1e9f;
                _lastRegion = "";
                _regionChangedAt = -1e9f;
                _scheduler.Reset();
                _limiter.Reset();
                _debugZone = -1;
                _debugMusic = -1;
            }
            if (s == GameState.Playing && (_lastState == GameState.Loading || _lastState == GameState.Title)) _playingSince = now;
            _lastState = s;
        }

        private static bool SessionActive()
        {
            GameState s = Game.State;
            return s != GameState.Loading && s != GameState.Title && Game.Player != null;
        }
    }
}
