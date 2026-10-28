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
    }
}
