from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
import argparse
parser=argparse.ArgumentParser(description='Render exact exported C# geometry. Not Unity or textured game evidence.')
parser.add_argument('folder',type=Path)
root=parser.parse_args().folder
col={'masonry':(170,158,132),'slate':(83,108,101),'iron':(68,72,71),'azure':(69,170,177),'warm':(242,169,80),'leaf':(59,90,48),'amethyst':(111,86,137)}
def read(name):
 vs=[];faces=[];role='masonry'
 for line in (root/(name+'.obj')).read_text().splitlines():
  a=line.split()
  if a[0]=='v':vs.append([float(x) for x in a[1:]])
  elif a[0]=='g':role=a[1]
  elif a[0]=='f':faces.append(([int(x)-1 for x in a[1:]],role))
 return np.array(vs),faces
def view(name,w=980,h=620,court=False):
 vs,faces=read(name);im=Image.new('RGB',(w,h),(221,225,218));d=ImageDraw.Draw(im)
 eye=np.array([49,34,-50] if not court else [104,82,-76]);at=np.array([0,5,20] if 'archive' in name else [0,5,12] if not court else [15,3,50]);forward=(at-eye).astype(float);forward/=np.linalg.norm(forward);right=np.cross(forward,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,forward)
 rel=vs-at;uv=np.stack([rel@right,-rel@up],axis=1);scale=11 if 'archive' in name else 14 if not court else 4.8;uv=uv*scale+[w/2,h/2+70];dep=(vs-eye)@forward
 sun=np.array([-.6,1,-.7]);sun/=np.linalg.norm(sun)
 for ids,role in sorted(faces,key=lambda item: -float(dep[item[0]].mean())):
  pts=vs[ids];n=np.cross(pts[1]-pts[0],pts[2]-pts[0]);l=np.linalg.norm(n);n=n/max(l,1e-9)
  if n@(eye-pts.mean(axis=0))<=0:continue
  k=.62+.38*max(0,n@sun);color=tuple(int(c*k) for c in col.get(role,col['masonry']))
  d.polygon([tuple(x) for x in uv[ids]],fill=color)
 d.text((22,18),name.replace('_',' ').upper(),fill=(24,37,40),font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',24))
 d.text((22,h-30),'Actual C# mesh export | neutral material roles | NOT Unity Game View',fill=(24,37,40),font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15))
 return im
im=Image.new('RGB',(1960,1240))
for row,id in enumerate(['lofts','forge']):
 for column,state in enumerate(['before','after']):im.paste(view(id+'_'+state),(column*980,row*620))
im.save(root/'service_mesh_comparison.png')
view('court_after',1200,800,True).save(root/'court_mesh.png')
print(root/'service_mesh_comparison.png')

im=Image.new('RGB',(1960,620))
for column,state in enumerate(['before','after']):im.paste(view('archive_'+state),(column*980,0))
im.save(root/'archive_mesh_comparison.png')
view('district_after',1600,1200,True).save(root/'district_mesh.png')
