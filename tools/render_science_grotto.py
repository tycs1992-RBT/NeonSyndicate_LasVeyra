"""Software projection of exact exported grotto meshes. Not Unity materials, lighting or gameplay."""
from pathlib import Path
import argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont
p=argparse.ArgumentParser(description=__doc__);p.add_argument('folder',type=Path);args=p.parse_args();root=args.folder
col={'masonry':(173,160,139),'slate':(82,108,104),'iron':(70,77,79),'azure':(68,156,164),'warm':(238,177,91),'leaf':(61,87,53),'earth':(113,99,78),'root':(88,72,53),'basalt':(116,121,114),'mineral':(68,137,141),'ground':(126,133,112),'path':(160,157,137)}
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',22)
small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
def read(name):
    vs=[];faces=[];role='masonry'
    for line in (root/(name+'.obj')).read_text().splitlines():
        a=line.split()
        if a[0]=='v':vs.append([float(x) for x in a[1:]])
        elif a[0]=='g':role=a[1]
        elif a[0]=='f':faces.append(([int(x)-1 for x in a[1:]],role))
    return np.array(vs),faces
def view(name,label,eye,at,scale,w=1100,h=780):
    vs,faces=read(name);im=Image.new('RGB',(w,h),(219,224,220));d=ImageDraw.Draw(im)
    eye=np.array(eye);at=np.array(at);forward=(at-eye).astype(float);forward/=np.linalg.norm(forward);right=np.cross(forward,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,forward)
    rel=vs-at;uv=np.stack([rel@right,-rel@up],axis=1)*scale+[w/2,h/2+70];dep=(vs-eye)@forward
    sun=np.array([-.45,1,.7]);sun/=np.linalg.norm(sun)
    pixels=np.array(im);zbuf=np.full((h,w),np.inf)
    for ids,role in sorted(faces,key=lambda item:-float(dep[item[0]].mean())):
        pts=vs[ids];n=np.cross(pts[1]-pts[0],pts[2]-pts[0]);n/=max(np.linalg.norm(n),1e-9)
        if n@(eye-pts.mean(axis=0))<=0:continue
        if np.max(uv[ids,0])<0 or np.min(uv[ids,0])>w or np.max(uv[ids,1])<0 or np.min(uv[ids,1])>h:continue
        k=.64+.36*max(0,n@sun);color=np.array([int(c*k) for c in col.get(role,col['masonry'])])
        tri=uv[ids];lo=np.maximum(np.floor(tri.min(axis=0)).astype(int),[0,0]);hi=np.minimum(np.ceil(tri.max(axis=0)).astype(int),[w-1,h-1])
        if np.any(lo>hi):continue
        yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];xx=xx+.5;yy=yy+.5
        a,b,c=tri;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
        wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;wc=1-wa-wb
        depth=wa*dep[ids[0]]+wb*dep[ids[1]]+wc*dep[ids[2]];area=zbuf[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        mask=(wa>=-1e-6)&(wb>=-1e-6)&(wc>=-1e-6)&(depth<area)&(depth>0);area[mask]=depth[mask]
        pixels[lo[1]:hi[1]+1,lo[0]:hi[0]+1][mask]=color
    im=Image.fromarray(pixels);d=ImageDraw.Draw(im)
    d.rectangle((0,0,w,48),fill=(232,236,231));d.text((20,12),label,fill=(27,40,42),font=font)
    d.rectangle((0,h-49,w,h),fill=(232,236,231));d.text((20,h-42),'Actual production C# meshes | software render | NOT Unity Game View',fill=(27,40,42),font=small)
    d.text((20,h-22),'Neutral role colours; flat ground/route context; textures and runtime lighting omitted.',fill=(27,40,42),font=small)
    return im
before=view('grotto_before','BEFORE: retained cave and woodland',[-91,23,109],[-108,2,82],25)
after=view('grotto_after','AFTER: mineral approach and reinforced entrance',[-91,23,109],[-108,2,82],25)
im=Image.new('RGB',(2200,780));im.paste(before,(0,0));im.paste(after,(1100,0));im.save(root/'grotto_comparison.png')
view('grotto_added','ADDED GEOMETRY: entrance, banks and arrival',[-97,13,100],[-110,3,82],34,1400,950).save(root/'grotto_added.png')

view('grotto_after','ARRIVAL: retained cave with integrated entrance',[-110,7,90],[-110,3,74],42,1400,950).save(root/'grotto_arrival.png')
