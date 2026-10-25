using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class LiveWildL2Tests
    {
        private static WildPiece Piece(WildPieceKind kind=WildPieceKind.Boulder,int seed=123)
        {return new WildPiece{Kind=kind,Seed=seed,Site=-1,Biome=WildBiome.HoodooBadlands,Position=new Vector3(-1200,3,-200),Size=new Vector3(3,4,3),Yaw=27,Solid=true};}
        [TestCase(WildPieceKind.Boulder)] [TestCase(WildPieceKind.RockSpire)] [TestCase(WildPieceKind.DeadTree)]
        public void OnlyTheThreeRedspireOpenScatterKindsAreEligible(WildPieceKind kind)
        {
            WildPiece p=Piece(kind);Assert.IsTrue(LiveWildsRulesL2.Eligible(p));p.Site=0;Assert.IsFalse(LiveWildsRulesL2.Eligible(p));p.Site=-1;p.Biome=WildBiome.DuneSea;Assert.IsFalse(LiveWildsRulesL2.Eligible(p));
        }
        [TestCase(WildPieceKind.Butte)] [TestCase(WildPieceKind.Arch)] [TestCase(WildPieceKind.Pond)] [TestCase(WildPieceKind.GroundPatch)]
        public void ProtectedKindsNeverEnterTheLane(WildPieceKind kind){Assert.IsFalse(LiveWildsRulesL2.Eligible(Piece(kind)));}
        [Test] public void VariantSelectionDoesNotConsumeGlobalRandomOrChangeThePiece()
        {
            HashSet<int> variants=new HashSet<int>();
            for(int seed=-200;seed<200;seed++){WildPiece p=Piece(WildPieceKind.Boulder,seed);int a=LiveWildsRulesL2.Variant(p,2);Assert.AreEqual(a,LiveWildsRulesL2.Variant(p,2));Assert.That(a,Is.InRange(0,1));Assert.AreEqual(seed,p.Seed);variants.Add(a);}
            Assert.AreEqual(2,variants.Count);Assert.AreEqual(-1,LiveWildsRulesL2.Variant(Piece(),0));Assert.AreEqual(-1,LiveWildsRulesL2.Variant(Piece(),9));
        }
        [TestCase("biome DuneSea")] [TestCase("collision mesh")] [TestCase("variant ../escape")] [TestCase("scale NaN")] [TestCase("version 2")]
        public void UnsafeKitExtensionsFailClosed(string mutation)
        {
            string text=File.ReadAllText("Assets/NeonSyndicate/Art/LiveWilds/Boulder/kit.txt")+"\n"+mutation;string material,reason;string[] variants;
            Assert.IsFalse(LiveWildsRulesL2.Kit(text,"Boulder",out material,out variants,out reason));
        }
        [TestCase("Boulder")] [TestCase("RockSpire")] [TestCase("DeadTree")]
        public void ActualKitsRequireOriginalCollisionAndDistinctVariants(string kind)
        {
            string material,reason;string[] variants;Assert.IsTrue(LiveWildsRulesL2.Kit(File.ReadAllText("Assets/NeonSyndicate/Art/LiveWilds/"+kind+"/kit.txt"),kind,out material,out variants,out reason),reason);Assert.AreEqual(2,variants.Length);Assert.AreNotEqual(variants[0],variants[1]);
        }
        [Test] public void FittedFramePreservesOriginalVisualEnvelopeAndCollisionBytes()
        {
            foreach(WildPieceKind kind in new[]{WildPieceKind.Boulder,WildPieceKind.RockSpire,WildPieceKind.DeadTree})
            {
                WildPiece p=Piece(kind);MeshBuilder visual=new MeshBuilder(WildsShapes.MatCount),collision=new MeshBuilder(1);WildsShapes.Build(p,visual,collision);Vector3[] before=collision.Vertices.ToArray();
                Vector3 center,size;Assert.IsTrue(LiveWildsRulesL2.Frame(p,visual.Vertices,out center,out size));Assert.That(size.x,Is.GreaterThan(0));Assert.AreEqual(before,collision.Vertices.ToArray());Assert.AreEqual(-1,p.Site);Assert.AreEqual(27,p.Yaw);
                Quaternion inverse=QMath.AY(-p.Yaw);foreach(Vector3 v in visual.Vertices){Vector3 local=inverse*(v-center);Assert.LessOrEqual(Mathf.Abs(local.x),size.x*.5f+.001f);Assert.LessOrEqual(Mathf.Abs(local.y),size.y*.5f+.001f);Assert.LessOrEqual(Mathf.Abs(local.z),size.z*.5f+.001f);}
            }
        }
        [Test] public void InvalidAndEmptyBoundsCannotSuppressTheFallback()
        {
            Vector3 c,s;Assert.IsFalse(LiveWildsRulesL2.Frame(Piece(),new Vector3[0],out c,out s));Assert.IsFalse(LiveWildsRulesL2.Frame(Piece(),new[]{new Vector3(float.NaN,0,0)},out c,out s));WildPiece p=Piece();p.Size=Vector3.zero;Assert.IsFalse(LiveWildsRulesL2.Eligible(p));
        }
        [Test] public void MissingMixedClusterOrInstalledFamiliesCannotInventARoute()
        {
            Vector3 at;string label;Assert.IsFalse(LiveWildsRulesL2.Walk(null,new[]{true,true,true},null,out at,out label));Assert.IsFalse(LiveWildsRulesL2.Walk(new WildsPlan(),new[]{true,false,true},null,out at,out label));
        }
        [Test] public void CurrentSeedHasAllThreeFamiliesWithoutEditingThePlanOrItsSeventeenSites()
        {
            CityLayout layout=new CityLayout(61724);layout.Generate();layout.ApplyProfile04O(CityRevision04O.GothicCivicH5,InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog()));
            WildsPlan plan=WildsLayout.Plan(layout,null);int count=plan.Pieces.Count;List<string> ids=new List<string>();foreach(WildSite site in plan.Sites)ids.Add(site.Id);int[] families=new int[3];
            foreach(WildPiece p in plan.Pieces)if(LiveWildsRulesL2.Eligible(p))families[LiveWildsRulesL2.Slot(p.Kind)]++;
            Assert.AreEqual(17,ids.Count);foreach(int n in families)Assert.Greater(n,0);Vector3 at;string label;Assert.IsTrue(LiveWildsRulesL2.Walk(plan,new[]{true,true,true},new HashSet<int>(),out at,out label));Assert.IsTrue(WildsLayout.InWilds(at.x,at.z));Assert.IsTrue(label.Contains("Hollowwind Maze") || label.Contains("Redwatch Tower"));Assert.AreEqual(count,plan.Pieces.Count);for(int i=0;i<17;i++)Assert.AreEqual(ids[i],plan.Sites[i].Id);
        }
        [Test] public void ExcludingEveryCandidatePreventsAStaleArtWalk()
        {
            WildsPlan plan=new WildsPlan();plan.Sites.Add(new WildSite{Biome=WildBiome.HoodooBadlands,Kind=WildSiteKind.HoodooMaze,Center=Piece().Position,Name="Fixture"});
            plan.Pieces.Add(Piece());plan.Pieces.Add(Piece(WildPieceKind.RockSpire));plan.Pieces.Add(Piece(WildPieceKind.DeadTree));
            Vector3 at;string label;
            Assert.IsFalse(LiveWildsRulesL2.Walk(plan,new[]{true,true,true},new HashSet<int>{0,1,2},out at,out label));
            Assert.AreEqual(3,plan.Pieces.Count);
        }
        [Test,Category("NeedsEngine")] public void AppearanceAndFallbackKeepTheActualOriginalCollider()
        {
            GameObject root=new GameObject("L2 native collider lifetime fixture");
            try
            {
                WildsPlan plan=new WildsPlan();plan.Pieces.Add(Piece());
                WildsChunkL1 chunk=root.AddComponent<WildsChunkL1>();chunk.Init(plan,new[]{0},new WildsBuilder.Result());
                MeshCollider collider=root.GetComponentInChildren<MeshCollider>();Mesh mesh=collider.sharedMesh;
                for(int i=0;i<3;i++)
                {
                    chunk.SetArtL2(true);Assert.AreSame(collider,root.GetComponentInChildren<MeshCollider>());Assert.AreSame(mesh,collider.sharedMesh);Assert.IsTrue(collider.gameObject.activeInHierarchy);
                    chunk.SetArtL2(false);Assert.AreSame(collider,root.GetComponentInChildren<MeshCollider>());Assert.AreSame(mesh,collider.sharedMesh);Assert.AreEqual(1,root.GetComponentsInChildren<MeshCollider>().Length);
                }
            }
            finally {Object.DestroyImmediate(root);}
        }
        [Test,Category("NeedsEngine")] public void OverlappingL1LeasesSurviveAppearanceRoundTrips()
        {
            GameObject root=new GameObject("L2 native L1 lease fixture");
            try
            {
                WildsPlan plan=new WildsPlan();plan.Pieces.Add(Piece());
                WildsChunkL1 chunk=root.AddComponent<WildsChunkL1>();chunk.Init(plan,new[]{0},new WildsBuilder.Result());
                object first=new object(),second=new object();chunk.SetLease(first,new HashSet<int>{0});chunk.SetLease(second,new HashSet<int>{0});
                chunk.SetArtL2(true);chunk.SetArtL2(false);chunk.SetLease(first,null);
                Assert.AreEqual(0,root.GetComponentsInChildren<MeshCollider>().Length);
                chunk.SetLease(second,null);Assert.AreEqual(1,root.GetComponentsInChildren<MeshCollider>().Length);
                Assert.AreEqual(1,plan.Pieces.Count);Assert.AreEqual(-1,plan.Pieces[0].Site);
            }
            finally {Object.DestroyImmediate(root);}
        }
        [Test] public void L1ExclusionsKeepTheirOriginalScopedSelection()
        {
            WildsPlan plan=new WildsPlan();WildPiece p=Piece();plan.Pieces.Add(p);WildPiece site=p;site.Site=2;plan.Pieces.Add(site);
            HashSet<int> excluded=LiveWildRulesL1.SelectClear(plan,p.Position-Vector3.one*8,p.Position+Vector3.one*8,10);
            Assert.IsTrue(excluded.Contains(0));Assert.IsFalse(excluded.Contains(1));Assert.AreEqual(2,plan.Pieces.Count);Assert.AreEqual(2,plan.Pieces[1].Site);
        }
    }
}
