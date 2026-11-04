#!/usr/bin/env python3
"""Author editable, in-place H7 Blender actions on the actual 53-bone character."""
import argparse,hashlib,json,math
from pathlib import Path
import bpy
from mathutils import Vector,Matrix,Quaternion,Euler
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();source=root/'ArtSource/AscensionH7/MalikVeyrH7.blend';bpy.ops.wm.open_mainfile(filepath=str(source));rig=bpy.data.objects['MalikRig'];scene=bpy.context.scene;scene.render.fps=30
for action in list(bpy.data.actions):bpy.data.actions.remove(action)
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
def update():bpy.context.view_layer.update()
def reset():
 for pb in rig.pose.bones:pb.location=(0,0,0);pb.rotation_quaternion=(1,0,0,0);pb.scale=(1,1,1)
 update()
def rotate_world(name,angles):
 pb=rig.pose.bones[name];m=pb.matrix.copy();q=Euler(tuple(math.radians(t)for t in angles),'XYZ').to_quaternion()@m.to_quaternion();pb.matrix=Matrix.LocRotScale(m.translation,q,Vector((1,1,1)));update()
def aim(name,point):
 pb=rig.pose.bones[name];m=pb.matrix.copy();direction=Vector(point)-m.translation
 if direction.length<1e-5:return
 q=(pb.tail-pb.head).normalized().rotation_difference(direction.normalized())@m.to_quaternion();pb.matrix=Matrix.LocRotScale(m.translation,q,Vector((1,1,1)));update()
def limb(upper,lower,endpoint,pole):
 first=rig.pose.bones[upper];second=rig.pose.bones[lower];origin=first.head.copy();target=Vector(endpoint);d=target-origin;length=d.length;u=d.normalized();l1=first.bone.length;l2=second.bone.length;length=max(abs(l1-l2)+.001,min(length,l1+l2-.001));target=origin+u*length
 along=(l1*l1-l2*l2+length*length)/(2*length);height=math.sqrt(max(0,l1*l1-along*along));bend=Vector(pole)-origin;bend=(bend-u*bend.dot(u)).normalized();joint=origin+u*along+bend*height;aim(upper,joint);aim(lower,target)
def hand(side,curl):
 for pb in rig.pose.bones:
  if pb.name.endswith('_'+side)and pb.name.startswith(('index_','middle_','ring_','pinky_')):pb.rotation_quaternion=Quaternion((1,0,0),curl)
  if pb.name.endswith('_'+side)and pb.name.startswith('thumb_'):pb.rotation_quaternion=Quaternion((0,0,1),-.23 if side=='l'else .23)
 update()
def pose(clip,t):
 reset();cycle=2*math.pi*t;moving=clip in('H7_Walk','H7_Run');running=clip=='H7_Run';ready=clip in('H7_SwordReady','H7_OathCut');lower=.035 if not moving else .065 if not running else .085
 bob=(.004*math.cos(cycle)if not moving else .012*math.cos(cycle*2)if not running else .021*math.cos(cycle*2));pb=rig.pose.bones['pelvis'];pb.matrix=Matrix.Translation(Vector((0,0,-lower+bob)))@pb.bone.matrix_local;update()
 rotate_world('spine_01',(2 if moving else 0,0,2*math.sin(cycle)if moving else .6*math.sin(cycle)))
 rotate_world('spine_03',(-5 if running else -1.5 if moving else .6*math.sin(cycle),.5*math.sin(cycle),-4*math.sin(cycle)if moving else .3*math.sin(cycle)))
 rotate_world('neck_01',(1,0,-1.2*math.sin(cycle)if moving else -.35*math.sin(cycle)))
 rotate_world('Head',(-.5+.35*math.sin(cycle),0,.35*math.sin(cycle)))
 for side,sign,phase in [('l',1,cycle),('r',-1,cycle+math.pi)]:
  if moving:stride=.255 if not running else .33;y=-stride*math.cos(phase);lift=max(0,-math.sin(phase))*(.065 if not running else .13)
  else:y=-.025 if side=='l'else .035;lift=0
  endpoint=(sign*.173,y-.012,.078+lift);limb('thigh_'+side,'calf_'+side,endpoint,(sign*.18,-.5,.55));aim('foot_'+side,Vector(endpoint)+Vector((sign*.005,-.14,-.045 if lift==0 else -.015)))
  swing=(.12 if not running else .24)*math.cos(phase)if moving else .008*math.sin(cycle)
  wrist=(sign*(.245 if not running else .26),-.10+swing,1.04 if not running else 1.19);limb('upperarm_'+side,'lowerarm_'+side,wrist,(sign*.55,.12,1.3));hand(side,.22 if not running else .42)
 if ready:
  rotate_world('spine_03',(-2,-2,-8));limb('upperarm_r','lowerarm_r',(-.20,-.29,1.29),(-.46,-.03,1.40));limb('upperarm_l','lowerarm_l',(.26,-.12,1.18),(.50,.07,1.34));hand('r',.95);hand('l',.35)
 if clip=='H7_OathCut':
  # Authored poses are retimed visually only. The original game owns every contact and recovery window.
  controls=[(0,(-.20,-.29,1.29),-8),(.19,(-.28,-.03,1.65),-17),(.36,(-.34,-.12,1.74),-22),(.47,(-.15,-.39,1.52),-7),(.55,(.14,-.39,1.22),17),(.69,(.32,-.19,1.04),23),(1,(-.20,-.29,1.29),-8)]
  for i in range(len(controls)-1):
   aa,bb=controls[i:i+2]
   if aa[0]<=t<=bb[0]:
    u=(t-aa[0])/(bb[0]-aa[0]);u=u*u*(3-2*u);w=Vector(aa[1]).lerp(Vector(bb[1]),u);yaw=aa[2]+(bb[2]-aa[2])*u;break
  rotate_world('spine_03',(-2-4*math.sin(math.pi*t),-2,yaw+8));limb('upperarm_r','lowerarm_r',w,(-.50,-.03,1.47));limb('upperarm_l','lowerarm_l',(.28,-.16,1.22),(.48,.10,1.36));hand('r',.95);hand('l',.45)
 update()
clips=[('H7_IdleBreath',90,True),('H7_Walk',48,True),('H7_Run',24,True),('H7_SwordReady',60,True),('H7_OathCut',36,False)];records=[];tracks=[]
for name,frames,loop in clips:
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data_create();rig.animation_data.action=action;scene.frame_start=1;scene.frame_end=frames+1
 first_pose={};previous={}
 for frame in range(1,frames+2):
  t=(frame-1)/frames;scene.frame_set(frame);pose(name,t)
  if frame==1:first_pose={pb.name:(pb.location.copy(),pb.rotation_quaternion.copy())for pb in rig.pose.bones}
  if loop and frame==frames+1:
   for pb in rig.pose.bones:pb.location=first_pose[pb.name][0];pb.rotation_quaternion=first_pose[pb.name][1]
  for pb in rig.pose.bones:
   q=pb.rotation_quaternion.copy()
   if pb.name in previous and q.dot(previous[pb.name])<0:q.negate();pb.rotation_quaternion=q
   previous[pb.name]=q
  update()
  for pb in rig.pose.bones:
   pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
   if pb.name=='pelvis':pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
 for fc in action.fcurves:
  for key in fc.keyframe_points:key.interpolation='LINEAR'
 action['h7_loop']=loop;action['h7_contact_frame']=21 if name=='H7_OathCut'else -1;action['h7_no_root_motion']=True
 # These are real sampled authored DCC rotations. Only three torso/head accents can enter the existing pose writer.
 samples=[]
 for frame in range(1,frames+2):
  scene.frame_set(frame);update();row=[]
  for bone in ['spine_03','neck_01','Head']:
   pb=rig.pose.bones[bone];q=pb.matrix.to_quaternion()@pb.bone.matrix_local.to_quaternion().inverted();q.normalize();row.append({'x':q.x,'y':-q.z,'z':q.y,'w':q.w})
  samples.append({'t':(frame-1)/30,'q':row})
 tracks.append({'name':name,'duration':frames/30,'loop':loop,'samples':samples})
 records.append({'name':name,'frames':[1,frames+1],'fps':30,'duration':frames/30,'loop':loop,'curves':len(action.fcurves),'keyframes':sum(len(fc.keyframe_points)for fc in action.fcurves),'root_motion':False,'events':[],'contact_frame':21 if name=='H7_OathCut'else None})
rig.animation_data.action=None;reset();scene.frame_set(1);scene.frame_end=91;bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True,relative_remap=False)
out=root/'Assets/NeonSyndicate/Art/ProductionH7/Animations';out.mkdir(parents=True,exist_ok=True)
(out/'MalikMotionH7.json').write_text(json.dumps({'schema':1,'assetId':'MALIK-H7-REVIEW-01','space':'canonical actor-local world rotation delta; Blender axial (x,-z,y,w)','bones':['Chest','Neck','Head'],'rootMotion':False,'events':False,'clips':tracks},separators=(',',':'))+'\n')
(root/'ArtSource/AscensionH7/animation-source.json').write_text(json.dumps({'scope':'Actual editable Blender actions, not native gameplay acceptance','creator':'Project-authored H7','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'actions':records,'runtime_scope':'Optional reversible Editor review accents only; full body playback requires native contact acceptance','native_unity':'NOT RUN'},indent=2)+'\n');art_path=root/'ArtSource/AscensionH7/art-source.json';art=json.loads(art_path.read_text());art['authored_mesh_stage_sha256']=art.get('authored_mesh_stage_sha256',art['h7_source_sha256']);art['h7_source_sha256']=hashlib.sha256(source.read_bytes()).hexdigest();art_path.write_text(json.dumps(art,indent=2)+'\n');print('AUTHORED ACTIONS',json.dumps(records),flush=True)
