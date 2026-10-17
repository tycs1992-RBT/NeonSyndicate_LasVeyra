using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NeonSyndicate
{
    /// <summary>
    /// One saved loadout: which owned items sit where, by stable inventory uid (never an object reference or an instance
    /// id), plus the gadgets, the ward aspect and which loot slot was in hand. Applying it only moves items the player
    /// still owns; nothing is ever created from a loadout.
    /// </summary>
    public sealed class Loadout04E
    {
        public readonly int[] Gear = new int[GearSystem.SlotCount];
        public readonly int[] Loot = new int[Weapons.LootSlots];
        public readonly int[] Gadgets = { -1, -1 };
        public string Aspect = "";
        /// <summary>The loot slot held in hand (-1 when the hand held another weapon).</summary>
        public int InHand = -1;
        public string Preset = "";
        public string Name = "";

        public Loadout04E Clone()
        {
            Loadout04E c = new Loadout04E { Aspect = Aspect, InHand = InHand, Preset = Preset, Name = Name };
            Array.Copy(Gear, c.Gear, Gear.Length);
            Array.Copy(Loot, c.Loot, Loot.Length);
            Array.Copy(Gadgets, c.Gadgets, Gadgets.Length);
            return c;
        }

        /// <summary>True when nothing is recorded at all.</summary>
        public bool IsEmpty
        {
            get
            {
                foreach (int u in Gear) if (u > 0) return false;
                foreach (int u in Loot) if (u > 0) return false;
                foreach (int g in Gadgets) if (g >= 0) return false;
                return string.IsNullOrEmpty(Aspect);
            }
        }
    }

    /// <summary>
    /// Flattens a loadout into one save string (BuildSave.Loadouts, BuildSave.PreviousLoadout) and reads it back,
    /// tolerating damage: unknown keys are ignored, bad numbers become empty, a garbage string reads as null.
    /// Format: "v1;g=uid,..;l=uid,..;d=id,id;a=aspect;h=slot;p=preset;n=name". Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class LoadoutCodec04E
    {
        public const string Prefix = "v1;";

        public static string Encode(Loadout04E l)
        {
            if (l == null) return "";
            StringBuilder sb = new StringBuilder(Prefix);
            sb.Append("g=").Append(Join(l.Gear)).Append(';');
            sb.Append("l=").Append(Join(l.Loot)).Append(';');
            sb.Append("d=").Append(Join(l.Gadgets)).Append(';');
            sb.Append("a=").Append(Clean(l.Aspect)).Append(';');
            sb.Append("h=").Append(l.InHand.ToString(CultureInfo.InvariantCulture)).Append(';');
            sb.Append("p=").Append(Clean(l.Preset)).Append(';');
            sb.Append("n=").Append(Clean(l.Name));
            return sb.ToString();
        }

        public static Loadout04E Decode(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.StartsWith(Prefix, StringComparison.Ordinal)) return null;
            Loadout04E l = new Loadout04E();
            string[] parts = s.Substring(Prefix.Length).Split(';');
            foreach (string part in parts)
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string key = part.Substring(0, eq);
                string val = part.Substring(eq + 1);
                switch (key)
                {
                    case "g": Fill(val, l.Gear, 0, true); break;
                    case "l": Fill(val, l.Loot, 0, true); break;
                    case "d": Fill(val, l.Gadgets, -1, false); break;
                    case "a": l.Aspect = WardRules04E.IsAspect(val) ? val : ""; break;
                    case "h":
                        int h;
                        l.InHand = int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out h) && h >= 0 && h < Weapons.LootSlots ? h : -1;
                        break;
                    case "p": l.Preset = Array.IndexOf(BuildSystem.Presets, val) >= 0 ? val : ""; break;
                    case "n": l.Name = val; break;
                }
            }
            for (int i = 0; i < l.Gadgets.Length; i++) if (l.Gadgets[i] >= GadgetRules.GadgetCount) l.Gadgets[i] = -1;
            return l;
        }

        private static string Join(int[] a)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(a[i].ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static void Fill(string val, int[] into, int empty, bool positiveOnly)
        {
            string[] xs = val.Split(',');
            for (int i = 0; i < into.Length; i++)
            {
                int v;
                if (i < xs.Length && int.TryParse(xs[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && (positiveOnly ? v > 0 : v >= 0)) into[i] = v;
                else into[i] = empty;
            }
        }

        /// <summary>Separators cannot appear inside a value.</summary>
        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace(";", " ").Replace("=", " ").Replace(",", " ");
        }

        /// <summary>The items a loadout names that are no longer owned (shown before applying it).</summary>
        public static int MissingCount(Loadout04E l, ICollection<int> ownedGear, ICollection<int> ownedLoot)
        {
            if (l == null) return 0;
            int n = 0;
            foreach (int u in l.Gear) if (u > 0 && (ownedGear == null || !ownedGear.Contains(u))) n++;
            foreach (int u in l.Loot) if (u > 0 && (ownedLoot == null || !ownedLoot.Contains(u))) n++;
            return n;
        }
    }
}
