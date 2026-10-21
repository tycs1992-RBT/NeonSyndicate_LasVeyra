#!/usr/bin/env python3
"""Render the actual versioned character in a DCC. Never a Unity screenshot or art approval."""
import argparse,json,hashlib,math
from pathlib import Path
import bpy
from mathutils import Vector,Matrix
p=argparse.ArgumentParser();p.add_argument('--model',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--views',default='front,face');p.add_argument('--action');p.add_argument('--frame',type=int,default=1);p.add_argument('--weapon-obj',type=Path);a=p.parse_args();a.output.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=str(a.model))
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.cycles.max_bounces=6
scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=768;scene.render.resolution_y=1024;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=0
scene.world=bpy.data.worlds.new('Neutral review world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.15,.19,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object;floor.name='Review floor, not a game asset';m=bpy.data.materials.new('Review ground');m.diffuse_color=(.065,.07,.085,1);floor.data.materials.append(m)
def light(name,pos,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1.1))-o.location).to_track_quat('-Z','Y').to_euler()
light('Neutral key',(2,-3,3.3),350,3,(1,.92,.84));light('Soft fill',(-2,-1.5,2.0),180,2,(.72,.82,1));light('Rim',(1,2,3.0),220,2,(.75,.83,1))
data=bpy.data.cameras.new('Source review camera');camera=bpy.data.objects.new('Source review camera',data);bpy.context.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO'
views={'front':((0,-4,1.02),(0,0,1.02),2.16),'side':((4,0,1.02),(0,0,1.02),2.16),'rear':((0,4,1.02),(0,0,1.02),2.16),'three-quarter':((2.8,-4,1.1),(0,0,1.02),2.16),'face':((0,-3,1.74),(0,-.01,1.74),.46)}
records=[]
neutral=bpy.data.materials.new('Neutral anatomy inspection');neutral.use_nodes=True;neutral.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.32,.32,.32,1);neutral.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.7
rig=bpy.data.objects['MalikRig']
prop=None
if a.weapon_obj:
 vertices=[];faces=[];materials=[];group=0
 for line in a.weapon_obj.read_text().splitlines():
  fields=line.split()
  if not fields:continue
  if fields[0]=='v':vertices.append(tuple(float(x)for x in fields[1:4]))
  if fields[0]=='g':group=int(fields[1][-1])
  if fields[0]=='f':faces.append(tuple(int(x)-1 for x in fields[1:]));materials.append(group)
 data=bpy.data.meshes.new('Actual canonical Oathblade review geometry');data.from_pydata(vertices,[],faces);data.update();prop=bpy.data.objects.new('REVIEW PROP ONLY, no inventory grant',data);bpy.context.collection.objects.link(prop)
 for name,color,metal in [('Grip',(.02,.016,.014,1),0),('Steel',(.12,.13,.16,1),.8),('Mineral',(.27,.06,.43,1),.25)]:
  mat=bpy.data.materials.new(name);mat.use_nodes=True;bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=color;bsdf.inputs['Metallic'].default_value=metal;bsdf.inputs['Roughness'].default_value=.36;prop.data.materials.append(mat)
 for face,mat in zip(prop.data.polygons,materials):face.material_index=mat

for name in a.views.split(','):
 for bone in rig.pose.bones:
  if not a.action:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0)
 if a.action:
  rig.animation_data_create();rig.animation_data.action=bpy.data.actions[a.action];scene.frame_set(a.frame);bpy.context.view_layer.update()
 neutral_view=name.startswith('neutral-');stress=name.startswith('pose-');key=name[8:]if neutral_view else'three-quarter'if stress else name
 scene.view_layers[0].material_override=neutral if neutral_view else None
 for obj in bpy.data.objects:
  if obj.name.startswith('Cornea'):obj.hide_render=neutral_view
 if stress:
  rig.pose.bones['upperarm_l'].rotation_euler=(.7,-.1,-.35);rig.pose.bones['lowerarm_l'].rotation_euler=(1.0,0,0);rig.pose.bones['upperarm_r'].rotation_euler=(-.35,.1,.3);rig.pose.bones['lowerarm_r'].rotation_euler=(.75,0,0)
  for bone in rig.pose.bones:
   if any(bone.name.startswith(f)for f in ['index_','middle_','ring_','pinky_']):bone.rotation_euler=(.45,0,0)
 if prop is not None:
  hand=rig.pose.bones['hand_r'];delta=hand.matrix.to_quaternion()@hand.bone.matrix_local.to_quaternion().inverted();prop.matrix_world=Matrix.LocRotScale(hand.head.lerp(hand.tail,.58),delta,Vector((1,1,1)))
 pos,target,scale=views[key];camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
 if name=='neutral':continue
 scene.render.filepath=str(a.output/(name+'.png'));bpy.ops.render.render(write_still=True)
 records.append({'view':name,'position':pos,'target':target,'ortho_scale':scale,'file':name+'.png','synthetic_deformation_pose':stress,'neutral_material_without_cornea':neutral_view})
(a.output/'render-manifest.json').write_text(json.dumps({'tag':'SOURCE 3D RENDER','source_blend':a.model.name,'source_sha256':hashlib.sha256(a.model.read_bytes()).hexdigest(),'engine':'DCC Cycles CPU, 24 samples, AgX','resolution':[768,1024],'native_unity':'NOT RUN','owner_approval':'NOT GIVEN','review_weapon_sha256':hashlib.sha256(a.weapon_obj.read_bytes()).hexdigest()if a.weapon_obj else None,'weapon_scope':'Actual existing Oathblade mesh, DCC grip study only; no inventory or native contact acceptance','authored_action':a.action,'action_frame':a.frame,'views':records},indent=2)+'\n')
