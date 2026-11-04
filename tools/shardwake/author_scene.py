"""Bake the original pilot once. Hand-edited scene transforms are preserved unless --replace-scene is explicit."""
import argparse
import math
import random
from assets import ART, CODE, PAINT, ROOT, SCENE, SHADER, Scene, guid, meta
import land

def assets():
    for folder in [ART,ART/'Meshes',ART/'Materials',CODE]:meta(folder)
    meta(SHADER,'shader');meta(ART/'HearthCommission.json','json')
    for p in CODE.glob('*.cs'):meta(p,'script')
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
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Lift: 1
    m_Colors: []
''');meta(PAINT,'material')
    meshes={name:land.terrain(name,rows) for name,rows in land.ROWS.items()}
    meshes.update({'Trail':land.path_mesh(),'Bridge':land.bridge(),'SplitCrown':land.spire(),'Water':land.water(),
            'Regulator':land.terminal(),'Lens':land.gem(),'Planting':land.planting(),'Flywheel':land.flywheel(),'LoadingRamp':land.loading_ramp()})
    for name,part in zip(['Workshop','SweptRoof','Workbenches'],land.workshop()):meshes[name]=part
    for i in range(3):meshes['Pine'+str(i)]=land.pine(i)
    for i in range(2):meshes['Talus'+str(i)]=land.rocks(i);meshes['Ridge'+str(i)]=land.ridge(i)
    signal=ART/'Materials/ValeSignal.shader';meta(signal,'shader')
    mat=ART/'Materials/ValeSignal.mat'
    mat.write_text(PAINT.read_text().replace('ValePalette','ValeSignal').replace(guid(SHADER),guid(signal)).replace('    m_Colors: []','    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}'))
    meta(mat,'material')
    return {name:mesh.save(name) for name,mesh in meshes.items()}

def build():
    meshes=assets();s=Scene()
    def piece(name,mesh,pos=(0,0,0),parent=None,solid=True,yaw=0,scale=(1,1,1)):
        n=s.obj(name,pos,yaw,scale,parent);s.geo(n,meshes[mesh],solid);return n
    pilot=s.obj('Shardwake Vale - pilot')
    ground=s.obj('01 - Terrain and river')
    piece('Ochre hearth meadow','NearBank',parent=ground)
    piece('Broken ravine walls and bed','Ravine',parent=ground)
    piece('Sage and lilac far bank','FarBank',parent=ground)
    piece('River current','Water',parent=ground,solid=False)
    piece('Footpath - both banks','Trail',parent=ground,solid=False)
    shop=s.obj('02 - Dante workshop',(-16.5,land.surface(-16.5,4),4))
    shell=piece('Open workshop hall','Workshop',parent=shop)
    piece('Graded workshop apron','LoadingRamp',parent=shop)
    piece('Copper throat and swept roof','SweptRoof',parent=shop)
    piece('Anvil benches and lamps','Workbenches',parent=shop)
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
    lenses=[]
    for i,(x,z) in enumerate([(-4.65,-6.15),(6.65,-2.25),(-4.9,5.5)]):
        y=land.surface(12+x,104+z)-crown['pos'][1]
        stand=piece('Regulator '+str(i+1),'Regulator',(x,y,z),crown)
        lenses.append(piece('Live mineral '+str(i+1),'Lens',parent=stand,solid=False))
    pick=piece('Removable hearth seed','Lens',(1.2,land.surface(13.2,98.35)-crown['pos'][1],-5.65),crown,False,scale=(.63,.60,.63))
    pick['active']=False
    greens=s.obj('05 - Pines and sheltered planting');rng=random.Random(729)
    groups=[(-29,1,4),(-24,17,6),(19,13,3),(29,24,5),(-23,61,6),(23,67,5),(-31,83,6),
            (-2,77,3),(34,89,5),(-18,110,5),(40,113,6),(5,128,7)]
    for i,(cx,cz,n) in enumerate(groups):
        for j in range(n):
            x=cx+rng.uniform(-4,4);z=cz+rng.uniform(-4,4);k=rng.uniform(.68,1.28)
            piece('Pine %02d.%02d'%(i+1,j+1),'Pine'+str((i+j)%3),(x,land.surface(x,z)-.08,z),greens,False,
                  rng.randrange(360),(k,k*rng.uniform(.86,1.19),k))
    for i,(x,z) in enumerate([(-13,-6),(-12,-4),(-14,-3),(-18,-3),(-9,-12),(-10,-9),(-2,-9),(0,-7),
            (-3,-2),(3,3),(-5,12),(-3,14),(9,12),(11,29),(18,23),(-9,60),(-8,61),(-9,85),(3,87),(7,99)]):
        k=rng.uniform(.8,1.36);piece('Bellfern clump %02d'%i,'Planting',(x,land.surface(x,z)-.025,z),greens,False,i*37,(k,k,k))
    crags=s.obj('06 - Broken stone shelves')
    for i,(x,z,k) in enumerate([(-23,9,1.3),(18,15,1.2),(23,26,1.65),(-9,26,1.45),(17,53,1.9),
                                (-13,57,1.4),(-24,67,1.3),(4,72,1.55),(-22,84,1.5),(22,91,1.6),(31,107,2)]):
        piece('Layered shelf %02d'%i,'Talus'+str(i%2),(x,land.surface(x,z)-.1,z),crags,True,i*33,(k,k*1.13,k))
    horizon=s.obj('07 - Painted horizon')
    for i in range(2):piece('Distant range '+str(i+1),'Ridge'+str(i),parent=horizon,solid=False)
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
  far clip plane: 330
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
    sun=s.obj('Warm ridge sun',(0,22,5),34,pitch=51);s.comp(sun,108,'Light',light(1,(1,.90,.76),1.26,120))
    def ref(n,offset=1):return '{fileID: '+str(n['id']+offset)+'}'
    fields=''.join('  '+key+': '+ref(node)+'\n' for key,node in [('spawn',spawn),('dante',dante),('bridge',bridge),
             ('refuge',refuge),('landmark',crown),('pickup',pick)])+'  sockets:\n'+''.join('  - '+ref(n)+'\n' for n in lenses)
    fields+='  forgeHeart: '+ref(heart,0)+'\n  forgeFire: '+ref(fire,0)+'\n  view: '+ref(camera,2)+'\n  bellowsWheel: '+ref(wheel)+'\n  terrain: '+ref(ground)+'\n  furnaceStone: '+ref(shell,3)+'\n  signalMaterial: {fileID: 2100000, guid: '+guid(ART/'Materials/ValeSignal.mat')+', type: 2}\n  commission: {fileID: 4900000, guid: '+guid(ART/'HearthCommission.json')+', type: 3}'
    s.script(pilot,'ShardwakeVale',fields);s.write()

def light(kind,color,power,radius):
    r,g,b=color
    return f'''  m_Enabled: 1
  serializedVersion: 11
  m_Type: {kind}
  m_Shape: 0
  m_Color: {{r: {r}, g: {g}, b: {b}, a: 1}}
  m_Intensity: {power}
  m_Range: {radius}
  m_Shadows:
    m_Type: 0
  m_CullingMask: {{serializedVersion: 2, m_Bits: 4294967295}}
  m_RenderingLayerMask: 1
  m_Lightmapping: 4'''

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--replace-scene',action='store_true')
    if SCENE.exists() and not p.parse_args().replace_scene:raise SystemExit('Scene exists. Explicit --replace-scene discards its hand edits.')
    build()
