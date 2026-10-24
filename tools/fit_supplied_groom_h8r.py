#!/usr/bin/env python3
"""Fit the owner's documented CC0 textured groom to the selected face; retain original rig authority."""
import bpy,argparse,hashlib,json,shutil
from pathlib import Path
from mathutils import Vector,Matrix
p=argparse.ArgumentParser();p.add_argument('--intake',type=Path,required=True);a=p.parse_args();root=Path(__file__).resolve().parents[1];path=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';out=root/'Assets/NeonSyndicate/Art/ProductionH8R/Textures';bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False);rig=bpy.data.objects['MalikRig'];before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str((a.intake/'Superhero_Male_FullBody.fbx').resolve()),use_anim=False);new=[o for o in bpy.data.objects if o not in before];donor=next(o for o in new if o.type=='ARMATURE');bpy.context.view_layer.update();donorHead=(donor.matrix_world@donor.data.bones['Head'].matrix_local).translation;head=rig.data.bones['Head'].head_local.copy();print('HEADS',list(donorHead),list(head),flush=True)
for o in new:bpy.data.objects.remove(o,do_unlink=True)
for o in list(bpy.data.objects):
 if o.type=='MESH'and ('swept undercut'in o.name or 'neat beard'in o.name or o.name.startswith(('H8R Short parted hair','H8R Groomed beard','H8R Fitted short hair'))):bpy.data.objects.remove(o,do_unlink=True)
material=bpy.data.materials.new('H8R Textured groom');material.use_nodes=True;bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.76;bs.inputs['Specular IOR Level'].default_value=.22
for name,target,normal in [('T_Hair_1_BaseColor.png','Groom_Albedo.png',False),('T_Hair_1_Normal.png','Groom_Normal.png',True)]:
 shutil.copyfile(a.intake/name,out/target);im=bpy.data.images.load(str(out/target));im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+target;n=material.node_tree.nodes.new('ShaderNodeTexImage');n.image=im
 if normal:im.colorspace_settings.name='Non-Color';nn=material.node_tree.nodes.new('ShaderNodeNormalMap');nn.inputs['Strength'].default_value=.35;material.node_tree.links.new(n.outputs['Color'],nn.inputs['Color']);material.node_tree.links.new(nn.outputs['Normal'],bs.inputs['Normal'])
 else:
  mix=material.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.018,.011,.007,1);material.node_tree.links.new(n.outputs['Color'],mix.inputs[1]);material.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color']);material.node_tree.links.new(n.outputs['Alpha'],bs.inputs['Alpha'])
material.surface_render_method='DITHERED';rows=[]
for name,role in [('Hair_Buzzed.fbx','Fitted short hair'),('Hair_Beard.fbx','Groomed beard')]:
 old=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str((a.intake/name).resolve()),use_anim=False);objects=[o for o in bpy.data.objects if o not in old]
 for o in objects:
  if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True);continue
  mat=o.matrix_world.copy();o.parent=None;o.matrix_world=Matrix.Identity(4)
  for v in o.data.vertices:
   q=mat@v.co
   # World-origin hair variants share the donor's head position. Uniform fitting is visual-only.
   v.co=head+(q-donorHead)*.94
  if role=='Fitted short hair':
   shift=max(v.co.z for v in o.data.vertices)-1.865
   for v in o.data.vertices:v.co.z-=shift
  else:
   from mathutils.bvhtree import BVHTree
   body=bpy.data.objects['MalikBodyH8R'];surface=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(f.vertices)for f in body.data.polygons])
   for v in o.data.vertices:
    v.co.z=1.68+(v.co.z-1.72)*.75
    hit=surface.ray_cast(Vector((v.co.x,-1,v.co.z)),Vector((0,1,0)),2)[0]
    if hit is not None:v.co.y=hit.y-.002
  o.name='H8R '+role;o.data.materials.clear();o.data.materials.append(material)
  for g in list(o.vertex_groups):o.vertex_groups.remove(g)
  g=o.vertex_groups.new(name='Head');g.add(list(range(len(o.data.vertices))),1,'REPLACE');o.parent=rig;m=o.modifiers.new('Existing head skin','ARMATURE');m.object=rig
  for f in o.data.polygons:f.use_smooth=True
  rows.append({'role':role,'vertices':len(o.data.vertices),'source_sha256':hashlib.sha256((a.intake/name).read_bytes()).hexdigest()})
for i in range(3):bpy.ops.outliner.orphans_purge(do_recursive=True)
material.name='H8R Textured groom'
for im in bpy.data.images:
 if im.source=='FILE':im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+Path(im.filepath).name
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True,relative_remap=False)
(root/'ArtSource/AscensionH8R/groom-fit.json').write_text(json.dumps({'author':'Quaternius','license':'CC0-1.0','source_url':'https://quaternius.com/','parts':rows,'native_unity':'NOT RUN','appearance_approval':'NOT GIVEN'},indent=2)+'\n')
print(json.dumps(rows),flush=True)
