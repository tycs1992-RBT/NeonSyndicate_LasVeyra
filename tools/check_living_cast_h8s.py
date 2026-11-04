#!/usr/bin/env python3
"""H8S living cast guard: frozen owners, the Hearth Witness loop, curated motion lineage, candidate receipts, no shipped
approval or prepared copy, no new world revision, dash-free changed text and LFS for changed binaries.
Source and asset evidence only. NOT native Unity import, play, save, input or visual acceptance."""
import argparse, hashlib, json, re, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = 'ae5003fc2b3b4e5754c00b95aaaaa97169b8ff3c'
RUNTIME = 'Assets/NeonSyndicate/Scripts/Runtime/'
FROZEN = ['Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/GraphicsSettings.asset',
          'ProjectSettings/QualitySettings.asset'] + [RUNTIME + p for p in [
    'Core/Game.cs', 'Core/CityRebirthBoot04O.cs', 'Characters/PlayerController.cs', 'Characters/PlayerCombat.cs',
    'Characters/PlayerCamera.cs', 'Characters/HumanoidRig.cs', 'Characters/HumanoidAnimator.cs',
    'Characters/HumanoidPose.cs', 'Characters/BodyBuilder.cs', 'Characters/ImportedVisual04OD.cs',
    'Characters/ImportedModelRegistry04OD.cs', 'Characters/ImportedArt04OH.cs', 'Characters/ImportedBindH3.cs',
    'Combat/WeaponController.cs', 'Gameplay/SaveSystem.cs', 'Gameplay/StoryMissions.cs',
    'Gameplay/Bonds/BondSystem.Resonance04N.cs', 'Gameplay/RegionalArsenal04OE.cs', 'Gameplay/ForgeTreatments04M.cs',
    'Gameplay/Quests/QuestRunner.cs', 'Gameplay/Quests/QuestValidator.cs', 'World/CityLayout.Rebirth04O.cs']]
# Owner decision 2026-10-23 (in chat): one unified game. The title, boot world and story start move to revision 10;
# MenuSystem.cs is intentionally edited for that and is no longer byte-frozen here.
HEARTH = 'Assets/NeonSyndicate/Resources/Quests/ascension_h3_hearth_witness.json'
CURATED = 'Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json'
CURATED_SOURCE = 'ArtSource/AscensionH8S/MalikVeyrH8S.blend'
CLIPS = ['H7_IdleBreath', 'H7_Walk', 'H7_Run', 'H7_SwordReady', 'H7_OathCut', 'H8_AegisLink', 'H8_CrystalCast', 'H8_Dodge',
         'H8_FireAim', 'H8_FireRecoil', 'H8_FlightLanding', 'H8_FlightTakeoff', 'H8_Guard_Brace', 'H8_JumpLanding',
         'H8_Landing_Supplied', 'H8_Listening_Supplied', 'H8_SurgeRise']
RECEIPTS = ['Assets/NeonSyndicate/Art/ProductionH8S/candidate.json', 'Assets/NeonSyndicate/Art/ProductionH8S/NPC/candidate.json',
            'Assets/NeonSyndicate/Art/ProductionH8S/Props/candidate.json', 'Assets/NeonSyndicate/Art/ProductionH9/candidate.json',
            'Assets/NeonSyndicate/Art/ProductionH9/NPC/candidate.json']
MOTION_CODE = ['Characters/MalikMotionH8.cs', 'Characters/HumanoidPose.MalikH8.cs', 'Characters/HumanoidPose.LandingH8S.cs',
               'Characters/HumanoidPose.QuietH8S.cs', 'Characters/HumanoidAnimator.MalikH8.cs', 'Characters/HumanoidAnimator.QuietH8S.cs',
               'Characters/LandingTriggerH8S.cs', 'Characters/QuietMotionH8S.cs']
FORBIDDEN = ['TakeDamage(', 'Spend(', 'SetCinematic(', 'timeScale=', 'timeScale =', 'Input.', 'SaveSystem.', 'AnimatorController',
             'AnimationEvent', 'transform.position=', 'transform.position =', 'PlayerPrefs.']
BINARY = ('.fbx', '.blend', '.png', '.jpg', '.jpeg', '.tga', '.psd', '.wav', '.ogg', '.mp3', '.exr')


def git(*args, text=True):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=text)


def sha(path):
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    checks = []

    def check(name, ok, detail=''):
        checks.append({'name': name, 'passed': bool(ok), 'detail': detail})

    for path in FROZEN:
        check('Frozen original owner unchanged / ' + path, (ROOT / path).read_bytes() == git('show', BASE + ':' + path, text=False))

    hearth = json.loads((ROOT / HEARTH).read_text())
    check('Hearth Witness file byte-identical (id, phases, counts and rewards unchanged)',
          (ROOT / HEARTH).read_bytes() == git('show', BASE + ':' + HEARTH, text=False))
    text = json.dumps(hearth)
    check('Hearth Witness keeps its stable id and revision-9 requirement',
          hearth.get('Id') == 'fg_04oh_h3_hearth_witness' or 'fg_04oh_h3_hearth_witness' in text)

    shipped = [p for p in git('ls-tree', '-r', '--name-only', BASE, '--', 'Assets/NeonSyndicate/Resources/Quests',
                              'Assets/NeonSyndicate/Resources/Events').splitlines() if p.endswith('.json')]
    changed = [p for p in shipped if not (ROOT / p).exists() or (ROOT / p).read_bytes() != git('show', BASE + ':' + p, text=False)]
    check('Every shipped quest and event file is unchanged (saved progress remembers their steps)', not changed, ', '.join(changed))

    data = json.loads((ROOT / CURATED).read_text())
    check('Curated motion is schema 2, the selected human, no root motion or events',
          data['schema'] == 2 and data['assetId'] == 'MALIK-H8R-REVIEW-01' and not data['rootMotion'] and not data['events'])
    check('Curated motion has exactly the seventeen ordered clips', [c['name'] for c in data['clips']] == CLIPS)
    check('Curated motion lineage matches its Blender source', data['sourceSha256'] == sha(CURATED_SOURCE))
    h9 = 'Assets/NeonSyndicate/Art/ProductionH9/Animations/MalikFullBodyH9.json'
    h9_source = 'ArtSource/AscensionH9/MalikVeyrH9.blend'
    if (ROOT / h9).exists():
        d9 = json.loads((ROOT / h9).read_text())
        check('H9 motion is the same curated schema 2 set with its own H9 lineage',
              d9['schema'] == 2 and d9['assetId'] == 'MALIK-H8R-REVIEW-01' and not d9['rootMotion'] and not d9['events']
              and [c['name'] for c in d9['clips']] == CLIPS and d9['sourceSha256'] == sha(h9_source) and d9['sourceSha256'] != data['sourceSha256'])
    else:
        check('Optional H9 motion not delivered', True, 'absent')

    for receipt in RECEIPTS:
        if not (ROOT / receipt).exists():
            check('Optional receipt not delivered / ' + receipt, True, 'absent')
            continue
        r = json.loads((ROOT / receipt).read_text())
        files = r.get('files', [])
        bad = [f['path'] for f in files if not (ROOT / f['path']).is_file() or sha(f['path']) != f['sha256']]
        check('Receipt files exist and match their hashes / ' + receipt, files and not bad, ', '.join(bad))
        check('Candidate stays disabled and unapproved / ' + receipt, r.get('ownerApproved', False) is False and r.get('enabled', False) is False)

    prepared = [p for p in git('ls-files', 'Assets/NeonSyndicate/Art').splitlines() if '/Prepared/' in p]
    check('No prepared review copy is committed', not prepared, ', '.join(prepared[:5]))
    check('No enabled imported-model registry is shipped', not (ROOT / 'Assets/NeonSyndicate/Resources/ImportedModels04OD.asset').exists())

    code = ''.join((ROOT / RUNTIME / p).read_text() for p in MOTION_CODE if (ROOT / RUNTIME / p).exists())
    hits = [t for t in FORBIDDEN if t in code]
    check('Authored motion adds no motor, damage, cost, camera, input, time, save or event writer', not hits, ', '.join(hits))
    pose = (ROOT / RUNTIME / 'Characters/HumanoidPose.cs').read_text()
    check('Authored body still feeds the original feet, smoothing and weapon IK',
          pose.index('AuthoredBodyH8(ref p,ref b,dt);') < pose.index('Feet(ref p, ref b, dt, rootRot, invRoot);'))

    enum = re.search(r'enum CityRevision04O \{([^}]*)\}', (ROOT / RUNTIME / 'World/CityLayout.Rebirth04O.cs').read_text()).group(1)
    values = [int(v) for v in re.findall(r'=\s*(\d+)', enum)]
    check('No world revision 11 or later exists', max(values) == 10)

    diff = git('diff', '--name-only', '--diff-filter=AM', BASE, '--').splitlines()
    untracked = git('ls-files', '--others', '--exclude-standard').splitlines()
    touched = sorted(set(diff + untracked))
    dashed = []
    for p in touched:
        if p.lower().endswith(BINARY) or not (ROOT / p).is_file():
            continue
        try:
            body = (ROOT / p).read_text(encoding='utf-8')
        except UnicodeDecodeError:
            continue
        if chr(0x2013) in body or chr(0x2014) in body:
            dashed.append(p)
    check('No em-dash or en-dash in any added or changed text file', not dashed, ', '.join(dashed))

    lfs = set(git('lfs', 'ls-files', '--name-only').splitlines())
    binaries = [p for p in diff if p.lower().endswith(BINARY)]
    not_lfs = [p for p in binaries if p not in lfs]
    check('Every added or changed binary is stored in Git LFS', not not_lfs, ', '.join(not_lfs[:8]))

    missing_meta = [p for p in touched if p.startswith('Assets/') and not p.endswith('.meta') and not (ROOT / (p + '.meta')).exists()]
    check('Every added Assets file has a Unity .meta', not missing_meta, ', '.join(missing_meta[:8]))

    report = {'baseline': BASE, 'scope': __doc__, 'passed': sum(c['passed'] for c in checks), 'total': len(checks),
              'checks': checks, 'native_unity': 'NOT RUN', 'owner_approval': 'NOT GIVEN'}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'passed': report['passed'], 'total': report['total'], 'failed': [c['name'] for c in checks if not c['passed']]}))
    return int(report['passed'] != report['total'])


if __name__ == '__main__':
    raise SystemExit(main())
