namespace NeonSyndicate
{
    /// <summary>Engine-free boundaries between automatic encounters, player consent and protected starting areas.</summary>
    public static class AmbientEncounterRules
    {
        public static void ResetStatics() { }

        public static bool MaySpawn(bool playerProtected, bool placementProtected, bool explicitlyRequested)
        {
            return explicitlyRequested || (!playerProtected && !placementProtected);
        }

        /// <summary>Distance-based engagement is deliberately absent: only a real manual choice preserves an automatic activity.</summary>
        public static bool RetireForSafety(bool playerProtected, bool placementProtected, bool explicitlyRequested, bool done)
        {
            return !done && !explicitlyRequested && (playerProtected || placementProtected);
        }

        public static bool OfferGuardChallenge(bool protectedArea, bool hasKey, bool missionActive, bool crewSpawned, bool crewDown, bool chestOpen)
        {
            return protectedArea && !hasKey && !missionActive && !crewSpawned && !crewDown && !chestOpen;
        }
    }
}
