from surface_lineage_wr02 import inherited_source_bytes
"""Actual Living Streets asset integrity and frozen owners. Source checks, never Unity import or play."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE='4e6246006836db88222303bc41bee24b74c90114'
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();rows=[]
 def check(name,ok):rows.append({'name':name,'passed':bool(ok)})
 art=ROOT/'Assets/NeonSyndicate/Art/HeartFR02A';m=json.loads((art/'manifest.json').read_text())
 check('Original rights, nine buildings, four court families and three LODs',m['license']=='CC0-1.0' and m['external_inputs']==[] and len(m['modules'])==39 and len({r['name']for r in m['modules']})==13)
 check('Fifteen original maps reused without texture copies',m['new_textures']==0 and len(m['textures'])==15 and not list(art.glob('*.png')) and m['textures']==json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartFR01/manifest.json').read_text())['textures'])
 for r in m['modules']+m['textures']:
  path=ROOT/r['path'];check('Exact payload '+path.name,path.is_file() and path.stat().st_size==r['bytes'] and hashlib.sha256(path.read_bytes()).hexdigest()==r['sha256'])
  if path.suffix=='.fbx':
   check('Real binary FBX '+path.name,path.read_bytes().startswith(b'Kaydara FBX Binary'))
   check('Shared slots and grounded source '+path.name,set(r['materials'])<=set(['Masonry','CarvedTrim','SlateTimber','MossRoot','AmethystBronze']) and abs(r['source_bounds'][0][2])<.001)
 for name in sorted({r['name']for r in m['modules']}):
  group=sorted([r for r in m['modules']if r['name']==name],key=lambda r:r['lod'])
  check('Three genuine LODs '+name,[r['lod']for r in group]==[0,1,2] and group[0]['triangles']>=group[1]['triangles']>=group[2]['triangles']>0)
  check('Structural height retained '+name,max(abs((r['source_bounds'][1][2]-r['source_bounds'][0][2])-(group[0]['source_bounds'][1][2]-group[0]['source_bounds'][0][2]))for r in group)<.12)
 source=ROOT/m['source'];check('Editable source payload',source.stat().st_size==m['source_bytes'] and hashlib.sha256(source.read_bytes()).hexdigest()==m['source_sha256'])
 protected=['Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.Gothic04OB.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.HeartFR01.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.GroundL3.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityDressing.Frost.cs','Assets/NeonSyndicate/Scripts/Runtime/World/GothicHeartH3.cs','Assets/NeonSyndicate/Scripts/Runtime/World/GothicCivicH5.cs','Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.cs','Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.Refinement.cs']
 for name in protected:check('Frozen owner '+name,inherited_source_bytes(ROOT,name)==subprocess.check_output(['git','show',BASE+':'+name],cwd=ROOT))
 for tree in ['Assets/NeonSyndicate/Art/LiveCast','Assets/NeonSyndicate/Art/ProductionH9','Assets/NeonSyndicate/Art/HeartFR01','ArtSource/HeartFR01','Assets/NeonSyndicate/Art/WorldFoundationH8R','Assets/NeonSyndicate/Scripts/Runtime/Gameplay','Assets/NeonSyndicate/Scripts/Runtime/Core','Assets/NeonSyndicate/Scripts/Runtime/Combat','Assets/NeonSyndicate/Scripts/Runtime/AI','Assets/NeonSyndicate/Scripts/Runtime/Vehicles','Assets/NeonSyndicate/Resources/Quests','Assets/NeonSyndicate/Resources/Events','Packages','ProjectSettings']:
  check('Original tree '+tree,not subprocess.check_output(['git','diff',BASE,'--',tree],cwd=ROOT).strip())
 old_tests=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE,'Assets/NeonSyndicate/Tests'],cwd=ROOT,text=True).splitlines()
 check('Every preexisting test and category retained',all(inherited_source_bytes(ROOT,name)==subprocess.check_output(['git','show',BASE+':'+name],cwd=ROOT)for name in old_tests))
 result={'scope':__doc__,'fetched_baseline':BASE,'passed':sum(r['passed']for r in rows),'total':len(rows),'checks':rows,'native_unity':'NOT RUN'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:result[k]for k in ['passed','total','native_unity']}|{'failed':[r['name']for r in rows if not r['passed']]}));return int(result['passed']!=len(rows))
if __name__=='__main__':raise SystemExit(main())
