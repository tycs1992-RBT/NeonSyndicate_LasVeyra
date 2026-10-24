using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The live crowd rules (owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies): which crowd
    /// row serves an NPC, the material and armour parsing, the deterministic per-person tint, the camera budget with its
    /// hysteresis and the live contract of a crowd row. Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class LiveCrowdRulesTests
    {
        /// <summary>A crowd row the way the generator writes it: the Dante test skeleton with a crowd identity and roles.</summary>
        internal static LiveCastRow Crowd(string slug, bool female, int roles)
        {
            ImportedModel04OD review = DanteReviewH8STests.Row();
            review.Identity = LiveCrowdRules.Prefix + slug;
            review.ArtProof.AssetId = "CROWD-H8R-" + slug.ToUpperInvariant();
            review.ArtProof.ExportAssetPath = "Assets/NeonSyndicate/Art/ProductionH8R/NPC/" + slug + ".fbx";
            review.ArtProof.RightsRecordId = "h8r-npc-study-cc0-lineage";
            LiveCastRow row = LiveCastRulesTests.Live(review, "", new string[0]);
            row.CrowdRoles = roles;
            row.CrowdFemale = female;
            row.Label = "crowd " + slug;
            row.Origin = "builtin:crowd-" + slug;
            return row;
        }

        private static Appearance Look(int skin, int hair, int shirt, int pants, bool female, float height, float bulk)
        {
            Appearance a = new Appearance();
            a.Skin = skin;
            a.Hair = hair;
            a.Shirt = shirt;
            a.Pants = pants;
            a.Female = female;
            a.Height = height;
            a.Bulk = bulk;
            return a;
        }

        [Test]
        public void RoleMasksCoverCrowdsButNeverCompanions()
        {
            foreach (NpcRole role in new[] { NpcRole.Civilian, NpcRole.Clerk, NpcRole.Police, NpcRole.Swat, NpcRole.Gang, NpcRole.Enemy })
                Assert.AreNotEqual(0, LiveCrowdRules.CrowdRoles & LiveCrowdRules.RoleBit(role), role.ToString());
            Assert.AreEqual(0, LiveCrowdRules.CrowdRoles & LiveCrowdRules.RoleBit(NpcRole.Companion));
            Assert.AreEqual(LiveCrowdRules.PlainRoles, LiveCrowdRules.PlainRoles & LiveCrowdRules.CrowdRoles, "plain roles are crowd roles");
            Assert.IsTrue(LiveCrowdRules.ValidRoles(LiveCrowdRules.CrowdRoles));
            Assert.IsTrue(LiveCrowdRules.ValidRoles(LiveCrowdRules.RoleBit(NpcRole.Police)));
            Assert.IsFalse(LiveCrowdRules.ValidRoles(0));
            Assert.IsFalse(LiveCrowdRules.ValidRoles(LiveCrowdRules.CrowdRoles | LiveCrowdRules.RoleBit(NpcRole.Companion)));
        }

        [Test]
        public void DropInRoleListsParseLeniently()
        {
            int mask;
            Assert.IsTrue(LiveCrowdRules.ParseRoles(null, out mask));
            Assert.AreEqual(LiveCrowdRules.CrowdRoles, mask, "no list means every crowd role");
            Assert.IsTrue(LiveCrowdRules.ParseRoles(new string[0], out mask));
            Assert.AreEqual(LiveCrowdRules.CrowdRoles, mask);
            Assert.IsTrue(LiveCrowdRules.ParseRoles(new[] { "civilian", " Clerk " }, out mask));
            Assert.AreEqual(LiveCrowdRules.RoleBit(NpcRole.Civilian) | LiveCrowdRules.RoleBit(NpcRole.Clerk), mask);
            Assert.IsTrue(LiveCrowdRules.ParseRoles(new[] { "ALL" }, out mask));
            Assert.AreEqual(LiveCrowdRules.CrowdRoles, mask);
            Assert.IsFalse(LiveCrowdRules.ParseRoles(new[] { "Companion" }, out mask), "companions are never crowd");
            Assert.AreEqual(0, mask);
            Assert.IsFalse(LiveCrowdRules.ParseRoles(new[] { "Police", "Wizard" }, out mask));
            Assert.AreEqual(0, mask);
            Assert.AreEqual("civilians, clerks", LiveCrowdRules.RoleList(LiveCrowdRules.RoleBit(NpcRole.Civilian) | LiveCrowdRules.RoleBit(NpcRole.Clerk)));
            Assert.AreEqual("nobody", LiveCrowdRules.RoleList(0));
            StringAssert.Contains("mission enemies", LiveCrowdRules.RoleList(LiveCrowdRules.CrowdRoles));
        }

        [Test]
        public void CrowdIdentitiesAreSlugsAndNeverActorIdentities()
        {
            Assert.IsTrue(LiveCrowdRules.ValidIdentity("crowd:court-defender-h8r"));
            Assert.IsTrue(LiveCrowdRules.ValidIdentity("crowd:guild-artisan"));
            foreach (string bad in new[] { "", "crowd:", "crowd:Court Defender", "crowd:a/b", "crowd:a:b", "npc:Corin Pell", "malik", "Crowd:x" })
                Assert.IsFalse(LiveCrowdRules.ValidIdentity(bad), bad);
            Assert.IsTrue(LiveCastRules.ValidIdentity("crowd:court-defender-h8r"), "the live rules accept crowd identities");
            Assert.IsFalse(LiveCastRules.Matches("crowd:court-defender-h8r", true, "", "npc:Pedestrian", ""), "a crowd row never matches by name");
            Assert.AreEqual(-1, LiveCastRules.Find(new List<LiveCastRow> { Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles) }, "npc:Hostile", ""));
        }

        [Test]
        public void ProportionsOutsideTheRangeStayProcedural()
        {
            Assert.IsTrue(LiveCrowdRules.ProportionsFit(1f, 1f));
            Assert.IsTrue(LiveCrowdRules.ProportionsFit(0.93f, 0.85f), "the shortest random civilian");
            Assert.IsTrue(LiveCrowdRules.ProportionsFit(1.07f, 1.2f), "the tallest random civilian");
            Assert.IsTrue(LiveCrowdRules.ProportionsFit(1.07f, 1.25f), "a sentinel");
            Assert.IsTrue(LiveCrowdRules.ProportionsFit(0f, 0f), "unset fields count as one");
            Assert.IsFalse(LiveCrowdRules.ProportionsFit(0.72f, 0.85f), "a child-sized body");
            Assert.IsFalse(LiveCrowdRules.ProportionsFit(1.2f, 1f));
            Assert.IsFalse(LiveCrowdRules.ProportionsFit(1f, 1.4f));
            Assert.IsFalse(LiveCrowdRules.ProportionsFit(float.NaN, 1f));
        }

        [Test]
        public void ServesByRoleAndSexOnly()
        {
            int all = LiveCrowdRules.CrowdRoles;
            Assert.IsTrue(LiveCrowdRules.Serves(all, false, true, NpcRole.Civilian, false, false, false, true));
            Assert.IsTrue(LiveCrowdRules.Serves(all, true, true, NpcRole.Police, true, false, false, true), "a female body serves a female officer");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, true, NpcRole.Civilian, true, false, false, true), "a male body never serves a woman");
            Assert.IsFalse(LiveCrowdRules.Serves(all, true, true, NpcRole.Gang, false, false, false, true), "a female body never serves a man");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, true, NpcRole.Companion, false, false, false, true), "companions stay procedural");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, true, NpcRole.Civilian, false, true, false, true), "creature-mode people stay procedural");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, true, NpcRole.Civilian, false, false, true, true), "named cast actors stay procedural");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, true, NpcRole.Civilian, false, false, false, false), "extreme proportions stay procedural");
            Assert.IsFalse(LiveCrowdRules.Serves(all, false, false, NpcRole.Civilian, false, false, false, true), "disabled rows never serve");
            Assert.IsFalse(LiveCrowdRules.Serves(LiveCrowdRules.RoleBit(NpcRole.Police), false, true, NpcRole.Civilian, false, false, false, true), "a police-only body leaves civilians alone");
        }

        [Test]
        public void FindCrowdTakesTheFirstServingRowSoDropInsWin()
        {
            LiveCastRow dropIn = Crowd("own-man", false, LiveCrowdRules.CrowdRoles);
            dropIn.Origin = "dropin:crowd";
            LiveCastRow male = Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles);
            LiveCastRow female = Crowd("guild-artisan-h8r", true, LiveCrowdRules.CrowdRoles);
            LiveCastRow dante = LiveCastRulesTests.Dante();
            List<LiveCastRow> rows = new List<LiveCastRow> { null, dante, dropIn, male, female };
            Assert.AreEqual(2, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, false, false, 1f, 1f));
            Assert.AreEqual(4, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, true, false, false, 1f, 1f));
            Assert.AreEqual(4, LiveCrowdRules.FindCrowd(rows, NpcRole.Gang, true, false, false, 0.98f, 1.1f));
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(rows, NpcRole.Companion, false, false, false, 1f, 1f));
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, true, false, 1f, 1f));
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, false, true, 1f, 1f));
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, false, false, 0.7f, 1f));
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(null, NpcRole.Civilian, false, false, false, 1f, 1f));
            dropIn.Model.Enabled = false;
            Assert.AreEqual(3, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, false, false, 1f, 1f), "a disabled drop-in leaves the built-in body");
        }

        [Test]
        [Category("NeedsEngine")]
        public void RegistryFindsCrowdRows()
        {
            LiveCastRow male = Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles);
            LiveCastRow female = Crowd("guild-artisan-h8r", true, LiveCrowdRules.CrowdRoles);
            LiveCastRow dante = LiveCastRulesTests.Dante();
            LiveCastRegistry registry = ScriptableObject.CreateInstance<LiveCastRegistry>();
            registry.Rows = new[] { dante, male, female };
            Assert.IsTrue(registry.HasCrowdRow);
            Assert.AreSame(female, registry.FindCrowd(NpcRole.Clerk, true, false, false, 1f, 1f));
            Assert.AreSame(male, registry.FindCrowd(NpcRole.Enemy, false, false, false, 1f, 1f));
            Assert.IsNull(registry.FindCrowd(NpcRole.Companion, true, false, false, 1f, 1f));
            registry.Rows = new[] { dante };
            Assert.IsFalse(registry.HasCrowdRow, "a cast-bound row is not a crowd row");
            UnityEngine.Object.DestroyImmediate(registry);
        }

        [Test]
        public void SurfacesAreReadFromTheImportedMaterialNames()
        {
            Assert.AreEqual(LiveCrowdSurface.Skin, LiveCrowdRules.Surface("CourtDefender Skin and underlayer"));
            Assert.AreEqual(LiveCrowdSurface.Skin, LiveCrowdRules.Surface("GuildArtisan Skin and underlayer (Instance)"));
            Assert.AreEqual(LiveCrowdSurface.Wool, LiveCrowdRules.Surface("H6 Charcoal wool.001"));
            Assert.AreEqual(LiveCrowdSurface.Wool, LiveCrowdRules.Surface("H6_Charcoal_wool"));
            Assert.AreEqual(LiveCrowdSurface.Leather, LiveCrowdRules.Surface("H6 Worn leather.001"));
            Assert.AreEqual(LiveCrowdSurface.Bronze, LiveCrowdRules.Surface("H6 Aged bronze"));
            Assert.AreEqual(LiveCrowdSurface.Steel, LiveCrowdRules.Surface("H6 Blackened steel.001"));
            Assert.AreEqual(LiveCrowdSurface.Brows, LiveCrowdRules.Surface("MI_Hair_1"));
            Assert.AreEqual(LiveCrowdSurface.Brows, LiveCrowdRules.Surface("MI_Hair_2"));
            Assert.AreEqual(LiveCrowdSurface.Eyes, LiveCrowdRules.Surface("MI_Eyes.001"));
            Assert.AreEqual(LiveCrowdSurface.Eyes, LiveCrowdRules.Surface("MI Eyes"));
            Assert.AreEqual(LiveCrowdSurface.Unknown, LiveCrowdRules.Surface("Dante worn forge apron"));
            Assert.AreEqual(LiveCrowdSurface.Unknown, LiveCrowdRules.Surface(""));
            Assert.AreEqual(LiveCrowdSurface.Unknown, LiveCrowdRules.Surface(null));
            // The generator names its prepared materials "<Role> <Surface>"; the live adapter reads those back by their last word.
            foreach (LiveCrowdSurface surface in new[] { LiveCrowdSurface.Skin, LiveCrowdSurface.Wool, LiveCrowdSurface.Leather, LiveCrowdSurface.Bronze, LiveCrowdSurface.Steel, LiveCrowdSurface.Brows, LiveCrowdSurface.Eyes })
            {
                Assert.AreEqual(surface, LiveCrowdRules.Surface("CourtDefender " + surface), surface.ToString());
                Assert.AreEqual(surface, LiveCrowdRules.Surface("GuildArtisan " + surface + " (Instance)"), surface.ToString());
            }
            // Plain drop-in names.
            Assert.AreEqual(LiveCrowdSurface.Wool, LiveCrowdRules.Surface("Veyran Woman Coat"));
            Assert.AreEqual(LiveCrowdSurface.Leather, LiveCrowdRules.Surface("Boots"));
            Assert.AreEqual(LiveCrowdSurface.Brows, LiveCrowdRules.Surface("Hair"));
            Assert.AreEqual(LiveCrowdSurface.Steel, LiveCrowdRules.Surface("Shoulder Armor"));
            Assert.AreEqual(LiveCrowdSurface.Eyes, LiveCrowdRules.Surface("Left Eye"));
            Assert.AreEqual(LiveCrowdSurface.Unknown, LiveCrowdRules.Surface("Visor"));
        }

        [Test]
        public void ArmourPiecesHideOnPlainRolesOnly()
        {
            Assert.IsTrue(LiveCrowdRules.ArmourPart("H7 shoulder armor l0.001", LiveCrowdSurface.Steel));
            Assert.IsTrue(LiveCrowdRules.ArmourPart("anything", LiveCrowdSurface.Steel), "every steel piece is armour");
            Assert.IsTrue(LiveCrowdRules.ArmourPart("H7 front armor edge -1.001", LiveCrowdSurface.Bronze));
            Assert.IsTrue(LiveCrowdRules.ArmourPart("H7 contoured chest plate 1.001", LiveCrowdSurface.Steel));
            Assert.IsTrue(LiveCrowdRules.ArmourPart("Merged armour bronze", LiveCrowdSurface.Bronze));
            Assert.IsFalse(LiveCrowdRules.ArmourPart("Sphere.014", LiveCrowdSurface.Bronze), "a coat fastener is not armour");
            Assert.IsFalse(LiveCrowdRules.ArmourPart("H7 tailored connected coat", LiveCrowdSurface.Wool));
            Assert.IsFalse(LiveCrowdRules.ArmourPart("H7 faced stand collar", LiveCrowdSurface.Leather));
            Assert.IsFalse(LiveCrowdRules.ArmourPart("", LiveCrowdSurface.Skin));
            Assert.IsFalse(LiveCrowdRules.PartShown(true, NpcRole.Civilian));
            Assert.IsFalse(LiveCrowdRules.PartShown(true, NpcRole.Clerk));
            foreach (NpcRole role in new[] { NpcRole.Police, NpcRole.Swat, NpcRole.Gang, NpcRole.Enemy })
            {
                Assert.IsTrue(LiveCrowdRules.PartShown(true, role), role.ToString());
                Assert.IsTrue(LiveCrowdRules.PartShown(false, role), role.ToString());
            }
            Assert.IsTrue(LiveCrowdRules.PartShown(false, NpcRole.Civilian), "the coat always shows");
        }

        private static void InRange(Color c, string what)
        {
            foreach (float v in new[] { c.r, c.g, c.b })
                Assert.That(v, Is.InRange(LiveCrowdRules.MinTint, LiveCrowdRules.MaxTint), what);
        }

        [Test]
        public void TintsAreDeterministicVariedAndBounded()
        {
            Appearance a = Look(2, 1, 5, 2, false, 1f, 1f);
            LiveCrowdTint first = LiveCrowdRules.Tint(NpcRole.Civilian, a, Color.black, false, false);
            LiveCrowdTint again = LiveCrowdRules.Tint(NpcRole.Civilian, a, Color.black, false, false);
            Assert.AreEqual(first.Skin, again.Skin);
            Assert.AreEqual(first.Cloth, again.Cloth);
            Assert.AreEqual(first.Hair, again.Hair);
            Assert.AreEqual(Color.black, first.Glow, "no glow on an ordinary civilian");
            InRange(first.Skin, "skin");
            InRange(first.Cloth, "cloth");
            InRange(first.Leather, "leather");
            InRange(first.Bronze, "bronze");
            // Skin follows the appearance palette from light to deep.
            float previous = float.MaxValue;
            for (int skin = 0; skin < 6; skin++)
            {
                Color result = LiveCrowdRules.SkinResult(skin);
                float lum = result.r + result.g + result.b;
                Assert.Less(lum, previous, "skin " + skin + " is deeper than " + (skin - 1));
                previous = lum;
                InRange(LiveCrowdRules.Tint(NpcRole.Civilian, Look(skin, 0, 0, 0, false, 1f, 1f), Color.black, false, false).Skin, "skin " + skin);
            }
            Assert.Greater(LiveCrowdRules.SkinResult(0).r, 0.8f, "the lightest tone is light");
            Assert.Less(LiveCrowdRules.SkinResult(5).r, 0.35f, "the deepest tone is deep");
            // The brows take the natural hair colour of the live hair (LiveHairRules), so brows and strands match.
            Assert.AreEqual(LiveHairRules.Naturals[LiveHairRules.Blond], LiveCrowdRules.Tint(NpcRole.Civilian, Look(0, 3, 0, 0, false, 1f, 1f), Color.black, false, false).Hair);
            Assert.AreEqual(LiveHairRules.Naturals[LiveHairRules.DarkBrown], LiveCrowdRules.Tint(NpcRole.Civilian, Look(0, 7, 0, 0, false, 1f, 1f), Color.black, false, false).Hair, "hair indices wrap");
            // A cast member's coat and trim follow their costume colours.
            LiveCrowdTint cast = LiveCrowdRules.CastTint(Look(2, 1, 0, 0, true, 1f, 1f), new Color(0.18f, 0.5f, 0.52f), new Color(0.95f, 0.76f, 0.3f));
            Assert.Greater(cast.Cloth.g, cast.Cloth.r, "a teal costume asks the wool for green and blue");
            Assert.Greater(cast.Bronze.r, cast.Bronze.b, "a gold accent warms the trim");
            Assert.AreEqual(Color.black, cast.Glow);
            Assert.AreEqual(LiveCrowdRules.CastTint(Look(2, 1, 0, 0, true, 1f, 1f), Color.red, Color.blue).Skin, cast.Skin, "the skin follows the look, not the costume");
            // Civilians vary their coats by shirt index; the first keeps the charcoal wool as it is.
            Assert.AreEqual(Color.white, LiveCrowdRules.Tint(NpcRole.Civilian, Look(0, 0, 0, 0, false, 1f, 1f), Color.black, false, false).Cloth);
            HashSet<Color> coats = new HashSet<Color>();
            for (int shirt = 0; shirt < 16; shirt++) coats.Add(LiveCrowdRules.Tint(NpcRole.Civilian, Look(0, 0, shirt, 0, false, 1f, 1f), Color.black, false, false).Cloth);
            Assert.AreEqual(8, coats.Count, "eight civilian coat colours");
            HashSet<Color> leathers = new HashSet<Color>();
            for (int pants = 0; pants < 16; pants++) leathers.Add(LiveCrowdRules.Tint(NpcRole.Civilian, Look(0, 0, 0, pants, false, 1f, 1f), Color.black, false, false).Leather);
            Assert.AreEqual(4, leathers.Count, "four leather shades");
            for (int pants = 0; pants < 4; pants++)
            {
                Color leather = LiveCrowdRules.LeatherResult(pants);
                foreach (float v in new[] { leather.r, leather.g, leather.b }) Assert.That(v, Is.InRange(0.03f, 0.25f), "leather stays dark leather, shade " + pants);
            }
        }

        [Test]
        public void RolesAndFactionsColourTheCoat()
        {
            Appearance a = Look(1, 1, 3, 1, false, 1f, 1f);
            LiveCrowdTint civilian = LiveCrowdRules.Tint(NpcRole.Civilian, a, Color.black, false, false);
            LiveCrowdTint police = LiveCrowdRules.Tint(NpcRole.Police, a, Color.black, false, false);
            LiveCrowdTint swat = LiveCrowdRules.Tint(NpcRole.Swat, a, Color.black, false, false);
            LiveCrowdTint enemy = LiveCrowdRules.Tint(NpcRole.Enemy, a, Color.black, false, false);
            LiveCrowdTint clerk = LiveCrowdRules.Tint(NpcRole.Clerk, a, Color.black, false, false);
            Assert.AreNotEqual(civilian.Cloth, police.Cloth);
            Assert.Greater(police.Cloth.b, police.Cloth.r, "police wear Veil navy");
            Assert.Less(swat.Cloth.r + swat.Cloth.g + swat.Cloth.b, police.Cloth.r + police.Cloth.g + police.Cloth.b, "sentinels wear near black");
            Assert.AreEqual(police.Skin, civilian.Skin, "the role never changes the skin");
            InRange(enemy.Cloth, "enemy cloth");
            InRange(clerk.Cloth, "clerk cloth");
            Color ember = new Color(1f, 0.35f, 0.15f);
            LiveCrowdTint gang = LiveCrowdRules.Tint(NpcRole.Gang, a, ember, true, false);
            LiveCrowdTint unknownGang = LiveCrowdRules.Tint(NpcRole.Gang, a, Color.black, false, false);
            Assert.Greater(gang.Cloth.r, gang.Cloth.b, "an Ember Court coat leans to the gang's colour");
            Assert.Greater(gang.Bronze.r, gang.Bronze.b, "the trim carries the gang colour");
            Assert.AreNotEqual(gang.Cloth, unknownGang.Cloth);
            InRange(gang.Cloth, "gang cloth");
            InRange(unknownGang.Cloth, "gang cloth without a gang");
            LiveCrowdTint elite = LiveCrowdRules.Tint(NpcRole.Enemy, a, Color.black, false, true);
            Assert.AreNotEqual(Color.black, elite.Glow, "a Shard-Touched elite glows");
            Assert.Greater(elite.Glow.b, elite.Glow.g, "restrained amethyst, not white");
            Assert.Less(elite.Glow.r + elite.Glow.g + elite.Glow.b, 2f, "restrained");
            Assert.AreEqual(Color.black, enemy.Glow);
            InRange(elite.Cloth, "elite cloth");
        }

        private static float[] Dist(params float[] d) { return d; }

        private static int Run(float[] distance, bool[] held, int budget, float radius, bool[] grant)
        {
            int count = distance.Length;
            return LiveCrowdRules.Select(count, distance, held, budget, radius, new int[count], new float[count], grant);
        }

        [Test]
        public void BudgetGrantsTheNearestInsideTheRadius()
        {
            float[] d = Dist(50f, 10f, 70f, 30f, 61f, 5f);
            bool[] held = new bool[6];
            bool[] grant = new bool[6];
            Assert.AreEqual(3, Run(d, held, 3, 60f, grant));
            CollectionAssert.AreEqual(new[] { false, true, false, true, false, true }, grant, "the three nearest");
            Assert.AreEqual(4, Run(d, held, 10, 60f, grant), "only four are inside the radius");
            CollectionAssert.AreEqual(new[] { true, true, false, true, false, true }, grant);
            Assert.AreEqual(0, Run(d, held, 0, 60f, grant), "budget zero grants nobody");
            CollectionAssert.AreEqual(new bool[6], grant);
        }

        [Test]
        public void HoldersKeepTheirBodyThroughTheHysteresisBand()
        {
            float[] d = Dist(65f, 70f, 61f);
            bool[] held = { true, true, false };
            bool[] grant = new bool[3];
            Assert.AreEqual(1, Run(d, held, 5, 60f, grant));
            Assert.IsTrue(grant[0], "a holder at 65 m keeps the body (release at 68 m)");
            Assert.IsFalse(grant[1], "a holder at 70 m gives it back");
            Assert.IsFalse(grant[2], "a newcomer at 61 m waits until it is inside 60 m");
        }

        [Test]
        public void ShrinkingTheBudgetReleasesTheFarthestHolders()
        {
            float[] d = Dist(10f, 40f, 20f, 30f);
            bool[] held = { true, true, true, true };
            bool[] grant = new bool[4];
            Assert.AreEqual(2, Run(d, held, 2, 60f, grant));
            CollectionAssert.AreEqual(new[] { true, false, true, false }, grant);
        }

        [Test]
        public void OneNewcomerSwapsInOnlyWhenClearlyNearer()
        {
            float[] d = Dist(30f, 40f, 12f, 36f);
            bool[] held = { true, true, false, false };
            bool[] grant = new bool[4];
            Assert.AreEqual(2, Run(d, held, 2, 60f, grant));
            CollectionAssert.AreEqual(new[] { true, false, true, false }, grant, "the 12 m newcomer replaces the 40 m holder; the 36 m one is not six metres nearer");
            held = new[] { true, true, false, false };
            d = Dist(30f, 40f, 35f, 38f);
            Assert.AreEqual(2, Run(d, held, 2, 60f, grant));
            CollectionAssert.AreEqual(new[] { true, true, false, false }, grant, "no swap for a few metres");
            d = Dist(30f, 40f, 12f, 1f);
            held = new[] { true, true, false, false };
            Run(d, held, 2, 60f, grant);
            Assert.AreEqual(1, Count(grant, 2, 3), "one swap per tick, the nearest newcomer");
            Assert.IsTrue(grant[3]);
        }

        private static int Count(bool[] grant, params int[] indices)
        {
            int n = 0;
            foreach (int i in indices) if (grant[i]) n++;
            return n;
        }

        [Test]
        public void BudgetIsStableAndSafeOnBadInput()
        {
            float[] d = Dist(10f, 20f, 30f);
            bool[] held = { true, true, true };
            bool[] grant = new bool[3];
            Assert.AreEqual(3, Run(d, held, 3, 60f, grant));
            CollectionAssert.AreEqual(new[] { true, true, true }, grant, "nothing changes while nothing moves");
            Assert.AreEqual(2, Run(Dist(float.NaN, 5f, float.PositiveInfinity, 7f), new bool[4], 3, 60f, new bool[4]), "nonfinite distances count as far away");
            Assert.AreEqual(0, LiveCrowdRules.Select(0, d, held, 3, 60f, new int[3], new float[3], grant));
            Assert.AreEqual(0, LiveCrowdRules.Select(3, d, held, 3, 60f, new int[2], new float[3], grant), "short scratch arrays grant nothing instead of throwing");
            Assert.AreEqual(0, LiveCrowdRules.Select(3, null, held, 3, 60f, new int[3], new float[3], grant));
            Assert.AreEqual(0, Run(d, held, -2, 60f, grant), "a negative budget counts as zero");
        }

        [Test]
        public void PerFrameCrowdCodeAllocatesNothing()
        {
            foreach (string name in new[] { "TickIfDue", "Eye" })
                NoAllocation(typeof(LiveCrowd).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic), name);
            NoAllocation(typeof(LiveCrowdRules).GetMethod("Select", BindingFlags.Static | BindingFlags.Public), "Select");
            foreach (string name in new[] { "CrowdDistance", "CrowdGrant", "get_CrowdBound" })
                NoAllocation(typeof(LiveCastVisual).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic), name);
        }

        private static void NoAllocation(MethodInfo method, string name)
        {
            Assert.That(method, Is.Not.Null, name);
            foreach (OpCode code in LiveCastRulesTests.Codes(method))
                Assert.That(code != OpCodes.Newobj && code != OpCodes.Newarr && code != OpCodes.Box, name + " allocates (" + code.Name + ")");
            foreach (MethodBase called in LiveCastRulesTests.Calls(method))
                Assert.That(!(called.DeclaringType == typeof(string) && (called.Name == "Concat" || called.Name == "Format")), name + " builds a string every frame");
        }

        [Test]
        public void CrowdRowsGoLiveUnderTheContractAndRefuseWrongShapes()
        {
            string reason;
            Assert.IsTrue(LiveCastRules.CanGoLive(Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles), out reason), reason);
            Assert.IsTrue(LiveCastRules.CanGoLive(Crowd("guild-artisan-h8r", true, LiveCrowdRules.RoleBit(NpcRole.Civilian) | LiveCrowdRules.RoleBit(NpcRole.Clerk)), out reason), reason);
            LiveCastRow row = Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles);
            row.CastId = CastIds04E.Dante;
            Assert.IsFalse(LiveCastRules.CanGoLive(row, out reason), "never cast-bound");
            row = Crowd("court-defender-h8r", false, 0);
            Assert.IsFalse(LiveCastRules.CanGoLive(row, out reason), "a crowd identity needs roles");
            row = Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles | LiveCrowdRules.RoleBit(NpcRole.Companion));
            Assert.IsFalse(LiveCastRules.CanGoLive(row, out reason), "companions are never crowd");
            LiveCastRow named = LiveCastRulesTests.Malik();
            named.CrowdRoles = LiveCrowdRules.CrowdRoles;
            Assert.IsFalse(LiveCastRules.CanGoLive(named, out reason), "a named identity cannot serve roles");
            Assert.IsNotEmpty(reason);
            row = Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles);
            row.Model.ArtProof.OwnerApproved = false;
            Assert.IsFalse(LiveCastRules.CanGoLive(row, out reason), "the owner direction is recorded on the row or it stays procedural");
        }
    }
}
