using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One surveyed opening in the city's ground (a stairwell shaft, a vehicle ramp). Rect is in world X/Z.</summary>
    public sealed class WorldAperture
    {
        public string Id = "";
        public RectF Rect;
    }

    /// <summary>
    /// Openings cut through the ground slab, the block curb platform and their visual surfaces. The Underheart world
    /// registers them before the city is built (only surveyed rectangles inside supported parcels); CityBuilder
    /// subtracts them so the rest of the city keeps its collision. Subtract is pure.
    /// </summary>
    public static class WorldApertures
    {
        private static readonly List<WorldAperture> _all = new List<WorldAperture>();

        public static IList<WorldAperture> All { get { return _all; } }

        public static void ResetStatics() { _all.Clear(); }

        public static void Register(string id, RectF rect)
        {
            if (rect.Width <= 0.05f || rect.Depth <= 0.05f) return;
            _all.RemoveAll(a => a.Id == id);
            _all.Add(new WorldAperture { Id = id ?? "", Rect = rect });
        }

        public static bool Any { get { return _all.Count > 0; } }

        /// <summary>True when the X/Z point lies inside any opening.</summary>
        public static bool Contains(float x, float z)
        {
            for (int i = 0; i < _all.Count; i++) if (_all[i].Rect.Contains(new Vector3(x, 0f, z))) return true;
            return false;
        }

        /// <summary>Splits rect into pieces that avoid every opening (the input itself when nothing overlaps). Pure.</summary>
        public static List<RectF> Subtract(RectF rect)
        {
            List<RectF> pieces = new List<RectF> { rect };
            foreach (WorldAperture a in _all) pieces = SubtractOne(pieces, a.Rect);
            return pieces;
        }

        /// <summary>Removes one rectangle from a list of rectangles (up to four pieces each). Pure.</summary>
        public static List<RectF> SubtractOne(List<RectF> pieces, RectF hole)
        {
            List<RectF> outList = new List<RectF>();
            foreach (RectF r in pieces)
            {
                if (!r.Overlaps(hole, 0f))
                {
                    outList.Add(r);
                    continue;
                }
                float hx0 = Mathf.Max(r.X0, hole.X0), hx1 = Mathf.Min(r.X1, hole.X1);
                float hz0 = Mathf.Max(r.Z0, hole.Z0), hz1 = Mathf.Min(r.Z1, hole.Z1);
                if (hz0 - r.Z0 > 0.01f) outList.Add(new RectF(r.X0, r.Z0, r.X1, hz0));
                if (r.Z1 - hz1 > 0.01f) outList.Add(new RectF(r.X0, hz1, r.X1, r.Z1));
                if (hx0 - r.X0 > 0.01f) outList.Add(new RectF(r.X0, hz0, hx0, hz1));
                if (r.X1 - hx1 > 0.01f) outList.Add(new RectF(hx1, hz0, r.X1, hz1));
            }
            return outList;
        }
    }
}
