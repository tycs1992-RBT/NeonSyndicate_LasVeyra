using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class MalikReviewH8RTests
    {
        private static ImportedModel04OD Candidate()
        {
            ImportedBone04OD[] bones=new ImportedBone04OD[RigLayout.BoneCount];for(int i=0;i<bones.Length;i++)bones[i]=new ImportedBone04OD{SourceBone=i,TargetPath="Art/Root/bone"+i};
            string[] roles={"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"};ImportedSocket04OD[] sockets=new ImportedSocket04OD[roles.Length];for(int i=0;i<roles.Length;i++)sockets[i]=new ImportedSocket04OD{Role=roles[i],Path="Art/Root/socket_"+roles[i]};
            return new ImportedModel04OD{Identity="malik",Enabled=false,Bones=bones,Sockets=sockets,AlignBindAxesH3=true,PreserveInfluencesH5=true,ArtProof=new ImportedArtRecord04OH{AssetId=MalikReviewH8R.AssetId,Creator="Recorded legal source and original project costume",LicenseId="CC0-1.0",RightsRecordId="actual-file-lineage",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false,ExportAssetPath=MalikReviewH8R.ExportPath,ExportSha256=new string('a',64),VisualDependencyHash=new string('b',32)}};
        }
        [Test] public void UnapprovedEditorInspectionCannotBecomeProductionApproval()
        {
            ImportedModel04OD model=Candidate();string reason;
            Assert.IsTrue(MalikReviewH8R.CanReview(model,true,out reason));
            Assert.IsFalse(ImportedArt04OH.CanActivate(model.ArtProof,model.AnimationAuthority,out reason));
            Assert.IsFalse(model.Enabled);Assert.IsFalse(model.ArtProof.OwnerApproved);
        }
        [Test] public void InspectionCannotEnterAPlayerBuild(){string reason;Assert.IsFalse(MalikReviewH8R.CanReview(Candidate(),false,out reason));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void BrokenRightsIdentitySourceOrAuthorityRemainRejected(int defect)
        {
            ImportedModel04OD model=Candidate();
            if(defect==0)model.Identity="companion:wild";
            if(defect==1)model.Enabled=true;
            if(defect==2)model.ArtProof.CommercialUse=false;
            if(defect==3)model.ArtProof.PublicSourceApproved=false;
            if(defect==4)model.ArtProof.Diagnostic=true;
            if(defect==5)model.ArtProof.ExportSha256="unverified";
            if(defect==6)model.ArtProof.ExportAssetPath="Assets/Unrelated.fbx";
            if(defect==7)model.AnimationAuthority=ImportedAnimationAuthority04OH.AuthoredClips;
            string reason;Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));Assert.IsNotEmpty(reason);
        }
        [Test] public void DuplicateBoneAndMissingSocketCannotPassInspection()
        {
            ImportedModel04OD model=Candidate();model.Bones[1].SourceBone=0;string reason;Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));
            model=Candidate();model.Sockets=new ImportedSocket04OD[0];Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));
        }
        [Test] public void CompleteInfluencesBindAlignmentAndExactCandidateAreMandatory()
        {
            string reason;ImportedModel04OD model=Candidate();model.PreserveInfluencesH5=false;
            Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));model=Candidate();model.AlignBindAxesH3=false;
            Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));model=Candidate();model.ArtProof.AssetId=MalikReviewH7.AssetId;
            Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));model=Candidate();model.ArtProof.ExportAssetPath=MalikReviewH7.ExportPath;
            Assert.IsFalse(MalikReviewH8R.CanReview(model,true,out reason));
        }
    }
}
