"""Authoring tools for ordinary serialized Unity meshes. Never run on scene load or during Play."""
import math
import struct
import uuid
import re
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT/'Assets/ShardwakeVale'
SCENE = ROOT/'Assets/Scenes/ShardwakeVale.unity'
PAINT = ART/'Materials/ValePalette.mat'
SHADER = ART/'Materials/ValePalette.shader'
CODE = ROOT/'Assets/NeonSyndicate/Scripts/Runtime/ShardwakeVale'
TEMPLATE = (ROOT/'Assets/NeonSyndicate/Art/CrystalGothicProof/Meshes/CG_StoneBeam.asset').read_text()
S = {'sand':'d6aa74','stone':'d2b687','edge':'f3d1a0','shade':'876856','basalt':'505768','teal':'4b7974',
     'roof':'406979','ridge':'7298a4','wood':'895b43','endgrain':'be8963','copper':'b56b43','brass':'f2ad56',
     'warm':'ffbb60','coal':'383743','grass':'8d9b56','moss':'667e52','fern':'71a36a','needles':'36665a',
     'pine':'4b8370','lilac':'a27fa9','quartz':'ab8cc9','glint':'d9b9f2','water':'447f83','foam':'b2dad1',
     'trail':'c79e6c','ore':'6c506f','sky':'a9c7ca'}

def guid(p):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'las-veyra/shardwake/'+p.relative_to(ROOT).as_posix()).hex

def meta(p, kind='asset'):
    dest=Path(str(p)+'.meta')
    if dest.exists(): return
    base='fileFormatVersion: 2\nguid: '+guid(p)+'\n'
    if p.is_dir(): base+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif kind=='asset' or kind=='material':
        base+='NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: '+('4300000' if kind=='asset' else '2100000')+'\n'
    elif kind=='shader': base+='ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n'
    elif kind=='script':base+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {fileID: 0}\n'
    elif kind=='json':base+='TextScriptImporter:\n  externalObjects: {}\n'
    elif kind=='scene': base+='DefaultImporter:\n  externalObjects: {}\n'
    dest.write_text(base)

def rgb(value):
    v=S.get(value,value);return tuple(int(v[i:i+2],16) for i in (0,2,4))

def vector(v):
    return '{'+', '.join(f'{k}: {float(x):.7g}' for k,x in zip('xyz',v))+'}'

def quat(yaw=0,pitch=0):
    y,p=map(lambda t: math.radians(t)/2,(yaw,pitch))
    return (math.sin(p)*math.cos(y),math.sin(y)*math.cos(p),-math.sin(y)*math.sin(p),math.cos(y)*math.cos(p))

class Mesh:
    def __init__(self): self.v=[]; self.idx=[]; self.lookup={}; self.code=255
    def vert(self,p,n,c):
        key=tuple(round(float(x),5) for x in (*p,*n))+tuple(c)
        if key not in self.lookup:
            self.lookup[key]=len(self.v);self.v.append(key)
        return self.lookup[key]
    def tri(self,a,b,c,tone,out=None):
        a,b,c=[np.asarray(x,float) for x in (a,b,c)]
        n=np.cross(b-a,c-a);size=np.linalg.norm(n)
        if size<1e-8:return
        if out is not None and np.dot(n,out)<0:b,c=c,b;n=-n
        n/=size;color=rgb(tone)+(int(self.code),)
        self.idx.extend([self.vert(p,n,color) for p in (a,b,c)])
    def shaded(self,a,b,c,tones,out=None):
        # One triangle with its own colour per corner, for smooth gradients such as the painted sky.
        a,b,c=[np.asarray(x,float) for x in (a,b,c)];tones=list(tones)
        n=np.cross(b-a,c-a);size=np.linalg.norm(n)
        if size<1e-8:return
        if out is not None and np.dot(n,out)<0:b,c=c,b;tones[1],tones[2]=tones[2],tones[1];n=-n
        n/=size
        self.idx.extend([self.vert(p,n,rgb(t)+(int(self.code),)) for p,t in zip((a,b,c),tones)])
    def quad(self,a,b,c,d,tone,out=None):
        self.tri(a,b,c,tone,out);self.tri(a,c,d,tone,out)
    def beam(self,a,b,w,d,tone):
        a,b=np.array(a,float),np.array(b,float);axis=b-a;axis/=np.linalg.norm(axis)
        side=np.cross(axis,[0,1,0])
        if np.linalg.norm(side)<.001:side=np.array([1.,0,0])
        side=side/np.linalg.norm(side)*w/2;deep=np.cross(axis,side);deep=deep/np.linalg.norm(deep)*d/2
        corners=[side+deep,side-deep,-side-deep,-side+deep]
        for i in range(4):
            p,q=corners[i],corners[(i+1)%4]
            self.quad(a+p,a+q,b+q,b+p,tone,p+q)
        self.quad(*(a+p for p in corners),tone,-axis);self.quad(*(b+p for p in corners),tone,axis)
    def box(self,p,size,tone):
        x,y,z=p;w,h,d=size;self.beam((x,y-h/2,z),(x,y+h/2,z),w,d,tone)
    def rings(self,rings,tones,angle=0):
        # Rows are (cx, y, cz, rx, rz); irregular offset centres can lean and twist the silhouette.
        n=len(tones);rows=[]
        for x,y,z,rx,rz in rings:
            rows.append([np.array((x+rx*math.cos(angle+i*math.tau/n),y,z+rz*math.sin(angle+i*math.tau/n))) for i in range(n)])
        for k in range(len(rows)-1):
            for i in range(n):
                a,b=rows[k][i],rows[k][(i+1)%n];c,d=rows[k+1][(i+1)%n],rows[k+1][i]
                normal=(a+b)*.5-np.array(rings[k][0:3])
                self.quad(a,b,c,d,tones[i],normal)
        for k,up in [(0,-1),(-1,1)]:
            centre=rings[k][:3]
            for i in range(n):self.tri(centre,rows[k][i],rows[k][(i+1)%n],tones[i],(0,up,0))
    def recode(self,rule):
        # Re-assign the surface code (vertex alpha) of an already built mesh from its colour, after construction.
        self.v=[key[:9]+(int(rule(key[6:9],key[9])),) for key in self.v]
    def save(self,name):
        p=ART/'Meshes'/(name+'.asset');assert len(self.v)<65535 and self.idx
        points=np.array(self.v)[:,:3];lo,hi=points.min(0),points.max(0)
        bounds=(vector((hi+lo)/2),vector((hi-lo)/2));data=TEMPLATE.replace('CG_StoneBeam',name)
        data=re.sub(r'(m_Center: )\{[^\n]+\}',lambda m:m[1]+bounds[0],data)
        data=re.sub(r'(m_Extent: )\{[^\n]+\}',lambda m:m[1]+bounds[1],data)
        data=re.sub(r'indexCount: \d+',f'indexCount: {len(self.idx)}',data)
        data=re.sub(r'(vertexCount|m_VertexCount): \d+',lambda m:m[1]+': '+str(len(self.v)),data)
        data=re.sub(r'm_IndexBuffer: [a-f0-9]*','m_IndexBuffer: '+struct.pack('<'+'H'*len(self.idx),*self.idx).hex(),data)
        channels=list(re.finditer(r'    - stream: 0\n      offset: \d+\n      format: \d+\n      dimension: \d+',data))
        m=channels[3];data=data[:m.start()]+'    - stream: 0\n      offset: 24\n      format: 2\n      dimension: 4'+data[m.end():]
        raw=b''.join(struct.pack('<6f4B',*v[:6],*v[6:10]) for v in self.v)
        data=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(len(raw)),data)
        data=re.sub(r'_typelessdata: [\da-f]+','_typelessdata: '+raw.hex(),data)
        p.write_text(data);meta(p);return p

class Scene:
    def __init__(self):self.nodes=[];self.next=1000
    def obj(self,name,pos=(0,0,0),yaw=0,scale=(1,1,1),parent=None,pitch=0):
        node=dict(name=name,pos=pos,rot=quat(yaw,pitch),scale=scale,parent=parent,id=self.next,comps=[])
        self.next+=20;self.nodes.append(node);return node
    def comp(self,n,kind,title,data):
        fid=n['id']+2+len(n['comps']);n['comps'].append((kind,title,data,fid));return fid
    def geo(self,n,asset,solid=True,cast=True):
        link='{fileID: 4300000, guid: '+guid(asset)+', type: 2}'
        self.comp(n,33,'MeshFilter','  m_Mesh: '+link)
        self.comp(n,23,'MeshRenderer','  m_Enabled: 1\n  m_CastShadows: '+str(int(cast))+'\n  m_ReceiveShadows: 1\n  m_RenderingLayerMask: 1\n  m_Materials:\n  - {fileID: 2100000, guid: '+guid(PAINT)+', type: 2}\n  m_SortingLayerID: 0\n  m_SortingOrder: 0')
        if solid:self.comp(n,64,'MeshCollider','  m_Material: {fileID: 0}\n  m_IsTrigger: 0\n  m_Enabled: 1\n  serializedVersion: 5\n  m_Convex: 0\n  m_CookingOptions: 30\n  m_Mesh: '+link)
    def script(self,n,filename,fields):
        p=CODE/(filename+'.cs');return self.comp(n,114,'MonoBehaviour','  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid(p)+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: NeonSyndicate.Runtime::NeonSyndicate.'+filename+'\n'+fields)
    def write(self):
        legacy=(ROOT/'Assets/Scenes/NeonSyndicate.unity').read_text()
        header=legacy.split('--- !u!1 &')[0]
        header=re.sub(r'  m_Fog: \d','  m_Fog: 0',header)
        header=re.sub(r'(m_AmbientSkyColor: ).*',r'\g<1>{r: 0.56, g: 0.66, b: 0.78, a: 1}',header)
        header=re.sub(r'(m_AmbientEquatorColor: ).*',r'\g<1>{r: 0.55, g: 0.57, b: 0.56, a: 1}',header)
        header=re.sub(r'(m_AmbientGroundColor: ).*',r'\g<1>{r: 0.38, g: 0.32, b: 0.26, a: 1}',header)
        lines=[header.rstrip()];base='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}'
        for n in self.nodes:
            i=n['id'];lines += [f'--- !u!1 &{i}\nGameObject:',base,'  serializedVersion: 6\n  m_Component:']
            lines += ['  - component: {fileID: '+str(k)+'}' for k in [i+1]+[c[3] for c in n['comps']]]
            lines += ['  m_Layer: 0','  m_Name: '+n['name'],'  m_TagString: '+('MainCamera' if n['name']=='Opening camera' else 'Untagged'),'  m_IsActive: '+str(int(n.get('active',True)))]
            q=n['rot']
            lines += [f'--- !u!4 &{i+1}\nTransform:',base,'  m_GameObject: {fileID: '+str(i)+'}','  serializedVersion: 2','  m_LocalRotation: {'+', '.join(f'{k}: {v:.9f}' for k,v in zip('xyzw',q))+'}','  m_LocalPosition: '+vector(n['pos']),'  m_LocalScale: '+vector(n['scale']),'  m_ConstrainProportionsScale: 0']
            children=[v['id']+1 for v in self.nodes if v['parent'] is n]
            lines+=['  m_Children:' if children else '  m_Children: []']+['  - {fileID: '+str(k)+'}' for k in children]
            lines+=['  m_Father: {fileID: '+str(n['parent']['id']+1 if n['parent'] else 0)+'}','  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}']
            for kind,title,body,idx in n['comps']:
                lines += [f'--- !u!{kind} &{idx}\n{title}:',base,'  m_GameObject: {fileID: '+str(i)+'}',body.rstrip()]
        lines+=['--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:']+['  - {fileID: '+str(n['id']+1)+'}' for n in self.nodes if n['parent'] is None]
        SCENE.write_text('\n'.join(lines)+'\n');meta(SCENE,'scene')
