using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class LiveGroundL3Tests
    {
        [TestCase(-1300f,-500f,1f)]
        [TestCase(1300f,0f,0f)]
        [TestCase(0f,-1300f,0f)]
        [TestCase(0f,0f,0f)]
        [TestCase(-1300f,1150f,0f)]
        [TestCase(-1600f,-500f,0f)]
        public void CoreAndProtectedRegions(float x,float z,float expected)
        { Assert.That(LiveGroundRulesL3.Weight(x,z),Is.EqualTo(expected).Within(.001f)); }

        [Test]
        public void MissingOrNonfiniteCoordinatesNeverPaintTheWorld()
        {
            Assert.That(LiveGroundRulesL3.Weight(float.NaN,0),Is.Zero);
            Assert.That(LiveGroundRulesL3.Weight(0,float.PositiveInfinity),Is.Zero);
        }
        [Test]
        public void NoisySouthernBorderHasStableIntermediateWeights()
        {
            int blended=0; float previous=LiveGroundRulesL3.Weight(-1300,-1520);
            for(int z=-1519;z<0;z++)
            {
                float weight=LiveGroundRulesL3.Weight(-1300,z);
                Assert.That(weight,Is.InRange(0f,1f));
                Assert.That(Mathf.Abs(weight-previous),Is.LessThan(.18f),"hard seam at z="+z);
                Assert.That(weight,Is.EqualTo(LiveGroundRulesL3.Weight(-1300,z)));
                if(weight>.02f && weight<.98f)blended++;
                previous=weight;
            }
            Assert.That(blended,Is.GreaterThan(8));
        }
        [Test]
        public void NorthernFrostAndInnerRingFeatherWithoutChangingBiomeAuthority()
        {
            foreach(Vector2 start in new[]{new Vector2(-1300,900),new Vector2(-1010,0)})
            {
                int mixed=0;
                for(int i=0;i<150;i++)
                {
                    float x=start.x-(start.y==0?i:0),z=start.y+(start.y!=0?i:0);
                    WildBiome before=WildsLayout.BiomeAt(x,z);
                    float weight=LiveGroundRulesL3.Weight(x,z);
                    Assert.That(WildsLayout.BiomeAt(x,z),Is.EqualTo(before));
                    if(weight>.01f && weight<.99f)mixed++;
                }
                Assert.That(mixed,Is.GreaterThan(5));
            }
        }
        [Test]
        public void SlopeChangesFamilyMixButCannotMoveTheRegion()
        {
            float weight=LiveGroundRulesL3.Weight(-1300,-500);
            Assert.That(LiveGroundRulesL3.Stony(-1300,-500,.4f),Is.GreaterThan(LiveGroundRulesL3.Stony(-1300,-500,1f)));
            Assert.That(LiveGroundRulesL3.Weight(-1300,-500),Is.EqualTo(weight));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void GroundAdmissionRequiresSelectionCompleteArtAndCurrentRevision(bool ground)
        {
            Assert.That(LiveGroundRulesL3.Active(ground,false,true,true),Is.EqualTo(ground));
            Assert.That(LiveGroundRulesL3.Active(ground,true,true,true),Is.False);
            Assert.That(LiveGroundRulesL3.Active(ground,false,false,true),Is.False);
            Assert.That(LiveGroundRulesL3.Active(ground,false,true,false),Is.False);
        }
        [TestCase(WildMat.Dirt,true)]
        [TestCase(WildMat.Sand,true)]
        [TestCase(WildMat.DryTurf,true)]
        [TestCase(WildMat.GlassSand,true)]
        [TestCase(WildMat.RedRock,false)]
        [TestCase(WildMat.Stone,false)]
        [TestCase(WildMat.Water,false)]
        [TestCase(WildMat.Snow,false)]
        [TestCase(WildMat.Crystal,false)]
        public void OnlyExistingGroundMaterialSlotsQualify(WildMat slot,bool expected)
        { Assert.That(LiveGroundRulesL3.GroundSlot(slot),Is.EqualTo(expected)); }

        [Test,Category("NeedsEngine")]
        public void NativePreparedMapsAndSharedMaterialRoundTrip()
        {
            LiveGroundCatalogL3 catalog=Resources.Load<LiveGroundCatalogL3>("LiveGroundPrepared/RedspireCatalog");
            Assert.That(catalog,Is.Not.Null,"Run Prepare Redspire ground before native tests.");
            Assert.That(catalog.Ready,Is.True,catalog.Reason);
            Material source=Mats.Sand,proxy=LiveGroundMaterialsL3.Floor;
            Shader original=source.shader;
            for(int i=0;i<12;i++)
            {
                LiveGroundMaterialsL3.Select(true,false,true);
                Assert.That(proxy.shader,Is.SameAs(catalog.SurfaceShader));
                Assert.That(LiveGroundMaterialsL3.Floor,Is.SameAs(proxy));
                LiveGroundMaterialsL3.Select(false,false,true);
                Assert.That(proxy.shader,Is.SameAs(original));
                Assert.That(proxy.mainTexture,Is.SameAs(source.mainTexture));
            }
            Texture2D missing=catalog.Earth[0];
            try
            {
                catalog.Earth[0]=null;
                LiveGroundMaterialsL3.Select(true,false,true);
                Assert.That(proxy.shader,Is.SameAs(original));
                Assert.That(LiveGroundMaterialsL3.Enabled,Is.False);
            }
            finally { catalog.Earth[0]=missing; LiveGroundMaterialsL3.Select(false,false,true); }
        }

        [Test,Category("NeedsEngine")]
        public void NativeFourAppearanceCombinationsPreserveChunkCollisionAndScopedLease()
        {
            GameObject root=new GameObject("L3 native ground and L1 lease fixture");
            try
            {
                WildsPlan plan=new WildsPlan();
                plan.Pieces.Add(new WildPiece{Kind=WildPieceKind.Boulder,Biome=WildBiome.HoodooBadlands,Site=-1,Seed=61724,Position=new Vector3(-1300,0,-500),Size=new Vector3(3,2,3),Solid=true});
                plan.Pieces.Add(new WildPiece{Kind=WildPieceKind.Swell,Biome=WildBiome.HoodooBadlands,Site=-1,Seed=61725,Position=new Vector3(-1300,0,-500),Size=new Vector3(30,3,30),Solid=true});
                WildsChunkL1 chunk=root.AddComponent<WildsChunkL1>();
                chunk.Init(plan,new[]{0,1},new WildsBuilder.Result());
                object lease=new object();
                chunk.SetLease(lease,new System.Collections.Generic.HashSet<int>{0});
                MeshCollider original=root.GetComponentInChildren<MeshCollider>();
                Mesh collision=original.sharedMesh;
                for(int round=0;round<3;round++) foreach(bool kit in new[]{false,true}) foreach(bool ground in new[]{false,true})
                {
                    chunk.SetArtL2(kit); LiveGroundMaterialsL3.Select(ground,false,true);
                    Assert.That(root.GetComponentsInChildren<MeshCollider>().Length,Is.EqualTo(1));
                    Assert.That(root.GetComponentInChildren<MeshCollider>(),Is.SameAs(original));
                    Assert.That(original.sharedMesh,Is.SameAs(collision));
                    System.Collections.Generic.HashSet<int> excluded=new System.Collections.Generic.HashSet<int>();
                    chunk.ExclusionsL2(excluded); Assert.That(excluded,Does.Contain(0));
                }
                chunk.SetLease(lease,null);
                Assert.That(plan.Pieces.Count,Is.EqualTo(2));
                Assert.That(root.GetComponentsInChildren<MeshCollider>().Length,Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); LiveGroundMaterialsL3.Select(false,false,true); }
        }
    }
}
