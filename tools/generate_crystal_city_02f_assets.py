#!/usr/bin/env python3
"""Original offline mesh/texture authoring for the 02F visual sprint.
The outputs are real OBJ and PNG files consumed by Unity, not reference images.
"""
from pathlib import Path
import math, json, uuid, hashlib, argparse
import numpy as np
from PIL import Image, ImageFilter

P = Path(__file__).resolve().parents[1]
R = P/'Assets/NeonSyndicate/Resources/CrystalCity02F'
TAU = math.tau

def guid(path): return uuid.uuid5(uuid.NAMESPACE_URL, 'crystal-manor-02f/'+path).hex

def meta(path, kind='folder'):
    rel = str(path.relative_to(P)).replace('\\','/')
    if kind=='folder': text=f'fileFormatVersion: 2\nguid: {guid(rel)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: Original Crystal City 02F asset.\n  assetBundleName:\n  assetBundleVariant:\n'
    elif kind=='obj':
        template=(P/'Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj.meta').read_text()
        import re
        text=re.sub(r'guid: [a-f0-9]+', 'guid: '+guid(rel), template).replace('Original mesh;', 'Original 02F mesh;')
    elif kind in ('albedo','linear','normal'):
        text=f'''fileFormatVersion: 2
guid: {guid(rel)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {1 if kind=='albedo' else 0}
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: {1 if kind=='normal' else 0}
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  compressionQuality: 70
  textureCompression: 1
  textureType: {1 if kind=='normal' else 0}
  textureShape: 1
  alphaSource: 1
  alphaIsTransparency: 0
  platformSettings: []
  userData: Original deterministic 02F surface. No external image source.
  assetBundleName:
  assetBundleVariant:
'''
    else: text=f'fileFormatVersion: 2\nguid: {guid(rel)}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    path.with_name(path.name+'.meta').write_text(text,encoding='utf-8')

class Mesh:
    def __init__(self): self.v=[]; self.n=[]; self.uv=[]; self.faces=[]
    def tri(self,a,b,c,outward=None):
        a,b,c = map(lambda v:np.asarray(v,dtype=float),(a,b,c))
        n=np.cross(b-a,c-a); length=np.linalg.norm(n)
        if length<1e-9:return
        if outward is not None and np.dot(n,outward)<0: b,c=c,b;n=-n
        n=n/length
        index=len(self.v)
        axes=[i for i in range(3) if i!=int(np.argmax(abs(n)))]
        for p in (a,b,c): self.v.append(p);self.n.append(n);self.uv.append(p[axes])
        self.faces.append((index,index+1,index+2))
    def quad(self,a,b,c,d,out=None):self.tri(a,b,c,out);self.tri(a,c,d,out)
    def rod(self,a,b,ra,rb,sides=6,caps=True):
        a,b=np.array(a,float),np.array(b,float);axis=b-a
        if np.linalg.norm(axis)<1e-8:return
        axis/=np.linalg.norm(axis);ref=np.array([1,0,0] if abs(axis[1])>.9 else [0,1,0]);right=np.cross(axis,ref);right/=np.linalg.norm(right);forward=np.cross(axis,right)
        for i in range(sides):
            n0=right*math.cos(i*TAU/sides)+forward*math.sin(i*TAU/sides)
            n1=right*math.cos((i+1)*TAU/sides)+forward*math.sin((i+1)*TAU/sides)
            p,q,r,s=a+n0*ra,a+n1*ra,b+n1*rb,b+n0*rb
            self.quad(p,q,r,s,n0+n1)
            if caps:self.tri(a,q,p,-axis);self.tri(b,s,r,axis)
    def curve(self, points, r0=.1,r1=.06,sides=6):
        for i in range(len(points)-1):
            t=i/(len(points)-1);t1=(i+1)/(len(points)-1)
            self.rod(points[i],points[i+1],r0*(1-t)+r1*t,r0*(1-t1)+r1*t1,sides,i in (0,len(points)-2))
    def ring(self,r=1,tube=.07,segments=32,sides=6,center=(0,0,0),tilt=None,arc=TAU,start=0):
        center=np.array(center);rot=np.eye(3) if tilt is None else np.array(tilt)
        def point(a,b):return center+rot@np.array([(r+tube*math.cos(b))*math.cos(a),tube*math.sin(b),(r+tube*math.cos(b))*math.sin(a)])
        for i in range(segments):
            a=start+arc*i/segments;a1=start+arc*(i+1)/segments
            for j in range(sides):
                b=j*TAU/sides;b1=(j+1)*TAU/sides
                out=rot@np.array([math.cos((a+a1)/2)*math.cos((b+b1)/2),math.sin((b+b1)/2),math.sin((a+a1)/2)*math.cos((b+b1)/2)])
                self.quad(point(a,b),point(a1,b),point(a1,b1),point(a,b1),out)
    def shard(self,base=(0,0,0),r=.3,h=2,lean=(.25,0,.1),sides=6):
        base=np.array(base,float);lean=np.array(lean,float)
        centers=[base,base+lean*.15+np.array([0,h*.17,0]),base+lean*.78+np.array([0,h*.76,0]),base+lean+np.array([0,h,0])];radii=[.02*r,r,.81*r,0]
        for j in range(3):
            for i in range(sides):
                a=TAU*i/sides;b=TAU*(i+1)/sides
                u=np.array([math.cos(a),0,math.sin(a)]);v=np.array([math.cos(b),0,math.sin(b)])
                outward=(u+v)/2+np.array([0, -.4 if j==0 else (.4 if j==2 else 0),0])
                self.quad(centers[j]+u*radii[j],centers[j]+v*radii[j],centers[j+1]+v*radii[j+1],centers[j+1]+u*radii[j+1],outward)
    def leaf(self,a,b,width=.25,fold=.14):
        a,b=np.array(a,float),np.array(b,float);axis=b-a;axis/=np.linalg.norm(axis);ref=np.array([0,1,0] if abs(axis[1])<.9 else [1,0,0]);side=np.cross(axis,ref);side/=np.linalg.norm(side);up=np.cross(side,axis);mid=a*.55+b*.45
        l,r,crest=mid-side*width,mid+side*width,mid+up*fold
        for x,y,z in [(a,l,crest),(l,b,crest),(b,r,crest),(r,a,crest)]:self.tri(x,y,z,up);self.tri(x,z,y,-up)
    def write(self,path):
        with path.open('w') as f:
            f.write('# Original 02F mesh. Metres, Y-up, one mesh, no colliders or materials.\no '+path.stem+'\n')
            for p in self.v:f.write('v %.7f %.7f %.7f\n'%tuple(p))
            for p in self.uv:f.write('vt %.7f %.7f\n'%tuple(p))
            for p in self.n:f.write('vn %.7f %.7f %.7f\n'%tuple(p))
            for face in self.faces:f.write('f '+' '.join(f'{i+1}/{i+1}/{i+1}' for i in face)+'\n')
        meta(path,'obj')

def polar(a,r,y=0):return np.array([r*math.cos(a),y,r*math.sin(a)])

def build_model(name,low):
    m=Mesh();n=5 if low else 8
    if name=='island':
        count=8 if low else 16
        rings=[]
        for y,r in [(0,3.7),(-.4,4.0),(-1.1,3.3),(-2.3,1.9),(-3.7,.1)]:
            rings.append([polar(TAU*i/count,r*(1+.09*math.sin(i*2.8)),y+.08*math.sin(i*2)) for i in range(count)])
        for j in range(len(rings)-1):
            for i in range(count):m.quad(rings[j][i],rings[j][(i+1)%count],rings[j+1][(i+1)%count],rings[j+1][i],polar(TAU*(i+.5)/count,1,-.4))
        for i in range(count):m.tri([0,.02,0],rings[0][i],rings[0][(i+1)%count],[0,1,0])
    elif name=='terrace':
        for y,r,h in [(0,3.65,.25),(.25,3.35,.2),(.45,2.9,.22)]:m.rod([0,y,0],[0,y+h,0],r,r*.98,12 if low else 24,True)
        if not low:
            for i in range(24):
                p=polar(i*TAU/24,3.34,.29);q=polar(i*TAU/24,3.34,.45);m.rod(p,q,.035,.035,4)
    elif name=='pavilion':
        ribs=6;steps=7 if low else 15
        m.ring(3.05,.105,20 if low else 40,4 if low else 6)
        for i in range(ribs):
            a=i*TAU/ribs
            pts=[polar(a+.2*t,3.0*(1-.85*t),6.0*math.sin(t*math.pi*.5)) for t in np.linspace(0,1,steps)]
            m.curve(pts,.14,.065,5 if low else 8)
            if not low:
                m.curve([polar(a,2.8,1),polar(a+.22,2.2,2.5),polar(a+.3,1.3,4.1)],.055,.035,5)
        m.ring(.65,.08,16 if low else 32,4,center=(0,5.8,0))
    elif name=='astrolabe':
        m.ring(3.1,.11,28 if low else 64,4 if low else 7)
        m.ring(2.8,.052,24 if low else 56,4 if low else 6)
        for i in range(12 if low else 24):
            a=i*TAU/(12 if low else 24); p=polar(a,2.8);q=polar(a,3.45 if i%3==0 else 3.25);m.rod(p,q,.025,.025,4)
            if not low and i%3==0:m.shard(polar(a,3.32),.09,.35,lean=polar(a,.08),sides=5)
    elif name=='crescent':
        for side in [-1,1]:
            pts=[]
            for t in np.linspace(0,1,9 if low else 20):pts.append([side*(.15+2.2*math.sin(t*math.pi*.75)),4.5*t,.15*math.sin(t*math.pi)])
            m.curve(pts,.15,.02,5 if low else 8)
        m.ring(.85,.065,16 if low else 32,4,center=(0,1.5,0),tilt=[[1,0,0],[0,0,-1],[0,1,0]])
    elif name=='crystal_fan':
        count=5 if low else 9
        for i in range(count):
            a=i*2.39996;r=.65+.15*(i%2);h=2.3+.5*(i%3)
            m.shard(polar(a,r,.06),.28,h,lean=polar(a,.5),sides=n)
        m.shard([0,.05,0],.52,4.1,lean=(.35,0,-.2),sides=n)
    elif name=='trunk':
        pts=[[.25*math.sin(t*4),t*4.8,.18*math.sin(t*3)] for t in np.linspace(0,1,6 if low else 12)]
        m.curve(pts,.28,.06,n)
        for i in range(6):
            a=i*TAU/6
            m.curve([polar(a,1.1,.06),polar(a,.45,.22),[0,1,0]],.055,.2,5)
            b=np.array([.05,2.8+(i%2)*.55,0]);c=polar(a,.85,4.0);d=polar(a+.17,2.4,5.0+(i%2)*.5)
            m.curve([b,c,d],.12,.02,n)
            if not low:
                m.curve([c,polar(a-.4,1.55,4.8),polar(a-.55,2.3,5.2)],.055,.009,5)
    elif name=='canopy':
        count=25 if low else 78
        for i in range(count):
            a=i*2.39996;r=.5+(i%7)/7*2.3;y=4.1+(i%5)*.3
            p=polar(a,r,y);q=p+polar(a+(.35 if i%2 else -.3),.8,-.12)
            m.leaf(p,q,.29+(i%3)*.04,.11)
        for i in range(6):m.leaf(polar(i*TAU/6,.25,4.8),polar(i*TAU/6,1.4,5.9),.35,.16)
    elif name=='vine':
        for i in range(3 if low else 5):
            pts=[[.35*(i-2)+.1*math.sin(t*7+i),-3.2*t,.16*math.sin(t*4+i)] for t in np.linspace(0,1,5 if low else 9)]
            m.curve(pts,.022,.008,4)
            for j,p in enumerate(pts[1:]):m.leaf(p,np.array(p)+[.22 if j%2 else -.22,-.24,.12],.12,.04)
    elif name=='luminous_caps':
        for i in range(3 if low else 5):
            a=i*2.399;r=(i%3)*.37;base=polar(a,r,0);h=.5+(i%3)*.3
            m.rod(base,base+[.03,h,0],.045,.035,5)
            pts=10 if low else 20
            for j in range(pts):
                a0=j*TAU/pts;a1=(j+1)*TAU/pts;c=base+[.03,h+.12,0]
                p=base+polar(a0,.35,h);q=base+polar(a1,.35,h)
                m.tri(c,p,q,[0,1,0]);m.tri(base+[0,h-.08,0],q,p,[0,-1,0])
    elif name=='rune_disc':
        for r in [.4,.75,1.0]:m.ring(r,.018,20 if low else 48,3 if low else 4)
        for i in range(6):
            a=i*TAU/6; b=polar(a,.75); c=polar(a,.97);m.rod(b,c,.015,.015,3)
            d=polar(a+.12,.84);m.rod(b,d,.015,.015,3)
    elif name=='light_seed':m.shard(r=.13,h=.55,lean=(.035,0,0),sides=n)
    elif name=='banner':
        steps=4 if low else 12
        for i in range(steps):
            a=i/steps;b=(i+1)/steps
            def p(t,side):return [side*(.52-.18*t*t),-2.3*t,.08*math.sin(t*8)+.08*t]
            m.quad(p(a,-1),p(a,1),p(b,1),p(b,-1),[0,0,1]);m.quad(p(a,-1),p(b,-1),p(b,1),p(a,1),[0,0,-1])
    elif name=='water_arc':
        steps=10 if low else 24
        for i in range(steps):
            a=i/steps;b=(i+1)/steps
            def p(t,side):return [2.1*t,2.5*math.sin(t*math.pi*.8),side*.035*(1-.4*t)]
            m.quad(p(a,-1),p(a,1),p(b,1),p(b,-1),[0,1,0]);m.quad(p(a,-1),p(b,-1),p(b,1),p(a,1),[0,-1,0])
    else:raise ValueError(name)
    return m

MODELS=['island','terrace','pavilion','astrolabe','crescent','crystal_fan','trunk','canopy','vine','luminous_caps','rune_disc','light_seed','banner','water_arc']

def texture_set(name,seed):
    N=512;rng=np.random.default_rng(seed)
    y,x=np.mgrid[0:N,0:N].astype(float);u=x/N;v=y/N
    small=rng.normal(0,.012,(N,N)); broad=.5+ .2*np.sin(TAU*(2*u+np.sin(TAU*v)*.3))+.14*np.sin(TAU*(3*v+.4*u))
    if name=='moonstone':
        mortar=(np.minimum(np.mod(x+(np.floor(y/128)%2)*64,128),128-np.mod(x+(np.floor(y/128)%2)*64,128))<2.5)|(np.minimum(np.mod(y,128),128-np.mod(y,128))<2.5)
        color=np.stack([.42+small,.46+small,.53+small],-1)*(broad*.35+.82)[...,None]
        color[mortar]*=.5; height=broad*.12+small*.25-mortar*.13
        metal=np.zeros_like(u);smooth=.3+.14*broad;emission=np.zeros_like(u)
    elif name=='verdant':
        vein=np.exp(-np.abs(np.sin(TAU*(u*4+np.sin(v*TAU)*.22)))*25)
        moss=np.clip((broad-.4)*1.8,0,1)
        color=np.stack([.22+.1*broad,.32+.13*broad,.28+.09*broad],-1)+small[...,None]
        color*=1-.12*vein[...,None];height=broad*.2+vein*.06; metal=np.zeros_like(u);smooth=.26+.12*broad;emission=vein*.25*moss
    elif name=='gilt':
        cx=np.mod(u*8,1);cy=np.mod(v*8,1)
        glyph=((abs(cx-.5)<.027)&(cy>.16)&(cy<.84))|((abs(cx+cy-1)<.025)&(cy>.3)&(cy<.7))
        brush=.04*np.sin(u*TAU*90)+small
        color=np.stack([.61+brush,.43+brush*.7,.2+brush*.4],-1)*(broad*.17+.87)[...,None];color[glyph]*=.55
        height=broad*.035-glyph*.035;metal=np.ones_like(u)*.88;smooth=np.ones_like(u)*.57;emission=glyph.astype(float)*.8
    else:
        veins=np.exp(-abs(np.sin((u + .13*np.sin(v*TAU))*TAU*5))*22)
        color=np.stack([.24+.13*broad,.54+.16*broad,.46+.22*broad],-1)
        color*=1+veins[...,None]*.15;height=veins*.08+small*.2;metal=np.zeros_like(u);smooth=np.ones_like(u)*.38;emission=veins*.7
    color=np.clip(color,0,1);height=np.asarray(height,float)
    dx=(np.roll(height,-1,1)-np.roll(height,1,1))*4;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*4
    norms=np.stack([-dx,-dy,np.ones_like(dx)],-1);norms/=np.linalg.norm(norms,axis=-1,keepdims=True)
    mask=np.stack([metal,np.ones_like(u)*.92,np.zeros_like(u),smooth],-1)
    outputs=[('Albedo',color,'albedo'),('Normal',norms*.5+.5,'normal'),('Mask',mask,'linear'),('Emission',np.repeat(emission[...,None],3,-1),'albedo')]
    for suffix,a,kind in outputs:
        path=R/'Textures'/f'{name}_{suffix}.png';Image.fromarray((np.clip(a,0,1)*255).astype('uint8')).save(path);meta(path,kind)

def node(model,material='stone',p=(0,0,0),s=(1,1,1),r=(0,0,0),**kw):
    return dict(model=model,material=material,position=list(p),scale=list(s) if hasattr(s,'__len__') else [s]*3,euler=list(r),**kw)

def compositions():
    out=[]
    # Units are metres; hero assemblies occupy a 10m square. Planner scales down for smaller roofs.
    for theme,title in enumerate(['Aster Crown Observatory','Dawnspire Atrium','Moonroot Sky Garden']):
        parts=[node('island','stone',p=(0,0,0),s=1.0),node('terrace','stone',p=(0,.1,0),s=1)]
        if theme==0:
            parts += [node('pavilion','gilt',p=(0,.7,0),s=1),node('crystal_fan','crystal',p=(0,1.1,0),s=(1.1,1.65,1.1))]
            parts += [node('astrolabe','gilt',p=(0,5.0,0),s=1.25,r=(60,0,20),spin=[0,1,0],speed=5),node('astrolabe','crystal',p=(0,5,0),s=.9,r=(-45,25,-15),spin=[0,1,0],speed=-8)]
            for i in range(4):
                a=i*TAU/4;parts+=[node('crescent','stone',p=tuple(polar(a,2.8,.8)),s=.5,r=(0,-i*90,0)),node('light_seed','crystal',p=tuple(polar(a,3.05,3.8)),s=1.7)]
        elif theme==1:
            parts += [node('pavilion','stone',p=(0,.7,0),s=(1.12,1.45,1.12)),node('crystal_fan','crystal',p=(0,1.15,0),s=(.7,1.2,.7)),node('astrolabe','gilt',p=(0,7.4,0),s=1.1,r=(90,0,0),spin=[0,1,0],speed=3),node('rune_disc','crystal',p=(0,7.4,0),s=2.2,r=(90,0,0),spin=[0,1,0],speed=-4)]
            for i in range(6):
                a=i*TAU/6;parts.append(node('banner','cloth',p=tuple(polar(a,2.8,4.7)),s=.75,r=(0,90-i*60,0)))
        else:
            parts += [node('trunk','bark',p=(0,.75,0),s=1.15),node('canopy','leaf',p=(0,.75,0),s=1.15),node('crescent','gilt',p=(-1.2,.7,1.1),s=.9,r=(0,50,-12)),node('crystal_fan','crystal',p=(1,.65,1),s=(.55,.8,.55))]
            for i in range(6):
                a=i*TAU/6;parts+=[node('vine','leaf',p=tuple(polar(a,2.8,.85)),s=(1.2,1.3,1.2),r=(0,90-i*60,0)),node('luminous_caps','leaf',p=tuple(polar(a,1.9,.75)),s=.85)]
            parts.append(node('astrolabe','crystal',p=(0,6.8,0),s=.9,r=(5,0,15),spin=[0,1,0],speed=4))
        out.append(dict(id=['aster_observatory','dawn_atrium','moonroot_garden'][theme],title=title,category='hero',radius=4.8,minY=-4.0,maxY=12.0,nodes=parts))
    # A crest appears on the already-built starter facade, above the interaction point, not in its path.
    out.append(dict(id='arrival_crest',title='Arrival Crown',category='threshold',radius=1.4,minY=-.1,maxY=2.9,nodes=[node('crescent','gilt',s=.55),node('light_seed','crystal',p=(0,.9,0),s=1.9),node('banner','cloth',p=(-1,1.4,0),s=.42),node('banner','cloth',p=(1,1.4,0),s=.42)]))
    out.append(dict(id='lantern_pair',title='Resonance Lanterns',category='lamp',radius=.5,minY=-.7,maxY=.6,nodes=[node('light_seed','crystal',p=(-.2,-.42,0),s=.9),node('light_seed','crystal',p=(.2,-.22,0),s=.7),node('rune_disc','gilt',s=.3,r=(90,0,0))]))
    parts=[node('rune_disc','gilt',s=.9),node('luminous_caps','leaf',p=(.18,.04,.1),s=.4)]
    out.append(dict(id='planter_bloom',title='Moonlit Planting',category='planter',radius=1.25,minY=0,maxY=1.15,nodes=parts))
    parts=[node('rune_disc','gilt',p=(0,.08,0),s=2.2)]
    for i in range(6):parts.append(node('water_arc','crystal',p=(0,1.45,0),s=.85,r=(0,i*60,0)))
    out.append(dict(id='fountain_halo',title='Resonance Fountain',category='fountain',radius=2.4,minY=0,maxY=3.7,nodes=parts))
    out.append(dict(id='plaza_inlay',title='Civic Star Inlay',category='plaza',radius=2.9,minY=-.003,maxY=.003,nodes=[node('rune_disc','gilt',s=(2.8,.14,2.8))]))
    parts=[node('terrace','stone',p=(0,-.31,0),s=.5),node('rune_disc','gilt',p=(0,.045,0),s=(1.4,.15,1.4)),node('crescent','gilt',p=(0,.04,.7),s=.55),node('crystal_fan','crystal',p=(0,.04,.45),s=.3)]
    for i in range(4):
        a=i*TAU/4;parts.append(node('luminous_caps','leaf',p=tuple(polar(a,1.1,.04)),s=.35,r=(0,i*90,0)))
    out.append(dict(id='moonwell_glade',title='Moonwell Glade',category='garden',radius=2.2,minY=-.31,maxY=2.7,nodes=parts))
    return dict(version='02F',models=MODELS,blueprints=out)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description='Regenerate original 02F assets. This intentionally overwrites that kit only.')
    parser.add_argument('--confirm-regenerate',action='store_true',help='Required to overwrite generated model/map files.')
    args=parser.parse_args()
    if not args.confirm_regenerate: parser.error('Back up artist edits, then use --confirm-regenerate to rebuild this kit.')
    for d in [R,R/'Models',R/'Textures']:
        d.mkdir(parents=True,exist_ok=True);meta(d)
    inventory=[]
    for name in MODELS:
        entry={'name':name}
        for low in [False,True]:
            model=build_model(name,low);path=R/'Models'/(name+('_lod' if low else '')+'.obj');model.write(path)
            v=np.asarray(model.v);entry['far' if low else 'near']={'vertices':len(v),'triangles':len(model.faces),'bounds_min':v.min(0).round(5).tolist(),'bounds_max':v.max(0).round(5).tolist()}
        inventory.append(entry)
    for i,key in enumerate(['moonstone','verdant','gilt','leaf']):texture_set(key,4107+i*71)
    b=R/'city02f_blueprints.json';b.write_text(json.dumps(compositions(),indent=2)+'\n');meta(b,'text')
    inv=R/'asset_inventory.json';inv.write_text(json.dumps({'origin':'Original mathematical meshes and textures authored for Crystal Manor Saga. No third-party artwork.','units':'metres, Y-up','models':inventory},indent=2)+'\n');meta(inv,'text')
    print(json.dumps(inventory,indent=2))
