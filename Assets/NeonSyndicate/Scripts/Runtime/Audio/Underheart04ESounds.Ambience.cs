using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E zone ambience and encounter score. Every frame the zone is resolved (the caller's SetZone, the Underheart
    /// region the player stands in, depth), the weights step toward it over UhAudioRules.ZoneFade, each zone's bed plays
    /// at its equal-power gain (silent beds are stopped), AudioManager reads SurfaceGain for the street layers, details
    /// of the dominant zone are scattered around the listener, and the score follows Underheart boss fights and
    /// skirmishes, releasing over a few seconds after victory.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        private static void TickZone(float now, float dt)
        {
            bool active = SessionActive();
            string region = "";
            if (active && Game.Underheart != null) region = Game.Underheart.PlayerRegion ?? "";
            if (region != _lastRegion)
            {
                _lastRegion = region;
                _regionChangedAt = now;
            }
            Vector3 p = Game.PlayerPosition;
            bool deep = active && MathUtil.IsFinite(p) && (p.y < UhAudioRules.DeepY || WorldBounds.IsUnderground(p));
            bool interior = active && Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.Inside;
            UhZone target = _debugZone >= 0 && _debugZone < UhAudioRules.ZoneCount
                ? (UhZone)_debugZone
                : UhAudioRules.Resolve(_requested, _requestedAt, region, _regionChangedAt, deep, interior, p.y, active, now);
            if (target != _zone)
            {
                _zone = target;
                // A session that starts underground (a load) starts in its own ambience instead of fading from the street.
                if (now - _playingSince < 1f) _blend.Snap(target);
            }
            _blend.Step(target, dt, UhAudioRules.ZoneFade);
            ApplyBeds(dt, active);
            TickDetails(now, active);
        }

        private static void ApplyBeds(float dt, bool active)
        {
            if (_beds == null) return;
            float sfx = Settings.SfxVolume;
            for (int z = 1; z < _beds.Length; z++)
            {
                AudioSource s = _beds[z];
                if (s == null) continue;
                UhZone zone = (UhZone)z;
                float target = active ? _blend.Gain(zone) * UhAudioRules.BedLevel(zone) : 0f;
                _bedLevel[z] = Mathf.MoveTowards(_bedLevel[z], target, dt * 2f);
                if (_bedLevel[z] > 0.0005f)
                {
                    if (!s.isPlaying)
                    {
                        AudioClip c = ClipAt(UhAudioBank.Bed(zone), false);
                        if (c == null)
                        {
                            // Not synthesized yet: fade in from silence once it is.
                            _bedLevel[z] = 0f;
                            continue;
                        }
                        if (s.clip != c) s.clip = c;
                        s.volume = 0f;
                        s.Play();
                    }
                    s.volume = _bedLevel[z] * sfx;
                }
                else if (s.isPlaying)
                {
                    s.volume = 0f;
                    s.Stop();
                }
            }
        }

        private static void TickDetails(float now, bool active)
        {
            if (_details == null || !active || Game.State != GameState.Playing || GameTime.Paused) return;
            UhZone dom = _blend.Dominant;
            float weight = _blend.Weight(dom);
            if (dom == UhZone.Surface || weight < 0.5f) return;
            UhAudioDetailSpec spec;
            float az, range, pitch01;
            if (!_scheduler.Tick(dom, now, out spec, out az, out range, out pitch01)) return;
            PlayDetail(spec, az, range, pitch01, weight);
        }

        private static void PlayDetail(UhAudioDetailSpec spec, float az, float range01, float pitch01, float weight)
        {
            AudioClip c = ClipAt(UhAudioBank.Detail(spec.Detail), true);
            if (c == null) return;
            Transform l = Listener();
            Vector3 c0 = l != null ? l.position : Game.PlayerPosition;
            float ang = az * Mathf.PI * 2f;
            float r = Mathf.Lerp(spec.MinRange, spec.MaxRange, range01);
            Vector3 at = c0 + new Vector3(Mathf.Cos(ang) * r, (range01 - 0.5f) * 3f, Mathf.Sin(ang) * r);
            AudioSource s = _details[_detailNext];
            _detailNext = (_detailNext + 1) % _details.Length;
            if (s == null) return;
            s.Stop();
            s.transform.position = at;
            s.clip = c;
            s.maxDistance = Mathf.Max(45f, spec.MaxRange * 2.5f);
            s.volume = Mathf.Clamp01(spec.Volume * weight) * Settings.SfxVolume;
            s.pitch = 1f + (pitch01 * 2f - 1f) * spec.PitchJitter;
            s.Play();
        }

        private static void TickMusic(float dt)
        {
            if (_music == null) return;
            bool active = SessionActive();
            bool boss = active && Game.UnderBosses != null && Game.UnderBosses.FightActive;
            bool skirmish = active && Game.Ecology04E != null && Game.Ecology04E.AnySkirmishActive;
            float drive;
            float target = UhAudioRules.MusicTarget(boss, skirmish, _zone, out drive);
            if (_debugMusic >= 0)
            {
                // Debug page audition: ignores the zone so the score can be heard anywhere.
                target = _debugMusic == 0 ? 0f : (_debugMusic == 2 ? 1f : 0.8f);
                drive = _debugMusic == 2 ? 1f : 0f;
            }
            // The car radio wins: no score over a playing station.
            else if (Game.Radio != null && Game.Radio.PlayerInVehicle && Game.Radio.Station >= 0)
            {
                target = 0f;
                drive = 0f;
            }
            float up = dt / UhAudioRules.MusicAttack;
            float down = dt / (_musicForceRelease ? 0.05f : UhAudioRules.MusicRelease);
            _musicLevel = target > _musicLevel ? Mathf.Min(target, _musicLevel + up) : Mathf.Max(target, _musicLevel - down);
            _driveLevel = drive > _driveLevel ? Mathf.Min(drive, _driveLevel + up) : Mathf.Max(drive, _driveLevel - down);
            if (_musicLevel <= 0f && _driveLevel <= 0f)
            {
                _musicForceRelease = false;
                if (_musicPlaying)
                {
                    for (int i = 0; i < _music.Length; i++)
                        if (_music[i] != null)
                        {
                            _music[i].volume = 0f;
                            _music[i].Stop();
                        }
                    _musicPlaying = false;
                }
                return;
            }
            if (!_musicPlaying)
            {
                AudioClip pulse = ClipAt(UhAudioBank.Music(UhAudioMusic.Pulse), false);
                AudioClip driveClip = ClipAt(UhAudioBank.Music(UhAudioMusic.Drive), false);
                if (pulse == null || driveClip == null || _music[0] == null || _music[1] == null)
                {
                    _musicLevel = 0f;
                    _driveLevel = 0f;
                    return;
                }
                // Both layers start together and share a length, so the drive stays on the beat when a boss arrives later.
                _music[0].clip = pulse;
                _music[1].clip = driveClip;
                _music[0].volume = 0f;
                _music[1].volume = 0f;
                double start = AudioSettings.dspTime + 0.05;
                _music[0].PlayScheduled(start);
                _music[1].PlayScheduled(start);
                _musicPlaying = true;
            }
            float mv = Settings.MusicVolume;
            if (_music[0] != null) _music[0].volume = _musicLevel * 0.55f * mv;
            if (_music[1] != null) _music[1].volume = _driveLevel * 0.5f * mv;
        }
    }
}
