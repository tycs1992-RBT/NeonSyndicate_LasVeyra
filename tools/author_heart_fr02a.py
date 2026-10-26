"""Original Living Streets geometry. Blender 4.5+, metres, editable source and three real mesh LODs.
Reuses the delivered FR01 maps by reference. No donor meshes or new texture copies.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/NeonSyndicate/Art/HeartFR02A';SRC=ROOT/'ArtSource/HeartFR02A'
ART.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0
NAMES=['Masonry','CarvedTrim','SlateTimber','MossRoot','AmethystBronze'];MATS=[]
for name in NAMES:
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Roughness'].default_value=.8
 for suffix,target in [('BaseColor','Base Color'),('Normal','Normal')]:
  tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'Assets/NeonSyndicate/Art/HeartFR01'/(name+'_'+suffix+'.png')),check_existing=True)
  if suffix=='Normal':
   tex.image.colorspace_settings.name='Non-Color';normal=m.node_tree.nodes.new('ShaderNodeNormalMap');m.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs[0],n.inputs[target])
  else:m.node_tree.links.new(tex.outputs['Color'],n.inputs[target])
 MATS.append(m)
parts=[];lod=0
def mesh(name,v,f,mat=0):
 d=bpy.data.meshes.new(name);d.from_pydata(v,[],f);d.update();o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);d.materials.append(MATS[mat]);parts.append(o);return o
def box(name,at,size,mat=0):
 x,y,z=at;a,b,c=[s/2 for s in size]
 return mesh(name,[(x+i*a,y+j*b,z+k*c)for i,j,k in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]],[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat)
def beam(name,a,b,width=.15,mat=2):
 delta=Vector(b)-Vector(a);o=box(name,(0,0,0),(width,width,delta.length),mat);o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.location=(Vector(a)+Vector(b))/2;return o
def cylinder(name,at,r,h,mat=2,sides=None):
 n=sides or [12,8,6][lod];x,y,z=at;v=[(x+r*math.cos(i*2*math.pi/n),y+r*math.sin(i*2*math.pi/n),z+k*h)for k in [0,1]for i in range(n)]
 return mesh(name,v,[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n)for i in range(n)],mat)
def roof(name,x,y,z,w,d,rise,offset=0):
 mesh(name,[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x-w/2,y+d/2,z),(x+w/2,y+d/2,z),(x+offset,y-d/2,z+rise),(x+offset,y+d/2,z+rise)],[(0,2,5,4),(1,4,5,3),(0,4,1),(2,3,5),(0,1,3,2)],2)
 beam('Ridge cap',(x+offset,y-d/2-.05,z+rise),(x+offset,y+d/2+.05,z+rise),.22,1)
 for sign in [-1,1]:
  beam('Verge coping',(x+sign*w/2,y-d/2-.04,z),(x+offset,y-d/2-.04,z+rise),.18,1)
  if lod<2:
   count=[7,3][lod]
   for i in range(1,count):
    t=i/count;xx=x+sign*w/2*(1-t)+offset*t
    beam('Slate course',(xx,y-d/2,z+rise*t+.035),(xx,y+d/2,z+rise*t+.035),.065,2)
def arch(x,y,z,half=1,spring=1.6,depth=.55,thick=.22):
 a=half*.35;r=half+a;end=math.acos(-a/r);count=[10,6,4][lod]
 for side in [-1,1]:
  for i in range(count):
   t0=math.pi+(end-math.pi)*i/count;t1=math.pi+(end-math.pi)*(i+1)/count;v=[]
   for yy in [-depth/2,depth/2]:
    for rr,t in [(r,t0),(r,t1),(r+thick,t1),(r+thick,t0)]:v.append((x+side*(a+rr*math.cos(t)),y+yy,z+spring+rr*math.sin(t)))
   mesh('Pointed cut stone',v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],1)
  box('Stone jamb',(x+side*(half+thick/2),y,z+spring/2),(thick,depth,spring),1)
def window(x,y,z):
 # A genuine half-metre recess: jambs and spandrels are on the front plane, dark glazing sits behind.
 box('Recessed glazing',(x,y+.28,z+1.05),(1.4,.09,2.1),2)
 arch(x,y,z,.68,1.3,.3,.16)
 box('Projecting sill',(x,y-.13,z),(1.85,.65,.18),1)
 beam('Stone mullion',(x,y-.1,z+.1),(x,y-.1,z+2.2),.09,1)
 if lod<2:
  for sign in [-1,1]:beam('Tracery fork',(x,y-.1,z+1.3),(x+sign*.45,y-.1,z+1.85),.075,1)
def door(x,y,z=0,w=1.5):
 box('Closed timber door',(x,y+.16,z+1.3),(w,.15,2.6),2);arch(x,y-.08,z,w/2,2.0,.6,.22)
 if lod<2:
  for xx in [-.35,0,.35]:beam('Door plank seam',(x+xx,y+.06,z+.15),(x+xx,y+.06,z+2.4),.045,1)
 box('Bronze latch',(x+w*.26,y-.1,z+1.25),(.13,.18,.13),4)
def facade(w,d,eaves,levels,timber=False):
 # Each story has solid piers and spandrels around real deep window recesses. Course height never changes with LOD.
 box('Foundation plinth',(0,0,.24),(w+.35,d+.35,.48),0)
 for sign in [-1,1]:box('Continuous side masonry',(sign*(w/2-.22),0,eaves/2),(.44,d,eaves),0)
 box('Rear wall',(0,d/2-.22,eaves/2),(w,.44,eaves),0)
 spacing=w/3;front=-d/2
 for story in range(levels):
  bottom=story*(eaves/levels);height=eaves/levels
  box('Floor string course',(0,front,bottom+.17),(w+.15,.6,.34),1)
  box('Window upper spandrel',(0,front,bottom+height-.4),(w,.44,.8),0)
  box('Window lower spandrel',(0,front,bottom+.55),(w,.44,1.1),0)
  for i in range(4):box('Window bay pier',(-w/2+i*spacing,front,bottom+height/2),(.6,.52,height),1 if timber and story>0 else 0)
  for i in range(3):window(-w/2+(i+.5)*spacing,front-.05,bottom+1)
  if timber and story>0:
   for i in range(3):beam('Upper timber brace',(-w/2+i*spacing,front-.34,bottom+.25),(-w/2+(i+1)*spacing,front-.34,bottom+height-.3),.15,2)
 box('Continuous stone eaves',(0,0,eaves-.08),(w+.3,d+.3,.2),1)
 # The door is visibly closed; there is no invented interior or use prompt behind it.
 door(0,front-.32,0,1.4)
 for sign in [-1,1]:
  box('Grounded buttress',(sign*(w/2-.35),front-.25,eaves*.32),(.65,1.25,eaves*.64),0)
  if lod<2:box('Buttress cap',(sign*(w/2-.35),front-.25,eaves*.64),(.85,1.45,.2),1)
def chimney(x,y,z,h):
 box('Masonry chimney',(x,y,z+h/2),(1,1,h),0);box('Chimney flue rim',(x,y,z+h+.05),(1.3,1.3,.25),1);box('Dark flue',(x,y,z+h+.2),(.6,.6,.07),2)
def balcony(w,d,z):
 box('Stone gallery floor',(0,-d/2-.7,z),(w,.95,.25),1)
 for x in [-w/2,w/2]:beam('Gallery bracket',(x,-d/2,z-1),(x,-d/2-1.15,z),.22,2)
 beam('Gallery handrail',(-w/2,-d/2-1.12,z+1.1),(w/2,-d/2-1.12,z+1.1),.12,1)
 for i in range(int(w)+1):beam('Gallery baluster',(-w/2+i,-d/2-1.12,z+.15),(-w/2+i,-d/2-1.12,z+1.1),.09,1)
def dormer(x,y,z):
 box('Dormer cheek',(x,y,z+.6),(1.6,1.9,1.2),0);window(x,y-.95,z);roof('Dormer gable',x,y,z+1.6,2,2.1,1.2)
def building(kind):
 specs=[(7,9,7,2),(8,10,11.4,3),(9,10,7.3,2),(11,10,5.6,1),(9,10,8,2),(14,8,7.6,2),(8,10,5.2,1),(8,10,8.4,2),(11,10,8,2)]
 w,d,h,levels=specs[kind];facade(w,d,h,levels,kind in [2,8])
 if kind==5:
  for x in [-3.7,3.7]:roof('Paired courtyard gable',x,0,h,7.8,d+.9,4)
 elif kind==3:
  roof('Asymmetric workshop roof',-1.4,0,h,8.4,d+.8,3.5,offset=-.9);roof('Lean-to workroom',4,0,3.9,3.2,d+.7,1.3,offset=-.6);chimney(-3,2,h+2,4)
 elif kind==6:
  roof('Retained roof fragment',-2,2,h,4.5,6,2.8);box('Broken upper wall',(-3.8,1,h+1),(.5,7,2),0)
  for i in range(4):box('Broken masonry tooth',(-2+i*1.3,4.7,h+.5+i%2*.4),(1.2,.7,1+i%2*.8),0)
 else:roof('Steep slate main roof',0,0,h,w+.8,d+.8,5 if kind==1 else 3.8,offset=.8 if kind==7 else 0)
 if kind in [0,1,2,4,7,8]:chimney(w*.32,d*.2,h+1.5,3)
 if kind in [1,7]:
  x=-w*.32 if kind==1 else w*.34;cylinder('Octagonal lantern base',(x,2,h+2),1.25,2,0);cylinder('Lantern cornice',(x,2,h+4),1.45,.22,1)
  n=[12,8,6][lod];v=[(x+1.6*math.cos(i*2*math.pi/n),2+1.6*math.sin(i*2*math.pi/n),h+4.2)for i in range(n)]+[(x,2,h+7)]
  mesh('Tapered lantern crown',v,[(i,(i+1)%n,n)for i in range(n)]+[tuple(reversed(range(n)))],2)
 if kind in [0,2,8] and lod<2:dormer(-w*.23,0,h+1.5)
 if kind in [4,8]:balcony(w*.72,d,h*.5);roof('Gallery canopy',0,-d/2-.9,h*.5+2.8,w*.8,2.1,1.2)
 if kind==2:
  stall(0,-d/2-1.15,small=True)
 if kind==4:arch(0,-d/2-.45,0,1.5,2.7,.8,.3)
 if kind==8:
  for x in [-w*.35,w*.35]:beam('Gallery roof post',(x,-d/2-1.1,h*.5),(x,-d/2-1.1,h*.5+2.8),.16,2)
 if lod==0:
  for i in range(9):
   x=-w*.47+(i%3)*.32;y=d*.2+(i//3)*.3;leaf=box('Ivy leaf',(0,0,0),(.32,.06,.42),3);leaf.location=(x,y,.45+(i%4)*.7);leaf.rotation_euler[1]=i*.4
def barrel(x,y,z=0):
 cylinder('Oak barrel',(x,y,z),.38,.9,2)
 for zz in [.12,.72]:cylinder('Barrel hoop',(x,y,z+zz),.405,.08,1)
def stall(x=0,y=0,small=False):
 w=3 if small else 5.5;d=1.5 if small else 3.6;top=2.8 if small else 3.6
 for sign in [-1,1]:
  for end in [-1,1]:beam('Market timber post',(x+sign*w/2,y+end*d/2,0),(x+sign*w/2,y+end*d/2,top),.15,2)
 roof('Shelter roof',x,y,top,w+.55,d+.55,.9)
 box('Merchant counter',(x,y-d*.22,.7),(w*.85,.9,1.4),2)
 # Hanging woven strip follows a physical ridge. No fake vendor or transaction owner.
 mesh('Draped cloth',[(x-w/2,y-d/2-.1,top),(x+w/2,y-d/2-.1,top),(x+w/2,y-d/2-.3,top-.65),(x,y-d/2-.45,top-.8),(x-w/2,y-d/2-.3,top-.65)],[(0,1,2,3,4)],3)
 for i in range([6,4,2][lod]):cylinder('Goods jar',(x-w*.3+i*w*.6/max(1,[6,4,2][lod]-1),y-d*.22,1.4),.12,.32,1)
 if not small:barrel(x+w*.3,y+d*.15);box('Stacked produce crate',(x-w*.3,y+d*.15,.5),(1,1,1),2)
def workyard():
 box('Workbench',(0,0,.95),(3,1.4,.2),2)
 for x in [-1.2,1.2]:
  for y in [-.5,.5]:box('Worktable leg',(x,y,.45),(.18,.18,.9),2)
 barrel(-1.4,1);box('Supply crate',(1.1,1,.55),(1.1,1.1,1.1),2)
 for i in range(3):box('Stored timber',(.3,1.2,1.2+i*.18),(2,.22,.16),2)
def garden():
 for sign in [-1,1]:box('Low garden coping',(0,sign*1.15,.45),(4,.35,.9),0)
 box('Garden earth',(0,0,.28),(3.6,2,.5),3)
 points=[]
 for i in range([12,7,4][lod]):
  x=-1.5+(i%4);y=-.7+(i//4)*.6;cylinder('Shrub stem',(x,y,.5),.07,1.1,2,6)
  points.append((x,y,1.2+(i%3)*.2))
 # A reproducible Geometry Nodes study is kept in the editable file. Realize before production export and UV unwrap.
 growth=mesh('Garden node points',points,[],3);group=bpy.data.node_groups.new('FR02A bounded garden variation LOD'+str(lod),'GeometryNodeTree');group.use_fake_user=True
 group.interface.new_socket(name='Geometry',in_out='INPUT',socket_type='NodeSocketGeometry');group.interface.new_socket(name='Geometry',in_out='OUTPUT',socket_type='NodeSocketGeometry')
 node_in=group.nodes.new('NodeGroupInput');cube=group.nodes.new('GeometryNodeMeshCube');cube.inputs['Size'].default_value=(.7,.55,.5)
 material=group.nodes.new('GeometryNodeSetMaterial');material.inputs['Material'].default_value=MATS[3];group.links.new(cube.outputs['Mesh'],material.inputs['Geometry'])
 scatter=group.nodes.new('GeometryNodeInstanceOnPoints');group.links.new(node_in.outputs['Geometry'],scatter.inputs['Points']);group.links.new(material.outputs['Geometry'],scatter.inputs['Instance'])
 variation=group.nodes.new('FunctionNodeRandomValue');variation.data_type='FLOAT_VECTOR';variation.inputs[0].default_value=(-.25,-.25,-.6);variation.inputs[1].default_value=(.25,.25,.6);variation.inputs['Seed'].default_value=61724+lod;group.links.new(variation.outputs['Value'],scatter.inputs['Rotation'])
 realize=group.nodes.new('GeometryNodeRealizeInstances');group.links.new(scatter.outputs['Instances'],realize.inputs['Geometry']);node_out=group.nodes.new('NodeGroupOutput');group.links.new(realize.outputs['Geometry'],node_out.inputs['Geometry'])
 modifier=growth.modifiers.new('Bounded authored garden variation','NODES');modifier.node_group=group;bpy.context.view_layer.objects.active=growth;growth.select_set(True);bpy.ops.object.modifier_apply(modifier=modifier.name)
 box('Stone garden seat',(0,-1.4,.55),(3.2,.65,.25),1)
def lantern():
 cylinder('Grounded bronze post',(0,0,0),.12,3.5,1)
 for z in [0,.2,2.9]:cylinder('Post collar',(0,0,z),.24,.12,1)
 box('Lantern cage',(0,0,3.3),(.48,.48,.65),4);roof('Lantern cap',0,0,3.65,.75,.75,.4)
MODELS={n:(lambda k=i:building(k))for i,n in enumerate(['StreetTownhouse','StreetResidence','StreetMerchant','StreetWorkshop','StreetGuild','StreetCourtRow','StreetRuin','StreetCorner','StreetGallery'])}
MODELS.update({'CourtMarket':stall,'CourtWorkyard':workyard,'CourtGarden':garden,'CourtLantern':lantern})
records=[]
for name,fn in MODELS.items():
 for lod in range(3):
  parts=[];fn();bpy.ops.object.select_all(action='DESELECT')
  for o in parts:o.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'_LOD'+str(lod)
  for i,m in enumerate(o.data.materials):
   if m is None:o.data.materials[i]=MATS[3]
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
  uv=o.data.uv_layers.new(name='SharedMetreUV')
  for poly in o.data.polygons:
   axis=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3)if i!=axis]
   for li in poly.loop_indices:
    co=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]]/4,co[axes[1]]/4)
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  mod=o.modifiers.new('Explicit triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
  path=ART/(o.name+'.fbx');bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,use_tspace=True,path_mode='RELATIVE')
  records.append({'name':name,'lod':lod,'path':path.relative_to(ROOT).as_posix(),'triangles':len(o.data.polygons),'vertices':len(o.data.vertices),'source_bounds':[[min(v.co[i]for v in o.data.vertices)for i in range(3)],[max(v.co[i]for v in o.data.vertices)for i in range(3)]],'materials':sorted({m.name for m in o.data.materials}),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size})
  o.hide_render=True;o.hide_set(True)
# Portable references to the existing maps, not embedded copies.
for img in bpy.data.images:
 if img.filepath:img.filepath=bpy.path.relpath(img.filepath,start=str(SRC))
source=SRC/'LivingGothicStreets.blend';bpy.ops.wm.save_as_mainfile(filepath=str(source))
old=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartFR01/manifest.json').read_text())
manifest={'license':'CC0-1.0','creator':'Original project geometry for FR02A','source':source.relative_to(ROOT).as_posix(),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'source_bytes':source.stat().st_size,'axis':'Blender Z-up metres, FBX -Z forward Y-up, baked transform','modules':records,'textures':old['textures'],'new_textures':0,'geometry_nodes':'Three retained bounded-garden variation node groups, realized into production meshes before UV unwrap. Architecture authored with reproducible Blender scripts.','external_inputs':[],'native_unity':'NOT RUN'}
(ART/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
(ART/'CREDITS.txt').write_text('Original Living Gothic Streets geometry authored for this project. CC0-1.0. Reuses original FR01 shared texture maps by reference. No external mesh or texture inputs. Editable source ArtSource/HeartFR02A/LivingGothicStreets.blend.\n')
(SRC/'SOURCE_RECEIPT.json').write_text(json.dumps({k:manifest[k]for k in ['source','source_sha256','source_bytes','license','native_unity']}|{'tool':'Blender '+bpy.app.version_string,'recipe':'tools/author_heart_fr02a.py','modules':len(MODELS),'exports':len(records),'new_textures':0},indent=2)+'\n')
print('FR02A authored',len(records),'exports',flush=True)
