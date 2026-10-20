using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class GothicRealm04OBTests
    {
        [Test] public void PreviewBankPreservesOriginalSlotNamesAndRejectsUnknownLayouts()
        {
            for (int slot = 0; slot <= SaveSystem.Slots; slot++)
            {
                string original = "lasveyra_save_" + slot + ".json";
                Assert.That(SaveSystem.SlotFileName04OB(slot, 0), Is.EqualTo(original));
                Assert.That(SaveSystem.SlotFileName04OB(slot, 1), Is.EqualTo(original));
                string realm = SaveSystem.SlotFileName04OB(slot, 2);
                Assert.That(realm, Is.Not.EqualTo(original));
                Assert.That(realm, Is.Not.EqualTo(SaveFileTransaction.BackupPath(original)));
                Assert.That(realm, Is.Not.EqualTo(SaveFileTransaction.TempPath(original)));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => SaveSystem.SlotFileName04OB(0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => SaveSystem.SlotFileName04OB(-1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => SaveSystem.SlotFileName04OB(4, 0));
        }
        [Test] public void RepeatedRealmTransactionsCannotRotateOrOverwriteOriginalSaveOrBackup()
        {
            string directory = Path.Combine(Path.GetTempPath(), "realm04ob-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                for (int slot = 0; slot <= SaveSystem.Slots; slot++)
                {
                    string oldPath = Path.Combine(directory, SaveSystem.SlotFileName04OB(slot, 0));
                    string realmPath = Path.Combine(directory, SaveSystem.SlotFileName04OB(slot, 2));
                    File.WriteAllText(oldPath, "original " + slot);
                    File.WriteAllText(SaveFileTransaction.BackupPath(oldPath), "protected backup " + slot);
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        string error;
                        Assert.That(SaveFileTransaction.Write(realmPath, "realm " + attempt, t => t.StartsWith("realm "), out error),
                            Is.EqualTo(SaveFileTransaction.Result.Ok), error);
                    }
                    bool recovered;
                    Assert.That(SaveFileTransaction.ReadValidated(realmPath, t => t.StartsWith("realm "), out recovered), Is.EqualTo("realm 2"));
                    Assert.That(recovered, Is.False);
                    Assert.That(File.ReadAllText(oldPath), Is.EqualTo("original " + slot));
                    Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(oldPath)), Is.EqualTo("protected backup " + slot));
                    Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(realmPath)), Is.EqualTo("realm 1"));
                }
            }
            finally { Directory.Delete(directory, true); }
        }
        private static CityLayout World(int seed,out List<VenueBinding04C> venues)
        {
            CityLayout layout=new CityLayout(seed);layout.Generate();venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(layout.ApplyProfile04O(CityRevision04O.GothicRealm04OB,venues),Is.True);return layout;
        }
        private static float Length(IList<Vector3> path){float n=0f;for(int i=1;i<path.Count;i++)n+=Vector3.Distance(path[i-1],path[i]);return n;}
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void RevisionTwoPreservesCanonicalIdentitiesAndOrderedReceipts(int seed)
        {
            CityLayout old=new CityLayout(seed);old.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(old,InhabitedCityBindings04CTests.ReadCatalog());
            string pois=string.Join(";",old.Pois.Select(p=>p.Id+"/"+p.Name+"/"+p.Type+"/"+p.District));
            string sites=string.Join(";",old.Sites.Select(p=>p.Key+"/"+p.Name));
            int[] counts={old.DataChipSpots.Count,old.CrystalShardSpots.Count,old.SpellStoneSpots.Count,old.CreatureSpots.Count,old.PickupSpots.Count,old.StuntJumps.Count};
            Building[] hosts=venues.Select(v=>v.Building).ToArray();
            Assert.That(old.ApplyProfile04O(CityRevision04O.GothicRealm04OB,venues),Is.True,string.Join(";",venues.Select(v=>v.Definition.Id+" bound="+(v.Building!=null)+" lot="+(v.Lot!=null))));
            Assert.That(string.Join(";",old.Pois.Select(p=>p.Id+"/"+p.Name+"/"+p.Type+"/"+p.District)),Is.EqualTo(pois));
            Assert.That(string.Join(";",old.Sites.Select(p=>p.Key+"/"+p.Name)),Is.EqualTo(sites));
            Assert.That(new[]{old.DataChipSpots.Count,old.CrystalShardSpots.Count,old.SpellStoneSpots.Count,old.CreatureSpots.Count,old.PickupSpots.Count,old.StuntJumps.Count},Is.EqualTo(counts));
            for(int i=0;i<hosts.Length;i++)
            {
                if(hosts[i]!=null)Assert.That(venues[i].Building,Is.SameAs(hosts[i]));
                Assert.That(venues.Count(v=>v.Building==venues[i].Building),Is.EqualTo(1));
                Assert.That(old.IsInsideBuilding(venues[i].Outside,.8f),Is.False,venues[i].Definition.Id);
                if(venues[i].Poi!=null)Assert.That(old.Blocks[venues[i].Poi.BlockId].Lots,Does.Contain(venues[i].Lot));
            }
            List<VenueBinding04C> other;CityLayout again=World(seed,out other);
            Assert.That(string.Join(";",old.Blocks.SelectMany(b=>b.Lots).SelectMany(l=>l.Buildings).Select(b=>b.Seed+"/"+b.Footprint+"/"+b.Height)),
                Is.EqualTo(string.Join(";",again.Blocks.SelectMany(b=>b.Lots).SelectMany(l=>l.Buildings).Select(b=>b.Seed+"/"+b.Footprint+"/"+b.Height))));
        }
        [Test] public void EveryOriginalServiceJoinsThePhysicalCourtNetwork()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);Vector3 home=venues.Find(v=>v.Definition.Id=="lofts").Outside;
            foreach(VenueBinding04C venue in venues)Assert.That(l.FindWalkRoute04O(home,venue.Outside),Is.Not.Empty,venue.Definition.Id);
            foreach(RealmRoute04OB route in l.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
                Assert.That(l.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True,route.Id+" segment "+i);
            Assert.That(l.FindWalkRoute04O(home,l.RealmThreshold04OB),Is.Not.Empty);
        }
        [Test] public void CompressionMeasuresActualRecurringRoutesWithoutChangingMovement()
        {
            CityLayout legacy=new CityLayout(61724);legacy.Generate();List<VenueBinding04C> before=InhabitedCityBindings04C.Select(legacy,InhabitedCityBindings04CTests.ReadCatalog());
            List<VenueBinding04C> after;CityLayout compact=World(61724,out after);
            foreach(string id in new[]{"clinic","forge","garage"})
            {
                List<Vector3> a=legacy.FindWalkRoute04O(before.Find(v=>v.Definition.Id=="lofts").Outside,before.Find(v=>v.Definition.Id==id).Outside);
                List<Vector3> b=compact.FindWalkRoute04O(after.Find(v=>v.Definition.Id=="lofts").Outside,after.Find(v=>v.Definition.Id==id).Outside);
                Assert.That(a,Is.Not.Empty,id);Assert.That(b,Is.Not.Empty,id);Assert.That(Length(b),Is.LessThan(Length(a)*.6f),id);
            }
            Assert.That(compact.Edges.Count,Is.LessThan(legacy.Edges.Count*.5f));
        }
        [Test] public void RevisionsCannotBeMutatedIntoAnotherSavedGeography()
        {
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);
            foreach(int known in new[]{0,1,2})Assert.That(CityRebirthRules04O.Known(known),Is.True);
            Assert.That(CityRebirthRules04O.Known(3),Is.False);
            CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> v=InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(l.ApplyProfile04O(CityRevision04O.GothicRealm04OB,null),Is.False);
            Assert.That(l.ApplyProfile04O(CityRevision04O.Crystalline04O,v),Is.True);
            Assert.That(l.ApplyProfile04O(CityRevision04O.GothicRealm04OB,v),Is.False);
            List<VenueBinding04C> other;CityLayout compact=World(61724,out other);
            Assert.That(compact.ApplyProfile04O(CityRevision04O.GothicRealm04OB,other),Is.True);
            Assert.That(compact.ApplyProfile04O(CityRevision04O.Legacy04N,other),Is.False);
        }
        [Test] public void VehicleGraphHasValidConnectionsAndRequiredTerrainCrossing()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            foreach(RoadEdge e in l.Edges){Assert.That(e.Id,Is.InRange(0,l.Edges.Count-1));Assert.That(l.Nodes[e.A].Edges,Does.Contain(e.Id));Assert.That(l.Nodes[e.B].Edges,Does.Contain(e.Id));}
            foreach(LanePath lane in l.Lanes)
            {Assert.That(lane.Next,Is.Not.Empty,"lane "+lane.Id);foreach(int id in lane.Next)Assert.That(id,Is.InRange(0,l.Lanes.Count-1));}
            Assert.That(l.Edges.Any(e=>e.Name=="Crystal realm approach" && e.HasBridge),Is.True);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void FamilySolidsHaveFiniteOutwardGeometryAndBoundedBudgets(int family)
        {
            Building b=new Building{Seed=61724,Footprint=new RectF(-11,0,11,26),Height=GothicRealmGeometry04OB.FamilyHeight(family),GothicFamily04OB=family};
            MeshBuilder a=new MeshBuilder(1),t=new MeshBuilder(1),c=new MeshBuilder(1);GothicRealmGeometry04OB.Family(a,t,c,b,true);
            Assert.That(a.VertexCount+t.VertexCount+c.VertexCount,Is.InRange(300,15000));
            foreach(MeshBuilder m in new[]{a,t,c})CheckMesh(m);
            // Unlike a solid frontal box, hall/cloister galleries leave real eye-level empty central space.
            if(family==1||family==2)Assert.That(HitTriangle(a,new Vector3(0,2f,-2),Vector3.forward,15f),Is.False);
        }
        [Test] public void ClosedVaultDoesNotFillItsPlayerAndCompanionAperture()
        {
            MeshBuilder arch=new MeshBuilder(1);GothicRealmGeometry04OB.Arch(arch,Vector3.zero,0f,7f,4f,4f,.6f,2f,16);
            CheckMesh(arch);Assert.That(HitTriangle(arch,new Vector3(0,1.7f,-2),Vector3.forward,6f),Is.False);
            Assert.That(HitTriangle(arch,new Vector3(3.8f,1.7f,-2),Vector3.forward,6f),Is.True);
            Assert.That(HitTriangle(arch,new Vector3(0,5f,1),Vector3.up,5f),Is.True);
        }
        [Test] public void SanctuaryRetainsEveryOriginalAnchorAndAddsAUsableDeepDestination()
        {

            VenueLayout04C source=VenueLayoutData04E.Layout("archive");
            VenueExpanded04C expanded=InhabitedCity04CComposition.Expand(source,30f,48f,22f);
            string[] ids=source.anchors.Select(a=>a.id).ToArray();float old=source.anchors.First(a=>a.id=="record3").position[2];
            GothicRoomComposition04OB.Prepare(expanded);
            Assert.That(expanded.anchors.Select(a=>a.id),Is.EqualTo(ids));Assert.That(source.anchors.First(a=>a.id=="record3").position[2],Is.EqualTo(old));
            Assert.That(expanded.anchors.First(a=>a.id=="record3").position,Is.EqualTo(new[]{0f,1.2f,43f}));
            Assert.That(expanded.lights.Length,Is.LessThanOrEqualTo(6));
            Assert.That(expanded.lights.Any(light=>light.position[2]>30f),Is.True);
            Assert.That(expanded.roomDepth,Is.EqualTo(48f));Assert.That(expanded.nodes.Any(n=>n.kind=="upper04A"||n.id=="rear_room_wall"),Is.False);
        }
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void TheOriginalUnderheartPlannerFitsTheRelocatedPressureFrame(int seed)
        {
            List<VenueBinding04C> venues;CityLayout l=World(seed,out venues);VenueBinding04C pressure=venues.Find(v=>v.Definition.Id==VenueIds04E.Pressure);
            UnderheartPlanner.Choice choice=UnderheartPlanner.Choose(l,new UnderheartFrame(pressure.Origin.x,pressure.Origin.z,pressure.Yaw),true);
            Assert.That(choice.Layout,Is.Not.Null,string.Join(";",choice.Rejected));
            Assert.That(UnderheartRules.Validate(choice.Layout),Is.Empty);
            Assert.That(choice.Layout.Anchor("service.clamp_hollow"),Is.Not.Null);
        }
        [Test] public void SharedBatchAppendPreservesOffsetTriangleIndices()
        {
            MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1);GothicRealmGeometry04OB.Box(a,Vector3.zero,0,Vector3.zero,Vector3.one);
            GothicRealmGeometry04OB.Crystal(b,new Vector3(5,0,2),new Vector3(2,4,2),.2f,1);
            int count=a.VertexCount;int[] expected=Indices(b).Select(i=>i+count).ToArray();a.Append(b);
            Assert.That(Indices(a).Skip(36),Is.EqualTo(expected));CheckMesh(a);
        }
        [Test] public void LandscapeIsActuallyAdmittedBesideTheSettlement()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            Assert.That(l.Landscape04OB.Count,Is.GreaterThanOrEqualTo(4));
            foreach(RealmRock04OB formation in l.Landscape04OB)
            {
                Assert.That(formation.Size.y,Is.InRange(20f,40f));
                MeshBuilder m=new MeshBuilder(1);GothicRealmGeometry04OB.Rock(m,formation.Position,formation.Size,formation.Seed);CheckMesh(m);
            }
        }
        [Test] public void OriginalRooftopReceiptsHaveActualSupportedServiceLandings()
        {
            CityLayout original=new CityLayout(61724);original.Generate();List<VenueBinding04C> before=InhabitedCityBindings04C.Select(original,InhabitedCityBindings04CTests.ReadCatalog());
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            List<Vector3>[] oldPoints={original.DataChipSpots,original.CrystalShardSpots,original.SpellStoneSpots,original.PickupSpots,original.RadioMastSpots};
            List<Vector3>[] newPoints={l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.PickupSpots,l.RadioMastSpots};
            foreach(VenueBinding04C old in before)
            {
                if(old.Building==null)continue;VenueBinding04C current=venues.Find(v=>v.Definition.Id==old.Definition.Id);
                MeshBuilder stone=new MeshBuilder(1),metal=new MeshBuilder(1),crystal=new MeshBuilder(1);
                GothicRealmGeometry04OB.Venue(stone,metal,crystal,current.Width,current.Depth,current.Building.Height,current.Definition.Id,true,current.Building.RoofPad04OB);
                for(int set=0;set<oldPoints.Length;set++)for(int i=0;i<oldPoints[set].Count;i++)
                {
                    Vector3 p=oldPoints[set][i];if(!old.Building.Footprint.Contains(p)||p.y<=old.Building.Height-2f)continue;
                    Vector3 local=InhabitedCityBindings04C.WorldToLocal(current.Origin,current.Yaw,newPoints[set][i]);
                    Assert.That(current.Building.RoofPad04OB,Is.True);
                    Assert.That(HitTriangle(stone,local+Vector3.up*2f,Vector3.down,4f),Is.True,current.Definition.Id+" receipt "+set+"/"+i);
                }
            }
        }
        [Test] public void ThePhysicalVehicleCircuitDoesNotCrossStandingBuildingFootprints()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            foreach(RoadEdge edge in l.Edges)
            {
                if(edge.Name!="Mineral service circuit")continue;Vector3 a=l.Nodes[edge.A].Pos,b=l.Nodes[edge.B].Pos;
                for(float t=0;t<=1.001f;t+=.02f)Assert.That(l.IsInsideBuilding(Vector3.Lerp(a,b,t),3.5f),Is.False,"service circuit edge "+edge.Id);
            }
        }
        [Test] public void WideClearanceSeesMovedBuildingsAcrossHashCellBoundaries()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            VenueBinding04C forge=venues.Find(v=>v.Definition.Id=="forge");Vector3 point=new Vector3(101f,CityLayout.CurbHeight,115f);
            Assert.That(forge.Building.Footprint.Inset(-12f).Contains(point),Is.True);
            Assert.That(l.IsInsideBuilding(point,12f),Is.True);
            foreach(VenueBinding04C v in venues)
            {
                Vector3 p=v.Building.Footprint.Center(CityLayout.CurbHeight);p.x=v.Building.Footprint.X1+11f;
                Assert.That(l.IsInsideBuilding(p,12f),Is.True,v.Definition.Id);
            }
        }
        [Test] public void CivilianSpawnAndCommuteQueriesCannotSelectAnIsolatedNode()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            Assert.That(l.WalkNodes,Is.Not.Empty);
            for(int i=0;i<l.WalkNodes.Count;i++){Assert.That(l.WalkNodes[i].Id,Is.EqualTo(i));Assert.That(l.WalkNodes[i].Links,Is.Not.Empty);}
            foreach(Block block in l.Blocks)foreach(int id in block.WalkNodes)Assert.That(id,Is.InRange(0,l.WalkNodes.Count-1));
            Assert.That(l.RealmRoutes04OB.Any(route=>route.Id.StartsWith("neighbor-path/")),Is.True);
            foreach(VenueBinding04C venue in venues)Assert.That(l.NearestWalkNode(venue.Outside,40f),Is.GreaterThanOrEqualTo(0),venue.Definition.Id);
        }
        [Test] public void ArchedWindowsAreDeepOpeningsWithSupportedMasonryAboveAndBelow()
        {
            MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1);
            GothicRealmGeometry04OB.ArchedWallBay(stone,trim,Vector3.zero,0f,5f,6f,.8f,true);
            CheckMesh(stone);CheckMesh(trim);stone.Append(trim);
            Assert.That(HitTriangle(stone,new Vector3(0f,2f,-1f),Vector3.forward,3f),Is.False,"Window aperture is filled");
            Assert.That(HitTriangle(stone,new Vector3(0f,.5f,-1f),Vector3.forward,3f),Is.True,"Sill has no wall");
            Assert.That(HitTriangle(stone,new Vector3(0f,5f,-1f),Vector3.forward,3f),Is.True,"Head has no wall");
            Assert.That(HitTriangle(stone,new Vector3(.5f,2f,.4f),Vector3.right,2f),Is.True,"Reveal has no depth");
        }
        [Test] public void OriginalSanctuaryHasASecondSupportedPhysicalRouteToItsDais()
        {
            MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);
            GothicRealmGeometry04OB.Venue(stone,trim,crystal,30f,48f,22f,"archive",true);
            CheckMesh(stone);CheckMesh(trim);CheckMesh(crystal);
            for(float z=25.5f;z<43f;z+=.5f)
            {
                float floor=z<31f?1.2f*(Mathf.Floor((z-25f)/.75f)+1f)/8f:1.2f;
                Assert.That(HitTriangle(stone,new Vector3(6.2f,floor+.12f,z),Vector3.down,.3f),Is.True,"Gallery/stair gap at "+z);
            }
            for(float x=3.4f;x<=6.2f;x+=.2f)Assert.That(HitTriangle(stone,new Vector3(x,1.3f,42f),Vector3.down,.2f),Is.True,"Dais connection gap");
            Assert.That(HitTriangle(trim,new Vector3(5.05f,2.25f,35f),Vector3.right,.4f),Is.True,"Gallery lacks physical guard rail");
            Assert.That(HitTriangle(trim,new Vector3(6.2f,1.7f,31f),Vector3.forward,9f),Is.False,"Rail blocks the gallery center");
        }
        [Test] public void CloisterWingsHaveOpenEntryAndHollowOccupiedSpace()
        {
            Building b=new Building{Seed=61724,Footprint=new RectF(-11f,0f,11f,26f),Height=11f,GothicFamily04OB=1};
            MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);
            GothicRealmGeometry04OB.Family(stone,trim,crystal,b,true);stone.Append(trim);stone.Append(crystal);
            Assert.That(HitTriangle(stone,new Vector3(-7.7f,1.7f,2f),Vector3.forward,9f),Is.False,"Cloister wing is a solid box");
            Assert.That(HitTriangle(stone,new Vector3(-7.7f,2f,8f),Vector3.down,3f),Is.True,"Wing has no floor");
        }
        [Test] public void PreviewQuestRequirementsCannotLeakIntoLegacyOrBypassTheWorldWithFreeRoam()
        {
            QuestDef scoped=new QuestDef{Requires=new QuestRequirements{WorldRevision=2}};
            foreach(int revision in new[]{0,1,2})
            {
                QuestProgress p=new QuestProgress{WorldRevision=revision,FreeRoam=true};
                Assert.That(QuestLogic.MissingRequirement(scoped,p,null)==null,Is.EqualTo(revision==2));
                Assert.That(QuestLogic.MissingRequirement(new QuestDef(),p,null),Is.Null,"Old data must remain unrestricted");
            }
        }
        [Test] public void TheRealmSurveyIsPhysicalFiniteAndUsesOriginalGuildAndMaterialOwners()
        {
            List<string> errors=new List<string>();
            QuestFile file=QuestJson.Read<QuestFile>(System.IO.File.ReadAllText(QuestTestData.ResourcesDir+"/Quests/gothic_realm_04ob.json"),"gothic_realm_04ob.json",errors,null);
            Assert.That(errors,Is.Empty);QuestDef q=file.Quests.Single();
            Assert.That(q.VenueGiver,Is.EqualTo("forge"));Assert.That(q.Requires.WorldRevision,Is.EqualTo(2));
            Assert.That(q.Phases.SelectMany(p=>p.Steps).Count(s=>s.Type=="Interact"),Is.EqualTo(2));
            Assert.That(q.Phases.SelectMany(p=>p.Steps).Any(s=>s.Type=="Collect"||s.Type=="Claim"),Is.False,"No replayable cache or entitlement grants");
            List<VenueBinding04C> venues;CityLayout layout=World(61724,out venues);
            QuestStep threshold=q.Phases.SelectMany(p=>p.Steps).First(s=>s.Type=="GoTo"&&s.At=="World");
            Assert.That(new Vector3(threshold.OffsetX,CityLayout.CurbHeight,threshold.OffsetZ),Is.EqualTo(layout.RealmThreshold04OB));
            Assert.That(q.Reward.Materials.Sum(m=>m.Count),Is.EqualTo(3));Assert.That(q.Reward.Rep.Single().Faction,Is.EqualTo("forgewrights"));
        }
        [Test] public void WideIndustrialFurnaceAndMineralDwellingsStayWithinTheirAdmittedFootprint()
        {
            foreach(int family in new[]{3,5})
            {
                Building b=new Building{Seed=63542,Footprint=new RectF(-101f,0f,101f,12.44f),Height=GothicRealmGeometry04OB.FamilyHeight(family),GothicFamily04OB=family};
                MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),mineral=new MeshBuilder(1);
                GothicRealmGeometry04OB.Family(stone,trim,mineral,b,true);
                foreach(MeshBuilder m in new[]{stone,trim,mineral})foreach(Vector3 p in m.Vertices)
                    Assert.That(b.Footprint.Inset(-1f).Contains(p),Is.True,"Source nav footprint misses solid "+family+" at "+p);
            }
        }
        [Test] public void CavernCanopiesAreCeilingRootedBoundedAndKeepCompanionHeadroom()
        {
            int candidates=0;
            for(int variant=0;variant<UnderheartLayout.VariantCount;variant++)
            foreach(UhCell cell in UnderheartLayout.Create(variant).Cells)
            for(int i=0;i<3;i++)
            {
                Vector3 root;float drop;
                if(!GothicRealmGeometry04OB.TryCavernCanopy(cell,i,out root,out drop))continue;
                candidates++;
                MeshBuilder m=new MeshBuilder(1);GothicRealmGeometry04OB.HangingMineral(m,root,drop,cell.Index*193+i);CheckMesh(m);
                foreach(Vector3 p in m.Vertices)
                {
                    Assert.That(cell.ContainsXZ(p.x,p.z,0f),Is.True);
                    Assert.That(p.y-cell.FloorAt(p.x,p.z),Is.GreaterThan(3.2f));
                    Assert.That(p.y,Is.LessThanOrEqualTo(root.y+.001f));
                }
            }
            Assert.That(candidates,Is.GreaterThan(0));
        }
        [TestCase("stone")][TestCase("paving")][TestCase("amethyst")][TestCase("teal")]
        public void AuthoredSurfacesTileWithFiniteCoherentReliefAndUnitNormals(string key)
        {
            float lo=1f,hi=0f;
            for(int y=0;y<=32;y++)for(int x=0;x<=32;x++)
            {
                float u=x/32f,v=y/32f;float h=GothicRealmMaterials04OB.SurfaceHeight04OB(key,u,v);
                lo=Mathf.Min(lo,h);hi=Mathf.Max(hi,h);
                Assert.That(h,Is.InRange(0f,1f));
                Assert.That(GothicRealmMaterials04OB.SurfaceHeight04OB(key,u+1f,v),Is.EqualTo(h).Within(.0001f));
                Assert.That(GothicRealmMaterials04OB.SurfaceHeight04OB(key,u,v+1f),Is.EqualTo(h).Within(.0001f));
                Vector3 n=GothicRealmMaterials04OB.SurfaceNormal04OB(key,u,v);Assert.That(n.magnitude,Is.EqualTo(1f).Within(.0001f));
                Assert.That(n.z,Is.GreaterThan(.1f));Color c=GothicRealmMaterials04OB.SurfaceColor04OB(key,u,v);
                Assert.That(c.r,Is.InRange(.4f,1f));Assert.That(c.a,Is.EqualTo(1f));
            }
            Assert.That(hi-lo,Is.GreaterThan(.15f),"A uniform palette is not a produced surface");
        }
        [Test] public void PreviouslyUnreplacedLowReceiptBuildingsGainNewShellsWithoutMovingRoofOwnership()
        {
            CityLayout original=new CityLayout(61724);original.Generate();
            List<VenueBinding04C> venues;CityLayout layout=World(61724,out venues);int houses=0,receipts=0;
            foreach(RealmCoverage04OB row in layout.Coverage04OB)
            {
                Building b=row.Source;if(b.GothicFamily04OB!=7)continue;houses++;
                Assert.That(b.Footprint,Is.EqualTo(row.Before));
                Building before=original.Blocks.SelectMany(block=>block.Lots).SelectMany(lot=>lot.Buildings).Single(old=>old.Seed==b.Seed && old.Footprint.Equals(row.Before));
                Assert.That(b.Height,Is.EqualTo(before.Height));
                MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),mineral=new MeshBuilder(1);
                GothicRealmGeometry04OB.Family(stone,trim,mineral,b,true);CheckMesh(stone);CheckMesh(trim);CheckMesh(mineral);
                foreach(float x in new[]{-.25f,0f,.25f})
                    Assert.That(HitTriangle(stone,new Vector3(before.Footprint.CenterX+before.Footprint.Width*x,CityLayout.CurbHeight+before.Height+.1f,before.Footprint.CenterZ),Vector3.down,.2f),Is.True,"Original roof plane lost");
                List<Vector3>[] oldSets={original.DataChipSpots,original.CrystalShardSpots,original.SpellStoneSpots,original.CreatureSpots,original.PickupSpots,original.RadioMastSpots};
                List<Vector3>[] newSets={layout.DataChipSpots,layout.CrystalShardSpots,layout.SpellStoneSpots,layout.CreatureSpots,layout.PickupSpots,layout.RadioMastSpots};
                for(int set=0;set<oldSets.Length;set++)for(int i=0;i<oldSets[set].Count;i++)
                    if(before.Footprint.Inset(-4f).Contains(oldSets[set][i])){receipts++;Assert.That(newSets[set][i],Is.EqualTo(oldSets[set][i]),"Protected receipt was moved");}
                for(int i=0;i<original.StuntJumps.Count;i++)if(before.Footprint.Inset(-4f).Contains(original.StuntJumps[i].Position))
                {receipts++;Assert.That(layout.StuntJumps[i].Position,Is.EqualTo(original.StuntJumps[i].Position));}
            }
            Assert.That(houses,Is.EqualTo(6));Assert.That(receipts,Is.GreaterThan(0));
            Assert.That(layout.Coverage04OB.Any(row=>row.Disposition=="retained low special/receipt structure"),Is.False);
            Assert.That(layout.Coverage04OB.Single(row=>row.Identity=="Rust Canal Boathouse").Source.GothicFamily04OB,Is.EqualTo(7));
        }
        [Test] public void SurfaceGrottoIsAnOpenSolidCaveOnTheOriginalPlayableThresholdRoute()
        {
            List<VenueBinding04C> venues;CityLayout layout=World(61724,out venues);
            Building cave=layout.RealmBuildings04OB.Single(b=>b.GothicFamily04OB==8);
            MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),mineral=new MeshBuilder(1);
            GothicRealmGeometry04OB.Family(stone,trim,mineral,cave,true);CheckMesh(stone);CheckMesh(mineral);stone.Append(mineral);
            Assert.That(HitTriangle(stone,new Vector3(12f,2f,304f),Vector3.forward,23f),Is.False,"The cave bore is blocked");
            Assert.That(HitTriangle(stone,new Vector3(12f,2f,316f),Vector3.up,12f),Is.True,"No real cave roof");
            Assert.That(HitTriangle(stone,new Vector3(12f,3.15f,316f),Vector3.down,8f),Is.False,"Original short ground probe must start below the roof");
            Assert.That(HitTriangle(stone,new Vector3(12f,2f,316f),Vector3.right,18f),Is.True,"No real cave side");
            Assert.That(layout.IsInsideBuilding(layout.RealmGrotto04OB,1.8f),Is.False,"Companion/navigation cannot enter");
            Assert.That(layout.IsInsideBuilding(new Vector3(23f,.15f,316f),.4f),Is.True,"The cave wall is missing from ground admission");
            Assert.That(layout.FindWalkRoute04O(venues.Find(v=>v.Definition.Id=="lofts").Outside,layout.RealmGrotto04OB),Is.Not.Empty);
            foreach(RealmRoute04OB route in layout.RealmRoutes04OB.Where(r=>r.Id.StartsWith("grotto-")))
                for(int i=1;i<route.Points.Count;i++)Assert.That(layout.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True,route.Id);
            List<string> errors=new List<string>();QuestFile file=QuestJson.Read<QuestFile>(File.ReadAllText(QuestTestData.ResourcesDir+"/Quests/gothic_realm_04ob.json"),"grotto quest",errors,null);
            QuestStep sample=file.Quests.Single().Phases.SelectMany(p=>p.Steps).Single(p=>p.Type=="Interact"&&p.At=="World");
            Assert.That(new Vector3(sample.OffsetX,CityLayout.CurbHeight,sample.OffsetZ),Is.EqualTo(layout.RealmGrotto04OB));
        }
        private static List<int> Indices(MeshBuilder m){return ((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m))[0];}
        private static void CheckMesh(MeshBuilder m)
        {
            List<int> t=Indices(m);Assert.That(t.Count%3,Is.Zero);
            foreach(Vector3 p in m.Vertices)Assert.That(MathUtil.IsFinite(p),Is.True);
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=m.Vertices[t[i]],b=m.Vertices[t[i+1]],c=m.Vertices[t[i+2]];
                Assert.That(Vector3.Cross(b-a,c-a).sqrMagnitude,Is.GreaterThan(1e-10f));
                Assert.That(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,m.Normals[t[i]]),Is.GreaterThan(.98f));
            }
        }
        private static bool HitTriangle(MeshBuilder m,Vector3 origin,Vector3 direction,float range)
        {
            List<int> t=Indices(m);
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=m.Vertices[t[i]],e1=m.Vertices[t[i+1]]-a,e2=m.Vertices[t[i+2]]-a;
                Vector3 h=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,h);if(Mathf.Abs(determinant)<1e-7f)continue;
                float f=1f/determinant;Vector3 s=origin-a;float u=f*Vector3.Dot(s,h);if(u<0||u>1)continue;
                Vector3 q=Vector3.Cross(s,e1);float v=f*Vector3.Dot(direction,q);if(v<0||u+v>1)continue;
                float distance=f*Vector3.Dot(e2,q);if(distance>=0 && distance<=range)return true;
            }
            return false;
        }
    }
}
