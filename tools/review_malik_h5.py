#!/usr/bin/env python3
"""Orthographic software review of actual repaired FBX components. NOT UNITY, PBR approval, authored animation or final Malik."""
import argparse,json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--geometry',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
 data={name:json.loads((a.geometry/(name+'-model.json')).read_text())for name in ['MaleBase','TravelClothing','Beard','ShortHair','Brows']}
 def bone(name,role):return np.array(next(b['position']for b in data[name]['bones']if b['name']==role))
 shift=bone('TravelClothing','Head')-bone('MaleBase','Head');neck=bone('MaleBase','neck_01')[2]-.055
 tris=[]
 for label,document in data.items():
  for mesh in document['meshes']:
   positions=np.array(mesh['positions'])
   for t in mesh['triangles']:
    xyz=positions[t['i']].copy()
    if label=='MaleBase'and xyz[:,2].min()<neck:continue
    if label!='TravelClothing':xyz+=shift
    material=t['material'].lower();role='Cloth'if label=='TravelClothing'else'Hair'if label in ['Beard','ShortHair','Brows']or 'hair'in material else'Eye'if'eye'in material else'Skin'
    if label=='TravelClothing'and ('regular'in material or 'base'in material):role='Hands'
    # FBX source uses Z-up; this review explicitly converts to Y-up. Native Unity conversion is separate.
    xyz=xyz[:,[0,2,1]];xyz[:,2]*=-1
    normals=np.array(t['normals'])[:,[0,2,1]];normals[:,2]*=-1
    tris.append((xyz,np.array(t['uv']),role,normals))
 textures={}
 for role in ['Cloth','Skin','Hair','Eye','Hands']:
  folder=ROOT/'Assets/NeonSyndicate/Art/CandidatesH3/Textures';path=folder/(role+'_Albedo.jpg')
  if not path.exists():path=folder/(role+'_Albedo.png')
  textures[role]=np.array(Image.open(path).convert('RGBA'),float)/255
 def render(yaw,width=560,height=720,close=False):
  angle=np.deg2rad(yaw);view=np.array([np.sin(angle),0,np.cos(angle)]);right=np.array([np.cos(angle),0,-np.sin(angle)]);up=np.array([0,1,0]);scale=height/(.52 if close else 2.15);centre=np.array([0,1.65 if close else .94,0])
  image=np.full((height,width,3),[26,29,36],float);depth=np.full((height,width),np.inf);light=np.array([-.35,.7,.65]);light/=np.linalg.norm(light)
  for xyz,uv,role,normals in tris:
   normal=np.cross(xyz[1]-xyz[0],xyz[2]-xyz[0]);length=np.linalg.norm(normal)
   if length<1e-12:continue
   normal/=length
   if normal@view<0:normal=-normal
   q=xyz-centre;screen=np.stack([q@right*scale+width*.5,-q@up*scale+height*.5,-q@view],1)
   lo=np.floor(screen[:,:2].min(0)).astype(int);hi=np.ceil(screen[:,:2].max(0)).astype(int);x0=max(0,lo[0]);y0=max(0,lo[1]);x1=min(width-1,hi[0]);y1=min(height-1,hi[1])
   if x1<x0 or y1<y0:continue
   x,y=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5);v0,v1,v2=screen;den=(v1[1]-v2[1])*(v0[0]-v2[0])+(v2[0]-v1[0])*(v0[1]-v2[1])
   if abs(den)<1e-9:continue
   u=((v1[1]-v2[1])*(x-v2[0])+(v2[0]-v1[0])*(y-v2[1]))/den;v=((v2[1]-v0[1])*(x-v2[0])+(v0[0]-v2[0])*(y-v2[1]))/den;w=1-u-v;z=u*v0[2]+v*v1[2]+w*v2[2];region=depth[y0:y1+1,x0:x1+1];inside=(u>=0)&(v>=0)&(w>=0)&(z<region)
   if not inside.any():continue
   tex=textures[role];tx=np.floor(np.mod(u*uv[0,0]+v*uv[1,0]+w*uv[2,0],1)*tex.shape[1]).astype(int);ty=np.floor((1-np.mod(u*uv[0,1]+v*uv[1,1]+w*uv[2,1],1))*tex.shape[0]).astype(int)%tex.shape[0];color=tex[ty,tx];inside&=color[:,:,3]>.45
   n=u[:,:,None]*normals[0]+v[:,:,None]*normals[1]+w[:,:,None]*normals[2];n/=np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-8);shade=.45+.55*np.maximum(0,n@light);tint=np.array([.45,.38,.34])if role=='Hair'else np.array([.48,.47,.52])if role=='Cloth'else np.ones(3);rgb=color[:,:,:3]*tint*shade[:,:,None];image[y0:y1+1,x0:x1+1][inside]=np.clip(rgb[inside]*255,0,255);region[inside]=z[inside]
  return Image.fromarray(image.astype('uint8'))
 font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18)
 board=Image.new('RGB',(1680,830),(16,19,25));d=ImageDraw.Draw(board)
 d.text((20,10),'REPAIRED RECEIVED COMPONENTS, TECHNICAL REFERENCE. NOT FINAL MALIK.',font=font,fill=(230,215,182))
 d.text((20,38),'OFFLINE FBX SOFTWARE REVIEW. NOT UNITY, OWNER APPROVAL OR PRODUCTION ANIMATION.',font=font,fill=(230,180,130))
 for i,(name,yaw)in enumerate([('Front',0),('Side',90),('Back',180)]):
  board.paste(render(yaw),(560*i,80));d.text((560*i+20,804),name+' / received wardrobe and face',font=font,fill=(200,210,225))
 board.save(a.output/'received-reference-orthographic.png')
 face=render(0,760,760,True);f=ImageDraw.Draw(face);f.rectangle((0,0,760,55),fill=(16,19,25));f.text((12,8),'RECEIVED FACE, OFFLINE. NOT FINAL MALIK / NOT UNITY.',font=font,fill=(230,180,130));face.save(a.output/'received-reference-face.png')
 (a.output/'review.json').write_text(json.dumps({'scope':__doc__,'source_components':'H5 lossless normalized derivative of H3 CC0 components','triangles':len(tris),'camera':'orthographic Y-up, FBX source converted from Z-up','shading':'simple offline albedo and directional diffuse, not Unity/PBR','owner_art_approval':'NOT REQUESTED AS FINAL / technical reference only','native':'NOT RUN'},indent=2)+'\n')
 print('Wrote actual component front/side/back and face software reviews, not Unity captures.')
if __name__=='__main__':main()
