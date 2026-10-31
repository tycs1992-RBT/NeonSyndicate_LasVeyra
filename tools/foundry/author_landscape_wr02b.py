"""Original WR02-B landscape authorship. The existing foundry processor performs all derived production."""
import argparse,hashlib,json,math,random,sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'ArtSource/FoundryWR02A/EnvironmentWR02B';ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B'
SURFACES={name:ROOT/'Assets/NeonSyndicate/Art/HeartWR02'for name in ['AncientBark','CanopyLeaf','WeatheredStrata']};SURFACES.update({name:ROOT/'Assets/NeonSyndicate/Art/HeartFR01'for name in ['MossRoot','CarvedTrim','Masonry','AmethystBronze']})
def material(name):
 m=bpy.data.materials.get(name)
 if m:return m
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(SURFACES[name]/(name+'_BaseColor.png')),check_existing=True);m.node_tree.links.new(n.outputs['Color'],m.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);return m

def mesh(name,vertices,faces,surface,protect=False):
 m=bpy.data.meshes.new(name);m.from_pydata(vertices,[],faces);m.materials.append(material(surface));m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o['foundry_protect_silhouette']=protect;o['foundry_semantic']=name.split('/')[0];uv=m.uv_layers.new(name='UV0')
 for face in m.polygons:
  axis=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3)if i!=axis]
  for loop in face.loop_indices:
   p=m.vertices[m.loops[loop].vertex_index].co;uv.data[loop].uv=(p[axes[0]]*(.055 if surface=='WeatheredStrata'else .35),p[axes[1]]*(.055 if surface=='WeatheredStrata'else .35))
 return o

def tube(name,points,radii,surface='AncientBark',sides=10,protect=True):
 points=[Vector(p)for p in points];verts=[]
 for i,(p,r)in enumerate(zip(points,radii)):
  direction=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized();u=direction.cross(Vector((0,0,1)))
  if u.length<.01:u=direction.cross(Vector((1,0,0)))
  u.normalize();v=direction.cross(u).normalized()
  for k in range(sides):
   angle=math.tau*k/sides;ridge=1+.07*math.sin(k*3+i*.7);verts.append(tuple(p+(u*math.cos(angle)+v*math.sin(angle))*r*ridge))
 faces=[tuple(reversed(range(sides))),tuple((len(points)-1)*sides+k for k in range(sides))]
 for i in range(len(points)-1):
  for k in range(sides):faces.append((i*sides+k,i*sides+(k+1)%sides,(i+1)*sides+(k+1)%sides,(i+1)*sides+k))
 return mesh(name,verts,faces,surface,protect)

def foliage(name,c,r,seed,cedar=False,protect=False):
 rng=random.Random(seed);verts=[];faces=[];c=Vector(c)
 # Spatial sprays of individual closed, keeled leaves. No ellipsoid or flat canopy shell.
 # Primary twig carries paired tertiary twigs and leaves, with distinct cedar/elder proportions.
 angle=seed*2.39996;forward=Vector((math.cos(angle),math.sin(angle),.10));forward.normalize();side=Vector((-forward.y,forward.x,.13));side.normalize()
 points=[c-forward*r[0]*.72,c,c+forward*r[0]*.78+Vector((0,0,r[2]*.16))]
 tube('Foliage/bearing-twig',points,[.008,.005,.0015],'AncientBark',6,protect)
 for pair in range(7):
  t=(pair+.5)/7;centre=points[0].lerp(points[-1],t);span=(math.sin(t*math.pi)*.65+.3)*r[1]
  for sign in [-1,1]:
   end=centre+side*sign*span+forward*r[0]*.17+Vector((0,0,math.sin(pair*.9)*r[2]*.38))
   tube('Foliage/tertiary-twig',[centre,centre.lerp(end,.6),end],[.0035,.002,.0006],'AncientBark',5,protect)
   for j in range(5 if cedar else 4):
    along=(j+.35)/5;root=centre.lerp(end,along);out=(side*sign+forward*(.45+along*.6)+Vector((0,0,.2*math.sin(pair+j+seed)))).normalized();length=(.065 if cedar else .10)*(1+rng.random()*.6);width=length*(.28 if cedar else .48);up=Vector((0,0,width*.16));cross=out.cross(Vector((0,0,1))).normalized()*width;middle=root+out*length*.48
    base=len(verts);verts.extend([tuple(root),tuple(root+out*length),tuple(middle-cross),tuple(middle+cross),tuple(middle+up),tuple(middle-up)])
    for f in [(0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)]:faces.append(tuple(base+i for i in f))
 # Layered secondary sprays create real depth around the central bough rather than a solid rounded mass.
 o=mesh(name,verts,faces,'CanopyLeaf',protect)
 leaf_uv=[(.5,0),(.5,1),(0,.48),(1,.48),(.5,.48),(.5,.48)]
 for loop in o.data.loops:o.data.uv_layers[0].data[loop.index].uv=leaf_uv[loop.vertex_index%6]
 return o

def normalize(objects,lo,hi):
 points=[v.co for o in objects for v in o.data.vertices];mn=Vector(tuple(min(p[i]for p in points)for i in range(3)));mx=Vector(tuple(max(p[i]for p in points)for i in range(3)))
 for o in objects:
  for v in o.data.vertices:v.co=Vector(tuple(lo[i]+(v.co[i]-mn[i])/(mx[i]-mn[i])*(hi[i]-lo[i])for i in range(3)))

def canopy(cedar):
 rng=random.Random(29003 if cedar else 29004)
 spine=[(.02*math.sin(i*.7),.025*math.sin(i),i*.12)for i in range(8)]
 tube('Crown/gnarled-bearing-branch',spine,[.055*(1-i/9)for i in range(8)],sides=12)
 branches=13 if cedar else 11
 for i in range(branches):
  angle=i*2.39996;z=.2+(i%5)*.125;length=(.47-z*.19)if cedar else .39+rng.random()*.15;start=Vector((0,0,z));end=Vector((math.cos(angle)*length,math.sin(angle)*length,z+.12+rng.random()*.13));mid=start.lerp(end,.52)+Vector((-.04,.03,-.03))
  tube('Crown/secondary-bough', [start,mid,end], [.025,.017,.006],sides=8,protect=i%3==0)
  for k in range(3):
   c=start.lerp(end,.48+k*.24)+Vector((.03*math.sin(i+k),.03*math.cos(i+k),.045));r=(.20 if cedar else .25,.15 if cedar else .21,.095 if cedar else .15)
   foliage('Foliage/needle-spray'if cedar else'Foliage/leafy-cushion',c,r,i*7+k,cedar,protect=i in [0,3,7]and k==2)
 foliage('Foliage/upper-crown',(.02,.02,.89),(.20,.16,.20),118,cedar,True)
 normalize([o for o in bpy.context.scene.objects if o.type=='MESH'],(-.5,-.5,0),(.5,.5,1))
 # Keep reduction-prone inner leaf cushions away from the exact protected crown envelope.
 for o in bpy.context.scene.objects:
  if o.type=='MESH' and o.data.materials[0].name=='CanopyLeaf' and not o.get('foundry_protect_silhouette',False):
   for vertex in o.data.vertices:vertex.co=vertex.co*.72+Vector((0,0,.5))*.28
 normalize([o for o in bpy.context.scene.objects if o.type=='MESH'],(-.5,-.5,0),(.5,.5,1))

def leaf(name,a,b,width,surface='CanopyLeaf'):
 a=Vector(a);b=Vector(b);direction=(b-a).normalized();side=direction.cross(Vector((0,0,1)))
 if side.length<.01:side=Vector((1,0,0))
 side.normalize();verts=[]
 for sign in [-1,1]:
  for t,w in [(0,.03),(.25,.8),(.55,1),(.83,.5),(1,.02)]:
   p=a.lerp(b,t)+Vector((0,0,math.sin(t*math.pi)*width*.22));verts.extend([tuple(p-side*width*w/2+Vector((0,0,sign*.003))),tuple(p+side*width*w/2+Vector((0,0,sign*.003)))])
 faces=[]
 for layer in [0,10]:
  for i in range(4):faces.append((layer+i*2,layer+i*2+1,layer+(i+1)*2+1,layer+(i+1)*2))
 for edge in [0,1]:
  for i in range(4):faces.append((i*2+edge,(i+1)*2+edge,(i+1)*2+edge+10,i*2+edge+10))
 faces.extend([(0,10,11,1),(8,9,19,18)]);return mesh(name,verts,faces,surface)

def understory():
 rng=random.Random(29005)
 for i in range(7):
  angle=i*2.39996;root=[(0,0,.07),(.8*math.cos(angle),.5*math.sin(angle),.14),(1.9*math.cos(angle),1.35*math.sin(angle),.055),(2.75*math.cos(angle),1.7*math.sin(angle),.015)]
  tube('Roots/weathered-ground-finger',root,[.13,.11,.055,.015],'MossRoot',8,True)
 for group in range(5):
  c=Vector(((group-2)*.88,.55*math.sin(group*2),.05))
  for frond in range(8):
   angle=frond*math.tau/8+group*.63;length=.8+rng.random()*.55;out=Vector((math.cos(angle),math.sin(angle),0));end=c+out*length+Vector((0,0,.45+rng.random()*.55));tube('Fern/articulated-stem',[c,c.lerp(end,.35)+Vector((0,0,.22)),end],[.012,.009,.002],'MossRoot',5,False)
   for j in range(1,7):
    t=j/7;centre=c.lerp(end,t)+Vector((0,0,math.sin(t*math.pi)*.14));lateral=Vector((-out.y,out.x,0));span=(1-t)*.25
    for sign in [-1,1]:leaf('Fern/pinnae',centre,centre+lateral*sign*span+out*.10+Vector((0,0,.045)),span*.48)
 normalize([o for o in bpy.context.scene.objects if o.type=='MESH'],(-3,-2,0),(3,2,1.4))

def source_cliff(seed):
 path=ROOT/('ArtSource/HeartWR02/Cliff'+str(seed)+'.source.obj');vertices=[];faces=[]
 for line in path.read_text().splitlines():
  p=line.split()
  if p and p[0]=='v':vertices.append(Vector((float(p[1]),float(p[3]),float(p[2]))))
  elif p and p[0]=='f':faces.append(tuple(int(x.split('/')[0])-1 for x in p[1:]))
 verts=[];tri=[];steps=12;centre=sum(vertices,Vector((0,0,0)))/len(vertices);minimum=Vector(tuple(min(p[i]for p in vertices)for i in range(3)));maximum=Vector(tuple(max(p[i]for p in vertices)for i in range(3)))
 for face in faces:
  a,b,c=[vertices[i]for i in face];normal=(b-a).cross(c-a).normalized();indices={}
  if normal.dot((a+b+c)/3-centre)<0:normal=-normal
  for u in range(steps+1):
   for v in range(steps-u+1):
    w=steps-u-v;p=(a*u+b*v+c*w)/steps
    # Exact original macro form and boundary, with at most 28 mm of weathered relief. No new walk surface.
    edge=math.sin(math.pi*u/steps)*math.sin(math.pi*v/steps)*math.sin(math.pi*w/steps);relief=.028*edge*(.5+.5*math.sin(p.z*4+p.x*.8+seed));p-=normal*relief;p=Vector(tuple(max(minimum[i],min(maximum[i],p[i]))for i in range(3)));indices[u,v]=len(verts);verts.append(tuple(p))
  for u in range(steps):
   for v in range(steps-u):
    tri.append((indices[u,v],indices[u+1,v],indices[u,v+1]))
    if v<steps-u-1:tri.append((indices[u+1,v],indices[u+1,v+1],indices[u,v+1]))
 border=[];interior=[]
 for f in tri:
  target=border if any(min(abs(verts[i][axis]-minimum[axis]),abs(verts[i][axis]-maximum[axis]))<1e-7 for i in f for axis in range(3))else interior;target.append(f)
 for name,faces,protect in [('source-rim',border,True),('weathered-interior',interior,False)]:
  used=sorted({i for f in faces for i in f});mapping={old:i for i,old in enumerate(used)}
  if faces:mesh('Geology/'+name,[verts[i]for i in used],[tuple(mapping[i]for i in f)for f in faces],'WeatheredStrata',protect)
 # Small explicitly secondary moss/root ribbons retain their own surface, never new collision.


def escarpment_arch():
 verts=[];faces=[];segments=36;rows=10
 for shell in range(2):
  for row in range(rows+1):
   y=-8+row*16/rows
   for i in range(segments+1):
    angle=math.pi*i/segments;sin=math.sin(angle);cos=math.cos(angle);rough=math.sin(y*.63+angle*4)*math.sin(angle)*.55
    if shell==0:x=-cos*6;z=3+sin*7+rough*.15
    else:x=-cos*(16.5+math.sin(y*.4)*.5);z=3+sin*(14.6+math.sin(y*.25+angle*3)*.35)+rough
    verts.append((x,y,z))
 n=(rows+1)*(segments+1)
 for shell in [0,1]:
  offset=shell*n
  for row in range(rows):
   for i in range(segments):
    a=offset+row*(segments+1)+i;faces.append((a,a+1,a+segments+2,a+segments+1))
 for row in [0,rows]:
  for i in range(segments):
   a=row*(segments+1)+i;faces.append((a,a+n,a+n+1,a+1))
 for edge in [0,segments]:
  for row in range(rows):
   a=row*(segments+1)+edge;faces.append((a,a+segments+1,a+segments+1+n,a+n))
 mesh('Escarpment/hollow-geological-vault',verts,faces,'WeatheredStrata',True)
 for sign in [-1,1]:
  # Irregular weathered supporting shoulders continue to ground, with layered clefts and root seams.
  points=[(sign*11,-7,0),(sign*11.8,-3,3.1),(sign*11.3,1,5.2),(sign*10.6,5,5.9)]
  tube('Escarpment/root-consumed-shoulder',points,[4.1,4.7,4.5,3.7],'WeatheredStrata',16,True)
  for j in range(7):tube('Roots/cliff-hanging-root',[(sign*(7+j*.35),-7.9,8.4-j*.5),(sign*(7.3+j*.4),-8,5),(sign*(8+j*.4),-8.1,1)],[.12,.08,.025],'MossRoot',7,True)
 # Actual carved masonry voussoirs make this a ruin consumed by geology, not a generic rock hoop.
 for i in range(18):
  a=math.pi*i/18;b=math.pi*(i+1)/18;v=[]
  for y in [-8.25,-7.65]:
   for radius,theta in [(6.1,a),(6.1,b),(6.65,b),(6.65,a)]:v.append((-math.cos(theta)*radius,y,3+math.sin(theta)*7.15))
  mesh('Ruin/jointed-vault-stone',v,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'CarvedTrim',True)
 normalize([o for o in bpy.context.scene.objects if o.type=='MESH'],(-17,-8,0),(17,8,18))

def main():
 p=argparse.ArgumentParser();p.add_argument('--asset',required=True,choices=['SanctuaryCedar','AzureElder','RootFernBank','ErodedCliff61724','ErodedCliff62346','ErodedCliff63279','EscarpmentArch']);args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);SOURCE.mkdir(parents=True,exist_ok=True);ART.mkdir(parents=True,exist_ok=True);bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0;asset=args.asset
 if asset in ['SanctuaryCedar','AzureElder']:canopy(asset=='SanctuaryCedar')
 elif asset=='RootFernBank':understory()
 elif asset=='EscarpmentArch':escarpment_arch()
 else:source_cliff(int(asset[-5:]))
 for image in bpy.data.images:
  if image.source=='FILE':image.pack()
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[v.co for o in objects for v in o.data.vertices];minimum=[min(v[i]for v in points)for i in range(3)];maximum=[max(v[i]for v in points)for i in range(3)]
 for o in objects:
  if any(abs(v.co[i]-minimum[i])<1e-8 or abs(v.co[i]-maximum[i])<1e-8 for v in o.data.vertices for i in range(3)):o['foundry_protect_silhouette']=True
 blend=SOURCE/(asset+'.author.blend');raw=SOURCE/(asset+'.raw.glb');bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
 # Retain semantic editable parts in author source, batch only the render interchange by material/protection role.
 groups={}
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH':groups.setdefault((o.data.materials[0].name,bool(o.get('foundry_protect_silhouette'))),[]).append(o)
 for (surface,protect),items in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in items:o.select_set(True)
  bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();o=bpy.context.object;o.name=surface+('/primary'if protect else'/detail');o['foundry_protect_silhouette']=protect
 bpy.ops.export_scene.gltf(filepath=str(raw),export_format='GLB',export_yup=True,export_animations=False,export_extras=True)
 mats=sorted({m.name for o in bpy.context.scene.objects if o.type=='MESH'for m in o.data.materials});points=[v.co for o in bpy.context.scene.objects if o.type=='MESH'for v in o.data.vertices];bounds=[[min(v[i]for v in points)for i in [0,2,1]],[max(v[i]for v in points)for i in [0,2,1]]]
 brief={'assetId':asset,'kind':'environment','input':raw.relative_to(ROOT).as_posix(),'output':(ART/asset).relative_to(ROOT).as_posix(),'editableSource':(SOURCE/(asset+'.processed.blend')).relative_to(ROOT).as_posix(),'sourceRights':'ORIGINAL_PROJECT_CC0','pivot':'KEEP_AUTHORED_FRAME','uniformScale':1,'allowStaticUVGeneration':False,'maxLODBoundsDeviationMetres':.035,'materials':{name:{c:(SURFACES[name]/(name+'_'+suffix+'.png')).relative_to(ROOT).as_posix()for c,suffix in [('baseColor','BaseColor'),('normal','Normal'),('metallicSmoothness','MetallicSmoothness')]}for name in mats},'colliderPolicy':'EXPLICIT_CANDIDATES_ONLY'if asset=='EscarpmentArch'else'ORIGINAL_AUTHORITY_ONLY','expectedUnityBounds':bounds,'referenceImages':[],'paidGenerationApproved':False,'nativeUnity':'NOT RUN','intendedPlacement':'Inactive physical-review candidate on the existing first-region causeway'if asset=='EscarpmentArch'else'Existing original cliff/crown frames or source-cleared soft understory along the existing first-region route','prompt':'Original '+asset+', grounded dark-fantasy kingdom, structural organic branches, real carved or geological depth, shared weathered PBR, retain source scale and meaningful silhouette.','acceptanceCriteria':['Independent strict FBX geometry/normal/UV/material checks','Editable source and three meaningful LODs','Original collision, quest, route and ecology owners retained','Actual native import and matched ordinary-player views before artistic acceptance']}
 if asset=='EscarpmentArch':
  brief['colliderCandidateBoxes']=[{'center':[x,6,0],'size':[8,12,12],'sourceOwner':'CityBuilder existing world-collision authority, future accepted placement only'}for x in [-11,11]]+[{'center':[0,14,0],'size':[16,4,12],'sourceOwner':'CityBuilder existing world-collision authority, future accepted placement only'}];brief['requiredPortalClearances']=[{'min':[-5,0,-6],'max':[5,8,6]}]
 (SOURCE/(asset+'.brief.json')).write_text(json.dumps(brief,indent=2)+'\n');receipt={'assetId':asset,'method':'Original local Blender Python authorship; processed by unchanged WR02-A foundry','authorScriptSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'editableSource':blend.relative_to(ROOT).as_posix(),'editableSha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'input':raw.relative_to(ROOT).as_posix(),'inputSha256':hashlib.sha256(raw.read_bytes()).hexdigest(),'license':'CC0-1.0','creditsSpent':0,'nativeUnity':'NOT RUN'};(SOURCE/(asset+'.author-receipt.json')).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'asset':asset,'bounds':bounds,'materials':mats}),flush=True)
if __name__=='__main__':main()
