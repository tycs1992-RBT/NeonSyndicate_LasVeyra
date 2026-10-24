#!/usr/bin/env python3
"""Verify V0 preserves gameplay owners and hydrated active art. No native Unity execution."""
import argparse, hashlib, json, subprocess
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
BASE = 'a6b3f075f8555927836203fc387d823e04edf9ac'
R = 'Assets/NeonSyndicate/Scripts/Runtime/'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    rows = []
    def check(name, ok): rows.append({'name': name, 'passed': bool(ok)})
    for path in ['Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/GraphicsSettings.asset',
                 'ProjectSettings/QualitySettings.asset', R+'Gameplay/SaveSystem.cs', R+'Gameplay/StoryMissions.cs',
                 R+'Characters/PlayerController.cs', R+'Characters/PlayerCombat.cs', R+'Characters/PlayerCamera.cs',
                 R+'Combat/WeaponController.cs', R+'Gameplay/Bonds/BondSystem.cs', R+'Gameplay/CraftingSystem.cs',
                 R+'Core/CityRebirthBoot04O.cs']:
        check('Fetched original owner unchanged: '+path, git('show', BASE+':'+path) == (ROOT/path).read_bytes())
    check('All shipped quest and event data unchanged', not git('diff', '--name-only', BASE, '--',
        'Assets/NeonSyndicate/Resources/Quests', 'Assets/NeonSyndicate/Resources/Events').strip())
    hooks = [
        (R+'UI/MenuSystem.cs', '            VisibleRealityV0.DrawTitleOffer(W, H);\n'),
        (R+'Creatures/StarterHabitat.cs', '\n        /// <summary>The existing calm habitat\'s real location, available to ordinary exploration without spawning anything.</summary>\n        public bool VisitPoint(out Vector3 at)\n        {\n            at = _sites.Count > 0 ? _sites[0] : _anchor;\n            return _configured && _sites.Count > 0;\n        }\n')]
    for path, hook in hooks:
        current = (ROOT/path).read_text()
        check('Only authorized read-only V0 hook: '+path, current.count(hook) == 1 and current.replace(hook,'').encode() == git('show',BASE+':'+path))
    roots = ('Assets/NeonSyndicate/Art/LiveCast/', 'Assets/NeonSyndicate/Art/ProductionH9/',
             'Assets/NeonSyndicate/Art/ProductionH8R/NPC/', 'Assets/NeonSyndicate/Art/WorldFoundationH8R/',
             'Assets/NeonSyndicate/Art/ProductionH8S/Props/', 'Assets/NeonSyndicate/Art/ProductionH8S/Textures/')
    assets = []
    for line in git('lfs','ls-files','--long').decode().splitlines():
        oid, state, path = line.split(' ',2)
        if not path.startswith(roots): continue
        file = ROOT/path
        actual = hashlib.sha256(file.read_bytes()).hexdigest() if file.exists() else 'missing'
        assets.append({'path':path,'expected':oid,'actual':actual,'passed':actual == oid})
    check('Active art has hydrated bytes matching every LFS SHA256', bool(assets) and all(a['passed'] for a in assets))
    changed = git('diff','--name-only',BASE).decode().splitlines() + git('ls-files','--others','--exclude-standard').decode().splitlines()
    for path in sorted(set(changed)):
        file=ROOT/path
        if file.is_file() and file.suffix in ('.cs','.md','.py','.json','.meta'):
            try: text=file.read_text()
            except UnicodeDecodeError: continue
            check('Dash-free changed text: '+path, '\u2013' not in text and '\u2014' not in text)
    report={'scope':__doc__.strip(),'baseline':BASE,'checks':rows,'hydrated_active_assets':assets,
            'passed':sum(r['passed']for r in rows),'failed':sum(not r['passed']for r in rows),'native_unity':'NOT RUN'}
    args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:report[k]for k in ('baseline','passed','failed')}))
    print('Hydrated active art:',len(assets))
    for row in rows:
        if not row['passed']:print('FAIL:',row['name'])
    return int(report['failed'] != 0)
if __name__ == '__main__': raise SystemExit(main())
