using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Sunbolt's look in the Vale: the gather in Malik's right hand after an accepted cast, the glowing bolt and its
    /// twisting trail, and the burst at the collar. Timing and shapes come from <see cref="SunboltFx"/>. The journey
    /// still decides everything that matters (cost, cooldown, which collar is grounded); a collar flares only when
    /// <c>ShardwakeJourney.Ground</c> accepts the hit.
    /// </summary>
    public sealed partial class ShardwakeVale
    {
        private Material _boltGlow;
        private GameObject _gather, _burst;
        private LineRenderer _gatherRing, _burstRing;
        private float _castT = -1f, _flight, _released, _burstT = -1f, _phase;
        private bool _burstHit;
        private Vector3 _burstAt;
        private readonly float[] _flare = { -1f, -1f, -1f };
        private readonly Vector3[] _trail = new Vector3[SunboltFx.TrailPoints];

        /// <summary>Additive glow material, the gather, burst and their rings, and the trail's taper.</summary>
        private void SetupSunbolt()
        {
            _boltGlow = new Material(signalMaterial) { name = "Sunbolt glow (runtime)" };
            _boltGlow.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _boltGlow.SetFloat("_Boost", 1.5f);
            _boltGlow.SetFloat("_Soft", 1f);
            _orb.GetComponent<MeshRenderer>().sharedMaterial = _boltGlow;
            _orb.transform.localScale = new Vector3(.30f, .30f, .30f);
            _streak.sharedMaterial = _boltGlow;
            _streak.textureMode = LineTextureMode.Stretch;
            _streak.numCapVertices = 0;
            _streak.widthMultiplier = SunboltFx.TrailWidth;
            Keyframe[] keys = new Keyframe[5];
            for (int i = 0; i < keys.Length; i++)
            {
                float u = i / (float)(keys.Length - 1);
                keys[i] = new Keyframe(u, SunboltFx.TrailWidthAt(u));
            }
            _streak.widthCurve = new AnimationCurve(keys);
            Color gold = _run.Sunbolt.Color;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(new Color(1f, .62f, .22f), 0f), new GradientColorKey(gold, .7f), new GradientColorKey(new Color(1f, .95f, .78f), 1f) },
                new[] { new GradientAlphaKey(SunboltFx.TrailAlphaAt(0f), 0f), new GradientAlphaKey(SunboltFx.TrailAlphaAt(.5f), .5f), new GradientAlphaKey(SunboltFx.TrailAlphaAt(1f), 1f) });
            _streak.colorGradient = fade;
            _gather = Bead("Sunbolt gather", transform, Vector3.zero, Vector3.one * .1f);
            _burst = Bead("Sunbolt burst", transform, Vector3.zero, Vector3.one * .3f);
            _gather.GetComponent<MeshRenderer>().sharedMaterial = _boltGlow;
            _burst.GetComponent<MeshRenderer>().sharedMaterial = _boltGlow;
            _gather.SetActive(false);
            _burst.SetActive(false);
            _gatherRing = Line("Sunbolt gather ring", .035f);
            _burstRing = Line("Sunbolt burst ring", .06f);
            _gatherRing.sharedMaterial = _boltGlow;
            _burstRing.sharedMaterial = _boltGlow;
        }

        private Vector3 HandPoint()
        {
            return _walk.Rig.HandR.position + Vector3.up * .10f;
        }

        /// <summary>Starts the gather after the journey has accepted the cast (and charged it).</summary>
        private void BeginSunbolt(int target)
        {
            _destination = GemPoint(target);
            _castT = 0f;
            _phase = Random.value * 6.2832f;
            _gather.SetActive(true);
            _gather.transform.position = HandPoint();
            _gather.transform.localScale = Vector3.one * .06f;
            _gatherRing.enabled = true;
            _gatherRing.startColor = _gatherRing.endColor = _run.Sunbolt.Color;
        }

        /// <summary>Advances the gather, the flight, the burst and any collar flare. Grounds the collar on arrival.</summary>
        private void TickSunbolt(float dt)
        {
            bool repaint = false;
            for (int i = 0; i < _flare.Length; i++)
            {
                if (_flare[i] < 0f) continue;
                _flare[i] += dt;
                if (_flare[i] >= SunboltFx.FlareSeconds) _flare[i] = -1f;
                repaint = true;
            }
            if (repaint) PaintSockets();
            TickBurst(dt);
            if (_shot < 0 || _castT < 0f) return;
            _castT += dt;
            Vector3 hand = HandPoint();
            if (SunboltFx.Gathering(_castT))
            {
                _gather.transform.position = hand;
                _gather.transform.localScale = Vector3.one * (.06f + .24f * SunboltFx.Gather(_castT));
                Ring(_gatherRing, hand, SunboltFx.GatherRing(_castT), false);
                return;
            }
            if (_gather.activeSelf)
            {
                // Release: the bolt leaves from wherever the hand is now.
                _gather.SetActive(false);
                _gatherRing.enabled = false;
                _origin = hand;
                _released = _castT;
                _flight = SunboltFx.FlightSeconds(Vector3.Distance(_origin, _destination));
                _orb.SetActive(true);
                _streak.positionCount = _trail.Length;
                _streak.enabled = true;
            }
            float t = SunboltFx.Progress(_castT - _released, _flight);
            _orb.transform.position = Vector3.Lerp(_origin, _destination, t);
            SunboltFx.Trail(_origin, _destination, t, _phase, _trail);
            _streak.SetPositions(_trail);
            if (t < 1f) return;
            int hit = _shot;
            _shot = -1;
            _focus = -1;
            _castT = -1f;
            _orb.SetActive(false);
            _streak.enabled = false;
            bool grounded = _run.Ground(hit);
            StartBurst(_destination, grounded);
            if (grounded)
            {
                _flare[hit] = 0f;
                PaintSockets();
                Tone(660, .32f);
                Changed(_run.Profile.step == ValeStep.TakeSeed ? "The Crown is quiet. Take the hearth seed." : "One collar grounded.");
            }
            else Tone(240, .18f, .14f);
        }

        private void StartBurst(Vector3 at, bool hit)
        {
            _burstT = 0f;
            _burstAt = at;
            _burstHit = hit;
            _burst.SetActive(true);
            _burstRing.enabled = true;
            TickBurst(0f);
        }

        private void TickBurst(float dt)
        {
            if (_burstT < 0f) return;
            _burstT += dt;
            float end = _burstHit ? SunboltFx.BurstSeconds : SunboltFx.FizzleSeconds;
            if (_burstT >= end)
            {
                _burstT = -1f;
                _burst.SetActive(false);
                _burstRing.enabled = false;
                return;
            }
            Color color;
            float radius;
            if (_burstHit)
            {
                color = _run.Sunbolt.Color;
                color.a = SunboltFx.BurstAlpha(_burstT);
                radius = SunboltFx.BurstRadius(_burstT);
            }
            else
            {
                color = SunboltFx.FizzleColor;
                color.a *= SunboltFx.FizzleAlpha(_burstT);
                radius = SunboltFx.FizzleRadius(_burstT);
            }
            _burst.transform.position = _burstAt;
            _burst.transform.localScale = Vector3.one * (radius * (_burstHit ? 1.1f : 2f));
            _paint.Clear();
            _paint.SetColor("_Color", color);
            _burst.GetComponent<MeshRenderer>().SetPropertyBlock(_paint);
            _burstRing.startColor = _burstRing.endColor = color;
            Ring(_burstRing, _burstAt, radius * (_burstHit ? 1.35f : 1f), false);
        }

        /// <summary>Flare strength of a collar now (0 unless it was grounded within the last FlareSeconds).</summary>
        private float FlareOf(int i)
        {
            return _flare[i] < 0f ? 0f : SunboltFx.Flare(_flare[i]);
        }

        /// <summary>Hides every Sunbolt piece (respawn, replay) without touching the journey.</summary>
        private void ClearSunbolt()
        {
            _castT = -1f;
            _burstT = -1f;
            for (int i = 0; i < _flare.Length; i++) _flare[i] = -1f;
            if (_gather != null) _gather.SetActive(false);
            if (_burst != null) _burst.SetActive(false);
            if (_gatherRing != null) _gatherRing.enabled = false;
            if (_burstRing != null) _burstRing.enabled = false;
        }

        private void ReleaseSunbolt()
        {
            if (_boltGlow != null) Destroy(_boltGlow);
            _boltGlow = null;
        }
    }
}
