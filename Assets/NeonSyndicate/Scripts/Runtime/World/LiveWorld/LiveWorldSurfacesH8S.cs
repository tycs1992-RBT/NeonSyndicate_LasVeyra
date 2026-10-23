using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a texture file holds, read from the end of its name (MyTower_BaseColor, Metal_Normal and so on).</summary>
    public enum LiveWorldChannelH8S
    {
        /// <summary>Not a texture channel the pipeline knows.</summary>
        None,
        /// <summary>Colour (also Albedo, Diffuse, Color).</summary>
        BaseColor,
        /// <summary>Normal map.</summary>
        Normal,
        /// <summary>Metallic in red and smoothness in alpha (Unity's packed layout).</summary>
        MetallicSmoothness,
        /// <summary>Glow colour.</summary>
        Emission,
        /// <summary>Ambient occlusion.</summary>
        Occlusion,
        /// <summary>A separate roughness map (needs packing into MetallicSmoothness first).</summary>
        Roughness,
        /// <summary>A separate metallic map (needs packing into MetallicSmoothness first).</summary>
        Metallic,
        /// <summary>A glTF style occlusion, roughness, metallic pack (needs repacking first).</summary>
        Packed
    }

    /// <summary>The world surface a material slot without its own textures takes, chosen from its name.</summary>
    public enum LiveWorldSurfaceH8S
    {
        /// <summary>Carved and weathered masonry.</summary>
        Stone,
        /// <summary>Carved edges and trims, a lighter stone.</summary>
        Trim,
        /// <summary>Bark and roots.</summary>
        Bark,
        /// <summary>Leaves, moss and ferns.</summary>
        Leaf,
        /// <summary>Mineral crystal: the only surface that glows.</summary>
        Crystal,
        /// <summary>Tarnished iron and bronze.</summary>
        Metal
    }

    /// <summary>
    /// Engine-free surface rules for live world pieces: the texture naming convention, how a material slot finds its
    /// texture set, which world surface a slot without textures takes from its name, the owner's canon palette per region
    /// (Gothic Heart, Amethyst Dominion, Azure Shardwood), the tint that brings a world texture's measured mean colour to a
    /// palette colour, and the box projection that gives module meshes the same texture density as the city's stone
    /// (half a texture tile per metre, like the H2 meshes). Only crystals glow.
    /// </summary>
    public static class LiveWorldSurfacesH8S
    {
        /// <summary>Texture tiles per metre for world surfaces: the density measured on the city's H2 meshes (0.48 to 0.50).</summary>
        public const float UvPerMetre = 0.5f;
        /// <summary>A tint may brighten a texture by at most this factor per channel.</summary>
        public const float MaxGain = 1.25f;
        /// <summary>Crystal glow strength: the palette glow colour times this (glow only on crystals, never a wash).</summary>
        public const float GlowStrength = 0.55f;

        /// <summary>
        /// Splits a texture file name (without extension) into its set and channel: "Metal_BaseColor" gives "Metal" and
        /// BaseColor, "MyTower_Base_Color" gives "MyTower". False when the name has no known channel ending.
        /// </summary>
        public static bool SplitTextureName(string fileName, out string set, out LiveWorldChannelH8S channel)
        {
            set = "";
            channel = LiveWorldChannelH8S.None;
            if (string.IsNullOrEmpty(fileName)) return false;
            string name = fileName.Trim();
            string lower = name.ToLowerInvariant();
            string[] twoPart = { "base_color", "base-color", "base color", "base_colour", "metallic_smoothness", "ambient_occlusion" };
            for (int i = 0; i < twoPart.Length; i++)
            {
                if (!lower.EndsWith(twoPart[i], StringComparison.Ordinal)) continue;
                int cut = name.Length - twoPart[i].Length;
                if (cut <= 1 || !Separator(name[cut - 1])) continue;
                set = name.Substring(0, cut - 1).Trim();
                channel = i < 4 ? LiveWorldChannelH8S.BaseColor : i == 4 ? LiveWorldChannelH8S.MetallicSmoothness : LiveWorldChannelH8S.Occlusion;
                return set.Length > 0;
            }
            int split = -1;
            for (int i = name.Length - 1; i > 0; i--)
                if (Separator(name[i])) { split = i; break; }
            if (split <= 0 || split >= name.Length - 1) return false;
            channel = Channel(lower.Substring(split + 1));
            if (channel == LiveWorldChannelH8S.None) return false;
            set = name.Substring(0, split).Trim();
            return set.Length > 0;
        }

        /// <summary>The channel named by a texture name ending (case already lowered), or None.</summary>
        public static LiveWorldChannelH8S Channel(string ending)
        {
            switch (ending)
            {
                case "basecolor": case "basecolour": case "albedo": case "diffuse": case "color": case "colour": case "col": case "base":
                    return LiveWorldChannelH8S.BaseColor;
                case "normal": case "normalgl": case "normalmap": case "nrm": case "nor": case "norm":
                    return LiveWorldChannelH8S.Normal;
                case "metallicsmoothness": case "metalsmooth": case "metallicgloss": case "metalsmoothness": case "ms":
                    return LiveWorldChannelH8S.MetallicSmoothness;
                case "emission": case "emissive": case "glow":
                    return LiveWorldChannelH8S.Emission;
                case "ao": case "occlusion": case "ambientocclusion":
                    return LiveWorldChannelH8S.Occlusion;
                case "roughness": case "rough":
                    return LiveWorldChannelH8S.Roughness;
                case "metallic": case "metalness":
                    return LiveWorldChannelH8S.Metallic;
                case "orm": case "arm":
                    return LiveWorldChannelH8S.Packed;
                default:
                    return LiveWorldChannelH8S.None;
            }
        }

        /// <summary>A material or set name reduced to lower-case letters and digits, without Blender's ".001" style endings.</summary>
        public static string Normalize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string s = name.Trim();
            int instance = s.IndexOf(" (Instance)", StringComparison.Ordinal);
            if (instance >= 0) s = s.Substring(0, instance);
            int dot = s.LastIndexOf('.');
            if (dot > 0 && dot < s.Length - 1)
            {
                bool digits = true;
                for (int i = dot + 1; i < s.Length; i++) if (!char.IsDigit(s[i])) { digits = false; break; }
                if (digits) s = s.Substring(0, dot);
            }
            StringBuilder b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) b.Append(c);
            }
            return b.ToString();
        }

        /// <summary>
        /// Which texture set a material slot uses: the set with the same name, else the longest set name found inside the
        /// material's name ("MI_Trim_Metal.001" finds "Metal"), else the only set when there is exactly one, else -1.
        /// </summary>
        public static int MatchSet(string materialName, IList<string> sets)
        {
            if (sets == null || sets.Count == 0) return -1;
            string m = Normalize(materialName);
            for (int i = 0; i < sets.Count; i++)
                if (Normalize(sets[i]) == m && m.Length > 0) return i;
            int best = -1, bestLength = 0;
            for (int i = 0; i < sets.Count; i++)
            {
                string s = Normalize(sets[i]);
                if (s.Length < 2 || s.Length <= bestLength || m.IndexOf(s, StringComparison.Ordinal) < 0) continue;
                best = i;
                bestLength = s.Length;
            }
            if (best >= 0) return best;
            return sets.Count == 1 ? 0 : -1;
        }

        /// <summary>
        /// The world surface a slot takes from its name (checked in this order: crystal, leaf, bark, metal, trim, stone
        /// words). False, with Stone, when no word matches.
        /// </summary>
        public static bool TryWorldSurface(string materialName, out LiveWorldSurfaceH8S surface)
        {
            string m = Normalize(materialName);
            surface = LiveWorldSurfaceH8S.Stone;
            if (m.Length == 0) return false;
            if (Has(m, "amethyst", "azure", "turquoise", "crystal", "gem", "shard", "quartz", "geode")) { surface = LiveWorldSurfaceH8S.Crystal; return true; }
            if (Has(m, "leaf", "leaves", "foliage", "moss", "fern", "grass", "canopy", "ivy")) { surface = LiveWorldSurfaceH8S.Leaf; return true; }
            if (Has(m, "bark", "wood", "timber", "root", "trunk", "branch", "plank")) { surface = LiveWorldSurfaceH8S.Bark; return true; }
            if (Has(m, "iron", "steel", "metal", "bronze", "brass", "copper")) { surface = LiveWorldSurfaceH8S.Metal; return true; }
            if (Has(m, "weathered", "edge", "trim", "carv", "molding", "moulding", "cornice")) { surface = LiveWorldSurfaceH8S.Trim; return true; }
            if (Has(m, "stone", "masonry", "brick", "rock", "marble", "mineral", "slate", "basalt", "plaster", "concrete", "cobble", "granite")) { surface = LiveWorldSurfaceH8S.Stone; return true; }
            return false;
        }

        /// <summary>The palette for a place: the Amethyst Dominion and Azure Shardwood anchors by name, Gothic Heart everywhere else.</summary>
        public static LiveWorldPaletteH8S PaletteFor(string place)
        {
            switch (place ?? "")
            {
                case "Realm:gallery": case "Realm:castellan": case "Realm:oath-seal": case "Realm:oath-witness": case "Realm:oath-terrace":
                case "Realm:hearth-lens": case "Realm:grotto": case "Realm:threshold":
                    return LiveWorldPaletteH8S.Amethyst;
                case "Realm:azure": case "Realm:sovereign": case "Realm:storm-shrine": case "Realm:storm-crossing": case "Realm:storm-spring":
                case "Realm:storm-vein": case "Realm:storm-cavern": case "Realm:storm-ledge":
                    return LiveWorldPaletteH8S.Azure;
                default:
                    return LiveWorldPaletteH8S.Gothic;
            }
        }

        /// <summary>The palette of a route region: home and the Forge are Gothic Heart.</summary>
        public static LiveWorldPaletteH8S PaletteFor(WorldRouteRegionH8S region)
        {
            switch (region)
            {
                case WorldRouteRegionH8S.Amethyst: return LiveWorldPaletteH8S.Amethyst;
                case WorldRouteRegionH8S.Azure: return LiveWorldPaletteH8S.Azure;
                default: return LiveWorldPaletteH8S.Gothic;
            }
        }

        /// <summary>A palette that is never Auto (Auto becomes the fallback).</summary>
        public static LiveWorldPaletteH8S Resolve(LiveWorldPaletteH8S asked, LiveWorldPaletteH8S fallback)
        {
            if (asked != LiveWorldPaletteH8S.Auto) return asked;
            return fallback == LiveWorldPaletteH8S.Auto ? LiveWorldPaletteH8S.Gothic : fallback;
        }

        /// <summary>
        /// The target mean colour (sRGB) of a surface in a palette, from the owner's canon swatches: Gothic Heart
        /// #15191f #31373f #665b4e #ad874f #c9b98d, Amethyst #140f1f #322142 #643f83 #9d67c1 #c6a2e4, Azure #0b2228 #135058
        /// #178d98 #46c4c8 #9bdbd3. Bark is the weathered stone swatch at 65 percent (wet bark).
        /// </summary>
        public static Color Target(LiveWorldPaletteH8S palette, LiveWorldSurfaceH8S surface)
        {
            switch (surface)
            {
                case LiveWorldSurfaceH8S.Stone:
                    return palette == LiveWorldPaletteH8S.Gothic || palette == LiveWorldPaletteH8S.Auto ? Hex(0x665b4e) : Hex(0x31373f);
                case LiveWorldSurfaceH8S.Trim:
                    return palette == LiveWorldPaletteH8S.Gothic || palette == LiveWorldPaletteH8S.Auto ? Hex(0xc9b98d) : Hex(0x665b4e);
                case LiveWorldSurfaceH8S.Bark:
                {
                    Color stone = Hex(0x665b4e);
                    return new Color(stone.r * 0.65f, stone.g * 0.65f, stone.b * 0.65f, 1f);
                }
                case LiveWorldSurfaceH8S.Leaf:
                    return palette == LiveWorldPaletteH8S.Amethyst ? Hex(0x643f83) : Hex(0x135058);
                case LiveWorldSurfaceH8S.Crystal:
                    return palette == LiveWorldPaletteH8S.Azure ? Hex(0x46c4c8) : palette == LiveWorldPaletteH8S.Amethyst ? Hex(0x9d67c1) : Hex(0x643f83);
                default:
                    return Hex(0xad874f);
            }
        }

        /// <summary>The glow colour of a palette's crystals before GlowStrength (Gothic Heart crystals are the dimmest).</summary>
        public static Color Glow(LiveWorldPaletteH8S palette)
        {
            switch (palette)
            {
                case LiveWorldPaletteH8S.Azure: return Hex(0x178d98);
                case LiveWorldPaletteH8S.Amethyst: return Hex(0x643f83);
                default: return Hex(0x322142);
            }
        }

        /// <summary>How much smoother a surface reads than its texture says: Azure stone, bark and moss are wet.</summary>
        public static float SmoothnessScale(LiveWorldPaletteH8S palette, LiveWorldSurfaceH8S surface)
        {
            if (surface == LiveWorldSurfaceH8S.Crystal) return 1.6f;
            if (palette == LiveWorldPaletteH8S.Azure && surface != LiveWorldSurfaceH8S.Metal) return 1.6f;
            return 1f;
        }

        /// <summary>The Resources path (without the channel suffix) of the world texture set a surface uses.</summary>
        public static string TextureSet(LiveWorldSurfaceH8S surface, LiveWorldPaletteH8S palette)
        {
            switch (surface)
            {
                case LiveWorldSurfaceH8S.Bark: return "AscensionH2/Textures/root";
                case LiveWorldSurfaceH8S.Leaf: return "AscensionH2/Textures/leaf";
                case LiveWorldSurfaceH8S.Crystal: return palette == LiveWorldPaletteH8S.Azure ? "AscensionH2/Textures/azure" : "AscensionH2/Textures/amethyst";
                case LiveWorldSurfaceH8S.Metal: return "AscensionH5/Textures/iron";
                default: return "AscensionH3/Textures/masonry";
            }
        }

        /// <summary>
        /// The measured mean colour (sRGB, from 128 by 128 downsamples of the shipped files) of a world texture set's
        /// albedo, used to compute palette tints. Unknown sets count as mid grey.
        /// </summary>
        public static Color TextureMean(string set)
        {
            switch (set)
            {
                case "AscensionH3/Textures/masonry": return new Color(0.535f, 0.499f, 0.417f);
                case "AscensionH3/Textures/basalt": return new Color(0.425f, 0.418f, 0.340f);
                case "AscensionH2/Textures/masonry": return new Color(0.263f, 0.272f, 0.291f);
                case "AscensionH2/Textures/root": return new Color(0.259f, 0.201f, 0.153f);
                case "AscensionH2/Textures/leaf": return new Color(0.205f, 0.370f, 0.319f);
                case "AscensionH2/Textures/amethyst": return new Color(0.462f, 0.230f, 0.597f);
                case "AscensionH2/Textures/azure": return new Color(0.153f, 0.471f, 0.529f);
                case "AscensionH5/Textures/iron": return new Color(0.673f, 0.563f, 0.354f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>
        /// The tint that turns a texture of the given mean colour into the target mean colour, per channel, at most
        /// maxGain. A ratio of sRGB values: right in Gamma colour space and close in Linear (the power curve is multiplicative).
        /// </summary>
        public static Color Tint(Color target, Color textureMean, float maxGain)
        {
            return new Color(Gain(target.r, textureMean.r, maxGain), Gain(target.g, textureMean.g, maxGain), Gain(target.b, textureMean.b, maxGain), 1f);
        }

        /// <summary>
        /// Box-projected texture coordinates for a point on a face: the face's dominant axis picks the plane (top and bottom
        /// faces use x and z, side faces the horizontal axis and y), scaled to uvPerMetre tiles per metre.
        /// </summary>
        public static Vector2 BoxUv(Vector3 point, Vector3 faceNormal, float uvPerMetre)
        {
            float ax = Mathf.Abs(faceNormal.x), ay = Mathf.Abs(faceNormal.y), az = Mathf.Abs(faceNormal.z);
            Vector2 uv;
            if (ay >= ax && ay >= az) uv = new Vector2(point.x, point.z);
            else if (ax >= az) uv = new Vector2(point.z, point.y);
            else uv = new Vector2(point.x, point.y);
            return uv * uvPerMetre;
        }

        /// <summary>A colour from a 0xRRGGBB number (sRGB, alpha 1).</summary>
        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private static float Gain(float target, float mean, float maxGain)
        {
            if (!(mean > 1e-4f)) return Mathf.Clamp(target, 0f, maxGain);
            return Mathf.Clamp(target / mean, 0f, maxGain);
        }

        private static bool Separator(char c)
        {
            return c == '_' || c == '-' || c == ' ' || c == '.';
        }

        private static bool Has(string normalized, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
                if (normalized.IndexOf(words[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }
    }
}
