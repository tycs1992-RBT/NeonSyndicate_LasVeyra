using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Actual engine tests, not represented as passed by source-only cloud validation.</summary>
    public sealed class StrongholdRebirth04ATests
    {
        [TestCase("hearthspire")] [TestCase("galehook")] [TestCase("sunscar")]
        public void SourceCompositionsRespectTheExistingSafePlot(string site)
        {
            StrongholdLayout04A layout = Load(site);
            Assert.DoesNotThrow(() => layout.Validate(site));
            foreach (StrongholdPart04A part in layout.parts)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(StrongholdLayout04A.Vector(part.position),
                    Quaternion.Euler(StrongholdLayout04A.Vector(part.euler)),StrongholdLayout04A.Vector(part.scale));
                for (int detail = 0; detail < 2; detail++)
                {
                    Mesh mesh = VisualRebirth04AAssets.Mesh(part.model,detail==1);
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(vertex);
                        Assert.That(Math.Abs(point.x),Is.LessThan(StrongholdPlacement.PlotHalf),site+" "+part.model+" X");
                        Assert.That(Math.Abs(point.z),Is.LessThan(StrongholdPlacement.PlotHalf),site+" "+part.model+" Z");
                        Assert.That(point.y,Is.InRange(-.3f,40f),site+" "+part.model+" Y");
                        bool gate = point.z > -23.5f && point.z < -11f && point.y>.35f && point.y<2.2f && Math.Abs(point.x)<2.75f;
                        Assert.That(gate,Is.False,site+" "+part.model+" obstructs the protected front approach");
                    }
                }
            }
        }

        [Test]
        public void RivalFortsAddNoCollisionAndKeepTheWardenSightlines()
        {
            Assert.That(Load("galehook").collisions.Length,Is.Zero);
            Assert.That(Load("sunscar").collisions.Length,Is.Zero);
        }

        [Test]
        public void HearthspirePhysicalRouteHasNoUnsupportedGapsOrCliffSteps()
        {
            StrongholdLayout04A home = Load("hearthspire");
            home.Validate("hearthspire");
            float previous = .07f;
            for (int step = 0; step <= 136; step++)
            {
                float z = -15.95f+step*.25f;
                float height = FloorAt(home,-20f,z);
                Assert.That(height,Is.GreaterThan(.07f),"Missing west route support at z="+z);
                Assert.That(height-previous,Is.InRange(-.01f,.251f),"Unsafe route step at z="+z);
                previous=height;
            }
            Assert.That(previous,Is.EqualTo(6.07f).Within(.002f));
            for (int step = 0; step <= 56; step++)
                Assert.That(FloorAt(home,-20f+step*.25f,19.8f),Is.EqualTo(6.07f).Within(.002f),"North overlook route gap");
            Assert.That(home.collisions.Length,Is.LessThanOrEqualTo(48));
        }

        [Test]
        public void CollisionGuardRejectsEntrySocketAndCombatIntrusion()
        {
            foreach (Vector3 bad in new[] {new Vector3(0,1,-20),StrongholdPlacement.Socket(0)+Vector3.up,new Vector3(5,1,5)})
            {
                StrongholdLayout04A home=Load("hearthspire");
                home.collisions=new[] {new StrongholdCollision04A {name="unsafe",position=new[] {bad.x,bad.y,bad.z},size=new[] {1f,1f,1f}}};
                Assert.Throws<InvalidOperationException>(()=>home.Validate("hearthspire"));
            }
        }

        [Test]
        public void VaultSoffitHasCornerSupportsAndNoHangingCenterPosts()
        {
            for(int detail=0;detail<2;detail++)
            {
                Vector3[] vertices=VisualRebirth04AAssets.Mesh("rib_vault",detail==1).vertices;
                foreach(Vector3 point in vertices)
                    if(Math.Abs(point.x)<.75f&&Math.Abs(point.z)<.75f)
                        Assert.That(point.y,Is.GreaterThan(2f),"A center post would hang through the preserved core route.");
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                {
                    Vector3 endpoint=new Vector3(x*2.9f,0f,z*2.9f);
                    float nearest=float.PositiveInfinity;
                    foreach(Vector3 point in vertices)nearest=Math.Min(nearest,(point-endpoint).sqrMagnitude);
                    Assert.That(nearest,Is.LessThan(.22f*.22f),"The soffit must reach all four column tops.");
                }
            }
        }

        [Test]
        public void VegetationSwitchLeavesStructuralRootsAndRoutePartsEnabled()
        {
            foreach(string model in new[] {"living_trunk","living_canopy","cypress_trunk","cypress_canopy",
                "willow_trunk","willow_canopy","fern","mushroom","flower","reeds","vine"})
                Assert.That(StrongholdLayout04A.IsVegetation(model),Is.True,model);
            foreach(string model in new[] {"root_arch","stair","terrace","railing","column","roof"})
                Assert.That(StrongholdLayout04A.IsVegetation(model),Is.False,model);
        }

        [Test]
        public void EverySavedSocketRotationProducesFiniteBoundedArtWithoutMutation()
        {
            for (int module=0;module<=4;module++) for (int rotation=0;rotation<4;rotation++)
            {
                List<FortSlotSave> slots=new List<FortSlotSave>();
                for(int i=0;i<6;i++)slots.Add(new FortSlotSave {Module=module,Rotation=rotation});
                List<StrongholdPart04A> parts=StrongholdLayout04A.SocketParts(slots);
                foreach(StrongholdPart04A part in parts)
                {
                    Vector3 position=StrongholdLayout04A.Vector(part.position);
                    Vector3 scale=StrongholdLayout04A.Vector(part.scale);
                    Assert.That(Math.Abs(position.x),Is.LessThan(14f));
                    Assert.That(Math.Abs(position.z),Is.LessThan(11f));
                    Assert.That(scale.x>0&&scale.y>0&&scale.z>0,Is.True);
                }
                foreach(FortSlotSave slot in slots)
                {Assert.That(slot.Module,Is.EqualTo(module));Assert.That(slot.Rotation,Is.EqualTo(rotation));}
                Assert.That(parts.Count,module==0?Is.EqualTo(0):Is.GreaterThan(0));
            }
        }

        private static float FloorAt(StrongholdLayout04A layout,float x,float z)
        {
            float top=0f;
            foreach(StrongholdCollision04A collision in layout.collisions)
            {
                if(collision.name.Contains("parapet")||collision.name.Contains("column"))continue;
                Vector3 at=StrongholdLayout04A.Vector(collision.position),size=StrongholdLayout04A.Vector(collision.size);
                if(Math.Abs(at.x-x)<=size.x*.5f+.0001f&&Math.Abs(at.z-z)<=size.z*.5f+.0001f)
                    top=Math.Max(top,at.y+size.y*.5f);
            }
            return top;
        }
        private static StrongholdLayout04A Load(string site)
        {
            TextAsset source=Resources.Load<TextAsset>("VisualRebirth04A/Layouts/"+site);
            Assert.That(source,Is.Not.Null,site);
            return JsonUtility.FromJson<StrongholdLayout04A>(source.text);
        }
    }
}
