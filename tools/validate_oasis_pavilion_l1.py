#!/usr/bin/env python3
"""Re-import the delivered FBX in Blender and test its actual structural proxy. Not Unity physics."""
import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root = Path(__file__).resolve().parents[1]
folder = root / 'Assets/NeonSyndicate/Art/LiveWorld/OasisPavilion'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(folder / 'OasisPavilion.fbx'), use_anim=False)
objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
checks = []
def check(name, value):
    checks.append({'name': name, 'passed': bool(value)})
def triangles(obj): return sum(len(p.vertices)-2 for p in obj.data.polygons)
names = {o.name: o for o in objects}
check('Exactly three real LOD meshes and one collision mesh', set(names) == {'OasisPavilion_LOD0','OasisPavilion_LOD1','OasisPavilion_LOD2','OasisPavilion_Collision'})
for obj in objects:
    check(obj.name + ' finite geometry', all(math.isfinite(v) for vert in obj.data.vertices for v in vert.co))
    check(obj.name + ' base origin and unit scale', obj.location.length < .001 and (obj.scale-Vector((1,1,1))).length < .001)
    if 'Collision' not in obj.name:
        check(obj.name + ' UVs', len(obj.data.uv_layers)>0 and len(obj.data.uv_layers[0].data)==len(obj.data.loops))
        check(obj.name + ' four named materials', {m.name.split('.')[0]for m in obj.data.materials} == {'OasisStone','OasisCarved','OasisMineral','OasisCrystal'})
check('LOD0 within 10000 to 40000 landmark budget', 10000 <= triangles(names['OasisPavilion_LOD0']) <= 40000)
check('Strictly cheaper distance meshes', triangles(names['OasisPavilion_LOD0']) > triangles(names['OasisPavilion_LOD1']) > triangles(names['OasisPavilion_LOD2']))
proxy = names['OasisPavilion_Collision']
check('Proxy below runtime 12000 triangle cap', triangles(proxy) <= 12000)
vertices = [proxy.matrix_world @ v.co for v in proxy.data.vertices]
bvh = BVHTree.FromPolygons(vertices, [list(p.vertices)for p in proxy.data.polygons])
def ray(origin,direction,distance): return bvh.ray_cast(Vector(origin),Vector(direction),distance)[0]
check('Open front entrance at player chest height', ray((0,-8,1.8),(0,1,0),8) is None)
check('Open exit at rear, offset from the basin', ray((2,0,1.8),(0,1,0),8) is None)
check('Front right column is solid', ray((2,-3.55,2.5),(1,0,0),4) is not None)
check('Front left column is solid', ray((-2,-3.55,2.5),(-1,0,0),4) is not None)
heights = []
for i in range(42):
    hit = ray((0,-7.2+i*.10,2),(0,0,-1),3)
    heights.append(0 if hit is None else hit.z)
check('Stair reaches the structural floor', abs(max(heights)-.72)<.005)
check('Every front riser is 18 cm or less', max(abs(b-a)for a,b in zip(heights,heights[1:])) <= .181)
lod0 = names['OasisPavilion_LOD0']
points=[lod0.matrix_world@Vector(v)for v in lod0.bound_box]
min_blender=[min(v[i]for v in points)for i in range(3)]
max_blender=[max(v[i]for v in points)for i in range(3)]
unity_min=[-max_blender[0],min_blender[2],-max_blender[1]]
unity_max=[-min_blender[0],max_blender[2],-min_blender[1]]
check('Delivered bounds enclosed by the conservative C# placement tests', all(a>=b-.001 for a,b in zip(unity_min,[-6.42,0,-6.72])) and all(a<=b+.001 for a,b in zip(unity_max,[6.42,12.64,6.72])))
for path in sorted(folder.glob('*.png')):
    image=bpy.data.images.load(str(path),check_existing=False)
    check(path.name+' is 1024 square',tuple(image.size)==(1024,1024))
report={'scope':__doc__,'checks':checks,'passed':sum(c['passed']for c in checks),'failed':sum(not c['passed']for c in checks),
        'reimported_meshes':[{'name':o.name,'triangles':triangles(o)}for o in objects],
        'unity_axis_bounds':{'min':unity_min,'max':unity_max},'native_unity':'NOT RUN'}
(root/'Docs/LiveWildL1/asset-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print('OASIS_L1_VALIDATION',json.dumps(report),flush=True)
if report['failed']: raise SystemExit(1)
