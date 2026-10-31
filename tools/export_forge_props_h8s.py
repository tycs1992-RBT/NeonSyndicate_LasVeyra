#!/usr/bin/env python3
"""Export three admitted static furnishings with explicit packed-channel conversion and no gameplay scripts."""
import argparse,hashlib,json,zipfile
from pathlib import Path
import bpy,numpy as np
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('--archive',type=Path,required=True);a=p.parse_args();root=Path(__file__).resolve().parents[1];out=root/'Assets/NeonSyndicate/Art/ProductionH8S/Props';stage=a.archive.parent/'quarantine-props-h8s';stage.mkdir(exist_ok=True)
if hashlib.sha256(a.archive.read_bytes()).hexdigest()!='8b6f7e806d222e585478f0e1bdc6b271bbc7bc6f84dd6af8ca703a7c64f0cb1e':raise ValueError('Archive changed')
files=[]
with zipfile.ZipFile(a.archive)as z:
 def selected(suffix):
  rows=[i for i in z.infolist()if i.filename==suffix]
  if len(rows)!=1 or rows[0].file_size>64*1024*1024 or '..'in Path(rows[0].filename).parts or (rows[0].external_attr>>16)&0o170000==0o120000:raise ValueError('Unsafe or ambiguous member')
  return z.read(rows[0])
 for role in ['Metal','Furniture']:
  for kind in ['BaseColor','Normal','ORM']:
   raw=selected('Textures/T_Trim_'+role+'_'+kind+'.png');file=out/(role+'_'+kind+'.png');file.write_bytes(raw);files.append(file)
  orm=np.array(Image.open(out/(role+'_ORM.png')).convert('RGBA'));packed=np.zeros_like(orm);packed[:,:,0]=orm[:,:,2];packed[:,:,3]=255-orm[:,:,1];file=out/(role+'_MetallicSmoothness.png');Image.fromarray(packed).save(file);files.append(file)
 for name in ['Anvil','Workbench','BookStand']:
  raw=selected('Exports/FBX/'+name+'.fbx');file=stage/(name+'.fbx');file.write_bytes(raw);bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(file),use_anim=False)
  for o in list(bpy.data.objects):
   if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
  for o in bpy.data.objects:
   if o.type=='MESH':
    if not o.data.uv_layers:raise ValueError('Missing source UV')
    for mod in list(o.modifiers):o.modifiers.remove(mod)
  target=out/(name+'.fbx');bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='RELATIVE');files.append(target)
r={'license':'CC0-1.0','creator':'Quaternius','commercialUse':True,'publicSourceApproved':True,'enabled':False,'native_unity':'NOT RUN','colliders':'Original Forge collider only; gallery pieces are decorative visual previews','files':[{'path':str(f.relative_to(root)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'bytes':f.stat().st_size}for f in files]};(out/'candidate.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r),flush=True)
