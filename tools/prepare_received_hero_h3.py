#!/usr/bin/env python3
"""Prepare the received CC0 base as a DISABLED art candidate, never as approved Malik. Requires both lawfully obtained source archives."""
import argparse,hashlib,io,json,re,uuid,zipfile
from pathlib import Path
from PIL import Image,ImageOps
ROOT=Path(__file__).resolve().parents[1]
BASE_HASH='fdbf1804c90dfc1ea03e992bff7da2dfd1a79318e13270a660180f9308455f40'
OUTFIT_HASH='c3468b18871cc8c8f05ab14df7712baf22cb9f389cbd870babf130e595187f70'
def sha(b):return hashlib.sha256(b).hexdigest()
def meta(path,kind='DefaultImporter'):
 text='fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,path.relative_to(ROOT).as_posix()).hex+'\n'
 if path.is_dir():text+='folderAsset: yes\n'
 text+=kind+':\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n';Path(str(path)+'.meta').write_text(text)
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--base',type=Path,required=True);p.add_argument('--outfit',type=Path,required=True);a=p.parse_args()
 assert sha(a.base.read_bytes())==BASE_HASH and sha(a.outfit.read_bytes())==OUTFIT_HASH,'Unexpected source, inspect rights and intake before preparing'
 out=ROOT/'Assets/NeonSyndicate/Art/CandidatesH3';models=out/'Models';textures=out/'Textures';models.mkdir(parents=True,exist_ok=True);textures.mkdir(exist_ok=True);rows=[]
 z=zipfile.ZipFile(a.base);o=zipfile.ZipFile(a.outfit)
 def get(archive,suffix):
  matches=[n for n in archive.namelist() if n.endswith('/'+suffix)];assert len(matches)==1,(suffix,matches)
  b=archive.read(matches[0]);return b,matches[0]
 sources=[(z,'Base Characters/Unity/Superhero_Male_FullBody.fbx','MaleBase.fbx'),(o,'Exports/FBX (Unity)/Outfits/Male_Ranger.fbx','TravelClothing.fbx'),(z,'Hairstyles/Rigged to Head Bone/FBX (Unity)/Hair_Beard.fbx','Beard.fbx'),(z,'Hairstyles/Rigged to Head Bone/FBX (Unity)/Hair_Buzzed.fbx','ShortHair.fbx'),(z,'Hairstyles/Rigged to Head Bone/FBX (Unity)/Eyebrows_Regular.fbx','Brows.fbx')]
 for archive,suffix,dest in sources:
  b,name=get(archive,suffix);path=models/dest;path.write_bytes(b)
  template=(ROOT/'Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx.meta').read_text();template=re.sub('guid: [a-f0-9]+','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,path.relative_to(ROOT).as_posix()).hex,template)
  template=template.replace('importAnimation: 1','importAnimation: 0').replace('Original diagnostic, never a production hero','Received CC0 art candidate, not owner-approved Malik').replace('    preserveHierarchy: 1','    preserveHierarchy: 1\n    skinWeightsMode: 2\n    maxBonesPerVertex: 4\n    minBoneWeight: 0.001')
  Path(str(path)+'.meta').write_text(template);rows.append({'path':path.relative_to(ROOT).as_posix(),'source_entry':name,'source_sha256':sha(b),'sha256':sha(b),'bytes':len(b),'derivation':'Unchanged source FBX bytes'})
 images={}
 specs=[(z,'Base Characters/Textures/T_Superhero_Male_Dark.png','Skin_Albedo'),(z,'Base Characters/Textures/Normals Unity - Godot/T_Superhero_Male_Normal.png','Skin_Normal'),(z,'Base Characters/Textures/T_Superhero_Male_Roughness.png','Skin_Rough'),(z,'Base Characters/Textures/T_Eye_Brown.png','Eye_Albedo'),(z,'Hairstyles/Textures/T_Hair_1_BaseColor.png','Hair_Albedo'),(z,'Hairstyles/Textures/Normals Unity - Godot/T_Hair_1_Normal.png','Hair_Normal'),(o,'Textures/Ranger/T_Ranger_3_BaseColor.png','Cloth_Albedo'),(o,'Textures/Ranger/T_Ranger_Normal.png','Cloth_Normal'),(o,'Textures/Ranger/T_Ranger_ORM.png','Cloth_ORM'),(o,'Textures/Base/T_Regular_Male_Dark_BaseColor.png','Hands_Albedo'),(o,'Textures/Base/T_Regular_Male_Normal.png','Hands_Normal'),(o,'Textures/Base/T_Regular_Male_Roughness.png','Hands_Rough')]
 for archive,suffix,key in specs:
  b,name=get(archive,suffix);im=Image.open(io.BytesIO(b)).convert('RGBA' if key=='Hair_Albedo' else 'RGB');size=im.size
  # Technical preview mip preparation. Source SHA is retained; no concept image or invented normal map.
  if max(im.size)>1024:im.thumbnail((1024,1024),Image.Resampling.LANCZOS)
  images[key]=im
  if key.endswith(('_Rough','_ORM')):continue
  normal=key.endswith('_Normal');path=textures/(key+('.png' if normal or key=='Hair_Albedo' else '.jpg'))
  if normal or key=='Hair_Albedo':im.save(path)
  else:im.save(path,quality=92,subsampling=0)
  rows.append({'path':path.relative_to(ROOT).as_posix(),'source_entry':name,'source_sha256':sha(b),'sha256':sha(path.read_bytes()),'bytes':path.stat().st_size,'source_dimensions':size,'dimensions':im.size,'derivation':'Bounded 1024px preview mip from actual licensed source, not a newly authored texture'})
 for role in ['Skin','Hands','Cloth']:
  if role=='Cloth':ao,rough,metal=images['Cloth_ORM'].split()
  else:
   rough=images[role+'_Rough'].convert('L');ao=Image.new('L',rough.size,255);metal=Image.new('L',rough.size,0)
  mask=Image.merge('RGBA',(metal,ao,Image.new('L',rough.size,0),ImageOps.invert(rough)));path=textures/(role+'_Mask.png');mask.save(path)
  rows.append({'path':path.relative_to(ROOT).as_posix(),'sha256':sha(path.read_bytes()),'bytes':path.stat().st_size,'dimensions':mask.size,'derivation':'RGBA metallic, occlusion, unused, smoothness. Smoothness = 255 - source roughness.'})
 for path in textures.iterdir():
  if path.suffix not in ['.png','.jpg']:continue
  kind='Normal' if '_Normal'in path.stem else 'Mask' if '_Mask'in path.stem else 'Albedo'
  template=(ROOT/'Assets/NeonSyndicate/Resources/AscensionH2/Textures'/('masonry_'+kind+'.png.meta')).read_text();template=re.sub('guid: [a-f0-9]+','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,path.relative_to(ROOT).as_posix()).hex,template).replace('maxTextureSize: 256','maxTextureSize: 1024').replace('maxTextureSize: 512','maxTextureSize: 1024');Path(str(path)+'.meta').write_text(template)
 for archive,dest in [(z,'BASE_LICENSE.txt'),(o,'CLOTHING_LICENSE.txt')]:
  name=next(n for n in archive.namelist() if n.endswith('/License_Standard.txt'));path=out/dest;path.write_text('\n'.join(x.rstrip()for x in archive.read(name).decode().splitlines())+'\n');meta(path,'TextScriptImporter')
 manifest={'id':'H3-RECEIVED-HUMANOID-01','state':'RECEIVED CANDIDATE, NOT APPROVED MALIK','creator':'Quaternius','license':'CC0-1.0','source_urls':['https://quaternius.com/packs/universalbasecharacters.html','https://quaternius.com/packs/modularcharacteroutfitsfantasy.html'],'archive_sha256':[BASE_HASH,OUTFIT_HASH],'paid_content_received':False,'owner_appearance_approved':False,'registry_enabled':False,'native_import':'NOT RUN','native_deformation':'NOT RUN','canonical_clothing':'ASSET REQUIRED: final long charcoal coat and crystal armor identity, artist/native review','authored_clips':'ASSET REQUIRED: no clips in received FBXs','facial_blendshapes':'NOT VERIFIED','files':rows}
 path=out/'candidate.json';path.write_text(json.dumps(manifest,indent=2)+'\n');meta(path,'TextScriptImporter')
 for folder in [out,models,textures]:meta(folder)
 print(json.dumps({'files':len(rows),'bytes':sum(r['bytes']for r in rows),'state':manifest['state']}))
if __name__=='__main__':main()
