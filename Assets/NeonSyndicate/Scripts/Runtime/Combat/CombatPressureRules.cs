namespace NeonSyndicate
{
    /// <summary>Pure difficulty and shared ranged-admission values for the existing combat director.</summary>
    public static class CombatPressureRules
    {
        public static void ResetStatics() { }

        public static float IncomingDamage(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 0.35f : (difficulty == DifficultyLevel.Balanced ? 0.62f : 1f);
        }

        public static float GunSpread(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 2.2f : (difficulty == DifficultyLevel.Balanced ? 1.35f : 1f);
        }

        public static float ReactionSeconds(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 1.8f : (difficulty == DifficultyLevel.Balanced ? 0.95f : 0.6f);
        }

        public static float WindupScale(DifficultyLevel difficulty)
        {
            return difficulty == DifficultyLevel.Story ? 1.45f : (difficulty == DifficultyLevel.Balanced ? 1f : 0.85f);
        }

        /// <summary>NPCs and creatures share one ranged cap. Negative counts must not accidentally free a slot.</summary>
        public static bool CanAdmitRanged(int npcCount, int creatureCount, int cap)
        {
            return cap > 0 && npcCount >= 0 && creatureCount >= 0 && npcCount < cap && creatureCount < cap - npcCount;
        }
    }
}
