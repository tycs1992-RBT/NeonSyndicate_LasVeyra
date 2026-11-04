#!/usr/bin/env python3
"""Label original DCC evidence and lay out exact delivered map channels. Not Unity."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];p=ROOT/'Docs/LiveGroundL3';art=ROOT/'Assets/NeonSyndicate/Art/LiveGround/Redspire';font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
for f in sorted(p.glob('*.png')):
 if f.stem.endswith(('_Before','_After','2x2','4x4')):
  im=Image.open(f);im.load();im=im.convert('RGB')
  if im.height==728:im=im.crop((0,78,im.width,im.height))
  bar=Image.new('RGB',(im.width,im.height+78),(20,24,28));bar.paste(im,(0,78));d=ImageDraw.Draw(bar);d.text((18,10),f.stem.replace('_',' '),fill='white',font=ImageFont.truetype(font,23));d.text((18,45),'BLENDER SOURCE RENDER. Unity NOT RUN. Before is a palette approximation.',fill=(239,205,132),font=ImageFont.truetype(font,15));bar.save(f)
for distance in ['Close','Mid','Far']:
 a=Image.open(p/(distance+'_Before.png'));b=Image.open(p/(distance+'_After.png'));out=Image.new('RGB',(2000,a.height));out.paste(a,(0,0));out.paste(b,(1000,0));out.save(p/(distance+'_Comparison.png'))
out=Image.new('RGB',(1200,880),(20,24,28));d=ImageDraw.Draw(out)
for i,name in enumerate(['RedspireWeatheredEarth','RedspireStonySand']):
 for j,ch in enumerate(['BaseColor','Normal','MetallicSmoothness']):
  im=Image.open(art/(name+'_'+ch+'.png'));im.load()
  if ch=='MetallicSmoothness':im=im.getchannel('A')
  out.paste(im.convert('RGB').resize((392,360)),(j*400,i*430+60));d.text((j*400+8,i*430+10),name,fill='white',font=ImageFont.truetype(font,15));d.text((j*400+8,i*430+32),ch if j<2 else 'Smoothness alpha (roughness inverse)',fill=(239,205,132),font=ImageFont.truetype(font,14))
out.save(p/'Map_Breakdown.png')
print('Labeled 10 DCC views, three matched pairs and channel breakdown.')
