#!/usr/bin/env python3
"""Validate the actual shipped OBJ/PNG/JSON assets and source integration.
Not a Unity compile, not a C# execution and not a frame-rate or art-quality score.
Usage: python tools/check_crystal_city_02f.py [--baseline PATH] [--report PATH]
"""
import argparse, hashlib, json, math, re, struct, sys
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
ART=Path('Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict')
RES=Path('Assets/NeonSyndicate/Resources/CrystalCity02F')
RESULT=[]

def check(name, result, detail=''):
    RESULT.append({'check':name,'passed':bool(result),'detail':detail})

def text_hash(p):
    return hashlib.sha256(p.read_text(encoding='utf-8-sig').replace('\r\n','\n').replace('\r','\n').encode('utf-8')).hexdigest()

def obj(path):
    v=[];normals=[];uv=[];f=[]
    for line in path.read_text().splitlines():
        a=line.split()
        if not a:continue
        if a[0]=='v':v.append([float(x) for x in a[1:4]])
        elif a[0]=='vn':normals.append([float(x) for x in a[1:4]])
        elif a[0]=='vt':uv.append([float(x) for x in a[1:3]])
        elif a[0]=='f':
            assert len(a)==4, 'Non-triangle face'
            triplets=[[int(y) for y in x.split('/')] for x in a[1:]]
            assert all(t[0]==t[1]==t[2] for t in triplets), 'Expected authored per-vertex UV and normal indices'
            f.append([x[0]-1 for x in triplets])
    return np.asarray(v,float),np.asarray(normals,float),np.asarray(uv,float),np.asarray(f,int)

def rotate(euler):
    x,y,z=map(math.radians,euler);cx,sx=math.cos(x),math.sin(x);cy,sy=math.cos(y),math.sin(y);cz,sz=math.cos(z),math.sin(z)
    rx=np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]]);ry=np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]]);rz=np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])
    return ry@rx@rz # Unity's documented Z, X, Y Euler order.

def mask_code(s):
    # Lexer-like removal of strings, char literals and comments, NOT a C# parser.
    return re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '',s,flags=re.S)

def balanced(s):
    stack=[]
    for c in mask_code(s):
        if c in '([{':stack.append(c)
        elif c in ')]}':
            if not stack or stack.pop()!={')':'(',']':'[','}':'{'}[c]:return False
    return not stack

def method(s,signature):
    start=s.index('{',s.index(signature));depth=1;i=start+1
    # Target methods do not contain braces in string literals. Used for narrow preservation checks.
    while depth and i<len(s):
        depth += (s[i]=='{')-(s[i]=='}');i+=1
    return s[start:i]

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--baseline',type=Path);ap.add_argument('--report',type=Path);args=ap.parse_args()
    data=json.loads((ROOT/RES/'city02f_blueprints.json').read_text());inventory=json.loads((ROOT/RES/'asset_inventory.json').read_text())
    models={};stats={};check('catalog version',data['version']=='02F');check('14 model definitions',len(data['models'])==14)
    check('nine compositions',len(data['blueprints'])==9);check('model IDs unique',len(set(data['models']))==len(data['models']))
    guidset=set()
    for p in (ROOT/RES).rglob('*.meta'):
        s=p.read_text();m=re.search(r'^guid: ([a-f0-9]{32})$',s,re.M)
        check(str(p.relative_to(ROOT))+' valid GUID',m is not None)
        if m:check(str(p.relative_to(ROOT))+' unique GUID',m[1] not in guidset);guidset.add(m[1])
    for name in data['models']:
        levels=[]
        for low in [False,True]:
            key=name+('_lod' if low else '');p=ROOT/RES/'Models'/(key+'.obj')
            v,n,uv,f=obj(p);models[key]=(v,n,uv,f)
            check(key+' nonempty mesh',len(v)>0 and len(f)>0)
            check(key+' finite vertices and UVs',np.isfinite(v).all() and np.isfinite(uv).all())
            check(key+' indices within mesh',f.min()>=0 and f.max()<len(v))
            check(key+' vertex normal UV counts',len(v)==len(n)==len(uv))
            cross=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]]);area=np.linalg.norm(cross,axis=1)
            check(key+' zero-area triangles absent',area.min()>1e-10,str(area.min()))
            check(key+' winding matches authored normals',np.min(np.einsum('ij,ij->i',cross/area[:,None],n[f[:,0]]))>.99)
            check(key+' normals normalized',np.max(abs(np.linalg.norm(n,axis=1)-1))<1e-5)
            s=p.with_name(p.name+'.meta').read_text()
            check(key+' importer readable and collision-free','isReadable: 1' in s and 'addColliders: 0' in s and 'importAnimation: 0' in s)
            levels.append(len(f));stats[key]={'triangles':len(f),'vertices':len(v),'min':v.min(0).tolist(),'max':v.max(0).tolist()}
        check(name+' actual LOD reduces triangles',levels[1]<levels[0])
    palette_roles={'stone','gilt','crystal','bark','leaf','cloth'}
    for b in data['blueprints']:
        points=[];animated=0
        for node in b['nodes']:
            check(b['id']+'/'+node['model']+' references valid',node['model'] in data['models'] and node['material'] in palette_roles)
            scale=np.asarray(node['scale']);p=np.asarray(node['position']);rot=rotate(node['euler']);v=models[node['model']][0]
            check(b['id']+'/'+node['model']+' positive scale and finite transform',np.all(scale>.001) and np.isfinite(rot).all() and np.isfinite(p).all())
            points.append((v*scale)@rot.T+p)
            if node.get('speed',0):
                animated+=1;check('spin axis exists and speed bounded',len(node.get('spin',[]))==3 and 0<abs(node['speed'])<=12 and np.linalg.norm(node['spin'])>.01)
        points=np.vstack(points);radius=np.linalg.norm(points[:,[0,2]],axis=1).max()
        check(b['id']+' declared radius contains actual geometry',radius<=b['radius']+.04,f'actual {radius:.3f}; declared {b["radius"]}')
        check(b['id']+' declared height contains actual geometry',points[:,1].min()>=b['minY']-.04 and points[:,1].max()<=b['maxY']+.04,f'actual {points[:,1].min():.3f}..{points[:,1].max():.3f}')
        check(b['id']+' per-site animation cap',animated<=2)
    for p in (ROOT/RES/'Textures').glob('*.png'):
        raw=p.read_bytes();w,h=struct.unpack('>II',raw[16:24]);check(p.stem+' 512 square PNG',raw[:8]==b'\x89PNG\r\n\x1a\n' and w==512 and h==512)
        meta=p.with_name(p.name+'.meta').read_text();check(p.stem+' mips enabled','enableMipMap: 1' in meta)
        if p.stem.endswith('_Normal'):check(p.stem+' normal-map importer','textureType: 1' in meta and 'sRGBTexture: 0' in meta)
        if p.stem.endswith('_Mask'):check(p.stem+' mask linear','sRGBTexture: 0' in meta)
    sources={p.name:p.read_text() for p in (ROOT/ART).glob('CrystalCity02F*.cs')}
    for name,s in sources.items():
        check(name+' balanced lexical delimiters',balanced(s))
        check(name+' has meta',(ROOT/ART/(name+'.meta')).exists())
        check(name+' no forbidden attribute/typo','NonParallelizable' not in s and 'ParticlesSystem' not in s)
    joined='\n'.join(sources.values())
    for forbidden in ('Time.timeScale =','RenderSettings.','AddComponent<BoxCollider>','AddComponent<Rigidbody>','Input.GetKey','SetCinematic(','OnGUI(', 'using UnityEditor;', 'PlayerPrefs.Set','File.Delete('):
        check('new runtime does not use '+forbidden,forbidden not in joined)
    check('new runtime does not assign Game.State/Seed',re.search(r'Game\.(?:State|Seed)\s*=(?!=)',mask_code(joined)) is None)
    architecture=(ROOT/ART/'DistrictArchitecture.cs').read_text();city=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs').read_text()
    check('automatic build hook once',city.count('yield return CrystalCity02FWorld.Build(L, _root);')==1)
    check('new source cache has reset hook',architecture.count('CrystalCity02FAssets.ResetStatics();')==1)
    check('optional iterator failures caught','pass.MoveNext()' in sources['CrystalCity02FWorld.cs'] and 'continuing base city boot' in sources['CrystalCity02FWorld.cs'])
    check('one motion controller creation',joined.count('AddComponent<CrystalCity02FMotion>')==1)
    check('world has idempotent install check','parent.Find(RootName) != null' in joined)
    check('new mesh outputs owned','OwnedMeshes.Track' in sources['CrystalCity02FComposer.cs'])
    check('no native Destroy on imported meshes','Destroy(mesh)' not in sources['CrystalCity02FAssets.cs'])
    check('landmark full-map marker ownership', 'ShowOnMinimap = false' in sources['CrystalCity02FMotion.cs'] and 'Blip.Remove(marker)' in sources['CrystalCity02FMotion.cs'])
    check('landmark map marker integrated after assembly', 'motion.AddMapMarker(group.Transform' in sources['CrystalCity02FWorld.cs'])
    check('glade route-segment clearance exists', 'layout.WalkLinks' in sources['CrystalCity02FPlanner.cs'])
    check('custom lot protection retained', 'lot.CustomBuilt' in sources['CrystalCity02FPlanner.cs'] and 'lot.CustomBuilt' in sources['CrystalCity02FWorld.cs'])
    blips=(ROOT/'Assets/NeonSyndicate/Scripts/Runtime/UI/Minimap.cs').read_text()
    check('source Blip.Add signature exists','public static Blip Add(BlipKind kind, Transform target, Vector3 pos, Color color, string letter, string label, bool edgeClamp)' in blips)
    check('source map API fields exist',all(x in blips for x in ('ShowOnMap','ShowOnMinimap','public bool Visible','public static void Remove(Blip b)')))
    # Independent arithmetic for actual placement constants. It does not execute the C# planner.
    for span in [10,12,14,18,25,40,100]:
        scale=min(1.45,max(.1,(span-2.4)/9.6));check('roof envelope '+str(span),scale*4.8+1.1<span/2)
        check('island clear of old equipment '+str(span),12-3.7*scale>6.5)
    check('full-cycle rotor wrap',all((3600*speed)%360==0 for speed in [3,-4,4,5,-8]))
    if args.baseline:
        for rel in ['Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs',str(ART/'DistrictArchitecture.cs')]:
            old=(args.baseline/rel).read_text();new=(ROOT/rel).read_text()
            if rel.endswith('CityBuilder.cs'):
                new=new.replace('            // 02F composes bounded imported landmarks on the approved world; no manual scene setup.\n            yield return CrystalCity02FWorld.Build(L, _root);\n','')
            else:new=new.replace('            CrystalCity02FAssets.ResetStatics();\n','')
            check(rel+' no unrelated code changed',old==new)
    report={'scope':'Actual OBJ/PNG/JSON validation plus source-contract checks and independent placement arithmetic','checks':len(RESULT),'passed':sum(x['passed'] for x in RESULT),'failed':[x for x in RESULT if not x['passed']], 'unity_compile':'NOT RUN','unity_editmode':'NOT RUN','CSharp_execution':'NOT RUN','gameplay':'NOT RUN','frame_rate':'NOT MEASURED','asset_statistics':stats,'source_sha256':{name:text_hash(ROOT/ART/name) for name in sources}}
    text=json.dumps(report,indent=2)
    if args.report: args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(text+'\n')
    print(json.dumps({k:v for k,v in report.items() if k not in ['asset_statistics','source_sha256']},indent=2))
    return 1 if report['failed'] else 0
if __name__=='__main__':sys.exit(main())
