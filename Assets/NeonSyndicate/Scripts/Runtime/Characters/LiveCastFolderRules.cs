using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a texture file in a drop-in folder is for, read from its name suffix.</summary>
    public enum LiveTextureRole
    {
        /// <summary>Base colour (sRGB); alpha drives cutout surfaces such as hair cards.</summary>
        Albedo = 0,
        /// <summary>Tangent-space normal map (imported as a normal map).</summary>
        Normal = 1,
        /// <summary>Glow colour (sRGB).</summary>
        Emission = 2,
        /// <summary>Metallic in red, smoothness in alpha (linear), the usual Unity metallic map.</summary>
        Metallic = 3
    }

    /// <summary>How a generated drop-in material draws.</summary>
    public enum LiveSurfaceMode
    {
        /// <summary>Solid surface.</summary>
        Opaque = 0,
        /// <summary>Alpha-tested surface (hair cards, lashes, brows): pixels below the cutoff are not drawn.</summary>
        Cutout = 1,
        /// <summary>See-through surface (cornea, glass).</summary>
        Transparent = 2
    }

    /// <summary>Whether an imported character already faces the game's forward (+Z) or must be turned around.</summary>
    public enum LiveFacing
    {
        /// <summary>Faces +Z with the left hand at -X.</summary>
        AsIs = 0,
        /// <summary>Faces -Z: turn the visual half a circle (never mirrored).</summary>
        TurnAround = 1,
        /// <summary>No forward marker, toes or spread hands to judge by.</summary>
        Unknown = 2
    }

    /// <summary>Default look of a drop-in material, guessed from its name (livecast.json can override every value).</summary>
    public struct LiveSurfaceHint
    {
        /// <summary>Opaque, cutout or transparent.</summary>
        public LiveSurfaceMode Mode;
        /// <summary>0 matte to 1 mirror.</summary>
        public float Smoothness;
        /// <summary>0 non-metal to 1 metal.</summary>
        public float Metallic;
        /// <summary>Normal map strength.</summary>
        public float Bump;
    }

    /// <summary>
    /// Engine-free naming rules of the live character drop-in pipeline (Docs/LIVE_CHARACTERS.md): one folder per character
    /// under Art/LiveCast named after the character, one exported FBX plus optional _LOD1 and _LOD2 files, textures named
    /// after the material with a role suffix, an optional livecast.json, sockets created on the H9 bones when the export
    /// lacks them, orientation from the forward marker (or toes or hands) and the rebuild fingerprint. Used by the Editor
    /// generator (LiveCastSetup) and tested offline.
    /// </summary>
    public static class LiveCastFolderRules
    {
        /// <summary>The drop-in root folder.</summary>
        public const string Root = "Assets/NeonSyndicate/Art/LiveCast";
        /// <summary>The generated subfolder of each character (git-ignored).</summary>
        public const string PreparedFolder = "Prepared";
        /// <summary>The optional settings file of a drop-in folder.</summary>
        public const string ConfigFile = "livecast.json";
        /// <summary>The H9 Malik art folder, also watched for re-exports.</summary>
        public const string BuiltinRoot = "Assets/NeonSyndicate/Art/ProductionH9";

        /// <summary>The ten socket roles and the H9 bone each one is created on when an export lacks it (as the exporter does).</summary>
        public static readonly ReadOnlyCollection<string> SocketRoles = DanteReviewH8S.SocketRoles;
        private static readonly string[] SocketBones = { "hand_r", "hand_l", "hand_l", "hand_r", "hand_r", "hand_r", "Head", "spine_03", "spine_03", "spine_03" };

        private static readonly string[] AlbedoSuffixes = { "_basecolor", "_base_color", "_albedo", "_diffuse", "_colour", "_color" };
        private static readonly string[] NormalSuffixes = { "_normal", "_nrm", "_norm" };
        private static readonly string[] EmissionSuffixes = { "_emission", "_emissive", "_glow" };
        private static readonly string[] MetallicSuffixes = { "_metallicsmoothness", "_metallic", "_metal" };
        private static readonly string[] TextureExtensions = { ".png", ".tga", ".jpg", ".jpeg", ".psd", ".tif", ".tiff" };

        /// <summary>Folders the scanner ignores: the generated Prepared folder and names starting with "_" or ".".</summary>
        public static bool SkipFolder(string name)
        {
            return string.IsNullOrWhiteSpace(name) || name == PreparedFolder || name.StartsWith("_", StringComparison.Ordinal) || name.StartsWith(".", StringComparison.Ordinal);
        }

        /// <summary>
        /// The identity a folder name gives when livecast.json names none: "Malik", "Malik Veyr" or "Player" mean the player
        /// (malik); any other folder is the in-game name of a person ("Corin Pell" gives npc:Corin Pell).
        /// </summary>
        public static string IdentityFromFolder(string folder)
        {
            string name = (folder ?? "").Trim();
            if (name.Length == 0) return "";
            string lower = name.ToLowerInvariant();
            if (lower == "malik" || lower == "malik veyr" || lower == "player") return LiveCastRules.PlayerIdentity;
            return LiveCastRules.NpcPrefix + name;
        }

        /// <summary>The campaign cast id of an npc identity whose name is a cast member's ("npc:Dante Rook" gives "dante"), else "".</summary>
        public static string CastIdFor(string identity)
        {
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith(LiveCastRules.NpcPrefix, StringComparison.Ordinal)) return "";
            string name = identity.Substring(LiveCastRules.NpcPrefix.Length);
            foreach (string id in CastIds04E.All)
                if (string.Equals(CastIds04E.Name(id), name, StringComparison.Ordinal)) return id;
            return "";
        }

        /// <summary>
        /// The LOD level of a model file: "Corin.fbx" or "Corin_LOD0.fbx" give 0, "Corin_LOD1.fbx" 1 and "Corin_LOD2.fbx" 2,
        /// each with baseName "Corin". Anything else (another extension, LOD3 and up) gives -1.
        /// </summary>
        public static int LodOfModel(string fileName, out string baseName)
        {
            baseName = "";
            if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return -1;
            string stem = fileName.Substring(0, fileName.Length - 4);
            if (stem.Length == 0) return -1;
            int at = stem.LastIndexOf("_LOD", StringComparison.OrdinalIgnoreCase);
            if (at > 0 && at == stem.Length - 5 && char.IsDigit(stem[stem.Length - 1]))
            {
                int level = stem[stem.Length - 1] - '0';
                if (level > 2) return -1;
                baseName = stem.Substring(0, at);
                return level;
            }
            baseName = stem;
            return 0;
        }

        /// <summary>
        /// The comparison key of a material name: lowercase letters and digits only, without Unity's " (Instance)" and a
        /// numeric duplicate suffix (".001"). "H6 Worn leather" and "H6_Worn_leather" both give "h6wornleather".
        /// </summary>
        public static string MaterialKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string text = name.Replace(" (Instance)", "").Trim();
            int dot = text.LastIndexOf('.');
            if (dot > 0 && dot < text.Length - 1)
            {
                bool digits = true;
                for (int i = dot + 1; i < text.Length; i++) if (text[i] < '0' || text[i] > '9') { digits = false; break; }
                if (digits) text = text.Substring(0, dot);
            }
            StringBuilder key = new StringBuilder(text.Length);
            foreach (char c in text.ToLowerInvariant())
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) key.Append(c);
            return key.ToString();
        }

        /// <summary>
        /// Reads a texture file name: "Corin_Skin_Albedo.png" gives key "corinskin" and role Albedo. Suffixes (any case,
        /// after "_", "-" or a space): Albedo, BaseColor, Diffuse, Color, Colour; Normal, Nrm, Norm; Emission, Emissive, Glow;
        /// Metallic, MetallicSmoothness, Metal. False for other files.
        /// </summary>
        public static bool TextureRole(string fileName, out string key, out LiveTextureRole role)
        {
            key = "";
            role = LiveTextureRole.Albedo;
            if (string.IsNullOrEmpty(fileName)) return false;
            string ext = "";
            foreach (string e in TextureExtensions)
                if (fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase)) { ext = e; break; }
            if (ext.Length == 0) return false;
            string stem = fileName.Substring(0, fileName.Length - ext.Length).Replace('-', '_').Replace(' ', '_');
            string lower = stem.ToLowerInvariant();
            int cut;
            if ((cut = Suffix(lower, MetallicSuffixes)) > 0) role = LiveTextureRole.Metallic;
            else if ((cut = Suffix(lower, NormalSuffixes)) > 0) role = LiveTextureRole.Normal;
            else if ((cut = Suffix(lower, EmissionSuffixes)) > 0) role = LiveTextureRole.Emission;
            else if ((cut = Suffix(lower, AlbedoSuffixes)) > 0) role = LiveTextureRole.Albedo;
            else return false;
            key = MaterialKey(stem.Substring(0, cut));
            return key.Length > 0;
        }

        private static int Suffix(string lower, string[] suffixes)
        {
            foreach (string s in suffixes)
                if (lower.Length > s.Length && lower.EndsWith(s, StringComparison.Ordinal)) return lower.Length - s.Length;
            return -1;
        }

        /// <summary>
        /// The default look for a material key: see-through for cornea, glass, lens and tear; cutout for hair, brows, lashes,
        /// grooming, beard, fur, feathers, leaves and cards; metal for metal names; glossy eyes; soft skin; matte cloth and
        /// leather. Precedence follows that order, so "eyelash" is a cutout and "eye" is glossy.
        /// </summary>
        public static LiveSurfaceHint DefaultSurface(string key)
        {
            LiveSurfaceHint h = new LiveSurfaceHint { Mode = LiveSurfaceMode.Opaque, Smoothness = 0.3f, Metallic = 0f, Bump = 0.6f };
            string k = key ?? "";
            if (Has(k, "cornea", "glass", "lens", "tear")) { h.Mode = LiveSurfaceMode.Transparent; h.Smoothness = 0.95f; h.Bump = 0f; return h; }
            if (Has(k, "hair", "brow", "lash", "groom", "beard", "fur", "feather", "leaf", "cutout", "card")) { h.Mode = LiveSurfaceMode.Cutout; h.Smoothness = 0.2f; h.Bump = 0.5f; return h; }
            if (Has(k, "metal", "steel", "iron", "bronze", "brass", "gold", "silver", "chrome", "copper")) { h.Metallic = 0.75f; h.Smoothness = 0.5f; return h; }
            if (Has(k, "eye", "iris", "pupil", "sclera")) { h.Smoothness = 0.8f; h.Bump = 0.5f; return h; }
            if (Has(k, "skin", "face", "body")) { h.Smoothness = 0.35f; h.Bump = 0.35f; return h; }
            if (Has(k, "leather", "boot", "belt")) { h.Smoothness = 0.25f; h.Bump = 0.65f; return h; }
            if (Has(k, "cloth", "wool", "cotton", "linen", "fabric", "apron", "shirt", "trouser", "coat", "cloak", "robe")) { h.Smoothness = 0.2f; h.Bump = 0.65f; return h; }
            return h;
        }

        private static bool Has(string key, params string[] words)
        {
            foreach (string w in words) if (key.IndexOf(w, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>Reads "opaque", "cutout" or "transparent" (any case) from livecast.json. False for anything else.</summary>
        public static bool ParseMode(string text, out LiveSurfaceMode mode)
        {
            mode = LiveSurfaceMode.Opaque;
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "opaque": mode = LiveSurfaceMode.Opaque; return true;
                case "cutout": mode = LiveSurfaceMode.Cutout; return true;
                case "transparent": mode = LiveSurfaceMode.Transparent; return true;
                default: return false;
            }
        }

        /// <summary>The H9 bone a socket role sits on ("muzzle" gives "hand_r", "chest" gives "spine_03"), or "".</summary>
        public static string SocketBone(string role)
        {
            int index = SocketRoles.IndexOf(role ?? "");
            return index >= 0 ? SocketBones[index] : "";
        }

        /// <summary>
        /// Which way an export faces. The forward marker (review_forward) wins; without it, toes in front of the ankles;
        /// without toes, a spread left hand at -X. Positions are in the model's own space after import (+Z forward, +X right).
        /// </summary>
        public static LiveFacing Facing(bool hasForward, float forwardZ, float headZ, bool hasToes, float toeZ, float ankleZ, float leftHandX, float rightHandX)
        {
            if (hasForward)
            {
                if (forwardZ > headZ + 0.01f) return LiveFacing.AsIs;
                if (forwardZ < headZ - 0.01f) return LiveFacing.TurnAround;
                return LiveFacing.Unknown;
            }
            if (hasToes && Mathf.Abs(toeZ - ankleZ) > 0.02f) return toeZ > ankleZ ? LiveFacing.AsIs : LiveFacing.TurnAround;
            if (Mathf.Abs(leftHandX - rightHandX) > 0.1f) return leftHandX < rightHandX ? LiveFacing.AsIs : LiveFacing.TurnAround;
            return LiveFacing.Unknown;
        }

        /// <summary>
        /// The rebuild fingerprint: the recipe, the render pipeline and every source path with its content hash, sorted by
        /// path so the order files were found in does not matter. Any changed byte, file, recipe or pipeline changes it.
        /// </summary>
        public static string Stamp(string recipe, bool urp, IList<string> paths, IList<string> hashes)
        {
            if (paths == null || hashes == null || paths.Count != hashes.Count) throw new ArgumentException("Every fingerprinted path needs one hash.");
            int[] order = new int[paths.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => string.CompareOrdinal(paths[a], paths[b]));
            StringBuilder s = new StringBuilder(recipe ?? "").Append('|').Append(urp ? "urp" : "builtin");
            foreach (int i in order) s.Append('|').Append(paths[i]).Append('=').Append(hashes[i] ?? "");
            return s.ToString();
        }

        /// <summary>True when a source must be prepared again: no stored fingerprint, a different one, or a missing prefab.</summary>
        public static bool NeedsRebuild(string stored, string current, bool prefabPresent)
        {
            return string.IsNullOrEmpty(stored) || !string.Equals(stored, current, StringComparison.Ordinal) || !prefabPresent;
        }

        /// <summary>True for a changed project file that can change a live character (a drop-in folder or the H9 art), never
        /// for generated Prepared output, so the generator's own writes do not schedule another run.</summary>
        public static bool WatchedSource(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            bool dropIn = assetPath.StartsWith(Root + "/", StringComparison.Ordinal);
            bool builtin = assetPath.StartsWith(BuiltinRoot + "/", StringComparison.Ordinal);
            if (!dropIn && !builtin) return false;
            return assetPath.IndexOf("/" + PreparedFolder + "/", StringComparison.Ordinal) < 0 && !assetPath.EndsWith("/" + PreparedFolder, StringComparison.Ordinal);
        }

        /// <summary>A file-name-safe version of a folder name (letters and digits only), for generated asset names.</summary>
        public static string SafeName(string folder)
        {
            StringBuilder s = new StringBuilder();
            foreach (char c in folder ?? "")
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) s.Append(c);
            return s.Length > 0 ? s.ToString() : "Character";
        }
    }
}
