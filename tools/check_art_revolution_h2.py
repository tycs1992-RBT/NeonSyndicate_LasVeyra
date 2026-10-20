#!/usr/bin/env python3
"""Actual H2 asset integrity and old-owner preservation. NOT native import, visual approval or FPS."""
import argparse,hashlib,json,math,re,struct,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='9c75dfa83c4619d1776d970f571af27172b35567'
ASSETS=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[];inventory=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 manifest=json.loads((ASSETS/'asset_manifest.json').read_text())
 check('Original source method and missing native approval are explicit',manifest['production_approval'] is False and manifest['native_import']=='NOT RUN' and manifest['license']=='CC0-1.0')
 check('Nineteen distinct source meshes, twelve distinct architectural categories',len(manifest['models'])==19 and len({m['id'] for m in manifest['models']})==19)
 for entry in manifest['models']:
  name=entry['id'];counts=[]
  for detail in ('high','low'):
   file=ASSETS/'Meshes'/f'{name}_{detail}.bytes';data=file.read_bytes();count=struct.unpack('<i',data[4:8])[0]
   valid=data[:4]==b'CMH1' and len(data)==8+count*32 and count%3==0 and 30<count<=90000
   rows=list(struct.iter_unpack('<8f',data[8:]));valid &= all(all(math.isfinite(v)for v in row) and abs(sum(v*v for v in row[3:6])-1)<.001 for row in rows)
   check(name+' / '+detail+' finite mesh, normals, length and budget',valid);counts.append(count)
   inventory.append({'path':file.relative_to(ROOT).as_posix(),'sha256':sha(file),'vertices':count})
  check(name+' actual lower detail does not exceed high',counts[1]<=counts[0])
  obj=ASSETS/'Models'/f'{name}.obj';verts=[tuple(map(float,l.split()[1:]))for l in obj.read_text().splitlines() if l.startswith('v ')]
  high=list(struct.iter_unpack('<8f',(ASSETS/'Meshes'/f'{name}_high.bytes').read_bytes()[8:]))
  check(name+' interchange positions match actual runtime stream',len(verts)==len(high) and all(abs(a-b)<2e-5 for v,row in zip(verts,high)for a,b in zip(v,row[:3])))
 for role in sorted({m['role'] for m in manifest['models']}):
  for kind in ('Albedo','Normal','Mask'):
   file=ASSETS/'Textures'/f'{role}_{kind}.png';raw=file.read_bytes();meta=Path(str(file)+'.meta').read_text();linear=kind!='Albedo'
   check(role+' / '+kind+' real PNG and correct color/normal importer',raw[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',raw[16:24])==(256,256) and f'sRGBTexture: {0 if linear else 1}' in meta and f'textureType: {1 if kind=="Normal" else 0}' in meta)
   inventory.append({'path':file.relative_to(ROOT).as_posix(),'sha256':sha(file),'bytes':len(raw)})
 runtime=ROOT/'Assets/NeonSyndicate/Scripts/Runtime'
 unchanged=['World/CityLayout.VisualSlice04OH.cs','World/CityLayout.Gothic04OB.cs','Characters/BodyBuilder.VisualSlice04OH.cs','Characters/ImportedVisual04OD.cs','Characters/ImportedArt04OH.cs','Characters/ImportedModelRegistry04OD.cs','Characters/HumanoidPose.VisualSlice04OH.cs','Creatures/CreatureBuilder.VisualSlice04OH.cs','Creatures/CreaturePose.Cinematic04OF.cs','Creatures/FrontierPerformance04OG.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/RegionalArmor04OE.cs','Gameplay/ForgeTreatments04M.cs','Core/CityRebirthBoot04O.cs']
 old_ok=True
 for rel in unchanged:
  path=runtime/rel
  if not path.exists():raise RuntimeError('Preservation path missing: '+rel)
  old_ok &= subprocess.check_output(['git','show',BASE+':'+path.relative_to(ROOT).as_posix()],cwd=ROOT)==path.read_bytes()
 check('H1 geometry, skin, original pose/import/companion/damage/Forge owners and default boot retain identical bytes',old_ok)
 check('Production hero registry not silently created',not (ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())
 check('Only original city batching/collision dispatch, no disconnected scene','L.ArtRevolutionH2 && BuildArtRouteH2(route)' in (runtime/'World/CityLayout.Frontier04OG.cs').read_text() and '_wildCollision04OC' in (runtime/'World/CityLayout.ArtRevolutionH2.cs').read_text())
 check('Both finite first-win admission paths include new revision','revision>8' in (runtime/'Creatures/FracturedWildsCombat04OC.cs').read_text() and 'revision==8' in (runtime/'Creatures/FracturedWildsCombat04OC.cs').read_text())
 quest=json.loads((ROOT/'Assets/NeonSyndicate/Resources/Quests/ascension_h2_living_lens.json').read_text())['Quests'][0];steps=[s for phase in quest['Phases']for s in phase['Steps']]
 check('Physical commission is data, original host, four finite creatures, three interactions, no entitlement grant',quest['VenueGiver']=='forge' and quest['Requires']=={'WorldRevisions':[8]} and sum(s.get('Count',0)for s in steps if s['Type']=='Kill')==4 and sum(s['Type']=='Interact'for s in steps)==3 and not any(s['Type']in ('GiveGun','Boss','Bond')for s in steps))
 check('No unsupported NUnit AnyOf remains in shipped tests',not any('Is.AnyOf('in f.read_text()for f in (ROOT/'Assets/NeonSyndicate/Tests').rglob('*.cs')))
 report={'scope':__doc__,'accepted_H1':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'asset_inventory':inventory,'native_unity':'NOT RUN','visual_acceptance':'NOT RUN'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
