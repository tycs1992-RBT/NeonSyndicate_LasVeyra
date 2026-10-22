#!/usr/bin/env python3
"""Correct observed boot coverage and resting coat penetration on the actual selected anatomy."""
import bpy,bmesh,math,json,argparse
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1];p=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';bpy.ops.wm.open_mainfile(filepath=str(p),load_ui=False,use_scripts=False);rig=bpy.data.objects['MalikRig'];body=bpy.data.objects['MalikBodyH8R'];rig.animation_data_clear()
for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=(1,0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
args=argparse.ArgumentParser();args.add_argument('--panel-source',type=Path);options=args.parse_args()
if options.panel_source:
 for obj in list(bpy.data.objects):
  if 'split walking panel'in obj.name:bpy.data.objects.remove(obj,do_unlink=True)
 with bpy.data.libraries.load(str(options.panel_source.resolve()),link=False)as (available,loaded):loaded.objects=[n for n in available.objects if 'split walking panel'in n]
 for obj in loaded.objects:
  previous=obj.parent;obj.data.materials.clear();obj.data.materials.append(bpy.data.materials['H6 Charcoal wool']);bpy.context.scene.collection.objects.link(obj);obj.parent=rig;obj.matrix_world=Matrix.Identity(4)
  for mod in obj.modifiers:
   if mod.type=='ARMATURE':mod.object=rig
  if previous and previous!=rig and previous.name in bpy.data.objects:bpy.data.objects.remove(previous,do_unlink=True)
# Hand/finger geometry at hip height is deliberately excluded from the garment envelope.
faces=[list(f.vertices)for f in body.data.polygons if all(abs(body.data.vertices[i].co.x)<.36 for i in f.vertices)]
surface=BVHTree.FromPolygons([v.co for v in body.data.vertices],faces);moved=0
for o in bpy.data.objects:
 if 'split walking panel'not in o.name:continue
 # Retain one outward cloth surface before fitting. Clamping two shell layers causes depth fighting.
 bm=bmesh.new();bm.from_mesh(o.data);bm.normal_update();remove=[]
 for face in bm.faces:
  c=face.calc_center_median();outward=Vector((c.x,c.y-.005,0))
  if face.normal.dot(outward)<-.00001:remove.append(face)
 bmesh.ops.delete(bm,geom=remove,context='FACES');loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00005);bm.to_mesh(o.data);bm.free()
 for v in o.data.vertices:
  direction=Vector((v.co.x,v.co.y-.005,0)).normalized();origin=Vector((0,.005,v.co.z))+direction*1.5;hit=surface.ray_cast(origin,-direction,2)[0]
  if hit is not None:
   center=Vector((0,.005,v.co.z));needed=(hit-center).dot(direction)+.035;distance=(v.co-center).dot(direction)
   if distance<needed:v.co+=direction*(needed-distance);moved+=1
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 thickness=o.modifiers.new('Restored cloth thickness after fit','SOLIDIFY');thickness.thickness=.004;thickness.offset=0;bpy.ops.object.modifier_apply(modifier=thickness.name)
old=bpy.data.objects.get('H8R fitted riding boots')
if old:bpy.data.objects.remove(old,do_unlink=True)
material=next(m for m in bpy.data.materials if m.name=='H6 Worn leather');vs=[];faces=[];weights=[];N=40
for side,sign in [('l',1),('r',-1)]:
 start=len(vs);levels=[-.005,.012,.035,.06,.09,.13,.19,.26,.34]
 for j,z in enumerate(levels):
  points=[v.co for v in body.data.vertices if v.co.x*sign>.07 and abs(v.co.z-max(z,.01))<(.018 if z<.10 else .025)]
  if len(points)<4:raise ValueError('Anatomical boot section missing')
  xmin=min(q.x for q in points);xmax=max(q.x for q in points);ymin=min(q.y for q in points);ymax=max(q.y for q in points);cx=(xmin+xmax)*.5;cy=(ymin+ymax)*.5;rx=(xmax-xmin)*.5+.014;ry=(ymax-ymin)*.5+.014
  if j==0 or j==1:rx=max(rx,.069);ry=max(ry,.12);cy=-.075
  for i in range(N):
   angle=i*2*math.pi/N;power=.5 if z<.095 else 1;co=math.cos(angle);si=math.sin(angle);vs.append((cx+rx*math.copysign(abs(co)**power,co),cy+ry*math.copysign(abs(si)**power,si),z));blend=max(0,min(1,(z-.08)/.12));weights.append([(side,'foot',1-blend),(side,'calf',blend)])
  if j:
   for i in range(N):a=start+(j-1)*N+i;b=start+(j-1)*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 faces.append(tuple(start+i for i in range(N-1,-1,-1)))
d=bpy.data.meshes.new('Anatomically fitted closed boot volumes');d.from_pydata(vs,[],faces);d.update();o=bpy.data.objects.new('H8R fitted riding boots',d);bpy.context.collection.objects.link(o);o.data.materials.append(material);o.parent=rig;groups={}
for side in ['l','r']:
 for bone in ['foot','calf']:groups[(side,bone)]=o.vertex_groups.new(name=bone+'_'+side)
for i,row in enumerate(weights):
 for side,bone,w in row:
  if w>0:groups[(side,bone)].add([i],w,'REPLACE')
for f in d.polygons:f.use_smooth=True
mod=o.modifiers.new('Complete original lower-leg skin','ARMATURE');mod.object=rig
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(p),compress=True,relative_remap=False)
(root/'ArtSource/AscensionH8R/garment-fit.json').write_text(json.dumps({'observed_issue':'Incomplete remesh boot coverage and resting thigh penetration in actual source renders','correction':'Closed body-section boot volumes with continuous calf/foot weights; coat projected outside actual body by 35mm','corrected_panel_vertices':moved,'boot_vertices':len(vs),'cloth_simulation':False,'native_unity':'NOT RUN','deep_pose_contact':'Still requires native review'},indent=2)+'\n');print('Corrected coat vertices',moved,'boot vertices',len(vs),flush=True)
