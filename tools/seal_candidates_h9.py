#!/usr/bin/env python3
"""Write the H9 candidate receipts (same format as H8S) after export, motion export and validation.

Every exported file under the candidate folder is listed with path, sha256 and bytes (Unity .meta files and the
receipt itself excluded). The candidates stay disabled and unapproved; native Unity import is NOT RUN.
Usage: python3 tools/seal_candidates_h9.py [--root <repo>] [--which malik,dante]
"""
import argparse, hashlib, json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--which', default='malik,dante')
a = p.parse_args()
root = a.root.resolve()
base = root / 'Assets/NeonSyndicate/Art/ProductionH9'


def entry(f):
    return {'path': str(f.relative_to(root)), 'sha256': hashlib.sha256(f.read_bytes()).hexdigest(), 'bytes': f.stat().st_size}


def listing(folder, first, skip_dirs=()):
    files = [f for f in folder.rglob('*') if f.is_file() and f.suffix != '.meta' and f.name != 'candidate.json'
             and not any(part in skip_dirs for part in f.relative_to(folder).parts[:-1])]
    files.sort(key=lambda f: (f != first, str(f.relative_to(folder))))
    return [entry(f) for f in files]


if 'malik' in a.which:
    fbx = base / 'Models/MalikVeyrH9.fbx'
    record = {'assetId': 'MALIK-H8R-REVIEW-01',
              'creator': 'Owner selected CC0 human and skin; project fitted original wardrobe, binding and H9 groom, face and garment refinement',
              'license': 'CC0-1.0', 'commercialUse': True, 'publicSourceApproved': True, 'ownerApproved': False, 'enabled': False,
              'preserveInfluences': True, 'sourceFbx': str(fbx.relative_to(root)), 'authoringSource': 'ArtSource/AscensionH9/MalikVeyrH9.blend',
              'NATIVE_UNITY_IMPORT': 'NOT RUN', 'OWNER_ART_APPROVAL': 'NOT GIVEN', 'files': listing(base, fbx, skip_dirs=('NPC',))}
    (base / 'candidate.json').write_text(json.dumps(record, indent=2) + '\n')
    print('malik receipt files', len(record['files']))
if 'dante' in a.which:
    npc = base / 'NPC'
    fbx = npc / 'DanteRookH9.fbx'
    stats_file = npc / 'export-stats.json'
    stats = json.loads(stats_file.read_text()) if stats_file.exists() else {}
    record = {'assetId': 'DANTE-H8S-REVIEW-01', 'creator': 'CC0 distinct male study and project fitted work clothing, H9 anatomy, apron, tool belt and groom refinement',
              'license': 'CC0-1.0', 'commercialUse': True, 'publicSourceApproved': True, 'preserveInfluences': True,
              'ownerApproved': False, 'enabled': False, 'bones': stats.get('bones', 65), 'lods': stats.get('lod_summary', []),
              'sourceFbx': str(fbx.relative_to(root)), 'authoringSource': 'ArtSource/AscensionH9/DanteRookH9.blend',
              'NATIVE_UNITY_IMPORT': 'NOT RUN', 'OWNER_ART_APPROVAL': 'NOT GIVEN', 'native_unity': 'NOT RUN', 'files': listing(npc, fbx)}
    (npc / 'candidate.json').write_text(json.dumps(record, indent=2) + '\n')
    print('dante receipt files', len(record['files']))
