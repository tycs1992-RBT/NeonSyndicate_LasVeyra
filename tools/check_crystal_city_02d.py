#!/usr/bin/env python3
"""Offline source-contract checks, NOT a C# compile, Unity test run or frame-rate measurement."""
from pathlib import Path
import hashlib, json, re, sys
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict'
NAMES = ('orbit','shard','crown','trunk','leaves','vine','fern','bloom','rosette','buttress','lantern','relic')
FILES = ['DistrictArchitecture.cs','CrystalGardenDressing.cs','CrystalCity02DGeometry.cs','CrystalCity02DMaterials.cs','CrystalCity02DBatch.cs','DistrictBuilding.Fantasy02D.cs']
checks=[]
def check(name, result): checks.append({'check':name,'passed':bool(result)})
def code_only(s):
    return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', s, flags=re.S)
def balanced(s):
    stack=[]
    for c in code_only(s):
        if c in '({[':stack.append(c)
        elif c in ')}]':
            if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]: return False
    return not stack
sources={f:(ART/f).read_text(encoding='utf-8') for f in FILES}
for f,s in sources.items():
    check(f+' delimiters',balanced(s))
    check(f+' no unsupported test attribute','NonParallelizable' not in s)
    check(f+' no typo ParticleSystem','ParticlesSystem' not in s)
    check(f+' no dash glyphs','\u2014' not in s and '\u2013' not in s)
    check(f+' meta exists',(ART/(f+'.meta')).exists())
# Preserved automatic entry points are more important than line counts.
a=sources['DistrictArchitecture.cs']
check('building is partial','sealed partial class DistrictBuilding' in a)
check('facade palette installed','CrystalCity02DMaterials.Get(_fantasyStyle, material)' in a)
check('new roof composition is called','LandmarkRoof02D(f, top);' in a)
check('new facade relief is called','FacadeRelief02D(center, rot, width, h, tier);' in a)
check('runtime caches reset',all(x+'.ResetStatics();' in a for x in ('CrystalCity02DGeometry','CrystalCity02DMaterials','CrystalGardenDressing')))
g=sources['CrystalCity02DGeometry.cs']
check('all twelve mesh factories',all('case "'+n+'":' in g for n in NAMES))
check('zero-area rejection','n.sqrMagnitude < 1e-14f' in g)
check('normals and UVs authored','mesh.SetNormals(_n)' in g and 'mesh.SetUVs(0, _uv)' in g)
check('near and far cache keys','_lod' in g and '_detail' in g)
check('new garden owns output meshes','OwnedMeshes.Track' in sources['CrystalCity02DBatch.cs'])
check('garden has LODs','new LOD(.06f, near)' in sources['CrystalCity02DBatch.cs'])
new='\n'.join(sources[f] for f in FILES if f!='DistrictArchitecture.cs')
for token in ('AddComponent<BoxCollider>','AddComponent<Rigidbody>','Physics.IgnoreLayerCollision','Time.timeScale =','SetCinematic(','RenderSettings.','Input.GetKey','OnGUI(','Canvas','NavMeshAgent','UnityEditor'):
    check('new runtime avoids '+token,token not in new)
check('no new runtime reflection-based magic','System.Reflection' not in new)
# These are independent numerical checks of the authored layout rules, not a Unity execution.
style=lambda x: 2 if x < -180 else (1 if x > 240 else 0)
check('palette boundary reference',all(style(x)==v for x,v in [(-181,2),(-180,0),(0,0),(240,0),(241,1)]))
check('park pocket bounds for 175 sizes',all((span*.5-6)+3 < span*.5 and (span*.5-6)-3 > 0 for span in range(26,201)))
results={'scope':'offline source contracts and independent placement arithmetic only','checks':len(checks),'passed':sum(x['passed'] for x in checks),'failed':[x for x in checks if not x['passed']], 'unity_compile':'NOT RUN','unity_editmode':'NOT RUN','unity_playtest':'NOT RUN','frame_time':'NOT MEASURED','sources':{f:hashlib.sha256(s.encode()).hexdigest() for f,s in sources.items()}}
print(json.dumps(results,indent=2))
if results['failed']:sys.exit(1)
