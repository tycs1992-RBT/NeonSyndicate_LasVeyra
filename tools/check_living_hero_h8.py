#!/usr/bin/env python3
"""Exact H8 original-owner, source action, review and no-default checks. Not native acceptance."""
import argparse,hashlib,json,subprocess
from pathlib import Path
from h8_owner_hooks import restore_owner_h8
ROOT=Path(__file__).resolve().parents[1];BASE='556ac54f8b15993fc208f2932166f5059315310d'
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);a=p.parse_args();checks=[]
 def check(n,v):checks.append({'name':n,'passed':bool(v)})
 def read(p):return(ROOT/p).read_text()
 def old(p):return subprocess.check_output(['git','show',BASE+':'+p],cwd=ROOT)
 for name in ['HumanoidAnimator.cs','HumanoidPose.cs']:
  path='Assets/NeonSyndicate/Scripts/Runtime/Characters/'+name;check('Only exact original pose hook / '+name,restore_owner_h8(path,(ROOT/path).read_bytes())==old(path))
 for path in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']+[ 'Assets/NeonSyndicate/Scripts/Runtime/'+p for p in ['Core/Game.cs','Core/CityRebirthBoot04O.cs','Characters/PlayerController.cs','Characters/PlayerCombat.cs','Characters/PlayerCamera.cs','Characters/HumanoidRig.cs','Characters/ImportedVisual04OD.cs','Characters/ImportedBindH3.cs','Gameplay/SaveSystem.cs','Gameplay/StoryMissions.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Gameplay/RegionalArsenal04OE.cs','Combat/WeaponController.cs','UI/MenuSystem.cs']]:check('H7 gameplay, banks, defaults byte preservation / '+path,(ROOT/path).read_bytes()==old(path))
 for prefix in ['ArtSource/AscensionH7/','Assets/NeonSyndicate/Art/ProductionH7/']:
  paths=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE,'--',prefix],cwd=ROOT,text=True).splitlines();check('All original H7 sources/assets preserved / '+prefix,all((ROOT/p).read_bytes()==old(p) if not p.endswith(('.fbx','.blend','.png','.jpg'))else subprocess.check_output(['git','hash-object','--path='+p,p],cwd=ROOT,text=True).strip()==subprocess.check_output(['git','rev-parse',BASE+':'+p],cwd=ROOT,text=True).strip()for p in paths))
 motion=read('Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidPose.MalikH8.cs');sampler=read('Assets/NeonSyndicate/Scripts/Runtime/Characters/MalikMotionH8.cs');anim=read('Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidAnimator.MalikH8.cs');review=read('Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.ReviewH6.cs')
 check('No authored motor, damage, resource, reward, camera, input or time writer',all(t not in motion+sampler+anim for t in ['TakeDamage(','Spend(','SetCinematic(','timeScale=','Input.','SaveSystem.','AnimatorController','AnimationEvent','transform.position=']))
 check('Complete body goes into original feet, smoothing and weapon IK',read('Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidPose.cs').index('AuthoredBodyH8(ref p,ref b,dt);')<read('Assets/NeonSyndicate/Scripts/Runtime/Characters/HumanoidPose.cs').index('            Feet(ref p, ref b, dt, rootRot, invRoot);'))
 check('Rollback clears real pose data and restores visibility cadence','_owner.Anim.EndFullBodyReviewH8();'in review and 'AlwaysAnimate=_priorAlwaysAnimateH8'in anim and 'Pose.SetReviewMotionH8(null)'in anim)
 data=json.loads(read('Assets/NeonSyndicate/Art/ProductionH8/Animations/MalikFullBodyH8.json'));source=ROOT/'ArtSource/AscensionH8/MalikVeyrH8.blend'
 if not source.exists():source=ROOT/'ArtSource/AscensionH7/MalikVeyrH7.blend'
 check('Actual source action samples have exact lineage',data['sourceSha256']==hashlib.sha256(source.read_bytes()).hexdigest()and len(data['bind'])==17 and len(data['clips'])>=5 and not data['rootMotion']and not data['events'])
 check('No enabled registry or prepared model shipped',not(ROOT/'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists()and not(ROOT/'Assets/NeonSyndicate/Art/ProductionH7/Prepared').exists())
 report={'baseline':BASE,'scope':__doc__,'passed':sum(x['passed']for x in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN','owner_approval':'NOT GIVEN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
