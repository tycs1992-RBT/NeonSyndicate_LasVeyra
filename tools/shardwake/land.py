"""Original Vale landforms and hard-surface models. Units are metres, with +Z looking up the valley."""
import functools
import bisect
import math
import random
import numpy as np
from assets import Mesh

def valley_z(x,t):
    meander=min(1,max(0,(t-11)/13))*min(1,max(0,(68-t)/12))*2.5*math.sin(x/22)
    bite=min(1,max(0,(t-4)/20))*min(1,max(0,(43-t)/16))
    return t+meander-11*math.exp(-((x-.4)/8.8)**2)*bite

def height(x,z):
    lo,hi=z-6,z+16
    for _ in range(20):
        mid=(lo+hi)/2
        if valley_z(x,mid)<z:lo=mid
        else:hi=mid
    t=(hi+lo)/2
    grades=[(-53,1.0),(-20,.05),(11,.06),(19,.42),(24,1.05),(28,-1.2),(32,-6.5),(36,-12.0),(42,-12.7),
            (47,-9.5),(51,-4.5),(55,1.9),(62,2.1),(77,3.15),(92,5.2),(107,6.55),(125,9.3),(164,15)]
    h=float(np.interp(t,[g[0] for g in grades],[g[1] for g in grades]))
    edge=min(1,max(0,(abs(x)-17)/28))
    if t<24:
        # The hearth sits on a shelf overlooking the span. The route descends gently to its near foot.
        lip=24-15*max(0,min(1,(x+12)/9))
        h+=4.55*max(0,min(1,(lip-t)/16))
    h+=edge*(3.8+2.4*math.sin(z*.086+x*.045)+1.8*math.cos(x*.12-z*.061))
    flank=min(1,max(0,(abs(x)-50)/70))
    h+=flank*(5.5+3.0*math.sin(.055*z+.029*x)+2.5*math.cos(.041*z-.077*x))
    if 65<z<124:h+=.35*math.sin((z-65)*math.pi/59)*math.sin(x/8)
    return h

XS=[-172,-124,-92,-70,-64,-54,-46,-38,-31,-25,-19,-13,-8,-4,0,4,8,13,19,25,31,38,46,54,64,70,92,124,172]
ROWS={'NearBank':[-49,-33,-22,-14,-7,0,6,12,18,21,24],
      'Ravine':[24,27,30,33,36,39,43,47,50,53,55,56],
      'FarBank':[56,60,66,71,76,82,89,95,101,108,115,124,138,160]}
STATIONS=sorted({z for row in ROWS.values() for z in row})

@functools.lru_cache(None)
def ground_vertex(x,t):
    z=valley_z(x,t)
    return (x,height(x,z),z)

def surface(x,z):
    """Barycentric height on the same two triangles used by the terrain MeshCollider."""
    ix=max(0,min(len(XS)-2,bisect.bisect_right(XS,x)-1))
    a,b=XS[ix:ix+2];u=(x-a)/(b-a)
    section=[(1-u)*valley_z(a,t)+u*valley_z(b,t) for t in STATIONS]
    iz=max(0,min(len(STATIONS)-2,bisect.bisect_right(section,z)-1))
    t,tt=STATIONS[iz:iz+2]
    p,q,r,s=(ground_vertex(a,t),ground_vertex(a,tt),ground_vertex(b,tt),ground_vertex(b,t))
    diagonal=(1-u)*p[2]+u*r[2]
    if z>=diagonal:
        v=(z-diagonal)/(q[2]-p[2])
        return (1-u-v)*p[1]+v*q[1]+u*r[1]
    v=(diagonal-z)/(r[2]-s[2])
    return (1-u)*p[1]+(u-v)*r[1]+v*s[1]

def terrain(name,zs):
    m=Mesh();xs=XS
    for j in range(len(zs)-1):
        for i in range(len(xs)-1):
            def point(k,l):
                x,z=xs[k],zs[l]
                return ground_vertex(x,z)
            p,q,r,s=point(i,j),point(i,j+1),point(i+1,j+1),point(i+1,j)
            xm=(p[0]+r[0])/2;zm=(p[2]+r[2])/2
            if name=='Ravine': tones=('746a65','817269','957e6d','696771') if min(p[1],q[1])<-.9 else ('969467','9ba26b','b2aa78')
            elif name=='FarBank':tones=('718e7b','83a18b','8ba891','849e87')
            else:tones=('9ca566','a7ad71','abae74','99a56c')
            band=(math.sin(xm*.16+zm*.06)+math.cos(zm*.19-xm*.04)+2)*.73
            k=min(len(tones)-1,int(band))
            m.quad(p,q,r,s,tones[k],(0,1,0))
    return m

def stone_ribbon(m,points,width):
    points=np.asarray(points,float);rng=random.Random(38)
    section=[]
    for i,(x,z) in enumerate(points):
        v=points[min(i+1,len(points)-1)]-points[max(0,i-1)]
        side=np.array([v[1],-v[0]])/np.linalg.norm(v);section.append([(x+side[0]*e*width/2,z+side[1]*e*width/2) for e in (-1,-.79,.79,1)])
    for i in range(len(points)-1):
        for j in range(3):
            raw=[section[i][j],section[i+1][j],section[i+1][j+1],section[i][j+1]]
            v=[(x,surface(x,z)+.039,z) for x,z in raw]
            m.quad(*v,rng.choice(('b9a27a','bca47d','c1a985')) if j==1 else 'ad9d75',(0,1,0))

def path_mesh():
    m=Mesh()
    for pts,w in [([(-7,-16),(-7,-9),(-8.5,-4),(-10,1),(-9,5),(-5,8),(0,10),(5,14),(10,19),(13.7,23.6)],3.7),
                  ([(-3.7,56.4),(-3.9,59),(-4,63),(-10,67),(-14,71),(-15,75),(-12,79),(-5,82),(2,86),(7,91),(11,96),(12,102)],3.9)]:
        detail=[]
        for a,b in zip(pts,pts[1:]):
            count=math.ceil(math.dist(a,b)/.8)
            detail.extend([tuple(np.array(a)+(np.array(b)-a)*(i/count)) for i in range(count)])
        stone_ribbon(m,detail+[pts[-1]],w)
    return m

def workshop():
    shell,roof,gear=Mesh(),Mesh(),Mesh()
    # Broad founded apron, open front and east work bay, with the habitable back under a separate pitched roof.
    shell.rings([(0,-.25,0,6.65,6.6),(0,.24,0,6.42,6.4)],['stone','sand','stone','edge','stone','sand','stone','edge'],math.pi/8)
    shell.box((-.75,2.24,4.1),(8.55,4.2,.67),'stone')
    shell.box((-4.68,2.0,.15),(.64,3.9,8.0),'stone')
    for x,z in [(-4.4,-4.0),(-.5,-4.0),(3.55,-4.0),(3.55,.95),(3.55,4.0)]:
        shell.rings([(x,.18,z,.69,.72),(x,.6,z,.61,.64),(x,3.6,z,.38,.45),(x,4.43,z,.7,.68)],
                   ['stone','sand','edge','stone','sand','stone'],.2)
        shell.rings([(x,.53,z,.65,.68),(x,.68,z,.65,.68)],['copper']*6,.2)
    def eave(x,z):
        # Two asymmetrical slopes and a hooked southeast drip; the long hall ridge is off centre.
        y=(6.16+(x+1.7)*.46 if x< -1.7 else 6.16-(x+1.7)*.245)
        return y+.29*math.cos(z*.47)+.085*(z+.8)
    xs=[-5.8,-4.2,-2.6,-1.7,-.35,1.3,3,4.7,5.6];zs=[-5.65,-4.6,-2.4,0,2.5,4.4,5.65]
    for i in range(len(xs)-1):
        for j in range(len(zs)-1):
            raw=[(xs[i],zs[j]),(xs[i+1],zs[j]),(xs[i+1],zs[j+1]),(xs[i],zs[j+1])]
            pts=[np.array((x,eave(x,z),z)) for x,z in raw];down=np.array([0,.30,0])
            roof.quad(*pts,('roof','426779','517080')[(i+j//2)%3],(0,1,0));roof.quad(*(p-down for p in pts),'wood',(0,-1,0))
            for a,b,edge in [(0,1,j==0),(1,2,i==len(xs)-2),(2,3,j==len(zs)-2),(3,0,i==0)]:
                if edge:
                    p,q=pts[a],pts[b];roof.quad(p,q,q-down,p-down,'brass',((p[0]+q[0])/2,0,(p[2]+q[2])/2))
    for z in [-4.6,0,4.4]:
        roof.beam((-5,eave(-5,z)-.36,z),(-1.7,eave(-1.7,z)-.36,z),.34,.31,'wood')
        roof.beam((-1.7,eave(-1.7,z)-.36,z),(4.9,eave(4.9,z)-.36,z),.34,.31,'wood')
    # Hearth and flared chimney. A backed amber mouth, thick sidewalls and cowl make a real recess.
    shell.box((.0,1.5,2.64),(2.5,2.75,.45),'basalt')
    for x in [-1.17,1.17]:shell.box((x,1.6,1.94),(.54,2.8,1.65),'shade')
    shell.box((0,2.99,1.94),(2.89,.48,1.80),'stone')
    shell.box((0,.40,1.49),(2.8,.42,1.7),'basalt')
    roof.rings([(0,3.1,2,1.45,1.2),(.1,4.7,2,1.16,1.01),(.25,7.0,2,.82,.76),(.25,7.35,2,1.13,1.01),
                (.25,8.35,2,.93,.88)],['shade','copper','shade','wood','copper','shade','copper','wood'],.16)
    roof.rings([(.25,8.29,2,1.0,.94),(.25,8.56,2,1.09,1.02)],['brass']*8,.16)
    for x in (-3.3,-1.9):
        gear.box((x,.82,-2.1),(.19,1.35,.9),'wood')
    gear.box((-2.60,1.53,-2.1),(2.07,.18,1.16),'endgrain')
    for i in range(3):
        gear.box((-3.1+i*.4,1.71,-2.12),(.14,.2,.51),'copper')
    gear.rings([(2.12,.25,.22,.63,.75),(2.12,.97,.22,.52,.63),(2.12,1.19,.22,.82,.63)],['basalt']*6)
    gear.box((2.14,1.32,.2),(1.45,.20,.7),'brass')
    for x,z in [(-3.7,-3.35),(3.55,-3.75)]:
        gear.beam((x,4.32,z),(x,3.04,z),.055,.055,'brass')
        gear.rings([(x,2.63,z,.12,.12),(x,2.77,z,.23,.23),(x,3.03,z,.16,.16)],['warm']*6)
    # Continuous timber fascia, recessed bands of amber panes and a copper service wheel under the eave.
    for x in [-3.45,-2.75,-2.05]:
        gear.box((x,2.55,3.70),(.62,1.22,.12),'warm')
        gear.box((x,1.85,3.55),(.76,.16,.40),'endgrain')
    for x in [-3.8,-1.72]:gear.box((x,2.55,3.52),(.11,1.62,.39),'wood')
    for z in [-3.72,.95]:
        gear.beam((3.55,3.28,z),(2.2,4.65,z),.28,.32,'wood')
        gear.beam((3.55,3.39,z),(4.85,4.11,z),.23,.30,'wood')
    for i in range(3):
        x=-4.4+i*.88;z=-4.62+.24*(i%2)
        gear.rings([(x,.23,z,.34,.37),(x,.78,z,.40,.42),(x,1.03,z,.29,.31)],['wood','endgrain','wood','wood','endgrain','wood'])
        gear.rings([(x,.72,z,.42,.44),(x,.8,z,.42,.44)],['copper']*6)
    # Copper hood over the public anvil, and a slatted exhaust band above it.
    gear.beam((3.78,3.12,-.72),(5.03,3.54,-.72),.17,.18,'wood')
    gear.beam((3.78,3.12,1.32),(5.03,3.54,1.32),.17,.18,'wood')
    for i in range(5):
        x=3.45+i*.35
        gear.box((x,3.68-(x-3.45)*.11,.30),(.34,.15,2.65),'copper' if i%2 else 'brass')
    for z in [2.6,3.4]:
        gear.beam((3.37,.26,z),(3.37,2.32,z),.29,.28,'basalt')
        gear.beam((3.37,2.2,z),(4.60,2.2,z),.23,.22,'copper')
    return shell,roof,gear

def loading_ramp():
    # The hearth's level apron is higher than the meadow on its eastern side.
    # The central loading strip has no steps. Side stones feather into the same physical bank.
    m=Mesh();cx,cz=-16.5,4.;base=surface(cx,cz)
    xs=[4.95,5.75,6.5,7.25,8.0,8.75,9.35];zs=[-5.,-4.05,-3.25,-2.05,-.85,.05,1.]
    def point(x,z):
        blend=max(0,min(1,(9.35-x)/(9.35-5.75)))
        skirt=max(0,min(1,(z+5)/.95,(1-z)/.95))
        ground=surface(cx+x,cz+z)-base
        # Pull edge vertices just above the bank, and overlap the hall top by a centimetre.
        return (x,ground+.024+max(0,.23-ground)*blend*skirt,z)
    for i,(a,b) in enumerate(zip(xs,xs[1:])):
        for j,(c,d) in enumerate(zip(zs,zs[1:])):
            m.quad(point(a,c),point(a,d),point(b,d),point(b,c),
                   ['baa781','c5ac86','cbb68d'][(i+j)%3] if 0<j<5 else 'a79877',(0,1,0))
    for z,sign in [(zs[0],-1),(zs[-1],1)]:
        for a,b in zip(xs,xs[1:]):
            p=np.array(point(a,z));q=np.array(point(b,z));down=np.array([0,.11,0])
            m.quad(p,q,q-down,p-down,'stone',(0,0,sign))
    return m

def deck_y(z):return 1.03+(z+16)/32*.89+.63*math.sin(math.pi*(z+16)/32)

def bridge():
    m=Mesh();n=32
    for i in range(n):
        a,b=-16+i,-15+i;y,yy=deck_y(a),deck_y(b)
        for x,x1 in [(-3.25,-2.62),(-2.62,0),(0,2.62),(2.62,3.25)]:
            m.quad((x,y,a),(x,yy,b),(x1,yy,b),(x1,y,a),('edge' if abs(x)==3.25 or x==2.62 else ['d0bda0','c7b89d','cdbda5'][i%3]),(0,1,0))
        m.quad((-3.25,y-.55,a),(3.25,y-.55,a),(3.25,yy-.55,b),(-3.25,yy-.55,b),'shade',(0,-1,0))
        for side in (-1,1):
            x=side*3.25
            m.quad((x,y,a),(x,yy,b),(x,yy-.55,b),(x,y-.55,a),'sand',(side,0,0))
            # Continuous stone curb. Low rails let the player see the gorge on both sides.
            m.beam((side*3.04,y+.28,a),(side*3.04,yy+.28,b),.43,.55,'stone')
            m.beam((side*3.04,y+.60,a),(side*3.04,yy+.60,b),.52,.13,'edge')
    for a,b in [(-16,-6),(-6,6),(6,16)]:
        for side in (-1,1):
            x=side*2.59
            for i in range(12):
                t=i/12;u=(i+1)/12;z=a+t*(b-a);zz=a+u*(b-a)
                def underside(p):return -5.65+5.8*(math.sin(math.pi*p)**.67)
                v=underside(t);w=underside(u)
                for dx,face in [(-.46,-1),(.46,1)]:
                    m.quad((x+dx,v,z),(x+dx,w,zz),(x+dx,deck_y(zz)-.44,zz),(x+dx,deck_y(z)-.44,z),
                           ['stone','sand','stone','edge'][i%4],(face,0,0))
                m.quad((x-.46,v,z),(x+.46,v,z),(x+.46,w,zz),(x-.46,w,zz),'shade',(0,-1,0))
        for z in (a,):
            for side in (-1,1):
                x=side*2.58
                m.rings([(x,-13,z,1.13,1.5),(x,-5,z,.84,1.06),(x,deck_y(z)-.36,z,.76,.81)],['stone','shade','sand','stone','shade','stone'],.3)
    for z in [-15.55,-6,6,15.55]:
        for side in (-1,1):
            x=side*3.03;y=deck_y(z)
            m.rings([(x,y,z,.48,.50),(x,y+.82,z,.39,.43),(x,y+1.08,z,.44,.46),(x,y+1.23,z,.32,.33)],['stone','sand','edge','stone','sand','edge'],.5)
    return m

def spire():
    m=Mesh()
    for side,s,x in [(-1,1.0,-2.1),(1,.73,3.0)]:
        m.rings([(x,-.55,0,3.6,3.4),(x+.25,2,0,3.2,3.05),(x+side*.95,5.3*s,.25,2.06,2.34),
                 (x+side*1.36,10*s,.65,1.21,1.64),(x+side*1.75,14*s,.5,.11,.18)],
                ['ore','basalt','745d77','8b7289','66526b','a489a3','795d7b'],.31)
    # Pitched mineral emerging from the cleft; one light plane surrounded by darker fractured faces.
    m.rings([(.1,1.3,.25,1.25,1.15),(.4,5.7,.25,1.48,1.28),(1.35,11.3,.54,.84,.81),(2.25,15.25,.55,.08,.09)],
            ['quartz','9475b0','glint','9375a9','b59dce','ad8bc2'],.46)
    for x,z,h in [(-4.1,2.5,4.2),(3.8,2.0,3.0),(-.4,-2.1,4.8)]:
        m.rings([(x,-.4,z,1.1,1.0),(x,.9,z,.82,.94),(x+.61,h,z+.12,.06,.09)],['ore','quartz','lilac','basalt','quartz'])
    # Broad stone seat and broken compression collar hold the central mineral under load.
    m.rings([(.38,-.07,.45,2.24,1.95),(.38,.58,.45,2.10,1.85),(.38,.93,.45,1.81,1.53)],
            ['basalt','ore','shade','basalt','745d77','ore','shade','basalt'],.23)
    for side in [-1,1]:
        m.beam((side*1.41,.69,-.64),(side*1.00+.23,3.35,-.71),.29,.32,'basalt')
        m.beam((side*1.02+.23,3.33,-.73),(side*.62+.24,3.58,-.80),.29,.24,'copper')
    # A few bedding planes fracture across the outer faces, away from the regulator approaches.
    for i,(x,z,h) in enumerate([(-3.85,-.65,3.5),(-2.9,2.05,2.7),(3.3,-1.15,2.35)]):
        m.rings([(x,-.25,z,1.32,.83),(x+.14,h*.45,z,.96,.71),
                 (x-.48,h*.79,z+.21,.80,.47),(x-.71,h,z+.38,.21,.27)],
                ['ore','795d77','8e7289','basalt','846e85'],.63*i)
    return m

def pine(variant):
    m=Mesh();rng=random.Random(918+variant);h=[7.1,9.5,6.2][variant];lean=rng.uniform(-.4,.4)
    m.rings([(0,-.22,0,.32,.30),(0,1.3,0,.22,.22),(lean*.6,h*.70,.12,.10,.12),(lean,h,0,.035,.035)],['wood','shade','wood','endgrain','shade','wood'])
    # Asymmetric tiered crowns, with open sheltered space below the lower branches.
    for i in range(5):
        y=h*(.28+i*.135);r=h*(.29-i*.046)
        tip=(lean*(i/5),y,0)
        m.rings([(tip[0],y-.18,0,r*.81,r*.77),(tip[0]+.10,y+.18,.07,r,r*.91),
                 (tip[0]-.05,y+h*.28,0,.03,.04)],
                ['needles','pine','487c66','needles','59917a','pine','376955'],i*.49+variant*.8)
    # Broken fans project beyond the regular central tiers. The understorey stays open.
    for i in range(6):
        a=variant*.91+i*2.39;y=h*(.30+i*.075);r=h*(.24-i*.017)
        x,z=math.cos(a)*r,math.sin(a)*r
        m.beam((lean*.3,y+.24,0),(x,y-.16,z),.10,.11,'wood')
        m.rings([(x*.82,y-.19,z*.82,r*.43,r*.38),(x*.83,y+.12,z*.85,r*.55,r*.48),
                 (x*.76,y+r*.80,z*.79,.035,.043)],
                ['needles','487865','4e8672','needles','59917a'],a)
    return m

def rocks(variant):
    m=Mesh();rng=random.Random(1301+variant)
    for i in range(3):
        x=(i-1)*1.4;z=rng.uniform(-.45,.45);h=rng.uniform(.8,1.6)
        m.rings([(x,-.3,z,1.3,1.08),(x-.09,.47*h,z,1.24,.93),(x+.19,h,z+.10,.65,.60)],
                ['basalt','78847e','7a8279','8b9182','758782','basalt'],i+variant)
    return m

def ridge(band):
    m=Mesh();rng=random.Random(430+band);z=138+band*34
    palette=[['91a5b2','869ead','9aafba','94aab4'],['a5b8c2','acbdc6','bbc9cc','b2c1c9']][band]
    rows=[]
    for i in range(31):
        x=-154+i*10; silhouette=(14+11*math.sin(i*0.67+.5*band)+4*math.cos(i*1.92)+band*7)
        # The cleft above the Split Crown gives the mineral tip a patch of sky.
        silhouette-=8*math.exp(-((x-13)/13)**2)
        height=max(8,silhouette)
        rows.append([(x,-4,z-10),(x+rng.uniform(-1,1),height*.48,z-5),(x+rng.uniform(-2,2),height*.76,z-.5),
                    (x+rng.uniform(-3,3),height,z+4)])
    for a,b in zip(rows,rows[1:]):
        for k in range(3):
            m.quad(a[k],b[k],b[k+1],a[k+1],palette[rng.randrange(4)],(0,0,-1))
    return m

def planting():
    m=Mesh();rng=random.Random(215)
    for j in range(16):
        x,z=rng.uniform(-1,1),rng.uniform(-.7,.7);a=rng.random()*math.tau
        d=np.array((math.cos(a),0,math.sin(a)));n=np.array((-d[2],0,d[0]))
        p=np.array((x,0,z));mid=p+d*.22+np.array((0,.42,0));tip=p+d*.56+np.array((0,rng.uniform(.45,.83),0))
        for face in [(0,1,0),(0,-1,0)]:m.quad(p,mid+n*.105,tip,mid-n*.105,'fern' if j%3 else 'moss',face)
        if j%5==0:
            m.rings([(x,.0,z,.031,.031),(x+.04,.7,z,.036,.036)],['moss']*4)
            m.rings([(x+.04,.65,z,.095,.095),(x+.04,.76,z,.13,.13),(x+.07,.81,z,.045,.045)],['warm','edge','sand','brass','edge'])
    return m

def flywheel():
    m=Mesh()
    for i in range(18):
        a=i*math.tau/18;b=(i+1)*math.tau/18
        p=np.array((0,math.cos(a),math.sin(a)));q=np.array((0,math.cos(b),math.sin(b)))
        m.beam(p*1.08,q*1.08,.14,.12,'copper' if i%3 else 'brass')
        if i%3==0:m.beam(p*.13,p*1.06,.10,.10,'brass')
    m.beam((-.41,0,0),(.21,0,0),.28,.28,'basalt')
    return m

def terminal():
    m=Mesh()
    m.rings([(0,-.23,0,1.04,.93),(0,.21,0,.88,.8),(0,.68,0,.60,.58),(0,.79,0,.68,.64)],['stone','edge','shade','stone','brass','stone'])
    for s in [-1,1]:
        m.beam((s*.48,.7,.02),(s*.55,1.57,.12),.13,.14,'copper')
    return m

def gem():
    m=Mesh();m.rings([(0,.72,0,.10,.10),(0,1.23,0,.43,.38),(0,1.77,0,.33,.29),(.07,2.21,.04,.028,.03)],['warm','brass','edge','warm','sand','warm'],.36);return m

def water():
    m=Mesh();rng=random.Random(120);m.quad((-68,-8.4,29),(-68,-8.4,53),(68,-8.4,53),(68,-8.4,29),'water',(0,1,0))
    for i in range(48):
        x=rng.uniform(-61,61);z=rng.uniform(36,46);l=rng.uniform(.8,4.4)
        m.tri((x,-8.35,z),(x+l,-8.35,z+.10),(x+.25*l,-8.35,z+.19),'foam',(0,1,0))
    return m
