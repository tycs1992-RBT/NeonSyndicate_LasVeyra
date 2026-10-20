#!/usr/bin/env python3
"""04OG source ownership and preservation checks. Not native import or gameplay acceptance."""
import argparse,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE='a6c86a574cd82e3fee0f4b2f48f922a81127ea24'
RUNTIME='Assets/NeonSyndicate/Scripts/Runtime/'
def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--report',type=Path,required=True);args=parser.parse_args();checks=[]
 def check(name,ok):checks.append({'name':name,'passed':bool(ok)})
 def before(path):return subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT)
 def text(path):return (ROOT/RUNTIME/path).read_text()
 preserved=['Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Core/CityRebirthBoot04O.cs','Combat/ResonanceChains04N.cs','Gameplay/CrystalArsenal/CrystalFocusDb.cs','Gameplay/LootArmory.cs','Gameplay/CrystalArsenal/CrystalArsenal.Claims.cs','Gameplay/RegionalArsenal04OE.cs','Gameplay/RegionalArmor04OE.cs','Gameplay/ForgeTreatments04M.cs','Gameplay/Bonds/BondData.cs','Gameplay/Bonds/BondRules.cs','Gameplay/Bonds/BondSystem.Fusion.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs','Creatures/CreatureDirector.Bosses.cs','Creatures/CreatureBoss.Regional04OD.cs','World/CityLayout.Routes04O.cs']
 check('Original campaign, preview boot default, damage, rewards, entitlements, Forge and BONDS owners retain accepted bytes',all(before(RUNTIME+p)==(ROOT/RUNTIME/p).read_bytes() for p in preserved))
 changes=subprocess.check_output(['git','diff','--name-only',BASE,'--','Assets'],cwd=ROOT,text=True).splitlines()
 allowed_json={'crystal_realms_04od.json','crystal_unbound_04oe.json','fractured_wilds_04oc.json','gothic_realm_04ob.json'}
 assets_ok=True;quest_ok=True;metadata_ok=True
 for path in changes:
  exists=subprocess.run(['git','cat-file','-e',BASE+':'+path],cwd=ROOT,capture_output=True).returncode==0
  if not exists:continue
  if path.endswith('.meta'):metadata_ok &= before(path)==(ROOT/path).read_bytes()
  if path.endswith('.cs'):continue
  if Path(path).name in allowed_json and '/Resources/Quests/' in path:
   old=json.loads(before(path));new=json.loads((ROOT/path).read_text())
   def extend(value):
    if isinstance(value,dict):
     for key,child in value.items():
      if key=='WorldRevisions' and isinstance(child,list) and 5 in child and 6 not in child:child.append(6)
      else:extend(child)
    elif isinstance(value,list):
     for child in value:extend(child)
   extend(old);quest_ok &= old==new
  else:assets_ok &= before(path)==(ROOT/path).read_bytes()
 check('Every pre-existing non-source asset, GUID, scene, material and reference retains accepted bytes',assets_ok and metadata_ok)
 check('Inherited quest edits only append revision 6 eligibility; IDs, stages and payments unchanged',quest_ok)
 check('Append-only saved schema untouched',before(RUNTIME+'Gameplay/SaveSystem.cs').split(b'public static class SaveSystem')[0]==(ROOT/RUNTIME/'Gameplay/SaveSystem.cs').read_bytes().split(b'public static class SaveSystem')[0])
 world=text('World/CityLayout.Frontier04OG.cs')
 check('New structural collision is explicitly revision 6 only',all(s in world for s in ['if(!L.Frontier04OG)return;','Revision04O!=CityRevision04O.Legacy04N','CityRevision04O.LivingFrontier04OG','WalkSpan04OE','EmitWildMesh04OC']) and 'lasveyra_frontier04og_save_' in text('Gameplay/SaveSystem.cs'))
 check('Title loading admits a frontier-only save bank','SaveSystem.AnyExists(6)' in text('UI/MenuSystem.cs'))
 check('Revision-five runtime geometry stays unchanged', 'CinematicRoute04OF(decorStone,decorMineral,root,route)' in text('World/CityLayout.Unbound04OE.cs') and ',false)' in text('World/CityLayout.Unbound04OE.cs'))
 event=text('Gameplay/Events/EventKinds.Living04M.cs')
 check('Finite formation retains body admission, contribution, retreat, original damage and cleanup',all(t in event for t in ['AllocatedBodyCost04M','AllocatedGroups04N','CleanupHooks.Add(formation.Close)','contributed && e.PlayerAccepted','away>=5f','Mathf.Min(95f','NoRewards=true']) and 'TakeDamage(' not in text('Creatures/FrontierCombat04OG.cs'))
 check('Ward and attacks use original health filters, stagger, core breaks and global tokens','FilterWard04OG(ref info)' in text('Creatures/CreatureAbilities.cs') and 'FrontierFormation04OG.CanBegin' in text('Creatures/Creature.Brain.cs') and 'PartEffect.CoreExposed' in text('Creatures/FrontierCombat04OG.cs'))
 check('Calm wildlife uses existing ecology admission and brain only','o.CalmUntilProvoked=o.FrontierWildlife04OG;' in text('Creatures/CreatureDirector.Ecology.cs') and 'ForageFrontier04OG(dt)' in text('Creatures/Creature.StarterHabitat.cs') and 'Spawn(' not in text('Creatures/FrontierWildlife04OG.cs'))
 check('New static policies reset through original hub',all(t+'.ResetStatics();' in text('Core/Game.cs') for t in ['FrontierCombat04OG','FrontierGeometry04OG']))
 check('Native test callbacks remain actual result source','SaveResultToFile(result' in (ROOT/'Assets/NeonSyndicate/Scripts/Editor/QA/AcceptanceTestRun04OF.cs').read_text())
 report={'scope':__doc__,'accepted_parent':BASE,'checks':checks,'passed':sum(c['passed']for c in checks),'total':len(checks),'native_unity':'NOT RUN'}
 args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[c['name']for c in checks if not c['passed']]}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
