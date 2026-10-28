#!/usr/bin/env python3
"""Exact WR02-A source and asset preservation. Source checks never certify native import or play."""
import argparse,hashlib,json,re,subprocess
from pathlib import Path
from foundry_lineage_wr02a import BASELINE,canonical_foundry_bytes,load_contract
from check_world_rewrite_wr01 import BaselinePayloads,check_frozen_tree
ROOT=Path(__file__).resolve().parents[1]
NEW=('Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.FoundryWR02A.cs','Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs','Assets/NeonSyndicate/Tests/EditMode/FoundryWR02ATests.cs')
PREFIXES=('Assets/NeonSyndicate/Art/FoundryWR02A/','Assets/NeonSyndicate/Art/FoundryWR02A.meta','ArtSource/FoundryWR02A/')
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();rows=[]
 def check(name,ok,detail=None):rows.append({'name':name,'passed':bool(ok),'detail':detail})
 paths=subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'--','Assets'],cwd=ROOT,text=True).splitlines();violations=[]
 for path in paths:
  if path.endswith('.cs'):
   actual=(ROOT/path).read_bytes()if(ROOT/path).is_file()else b'';original=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT)
   if canonical_foundry_bytes(ROOT,path,actual)!=original:violations.append(path)
 check('All inherited C# bytes except complete exact pinned foundry hooks',not violations,violations)
 current=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z','--','Assets'],cwd=ROOT).split(b'\0');unknown=[p.decode()for p in current if p and p.decode().endswith('.cs')and p.decode()not in paths and p.decode()not in NEW];check('Only three explicit new C# files',not unknown,unknown)
 reader=BaselinePayloads(ROOT)
 try:
  for tree,predicate,allowed in [('Assets',lambda p:not p.endswith('.cs'),PREFIXES+tuple(x+'.meta'for x in NEW)),('ArtSource',None,PREFIXES),('Packages',None,()),('ProjectSettings',None,()),('Assets/NeonSyndicate/Tests',None,tuple(NEW)+tuple(x+'.meta'for x in NEW))]:
   result=check_frozen_tree(ROOT,BASELINE,tree,allowed,predicate,reader);check('Inherited payload, GUID and hydration '+tree,result['passed']and result['hydrated'],result)
 finally:reader.close()
 art=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A';manifest=json.loads((art/'manifest.json').read_text());check('Exactly two demonstrations and six LODs',len(manifest['modules'])==6 and {x['name']for x in manifest['modules']}=={'WRArchiveCrown','CrystalBasiliskCandidate'})
 for row in manifest['modules']+manifest['textures']+manifest['sources']+[manifest['originalCreatureContract']]:
  f=ROOT/row['path'];check('Exact current payload '+row['path'],f.is_file()and f.stat().st_size==row['bytes']and hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'])
 for name in ['SanctuaryArchive','CrystalBasiliskCandidate']:
  d=json.loads((art/name/'processed.json').read_text());check('Actual current input and processing recipe '+name,d['input_sha256']==hashlib.sha256((ROOT/d['input']).read_bytes()).hexdigest()and d['processor_sha256']==hashlib.sha256((ROOT/'tools/foundry/process_asset.py').read_bytes()).hexdigest())
  for row in d['outputs']:check('Actual successful independent LOD parse '+name+str(row['lod']),row.get('independentInspection',{}).get('passed')is True)
  c=json.loads((ROOT/d['colliderManifest']).read_text());check('Original collision retained '+name,c['policy']=='ORIGINAL_AUTHORITY_ONLY'and c['unityCollidersCreated']==0 and not c['nativeAccepted'])
 check('No paid provider task or false native claim',manifest['creditsSpent']==0 and manifest['providerConnection']=='NOT ESTABLISHED'and manifest['nativeUnity']=='NOT RUN')
 text='\n'.join((ROOT/p).read_text()for p in NEW if '/Scripts/'in p)
 for token in ['AddComponent<PlayerController>','AddComponent<NpcController>','AddComponent<Collider>','AddComponent<MeshCollider>','SaveSystem.Save(','PlayerPrefs.Set','TakeDamage(','TRIPO_API_KEY','openapi.tripo3d','void Update(']:check('No new gameplay, credential or per-object owner '+token,token not in text)
 for path in ['Assets/NeonSyndicate/Resources/HeartPreparedWR02A/generated.asset','.foundry-local/keys.json','ArtSource/FoundryWR02A/example.blend1']:
  check('Private or generated output ignored '+path,subprocess.run(['git','check-ignore','-q',path],cwd=ROOT).returncode==0)
 seen={};duplicate=[]
 for p in (ROOT/'Assets').rglob('*.meta'):
  m=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(errors='replace'),re.M)
  if m:
   if m[1]in seen:duplicate.append(str(p.relative_to(ROOT)))
   seen[m[1]]=p
 check('All metadata GUIDs unique',not duplicate,duplicate)
 result={'baseline':BASELINE,'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'worktreeDirty':bool(subprocess.check_output(['git','status','--porcelain'],cwd=ROOT)),'passed':sum(r['passed']for r in rows),'total':len(rows),'checks':rows,'nativeUnity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':result['passed'],'total':result['total'],'failed':[r['name']for r in rows if not r['passed']]}));return int(result['passed']!=result['total'])
if __name__=='__main__':raise SystemExit(main())
