#!/usr/bin/env python3
"""Fit lawful human source parts in a real DCC. Outputs art, never a gameplay controller."""
import argparse,json,sys
from pathlib import Path
import bpy
p=argparse.ArgumentParser();p.add_argument('--mpfb',type=Path,required=True);p.add_argument('--assets',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
bpy.context.preferences.extensions.repos.new(name='H6 local authoring',module='h6',custom_directory=str(a.mpfb/'src'))
bpy.ops.preferences.addon_enable(module='bl_ext.h6.mpfb')
from bl_ext.h6.mpfb.services.humanservice import HumanService
from bl_ext.h6.mpfb.services.targetservice import TargetService
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict();macro.update({'gender':1.0,'age':.56,'muscle':.62,'weight':.48,'height':.57,'proportions':.55});macro['race']={'african':.65,'asian':.05,'caucasian':.30}
body=HumanService.create_human(macro_detail_dict=macro);body.name='MalikBody';rig=HumanService.add_builtin_rig(body,'game_engine');rig.name='MalikRig'
parts=[('Iris','system/eyes/high-poly/high-poly.mhclo','Eyes'),('Brows','system/eyebrows/eyebrow001/eyebrow001.mhclo','Eyebrows'),('Lashes','system/eyelashes/eyelashes01/eyelashes01.mhclo','Eyelashes'),('Boots','system/clothes/shoes06/shoes06.mhclo','Clothes')]
for role,rel,kind in parts:
 source=a.assets/rel
 if not source.exists():raise FileNotFoundError(source)
 obj=HumanService.add_mhclo_asset(str(source),body,asset_type=kind,subdiv_levels=0,material_type='NONE',set_up_rigging=True,import_subrig=False)
 obj.name=role
 print('FITTED',role,len(obj.data.vertices),len(obj.data.polygons),flush=True)
a.output.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(a.output),compress=True)
a.output.with_suffix('.json').write_text(json.dumps({'macro':macro,'source_rig':'game_engine','rig_bones':len(rig.data.bones),'source_parts':[{'role':role,'path':rel}for role,rel,_ in parts],'native_unity':'NOT RUN','owner_approval':'NOT GIVEN'},indent=2)+'\n')
print('FIT FILE WRITTEN',a.output,flush=True)
# The addon crashes in Python-module finalization after a successful save on this host.
# Replace that finalization path with an independent fresh-process reopen validator.
# An export/fit exception never reaches this handoff; the validator must itself succeed.
import os
os.execv(sys.executable,[sys.executable,str(Path(__file__).with_name('validate_fit_malik_h6.py')),str(a.output)])
