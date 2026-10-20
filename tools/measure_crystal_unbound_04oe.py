#!/usr/bin/env python3
"""Execute actual revision-four/five graph and mesh source. NOT UNITY."""
import measure_crystal_realms_04od as predecessor
base=predecessor.base
base.CODE=base.CODE.replace('new[]{3,4}','new[]{4,5}').replace('if(rev!=4)continue;','if(rev!=5)continue;').replace('04od-','04oe-')
base.CODE=base.CODE.replace('worlds.Add(new{revision=rev,spawn=P(start),','worlds.Add(new{revision=rev,spawn=P(start),oathTerrace=new{position=P(l.OathTerrace04OE),metres=l.Unbound04OE?Route(l,start,l.OathTerrace04OE):null},stormLedge=new{position=P(l.StormLedge04OE),metres=l.Unbound04OE?Route(l,start,l.StormLedge04OE):null},stormCavern=new{position=P(l.StormCavern04OE),metres=l.Unbound04OE?Route(l,start,l.StormCavern04OE):null},')
base.CODE=base.CODE.replace('var district=new MeshBuilder(1);',r'''
var walks=new MeshBuilder(1);foreach(var route in l.RealmRoutes04OB)if(route.Id.StartsWith("04oe/"))GothicRealmGeometry04OB.UnboundRoute04OE(walks,walks,route);exports.Add(Write("04oe-raised-walks-cavern",walks));
foreach(int unique in new[]{23,24}){var blade=new MeshBuilder(3);WeaponModels.BuildRegional04OE(blade,unique,1);exports.Add(Write("04oe-permanent-blade-"+unique,blade));}
foreach(bool azure in new[]{false,true}){var regalia=new MeshBuilder(1);for(int part=0;part<3;part++)RegionalBossArt04OE.Build(regalia,regalia,azure,part);exports.Add(Write("04oe-regalia-"+(azure?"stormroot":"castellan"),regalia));}
var look=Appearance.Malik();var body=BodyBuilder.Build(look,RigLayout.From(look),2);var skin=new MeshBuilder(1);skin.Vertices.AddRange(body.Vertices);skin.Normals.AddRange(body.Normals);((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(skin))[0].AddRange(body.Triangles);exports.Add(Write("04oe-malik-fallback",skin));
var district=new MeshBuilder(1);district.Append(walks);''')
base.CODE=base.CODE.replace('Actual compiled 04OD source topology and geometry.','Actual compiled 04OE source topology and geometry.')
if __name__=='__main__':base.main()
