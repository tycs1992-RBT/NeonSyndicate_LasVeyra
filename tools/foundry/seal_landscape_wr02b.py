"""Seal only new WR02-B assets; inherited source, asset bytes and GUIDs remain untouched."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B';SRC=ROOT/'ArtSource/FoundryWR02A/EnvironmentWR02B'
def identity(p):return {'path':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def main():
 modules=[];sources=[];textures={};frames=[]
 for f in sorted(SRC.glob('*.brief.json')):
  brief=json.loads(f.read_text());asset=brief['assetId'];processed=json.loads((ART/asset/'processed.json').read_text())
  assert processed['processor_sha256']==identity(ROOT/'tools/foundry/process_asset.py')['sha256']
  assert processed['input_sha256']==identity(ROOT/brief['input'])['sha256']
  for row in processed['outputs']:
   assert row['sha256']==identity(ROOT/row['path'])['sha256'] and row['independentInspection']['passed']
   modules.append(dict(name=asset,lod=row['lod'],triangles=row['triangles'],candidateOnly=asset=='EscarpmentArch',**identity(ROOT/row['path'])))
  for surface in brief['materials'].values():
   for path in surface.values():textures[path]=identity(ROOT/path)
  for suffix in ['author.blend','processed.blend','raw.glb','brief.json','author-receipt.json']:sources.append(identity(SRC/(asset+'.'+suffix)))
  frames.append({'name':asset,'min':brief['expectedUnityBounds'][0],'max':brief['expectedUnityBounds'][1],'candidateOnly':asset=='EscarpmentArch'})
 assert len(modules)==21 and len(frames)==7 and len(textures)==15
 (ART/'manifest.json').write_text(json.dumps({'schema_version':1,'license':'CC0-1.0','method':'Original local Blender geometry, unchanged WR02-A processor. No provider tasks or paid generation.','modules':modules,'textures':list(textures.values()),'sources':sources,'frames':frames,'nativeUnity':'NOT RUN','providerConnection':'NOT ESTABLISHED','creditsSpent':0,'collisionPolicy':'Original authority retained. New physical escarpment is a disabled candidate only.'},indent=2)+'\n')
 (SRC/'LICENSE.md').write_text('Original WR02-B geometry is dedicated to the public domain under CC0-1.0. Authored locally with the checked-in Blender Python source. No external generated or purchased asset was used. Shared existing project PBR maps retain their existing provenance. No Tripo connection established or credits spent.\n')
 template=(ROOT/'Assets/NeonSyndicate/Art/HeartWR02/CanopySpreading_LOD0.fbx.meta').read_text();folder=(ROOT/'Assets/NeonSyndicate/Art/HeartWR02.meta').read_text();script=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.SurfaceWR02.cs.meta').read_text()
 paths=[ART]+sorted(ART.rglob('*'))+[p for p in (ROOT/'Assets').rglob('*.cs')if 'WR02B' in p.name]
 for p in paths:
  if p.suffix=='.meta':continue
  meta=Path(str(p)+'.meta')
  if meta.exists():continue
  text=folder if p.is_dir()else template if p.suffix=='.fbx'else script if p.suffix=='.cs'else'fileFormatVersion: 2\nguid: PLACEHOLDER\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
  guid=hashlib.md5(('CrystalManorSaga/WR02B/'+p.relative_to(ROOT).as_posix()).encode()).hexdigest();text=re.sub(r'(?m)^guid: .*$', 'guid: '+guid,text);meta.write_text('\n'.join(x.rstrip()for x in text.splitlines())+'\n')
 print('Sealed 7 authored modules, 21 LOD exports, 15 reused PBR maps. Native Unity NOT RUN.')
if __name__=='__main__':main()
