#!/usr/bin/env python3
"""H3 source/asset safety, not native art acceptance. Raw candidate weight normalization remains an explicit unresolved gate."""
import argparse,hashlib,json,re,struct,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE='f78536834074b82ae6e1d09f9852413381713259'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
 def text(rel):return (ROOT/rel).read_text()
 def original(rel):return subprocess.check_output(['git','show',BASE+':'+rel],cwd=ROOT)
 root=ROOT/'Assets/NeonSyndicate';runtime=root/'Scripts/Runtime'
 manifest=json.loads((root/'Art/CandidatesH3/candidate.json').read_text())
 check('Received candidate is real source, CC0 and explicitly unapproved/disabled',manifest['license']=='CC0-1.0' and manifest['creator']=='Quaternius' and manifest['owner_appearance_approved']is False and manifest['registry_enabled']is False and manifest['native_import']=='NOT RUN' and manifest['paid_content_received']is False)
 for row in manifest['files']:
  path=ROOT/row['path'];check('Received file fingerprint / '+path.name,path.is_file() and sha(path)==row['sha256'])
  if path.suffix=='.fbx':check('Actual binary FBX / '+path.name,path.read_bytes().startswith(b'Kaydara FBX Binary'))
 for name in ['BASE_LICENSE.txt','CLOTHING_LICENSE.txt']:check('Original CC0 declaration retained / '+name,'CC0 1.0 Universal' in (root/'Art/CandidatesH3'/name).read_text())
 check('Head hair alpha is preserved, not an opaque converted card',struct.unpack('>II',(root/'Art/CandidatesH3/Textures/Hair_Albedo.png').read_bytes()[16:24])==(1024,1024) and (root/'Art/CandidatesH3/Textures/Hair_Albedo.png').read_bytes()[25]==6)
 maps=json.loads((root/'Resources/AscensionH3/material_sources.json').read_text())
 for row in maps['files']:
  path=ROOT/row['path'];check('Received CC0 material fingerprint / '+path.name,sha(path)==row['sha256'])
 for source in maps['assets']:check('Actual provider CC0 source / '+source['role'],source['license']=='CC0-1.0' and source['native_import']=='NOT RUN' and source['source'].startswith('https://polyhaven.com/a/'))
 for folder in ['Art/CandidatesH3/Textures','Resources/AscensionH3/Textures']:
  for path in sorted((root/folder).iterdir()):
   if path.suffix not in ['.jpg','.png']:continue
   meta=Path(str(path)+'.meta').read_text();normal='_Normal'in path.stem;linear=normal or '_Mask'in path.stem
   check('Explicit color/normal import / '+folder+'/'+path.name,'sRGBTexture: '+('0'if linear else'1')in meta and 'textureType: '+('1'if normal else'0')in meta)
 for rel in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','Assets/NeonSyndicate/Scripts/Runtime/Core/CityRebirthBoot04O.cs']:
  check('Original default/project owner / '+rel,(ROOT/rel).read_bytes()==original(rel))
 for rel in ['Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/RegionalArmor04OE.cs','Gameplay/ForgeTreatments04M.cs','Gameplay/Quests/QuestRunner.cs','Gameplay/Quests/QuestValidator.cs','Characters/HumanoidRig.cs','Characters/PlayerController.cs','Combat/WeaponController.cs','Characters/ImportedArt04OH.cs']:
  path=runtime/rel
  if not path.exists():raise RuntimeError('Protected owner path missing: '+rel)
  check('Original gameplay authority / '+rel,path.read_bytes()==original(path.relative_to(ROOT).as_posix()))
 check('H2 kit and its metadata remain unchanged',not subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets/NeonSyndicate/Resources/AscensionH2'],cwd=ROOT).strip())
 check('No runtime registry is silently introduced',not(root/'Resources/ImportedModels04OD.asset').exists())
 editor=text('Assets/NeonSyndicate/Scripts/Editor/ReceivedHeroCandidateH3.cs');wizard=text('Assets/NeonSyndicate/Scripts/Editor/ImportedVisualWizard04OF.cs')
 check('Preparation is explicit EditMode, actual files, original shader, four-weight normalization and safe failure',all(s in editor for s in ['!EditorApplication.isPlayingOrWillChangePlaymode','manifest.files','FileHash(file.path)','ModelImporterSkinWeights.Custom','maxBonesPerVertex=4','ValidateSkin(renderer)','Mats.StandardShader','DestroyImmediate(copy)']))
 check('Canonical approval and activation remain withheld','OwnerApproved=false'in wizard and 'Enabled=false,Prefab=prefab'in wizard and 'ExistingPose'in text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedArt04OH.cs'))
 check('New topology uses actual original builder and isolated bank','BuildGothicHeartH3();'in text('Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs') and 'lasveyra_heart_h3_save_'in text('Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs') and 'Gothic Heart H3'in text('Assets/NeonSyndicate/Scripts/Runtime/UI/MenuSystem.cs'))
 check('New look and import statics use the original reset hub',all(s in text('Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs')for s in ['GothicHeartH3.ResetStatics();','ImportedBindH3.ResetStatics();']))
 quest=json.loads((root/'Resources/Quests/ascension_h3_hearth_witness.json').read_text())['Quests'][0];steps=[s for phase in quest['Phases']for s in phase['Steps']]
 check('Home reading and real regional commission are finite original-runner content',quest['Requires']=={'WorldRevisions':[9]} and quest['VenueGiver']=='forge' and sum(s.get('Count',0)for s in steps if s['Type']=='Kill')==3 and sum(s['Type']=='Interact'for s in steps)==3 and any(s.get('At')=='Realm:hearth-lens'for s in steps) and not any(s['Type']in ['GiveGun','Bond','Boss']for s in steps))
 for rel in ['Characters/BodyBuilder.GothicArtH3.cs','Creatures/CreatureBuilder.GothicArtH3.cs','Creatures/CompanionSculpt.GothicArtH3.cs']:
  s=(runtime/rel).read_text();check('Art adds no competing damage, motor, time or inventory / '+rel,all(term not in s for term in ['TakeDamage(','.Spend(','Teleport(','SetCinematic(','timeScale','AddComponent<Collider']))
 report={'scope':__doc__,'baseline':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'candidate_raw_normalization':'FAILED in strict source inspection, retained explicitly. Native four-weight import/normalization and appearance remain unverified. No activation.','native_import':'NOT RUN','native_play':'NOT RUN','visual_acceptance':'NOT RUN','performance':'NOT RUN','windows_player':'NOT RUN'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
