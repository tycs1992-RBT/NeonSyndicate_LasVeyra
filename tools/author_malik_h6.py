#!/usr/bin/env python3
"""Author a distinct editable Malik candidate from fitted CC0 anatomy. No gameplay or approval is changed."""
import argparse,json,math,hashlib
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree
p=argparse.ArgumentParser();p.add_argument('--fitted',type=Path,required=True);p.add_argument('--textures',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--source-dir',type=Path,required=True);a=p.parse_args()
bpy.ops.wm.open_mainfile(filepath=str(a.fitted));tex=a.textures;out=a.output;out.mkdir(parents=True,exist_ok=True)
rig=bpy.data.objects['MalikRig'];body=bpy.data.objects['MalikBody']
# Retain the received anatomy's designed joint loops. Apply only helper masks before source fitting.
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for obj in list(bpy.data.objects):
 if obj.type not in {'MESH','ARMATURE'}:bpy.data.objects.remove(obj,do_unlink=True)
for obj in [x for x in bpy.data.objects if x.type=='MESH']:
 active(obj)
 if obj.data.shape_keys is not None:
  mixed=obj.shape_key_add(name='H6 evaluated source shape',from_mix=True)
  positions=[v.co.copy()for v in mixed.data];obj.shape_key_clear()
  for v,point in zip(obj.data.vertices,positions):v.co=point
 for mod in list(obj.modifiers):
  if mod.type=='MASK':bpy.ops.object.modifier_apply(modifier=mod.name)
  elif mod.type=='SUBSURF':obj.modifiers.remove(mod)
# Clear old helper masks after fitting, not before MHCLO's indexed correspondence.
for old,new in [('Root','root'),('head','Head')]:
 rig.data.bones[old].name=new
 for obj in [x for x in bpy.data.objects if x.type=='MESH']:
  if old in obj.vertex_groups:obj.vertex_groups[old].name=new
# Author fitted leather trousers from the new anatomical mesh, not the old fallback.
pants=body.copy();pants.data=body.data.copy();bpy.context.collection.objects.link(pants);pants.name='Pants'
import bmesh
bm=bmesh.new();bm.from_mesh(pants.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>1.035 or v.co.z<.13],context='VERTS');bm.to_mesh(pants.data);bm.free()
pants.data.update()
for v in pants.data.vertices:
 wrinkle=.003*math.sin(v.co.z*72+v.co.x*18)*math.exp(-((v.co.z-.50)/.12)**2)
 v.co+=v.normal*(.014+wrinkle)
bones=set(rig.data.bones.keys())
# Body positions and authored normalized complete weights for fitting new garment panels.
verts=[body.matrix_world@v.co for v in body.data.vertices]
kdt=KDTree(len(verts))
for i,v in enumerate(verts):kdt.insert(v,i)
kdt.balance()
bodyweights=[]
for v in body.data.vertices:
 rows=[(body.vertex_groups[g.group].name,g.weight)for g in v.groups if body.vertex_groups[g.group].name in bones and g.weight>0]
 if not rows:raise ValueError('Unweighted real body vertex')
 s=sum(w for _,w in rows);bodyweights.append([(b,w/s)for b,w in rows])
def skin(obj,rigid=None,skirt_side=None):
 obj.parent=rig
 for group in list(obj.vertex_groups):obj.vertex_groups.remove(group)
 groups={b:obj.vertex_groups.new(name=b)for b in bones}
 for v in obj.data.vertices:
  point=obj.matrix_world@v.co
  if rigid:rows=[(rigid,1)]
  elif skirt_side:
   t=max(0,min(1,(1.04-point.z)/.65));rows=[('pelvis',1-.64*t),('thigh_'+skirt_side,.64*t)]
  else:rows=bodyweights[kdt.find(point)[1]]
  for name,w in rows:
   if w>0:groups[name].add([v.index],w,'REPLACE')
 for m in list(obj.modifiers):
  if m.type=='ARMATURE':obj.modifiers.remove(m)
 m=obj.modifiers.new('Original-compatible visual skin','ARMATURE');m.object=rig;m.use_deform_preserve_volume=False
 for poly in obj.data.polygons:poly.use_smooth=True
 return obj
def material(name,color,rough=.5,metal=0,albedo=None,normal=None,alpha=False,skin=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 if skin:p.inputs['Specular IOR Level'].default_value=.28;p.inputs['Subsurface Weight'].default_value=.04;p.inputs['Subsurface Radius'].default_value=(1,.45,.25)
 if albedo:
  im=bpy.data.images.load(str(tex/albedo),check_existing=True);t=n.new('ShaderNodeTexImage');t.image=im;l.new(t.outputs['Color'],p.inputs['Base Color'])
  if alpha:l.new(t.outputs['Alpha'],p.inputs['Alpha']);m.surface_render_method='DITHERED'
 if normal:
  im=bpy.data.images.load(str(tex/normal),check_existing=True);im.colorspace_settings.name='Non-Color';t=n.new('ShaderNodeTexImage');t.image=im;nm=n.new('ShaderNodeNormalMap');l.new(t.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs['Normal'],p.inputs['Normal']);nm.inputs['Strength'].default_value=.35 if skin else .65
 return m
skinmat=material('H6 Skin',(.34,.2,.14),.55,albedo='Skin_Albedo.jpg',normal='Skin_Normal.png',skin=True)
cloth=material('H6 Charcoal wool',(.025,.029,.038),.79,albedo='Cloth_Albedo.png',normal='Cloth_Normal.png')
leather=material('H6 Worn leather',(.045,.034,.029),.76,albedo='Leather_Albedo.png',normal='Leather_Normal.png')
steel=material('H6 Blackened steel',(.025,.030,.038),.55,.75)
bronze=material('H6 Aged bronze',(.18,.095,.035),.47,.78)
gem=material('H6 Restrained amethyst',(.16,.035,.28),.22,.15);pbr=gem.node_tree.nodes.get('Principled BSDF');pbr.inputs['Emission Color'].default_value=(.12,.012,.25,1);pbr.inputs['Emission Strength'].default_value=.24
hair=material('H6 Hair',(.008,.005,.003),.8)
brows=material('H6 Brows',(.025,.016,.012),.6,albedo='Brows_Albedo.png',alpha=True)
lashes=material('H6 Lashes',(.025,.016,.012),.6,albedo='Lashes_Albedo.png',alpha=True)
eye=material('H6 Eyes',(.35,.2,.08),.15,albedo='Eyes_Albedo.png')
body.data.materials.clear();body.data.materials.append(skinmat)
for name,mat in [('Brows',brows),('Lashes',lashes),('Iris',eye),('Pants',leather),('Boots',leather)]:
 obj=bpy.data.objects.get(name)
 if not obj:raise ValueError('Missing fitted source '+name)
 obj.data.materials.clear();obj.data.materials.append(mat)
 if name in {'Hair','Beard','Brows','Lashes','Iris'}:skin(obj,rigid='Head')
 else:skin(obj)
# Trim the received casual outfit to a lower leather layer, hidden at the upper edge by the long coat.
import bmesh
pants=bpy.data.objects['Pants'];bm=bmesh.new();bm.from_mesh(pants.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if (pants.matrix_world@v.co).z>1.015],context='VERTS');bm.to_mesh(pants.data);bm.free()
# A fitted leather underlayer closes the real shoulder/torso transitions beneath the outer coat.
under=body.copy();under.data=body.data.copy();bpy.context.collection.objects.link(under);under.name='Fitted leather underlayer'
bm=bmesh.new();bm.from_mesh(under.data);bm.verts.ensure_lookup_table()
# Keep the complete anatomical shoulder and arm surface beneath the loose shell.
# The earlier rectangular crop left open underarms and elbow holes in rear/side views.
removed=[]
for v in bm.verts:
 original=under.data.vertices[v.index]
 distal=sum(g.weight for g in original.groups if under.vertex_groups[g.group].name.startswith(('hand_','thumb_','index_','middle_','ring_','pinky_')))
 if v.co.z<.96 or v.co.z>1.59 or distal>.62:removed.append(v)
bmesh.ops.delete(bm,geom=removed,context='VERTS');bm.to_mesh(under.data);bm.free();under.data.update()
for v in under.data.vertices:v.co+=v.normal*.019
under.data.materials.clear();under.data.materials.append(leather);skin(under)
# Source body stays skin only where it is actually visible, preventing body-through-clothes and duplicate hidden surfaces.
bm=bmesh.new();bm.from_mesh(body.data)
remove=[]
for f in bm.faces:
 points=[body.matrix_world@v.co for v in f.verts]
 if all(q.z<1.55 and abs(q.x)<.46 for q in points):remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(body.data);bm.free()
skin(body)
newmeshes=[]
def mesh(name,vs,fs,mat,rigid=None,skirt_side=None,thick=.003,subdiv=1):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
 active(o)
 # Real UVs for the fixed cloth/leather maps. These are editable art meshes, not runtime mesh generation.
 if name=='Hair':
  uv=o.data.uv_layers.new(name='Groom strand atlas')
  for poly in o.data.polygons:
   for loop in poly.loop_indices:
    index=o.data.loops[loop].vertex_index%12;uv.data[loop].uv=(index%2,(index//2)/5)
 elif name=='Scalp roots':
  uv=o.data.uv_layers.new(name='Scalp')
  for loop in o.data.loops:
   q=o.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(q.x*4+.5,q.y*4+.5)
 else:
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 if subdiv:
  m=o.modifiers.new('Authored surface smoothing','SUBSURF');m.levels=subdiv;m.render_levels=subdiv;bpy.ops.object.modifier_apply(modifier=m.name)
 if thick:
  m=o.modifiers.new('Real garment thickness','SOLIDIFY');m.thickness=thick;m.offset=0;bpy.ops.object.modifier_apply(modifier=m.name)
 if name.startswith('Armor panel'):
  m=o.modifiers.new('Forged edge bevel','BEVEL');m.width=.0015;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
 skin(o,rigid,skirt_side);newmeshes.append(o);return o
def loft(name,rings,mat,rigid=None,start=0,end=2*math.pi,segments=40,skirt_side=None,thick=.003):
 vs=[];fs=[]
 for j,(z,rx,ry,cy)in enumerate(rings):
  for i in range(segments+1):
   t=start+(end-start)*i/segments;fold=.0028*math.sin(t*11+j*.6)*(1 if j>0 else .4)
   vs.append(((rx+fold)*math.sin(t),cy-(ry+fold)*math.cos(t),z))
 for j in range(len(rings)-1):
  for i in range(segments):
   k=j*(segments+1)+i;fs.append((k,k+1,k+segments+2,k+segments+1))
 return mesh(name,vs,fs,mat,rigid,skirt_side,thick)
# Patterned, loose upper coat with open collar. Actual split tails are independent from the pelvis.
loft('Coat upper',[(.97,.185,.145,0),(1.04,.186,.145,0),(1.16,.205,.157,0),(1.30,.235,.174,0),(1.43,.25,.16,.01),(1.50,.22,.13,.02),(1.55,.10,.10,.01)],cloth,start=.06,end=2*math.pi-.06)
for side,start,end in [('l',.13,math.pi-.055),('r',math.pi+.055,2*math.pi-.13)]:
 loft('Split coat tail '+side,[(1.035,.19,.15,0),(.94,.20,.16,0),(.82,.225,.18,.01),(.69,.25,.205,.025),(.53,.275,.225,.03),(.43,.29,.235,.02)],cloth,start=start,end=end,skirt_side=side)
# Actual short swept groom, seated on the new source scalp. This is exported mesh art.
import random
rng=random.Random(40606);patch=[]
for f in body.data.polygons:
 points=[body.matrix_world@body.data.vertices[i].co for i in f.vertices]
 center=sum(points,Vector())/len(points)
 if abs(center.x)<.12 and (center.z>1.795 or center.z>1.72 and center.y>-.025):
  for i in range(1,len(points)-1):patch.append((points[0],points[i],points[i+1]))
if not patch:raise ValueError('No anatomical scalp surface')
vs=[];fs=[]
for _ in range(7500):
 aa,bb,cc=rng.choice(patch);u=rng.random();v=rng.random()
 if u+v>1:u=1-u;v=1-v
 root=aa+(bb-aa)*u+(cc-aa)*v;normal=(bb-aa).cross(cc-aa).normalized()
 if normal.dot(root-Vector((0,0,1.75)))<0:normal=-normal
 hairline=1.794+.006*math.sin(root.x*24) if root.y<-.065 else 1.735+.028*abs(root.x)/.12
 if rng.random()>max(0,min(1,(root.z-hairline)/.014)):continue
 short=abs(root.x)>.072 or root.z<1.79
 length=(.004+.004*rng.random())if short else(.020+.012*rng.random())
 growth=(normal*.4+Vector((-.14,.8,.22))).normalized();width_axis=growth.cross(normal)
 if width_axis.length<.001:width_axis=Vector((1,0,0))
 width_axis.normalize();base=len(vs)
 for j in range(6):
  t=j/5;center=root+normal*(.0006+length*t*.35)+growth*(length*t*.75);width=.00017*(1-t*.92)
  vs.extend([tuple(center-width_axis*width),tuple(center+width_axis*width)])
 for j in range(5):k=base+j*2;fs.append((k,k+1,k+3,k+2))
hair_obj=mesh('Hair',vs,fs,hair,rigid='Head',thick=0,subdiv=0)
# Paint a soft anatomical root fade into the actual skin UVs, avoiding a faceted scalp shell.
import numpy as np
image=bpy.data.images.load(str(tex/'Skin_Albedo.jpg'),check_existing=True);w,h=image.size;pixels=np.empty(w*h*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape((h,w,4));uv=body.data.uv_layers.active
for f in body.data.polygons:
 ids=list(f.vertices);pp=[body.matrix_world@body.data.vertices[i].co for i in ids]
 if max(q.z for q in pp)<1.72 or min(abs(q.x)for q in pp)>.13:continue
 lu=[uv.data[i].uv.copy()for i in f.loop_indices]
 for j in range(1,len(ids)-1):
  pos=np.array([pp[0],pp[j],pp[j+1]]);tt=np.array([lu[0],lu[j],lu[j+1]])*np.array([w,h]);lo=np.maximum(0,np.floor(tt.min(0)).astype(int));hi=np.minimum([w-1,h-1],np.ceil(tt.max(0)).astype(int))
  if (hi<lo).any():continue
  yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];v0=tt[1]-tt[0];v1=tt[2]-tt[0];den=v0[0]*v1[1]-v1[0]*v0[1]
  if abs(den)<1e-6:continue
  dx=xx+.5-tt[0,0];dy=yy+.5-tt[0,1];u=(dx*v1[1]-v1[0]*dy)/den;v=(v0[0]*dy-dx*v0[1])/den;inside=(u>=0)&(v>=0)&(u+v<=1)
  xyz=pos[0]+u[:,:,None]*(pos[1]-pos[0])+v[:,:,None]*(pos[2]-pos[0]);line=np.where(xyz[:,:,1]<-.065,1.794+.006*np.sin(xyz[:,:,0]*24),1.735+.028*np.abs(xyz[:,:,0])/.12);coverage=np.clip((xyz[:,:,2]-line)/.014,0,1)*inside
  factor=1-coverage*.93;patch=pixels[lo[1]:hi[1]+1,lo[0]:hi[0]+1,:3];patch*=factor[:,:,None]
paint=bpy.data.images.new('H6 scalp-painted skin',width=w,height=h,alpha=True);paint.pixels.foreach_set(pixels.ravel());paint.filepath_raw=str(tex/'Skin_Albedo_Painted.png');paint.file_format='PNG';paint.save()
lineage=json.loads((tex/'texture-lineage.json').read_text());lineage['textures']=[r for r in lineage['textures']if r['file']!='Skin_Albedo_Painted.png'];lineage['textures'].append({'file':'Skin_Albedo_Painted.png','source':'Skin_Albedo.jpg','source_sha256':hashlib.sha256((tex/'Skin_Albedo.jpg').read_bytes()).hexdigest(),'creator':'Original anatomical UV scalp-fade painting over CC0 Mindfront skin','license':'CC0-1.0','width':w,'height':h,'sha256':hashlib.sha256((tex/'Skin_Albedo_Painted.png').read_bytes()).hexdigest()});(tex/'texture-lineage.json').write_text(json.dumps(lineage,indent=2)+'\n')
for node in skinmat.node_tree.nodes:
 if node.type=='TEX_IMAGE'and node.image==image:node.image=paint
# Raised collar and visibly layered leather lapels, not a painted neck ring.
loft('Stand collar',[(1.53,.092,.078,-.035),(1.58,.091,.078,-.035),(1.625,.086,.074,-.035)],leather,start=.35,end=2*math.pi-.35,segments=32,rigid='spine_03')
for sign in [-1,1]:
 mesh('Layered lapel '+str(sign),[(sign*.065,-.108,1.57),(sign*.16,-.151,1.45),(sign*.095,-.188,1.26),(sign*.025,-.179,1.38)],[(0,1,2,3)],leather,rigid='spine_03',thick=.004,subdiv=0)
def tube(name,p0,p1,r0,r1,mat,bone,segments=28,levels=8):
 axis=p1-p0;unit=axis.normalized();u=unit.cross(Vector((0,1,0))).normalized();v=unit.cross(u).normalized();vs=[];fs=[]
 for j in range(levels+1):
  t=j/levels;center=p0+axis*t;r=r0*(1-t)+r1*t
  for i in range(segments):
   th=i/segments*2*math.pi;fold=1+.035*math.sin(th*6+t*12);vs.append(tuple(center+(u*math.cos(th)+v*math.sin(th))*r*fold))
 for j in range(levels):
  for i in range(segments):
   k=j*segments+i;n=j*segments+(i+1)%segments;fs.append((k,n,n+segments,k+segments))
 return mesh(name,vs,fs,mat,rigid=bone,thick=.004)
for side in ['l','r']:
 upper=rig.data.bones['upperarm_'+side].head_local;elbow=rig.data.bones['lowerarm_'+side].head_local;wrist=rig.data.bones['hand_'+side].head_local
 sleeve=tube('Coat sleeve '+side,upper, wrist,.087,.049,cloth,None,levels=14);skin(sleeve)
 tube('Forearm fitting '+side,elbow.lerp(wrist,.36),elbow.lerp(wrist,.94),.071,.056,leather,'lowerarm_'+side,levels=5)
 # Three layered curved shoulder lames retain real forearm/aim clearance.
 for layer in range(3):
  center=upper.lerp(elbow,.08+layer*.08);vs=[];fs=[]
  axis=(elbow-upper).normalized();u=Vector((0,-1,0));v=axis.cross(u).normalized()
  for j in range(4):
   t=j/3;pos=center+axis*(t-.5)*.085
   for i in range(17):
    th=-.2+math.pi*1.2*i/16;rad=.100-layer*.005;vs.append(tuple(pos+u*math.cos(th)*rad+v*math.sin(th)*rad))
  for j in range(3):
   for i in range(16):k=j*17+i;fs.append((k,k+1,k+18,k+17))
  mesh('Shoulder articulated lame '+side+str(layer),vs,fs,steel,rigid='upperarm_'+side,thick=.005)
# Contoured blackened armor, bronze borders and a small amethyst Third Eye setting.
for sign in [-1,1]:
 vs=[(sign*.025,-.186,1.43),(sign*.14,-.183,1.45),(sign*.195,-.166,1.39),(sign*.155,-.188,1.27),(sign*.045,-.196,1.235),(sign*.016,-.19,1.31)]
 mesh('Armor panel '+str(sign),vs,[(0,1,2,3,4,5)],steel,rigid='spine_03',thick=.004,subdiv=0)
loft('Utility waist belt',[(.997,.196,.159,-.002),(1.045,.198,.16,-.002)],leather,rigid='pelvis',segments=48,thick=.006)
def sphere(name,center,scale,mat,bone):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=center);o=bpy.context.object;o.name=name;o.scale=scale;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat);skin(o,rigid=bone);newmeshes.append(o);return o
sphere('Bronze eye setting',(0,-.199,1.40),(.020,.007,.031),bronze,'spine_03')
sphere('Restrained amethyst seal',(0,-.209,1.40),(.011,.005,.019),gem,'spine_03')
for sign in [-1,1]:
 for z in [1.08,1.16,1.23]:sphere('Bronze coat fastener',(sign*.04,-.17,z),(.009,.006,.009),bronze,'spine_01')
# Separate sclera, iris, pupil and corneal geometry seated in the actual source eye sockets.
old=bpy.data.objects['Iris'];points=[old.matrix_world@v.co for v in old.data.vertices];centers=[]
for sign in [-1,1]:
 q=[v for v in points if v.x*sign>0];centers.append(Vector((sum(v.x for v in q)/len(q),-.1349,(min(v.z for v in q)+max(v.z for v in q))*.5)))
bpy.data.objects.remove(old,do_unlink=True)
sclera=material('H6 Sclera',(.27,.265,.24),.4)
pupil=material('H6 Pupils',(.001,.0007,.0005),.21)
cornea=material('H6 Cornea',(.98,.98,.98),.035);cp=cornea.node_tree.nodes.get('Principled BSDF');cp.inputs['Transmission Weight'].default_value=0;cp.inputs['Alpha'].default_value=.07;cp.inputs['IOR'].default_value=1.376;cornea.surface_render_method='DITHERED'
# A thin highlight shell uses an explicit transparent/gloss mixture, avoiding an opaque veil.
cornea.node_tree.nodes.clear();cn=cornea.node_tree.nodes;cl=cornea.node_tree.links;co=cn.new('ShaderNodeOutputMaterial');ct=cn.new('ShaderNodeBsdfTransparent');cg=cn.new('ShaderNodeBsdfGlossy');cg.inputs['Roughness'].default_value=.035;cm=cn.new('ShaderNodeMixShader');cm.inputs[0].default_value=.035;cl.new(ct.outputs[0],cm.inputs[1]);cl.new(cg.outputs[0],cm.inputs[2]);cl.new(cm.outputs[0],co.inputs['Surface']);cornea.diffuse_color=(.8,.85,.9,.035)
for side,c in enumerate(centers):
 radius=.0131;sphere('Sclera '+str(side),c,(radius,radius,radius),sclera,'Head')
 for name,rad,mat,depth in [('Iris',.0065,eye,.00015),('Pupil',.0020,pupil,.00030),('Cornea',.0069,cornea,.00035)]:
  vs=[tuple(c+Vector((0,-radius-depth,0)))];fs=[];segments=48;rings=6
  for j in range(1,rings+1):
   rr=rad*j/rings
   for i in range(segments):
    t=i*2*math.pi/segments;vs.append((c.x+rr*math.cos(t),c.y-math.sqrt(radius*radius-rr*rr)-depth,c.z+rr*math.sin(t)))
  for i in range(segments):fs.append((0,1+i,1+(i+1)%segments))
  for j in range(rings-1):
   for i in range(segments):k=1+j*segments+i;n=1+j*segments+(i+1)%segments;fs.append((k,n,n+segments,k+segments))
  o=mesh(name+' '+str(side),vs,fs,mat,rigid='Head',thick=0,subdiv=0)
  uv=o.data.uv_layers.active
  for loop in o.data.loops:
   q=o.data.vertices[loop.vertex_index].co-c;uv.data[loop.index].uv=(.296+q.x/.0065*.112,.292+q.z/.0065*.112)
# Independently authored short facial groom from the actual anatomical surface.
from mathutils.bvhtree import BVHTree
# Use current body indices because hidden-torso removal reindexed the mesh.
headpoints=[body.matrix_world@v.co for v in body.data.vertices];headsurface=BVHTree.FromPolygons(headpoints,[list(f.vertices)for f in body.data.polygons],all_triangles=False)
# Independently authored beard mask from the lawful anatomical face. No third-party beard data.
seeds=[]
for f in body.data.polygons:
 pp=[body.matrix_world@body.data.vertices[i].co for i in f.vertices];center=sum(pp,Vector())/len(pp);x=abs(center.x)
 lower=1.615+.045*(x/.10)**2;upper=1.696+.023*min(1,x/.065)
 mouth=x<.026 and 1.652<center.z<1.673
 nose=x<.026 and center.z>1.694
 if lower<center.z<upper and x<.093 and center.y<-.078 and not mouth and not nose:
  for i in range(1,len(pp)-1):seeds.append((pp[0],pp[i],pp[i+1]))
if not seeds:raise ValueError('No anatomical beard surface')
groom=material('H6 Short groom',(.008,.005,.003),.75);groom.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.16
vs=[];fs=[];rng=random.Random(406062)
for _ in range(14500):
 aa,bb,cc=rng.choice(seeds);u=rng.random();v=rng.random()
 if u+v>1:u=1-u;v=1-v
 near,normal,_,dist=headsurface.find_nearest(aa+(bb-aa)*u+(cc-aa)*v)
 if near is None:continue
 length=.0025+rng.random()*.0030;direction=(normal*.65+Vector((0,0,-.75))).normalized();across=direction.cross(normal)
 if across.length<.001:across=Vector((1,0,0))
 across.normalize();start=len(vs)
 for j in range(3):
  t=j/2;center=near+normal*.00035+direction*length*t;width=.00021*(1-.8*t);vs.extend([tuple(center-across*width),tuple(center+across*width)])
 fs.extend([(start,start+1,start+3,start+2),(start+2,start+3,start+5,start+4)])
mesh('Beard',vs,fs,groom,rigid='Head',thick=0,subdiv=0)
# The same matte fiber response is used by the scalp groom; no white plastic card highlights.
bpy.data.objects['Hair'].data.materials.clear();bpy.data.objects['Hair'].data.materials.append(groom)
# Explicit visual sockets are locators only. Original weapon/contact authority is untouched.
anchors={'hand_r':'hand_r','hand_l':'hand_l','foregrip':'hand_l','muzzle':'hand_r','blade_hilt':'hand_r','blade_tip':'hand_r','head':'Head','chest':'spine_03','fx':'hand_l','synergy':'spine_03'}
for role,bone in anchors.items():
 loc=bpy.data.objects.new('socket_'+role,None);bpy.context.collection.objects.link(loc);loc.parent=rig;loc.parent_type='BONE';loc.parent_bone=bone;loc.location=(0,-rig.data.bones[bone].length,0);loc.empty_display_size=.025
marker=bpy.data.objects.new('review_forward',None);bpy.context.collection.objects.link(marker);marker.parent=rig;marker.location=(0,-.3,1.7073)
# Normalize every final influence; never truncate to four. Do not alter original H3/H5 files.
stats=[]
for obj in [x for x in bpy.data.objects if x.type=='MESH']:
 maxinf=0;maxerr=0
 for vert in obj.data.vertices:
  ws=[(g.group,g.weight)for g in vert.groups if obj.vertex_groups[g.group].name in bones and g.weight>0]
  if not ws:raise ValueError('Missing final skin '+obj.name)
  s=sum(x[1]for x in ws)
  for idx,w in ws:obj.vertex_groups[idx].add([vert.index],w/s,'REPLACE')
  maxinf=max(maxinf,len(ws));maxerr=max(maxerr,abs(s-1))
 if maxinf>8:raise ValueError('Complete skin exceeds reviewed eight-influence reference policy: '+obj.name)
 stats.append({'mesh':obj.name,'vertices':len(obj.data.vertices),'triangles':sum(len(f.vertices)-2 for f in obj.data.polygons),'max_influences':maxinf,'pre_normalize_sum_error':maxerr,'materials':[m.name for m in obj.data.materials]})
# Drop unused import intermediates and materials; keep only the actual lawful final art.
bpy.data.orphans_purge(do_recursive=True)
# Export only the complete visual actor. No light/camera/controller/physics/animation authority is added.
rig.animation_data_clear();bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
for obj in bpy.data.objects:obj.select_set(obj.type in {'MESH','ARMATURE','EMPTY'})
bpy.context.view_layer.objects.active=rig
a.source_dir.mkdir(parents=True,exist_ok=True);blend=a.source_dir/'MalikVeyrH6.blend';fbx=out/'MalikVeyrH6.fbx'
import os
for image in bpy.data.images:
 if image.filepath and (tex/Path(image.filepath).name).exists():image.filepath='//'+os.path.relpath(tex/Path(image.filepath).name,blend.parent)
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True,relative_remap=False)
# Actual mid/far geometry shares the same skeleton and material set. No runtime generation.
originals=[o for o in bpy.data.objects if o.type=='MESH'];lod_receipts=[]
for level in [1,2]:
 for original in originals:
  obj=original.copy();obj.data=original.data.copy();bpy.context.collection.objects.link(obj);obj.name='LOD'+str(level)+'_'+original.name
  active(obj)
  if original.name in ['Hair','Beard']:
   count=12 if original.name=='Hair'else 6;stride=(2 if original.name=='Hair'else 3)if level==1 else(12 if original.name=='Hair'else 20)
   bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if (v.index//count)%stride!=0],context='VERTS');bm.to_mesh(obj.data);bm.free()
  elif original.name not in ['MalikBody','Brows','Lashes'] and not any(original.name.startswith(n)for n in ['Iris','Cornea','Pupil','Sclera']):
   backup=obj.data.copy()
   for m in list(obj.modifiers):
    if m.type=='ARMATURE':obj.modifiers.remove(m)
   mod=obj.modifiers.new('Authored lower-detail export','DECIMATE');mod.ratio=.60 if level==1 else .28;bpy.ops.object.modifier_apply(modifier=mod.name)
   if any(sum(g.weight>0 and obj.vertex_groups[g.group].name in bones for g in v.groups)>8 for v in obj.data.vertices):obj.data=backup
   arm=obj.modifiers.new('Original-compatible visual skin','ARMATURE');arm.object=rig
  for vert in obj.data.vertices:
   weights=[(g.group,g.weight)for g in vert.groups if obj.vertex_groups[g.group].name in bones and g.weight>0];total=sum(x[1]for x in weights)
   if not weights or len(weights)>8:raise ValueError('Invalid actual LOD skin')
   for index,weight in weights:obj.vertex_groups[index].add([vert.index],weight/total,'REPLACE')
for o in originals:o.name='LOD0_'+o.name
material_groups={}
for obj in [x for x in bpy.data.objects if x.type=='MESH']:
 level=obj.name.split('_')[0];material_groups.setdefault((level,obj.data.materials[0].name),[]).append(obj)
for (level,name),objects in material_groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for obj in objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 if len(objects)>1:bpy.ops.object.join()
 obj=bpy.context.object;obj.name=level+'_'+name;active(obj)
 tri=obj.modifiers.new('Runtime tangent-safe triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 lod_receipts.append({'lod':level,'mesh':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.polygons),'material':name})
# Each real LOD is a separate FBX using the same exact skeleton and bind pose.
# This preserves all geometry while keeping each recoverable GitHub transfer bounded.
exports=[]
for level in range(3):
 target=fbx if level==0 else out/('MalikVeyrH6_LOD'+str(level)+'.fbx')
 for obj in bpy.data.objects:obj.select_set(obj.type in {'ARMATURE','EMPTY'}or obj.type=='MESH'and obj.name.startswith('LOD'+str(level)+'_'))
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE',mesh_smooth_type='FACE',use_tspace=False)
 exports.append(target)
(out/'authoring-stats.json').write_text(json.dumps({'source':'Actual CC0 anatomical base and fitted parts, original DCC coat and armor','candidate':'MALIK-H6-REVIEW-01','owner_approval':'NOT GIVEN','runtime_enabled':False,'native_unity':'NOT RUN','authored_animation':'ASSET REQUIRED','bone_count':len(rig.data.bones),'editable_meshes':stats,'runtime_skins':len(material_groups),'lods':lod_receipts,'meshes':[{'mesh':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.polygons),'material':o.data.materials[0].name}for o in bpy.data.objects if o.type=='MESH'],'files':[{'path':x.name,'bytes':x.stat().st_size,'sha256':hashlib.sha256(x.read_bytes()).hexdigest()}for x in [blend]+exports]},indent=2)+'\n')
print('ACTUAL NEW CANDIDATE EXPORTED',sum(x['triangles']for x in stats),'triangles',len(stats),'skins',flush=True)
