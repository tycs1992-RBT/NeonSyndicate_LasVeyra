#!/usr/bin/env python3
"""Original safe-plot compositions consumed identically by Unity and offline review."""
from pathlib import Path
import json, math, hashlib
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/NeonSyndicate/Resources/VisualRebirth04A/Gardens'

def part(model,role,p,s=(1,1,1),yaw=0,far=True,solid=False):
 return dict(model=model,role=role,position=list(p),scale=list(s),euler=[0,yaw,0],far=far,solid=solid)

def build(kind):
 nodes=[]
 def add(*args,**kwargs): nodes.append(part(*args,**kwargs))
 # A 9m-wide cross and four entries remain open. All larger masses stay in the side quadrants.
 for x in [-16,16]:
  for z in [-16,16]:
   add('ward_pylon','trim',(x,0,z),(.55,.55,.55),45,solid=True)
   add('lantern','metal',(x,2.5,z),(.5,.5,.5),far=False)
 for x in [-11,11]:
  for z in [-7.5,7.5]: add('bench','stone',(x,0,z),(.75,.75,.75),90 if x>0 else 270,far=False,solid=True)
 for x,z in [(-13,-12),(13,12),(-13,12),(13,-12)]:
  add('planter','trim',(x,0,z),(.75,.75,.75),solid=True)
  for i in range(5):
   a=i*math.tau/5
   add('flower','flower',(x+math.cos(a)*.9,.32,z+math.sin(a)*.9),(.42,.55,.42),i*71,far=False)
 if kind=='sanctuary':
  # Broad shallow soil beds, layered edge planting and seating make a coherent garden.
  # Every bed stays outside the 9m cross; its matching low mesh supplies the 3cm lip.
  for sx in [-1,1]:
   for sz in [-1,1]:
    add('terrace','soil',(sx*14,.005,sz*14),(2.1,.04,2.1),solid=True)
    add('willow_trunk','bark',(sx*18,0,sz*18),(.42,.5,.42),0,solid=True)
    add('willow_canopy','leaf',(sx*18,0,sz*18),(.42,.5,.42))
    add('railing','metal',(sx*14,0,sz*21.8),(2.1,.75,.75),0,solid=True)
    add('railing','metal',(sx*21.8,0,sz*14),(2.1,.75,.75),90,solid=True)
    add('planter','trim',(sx*18,0,sz*10),(1,.6,1),solid=True)
    add('fern','leaf',(sx*18,.58,sz*10),(1.05,1.25,1.05),45,far=False)
    add('bench','bark',(sx*10,0,sz*19),(1,1,1),0 if sz<0 else 180,far=False,solid=True)
    for i,(x,z) in enumerate([(7,10),(7,15),(10,7),(15,7)]):
     add('fern','leaf',(sx*x,.03,sz*z),(1.1,1.15,1.1),i*47,far=False)
     add('flower','flower',(sx*(x+.6),.04,sz*(z+.45)),(.8,.9,.8),i*71,far=False)
  add('living_trunk','bark',(-12,0,11),(.65,.8,.65),solid=True)
  add('living_canopy','leaf',(-12,0,11),(.65,.8,.65))
  add('root_arch','bark',(12,0,-10),(.8,.8,.8),90,solid=True)
  add('fountain','stone',(12,0,11),(.65,.65,.65),solid=True)
  add('sky_ring','metal',(12,4.4,11),(.6,.6,.6),90)
  add('rock','stone',(-12,0,-10),(2,.7,1.4),25,solid=True)
  for i in range(12):
   x=-18+(i%4)*2;z=11+(i//4)*2
   add('fern','leaf',(x,.04,z),(.7,.8,.7),i*37,far=False)
  for i in range(9):
   a=i*.7
   add('mushroom','flower',(-12+math.cos(a)*3,.03,11+math.sin(a)*3),(.6,.65,.6),i*19,far=False)
 elif kind=='workshop':
  for x in [-12,12]:
   add('market_awning','cloth',(x,0,-10),(1.25,1.15,1.1),0,solid=False)
   add('arcade','stone',(x,0,10),(1.3,1.4,1.2),90,solid=True)
   for dx in [-3.48,3.48]:
    for dz in [-3.77,3.77]: add('column','stone',(x+dx,0,10+dz),(.42,1.4,.42),solid=True)
   add('rib_vault','trim',(x,8.4,10),(1.3,.5,1.2),90)
   add('banner','cloth',(x,3,-13),(.85,.85,.85),far=False)
   add('monolith','stone',(x,0,10),(.5,.5,.5),solid=True)
   add('ore','crystal',(x,1.3,10),(.55,.7,.55))
  for x in [-15,15]:
   for z in [-12,-9,-6]: add('threshold','bark',(x,0,z),(.5,.7,.5),far=False,solid=True)
 elif kind=='resource':
  add('drill','metal',(12,0,10),(.7,.8,.7),solid=True)
  add('ruin','stone',(-13,0,10),(1.2,1.4,1.2),45,solid=True)
  add('portal','trim',(-12,0,-10),(.65,.65,.65),0,solid=True)
  add('sigil','metal',(-12,5,-10),(.8,.8,.8))
  for i in range(8):
   a=i*math.tau/8
   add('rock','roof',(12+math.cos(a)*3,0,-10+math.sin(a)*3),(1.1,.65,1),i*47,solid=True)
   add('ore','crystal',(12+math.cos(a)*2,.2,-10+math.sin(a)*2),(.45,.65+(i%3)*.15,.45),i*47)
 else: raise ValueError(kind)
 return {'version':'04A','id':kind,'title':{'sanctuary':'Hearthspire Rootwater Sanctuary','workshop':'Hearthspire Lantern Court','resource':'Fracture Survey Garden'}[kind],'nodes':nodes,'drake':[-12,1.1,-10] if kind=='sanctuary' else [],'notes':'Decorative regional site. No capture, combat, resource reward, quest or save state is added. Center cross path has 9m clear width.'}

def main():
 DEST.mkdir(parents=True,exist_ok=True)
 for kind in ['sanctuary','workshop','resource']:
  path=DEST/(kind+'.json');path.write_text(json.dumps(build(kind),indent=2)+'\n')
  meta=Path(str(path)+'.meta')
  if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+hashlib.md5(('04A/'+str(path.relative_to(ROOT))).encode()).hexdigest()+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: Original 04A safe plot composition\n  assetBundleName:\n  assetBundleVariant:\n')
 print('04A: 3 shared garden compositions written')
if __name__=='__main__':main()
