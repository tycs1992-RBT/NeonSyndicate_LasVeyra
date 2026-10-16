using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The changed arrival neighborhood of 04E around the ordinary Veyra Lofts arrival: the Cinder Dispatch frontage and
    /// receiver mast, the warm conduit leading to the Pressure House, the open lobby touches, the crystal-repair worktable
    /// and the companion perch, placed on safe frontage that respects lanes, doors and existing markers. FOUNDATION STUB:
    /// the VENUES work stream replaces the body and keeps every signature.
    /// </summary>
    public static class ArrivalScene04E
    {
        /// <summary>The warm conduit's ground route from Cinder Dispatch to the Pressure House door (empty until built).</summary>
        public static readonly List<Vector3> ConduitPath = new List<Vector3>();

        /// <summary>True once the arrival dressing exists in the world.</summary>
        public static bool Built { get; private set; }

        /// <summary>Builds the arrival dressing after the venues are active. Never throws; reports through ModuleStatus.</summary>
        public static void Build(Transform parent) { }

        /// <summary>Lights the conduit route while a quest asks the player to follow it.</summary>
        public static void SetConduitLit(bool lit) { }

        public static void ResetStatics()
        {
            ConduitPath.Clear();
            Built = false;
        }
    }

    /// <summary>Reset hub for every static the VENUES work stream adds.</summary>
    public static class VenueStatics04E
    {
        public static void ResetStatics() { ArrivalScene04E.ResetStatics(); }
    }
}
