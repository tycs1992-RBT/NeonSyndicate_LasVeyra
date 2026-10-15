using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Bounded, reproducible site surveys. Existing routes and occupied world geometry always win.</summary>
    public static class TerritoryPlacement
    {
        public const float ReservedDistance = 88f;
        public const int CandidateRings = 7;
        public const float CandidateStep = 32f;

        /// <summary>The preferred site followed by seven square rings, clipped to the existing safe outskirts.</summary>
        public static List<Vector3> Candidates(float x, float z)
        {
            List<Vector3> points = new List<Vector3>(225);
            if (!Finite(x) || !Finite(z)) return points;
            AddCandidate(points, new Vector3(x, 0f, z));
            for (int ring = 1; ring <= CandidateRings; ring++)
                for (int side = 0; side < 4; side++)
                    for (int offset = -ring; offset < ring; offset++)
                    {
                        float dx = side == 0 ? offset : side == 1 ? ring : side == 2 ? -offset : -ring;
                        float dz = side == 0 ? -ring : side == 1 ? offset : side == 2 ? ring : -offset;
                        AddCandidate(points, new Vector3(x + dx * CandidateStep, 0f, z + dz * CandidateStep));
                    }
            return points;
        }

        private static void AddCandidate(List<Vector3> points, Vector3 point)
        {
            if (StrongholdPlacement.RegionAllowed(point)) points.Add(point);
        }

        /// <summary>True only when the new plot is separate from all successful territory and original fort anchors.</summary>
        public static bool CanReserve(Vector3 candidate, IList<Vector3> reserved)
        {
            if (!Finite(candidate.x) || !Finite(candidate.y) || !Finite(candidate.z) ||
                !StrongholdPlacement.RegionAllowed(candidate) || reserved == null) return false;
            foreach (Vector3 other in reserved)
            {
                if (!Finite(other.x) || !Finite(other.y) || !Finite(other.z) ||
                    MathUtil.FlatSqrDistance(candidate, other) < ReservedDistance * ReservedDistance) return false;
            }
            return true;
        }

        /// <summary>Reuses the exact roads, lots, POIs, spawns, interactables and native ground/obstruction test.</summary>
        public static bool Probe(CityLayout layout, Vector3 candidate, List<Vector3> reserved, out Vector3 ground)
        {
            ground = Vector3.zero;
            if (!CanReserve(candidate, reserved)) return false;
            // The original 220m fort-to-fort spacing is replaced only by this layer's explicit 88m reservation.
            // Every other original exclusion and all 25 native ground rays plus the obstruction box remain intact.
            return StrongholdPlacement.Probe(layout, candidate, new List<Vector3>(), out ground);
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static void ResetStatics() { }
    }
}
