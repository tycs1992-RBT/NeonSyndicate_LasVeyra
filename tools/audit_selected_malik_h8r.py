#!/usr/bin/env python3
"""Execute source mesh, skin, material and authored-pose checks in Blender. Never a native Unity result."""
import bpy,json,math,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1];path=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False);rig=bpy.data.objects['MalikRig'];checks=[];issues=[]
def check(n,v):checks.append({'name':n,'passed':bool(v)})
check('One actual selected-human 53-bone skeleton',len([o for o in bpy.data.objects if o.type=='ARMATURE'])==1 and len(rig.data.bones)==53)
check('Source body identity and actual original UV',bpy.data.objects['MalikBodyH8R']['owner_source_sha256']=='a69c2a5d916a000990d7f8e4ab8b51519a3b877a8e3a5622b5ddb6c125c523f6'and len(bpy.data.objects['MalikBodyH8R'].data.uv_layers)==1)
maxw=0;unweighted=0;bad=0;meshes=[]
for o in bpy.data.objects:
 if o.type!='MESH':continue
 missing_uv=not o.data.uv_layers
 if missing_uv:issues.append('Editable source UV missing: '+o.name+'; FBX export generates UV before delivery')
 for v in o.data.vertices:
  weights=[g.weight for g in v.groups if o.vertex_groups[g.group].name in rig.data.bones and g.weight>0];maxw=max(maxw,len(weights));unweighted+=not weights;bad+=abs(sum(weights)-1)>.00001 or not all(math.isfinite(x)for x in v.co)
 meshes.append({'name':o.name,'vertices':len(o.data.vertices),'uv_layers':len(o.data.uv_layers)})
check('Complete normalized finite skin, no four-weight pruning',unweighted==0 and bad==0 and maxw<=8)
images=[]
for im in bpy.data.images:
 if im.source!='FILE':continue
 p=Path(bpy.path.abspath(im.filepath));valid=im.filepath.startswith('//')and p.is_file();check('Portable actual image: '+im.name,valid)
 images.append({'name':im.name,'relative':im.filepath,'bytes':p.stat().st_size if p.exists()else 0,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()if p.exists()else''})
clipnames=['H7_IdleBreath','H7_Walk','H7_Run','H7_SwordReady','H7_OathCut','H8_Dodge','H8_CrystalCast','H8_FireAim','H8_FireRecoil','H8_JumpLanding','H8_SurgeRise','H8_FlightTakeoff','H8_FlightLanding','H8_AegisLink','H8_Guard_Brace'];poses=[]
for name in clipnames:
 action=bpy.data.actions.get(name);check('Actual source action: '+name,action is not None)
 if action is None:continue
 rig.animation_data_create();rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];lo,hi=action.frame_range;valid=True;maximum=0
 for f in [int(lo),int((lo+hi)/2),int(hi)]:
  bpy.context.scene.frame_set(f);dg=bpy.context.evaluated_depsgraph_get()
  for o in [bpy.data.objects['MalikBodyH8R'],bpy.data.objects['H7 tailored connected coat'],bpy.data.objects['H7 split walking panel l'],bpy.data.objects['H7 split walking panel r']]:
   ev=o.evaluated_get(dg);mesh=ev.to_mesh()
   for v in mesh.vertices:
    valid=valid and all(math.isfinite(x)for x in v.co);maximum=max(maximum,v.co.length)
   ev.to_mesh_clear()
 check('Finite bounded actual skinned deformation: '+name,valid and maximum<4);poses.append({'name':name,'frames_sampled':3,'maximum_vertex_radius':maximum})
report={'scope':__doc__,'source_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'checks':checks,'passed':sum(x['passed']for x in checks),'total':len(checks),'meshes':meshes,'maximum_influences':maxw,'unweighted':unweighted,'normalization_or_nonfinite_errors':bad,'images':images,'poses':poses,'remaining_art_issues':issues+['Finite poses do not establish collision-free cloth, planted feet, weapon contact or final appearance. Those require native review.'],'native_unity':'NOT RUN','appearance_approval':'NOT GIVEN'}
p=root/'Docs/Ascension04OH/EvidenceH8R/dcc-audit.json';p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'issues':issues,'failed':[x['name']for x in checks if not x['passed']]}),flush=True)
if report['passed']!=report['total']:raise SystemExit(1)
