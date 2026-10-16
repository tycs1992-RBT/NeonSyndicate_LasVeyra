#!/usr/bin/env python3
"""Original 04C furniture and human-scale room sources. Metres, Y-up, no external art.

Structural shell expansion is authored once in InhabitedCity04CComposition.cs;
this generator authors its shared furnishing/anchor JSON and editable OBJ/CMH1.
"""
from pathlib import Path
import argparse, hashlib, json, math, re, struct, uuid
import numpy as np
from PIL import Image
from generate_visual_rebirth_04a import Mesh, polar, TAU
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/NeonSyndicate/Resources/InhabitedCity04C'
MODELS=['door_frame','timber_bench','chair_frame','bed_frame','mattress_fold','bookcase','service_counter','medical_cot','tool_rack','ventilation_hood','pottery_set','record_stack']

def meta(path,kind='text'):
 p=Path(str(path)+'.meta')
 if p.exists():return
 guid=uuid.uuid5(uuid.NAMESPACE_URL,'crystal-manor-04c/'+path.relative_to(ROOT).as_posix()).hex
 if kind=='obj':
  text=(ROOT/'Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj.meta').read_text()
  text=re.sub(r'guid: [a-f0-9]+','guid: '+guid,text)
  text=re.sub(r'userData:.*','userData: Original 04C furniture. Metres Y-up.',text)
 elif kind=='texture':
  text=(ROOT/'Assets/NeonSyndicate/Resources/VisualRebirth04A/Textures/bark_Albedo.png.meta').read_text()
  text=re.sub(r'guid: [a-f0-9]+','guid: '+guid,text)
 else:
  text='fileFormatVersion: 2\nguid: '+guid+'\n'
  text+='folderAsset: yes\nDefaultImporter:\n' if kind=='folder' else 'MonoImporter:\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n' if kind=='cs' else 'TextScriptImporter:\n'
  text+='  externalObjects: {}\n  userData: Original Inhabited City 04C source\n  assetBundleName:\n  assetBundleVariant:\n'
 p.write_text('\n'.join(x.rstrip() for x in text.splitlines())+'\n')

def model(name,low):
 m=Mesh();s=6 if low else 10;n=10 if low else 24
 if name=='door_frame':
  # A human-scale, open 2.8m x 3.2m threshold, with recessed jambs and carved capitals.
  for x in [-1.57,1.57]:
   m.box((x,.12,0),(.34,.24,.58),.025)
   m.box((x,1.7,0),(.24,2.95,.4),.025)
   m.box((x,3.2,0),(.38,.2,.6),.025)
   if not low:
    for z in [-.16,.16]:m.rod((x-.09,.3,z),(x-.09,3.1,z),.025,sides=6)
  m.box((0,3.35,0),(3.5,.22,.58),.04)
  m.sweep([(-1.65,3.4,0),(0,3.72,0),(1.65,3.4,0)],[.08,.12,.08],s)
 elif name=='timber_bench':
  for x in [-1.05,1.05]:
   for z in [-.28,.28]:m.lathe([(0,0),(.08,0),(.1,.08),(.065,.15),(.065,.42),(.1,.48),(0,.48)],s,center=(x,0,z))
   m.box((x,.3,0),(.12,.12,.64),.015)
   m.sweep([(x,.4,.32),(x,.8,.4),(x,1.12,.46)],[.08,.065,.055],s)
  for z in [-.25,0,.25]:m.box((0,.5,z),(2.5,.12,.23),.022)
  for y in [.76,.98]:m.box((0,y,.42),(2.45,.14,.13),.02)
 elif name=='chair_frame':
  for x in [-.32,.32]:
   for z in [-.32,.32]:m.sweep([(x*1.18,0,z*1.18),(x,.45,z)],[.055,.07],s)
   m.sweep([(x,.4,.33),(x,.85,.37),(x,1.14,.46)],[.07,.06,.045],s)
  m.box((0,.46,0),(.84,.12,.84),.025)
  m.box((0,.91,.39),(.75,.18,.1),.025)
  m.box((0,1.14,.46),(.8,.09,.12),.025)
 elif name=='bed_frame':
  for x in [-1,1]:
   for z in [-1.35,1.35]:
    m.lathe([(0,0),(.095,0),(.095,.1),(.07,.14),(.07,.56),(.11,.62),(0,.62)],s,center=(x,0,z))
  m.box((0,.42,0),(2.2,.22,2.95),.05)
  m.box((0,.87,1.38),(2.2,1.05,.18),.06)
  m.box((0,.61,-1.38),(2.2,.48,.15),.04)
  if not low:
   for x in [-.8,-.4,0,.4,.8]:m.box((x,1.0,1.27),(.07,.62,.05),.012)
 elif name=='mattress_fold':
  m.box((0,.64,0),(2.02,.31,2.68),.11)
  m.box((0,.86,.88),(1.5,.18,.6),.08)
  # A draped folded quilt has visible rounded ridges and a hanging end.
  for z in np.linspace(-1.1,.3,4 if low else 8):m.box((0,.82,z),(2.05,.09,.2 if low else .15),.04)
  m.box((0,.58,-1.35),(2.02,.5,.08),.035)
 elif name=='bookcase':
  for x in [-1.15,1.15]:
   m.box((x,1.18,0),(.18,2.36,.62),.03)
   m.box((x,.08,0),(.28,.16,.75),.025)
  m.box((0,1.2,.27),(2.25,2.35,.08),.015)
  for y in [.16,.74,1.32,1.9,2.35]:m.box((0,y,0),(2.5,.12,.69),.025)
  m.box((0,2.47,0),(2.65,.18,.77),.03)
 elif name=='service_counter':
  # Panelled counter with supported thick top and a recessed foot rail.
  for x in [-1.35,1.35]:m.box((x,.53,0),(.24,1.06,1.05),.035)
  m.box((0,.48,.35),(2.7,.88,.15),.035)
  m.box((0,1.1,0),(3.1,.18,1.25),.05)
  m.box((0,.15,-.49),(2.65,.12,.12),.025)
  for x in [-.9,0,.9]:m.box((x,.57,.24),(.64,.55,.04),.018)
  if not low:
   for x in [-.9,0,.9]:m.box((x,.57,.21),(.48,.38,.035),.01)
 elif name=='medical_cot':
  for x in [-.9,.9]:
   for z in [-1.25,1.25]:
    m.rod((x,0,z),(x,.65,z),.055,sides=s)
    m.lathe([(0,.02),(.11,.02),(.11,.08),(0,.08)],s,center=(x,0,z))
  m.box((0,.5,0),(2.0,.15,2.85),.035)
  for x in [-.95,.95]:m.sweep([(x,.5,1.3),(x,1.05,1.3),(x*.7,1.22,1.3)],[.04,.04,.04],s)
  m.rod((-.75,1.2,1.3),(.75,1.2,1.3),.04,sides=s)
  m.box((0,.15,0),(1.6,.1,1.9),.025)
 elif name=='tool_rack':
  for x in [-.9,.9]:m.box((x,1.1,0),(.12,2.2,.18),.02)
  for y in [.12,1.25,2.12]:m.box((0,y,0),(2.1,.13,.18),.02)
  for i,x in enumerate([-.65,0,.65]):
   m.rod((x,.85,-.16),(x,1.9,-.16),.035,sides=s)
   if i==1:
    m.ring(.18,.035,(x,1.8,-.16),n,s,tilt=math.pi*.5,arc=math.pi*1.5,start=math.pi*.25)
   else:m.box((x,1.87,-.16),(.44,.14,.14),.025)
  m.box((0,.2,0),(2.4,.16,.75),.035)
 elif name=='ventilation_hood':
  m.box((0,.1,0),(2.8,.2,2.2),.055)
  for x in [-1.15,1.15]:m.box((x,.7,.2),(.35,1.2,1.6),.04)
  m.box((0,1.3,.2),(2.8,.2,1.9),.04)
  # Broad tapered smoke catcher, open underneath, narrowing to a real supported flue.
  for sign in [-1,1]:
   m.quad((sign*1.5,2.1,-1),(sign*1.5,2.1,1),(sign*.48,3.5,.48),(sign*.48,3.5,-.48),(sign,0,0))
   m.quad((-1.5,2.1,sign*1),(-.48,3.5,sign*.48),(.48,3.5,sign*.48),(1.5,2.1,sign*1),(0,0,sign))
  for x in [-1.25,1.25]:m.rod((x,1.35,.7),(x,2.2,.7),.055,sides=s)
  m.box((0,3.95,0),(.95,.9,.95),.05)
  m.box((0,4.43,0),(1.2,.12,1.2),.035)
 elif name=='pottery_set':
  for x,z,h,r in [(-.35,0,.52,.19),(.22,.1,.36,.15),(.45,-.3,.24,.12)]:
   m.lathe([(0,0),(r*.65,0),(r,.12*h),(r*1.05,.5*h),(r*.7,.83*h),(r*.55,h),(r*.4,h),(r*.5,.75*h),(0,.15*h)],n,center=(x,0,z))
  m.lathe([(0,0),(.27,0),(.35,.04),(.33,.07),(0,.07)],n,center=(0,0,-.4))
 elif name=='record_stack':
  for i in range(3):
   w=.65-i*.035;z=i*.025
   m.box((0,.055+i*.13,z),(w,.08,.47),.013)
   m.box((0,.108+i*.13,z),(w+.035,.026,.5),.009)
   m.box((0,.005+i*.13,z),(w+.035,.025,.5),.009)
   m.box((-w*.5,.06+i*.13,z),(.035,.12,.5),.012)
 elif name=='survey_scope':
  for a in [0,TAU/3,2*TAU/3]:m.sweep([polar(a,.7,0),polar(a,.4,1),polar(a,.2,1.5)],[.065,.07,.08],s)
  m.lathe([(0,1.2),(.35,1.2),(.35,1.32),(.2,1.4),(.2,1.7),(0,1.7)],n)
  m.sweep([(0,1.65,-.8),(0,1.9,.3),(0,2.1,1.15)],[.14,.2,.33],n)
  for z,y,r in [(-.7,1.67,.19),(.3,1.9,.25),(1.05,2.08,.36)]:m.ring(r,.04,(0,y,z),n,s,tilt=math.pi*.5)
 else:raise ValueError(name)
 return m

def export(name,low):
 data=np.asarray(model(name,low).vertices,dtype='<f4');variant=name+('_low' if low else '_high')
 assert np.isfinite(data).all() and len(data)>0 and len(data)%3==0
 tri=data[:,:3].reshape(-1,3,3);area=np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)
 assert area.min()>1e-9,(name,'degenerate')
 assert np.max(abs(np.linalg.norm(data[:,3:6],axis=1)-1))<1e-5
 payload=b'CMH1'+struct.pack('<i',len(data))+data.tobytes();binary=DEST/'Meshes'/(variant+'.bytes');binary.write_bytes(payload);meta(binary)
 obj=DEST/'Models'/(variant+'.obj')
 with obj.open('w') as f:
  f.write('# Original Crystal Manor 04C. Editable metres Y-up.\no '+variant+'\n')
  for v in data:f.write('v %.7f %.7f %.7f\n'%tuple(v[:3]))
  for v in data:f.write('vt %.7f %.7f\n'%tuple(v[6:]))
  for v in data:f.write('vn %.7f %.7f %.7f\n'%tuple(v[3:6]))
  for i in range(0,len(data),3):f.write('f '+' '.join(f'{j}/{j}/{j}' for j in range(i+1,i+4))+'\n')
 meta(obj,'obj')
 return {'name':name,'lod':'low' if low else 'high','vertices':len(data),'triangles':len(data)//3,'bounds_min':data[:,:3].min(0).tolist(),'bounds_max':data[:,:3].max(0).tolist(),'sha256':hashlib.sha256(payload).hexdigest(),'binary':binary.relative_to(ROOT).as_posix(),'source':obj.relative_to(ROOT).as_posix()}

def node(id,model,role,position,scale=(1,1,1),yaw=0,source='04C',solid=True,far=True,progress=0):
 return {'id':id,'kind':'mesh','source':source,'model':model,'role':role,'position':list(position),'scale':list(scale),'euler':[0,yaw,0],'solid':solid,'far':far,'interior':True,'progress':progress}

def box(id,role,p,s,solid=True):
 return {'id':id,'kind':'box','role':role,'position':list(p),'scale':list(s),'euler':[0,0,0],'solid':solid,'far':True,'interior':True,'progress':0}

def anchor(id,p,yaw=0):return {'id':id,'position':list(p),'yaw':yaw}
def keepout(id,lo,hi):return {'id':id,'min':list(lo),'max':list(hi)}

def layout(kind):
 nodes=[];A=[];keep=[]
 def add(*a,**k):nodes.append(node(*a,**k))
 def B(*a,**k):nodes.append(box(*a,**k))
 for name,p,y in [('staff',(5.3,0,10),180),('staff_next',(5.3,0,12),180),('visitor1',(-4.7,0,3.8),90),('visitor1_next',(-4.7,0,5.2),90),('visitor2',(4.8,0,2.6),270),('visitor2_next',(6.2,0,2.6),270)]:A.append(anchor(name,p,y))
 A.extend([anchor('exit',(0,0,-2),180),anchor('service',(-4.5,0,8.2) if kind=='forge' else (0,0,10.5) if kind=='garage' else (3.6,0,6.1),0),
  anchor('activity',(2.8,0,14.5),90),anchor('record1',(-2.8,0,13.7),180),anchor('record2',(0,0,15.5),180),anchor('record3',(2.8,0,13.7),180),
  anchor('defense1',(-2.8,0,9),180),anchor('defense2',(2.8,0,11),180),anchor('retrieve',(0,0,15.2),180),
  anchor('instrument1',(-1.5,4.2 if kind in ['lofts','observatory'] else 0,3.5),180),anchor('instrument2',(-1.5 if kind=='lofts' else 2.1,4.2 if kind in ['lofts','observatory'] else 0,5.3),90),anchor('instrument3',(3.8,4.2 if kind in ['lofts','observatory'] else 0,3.1),180)])
 keep.extend([keepout('public_threshold',(-1.25,0,-2),(1.25,2.2,.45)),keepout('principal_corridor',(-1.6,0,.45),(1.6,2.2,17)),keepout('staff_route',(4.6,0,9.3),(6,2.1,12.7)),
  keepout('visitor1_route',(-5.4,0,3.1),(-4,2.1,5.85)),keepout('visitor2_route',(4.1,0,1.9),(6.9,2.1,3.3))])
 if kind!='garage':add('carved_public_threshold','door_frame','trim',(0,0,-.08),solid=False)
 for x in [-7.45,7.45]:
  # Existing lantern pivots are base-mounted. A short shelf visibly supports each.
  add('sconce_'+str(x),'lantern','metal',(x,2.1,5),(.33,.33,.33),90 if x<0 else 270,source='04A',solid=False)
  B('sconce_shelf_'+str(x),'trim',(x,2.07,5),(.55,.1,.4),False)
 # Ground-level reading and work lamps are part of the visible fixture budget.
 lights=[{'id':'entry_practical','position':[0,3.25,3.8],'color':[1,.75,.47],'range':9,'intensity':1.25},
         {'id':'work_practical','position':[0,3.5,12.8],'color':[1,.82,.61],'range':10,'intensity':1.2}]
 for j,z in enumerate([3.8,12.8]):
  B('ceiling_light_hanger_'+str(j),'metal',(0,3.85 if kind in ['lofts','observatory'] else 4.3,z),(.05,.5 if kind in ['lofts','observatory'] else 1.4,.05),False)
  B('ceiling_light_shade_'+str(j),'metal',(0,3.5,z),(.6,.12,.6),False)
  B('ceiling_light_lens_'+str(j),'lamp',(0,3.38,z),(.42,.14,.42),False)
 labels=[{'id':'entrance_name','text':{'lofts':'VEYRA LOFTS','clinic':'MARA SOLANE\nSTREET CLINIC','forge':"DANTE ROOK'S\nCRYSTAL FORGE"}.get(kind,kind.upper()),'position':[0,4.3,-.24],'yaw':0,'characterSize':.105}]
 B('entrance_name_board','roof',(0,4.3,-.12),(6.5,1.15,.16),False)
 for x in [-3.3,3.3]:B('name_board_edge_'+str(x),'metal',(x,4.3,-.14),(.1,1.2,.2),False)
 if kind=='lofts':
  # A real supported window balcony retains a matching flight landing surface and guard.
  B('home_balcony_deck','trim',(-4.2,4.09,-.46),(4.3,.22,.95))
  B('home_balcony_guard','metal',(-4.2,5.14,-.94),(4.3,.08,.08))
  for i in range(5):B('home_balcony_post_'+str(i),'metal',(-6.2+i,4.67,-.94),(.07,.94,.07))
  for x in [-5.75,-2.65]:
   B('home_balcony_support_'+str(x),'trim',(x,3.6,-.18),(.22,.85,.35))
  add('lobby_ledger','service_counter','timber',(4.4,0,8))
  add('ledger_book','record_stack','linen',(4.2,1.2,8),solid=False)
  add('lobby_bench','timber_bench','timber',(-6.8,0,3.5),yaw=90)
  add('entry_vase','pottery_set','trim',(-2.9,0,2.0),(.9,.9,.9),solid=False)
  add('lobby_reference_shelf','bookcase','timber',(6.5,0,15.7),yaw=270)
  for y in [.82,1.4,1.98]:add('lobby_books_'+str(y),'record_stack','cloth',(6.25,y,15.65),(.75,.75,.75),yaw=90,solid=False)
  add('private_bed','bed_frame','timber',(4.6,4.2,13.4),yaw=180)
  add('private_bedding','mattress_fold','linen',(4.6,4.2,13.4),yaw=180,solid=False)
  add('private_reading_chair','chair_frame','timber',(2.8,4.2,5.8),yaw=180)
  add('private_shelf','bookcase','timber',(6.4,4.2,8.2),yaw=270)
  add('private_bench','timber_bench','timber',(-2,4.2,2.1),yaw=180)
  add('private_worktable','service_counter','timber',(0,4.2,10.5),(.72,.85,.8),yaw=180)
  add('private_papers','record_stack','linen',(0,5.22,10.5),solid=False)
  A.extend([anchor('bed',(2.0,4.2,13.3),90),anchor('desk',(0,4.2,8.7),0)])
  add('receiver_worktable','service_counter','timber',(3.5,0,15.9),(.72,.85,.8))
  add('damaged_receiver_case','tool_rack','metal',(3.5,.99,16.0),(.26,.26,.26),solid=False)
  # Empty sockets stay empty until the existing 04B ownership count earns each crystal.
  B('earned_display_shelf','timber',(3.7,2.32,16.8),(5,.12,.7),False)
  for i in range(9):
   x=1.7+i*.5
   B('earned_socket_'+str(i),'metal',(x,2.42,16.8),(.3,.08,.3),False)
   add('earned_crystal_'+str(i+1),'crystal_cluster','crystal',(x,2.46,16.8),(.09,.18,.09),source='04A',solid=False,progress=i+1)
  add('quiet_signal_receiver','record_stack','metal',(3.7,2.46,16.8),(1.5,1.4,1.5),solid=False,progress=10)
  lights.append({'id':'apartment_reading','position':[0,7.3,9],'color':[1,.8,.52],'range':10,'intensity':1.2})
  B('apartment_fixture','lamp',(0,8.1,9),(.7,.16,.7),False)
 elif kind=='clinic':
  B('clinic_canvas_awning','linen',(-4.2,3.48,-.52),(4.5,.12,1.65),False)
  for x in [-6.2,-2.2]:B('clinic_awning_support_'+str(x),'metal',(x,3.1,-.12),(.1,.7,.38),False)
  for x in [-6.8,6.8]:
   add('clinic_entry_vessel_'+str(x),'pottery_set','trim',(x,0,-.55),(1.25,1.25,1.25))
   add('clinic_entry_plant_'+str(x),'flower','leaf',(x-.35,.45,-.55),(1.3,1.3,1.3),source='04A',solid=False)
  add('reception_counter','service_counter','timber',(4.4,0,8))
  add('reception_register','record_stack','linen',(4.2,1.2,8),solid=False)
  add('waiting_bench','timber_bench','timber',(-6.8,0,3.6),yaw=90)
  add('treatment_cot','medical_cot','metal',(-4.7,0,13),yaw=0)
  add('treatment_linen','mattress_fold','linen',(-4.7,-.08,13),(.85,.92,.9),solid=False)
  add('supply_shelves','bookcase','timber',(6.7,0,15.8),yaw=270)
  for j,y in enumerate([.8,1.4,1.95]):add('supply_vessels_'+str(j),'pottery_set','trim',(6.45,y,15.7),(.7,.7,.7),yaw=90,solid=False)
  # Segmented cloth screen leaves a 2.8m treatment entry, never the central aisle.
  for z in [11.7,16.0]:
   B('screen_foot_'+str(z),'metal',(-2.7,.08,z),(.8,.16,1.3))
   B('screen_rail_'+str(z),'metal',(-2.7,2.2,z),(.06,.08,1.8))
   for dz in [-.85,.85]:B('screen_post_'+str(z)+str(dz),'metal',(-2.7,1.1,z+dz),(.06,2.2,.06))
   B('screen_cloth_'+str(z),'linen',(-2.7,1.25,z),(.04,1.7,1.7))
  add('medical_wash_vessels','pottery_set','trim',(-6.8,0,9),scale=(1.4,1.4,1.4))
  add('supply_ledger','record_stack','cloth',(3.3,1.2,15.9),solid=False)
  add('supply_table','service_counter','timber',(3.3,0,15.9),(.7,1,.75))
  next(a for a in A if a['id']=='record1')['position']=[4.8,0,14.3]
 elif kind=='forge':
  B('forge_overhead_duct','metal',(-5.4,4.65,7.5),(.6,.45,15),False)
  B('forge_external_vent','metal',(-5.4,4.65,-.25),(1.1,.65,.6),False)
  for i in range(4):B('forge_vent_louver_'+str(i),'roof',(-5.4,4.43+i*.14,-.57),(1.0,.055,.08),False)
  # The genuine ForgeStations.Anvil occupies the reserved service anchor at runtime.
  add('ventilated_hearth','ventilation_hood','metal',(-5.4,0,15.0))
  B('hearth_coals','lamp',(-5.4,.3,15),(1.5,.12,1.0),False)
  add('material_rack','bookcase','timber',(6.5,0,15.9),yaw=270)
  for j,y in enumerate([.82,1.4,1.98]):add('materials_'+str(j),'crystal_cluster','stone',(6.3,y,15.9),(.14,.1,.14),source='04A',solid=False)
  add('repair_bench','service_counter','timber',(3.6,0,16),(.95,1.0,.9))
  add('repair_tools','tool_rack','metal',(-6.5,0,6.2),yaw=90)
  add('bench_tools','pottery_set','metal',(4,1.2,16),(.8,.8,.8),solid=False)
  add('workshop_chair','chair_frame','timber',(-6.5,0,3.4),yaw=90)
  add('recipe_ledger','record_stack','linen',(2.8,1.2,15.8),solid=False)
 else:raise ValueError('Later stage layout not authored yet: '+kind)
 if kind in ['lofts','observatory']:keep.append(keepout('stairs_and_headroom',(-7,0,6),(-4.4,6.4,13.68)))
 return {'version':'04C','id':kind,'title':{'lofts':'Veyra Lofts','clinic':'Mara Solane Street Clinic','forge':"Dante Rook's Crystal Forge"}[kind],
  'style':1 if kind=='clinic' else 3 if kind=='forge' else 0,'twoStoreys':kind in ['lofts','observatory'],
  'notes':'Exact structural nodes are expanded by the shipped C# composition. Open public threshold, true room shell, retained original upper footprint and roof height. Existing canonical service executes through its shared adapter.',
  'nodes':nodes,'anchors':A,'keepouts':keep,'lights':lights,'labels':labels}

def texture():
 n=256;y,x=np.mgrid[:n,:n];grain=np.sin(x*.22+np.sin(y*.025)*2)+.35*np.sin(x*1.11+y*.015)+.22*np.cos(x*2.3+y*.12)
 plank=((x//64)%2)*.08;knots=np.zeros_like(grain)
 for cx,cy in [(40,70),(167,181)]:
  rr=np.sqrt(((x-cx)/1.6)**2+((y-cy)*.35)**2);knots+=np.sin(rr*.55)*np.exp(-rr/35)*.5
 value=np.clip(.52+.075*grain+.04*knots+plank,0,1)
 rgb=np.stack([value*.9,value*.68,value*.43],axis=-1);path=DEST/'Textures/Timber_Albedo.png'
 Image.fromarray((rgb*255).astype('uint8'),'RGB').save(path);meta(path,'texture')

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--stage',choices=['route','all'],default='route');args=parser.parse_args()
 for name in ['Models','Meshes','Layouts','Textures']:
  p=DEST/name;p.mkdir(parents=True,exist_ok=True);meta(p,'folder')
 meta(DEST,'folder')
 runtime=ROOT/'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C';meta(runtime,'folder')
 for p in runtime.glob('*.cs'):meta(p,'cs')
 report=[export(name,low) for name in MODELS for low in [False,True]]
 for kind in ['lofts','clinic','forge']:
  p=DEST/'Layouts'/(kind+'.json');p.write_text(json.dumps(layout(kind),indent=2)+'\n');meta(p)
 texture()
 catalog=DEST/'asset_manifest.json';catalog.write_text(json.dumps({'version':'04C','provenance':'Original project-owned procedural source geometry and timber albedo; no third-party assets. Metric Y-up. The unchanged 04A/04B libraries supply explicitly referenced modules and surface maps.','models':report},indent=2)+'\n');meta(catalog)
 print(json.dumps({'models':len(MODELS),'lods':len(report),'triangles':sum(x['triangles'] for x in report),'layouts':3,'stage':args.stage}))
if __name__=='__main__':main()
