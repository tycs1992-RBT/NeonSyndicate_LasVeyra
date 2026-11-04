using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The runtime textures and materials of the live hair (owner direction in chat 2026-10-23: swap NPCs and enemies for
    /// realistic bodies): the three strand textures painted by LiveHairTexture wrapped as Texture2D with their normal
    /// maps, and four materials on the project's lit shader for the active pipeline: the opaque base cap, the alpha-tested
    /// long strands, the alpha-tested crop (short hair, short beards) and the alpha-tested stubble. All are white and
    /// carry black emission, so one person's colours and the hit flash arrive through the live adapter's property blocks.
    /// Created once per play session; the Editor keep set carries the cutout, normal map and emission variant for builds.
    /// </summary>
    public static class LiveHairMaterials
    {
        /// <summary>The alpha cutoff of the strand materials.</summary>
        public const float Cutoff = 0.42f;
        /// <summary>The texture seed (changing it repaints every person's strands).</summary>
        public const int Seed = 61724;

        private static Texture2D _longAlbedo, _longNormal, _cropAlbedo, _cropNormal, _stubbleAlbedo, _stubbleNormal;
        private static Material _base, _strands, _crop, _stubble;
        private static bool _failed;
        private static string _failure = "";

        /// <summary>Forgets the textures and materials (Enter Play Mode without a domain reload starts clean).</summary>
        public static void ResetStatics()
        {
            _longAlbedo = _longNormal = _cropAlbedo = _cropNormal = _stubbleAlbedo = _stubbleNormal = null;
            _base = _strands = _crop = _stubble = null;
            _failed = false;
            _failure = "";
        }

        /// <summary>True when the materials could not be made this session (Why says why); the people then keep bare heads.</summary>
        public static bool Failed { get { return _failed; } }
        public static string Why { get { return _failure; } }

        /// <summary>The opaque base cap under the strands.</summary>
        public static Material Base { get { Ensure(); return _base; } }
        /// <summary>The alpha-tested long strands (shells, drapes, braids, crests).</summary>
        public static Material Strands { get { Ensure(); return _strands; } }
        /// <summary>The alpha-tested short crop (short beards).</summary>
        public static Material Crop { get { Ensure(); return _crop; } }
        /// <summary>The alpha-tested stubble (shaved areas, stubble beards).</summary>
        public static Material Stubble { get { Ensure(); return _stubble; } }

        /// <summary>The material array of a hair slot renderer: base, strands, accent strands, shaved.</summary>
        public static Material[] HairSet()
        {
            Ensure();
            return _failed ? null : new[] { _base, _strands, _strands, _stubble };
        }

        /// <summary>The material array of a beard slot renderer: stubble, short beard.</summary>
        public static Material[] BeardSet()
        {
            Ensure();
            return _failed ? null : new[] { _stubble, _crop };
        }

        private static void Ensure()
        {
            if (_base != null || _failed) return;
            try
            {
                _longAlbedo = Wrap(LiveHairTexture.Albedo(LiveHairTexture.Size, LiveHairTextureKind.Long, Seed), "Live hair strands", false);
                _longNormal = Wrap(LiveHairTexture.Normal(LiveHairTexture.Size, LiveHairTextureKind.Long, Seed), "Live hair strands normal", true);
                _cropAlbedo = Wrap(LiveHairTexture.Albedo(LiveHairTexture.Size, LiveHairTextureKind.Crop, Seed), "Live hair crop", false);
                _cropNormal = Wrap(LiveHairTexture.Normal(LiveHairTexture.Size, LiveHairTextureKind.Crop, Seed), "Live hair crop normal", true);
                _stubbleAlbedo = Wrap(LiveHairTexture.Albedo(LiveHairTexture.Size / 2, LiveHairTextureKind.Stubble, Seed), "Live hair stubble", false);
                _stubbleNormal = Wrap(LiveHairTexture.Normal(LiveHairTexture.Size / 2, LiveHairTextureKind.Stubble, Seed), "Live hair stubble normal", true);
                _base = Mats.Textured("Live hair base", _longAlbedo, Color.white, 0.4f, 0f, _longNormal, 0.7f);
                Mats.SetEmission(_base, Color.black, null);
                _strands = Cutout("Live hair strands", _longAlbedo, _longNormal, 0.46f);
                _crop = Cutout("Live hair crop", _cropAlbedo, _cropNormal, 0.4f);
                _stubble = Cutout("Live hair stubble", _stubbleAlbedo, _stubbleNormal, 0.3f);
            }
            catch (System.Exception e)
            {
                _failed = true;
                _failure = e.Message;
                _base = _strands = _crop = _stubble = null;
                Log.Warn("Live hair: the strand materials could not be made (" + e.Message + "). The live crowd keeps bare heads this session.");
            }
        }

        private static Texture2D Wrap(Color32[] pixels, string name, bool linear)
        {
            int size = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
            t.name = name;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 6;
            t.SetPixels32(pixels);
            t.Apply(true, false);
            return t;
        }

        /// <summary>An alpha-tested lit material with a normal map and black emission, on either pipeline.</summary>
        private static Material Cutout(string name, Texture2D albedo, Texture2D normal, float smoothness)
        {
            Material m = Mats.Textured(name, albedo, Color.white, smoothness, 0f, normal, 0.7f);
            Mats.SetEmission(m, Color.black, null);
            if (RenderPath.IsURP)
            {
                Mats.MakeUrpCutout(m, Cutoff);
                return m;
            }
            m.SetFloat("_Mode", 1f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", Cutoff);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetInt("_SrcBlend", (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.EnableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            return m;
        }
    }
}
