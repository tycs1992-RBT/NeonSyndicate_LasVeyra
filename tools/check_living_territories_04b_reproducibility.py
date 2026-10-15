#!/usr/bin/env python3
"""Regenerate only original 04B art and prove its payloads/metas repeat exactly.

Writes GUID/hash provenance and exact selected-layout source geometry budgets.
This is not a native import, collider, renderer, or performance test.
"""
from pathlib import Path
import argparse, hashlib, json, re, subprocess, sys
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/NeonSyndicate/Resources/LivingTerritories04B'
OUT=ROOT/'Docs/LivingTerritories04B/Validation'

def main():
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--confirm-regenerate',action='store_true')
 args=parser.parse_args()
 if not args.confirm_regenerate:parser.error('Pass --confirm-regenerate after protecting any artist changes.')
 files=sorted(p for folder in ('Models','Meshes','Layouts','Props') for p in (ART/folder).rglob('*') if p.is_file())
 before={p:hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
 subprocess.run([sys.executable,str(ROOT/'tools/generate_living_territories_04b_art.py'),'--confirm-regenerate'],check=True)
 after={p:hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
 changed=[str(p.relative_to(ROOT)) for p in files if before[p]!=after[p]]
 assets=[]
 for path in files:
  if path.suffix=='.meta':continue
  match=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(path)+'.meta').read_text(),re.M)
  assets.append({'path':str(path.relative_to(ROOT)),'guid':match[1] if match else None,'sha256':after[path],'bytes':path.stat().st_size})
 report={'scope':'Original authored 04B art assets; runtime catalog/populations belong to separate systems.',
  'generator':'tools/generate_living_territories_04b_art.py','repeat_generation_identical':not changed,
  'files_compared_including_metadata':len(files),'changed':changed,'assets':assets,'native_Unity_import':'NOT RUN',
  'materials':'Shared 04A native Lit materials and their authored maps; no duplicated texture payloads.'}
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'art_provenance.json').write_text(json.dumps(report,indent=2)+'\n')
 triangles={}
 for version,folder in [('04A','VisualRebirth04A'),('04B','LivingTerritories04B')]:
  for model in json.loads((ART.parent/folder/'Models/model_inventory.json').read_text())['models']:
   triangles[(version,model['name'],model['lod'])]=model['triangles']
 layouts=[]
 for path in sorted((ART/'Layouts').glob('*.json')):
  data=json.loads(path.read_text());relay=data['id'].startswith('relay_')
  for variant in range(3) if relay else range(1):
   for state,tier,owned in [('base',0,False),('fortified2',2,True)] if relay else [('base',0,False)]:
    nodes=[n for n in data['nodes'] if n['tier']<=tier and (not n['owned'] or owned) and (n.get('variant',0)==0 or n['variant']==variant+1)]
    high=sum(triangles[(n['source'],n['model'],0)] for n in nodes)
    low=sum(triangles[(n['source'],n['model'],1)] for n in nodes if n['far'] or n['solid'])
    layouts.append({'layout':data['id'],'variant':variant,'state':state,'resource_claimed':False,
     'authored_parts':len(nodes),'active_solid_nodes':sum(n['solid'] for n in nodes),
     'high_source_triangles':high,'low_source_triangles':low,'cross_paving_triangles':4})
 (OUT/'art_geometry_budgets.json').write_text(json.dumps({'scope':'Exact authored source sums for selected state and variant, plus four paving triangles. Resources are unclaimed for the maximum active source budget. CPU/renderer/GPU performance is NOT RUN. High retains fine plants; Low keeps all intended solids and silhouette parts.','layouts':layouts},indent=2)+'\n')
 print(json.dumps({'files_compared':len(files),'assets':len(assets),'unchanged':not changed,'changed':changed,'geometry_states':len(layouts)}))
 return 1 if changed else 0
if __name__=='__main__':raise SystemExit(main())
