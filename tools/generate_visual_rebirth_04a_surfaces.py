#!/usr/bin/env python3
"""Original repeatable 04A surfaces. No external images. Requires numpy and Pillow.
Mask RGBA = metallic, ambient occlusion, reserved, smoothness. Unity normals are
imported as normal maps; Blender reads these same tangent-space RGB normal maps.
"""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/NeonSyndicate/Resources/VisualRebirth04A'
SIZE = 512
FAMILIES = ['limestone','basalt','copper','bark','cloth','crystal','foliage','paving','glass','water','soil']

def guid(path):
    return hashlib.md5(('VisualRebirth04A/' + str(path.relative_to(ROOT))).encode()).hexdigest()

def meta(path, body='DefaultImporter:\n  externalObjects: {}\n  userData: Original Visual Rebirth 04A asset\n  assetBundleName:\n  assetBundleVariant:\n'):
    side = Path(str(path)+'.meta')
    if not side.exists(): side.write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\n'+body)

def texture_meta(path, normal=False, linear=False):
    meta(path, f'''TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {0 if normal or linear else 1}
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: {int(normal)}
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 1
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 4
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  compressionQuality: 80
  textureCompression: 1
  textureType: {int(normal)}
  textureShape: 1
  alphaSource: 1
  alphaIsTransparency: 0
  platformSettings: []
  userData: Original 04A surface. Mask RGBA metallic AO reserved smoothness.
  assetBundleName:
  assetBundleVariant:
''')

def periodic_noise(n, seed):
    rng = np.random.default_rng(seed)
    u,v = np.meshgrid(np.arange(n)/n,np.arange(n)/n)
    value=np.zeros((n,n))
    for f,weight in [(1,.3),(2,.22),(4,.16),(8,.1),(16,.065),(32,.04),(64,.02)]:
        for _ in range(4):
            angle=rng.uniform(0,math.tau)
            a,b=rng.integers(-f,f+1,2)
            value += weight/4*np.sin(math.tau*(a*u+b*v)+angle)
    return value

def save_maps(family, index):
    n=SIZE; u,v=np.meshgrid(np.arange(n)/n,np.arange(n)/n)
    noise=periodic_noise(n,101+index)
    fine=periodic_noise(n,907+index)
    h=.5+noise*.4; light=.8+noise*.45; metal=0.; gloss=.28; emit=np.zeros((n,n))
    if family in ('limestone','paving'):
        rows=6 if family=='limestone' else 4
        xx=(u*3+(np.floor(v*rows)%2)*.5)%1; yy=v*rows%1
        edge=np.minimum.reduce([xx,1-xx,yy,1-yy])
        bevel=np.clip(edge/.045,0,1)
        vein=np.exp(-np.abs(np.sin(math.tau*(u*3+v*2)+noise*6))*35)
        h=.25+.45*bevel+noise*.17+fine*.06-vein*.025
        light=.66+.2*bevel+noise*.35-vein*.06
        gloss=.25 if family=='limestone' else .34
    elif family=='basalt':
        ridges=np.abs(np.sin(math.tau*(u*5+v*2)+noise*9))
        cracks=np.exp(-ridges*38)
        h=.55+noise*.35-cracks*.12
        light=.63+noise*.4-cracks*.17; gloss=.3
        emit=cracks*.24
    elif family=='copper':
        patina=np.clip(noise*4+.35,0,1)
        h=.5+fine*.08+patina*.025; light=.75+noise*.5
        metal=.85; gloss=.48+fine*.2
    elif family=='bark':
        grain=np.sin(math.tau*(u*18)+np.sin(v*math.tau*2)*.8+noise*8)
        ridges=np.abs(grain)**.5
        h=.25+.45*ridges+fine*.12; light=.52+.33*ridges+noise*.35; gloss=.17
    elif family=='cloth':
        weave=np.sin(u*math.tau*128)*np.sin(v*math.tau*128)
        xx=np.minimum(u,1-u); yy=np.minimum(v,1-v)
        border=(xx<.055)|(yy<.045)
        diamond=np.abs(np.abs(u-.5)+np.abs(v-.5)*.65-.23)<.012
        emblem=diamond|((np.abs(u-.5)<.012)&(v>.22)&(v<.78))
        h=.5+weave*.025; light=.65+noise*.2+weave*.04+border*.2+emblem*.28; gloss=.18
    elif family=='crystal':
        fissure=np.exp(-np.abs(np.sin(math.tau*(u*4+v*3)+noise*7))*55)
        facets=np.floor((u*5+v*3+noise*.5)%1*6)/6
        h=.45+facets*.14-fissure*.05; light=.65+facets*.32+noise*.18
        gloss=.78; metal=.18; emit=fissure*.9+facets*.025
    elif family=='foliage':
        mid=np.exp(-np.abs(u-.5)*180)
        vein=np.exp(-np.abs(np.sin(math.tau*(v*14+np.abs(u-.5)*7)))*28)
        h=.46+vein*.02+mid*.07+noise*.04; light=.73+vein*.1+mid*.11+noise*.4; gloss=.3
    elif family=='glass':
        x,y=u-.5,v-.5; radius=np.sqrt(x*x+y*y); angle=np.arctan2(y,x)
        lead=(np.abs(np.sin(angle*6))<.045)|(np.abs(np.sin(radius*math.tau*6))<.065)
        h=.5+lead*.04; light=.75+noise*.2-lead*.5; gloss=.82; metal=.18
        emit=(~lead)*(.5+.5*np.cos(angle*3+radius*8))*.7
    elif family=='water':
        wave=np.sin(math.tau*(u*3+v*2)+noise*4)+np.sin(math.tau*(u*4-v*3))*.4
        h=.5+wave*.025; light=.8+wave*.035; gloss=.85; metal=.1
    elif family=='soil':
        h=.5+noise*.5+fine*.3; light=.65+noise*.5+fine*.25; gloss=.12
    rgb=np.stack([light,light,light],-1)
    if family=='copper':
        rgb=np.stack([light*(1-patina*.4),light*(.9+patina*.1),light*(.75+patina*.2)],-1)
    if family=='glass':
        rgb*=np.stack([.66+.22*np.sin(angle*3)**2,.72+.2*np.cos(angle*2)**2,np.ones_like(u)],-1)
    if family=='foliage': rgb*=np.array([.88,1,.78])
    dx=(np.roll(h,-1,1)-np.roll(h,1,1))*7
    dy=(np.roll(h,-1,0)-np.roll(h,1,0))*7
    normal=np.stack([-dx,-dy,np.ones_like(h)],-1)
    normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    occlusion=np.clip(.84+(h-h.mean())*.3,.5,1)
    mask=np.stack([np.full_like(h,metal),occlusion,np.zeros_like(h),np.broadcast_to(gloss,h.shape)],-1)
    maps={'Albedo':rgb,'Normal':normal*.5+.5,'Mask':mask,'Emission':np.stack([emit]*3,-1)}
    for kind,data in maps.items():
        path=DEST/'Textures'/f'{family}_{kind}.png'
        Image.fromarray((np.clip(data,0,1)*255).astype(np.uint8)).save(path,optimize=True)
        texture_meta(path,kind=='Normal',kind=='Mask')

PALETTES=[
 {'stone':[.83,.84,.8],'trim':[.95,.9,.77],'roof':[.18,.24,.31],'metal':[.51,.58,.63],'gilt':[.75,.61,.34],'leaf':[.26,.43,.32],'cloth':[.33,.25,.49],'glow':[.48,.58,1]},
 {'stone':[.88,.77,.57],'trim':[.95,.89,.72],'roof':[.31,.34,.25],'metal':[.62,.49,.28],'gilt':[.82,.65,.32],'leaf':[.38,.48,.24],'cloth':[.65,.38,.2],'glow':[1,.68,.27]},
 {'stone':[.57,.64,.5],'trim':[.79,.8,.63],'roof':[.19,.32,.28],'metal':[.32,.54,.43],'gilt':[.61,.62,.34],'leaf':[.21,.49,.3],'cloth':[.17,.39,.32],'glow':[.23,.85,.67]},
 {'stone':[.31,.32,.34],'trim':[.54,.48,.4],'roof':[.14,.17,.2],'metal':[.5,.36,.26],'gilt':[.67,.4,.2],'leaf':[.32,.35,.25],'cloth':[.47,.16,.1],'glow':[1,.37,.1]},
]
ROLE_FAMILY={'stone':'limestone','trim':'limestone','roof':'basalt','metal':'copper','gilt':'copper','bark':'bark','leaf':'foliage','cloth':'cloth','crystal':'crystal','water':'water','glass':'glass','paving':'paving','soil':'soil','flower':'foliage'}

def materials():
    surfaces=[]
    for style,palette in enumerate(PALETTES):
        for role,family in ROLE_FAMILY.items():
            if role=='stone' and style==3: family='basalt'
            tint=palette.get(role,{'bark':[.44,.32,.21],'crystal':palette['glow'],'water':[.13,.4,.39],'glass':palette['glow'],'paving':[.64,.62,.54],'soil':[.36,.31,.2],'flower':[.78,.56,.37]}.get(role,[1,1,1]))
            metallic=.8 if role in ('metal','gilt') else (.18 if role in ('glass','crystal') else 0)
            gloss=.85 if role in ('water','glass') else (.75 if role=='crystal' else (.53 if metallic>.5 else (.17 if role in ('bark','cloth','soil') else .3)))
            strength=.35 if role=='crystal' else (.18 if role=='glass' else (.04 if role=='flower' else 0))
            s={'style':style,'role':role,'family':family,'tint':tint,'metallic':metallic,'smoothness':gloss,'normalStrength':.6 if role in ('stone','trim','bark') else .35,'emission':[c*strength for c in palette['glow']]}
            surfaces.append(s)
            path=DEST/'Materials'/f'{style}_{role}.mat'; tex=DEST/'Textures'
            texture_lines=[]
            for key,kind in [('_MainTex','Albedo'),('_BumpMap','Normal'),('_MetallicGlossMap','Mask'),('_OcclusionMap','Mask'),('_EmissionMap','Emission')]:
                texture_lines.append(f'''    - {key}:
        m_Texture: {{fileID: 2800000, guid: {guid(tex/f'{family}_{kind}.png')}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}''')
            keywords='_NORMALMAP _METALLICGLOSSMAP'+(' _EMISSION' if strength else '')
            path.write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Visual Rebirth 04A {style} {role}
  m_Shader: {{fileID: 46, guid: 0000000000000000f0000000000000000, type: 0}}
  m_ShaderKeywords: {keywords}
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 2000
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
{chr(10).join(texture_lines)}
    m_Floats:
    - _BumpScale: {s['normalStrength']}
    - _Glossiness: {gloss}
    - _GlossMapScale: 1
    - _Metallic: {metallic}
    - _OcclusionStrength: 0.65
    m_Colors:
    - _Color: {{r: {tint[0]}, g: {tint[1]}, b: {tint[2]}, a: 1}}
    - _EmissionColor: {{r: {s['emission'][0]}, g: {s['emission'][1]}, b: {s['emission'][2]}, a: 1}}
''')
            meta(path,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: Original 04A Built-in look-development material. Runtime selects active pipeline.\n  assetBundleName:\n  assetBundleVariant:\n')
    path=DEST/'material_manifest.json'
    path.write_text(json.dumps({'version':'04A','maskChannels':'R metallic, G ambient occlusion, B reserved, A smoothness','surfaces':surfaces},indent=2)+'\n')
    meta(path)

def main():
    for path in [DEST,DEST/'Textures',DEST/'Materials']:
        path.mkdir(parents=True,exist_ok=True)
        meta(path,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    for i,family in enumerate(FAMILIES): save_maps(family,i)
    materials()
    print(f'04A: {len(FAMILIES)} original 512x512 surface families, {len(FAMILIES)*4} maps, {len(PALETTES)*len(ROLE_FAMILY)} materials')
if __name__=='__main__': main()
