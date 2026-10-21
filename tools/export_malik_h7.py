#!/usr/bin/env python3
"""Export actual H7 geometry and authored rig actions, without modifying the artist source."""
import argparse,hashlib,json
from pathlib import Path
import bpy,bmesh
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();source=root/'ArtSource/AscensionH7/MalikVeyrH7.blend';bpy.ops.wm.open_mainfile(filepath=str(source));rig=bpy.data.objects['MalikRig'];out=root/'Assets/NeonSyndicate/Art/ProductionH7/Models';out.mkdir(parents=True,exist_ok=True)
def active(obj):
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
# A rig-only FBX carries real actions. It cannot add a visual, root-motion motor or gameplay event.
active(rig);rig.animation_data_create();rig.animation_data.action=bpy.data.actions['H7_IdleBreath'];bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=91
motion=out.parent/'Animations/MalikMotionH7.fbx';bpy.ops.export_scene.fbx(filepath=str(motion),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL')
rig.animation_data.action=None
for pb in rig.pose.bones:pb.location=(0,0,0);pb.rotation_quaternion=(1,0,0,0);pb.scale=(1,1,1)
bpy.context.scene.frame_set(1);bpy.context.view_layer.update();bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
originals=[o for o in bpy.data.objects if o.type=='MESH'];stats=[];bones=set(rig.data.bones.keys())
for level in [1,2]:
 for original in originals:
  obj=original.copy();obj.data=original.data.copy();bpy.context.collection.objects.link(obj);obj.name='LOD'+str(level)+'_'+original.name;active(obj)
  count=original.get('h7_strand_vertices')
  if count:
   stride=(2 if count==12 else 3)if level==1 else(12 if count==12 else 20);bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if(v.index//count)%stride!=0],context='VERTS');bm.to_mesh(obj.data);bm.free()
  elif original.name not in ['MalikBody','Brows','Lashes']and not original.name.startswith(('Iris','Cornea','Pupil','Sclera')):
   backup=obj.data.copy()
   for m in list(obj.modifiers):
    if m.type=='ARMATURE':obj.modifiers.remove(m)
   mod=obj.modifiers.new('Authored lower-detail export','DECIMATE');mod.ratio=.60 if level==1 else .28;bpy.ops.object.modifier_apply(modifier=mod.name)
   if any(not any(g.weight>0 and obj.vertex_groups[g.group].name in bones for g in v.groups)or sum(g.weight>0 and obj.vertex_groups[g.group].name in bones for g in v.groups)>8 for v in obj.data.vertices):obj.data=backup
   arm=obj.modifiers.new('Original-compatible skin','ARMATURE');arm.object=rig
  for vertex in obj.data.vertices:
   weights=[(g.group,g.weight)for g in vertex.groups if obj.vertex_groups[g.group].name in bones and g.weight>0];total=sum(w for _,w in weights)
   if not weights or len(weights)>8:raise ValueError('Invalid H7 LOD influence '+obj.name)
   for index,w in weights:obj.vertex_groups[index].add([vertex.index],w/total,'REPLACE')
for obj in originals:obj.name='LOD0_'+obj.name
material_groups={}
for obj in [o for o in bpy.data.objects if o.type=='MESH']:material_groups.setdefault((obj.name.split('_')[0],obj.data.materials[0].name),[]).append(obj)
for(level,name),objects in material_groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for obj in objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 if len(objects)>1:bpy.ops.object.join()
 obj=bpy.context.object;obj.name=level+'_'+name;active(obj);tri=obj.modifiers.new('Runtime tangent-safe triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 stats.append({'lod':level,'mesh':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.polygons),'material':name})
files=[source,motion]
for level in range(3):
 target=out/('MalikVeyrH7'+(''if level==0 else'_LOD'+str(level))+'.fbx')
 for obj in bpy.data.objects:obj.select_set(obj.type in{'ARMATURE','EMPTY'}or obj.type=='MESH'and obj.name.startswith('LOD'+str(level)+'_'))
 bpy.context.view_layer.objects.active=rig;bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE',mesh_smooth_type='FACE',use_tspace=False);files.append(target)
(out/'authoring-stats.json').write_text(json.dumps({'candidate':'MALIK-H7-REVIEW-01','bone_count':len(rig.data.bones),'lods':stats,'runtime_skins':len(stats),'owner_approval':'NOT GIVEN','native_unity':'NOT RUN','authored_animation':'Five actual Blender actions and rig-only FBX','files':[{'path':str(f.relative_to(root)),'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()}for f in files]},indent=2)+'\n');print('ACTUAL H7 EXPORT',json.dumps([(f.name,f.stat().st_size)for f in files]),flush=True)
