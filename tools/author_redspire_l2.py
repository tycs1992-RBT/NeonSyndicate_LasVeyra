#!/usr/bin/env python3
"""Original weathered sandstone and skeletal wood, editable source, real UV meshes and shared original 1K maps."""
import argparse,hashlib,json,math
from pathlib import Path
import bpy,numpy as np
from PIL import Image
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);a=p.parse_args();root=a.root.resolve();source=root/'ArtSource/RedspireL2';art=root/'Assets/NeonSyndicate/Art/LiveWilds';evidence=root/'Docs/LiveWildL2';source.mkdir(parents=True,exist_ok=True);evidence.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC';bpy.context.preferences.filepaths.save_version=0
N=1024;y,x=np.mgrid[0:N,0:N].astype(np.float32)/N
rng=np.random.default_rng(218206)
def noise(scale):
 size=max(2,int(scale));small=rng.uniform(0,255,(size,size)).astype(np.uint8);return np.asarray(Image.fromarray(small).resize((N,N),Image.Resampling.BICUBIC),dtype=np.float32)/255-.5
broad=noise(13);fine=noise(200);grain=noise(512);warp=noise(20);materials={};textures=[]
for role in ['Sandstone','Bark']:
 folder=art/'Shared';folder.mkdir(parents=True,exist_ok=True)
 if role=='Sandstone':
  band=np.sin((y+.035*warp+.012*np.sin(x*17))*math.pi*34);strata=np.sin((y+.014*warp)*math.pi*143);fissure=np.maximum(0,np.sin(x*37+warp*9)-.91)*7
  h=.43*broad+.11*band+.035*strata+.13*fine+.035*grain-.19*fissure
  tint=.7+.17*broad+.07*band+.04*strata+.05*fine-.13*fissure
  rgb=np.stack([tint*.68+.05,tint*.36+.055,tint*.20+.06],axis=-1);rough=.84+.12*fine+.03*broad
 else:
  longitudinal=np.sin((x+.025*warp)*math.pi*71);cracks=np.maximum(0,longitudinal-.6)*2.0;h=.18*broad+.13*fine+.16*longitudinal-.18*cracks
  tint=.52+.16*broad+.055*fine+.045*longitudinal-.10*cracks;rgb=np.stack([tint*.60+.09,tint*.49+.075,tint*.36+.065],axis=-1);rough=.9+.1*fine
  # Right quarter is original broken end-grain, selected by cap UVs.
  radius=np.sqrt(((x-.875)*4)**2+((y-.5)*1.2)**2);rings=np.sin(radius*95+warp*2);mask=x>.755
  for c,base in enumerate([.48,.38,.26]):rgb[:,:,c]=np.where(mask,base+.035*rings+.03*broad,rgb[:,:,c])
  h=np.where(mask,.045*rings+.05*fine,h)
 base=folder/(role+'_BaseColor.png');Image.fromarray((np.clip(rgb,0,1)*255).astype(np.uint8)).save(base)
 gy,gx=np.gradient(h);normal=np.stack([-gx*32,-gy*32,np.ones_like(h)],axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True);norm=folder/(role+'_Normal.png');Image.fromarray(((normal*.5+.5)*255).astype(np.uint8)).save(norm)
 packed=np.zeros((N,N,4),dtype=np.uint8);packed[:,:,3]=(np.clip(1-rough,0,1)*255).astype(np.uint8);metal=folder/(role+'_MetallicSmoothness.png');Image.fromarray(packed).save(metal);textures.extend([base,norm,metal])
 m=bpy.data.materials.new('Redspire'+role);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.86;al=m.node_tree.nodes.new('ShaderNodeTexImage');al.image=bpy.data.images.load(str(base));al.image.filepath='//../../'+base.relative_to(root).as_posix();m.node_tree.links.new(al.outputs['Color'],bs.inputs['Base Color']);nm=m.node_tree.nodes.new('ShaderNodeTexImage');nm.image=bpy.data.images.load(str(norm));nm.image.colorspace_settings.name='Non-Color';nm.image.filepath='//../../'+norm.relative_to(root).as_posix();node=m.node_tree.nodes.new('ShaderNodeNormalMap');node.inputs['Strength'].default_value=.55;m.node_tree.links.new(nm.outputs['Color'],node.inputs['Color']);m.node_tree.links.new(node.outputs['Normal'],bs.inputs['Normal']);materials[role]=m

def mesh_object(name,verts,faces,uvs,role):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();uv=mesh.uv_layers.new(name='Authored surface UV')
 for face,coords in zip(mesh.polygons,uvs):
  for loop,coord in zip(face.loop_indices,coords):uv.data[loop].uv=coord
  face.use_smooth=True
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(materials[role]);return o

def rock(kind,variant):
 sides=40 if kind=='Boulder' else 36;rings=20 if kind=='Boulder'else 30;verts=[];faces=[];uvs=[]
 anchors=[0,.08,.18,.26,.34,.43,.52,.61,.70,.76,.80,.88,.95,1];radii=[.58,.64,.55,.61,.42,.51,.38,.43,.26,.35,.68,.77,.62,.36]
 for k in range(rings):
  t=k/(rings-1)
  for i in range(sides):
   theta=i*2*math.pi/sides;co=math.cos(theta);si=math.sin(theta)
   if kind=='Boulder':
    shape=(.60+.37*math.sin(math.pi*t))*(1-.14*t);shape*=1+.055*math.sin(3*theta+.7)+.035*math.sin(7*theta+t*4)
    cut=1-.20*max(0,co)**5*(.4+.6*t);rad=shape*cut;rx=math.copysign(abs(co)**.72,co)*rad;ry=math.copysign(abs(si)**.75,si)*rad
    layer=.025*math.sin(t*math.pi*27+.5*math.sin(theta*2))+.008*math.sin(t*math.pi*67+theta*4)
    height=t*(2.15 if variant==0 else 1.65);vx=(rx+layer*co)*1.65;vy=(ry+layer*si)*1.35
    if variant:height+=.13*t*math.sin(theta*2)+.09*t*co;vx+=.24*t;vy*=.77+.12*co
   else:
    rad=float(np.interp(t,anchors,radii));rad*=1+.075*math.sin(theta*3+t*2)+.025*math.sin(theta*7+t*13)
    # Directional fracture planes and deep wind-worn bands, rather than independent random vertices.
    rad-=.06*max(0,math.cos(theta-.7))**12*math.sin(math.pi*t)**2
    if variant:rad*=1-.32*max(0,math.cos(theta+1.5))**8*math.exp(-((t-.62)/.12)**2)
    rad+=.018*math.sin(t*math.pi*49+math.sin(theta*3));vx=co*rad*1.8+(math.sin(t*2.4)*.35 if variant else t*t*.14);vy=si*rad*1.55+.12*math.sin(t*4);height=t*(7.8 if variant else 7.1)
   verts.append((vx,vy,height))
 for k in range(rings-1):
  for i in range(sides):
   j=(i+1)%sides;faces.append((k*sides+i,k*sides+j,(k+1)*sides+j,(k+1)*sides+i));uvs.append(((i/sides,k/(rings-1)),((i+1)/sides,k/(rings-1)),((i+1)/sides,(k+1)/(rings-1)),(i/sides,(k+1)/(rings-1))))
 for top in [False,True]:
  ring=(rings-1 if top else 0)*sides;indices=[ring+i for i in range(sides)];c=len(verts);verts.append(tuple(sum(verts[j][axis]for j in indices)/sides for axis in range(3)))
  for i in range(sides):
   j=(i+1)%sides;f=(c,ring+i,ring+j)if top else(c,ring+j,ring+i);faces.append(f);uvs.append(tuple((.5+verts[v][0]*.18,.5+verts[v][1]*.18)for v in f))
 return mesh_object(kind+'_'+('A'if variant==0 else'B'),verts,faces,uvs,'Sandstone')

def tree(variant):
 verts=[];faces=[];uvs=[]
 def tube(points,radii,sides):
  start=len(verts)
  for k,p in enumerate(points):
   tangent=Vector(points[min(k+1,len(points)-1)])-Vector(points[max(0,k-1)]);tangent.normalize();u=tangent.cross(Vector((0,1,0)))
   if u.length<.1:u=tangent.cross(Vector((1,0,0)))
   u.normalize();v=tangent.cross(u).normalized()
   for i in range(sides):
    theta=i*2*math.pi/sides;rr=radii[k]*(1+.10*math.sin(theta*5+k*.45));pos=Vector(p)+(u*math.cos(theta)+v*math.sin(theta))*rr;verts.append(tuple(pos))
  for k in range(len(points)-1):
   for i in range(sides):
    j=(i+1)%sides;faces.append((start+k*sides+i,start+k*sides+j,start+(k+1)*sides+j,start+(k+1)*sides+i));uvs.append(((.73*i/sides,k*.25),(.73*(i+1)/sides,k*.25),(.73*(i+1)/sides,(k+1)*.25),(.73*i/sides,(k+1)*.25)))
  for top in [False,True]:
   ring=start+(len(points)-1 if top else 0)*sides;c=len(verts);verts.append(points[-1]if top else points[0])
   for i in range(sides):
    j=(i+1)%sides;f=(c,ring+i,ring+j)if top else(c,ring+j,ring+i);faces.append(f);uvs.append(((.875,.5),(.875+.11*math.cos(i*2*math.pi/sides),.5+.35*math.sin(i*2*math.pi/sides)),(.875+.11*math.cos(j*2*math.pi/sides),.5+.35*math.sin(j*2*math.pi/sides))))
 trunk=[]
 for k in range(14):
  t=k/13;trunk.append((.40*math.sin(t*2.1)*(1 if variant else-1),.19*math.sin(t*4+variant),t*4.2))
 tube(trunk,[.26*(1-k/17)**1.4+.015 for k in range(14)],12)
 for i in range(6):
  theta=(i*2.18+variant*.8);k=5+i%6;base=Vector(trunk[k]);length=1.1+(i%3)*.35
  direction=Vector((math.cos(theta),math.sin(theta),.6 if i%2 else .95)).normalized();points=[]
  for j in range(7):
   t=j/6;points.append(tuple(base+direction*(length*t)+Vector((.10*math.sin(t*3+i),.09*math.sin(t*4+i),-.25*t*t))))
  tube(points,[.13*(1-j/7)**1.35+.008 for j in range(7)],9)
  for fork in [-1,1]:
   b=Vector(points[4]);d=Vector((direction.x+fork*.5,direction.y-fork*.35,.7)).normalized();ps=[tuple(b+d*(j*.20)+Vector((0,0,-.035*j*j)))for j in range(5)];tube(ps,[.055,.047,.035,.022,.008],7)
 for i in range(5):
  theta=i*2*math.pi/5;tube([(math.cos(theta)*.63,math.sin(theta)*.55,0),(math.cos(theta)*.31,math.sin(theta)*.25,.12),trunk[1]],[.035,.10,.17],8)
 return mesh_object('DeadTree_'+('A'if variant==0 else'B'),verts,faces,uvs,'Bark')

objects=[rock(k,v)for k in ['Boulder','RockSpire']for v in [0,1]]+[tree(v)for v in [0,1]];rows=[];files=[]
for original in objects:
 kind=original.name.split('_')[0];folder=art/kind;folder.mkdir(parents=True,exist_ok=True)
 # Exactly one source silhouette is exported at a time. Source keeps the full editable geometry.
 for level,ratio in [(0,1),(1,.48),(2,.20)]:
  o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);o.name=original.name+'_LOD'+str(level);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  if level:
   m=o.modifiers.new('Distance simplification','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name)
  m=o.modifiers.new('Explicit export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=m.name)
  verts2=[o.matrix_world@v.co for v in o.data.vertices];bounds=[[min(v[i]for v in verts2),max(v[i]for v in verts2)]for i in range(3)];triangles=len(o.data.polygons)
  if not o.data.uv_layers or not all(math.isfinite(x)for v in verts2 for x in v):raise ValueError('Invalid authored geometry')
  path=folder/(original.name+'_LOD'+str(level)+'.fbx');bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,path_mode='RELATIVE',mesh_smooth_type='FACE');files.append(path);rows.append({'kind':kind,'variant':original.name,'lod':level,'vertices':len(verts2),'triangles':triangles,'bounds_blender':bounds,'material':o.data.materials[0].name});bpy.data.objects.remove(o,do_unlink=True)
 for obj in objects:obj.hide_set(obj!=original)
 original['authorship']='Original project sculpture and UV geometry; no external inputs';original['units']='metres, source origin at ground'
for o in objects:o.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'RedspireWildernessL2.blend'),compress=True,relative_remap=False);files+=textures+[source/'RedspireWildernessL2.blend']
report={'schema':1,'creator':'Neon Syndicate project environment art','license':'CC0-1.0','rights':'Original geometry and maps, dedicated to the public domain by the project for this delivery','external_inputs':[],'native_unity':'NOT RUN','owner_approval':'PENDING','lods':rows,'files':[{'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}for p in files]};(evidence/'ASSET_MANIFEST_L2.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'assets':len(files),'lods':rows}),flush=True)
