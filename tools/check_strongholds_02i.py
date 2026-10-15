#!/usr/bin/env python3
"""Checks actual 02I asset bytes, source contracts, placement arithmetic and retained source files.
Not a C# compiler, Unity invocation, playtest, renderer or performance benchmark.
"""
from pathlib import Path
import argparse, hashlib, json, math, re, struct, sys
import numpy as np
from PIL import Image
P=Path(__file__).resolve().parents[1];R=P/'Assets/NeonSyndicate/Resources/CrystalStrongholds02I';S=P/'Assets/NeonSyndicate/Scripts/Runtime'
checks=[]
def check(label,ok):checks.append({'check':label,'passed':bool(ok)})
def norm(p):return p.read_text(encoding='utf-8-sig').replace('\r\n','\n').replace('\r','\n')
def strip(s):return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'','',s,flags=re.S)
def balanced(s):
 stack=[]
 for c in strip(s):
  if c in '({[':stack.append(c)
  elif c in ')}]':
   if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]:return False
 return not stack
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--baseline',type=Path);ap.add_argument('--report',type=Path);a=ap.parse_args()
 cat=json.loads((R/'strongholds_catalog.json').read_text());inv=json.loads((R/'model_inventory.json').read_text())
 check('three fixed save identities',[d['id'] for d in cat['sites']]==['hearthspire','galehook','sunscar'])
 check('exactly one peaceful home',sum(bool(d['home']) for d in cat['sites'])==1 and cat['sites'][0]['guardSpecies']==[])
 for d in cat['sites']:
  check(d['id']+' bounded anchor',len(d['anchor'])==3 and all(math.isfinite(x) and abs(x)<=1500 for x in d['anchor']))
  check(d['id']+' guard budget',len(d['guardSpecies'])<=2)
 models={}
 for row in inv['models']:
  name=row['model'];f=R/'Geometry'/(name+'.bytes');raw=f.read_bytes();n=struct.unpack('<i',raw[4:8])[0];data=np.frombuffer(raw[8:],dtype='<f4').reshape(-1,8)
  check(name+' CMH1 header and length',raw[:4]==b'CMH1' and 0<n<=300000 and n%3==0 and len(raw)==8+n*32)
  check(name+' finite',np.isfinite(data).all());check(name+' normals',np.allclose(np.linalg.norm(data[:,3:6],axis=1),1,atol=.0001))
  tri=data[:,:3].reshape(-1,3,3);cross=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);area=np.linalg.norm(cross,axis=1)
  check(name+' nondegenerate',np.all(area>1e-8));dot=np.sum(cross*data[::3,3:6],axis=1)
  check(name+' winding agrees with normal',np.all(dot>0));check(name+' inventory digest',hashlib.sha256(raw).hexdigest()==row['sha256'])
  obj=R/'Models'/(name+'.obj');positions=[];normals=[];uv=[];faces=[]
  for line in obj.read_text().splitlines():
   p=line.split()
   if not p:continue
   if p[0]=='v':positions.append(list(map(float,p[1:])))
   elif p[0]=='vn':normals.append(list(map(float,p[1:])))
   elif p[0]=='vt':uv.append(list(map(float,p[1:])))
   elif p[0]=='f':faces.append([int(v.split('/')[0])-1 for v in p[1:]])
  check(name+' OBJ matches binary vertices',np.allclose(positions,data[:,:3],atol=2e-6) and len(faces)*3==n)
  check(name+' OBJ matches binary normals/UV',np.allclose(normals,data[:,3:6],atol=2e-6) and np.allclose(uv,data[:,6:],atol=2e-6))
  check(name+' sequential triangles',np.array_equal(np.array(faces).reshape(-1),np.arange(n)))
  models[name]=dict(vertices=n,triangles=n//3,lo=data[:,:3].min(0),hi=data[:,:3].max(0))
 check('26 variant meshes',len(models)==26)
 for name in list(models):
  if not name.endswith('_lod'):check(name+' distant mesh no heavier',models[name+'_lod']['vertices']<=models[name]['vertices'])
 for img in sorted((R/'Textures').glob('*.png')):
  with Image.open(img) as im:im.load();check(img.name+' 512px texture',im.size==(512,512) and im.mode in ('RGB','RGBA'))
 newfiles=list((S/'Gameplay/Strongholds').glob('*.cs'))+[P/'Assets/NeonSyndicate/Tests/EditMode/Stronghold02ITests.cs']
 for f in newfiles:
  text=norm(f);check(f.name+' balanced delimiters',balanced(text));check(f.name+' ASCII',text.isascii());check(f.name+' metadata',Path(str(f)+'.meta').exists())
 allguids={};duplicates=[]
 for f in (P/'Assets').rglob('*.meta'):
  m=re.search(r'^guid: ([a-f0-9]{32})$',norm(f),re.M)
  if m:
   guid=m.group(1)
   if guid in allguids and ('Stronghold' in str(f) or 'Stronghold' in str(allguids[guid])):duplicates.append((str(f),str(allguids[guid])))
   allguids[guid]=f
 check('new metadata GUID uniqueness',not duplicates)
 for f in R.rglob('*'):
  if f.is_file() and f.suffix!='.meta':check(f.relative_to(R).as_posix()+' metadata exists',Path(str(f)+'.meta').exists())
 code={f.name:norm(f) for f in newfiles};runtime='\n'.join(v for k,v in code.items() if k!='Stronghold02ITests.cs')
 for forbidden in ['UnityEngine.InputSystem','UnityEditor','Time.timeScale =','SceneManager.LoadScene','PlayerPrefs.','Application.persistentDataPath','NonParallelizable','Resources.Load<GameObject>','new NavMesh','Physics.IgnoreLayerCollision']:
  check('no new '+forbidden,forbidden not in runtime)
 system=code['StrongholdSystem.cs'];book=code['StrongholdBook.cs'];view=code['StrongholdView.cs'];ui=code['StrongholdSystem.UI.cs']
 for token in ['NoRewards=true','Persistent=true','Game.State==GameState.Paused','Book.AdvanceCapture','Health.Died+=','Died-=','WorldSystems.Register(this)','MenuPages.Register(_page)','SaveData save','Book.Restore','Book.Infuse()']:
  check('runtime '+token,token in system)
 for token in ['Resonance * .04f','Resonance * .03f','Math.Min(2,n)','guards != d.guardSpecies.Length','Math.Min(.1f,dt)','ActiveRaid == null','s.EchoInfused','SlotCount = 6']:
  check('ledger '+token,token in book)
 check('world interaction and controller menu used','Interactable.Create' in view and 'UIKit.Button' in ui)
 check('new physics isolated to fort view','AddComponent<BoxCollider>' in view and 'c.layer=Layers.World' in view)
 check('collision-free art stage before swap','staged.SetActive(false)' in view and 'previous.SetActive(false)' in view and 'staged.SetActive(true)' in view)
 check('low detail batching','CombineMeshes' in view and 'new LOD' in view and 'UploadMeshData(true)' in view)
 check('confirmed H binary decoder reused','CrystalCity02HMeshFallback.Decode' in code['StrongholdAssets.cs'])
 check('boot before title/save spawn',norm(S/'Core/GameRoot.cs').index('Game.Strongholds.BuildWorld()')<norm(S/'Core/GameRoot.cs').index('EnterTitle();'))
 check('static registry reset','Strongholds = null;' in norm(S/'Core/Game.cs'))
 check('save field append','public StrongholdSave Strongholds = new StrongholdSave();' in norm(S/'Gameplay/SaveSystem.cs'))
 check('save freeze expanded','"Strongholds:StrongholdSave"' in norm(P/'Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs'))
 travel=norm(S/'Characters/PlayerController.Travel.cs')
 check('aim drift preserved',travel.count('!Aiming && Game.Strongholds != null')==2)
 check('flight target integration','TravelMath.FlightSpeed(fast, Aiming) * fortSpeed' in travel and 'TravelMath.FlightClimbSpeed(fast, Aiming) * fortClimb' in travel)
 # Collider and slot positions correspond to shipped mesh dimensions, not estimates from screenshots.
 check('foundation top .07',abs(models['foundation']['hi'][1]-.07)<1e-5)
 check('tower deck inside base',models['tower']['lo'][0]>=-2.5 and models['tower']['hi'][0]<=2.5)
 for module in ['tower','wall','dais','trellis']:
  bounds=models[module];radius=max(abs(bounds['lo'][0]),abs(bounds['lo'][2]),abs(bounds['hi'][0]),abs(bounds['hi'][2]))
  check(module+' inside all six rotated sockets',radius+11<16 and radius+8<16)
 check('gate pedestrian width',3.7*2-1.12>6)
 # Read API declarations actually present in baseline, to catch guessed method names.
 for file,token in [('Creatures/CreatureDirector.cs','public CharacterBase Spawn('),('Creatures/Creature.cs','public bool Ready'),('Characters/CharacterHealth.cs','event Action<DamageInfo> Died'),('UI/Minimap.cs','public static Blip Add('),('Core/Interactables.cs','public static Interactable Create('),('ArtDistrict/CrystalCity02HMeshFallback.cs','public static Mesh Decode('),('ArtDistrict/CrystalCity02FAssets.cs','public static Material Surface(')]:
  check('existing API '+token,token in norm(S/file))
 if a.baseline:
  allowed={'Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs','Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs','Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs','Assets/NeonSyndicate/Scripts/Runtime/Characters/PlayerController.Travel.cs','Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs'}
  retained=[];changed=[]
  for f in a.baseline.rglob('*'):
   if not f.is_file():continue
   rel=f.relative_to(a.baseline).as_posix();dest=P/rel
   if not dest.exists() or dest.read_bytes()!=f.read_bytes():changed.append(rel)
   else:retained.append(rel)
  check('exactly approved five original files changed',set(changed)==allowed)
  check('save existing fields are retained',norm(S/'Gameplay/SaveSystem.cs').replace('        // 02I append-only local fort ledger. Old saves start with unclaimed forts.\n        public StrongholdSave Strongholds = new StrongholdSave();\n','')==norm(a.baseline/'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs'))
 report={'scope':'Actual binary/OBJ asset data, source contracts, retained files, independent geometry arithmetic. NOT C# or Unity execution.',
 'checks':len(checks),'passed':sum(c['passed'] for c in checks),'failed':[c for c in checks if not c['passed']],
 'retained_baseline_files':len(retained) if a.baseline else None,'unity_compile':'NOT RUN','unity_playtest':'NOT RUN','native_editmode_tests':'NOT RUN','frame_rate':'NOT MEASURED',
 'mesh_triangles':{k:v['triangles'] for k,v in models.items()},'source_hashes':{f.relative_to(P).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in newfiles}}
 if a.report:a.report.write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps(report,indent=2));sys.exit(bool(report['failed']))
