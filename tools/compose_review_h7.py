#!/usr/bin/env python3
"""Compose actual matched source renders, excluding only the old H6 annotation footer."""
import json,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];dst=ROOT/'Docs/Ascension04OH/ArtReviewH7';before=ROOT/'Docs/Ascension04OH/ArtReviewH6';font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',20);small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',13);m=json.loads((dst/'render-manifest.json').read_text());old=json.loads((before/'render-manifest.json').read_text());receipts=[]
for views,name in [(['front','side','rear'],'H6-H7-body-comparison.png'),(['face'],'H6-H7-face-comparison.png')]:
 w=460;h=640;sheet=Image.new('RGB',(w*2,h*len(views)+110),(20,22,27));draw=ImageDraw.Draw(sheet);draw.text((16,10),'SOURCE 3D RENDER | H6 BEFORE / H7 AFTER',font=font,fill='white');draw.text((16,42),'Actual Blender source. NOT Unity. Appearance approval NOT GIVEN.',font=small,fill='#d7c0ff');draw.text((12,59),'H6 source '+old['source_sha256'][:16]+' | H7 source '+m['source_sha256'][:16],font=small,fill='white')
 for row,view in enumerate(views):
  for column,folder in enumerate([before,dst]):
   p=folder/(view+'.png');image=Image.open(p).convert('RGB');original=image.size
   # H6 includes a 36-pixel annotation footer outside its actual 768x1024 render.
   if column==0:
    if original!=(768,1060):raise ValueError('Unexpected H6 image dimensions; inspect rather than guessing a crop')
    image=image.crop((0,0,768,1024))
   elif original!=(768,1024):raise ValueError('Unexpected H7 render dimensions')
   receipts.append({'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'input_dimensions':original,'source_frame_crop':[0,0,768,1024]})
   image.thumbnail((w-8,h-38));x=column*w+(w-image.width)//2;y=80+row*h;sheet.paste(image,(x,y));draw.text((column*w+12,y+h-35),('H6'if column==0 else'H7')+' '+view,font=font,fill='white')
 draw.text((12,sheet.height-25),'Cycles CPU / 24 samples / AgX / equal source frames; footer excluded, original pixels preserved',font=small,fill='white');sheet.save(dst/name,optimize=True)
(dst/'comparison-manifest.json').write_text(json.dumps({'scope':__doc__,'native_unity':'NOT RUN','h6_source':old['source_sha256'],'h7_source':m['source_sha256'],'inputs':receipts,'original_h6_files_modified':False},indent=2)+'\n')
