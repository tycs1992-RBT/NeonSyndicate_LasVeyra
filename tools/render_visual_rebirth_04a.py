#!/usr/bin/env python3
"""Render actual shipped CMH1 geometry and runtime layout JSON in Blender.

These are offline geometry/material review renders, never Unity screenshots.
No decorative geometry is invented by the renderer. The floor, camera, lights
and world are an explicitly documented review rig, not game-world exports.
A small managed recording adapter exports the unchanged 03A2 mesh-generating
source to CMH1. This is geometry extraction, not a Unity compilation test.

Usage:
  python3 tools/render_visual_rebirth_04a.py --export-baseline
  python3 tools/render_visual_rebirth_04a.py --views hearthspire_flight,hearthspire_ground
  python3 tools/render_visual_rebirth_04a.py --views all --samples 32
Requires Blender 4.x, Python Pillow and .NET 8 for optional baseline extraction.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/NeonSyndicate/Resources/VisualRebirth04A'
OLD = ROOT / 'Assets/NeonSyndicate/Resources/CrystalStrongholds02I'
OUT = ROOT / 'Docs/VisualRebirth04A/Previews'
CACHE = Path(tempfile.gettempdir()) / 'visual-rebirth-04a-preview'
SOURCE = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Strongholds'

# Camera positions use Unity's original Y-up coordinate system in site-local metres.
VIEWS = {
    'hearthspire_before': ('hearthspire', (42, 31, -48), (0, 5, 1), 'day', True),
    'hearthspire_flight': ('hearthspire', (42, 31, -48), (0, 5, 1), 'day', False),
    'hearthspire_ground': ('hearthspire', (0, 2.3, -32), (0, 5.2, 5), 'day', False),
    'hearthspire_garden': ('hearthspire', (-18, 5.2, -11), (0, 4, 11), 'day', False),
    'hearthspire_night': ('hearthspire', (33, 20, -43), (0, 5, 1), 'night', False),
    'galehook_flight': ('galehook', (49, 39, -58), (0, 8, 2), 'day', False),
    'galehook_ground': ('galehook', (0, 2.3, -32), (0, 6.0, 6), 'day', False),
    'sunscar_flight': ('sunscar', (49, 39, -58), (0, 8, 2), 'day', False),
    'sunscar_night': ('sunscar', (53, 42, -64), (0, 10, 2), 'night', False),
}
STYLE = {'hearthspire': 0, 'galehook': 2, 'sunscar': 3}

# Recording-only stand-ins. Native Unity behavior is NOT tested by this adapter.
ADAPTER = r'''
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16, UInt32 } }
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 zero=>new Vector3(0,0,0); public static Vector3 one=>new Vector3(1,1,1);
  public static Vector3 up=>new Vector3(0,1,0); public static Vector3 down=>new Vector3(0,-1,0);
  public static Vector3 right=>new Vector3(1,0,0); public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 back=>new Vector3(0,0,-1);
  public float sqrMagnitude=>x*x+y*y+z*z; public Vector3 normalized {get {float n=(float)Math.Sqrt(sqrMagnitude);return n>1e-10f?this/n:zero;}}
  public void Normalize(){this=normalized;} public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); public static Vector3 operator -(Vector3 a)=>a*-1f;
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b); public static Vector3 operator *(float b,Vector3 a)=>a*b;
  public static Vector3 operator /(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
  public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
 }
 public struct Quaternion {
  public Vector3 e; public static Quaternion identity=>new Quaternion();
  public static Quaternion Euler(float x,float y,float z)=>new Quaternion {e=new Vector3(x,y,z)};
  public static Vector3 operator *(Quaternion q,Vector3 p) { double a=q.e.y*Math.PI/180; return new Vector3((float)(p.x*Math.Cos(a)+p.z*Math.Sin(a)),p.y,(float)(-p.x*Math.Sin(a)+p.z*Math.Cos(a))); }
 }
 public static class Mathf { public const float PI=(float)Math.PI; public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v)); public static int Max(int a,int b)=>Math.Max(a,b); public static float Cos(float v)=>(float)Math.Cos(v); public static float Sin(float v)=>(float)Math.Sin(v); }
 public class Mesh {
  public string name; public Rendering.IndexFormat indexFormat; public List<Vector3> v,n; public List<Vector2> uv; public List<int> indices;
  public void SetVertices(List<Vector3> a){v=a;} public void SetNormals(List<Vector3> a){n=a;} public void SetUVs(int c,List<Vector2> a){uv=a;} public void SetTriangles(List<int> a,int c){indices=a;}
  public void RecalculateBounds(){} public void RecalculateTangents(){}
 }
}
namespace NeonSyndicate {
 using UnityEngine;
 public enum FortModule { Empty, Watchtower, Conservatory, Bulwark, FlightDais }
 public class FortSlotSave { public int Module,Rotation; }
 public class StrongholdSiteSave { public bool Owned; public int Theme; public List<FortSlotSave> Slots=new List<FortSlotSave>(); }
 public static class StrongholdPlacement { public const float PlotHalf=24; public static Vector3 Socket(int index) { SOCKET_BODY } }
 public class Recorder {
  public List<object> parts=new List<object>();
  public void Part(string model,string role,Vector3 p,Vector3 s,Quaternion e){parts.Add(new {model,role,position=A(p),scale=A(s),euler=A(e.e)});}
  public void Finish(){} public static float[] A(Vector3 v)=>new[]{v.x,v.y,v.z};
 }
 public class FortBatch:Recorder { public FortBatch(object p,object a,int t){} }
 public class Dummy { public object transform; }
 public class Program {
  static void Box(object p,Vector3 v,Vector3 s,Quaternion q){} static void Cylinder(object p,Vector3 v,float r,float h){}
  static List<object> Base(StrongholdSiteSave state) { Dummy staged=new Dummy();object _assets=null;int tier=1; BASE_BODY return batch.parts; }
  public static void Main(string[] args) {
   string dest=args[0];Directory.CreateDirectory(dest);
   foreach(string id in CitadelGeometry03A2.ModelIds()){
    Mesh mesh=CitadelGeometry03A2.Build(id,false);using(BinaryWriter w=new BinaryWriter(File.Create(Path.Combine(dest,id+".bytes")))) {
     w.Write(new byte[]{67,77,72,49});w.Write(mesh.indices.Count);
     foreach(int i in mesh.indices){Vector3 p=mesh.v[i],n=mesh.n[i];Vector2 uv=mesh.uv[i];w.Write(p.x);w.Write(p.y);w.Write(p.z);w.Write(n.x);w.Write(n.y);w.Write(n.z);w.Write(uv.x);w.Write(uv.y);}
    }
   }
   foreach(string site in new[]{"hearthspire","galehook","sunscar"}) {
    int style=site=="galehook"?2:site=="sunscar"?3:0;
    StrongholdSiteSave state=new StrongholdSiteSave {Theme=style==3?1:style};
    for(int i=0;i<6;i++)state.Slots.Add(new FortSlotSave{Module=i%2==0?1:3});
    List<object> parts=new List<object>(); foreach(CitadelPart03A2 p in CitadelLayout03A2.Build(style,state.Slots))parts.Add(new {model=p.Model,role=p.Role,position=Recorder.A(p.Position),scale=Recorder.A(p.Scale),euler=Recorder.A(p.Euler),fine=p.Fine});
    List<object> socketParts=new List<object>();foreach(StrongholdPart04A p in StrongholdLayout04A.SocketParts(state.Slots))socketParts.Add(new {model=p.model,role=p.role,position=p.position,scale=p.scale,euler=p.euler,fine=p.fine});
    File.WriteAllText(Path.Combine(dest,site+".json"),JsonSerializer.Serialize(new {version="03A2",site,parts,baseParts=Base(state),socketParts}));
   }
  }
 }
}
'''


def export_baseline(dotnet=None):
    """Extract unmodified source geometry via a recording adapter, not fake Unity tests."""
    CACHE.mkdir(parents=True, exist_ok=True)
    project = CACHE / 'source-export'
    project.mkdir(exist_ok=True)
    source = (SOURCE / 'StrongholdView.cs').read_text()
    start = source.index('FortBatch batch =')
    end = source.index('batch.Finish();', start) + len('batch.Finish();')
    base_body = source[start:end]
    placement = (SOURCE / 'StrongholdPlacement.cs').read_text()
    start = placement.index('{', placement.index('Vector3 Socket(')) + 1
    socket_body = placement[start:placement.index('}', start)]
    (project / 'Program.cs').write_text(ADAPTER.replace('BASE_BODY', base_body).replace('SOCKET_BODY', socket_body))
    source_hashes = {}
    for name in ('CitadelGeometry03A2.cs', 'CitadelLayout03A2.cs'):
        text = (SOURCE / name).read_text()
        (project / name).write_text(text)
        source_hashes[name] = hashlib.sha256(text.encode()).hexdigest()
    sockets = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/ArtRebirth04A/StrongholdLayout04A.cs'
    (project / sockets.name).write_text(sockets.read_text())
    source_hashes[sockets.name] = hashlib.sha256(sockets.read_bytes()).hexdigest()
    source_hashes['StrongholdView.cs:baseParts'] = hashlib.sha256(base_body.encode()).hexdigest()
    source_hashes['StrongholdPlacement.cs:Socket'] = hashlib.sha256(socket_body.encode()).hexdigest()
    (project / 'Recorder.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
    dotnet = dotnet or shutil.which('dotnet') or '/workspace/.tools/dotnet/dotnet'
    env = dict(os.environ, DOTNET_CLI_HOME=str(CACHE / 'dotnet-home'), DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    # Direct Roslyn avoids network restores and first-run NuGet mutexes in a sandbox.
    sdk = Path(dotnet).resolve().parent
    compiler = sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    references = sorted(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    output = project / 'Recorder.dll'
    command = [dotnet, str(compiler), '-nologo', '-target:exe', '-langversion:9', '-out:' + str(output)]
    command += ['-r:' + str(p) for p in references]
    command += [str(p) for p in project.glob('*.cs')]
    subprocess.run(command, env=env, check=True)
    runtime_version = sorted((sdk / 'shared/Microsoft.NETCore.App').iterdir())[-1].name
    output.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime_version}}}))
    subprocess.run([dotnet, str(output), str(CACHE / 'baseline')], env=env, check=True)
    (CACHE / 'baseline/source_provenance.json').write_text(json.dumps({'method': 'Unchanged source executed with a managed vertex-recording adapter. Not a Unity compile or runtime test.', 'sha256': source_hashes}, indent=2))


def cmh(path):
    raw = path.read_bytes()
    if raw[:4] != b'CMH1':
        raise ValueError('Unsupported mesh header: ' + str(path))
    count = struct.unpack_from('<i', raw, 4)[0]
    if count % 3 or len(raw) != 8 + count * 32:
        raise ValueError('Invalid triangle soup: ' + str(path))
    return list(struct.iter_unpack('<8f', raw[8:]))


def baseline_maps():
    """Reproduce CitadelKit03A2 procedural maps for a fair source-geometry comparison."""
    import numpy as np
    from PIL import Image
    out = CACHE / 'baseline/maps'; out.mkdir(parents=True, exist_ok=True)
    n = 256
    y, x = np.mgrid[:n, :n]
    u, v = x / n, y / n
    h = (x.astype(np.uint64) * 374761393 + y.astype(np.uint64) * 668265263 + 1297) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
    noise = (h & 65535) / 65535
    bx, by = (u * 4 + (np.floor(v * 8) % 2) * .5) % 1, (v * 8) % 1
    edge = np.minimum(np.minimum(bx, 1 - bx), np.minimum(by, 1 - by))
    height = .59 + .28 * np.minimum(1, edge / .055) + .035 * noise
    stone = height * (.93 + .07 * noise)
    px, py = np.abs(u - .5), np.abs(v - .5)
    border = (u < .045) | (u > .955) | (v < .035) | (v > .965)
    emblem = (np.abs(px + py * .65 - .22) < .017) | ((px < .018) & (v > .24) & (v < .75))
    cloth = np.where(border | emblem, .97, .60 + .09 * ((x + y) % 2) + .035 * noise)
    leaf = .65 + .18 * np.abs(np.cos((u + .12 * np.sin(v * 15)) * 22)) ** 12 + .10 * noise
    mineral = .74 + .17 * np.sin(u * 10 + v * 7) + .05 * noise
    for name, gray in [('stone', stone), ('cloth', cloth), ('leaf', leaf), ('mineral', mineral)]:
        rgb = np.stack([gray, gray, gray], -1)
        if name == 'stone':
            rgb *= np.array([1, .99, .97])
        Image.fromarray(np.rint(np.clip(rgb, 0, 1) * 255).astype(np.uint8)).save(out / (name + '.png'))
    dx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
    dy = np.roll(height, -1, 0) - np.roll(height, 1, 0)
    normal = np.stack([-dx * 2.2, -dy * 2.2, np.ones_like(dx)], -1)
    normal /= np.linalg.norm(normal, axis=-1)[..., None]
    Image.fromarray(np.rint((normal * .5 + .5) * 255).astype(np.uint8)).save(out / 'normal.png')


def label_image(path, title, detail):
    from PIL import Image, ImageDraw, ImageFont
    im = Image.open(path).convert('RGB')
    draw = ImageDraw.Draw(im)
    font_path = '/usr/share/fonts/truetype/open-sans/OpenSans-Semibold.ttf'
    small_path = '/usr/share/fonts/truetype/open-sans/OpenSans-Regular.ttf'
    font = ImageFont.truetype(font_path, max(14, im.width // 75)) if Path(font_path).exists() else ImageFont.load_default(size=max(14, im.width // 75))
    small = ImageFont.truetype(small_path, max(12, im.width // 100)) if Path(small_path).exists() else ImageFont.load_default(size=max(12, im.width // 100))
    footer = max(64, im.height // 10)
    draw.rectangle((0, im.height - footer, im.width, im.height), fill=(8, 16, 25))
    draw.text((22, im.height - footer + 9), title + ' | OFFLINE BLENDER PREVIEW', font=font, fill=(232, 235, 228))
    draw.text((22, im.height - footer + 35), detail, font=small, fill=(168, 190, 198))
    im.save(path, optimize=True)


def preview_manifest():
    p = OUT / 'preview_manifest.json'
    return json.loads(p.read_text()) if p.exists() else {'format': '04A offline geometry review', 'unity_screenshots': False, 'views': {}}


def main():
    global OUT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--views', default='hearthspire_flight')
    parser.add_argument('--samples', type=int, default=32)
    parser.add_argument('--width', type=int, default=1280)
    parser.add_argument('--height', type=int, default=800)
    parser.add_argument('--export-baseline', action='store_true')
    parser.add_argument('--inside-blender', action='store_true')
    parser.add_argument('--dotnet')
    parser.add_argument('--city-dir', default=str(OUT / 'CitySource'))
    parser.add_argument('--output-dir', default=str(OUT))
    parser.add_argument('--threads', type=int, default=min(4, os.cpu_count() or 1))
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else None)
    OUT = Path(args.output_dir)
    if args.inside_blender:
        render(args)
        return
    OUT.mkdir(parents=True, exist_ok=True)
    CACHE.mkdir(parents=True, exist_ok=True)
    if args.export_baseline:
        export_baseline(args.dotnet)
        return
    if not (CACHE / 'baseline/hearthspire.json').exists():
        export_baseline(args.dotnet)
    baseline_maps()
    names = list(VIEWS) + ['sanctuary_ground', 'sanctuary_flight', 'tree_lineup', 'kit_sheet'] if args.views == 'all' else args.views.split(',')
    if args.views == 'all':
        names += [name for name in ('city_spawn', 'city_verdant', 'city_dawnstone', 'city_obsidian') if (Path(args.city_dir) / (name + '.json')).exists()]
    for name in names:
        if name not in VIEWS and name not in ('kit_sheet', 'sanctuary_ground', 'sanctuary_flight', 'tree_lineup') and not name.startswith('city_'):
            raise ValueError('Unknown view: ' + name)
    subprocess.run(['blender', '-b', '--threads', str(args.threads), '--python', str(Path(__file__).resolve()), '--', '--inside-blender', '--views', ','.join(names), '--samples', str(args.samples), '--width', str(args.width), '--height', str(args.height), '--city-dir', args.city_dir, '--output-dir', args.output_dir], check=True)
    for name in names:
        title = name.replace('_', ' ').title()
        detail = 'Actual shipped geometry and source layout. Review lighting. Unity import, gameplay and performance unverified.'
        if name in ('kit_sheet', 'tree_lineup'):
            detail = 'Actual CMH1 master meshes. Catalog arrangement only, not a runtime scene. Review lighting and palette.'
        elif name.startswith('city_all'):
            detail = 'All 1,319 eligible source-generated buildings loaded. Ordinary geometry budget; roads, terrain and gameplay omitted.'
        elif name.startswith('city_'):
            detail = 'Actual CityLayout 61724 parcel and architecture-source geometry. Ordinary city budget. Offline review rig.'
        elif name.endswith('_before'):
            detail = '03A2 source geometry via recording adapter. Matched camera and lighting. No Unity runtime was executed.'
        label_image(OUT / (name + '.png'), title, detail)


def render(args):
    import bpy
    from mathutils import Matrix, Vector
    names = args.views.split(',')
    manifest = preview_manifest()
    model_cache = {}
    material_cache = {}
    hashes = {}
    material_manifest = json.loads((ART / 'material_manifest.json').read_text()) if (ART / 'material_manifest.json').exists() else {}
    C = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
    CI = C.inverted()

    def transform(part):
        pos = part.get('position', [0, 0, 0]); rot = part.get('euler', [0, 0, 0]); scl = part.get('scale', [1, 1, 1])
        r = [math.radians(x) for x in rot]
        return C @ Matrix.Translation(Vector(pos)) @ Matrix.Rotation(r[1], 4, 'Y') @ Matrix.Rotation(r[0], 4, 'X') @ Matrix.Rotation(r[2], 4, 'Z') @ Matrix.Diagonal((*scl, 1)) @ CI

    def point(v):
        return (v[0], -v[2], v[1])

    def mesh(path):
        key = str(path)
        if key in model_cache:
            return model_cache[key]
        import numpy as np
        raw = path.read_bytes()
        count = struct.unpack_from('<i', raw, 4)[0]
        if raw[:4] != b'CMH1' or count % 3 or len(raw) != 8 + count * 32:
            raise ValueError('Invalid runtime mesh: ' + str(path))
        data = np.frombuffer(raw, dtype='<f4', offset=8).reshape(count, 8)
        if not np.isfinite(data).all():
            raise ValueError('Non-finite runtime vertex: ' + str(path))
        coords = np.stack((data[:, 0], -data[:, 2], data[:, 1]), axis=1)
        normals = np.stack((data[:, 3], -data[:, 5], data[:, 4]), axis=1)
        m = bpy.data.meshes.new(path.stem)
        m.vertices.add(count); m.vertices.foreach_set('co', coords.ravel())
        m.loops.add(count); m.loops.foreach_set('vertex_index', np.arange(count, dtype=np.int32))
        m.polygons.add(count // 3)
        m.polygons.foreach_set('loop_start', np.arange(0, count, 3, dtype=np.int32))
        m.polygons.foreach_set('loop_total', np.full(count // 3, 3, dtype=np.int32))
        m.update()
        uv = m.uv_layers.new(name='UVMap'); uv.data.foreach_set('uv', data[:, 6:8].ravel())
        # Original hard-edge and smooth normals are retained, including crystal facets.
        m.normals_split_custom_set_from_vertices(normals)
        model_cache[key] = m
        hashes[str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
        return m

    def old_tint(style, role):
        table = {
            'stone': [(0.59, .63, .76), (.86, .79, .61), (.48, .56, .48), (.21, .23, .27)],
            'trim': [(.91, .88, .77), (.91, .88, .77), (.72, .76, .57), (.43, .36, .31)],
            'gilt': [(.84, .70, .38), (.84, .70, .38), (.66, .69, .37), (.67, .38, .17)],
            'bark': [(.32, .25, .19)] * 4,
            'leaf': [(.21, .46, .34), (.37, .49, .24), (.21, .46, .34), (.23, .28, .24)],
            'cloth': [(.30, .20, .49), (.58, .36, .14), (.13, .39, .29), (.38, .12, .09)],
            'crystal': [(.47, .57, 1), (1, .73, .34), (.19, .8, .56), (1, .32, .07)]}
        return table.get(role, table['crystal'])[style]

    def material(style, role, baseline=False):
        key = (style, role, baseline)
        if key in material_cache:
            return material_cache[key]
        spec = resolve_material(material_manifest, style, role) if not baseline else {}
        if baseline:
            family = 'stone' if role in ('stone', 'trim') else 'cloth' if role == 'cloth' else 'leaf' if role in ('leaf', 'bark') else 'mineral'
            spec = {'maps': {'albedo': str(CACHE / 'baseline/maps' / (family + '.png'))}, 'normalStrength': .42}
            if family == 'stone':
                spec['maps']['normal'] = str(CACHE / 'baseline/maps/normal.png')
        tint = spec.get('tint', old_tint(style, role))
        rough = spec.get('roughness', .44 if role in ('gilt', 'metal') else .75 if role in ('stone', 'trim') else .64)
        metallic = spec.get('metallic', .7 if role in ('gilt', 'metal') else 0)
        m = bpy.data.materials.new(('03A2 ' if baseline else '04A ') + str(style) + '/' + role)
        m.use_nodes = True
        nodes = m.node_tree.nodes; links = m.node_tree.links
        bsdf = nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*tint[:3], 1)
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = rough
        maps = spec.get('maps', {})
        for channel, socket in [('albedo', 'Base Color'), ('roughness', 'Roughness')]:
            file = maps.get(channel)
            if not file:
                continue
            path = ART / file
            if not path.exists():
                raise FileNotFoundError(path)
            image = bpy.data.images.load(str(path), check_existing=True)
            image.colorspace_settings.name = 'sRGB' if channel == 'albedo' else 'Non-Color'
            tex = nodes.new('ShaderNodeTexImage'); tex.image = image
            if channel == 'albedo':
                mul = nodes.new('ShaderNodeMixRGB'); mul.blend_type = 'MULTIPLY'; mul.inputs[0].default_value = 1
                mul.inputs[2].default_value = (*tint[:3], 1)
                links.new(tex.outputs['Color'], mul.inputs[1]); links.new(mul.outputs['Color'], bsdf.inputs[socket])
            else:
                links.new(tex.outputs['Color'], bsdf.inputs[socket])
        if maps.get('normal'):
            tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['normal']), check_existing=True); tex.image.colorspace_settings.name = 'Non-Color'
            norm = nodes.new('ShaderNodeNormalMap'); norm.inputs['Strength'].default_value = spec.get('normalStrength', .55)
            links.new(tex.outputs['Color'], norm.inputs['Color']); links.new(norm.outputs['Normal'], bsdf.inputs['Normal'])
        if maps.get('mask'):
            tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['mask']), check_existing=True); tex.image.colorspace_settings.name = 'Non-Color'
            separate = nodes.new('ShaderNodeSeparateColor'); links.new(tex.outputs['Color'], separate.inputs['Color'])
            links.new(separate.outputs['Red'], bsdf.inputs['Metallic'])
            inverse = nodes.new('ShaderNodeMath'); inverse.operation = 'SUBTRACT'; inverse.inputs[0].default_value = 1
            links.new(tex.outputs['Alpha'], inverse.inputs[1]); links.new(inverse.outputs[0], bsdf.inputs['Roughness'])
        emission = spec.get('emission', .20 if role in ('crystal', 'bloom') else 0)
        if isinstance(emission, list):
            emit_tint = emission; emission = 1
        else:
            emit_tint = tint
        if emission:
            bsdf.inputs['Emission Color'].default_value = (*emit_tint[:3], 1)
            bsdf.inputs['Emission Strength'].default_value = emission
            if maps.get('emission'):
                tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['emission']), check_existing=True)
                tex.image.colorspace_settings.name = 'sRGB'
                mul = nodes.new('ShaderNodeMixRGB'); mul.blend_type = 'MULTIPLY'; mul.inputs[0].default_value = 1; mul.inputs[2].default_value = (*emit_tint[:3], 1)
                links.new(tex.outputs['Color'], mul.inputs[1]); links.new(mul.outputs['Color'], bsdf.inputs['Emission Color'])
        material_cache[key] = m
        return m

    def add_part(part, style, source='04A', baseline=False):
        name = part['model']
        if source == '02I':
            path = OLD / 'Geometry' / (name + '.bytes')
        elif source == 'CITY':
            path = Path(args.city_dir) / (name + '.bytes')
        elif source == '03A2':
            path = CACHE / 'baseline' / (name + '.bytes')
        else:
            path = ART / 'Meshes' / (name + '_high.bytes')
        ob = bpy.data.objects.new(source + '/' + name, mesh(path))
        bpy.context.collection.objects.link(ob)
        ob.matrix_world = transform(part)
        ob.data.materials.clear() if not ob.data.materials else None
        ob.data.materials.append(material(style, part['role'], baseline)) if not ob.data.materials else None
        ob.material_slots[0].link = 'OBJECT'
        ob.material_slots[0].material = material(style, part['role'], baseline)
        return ob

    def setup(camera_pos, target, mood):
        scene = bpy.context.scene
        scene.render.engine = 'CYCLES'
        scene.cycles.samples = args.samples
        scene.cycles.use_adaptive_sampling = True
        scene.cycles.adaptive_threshold = .04
        scene.cycles.adaptive_min_samples = min(16, args.samples)
        scene.cycles.use_denoising = False
        scene.cycles.max_bounces = 5
        scene.cycles.diffuse_bounces = 3
        scene.cycles.glossy_bounces = 3
        scene.render.resolution_x = args.width; scene.render.resolution_y = args.height; scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = 'PNG'
        scene.view_settings.view_transform = 'AgX'
        scene.view_settings.look = 'AgX - Medium High Contrast'
        scene.view_settings.exposure = .5 if mood == 'day' else 1.1
        world = bpy.data.worlds.new('Offline review world')
        scene.world = world; world.use_nodes = True
        bg = world.node_tree.nodes.get('Background')
        bg.inputs['Color'].default_value = (.58, .69, .81, 1) if mood == 'day' else (.1, .16, .28, 1)
        bg.inputs['Strength'].default_value = .55 if mood == 'day' else .35
        bpy.ops.object.light_add(type='SUN', location=(0, 0, 40))
        sun = bpy.context.object; sun.name = 'Offline review directional light'
        sun.rotation_euler = (math.radians(30), math.radians(-20), math.radians(-35))
        sun.data.energy = 2.1 if mood == 'day' else .65
        sun.data.angle = math.radians(9 if mood == 'day' else 14)
        sun.data.color = (1, .85, .68) if mood == 'day' else (.52, .68, 1)
        bpy.ops.mesh.primitive_plane_add(size=2000, location=(0, 0, -.035))
        ground = bpy.context.object; ground.name = 'Review ground plane, not game terrain'
        mat = bpy.data.materials.get('Review floor') or bpy.data.materials.new('Review floor')
        mat.use_nodes = True; p = mat.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value = (.155, .17, .16, 1); p.inputs['Roughness'].default_value = .95
        ground.data.materials.append(mat)
        bpy.ops.object.camera_add(location=point(camera_pos))
        camera = bpy.context.object
        direction = Vector(point(target)) - camera.location
        camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        camera.data.lens = 42
        camera.data.clip_end = 2500
        scene.camera = camera
        return scene

    def clear():
        for ob in list(bpy.data.objects):
            bpy.data.objects.remove(ob, do_unlink=True)

    def fit_source_camera(scene, target_unity, padding=1.15):
        # Review cameras must show the complete real source silhouette. Do not
        # include the artificial review ground in the framing calculation.
        bpy.context.view_layer.update()
        camera = scene.camera
        target = Vector(point(target_unity))
        outward = (camera.location - target).normalized()
        right = camera.rotation_euler.to_matrix() @ Vector((1, 0, 0))
        up = camera.rotation_euler.to_matrix() @ Vector((0, 1, 0))
        frame = camera.data.view_frame(scene=scene)
        tan_x = max(abs(v.x / v.z) for v in frame)
        tan_y = max(abs(v.y / v.z) for v in frame)
        distance = (camera.location - target).length
        for ob in scene.objects:
            if ob.type != 'MESH' or not ob.name.startswith('CITY/'):
                continue
            for corner in ob.bound_box:
                delta = ob.matrix_world @ Vector(corner) - target
                distance = max(distance,
                    abs(delta.dot(right)) * padding / tan_x + delta.dot(outward),
                    abs(delta.dot(up)) * padding / tan_y + delta.dot(outward))
        camera.location = target + outward * distance
        x, y, z = camera.location
        return (x, z, -y)

    for name in names:
        clear()
        if name.startswith('city_'):
            layout_path = Path(args.city_dir) / (('city_all' if name.startswith('city_all') else name) + '.json')
            data = json.loads(layout_path.read_text())
            for part in data['parts']:
                add_part(part, part['style'], 'CITY')
            if data.get('ordinaryBudget') and 'bounds_min' in data:
                lo, hi = data['bounds_min'], data['bounds_max']
                span = max(hi[0] - lo[0], hi[2] - lo[2]); center = [(hi[i] + lo[i]) * .5 for i in range(3)]
                if name.endswith('_flight'):
                    camera_pos = (1100, 740, -1100); target = (0, 45, 100)
                else:
                    camera_pos = (center[0] + span * .64, span * .78, center[2] - span * .83)
                    target = (center[0], 60, center[2])
                scene = setup(camera_pos, target, 'day')
                scene.camera.data.clip_end = 14000
                ground = bpy.data.objects['Review ground plane, not game terrain']; ground.scale *= max(1, span / 500)
            else:
                all_bounds = [ob.matrix_world @ Vector(corner) for ob in bpy.context.scene.objects if ob.type == 'MESH' for corner in ob.bound_box]
                max_y = max(v.z for v in all_bounds); width = max(data['footprint'])
                d = max(35, max_y * 1.7 + width * .4)
                target = (0, max_y * .43, 0)
                camera_pos = (d * .65 * 1.25, target[1] + (max_y * .58 + 8 - target[1]) * 1.25, -d * .9 * 1.25)
                scene = setup(camera_pos, target, 'day')
            if not name.endswith('_flight'):
                camera_pos = fit_source_camera(scene, target, padding=1.4 if name == 'city_all' else 1.15)
            count = len(data['parts']); site = data['id']; mood = 'day'; before = False
            provenance = dict(data); provenance['camera_unity'] = camera_pos; provenance['target_unity'] = target
            provenance['layout_sha256'] = hashlib.sha256(layout_path.read_bytes()).hexdigest()
        elif name == 'kit_sheet':
            # A source gallery must not retain the preceding whole-city export as orphan data.
            model_cache.clear(); material_cache.clear()
            bpy.data.orphans_purge(do_recursive=True)
            kit_manifest_path = ART / 'Models/model_inventory.json'
            kit_data = json.loads(kit_manifest_path.read_text())
            items = kit_data.get('models', kit_data.get('assets', []))
            ids = []
            for item in items:
                mid = item.get('id', item.get('model', item.get('name', '')))
                mid = mid.removesuffix('_high').removesuffix('_low')
                if mid and mid not in ids:
                    ids.append(mid)
            if not ids:
                ids = sorted(p.stem.removesuffix('_high') for p in (ART / 'Meshes').glob('*_high.bytes'))
            for i, mid in enumerate(ids):
                rows = cmh(ART / 'Meshes' / (mid + '_high.bytes'))
                mins = [min(r[j] for r in rows) for j in range(3)]; maxs = [max(r[j] for r in rows) for j in range(3)]
                scale = min(1.0, 5 / max(maxs[j] - mins[j] for j in range(3)))
                col, row = i % 8, i // 8
                role = next((x.get('role', 'stone') for x in items if x.get('id', x.get('model', x.get('name', ''))).startswith(mid)), 'stone')
                add_part({'model': mid, 'role': role, 'position': [(col - 3.5) * 7, 0, row * 8], 'scale': [scale] * 3}, 0)
                text_curve = bpy.data.curves.new('Catalog label', 'FONT'); text_curve.body = mid.replace('_', ' '); text_curve.size = .62; text_curve.align_x = 'CENTER'
                text_obj = bpy.data.objects.new('Catalog label / ' + mid, text_curve); bpy.context.collection.objects.link(text_obj)
                text_obj.location = point(((col - 3.5) * 7, .04, row * 8 + 3.3))
                text_obj.data.materials.append(material(0, 'trim'))
            scene = setup((45, 65, -48), (0, 0, 20), 'day')
            scene.camera.data.type = 'ORTHO'; scene.camera.data.ortho_scale = 78
            scene.render.resolution_x = max(1800, args.width); scene.render.resolution_y = max(1200, args.height)
            count = len(ids); site = 'catalog'; mood = 'day'; before = False
            provenance = {'layout': 'Labeled catalog grid, not a runtime arrangement', 'models': ids}
        elif name == 'tree_lineup':
            species = [('cypress', -14, .85), ('willow', 0, 1), ('living', 16, .85)]
            for species_name, x, scale in species:
                for component, role in [('trunk', 'bark'), ('canopy', 'leaf')]:
                    add_part({'model':species_name + '_' + component, 'role':role, 'position':[x,0,0], 'scale':[scale]*3}, 2)
            scene = setup((0, 17, -64), (0, 7.5, 0), 'day')
            scene.camera.data.lens = 42
            count = 6; site = 'tree catalog'; mood = 'day'; before = False
            provenance = {'layout':'Labeled tree-species lineup, not a runtime arrangement', 'species_scales':species}
        elif name.startswith('sanctuary_'):
            layout_path = ART / 'Gardens/sanctuary.json'
            data = json.loads(layout_path.read_text()); site = 'sanctuary'; mood = 'day'; before = False
            for part in data['nodes']:
                add_part(part, 2)
            for x0, z0, x1, z1, y in [(-23, -4.5, 23, 4.5, .012), (-4.5, -23, 4.5, 23, .013)]:
                mesh_data = bpy.data.meshes.new('Runtime garden paving')
                mesh_data.from_pydata([point((x0,y,z0)),point((x0,y,z1)),point((x1,y,z1)),point((x1,y,z0))], [], [(0,1,2,3)])
                mesh_data.update(); uv = mesh_data.uv_layers.new(name='UVMap')
                for i, loop in enumerate(uv.data):
                    loop.uv = [(x0,z0),(x0,z1),(x1,z1),(x1,z0)][i]
                paving = bpy.data.objects.new('Exact runtime cross path', mesh_data); bpy.context.collection.objects.link(paving); paving.data.materials.append(material(2,'paving'))
            camera_pos, target = ((42, 37, -53),(0, 7, 0)) if name.endswith('flight') else ((-8, 3.1, -37),(0, 7, 6))
            scene = setup(camera_pos, target, 'day'); count = len(data['nodes']) + 2
            provenance = {'layout':str(layout_path.relative_to(ROOT)), 'layout_sha256':hashlib.sha256(layout_path.read_bytes()).hexdigest(), 'camera_unity':camera_pos, 'target_unity':target, 'omitted':'Existing animated drake not exported; no substitute.'}
        else:
            site, camera_pos, target, mood, before = VIEWS[name]
            baseline_data = json.loads((CACHE / 'baseline' / (site + '.json')).read_text())
            data = baseline_data if before else json.loads((ART / 'Layouts' / (site + '.json')).read_text())
            style = STYLE[site]
            for part in baseline_data['baseParts']:
                add_part(part, style, '02I', before)
            for part in data['parts'] + ([] if before else baseline_data.get('socketParts', [])):
                add_part(part, style, '03A2' if before else '04A', before)
            scene = setup(camera_pos, target, mood)
            count = len(data['parts']) + len(baseline_data['baseParts']) + (0 if before else len(baseline_data.get('socketParts', [])))
            if mood == 'night':
                # One approximate core light corresponding to StrongholdView's bounded core light.
                bpy.ops.object.light_add(type='POINT', location=point((0, 3.7, 5)))
                lamp = bpy.context.object; lamp.name = 'Approximate existing core light, offline intensity'
                lamp.data.energy = 170; lamp.data.color = old_tint(style, 'crystal'); lamp.data.shadow_soft_size = 1.1
            provenance = {'layout': str((ART / 'Layouts' / (site + '.json')).relative_to(ROOT)) if not before else 'Source CitadelLayout03A2.Build', 'layout_sha256': hashlib.sha256(json.dumps(data, sort_keys=True).encode()).hexdigest(), 'camera_unity': camera_pos, 'target_unity': target}
        scene.render.filepath = str(OUT / (name + '.png'))
        bpy.ops.render.render(write_still=True)
        if name == 'kit_sheet':
            scene['review_status'] = 'Offline asset gallery. Not a Unity scene or screenshot.'
            bpy.ops.file.pack_all()
            bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'visual_rebirth_04a_gallery.blend'), compress=True)
        manifest['views'][name] = {'file': name + '.png', 'site': site, 'mood': mood, 'parts': count, 'samples': args.samples, 'resolution': [scene.render.resolution_x, scene.render.resolution_y], 'before_03A2': before, 'provenance': provenance}
        manifest['geometry_sha256'] = dict(manifest.get('geometry_sha256', {}), **hashes)
        manifest['blender_version'] = bpy.app.version_string
        manifest['renderer'] = 'CPU Cycles, no denoising'
        manifest['material_manifest_sha256'] = hashlib.sha256((ART / 'material_manifest.json').read_bytes()).hexdigest()
        manifest['texture_sha256'] = {str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ART / 'Textures').glob('*.png'))}
        manifest['review_rig'] = {'camera_lens_mm':42,'day_sun_energy':2.1,'day_world_strength':.55,'night_sun_energy':.65,'night_world_strength':.35,'exposure_day':.5,'exposure_night':1.1,'ground_y':-.035,'color_management':'Blender AgX Medium High Contrast'}
        manifest['baseline_extraction'] = json.loads((CACHE / 'baseline/source_provenance.json').read_text())
        manifest['limitations'] = ['Offline Blender renders, not Unity screenshots.', 'Actual runtime mesh bytes and layout data; review ground plane, camera and lighting.', 'Blender Principled materials approximate Unity Standard or URP Lit response.', 'Fort and garden views omit their surrounding city. Whole-city views load ordinary-budget architecture only; roads, terrain, creatures, player, gameplay physics, post effects and performance are unverified.', 'Baseline geometry extracted from unchanged C# math using a recording adapter; this is not a Unity compile check.']
        (OUT / 'preview_manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


def resolve_material(manifest, style, role):
    """Resolve the exact shipping surface role and texture-family manifest."""
    for source in manifest.get('surfaces', []):
        if source['style'] == style and source['role'] == role:
            spec = dict(source)
            family = spec['family']
            spec['maps'] = {key: 'Textures/' + family + '_' + suffix + '.png'
                            for key, suffix in [('albedo', 'Albedo'), ('normal', 'Normal'), ('mask', 'Mask'), ('emission', 'Emission')]}
            spec['roughness'] = 1 - spec['smoothness']
            return spec
    return {}


if __name__ == '__main__':
    main()
