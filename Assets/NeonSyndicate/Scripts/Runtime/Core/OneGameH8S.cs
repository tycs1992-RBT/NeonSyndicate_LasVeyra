namespace NeonSyndicate
{
    /// <summary>
    /// One unified game (owner decision, 2026-10-23): every title button plays the newest, most complete world, Gothic
    /// Civic H5 (world revision 10). Older worlds stay on disk and readable: their saves are listed under "Older saves" in
    /// Load Game and reload into the world they were made in, unchanged. No save is moved, copied or converted, and no
    /// new world revision exists. Pure rules (no engine calls), shared by the title screen, the boot and the tests.
    /// </summary>
    public static class OneGameH8S
    {
        /// <summary>The world every title button plays (New Game, Start After the Prologue, Continue, Free Roam).</summary>
        public const CityRevision04O World = CityRevision04O.GothicCivicH5;

        /// <summary>The save bank of that world: bank 10, files lasveyra_civic_h5_save_N.json.</summary>
        public const int Bank = 10;

        /// <summary>True when the loaded scene shows another world, so a title button must reload into the unified one.</summary>
        public static bool NeedsReload(CityRevision04O loaded)
        {
            return loaded != World;
        }

        /// <summary>
        /// The next bank of the Load Game cycle: the current world first, then every older bank newest first
        /// (9, 8 ... 2, then 0, which the original city shares with the 04O crystal city), then back. Unknown banks
        /// return to the current world. Every bank 0 and 2 to 10 appears exactly once per cycle.
        /// </summary>
        public static int NextBank(int bank)
        {
            if (bank == Bank) return 9;
            if (bank >= 3 && bank <= 9) return bank - 1;
            if (bank == 2) return 0;
            return Bank;
        }

        /// <summary>True for a bank that holds older-world saves (anything but the unified world's bank).</summary>
        public static bool IsOlderBank(int bank)
        {
            return bank != Bank;
        }

        /// <summary>A short player-facing name for the world a bank belongs to.</summary>
        public static string WorldName(int bank)
        {
            switch (bank)
            {
                case 10: return "Gothic Civic (H5)";
                case 9: return "Gothic Heart (H3)";
                case 8: return "Ascension Art (H2)";
                case 7: return "Visual Ascension (H1)";
                case 6: return "Living Frontier (04O-G)";
                case 5: return "Crystal Realms Unbound (04O-E)";
                case 4: return "Crystal Realms (04O-D)";
                case 3: return "Fractured Wilds (04O-C)";
                case 2: return "Gothic Crystal Realm (04O-B)";
                case 0: return "Original city and 04O crystal city";
                default: return "Unknown world";
            }
        }

        /// <summary>The Load Game bank button: a clear way into older saves from the current world's list, and onward.</summary>
        public static string BankButton(int bank)
        {
            return bank == Bank ? "Show older saves" : "Older saves: " + WorldName(bank) + "  (next)";
        }

        /// <summary>The line under the slot list while an older bank is shown.</summary>
        public const string OlderHint = "Older saves load into the world they were made in, unchanged. Saving always uses the world you are playing.";
    }
}
