using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The kinds of generated city interior (blueprint item: enterable interiors).</summary>
    public enum InteriorKind { NoodleBar, TeaHouse, Boutique, Pawnshop, Gym, Bookshop, Chapel, Arcade }

    /// <summary>One generated interior: the building it belongs to, its street door and the room above.</summary>
    public sealed class InteriorSite
    {
        /// <summary>Stable id saved in InteriorSave.Visited ("int-" plus the building seed; the default city never changes it).</summary>
        public string Id = "";
        public string Name = "";
        public InteriorKind Kind;
        public District District;
        public Building Building;
        public Lot Lot;
        /// <summary>The door on the facade (street level), the way into the building and the sidewalk point outside it.</summary>
        public Vector3 Door, Outside;
        /// <summary>Yaw of the direction from the street into the building (the room is built facing the same way).</summary>
        public float Yaw;
        /// <summary>The room's door corner, high above the building (InteriorRules.RoomAltitude).</summary>
        public Vector3 RoomOrigin;
        public int Seed;
    }

    /// <summary>
    /// Generated interiors, pure rules: the eight kinds (names, colours, size, service), which ordinary buildings get a
    /// street door (spread over the city districts, on a public sidewalk, away from story doors and from each other), and
    /// the stable ids saves remember. A room is built far above its own building only while the player is inside, so the
    /// map, the minimap and every distance to it stay right and nothing hangs in the sky (CityInteriors). No engine calls.
    /// </summary>
    public static class InteriorRules
    {
        /// <summary>How many generated interiors the city gets (with the twelve venues the blueprint's forty).</summary>
        public const int Target = 28;
        /// <summary>The height of every room above the street (far above the flight ceiling of TravelMath.SoftCeiling).</summary>
        public const float RoomAltitude = 1200f;
        /// <summary>The closest two interior doors may stand, in metres.</summary>
        public const float Spacing = 45f;
        /// <summary>The closest an interior door may stand to a point of interest door, in metres.</summary>
        public const float PoiClearance = 9f;
        /// <summary>Visiting every generated interior pays this much once.</summary>
        public const int AllVisitedReward = 2500;

        /// <summary>Room size per kind: width (x), depth (z, into the building) and height, in metres.</summary>
        public static Vector3 RoomSize(InteriorKind kind)
        {
            switch (kind)
            {
                case InteriorKind.NoodleBar: return new Vector3(9f, 4f, 11f);
                case InteriorKind.TeaHouse: return new Vector3(10f, 4f, 12f);
                case InteriorKind.Boutique: return new Vector3(10f, 4.2f, 12f);
                case InteriorKind.Pawnshop: return new Vector3(8f, 3.8f, 10f);
                case InteriorKind.Gym: return new Vector3(14f, 5f, 16f);
                case InteriorKind.Bookshop: return new Vector3(10f, 4.4f, 13f);
                case InteriorKind.Chapel: return new Vector3(11f, 7f, 18f);
                default: return new Vector3(12f, 4.2f, 14f);
            }
        }

        /// <summary>What the counter sells, with its price and the share of health it restores (0 when the room has no counter service).</summary>
        public static bool Service(InteriorKind kind, out string what, out int price, out float heal)
        {
            what = ""; price = 0; heal = 0f;
            switch (kind)
            {
                case InteriorKind.NoodleBar: what = "a bowl of ember broth"; price = 15; heal = 0.45f; return true;
                case InteriorKind.TeaHouse: what = "a pot of glasswater tea"; price = 10; heal = 0.25f; return true;
                case InteriorKind.Gym: what = "a sparring round"; price = 25; heal = 0.15f; return true;
                default: return false;
            }
        }

        /// <summary>The kind's word for the HUD and the map ("noodle bar").</summary>
        public static string KindName(InteriorKind kind)
        {
            switch (kind)
            {
                case InteriorKind.NoodleBar: return "noodle bar";
                case InteriorKind.TeaHouse: return "tea house";
                case InteriorKind.Boutique: return "boutique";
                case InteriorKind.Pawnshop: return "pawnshop";
                case InteriorKind.Gym: return "gym";
                case InteriorKind.Bookshop: return "bookshop";
                case InteriorKind.Chapel: return "chapel";
                default: return "arcade";
            }
        }

        private static readonly string[][] Names =
        {
            new[] { "Ember Broth Counter", "Ninefold Noodles", "The Steam Lantern", "Cinder Bowl", "Night Ladle" },
            new[] { "Glasswater Tea Rooms", "The Quiet Kettle", "Amberleaf House", "Saltmoon Tea" },
            new[] { "Thread and Prism", "Veilcut Tailors", "House of Lumen Silk", "The Brass Hem" },
            new[] { "Second Light Pawn", "Odd Shard Exchange", "Last Ledger Loans", "Copperjaw Pawn" },
            new[] { "Ironlung Gym", "The Resonance Ring", "Hollowfist Boxing", "Quartzline Fitness" },
            new[] { "Dog-Ear Books", "The Margin Reader", "Lanternpage Books", "Spine and Ink" },
            new[] { "Chapel of the Low Bell", "Shrine of Still Water", "The Ninth Lamp Chapel", "Saint Ashlow's" },
            new[] { "Pixelvault Arcade", "The Coin Furnace", "Neon Ladder Arcade", "Bitglass Hall" }
        };

        /// <summary>The colour index (Mats.Neon) of a kind's sign and door frame.</summary>
        public static int NeonIndex(InteriorKind kind)
        {
            return (int)kind * 3 + 1;
        }

        /// <summary>The kind for the n-th selected interior (each kind three or four times, mixed).</summary>
        public static InteriorKind KindFor(int n)
        {
            return (InteriorKind)(((n * 5) + n / 8) % 8);
        }

        /// <summary>A unique name for a site: the kind's list in turn, then the list again with the district's short name.</summary>
        public static string NameFor(InteriorKind kind, int nthOfKind, string districtShort)
        {
            string[] list = Names[(int)kind];
            string name = list[nthOfKind % list.Length];
            return nthOfKind < list.Length ? name : name + " " + districtShort;
        }

        /// <summary>The stable id of the interior in a building.</summary>
        public static string IdFor(Building building)
        {
            return "int-" + (building != null ? building.Seed : 0);
        }

        /// <summary>True for the districts that get interiors (the wilderness zones have no shopfronts).</summary>
        public static bool CityDistrict(District d)
        {
            return d != District.QuartzBadlands && d != District.Frostline;
        }

        /// <summary>
        /// Picks up to Target buildings for interiors: ordinary one-storey-plus buildings (InhabitedCityBindings04C.Eligible)
        /// on lots with no story point of interest, not a venue (excluded), with a door that opens onto the block's public
        /// sidewalk, PoiClearance from any point of interest door and Spacing from each other. Districts take turns, so the
        /// interiors spread over the whole city; within a district the order follows a hash of the building seed.
        /// </summary>
        public static List<InteriorSite> Select(CityLayout layout, ICollection<Building> excluded)
        {
            List<InteriorSite> result = new List<InteriorSite>();
            if (layout == null || layout.Blocks == null) return result;
            Dictionary<District, List<InteriorSite>> byDistrict = new Dictionary<District, List<InteriorSite>>();
            foreach (Block block in layout.Blocks)
            {
                if (block == null || block.IsPark || !CityDistrict(block.District)) continue;
                foreach (Lot lot in block.Lots)
                {
                    if (lot == null || lot.CustomBuilt || lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName)) continue;
                    if (lot.Use != LotUse.Building && lot.Use != LotUse.Tower && lot.Use != LotUse.Projects) continue;
                    foreach (Building b in lot.Buildings)
                    {
                        if (b == null || (excluded != null && excluded.Contains(b)) || !InhabitedCityBindings04C.Eligible(b)) continue;
                        Vector3 origin, outward; float yaw;
                        if (!StreetDoor(block, b.Footprint, out origin, out outward, out yaw)) continue;
                        if (NearPoi(layout, origin)) continue;
                        InteriorSite site = new InteriorSite
                        {
                            Id = IdFor(b), Building = b, Lot = lot, District = block.District, Door = origin, Outside = origin + outward * 2.2f,
                            Yaw = yaw, Seed = b.Seed
                        };
                        List<InteriorSite> list;
                        if (!byDistrict.TryGetValue(block.District, out list)) { list = new List<InteriorSite>(); byDistrict.Add(block.District, list); }
                        list.Add(site);
                    }
                }
            }
            List<District> districts = new List<District>(byDistrict.Keys);
            districts.Sort();
            foreach (District d in districts) byDistrict[d].Sort((a, b) => Order(a.Seed).CompareTo(Order(b.Seed)));
            int[] next = new int[districts.Count];
            bool progress = true;
            while (result.Count < Target && progress)
            {
                progress = false;
                for (int i = 0; i < districts.Count && result.Count < Target; i++)
                {
                    List<InteriorSite> list = byDistrict[districts[i]];
                    while (next[i] < list.Count)
                    {
                        InteriorSite candidate = list[next[i]++];
                        if (TooClose(result, candidate.Door)) continue;
                        result.Add(candidate);
                        progress = true;
                        break;
                    }
                }
            }
            int[] perKind = new int[8];
            for (int n = 0; n < result.Count; n++)
            {
                InteriorSite s = result[n];
                s.Kind = KindFor(n);
                s.Name = NameFor(s.Kind, perKind[(int)s.Kind]++, CityLayout.DistrictName(s.District));
                s.RoomOrigin = new Vector3(s.Door.x, RoomAltitude, s.Door.z);
            }
            return result;
        }

        /// <summary>
        /// The street door of a footprint: of its four sides, the one nearest the block's edge whose door opens onto the
        /// public sidewalk (most lots do not record a facing). False when no side reaches the sidewalk or the building is
        /// too narrow for a shopfront.
        /// </summary>
        public static bool StreetDoor(Block block, RectF footprint, out Vector3 origin, out Vector3 outward, out float yaw)
        {
            origin = Vector3.zero; outward = Vector3.zero; yaw = 0f;
            if (block == null || !footprint.Valid) return false;
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            float[] gaps =
            {
                block.Inner.X1 - footprint.X1, footprint.X0 - block.Inner.X0, block.Inner.Z1 - footprint.Z1, footprint.Z0 - block.Inner.Z0
            };
            bool found = false;
            float best = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                if (gaps[i] >= best) continue;
                Vector3 o; float width, depth, y;
                InhabitedCityBindings04C.Door(footprint, dirs[i], out o, out width, out depth, out y);
                if (width < 8f || depth < 8f) continue;
                if (!InhabitedCityBindings04C.DoorOnSidewalk04E(block, o, dirs[i])) continue;
                best = gaps[i]; origin = o; outward = dirs[i]; yaw = y; found = true;
            }
            return found;
        }

        /// <summary>A deterministic order inside a district (the building seeds are dense, the hash spreads them).</summary>
        public static uint Order(int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return h;
            }
        }

        private static bool TooClose(List<InteriorSite> chosen, Vector3 door)
        {
            foreach (InteriorSite s in chosen) if (MathUtil.FlatSqrDistance(s.Door, door) < Spacing * Spacing) return true;
            return false;
        }

        private static bool NearPoi(CityLayout layout, Vector3 door)
        {
            if (layout.Pois == null) return false;
            foreach (Poi poi in layout.Pois)
                if (poi != null && MathUtil.FlatSqrDistance(poi.Position, door) < PoiClearance * PoiClearance) return true;
            return false;
        }

        /// <summary>
        /// True when the player may go in through a door: on foot, alive, no wanted stars, not in a fight and not in the Veil.
        /// why says what stops them.
        /// </summary>
        public static bool CanEnter(bool onFoot, bool alive, int wantedStars, bool inCombat, bool inVeil, out string why)
        {
            why = "";
            if (!alive) { why = "not now"; return false; }
            if (!onFoot) { why = "get out of the vehicle first"; return false; }
            if (wantedStars > 0) { why = "lose the pursuit first"; return false; }
            if (inCombat) { why = "the door stays shut during a fight"; return false; }
            if (inVeil) { why = "step out of the Veil first"; return false; }
            return true;
        }
    }
}
