namespace NeonSyndicate
{
    /// <summary>04D boss profiles for the Foreman, the Djinn and the Queen (owner: bosses). The Titan is unchanged.</summary>
    public static class BossProfiles04D
    {
        public static void ResetStatics() { }
    }

    /// <summary>One reset hub for every static the bosses pass adds (called from Game.ResetStatics).</summary>
    public static class BossProfileStatics
    {
        public static void ResetStatics()
        {
            BossProfiles04D.ResetStatics();
        }
    }
}
