"""Blender detail and UV production on the exact existing source cliffs. No displacement or new physical volume."""
import bpy,hashlib,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];art=root/'Assets/NeonSyndicate/Art/HeartWR02';src=root/'ArtSource/HeartWR02'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
material=bpy.data.materials.new('WeatheredStrata');material.use_nodes=True;material.diffuse_color=(.32,.34,.27,1)
shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.88
tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(art/'WeatheredStrata_BaseColor.png'));material.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
rows=[];inputs=[]
for path in sorted(src.glob('Cliff*.source.obj')):
 name=path.name.split('.')[0];vertices=[];faces=[]
 for line in path.read_text().splitlines():
  row=line.split()
  if row[0]=='v':
   x,y,z=map(float,row[1:]);vertices.append((x,z,y))
  elif row[0]=='f':faces.append(tuple(reversed([int(s)-1 for s in row[1:]])))
 inputs.append({'path':path.relative_to(root).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size,'basis':'Exact compiled original C# vertices and triangles, local to original rock position. Reversed winding accounts for the Y/Z handedness change.'})
 for lod,subdivision in ((0,2),(1,1),(2,0)):
  mesh=bpy.data.meshes.new(name+'_LOD'+str(lod));mesh.from_pydata(vertices,[],faces);mesh.materials.append(material);mesh.update()
  obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
  if subdivision:
   modifier=obj.modifiers.new('Surface-only midpoint detail','SUBSURF');modifier.subdivision_type='SIMPLE';modifier.levels=subdivision;bpy.ops.object.modifier_apply(modifier=modifier.name)
  modifier=obj.modifiers.new('Export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=modifier.name)
  uv=obj.data.uv_layers.new(name='UV0')
  for polygon in obj.data.polygons:
   axis=max(range(3),key=lambda i:abs(polygon.normal[i]));axes=[i for i in range(3) if i!=axis]
   for index in polygon.loop_indices:
    p=obj.data.vertices[obj.data.loops[index].vertex_index].co;uv.data[index].uv=(p[axes[0]]/4,p[axes[1]]/4)
  exported=art/(obj.name+'.fbx')
  bpy.ops.export_scene.fbx(filepath=str(exported),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,bake_anim=False,add_leaf_bones=False,use_tspace=True,path_mode='STRIP')
  lo=[min(v.co[i] for v in obj.data.vertices) for i in range(3)];hi=[max(v.co[i] for v in obj.data.vertices) for i in range(3)]
  rows.append({'name':name,'lod':lod,'path':exported.relative_to(root).as_posix(),'bytes':exported.stat().st_size,'sha256':hashlib.sha256(exported.read_bytes()).hexdigest(),'vertices':len(obj.data.vertices),'triangles':len(obj.data.polygons),'source_bounds':[lo,hi],'unity_bounds':[[lo[0],lo[2],lo[1]],[hi[0],hi[2],hi[1]]],'material_slots':['WeatheredStrata'],'source_surface':inputs[-1],'fit_policy':'Original local metres and origin, scale 1. Simple subdivisions remain on original triangle planes. No displaced or new traversable volume.'})
  obj.hide_render=lod!=0;obj.hide_set(lod!=0)
for image in bpy.data.images:
 if image.source=='FILE':image.pack()
blend=src/'ExistingEscarpmentsWR02.blend';bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
manifest=json.loads((art/'manifest.json').read_text());manifest['modules']=[row for row in manifest['modules'] if not row['name'].startswith('Cliff')]+rows
manifest['additional_sources']=[{'path':blend.relative_to(root).as_posix(),'bytes':blend.stat().st_size,'sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'kind':'Editable source-specific original cliff surface production'}]+inputs
manifest['geometry_contract']='Three normalized noncolliding crowns and three source-specific cliffs. Original source cliff planes and every physical owner stay fixed. Native import/contact/visual acceptance NOT RUN.'
(art/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');(src/'SOURCE_RECEIPT.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'cliff_lods':len(rows),'source_bytes':blend.stat().st_size,'triangles_by_lod':[[r['name'],r['lod'],r['triangles']] for r in rows]}),flush=True)
