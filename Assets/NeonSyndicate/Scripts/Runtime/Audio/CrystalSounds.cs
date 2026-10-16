using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Synthesized crystal weapon audio (owner: models and audio). Loops are keyed by an int the caller owns (the shooter's
    /// instance id) so two loops never overlap and a stop always finds its loop: StartLoop on a key that already has a
    /// loop (even one still fading out) reuses it, StopLoop fades it out in 60 ms (no click) and plays the beam's release.
    /// One-shots go through a small pool of our own sources with a per-clip minimum gap, so a burst of identical calls
    /// cannot stack into a volume spike. The bank (about 12 s of audio, 100 to 200 ms of synthesis in total) is built a
    /// clip per frame by CrystalSoundsDriver once anything asks for it (Init, Prewarm on the first focus model, or the
    /// first sound); a clip needed before its turn is synthesized on the spot, so no sound is ever skipped and no frame
    /// pays for the whole bank. A recipe failure is logged once and that clip stays silent.
    /// </summary>
    public static partial class CrystalSounds
    {
        private sealed class LoopVoice
        {
            public AudioSource Src;
            public bool HasKey;
            public int Key;
            public bool Active;
            public bool Stopping;
            public Clip Clip;
            public float Vol;
            public float Target;
            public float Pitch = 1f;
            public float TargetPitch = 1f;
            public float FadeOut = 0.06f;
            public float Started;
        }

        private const int OneShotVoices = 10;
        private const int LoopVoices = 4;
        /// <summary>Base loop volume before intensity (Settings.SfxVolume applies on top).</summary>
        private const float LoopVolume = 0.45f;

        private static AudioClip[] _clips;
        private static AudioSource[] _pool;
        private static LoopVoice[] _loops;
        private static int _next;
        private static float[] _lastPlay;
        private static GameObject _root;
        private static bool _failed;
        private static int _nextBuild;
        private static bool[] _clipFailed;

        public static void ResetStatics()
        {
            _clips = null;
            _pool = null;
            _loops = null;
            _next = 0;
            _lastPlay = null;
            _root = null;
            _failed = false;
            _nextBuild = 0;
            _clipFailed = null;
        }

        /// <summary>True once the sources exist (clips still being built are synthesized on demand).</summary>
        public static bool Ready { get { return _clips != null && _pool != null && _loops != null && _root != null; } }

        /// <summary>True once every clip of the bank has been synthesized.</summary>
        public static bool BankComplete { get { return _clips != null && _nextBuild >= _clips.Length; } }

        /// <summary>Creates the sources under parent and starts building the bank (optional: it also starts on first use).</summary>
        public static void Init(Transform parent)
        {
            Build(parent);
        }

        /// <summary>Starts building the sound bank if it is not started yet (called when the first focus model appears).</summary>
        public static void Prewarm()
        {
            EnsureReady();
        }

        /// <summary>The family's discharge (for Prism Thread a soft ignite, at most every 0.4 s; its sustain is the loop).</summary>
        public static void PlayShot(CrystalBehavior b, Vector3 at)
        {
            Clip c = ShotClip(b);
            switch (b)
            {
                case CrystalBehavior.PrismThread: Play(c, at, 0.45f, 1f, 0.6f, 90f, 0.4f, 0.02f); break;
                case CrystalBehavior.GyreCrescent: Play(c, at, 0.55f, 1f, 0.65f, 100f, 0.08f, 0.04f); break;
                case CrystalBehavior.Sunderheart: Play(c, at, 0.7f, 1f, 0.6f, 160f, 0.15f, 0.03f); break;
                case CrystalBehavior.MirageChoir: Play(c, at, 0.5f, 1f, 0.65f, 110f, 0.1f, 0.03f); break;
                case CrystalBehavior.Mirrorwake: Play(c, at, 0.6f, 1f, 0.6f, 140f, 0.1f, 0.02f); break;
                default: Play(c, at, 0.5f, 1f, 0.65f, 110f, 0.03f, 0.05f); break;
            }
        }

        public static void PlayChargeReady(CrystalBehavior b, Vector3 at)
        {
            float pitch = b == CrystalBehavior.Sunderheart ? 0.7f : (b == CrystalBehavior.Mirrorwake ? 1.22f : (b == CrystalBehavior.Shardwake ? 1f : 1.1f));
            Play(Clip.ChargeReady, at, 0.4f, pitch, 0.3f, 40f, 0.15f, 0f);
        }

        /// <summary>Starts (or keeps) the loop owned by key: the beam's sustain for Prism Thread, the charge hum otherwise.</summary>
        public static void StartLoop(int key, CrystalBehavior b, Vector3 at)
        {
            if (!EnsureReady()) return;
            Clip clip = LoopClip(b);
            LoopVoice v = FindKeyed(key);
            if (v != null)
            {
                // Same owner: revive or keep it, never a second loop for one key.
                v.Active = true;
                v.Stopping = false;
                v.Target = LoopVolume;
                if (v.Clip != clip)
                {
                    v.Clip = clip;
                    v.Src.clip = ClipOf(clip);
                    v.Vol = 0f;
                    v.Src.volume = 0f;
                    v.Src.Play();
                }
                if (v.Src != null) v.Src.transform.position = at;
                return;
            }
            v = FreeVoice();
            if (v == null || v.Src == null) return;
            v.HasKey = true;
            v.Key = key;
            v.Active = true;
            v.Stopping = false;
            v.Clip = clip;
            v.Vol = 0f;
            v.Target = LoopVolume;
            v.Pitch = v.TargetPitch = 1f;
            v.FadeOut = 0.06f;
            v.Started = Time.unscaledTime;
            v.Src.transform.position = at;
            v.Src.clip = ClipOf(clip);
            v.Src.volume = 0f;
            v.Src.pitch = 1f;
            v.Src.Play();
            if (clip == Clip.BeamLoop) Play(Clip.ThreadIgnite, at, 0.45f, 1f, 0.6f, 90f, 0.05f, 0.02f);
        }

        /// <summary>Moves the loop and sets its intensity 0 to 1 (louder and higher with heat or charge).</summary>
        public static void UpdateLoop(int key, Vector3 at, float intensity)
        {
            if (!Ready) return;
            LoopVoice v = FindKeyed(key);
            if (v == null || !v.Active) return;
            float k = Mathf.Clamp01(intensity);
            if (v.Src != null) v.Src.transform.position = at;
            v.Target = LoopVolume * (0.75f + 0.35f * k);
            v.TargetPitch = v.Clip == Clip.BeamLoop ? 0.95f + 0.17f * k : 0.9f + 0.45f * k;
        }

        /// <summary>Fades the key's loop out promptly (60 ms, no click); the beam also plays its cooling release.</summary>
        public static void StopLoop(int key)
        {
            if (!Ready) return;
            LoopVoice v = FindKeyed(key);
            if (v == null || !v.Active) return;
            v.Active = false;
            v.Stopping = true;
            v.Target = 0f;
            v.FadeOut = 0.06f;
            if (v.Clip == Clip.BeamLoop && v.Src != null) Play(Clip.ThreadRelease, v.Src.transform.position, 0.5f, 1f, 0.6f, 90f, 0.05f, 0.02f);
        }

        /// <summary>The impact for what a hit struck (each kind at most every 35 to 50 ms, so beam ticks cannot stack).</summary>
        public static void PlayImpact(CrystalImpact kind, Vector3 at)
        {
            Clip c = ImpactClip(kind);
            switch (kind)
            {
                case CrystalImpact.Armor: Play(c, at, 0.5f, 1f, 1f, 70f, 0.035f, 0.06f); break;
                case CrystalImpact.WeakPoint: Play(c, at, 0.6f, 1f, 1f, 80f, 0.05f, 0.04f); break;
                case CrystalImpact.Crystal: Play(c, at, 0.5f, 1f, 1f, 70f, 0.035f, 0.06f); break;
                case CrystalImpact.Flesh: Play(c, at, 0.4f, 1f, 1f, 50f, 0.035f, 0.08f); break;
                case CrystalImpact.Air: Play(c, at, 0.3f, 1f, 1f, 40f, 0.05f, 0.08f); break;
                default: Play(c, at, 0.45f, 1f, 1f, 60f, 0.035f, 0.08f); break;
            }
        }

        public static void PlayVent(CrystalBehavior b, Vector3 at)
        {
            float pitch = b == CrystalBehavior.Sunderheart ? 0.8f : (b == CrystalBehavior.Shardwake ? 1.1f : (b == CrystalBehavior.Mirrorwake ? 1.05f : 0.95f));
            Play(Clip.Vent, at, 0.5f, pitch, 0.6f, 45f, 0.3f, 0f);
        }

        public static void PlayDenied(Vector3 at)
        {
            Play(Clip.Denied, at, 0.45f, 1f, 0.2f, 30f, 0.12f, 0f);
        }

        public static void PlayReward(Vector3 at)
        {
            Play(Clip.Reward, at, 0.6f, 1f, 0.2f, 200f, 0.5f, 0f);
        }

        /// <summary>Silences every loop (pause, title, load, death): a 30 ms fade, then the sources stop.</summary>
        public static void StopAll()
        {
            if (_loops == null) return;
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v == null) continue;
                v.Active = false;
                v.Target = 0f;
                v.FadeOut = 0.03f;
                v.Stopping = v.Src != null && v.Src.isPlaying;
                if (!v.Stopping) v.HasKey = false;
            }
        }

        /// <summary>How many loops are audible or fading (debug and tests of the one-loop-per-key rule).</summary>
        public static int LiveLoops
        {
            get
            {
                if (_loops == null) return 0;
                int n = 0;
                for (int i = 0; i < _loops.Length; i++) if (_loops[i] != null && (_loops[i].Active || _loops[i].Stopping)) n++;
                return n;
            }
        }

        // ------------------------------------------------------------------ internals

        /// <summary>
        /// Advances loop fades and pitch glides and synthesizes the next missing clip of the bank; called every frame by
        /// CrystalSoundsDriver (real time).
        /// </summary>
        internal static void Tick(float dt)
        {
            if (_clips != null && _nextBuild < _clips.Length)
            {
                ClipOf((Clip)_nextBuild);
                _nextBuild++;
            }
            if (_loops == null) return;
            float slow = Time.timeScale < 0.8f && Time.timeScale > 0f ? 0.75f : 1f;
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v == null || v.Src == null) continue;
                if (!v.Active && !v.Stopping) continue;
                float rate = v.Target >= v.Vol ? LoopVolume / 0.05f : LoopVolume / Mathf.Max(0.01f, v.FadeOut);
                v.Vol = Mathf.MoveTowards(v.Vol, v.Target, rate * dt);
                v.Pitch = Mathf.MoveTowards(v.Pitch, v.TargetPitch, dt * 1.5f);
                v.Src.volume = v.Vol * Settings.SfxVolume;
                v.Src.pitch = v.Pitch * slow;
                if (v.Stopping && v.Vol <= 0.0001f)
                {
                    v.Src.Stop();
                    v.Stopping = false;
                    v.HasKey = false;
                }
            }
        }

        private static LoopVoice FindKeyed(int key)
        {
            if (_loops == null) return null;
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v != null && v.HasKey && v.Key == key && (v.Active || v.Stopping)) return v;
            }
            return null;
        }

        /// <summary>A silent voice, else the one closest to silence among those fading out, else the oldest active one.</summary>
        private static LoopVoice FreeVoice()
        {
            LoopVoice best = null;
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v != null && !v.Active && !v.Stopping) return v;
            }
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v != null && v.Stopping && (best == null || v.Vol < best.Vol)) best = v;
            }
            if (best != null) return best;
            for (int i = 0; i < _loops.Length; i++)
            {
                LoopVoice v = _loops[i];
                if (v != null && (best == null || v.Started < best.Started)) best = v;
            }
            return best;
        }

        private static void Play(Clip c, Vector3 at, float volume, float pitch, float spatial, float maxDistance, float minGap, float pitchJitter)
        {
            if (!EnsureReady()) return;
            float now = Time.unscaledTime;
            int ci = (int)c;
            if (now - _lastPlay[ci] < minGap) return;
            _lastPlay[ci] = now;
            AudioSource s = null;
            for (int i = 0; i < _pool.Length; i++)
            {
                AudioSource o = _pool[(_next + i) % _pool.Length];
                if (o != null && !o.isPlaying)
                {
                    s = o;
                    _next = (_next + i + 1) % _pool.Length;
                    break;
                }
            }
            if (s == null)
            {
                s = _pool[_next];
                _next = (_next + 1) % _pool.Length;
            }
            if (s == null) return;
            AudioClip clip = ClipOf(c);
            if (clip == null) return;
            float jitter = pitchJitter > 0f ? UnityEngine.Random.Range(-pitchJitter, pitchJitter) : 0f;
            s.transform.position = at;
            s.clip = clip;
            s.volume = Mathf.Clamp(volume, 0f, 0.8f) * Settings.SfxVolume;
            s.pitch = (pitch + jitter) * (Time.timeScale < 0.8f && Time.timeScale > 0f ? 0.75f : 1f);
            s.spatialBlend = spatial;
            s.minDistance = 3f;
            s.maxDistance = maxDistance;
            s.Play();
        }

        private static bool EnsureReady()
        {
            if (Ready) return true;
            if (_failed) return false;
            Build(null);
            return Ready;
        }

        /// <summary>Allocates the bank (empty clips are filled a frame at a time) and (re)creates the sources if their object is gone.</summary>
        private static void Build(Transform parent)
        {
            try
            {
                if (_clips == null)
                {
                    int n = (int)Clip.Count;
                    _clips = new AudioClip[n];
                    _clipFailed = new bool[n];
                    _lastPlay = new float[n];
                    for (int i = 0; i < n; i++) _lastPlay[i] = -10f;
                    _nextBuild = 0;
                }
                if (_root == null)
                {
                    _root = new GameObject("Crystal Sounds");
                    if (parent != null) _root.transform.SetParent(parent, false);
                    _root.AddComponent<CrystalSoundsDriver>();
                    _pool = new AudioSource[OneShotVoices];
                    for (int i = 0; i < _pool.Length; i++) _pool[i] = NewSource("Crystal Sound " + i, false);
                    _loops = new LoopVoice[LoopVoices];
                    for (int i = 0; i < _loops.Length; i++) _loops[i] = new LoopVoice { Src = NewSource("Crystal Loop " + i, true) };
                    _next = 0;
                }
                else if (parent != null && _root.transform.parent == null)
                {
                    _root.transform.SetParent(parent, false);
                }
            }
            catch (Exception e)
            {
                _failed = true;
                Log.Exception("Crystal sounds", e);
            }
        }

        /// <summary>The clip, synthesized now if the bank has not reached it yet (null only if its recipe failed).</summary>
        private static AudioClip ClipOf(Clip c)
        {
            int i = (int)c;
            if (_clips == null || i < 0 || i >= _clips.Length) return null;
            if (_clips[i] != null) return _clips[i];
            if (_clipFailed != null && _clipFailed[i]) return null;
            try
            {
                float[] data = Make(c);
                AudioClip ac = AudioClip.Create("Crystal " + c, data.Length, 1, AudioSynth.Rate, false);
                ac.SetData(data, 0);
                _clips[i] = ac;
            }
            catch (Exception e)
            {
                Log.Exception("Crystal sound " + c, e);
                _clips[i] = null;
                if (_clipFailed != null) _clipFailed[i] = true;
            }
            return _clips[i];
        }

        private static AudioSource NewSource(string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.dopplerLevel = 0f;
            s.spatialBlend = loop ? 0.75f : 0.7f;
            s.minDistance = 3f;
            s.maxDistance = loop ? 70f : 110f;
            s.volume = 0f;
            return s;
        }
    }
}
