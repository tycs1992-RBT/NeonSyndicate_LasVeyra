// Compile boundary only. The real view is a native Unity object; no view is constructed by these source tests.
using System;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed class VenueView04C
    {
        public Vector3 WorldPoint(string anchor) { throw new InvalidOperationException("Native room view is unavailable in source validation."); }
    }
}
