#!/usr/bin/env python3
"""Export the real selected human and fitted wardrobe; preserve complete weights and visual-only authority."""
import bpy,bmesh,argparse,hashlib,json
from pathlib import Path
from mathutils import Vector,Matrix
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();source=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';out=root/'Assets/NeonSyndicate/Art/ProductionH8R';bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False);rig=bpy.data.objects['MalikRig'];rig.animation_data_clear()
for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=(1,0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
for o in list(bpy.data.objects):
 if o.type not in {'MESH','ARMATURE'}:bpy.data.objects.remove(o,do_unlink=True)
# Visual review locators are rebuilt on the selected bind, without moving the gameplay weapon.
roles={'hand_r':'hand_r','hand_l':'hand_l','foregrip':'hand_l','muzzle':'hand_r','blade_hilt':'hand_r','blade_tip':'hand_r','head':'Head','chest':'spine_03','fx':'spine_03','synergy':'spine_03'}
for role,bone in roles.items():
 o=bpy.data.objects.new('socket_'+role,None);bpy.context.collection.objects.link(o);o.parent=rig;o.parent_type='BONE';o.parent_bone=bone;bpy.context.view_layer.update();o.matrix_world=Matrix.Translation(rig.data.bones[bone].head_local)
o=bpy.data.objects.new('review_forward',None);bpy.context.collection.objects.link(o);o.location=(0,-.4,1.7);o.parent=rig
originals=[o for o in bpy.data.objects if o.type=='MESH'];bones=set(rig.data.bones.keys())
def active(o):bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for obj in originals:
 for m in list(obj.modifiers):
  if m.type!='ARMATURE':active(obj);bpy.ops.object.modifier_apply(modifier=m.name)
 if not obj.data.uv_layers:
  active(obj);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
 for v in obj.data.vertices:
  rows=[(g.group,g.weight)for g in v.groups if obj.vertex_groups[g.group].name in bones and g.weight>0];total=sum(w for _,w in rows)
  if not rows or len(rows)>8:raise ValueError('Complete weight policy cannot export '+obj.name+' vertex '+str(v.index))
  for i,w in rows:obj.vertex_groups[i].add([v.index],w/total,'REPLACE')
for level in [1,2]:
 for original in originals:
  obj=original.copy();obj.data=original.data.copy();bpy.context.collection.objects.link(obj);obj.name='LOD'+str(level)+'_'+original.name;active(obj)
  for m in list(obj.modifiers):obj.modifiers.remove(m)
  strands=original.get('h7_strand_vertices')
  if strands:
   stride=3 if level==1 else 9;bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if(v.index//strands)%stride!=0],context='VERTS');bm.to_mesh(obj.data);bm.free()
  elif len(obj.data.polygons)>180:
   mod=obj.modifiers.new('Selected anatomy distance LOD','DECIMATE');mod.ratio=.65 if level==1 else .34;bpy.ops.object.modifier_apply(modifier=mod.name)
  for v in obj.data.vertices:
   rows=[(g.group,g.weight)for g in v.groups if obj.vertex_groups[g.group].name in bones and g.weight>0];total=sum(w for _,w in rows)
   if not rows or len(rows)>8:raise ValueError('Invalid complete LOD influences '+obj.name)
   for i,w in rows:obj.vertex_groups[i].add([v.index],w/total,'REPLACE')
  mod=obj.modifiers.new('Complete selected skin','ARMATURE');mod.object=rig
for obj in originals:obj.name='LOD0_'+obj.name
batches={}
for o in [o for o in bpy.data.objects if o.type=='MESH']:
 if len(o.data.materials)!=1:raise ValueError('Unbatched source material '+o.name)
 batches.setdefault((o.name.split('_')[0],o.data.materials[0].name),[]).append(o)
rows=[]
for (level,material),objects in batches.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 if len(objects)>1:bpy.ops.object.join()
 o=bpy.context.object;o.name=level+'_'+material;active(o);m=o.modifiers.new('Explicit tangent-safe triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=m.name)
 rows.append({'lod':level,'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.polygons),'material':material})
files=[]
for level in range(3):
 f=out/'Models'/('MalikVeyrH8R'+(''if level==0 else'_LOD'+str(level))+'.fbx')
 for o in bpy.data.objects:o.select_set(o.type in {'ARMATURE','EMPTY'}or o.type=='MESH'and o.name.startswith('LOD'+str(level)+'_'))
 bpy.context.view_layer.objects.active=rig;bpy.ops.export_scene.fbx(filepath=str(f),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE',mesh_smooth_type='FACE');files.append(f)
files+=sorted((out/'Textures').glob('*.png'))
record={'assetId':'MALIK-H8R-REVIEW-01','creator':'Owner selected CC0 human and skin; project fitted original wardrobe and binding','license':'CC0-1.0','commercialUse':True,'publicSourceApproved':True,'ownerApproved':False,'enabled':False,'preserveInfluences':True,'sourceFbx':str(files[0].relative_to(root)),'authoringSource':str(source.relative_to(root)),'NATIVE_UNITY_IMPORT':'NOT RUN','OWNER_ART_APPROVAL':'NOT GIVEN','files':[{'path':str(f.relative_to(root)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'bytes':f.stat().st_size}for f in files]}
(out/'candidate.json').write_text(json.dumps(record,indent=2)+'\n');(out/'Models/export-stats.json').write_text(json.dumps({'bones':len(rig.data.bones),'lods':rows,'native_unity':'NOT RUN','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()},indent=2)+'\n');print(json.dumps([(r['lod'],r['name'],r['vertices'])for r in rows]),flush=True)
