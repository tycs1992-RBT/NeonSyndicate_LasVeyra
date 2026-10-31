"""Export actual compiled source proposals. Never label source clearance as native admission."""
import argparse,contextlib,io,json,sys
from pathlib import Path
import measure_city_rebirth_04o as compiler
from measure_world_rewrite_wr01 import source_digest,commit_source_digest
CODE=r'''

using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class Probe { static float[] P(Vector3 p){return new[]{p.x,p.y,p.z};} public static void Main(string[]args){List<string> errors=new List<string>();Input input=QuestJson.Read<Input>(File.ReadAllText(args[0]),args[0],errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(l,new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()});l.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues);var reserved=new List<HeartLivingFR02A>();foreach(var p in HeartPlanFR01.LivingProposals(l)){string why;if(HeartPlanFR01.LivingClear(l,p,reserved,out why))reserved.Add(p);}var rows=new List<object>();int eligible=0;foreach(var p in HeartPlanFR01.CompositionProposalsWR02B(l)){string why;bool clear=HeartPlanFR01.CompositionClearWR02B(l,p,reserved,out why);if(clear)eligible++;rows.Add(new{p.stableId,p.zone,p.route,p.family,position=P(p.position),size=P(p.size),p.yaw,sourceEligible=clear,reason=why,nativeAdmission="NOT RUN"});}var trees=l.WildScenery04OC.Where(t=>t.Kind==0&&HeartPlanFR01.SurfaceRegionWR02(l,t.Position)&&(t.Region==0||t.Region==1)).Select(t=>new{stableId="revision:10/original-tree:"+t.Seed,t.Seed,t.Region,position=P(t.Position),family=HeartPlanFR01.CompositionCanopyWR02B(t.Region),originalPhysicalTrunkRetained=true,nativeAdmission="NOT RUN"});Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled revision-10 source census. Source-eligible is not native admitted/active. Existing FR02 source reservations included; quest/trigger/ground/camera probes run only natively.",seed=61724,sourcePlanned=rows.Count,sourceEligible=eligible,sourceRefused=rows.Count-eligible,nativeAdmitted=(int?)null,nativeActive=(int?)null,sourceReservedFR02=reserved.Count,softPlacementLimit=HeartPlanFR01.CompositionSoftLimitWR02B,trees,placements=rows,geometry=new object[0],nativeUnity="NOT RUN"}));}}
'''
def main():
 p=argparse.ArgumentParser()
 for key in ['root','dotnet','assembly','unity-references','output','matrix-report']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--source-commit',default='');a=p.parse_args();fingerprint=source_digest(a.root.resolve())
 if fingerprint!=json.loads(a.matrix_report.read_text())['source_sha256']:raise SystemExit('Stale compiled source census refused.')
 if a.source_commit and fingerprint!=commit_source_digest(a.root.resolve(),a.source_commit):raise SystemExit('Source commit differs from compiled census.')
 compiler.CODE=CODE;sys.argv=[__file__]+sum((['--'+k,str(getattr(a,k.replace('-','_')).resolve())]for k in ['root','dotnet','assembly','unity-references','output']),[])
 with contextlib.redirect_stdout(io.StringIO()):compiler.main()
 d=json.loads((a.output/'world-manifest.json').read_text());d['sourceCommit']=a.source_commit;d['sourceCSharpSha256']=fingerprint;(a.output/'world-manifest.json').write_text(json.dumps(d,indent=2)+'\n');print(json.dumps({k:v for k,v in d.items()if k not in ['placements','trees','geometry']}))
if __name__=='__main__':main()
