#!/usr/bin/env python3
"""04O-C owner and integration guards. These are not native Unity, gameplay or visual acceptance."""
import argparse,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SUPERSEDED={
 'Original owner retained: Gameplay/Quests/QuestPlaces.cs':'Adds Realm tokens resolved from the active layout; old token branches remain.',
 'Original owner retained: Creatures/CreatureDirector.cs':'Appends a runtime-only prowler option; saved species/bond IDs are unchanged.',
 '04N inherited: Legitimate finite muster caster with capped admission':'Regional species dispatch extends the same finite Muster, budget and damage owner.',
 '04N inherited: Existing eight paid Forge treatments remain exact':'Original eight definitions are retained; two quest-earned exclusive treatments append.',
 'Only explicitly authorized revision-2 spatial-query scope differs from prior guards':'Explicit revision-3 option is added, without changing old profile geography.',
 'Original durable payment capture/restore and cleanup bytes retained; only selection admits the new event':'Revision-3 regional reward uses one first-win receipt; old occurrence tokens remain.',
 'Original finite Muster runtime is unchanged':'Existing finite runtime now selects the regional body and attack profile.'}
def main():
 a=argparse.ArgumentParser(description=__doc__);a.add_argument('--report',type=Path,required=True);x=a.parse_args();x.report.parent.mkdir(parents=True,exist_ok=True)
 old=x.report.with_name('inherited-04ob-raw.json');older=x.report.with_name('inherited-04o-raw.json')
 run=subprocess.run([sys.executable,str(ROOT/'tools/check_gothic_realm_04ob.py'),'--report',str(old),'--inherited-report',str(older)],capture_output=True,text=True,cwd=ROOT)
 previous=json.loads(old.read_text());failed=[c['name']for c in previous['checks']if not c['passed']];unexpected=[name for name in failed if name not in SUPERSEDED]
 def read(p):return (ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/p).read_text()
 checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 check('No unexpected predecessor guard failures',not unexpected)
 check('All superseded guard scopes stay explicitly recorded',set(failed)==set(SUPERSEDED))
 world=read('World/CityLayout.Fractured04OC.cs');scenery=read('World/CityLayout.Fractured04OC.Scenery.cs');build=read('World/CityBuilder.Fractured04OC.cs');save=read('Gameplay/SaveSystem.cs');event=read('Gameplay/Events/EventKinds.Living04M.cs');receipts=read('Gameplay/Events/WorldEvents.Crystal04F.cs')
 check('Only explicit new profile calls the reconstruction','revision == CityRevision04O.FracturedWilds04OC' in read('World/CityLayout.Rebirth04O.cs') and 'Revision04O!=CityRevision04O.Legacy04N' in world)
 check('Revision two is derived before new-only changes','!ApplyGothic04OB(venues)' in world and 'Revision04O=CityRevision04O.FracturedWilds04OC' in world)
 check('Bounded settlement removes real unowned buildings','lot.Buildings.RemoveAt(i)' in world and 'InSettlement04OC' in world)
 check('All exposed source structures receive a disposition','WildCoverage04OC.Add(row)' in world and 'preserved real service owner' in world)
 check('Real grotto footprint moves with its semantic destination','b.Footprint=Translate04OB' in world and 'RealmGrotto04OB-new Vector3(12f' in world)
 check('Terrain geometry and exact collision share their source mesh','WildTerrain04OC(terrain,L,patch)' in build and 'AddComponent<MeshCollider>().sharedMesh=data' in build)
 check('World constructor calls new terrain through the existing city root','BuildFracturedWilds04OC();' in read('World/CityBuilder.cs'))
 check('Roads, receipts, routes and sites reserve terrain clearance',all(v in scenery for v in ['RealmRoutes04OB','EdgeRect(e)','DataChipSpots','ParkingSpots','SpecialSite']))
 check('All save banks have explicit filenames','lasveyra_wilds04oc_save_' in save and 'lasveyra_realm04ob_save_' in save and 'lasveyra_save_' in save)
 check('Wrong-bank restore fails inside verified backup selection','BankMatches04OC(candidate.WorldRevision04O, revision)' in save)
 check('Old profile entry buttons remain and new profile is explicit',all(v in read('UI/MenuSystem.cs')for v in ['Gothic realm preview (04O-B)','Fractured wilds preview (04O-C)','CityRevision04O.Crystalline04O']))
 check('Muster preserves body admission, consent, contribution and withdrawal',all(v in event for v in ['AllocatedBodyCost04M','e.AddInteract','contributed && e.PlayerAccepted','away>=5f','e.DespawnCreatures()','NoRewards=true']))
 check('Regional first-win token does not replace old event receipts','FracturedWildsCombat04OC.RewardToken' in receipts and 'first-win-04oc' in read('Creatures/FracturedWildsCombat04OC.cs') and 'eventId+":"+occurrence' in read('Creatures/FracturedWildsCombat04OC.cs'))
 check('Storm prowler body and attacks are called by original owners','AzureLook(j.Look)' in read('Creatures/CreatureDirector.Bodies.cs') and 'FracturedWildsCombat04OC.AzureAttacks()' in read('Creatures/Creature.cs'))
 check('New static combat data joins reset','FracturedWildsCombat04OC.ResetStatics();' in read('Core/Game.cs'))
 check('Original side story, bonds, focus and damage owners remain byte-preserved',all(subprocess.run(['git','diff','--quiet','c84ac3f005b966d6fc4fcbbab78065b107ce8c3e','--',str(ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/p)],cwd=ROOT).returncode==0 for p in ['Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Combat/ResonanceChains04N.cs','Creatures/CompanionController.cs','Gameplay/LootArmory.cs','Gameplay/CraftingSystem.cs']))
 report={'scope':__doc__,'inherited':{'passed':previous['passed'],'total':previous['total'],'raw_exit':run.returncode,'superseded':[{'name':n,'reason':SUPERSEDED[n]}for n in failed if n in SUPERSEDED],'unexpected':unexpected},'passed':sum(c['passed']for c in checks),'total':len(checks),'checks':checks,'native_unity':'NOT RUN'}
 x.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='checks'},indent=2));return 0 if all(c['passed']for c in checks)else 1
if __name__=='__main__':raise SystemExit(main())
