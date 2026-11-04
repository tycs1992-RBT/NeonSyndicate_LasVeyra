"""Offline scene preview of the checked-in Vale scene, never a Unity capture or a gameplay screenshot.

It reads the scene file and its serialized mesh buffers, then shades every pixel with a CPU copy of
ValePalette.shader: the scene's directional sun with a soft shadow map (5x5 tent, strength and distance as set),
the trilight ambient projected to spherical harmonics like ShadeSH9, the world-space grain chosen by the vertex
surface code (triplanar, mipmapped), the warm-to-cool haze and the self-lit codes. Characters, when exported with
the offline harness, are shaded with the same sun and ambient but not with the character shader. Not reproduced:
Unity's exact shadow cascades, its anti-aliasing, any post effect, animation over time, the HUD and the audio.
"""
import argparse
import json
import math
import re
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import yaml
from assets import ART, ROOT, SCENE

SHOTS = ROOT/'Docs/ShardwakeVale/SplitCrown/cameras.json'
REVIEW = ROOT/'Docs/ShardwakeVale/SplitCrown/review-shots.json'
GRAIN = ART/'Materials/SurfaceGrain.png'
HIDDEN = ('Installed hearth lens', 'Restored furnace light', 'Removable hearth seed')
ATLAS = ART/'Materials/FoliageAtlas.png'


def _guid(path):
    meta = Path(str(path)+'.meta')
    return re.search(r'guid: (\w+)', meta.read_text())[1] if meta.exists() else None


FOLIAGE_GUID = _guid(ART/'Materials/ValeFoliage.mat')


def vec(v):
    return np.array([v[k] for k in 'xyz'], float)


def shader_defaults(shader='ValePalette'):
    """Float and colour defaults from the shader's Properties block, overridden by the material."""
    if not (ART/'Materials'/(shader+'.shader')).exists():
        return {}
    text = (ART/'Materials'/(shader+'.shader')).read_text()
    props = {}
    for name, value in re.findall(r'(_\w+) \("[^"]*",\s*(?:Range\([^)]*\)|\w+)\) = ([^\n]+)', text):
        value = value.strip()
        if value.startswith('('):
            props[name] = np.array([float(x) for x in value.strip('()').split(',')])
        elif re.match(r'^-?[\d.]+$', value):
            props[name] = float(value)
    mat = (ART/'Materials'/(shader+'.mat')).read_text()
    for name, value in re.findall(r'- (_\w+): ([-\d.]+)\n', mat):
        props[name] = float(value)
    for name, r, g, b, a in re.findall(r'- (_\w+): \{r: ([-\d.]+), g: ([-\d.]+), b: ([-\d.]+), a: ([-\d.]+)\}', mat):
        props[name] = np.array([float(r), float(g), float(b), float(a)])
    return props


def load(scene_path=SCENE):
    text = scene_path.read_text()
    docs = {}
    for head, section in zip(re.findall(r'^--- !u!(\d+) &(\d+)\n', text, re.M),
                             re.split(r'^--- !u!\d+ &\d+\n', text, flags=re.M)[1:]):
        kind, fid = map(int, head)
        docs[fid] = (kind, list(yaml.safe_load(section).values())[0])
    transforms = {fid: d for fid, (k, d) in docs.items() if k == 4}
    cache = {}

    def matrix(tid):
        if tid in cache:
            return cache[tid]
        tr = transforms[tid]
        q = tr['m_LocalRotation']
        x, y, z, w = (q[k] for k in 'xyzw')
        rot = np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)], [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                        [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])
        m = np.eye(4)
        m[:3, :3] = rot @ np.diag(vec(tr['m_LocalScale']))
        m[:3, 3] = vec(tr['m_LocalPosition'])
        parent = tr['m_Father']['fileID']
        cache[tid] = matrix(parent) @ m if parent else m
        return cache[tid]

    def active(tid):
        while tid:
            go = docs[transforms[tid]['m_GameObject']['fileID']][1]
            if not go.get('m_IsActive', 1):
                return False
            tid = transforms[tid]['m_Father']['fileID']
        return True

    meshes = {}
    for p in ART.rglob('*.asset'):
        g = re.search(r'guid: (\w+)', Path(str(p)+'.meta').read_text())[1]
        data = yaml.safe_load(re.split(r'^---[^\n]*\n', p.read_text(), flags=re.M)[1])['Mesh']
        raw = bytes.fromhex(str(data['m_VertexData']['_typelessdata']))
        has_uv = data['m_VertexData']['m_Channels'][4]['dimension'] == 2
        fields = [('f', '<f4', 6), ('c', 'u1', 4)] + ([('uv', '<f4', 2)] if has_uv else [])
        v = np.frombuffer(raw, np.dtype(fields))
        idx = np.frombuffer(bytes.fromhex(str(data['m_IndexBuffer'])), dtype='<u2').reshape(-1, 3)
        uv = v['uv'].astype(float) if has_uv else np.zeros((len(v), 2))
        meshes[g] = (v['f'][:, :3].astype(float), v['f'][:, 3:].astype(float), v['c'].astype(float), idx, uv)
    objects, parts, light, cam, ambient = {}, [], None, None, None
    for fid, (kind, data) in docs.items():
        if kind == 104:
            ambient = [np.array([data[k][c] for c in 'rgb']) for k in
                       ('m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor')]
        if kind != 1:
            continue
        ids = [row['component']['fileID'] for row in data['m_Component']]
        m = matrix(ids[0])
        objects[data['m_Name']] = (m, ids)
        if not active(ids[0]) or data['m_Name'] in HIDDEN:
            continue
        filt = rend = None
        for cid in ids[1:]:
            k, comp = docs[cid]
            if k == 20:
                cam = (m, comp)
            if k == 108 and comp.get('m_Type') == 1:
                light = dict(forward=m[:3, 2]/np.linalg.norm(m[:3, 2]),
                             color=np.array([comp['m_Color'][c] for c in 'rgb'])*comp['m_Intensity'],
                             shadows=comp['m_Shadows']['m_Type'], strength=comp['m_Shadows'].get('m_Strength', 1))
            if k == 33:
                filt = comp
            if k == 23:
                rend = comp
        if filt and rend and rend.get('m_Enabled', 1):
            v, n, c, idx, uv = meshes[filt['m_Mesh']['guid']]
            foliage = rend['m_Materials'][0]['guid'] == FOLIAGE_GUID
            parts.append(dict(name=data['m_Name'], v=v @ m[:3, :3].T + m[:3, 3], n=n @ np.linalg.inv(m[:3, :3]), c=c,
                              idx=idx, uv=uv, foliage=foliage, cast=rend.get('m_CastShadows', 1) != 0, actor=False))
    return dict(objects=objects, parts=parts, light=light, camera=cam, ambient=ambient)


def actor(path, m):
    raw = path.read_bytes()
    count = int.from_bytes(raw[:4], 'little')
    verts = np.frombuffer(raw, dtype='<f4', count=9*count, offset=4).reshape(-1, 9).astype(float)
    off = 4 + 36*count
    n = int.from_bytes(raw[off:off+4], 'little')
    faces = np.frombuffer(raw, dtype='<i4', count=n, offset=off+4).reshape(-1, 3)
    color = np.hstack([verts[:, 6:9], np.full((count, 1), 255.)])
    return dict(name=path.stem, v=verts[:, :3] @ m[:3, :3].T + m[:3, 3], n=verts[:, 3:6] @ np.linalg.inv(m[:3, :3]),
                c=color, idx=faces, uv=np.zeros((count, 2)), foliage=False, cast=True, actor=True)


# ---------------------------------------------------------------------------------------------- rasterisation
def raster(sx, sy, depth_at, tris, w, h, persp, uv=None, alpha=None, cutout=None, cut=.45):
    """Depth-tested triangle ids. sx, sy are pixel coordinates (pixel centres at .5); persp interpolates 1/z.
    Triangles flagged in cutout are drawn from both sides and lose the pixels where the atlas alpha is below cut."""
    zbuf = np.full((h, w), np.inf)
    ids = np.full((h, w), -1, np.int64)
    for t, (ia, ib, ic) in enumerate(tris):
        ax, ay, bx, by, cx, cy = sx[ia], sy[ia], sx[ib], sy[ib], sx[ic], sy[ic]
        den = (by-cy)*(ax-cx) + (cx-bx)*(ay-cy)
        cut_this = cutout is not None and cutout[t]
        if cut_this and abs(den) < 1e-12:
            continue
        if persp and not cut_this and den < 1e-9:
            continue
        if not persp and abs(den) < 1e-12:
            continue
        l = max(0, math.floor(min(ax, bx, cx))); r = min(w-1, math.ceil(max(ax, bx, cx)))
        tp = max(0, math.floor(min(ay, by, cy))); bt = min(h-1, math.ceil(max(ay, by, cy)))
        if l > r or tp > bt:
            continue
        Y, X = np.ogrid[tp+.5:bt+1.5, l+.5:r+1.5]
        a = ((by-cy)*(X-cx) + (cx-bx)*(Y-cy))/den
        b = ((cy-ay)*(X-cx) + (ax-cx)*(Y-cy))/den
        c = 1-a-b
        inside = (a >= 0) & (b >= 0) & (c >= 0)
        if not inside.any():
            continue
        za, zb, zc = depth_at[ia], depth_at[ib], depth_at[ic]
        d = 1/np.maximum(1e-9, a/za + b/zb + c/zc) if persp else a*za + b*zb + c*zc
        if cut_this:
            if persp:
                k = d
                u = (a*uv[ia, 0]/za + b*uv[ib, 0]/zb + c*uv[ic, 0]/zc)*k
                v = (a*uv[ia, 1]/za + b*uv[ib, 1]/zb + c*uv[ic, 1]/zc)*k
            else:
                u = a*uv[ia, 0] + b*uv[ib, 0] + c*uv[ic, 0]
                v = a*uv[ia, 1] + b*uv[ib, 1] + c*uv[ic, 1]
            size = alpha.shape[0]
            iu = np.clip((u*size).astype(int), 0, size-1)
            iv = np.clip(((1-v)*size).astype(int), 0, size-1)
            inside = inside & (alpha[iv, iu] >= cut)
        dest = zbuf[tp:bt+1, l:r+1]
        upd = inside & (d < dest)
        dest[upd] = d[upd]
        ids[tp:bt+1, l:r+1][upd] = t
    return zbuf, ids


def merge(parts):
    V, N, C, T, A, CAST, UV, F = [], [], [], [], [], [], [], []
    off = 0
    for p in parts:
        V.append(p['v']); N.append(p['n']); C.append(p['c']); UV.append(p['uv'])
        T.append(p['idx'].astype(np.int64)+off)
        A.append(np.full(len(p['idx']), p['actor'])); CAST.append(np.full(len(p['idx']), p['cast']))
        F.append(np.full(len(p['idx']), p['foliage']))
        off += len(p['v'])
    n = np.vstack(N)
    n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)
    return (np.vstack(V), n, np.vstack(C), np.vstack(T), np.concatenate(A), np.concatenate(CAST), np.vstack(UV),
            np.concatenate(F))


class ShadowMap:
    """Orthographic depth map from the sun over the walkable valley, sampled with a 5x5 tent like soft shadows."""

    def __init__(self, V, T, forward, centre, extent=230., texel=.055, uv=None, alpha=None, cutout=None):
        f = forward/np.linalg.norm(forward)
        right = np.cross([0, 1, 0], f); right /= np.linalg.norm(right)
        up = np.cross(f, right)
        self.basis = np.stack([right, up, f])
        self.texel = texel
        self.size = int(extent/texel)
        self.origin = self.basis @ centre - np.array([extent/2, extent/2, 0])
        p = V @ self.basis.T - self.origin
        sx, sy = p[:, 0]/texel, self.size - p[:, 1]/texel
        self.depth, _ = raster(sx, sy, p[:, 2], T, self.size, self.size, False, uv, alpha, cutout)

    def visibility(self, P, Nrm):
        p = (P + Nrm*self.texel*1.5) @ self.basis.T - self.origin
        fx, fy = p[:, 0]/self.texel - .5, self.size - p[:, 1]/self.texel - .5
        d = p[:, 2] - self.texel*1.2
        vis = np.zeros(len(P)); total = 0.
        for oy in range(-2, 3):
            for ox in range(-2, 3):
                wgt = (3-abs(ox))*(3-abs(oy))
                ix = np.clip(np.round(fx+ox).astype(int), 0, self.size-1)
                iy = np.clip(np.round(fy+oy).astype(int), 0, self.size-1)
                vis += wgt*(d <= self.depth[iy, ix]); total += wgt
        out = (fx < 0) | (fy < 0) | (fx >= self.size) | (fy >= self.size)
        vis = vis/total
        vis[out] = 1
        return vis


def sh9(sky, equator, ground):
    """Project the trilight gradient to nine SH coefficients and convolve with the clamped cosine (irradiance/pi)."""
    rng = np.random.default_rng(3)
    d = rng.normal(size=(60000, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    y = d[:, 1:2]
    L = np.where(y > 0, equator + (sky-equator)*y, equator + (ground-equator)*(-y))
    B = basis(d)
    coeff = (B.T @ L) * (4*math.pi/len(d))
    band = np.array([1, 2/3, 2/3, 2/3, .25, .25, .25, .25, .25])
    return coeff*band[:, None]


def basis(d):
    x, y, z = d[:, 0], d[:, 1], d[:, 2]
    return np.stack([.282095*np.ones_like(x), .488603*y, .488603*z, .488603*x, 1.092548*x*y, 1.092548*y*z,
                     .315392*(3*z*z-1), 1.092548*x*z, .546274*(x*x-y*y)], 1)


class Grain:
    def __init__(self, path):
        img = np.asarray(Image.open(path).convert('RGBA'), float)/255
        self.mips = [img]
        while self.mips[-1].shape[0] > 1:
            a = self.mips[-1]
            self.mips.append((a[0::2, 0::2]+a[1::2, 0::2]+a[0::2, 1::2]+a[1::2, 1::2])/4)

    def bilinear(self, level, u, v):
        img = self.mips[level]; s = img.shape[0]
        x = u*s - .5; y = v*s - .5
        x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
        fx = (x-x0)[:, None]; fy = (y-y0)[:, None]
        x0 %= s; y0 %= s; x1 = (x0+1) % s; y1 = (y0+1) % s
        top = img[y0, x0]*(1-fx) + img[y0, x1]*fx
        bot = img[y1, x0]*(1-fx) + img[y1, x1]*fx
        return top*(1-fy) + bot*fy

    def sample(self, u, v, lod):
        lod = np.clip(lod, 0, len(self.mips)-1)
        out = np.zeros((len(u), self.mips[0].shape[2]))
        lo = np.floor(lod).astype(int); f = (lod-lo)[:, None]
        for level in np.unique(lo):
            m = lo == level
            a = self.bilinear(level, u[m], v[m])
            b = self.bilinear(min(level+1, len(self.mips)-1), u[m], v[m])
            out[m] = a*(1-f[m]) + b*f[m]
        return out


def camera_basis(forward, up):
    f = forward/np.linalg.norm(forward)
    r = np.cross(up, f); r /= np.linalg.norm(r)
    return f, r, np.cross(f, r)


def clip_near(V, N, C, T, eye, f, near):
    d = (V-eye) @ f
    keep = d[T].min(1) >= near
    cross = (~keep) & (d[T].max(1) >= near)
    nv, nn, nc, nt = [V], [N], [C], [T[keep]]
    off = len(V)
    owners = [np.nonzero(keep)[0]]
    for t in np.nonzero(cross)[0]:
        poly = []
        ids = T[t]
        for k in range(3):
            a, b = ids[k-1], ids[k]
            da, db = d[a], d[b]
            if (da >= near) != (db >= near):
                s = (near-da)/(db-da)
                poly.append((V[a]+s*(V[b]-V[a]), N[a]+s*(N[b]-N[a]), C[a]+s*(C[b]-C[a])))
            if db >= near:
                poly.append((V[b], N[b], C[b]))
        for k in range(1, len(poly)-1):
            for q in (poly[0], poly[k], poly[k+1]):
                nv.append(q[0][None]); nn.append(q[1][None]); nc.append(q[2][None])
            nt.append(np.array([[off, off+1, off+2]])); owners.append(np.array([t]))
            off += 3
    return np.vstack(nv), np.vstack(nn), np.vstack(nc), np.vstack(nt), np.concatenate(owners)


def render(world, eye, forward, up, fov, w, h, ss=2, overlays=None, socket_paint=None):
    """Render one view at w x h (supersampled ss x ss). overlays: additive or alpha signal geometry (see vfx)."""
    props = shader_defaults()
    leaf = shader_defaults('ValeFoliage')
    parts = world['parts']
    V, N, C, T, isactor, cast, UV, fol = merge(parts)
    atlas = world.setdefault('atlas', Grain(ATLAS) if ATLAS.exists() else None)
    alpha0 = atlas.mips[0][..., 3] if atlas is not None else None
    cut = leaf.get('_Cutoff', .45)
    light = world['light']
    if 'shadow' not in world:
        world['shadow'] = ShadowMap(V, T[cast], light['forward'], np.array([2., 0, 45.]), uv=UV, alpha=alpha0,
                                    cutout=fol[cast])
    shadow = world['shadow']
    f, r, u = camera_basis(np.asarray(forward, float), np.asarray(up, float))
    W, H = w*ss, h*ss
    focal = H*.5/math.tan(math.radians(fov/2))
    # Per-object paint overrides (the regulators' property blocks at the current step).
    lift = np.ones(len(T)); tint = np.ones((len(T), 3))
    if socket_paint:
        off = 0
        for p in parts:
            n = len(p['idx'])
            if p['name'] in socket_paint:
                lift[off:off+n] = socket_paint[p['name']][0]; tint[off:off+n] = socket_paint[p['name']][1]
            off += n
    Vc, Nc, Cc, Tc, owner = clip_near(V, N, np.hstack([C, UV]), T, eye, f, .091)
    UVc = Cc[:, 4:6]; Cc = Cc[:, :4]
    rel = Vc-eye
    z = rel @ f
    sx = W/2 + focal*(rel @ r)/np.maximum(z, 1e-4)
    sy = H/2 - focal*(rel @ u)/np.maximum(z, 1e-4)
    zbuf, ids = raster(sx, sy, z, Tc, W, H, True, UVc, alpha0, fol[owner], cut)
    # Pixel rays and exact surface points on each pixel's triangle.
    py, px = np.nonzero(ids >= 0)
    tri = ids[py, px]
    rays = f[None]*focal + r[None]*(px[:, None]+.5-W/2) - u[None]*(py[:, None]+.5-H/2)
    rays /= np.linalg.norm(rays, axis=1, keepdims=True)
    A, B, Cv = Vc[Tc[tri, 0]], Vc[Tc[tri, 1]], Vc[Tc[tri, 2]]
    fn = np.cross(B-A, Cv-A)
    t = np.einsum('ij,ij->i', A-eye, fn)/np.where(np.abs(np.einsum('ij,ij->i', rays, fn)) < 1e-12, 1e-12,
                                                  np.einsum('ij,ij->i', rays, fn))
    P = eye + rays*t[:, None]
    # Barycentrics for the vertex normal, paint and UV.
    v0, v1, v2 = B-A, Cv-A, P-A
    d00 = np.einsum('ij,ij->i', v0, v0); d01 = np.einsum('ij,ij->i', v0, v1); d11 = np.einsum('ij,ij->i', v1, v1)
    d20 = np.einsum('ij,ij->i', v2, v0); d21 = np.einsum('ij,ij->i', v2, v1)
    den = np.maximum(d00*d11 - d01*d01, 1e-12)
    bb = np.clip((d11*d20 - d01*d21)/den, 0, 1); bc = np.clip((d00*d21 - d01*d20)/den, 0, 1); ba = np.clip(1-bb-bc, 0, 1)
    def interp(arr):
        return arr[Tc[tri, 0]]*ba[:, None] + arr[Tc[tri, 1]]*bb[:, None] + arr[Tc[tri, 2]]*bc[:, None]
    nrm = interp(Nc)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-9)
    col = interp(Cc)
    src = owner[tri]
    leafy = fol[src]
    paint = col[:, :3]/255 * tint[src]
    code = col[:, 3]/255
    act = isactor[src]
    glow = np.clip(1 - code*5, 0, 1)
    glow[act | leafy] = 0
    dist = np.linalg.norm(P-eye, axis=1)
    pix = 2*math.tan(math.radians(fov/2))/h
    cosv = np.abs(np.einsum('ij,ij->i', nrm, rays))
    # Grain (palette surfaces only), channel by surface code like the shader of this revision.
    g = Grain(GRAIN)
    scale = props['_GrainScale']
    wgt = nrm**2; wgt = wgt**2; wgt /= np.maximum(wgt.sum(1, keepdims=True), 1e-4)
    lod = np.log2(np.maximum(dist*pix/np.maximum(cosv, .25)*scale*256, 1e-6))
    uvw = P*scale
    grain = (g.sample(uvw[:, 2], uvw[:, 1], lod)*wgt[:, :1] + g.sample(uvw[:, 0], uvw[:, 2], lod)*wgt[:, 1:2]
             + g.sample(uvw[:, 0], uvw[:, 1], lod)*wgt[:, 2:3])
    if '_BarkStrength' in props:
        bark = (code >= .86) & (code < .95)
        pick = np.where(code >= .95, grain[:, 0], np.where(bark, grain[:, 3], np.where(code >= .6, grain[:, 1], grain[:, 2])))
        strength = np.where(bark, props['_BarkStrength'], props['_GrainStrength'])*(1-glow)
    else:
        pick = np.where(code >= .9, grain[:, 0], np.where(code >= .6, grain[:, 1], grain[:, 2]))
        strength = props['_GrainStrength']*(1-glow)
    strength[act | leafy] = 0
    paint = paint * (1 + (pick*2-1)*strength)[:, None]
    # Foliage albedo: atlas colour (mipmapped) times the card tint.
    if leafy.any():
        uvp = interp(UVc)[leafy]
        texel = dist[leafy]*pix/np.maximum(cosv[leafy], .3)*360
        tex = atlas.sample(uvp[:, 0], 1-uvp[:, 1], np.log2(np.maximum(texel, 1e-6)))
        paint[leafy] = tex[:, :3] * col[leafy, :3]/255
    # Sun, soft shadow and ambient.
    to_sun = -light['forward']/np.linalg.norm(light['forward'])
    wrap = np.where(leafy, leaf.get('_Wrap', .55), props['_Wrap'])
    wrapped = np.clip((nrm @ to_sun + wrap)/(1+wrap), 0, 1)
    vis = shadow.visibility(P, nrm) if light['shadows'] else np.ones(len(P))
    fade = np.clip((150 - (P-eye) @ f)/15, 0, 1)
    vis = 1 - (1-vis)*fade
    atten = (1-light['strength']) + light['strength']*vis
    sun_scale = np.where(leafy, leaf.get('_SunScale', .95), props['_SunScale'])
    sun = light['color'][None]*(wrapped*atten*sun_scale)[:, None]
    if leafy.any():
        through = np.clip(rays[leafy] @ to_sun, 0, 1)**3*leaf.get('_Translucency', .7)
        trans = np.asarray(leaf.get('_TransColor', np.array([.92, 1, .5, 1])))[:3]
        sun[leafy] += light['color'][None]*(atten[leafy]*leaf.get('_SunScale', .95)*through)[:, None]*trans[None]
    coeff = sh9(*world['ambient'])
    sky = (basis(nrm) @ coeff)*np.where(leafy, leaf.get('_SkyScale', .8), props['_SkyScale'])[:, None]
    c = paint*(sun+sky)*lift[src][:, None]
    c = c*(1-glow[:, None]) + paint*lift[src][:, None]*glow[:, None]
    # Haze.
    haze = np.clip((dist - props['_HazeStart'])/props['_HazeDepth'], 0, 1)*props['_HazeMax']
    haze *= np.clip(1.15 - np.maximum(0, P[:, 1]-8)/70, 0, 1)
    toward = np.clip(rays @ to_sun*.5 + .5, 0, 1)
    near, far = props['_NearHaze'][:3], props['_FarHaze'][:3]
    hc = far[None] + (near-far)[None]*(toward**2)[:, None]
    haze = haze*(glow <= .99)
    haze[act] = 0
    c = c*(1-haze[:, None]) + hc*haze[:, None]
    image = np.zeros((H, W, 3))
    image[:] = np.array([.64, .77, .82])
    image[py, px] = c
    if overlays:
        image = composite(image, zbuf, overlays, eye, f, r, u, focal, W, H)
    image = np.clip(image, 0, 1)
    image = image.reshape(h, ss, w, ss, 3).mean((1, 3))
    return Image.fromarray((image*255+.5).astype(np.uint8))


def composite(image, zbuf, overlays, eye, f, r, u, focal, W, H):
    """Camera-facing signal ribbons and glow beads, depth tested, alpha or additive like ValeSignal.shader."""
    for item in overlays:
        if item['kind'] == 'ribbon':
            pts = np.asarray(item['points'], float)
            widths = np.asarray(item['widths'], float); cols = np.asarray(item['colors'], float)
            for k in range(len(pts)-1):
                a, b = pts[k], pts[k+1]
                seg = b-a
                if np.linalg.norm(seg) < 1e-6:
                    continue
                mid = (a+b)/2
                side = np.cross(seg, mid-eye); side /= max(np.linalg.norm(side), 1e-9)
                quad = [a-side*widths[k]/2, a+side*widths[k]/2, b+side*widths[k+1]/2, b-side*widths[k+1]/2]
                ca, cb = cols[k], cols[k+1]
                fill(image, zbuf, quad, [(ca, 0), (ca, 1), (cb, 1), (cb, 0)], item, eye, f, r, u, focal, W, H)
        elif item['kind'] == 'bead':
            centre = np.asarray(item['centre'], float); rad = item['radius']; col = np.asarray(item['color'], float)
            rel = centre-eye; z = rel @ f
            if z < .1:
                continue
            cx = W/2 + focal*(rel @ r)/z; cy = H/2 - focal*(rel @ u)/z; pr = focal*rad/z
            l = max(0, int(cx-pr*1.6)); rr = min(W-1, int(cx+pr*1.6)); t = max(0, int(cy-pr*1.6)); b = min(H-1, int(cy+pr*1.6))
            if l > rr or t > b:
                continue
            Y, X = np.ogrid[t+.5:b+1.5, l+.5:rr+1.5]
            q = np.sqrt((X-cx)**2 + (Y-cy)**2)/max(pr, 1e-6)
            a = (q < 1)*col[3]  # an unlit sphere draws as a flat disc of its colour
            vis = zbuf[t:b+1, l:rr+1] > z - rad
            a = a*vis
            region = image[t:b+1, l:rr+1]
            rgb = col[:3]*item.get('boost', 1)
            if item.get('additive'):
                region += rgb*a[..., None]
            else:
                region[:] = region*(1-a[..., None]) + rgb*a[..., None]
    return image


def fill(image, zbuf, quad, cols, item, eye, f, r, u, focal, W, H):
    pts = np.array(quad)
    rel = pts-eye; z = rel @ f
    if (z < .1).any():
        return
    sx = W/2 + focal*(rel @ r)/z; sy = H/2 - focal*(rel @ u)/z
    for tri in ((0, 1, 2), (0, 2, 3)):
        ia, ib, ic = tri
        ax, ay, bx, by, cx, cy = sx[ia], sy[ia], sx[ib], sy[ib], sx[ic], sy[ic]
        den = (by-cy)*(ax-cx) + (cx-bx)*(ay-cy)
        if abs(den) < 1e-9:
            continue
        l = max(0, math.floor(min(ax, bx, cx))); rr = min(W-1, math.ceil(max(ax, bx, cx)))
        t = max(0, math.floor(min(ay, by, cy))); b = min(H-1, math.ceil(max(ay, by, cy)))
        if l > rr or t > b:
            continue
        Y, X = np.ogrid[t+.5:b+1.5, l+.5:rr+1.5]
        wa = ((by-cy)*(X-cx) + (cx-bx)*(Y-cy))/den
        wb = ((cy-ay)*(X-cx) + (ax-cx)*(Y-cy))/den
        wc = 1-wa-wb
        inside = (wa >= 0) & (wb >= 0) & (wc >= 0)
        if not inside.any():
            continue
        depth = 1/np.maximum(1e-9, wa/z[ia] + wb/z[ib] + wc/z[ic])
        inside &= depth < zbuf[t:b+1, l:rr+1]
        col = (wa[..., None]*cols[ia][0] + wb[..., None]*cols[ib][0] + wc[..., None]*cols[ic][0])
        across = wa*cols[ia][1] + wb*cols[ib][1] + wc*cols[ic][1]
        edge = 1-np.abs(across*2-1)
        alpha = col[..., 3]*(1 + (edge*edge-1)*item.get('soft', 0))*inside
        rgb = col[..., :3]*item.get('boost', 1)
        region = image[t:b+1, l:rr+1]
        if item.get('additive'):
            region += rgb*alpha[..., None]
        else:
            region[:] = region*(1-alpha[..., None]) + rgb*alpha[..., None]


def label(img, title, note):
    sheet = Image.new('RGB', (img.width, img.height+38), '#1b2433')
    sheet.paste(img)
    draw = ImageDraw.Draw(sheet)
    path = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    big = ImageFont.truetype(path, 12) if Path(path).exists() else ImageFont.load_default()
    small = ImageFont.truetype(path, 10) if Path(path).exists() else ImageFont.load_default()
    draw.text((13, img.height+5), title, fill='#f3ead6', font=big)
    draw.text((13, img.height+22), note, fill='#b9c3cf', font=small)
    return sheet


NOTE = ('Software render of the scene file with a CPU copy of the Vale shader (sun, soft shadow map, ambient, grain, '
        'haze). Unity cascades, anti-aliasing, post, animation and HUD not shown.')


GOLD = np.array([1., .7, .25])


def ring(centre, radius, cam_r, cam_u, n=49):
    """Camera-facing ring through the same 49 points as ShardwakeVale.Ring(..., ground: false)."""
    a = np.linspace(0, 2*math.pi, n)
    return centre[None] + (np.cos(a)[:, None]*cam_r[None] + np.sin(a)[:, None]*cam_u[None])*radius


def cast_sheet(world, folder, out, tag):
    """Four frames of one Sunbolt cast at Regulator 1, from the exported pose and SunboltFx shapes."""
    data = json.loads((folder/'cast.json').read_text())
    gem = np.array(data['gem'])
    body = np.array([5.85, 5.1626, 90.35]); yaw = math.radians(11.3)
    f = np.array([math.sin(yaw), 0, math.cos(yaw)]); rt = np.array([f[2], 0, -f[0]])
    # Side-on, so the whole flight crosses the frame: right of the flight line, a little behind its middle.
    origin = np.array(data['origin'])
    line = gem-origin; line /= np.linalg.norm(line)
    side = np.cross([0, 1., 0], line); side /= np.linalg.norm(side)
    mid = (origin+gem)/2
    eye = mid - side*6.5 - line*1.5 + np.array([0, .6, 0])
    look = mid - line*.6 - np.array([0, .15, 0])
    fwd = look-eye; fwd /= np.linalg.norm(fwd)
    _, cam_r, cam_u = camera_basis(fwd, np.array([0, 1., 0]))
    m = np.eye(4); m[:3, :3] = np.array([[math.cos(yaw), 0, math.sin(yaw)], [0, 1, 0], [-math.sin(yaw), 0, math.cos(yaw)]]); m[:3, 3] = body
    base = list(world['parts'])
    tiles = []
    names = ['GATHER', 'FLIGHT', 'GROUNDED: BURST AND FLARE', 'SETTLED']
    live = (1.34, np.ones(3))
    for k, fr in enumerate(data['frames']):
        world['parts'] = base + [actor(folder/fr['actor'], m)]
        world.pop('shadow', None)
        paint = {'Regulator %d' % (i+1): live for i in range(3)}
        over = []
        if 'gather' in fr:
            hand = np.array(fr['hand']); g = fr['gather']
            over.append(dict(kind='bead', centre=hand, radius=(.06+.24*g)/2, color=[*GOLD, 1], additive=True, boost=1.5))
            pts = ring(hand, fr['ring'], cam_r, cam_u)
            over.append(dict(kind='ribbon', points=pts, widths=[.035]*len(pts), colors=[[*GOLD, 1]]*len(pts), additive=True, boost=1.5, soft=1))
        if 'progress' in fr:
            tail, mid, head = np.array([1, .62, .22]), GOLD, np.array([1, .95, .78])
            cols = []
            for i, a in enumerate(fr['alpha']):
                u = i/(len(fr['alpha'])-1)
                c = tail+(mid-tail)*u/.7 if u < .7 else mid+(head-mid)*(u-.7)/.3
                cols.append([*c, a])
            over.append(dict(kind='ribbon', points=fr['trail'], widths=fr['width'], colors=cols, additive=True, boost=1.5, soft=1))
            over.append(dict(kind='bead', centre=fr['head'], radius=.15, color=[*GOLD, 1], additive=True, boost=1.5))
        if 'gather' in fr or 'progress' in fr:
            pts = ring(gem, .70, cam_r, cam_u)
            over.append(dict(kind='ribbon', points=pts, widths=[.075]*len(pts), colors=[[1, .80, .36, 1]]*len(pts)))
        if 'since_hit' in fr:
            paint['Regulator 1'] = (fr['lift'], np.array(fr['tint']))
            if 'burst' in fr:
                over.append(dict(kind='bead', centre=gem, radius=fr['burst']*1.1/2, color=[*GOLD, fr['burst_alpha']], additive=True, boost=1.5))
                pts = ring(gem, fr['burst']*1.35, cam_r, cam_u)
                over.append(dict(kind='ribbon', points=pts, widths=[.06]*len(pts), colors=[[*GOLD, fr['burst_alpha']]]*len(pts), additive=True, boost=1.5, soft=1))
        img = render(world, eye, fwd, np.array([0, 1., 0]), 58, 544, 306, overlays=over, socket_paint=paint)
        d = ImageDraw.Draw(img)
        path = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
        font = ImageFont.truetype(path, 12) if Path(path).exists() else ImageFont.load_default()
        d.rectangle((0, 0, 544, 20), fill='#1b2433')
        d.text((8, 3), '%d  %s   t = %.2f s after the cast' % (k+1, names[k], fr['t']), fill='#f3ead6', font=font)
        tiles.append(img)
    world['parts'] = base
    sheet = Image.new('RGB', (1088, 612))
    for k, t in enumerate(tiles):
        sheet.paste(t, ((k % 2)*544, (k//2)*306))
    label(sheet, 'OFFLINE SCENE PREVIEW  |  SUNBOLT CAST TO HIT  |  %s  |  Regulator 1, flight %.2f s' % (tag, data['flight']),
          'Pose: idle plus the point gesture exported from the game code; bolt shapes from SunboltFx. Unity shading, '
          'walk animation, camera motion and HUD not shown.').save(out, optimize=True)
    print('wrote', out)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('output', type=Path)
    ap.add_argument('--actors', type=Path)
    ap.add_argument('--tag', default='working tree')
    ap.add_argument('--shots', default='spawn,bridge')
    ap.add_argument('--cast', type=Path, help='folder with cast.json and posed actors from the offline exporter')
    a = ap.parse_args()
    a.output.mkdir(parents=True, exist_ok=True)
    world = load()
    if a.actors:
        world['parts'].append(actor(a.actors/'Malik.bin', world['objects']['Malik - start and facing'][0]))
        world['parts'].append(actor(a.actors/'Dante.bin', world['objects']['Dante - conversation and actor'][0]))
    if a.cast:
        cast_sheet(world, a.cast, a.output/'sunbolt-cast.png', a.tag)
        if not a.shots:
            return
    shots = json.loads(SHOTS.read_text())['shots']
    shots.update(json.loads(REVIEW.read_text())['shots'])
    for name in a.shots.split(','):
        s = shots[name]
        img = render(world, np.array(s['eye']), np.array(s['forward']), np.array(s['up']), s['fov'], 1088, 612,
                     socket_paint={'Regulator %d' % (i+1): (.92, np.ones(3)) for i in range(3)})
        label(img, 'OFFLINE SCENE PREVIEW  |  %s AFTER  |  %s  |  Assets/Scenes/ShardwakeVale.unity' % (name.upper().replace('_', ' '), a.tag),
              NOTE).save(a.output/(name+'-after.png'), optimize=True)
        print('wrote', a.output/(name+'-after.png'))


if __name__ == '__main__':
    main()
