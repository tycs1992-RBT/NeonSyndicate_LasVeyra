#!/usr/bin/env python3
"""Independent byte/channel/seam/import and owner checks. Does not import into Unity."""
import argparse,hashlib,json,subprocess
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]

def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();rows=[]
 def check(n,condition):rows.append({'name':n,'passed':bool(condition)})
 art=ROOT/'Assets/NeonSyndicate/Art/LiveGround/Redspire';receipt=json.loads((art/'manifest.json').read_text())
 check('Original lawful two-family source',receipt['license']=='CC0-1.0' and receipt['external_inputs']==[] and len(receipt['files'])==6)
 for row in receipt['files']:
  f=ROOT/row['path'];check('Exact bytes '+f.name,f.stat().st_size==row['bytes'] and hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'])
  with Image.open(f)as im:im.load();pix=np.array(im)
  check('Square 1K '+f.name,pix.shape[:2]==(1024,1024))
  check('Exact opposing repeat edges '+f.name,np.array_equal(pix[0],pix[-1]) and np.array_equal(pix[:,0],pix[:,-1]))
  meta=Path(str(f)+'.meta').read_text();srgb='_BaseColor'in f.name
  check('Color space '+f.name,('sRGBTexture: '+str(int(srgb)))in meta)
  check('Repeat mipmapped bounded import '+f.name,all(x in meta for x in ['enableMipMap: 1','maxTextureSize: 1024','wrapU: 0','wrapV: 0','textureCompression: 0']))
  if '_Normal'in f.name:
   n=pix.astype(float)/255*2-1
   check('Normal unit vectors '+f.name,np.quantile(abs(np.linalg.norm(n,axis=2)-1),.999)<.025)
   check('Normal importer +Y '+f.name,'textureType: 1'in meta and 'flipGreenChannel: 0'in meta)
  if '_MetallicSmoothness'in f.name:
   check('Nonmetallic RGB '+f.name,np.max(pix[:,:,:3])==0)
   check('Rough earth smoothness alpha '+f.name,pix.shape[2]==4 and np.min(pix[:,:,3])>=0 and np.max(pix[:,:,3])<110 and np.ptp(pix[:,:,3])>15)
 runtime=ROOT/'Assets/NeonSyndicate/Scripts/Runtime'
 source=(runtime/'World/LiveGround/LiveGroundMaterialsL3.cs').read_text()
 check('No runtime image decoding/filesystem or frame loop',all(s not in source for s in ['File.Read','LoadImage(','void Update(','AddComponent<','Rebuild(']))
 check('Shared map and material lifetime reset','Dispose(_mask)'in source and 'Proxies.Clear()'in source and 'LiveGroundMaterialsL3.ResetStatics();'in (runtime/'World/Wilds/WildsMaterials.cs').read_text())
 baseline='7a28c90f00d25a06ff946cd47f6c17f0e6a99b2e'
 for rel in ['Core/Game.cs','Core/GameRoot.cs','Gameplay/SaveSystem.cs','Characters/PlayerController.cs','Characters/PlayerCamera.cs','Characters/PlayerCombat.cs','World/Wilds/WildsLayout.cs','World/Wilds/WildsShapes.cs','World/Wilds/WildsSites.cs','World/Wilds/WildsTypes.cs','World/Wilds/WildsSystem.cs','World/Wilds/WildsChunkL1.cs','World/Wilds/WildsChunkL1.ArtL2.cs','World/Wilds/WildsSystem.ArtL2.cs']:
  path=runtime/rel;expected=subprocess.check_output(['git','show',baseline+':'+path.relative_to(ROOT).as_posix()],cwd=ROOT)
  check('Original owner '+rel,path.read_bytes()==expected)
 city=(runtime/'World/CityBuilder.cs').read_text();frost=(runtime/'World/CityDressing.Frost.cs').read_bytes()
 original_frost=subprocess.check_output(['git','show',baseline+':Assets/NeonSyndicate/Scripts/Runtime/World/CityDressing.Frost.cs'],cwd=ROOT)
 check('Original frost geometry and snow material owner byte-identical',frost==original_frost)
 original_city=subprocess.check_output(['git','show',baseline+':Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs'],cwd=ROOT,text=True)
 expected_city=original_city.replace('_noShadow.Add(Mats.Sand);','_noShadow.Add(Mats.Sand);\n            _noShadow.Add(LiveGroundMaterialsL3.Floor);')
 expected_city=expected_city.replace('Get(piece.Center(0f), Mats.Sand, 12f)','Get(piece.Center(0f), LiveGroundMaterialsL3.Floor, 12f)').replace('Get(c, Mats.Sand, 12f)','Get(c, LiveGroundMaterialsL3.Floor, 12f)')
 expected_city=expected_city.replace('if (!BuildFrostTile(c, x, z, x1, z1))','if (!BuildFrostGroundL3(c, x, z, x1, z1))')
 expected_city=expected_city.replace('public MeshBuilder Get(Vector3 at, Material mat, float tile)\n        {','public MeshBuilder Get(Vector3 at, Material mat, float tile)\n        {\n            if (_frostGroundL3 && mat == Mats.Sand) mat = LiveGroundMaterialsL3.Floor;')
 check('Exact floor-only routing edits preserve all original batching and collision source',city==expected_city)
 scoped=(runtime/'World/CityBuilder.GroundL3.cs').read_text()
 check('Original frost builder runs under exception-safe instance scope',all(s in scoped for s in ['private bool _frostGroundL3;', 'bool previous = _frostGroundL3;', 'try\n', '_frostGroundL3 = true;', 'return BuildFrostTile(center, x0, z0, x1, z1);', 'finally { _frostGroundL3 = previous; }']) and 'static 'not in scoped)
 check('Original rendered floor and frost bare-ground connected',city.count('LiveGroundMaterialsL3.Floor, 12f')==2 and 'if (_frostGroundL3 && mat == Mats.Sand) mat = LiveGroundMaterialsL3.Floor;'in city and 'BuildFrostTile(center, x0, z0, x1, z1)'in scoped)
 check('Original no-shadow floor policy','_noShadow.Add(LiveGroundMaterialsL3.Floor);'in city)
 check('Default off and no progression block','PlayerPrefs.GetInt(GroundPreferenceL3, 0)'in (runtime/'World/Wilds/WildsSystem.GroundL3.cs').read_text())
 check('Existing comparison restores ground','CompareGroundL3(true)'in (runtime/'UI/VisibleRealityV0.cs').read_text() and 'CompareGroundL3(false)'in (runtime/'UI/VisibleRealityV0.cs').read_text())
 check('URP source cannot import on package-absent Built-in',not(art/'RedspireGroundURP.shader').exists() and (ROOT/'ArtSource/RedspireL3/RedspireGroundURP.shader.txt').is_file())
 result={'scope':__doc__,'passed':sum(r['passed']for r in rows),'total':len(rows),'checks':rows,'native_unity':'NOT RUN'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'passed':result['passed'],'total':len(rows),'failed':[r['name']for r in rows if not r['passed']]}));return int(result['passed']!=len(rows))
if __name__=='__main__':raise SystemExit(main())
