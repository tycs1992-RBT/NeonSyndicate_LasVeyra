"""Real Blender source/reimport review and labelled DCC frames. Never Unity screenshots or gameplay acceptance."""
import argparse,hashlib,json,math,struct,sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--archive-context',type=Path,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True);rows=[];sources=[]
for asset in ['SanctuaryArchive','CrystalBasiliskCandidate']:
 manifest=json.loads((ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A'/asset/'processed.json').read_text())
 for suffix in ['author','processed']:
  f=ROOT/'ArtSource/FoundryWR02A'/(asset+'.'+suffix+'.blend');bpy.ops.wm.open_mainfile(filepath=str(f));sources.append({'path':f.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'editableMeshes':sum(o.type=='MESH'for o in bpy.data.objects),'passed':True})
 for row in manifest['outputs']:
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30;bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=True);arms=[o for o in bpy.data.objects if o.type=='ARMATURE'];helpers={p.custom_shape for arm in arms for p in arm.pose.bones if p.custom_shape};meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'and o not in helpers];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons)for o in meshes);uv=all(o.data.uv_layers and all(math.isfinite(c)for v in o.data.uv_layers[0].data for c in v.uv)for o in meshes);normals=all(abs(n.vector.length-1)<.02 for o in meshes for n in o.data.corner_normals);passed=bool(meshes)and triangles==row['triangles']and uv and normals
  rows.append({'path':row['path'],'triangles':triangles,'meshes':len(meshes),'finiteUV':bool(uv),'unitNormals':normals,'actions':len(bpy.data.actions),'passed':bool(passed)})
report={'scope':'Actual four editable Blender loads and six FBX reimports. DCC only.','sources':sources,'rows':rows,'passed':sum(r['passed']for r in rows),'total':len(rows),'nativeUnity':'NOT RUN'};(a.output/'blender-reimport.json').write_text(json.dumps(report,indent=2)+'\n');assert report['passed']==6
images=[]
def setup(eye,target,label):
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 if scene.world is None:scene.world=bpy.data.worlds.new('DCC review lighting')
 scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.32,.4,.52,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.65
 bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.rotation_euler=(.4,-.4,-.55);sun.data.energy=3
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.lens=45;cam.data.clip_end=1000;cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
 bpy.ops.object.text_add();o=bpy.context.object;o.parent=cam;o.location=(-.43,.29,-1.15);o.data.size=.014;o.data.body='DCC ASSET REVIEW / NOT UNITY\n'+label;mat=bpy.data.materials.new('DCC evidence label');mat.use_nodes=True;shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Emission Color'].default_value=(1,.72,.24,1);shader.inputs['Emission Strength'].default_value=2;o.data.materials.append(mat)
 bpy.ops.mesh.primitive_plane_add(size=300);ground=bpy.context.object;ground.location.z=-.02;material=bpy.data.materials.new('Neutral DCC ground, no native physics');material.diffuse_color=(.12,.13,.12,1);ground.data.materials.append(material)
 return scene,cam
def render(name):
 scene=bpy.context.scene;scene.render.filepath=str(a.output/(name+'.png'));bpy.ops.render.render(write_still=True);f=Path(scene.render.filepath);images.append({'file':f.name,'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'nativeUnity':'NOT RUN'})
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/FoundryWR02A/SanctuaryArchive.processed.blend'))
context=json.loads((a.archive_context/'world-manifest.json').read_text())
def neutral_mesh(name,vertices,faces,material=None):
 data=bpy.data.meshes.new(name);data.from_pydata([(x,z,y)for x,y,z in vertices],[],faces);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o)
 if material:data.materials.append(material)
 return o
masonry=bpy.data.materials.new('Neutral retained H5 source geometry, not native material evidence');masonry.diffuse_color=(.28,.3,.31,1)
for row in context['geometry']:
 f=a.archive_context/(row['name']+'.obj');assert hashlib.sha256(f.read_bytes()).hexdigest()==row['sha256'];vertices=[];faces=[]
 for line in f.read_text().splitlines():
  v=line.split()
  if v[0]=='v':vertices.append(tuple(map(float,v[1:4])))
  elif v[0]=='f':faces.append(tuple(int(x)-1 for x in v[1:]))
 neutral_mesh('Actual retained H5 '+row['name'],vertices,faces,masonry)
for part in context['parts']:
 low=part['physicalWitness'];f=ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Meshes'/(part['model']+('_low'if low else'_high')+'.bytes');raw=f.read_bytes();assert raw[:4]==b'CMH1';count=struct.unpack_from('<I',raw,4)[0];values=[struct.unpack_from('<8f',raw,8+i*32)for i in range(count)];angle=math.radians(part['yaw']);c,s=math.cos(angle),math.sin(angle);p=part['position'];scale=part['scale'];vertices=[]
 for v in values:
  x,y,z=[v[i]*scale[i]for i in range(3)];vertices.append((p[0]+c*x+s*z,p[1]+y,p[2]-s*x+c*z))
 neutral_mesh('Actual retained H5 source '+part['model'],vertices,[(i,i+1,i+2)for i in range(0,count,3)],masonry)
setup((51,-61,40),(0,20,17),'Archive crown + retained H5 source base in neutral material. No gameplay simulation.');render('Sanctuary_Archive_DCC')
cam=bpy.context.scene.camera;cam.location=(28,-37,22);cam.rotation_euler=(Vector((0,4,17))-cam.location).to_track_quat('-Z','Y').to_euler();render('Sanctuary_Detail_DCC')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/FoundryWR02A/CrystalBasiliskCandidate.processed.blend'));arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
scene,cam=setup((3.5,4.3,2.2),(0,0,.35),'Original Crystal Basilisk candidate. Motion/contact acceptance NOT RUN.')
for action,frame in [('Idle',1),('Walk',18),('Attack',27),('Impact',13),('Death',58)]:
 if arm.animation_data:
  for track in arm.animation_data.nla_tracks:track.mute=True
 arm.animation_data.action=next(x for x in bpy.data.actions if 'Foundry_'+action in x.name);scene.frame_set(frame);render('Basilisk_'+action+'_DCC')
(a.output/'DCC_EVIDENCE.json').write_text(json.dumps({'scope':'Labelled DCC source review using actual processed assets, actual retained H5 source geometry, original skeleton and procedural pose replay. No Unity lighting, camera, NPC, collision, service interaction or ordinary-play screenshot is claimed.','archiveContextSource':context['sourceCommit'],'images':images,'nativeUnity':'NOT RUN'},indent=2)+'\n');print('DCC sources/reimport/render complete',flush=True)
