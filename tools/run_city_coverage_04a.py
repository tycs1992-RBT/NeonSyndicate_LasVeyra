#!/usr/bin/env python3
"""Execute the real CityLayout and 04A architecture CPU source with explicit recording adapters.

This is supplementary offline coverage and geometry evidence, not Unity compilation,
physics, imports, rendering, EditMode tests or performance. Only plain mathematical
Vector/Quaternion APIs, materials and resource reads are adapted. Source generator,
04A composition and MeshBuilder primitive formulas are copied without changes.

Requires a .NET 8 SDK. Example:
  python3 tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'
HARNESS = ROOT / 'tools/visual_rebirth_04a_harness'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', type=Path, default=ROOT.parent / '.tools/dotnet/dotnet')
    parser.add_argument('--output', type=Path, default=ROOT / 'Docs/VisualRebirth04A/Reports')
    parser.add_argument('--geometry', action='store_true')
    parser.add_argument('--examples-only', action='store_true', help='Refresh four source geometry previews without the full-city count and invariants.')
    parser.add_argument('--whole-city', type=Path, help='Export the full city architecture to this external scratch directory (roughly 300 MB).')
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    if not dotnet.is_file():
        raise SystemExit('Supply --dotnet with an installed .NET 8 SDK executable.')
    compilers = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))
    refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    if not compilers or not refs:
        raise SystemExit('The supplied SDK needs the .NET 8 reference pack and Roslyn compiler.')
    args.output.mkdir(parents=True, exist_ok=True)
    source_hashes = {}

    def read(path):
        source_hashes[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
        return path.read_text()

    with tempfile.TemporaryDirectory(prefix='visual04a-city-') as directory:
        scratch = Path(directory)
        sources = []
        for path in sorted((RUNTIME / 'World').glob('CityLayout*.cs')):
            text = read(path)
            if path.name == 'CityLayout.Frost.cs':
                # SnowMesher is a separate class after CityLayout, not part of layout generation.
                boundary = text.index('    public static class SnowMesher')
                text = text[:text.rfind('    /// <summary>', 0, boundary)] + '}\n'
            target = scratch / path.name
            target.write_text(text)
            sources.append(target)
        util = read(RUNTIME / 'Core/Util.cs')
        (scratch / 'UtilPure.cs').write_text(util[:util.index('    public static class Log')] + '}\n')
        sources.append(scratch / 'UtilPure.cs')
        mats = read(RUNTIME / 'Core/Mats.cs')
        begin = mats.index('    public enum FacadeStyle')
        end = mats.index('    /// <summary>', begin)
        (scratch / 'Enums.cs').write_text('namespace NeonSyndicate {\n' + mats[begin:end] +
            'public enum CrystalCityStyle { AmethystWard, DawnstoneCourt, VerdantReach }\n}')
        sources.append(scratch / 'Enums.cs')
        for relative in ('World/CityTypes.cs', 'ArtDistrict/CrystalCity02ECoverage.cs', 'ArtRebirth04A/VisualRebirth04ACityPlan.cs'):
            path = RUNTIME / relative
            read(path)
            sources.append(path)
        sources.append(HARNESS / 'MathAdapter.cs')

        def execute(name, extra, arguments):
            assembly = scratch / (name + '.dll')
            response = scratch / (name + '.rsp')
            options = ['-nologo', '-target:exe', '-langversion:9', '-out:' + str(assembly)]
            options += ['-r:' + str(path) for path in refs] + [str(path) for path in sources + extra]
            response.write_text('\n'.join('"' + item + '"' for item in options))
            subprocess.run([str(dotnet), str(compilers[-1]), '@' + str(response)], check=True)
            (scratch / (name + '.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions': {
                'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
            completed = subprocess.run([str(dotnet), str(assembly)] + arguments, capture_output=True, text=True, check=True)
            if completed.stderr:
                print(completed.stderr)
            return json.loads(completed.stdout)

        coverage = execute('coverage', [HARNESS / 'CoverageProgram.cs'], [])
        tree_triangles = {}
        for model, count in coverage['plannedTreeModels'].items():
            resource = ROOT / 'Assets/NeonSyndicate/Resources/VisualRebirth04A/Meshes' / (model + '_low.bytes')
            payload = resource.read_bytes()
            leaf_triangles = struct.unpack_from('<i', payload, 4)[0] // 3
            tree_triangles[model] = {'instances': count, 'canopy_triangles_each': leaf_triangles,
                'retained_trunk_and_branches_triangles_each': 44, 'total_triangles': count * (leaf_triangles + 44),
                'mesh_sha256': hashlib.sha256(payload).hexdigest()}
        coverage['planned_tree_geometry'] = tree_triangles
        coverage['source_sha256'] = dict(source_hashes)
        coverage['adapter_sha256'] = {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in HARNESS.glob('*.cs')}
        (args.output / 'planned_city_coverage.json').write_text(json.dumps(coverage, indent=2) + '\n')
        print(json.dumps({key: value for key, value in coverage.items() if key not in
            ('buildings', 'byDistrictAndArchetype', 'source_sha256', 'adapter_sha256')}, indent=2))
        if args.geometry:
            mesh = read(RUNTIME / 'Core/MeshKit.cs')
            (scratch / 'MeshBuilderPure.cs').write_text(mesh[:mesh.index('        public Mesh ToMesh(string name)')] + '    }\n}\n')
            city = read(RUNTIME / 'World/CityBuilder.cs')
            begin = city.index('            GRand r = new GRand(bld.Seed);')
            end = city.index('            // The optional art pass', begin)
            program = (HARNESS / 'GeometryProgram.cs').read_text().replace('  TIER_SETUP', city[begin:end])
            (scratch / 'GeometryProgram.cs').write_text(program)
            composition = RUNTIME / 'ArtRebirth04A/VisualRebirth04AArchitecture.cs'
            read(composition)
            extra = [HARNESS / 'GeometryAdapter.cs', scratch / 'MeshBuilderPure.cs', scratch / 'GeometryProgram.cs', composition]
            geometry = execute('geometry', extra, [str(ROOT / 'Assets/NeonSyndicate/Resources/VisualRebirth04A/Meshes'),
                str(args.output / 'CityGeometry')] + (['all', str(args.whole_city.resolve())] if args.whole_city else (['examples'] if args.examples_only else [])))
            geometry['source_sha256'] = source_hashes
            if not args.examples_only:
                (args.output / 'city_geometry_counts.json').write_text(json.dumps(geometry, indent=2) + '\n')
            print(json.dumps({key: value for key, value in geometry.items() if key not in ('counts', 'examples', 'source_sha256')}, indent=2))
            if any(geometry.get(key, 0) for key in ('invalidVertices', 'degenerateTriangles', 'opposedVertexNormals', 'envelopeViolations', 'landingViolations')):
                return 1
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
