namespace NeonSyndicate
{
    /// <summary>Engine-free response budgets. Counts include officers inside dispatched cars, so deployment cannot create extra capacity.</summary>
    public static class PoliceResponseRules
    {
        /// <summary>Stateless rules have no per-session data to retain.</summary>
        public static void ResetStatics() { }

        public static int Cars(DifficultyLevel difficulty, int stars)
        {
            if (stars <= 0) return 0;
            if (stars > 5) stars = 5;
            if (difficulty == DifficultyLevel.Story) return (stars + 1) / 2;
            if (difficulty == DifficultyLevel.Balanced) return stars;
            return stars + 1;
        }

        public static int Officers(DifficultyLevel difficulty, int stars)
        {
            if (stars <= 0) return 0;
            if (stars > 5) stars = 5;
            if (difficulty == DifficultyLevel.Story) return stars <= 3 ? stars + 1 : stars * 2 - 2;
            if (difficulty == DifficultyLevel.Balanced) return stars * 2 + 1;
            return stars * 3 + 2;
        }

        public static int AvailableOfficers(DifficultyLevel difficulty, int stars, int active)
        {
            int remaining = Officers(difficulty, stars) - (active < 0 ? 0 : active);
            return remaining > 0 ? remaining : 0;
        }

        /// <summary>Patrol recruitment consumes the same car and crew budget as a newly dispatched car.</summary>
        public static bool CanRecruit(DifficultyLevel difficulty, int stars, int cars, int activeOfficers, int crew)
        {
            return crew > 0 && cars < Cars(difficulty, stars)
                && crew <= AvailableOfficers(difficulty, stars, activeOfficers);
        }

        public static float DispatchSeconds(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 5f : (difficulty == DifficultyLevel.Balanced ? 3.5f : 2f);
        }

        public static float InitialDelay(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 5f : (difficulty == DifficultyLevel.Balanced ? 3f : 1.5f);
        }

        public static float HeatScale(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 0.65f : (difficulty == DifficultyLevel.Balanced ? 0.85f : 1f);
        }

        public static float EscapeScale(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 1.35f : (difficulty == DifficultyLevel.Balanced ? 1.15f : 1f);
        }

        public static bool MayCompleteCall(bool playing, bool suppressed, bool missionIgnoresCrimes)
        {
            return playing && !suppressed && !missionIgnoresCrimes;
        }
    }
}
