using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Durable state (SaveData.Underheart, append-only): open shortcuts, discovered regions, cleared rooms, the last rest
    /// point, and for an underground save the region and a verified fallback anchor. Loading resumes an underground save at
    /// its exact spot when that spot is still inside the same layout, out of the heat and on solid floor; otherwise at the
    /// saved anchor or the nearest rest point; and when the Underheart is unavailable, at the Pressure House door with a
    /// readable notice and every bit of progress kept. Rest point choice for falls, deaths and lost cars lives here too.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        /// <summary>Where a car lost in a transit tunnel is put back (vehicle cells only, never a walk-only room).</summary>
        private static readonly string[] VehicleRecovery = { "canals.transit", "canals.portal_bottom", "workshops.convoy_start", "workshops.convoy_end" };

        private bool _snapAtmosphere;

        /// <summary>The anchor that begins each region's critical path (the respawn fallback before its rest point is known).</summary>
        public static string EntryAnchorOf(string region)
        {
            switch (region)
            {
                case UnderheartIds.ServiceLevel: return "service.stair_bottom";
                case UnderheartIds.LanternRefuge: return "refuge.entry";
                case UnderheartIds.EmberCanals: return "canals.entry";
                case UnderheartIds.PyreGardens: return "gardens.entry";
                case UnderheartIds.CrucibleWorkshops: return "workshops.entry";
                case UnderheartIds.AshReliquary: return "reliquary.entry";
                case UnderheartIds.FoundryCrown: return "crown.staging";
                default: return "service.stair_bottom";
            }
        }

        private static bool IsRegionId(string id) { return !string.IsNullOrEmpty(id) && Array.IndexOf(UnderheartIds.Regions, id) >= 0; }

        // ================================================================== IWorldSystem

        public void ResetAll()
        {
            _gardenCircuit04N.Reset();
            _shortcuts.Clear();
            _discovered.Clear();
            _cleared.Clear();
            _restAnchor = "";
            _restReady = 0f;
            _lastBanner.Clear();
            _pendingNotice = "";
            ResetHeat();
            ResetWindow();
            string was = PlayerRegion;
            PlayerRegion = "";
            _playerCell = -1;
            try { Underheart04ESounds.SetZone(UhZone.Surface); }
            catch (Exception e) { Log.Exception("Underheart audio zone", e); }
            ApplyShortcutStates(true);
            if (Game.Atmo != null)
            {
                Game.Atmo.UndergroundTarget = 0f;
                Game.Atmo.UndergroundLight = 1f;
                Game.Atmo.SnapUnderground04E();
            }
            _snapAtmosphere = true;
            if (was.Length > 0) RaiseRegionChanged(was, "");
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            UnderheartSave s = new UnderheartSave();
            s.Version = 2;
            s.Shortcuts.AddRange(_shortcuts);
            s.Shortcuts.Sort(StringComparer.Ordinal);
            s.Discovered.AddRange(_discovered);
            s.Cleared.AddRange(_cleared);
            s.Cleared.Sort(StringComparer.Ordinal);
            s.RestAnchor = _restAnchor ?? "";
            s.LayoutVariant = _layout != null ? _layout.Variant : -1;
            if (Ready && Game.Player != null)
            {
                Vector3 pos = Game.PlayerPosition;
                int cell = CellAtWorld(pos, _playerCell);
                if (cell >= 0 && pos.y < WorldBounds.UndergroundCeiling)
                {
                    s.SavedRegion = _layout.Cells[cell].Region;
                    s.SavedAnchor = RestAnchorFor(pos, cell);
                }
            }
            d.Underheart = s;
        }

        public void Restore(SaveData d)
        {
            _gardenCircuit04N.Reset();
            UnderheartSave s = d != null && d.Underheart != null ? d.Underheart : new UnderheartSave();
            _shortcuts.Clear();
            if (s.Shortcuts != null)
                foreach (string id in s.Shortcuts)
                    if (UnderheartIds.HasShortcut(id)) _shortcuts.Add(id);
            _discovered.Clear();
            if (s.Discovered != null)
                foreach (string r in s.Discovered)
                    if (IsRegionId(r) && !_discovered.Contains(r)) _discovered.Add(r);
            _cleared.Clear();
            if (s.Cleared != null)
                foreach (string c in s.Cleared)
                    if (!string.IsNullOrEmpty(c)) _cleared.Add(c);
            _restAnchor = UnderheartIds.HasAnchor(s.RestAnchor) ? s.RestAnchor : "";
            ApplyShortcutStates(true);
            // A loaded game starts with no descent window: the window is never saved, it follows a running quest only.
            ResetWindow();
            _snapAtmosphere = true;
        }

        // ================================================================== resume and recovery

        /// <summary>On load: replace the saved position with a verified underground anchor when the save was underground.</summary>
        public bool TryResolveSavedPosition(SaveData d, ref Vector3 pos, ref float yaw)
        {
            UnderheartSave s = d != null ? d.Underheart : null;
            bool savedBelow = s != null && !string.IsNullOrEmpty(s.SavedRegion);
            bool posBelow = Ready && MathUtil.IsFinite(pos) && pos.y < WorldBounds.UndergroundCeiling && CellAtWorld(pos) >= 0;
            if (!savedBelow && !posBelow) return false;
            if (!Ready)
            {
                // The Underheart is unavailable this session: wake at the Pressure House door (or on the street above).
                Vector3 fallback = SurfaceEntrance.sqrMagnitude > 1f ? SurfaceEntrance : StreetAbove(pos);
                pos = GameRoot.FindGround(new Vector3(fallback.x, 2f, fallback.z)) + Vector3.up * 0.05f;
                _pendingNotice = "The Underheart is not available in this session, so you wake on the street above it. Your progress is kept. (" + Status + ")";
                Log.Warn("Underheart resume: save was underground but the world is unavailable; resuming at the surface.");
                return false;
            }
            Vector3 ground;
            // 1. The exact saved spot, when it is still inside the same layout, out of the heat and on solid floor.
            if (s == null || s.LayoutVariant < 0 || s.LayoutVariant == _layout.Variant)
            {
                int cell = CellAtWorld(pos);
                if (cell >= 0 && SafeToStand(pos, cell, out ground))
                {
                    pos = ground + Vector3.up * 0.05f;
                    _snapAtmosphere = true;
                    Log.Info("Underheart resume: exact position in " + _layout.Cells[cell].Id);
                    return true;
                }
            }
            // 2. The saved fallback anchor, then the rest point nearest the saved spot.
            Vector3 a;
            float ay;
            string anchor = s != null ? s.SavedAnchor : "";
            if (string.IsNullOrEmpty(anchor) || !TryAnchor(anchor, out a, out ay) || !Supported(a, out ground))
            {
                anchor = RestAnchorFor(pos, NearestCell(pos, 20f));
                if (!TryAnchor(anchor, out a, out ay) || !Supported(a, out ground))
                {
                    pos = GameRoot.FindGround(new Vector3(SurfaceEntrance.x, 2f, SurfaceEntrance.z)) + Vector3.up * 0.05f;
                    _pendingNotice = "Your underground save point could not be found in this layout, so you wake at the Pressure House. Your progress is kept.";
                    Log.Warn("Underheart resume: no verified anchor; resuming at the Pressure House door.");
                    return false;
                }
            }
            pos = ground + Vector3.up * 0.05f;
            yaw = ay;
            _snapAtmosphere = true;
            _pendingNotice = "You wake at the " + UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(anchor)) + " (" + AnchorTitle(anchor) + "), the nearest safe point to where you saved.";
            Log.Info("Underheart resume: anchor " + anchor);
            return true;
        }

        /// <summary>The nearest unlocked rest point (or the region entry) to a point, for respawns and falls.</summary>
        public bool TryNearestRestPoint(Vector3 from, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            if (!Ready || !MathUtil.IsFinite(from)) return false;
            int cell = CellAtWorld(from, -1);
            if (cell < 0) cell = NearestCell(from, 20f);
            return TryAnchor(RestAnchorFor(from, cell), out pos, out yaw);
        }

        /// <summary>
        /// The recovery anchor for a point: a vehicle bay for a point in a transit tunnel; otherwise the region's rest point
        /// once the region is discovered, then the last rest point used in the same region, then the region's entry.
        /// </summary>
        private string RestAnchorFor(Vector3 world, int cell)
        {
            if (_layout == null) return "service.stair_bottom";
            if (cell >= 0 && _layout.Cells[cell].Vehicle)
            {
                string best = "";
                float bd = float.MaxValue;
                foreach (string id in VehicleRecovery)
                {
                    UhAnchor a = _layout.Anchor(id);
                    if (a == null) continue;
                    float d = (_frame.ToWorld(a.Pos) - world).sqrMagnitude;
                    if (d < bd) { bd = d; best = id; }
                }
                if (best.Length > 0) return best;
            }
            string region = cell >= 0 ? _layout.Cells[cell].Region : "";
            UhRegionInfo info = region.Length > 0 ? _layout.Region(region) : null;
            if (info != null && !string.IsNullOrEmpty(info.Rest) && _discovered.Contains(region) && _layout.Anchor(info.Rest) != null) return info.Rest;
            if (_restAnchor.Length > 0 && UnderheartIds.RegionOfAnchor(_restAnchor) == region && _layout.Anchor(_restAnchor) != null) return _restAnchor;
            string entry = EntryAnchorOf(region);
            return _layout.Anchor(entry) != null ? entry : "service.stair_bottom";
        }

        /// <summary>The cell whose volume is nearest a point within a reach (for points just outside every cell).</summary>
        private int NearestCell(Vector3 world, float reach)
        {
            if (_layout == null || !MathUtil.IsFinite(world)) return -1;
            Vector3 p = _frame.ToLocal(world);
            int best = -1;
            float bd = reach * reach;
            foreach (UhCell c in _layout.Cells)
            {
                float dx = Mathf.Max(0f, Mathf.Max(c.X0 - p.x, p.x - c.X1));
                float dz = Mathf.Max(0f, Mathf.Max(c.Z0 - p.z, p.z - c.Z1));
                float f = c.FloorAt(Mathf.Clamp(p.x, c.X0, c.X1), Mathf.Clamp(p.z, c.Z0, c.Z1));
                float dy = p.y < f ? f - p.y : Mathf.Max(0f, p.y - (f + c.Height));
                float d = dx * dx + dz * dz + dy * dy;
                if (d < bd) { bd = d; best = c.Index; }
            }
            return best;
        }

        /// <summary>True when a world point has floor right under it (a collider within 2 m below).</summary>
        private static bool Supported(Vector3 world, out Vector3 ground)
        {
            RaycastHit hit;
            if (Physics.Raycast(world + Vector3.up * 1.0f, Vector3.down, out hit, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                ground = hit.point;
                return true;
            }
            ground = world;
            return false;
        }

        /// <summary>A resume spot must have floor under it and no heat in its cell at that spot.</summary>
        private bool SafeToStand(Vector3 world, int cell, out Vector3 ground)
        {
            ground = world;
            if (!Supported(world, out ground)) return false;
            Vector3 local = _frame.ToLocal(ground);
            foreach (UhHazard h in _layout.Hazards)
                if (h.Cell == cell && h.Heat > 0f && h.Contains(local, 0.6f)) return false;
            return true;
        }

        private static Vector3 StreetAbove(Vector3 p)
        {
            if (Game.Layout != null)
            {
                int n = Game.Layout.NearestNode(new Vector3(p.x, 0f, p.z));
                if (n >= 0) return Game.Layout.Nodes[n].Pos;
            }
            return new Vector3(p.x, 0f, p.z);
        }

        /// <summary>A short readable name for an anchor id ("rest point", "entry", "lift").</summary>
        private static string AnchorTitle(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId)) return "";
            int dot = anchorId.IndexOf('.');
            string tail = dot >= 0 ? anchorId.Substring(dot + 1) : anchorId;
            if (Array.IndexOf(UnderheartIds.RestPoints, anchorId) >= 0) return "rest point";
            return tail.Replace('_', ' ');
        }
    }
}
