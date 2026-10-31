#!/usr/bin/env python3
"""Export the fitted male study with complete skin weights and three actual LODs."""
import bpy,argparse,hashlib,json,shutil,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--archives',type=Path,required=True);a=p.parse_args();root=Path(__file__).resolve().parents[1];out=root/'Assets/NeonSyndicate/Art/ProductionH8S/NPC'
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AscensionH8S/DanteRookH8S.blend'),load_ui=False,use_scripts=False);rig=bpy.data.objects['DanteRig'];assert len(rig.data.bones)==65
with zipfile.ZipFile(a.archives/'Universal Base Characters[Standard].zip') as z:
 for name in ['T_Eye_Brown.png','T_Eye_Normal.png','T_Superhero_Male_Normal.png']:
  n=next(n for n in z.namelist() if n.endswith('/Base Characters/Textures/'+name));file=out/name;file.write_bytes(z.read(n));image=bpy.data.images.load(str(file));image.scale(1024,1024);image.filepath_raw=str(file);image.save()
shutil.copyfile(root/'Assets/NeonSyndicate/Art/ProductionH8R/NPC/CourtDefender_Albedo.png',out/'DanteAlbedo.png')
originals=[o for o in bpy.data.objects if o.type=='MESH'];files=[];stats=[]
def active(o):bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for level,ratio in [(0,1),(1,.6),(2,.3)]:
 copies=[]
 for original in originals:
  o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);o.name='LOD'+str(level)+'_'+original.name;active(o)
  if ratio<1 and len(o.data.polygons)>180:
   for m in list(o.modifiers):o.modifiers.remove(m)
   m=o.modifiers.new('Distance simplification','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name)
   m=o.modifiers.new('Complete original skin','ARMATURE');m.object=rig
  for v in o.data.vertices:
   rows=[(g.group,g.weight)for g in v.groups if o.vertex_groups[g.group].name in rig.data.bones and g.weight>0];total=sum(w for _,w in rows)
   if not rows or len(rows)>8:raise ValueError('Invalid complete LOD skin')
   for i,w in rows:o.vertex_groups[i].add([v.index],w/total,'REPLACE')
  copies.append(o)
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
 for o in copies:o.select_set(True)
 for o in bpy.data.objects:
  if o.type=='EMPTY':o.select_set(True)
 bpy.context.view_layer.objects.active=rig;file=out/('DanteRookH8S'+('' if level==0 else '_LOD'+str(level))+'.fbx');bpy.ops.export_scene.fbx(filepath=str(file),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE');files.append(file);stats.append({'lod':level,'vertices':sum(len(o.data.vertices)for o in copies),'skins':len(copies)})
 for o in copies:bpy.data.objects.remove(o,do_unlink=True)
files+=list(out.glob('*.png'));r={'assetId':'DANTE-H8S-REVIEW-01','creator':'CC0 distinct male study and project fitted work clothing','license':'CC0-1.0','commercialUse':True,'publicSourceApproved':True,'preserveInfluences':True,'ownerApproved':False,'enabled':False,'bones':65,'lods':stats,'files':[{'path':str(f.relative_to(root)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'bytes':f.stat().st_size}for f in files],'native_unity':'NOT RUN'};(out/'candidate.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(stats),flush=True)
