using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable] public sealed class VenueCatalog04C
    {
        public string Version;
        public VenueDefinition04C[] Venues;
    }

    /// <summary>Stable venue identity binds an existing parcel without moving a story POI.</summary>
    [Serializable] public sealed class VenueDefinition04C
    {
        public string Id, Title, Kind, Layout, PoiName;
        public int PreferredBuildingSeed;
        public float OffsetX, OffsetZ, MaxDistance = 250f;
        public float FacingX, FacingZ;
    }

    public sealed class VenueBinding04C
    {
        public VenueDefinition04C Definition;
        public Poi Poi;
        public Lot Lot;
        public Building Building;
        public Vector3 Origin, Outward, Entrance, Outside;
        public float Yaw, Width, Depth;
        public VenueView04C View;
        public string Status = "Awaiting building construction.";
        public bool Available { get { return View != null; } }
        public Vector3 Point(string anchor)
        {
            return View != null ? View.WorldPoint(anchor) : Entrance;
        }
    }

    [Serializable] public sealed class InhabitedCitySave
    {
        public int Version = 1;
        public List<string> Visited = new List<string>();
        public List<string> Actions = new List<string>();
        public List<string> ReadRecords = new List<string>();
        public string ResumeVenue = "";
        public bool IntroSeen;
    }
}
