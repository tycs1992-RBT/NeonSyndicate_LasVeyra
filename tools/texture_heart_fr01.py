"""Original repeatable one-metre stone/slate/timber/moss/mineral texture families."""
from pathlib import Path
import numpy as np
from PIL import Image
import json,hashlib
root=Path(__file__).resolve().parents[1];art=root/'Assets/NeonSyndicate/Art/HeartFR01';manifest=json.loads((art/'manifest.json').read_text());rows=[]
y,x=np.mgrid[0:1024,0:1024]/1024
for k,(name,base)in enumerate([('Masonry',[106,97,78]),('CarvedTrim',[170,153,115]),('SlateTimber',[45,57,65]),('MossRoot',[44,72,37]),('AmethystBronze',[105,54,133])]):
 rng=np.random.default_rng(61724+k);h=np.zeros_like(x)
 for i in range(35):
  a,b=rng.integers(-30,31,2);h+=np.sin(2*np.pi*(a*x+b*y)+rng.uniform(0,6.28))/(1+abs(a)+abs(b))
 h=(h-h.min())/(h.max()-h.min());mortar=np.zeros_like(h)
 if k<2:
  course=(np.floor(y*8)%2)*.5;xx=(x*6+course)%1;yy=(y*8)%1;mortar=(np.minimum(xx,1-xx)<.025)|(np.minimum(yy,1-yy)<.025);h=np.where(mortar,h*.15,h*.7+.3)
 elif k==2:h=h*.4+.3+.18*np.sin(2*np.pi*y*32+.18*np.sin(2*np.pi*x*2))
 h[-1]=h[0];h[:,-1]=h[:,0]
 basecolor=np.clip(np.array(base)[None,None,:]*(.6+.6*h[:,:,None]),0,255).astype('uint8');Image.fromarray(basecolor).save(art/(name+'_BaseColor.png'))
 dx=(np.roll(h,-1,1)-np.roll(h,1,1))*.8;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*.8;n=np.stack([-dx,-dy,np.ones_like(h)],axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None];Image.fromarray(np.clip((n*.5+.5)*255,0,255).astype('uint8')).save(art/(name+'_Normal.png'))
 packed=np.zeros((1024,1024,4),dtype='uint8');packed[:,:,0]=180 if k==4 else 0;packed[:,:,3]=(35+h*(85 if k==4 else 35)).astype('uint8');Image.fromarray(packed).save(art/(name+'_MetallicSmoothness.png'))
 for channel in ['BaseColor','Normal','MetallicSmoothness']:
  p=art/(name+'_'+channel+'.png');rows.append({'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
manifest['textures']=rows;(art/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
