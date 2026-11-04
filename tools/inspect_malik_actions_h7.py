#!/usr/bin/env python3
"""Inspect the actual editable actions and compatible bind rig. Never native gameplay or cloth/contact acceptance."""
import argparse,hashlib,json,math
from pathlib import Path
import bpy
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);p.add_argument('--output',type=Path,required=True);a=p.parse_args();root=a.root.resolve();bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AscensionH6/MalikVeyrH6.blend'));before={b.name:{'parent':b.parent.name if b.parent else None,'matrix':[v for row in b.matrix_local for v in row]}for b in bpy.data.objects['MalikRig'].data.bones};source=root/'ArtSource/AscensionH7/MalikVeyrH7.blend';bpy.ops.wm.open_mainfile(filepath=str(source));rig=bpy.data.objects['MalikRig'];checks=[]
def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
check('Exactly the same 53 source bones and parents',set(before)==set(rig.data.bones.keys())and all(before[b.name]['parent']==(b.parent.name if b.parent else None)for b in rig.data.bones))
check('Bind matrices unchanged within 0.1 micrometre',all(max(abs(x-y)for x,y in zip(before[b.name]['matrix'],[v for row in b.matrix_local for v in row]))<1e-7 for b in rig.data.bones))
roles=['hand_r','hand_l','foregrip','muzzle','blade_hilt','blade_tip','head','chest','fx','synergy'];check('Ten real visual-only source locators remain',all('socket_'+role in bpy.data.objects for role in roles));records=[]
for action in sorted(bpy.data.actions,key=lambda x:x.name):
 rig.animation_data_create();rig.animation_data.action=action;frames=[int(action.frame_range[0]),int(action.frame_range[1])];root_positions=[];matrices=[];ankles=[]
 for frame in range(frames[0],frames[1]+1):
  bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();root_positions.append(tuple(rig.pose.bones['root'].head));matrices.append([tuple(pb.rotation_quaternion)for pb in rig.pose.bones]);ankles.append([rig.pose.bones['foot_'+side].head.z for side in ['l','r']])
 max_root=max(math.dist(root_positions[0],v)for v in root_positions);loop_error=max(min(math.sqrt(sum((x-y)**2 for x,y in zip(first,last))),math.sqrt(sum((x+y)**2 for x,y in zip(first,last))))for first,last in zip(matrices[0],matrices[-1]))
 check(action.name+' actual finite bone samples',all(math.isfinite(v)for frame in matrices for q in frame for v in q));check(action.name+' zero root travel',max_root<1e-7)
 if action.get('h7_loop'):check(action.name+' loop quaternion closure',loop_error<1e-5)
 check(action.name+' genuine editable channels',len(action.fcurves)>100 and sum(len(fc.keyframe_points)for fc in action.fcurves)>1000)
 records.append({'name':action.name,'frames':frames,'curves':len(action.fcurves),'keys':sum(len(fc.keyframe_points)for fc in action.fcurves),'max_root_translation_metres':max_root,'loop_quaternion_error':loop_error,'ankle_height_range_metres':[min(v for pair in ankles for v in pair),max(v for pair in ankles for v in pair)],'contact_acceptance':'NOT RUN in Unity; ankle range is not sole penetration or foot sliding proof'})
check('Five named actual actions',set(x['name']for x in records)=={'H7_IdleBreath','H7_Walk','H7_Run','H7_SwordReady','H7_OathCut'})
report={'scope':__doc__,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'checks':checks,'passed':sum(x['passed']for x in checks),'total':len(checks),'actions':records,'native_unity':'NOT RUN','production_motion_acceptance':'NOT ACHIEVED'};a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(report,indent=2)+'\n');print('ACTUAL ACTION PROOF',report['passed'],report['total'],flush=True)
if report['passed']!=report['total']:raise SystemExit(1)
