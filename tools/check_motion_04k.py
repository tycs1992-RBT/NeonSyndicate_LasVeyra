#!/usr/bin/env python3
"""04K source/preservation guard. Never a native gameplay acceptance test."""
import argparse, hashlib, json, subprocess
from pathlib import Path

BASE='221cefe8fa2fb3e7f9c927f6a2eeb7399ab1eb75'
ROOT=Path(__file__).resolve().parents[1]

def inspect(root):
    def git(*args):return subprocess.check_output(['git',*args],cwd=root,text=True).strip()
    original=set(git('ls-tree','-r','--name-only',BASE).splitlines())
    changed=[p for p in git('diff','--name-only',BASE,'--').splitlines() if p in original]
    rows=[]
    def check(name,ok):rows.append({'name':name,'passed':bool(ok)})
    check('Pinned Unity/package settings preserved',not any(p.startswith(('Packages/','ProjectSettings/')) for p in changed))
    check('Original scenes, prefabs, binary art and metadata preserved',not any(p.endswith(('.unity','.prefab','.meta','.fbx','.png','.jpg','.wav','.asset','.obj')) for p in changed))
    check('Original quest/event files preserved',not any(p.startswith(('Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/')) for p in changed))
    check('Original save declaration files preserved',not any('Save' in Path(p).name and p.endswith('.cs') for p in changed))
    qdir=root/'Assets/NeonSyndicate/Resources/Quests';quests=[]
    for file in sorted(qdir.glob('*.json')):quests+=json.loads(file.read_text()).get('Quests',[])
    ids=[q['Id'] for q in quests];check('Quest IDs are globally unique',len(ids)==len(set(ids)))
    new=[q for q in quests if q['Id'].startswith('uh05_04k_')]
    check('One optional 04K adventure',len(new)==1 and new[0].get('Kind')=='Side')
    q=new[0];steps=[s for p in q['Phases'] for s in p['Steps']]
    check('Route physically returns and exits Dispatch',steps[-1].get('Type')=='GoTo' and steps[-1].get('At')=='Venue:dispatch.exit')
    check('No direct gun grants or unearned companions/fusion',not any(s['Type'] in ('GiveGun','Rescue','BondRank','Fuse') for s in steps))
    check('All encounter reinforcements are finite and zero',all(s.get('Reinforce',0)==0 for s in steps if s['Type']=='Skirmish'))
    check('At least two real interiors and an Underheart route',all(any(s.get('At','').startswith(prefix) for s in steps) for prefix in ('Venue:dispatch.','Venue:pressure.','Under:')))
    check('Exactly one durable caster entitlement',sum(s['Type']=='Claim' for s in steps)==1 and q['Claims']==['uh05_04k_recovered_caster'])
    digest=hashlib.sha256()
    for p in sorted((root/'Assets/NeonSyndicate').rglob('*.cs')):digest.update(p.relative_to(root).as_posix().encode()+b'\0'+p.read_bytes()+b'\0')
    return {'scope':__doc__,'passed':all(r['passed'] for r in rows),'checks':rows,'base':BASE,'csharp_sha256':digest.hexdigest(),'changed_existing_files':changed,'native_unity':'BLOCKED: separate measured licensing/UPM IPC failure; this guard does not run Unity'}

def main():
    a=argparse.ArgumentParser(description=__doc__);a.add_argument('--report',type=Path,required=True);x=a.parse_args();result=inspect(ROOT)
    x.report.parent.mkdir(parents=True,exist_ok=True);x.report.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'passed':result['passed'],'checks':len(result['checks']),'failed':[r['name'] for r in result['checks'] if not r['passed']]}));return int(not result['passed'])

if __name__=='__main__':raise SystemExit(main())
