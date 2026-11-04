"""Check player-height landmark visibility against the rendered triangles, including retained rooms."""
import argparse
import json
from pathlib import Path

import numpy as np

from check_science_circuit_walk import meshes
from render_science_landmarks import CAMERAS


def first_surface(parts, eye, target):
    direction = target-eye
    direction /= np.linalg.norm(direction)
    nearest, role = float('inf'), None
    for name, positions, edges, diagonals in parts:
        p = np.cross(direction, diagonals)
        det = np.sum(edges*p, axis=1)
        valid = abs(det) > 1e-8
        det[~valid] = 1
        offset = eye-positions
        u = np.sum(offset*p, axis=1)/det
        q = np.cross(offset, edges)
        v = np.sum(direction*q, axis=1)/det
        distance = np.sum(diagonals*q, axis=1)/det
        valid &= (u >= 0) & (v >= 0) & (u+v <= 1) & (distance > .01)
        hit = distance[valid].min(initial=np.inf)
        if hit < nearest:
            nearest, role = hit, name
    return role, eye+direction*nearest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('room_export', type=Path)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    parts = []
    for name, vertices, indices in meshes(args.room_export/'circuit.bin'):
        tris = vertices[:, :3][indices]
        parts.append((name, tris[:, 0], tris[:, 1]-tris[:, 0], tris[:, 2]-tris[:, 0]))
    samples = [
        ('home', 'west glazing', (-3.4, 12.93, -37.38), 'amber-glass', .06),
        ('home', 'east glazing', (-5.43, 12.93, -37.38), 'amber-glass', .06),
        ('home', 'bench back', (-4.3, 10.58, -33.73), 'root', .07),
        ('court', 'open crown rim', (80.1, 16.09, 106), 'iron', .4),
        ('workshop', 'service lens', (80.1, 12.61, 107.11), 'forge-lens', .1),
        ('return', 'middle shelf', (-51.46, 2.85, 44.9), 'basalt', .12),
        ('return', 'far Archive roof', (-4.23, 15.51, 152.61), 'slate', .16),
    ]
    report = []
    for camera, label, target, expected, tolerance in samples:
        eye = np.asarray(CAMERAS[camera][1], dtype=float)
        target = np.array(target, dtype=float)
        role, point = first_surface(parts, eye, target)
        error = float(np.linalg.norm(point-target))
        report.append({'camera': camera, 'feature': label, 'expected_role': expected, 'first_role': role,
                       'target_world': target.tolist(), 'hit_world': np.round(point, 3).tolist(),
                       'passed': role == expected and error < tolerance})
    out = {'scope': 'Software-camera ray to the first two-sided exported triangle. Includes retained 04C rooms. Native occlusion untested.', 'samples': report}
    args.report.write_text(json.dumps(out, indent=2)+'\n')
    print(json.dumps([(r['feature'], r['passed']) for r in report]))
    return int(not all(r['passed'] for r in report))


if __name__ == '__main__':
    raise SystemExit(main())
