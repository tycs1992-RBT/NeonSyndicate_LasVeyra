#!/usr/bin/env python3
"""Verify exact 04A asset/input/travel/stronghold preservation and append-only saves.

This reads Git and source data only. It does not load player saves or run Unity.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
ROOT=Path(__file__).resolve().parents[1]
BASE='1afbaa9b7e311f977219412a1fb345bf6857c5a6'
RUNTIME='Assets/NeonSyndicate/Scripts/Runtime/'

def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--report',type=Path,required=True);args=parser.parse_args()
    checks=[]
    def check(name,passed,detail=None):checks.append({'name':name,'passed':bool(passed),'detail':detail})
    paths=git('ls-tree','-r','--name-only',BASE).decode().splitlines()
    unchanged=[];changed=[];bytes_checked=0
    for name in paths:
        suffix=Path(name).suffix
        protected=name.startswith(('Packages/','ProjectSettings/')) or (name.startswith('Assets/') and suffix in ('.meta','.obj','.bytes','.png','.wav','.shader','.mat','.prefab','.unity')) or name.startswith(('Assets/NeonSyndicate/Resources/Quests/','Assets/NeonSyndicate/Resources/Events/'))
        protected=protected or name.startswith(RUNTIME+'Gameplay/Strongholds/') or name.startswith(RUNTIME+'Gameplay/Discovery/') or name.startswith(RUNTIME+'Characters/PlayerController') or name.startswith(RUNTIME+'Characters/PlayerCamera') or name in [RUNTIME+'Core/'+x+'.cs' for x in ('Act','GInput','Pad','GameTime')] or name.startswith(RUNTIME+'Gameplay/Travel')
        if name == RUNTIME+'Gameplay/Strongholds/StrongholdSystem.cs':continue
        if not protected:continue
        old=git('show',BASE+':'+name);path=ROOT/name
        if not path.is_file():changed.append(name);continue
        current=path.read_bytes();bytes_checked+=len(current)
        if old.startswith(b'version https://git-lfs.github.com/spec/v1'):
            expected=re.search(rb'oid sha256:([a-f0-9]{64})',old).group(1).decode()
            equal=hashlib.sha256(current).hexdigest()==expected
        else:equal=old==current
        (unchanged if equal else changed).append(name)
    check('Original metadata/assets/shaders/scenes/packages/input/travel/02I/02G protected files retain exact contents',not changed,{'checked':len(unchanged)+len(changed),'changed':changed,'bytes_checked':bytes_checked})
    host=RUNTIME+'Gameplay/Strongholds/StrongholdSystem.cs'
    host_current=(ROOT/host).read_text()
    guard='            if((Game.Territories!=null && Game.Territories.HasActiveEncounter) || (Game.Events!=null && Game.Events.TerritoryEventActive))\n                return "Finish the territory encounter first.";\n'
    check('Removing the two-line mutual-encounter guard recovers exact original stronghold runtime',host_current.replace(guard,'')==git('show',BASE+':'+host).decode())
    allowed=['ArtRebirth04A/VisualRebirth04ABatch.cs','Core/Game.cs','Core/GameRoot.cs','Creatures/CreatureDirector.cs',
        'Creatures/Creature.Motion.cs','Creatures/Creature.StarterHabitat.cs','Gameplay/Events/EventInstance.cs',
        'Gameplay/Events/EventKinds.cs','Gameplay/Events/EventSelector.cs','Gameplay/Quests/QuestCatalog.cs',
        'Gameplay/Quests/QuestData.cs','Gameplay/Quests/QuestValidator.cs','Gameplay/WorldEvents.cs','Gameplay/SaveSystem.cs',
        'Gameplay/Strongholds/StrongholdSystem.cs','QA/QaTools.cs','UI/MenuSystem.cs']
    modified=set(git('diff','--name-only',BASE,'--',RUNTIME).decode().splitlines())
    existing=set(paths)
    unexpected=sorted((modified&existing)-{RUNTIME+p for p in allowed})
    check('Existing runtime edits remain limited to reviewed baseline and integration hosts',not unexpected,{'unexpected':unexpected,'hosts':sorted(modified&existing)})
    savepath=RUNTIME+'Gameplay/SaveSystem.cs';old=git('show',BASE+':'+savepath).decode();current=(ROOT/savepath).read_text()
    def declarations(text):
        segment=text[text.index('public sealed class SaveData'):text.index('public static class SaveSystem')]
        return re.findall(r'^        public (?!sealed|static)([^\n]+;)$',segment,re.M)
    prior=declarations(old);now=declarations(current)
    check('SaveData old declarations and initializers remain byte-equivalent and in order',now[:len(prior)]==prior,{'previous_declarations':len(prior),'new_declarations':now[len(prior):]})
    check('SaveData only appends the versioned optional territory block',now[len(prior):]==['TerritorySave Territories = new TerritorySave();'])
    freeze=(ROOT/'Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs').read_text()
    check('SaveData freeze list includes appended territory identity','"Territories:TerritorySave"' in freeze)
    game=(ROOT/(RUNTIME+'Core/Game.cs')).read_text()
    statics=['TerritorySettings04B','TerritoryCatalog','TerritoryPlacement','TerritoryEcologyRules04B','TerritoryEventRules','LivingTerritories04BQa','ArtTerritories04B']
    check('New static lifecycle hooks are registered',all(name+'.ResetStatics();' in game for name in statics),statics)
    boot=(ROOT/(RUNTIME+'Core/GameRoot.cs')).read_text()
    old_courts=boot.index('VisualRebirth04ANeighborhoods.Build');new_sites=boot.index('Game.Territories.BuildWorld')
    check('04B survey starts after original forts and 04A courts',old_courts<new_sites)
    settings=(ROOT/(RUNTIME+'Gameplay/Territories/TerritorySettings04B.cs')).read_text()
    check('04B switches have separate preference namespace','NSLV_04B' in settings and 'NSLV_04A' not in settings)
    system=(ROOT/(RUNTIME+'Gameplay/Territories/TerritorySystem.cs')).read_text()
    check('Disabled visual layer still registers save system before BuildWorld return','WorldSystems.Register(this)' in system and system.index('WorldSystems.Register(this)')<system.index('if (!_enabled)'))
    art=ROOT/'Assets/NeonSyndicate/Resources/LivingTerritories04B'
    pointers=[];lfs=[]
    for path in art.rglob('*'):
        if path.suffix not in ('.bytes','.obj','.png','.wav','.blend'):continue
        if path.read_bytes().startswith(b'version https://git-lfs.github.com/spec/v1'):pointers.append(str(path.relative_to(ROOT)))
        relative=str(path.relative_to(ROOT));attribute=git('check-attr','filter','--',relative).decode().strip();lfs.append({'path':relative,'attribute':attribute})
    check('New runtime art uses real payload bytes',not pointers,pointers)
    check('New binary/model payloads match existing Git LFS tracking',all(row['attribute'].endswith(': lfs') for row in lfs),lfs)
    metadata=[]
    scopes=[art,ROOT/(RUNTIME+'Gameplay/Territories'),ROOT/(RUNTIME+'ArtTerritories04B')]
    for scope in scopes:
        for path in [scope,*scope.rglob('*')]:
            if path.suffix!='.meta' and not Path(str(path)+'.meta').is_file():metadata.append(str(path.relative_to(ROOT)))
    check('New territory runtime/resources have Unity metadata',not metadata,metadata)
    result={'scope':__doc__.strip(),'baseline':BASE,'checks':len(checks),'passed':sum(row['passed'] for row in checks),'failed':[row for row in checks if not row['passed']],
        'details':checks,'native_save_migration':'NOT RUN','native_gameplay':'NOT RUN'}
    args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='details'},indent=2))
    return 0 if not result['failed'] else 1
if __name__=='__main__':raise SystemExit(main())
