using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E audio (Underheart04ESounds and its pure rules): every clip the bank can make (55 cues, 7 loops, 7 zone beds,
    /// 16 details, 2 score layers) is finite, audible, distinct and never clips; one-shots start and end at silence;
    /// loops wrap without a click or a level jump; the per-id rate limit drops stacked plays (one collider hit, one
    /// sound) while separate events still sound; the zone weights always sum to 1, fade in the stated time and keep the
    /// street ambience exactly unchanged on the surface; the zone resolver, detail scheduler, loop table and score rules
    /// behave as documented. All engine-free (no clip, source or clock is touched).
    /// </summary>
    public class Audio04ETests
    {
        private static readonly Dictionary<int, float[]> Cache = new Dictionary<int, float[]>();

        private static float[] Clip(int index)
        {
            float[] b;
            if (!Cache.TryGetValue(index, out b))
            {
                b = UhAudioBank.Make(index);
                Cache[index] = b;
            }
            return b;
        }

        private static float Rms(float[] b, int start, int count)
        {
            double s = 0;
            int end = Mathf.Min(b.Length, start + count);
            for (int i = start; i < end; i++) s += b[i] * b[i];
            return (float)System.Math.Sqrt(s / System.Math.Max(1, end - start));
        }

        // ------------------------------------------------------------------ clips

        [Test]
        public void EveryClipIsFiniteAudibleAndNeverClips()
        {
            for (int i = 0; i < UhAudioBank.Total; i++)
            {
                string name = UhAudioBank.Name(i);
                float[] b = Clip(i);
                Assert.That(b, Is.Not.Null, name);
                Assert.That(b.Length, Is.InRange(AudioSynth.Rate / 10, AudioSynth.Rate * 7), name + " length");
                float peak = 0f;
                double energy = 0;
                for (int k = 0; k < b.Length; k++)
                {
                    float x = b[k];
                    Assert.That(float.IsNaN(x) || float.IsInfinity(x), Is.False, name + " sample " + k + " is finite");
                    peak = Mathf.Max(peak, Mathf.Abs(x));
                    energy += x * x;
                }
                float cap = UhAudioBank.Peak(i);
                Assert.That(cap, Is.LessThan(1f), name + " cap leaves headroom");
                Assert.That(peak, Is.LessThanOrEqualTo(cap + 1e-4f), name + " never exceeds its peak (never clips)");
                Assert.That(peak, Is.GreaterThan(cap * 0.95f), name + " is normalized (audible)");
                float rms = (float)System.Math.Sqrt(energy / b.Length);
                Assert.That(rms, Is.GreaterThan(cap * 0.04f), name + " carries real energy, not a lone click (rms " + rms + ")");
                if (!UhAudioBank.IsLooping(i))
                {
                    Assert.That(Mathf.Abs(b[0]), Is.LessThan(0.02f), name + " starts at silence");
                    Assert.That(Mathf.Abs(b[b.Length - 1]), Is.LessThan(0.02f), name + " ends at silence (no click)");
                }
            }
        }

        [Test]
        public void LoopsWrapWithoutAClickOrALevelJump()
        {
            int window = AudioSynth.Rate / 10;
            for (int i = 0; i < UhAudioBank.Total; i++)
            {
                if (!UhAudioBank.IsLooping(i)) continue;
                string name = UhAudioBank.Name(i);
                float[] b = Clip(i);
                float maxStep = 0f;
                for (int k = 1; k < b.Length; k++) maxStep = Mathf.Max(maxStep, Mathf.Abs(b[k] - b[k - 1]));
                float wrap = Mathf.Abs(b[0] - b[b.Length - 1]);
                Assert.That(wrap, Is.LessThanOrEqualTo(maxStep * 0.75f + 1e-4f), name + " wrap step " + wrap + " vs largest step " + maxStep);
                // The level across the wrap changes no more than the clip's own level changes between neighbouring windows.
                float lo = float.MaxValue, hi = 0f;
                for (int s = 0; s + 2 * window <= b.Length; s += window)
                {
                    float r = Rms(b, s, window) / Mathf.Max(1e-6f, Rms(b, s + window, window));
                    lo = Mathf.Min(lo, r);
                    hi = Mathf.Max(hi, r);
                }
                float seam = Rms(b, b.Length - window, window) / Mathf.Max(1e-6f, Rms(b, 0, window));
                Assert.That(seam, Is.InRange(lo * 0.9f, hi * 1.1f), name + " seam level ratio " + seam + " outside its own range " + lo + " to " + hi);
            }
        }

        [Test]
        public void EveryClipIsDistinct()
        {
            int n = UhAudioBank.Total;
            int[] len = new int[n];
            float[] zcr = new float[n];
            float[] bright = new float[n];
            for (int i = 0; i < n; i++)
            {
                float[] b = Clip(i);
                int zc = 0;
                double e = 0, d = 0;
                for (int k = 1; k < b.Length; k++)
                {
                    if ((b[k - 1] < 0f) != (b[k] < 0f)) zc++;
                    float s = b[k] - b[k - 1];
                    d += s * s;
                    e += b[k] * b[k];
                }
                len[i] = b.Length;
                zcr[i] = zc / (float)b.Length;
                bright[i] = (float)(d / System.Math.Max(1e-12, e));
            }
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    bool differ = Mathf.Abs(len[i] - len[j]) > 100 || Mathf.Abs(zcr[i] - zcr[j]) > 0.002f
                        || Mathf.Abs(bright[i] - bright[j]) > 0.025f * Mathf.Max(bright[i], bright[j]);
                    Assert.That(differ, Is.True, UhAudioBank.Name(i) + " and " + UhAudioBank.Name(j) + " are too alike");
                }
        }

        [Test]
        public void BedsAreLongSteadyTexturesAndTheScoreLayersShareTheBeatGrid()
        {
            for (int z = 1; z < UhAudioRules.ZoneCount; z++)
            {
                int idx = UhAudioBank.Bed((UhZone)z);
                Assert.That(UhAudioBank.IsBed(idx), Is.True, ((UhZone)z) + " has a bed");
                Assert.That(Clip(idx).Length, Is.GreaterThanOrEqualTo((int)(AudioSynth.Rate * 5f)), ((UhZone)z) + " bed is long enough not to sound like a loop");
            }
            Assert.That(UhAudioBank.Bed(UhZone.Surface), Is.EqualTo(-1), "the surface keeps the existing city ambience");
            float[] pulse = Clip(UhAudioBank.Music(UhAudioMusic.Pulse));
            float[] drive = Clip(UhAudioBank.Music(UhAudioMusic.Drive));
            Assert.That(drive.Length, Is.EqualTo(pulse.Length), "both score layers loop together");
            float beatSamples = AudioSynth.Rate * 60f / Underheart04ESounds.MusicBpm;
            Assert.That(pulse.Length / beatSamples, Is.EqualTo(Underheart04ESounds.MusicBeats).Within(0.01f), "a whole number of beats");
        }

        // ------------------------------------------------------------------ tables

        [Test]
        public void TheBankIndexCoversEveryCueOnceInAUsefulOrder()
        {
            Assert.That(UhAudioBank.Total, Is.EqualTo((int)UhSfx.Count + (int)UhLoop.Count + (UhAudioRules.ZoneCount - 1) + (int)UhAudioDetail.Count + (int)UhAudioMusic.Count));
            Assert.That(UhAudioRules.ZoneCount, Is.EqualTo(System.Enum.GetValues(typeof(UhZone)).Length), "ZoneCount follows the frozen UhZone");
            int[] order = UhAudioBank.BuildOrder();
            Assert.That(order.Length, Is.EqualTo(UhAudioBank.Total));
            HashSet<int> seen = new HashSet<int>(order);
            Assert.That(seen.Count, Is.EqualTo(UhAudioBank.Total), "build order is a permutation");
            HashSet<string> names = new HashSet<string>();
            for (int i = 0; i < UhAudioBank.Total; i++) Assert.That(names.Add(UhAudioBank.Name(i)), Is.True, "unique name " + UhAudioBank.Name(i));
            // The first-route cues come first: every interface and companion cue before any boss, creature, loop or bed.
            int lastEarly = -1, firstLate = int.MaxValue;
            for (int k = 0; k < order.Length; k++)
            {
                int i = order[k];
                bool early = i < UhAudioBank.SfxCount && (UhAudioRules.KindOf((UhSfx)i) == UhAudioCueKind.Ui || UhAudioRules.KindOf((UhSfx)i) == UhAudioCueKind.Companion);
                if (early) lastEarly = Mathf.Max(lastEarly, k);
                else firstLate = Mathf.Min(firstLate, k);
            }
            Assert.That(lastEarly, Is.LessThan(firstLate), "interface and companion cues are synthesized first");
            Assert.That(UhAudioBank.Sfx(UhSfx.UiAccept), Is.EqualTo(0));
            Assert.That(UhAudioBank.Loop(UhLoop.FormHum), Is.EqualTo(UhAudioBank.LoopBase));
        }

        [Test]
        public void EveryCueHasSaneRules()
        {
            for (int i = 0; i < (int)UhSfx.Count; i++)
            {
                UhSfx id = (UhSfx)i;
                UhAudioCue c = UhAudioRules.Cue(id);
                Assert.That(c.Volume, Is.InRange(0.2f, 1f), id + " volume");
                Assert.That(c.MinGap, Is.InRange(0.05f, 6f), id + " has a rate limit");
                Assert.That(c.Burst, Is.InRange(1, 4), id + " burst");
                Assert.That(c.Radius, Is.GreaterThanOrEqualTo(0f), id + " radius");
                Assert.That(c.MaxDistance, Is.GreaterThan(c.MinDistance * 2f), id + " reach");
                Assert.That(c.PitchJitter, Is.InRange(0f, 0.15f), id + " pitch spread");
            }
            Assert.That(UhAudioRules.KindOf(UhSfx.UiAccept), Is.EqualTo(UhAudioCueKind.Ui));
            Assert.That(UhAudioRules.KindOf(UhSfx.CompanionRecall), Is.EqualTo(UhAudioCueKind.Companion));
            Assert.That(UhAudioRules.KindOf(UhSfx.FuseLock), Is.EqualTo(UhAudioCueKind.Fusion));
            Assert.That(UhAudioRules.KindOf(UhSfx.HeatTick), Is.EqualTo(UhAudioCueKind.World));
            Assert.That(UhAudioRules.KindOf(UhSfx.BossTelegraph), Is.EqualTo(UhAudioCueKind.Boss));
            Assert.That(UhAudioRules.KindOf(UhSfx.MothFlutter), Is.EqualTo(UhAudioCueKind.Creature));
            Assert.That(UhAudioRules.Cue(UhSfx.BossTelegraph).MaxDistance, Is.GreaterThan(UhAudioRules.Cue(UhSfx.UiAccept).MaxDistance * 3f), "boss warnings carry across an arena");
            // Companion injury and downed cues never chatter.
            Assert.That(UhAudioRules.Cue(UhSfx.CompanionHurt).MinGap, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(UhAudioRules.Cue(UhSfx.CompanionDowned).MinGap, Is.GreaterThanOrEqualTo(2f));
            for (int i = 0; i < (int)UhLoop.Count; i++)
            {
                float vol, min, max;
                UhAudioRules.LoopSpec((UhLoop)i, out vol, out min, out max);
                Assert.That(vol, Is.InRange(0.2f, 1f), ((UhLoop)i) + " loop level");
                Assert.That(max, Is.GreaterThan(min * 3f), ((UhLoop)i) + " loop reach");
            }
            Assert.That(UhAudioRules.Attenuation(1f, 3f, 40f), Is.EqualTo(1f));
            Assert.That(UhAudioRules.Attenuation(40f, 3f, 40f), Is.EqualTo(0f));
            Assert.That(UhAudioRules.Attenuation(12f, 3f, 40f), Is.LessThan(UhAudioRules.Attenuation(6f, 3f, 40f)), "farther is quieter");
        }

        // ------------------------------------------------------------------ rate limit

        [Test]
        public void TheSameEventNeverStacksOncePerColliderHit()
        {
            UhAudioRateLimiter rl = new UhAudioRateLimiter(UhAudioBank.SfxCount);
            UhAudioCue crack = UhAudioRules.Cue(UhSfx.PlateCrack);
            Vector3 p = new Vector3(10f, -26f, 4f);
            int accepted = 0;
            // One explosion touching ten colliders of the same plate in one frame.
            for (int k = 0; k < 10; k++) if (rl.TryAccept((int)UhSfx.PlateCrack, 5f, p + new Vector3(0.05f * k, 0f, 0f), true, crack)) accepted++;
            Assert.That(accepted, Is.EqualTo(1), "one hit, one sound");
            // Held contact: a call every frame at 60 fps for two seconds plays only once per gap.
            UhAudioRateLimiter rl2 = new UhAudioRateLimiter(UhAudioBank.SfxCount);
            UhAudioCue tick = UhAudioRules.Cue(UhSfx.HeatTick);
            float last = -100f;
            int plays = 0;
            for (int f = 0; f < 600; f++)
            {
                float t = f / 60f;
                if (!rl2.TryAccept((int)UhSfx.HeatTick, t, Vector3.zero, true, tick)) continue;
                Assert.That(t - last, Is.GreaterThanOrEqualTo(tick.MinGap - 1e-4f), "plays are at least the gap apart");
                last = t;
                plays++;
            }
            Assert.That(plays, Is.LessThanOrEqualTo(Mathf.CeilToInt(10f / tick.MinGap) + 1), "heat ticks are rate limited");
            Assert.That(plays, Is.GreaterThan(5), "but still keep ticking");
        }

        [Test]
        public void SeparateEventsFarApartStillSoundUpToTheBurst()
        {
            UhAudioRateLimiter rl = new UhAudioRateLimiter(UhAudioBank.SfxCount);
            UhAudioCue cue = UhAudioRules.Cue(UhSfx.ScavengerChitter);
            int accepted = 0;
            for (int k = 0; k < 8; k++) if (rl.TryAccept((int)UhSfx.ScavengerChitter, 1f, new Vector3(k * 20f, -26f, 0f), true, cue)) accepted++;
            Assert.That(accepted, Is.EqualTo(cue.Burst), "distinct scavengers sound, capped per gap");
            // After the gap the same id plays again.
            Assert.That(rl.TryAccept((int)UhSfx.ScavengerChitter, 1f + cue.MinGap + 0.01f, Vector3.zero, true, cue), Is.True);
            // 2D plays have no position: always the same place.
            UhAudioRateLimiter r2 = new UhAudioRateLimiter(UhAudioBank.SfxCount);
            UhAudioCue ui = UhAudioRules.Cue(UhSfx.UiAccept);
            Assert.That(r2.TryAccept((int)UhSfx.UiAccept, 2f, Vector3.zero, false, ui), Is.True);
            Assert.That(r2.TryAccept((int)UhSfx.UiAccept, 2.01f, new Vector3(500f, 0f, 0f), false, ui), Is.False);
            // Ids are independent.
            Assert.That(r2.TryAccept((int)UhSfx.UiTrack, 2.01f, Vector3.zero, false, UhAudioRules.Cue(UhSfx.UiTrack)), Is.True);
            // Bad ids are refused, a clock that went backwards counts as long ago, Reset forgets.
            Assert.That(r2.TryAccept(-1, 3f, Vector3.zero, false, ui), Is.False);
            Assert.That(r2.TryAccept((int)UhSfx.Count, 3f, Vector3.zero, false, ui), Is.False);
            Assert.That(r2.TryAccept((int)UhSfx.UiTrack, 0.5f, Vector3.zero, false, UhAudioRules.Cue(UhSfx.UiTrack)), Is.True);
            r2.Reset();
            Assert.That(r2.TryAccept((int)UhSfx.UiAccept, 2.02f, Vector3.zero, false, ui), Is.True);
        }

        [Test]
        public void ABurstOfDifferentCuesIsCappedButTheInterfaceIsNot()
        {
            UhAudioRateLimiter rl = new UhAudioRateLimiter(UhAudioBank.SfxCount);
            int world = 0;
            for (int i = 0; i < (int)UhSfx.Count; i++)
            {
                UhSfx id = (UhSfx)i;
                if (UhAudioRules.KindOf(id) == UhAudioCueKind.Ui) continue;
                if (rl.TryAccept(i, 7f, new Vector3(i * 3f, 0f, 0f), true, UhAudioRules.Cue(id))) world++;
            }
            Assert.That(world, Is.EqualTo(UhAudioRules.GlobalCap), "at most GlobalCap non-interface cues start in one window");
            int ui = 0;
            for (int i = 0; i < (int)UhSfx.Count; i++)
            {
                UhSfx id = (UhSfx)i;
                if (UhAudioRules.KindOf(id) != UhAudioCueKind.Ui) continue;
                if (rl.TryAccept(i, 7f, Vector3.zero, false, UhAudioRules.Cue(id))) ui++;
            }
            Assert.That(ui, Is.EqualTo(5), "interface feedback is never squeezed out by a busy fight");
            Assert.That(rl.TryAccept((int)UhSfx.SteamBurst, 7f + UhAudioRules.GlobalWindow + 0.001f, new Vector3(999f, 0f, 0f), true, UhAudioRules.Cue(UhSfx.SteamBurst)), Is.True, "the window passes");
        }

        // ------------------------------------------------------------------ zone blend

        [Test]
        public void ZoneWeightsStayNormalizedThroughAnySequence()
        {
            UhAudioZoneBlend blend = new UhAudioZoneBlend();
            GRand r = new GRand(4401);
            UhZone target = UhZone.Surface;
            float[] before = new float[UhAudioRules.ZoneCount];
            for (int step = 0; step < 6000; step++)
            {
                if (r.Chance(0.01f)) target = (UhZone)r.Range(0, UhAudioRules.ZoneCount);
                float dt = r.Range(0.001f, 0.1f);
                for (int z = 0; z < before.Length; z++) before[z] = blend.Weight((UhZone)z);
                if (r.Chance(0.002f)) blend.Snap(target);
                else blend.Step(target, dt, UhAudioRules.ZoneFade);
                Assert.That(blend.Sum, Is.EqualTo(1f).Within(1e-4f), "weights sum to 1 at step " + step);
                for (int z = 0; z < before.Length; z++)
                {
                    float w = blend.Weight((UhZone)z);
                    Assert.That(w, Is.InRange(0f, 1f), "weight in range");
                }
                Assert.That(blend.SurfaceGain, Is.InRange(0f, 1f));
            }
            Assert.That(blend.Weight((UhZone)99), Is.EqualTo(0f));
        }

        [Test]
        public void AZoneFadeTakesAboutOneAndAHalfSecondsAndNeverJumps()
        {
            UhAudioZoneBlend blend = new UhAudioZoneBlend();
            Assert.That(blend.SurfaceGain, Is.EqualTo(1f), "street audio unchanged on the surface");
            Assert.That(blend.Gain(UhZone.Surface), Is.EqualTo(1f));
            float dt = 1f / 60f;
            float t = 0f;
            float prevSurface = 1f;
            while (!blend.Settled || blend.Target != UhZone.Canals)
            {
                blend.Step(UhZone.Canals, dt, UhAudioRules.ZoneFade);
                t += dt;
                float gs = blend.Gain(UhZone.Surface), gc = blend.Gain(UhZone.Canals);
                Assert.That(gs * gs + gc * gc, Is.EqualTo(1f).Within(1e-3f), "equal-power crossfade");
                Assert.That(blend.SurfaceGain, Is.LessThanOrEqualTo(prevSurface + 1e-6f), "street ambience only fades while descending");
                prevSurface = blend.SurfaceGain;
                if (Mathf.Abs(t - 0.75f) < dt * 0.5f) Assert.That(blend.Weight(UhZone.Canals), Is.EqualTo(0.5f).Within(0.03f), "halfway at half the fade");
                Assert.That(t, Is.LessThan(5f), "the fade finishes");
            }
            Assert.That(t, Is.EqualTo(UhAudioRules.ZoneFade).Within(dt * 2f), "full fade in ZoneFade seconds");
            Assert.That(blend.SurfaceGain, Is.EqualTo(0f), "silent street underground");
            Assert.That(blend.Dominant, Is.EqualTo(UhZone.Canals));
            // Change of mind halfway: blends on from where it was, no weight moves faster than the fade allows.
            blend.Step(UhZone.Refuge, 0.75f, UhAudioRules.ZoneFade);
            float wc = blend.Weight(UhZone.Canals), wr = blend.Weight(UhZone.Refuge);
            for (int k = 0; k < 30; k++)
            {
                blend.Step(UhZone.Crown, dt, UhAudioRules.ZoneFade);
                Assert.That(Mathf.Abs(blend.Weight(UhZone.Canals) - wc), Is.LessThanOrEqualTo(dt / UhAudioRules.ZoneFade + 1e-4f));
                Assert.That(Mathf.Abs(blend.Weight(UhZone.Refuge) - wr), Is.LessThanOrEqualTo(dt / UhAudioRules.ZoneFade + 1e-4f));
                wc = blend.Weight(UhZone.Canals);
                wr = blend.Weight(UhZone.Refuge);
            }
            blend.Snap(UhZone.ServiceInterior);
            Assert.That(blend.SurfaceGain, Is.EqualTo(UhAudioRules.SurfaceLeak(UhZone.ServiceInterior)).Within(1e-6f), "muffled street in a service interior");
            blend.Snap(UhZone.Surface);
            Assert.That(blend.SurfaceGain, Is.EqualTo(1f), "exactly 1 again on the surface");
            blend.Step(UhZone.Surface, 0.5f, UhAudioRules.ZoneFade);
            Assert.That(blend.SurfaceGain, Is.EqualTo(1f), "stepping toward the surface while there changes nothing");
        }

        // ------------------------------------------------------------------ zone resolver

        [Test]
        public void TheZoneFollowsTheWorldAndRefusesStaleRequests()
        {
            // Outside a session (title, loading) it is always the surface.
            Assert.That(UhAudioRules.Resolve(UhZone.Crown, 10f, UnderheartIds.FoundryCrown, 10f, true, false, -95f, false, 11f), Is.EqualTo(UhZone.Surface));
            // Nobody called SetZone: the region the player stands in decides.
            Assert.That(UhAudioRules.Resolve(UhZone.Surface, -1e9f, UnderheartIds.EmberCanals, 5f, true, false, -26f, true, 6f), Is.EqualTo(UhZone.Canals));
            // A Surface request never applies underground (a venue exit firing while the player went down the stairs).
            Assert.That(UhAudioRules.Resolve(UhZone.Surface, 9f, UnderheartIds.ServiceLevel, 5f, true, false, -6f, true, 9.5f), Is.EqualTo(UhZone.ServiceInterior));
            // Between a request and a region change, the newer wins.
            Assert.That(UhAudioRules.Resolve(UhZone.Gardens, 8f, UnderheartIds.EmberCanals, 5f, true, false, -30f, true, 9f), Is.EqualTo(UhZone.Gardens));
            Assert.That(UhAudioRules.Resolve(UhZone.Gardens, 4f, UnderheartIds.EmberCanals, 5f, true, false, -30f, true, 9f), Is.EqualTo(UhZone.Canals));
            // On the street a service-interior request holds while the player is inside a venue...
            Assert.That(UhAudioRules.Resolve(UhZone.ServiceInterior, 1f, "", -1e9f, false, true, 0.15f, true, 100f), Is.EqualTo(UhZone.ServiceInterior));
            // ...and a forgotten one (the player walked out and nobody said Surface) heals itself after the grace.
            Assert.That(UhAudioRules.Resolve(UhZone.ServiceInterior, 1f, "", -1e9f, false, false, 0.15f, true, 1.5f), Is.EqualTo(UhZone.ServiceInterior));
            Assert.That(UhAudioRules.Resolve(UhZone.ServiceInterior, 1f, "", -1e9f, false, false, 0.15f, true, 100f), Is.EqualTo(UhZone.Surface));
            // An underground zone requested on the street is honoured briefly (a door threshold), then dropped.
            Assert.That(UhAudioRules.Resolve(UhZone.Refuge, 10f, "", -1e9f, false, false, 0.15f, true, 10.5f), Is.EqualTo(UhZone.Refuge));
            Assert.That(UhAudioRules.Resolve(UhZone.Refuge, 10f, "", -1e9f, false, false, 0.15f, true, 10f + UhAudioRules.StaleZoneGrace + 0.1f), Is.EqualTo(UhZone.Surface));
            // Underground with no region reported: the request applies, otherwise depth picks a sensible zone.
            Assert.That(UhAudioRules.Resolve(UhZone.Workshops, 1f, "", -1e9f, true, false, -55f, true, 50f), Is.EqualTo(UhZone.Workshops));
            Assert.That(UhAudioRules.Resolve(UhZone.Surface, -1e9f, "", -1e9f, true, false, -72f, true, 50f), Is.EqualTo(UhZone.Reliquary));
            // A garbage request is treated as the surface.
            Assert.That(UhAudioRules.Resolve((UhZone)42, 1f, "", -1e9f, false, false, 0f, true, 2f), Is.EqualTo(UhZone.Surface));
        }

        [Test]
        public void EveryRegionAndDepthHasItsOwnZone()
        {
            HashSet<UhZone> zones = new HashSet<UhZone>();
            foreach (string region in UnderheartIds.Regions)
            {
                UhZone z = UhAudioRules.ZoneForRegion(region);
                Assert.That(z, Is.Not.EqualTo(UhZone.Surface), region + " maps underground");
                Assert.That(zones.Add(z), Is.True, region + " has its own zone");
            }
            Assert.That(zones.Count, Is.EqualTo(UhAudioRules.ZoneCount - 1), "every non-surface zone is reachable");
            Assert.That(UhAudioRules.ZoneForRegion(""), Is.EqualTo(UhZone.Surface));
            Assert.That(UhAudioRules.ZoneForRegion(null), Is.EqualTo(UhZone.Surface));
            Assert.That(UhAudioRules.ZoneForRegion("nowhere"), Is.EqualTo(UhZone.Surface));
            float[] depths = { -6f, -18f, -26f, -40f, -55f, -72f, -95f };
            UhZone[] expect = { UhZone.ServiceInterior, UhZone.Refuge, UhZone.Canals, UhZone.Gardens, UhZone.Workshops, UhZone.Reliquary, UhZone.Crown };
            for (int i = 0; i < depths.Length; i++) Assert.That(UhAudioRules.ZoneForDepth(depths[i]), Is.EqualTo(expect[i]), "planned depth " + depths[i]);
            for (int z = 0; z < UhAudioRules.ZoneCount; z++)
            {
                UhZone zone = (UhZone)z;
                Assert.That(UhAudioRules.SurfaceLeak(zone), Is.InRange(0f, 1f));
                if (zone == UhZone.Surface) Assert.That(UhAudioRules.BedLevel(zone), Is.EqualTo(0f), "no bed on the surface");
                else Assert.That(UhAudioRules.BedLevel(zone), Is.InRange(0.2f, 0.8f), zone + " bed level");
                Assert.That(UhAudioRules.IsUnderground(zone), Is.EqualTo(zone >= UhZone.Refuge));
            }
            Assert.That(UhAudioRules.SurfaceLeak(UhZone.Surface), Is.EqualTo(1f));
            Assert.That(UhAudioRules.SurfaceLeak(UhZone.Crown), Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ details and score

        [Test]
        public void DetailsAreScatteredAtBoundedRates()
        {
            UhAudioDetailScheduler s = new UhAudioDetailScheduler(77);
            UhAudioDetailSpec spec;
            float az, range, pitch;
            Assert.That(s.Tick(UhZone.Surface, 0f, out spec, out az, out range, out pitch), Is.False, "no details on the surface");
            for (int z = 1; z < UhAudioRules.ZoneCount; z++)
            {
                UhZone zone = (UhZone)z;
                UhAudioDetailSpec[] list = UhAudioRules.Details(zone);
                Assert.That(list.Length, Is.InRange(1, 8), zone + " has details");
                int[] count = new int[(int)UhAudioDetail.Count];
                float seconds = 600f;
                float t0 = 1000f * z;
                for (int f = 0; f <= (int)(seconds * 60f); f++)
                {
                    float t = t0 + f / 60f;
                    if (!s.Tick(zone, t, out spec, out az, out range, out pitch)) continue;
                    count[(int)spec.Detail]++;
                    Assert.That(az, Is.InRange(0f, 1f));
                    Assert.That(range, Is.InRange(0f, 1f));
                }
                foreach (UhAudioDetailSpec d in list)
                {
                    Assert.That(d.MaxGap, Is.GreaterThan(d.MinGap), d.Detail + " gaps");
                    Assert.That(d.Volume, Is.InRange(0.1f, 0.6f), d.Detail + " sits under the cues");
                    Assert.That(count[(int)d.Detail], Is.LessThanOrEqualTo(Mathf.CeilToInt(seconds / d.MinGap) + 2), zone + " " + d.Detail + " never floods");
                    Assert.That(count[(int)d.Detail], Is.GreaterThanOrEqualTo(Mathf.FloorToInt(seconds / d.MaxGap) - 2), zone + " " + d.Detail + " keeps happening");
                }
            }
            // A pause in ticking re-arms instead of firing every stale detail at once.
            UhAudioDetailScheduler p = new UhAudioDetailScheduler(5);
            p.Tick(UhZone.Canals, 0f, out spec, out az, out range, out pitch);
            int burst = 0;
            for (int f = 0; f < 10; f++) if (p.Tick(UhZone.Canals, 500f + f / 60f, out spec, out az, out range, out pitch)) burst++;
            Assert.That(burst, Is.EqualTo(0), "no burst after a long pause");
        }

        [Test]
        public void TheScoreFollowsBossesAndSkirmishesAndNeverPlaysOnTheStreet()
        {
            float drive;
            Assert.That(UhAudioRules.MusicTarget(false, false, UhZone.Canals, out drive), Is.EqualTo(0f));
            Assert.That(drive, Is.EqualTo(0f));
            Assert.That(UhAudioRules.MusicTarget(false, true, UhZone.ServiceInterior, out drive), Is.GreaterThan(0.5f), "a skirmish brings the pulse");
            Assert.That(drive, Is.EqualTo(0f), "but not the boss drive");
            Assert.That(UhAudioRules.MusicTarget(true, false, UhZone.Crown, out drive), Is.EqualTo(1f));
            Assert.That(drive, Is.EqualTo(1f), "a boss fight adds the drive");
            Assert.That(UhAudioRules.MusicTarget(true, true, UhZone.Surface, out drive), Is.EqualTo(0f), "street audio never changes");
            Assert.That(drive, Is.EqualTo(0f));
            Assert.That(UhAudioRules.MusicRelease, Is.GreaterThan(UhAudioRules.MusicAttack), "the score releases gently after victory");
        }
    }
}
