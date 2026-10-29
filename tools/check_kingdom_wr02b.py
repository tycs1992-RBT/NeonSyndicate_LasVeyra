#!/usr/bin/env python3
"""Source/payload preservation and actual current art receipts. No native import or gameplay claim."""
import argparse,hashlib,json,re,subprocess
from pathlib import Path
from composition_lineage_wr02b import BASELINE,canonical_composition_bytes,load_contract
from check_world_rewrite_wr01 import BaselinePayloads,check_frozen_tree
from wr02c3_lineage import ADDITIONS as WR02C3_ADDITIONS
ROOT=Path(__file__).resolve().parents[1]
NEW_SOURCES_WR02B=frozenset('Assets/NeonSyndicate/'+p for p in (
'Scripts/Editor/FantasyAcceptanceV1.BasiliskWR02B.cs','Scripts/Editor/HeartSetupFR01.CompositionWR02B.cs',
'Scripts/Runtime/Characters/BasiliskReviewAssetWR02B.cs','Scripts/Runtime/Characters/BasiliskSkinRulesWR02B.cs','Scripts/Runtime/Characters/ImportedVisual04OD.BasiliskWR02B.cs',
'Scripts/Runtime/World/CityBuilder.CompositionWR02B.cs','Scripts/Runtime/World/HeartFR01/HeartPlanFR01.CompositionWR02B.cs','Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.CompositionWR02B.cs','Tests/EditMode/KingdomWR02BTests.cs')) | frozenset(WR02C3_ADDITIONS)
PREFIXES=('Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B/','Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B.meta','ArtSource/FoundryWR02A/EnvironmentWR02B/')
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,ok,detail=None):checks.append({'name':name,'passed':bool(ok),'detail':detail})
 inherited=subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'--','Assets'],cwd=ROOT,text=True).splitlines();violations=[]
 for path in inherited:
  if path.endswith('.cs'):
   original=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT);current=(ROOT/path).read_bytes()if(ROOT/path).is_file()else b''
   if canonical_composition_bytes(ROOT,path,current)!=original:violations.append(path)
 check('Every inherited C# byte except complete pinned WR02-B hooks',not violations,violations)
 paths=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z','--','Assets'],cwd=ROOT).split(b'\0');unknown=[p.decode()for p in paths if p and p.decode().endswith('.cs')and p.decode()not in inherited and p.decode()not in NEW_SOURCES_WR02B];check('Only explicit reviewed new source paths',not unknown,unknown)
 for path,digest in WR02C3_ADDITIONS.items():check('Exact reviewed WR02-C3 source '+path,(ROOT/path).is_file() and hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest)
 reader=BaselinePayloads(ROOT)
 try:
  for tree,predicate,allowed in [('Assets',lambda p:not p.endswith('.cs'),PREFIXES+tuple(p+'.meta'for p in NEW_SOURCES_WR02B)),('ArtSource',None,PREFIXES),('Packages',None,()),('ProjectSettings',None,()),('Assets/NeonSyndicate/Tests',None,tuple(NEW_SOURCES_WR02B)+tuple(p+'.meta'for p in NEW_SOURCES_WR02B))]:
   result=check_frozen_tree(ROOT,BASELINE,tree,allowed,predicate,reader);check('Inherited owner payloads, GUIDs and hydration '+tree,result['passed']and result['hydrated'],result)
 finally:reader.close()
 for tool in ['process_asset.py','policy.py','tripo_v3.py','factory_queue.py','author_heroes.py','inspect_fbx.c','seal_delivery.py','check_motion.py']:
  path='tools/foundry/'+tool;check('Existing foundry recipe frozen '+tool,(ROOT/path).read_bytes()==subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT))
 art=ROOT/PREFIXES[0];manifest=json.loads((art/'manifest.json').read_text());check('Seven real authored families and twenty-one LODs',len(manifest['modules'])==21 and len(manifest['frames'])==7 and all({x['lod']for x in manifest['modules']if x['name']==frame['name']}=={0,1,2}for frame in manifest['frames']))
 check('Existing PBR families reused without new textures',len(manifest['textures'])==15 and not list(art.rglob('*.png')))
 for row in manifest['modules']+manifest['textures']+manifest['sources']:
  f=ROOT/row['path'];check('Exact current payload '+row['path'],f.is_file()and f.stat().st_size==row['bytes']and hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'])
 for frame in manifest['frames']:
  name=frame['name'];d=json.loads((art/name/'processed.json').read_text());check('Actual recipe/input '+name,d['processor_sha256']==hashlib.sha256((ROOT/'tools/foundry/process_asset.py').read_bytes()).hexdigest()and d['input_sha256']==hashlib.sha256((ROOT/d['input']).read_bytes()).hexdigest());c=json.loads((ROOT/d['colliderManifest']).read_text());check('No native physics promotion '+name,c['unityCollidersCreated']==0 and c['nativeAccepted']is False and c['policy']==('EXPLICIT_CANDIDATES_ONLY'if name=='EscarpmentArch'else'ORIGINAL_AUTHORITY_ONLY'))
  for row in d['outputs']:check('Real independent current FBX receipt '+name+'/'+str(row['lod']),row['independentInspection']['passed']and row['sha256']==hashlib.sha256((ROOT/row['path']).read_bytes()).hexdigest())
 check('No connected/paid generation or false native receipt',manifest['providerConnection']=='NOT ESTABLISHED'and manifest['creditsSpent']==0 and manifest['nativeUnity']=='NOT RUN')
 text='\n'.join((ROOT/p).read_text()for p in NEW_SOURCES_WR02B if '/Scripts/'in p)
 for token in ['AddComponent<PlayerController>','AddComponent<NpcController>','AddComponent<Creature>','AddComponent<MeshCollider>','AddComponent<BoxCollider>','SaveSystem.Save(','PlayerPrefs.Set','TakeDamage(','void Update(','TRIPO_API_KEY','new QuestDef','RewardLedger']:
  check('No new gameplay/physics/credential owner '+token,token not in text)
 check('Live creature entry remains Editor-only', '#if UNITY_EDITOR\n' in (ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.BasiliskWR02B.cs').read_text())
 for path in ['Assets/NeonSyndicate/Resources/HeartPreparedWR02B/Catalog.asset','ArtSource/FoundryWR02A/EnvironmentWR02B/example.blend1']:check('Generated output ignored '+path,subprocess.run(['git','check-ignore','-q',path],cwd=ROOT).returncode==0)
 guids={};duplicates=[]
 for p in (ROOT/'Assets').rglob('*.meta'):
  m=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(errors='replace'),re.M)
  if m:
   if m[1]in guids:duplicates.append(str(p.relative_to(ROOT)))
   guids[m[1]]=str(p)
 check('All existing and new GUIDs unique',not duplicates,duplicates)
 result={'baseline':BASELINE,'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'nativeUnity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':result['passed'],'total':result['total'],'failed':[x for x in checks if not x['passed']]}));return int(result['passed']!=result['total'])
if __name__=='__main__':raise SystemExit(main())
