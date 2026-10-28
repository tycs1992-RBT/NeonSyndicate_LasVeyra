"""Original editable foundry demonstration assets, authored locally. No provider generation or paid content."""
import argparse,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector,Quaternion
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'ArtSource/FoundryWR02A';ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A';SRC.mkdir(exist_ok=True);ART.mkdir(exist_ok=True)
p=argparse.ArgumentParser();p.add_argument('--contract',type=Path,required=True);p.add_argument('--asset',choices=['architecture','creature'],required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.read_factory_settings(use_empty=True)
def material(name):
 m=bpy.data.materials.get(name)
 if m:return m
 m=bpy.data.materials.new(name);m.use_nodes=True;shader=m.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.85
 folder=ART if name.startswith('Basilisk')else ROOT/'Assets/NeonSyndicate/Art/HeartFR01'
 image=bpy.data.images.load(str(folder/(name+'_BaseColor.png')),check_existing=True);t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=image;m.node_tree.links.new(t.outputs['Color'],shader.inputs['Base Color'])
 return m
def mesh(name,vertices,faces,surface,protect=True,weights=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.materials.append(material(surface));data.update();o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o['foundry_protect_silhouette']=protect;o['foundry_semantic']=name.split('/')[0];uv=data.uv_layers.new(name='UV0')
 for f in data.polygons:
  axis=max(range(3),key=lambda i:abs(f.normal[i]));axes=[i for i in range(3)if i!=axis]
  for j in f.loop_indices:
   v=data.vertices[data.loops[j].vertex_index].co;uv.data[j].uv=(v[axes[0]]*.45,v[axes[1]]*.45)
 if weights:
  for bone,values in weights.items():
   g=o.vertex_groups.new(name=bone)
   for i,w in values:g.add([i],w,'REPLACE')
 return o
def box(name,c,size,surface='Masonry',bevel=.035,protect=True):
 x,y,z=c;w,d,h=(v*.5 for v in size);v=[(x+sx*w,y+sy*d,z+sz*h)for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]];f=[(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)];o=mesh(name,v,f,surface,protect)
 if bevel:
  mod=o.modifiers.new('Carved edge','BEVEL');mod.width=min(bevel,min(size)*.2);mod.segments=2
 return o
def arch(name,x,y,width,spring,apex,ring,depth,surface='CarvedTrim',side=False,base=8.6):
 # Real open voussoir ring, formed from two pointed quadratic arcs, not a painted window.
 points=[];height=apex-spring;center=height*height/width-width*.25;radius=center+width*.5;end=math.atan2(height,-center)
 left=[Vector((center+radius*math.cos(math.pi+(end-math.pi)*i/12),spring+radius*math.sin(math.pi+(end-math.pi)*i/12)))for i in range(13)]
 points=left+[Vector((-point.x,point.y))for point in reversed(left[:-1])]
 for i,(u,v)in enumerate(zip(points,points[1:])):
  tangent=(v-u).normalized();normal=Vector((-tangent.y,tangent.x));corners=[u,v,v+normal*ring,u+normal*ring];verts=[]
  for dy in [-depth*.5,depth*.5]:
   for px,pz in corners:verts.append((x+(dy if side else px),y+(px if side else dy),pz))
  mesh(name+'/'+str(i),verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],surface)
 for sign in [-1,1]:box(name+'/jamb',(x+(0 if side else sign*width*.5),y+(sign*width*.5 if side else 0),(base+spring)*.5),(depth if side else ring,ring if side else depth,spring-base),surface,.02)
def roof(name,x,y,z,width,length,rise):
 v=[(x-width/2,y-length/2,z),(x+width/2,y-length/2,z),(x,y-length/2,z+rise),(x-width/2,y+length/2,z),(x+width/2,y+length/2,z),(x,y+length/2,z+rise)];return mesh(name,v,[(0,2,1),(3,4,5),(0,3,5,2),(2,5,4,1),(1,4,3,0)],'SlateTimber')
def spire(name,x,y,z,radius,height,surface="SlateTimber"):
 n=8;verts=[(x+math.cos(i*2*math.pi/n)*radius,y+math.sin(i*2*math.pi/n)*radius,z)for i in range(n)]+[(x,y,z+height)];mesh(name,verts,[tuple(reversed(range(n)))]+[(i,(i+1)%n,n)for i in range(n)],surface)
def architecture():
 # Same local metre envelope as the existing Archive crown. Ground doors and H5 service walls stay original.
 for x in [-14.77,14.77]:box('Foundation/aisle-course',(x,23.8775,8.44),(.5,49.205,.22))
 for y in [-.475,48.23]:box('Foundation/end-course',(0,y,8.44),(30.04,.5,.22))
 roof('Nave/steep-primary-roof',0,23.5,21.2,17,46,8.2)
 for side in [-1,1]:
  roof('Aisle/steep-roof',side*11.1,25.5,13.6,6.1,43.2,5.2)
  for i,y in enumerate([3.8,11.4,19,26.6,34.2,41.8]):
   x=side*8.1;arch('Clerestory/vaulted-window',x,y,4.5,18.8,21.8,.28,1,'CarvedTrim',True,16.7)
   box('Nave/structural-pier',(side*8.05,y-3.2,15.15),(1.15,1.3,13.3),'Masonry',.055)
   box('Buttress/ground-shaft',(side*14.1,y,12.6),(1.5,1.65,8.0),'Masonry',.045)
   # Flying buttress has its own actual curved stone arch and a supported outer pinnacle.
   arch('Buttress/flying-arch',side*11.3,y,5.2,15.2,21.2,.5,1.05)
   spire('Buttress/pinnacle',side*14.1,y,16.6,.72,1.8)
   for level in [9.1,11,13.1]:box('Buttress/carved-cap',(side*14.1,y,level),(1.78,1.9,.2),'CarvedTrim',.035,False)
  for j in range(14):
   y=2+j*3.3;box('Aisle/masonry-bay',(side*14.45,y,10.4),(.6,2.9,3.7),'Masonry',.03)
  for segment in range(8):box('Nave/jointed-cornice-stone',(side*8.5,.225+2.9+segment*5.8,21.1),(.55,5.75,.36),'CarvedTrim',.025)
 # Asymmetric front towers, one square stone base and an octagonal open belfry, the other a stepped observatory.
 for x,y,top,cap in [(-10.6,5.1,27.2,7.9),(10.8,7,23.5,5.8)]:
  bodytop=top-4.6
  for sx in [-1,1]:
   for sy in [-1,1]:
    box('Tower/corner-pier',(x+sx*2.25,y+sy*2.25,(8.5+bodytop)*.5),(1.1,1.1,bodytop-8.5),'Masonry',.06)
    box('Tower/belfry-bearing-pier',(x+sx*2.25,y+sy*2.25,(bodytop+top)*.5),(.8,.8,top-bodytop),'Masonry',.035)
  for sign in [-1,1]:
   box('Tower/heavy-side-wall',(x+sign*2.3,y,(8.5+bodytop-2.8)*.5),(.7,4.6,bodytop-2.8-8.5),'Masonry',.045)
   box('Tower/heavy-end-wall',(x,y+sign*2.3,(8.5+bodytop-2.8)*.5),(4.6,.7,bodytop-2.8-8.5),'Masonry',.045)

  for level in [12.2,17,21.1]:box('Tower/structural-belt',(x,y,level),(5.8,5.8,.38),'CarvedTrim',.04)
  for angle in [0,math.pi/2,math.pi,math.pi*1.5]:
   xx=x+math.cos(angle)*2.25;yy=y+math.sin(angle)*2.25;arch('Tower/belfry-opening',xx,yy,3.2,top-3.9,top-1,.32,.85,'CarvedTrim',abs(math.cos(angle))>.5,bodytop)
  box('Tower/belfry-cornice',(x,y,top-.15),(5.8,5.8,.32),'CarvedTrim',.04);spire('Tower/primary-asymmetric-spire',x,y,top,3.05,cap)
 # A structural front arch, deep stone reveals and radial tracery above the retained original lower doorway.
 arch('Facade/deep-vaulted-opening',0,.5,10.8,17.5,28.4,.6,2.1)
 for x in [-6.9,6.9]:box('Facade/heavy-pier',(x,.65,15.6),(1.25,1.55,14.5),'Masonry',.055)
 n=48;radius=3.5;v=[]
 for y in [-.38,.02]:
  for r in [radius,radius-.38]:v +=[(math.cos(i*2*math.pi/n)*r,y,24.0+math.sin(i*2*math.pi/n)*r)for i in range(n)]
 f=[]
 for i in range(n):j=(i+1)%n;f.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 mesh('Facade/resonance-oculus',v,f,'CarvedTrim')
 for i in range(12):
  angle=i*math.pi*2/12;center=Vector((math.cos(angle)*1.8,-.18,24.0+math.sin(angle)*1.8));o=box('Facade/radial-tracery',center,(.12,.25,3.2),'CarvedTrim',.015);rotation=Matrix.Rotation(math.pi/2-angle,3,'Y')
  for vertex in o.data.vertices:vertex.co=center+rotation@(vertex.co-center)
 for petal in range(6):
  angle=petal*math.pi/3;cx=math.cos(angle)*1.85;cz=24.0+math.sin(angle)*1.85;verts=[];segments=24
  for yy in [-.32,-.06]:
   for radius in [.9,.79]:
    for i in range(segments):
     phase=i*2*math.pi/segments;r=radius*(1+.09*math.cos(3*phase));verts.append((cx+math.cos(phase)*r,yy,cz+math.sin(phase)*r))
  faces=[]
  for i in range(segments):
   j=(i+1)%segments;faces.extend([(i,j,segments+j,segments+i),(2*segments+i,3*segments+i,3*segments+j,2*segments+j),(i,2*segments+i,2*segments+j,j),(segments+i,segments+j,3*segments+j,3*segments+i)])
  mesh('Facade/cusped-petal-tracery',verts,faces,'CarvedTrim')
 for y in [14,25.5,37]:arch('Nave/transverse-vault',0,y,14.5,16.8,24.8,.3,.36)
 # Sparse mineral technology: four framed conductors, not an emissive facade.
 for x,z in [(-1.2,24),(1.2,24),(0,22.8),(0,25.2)]:spire('Crystal/restrained-amethyst-core',x,-.2,z,.16,.8,'AmethystBronze')
 # Weathered individual masonry tooling remains editable in the authoring source.
 for side in [-1,1]:
  for row in range(6):
   for col in range(22):
    y=1.05+col*2.14+(row%2)*.25
    if y>47:continue
    box('Masonry/individual-course',(side*14.77,y,8.8+row*.64),(.12,2.04,.56),'MossRoot'if(row*7+col)%31==0 else'Masonry',.025,False)
 return 'SanctuaryArchive'
BASIS=Matrix(((1,0,0),(0,0,1),(0,1,0)))
def B(v):return BASIS@Vector(v)
def creature(contract):
 bones=contract['bones'];lookup={r['name']:i for i,r in enumerate(bones)};positions={r['name']:Vector(r['model'])for r in bones};objects=[]
 def loft(name,sections,surface,bone_names,sides=20):
  verts=[];faces=[];weights={};centers=[Vector(s[0])for s in sections]
  for i,(center,rx,ry)in enumerate(sections):
   tangent=(centers[min(i+1,len(centers)-1)]-centers[max(0,i-1)]).normalized();right=tangent.cross(Vector((0,1,0))).normalized()
   if right.length<.1:right=Vector((1,0,0))
   up=right.cross(tangent).normalized()
   for j in range(sides):
    angle=j*math.pi*2/sides;point=Vector(center)+right*(math.cos(angle)*rx)+up*(math.sin(angle)*ry);index=len(verts);verts.append(B(point))
    near=sorted([(positions[bn]-point).length_squared for bn in bone_names]);pairs=sorted(((bn,(positions[bn]-point).length_squared)for bn in bone_names),key=lambda x:x[1])[:2];values=[1/max(.0002,d)**2 for bn,d in pairs];total=sum(values)
    for(bn,d),w in zip(pairs,values):weights.setdefault(bn,[]).append((index,w/total))
  for i in range(len(sections)-1):
   for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
  faces+=[tuple(reversed(range(sides))),tuple((len(sections)-1)*sides+j for j in range(sides))];o=mesh(name,verts,faces,surface,True,weights)
  side_faces=(len(sections)-1)*sides
  for polygon in o.data.polygons:
   for loop_index in polygon.loop_indices:
    index=o.data.loops[loop_index].vertex_index;ring_index=index//sides;around=index%sides
    if polygon.index<side_faces:
     seam=polygon.index%sides==sides-1;u=around/sides+(1 if seam and around==0 else 0);v=ring_index/max(1,len(sections)-1)
    else:
     angle=around*math.pi*2/sides;u=2.5+math.cos(angle)*.48;v=2.5+math.sin(angle)*.48
    o.data.uv_layers[0].data[loop_index].uv=(u,v)

  for poly in o.data.polygons:poly.use_smooth=True
  if name.startswith('Anatomy/'):
   bpy.context.view_layer.objects.active=o;modifier=o.modifiers.new('Organic anatomical surface','SUBSURF');modifier.levels=1;modifier.subdivision_type='CATMULL_CLARK';bpy.ops.object.modifier_apply(modifier=modifier.name)
  objects.append(o);return o
 torso=[((0,y,z),w,h)for z,y,w,h in [(-.6,.4,.09,.09),(-.49,.41,.21,.18),(-.33,.41,.25,.20),(-.1,.425,.275,.21),(.15,.43,.27,.205),(.36,.43,.25,.20),(.53,.44,.18,.16),(.65,.46,.13,.11),(.78,.49,.14,.11),(.90,.51,.19,.12),(1.03,.49,.19,.09),(1.18,.48,.12,.055),(1.26,.48,.07,.035)]]
 loft('Anatomy/continuous-torso-neck-skull',torso,'BasiliskHide',['Pelvis','Root','Spine','Chest','Neck1','Neck2','Head'],24)
 loft('Anatomy/articulated-lower-jaw',[((0,.448,.91),.155,.048),((0,.435,1.02),.17,.045),((0,.438,1.18),.105,.035),((0,.45,1.255),.055,.018)],'BasiliskKeratin',['Jaw'],20)
 tailnames=[b['name']for b in bones if b['name'].startswith('Tail')];tail=[(positions[n],.10*(1-i/7),.10*(1-i/7))for i,n in enumerate(tailnames)]+[((0,.20,-2.04),.008,.008)];loft('Anatomy/articulated-tail',tail,'BasiliskHide',tailnames,20)
 for side in ['L','R']:
  for chain in ['Fore','Hind']:
   names=[chain+n+'.'+side for n in ['Upper','Lower','Foot','Toe']];points=[positions[n]for n in names];sections=[]
   for i,point in enumerate(points):
    radius=[.10,.078,.05,.04][i]
    if i==0:sections.append((point+Vector((0,.025,0)),radius*1.1,radius))
    sections.append((point,radius,radius*.85))
    if i<len(points)-1:sections.append(((point+points[i+1])*.5,radius*.82,radius*.78))
   sections.append((points[-1]+Vector((0,-.008,.07)),.048,.022));loft('Anatomy/'+chain+side,sections,'BasiliskHide',names,16)
   toe=points[-1]
   for j in [-1,0,1]:loft('Claw/'+chain+side+str(j),[(toe+Vector((j*.03,.009,.025)),.017,.018),(toe+Vector((j*.033,.004,.09)),.008,.008),(toe+Vector((j*.034,0,.12)),.001,.001)],'BasiliskKeratin',[names[-1]],10)
 # Armoured scutes follow anatomy, rather than hiding limbs under an undifferentiated crystal lump.
 for i in range(15):
  z=-.51+i*.09;y=.61+.025*math.sin(i*.6);bn=min(['Pelvis','Root','Spine','Chest','Neck1'],key=lambda n:abs(positions[n].z-z));loft('Carapace/dorsal-scute',[( (0,y-.015,z-.035),.13,.025),((0,y+.026,z),.145,.042),((0,y-.005,z+.055),.11,.018)],'BasiliskKeratin',[bn],14)
 for sign in [-1,1]:
  for i in range(11):
   z=-.4+i*.1;y=.43;bn=min(['Pelvis','Root','Spine','Chest'],key=lambda n:abs(positions[n].z-z));loft('Carapace/flank-scute',[((sign*.24,y-.04,z-.028),.04,.02),((sign*.27,y,z),.055,.024),((sign*.23,y+.04,z+.045),.035,.014)],'BasiliskKeratin',[bn],10)
  for i in range(5):
   z=-.3+i*.22;bn=min(['Pelvis','Root','Spine','Chest'],key=lambda n:abs(positions[n].z-z));loft('Crystal/restrained-spine',[((sign*.08,.61,z),.025,.025),((sign*.10,.70,z-.03),.013,.014),((sign*.11,.73,z-.06),.001,.001)],'AmethystBronze',[bn],6)
  eye=(sign*.175,.545,1.02);loft('Head/eye-ridge',[(eye,.04,.035),((sign*.16,.56,1.09),.03,.02)],'BasiliskKeratin',['Head'],14);loft('Head/mineral-eye',[((sign*.187,.552,1.04),.019,.02),((sign*.19,.552,1.07),.015,.015)],'AmethystBronze',['Head'],12)
 armdata=bpy.data.armatures.new('ExistingCrystalBasilisk32');arm=bpy.data.objects.new('ExistingCrystalBasilisk32',armdata);bpy.context.collection.objects.link(arm);bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for i,row in enumerate(bones):
  bone=armdata.edit_bones.new(row['name']);bone.head=B(row['model']);bone.tail=bone.head+Vector((0,.08,0));bone.use_deform=True
  if row['parent']>=0:bone.parent=armdata.edit_bones[bones[row['parent']]['name']]
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in objects:
  mod=o.modifiers.new('Original species skeleton','ARMATURE');mod.object=arm;o.parent=arm
 arm.animation_data_create()
 for clip in contract['clips']:
  action=bpy.data.actions.new('Foundry_'+clip['id']);arm.animation_data.action=action
  for frame in clip['frames']:
   for i,row in enumerate(bones):
    pb=arm.pose.bones[row['name']];x,y,z,w=frame['rotations'][i];q=Quaternion((w,x,y,z));pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(BASIS@q.to_matrix()@BASIS).to_quaternion();pb.location=B(Vector(frame['positions'][i])-Vector(row['local']));pb.keyframe_insert('rotation_quaternion',frame=frame['frame']);pb.keyframe_insert('location',frame=frame['frame'])
  action.use_fake_user=True
 arm.animation_data.action=None
 for pb in arm.pose.bones:pb.rotation_quaternion=Quaternion();pb.location=Vector((0,0,0))
 arm['foundry_contract_sha256']=hashlib.sha256(a.contract.read_bytes()).hexdigest();arm['native_motion_accepted']=False
 return 'CrystalBasiliskCandidate'
name=architecture()if a.asset=='architecture'else creature(json.loads(a.contract.read_text()))
for im in bpy.data.images:
 if im.source=='FILE':im.pack()
bpy.context.scene.render.fps=30;bpy.context.scene.frame_set(1);blend=SRC/(name+'.author.blend');bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
# Preserve the editable authoring hierarchy in .blend, batch compatible surfaces for efficient import/export.
groups={}
for o in list(bpy.data.objects):
 if o.type=='MESH':groups.setdefault((o.data.materials[0].name,bool(o.get('foundry_protect_silhouette'))),[]).append(o)
depsgraph=bpy.context.evaluated_depsgraph_get()
for key,items in groups.items():
 if a.asset=='architecture':
  verts=[];faces=[]
  for item in items:
   evaluated=item.evaluated_get(depsgraph);data=evaluated.to_mesh();offset=len(verts);verts.extend(item.matrix_world@v.co for v in data.vertices);faces.extend(tuple(offset+i for i in p.vertices)for p in data.polygons);evaluated.to_mesh_clear()
  o=mesh(key[0]+('/primary'if key[1]else'/detail'),verts,faces,key[0],key[1])
  for item in items:bpy.data.objects.remove(item,do_unlink=True)
 else:
  bpy.ops.object.select_all(action='DESELECT')
  for o in items:o.select_set(True)
  bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();o=bpy.context.object;o.name=key[0]+('/primary'if key[1]else'/detail');o['foundry_protect_silhouette']=key[1]
raw=SRC/(name+'.raw.glb');bpy.ops.export_scene.gltf(filepath=str(raw),export_format='GLB',export_extras=True,export_animations=a.asset=='creature',export_animation_mode='ACTIONS',export_nla_strips=False,export_force_sampling=True,export_frame_range=False,export_skins=True,export_all_influences=False)
receipt={'asset':name,'method':'Original AI-assisted Blender Python authorship, no remote 3D provider task','authoringSource':blend.relative_to(ROOT).as_posix(),'source_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'input':raw.relative_to(ROOT).as_posix(),'input_sha256':hashlib.sha256(raw.read_bytes()).hexdigest(),'provider':'LOCAL_AUTHORING','creditsSpent':'0','license':'CC0-1.0','nativeUnity':'NOT RUN'};(SRC/(name+'.author-receipt.json')).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt),flush=True)
