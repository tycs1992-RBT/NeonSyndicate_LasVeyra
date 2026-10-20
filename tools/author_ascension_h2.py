#!/usr/bin/env python3
"""Original source-authored H2 environment candidates. Offline assets, never native visual acceptance."""
import hashlib, json, math, struct, uuid
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2'
NAMESPACE=uuid.UUID('a07cb547-093c-4e13-ad2c-b828ae39e296')
def meta(path,normal=False):
    rel=path.relative_to(ROOT).as_posix();guid=uuid.uuid5(NAMESPACE,rel).hex
    if path.is_dir(): body='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix=='.cs':
        body='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif path.suffix=='.obj':
        template=(ROOT/'Assets/NeonSyndicate/Resources/VisualRebirth04A/Models/arcade_high.obj.meta').read_text()
        import re
        Path(str(path)+'.meta').write_text(re.sub(r'guid: [0-9a-f]{32}', 'guid: '+guid,template,count=1));return
    elif path.suffix=='.png':
        body=f'TextureImporter:\n  externalObjects: {{}}\n  serializedVersion: 13\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 1\n    sRGBTexture: {0 if normal or "Mask" in path.name else 1}\n  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: {1 if normal else 0}\n  isReadable: 0\n  textureType: {1 if normal else 0}\n  textureShape: 1\n  maxTextureSize: 512\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 2\n    aniso: 4\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n  alphaSource: 1\n  alphaIsTransparency: 0\n  userData: Original H2 material maps\n  assetBundleName: \n  assetBundleVariant: \n'
    else: body='TextScriptImporter:\n  externalObjects: {}\n  userData: Original H2 source asset\n  assetBundleName: \n  assetBundleVariant: \n'
    Path(str(path)+'.meta').write_text('\n'.join(line.rstrip() for line in ('fileFormatVersion: 2\nguid: '+guid+'\n'+body).splitlines())+'\n')
def folder(path):
    path.mkdir(parents=True,exist_ok=True)
    if path!=ROOT and not Path(str(path)+'.meta').exists():meta(path)
def pointed(t,width,rise):
    a=width/2;center=(rise*rise-a*a)/(2*a);radius=a+center;x=a*math.cos(t)
    return x,math.sqrt(max(0,radius*radius-(abs(x)+center)**2))
class Mesh:
    def __init__(self):self.v=[]
    def tri(self,a,b,c):
        a,b,c=map(lambda p:np.array(p,dtype=float),(a,b,c));n=np.cross(b-a,c-a);mag=np.linalg.norm(n)
        if mag<1e-9:return
        n/=mag;axis=int(np.argmax(np.abs(n)));uvaxes=[i for i in range(3) if i!=axis]
        for p in (a,b,c): self.v.append(tuple(p)+tuple(n)+(p[uvaxes[0]]*.5,p[uvaxes[1]]*.5))
    def quad(self,a,b,c,d):self.tri(a,b,c);self.tri(a,c,d)
    def prism(self,ring,depth):
        front=[(x,y,-depth/2) for x,y in ring];back=[(x,y,depth/2) for x,y in ring]
        # ring is counter-clockwise viewed from +Z. Fan polygons here are convex.
        for i in range(1,len(ring)-1):self.tri(front[0],front[i+1],front[i]);self.tri(back[0],back[i],back[i+1])
        for i in range(len(ring)):j=(i+1)%len(ring);self.quad(front[i],front[j],back[j],back[i])
    def box(self,center,size,bevel=0):
        x,y,z=center;w,h,d=np.array(size)/2
        if bevel:
            # Eight-sided bevelled cross section, rather than a six-face box.
            r=[(-w+bevel,-d), (w-bevel,-d),(w,-d+bevel),(w,d-bevel),(w-bevel,d),(-w+bevel,d),(-w,d-bevel),(-w,-d+bevel)]
            self.loft([[(x+a,y-h,z+b) for a,b in r],[(x+a,y+h,z+b) for a,b in r]])
        else:
            self.prism_at([(-w,-h),(w,-h),(w,h),(-w,h)],d,(x,y,z))
    def prism_at(self,ring,depth,at):
        other=Mesh();other.prism(ring,depth);self.add(other,at)
    def add(self,other,at=(0,0,0),scale=(1,1,1),yaw=0):
        a=math.radians(yaw);rot=np.array([[math.cos(a),0,math.sin(a)],[0,1,0],[-math.sin(a),0,math.cos(a)]])
        for row in other.v:
            p=rot@(np.array(row[:3])*scale)+at;n=rot@(np.array(row[3:6])/scale);n/=np.linalg.norm(n)
            self.v.append(tuple(p)+tuple(n)+row[6:])
    def loft(self,rings,caps=True):
        count=len(rings[0])
        for a,b in zip(rings,rings[1:]):
            for i in range(count):j=(i+1)%count;self.quad(a[j],a[i],b[i],b[j])
        if caps:
            for i in range(1,count-1):self.tri(rings[0][0],rings[0][i],rings[0][i+1]);self.tri(rings[-1][0],rings[-1][i+1],rings[-1][i])
    def tube(self,path,radii,sides=12):
        rings=[]
        for i,p in enumerate(path):
            p=np.array(p);t=np.array(path[min(i+1,len(path)-1)])-np.array(path[max(0,i-1)]);t=t/np.linalg.norm(t)
            u=np.cross(t,[0,0,1] if abs(t[2])<.9 else [1,0,0]);u/=np.linalg.norm(u);v=np.cross(t,u)
            rings.append([tuple(p+radii[i]*(u*math.cos(j*math.tau/sides)+v*math.sin(j*math.tau/sides))) for j in range(sides)])
        self.loft(rings)
    def arch(self,width,spring,rise,thick,depth,steps,at=(0,0,0),legs=True):
        # Separate tapered voussoirs, subtly recessed joints, layered extrados.
        for i in range(steps):
            t0=math.pi*i/steps+.009;t1=math.pi*(i+1)/steps-.009
            outer=lambda t:(pointed(t,width+2*thick,rise+thick)[0],spring+pointed(t,width+2*thick,rise+thick)[1])
            inner=lambda t:(pointed(t,width,rise)[0],spring+pointed(t,width,rise)[1])
            ring=[inner(t0),outer(t0),outer(t1),inner(t1)]
            self.prism_at(ring,depth,at)
        for sign in ((-1,1) if legs else ()):
            for course in range(max(1,int(spring/.6))):
                count=max(1,int(spring/.6));self.box((at[0]+sign*(width/2+thick/2),at[1]+spring*(course+.5)/count,at[2]),(thick,spring/count-.018,depth),min(.045,thick/5))

def make(name,low=False):
    m=Mesh();n=12 if low else 24
    if name=='portal_bay':
        m.arch(9.6,6.8,5.8,.68,1.25,n)
        m.arch(9.25,6.8,5.8,.13,1.55,n)
        for s in (-1,1):
            for y,w in [(0.2,1.35),(1.05,1.12),(6.55,1.35)]:m.box((s*5.15,y,0),(w,.28,1.7),.09)
    elif name=='flying_buttress':
        m.prism([(-.8,0),(.8,0),(.68,3.5),(.35,6.6),(-.35,6.6),(-.68,3.5)],1.5)
        m.tube([(0,5.1,0),(0,5.45,1),(0,6.3,2.25),(0,7.9,3.7)],[.4,.38,.32,.22],8 if low else 12)
        for y in (1.5,3.4,5.5):m.box((0,y,0),(1.55,.19,1.7),.06)
        m.tube([(0,6.5,0),(0,7.15,0),(0,8.8,0)],[.5,.4,.03],8)
    elif name=='ribbed_vault':
        # Solid roof shell over a clear ten metre nave. Ribs cross in plan and rise to the keystone.
        for z in (-2.2,2.2):
            a=Mesh();a.arch(10,7,5.9,.32,.38,n,legs=False);m.add(a,(0,0,z))
        for sign in (-1,1):
            path=[(sign*5*(1-t),7+5.7*math.sin(t*math.pi/2),-2.2+4.4*t) for t in np.linspace(0,1,7 if low else 13)]
            m.tube(path,[.18]*len(path),6 if low else 10)
        # Four thin vaulted stone webs, open beneath, open at both ends.
        for i in range(n):
            t0=math.pi*i/n;t1=math.pi*(i+1)/n
            a=(pointed(t0,10.6,6.0)[0],7.2+pointed(t0,10.6,6.0)[1]);b=(pointed(t1,10.6,6.0)[0],7.2+pointed(t1,10.6,6.0)[1])
            m.prism([(a[0],a[1]),(a[0],a[1]+.24),(b[0],b[1]+.24),(b[0],b[1])],4.4)
    elif name=='cluster_column':
        for x,z in [(0,0),(.33,0),(-.33,0),(0,.33),(0,-.33)]:
            m.tube([(x,.35,z),(x,.6,z),(x,6.5,z),(x,6.85,z)],[.28,.21,.21,.31],8 if low else 16)
        for y,rad in [(.12,.71),(.35,.58),(6.85,.64),(7.06,.75)]:m.tube([(0,y-.10,0),(0,y+.10,0)],[rad,rad],8 if low else 16)
    elif name=='slate_roof':
        for sign in (-1,1):
            # Overlapping rows with a real ridge, eaves, and weathered scalloped silhouette.
            rows=5 if low else 9
            for row in range(rows):
                t=row/rows;tn=(row+1)/rows
                for j in range(3 if low else 7):
                    count=3 if low else 7;z=-4+j*8/count;ze=z+8/count-.018
                    p=[(sign*(5.7*(1-t)),.2+8.0*t,z),(sign*(5.7*(1-tn)),.26+8.0*tn,z),(sign*(5.7*(1-tn)),.26+8.0*tn,ze),(sign*(5.7*(1-t)),.2+8.0*t,ze)]
                    if sign<0:p.reverse()
                    m.quad(*p)
        m.tube([(0,8.2,-4.2),(0,8.2,4.2)],[.17,.17],8)
    elif name=='reliquary_door':
        m.arch(3.8,3.2,2.2,.6,1.8,n)
        m.arch(3.55,3.2,2.2,.10,2.0,n)
        for s in (-1,1):m.box((s*2.4,1.5,.1),(.45,3.0,2),.1)
    elif name=='tracery_wall':
        # Recessed mullions and pierced double lancets; no solid quad pretending to be a window.
        m.box((0,.65,0),(4,1.3,.65),.08)
        for s in (-1,1):m.box((s*1.85,3.6,0),(.3,5.9,.65),.05)
        m.box((0,6.85,0),(4,.5,.75),.08)
        for x in (-.86,.86):
            a=Mesh();a.arch(1.4,4.0,1.45,.15,.36,n//2);m.add(a,(x,1.3,-.06))
        m.tube([(0,1.3,-.08),(0,6.5,-.08)],[.1,.1],8)
    elif name=='gallery_rail':
        for x in (-2,0,2):m.box((x,.75,0),(.24,1.5,.36),.05)
        m.box((0,1.42,0),(4.2,.18,.5),.05);m.box((0,.12,0),(4.2,.24,.48),.04)
        for x in (-1.5,-.5,.5,1.5):
            a=Mesh();a.arch(.62,.25,.62,.07,.18,8 if low else 14);m.add(a,(x,.22,0))
    elif name=='processional_stair':
        if low:m.loft([[(-2,0,0),(2,0,0),(2,0,4),(-2,0,4)],[(-2,.001,0),(2,.001,0),(2,.5,4),(-2,.5,4)]])
        else:
            for i in range(8):m.box((0,(i+1)*.03125, (i+.5)*.5),(4,(i+1)*.0625,.5),0)
        for s in (-1,1):m.tube([(s*2.2,.3,0),(s*2.2,.8,4)],[.2,.2],8)
    elif name=='mineral_pier':
        for x,z,h,r in [(-.45,0,3.4,.55),(.35,.1,4.8,.65),(.1,-.55,2.7,.43),(.85,.5,2.1,.38)]:
            m.tube([(x,0,z),(x,.7,z),(x+.12,h*.78,z+.1),(x+.28,h,z+.12)],[r*.9,r,r*.8,.008],6)
    elif name=='broken_apse':
        steps=10 if low else 20
        for i in range(steps):
            t0=math.pi*(.05+.9*i/steps);t1=math.pi*(.05+.9*(i+1)/steps)
            height=5+1.5*math.sin(i*1.7)
            r0,r1=5.5,6.2
            m.loft([[(r*math.cos(t),0,r*math.sin(t)) for r,t in [(r0,t0),(r0,t1),(r1,t1),(r1,t0)]],[(r*math.cos(t),height,r*math.sin(t)) for r,t in [(r0,t0),(r0,t1),(r1,t1),(r1,t0)]]])
    elif name=='forge_bay':
        m.arch(6,4.2,2.3,.65,3.4,n)
        for s in (-1,1):
            m.box((s*3.65,2.8,1.0),(.65,5.6,4.4),.15)
            m.box((s*2.75,4.9,0),(.42,.8,4.6),.08)
        # Suspended open smoke hood, leaving functional service frontage clear.
        m.loft([[(-1.6,4.9,-.9),(1.6,4.9,-.9),(1.6,4.9,1.5),(-1.6,4.9,1.5)],[(-.6,6.6,-.2),(.6,6.6,-.2),(.6,6.6,.8),(-.6,6.6,.8)]],False)
    elif name=='basalt_strata':
        rings=[];sides=10 if low else 18
        for j,(y,r) in enumerate([(-.6,.75),(.3,1.05),(1.4,.83),(2.6,.96),(3.3,.5)]):
            rings.append([(math.cos(k*math.tau/sides)*r*(1+.12*math.sin(k*2.1+j)),y+.14*math.sin(k*2+j),math.sin(k*math.tau/sides)*r*.8) for k in range(sides)])
        m.loft(rings)
    elif name=='root_causeway':
        for i in range(3):
            path=[(-1.5+i*1.5+.4*math.sin(t*3+i),.25+.55*math.sin(math.pi*t),t*6) for t in np.linspace(0,1,7 if low else 13)]
            m.tube(path,[.3*(1-.3*t) for t in np.linspace(0,1,len(path))],8 if low else 12)
    elif name=='cavern_shell':
        # Broad open mouths, asymmetrical mineral roof and walls, ten metre clear chamber.
        for i in range(12 if low else 24):
            count=12 if low else 24;t0=math.pi*i/count;t1=math.pi*(i+1)/count
            r=lambda t:(6.1*math.cos(t),2.8+4.8*math.sin(t)+.35*math.sin(t*5))
            a,b=r(t0),r(t1);m.prism([(a[0],a[1]),(a[0]*1.08,a[1]+.7),(b[0]*1.08,b[1]+.7),(b[0],b[1])],4.6)
        for s in (-1,1):
            rock=make('basalt_strata',low);m.add(rock,(s*6.4,-.4,0),(1.4,1.35,2.5))
    elif name=='fern_colony':
        # Solid tapered leaves, not alpha cards. Visual only, no shot or vehicle blockers.
        for angle in range(0,360,60 if low else 30):
            a=math.radians(angle);tip=np.array([math.sin(a)*1.7, .45,math.cos(a)*1.7]);base=np.array([0,.12,0]);mid=tip*.55+np.array([0,.9,0]);side=np.array([math.cos(a),0,-math.sin(a)])*.24
            m.quad(base,mid-side,tip,mid+side);m.quad(mid+side,tip,mid-side,base)
    elif name=='crystal_seam':
        for i in range(7):
            x=math.sin(i*2.4)*.85;z=i*.5
            m.tube([(x,-.1,z),(x,.4,z),(x+.15,1.1+i%3*.3,z+.14)],[.18,.27,.005],6)
    elif name=='record_lens':
        m.tube([(0,.05,0),(0,.24,0),(0,.35,0)],[.64,.62,.40],12 if low else 24)
        m.tube([(0,.35,0),(0,.82,0)],[.12,.10],8)
        for radius in (.46,.62):
            path=[(radius*math.cos(t),1.35+radius*math.sin(t),0) for t in np.linspace(0,math.tau,17 if low else 33)]
            m.tube(path,[.045]*len(path),6 if low else 10)
        for t in np.linspace(0,math.tau,8,endpoint=False):
            m.tube([(math.cos(t)*.47,1.35+math.sin(t)*.47,0),(math.cos(t)*.67,1.35+math.sin(t)*.67,0)],[.035,.02],6)
    elif name=='castellan_mantle':
        for rib in range(4):
            p=[(.17+rib*.10,-.18-rib*.09,-.20),(.52+rib*.12,.20-rib*.03,-.28),(.63+rib*.08,.68-rib*.07,-.22),(.29+rib*.08,1.21-rib*.13,-.16)]
            m.tube(p,[.065,.095,.075,.018],8 if low else 12)
        for y in (.14,.4,.64):m.tube([(.22,y,-.20),(.5,y+.06,-.25),(.75,y-.04,-.22)],[.025,.035,.025],6)
    else:raise ValueError(name)
    return m
ROLES={**{n:'masonry' for n in ['portal_bay','flying_buttress','ribbed_vault','cluster_column','reliquary_door','tracery_wall','gallery_rail','processional_stair','broken_apse','forge_bay']},'slate_roof':'slate','mineral_pier':'amethyst','basalt_strata':'basalt','root_causeway':'root','cavern_shell':'basalt','fern_colony':'leaf','crystal_seam':'azure','castellan_mantle':'iron','record_lens':'iron'}
def write_mesh(path,mesh):
    path.write_bytes(b'CMH1'+struct.pack('<i',len(mesh.v))+b''.join(struct.pack('<8f',*row) for row in mesh.v));meta(path)
def textures():
    folder(OUT/'Textures');s=256;y,x=np.mgrid[:s,:s]/s
    for name in sorted(set(ROLES.values())):
        noise=sum(np.sin((x*a+y*b)*math.tau)*w for a,b,w in [(13,7,.035),(31,-23,.025),(67,51,.012),(5,-3,.045)])
        if name=='masonry':
            course=np.floor(y*8);seam=np.minimum(np.mod(x*4+np.mod(course,2)*.5,1),1-np.mod(x*4+np.mod(course,2)*.5,1));joint=(np.minimum(np.mod(y*8,1),1-np.mod(y*8,1))<.04)|(seam<.028)
            h=.58+noise-joint*.25+.025*np.sin(course*2.1);base=np.array([.28,.29,.31]);metal=0;smooth=.24
        elif name=='slate':h=.52+noise+.05*np.sin(y*math.tau*32);base=np.array([.16,.18,.23]);metal=.12;smooth=.30
        elif name in ('amethyst','azure'):
            h=.56+noise+np.sin((x*7+y*3+np.sin(y*math.tau)*.12)*math.tau)*.018;base=np.array([.48,.24,.62] if name=='amethyst' else [.16,.49,.55]);metal=.15;smooth=.72
        elif name=='basalt':h=.56+noise+.055*np.sin((y*17+x*2+np.sin(x*math.tau*3)*.1)*math.tau);base=np.array([.29,.33,.33]);metal=.02;smooth=.25
        elif name=='root':h=.56+noise+.05*np.sin((x*23+np.sin(y*math.tau)*.2)*math.tau);base=np.array([.27,.21,.16]);metal=0;smooth=.2
        elif name=='leaf':h=.6+noise+.07*np.cos(x*math.tau*12);base=np.array([.20,.36,.31]);metal=0;smooth=.3
        else:h=.54+noise;base=np.array([.32,.28,.23]);metal=.75;smooth=.45
        color=np.clip(base[None,None,:]*(h[:,:,None]/.58),0,1)
        # Periodic tangent normal. No inversion or DirectX green-channel guess; actual Y-up tangent convention.
        dx=(np.roll(h,-1,1)-np.roll(h,1,1))*5;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*5
        n=np.stack([-dx,-dy,np.ones_like(h)],2);n/=np.linalg.norm(n,axis=2)[:,:,None]
        mask=np.stack([np.full_like(h,metal),np.clip(.82+(h-.5)*.3,0,1),np.zeros_like(h),np.clip(smooth+(h-.5)*.12,0,1)],2)
        for suffix,data in [('Albedo',color),('Normal',n*.5+.5),('Mask',mask)]:
            path=OUT/'Textures'/f'{name}_{suffix}.png';Image.fromarray(np.uint8(np.clip(data,0,1)*255)).save(path);meta(path,suffix=='Normal')
def main():
    folder(OUT);folder(OUT/'Meshes');folder(OUT/'Models');entries=[]
    for name,role in ROLES.items():
        high=make(name);low=make(name,True)
        for suffix,m in [('high',high),('low',low)]:write_mesh(OUT/'Meshes'/f'{name}_{suffix}.bytes',m)
        # Readable DCC source alongside the fixed runtime streams, no external importer dependency.
        path=OUT/'Models'/f'{name}.obj'
        with path.open('w') as f:
            f.write('# Original H2 source-authored candidate. Metres, +Y up, +Z forward. Not native accepted.\n')
            for row in high.v:f.write('v %.6f %.6f %.6f\n'%row[:3])
            for row in high.v:f.write('vt %.6f %.6f\n'%row[6:])
            for row in high.v:f.write('vn %.6f %.6f %.6f\n'%row[3:6])
            for i in range(1,len(high.v),3):f.write('f '+' '.join(f'{j}/{j}/{j}' for j in range(i,i+3))+'\n')
        meta(path)
        xyz=np.array(high.v)[:,:3]
        entries.append({'id':name,'role':role,'high_vertices':len(high.v),'low_vertices':len(low.v),'bounds_min':xyz.min(0).tolist(),'bounds_max':xyz.max(0).tolist(),'units':'metres','pivot':'local ground or documented articulated chest pivot','source':'Original offline parameter-authored mesh, not hand-sculpted or imported production art','license':'CC0-1.0','collision':'low mesh' if name not in ('fern_colony','castellan_mantle','record_lens') else 'none'})
    textures()
    manifest={'version':'04OH-H2','creator':'Neon Syndicate original project work','license':'CC0-1.0','production_approval':False,'native_import':'NOT RUN','method':'Explicit offline mesh construction, UV projection at two metres per repeat, dual-detail assets. No scraped assets, AI images, Blender sculpt or authored motion claim.','texture_channels':{'Albedo':'sRGB RGB','Normal':'linear tangent +Y','Mask':'linear R metallic, G occlusion, B unused, A smoothness'},'models':entries}
    path=OUT/'asset_manifest.json';path.write_text(json.dumps(manifest,indent=2)+'\n');meta(path)
    print(json.dumps({'models':len(entries),'high_vertices':sum(e['high_vertices'] for e in entries),'low_vertices':sum(e['low_vertices'] for e in entries)}))
if __name__=='__main__':main()
