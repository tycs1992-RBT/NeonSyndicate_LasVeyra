#!/usr/bin/env python3
"""Convert tracked CC0 source maps and author small fabric maps for the real candidate's UVs."""
import argparse,json,hashlib,shutil
from pathlib import Path
import numpy as np
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('--assets',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(exist_ok=True,parents=True)
sources={'Skin_Albedo.jpg':'skins02/skins/mindfront_skin_male_african_middleage/skin_male_african_middleage.png','Skin_Normal.png':'skins02/skins/mindfront_skin_male_african_middleage/skin_male_african_middleage_NRM.png','Skin_Specular.png':'skins02/skins/mindfront_skin_male_african_middleage/skin_male_african_middleage_SPEC.png','Brows_Albedo.png':'system/eyebrows/eyebrow001/eyebrow001.png','Lashes_Albedo.png':'system/eyelashes/eyelashes01/eyelashes01.png','Eyes_Albedo.png':'system/eyes/materials/brown_eye.png'}
rows=[]
for name,source in sources.items():
 src=a.assets/source;im=Image.open(src);dest=a.output/name
 if name.endswith('.jpg'):im.convert('RGB').save(dest,quality=95,subsampling=0)
 else:shutil.copyfile(src,dest)
 rows.append({'file':name,'source_member':source,'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'width':im.width,'height':im.height,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'conversion':'RGB JPEG quality95, no chroma subsampling'if name.endswith('.jpg')else'original source bytes unchanged'})
size=1024;y,x=np.mgrid[:size,:size];rng=np.random.default_rng(406061);grain=rng.random((size,size))
for name,base,scale in [('Cloth',np.array([28,30,36.]),.035),('Leather',np.array([39,29,23.]),.065)]:
 if name=='Cloth':height=np.sin(x*np.pi/2)*np.sin(y*np.pi/2)*.16+grain*.06;tone=.92+.08*grain+.04*np.sin((x+y)*np.pi/4)
 else:height=grain*.14+.04*np.sin(x*.02)*np.sin(y*.025);tone=.85+.3*grain
 rgb=np.clip(base[None,None,:]*tone[:,:,None],0,255).astype('uint8');Image.fromarray(rgb).save(a.output/(name+'_Albedo.png'))
 gy,gx=np.gradient(height);n=np.dstack((-gx*scale,-gy*scale,np.ones_like(height)));n/=np.linalg.norm(n,axis=2)[:,:,None];Image.fromarray(np.rint((n*.5+.5)*255).astype('uint8')).save(a.output/(name+'_Normal.png'))
 for suffix in ['Albedo','Normal']:
  f=a.output/(name+'_'+suffix+'.png');rows.append({'file':f.name,'creator':'Project-authored fabric microstructure','license':'CC0-1.0','width':size,'height':size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(a.output/'texture-lineage.json').write_text(json.dumps({'scope':'Actual received maps and original small fabric maps. Not a production appearance approval.','textures':rows},indent=2)+'\n')
print('ACTUAL TEXTURES',len(rows),'WRITTEN')
