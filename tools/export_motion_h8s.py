#!/usr/bin/env python3
"""Sample the real H7/H8 artist actions for the original IK pose owner. No engine events or root motor."""
import argparse,hashlib,json
from pathlib import Path
import bpy
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);p.add_argument('--source',default='ArtSource/AscensionH8S/MalikVeyrH8S.blend');a=p.parse_args();root=a.root.resolve();source=root/a.source
bpy.ops.wm.open_mainfile(filepath=str(source));rig=bpy.data.objects['MalikRig'];scene=bpy.context.scene
names=['pelvis','spine_01','spine_03','neck_01','Head','upperarm_l','lowerarm_l','hand_l','upperarm_r','lowerarm_r','hand_r','thigh_l','calf_l','foot_l','thigh_r','calf_r','foot_r']
def pos(v):return {'x':-v.x,'y':v.z,'z':-v.y}
def quat(q):q.normalize();return {'x':q.x,'y':-q.z,'z':q.y,'w':q.w}
bind=[pos(rig.data.bones[n].head_local)for n in names];clips=[]
for name in ['H7_IdleBreath','H7_Walk','H7_Run','H7_SwordReady','H7_OathCut']+sorted(x.name for x in bpy.data.actions if x.name.startswith('H8_')):
 action=bpy.data.actions[name];rig.animation_data_create();rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];first,last=map(int,action.frame_range);frames=[]
 for f in range(first,last+1):
  scene.frame_set(f);bpy.context.view_layer.update();frames.append({'t':(f-first)/30,'p':[pos(rig.pose.bones[n].head)for n in names],'q':[quat(rig.pose.bones[n].matrix.to_quaternion()@rig.data.bones[n].matrix_local.to_quaternion().inverted())for n in names]})
 clips.append({'name':name,'duration':(last-first)/30,'loop':bool(action.get('h7_loop',action.get('h8_loop',False))),'samples':frames})
out=root/'Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json';out.parent.mkdir(parents=True,exist_ok=True)
d={'schema':2,'assetId':'MALIK-H8R-REVIEW-01','sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'space':'canonical actor-local joint positions and world rotation deltas; reflected Blender (-x,z,-y)','rootMotion':False,'events':False,'bind':bind,'clips':clips};out.write_text(json.dumps(d,separators=(',',':'))+'\n');print(json.dumps({'clips':len(clips),'samples':sum(len(c['samples'])for c in clips),'source_sha256':d['sourceSha256'],'bytes':out.stat().st_size}))

# Individual admitted derivatives are import-review assets, never gameplay event owners.
for name in ['H8_Landing_Supplied','H8_Listening_Supplied']:
 action=bpy.data.actions[name];rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 first,last=map(int,action.frame_range);scene.frame_start=first;scene.frame_end=last;scene.frame_set(first)
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(out.parent/(name+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL')
