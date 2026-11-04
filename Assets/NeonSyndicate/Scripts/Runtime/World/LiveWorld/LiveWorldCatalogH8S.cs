using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The prepared list of live world pieces that a game build carries. The Editor writes it (Neon Syndicate > Live World,
    /// on project load, before Play and before every build) into Resources/LiveWorldPrepared, a generated and git-ignored
    /// folder, so the build includes each piece's model and textures. Each entry names a piece folder under
    /// Art/LiveWorld, its placement text and the model and textures it found. The runtime (LiveWorldArtH8S) reads it; when
    /// it is missing the world simply stays as it was.
    /// </summary>
    public sealed class LiveWorldCatalogH8S : ScriptableObject
    {
        /// <summary>Where the prepared list lives inside Resources (without extension).</summary>
        public const string ResourcePath = "LiveWorldPrepared/LiveWorldCatalog";
        /// <summary>Format version of this list.</summary>
        public int Version = 1;
        /// <summary>When the Editor last prepared the list (UTC, for the debug page).</summary>
        public string PreparedUtc = "";
        /// <summary>One entry per piece folder that has a placement file.</summary>
        public List<LiveWorldEntryH8S> Entries = new List<LiveWorldEntryH8S>();
    }

    /// <summary>One prepared piece: its folder, placement text, model and texture sets, and any problem the Editor found.</summary>
    [Serializable]
    public sealed class LiveWorldEntryH8S
    {
        /// <summary>The piece's name (its folder name).</summary>
        public string Name = "";
        /// <summary>The piece folder's asset path (for messages and the Editor's re-read).</summary>
        public string Folder = "";
        /// <summary>The placement file's text when it was prepared.</summary>
        public string Placement = "";
        /// <summary>The asset path of the model that was used.</summary>
        public string ModelPath = "";
        /// <summary>The imported model (null when it was not found or did not import).</summary>
        public GameObject Model;
        /// <summary>The texture sets found next to the piece or its model.</summary>
        public List<LiveWorldTextureSetH8S> Textures = new List<LiveWorldTextureSetH8S>();
        /// <summary>True when the model's meshes keep a CPU copy, so world surfaces can get world-scale texture coordinates.</summary>
        public bool Readable;
        /// <summary>What the Editor could not use ("" when nothing): the runtime skips the piece with this reason.</summary>
        public string Problem = "";
        /// <summary>Readable notes from the Editor (texture names it could not use, settings it corrected).</summary>
        public List<string> Notes = new List<string>();
    }

    /// <summary>One texture set: the maps that share a name prefix, for example Metal_BaseColor and Metal_Normal.</summary>
    [Serializable]
    public sealed class LiveWorldTextureSetH8S
    {
        /// <summary>The shared name prefix (Metal, MyTower).</summary>
        public string Set = "";
        /// <summary>Colour map (sRGB).</summary>
        public Texture2D BaseColor;
        /// <summary>Normal map (imported as a normal map).</summary>
        public Texture2D Normal;
        /// <summary>Metallic in red, smoothness in alpha (linear).</summary>
        public Texture2D MetallicSmoothness;
        /// <summary>Glow colour map (sRGB); only used when present.</summary>
        public Texture2D Emission;
        /// <summary>Ambient occlusion in green (linear).</summary>
        public Texture2D Occlusion;
    }
}
