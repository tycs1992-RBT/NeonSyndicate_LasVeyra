#!/usr/bin/env python3
"""WR02 exact inherited-source, owner-asset, GUID and authored-payload check. Never native acceptance."""
import argparse,hashlib,json,re,subprocess
from pathlib import Path
from surface_lineage_wr02 import BASELINE,inherited_source_bytes,load_contract
from check_world_rewrite_wr01 import BaselinePayloads,check_frozen_tree,ART_ADDITIONS,NEW_SOURCES
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);args=p.parse_args();rows=[]
 def check(name,ok,detail=None):rows.append({'name':name,'passed':bool(ok),'detail':detail})
 contract=load_contract();changed=[]
 names=subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'--','Assets','Packages','ProjectSettings'],cwd=ROOT,text=True).splitlines()
 for path in names:
  if path.endswith('.cs'):
   original=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT)
   if not (ROOT/path).is_file() or inherited_source_bytes(ROOT,path)!=original:changed.append(path)
 check('Every inherited C# byte frozen except complete exact pinned visual hooks',not changed,changed)
 reader=BaselinePayloads(ROOT)
 try:
  for tree,predicate,allowed in [('Assets',lambda p:not p.endswith('.cs'),ART_ADDITIONS+tuple(x+'.meta'for x in NEW_SOURCES)),('ArtSource',None,ART_ADDITIONS),('Packages',None,()),('ProjectSettings',None,()),('Assets/NeonSyndicate/Tests',None,tuple(NEW_SOURCES)+tuple(x+'.meta'for x in NEW_SOURCES))]:
   r=check_frozen_tree(ROOT,BASELINE,tree,allowed,predicate,reader);check('Inherited payload and metadata exact: '+tree,r['passed'] and r['hydrated'],r)
 finally:reader.close()
 manifest=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartWR02/manifest.json').read_text())
 payloads=manifest['modules']+manifest['textures']+manifest['additional_sources']+[{'path':manifest['source'],'bytes':manifest['source_bytes'],'sha256':manifest['source_sha256']}]
 for row in payloads:
  f=ROOT/row['path'];check('Authored payload '+row['path'],f.is_file()and f.stat().st_size==row['bytes']and hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'])
 check('Six real families and three LOD exports each',len(manifest['modules'])==18 and len({r['name']for r in manifest['modules']})==6 and all({r['lod']for r in manifest['modules']if r['name']==name}=={0,1,2}for name in {r['name']for r in manifest['modules']}))
 check('Five shared PBR families, no per-building materials',len(manifest['textures'])==15)
 guid={};duplicates=[]
 for f in (ROOT/'Assets').rglob('*.meta'):
  m=re.search(r'^guid: ([0-9a-f]{32})$',f.read_text(errors='replace'),re.M)
  if m:
   if m[1]in guid:duplicates.append([guid[m[1]],str(f.relative_to(ROOT))])
   guid[m[1]]=str(f.relative_to(ROOT))
 check('All established and new metadata GUIDs unique',not duplicates,duplicates)
 new=[ROOT/x for x in NEW_SOURCES if 'WR02' in x]
 text='\n'.join(f.read_text()for f in new if '/Scripts/' in str(f))
 for token in ['AddComponent<BoxCollider>','AddComponent<MeshCollider>','AddComponent<PlayerController>','AddComponent<NpcController>','SaveSystem.Save(','PlayerPrefs.Set','new SaveData','void Update(']:check('No new gameplay or per-object authority: '+token,token not in text)
 for private in ['Assets/NeonSyndicate/Resources/HeartPreparedWR02/test.asset','Assets/NeonSyndicate/Art/HeartWR02/Materials/test.mat']:
  check('Generated output ignored '+private,subprocess.run(['git','check-ignore','-q',private],cwd=ROOT).returncode==0)
 data={'baseline':BASELINE,'source_head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'worktree_dirty':bool(subprocess.check_output(['git','status','--porcelain'],cwd=ROOT)),'passed':sum(x['passed']for x in rows),'total':len(rows),'checks':rows,'native_unity':'NOT RUN'}
 args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(data,indent=2)+'\n');print(json.dumps({'passed':data['passed'],'total':data['total'],'failed':[r['name']for r in rows if not r['passed']]}));return int(data['passed']!=data['total'])
if __name__=='__main__':raise SystemExit(main())
