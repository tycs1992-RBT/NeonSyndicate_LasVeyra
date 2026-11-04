#!/usr/bin/env python3
"""Write Docs/Ascension04OH/EvidenceH9/manifest.json: matched before/after SOURCE RENDER pairs plus measured numbers.

Pairs come from the render-manifest.json files written by render_evidence_h9.py; a view is listed only when both the
before and the after image exist and were made with the same camera, pose and frame (one exception, labelled in the
pair: Dante's face view keeps the same lens and camera offset from each model's own head). Also records LOD totals from
the export stats and the garment audit summary. Nothing here is a Unity screenshot or an acceptance result.
Usage: python3 tools/evidence_manifest_h9.py [--root <repo>]
"""
import argparse, hashlib, json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
a = p.parse_args()
root = a.root.resolve()
ev = root / 'Docs/Ascension04OH/EvidenceH9'
HEAD_RELATIVE = {('Dante', 'face_three_quarter')}
out = {'tag': 'SOURCE RENDER', 'notice': 'Blender Cycles renders of the editable sources with identical camera, lights, '
       'pose and frame for each pair. Not Unity screenshots, not native import, not owner approval.',
       'native_unity': 'NOT RUN', 'owner_approval': 'NOT GIVEN', 'sets': {}}
for subject in ('Malik', 'Dante'):
    b = ev / 'SourceBefore' / subject / 'render-manifest.json'
    f = ev / 'SourceAfter' / subject / 'render-manifest.json'
    if not b.exists() or not f.exists():
        continue
    mb, ma = json.loads(b.read_text()), json.loads(f.read_text())
    vb = {v['view']: v for v in mb['views']}
    va = {v['view']: v for v in ma['views']}
    pairs = []
    for name in sorted(set(vb) & set(va)):
        x, y = vb[name], va[name]
        same = all(x.get(k) == y.get(k) for k in ('authored_action', 'action_frame', 'synthetic_deformation_pose',
                                                   'lens_mm', 'ortho_scale'))
        framing = 'identical camera and target'
        if x.get('camera') != y.get('camera') or x.get('target') != y.get('target'):
            # Only a head-relative face view may differ: same lens and the same camera offset from each model's own
            # head height (the H9 refit raised Dante's head by about 9.5 cm, a fixed camera would crop it).
            ox = [c - t for c, t in zip(x['camera'], x['target'])]
            oy = [c - t for c, t in zip(y['camera'], y['target'])]
            same = same and (subject, name) in HEAD_RELATIVE and all(abs(p - q) < 1e-3 for p, q in zip(ox, oy))
            framing = 'head-relative: same lens and the same camera offset from each model\'s own head'
        before = ev / 'SourceBefore' / subject / x['file']
        after = ev / 'SourceAfter' / subject / y['file']
        if not same or not before.exists() or not after.exists():
            continue
        pairs.append({'view': name, 'before': str(before.relative_to(root)), 'after': str(after.relative_to(root)),
                      'framing': framing, 'authored_action': x['authored_action'], 'action_frame': x['action_frame'],
                      'synthetic_deformation_pose': x['synthetic_deformation_pose'],
                      'before_sha256': hashlib.sha256(before.read_bytes()).hexdigest(),
                      'after_sha256': hashlib.sha256(after.read_bytes()).hexdigest()})
    out['sets'][subject] = {'before_source': mb['source_blend'], 'before_source_sha256': mb['source_sha256'],
                            'after_source': ma['source_blend'], 'after_source_sha256': ma['source_sha256'],
                            'engine': ma['engine'], 'resolution': ma['resolution'], 'pairs': pairs}
stats = root / 'Assets/NeonSyndicate/Art/ProductionH9/Models/export-stats.json'
if stats.exists():
    s = json.loads(stats.read_text())
    out['malik_lod_totals'] = {'h8s': s.get('h8s_totals'), 'h9': s.get('totals')}
dstats = root / 'Assets/NeonSyndicate/Art/ProductionH9/NPC/export-stats.json'
if dstats.exists():
    s = json.loads(dstats.read_text())
    out['dante_lod_totals'] = {'h8s': s.get('h8s_lods'), 'h9': s.get('lod_summary')}
audit = {}
for tag in ('h8s', 'h9'):
    fa = ev / ('garment-audit-%s.json' % tag)
    if not fa.exists():
        continue
    d = json.loads(fa.read_text())
    rows = [r for v in d['actions'].values() for r in v]
    audit[tag] = {'sampled_frames': len(rows), 'actions': len(d['actions']),
                  'coat_worst_mm': max(r['coat_worst_mm'] for r in rows), 'coat_pokes_max': max(r['coat_pokes'] for r in rows),
                  'skirt_worst_mm': max(r['skirt_worst_mm'] for r in rows),
                  'collar_min_clearance_mm': min(r['collar_min_mm_and_top_gap_mm'][0] for r in rows),
                  'collar_top_gap_max_mm': max(r['collar_min_mm_and_top_gap_mm'][1] for r in rows)}
if audit:
    out['garment_audit'] = {'method': 'tools/audit_garment_h9.py: body vertices tracked against their rest attachment on the '
                            'garment through 5 frames of each of the 17 actions; collar measured on its visible part',
                            'results': audit}
(ev / 'manifest.json').write_text(json.dumps(out, indent=2) + '\n')
print(json.dumps({k: len(v['pairs']) for k, v in out['sets'].items()}))
