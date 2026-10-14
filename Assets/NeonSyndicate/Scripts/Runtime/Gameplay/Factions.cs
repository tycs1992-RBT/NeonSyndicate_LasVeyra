using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum FactionKind
    {
        Syndicate,
        Guild,
        Corporate,
        Cult
    }

    /// <summary>A faction with a reputation track from -100 (hunted) to +100 (family).</summary>
    public sealed class FactionDef
    {
        public string Id;
        public string Name;
        public FactionKind Kind;
        /// <summary>For syndicates: the matching street gang (Gangs id); -1 otherwise.</summary>
        public int GangId = -1;
        public District Home;
        public string Rival;
        /// <summary>Story contact who speaks for the faction (MissionManager.PoiPos name), if any.</summary>
        public string Contact = "";
        public string Offers = "";
        public string Blurb = "";
        public Color Color = Color.white;
    }

    /// <summary>The factions of Las Veyra. Ids are stable (saves, quests and events refer to them).</summary>
    public static class FactionDb
    {
        public static readonly FactionDef[] All =
        {
            new FactionDef { Id = "ember_court", Name = "Ember Court", Kind = FactionKind.Syndicate, GangId = 0, Home = District.AshUnderpass, Rival = "brass_union", Offers = "Robbery and smuggling contracts", Blurb = "Fire-crowned crews of the Ash Underpass.", Color = new Color(1f, 0.35f, 0.15f) },
            new FactionDef { Id = "quartz_kings", Name = "Quartz Kings", Kind = FactionKind.Syndicate, GangId = 1, Home = District.QuartzBadlands, Rival = "glass_jury", Offers = "Desert runs and chop work", Blurb = "Bikers and miners who own the badlands roads.", Color = new Color(0.95f, 0.75f, 0.3f) },
            new FactionDef { Id = "mirage_lotus", Name = "Mirage Lotus", Kind = FactionKind.Syndicate, GangId = 2, Home = District.MirageStrip, Rival = "solane_holdings", Offers = "Casino jobs and the black market", Blurb = "The velvet syndicate behind the Strip's lights.", Color = new Color(1f, 0.3f, 0.7f) },
            new FactionDef { Id = "brass_union", Name = "Brass Tower Union", Kind = FactionKind.Syndicate, GangId = 3, Home = District.BrassProjects, Rival = "ember_court", Offers = "Protection rackets and fight pits", Blurb = "Tower blocks that vote as one and fight as one.", Color = new Color(0.9f, 0.7f, 0.35f) },
            new FactionDef { Id = "static_saints", Name = "Static Saints", Kind = FactionKind.Syndicate, GangId = 4, Home = District.GraveFrequency, Rival = "hollow_cult", Offers = "Signal theft and pirate radio", Blurb = "Hackers and preachers of the dead frequencies.", Color = new Color(0.55f, 0.6f, 1f) },
            new FactionDef { Id = "canal_choir", Name = "Canal Choir", Kind = FactionKind.Syndicate, GangId = 5, Home = District.SunkenRow, Rival = "roof_oni", Offers = "Canal smuggling and boat jobs", Blurb = "They sing on the water so you know they are coming.", Color = new Color(0.2f, 0.85f, 0.8f) },
            new FactionDef { Id = "glass_jury", Name = "Glass Jury", Kind = FactionKind.Syndicate, GangId = 6, Home = District.GlassHeights, Rival = "quartz_kings", Offers = "Bribes, records and lawyers", Blurb = "Lawyers and fixers who run the courts like a racket.", Color = new Color(0.8f, 0.9f, 1f) },
            new FactionDef { Id = "roof_oni", Name = "Roof Oni", Kind = FactionKind.Syndicate, GangId = 7, Home = District.SkyrailOrbit, Rival = "canal_choir", Offers = "Air races and rooftop runs", Blurb = "Masked racers who live on the skyrail roofs.", Color = new Color(1f, 0.25f, 0.3f) },
            new FactionDef { Id = "frequency_order", Name = "Frequency Order", Kind = FactionKind.Guild, Home = District.GraveFrequency, Rival = "hollow_cult", Contact = "Iris Moon", Offers = "Third Eye training, Veil rifts, Inner World lore", Blurb = "Mystics who listen to the Inner World through the static.", Color = new Color(0.6f, 0.4f, 1f) },
            new FactionDef { Id = "menders", Name = "Menders", Kind = FactionKind.Guild, Home = District.SunkenRow, Contact = "Mara Solane", Offers = "Healing, revival and Mercy powers", Blurb = "Healers who treat anyone who walks in, no questions.", Color = new Color(0.3f, 1f, 0.6f) },
            new FactionDef { Id = "forgewrights", Name = "Forgewrights", Kind = FactionKind.Guild, Home = District.OldSilver, Contact = "Dante Rook", Offers = "Crafting, weapon parts and mythic forging", Blurb = "Smiths who can forge anything from what a creature leaves behind.", Color = new Color(1f, 0.6f, 0.2f) },
            new FactionDef { Id = "frost_wardens", Name = "Frost Wardens", Kind = FactionKind.Guild, Home = District.Frostline, Rival = "hollow_cult", Contact = "Frost Wardens Outpost", Offers = "Creature hunts and Frostline contracts", Blurb = "Hunters who keep watch over the Frostline and what sleeps in it.", Color = new Color(0.55f, 0.85f, 1f) },
            new FactionDef { Id = "solane_holdings", Name = "Solane Holdings", Kind = FactionKind.Corporate, Home = District.GlassHeights, Rival = "mirage_lotus", Contact = "Ion Solane", Offers = "Corporate jobs and high-end gear", Blurb = "The dynasty that built the Crystal Manor, and wants it back.", Color = new Color(0.95f, 0.95f, 0.8f) },
            new FactionDef { Id = "hollow_cult", Name = "Hollow Cult", Kind = FactionKind.Cult, Home = District.GraveFrequency, Rival = "frequency_order", Offers = "Power, at a price", Blurb = "Worshippers of the Inner World who open doors that should stay shut.", Color = new Color(0.6f, 0.1f, 0.4f) }
        };

        private static Dictionary<string, FactionDef> _byId;

        /// <summary>Drops the lookup cache (rebuilt on the next Get), for Enter Play Mode without domain reload.</summary>
        public static void ResetStatics()
        {
            _byId = null;
        }

        public static FactionDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, FactionDef>();
                foreach (FactionDef f in All) _byId[f.Id] = f;
            }
            FactionDef d;
            return _byId.TryGetValue(id, out d) ? d : null;
        }

        /// <summary>The syndicate that matches a street gang, or null.</summary>
        public static FactionDef ForGang(int gangId)
        {
            foreach (FactionDef f in All) if (f.GangId == gangId && gangId >= 0) return f;
            return null;
        }
    }
}
