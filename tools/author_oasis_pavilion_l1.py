#!/usr/bin/env python3
"""Author the original Mirrorwell pavilion in Blender. No external mesh or texture inputs.

Run: blender -b --python tools/author_oasis_pavilion_l1.py
Exports the real gameplay FBX, editable source, PBR maps and labeled DCC source renders.
"""
import bpy
import json
import math
import random
import sys
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion'
SOURCE = ROOT / 'ArtSource/OasisPavilionL1'
REVIEW = ROOT / 'Docs/LiveWildL1'
for directory in (OUT, SOURCE, REVIEW):
    directory.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
rng = random.Random(71109)
parts, collision = [], []

def active(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def map_png(name, pixels, data=False):
    size = pixels.shape[0]
    image = bpy.data.images.new(name, size, size, alpha=True)
    if data:
        image.colorspace_settings.name = 'Non-Color'
    image.pixels.foreach_set(np.ascontiguousarray(pixels, dtype=np.float32).ravel())
    image.filepath_raw = str(OUT / (name + '.png'))
    image.file_format = 'PNG'
    image.save()
    return image

def stone_maps(name, base, roughness, seed, mineral=False, glow=False):
    """Original seamless stone: banded grain, mineral faults, fine pits and tangent normal relief."""
    n = 1024
    prng = np.random.default_rng(seed)
    yy, xx = np.mgrid[:n, :n] / n * 2 * math.pi
    field = np.zeros((n, n))
    for octave in range(1, 7):
        for _ in range(5):
            fx, fy = prng.integers(1, 2 ** octave + 1, 2)
            field += np.sin(xx * fx + yy * fy + prng.uniform(0, 6.28)) / (octave * 8)
    grain = prng.normal(0, .035, (n, n))
    vein = np.exp(-np.abs(np.sin(xx * 3 + np.sin(yy * 2) * .65 + field * 2)) * 50)
    pores = (prng.random((n, n)) > .995).astype(float)
    height = field * .14 + grain * .18 - pores * .13 - vein * .028
    rgba = np.ones((n, n, 4))
    tint = np.array(base)
    rgba[:, :, :3] = np.clip(tint[None, None, :] * (1 + field[:, :, None] * .33 + grain[:, :, None])
                                     + vein[:, :, None] * (.065 if not mineral else .035) - pores[:, :, None] * .06, .005, .95)
    albedo = map_png(name + '_BaseColor', rgba)
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 3
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 3
    normals = np.stack((-dx, -dy, np.ones_like(dx)), 2)
    normals /= np.linalg.norm(normals, axis=2)[:, :, None]
    rgba[:, :, :3] = normals * .5 + .5
    rgba[:, :, 3] = 1
    normal = map_png(name + '_Normal', rgba, True)
    rgba[:, :, :3] = .15 if mineral else .015
    rgba[:, :, 3] = np.clip(1 - roughness - field * .10 - pores * .15, .035, .55)
    mask = map_png(name + '_MetallicSmoothness', rgba, True)
    emission = None
    if glow:
        rgba[:, :, :3] = np.array(base)[None, None, :] * (.12 + vein[:, :, None] * .25)
        rgba[:, :, 3] = 1
        emission = map_png(name + '_Emission', rgba)
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value = roughness
    nodes, links = material.node_tree.nodes, material.node_tree.links
    image = nodes.new('ShaderNodeTexImage'); image.image = albedo
    links.new(image.outputs['Color'], bsdf.inputs['Base Color'])
    image = nodes.new('ShaderNodeTexImage'); image.image = normal
    norm = nodes.new('ShaderNodeNormalMap'); norm.inputs['Strength'].default_value = .7
    links.new(image.outputs['Color'], norm.inputs['Color']); links.new(norm.outputs['Normal'], bsdf.inputs['Normal'])
    if emission:
        image = nodes.new('ShaderNodeTexImage'); image.image = emission
        links.new(image.outputs['Color'], bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value = .45
    return material

mats = {
    'stone': stone_maps('OasisStone', (.30, .275, .23), .86, 7),
    'carved': stone_maps('OasisCarved', (.47, .425, .33), .78, 13),
    'mineral': stone_maps('OasisMineral', (.085, .12, .115), .67, 31, True),
    'crystal': stone_maps('OasisCrystal', (.045, .34, .30), .42, 93, True, True),
}

def mesh(name, vertices, faces, mat='stone', proxy=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces); data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    if not proxy: data.materials.append(mats[mat])
    (collision if proxy else parts).append(obj)
    return obj

def block(name, centre, size, mat='stone', bevel=.035, proxy=False):
    # Eight individually cut corners, with small stone-edge variation before beveling.
    x, y, z = centre; a, b, c = [v / 2 for v in size]
    jitter = 0 if proxy else min(.025, min(size) * .07)
    vertices = [(x + sx * a + rng.uniform(-jitter, jitter), y + sy * b + rng.uniform(-jitter, jitter), z + sz * c)
                for sz in (-1, 1) for sy in (-1, 1) for sx in (-1, 1)]
    faces = [(0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)]
    obj = mesh(name, vertices, faces, mat, proxy)
    if bevel and not proxy:
        active(obj)
        mod = obj.modifiers.new('Worn dressed arris', 'BEVEL'); mod.width = bevel; mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def profile(name, x, y, rings, mat='carved', sides=24, flute=0, proxy=False, caps=True):
    vertices, faces = [], []
    for z, radius in rings:
        for i in range(sides):
            a = i * math.tau / sides
            r = radius * (1 + flute * math.cos(a * 8))
            vertices.append((x + math.cos(a) * r, y + math.sin(a) * r, z))
    for j in range(len(rings) - 1):
        for i in range(sides):
            a = j * sides + i; b = j * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    if caps: faces.extend([tuple(range(sides - 1, -1, -1)), tuple((len(rings) - 1) * sides + i for i in range(sides))])
    return mesh(name, vertices, faces, mat, proxy)

def ribbon(name, points, width, depth, mat='carved', proxy=False):
    vertices, faces = [], []
    for j, p in enumerate(points):
        tangent = Vector(points[min(j+1, len(points)-1)]) - Vector(points[max(0, j-1)])
        tangent.normalize(); normal = Vector((-tangent.z, 0, tangent.x)).normalized()
        for sign_y, sign_n in [(-1,-1), (-1,1), (1,1), (1,-1)]:
            vertices.append(tuple(Vector(p) + Vector((0, sign_y * depth / 2, 0)) + normal * sign_n * width / 2))
    for j in range(len(points)-1):
        for i in range(4):
            a = j*4+i; b = j*4+(i+1)%4
            faces.append((a,b,b+4,a+4))
    faces.extend([(3,2,1,0),tuple((len(points)-1)*4+i for i in range(4))])
    return mesh(name, vertices, faces, mat, proxy)

def arch_points(width, spring, y, count=32):
    # Two circular arcs make a true pointed gothic arch.
    half = width / 2; radius = width
    angle = math.acos(.5)
    left = [(-half + radius * math.cos(math.pi - angle * i / count), y, spring + radius * math.sin(math.pi - angle * i / count)) for i in range(count+1)]
    # Radius origin +half for left half, and -half for right half.
    left = [(half + radius * math.cos(math.pi - angle*i/count), y, spring + radius*math.sin(math.pi - angle*i/count)) for i in range(count+1)]
    right = [(-half + radius * math.cos(angle*(1-i/count)), y, spring + radius*math.sin(angle*(1-i/count))) for i in range(1,count+1)]
    return left + right

# The wide three-sided stair is the approach. Risers stay below the character's normal step height.
for k in range(4):
    top = (k + 1) * .18
    width, depth = 12.8 - k*.7, 13.4 - k*.7
    block('Foundation course %d' % k, (0,0,top/2), (width,depth,top), 'stone', .025)
    block('Stair course %d_Collision' % k, (0,0,top/2), (width,depth,top), proxy=True)
    # Separate fitted edge stones and incised mineral seam, visible above the previous course.
    for x in range(-5,6):
        block('Tread nosing', (x*1.04,-depth/2+.12,top+.012), (1.015,.24,.055),'carved',.014)

# Fragmented fitted floor, continuous structural collision beneath it.
for ix in range(-5,6):
    for iy in range(-4,5):
        x, y = ix*.91, iy*1.00
        if abs(x) > 3.7 and y > 2.5 and rng.random() < .3: continue
        block('Ancient floor flag', (x,y,.735+rng.uniform(-.008,.008)), (.88,.97,.055), 'carved' if (ix+iy)%7==0 else 'stone', .018)

# Six carved piers: stepped octagonal feet, fluted shafts, ornamental bands and branching capitals.
for x in (-4.1,4.1):
    for y in (-3.55,0,3.55):
        profile('Octagonal plinth',x,y,[(.72,.90),(.87,.90),(.91,.79),(1.03,.79),(1.10,.63)], sides=8)
        profile('Reeded pier shaft',x,y,[(1.08,.57),(1.2,.59),(1.28,.52),(4.5,.48),(4.75,.55)],'stone',48,.09)
        for z in (1.35,3.0,4.6):
            profile('Incised collar',x,y,[(z,.55),(z+.05,.61),(z+.14,.61),(z+.19,.54)], sides=24)
            for i in range(8):
                a=i*math.tau/8
                block('Mineral inlay', (x+math.cos(a)*.56,y+math.sin(a)*.56,z+.10),(.075,.075,.075),'mineral',.008)
        profile('Foliate capital',x,y,[(4.72,.52),(4.85,.61),(5.08,.76),(5.22,.82),(5.30,.82)], sides=16,flute=.06)
        for i in range(8):
            a=i*math.tau/8
            profile('Carved hanging bud',x+math.cos(a)*.59,y+math.sin(a)*.59,[(4.62,.035),(4.76,.14),(4.96,.09)], sides=8)
        profile('Pier_Collision',x,y,[(.72,.87),(1.08,.87),(1.08,.61),(4.75,.61),(5.30,.80)],sides=12,proxy=True)

# Full pointed front arch, broken rear arch and surviving longitudinal ribs.
for y, broken in [(-3.55,False),(3.55,True)]:
    points=arch_points(8.2,5.28,y,24)
    for j in range(len(points)-1):
        if broken and 17 <= j <= 30: continue
        ribbon('Radial carved voussoir',points[j:j+2],.57,.88,'carved' if j%4==0 else 'stone')
        ribbon('Arch_Collision',points[j:j+2],.55,.83,proxy=True)
    for shift in (-.48,.48):
        detail=[(p[0],p[1]+shift,p[2]+.14) for p in points]
        ribbon('Arch moulding bead',detail if not broken else detail[:18],.095,.095,'carved')

for x in (-4.1,4.1):
    for start,end in [(-3.55,0),(0,3.55)]:
        for j in range(6):
            y=start+(end-start)*(j+.5)/6
            block('Longitudinal entablature',(x,y,5.40),(.79,(end-start)/6-.018,.40),'stone',.035)
            block('Cornice drip edge',(x,y,5.66),(1.0,(end-start)/6-.02,.14),'carved',.022)
        block('Entablature_Collision',(x,(start+end)/2,5.44),(.8,end-start,.6),proxy=True)

# Ribbed broken vault canopy. Deep ribs remain; irregular shell fragments have missing bays.
for y in (-1.65,1.65):
    path=arch_points(8.2,5.36,y,20)
    ribbon('Vault transverse rib',path,.19,.21,'carved')
    for j in range(len(path)-1):
        if (y>0 and 12<j<31) or (y<0 and 22<j<32): continue
        # Deliberately torn upper canopy, thinner than the structural arch.
        p,q=Vector(path[j]),Vector(path[j+1])
        ys=1.65 if y<0 else 1.40
        vs=[tuple(p+Vector((0,s*ys,.13))) for s in (-1,1)]+[tuple(q+Vector((0,s*ys,.13))) for s in (-1,1)]
        mesh('Fragment of vaulted roof',vs,[(0,2,3,1)],'stone')
        obj=parts[-1];active(obj);mod=obj.modifiers.new('Vault stone depth','SOLIDIFY');mod.thickness=.18;bpy.ops.object.modifier_apply(modifier=mod.name)

# Broken low side walls, with real gaps to the side and rear, never across the main entrance.
for x in (-4.7,4.7):
    for course in range(4):
        for j in range(5):
            if (x<0 and j==3) or (course==3 and j>2): continue
            y=.25+j*.68
            block('Weathered wall ashlar',(x,y,.88+course*.32),(.72,.65,.30),'stone',.027)
            block('Wall ashlar_Collision',(x,y,.88+course*.32),(.70,.63,.30),proxy=True)


# Small mineral well basin at the rear, with enough space around it for the camera and player.
profile('Octagonal mirror basin',0,2.15,[(.77,.83),(.90,.94),(1.20,.94),(1.27,1.02),(1.36,1.02)],'mineral',sides=16)
profile('Turquoise stillwater',0,2.15,[(1.365,.78),(1.37,.78)],'crystal',sides=32)
profile('Basin_Collision',0,2.15,[(.75,.94),(1.36,1.02)],sides=12,proxy=True)
for i in range(5):
    a=i*math.tau/5
    profile('Small mineral growth',math.cos(a)*.48,2.15+math.sin(a)*.48,[(1.37,.08),(1.70+rng.random()*.18,.09),(1.87+rng.random()*.20,0)],'crystal',sides=5)

# Thin geometric floor tracery and carved quatrefoil seal, visible without an emissive billboard.
for radius in (1.10,1.30,1.43):
    profile('Inlaid floor ring',0,-1.0,[(.772,radius),(.782,radius+.025)],'mineral',sides=64,caps=False)
for i in range(4):
    angle=i*math.pi/2
    profile('Four leaf floor seal',math.cos(angle)*.52,-1+math.sin(angle)*.52,[(.777,.39),(.787,.415)],'mineral',sides=32,caps=False)
for i in range(26):
    x=rng.choice((-1,1))*rng.uniform(5.0,5.4);y=rng.uniform(1.8,4.5)
    block('Fallen carved fragment',(x,y,.78+rng.uniform(0,.12)),(rng.uniform(.20,.44),rng.uniform(.22,.5),rng.uniform(.09,.25)),'carved',.025)

def join_mesh(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join();obj=bpy.context.object;obj.name=name;obj.data.name=name
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    uv=obj.data.uv_layers.new(name='Metric stone UV')
    for poly in obj.data.polygons:
        normal=poly.normal;axis=max(range(3),key=lambda i:abs(normal[i]))
        for loop in poly.loop_indices:
            p=obj.data.vertices[obj.data.loops[loop].vertex_index].co
            a,b=((p.y,p.z)if axis==0 else ((p.x,p.z)if axis==1 else(p.x,p.y)))
            uv.data[loop].uv=(a*.5,b*.5)
    return obj

render=join_mesh(parts,'OasisPavilion_LOD0')
active(render);mod=render.modifiers.new('Landmark triangle budget','DECIMATE');mod.ratio=.65;bpy.ops.object.modifier_apply(modifier=mod.name)
proxy=join_mesh(collision,'OasisPavilion_Collision')
proxy.hide_render=True;proxy.hide_set(True)
lods=[render]
for level,ratio in [(1,.52),(2,.20)]:
    obj=render.copy();obj.data=render.data.copy();bpy.context.collection.objects.link(obj);obj.name='OasisPavilion_LOD%d'%level;obj.data.name=obj.name
    active(obj);mod=obj.modifiers.new('Distance stone simplification','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.hide_render=True;obj.hide_set(True);lods.append(obj)

# Export all three LOD levels plus the collision proxy, with no presentation environment.
bpy.ops.object.select_all(action='DESELECT')
for obj in lods+[proxy]:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=render
bpy.ops.export_scene.fbx(filepath=str(OUT/'OasisPavilion.fbx'),use_selection=True,object_types={'MESH'},
                        bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
                        apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,mesh_smooth_type='FACE',path_mode='STRIP')
for obj in lods[1:]+[proxy]:obj.hide_render=True;obj.hide_set(True)

stats={'tool':bpy.app.version_string,'original_source':True,'metres':True,'origin':[0,0,0],
       'export':{'forward':'-Z','up':'Y','apply_unit':True,'apply_transform':True,'animation':False},
       'meshes':[{'name':obj.name,'vertices':len(obj.data.vertices),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons)} for obj in lods+[proxy]],
       'bounds_blender':[list(v)for v in render.bound_box],'step_rise':.18,'entrance_clear_width':6.4,
       'native_unity':'NOT RUN','render_label':'DCC SOURCE RENDER'}

# DCC-only presentation stage. Excluded from FBX and kept separately in editable source.
stage=bpy.data.collections.new('DCC source render stage, not exported');bpy.context.scene.collection.children.link(stage)
def stage_link(obj):
    for col in list(obj.users_collection):col.objects.unlink(obj)
    stage.objects.link(obj)

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.035));ground=bpy.context.object;ground.name='DCC sand stage';stage_link(ground)
sand=bpy.data.materials.new('DCC sand');sand.diffuse_color=(.29,.20,.115,1);sand.use_nodes=True
bs=sand.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.29,.20,.115,1);bs.inputs['Roughness'].default_value=.93;ground.data.materials.append(sand)
noise=sand.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=7
bump=sand.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.06
sand.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);sand.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
bpy.ops.object.camera_add(location=(22,-30,15));cam=bpy.context.object;cam.name='DCC source camera';stage_link(cam)
cam.rotation_euler=(Vector((0,0,5.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=43;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='SUN',location=(0,0,18));sun=bpy.context.object;stage_link(sun);sun.rotation_euler=(.5,-.5,-.6);sun.data.angle=.15
bpy.ops.object.light_add(type='AREA',location=(-6,-8,10));fill=bpy.context.object;stage_link(fill);fill.data.energy=1200;fill.data.shape='DISK';fill.data.size=9
world=bpy.data.worlds.new('DCC world');bpy.context.scene.world=world;world.use_nodes=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=32
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'

# Camera-facing text is part of the DCC stage, never a Unity screenshot label added afterwards.
bpy.ops.object.text_add();label=bpy.context.object;stage_link(label);label.name='DCC label';label.parent=cam
label.location=(-.52,-.35,-1.6);label.rotation_euler=(0,0,0);label.data.size=.020
labelmat=bpy.data.materials.new('DCC label white');labelmat.use_nodes=True
nodes=labelmat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(1,.91,.73,1);output=nodes.new('ShaderNodeOutputMaterial');labelmat.node_tree.links.new(em.outputs[0],output.inputs[0]);label.data.materials.append(labelmat)

for title,energy,strength,color in [('Daylight',2.8,.32,(.61,.72,.86,1)),('Night',.06,.055,(.10,.19,.34,1))]:
    sun.data.energy=energy;world.node_tree.nodes['Background'].inputs['Color'].default_value=color;world.node_tree.nodes['Background'].inputs['Strength'].default_value=strength
    fill.data.energy=1200 if title=='Daylight'else 450;fill.data.color=(1,.87,.68)if title=='Daylight'else(.35,.64,1)
    label.data.body='DCC SOURCE RENDER  /  MIRRORWELL OASIS  /  '+title.upper()+'\nOriginal Blender asset. Unity lighting and gameplay not verified.'
    scene.render.filepath=str(REVIEW/('OasisPavilion_'+title+'.png'))
    if '--skip-renders' not in sys.argv: bpy.ops.render.render(write_still=True)
stats['source_render_setup']={'camera':list(cam.location),'target':[0,0,5.1],'lens_mm':43,'resolution':[1600,1100],
                              'engine':'Cycles CPU','samples':32,'day_sun_energy':2.8,'night_sun_energy':.06,
                              'day_world_strength':.32,'night_world_strength':.055,'stage_exported':False}
(REVIEW/'asset-stats.json').write_text(json.dumps(stats,indent=2)+'\n')
# Keep source opening at the daytime setup, with packed editable textures.
sun.data.energy=2.8;world.node_tree.nodes['Background'].inputs['Strength'].default_value=.32;fill.data.energy=1200
label.data.body='DCC SOURCE RENDER  /  MIRRORWELL OASIS  /  DAYLIGHT\nOriginal Blender asset. Unity lighting and gameplay not verified.'
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.61,.72,.86,1);fill.data.color=(1,.87,.68)
# Source uses portable relative paths to the maps shipped in the game folder.
for image in bpy.data.images:
    if image.source == 'FILE' and image.filepath:
        image.filepath='//../../Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion/'+Path(image.filepath).name
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'OasisPavilion.blend'),compress=True)
print('OASIS_L1_ASSET',json.dumps(stats),flush=True)
