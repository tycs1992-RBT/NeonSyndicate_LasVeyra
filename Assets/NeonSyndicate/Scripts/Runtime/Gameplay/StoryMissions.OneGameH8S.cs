using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class StoryMissions
    {
        /// <summary>
        /// One game (owner decision 2026-10-23): the story plays in world revision 10, which has no downtown street grid.
        /// Called once at boot after the story is built. In revision 10 only, the story race (chapter 3, Ghost Lanes) runs
        /// on that world's real roads (CityRacesH8S); its id, rivals, reward, stat and the rest of the story are unchanged.
        /// Every other world keeps the original script.
        /// </summary>
        public static void FitWorldH8S(MissionManager missions, CityLayout layout)
        {
            if (missions == null || layout == null || !layout.GothicCivicH5) return;
            foreach (MissionDef def in missions.Story)
                if (def.Id == "m02_ghost_lanes") def.Script = GhostLanesCivicH8S;
        }

        /// <summary>
        /// Ghost Lanes in revision 10: the original chapter (same lines, loaner, rivals and stat) with Nico's route line
        /// and the checkpoints moved onto Ashline Avenue, the Beltway and the north rim. The loaner faces the first
        /// checkpoint because Nico's street is a dead end in this world.
        /// </summary>
        private static IEnumerator GhostLanesCivicH8S(MissionContext c)
        {
            Vector3 nico = MissionManager.PoiPos("Nico Ash", new Vector3(650f, 0f, 400f));
            StoryStage04L.Place(c, PedManager.Ground(nico + new Vector3(0f, 0f, 8f)), SagaSet04L.GhostLights);
            yield return c.Talk("Nico Ash", "You're the new guy Jax keeps talking about. Heard you flew over Skyrail last night. Can you actually drive?",
                "Malik", "Point me at the finish line.",
                "Nico Ash", CityRacesH8S.GhostLanesCivicLine);
            Vector3[] way = CityRacesH8S.GhostLanes(nico, true);
            for (int i = 0; i < way.Length; i++) way[i] = MissionManager.RoadNear(way[i]);
            float yaw;
            Vector3 startRoad = CityRacesH8S.GhostLanesStart(c.L, nico, way[0], true, out yaw);
            if (!c.Player.InVehicle)
            {
                Vehicle loaner = c.Car(VehicleDb.Find("Northline Arrow"), startRoad, yaw, new Color(0.95f, 0.75f, 0.05f));
                yield return c.EnterVehicle(loaner, "Get in Nico's Northline Arrow.");
            }
            List<Vector3> cps = RaceCheckpoints(c.Player.CurrentVehicle.transform.position, way, 170f);
            yield return RunRace(c, cps, new[] { VehicleDb.Find("Mistral Comet"), VehicleDb.Find("Torque Falcon") }, "GHOST LANES");
            if (Game.Stats != null) Game.Stats.Add(StatId.RacesWon, 1);
            yield return c.Talk("Nico Ash", "Okay. Okay! You drive like the road owes you money. I like it.",
                "Nico Ash", "There are more races around town. Look for the white F flags on your map.",
                "Nico Ash", "And Malik? Those ghost lights chased you the whole way. Something over there knows your name.");
        }
    }
}
