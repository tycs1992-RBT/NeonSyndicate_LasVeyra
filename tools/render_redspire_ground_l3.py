#!/usr/bin/env python3
"""Blender material source and neutral comparison renders. Never Unity gameplay evidence."""
from pathlib import Path
import json
import os
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/NeonSyndicate/Art/LiveGround/Redspire'
OUT = ROOT / 'Docs/LiveGroundL3'
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.cycles.max_bounces = 3
scene.render.threads_mode = 'FIXED'
scene.render.threads = 3
scene.render.resolution_x = 1000
scene.render.resolution_y = 650
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'AgX'
scene.world = bpy.data.worlds.new('Neutral material review')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45


def material(family):
    m = bpy.data.materials.new(family)
    m.use_nodes = True
    m.use_fake_user = True
    nodes = m.node_tree.nodes
    links = m.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    coord = nodes.new('ShaderNodeTexCoord')
    mapping = nodes.new('ShaderNodeVectorMath')
    mapping.operation = 'SCALE'
    mapping.inputs[3].default_value = 1 / 4
    links.new(coord.outputs['Object'], mapping.inputs[0])
    for channel in ['BaseColor', 'Normal', 'MetallicSmoothness']:
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(str(ART / (family + '_' + channel + '.png')))
        tex.image.colorspace_settings.name = 'sRGB' if channel == 'BaseColor' else 'Non-Color'
        tex.extension = 'REPEAT'
        links.new(mapping.outputs['Vector'], tex.inputs['Vector'])
        if channel == 'BaseColor':
            links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
        elif channel == 'Normal':
            normal = nodes.new('ShaderNodeNormalMap')
            links.new(tex.outputs['Color'], normal.inputs['Color'])
            links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
        else:
            invert = nodes.new('ShaderNodeMath')
            invert.operation = 'SUBTRACT'
            invert.inputs[0].default_value = 1
            links.new(tex.outputs['Alpha'], invert.inputs[1])
            links.new(invert.outputs[0], bsdf.inputs['Roughness'])
    return m


earth = material('RedspireWeatheredEarth')
sand = material('RedspireStonySand')
bpy.ops.mesh.primitive_plane_add(size=32)
floor = bpy.context.object
floor.name = 'Material review floor, presentation only'
floor.data.materials.append(earth)
for name, pos, energy, size in [('Neutral sun',(-5,-9,15),2300,8),('Sky',(9,7,12),1600,11)]:
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = energy
    data.size = size
    o = bpy.data.objects.new(name,data)
    scene.collection.objects.link(o)
    o.location = pos
    o.rotation_euler = (Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
data = bpy.data.cameras.new('Matched DCC review')
camera = bpy.data.objects.new('Matched DCC review',data)
scene.collection.objects.link(camera)
scene.camera = camera


def look(pos, target, ortho=0):
    camera.location = pos
    camera.rotation_euler = (Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type = 'ORTHO' if ortho else 'PERSP'
    if ortho: camera.data.ortho_scale = ortho
    else: camera.data.lens = 38


def render(name):
    if '--source-only' in sys.argv: return
    scene.render.filepath = str(OUT / (name + '.png'))
    bpy.ops.render.render(write_still=True)


OUT.mkdir(parents=True,exist_ok=True)
for m in [earth,sand]:
    floor.data.materials[0] = m
    for repeats in [2,4]:
        look((0,0,20),(0,0,0),repeats*4)
        render(m.name + '_' + str(repeats) + 'x' + str(repeats))

# Reuse the delivered editable L2 source. No re-sculpt or export of those assets.
with bpy.data.libraries.load(str(ROOT / 'ArtSource/RedspireL2/RedspireWildernessL2.blend'),link=False) as (src,dst):
    dst.objects = [n for n in src.objects if n in ['Boulder_A','RockSpire_A','DeadTree_B']]
props = []
positions = {'Boulder_A':(-3,-1,0),'RockSpire_A':(3,4,0),'DeadTree_B':(-5,5,0)}
for o in dst.objects:
    scene.collection.objects.link(o)
    o.location = positions[o.name]
    o.hide_render = False
    props.append(o)
for im in bpy.data.images:
    name = Path(im.filepath).name
    candidate = ROOT / 'Assets/NeonSyndicate/Art/LiveWilds/Shared' / name
    if candidate.exists(): im.filepath = str(candidate); im.reload()
legacy = bpy.data.materials.new('Original floor palette approximation, not Unity shader')
legacy.use_nodes = True
legacy.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.33,.23,.16,1)
legacy.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
records = []
for distance, pos, target in [('Close',(1,-4,2.5),(0,0,0)),('Mid',(12,-19,9),(0,1,1.7)),('Far',(21,-31,17),(0,1,1.7))]:
    look(pos,target)
    for label,m in [('Before',legacy),('After',earth)]:
        floor.data.materials[0] = m
        render(distance + '_' + label)
        records.append({'file':distance+'_'+label+'.png','camera':list(camera.location),'target':target,'material':m.name})
floor.data.materials[0] = earth
source = ROOT / 'ArtSource/RedspireL3/RedspireGroundL3.blend'
for im in bpy.data.images:
    if im.source == 'FILE': im.filepath = '//' + os.path.relpath(bpy.path.abspath(im.filepath),source.parent)
bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True,relative_remap=False)
(OUT/'dcc-views.json').write_text(json.dumps({'scope':__doc__,'renderer':'Blender 4.5.3 Cycles CPU, 24 samples, neutral lighting','native_unity':'NOT RUN','before':'Original palette approximation, not a reproduction of the Unity procedural shader','after':'Delivered original PBR maps on a presentation plane with unchanged L2 source assets','views':records},indent=2)+'\n')
