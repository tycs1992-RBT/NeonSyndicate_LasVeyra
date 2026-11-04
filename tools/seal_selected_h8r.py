#!/usr/bin/env python3
"""Record real export hashes and stable new metadata. Never writes a native or appearance acceptance."""
import hashlib,json,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1];art=root/'Assets/NeonSyndicate/Art/ProductionH8R';src=root/'ArtSource/AscensionH8R'
r=json.loads((art/'candidate.json').read_text());r['creator']='Owner selected CC0 human and skin; original project fitted wardrobe and selected-surface groom; supplied CC0 defense motion by Quaternius';r['files']=[{'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}for folder in ['Models','Textures','Animations']for p in sorted((art/folder).glob('*'))if p.suffix in ['.fbx','.png','.json']];(art/'candidate.json').write_text(json.dumps(r,indent=2)+'\n')
for p in (root/'Assets/NeonSyndicate').rglob('*'):
 if p.suffix=='.meta' or ('ProductionH8R'not in str(p)and 'WorldFoundationH8R'not in str(p)and p.name not in ['MalikReviewH8R.cs','ProductionMalikH6.H8R.cs','MalikReviewH8RTests.cs','SelectedMotionH8RTests.cs','WorldArtReviewH8R.cs']):continue
 meta=Path(str(p)+'.meta')
 if not meta.exists():
  guid=uuid.uuid5(uuid.NAMESPACE_URL,'neonsyndicate/h8r/'+p.relative_to(root).as_posix()).hex;meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'if p.is_dir()else''))
r=json.loads((src/'guard-retarget.json').read_text());r['final_art_source_sha256']=hashlib.sha256((src/'MalikVeyrH8R.blend').read_bytes()).hexdigest();(src/'guard-retarget.json').write_text(json.dumps(r,indent=2)+'\n')
print('Sealed actual files',len(json.loads((art/'candidate.json').read_text())['files']))
