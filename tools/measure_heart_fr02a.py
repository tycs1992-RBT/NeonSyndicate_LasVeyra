"""Export actual FR02A proposals, protected lot inventory and unchanged H3/H5 source context. Not native admission."""
import measure_heart_fr01e as previous
base=previous.base
add=r'''
List<object> living=new List<object>(),inventory=new List<object>();List<HeartLivingFR02A> admitted=new List<HeartLivingFR02A>();int houses=0,props=0;
foreach(bool isHouse in new[]{false,true})foreach(HeartLivingFR02A p in HeartPlanFR01.LivingProposals(l))if(p.House==isHouse){string reason,aloneReason;bool alone=HeartPlanFR01.LivingClear(l,p,null,out aloneReason);bool source=HeartPlanFR01.LivingClear(l,p,admitted,out reason);bool limit=isHouse?houses>=HeartPlanFR01.LivingHouseLimit:props>=HeartPlanFR01.LivingPropLimit;if(limit){source=false;reason="Bounded module limit.";}RectF area=HeartPlanFR01.LivingFootprint(p,1);living.Add(new{key=p.Key,module=p.Module,route=p.Route,house=p.House,position=P(p.Position),size=P(p.Size),yaw=p.Yaw,area=new[]{area.X0,area.Z0,area.X1,area.Z1},source_clear_alone=alone,alone_reason=aloneReason,source_admitted=source,reason});if(source){admitted.Add(p);if(isHouse)houses++;else props++;}}
foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)if(HeartPlanFR01.LivingSlice(l,b.Footprint.Center(0)))inventory.Add(new{block=block.Id,lot=lot.Seed,seed=b.Seed,poi=lot.PoiName,poi_type=lot.Poi.ToString(),custom=lot.CustomBuilt,sign=b.Sign,roof=b.Roof.ToString(),roof_pad=b.RoofPad04OB,family=b.GothicFamily04OB,rect=new[]{b.Footprint.X0,b.Footprint.Z0,b.Footprint.X1,b.Footprint.Z1},height=b.Height,fr01=HeartPlanFR01.Eligible(l,b),classification=HeartPlanFR01.LivingClassification(l,lot,b)});
'''
base.CODE=base.CODE.replace('Console.WriteLine(JsonSerializer.Serialize(',add+'Console.WriteLine(JsonSerializer.Serialize(')
base.CODE=base.CODE.replace('candidates,sides,legacy_candidates','living,inventory,source_houses=houses,source_props=props,candidates,sides,legacy_candidates')
if __name__=='__main__':base.main()
