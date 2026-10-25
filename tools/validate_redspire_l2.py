#!/usr/bin/env python3
"""Actual Blender re-import, UV, finite geometry, material and distance-mesh checks. Not native Unity import."""
from pathlib import Path
import bpy,json,hashlib,math
from PIL import Image
root=Path(__file__).resolve().parents[1];manifest=json.loads((root/'Docs/LiveWildL2/ASSET_MANIFEST_L2.json').read_text());checks=[]
def check(name,value):checks.append({'name':name,'passed':bool(value)})
for row in manifest['files']:
 path=root/row['path'];check('Exact delivered hash '+row['path'],path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'])
for file in (root/'Assets/NeonSyndicate/Art/LiveWilds/Shared').glob('*.png'):
 with Image.open(file) as image:image.load();check('Complete 1K image '+file.name,image.size==(1024,1024))
for row in manifest['lods']:
 file=root/'Assets/NeonSyndicate/Art/LiveWilds'/row['kind']/(row['variant']+'_LOD'+str(row['lod'])+'.fbx');bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(file),use_anim=False);meshes=[o for o in bpy.data.objects if o.type=='MESH']
 check('One passive UV mesh '+file.name,len(meshes)==1 and len(meshes[0].data.uv_layers)>0 and len(meshes[0].data.materials)==1 and not bpy.data.actions and not any(o.type in {'CAMERA','LIGHT','ARMATURE'}for o in bpy.data.objects))
 mesh=meshes[0];check('Finite complete surface '+file.name,all(all(math.isfinite(x)for x in v.co)for v in mesh.data.vertices) and all(min(o.scale)>0 for o in bpy.data.objects))
 mesh.data.calc_loop_triangles();check('Measured triangle count '+file.name,len(mesh.data.loop_triangles)==row['triangles'])
for kind in ['Boulder','RockSpire','DeadTree']:
 for suffix in ['A','B']:
  tris=[r['triangles']for r in manifest['lods']if r['variant']==kind+'_'+suffix];check('Strict descending LODs '+kind+'_'+suffix,len(tris)==3 and tris[0]>tris[1]>tris[2]>0 and tris[0]<=3000)
report={'scope':__doc__,'passed':sum(r['passed']for r in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN'};(root/'Docs/LiveWildL2/asset-validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':len(checks),'failed':[r['name']for r in checks if not r['passed']]}),flush=True)
if report['passed']!=report['total']:raise SystemExit(1)
