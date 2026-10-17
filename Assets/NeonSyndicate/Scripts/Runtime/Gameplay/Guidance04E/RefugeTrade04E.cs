using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One bundle the Lantern Refuge trade stall sells: a count of one material for cash.</summary>
    public struct RefugeOffer04E
    {
        public string Material;
        public int Count;
        public int Price;
        /// <summary>Why the refuge has it, in a few words.</summary>
        public string Note;
    }

    /// <summary>
    /// 04E: the Lantern Refuge trade stall that opens after the Kiln Regent, run by Ilse Varro (talk to her). What it
    /// stocks follows regent_end: contain keeps the Crown warm and steady, so the old kilns grow ember crystal again and
    /// the gardens seed (ember crystal, a Pyre seed-core); shutdown runs the Crown cold and manual, so salt condenses in
    /// every duct and the crews strip spare cogs from the dead regulators (coolant salt, regulator cogs). Engine-free and
    /// tested offline. Every bundle costs more per piece than a Forgewright pays for that material (MaterialDef.Value),
    /// so buying here and selling there never makes money, and nothing sold is a boss material.
    /// </summary>
    public static class RefugeTrade04E
    {
        public const string StallName = "Lantern Refuge stall";

        /// <summary>The bundles on sale for a regent_end value (empty before the decision: the stall is not open yet).</summary>
        public static void Offers(string regentEnd, List<RefugeOffer04E> into)
        {
            into.Clear();
            switch (ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, regentEnd))
            {
                case ChoiceEcology04E.Contain:
                    into.Add(Offer(MaterialIds04E.EmberCrystal, 3, 1.5f, "grown in the warm kilns"));
                    into.Add(Offer(MaterialIds04E.SeedCore, 1, 1.6f, "the gardens seed again"));
                    break;
                case ChoiceEcology04E.Shutdown:
                    into.Add(Offer(MaterialIds04E.CoolantSalt, 5, 1.25f, "condensed in the cold ducts"));
                    into.Add(Offer(MaterialIds04E.RegulatorCog, 2, 1.5f, "stripped from the dead regulators"));
                    break;
            }
        }

        /// <summary>True once the stall trades (the regent decision is made).</summary>
        public static bool Open(string regentEnd)
        {
            return ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, regentEnd).Length > 0;
        }

        /// <summary>The cash price of count pieces at markup over the Forgewright value, rounded up to the next $10.</summary>
        public static int Price(string material, int count, float markup)
        {
            MaterialDef d = MaterialDb.Get(material);
            int value = d != null ? d.Value : 50;
            float raw = Mathf.Max(1, count) * value * Mathf.Max(1.05f, markup);
            return Mathf.CeilToInt(raw / 10f) * 10;
        }

        /// <summary>The stall card's title for a decision.</summary>
        public static string Title(string regentEnd)
        {
            return ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, regentEnd) == ChoiceEcology04E.Shutdown ? "COLD-RUN STOCK" : "WARM-GROWN STOCK";
        }

        /// <summary>Ilse's line on the stall card.</summary>
        public static string Intro(string regentEnd)
        {
            return ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, regentEnd) == ChoiceEcology04E.Shutdown
                ? "Ilse Varro: \"With the Crown run by hand, the cold leaves salt in every duct and the crews keep finding spare cogs. Fair prices, refuge rates.\""
                : "Ilse Varro: \"Your custodian keeps the heat steady, so the old kilns grow crystal again and the gardens seed. Fair prices, refuge rates.\"";
        }

        /// <summary>The button label for a bundle ("BUY 3 EMBER CRYSTAL ($320)").</summary>
        public static string Label(RefugeOffer04E o)
        {
            return "BUY " + o.Count + " " + MaterialDb.NameOf(o.Material).ToUpperInvariant() + " (" + MathUtil.Money(o.Price) + ")";
        }

        private static RefugeOffer04E Offer(string material, int count, float markup, string note)
        {
            return new RefugeOffer04E { Material = material, Count = count, Price = Price(material, count, markup), Note = note };
        }
    }
}
