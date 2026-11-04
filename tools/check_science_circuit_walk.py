"""Find a continuous source-mesh walk over the circuit. No Unity navigation, collision cooking or actor simulation."""
import argparse,csv,heapq,json,math,struct
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

def meshes(path):
    with path.open('rb') as f:
        magic,groups=struct.unpack('<ii',f.read(8));assert magic==0x53463035
        for _ in range(groups):
            key=f.read(f.read(1)[0]).decode();n=struct.unpack('<i',f.read(4))[0]
            vs=np.frombuffer(f.read(32*n),'<f4').reshape(-1,8);n=struct.unpack('<i',f.read(4))[0]
            tri=np.frombuffer(f.read(4*n),'<i4').reshape(-1,3);yield key,vs,tri

# Grid coordinates are world X,Z at 25 cm spacing, with Z increasing down the internal array.
X0,Z0,STEP=-145,-43,.25
W,H=1001,801
def grid(x,z):return (int(round((x-X0)/STEP)),int(round((z-Z0)/STEP)))
def world(node):return (X0+node[0]*STEP,Z0+node[1]*STEP)
def blank():return Image.new('L',(W,H),0)
def strip(draw,a,b,r):
    da=np.array(b)-a;d=da/max(.000001,np.linalg.norm(da));s=np.array([-d[1],d[0]])*r
    draw.polygon([grid(*(a+s)),grid(*(b+s)),grid(*(b-s)),grid(*(a-s))],fill=255)

def write_heights(heights,triangles):
    """Sample the actual upward-facing triangles at grid nodes, retaining the highest low support."""
    for a,b,c in triangles:
        lo=np.maximum([0,0],np.ceil((np.minimum(np.minimum(a,c),b)[[0,2]]-[X0,Z0])/STEP).astype(int))
        hi=np.minimum([W-1,H-1],np.floor((np.maximum(np.maximum(a,c),b)[[0,2]]-[X0,Z0])/STEP).astype(int))
        if np.any(lo>hi):continue
        zz,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];x=X0+xx*STEP;z=Z0+zz*STEP
        den=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2]);
        if abs(den)<1e-10:continue
        wa=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den
        wb=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den;wc=1-wa-wb
        valid=(wa>=-1e-6)&(wb>=-1e-6)&(wc>=-1e-6);patch=heights[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        values=wa*a[1]+wb*b[1]+wc*c[1];patch[valid]=np.maximum(patch[valid],values[valid])

def survey(root):
    heights=np.zeros((H,W),np.float32)
    support=blank();block=blank();sd=ImageDraw.Draw(support);bd=ImageDraw.Draw(block)
    # Physical route boxes in BuildRealmLandscape04OB, plus finite flat-ground approaches at existing doorways.
    for row in csv.DictReader((root/'routes.csv').open()):
        a=np.array([float(row['ax']),float(row['az'])]);b=np.array([float(row['bx']),float(row['bz'])]);strip(sd,a,b,float(row['width'])/2)
    for a,b in [((0,-30),(0,-34)),((75,96),(75,100)),((-6,118),(-6,122)),((-94,89),(-94,92))]:strip(sd,np.array(a),np.array(b),2)
    # Treat unexported occupied parcels conservatively as full obstructions. The three service shells and cave use actual triangles.
    for row in csv.DictReader((root/'parcels.csv').open()):
        if int(row['seed']) not in (970617,584924,479130,961824):
            bd.rectangle([grid(float(row['x0']),float(row['z0'])),grid(float(row['x1']),float(row['z1']))],fill=255)
    for hole in csv.DictReader((root/'openings.csv').open()):
        bd.rectangle([grid(float(hole['x0']),float(hole['z0'])),grid(float(hole['x1']),float(hole['z1']))],fill=255)
    for role,vs,tri in meshes(root/'circuit.bin'):
        if role in ('leaf','ground','flagstone'):continue
        p=vs[tri,:3];norm=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);length=np.linalg.norm(norm,axis=1)
        walk=(norm[:,1]>.82*length)&(p[:,:,1].min(axis=1)>=-.01)&(p[:,:,1].max(axis=1)<.8)
        write_heights(heights,p[walk] if role!="terrain" else p[norm[:,1]>.05*length])
        for pts in p[walk][:,:,[0,2]]:sd.polygon([grid(*v) for v in pts],fill=255)
        # Four horizontal cuts of the exact solid geometry. Foliage never enters this mesh.
        for height in (.37,.85,1.55,2.05):
            intersects=(p[:,:,1].min(axis=1)<height)&(p[:,:,1].max(axis=1)>height)
            for pts in p[intersects]:
                ends=[]
                for i in range(3):
                    a,b=pts[i],pts[(i+1)%3]
                    if (a[1]<height)!=(b[1]<height):
                        hit=a+(b-a)*((height-a[1])/(b[1]-a[1]));ends.append(grid(hit[0],hit[2]))
                if len(ends)==2:bd.line(ends,fill=255,width=1)
    # Erode supported area and dilate solid cross sections by a 45 cm test body radius.
    walk=(ndimage.distance_transform_edt(np.array(support)>0)*STEP>=.45)&(ndimage.distance_transform_edt(np.array(block)==0)*STEP>.45)
    return walk,heights

def path_between(walk,heights,start,end):
    sx,sy=grid(*start);ex,ey=grid(*end)
    if not walk[sy,sx] or not walk[ey,ex]:raise ValueError(f'unsupported or occupied stop {start} -> {end}: {walk[sy,sx]} / {walk[ey,ex]}')
    dist={(sx,sy):0};parent={};queue=[(0,0,(sx,sy))]
    while queue:
        _,cost,node=heapq.heappop(queue)
        if cost!=dist[node]:continue
        if node==(ex,ey):
            path=[node]
            while path[-1]!=(sx,sy):path.append(parent[path[-1]])
            return list(reversed(path)),cost*STEP
        x,y=node
        for dx,dy in ((0,-1),(1,0),(0,1),(-1,0),(1,1),(1,-1),(-1,1),(-1,-1)):
            nx,ny=x+dx,y+dy
            if not (0<nx<W-1 and 0<ny<H-1 and walk[ny,nx] and walk[y,nx] and walk[ny,x]):continue
            if abs(heights[ny,nx]-heights[y,x])>.20:continue
            nc=cost+(math.sqrt(2) if dx and dy else 1)
            if nc>=dist.get((nx,ny),float('inf')):continue
            dist[nx,ny]=nc;parent[nx,ny]=node;heapq.heappush(queue,(nc+math.hypot(nx-ex,ny-ey),nc,(nx,ny)))
    raise ValueError(f'disconnected leg {start} -> {end}')

def grotto_view(root):
    """Check the exported sightline between the two banks, including foliage, at a standing camera height."""
    origin=np.array([-108.,1.82,29.]);target=np.array([-110.,3.2,64.1]);ray=target-origin;hits=[]
    for role,vs,indices in meshes(root/'circuit.bin'):
        triangles=vs[indices,:3];a=triangles[:,0];e=triangles[:,1]-a;f=triangles[:,2]-a
        h=np.cross(ray,f);det=np.einsum('ij,ij->i',e,h);ok=abs(det)>1e-8;inv=np.zeros_like(det);inv[ok]=1/det[ok]
        delta=origin-a;u=np.einsum('ij,ij->i',delta,h)*inv;q=np.cross(delta,e);v=(q@ray)*inv
        t=np.einsum('ij,ij->i',f,q)*inv;ok &= (u>=0)&(v>=0)&(u+v<=1)&(t>.0001)&(t<.9999)
        if np.any(ok):hits.append(role)
    return {'from':origin.tolist(),'to':target.tolist(),'obstructing_exported_roles':hits,'clear':not hits}

def route_map(root,output,report,points):
    image=Image.new('RGB',(1024,880),'#f3f0e8');draw=ImageDraw.Draw(image)
    font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    title=ImageFont.truetype(font_path,28);body=ImageFont.truetype(font_path,15)
    def xy(x,z):return (int(70+(x+140)*3.72),int(796-(z+42)*3.72))
    for row in csv.DictReader((root/'routes.csv').open()):
        a=np.array([float(row['ax']),float(row['az'])]);b=np.array([float(row['bx']),float(row['bz'])]);
        if np.any(a<[-145,-43]) or np.any(b<[-145,-43]) or np.any(a>[107,145]) or np.any(b>[107,145]):continue
        draw.line([xy(*a),xy(*b)],fill='#dbd6ca',width=max(2,round(float(row['width'])*3.72)))
    for row in csv.DictReader((root/'parcels.csv').open()):
        x0,z0,x1,z1=(float(row[k]) for k in ('x0','z0','x1','z1'));draw.rectangle([xy(x0,z1),xy(x1,z0)],fill='#c8c4b9',outline='#96998b')
    cursor=0
    for leg in report['legs']:
        nodes=points[cursor:cursor+leg['samples']];cursor+=len(nodes);colour='#287963' if cursor and leg['to'] in ('west loggia','woodland gateway','shelter','mineral approach','north mouth') else '#53728f' if leg['to'] in ('grotto arrival','south sill','south bend') else '#8c6136'
        if len(nodes)>1:draw.line([xy(*world(n)) for n in nodes],fill=colour,width=4)
    labels=[('Lofts',0,-33,8,4),('Forge',75,98,6,6),('Archive',-6,120,8,-25),('West loggia',-24,90,5,3),('Woodland',-52,92,-36,-24),('Shelter',-82,78,-36,5),('Grotto',-110,74,-61,-26),('South sill',-110,64,-71,0),('Threshold',-108,28,-80,3),('Seam station',-86,42,5,-22),('Return',-66,30,8,3)]
    for label,x,z,dx,dy in labels:
        px,py=xy(x,z);draw.ellipse([px-3,py-3,px+3,py+3],fill='#233b35');draw.text((px+dx,py+dy),label,font=body,fill='#203d36',stroke_width=2,stroke_fill='#f3f0e8')
    draw.line([xy(-86,29.375),xy(-86,39.9)],fill='#8c6136',width=2)
    draw.rectangle((0,0,1024,92),fill='#f3f0e8');draw.rectangle((0,820,1024,880),fill='#f3f0e8')
    draw.text((44,20),'CRYSTAL DISTRICT CIRCUIT',font=title,fill='#213b38')
    draw.text((45,60),'Canonical layout, world X,Z metres. Source walk survey at 0.25 m resolution.',font=body,fill='#40514b')
    draw.line([(824,844),(898,844)],fill='#213b38',width=3);draw.text((908,834),'20 m',font=body,fill='#213b38')
    draw.text((44,835),'Surveyed source path, not a Unity navigation or owner playtest capture.',font=body,fill='#40514b');image.save(output/'circuit_map.png',optimize=True)

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('export',type=Path);ap.add_argument('output',type=Path);args=ap.parse_args();args.output.mkdir(parents=True,exist_ok=True);walk,heights=survey(args.export)
    stops=[('Lofts',(0,-33)),('home court',(0,38)),('Forge',(75,98)),('Archive',(-6,120)),('Archive promenade east',(10,92)),('west loggia',(-24,90)),('woodland gateway',(-52,92)),('shelter',(-82,78)),('mineral approach',(-94,91.5)),('north mouth',(-110,82)),('grotto arrival',(-110,74)),('south sill',(-110,64)),('south bend',(-108,48)),('threshold court',(-108,28)),('settlement return',(-76,30)),('dispatch junction',(-20,6)),('Lofts',(0,-33))]
    legs=[];all_points=[];errors=[]
    for (name,start),(target,end) in zip(stops,stops[1:]):
        try:points,length=path_between(walk,heights,start,end);all_points.extend(points);legs.append({'from':name,'to':target,'metres':round(length,2),'samples':len(points),'max_height_step_m':round(max(abs(float(heights[y1,x1]-heights[y2,x2])) for (x1,y1),(x2,y2) in zip(points,points[1:])),3)})
        except ValueError as e:errors.append(str(e))
    spur=None
    try:
        _,length=path_between(walk,heights,(-86,29.375),(-86,39.9));spur={'one_way_metres':round(length,2),'from':'Shardwood return','to':'seam station terrace'}
    except ValueError as error:errors.append(str(error))
    view=grotto_view(args.export)
    if not view['clear']:errors.append('Threshold sightline obstructed: '+str(view['obstructing_exported_roles']))
    report={'threshold_view':view,'station_access':spur,'scope' :'Canonical source geometry on a 25 cm grid. 45 cm body radius and four height slices, sampled top surfaces, maximum 20 cm grid step. Protected unknown parcels are obstacles. Existing door apron gaps use bounded flat ground. Native walking, camera and controller NOT RUN.','complete':not errors,'errors':errors,'legs':legs,'stops':stops,'distance_m':round(sum(x['metres'] for x in legs),2)}
    (args.output/'walk.json').write_text(json.dumps(report,indent=2)+'\n');np.save(args.output/'walk_mask.npy',walk)
    with (args.output/'walk-points.csv').open('w') as f:
        f.write('x,z\n');f.writelines(f'{x:.2f},{z:.2f}\n' for x,z in map(world,all_points))
    route_map(args.export,args.output,report,all_points)
    print(json.dumps({k:v for k,v in report.items() if k not in ('stops','legs')}));return 1 if errors else 0
if __name__=='__main__':raise SystemExit(main())
