using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Game.Underheart: the physical fire-crystal network under Las Veyra (04E). Prepare runs before the city is built: it
    /// reads the Pressure House binding, chooses the layout variant that fits this place (UnderheartPlanner), plans the
    /// surface vehicle portal on a surveyed park lot and registers both ground openings with WorldApertures so CityBuilder
    /// cuts them. BuildWorld builds every cell after the city (UnderheartBuilder). The component then owns region context
    /// (WorldContext, banners, audio zones, the atmosphere's underground mode), renderer streaming by region, heat zones,
    /// shortcuts, rest points, the underground map and HUD panel, routing, saves and the F2 debug page. Partials: Runtime,
    /// Hazards, Shortcuts, Map, Save, Debug, Window (the descent window of "Down to the Crown").
    /// </summary>
    public sealed partial class UnderheartWorld : MonoBehaviour, IWorldSystem
    {
        public const string StairApertureId = "underheart_stair";
        public const string PortalApertureId = "underheart_portal";
        public const string ModuleId = "underheart";
        public const string PortalModuleId = "underheart_portal";

        /// <summary>Raised when the player moves between Underheart regions (from, to; "" is the surface).</summary>
        public event Action<string, string> RegionChanged;
        /// <summary>Raised when a shortcut opens (shortcut id).</summary>
        public event Action<string> ShortcutOpened;

        /// <summary>True once the cells exist and the first entrance is bound.</summary>
        public bool Ready { get; private set; }
        /// <summary>Plain-language state for the STATUS page (why it is unavailable when not Ready).</summary>
        public string Status { get; private set; } = "Not built yet.";
        /// <summary>The Pressure House door (the first entrance) in the live world, for routing. Zero when unbound.</summary>
        public Vector3 SurfaceEntrance { get; private set; }
        /// <summary>The region the player is in ("" on the surface).</summary>
        public string PlayerRegion { get; private set; } = "";

        /// <summary>The chosen layout (null when the Underheart is disabled).</summary>
        public UnderheartLayout Layout { get { return _layout; } }
        public UnderheartFrame Frame { get { return _frame; } }
        public UnderheartBuilder Builder { get { return _builder; } }
        /// <summary>True when the Pressure House stair is the live entrance (the venue was built in place).</summary>
        public bool StairOpen { get; private set; }
        /// <summary>True when the surface vehicle portal was surveyed and built.</summary>
        public bool PortalBuilt { get { return Ready && _layout != null && _layout.Portal != null; } }
        /// <summary>Why the vehicle portal was skipped ("" when it exists).</summary>
        public string PortalReason { get; private set; } = "";
        public string VariantReport { get; private set; } = "";

        private CityLayout _city;
        private UnderheartLayout _layout;
        private UnderheartFrame _frame;
        private VenueBinding04C _venue;
        private UnderheartBuilder _builder;
        private bool _prepared;
        private readonly HashSet<string> _shortcuts = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _discovered = new List<string>();
        private readonly HashSet<string> _cleared = new HashSet<string>(StringComparer.Ordinal);
        private ReadOnlyCollection<string> _discoveredView;
        private string _restAnchor = "";
        private int _playerCell = -1;
        private Bounds _worldBounds;
        private float _prepareMs, _buildMs;

        // ================================================================== lifecycle

        /// <summary>Before CityBuilder: bind the entrances to their parcels and register WorldApertures.</summary>
        public void Prepare(CityLayout layout)
        {
            WorldSystems.Register(this);
            RegisterDebugPage();
            _city = layout;
            _discoveredView = new ReadOnlyCollection<string>(_discovered);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                InhabitedCitySystem ic = Game.Inhabited;
                _venue = ic != null ? ic.FindVenue(VenueIds04E.Pressure) : null;
                if (ic == null)
                {
                    Disable("The inhabited city (04C) did not start, so the Pressure House and its stair do not exist.", "Restart the game; if it repeats, send the Console error.");
                    return;
                }
                if (_venue == null)
                {
                    Disable("The Pressure House is not in this build's venue catalog, so there is no stair down.", "Update to a build that includes the 04E venues.");
                    return;
                }
                if (_venue.Building == null)
                {
                    Disable("The Pressure House found no parcel in this city: " + _venue.Status, "The rest of the game is unaffected.");
                    return;
                }
                _frame = new UnderheartFrame(_venue.Origin.x, _venue.Origin.z, _venue.Yaw);
                UnderheartPlanner.Choice choice = UnderheartPlanner.Choose(layout, _frame, true);
                if (choice.Layout == null)
                {
                    Disable("No Underheart layout fits under the Pressure House here (" + (choice.Rejected.Count > 0 ? choice.Rejected[0] : "unknown") + ").", "The rest of the game is unaffected.");
                    return;
                }
                _layout = choice.Layout;
                VariantReport = choice.Report;
                PortalReason = choice.PortalPlanned ? "" : choice.PortalReason;
                List<string> problems = UnderheartRules.Validate(_layout);
                for (int i = 0; i < problems.Count && i < 6; i++) Log.Warn("Underheart layout: " + problems[i]);
                StairOpen = ic.EnabledAtBoot;
                if (StairOpen)
                    WorldApertures.Register(StairApertureId, _frame.RectToWorld(VenueIds04E.StairX0, VenueIds04E.StairZ0, VenueIds04E.StairX1, VenueIds04E.StairZ1));
                if (_layout.Portal != null) WorldApertures.Register(PortalApertureId, _layout.Portal.CutWorld);
                SurfaceEntrance = _venue.Outside;
                _worldBounds = ComputeBounds();
                BuildIndex();
                _prepared = true;
                Status = "Prepared under the Pressure House (" + VariantReport + "); building after the city.";
                Log.Info("UNDERHEART_04E prepare " + VariantReport + " cells=" + _layout.Cells.Count + " anchors=" + _layout.Anchors.Count +
                    (StairOpen ? " stair=open" : " stair=closed (04C off)") + (_layout.Portal != null ? " portal=" + _layout.Portal.LotText : " portal=skipped (" + PortalReason + ")"));
            }
            catch (Exception e)
            {
                Log.Exception("Underheart prepare", e);
                WorldApertures.Unregister(StairApertureId);
                WorldApertures.Unregister(PortalApertureId);
                _layout = null;
                _prepared = false;
                Disable("The Underheart could not be planned: " + e.Message, "The city is unchanged; send the Console error.");
            }
            finally
            {
                sw.Stop();
                _prepareMs = sw.ElapsedMilliseconds;
            }
        }

        private void Disable(string why, string next)
        {
            Status = "Disabled: " + why;
            Ready = false;
            ModuleStatus.Report(ModuleId, "Underheart world", ModuleState.Disabled, why, next);
            ModuleStatus.SetCount(ModuleId, 0, "underground rooms built");
            Log.Warn("Underheart disabled: " + why);
        }

        /// <summary>After the city: build every cell (colliders always, renderers streamed by region).</summary>
        public IEnumerator BuildWorld()
        {
            if (!_prepared || _layout == null) yield break;
            Stopwatch sw = Stopwatch.StartNew();
            _builder = new UnderheartBuilder(_layout, _frame, transform);
            yield return _builder.Build(null);
            sw.Stop();
            _buildMs = sw.ElapsedMilliseconds;
            try { FinishBuild(); }
            catch (Exception e)
            {
                Log.Exception("Underheart finish", e);
                Ready = false;
                Status = "Disabled: the Underheart was built but could not start (" + e.Message + ").";
                ModuleStatus.Report(ModuleId, "Underheart world", ModuleState.Failed, e.Message, "The rest of the game still runs. Send the Console error along.");
            }
        }

        private void FinishBuild()
        {
            Physics.SyncTransforms();
            bool venueBuilt = _venue != null && _venue.Available;
            StairOpen = StairOpen && venueBuilt;
            WorldBounds.UndergroundProbe = IsUnderground;
            WorldBounds.CameraLimitProbe = CameraLimitAt;
            CreateInteractions();
            CreateGardenCircuit04N();
            ApplyShortcutStates(true);
            RegisterUi();
            Ready = true;
            UpdateStreaming(Vector3.zero, "", true);
            ModuleState state = ModuleState.Loaded;
            List<string> notes = new List<string>();
            if (!StairOpen) { state = ModuleState.Partial; notes.Add(venueBuilt ? "the Pressure House stair is closed" : "the Pressure House did not build, so its stair is closed"); }
            if (_layout.Portal == null) { state = ModuleState.Partial; notes.Add("no vehicle portal (" + PortalReason + ")"); }
            if (_builder.Problems.Count > 0) { state = ModuleState.Partial; notes.Add(_builder.Problems.Count + " build problem(s): " + _builder.Problems[0]); }
            string detail = notes.Count == 0 ? "" : string.Join("; ", notes.ToArray()) + ".";
            ModuleStatus.Report(ModuleId, "Underheart world", state, detail,
                state == ModuleState.Loaded ? "" : (StairOpen ? "Enter through the Pressure House stair." : (_layout.Portal != null ? "Drive in through the vehicle portal." : "The surface city is unaffected.")));
            ModuleStatus.SetCount(ModuleId, _builder.CellsBuilt, "underground rooms built");
            ModuleStatus.Report(PortalModuleId, "Underheart vehicle portal", _layout.Portal != null ? ModuleState.Loaded : ModuleState.Disabled,
                _layout.Portal != null ? "" : PortalReason, _layout.Portal != null ? "" : "Walk in through the Pressure House stair.");
            ModuleStatus.SetCount(PortalModuleId, _layout.Portal != null ? 1 : 0, "vehicle ramp");
            Status = (state == ModuleState.Loaded ? "Ready: " : "Partly ready: ") + "7 regions, " + _builder.CellsBuilt + " rooms, " + _layout.Anchors.Count + " named places; " +
                (StairOpen ? "stair in the Pressure House" : "stair closed") + "; " + (_layout.Portal != null ? "vehicle portal in the " + _layout.Portal.LotText : "no vehicle portal") + (detail.Length > 0 ? " (" + detail + ")" : ".");
            Log.Info("UNDERHEART_04E built rooms=" + _builder.CellsBuilt + " colliders=" + _builder.ColliderCount + " renderers=" + _builder.RendererCount +
                " triangles=" + _builder.TriangleCount + " lights=" + _builder.Lights.Count + " props=" + _builder.PropCount + " prepareMs=" + _prepareMs + " buildMs=" + _buildMs + " / " + Status);
        }

        private void OnDestroy()
        {
            if (WorldBounds.UndergroundProbe != null && WorldBounds.UndergroundProbe.Target == (object)this) WorldBounds.UndergroundProbe = null;
            if (WorldBounds.CameraLimitProbe != null && WorldBounds.CameraLimitProbe.Target == (object)this) WorldBounds.CameraLimitProbe = null;
            UnregisterUi();
            if (_builder != null) _builder.ReleaseMeshes();
            if (Game.Underheart == this) Game.Underheart = null;
        }

        // ================================================================== spatial queries

        private Bounds ComputeBounds()
        {
            Bounds b = new Bounds();
            bool first = true;
            foreach (UhCell c in _layout.Cells)
            {
                RectF w = _frame.CellToWorld(c);
                Bounds cb = new Bounds();
                cb.SetMinMax(new Vector3(w.X0, c.MinFloor - 1f, w.Z0), new Vector3(w.X1, c.Top + (c.OpenTop ? 4f : 0.5f), w.Z1));
                if (first) { b = cb; first = false; }
                else b.Encapsulate(cb);
            }
            return b;
        }

        // A coarse grid over the venue frame so point queries test only a few cells.
        private const float IndexCell = 16f;
        private float _ix0, _iz0;
        private int _inx, _inz;
        private List<int>[] _index;

        private void BuildIndex()
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (UhCell c in _layout.Cells) { x0 = Mathf.Min(x0, c.X0); z0 = Mathf.Min(z0, c.Z0); x1 = Mathf.Max(x1, c.X1); z1 = Mathf.Max(z1, c.Z1); }
            _ix0 = x0 - 1f;
            _iz0 = z0 - 1f;
            _inx = Mathf.Max(1, Mathf.CeilToInt((x1 - x0 + 2f) / IndexCell));
            _inz = Mathf.Max(1, Mathf.CeilToInt((z1 - z0 + 2f) / IndexCell));
            _index = new List<int>[_inx * _inz];
            foreach (UhCell c in _layout.Cells)
            {
                int ax = Mathf.Clamp(Mathf.FloorToInt((c.X0 - _ix0) / IndexCell), 0, _inx - 1), bx = Mathf.Clamp(Mathf.FloorToInt((c.X1 - _ix0) / IndexCell), 0, _inx - 1);
                int az = Mathf.Clamp(Mathf.FloorToInt((c.Z0 - _iz0) / IndexCell), 0, _inz - 1), bz = Mathf.Clamp(Mathf.FloorToInt((c.Z1 - _iz0) / IndexCell), 0, _inz - 1);
                for (int i = ax; i <= bx; i++)
                    for (int k = az; k <= bz; k++)
                    {
                        int slot = k * _inx + i;
                        if (_index[slot] == null) _index[slot] = new List<int>(4);
                        _index[slot].Add(c.Index);
                    }
            }
        }

        /// <summary>The cell containing a world point (-1 when none). The hint cell is tested first.</summary>
        public int CellAtWorld(Vector3 world, int hint = -1)
        {
            if (_layout == null || _index == null || !MathUtil.IsFinite(world)) return -1;
            if (!_worldBounds.Contains(world)) return -1;
            Vector3 p = _frame.ToLocal(world);
            if (hint >= 0 && hint < _layout.Cells.Count && _layout.Cells[hint].Contains(p, 0f)) return hint;
            int ix = Mathf.FloorToInt((p.x - _ix0) / IndexCell), iz = Mathf.FloorToInt((p.z - _iz0) / IndexCell);
            if (ix < 0 || iz < 0 || ix >= _inx || iz >= _inz) return -1;
            List<int> list = _index[iz * _inx + ix];
            if (list == null) return -1;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                UhCell c = _layout.Cells[list[i]];
                if (!c.Contains(p, 0f)) continue;
                float d = Mathf.Abs(p.y - c.FloorAt(p.x, p.z));
                if (d < bestD) { bestD = d; best = c.Index; }
            }
            return best;
        }

        /// <summary>True when the point lies inside an Underheart cell volume (3D).</summary>
        public bool IsUnderground(Vector3 p)
        {
            return Ready && p.y < WorldBounds.UndergroundCeiling && CellAtWorld(p) >= 0;
        }

        /// <summary>The region id at a point, or "" outside every cell.</summary>
        public string RegionAt(Vector3 p)
        {
            if (!Ready) return "";
            int c = CellAtWorld(p);
            return c >= 0 ? _layout.Cells[c].Region : "";
        }

        /// <summary>True inside a safe room (the refuge, the rest rooms): no damaging heat, no vehicles, no routine hostile
        /// spawns. Additive helper for the ecology and encounter streams.</summary>
        public bool IsSafeAt(Vector3 p)
        {
            if (!Ready) return false;
            int c = CellAtWorld(p);
            return c >= 0 && _layout.Cells[c].Safe;
        }

        private float CameraLimitAt(Vector3 p)
        {
            int c = CellAtWorld(p, _playerCell);
            return c >= 0 ? UnderheartRules.CameraLimit(_layout.Cells[c]) : float.PositiveInfinity;
        }

        /// <summary>A layout anchor's world position and yaw.</summary>
        public void AnchorWorld(UhAnchor a, out Vector3 pos, out float yaw)
        {
            pos = _frame.ToWorld(a.Pos);
            yaw = _frame.YawToWorld(a.Yaw);
        }

        /// <summary>A named anchor's live position and facing (ids in UnderheartIds.Anchors).</summary>
        public bool TryAnchor(string anchorId, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            if (_layout == null || !_prepared) return false;
            UhAnchor a = _layout.Anchor(anchorId);
            if (a == null) return false;
            AnchorWorld(a, out pos, out yaw);
            return true;
        }

        public bool ShortcutOpen(string id) { return !string.IsNullOrEmpty(id) && _shortcuts.Contains(id); }

        /// <summary>Encounter rooms cleared during this run (ordinary rooms; bosses persist through the reward ledger).</summary>
        public void MarkCleared(string roomId)
        {
            if (!string.IsNullOrEmpty(roomId)) _cleared.Add(roomId);
        }

        public bool IsCleared(string roomId) { return !string.IsNullOrEmpty(roomId) && _cleared.Contains(roomId); }

        /// <summary>Damaging heat at a point (lava, hot vents) in health per second; warm scenery returns false.</summary>
        public bool HazardAt(Vector3 p, out float heatPerSecond)
        {
            heatPerSecond = 0f;
            if (!Ready) return false;
            int c = CellAtWorld(p);
            if (c < 0) return false;
            // Only the hazards of the cell the point is in (no heat through a ceiling or a wall), scaled by a descent window.
            Vector3 local = _frame.ToLocal(p);
            float now = Time.time;
            float window = WindowHeatScale(_layout.Cells[c].Region);
            foreach (UhHazard h in _layout.Hazards)
            {
                if (h.Cell != c || h.Heat <= 0f || !h.ActiveAt(now) || !h.Contains(local, 0f)) continue;
                heatPerSecond = Mathf.Max(heatPerSecond, h.Heat * window);
            }
            return heatPerSecond > 0f;
        }

        /// <summary>True when a vehicle may drive or fuse at a point: the surface, or an Underheart cell built for vehicles
        /// (the broad transit tunnels). headroom is the clear height above the floor there.</summary>
        public bool VehicleSpaceAt(Vector3 p, out float headroom)
        {
            int c = Ready ? CellAtWorld(p) : -1;
            if (c < 0)
            {
                headroom = 60f;
                return !WorldBounds.IsUnderground(p);
            }
            UhCell cell = _layout.Cells[c];
            headroom = cell.OpenTop ? 60f : cell.Height;
            return cell.Vehicle;
        }

        /// <summary>Regions the player has entered at least once.</summary>
        public IList<string> DiscoveredRegions
        {
            get
            {
                if (_discoveredView == null) _discoveredView = new ReadOnlyCollection<string>(_discovered);
                return _discoveredView;
            }
        }

        private void RaiseRegionChanged(string from, string to)
        {
            if (RegionChanged == null) return;
            foreach (Delegate h in RegionChanged.GetInvocationList())
            {
                try { ((Action<string, string>)h)(from, to); }
                catch (Exception e) { Log.Exception("Underheart RegionChanged listener", e); }
            }
        }

        private void RaiseShortcutOpened(string id)
        {
            if (ShortcutOpened == null) return;
            foreach (Delegate h in ShortcutOpened.GetInvocationList())
            {
                try { ((Action<string>)h)(id); }
                catch (Exception e) { Log.Exception("Underheart ShortcutOpened listener", e); }
            }
        }
    }

    /// <summary>The Underheart's durable block (SaveData.Underheart). Append fields only.</summary>
    [Serializable]
    public sealed class UnderheartSave
    {
        public int Version = 1;
        public List<string> Shortcuts = new List<string>();
        public List<string> Discovered = new List<string>();
        public List<string> Cleared = new List<string>();
        /// <summary>The rest point the player last used ("" for none): underground saves resume there.</summary>
        public string RestAnchor = "";
        /// <summary>Appended (version 2): the region the player saved in ("" on the surface).</summary>
        public string SavedRegion = "";
        /// <summary>Appended (version 2): the verified anchor an underground save resumes at.</summary>
        public string SavedAnchor = "";
        /// <summary>Appended (version 2): the layout variant in use when saved (diagnostics; anchors resolve by id).</summary>
        public int LayoutVariant = -1;
    }

    /// <summary>Reset hub for every static the UNDERWORLD work stream adds (called from Game.ResetStatics).</summary>
    public static class UnderworldStatics
    {
        /// <summary>The Underheart keeps no statics of its own today (the probes live in WorldBounds, the openings in WorldApertures).</summary>
        public static void ResetStatics() { }
    }
}
