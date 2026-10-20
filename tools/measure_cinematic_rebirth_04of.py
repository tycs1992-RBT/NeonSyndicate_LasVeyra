#!/usr/bin/env python3
"""Execute compiled 04OF topology and geometry for source diagnostics. NOT UNITY OR SCREENSHOTS."""
import measure_crystal_unbound_04oe as predecessor
base=predecessor.base
base.CODE=base.CODE.replace('var district=new MeshBuilder(1);',r'''
MeshBuilder cinematic=new MeshBuilder(1);
foreach(RealmRoute04OB route in l.RealmRoutes04OB)GothicRealmGeometry04OB.CinematicRoute04OF(cinematic,cinematic,cinematic,route);
exports.Add(Write("04of-route-decoration-noncolliding",cinematic));
foreach(bool azureCrest in new[]{false,true})
{
    MeshBuilder crest=new MeshBuilder(1);
    foreach(int side in new[]{-1,1})RegionalBossPose04OF.Crest(crest,crest,azureCrest,side);
    exports.Add(Write("04of-articulated-crest-"+(azureCrest?"stormroot":"castellan"),crest));
}
var district=new MeshBuilder(1);''')
base.CODE=base.CODE.replace('Actual compiled 04OE source topology and geometry.','Actual compiled 04OF source topology and geometry. New route decoration is noncolliding; old route collision is unchanged.')
if __name__=='__main__':base.main()
