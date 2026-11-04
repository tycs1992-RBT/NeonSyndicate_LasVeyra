#!/usr/bin/env python3
"""Retarget one owner-supplied CC0 defensive motion to the selected 53-bone visual. No game events/root motor."""
import argparse,hashlib,json
from pathlib import Path
import bpy
from mathutils import Matrix,Vector
p=argparse.ArgumentParser();p.add_argument('--donor',type=Path,required=True);p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();source=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False);target=bpy.data.objects['MalikRig'];before=set(bpy.data.objects);prior=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(a.donor.resolve()),use_anim=True);added=[o for o in bpy.data.objects if o not in before];donor=next(o for o in added if o.type=='ARMATURE');action=next(x for x in bpy.data.actions if x not in prior and x.name.endswith('|Sword_Block'));donor.animation_data.action=action
if action.slots:donor.animation_data.action_slot=action.slots[0]
created=bpy.data.actions.new('H8_Guard_Brace');created.use_fake_user=True;target.animation_data_create();target.animation_data.action=created
# Matched core names, rest-axis deltas, complete hierarchy. Leaf-only donor bones do not replace the rig.
common=[b for b in target.data.bones if b.name in donor.data.bones]
if len(common)!=53:raise ValueError('The defensive donor does not cover the entire existing 53-bone source.')
for b in target.pose.bones:b.rotation_mode='QUATERNION';b.location=(0,0,0);b.scale=(1,1,1)
first,last=map(int,action.frame_range)
for frame in range(first,last+1):
 bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();desired={b.name:donor.pose.bones[b.name].matrix.to_quaternion()@donor.data.bones[b.name].matrix_local.to_quaternion().inverted()@b.matrix_local.to_quaternion()for b in common}
 for b in common:
  pb=target.pose.bones[b.name];parent=target.data.bones[b.parent.name]if b.parent else None;restLocal=(parent.matrix_local.inverted()@b.matrix_local).to_quaternion()if parent else b.matrix_local.to_quaternion();parentRot=desired[parent.name]if parent else Matrix.Identity(4).to_quaternion();pb.rotation_quaternion=restLocal.inverted()@parentRot.inverted()@desired[b.name];pb.location=(0,0,0);pb.scale=(1,1,1);pb.keyframe_insert('rotation_quaternion',frame=frame-first+1,group=b.name)
created['h8_loop']=False;created['source_license']='CC0-1.0';created['source_sha256']=hashlib.sha256(a.donor.read_bytes()).hexdigest();created['retarget']='Rest-axis world delta onto original 53-bone hierarchy, zero translation and scale'
for o in added:bpy.data.objects.remove(o,do_unlink=True)
for x in list(bpy.data.actions):
 if x not in prior and x!=created:bpy.data.actions.remove(x)
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=last-first+1;bpy.context.scene.frame_set(1);bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True,relative_remap=False)
folder=root/'Assets/NeonSyndicate/Art/ProductionH8R/Animations';folder.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT');target.select_set(True);bpy.context.view_layer.objects.active=target;bpy.ops.export_scene.fbx(filepath=str(folder/'GuardBraceH8R.fbx'),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL')
(root/'ArtSource/AscensionH8R/guard-retarget.json').write_text(json.dumps({'donor_sha256':created['source_sha256'],'donor_clip':'Sword_Block','license':'CC0-1.0','author':'Quaternius','source_url':'https://quaternius.com/','matched_bones':len(common),'frames':last-first+1,'root_translation':False,'game_events':False,'native_unity':'NOT RUN','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()},indent=2)+'\n');print('Retargeted',len(common),'bones',last-first+1,'frames',flush=True)
