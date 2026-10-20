#!/usr/bin/env python3
"""04OH owner, source-baseline and diagnostic-asset preservation. NOT native acceptance."""
import argparse,hashlib,json,subprocess
from pathlib import Path
BASE='721c1238a070e8dea08118314511c17023528feb'
ROOT=Path(__file__).resolve().parents[1]
OWNERS=['Scripts/Runtime/Characters/ImportedModelRegistry04OD.cs','Scripts/Runtime/Characters/ImportedVisual04OD.cs','Scripts/Runtime/Core/Game.cs','Scripts/Runtime/QA/CinematicAcceptance04OF.cs','Scripts/Editor/ImportedVisualWizard04OF.cs','Scripts/Editor/QA/CinematicAcceptanceWindow04OF.cs']
# H1 authorization adds an isolated topology, procedural skins and presentation in the original owners.
OWNERS += ['Scripts/Runtime/'+n for n in [
 'Characters/BodyBuilder.cs','Characters/HumanoidPose.cs','Characters/HumanoidPose.Cinematic04OF.cs','Characters/PlayerController.Travel.cs',
 'Creatures/CreatureBuilder.cs','Creatures/CreaturePose.Performance04L.cs','Creatures/CreaturePose.Cinematic04OF.cs','Creatures/FrontierPerformance04OG.cs',
 'Creatures/RegionalBossPresentation04OF.cs','Creatures/FracturedWildsCombat04OC.cs','Combat/Fx.cs','Core/WorldIdentity04OB.cs',
 'World/CityLayout.Rebirth04O.cs','World/CityLayout.Frontier04OG.cs','Gameplay/SaveSystem.cs','Gameplay/Quests/QuestLogic.cs',
 'Gameplay/Bonds/BondSystem.Resonance04N.cs','UI/MenuSystem.cs']]
OWNERS += ['Tests/EditMode/'+n for n in ['GothicRealm04OBTests.cs','FracturedWilds04OCTests.cs','CrystalRealms04ODTests.cs']]

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(name,condition):checks.append({'name':name,'passed':bool(condition)})
 def text(p):return(ROOT/'Assets/NeonSyndicate'/p).read_text()
 changed=subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets','Packages','ProjectSettings'],cwd=ROOT,text=True).splitlines()
 old_changes=[]
 for name in changed:
  if subprocess.run(['git','cat-file','-e',BASE+':'+name],cwd=ROOT,capture_output=True).returncode==0:old_changes.append(name)
 check('Existing changes restricted to explicit H0 import and authorized H1 presentation / isolated-preview owners',set(old_changes)<=set('Assets/NeonSyndicate/'+n for n in OWNERS))
 check('Renderer and package assignments remain identical to baseline',all(subprocess.check_output(['git','show',BASE+':'+p],cwd=ROOT)==(ROOT/p).read_bytes()for p in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']))
 live=text('Scripts/Runtime/Characters/ImportedVisual04OD.cs');pre=live.index('foreach(Component component in model.Prefab');clone=live.index('_visual=Instantiate(')
 check('Prefab components are inspected before any Awake/OnEnable can run',pre<clone and 'ImportedArt04OH.VisualComponent(component.GetType())'in live[pre:clone] and 'animation.runtimeAnimatorController!=null'in live[pre:clone])
 check('Existing procedural pose is the only implemented imported animation authority','authority!=ImportedAnimationAuthority04OH.ExistingPose'in text('Scripts/Runtime/Characters/ImportedArt04OH.cs') and 'animator.enabled=false;animator.applyRootMotion=false;'in live)
 check('Provenance and fingerprint policy are integrated with original registry and wizard','ImportedArt04OH.CanActivate(model.ArtProof'in text('Scripts/Runtime/Characters/ImportedModelRegistry04OD.cs') and 'ImportedArtProof04OH.Stamp(proof,_asset,prefab)'in text('Scripts/Editor/ImportedVisualWizard04OF.cs'))
 check('Diagnostic is not a runtime actor grant','if(proof.Diagnostic)'in text('Scripts/Runtime/Characters/ImportedArt04OH.cs') and 'Enabled=false'in text('Scripts/Editor/ImportedVisualWizard04OF.cs'))
 check('No runtime registry or unrelated scene was introduced',not(ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())
 fixture=ROOT/'Assets/NeonSyndicate/Art/ImportCalibration04OH/Calibration47.fbx';manifest=json.loads(fixture.with_suffix('.fixture.json').read_text())
 check('Actual skinned fixture bytes match their manifest',manifest['export_sha256']==hashlib.sha256(fixture.read_bytes()).hexdigest() and len(manifest['bones'])==47 and manifest['production_candidate']is False and manifest['registry_enabled']is False)
 check('New runtime policy resets through existing hub','ImportedArt04OH.ResetStatics();'in text('Scripts/Runtime/Core/Game.cs'))
 check('Original evidence tool retains callbacks and adds hardware/proof context','SaveResultToFile(result'in text('Scripts/Editor/QA/AcceptanceTestRun04OF.cs') and 'VisualDependencyHash'in text('Scripts/Editor/QA/CinematicAcceptanceWindow04OF.cs') and 'SystemInfo.processorType'in text('Scripts/Editor/QA/CinematicAcceptanceWindow04OF.cs'))
 # H1 keeps pre-existing failed/excluded test semantics; only the now-valid revision sentinel changes.
 for filename in ['GothicRealm04OBTests.cs','FracturedWilds04OCTests.cs','CrystalRealms04ODTests.cs']:
  path='Assets/NeonSyndicate/Tests/EditMode/'+filename
  original=subprocess.check_output(['git','show','c5778fa0e8bcd78e49c3a9b841b36524d301e1a8:'+path],cwd=ROOT,text=True)
  expected=original.replace('SlotFileName04OB(0, 7)','SlotFileName04OB(0, 99)').replace('SlotFileName04OB(0,7)','SlotFileName04OB(0,99)').replace('Known(7)','Known(99)')
  check('Only obsolete unknown-revision sentinel changes in '+filename,(ROOT/path).read_text()==expected)
 check('H1 world has an explicit isolated bank and no default boot change','lasveyra_ascension04oh_save_' in text('Scripts/Runtime/Gameplay/SaveSystem.cs') and 'Visual Ascension H1' in text('Scripts/Runtime/UI/MenuSystem.cs') and subprocess.check_output(['git','show',BASE+':Assets/NeonSyndicate/Scripts/Runtime/Core/CityRebirthBoot04O.cs'],cwd=ROOT)==(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/Core/CityRebirthBoot04O.cs').read_bytes())
 check('H1 effects are bounded and own no damage, input, camera, time or displacement',all(t in text('Scripts/Runtime/Combat/Fx.VisualSlice04OH.cs') for t in ['new GameObject[8]','!GameTime.Paused','ReducedEffects','Layers.IgnoreRaycast']) and all(t not in text('Scripts/Runtime/Combat/Fx.VisualSlice04OH.cs') for t in ['TakeDamage(','Spend(','Teleport(','SetCinematic(','timeScale','AddComponent<Collider']))
 report={'scope':__doc__,'base':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'native_unity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
