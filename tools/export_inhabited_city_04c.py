#!/usr/bin/env python3
"""Execute actual 04C room expansion and 04A architecture for source-only review."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'
OLD = ROOT / 'tools/visual_rebirth_04a_harness'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--dotnet', type=Path, default=ROOT.parent / '.tools/dotnet/dotnet')
    p.add_argument('--venues', default='lofts,clinic,forge')
    p.add_argument('--output', type=Path, default=ROOT / 'Docs/InhabitedCity04C/Previews/SourceGeometry')
    args = p.parse_args(); args.output.mkdir(parents=True, exist_ok=True)
    hashes = {}

    def read(path):
        hashes[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
        return path.read_text()

    with tempfile.TemporaryDirectory(prefix='04c-source-geometry-') as directory:
        scratch = Path(directory); sources = []
        for path in sorted((RUNTIME / 'World').glob('CityLayout*.cs')):
            source = read(path)
            if path.name == 'CityLayout.Frost.cs':
                end = source.index('    public static class SnowMesher')
                source = source[:source.rfind('    /// <summary>', 0, end)] + '}\n'
            target = scratch / path.name; target.write_text(source); sources.append(target)
        source = read(RUNTIME / 'Core/Util.cs')
        target = scratch / 'UtilPure.cs'; target.write_text(source[:source.index('    public static class Log')] + '}\n'); sources.append(target)
        source = read(RUNTIME / 'Core/Mats.cs'); start = source.index('    public enum FacadeStyle'); end = source.index('    /// <summary>', start)
        target = scratch / 'Enums.cs'; target.write_text('namespace NeonSyndicate {\n' + source[start:end] + 'public enum CrystalCityStyle { AmethystWard, DawnstoneCourt, VerdantReach }\n}'); sources.append(target)
        source = read(RUNTIME / 'Core/MeshKit.cs')
        target = scratch / 'MeshBuilderPure.cs'; target.write_text(source[:source.index('        public Mesh ToMesh(string name)')] + '    }\n}\n'); sources.append(target)
        source = read(RUNTIME / 'World/CityBuilder.cs'); start = source.index('            GRand r = new GRand(bld.Seed);'); end = source.index('            // The optional art pass', start)
        target = scratch / 'OriginalGeometryProgram.cs'; target.write_text(read(OLD / 'GeometryProgram.cs').replace('  TIER_SETUP', source[start:end])); sources.append(target)
        paths = [RUNTIME / name for name in ['World/CityTypes.cs', 'ArtDistrict/CrystalCity02ECoverage.cs',
                 'ArtRebirth04A/VisualRebirth04ACityPlan.cs', 'ArtRebirth04A/VisualRebirth04AArchitecture.cs',
                 'ArtInhabitedCity04C/InhabitedCity04CComposition.cs']]
        paths += [OLD / 'MathAdapter.cs', OLD / 'GeometryAdapter.cs', ROOT / 'tools/inhabited_city_04c_harness/GeometryExportProgram.cs']
        for path in paths: read(path); sources.append(path)
        read(RUNTIME / 'ArtInhabitedCity04C/VenueView04C.cs')
        dotnet = args.dotnet.resolve(); csc = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
        refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
        assembly = scratch / 'Geometry.dll'; response = scratch / 'compile.rsp'
        options = ['-nologo', '-target:exe', '-langversion:9', '-main:InhabitedGeometryExportProgram', '-out:' + str(assembly)]
        options += ['-r:' + str(path) for path in refs] + [str(path) for path in sources]
        response.write_text('\n'.join('"' + item + '"' for item in options))
        subprocess.run([str(dotnet), str(csc), '@' + str(response)], check=True)
        assembly.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        art = ROOT / 'Assets/NeonSyndicate/Resources'
        report = ROOT / 'Docs/InhabitedCity04C/Validation/source_book_bindings.json'
        for venue in args.venues.split(','): read(art / 'InhabitedCity04C/Layouts' / (venue + '.json'))
        read(report); read(Path(__file__).resolve())
        subprocess.run([str(dotnet), str(assembly), str(art / 'VisualRebirth04A/Meshes'), str(art / 'InhabitedCity04C/Layouts'), str(report), str(args.output.resolve()), args.venues], check=True)
        provenance = {'scope': 'Actual source geometry extraction with disclosed managed math/material adapters. No native Unity or gameplay execution.',
                      'source_sha256': hashes, 'venues': args.venues.split(','),
                      'extractions': ['CityLayout excludes unrelated SnowMesher.', 'Exact GRand/MathUtil and FacadeStyle definitions.',
                                      'MeshBuilder geometry methods before native ToMesh.', 'Original CityBuilder tier setup; actual 04A Tier/Crown.',
                                      'Unchanged 04C Composition.Expand.', 'Upper adapter mirrors VenueView04C.Upper local building and invokes its actual Tier/Crown methods.']}
        (args.output / 'source_provenance.json').write_text(json.dumps(provenance, indent=2) + '\n')


if __name__ == '__main__': main()
