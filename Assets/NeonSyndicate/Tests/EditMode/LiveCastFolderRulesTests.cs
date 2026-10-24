using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The drop-in naming rules (Docs/LIVE_CHARACTERS.md) and the rebuild fingerprint. Engine-free.</summary>
    public sealed class LiveCastFolderRulesTests
    {
        [Test]
        public void FolderNameIsTheCharacter()
        {
            foreach (string player in new[] { "Malik", "malik", "Malik Veyr", "Player", " Malik " })
                Assert.AreEqual("malik", LiveCastFolderRules.IdentityFromFolder(player), player);
            Assert.AreEqual("npc:Corin Pell", LiveCastFolderRules.IdentityFromFolder("Corin Pell"));
            Assert.AreEqual("npc:Corin Pell", LiveCastFolderRules.IdentityFromFolder("  Corin Pell "));
            Assert.AreEqual("", LiveCastFolderRules.IdentityFromFolder(""));
            Assert.IsTrue(LiveCastFolderRules.SkipFolder("Prepared"));
            Assert.IsTrue(LiveCastFolderRules.SkipFolder("_Example"));
            Assert.IsTrue(LiveCastFolderRules.SkipFolder(".hidden"));
            Assert.IsFalse(LiveCastFolderRules.SkipFolder("Corin Pell"));
        }

        [Test]
        public void CastNamesBindToTheExactCastActor()
        {
            foreach (CastDef04E cast in CastRoster04E.All)
                Assert.AreEqual(cast.Id, LiveCastFolderRules.CastIdFor("npc:" + cast.Name), cast.Name);
            Assert.AreEqual("dante", LiveCastFolderRules.CastIdFor(DanteReviewH8S.Identity));
            Assert.AreEqual("", LiveCastFolderRules.CastIdFor("npc:Somebody Else"));
            Assert.AreEqual("", LiveCastFolderRules.CastIdFor("malik"));
            Assert.AreEqual("", LiveCastFolderRules.CastIdFor(null));
        }

        [Test]
        public void ModelFilesNameTheirLevel()
        {
            string baseName;
            Assert.AreEqual(0, LiveCastFolderRules.LodOfModel("CorinPell.fbx", out baseName));
            Assert.AreEqual("CorinPell", baseName);
            Assert.AreEqual(0, LiveCastFolderRules.LodOfModel("CorinPell_LOD0.fbx", out baseName));
            Assert.AreEqual(1, LiveCastFolderRules.LodOfModel("CorinPell_LOD1.fbx", out baseName));
            Assert.AreEqual("CorinPell", baseName);
            Assert.AreEqual(2, LiveCastFolderRules.LodOfModel("corinpell_lod2.FBX", out baseName));
            Assert.AreEqual(-1, LiveCastFolderRules.LodOfModel("CorinPell_LOD3.fbx", out baseName));
            Assert.AreEqual(-1, LiveCastFolderRules.LodOfModel("CorinPell.obj", out baseName));
            Assert.AreEqual(-1, LiveCastFolderRules.LodOfModel(".fbx", out baseName));
            Assert.AreEqual(0, LiveCastFolderRules.LodOfModel("MalikVeyrH9.fbx", out baseName));
            Assert.AreEqual(2, LiveCastFolderRules.LodOfModel("MalikVeyrH9_LOD2.fbx", out baseName));
            Assert.AreEqual("MalikVeyrH9", baseName);
        }

        [Test]
        public void TexturesMatchMaterialsByName()
        {
            Assert.AreEqual(LiveCastFolderRules.MaterialKey("H6 Worn leather"), LiveCastFolderRules.MaterialKey("H6_Worn_leather"));
            Assert.AreEqual("mieyes", LiveCastFolderRules.MaterialKey("MI_Eyes.001"));
            Assert.AreEqual("skin", LiveCastFolderRules.MaterialKey("Skin (Instance)"));
            Assert.AreEqual("", LiveCastFolderRules.MaterialKey(null));
            Check("Corin_Skin_Albedo.png", "corinskin", LiveTextureRole.Albedo);
            Check("H6_Worn_leather_Normal.png", "h6wornleather", LiveTextureRole.Normal);
            Check("skin-normal.TGA", "skin", LiveTextureRole.Normal);
            Check("Armor Metallic.png", "armor", LiveTextureRole.Metallic);
            Check("Gem_Emission.jpg", "gem", LiveTextureRole.Emission);
            Check("Cloth_BaseColor.png", "cloth", LiveTextureRole.Albedo);
            Check("Hair_Diffuse.psd", "hair", LiveTextureRole.Albedo);
            string key;
            LiveTextureRole role;
            Assert.IsFalse(LiveCastFolderRules.TextureRole("T_Eye_Brown.png", out key, out role), "no role suffix: only usable when livecast.json names it");
            Assert.IsFalse(LiveCastFolderRules.TextureRole("_Albedo.png", out key, out role));
            Assert.IsFalse(LiveCastFolderRules.TextureRole("notes_Albedo.txt", out key, out role));
            Assert.IsTrue(LiveCastFolderRules.IsImage("T_Eye_Brown.png"));
            Assert.IsFalse(LiveCastFolderRules.IsImage("livecast.json"));
            Assert.IsFalse(LiveCastFolderRules.IsImage(".png"));
        }

        private static void Check(string file, string expectedKey, LiveTextureRole expectedRole)
        {
            string key;
            LiveTextureRole role;
            Assert.IsTrue(LiveCastFolderRules.TextureRole(file, out key, out role), file);
            Assert.AreEqual(expectedKey, key, file);
            Assert.AreEqual(expectedRole, role, file);
        }

        [Test]
        public void MaterialNamesGiveASensibleStartingLook()
        {
            Assert.AreEqual(LiveSurfaceMode.Transparent, LiveCastFolderRules.DefaultSurface("h6cornea").Mode);
            Assert.AreEqual(LiveSurfaceMode.Cutout, LiveCastFolderRules.DefaultSurface("h8rtexturedgroom").Mode);
            Assert.AreEqual(LiveSurfaceMode.Cutout, LiveCastFolderRules.DefaultSurface("eyelashes").Mode, "a lash is a cutout before it is an eye");
            Assert.AreEqual(LiveSurfaceMode.Opaque, LiveCastFolderRules.DefaultSurface("h6eyes").Mode);
            Assert.Greater(LiveCastFolderRules.DefaultSurface("h6eyes").Smoothness, 0.7f);
            Assert.Greater(LiveCastFolderRules.DefaultSurface("h6blackenedsteel").Metallic, 0.5f);
            Assert.AreEqual(0f, LiveCastFolderRules.DefaultSurface("h6charcoalwool").Metallic);
            Assert.AreEqual(LiveSurfaceMode.Opaque, LiveCastFolderRules.DefaultSurface("").Mode);
            LiveSurfaceMode mode;
            Assert.IsTrue(LiveCastFolderRules.ParseMode(" Cutout ", out mode));
            Assert.AreEqual(LiveSurfaceMode.Cutout, mode);
            Assert.IsTrue(LiveCastFolderRules.ParseMode("transparent", out mode));
            Assert.IsFalse(LiveCastFolderRules.ParseMode("glossy", out mode));
            Assert.IsFalse(LiveCastFolderRules.ParseMode(null, out mode));
        }

        [Test]
        public void MissingSocketsSitOnTheExporterBones()
        {
            Dictionary<string, string> expected = new Dictionary<string, string>
            {
                { "hand_r", "hand_r" }, { "hand_l", "hand_l" }, { "foregrip", "hand_l" }, { "muzzle", "hand_r" }, { "blade_hilt", "hand_r" },
                { "blade_tip", "hand_r" }, { "head", "Head" }, { "chest", "spine_03" }, { "fx", "spine_03" }, { "synergy", "spine_03" }
            };
            Assert.AreEqual(expected.Count, LiveCastFolderRules.SocketRoles.Count);
            foreach (string role in LiveCastFolderRules.SocketRoles) Assert.AreEqual(expected[role], LiveCastFolderRules.SocketBone(role), role);
            Assert.AreEqual("", LiveCastFolderRules.SocketBone("tail"));
        }

        [Test]
        public void FacingComesFromTheMarkerThenToesThenHands()
        {
            Assert.AreEqual(LiveFacing.AsIs, LiveCastFolderRules.Facing(true, 0.4f, 0f, true, -1f, 0f, 1f, -1f), "the marker wins over toes and hands");
            Assert.AreEqual(LiveFacing.TurnAround, LiveCastFolderRules.Facing(true, -0.4f, 0f, false, 0f, 0f, -1f, 1f));
            Assert.AreEqual(LiveFacing.Unknown, LiveCastFolderRules.Facing(true, 0f, 0f, true, 1f, 0f, -1f, 1f));
            Assert.AreEqual(LiveFacing.AsIs, LiveCastFolderRules.Facing(false, 0f, 0f, true, 0.12f, 0f, 1f, -1f), "toes forward beat the hands");
            Assert.AreEqual(LiveFacing.TurnAround, LiveCastFolderRules.Facing(false, 0f, 0f, true, -0.12f, 0f, -1f, 1f));
            Assert.AreEqual(LiveFacing.AsIs, LiveCastFolderRules.Facing(false, 0f, 0f, false, 0f, 0f, -0.8f, 0.8f));
            Assert.AreEqual(LiveFacing.TurnAround, LiveCastFolderRules.Facing(false, 0f, 0f, false, 0f, 0f, 0.8f, -0.8f));
            Assert.AreEqual(LiveFacing.Unknown, LiveCastFolderRules.Facing(false, 0f, 0f, true, 0.01f, 0f, 0.02f, -0.02f));
        }

        [Test]
        public void FingerprintChangesOnlyWhenTheArtChanges()
        {
            string[] paths = { "Assets/A/B.fbx", "Assets/A/Skin_Albedo.png", "import-settings" };
            string[] hashes = { "11", "22", "ready" };
            string stamp = LiveCastFolderRules.Stamp("livecast-1", false, paths, hashes);
            Assert.AreEqual(stamp, LiveCastFolderRules.Stamp("livecast-1", false, new[] { paths[2], paths[0], paths[1] }, new[] { hashes[2], hashes[0], hashes[1] }), "file order does not matter");
            Assert.AreNotEqual(stamp, LiveCastFolderRules.Stamp("livecast-1", false, paths, new[] { "11", "23", "ready" }), "a changed texture");
            Assert.AreNotEqual(stamp, LiveCastFolderRules.Stamp("livecast-1", false, paths, new[] { "11", "22", "pending" }), "reset import settings");
            Assert.AreNotEqual(stamp, LiveCastFolderRules.Stamp("livecast-1", true, paths, hashes), "a pipeline switch rebuilds the materials");
            Assert.AreNotEqual(stamp, LiveCastFolderRules.Stamp("livecast-2", false, paths, hashes), "a new recipe");
            Assert.AreNotEqual(stamp, LiveCastFolderRules.Stamp("livecast-1", false, new[] { paths[0], paths[1] }, new[] { hashes[0], hashes[1] }), "a removed file");
            Assert.Throws<ArgumentException>(() => LiveCastFolderRules.Stamp("x", false, paths, new[] { "1" }));
            Assert.IsFalse(LiveCastFolderRules.NeedsRebuild(stamp, stamp, true));
            Assert.IsTrue(LiveCastFolderRules.NeedsRebuild(stamp, stamp + "x", true));
            Assert.IsTrue(LiveCastFolderRules.NeedsRebuild(stamp, stamp, false), "a deleted prefab is prepared again");
            Assert.IsTrue(LiveCastFolderRules.NeedsRebuild("", stamp, true));
        }

        [Test]
        public void OnlySourceChangesWakeTheGenerator()
        {
            Assert.IsTrue(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/LiveCast/Corin Pell/CorinPell.fbx"));
            Assert.IsTrue(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/LiveCast/Corin Pell/livecast.json"));
            Assert.IsTrue(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/ProductionH9/Models/MalikVeyrH9.fbx"));
            Assert.IsFalse(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/LiveCast/Corin Pell/Prepared/CorinPellLive.prefab"), "the generator's own output");
            Assert.IsFalse(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/LiveCast/Corin Pell/Prepared"));
            Assert.IsFalse(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/ProductionH9/Prepared/H9Review.prefab"));
            Assert.IsFalse(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Art/ProductionH8S/Models/MalikVeyrH8S.fbx"));
            Assert.IsFalse(LiveCastFolderRules.WatchedSource("Assets/NeonSyndicate/Resources/LiveCast/LiveCastRegistry.asset"));
            Assert.IsFalse(LiveCastFolderRules.WatchedSource(null));
        }

        [Test]
        public void GeneratedNamesAndLodHeightsAreSafe()
        {
            Assert.AreEqual("CorinPell", LiveCastFolderRules.SafeName("Corin Pell"));
            Assert.AreEqual("Character", LiveCastFolderRules.SafeName("  !! "));
            foreach (int levels in new[] { 1, 2, 3 })
            {
                float[] cut = LiveCastFolderRules.LodCutoffs(levels);
                Assert.AreEqual(levels, cut.Length);
                for (int i = 1; i < cut.Length; i++) Assert.Less(cut[i], cut[i - 1]);
                Assert.AreEqual(0.02f, cut[cut.Length - 1], 1e-6f);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => LiveCastFolderRules.LodCutoffs(4));
        }
    }
}
