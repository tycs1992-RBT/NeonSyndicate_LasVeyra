using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting, reflections: one realtime reflection probe per distinct area (the nine districts, the canal,
    /// the desert quadrants, the Frostline, each Underheart region and each inhabited venue room), sized to the area's
    /// box, rendered by script with time slicing (one probe at a time, one face per frame) and re-rendered when the sky
    /// changed (Atmosphere.EnvironmentStamp) or the probe just came into use. The budget follows the preset through
    /// ReflectionProbeRules: Low none, Medium one camera-following probe at 64, High the nearest 4 areas at 128, Ultra the
    /// nearest 8 at 256. Probes blend at their box edges; interiors and caves use box projection and a higher importance
    /// than the districts, so chrome inside a venue reflects the room rather than the sky. Settings.AreaReflections is
    /// the off switch. Created by Atmosphere.Init; the Underheart and venue areas are added when those systems come up.
    /// </summary>
    public sealed class ReflectionProbes : MonoBehaviour
    {
        /// <summary>The live system (null before Atmosphere.Init or when creation failed).</summary>
        public static ReflectionProbes Instance { get; private set; }
        /// <summary>True while area probes are rendering for the current preset (NeonPost's camera probe is then redundant).</summary>
        public static bool Active { get { return Instance != null && Instance._active; } }

        private sealed class Slot
        {
            public ProbeArea Area;
            public ReflectionProbe Probe;
            public int RenderedStamp = int.MinValue;
            public int RenderStampPending;
            public float LastRender = -999f;
            public int RenderId = -1;
            public bool Live;
        }

        private readonly List<ProbeArea> _areas = new List<ProbeArea>();
        private readonly Dictionary<string, Slot> _slots = new Dictionary<string, Slot>();
        private readonly List<Slot> _live = new List<Slot>();
        private Slot _rendering;
        private ReflectionProbe _cameraProbe;
        private float _cameraTimer;
        private int _cameraRender = -1;
        private float _selectTimer;
        private float _discoverTimer;
        private bool _underheartAdded, _venuesAdded;
        private bool _active;
        private GraphicsPreset _preset = (GraphicsPreset)(-1);
        private int _renders;

        public int AreaCount { get { return _areas.Count; } }
        public int LiveCount { get { return _live.Count; } }
        public int RenderCount { get { return _renders; } }

        /// <summary>Creates the system under the Atmosphere (once).</summary>
        public static ReflectionProbes Create(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("Reflection Probes");
            if (parent != null) go.transform.SetParent(parent, false);
            ReflectionProbes rp = go.AddComponent<ReflectionProbes>();
            Instance = rp;
            rp._areas.AddRange(ReflectionProbeRules.CityAreas());
            rp.PlaceCityCaptures();
            return rp;
        }

        /// <summary>Moves each district's capture point to the nearest road node, so it never sits inside a building.</summary>
        private void PlaceCityCaptures()
        {
            CityLayout L = Game.Layout;
            if (L == null) return;
            foreach (ProbeArea a in _areas)
            {
                if (a.Kind != ProbeAreaKind.Outdoor || a.Id == "canal") continue;
                int n = L.NearestNode(a.Capture);
                if (n < 0 || n >= L.Nodes.Count) continue;
                Vector3 p = L.Nodes[n].Pos;
                if (!a.Contains(new Vector3(p.x, a.Center.y, p.z))) continue;
                a.Capture = new Vector3(p.x, p.y + 7f, p.z);
            }
        }

        /// <summary>Adds the Underheart regions once the underground world is ready, and the venue rooms once they stand.</summary>
        private void Discover()
        {
            if (!_underheartAdded && Game.Underheart != null && Game.Underheart.Ready && Game.Underheart.Layout != null)
            {
                _underheartAdded = true;
                try { _areas.AddRange(ReflectionProbeRules.UnderheartAreas(Game.Underheart.Layout, Game.Underheart.Frame)); }
                catch (System.Exception e) { Log.Exception("ReflectionProbes.Underheart", e); }
            }
            if (!_venuesAdded && Game.Inhabited != null && Game.Inhabited.IsOperational)
            {
                int added = 0, pending = 0;
                foreach (VenueBinding04C v in Game.Inhabited.Venues)
                {
                    if (v == null) continue;
                    if (!v.Available || v.View == null) { pending++; continue; }
                    string id = v.Definition != null && !string.IsNullOrEmpty(v.Definition.Id) ? v.Definition.Id : v.View.name;
                    if (_slots.ContainsKey("venue_" + id) || HasArea("venue_" + id)) continue;
                    try
                    {
                        _areas.Add(ReflectionProbeRules.VenueArea(id, v.View.transform.position, v.View.transform.eulerAngles.y, v.View.IndoorHeight));
                        added++;
                    }
                    catch (System.Exception e) { Log.Exception("ReflectionProbes.Venue", e); }
                }
                if (pending == 0) _venuesAdded = true;
                if (added > 0) _selectTimer = 0f;
            }
        }

        private bool HasArea(string id)
        {
            for (int i = 0; i < _areas.Count; i++) if (_areas[i].Id == id) return true;
            return false;
        }

        private void Update()
        {
            GraphicsPreset q = Settings.Graphics;
            bool want = Settings.AreaReflections && !RenderPath.IsURP && q >= GraphicsPreset.Medium;
            if (q != _preset)
            {
                _preset = q;
                // A new preset means new resolutions: drop every probe and rebuild on demand.
                foreach (Slot s in _slots.Values) if (s.Probe != null) Destroy(s.Probe.gameObject);
                _slots.Clear();
                _live.Clear();
                _rendering = null;
                if (_cameraProbe != null) { Destroy(_cameraProbe.gameObject); _cameraProbe = null; }
            }
            if (!want)
            {
                if (_active) SetAllLive(false);
                _active = false;
                return;
            }
            _discoverTimer -= Time.unscaledDeltaTime;
            if (_discoverTimer <= 0f) { _discoverTimer = 2f; Discover(); }
            Vector3 eye = Camera.main != null ? Camera.main.transform.position : Game.PlayerPosition;
            if (ReflectionProbeRules.UsesCameraProbe(q))
            {
                _active = true;
                UpdateCameraProbe(eye, q);
                return;
            }
            _active = true;
            _selectTimer -= Time.unscaledDeltaTime;
            if (_selectTimer <= 0f)
            {
                _selectTimer = 0.75f;
                Select(eye, q);
            }
            Schedule(q);
        }

        /// <summary>Medium: one probe that follows the camera, re-rendered every few seconds.</summary>
        private void UpdateCameraProbe(Vector3 eye, GraphicsPreset q)
        {
            if (_cameraProbe == null)
            {
                GameObject go = new GameObject("Area Reflections (camera)");
                go.transform.SetParent(transform, false);
                _cameraProbe = go.AddComponent<ReflectionProbe>();
                Configure(_cameraProbe, ProbeAreaKind.Outdoor, new Vector3(300f, 160f, 300f), false, ReflectionProbeRules.Resolution(q));
                _cameraProbe.importance = 12;
                _cameraProbe.farClipPlane = 180f;
                _cameraTimer = 0f;
            }
            _cameraTimer -= Time.unscaledDeltaTime;
            bool busy = _cameraRender >= 0 && !_cameraProbe.IsFinishedRendering(_cameraRender);
            if (_cameraTimer > 0f || busy) return;
            _cameraProbe.transform.position = new Vector3(eye.x, WorldBounds.IsUnderground(eye) ? eye.y - 1f : Mathf.Max(eye.y - 1f, 1.5f), eye.z);
            _cameraTimer = ReflectionProbeRules.MinRenderInterval(q);
            _cameraRender = _cameraProbe.RenderProbe();
            _renders++;
        }

        private void Configure(ReflectionProbe p, ProbeAreaKind kind, Vector3 size, bool boxProjection, int resolution)
        {
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            p.size = size;
            p.center = Vector3.zero;
            p.boxProjection = boxProjection;
            p.importance = ReflectionProbeRules.Importance(kind);
            p.blendDistance = ReflectionProbeRules.BlendDistance(kind);
            p.hdr = true;
            p.resolution = resolution;
            p.shadowDistance = 0f;
            p.nearClipPlane = 0.3f;
            p.clearFlags = ReflectionProbeClearFlags.Skybox;
            p.cullingMask = Layers.WorldMask;
            p.intensity = 1f;
        }

        /// <summary>Picks the nearest areas for the preset, creating probes for new ones and sleeping the rest.</summary>
        private void Select(Vector3 eye, GraphicsPreset q)
        {
            int count = ReflectionProbeRules.LiveCount(q);
            List<int> nearest = ReflectionProbeRules.Nearest(_areas, eye, count);
            foreach (Slot s in _live) s.Live = false;
            _live.Clear();
            foreach (int i in nearest)
            {
                ProbeArea a = _areas[i];
                Slot s;
                if (!_slots.TryGetValue(a.Id, out s))
                {
                    GameObject go = new GameObject("Reflection " + a.Id);
                    go.transform.SetParent(transform, false);
                    ReflectionProbe p = go.AddComponent<ReflectionProbe>();
                    Configure(p, a.Kind, a.Size, a.Kind != ProbeAreaKind.Outdoor, ReflectionProbeRules.Resolution(q));
                    p.farClipPlane = ReflectionProbeRules.FarClip(a);
                    // The probe sits at its capture point; the box is offset so it still covers the whole area.
                    go.transform.position = a.Capture;
                    p.center = a.Center - a.Capture;
                    s = new Slot { Area = a, Probe = p };
                    _slots[a.Id] = s;
                }
                s.Live = true;
                _live.Add(s);
            }
            foreach (Slot s in _slots.Values)
            {
                if (s.Probe == null) continue;
                bool on = s.Live && s.RenderedStamp != int.MinValue;
                // A probe that has never rendered stays off until its first render finishes (no black reflections).
                if (s.Live && s.RenderedStamp == int.MinValue) on = s == _rendering;
                if (s.Probe.gameObject.activeSelf != on && (on || s != _rendering)) s.Probe.gameObject.SetActive(on);
            }
        }

        private void SetAllLive(bool live)
        {
            foreach (Slot s in _slots.Values)
            {
                s.Live = live;
                if (s.Probe != null) s.Probe.gameObject.SetActive(live && s.RenderedStamp != int.MinValue);
            }
            if (!live) _live.Clear();
        }

        /// <summary>One probe renders at a time: the stalest live probe whose minimum interval has passed, nearest first.</summary>
        private void Schedule(GraphicsPreset q)
        {
            int stamp = Game.Atmo != null ? Game.Atmo.EnvironmentStamp : 0;
            if (_rendering != null)
            {
                if (_rendering.Probe == null) { _rendering = null; }
                else if (_rendering.Probe.IsFinishedRendering(_rendering.RenderId))
                {
                    _rendering.RenderedStamp = _rendering.RenderStampPending;
                    _rendering.LastRender = Time.unscaledTime;
                    if (!_rendering.Live) _rendering.Probe.gameObject.SetActive(false);
                    _rendering = null;
                }
                else return;
            }
            float minInterval = ReflectionProbeRules.MinRenderInterval(q);
            Slot pick = null;
            for (int i = 0; i < _live.Count; i++)
            {
                Slot s = _live[i];
                if (s.Probe == null) continue;
                bool never = s.RenderedStamp == int.MinValue;
                bool stale = s.RenderedStamp != stamp;
                if (!never && (!stale || Time.unscaledTime - s.LastRender < minInterval)) continue;
                pick = s;
                if (never) break;
            }
            if (pick == null) return;
            pick.Probe.gameObject.SetActive(true);
            pick.RenderStampPending = stamp;
            pick.RenderId = pick.Probe.RenderProbe();
            _rendering = pick;
            _renders++;
        }

        /// <summary>Forces every live probe to render again (a preset or settings change).</summary>
        public void Invalidate()
        {
            foreach (Slot s in _slots.Values) s.LastRender = -999f;
            _selectTimer = 0f;
        }

        /// <summary>Plain-language state for the debug page.</summary>
        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_active ? "on" : "off").Append(", areas ").Append(_areas.Count).Append(", live ").Append(_live.Count).Append(", renders ").Append(_renders);
            foreach (Slot s in _live)
            {
                sb.Append("\n  ").Append(s.Area.Id).Append(s.RenderedStamp == int.MinValue ? " (pending)" : "").Append(s == _rendering ? " (rendering)" : "");
            }
            return sb.ToString();
        }

        private void OnDestroy()
        {
            foreach (Slot s in _slots.Values) if (s.Probe != null) Destroy(s.Probe.gameObject);
            _slots.Clear();
            if (_cameraProbe != null) Destroy(_cameraProbe.gameObject);
            if (Instance == this) Instance = null;
        }

        public static void ResetStatics()
        {
            Instance = null;
        }
    }
}
