using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Passive imported texture references generated before Play/build by the existing preparation owner.</summary>
    public sealed class LiveGroundCatalogL3 : ScriptableObject
    {
        public Texture2D[] Earth, Sand;
        public Shader SurfaceShader;
        public string Fingerprint, Reason;
        public bool Ready
        {
            get
            {
                if (SurfaceShader == null || !SurfaceShader.isSupported || !Maps(Earth) || !Maps(Sand)) return false;
                return true;
            }
        }
        private static bool Maps(Texture2D[] maps)
        {
            if (maps == null || maps.Length != 3) return false;
            foreach (Texture2D map in maps) if (map == null || map.width != 1024 || map.height != 1024) return false;
            return true;
        }
    }
}
