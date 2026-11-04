"""Independently check exported recipe vertices and face centroids against the actual original collision triangles.

This is a geometric correspondence check. It cannot test Unity collider cooking, player contact or landing.
Run the managed Forge harness first, then pass its OBJ output directory. Requires numpy.
"""
import argparse
import json
from collections import defaultdict
from pathlib import Path
import numpy as np


def read_obj(path):
    vertices, faces = [], []
    for line in path.read_text().splitlines():
        if line.startswith('v '):
            vertices.append([float(v) for v in line.split()[1:]])
        elif line.startswith('f '):
            faces.append([int(v) - 1 for v in line.split()[1:]])
    return np.array(vertices), np.array(faces)


def check(directory):
    support, indices = read_obj(directory / 'forge-support.obj')
    near, faces = read_obj(directory / 'forge-near.obj')
    triangles = support[indices]
    points = np.unique(np.concatenate([near, near[faces].mean(axis=1)]), axis=0)
    cell_size = 2.0
    buckets = defaultdict(list)
    for index, tri in enumerate(triangles):
        lo = np.floor((tri.min(axis=0) - .023) / cell_size).astype(int)
        hi = np.floor((tri.max(axis=0) + .023) / cell_size).astype(int)
        for x in range(lo[0], hi[0] + 1):
            for y in range(lo[1], hi[1] + 1):
                for z in range(lo[2], hi[2] + 1):
                    buckets[x, y, z].append(index)
    samples = defaultdict(list)
    for index, p in enumerate(points):
        samples[tuple(np.floor(p / cell_size).astype(int))].append(index)
    failed, maximum = [], 0.0
    for key, point_ids in samples.items():
        t = triangles[buckets[key]]
        if len(t) == 0:
            failed.extend(point_ids)
            continue
        a, ab, ac = t[:, 0], t[:, 1] - t[:, 0], t[:, 2] - t[:, 0]
        n = np.cross(ab, ac)
        n /= np.linalg.norm(n, axis=1)[:, None]
        aa = np.einsum('ij,ij->i', ab, ab)
        cc = np.einsum('ij,ij->i', ac, ac)
        bc = np.einsum('ij,ij->i', ab, ac)
        denom = aa * cc - bc * bc
        for start in range(0, len(point_ids), 128):
            ids = point_ids[start:start + 128]
            delta = points[ids, None, :] - a[None, :, :]
            distances = np.abs(np.einsum('ptj,tj->pt', delta, n))
            dab = np.einsum('ptj,tj->pt', delta, ab)
            dac = np.einsum('ptj,tj->pt', delta, ac)
            u = (dab * cc - dac * bc) / denom
            v = (dac * aa - dab * bc) / denom
            inside = (u >= -.00002) & (v >= -.00002) & (u + v <= 1.00002)
            distances[~inside] = np.inf
            best = distances.min(axis=1)
            failed.extend(i for i, d in zip(ids, best) if d > .02205)
            finite = best[np.isfinite(best)]
            if len(finite):
                maximum = max(maximum, float(finite.max()))
    result = {'sampled_vertices_and_face_centroids': len(points), 'support_triangles': len(triangles),
              'unmatched_samples': len(failed), 'maximum_normal_distance_metres': maximum,
              'permitted_relief_metres': .022, 'roundoff_tolerance_metres': .00005,
              'native_collision': 'NOT RUN'}
    if failed:
        result['first_unmatched'] = points[failed[:12]].tolist()
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    result = check(args.directory)
    if args.report:
        args.report.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))
    raise SystemExit(bool(result['unmatched_samples']))
