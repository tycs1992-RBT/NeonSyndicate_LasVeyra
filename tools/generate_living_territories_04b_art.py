#!/usr/bin/env python3
"""Original 04B hero geometry and collision-authored 48m compositions.

Reuses the 04A mesh author's mathematical helpers, not duplicate mesh payloads.
All outputs are project-owned original geometry, metres Y-up. No external art.
"""
from pathlib import Path
import argparse, hashlib, json, math, re, struct, uuid
import numpy as np
from generate_visual_rebirth_04a import Mesh, polar, TAU
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/NeonSyndicate/Resources/LivingTerritories04B'
ROLES={'resonance_armillary':'metal','rootwater_heart':'bark','rift_extractor':'metal',
       'canopy_shelter':'bark','crystal_conduit':'metal','study_terminal':'trim'}

def meta(path, kind='text'):
 target=Path(str(path)+'.meta')
 if target.exists():return
 guid=uuid.uuid5(uuid.NAMESPACE_URL,'crystal-manor-04b/'+path.relative_to(ROOT).as_posix()).hex
 if kind=='obj':
  text=(ROOT/'Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj.meta').read_text()
  text=re.sub(r'guid: [a-f0-9]+','guid: '+guid,text)
  text=re.sub(r'userData:.*','userData: Original 04B geometry. Metres Y-up.',text)
 else:
  text='fileFormatVersion: 2\nguid: '+guid+'\n'
  text+= 'folderAsset: yes\nDefaultImporter:\n' if kind=='folder' else 'TextScriptImporter:\n'
  text+='  externalObjects: {}\n  userData: Original Living Territories 04B artwork\n  assetBundleName:\n  assetBundleVariant:\n'
 target.write_text('\n'.join(x.rstrip() for x in text.splitlines())+'\n')

def model(name,low):
 m=Mesh();n=12 if low else 32;s=6 if low else 10
 if name=='resonance_armillary':
  # Three flying buttresses cradle an astronomical cage. Empty core is visible.
  m.lathe([(0,0),(3.3,0),(3.3,.3),(2.9,.55),(2.5,.55),(2.5,.8),(0,.8)],n)
  for a in [math.pi/2,math.pi*7/6,math.pi*11/6]:
   m.sweep([polar(a,2.8,.5),polar(a,3,2.5),polar(a,2.7,5),polar(a,1.8,6.2)], [.38,.3,.22,.12],s)
   m.box(polar(a,2.8,.75),(.9,.4,.9),.08)
   m.sweep([polar(a,2.1,.8),polar(a,1.8,2.6),polar(a,1.1,3.3)],[.18,.14,.1],s)
  m.ring(2.25,.13,(0,5,0),n,s,tilt=math.pi*.31)
  m.ring(1.95,.11,(0,5,0),n,s,tilt=-math.pi*.33)
  m.ring(1.6,.09,(0,5,0),n,s,tilt=math.pi*.5)
  m.lathe([(0,.8),(.65,.8),(.5,1.2),(.3,2.6),(.5,2.9),(0,3)],n)
  for i in range(6 if low else 12):
   a=i*TAU/(6 if low else 12);m.box(polar(a,2.85,.48),(.2,.15,.35),.02)
 elif name=='rootwater_heart':
  # Open-sided root throne, four grounded feet, split crown and woven seed cup.
  for i in range(4):
   a=i*TAU/4+math.pi*.25
   m.sweep([polar(a,3.5,.12),polar(a,2.7,.4),polar(a,1.9,1.6),polar(a,2,4.5),polar(a,3.3,7.6),polar(a,2.1,9)], [.55,.7,.6,.48,.25,.07],s,fluting=.1)
   m.sweep([polar(a,2.5,.2),polar(a+.2,1.2,1.4),polar(a+.4,.5,2.3)],[.3,.26,.13],s)
   m.sweep([polar(a,2.1,4),polar(a+.5,3.4,5),polar(a+.7,4,5.4)],[.25,.18,.02],s)
  for h,r in [(1.5,1.45),(1.9,1.2),(2.3,.85)]:m.ring(r,.11,(0,h,0),n,s)
  if not low:
   for i in range(12):
    a=i*TAU/12;m.sweep([polar(a,1.6,1.4),polar(a+.35,1.3,1.9),polar(a+.6,.9,2.3)],[.065,.06,.04],6)
 elif name=='rift_extractor':
  # Wide anchored machine, paired flywheels, piston braces and split drill jaw.
  m.box((0,.35,0),(6.2,.7,4.8),.16)
  for x in [-2.25,2.25]:
   m.box((x,2.1,.5),(.8,3.5,1.6),.1)
   m.sweep([(x,3.4,.5),(x,5.6,.5),(x*.5,6.9,.5)], [.44,.4,.32],s)
   for z in [-1.7,1.8]:m.rod((x,.6,z),(x,3.8,.5),.2,sides=s)
   m.ring(.75,.18,(x,3.2,.5),n,s,tilt=math.pi*.5)
  m.box((0,6.5,.5),(4.4,.8,1.1),.1)
  m.lathe([(0,1.2),(.35,1.4),(.6,2.2),(.45,2.5),(.7,3.2),(.7,4.3),(.95,4.5),(.95,4.8),(.55,5.1),(.55,6.4),(0,6.4)],n,center=(0,0,.5))
  for i in range(3 if low else 6):
   a=i*TAU/(3 if low else 6)
   m.sweep([polar(a,.4,1.6)+(0,0,.5),polar(a+.5,.75,2.9)+(0,0,.5),polar(a+1,.75,4.2)+(0,0,.5)],[.12,.14,.12],s)
  for x in [-1.8,1.8]:m.box((x,.85,-1.9),(1.15,.3,.45),.04)
 elif name=='canopy_shelter':
  # Four rooted legs, braided rafters and layered fern roof; interior 4m headroom.
  for x in [-2.6,2.6]:
   for z in [-2,2]:
    m.sweep([(x*1.15,0,z*1.15),(x,.6,z),(x,3.3,z),(x*.8,4.2,z*.8)],[.3,.26,.19,.14],s)
    m.rod((x,3,z),(0,4.8,0),.12,sides=s)
  for z in [-1.7,0,1.7]:m.sweep([(-3.3,3.8,z),(0,4.8,z*.8),(3.3,3.8,z)],[.13,.17,.13],s)
  for j in range(6 if low else 10):
   z=-2.3+j*4.6/(5 if low else 9)
   for side in [-1,1]:m.leaf((0,4.85,z),(side*3.6,3.9,z+.2),.65 if low else .55,.12,lobes=.05)
 elif name=='crystal_conduit':
  # 6m supported horizontal service pipe with flanges and readable inspection port.
  m.rod((0,1.45,-3),(0,1.45,3),.28,sides=n)
  for z in [-2.5,0,2.5]:
   m.box((0,.16,z),(1.4,.32,.7),.04)
   m.box((0,.7,z),(.25,1.1,.35),.025)
   m.ring(.4,.09,(0,1.45,z),n,s,tilt=math.pi*.5)
  m.box((0,1.8,0),(.7,.7,.8),.08)
  m.rod((-.65,2.05,0),(.65,2.05,0),.07,sides=s)
  if not low:
   for z in [-2.5,0,2.5]:
    for a in range(6):
     t=a*TAU/6;m.box((math.cos(t)*.4,1.45+math.sin(t)*.4,z),(.1,.1,.12),.01)
 elif name=='study_terminal':
  # Low accessible writing desk, sculpted legs, sloped crystal lectern and drawers.
  for x in [-.95,.95]:
   m.box((x,.08,0),(.55,.16,1.25),.04)
   m.sweep([(x,.15,.35),(x,.65,.25),(x,1.12,0)],[.18,.16,.12],s)
   m.rod((x,.2,-.4),(x,1.1,0),.1,sides=s)
  m.box((0,1.15,0),(2.7,.18,1.6),.07)
  m.box((0,.88,.15),(1.7,.32,.8),.04)
  m.quad((-1,1.24,-.45),(1,1.24,-.45),(1,1.65,.35),(-1,1.65,.35),np.array([0,1,-.5]))
  m.rod((-1,1.23,-.46),(1,1.23,-.46),.045,sides=s)
  for x in [-.5,.5]:m.box((x,.9,-.3),(.45,.06,.08),.01)
 else:raise ValueError(name)
 if name=='rootwater_heart':
  for vertex in m.vertices:vertex[1]+=.4 # Ground the radial root feet; retain normals and a shared LOD pivot.
 return m

def export(name,low):
 m=model(name,low);data=np.asarray(m.vertices,dtype='<f4');variant=name+('_low' if low else '_high')
 assert np.isfinite(data).all() and len(data)>0 and len(data)%3==0
 tris=data[:,:3].reshape(-1,3,3);cross=np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0]);areas=np.linalg.norm(cross,axis=1)*.5
 assert areas.min()>1e-10
 assert np.max(abs(np.linalg.norm(data[:,3:6],axis=1)-1))<1e-5
 payload=b'CMH1'+struct.pack('<i',len(data))+data.tobytes();p=DEST/'Meshes'/(variant+'.bytes');p.write_bytes(payload);meta(p)
 obj=DEST/'Models'/(variant+'.obj')
 with obj.open('w') as f:
  f.write('# Original 04B. Metres Y-up. Role '+ROLES[name]+'\no '+variant+'\n')
  for r in data:f.write('v %.7f %.7f %.7f\n'%tuple(r[:3]))
  for r in data:f.write('vt %.7f %.7f\n'%tuple(r[6:]))
  for r in data:f.write('vn %.7f %.7f %.7f\n'%tuple(r[3:6]))
  for i in range(0,len(data),3):f.write('f '+' '.join(f'{k}/{k}/{k}' for k in range(i+1,i+4))+'\n')
 meta(obj,'obj')
 return {'name':name,'variant':variant,'lod':int(low),'role':ROLES[name],'vertices':len(data),'triangles':len(data)//3,'bounds_min':data[:,:3].min(axis=0).tolist(),'bounds_max':data[:,:3].max(axis=0).tolist(),'sha256':hashlib.sha256(payload).hexdigest(),'binary':p.relative_to(ROOT).as_posix(),'source':obj.relative_to(ROOT).as_posix()}

def part(model,role,p,s=(1,1,1),yaw=0,solid=False,far=True,tier=0,owned=False,new=False,variant=0,progress=0,resource=False):
 return {'model':model,'role':role,'position':list(p),'scale':list(s),'euler':[0,yaw,0],'solid':solid,'far':far,'tier':tier,'owned':owned,'source':'04B' if new else '04A','variant':variant,'progress':progress,'resource':resource}

def layout(kind):
 nodes=[]
 def add(*a,**kw):nodes.append(part(*a,**kw))
 relay=kind.startswith('relay_');region=kind.split('_')[-1]
 verdant=region=='verdant' or kind in ['habitat_sanctuary','habitat_canopy','habitat_transition']
 obsidian=region=='obsidian' or kind=='habitat_rift'
 style=2 if verdant else 3 if obsidian else 0
 # Ground-supported thin paths are authored quads emitted by runtime, not raised colliders.
 # Cover pockets sit beyond clear 9m cross. All live AI anchors below remain empty.
 # Authored low lips ground the composition: planted soils or worked stone quadrants.
 for sx in [-1,1]:
  for sz in [-1,1]:
   add('terrace','soil' if verdant else 'roof' if obsidian else 'stone',(sx*14,.005,sz*14),(1.8,.04,1.8),solid=True)
 for x in [-18,18]:
  for z in [-18,18]:
   add('ward_pylon','trim',(x,0,z),(.6,.75,.6),solid=True)
   add('lantern','metal',(x,2.8,z),(.5,.5,.5),far=False)
 if relay:
  hero={'amethyst':'resonance_armillary','verdant':'rootwater_heart','obsidian':'rift_extractor'}[region]
  add(hero,ROLES[hero],(0,0,13),solid=True,new=True)
  add('crystal_cluster','crystal',(0,3.2 if region=='amethyst' else 2.4,13),(.55,.55,.55))
  add('sigil','gilt',(0,.1,7.6),(1.3,1.3,1.3),0,far=True) # swapped to horizontal below
  nodes[-1]['euler']=[90,0,0]
  # Hub-only rune station. The bright source layer extinguishes after its finite cache is claimed.
  for role,height,live in [('metal',.08,False),('crystal',.1,True)]:
   add('sigil',role,(0,height,3.4),(1.1,1.1,1.1),variant=1,resource=live)
   nodes[-1]['euler']=[90,0,0]
  for x in [-7.5,7.5]:
   add('wall_panel','stone',(x,0,-9),(.7,.38,1),solid=True)
   add('wall_panel','stone',(x,0,8),(.7,.38,1),solid=True)
   add('banner','cloth',(x,1.7,-9),(.75,.75,.75))
   add('column','metal',(x,0,-9.1),(.17,.75,.17),solid=True)
  # Fortification adds cover behind original pockets, never closes approach lanes.
  for x in [-18,18]:
   add('wall_panel','stone',(x,0,-7),(.8,.58,1),90,solid=True,tier=1)
   add('buttress','roof',(x,0,9),(.7,.65,.7),90 if x<0 else 270,solid=True,tier=2)
   add('crystal_conduit','metal',(x,0,14),(1,1,1),solid=True,tier=2,new=True)
  # Each shipped family has three bounded secondary silhouettes, selected by stable site variant.
  if region=='amethyst':
   add('guardian','trim',(20,0,-10),(.6,.8,.6),270,solid=True,variant=1)
   add('portal','stone',(20,0,-10),(.55,.65,.55),90,solid=True,variant=2)
   add('spire','roof',(20,0,-10),(.6,.8,.6),solid=True,variant=3)
  elif region=='verdant':
   add('root_arch','bark',(20,0,-9),(.45,.65,.45),90,solid=True,variant=1)
   add('cypress_trunk','bark',(20,0,-9),(.65,.65,.65),solid=True,variant=2)
   add('cypress_canopy','leaf',(20,0,-9),(.65,.65,.65),variant=2)
   add('fountain','stone',(20,0,-9),(.6,.65,.6),solid=True,variant=3)
  else:
   add('ward_pylon','roof',(-20,0,-8),(1.2,1.2,1.2),solid=True,variant=1)
   add('ruin','stone',(-20,0,-8),(.8,1,.8),90,solid=True,variant=2)
   add('drill','metal',(-20,0,-8),(.6,.6,.6),solid=True,variant=3)
  for i in range(3):add('crystal_cluster','crystal',(-1.8+i*1.8,6.5,13),(.24,.28,.24),owned=True)
 else:
  add('study_terminal','trim',(12,0,-13),(1.15,1,1.15),new=True,solid=True)
  add('fountain','stone',(-13,0,-13),(.68,.55,.68),solid=True)
 # Safe fly-to overlook: continuous deck and four visually grounded columns. Exact source collider.
 ox=-15 if not obsidian else 15;oz=12
 add('terrace','stone',(ox,4.7,oz),(1.1,.5,1.1),solid=True)
 for dx in [-2.7,2.7]:
  for dz in [-2.7,2.7]:add('column','stone',(ox+dx,0,oz+dz),(.55,4.7/6,.55),solid=True)
 for dx,dz,yaw in [(-4,0,90),(4,0,90),(0,4,0)]:add('railing','metal',(ox+dx,5,oz+dz),(1.15,1,1),yaw,solid=True)
 if not obsidian:
  # Shelter is rooted; terminal below is an actual usable staff staging point.
  add('canopy_shelter' if verdant else 'market_awning','bark' if verdant else 'cloth',(-13,0,-13),(1,1,1),new=verdant,solid=verdant)
  add('study_terminal','trim',(-13,0,-14),(1,1,1),new=True,solid=True)
  if not verdant:
   # Explicit posts support the 04A awning, which only models cloth and frame.
   for dx in [-2.25,2.25]:
    for dz in [-1.35,1.35]:add('column','stone',(-13+dx,0,-13+dz),(.2,.4,.2),solid=True)
   add('observatory','stone',(15,0,14),(.6,.6,.6),solid=True)
   # A supported vaulted conversation room, with paired arcades and a writing station.
   for z in [-17.5,-11.5]:add('arcade','stone',(13,0,z),(1,.9,1),solid=True)
   for x in [10,16]:
    for z in [-17.5,-11.5]:add('column','stone',(x,0,z),(.35,.9,.35),solid=True)
   add('rib_vault','trim',(13,5.4,-14.5),(3/2.9,.55,3/2.9))
   add('roof','roof',(13,5.4,-14.5),(.9,.45,.9))
   add('bench','stone',(13,0,-13),(1,1,1),solid=True)
   add('study_terminal','trim',(13,0,-16),(.8,.9,.8),new=True,solid=True)
   for x in [10,16]:add('banner','cloth',(x,1.2,-11.45),(.65,.9,.65))
 else:
  add('drill','metal',(-15,0,13),(.85,1,.85),solid=True)
  add('crystal_conduit','metal',(-13,0,-14),(1,1,1),90,solid=True,new=True)
  add('ruin','stone',(14,0,-15),(1.2,.9,1),30,solid=True)
  for x in [-19,-12]:add('ore','crystal',(x,0,7),(.75,1.15,.75),45)
 if verdant:
  # Off-axis tree masses, true leaf geometry at several layers, understory below.
  for x,z,scale in [(14,13,.65),(-15,-14,.48),(15,-15,.45)]:
   add('living_trunk','bark',(x,0,z),(scale,scale*1.15,scale),solid=True)
   add('living_canopy','leaf',(x,0,z),(scale,scale*1.15,scale))
  add('root_arch','bark',(14,0,15),(.75,.8,.7),90,solid=True)
  add('terrace','water',(13,.032,12),(.85,.025,.75),solid=False)
  for x,z in [(10,10),(16,10),(10,14),(16,14)]:
   add('reeds','leaf',(x,.03,z),(.8,.9,.8),x*19,far=False)
   add('rock','stone',(x,.03,z),(.45,.25,.5),x*17,solid=True)
  for sx in [-1,1]:
   for sz in [-1,1]:
    for j,(x,z) in enumerate([(8,9),(10,8),(8,12),(11,7),(16,7),(18,11)]):
     add('fern','leaf',(sx*x,.035,sz*z),(1.15,1.35,1.15),j*59,far=False)
  for sx in [-1,1]:
   for sz in [-1,1]:
    for j,(x,z) in enumerate([(7,16),(10,19),(18,8),(20,14),(8,20)]):
     add('fern','leaf',(sx*x,.02,sz*z),(.9,1,.9),j*67,far=False)
     add('flower' if j%2 else 'mushroom','flower',(sx*(x+.7),.02,sz*(z-.7)),(.65,.8,.65),j*67,far=False)
  if kind=='habitat_canopy':
   add('canopy_shelter','bark',(-15,5,12),(1,1,1),new=True,solid=True)
   add('sky_ring','metal',(14,17,13),(.9,.9,.9))
  if kind=='habitat_transition':
   for z in [-18,-11,10,18]:add('rock','stone',(20,0,z),(1.1,.6,1.4),z*7,solid=True)
 elif obsidian:
  for x,z in [(-19,-17),(-9,18),(10,19),(20,-9)]:
   add('rock','roof',(x,0,z),(1.5,.8,1.2),x*11,solid=True)
   add('ore','crystal',(x,.5,z),(.7,1.1,.7),z*13)
  if kind=='habitat_rift':
   add('portal','roof',(0,0,14),(1.1,1.1,1.1),solid=True)
   for x in [-2,2]:add('crystal_cluster','crystal',(x,0,14),(.7,1,.7))
 else:
  for x,z in [(18,-18),(18,7),(-20,7),(-18,-19)]:
   add('planter','stone',(x,0,z),(1,.8,1),solid=True)
   add('flower','flower',(x,.8,z),(1.4,1.2,1.4),far=False)
 title={'relay_amethyst':'Resonance Court Relay','relay_verdant':'Rootwater Heart Relay','relay_obsidian':'Shatterworks Extractor Relay','habitat_sanctuary':'Rootwater Creature Sanctuary','habitat_canopy':'Canopy Observation Court','habitat_rift':'Hazardous Fracture Nest','habitat_transition':'Frontier Roaming Verge'}[kind]
 return {'version':'04B','id':kind,'title':title,'style':style,'nodes':nodes,'core':[0,.1,8 if relay else 0],
 'defenders':[{'position':p} for p in [[-10,0,-6],[10,0,-6],[-10,0,5],[10,0,5]]],
 'staff':[{'position':p} for p in [[-9,0,-3],[9,0,-3]]],'creatures':[{'position':p} for p in [[-9,0,0],[9,0,0],[0,0,-12],[0,0,6]]],
 'overlook':[ox,5.05,oz],'clearAreas':[{'rect':p} for p in [[-23,23,-4.5,4.5],[-4.5,4.5,-23,4.5]]],
 'notes':'48m separately surveyed plot. Two pedestrian approach routes and open center. Overlook is fly-to, genuine source-mesh collision at y5. No new input or gameplay implied by art. Fortifications occupy side pockets only.'}

def home_display():
 nodes=[part('study_terminal','trim',(0,0,-.7),new=True,solid=True),
        part('arcade','stone',(0,0,1),(1.1,.68,.7),solid=True),
        part('banner','cloth',(-2.8,.8,1),(.6,.6,.6)),
        part('banner','cloth',(2.8,.8,1),(.6,.6,.6))]
 for i in range(9):
  nodes.append(part('crystal_cluster','crystal',(-2.4+i*.6,2.1,1),(.12,.22,.12),progress=i+1))
 return {'version':'04B','id':'home_ledger','title':'Hearthspire Relay Steward','style':0,'nodes':nodes,
         'core':[0,0,-3],'staff':[{'position':[-2,0,-2.5]}],
         'notes':'Compact 7m by 5m ledger display. Nine independently lit crystals show owned relay count. No new ownership save block. Caller surveys and supplies safe location.'}

def main():
 p=argparse.ArgumentParser();p.add_argument('--confirm-regenerate',action='store_true');args=p.parse_args()
 if not args.confirm_regenerate:p.error('Pass --confirm-regenerate after protecting artist changes.')
 for folder in [DEST,DEST/'Models',DEST/'Meshes',DEST/'Layouts',DEST/'Props']:
  folder.mkdir(parents=True,exist_ok=True);meta(folder,'folder')
 records=[]
 for name in ROLES:
  for low in [False,True]:records.append(export(name,low))
 inv={'version':'04B','units':'metres','up_axis':'Y','unique_prototypes':len(ROLES),'variants':len(records),'license':'Original project-owned artwork, authored mathematical surfaces; no external sources.','models':records,'totals':{'lod0_triangles':sum(x['triangles'] for x in records if x['lod']==0),'lod1_triangles':sum(x['triangles'] for x in records if x['lod']==1)}}
 p=DEST/'Models/model_inventory.json';p.write_text(json.dumps(inv,indent=2)+'\n');meta(p)
 for name in ['relay_amethyst','relay_verdant','relay_obsidian','habitat_sanctuary','habitat_canopy','habitat_rift','habitat_transition']:
  p=DEST/'Layouts'/(name+'.json');p.write_text(json.dumps(layout(name),indent=2)+'\n');meta(p)
 p=DEST/'Props/home_ledger.json';p.write_text(json.dumps(home_display(),indent=2)+'\n');meta(p)
 print(json.dumps(inv['totals']))
if __name__=='__main__':main()
