#!/usr/bin/env python3
"""Original Visual Rebirth 04A modular sculpture and architecture authoring.

Run python tools/generate_visual_rebirth_04a.py --confirm-regenerate.
Only writes the 04A Models and Meshes directories. Requires numpy. Geometry is
metres, Y-up, base-centred; balconies use deck-height origin. OBJ is editable
source, CMH1 is the identical triangle soup read by the existing 02H decoder.
No external meshes, textures, or generative image inputs are used.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import re
import struct
import uuid

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets/NeonSyndicate/Resources/VisualRebirth04A"
TAU = math.tau


def v3(value):
    return np.asarray(value, dtype=float)


def unit(value):
    value = v3(value)
    return value / max(1e-12, np.linalg.norm(value))


def polar(angle, radius, y=0):
    return np.array([math.cos(angle) * radius, y, math.sin(angle) * radius])


def meta(path, kind="text"):
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "crystal-manor-04a/" + path.relative_to(ROOT).as_posix()).hex
    if kind == "obj":
        source = ROOT / "Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj.meta"
        content = re.sub(r"guid: [a-f0-9]+", "guid: " + guid, source.read_text())
        content = re.sub(r"userData:.*", "userData: Original 04A geometry. Metres Y-up. Single material role.", content)
    else:
        content = "fileFormatVersion: 2\nguid: " + guid + "\n"
        content += "folderAsset: yes\nDefaultImporter:\n" if kind == "folder" else "TextScriptImporter:\n"
        content += "  externalObjects: {}\n  userData: Original Visual Rebirth 04A asset.\n  assetBundleName:\n  assetBundleVariant:\n"
    Path(str(path) + ".meta").write_text(content)


class Mesh:
    """Explicit smooth-normal triangle soup with metric UVs and hard-edge control."""

    def __init__(self):
        self.vertices = []
        self.leaf_steps = 3

    def tri(self, a, b, c, outward=None, normals=None, uvs=None):
        pts = [v3(a), v3(b), v3(c)]
        cross = np.cross(pts[1] - pts[0], pts[2] - pts[0])
        if np.linalg.norm(cross) < 1e-9:
            return
        normal = unit(cross)
        ns = [normal] * 3 if normals is None else [unit(n) for n in normals]
        axes = [i for i in range(3) if i != np.argmax(abs(normal))]
        tex = [p[axes] * .5 for p in pts] if uvs is None else list(uvs)
        if outward is not None and np.dot(normal, outward) < 0:
            pts[1], pts[2] = pts[2], pts[1]
            tex[1], tex[2] = tex[2], tex[1]
            ns = [-normal] * 3 if normals is None else [ns[0], ns[2], ns[1]]
        for p, n, uv in zip(pts, ns, tex):
            self.vertices.append([*p, *n, *uv])

    def quad(self, a, b, c, d, outward=None, normals=None, uvs=None):
        normals = normals or [None] * 4
        uvs = uvs or [None] * 4
        for ids in ((0, 1, 2), (0, 2, 3)):
            pts = [a, b, c, d]
            self.tri(*(pts[i] for i in ids), outward,
                     [normals[i] for i in ids] if normals[0] is not None else None,
                     [uvs[i] for i in ids] if uvs[0] is not None else None)

    def box(self, center, size, bevel=0):
        center, half = v3(center), v3(size) * .5
        if bevel <= 0:
            for axis in range(3):
                other = [i for i in range(3) if i != axis]
                for sign in (-1, 1):
                    normal = np.zeros(3)
                    normal[axis] = sign
                    pts = []
                    for j, k in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                        p = center.copy()
                        p[axis] += sign * half[axis]
                        p[other[0]] += j * half[other[0]]
                        p[other[1]] += k * half[other[1]]
                        pts.append(p)
                    self.quad(*pts, normal)
            return
        # Bevelled edges have actual silhouette depth and catch grazing light.
        bevel = min(bevel, float(min(half)) * .45)
        for axis in range(3):
            others = [i for i in range(3) if i != axis]
            for sign in (-1, 1):
                pts = []
                for j, k in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                    p = center.copy()
                    p[axis] += sign * half[axis]
                    p[others[0]] += j * (half[others[0]] - bevel)
                    p[others[1]] += k * (half[others[1]] - bevel)
                    pts.append(p)
                normal = np.zeros(3)
                normal[axis] = sign
                self.quad(*pts, normal)
        for along in range(3):
            a, b = [i for i in range(3) if i != along]
            for sa in (-1, 1):
                for sb in (-1, 1):
                    pts = []
                    for end, edge in ((-1, 0), (1, 0), (1, 1), (-1, 1)):
                        p = center.copy()
                        p[along] += end * (half[along] - bevel)
                        p[a] += sa * (half[a] - bevel * edge)
                        p[b] += sb * (half[b] - bevel * (1 - edge))
                        pts.append(p)
                    n = np.zeros(3)
                    n[a], n[b] = sa, sb
                    self.quad(*pts, n)
        for sx in (-1, 1):
            for sy in (-1, 1):
                for sz in (-1, 1):
                    signs = np.array([sx, sy, sz])
                    corner = center + signs * (half - bevel)
                    points = [corner + np.eye(3)[axis] * signs[axis] * bevel for axis in range(3)]
                    self.tri(*points, signs)

    def sweep(self, points, radii, sides=8, caps=True, fluting=0):
        points = [v3(p) for p in points]
        radii = np.full(len(points), radii) if np.isscalar(radii) else radii
        rings, norms, distances = [], [], [0]
        previous = None
        for i, p in enumerate(points):
            tangent = unit(points[min(i + 1, len(points) - 1)] - points[max(0, i - 1)])
            ref = np.array([1, 0, 0]) if abs(tangent[1]) > .92 else np.array([0, 1, 0])
            right = unit(np.cross(tangent, ref)) if previous is None else unit(previous-tangent*np.dot(previous,tangent))
            forward = np.cross(tangent, right)
            previous = right
            ring, ns = [], []
            for j in range(sides):
                theta = j * TAU / sides
                n = right * math.cos(theta) + forward * math.sin(theta)
                ring.append(p + n * radii[i] * (1 + fluting * math.cos(theta * 6)))
                ns.append(n)
            rings.append(ring)
            norms.append(ns)
            if i:
                distances.append(distances[-1] + np.linalg.norm(points[i] - points[i-1]))
        for i in range(len(points) - 1):
            for j in range(sides):
                k = (j + 1) % sides
                self.quad(rings[i][j], rings[i][k], rings[i+1][k], rings[i+1][j],
                          norms[i][j] + norms[i][k],
                          [norms[i][j], norms[i][k], norms[i+1][k], norms[i+1][j]],
                          [(j / sides, distances[i] * .3), ((j+1) / sides, distances[i] * .3),
                           ((j+1) / sides, distances[i+1] * .3), (j / sides, distances[i+1] * .3)])
        if caps:
            for j in range(sides):
                k = (j+1) % sides
                self.tri(points[0], rings[0][k], rings[0][j], points[0] - points[1])
                self.tri(points[-1], rings[-1][j], rings[-1][k], points[-1] - points[-2])

    def rod(self, a, b, radius=.1, top=None, sides=8):
        self.sweep([a, b], [radius, radius if top is None else top], sides)

    def lathe(self, profile, sides=24, center=(0, 0, 0), fluting=0, phase=0):
        center = v3(center)
        for j in range(len(profile) - 1):
            r, y = profile[j]
            rn, yn = profile[j+1]
            radial, vertical = yn-y, r-rn
            for i in range(sides):
                a, b = i * TAU / sides + phase, (i+1) * TAU / sides + phase
                p = [center + polar(a, r*(1+fluting*math.cos(a*6)), y),
                     center + polar(b, r*(1+fluting*math.cos(b*6)), y),
                     center + polar(b, rn*(1+fluting*math.cos(b*6)), yn),
                     center + polar(a, rn*(1+fluting*math.cos(a*6)), yn)]
                na, nb = unit(polar(a, radial, vertical)), unit(polar(b, radial, vertical))
                self.quad(*p, na+nb, [na, nb, nb, na],
                          [(i/sides, y*.25), ((i+1)/sides, y*.25), ((i+1)/sides, yn*.25), (i/sides, yn*.25)])

    def ring(self, radius, tube, center=(0, 0, 0), segments=24, sides=6, tilt=0, arc=TAU, start=0):
        c, s = math.cos(tilt), math.sin(tilt)
        rotation = np.array([[1,0,0],[0,c,-s],[0,s,c]])
        points = [v3(center) + rotation @ polar(start + arc * i / segments, radius) for i in range(segments+1)]
        self.sweep(points, tube, sides, caps=arc < TAU-.001)

    def leaf(self, a, b, width, bend=.1, lobes=0):
        a, b = v3(a), v3(b)
        axis = unit(b-a)
        ref = np.array([0,1,0]) if abs(axis[1]) < .9 else np.array([1,0,0])
        side = unit(np.cross(axis, ref))
        up = unit(np.cross(side, axis))
        for i in range(self.leaf_steps):
            t0, t1 = i/self.leaf_steps, (i+1)/self.leaf_steps
            def section(t):
                center = a * (1-t) + b*t + up*math.sin(t*math.pi)*bend
                w = math.sin(t*math.pi)*width*(1+lobes*math.sin(t*math.pi*6))
                return [center-side*w, center+up*w*.16, center+side*w]
            s0, s1 = section(t0), section(t1)
            for j in range(2):
                pts = [s0[j], s0[j+1], s1[j+1], s1[j]]
                self.quad(*pts, up, uvs=[(j*.5,t0),((j+1)*.5,t0),((j+1)*.5,t1),(j*.5,t1)])
                self.quad(*pts, -up, uvs=[(j*.5,t0),((j+1)*.5,t0),((j+1)*.5,t1),(j*.5,t1)])

    def arch(self, width=6, height=6, depth=.65, thickness=.5, low=False, center=(0,0,0)):
        # Separate beveled voussoirs along a pointed arch, including the vertical piers.
        segments = 7 if low else 15
        rise, spring, half = height*.43, height*.57, width*.5-thickness*.5
        left = [np.array([-half, 0, 0]), np.array([-half, spring, 0])]
        for t in np.linspace(0, 1, segments+1)[1:]:
            left.append(np.array([-half*(1-t)**.75, spring+rise*math.sin(t*math.pi*.5), 0]))
        points = left + [np.array([-p[0],p[1],0]) for p in left[-2::-1]]
        for i in range(len(points)-1):
            a, b = points[i] + center, points[i+1] + center
            direction = unit(b-a)
            side = np.array([-direction[1], direction[0], 0])
            trim = .006 if not low else 0
            a, b = a+direction*trim, b-direction*trim
            pts = [a+side*thickness*.5, a-side*thickness*.5, b-side*thickness*.5, b+side*thickness*.5]
            for z in (-depth*.5, depth*.5):
                self.quad(*(p+np.array([0,0,z]) for p in pts), [0,0,np.sign(z)])
            for j in range(4):
                k = (j+1)%4
                face = [pts[j]+[0,0,-depth*.5],pts[k]+[0,0,-depth*.5],pts[k]+[0,0,depth*.5],pts[j]+[0,0,depth*.5]]
                self.quad(*face, (pts[j]+pts[k])*.5-(a+b)*.5)

    def shard(self, base, radius, height, lean=(0,0,0), sides=6):
        base, lean = v3(base), v3(lean)
        rings = []
        for y, r, shift in ((0,.48,0),(.18,1,.13),(.77,.82,.75),(1,0,1)):
            rings.append([base+lean*shift+polar(i*TAU/sides,radius*r,height*y) for i in range(sides)])
        for j in range(3):
            for i in range(sides):
                k=(i+1)%sides
                self.quad(rings[j][i],rings[j][k],rings[j+1][k],rings[j+1][i],polar((i+.5)*TAU/sides,1,.2))
        for i in range(sides):
            self.tri(base,rings[0][i],rings[0][(i+1)%sides],[0,-1,0])

    def transformed(self, scale=(1,1,1), translate=(0,0,0), yaw=0):
        c,s=math.cos(yaw),math.sin(yaw)
        rotation=np.array([[c,0,s],[0,1,0],[-s,0,c]])
        other=Mesh()
        for p in self.vertices:
            n=unit(rotation @ (v3(p[3:6])/v3(scale)))
            position=rotation @ (v3(p[:3])*v3(scale))+v3(translate)
            other.vertices.append([*position,*n,*p[6:]])
        return other

    def add(self, other):
        self.vertices.extend(other.vertices)


MODEL_ROLES = {
    "arcade":"stone", "wall_panel":"stone", "buttress":"stone", "capital":"stone",
    "rib_vault":"stone", "balcony":"stone", "terrace":"stone", "stair":"stone",
    "portal":"stone", "spire":"stone", "roof":"roof", "observatory":"metal",
    "living_trunk":"bark", "living_canopy":"leaf", "willow_trunk":"bark", "willow_canopy":"leaf",
    "cypress_trunk":"bark", "cypress_canopy":"leaf", "root_arch":"bark", "fern":"leaf",
    "mushroom":"flower", "flower":"flower", "reeds":"leaf", "rock":"stone", "ore":"crystal",
    "drill":"metal", "monolith":"stone", "fountain":"stone", "lantern":"metal", "banner":"cloth",
    "market_awning":"cloth", "railing":"metal", "guardian":"stone", "sky_ring":"metal",
    "bench":"bark", "planter":"stone", "threshold":"stone", "ruin":"stone", "vine":"leaf",
    "sigil":"metal", "ward_pylon":"stone", "bridge":"stone", "column":"stone",
    "tower":"stone", "window":"metal", "crystal_cluster":"crystal", "roof_finial":"metal",
}


def column(m, low, center=(0,0,0), height=6, radius=.5):
    profile=[(0,0),(radius*1.5,0),(radius*1.5,.18),(radius*1.25,.27),(radius*1.25,.40),
             (radius,.58),(radius*.78,height-.7),(radius*1.08,height-.52),(radius*1.3,height-.32),
             (radius*1.4,height-.18),(radius*1.4,height),(0,height)]
    m.lathe(profile,12 if low else 24,center,fluting=.07)


def railing(m, low, width=6, center=(0,0,0)):
    center=v3(center)
    for x in np.linspace(-width*.5,width*.5,5 if low else 9):
        m.rod(center+[x,0,0],center+[x,1.12,0],.045,.035,5 if low else 8)
        if not low:
            m.shard(center+[x,1.05,0],.075,.22,sides=4)
    for y in (.16,1.1):
        m.rod(center+[-width*.5,y,0],center+[width*.5,y,0],.06,sides=6)
    if not low:
        for x in np.linspace(-width*.5+.4,width*.5-.4,7):
            m.ring(.22,.024,center+[x,.67,0],16,4,tilt=math.pi*.5)


def tree_paths(kind):
    if kind=="living":
        trunk=[(0,0,0),(.35,3.0,.1),(-.35,7.0,.35),(.35,11.0,-.2),(.7,15.4,.2),(1.1,18.2,.4)]
        radii=[1.6,1.28,.95,.67,.4,.11]
        branches=[]
        for i in range(12):
            a=i*2.39996
            h=7.2+(i%4)*2.35
            r=4.8+(i%3)*1.0
            branches.append([(0,h,0),polar(a,r*.33,h+1),polar(a+.12,r*.72,h+2.2),polar(a+.25,r,h+3.7)])
    elif kind=="willow":
        trunk=[(0,0,0),(.18,2.3,.3),(.45,4.5,.08),(.8,7.3,-.4),(.55,9.4,-.55)]
        radii=[.65,.52,.38,.19,.05]
        branches=[]
        for i in range(10):
            a=i*2.39996
            h=4.4+(i%3)*1.0
            branches.append([(.3,h,0),polar(a,2.1,h+2),polar(a,4.2,h+1.8),polar(a+.08,4.6,h+.7)])
        for i in range(3):
            a=i*TAU/3+.6
            branches.append([(.8,7.3,-.4),polar(a,1.5,8.8),polar(a,2.6,9.1),polar(a+.08,3.0,8.7)])
    else:
        trunk=[(0,0,0),(.1,4,0),(-.1,8,.1),(0,12.7,0)]
        radii=[.38,.26,.16,.025]
        branches=[]
        for i in range(18):
            a=i*2.39996
            h=2.0+i*.52
            r=1.75*(1-h/15)
            branches.append([(0,h,0),polar(a,r,h+.7),polar(a+.15,r*.82,h+1.7)])
    return trunk,radii,branches


def build(name, low):
    m=Mesh()
    m.leaf_steps=2 if low else 3
    n=8 if low else 16
    detail=not low
    if name=="column":
        column(m,low,radius=.42)
    elif name=="arcade":
        m.arch(5.4,5.75,.7,.5,low)
        for x in (-2.7,2.7):
            m.box((x,.15,0),(.7,.3,1),.06)
            m.box((x,3.2,0),(.72,.22,.9),.04)
        m.box((0,5.85,0),(6,.3,.9),.06)
        if detail:
            m.arch(5.65,5.85,.12,.12,low,center=(0,0,-.42))
    elif name=="wall_panel":
        m.box((0,2,.15),(6,4,.45),.08)
        for x in (-2.82,2.82):
            column(m,low,(x,0,-.2),4,.14)
        for y in (.15,3.8):
            m.box((0,y,-.15),(6,.2,.5),.04)
        if detail:
            for x in (-1.45,0,1.45):
                m.arch(1.22,2.8,.1,.12,low,center=(x,.5,-.18))
                m.ring(.32,.04,(x,2.75,-.24),24,5,tilt=math.pi*.5)
    elif name=="buttress":
        profile=[(0,0),(1.0,0),(1.0,.35),(.75,.5),(.75,1.4),(.56,1.6),(.56,3.5),(.35,3.8),(.32,5.5),(.20,5.8),(0,6.0)]
        m.lathe(profile,8 if low else 12,center=(0,0,.5))
        pts=[(0,.3,-1.0),(0,1.2,-1.15),(0,2.9,-.7),(0,4.5,.25),(0,5.5,.5)]
        m.sweep(pts,[.42,.38,.3,.2,.14],6 if low else 10)
        m.box((0,.15,0),(1.9,.3,2.0),.08)
        if detail:
            m.shard((0,5.6,.5),.25,.9,sides=6)
    elif name=="capital":
        m.lathe([(0,0),(.55,0),(.62,.2),(.6,.5),(.82,.72),(1,.9),(1,1.1),(0,1.1)],n)
        for i in range(4 if low else 8):
            a=i*TAU/(4 if low else 8)
            pts=[polar(a,.57,.28),polar(a,.85,.6),polar(a,1.0,.82),polar(a,.91,1.02)]
            m.sweep(pts,[.1,.11,.1,.045],5 if low else 8)
            if detail:
                m.leaf(polar(a,.6,.25),polar(a,.95,.8),.14,.08)
    elif name=="rib_vault":
        # An overhead soffit: every rib ends at a corner column capital. Full
        # ground-level arches here would leave hanging piers when raised to eaves.
        # The slight downward bow remains below the matching pitched roof planes.
        segments=10 if low else 24
        for direction in (-1,1):
            points=[]
            for t in np.linspace(0,1,segments+1):
                u=1-abs(2*t-1)
                points.append(np.array([-2.9+5.8*t,3*u-.23*math.sin(math.pi*u),direction*(-2.9+5.8*t)]))
            m.sweep(points,.11,6 if low else 10)
            if detail:
                side=unit((1,0,-direction))*.16
                for sign in (-1,1):
                    trim=[p+side*sign+np.array([0,-.10,0]) for p in points]
                    m.sweep(trim,.035,6)
        m.lathe([(0,2.88),(.24,2.88),(.36,2.94),(.36,3.02),(.25,3.08),(0,3.08)],12 if low else 24)
        if detail:
            for i in range(8):
                a=i*TAU/8
                m.sweep([polar(a,.1,2.88),polar(a,.25,2.85),polar(a,.32,2.94)],.025,5)
    elif name=="balcony":
        m.box((0,-.15,0),(6,.3,3),.07)
        railing(m,low,5.8,(0,0,-1.35))
        for x in (-2.8,2.8):
            p=Mesh();railing(p,low,2.7);m.add(p.transformed(translate=(x,0,0),yaw=math.pi*.5))
            m.sweep([(x,-1.5,1.3),(x,-1.3,.5),(x,-.3,-1.25)],[.15,.15,.1],6 if low else 10)
    elif name=="terrace":
        m.lathe([(0,0),(4,0),(4,.15),(3.9,.23),(3.9,.45),(4,.49),(4,.6),(0,.6)],8,phase=math.pi*.125)
        # The contract is an eight metre square, clipped corners, rather than a circular landing.
        m=Mesh()
        ring=[(-3,-4),(3,-4),(4,-3),(4,3),(3,4),(-3,4),(-4,3),(-4,-3)]
        for i,(x,z) in enumerate(ring):
            xx,zz=ring[(i+1)%8]
            m.quad((x,0,z),(xx,0,zz),(xx,.6,zz),(x,.6,z),(x+xx,0,z+zz))
            m.tri((0,.6,0),(x,.6,z),(xx,.6,zz),(0,1,0))
            m.tri((0,0,0),(xx,0,zz),(x,0,z),(0,-1,0))
            if detail:
                m.rod((x,.12,z),(xx,.12,zz),.035,sides=4)
    elif name=="stair":
        for i in range(12):
            height=(i+1)*.25
            m.box((0,height*.5,-2.75+i*.5),(4,height,.5))
            if detail:
                m.box((0,height-.045,-2.97+i*.5),(4,.09,.06),.015)
    elif name=="portal":
        m.arch(7.3,8.4,1.3,.85,low)
        for x in (-3.65,3.65):
            m.box((x,.25,0),(1.05,.5,1.85),.08)
            m.lathe([(0,0),(.65,0),(.65,.2),(.4,.4),(.4,1.1),(0,1.4)],n,center=(x,7.5,0))
            column(m,low,(x,0,-.7),5.2,.18)
        if detail:
            m.arch(7.7,8.7,.18,.12,low,center=(0,0,-.76))
            m.arch(6.2,7.9,.1,.12,low,center=(0,0,-.77))
        m.shard((0,8.2,0),.35,1.0,sides=6)
    elif name=="spire":
        profile=[(0,0),(1.8,0),(1.8,.4),(1.3,.7),(1.2,2.8),(1.4,3),(1.4,3.3),
                 (1.1,3.6),(1.0,7.8),(1.3,8.0),(1.3,8.5),(.95,8.8),(.6,11.5),(.12,13.8),(0,14)]
        m.lathe(profile,8 if low else 16,fluting=.05)
        for i in range(4):
            a=i*TAU/4
            m.sweep([polar(a,1.7,.4),polar(a,1.6,3),polar(a,.8,8.6)],[.16,.13,.08],5 if low else 8)
        if detail:
            for y in (4.0,5.5,7.0):
                m.ring(1.07,.055,(0,y,0),24,5)
    elif name=="roof":
        # Four swept roof planes, curved corners, individually stepped tile courses.
        bands=4 if low else 10
        sides=4
        for i in range(bands):
            t0,t1=i/bands,(i+1)/bands
            r0,r1=3.5*math.sqrt(2)*(1-t0*.92),3.5*math.sqrt(2)*(1-t1*.92)
            y0,y1=3.9*(t0**.8),3.9*(t1**.8)
            for j in range(sides):
                a,b=j*TAU/sides+math.pi*.25,(j+1)*TAU/sides+math.pi*.25
                p0,p1=polar(a,r0,y0),polar(b,r0,y0)
                q0,q1=polar(a,r1,y1),polar(b,r1,y1)
                m.quad(p0,p1,q1,q0,polar((a+b)*.5,1,1))
                if detail and i:
                    m.rod(p0,p1,.035,sides=4)
        for i in range(4):
            a=i*TAU/4+math.pi*.25
            points=[polar(a,3.5*math.sqrt(2)*(1-t*.92),3.9*t**.8+.05) for t in np.linspace(0,1,5 if low else 12)]
            m.sweep(points,.07,5 if low else 8)
        m.lathe([(.3,3.8),(.35,4),(.15,4.15),(0,4.3)],8)
    elif name=="observatory":
        m.lathe([(0,0),(5.5,0),(5.5,.4),(4.8,.7),(4.8,1.0),(0,1)],12 if low else 24)
        for i in range(6):
            a=i*TAU/6
            column(m,low,polar(a,4.25,.8),7,.27)
            m.sweep([polar(a,4.25,7.8),polar(a,3.8,10.2),polar(a,2.0,12.0),polar(a,.45,12.8)],
                    [.22,.18,.14,.08],6 if low else 10)
        m.ring(4.5,.17,(0,7.9,0),24 if low else 48,6)
        m.ring(3.3,.15,(0,9.3,0),24 if low else 48,6,tilt=.2)
        for tilt in (-.72,.72):
            m.ring(3.2,.12,(0,13.9,0),24 if low else 56,6,tilt=tilt)
        m.rod((0,10.5,0),(0,17.9,0),.12,.05,8)
        if detail:
            for i in range(12):
                a=i*TAU/12
                m.shard(polar(a,4.5,8),.12,.7,sides=4)
    elif name.endswith("_trunk") or name.endswith("_canopy"):
        kind=name.split("_")[0]
        trunk,radii,branches=tree_paths(kind)
        if name.endswith("_trunk"):
            m.sweep(trunk,radii,8 if low else 18,fluting=.08)
            for i,points in enumerate(branches):
                radius=(.48 if kind=="living" else .22 if kind=="willow" else .09)*(1-(i%3)*.13)
                m.sweep(points,np.linspace(radius,.025,len(points)),5 if low else 10)
                if detail and kind!="cypress":
                    end=v3(points[-2]);a=i*2.39996
                    m.sweep([end,end+polar(a+.65,1.2,.6),end+polar(a+.9,2.1,1.1)],[.08,.045,.008],5)
            for i in range(5 if low else 9):
                a=i*2.39996;r=radii[0]
                m.sweep([polar(a,r*.4,1.3),polar(a,r*1.15,.35),polar(a,r*2.25,.05)],
                        [r*.30,r*.20,.025],5 if low else 8)
        else:
            # Individual folded blades need only two sections. Reinvest that
            # triangle budget into a layered crown with actual depth and overlap.
            m.leaf_steps=2
            for i,points in enumerate(branches):
                end=v3(points[-1]);a=i*2.39996
                if kind=="willow":
                    count=3 if low else 7
                    for j in range(count):
                        p=end+polar(j*TAU/count,.7,0)
                        hang=2.0+(j%3)*.6
                        q=p+np.array([.15,-hang,.13])
                        if detail:m.rod(p,q,.012,.006,4)
                        for k in range(3 if low else 7):
                            t=(k+.2)/(3 if low else 7)
                            leafbase=p*(1-t)+q*t
                            m.leaf(leafbase,leafbase+polar(a+k*2.1,.65,-.8),.15,.04)
                    for j in range(6 if low else 26):
                        theta=j*2.39996
                        t=(j+.5)/(6 if low else 26)
                        p=end+polar(theta,math.sqrt(t)*1.15,.55*math.sin(theta*1.7))
                        m.leaf(p,p+polar(theta,1.05,.3*math.cos(theta)),.29,.09)
                elif kind=="cypress":
                    for j in range(5 if low else 30):
                        theta=j*2.39996
                        mix=.45+.55*(j%3)/2
                        p=end*mix+v3(points[-2])*(1-mix)+polar(theta,.35,-.65+.32*math.sin(theta))
                        m.leaf(p,p+polar(theta,.65,1.15),.43,.08,.1)
                else:
                    count=12 if low else 55
                    for j in range(count):
                        t=j/max(1,count-1);theta=j*2.39996
                        mix=.45+.55*(j%3)/2
                        center=end*mix+v3(points[-2])*(1-mix)
                        p=center+polar(theta,math.sqrt(t)*1.9,.85*math.sin(theta*1.7))
                        m.leaf(p,p+polar(theta+.25,1.95,.2+.65*math.cos(theta)),.82,.18,.06)
            if kind=="living":
                for i in range(10 if low else 32):
                    a=i*2.39996
                    p=polar(a,1.3,17.8+.55*math.sin(a*1.7))
                    m.leaf(p,p+polar(a,2.1,.5),.65,.18)
            elif kind=="willow":
                for i in range(4 if low else 9):
                    a=i*2.39996
                    p=np.array([.55,9.1,-.55])+polar(a,.35)
                    m.leaf(p,p+polar(a,1.1,.35),.23,.09)
    elif name=="root_arch":
        for shift in (-.35,.35):
            pts=[(-4,0,shift),(-3.6,2.5,shift),(-2.0,4.9,shift),(0,5.7,shift),(2.0,4.9,shift),(3.6,2.5,shift),(4,0,shift)]
            m.sweep(pts,[.65,.45,.32,.27,.32,.45,.65],7 if low else 14,fluting=.09)
        for sign in (-1,1):
            m.sweep([(sign*3.4,1.8,0),(sign*4.4,.2,-.6),(sign*5.2,.05,-1.1)],[.4,.2,.02],6 if low else 10)
        if detail:
            m.sweep([(-3.5,2,.3),(-2,3,.6),(0,5.7,.3),(2.5,5,.4),(3.8,2.5,.4)],.10,8)
    elif name=="fern":
        for i in range(5 if low else 9):
            a=i*2.39996
            tip=polar(a,1.15,.4+(i%3)*.17)
            pts=[(0,0,0),polar(a,.3,.6),polar(a,.75,.78),tip]
            m.sweep(pts,[.025,.021,.015,.003],4)
            for j in range(3 if low else 7):
                t=(j+1)/(4 if low else 8)
                base=polar(a,1.05*t,math.sin(t*math.pi)*.65+.15)
                for side in (-1,1):
                    m.leaf(base,base+polar(a+side*1.05,.38*(1-t*.65),.03),.07,.025)
    elif name=="mushroom":
        for i,(x,z,h,r) in enumerate(((0,0,1.2,.75),(.65,.35,.65,.45),(-.5,.45,.45,.28))):
            m.sweep([(x,0,z),(x+.04,h*.7,z),(x,h*.9,z)],[r*.18,r*.13,r*.17],6 if low else 12)
            m.lathe([(0,h*.7),(r*.5,h*.7),(r,h*.78),(r*.96,h*.86),(r*.65,h*.98),(0,h*1.03)],
                    10 if low else 24,center=(x,0,z))
            if detail:
                for j in range(12):
                    a=j*TAU/12
                    m.rod(v3((x,h*.72,z))+polar(a,r*.2),v3((x,h*.78,z))+polar(a,r*.9),.012,sides=4)
    elif name=="flower":
        for i in range(3 if low else 7):
            a=i*2.39996;p=polar(a,.35*math.sqrt(i/max(1,6)),0);h=.5+(i%3)*.15
            end=p+np.array([.08,h,.02])
            m.rod(p,end,.012,.008,4)
            for j in range(5):
                theta=j*TAU/5
                m.leaf(end,end+polar(theta,.24,.08),.10,.04)
            m.lathe([(0,0),(.06,.015),(.045,.07),(0,.09)],6,center=end)
            if detail:m.leaf(p+[0,.2,0],p+polar(a,.25,.4),.08,.02)
    elif name=="reeds":
        for i in range(5 if low else 12):
            a=i*2.39996;p=polar(a,.55*math.sqrt((i+.3)/12));h=1.1+(i%4)*.18
            end=p+polar(a,.13,h)
            m.sweep([p,p+[0,h*.65,0],end],[.015,.014,.009],4)
            m.lathe([(0,0),(.05,.06),(.055,.3),(0,.36)],6,center=end)
            for j in range(1 if low else 3):
                base=p+[0,.25+j*.2,0]
                m.leaf(base,base+polar(a+j,.5,.6),.045,.08)
    elif name in ("rock","ore"):
        if name=="rock":
            rng=np.random.default_rng(414)
            sides=7 if low else 13
            rings=[]
            for k,(h,r) in enumerate(((0,1.3),(.35,1.8),(1.0,1.45),(1.55,.85),(1.8,.3))):
                rings.append([polar(i*TAU/sides,r*(.85+rng.random()*.28),h+rng.random()*.1) for i in range(sides)])
            for j in range(4):
                for i in range(sides):
                    k=(i+1)%sides
                    m.quad(rings[j][i],rings[j][k],rings[j+1][k],rings[j+1][i],polar((i+.5)*TAU/sides,1,.25))
            for i in range(sides):
                m.tri((0,1.86,0),rings[-1][i],rings[-1][(i+1)%sides],(0,1,0))
                m.tri((0,0,0),rings[0][(i+1)%sides],rings[0][i],(0,-1,0))
        else:
            for i in range(4 if low else 9):
                a=i*2.39996
                m.shard(polar(a,.65*(i%3)/2),.18+(i%3)*.1,.65+(i%4)*.35,polar(a,.35),5 if low else 7)
    elif name=="drill":
        m.lathe([(0,0),(2.7,0),(2.7,.5),(2.4,.8),(2.4,1.4),(1.7,1.8),(1.7,3.4),(0,3.4)],8 if low else 16)
        for sign in (-1,1):
            m.sweep([(sign*2.7,.4,0),(sign*2.7,6,0),(sign*1.7,8.1,0)],[.4,.28,.2],6 if low else 10)
            m.rod((sign*2.7,1.1,-.8),(sign*2.7,6,.8),.09,sides=6)
        m.rod((-2.7,6.1,0),(2.7,6.1,0),.3,sides=8)
        m.lathe([(0,3.1),(.15,3.1),(.95,5.8),(1,7.2),(.7,8.2),(.7,10.5),(.95,10.7),(.95,11.1),(0,11.2)],n)
        segments=24 if low else 72
        points=[polar(t*TAU*3,.12+.8*t,3.2+3.8*t) for t in np.linspace(0,1,segments)]
        m.sweep(points,.15 if low else .18,5 if low else 8)
        if detail:
            for i in range(8):
                a=i*TAU/8
                m.box(polar(a,2.4,.9),(.45,.6,.45),.04)
            for h in (8.4,9.2,10):m.ring(.76,.08,(0,h,0),24,6)
    elif name=="monolith":
        m.lathe([(0,0),(1.4,0),(1.4,.25),(1.15,.4),(0,.4)],8)
        m.shard((0,.3,0),1.0,5.8,(.35,0,.1),5 if low else 7)
        for i in range(2 if low else 5):
            y=1.0+i*.72
            m.ring(.22,.035,(.06,y,-.78),12 if low else 24,4,tilt=math.pi*.5)
    elif name=="fountain":
        m.lathe([(0,0),(2.9,0),(2.9,.2),(2.65,.35),(2.65,.7),(2.45,.8),(2.25,.62),(2.25,.3),(0,.3)],16 if low else 40)
        m.lathe([(0,.3),(.65,.3),(.55,.6),(.4,1.5),(.9,1.8),(1.7,2.0),(1.7,2.18),(1.5,2.23),(.4,1.95),
                 (.28,2.8),(.7,3.1),(1.1,3.2),(1.1,3.38),(.95,3.4),(.2,3.15),(.18,4.0),(0,4.15)],16 if low else 32)
        for i in range(4 if low else 8):
            a=i*TAU/(4 if low else 8)
            m.sweep([polar(a,.35,.6),polar(a,.8,1.0),polar(a,1.15,1.8)],[.10,.12,.06],6 if low else 10)
        m.shard((0,4.0,0),.28,.7,sides=6)
    elif name=="lantern":
        column(m,low,height=2.2,radius=.075)
        m.sweep([(0,2.0,0),(.3,2.5,0),(.72,2.55,0),(.85,2.2,0)],[.035]*4,6)
        for y,r in ((1.4,.23),(2.05,.27)):
            m.lathe([(0,0),(r,0),(r,.07),(r*.7,.18),(0,.18)],6,center=(.85,y,0))
        for i in range(6):
            a=i*TAU/6
            m.rod(polar(a,.19,1.55)+[.85,0,0],polar(a,.22,2.05)+[.85,0,0],.018,sides=4)
        if detail:m.ring(.09,.015,(.85,2.25,0),16,4,tilt=math.pi*.5)
    elif name=="banner":
        cols,rows=(3,5) if low else (10,18)
        for i in range(cols):
            for j in range(rows):
                def p(x,y):
                    xx=(x-.5)*1.8
                    bottom=3.2-.5*abs(x-.5)*2
                    return (xx,(1-y)*bottom,.12*math.sin(x*TAU*1.5+y*4)*math.sin(y*math.pi*.6))
                x,y=i/cols,j/rows;xx,yy=(i+1)/cols,(j+1)/rows
                points=[p(x,y),p(xx,y),p(xx,yy),p(x,yy)]
                uv=[(x,1-y),(xx,1-y),(xx,1-yy),(x,1-yy)]
                m.quad(*points,(0,0,-1),uvs=uv);m.quad(*points,(0,0,1),uvs=uv)
    elif name=="market_awning":
        cols,rows=(4,3) if low else (16,8)
        for i in range(cols):
            for j in range(rows):
                def p(x,y):return ((x-.5)*4.8,2.5+.65*math.sin(y*math.pi*.65)-.15*math.sin(x*TAU*4)**2,(y-.5)*3)
                x,y=i/cols,j/rows;xx,yy=(i+1)/cols,(j+1)/rows
                points=[p(x,y),p(xx,y),p(xx,yy),p(x,yy)]
                uv=[(x,y),(xx,y),(xx,yy),(x,yy)]
                m.quad(*points,(0,1,0),uvs=uv);m.quad(*points,(0,-1,0),uvs=uv)
        # Scalloped valance, separate silhouette from the supporting shop architecture.
        for i in range(8):
            a,b=-2.4+i*.6,-2.4+(i+1)*.6
            m.quad((a,2.5,-1.5),(b,2.5,-1.5),(b,2.22,-1.5),((a+b)*.5,2.1,-1.5),(0,0,-1))
            m.tri((a,2.5,-1.5),((a+b)*.5,2.1,-1.5),(a,2.22,-1.5),(0,0,-1))
    elif name=="railing":
        railing(m,low)
    elif name=="guardian":
        # Original winged mineral sentinel relief with sculpted head and angular folded wings.
        m.lathe([(0,0),(1.6,0),(1.6,.3),(1.3,.55),(1.3,.7),(0,.7)],8 if low else 16)
        m.lathe([(0,.7),(.7,.7),(.8,1.1),(.53,3.4),(.8,4.5),(.95,4.9),(.4,5.3),(.3,5.8),
                 (.48,6),(.52,6.6),(.3,6.95),(0,7.1)],8 if low else 12,fluting=.08)
        for sign in (-1,1):
            m.sweep([(sign*.7,4.9,0),(sign*1.2,4.0,-.25),(sign*.65,3.5,-.6)],[.22,.18,.14],6 if low else 10)
            for i in range(3 if low else 7):
                t=i/(3 if low else 7)
                m.leaf((sign*.6,4.8,.2),(sign*(1.6+t*.8),6.5-t*3.2,.3+t*.35),.35,.13)
            m.shard((sign*.22,6.85,0),.13,.75,(sign*.18,0,0),5)
        m.rod((0,.7,-.7),(0,5.1,-.7),.09,sides=6)
        m.ring(.6,.05,(0,6.55,.3),20 if low else 36,5,tilt=math.pi*.5)
    elif name=="sky_ring":
        m.ring(3.65,.11,(0,0,0),32 if low else 64,6 if low else 10)
        m.ring(3.3,.055,(0,0,0),24 if low else 56,5)
        for i in range(8 if low else 16):
            a=i*TAU/(8 if low else 16)
            m.rod(polar(a,3.28),polar(a,3.72),.045,sides=5)
            m.shard(polar(a,3.64),.10,.45,sides=4)
        if detail:
            for i in range(8):
                a=i*TAU/8
                m.ring(.2,.025,polar(a,3.49),16,4)
    elif name=="bench":
        for z in (-.35,-.12,.12,.35):
            m.box((0,.48,z),(2.3,.10,.20),.025)
        for x in (-.85,.85):
            m.sweep([(x,0,-.4),(x,.5,-.35),(x,1.0,.45)],[.09,.08,.06],6)
            m.rod((x,.05,.4),(x,.48,.35),.08,sides=6)
        for y in (.8,1.05):m.box((0,y,.45),(2.3,.16,.09),.025)
    elif name=="planter":
        m.lathe([(0,0),(.8,0),(.8,.15),(.65,.25),(.78,1.0),(1.0,1.05),(1.0,1.25),(.83,1.25),(.68,.3),(0,.3)],
                8 if low else 20)
        if detail:
            for i in range(10):
                a=i*TAU/10
                m.sweep([polar(a,.7,.3),polar(a,.78,.7),polar(a,.89,1.05)],[.04,.055,.04],5)
    elif name=="threshold":
        m.box((0,.1,0),(3.8,.2,1.7),.05)
        m.box((0,.25,.3),(3.4,.1,1.0),.035)
        if detail:
            for x in (-1.4,1.4):
                m.ring(.18,.025,(x,.32,.3),20,4)
    elif name=="ruin":
        for x in (-2,2):column(m,low,(x,0,0),3.1 if x<0 else 1.8,.35)
        part=Mesh();part.arch(4,4.3,.6,.5,low)
        # Remove triangles above a stepped break to produce an actually damaged silhouette.
        for i in range(0,len(part.vertices),3):
            tri=part.vertices[i:i+3];c=np.mean(np.asarray(tri)[:,:3],axis=0)
            if c[0]<-.3 or (c[0]>1.3 and c[1]<2):m.vertices.extend(tri)
        for i in range(4 if low else 9):
            rock=Mesh();rock.box((0,.15,0),(.45,.3,.42),.06)
            m.add(rock.transformed(translate=(math.sin(i*2.4)*2.2,0,math.cos(i*2.4)*.65),yaw=i*.7))
    elif name=="vine":
        points=[(.25*math.sin(t*8),t*4,.12*math.cos(t*6)) for t in np.linspace(0,1,8 if low else 18)]
        m.sweep(points,.025,5)
        for i in range(6 if low else 18):
            t=(i+.3)/(6 if low else 18)
            p=np.array([.25*math.sin(t*8),t*4,.12*math.cos(t*6)])
            m.leaf(p,p+np.array([(-1 if i%2 else 1)*.43,.2,-.1]),.16,.04,.15)
    elif name=="sigil":
        m.ring(.9,.06,(0,1,0),24 if low else 48,6,tilt=math.pi*.5)
        for side in (-1,1):
            m.sweep([(0,.1,0),(side*.62,.8,0),(side*.36,1.55,0),(0,1.9,0)],.055,6)
        m.rod((0,.25,0),(0,1.65,0),.06,sides=6)
        if detail:
            for i in range(3):
                y=.55+i*.35
                m.rod((-.32,y,0),(.32,y+.18,0),.025,sides=5)
    elif name=="ward_pylon":
        m.lathe([(0,0),(1,0),(1,.25),(.78,.45),(.52,1.6),(.62,2.2),(.82,2.4),(.82,2.65),(.5,2.75),(0,2.75)],n)
        for i in range(4):
            a=i*TAU/4
            m.sweep([polar(a,.7,2.2),polar(a,.9,3.0),polar(a,.48,3.8)],[.1,.1,.04],6 if low else 10)
        if detail:m.ring(.85,.06,(0,2.6,0),32,6)
    elif name=="bridge":
        # Eight metre accessible deck, upward arc .4m, rails and hanging pointed braces.
        segments=8 if low else 16
        for i in range(segments):
            z=-4+i*8/segments;zn=-4+(i+1)*8/segments
            y=.4*math.cos(z*math.pi/8);yn=.4*math.cos(zn*math.pi/8)
            m.quad((-1.5,y,z),(1.5,y,z),(1.5,yn,zn),(-1.5,yn,zn),(0,1,0))
            m.quad((-1.5,y-.25,z),(-1.5,yn-.25,zn),(1.5,yn-.25,zn),(1.5,y-.25,z),(0,-1,0))
            for s in (-1,1):
                m.quad((s*1.5,y,z),(s*1.5,yn,zn),(s*1.5,yn-.25,zn),(s*1.5,y-.25,z),(s,0,0))
        for s in (-1,1):
            pts=[(s*1.5,.4*math.cos(z*math.pi/8)+1.1,z) for z in np.linspace(-4,4,9)]
            m.sweep(pts,.065,6)
            for z in np.linspace(-4,4,5 if low else 9):
                y=.4*math.cos(z*math.pi/8)
                m.rod((s*1.5,y,z),(s*1.5,y+1.1,z),.045,sides=6)
            m.sweep([(s*1.4,-1.8,-4),(s*1.4,-.75,-2),(s*1.4,-.2,0),(s*1.4,-.75,2),(s*1.4,-1.8,4)],.14,6 if low else 10)
    elif name=="tower":
        m.lathe([(0,0),(3,0),(3,.4),(2.55,.8),(2.35,7.8),(2.7,8.2),(2.7,8.6),(2.3,8.8),
                 (2.25,10.7),(2.7,10.9),(2.7,11.3),(0,11.3)],8 if low else 16)
        for i in range(8):
            a=i*TAU/8
            m.box(polar(a,2.4,11.65),(.6,.7,.6),.05)
            if detail:
                m.rod(polar(a,2.5,.7),polar(a,2.4,8.2),.11,.07,8)
        if detail:
            for h in (2.6,5,7.2):m.ring(2.4,.06,(0,h,0),32,5)
    elif name=="window":
        m.arch(2,3.8,.22,.16,low)
        m.rod((0,.12,0),(0,3.5,0),.045,sides=6)
        m.rod((-.8,1.4,0),(.8,1.4,0),.045,sides=6)
        m.box((0,.09,0),(2.2,.18,.4),.03)
        m.ring(.33,.04,(0,2.85,0),16 if low else 32,5,tilt=math.pi*.5)
        if detail:
            for x in (-.45,.45):
                m.arch(.68,2.3,.1,.055,low,center=(x,.12,0))
    elif name=="crystal_cluster":
        m.shard((0,0,0),.65,4.2,(.25,0,.15),6 if low else 8)
        for i in range(4 if low else 8):
            a=i*2.39996
            m.shard(polar(a,.65+(i%2)*.2),.22+(i%2)*.12,1.3+(i%3)*.55,polar(a,.4),5 if low else 6)
    elif name=="roof_finial":
        m.lathe([(0,0),(.65,0),(.65,.12),(.4,.3),(.32,.9),(.55,1.2),(.2,1.5),(.1,2.4),(0,2.6)],n)
        for a in (0,math.pi*.5,math.pi,math.pi*1.5):
            m.sweep([polar(a,.3,.7),polar(a,.9,1.2),polar(a,.55,1.9),polar(a,.12,2.3)],[.065,.075,.05,.03],6)
    else:
        raise ValueError(name)
    return m


def export_mesh(name, low, mesh):
    variant=name+("_low" if low else "_high")
    data=np.asarray(mesh.vertices,dtype="<f4")
    if len(data)==0 or len(data)%3 or len(data)>300000:
        raise ValueError("Invalid vertex count: "+variant)
    if not np.isfinite(data).all():
        raise ValueError("Nonfinite data: "+variant)
    lengths=np.linalg.norm(data[:,3:6],axis=1)
    if np.max(abs(lengths-1))>1e-5:
        raise ValueError("Nonunit normals: "+variant)
    triangles=data[:,:3].reshape((-1,3,3))
    areas=np.linalg.norm(np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0]),axis=1)*.5
    if min(areas)<1e-10:
        raise ValueError("Degenerate triangle: "+variant)
    binary=ASSETS/"Meshes"/(variant+".bytes")
    payload=b"CMH1"+struct.pack("<i",len(data))+data.tobytes()
    binary_temporary=binary.with_suffix(".bytes.tmp")
    binary_temporary.write_bytes(payload)
    binary_temporary.replace(binary)
    meta(binary)
    obj=ASSETS/"Models"/(variant+".obj")
    obj_temporary=obj.with_suffix(".obj.tmp")
    with obj_temporary.open("w") as stream:
        stream.write("# Original Visual Rebirth 04A. Metres Y-up. Material role: "+MODEL_ROLES[name]+"\n")
        stream.write("o "+variant+"\n")
        for row in data:stream.write("v %.7f %.7f %.7f\n"%tuple(row[:3]))
        for row in data:stream.write("vt %.7f %.7f\n"%tuple(row[6:]))
        for row in data:stream.write("vn %.7f %.7f %.7f\n"%tuple(row[3:6]))
        for i in range(0,len(data),3):stream.write("f "+" ".join(f"{k}/{k}/{k}" for k in range(i+1,i+4))+"\n")
    obj_temporary.replace(obj)
    meta(obj,"obj")
    bounds_min=data[:,:3].min(axis=0)
    bounds_max=data[:,:3].max(axis=0)
    return {
        "name":name,"variant":variant,"lod":1 if low else 0,"role":MODEL_ROLES[name],
        "vertices":len(data),"triangles":len(data)//3,"bounds_min":bounds_min.tolist(),
        "bounds_max":bounds_max.tolist(),"dimensions":(bounds_max-bounds_min).tolist(),
        "pivot":"deck center" if name in ("balcony","bridge") else "ground center",
        "uv":"metric tiled UV, or individual full leaf/fabric UV",
        "normals":"explicit unit smooth and hard-edge normals",
        "minimum_triangle_area":float(min(areas)),"sha256":hashlib.sha256(payload).hexdigest(),
        "binary":binary.relative_to(ROOT).as_posix(),"source":obj.relative_to(ROOT).as_posix(),
    }


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--confirm-regenerate",action="store_true",help="Regenerate only original 04A mesh outputs.")
    parser.add_argument("--only",nargs="*",choices=list(MODEL_ROLES),help="Generate selected models for authoring iteration.")
    args=parser.parse_args()
    if not args.confirm_regenerate:parser.error("Pass --confirm-regenerate after preserving any local artist changes.")
    for directory in (ASSETS/"Models",ASSETS/"Meshes"):
        directory.mkdir(parents=True,exist_ok=True)
        meta(directory,"folder")
    records=[]
    for name in args.only or MODEL_ROLES:
        pair=[]
        for low in (False,True):
            record=export_mesh(name,low,build(name,low))
            records.append(record)
            pair.append(record["triangles"])
        print(f"{name:19s} LOD0 {pair[0]:5d} triangles, LOD1 {pair[1]:5d} triangles",flush=True)
    if not args.only:
        inventory={
            "version":"04A","units":"metres","up_axis":"Y","unique_prototypes":len(MODEL_ROLES),
            "variants":len(records),"license":"Original project-owned procedural artwork. No external asset sources.",
            "runtime_format":"CMH1: 4 byte magic, int32 vertex count, float32 position3 normal3 uv2; sequential triangles.",
            "coordinate_contracts":{
                "stair":"12 steps, x -2..2, y 0..3, z -3..3; ascending toward +Z.",
                "terrace":"Octagonal clipped-square deck x/z -4..4, top y .6.",
                "trees":"Trunk and canopy names for one species share the same base-center pivot. Scale both identically.",
                "balcony":"Top of deck at y0; support braces extend below; rail extends above.",
                "bridge":"8m along Z, width 3m; top deck arches from y0 at ends to y.4 in middle.",
                "sky_ring":"Ring plane XZ, centered y0. Rotate X90 for vertical orientation.",
                "frontages":"Arcade, portal, window, wall, sigil face -Z. Apply yaw at placement.",
                "rib_vault":"Overhead diagonal soffit ribs only: four endpoint centers x/z +/-2.9 at y0, central boss y3.08. Place endpoints on column capitals, never ground. No vertical piers.",
            },
            "models":records,
            "totals":{"lod0_triangles":sum(r["triangles"] for r in records if not r["lod"]),
                      "lod1_triangles":sum(r["triangles"] for r in records if r["lod"]),
                      "binary_bytes":sum(8+r["vertices"]*32 for r in records)},
        }
        path=ASSETS/"Models/model_inventory.json"
        path.write_text(json.dumps(inventory,indent=2)+"\n")
        meta(path)
        print(json.dumps(inventory["totals"]))


if __name__=="__main__":
    main()
