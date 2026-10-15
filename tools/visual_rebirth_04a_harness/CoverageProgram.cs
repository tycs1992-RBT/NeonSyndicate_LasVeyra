// Offline CPU geometry/coverage recorder only. These adapters do not emulate Unity rendering, physics or imports.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NeonSyndicate;
public static class Program
{
    private sealed class Group { public int seen,eligible,excluded,helipadRoofs,variantMask; }
    public static void Main(string[] args)
    {
        CityLayout layout=new CityLayout(61724); layout.Generate();
        SortedDictionary<string,Group> groups=new SortedDictionary<string,Group>();
        List<object> buildings=new List<object>();
        int total=0,eligible=0,excluded=0,helipads=0;
        foreach(Lot lot in layout.Blocks.SelectMany(b=>b.Lots).Concat(layout.DesertLots))
            foreach(Building building in lot.Buildings)
            {
                string district=CityLayout.DistrictAt(building.Footprint.Center(0f)).ToString();
                string key=district+"/"+building.Style;
                if(!groups.ContainsKey(key)) groups.Add(key,new Group());
                Group group=groups[key]; group.seen++;total++;
                bool allowed=VisualRebirth04ACityPlan.CanTheme(building,lot);
                if(allowed) {group.eligible++;eligible++;group.variantMask |= 1<<VisualRebirth04ACityPlan.Variant(building.Seed,building.Style);}
                else {group.excluded++;excluded++;}
                if(building.Roof==RoofType.Helipad) {group.helipadRoofs++;helipads++;}
                buildings.Add(new {district,archetype=building.Style.ToString(),roof=building.Roof.ToString(),
                    footprint=new[]{building.Footprint.X0,building.Footprint.Z0,building.Footprint.X1,building.Footprint.Z1},
                    building.Height,building.Tiers,building.GroundShops,building.Seed,
                    style=VisualRebirth04ACityPlan.StyleAt(building.Footprint.CenterX,building.Footprint.CenterZ),
                    variant=VisualRebirth04ACityPlan.Variant(building.Seed,building.Style),eligible=allowed,
                    exclusion=VisualRebirth04ACityPlan.Exclusion(building,lot)});
            }
        List<Tuple<UnityEngine.Vector3,int>> treeCandidates=new List<Tuple<UnityEngine.Vector3,int>>();
        foreach(UnityEngine.Vector3 point in layout.TreeSpots) treeCandidates.Add(Tuple.Create(point,MathUtil.Hash((int)point.x,(int)point.z)));
        foreach(PropSpot prop in layout.Props) if(prop.Kind==PropKind.SidewalkTree) treeCandidates.Add(Tuple.Create(prop.Pos,prop.Variant+(int)prop.Pos.x*7));
        SortedDictionary<string,int> treeModels=new SortedDictionary<string,int>();
        foreach(Tuple<UnityEngine.Vector3,int> tree in treeCandidates) {
            if(!CrystalCity02ECoverage.IsCityProp(tree.Item1))continue;
            int style=VisualRebirth04ACityPlan.StyleAt(tree.Item1.x,tree.Item1.z);
            string model=style==1||(tree.Item2&3)==0?"cypress_canopy":"willow_canopy";
            if(!treeModels.ContainsKey(model))treeModels[model]=0;treeModels[model]++;
        }
        object report=new {scope="Executed unchanged CityLayout generator and 04A pure eligibility rules using a small managed math adapter (UnityEngine reference execution failed: internal-call runtime methods). This is planned coverage, not evidence of Unity scene instantiation.",seed=61724,
            blocks=layout.Blocks.Count,totalBuildings=total,eligibleBuildings=eligible,excludedBuildings=excluded,protectedHelipadRoofs=helipads,
            treeSpots=layout.TreeSpots.Count,treeCandidates=treeCandidates.Count,plannedTreeModels=treeModels,palmSpots=layout.PalmSpots.Count,roads=layout.Edges.Count,pois=layout.Pois.Count,
            byDistrictAndArchetype=groups,buildings,
            starterPois=layout.Pois.Where(p=>p.Name=="Veyra Lofts" || p.Name=="Mara Solane Street Clinic").Select(p=>new {p.Name,position=new[]{p.Position.x,p.Position.y,p.Position.z}})};
        Console.WriteLine(JsonSerializer.Serialize(report,new JsonSerializerOptions {WriteIndented=true,IncludeFields=true}));
    }
}
