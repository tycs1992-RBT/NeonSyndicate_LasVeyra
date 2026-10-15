#!/usr/bin/env python3
"""Check actual ecology rule methods, habitat data and conservative source geometry.

Rule methods execute unchanged in .NET with the actual Weather enum and JSON slot
schema. This does not emulate Unity objects, physiology, physics, AI or input.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile
import numpy as np
from check_visual_rebirth_04a import intersects_box, transform_vertices

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'Assets/NeonSyndicate/Resources'
RULES = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryEcologyRules04B.cs'
LAYOUTS = {'neutral_sanctuary': ('LivingTerritories04B', 'Layouts/habitat_sanctuary.json'),
           'canopy_observation': ('LivingTerritories04B', 'Layouts/habitat_canopy.json'),
           'rift_nesting': ('LivingTerritories04B', 'Layouts/habitat_rift.json'),
           'transitional_roaming': ('LivingTerritories04B', 'Layouts/habitat_transition.json'),
           'home_care': ('VisualRebirth04A', 'Gardens/sanctuary.json')}


def block(source, marker):
    begin = source.index(marker)
    pos = source.index('{', begin)
    depth, end = 1, pos + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[begin:end]


PROGRAM = r'''
public static class Program {
 static int passed, failed;
 static void Check(bool value) { if (value) passed++; else failed++; }
 public static void Main() {
  bool loaded=false;
  float[] path={260,171,169,172,169,200,254,255,256,200,171,169};
  bool[] expect={false,false,true,true,true,true,true,true,false,false,false,true};
  for(int i=0;i<path.Length;i++) { loaded=Rules.ShouldLoad(loaded,path[i]); Check(loaded==expect[i]); }
  for(int preset=0;preset<4;preset++) {
   int cap=Rules.CreatureCap(preset), limit=Rules.DirectorLimit(preset)-Rules.DirectorReserve;
   Check(Rules.CanAllocateCreature(preset,cap-1,limit-1));
   Check(!Rules.CanAllocateCreature(preset,cap,0));
   Check(!Rules.CanAllocateCreature(preset,0,limit));
   Check(!Rules.CanAllocateCreature(preset,0,limit+6));
   Check(Rules.StaffPerSite(preset,false)<=Rules.StaffCap(preset));
  }
  var seeds=new System.Collections.Generic.HashSet<int>();
  foreach(string id in new[]{"neutral_sanctuary","canopy_observation","rift_nesting","transitional_roaming","home_care"})
   for(int slot=0;slot<3;slot++) {
    int seed=Rules.Seed(61724,id,slot);Check(seed>0);Check(seeds.Add(seed));
    Check(seed==Rules.Seed(61724,id,slot));Check(seed!=Rules.Seed(61725,id,slot));
   }
  var s=new TerritoryHabitatSlot04B{day="sun_griffin",night="violet_wyvern",wet="sun_griffin",aerial=true,minimumPreset=1};
  Check(Rules.Species(s,12,(int)Weather.Clear)=="sun_griffin");
  Check(Rules.Species(s,23,(int)Weather.Clear)=="violet_wyvern");
  Check(Rules.Species(s,23,(int)Weather.Rain)=="sun_griffin");
  Check(!Rules.SlotEnabled(s,0,(int)Weather.Clear));Check(Rules.SlotEnabled(s,1,(int)Weather.Clear));
  Check(!Rules.SlotEnabled(s,3,(int)Weather.Storm));Check(!Rules.SlotEnabled(s,3,(int)Weather.Sandstorm));
  s.aerial=false;Check(Rules.SlotEnabled(s,3,(int)Weather.Storm));
  System.Console.Write(System.Text.Json.JsonSerializer.Serialize(new{passed,failed}));
 }
}
'''


def rules(dotnet):
    source = RULES.read_text()
    constants = '\n'.join(re.findall(r'public const (?:float|int) [^;]+;', source))
    names = ['CreatureCap', 'StaffCap', 'StaffPerSite', 'DirectorLimit', 'CanAllocateCreature', 'ShouldLoad', 'Night', 'SevereWeather', 'Species', 'SlotEnabled', 'Seed']
    methods = [block(source, re.search(r'public static \w+ ' + name + r'\(', source).group()) for name in names]
    weather = block((ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/Atmosphere.cs').read_text(), 'public enum Weather')
    slots = block(source, 'public sealed class TerritoryHabitatSlot04B')
    code = weather + '\n' + slots + '\npublic static class Rules{\n' + constants + '\n' + '\n'.join(methods) + '\n}\n' + PROGRAM
    code = 'using System;\n' + code
    compiler = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    with tempfile.TemporaryDirectory(prefix='04b-ecology-') as temp:
        scratch = Path(temp)
        path = scratch / 'Program.cs'; path.write_text(code)
        dll = scratch / 'Ecology.dll'
        args = ['-nologo', '-target:exe', '-langversion:9', '-out:' + str(dll)] + ['-r:' + str(p) for p in refs] + [str(path)]
        response = scratch / 'compile.rsp'; response.write_text('\n'.join('"' + a + '"' for a in args))
        subprocess.run([str(dotnet), str(compiler), '@' + str(response)], check=True, capture_output=True, text=True)
        (scratch / 'Ecology.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        result = json.loads(subprocess.check_output([str(dotnet), str(dll)], text=True))
    return dict(result, scope='Actual unmodified rule methods extracted from production source, executed as managed C#. No Unity objects, physics or live AI.', methods=names, sha256=hashlib.sha256(source.encode()).hexdigest())


def geometry():
    rows, sources = [], {}
    for habitat, (package, filename) in LAYOUTS.items():
        data_path = RES / 'LivingTerritories04B/Habitats' / (habitat + '.json')
        layout_path = RES / package / filename
        data, layout = json.loads(data_path.read_text()), json.loads(layout_path.read_text())
        for path in (data_path, layout_path): sources[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
        for lod in ('high', 'low'):
            solid = []
            for node in layout['nodes']:
                if not node['solid']: continue
                mesh_path = RES / ('VisualRebirth04A' if node.get('source', '04A') == '04A' else 'LivingTerritories04B') / 'Meshes' / (node['model'] + '_' + lod + '.bytes')
                raw = mesh_path.read_bytes(); sources[str(mesh_path.relative_to(ROOT))] = hashlib.sha256(raw).hexdigest()
                vertices = np.frombuffer(raw, '<f4', offset=8).reshape(-1, 8)[:, :3]
                solid.append((node, transform_vertices(vertices, node).reshape(-1, 3, 3)))
            for index, slot in enumerate(data['slots']):
                position = np.asarray(slot['position'], float)
                radius = slot['clearance']; half = radius + slot['roam']
                lower = position + [-half, .15, -half]
                upper = position + [half, radius * 2 + 1.5, half]
                hits = [dict(model=node['model'], position=node['position']) for node, triangles in solid if intersects_box(triangles, lower, upper)]
                rows.append(dict(habitat=habitat, slot=index, lod=lod, species=slot['day'], position=slot['position'], yaw=slot['yaw'], body_clearance=radius, roam=slot['roam'], solid_intersections=hits, passed=not hits))
    return dict(scope='Conservative body capsule bounding box expanded by authored roam/orbit radius against exact transformed authored solid source triangles. Does not prove native ground support, runtime collider behavior, actor avoidance or terrain placement.',
                cases=rows, passed=sum(row['passed'] for row in rows), failed=sum(not row['passed'] for row in rows), source_sha256=sources)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', type=Path, default=ROOT.parent / '.tools/dotnet/dotnet')
    parser.add_argument('--report', type=Path, default=ROOT / 'Docs/LivingTerritories04B/Validation/ecology_source_checks.json')
    args = parser.parse_args()
    report = dict(native_unity='NOT RUN', rules=rules(args.dotnet.resolve()), source_geometry=geometry())
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: {field: value[field] for field in ('passed', 'failed')} for key, value in report.items() if isinstance(value, dict)}))
    return 1 if report['rules']['failed'] or report['source_geometry']['failed'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
