#!/usr/bin/env python3
"""L1 preservation and shipped-asset integrity. Does not run Unity or establish visual acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BASE = 'dde58fa18e4bce0fdd84b65566a7681cc26f9b25'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    rows = []
    def check(name, value): rows.append({'name':name,'passed':bool(value)})
    def old(path): return subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT)
    runtime='Assets/NeonSyndicate/Scripts/Runtime/'
    owners=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset']
    owners += [runtime+p for p in ['Core/Game.cs','Core/GameRoot.cs','Core/CityRebirthBoot04O.cs','Characters/CharacterBase.cs',
        'Characters/PlayerController.cs','Characters/PlayerCombat.cs','Characters/PlayerCamera.cs','Characters/LiveCastRegistry.cs',
        'Characters/LiveCastVisual.cs','Characters/LiveCrowd.cs','AI/PedManager.cs','Gameplay/SaveSystem.cs','Gameplay/StoryMissions.cs',
        'Gameplay/Bonds/BondSystem.cs','Gameplay/CraftingSystem.cs','Combat/WeaponController.cs',
        'World/Wilds/WildsSystem.cs','World/Wilds/WildsLayout.cs','World/Wilds/WildsSites.cs','World/Wilds/WildsShapes.cs',
        'World/Wilds/WildsTypes.cs','World/Wilds/WildsMap.cs']]
    for path in owners: check('Original owner byte-identical: '+path, (ROOT/path).read_bytes()==old(path))
    for prefix in ['Assets/NeonSyndicate/Art/LiveCast','Assets/NeonSyndicate/Art/ProductionH9','Assets/NeonSyndicate/Resources/Quests','Assets/NeonSyndicate/Resources/Events']:
        changed=subprocess.check_output(['git','diff','--name-only',BASE,'--',prefix],cwd=ROOT).strip()
        check('Original content unchanged: '+prefix,not changed)
    manifest=json.loads((ROOT/'Docs/LiveWildL1/asset-integrity.json').read_text())
    for row in manifest['files']:
        path=ROOT/row['path']
        actual=hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else 'missing'
        check('Hydrated original L1 delivery hash: '+row['path'],actual==row['sha256'] and path.stat().st_size==row['bytes'])
    check('Real assets, source and both source renders delivered',len(manifest['files'])==17)
    validation=json.loads((ROOT/'Docs/LiveWildL1/asset-validation.json').read_text())
    check('Executed Blender re-import and proxy checks retained',validation['passed']==38 and validation['failed']==0 and validation['native_unity']=='NOT RUN')
    independent=json.loads((ROOT/'Docs/LiveWildL1/independent-fbx.json').read_text())
    check('Strict independent static FBX inspection passed',independent['passed'] and independent['bones']==0 and independent['clips']==0)
    placement=(ROOT/'Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion/placement.txt').read_text()
    check('Shipped normal-play site, collision and bounded clearing',all(s in placement for s in ['near Wild:Mirrorwell Oasis offset 34 0','collide mesh','clear 10']) and '\nskip' not in placement)
    report={'scope':__doc__,'baseline':BASE,'checks':rows,'passed':sum(r['passed']for r in rows),'total':len(rows),'native_unity':'NOT RUN'}
    args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'passed':report['passed'],'total':report['total'],'failed':[r['name']for r in rows if not r['passed']]}))
    return int(any(not r['passed']for r in rows))

if __name__=='__main__':raise SystemExit(main())
