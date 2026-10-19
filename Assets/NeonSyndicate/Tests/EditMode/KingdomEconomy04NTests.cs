using System.Collections.Generic;
using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    public sealed class KingdomEconomy04NTests
    {
        [Test] public void AStockItemCannotBeBoughtTwiceOrRestoredByReload()
        {
            var b=new MerchantStock04N();b.Initialize(12);Assert.IsTrue(b.Take("gun0"));Assert.IsFalse(b.Take("gun0"));
            b.Tick(345,true);var s=b.Capture();var loaded=new MerchantStock04N();loaded.Restore(s);
            Assert.AreEqual(0,loaded.Available("gun0"));Assert.AreEqual(12,loaded.Level);Assert.AreEqual(555,loaded.Remaining);
            Assert.IsTrue(loaded.Take("gun1"));Assert.AreEqual(0,b.Capture().SoldCounts.FindAll(x=>x==0).Count);
        }
        [Test] public void StockClockFreezesInMenusAndReplenishesOnlyAtTheSavedDeadline()
        {
            var b=new MerchantStock04N();b.Take("gun0");b.Tick(900,false);Assert.AreEqual(900,b.Remaining);Assert.AreEqual(0,b.Available("gun0"));
            Assert.IsFalse(b.Tick(899,true));Assert.AreEqual(0,b.Available("gun0"));
            Assert.IsTrue(b.Tick(1,true));Assert.AreEqual(1,b.Available("gun0"));Assert.AreEqual(2,b.Round);Assert.AreEqual(900,b.Remaining);
        }
        [Test] public void MalformedAndOldStockSavesNormalizeConservatively()
        {
            var b=new MerchantStock04N();b.Restore(null,8);Assert.AreEqual(8,b.Round);Assert.AreEqual(2,b.Available("refuge:seed_core"));
            b.Restore(new MerchantStockSave04N{Round=-1,Level=999,Remaining=float.NaN,SoldKeys=new List<string>{"gun0","gun0","boss_core",null,"refuge:seed_core"},SoldCounts=new List<int>{1,0,99,99,99}});
            Assert.AreEqual(1,b.Round);Assert.AreEqual(100,b.Level);Assert.AreEqual(900,b.Remaining);
            Assert.AreEqual(0,b.Available("gun0"));Assert.AreEqual(0,b.Available("boss_core"));Assert.AreEqual(0,b.Available("refuge:seed_core"));
            Assert.IsFalse(b.Tick(float.PositiveInfinity,true));Assert.IsFalse(b.Tick(-900,true));
        }
        [Test] public void StockGenerationIsDeterministicAndCannotSellSignatureFocuses()
        {
            for(int round=1;round<=10;round++)for(int i=0;i<4;i++)
            {var a=LootEconomy.ShopGun(round,i,12);var b=LootEconomy.ShopGun(round,i,12);
                Assert.AreEqual(a.Seed,b.Seed);Assert.AreEqual(a.Name,b.Name);Assert.AreEqual(-1,a.Unique);
                Assert.That(LootEconomy.BuyPrice(a),Is.GreaterThan(LootEconomy.SellValue(a)));}
        }
        [Test] public void EightPaidTreatmentsProduceDistinctBoundedActualGeometry()
        {
            var variants=new HashSet<string>();
            for(int i=0;i<8;i++)
            {var b=new MeshBuilder(2);GearFittings04N.Bracer(b,i);variants.Add(string.Join(";",b.Vertices));
                Assert.That(b.VertexCount,Is.InRange(100,400));foreach(var v in b.Vertices){Assert.That(v.magnitude,Is.LessThan(.3f));}}
            Assert.AreEqual(8,variants.Count);var invalid=new MeshBuilder(2);GearFittings04N.Bracer(invalid,-1);Assert.AreEqual(0,invalid.VertexCount);
        }
        [Test] public void ShellFittingsGrowOnlyWithinTheExistingFivePaidUpgradeRanks()
        {
            int count=0;for(int rank=1;rank<=5;rank++){var b=new MeshBuilder(1);GearFittings04N.Shell(b,rank);Assert.That(b.VertexCount,Is.GreaterThan(count));count=b.VertexCount;}
            var max=new MeshBuilder(1);GearFittings04N.Shell(max,999);Assert.AreEqual(count,max.VertexCount);
        }
    }
}
