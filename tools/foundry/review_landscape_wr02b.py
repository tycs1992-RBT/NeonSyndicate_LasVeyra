"""Actual Blender source loads, FBX reimports and labelled DCC evidence. Never native Unity proof."""
import argparse,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B';SRC=ROOT/'ArtSource/FoundryWR02A/EnvironmentWR02B'
p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);a.output.mkdir(parents=True,exist_ok=True);manifest=json.loads((ART/'manifest.json').read_text());rows=[];sources=[]
for frame in manifest['frames']:
 for suffix in ['author','processed']:
  f=SRC/(frame['name']+'.'+suffix+'.blend');bpy.ops.wm.open_mainfile(filepath=str(f));meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert meshes and all(o.data.uv_layers for o in meshes);sources.append({'path':f.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'meshes':len(meshes),'passed':True})
for row in manifest['modules']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(ROOT/row['path']),use_anim=False);meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons)for o in meshes);uv=all(o.data.uv_layers and all(math.isfinite(c)for v in o.data.uv_layers[0].data for c in v.uv)for o in meshes);normals=all(abs(n.vector.length-1)<.02 for o in meshes for n in o.data.corner_normals);passed=bool(meshes)and triangles==row['triangles']and uv and normals and not any(o.type in ['ARMATURE','LIGHT','CAMERA']for o in bpy.context.scene.objects);assert passed,row['path'];rows.append({'path':row['path'],'sha256':hashlib.sha256((ROOT/row['path']).read_bytes()).hexdigest(),'triangles':triangles,'finiteUV':bool(uv),'unitNormals':normals,'passed':bool(passed)})
(a.output/'blender-reimport.json').write_text(json.dumps({'scope':'Fourteen actual editable Blender loads, twenty-one FBX reimports. DCC geometry evidence only.','sources':sources,'rows':rows,'passed':len(rows),'total':21,'nativeUnity':'NOT RUN'},indent=2)+'\n')
def load(asset,scale,offset):
 before=set(bpy.data.objects)
 with bpy.data.libraries.load(str(SRC/(asset+'.author.blend')),link=False)as(src,dst):dst.objects=[n for n in src.objects]
 for o in dst.objects:
  if o is None:continue
  bpy.context.collection.objects.link(o);o.scale=Vector(scale);o.location=Vector(offset)
 return [o for o in bpy.data.objects if o not in before]
def setup(eye,target,label):
 s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=True;s.render.threads_mode='FIXED';s.render.threads=2;s.render.resolution_x=1200;s.render.resolution_y=900;s.render.resolution_percentage=100
 if s.world is None:s.world=bpy.data.worlds.new('DCC neutral daylight')
 s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.3,.4,.5,1);s.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.6
 bpy.ops.object.light_add(type='SUN');bpy.context.object.rotation_euler=(.5,-.3,-.6);bpy.context.object.data.energy=3
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.clip_end=1000;cam.data.lens=45;s.camera=cam
 bpy.ops.object.text_add();o=bpy.context.object;o.parent=cam;o.location=(-.43,.29,-1.15);o.data.size=.014;o.data.body='DCC SOURCE REVIEW / NOT UNITY\n'+label;m=bpy.data.materials.new('Evidence label');m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Emission Color'].default_value=(1,.7,.2,1);n.inputs['Emission Strength'].default_value=2;o.data.materials.append(m)
 bpy.ops.mesh.primitive_plane_add(size=500);ground=bpy.context.object;ground.location.z=-.04;m=bpy.data.materials.new('Neutral DCC floor');m.diffuse_color=(.15,.16,.13,1);ground.data.materials.append(m)
def render(name):
 f=a.output/(name+'.png');bpy.context.scene.render.filepath=str(f);bpy.ops.render.render(write_still=True);return {'path':str(f),'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'nativeUnity':'NOT RUN'}
images=[];bpy.ops.wm.read_factory_settings(use_empty=True)
load('SanctuaryCedar',(12,12,10),(-9,0,6));load('AzureElder',(13,13,9),(8,1,6));load('RootFernBank',(1,1,1),(0,-3,0))
# Neutral trunk witnesses only: these represent the existing crown-to-trunk attachment, not authored replacement physics.
for x in [-9,8]:
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.45,depth=7,location=(x,0,3.5));bpy.context.object.name='Neutral original trunk attachment witness'
setup((32,-40,24),(0,0,9),'Two original crown families + rooted understory. Existing physical trunks retained.');images.append(render('Forest_Families_DCC'))
bpy.ops.wm.read_factory_settings(use_empty=True);load('ErodedCliff61724',(1,1,1),(0,0,0));load('EscarpmentArch',(1,1,1),(32,0,0));setup((75,-74,48),(15,0,13),'Left: original cliff frame with authored erosion. Right: INACTIVE new physical arch candidate.');images.append(render('Geology_Candidates_DCC'))
(a.output/'DCC_EVIDENCE.json').write_text(json.dumps({'scope':'Actual original Blender source, labelled DCC renders. Neutral trunk/floor witnesses are not game geometry. No Unity lighting, import, collision, ordinary-play or artistic acceptance claim.','images':images,'nativeUnity':'NOT RUN'},indent=2)+'\n');print('DCC review complete',flush=True)
