"""Measure the serialized Vale meshes and their physical route. No Unity process or game simulation."""
import argparse
import json
import math
import re
import numpy as np
from assets import ART, CODE, ROOT, SCENE
from review import read_scene


class Solids:
    def __init__(self, docs, objects, meshes):
        triangles=[]; names=[]
        for name,vertices,normals,colors,indices in meshes:
            assert np.isfinite(vertices).all() and np.isfinite(normals).all(), name
            ids=objects[name][2]
            comps={docs[i][0]:docs[i][1] for i in ids}
            if 64 not in comps:
                continue
            assert comps[64]['m_Mesh']==comps[33]['m_Mesh'] and comps[64]['m_IsTrigger']==0, name
            assert 23 in comps and comps[23]['m_Enabled'] and comps[64]['m_Enabled'], name
            tri=vertices[indices];triangles.extend(tri);names.extend([name]*len(tri))
        self.t=np.array(triangles);self.names=np.array(names)
        self.lo=self.t.min(axis=1);self.hi=self.t.max(axis=1);self.e1=self.t[:,1]-self.t[:,0];self.e2=self.t[:,2]-self.t[:,0]
        normals=np.cross(self.e1,self.e2);areas=np.linalg.norm(normals,axis=1)
        assert np.min(areas)>1e-8, 'Degenerate physical triangle'
        self.n=normals/areas[:,None]
        self.count=len(set(names))

    def column(self,x,z):
        ids=np.flatnonzero((self.lo[:,0]<=x+1e-6)&(self.hi[:,0]>=x-1e-6)&
                           (self.lo[:,2]<=z+1e-6)&(self.hi[:,2]>=z-1e-6)&(np.abs(self.n[:,1])>.001))
        x0=self.t[ids,0,0];z0=self.t[ids,0,2];dx=x-x0;dz=z-z0
        ax,az=self.e1[ids,0],self.e1[ids,2];bx,bz=self.e2[ids,0],self.e2[ids,2]
        den=ax*bz-az*bx
        u=(dx*bz-bx*dz)/den;v=(ax*dz-dx*az)/den
        y=self.t[ids,0,1]+u*self.e1[ids,1]+v*self.e2[ids,1]
        inside=(u>=-1e-6)&(v>=-1e-6)&(u+v<=1+1e-6)
        return y[inside],ids[inside]

    def floor(self,x,z,previous):
        ys,ids=self.column(x,z)
        ground=(self.n[ids,1]>=math.cos(math.radians(51))) & (ys<previous+.31)
        if not np.any(ground):
            raise AssertionError(f'Missing walkable ground at ({x:.2f},{z:.2f}) after Y {previous:.2f}')
        y=float(ys[ground].max())
        assert y>=previous-.305, f'Unsupported step at ({x:.2f},{z:.2f}) {previous:.2f} to {y:.2f}'
        head=(ys>y+.305)&(ys<y+1.86)
        assert not np.any(head), f'Body obstruction at ({x:.2f},{z:.2f}): {self.names[ids[head]]}'
        return y

    def hit(self,a,b):
        """Nearest two-sided triangle on an open segment, including vertical gameplay obstructions."""
        a=np.array(a);b=np.array(b);direction=b-a
        ids=np.flatnonzero(((self.lo<=np.maximum(a,b)+1e-5)&(self.hi>=np.minimum(a,b)-1e-5)).all(axis=1))
        h=np.cross(direction,self.e2[ids]);det=np.sum(self.e1[ids]*h,axis=1)
        valid=np.abs(det)>1e-9
        ids,h,det=ids[valid],h[valid],det[valid]
        delta=a-self.t[ids,0];q=np.cross(delta,self.e1[ids])
        u=np.sum(delta*h,axis=1)/det;v=(q@direction)/det;t=np.sum(self.e2[ids]*q,axis=1)/det
        ok=(u>=-1e-6)&(v>=-1e-6)&(u+v<=1+1e-6)&(t>1e-5)&(t<1-1e-5)
        if not np.any(ok):
            return None
        i=np.argmin(t[ok])
        return self.names[ids[ok][i]],float(t[ok][i])


def route(solids,points,initial):
    """Probe three strips across Malik's capsule footprint and ray-test chest height along each."""
    previous=[initial]*3;last=[None]*3;steps=0;peak=0;length=0
    for a,b in zip(np.array(points,float)[:-1],np.array(points,float)[1:]):
        delta=b-a;span=np.linalg.norm(delta);length+=span;count=max(1,math.ceil(span/.28))
        lateral=np.array([-delta[1],delta[0]])/max(span,1e-8)
        if last[0] is not None:
            # At a corner (or retracing from Dante) left/right swap. Follow the nearest
            # point of the previous footprint rather than inventing a step across the slope.
            old_last=list(last);old_previous=list(previous)
            for j,offset in enumerate([-.34,0,.34]):
                projected=a+lateral*offset
                k=min(range(3),key=lambda k: np.linalg.norm(old_last[k][[0,2]]-projected))
                last[j]=old_last[k];previous[j]=old_previous[k]
        for i in range(count+1):
            at=a+i/count*delta
            for j,offset in enumerate([-.34,0,.34]):
                x,z=at+lateral*offset;y=solids.floor(x,z,previous[j])
                chest=np.array([x,y+.98,z])
                if last[j] is not None:
                    obstacle=solids.hit(last[j],chest)
                    assert obstacle is None,f'Chest-height obstruction at {x:.2f},{z:.2f}: {obstacle}'
                last[j]=chest;peak=max(peak,abs(y-previous[j]));previous[j]=y
            steps+=1
    return {'length_m':round(length,1),'cross_sections':steps,'largest_sample_rise_or_drop_m':round(peak,3)}


def check():
    docs,objects,meshes,camera=read_scene()
    def xyz(name):
        return objects[name][1][:3,3]
    def xz(name):
        return xyz(name)[[0,2]]
    def at(name,x,y,z):
        return (objects[name][1]@np.array([x,y,z,1]))[:3]
    def xzat(name,x,z):
        return at(name,x,0,z)[[0,2]]
    s=Solids(docs,objects,meshes)
    source=SCENE.read_text()
    guid_map={}
    for folder in [ART,CODE]:
        for p in folder.rglob('*.meta'):
            guid_map[re.search(r'^guid: ([0-9a-f]+)',p.read_text(),re.M)[1]]=p
    for g in re.findall(r'guid: ([0-9a-f]{32})',source):
        assert g in guid_map or g.startswith('0000000000000000'),g
    owners=[d for k,d in docs.values() if k==114]
    assert len(owners)==1 and 'ShardwakeVale' in owners[0]['m_EditorClassIdentifier']
    owner=owners[0]
    for field in ['spawn','dante','bridge','refuge','landmark','pickup','bellowsWheel','terrain','forgeHeart','forgeFire','furnaceStone','view']:
        assert owner[field]['fileID'] in docs,field
    assert len(owner['sockets'])==3 and all(docs[x['fileID']][0]==4 for x in owner['sockets'])
    assert all(owner[k]['guid'] in guid_map for k in ['commission','signalMaterial'])
    shot=camera[0];screen={}
    for name,target in [('workshop','Copper throat and swept roof'),('bridge','Stone span deck and parapets'),('landmark','Split mineral spine')]:
        row=next(row for row in meshes if row[0]==target)
        vertices=row[1];eye=shot[:3,3];cam=(vertices-eye)@shot[:3,:3]
        visible=cam[:,2]>.1
        proj=cam[visible,:2]/cam[visible,2,None]
        sx=.5+proj[:,0]/(2*math.tan(math.radians(29))*16/9)
        sy=.5+proj[:,1]/(2*math.tan(math.radians(29)))
        in_frame=(sx>0)&(sx<1)&(sy>0)&(sy<1)
        assert np.count_nonzero(in_frame)>=10,f'{target} outside opening frame'
        target_ids=np.flatnonzero(visible)[in_frame]
        clear=sum(s.hit(eye,vertices[i]) is None or s.hit(eye,vertices[i])[0]==target for i in target_ids[::max(1,len(target_ids)//36)])
        assert clear>=2,f'{target} is hidden by foreground geometry'
        screen[name]={'center_x_fraction':round(float(np.median(sx[in_frame])),2),'unobstructed_surface_sample_rays':clear}
    assert screen['workshop']['center_x_fraction']<screen['bridge']['center_x_fraction']
    trail=next(row for row in meshes if row[0]=='Footpath - both banks')
    centres=trail[1][trail[4]].mean(axis=1);gap=[]
    for x,y,z in centres:
        heights,ids=s.column(x,z)
        floor=heights[np.isin(s.names[ids],['Ochre hearth meadow','Broken ravine walls and bed','Sage and lilac far bank'])]
        assert len(floor),f'Paint extends beyond the terrain at ({x},{z})'
        gap.append(float(y-floor.max()))
    assert min(gap)>.025 and max(gap)<.031,'Footpath paint detached or buried'

    spawn='Malik - start and facing';shop='02 - Dante workshop';bridge='03 - Three arch pack bridge';crown='04 - Split Crown landmark'
    talk=xzat(shop,6.25,-2.65);clamp=xzat(shop,.16,-.88)
    assert np.linalg.norm(talk-xz('Dante - conversation and actor'))<3.1 and np.linalg.norm(clamp-xz('Installed hearth lens'))<2.9
    foot=[xzat(bridge,0,z) for z in (-16,-10,0,13,16)]
    out=[xz(spawn),(-7,-9),(-8.5,-4),xzat(shop,9.18,-2.55),talk,xzat(shop,9.18,-2.55),(-8,5),(-5,8),(0,10),(5,14),(10,19),*foot,
         (-3.9,59),(-4,63),(-10,67),(-14,71),(-15,75),(-12,79),(-5,82),(2,86),(7,91),(9,94)]
    excursion=[out[-1],(16,95),(21.5,98),(23,104),(22,109),(18.5,112),(13,114),(9,114),(6,112),
               (4,109),(4,103),(4.2,97),(5.5,94.8),(9,95.5),(12,96.5)]
    assert np.linalg.norm(excursion[-1]-xz('Removable hearth seed'))<2.8
    outbound=route(s,out,xyz(spawn)[1])
    # The near bank, bridge, far bank, approach, and return all use actual MeshCollider triangles.
    y0=max(y for y in s.column(*out[-1])[0] if y<15)
    around=route(s,excursion,y0)
    back=excursion[-1],(9,94)
    reentry=list(back)+list(reversed(out[4:-1]))+[xzat(shop,2,-2.2),clamp]
    y0=max(y for y in s.column(*back[0])[0] if y<15)
    returned=route(s,reentry,y0)
    shots=[]
    for i,(x,z) in enumerate([excursion[0],excursion[4],excursion[9]]):
        ys,ids=s.column(x,z);y=float(max(ys[s.n[ids,1]>.75]))
        a=np.array([x,y+1.32,z]);b=at('Live mineral '+str(i+1),0,1.48,0)
        blocked=s.hit(a,b);assert blocked is None,f'Regulator {i+1} blocked from ({x},{z}) by {blocked}'
        distance=round(np.linalg.norm(b-np.array([x,y,z])),2);assert distance<15.5
        shots.append(float(distance))
    assert docs[objects['Installed hearth lens'][0]][1]['m_IsActive']==0 and docs[objects['Removable hearth seed'][0]][1]['m_IsActive']==0
    return {'scope':'Serialized mesh and scene checks. No Unity execution.','solid_mesh_owners':s.count,
            'physical_triangles':len(s.t),'same_mesh_filter_and_collider':True,'opening':screen,
            'paint_ground_gap_m':[round(min(gap),4),round(max(gap),4)],
            'outbound':outbound,'crown_loop':around,'return_to_clamp':returned,'clear_regulator_distances_m':shots}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=argparse.FileType('w'))
    args=p.parse_args();result=json.dumps(check(),indent=2)+'\n'
    print(result,end='')
    if args.output:args.output.write(result)
