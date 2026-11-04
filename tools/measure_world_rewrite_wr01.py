#!/usr/bin/env python3
"""Export compiled source coverage, not native admission, trigger volumes, rendering or gameplay evidence."""
import argparse
import collections
import contextlib
import copy
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys

import measure_city_rebirth_04o as compiler

CODE = r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class Probe {
static float[] P(Vector3 p){return new[]{p.x,p.y,p.z};}
static float[] R(RectF r){return new[]{r.X0,r.Z0,r.X1,r.Z1};}
static object B(Vector3 min,Vector3 max,string basis){return new{min=P(min),max=P(max),center=P((min+max)*.5f),size=P(max-min),frame="world",measurement=basis};}
static object GeometryBounds(MeshBuilder mesh,Vector3 origin,float yaw){if(mesh.VertexCount==0)return null;Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);foreach(Vector3 v in mesh.Vertices){Vector3 p=InhabitedCityBindings04C.LocalToWorld(origin,yaw,v);if(!MathUtil.IsFinite(p))throw new Exception("Nonfinite source geometry");min=Vector3.Min(min,p);max=Vector3.Max(max,p);}return B(min,max,"Actual compiled source structural vertices; no native collider or renderer observed.");}
static long Chunk(Vector3 p){return ((long)Mathf.FloorToInt(p.x/160f)<<32)^(uint)Mathf.FloorToInt(p.z/160f);}
static T Read<T>(string path)where T:class,new(){T value=JsonSerializer.Deserialize<T>(File.ReadAllText(path),new JsonSerializerOptions{IncludeFields=true});if(value==null)throw new Exception("Missing authored JSON: "+path);return value;}
static List<object> Receipts(CityLayout l,Building b){List<object> result=new List<object>();string[] kinds={"DataChipSpots","CrystalShardSpots","SpellStoneSpots","CreatureSpots","PickupSpots","RadioMastSpots"};List<Vector3>[] points={l.DataChipSpots,l.CrystalShardSpots,l.SpellStoneSpots,l.CreatureSpots,l.PickupSpots,l.RadioMastSpots};for(int k=0;k<kinds.Length;k++)for(int i=0;i<points[k].Count;i++)if(b.Footprint.Inset(-4).Contains(points[k][i]))result.Add(new{owner="CityLayout."+kinds[k],index=i,position=P(points[k][i])});for(int i=0;i<l.StuntJumps.Count;i++)if(b.Footprint.Inset(-4).Contains(l.StuntJumps[i].Position))result.Add(new{owner="CityLayout.StuntJumps",index=i,position=P(l.StuntJumps[i].Position)});return result;}
static Dictionary<string,object> Row(string stable,string owner,string classification,string reason){return new Dictionary<string,object>{{"stableId",stable},{"worldRevision",10},{"sourceOwner",owner},{"replacementClass",classification},{"reason",reason},{"nativeAdmission","NOT RUN"},{"nativeActiveCount",null},{"nativeRefusalCount",null},{"questAnchors",new object[0]},{"npcAnchors",new object[0]},{"portalAnchors",new object[0]},{"questRuntimeTriggerVolumes","NOT RUN; authored source tokens are inventoried separately from native trigger objects."}};}
public static void Main(string[] args){
string resource=Path.GetDirectoryName(args[0]);string root=Path.GetFullPath(Path.Combine(resource,"..","..","..",".."));Input input=Read<Input>(args[0]);CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(l,new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()});if(!l.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues))throw new Exception("Revision 10 rejected");
Dictionary<string,MeshBuilder> art=new Dictionary<string,MeshBuilder>();foreach(string id in AscensionArtH2.Models)art.Add(id,AscensionArtH2.Decode(File.ReadAllBytes(Path.Combine(resource,"..","AscensionH2","Meshes",id+"_high.bytes"))));
Dictionary<string,object[]> anchors=new Dictionary<string,object[]>();Dictionary<string,VenueExpanded04C> compositions=new Dictionary<string,VenueExpanded04C>();foreach(VenueBinding04C v in venues){VenueLayout04C inputRoom=Read<VenueLayout04C>(Path.Combine(resource,"Layouts",v.Definition.Layout+".json"));VenueExpanded04C room=InhabitedCity04CComposition.Expand(inputRoom,v.Width,v.Depth,v.Building.Height);GothicRoomComposition04OB.Prepare(room);GothicHeartH3.PrepareRoom(room,GothicCivicH5.Replaces(v.Definition.Id));compositions.Add(v.Definition.Id,room);anchors.Add(v.Definition.Id,room.anchors.Select(a=>(object)new{token="Venue:"+v.Definition.Id+"."+a.id,id=a.id,localPosition=a.position,worldPosition=P(InhabitedCityBindings04C.LocalToWorld(v.Origin,v.Yaw,new Vector3(a.position[0],a.position[1],a.position[2]))),yaw=v.Yaw+a.yaw,sourceOwner="VenueExpanded04C.anchors",nativeObserved=false}).ToArray());}
List<Dictionary<string,object>> entries=new List<Dictionary<string,object>>();
foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings){if(MathUtil.FlatDistance(b.Footprint.Center(0),l.RealmCourt04OB)>650f)continue;VenueBinding04C service=venues.Find(v=>v.Building==b);HeartRewritePlanWR01 plan;string planReason;bool rewrite=service!=null?HeartPlanFR01.TryRewritePlanWR01(l,service,out plan,out planReason):HeartPlanFR01.TryRewriteBuildingPlanWR01(l,b,out plan,out planReason);List<object> receipts=Receipts(l,b);bool protectedOwner=service!=null||lot.CustomBuilt||lot.Poi!=PoiType.None||!string.IsNullOrEmpty(lot.PoiName)||b.GothicFamily04OB==0||b.GothicFamily04OB>=7||b.RoofPad04OB||b.Roof!=RoofType.Flat||receipts.Count>0;string classification=rewrite?"REPLACE_VISUAL":protectedOwner?"PROTECTED":"NATIVE_COLLISION_REVIEW";string stable=rewrite?plan.StableId:service!=null?"revision:10/venue:"+service.Definition.Id+"/seed:"+b.Seed+"/service":"revision:10/lot:"+lot.Seed+"/seed:"+b.Seed;string reason=rewrite?planReason:protectedOwner?"Existing service, named POI, receipt, roof, grotto or unresolved family remains with its current owner.":"Original open halls, stairs, ledges or passage topology is not admitted to another full shell. Native collision and portal review required.";string owner=service!=null?"VenueView04C.GothicHeartShellH3":b.GothicFamily04OB>0?"CityBuilder.Gothic04OB.BuildGothicBuilding04OB":b.RebirthFamily04O>0?"CityBuilder.Rebirth04O.BuildRebirth04O":"CityBuilder.BuildBuilding";Dictionary<string,object> row=Row(stable,owner,classification,reason);row["seed"]=b.Seed;row["lotSeed"]=lot.Seed;row["blockId"]=block.Id;row["family"]=b.GothicFamily04OB;row["currentVisualCategory"]=service!=null?"existing civic/service shell":"existing generated family "+b.GothicFamily04OB;row["poiName"]=lot.PoiName;row["poiType"]=lot.Poi.ToString();row["serviceOwner"]=service!=null?service.Definition.Id:"";row["worldFootprint"]=R(b.Footprint);row["nominalHeight"]=b.Height;row["roofType"]=b.Roof.ToString();row["roofPad"]=b.RoofPad04OB;row["receipts"]=receipts;row["fr01ExistingCandidate"]=HeartPlanFR01.Eligible(l,b);row["collisionOwner"]=service!=null?"VenueView04C original room and H3 fixed-asset service shell / "+service.Definition.Id:b.GothicFamily04OB>0?"CityBuilder.Gothic04OB / 04O-B exact compound solid":"CityBuilder original building collision";row["collisionPolicy"]=rewrite?plan.CollisionPolicy:"PRESERVE_ORIGINAL";row["risk"]=rewrite?"HIGH: preview only, native contact and flight review required":protectedOwner?"PROTECTED":"HIGH: not admitted";row["proposedFantasyFamily"]=rewrite?plan.Family:"UNSPECIFIED, retained";row["legacyBatch"]=service!=null?"Existing per-venue VisualRebirth04ABatch":("160m chunk "+Chunk(b.Footprint.Center(0))+", per-material stone/trim/mineral groups");row["sharedBatchMembership"]=rewrite?(service!=null?"Only slate_roof/mineral_pier isolated from the existing venue batch":"Three exact original material appends isolated before shared city merging"):"Original batch retained";row["visualRoot"]=rewrite?(service!=null?"Visual Rebirth 04A / WR01 isolated legacy / "+plan.StableId:"WR01 isolated original skyline / "+b.Seed):"Original existing source batch; native object path NOT RUN";row["replacementScope"]=rewrite?(service!=null?"Civic crown only; all rooms, doors, supporting structure and full original collider remain":"One audited skyline emitter; full original source remains as physical witness, landing and receipts unchanged"):"No WR01 suppression";
MeshBuilder geometry=new MeshBuilder(1);if(service!=null&&(GothicHeartH3.Replaces(service.Definition.Id)||GothicCivicH5.Replaces(service.Definition.Id))){MeshBuilder mineral=new MeshBuilder(1);bool civic=GothicCivicH5.Replaces(service.Definition.Id);if(civic)GothicCivicH5.Structure(geometry,service,mineral);else GothicHeartH3.Structure(geometry,service);geometry.Append(mineral);foreach(ArtPlacementH2 p in civic?GothicCivicH5.Plan(service):GothicHeartH3.ServicePlan(service))AscensionArtH2.Append(geometry,art[p.Model],p.Position,p.Yaw,p.Scale);row["bounds"]=GeometryBounds(geometry,service.Origin,service.Yaw);}else if(b.GothicFamily04OB>0){GothicRealmGeometry04OB.Family(geometry,geometry,geometry,b,true);row["bounds"]=GeometryBounds(geometry,Vector3.zero,0);}else row["bounds"]=B(new Vector3(b.Footprint.X0,CityLayout.CurbHeight,b.Footprint.Z0),new Vector3(b.Footprint.X1,CityLayout.CurbHeight+b.Height,b.Footprint.Z1),"Nominal source footprint and height envelope, not a measured renderer bound.");
if(service!=null){row["serviceAnchors"]=anchors[service.Definition.Id];row["portalAnchors"]=new object[]{new{kind="existing venue binding",origin=P(service.Origin),outside=P(service.Outside),entrance=P(service.Entrance),yaw=service.Yaw,sourceOwner="VenueBinding04C",nativeObserved=false}};row["npcAnchors"]=CastRoster04E.All.Where(c=>c.Venue==service.Definition.Id||c.RemoteVenue==service.Definition.Id).Select(c=>(object)new{id=c.Id,name=c.Name,post=c.Post.ToString(),venue=c.Venue,anchor=c.Anchor,remoteVenue=c.RemoteVenue,remoteAnchor=c.RemoteAnchor,onDemand=c.OnDemand,storyIdentity=c.StoryIdentity,storyUnlock=c.StoryUnlock,sourceOwner="CastRoster04E",nativeSpawned="NOT RUN"}).ToArray();}else{row["serviceAnchors"]=new object[0];row["npcAnchors"]=l.Pois.Where(p=>p.Type==PoiType.Contact&&b.Footprint.Inset(-12).Contains(p.Position)).Select(p=>(object)new{name=p.Name,position=P(p.Position),sourceOwner="CityLayout.Pois Contact",association="Source position within parcel plus12m; no new ownership is inferred",nativeSpawned="NOT RUN"}).ToArray();}
if(rewrite&&service!=null){MeshBuilder selected=new MeshBuilder(1);foreach(ArtPlacementH2 part in GothicCivicH5.Plan(service))if(HeartPlanFR01.CrownPartWR01(part.Model))AscensionArtH2.Append(selected,art[part.Model],part.Position,part.Yaw,part.Scale);row["isolatedVisualBounds"]=GeometryBounds(selected,service.Origin,service.Yaw);}if(rewrite){row["fitPolicy"]=plan.FitPolicy;row["fallbackPolicy"]=plan.FallbackPolicy;row["requiredPortalContract"]=plan.PortalContract;row["requiredPortalLocalCenter"]=P(plan.PortalCenter);row["requiredPortalLocalSize"]=P(plan.PortalSize);}entries.Add(row);}
VenueBinding04C pressure=venues.Single(v=>v.Definition.Id=="pressure");UnderheartFrame frame=new UnderheartFrame(pressure.Origin.x,pressure.Origin.z,pressure.Yaw);UnderheartPlanner.Choice choice=UnderheartPlanner.Choose(l,frame,true);if(choice.Layout==null)throw new Exception("No source Underheart variant fits");UnderheartLayout under=choice.Layout;UpperUnderheartRewriteRules rules=new UpperUnderheartRewriteRules();foreach(UhCell cell in under.Cells){if(!rules.Eligible(cell))continue;RectF world=frame.CellToWorld(cell);Dictionary<string,object> row=Row("under:"+cell.Id,"UnderheartBuilder.WorldRewrite","REPLACE_VISUAL","Existing upper-cell shell and selected industrial material groups are isolated; candidate reuses exact shell meshes plus overhead ribs. Doors, fixtures, gates, hazards, collision and streaming remain under original owners.");row["cellId"]=cell.Id;row["cellIndex"]=cell.Index;row["region"]=cell.Region;row["theme"]=cell.Theme.ToString();row["cellKind"]=cell.Kind.ToString();row["serviceOwner"]="UnderheartWorld / Pressure House";row["sourceFrame"]=new{origin=P(new Vector3(frame.OriginX,0,frame.OriginZ)),yaw=frame.Yaw,variant=under.Variant};row["bounds"]=B(new Vector3(world.X0,cell.MinFloor,world.Z0),new Vector3(world.X1,cell.Top,world.Z1),"Actual compiled cell walk volume; decorative mesh bounds and native renderer bounds are distinct.");row["worldFootprint"]=R(world);row["floorLo"]=cell.FloorLo;row["floorHi"]=cell.FloorHi;row["headroom"]=cell.Height;row["safe"]=cell.Safe;row["vehicle"]=cell.Vehicle;row["visualRoot"]="Retained upper region / "+cell.Id;row["rendererGroup"]="Masonry upper region / "+cell.Id;row["legacyBatch"]="Existing UnderheartBuilder region / "+cell.Region+", cell-scoped shell/dressing bucket";row["sharedBatchMembership"]="One passive visual pair beneath the same original region streaming parent";row["collisionOwner"]="UnderheartBuilder original floor, wall, mesh and doorway collision outside both visual roots";row["collisionPolicy"]="NATIVE_COLLISION_REVIEW";row["risk"]="Preview only, native stair, camera and contact review required";row["proposedFantasyFamily"]="UpperUnderheartMasonry";row["fitPolicy"]="Original cell dimensions, source shell and floor heights retained; material substitution and decorative ribs only, minimum2.8m headroom";row["fallbackPolicy"]="Keep original complete cell presentation when required shared maps, shell or candidate validation fails";row["portalAnchors"]=under.Portals.Where(p=>p.A==cell.Index||p.B==cell.Index).Select(p=>(object)new{index=p.Index,otherCell=under.Cells[p.Other(cell.Index)].Id,kind=p.Kind.ToString(),width=p.Width,height=p.Height,localPosition=P(p.Center),worldPosition=P(frame.ToWorld(p.Center)),shortcut=p.Shortcut,sourceOwner="UnderheartLayout.Portals",nativeObserved=false}).ToArray();UhAnchor[] cellAnchors=under.Anchors.Where(a=>a.Cell==cell.Index).ToArray();row["serviceAnchors"]=cellAnchors.Select(a=>(object)new{token="Under:"+a.Id,id=a.Id,localPosition=P(a.Pos),worldPosition=P(frame.ToWorld(a.Pos)),yaw=frame.YawToWorld(a.Yaw),sourceOwner="UnderheartLayout.Anchors",nativeObserved=false}).ToArray();row["npcAnchors"]=CastRoster04E.All.Where(c=>c.Post==CastPostKind.UnderAnchor&&cellAnchors.Any(a=>a.Id==c.Anchor)).Select(c=>(object)new{id=c.Id,name=c.Name,post=c.Post.ToString(),anchor=c.Anchor,onDemand=c.OnDemand,storyIdentity=c.StoryIdentity,storyUnlock=c.StoryUnlock,sourceOwner="CastRoster04E",nativeSpawned="NOT RUN"}).ToArray();entries.Add(row);}
Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled revision-10 source plans and authored anchor data. Source candidates are not native admissions or activated objects. No Unity Editor, renderer, GPU, physics, NPC spawn or gameplay was executed.",seed=61724,worldRevision=10,court=P(l.RealmCourt04OB),coverageRadiusMetres=650,underheartVariant=under.Variant,underheartChoice=choice.Report,sourceOnly=true,nativeUnity="NOT RUN",entries,routes=l.RealmRoutes04OB.Select(r=>new{id=r.Id,width=r.Width,points=r.Points.Select(p=>P(p)).ToArray()}),geometry=new object[0]}));
}}
'''


def source_digest(root):
    digest = hashlib.sha256()
    for folder in ('Runtime', 'Editor', '../Tests/EditMode'):
        base = root / 'Assets/NeonSyndicate/Scripts' / folder
        for path in sorted(base.resolve().rglob('*.cs')):
            digest.update(path.relative_to(root).as_posix().encode() + b'\0' + path.read_bytes() + b'\0')
    return digest.hexdigest()


def commit_source_digest(root, ref):
    paths = []
    for folder in ('Assets/NeonSyndicate/Scripts/Runtime', 'Assets/NeonSyndicate/Scripts/Editor',
                   'Assets/NeonSyndicate/Tests/EditMode'):
        files = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', ref, '--', folder], cwd=root, text=True)
        paths.extend(sorted(name for name in files.splitlines() if name.endswith('.cs')))
    digest = hashlib.sha256()
    for name in paths:
        data = subprocess.check_output(['git', 'show', ref + ':' + name], cwd=root)
        digest.update(name.encode() + b'\0' + data + b'\0')
    return digest.hexdigest()


def all_strings(value, prefix=''):
    if isinstance(value, str):
        yield prefix, value
    elif isinstance(value, dict):
        for key, child in value.items():
            yield from all_strings(child, prefix + '.' + key)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from all_strings(child, prefix + '[' + str(index) + ']')


def enrich_quests(root, report):
    authored = []
    for path in sorted((root / 'Assets/NeonSyndicate/Resources/Quests').glob('*.json')):
        data = json.loads(path.read_text())
        for index, quest in enumerate(data.get('Quests', [])):
            for field, token in all_strings(quest):
                if token.startswith(('Venue:', 'Under:', 'Cast:', 'Realm:')) or field in ('.VenueGiver', '.Giver') or field.endswith('.At'):
                    authored.append({'file': path.relative_to(root).as_posix(), 'questId': quest.get('Id'),
                                     'field': field, 'token': token, 'questIndex': index})
    for row in report['entries']:
        service = row.get('serviceOwner', '')
        anchor_tokens = {anchor['token'] for anchor in row.get('serviceAnchors', [])}
        cast_tokens = {'Cast:' + cast['id'] for cast in row['npcAnchors'] if 'id' in cast}
        row['questAnchors'] = [item for item in authored if item['token'] in anchor_tokens | cast_tokens
                               or (service and (item['token'].startswith('Venue:' + service + '.')
                                               or item['field'] == '.VenueGiver' and item['token'] == service))
                               or (row.get('poiName') and item['field'].endswith('.At') and item['token'] == row['poiName'])]
    report['questAnchorScope'] = 'Exact authored JSON references to this service, cell anchor or cast post. Empty lists do not certify absence of dynamic mission triggers.'


def split_surface_groups(report):
    rows = []
    for row in report['entries']:
        if 'cellId' in row:
            row['rowType'] = 'UNDERHEART_CELL'
            rows.append(row)
            continue
        physical = copy.deepcopy(row)
        physical['rowType'] = 'PHYSICAL_BUILDING'
        physical['stableId'] = 'revision:10/lot:' + str(row['lotSeed']) + '/seed:' + str(row['seed']) + '/building'
        physical['wholePhysicalBuildingRemoved'] = False
        physical['visualGroupStableIds'] = []
        if row['replacementClass'] == 'REPLACE_VISUAL':
            physical['replacementClass'] = 'PROTECTED'
            physical['reason'] = 'Complete physical building, collision, service, landing and receipt ownership remain unchanged. Only the separately inventoried child visual group is eligible for temporary replacement.'
            physical['replacementScope'] = 'No physical building removal or authority substitution'
            physical['proposedFantasyFamily'] = 'Physical authority remains unchanged'
            physical['visualGroupStableIds'] = [row['stableId']]
            visual = copy.deepcopy(row)
            visual['rowType'] = 'SURFACE_VISUAL_GROUP'
            visual['parentStableId'] = physical['stableId']
            visual['wholePhysicalBuildingRemoved'] = False
            visual['bounds'] = visual.pop('isolatedVisualBounds', visual['bounds'])
            rows.extend((physical, visual))
        else:
            rows.append(physical)
    report['entries'] = rows
    report['rowTypeCounts'] = dict(collections.Counter(row['rowType'] for row in rows))
    report['distinctPhysicalBuildingCount'] = report['rowTypeCounts'].get('PHYSICAL_BUILDING', 0)
    report['distinctCoveredUnderheartCellCount'] = report['rowTypeCounts'].get('UNDERHEART_CELL', 0)
    report['wholePhysicalBuildingsRemoved'] = 0
    report['countingContract'] = 'Physical buildings, their individually replaceable visual groups and existing underground cells are different row types. Five surface visual rows refer to their physical parent; they are not five additional buildings. Source eligibility is not native admission.'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ('dotnet', 'assembly', 'unity-references', 'output'):
        parser.add_argument('--' + key, type=Path, required=True)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--docs', type=Path)
    parser.add_argument('--matrix-report', type=Path)
    parser.add_argument('--source-commit', help='Published source commit whose complete C# bytes must match the matrix and working source.')
    args = parser.parse_args()
    root, output = args.root.resolve(), args.output.resolve()
    matrix_file = args.matrix_report or args.assembly.resolve().parent / 'report.json'
    matrix = json.loads(matrix_file.read_text())
    fingerprint = source_digest(root)
    if fingerprint != matrix['source_sha256']:
        raise SystemExit('Current C# bytes do not match the referenced managed matrix. Refusing stale source coverage.')
    base_commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    source_commit = subprocess.check_output(['git', 'rev-parse', args.source_commit or 'HEAD'], cwd=root, text=True).strip()
    if commit_source_digest(root, source_commit) != fingerprint:
        raise SystemExit('Published source commit C# bytes differ from the matrix or working source. Refusing mislabeled coverage.')
    compiler.CODE = CODE
    prior = sys.argv
    sys.argv = [__file__, '--dotnet', str(args.dotnet.resolve()), '--assembly', str(args.assembly.resolve()),
                '--unity-references', str(args.unity_references.resolve()), '--output', str(output), '--root', str(root)]
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            compiler.main()
    finally:
        sys.argv = prior
    report = json.loads((output / 'world-manifest.json').read_text())
    report.pop('geometry', None)
    split_surface_groups(report)
    report['underheartVariantBasis'] = 'SOURCE_ONLY default source-world sample, using actual UnderheartPlanner.Choose(city, Pressure House frame, wantPortal:true). Saved or current native variant and bindings are NOT RUN.'
    report['nativeUnderheartVariant'] = 'NOT RUN'
    for row in report['entries']:
        row.setdefault('rendererGroup', row['visualRoot'])
        if row['rowType'] == 'UNDERHEART_CELL':
            row['sourceFrame']['evidence'] = 'SOURCE_ONLY planner-selected sample, not a measured native or loaded-save frame'
            row['sourceFrame']['nativeVariant'] = 'NOT RUN'
    enrich_quests(root, report)
    report['sourceCommit'] = source_commit
    report['sourceCommitVerified'] = True
    report['workingTreeBaseCommit'] = base_commit
    report['workingTreeDirty'] = bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=root))
    report['sourceCSharpSha256'] = fingerprint
    report['managedAssemblySha256'] = hashlib.sha256(args.assembly.read_bytes()).hexdigest()
    report['generator'] = 'tools/measure_world_rewrite_wr01.py via tools/measure_city_rebirth_04o.py'
    report['classificationCounts'] = dict(collections.Counter(row['replacementClass'] for row in report['entries']))
    report['sourcePlannedReplacements'] = sum(row['replacementClass'] == 'REPLACE_VISUAL' for row in report['entries'])
    report['nativeCounts'] = {'admitted': 'NOT RUN', 'active': 'NOT RUN', 'refused': 'NOT RUN'}
    keys = [row['stableId'] for row in report['entries']]
    if len(keys) != len(set(keys)):
        raise SystemExit('Duplicate stable coverage key')
    docs = args.docs.resolve() if args.docs else root / 'Docs/WorldRewrite/WR01'
    docs.mkdir(parents=True, exist_ok=True)
    body = json.dumps(report, indent=2) + '\n'
    for name in ('WORLD_REWRITE_COVERAGE.json', 'COVERAGE_MAP.json'):
        (docs / name).write_text(body)
    replacements = {key: report[key] for key in ('scope', 'sourceCommit', 'sourceCSharpSha256', 'managedAssemblySha256',
                                                'sourceCommitVerified', 'workingTreeBaseCommit', 'workingTreeDirty',
                                                'sourceOnly', 'nativeUnity', 'sourcePlannedReplacements', 'nativeCounts',
                                                'wholePhysicalBuildingsRemoved', 'countingContract')}
    replacements['records'] = [row for row in report['entries'] if row['replacementClass'] == 'REPLACE_VISUAL']
    (docs / 'REPLACEMENT_MANIFEST.json').write_text(json.dumps(replacements, indent=2) + '\n')
    print(json.dumps({'entries': len(keys), 'classificationCounts': report['classificationCounts'],
                      'sourceCSharpSha256': fingerprint, 'nativeUnity': 'NOT RUN'}))


if __name__ == '__main__':
    main()
