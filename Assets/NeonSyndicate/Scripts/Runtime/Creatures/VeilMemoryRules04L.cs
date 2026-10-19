namespace NeonSyndicate
{
    public static class VeilMemoryRules04L
    {
        public const string Quest = "uh05_04l_01_veil_memory";
        public const string Arena = "Under:workshops.store_room";
        public const string Victory = "uh05_04l_witness_defeated";
        public static bool Authorized(string quest, string arena) { return quest == Quest && arena == Arena; }
        public static bool Owns(string owner) { return owner == "quest:" + Quest; }
    }
}
