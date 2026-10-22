#!/usr/bin/env python3
"""Fit the versioned original wardrobe to the owner's selected CC0 human. No gameplay changes."""
import argparse,hashlib,json,math,shutil
from pathlib import Path
import bpy
from mathutils import Vector,Matrix
from mathutils.kdtree import KDTree
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();out=root/'Assets/NeonSyndicate/Art/ProductionH8R';src=root/'ArtSource/AscensionH8R';tex=out/'Textures';tex.mkdir(parents=True,exist_ok=True);src.mkdir(parents=True,exist_ok=True)
expected='a69c2a5d916a000990d7f8e4ab8b51519a3b877a8e3a5622b5ddb6c125c523f6'
if hashlib.sha256(a.source.read_bytes()).hexdigest()!=expected:raise ValueError('This recipe is fitted to the inspected owner source only.')
bpy.ops.wm.open_mainfile(filepath=str(a.source.resolve()),load_ui=False,use_scripts=False)
body=bpy.data.objects['Human'];rig=bpy.data.objects['Human.rig'];eye_ref=bpy.data.objects['Human.low-poly'];eye_points=[eye_ref.matrix_world@v.co for v in eye_ref.data.vertices]
# Keep the selected face, body and 53-bone source. Exclude duplicate rig, masked body and unverified accessories.
for o in list(bpy.data.objects):
 if o not in [body,rig]:bpy.data.objects.remove(o,do_unlink=True)
for m in list(body.modifiers):
 if m.type=='ARMATURE' or m.name!='Hide helpers':body.modifiers.remove(m)
bpy.context.view_layer.objects.active=body;body.select_set(True);bpy.ops.object.shape_key_remove(all=True,apply_mix=True)
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
active(body)
for m in list(body.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)
# Actual source height is 2.43m. Normalize this visual alone to 1.86m, never world/gameplay coordinates.
minz=min(v.co.z for v in body.data.vertices);maxz=max(v.co.z for v in body.data.vertices);scale=1.86/(maxz-minz)
for v in body.data.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-minz)*scale))
active(rig);bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones:
 b.head=Vector((b.head.x*scale,b.head.y*scale,(b.head.z-minz)*scale));b.tail=Vector((b.tail.x*scale,b.tail.y*scale,(b.tail.z-minz)*scale))
rig.data.edit_bones['Root'].name='root';rig.data.edit_bones['head'].name='Head';bpy.ops.object.mode_set(mode='OBJECT')
if body.vertex_groups.get('Root'):body.vertex_groups['Root'].name='root'
if body.vertex_groups.get('head'):body.vertex_groups['head'].name='Head'
body.name='MalikBodyH8R';rig.name='MalikRig';rig.data.name='Selected53BoneVisual';body.data.name='SelectedOwnerHuman';rig.animation_data_clear()
for v in body.data.vertices:
 rows=[(g.group,g.weight)for g in v.groups if body.vertex_groups[g.group].name in rig.data.bones and g.weight>0];total=sum(w for _,w in rows)
 if total<=0:raise ValueError('Unweighted selected human vertex '+str(v.index))
 for i,w in rows:body.vertex_groups[i].add([v.index],w/total,'REPLACE')
for g in list(body.vertex_groups):
 if g.name not in rig.data.bones:body.vertex_groups.remove(g)
for poly in body.data.polygons:poly.use_smooth=True
arm=body.modifiers.new('Original-compatible full skin','ARMATURE');arm.object=rig;body.parent=rig
# Only the documented CC0 skin images are retained from the packed upload.
images={}
for original,new in [('skin_male_african_middleage.png','Skin_Albedo.png'),('skin_male_african_middleage_NRM.png','Skin_Normal.png')]:
 im=bpy.data.images[original];im.filepath_raw=str(tex/new);im.file_format='PNG';im.save();im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+new;images[new]=im
skin=bpy.data.materials.new('H8R Skin');skin.use_nodes=True;nodes=skin.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.53;bs.inputs['Subsurface Weight'].default_value=.07
for name,socket in [('Skin_Albedo.png','Base Color'),('Skin_Normal.png','Normal')]:
 n=nodes.new('ShaderNodeTexImage');n.image=images[name]
 if socket=='Normal':n.image.colorspace_settings.name='Non-Color';normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.25;skin.node_tree.links.new(n.outputs['Color'],normal.inputs['Color']);skin.node_tree.links.new(normal.outputs['Normal'],bs.inputs['Normal'])
 else:skin.node_tree.links.new(n.outputs['Color'],bs.inputs[socket])
body.data.materials.clear();body.data.materials.append(skin)
# Read actual H8 wardrobe, sockets, groom, motions and CC0 textures. Old body/face stays in its own source.
oldfile=root/'ArtSource/AscensionH8/MalikVeyrH8.blend'
with bpy.data.libraries.load(str(oldfile),link=False) as (available,loaded):loaded.objects=available.objects;loaded.actions=available.actions
oldrig=next(o for o in loaded.objects if o and o.type=='ARMATURE');oldbones={b.name:b.matrix_local.copy()for b in oldrig.data.bones};newbones={b.name:b.matrix_local.copy()for b in rig.data.bones};oldhead=oldrig.data.bones['Head'].head_local.copy();newhead=rig.data.bones['Head'].head_local.copy()
wardrobe=[];excluded=[]
for o in loaded.objects:
 if not o:continue
 if o.type=='MESH' and o.name.split('.')[0] not in ['MalikBody','Pants','Boots','Bronze eye setting','Third Eye stone']:
  # Existing original eye parts are remapped below to the actual selected sockets.
  bpy.context.scene.collection.objects.link(o);wardrobe.append(o)
 elif o.type=='EMPTY':bpy.context.scene.collection.objects.link(o)
 elif o!=oldrig:excluded.append(o)
bpy.context.scene.collection.objects.link(oldrig);bpy.context.view_layer.update()
# Complete-influence affine bind transfer fits actual cloth to the new anatomical bone frames.
for o in wardrobe:
 for mod in list(o.modifiers):o.modifiers.remove(mod)
 oldmat=o.matrix_world.copy();o.parent=None;o.matrix_world=Matrix.Identity(4)
 groups={g.index:g.name for g in o.vertex_groups};is_eye=o.name.startswith(('Iris','Sclera','Pupil','Cornea','Lashes','Brows'));is_groom='groom'in o.name.lower()or 'beard'in o.name.lower()or 'undercut'in o.name.lower()
 for v in o.data.vertices:
  point=oldmat@v.co;rows=[(groups[g.group],g.weight)for g in v.groups if groups[g.group]in oldbones and groups[g.group]in newbones and g.weight>0];total=sum(w for _,w in rows)
  if not rows:raise ValueError('Unweighted inherited garment '+o.name)
  pos=Vector((0,0,0))
  for n,w in rows:pos+=(newbones[n]@oldbones[n].inverted()@point)*(w/total)
  v.co=pos
 o.parent=rig;mod=o.modifiers.new('Selected human complete skin','ARMATURE');mod.object=rig
 for v in o.data.vertices:
  rows=[(g.group,g.weight)for g in v.groups if groups[g.group]in rig.data.bones and g.weight>0];total=sum(w for _,w in rows)
  for i,w in rows:o.vertex_groups[i].add([v.index],w/total,'REPLACE')
 for poly in o.data.polygons:poly.use_smooth=True
# Tailor the inherited coat around the selected torso instead of retaining its broad prototype cut.
for o in wardrobe:
 if 'tailored connected coat' in o.name:
  for v in o.data.vertices:
   q=v.co;t=max(0,min(1,(.40-abs(q.x))/.18));q.x*=1-.15*t;q.y*=.91
 if 'split walking panel' in o.name:
  for v in o.data.vertices:
   q=v.co;t=max(0,min(1,(1.02-q.z)/.60));q.x*=.84+.12*t;q.y*=.90+.10*t
# Rebuild optical surfaces to the selected eye socket dimensions. Existing H7 eyes were too large.
for o in list(wardrobe):
 if o.name.startswith(('Iris','Sclera','Pupil','Cornea','Lashes')):wardrobe.remove(o);bpy.data.objects.remove(o,do_unlink=True)
import numpy as np
N=512;yy,xx=np.mgrid[0:N,0:N];dx=(xx-N/2)/(N/2);dy=(yy-N/2)/(N/2);rr=np.sqrt(dx*dx+dy*dy);angle=np.arctan2(dy,dx);fiber=(np.sin(angle*127+rr*35)+np.sin(angle*67-rr*52))*.035
pix=np.ones((N,N,4),dtype=np.float32);mask=np.clip(1-(rr-.76)*3,0,1);pix[:,:,0]=(.19+fiber)*mask;pix[:,:,1]=(.074+fiber*.45)*mask;pix[:,:,2]=(.024+fiber*.22)*mask
im=bpy.data.images.new('H8R natural brown iris',width=N,height=N);im.pixels.foreach_set(pix.ravel());im.filepath_raw=str(tex/'Eyes_Albedo.png');im.file_format='PNG';im.save();im.pack()
irisMat=next(m for m in bpy.data.materials if m.name.startswith('H6 Eyes'))
for n in irisMat.node_tree.nodes:
 if n.type=='TEX_IMAGE':n.image=im
sclera=next(m for m in bpy.data.materials if m.name.startswith('H6 Sclera'));sclera.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.46,.41,.36,1)
def eyeSkin(obj):
 obj.parent=rig;group=obj.vertex_groups.new(name='Head');group.add(list(range(len(obj.data.vertices))),1,'REPLACE');mod=obj.modifiers.new('Selected head skin','ARMATURE');mod.object=rig
 for f in obj.data.polygons:f.use_smooth=True
 wardrobe.append(obj)
for index,sign in [(0,1),(1,-1)]:
 points=[q for q in eye_points if q.x*sign>0];center=sum(points,Vector())/len(points);center=Vector((center.x*scale,center.y*scale+.010,(center.z-minz)*scale-.0018));radius=.0132
 bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,radius=radius,location=center);o=bpy.context.object;active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.name='Sclera '+str(index);o.data.materials.append(sclera);eyeSkin(o)
 for name,r,mat,offset in [('Iris',.0059,irisMat,.00025),('Pupil',.0023,next(m for m in bpy.data.materials if m.name.startswith('H6 Pupils')),.00042),('Cornea',.0062,next(m for m in bpy.data.materials if m.name.startswith('H6 Cornea')),.00065)]:
  vs=[tuple(center+Vector((0,-radius-offset,0)))];uv=[(.5,.5)];fs=[];rings=10;segments=48
  for j in range(1,rings+1):
   rad=r*j/rings
   for i in range(segments):
    t=2*math.pi*i/segments;x=rad*math.cos(t);z=rad*math.sin(t);vs.append(tuple(center+Vector((x,-math.sqrt(radius*radius-rad*rad)-offset,z))));uv.append((.5+x/(r*2),.5+z/(r*2)))
  for i in range(segments):fs.append((0,1+(i+1)%segments,1+i))
  for j in range(rings-1):
   for i in range(segments):fa=1+j*segments+i;fb=1+j*segments+(i+1)%segments;fs.append((fa,fb,fb+segments,fa+segments))
  data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name+' '+str(index),data);bpy.context.collection.objects.link(o);data.materials.append(mat);layer=data.uv_layers.new(name='OpticalUV')
  for f in data.polygons:
   for li in f.loop_indices:layer.data[li].uv=uv[data.loops[li].vertex_index]
  eyeSkin(o)
# Cover the selected anatomical feet with fitted, continuous leather boots, not the old shoe proxy.
import bmesh
leather=next(m for m in bpy.data.materials if m.name.startswith('H6 Worn leather'))
boots=body.copy();boots.data=body.data.copy();bpy.context.scene.collection.objects.link(boots);boots.name='H8R fitted riding boots';boots.data.materials.clear();boots.data.materials.append(leather)
bm=bmesh.new();bm.from_mesh(boots.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>.36],context='VERTS');bm.to_mesh(boots.data);bm.free()
for v in boots.data.vertices:
 v.co+=v.normal*.008
 if v.co.z<.018:v.co.z=-.004
# Merge the anatomical toe divisions into a smooth practical toe box and rebind complete source weights.
for m in list(boots.modifiers):boots.modifiers.remove(m)
active(boots);mod=boots.modifiers.new('Joined leather toe volume','REMESH');mod.mode='VOXEL';mod.voxel_size=.007;mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
for g in list(boots.vertex_groups):boots.vertex_groups.remove(g)
groups={b:boots.vertex_groups.new(name=b)for b in rig.data.bones.keys()}
bootKd=KDTree(len(body.data.vertices))
for v in body.data.vertices:bootKd.insert(v.co,v.index)
bootKd.balance()
for v in boots.data.vertices:
 srcVertex=body.data.vertices[bootKd.find(v.co)[1]]
 for g in srcVertex.groups:groups[body.vertex_groups[g.group].name].add([v.index],g.weight,'REPLACE')
mod=boots.modifiers.new('Complete selected boot skin','ARMATURE');mod.object=rig
wardrobe.append(boots)
# Fit the restrained eye crystal directly to the new forehead, with original head weights.
for o in wardrobe:
 if 'Third Eye' in o.name or 'eye setting' in o.name:
  center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices);target=Vector((0,-.101,1.797))
  for v in o.data.vertices:v.co+=target-center
# Fit old garment clearance to selected body: same body weights, physical outward offset, no body reshaping.
kd=KDTree(len(body.data.vertices))
for v in body.data.vertices:kd.insert(v.co,v.index)
kd.balance();corrected=0
for o in wardrobe:
 if any(t in o.name.lower()for t in ['eye','iris','cornea','pupil','sclera','lash','brow','beard','undercut','groom']):continue
 for v in o.data.vertices:
  nearest,index,d=kd.find(v.co);normal=body.data.vertices[index].normal
  if normal.length<.5:continue
  signed=(v.co-nearest).dot(normal)
  clearance=.026 if 'walking panel' in o.name else .012
  if d<.075 and signed<clearance:v.co+=normal*(clearance-signed);corrected+=1
# Retain original socket transforms on their corresponding new visual bones.
for o in [o for o in bpy.data.objects if o.type=='EMPTY']:
 parentname=o.parent_bone if o.parent_type=='BONE' else None
 if parentname and parentname in oldbones and parentname in newbones:
  oldworld=o.matrix_world.copy();o.parent=None;o.matrix_world=newbones[parentname]@oldbones[parentname].inverted()@oldworld;o.parent=rig;o.parent_type='BONE';o.parent_bone=parentname;o.matrix_world=o.matrix_world.copy()
 else:
  # review_forward is an orientation locator only.
  o.parent=rig;o.location=(0,-.35,1.65)
# New fitted leather leg shell uses the selected anatomy, preserving toes and knee form.
import bmesh
pants=body.copy();pants.data=body.data.copy();bpy.context.scene.collection.objects.link(pants);pants.name='H8R fitted leather trousers';pants.data.materials.clear();pants.data.materials.append(next(m for m in bpy.data.materials if m.name.startswith('H6 Worn leather')))
bm=bmesh.new();bm.from_mesh(pants.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>1.04 or v.co.z<.20 or abs(v.co.x)>.36],context='VERTS');bm.to_mesh(pants.data);bm.free()
for v in pants.data.vertices:v.co+=v.normal*.007
wardrobe.append(pants)
# Texture paths are portable, and only used verified materials survive final purge.
used={m for o in [body]+wardrobe for m in o.data.materials if m}
for m in used:
 if m==skin or not m.use_nodes:continue
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image:
   im=n.image;oldpath=Path(bpy.path.abspath(im.filepath,library=im.library));name=oldpath.name
   source=root/'Assets/NeonSyndicate/Art/ProductionH7/Textures'/name
   if not source.is_file():source=root/'Assets/NeonSyndicate/Art/ProductionH6/Textures'/name
   if source.is_file() and im.name!='H8R natural brown iris':shutil.copyfile(source,tex/name);im.filepath=str(tex/name);im.reload();im.pack()
# Preserve selected source head socket, fixed semantic compatibility and existing action library.
for action in loaded.actions:
 if action:action.use_fake_user=True
for o in excluded+[oldrig]:
 if o and o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
for o in bpy.context.scene.objects:
 if o.type=='MESH':o['candidate_source']='selected-owner-human-h8r'if o==body else'fitted-original-wardrobe-h8r'
body['owner_source_sha256']=expected;rig['gameplay_authority']='ExistingPose';rig['owner_approval']='NOT GIVEN'
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1;bpy.context.scene.frame_set(1)
# No imported scripts, source paths, discarded material dependencies or duplicate skeletons survive.
for text in list(bpy.data.texts):bpy.data.texts.remove(text)
for i in range(4):bpy.ops.outliner.orphans_purge(do_recursive=True)
for im in bpy.data.images:
 if im.source=='FILE':
  if im.packed_file:im.unpack(method='REMOVE')
  im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+Path(im.filepath).name
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=24
source=src/'MalikVeyrH8R.blend';bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
report={'source_file':'Owner supplied packed human, version 5','source_sha256':expected,'source_preserved':hashlib.sha256(a.source.read_bytes()).hexdigest()==expected,'body_vertices':len(body.data.vertices),'body_polygons':len(body.data.polygons),'visual_height_metres':1.86,'source_height_metres':maxz-minz,'source_scale':scale,'bones':len(rig.data.bones),'bone_names':[b.name for b in rig.data.bones],'clothing_clearance_corrections':corrected,'skin_material':'Selected CC0 2K skin and normal','wardrobe':'Actual H7/H8 original coat, armor and groom fitted by complete bind transfer plus new selected-body trousers','native_unity':'NOT RUN','owner_approval':'NOT GIVEN','output_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'uvs':len(o.data.uv_layers)}for o in [body]+wardrobe]}
(src/'authoring.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['meshes','bone_names']},indent=2),flush=True)
