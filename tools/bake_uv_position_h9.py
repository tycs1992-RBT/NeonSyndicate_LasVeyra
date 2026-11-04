#!/usr/bin/env python3
"""Rasterise the selected body into its skin UV space: per-texel bind-pose position, smooth normal and coverage.

Run inside Blender's Python. The output .npz feeds make_skin_textures_h9.py, which paints the H9 skin layers by 3D
region (scalp, undercut fade, beard, lips, lid margin) so the painted texture agrees with the groom geometry.
Only faces above the chest are rasterised; other texels keep the original skin unchanged.
Usage: python tools/bake_uv_position_h9.py -- --blend <MalikVeyrH8S.blend> --out <file.npz> [--size 2048]
"""
import argparse, sys
import numpy as np
import bpy

p = argparse.ArgumentParser()
p.add_argument('--blend', required=True)
p.add_argument('--out', required=True)
p.add_argument('--size', type=int, default=2048)
p.add_argument('--zmin', type=float, default=1.30)
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
bpy.ops.wm.open_mainfile(filepath=a.blend, load_ui=False, use_scripts=False)
body = bpy.data.objects['MalikBodyH8R']
me = body.data
me.calc_loop_triangles()
S = a.size
V = np.array([v.co[:] for v in me.vertices], dtype=np.float64)
Nv = np.array([v.normal[:] for v in me.vertices], dtype=np.float64)
uvl = me.uv_layers.active.data
pos = np.zeros((S, S, 3), dtype=np.float32)
nrm = np.zeros((S, S, 3), dtype=np.float32)
cov = np.zeros((S, S), dtype=np.uint8)
count = 0
for tri in me.loop_triangles:
    vi = tri.vertices
    if max(V[i][2] for i in vi) < a.zmin:
        continue
    uv = np.array([uvl[li].uv[:] for li in tri.loops], dtype=np.float64)
    # texel centres: x = u*S - 0.5, y (row from top) = (1 - v)*S - 0.5
    px = uv[:, 0] * S - 0.5
    py = (1.0 - uv[:, 1]) * S - 0.5
    x0, x1 = int(np.floor(px.min())) - 1, int(np.ceil(px.max())) + 1
    y0, y1 = int(np.floor(py.min())) - 1, int(np.ceil(py.max())) + 1
    x0, y0 = max(x0, 0), max(y0, 0)
    x1, y1 = min(x1, S - 1), min(y1, S - 1)
    if x1 < x0 or y1 < y0:
        continue
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
    d = (py[1] - py[2]) * (px[0] - px[2]) + (px[2] - px[1]) * (py[0] - py[2])
    if abs(d) < 1e-12:
        continue
    w0 = ((py[1] - py[2]) * (gx - px[2]) + (px[2] - px[1]) * (gy - py[2])) / d
    w1 = ((py[2] - py[0]) * (gx - px[2]) + (px[0] - px[2]) * (gy - py[2])) / d
    w2 = 1.0 - w0 - w1
    eps = -0.02  # slight conservative growth so neighbouring triangles leave no unfilled cracks
    inside = (w0 >= eps) & (w1 >= eps) & (w2 >= eps)
    if not inside.any():
        continue
    W = np.stack([w0[inside], w1[inside], w2[inside]], axis=-1)
    P = W @ V[list(vi)]
    Nn = W @ Nv[list(vi)]
    Nn /= np.maximum(np.linalg.norm(Nn, axis=-1, keepdims=True), 1e-9)
    ys, xs = gy[inside], gx[inside]
    pos[ys, xs] = P
    nrm[ys, xs] = Nn
    cov[ys, xs] = 1
    count += 1
np.savez_compressed(a.out, pos=pos, nrm=nrm, cov=cov)
print('rasterised triangles', count, 'covered texels', int(cov.sum()), flush=True)
