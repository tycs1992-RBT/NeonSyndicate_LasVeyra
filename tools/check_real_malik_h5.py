#!/usr/bin/env python3
"""H5 actual received/derived assets and protected owners. No native or production-art approval is implied."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE='ab5887779a9f3e6ed0775721d56237e247d7b81b'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,value):checks.append({'name':name,'passed':bool(value)})
 def raw(rel):return(ROOT/rel).read_bytes()
 def text(rel):return raw(rel).decode()
 def old(rel):return subprocess.check_output(['git','show',BASE+':'+rel],cwd=ROOT)
 def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
 model=ROOT/'Assets/NeonSyndicate/Art/CandidatesH5';m=json.loads((model/'skin-lineage.json').read_text())
 check('Actual CC0 derivative, disabled and unapproved',m['creator']=='Quaternius' and m['license']=='CC0-1.0' and m['registry_enabled']is False and m['owner_appearance_approved']is False and m['native_import']=='NOT RUN' and len(m['models'])==5)
 for r in m['models']:
  check('Original unchanged / '+r['source'],sha(ROOT/r['source'])==r['original_sha256'])
  check('Actual binary derivative fingerprint / '+r['output'],sha(ROOT/r['output'])==r['derived_sha256'] and (ROOT/r['output']).read_bytes().startswith(b'Kaydara FBX Binary'))
  check('No pruning or altered bind/geometry properties / '+r['output'],r['non_skin_properties_unchanged'] and all(q['maximum_discarded_normalized_mass']==0 and q['pruned_influences']==0 for q in r['meshes']))
 check('Four-weight failure is retained, not accepted','FAILED' in m['pruning_policy'] and '19.503309' in m['pruning_policy'])
 material=json.loads((ROOT/'Assets/NeonSyndicate/Resources/AscensionH5/material-source.json').read_text())
 check('Received metal has primary source and actual CC0 attribution',material['creator']=='Rob Tuytel' and material['license']=='CC0-1.0' and material['source']=='https://polyhaven.com/a/rusty_metal_02')
 for row in material['files']:check('Actual metal map / '+row['path'],sha(ROOT/row['path'])==row['sha256'])
 for rel in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','Assets/NeonSyndicate/Scripts/Runtime/Core/CityRebirthBoot04O.cs']:
  check('Default project/renderer owner / '+rel,raw(rel)==old(rel))
 for rel in ['Characters/BodyBuilder.cs','Characters/BodyBuilder.GothicArtH3.cs','Characters/HumanoidRig.cs','Characters/HumanoidAnimator.cs','Characters/HumanoidPose.cs','Characters/PlayerController.cs','Combat/WeaponController.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/ForgeTreatments04M.cs','Gameplay/Quests/QuestRunner.cs','Gameplay/Quests/QuestValidator.cs','Characters/ImportedArt04OH.cs','Characters/ImportedBindH3.cs']:
  path='Assets/NeonSyndicate/Scripts/Runtime/'+rel;check('Original actor/gameplay authority / '+rel,raw(path)==old(path))
 check('All prior tests and exclusions preserved',not subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets/NeonSyndicate/Tests/EditMode'],cwd=ROOT).decode().strip())
 # New tests are untracked or newly added relative to BASE and must not be mistaken for changed original tests.
 checks[-1]['passed']=all(subprocess.run(['git','cat-file','-e',BASE+':'+path],cwd=ROOT,capture_output=True).returncode!=0 for path in subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets/NeonSyndicate/Tests/EditMode'],cwd=ROOT).decode().splitlines())
 runtime=text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.cs');wizard=text('Assets/NeonSyndicate/Scripts/Editor/ImportedVisualWizard04OF.cs');prep=text('Assets/NeonSyndicate/Scripts/Editor/ReceivedHeroCandidateH5.cs')
 check('Complete weight inspection, no global quality mutation','SkinWeightsH5.Referenced(skin.sharedMesh,skin.bones.Length)'in runtime and 'QualitySettings.skinWeights!=SkinWeights.Unlimited'in runtime and 'QualitySettings.skinWeights='not in runtime)
 check('Default registry option is opt-in, canonical approval remains explicit','public bool PreserveInfluencesH5;'in text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedModelRegistry04OD.cs') and 'Enabled=false,Prefab=prefab'in wizard and 'OwnerApproved=false'in wizard)
 check('Native prepared reference is explicit, validates both lineages and retains all weights',all(x in prep for x in ['if(!CanPrepare())','original.files.Length!=17','manifest.models.Length!=5','FileHash(file.output)','maxBonesPerVertex=8','minBoneWeight=0','SkinWeightsH5.Referenced','OpenDerivedH5']))
 check('No actual prepared prefab or activated registry shipped',not(model/'Prepared').exists() and not(ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())
 game=text('Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs');check('New pure statics reset through original hub','SkinWeightsH5.ResetStatics();'in game and 'GothicCivicH5.ResetStatics();'in game)
 check('Physical new profile is isolated and explicitly selectable','GothicCivicH5 = 10'in text('Assets/NeonSyndicate/Scripts/Runtime/World/CityLayout.Rebirth04O.cs') and 'lasveyra_civic_h5_save_'in text('Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs') and 'Gothic Civic H5'in text('Assets/NeonSyndicate/Scripts/Runtime/UI/MenuSystem.cs'))
 check('Actual two civic shells use the original venue owner','id=="archive" || id=="office"'in text('Assets/NeonSyndicate/Scripts/Runtime/World/GothicCivicH5.cs') and 'if(civic)GothicCivicH5.Structure(structure,binding,mineral)'in text('Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs'))
 report={'scope':__doc__,'baseline':BASE,'passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'raw_h3_normalization':'FAILED, immutable earlier evidence preserved','derived_normalization':'PASSED independently, full original influences retained','four_weight_deformation':'FAILED on body/clothing, not chosen','malik':'FALLBACK / NOT FINAL','native_import':'NOT RUN','native_gameplay':'NOT RUN','owner_art_approval':'NOT GIVEN','production_acceptance':'NOT RUN'}
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[x['name']for x in checks if not x['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
