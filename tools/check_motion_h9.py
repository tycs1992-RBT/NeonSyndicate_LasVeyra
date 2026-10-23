#!/usr/bin/env python3
"""Check MalikFullBodyH9.json against the curated schema 2 contract and against the H8S export it must match.

Contract (integration owner): assetId MALIK-H8R-REVIEW-01, exactly the 17 clips in the H8S order, loop flags true
only for the first four clips, H8_FireAim and H8_Listening_Supplied, samples at exactly 30 Hz
(count = round(duration * 30) + 1, t_k = k / 30 within 0.002 s), every frame 17 positions and 17 unit quaternions,
joint step at most 0.35 m and 100 degrees between samples, landing 1.2667 +/- 0.05 s, listening 2.5 +/- 0.01 s,
loops closed, rootMotion false, events false. The H9 rig and actions are unchanged, so every sample must equal
the H8S sample and only sourceSha256 may differ.
"""
import argparse, hashlib, json, math, sys
from pathlib import Path

ORDER = ['H7_IdleBreath', 'H7_Walk', 'H7_Run', 'H7_SwordReady', 'H7_OathCut', 'H8_AegisLink', 'H8_CrystalCast',
         'H8_Dodge', 'H8_FireAim', 'H8_FireRecoil', 'H8_FlightLanding', 'H8_FlightTakeoff', 'H8_Guard_Brace',
         'H8_JumpLanding', 'H8_Landing_Supplied', 'H8_Listening_Supplied', 'H8_SurgeRise']
LOOPS = {'H7_IdleBreath', 'H7_Walk', 'H7_Run', 'H7_SwordReady', 'H8_FireAim', 'H8_Listening_Supplied'}


def qangle(a, b):
    d = abs(a['x'] * b['x'] + a['y'] * b['y'] + a['z'] * b['z'] + a['w'] * b['w'])
    return math.degrees(2 * math.acos(min(1.0, d)))


def dist(a, b):
    return math.sqrt((a['x'] - b['x']) ** 2 + (a['y'] - b['y']) ** 2 + (a['z'] - b['z']) ** 2)


def main():
    root = Path(__file__).resolve().parents[1]
    p = argparse.ArgumentParser()
    p.add_argument('--json', type=Path, default=root / 'Assets/NeonSyndicate/Art/ProductionH9/Animations/MalikFullBodyH9.json')
    p.add_argument('--blend', type=Path, default=root / 'ArtSource/AscensionH9/MalikVeyrH9.blend')
    p.add_argument('--reference', type=Path, default=root / 'Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json',
                   help='MalikFullBodyH8S.json')
    a = p.parse_args()
    d = json.loads(a.json.read_text())
    ref = json.loads(a.reference.read_text())
    errors, report = [], {}

    def need(cond, msg):
        if not cond:
            errors.append(msg)
    need(d.get('schema') == 2, 'schema must be 2')
    need(d.get('assetId') == 'MALIK-H8R-REVIEW-01', 'assetId')
    need(d.get('rootMotion') is False and d.get('events') is False, 'rootMotion/events must be false')
    need(d.get('space') == ref.get('space'), 'space string differs from H8S')
    need(d.get('sourceSha256') == hashlib.sha256(a.blend.read_bytes()).hexdigest(), 'sourceSha256 is not the H9 blend hash')
    need(len(d.get('bind', [])) == 17 and d['bind'] == ref['bind'], 'bind differs from H8S')
    need(sorted(d.keys()) == sorted(ref.keys()), 'top-level keys differ from H8S')
    names = [c['name'] for c in d.get('clips', [])]
    need(names == ORDER, 'clip order/names differ: %s' % names)
    worst_step_m, worst_step_deg, identical = 0.0, 0.0, True
    for c, rc in zip(d.get('clips', []), ref.get('clips', [])):
        name = c['name']
        need(sorted(c.keys()) == sorted(rc.keys()), name + ': clip keys differ')
        need(c['loop'] == (name in LOOPS), name + ': loop flag')
        n = len(c['samples'])
        need(n == round(c['duration'] * 30) + 1, name + ': sample count %d for duration %.4f' % (n, c['duration']))
        for k, s in enumerate(c['samples']):
            if abs(s['t'] - k / 30.0) > 0.002:
                errors.append('%s: t[%d]=%.4f' % (name, k, s['t'])); break
            if len(s['p']) != 17 or len(s['q']) != 17:
                errors.append('%s: frame %d lacks 17 joints' % (name, k)); break
            for q in s['q']:
                ln = math.sqrt(q['x'] ** 2 + q['y'] ** 2 + q['z'] ** 2 + q['w'] ** 2)
                if abs(ln - 1) > 1e-3:
                    errors.append('%s: non-unit quaternion at frame %d' % (name, k)); break
            if k:
                prev = c['samples'][k - 1]
                for j in range(17):
                    worst_step_m = max(worst_step_m, dist(s['p'][j], prev['p'][j]))
                    worst_step_deg = max(worst_step_deg, qangle(s['q'][j], prev['q'][j]))
        if c['loop']:
            f, l = c['samples'][0], c['samples'][-1]
            gap_m = max(dist(f['p'][j], l['p'][j]) for j in range(17))
            gap_deg = max(qangle(f['q'][j], l['q'][j]) for j in range(17))
            report[name + '_loop_gap'] = [round(gap_m, 5), round(gap_deg, 3)]
        if c != rc:
            identical = False
            errors.append(name + ': clip data differs from H8S')
    landing = next((c for c in d['clips'] if c['name'] == 'H8_Landing_Supplied'), None)
    listening = next((c for c in d['clips'] if c['name'] == 'H8_Listening_Supplied'), None)
    need(landing is not None and abs(landing['duration'] - 1.2667) <= 0.05, 'landing duration')
    need(listening is not None and abs(listening['duration'] - 2.5) <= 0.01, 'listening duration')
    need(worst_step_m <= 0.35, 'joint step %.3f m exceeds 0.35' % worst_step_m)
    need(worst_step_deg <= 100, 'joint step %.1f deg exceeds 100' % worst_step_deg)
    report.update({'clips': len(names), 'samples': sum(len(c['samples']) for c in d['clips']),
                   'durations': {c['name']: round(c['duration'], 4) for c in d['clips']},
                   'worst_step_m': round(worst_step_m, 4), 'worst_step_deg': round(worst_step_deg, 2),
                   'identical_to_h8s_except_source_hash': identical, 'errors': errors})
    print(json.dumps(report, indent=1))
    sys.exit(1 if errors else 0)


if __name__ == '__main__':
    main()
