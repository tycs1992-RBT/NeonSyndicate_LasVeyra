using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class ArtProductionH3Tests
    {
        [Test] public void AllOriginalBonesMapUniquelyToTheActualReceivedSkeleton()
        {
            string source=File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Art/CandidatesH3/received_rig.json"));
            List<string> names=Regex.Matches(source,"\"name\"\\s*:\\s*\"([^\"]+)\"").Cast<Match>().Select(m=>m.Groups[1].Value).ToList();
            string[] mapped=RigLayout.Names.Select(ImportedBindH3.CandidateBone).ToArray();
            Assert.That(mapped.Distinct().Count(),Is.EqualTo(RigLayout.BoneCount));foreach(string name in mapped)Assert.That(names,Does.Contain(name));
            string manifest=File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Art/CandidatesH3/candidate.json"));
            Assert.That(manifest,Does.Contain("\"owner_appearance_approved\": false"));Assert.That(manifest,Does.Contain("\"registry_enabled\": false"));
        }
        [Test] public void OptInBindAimHandlesDifferentRestPosesAndWorldTranslation()
        {
            Vector3[] original={Vector3.zero,Vector3.down},target={new Vector3(2,3,4),new Vector3(3,3,4)};int[] parents={-1,0};Quaternion[] rotations={QMath.Identity,QMath.Identity};
            Quaternion[] aligned=ImportedBindH3.Align(original,parents,target,rotations,QMath.Identity);
            Assert.That(Vector3.Distance(aligned[0]*Vector3.right,Vector3.down),Is.LessThan(.00001f));
            for(int i=0;i<target.Length;i++)target[i]+=new Vector3(-20,4,10);
            Quaternion[] moved=ImportedBindH3.Align(original,parents,target,rotations,QMath.Identity);
            Assert.That(Mathf.Abs(QMath.Dot(aligned[0],moved[0])),Is.EqualTo(1).Within(.00001));
            Assert.That(new ImportedModel04OD().AlignBindAxesH3,Is.False,"Older visual entries retain their original bind policy.");
        }
        [Test] public void UnusedBindEntriesDoNotRequireFabricatedGameplayMappings()
        {
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.ReferencedBones(new BoneWeight[0],4));
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.ReferencedBones(new[]{new BoneWeight()},4));
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.ReferencedBones(new[]{new BoneWeight{boneIndex0=1,weight0=2}},4));
            bool[] actual=ImportedBindH3.ReferencedBones(new[]{new BoneWeight{boneIndex0=2,weight0=1}},4);
            Assert.That(actual,Is.EqualTo(new[]{false,false,true,false}));
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.ReferencedBones(new[]{new BoneWeight{boneIndex0=9,weight0=1}},4));
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.ReferencedBones(new[]{new BoneWeight{boneIndex0=1,weight0=float.NaN}},4));
        }
        [Test] public void IncompleteOrCollapsedCandidateBindFailsClosed()
        {
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.Align(null,null,null,null,QMath.Identity));
            Vector3[] a={Vector3.zero,Vector3.down},bad={Vector3.zero,Vector3.zero};
            Assert.Throws<ArgumentException>(()=>ImportedBindH3.Align(a,new[]{-1,0},bad,new[]{QMath.Identity,QMath.Identity},QMath.Identity));
            bad[1]=new Vector3(float.NaN,0,0);Assert.Throws<ArgumentException>(()=>ImportedBindH3.Align(a,new[]{-1,0},bad,new[]{QMath.Identity,QMath.Identity},QMath.Identity));
        }
        [TestCase("dante")] [TestCase("dispatcher")]
        public void NamedServiceSkinsKeepCompleteWeightingFacialChannelsAndBudgets(string id)
        {
            Appearance look=CastRoster04E.Look(id=="dante"?CastIds04E.Dante:CastIds04E.Dispatcher);BodyData body=BodyBuilder.Build(look,RigLayout.From(look),1);
            Assert.That(body.Vertices.Count,Is.InRange(1000,8200));Assert.That(body.FaceShapes04L.Count,Is.GreaterThan(0));
            Assert.That(body.Vertices.All(MathUtil.IsFinite),Is.True);
            foreach(BoneWeight w in body.Weights)Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.0001));
            foreach(int index in body.Triangles)Assert.That(index,Is.InRange(0,body.Vertices.Count-1));
        }
        [Test] public void EarnedGuardianDetailAppendsToTheExistingRigAndAttackVolumes()
        {
            CreatureBody body=CreatureBuilder.Build(CompanionLooks.For(BondIds.AshWarden,61724,1),1);Vector3[] before=body.Mesh.Vertices.ToArray();float radius=body.Radius,height=body.Height;int bones=body.Skeleton.Count,volumes=body.HitVolumes.Count;
            int added=CompanionSculpt.Apply(body,BondIds.AshWarden,1,"",1);Assert.That(added,Is.GreaterThan(0));
            Assert.That(body.Mesh.Vertices.Take(before.Length),Is.EqualTo(before));Assert.That(body.Skeleton.Count,Is.EqualTo(bones));Assert.That(body.HitVolumes.Count,Is.EqualTo(volumes));
            Assert.That(body.Radius,Is.EqualTo(radius));Assert.That(body.Height,Is.EqualTo(height));Assert.That(body.Mesh.Vertices.All(MathUtil.IsFinite),Is.True);
            foreach(BoneWeight w in body.Mesh.Weights)Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.0001));
        }
        [Test] public void ThreeBossMantlePosesHaveDifferentBoundedPhaseSilhouettes()
        {
            Vector3 first=RegionalBossPose04OF.MantleAnglesH3(1,0,0,0),last=RegionalBossPose04OF.MantleAnglesH3(1,2,0,0),mirror=RegionalBossPose04OF.MantleAnglesH3(-1,2,0,0);
            Assert.That(first,Is.Not.EqualTo(last));Assert.That(last.y,Is.EqualTo(-mirror.y));
            foreach(int phase in new[]{0,1,2})foreach(float cast in new[]{0,.5f,1})foreach(float recovery in new[]{0,.5f,1})
            {Vector3 pose=RegionalBossPose04OF.MantleAnglesH3(1,phase,cast,recovery);Assert.That(MathUtil.IsFinite(pose),Is.True);Assert.That(pose.magnitude,Is.LessThan(30));}
        }
        [Test] public void HearthInvestigationIsPhysicalFiniteAndRestrictedToTheNewBank()
        {
            string path=Path.Combine(Directory.GetCurrentDirectory(),"Assets/NeonSyndicate/Resources/Quests/ascension_h3_hearth_witness.json");List<string> errors=new List<string>();QuestFile file=QuestJson.Read<QuestFile>(File.ReadAllText(path),path,errors,null);Assert.That(errors,Is.Empty);
            QuestDef quest=file.Quests.Single();Assert.That(quest.Requires.WorldRevisions,Is.EqualTo(new[]{9}));Assert.That(quest.VenueGiver,Is.EqualTo("forge"));
            QuestStep[] steps=quest.Phases.SelectMany(p=>p.Steps).ToArray();Assert.That(steps.Count(s=>s.Type=="Interact"),Is.EqualTo(3));Assert.That(steps.First(s=>s.Type=="Interact").At,Is.EqualTo("Realm:hearth-lens"));
            Assert.That(steps.Where(s=>s.Type=="Kill").Sum(s=>s.Count),Is.EqualTo(3));Assert.That(steps.Where(s=>s.Type=="Kill").All(s=>s.MaxAlive==1),Is.True);Assert.That(steps.Any(s=>s.Type=="GiveGun" || s.Type=="Boss" || s.Type=="Bond"),Is.False);
            Assert.That(quest.Reward.Cash,Is.EqualTo(60));Assert.That(quest.NoAmbient && quest.IgnoreCrimes,Is.True);
            Assert.That(QuestLogic.WorldMatches(quest.Requires,8),Is.False);Assert.That(QuestLogic.WorldMatches(quest.Requires,9),Is.True);
        }
        [Test] public void NewAcceptanceRowsPreserveEveryOlderObservation()
        {
            string[] before=CinematicAcceptance04OF.ChecklistArtH2(),after=CinematicAcceptance04OF.ChecklistHeartH3();Assert.That(after.Take(before.Length),Is.EqualTo(before));Assert.That(after.Length,Is.EqualTo(before.Length+4));
        }
    }
}
