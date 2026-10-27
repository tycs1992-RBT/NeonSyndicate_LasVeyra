"""Actual Blender source/FBX checks and matched retained-source DCC views. Never Unity proof.
Recovered reproducible recipe, with fresh receipts after external workspace pruning.
"""
import argparse,hashlib,json,math,struct,sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--context',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--no-render',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]if '--'in sys.argv else[]);args.output.mkdir(parents=True,exist_ok=True)
manifest=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartWR01/manifest.json').read_text());context_path=args.context/'world-manifest.json';plan=json.loads(context_path.read_text());assert plan['revision']==10 and plan['seed']==61724
bpy.ops.wm.open_mainfile(filepath=str(ROOT/manifest['source']));source={o.name:o for o in bpy.data.objects if o.type=='MESH'};checks=[]
for row in manifest['modules']:
 path=ROOT/row['path'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False);imported=[o for o in bpy.data.objects if o not in before];meshes=[o for o in imported if o.type=='MESH'];vertices=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 bounds=[[min(v[i]for v in vertices)for i in range(3)],[max(v[i]for v in vertices)for i in range(3)]];finite=all(math.isfinite(x)for v in vertices for x in v);uv_ok=all(o.data.uv_layers and all(math.isfinite(x)for uv in o.data.uv_layers[0].data for x in uv.uv)for o in meshes);normals_ok=all(abs(n.vector.length-1)<.015 for o in meshes for n in o.data.corner_normals);materials=sorted({m.name.split('.')[0]for o in meshes for m in o.data.materials});triangles=sum(len(o.data.polygons)for o in meshes);size_ok=max(abs(bounds[j][i]-row['source_bounds'][j][i])for j in range(2)for i in range(3))<.015;passed=bool(meshes)and finite and uv_ok and normals_ok and size_ok and triangles==row['triangles']
 checks.append({'path':row['path'],'sha256':row['sha256'],'passed':bool(passed),'meshes':len(meshes),'triangles':triangles,'source_bounds':bounds,'finite_vertices':finite,'finite_uv':bool(uv_ok),'unit_normals':normals_ok,'materials':materials,'bounds_match':bool(size_ok)})
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)
assert all(r['passed']for r in checks),'Actual WR01 FBX reimport failed'
(args.output/'dcc-import.json').write_text(json.dumps({'scope':'Actual Blender '+bpy.app.version_string+' compressed source load and fifteen FBX reimports. Not Unity. Fresh recovery run.','source_sha256':hashlib.sha256((ROOT/manifest['source']).read_bytes()).hexdigest(),'source_objects':sorted(source),'passed':sum(r['passed']for r in checks),'total':len(checks),'rows':checks,'native_unity':'NOT RUN'},indent=2)+'\n')
if args.no_render:sys.exit(0)
for o in bpy.data.objects:o.hide_render=True
common=[];legacy=[];witness=[];rewritten=[];cache={};references=[]

def place(data,name,position,yaw=0,scale=(1,1,1)):
 o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=(position[0],-position[2],position[1]);o.rotation_euler[2]=math.radians(yaw);o.scale=(scale[0],scale[2],scale[1]);return o

def material(role):
 key='Retained source '+role;m=bpy.data.materials.get(key)
 if m:return m
 m=bpy.data.materials.new(key);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Roughness'].default_value=.82;t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Textures'/(role+'_Albedo.png')),check_existing=True);m.node_tree.links.new(t.outputs['Color'],n.inputs['Base Color']);return m

def data(name,vertices,faces,surface):
 d=bpy.data.meshes.new(name);d.from_pydata([(x,-z,y)for x,y,z in vertices],[],faces);d.materials.append(surface);d.update();return d
roles={r['id']:r['role']for r in json.loads((ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/asset_manifest.json').read_text())['models']}

def cmh(model,low=False):
 path=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Meshes'/(model+('_low'if low else'_high')+'.bytes');key=str(path)
 if key in cache:return cache[key]
 raw=path.read_bytes();assert raw[:4]==b'CMH1','Hydrate source '+str(path);count=struct.unpack_from('<I',raw,4)[0];assert len(raw)==8+count*32 and count%3==0;values=[struct.unpack_from('<8f',raw,8+i*32)for i in range(count)];d=data(model+(' low physical witness'if low else' high source'),[v[:3]for v in values],[(i,i+1,i+2)for i in range(0,count,3)],material(roles[model]));uv=d.uv_layers.new()
 for i,x in enumerate(uv.data):x.uv=values[i][6:8]
 references.append({'path':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(raw).hexdigest()});cache[key]=d;return d
suppressed=[]
for row in plan['art_parts']:
 o=place(cmh(row['model']),row['owner']+' / '+row['model'],row['position'],row['yaw'],row['scale'])
 if row['owner']in['archive','office']and row['model']in['slate_roof','mineral_pier']:
  legacy.append(o);witness.append(place(cmh(row['model'],True),row['owner']+' visible physical witness / '+row['model'],row['position'],row['yaw'],row['scale']));suppressed.append(row)
 else:common.append(o)
family9_names=['retained-family-809556','retained-family-32874','retained-family-53496']
for row in plan['geometry']:
 path=args.context/(row['name']+'.obj');assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];vertices=[];faces=[]
 for line in path.read_text().splitlines():
  if line.startswith('v '):vertices.append(tuple(map(float,line.split()[1:4])))
  elif line.startswith('f '):faces.append(tuple(int(v.split('/')[0])-1 for v in line.split()[1:]))
 geometry=data(row['name'],vertices,faces,bpy.data.materials['Masonry']);o=place(geometry,row['name'],(0,0,0))
 if row['name']in family9_names:legacy.append(o);witness.append(place(geometry,row['name']+' full original physical witness',(0,0,0)))
 else:common.append(o)
venues=json.loads((ROOT/'Docs/FantasyRenaissance/FR01/source-plan.json').read_text())['venues'];venue_seeds={r['seed']:r['id']for r in venues};fixed={r['name'].replace('retained-service-','')for r in plan['geometry']if r['name'].startswith('retained-service-')};approximate=[]
for row in plan['buildings']:
 venue=venue_seeds.get(row['seed'])
 if venue and venue not in fixed:
  x0,z0,x1,z1=row['rect'];bpy.ops.mesh.primitive_cube_add(size=1,location=((x0+x1)/2,-(z0+z1)/2,row['height']/2));o=bpy.context.object;o.name='Approximate unchanged service envelope / '+venue;o.scale=(x1-x0,z1-z0,row['height']);o.data.materials.append(bpy.data.materials['Masonry']);common.append(o);approximate.append(venue)
for venue in venues:
 module='WRArchiveCrown'if venue['id']=='archive'else'WROfficeCrown'if venue['id']=='office'else None
 if not module:continue
 copy=source[module+'_LOD0'].data.copy()
 for v in copy.vertices:v.co.y=-v.co.y
 copy.flip_normals();rewritten.append(place(copy,'Actual fitted '+module,venue['origin'],venue['yaw']))
for building in plan['buildings']:
 module='WRSkyline'+str(building['seed'])
 if module+'_LOD0'not in source:continue
 x0,z0,x1,z1=building['rect'];copy=source[module+'_LOD0'].data.copy()
 for v in copy.vertices:v.co.y=-v.co.y
 copy.flip_normals();rewritten.append(place(copy,'Exact fitted '+module,[(x0+x1)/2,.15,(z0+z1)/2]))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=760;scene.render.resolution_percentage=100;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.35,.42,.53,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.7
bpy.ops.mesh.primitive_plane_add(size=1500);floor=bpy.context.object;floor.name='Neutral source reference ground, not native terrain';floor.data.materials.append(bpy.data.materials['Masonry']);bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.rotation_euler=(.45,-.35,-.7);sun.data.energy=3;bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.lens=30;camera.data.clip_end=1500
bpy.ops.object.text_add();label=bpy.context.object;label.parent=camera;label.location=(-.65,.385,-1.2);label.data.size=.019;label.data.body='DCC SOURCE REVIEW / NOT UNITY';lm=bpy.data.materials.new('WR01 DCC evidence label');lm.use_nodes=True;nodes=lm.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.8,.3,1);out=nodes.new('ShaderNodeOutputMaterial');lm.node_tree.links.new(em.outputs[0],out.inputs[0]);label.data.materials.append(lm)
views=[('Home_North_Legacy_DCC',(0,-28,1.8),(-4,135,18),False),('Home_North_Rewrite_DCC',(0,-28,1.8),(-4,135,18),True),('North_Civic_Legacy_DCC',(-10,72,1.8),(-6,146,16),False),('North_Civic_Rewrite_DCC',(-10,72,1.8),(-6,146,16),True),('West_Civic_Legacy_DCC',(-16,-30,1.8),(-68,-58,14),False),('West_Civic_Rewrite_DCC',(-16,-30,1.8),(-68,-58,14),True),('Archive_High_Oblique_Rewrite_DCC',(-60,82,35),(-6,145,18),True)]
for name,eye,target,enabled in views:
 for group,active in [(legacy,not enabled),(witness,enabled),(rewritten,enabled)]:
  for o in group:o.hide_render=not active
 for o in common:o.hide_render=False
 camera.location=(eye[0],-eye[1],eye[2]);aim=Vector((target[0],-target[1],target[2]));camera.rotation_euler=(aim-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(args.output/(name+'.png'));bpy.ops.render.render(write_still=True)
(args.output/'dcc-views.json').write_text(json.dumps({'scope':'Matched DCC compositions using actual retained CMH1 high/low meshes and compiled original structure. Not Unity screenshots, runtime admission, gameplay, collision or performance proof. Fresh recovery run.','source_context_sha256':hashlib.sha256(context_path.read_bytes()).hexdigest(),'source_context':'Historical compiled FR02A source context; unchanged layout/H3/H5/family9 geometry is retained. WR01 splits presentation only. These comparisons isolate WR01 with FR01/FR02A optional art absent in both states.','retained_placements':len(plan['art_parts']),'retained_geometry_files':len(plan['geometry']),'actual_h5_low_source_witnesses':len(witness)-3,'actual_family9_full_source_witnesses':3,'actual_collision_witness_roots':len(witness),'suppressed_high_parts':suppressed,'art_source_sha256':manifest['source_sha256'],'image_receipts':[{'path':str(args.output/(v[0]+'.png')),'bytes':(args.output/(v[0]+'.png')).stat().st_size,'sha256':hashlib.sha256((args.output/(v[0]+'.png')).read_bytes()).hexdigest(),'native_unity':'NOT RUN'}for v in views],'approximate_service_ids':approximate,'native_ground_and_actors':'NOT SIMULATED','validated_mesh_references':references,'views':views,'native_unity':'NOT RUN'},indent=2)+'\n')
print('WR01 DCC source reviews completed',flush=True)
