using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        private bool _frostGroundL3;

        /// <summary>Route only the original frost tile's bare sand through the shared ground proxy.</summary>
        private bool BuildFrostGroundL3(Vector3 center, float x0, float z0, float x1, float z1)
        {
            bool previous = _frostGroundL3;
            try
            {
                _frostGroundL3 = true;
                return BuildFrostTile(center, x0, z0, x1, z1);
            }
            finally { _frostGroundL3 = previous; }
        }
    }
}
