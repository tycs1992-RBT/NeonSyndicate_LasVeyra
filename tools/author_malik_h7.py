#!/usr/bin/env python3
"""Refine the actual H6 editable anatomy and costume. Preserve all H6 files and original gameplay."""
import argparse,bisect,hashlib,json,math,os,random,shutil
from pathlib import Path
import bpy,bmesh
from mathutils import Vector
from mathutils.kdtree import KDTree
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();out=root/'Assets/NeonSyndicate/Art/ProductionH7';src=root/'ArtSource/AscensionH7';tex=out/'Textures'
before=root/'ArtSource/AscensionH6/MalikVeyrH6.blend';before_hash=hashlib.sha256(before.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(before))
rig=bpy.data.objects['MalikRig'];body=bpy.data.objects['MalikBody'];under=bpy.data.objects['Fitted leather underlayer'];bones=set(rig.data.bones)
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
rig.animation_data_clear()
for image in bpy.data.images:
 if image.filepath:
  old=Path(bpy.path.abspath(image.filepath));dest=tex/old.name
  if old.is_file():shutil.copyfile(old,dest);image.filepath=str(dest)
# Original skin, eye and costume material families remain distinct and renderer-compatible.
mats={m.name:m for m in bpy.data.materials};cloth=mats['H6 Charcoal wool'];leather=mats['H6 Worn leather'];steel=mats['H6 Blackened steel'];bronze=mats['H6 Aged bronze'];groom=mats['H6 Short groom'];skinmat=mats['H6 Skin']
# Correct the original height-only trouser crop: fingers crossed that plane in the A-pose.
pants=bpy.data.objects['Pants'];bm=bmesh.new();bm.from_mesh(pants.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if abs((pants.matrix_world@v.co).x)>.34 or sum(g.weight for g in pants.data.vertices[v.index].groups if pants.vertex_groups[g.group].name.startswith(('upperarm_','lowerarm_','hand_','thumb_','index_','middle_','ring_','pinky_')))>.1],context='VERTS');bm.to_mesh(pants.data);bm.free()
# Subtle actual anatomical sculpt edits, preserving UV indices and the weighted source rig.
for v in body.data.vertices:
 q=v.co;front=max(0,min(1,(-q.y-.035)/.08))
 if 1.63<q.z<1.72:
  jaw=math.exp(-((q.z-1.665)/.035)**2)*math.exp(-((abs(q.x)-.057)/.027)**2)*front;q.x+=math.copysign(.0025*jaw,q.x)
 if 1.71<q.z<1.78 and abs(q.x)<.065:
  cheek=math.exp(-((q.z-1.739)/.022)**2)*math.exp(-((abs(q.x)-.048)/.020)**2)*front;q.y+=.0015*cheek
  for sign in [-1,1]:
   lid=math.exp(-((q.x-sign*.032)/.015)**2)*math.exp(-((q.z-1.754)/.005)**2)*front
   q.z-=.0009*lid
body.data.update()
# Use the actual fitted shoulder/arm mesh as the pattern, rather than a straight tube over a bent elbow.
for obj in list(bpy.data.objects):
 if obj.name.startswith(('Coat upper','Coat sleeve','Split coat tail','Stand collar','Layered lapel','Armor panel','Utility waist belt')):bpy.data.objects.remove(obj,do_unlink=True)
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def unwrap(o):
 active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
# Nearest anatomical weights are kept complete. Rigid armor is explicitly bound to one original bone.
weights=[];kd=KDTree(len(under.data.vertices))
for v in under.data.vertices:
 kd.insert(under.matrix_world@v.co,v.index);rows=[(under.vertex_groups[g.group].name,g.weight)for g in v.groups if under.vertex_groups[g.group].name in rig.data.bones and g.weight>0];total=sum(w for _,w in rows);weights.append([(n,w/total)for n,w in rows])
kd.balance()
def bind(o,rigid=None,tail=None):
 o.parent=rig
 for g in list(o.vertex_groups):o.vertex_groups.remove(g)
 groups={n:o.vertex_groups.new(name=n)for n in rig.data.bones.keys()}
 for v in o.data.vertices:
  point=o.matrix_world@v.co
  if rigid:rows=[(rigid,1)]
  elif tail:
   t=max(0,min(1,(1.04-point.z)/.55));rows=[('pelvis',1-t*.70),('thigh_'+tail,t*.70)]
  else:rows=weights[kd.find(point)[1]]
  for n,w in rows:
   if w>0:groups[n].add([v.index],w,'REPLACE')
 for m in list(o.modifiers):
  if m.type=='ARMATURE':o.modifiers.remove(m)
 arm=o.modifiers.new('Original compatible weighted visual','ARMATURE');arm.object=rig
 for f in o.data.polygons:f.use_smooth=True
 return o
def mesh(name,vs,fs,mat,rigid=None,tail=None,sub=0,thick=.003):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat);unwrap(o)
 if sub:
  m=o.modifiers.new('Tailored smooth surface','SUBSURF');m.levels=sub;active(o);bpy.ops.object.modifier_apply(modifier=m.name)
 if thick:
  m=o.modifiers.new('Real fabric edge thickness','SOLIDIFY');m.thickness=thick;m.offset=0;active(o);bpy.ops.object.modifier_apply(modifier=m.name)
 return bind(o,rigid,tail)
# Build a clean sewn-volume garment rather than retaining the masked anatomical underlayer.
# Connected shoulder/arm volumes are remeshed in the editable DCC, then weight-transferred.
parts=[]
def loft(name,centers,radii):
 vs=[];fs=[];N=40
 for j,(center,radius)in enumerate(zip(centers,radii)):
  center=Vector(center);forward=(Vector(centers[min(j+1,len(centers)-1)])-Vector(centers[max(j-1,0)])).normalized()
  right=forward.cross(Vector((0,1,0))).normalized();up=right.cross(forward).normalized()
  for i in range(N):
   t=2*math.pi*i/N;vs.append(tuple(center+right*radius[0]*math.cos(t)+up*radius[1]*math.sin(t)))
 for j in range(len(centers)-1):
  for i in range(N):k=j*N+i;n=j*N+(i+1)%N;fs.append((k,n,n+N,k+N))
 fs.extend([tuple(range(N-1,-1,-1)),tuple((len(centers)-1)*N+i for i in range(N))]);o=mesh(name,vs,fs,cloth,thick=0);parts.append(o)
loft('H7 garment torso',[(0,.005,1.005),(0,.008,1.08),(0,.005,1.24),(0,-.004,1.39),(0,-.012,1.48),(0,-.015,1.55),(0,-.018,1.586),(0,-.021,1.625)],[(.229,.183),(.221,.181),(.229,.184),(.245,.181),(.247,.162),(.223,.141),(.155,.096),(.073,.064)])
for side,sign in [('l',1),('r',-1)]:
 shoulder=rig.data.bones['upperarm_'+side].head_local;elbow=rig.data.bones['lowerarm_'+side].head_local;wrist=rig.data.bones['hand_'+side].head_local
 centers=[shoulder-Vector((sign*.035,0,-.020)),shoulder.lerp(elbow,.35),elbow,elbow.lerp(wrist,.55),wrist+Vector((sign*.004,-.004,-.002))]
 loft('H7 garment sleeve '+side,centers,[(.106,.102),(.106,.102),(.087,.086),(.075,.074),(.063,.061)])
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();coat=bpy.context.object;coat.name='H7 tailored connected coat'
for mod in list(coat.modifiers):coat.modifiers.remove(mod)
mod=coat.modifiers.new('Clean sewn shoulder topology','REMESH');mod.mode='VOXEL';mod.voxel_size=.006;mod.use_smooth_shade=True;active(coat);bpy.ops.object.modifier_apply(modifier=mod.name)
mod=coat.modifiers.new('Relax tailored surface','SMOOTH');mod.factor=.65;mod.iterations=3;bpy.ops.object.modifier_apply(modifier=mod.name)
bm=bmesh.new();bm.from_mesh(coat.data);remove=[]
for f in bm.faces:
 c=f.calc_center_median()
 if c.z<1.011 or c.z>1.617:remove.append(f);continue
 for side in ['l','r']:
  wrist=rig.data.bones['hand_'+side].head_local;elbow=rig.data.bones['lowerarm_'+side].head_local;direction=(wrist-elbow).normalized()
  if(c-wrist).dot(direction)>-.004 and(c-wrist).length<.095:remove.append(f);break
bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(coat.data);bm.free();unwrap(coat);bind(coat)
# This original fitted intermediate is not a second visible garment or exported mesh.
bpy.data.objects.remove(under,do_unlink=True)
# A collar follows the actual neck centre and lowers in front, rather than a tall intersecting cylinder.
vs=[];fs=[];N=64
for j in range(4):
 for i in range(N+1):
  t=.27+(2*math.pi-.54)*i/N;front=(math.cos(t)+1)*.5;z0=1.586+.010*front;z=z0+j/3*(.050-.015*front);rx=.073-j*.001;ry=.066-j*.001
  vs.append((rx*math.sin(t),-.017-ry*math.cos(t),z))
for j in range(3):
 for i in range(N):k=j*(N+1)+i;fs.append((k,k+1,k+N+2,k+N+1))
mesh('H7 faced stand collar',vs,fs,leather,rigid='neck_01',sub=1,thick=.004)
# Tailored lower panels have a narrower silhouette, weighted walking vent, side seams and rounded hem.
for side,start,end in [('l',.075,math.pi-.09),('r',math.pi+.09,2*math.pi-.075)]:
 vs=[];fs=[];N=48;levels=22
 for j in range(levels+1):
  t=j/levels;z=1.025-.57*t;rx=.242+.032*t;ry=.187+.012*t
  for i in range(N+1):
   angle=start+(end-start)*i/N;fold=.0035*math.sin(angle*9+.5)*t*t;hem=.010*math.sin(angle*2)**2*t**5
   vs.append(((rx+fold)*math.sin(angle),.008-(ry+fold)*math.cos(angle),z+hem))
 for j in range(levels):
  for i in range(N):k=j*(N+1)+i;fs.append((k,k+1,k+N+2,k+N+1))
 mesh('H7 split walking panel '+side,vs,fs,cloth,tail=side,sub=1,thick=.004)
# Actual lapel/facing geometry and a fitted asymmetrical curved cuirass.
for sign in [-1,1]:
 mesh('H7 folded lapel '+str(sign),[(sign*.042,-.085,1.59),(sign*.122,-.170,1.48),(sign*.092,-.199,1.32),(sign*.019,-.198,1.43)],[(0,1,2,3)],leather,rigid='spine_03',sub=0,thick=.005)
 vs=[];fs=[];N=14
 for j in range(6):
  u=j/5;z=1.445-.205*u
  for i in range(N+1):
   theta=.10+i/N*(.91 if sign>0 else .78);rx=.215-.032*u;ry=.191+.009*math.sin(u*math.pi);x=sign*rx*math.sin(theta);y=.004-ry*math.cos(theta)-.006
   vs.append((x,y,z+.018*math.sin(theta)*u))
 for j in range(5):
  for i in range(N):k=j*(N+1)+i;fs.append((k,k+1,k+N+2,k+N+1))
 mesh('H7 contoured chest plate '+str(sign),vs,fs,steel,rigid='spine_03',sub=1,thick=.004)
# Physically modelled piping and seams, kept understated rather than emissive trim.
def seam(name,points,mat,radius=.0012,rigid=None,tail=None):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=4;curve.bevel_depth=radius;curve.bevel_resolution=1;s=curve.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for v,point in zip(s.bezier_points,points):v.co=point;v.handle_left_type='AUTO';v.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);o.data.materials.append(mat);active(o);bpy.ops.object.convert(target='MESH');bind(o,rigid,tail);return o
for sign in [-1,1]:
 seam('H7 side tailoring seam '+str(sign),[(sign*.18,.018,1.02),(sign*.217,.018,.79),(sign*.251,.018,.465)],leather,.0018,tail='l'if sign>0 else'r')
 seam('H7 front armor edge '+str(sign),[(sign*.021,-.155,1.444),(sign*.029,-.163,1.35),(sign*.021,-.153,1.243)],bronze,.0013,'spine_03')
# A real belt sits on the waist rather than floating over the old tube coat.
vs=[];fs=[];N=64
for z in [1.00,1.035]:
 for i in range(N):t=2*math.pi*i/N;vs.append((.244*math.sin(t),.008-.191*math.cos(t),z))
for i in range(N):j=(i+1)%N;fs.append((i,j,j+N,i+N))
mesh('H7 anchored belt',vs,fs,leather,rigid='pelvis',thick=.004)
# Move original fasteners/setting onto the rebuilt front, retaining the established amethyst identity.
for o in list(bpy.data.objects):
 if o.name.startswith('Shoulder articulated lame'):
  bpy.data.objects.remove(o,do_unlink=True);continue
 if o.name.startswith(('Bronze eye setting','Restrained amethyst seal')):o.location.y+=.010
 if o.name.startswith('Bronze coat fastener'):o.location.y-=.015
# Three curved, overlapping armor lames follow each actual shoulder axis.
for side in ['l','r']:
 shoulder=rig.data.bones['upperarm_'+side].head_local;elbow=rig.data.bones['lowerarm_'+side].head_local;axis=(elbow-shoulder).normalized();u=Vector((0,-1,0));v=axis.cross(u).normalized();v=v if v.z>0 else-v
 for j in range(3):
  vs=[];fs=[];N=20
  for row in range(2):
   center=shoulder+axis*(.004+.035*j+.034*row)
   for i in range(N+1):
    t=.08+(math.pi-.16)*i/N;r=.128-.003*j;vs.append(tuple(center+u*math.cos(t)*r+v*math.sin(t)*(r+.012)))
  for i in range(N):fs.append((i,i+1,i+N+2,i+N+1))
  mesh('H7 shoulder armor '+side+str(j),vs,fs,steel,rigid='upperarm_'+side,sub=1,thick=.004)
# Coherent area-weighted grooms follow the actual surface and swept undercut flow.
for name in ['Hair','Beard']:bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
from mathutils.bvhtree import BVHTree
points=[body.matrix_world@v.co for v in body.data.vertices];surface=BVHTree.FromPolygons(points,[list(f.vertices)for f in body.data.polygons]);triangles=[]
for f in body.data.polygons:
 pp=[points[i]for i in f.vertices]
 for j in range(1,len(pp)-1):triangles.append((pp[0],pp[j],pp[j+1]))
def groom_mesh(name,count,eligible,kind):
 rng=random.Random(40701 if kind=='hair'else 40702);eligible_tri=[];areas=[];total=0
 for tri in triangles:
  c=sum(tri,Vector())/3
  if eligible(c):
   area=(tri[1]-tri[0]).cross(tri[2]-tri[0]).length*.5
   if area>1e-10:total+=area;areas.append(total);eligible_tri.append(tri)
 vs=[];fs=[]
 for unused in range(count):
  aa,bb,cc=eligible_tri[bisect.bisect_left(areas,rng.random()*total)];u=rng.random();v=rng.random()
  if u+v>1:u=1-u;v=1-v
  root=aa+(bb-aa)*u+(cc-aa)*v;normal=(bb-aa).cross(cc-aa).normalized();normal=normal if normal.dot(root-Vector((0,0,1.75)))>0 else-normal
  if kind=='hair':
   line=1.794+.005*math.sin(root.x*25) if root.y<-.065 else 1.735+.028*abs(root.x)/.12
   if rng.random()>max(0,min(1,(root.z-line)/.014)):continue
   long=root.z>1.79 and abs(root.x)<.076;length=(.027+.014*rng.random())if long else .006+.004*rng.random();flow=Vector((-.10,1,-.12));width=.00032 if unused%3 else .00070
  else:
   x=abs(root.x);lo=1.616+.035*(x/.10)**2;hi=1.700+.019*min(1,x/.07);edge=min((root.z-lo)/.009,(hi-root.z)/.009,(.092-x)/.008)
   if rng.random()>max(0,min(1,edge)):continue
   length=.0018+.0018*rng.random();flow=Vector((0,.15,-1));width=.00013
  flow=(flow-normal*flow.dot(normal)).normalized();across=normal.cross(flow).normalized();base=len(vs);levels=5 if kind=='hair'else 2
  for j in range(levels+1):
   t=j/levels;desired=root+flow*length*t;near,n,idx,dist=surface.find_nearest(desired)
   if near is None:near=root;n=normal
   center=near+n*(.00035+(math.sin(math.pi*t)*.0038 if kind=='hair'else .00035*t));w=width*(1-t*.92)
   vs.extend([tuple(center-across*w),tuple(center+across*w)])
  for j in range(levels):k=base+2*j;fs.append((k,k+1,k+3,k+2))
 o=mesh(name,vs,fs,groom,rigid='Head',thick=0);o['h7_strand_vertices']=12 if kind=='hair'else 6
 return o
hair=groom_mesh('H7 swept undercut',6500,lambda c:abs(c.x)<.12 and(c.z>1.795 or c.z>1.72 and c.y>-.025),'hair')
beard=groom_mesh('H7 neat beard',17500,lambda c:1.615<c.z<1.725 and abs(c.x)<.094 and c.y<-.078 and not(abs(c.x)<.026 and 1.651<c.z<1.675)and not(abs(c.x)<.027 and c.z>1.694),'beard')
# Refine actual roughness and hair response; original source texture rights stay traceable.
groom.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.83;groom.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.10
skinmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.59
for n in skinmat.node_tree.nodes:
 if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.28
steel.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.47
# Portable editable source, no dependency on the original authoring extension or runtime generation.
src.mkdir(parents=True,exist_ok=True)
for image in bpy.data.images:
 if image.filepath and Path(image.filepath).is_file():image.filepath='//'+os.path.relpath(Path(image.filepath),src)
for o in bpy.data.objects:
 if o.type=='MESH':
  for vert in o.data.vertices:
   rows=[g for g in vert.groups if o.vertex_groups[g.group].name in rig.data.bones and g.weight>0];total=sum(g.weight for g in rows)
   if not rows or len(rows)>8:raise ValueError('Unweighted or excessive source influence: '+o.name)
   for g in rows:o.vertex_groups[g.group].add([vert.index],g.weight/total,'REPLACE')
rig.animation_data_clear();bpy.context.scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(src/'MalikVeyrH7.blend'),compress=True,relative_remap=False)
record={'scope':'Actual H6-source DCC mesh/costume/groom refinement, no native or appearance approval','h6_source_sha256':before_hash,'h7_source_sha256':hashlib.sha256((src/'MalikVeyrH7.blend').read_bytes()).hexdigest(),'bones':len(rig.data.bones),'objects':[{'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons)}for o in bpy.data.objects if o.type=='MESH'],'owner_approval':'NOT GIVEN','native_unity':'NOT RUN'}
(src/'art-source.json').write_text(json.dumps(record,indent=2)+'\n');print('H7 ACTUAL MODEL SAVED',record['h7_source_sha256'],flush=True)
