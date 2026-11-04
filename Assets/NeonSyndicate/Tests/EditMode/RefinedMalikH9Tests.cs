using System.IO;
using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    /// <summary>The H9 refinement of the selected human: its motion set passes the same curated schema 2 contract with its
    /// own source lineage, its receipt records the exact export, and the review dispatch picks the H9 contract only for
    /// the H9 export. Offline maths and file checks only; no native Unity.</summary>
    public sealed class RefinedMalikH9Tests
    {
        [Test] public void H9MotionPassesTheCuratedContractWithItsOwnLineage()
        {
            MalikDataH8 h9=CuratedMotionH8STests.Load(MalikReviewH9.MotionPath),h8s=CuratedMotionH8STests.Load(MalikReviewH8S.MotionPath);
            string reason;Assert.IsTrue(new MalikMotionH8().Valid(h9,out reason),reason);
            Assert.AreEqual(2,h9.schema);Assert.AreEqual(MalikReviewH9.AssetId,h9.assetId);
            Assert.AreNotEqual(h8s.sourceSha256,h9.sourceSha256,"H9 motion records the H9 Blender source, not the H8S one");
            Assert.AreEqual(MalikMotionH8.CuratedClipsH8S(),System.Array.ConvertAll(h9.clips,c=>c.name));
            for(int i=0;i<h9.clips.Length;i++)
            {
                Assert.AreEqual(h8s.clips[i].duration,h9.clips[i].duration,.0001,h9.clips[i].name);
                Assert.AreEqual(h8s.clips[i].loop,h9.clips[i].loop,h9.clips[i].name);
                Assert.AreEqual(h8s.clips[i].samples.Length,h9.clips[i].samples.Length,h9.clips[i].name);
            }
        }
        [Test] public void H9ReceiptRecordsTheExactExportAndStaysDisabled()
        {
            string text=File.ReadAllText(MalikReviewH9.Root+"candidate.json");
            StringAssert.Contains("\"assetId\": \""+MalikReviewH9.AssetId+"\"",text);
            StringAssert.Contains("\"sourceFbx\": \""+MalikReviewH9.ExportPath+"\"",text);
            StringAssert.Contains("\"authoringSource\": \""+MalikReviewH9.SourcePath+"\"",text);
            StringAssert.Contains("\"ownerApproved\": false",text);StringAssert.Contains("\"enabled\": false",text);
            StringAssert.Contains(MalikReviewH9.MotionPath,text);
            Assert.IsTrue(File.Exists(MalikReviewH9.ExportPath) && File.Exists(MalikReviewH9.MotionPath));
        }
        [Test] public void OnlyTheH9ExportReachesTheH9Contract()
        {
            string reason;
            ImportedModel04OD h9=CandidateH9(MalikReviewH9.ExportPath);
            Assert.IsTrue(MalikReviewH8S.CanReviewSelected(h9,true,out reason),reason);
            Assert.IsTrue(MalikReviewH9.CanReview(h9,true,out reason),reason);
            Assert.IsFalse(MalikReviewH9.CanReview(CandidateH9(MalikReviewH8S.ExportPath),true,out reason),"the H9 contract refuses the H8S export");
            Assert.IsFalse(MalikReviewH9.CanReview(h9,false,out reason),"never a player-build override");
            h9.PreserveInfluencesH5=false;Assert.IsFalse(MalikReviewH8S.CanReviewSelected(h9,true,out reason),"complete weights stay mandatory");
        }
        private static ImportedModel04OD CandidateH9(string exportPath)
        {
            ImportedBone04OD[] bones=new ImportedBone04OD[RigLayout.BoneCount];for(int i=0;i<bones.Length;i++)bones[i]=new ImportedBone04OD{SourceBone=i,TargetPath="Art/Root/bone"+i};
            string[] roles={"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"};ImportedSocket04OD[] sockets=new ImportedSocket04OD[roles.Length];for(int i=0;i<roles.Length;i++)sockets[i]=new ImportedSocket04OD{Role=roles[i],Path="Art/Root/socket_"+roles[i]};
            return new ImportedModel04OD{Identity="malik",Enabled=false,Bones=bones,Sockets=sockets,AlignBindAxesH3=true,PreserveInfluencesH5=true,ArtProof=new ImportedArtRecord04OH{AssetId=MalikReviewH9.AssetId,Creator="Recorded legal source and original project costume",LicenseId="CC0-1.0",RightsRecordId="actual-file-lineage",CommercialUse=true,PublicSourceApproved=true,OwnerApproved=false,ExportAssetPath=exportPath,ExportSha256=new string('a',64),VisualDependencyHash=new string('b',32)}};
        }
    }
}
