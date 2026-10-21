#!/usr/bin/env python3
"""Reopen actual DCC output in a fresh process; do not infer Unity acceptance."""
import json,sys,math
from pathlib import Path
import bpy
path=Path(sys.argv[1]);record=json.loads(path.with_suffix('.json').read_text());bpy.ops.wm.open_mainfile(filepath=str(path))
rig=bpy.data.objects['MalikRig'];assert rig.type=='ARMATURE'and len(rig.data.bones)==53
for role in ['MalikBody']+[r['role']for r in record['source_parts']]:
 obj=bpy.data.objects.get(role);assert obj and obj.type=='MESH'and len(obj.data.vertices)>20,role
 assert all(all(math.isfinite(v)for v in point.co)for point in obj.data.vertices),role
assert all(b in rig.data.bones for b in ['pelvis','spine_01','spine_03','neck_01','head','hand_l','hand_r','index_03_l','pinky_03_r'])
print('Fresh-process fitted-source validation PASS; 53-bone real human scene. Native Unity NOT RUN.',flush=True)
