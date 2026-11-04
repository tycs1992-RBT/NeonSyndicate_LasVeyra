"""Actual retained CMH1 geometry, FBX reimports and matched DCC comparisons. Never Unity evidence."""
import bpy,math,json,argparse,sys,struct,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--context',type=Path,required=True);p.add_argument('--baseline-root',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]if '--'in sys.argv else[]);a.output.mkdir(parents=True,exist_ok=True)
ART=ROOT/'Assets/NeonSyndicate/Art/HeartFR01';plan=json.loads((a.context/'world-manifest.json').read_text());manifest=json.loads((ART/'manifest.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/HeartFR01/GothicHeartFR01.blend'))
source={o.name:o for o in bpy.data.objects if o.type=='MESH'};oldsource={}
with bpy.data.libraries.load(str(a.baseline_root/'ArtSource/HeartFR01/GothicHeartFR01.blend'),link=False)as (available,requested):requested.objects=[n for n in available.objects if n.startswith(('HeartGate_','HeartHouseA_','HeartHouseB_'))]
for o in requested.objects:
 if o is not None:oldsource[o.name.split('.')[0]]=o
checks=[]
for row in manifest['modules']:
 prior=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=False);objects=[o for o in bpy.data.objects if o not in prior];meshes=[o for o in objects if o.type=='MESH'];vs=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 bounds=[[min(v[i]for v in vs)for i in range(3)],[max(v[i]for v in vs)for i in range(3)]]
 ok=bool(meshes)and all(o.data.uv_layers and o.data.vertices for o in meshes)and max(abs(bounds[j][i]-row['source_bounds'][j][i])for j in range(2)for i in range(3))<.015
 checks.append({'path':row['path'],'sha256':hashlib.sha256((ROOT/row['path']).read_bytes()).hexdigest(),'passed':bool(ok),'bounds':bounds})
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
assert all(r['passed']for r in checks),'Actual FBX reimport failed'
(a.output/'dcc-import.json').write_text(json.dumps({'scope':'Blender 4.5.3 actual FBX reimport, not Unity','passed':sum(r['passed']for r in checks),'total':len(checks),'rows':checks,'native_unity':'NOT RUN'},indent=2)+'\n')
for o in bpy.data.objects:o.hide_render=True
old=[];new=[];legacy=[];common=[];meshcache={};static=[];refs=[]
def place(data,name,position,yaw=0,scale=(1,1,1)):
 o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=(position[0],-position[2],position[1]);o.rotation_euler[2]=math.radians(yaw);o.scale=(scale[0],scale[2],scale[1]);return o
def world(v,position,yaw,scale):
 v=np.asarray(v)*np.asarray(scale);r=math.radians(yaw);c,s=math.cos(r),math.sin(r);rot=np.array([[c,0,s],[0,1,0],[-s,0,c]]);return v@rot.T+position
def data(name,v,faces,material):
 m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y)for x,y,z in v],[],faces);m.materials.append(material);m.update();return m
materials={}
for row in json.loads((ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/asset_manifest.json').read_text())['models']:materials[row['id']]=row['role']
def surface(role):
 key='Retained '+role;m=bpy.data.materials.get(key)
 if m:return m
 m=bpy.data.materials.new(key);m.use_nodes=True;node=m.node_tree.nodes.get('Principled BSDF');node.inputs['Roughness'].default_value=.82
 path=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Textures'/(role+'_Albedo.png')
 assert path.is_file(),str(path)
 tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);m.node_tree.links.new(tex.outputs['Color'],node.inputs['Base Color']);return m
def cmh(model,low=False):
 path=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Meshes'/(model+('_low'if low else'_high')+'.bytes')
 raw=path.read_bytes();assert raw[:4]==b'CMH1';n=struct.unpack_from('<I',raw,4)[0];assert len(raw)==8+n*32 and n%3==0
 v=np.frombuffer(raw,offset=8,dtype='<f4').reshape(n,8);return path,v
for row in plan['art_parts']:
 path,v=cmh(row['model']);key=str(path)
 if key not in meshcache:
  meshcache[key]=data(row['model'],v[:,:3],[(i,i+1,i+2)for i in range(0,len(v),3)],surface(materials[row['model']]))
  uv=meshcache[key].uv_layers.new()
  for i,item in enumerate(uv.data):item.uv=v[i,6:8]
  refs.append({'path':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
 common.append(place(meshcache[key],row['owner']+' / '+row['model'],row['position'],row['yaw'],row['scale']))
 if row['model']not in ['fern_colony','record_lens','castellan_mantle']:
  _,v=cmh(row['model'],True);static.append(world(v[:,:3],row['position'],row['yaw'],row['scale']).reshape(-1,3,3))
def objread(path):
 v=[];f=[]
 for line in path.read_text().splitlines():
  if line.startswith('v '):v.append(tuple(map(float,line.split()[1:4])))
  elif line.startswith('f '):f.append(tuple(int(x.split('/')[0])-1 for x in line.split()[1:]))
 return np.asarray(v),np.asarray(f,dtype=int)
eligible={r['seed']for r in plan['buildings']if r['eligible']}
for row in plan['geometry']:
 path=a.context/(row['name']+'.obj');assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];v,f=objread(path);static.append(v[f]);o=place(data(row['name'],v,f,bpy.data.materials['Masonry']),row['name'],(0,0,0))
 if row['name'].startswith('retained-family-')and int(row['name'].split('-')[-1])in eligible:legacy.append(o)
 else:common.append(o)
# Keep the five unrefined service envelopes visible, rather than omit potentially modern background masses.
prior_layout=json.loads((a.baseline_root/'Docs/FantasyRenaissance/FR01/source-plan.json').read_text())
venues_by_seed={r['seed']:r['id']for r in prior_layout['venues']};fixed={r['name'].replace('retained-service-','')for r in plan['geometry']if r['name'].startswith('retained-service-')};approximate_services=[]
for row in plan['buildings']:
 venue=venues_by_seed.get(row['seed'])
 if venue and venue not in fixed:
  x0,z0,x1,z1=row['rect'];bpy.ops.mesh.primitive_cube_add(size=1,location=((x0+x1)/2,-(z0+z1)/2,row['height']/2));o=bpy.context.object;o.name='Approximate unchanged service envelope / '+venue;o.scale=(x1-x0,z1-z0,row['height']);o.data.materials.append(bpy.data.materials['Masonry']);common.append(o);approximate_services.append(venue)
triangles=np.concatenate(static);mins=triangles.min(axis=1);maxs=triangles.max(axis=1)
def solid(center,ext,yaw):
 angle=math.radians(yaw);c,s=math.cos(angle),math.sin(angle);rot=np.array([[c,0,s],[0,1,0],[-s,0,c]]);center=np.asarray(center);ext=np.asarray(ext);aabb=np.abs(rot)@ext
 ts=triangles[np.all(maxs>=center-aabb,axis=1)&np.all(mins<=center+aabb,axis=1)]
 if not len(ts):return False
 ts=(ts-center)@rot;e=[ts[:,1]-ts[:,0],ts[:,2]-ts[:,1],ts[:,0]-ts[:,2]];axes=[np.broadcast_to(x,(len(ts),3))for x in np.eye(3)]+[np.cross(e[0],e[1])]+[np.cross(edge,x)for edge in e for x in np.eye(3)];separated=np.zeros(len(ts),dtype=bool)
 for axis in axes:
  projection=np.einsum('nvi,ni->nv',ts,axis);r=np.abs(axis)@ext;separated|=(projection.min(axis=1)>r+1e-5)|(projection.max(axis=1)<-r-1e-5)
 return bool(np.any(~separated))
def gateclear(row):
 at=np.array(row['position']);angle=math.radians(row['yaw']);right=np.array([math.cos(angle),0,-math.sin(angle)])
 return not any(solid(at+right*sign*9+[0,10.6,0],[3.2,10.4,3.4],row['yaw'])for sign in [-1,1])and not solid(at+[0,10.9,0],[7.9,5.3,1.8],row['yaw'])
admissions=[]
for row in plan['candidates']:
 valid=row['source_admitted']and gateclear(row);admissions.append({**row,'static_source_clear':valid})
valid=[r for r in admissions if r['static_source_clear']];gate=valid[0]if valid else None
(a.output/'dcc-static-admission.json').write_text(json.dumps({'scope':'Triangle versus oriented-box source probes on retained H3/H5 and original family geometry. Dynamic actors, other owners and triggers are not simulated. NOT native admission.','candidates':admissions,'selected_source_proposal':gate,'native_unity':'NOT RUN'},indent=2)+'\n')
def fit(o,row,correct):
 coords=np.asarray([v.co[:]for v in o.data.vertices]);mn=coords.min(axis=0);mx=coords.max(axis=0);size=mx-mn;center=(mn+mx)*.5;x0,z0,x1,z1=row['rect'];scale=[(x1-x0)/size[0],row['height']/size[2],(z1-z0)/size[1]];at=[(x0+x1)/2,.15,(z0+z1)/2]
 if correct:at=[at[0]-center[0]*scale[0],at[1]-mn[2]*scale[1],at[2]+center[1]*scale[2]]
 return place(o.data,'Imported '+o.name,at,0,scale)
for row in plan['buildings']:
 if row['eligible']:
  name=('HeartHouseA'if row['seed']%2==0 else'HeartHouseB')+'_LOD0';new.append(fit(source[name],row,True));old.append(fit(oldsource[name],row,False))
prior_gate=None
for row in plan['legacy_candidates']:
 if row['source_admitted']:
  at=np.array(row['position']);angle=math.radians(row['yaw']);right=np.array([math.cos(angle),0,-math.sin(angle)])
  if not any(solid(at+right*sign*22+[0,6,0],[3.2,5.9,2.6],row['yaw'])for sign in [-1,1]):prior_gate=row;break
if prior_gate:old.append(place(oldsource['HeartGate_LOD0'].data,'Prior FR01 stretched gate',prior_gate['position'],prior_gate['yaw'],(11/9,1,1)))
for row in plan['legacy_sides']:
 at=np.array(row['position']);radius=row['radius']
 if row['source_clear']and not solid(at+[0,3.6,0],[radius,3.3,radius],0):
  old.append(place(source[row['module']+'_LOD0'].data,'Prior source-admitted side piece',at,row['yaw']))
  if row['module']not in ['HeartArcade','HeartBlindBay']:new.append(place(source[row['module']+'_LOD0'].data,'Retained source side piece',at,row['yaw']))
if gate:new.append(place(source['HeartGate_LOD0'].data,'Compact source-admitted gateway',gate['position'],gate['yaw']))
for row in plan['routes']:
 if row['id']not in ['home-court','court-crossing','forge-market','sanctuary-approach']:continue
 for start,end in zip(row['points'],row['points'][1:]):
  d=np.array(end)-start;length=math.hypot(d[0],d[2]);n=math.ceil(length/4)
  for i in range(n):
   at=np.array(start)+d*((i+.5)/n)+[0,.021,0];mesh=source['HeartStreet_LOD0'].data;scale=(row['width']/4,1,length/n/4);new.append(place(mesh,'Source stone paving',at,math.degrees(math.atan2(d[0],d[2])),scale));old.append(place(mesh,'Prior source stone paving',at,math.degrees(math.atan2(d[0],d[2])),scale))
sidecount=0
for row in plan['sides']:
 at=np.array(row['position']);safe=row['source_clear']
 if gate:
  d=at-gate['position'];ang=math.radians(gate['yaw']);safe=safe and(abs(d[0]*math.cos(ang)-d[2]*math.sin(ang))>15 or abs(d[0]*math.sin(ang)+d[2]*math.cos(ang))>7.5)
 if safe and not solid(at+[0,3.6,0],[3,3.3,3],0):new.append(place(source[row['module']+'_LOD0'].data,'Source-admitted street bay',at,row['yaw']));sidecount+=1
# Neutral reference ground and matched source lighting, not native terrain, actors or post-processing.
bpy.ops.mesh.primitive_plane_add(size=1500);floor=bpy.context.object;floor.data.materials.append(bpy.data.materials['Masonry'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=1100;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.35,.42,.52,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.7
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.rotation_euler=(.45,-.35,-.7);sun.data.energy=3
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.lens=26;cam.data.clip_end=1500
bpy.ops.object.text_add();label=bpy.context.object;label.parent=cam;label.location=(-.64,.38,-1.2);label.data.size=.02;label.data.body='DCC SOURCE REVIEW / NOT UNITY'
mat=bpy.data.materials.new('FR01E evidence label');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.8,.3,1);out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(em.outputs[0],out.inputs[0]);label.data.materials.append(mat)
views=[('Legacy_OFF_DCC',(-8,-12,1.8),(6,60,4),'legacy',False),('FR01_Before_DCC',(-8,-12,1.8),(6,60,4),'old',False),('FR01E_After_DCC',(-8,-12,1.8),(6,60,4),'new',False),('Street_After_DCC',(7,55,1.8),(12,90,5),'new',False),('Evening_After_DCC',(-8,-12,1.8),(6,60,4),'new',True)]
for name,eye,target,mode,night in views:
 for group,tag in [(new,'new'),(old,'old'),(legacy,'legacy')]:
  for o in group:o.hide_render=tag!=mode
 for o in common:o.hide_render=False
 cam.location=(eye[0],-eye[1],eye[2]);aim=Vector((target[0],-target[1],target[2]));cam.rotation_euler=(aim-cam.location).to_track_quat('-Z','Y').to_euler();sun.data.energy=.3 if night else 3;scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.1 if night else .7
 scene.render.filepath=str(a.output/(name+'.png'));bpy.ops.render.render(write_still=True)
(a.output/'dcc-views.json').write_text(json.dumps({'scope':'Matched DCC source compositions. Actual CMH1 meshes and compiled H3/H5 structural geometry, not Unity. No NPC, native terrain/post, trigger or performance proof. Prior FR01 images skipped CMH1 context; these correct that evidence error.','approximate_retained_service_ids':approximate_services,'retained_placements':len(plan['art_parts']),'retained_geometry_files':len(plan['geometry']),'validated_mesh_references':refs,'source_side_bays_shown':sidecount,'prior_gate_static_source_proposal':prior_gate,'views':views,'native_unity':'NOT RUN'},indent=2)+'\n')
