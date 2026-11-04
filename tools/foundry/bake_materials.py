"""Two original biological surface families. Existing architecture maps are reused, never regenerated."""
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A';OUT.mkdir(exist_ok=True)
n=1024;y,x=np.mgrid[0:n,0:n]/n;rng=np.random.default_rng(62029)
noise=np.zeros((n,n))
for s,w in [(8,.35),(32,.3),(128,.2),(512,.15)]:
 a=rng.random((s,s));a=np.pad(a,((0,1),(0,1)),mode='wrap');u=x*s;v=y*s;i=u.astype(int);j=v.astype(int);fx=u-i;fy=v-j;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);noise+=w*((a[j,i]*(1-fx)+a[j,i+1]*fx)*(1-fy)+(a[j+1,i]*(1-fx)+a[j+1,i+1]*fx)*fy)
def save(name,base,height,rough):
 Image.fromarray(np.uint8(np.clip(base,0,1)*255)).save(OUT/(name+'_BaseColor.png'))
 dx=(np.roll(height,-1,1)-np.roll(height,1,1))*16;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*16;normal=np.stack([-dx,-dy,np.ones_like(dx)],-1);normal/=np.linalg.norm(normal,axis=-1)[...,None];Image.fromarray(np.uint8((normal*.5+.5)*255)).save(OUT/(name+'_Normal.png'))
 metal=np.zeros((n,n,4),dtype=np.uint8);metal[:,:,3]=np.uint8(np.clip(1-rough,0,1)*255);Image.fromarray(metal).save(OUT/(name+'_MetallicSmoothness.png'))
row=np.floor(y*22);u=(x*18+(row%2)*.5)%1;v=(y*22)%1;edge=np.maximum(np.abs(u-.5)*2,np.abs(v-.5)*2);scale=np.clip((1-edge)*7,0,1);variation=np.sin(np.floor(x*18)*17+row*71)*.5+.5
base=(.075+noise*.08+scale*.045+variation*.02)[...,None]*np.array([1.04,.99,.87]);pores=(noise>.69)*.035;base-=pores[...,None];save('BasiliskHide',base,scale*.2+noise*.06,.86-noise*.1)
striations=(np.sin((x*38+noise*.6)*2*np.pi)*.5+.5);base=(.035+noise*.065+striations*.018)[...,None]*np.array([1,.94,1.08]);save('BasiliskKeratin',base,striations*.07+noise*.025,.57-noise*.1)
print('Six original 1024 PBR maps saved.')
