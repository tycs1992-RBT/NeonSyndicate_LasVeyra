using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class CinematicRebirth04OFTests
    {
        [TestCase("../save.json",false)]
        [TestCase("lasveyra_unbound04oe_save_0.json",false)]
        [TestCase("C:/private/result.xml",false)]
        [TestCase("shots/castellan_phase_2.png",true)]
        [TestCase("tests_EditMode/nunit-results.xml",true)]
        [TestCase("profile/traversal.csv",true)]
        [TestCase("model.fbx",false)]
        public void ShareBundleFiltersSavesAndKeepsRealEvidence(string path,bool expected)
        {Assert.That(CinematicAcceptance04OF.SafeEvidenceFile(path),Is.EqualTo(expected));}
        [TestCase("",false)][TestCase("../hips",false)][TestCase("root//hips",false)][TestCase("/root/hips",false)]
        [TestCase("root/hips",true)][TestCase("root/Hand.R",true)]
        public void MappingRejectsAmbiguousPaths(string path,bool valid)
        {
            string reason;Assert.That(ImportedModelRegistry04OD.ValidMapping(1,new[]{new ImportedBone04OD{SourceBone=0,TargetPath=path}},1,out reason),Is.EqualTo(valid));
        }
        [Test] public void ImportedPoseUsesRootBindAndAppliesParentsBeforeChildren()
        {
            Quaternion root=QMath.AY(31),source=root*QMath.AX(27),target=root*QMath.AZ(12);
            Quaternion actual=ImportedModelRegistry04OD.BindRotation04OF(source,root,target);
            Quaternion expected=root*QMath.AX(27)*QMath.AZ(12);
            Assert.That(actual.x,Is.EqualTo(expected.x).Within(.00001));Assert.That(actual.y,Is.EqualTo(expected.y).Within(.00001));
            Assert.That(actual.z,Is.EqualTo(expected.z).Within(.00001));Assert.That(actual.w,Is.EqualTo(expected.w).Within(.00001));
            Assert.That(ImportedModelRegistry04OD.ParentFirst04OF(new[]{4,0,2,2,1}),Is.EqualTo(new[]{1,4,2,3,0}));
            Assert.That(ImportedModelRegistry04OD.ParentFirst04OF(null),Is.Empty);
        }
        [Test] public void EvidenceLabelsCannotEscapeFolder()
        {
            Assert.That(CinematicAcceptance04OF.Label("../Malik: attack/side"),Is.EqualTo("___Malik__attack_side"));
            Assert.That(CinematicAcceptance04OF.Label(new string('a',100)).Length,Is.EqualTo(64));
            string[] a=CinematicAcceptance04OF.Checklist(),b=CinematicAcceptance04OF.Checklist();a[0]="altered";Assert.That(b[0],Is.Not.EqualTo("altered"));
            Assert.That(CinematicAcceptance04OF.ProfileWarning(4),Does.Contain("WRONG PREVIEW"));Assert.That(CinematicAcceptance04OF.ProfileWarning(5),Does.Not.Contain("WRONG"));
        }
        [TestCase(1)][TestCase(2)] public void MalikHasActualNewGeometryWithinExistingBudgets(int detail)
        {
            Appearance look=Appearance.Malik();RigLayout rig=RigLayout.From(look);BodyData body=BodyBuilder.Build(look,rig,detail);
            Assert.That(body.UnboundTailoringVertices04OE,Is.GreaterThan(0),"Preserve prior layer");
            Assert.That(body.CinematicTailoringVertices04OF,Is.GreaterThan(100),"Must actually emit the new layer");
            Assert.That(body.Vertices.Count,Is.LessThanOrEqualTo(detail==2?13500:8200));
            Assert.That(body.Weights.Count,Is.EqualTo(body.Vertices.Count));Assert.That(rig.World.Length,Is.EqualTo(47));
            foreach(KeyValuePair<string,Vector3[]> shape in body.FaceShapes04L)Assert.That(shape.Value.Length,Is.EqualTo(body.Vertices.Count));
            foreach(BoneWeight weight in body.Weights)Assert.That(weight.weight0+weight.weight1+weight.weight2+weight.weight3,Is.EqualTo(1).Within(.0001));
        }
        [Test] public void DanteKeepsIdentityAndGainsWorkingOutfitGeometry()
        {
            Appearance look=CastRoster04E.Look(CastIds04E.Dante);BodyData body=BodyBuilder.Build(look,RigLayout.From(look),2);
            Assert.That(look.HairStyle,Is.EqualTo(HairStyle.Long));Assert.That(look.Skin,Is.EqualTo(3));
            Assert.That(body.ForgewrightVertices04OF,Is.GreaterThan(0));Assert.That(body.Vertices.Count,Is.LessThanOrEqualTo(13500));
        }
        [Test] public void CollapsedFaceCleanupKeepsVertexExpressionAndWeightIndices()
        {
            Appearance look=Appearance.Malik();BodyData body=BodyBuilder.Build(look,RigLayout.From(look),2);
            Assert.That(body.CollapsedTrianglesRemoved04OF,Is.GreaterThan(0));Assert.That(body.Triangles.Count%3,Is.EqualTo(0));
            for(int i=0;i<body.Triangles.Count;i+=3)
            {
                Vector3 a=body.Vertices[body.Triangles[i]],b=body.Vertices[body.Triangles[i+1]],c=body.Vertices[body.Triangles[i+2]];
                Vector3 cross=Vector3.Cross(b-a,c-a);Assert.That(cross.x!=0 || cross.y!=0 || cross.z!=0,Is.True);
            }
        }
        [Test] public void BossCrestRespondsToPhaseAndBreakWithoutChangingFightRules()
        {
            foreach(bool azure in new[]{false,true})
            {
                float initial=RegionalBossPose04OF.Spread(azure,0,PartEffect.None,0,0);
                Assert.That(RegionalBossPose04OF.Spread(azure,2,PartEffect.CoreExposed,1,0),Is.GreaterThan(initial));
                Assert.That(RegionalBossPose04OF.Spread(azure,99,PartEffect.ArmorLost,99,-2),Is.LessThanOrEqualTo(65));
                MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1);RegionalBossPose04OF.Crest(stone,crystal,azure,-1);
                Assert.That(stone.VertexCount,Is.GreaterThan(50));Assert.That(crystal.VertexCount,Is.GreaterThan(20));
            }
            RegionalBossCycle04OD cycle=new RegionalBossCycle04OD();cycle.Tick(4,false);Assert.That(cycle.Begin(1.5f),Is.True);
            Assert.That(cycle.Tick(.2f,true),Is.EqualTo(RegionalBeatEvent04OD.Interrupted));Assert.That(cycle.Tell,Is.EqualTo(0));
            cycle.Close();Assert.That(cycle.Tick(100,false),Is.EqualTo(RegionalBeatEvent04OD.None));
        }
        [Test] public void PartnerBeatPeaksAtOriginalStrikeAndHasFiniteRelease()
        {
            Assert.That(CinematicMotion04OF.Beat(.85f).y,Is.EqualTo(1).Within(.0001));
            Assert.That(CinematicMotion04OF.Beat(1).z,Is.EqualTo(1));Assert.That(CinematicMotion04OF.Beat(float.NaN),Is.EqualTo(Vector3.zero));
            Assert.That(CinematicMotion04OF.Beat(-1),Is.EqualTo(Vector3.zero));
            for(int i=0;i<=100;i++){Vector3 beat=CinematicMotion04OF.Beat(i/100f);Assert.That(beat.x,Is.InRange(0,1));Assert.That(beat.y,Is.InRange(0,1));Assert.That(beat.z,Is.InRange(0,1));}
            SynergyWindow04N window=new SynergyWindow04N();window.Begin(false);window.Tick(.5f,true,true,false);Assert.That(window.Time,Is.EqualTo(0));
            window.Tick(.3f,false,true,false);bool enhanced;Assert.That(window.Resolve(out enhanced),Is.True);Assert.That(enhanced,Is.True);Assert.That(window.Resolve(out enhanced),Is.False);
            window.Begin(false);window.Cancel();Assert.That(window.Resolve(out enhanced),Is.False);
        }
        [Test] public void AzureBodyAndCadenceKeepOriginalSpeciesAndFiniteFormation()
        {
            CreatureLook source=CreatureLooks.For("neon_jackal",12);float length=source.BodyLength;
            CreatureLook azure=FracturedWildsCombat04OC.AzureLook(source);
            Assert.That(source.BodyLength,Is.EqualTo(length));Assert.That(azure.StormProwler04OF,Is.True);
            Assert.That(azure.BodyLength,Is.GreaterThan(source.BodyLength));Assert.That(azure.TailStyle,Is.EqualTo(3));
            Assert.That(FracturedWildsCombat04OC.Species("ev_04oc_azure_watch",0),Is.EqualTo("neon_jackal"));
            CreatureAttack[] attacks=FracturedWildsCombat04OC.AzureAttacks();Assert.That(attacks.Length,Is.EqualTo(2));
            foreach(CreatureAttack attack in attacks){Assert.That(attack.Windup,Is.GreaterThan(.5));Assert.That(attack.Recovery,Is.GreaterThan(.9));Assert.That(attack.Cooldown,Is.GreaterThan(2));}
        }
        [TestCase("ev_04oc_azure_watch")][TestCase("ev_04ob_threshold_watch")]
        public void CurrentPreviewCannotRepeatRegionalFirstWinPayment(string id)
        {
            Assert.That(FracturedWildsCombat04OC.PreviouslyPaid04OF(new[]{id+":7"},id,5),Is.True);
            Assert.That(FracturedWildsCombat04OC.PreviouslyPaid04OF(new[]{id+":first-win-04oc"},id,5),Is.True);
            Assert.That(FracturedWildsCombat04OC.PreviouslyPaid04OF(new[]{id+"_other:7"},id,5),Is.False);
            Assert.That(FracturedWildsCombat04OC.RewardToken(id,1,5),Is.EqualTo(FracturedWildsCombat04OC.RewardToken(id,900,5)));
            Assert.That(FracturedWildsCombat04OC.RewardToken(id,1,5),Is.EqualTo(FracturedWildsCombat04OC.RewardToken(id,2,4)));
            Assert.That(FracturedWildsCombat04OC.RewardToken("ordinary_event",1,5),Is.Not.EqualTo(FracturedWildsCombat04OC.RewardToken("ordinary_event",2,5)));
        }
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void AddedStructureUsesExistingPreviewRoutesWithoutMovingSavedAnchors(int seed)
        {
            CityLayout layout=new CityLayout(seed);layout.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(layout.ApplyProfile04O(CityRevision04O.CrystalUnbound04OE,venues),Is.True);
            int authored=0;
            foreach(RealmRoute04OB route in layout.RealmRoutes04OB)
            {
                MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1),root=new MeshBuilder(1);
                GothicRealmGeometry04OB.CinematicRoute04OF(stone,mineral,root,route);
                if(stone.VertexCount+root.VertexCount==0)continue;authored++;
                Assert.That(route.Id,Is.AnyOf("04oe/oath-terrace","04oe/storm-ledge","04oe/storm-cavern"));
                Assert.That(stone.VertexCount+mineral.VertexCount+root.VertexCount,Is.LessThan(16000));
            }
            Assert.That(authored,Is.EqualTo(3));Assert.That((int)layout.Revision04O,Is.EqualTo(5));
            Assert.That(GoldenViews.Build(layout).FindAll(v=>v.Id.StartsWith("04of_")).Count,Is.EqualTo(3));
            Assert.That(SaveSystem.BankForRevision04OC(4),Is.EqualTo(4));Assert.That(SaveSystem.BankForRevision04OC(5),Is.EqualTo(5));
        }
    }
}
