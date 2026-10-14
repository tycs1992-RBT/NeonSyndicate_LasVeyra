using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Shared fixtures for the EditMode tests: the city for the default seed (generated once per test run, never
    /// modified), finiteness helpers and a simple building-volume check for camera placement.
    /// </summary>
    internal static class TestData
    {
        /// <summary>The seed the game always plays (Game.Seed).</summary>
        public const int DefaultSeed = 61724;

        /// <summary>Category for tests that need the Unity engine itself (they are skipped by the offline runner).</summary>
        public const string NeedsEngine = "NeedsEngine";

        private static CityLayout _city;

        public static CityLayout City
        {
            get
            {
                if (_city == null)
                {
                    CityLayout c = new CityLayout(DefaultSeed);
                    c.Generate();
                    _city = c;
                }
                return _city;
            }
        }

        public static bool Finite(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        public static bool Finite(Vector3 v)
        {
            return Finite(v.x) && Finite(v.y) && Finite(v.z);
        }

        public static bool Finite(Quaternion q)
        {
            return Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w);
        }

        public static bool Finite(Color c)
        {
            return Finite(c.r) && Finite(c.g) && Finite(c.b) && Finite(c.a);
        }

        /// <summary>True when the point is inside the playable map square (horizontally).</summary>
        public static bool InsideMap(Vector3 p)
        {
            return Mathf.Abs(p.x) <= CityLayout.MapHalf && Mathf.Abs(p.z) <= CityLayout.MapHalf;
        }

        /// <summary>
        /// Name of the building whose volume (footprint widened by margin, up to its roof plus roofMargin) contains the
        /// point, or null when the point is in the open. Covers city blocks and the desert sites.
        /// </summary>
        public static string BuildingAt(CityLayout layout, Vector3 p, float margin, float roofMargin)
        {
            foreach (Block b in layout.Blocks)
            {
                if (!b.Curb.Inset(-margin).Contains(p)) continue;
                foreach (Lot lot in b.Lots)
                    foreach (Building bld in lot.Buildings)
                        if (bld.Footprint.Inset(-margin).Contains(p) && p.y < CityLayout.CurbHeight + bld.Height + roofMargin)
                            return b.District + " building " + bld.Footprint + " (" + bld.Height.ToString("0") + " m)";
            }
            foreach (Lot lot in layout.DesertLots)
                foreach (Building bld in lot.Buildings)
                    if (bld.Footprint.Inset(-margin).Contains(p) && p.y < CityLayout.CurbHeight + bld.Height + roofMargin)
                        return (lot.PoiName ?? "desert") + " building " + bld.Footprint;
            return null;
        }

        /// <summary>Every bone's world position for a solved pose (the same forward kinematics HumanoidRig applies).</summary>
        public static Vector3[] PoseWorld(HumanoidPose pose, Vector3 root, float yaw, out Quaternion[] rotations)
        {
            RigLayout L = pose.Layout;
            int n = RigLayout.BoneCount;
            Vector3[] pos = new Vector3[n];
            rotations = new Quaternion[n];
            Quaternion rr = QMath.AY(yaw);
            for (int i = 0; i < n; i++)
            {
                int parent = RigLayout.Parent[i];
                Vector3 local = i == 0 ? pose.HipsPosition : L.Local[i];
                if (parent < 0)
                {
                    pos[i] = root + rr * local;
                    rotations[i] = rr * pose.Local[i];
                }
                else
                {
                    pos[i] = pos[parent] + rotations[parent] * local;
                    rotations[i] = rotations[parent] * pose.Local[i];
                }
            }
            return pos;
        }

        /// <summary>Distinct, repeatable looks for the body and pose tests (Malik, Luma, police, SWAT, armoured and random civilians).</summary>
        public static Appearance Look(string name)
        {
            switch (name)
            {
                case "malik": return Appearance.Malik();
                case "luma": return Appearance.Luma();
                case "police": return Appearance.Police(new System.Random(11));
                case "swat": return Appearance.Swat(new System.Random(12));
                case "armoured":
                    {
                        Appearance a = Appearance.Random(new System.Random(13));
                        a.ShellStyle = 3;
                        a.GauntletStyle = 1;
                        a.StriderStyle = 1;
                        a.VisorStyle = 2;
                        return a;
                    }
                default:
                    {
                        int seed = 100;
                        if (name.StartsWith("random")) int.TryParse(name.Substring(6), out seed);
                        return Appearance.Random(new System.Random(seed));
                    }
            }
        }

        public static List<Vector3> AllPositions(CityLayout L)
        {
            List<Vector3> list = new List<Vector3>();
            foreach (RoadNode n in L.Nodes) list.Add(n.Pos);
            foreach (WalkNode w in L.WalkNodes) list.Add(w.Pos);
            foreach (Poi p in L.Pois) list.Add(p.Position);
            foreach (SpecialSite s in L.Sites) list.Add(s.Position);
            foreach (PropSpot p in L.Props) list.Add(p.Pos);
            foreach (LanePath lane in L.Lanes) list.AddRange(lane.Points);
            foreach (StuntJumpDef j in L.StuntJumps) list.Add(j.Position);
            list.AddRange(L.DataChipSpots);
            list.AddRange(L.CrystalShardSpots);
            list.AddRange(L.SpellStoneSpots);
            list.AddRange(L.CreatureSpots);
            list.AddRange(L.PickupSpots);
            list.AddRange(L.ParkingSpots);
            list.AddRange(L.StreetLampSpots);
            list.AddRange(L.TreeSpots);
            list.AddRange(L.PalmSpots);
            list.AddRange(L.CactusSpots);
            list.AddRange(L.RockSpots);
            list.AddRange(L.RadioMastSpots);
            return list;
        }
    }
}
