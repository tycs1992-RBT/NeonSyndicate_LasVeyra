from pathlib import Path
import numpy as np
from PIL import Image
out=Path(__file__).resolve().parents[1]/'Assets/NeonSyndicate/Art/HeartWR02';out.mkdir(parents=True,exist_ok=True)
n=1024; z,x=np.mgrid[0:n,0:n]/n
rng=np.random.default_rng(62028)
def noise(seed):
 r=np.random.default_rng(seed);v=np.zeros((n,n))
 for size,weight in ((8,.45),(32,.27),(128,.18),(512,.1)):
  a=r.random((size,size)); a=np.pad(a,((0,1),(0,1)),mode='wrap')
  u=x*size;w=z*size;i=u.astype(int);j=w.astype(int);fx=u-i;fz=w-j
  fx=fx*fx*(3-2*fx);fz=fz*fz*(3-2*fz)
  v+=weight*((a[j,i]*(1-fx)+a[j,i+1]*fx)*(1-fz)+(a[j+1,i]*(1-fx)+a[j+1,i+1]*fx)*fz)
 return v
fine=noise(3); broad=noise(19)
def save(name,color,height,rough):
 c=np.uint8(np.clip(color,0,1)*255);Image.fromarray(c,'RGB').save(out/(name+'_BaseColor.png'))
 dx=(np.roll(height,-1,1)-np.roll(height,1,1))*12;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*12
 norm=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);norm/=np.linalg.norm(norm,axis=-1)[...,None]
 Image.fromarray(np.uint8((norm*.5+.5)*255),'RGB').save(out/(name+'_Normal.png'))
 m=np.zeros((n,n,4),dtype=np.uint8);m[:,:,3]=np.uint8(np.clip(1-rough,0,1)*255)
 Image.fromarray(m,'RGBA').save(out/(name+'_MetallicSmoothness.png'))
row=np.floor(z*9).astype(int);u=(x*7+(row%2)*.5+.08*np.sin(z*np.pi*18));v=z*9+.018*np.sin(x*np.pi*14)
edge=np.minimum(np.minimum(u%1,1-u%1),np.minimum(v%1,1-v%1));seam=np.clip((edge-.018)/.065,0,1)
cell=(np.sin(np.floor(u)*31+np.floor(v)*71)*.5+.5);moss=(1-seam)*np.clip((broad-.38)*2.8,0,1)
base=(.26+fine*.12+cell*.06)[...,None]*np.array([1.07,1.04,.94]);base*=seam[...,None]*.5+.5
base=base*(1-moss[...,None]*.7)+moss[...,None]*np.array([.07,.12,.034])
height=seam*.58+fine*.12;save('CausewayStone',base,height,.87-fine*.07)
leaves=np.zeros((n,n));veins=np.zeros_like(leaves)
for i in range(1300):
 cx,cz=rng.random(2);angle=rng.random()*np.pi*2;length=rng.uniform(.006,.024);width=length*rng.uniform(.3,.7)
 dx=(x-cx+.5)%1-.5;dz=(z-cz+.5)%1-.5;a=dx*np.cos(angle)+dz*np.sin(angle);b=-dx*np.sin(angle)+dz*np.cos(angle)
 mask=(np.abs(a)/length+np.abs(b)/width<1);leaves[mask]=rng.uniform(.2,.85);veins[mask & (np.abs(b)<width*.12)]=.3
base=np.stack((.11+fine*.15+leaves*.14,.105+fine*.12+leaves*.08,.055+fine*.075+leaves*.025),axis=-1)
green=np.clip((broad-.51)*5,0,.5);base=base*(1-green[...,None])+green[...,None]*np.array([.07,.15,.035])
save('WoodlandFloor',base,fine*.1+leaves*.12+veins*.03,.94-fine*.1)
vein=np.exp(-np.abs(x-.5)*90);side=np.cos((z*14+np.abs(x-.5)*10)*np.pi*2)**18
base=np.stack((.07+fine*.1,.16+fine*.17,.035+fine*.06),axis=-1);base+=vein[...,None]*np.array([.035,.055,.012])
save('CanopyLeaf',base,fine*.08+vein*.1+side*.035,.8-fine*.12)
bands=(np.sin((z*14+.11*np.sin(x*2*np.pi)+fine*.1)*np.pi*2)*.5+.5);moss=np.clip((broad-.48)*5,0,.5)
base=(.24+fine*.17+bands*.045)[...,None]*np.array([1.02,1.03,.97]);base=base*(1-moss[...,None])+moss[...,None]*np.array([.06,.115,.04])
save('WeatheredStrata',base,fine*.28+bands*.15,.91-fine*.04)
groove=np.sin((x*28+fine*.75)*np.pi*2)*.5+.5
base=np.stack((.15+fine*.12+groove*.02,.105+fine*.1+groove*.02,.065+fine*.08),axis=-1)
save('AncientBark',base,groove*.25+fine*.2,.94-fine*.08)
print('15 original tileable PBR maps written',flush=True)
