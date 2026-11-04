using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The five lands of the wilds around Las Veyra (WildsLayout.BiomeAt).</summary>
    public enum WildBiome
    {
        /// <summary>West: red rock pillars, arches, layered buttes and boulder fields.</summary>
        HoodooBadlands = 0,
        /// <summary>South: wind-shaped dunes, bleached bones, dry grass.</summary>
        DuneSea = 1,
        /// <summary>East: glassy ground and quartz crystals that glow at night.</summary>
        CrystalBarrens = 2,
        /// <summary>North and the band around the beltway: scrub, junipers, rock outcrops.</summary>
        ScrubSteppe = 3,
        /// <summary>The Frostline's snow field: ice spires, snow boulders, frozen dead trees.</summary>
        FrostTaiga = 4
    }

    /// <summary>What a placed piece of the wilds is (WildsShapes builds each one).</summary>
    public enum WildPieceKind
    {
        Boulder, Pebbles, RockSpire, Butte, Arch, Dune, Crystal, CrystalCluster, Shrub, GrassTuft, DeadTree, Juniper, Palm,
        Reeds, Pond, RibBone, Skull, RuinWall, RuinColumn, RuinArchway, Watchtower, Tent, Campfire, WreckHull, WreckFin,
        Monolith, StoneRing, Geyser, SaltPan, IceSpire, SnowBoulder, Obelisk, Turbine, Hut, Pier, Swell, GroundPatch
    }

    /// <summary>A named place to find in the wilds.</summary>
    public enum WildSiteKind
    {
        Oasis, CrystalGrove, HoodooMaze, TitanBones, SkyshipWreck, RuinedVillage, StoneCircle, Watchtower, NomadCamp,
        GeyserFlats, SunkenTemple, WindRelay, IceCave, FrozenWreck, HotSpring, Obelisks
    }

    /// <summary>One piece of the wilds: where, which way, how big, its own seed, and whether it blocks movement.</summary>
    public struct WildPiece
    {
        public WildPieceKind Kind;
        public Vector3 Position;
        public float Yaw;
        public Vector3 Size;
        public int Seed;
        public bool Solid;
        /// <summary>The named site this piece belongs to (-1 for the open scatter).</summary>
        public int Site;
        /// <summary>The land it stands in (picks the rock colour).</summary>
        public WildBiome Biome;
    }

    /// <summary>A named place: its id (saved), name, kind, land, centre and the radius that counts as visiting it.</summary>
    public sealed class WildSite
    {
        public int Index;
        public string Id;
        public string Name;
        public WildSiteKind Kind;
        public WildBiome Biome;
        public Vector3 Center;
        public float Radius;
        public int Seed;
    }

    /// <summary>The whole plan: the named sites and every piece (site dressing first, then the open scatter).</summary>
    public sealed class WildsPlan
    {
        public readonly List<WildSite> Sites = new List<WildSite>();
        public readonly List<WildPiece> Pieces = new List<WildPiece>();
        /// <summary>The gentle rises of the land (also in Pieces), for WildsLayout.SurfaceAt.</summary>
        public readonly List<WildPiece> Swells = new List<WildPiece>();
    }
}
