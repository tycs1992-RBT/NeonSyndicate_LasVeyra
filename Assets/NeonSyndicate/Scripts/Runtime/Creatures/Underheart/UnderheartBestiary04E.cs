using System;
using System.Text;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: what the BESTIARY page says about the nine Underheart species. They keep CreatureTier.Badlands in
    /// CreatureSpeciesDb (quest pay scaling and the creature reputation table read it, and a saved or shipped value is
    /// never changed), so the page asks here for their real home instead: the region label "The Underheart", the
    /// subregions where the player actually meets each one (the authored encounter rosters, boss and Choir
    /// reinforcements, contract grammars and resident slots; Bestiary04ETests cross-checks this table against that data),
    /// that day and night do not matter below the street, and, for the three residents, a kind label that says they are
    /// residents rather than "Swarm" or "Territorial". Engine-free.
    /// </summary>
    public static class UnderheartBestiary04E
    {
        public const string RegionName = "The Underheart";
        public const string WhenLabel = "Underground, day and night";

        /// <summary>True for the nine species appended for 04E (six hostile roles and three residents).</summary>
        public static bool IsUnderground(string speciesId)
        {
            return !string.IsNullOrEmpty(speciesId) && (Array.IndexOf(EnemyIds04E.Hostile, speciesId) >= 0 || IsResident(speciesId));
        }

        /// <summary>True for the cinder moth, the kiln crab and the ember hatchling.</summary>
        public static bool IsResident(string speciesId)
        {
            return !string.IsNullOrEmpty(speciesId) && Array.IndexOf(EnemyIds04E.Residents, speciesId) >= 0;
        }

        /// <summary>The Underheart regions (UnderheartIds.Regions order) where the player meets a species; empty for others.</summary>
        public static string[] Regions(string speciesId)
        {
            switch (speciesId)
            {
                case EnemyIds04E.EmberScavenger:
                    return new[] { UnderheartIds.EmberCanals, UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.AshBurrower:
                    return new[] { UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops };
                case EnemyIds04E.GlasswingAmbusher:
                    return new[] { UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.FurnaceCaster:
                    return new[] { UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.ExtractionSentinel:
                    return new[] { UnderheartIds.ServiceLevel, UnderheartIds.EmberCanals, UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.RepairConstruct:
                    return new[] { UnderheartIds.ServiceLevel, UnderheartIds.EmberCanals, UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.CinderMoth:
                    return new[] { UnderheartIds.ServiceLevel, UnderheartIds.LanternRefuge, UnderheartIds.EmberCanals, UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops, UnderheartIds.AshReliquary, UnderheartIds.FoundryCrown };
                case EnemyIds04E.KilnCrab:
                    return new[] { UnderheartIds.LanternRefuge, UnderheartIds.EmberCanals, UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops };
                case EnemyIds04E.EmberHatchling:
                    return new[] { UnderheartIds.ServiceLevel, UnderheartIds.EmberCanals, UnderheartIds.PyreGardens };
                default:
                    return new string[0];
            }
        }

        /// <summary>True when the species is met in that region (an Underheart region id).</summary>
        public static bool FoundIn(string speciesId, string regionId)
        {
            return !string.IsNullOrEmpty(regionId) && Array.IndexOf(Regions(speciesId), regionId) >= 0;
        }

        /// <summary>
        /// "Where" line for the entry: the regions in plain words ("Ember Canals, Pyre Gardens and Crucible Workshops"),
        /// all seven as "every region"; the hatchlings add the Pressure House nest where they are first met.
        /// </summary>
        public static string HabitatLine(string speciesId)
        {
            string[] r = Regions(speciesId);
            if (r.Length == 0) return "";
            string where;
            if (r.Length == UnderheartIds.Regions.Length) where = "Every region of the Underheart, from the service level to the Foundry Crown";
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < r.Length; i++)
                {
                    if (i > 0) sb.Append(i == r.Length - 1 ? " and " : ", ");
                    sb.Append(UnderheartIds.RegionTitle(r[i]));
                }
                where = sb.ToString();
            }
            if (speciesId == EnemyIds04E.EmberHatchling) where = "The Pressure House nest, then " + where;
            return where + ".";
        }

        /// <summary>The kind shown before the region: residents read as residents, hostile roles keep their temperament.</summary>
        public static string KindLabel(string speciesId, Temperament temperament)
        {
            switch (speciesId)
            {
                case EnemyIds04E.CinderMoth: return "Resident, harmless";
                case EnemyIds04E.EmberHatchling: return "Resident, harmless";
                case EnemyIds04E.KilnCrab: return "Resident, guards its nest";
                default: return temperament.ToString();
            }
        }
    }
}
