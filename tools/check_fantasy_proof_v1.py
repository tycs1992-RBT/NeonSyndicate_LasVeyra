#!/usr/bin/env python3
"""FR-V1 source wiring, privacy exclusions and exact owner preservation. Never native acceptance."""
import argparse
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = '57975b3d50a85bccd52cbcd07d00af1a7df25377'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    rows = []

    def check(name, passed):
        rows.append({'name': name, 'passed': bool(passed)})

    runtime = 'Assets/NeonSyndicate/Scripts/Runtime/'
    trees = ['Assets/NeonSyndicate/Art', 'ArtSource', 'Packages', 'ProjectSettings',
             'Assets/NeonSyndicate/Resources/Quests', 'Assets/NeonSyndicate/Resources/Events']
    trees += [runtime + name for name in ['Core', 'Gameplay', 'AI', 'Combat', 'Creatures', 'Vehicles', 'Characters']]
    for tree in trees:
        delta = subprocess.check_output(['git', 'diff', BASE, '--', tree], cwd=ROOT)
        check('Frozen original tree: ' + tree, not delta.strip())
    for name in ['CityBuilder.cs', 'CityBuilder.Gothic04OB.cs', 'CityBuilder.HeartFR01.cs',
                 'CityBuilder.GroundL3.cs', 'CityDressing.Frost.cs', 'GothicHeartH3.cs', 'GothicCivicH5.cs']:
        path = runtime + 'World/' + name
        check('Frozen world owner: ' + name, (ROOT / path).read_bytes() == subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT))
    tests = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', BASE, 'Assets/NeonSyndicate/Tests'], cwd=ROOT, text=True).splitlines()
    check('Every inherited test byte preserved', all((ROOT / path).read_bytes() == subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT) for path in tests))
    for path in ['UserCaptures/FantasyWorldProof/example.png', 'UserBackups/FantasyWorldProof/example.json',
                 'Assets/NeonSyndicate/Resources/FantasyBuildV1.json', 'Assets/NeonSyndicate/Resources/FantasyBuildV1.json.meta']:
        check('Local proof data ignored: ' + path, subprocess.run(['git', 'check-ignore', '-q', path], cwd=ROOT).returncode == 0)
    check('No local proof or saves tracked', not subprocess.check_output(['git', 'ls-files', '--', 'UserCaptures', 'UserBackups', 'Assets/NeonSyndicate/Resources/FantasyBuildV1.json'], cwd=ROOT).strip())
    session = (ROOT / (runtime + 'UI/VisibleRealityV0.FantasySessionV1.cs')).read_text()
    check('Existing ordinary Free Roam entry, no player relocation', 'Game.Root.NewGame(true)' in session and all(token not in session for token in ['PlacePlayer(', 'SetPositionAndRotation(', 'SaveSystem.Save(', 'PlayerPrefs.Set']))
    title = (ROOT / (runtime + 'UI/MenuSystem.cs')).read_text()
    check('Central title entry and existing gamepad button path', '"PLAY FANTASY WORLD"' in title and 'VisibleRealityV0.PlayFantasyWorld(out reason)' in title)
    proof = (ROOT / (runtime + 'QA/FantasyProofV1.cs')).read_text()
    check('Independent native truth DTO, not renderer-isVisible shortcut', all(token in proof for token in ['inCameraFrustumRenderers', 'policyActive', 'activeRenderers', 'characterAppearance = "UNVERIFIED"']) and 'renderer.isVisible' not in proof)
    editor = ''.join(p.read_text() for p in (ROOT / 'Assets/NeonSyndicate/Scripts/Editor').glob('FantasyAcceptanceV1*.cs'))
    check('Genuine local checkout stamp', 'GitRefReader04E.TryRead(ProjectRoot' in editor and 'stamp.gitCommit = sha.ToLowerInvariant()' in editor)
    check('Native rendered frame capture, separate from ordinary QA suspension', 'ScreenCapture.CaptureScreenshotAsTexture()' in editor and 'WaitForEndOfFrame' in editor and 'QaTools.Start' not in editor)
    check('Exact acceptance menu exposed', 'Neon Syndicate/Acceptance/Verify Fantasy World' in editor)
    check('No automatic screenshot publication', all(token not in editor for token in ['git add ', 'git push ', 'HttpClient', 'UnityWebRequest']))
    result = {'scope': __doc__, 'baseline': BASE, 'passed': sum(r['passed'] for r in rows), 'total': len(rows), 'checks': rows, 'native_unity': 'NOT RUN'}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'passed': result['passed'], 'total': len(rows), 'failed': [r['name'] for r in rows if not r['passed']]}))
    return int(result['passed'] != result['total'])


if __name__ == '__main__':
    raise SystemExit(main())
