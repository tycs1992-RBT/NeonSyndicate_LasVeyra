#!/usr/bin/env python3
"""Original reusable stone, wilderness and cavern meshes for the protected existing-world art review."""
import bpy,math,random,json,hashlib
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'Assets/NeonSyndicate/Art/WorldFoundationH8R';out.mkdir(exist_ok=True);src=root/'ArtSource/WorldFoundationH8R';src.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mats={}
for n,c in [('MineralStone',(.065,.077,.091,1)),('WeatheredEdge',(.16,.15,.14,1)),('Amethyst',(.15,.035,.22,1)),('Azure',(.025,.19,.22,1)),('Bark',(.065,.046,.034,1)),('Leaf',(.025,.095,.059,1))]:
 m=bpy.data.materials.new(n);m.diffuse_color=c;m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=c;bs.inputs['Roughness'].default_value=.82 if n not in ['Amethyst','Azure']else .25;mats[n]=m
 if n in ['Amethyst','Azure']:bs.inputs['Emission Color'].default_value=c;bs.inputs['Emission Strength'].default_value=.22
modules=[];parts=[]
def active(o):bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def mesh(n,vs,fs,mat):
 d=bpy.data.meshes.new(n);d.from_pydata(vs,[],fs);d.update();o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);d.materials.append(mats[mat]);parts.append(o);return o
def block(c,size,mat='MineralStone',bevel=.04):
 bpy.ops.mesh.primitive_cube_add(size=1,location=c);o=bpy.context.object;o.scale=size;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mats[mat]);parts.append(o)
 if bevel:
  m=o.modifiers.new('Dressed stone edge','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
 return o
def tube(points,radii,mat,sides=12,ridge=.04):
 vs=[];fs=[]
 for j,p in enumerate(points):
  p=Vector(p);axis=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized();right=axis.cross(Vector((0,1,0))).normalized()
  if right.length<.1:right=Vector((1,0,0))
  up=right.cross(axis).normalized()
  for i in range(sides):a=i*2*math.pi/sides;r=radii[j]*(1+ridge*math.sin(i*3+j*.7));vs.append(tuple(p+right*math.cos(a)*r+up*math.sin(a)*r))
 for j in range(len(points)-1):
  for i in range(sides):a=j*sides+i;b=j*sides+(i+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.append(tuple(range(sides-1,-1,-1)));fs.append(tuple((len(points)-1)*sides+i for i in range(sides)));return mesh('Flowing natural volume',vs,fs,mat)
def arch(width,height,depth,stone=True):
 r=width/2;spring=height-r
 for sign in [-1,1]:
  for k in range(5):block((sign*(r+.25),0,spring*(k+.5)/5),(.5,depth,spring/5-.015))
 for k in range(20):
  a=k*math.pi/20;b=(k+1)*math.pi/20;vs=[]
  for y in [-depth/2,depth/2]:
   for rr,t in [(r,a),(r,b),(r+.5,b),(r+.5,a)]:vs.append((rr*math.cos(t),y,spring+rr*math.sin(t)))
  mesh('Radial structural voussoir',vs,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'WeatheredEdge'if k%4==0 else'MineralStone')
def crystal(c,h,mat):
 rng=random.Random(int(h*100)+len(parts));points=[]
 for z,r in [(0,h*.14),(h*.75,h*.16),(h,0)]:
  for i in range(6):a=i*math.pi/3;points.append((c[0]+r*math.cos(a),c[1]+r*math.sin(a),c[2]+z))
 fs=[]
 for j in range(2):
  for i in range(6):fs.append((j*6+i,j*6+(i+1)%6,(j+1)*6+(i+1)%6,(j+1)*6+i))
 mesh('Mineral growth',points,fs,mat)
def finish(name,clearance=None):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',mesh_smooth_type='FACE')
 modules.append({'name':name,'vertices':len(o.data.vertices),'triangles':sum(len(f.vertices)-2 for f in o.data.polygons),'clearance':clearance,'source_bounds':[list(v)for v in o.bound_box],'sha256':hashlib.sha256((out/(name+'.fbx')).read_bytes()).hexdigest()});o.location.x=len(modules)*12;parts=[]
arch(5,5.3,1.5)
for sign in [-1,1]:
 for j in range(6):block((sign*(3.1+.11*j),.35, .25+j*.55),(.8-.07*j,1.9-.14*j,.55))
 crystal((sign*3.05,-.9,0),1.6,'Amethyst')
finish('SanctuaryPortal',{'walk_width':5,'walk_height':5.3,'topology_change':False})
for k in range(4):
 arch(5,5.3,.20)
 for o in parts[-30:]:o.location.y+=k*1.2
finish('ProcessionalVault',{'walk_width':5,'walk_height':5.3})
for j in range(12):block((0,j*.30,(j+1)*.09),(2.8,.30,(j+1)*.18),bevel=.015)
finish('SanctuaryStair',{'rise':2.16,'run':3.6,'not_placed_in_revision_10':True})
# One continuous irregular rock arch, with a genuinely open passage.
N=40;vs=[];fs=[]
for y in [-1.2,0,1.2]:
 for radius in [2.65,3.55]:
  for i in range(N+1):
   t=i*math.pi/N;r=radius+.09*math.sin(t*9+y);vs.append((r*math.cos(t),y+.12*math.sin(t*7),.15+r*math.sin(t)))
stride=2*(N+1)
for j in range(2):
 for ring in range(2):
  for i in range(N):a=j*stride+ring*(N+1)+i;fs.append((a,a+1,a+1+stride,a+stride))
for j in [0,2]:
 for i in range(N):a=j*stride+i;fs.append((a,a+1,a+N+2,a+N+1))
mesh('Stratified cave mouth',vs,fs,'MineralStone')
for sign in [-1,1]:
 for k in range(5):crystal((sign*(2.8+k*.1),-.7+k*.3,0),.7+k*.13,'Azure')
finish('RootSpringArch',{'open_width':5.3,'open_height':2.65,'no_new_story_entrance':True})
rng=random.Random(40883)
for k in range(6):
 a=k*math.pi/3;tube([(0,0,0),(math.cos(a)*.5,math.sin(a)*.5,.25),(math.cos(a)*1.5,math.sin(a)*1.5,.03)],[.28,.15,.025],'Bark')
tube([(0,0,.05),(.18,.05,1.4),(-.10,.12,2.8),(.1,.03,4.3)],[.35,.30,.22,.08],'Bark',16,.13)
for k in range(7):
 a=k*2.399;end=Vector((math.cos(a)*1.6,math.sin(a)*1.6,3.6+k*.12));tube([(0,0,2.1+k*.17),tuple(end*.7+Vector((0,0,1))),tuple(end)],[.13,.08,.015],'Bark')
 for l in range(26):
  c=end+Vector((rng.uniform(-.7,.7),rng.uniform(-.7,.7),rng.uniform(-.25,.35)));dx=Vector((.22,0,.04));dy=Vector((0,.13,0));mesh('Leaf spray',[tuple(c-dx),tuple(c+dy),tuple(c+dx),tuple(c-dy)],[(0,1,2),(0,2,3)],'Leaf')
finish('ShardwoodElder',{'solid_trunk_radius':.35,'canopy_height':4.8})
for k in range(5):tube([(-2+k*.3,-1,.05),(-.7,0,.3),(1.2,1,.18),(2.1,1.4,.04)],[.12,.16,.11,.015],'Bark')
for k in range(9):crystal((rng.uniform(-1.8,1.8),rng.uniform(-.8,.8),.02),rng.uniform(.35,1.3),'Azure')
finish('RootMineralBank',{'visual_side_dressing':True,'not_a_new_floor':True})
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(src/'WorldFoundationH8R.blend'),compress=True)
(out/'manifest.json').write_text(json.dumps({'license':'CC0-1.0','creator':'Original project environment mesh authoring','modules':modules,'native_unity':'NOT RUN','performance_accepted':False,'preview':'Explicit Editor overlay in existing revision 10 only, no default promotion'},indent=2)+'\n');(src/'RIGHTS.md').write_text('# Original H8R world modules\n\nOriginal mesh construction and material definitions for this project, released under CC0-1.0. No external meshes or textures. These are a small reusable art foundation, not a rebuilt kingdom. Native import, navigation, appearance and performance remain NOT RUN.\n');print(json.dumps(modules),flush=True)
