"""Original editable Gothic Heart geometry, metres, three authored LODs. Run with Blender Python."""
import bpy,math,json,hashlib,random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Assets/NeonSyndicate/Art/HeartFR01';SRC=ROOT/'ArtSource/HeartFR01';DOC=ROOT/'Docs/FantasyRenaissance/FR01';ART.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
COLORS=[('Masonry',(.25,.22,.18,1)),('CarvedTrim',(.55,.48,.34,1)),('SlateTimber',(.075,.10,.12,1)),('MossRoot',(.075,.16,.09,1)),('AmethystBronze',(.28,.10,.39,1))]
MATS=[]
for name,c in COLORS:
 m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True;n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=c;n.inputs['Roughness'].default_value=.82
 if name=='AmethystBronze':n.inputs['Metallic'].default_value=.25
 MATS.append(m)
parts=[];lod=0
def mesh(name,vs,fs,mat=0):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(MATS[mat]);parts.append(o);return o
def box(name,at,size,mat=0):
 x,y,z=at;a,b,c=[v/2 for v in size];vs=[(x+i*a,y+j*b,z+k*c)for i,j,k in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
 return mesh(name,vs,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat)
def cone(name,at,r,h,mat=1,n=None):
 n=n or [16,10,6][lod];x,y,z=at;v=[(x+r*math.cos(i*2*math.pi/n),y+r*math.sin(i*2*math.pi/n),z)for i in range(n)]+[(x,y,z+h)];return mesh(name,v,[(i,(i+1)%n,n)for i in range(n)]+[tuple(reversed(range(n)))],mat)
def arch(name,x,y,z,half=2,spring=3,depth=1,thick=.45,mat=1):
 # Two true pointed arcs of a masonry arch, each segmented into solid voussoirs.
 a=half*.35;r=half+a;end=math.acos(-a/r);count=[18,10,6][lod]
 for side in [-1,1]:
  for i in range(count):
   t0=math.pi+(end-math.pi)*(i+.025)/count;t1=math.pi+(end-math.pi)*(i+.975)/count;vs=[]
   for yy in [-depth/2,depth/2]:
    for rr,t in [(r,t0),(r,t1),(r+thick,t1),(r+thick,t0)]:vs.append((x+side*(a+rr*math.cos(t)),y+yy,z+spring+rr*math.sin(t)))
   o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)
   # Recalculate outward normals after joining to handle mirrored stones.
 for side in [-1,1]:
  box(name+' pier',(x+side*(half+thick/2),y,z+spring/2),(thick,depth,spring),mat)
def roof(name,x,y,z,w,d,h,mat=2):
 v=[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x-w/2,y+d/2,z),(x+w/2,y+d/2,z),(x,y-d/2,z+h),(x,y+d/2,z+h)]
 mesh(name,v,[(0,2,5,4),(1,4,5,3),(0,4,1),(2,3,5),(0,1,3,2)],mat)
 if lod<2:
  for t in range(1,[8,4][lod]):box('Slate ridge cap',(x,y-d/2+d*t/[8,4][lod],z+h),(.24,.4,.2),1)
def bay(x=0,y=0,z=0,blind=False):
 box('Dressed foundation',(x,y,z+.22),(4,1.3,.44),0)
 arch('Deep pointed bay',x,y,z+.44,1.2,2.5,1,.38,1)
 for side in [-1,1]:
  box('Battered buttress',(x+side*1.8,y+.3,z+2),( .42,1.8,4),0);cone('Pier finial',(x+side*1.8,y+.3,z+4),.35,1.1,1)
 if blind:box('Recessed blind stone',(x,y+.42,z+2),(2.6,.18,3),0)
 else:
  box('Window transom',(x,y,z+2.1),(2.4,.12,.16),1)
  for xx in [-.42,.42]:box('Stone mullion',(x+xx,y,z+2.8),(.12,.15,3.8),1)
 roof('Steep bay gable',x,y,z+5,4.2,1.8,2.1)
def house(variant):
 # Closed nonservice shell, not a new accessible room. Footprint 12 by 14; 14m high.
 for row in range([6,4,2][lod]):
  zz=row*1.45
  for side in [-1,1]:box('Coursed side wall',(side*5.65,0,zz+.72),(.7,14,1.40),0)
 box('Rear masonry',(0,6.7,4.2),(12,.6,8.4),0)
 for xx in [-4,0,4]:bay(xx,-6.7,0,blind=xx==0)
 roof('Long slate roof',0,0,8.5,12.8,14.2,4.8 if variant==0 else 3.5)
 for side in [-1,1]:
  box('Chimney',(side*3.5,3.4,12.4),(1.2,1.2,3),0);box('Chimney cap',(side*3.5,3.4,14),(1.6,1.6,.3),1)
 if variant==1:
  box('Offset lantern base',(3,1.8,12),(3,3,2),0);roof('Lantern hip',3,1.8,13,4,4,4)
 else:cone('Asymmetric bell spire',(-3,2,11),2,5,2)
def gate():
 for side in [-1,1]:
  x=side*18
  box('Stepped foundation',(x,0,.4),(5,5,.8),0)
  for j in range([10,7,4][lod]):box('Ashlar tower course',(x,0,1+j*1.1),(3.6+(j%2)*.12,3.8,1.06),0)
  for dx in [-1,1]:box('Gate buttress',(x+dx*2,1.2,4),(1,5,8),0)
  arch('Tower blind niche',x,-2,3,1.05,3, .4,.26,1)
  box('Crown cornice',(x,0,12.4),(4.8,4.8,.6),1)
  roof('Unequal slate crown',x,0,12.7,5,5,8 if side<0 else 5)
  for dx in [-1,1]:cone('Detached pinnacle',(x+dx*2,1.9,12.5),.45,3.8 if dx<0 else 2.5,1)
 arch('Monumental processional arch',0,0,0,16,7,2.8,.9,1)
 # A suspended carved crest never fills the pedestrian opening.
 box('Structural keystone',(0,0,28.2),(1.7,3.1,1.2),1)
 cone('Amethyst crest',(0,0,28.8),.55,2.0,4)
def growth(ruin=False):
 if ruin:
  for j in range([5,4,3][lod]):box('Broken wall',(j*.65-1.3,0,.5+j*.18),(.64,1.0,1+j*.32),0)
  arch('Ruined rib',0,0,0,1.4,1.8,.6,.28,1)
 else:
  for j in range([6,4,3][lod]):
   cone('Rooted trunk',(math.sin(j)*.16,0,j*.55),.46-j*.045,1.5,2,8)
  for k in range([9,6,4][lod]):
   a=k*2.4;r=1.2+(k%3)*.45;o=cone('Leaf canopy',(math.cos(a)*r,math.sin(a)*r,3+(k%3)*.7),1.5,1.8,3,8)
 for k in range([22,12,6][lod]):
  a=k*2.4;rr=.5+k*.055;cone('Moss fern',(math.cos(a)*rr,math.sin(a)*rr,.03),.22,.6,3,5)
def focus():
 for j in range(3):box('Bronze plinth',(0,0,.15+j*.23),(2-j*.3,2-j*.3,.3),1)
 for k in range(5):a=k*1.256;cone('Amethyst votive',(math.cos(a)*.45,math.sin(a)*.45,.65),.22,1.4+(k%2)*.5,4,6)
MODELS={'HeartGate':gate,'HeartHouseA':lambda:house(0),'HeartHouseB':lambda:house(1),'HeartArcade':lambda:bay(),'HeartBlindBay':lambda:bay(blind=True),'HeartRoofSpire':lambda:cone('Octagonal slate spire',(0,0,0),2.4,8,2),'HeartRoofGable':lambda:roof('Cross-gable',0,0,0,6,5,4),'HeartStreet':lambda:box('Flush stone paving',(0,0,-.015),(4,4,.03),0),'HeartRootCourt':lambda:growth(False),'HeartIvyRuin':lambda:growth(True),'HeartFocus':focus}
records=[];source_objects={}
for name,fn in MODELS.items():
 source_objects[name]=[]
 for lod in range(3):
  parts=[];fn();bpy.ops.object.select_all(action='DESELECT')
  for o in parts:o.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'_LOD'+str(lod)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
  # World metre UV projection, stable across LODs.
  uv=o.data.uv_layers.new(name='MetreUV')
  for poly in o.data.polygons:
   axis=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3)if i!=axis]
   for li in poly.loop_indices:
    co=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]]/4,co[axes[1]]/4)
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
  bpy.ops.object.modifier_add(type='TRIANGULATE');bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
  path=ART/(o.name+'.fbx');bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
  record={'name':name,'lod':lod,'path':path.relative_to(ROOT).as_posix(),'triangles':len(o.data.polygons),'vertices':len(o.data.vertices),'uv_vertices':len(o.data.uv_layers[0].data),'source_bounds':[list(min(v.co[i]for v in o.data.vertices)for i in range(3)),list(max(v.co[i]for v in o.data.vertices)for i in range(3))],'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size};records.append(record);source_objects[name].append(o);o.hide_render=True;o.hide_set(True)
# Arrange high-detail source collection for editable inspection, origins and geometry remain original.
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'GothicHeartFR01.blend'))
manifest={'license':'CC0-1.0','creator':'Original project geometry authored for FR01','external_inputs':[],'axis':'Blender Z-up metres, FBX -Z forward Y-up, baked axis transform','modules':records,'native_unity':'NOT RUN'}
(ART/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');(ART/'CREDITS.txt').write_text('Original Gothic Heart FR01 mesh authoring. CC0-1.0. No external mesh or texture inputs. Editable source ArtSource/HeartFR01/GothicHeartFR01.blend.\n')
print('FR01 exported',len(records),'LOD assets',flush=True)
