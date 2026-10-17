using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Per frame: the player's world context and Underheart region (banners, audio zone, the atmosphere's underground mode,
    /// the RegionChanged event), renderer streaming by region (current plus adjacent; the stair and the portal stream with
    /// the street near them), the practical light budget (the nearest few on, shadowless), heat zone visuals, sliding
    /// gates and the flowing lava. No per-frame allocation.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private const float StreamInterval = 0.25f;
        private const float StairStreamRange = 70f;
        private const float PortalStreamRange = 450f;

        private float _streamClock, _lightClock;
        private readonly Dictionary<string, float> _lastBanner = new Dictionary<string, float>(StringComparer.Ordinal);
        private Dictionary<string, List<string>> _adjacent;
        private readonly HashSet<string> _activeGroups = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _groupScratch = new List<string>(16);
        private string _pendingNotice = "";
        private MaterialPropertyBlock _ringBlock;
        private int _lightsOn;
        private readonly int[] _nearest = new int[12];
        private readonly float[] _nearestD = new float[12];

        private void Update()
        {
            WorldContext.Refresh();
            if (!Ready) return;
            PlayerController p = Game.Player;
            bool live = p != null && (Game.IsPlaying || Game.State == GameState.Cutscene || Game.State == GameState.Wasted);
            Vector3 pos = live ? Game.PlayerPosition : Vector3.zero;
            int cell = live ? CellAtWorld(pos, _playerCell) : -1;
            _playerCell = cell;
            // The region (and the underground mood) starts a step below the street, so the top of the stair and the
            // mouth of the portal still belong to the city.
            bool below = cell >= 0 && pos.y < WorldBounds.UndergroundCeiling + 0.5f;
            string region = below ? _layout.Cells[cell].Region : "";
            bool changed = region != PlayerRegion;
            if (changed) OnRegionChange(PlayerRegion, region);
            UpdateAtmosphereTarget(cell, pos, below, live);
            _streamClock -= Time.unscaledDeltaTime;
            if (changed || _streamClock <= 0f)
            {
                _streamClock = StreamInterval;
                UpdateStreaming(pos, region, changed);
            }
            _lightClock -= Time.unscaledDeltaTime;
            if (_lightClock <= 0f)
            {
                _lightClock = StreamInterval;
                UpdateLights(ViewPoint(live, pos));
            }
            UpdateHazards(Time.deltaTime, pos, cell);
            UpdateHazardVisuals();
            UpdateGates(Time.deltaTime);
            UpdateRouteCache();
            Mats.UhScrollLava(Time.time);
            if (_pendingNotice.Length > 0 && Game.IsPlaying && Game.Hud != null && Game.Hud.FadeAlpha < 0.2f)
            {
                Game.Hud.Notify(_pendingNotice, 6f);
                _pendingNotice = "";
            }
        }

        /// <summary>Where the view is: the camera when there is one (the title flyover too), else the player.</summary>
        private static Vector3 ViewPoint(bool live, Vector3 player)
        {
            if (Game.Cam != null) return Game.Cam.transform.position;
            Camera main = Camera.main;
            if (main != null) return main.transform.position;
            return player;
        }

        // ================================================================== regions

        private static UhZone ZoneOf(string region)
        {
            switch (region)
            {
                case UnderheartIds.ServiceLevel: return UhZone.ServiceInterior;
                case UnderheartIds.LanternRefuge: return UhZone.Refuge;
                case UnderheartIds.EmberCanals: return UhZone.Canals;
                case UnderheartIds.PyreGardens: return UhZone.Gardens;
                case UnderheartIds.CrucibleWorkshops: return UhZone.Workshops;
                case UnderheartIds.AshReliquary: return UhZone.Reliquary;
                case UnderheartIds.FoundryCrown: return UhZone.Crown;
                default: return UhZone.Surface;
            }
        }

        /// <summary>The one-line banner subtitle for a region.</summary>
        public static string RegionBlurb(string region)
        {
            switch (region)
            {
                case UnderheartIds.ServiceLevel: return "The Pressure House basement. The service tunnel leads down to the refuge.";
                case UnderheartIds.LanternRefuge: return "Safe ground. Rest, fit your gear and read the contract board.";
                case UnderheartIds.EmberCanals: return "Basalt channels and lava. Blue vents cool you; orange rings breathe heat.";
                case UnderheartIds.PyreGardens: return "A living fire-crystal cavern. Not everything here is hostile.";
                case UnderheartIds.CrucibleWorkshops: return "Abandoned workrooms, salvage and a sealed vault.";
                case UnderheartIds.AshReliquary: return "A buried civic archive. Quiet, and watched.";
                case UnderheartIds.FoundryCrown: return "The heat source of the Underheart.";
                default: return "";
            }
        }

        private void OnRegionChange(string from, string to)
        {
            PlayerRegion = to;
            bool first = false;
            if (to.Length > 0 && !_discovered.Contains(to))
            {
                _discovered.Add(to);
                first = true;
            }
            try { Underheart04ESounds.SetZone(ZoneOf(to)); }
            catch (Exception e) { Log.Exception("Underheart audio zone", e); }
            if (Game.Hud != null && Game.IsPlaying)
            {
                if (to.Length > 0)
                {
                    float last;
                    bool recent = _lastBanner.TryGetValue(to, out last) && Time.unscaledTime - last < 25f;
                    if (first || !recent)
                    {
                        UhRegionInfo info = _layout.Region(to);
                        string depth = info != null ? Mathf.RoundToInt(info.Depth) + " m below the street. " : "";
                        Game.Hud.Banner((first ? "NEW AREA: " : "") + UnderheartIds.RegionTitle(to).ToUpperInvariant(), depth + RegionBlurb(to), info != null ? info.Tint : UIKit.Cyan, first ? 5f : 3f);
                        _lastBanner[to] = Time.unscaledTime;
                    }
                }
                else if (from.Length > 0) Game.Hud.Notify("Back on the street.", 2.5f);
            }
            RaiseRegionChanged(from, to);
        }

        // ================================================================== atmosphere

        private Vector3 _lastAtmoPos;

        private void UpdateAtmosphereTarget(int cell, Vector3 pos, bool below, bool live)
        {
            Atmosphere atmo = Game.Atmo;
            if (atmo == null) return;
            if (!below) atmo.UndergroundTarget = 0f;
            else
            {
                UhCell c = _layout.Cells[cell];
                atmo.UndergroundRegion = c.Region;
                // The service level is right under the street; everything deeper is fully enclosed.
                atmo.UndergroundTarget = c.MinFloor > -10f ? 0.85f : 1f;
            }
            // A teleport (lift, load, respawn, debug) snaps the mood instead of fading it across the fade-in.
            bool jump = live && (pos - _lastAtmoPos).sqrMagnitude > 8f * 8f;
            _lastAtmoPos = pos;
            if ((_snapAtmosphere || jump) && live)
            {
                atmo.SnapUnderground04E();
                _snapAtmosphere = false;
            }
        }

        // ================================================================== streaming

        private void BuildAdjacency()
        {
            _adjacent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (UhPortal d in _layout.Portals)
            {
                string a = UnderheartBuilder.GroupOf(_layout.Cells[d.A]), b = UnderheartBuilder.GroupOf(_layout.Cells[d.B]);
                if (a == b) continue;
                Link(a, b);
                Link(b, a);
            }
        }

        private void Link(string a, string b)
        {
            List<string> list;
            if (!_adjacent.TryGetValue(a, out list)) { list = new List<string>(); _adjacent[a] = list; }
            if (!list.Contains(b)) list.Add(b);
        }

        /// <summary>Shows the render groups the player can see: the current region and its neighbours, plus the stair and
        /// the portal when the player is near them on the street. Colliders never stream.</summary>
        private void UpdateStreaming(Vector3 pos, string region, bool force)
        {
            if (_builder == null) return;
            if (_adjacent == null) BuildAdjacency();
            _groupScratch.Clear();
            bool live = Game.Player != null && (Game.IsPlaying || Game.State == GameState.Cutscene || Game.State == GameState.Wasted);
            if (live)
            {
                if (region.Length > 0)
                {
                    int cell = _playerCell;
                    string own = cell >= 0 ? UnderheartBuilder.GroupOf(_layout.Cells[cell]) : region;
                    AddGroup(own);
                    AddGroup(region);
                    List<string> next;
                    if (_adjacent.TryGetValue(own, out next)) foreach (string n in next) AddGroup(n);
                    if (own != region && _adjacent.TryGetValue(region, out next)) foreach (string n in next) AddGroup(n);
                }
            }
            // The two openings show from the street (and on the title flyover) when the view is near them.
            Vector3 view = ViewPoint(live, pos);
            if (StairOpen && MathUtil.FlatDistance(view, SurfaceEntrance) < StairStreamRange && view.y > -8f)
            {
                AddGroup(UnderheartBuilder.GroupEntrance);
                AddGroup(UnderheartIds.ServiceLevel);
            }
            if (_layout.Portal != null && MathUtil.FlatDistance(view, _layout.Portal.TopWorld) < PortalStreamRange && view.y > -8f)
                AddGroup(UnderheartBuilder.GroupPortal);
            foreach (KeyValuePair<string, Transform> kv in _builder.Groups)
            {
                bool want = _groupScratch.Contains(kv.Key);
                if (kv.Value.gameObject.activeSelf != want || force) kv.Value.gameObject.SetActive(want);
            }
            _activeGroups.Clear();
            foreach (string g in _groupScratch) _activeGroups.Add(g);
        }

        private void AddGroup(string g)
        {
            if (!_groupScratch.Contains(g)) _groupScratch.Add(g);
        }

        /// <summary>Keeps the nearest practical lights in the visible groups on (8, or 5 on Low; all shadowless).</summary>
        private void UpdateLights(Vector3 from)
        {
            if (_builder == null) return;
            int budget = Settings.Graphics == GraphicsPreset.Low ? 5 : 8;
            int n = 0;
            for (int i = 0; i < _nearest.Length; i++) { _nearest[i] = -1; _nearestD[i] = float.MaxValue; }
            List<UnderheartBuilder.LightRef> lights = _builder.Lights;
            for (int i = 0; i < lights.Count; i++)
            {
                UnderheartBuilder.LightRef l = lights[i];
                if (l.Light == null || !_activeGroups.Contains(l.Group)) continue;
                float d = (l.World - from).sqrMagnitude;
                if (d > l.Light.range * l.Light.range * 4f + 400f) continue;
                // Insert into the sorted nearest list.
                if (d >= _nearestD[budget - 1]) continue;
                int k = budget - 1;
                while (k > 0 && _nearestD[k - 1] > d)
                {
                    _nearestD[k] = _nearestD[k - 1];
                    _nearest[k] = _nearest[k - 1];
                    k--;
                }
                _nearestD[k] = d;
                _nearest[k] = i;
            }
            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i].Light;
                if (light == null) continue;
                bool on = false;
                for (int k = 0; k < budget; k++) if (_nearest[k] == i) { on = true; break; }
                if (light.enabled != on) light.enabled = on;
                if (on) n++;
            }
            _lightsOn = n;
        }

        // ================================================================== visuals

        private void UpdateHazardVisuals()
        {
            if (_builder == null) return;
            if (_ringBlock == null) _ringBlock = new MaterialPropertyBlock();
            float t = Time.time;
            List<UnderheartBuilder.HazardVisual> list = _builder.HazardVisuals;
            for (int i = 0; i < list.Count; i++)
            {
                UnderheartBuilder.HazardVisual v = list[i];
                if (!_activeGroups.Contains(v.Group)) continue;
                UhHazard h = v.Hazard;
                if (h.Kind == UhHazardKind.HotVent)
                {
                    bool active = h.ActiveAt(t);
                    float warn = h.WarningAt(t);
                    if (v.Ring != null)
                    {
                        float glow = active ? 2.6f + Mathf.Sin(t * 14f) * 0.4f : 0.5f + warn * warn * 2.2f * (0.6f + 0.4f * Mathf.Sin(t * 9f));
                        _ringBlock.SetColor("_EmissionColor", v.RingColor * glow);
                        v.Ring.SetPropertyBlock(_ringBlock);
                    }
                    if (v.Particles != null)
                    {
                        ParticleSystem.EmissionModule e = v.Particles.emission;
                        e.rateOverTime = active ? 26f : 0f;
                    }
                }
                else if (h.Kind == UhHazardKind.Cooling && v.Ring != null)
                {
                    _ringBlock.SetColor("_EmissionColor", v.RingColor * (1.4f + 0.4f * Mathf.Sin(t * 2.4f + i)));
                    v.Ring.SetPropertyBlock(_ringBlock);
                }
            }
        }

        private void UpdateGates(float dt)
        {
            if (_builder == null) return;
            foreach (UnderheartBuilder.GateRef g in _builder.Gates)
            {
                if (g.Door == null) continue;
                bool open = _shortcuts.Contains(g.Shortcut);
                Vector3 target = open ? g.Open : g.Closed;
                if ((g.Door.localPosition - target).sqrMagnitude > 0.0001f)
                    g.Door.localPosition = Vector3.MoveTowards(g.Door.localPosition, target, dt * 1.6f);
                if (g.Blocker != null && g.Blocker.enabled == open) g.Blocker.enabled = !open;
            }
        }

        /// <summary>Snaps every gate to its saved state (load, new game).</summary>
        private void ApplyShortcutStates(bool snap)
        {
            if (_builder == null) return;
            foreach (UnderheartBuilder.GateRef g in _builder.Gates)
            {
                if (g.Door == null) continue;
                bool open = _shortcuts.Contains(g.Shortcut);
                if (snap) g.Door.localPosition = open ? g.Open : g.Closed;
                if (g.Blocker != null) g.Blocker.enabled = !open;
            }
        }
    }
}
