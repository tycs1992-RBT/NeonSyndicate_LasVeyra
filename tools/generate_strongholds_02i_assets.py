#!/usr/bin/env python3
"""Original stronghold kit. OBJ authoring copies plus CMH1 data for reliable runtime loading.
Requires numpy and Pillow to REGENERATE, not to play. Does not modify older art kits.
"""
from pathlib import Path
import numpy as np, math, struct, json, hashlib, importlib.util, argparse
from PIL import Image
P=Path(__file__).resolve().parents[1];R=P/'Assets/NeonSyndicate/Resources/CrystalStrongholds02I'
spec=importlib.util.spec_from_file_location('base_art',P/'tools/generate_crystal_city_02f_assets.py');A=importlib.util.module_from_spec(spec);spec.loader.exec_module(A)
Mesh=A.Mesh;TAU=math.tau

def meta(path,kind='text'):
 guid=hashlib.md5(('CrystalManor02I:'+path.relative_to(P).as_posix()).encode()).hexdigest()
 if kind=='obj':
  import re
  template=(P/'Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj.meta').read_text()
  text=re.sub('guid: [a-f0-9]+','guid: '+guid,template)
 elif kind in ('albedo','normal','linear'):
  # Match the already shipped 02F importer settings. The runtime also has binary geometry.
  import re
  src=P/'Assets/NeonSyndicate/Resources/CrystalCity02F/Textures'/('moonstone_'+('Albedo' if kind=='albedo' else ('Normal' if kind=='normal' else 'Mask'))+'.png.meta')
  text=re.sub('guid: [a-f0-9]+','guid: '+guid,src.read_text())
 else:
  text='fileFormatVersion: 2\nguid: '+guid+'\n'
  if kind=='folder':text+='folderAsset: yes\nDefaultImporter:\n'
  else:text+='TextScriptImporter:\n'
  text+='  externalObjects: {}\n  userData: Original 02I stronghold kit.\n  assetBundleName:\n  assetBundleVariant:\n'
 Path(str(path)+'.meta').write_text(text)

def box(m,c,size):
 c=np.array(c);h=np.array(size)/2
 for ax in range(3):
  other=[a for a in range(3) if a!=ax]
  for sign in (-1,1):
   n=np.zeros(3);n[ax]=sign
   corners=[]
   for a,b in [(-1,-1),(-1,1),(1,1),(1,-1)]:
    v=c.copy().astype(float);v[ax]+=sign*h[ax];v[other[0]]+=a*h[other[0]];v[other[1]]+=b*h[other[1]];corners.append(v)
   m.quad(*corners,n)

def model(name,low):
 m=Mesh();n=6 if low else 10
 if name=='foundation':
  box(m,(0,-.08,0),(32,.30,32));box(m,(0,-.25,0),(32.6,.12,32.6))
  for sign in [-1,1]:
   for v in range(-14,15,4):
    box(m,(v,-.20,sign*16.1),(3.9,.36,.24));box(m,(sign*16.1,-.20,v),(.24,.36,3.9))
 elif name=='gate':
  for s in [-1,1]:
   m.rod((s*3.7,0,0),(s*3.7,5.3,0),.56,.4,n)
   box(m,(s*3.7,.22,0),(1.55,.44,1.55));box(m,(s*3.7,5.3,0),(1.2,.3,1.2))
   pts=[(s*3.7*(1-t),5.3+3.2*math.sin(t*math.pi/2),0) for t in np.linspace(0,1,6 if low else 13)]
   m.curve(pts,.29,.21,n)
  box(m,(0,8.5,0),(.75,.3,.75))
 elif name=='gate_trim':
  for s in [-1,1]:
   pts=[(s*3.7*(1-t),5.3+3.2*math.sin(t*math.pi/2),-.3) for t in np.linspace(0,1,6 if low else 13)]
   m.curve(pts,.035,.035,4 if low else 6)
   m.rod((s*3.7,.4,-.49),(s*3.7,5,-.39),.035,.035,4)
  m.ring(.58,.035,16 if low else 32,4,center=(0,7.45,-.34),tilt=[[1,0,0],[0,0,-1],[0,1,0]])
 elif name=='tower':
  box(m,(0,.12,0),(4.7,.24,4.7));m.rod((0,.24,0),(0,5.65,0),1.3,1.08,n)
  for i in range(4):
   a=i*math.pi/2+math.pi/4;p=A.polar(a,1.58)
   m.rod(p,p+np.array([0,5.75,0]),.19,.14,n)
  box(m,(0,5.85,0),(4.8,.3,4.8))
  for s in [-1,1]:
   for v in [-1.8,0,1.8]:
    box(m,(v,6.4,s*2.2),(1.0,.8,.4));box(m,(s*2.2,6.4,v),(.4,.8,1.0))
  if not low:
   for y in [1.0,2.2,3.4,4.6]:m.ring(1.2,.045,20,4,center=(0,y,0))
 elif name=='wall':
  box(m,(0,1.5,0),(5.8,3,.7));box(m,(0,.15,0),(6.1,.3,1.25));box(m,(0,2.95,0),(6.05,.25,1.0))
  for x in [-2.3,-1.15,0,1.15,2.3]:box(m,(x,3.45,0),(.78,.8,.85))
  if not low:
   for x in [-2.8,2.8]:m.rod((x,.3,0),(x,3.2,0),.29,.19,8)
 elif name=='dais':
  for y,r,h in [(0,2.9,.22),(.22,2.6,.22),(.44,2.35,.15)]:m.rod((0,y,0),(0,y+h,0),r,r,12 if low else 24)
  for a in np.linspace(0,TAU,4,endpoint=False):
   p=A.polar(a,1.6,.59);m.rod(p,p+np.array([0,2.2,0]),.21,.14,n)
  box(m,(0,2.92,0),(5.0,.24,5.0))
 elif name=='trellis':
  for x in [-2,2]:
   for z in [-2,2]:m.rod((x,0,z),(x,3.1,z),.13,.09,n)
  for z in [-2,2]:
   pts=[(x,3.1+1.2*math.cos(x/2*math.pi/2),z) for x in np.linspace(-2,2,7 if low else 15)];m.curve(pts,.08,.08,n)
  for x in [-2,0,2]:m.rod((x,3.2,-2),(x,3.2,2),.05,.05,4)
 elif name=='podium':
  for y,r,h in [(0,1.4,.18),(.18,1.18,.36),(.54,.92,.35)]:m.rod((0,y,0),(0,y+h,0),r,r*.94,8 if low else 16)
  for a in np.linspace(0,TAU,4,endpoint=False):
   pts=[A.polar(a,1.05,.4),A.polar(a,1.5,1.9),A.polar(a,.7,3.4)]
   m.curve(pts,.10,.05,n)
 elif name=='heart':
  m.shard((0,0,0),.65,2.9,(.14,0,.12),6 if low else 10)
  for i in range(3 if low else 5):m.shard(A.polar(i*2.39996,.48,.05),.12,.7+(i%2)*.25,A.polar(i*2.39996,.2),5)
 elif name=='spire':
  m.rod((0,0,0),(0,2,0),.7,.45,n);m.shard((0,1.6,0),.6,5.9,(.4,0,.12),n)
  m.ring(.66,.04,12 if low else 24,4,center=(0,1.4,0))
 elif name=='banner':
  seg=4 if low else 14
  for i in range(seg):
   for j in range(2 if low else 5):
    def pt(a,b):return np.array([(a-.5)*1.9,-b*3.2,math.sin(a*6+b*3.1)*.11*b])
    a0=i/seg;a1=(i+1)/seg;b0=j/(2 if low else 5);b1=(j+1)/(2 if low else 5)
    p=[pt(a0,b0),pt(a1,b0),pt(a1,b1),pt(a0,b1)]
    m.quad(*p,[0,0,-1]);m.quad(p[0],p[3],p[2],p[1],[0,0,1])
 elif name=='inlay':
  m.ring(1,.015,24 if low else 48,4);m.ring(.7,.009,24 if low else 48,3)
  for i in range(6):
   a=i*math.pi/3;m.rod(A.polar(a,.3),A.polar(a,.9),.012,.012,3)
 elif name=='canopy':
  # Deliberately lower, spreading fronds for the conservatory rather than a spherical crown.
  for i in range(6 if low else 12):
   a=i*2.39996;p=A.polar(a,.12,2.0+(i%3)*.18);q=A.polar(a,1.8,2.8+(i%3)*.14)
   for j in range(3 if low else 6):
    t=(j+1)/(3 if low else 6);b=p*(1-t)+q*t
    m.leaf(b,b+A.polar(a+(-1 if j%2 else 1)*.65,.70,-.1),.20,.07)
 else:raise ValueError(name)
 return m

MODELS=['foundation','gate','gate_trim','tower','wall','dais','trellis','podium','heart','spire','banner','inlay','canopy']

def write_mesh(m,path):
 with path.open('w') as f:
  f.write('# Original Crystal Strongholds 02I model. Metres, Y-up.\no '+path.stem+'\n')
  for p in m.v:f.write('v %.7f %.7f %.7f\n'%tuple(p))
  for p in m.uv:f.write('vt %.7f %.7f\n'%tuple(p))
  for p in m.n:f.write('vn %.7f %.7f %.7f\n'%tuple(p))
  for face in m.faces:f.write('f '+' '.join(f'{i+1}/{i+1}/{i+1}' for i in face)+'\n')
 meta(path,'obj')
 data=np.hstack([np.asarray(m.v),np.asarray(m.n),np.asarray(m.uv)]).astype('<f4')
 binary=R/'Geometry'/(path.stem+'.bytes');binary.write_bytes(b'CMH1'+struct.pack('<i',len(data))+data.tobytes());meta(binary)
 return {'model':path.stem,'vertices':len(data),'triangles':len(m.faces),'min':data[:,:3].min(0).tolist(),'max':data[:,:3].max(0).tolist(),'sha256':hashlib.sha256(binary.read_bytes()).hexdigest()}

def textures():
 n=512;y,x=np.mgrid[:n,:n]/n;rng=np.random.default_rng(219901)
 row=np.floor(y*8);u=(x*8+(row%2)*.5)%1;v=(y*8)%1
 joints=np.minimum(np.minimum(u,1-u),np.minimum(v,1-v))
 mortar=np.clip(joints/.04,0,1);noise=rng.random((n,n))*.06
 c=(.25+.4*mortar+noise)[...,None]*np.array([.78,.81,.92]);h=mortar*.08+noise*.1
 dx=(np.roll(h,-1,1)-np.roll(h,1,1))*4;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*4
 normals=np.stack([-dx,-dy,np.ones_like(dx)],-1);normals/=np.linalg.norm(normals,axis=-1,keepdims=True)
 mask=np.stack([np.ones_like(x)*.08,np.clip(.5+mortar*.48,0,1),np.zeros_like(x),np.ones_like(x)*.38],-1)
 for suffix,img,kind in [('Albedo',c,'albedo'),('Normal',normals*.5+.5,'normal'),('Mask',mask,'linear')]:
  p=R/'Textures'/('fortstone_'+suffix+'.png');Image.fromarray((np.clip(img,0,1)*255).astype('uint8')).save(p);meta(p,kind)
 # Geometric woven heraldry, original motif rather than a borrowed insignia.
 weave=(.7+.15*np.sin(x*math.tau*160)*np.sin(y*math.tau*180));dx=x-.5;dy=y-.4
 diamond=np.abs(np.abs(dx)+np.abs(dy)*.8-.22)<.012
 stem=(np.abs(dx)<.014)&(y>.2)&(y<.75);wings=(np.abs(np.abs(dx)-(.38-y*.4))<.009)&(y>.23)&(y<.55)
 sigil=diamond|stem|wings;rgb=np.stack([weave*.13,weave*.24,weave*.45],-1);rgb[sigil]=[.91,.77,.4]
 p=R/'Textures/banner_Albedo.png';Image.fromarray((rgb*255).astype('uint8')).save(p);meta(p,'albedo')

if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--confirm-regenerate',action='store_true');args=ap.parse_args()
 if not args.confirm_regenerate:ap.error('Use --confirm-regenerate. Back up artist changes first.')
 for d in [R/'Models',R/'Geometry',R/'Textures']:d.mkdir(parents=True,exist_ok=True);meta(d,'folder')
 inventory=[]
 for name in MODELS:
  for low in [False,True]: inventory.append(write_mesh(model(name,low),R/'Models'/(name+('_lod' if low else '')+'.obj')))
 textures();p=R/'model_inventory.json';p.write_text(json.dumps({'version':'02I','models':inventory},indent=2)+'\n');meta(p)
 print('Wrote',len(inventory),'OBJ models with matching readable binary geometry.')
