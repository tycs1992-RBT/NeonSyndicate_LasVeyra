namespace NeonSyndicate
{
    /// <summary>Pure limits shared by local enforcement and the existing world event director.</summary>
    public static class CityCombatRules04F
    {
        public const float IncidentSeconds = 100f;
        public const float ContainmentSeconds = 75f;
        public const float QuietSeconds = 40f;
        public const float EventSeconds = 95f;
        public const float SetupSeconds = 8f;
        public const float EventCooldown = 240f;
        public static void ResetStatics() { }

        public static int IncidentOfficers(DifficultyLevel difficulty, int stars)
        {
            int severity = stars < 1 ? 1 : stars > 5 ? 5 : stars;
            return difficulty == DifficultyLevel.Story ? 3 + severity : difficulty == DifficultyLevel.Balanced ? 4 + severity : 6 + severity;
        }

        public static int RemainingDispatch(int budget, int alreadyDispatched, int activeSpace)
        {
            int left = budget - alreadyDispatched;
            if (left < 0) return 0;
            return left < activeSpace ? left : activeSpace;
        }

        /// <summary>Story pursuits never inherit local containment or a cosmetic wanted reset.</summary>
        public static bool StoryOwnsPursuit(bool frozenWanted, bool missionPresent, bool missionStory, bool missionQuiet)
        {
            return frozenWanted || (missionPresent && missionStory && !missionQuiet);
        }

        public static bool LocalCanDispatch(bool story, bool contained, float incidentAge, int remaining)
        {
            return story || (!contained && incidentAge < IncidentSeconds && remaining > 0);
        }

        public static int FightersPerSide(bool lowGraphics) { return lowGraphics ? 2 : 3; }
        public static int ProjectileCap(bool lowGraphics) { return lowGraphics ? 12 : 24; }
        public static bool IsCityKind(string kind)
        {
            return kind == "CrystalDispute" || kind == "DrakeRescue" || kind == "CrystalCache";
        }

        /// <summary>Only a recorded explicit action can earn an encounter reward. Watching never counts.</summary>
        public static bool RescueReady(bool handlerAlive, bool physicallyReunited, bool routeOpened, bool hostileCrewDefeated)
        {
            return handlerAlive && physicallyReunited && (routeOpened || hostileCrewDefeated);
        }

        public static bool OpposedEventTarget(bool sameEvent, int attackerSide, int targetSide, bool targetPlayer, bool retaliating)
        {
            return targetPlayer ? retaliating : sameEvent && attackerSide != targetSide;
        }

        public static bool OrdinaryTarget(bool explicitTarget, bool protectedRole, bool sameTeam, bool rivalGang)
        {
            return explicitTarget && !protectedRole && (!sameTeam || rivalGang);
        }

        public static bool Earned(bool accepted, bool objectiveComplete, bool alreadyDone)
        {
            return accepted && objectiveComplete && !alreadyDone;
        }
    }
}
