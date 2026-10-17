using System;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: what the cast says once a campaign decision is made (engine-free, read from the stored choices, so a loaded
    /// save hears the same lines). Ilse Varro at the Lantern Refuge greets you by the regent_end you chose after the Kiln
    /// Regent, and Bram Achterberg in Pyre Gardens by the matron_fate you chose after the Cinder Matron. Everyone else, and
    /// these two before the decision, keep the greeting from CastRoster04E.
    /// </summary>
    public static class OutcomeLines04E
    {
        public const string RefugeContain = "The Crown hums steady below us. Your custodian keeps the heat; we keep the lamps lit.";
        public const string RefugeShutdown = "Cold down there now. My crews turn the valves by hand, and nothing gets fed to anything.";
        public const string GardensContain = "She ate from the bucket today and left my arm alone. Her little ones sleep by the pen.";
        public const string GardensRelocate = "No word from the deep vents. With her kind, that means she is fine. Her nest sits empty.";

        /// <summary>The greeting a cast member says on a later approach (fallback when no decision changes it).</summary>
        public static string Greeting(string castId, Func<string, string> choice, string fallback)
        {
            switch (castId)
            {
                case CastIds04E.RefugeKeeper:
                    switch (Read(choice, ChoiceEcology04E.RegentKey))
                    {
                        case ChoiceEcology04E.Contain: return RefugeContain;
                        case ChoiceEcology04E.Shutdown: return RefugeShutdown;
                    }
                    break;
                case CastIds04E.GardensKeeper:
                    switch (Read(choice, ChoiceEcology04E.MatronKey))
                    {
                        case ChoiceEcology04E.Contain: return GardensContain;
                        case ChoiceEcology04E.Relocate: return GardensRelocate;
                    }
                    break;
            }
            return fallback ?? "";
        }

        private static string Read(Func<string, string> choice, string key)
        {
            if (choice == null) return "";
            string v = choice(key);
            return ChoiceEcology04E.Normalize(key, v);
        }
    }
}
