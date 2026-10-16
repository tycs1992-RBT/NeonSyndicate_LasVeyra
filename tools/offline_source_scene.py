"""Blender scene helpers for honest repository-source inspection, not Unity parity."""
import copy
import hashlib
import json
import math
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / 'Assets/NeonSyndicate/Resources'


class SourceScene:
    def __init__(self):
        import bpy
        from mathutils import Matrix
        self.bpy = bpy
        self.C = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
        self.CI = self.C.inverted()
        self.meshes = {}; self.materials = {}; self.hashes = {}
        self.surfaces = json.loads((RESOURCES / 'VisualRebirth04A/material_manifest.json').read_text())

    def hash(self, path):
        self.hashes[str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()

    @staticmethod
    def point(value): return (value[0], -value[2], value[1])

    def transform(self, part):
        from mathutils import Matrix, Vector
        rot = [math.radians(x) for x in part.get('euler', [0, 0, 0])]
        return self.C @ Matrix.Translation(Vector(part.get('position', [0, 0, 0]))) @ Matrix.Rotation(rot[1], 4, 'Y') @ Matrix.Rotation(rot[0], 4, 'X') @ Matrix.Rotation(rot[2], 4, 'Z') @ Matrix.Diagonal((*part.get('scale', [1, 1, 1]), 1)) @ self.CI

    def mesh(self, path):
        import numpy as np
        key = str(path)
        if key in self.meshes: return self.meshes[key]
        raw = path.read_bytes(); count = struct.unpack_from('<i', raw, 4)[0]
        if raw[:4] != b'CMH1' or count % 3 or len(raw) != 8 + count * 32: raise ValueError('Invalid source CMH1: ' + key)
        values = np.frombuffer(raw, dtype='<f4', offset=8).reshape(count, 8)
        if not np.isfinite(values).all(): raise ValueError('Nonfinite source geometry: ' + key)
        coords = np.stack((values[:, 0], -values[:, 2], values[:, 1]), axis=1)
        normals = np.stack((values[:, 3], -values[:, 5], values[:, 4]), axis=1)
        mesh = self.bpy.data.meshes.new(path.stem)
        mesh.vertices.add(count); mesh.vertices.foreach_set('co', coords.ravel())
        mesh.loops.add(count); mesh.loops.foreach_set('vertex_index', np.arange(count, dtype=np.int32))
        mesh.polygons.add(count // 3); mesh.polygons.foreach_set('loop_start', np.arange(0, count, 3, dtype=np.int32))
        mesh.polygons.foreach_set('loop_total', np.full(count // 3, 3, dtype=np.int32)); mesh.update()
        uv = mesh.uv_layers.new(name='Source UV'); uv.data.foreach_set('uv', values[:, 6:8].ravel())
        mesh.normals_split_custom_set_from_vertices(normals)
        self.hash(path); self.meshes[key] = mesh
        return mesh

    def surface(self, style, role):
        from render_visual_rebirth_04a import resolve_material
        key = (style, role)
        if key in self.materials: return self.materials[key]
        inherited = {'timber': 'bark', 'linen': 'cloth', 'lamp': 'crystal'}.get(role, role)
        spec = copy.deepcopy(resolve_material(self.surfaces, style, inherited))
        if role in ('timber', 'linen', 'lamp'):
            spec['tint'] = {'timber': [.72, .52, .32], 'linen': [.9, .86, .72], 'lamp': [1, .67, .3]}[role]
            spec['emission'] = [.8, .38, .12] if role == 'lamp' else [0, 0, 0]
            if role == 'timber': spec['maps']['albedo'] = str(RESOURCES / 'InhabitedCity04C/Textures/Timber_Albedo.png')
        bpy = self.bpy; material = bpy.data.materials.new(str(style) + '/' + role); material.use_nodes = True
        nodes = material.node_tree.nodes; links = material.node_tree.links; shader = nodes.get('Principled BSDF')
        tint = spec.get('tint', [.65, .65, .65]); shader.inputs['Base Color'].default_value = (*tint, 1)
        shader.inputs['Metallic'].default_value = spec.get('metallic', 0)
        shader.inputs['Roughness'].default_value = 1 - spec.get('smoothness', .35)
        textures = {}
        for channel, file in spec.get('maps', {}).items():
            path = Path(file) if Path(file).is_absolute() else RESOURCES / 'VisualRebirth04A' / file
            self.hash(path); texture = nodes.new('ShaderNodeTexImage'); texture.image = bpy.data.images.load(str(path), check_existing=True)
            texture.image.colorspace_settings.name = 'sRGB' if channel in ('albedo', 'emission') else 'Non-Color'
            textures[channel] = texture
        if 'albedo' in textures:
            multiply = nodes.new('ShaderNodeMixRGB'); multiply.blend_type = 'MULTIPLY'; multiply.inputs[0].default_value = 1
            multiply.inputs[2].default_value = (*tint, 1); links.new(textures['albedo'].outputs['Color'], multiply.inputs[1]); links.new(multiply.outputs['Color'], shader.inputs['Base Color'])
        if 'mask' in textures:
            separate = nodes.new('ShaderNodeSeparateColor'); links.new(textures['mask'].outputs['Color'], separate.inputs['Color'])
            links.new(separate.outputs['Red'], shader.inputs['Metallic'])
            inverse = nodes.new('ShaderNodeMath'); inverse.operation = 'SUBTRACT'; inverse.inputs[0].default_value = 1
            links.new(textures['mask'].outputs['Alpha'], inverse.inputs[1]); links.new(inverse.outputs[0], shader.inputs['Roughness'])
        if 'normal' in textures:
            normal = nodes.new('ShaderNodeNormalMap'); normal.inputs['Strength'].default_value = spec.get('normalStrength', .55)
            links.new(textures['normal'].outputs['Color'], normal.inputs['Color']); links.new(normal.outputs['Normal'], shader.inputs['Normal'])
        emission = spec.get('emission', [0, 0, 0]); shader.inputs['Emission Color'].default_value = (*emission, 1); shader.inputs['Emission Strength'].default_value = 1
        if 'emission' in textures and any(emission):
            multiply = nodes.new('ShaderNodeMixRGB'); multiply.blend_type = 'MULTIPLY'; multiply.inputs[0].default_value = 1
            multiply.inputs[2].default_value = (*emission, 1); links.new(textures['emission'].outputs['Color'], multiply.inputs[1]); links.new(multiply.outputs['Color'], shader.inputs['Emission Color'])
        self.materials[key] = material
        return material

    def add(self, part, path, parent=None):
        ob = self.bpy.data.objects.new(part.get('nodeId', path.stem), self.mesh(path))
        self.bpy.context.collection.objects.link(ob)
        ob.matrix_world = self.transform(part) if parent is None else parent @ self.transform(part)
        material = self.surface(part.get('style', 0), part['role'])
        if not ob.data.materials: ob.data.materials.append(material)
        ob.material_slots[0].link = 'OBJECT'; ob.material_slots[0].material = material
        return ob

    def clear(self):
        for ob in list(self.bpy.data.objects): self.bpy.data.objects.remove(ob, do_unlink=True)

    def setup(self, camera, target, width=1440, height=900, samples=48, night=False, lens=28):
        from mathutils import Vector
        bpy = self.bpy; scene = bpy.context.scene
        scene.render.engine = 'CYCLES'; scene.cycles.samples = samples; scene.cycles.use_denoising = False
        scene.cycles.use_adaptive_sampling = True; scene.cycles.adaptive_threshold = .035; scene.cycles.adaptive_min_samples = min(16, samples)
        scene.cycles.max_bounces = 6; scene.cycles.diffuse_bounces = 4; scene.cycles.glossy_bounces = 3
        scene.render.resolution_x = width; scene.render.resolution_y = height; scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = 'PNG'; scene.view_settings.view_transform = 'AgX'; scene.view_settings.look = 'AgX - Medium High Contrast'
        scene.view_settings.exposure = .5 if not night else 1
        world = bpy.data.worlds.new('Offline review lighting, not Unity atmosphere'); world.use_nodes = True; scene.world = world
        background = world.node_tree.nodes.get('Background'); background.inputs['Color'].default_value = (.58, .69, .81, 1) if not night else (.1, .16, .28, 1)
        background.inputs['Strength'].default_value = .55 if not night else .2
        bpy.ops.object.light_add(type='SUN'); sun = bpy.context.object; sun.name = 'Offline sun, not native lighting'
        sun.rotation_euler = (math.radians(30), math.radians(-20), math.radians(-35)); sun.data.energy = 2.1 if not night else .35
        sun.data.angle = math.radians(9); sun.data.color = (1, .85, .68) if not night else (.52, .68, 1)
        bpy.ops.object.camera_add(location=self.point(camera)); cam = bpy.context.object
        cam.rotation_euler = (Vector(self.point(target)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.data.lens = lens; cam.data.clip_start = .03; cam.data.clip_end = 6000; scene.camera = cam
        return scene

    def practical(self, light, parent):
        from mathutils import Vector
        position = parent @ Vector((*self.point(light['position']), 1))
        self.bpy.ops.object.light_add(type='POINT', location=position[:3]); ob = self.bpy.context.object
        ob.name = 'Source practical / ' + light['id']; ob.data.energy = light['intensity'] * 90
        ob.data.color = light['color']; ob.data.shadow_soft_size = .14
        ob.data.use_shadow = False


def label(path, title, detail):
    from PIL import Image, ImageDraw, ImageFont
    im = Image.open(path).convert('RGB'); draw = ImageDraw.Draw(im); height = max(90, int(im.height * .1))
    font_path = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    font = ImageFont.truetype(font_path, 16); small = ImageFont.truetype(font_path, 13)
    draw.rectangle((0, im.height - height, im.width, im.height), fill=(8, 16, 25))
    draw.text((20, im.height - height + 8), 'OFFLINE SOURCE PREVIEW, NOT UNITY GAMEPLAY', font=font, fill=(235, 235, 228))
    draw.text((20, im.height - height + 31), title, font=font, fill=(235, 235, 228))
    draw.text((20, im.height - height + 55), detail, font=small, fill=(168, 190, 198)); im.save(path)
