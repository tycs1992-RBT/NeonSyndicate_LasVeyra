#!/usr/bin/env python3
"""Prepare two distinct supplied CC0 humanoid studies. No NPC brain, identity or automatic activation is changed."""
import argparse,bpy,hashlib,json,zipfile
from pathlib import Path
from mathutils import Matrix,Vector
p=argparse.ArgumentParser();p.add_argument('--intake',type=Path,required=True);p.add_argument('--archive',type=Path,required=True);a=p.parse_args();root=Path(__file__).resolve().parents[1];out=root/'Assets/NeonSyndicate/Art/ProductionH8R/NPC';out.mkdir(exist_ok=True);records=[]
for role,source,token in [('GuildArtisan','Superhero_Female_FullBody.fbx','Female'),('CourtDefender','Superhero_Male_FullBody.fbx','Male')]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str((a.intake/source).resolve()),use_anim=False);rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');rig.name=role+'Rig'
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 weighted={g.group for o in bpy.data.objects if o.type=='MESH'for v in o.data.vertices for g in v.groups if g.weight>0}
 # Supplied terminal bones have real skin weights. Preserve all 65, with original passive mapping.
 if len(rig.data.bones)!=65:raise ValueError('Unexpected supplied NPC rig version')
 oldobjects=set(bpy.data.objects)
 with bpy.data.libraries.load(str(root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend'),link=False)as (available,loaded):loaded.objects=available.objects
 oldrig=next(o for o in loaded.objects if o.type=='ARMATURE');bpy.context.scene.collection.objects.link(oldrig);old=oldrig.matrix_world.copy();new=rig.matrix_world.copy();clothes=[]
 for o in loaded.objects:
  if not o or o.type!='MESH':continue
  name=o.name.lower()
  if not any(k in name for k in ['coat','collar','lapel','shoulder armor','forearm fitting','chest plate','armor edge','walking panel','leather trousers','riding boots']):continue
  if role=='GuildArtisan'and any(k in name for k in ['shoulder armor','chest plate','armor edge']):continue
  bpy.context.scene.collection.objects.link(o);clothes.append(o)
 bpy.context.view_layer.update()
 for o in clothes:
  world=o.matrix_world.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);groups={g.index:g.name for g in o.vertex_groups}
  for v in o.data.vertices:
   point=world@v.co;rows=[(groups[g.group],g.weight)for g in v.groups if groups[g.group]in rig.data.bones and groups[g.group]in oldrig.data.bones and g.weight>0];total=sum(w for _,w in rows);pos=Vector()
   if total<=0:raise ValueError('NPC clothing lost an original weight')
   for n,w in rows:pos+=(new@rig.data.bones[n].matrix_local@(old@oldrig.data.bones[n].matrix_local).inverted()@point)*(w/total)
   v.co=pos
  for m in list(o.modifiers):o.modifiers.remove(m)
  o.parent=rig;o.matrix_world=Matrix.Identity(4);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  if len(o.data.polygons)>250:m=o.modifiers.new('Bounded NPC garment detail','DECIMATE');m.ratio=.18;bpy.ops.object.modifier_apply(modifier=m.name)
  m=o.modifiers.new('Original NPC pose compatibility','ARMATURE');m.object=rig
 for o in list(bpy.data.objects):
  if o not in oldobjects and o not in clothes:bpy.data.objects.remove(o,do_unlink=True)
 # Bake a portable reduced-resolution albedo for each study; preserve source UV and actual distinct faces.
 z=zipfile.ZipFile(a.archive);suffix='T_Superhero_Female_Dark_BaseColor.png'if token=='Female'else'T_Superhero_Male_Dark.png';member=next(n for n in z.namelist()if '/Base Characters/Textures/'in n and Path(n).name==suffix);texture=out/(role+'_Albedo.png');texture.write_bytes(z.read(member));im=bpy.data.images.load(str(texture));im.scale(1024,1024);im.filepath_raw=str(texture);im.file_format='PNG';im.save();im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/NPC/'+texture.name
 for o in oldobjects:
  if o.type!='MESH':continue
  for m in o.data.materials:
   if not m:continue
   if 'Superhero'in m.name or 'SuperHero'in m.name:
    m.name=role+' Skin and underlayer';m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;m.node_tree.links.new(n.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.7
 for o in bpy.data.objects:
  if o.type=='MESH':
   o.name=role+' '+o.name.replace('Superhero','Human').replace('SuperHero','Human');
   for v in o.data.vertices:
    rows=[(g.group,g.weight)for g in v.groups if o.vertex_groups[g.group].name in rig.data.bones and g.weight>0];total=sum(w for _,w in rows)
    if not rows:raise ValueError('Unweighted actual supplied NPC vertex')
    for i,w in rows:o.vertex_groups[i].add([v.index],w/total,'REPLACE')
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig;target=out/(role+'H8R.fbx');bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE')
 for im in bpy.data.images:
  if im.source=='FILE':
   candidate=root/'Assets/NeonSyndicate/Art/ProductionH8R/Textures'/Path(im.filepath).name
   if candidate.is_file():im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+candidate.name
 bpy.context.preferences.filepaths.save_version=0;blend=root/('ArtSource/AscensionH8R/'+role+'Study.blend');bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True,relative_remap=False)
 records.append({'role':role,'donor_sha256':hashlib.sha256((a.intake/source).read_bytes()).hexdigest(),'bones':len(rig.data.bones),'vertices':sum(len(o.data.vertices)for o in bpy.data.objects if o.type=='MESH'),'export_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'source_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'license':'CC0-1.0','enabled':False,'native_import':'NOT RUN','appearance':'Unapproved study; supplied faces are distinct, garment fit unfinished','integration':'Prepared for existing visual-only import adapter, no NPC replacement or crowd spawning'})
(out/'studies.json').write_text(json.dumps({'author':'Quaternius and original project fitted wardrobe','source_url':'https://quaternius.com/','license':'CC0-1.0','candidates':records,'native_unity':'NOT RUN'},indent=2)+'\n');print(json.dumps(records),flush=True)
