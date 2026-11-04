using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The hair, beard and headwear meshes of one live body prefab (a crowd study or a drop-in), built once per style,
    /// headpiece and beard from the body's measured skull (LiveSkullFit) and shared by every person who wears that body:
    /// a mesh rides each person's head bone rigidly, so a style is one mesh however many people wear it. Meshes are made
    /// on first use and live for the play session. Shared between the crowd rows and the cast rows of the same prefab.
    /// </summary>
    public sealed class LiveHairKit
    {
        /// <summary>The prefab the kit was measured from.</summary>
        public readonly GameObject Prefab;
        /// <summary>The skull measurement (Valid false means no hair for this body; Reason says why).</summary>
        public readonly LiveSkullFit Fit;
        private readonly Dictionary<int, Mesh> _hair = new Dictionary<int, Mesh>();
        private readonly Dictionary<int, Mesh> _beards = new Dictionary<int, Mesh>();
        private readonly Dictionary<int, Mesh> _heads = new Dictionary<int, Mesh>();
        /// <summary>Meshes built so far (the F2 page).</summary>
        public int Built { get; private set; }

        public LiveHairKit(GameObject prefab, LiveSkullFit fit)
        {
            Prefab = prefab;
            Fit = fit;
        }

        /// <summary>The hair mesh of a style under a headpiece, or null when nothing shows (bald, or under a helm or hood).</summary>
        public Mesh Hair(HairStyle style, LiveHeadpiece under)
        {
            if (Fit == null || !Fit.Valid || !LiveHairRules.ShowsHair(style, LiveHairRules.Cover(under))) return null;
            LiveHairCover cover = LiveHairRules.Cover(under);
            // Pieces that leave the hair alone share one mesh per style; capped pieces each have their own line.
            int key = (int)style * 64 + (cover == LiveHairCover.Capped ? (int)under : 0);
            Mesh mesh;
            if (_hair.TryGetValue(key, out mesh)) return mesh;
            MeshBuilder b = new MeshBuilder(LiveHairShells.HairSubmeshes);
            mesh = LiveHairShells.BuildHair(b, Fit, style, cover == LiveHairCover.Capped ? under : LiveHeadpiece.None) ? Finish(b, "Live hair " + style + (cover == LiveHairCover.Capped ? " under " + under : "")) : null;
            _hair.Add(key, mesh);
            return mesh;
        }

        /// <summary>The beard mesh, or null for none.</summary>
        public Mesh Beard(LiveBeard beard)
        {
            if (Fit == null || !Fit.Valid || beard == LiveBeard.None) return null;
            Mesh mesh;
            if (_beards.TryGetValue((int)beard, out mesh)) return mesh;
            MeshBuilder b = new MeshBuilder(LiveHairShells.BeardSubmeshes);
            mesh = LiveHairShells.BuildBeard(b, Fit, beard) ? Finish(b, "Live beard " + beard) : null;
            _beards.Add((int)beard, mesh);
            return mesh;
        }

        /// <summary>The headpiece mesh, or null for none.</summary>
        public Mesh Headpiece(LiveHeadpiece piece)
        {
            if (Fit == null || !Fit.Valid || piece == LiveHeadpiece.None) return null;
            Mesh mesh;
            if (_heads.TryGetValue((int)piece, out mesh)) return mesh;
            MeshBuilder b = new MeshBuilder(LiveHairShells.HeadSubmeshes);
            mesh = LiveHairShells.BuildHeadpiece(b, Fit, piece) ? Finish(b, "Live headwear " + piece) : null;
            _heads.Add((int)piece, mesh);
            return mesh;
        }

        private Mesh Finish(MeshBuilder b, string name)
        {
            Mesh mesh = b.ToMesh(name);
            mesh.UploadMeshData(true);
            Built++;
            return mesh;
        }
    }

    /// <summary>The kits of this play session, one per live body prefab.</summary>
    public static class LiveHairKits
    {
        private static readonly Dictionary<GameObject, LiveHairKit> _kits = new Dictionary<GameObject, LiveHairKit>();

        /// <summary>Forgets every kit (Enter Play Mode without a domain reload starts clean).</summary>
        public static void ResetStatics()
        {
            _kits.Clear();
        }

        /// <summary>The kit measured for a prefab this session, or null.</summary>
        public static LiveHairKit Find(GameObject prefab)
        {
            LiveHairKit kit;
            return prefab != null && _kits.TryGetValue(prefab, out kit) ? kit : null;
        }

        /// <summary>Remembers a kit for a prefab (the first live instance of a body measures it).</summary>
        public static void Add(LiveHairKit kit)
        {
            if (kit == null || kit.Prefab == null) return;
            _kits[kit.Prefab] = kit;
        }

        /// <summary>How many prefabs were measured, how many of them have a usable skull, and the meshes built (the F2 page).</summary>
        public static string Summary()
        {
            int valid = 0, built = 0;
            foreach (LiveHairKit kit in _kits.Values)
            {
                if (kit.Fit != null && kit.Fit.Valid) valid++;
                built += kit.Built;
            }
            return "skulls measured " + _kits.Count + " (" + valid + " usable), hair and headwear meshes built " + built;
        }
    }
}
