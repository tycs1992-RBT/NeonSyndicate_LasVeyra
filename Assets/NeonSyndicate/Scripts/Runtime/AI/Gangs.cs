using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The eight Mythic Las Veyra gangs and the districts they control.</summary>
    public sealed class GangDef
    {
        public int Id;
        public string Name;
        public District Turf;
        public int ColorRow;
        public int ColorCol;
        public int AccentRow;
        public int AccentCol;
        public WeaponId[] Arsenal;
        public string Motto;
        public Color MapColor;
    }

    public static class Gangs
    {
        public static readonly GangDef[] All =
        {
            new GangDef { Id = 0, Name = "Ember Court", Turf = District.AshUnderpass, ColorRow = 4, ColorCol = 5, AccentRow = 3, AccentCol = 0, Arsenal = new[] { WeaponId.VectorSidearm, WeaponId.MicroSpray, WeaponId.SluggerBat, WeaponId.FireBottle }, Motto = "Everything burns brighter in the Court.", MapColor = new Color(1f, 0.35f, 0.15f) },
            new GangDef { Id = 1, Name = "Quartz Kings", Turf = District.QuartzBadlands, ColorRow = 3, ColorCol = 12, AccentRow = 3, AccentCol = 8, Arsenal = new[] { WeaponId.BrassHammer, WeaponId.GlassShot, WeaponId.ArcCarbine }, Motto = "The desert keeps what it takes.", MapColor = new Color(0.65f, 0.25f, 0.85f) },
            new GangDef { Id = 2, Name = "Mirage Lotus", Turf = District.MirageStrip, ColorRow = 4, ColorCol = 0, AccentRow = 3, AccentCol = 10, Arsenal = new[] { WeaponId.VectorSidearm, WeaponId.CoilBurst, WeaponId.Switchblade }, Motto = "Smile for the cameras. Then disappear.", MapColor = new Color(1f, 0.3f, 0.7f) },
            new GangDef { Id = 3, Name = "Brass Tower Union", Turf = District.BrassProjects, ColorRow = 3, ColorCol = 4, AccentRow = 3, AccentCol = 8, Arsenal = new[] { WeaponId.VectorSidearm, WeaponId.GlassShot, WeaponId.TireIron }, Motto = "Union dues are paid in full.", MapColor = new Color(0.95f, 0.8f, 0.2f) },
            new GangDef { Id = 4, Name = "Static Saints", Turf = District.GraveFrequency, ColorRow = 3, ColorCol = 10, AccentRow = 3, AccentCol = 0, Arsenal = new[] { WeaponId.BrassHammer, WeaponId.CoilBurst, WeaponId.SluggerBat }, Motto = "We hear everything on the dead air.", MapColor = new Color(0.9f, 0.9f, 0.95f) },
            new GangDef { Id = 5, Name = "Canal Choir", Turf = District.SunkenRow, ColorRow = 4, ColorCol = 1, AccentRow = 1, AccentCol = 13, Arsenal = new[] { WeaponId.VectorSidearm, WeaponId.MicroSpray, WeaponId.Switchblade }, Motto = "Sing low, sink slow.", MapColor = new Color(0.1f, 0.85f, 0.9f) },
            new GangDef { Id = 6, Name = "Glass Jury", Turf = District.GlassHeights, ColorRow = 3, ColorCol = 15, AccentRow = 1, AccentCol = 14, Arsenal = new[] { WeaponId.VectorSidearm, WeaponId.ArcCarbine, WeaponId.CoilBurst }, Motto = "Verdicts are final.", MapColor = new Color(0.3f, 0.55f, 1f) },
            new GangDef { Id = 7, Name = "Roof Oni", Turf = District.SkyrailOrbit, ColorRow = 4, ColorCol = 3, AccentRow = 3, AccentCol = 0, Arsenal = new[] { WeaponId.Switchblade, WeaponId.CoilBurst, WeaponId.VectorSidearm }, Motto = "Watch the rooftops. We already are.", MapColor = new Color(0.3f, 1f, 0.4f) },
        };

        /// <summary>Hostility of each gang toward the player (0 calm, 1 shoot on sight). Raised by attacks and story.</summary>
        public static readonly float[] Heat = new float[8];

        /// <summary>Story-driven minimum hostility (for example the Ember Court after Ashline Treaty). Heat never cools below it.</summary>
        public static readonly float[] MinHeat = new float[8];

        /// <summary>Faction standing (FactionSystem): multiplier on heat from offences, 1 normally, lower for friends.</summary>
        public static readonly float[] HeatScale = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        /// <summary>Faction standing (FactionSystem): multiplier on how fast heat cools, 1 normally, higher for friends.</summary>
        public static readonly float[] CoolScale = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        public static void ResetStatics()
        {
            for (int i = 0; i < Heat.Length; i++) Heat[i] = 0f;
            for (int i = 0; i < MinHeat.Length; i++) MinHeat[i] = 0f;
            ResetTolerance();
        }

        /// <summary>Sets how a gang treats offences and how fast it cools (reputation ranks call this).</summary>
        public static void SetTolerance(int gang, float heatScale, float coolScale)
        {
            if (gang < 0 || gang >= HeatScale.Length) return;
            HeatScale[gang] = Mathf.Max(0f, heatScale);
            CoolScale[gang] = Mathf.Max(0f, coolScale);
        }

        public static void ResetTolerance()
        {
            for (int i = 0; i < HeatScale.Length; i++) HeatScale[i] = 1f;
            for (int i = 0; i < CoolScale.Length; i++) CoolScale[i] = 1f;
        }

        public static void SetFloor(int gang, float floor)
        {
            if (gang < 0 || gang >= MinHeat.Length) return;
            MinHeat[gang] = Mathf.Clamp01(floor);
            Heat[gang] = Mathf.Max(Heat[gang], MinHeat[gang]);
        }

        /// <summary>Re-applies the hostility the story has caused so far (used after loading a save).</summary>
        public static void ApplyStory(ICollection<string> passedMissions)
        {
            if (passedMissions == null) return;
            if (passedMissions.Contains("m05_ashline_treaty")) SetFloor(0, 0.85f);
            if (passedMissions.Contains("m09_crownless_mile")) SetFloor(2, 0.85f);
        }

        public static GangDef ForDistrict(District d)
        {
            foreach (GangDef g in All) if (g.Turf == d) return g;
            return null;
        }

        public static GangDef Get(int id)
        {
            return id >= 0 && id < All.Length ? All[id] : null;
        }

        public static Appearance Look(GangDef g, System.Random r)
        {
            Appearance a = Appearance.Random(r);
            a.ShirtRow = g.ColorRow;
            a.Shirt = g.ColorCol;
            if (r.NextDouble() < 0.6)
            {
                a.Jacket = true;
                a.JacketRow = g.AccentRow;
                a.JacketColor = g.AccentCol;
                a.LongSleeves = true;
            }
            if (r.NextDouble() < 0.4)
            {
                a.Headwear = r.NextDouble() < 0.5 ? Headwear.Bandana : Headwear.Cap;
                a.HatRow = g.ColorRow;
                a.Hat = g.ColorCol;
            }
            a.Female = r.NextDouble() < 0.25;
            if (a.Female) a.Beard = false;
            a.Bulk = Mathf.Max(a.Bulk, 1f);
            return a;
        }

        public static void AddHeat(int gang, float amount)
        {
            if (gang < 0 || gang >= Heat.Length) return;
            // Friendly syndicates shrug off part of every offence (reputation, see FactionSystem).
            if (amount > 0f && gang < HeatScale.Length) amount *= HeatScale[gang];
            Heat[gang] = Mathf.Clamp01(Heat[gang] + amount);
        }

        public static void Cool(float dt)
        {
            for (int i = 0; i < Heat.Length; i++) Heat[i] = Mathf.Max(MinHeat[i], Heat[i] - dt * 0.004f * (i < CoolScale.Length ? CoolScale[i] : 1f));
        }
    }
}
