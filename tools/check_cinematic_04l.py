#!/usr/bin/env python3
"""04L source/preservation audit. This is not a Unity import, playtest or visual assessment."""
import argparse, hashlib, json, re, subprocess
from pathlib import Path
BASE='df9072930596202b2a8f0fc23c9499f76faada6a'
ROOT=Path(__file__).resolve().parents[1]
def inspect(root):
 def git(*a):return subprocess.check_output(['git',*a],cwd=root,text=True).strip()
 rows=[]
 def check(name,ok):rows.append({'name':name,'passed':bool(ok)})
 original=set(git('ls-tree','-r','--name-only',BASE).splitlines())
 changed=[p for p in git('diff','--name-only',BASE,'--').splitlines() if p in original]
 check('Exact 04K ancestor retained',subprocess.run(['git','merge-base','--is-ancestor',BASE,'HEAD'],cwd=root).returncode==0)
 check('Pinned packages and project settings preserved',not any(p.startswith(('Packages/','ProjectSettings/')) for p in changed))
 check('All original scenes, prefabs, art and metadata preserved',not any(p.endswith(('.unity','.prefab','.meta','.fbx','.obj','.png','.jpg','.wav','.asset')) for p in changed))
 check('All predecessor quest/event JSON preserved',not any(p.startswith(('Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/')) for p in changed))
 check('Save declarations and original controls unchanged',not any(('Save' in Path(p).name and p.endswith('.cs')) or p=='Docs/CONTROLS.md' for p in changed))
 runtime='Assets/NeonSyndicate/Scripts/Runtime/'
 for p in ['Characters/RigLayout.cs','Gameplay/Bonds/BondIds.cs']:
  path=runtime+p
  if path in original:check('Stable rig/bond declaration '+p,path not in changed)
 story=runtime+'Gameplay/StoryMissions.cs'
 def missions(s):return re.findall(r'Add\(m, "(m\d\d_[^"]+)", "([^"]+)", "([^"]+)", "([^"]+)", "[^"]*", (\d+),',s)
 before=git('show',BASE+':'+story);after=(root/story).read_text()
 check('Twelve original mission IDs, titles, contacts and rewards retained',len(missions(after))==12 and missions(before)==missions(after))
 for line in ['Mercy. This place shelters people. It doesn\'t rule them.','Power. Nobody takes my family again. Nobody.']:
  check('Original ending dialogue retained: '+line.split('.')[0],line in after and line in before)
 quests=[]
 for p in (root/'Assets/NeonSyndicate/Resources/Quests').glob('*.json'):quests+=json.loads(p.read_text()).get('Quests',[])
 ids=[q['Id'] for q in quests];check('Globally unique quest IDs',len(ids)==len(set(ids)))
 new=[q for q in quests if q['Id'].startswith('uh05_04l_')]
 check('Exactly one optional 04L investigation',len(new)==1 and new[0].get('Kind')=='Side' and new[0].get('Giver')=='Cast:dispatcher')
 if new:
  q=new[0];steps=[s for p in q['Phases'] for s in p['Steps']]
  check('Physical clue, owned care/assist, courier return and Dispatch exit',all(any(s['Type']==t for s in steps) for t in ('Interact','Care','Assist','Courier','Skirmish')) and steps[-1].get('At')=='Venue:dispatch.exit')
  check('No direct item/bond/fusion or power grants',not any(s['Type'] in ('Give','GiveGun','Rescue','BondRank','Fuse','Shift') for s in steps))
  fights=[s for s in steps if s['Type']=='Skirmish']
  check('One four-body fight, zero reinforcements',len(fights)==1 and sum(e['Count'] for e in fights[0]['Roster'])==4 and fights[0].get('Reinforce',0)==0)
 manager=(root/(runtime+'Gameplay/MissionManager.cs')).read_text()
 check('Existing context owns dialogue and interruption cleanup','BeginPerformance04L(pairs)' in manager and 'finally { EndPerformance04L(); }' in manager and 'ctx.EndPerformance04L()' in manager and 'private void OnDisable()' in manager)
 check('Source-only evidence explicitly labelled',all('native' in p.name.lower() or p.suffix in ('.json','.log','.xml','.gz') for p in (root/'Docs/CinematicSaga04L/Evidence').glob('*')))
 return rows
def main():
 a=argparse.ArgumentParser();a.add_argument('--root',type=Path,default=ROOT);a.add_argument('--report',type=Path);x=a.parse_args();rows=inspect(x.root)
 report={'baseline':BASE,'scope':'source only; native import/play/render/save/performance/build are NOT proven','checks':rows,'passed':sum(r['passed'] for r in rows),'total':len(rows)}
 if x.report:x.report.parent.mkdir(parents=True,exist_ok=True);x.report.write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps(report,indent=2));return 0 if all(r['passed'] for r in rows) else 1
if __name__=='__main__':raise SystemExit(main())
