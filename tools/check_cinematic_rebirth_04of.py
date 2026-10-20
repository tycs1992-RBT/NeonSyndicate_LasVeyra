#!/usr/bin/env python3
"""04OF source integration guards. Never native import, gameplay or art acceptance."""
import argparse,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='9c8f32119a6ee9b7b94b5e3e879429981179507b'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();a.report.parent.mkdir(parents=True,exist_ok=True)
 raw=a.report.with_name('inherited-04oe-raw.json')
 run=subprocess.run([sys.executable,str(ROOT/'tools/check_crystal_unbound_04oe.py'),'--report',str(raw)],cwd=ROOT,capture_output=True,text=True)
 old=json.loads(raw.read_text());checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def read(path):return (ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/path).read_text()
 def unchanged(path):return subprocess.run(['git','diff','--quiet',BASE,'--',path],cwd=ROOT).returncode==0
 check('Only the documented Editor asmdef changes the predecessor asset guard',[c['name']for c in old['checks']if not c['passed']]==['No unlisted inherited owner changes'])
 asm='Assets/NeonSyndicate/Scripts/Editor/NeonSyndicate.Editor.asmdef'
 before=json.loads(subprocess.check_output(['git','show',BASE+':'+asm],cwd=ROOT));after=json.loads((ROOT/asm).read_text())
 before['references']+=['UnityEditor.TestRunner','UnityEngine.TestRunner']
 check('Editor dependency change is exactly the real Test Framework API',before==after)
 changes=subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets'],cwd=ROOT,text=True).splitlines()
 check('No existing non-source asset changed except explicit Editor references',all(path.endswith('.cs') or path==asm or subprocess.run(['git','cat-file','-e',BASE+':'+path],cwd=ROOT,capture_output=True).returncode!=0 for path in changes))
 check('All previous guards except the precise asmdef scope still pass',old['passed']==old['total']-1)
 preserved=['Gameplay/SaveSystem.cs','Core/CityRebirthBoot04O.cs','UI/MenuSystem.cs','World/CityLayout.Rebirth04O.cs','World/CityLayout.Routes04O.cs','Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Combat/ResonanceChains04N.cs','Gameplay/CrystalArsenal/CrystalFocusDb.cs','Gameplay/LootArmory.cs','Gameplay/CrystalArsenal/CrystalArsenal.Claims.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/RegionalArmor04OE.cs','Gameplay/ForgeTreatments04M.cs','Gameplay/Bonds/BondData.cs','Gameplay/Bonds/BondRules.cs','Gameplay/Bonds/BondSystem.Fusion.cs','Creatures/CreatureDirector.cs','Creatures/CreatureDirector.Bosses.cs','Creatures/CreatureDirector.Ecology.cs']
 check('Saved layouts defaults banks campaign entitlements costs and spawn budgets retain 04OE bytes',all(unchanged('Assets/NeonSyndicate/Scripts/Runtime/'+s) for s in preserved))
 check('New tailoring and exact collapsed-face cleanup run in the original body builder',all(s in read('Characters/BodyBuilder.cs')for s in ['BuildCinematicTailoring04OF();','BuildForgewright04OF();','b.RemoveCollapsedTriangles04OF();']) and 'FinishFace04L();' in read('Characters/BodyBuilder.cs'))
 check('Phase art is attached to original boss chest and reads original broken parts','AddComponent<RegionalBossPresentation04OF>().Bind' in read('Creatures/CreatureBoss.Regional04OD.cs') and 'BrokenEffects' in read('Creatures/RegionalBossPresentation04OF.cs') and 'TakeDamage' not in read('Creatures/RegionalBossPresentation04OF.cs'))
 check('Original single synergy impact spend and cancel remain authoritative',read('Gameplay/Bonds/BondSystem.Resonance04N.cs').count('target.Health.TakeDamage(hit)')==1 and all(t in read('Gameplay/Bonds/BondSystem.Resonance04N.cs')for t in ['Game.CrystalCombat.Spend(cost)','PartnerEligible','RegionalSynergyKind04OF=0']))
 check('Only existing revision-five routes receive collidable new structure','CinematicRoute04OF(stone,mineral,root,route)' in read('World/CityLayout.Unbound04OE.cs') and 'EmitWildMesh04OC(route.Points[0],root' in read('World/CityLayout.Unbound04OE.cs'))
 check('Legacy paid event receipts block renewed payout without migrating saves','PreviouslyPaid04OF(_cityRewardTokens04F.Tokens' in read('Gameplay/Events/WorldEvents.Crystal04F.cs') and 'revision==5' in read('Creatures/FracturedWildsCombat04OC.cs'))
 editor=(ROOT/'Assets/NeonSyndicate/Scripts/Editor/QA/CinematicAcceptanceWindow04OF.cs').read_text()
 check('Acceptance toolkit reuses live capture and staged QA authority',all(t in editor for t in ['QaTools.StartPlaceMomentShot()','QaTools.StartGoldenShots()','QaTools.StartPerfCapture()','QaTools.Cancel()','SafeEvidenceFile','Logs','Acceptance04OF']) and 'SaveSystem.Save(' not in editor and 'SetCinematic' not in editor)
 runner=(ROOT/'Assets/NeonSyndicate/Scripts/Editor/QA/AcceptanceTestRun04OF.cs').read_text()
 check('Native test status derives from real callbacks and exports raw XML',all(t in runner for t in ['ICallbacks','SaveResultToFile(result','CancelTestRun','result.ResultState','NO EXECUTED CASES','LAUNCHED']))
 wizard=(ROOT/'Assets/NeonSyndicate/Scripts/Editor/ImportedVisualWizard04OF.cs').read_text()
 check('Import wizard writes separate prefab and disabled original registry row',all(t in wizard for t in ['ValidMapping','GetComponentsInChildren<SkinnedMeshRenderer>','PrefabUtility.SaveAsPrefabAsset','Enabled=false','ImportedModels04OD.asset','ASSET REQUIRED']))
 check('New pure policies join the original reset hub',all(t+'.ResetStatics();' in read('Core/Game.cs')for t in ['CinematicAcceptance04OF','CinematicMotion04OF','RegionalBossPose04OF']))
 report={'scope':__doc__,'accepted_parent':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'predecessor_raw_exit':run.returncode,'superseded_scope':'Exactly two Test Framework references added to the Editor asmdef. All other non-source asset bytes and prior guards remain protected.','native_unity':'NOT RUN'}
 a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return 0 if all(c['passed']for c in checks)else 1
if __name__=='__main__':raise SystemExit(main())
