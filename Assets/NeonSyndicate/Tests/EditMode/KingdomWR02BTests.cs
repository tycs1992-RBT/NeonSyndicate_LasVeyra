using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class KingdomWR02BTests
    {
        private static BasiliskNativeReviewWR02B Approved()
        { return new BasiliskNativeReviewWR02B { sourceFingerprint = new string('a', 64), assetFingerprint = new string('b', 64), bodyFingerprint = new string('c', 64), unityVersion = "6000.3.16f1", scope = "ACTUAL NATIVE ISOLATED MOTION REVIEW", imported = true, bindValidated = true, idle = true, walk = true, turning = true, attack = true, impact = true, death = true, feet = true, skin = true, materials = true, ownerMotionApproved = true }; }
        [Test] public void NoNativeEvidenceOrAnotherBodyCanEnableReview()
        {
            BasiliskNativeReviewWR02B empty = new BasiliskNativeReviewWR02B(); Assert.That(empty.AllowsReview(new string('a', 64), new string('b', 64), new string('c', 64)), Is.False);
            BasiliskNativeReviewWR02B review = Approved(); Assert.That(review.AllowsReview(review.sourceFingerprint, review.assetFingerprint, review.bodyFingerprint), Is.True);
            Assert.That(review.AllowsReview(new string('d', 64), review.assetFingerprint, review.bodyFingerprint), Is.False); Assert.That(review.AllowsReview(review.sourceFingerprint, new string('d', 64), review.bodyFingerprint), Is.False); Assert.That(review.AllowsReview(review.sourceFingerprint, review.assetFingerprint, new string('d', 64)), Is.False);
            review.unityVersion = "metadata-only"; Assert.That(review.AllowsReview(review.sourceFingerprint, review.assetFingerprint, review.bodyFingerprint), Is.False);
        }
        [TestCase("imported")] [TestCase("bindValidated")] [TestCase("idle")] [TestCase("walk")] [TestCase("turning")] [TestCase("attack")] [TestCase("impact")] [TestCase("death")] [TestCase("feet")] [TestCase("skin")] [TestCase("materials")] [TestCase("ownerMotionApproved")]
        public void EveryNativeMotionAndOwnerGateIsRequired(string field)
        { BasiliskNativeReviewWR02B review = Approved(); typeof(BasiliskNativeReviewWR02B).GetField(field).SetValue(review, false); Assert.That(review.AllowsReview(review.sourceFingerprint, review.assetFingerprint, review.bodyFingerprint), Is.False); }
        [Test] public void CompleteHierarchyAndOnlyThatSpeciesAreAdmitted()
        {
            CreatureSkeleton skeleton = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0).Skeleton;
            List<ImportedBone04OD> map = new List<ImportedBone04OD>(); for (int i = 0; i < skeleton.Count; i++) map.Add(new ImportedBone04OD { SourceBone = i, TargetPath = "Armature/" + skeleton.Names[i] });
            string reason; Assert.That(BasiliskSkinRulesWR02B.Mapping(BasiliskSkinRulesWR02B.SpeciesId, skeleton, map, skeleton.Names, skeleton.Parents, out reason), Is.True, reason);
            Assert.That(BasiliskSkinRulesWR02B.Mapping("azure_prowler", skeleton, map, skeleton.Names, skeleton.Parents, out reason), Is.False);
            int[] parents = skeleton.Parents.ToArray(); parents[4] = 4; Assert.That(BasiliskSkinRulesWR02B.Mapping(BasiliskSkinRulesWR02B.SpeciesId, skeleton, map, skeleton.Names, parents, out reason), Is.False);
            map.RemoveAt(3); Assert.That(BasiliskSkinRulesWR02B.Mapping(BasiliskSkinRulesWR02B.SpeciesId, skeleton, map, skeleton.Names, skeleton.Parents, out reason), Is.False);
        }
        [TestCase(0, true)] [TestCase(1, false)] [TestCase(3, false)]
        public void EvenCosmeticPartBreaksRestoreOriginalPresentation(int broken, bool expected)
        { Assert.That(BasiliskSkinRulesWR02B.KeepCandidate(true, true, false, false, PartEffect.None, broken), Is.EqualTo(expected)); }
        [Test] public void RagdollAndLostOwnersCannotLeaveAnImportedSkinActive()
        { Assert.That(BasiliskSkinRulesWR02B.KeepCandidate(false, true, false, false, PartEffect.None), Is.False); Assert.That(BasiliskSkinRulesWR02B.KeepCandidate(true, true, true, false, PartEffect.None), Is.False); }
        [Test] public void DissolvePreservesImportedBindScale()
        { Assert.That(BasiliskSkinRulesWR02B.LocalScale(new Vector3(.01f, .02f, .03f), Vector3.one * .5f), Is.EqualTo(new Vector3(.005f, .01f, .015f))); }
        [TestCase(0, "SanctuaryCedar")] [TestCase(1, "AzureElder")] [TestCase(2, "")] [TestCase(-1, "")]
        public void RegionIdentityIsRetained(int region, string expected)
        { Assert.That(HeartPlanFR01.CompositionCanopyWR02B(region), Is.EqualTo(expected)); }
        [TestCase(false, true, false, false, true, false)] [TestCase(true, false, false, false, true, false)] [TestCase(true, true, true, false, true, false)] [TestCase(true, true, false, true, true, false)] [TestCase(true, true, false, false, false, false)] [TestCase(true, true, false, false, true, true)]
        public void PreviewIsExplicitAndReversible(bool session, bool selected, bool compare, bool suspended, bool prepared, bool expected)
        { Assert.That(HeartPlanFR01.CompositionPolicyWR02B(session, selected, compare, suspended, prepared), Is.EqualTo(expected)); }
        [TestCase(true, true, true, true, true)] [TestCase(true, true, false, true, false)] [TestCase(true, true, true, false, false)] [TestCase(true, false, true, true, false)] [TestCase(false, true, true, true, false)]
        public void LostRootsOrAdmissionCannotLeaveStaleActiveProof(bool selected, bool admitted, bool present, bool hierarchy, bool expected)
        { Assert.That(HeartPlanFR01.CompositionObservedWR02B(selected, admitted, present, hierarchy), Is.EqualTo(expected)); }
        [Test] public void MissingWorldAndNewPhysicsStayPreserved()
        {
            Assert.That(HeartPlanFR01.CompositionProposalsWR02B(null), Is.Empty);
            CityLayout layout = new CityLayout(61724); string reason;
            Assert.That(HeartPlanFR01.CompositionClearWR02B(layout, new HeartCompositionPlacementWR02B { nativePhysicsRequired = true, position = Vector3.zero, size = Vector3.one }, null, out reason), Is.False);
        }
        [Test] public void CapturedProofDoesNotMutateOnRollback()
        { FantasyCompositionRecordWR02B original = new FantasyCompositionRecordWR02B { active = true, stableId = "original" }; FantasyCompositionRecordWR02B copy = original.Copy(); original.active = false; original.reason = "later refusal"; Assert.That(copy.active, Is.True); Assert.That(copy.reason, Is.Empty); }
        [Test] public void NativeBodyReceiptIsIndependentOfEditorLocale()
        {
            System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                CreatureBody body = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0);
                string a = ImportedVisual04OD.BasiliskBodyFingerprintWR02B(body);
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                Assert.That(ImportedVisual04OD.BasiliskBodyFingerprintWR02B(body), Is.EqualTo(a));
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
        }
        [Test] public void SourceBodyReceiptsAreDeterministicAndVariantSpecific()
        {
            CreatureBody a = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0), b = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61725), 0);
            string fingerprint = ImportedVisual04OD.BasiliskBodyFingerprintWR02B(a); Assert.That(fingerprint.Length, Is.EqualTo(64)); Assert.That(ImportedVisual04OD.BasiliskBodyFingerprintWR02B(a), Is.EqualTo(fingerprint)); Assert.That(ImportedVisual04OD.BasiliskBodyFingerprintWR02B(b), Is.Not.EqualTo(fingerprint));
        }

        [Test] public void FantasyCircuitUsesOriginalRealmAnchors()
        {
            CityLayout layout = new CityLayout(61724);
            layout.Generate();
            List<VenueBinding04C> venues = InhabitedCityBindings04C.Select(layout, InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(layout.ApplyProfile04O(CityRevision04O.GothicCivicH5, venues), Is.True);
            CityLayout previous = Game.Layout;
            try
            {
                Game.Layout = layout;
                Vector3 oldTarget; float yaw; string reason;
                Assert.That(CampaignPlacesRuntime04E.TryResolve("Realm:azure", out oldTarget, out yaw, out reason), Is.False);
                foreach (string token in new[] { "Realm:threshold", "Realm:grotto", "Realm:azure" })
                {
                    Vector3 expected, actual;
                    Assert.That(layout.ResolveRealmAnchor04OC(token, out expected), Is.True, token);
                    Assert.That(VisibleRealityV0.TryResolveWorldRewriteRouteWR02C(token, out actual), Is.True, token);
                    Assert.That(actual, Is.EqualTo(expected), token);
                }
                Vector3 missing;
                Assert.That(VisibleRealityV0.TryResolveWorldRewriteRouteWR02C("Realm:unknown", out missing), Is.False);
                Assert.That(VisibleRealityV0.TryResolveWorldRewriteRouteWR02C(null, out missing), Is.False);
                Game.Layout = null;
                Assert.That(VisibleRealityV0.TryResolveWorldRewriteRouteWR02C("Realm:azure", out missing), Is.False);
            }
            finally { Game.Layout = previous; }
        }

        [TestCase("interior_floor", 16f, .212f, 18f)]
        [TestCase("west_room_wall", .3f, 5f, 18f)]
        [TestCase("east_room_wall", .3f, 5f, 18f)]
        [TestCase("rear_room_wall", 16f, 5f, .3f)]
        public void ForgeSurfaceSectionsExactlyFillTheExistingSolid(string id, float x, float y, float z)
        {
            Vector3 size = new Vector3(x, y, z);
            List<ForgeSurfaceSectionWR02C3> sections = HeartPlanFR01.ForgeSurfacePlanWR02C3(id, size);
            Assert.That(sections.Count, Is.GreaterThan(1));
            float volume = 0f;
            for (int i = 0; i < sections.Count; i++)
            {
                ForgeSurfaceSectionWR02C3 a = sections[i];
                Vector3 min = a.Center - a.Size * .5f, max = a.Center + a.Size * .5f;
                Assert.That(a.Size.x, Is.GreaterThan(0)); Assert.That(a.Size.y, Is.GreaterThan(0)); Assert.That(a.Size.z, Is.GreaterThan(0));
                Assert.That(min.x, Is.GreaterThanOrEqualTo(-x * .5f - .0001f)); Assert.That(max.x, Is.LessThanOrEqualTo(x * .5f + .0001f));
                Assert.That(min.y, Is.GreaterThanOrEqualTo(-y * .5f - .0001f)); Assert.That(max.y, Is.LessThanOrEqualTo(y * .5f + .0001f));
                Assert.That(min.z, Is.GreaterThanOrEqualTo(-z * .5f - .0001f)); Assert.That(max.z, Is.LessThanOrEqualTo(z * .5f + .0001f));
                volume += a.Size.x * a.Size.y * a.Size.z;
                for (int j = 0; j < i; j++)
                {
                    ForgeSurfaceSectionWR02C3 b = sections[j]; Vector3 delta = a.Center - b.Center;
                    bool overlap = Mathf.Abs(delta.x) < (a.Size.x + b.Size.x) * .5f - .0001f &&
                        Mathf.Abs(delta.y) < (a.Size.y + b.Size.y) * .5f - .0001f && Mathf.Abs(delta.z) < (a.Size.z + b.Size.z) * .5f - .0001f;
                    Assert.That(overlap, Is.False, "Sections overlap in " + id);
                }
            }
            Assert.That(volume, Is.EqualTo(x * y * z).Within(.001f));
            Assert.That(HeartPlanFR01.ForgeSurfacePlanWR02C3(id, size + Vector3.right), Is.Empty);
        }

        [Test] public void ForgeFloorFramesTheExistingDoorAndServiceRatherThanMovingThem()
        {
            List<ForgeSurfaceSectionWR02C3> floor = HeartPlanFR01.ForgeSurfacePlanWR02C3("interior_floor", new Vector3(16f, .212f, 18f));
            Assert.That(ForgeFloorSurface(floor, new Vector3(0f, 0f, -6f)), Is.EqualTo("CarvedTrim"));
            Assert.That(ForgeFloorSurface(floor, new Vector3(-4.5f, 0f, -.8f)), Is.EqualTo("CarvedTrim"));
            Assert.That(ForgeFloorSurface(floor, new Vector3(4f, 0f, -.8f)), Is.EqualTo("Masonry"));
            Assert.That(HeartPlanFR01.ForgeSurfaceSourceWR02C3("interior_floor", new Vector3(0f, -.094f, 9f), new Vector3(16f, .212f, 18f), Vector3.zero), Is.True);
            Assert.That(HeartPlanFR01.ForgeSurfaceSourceWR02C3("interior_floor", new Vector3(0f, -.094f, 9f), new Vector3(16f, .212f, 18f), Vector3.up), Is.False);
        }

        private static string ForgeFloorSurface(List<ForgeSurfaceSectionWR02C3> sections, Vector3 point)
        {
            foreach (ForgeSurfaceSectionWR02C3 section in sections)
                if (Mathf.Abs(point.x - section.Center.x) < section.Size.x * .5f && Mathf.Abs(point.z - section.Center.z) < section.Size.z * .5f)
                    return section.Surface;
            return "";
        }

#if UNITY_EDITOR
        [Test, Category("NeedsEngine")]
        public void DistrictPreparationDoesNotReuseAStaleBrokenPrefab()
        {
            Type setup = Type.GetType("NeonSyndicate.Editor.HeartSetupFR01, NeonSyndicate.Editor");
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("Prepare", new[] { typeof(bool) }).Invoke(null, new object[] { false });
            System.Reflection.MethodInfo ready = setup.GetMethod("PreparedSurfacesReady", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.That(ready, Is.Not.Null);
            Material material = Resources.Load<Material>("HeartPreparedFR01/Masonry");
            Assert.That(material, Is.Not.Null);
            MeshBuilder builder = new MeshBuilder(1); builder.Box(0, Vector3.zero, Vector3.one, UVMode.World);
            Mesh mesh = builder.ToMesh("WR02C3 stale cache test");
            GameObject prefab = new GameObject("stale prefab");
            HeartCatalogFR01 catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>();
            try
            {
                MeshFilter filter = prefab.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                catalog.Modules = new[] { new HeartModuleFR01 { Id = "test", Lods = new[] { prefab } } };
                Assert.That(ready.Invoke(null, new object[] { catalog }), Is.False, "A missing renderer must rebuild the cache.");
                MeshRenderer renderer = prefab.AddComponent<MeshRenderer>(); renderer.sharedMaterials = new Material[0];
                Assert.That(ready.Invoke(null, new object[] { catalog }), Is.False, "Missing material slots must rebuild the cache.");
                renderer.sharedMaterial = material;
                Assert.That(ready.Invoke(null, new object[] { catalog }), Is.True);
                filter.sharedMesh = null;
                Assert.That(ready.Invoke(null, new object[] { catalog }), Is.False, "A missing mesh must rebuild the cache.");
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test, Category("NeedsEngine")]
        public void ForgeWorkroomRestoresTheOriginalIfItsCandidateLosesMaterial()
        {
            Type setup = Type.GetType("NeonSyndicate.Editor.HeartSetupFR01, NeonSyndicate.Editor");
            Assert.That(setup, Is.Not.Null);
            setup.GetMethod("Prepare", new[] { typeof(bool) }).Invoke(null, new object[] { false });
            Material material = Resources.Load<Material>("HeartPreparedFR01/Masonry");
            Assert.That(material, Is.Not.Null);
            MeshBuilder builder = new MeshBuilder(1); builder.Box(0, Vector3.zero, Vector3.one, UVMode.World);
            Mesh mesh = builder.ToMesh("WR02C3 transaction test");
            GameObject host = new GameObject("WR02C3 Forge transaction test");
            GameObject original = new GameObject("original"), candidate = new GameObject("candidate");
            original.transform.SetParent(host.transform, false); candidate.transform.SetParent(host.transform, false);
            original.AddComponent<MeshFilter>().sharedMesh = mesh; original.AddComponent<MeshRenderer>().sharedMaterial = material;
            candidate.AddComponent<MeshFilter>().sharedMesh = mesh; MeshRenderer candidateRenderer = candidate.AddComponent<MeshRenderer>(); candidateRenderer.sharedMaterial = material;
            candidate.SetActive(false);
            try
            {
                HeartPresentationFR01 heart = new HeartPresentationFR01(new CityLayout(61724), host.transform);
                string reason;
                Assert.That(heart.RegisterRewriteVisual("test:forge", "ForgeWorkroomWR02C3", original, candidate, "test", out reason), Is.True, reason);
                heart.State(false, false);
                heart.BeginFantasyPreviewV1();
                Assert.That(candidate.activeSelf, Is.True);
                Assert.That(original.activeSelf, Is.False);
                candidateRenderer.sharedMaterial = null;
                heart.FillRewriteProofWR01(new FantasyProofSnapshotV1(), null);
                Assert.That(candidate.activeSelf, Is.False);
                Assert.That(original.activeSelf, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test, Category("NeedsEngine")]
        public void DistrictProofNamesTheMissingCatalogOrInvalidModel()
        {
            System.Reflection.MethodInfo measure = typeof(HeartPresentationFR01).GetMethod("MeasureLayerV1", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.That(measure, Is.Not.Null);
            FantasyLayerProofV1 missing = (FantasyLayerProofV1)measure.Invoke(null, new object[] { "FR01", null, false, false, new List<GameObject>(), null });
            Assert.That(missing.prepared, Is.False);
            Assert.That(missing.firstRefusal, Does.Contain("FR01 prepared catalog missing"));

            HeartCatalogFR01 catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>();
            GameObject invalidModel = new GameObject("No mesh");
            try
            {
                GameObject[] lods = { invalidModel, invalidModel, invalidModel };
                catalog.Modules = new[] { new HeartModuleFR01 { Id = "HeartGate", Lods = lods }, new HeartModuleFR01 { Id = "HeartHouseA", Lods = lods }, new HeartModuleFR01 { Id = "HeartHouseB", Lods = lods } };
                Assert.That(catalog.Ready, Is.True);
                FantasyLayerProofV1 invalid = (FantasyLayerProofV1)measure.Invoke(null, new object[] { "FR01", catalog, false, false, new List<GameObject>(), null });
                Assert.That(invalid.prepared, Is.False);
                Assert.That(invalid.firstRefusal, Does.Contain("HeartGate LOD0"));
                Assert.That(invalid.firstRefusal, Does.Contain("no mesh"));
            }
            finally { UnityEngine.Object.DestroyImmediate(invalidModel); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test, Category("NeedsEngine")]
        public void PreparedFoundryArchiveUsesOriginalSharedSurfaces()
        {
            Type setup = Type.GetType("NeonSyndicate.Editor.HeartSetupFR01, NeonSyndicate.Editor");
            Assert.That(setup, Is.Not.Null, "The existing Editor preparation assembly must be loaded.");
            System.Reflection.MethodInfo prepare = setup.GetMethod("Prepare", new[] { typeof(bool) });
            System.Reflection.MethodInfo rewrite = setup.GetMethod("PrepareRewrite", new[] { typeof(bool) });
            System.Reflection.MethodInfo foundry = setup.GetMethod("PrepareFoundryWR02A", new[] { typeof(bool) });
            Assert.That(prepare, Is.Not.Null); Assert.That(rewrite, Is.Not.Null); Assert.That(foundry, Is.Not.Null);
            prepare.Invoke(null, new object[] { false });
            rewrite.Invoke(null, new object[] { false });
            foundry.Invoke(null, new object[] { false });

            HeartCatalogFR01 catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>("Assets/NeonSyndicate/Resources/HeartPreparedWR02A/Catalog.asset");
            Assert.That(catalog, Is.Not.Null, "Foundry preparation did not create its catalog.");
            HeartModuleFR01 archive = catalog.Find("WRArchiveCrown");
            Assert.That(archive, Is.Not.Null, catalog.Reason);
            Assert.That(archive.Lods, Has.Length.EqualTo(3));
            foreach (GameObject prefab in archive.Lods)
            {
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty, "The Archive candidate must remain visual only.");
                MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                Assert.That(renderers.Length, Is.GreaterThan(0));
                foreach (MeshRenderer renderer in renderers) foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(UnityEditor.AssetDatabase.GetAssetPath(material), Does.StartWith("Assets/NeonSyndicate/Resources/HeartPreparedFR01/"));
                    Assert.That(material.mainTexture, Is.Not.Null);
                    Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null);
                    Assert.That(material.GetTexture("_MetallicGlossMap"), Is.Not.Null);
                }
            }

            HeartCatalogFR01 original = Resources.Load<HeartCatalogFR01>("HeartPreparedWR01/Catalog");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.RewriteReady, Is.True, original.Reason);
            Assert.That(Resources.Load<HeartCatalogFR01>("HeartPreparedWR02A/Catalog"), Is.SameAs(catalog));
            GameObject host = new GameObject("WR02C2 selection test");
            GameObject savedLod = archive.Lods[1];
            try
            {
                HeartPresentationFR01 presentation = new HeartPresentationFR01(new CityLayout(61724), host.transform);
                System.Reflection.MethodInfo select = typeof(HeartPresentationFR01).GetMethod("CivicCatalogFoundryWR02A", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(select, Is.Not.Null);
                HeartRewritePlanWR01 plan = new HeartRewritePlanWR01 { Seed = 479130, Service = "archive", Family = "WRArchiveCrown", StableId = "revision:10/venue:archive/seed:479130/crown" };
                FantasyRewriteRecordWR01 record = new FantasyRewriteRecordWR01();
                Assert.That(select.Invoke(presentation, new object[] { plan, record }), Is.SameAs(catalog), record.assetVariant);

                archive.Lods[1] = null;
                record = new FantasyRewriteRecordWR01();
                Assert.That(select.Invoke(presentation, new object[] { plan, record }), Is.SameAs(original));
                Assert.That(record.assetVariant, Does.Contain("fallback"));
                Assert.That(record.assetVariant, Does.Contain("not passive"));
            }
            finally
            {
                archive.Lods[1] = savedLod;
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
#endif
    }
}
