using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E second review fixes around saving: an Underheart rest point is the resume point for saves made
    /// underground only (a save made on the surface stores a surface safehouse), the appended surface safehouse fields,
    /// old saves, the repair for surface saves an earlier 04E build wrote with a rest point, and a checkpoint autosave
    /// that only counts when the file was really written. All engine-free except the one JsonUtility test.
    /// </summary>
    public sealed class Safehouse04ETests
    {
        private static readonly Vector3 Lofts = new Vector3(-13.85f, 0.15f, -62.4f);
        private static readonly Vector3 OtherSafehouse = new Vector3(240f, 0.15f, 118f);
        private static readonly Vector3 RefugeRest = new Vector3(38f, -18f, -70f);
        private static readonly Vector3 GarageBay = new Vector3(610f, 0.15f, 540f);
        private static readonly Vector3 CanalsSpot = new Vector3(52f, -26f, -88f);

        // ================================================================== what a save stores

        [Test]
        public void ASurfaceSaveAfterARestStoresTheSurfaceSafehouse()
        {
            bool at;
            Vector3 pos = SafehouseRules04E.SavePosition(GarageBay, false, RefugeRest, Lofts, out at);
            Assert.That(pos, Is.EqualTo(Lofts), "the review's scenario: the bay checkpoints after resting in the refuge");
            Assert.That(at, Is.True, "it is still a safehouse save, so later saves keep returning there");
            pos = SafehouseRules04E.SavePosition(GarageBay, false, RefugeRest, OtherSafehouse, out at);
            Assert.That(pos, Is.EqualTo(OtherSafehouse), "the last surface safehouse chosen, not always the Lofts");
        }

        [Test]
        public void AnUndergroundSaveKeepsTheRestPoint()
        {
            bool at;
            Vector3 pos = SafehouseRules04E.SavePosition(CanalsSpot, true, RefugeRest, Lofts, out at);
            Assert.That(pos, Is.EqualTo(RefugeRest));
            Assert.That(at, Is.True);
            // Underground with only a surface safehouse: unchanged (the Underheart load path resumes at the saved anchor).
            pos = SafehouseRules04E.SavePosition(CanalsSpot, true, Lofts, Lofts, out at);
            Assert.That(pos, Is.EqualTo(Lofts));
            Assert.That(at, Is.True);
        }

        [Test]
        public void SavesWithoutAnUndergroundSafehouseAreUnchanged()
        {
            bool at;
            Assert.That(SafehouseRules04E.SavePosition(GarageBay, false, Vector3.zero, Vector3.zero, out at), Is.EqualTo(GarageBay), "no safehouse yet: the player's spot");
            Assert.That(at, Is.False);
            Assert.That(SafehouseRules04E.SavePosition(CanalsSpot, true, Vector3.zero, Lofts, out at), Is.EqualTo(CanalsSpot));
            Assert.That(at, Is.False);
            Assert.That(SafehouseRules04E.SavePosition(GarageBay, false, OtherSafehouse, Lofts, out at), Is.EqualTo(OtherSafehouse), "a surface safehouse, as before");
            Assert.That(at, Is.True);
        }

        [Test]
        public void ASurfaceSaveNeverStoresARestPointEvenWithoutASurfaceSafehouse()
        {
            bool at;
            // The caller passes the home spawn when no surface safehouse was chosen; without even that, the player's spot.
            Vector3 pos = SafehouseRules04E.SavePosition(GarageBay, false, RefugeRest, Vector3.zero, out at);
            Assert.That(pos, Is.EqualTo(GarageBay));
            Assert.That(at, Is.False);
            pos = SafehouseRules04E.SavePosition(GarageBay, false, RefugeRest, new Vector3(1f, -20f, 1f), out at);
            Assert.That(pos, Is.EqualTo(GarageBay), "an underground point is never accepted as the surface safehouse");
            Assert.That(at, Is.False);
            pos = SafehouseRules04E.SavePosition(GarageBay, false, new Vector3(float.NaN, 0f, 0f), Lofts, out at);
            Assert.That(pos, Is.EqualTo(GarageBay), "a broken safehouse is ignored");
            Assert.That(at, Is.False);
        }

        // ================================================================== loading

        [Test]
        public void LoadingKeepsTheSurfaceSafehouse()
        {
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(OtherSafehouse, true, Lofts), Is.EqualTo(OtherSafehouse), "the loaded surface safehouse is the newest");
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(RefugeRest, true, Lofts), Is.EqualTo(Lofts), "an underground save keeps the recorded surface safehouse");
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(Vector3.zero, true, Lofts), Is.EqualTo(Lofts));
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(OtherSafehouse, false, Vector3.zero), Is.EqualTo(OtherSafehouse), "an older save without the fields");
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(RefugeRest, false, Vector3.zero), Is.EqualTo(Vector3.zero), "an older save after a rest: surface saves use the home spawn");
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(Vector3.zero, true, RefugeRest), Is.EqualTo(Vector3.zero), "a recorded point underground is refused");
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(Vector3.zero, false, Lofts), Is.EqualTo(Vector3.zero), "the fields count only when recorded");
        }

        [Test]
        public void OnlyASurfaceSaveHoldingARestPointIsRepaired()
        {
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(true, RefugeRest, ""), Is.True, "an earlier 04E build's surface save");
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(true, RefugeRest, null), Is.True);
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(true, RefugeRest, UnderheartIds.EmberCanals), Is.False, "made underground: the Underheart resumes it");
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(true, Lofts, ""), Is.False, "a surface safehouse");
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(false, CanalsSpot, ""), Is.False, "not a safehouse save");
            Assert.That(SafehouseRules04E.SurfaceSaveHeldRestPoint(true, new Vector3(float.NaN, -20f, 0f), ""), Is.False);
        }

        [Test]
        public void TheReviewRouteNeverLoadsUndergroundFromTheSurface()
        {
            // Free Roam start at the Lofts, rest in the Lantern Refuge, walk up to Jax's bay and autosave there.
            Vector3 last = Lofts, surface = Lofts;
            last = RefugeRest;
            bool at;
            Vector3 saved = SafehouseRules04E.SavePosition(GarageBay, false, last, surface, out at);
            Assert.That(SafehouseRules04E.IsSurface(saved), Is.True, "the bay checkpoint saves a surface point");
            // Loading it: the Underheart does not move a surface position, so the loaded safehouse is the saved one.
            last = SafehouseRules04E.LoadedSafehouse(saved, saved, false);
            surface = SafehouseRules04E.LoadedSurfaceSafehouse(last, true, Lofts);
            Assert.That(last, Is.EqualTo(Lofts));
            Assert.That(surface, Is.EqualTo(Lofts));
            // Back down, rest again and save underground: the rest point. The Underheart resumes it at a verified anchor.
            last = RefugeRest;
            saved = SafehouseRules04E.SavePosition(CanalsSpot, true, last, surface, out at);
            Assert.That(saved, Is.EqualTo(RefugeRest));
            Vector3 anchor = RefugeRest + new Vector3(0f, 0.05f, 0f);
            last = SafehouseRules04E.LoadedSafehouse(saved, anchor, true);
            surface = SafehouseRules04E.LoadedSurfaceSafehouse(last, true, Lofts);
            Assert.That(last, Is.EqualTo(anchor));
            Assert.That(surface, Is.EqualTo(Lofts), "the surface safehouse survives the underground save");
            // Walk up and save again: the surface safehouse, never the rest point.
            saved = SafehouseRules04E.SavePosition(GarageBay, false, last, surface, out at);
            Assert.That(saved, Is.EqualTo(Lofts));
        }

        // ================================================================== save data

        /// <summary>The fields this pass appended to SaveData (also belongs in SaveDataTests.SaveDataFields).</summary>
        private static readonly string[] AppendedSaveDataFields =
        {
            "HasSurfaceSafehouse:Boolean", "SurfaceSafeX:Single", "SurfaceSafeY:Single", "SurfaceSafeZ:Single"
        };

        [Test]
        public void SurfaceSafehouseFieldsAreFrozen()
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(SaveData).GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = f.FieldType.Name;
            foreach (string entry in AppendedSaveDataFields)
            {
                string[] parts = entry.Split(':');
                string type;
                Assert.That(now.TryGetValue(parts[0], out type), Is.True, "SaveData." + parts[0] + " was renamed or removed; only append fields");
                Assert.That(type, Is.EqualTo(parts[1]), "SaveData." + parts[0] + " changed type");
            }
            SaveData fresh = new SaveData();
            Assert.That(fresh.HasSurfaceSafehouse, Is.False, "a save that never recorded one has none");
            Assert.That(fresh.SurfaceSafeX, Is.EqualTo(0f));
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void OldSavesLoadWithoutASurfaceSafehouse()
        {
            SaveData d = JsonUtility.FromJson<SaveData>("{\"Version\":1,\"AtSafehouse\":true,\"PosX\":-13.85,\"PosY\":0.15,\"PosZ\":-62.4}");
            Assert.That(d.HasSurfaceSafehouse, Is.False);
            Vector3 loaded = SafehouseRules04E.LoadedSafehouse(new Vector3(d.PosX, d.PosY, d.PosZ), new Vector3(d.PosX, d.PosY, d.PosZ), false);
            Assert.That(SafehouseRules04E.LoadedSurfaceSafehouse(loaded, d.HasSurfaceSafehouse, Vector3.zero), Is.EqualTo(loaded), "its own surface safehouse");
        }

        // ================================================================== checkpoint autosave

        [Test]
        public void ACheckpointSaveCountsOnlyWhenTheFileWasWritten()
        {
            Assert.That(CheckpointSaveRules04E.Saved(true, null), Is.True);
            Assert.That(CheckpointSaveRules04E.Saved(true, ""), Is.True);
            Assert.That(CheckpointSaveRules04E.Saved(true, "The save could not be written (WriteFailed). Your previous save is still on disk."), Is.False, "captured but not written");
            Assert.That(CheckpointSaveRules04E.Saved(false, null), Is.False, "the autosave never ran (throttle, menu, transition)");
            Assert.That(QuestRuntimeRules04E.CheckpointNeedsSecondAsk(CheckpointSaveRules04E.Saved(true, "disk full")), Is.True, "a failed write asks once more");
            Assert.That(QuestRuntimeRules04E.CheckpointNeedsSecondAsk(CheckpointSaveRules04E.Saved(true, null)), Is.False, "a written checkpoint saves once");
        }
    }
}
