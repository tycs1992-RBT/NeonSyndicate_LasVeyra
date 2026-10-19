#!/usr/bin/env python3
"""04O integration and preservation guard. Source evidence only, never Unity play acceptance."""
import argparse,json,re,subprocess
from pathlib import Path
BASE='cd6d684541f86a4b1a31a8bfc8335783b9f99e2f'
ROOT=Path(__file__).resolve().parents[1]
def inspect(root):
 rows=[]
 def check(name,ok):rows.append({'name':name,'passed':bool(ok)})
 def source(path):return (root/'Assets/NeonSyndicate/Scripts/Runtime'/path).read_text()
 def git(*args):return subprocess.check_output(['git',*args],cwd=root,text=True)
 unchanged=['World/CityLayout.cs','World/CityLayout.Graph.cs','World/CityLayout.Frost.cs','World/CityLayout.Veil.cs',
 'Gameplay/StoryMissions.cs','Gameplay/MissionManager.cs','Gameplay/Quests/QuestPlaces.cs',
 'Combat/ResonanceChains04N.cs','Combat/ResonanceSystem.Chains04N.cs','Gameplay/Bonds/BondSystem.Resonance04N.cs',
 'Creatures/CreatureDirector.cs','Creatures/CompanionController.cs','Gameplay/LootArmory.cs','Gameplay/CraftingSystem.cs']
 for name in unchanged:
  path='Assets/NeonSyndicate/Scripts/Runtime/'+name
  check('Original owner retained: '+name,(root/path).is_file() and not git('diff',BASE,'--',path).strip())
 changed=git('diff','--name-only',BASE).splitlines()
 check('No original asset, scene, GUID or binary LFS payload changed',not any(
  p.startswith('Assets/') and not p.endswith('.cs') and subprocess.run(['git','cat-file','-e',BASE+':'+p],cwd=root,capture_output=True).returncode==0 for p in changed))
 added=git('diff','--unified=0',BASE,'--','Assets/NeonSyndicate/Scripts','Assets/NeonSyndicate/Tests').splitlines()
 check('No new em or en dash in source additions',not any(('\u2014' in l or '\u2013' in l) for l in added if l.startswith('+') and not l.startswith('+++')))
 root_source=source('Core/GameRoot.cs');builder=source('World/CityBuilder.cs');profile=source('World/CityLayout.Rebirth04O.cs')
 check('Explicit profile executes after original service binding before Underheart preparation',root_source.index('Game.Inhabited.Prepare(layout)')<root_source.index('layout.ApplyProfile04O(')<root_source.index('Game.Underheart.Prepare(layout)'))
 check('Legacy saves choose their revision before session restore','d.WorldRevision04O != (int)Game.Layout.Revision04O' in root_source and 'CityRebirthRules04O.Known(d.WorldRevision04O)' in root_source)
 check('New story returns to original layout','!freeRoam && Game.Layout' in root_source and 'CityRevision04O.Legacy04N, null, false' in root_source)
 check('Prologue skip crosses reload after boot','_resumeSkip04O && Game.Missions' in root_source and 'false,afterPrologue' in source('UI/MenuSystem.cs'))
 check('No migration writes in scene handoff','SaveSystem.Save' not in source('Core/CityRebirthBoot04O.cs') and 'save.WorldRevision04O!=(int)revision' in source('Core/CityRebirthBoot04O.cs'))
 check('Atomic original save capture stamps active revision','d.WorldRevision04O = Game.Layout' in source('Gameplay/SaveSystem.cs'))
 check('Save freeze adds revision','"WorldRevision04O:Int32"' in (root/'Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs').read_text())
 check('Geometry called by original building owner','if (BuildRebirth04O(bld)) return;' in builder and 'BuildSpine04O();' in builder)
 check('Original merged city meshes and new Spine have destruction owners','OwnedMeshes.Track(go,mesh)' in builder and 'OwnedMeshes.Track(part,mesh)' in source('World/CityBuilder.Rebirth04O.cs'))
 check('Actual collidable solids share geometry data','CityRebirthGeometry04O.Solids(building)' in source('World/CityBuilder.Rebirth04O.cs') and 'sharedMesh=mesh' in source('World/CityBuilder.Rebirth04O.cs'))
 check('All twelve service bindings required before mutation','venues.Count < 12' in profile and 'allLots.Contains(venue.Lot)' in profile)
 check('Receipt, roof and service parcels explicitly guarded','HoldsReceipt04O' in profile and 'RoofType.Helipad' in profile and 'protectedLots.Contains(lot)' in profile)
 check('Existing on-foot GPS uses revised graph, vehicles retain road path','Game.Layout.FindWalkRoute04O' in source('Gameplay/Guidance04E/GpsRoute04E.cs') and '!Game.Player.InVehicle' in source('Gameplay/Guidance04E/GpsRoute04E.cs'))
 check('Wanted and vehicles retain original road map','!player.InVehicle' in source('UI/Hud.Rebirth04O.cs') and 'Game.Wanted.Stars==0' in source('UI/Hud.Rebirth04O.cs'))
 check('All added state has normal reset hooks',all(name in source('Core/Game.cs') for name in ['CityRebirthBoot04O.ResetStatics();','CityRebirthRules04O.ResetStatics();','CityRebirthGeometry04O.ResetStatics();','Settings.ResetRebirth04O();']))
 check('No new combat, inventory, population or save manager',not any(re.search(r'\bclass\s+\w*(?:CombatManager|InventoryManager|PopulationManager|SaveManager)\b',p.read_text()) for p in (root/'Assets/NeonSyndicate/Scripts/Runtime').rglob('*04O.cs')))
 # Older sprint exact-file guards correctly flag the now-authorized menu and appended controls changes.
 # Keep their raw report, and fail on any additional inherited source-guard regression.
 import check_crystal_kingdom_04n as prior
 inherited=prior.inspect(root)
 approved={'Protected original owner UI/MenuSystem.cs','SaveData and old control declarations retained'}
 for row in inherited:
  if row['name'] not in approved:check('04N inherited: '+row['name'],row['passed'])
 check('Only approved menu/controls deltas differ from 04N exact-file guards',all(row['passed'] or row['name'] in approved for row in inherited))
 old_controls=git('show',BASE+':Docs/CONTROLS.md')
 check('All prior control documentation retained as a prefix',(root/'Docs/CONTROLS.md').read_text().startswith(old_controls))
 return rows
if __name__=='__main__':
 a=argparse.ArgumentParser(description=__doc__);a.add_argument('--root',type=Path,default=ROOT);a.add_argument('--report',type=Path);x=a.parse_args();rows=inspect(x.root)
 report={'scope':__doc__,'baseline':BASE,'passed':sum(r['passed'] for r in rows),'total':len(rows),'checks':rows}
 if x.report:x.report.parent.mkdir(parents=True,exist_ok=True);x.report.write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps(report,indent=2));raise SystemExit(0 if all(r['passed'] for r in rows) else 1)
