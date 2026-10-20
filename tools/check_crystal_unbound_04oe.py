#!/usr/bin/env python3
"""04OE source integration and preservation guards. Never native Unity acceptance."""
import argparse,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='0765f10f02125dca0ef95b86e9f7263e14963e2c'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);a=p.parse_args();a.report.parent.mkdir(parents=True,exist_ok=True)
 raw=a.report.with_name('inherited-04od-raw.json')
 run=subprocess.run([sys.executable,str(ROOT/'tools/check_crystal_realms_04od.py'),'--report',str(raw)],cwd=ROOT,capture_output=True,text=True)
 previous=json.loads(raw.read_text());checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def read(path):return (ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/path).read_text()
 expected=['Predecessor raw differences are exactly the documented regional synergy extension','All other 04OC current guards pass','Revision 4 has an isolated filename and no default promotion','Campaign focus bonds inventory and transaction owners retain baseline bytes']
 differences=[c['name']for c in previous['checks']if not c['passed']]
 check('Every predecessor difference has a narrow explicit scope',differences==expected)
 scopes={
 'Original owner retained: Combat/ResonanceSystem.Chains04N.cs':'Adds regional mastery text to the existing Resonance page.',
 'Original owner retained: Gameplay/Bonds/BondSystem.Resonance04N.cs':'Original one-impact art receives earned regional confirmations and pose selection.',
 'Original owner retained: Creatures/CompanionController.cs':'One optional visual adapter hook after original rig pose.',
 'Original owner retained: Gameplay/CraftingSystem.cs':'Paid regional commissions use the original Gear transaction.',
 '04N inherited: Original all-eight treatment group/expiration owner preserved':'Crafting owner now adds permanent gear commissions; original treatment activation and timers remain unchanged.'}
 check('No unlisted inherited owner changes',set(previous['inherited_04ob']['unexpected'])==set(scopes))
 check('Both old and new previews remain explicit','CityRevision04O.CrystalRealms04OD,null,true' in read('UI/MenuSystem.cs') and 'CityRevision04O.CrystalUnbound04OE,null,true' in read('UI/MenuSystem.cs'))
 check('New topology has a separate bank and cannot reinterpret revision four','lasveyra_unbound04oe_save_' in read('Gameplay/SaveSystem.cs') and 'Revision04O!=CityRevision04O.Legacy04N' in read('World/CityLayout.Unbound04OE.cs'))
 check('Wrong-bank validation remains inside backup selection','BankMatches04OC(candidate.WorldRevision04O, revision)' in read('Gameplay/SaveSystem.cs'))
 check('Permanent rewards use original pending ledger UID and claim owner','RegionalArsenal04OE.SignatureFor(bossId)' in read('Gameplay/CrystalArsenal/CrystalArsenal.cs') and 'CrystalEntitlementRules.Create(_entitlements' in read('Gameplay/CrystalArsenal/CrystalArsenal.cs') and 'RegionalCollection04OE(e.Unique)' in read('Gameplay/CrystalArsenal/CrystalArsenal.Claims.cs'))
 check('Melee definitions bypass converted gun triggers','RegionalArsenal04OE.Definition(g, slotId)' in read('Gameplay/LootGenerator.cs') and 'Fire=FireKind.Melee' in read('Gameplay/RegionalArsenal04OE.cs') and 'RegionalArsenal04OE.IsWeapon(g)' in read('Combat/CrystalCombatPolicy04F.cs'))
 check('Held geometry contact and move timing join original owners','RegionalArsenal04OE.Move(move,d.Loot)' in read('Characters/PlayerCombat.cs') and 'RegionalArsenal04OE.Tip' in read('Combat/BladeSweep04N.cs') and 'CreateRegional04OE(live.Loot' in read('Combat/WeaponModels.cs'))
 check('New walk geometry goes through exact original collision chunks','BuildUnbound04OE();' in read('World/GothicRealmGeometry04OD.cs') and 'EmitWildMesh04OC' in read('World/CityLayout.Unbound04OE.cs') and 'AddComponent<MeshCollider>().sharedMesh=data' in read('World/CityBuilder.Fractured04OC.cs'))
 check('New height connections are restricted to authored spans','SharedUnboundSpan04OE' in read('World/CityLayout.Routes04O.cs') and 'Unbound04OE?' in read('World/CityLayout.Gothic04OB.cs'))
 check('Regional coats use existing GearItem payment and original timers','RegionalArmor04OE.Build' in read('Gameplay/CraftingSystem.cs') and 'ForgeMath.TryPay(GearPrice(o), _wallet)' in read('Gameplay/CraftingSystem.cs') and 'RegionalArmor04OE.Shell' in read('Gameplay/Gear.Fittings04N.cs'))
 check('Synergy keeps one impact and original eligibility cost and cancellation',read('Gameplay/Bonds/BondSystem.Resonance04N.cs').count('target.Health.TakeDamage(hit)')==1 and all(t in read('Gameplay/Bonds/BondSystem.Resonance04N.cs')for t in ['Game.CrystalCombat.Spend(cost)','PartnerEligible','CancelSynergy04N','RegionalResonance04OE.Credit']))
 check('Import adaptation uses owned drake original rig and explicit passive paths',all(t in read('Characters/ImportedVisual04OD.cs')for t in ['_companion.Uid>0','_companion.Rig.Bones','PassiveBone04OE','Restore();','GetComponentsInChildren<Collider>']))
 policies=['RegionalArsenal04OE','RegionalArmor04OE','RegionalBossArt04OE','RegionalResonance04OE']
 check('New static policies reset through original hub',all(t+'.ResetStatics();' in read('Core/Game.cs')for t in policies))
 preserved=['Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Combat/ResonanceChains04N.cs','Gameplay/LootArmory.cs','Gameplay/CrystalArsenal/CrystalFocusDb.cs','Gameplay/Bonds/BondRules.cs','Gameplay/Bonds/BondData.cs','Gameplay/Bonds/BondSystem.Fusion.cs','Gameplay/ForgeTreatments04M.cs','Creatures/CreatureDirector.cs','Creatures/CreatureDirector.Bosses.cs','Creatures/CreatureDirector.Ecology.cs']
 check('Campaign entitlements bonds fusion spawner budgets and treatment timers retain accepted bytes',all(subprocess.run(['git','diff','--quiet',BASE,'--','Assets/NeonSyndicate/Scripts/Runtime/'+s],cwd=ROOT).returncode==0 for s in preserved))
 changed=subprocess.check_output(['git','diff','--name-status',BASE,'--','Assets'],cwd=ROOT,text=True).splitlines()
 check('No existing asset or metadata deletion or GUID rewrite',not any(t.startswith('D\t') or (t.startswith('M\t') and t.endswith('.meta'))for t in changed))
 report={'scope':__doc__,'accepted_parent':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'predecessor_raw_exit':run.returncode,'superseded_04od_checks':expected,'additional_owner_scopes':scopes,'native_unity':'NOT RUN'}
 a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return 0 if all(c['passed']for c in checks)else 1
if __name__=='__main__':raise SystemExit(main())
