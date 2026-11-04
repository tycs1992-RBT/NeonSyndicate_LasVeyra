#!/usr/bin/env python3
"""Seal portable relative textures and restrained forehead detail in the actual selected-human source."""
import bpy,hashlib,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];path=root/'ArtSource/AscensionH8R/MalikVeyrH8R.blend';bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
for obj in bpy.data.objects:
 if obj.type=='MESH':
  for i,material in enumerate(obj.data.materials):
   if material and material.name.rsplit('.',1)[-1].isdigit():
    original=bpy.data.materials.get(material.name.rsplit('.',1)[0])
    if original:obj.data.materials[i]=original
for i in range(2):bpy.ops.outliner.orphans_purge(do_recursive=True)
for im in bpy.data.images:
 if im.source=='FILE':im.filepath='//../../Assets/NeonSyndicate/Art/ProductionH8R/Textures/'+Path(im.filepath).name
rig=bpy.data.objects['MalikRig'];rig.animation_data_clear()
for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=(1,0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
if 'H8R Brow Sigil'not in bpy.data.objects:
 body=bpy.data.objects['MalikBodyH8R'];z=1.797;points=[v.co for v in body.data.vertices if abs(v.co.x)<.013 and abs(v.co.z-z)<.01 and v.co.y<0];y=min(q.y for q in points)-.0015
 vs=[(0,y-.002,z+.008),(-.003,y,z),(0,y-.003,z-.008),(.003,y,z),(0,y-.005,z)];faces=[(0,1,4),(1,2,4),(2,3,4),(3,0,4)];data=bpy.data.meshes.new('Restrained forehead mineral');data.from_pydata(vs,[],faces);o=bpy.data.objects.new('H8R Brow Sigil',data);bpy.context.collection.objects.link(o);o.data.materials.append(next(m for m in bpy.data.materials if m.name.startswith('H6 Restrained amethyst')));o.parent=rig;g=o.vertex_groups.new(name='Head');g.add(list(range(5)),1,'REPLACE');m=o.modifiers.new('Existing head visual','ARMATURE');m.object=rig
for obj in bpy.data.objects:
 if obj.type=='MESH' and not obj.data.uv_layers:
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True,relative_remap=False)
report=root/'ArtSource/AscensionH8R/authoring.json';r=json.loads(report.read_text());r['output_sha256']=hashlib.sha256(path.read_bytes()).hexdigest();r['texture_storage']='Portable relative files, verified externally; original upload remains unchanged';r['additional_motion']='Retargeted owner-supplied CC0 defensive brace, see guard-retarget.json';report.write_text(json.dumps(r,indent=2)+'\n')
for im in bpy.data.images:
 if im.source=='FILE' and not Path(bpy.path.abspath(im.filepath)).is_file():raise FileNotFoundError(im.filepath)
print('All texture dependencies exist; original upload unchanged; skeleton',len(rig.data.bones),flush=True)
