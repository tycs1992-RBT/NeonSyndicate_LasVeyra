#!/usr/bin/env python3
"""04OD additive source guards. Not native Unity acceptance."""
import argparse,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);args=p.parse_args();args.report.parent.mkdir(parents=True,exist_ok=True)
 inherited=args.report.with_name('inherited-04oc-raw.json')
 run=subprocess.run([sys.executable,str(ROOT/'tools/check_fractured_wilds_04oc.py'),'--report',str(inherited)],capture_output=True,text=True,cwd=ROOT)
 old=json.loads(inherited.read_text());checks=[]
 def check(name,value):checks.append({'name':name,'passed':bool(value)})
 def read(path):return (ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/path).read_text()
 # Exactly one additional byte-preservation exception: two earned regional variants inside the same synergy impact.
 expected=['Original owner retained: Gameplay/Bonds/BondSystem.Resonance04N.cs']
 check('Predecessor raw differences are exactly the documented regional synergy extension',old['inherited']['unexpected']==expected)
 changed=[c['name'] for c in old['checks'] if not c['passed']]
 check('All other 04OC current guards pass',changed==['No unexpected predecessor guard failures','All superseded guard scopes stay explicitly recorded'])
 check('Revision 4 has an isolated filename and no default promotion','lasveyra_realms04od_save_' in read('Gameplay/SaveSystem.cs') and 'Crystal realms preview (04O-D)' in read('UI/MenuSystem.cs') and 'new SaveData' not in read('World/CityLayout.Realms04OD.cs'))
 check('The real city builder emits regional visible and collision meshes','BuildRealms04OD();' in read('World/CityBuilder.Fractured04OC.cs') and 'EmitWildMesh04OC' in read('World/GothicRealmGeometry04OD.cs'))
 check('Bosses extend the original fight, cleanup and reward authority',all(t in read('Creatures/CreatureBoss.cs') for t in ['UpdateRegional04OD(dt)','_regionalCycle04OD.Close()']) and 'RegionalBossRules04OD.FirstWin(byPlayer,IsBossDefeated(sp.Id))' in read('Creatures/CreatureDirector.cs'))
 check('Boss admission reserves the original body and group budgets','AllocatedBodyCost04M' in read('Creatures/CreatureDirector.Bosses.cs') and 'AllocatedGroups04N' in read('Creatures/CreatureDirector.Bosses.cs'))
 check('Regional quest admission cannot fall back into an unowned combat body','RegionalBossRules04OD.IsRegional(species))return null' in read('Gameplay/Quests/QuestRunner.cs'))
 check('Broken-part materials use saved original bestiary receipts','!Game.Compendium.IsPartBroken(Species.Id,def.Id)' in read('Creatures/CreaturePartsRuntime.cs'))
 check('Paid modifiers preserve original sweep and stamina owner','RegionalGear04OD.Move' in read('Characters/PlayerCombat.cs') and 'RegionalGear04OD.Build' not in read('Combat/WeaponController.cs'))
 check('Synergy remains one original cost and one original impact','RegionalGear04OD.Synergy' in read('Gameplay/Bonds/BondSystem.Resonance04N.cs') and read('Gameplay/Bonds/BondSystem.Resonance04N.cs').count('target.Health.TakeDamage(hit)')==1)
 check('Optional models retain fallback and never import physics','Restore();' in read('Characters/ImportedVisual04OD.cs') and 'GetComponentsInChildren<Collider>' in read('Characters/ImportedVisual04OD.cs'))
 check('New static policies join the original reset hub',all(t+'.ResetStatics();' in read('Core/Game.cs') for t in ['RegionalBossRules04OD','RegionalGear04OD','ImportedModelRegistry04OD']))
 preserved=['Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Combat/ResonanceChains04N.cs','Creatures/CompanionController.cs','Gameplay/LootArmory.cs','Gameplay/CraftingSystem.cs','Gameplay/CrystalArsenal/CrystalFocusDb.cs']
 check('Campaign focus bonds inventory and transaction owners retain baseline bytes',all(subprocess.run(['git','diff','--quiet','00f65a4158892f52c806ce153fe14e22e0fc79a5','--',str(ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/s)],cwd=ROOT).returncode==0 for s in preserved))
 report={'scope':__doc__,'checks':checks,'passed':sum(c['passed'] for c in checks),'total':len(checks),'predecessor_raw_exit':run.returncode,'inherited_04ob':old['inherited'],'additional_superseded_scope':{'name':expected[0],'reason':'Original synergy receives two quest-earned, focus-qualified regional variants. Same original eligibility, costs, window and single damage call.'},'native_unity':'NOT RUN'}
 args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return 0 if all(c['passed']for c in checks)else 1
if __name__=='__main__':raise SystemExit(main())
