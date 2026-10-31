"""Original editable canopy families. Original game trunks and physics remain authoritative."""
import bpy,math,random,json,hashlib,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];art=root/'Assets/NeonSyndicate/Art/HeartWR02';source=root/'ArtSource/HeartWR02';source.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('CanopyLeaf');mat.diffuse_color=(.11,.24,.055,1);mat.use_nodes=True
shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.82
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(art/'CanopyLeaf_BaseColor.png'));mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
def leaf(verts,faces,uvs,p,side,length,width,tilt):
 along=Vector(side).normalized();cross=along.cross(Vector((0,0,1))).normalized()
 if cross.length<.1: cross=Vector((1,0,0))
 across=(cross*math.cos(tilt)+Vector((0,0,1))*math.sin(tilt)).normalized()
 pts=[p-along*length*.5,p+across*width*.5,p+along*length*.5,p-across*width*.5,p+Vector((0,0,width*.08)),p-Vector((0,0,width*.08))]
 start=len(verts);verts.extend(pts);coords=[(.5,0),(1,.5),(.5,1),(0,.5),(.5,.5)]
 for a,b in ((0,1),(1,2),(2,3),(3,0)):
  faces.append((start+a,start+b,start+4));uvs.append((coords[a],coords[b],coords[4]))
  faces.append((start+5,start+b,start+a));uvs.append((coords[4],coords[b],coords[a]))
def canopy(name,variant,lod):
 r=random.Random(8100+variant);verts=[];faces=[];uvs=[]
 # Stable skeletal branch volumes define three intentionally different crown silhouettes.
 lobes=[]
 if variant==0:
  for i in range(17):
   a=i*2.39996;rad=.25+.65*(i%4)/3;lobes.append((Vector((math.cos(a)*rad,math.sin(a)*rad*.83,.55+(i%5)*.13)),Vector((.5,.43,.32))))
 elif variant==1:
  for level in range(6):
   for i in range(5):
    a=i*math.pi*2/5+level*.57;rad=(1-level*.135)*.75;lobes.append((Vector((math.cos(a)*rad,math.sin(a)*rad,level*.2)),Vector((.4-level*.035,.32-level*.026,.22))))
 else:
  for i in range(19):
   a=i*2.39996;t=i/18;lobes.append((Vector((t*.85+math.cos(a)*.38,math.sin(a)*.65,.25+t*.8)),Vector((.45,.32,.23))))
 count=(95,42,12)[lod]
 for center,radii in lobes:
  for j in range(95):
   v=Vector((r.gauss(0,.55),r.gauss(0,.55),r.gauss(0,.45)));v=Vector((max(-1,min(1,v.x)),max(-1,min(1,v.y)),max(-1,min(1,v.z))))
   p=center+Vector((v.x*radii.x,v.y*radii.y,v.z*radii.z));angle=r.uniform(0,math.pi*2);length=r.uniform(.11,.19)*(1 if lod==0 else 1.5 if lod==1 else 2.4)
   if j<count: leaf(verts,faces,uvs,p,(math.cos(angle),math.sin(angle),r.uniform(-.25,.25)),length,length*.47,r.uniform(-.9,.9))
 # Normalize this natural crown only. It is fitted to the actual source leaf envelope, never a building.
 lo=Vector((min(v.x for v in verts),min(v.y for v in verts),min(v.z for v in verts)));hi=Vector((max(v.x for v in verts),max(v.y for v in verts),max(v.z for v in verts)))
 for i,v in enumerate(verts): verts[i]=((v.x-lo.x)/(hi.x-lo.x)-.5,(v.y-lo.y)/(hi.y-lo.y)-.5,(v.z-lo.z)/(hi.z-lo.z))
 mesh=bpy.data.meshes.new(name+'_LOD'+str(lod));mesh.from_pydata(verts,[],faces);mesh.materials.append(mat);mesh.update()
 uv=mesh.uv_layers.new(name='UV0')
 for poly,coords in zip(mesh.polygons,uvs):
  for idx,co in zip(poly.loop_indices,coords): uv.data[idx].uv=co
 obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj);obj['source']='Original project-authored foliage, original game trunk and collider retained';obj['LOD']=lod
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 path=art/(mesh.name+'.fbx');bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',use_space_transform=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,use_tspace=True,use_custom_props=False,path_mode='STRIP')
 return {'name':name,'lod':lod,'path':path.relative_to(root).as_posix(),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'vertices':len(verts),'triangles':len(faces),'source_bounds':[[-.5,-.5,0],[.5,.5,1]],'unity_bounds':[[-.5,0,-.5],[.5,1,.5]],'material_slots':['CanopyLeaf']}
rows=[]
for v,name in enumerate(('CanopySpreading','CanopyPyramidal','CanopyBent')):
 for lod in range(3): rows.append(canopy(name,v,lod))
for image in bpy.data.images:
 if image.source=='FILE': image.pack()
blend=source/'LivingCanopiesWR02.blend';bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
textures=[]
for p in sorted(art.glob('*.png')): textures.append({'name':p.stem,'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
manifest={'schema_version':1,'license':'CC0-1.0','creator':'Original project-authored WR02 foliage and tileable surface study','source':blend.relative_to(root).as_posix(),'source_bytes':blend.stat().st_size,'source_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'axis':'Blender Z-up, FBX -Z forward Y-up, metres','pivot':'Normalized natural canopy envelope at base center; original source trunk and physical tree remain','modules':rows,'textures':textures,'native_unity':'NOT RUN','collision_policy':'No new collider. Natural foliage only fits exact original crown bounds. Physical terrain, roots, trunks, roads, apertures and saved actors stay under existing owners.'}
(art/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');(source/'SOURCE_RECEIPT.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'source_bytes':blend.stat().st_size,'FBX':len(rows),'maps':len(textures),'triangles_LOD0':sum(r['triangles'] for r in rows if r['lod']==0)}),flush=True)
