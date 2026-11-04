using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One street race: stable id, name, rivals and prize, where its start line is looked for, its waypoints (snapped to the nearest road by the caller) and Nico's intro line.</summary>
    public sealed class CityRaceRouteH8S
    {
        public string Id = "";
        public string Name = "";
        public string[] Rivals = new string[0];
        public int Prize;
        public Vector3 Near;
        public Vector3[] Way = new Vector3[0];
        public string Intro = "";
    }

    /// <summary>
    /// Where the city street races and the story race (Ghost Lanes) run. The original city keeps its original routes.
    /// World revision 10, the world of the unified game, has no downtown street grid: only the perimeter avenues, the
    /// Badlands Beltway, the Mineral service circuit around the central court and its approaches. Its races follow those
    /// real roads instead (same ids, names, rivals and prizes), so no checkpoint sits at a dead end, off the road or in a
    /// building. Pure data and maths, so the tests check exactly the routes the game uses.
    /// </summary>
    public static class CityRacesH8S
    {
        /// <summary>The three city street races (strip, canal, skyrail), in the order Activities adds them.</summary>
        public static CityRaceRouteH8S[] StreetRaces(bool civic)
        {
            if (civic) return CivicStreetRaces();
            float sx = CityLayout.StripX;
            return new[]
            {
                new CityRaceRouteH8S { Id = "race_strip", Name = "Strip Sprint", Rivals = StripRivals(), Prize = 3000, Near = new Vector3(sx, 0f, -380f),
                    Way = new[] { new Vector3(sx, 0f, 430f), new Vector3(660f, 0f, 430f), new Vector3(660f, 0f, -160f), new Vector3(sx, 0f, -380f) },
                    Intro = "Mirage Strip, end to end and back. The lights are just suggestions tonight." },
                new CityRaceRouteH8S { Id = "race_canal", Name = "Canal Kings", Rivals = CanalRivals(), Prize = 3500, Near = new Vector3(-660f, 0f, -470f),
                    Way = new[] { new Vector3(660f, 0f, -470f), new Vector3(660f, 0f, -690f), new Vector3(-660f, 0f, -690f), new Vector3(-660f, 0f, -470f) },
                    Intro = "Both banks of the canal. The bridges are narrow, and so are my rivals' brains." },
                new CityRaceRouteH8S { Id = "race_skyrail", Name = "Skyrail Circuit", Rivals = SkyrailRivals(), Prize = 4500, Near = new Vector3(700f, 0f, 260f),
                    Way = new[] { new Vector3(700f, 0f, 650f), new Vector3(-160f, 0f, 650f), new Vector3(-160f, 0f, 220f), new Vector3(700f, 0f, 260f) },
                    Intro = "Skyrail Orbit into Glass Heights and home. Supercars only. Bring one or pray." }
            };
        }

        private static string[] StripRivals() { return new[] { "Vanta Arrow", "Orion Vista", "Mistral Comet" }; }
        private static string[] CanalRivals() { return new[] { "Torque Falcon", "Kestrel Marauder", "Bishop Barrage" }; }
        private static string[] SkyrailRivals() { return new[] { "Bishop Vektor", "Torque Surge", "Mistral Comet" }; }

        /// <summary>Revision 10 loops: the south rim and Beltway, both canal bridges through the court's ring, and Skyrail through Glass Heights.</summary>
        private static CityRaceRouteH8S[] CivicStreetRaces()
        {
            return new[]
            {
                // 1st Avenue east, Ashline Avenue north, Core Avenue East, the Beltway south and west, Veyra Avenue South.
                new CityRaceRouteH8S { Id = "race_strip", Name = "Strip Sprint", Rivals = StripRivals(), Prize = 3000, Near = new Vector3(300f, 0f, -800f),
                    Way = new[] { new Vector3(650f, 0f, -800f), new Vector3(800f, 0f, -400f), new Vector3(900f, 0f, 0f), new Vector3(1000f, 0f, -500f),
                        new Vector3(500f, 0f, -1000f), new Vector3(0f, 0f, -900f), new Vector3(300f, 0f, -800f) },
                    Intro = "The south rim, Ashline and the Beltway, then back to the Strip. The lights are just suggestions tonight." },
                // North over the west canal bridge, along the ring's south side, south over the east bridge, home on the realm returns.
                new CityRaceRouteH8S { Id = "race_canal", Name = "Canal Kings", Rivals = CanalRivals(), Prize = 3500, Near = new Vector3(-150f, 0f, -800f),
                    Way = new[] { new Vector3(-150f, 0f, -400f), new Vector3(0f, 0f, -150f), new Vector3(155f, 0f, -400f), new Vector3(155f, 0f, -900f),
                        new Vector3(75f, 0f, -1000f), new Vector3(-75f, 0f, -1000f), new Vector3(-150f, 0f, -850f) },
                    Intro = "Both banks of the canal. The bridges are narrow, and so are my rivals' brains." },
                // Ashline north, 16th Avenue west, down the west approach into Glass Heights, along the ring, back out east.
                new CityRaceRouteH8S { Id = "race_skyrail", Name = "Skyrail Circuit", Rivals = SkyrailRivals(), Prize = 4500, Near = new Vector3(800f, 0f, 260f),
                    Way = new[] { new Vector3(800f, 0f, 600f), new Vector3(400f, 0f, 800f), new Vector3(0f, 0f, 900f), new Vector3(-75f, 0f, 1000f),
                        new Vector3(-150f, 0f, 600f), new Vector3(0f, 0f, 252f), new Vector3(155f, 0f, 600f), new Vector3(230f, 0f, 1000f),
                        new Vector3(650f, 0f, 1000f), new Vector3(1000f, 0f, 500f), new Vector3(900f, 0f, 0f), new Vector3(800f, 0f, 150f) },
                    Intro = "Skyrail Orbit into Glass Heights and home. Supercars only. Bring one or pray." }
            };
        }

        /// <summary>
        /// Where Nico's loaner waits for the story race and which way it faces: on the lane nearest to a point 12 m
        /// north of Nico (the same point and lane as MissionManager.RoadNear and RoadYawNear give the story script). In
        /// revision 10 Nico's street ends in a dead end, so the loaner takes the lane heading toward the first checkpoint.
        /// </summary>
        public static Vector3 GhostLanesStart(CityLayout layout, Vector3 nico, Vector3 firstWaypoint, bool civic, out float yaw)
        {
            Vector3 near = nico + new Vector3(0f, 0f, 12f);
            yaw = 0f;
            if (layout == null) return near;
            float along = 0f;
            int lane = civic ? layout.NearestLaneHeading(near, 40f, firstWaypoint - near, out along) : -1;
            if (lane < 0) lane = layout.NearestLane(near, 120f, out along, true);
            if (lane < 0)
            {
                int edge = layout.NearestEdge(near);
                return edge >= 0 ? layout.ProjectOnEdge(layout.Edges[edge], near) : near;
            }
            yaw = MathUtil.YawOf(layout.Lanes[lane].DirectionAt(along));
            return layout.Lanes[lane].PointAt(along);
        }

        /// <summary>The story race's waypoints from Nico Ash's corner (before snapping to roads).</summary>
        public static Vector3[] GhostLanes(Vector3 nico, bool civic)
        {
            if (civic)
            {
                // Revision 10: Ashline Avenue south, Core Avenue East, the Beltway north and west, Veyra Avenue North,
                // 16th Avenue east and Ashline south again to the finish at Nico's corner.
                return new[]
                {
                    new Vector3(800f, 0f, 200f), new Vector3(900f, 0f, 0f), new Vector3(1000f, 0f, 500f), new Vector3(650f, 0f, 1000f),
                    new Vector3(0f, 0f, 900f), new Vector3(400f, 0f, 800f), new Vector3(800f, 0f, 600f), new Vector3(nico.x + 25f, 0f, 350f)
                };
            }
            return new[]
            {
                new Vector3(CityLayout.StripX, 0f, 250f), new Vector3(CityLayout.StripX, 0f, -250f),
                new Vector3(60f, 0f, -150f), new Vector3(60f, 0f, 300f), nico
            };
        }

        /// <summary>Nico's description of the revision 10 route (the original line names streets that world no longer has).</summary>
        public const string GhostLanesCivicLine = "Ghost Lanes. Skyrail, the Beltway, the north rim and back. Named for the red lights that show a second city under the road.";
    }
}
