using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Game.Underheart: the physical fire-crystal network under Las Veyra (04E). Prepare runs before the city is built
    /// (binds the Pressure House stair and the vehicle portal, registers the ground openings), BuildWorld builds the
    /// cells after the city, and the component then owns region context, streaming, hazards, shortcuts, rest points and
    /// the underground map. FOUNDATION STUB: the UNDERWORLD work stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class UnderheartWorld : MonoBehaviour, IWorldSystem
    {
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

        /// <summary>Before CityBuilder: bind the entrances to their parcels and register WorldApertures.</summary>
        public void Prepare(CityLayout layout)
        {
            WorldSystems.Register(this);
            Status = "Underheart is not implemented in this build.";
        }

        /// <summary>After the city: build every cell (colliders always, renderers streamed by region).</summary>
        public IEnumerator BuildWorld()
        {
            yield break;
        }

        /// <summary>True when the point lies inside an Underheart cell volume (3D).</summary>
        public bool IsUnderground(Vector3 p) { return false; }

        /// <summary>The region id at a point, or "" outside every cell.</summary>
        public string RegionAt(Vector3 p) { return ""; }

        /// <summary>A named anchor's live position and facing (ids in UnderheartIds.Anchors).</summary>
        public bool TryAnchor(string anchorId, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            return false;
        }

        public bool ShortcutOpen(string id) { return false; }

        /// <summary>Opens a shortcut (persistent). False when unknown or already open.</summary>
        public bool UnlockShortcut(string id, bool announce) { return false; }

        /// <summary>
        /// A walkable route from a point to a target through entrances and portals (surface door first, then down), as
        /// world points the guidance can draw. floorLabel describes the target ("Lantern Refuge, 18 m below").
        /// </summary>
        public bool TryRoute(Vector3 from, Vector3 to, List<Vector3> points, out string floorLabel)
        {
            floorLabel = "";
            return false;
        }

        /// <summary>The nearest unlocked rest point (or the region entry) to a point, for respawns and falls.</summary>
        public bool TryNearestRestPoint(Vector3 from, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            return false;
        }

        /// <summary>On load: replace the saved position with a verified underground anchor when the save was underground.</summary>
        public bool TryResolveSavedPosition(SaveData d, ref Vector3 pos, ref float yaw) { return false; }

        /// <summary>Encounter rooms cleared during this run (ordinary rooms; bosses persist through the reward ledger).</summary>
        public void MarkCleared(string roomId) { }
        public bool IsCleared(string roomId) { return false; }

        /// <summary>Damaging heat at a point (lava, hot vents) in health per second; warm scenery returns false.</summary>
        public bool HazardAt(Vector3 p, out float heatPerSecond)
        {
            heatPerSecond = 0f;
            return false;
        }

        /// <summary>True when a vehicle may drive or fuse at a point: the surface, or an Underheart cell built for vehicles
        /// (the broad transit tunnels). headroom is the clear height above the floor there.</summary>
        public bool VehicleSpaceAt(Vector3 p, out float headroom)
        {
            headroom = 0f;
            return !WorldBounds.IsUnderground(p);
        }

        /// <summary>Regions the player has entered at least once.</summary>
        public IList<string> DiscoveredRegions { get { return new List<string>(); } }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

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
    }

    /// <summary>Reset hub for every static the UNDERWORLD work stream adds (called from Game.ResetStatics).</summary>
    public static class UnderworldStatics
    {
        public static void ResetStatics() { }
    }
}
