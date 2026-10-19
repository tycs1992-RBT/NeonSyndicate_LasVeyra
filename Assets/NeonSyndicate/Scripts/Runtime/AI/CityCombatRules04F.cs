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

        /// <summary>04I: one existing crew leader may become a recognizably crystal-armed fighter.
        /// These rules do not change crew size, spawn times, faction heat or story ownership.</summary>
        public static bool CrystalCrewLeader04I(int memberOrdinal, float roll)
        {
            return memberOrdinal == 0 && roll >= 0f && roll < 0.7f;
        }

        /// <summary>Different district crews favor different crystal combat silhouettes.</summary>
        public static CrystalNpcRole04F CrewRole04I(int gangId)
        {
            int slot = ((gangId % 4) + 4) % 4;
            return slot == 0 ? CrystalNpcRole04F.BladeWarden :
                slot == 1 ? CrystalNpcRole04F.FocusWeaver :
                slot == 2 ? CrystalNpcRole04F.RiftDuelist : CrystalNpcRole04F.WardSentinel;
        }

        /// <summary>Visible stationed enforcement carries crystal arms even before a wanted incident.</summary>
        public static CrystalNpcRole04F StationRole04I(int patrolOrdinal)
        {
            return (patrolOrdinal & 1) == 0 ? CrystalNpcRole04F.BladeWarden : CrystalNpcRole04F.FocusWeaver;
        }

        public static int FightersPerSide(bool lowGraphics) { return lowGraphics ? 2 : 3; }
        public static int ProjectileCap(bool lowGraphics) { return lowGraphics ? 12 : 24; }
        public static bool IsCityKind(string kind)
        {
            return kind == "CrystalMuster" || kind == "CrystalDispute" || kind == "DrakeRescue" || kind == "CrystalCache";
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

        /// <summary>A target that stopped fighting must leave every event fighter's focus and threat list immediately.</summary>
        public static bool EligibleEventRival(bool sameEvent, bool oppositeSide, bool alive, bool finished, bool stoodDown, bool held)
        {
            return sameEvent && oppositeSide && alive && !finished && !stoodDown && !held;
        }

        public static bool OrdinaryTarget(bool explicitTarget, bool protectedRole, bool sameTeam, bool rivalGang)
        {
            return explicitTarget && !protectedRole && (!sameTeam || rivalGang);
        }

        /// <summary>Keep a living rival in focus until another is substantially closer. Prevents rapid target flipping while duelists exchange blows.</summary>
        public static bool RetainEventOpponent(bool sameEvent, bool alive, float focusSqrDistance, float closestSqrDistance)
        {
            if (!sameEvent || !alive || float.IsNaN(focusSqrDistance) || float.IsNaN(closestSqrDistance)) return false;
            if (focusSqrDistance < 0f || closestSqrDistance < 0f || float.IsInfinity(focusSqrDistance) || float.IsInfinity(closestSqrDistance)) return false;
            return focusSqrDistance <= 900f && focusSqrDistance <= closestSqrDistance * 1.8f + 4f;
        }

        public static bool Earned(bool accepted, bool objectiveComplete, bool alreadyDone)
        {
            return accepted && objectiveComplete && !alreadyDone;
        }
    }
}
