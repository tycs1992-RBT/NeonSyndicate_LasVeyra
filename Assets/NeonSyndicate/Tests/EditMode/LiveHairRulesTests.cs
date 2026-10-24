using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The live hair rules (LiveHairRules): natural hair colours from the palette index, dyed tips only for gang members,
    /// the headpiece a role and look wear, how it covers the hair, beards, and the cast colours. Engine-free.
    /// </summary>
    public sealed class LiveHairRulesTests
    {
        private static Appearance Look(int hair, int shirt, int pants, bool female, bool beard, Headwear headwear, bool glasses)
        {
            Appearance a = new Appearance();
            a.Hair = hair;
            a.Shirt = shirt;
            a.Pants = pants;
            a.Female = female;
            a.Beard = beard;
            a.Headwear = headwear;
            a.Glasses = glasses;
            a.HairStyle = HairStyle.Short;
            return a;
        }

        [Test]
        public void EveryPaletteColumnBecomesTwoNaturalShades()
        {
            for (int hair = 0; hair < 6; hair++)
            {
                int a = LiveHairRules.NaturalIndex(Look(hair, 0, 0, false, false, Headwear.None, false));
                int b = LiveHairRules.NaturalIndex(Look(hair, 1, 0, false, false, Headwear.None, false));
                Assert.That(a, Is.InRange(0, LiveHairRules.Naturals.Length - 1));
                Assert.That(b, Is.InRange(0, LiveHairRules.Naturals.Length - 1));
                if (hair != 0) Assert.AreNotEqual(a, b, "column " + hair + " has a second shade");
            }
            Assert.AreEqual(LiveHairRules.Black, LiveHairRules.NaturalIndex(Look(0, 1, 0, false, false, Headwear.None, false)));
            Assert.AreEqual(LiveHairRules.Grey, LiveHairRules.NaturalIndex(Look(4, 0, 0, false, false, Headwear.None, false)));
            Assert.AreEqual(LiveHairRules.White, LiveHairRules.NaturalIndex(Look(4, 1, 0, false, false, Headwear.None, false)));
            Assert.AreEqual(LiveHairRules.Red, LiveHairRules.NaturalIndex(Look(5, 0, 0, false, false, Headwear.None, false)));
            Assert.AreEqual(LiveHairRules.Red, LiveHairRules.NaturalIndex(Look(-7, 0, 0, false, false, Headwear.None, false)), "negative indices wrap (-7 is column 5)");
            foreach (Color c in LiveHairRules.Naturals)
            {
                Assert.That(c.r, Is.InRange(0f, 1f));
                Assert.That(c.g, Is.InRange(0f, 1f));
                Assert.That(c.b, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void DyedTipsOnlyForGangMembersWithTheImplantBit()
        {
            Appearance dyed = Look(2, 0, 0, false, false, Headwear.None, false);
            dyed.Implant = LiveHairRules.DyedTipsBit | 1;
            Appearance plain = Look(2, 0, 0, false, false, Headwear.None, false);
            Assert.IsTrue(LiveHairRules.DyedTips(NpcRole.Gang, dyed));
            Assert.IsFalse(LiveHairRules.DyedTips(NpcRole.Gang, plain));
            Assert.IsFalse(LiveHairRules.DyedTips(NpcRole.Civilian, dyed));
            Assert.IsFalse(LiveHairRules.DyedTips(NpcRole.Enemy, dyed));
            Color gang = new Color(0.1f, 0.8f, 0.3f);
            LiveHeadTint gangTint = LiveHairRules.Colors(NpcRole.Gang, dyed, gang, true, LiveHeadpiece.None);
            LiveHeadTint civic = LiveHairRules.Colors(NpcRole.Civilian, dyed, gang, true, LiveHeadpiece.None);
            Assert.AreNotEqual(gangTint.Hair, gangTint.Tips, "the gang member's tips carry the gang colour");
            Assert.Greater(gangTint.Tips.g, gangTint.Tips.r, "toward the gang green");
            Assert.AreEqual(civic.Hair, civic.Tips, "everyone else keeps natural tips");
            Assert.AreEqual(1f, gangTint.Hair.a);
            Assert.AreEqual(1f, gangTint.Beard.a);
            Assert.Less(gangTint.Beard.r, gangTint.Hair.r, "the beard is a shade darker than the hair");
        }

        [Test]
        public void ColorsAreDeterministicAndFollowTheRole()
        {
            Appearance look = Look(1, 3, 5, false, true, Headwear.Cap, false);
            LiveHeadTint a = LiveHairRules.Colors(NpcRole.Civilian, look, Color.black, false, LiveHeadpiece.Cap);
            LiveHeadTint b = LiveHairRules.Colors(NpcRole.Civilian, look, Color.black, false, LiveHeadpiece.Cap);
            Assert.AreEqual(a.Hair, b.Hair);
            Assert.AreEqual(a.HeadCloth, b.HeadCloth);
            LiveHeadTint police = LiveHairRules.Colors(NpcRole.Police, look, Color.black, false, LiveHeadpiece.OfficerCap);
            Assert.Greater(police.HeadCloth.b, police.HeadCloth.r, "the officer's cap is Veil navy");
            Assert.AreEqual(Color.black, police.HeadGlow);
            LiveHeadTint swat = LiveHairRules.Colors(NpcRole.Swat, look, Color.black, false, LiveHeadpiece.Helm);
            Assert.AreNotEqual(Color.black, swat.HeadGlow, "the sentinel visor is lit");
            Assert.Less(swat.HeadMetal.r, 0.2f, "near-black steel");
            Appearance bright = Look(1, 3, 5, false, true, Headwear.Beanie, false);
            bright.Hat = 1;
            LiveHeadTint hood = LiveHairRules.Colors(NpcRole.Enemy, bright, Color.black, false, LiveHeadpiece.Hood);
            LiveHeadTint beanie = LiveHairRules.Colors(NpcRole.Civilian, bright, Color.black, false, LiveHeadpiece.Beanie);
            Assert.Less(hood.HeadCloth.maxColorComponent, beanie.HeadCloth.maxColorComponent, "an enemy's hood is darker than a civilian's pale beanie");
        }

        [Test]
        public void HeadpiecesFollowRoleAndLook()
        {
            Assert.AreEqual(LiveHeadpiece.OfficerCap, LiveHairRules.Headpiece(NpcRole.Police, Look(0, 0, 0, false, false, Headwear.Helmet, false)));
            Assert.AreEqual(LiveHeadpiece.OfficerCap, LiveHairRules.Headpiece(NpcRole.Police, Look(0, 0, 0, true, false, Headwear.None, false)));
            Assert.AreEqual(LiveHeadpiece.Helm, LiveHairRules.Headpiece(NpcRole.Swat, Look(0, 0, 0, false, false, Headwear.Helmet, false)));
            Assert.AreEqual(LiveHeadpiece.Cap, LiveHairRules.Headpiece(NpcRole.Civilian, Look(0, 0, 0, false, false, Headwear.Cap, true)));
            Assert.AreEqual(LiveHeadpiece.Beanie, LiveHairRules.Headpiece(NpcRole.Clerk, Look(0, 0, 0, false, false, Headwear.Beanie, false)));
            Assert.AreEqual(LiveHeadpiece.Hood, LiveHairRules.Headpiece(NpcRole.Gang, Look(0, 0, 0, false, false, Headwear.Beanie, false)));
            Assert.AreEqual(LiveHeadpiece.Hood, LiveHairRules.Headpiece(NpcRole.Enemy, Look(0, 0, 0, false, false, Headwear.Beanie, false)));
            Assert.AreEqual(LiveHeadpiece.Bandana, LiveHairRules.Headpiece(NpcRole.Gang, Look(0, 0, 0, false, false, Headwear.Bandana, false)));
            Assert.AreEqual(LiveHeadpiece.WorkHelm, LiveHairRules.Headpiece(NpcRole.Civilian, Look(0, 0, 0, false, false, Headwear.Helmet, false)));
            Assert.AreEqual(LiveHeadpiece.Glasses, LiveHairRules.Headpiece(NpcRole.Civilian, Look(0, 0, 0, false, false, Headwear.None, true)));
            Assert.AreEqual(LiveHeadpiece.None, LiveHairRules.Headpiece(NpcRole.Civilian, Look(0, 0, 0, false, false, Headwear.None, false)));
        }

        [Test]
        public void CastHeadpiecesComeFromTheCostumeFirst()
        {
            Appearance cap = Look(0, 0, 0, false, false, Headwear.Cap, false);
            Assert.AreEqual(LiveHeadpiece.Hood, LiveHairRules.CastHeadpiece(CostumeHead.Hood, cap));
            Assert.AreEqual(LiveHeadpiece.BrimHat, LiveHairRules.CastHeadpiece(CostumeHead.BrimHat, cap));
            Assert.AreEqual(LiveHeadpiece.Headset, LiveHairRules.CastHeadpiece(CostumeHead.Headset, cap));
            Assert.AreEqual(LiveHeadpiece.Goggles, LiveHairRules.CastHeadpiece(CostumeHead.Goggles, cap));
            Assert.AreEqual(LiveHeadpiece.Circlet, LiveHairRules.CastHeadpiece(CostumeHead.Circlet, cap));
            Assert.AreEqual(LiveHeadpiece.Cap, LiveHairRules.CastHeadpiece(CostumeHead.None, cap));
            Assert.AreEqual(LiveHeadpiece.Glasses, LiveHairRules.CastHeadpiece(CostumeHead.None, Look(0, 0, 0, true, false, Headwear.None, true)));
            foreach (string id in CastIds04E.All)
            {
                CastDef04E def = CastRoster04E.Find(id);
                LiveHeadpiece piece = LiveHairRules.CastHeadpiece(def.Costume.Head, CastRoster04E.Look(id));
                LiveHeadTint tint = LiveHairRules.CastColors(CastRoster04E.Look(id), def.Costume.Main, def.Costume.Accent, piece);
                Assert.AreEqual(1f, tint.HeadCloth.a, id);
                if (piece == LiveHeadpiece.Goggles || piece == LiveHeadpiece.Circlet || piece == LiveHeadpiece.Headset) Assert.AreNotEqual(Color.black, tint.HeadGlow, id + " glows like its costume");
                else Assert.AreEqual(Color.black, tint.HeadGlow, id);
            }
        }

        [Test]
        public void DropInOwnHairIsReadFromTheMaterialName()
        {
            Assert.IsTrue(LiveCastFolderRules.OwnHairMaterial("Hair"));
            Assert.IsTrue(LiveCastFolderRules.OwnHairMaterial("Hair.001"));
            Assert.IsTrue(LiveCastFolderRules.OwnHairMaterial("Long hair"));
            Assert.IsTrue(LiveCastFolderRules.OwnHairMaterial("Hair_Cards (Instance)"));
            Assert.IsFalse(LiveCastFolderRules.OwnHairMaterial("MI_Hair_1"), "the pack's eyebrow material is not hair");
            Assert.IsFalse(LiveCastFolderRules.OwnHairMaterial("Eyebrow hair"), "brows are not hair");
            Assert.IsFalse(LiveCastFolderRules.OwnHairMaterial("Coat"));
            Assert.IsFalse(LiveCastFolderRules.OwnHairMaterial(""));
            Assert.AreEqual(LiveCrowdSurface.Unknown, LiveCrowdRules.Surface("Live hair strands"), "the live slots set their surfaces themselves");
            Color multiplier = LiveCrowdRules.ClothMultiplier(new Color(0.11f, 0.16f, 0.27f));
            Assert.Greater(multiplier.b, multiplier.r, "a navy target asks for more blue from the dark wool map");
            Assert.That(multiplier.r, Is.InRange(LiveCrowdRules.MinTint, LiveCrowdRules.MaxTint));
        }

        [Test]
        public void CoverAndBeardRules()
        {
            Assert.AreEqual(LiveHairCover.Capped, LiveHairRules.Cover(LiveHeadpiece.Cap));
            Assert.AreEqual(LiveHairCover.Capped, LiveHairRules.Cover(LiveHeadpiece.Bandana));
            Assert.AreEqual(LiveHairCover.Capped, LiveHairRules.Cover(LiveHeadpiece.BrimHat));
            Assert.AreEqual(LiveHairCover.Hidden, LiveHairRules.Cover(LiveHeadpiece.Helm));
            Assert.AreEqual(LiveHairCover.Hidden, LiveHairRules.Cover(LiveHeadpiece.Hood));
            Assert.AreEqual(LiveHairCover.None, LiveHairRules.Cover(LiveHeadpiece.Goggles));
            Assert.AreEqual(LiveHairCover.None, LiveHairRules.Cover(LiveHeadpiece.None));
            Assert.AreEqual(LiveBeard.None, LiveHairRules.Beard(Look(0, 0, 0, true, true, Headwear.None, false)), "women never");
            Assert.AreEqual(LiveBeard.None, LiveHairRules.Beard(Look(0, 0, 0, false, false, Headwear.None, false)));
            Appearance bearded = Look(0, 0, 0, false, true, Headwear.None, false);
            LiveBeard first = LiveHairRules.Beard(bearded);
            Assert.AreNotEqual(LiveBeard.None, first);
            Assert.AreEqual(first, LiveHairRules.Beard(bearded), "deterministic");
            bearded.Skin = 1;
            Assert.AreNotEqual(first, LiveHairRules.Beard(bearded), "the other kind by the look's other indices");
            Assert.AreEqual(HairStyle.Bald, LiveHairRules.Style(Look(0, 0, 0, false, false, Headwear.None, false), LiveHairCover.Hidden));
            Assert.AreEqual(HairStyle.Short, LiveHairRules.Style(Look(0, 0, 0, false, false, Headwear.None, false), LiveHairCover.Capped));
            Assert.IsFalse(LiveHairRules.ShowsHair(HairStyle.Bald, LiveHairCover.None));
            Assert.IsFalse(LiveHairRules.ShowsHair(HairStyle.Long, LiveHairCover.Hidden));
            Assert.IsTrue(LiveHairRules.ShowsHair(HairStyle.Long, LiveHairCover.Capped));
        }
    }
}
