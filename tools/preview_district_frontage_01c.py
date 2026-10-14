#!/usr/bin/env python3
"""Offline source-placement study for 01C, not a Unity screenshot.

Reconstructs one 24m-wide x 24m-high facade from the authored source modules,
using the constants and placement equations in the two DistrictArchitecture versions.
Uses neutral material classes and a CPU depth buffer. No engine lighting is simulated.
"""
import argparse
import importlib.util
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
W, H = 24.0, 24.0
COLORS = {'basalt':(38,50,66), 'sandstone':(156,164,183), 'ceramic':(64,82,108),
 'glass':(28,40,65), 'bronze':(144,115,75), 'crystal':(65,195,226),
 'crystalViolet':(142,89,202), 'crystalWarm':(224,170,91), 'fabric':(56,69,95)}


def load(path, name):
    spec=importlib.util.spec_from_file_location(name,path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    return {m.name:m for m in module.build_kit()}


class Scene:
    def __init__(self, kit):
        self.kit=kit;self.vertices=[];self.normals=[];self.faces=[];self.colors=[]
        self.near_count=0;self.far_count=0;self.instances=0
    def part(self,name,material,at,scale=(1,1,1),far=False,detailed=True):
        mesh=self.kit[name]
        if far:self.far_count+=len(mesh.faces)
        if not detailed:return
        self.near_count+=len(mesh.faces);self.instances+=1
        start=len(self.vertices)
        self.vertices.extend(tuple(at[j]+v[j]*scale[j] for j in range(3)) for v in mesh.vertices)
        for n in mesh.normals:
            q=tuple(n[j]/scale[j] for j in range(3));length=math.sqrt(sum(v*v for v in q))
            self.normals.append(tuple(v/length for v in q))
        self.faces.extend(tuple(start+i for i in face) for face in mesh.faces)
        self.colors.extend([COLORS[material]]*len(mesh.faces))
    def box(self,material,at,size,far=False):
        self.part('block',material,(at[0],at[1]-size[1]/2,at[2]),size,far)


def baseline(kit):
    s=Scene(kit)
    bays=min(16,max(1,math.floor(W/4.3)));spacing=W/bays
    floors=min(12,max(1,math.floor(H/4.5)));level=H/floors
    s.box('basalt',(0,H/2,-.64),(W,H,1.2),True)
    for floor in range(floors):
        y=floor*level;room=min(4,level-.2)
        for bay in range(bays):
            x=(bay+.5)*spacing-W*.5;at=(x,y,.14)
            if floor==0:
                scale=(min(1.1,spacing/3.5),room/4,1)
                s.part('arch_frame','sandstone',at,scale)
                s.part('arch_infill','glass',(x,y,.145),scale,True)
                if bay%3==1:s.part('lattice_panel','bronze',(x,y+.25,.24),(.86,room/3.9,1))
            else:
                scale=(min(1.1,spacing/2.5),min(1.1,room/3),1)
                s.part('window_frame','bronze',(x,y+.4,.14),scale)
                s.part('window_infill','glass',(x,y+.4,.145),scale,True)
                if floor==1 and bay%2==0:s.part('balcony','bronze',(x,y+.08,.14),(.9,1,.8))
            if floor==0 and room>3 and bay%3==1:
                for name,material in [('canopy','fabric'),('canopy_bracket','bronze')]:
                    s.part(name,material,(x,y+3.25,.14),(min(1,spacing/3.3),1,.8))
                for name,material in [('lantern','bronze'),('lantern_core','crystalWarm')]:
                    s.part(name,material,(x+spacing*.38,y+3.1,.32),(.55,.55,.55))
            if floor==floors-1 and floor>0 and bay==bays//2:s.part('rosette','bronze',(x,y+room-.8,.26),(.38,.38,.38))
            s.box('ceramic',(x,y+room-.18,.18),(2.4,.24,.14))
        for bay in range(bays+1):s.part('pilaster','sandstone',(bay*spacing-W*.5,y,-.05),(.8,room/4,.7))
        s.box('ceramic',(0,y+level-.15,.04),(W,.13,.18),True)
        if floor==0 or floor==floors-1:
            joint=(-1 if floor==0 else 1)*(W*.5-spacing)
            s.box('crystal',(joint,y+level*.52,.145),(.035,min(3.3,level*.75),.055),True)
    count=math.ceil(W/4)
    for i in range(count):
        x=(i+.5)*W/count-W*.5
        s.part('cornice','sandstone',(x,H-.12,-.1),(W/count/4,1,1))
        s.part('parapet','sandstone',(x,H+.13,-.1),(W/count/4,.8,.8))
    s.box('sandstone',(0,H+.38,-.12),(W,.7,.24),True)
    s.part('dome','ceramic',(0,H+.08,-3.4),(3.4,3.4*.8,3.4),True)
    s.part('crystal_cluster','crystal',(0,H+.08+3.4*.42,-3.4),(.46,.9,.46),True)
    return s


def refined(kit):
    s=Scene(kit);s.box('basalt',(0,H/2,-.64),(W,H,1.2),True)
    bays=min(8,max(1,math.floor(W/8.5)));spacing=W/bays
    registers=min(3,max(1,math.ceil(H/24)));level=H/registers
    for row in range(registers):
        y=row*level+.6;arc=max(1,level-1.3)
        for bay in range(bays):
            x=(bay+.5)*spacing-W*.5;scale=(min(2.4,spacing/3.4),arc/4,1.3)
            s.part('arch_frame','sandstone',(x,y,.21),scale)
            s.part('arch_frame_lod','sandstone',(x,y,.21),scale,True,False)
            s.part('arch_infill','glass',(x,y,.215),scale,True)
            s.part('window_frame','bronze',(x,y+arc*.17,.23),(scale[0]*.36,arc*.19,1))
            s.part('window_infill','crystal',(x,y+arc*.17,.245),(scale[0]*.36,arc*.19,1),True)
            if row==0 and bay==bays//2:
                s.part('window_frame','bronze',(x,.2,.34),(1.12,1.15,1))
                s.part('window_infill','glass',(x,.2,.345),(1.12,1.15,1))
                for name,material in [('canopy','fabric'),('canopy_bracket','bronze')]:
                    s.part(name,material,(x,3.35,.34),(1.1,1,.72))
                for name,material in [('lantern','bronze'),('lantern_core','crystalWarm')]:
                    s.part(name,material,(x+1.65,2.9,.49),(.66,.66,.66))
        s.box('ceramic',(0,(row+1)*level-.15,.1),(W,.20,.3),True)
    for pier in range(bays+1):
        x=max(-W*.5+.52,min(W*.5-.52,pier*spacing-W*.5))
        s.part('pilaster','ceramic',(x,0,.07),(1.5,H/4,1.45),True)
        if pier==0 or pier==bays or pier%2==0:s.part('crystal_shard','crystal',(x,3,.4),(1.9,H-3.2,.70),True)
    count=math.ceil(W/8)
    for i in range(count):
        x=(i+.5)*W/count-W*.5
        s.part('cornice','sandstone',(x,H-.12,-.1),(W/count/4,1,1.2),True)
        s.part('parapet','ceramic',(x,H+.13,-.1),(W/count/4,.85,1),True)
    s.part('dome','ceramic',(0,H+.08,-3.4),(3.4,3.4,3.4),True)
    s.part('crystal_cluster','crystal',(0,H+.08+3.4*.38,-3.4),(.82,1.05,.82),True)
    return s


def font(size):
    for path in ('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 'C:/Windows/Fonts/arial.ttf'):
        if Path(path).is_file(): return ImageFont.truetype(path,size)
    return ImageFont.load_default()


def render(scene,width,height,scale):
    # Slightly elevated orthographic view exposes the actual layered surface depth.
    yaw=math.radians(9);elev=math.radians(8)
    basis=np.array([[math.cos(yaw),0,-math.sin(yaw)],[-math.sin(yaw)*math.sin(elev),math.cos(elev),-math.cos(yaw)*math.sin(elev)],[math.sin(yaw)*math.cos(elev),math.sin(elev),math.cos(yaw)*math.cos(elev)]])
    p=np.array(scene.vertices)@basis.T
    p[:,0]=width*.5+p[:,0]*scale;p[:,1]=height-28-p[:,1]*scale
    n=np.array(scene.normals)@basis.T
    colors=np.zeros((height,width,4),dtype=np.uint8);depth=np.full((height,width),-np.inf)
    for index,face in enumerate(scene.faces):
        if n[face[0],2]<=0:continue
        a,b,c=p[list(face)]
        x0=max(0,math.floor(min(a[0],b[0],c[0])));x1=min(width-1,math.ceil(max(a[0],b[0],c[0])))
        y0=max(0,math.floor(min(a[1],b[1],c[1])));y1=min(height-1,math.ceil(max(a[1],b[1],c[1])))
        if x0>x1 or y0>y1:continue
        d=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(d)<1e-9:continue
        yy,xx=np.mgrid[y0:y1+1,x0:x1+1];xx=xx+.5;yy=yy+.5
        u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/d
        v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/d;w=1-u-v
        z=u*a[2]+v*b[2]+w*c[2];dv=depth[y0:y1+1,x0:x1+1]
        visible=(u>=-1e-8)&(v>=-1e-8)&(w>=-1e-8)&(z>=dv)
        dv[visible]=z[visible]
        shade=.43+.57*max(0,np.dot(np.array(scene.normals[face[0]]),np.array((.33,.4,.855))))
        color=tuple(int(q*shade) for q in scene.colors[index])
        colors[y0:y1+1,x0:x1+1][visible]=(*color,255)
    return Image.fromarray(colors)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline-generator',type=Path,required=True)
    parser.add_argument('--output',type=Path,default=ROOT/'Docs/ArtDistrict01C/FRONTAGE_SOURCE_COMPARISON.png')
    args=parser.parse_args()
    before=baseline(load(args.baseline_generator,'before'));after=refined(load(ROOT/'tools/generate_district_meshes.py','after'))
    image=Image.new('RGB',(1600,1160),'#111a28');draw=ImageDraw.Draw(image)
    draw.text((35,24),'01C | A DIFFERENT ARCHITECTURAL SCALE',font=font(30),fill='#e3e9f1')
    draw.text((35,70),'Offline source-placement reconstruction of one 24m x 24m facade, using shipped OBJ meshes.',font=font(20),fill='#b9cce0')
    draw.text((35,103),'Not a Unity screenshot. Same view, scale and neutral material classes; no texture or lighting simulation.',font=font(18),fill='#e3b776')
    for column,(label,scene) in enumerate((('01B | SMALL REPEATED WINDOWS',before),('01C | MONUMENTAL MINERAL ARCADES',after))):
        x=35+column*780
        draw.rounded_rectangle((x,150,x+748,1010),radius=9,fill='#1b283b',outline='#3b4c66')
        draw.text((x+18,171),label,font=font(22),fill='#c2d5e9')
        layer=render(scene,730,755,25)
        image.paste(layer,(x+9,213),layer)
        draw.text((x+18,978),f'{scene.instances} placed parts | {scene.near_count:,} near / {scene.far_count:,} far triangles',font=font(17),fill='#c0d4e9')
    draw.text((35,1040),'The source assembly keeps the wall envelope and human-scale threshold. Growth begins above head height.',font=font(19),fill='#b8cede')
    draw.text((35,1075),'Counts cover this representative facade plus backing and crown, not the entire district. No frame-rate claim.',font=font(19),fill='#b8cede')
    draw.text((35,1110),'Actual Unity import, LOD transitions, collision alignment, appearance and performance still require Play mode.',font=font(19),fill='#e3b776')
    args.output.parent.mkdir(parents=True,exist_ok=True);image.save(args.output)
    stats={'scope':'Offline mirrored source-placement study, width24m height24m, one facade plus backing/crown. Not Unity renderer capture.', 'before':{'parts':before.instances,'near_triangles':before.near_count,'far_triangles':before.far_count},'after':{'parts':after.instances,'near_triangles':after.near_count,'far_triangles':after.far_count},'image':args.output.name,'unity_tests':'NOT RUN'}
    args.output.with_suffix('.json').write_text(json.dumps(stats,indent=2)+'\n');print(json.dumps(stats,indent=2))

if __name__=='__main__':main()
