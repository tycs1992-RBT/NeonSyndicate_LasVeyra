#!/usr/bin/env python3
"""04M preservation/integration receipts only. Not a Unity playtest or visual assessment."""
import argparse,json,re,subprocess
from pathlib import Path
BASE='8c6c3804b70898e745479affecdfe5f66d95ad1e'
ROOT=Path(__file__).resolve().parents[1]
def inspect(root):
 def git(*args):return subprocess.check_output(['git',*args],cwd=root,text=True)
 def before(path):return git('show',BASE+':'+path)
 rows=[]
 def check(name,passed):rows.append({'name':name,'passed':bool(passed)})
 original=set(git('ls-tree','-r','--name-only',BASE).splitlines())
 changed=[p for p in git('diff','--name-only',BASE,'--').splitlines() if p in original]
 check('Original settings and packages retained',not any(p.startswith(('ProjectSettings/','Packages/')) for p in changed))
 check('Original scene, art, prefab and GUID payloads retained',not any(p.endswith(('.meta','.unity','.prefab','.asset','.fbx','.obj','.png','.jpg','.wav')) for p in changed))
 check('Original quests and events retained',not any(p.startswith(('Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/')) for p in changed))
 prefix='Assets/NeonSyndicate/Scripts/Runtime/'
 for name in ['Characters/PlayerCamera.cs','Core/GInput.cs','UI/MenuSystem.cs','Gameplay/MissionManager.cs','Gameplay/SagaDirection04L.cs','Gameplay/MissionContext.Performance04L.cs','Gameplay/MissionContext.Cast04L.cs','Characters/HumanoidAnimator.cs','Gameplay/StoryMissions.cs']:
  path=prefix+name;check('04L campaign/cinematic owner retained '+name,(root/path).read_text()==before(path))
 check('Original saved declaration files retained',not any('Save' in Path(p).name and p.endswith('.cs') for p in changed))
 # Field declarations in mixed owner files must remain exact even though methods may grow.
 for name,classes in [('Gameplay/MaterialStore.cs',['MaterialSave']),('Gameplay/CraftingSystem.cs',['CraftSave']),('Gameplay/FactionSystem.cs',['FactionSave'])]:
  path=prefix+name;old=before(path);new=(root/path).read_text()
  for kind in classes:
   def fields(text):
    match=re.search(r'class '+kind+r'\s*\{(.*?)\n    \}',text,re.S);return re.findall(r'public\s+[^;\n]+;',match.group(1)) if match else []
   check('Existing fields retained '+kind,fields(old)==fields(new) and bool(fields(old)))
 check('Old controls retained','Docs/CONTROLS.md' not in changed)
 events=json.loads((root/'Assets/NeonSyndicate/Resources/Events/living_world_04m.json').read_text())['Events']
 check('Three new finite event identities',len(events)==3 and len({e['Id'] for e in events})==3 and {e['Kind'] for e in events}=={'CrystalMuster','DrakeRescue','CrystalCache'})
 check('Event cooldowns and durations bounded',all(e['Cooldown']>=420 and 0<e['Duration']<=240 and e['Veil']=='Outer' for e in events))
 contracts=json.loads((root/'Assets/NeonSyndicate/Resources/Quests/living_world_contracts_04m.json').read_text())['Contracts']
 check('Three real guild contracts',len(contracts)==3 and {c['Guild'] for c in contracts}=={'menders','frequency_order','forgewrights'} and {c['Kind'] for c in contracts}=={'Escort','Rift','Courier'})
 treatment=(root/(prefix+'Gameplay/ForgeTreatments04M.cs')).read_text()
 check('Eight applied recipes appended through original Forge','ForgeTreatments04M.Append(l)' in (root/(prefix+'Gameplay/ForgeDb.cs')).read_text() and len(re.findall(r'Add\(recipes,\d,',treatment))==8)
 owner=(root/(prefix+'Gameplay/CraftingSystem.cs')).read_text()
 check('Paid treatment and restore integration','ForgeMath.TryPay(c, _wallet)' in owner and 'ForgeTreatments04M.Activate(_buffs' in owner and 'ForgeTreatments04M.Normalize(_buffs)' in owner)
 gather=(root/(prefix+'Gameplay/MaterialStore.Fieldwork04M.cs')).read_text()
 check('Finite receipts, zone ownership and mission-priority-safe GPS','SetFlag(node.Source.Receipt)' in gather and 'WorldBounds.IsUnderground' in gather and 'SetWaypoint(point)' in gather and 'SetMissionTarget' not in gather)
 muster=(root/(prefix+'Gameplay/Events/EventKinds.Living04M.cs')).read_text()
 check('Muster preserves explicit consent plus contribution', 'if(contributed && e.PlayerAccepted)' in muster and 'e.PlayerAccepted=true' not in muster)
 check('Muster uses original pending/live reservations and group cap','AllocatedBodyCost04M' in muster and 'groups+1>capGroups' in muster)
 check('Humanoid calm observers honor explicit combat and provocation','LivingEcology04M.CalmGuard' in (root/(prefix+'AI/NpcController.Creature.cs')).read_text())
 return rows
if __name__=='__main__':
 a=argparse.ArgumentParser(description=__doc__);a.add_argument('--root',type=Path,default=ROOT);a.add_argument('--report',type=Path);x=a.parse_args();rows=inspect(x.root)
 report={'scope':__doc__,'baseline':BASE,'passed':sum(r['passed'] for r in rows),'total':len(rows),'checks':rows}
 if x.report:x.report.parent.mkdir(parents=True,exist_ok=True);x.report.write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps(report,indent=2));raise SystemExit(0 if all(r['passed'] for r in rows) else 1)
