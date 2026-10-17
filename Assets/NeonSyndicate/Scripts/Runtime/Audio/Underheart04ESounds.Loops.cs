using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Keyed 04E loops. Each StartLoop makes a logical loop with its own key (never reused in a session); up to 48 may be
    /// alive, and each frame the eight most audible (level times distance attenuation, with a bonus for loops already
    /// sounding so voices do not flap) get one of the eight real voices. A voice fades in over 0.12 s, glides its pitch,
    /// and fades out before it is reused, so nothing clicks; a stopped key is gone at once even while its voice fades.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        private sealed class LoopSlot
        {
            public int Key;
            public UhLoop Id;
            public Vector3 Pos;
            public Transform Follow;
            public bool HasFollow;
            public float Volume = 1f;
            public float Pitch = 1f;
            /// <summary>Index of the voice playing it, -1 while it is out of earshot or outranked.</summary>
            public int Voice = -1;
        }

        private sealed class LoopVoice
        {
            public AudioSource Src;
            /// <summary>The loop key it plays, 0 when free.</summary>
            public int Key;
            /// <summary>Fade envelope 0 to 1.</summary>
            public float Level;
            /// <summary>Smoothed loop level (spec level times the caller's volume).</summary>
            public float Gain;
            public float PitchNow = 1f;
            public bool Releasing;
            public float Fade = LoopStopFade;
        }

        private static readonly List<LoopSlot> _loops = new List<LoopSlot>();
        private static readonly Dictionary<int, LoopSlot> _loopsByKey = new Dictionary<int, LoopSlot>();
        private static LoopVoice[] _voices;
        private static int _nextKey = 1;
        private static int _loopRefused;
        private static readonly LoopSlot[] _pick = new LoopSlot[LoopVoices];
        private static readonly float[] _pickScore = new float[LoopVoices];
        private static int _picked;
        private static bool _musicForceRelease;

        private static int StartLoopInternal(UhLoop loop, Vector3 pos, Transform follow, float volume)
        {
            int li = (int)loop;
            if (li < 0 || li >= UhAudioBank.LoopCount) return 0;
            if (!MathUtil.IsFinite(pos)) return 0;
            if (!EnsureReady()) return 0;
            if (_loops.Count >= MaxLoops)
            {
                _loopRefused++;
                if (_loopRefused == 1) Log.Warn("Underheart 04E audio: " + MaxLoops + " loops are already alive; " + loop + " was not started. A caller is probably not stopping its loops.");
                return 0;
            }
            int key = _nextKey;
            while (key == 0 || _loopsByKey.ContainsKey(key)) key = key >= int.MaxValue - 1 ? 1 : key + 1;
            _nextKey = key >= int.MaxValue - 1 ? 1 : key + 1;
            LoopSlot s = new LoopSlot
            {
                Key = key,
                Id = loop,
                Pos = pos,
                Follow = follow,
                HasFollow = follow != null,
                Volume = Mathf.Clamp01(float.IsNaN(volume) ? 1f : volume),
                Pitch = 1f,
                Voice = -1
            };
            _loops.Add(s);
            _loopsByKey[key] = s;
            return key;
        }

        private static void UpdateLoopInternal(int key, Vector3 pos, float volume, float pitch)
        {
            LoopSlot s;
            if (key == 0 || !_loopsByKey.TryGetValue(key, out s)) return;
            if (MathUtil.IsFinite(pos))
            {
                s.Pos = pos;
                s.Follow = null;
                s.HasFollow = false;
            }
            if (!float.IsNaN(volume)) s.Volume = Mathf.Clamp01(volume);
            if (!float.IsNaN(pitch)) s.Pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        }

        private static void StopLoopInternal(int key, float fade)
        {
            LoopSlot s;
            if (key == 0 || !_loopsByKey.TryGetValue(key, out s)) return;
            _loopsByKey.Remove(key);
            _loops.Remove(s);
            ReleaseVoiceOf(s, fade);
        }

        private static void ReleaseVoiceOf(LoopSlot s, float fade)
        {
            if (_voices == null || s.Voice < 0 || s.Voice >= _voices.Length) return;
            LoopVoice v = _voices[s.Voice];
            if (v.Key == s.Key)
            {
                v.Releasing = true;
                v.Fade = Mathf.Max(0.01f, fade);
            }
            s.Voice = -1;
        }

        private static void StopAllInternal()
        {
            StopAllInternal(0.05f);
        }

        private static void StopAllInternal(float fade)
        {
            for (int i = 0; i < _loops.Count; i++) ReleaseVoiceOf(_loops[i], fade);
            _loops.Clear();
            _loopsByKey.Clear();
            if (_voices != null)
                for (int i = 0; i < _voices.Length; i++)
                    if (_voices[i].Key != 0 && !_voices[i].Releasing)
                    {
                        _voices[i].Releasing = true;
                        _voices[i].Fade = fade;
                    }
            _musicForceRelease = true;
        }

        private static bool IsPicked(LoopSlot s)
        {
            for (int k = 0; k < _picked; k++) if (_pick[k] == s) return true;
            return false;
        }

        /// <summary>Keeps the eight most audible loops in a small sorted array (no allocation).</summary>
        private static void OfferPick(LoopSlot s, float score)
        {
            int at = _picked;
            while (at > 0 && _pickScore[at - 1] < score) at--;
            if (at >= _pick.Length) return;
            int last = Mathf.Min(_picked, _pick.Length - 1);
            for (int k = last; k > at; k--)
            {
                _pick[k] = _pick[k - 1];
                _pickScore[k] = _pickScore[k - 1];
            }
            _pick[at] = s;
            _pickScore[at] = score;
            if (_picked < _pick.Length) _picked++;
        }

        private static int FreeVoice()
        {
            for (int i = 0; i < _voices.Length; i++) if (_voices[i].Key == 0 && _voices[i].Src != null) return i;
            return -1;
        }

        private static void TickLoops(float dt)
        {
            if (_voices == null) return;
            Transform l = Listener();
            Vector3 lp = l != null ? l.position : Game.PlayerPosition;
            float sfx = Settings.SfxVolume;
            float slow = Time.timeScale < 0.8f && Time.timeScale > 0f ? 0.75f : 1f;

            // Followed transforms: move with them, stop when they are destroyed.
            for (int i = _loops.Count - 1; i >= 0; i--)
            {
                LoopSlot s = _loops[i];
                if (!s.HasFollow) continue;
                if (s.Follow == null)
                {
                    StopLoopInternal(s.Key, LoopStopFade);
                    continue;
                }
                s.Pos = s.Follow.position;
            }

            // Rank by audibility.
            _picked = 0;
            for (int i = 0; i < _pick.Length; i++) _pick[i] = null;
            for (int i = 0; i < _loops.Count; i++)
            {
                LoopSlot s = _loops[i];
                float vol, min, max;
                UhAudioRules.LoopSpec(s.Id, out vol, out min, out max);
                float score = vol * s.Volume * UhAudioRules.Attenuation(Vector3.Distance(s.Pos, lp), min, max);
                if (score <= 0.0005f) continue;
                if (s.Voice >= 0) score *= 1.3f;
                OfferPick(s, score);
            }

            // Voices whose loop was outranked, went out of earshot or stopped fade out.
            for (int vi = 0; vi < _voices.Length; vi++)
            {
                LoopVoice v = _voices[vi];
                if (v.Key == 0 || v.Releasing) continue;
                LoopSlot owner;
                bool alive = _loopsByKey.TryGetValue(v.Key, out owner);
                if (alive && IsPicked(owner)) continue;
                v.Releasing = true;
                v.Fade = alive ? 0.15f : LoopStopFade;
                if (alive) owner.Voice = -1;
            }

            // Picked loops without a voice take a free one.
            for (int k = 0; k < _picked; k++)
            {
                LoopSlot s = _pick[k];
                if (s == null || s.Voice >= 0) continue;
                int fv = FreeVoice();
                if (fv < 0) break;
                AudioClip clip = ClipAt(UhAudioBank.Loop(s.Id), true);
                if (clip == null) continue;
                float vol, min, max;
                UhAudioRules.LoopSpec(s.Id, out vol, out min, out max);
                LoopVoice v = _voices[fv];
                v.Key = s.Key;
                v.Releasing = false;
                v.Level = 0f;
                v.Gain = vol * s.Volume;
                v.PitchNow = s.Pitch;
                v.Src.clip = clip;
                v.Src.minDistance = min;
                v.Src.maxDistance = max;
                v.Src.transform.position = s.Pos;
                v.Src.volume = 0f;
                v.Src.pitch = s.Pitch * slow;
                v.Src.Play();
                // Start somewhere inside the loop so two identical emitters never play in lockstep (silent until the fade-in).
                v.Src.time = UnityEngine.Random.Range(0f, clip.length * 0.9f);
                s.Voice = fv;
            }

            // Advance fades, glides and positions.
            for (int vi = 0; vi < _voices.Length; vi++)
            {
                LoopVoice v = _voices[vi];
                if (v.Key == 0 || v.Src == null) continue;
                if (v.Releasing)
                {
                    v.Level -= dt / Mathf.Max(0.01f, v.Fade);
                    if (v.Level <= 0f)
                    {
                        v.Level = 0f;
                        v.Src.volume = 0f;
                        v.Src.Stop();
                        v.Key = 0;
                        v.Releasing = false;
                        continue;
                    }
                    v.Src.volume = v.Level * v.Gain * sfx;
                    continue;
                }
                LoopSlot s;
                if (!_loopsByKey.TryGetValue(v.Key, out s)) continue;
                float vol, min, max;
                UhAudioRules.LoopSpec(s.Id, out vol, out min, out max);
                v.Gain = Mathf.MoveTowards(v.Gain, vol * s.Volume, dt * 3f);
                v.Level = Mathf.Min(1f, v.Level + dt / 0.12f);
                v.PitchNow = Mathf.MoveTowards(v.PitchNow, s.Pitch, dt * 2f);
                v.Src.transform.position = s.Pos;
                v.Src.pitch = v.PitchNow * slow;
                v.Src.volume = v.Level * v.Gain * sfx;
            }
        }
    }
}
