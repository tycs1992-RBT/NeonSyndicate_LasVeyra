#!/usr/bin/env python3
"""Software renders of the actual H2 mesh/texture files for asset review. NOT UNITY or gameplay evidence."""
import argparse,json,struct
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2'
def read(name,low=False):
    raw=(ASSETS/'Meshes'/f'{name}_{"low" if low else "high"}.bytes').read_bytes()
    assert raw[:4]==b'CMH1';return np.frombuffer(raw[8:],dtype='<f4').reshape(-1,8).astype(float)
def render(data,role,w=480,h=340):
    positions=data[:,:3];center=(positions.max(0)+positions.min(0))*.5
    view=np.array([.73,.38,.65]);view/=np.linalg.norm(view);right=np.cross([0,1,0],view);right/=np.linalg.norm(right);up=np.cross(view,right)
    p=positions-center;q=np.stack([p@right,p@up,-p@view],1);extent=np.ptp(q[:,:2],axis=0);scale=min((w-44)/max(extent[0],.01),(h-32)/max(extent[1],.01))
    q[:,0]=q[:,0]*scale+w/2;q[:,1]=-q[:,1]*scale+h/2
    image=np.full((h,w,3),[20,24,31],dtype=float);depth=np.full((h,w),np.inf)
    tex=np.asarray(Image.open(ASSETS/'Textures'/f'{role}_Albedo.png').convert('RGB'),dtype=float)/255
    light=np.array([.25,.8,.6]);light/=np.linalg.norm(light)
    for i in range(0,len(data),3):
        tri=q[i:i+3];norm=data[i:i+3,3:6].mean(0)
        if norm@view<=.0001:continue
        x0=max(0,int(np.floor(tri[:,0].min())));x1=min(w-1,int(np.ceil(tri[:,0].max())));y0=max(0,int(np.floor(tri[:,1].min())));y1=min(h-1,int(np.ceil(tri[:,1].max())))
        if x1<x0 or y1<y0:continue
        x,y=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
        a,b,c=tri;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-9:continue
        u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;t=1-u-v
        z=u*a[2]+v*b[2]+t*c[2];region=depth[y0:y1+1,x0:x1+1];inside=(u>=0)&(v>=0)&(t>=0)&(z<region)
        if not inside.any():continue
        uv=data[i:i+3,6:];tx=np.floor(np.mod(u*uv[0,0]+v*uv[1,0]+t*uv[2,0],1)*tex.shape[1]).astype(int);ty=np.floor(np.mod(u*uv[0,1]+v*uv[1,1]+t*uv[2,1],1)*tex.shape[0]).astype(int)
        color=tex[ty,tx]*(.47+.75*max(0,norm@light));color=np.clip(color,0,1)*255
        image[y0:y1+1,x0:x1+1][inside]=color[inside];region[inside]=z[inside]
    return Image.fromarray(np.uint8(image))
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((ASSETS/'asset_manifest.json').read_text());cols=4;cellw=480;cellh=390;rows=(len(manifest['models'])+cols-1)//cols
    board=Image.new('RGB',(cols*cellw,rows*cellh+100),(15,18,24));draw=ImageDraw.Draw(board)
    try:font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18);title=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',25)
    except OSError:font=title=ImageFont.load_default()
    draw.text((20,14),'H2 ORIGINAL MESH / MATERIAL CANDIDATES',font=title,fill=(222,222,232))
    draw.text((20,53),'OFFLINE ASSET REVIEW. NOT UNITY, GAMEPLAY, VISUAL APPROVAL OR PERFORMANCE EVIDENCE.',font=font,fill=(218,178,116))
    for i,item in enumerate(manifest['models']):
        x=(i%cols)*cellw;y=(i//cols)*cellh+100;board.paste(render(read(item['id']),item['role']), (x,y))
        draw.text((x+15,y+343),item['id'].replace('_',' '),font=font,fill=(213,216,228))
        draw.text((x+15,y+366),f"{item['high_vertices']//3:,} tris / {item['role']} / native NOT RUN",font=font,fill=(145,158,179))
    dest=a.output/'h2-kit-source-review.png';board.save(dest);print(str(dest))
if __name__=='__main__':main()
