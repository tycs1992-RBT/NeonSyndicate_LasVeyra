#!/usr/bin/env python3
"""Matched SOURCE RENDER evidence for H9 (Blender Cycles on the editable source; never a Unity screenshot).

Same world, lights, cameras, resolution, samples and poses for the before and the after model, so differences come
from the art. Lighting matches render_selected_malik_h8r.py. Poses come from the real actions in the source; the
head turn is a labelled synthetic deformation pose (no head-turn action exists).
Usage: python tools/render_evidence_h9.py -- --model <blend> --rig MalikRig|DanteRig --output <dir> --set malik|dante
       [--views a,b] [--samples 24] [--res 640x800]
"""
import argparse, hashlib, json, math, sys
from pathlib import Path
import bpy
from mathutils import Vector

p = argparse.ArgumentParser()
p.add_argument('--model', type=Path, required=True)
p.add_argument('--rig', required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--set', choices=('malik', 'dante'), required=True)
p.add_argument('--views', default='')
p.add_argument('--samples', type=int, default=24)
p.add_argument('--res', default='640x800')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
a.output.mkdir(parents=True, exist_ok=True)
FULL_34 = {'cam': (2.3, -3.1, 1.35), 'target': (0, 0, 0.98), 'lens': 45}
UPPER_34 = {'cam': (1.25, -1.75, 1.62), 'target': (0, 0, 1.38), 'lens': 50}
VIEWS = {
    'malik': [
        ('neutral_front', None, 1, {'cam': (0, -4.2, 1.0), 'target': (0, 0, 1.0), 'ortho': 2.2}, None),
        ('neutral_profile', None, 1, {'cam': (4.2, 0, 1.0), 'target': (0, 0, 1.0), 'ortho': 2.2}, None),
        ('neutral_three_quarter', None, 1, {'cam': (2.6, -3.3, 1.25), 'target': (0, 0, 1.0), 'lens': 50}, None),
        ('face_front', None, 1, {'cam': (0, -0.95, 1.745), 'target': (0, -0.04, 1.735), 'lens': 85}, None),
        ('face_three_quarter', None, 1, {'cam': (0.62, -0.74, 1.77), 'target': (0, -0.04, 1.74), 'lens': 85}, None),
        ('head_turn', None, 1, {'cam': (0.95, -1.25, 1.72), 'target': (0, -0.02, 1.62), 'lens': 70},
         {'neck_01': (0, math.radians(18), 0), 'Head': (0, math.radians(32), 0)}),
        ('run', 'H7_Run', 7, FULL_34, None),
        ('planted_stop', 'H8_JumpLanding', 12, FULL_34, None),
        ('sword_sweep', 'H7_OathCut', 21, FULL_34, None),
        ('guard', 'H8_Guard_Brace', 20, FULL_34, None),
        ('dodge', 'H8_Dodge', 12, FULL_34, None),
        ('two_handed_gun', 'H8_FireAim', 30, FULL_34, None),
        ('cast', 'H8_CrystalCast', 13, FULL_34, None),
        ('flight', 'H8_FlightTakeoff', 16, FULL_34, None),
        ('sword_sweep_upper', 'H7_OathCut', 21, UPPER_34, None),
        ('guard_upper', 'H8_Guard_Brace', 20, UPPER_34, None),
        ('dodge_upper', 'H8_Dodge', 12, UPPER_34, None),
        ('two_handed_gun_upper', 'H8_FireAim', 30, UPPER_34, None),
        ('cast_upper', 'H8_CrystalCast', 13, UPPER_34, None),
    ],
    'dante': [
        ('front', None, 1, {'cam': (0, -4.4, 0.98), 'target': (0, 0, 0.98), 'ortho': 2.3}, None),
        ('three_quarter', None, 1, {'cam': (2.6, -3.4, 1.3), 'target': (0, 0, 0.98), 'lens': 45}, None),
        ('face_three_quarter', None, 1, {'cam': (0.62, -0.78, 1.72), 'target': (0, 0, 1.68), 'lens': 85}, 'auto_head'),
    ],
}
bpy.ops.wm.open_mainfile(filepath=str(a.model), load_ui=False, use_scripts=False)
scene = bpy.context.scene
w, h = (int(x) for x in a.res.split('x'))
scene.render.engine = 'CYCLES'; scene.cycles.device = 'CPU'; scene.cycles.samples = a.samples
scene.cycles.use_denoising = True; scene.cycles.max_bounces = 6
scene.render.threads_mode = 'FIXED'; scene.render.threads = 1
scene.render.resolution_x = w; scene.render.resolution_y = h; scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'AgX'; scene.view_settings.look = 'AgX - Medium High Contrast'; scene.view_settings.exposure = 0
scene.world = bpy.data.worlds.new('Neutral review world'); scene.world.use_nodes = True
bg = scene.world.node_tree.nodes['Background']; bg.inputs[0].default_value = (.13, .15, .19, 1); bg.inputs[1].default_value = .35
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.012)); floor = bpy.context.object; floor.name = 'Review floor, not a game asset'
m = bpy.data.materials.new('Review ground'); m.diffuse_color = (.065, .07, .085, 1); floor.data.materials.append(m)


def light(name, pos, power, size, color):
    data = bpy.data.lights.new(name, 'AREA'); data.energy = power; data.shape = 'DISK'; data.size = size; data.color = color
    o = bpy.data.objects.new(name, data); bpy.context.collection.objects.link(o); o.location = pos
    o.rotation_euler = (Vector((0, 0, 1.1)) - o.location).to_track_quat('-Z', 'Y').to_euler()


light('Neutral key', (2, -3, 3.3), 350, 3, (1, .92, .84)); light('Soft fill', (-2, -1.5, 2.0), 180, 2, (.72, .82, 1)); light('Rim', (1, 2, 3.0), 220, 2, (.75, .83, 1))
cam_data = bpy.data.cameras.new('Source review camera'); cam = bpy.data.objects.new('Source review camera', cam_data)
bpy.context.collection.objects.link(cam); scene.camera = cam
cam_data.clip_start = 0.01
rig = bpy.data.objects[a.rig]
want = set(x for x in a.views.split(',') if x)
records = []
for name, action, frame, view, pose in VIEWS[a.set]:
    if want and name not in want:
        continue
    if rig.animation_data:
        rig.animation_data.action = None
    for b in rig.pose.bones:
        b.rotation_mode = 'QUATERNION'; b.rotation_quaternion = (1, 0, 0, 0); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    if action:
        act = bpy.data.actions[action]
        rig.animation_data_create(); rig.animation_data.action = act
        if act.slots:
            rig.animation_data.action_slot = act.slots[0]
    scene.frame_set(frame)
    if isinstance(pose, dict):
        for bn, rot in pose.items():
            pb = rig.pose.bones[bn]; pb.rotation_mode = 'XYZ'; pb.rotation_euler = rot
    bpy.context.view_layer.update()
    target = Vector(view['target'])
    campos = Vector(view['cam'])
    if pose == 'auto_head':  # frame the head at its actual height (the H9 refit raised Dante's head)
        hz = (rig.matrix_world @ rig.data.bones['Head'].head_local).z + 0.08
        campos.z += hz - target.z; target.z = hz
    cam.location = campos
    cam.rotation_euler = (target - campos).to_track_quat('-Z', 'Y').to_euler()
    if 'ortho' in view:
        cam_data.type = 'ORTHO'; cam_data.ortho_scale = view['ortho']
    else:
        cam_data.type = 'PERSP'; cam_data.lens = view['lens']
    scene.render.filepath = str(a.output / (name + '.png'))
    bpy.ops.render.render(write_still=True)
    records.append({'view': name, 'file': name + '.png', 'authored_action': action, 'action_frame': frame,
                    'camera': [round(c, 4) for c in campos], 'target': [round(c, 4) for c in target],
                    'ortho_scale' if 'ortho' in view else 'lens_mm': view.get('ortho', view.get('lens')),
                    'synthetic_deformation_pose': isinstance(pose, dict)})
    print('RENDERED', name, flush=True)
mf = a.output / 'render-manifest.json'
old = json.loads(mf.read_text()) if mf.exists() else {}
merged = {r['view']: r for r in old.get('views', [])}
for r in records:
    merged[r['view']] = r
mf.write_text(json.dumps({'tag': 'SOURCE RENDER', 'not': 'Not a Unity screenshot, not native acceptance, not owner approval',
                          'source_blend': str(a.model.name), 'source_sha256': hashlib.sha256(a.model.read_bytes()).hexdigest(),
                          'engine': 'Blender Cycles CPU, %d samples, denoised, AgX Medium High Contrast' % a.samples,
                          'resolution': [w, h], 'lighting': 'Neutral key, soft fill and rim area lights as render_selected_malik_h8r.py',
                          'native_unity': 'NOT RUN', 'owner_approval': 'NOT GIVEN',
                          'views': [merged[k] for k in sorted(merged)]}, indent=2) + '\n')
