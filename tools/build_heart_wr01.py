"""Original fitted civic crowns and skyline reliquaries. Run in Blender 4.5+.
Visual-only art, three silhouette LODs, existing FR01 maps, no gameplay or physics owner.
Recovery rebuild of the uncommitted WR01 source after external workspace pruning.
"""
import bpy,math,json,hashlib,uuid
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/NeonSyndicate/Art/HeartWR01';SOURCE=ROOT/'ArtSource/HeartWR01'
ART.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
MATERIAL_NAMES=['Masonry','CarvedTrim','SlateTimber','MossRoot','AmethystBronze'];MATERIALS=[]
for name in MATERIAL_NAMES:
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.78
 for suffix,target in [('BaseColor','Base Color'),('Normal','Normal')]:
  path=ROOT/'Assets/NeonSyndicate/Art/HeartFR01'/(name+'_'+suffix+'.png')
  if path.read_bytes()[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('Hydrate original shared map: '+str(path))
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path),check_existing=True)
  if suffix=='Normal':
   t.image.colorspace_settings.name='Non-Color';n=m.node_tree.nodes.new('ShaderNodeNormalMap');m.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs[0],p.inputs[target])
  else:m.node_tree.links.new(t.outputs['Color'],p.inputs[target])
 MATERIALS.append(m)
PARTS=[];LOD=0

def mesh(name,vertices,faces,material=0):
 d=bpy.data.meshes.new(name);d.from_pydata(vertices,[],faces);d.update();o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);d.materials.append(MATERIALS[material]);PARTS.append(o);return o

def box(name,at,size,material=0):
 x,y,z=at;a,b,c=[v/2 for v in size]
 v=[(x+i*a,y+j*b,z+k*c)for i,j,k in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
 return mesh(name,v,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],material)

def beam(name,start,end,width=.15,material=1,thickness=None):
 d=Vector(end)-Vector(start);o=box(name,(0,0,0),(width,thickness or width,d.length),material);o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.location=(Vector(start)+Vector(end))/2;return o

def cylinder(name,at,radius,height,material=0,sides=None,top_radius=None):
 n=sides or [16,12,8][LOD];x,y,z=at;top_radius=radius if top_radius is None else top_radius
 v=[(x+r*math.cos(i*2*math.pi/n),y+r*math.sin(i*2*math.pi/n),z+k*height)for k,r in [(0,radius),(1,top_radius)]for i in range(n)]
 return mesh(name,v,[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n)for i in range(n)],material)

def pointed_arch(name,x,y,z,half_width,spring,depth=.5,thickness=.25,material=1):
 a=half_width*.35;r=half_width+a;end=math.acos(-a/r);count=[14,8,4][LOD]
 for side in [-1,1]:
  for i in range(count):
   t0=math.pi+(end-math.pi)*i/count;t1=math.pi+(end-math.pi)*(i+1)/count;v=[]
   for yy in [-depth/2,depth/2]:
    for rr,t in [(r,t0),(r,t1),(r+thickness,t1),(r+thickness,t0)]:v.append((x+side*(a+rr*math.cos(t)),y+yy,z+spring+rr*math.sin(t)))
   mesh(name+' voussoir',v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],material)
  box(name+' jamb',(x+side*(half_width+thickness/2),y,z+spring/2),(thickness,depth,spring),material)

def lancet(x,y,z,half=.58,height=2.3,depth=.24):
 box('Deep slate lancet recess',(x,y+depth*.7,z+height*.45),(half*2,.08,height*.9),2)
 pointed_arch('Carved pointed lancet',x,y,z,half,height*.67,depth,.15)
 box('Drip sill',(x,y-.09,z+.02),(half*2+.38,.45,.15),1)
 if LOD<2:
  beam('Window mullion',(x,y-.03,z+.13),(x,y-.03,z+height),.075)
  for sign in [-1,1]:beam('Branch tracery',(x,y-.04,z+height*.6),(x+sign*half*.7,y-.04,z+height*.79),.065)

def gable_roof(name,x,y,eaves,width,depth,rise,offset=0,material=2):
 mesh(name,[(x-width/2,y-depth/2,eaves),(x+width/2,y-depth/2,eaves),(x-width/2,y+depth/2,eaves),(x+width/2,y+depth/2,eaves),(x+offset,y-depth/2,eaves+rise),(x+offset,y+depth/2,eaves+rise)],[(0,2,5,4),(1,4,5,3),(0,4,1),(2,3,5),(0,1,3,2)],material)
 beam(name+' ridge',(x+offset,y-depth/2-.08,eaves+rise),(x+offset,y+depth/2+.08,eaves+rise),.23,1)
 for end in [-1,1]:
  for sign in [-1,1]:beam('Carved gable verge',(x+sign*width/2,y+end*depth/2,eaves),(x+offset,y+end*depth/2,eaves+rise),.26,1)
 for sign in [-1,1]:
  beam('Stone eave coping',(x+sign*width/2,y-depth/2,eaves),(x+sign*width/2,y+depth/2,eaves),.24,1)
  if LOD<2:
   for course in range(1,[12,5][LOD]):
    t=course/[12,5][LOD];xx=x+sign*width/2*(1-t)+offset*t
    beam('Slate overlapping course',(xx,y-depth/2,eaves+rise*t+.025),(xx,y+depth/2,eaves+rise*t+.025),.055,2)

def radial_roof(name,x,y,z,radius,height,sides=8,material=2):
 cylinder(name+' lower sweep',(x,y,z),radius,height*.28,material,sides,radius*.56)
 cylinder(name+' upper needle',(x,y,z+height*.28),radius*.56,height*.72,material,sides,.10)
 cylinder(name+' lower coping',(x,y,z-.08),radius+.12,.19,1,sides)
 cylinder('Bronze finial',(x,y,z+height),.13,.8,4,8,.045)
 if LOD<2:
  for i in range(sides):
   a=i*2*math.pi/sides;beam('Roof diagonal rib',(x+radius*math.cos(a),y+radius*math.sin(a),z),(x,y,z+height),.09,1)

def flying_buttress(x,y,low,high,sign):
 count=[10,6,4][LOD];prior=None
 for i in range(count+1):
  t=i/count;p=(x+sign*4.1*t,y,high+(low-high)*t-1.4*math.sin(t*math.pi))
  if prior is not None:beam('Flying arch structural rib',prior,p,.28,0,.42)
  prior=p
 box('Upper buttress pier cap',(x+sign*4.1,y,low+.15),(.65,.8,.3),1)
 cylinder('Buttress needle',(x+sign*4.1,y,low+.3),.25,1.25,1,6,.025)

def rose_window(x,y,z,radius):
 count=[24,16,12][LOD]
 for i in range(count):
  a=i*2*math.pi/count;b=(i+1)*2*math.pi/count
  beam('Rose stone ring',(x+radius*math.cos(a),y,z+radius*math.sin(a)),(x+radius*math.cos(b),y,z+radius*math.sin(b)),.18,1)
 for i in range([12,8,6][LOD]):
  a=i*2*math.pi/[12,8,6][LOD];beam('Rose tracery ray',(x,y,z),(x+radius*.94*math.cos(a),y,z+radius*.94*math.sin(a)),.10,1)
 cylinder('Rose mineral boss',(0,0,0),.23,.16,4,12).rotation_euler[0]=math.pi/2;PARTS[-1].location=(x,y+.02,z)

def square_tower(x,y,base,width,shaft_top,crown,asymmetric=False):
 h=shaft_top-base;box('Tower primary stone shaft',(x,y,base+h/2),(width,width,h),0)
 for sign in [-1,1]:
  for end in [-1,1]:box('Tower engaged corner pier',(x+sign*(width/2-.13),y+end*(width/2-.13),base+h/2),(.42,.42,h),1)
 for level in range([5,3,2][LOD]):box('Tower belt course',(x,y,base+h*(level+1)/([5,3,2][LOD]+1)),(width+.2,width+.2,.18),1)
 for level in range(2):
  zz=shaft_top-3.9-level*4.1
  if zz<base+.5:continue
  for xx in [-width*.23,width*.23]:lancet(x+xx,y-width/2-.04,zz,.38,2.65)
 box('Tower projecting cornice',(x,y,shaft_top),(width+.7,width+.7,.45),1)
 if asymmetric:
  cylinder('Octagonal belfry',(x,y,shaft_top+.2),width*.44,2.8,0,8);radial_roof('Octagonal upper crown',x,y,shaft_top+3,width*.62,crown,8)
  for i,(xx,yy)in enumerate([(-1,-1),(-1,1),(1,-1),(1,1)]):cylinder('Corner pinnacle',(x+xx*width*.48,y+yy*width*.48,shaft_top+.23),.28,1.4+(i%2)*.4,1,6,.04)
 else:
  gable_roof('Broken-backed saddle crown',x,y,shaft_top+.24,width+.9,width+.8,crown,offset=.5)
  for sign in [-1,1]:box('Crenellated side crest',(x+sign*width*.47,y,shaft_top+.65),(.28,width+.4,.85),1)
  cylinder('Bell mineral cap',(x+.5,y,shaft_top+crown+.3),.2,.8,4,8,.03)

def archive():
 gable_roof('Long sanctuary nave',0,24,14.8,17.1,48.7,8.0)
 for sign in [-1,1]:
  box('Raised clerestory wall',(sign*7.8,24,12.9),(.38,46.8,3.8),0);box('Clerestory deep string',(sign*7.8,24,11.1),(.64,47,.24),1)
  gable_roof('Open cloister aisle',sign*11.6,24,8.45,6.6,48.3,3.25)
  for bay in range(7):
   depth=2.6+bay*42.8/6;first=len(PARTS);lancet(0,0,11.4,.66,2.65)
   for o in PARTS[first:]:o.rotation_euler[2]=-sign*math.pi/2;o.location+=Vector((sign*8.04,depth,0))
   flying_buttress(sign*7.7,depth,9.7,13.6,sign)
   if LOD<2:cylinder('Clerestory parapet pinnacle',(sign*8.14,depth,14.9),.19,1.5,1,6,.015)
 mesh('Pointed upper west front',[(-8.35,-.42,14.7),(8.35,-.42,14.7),(0,-.42,22.8),(-8.35,-.03,14.7),(8.35,-.03,14.7),(0,-.03,22.8)],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],0)
 rose_window(0,-.48,17.6,1.5);pointed_arch('Front rose enclosing hood',0,-.55,14.75,2.2,2.0,.35,.26)
 for sign in [-1,1]:
  for i in range([5,3,2][LOD]):
   t=(i+1)/([5,3,2][LOD]+1);cylinder('Stepped front crest',(sign*8.0*(1-t),-.3,14.9+7.6*t),.16,.8,1,6,.015)
 square_tower(-11.4,35.04,8.45,3.8,26.0,5.3,True);square_tower(11.4,35.04,8.45,3.8,24.4,5.0,False)
 cylinder('Crossing octagonal drum',(0,28.5,21.5),2.25,2.0,0,8);radial_roof('Crossing amethyst lantern',0,28.5,23.5,2.65,4.5,8)
 for sign in [-1,1]:beam('Bronze ridge conduit',(sign*.21,1.5,22.86),(sign*.21,45.7,22.86),.08,4)

def office():
 gable_roof('Civic hall steep roof',0,15,12.8,17.1,30.6,5.7,offset=-.6)
 for sign in [-1,1]:
  gable_roof('Civic open side gallery',sign*11.6,15,8.45,6.6,30.2,3.25);box('Hall clerestory stone',(sign*7.8,15,11.1),(.36,28.5,3.25),0)
  for bay in range(4):flying_buttress(sign*7.7,2.6+bay*24.8/3,9.7,12.7,sign)
 square_tower(11.9,21,8.45,3.4,12.7,3.0,False)
 cylinder('Civic bell drum',(-11.6,17.4,23.6),2.0,2.8,0,8)
 for i in range(8):
  a=i*math.pi/4;x=-11.6+1.7*math.cos(a);y=17.4+1.7*math.sin(a);beam('Bell lantern engaged rib',(x,y,23.5),(x,y,26.5),.18,1)
 radial_roof('Tall split bell needle',-11.6,17.4,26.5,2.6,5.9,8);gable_roof('Forward civic pediment',0,.1,12.8,8.8,1.7,4.0);rose_window(-.6,-.81,15.1,.9)
 if LOD<2:
  for depth in [4,11,20,27]:
   for sign in [-1,1]:cylinder('Civic roof finial',(sign*8.25,depth,13.0),.17,1.2,1,6,.02)
 for depth in [8,22]:box('Hall dormer stone',(3.8,depth,15.1),(2.0,2.3,2.0),0);gable_roof('Hall dormer crown',3.8,depth,16.1,2.6,2.7,1.6)

SKYLINES={'WRSkyline809556':(809556,45.001105,21.18362,162.37482),'WRSkyline32874':(32874,50.74799,20.03610,128.66837),'WRSkyline53496':(53496,40.0,34.000003,162.56671)}

def mineral_pier(seed,x,y,radius,height,lean):
 # The small radial offset prevents coplanar clashes with visible physical witnesses.
 bottom=[];shoulder=[];size=radius*2.4
 for i in range(6):
  a=i*math.pi/3;variation=1+((seed+i*7)%5)*.035
  bottom.append((x+math.cos(a)*size/2*variation*1.008,y+math.sin(a)*size/2*1.008,0));shoulder.append((bottom[-1][0]+lean*height*.65,bottom[-1][1]+size*.06,height*(.65+(i%2)*.04)))
 tip=(x+lean*height,y+size*.12,height);v=bottom+shoulder+[tip,(x,y,0)];f=[]
 for i in range(6):
  j=(i+1)%6;f +=[(i,j,j+6,i+6),(i+6,j+6,12),(13,j,i)]
 mesh('Fitted six-faced masonry pier',v,f,0)
 for i in range(6):beam('Monumental pier engaged rib',bottom[i],shoulder[i],.20,1);beam('Upper taper engaged rib',shoulder[i],tip,.15,1)
 levels=[12,7,4][LOD]
 for level in range(1,levels+1):
  t=level/(levels+1)*.62
  for i in range(6):
   j=(i+1)%6;a=tuple(bottom[i][axis]+(shoulder[i][axis]-bottom[i][axis])*t/.65 for axis in range(3));b=tuple(bottom[j][axis]+(shoulder[j][axis]-bottom[j][axis])*t/.65 for axis in range(3));beam('Pier dressed-stone belt',a,b,.19,1)
 for tier in range([4,2,1][LOD]):
  zz=14+tier*height*.11;fraction=zz/height;lancet(x+lean*height*fraction,y-size*.42,zz,.48,3.2,.22)

def skyline(module):
 seed,width,depth,height=SKYLINES[module];radius=min(width,depth)*.11;shaft_height=height-.3
 for xs in [-1,1]:
  for ys in [-1,1]:mineral_pier(seed+xs+ys,xs*width*.34,ys*depth*.34,radius,shaft_height,-xs*.06)
 rise=18 if seed==809556 else 23 if seed==32874 else 28
 for end in [-1,1]:gable_roof('Steep under-landing sanctuary roof',0,end*depth*.245,height-rise-.65,width*.96,depth*.49,rise)
 for end in [-1,1]:
  y=end*depth*.34;pointed_arch('Monumental sky arcade',0,y,height*.61,width*.235,height*.11,1.0,.65,0)
  if LOD<2:pointed_arch('Sky arcade carved surround',0,y-.6,height*.61,width*.235+.8,height*.11,.35,.24,1)
 # Full upper belfry mass, below original standing plane. Ground voids remain open.
 arcade_base=height-(58 if seed==809556 else 51 if seed==32874 else 62);clerestory_base=height-34;eaves=height-rise-.65;wall_top=eaves;half_width=width*.40;half_depth=depth*.37;bays=4 if seed==32874 else 3;spacing=width*.76/bays
 for end in [-1,1]:
  front=end*half_depth
  for bay in range(bays):
   xx=(bay-(bays-1)/2)*spacing;pointed_arch('Upper reliquary open arcade',xx,front,arcade_base,spacing*.38,clerestory_base-arcade_base-spacing*.40,1.25,.52,0)
   for sign in [-1,1]:
    box('Belfry engaged load pier',(xx+sign*spacing*.45,front,(arcade_base+wall_top)/2),(.70,1.4,wall_top-arcade_base),0);box('High pier carved capital',(xx+sign*spacing*.45,front,clerestory_base),(1.0,1.75,.45),1)
   if wall_top-clerestory_base>3:
    box('Upper window lower spandrel',(xx,front,clerestory_base+.45),(spacing,.85,.9),0);box('Upper window parapet',(xx,front,wall_top-.45),(spacing,.85,.9),0)
    for sign in [-1,1]:box('Upper deep window jamb',(xx+sign*spacing*.34,front,(clerestory_base+wall_top)/2),(spacing*.21,.85,wall_top-clerestory_base),0)
    lancet(xx,front-end*.07,clerestory_base+1.0,spacing*.20,min(5.0,wall_top-clerestory_base-1.1),.4)
  box('Continuous reliquary dressed eave',(0,front,wall_top),(width*.82,1.6,.45),1);box('Deep arcade string course',(0,front,clerestory_base-.1),(width*.82,1.6,.45),1)
 for side in [-1,1]:
  first=len(PARTS);pointed_arch('Upper transept open arcade',0,0,arcade_base,depth*.26,clerestory_base-arcade_base-depth*.27,1.1,.6,0)
  for o in PARTS[first:]:o.rotation_euler[2]=math.pi/2;o.location.x+=side*half_width
  box('Side high clerestory strip',(side*half_width,0,(clerestory_base+wall_top)/2),(.75,depth*.70,wall_top-clerestory_base),0)
  for bay in [-1,0,1]:
   y=bay*depth*.22;beam('High reliquary stepped buttress',(side*width*.42,y,arcade_base+4),(side*width*.34,y,wall_top+.4),.85,0);box('Buttress upper coping',(side*width*.34,y,wall_top+.4),(1.15,1.6,.28),1)
 if seed==809556:
  for side in [-1,1]:gable_roof('Twin sanctuary high nave',side*width*.21,0,eaves,width*.49,depth*.92,rise-.1)
 elif seed==32874:gable_roof('Asymmetric civic high saddle',0,0,eaves,width*.94,depth*.92,rise-.1,offset=-width*.12)
 else:
  gable_roof('Eastern high crossing nave',0,0,eaves,width*.94,depth*.92,rise-.1)
  for side in [-1,1]:gable_roof('Eastern upper transept gable',side*width*.23,0,eaves+3.0,width*.46,depth*.70,rise-6.0,offset=side*1.1)
 for sign in [-1,1]:
  start=(sign*width*.27,-depth*.34,height*.76);finish=(sign*width*.27,depth*.34,height*.76);count=[12,8,4][LOD];previous=start
  for i in range(1,count+1):
   t=i/count;p=(start[0]-sign*1.1*math.sin(t*math.pi),start[1]+(finish[1]-start[1])*t,height*.76+height*.09*math.sin(t*math.pi));beam('Lateral sky flying buttress',previous,p,.65,0);previous=p
 for xs in [-1,1]:
  for ys in [-1,1]:
   x=xs*(width*.5-.5);y=ys*depth*.33;cylinder('Existing edge reservation stone crest',(x,y,height),.50,1.1,1,6,.42);radial_roof('Existing edge reservation crown',x,y,height+1.1,.63,2.0,6)
 if seed==809556:
  for sign in [-1,1]:beam('Paired sanctuary blade',(sign*width*.44,0,height-16),(sign*width*.31,0,height-.8),.4,4)
 elif seed==32874:
  for end in [-1,1]:
   for i in range([5,3,2][LOD]):
    x=(i-([5,3,2][LOD]-1)/2)*3.0;beam('Split saddle apse rib',(x,end*depth*.45,height-18),(x*.7,end*depth*.26,height-.8),.25,1)
 else:
  for side in [-1,1]:gable_roof('Eastern transept canopy',side*width*.23,0,height-24.0,width*.43,depth*.88,16.0,offset=side*1.4)

MODELS={'WRArchiveCrown':archive,'WROfficeCrown':office};MODELS.update({name:(lambda selected=name:skyline(selected))for name in SKYLINES});records=[]
for name,author in MODELS.items():
 for LOD in range(3):
  PARTS=[];author();bpy.ops.object.select_all(action='DESELECT')
  for o in PARTS:o.select_set(True)
  bpy.context.view_layer.objects.active=PARTS[0];bpy.ops.object.join();o=bpy.context.object;o.name=name+'_LOD'+str(LOD);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
  uv=o.data.uv_layers.new(name='SharedFourMetreUV')
  for face in o.data.polygons:
   axis=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3)if i!=axis]
   for li in face.loop_indices:
    v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]]/4,v[axes[1]]/4)
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');mod=o.modifiers.new('Explicit export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
  path=ART/(o.name+'.fbx');bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,use_tspace=True,path_mode='RELATIVE')
  bounds=[[min(v.co[i]for v in o.data.vertices)for i in range(3)],[max(v.co[i]for v in o.data.vertices)for i in range(3)]]
  records.append({'name':name,'lod':LOD,'path':path.relative_to(ROOT).as_posix(),'triangles':len(o.data.polygons),'vertices':len(o.data.vertices),'uv_vertices':len(uv.data),'source_bounds':bounds,'unity_bounds':[[bounds[0][0],bounds[0][2],bounds[0][1]],[bounds[1][0],bounds[1][2],bounds[1][1]]],'materials':[m.name for m in o.data.materials],'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size});o.hide_render=True;o.hide_set(True)
for image in bpy.data.images:
 if image.filepath:image.filepath=bpy.path.relpath(image.filepath,start=str(SOURCE))
source_path=SOURCE/'GothicCivicCrownsWR01.blend'
# Compression keeps the editable source within the connector request limit without losing geometry.
bpy.ops.wm.save_as_mainfile(filepath=str(source_path),compress=True)
foundation=json.loads((ROOT/'Assets/NeonSyndicate/Art/HeartFR01/manifest.json').read_text())
manifest={'schema_version':1,'license':'CC0-1.0','creator':'Original fitted civic crowns and skyline reliquaries authored for this project','source':source_path.relative_to(ROOT).as_posix(),'source_sha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),'source_bytes':source_path.stat().st_size,'axis':'Blender Z-up metres; FBX -Z forward Y-up, baked transform','pivot':'Original venue local origin, not bounding-box centre. Unity x,height,depth maps to Blender x,depth,height.','scale_policy':'Scale1 only; exact Archive depth48 and Office depth30. No automatic fit distortion.','secondary_visual_only':True,'collision_policy':'Visual only. Keep existing H5 Plan or original family9 physical surfaces visible as collision witness, plus all original structure/doors/actors. New upper silhouettes are not advertised as walkable. Native contact acceptance NOT RUN.','modules':records,'textures':foundation['textures'],'new_textures':0,'external_inputs':[],'native_unity':'NOT RUN','contracts':[{'module':'WRArchiveCrown','service':'archive','seed':479130,'width':30,'depth':48,'minimum_high_roof':8.3,'preserved_voids':['nave ground and door','side galleries below original roofs','altar and gallery stair','all ground service interactions'],'proposed_suppression_models':['slate_roof','mineral_pier']},{'module':'WROfficeCrown','service':'office','seed':472154,'width':30,'depth':30,'minimum_high_roof':8.3,'preserved_voids':['main civic door','side galleries below original roofs','rear wall','all ground service interactions'],'proposed_suppression_models':['slate_roof','mineral_pier']}]+[{'module':name,'seed':v[0],'width':v[1],'depth':v[2],'height':v[3],'pivot':'Original footprint centre at CurbHeight','preserved_voids':['all original ground voids between four original piers','original top standing plane and receipt positions'],'source_collision_owner':'CityBuilder.Gothic04OB original compound solid','native_collision_review':'NOT RUN','proposed_suppression':'Only exact matching original three visual appends'}for name,v in SKYLINES.items()]}
(ART/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');(ART/'CREDITS.txt').write_text('Original Gothic Heart World Rewrite civic crowns and skyline reliquaries authored for this project. CC0-1.0.\nNo external mesh, purchased asset or texture input. Reuses the original FR01 shared maps by reference.\nEditable source: ArtSource/HeartWR01/GothicCivicCrownsWR01.blend.\nRuntime art is visual only. Existing H5 physical surfaces and service owners remain authoritative.\n')
(SOURCE/'SOURCE_RECEIPT.json').write_text(json.dumps({'source':manifest['source'],'source_sha256':manifest['source_sha256'],'source_bytes':manifest['source_bytes'],'tool':'Blender '+bpy.app.version_string,'recipe':'tools/build_heart_wr01.py','families':len(MODELS),'exports':len(records),'new_textures':0,'license':manifest['license'],'native_unity':'NOT RUN','recovery':'Fresh regenerated compressed editable source after external workspace pruning. No previous receipt is reused.'},indent=2)+'\n')
for path in [ART]+sorted(ART.iterdir()):
 if path.suffix=='.meta':continue
 sidecar=Path(str(path)+'.meta')
 if not sidecar.exists():sidecar.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'neonsyndicate:world-rewrite:'+path.relative_to(ROOT).as_posix()).hex+'\n')
print('WR01 authored',len(records),'FBX exports; compressed source bytes',source_path.stat().st_size,flush=True)
