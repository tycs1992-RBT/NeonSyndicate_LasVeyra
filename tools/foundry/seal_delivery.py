"""Hash the two demonstrations and create only missing deterministic Unity metadata. Never rewrite existing GUIDs."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A';SRC=ROOT/'ArtSource/FoundryWR02A'
def identity(path):return {'path':path.relative_to(ROOT).as_posix(),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
def guid(path):return hashlib.md5(('CrystalManorSaga/FoundryWR02A/'+path.relative_to(ROOT).as_posix()).encode()).hexdigest()
def main():
 modules=[];sources=[]
 for asset in ['SanctuaryArchive','CrystalBasiliskCandidate']:
  processed=json.loads((ART/asset/'processed.json').read_text());brief=json.loads((SRC/(asset+'.brief.json')).read_text())
  if processed['processor_sha256']!=identity(ROOT/'tools/foundry/process_asset.py')['sha256']:raise ValueError('Processing recipe changed; reprocess actual outputs before sealing.')
  if processed['input_sha256']!=identity(ROOT/processed['input'])['sha256']:raise ValueError('Original source hash changed.')
  for row in processed['outputs']:
   if row['sha256']!=identity(ROOT/row['path'])['sha256']or row.get('independentInspection',{}).get('passed')is not True:raise ValueError('No genuine successful independent parse for current payload.')
   modules.append(dict(name='WRArchiveCrown'if asset=='SanctuaryArchive'else asset,lod=row['lod'],**identity(ROOT/row['path']),triangles=row['triangles'],candidateOnly=asset!='SanctuaryArchive'))
  for p in [SRC/(asset+'.raw.glb'),SRC/(asset+'.author.blend'),SRC/(asset+'.processed.blend'),SRC/(asset+'.brief.json')]:sources.append(identity(p))
 textures=[identity(p)for p in sorted((ROOT/'Assets/NeonSyndicate/Art/HeartFR01').glob('*.png'))]+[identity(p)for p in sorted(ART.glob('*.png'))]
 if len(textures)!=21:raise ValueError('Unexpected shared PBR texture inventory.')
 manifest={'schema_version':1,'license':'CC0-1.0','method':'Original local AI-assisted Blender production. No text/image-to-3D provider task was submitted.','modules':modules,'textures':textures,'sources':sources,'originalCreatureContract':identity(SRC/'CrystalBasilisk.original-contract.json'),'creditsSpent':0,'providerConnection':'NOT ESTABLISHED','nativeUnity':'NOT RUN','creaturePolicy':'CANDIDATE ONLY, original existing species remains until actual native motion and gameplay correspondence acceptance.','architecturePolicy':'Original Archive crown stable key and exact bounds, existing Heart transaction. No new physics or service owner.'}
 (ART/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 folder_template=(ROOT/'Assets/NeonSyndicate/Art/HeartWR02.meta').read_text()
 model_template=(ROOT/'Assets/NeonSyndicate/Art/HeartWR02/CanopySpreading_LOD0.fbx.meta').read_text()
 script_template=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.SurfaceWR02.cs.meta').read_text()
 for path in [ART]+sorted(ART.rglob('*'))+[ROOT/'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs',ROOT/'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.FoundryWR02A.cs',ROOT/'Assets/NeonSyndicate/Tests/EditMode/FoundryWR02ATests.cs']:
  if path.suffix=='.meta':continue
  meta=path.with_name(path.name+'.meta')
  if meta.exists():continue
  if path.is_dir():text=folder_template
  elif path.suffix=='.fbx':
   text=model_template
   if path.name.startswith('CrystalBasiliskCandidate'):
    text=re.sub(r'(?m)^  animationType: .*$', '  animationType: 2',text);text=re.sub(r'(?m)^    importAnimation: .*$', '    importAnimation: 1',text);text=re.sub(r'(?m)^  importAnimation: .*$', '  importAnimation: 1',text)
  elif path.suffix=='.png':
   channel='Normal'if'_Normal'in path.stem else'MetallicSmoothness'if'_MetallicSmoothness'in path.stem else'BaseColor';text=(ROOT/('Assets/NeonSyndicate/Art/HeartWR02/CausewayStone_'+channel+'.png.meta')).read_text()
  elif path.suffix=='.cs':text=script_template
  else:text='fileFormatVersion: 2\nguid: PLACEHOLDER\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
  text=re.sub(r'(?m)^guid: .*$', 'guid: '+guid(path),text);meta.write_text('\n'.join(line.rstrip()for line in text.splitlines())+'\n')
 print('Sealed',len(modules),'FBX LODs,',len(textures),'PBR maps,',len(sources),'editable/input/brief source records.')
if __name__=='__main__':main()
