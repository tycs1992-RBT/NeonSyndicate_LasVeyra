#!/usr/bin/env python3
"""Audit actual 04B asset payloads and authored collision/approach geometry.

This is numerical source verification, not Unity import, physics or gameplay.
Requires numpy and Pillow (shared geometry helpers are used from the 04A audit).
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import numpy as np
from check_visual_rebirth_04a import Audit, decode, obj_vertices, transform_vertices, intersects_box, visual_surface_at

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/NeonSyndicate/Resources/LivingTerritories04B'
OLD=ROOT/'Assets/NeonSyndicate/Resources/VisualRebirth04A'

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report',type=Path,required=True)
    args=parser.parse_args()
    audit=Audit();cache={};inventory=json.loads((ART/'Models/model_inventory.json').read_text());rows=inventory['models']
    audit.check('unique model variants',len({r['variant'] for r in rows})==len(rows))
    audit.check('model inventory includes each binary',set(r['variant'] for r in rows)==set(p.stem for p in (ART/'Meshes').glob('*.bytes')))
    for row in rows:
        key=row['variant'];path=ROOT/row['binary'];raw,data=decode(path);p=data[:,:3];n=data[:,3:6];tri=p.reshape(-1,3,3).astype(np.float64)
        cross=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);areas=np.linalg.norm(cross,axis=1)
        audit.check(key+': finite source attributes',np.isfinite(data).all())
        audit.check(key+': unit authored normals',np.allclose(np.linalg.norm(n,axis=1),1,atol=2e-4))
        audit.check(key+': nondegenerate triangles',np.all(areas>1e-9),float(areas.min()))
        audit.check(key+': consistent triangle winding',np.all(np.sum(cross*n[::3],axis=1)>0))
        audit.check(key+': usable UVs',np.abs(data[:,6:]).max()<100 and np.ptp(data[:,6])>.01 and np.ptp(data[:,7])>.01)
        expanded=obj_vertices(ROOT/row['source'])
        audit.check(key+': editable OBJ equals runtime bytes',expanded.shape==data.shape and np.allclose(expanded,data,atol=3e-5))
        audit.check(key+': manifest hash and counts',hashlib.sha256(raw).hexdigest()==row['sha256'] and row['vertices']==len(data) and row['triangles']==len(tri))
        audit.check(key+': manifest bounds',np.allclose(p.min(0),row['bounds_min'],atol=1e-5) and np.allclose(p.max(0),row['bounds_max'],atol=1e-5))
        cache['04B/'+key]=p
    for row in rows:
        if row['lod']==0:
            high=len(cache['04B/'+row['variant']]);low=len(cache['04B/'+row['variant'].replace('_high','_low')])
            audit.check(row['name']+': distinct lower detail budget',0<low<high)
    surfaces=json.loads((OLD/'material_manifest.json').read_text())['surfaces'];roles={(r['style'],r['role']) for r in surfaces}
    def vertices(source,model,lod):
        key=source+'/'+model+'_'+lod
        if key not in cache:cache[key]=decode((OLD if source=='04A' else ART)/'Meshes'/(model+'_'+lod+'.bytes'))[1][:,:3]
        return cache[key]
    layouts={}
    for path in sorted((ART/'Layouts').glob('*.json')):
        layout=json.loads(path.read_text());name=layout['id'];nodes=layout['nodes'];counts={};solid_triangles={}
        audit.check(name+': version and stable file ID',layout['version']=='04B' and name==path.stem)
        audit.check(name+': bounded composition',0<len(nodes)<=160)
        for lod in ('high','low'):
            triangles=[];solids=[];bounds=[];paths=[];invalid=[];unknown=[]
            for index,node in enumerate(nodes):
                label=str(index)+': '+node['model'];v=vertices(node['source'],node['model'],lod)
                if (layout['style'],node['role']) not in roles:unknown.append(label)
                if not (np.isfinite(node['position']).all() and np.isfinite(node['scale']).all() and np.isfinite(node['euler']).all() and min(node['scale'])>0):invalid.append(label);continue
                transformed=transform_vertices(v,node);tri=transformed.reshape(-1,3,3);triangles.append(tri)
                if node['solid']:solids.append(tri)
                lo,hi=v.min(0),v.max(0)
                corners=np.asarray([[hi[a] if mask&(1<<a) else lo[a] for a in range(3)] for mask in range(8)])
                corners=transform_vertices(corners,node)
                if not (np.abs(corners[:,[0,2]]).max()<=23.8 and corners[:,1].min()>=-.2 and corners[:,1].max()<=32):bounds.append([label,corners.min(0).tolist(),corners.max(0).tolist()])
                for clear in layout['clearAreas']:
                    x0,x1,z0,z1=clear['rect']
                    if intersects_box(tri,np.asarray([x0,.2,z0]),np.asarray([x1,2.5,z1])):paths.append(label);break
            counts[lod]=sum(len(t) for t in triangles);solid_triangles[lod]=np.concatenate(solids) if solids else np.zeros((0,3,3))
            audit.check(name+'/'+lod+': transforms finite and positive',not invalid,invalid)
            audit.check(name+'/'+lod+': materials resolve to original shared family',not unknown,unknown)
            audit.check(name+'/'+lod+': conservative surveyed bounds',not bounds,bounds)
            audit.check(name+'/'+lod+': authored pedestrian approaches clear',not paths,paths)
            # The inherited portal's detached caps need actual ground-connected support in each 04B composition.
            for portal in [node for node in nodes if node['model']=='portal']:
                caps=transform_vertices(np.asarray([[-3.65,7.5,0],[3.65,7.5,0]]),portal)
                supported=[]
                for point in caps:
                    found=False
                    for column in [n for n in nodes if n['model']=='column' and n['solid'] and n.get('variant',0) in (0,portal.get('variant',0))]:
                        points=transform_vertices(vertices(column['source'],column['model'],lod),column)
                        if points[:,1].min()<=.05 and visual_surface_at(points.reshape(-1,3,3),*point):found=True;break
                    supported.append(found)
                audit.check(name+'/'+lod+': portal shoulder caps have grounded column support',all(supported),supported)
            # Every advertised fly-to landing has an actual visible solid top and headroom.
            x,y,z=layout['overlook'];t=solid_triangles[lod]
            support=[visual_surface_at(t,x+dx,5,z+dz) for dx,dz in ((0,0),(-1,-1),(-1,1),(1,-1),(1,1))]
            audit.check(name+'/'+lod+': landing surface supports center and four corners',all(support))
            audit.check(name+'/'+lod+': landing has standing headroom',not intersects_box(t,np.asarray([x-.55,5.1,z-.55]),np.asarray([x+.55,7.2,z+.55])))
        low=solid_triangles['low']
        for kind in ('defenders','staff','creatures'):
            blocked=[]
            for index,anchor in enumerate(layout[kind]):
                p=np.asarray(anchor['position']);radius=.6 if kind!='creatures' else .8
                if intersects_box(low,p+[-radius,.15,-radius],p+[radius,2.15,radius]):blocked.append(index)
            audit.check(name+': '+kind+' anchors clear of authored solid triangles',not blocked,blocked)
        core=np.asarray(layout['core'])
        audit.check(name+': core interaction envelope is open',not intersects_box(low,core+[-1,.2,-1],core+[1,2.2,1]))
        layouts[name]={'parts':len(nodes),'solid_nodes':sum(n['solid'] for n in nodes),'high_triangles':counts['high'],'low_triangles':counts['low'],'note':'All tiers summed. Native installed position/collision NOT RUN.'}
    home_path=ART/'Props/home_ledger.json'
    if home_path.is_file():
        home=json.loads(home_path.read_text());steps=[n.get('progress',0) for n in home['nodes'] if n.get('progress',0)>0]
        audit.check('home ledger: exactly nine distinct progress ornaments',sorted(steps)==list(range(1,10)))
        audit.check('home ledger: progress cannot add physical obstacles',all(not n['solid'] for n in home['nodes'] if n.get('progress',0)>0))
        for lod in ('high','low'):
            solids=[];outside=[]
            for index,node in enumerate(home['nodes']):
                transformed=transform_vertices(vertices(node['source'],node['model'],lod),node)
                if node['solid']:solids.append(transformed.reshape(-1,3,3))
                if np.abs(transformed[:,[0,2]]).max()>3.95 or transformed[:,1].min()<-.2 or transformed[:,1].max()>5.8:outside.append(index)
            audit.check('home ledger/'+lod+': visible geometry fits its surveyed footer box',not outside,outside)
            solid=np.concatenate(solids)
            obstructed=[]
            for node in home['nodes']:
                if node.get('progress',0)<=0:continue
                transformed=transform_vertices(vertices(node['source'],node['model'],lod),node)
                if intersects_box(solid,transformed.min(0),transformed.max(0)):obstructed.append(node['progress'])
            audit.check('home ledger/'+lod+': progress crystals clear actual structure',not obstructed,obstructed)
            for label,point in [('core',home['core']),('caretaker',home['staff'][0]['position'])]:
                at=np.asarray(point)
                audit.check('home ledger/'+lod+': '+label+' has standing clearance',not intersects_box(solid,at+[-.55,.15,-.55],at+[.55,2.15,.55]))
    guids={};duplicates=[]
    for p in (ROOT/'Assets').rglob('*.meta'):
        m=re.search(r'^guid: ([a-f0-9]{32})$',p.read_text(),re.M)
        if m:
            if m[1] in guids:duplicates.append([guids[m[1]],str(p.relative_to(ROOT))])
            guids[m[1]]=str(p.relative_to(ROOT))
    audit.check('Unity asset GUIDs remain unique',not duplicates,duplicates)
    for p in [ART,*ART.rglob('*')]:
        if p.suffix!='.meta':audit.check(str(p.relative_to(ROOT))+': Unity metadata present',Path(str(p)+'.meta').is_file())
    report={'scope':__doc__.strip(),'checks':len(audit.checks),'passed':sum(c['passed'] for c in audit.checks),'failed':[c for c in audit.checks if not c['passed']],
        'unique_masters':inventory['unique_prototypes'],'mesh_variants':len(rows),'high_library_triangles':sum(r['triangles'] for r in rows if r['lod']==0),
        'low_library_triangles':sum(r['triangles'] for r in rows if r['lod']==1),'layouts':layouts,'unity_import':'NOT RUN','unity_collision':'NOT RUN'}
    args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k!='layouts'},indent=2))
    return 0 if not report['failed'] else 1

if __name__=='__main__':raise SystemExit(main())
