using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: how two campaign decisions change the Underheart's residents from then on. Engine-free (tested offline) and
    /// read every second by the ecology from the stored choices, so a loaded save shows the same world without a save
    /// field of its own.
    /// <para>matron_fate (The Cinder Matron). contain: she lives in the old cooling pen in her lair; the young hatchlings
    /// that wandered the gardens' third study point go back to her nest (a sheltered clutch at Under:gardens.matron_arena)
    /// and the habitat crab settles (calm: it no longer warns). relocate: she goes wild in the deep; her nest stays empty,
    /// the habitat crab keeps guarding its nest, the wandering hatchlings follow her down and nest by the warm stones of
    /// the Ash Reliquary rest room (Under:reliquary.rest, a sheltered clutch no encounter ever claims), and the reliquary
    /// moths thin out around her range.</para>
    /// <para>regent_end (The Kiln Regent). contain: the custodian keeps the Crown warm and steady; cinder moths return to
    /// the Heart Furnace balcony (Under:crown.vista) and young hatchlings settle in the staging refuge. shutdown: the Crown
    /// runs cold and quiet; the staging refuge's moths leave and each Ash Reliquary hall keeps a single moth.</para>
    /// No region ever holds more than ResidentRules04E.MaxPerRegion: authored moths give way first, a choice slot never.
    /// </summary>
    public static class ChoiceEcology04E
    {
        public const string MatronKey = "matron_fate";
        public const string RegentKey = "regent_end";
        public const string Contain = "contain";
        public const string Relocate = "relocate";
        public const string Shutdown = "shutdown";

        /// <summary>The gardens study point whose wandering hatchlings go back to the Matron (contain) or follow her down (relocate).</summary>
        public const string WanderingClutch = "gardens.study_c";
        /// <summary>The Matron's nest in her lair (contain: the clutch comes home).</summary>
        public const string MatronNest = "gardens.matron_arena";
        /// <summary>The gardens crab that settles once the Matron is penned.</summary>
        public const string HabitatCrab = "gardens.habitat";
        /// <summary>Her brood's new home in the deep (relocate): the reliquary rest room, where no encounter is ever fought.</summary>
        public const string DeepBrood = "reliquary.rest";
        /// <summary>The Heart Furnace balcony (regent contain: the moths come back).</summary>
        public const string FurnaceBalcony = "crown.vista";
        /// <summary>The Crown's staging refuge (regent contain: young hatchlings; shutdown: its moths leave).</summary>
        public const string CrownStaging = "crown.staging";

        /// <summary>A stored choice value as this table reads it ("" when not made or not a value of that key).</summary>
        public static string Normalize(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (key == MatronKey) return value == Contain || value == Relocate ? value : "";
            if (key == RegentKey) return value == Contain || value == Shutdown ? value : "";
            return "";
        }

        /// <summary>
        /// The resident slots for these decisions: the authored ResidentRules04E.Slots with the choices applied (choice
        /// slots first), every region trimmed to MaxPerRegion by taking authored moths away. into is cleared first.
        /// </summary>
        public static void EffectiveSlots(string matronFate, string regentEnd, List<ResidentSlot04E> into)
        {
            into.Clear();
            string m = Normalize(MatronKey, matronFate);
            string r = Normalize(RegentKey, regentEnd);
            string mSrc = m.Length > 0 ? MatronKey + "=" + m : "";
            string rSrc = r.Length > 0 ? RegentKey + "=" + r : "";
            // The slots a choice adds come first, so they are never the ones a full region drops.
            if (m == Contain) into.Add(new ResidentSlot04E(UnderheartIds.PyreGardens, MatronNest, EnemyIds04E.EmberHatchling, 2, ResidentTemper04E.Calm, true, mSrc));
            if (m == Relocate) into.Add(new ResidentSlot04E(UnderheartIds.AshReliquary, DeepBrood, EnemyIds04E.EmberHatchling, 2, ResidentTemper04E.Calm, true, mSrc));
            if (r == Contain)
            {
                into.Add(new ResidentSlot04E(UnderheartIds.FoundryCrown, FurnaceBalcony, EnemyIds04E.CinderMoth, 2, ResidentTemper04E.Calm, false, rSrc));
                into.Add(new ResidentSlot04E(UnderheartIds.FoundryCrown, CrownStaging, EnemyIds04E.EmberHatchling, 2, ResidentTemper04E.Calm, false, rSrc));
            }
            foreach (ResidentSlot04E authored in ResidentRules04E.Slots)
            {
                ResidentSlot04E s = authored;
                if (m.Length > 0 && s.Anchor == WanderingClutch && s.Species == EnemyIds04E.EmberHatchling) continue;
                if (m == Contain && s.Anchor == HabitatCrab && s.Species == EnemyIds04E.KilnCrab)
                {
                    s.Temper = ResidentTemper04E.Calm;
                    s.Source = mSrc;
                }
                if (r == Shutdown && s.Species == EnemyIds04E.CinderMoth)
                {
                    if (s.Anchor == CrownStaging) continue;
                    if (s.Region == UnderheartIds.AshReliquary)
                    {
                        s.Count = 1;
                        s.Source = rSrc;
                    }
                }
                into.Add(s);
            }
            for (int i = 0; i < UnderheartIds.Regions.Length; i++) Trim(UnderheartIds.Regions[i], into);
        }

        /// <summary>Total residents a slot list places in a region.</summary>
        public static int RegionTotal(string region, IList<ResidentSlot04E> slots)
        {
            int n = 0;
            for (int i = 0; i < slots.Count; i++) if (slots[i].Region == region) n += slots[i].Count;
            return n;
        }

        /// <summary>Takes authored moths away (the last slot first) until the region fits; drops slots left empty.</summary>
        private static void Trim(string region, List<ResidentSlot04E> slots)
        {
            int over = RegionTotal(region, slots) - ResidentRules04E.MaxPerRegion;
            for (int i = slots.Count - 1; i >= 0 && over > 0; i--)
            {
                ResidentSlot04E s = slots[i];
                if (s.Region != region || s.FromChoice || s.Species != EnemyIds04E.CinderMoth) continue;
                int take = s.Count < over ? s.Count : over;
                s.Count -= take;
                over -= take;
                if (s.Count <= 0) slots.RemoveAt(i);
                else slots[i] = s;
            }
            for (int i = slots.Count - 1; i >= 0; i--) if (slots[i].Count <= 0) slots.RemoveAt(i);
        }

        /// <summary>One line for the debug page and the docs: what the decisions changed ("" when neither is made).</summary>
        public static string Describe(string matronFate, string regentEnd)
        {
            string m = Normalize(MatronKey, matronFate);
            string r = Normalize(RegentKey, regentEnd);
            string line = "";
            if (m == Contain) line += "Matron penned: the gardens clutch is back at her nest, the habitat crab is calm. ";
            if (m == Relocate) line += "Matron wild: her nest is empty, her brood sleeps in the reliquary rest room, the habitat crab still guards. ";
            if (r == Contain) line += "Crown kept warm: moths on the furnace balcony, young in the staging refuge. ";
            if (r == Shutdown) line += "Crown shut down: the staging moths left, one moth per reliquary hall. ";
            return line.Trim();
        }
    }
}
