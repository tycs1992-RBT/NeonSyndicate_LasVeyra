"""Read the checked-in scene and native Unity mesh buffers. Produce a software camera review, never a Game View."""
import argparse
import functools
import math
from pathlib import Path
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import yaml
from assets import ART, ROOT, SCENE

def array(v):return np.array([v[x] for x in 'xyz'],float)

def read_scene():
    text=SCENE.read_text();docs={}
    for head,section in zip(re.findall(r'^--- !u!(\d+) &(\d+)\n',text,re.M),re.split(r'^--- !u!\d+ &\d+\n',text,flags=re.M)[1:]):
        kind,fid=map(int,head);data=yaml.safe_load(section);docs[fid]=(kind,list(data.values())[0])
    transforms={fid:d for fid,(k,d) in docs.items() if k==4}
    @functools.lru_cache(None)
    def matrix(tid):
        tr=transforms[tid];q=tr['m_LocalRotation'];x,y,z,w=(q[k] for k in 'xyzw')
        rot=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
        m=np.eye(4);m[:3,:3]=rot@np.diag(array(tr['m_LocalScale']));m[:3,3]=array(tr['m_LocalPosition'])
        parent=tr['m_Father']['fileID'];return matrix(parent)@m if parent else m
    assets={}
    for p in ART.rglob('*.asset'):
        g=re.search(r'guid: (\w+)',Path(str(p)+'.meta').read_text())[1]
        data=yaml.safe_load(re.split(r'^---[^\n]*\n',p.read_text(),flags=re.M)[1])['Mesh']
        n=data['m_VertexData']['m_VertexCount'];raw=bytes.fromhex(str(data['m_VertexData']['_typelessdata']))
        dt=np.dtype([('floats','<f4',6),('color','u1',4)]);v=np.frombuffer(raw,dt)
        idx=np.frombuffer(bytes.fromhex(str(data['m_IndexBuffer'])),dtype='<u2').reshape(-1,3)
        assert len(v)==n and np.max(idx)<n and idx.size==data['m_SubMeshes'][0]['indexCount']
        assets[g]=(v['floats'][:,:3],v['floats'][:,3:],v['color'][:,:3],idx)
    objects={};meshes=[];camera=None
    for fid,(kind,data) in docs.items():
        if kind!=1:continue
        ids=[row['component']['fileID'] for row in data['m_Component']];m=matrix(ids[0])
        name=data['m_Name'];objects[name]=(fid,m,ids)
        for cid in ids[1:]:
            k,comp=docs[cid]
            if k==20:camera=(m,comp)
            if k!=33:continue
            v,n,c,idx=assets[comp['m_Mesh']['guid']]
            vert=v@m[:3,:3].T+m[:3,3];norm=n@np.linalg.inv(m[:3,:3]);norm/=np.maximum(np.linalg.norm(norm,axis=1,keepdims=True),1e-9)
            meshes.append((name,vert,norm,c,idx))
    return docs,objects,meshes,camera

def draw(meshes,eye,forward,up,fov=58,w=1088,h=612):
    forward=forward/np.linalg.norm(forward);right=np.cross(up,forward);right/=np.linalg.norm(right);up=np.cross(forward,right)
    focal=h*.5/math.tan(math.radians(fov/2))
    image=np.zeros((h,w,3),np.uint8);image[:]=[163,196,209];depth=np.full((h,w),1e9)
    sun=np.array([-.52,.85,-.38]);sun/=np.linalg.norm(sun)
    for name,v,n,c,idx in meshes:
        if name in ('Installed hearth lens','Restored furnace light'):continue
        rel=v-eye;z=rel@forward
        xs=w/2+focal*(rel@right)/np.maximum(.001,z);ys=h/2-focal*(rel@up)/np.maximum(.001,z)
        shade=.59+.34*np.clip(n@sun,0,1)+.17*np.clip(n[:,1]*.5+.5,0,1)
        haze=.55*np.clip((np.linalg.norm(rel,axis=1)-52)/165,0,1)
        color=(c*shade[:,None])*(1-haze[:,None])+np.array([163,189,199])*haze[:,None]
        for ia,ib,ic in idx:
            if min(z[ia],z[ib],z[ic])<.09:continue
            ax,ay,bx,by,cx,cy=xs[ia],ys[ia],xs[ib],ys[ib],xs[ic],ys[ic]
            den=(by-cy)*(ax-cx)+(cx-bx)*(ay-cy)
            if den<1e-8:continue
            l=max(0,math.floor(min(ax,bx,cx)));r=min(w-1,math.ceil(max(ax,bx,cx)));t=max(0,math.floor(min(ay,by,cy)));b=min(h-1,math.ceil(max(ay,by,cy)))
            if l>r or t>b:continue
            Y,X=np.ogrid[t+.5:b+1.5,l+.5:r+1.5]
            a=((by-cy)*(X-cx)+(cx-bx)*(Y-cy))/den
            bb=((cy-ay)*(X-cx)+(ax-cx)*(Y-cy))/den
            cc=1-a-bb;inside=(a>=0)&(bb>=0)&(cc>=0)
            d=1/np.maximum(1e-8,a/z[ia]+bb/z[ib]+cc/z[ic]);dest=depth[t:b+1,l:r+1];update=inside&(d<dest)
            if not np.any(update):continue
            dest[update]=d[update]
            image[t:b+1,l:r+1][update]=np.clip(color[[ia,ib,ic]].mean(axis=0),0,255).astype(np.uint8)
    return Image.fromarray(image)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('output',type=Path);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
    docs,objects,meshes,camera=read_scene();m,c=camera
    shots=[('opening',m[:3,3],m[:3,2],m[:3,1]),('crossing',np.array([5,3.5,33]),np.array([.04,.05,1.]),np.array([0.,1,0]))]
    for name,eye,fwd,up in shots:
        im=draw(meshes,eye,fwd,up,c['field of view']);sheet=Image.new('RGB',(im.width,im.height+38),'#233332');sheet.paste(im)
        dr=ImageDraw.Draw(sheet);dr.text((13,im.height+9),'SHARDWAKE VALE   /   '+name.upper()+'     |     Software view of the serialized environment. Actors and native lighting omitted.',fill='#f0e5cf',font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15))
        sheet.quantize(224).save(a.output/(name+'.png'),optimize=True)
    print(len(meshes),'visible mesh owners,',sum(len(row[-1]) for row in meshes),'placed triangles')

if __name__=='__main__':main()
