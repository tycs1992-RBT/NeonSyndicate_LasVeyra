"""Real Blender source load and FBX reimport. DCC evidence only, never native Unity proof."""
import argparse,hashlib,json,math,sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True)
m=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartWR02/manifest.json').read_text());sources=[]
for source in [m['source']]+[x['path']for x in m['additional_sources']if x['path'].endswith('.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/source));meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert meshes and all(o.data.uv_layers for o in meshes);sources.append({'path':source,'sha256':hashlib.sha256((ROOT/source).read_bytes()).hexdigest(),'editable_meshes':len(meshes),'passed':True})
rows=[]
for row in m['modules']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=False);meshes=[o for o in bpy.data.objects if o.type=='MESH'];vertices=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];bounds=[[min(v[i]for v in vertices)for i in range(3)],[max(v[i]for v in vertices)for i in range(3)]];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons)for o in meshes)
 uv=all(o.data.uv_layers and all(math.isfinite(x)for v in o.data.uv_layers[0].data for x in v.uv)for o in meshes);normals=all(abs(n.vector.length-1)<.015 for o in meshes for n in o.data.corner_normals);size=max(abs(bounds[j][i]-row['source_bounds'][j][i])for j in range(2)for i in range(3))<.015
 ok=len(meshes)==1 and triangles==row['triangles']and uv and normals and size;rows.append({'path':row['path'],'passed':bool(ok),'triangles':triangles,'finite_uv':bool(uv),'unit_normals':normals,'bounds_match':bool(size),'bounds':bounds})
result={'scope':'Actual Blender '+bpy.app.version_string+' source loads and eighteen FBX reimports. No Unity execution.','source_loads':sources,'rows':rows,'passed':sum(r['passed']for r in rows),'total':len(rows),'native_unity':'NOT RUN'}
(a.output/'blender-reimport.json').write_text(json.dumps(result,indent=2)+'\n');assert all(r['passed']for r in rows),'FBX reimport failed'
