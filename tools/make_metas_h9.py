#!/usr/bin/env python3
"""Create Unity .meta files for new H9 art: copy the matching H8S .meta (same folder role and file kind) and give it a
fresh random GUID. Existing .meta files and GUIDs are never changed.
Usage: python3 tools/make_metas_h9.py [--root <repo>]
"""
import argparse, re, uuid
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
a = p.parse_args()
root = a.root.resolve()
h9 = root / 'Assets/NeonSyndicate/Art/ProductionH9'
h8s = root / 'Assets/NeonSyndicate/Art/ProductionH8S'


def template(path):
    rel = path.relative_to(h9) if path != h9 else Path('.')
    cand = h8s / str(rel).replace('H9', 'H8S')
    if path == h9:
        cand = h8s
    meta = Path(str(cand) + '.meta')
    if meta.exists():
        return meta.read_text()
    if path.is_dir():
        return (h8s / 'Models.meta').read_text()
    sib = sorted((h8s / rel.parent).glob('*' + path.suffix + '.meta')) if (h8s / rel.parent).exists() else []
    sib = sib or sorted(h8s.rglob('*' + path.suffix + '.meta'))
    return sib[0].read_text()


made = []
for path in [h9] + sorted(h9.rglob('*')):
    if path.suffix == '.meta':
        continue
    meta = Path(str(path) + '.meta')
    if meta.exists():
        continue
    text = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, template(path))
    meta.write_text(text)
    made.append(str(meta.relative_to(root)))
print('\n'.join(made))
