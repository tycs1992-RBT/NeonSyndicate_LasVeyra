using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where a cast member's post is bound.</summary>
    public enum CastPostKind
    {
        /// <summary>Inside a 04C or 04E venue at its host station ("staff" anchor), keeping the venue's host slot.</summary>
        VenueStaff = 0,
        /// <summary>On the sidewalk beside a venue's front door, facing the street (Nessa at the Veyra Lofts).</summary>
        VenueDoorOutside = 1,
        /// <summary>At a named Underheart anchor (the refuge and gardens keepers).</summary>
        UnderAnchor = 2,
        /// <summary>Beside the door of a named city place (POI) outdoors.</summary>
        PoiDoor = 3
    }

    /// <summary>The headgear layer of a cast member's costume (on top of hair and the Appearance headwear).</summary>
    public enum CostumeHead { None = 0, Hood = 1, BrimHat = 2, Headset = 3, Goggles = 4, Circlet = 5 }

    /// <summary>The upper-body layer of a cast member's costume.</summary>
    public enum CostumeTorso { None = 0, Scarf = 1, Mantle = 2, Cloak = 3, Satchel = 4, Sash = 5, Apron = 6 }

    /// <summary>The waist layer of a cast member's costume.</summary>
    public enum CostumeWaist { None = 0, ToolBelt = 1, Pouches = 2 }

    /// <summary>The tool a cast member carries in one hand.</summary>
    public enum HeldTool { None = 0, Clipboard = 1, Tablet = 2, Wrench = 3, Lantern = 4, Ledger = 5, Staff = 6, Medkit = 7, Hammer = 8, DataSlate = 9 }

    /// <summary>The procedural costume layers that give a cast member a silhouette of their own.</summary>
    public struct CastCostume04E
    {
        public CostumeHead Head;
        public CostumeTorso Torso;
        public CostumeWaist Waist;
        public HeldTool Tool;
        /// <summary>The main costume colour (cloth layers) and the accent colour (trim, lenses, glow).</summary>
        public Color Main;
        public Color Accent;
    }

    /// <summary>One named campaign character: identity, post, story gate, presentation and lines.</summary>
    public sealed class CastDef04E
    {
        public string Id = "";
        public string Name = "";
        /// <summary>What they do, in a few words ("Veyra Lofts host").</summary>
        public string Role = "";
        public CastPostKind Post;
        /// <summary>The venue (04C or 04E id) of a venue post.</summary>
        public string Venue = "";
        /// <summary>The venue anchor or the Underheart anchor of the post.</summary>
        public string Anchor = "";
        /// <summary>A city place (POI name) used when the venue is unavailable, or the post itself for PoiDoor.</summary>
        public string Poi = "";
        /// <summary>Story missions passed before this person may appear in story mode (Free Roam always passes).</summary>
        public int StoryUnlock;
        /// <summary>An original story character: never shown while a story mission or other non-campaign mission runs,
        /// because those missions may stage the same person themselves.</summary>
        public bool StoryIdentity;
        /// <summary>Present in person only while one of their quests is offered or running (otherwise remote or absent).</summary>
        public bool OnDemand;
        /// <summary>Where they are when not present in person (for example Ion on the dispatch call screen).</summary>
        public string RemoteNote = "";
        /// <summary>A remote contact point (venue and anchor) the route can lead to when they are not present.</summary>
        public string RemoteVenue = "";
        public string RemoteAnchor = "";
        /// <summary>Said once, the first time the player comes close in this save.</summary>
        public string Intro = "";
        /// <summary>A short acknowledgement on later approaches (at most once per approach, with a long cooldown).</summary>
        public string Greeting = "";
        public CastCostume04E Costume;
    }

    /// <summary>
    /// The 04E cast (CastIds04E): every named character's post, story gate, recognizable look (Appearance plus
    /// procedural costume layers) and their two short lines. Pure data with no engine calls, shared by the runtime and
    /// the EditMode tests.
    /// </summary>
    public static class CastRoster04E
    {
        private static CastDef04E[] _all;

        public static void ResetStatics() { _all = null; }

        /// <summary>Every cast member in CastIds04E order.</summary>
        public static CastDef04E[] All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        public static CastDef04E Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CastDef04E[] all = All;
            for (int i = 0; i < all.Length; i++) if (all[i].Id == id) return all[i];
            return null;
        }

        /// <summary>"Cast:nessa" gives "nessa"; anything else gives "".</summary>
        public static string IdFromToken(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Cast:", StringComparison.Ordinal)) return "";
            return token.Substring(5);
        }

        public static string Token(string castId) { return "Cast:" + castId; }

        private static Color Rgb(int hex)
        {
            return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);
        }

        private static CastCostume04E Costume(CostumeHead head, CostumeTorso torso, CostumeWaist waist, HeldTool tool, int main, int accent)
        {
            return new CastCostume04E { Head = head, Torso = torso, Waist = waist, Tool = tool, Main = Rgb(main), Accent = Rgb(accent) };
        }

        private static CastDef04E[] Build()
        {
            return new[]
            {
                new CastDef04E
                {
                    Id = CastIds04E.Nessa, Name = CastIds04E.Name(CastIds04E.Nessa), Role = "Veyra Lofts host",
                    Post = CastPostKind.VenueDoorOutside, Venue = "lofts", Poi = "Veyra Lofts",
                    Intro = "Malik, over here. Something keeps knocking under the Lofts, and Cinder Dispatch across the avenue wants a steady pair of hands.",
                    Greeting = "Lofts are quiet. The street is not.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Scarf, CostumeWaist.None, HeldTool.Clipboard, 0x2f7f86, 0xf2c14e)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Dispatcher, Name = CastIds04E.Name(CastIds04E.Dispatcher), Role = "Cinder Dispatch",
                    Post = CastPostKind.VenueStaff, Venue = VenueIds04E.Dispatch, Anchor = "staff",
                    Intro = "Corin Pell, dispatch. If it hums, leaks or bites anywhere on this block, it crosses my desk first.",
                    Greeting = "Board is full. Pick a light.",
                    Costume = Costume(CostumeHead.Headset, CostumeTorso.None, CostumeWaist.Pouches, HeldTool.Tablet, 0x3b4252, 0xff8a3d)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Maintainer, Name = CastIds04E.Name(CastIds04E.Maintainer), Role = "Pressure House maintenance",
                    Post = CastPostKind.VenueStaff, Venue = VenueIds04E.Pressure, Anchor = "staff",
                    Intro = "Tamsin. I keep the Pressure House from cooking the block. Ion says you can hold a valve steady.",
                    Greeting = "Valves are holding. For now.",
                    Costume = Costume(CostumeHead.Goggles, CostumeTorso.None, CostumeWaist.ToolBelt, HeldTool.Wrench, 0xd9822b, 0x9fe7ff)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Caretaker, Name = CastIds04E.Name(CastIds04E.Caretaker), Role = "Bondkeeper's Refuge caretaker",
                    Post = CastPostKind.VenueStaff, Venue = VenueIds04E.Bondkeeper, Anchor = "staff",
                    Intro = "Slow voice, open hands. That is the whole trick with frightened creatures. Mara taught me that.",
                    Greeting = "They heard you coming. That is a good sign.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Apron, CostumeWaist.Pouches, HeldTool.Medkit, 0x6b8f4e, 0xf3e3c3)
                },
                new CastDef04E
                {
                    Id = CastIds04E.RefugeKeeper, Name = CastIds04E.Name(CastIds04E.RefugeKeeper), Role = "Lantern Refuge keeper",
                    Post = CastPostKind.UnderAnchor, Anchor = "refuge.caretaker",
                    Intro = "Lantern Refuge. Rest while the lamps burn. Nothing hunts inside their light.",
                    Greeting = "Sit a while. The lamps are lit.",
                    Costume = Costume(CostumeHead.Hood, CostumeTorso.Cloak, CostumeWaist.None, HeldTool.Lantern, 0x4a5d7a, 0x9be8ff)
                },
                new CastDef04E
                {
                    Id = CastIds04E.GardensKeeper, Name = CastIds04E.Name(CastIds04E.GardensKeeper), Role = "Pyre Gardens caretaker",
                    Post = CastPostKind.UnderAnchor, Anchor = "gardens.caretaker",
                    Intro = "Careful where you step. Half of what glows down here is alive, and the other half is hot.",
                    Greeting = "The roots are restless today.",
                    Costume = Costume(CostumeHead.BrimHat, CostumeTorso.Mantle, CostumeWaist.None, HeldTool.Staff, 0x7a4b2a, 0xff9d3c)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Clerk, Name = CastIds04E.Name(CastIds04E.Clerk), Role = "Resonance Office clerk",
                    Post = CastPostKind.VenueStaff, Venue = VenueIds04E.Office, Anchor = "staff",
                    Intro = "Resonance Office. Forms on the left, records on the right, and nobody reads either.",
                    Greeting = "Take a number. Any number.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Sash, CostumeWaist.None, HeldTool.Ledger, 0x2b2d42, 0xc9a227)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Ion, Name = CastIds04E.Name(CastIds04E.Ion), Role = "signal engineer",
                    Post = CastPostKind.VenueStaff, Venue = "observatory", Anchor = "instrument2", StoryUnlock = 1, StoryIdentity = true, OnDemand = true,
                    RemoteNote = "Ion speaks over the call screen at Cinder Dispatch until the observatory chapter.",
                    RemoteVenue = VenueIds04E.Dispatch, RemoteAnchor = "ion_screen",
                    Intro = "The signal says you are standing right in front of me. Hi.",
                    Greeting = "The instruments like you. That is weird.",
                    Costume = Costume(CostumeHead.Goggles, CostumeTorso.Satchel, CostumeWaist.None, HeldTool.DataSlate, 0x51607a, 0x6cf0ff)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Mara, Name = CastIds04E.Name(CastIds04E.Mara), Role = "street clinic doctor",
                    Post = CastPostKind.VenueStaff, Venue = "clinic", Anchor = "staff", Poi = "Mara Solane", StoryUnlock = 1, StoryIdentity = true,
                    Intro = "Sit. Whatever you broke, I have patched worse this week.",
                    Greeting = "Still in one piece? Good.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Satchel, CostumeWaist.None, HeldTool.Medkit, 0xe8e2d6, 0xd94a4a)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Jax, Name = CastIds04E.Name(CastIds04E.Jax), Role = "Jax Calder Customs",
                    Post = CastPostKind.VenueStaff, Venue = "garage", Anchor = "staff", Poi = "Jax Calder", StoryUnlock = 2, StoryIdentity = true,
                    Intro = "Bay is open. Do not touch anything that is glowing, humming or mine.",
                    Greeting = "Your engine sounds jealous of you.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.None, CostumeWaist.ToolBelt, HeldTool.Wrench, 0x2e3440, 0xffc857)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Dante, Name = CastIds04E.Name(CastIds04E.Dante), Role = "Forgewright",
                    Post = CastPostKind.VenueStaff, Venue = "forge", Anchor = "staff", Poi = "Dante Rook", StoryUnlock = 4, StoryIdentity = true,
                    Intro = "One more impossible job, Malik. I keep saying that.",
                    Greeting = "Forge is hot. Talk fast.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Apron, CostumeWaist.ToolBelt, HeldTool.Hammer, 0x4a3426, 0xff7a29)
                },
                new CastDef04E
                {
                    Id = CastIds04E.Iris, Name = CastIds04E.Name(CastIds04E.Iris), Role = "archivist",
                    Post = CastPostKind.VenueStaff, Venue = "archive", Anchor = "staff", Poi = "Iris Moon", StoryUnlock = 7, StoryIdentity = true,
                    Intro = "Every record lies a little. The trick is knowing which way.",
                    Greeting = "I found something you will hate.",
                    Costume = Costume(CostumeHead.Circlet, CostumeTorso.Mantle, CostumeWaist.None, HeldTool.Ledger, 0x3d2f5b, 0xb8a1ff)
                },
                new CastDef04E
                {
                    Id = CastIds04E.June, Name = CastIds04E.Name(CastIds04E.June), Role = "civic liaison",
                    Post = CastPostKind.PoiDoor, Poi = "June Pyre", StoryUnlock = 6, StoryIdentity = true, OnDemand = true,
                    RemoteNote = "June Pyre meets you in person when the civic chapter opens.",
                    Intro = "The city keeps score, Malik. Let us make the numbers mean something.",
                    Greeting = "Walk with me.",
                    Costume = Costume(CostumeHead.None, CostumeTorso.Sash, CostumeWaist.Pouches, HeldTool.Clipboard, 0x8c2f39, 0xf4d58d)
                }
            };
        }

        /// <summary>
        /// The body look of a cast member: hair, headwear, clothing, height and build chosen so each person reads at a
        /// distance. Mara and Ion keep their original story looks. Unknown ids give a plain citizen.
        /// </summary>
        public static Appearance Look(string id)
        {
            Appearance a;
            switch (id)
            {
                case CastIds04E.Nessa:
                    a = Base(4101, true, 1.02f, 0.95f);
                    a.HairStyle = HairStyle.Braids; a.Hair = 1; a.Skin = 4;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 5; a.ShirtRow = 1; a.Shirt = 9; a.Pants = 0;
                    a.Glasses = true; a.SuitStyle = 1; a.Glow = 4;
                    return a;
                case CastIds04E.Dispatcher:
                    a = Base(4102, false, 0.98f, 1.12f);
                    a.HairStyle = HairStyle.Short; a.Headwear = Headwear.Cap; a.HatRow = 1; a.Hat = 7; a.Beard = true;
                    a.Jacket = true; a.JacketRow = 3; a.JacketColor = 8; a.ShirtRow = 1; a.Shirt = 4; a.SuitStyle = 1; a.Glow = 7;
                    return a;
                case CastIds04E.Maintainer:
                    a = Base(4103, true, 0.96f, 1.08f);
                    a.HairStyle = HairStyle.Undercut; a.Hair = 4; a.Headwear = Headwear.Helmet; a.HatRow = 1; a.Hat = 11;
                    a.Jacket = false; a.LongSleeves = false; a.ShirtRow = 1; a.Shirt = 3; a.PantsRow = 1; a.Pants = 3;
                    a.GauntletStyle = 1; a.SuitStyle = 0; a.Glow = 0;
                    return a;
                case CastIds04E.Caretaker:
                    a = Base(4104, true, 0.97f, 1.02f);
                    a.HairStyle = HairStyle.Bun; a.Hair = 5; a.Skin = 1;
                    a.Jacket = false; a.LongSleeves = true; a.ShirtRow = 1; a.Shirt = 12; a.PantsRow = 1; a.Pants = 6;
                    a.SuitStyle = 0; a.Glow = 3;
                    return a;
                case CastIds04E.RefugeKeeper:
                    a = Base(4105, true, 1.0f, 0.92f);
                    a.HairStyle = HairStyle.Long; a.Hair = 2; a.Skin = 2;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 9; a.ShirtRow = 1; a.Shirt = 14; a.SuitStyle = 1; a.Glow = 5;
                    a.Implant = 2; a.EyeColor = 12;
                    return a;
                case CastIds04E.GardensKeeper:
                    a = Base(4106, false, 1.06f, 1.2f);
                    a.HairStyle = HairStyle.Bald; a.Beard = true; a.Skin = 0;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 3; a.ShirtRow = 1; a.Shirt = 2; a.ShellStyle = 1; a.ArmorColor = 2;
                    a.SuitStyle = 3; a.Glow = 4;
                    return a;
                case CastIds04E.Clerk:
                    a = Base(4107, false, 1.05f, 0.85f);
                    a.HairStyle = HairStyle.Short; a.Hair = 0; a.Glasses = true; a.Beard = false;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 0; a.ShirtRow = 1; a.Shirt = 15; a.SuitStyle = 0; a.Glow = 0;
                    return a;
                case CastIds04E.Ion:
                    return StoryMissions.IonLook();
                case CastIds04E.Mara:
                    return StoryMissions.MaraLook();
                case CastIds04E.Jax:
                    a = Base(4110, false, 1.0f, 1.16f);
                    a.HairStyle = HairStyle.Mohawk; a.Hair = 3; a.Headwear = Headwear.Bandana; a.HatRow = 1; a.Hat = 2;
                    a.Jacket = true; a.JacketRow = 3; a.JacketColor = 12; a.LongSleeves = false; a.GauntletStyle = 1; a.SuitStyle = 0;
                    return a;
                case CastIds04E.Dante:
                    a = Base(4111, false, 1.04f, 1.25f);
                    a.HairStyle = HairStyle.Long; a.Hair = 0; a.Beard = true; a.Skin = 3;
                    a.Jacket = true; a.JacketRow = 3; a.JacketColor = 0; a.LongSleeves = false; a.GauntletStyle = 1; a.Glow = 4;
                    return a;
                case CastIds04E.Iris:
                    a = Base(4112, true, 1.03f, 0.9f);
                    a.HairStyle = HairStyle.Long; a.Hair = 0; a.Glasses = false; a.Implant = 2 | 1; a.EyeColor = 13;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 6; a.ShirtRow = 1; a.Shirt = 13; a.SuitStyle = 2; a.Glow = 5;
                    return a;
                case CastIds04E.June:
                    a = Base(4113, true, 1.04f, 1.0f);
                    a.HairStyle = HairStyle.Undercut; a.Hair = 1; a.Skin = 5;
                    a.Jacket = true; a.JacketRow = 1; a.JacketColor = 1; a.ShirtRow = 1; a.Shirt = 10; a.SuitStyle = 1; a.Glow = 1;
                    return a;
                default:
                    return Appearance.Random(new System.Random(4199));
            }
        }

        private static Appearance Base(int seed, bool female, float height, float bulk)
        {
            Appearance a = Appearance.Random(new System.Random(seed));
            a.Female = female;
            if (female) a.Beard = false;
            a.Height = height;
            a.Bulk = bulk;
            a.Headwear = Headwear.None;
            a.Implant &= ~16;
            a.ShellStyle = 0;
            a.VisorStyle = 0;
            a.Creature = HumanoidCreature.None;
            return a;
        }

        /// <summary>A short code naming the parts of a silhouette that read at a distance: hair, headwear, costume
        /// head layer, torso layer and held tool. Tests require it to differ for every pair of cast members.</summary>
        public static string Silhouette(string id)
        {
            CastDef04E d = Find(id);
            if (d == null) return "";
            Appearance a = Look(id);
            return a.HairStyle + "/" + a.Headwear + "/" + d.Costume.Head + "/" + d.Costume.Torso + "/" + d.Costume.Tool;
        }

        /// <summary>How many of the readable silhouette layers (headgear or costume head, torso layer, held tool) a
        /// cast member wears. Every cast member wears at least two.</summary>
        public static int SilhouetteLayers(string id)
        {
            CastDef04E d = Find(id);
            if (d == null) return 0;
            Appearance a = Look(id);
            int n = 0;
            if (d.Costume.Head != CostumeHead.None || a.Headwear != Headwear.None) n++;
            if (d.Costume.Torso != CostumeTorso.None || d.Costume.Waist != CostumeWaist.None) n++;
            if (d.Costume.Tool != HeldTool.None) n++;
            return n;
        }
    }
}
