from surface_lineage_wr02 import inherited_source_bytes
"""FR01 real asset integrity and exact preservation. Not a native importer or gameplay test."""
import argparse,hashlib,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];BASE='6a779c232eecad2925e2b2bbc9cf77a3201399a6'
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();rows=[]
 def check(n,v):rows.append({'name':n,'passed':bool(v)})
 art=ROOT/'Assets/NeonSyndicate/Art/HeartFR01';m=json.loads((art/'manifest.json').read_text());check('Original rights and complete production families',m['license']=='CC0-1.0' and m['external_inputs']==[] and len(m['modules'])==33 and len(m['textures'])==15)
 for r in m['modules']+m['textures']:
  f=ROOT/r['path'];check('Exact delivered bytes '+f.name,f.is_file()and f.stat().st_size==r['bytes']and hashlib.sha256(f.read_bytes()).hexdigest()==r['sha256'])
  if f.suffix=='.fbx':check('Binary FBX '+f.name,f.read_bytes().startswith(b'Kaydara FBX Binary'))
 for name in set(r['name']for r in m['modules']):
  rs=sorted([r for r in m['modules']if r['name']==name],key=lambda r:r['lod']);check('Complete bounded LOD family '+name,[r['lod']for r in rs]==[0,1,2]and rs[-1]['triangles']<=rs[0]['triangles'] and all(r['uv_vertices']==r['triangles']*3 for r in rs))
 for r in m['textures']:
  f=ROOT/r['path'];im=np.array(Image.open(f));check('1K valid map '+f.name,im.shape[:2]==(1024,1024));meta=Path(str(f)+'.meta').read_text();check('Correct color space '+f.name,'sRGBTexture: '+('1'if '_BaseColor'in f.name else'0')in meta)
 protected=['Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.GroundL3.cs','Assets/NeonSyndicate/Scripts/Runtime/World/CityDressing.Frost.cs','Assets/NeonSyndicate/Scripts/Runtime/World/GothicHeartH3.cs','Assets/NeonSyndicate/Scripts/Runtime/World/GothicCivicH5.cs','Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs','Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs','Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']
 for n in protected:check('Frozen owner '+n,inherited_source_bytes(ROOT,n)==subprocess.check_output(['git','show',BASE+':'+n],cwd=ROOT))
 for n in ['Assets/NeonSyndicate/Art/LiveCast','Assets/NeonSyndicate/Art/ProductionH9','Assets/NeonSyndicate/Art/WorldFoundationH8R','Assets/NeonSyndicate/Resources/Quests','Assets/NeonSyndicate/Resources/Events']:
  check('Original source content '+n,not subprocess.check_output(['git','diff','--name-only',BASE,'--',n],cwd=ROOT).strip())
 runtime=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs').read_text()
 check('No runtime file IO, new game loop or reward controller',all(x not in runtime for x in ['File.Read','void Update(','TakeDamage(','.Spend(','SaveData','SetCinematic(','timeScale','ClearLandmark']))
 check('Visual source partition, never shared renderer suppression','pair.Legacy.SetActive'in runtime and 'pair.Failed=true'in runtime and 'GameObject old=Shapes.MeshObject'in runtime)
 check('New collision bounded to two gate bases','solid.AddComponent<BoxCollider>().size=new Vector3(5,12,5)'in runtime and 'MeshCollider'not in runtime)
 check('Opt-in, comparison and suspended lifetime','PlayerPrefs.GetInt("NSLV_heart_fr01",0)'in runtime and 'HeartPlanFR01.Active'in runtime)
 result={'scope':__doc__,'baseline':BASE,'passed':sum(r['passed']for r in rows),'total':len(rows),'checks':rows,'native_unity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':result['passed'],'total':len(rows),'failed':[r['name']for r in rows if not r['passed']]}));return int(result['passed']!=len(rows))
if __name__=='__main__':raise SystemExit(main())
