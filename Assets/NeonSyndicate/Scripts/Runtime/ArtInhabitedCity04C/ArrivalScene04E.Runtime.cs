using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Drives the 04E arrival scene after it is built: the receiver crystals' slow pulse, the conduit joints (an ember glow,
    /// or a chase toward the Pressure House while lit), the turning beacon, the aviation blink, night-scaled lamps, at most
    /// two shadowless practical lights near the player, the Pressure House's ambient sparks, the receiver hum and the
    /// muffled service-interior audio zone. Owns every material and mesh it created and releases them on destroy. A
    /// session reset (new game or load) dims the conduit and returns the sparks to automatic.
    /// </summary>
    public sealed class ArrivalScene04EController : MonoBehaviour, IWorldSystem
    {
        public Material Pulse, RefugeGlow, Beacon, Aviation, Lamp;
        public readonly Material[] ConduitGlow = new Material[3];
        public readonly List<Mesh> OwnedMeshes = new List<Mesh>();
        public Transform BeaconLens;
        public Vector3 MastHead, ConduitStart, ConduitEnd;

        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<bool> _lightNeedsLit = new List<bool>();
        private readonly Vector3[] _sparkPoints = new Vector3[3];
        private Vector3 _pressureDoor;
        private bool _hasPressure, _zoneSet;
        private float _lightClock, _zoneClock, _sparkTimer = 1.5f, _pingTimer = 6f, _night = .5f, _nightClock;
        private int _receiverLoop;
        private System.Random _rand = new System.Random(61724);

        private static readonly Color PulseColor = new Color(1f, .58f, .2f);
        private static readonly Color ConduitColor = new Color(1f, .42f, .12f);
        private static readonly Color RefugeColor = new Color(.45f, 1f, .78f);
        private static readonly Color LampColor = new Color(1f, .78f, .48f);

        public void Init()
        {
            Pulse = Own("04E receiver crystal", PulseColor, .75f, 1.2f);
            RefugeGlow = Own("04E refuge perch glow", RefugeColor, .7f, .9f);
            Beacon = Own("04E dispatch beacon", new Color(1f, .62f, .15f), .6f, 2.6f);
            Aviation = Own("04E aviation lamp", new Color(1f, .12f, .06f), .6f, 2.2f);
            Lamp = Own("04E warm lamp", LampColor, .5f, 1.4f);
            for (int i = 0; i < 3; i++) ConduitGlow[i] = Own("04E conduit joint " + i, ConduitColor, .55f, .3f);
            WorldSystems.Register(this);
        }

        private Material Own(string name, Color color, float smoothness, float emission)
        {
            Material m = Mats.Standard(name, color, smoothness, 0f);
            Mats.SetEmission(m, color * emission, null);
            _materials.Add(m);
            return m;
        }

        /// <summary>After the pieces exist: the practical lights and the Pressure House spark emitters.</summary>
        public void BindVenues(InhabitedCitySystem city)
        {
            VenueBinding04C dispatch = city.FindVenue(VenueIds04E.Dispatch), lofts = city.FindVenue("lofts"), pressure = city.FindVenue(VenueIds04E.Pressure);
            if (dispatch != null && dispatch.Available)
                AddLight("Cinder Dispatch frontage", InhabitedCityBindings04C.LocalToWorld(dispatch.Origin, dispatch.Yaw, new Vector3(1.2f, 3.05f, -1.6f)), new Color(1f, .72f, .45f), 9f, 1.1f, false);
            if (lofts != null && lofts.Available)
                AddLight("Veyra Lofts entrance", InhabitedCityBindings04C.LocalToWorld(lofts.Origin, lofts.Yaw, new Vector3(0f, 3.0f, -1.1f)), new Color(1f, .8f, .55f), 7f, 1.0f, false);
            if (ConduitEnd != Vector3.zero)
                AddLight("Warm conduit end", ConduitEnd + Vector3.up * .3f, new Color(1f, .5f, .2f), 5f, 1.4f, true);
            if (pressure != null && pressure.Available)
            {
                _hasPressure = true;
                _pressureDoor = pressure.Origin;
                // Fuse cabinet top, the vent line valve and the nest corner's cracked wall vent (venue-local positions).
                _sparkPoints[0] = InhabitedCityBindings04C.LocalToWorld(pressure.Origin, pressure.Yaw, new Vector3(-6.95f, 2.05f, 9.6f));
                _sparkPoints[1] = InhabitedCityBindings04C.LocalToWorld(pressure.Origin, pressure.Yaw, new Vector3(.6f, 1.75f, 17.05f));
                _sparkPoints[2] = InhabitedCityBindings04C.LocalToWorld(pressure.Origin, pressure.Yaw, new Vector3(-7.45f, .95f, 15.4f));
            }
        }

        private void AddLight(string name, Vector3 at, Color color, float range, float intensity, bool needsLit)
        {
            GameObject go = new GameObject("Arrival 04E practical / " + name);
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.range = range; l.intensity = intensity;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.Auto; l.enabled = false;
            _lights.Add(l);
            _lightNeedsLit.Add(needsLit);
        }

        public void OnConduitChanged(bool lit)
        {
            if (lit && ConduitStart != Vector3.zero) Underheart04ESounds.Play(UhSfx.ConduitPulse, ConduitStart, .8f);
            _lightClock = 0f;
        }

        private void Update()
        {
            float t = Time.time;
            bool lit = ArrivalScene04E.ConduitLit;
            // Receiver crystals: a slow breath, quicker and brighter while the conduit asks to be followed.
            float period = lit ? 1.7f : 3.4f;
            float breath = .5f + .5f * Mathf.Sin(t * Mathf.PI * 2f / period);
            SetEmission(Pulse, PulseColor * ((lit ? .9f : .55f) + 1.6f * breath));
            SetEmission(RefugeGlow, RefugeColor * (.55f + .5f * (.5f + .5f * Mathf.Sin(t * 1.3f))));
            SetEmission(Aviation, Mathf.Repeat(t, 2f) < .45f ? new Color(1f, .12f, .06f) * 2.4f : new Color(.15f, .02f, .01f));
            for (int i = 0; i < 3; i++)
            {
                float k = lit ? .6f + 2.4f * Mathf.Max(0f, Mathf.Sin((t * 1.4f - i / 3f) * Mathf.PI * 2f)) : .28f + .06f * Mathf.Sin(t * .7f + i);
                SetEmission(ConduitGlow[i], ConduitColor * k);
            }
            if (BeaconLens != null) BeaconLens.Rotate(0f, 150f * Time.deltaTime, 0f, Space.Self);
            _nightClock -= Time.unscaledDeltaTime;
            if (_nightClock <= 0f)
            {
                _nightClock = 1f;
                _night = Game.Atmo != null ? Game.Atmo.NightFactor : .5f;
                SetEmission(Lamp, LampColor * Mathf.Lerp(.35f, 1.5f, _night));
            }
            bool playing = Game.Player != null && Game.State != GameState.Title && Game.State != GameState.Loading;
            Vector3 player = playing ? Game.PlayerPosition : Vector3.zero;
            _lightClock -= Time.unscaledDeltaTime;
            if (_lightClock <= 0f) { _lightClock = .4f; UpdateLights(playing, player, lit); }
            if (!playing) { StopReceiverLoop(); return; }
            UpdateAudio(player, Time.deltaTime);
            UpdateSparks(player, Time.deltaTime);
            _zoneClock -= Time.unscaledDeltaTime;
            if (_zoneClock <= 0f) { _zoneClock = .25f; UpdateZone(player); }
        }

        private static void SetEmission(Material m, Color c)
        {
            if (m != null && m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
        }

        /// <summary>At most two shadowless practicals near the player (one on Low); the conduit light only while lit. The
        /// nearest venue keeps its own three, so the arrival never runs more than five point lights at once.</summary>
        private void UpdateLights(bool playing, Vector3 player, bool lit)
        {
            int budget = Settings.Graphics == GraphicsPreset.Low ? 1 : 2;
            for (int i = 0; i < _lights.Count; i++) if (_lights[i] != null) _lights[i].enabled = false;
            if (!playing || (Game.Inner != null && Game.Inner.IsInner)) return;
            for (int slot = 0; slot < budget; slot++)
            {
                int best = -1;
                float bestD = 70f * 70f;
                for (int i = 0; i < _lights.Count; i++)
                {
                    Light l = _lights[i];
                    if (l == null || l.enabled || (_lightNeedsLit[i] && !lit)) continue;
                    float d = (l.transform.position - player).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best < 0) break;
                _lights[best].enabled = true;
            }
        }

        private void UpdateAudio(Vector3 player, float dt)
        {
            if (MastHead == Vector3.zero) return;
            float d = MathUtil.FlatDistance(player, MastHead);
            if (d < 45f && _receiverLoop == 0) _receiverLoop = Underheart04ESounds.StartLoop(UhLoop.Receiver, MastHead, .35f);
            else if (d > 55f) StopReceiverLoop();
            if (_receiverLoop != 0) Underheart04ESounds.UpdateLoop(_receiverLoop, MastHead, .35f, 1f);
            _pingTimer -= dt;
            if (_pingTimer <= 0f)
            {
                _pingTimer = 14f;
                if (d < 45f) Underheart04ESounds.Play(UhSfx.ReceiverPing, MastHead, .45f);
            }
        }

        private void StopReceiverLoop()
        {
            if (_receiverLoop == 0) return;
            Underheart04ESounds.StopLoop(_receiverLoop);
            _receiverLoop = 0;
        }

        /// <summary>Short spark bursts in the Pressure House while the campaign has not calmed it, only when the player is near.</summary>
        private void UpdateSparks(Vector3 player, float dt)
        {
            if (!_hasPressure || Game.Fx == null || !Game.IsPlaying || GameTime.Paused || !ArrivalScene04E.PressureSparksActive) return;
            if (MathUtil.FlatDistance(player, _pressureDoor) > 32f) return;
            _sparkTimer -= dt;
            if (_sparkTimer > 0f) return;
            _sparkTimer = .7f + (float)_rand.NextDouble() * 1.7f;
            int which = _rand.Next(3);
            Vector3 at = _sparkPoints[which];
            Vector3 v = new Vector3(((float)_rand.NextDouble() - .5f) * 2f, 1.2f + (float)_rand.NextDouble(), ((float)_rand.NextDouble() - .5f) * 2f);
            Game.Fx.Sparks(at, v, 6 + _rand.Next(9));
            if (_rand.Next(3) == 0) Underheart04ESounds.Play(UhSfx.HeatTick, at, .3f, .9f + (float)_rand.NextDouble() * .3f);
        }

        /// <summary>Inside the Pressure House the ambience muffles (service interior); the Underheart owns it once below ground.</summary>
        private void UpdateZone(Vector3 player)
        {
            bool inside = Game.Inhabited != null && Game.Inhabited.OccupiedId == VenueIds04E.Pressure;
            bool underground = Game.Underheart != null && Game.Underheart.IsUnderground(player);
            if (inside && !underground)
            {
                if (Underheart04ESounds.Zone == UhZone.Surface) { Underheart04ESounds.SetZone(UhZone.ServiceInterior); _zoneSet = true; }
                return;
            }
            if (!_zoneSet) return;
            if (!underground && Underheart04ESounds.Zone == UhZone.ServiceInterior) Underheart04ESounds.SetZone(UhZone.Surface);
            _zoneSet = false;
        }

        // ------------------------------------------------------------------ session

        public void ResetAll()
        {
            ArrivalScene04E.SetConduitLit(false);
            ArrivalScene04E.ClearPressureSparksOverride();
        }

        public void Capture(SaveData d) { }

        public void Restore(SaveData d) { ResetAll(); }

        private void OnDisable()
        {
            foreach (Light l in _lights) if (l != null) l.enabled = false;
            StopReceiverLoop();
        }

        private void OnDestroy()
        {
            StopReceiverLoop();
            foreach (Mesh m in OwnedMeshes) ArtInhabitedCity04C.Release(m);
            foreach (Material m in _materials) ArtInhabitedCity04C.Release(m);
            OwnedMeshes.Clear();
            _materials.Clear();
        }
    }
}
