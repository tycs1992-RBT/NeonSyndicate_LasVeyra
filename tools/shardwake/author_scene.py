"""Bake the original pilot once. Hand-edited scene transforms are preserved unless --replace-scene is explicit."""
import argparse
import math
import random
from assets import ART, CODE, PAINT, ROOT, SCENE, SHADER, Scene, guid, meta
FOLIAGE = ART/'Materials/ValeFoliage.mat'
# River piers of Three Arch Bridge in world space (bridge at (5, 0, 40), yaw -28, z scale 1.16): x, z, size.
PIERS = [(1.73, 46.15, 1.5), (8.27, 33.85, 1.5)]
GROUND_PARTS = 3  # ground cover split so each mesh stays under the 16-bit vertex limit
import land
import vale_art as art
import vale_foliage as leafy
import vale_ravine as ravine
import vale_workshop as shopart

# The sun: from the west and a little behind the opening camera, low enough for long shadows and warm slopes.
SUN_YAW, SUN_PITCH = 74, 33

def assets():
    for folder in [ART,ART/'Meshes',ART/'Materials',CODE]:meta(folder)
    meta(SHADER,'shader');meta(ART/'HearthCommission.json','json')
    for p in CODE.glob('*.cs'):meta(p,'script')
    grain=art.grain_texture()
    PAINT.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: ValePalette
  m_Shader: {fileID: 4800000, guid: '''+guid(SHADER)+''', type: 3}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_CustomRenderQueue: -1
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _Grain:
        m_Texture: {fileID: 2800000, guid: '''+guid(grain)+''', type: 3}
        m_Scale: {x: 1, y: 1}
        m_Offset: {x: 0, y: 0}
    m_Ints: []
    m_Floats:
    - _Lift: 1
    m_Colors: []
''');meta(PAINT,'material')
    meshes={name:land.terrain(name,rows) for name,rows in land.ROWS.items()}
    for name in land.ROWS:meshes[name].recode(lambda c,code:art.SOIL)
    meshes.update({'Trail':land.path_mesh(),'Bridge':art.bridge2(),'SplitCrown':art.crown3(),'CrownBase':art.crown_base(),'Water':land.water(),
            'Lens':land.gem(),'Flywheel':land.flywheel(),'LoadingRamp':land.loading_ramp(),
            'PathEdge':art.path_edge(),'Sky':art.sky_dome(sun_direction())})
    meshes['Trail'].recode(lambda c,code:art.SOIL)
    meshes['Lens'].recode(lambda c,code:art.LAMP if c==(255,187,96) else code)
    for name,part in zip(['Workshop','SweptRoof','Workbenches'],land.workshop()):meshes[name]=part
    # Warm panes and lamps glow a little, so Dante's workshop reads warm from the spawn.
    meshes['Workbenches'].recode(lambda c,code:art.LAMP if c==(255,187,96) else art.WOOD if c in WOODS else code)
    meshes['SweptRoof'].recode(lambda c,code:art.WOOD if c in WOODS else code)
    for i in range(3):
        meshes['Regulator'+str(i)]=art.collar(i)
    for i in range(2):meshes['Talus'+str(i)]=land.rocks(i)
    for i in range(4):meshes['Ridge'+str(i)]=art.ridge2(i)
    # Foliage: painted atlas, broadleaf and conifer variants (bark wood plus leaf cards), ground cover.
    atlas=leafy.atlas();foliage_shader=ART/'Materials/ValeFoliage.shader';meta(foliage_shader,'shader')
    FOLIAGE.write_text(PAINT.read_text().replace('ValePalette','ValeFoliage').replace(guid(SHADER),guid(foliage_shader))
        .replace("    - _Grain:\n        m_Texture: {fileID: 2800000, guid: "+guid(art.GRAIN),"    - _MainTex:\n        m_Texture: {fileID: 2800000, guid: "+guid(atlas))
        .replace('  m_CustomRenderQueue: -1','  m_CustomRenderQueue: 2450').replace('    m_Colors: []','    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}'))
    meta(FOLIAGE,'material')
    leaves={}
    for i in range(len(leafy.BROADLEAF)):
        meshes['BroadleafWood'+str(i)],leaves['BroadleafLeaves'+str(i)]=leafy.broadleaf(i)
    for i in range(len(leafy.CONIFERS)):
        meshes['ConiferWood'+str(i)],leaves['ConiferNeedles'+str(i)]=leafy.conifer(i)
    # The ravine: stepped strata, shoreline boulders and a moving river surface (decoration over the collider).
    meshes['RavineStrata'],pockets=ravine.strata()
    # Dante's workshop up close: fitted stone, the roof's timber frame, flagstones, banked embers; stones by the path.
    meshes['ShopColumns']=shopart.columns();meshes['ShopTimber']=shopart.timber()
    meshes['ShopFlagstones']=shopart.flagstones();meshes['KilnEmbers']=shopart.embers()
    meshes['PathPebbles']=shopart.pebbles([(x+side*rng_off,z,.55,5) for (x,z) in [(-7,-15),(-7,-11.5),(-7.6,-7),(-8.3,-3.5),(-8,1.5)]
                                           for side,rng_off in [(-1,2.6),(1,2.9)]])
    meshes['ShoreRocks']=ravine.shore_rocks()
    meshes['Water']=ravine.river(PIERS)
    spots=leafy.meadow_spots(art.EDGE_TUFTS,art.NEAR_TRAIL,art.FAR_TRAIL)+pockets
    for k in range(GROUND_PARTS):leaves['GroundCover'+str(k)]=leafy.ground_cover(spots[k::GROUND_PARTS],6203+k)
    signal=ART/'Materials/ValeSignal.shader';meta(signal,'shader')
    mat=ART/'Materials/ValeSignal.mat'
    mat.write_text(PAINT.read_text().replace('ValePalette','ValeSignal').replace(guid(SHADER),guid(signal)).replace(PAINT.read_text().split('    m_TexEnvs:')[1].split('    m_Ints:')[0],' []\n').replace('    m_Colors: []','    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}'))
    meta(mat,'material')
    saved={name:mesh.save(name) for name,mesh in meshes.items()}
    saved.update({name:cards.save(name) for name,cards in leaves.items()})
    return saved

WOODS={tuple(int(h[i:i+2],16) for i in (0,2,4)) for h in ('895b43','be8963','876856')}

def sun_direction():
    """Unit vector toward the sun, from the light's yaw and pitch (Unity forward is +Z)."""
    y,p=math.radians(SUN_YAW),math.radians(SUN_PITCH)
    forward=(math.sin(y)*math.cos(p),-math.sin(p),math.cos(y)*math.cos(p))
    return tuple(-v for v in forward)

def build():
    meshes=assets();s=Scene()
    def piece(name,mesh,pos=(0,0,0),parent=None,solid=True,yaw=0,scale=(1,1,1),cast=True,material=None):
        n=s.obj(name,pos,yaw,scale,parent);s.geo(n,meshes[mesh],solid,cast,material);return n
    def tree(name,wood,crown,pos,parent,yaw,scale):
        n=s.obj(name,pos,yaw,scale,parent)
        piece('Bark and roots',wood,parent=n,solid=False);piece('Leaves',crown,parent=n,solid=False,material=FOLIAGE)
        return n
    pilot=s.obj('Shardwake Vale - pilot')
    ground=s.obj('01 - Terrain and river')
    piece('Ochre hearth meadow','NearBank',parent=ground)
    piece('Broken ravine walls and bed','Ravine',parent=ground)
    piece('Sage and lilac far bank','FarBank',parent=ground)
    piece('River current','Water',parent=ground,solid=False,cast=False)
    piece('Ravine strata and shelves','RavineStrata',parent=ground,solid=False)
    piece('Shoreline boulders','ShoreRocks',parent=ground,solid=False)
    piece('Footpath - both banks','Trail',parent=ground,solid=False)
    piece('Path edge - roots, cover and rock','PathEdge',parent=ground,solid=False)
    piece('Stones beside the first steps','PathPebbles',parent=ground,solid=False)
    for k in range(GROUND_PARTS):piece('Grass, ferns and flowers '+str(k+1),'GroundCover'+str(k),parent=ground,solid=False,material=FOLIAGE)
    shop=s.obj('02 - Dante workshop',(-16.5,land.surface(-16.5,4),4))
    shell=piece('Open workshop hall','Workshop',parent=shop)
    piece('Graded workshop apron','LoadingRamp',parent=shop)
    piece('Copper throat and swept roof','SweptRoof',parent=shop)
    piece('Anvil benches and lamps','Workbenches',parent=shop)
    piece('Fitted stone columns','ShopColumns',parent=shop,solid=False)
    piece('Roof frame - plates, braces, rafters','ShopTimber',parent=shop,solid=False)
    piece('Flagstone floor','ShopFlagstones',parent=shop,solid=False,cast=False)
    piece('Banked kiln embers','KilnEmbers',parent=shop,solid=False,cast=False)
    wheel=piece('Bellows drive wheel','Flywheel',(4.15,2.16,2.97),shop,False)
    dante=s.obj('Dante - conversation and actor',(4.75,.26,-1.95),143,parent=shop)
    # An empty clamp is visible on arrival. The commissioned heart and heated fire return at completion.
    heart=piece('Installed hearth lens','Lens',(0,.38,1.65),shop,False,scale=(.7,.9,.7))
    fire=s.obj('Restored furnace light',(0,2.05,.95),parent=shop)
    s.comp(fire,108,'Light',light(2,(1,.49,.18),3.4,6))
    heart['active']=False;fire['active']=False
    bridge=s.obj('03 - Three arch pack bridge',(5,0,40),-28,(1,1,1.16))
    piece('Stone span deck and parapets','Bridge',parent=bridge)
    refuge=s.obj('Far landing - safe checkpoint',(0,2.18,18.2),parent=bridge)
    crown=s.obj('04 - Split Crown landmark',(12,land.surface(12,104),104))
    piece('Split mineral spine','SplitCrown',parent=crown)
    piece('Mineral apron - shards and rock','CrownBase',parent=crown,solid=False)
    lenses=[]
    for i,(x,z) in enumerate([(-4.65,-6.15),(6.65,-2.25),(-4.9,5.5)]):
        y=land.surface(12+x,104+z)-crown['pos'][1]
        stand=piece('Regulator '+str(i+1),'Regulator'+str(i),(x,y,z),crown)
        lenses.append(piece('Live mineral '+str(i+1),'Lens',parent=stand,solid=False))
    pick=piece('Removable hearth seed','Lens',(1.2,land.surface(13.2,98.35)-crown['pos'][1],-5.65),crown,False,scale=(.63,.60,.63))
    pick['active']=False
    greens=s.obj('05 - Pines and sheltered planting');rng=random.Random(729)
    groups=[(-29,1,4),(-24,17,6),(19,13,3),(29,24,5),(-23,61,6),(23,67,5),(-31,83,6),
            (-2,77,3),(34,89,5),(-18,110,5),(40,113,6),(5,128,7)]
    for i,(cx,cz,n) in enumerate(groups):
        for j in range(n):
            x=cx+rng.uniform(-4,4);z=cz+rng.uniform(-4,4);k=rng.uniform(.68,1.28)
            yaw=rng.randrange(360);tall=k*rng.uniform(.86,1.19)
            y=land.surface(x,z)-.05
            if i<4:
                # The four groups nearest the spawn: broadleaf trees, three related variants.
                v=(i*2+j)%len(leafy.BROADLEAF)
                tree('Broadleaf %02d.%02d'%(i+1,j+1),'BroadleafWood'+str(v),'BroadleafLeaves'+str(v),(x,y,z),greens,yaw,(k,tall,k))
            else:
                v=(i+j)%len(leafy.CONIFERS)
                tree('Conifer %02d.%02d'%(i+1,j+1),'ConiferWood'+str(v),'ConiferNeedles'+str(v),(x,y,z),greens,yaw,(k,tall,k))
    crags=s.obj('06 - Broken stone shelves')
    for i,(x,z,k) in enumerate([(-23,9,1.3),(18,15,1.2),(23,26,1.65),(-9,26,1.45),(17,53,1.9),
                                (-13,57,1.4),(-24,67,1.3),(4,72,1.55),(-22,84,1.5),(22,91,1.6),(31,107,2)]):
        piece('Layered shelf %02d'%i,'Talus'+str(i%2),(x,land.surface(x,z)-.1,z),crags,True,i*33,(k,k*1.13,k))
    horizon=s.obj('07 - Painted horizon')
    for i in range(4):piece('Distant range '+str(i+1),'Ridge'+str(i),parent=horizon,solid=False,cast=False)
    piece('Painted sky','Sky',parent=horizon,solid=False,cast=False)
    spawn=s.obj('Malik - start and facing',(-7,land.surface(-7,-12)+.1,-12),9)
    # The runtime camera uses this authored azimuth and the same shoulder, pitch and target.
    camera=s.obj('Opening camera',(-7.49,land.surface(-7,-12)+3.11,-18.49),9,pitch=10.6)
    s.comp(camera,20,'Camera','''  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {r: 0.64, g: 0.77, b: 0.82, a: 1}
  m_projectionMatrixMode: 1
  m_NormalizedViewPortRect: {x: 0, y: 0, width: 1, height: 1}
  near clip plane: 0.09
  far clip plane: 650
  field of view: 58
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask: {serializedVersion: 2, m_Bits: 4294967295}
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_OcclusionCulling: 1''');s.comp(camera,81,'AudioListener','  m_Enabled: 1')
    sun=s.obj('Warm ridge sun',(0,22,5),SUN_YAW,pitch=SUN_PITCH);s.comp(sun,108,'Light',light(1,(1,.86,.68),1.18,120,shadows=2))
    def ref(n,offset=1):return '{fileID: '+str(n['id']+offset)+'}'
    fields=''.join('  '+key+': '+ref(node)+'\n' for key,node in [('spawn',spawn),('dante',dante),('bridge',bridge),
             ('refuge',refuge),('landmark',crown),('pickup',pick)])+'  sockets:\n'+''.join('  - '+ref(n)+'\n' for n in lenses)
    fields+='  forgeHeart: '+ref(heart,0)+'\n  forgeFire: '+ref(fire,0)+'\n  view: '+ref(camera,2)+'\n  bellowsWheel: '+ref(wheel)+'\n  terrain: '+ref(ground)+'\n  furnaceStone: '+ref(shell,3)+'\n  signalMaterial: {fileID: 2100000, guid: '+guid(ART/'Materials/ValeSignal.mat')+', type: 2}\n  commission: {fileID: 4900000, guid: '+guid(ART/'HearthCommission.json')+', type: 3}'
    s.script(pilot,'ShardwakeVale',fields);s.write()

def light(kind,color,power,radius,shadows=0):
    r,g,b=color
    return f'''  m_Enabled: 1
  serializedVersion: 11
  m_Type: {kind}
  m_Shape: 0
  m_Color: {{r: {r}, g: {g}, b: {b}, a: 1}}
  m_Intensity: {power}
  m_Range: {radius}
  m_Shadows:
    m_Type: {shadows}
    m_Strength: 0.82
    m_Bias: 0.05
    m_NormalBias: 0.4
  m_CullingMask: {{serializedVersion: 2, m_Bits: 4294967295}}
  m_RenderingLayerMask: 1
  m_Lightmapping: 4'''

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--replace-scene',action='store_true')
    if SCENE.exists() and not p.parse_args().replace_scene:raise SystemExit('Scene exists. Explicit --replace-scene discards its hand edits.')
    build()
