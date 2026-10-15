#!/usr/bin/env python3
"""Verify staged/tracked delivery LFS bytes without asserting unrelated archives."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PREFIXES = ('Assets/', 'Docs/VisualRebirth04A/', 'Docs/LivingTerritories04B/')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path,
                        default=ROOT / 'Docs/LivingTerritories04B/Validation/lfs_payloads.json')
    args = parser.parse_args()
    listing = subprocess.check_output(['git', 'lfs', 'ls-files', '--long'], cwd=ROOT, text=True)
    entries = []
    failed = []
    for line in listing.splitlines():
        expected, marker, name = line.split(' ', 2)
        if not name.startswith(PREFIXES):
            continue
        path = ROOT / name
        payload = path.read_bytes() if path.is_file() else b''
        actual = hashlib.sha256(payload).hexdigest()
        matches = bool(payload) and actual == expected and marker == '*'
        entries.append({'path': name, 'sha256': expected, 'bytes': len(payload), 'matches': matches})
        if not matches:
            failed.append(name)
    if not entries:
        failed.append('No scoped LFS entries found')
    report = {
        'scope': 'Current staged/tracked Unity asset payloads and retained 04A/04B delivery evidence. '
                 'Other historical archives/screenshots outside these prefixes are not asserted. '
                 'This local hash check does not prove remote availability; verify successful Git/LFS push separately.',
        'prefixes': PREFIXES,
        'inventory_source': 'git lfs ls-files --long (current index and working tree)',
        'checked': len(entries),
        'matched': sum(entry['matches'] for entry in entries),
        'bytes': sum(entry['bytes'] for entry in entries),
        'failed': failed,
        'entries': entries,
    }
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: report[key] for key in ('checked', 'matched', 'bytes', 'failed')}))
    return 1 if failed else 0


if __name__ == '__main__':
    raise SystemExit(main())
