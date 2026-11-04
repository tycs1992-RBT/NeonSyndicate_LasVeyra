#!/usr/bin/env python3
"""Read-only art-drop inventory. Does not extract, import, execute, purchase or approve assets."""
import argparse,hashlib,json,zipfile
from collections import Counter
from pathlib import Path,PurePosixPath
MODEL={'.fbx','.dae','.glb','.gltf','.blend','.obj'}
TEXTURE={'.png','.jpg','.jpeg','.tga','.exr','.tif','.tiff','.psd'}
CODE={'.cs','.dll','.exe','.so','.dylib','.js','.unitypackage'}
def kind(name):
 path=PurePosixPath(name.replace('\\','/'));extension=path.suffix.lower();lower=name.lower()
 if extension in CODE:return 'EXECUTABLE_OR_IMPORT_PACKAGE_REVIEW_REQUIRED'
 if extension in MODEL:return 'model_interchange_requires_rig_validation'
 if extension in {'.anim','.controller','.overridecontroller'}:return 'animation_or_controller_requires_authority_review'
 if extension in TEXTURE:return 'REFERENCE_ONLY'if 'reference' in lower or 'annotated_board' in lower else 'image_not_proof_of_pbr_or_rig'
 return 'document_or_other'
def inspect(source):
 source=Path(source);items=[];unsafe=[]
 if source.is_dir():
  for p in sorted(source.rglob('*')):
   if p.is_symlink():unsafe.append(str(p.relative_to(source)));continue
   if not p.is_file():continue
   rel=p.relative_to(source).as_posix();data=p.read_bytes();items.append({'path':rel,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'kind':kind(rel)})
 else:
  with zipfile.ZipFile(source) as archive:
   for entry in archive.infolist():
    if entry.is_dir():continue
    name=entry.filename.replace('\\','/');path=PurePosixPath(name)
    if path.is_absolute() or '..' in path.parts or ':' in name or (entry.external_attr>>16)&0o170000==0o120000:unsafe.append(name);continue
    data=archive.read(entry);items.append({'path':name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'kind':kind(name)})
 return {'scope':__doc__,'source_name':source.name,'counts':dict(Counter(i['kind']for i in items)),'files':items,'unsafe_paths':unsafe,'rights_status':'UNKNOWN, require actual creator/license/owner approval','native_import':'NOT RUN'}
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('source',type=Path);p.add_argument('--report',type=Path,required=True);a=p.parse_args();report=inspect(a.source);a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'files':len(report['files']),'counts':report['counts'],'unsafe_paths':report['unsafe_paths']}));return int(bool(report['unsafe_paths']))
if __name__=='__main__':raise SystemExit(main())
