"""Convert an owner MPFB character (.blend with the MPFB default rig) into a live crowd drop-in folder.

Run with Blender 4.2 or newer (or the bpy module):
    blender -b --python tools/convert_mpfb_crowd_dropin.py -- <character.blend> <out folder> <recipe.json>

Recipes live in tools/mpfb_recipes/. What the tool does, in order:
1. Opens the file with the rig in its rest position, drops the Subdivision modifiers, bakes the shape key mix and
   applies the Mask modifiers (so the skin hidden under clothing is cut, as MPFB shows it).
2. Bakes every object transform into its mesh, stands the character with the hips over the origin and the lowest
   vertex on the ground, and optionally scales it so the head bone sits at the recipe's height.
3. Builds the 53-bone H9 skeleton (the MalikRig names of Docs/LIVE_CHARACTERS.md) on the MPFB joint positions and
   folds the MPFB weights onto it (face, jaw, eyes and tongue go to Head; the shoulder blade is shared between the
   clavicle and the upper arm; helper groups are ignored), then normalizes them (at most four bones per vertex).
4. Optionally borrows the eyes of another MPFB file (moved and sized onto this head's eye bones) and makes garments
   from the body's own surface: the faces a garment covers are copied, pushed out along the normals and keep the
   body's weights exactly, so they move with the body and never cut through it; the skin left fully inside a garment
   is removed.
5. Lowers heavy pieces to the recipe's vertex budget, renames each piece's material after the recipe and writes its
   textures as <Material>_Albedo.png and <Material>_Normal.png. A piece may be painted instead (an original design
   painted in its UV space from each texel's place on the body).
6. Exports the FBX with the project's settings and writes livecast.json and CREDITS.txt from the recipe.
"""
import json
import os
import sys

import bpy
import bmesh
import mathutils
import numpy as np

H9_FINGERS = {1: 'thumb', 2: 'index', 3: 'middle', 4: 'ring', 5: 'pinky'}
# The average sRGB colour of the shared wool map the crowd tint is tuned for (LiveCrowdRules.ClothBase).
CLOTH_BASE = np.array([0.103, 0.111, 0.134], dtype=np.float32)


def args():
    argv = sys.argv
    argv = argv[argv.index('--') + 1:] if '--' in argv else argv[1:]
    if len(argv) != 3:
        raise SystemExit('usage: -- <character.blend> <out folder> <recipe.json>')
    return argv


def target_bone(name, parents):
    """The H9 bone an MPFB bone's weights fold into (a pair splits evenly), or None for helper groups."""
    if name not in parents:
        return None
    base, side = name, ''
    if name.endswith('.L') or name.endswith('.R'):
        base, side = name[:-2], '_' + name[-1].lower()
    if name in ('root', 'pelvis.L', 'pelvis.R', 'spine05'):
        return 'pelvis'
    if name == 'spine04':
        return 'spine_01'
    if name == 'spine03':
        return 'spine_02'
    if name in ('spine02', 'spine01') or base == 'breast':
        return 'spine_03'
    if name in ('neck01', 'neck02'):
        return 'neck_01'
    if base == 'clavicle':
        return 'clavicle' + side
    if base == 'shoulder01':
        return ('clavicle' + side, 'upperarm' + side)
    if base in ('upperarm01', 'upperarm02'):
        return 'upperarm' + side
    if base in ('lowerarm01', 'lowerarm02'):
        return 'lowerarm' + side
    if base == 'wrist' or base.startswith('metacarpal'):
        return 'hand' + side
    if base.startswith('finger') and '-' in base:
        digit, joint = base[6:].split('-')
        return '%s_%02d%s' % (H9_FINGERS[int(digit)], int(joint), side)
    if base in ('upperleg01', 'upperleg02'):
        return 'thigh' + side
    if base in ('lowerleg01', 'lowerleg02'):
        return 'calf' + side
    if base == 'foot':
        return 'foot' + side
    if base.startswith('toe'):
        return 'ball' + side
    # Everything that hangs under the top of the neck or the head (jaw, tongue, eyes, face muscles) rides the head.
    if name in ('neck03', 'head'):
        return 'Head'
    chain = parents.get(name)
    while chain is not None:
        if chain in ('neck03', 'head'):
            return 'Head'
        chain = parents.get(chain)
    return None


def override(obj):
    return bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj], selected_editable_objects=[obj])


def bake_piece(obj):
    """Subdivision dropped, shape keys and masks applied, the transform baked: plain rest geometry with its weights."""
    for m in list(obj.modifiers):
        if m.type in ('SUBSURF', 'ARMATURE'):
            obj.modifiers.remove(m)
    if obj.data.shape_keys is not None:
        with override(obj):
            bpy.ops.object.shape_key_remove(all=True, apply_mix=True)
    for m in list(obj.modifiers):
        if m.type == 'MASK':
            with override(obj):
                bpy.ops.object.modifier_apply(modifier=m.name)
        else:
            obj.modifiers.remove(m)
    matrix = obj.matrix_world.copy()
    obj.parent = None
    obj.data.transform(matrix)
    obj.matrix_world = mathutils.Matrix.Identity(4)


def find_image(material, normal):
    """The colour map (fed into the MPFB colour mix or the shader) or the normal map of a material."""
    if material is None or material.node_tree is None:
        return None
    for node in material.node_tree.nodes:
        if node.type != 'TEX_IMAGE' or node.image is None:
            continue
        uses = [link.to_node.type for link in node.outputs[0].links]
        if normal and 'NORMAL_MAP' in uses:
            return node.image
        if not normal and ('MIX_RGB' in uses or 'MIX' in uses or 'BSDF_PRINCIPLED' in uses):
            return node.image
    return None


def save_image(image, path, size, keep_alpha):
    copy = image.copy()
    if max(copy.size[0], copy.size[1]) > size:
        copy.scale(size, size)
    scene = bpy.context.scene
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA' if keep_alpha else 'RGB'
    copy.save_render(path, scene=scene)
    bpy.data.images.remove(copy)


def write_pixels(rgb, path):
    size = rgb.shape[0]
    rgba = np.ones((size, size, 4), dtype=np.float32)
    rgba[:, :, :3] = np.clip(rgb, 0.0, 1.0)
    image = bpy.data.images.new('painted', size, size, alpha=False)
    image.pixels.foreach_set(rgba.ravel())
    scene = bpy.context.scene
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGB'
    image.save_render(path, scene=scene)
    bpy.data.images.remove(image)


# ------------------------------------------------------------------ painting in UV space

def bake_maps(obj, size):
    """Per texel of the piece's UV map: the 3D position and normal of the surface there, and whether any face covers it."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    uvl = mesh.uv_layers.active.data
    co = np.array([v.co[:] for v in mesh.vertices], dtype=np.float32)
    vn = np.array([v.normal[:] for v in mesh.vertices], dtype=np.float32)
    pos = np.zeros((size, size, 3), dtype=np.float32)
    nrm = np.zeros((size, size, 3), dtype=np.float32)
    mask = np.zeros((size, size), dtype=bool)
    for tri in mesh.loop_triangles:
        uv = np.array([uvl[i].uv[:] for i in tri.loops], dtype=np.float64) * size
        idx = list(tri.vertices)
        x0, x1 = int(max(0, np.floor(uv[:, 0].min()))), int(min(size - 1, np.ceil(uv[:, 0].max())))
        y0, y1 = int(max(0, np.floor(uv[:, 1].min()))), int(min(size - 1, np.ceil(uv[:, 1].max())))
        if x1 < x0 or y1 < y0:
            continue
        (ax, ay), (bx, by), (cx, cy) = uv
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12:
            continue
        X, Y = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        w0 = ((by - cy) * (X - cx) + (cx - bx) * (Y - cy)) / den
        w1 = ((cy - ay) * (X - cx) + (ax - cx) * (Y - cy)) / den
        w2 = 1.0 - w0 - w1
        inside = (w0 >= -0.01) & (w1 >= -0.01) & (w2 >= -0.01)
        if not inside.any():
            continue
        W = np.stack([w0[inside], w1[inside], w2[inside]], axis=1).astype(np.float32)
        rows = (Y[inside] - 0.5).astype(int)
        cols = (X[inside] - 0.5).astype(int)
        pos[rows, cols] = W @ co[idx]
        nrm[rows, cols] = W @ vn[idx]
        mask[rows, cols] = True
    length = np.linalg.norm(nrm, axis=2, keepdims=True)
    nrm = nrm / np.maximum(length, 1e-6)
    return pos, nrm, mask


def dilate(rgb, mask, steps=6):
    """Spreads painted texels into the empty ones around them so mip levels and seams never pull in black."""
    rgb = rgb.copy()
    filled = mask.copy()
    for _ in range(steps):
        grow = np.zeros_like(rgb)
        count = np.zeros(mask.shape, dtype=np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            shifted = np.roll(np.roll(rgb, dy, axis=0), dx, axis=1)
            ok = np.roll(np.roll(filled, dy, axis=0), dx, axis=1) & ~filled
            grow[ok] += shifted[ok]
            count[ok] += 1
        new = count > 0
        rgb[new] = grow[new] / count[new][:, None]
        filled |= new
    return rgb


def weave(size, seed, scale):
    rng = np.random.default_rng(seed)
    fine = rng.normal(0.0, 1.0, (size, size)).astype(np.float32)
    coarse = rng.normal(0.0, 1.0, (size // 8, size // 8)).astype(np.float32)
    coarse = np.kron(coarse, np.ones((8, 8), dtype=np.float32))
    return 1.0 + scale * (0.6 * fine + 0.4 * coarse)


def paint(obj, kind, size, palette, body_height):
    """An original picture for a piece, decided per texel from its height on the body (rel), side and facing."""
    pos, nrm, mask = bake_maps(obj, size)
    rel = pos[:, :, 2] / max(0.01, body_height)
    x = pos[:, :, 0]
    front = nrm[:, :, 1] < -0.35
    side = np.abs(nrm[:, :, 0]) > 0.85
    c = {k: np.array(v, dtype=np.float32) for k, v in palette.items()}
    rgb = np.zeros((size, size, 3), dtype=np.float32)
    rgb[:] = c['base']

    def put(where, colour):
        rgb[where] = colour

    if kind == 'runner-suit':
        # Only height bands: a suit mesh may share one UV area between its front and back (or both sides), so a
        # pattern that depends on facing or side would tear. Teal body, charcoal hood and collar, charcoal boots.
        put(rel < 0.075, c['hood'])
        put(rel > 0.84, c['hood'])
        rgb *= weave(size, 3, 0.02)[:, :, None]
    elif kind == 'jacket':
        put(front & (np.abs(x) < 0.007) & (rel > 0.56), c['zip'])
        put(side & (rel < 0.78), c['seam'])
        put((rel > 0.78) & (nrm[:, :, 2] > 0.45), c['yoke'])
        upper_arm = (np.abs(x) > 0.24 * body_height / 1.8) & (rel > 0.66) & (rel < 0.74)
        put(upper_arm, c['accent'])
        rgb *= weave(size, 5, 0.06)[:, :, None]
    elif kind == 'trousers':
        knee = front & (rel > 0.24) & (rel < 0.31)
        put(knee, c['pad'])
        pocket = (np.abs(nrm[:, :, 0]) > 0.6) & (rel > 0.38) & (rel < 0.47)
        put(pocket, c['pocket'])
        edge = (np.abs(nrm[:, :, 0]) > 0.6) & ((np.abs(rel - 0.38) < 0.003) | (np.abs(rel - 0.47) < 0.003))
        put(edge, c['seam'])
        rgb *= weave(size, 7, 0.07)[:, :, None]
    elif kind == 'boots':
        put(rel < 0.012, c['sole'])
        laces = front & (np.abs(nrm[:, :, 0]) < 0.4) & (rel > 0.035) & (rel < 0.12) & (np.mod(rel * 200.0, 2.0) < 0.5)
        put(laces, c['lace'])
        rgb *= weave(size, 9, 0.08)[:, :, None]
    elif kind == 'vest':
        plates = (np.mod(pos[:, :, 2] * 9.0, 1.0) < 0.06) | (np.abs(x) < 0.004)
        put(plates, c['seam'])
        put(side, c['strap'])
        rgb *= weave(size, 11, 0.05)[:, :, None]
    else:
        raise SystemExit('unknown painted look ' + kind)
    if palette.get('match_cloth_base'):
        # The crowd tint multiplies the coat by a colour tuned for the shared wool map; land on its average.
        mean = rgb[mask].mean(axis=0)
        rgb *= (CLOTH_BASE / np.maximum(mean, 1e-3))[None, None, :]
    return dilate(rgb, mask)


# ------------------------------------------------------------------ skeleton and weights

def build_rig(joints):
    """The MalikRig armature on the MPFB joints (53 bones, the names of the H9 skeleton)."""
    data = bpy.data.armatures.new('MalikRig')
    rig = bpy.data.objects.new('MalikRig', data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    with override(rig):
        bpy.ops.object.mode_set(mode='EDIT')
    bones = {}

    def bone(name, head, tail, parent):
        b = data.edit_bones.new(name)
        b.head, b.tail = head, tail
        if (b.tail - b.head).length < 0.002:
            b.tail = b.head + mathutils.Vector((0, 0, 0.02))
        if parent:
            b.parent = bones[parent]
        bones[name] = b

    J = joints
    hip = J['spine05'][0]
    bone('root', mathutils.Vector((0, 0, 0)), mathutils.Vector((0, hip.z, 0)), None)
    bone('pelvis', hip, J['spine05'][1], 'root')
    bone('spine_01', J['spine04'][0], J['spine04'][1], 'pelvis')
    bone('spine_02', J['spine03'][0], J['spine03'][1], 'spine_01')
    bone('spine_03', J['spine02'][0], J['spine01'][1], 'spine_02')
    bone('neck_01', J['neck01'][0], J['head'][0], 'spine_03')
    bone('Head', J['head'][0], J['head'][1], 'neck_01')
    for s, m in (('l', 'L'), ('r', 'R')):
        bone('clavicle_' + s, J['clavicle.' + m][0], J['upperarm01.' + m][0], 'spine_03')
        bone('upperarm_' + s, J['upperarm01.' + m][0], J['lowerarm01.' + m][0], 'clavicle_' + s)
        bone('lowerarm_' + s, J['lowerarm01.' + m][0], J['wrist.' + m][0], 'upperarm_' + s)
        bone('hand_' + s, J['wrist.' + m][0], J['finger3-1.' + m][0], 'lowerarm_' + s)
        for digit, word in H9_FINGERS.items():
            parent = 'hand_' + s
            for joint in (1, 2, 3):
                key = 'finger%d-%d.%s' % (digit, joint, m)
                name = '%s_%02d_%s' % (word, joint, s)
                bone(name, J[key][0], J[key][1], parent)
                parent = name
        bone('thigh_' + s, J['upperleg01.' + m][0], J['lowerleg01.' + m][0], 'pelvis')
        bone('calf_' + s, J['lowerleg01.' + m][0], J['foot.' + m][0], 'thigh_' + s)
        bone('foot_' + s, J['foot.' + m][0], J['toe1-1.' + m][0], 'calf_' + s)
        toe_tail = J['toe1-1.' + m][1].copy()
        toe_tail.z = max(toe_tail.z, 0.01)
        bone('ball_' + s, J['toe1-1.' + m][0], toe_tail, 'foot_' + s)
    with override(rig):
        bpy.ops.object.mode_set(mode='OBJECT')
    return rig


def remap_weights(obj, parents, rig_names):
    """Folds the MPFB groups onto the H9 bones, at most four per vertex, normalized; helper-only vertices borrow
    the weights of the nearest weighted vertex."""
    mesh = obj.data
    old = {g.index: g.name for g in obj.vertex_groups}
    per_vertex = []
    for v in mesh.vertices:
        acc = {}
        for g in v.groups:
            name = old.get(g.group)
            if name is None or g.weight <= 0.0:
                continue
            target = target_bone(name, parents)
            if target is None:
                continue
            if isinstance(target, tuple):
                for t in target:
                    acc[t] = acc.get(t, 0.0) + g.weight / len(target)
            else:
                acc[target] = acc.get(target, 0.0) + g.weight
        per_vertex.append(acc)
    weighted = [i for i, acc in enumerate(per_vertex) if acc]
    if not weighted:
        raise SystemExit(obj.name + ': no vertex follows a body bone.')
    tree = mathutils.kdtree.KDTree(len(weighted))
    for k, i in enumerate(weighted):
        tree.insert(mesh.vertices[i].co, k)
    tree.balance()
    for i, acc in enumerate(per_vertex):
        if not acc:
            _, k, _ = tree.find(mesh.vertices[i].co)
            per_vertex[i] = dict(per_vertex[weighted[k]])
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    groups = {}
    for i, acc in enumerate(per_vertex):
        items = sorted(acc.items(), key=lambda kv: -kv[1])[:4]
        total = sum(w for _, w in items)
        for name, w in items:
            if name not in rig_names:
                raise SystemExit('internal: unknown target bone ' + name)
            if name not in groups:
                groups[name] = obj.vertex_groups.new(name=name)
            groups[name].add([i], w / total, 'REPLACE')


def dominant_bones(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    out = []
    for v in obj.data.vertices:
        best, bw = None, -1.0
        for g in v.groups:
            if g.weight > bw:
                best, bw = names[g.group], g.weight
        out.append(best)
    return out


def decimate(obj, budget):
    count = len(obj.data.vertices)
    if budget <= 0 or count <= budget:
        return
    m = obj.modifiers.new('budget', 'DECIMATE')
    m.ratio = max(0.05, budget / float(count))
    with override(obj):
        bpy.ops.object.modifier_apply(modifier=m.name)


# ------------------------------------------------------------------ garments and borrowed eyes

def garment_vertex(rule, bone, z, height):
    """Whether a body vertex belongs to a garment: its strongest bone is in the garment's "bones" (taken whole, the
    sleeves), or in its "band_bones" with its height on the body (z over the height) inside the garment's band, so
    the hems at the waist, the ankles and the collar follow one straight line."""
    if bone is None:
        return False
    stem = bone[:-2] if bone.endswith('_l') or bone.endswith('_r') else bone
    if stem in rule.get('bones', ()):
        return True
    if stem not in rule.get('band_bones', ()):
        return False
    rel = z / height
    return rule.get('from', -1.0) <= rel <= rule.get('to', 2.0)


def make_garments(body, rules, height):
    """Each garment is a copy of the body faces it covers, pushed out along the smooth normals, with the body's own
    weights and UVs. Faces fully inside a garment that hides skin are then removed from the body."""
    dominant = dominant_bones(body)
    zs = [v.co.z for v in body.data.vertices]
    made = []
    covered = set()
    for rule in rules:
        inside = [garment_vertex(rule, dominant[i], zs[i], height) for i in range(len(zs))]
        copy = body.copy()
        copy.data = body.data.copy()
        copy.name = rule['object']
        bpy.context.scene.collection.objects.link(copy)
        bm = bmesh.new()
        bm.from_mesh(copy.data)
        bm.verts.ensure_lookup_table()
        drop = [f for f in bm.faces if not all(inside[v.index] for v in f.verts)]
        keep = [f.index for f in bm.faces if all(inside[v.index] for v in f.verts)]
        if rule.get('hide_skin', True):
            covered.update(keep)
        bmesh.ops.delete(bm, geom=drop, context='FACES')
        loose = [v for v in bm.verts if not v.link_faces]
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
        bm.normal_update()
        offset = rule['offset']
        flare = rule.get('flare', 0.0)
        for v in bm.verts:
            push = offset
            if flare > 0.0 and v.is_boundary:
                push += flare
            v.co += v.normal * push
        bm.to_mesh(copy.data)
        bm.free()
        made.append((copy, rule))
    if covered:
        # Keep the skin faces at a garment's edge (one ring), so no gap can open where the garment ends.
        bm = bmesh.new()
        bm.from_mesh(body.data)
        bm.faces.ensure_lookup_table()
        border = set()
        for i in covered:
            for e in bm.faces[i].edges:
                for f in e.link_faces:
                    if f.index not in covered:
                        border.add(i)
        doomed = [bm.faces[i] for i in covered if i not in border]
        bmesh.ops.delete(bm, geom=doomed, context='FACES')
        loose = [v for v in bm.verts if not v.link_faces]
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
        bm.to_mesh(body.data)
        bm.free()
    return made


def borrow_eyes(source_blend, object_match, joints):
    """Appends the eye mesh of another MPFB file and fits each eyeball onto this head's eye bones (moved, sized by
    the distance between the eyes)."""
    with bpy.data.libraries.load(source_blend, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if object_match in n.lower() or n.endswith('.rig')]
    eyes = None
    rig = None
    for o in dst.objects:
        if o is None:
            continue
        if o.type == 'ARMATURE':
            rig = o
        elif object_match in o.name.lower():
            eyes = o
    if eyes is None or rig is None:
        raise SystemExit('the eye source has no ' + object_match + ' mesh or no rig')
    bpy.context.scene.collection.objects.link(eyes)
    sl = rig.matrix_world @ rig.data.bones['eye.L'].head_local
    sr = rig.matrix_world @ rig.data.bones['eye.R'].head_local
    tl, tr = joints['eye.L'][0], joints['eye.R'][0]
    scale = (tl - tr).length / max(1e-4, (sl - sr).length)
    for m in list(eyes.modifiers):
        eyes.modifiers.remove(m)
    matrix = eyes.matrix_world.copy()
    eyes.parent = None
    eyes.data.transform(matrix)
    eyes.matrix_world = mathutils.Matrix.Identity(4)
    for v in eyes.data.vertices:
        left = v.co.x > (sl.x + sr.x) * 0.5
        s, t = (sl, tl) if left else (sr, tr)
        v.co = t + (v.co - s) * scale
    for g in list(eyes.vertex_groups):
        eyes.vertex_groups.remove(g)
    eyes.vertex_groups.new(name='head').add(list(range(len(eyes.data.vertices))), 1.0, 'REPLACE')
    bpy.data.objects.remove(rig)
    return eyes


# ------------------------------------------------------------------ main

def main():
    blend, out, recipe_path = args()
    with open(recipe_path, 'r', encoding='utf-8') as f:
        recipe = json.load(f)
    bpy.ops.wm.open_mainfile(filepath=blend, load_ui=False)
    os.makedirs(out, exist_ok=True)
    rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    if len(rigs) != 1:
        raise SystemExit('expected one MPFB rig, found %d' % len(rigs))
    mpfb = rigs[0]
    mpfb.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    parents = {b.name: (b.parent.name if b.parent else None) for b in mpfb.data.bones}
    world = mpfb.matrix_world
    joints = {b.name: [world @ b.head_local, world @ b.tail_local] for b in mpfb.data.bones}

    pieces = []
    for obj in [o for o in bpy.data.objects if o.type == 'MESH']:
        key = obj.name.lower()
        material = obj.data.materials[0].name.lower() if obj.data.materials and obj.data.materials[0] else ''
        rule = None
        for r in recipe['pieces']:
            if (r['match'] == '.body' and material.endswith('.body')) or (r['match'] != '.body' and r['match'] in key):
                rule = r
                break
        if rule is None:
            raise SystemExit('no recipe piece matches ' + obj.name)
        if rule.get('drop'):
            bpy.data.objects.remove(obj)
            continue
        first = obj.data.materials[0] if obj.data.materials else None
        source, normal = find_image(first, False), find_image(first, True)
        bake_piece(obj)
        pieces.append([obj, rule, source, normal])

    if 'eyes' in recipe:
        e = recipe['eyes']
        source_blend = os.path.join(os.path.dirname(os.path.abspath(blend)), e['blend'])
        eyes = borrow_eyes(source_blend, e['match'], joints)
        first = eyes.data.materials[0] if eyes.data.materials else None
        pieces.append([eyes, e['piece'], find_image(first, False), None])

    # Hips over the origin, the lowest vertex on the ground, then the recipe's head height.
    lowest = min(min(v.co.z for v in p[0].data.vertices) for p in pieces)
    hip = joints['spine05'][0]
    move = mathutils.Matrix.Translation((-hip.x, -hip.y, -lowest))
    scale = 1.0
    if 'head_height' in recipe:
        scale = recipe['head_height'] / max(0.1, joints['head'][0].z - lowest)
    fit = mathutils.Matrix.Scale(scale, 4) @ move
    for p in pieces:
        p[0].data.transform(fit)
    for name in joints:
        joints[name] = [fit @ joints[name][0], fit @ joints[name][1]]

    rig = build_rig(joints)
    rig_names = set(b.name for b in rig.data.bones)
    bpy.data.objects.remove(mpfb)
    for p in pieces:
        if p[0].vertex_groups.get('head') is not None and len(p[0].vertex_groups) == 1:
            p[0].vertex_groups[0].name = 'Head'
        else:
            remap_weights(p[0], parents, rig_names)
    height = max(max(v.co.z for v in p[0].data.vertices) for p in pieces)

    body = next((p for p in pieces if p[1]['match'] == '.body'), None)
    if recipe.get('garments'):
        for garment, rule in make_garments(body[0], recipe['garments'], height):
            pieces.append([garment, rule, None, None])

    report = {'pieces': []}
    written = set()
    for obj, rule, image, normal in pieces:
        decimate(obj, rule.get('budget', 0))
        name = rule['material']
        obj.name = rule.get('object', name)
        material = bpy.data.materials.new(name)
        obj.data.materials.clear()
        obj.data.materials.append(material)
        obj.parent = rig
        arm = obj.modifiers.new('Armature', 'ARMATURE')
        arm.object = rig
        size = rule.get('size', 1024)
        stem = name.replace(' ', '_')
        if name not in written:
            written.add(name)
            albedo = os.path.join(out, stem + '_Albedo.png')
            if 'paint' in rule:
                write_pixels(paint(obj, rule['paint'], size, rule['palette'], height), albedo)
            elif image is not None:
                save_image(image, albedo, size, rule.get('alpha', False))
            if normal is not None and 'paint' not in rule:
                save_image(normal, os.path.join(out, stem + '_Normal.png'), size, False)
        report['pieces'].append({'object': obj.name, 'material': name, 'vertices': len(obj.data.vertices),
                                 'bones': sorted(g.name for g in obj.vertex_groups)[:6]})
    head = joints['head'][0]
    forward = bpy.data.objects.new('review_forward', None)
    bpy.context.scene.collection.objects.link(forward)
    forward.location = (0.0, -0.4, head.z)
    forward.parent = rig

    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig, forward] + [p[0] for p in pieces]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    fbx = os.path.join(out, recipe['file'] + '.fbx')
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
                             bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             path_mode='RELATIVE', mesh_smooth_type='FACE')
    with open(os.path.join(out, 'livecast.json'), 'w', encoding='utf-8') as f:
        json.dump(recipe['livecast'], f, indent=2)
        f.write('\n')
    with open(os.path.join(out, 'CREDITS.txt'), 'w', encoding='utf-8') as f:
        f.write('\n'.join(recipe['credits']) + '\n')
    if recipe.get('keep_blend'):
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(recipe['keep_blend']))
    report['height'] = height
    report['head_bone'] = head.z
    report['bones'] = len(rig.data.bones)
    report['vertices'] = sum(len(p[0].data.vertices) for p in pieces)
    print('CONVERTED ' + json.dumps(report))


main()
