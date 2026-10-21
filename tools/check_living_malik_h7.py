#!/usr/bin/env python3
"""H7 actual asset, literal owner-hook and approval protection audit. NOT native acceptance."""
import argparse,hashlib,json,subprocess
from h8_owner_hooks import restore_owner_h8
from pathlib import Path
from h7_owner_hooks import restore_h6_visual
ROOT=Path(__file__).resolve().parents[1];BASE='b63aa570ad0bc1f237c134398d7124fa5a390976'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def text(path):return(ROOT/path).read_text()
 def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
 art=ROOT/'Assets/NeonSyndicate/Art/ProductionH7';receipt=json.loads((art/'candidate.json').read_text());stats=json.loads((art/'Models/authoring-stats.json').read_text());actions=json.loads((ROOT/'ArtSource/AscensionH7/animation-source.json').read_text())
 check('Fixed H7 identity, lawful rights, disabled and unapproved',receipt['assetId']=='MALIK-H7-REVIEW-01' and receipt['license']=='CC0-1.0' and receipt['commercialUse']and receipt['publicSourceApproved']and receipt['preserveInfluences']and not receipt['enabled']and not receipt['ownerApproved'])
 for row in receipt['files']:
  path=ROOT/row['path'];check('Real asset fingerprint / '+row['path'],path.is_file()and path.stat().st_size==row['bytes']and sha(path)==row['sha256'])
 for row in stats['files']:
  path=ROOT/row['path'];check('Editable/export lineage / '+row['path'],path.is_file()and path.stat().st_size==row['bytes']and sha(path)==row['sha256'])
 source=ROOT/receipt['authoringSource'];check('Real artist source and matching real-action proof',source.read_bytes().startswith(b'\x28\xb5\x2f\xfd')and actions['source_sha256']==sha(source))
 check('Three decreasing real LODs and 53 original bones',stats['bone_count']==53 and stats['runtime_skins']==39 and all(sum(x['triangles']for x in stats['lods']if x['lod']=='LOD'+str(i))>sum(x['triangles']for x in stats['lods']if x['lod']=='LOD'+str(i+1))for i in [0,1]))
 check('Five editable keyframed actions, no event or root authority',len(actions['actions'])==5 and all(x['curves']>100 and x['keyframes']>1000 and x['root_motion']is False and x['events']==[]for x in actions['actions']))
 for prefix in ['ArtSource/AscensionH6','Assets/NeonSyndicate/Art/ProductionH6','Docs/Ascension04OH/ArtReviewH6']:
  paths=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE,'--',prefix],cwd=ROOT,text=True).splitlines()
  check('H6 source, art and proof unchanged / '+prefix,all(restore_owner_h8(path,(ROOT/path).read_bytes())==subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT)for path in paths))
 protected=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']
 protected+=['Assets/NeonSyndicate/Scripts/Runtime/'+x for x in ['Core/Game.cs','Characters/PlayerController.cs','Characters/PlayerCombat.cs','Characters/HumanoidAnimator.cs','Characters/HumanoidRig.cs','Characters/HumanoidPose.cs','Characters/ImportedModelRegistry04OD.cs','Characters/ImportedArt04OH.cs','Characters/ImportedBindH3.cs','Combat/WeaponController.cs','Combat/WeaponModels.Regional04OE.cs','Gameplay/SaveSystem.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/ForgeTreatments04M.cs','World/CityLayout.Rebirth04O.cs','Core/CityRebirthBoot04O.cs','UI/MenuSystem.cs']]
 for path in protected:check('Frozen gameplay, save or default owner / '+path,restore_owner_h8(path,(ROOT/path).read_bytes())==subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT))
 path='Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.cs';live=text(path)
 check('Only three exact optional visual hooks; all H6 importer validation intact',restore_h6_visual(live)==subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT,text=True))
 motion=text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.MotionH7.cs');review=text('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.ReviewH6.cs');editor=text('Assets/NeonSyndicate/Scripts/Editor/ProductionMalikH6.cs')
 check('One mapped world rotation write, no second transform writer',live.count('_target[i].rotation=')==1 and '.position='not in motion and '.rotation='not in motion and '.localRotation='not in motion)
 check('Presentation helper is Editor-only and clears on existing restoration',motion.startswith('#if UNITY_EDITOR')and 'ClearMotionH7();'in review and '_motionH7=null;'in motion)
 check('Actual motion uses original phase; keeps original hands, feet and hit authority',all(s in motion for s in ['a.AttackT,a.AttackWindup,a.AttackActive','semantic<RigLayout.Chest || semantic>RigLayout.Head','a.AttackAnim!=MeleeAnim.SlashRight','a.HoldBlade','a.AttackMirror'])and all(s not in motion for s in ['TakeDamage(','Spend(','Instantiate(','AnimatorController','AnimationEvent','Input.','Time.timeScale','SaveSystem.']))
 check('Original travel, aim, gesture, interruption, Pause and guard exclude accents',all(s in motion for s in ['a.Travel!=TravelPose.None','a.Aiming','a.HeadLook','a.DodgeT>=0','a.StaggerT>=0','a.Guard>.01f','GameTime.Paused','a.SynergyCast04N','a.Frozen','a.Cover']))
 check('H7 alias uses existing review and cleans on changed imports',all(s in editor for s in ['OpenH7','SelectVersion','EditorApplication.projectChanged+=Stop','ReviewMotionH7','_authoredAccents=false','OwnerApproved=false','Enabled=false']))
 check('No enabled registry, prepared gameplay copy or default change',not(art/'Prepared').exists()and not(ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())
 for folder in [ROOT/'Docs/Ascension04OH/ArtReviewH7',ROOT/'Docs/Ascension04OH/ArtReviewH7/OathCut',ROOT/'Docs/Ascension04OH/ArtReviewH7/Equipped']:
  manifest=json.loads((folder/'render-manifest.json').read_text());check('Actual DCC views tied to current source / '+folder.name,manifest['source_sha256']==sha(source)and manifest['native_unity']=='NOT RUN'and manifest['owner_approval']=='NOT GIVEN'and all((folder/x['file']).exists()for x in manifest['views']))
 report={'scope':__doc__,'baseline':BASE,'passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN','owner_appearance':'NOT GIVEN','production_character':'NOT ACCEPTED'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[x['name']for x in checks if not x['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
