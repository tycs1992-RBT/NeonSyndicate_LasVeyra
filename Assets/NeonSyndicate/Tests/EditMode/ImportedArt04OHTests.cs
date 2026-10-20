using System;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class ImportedArt04OHTests
    {
        private static ImportedArtRecord04OH Proof()
        {
            return new ImportedArtRecord04OH{AssetId="test-candidate",Creator="Test creator",LicenseId="Test license",RightsRecordId="private-record-001",CommercialUse=true,OwnerApproved=true,
                ExportAssetPath="Assets/TestModel.fbx",ExportSha256=new string('a',64),VisualDependencyHash=new string('b',32)};
        }
        [TestCase(typeof(Transform))] [TestCase(typeof(MeshFilter))] [TestCase(typeof(MeshRenderer))]
        [TestCase(typeof(SkinnedMeshRenderer))] [TestCase(typeof(Animator))] [TestCase(typeof(LODGroup))]
        public void OnlyInertPresentationComponentsAreAdmitted(Type type)
        {Assert.That(ImportedArt04OH.VisualComponent(type),Is.True);}
        [TestCase(typeof(Animation))] [TestCase(typeof(Rigidbody))] [TestCase(typeof(MeshCollider))]
        [TestCase(typeof(CharacterController))] [TestCase(typeof(Camera))] [TestCase(typeof(AudioSource))]
        [TestCase(typeof(Light))] [TestCase(typeof(ParticleSystem))] [TestCase(typeof(ImportedVisual04OD))] [TestCase(null)]
        public void AdditionalBoneWritersAndActiveComponentsAreRejectedBeforeCloning(Type type)
        {Assert.That(ImportedArt04OH.VisualComponent(type),Is.False);}
        [Test] public void UnknownRightsAndUnapprovedAppearanceRemainDisabled()
        {
            string reason;ImportedArtRecord04OH proof=new ImportedArtRecord04OH();
            Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);
            proof=Proof();proof.OwnerApproved=false;Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);
            proof.OwnerApproved=true;proof.CommercialUse=false;Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);
        }
        [Test] public void PrivateLicensedCandidateDoesNotRequirePublicRawArtRedistribution()
        {
            string reason;ImportedArtRecord04OH proof=Proof();proof.PublicSourceApproved=false;
            Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.True);
        }
        [Test] public void DiagnosticCannotBecomeMalikByCheckingAnEnableBox()
        {
            string reason;ImportedArtRecord04OH proof=Proof();proof.Diagnostic=true;
            Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);
            StringAssert.Contains("Diagnostic",reason);
        }
        [TestCase(ImportedAnimationAuthority04OH.AuthoredClips)] [TestCase((ImportedAnimationAuthority04OH)99)]
        public void UnsupportedBoneAuthorityRetainsOriginalFallback(ImportedAnimationAuthority04OH authority)
        {string reason;Assert.That(ImportedArt04OH.CanActivate(Proof(),authority,out reason),Is.False);}
        [Test] public void ExistingRowsKeepTheirOriginalAdapterContract()
        {string reason;Assert.That(ImportedArt04OH.CanActivate(null,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.True);}
        [TestCase("")] [TestCase("Assets/../Private.fbx")] [TestCase("C:/Private.fbx")] [TestCase("/Private.fbx")]
        public void ProofCannotPointOutsideTheProject(string path)
        {string reason;ImportedArtRecord04OH proof=Proof();proof.ExportAssetPath=path;Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);}
        [Test] public void ChangedExportOrMaterialDependencyInvalidatesItsAudit()
        {
            ImportedArtRecord04OH proof=Proof();Assert.That(ImportedArt04OH.FingerprintMatches(proof,new string('A',64),new string('B',32)),Is.True);
            Assert.That(ImportedArt04OH.FingerprintMatches(proof,new string('c',64),proof.VisualDependencyHash),Is.False);
            Assert.That(ImportedArt04OH.FingerprintMatches(proof,proof.ExportSha256,new string('c',32)),Is.False);
            Assert.That(ImportedArt04OH.FingerprintMatches(proof,"",proof.VisualDependencyHash),Is.False);
            proof.ExportSha256=new string('z',64);string reason;Assert.That(ImportedArt04OH.CanActivate(proof,ImportedAnimationAuthority04OH.ExistingPose,out reason),Is.False);
        }
    }
}
