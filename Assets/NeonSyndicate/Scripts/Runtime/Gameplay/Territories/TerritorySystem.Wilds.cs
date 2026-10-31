using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>TerritorySystem, the plots it actually built, so the wilds (WildsSystem) keep clear of them.</summary>
    public sealed partial class TerritorySystem
    {
        /// <summary>The ground point of every territory and habitat that was placed.</summary>
        public List<Vector3> PlacedOrigins()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (Site site in _sites) if (site.Placed) points.Add(site.Origin);
            return points;
        }
    }
}
