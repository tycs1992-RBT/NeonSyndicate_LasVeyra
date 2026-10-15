#!/usr/bin/env python3
"""Record actual existing body builders and pose solvers for offline source renders.

This executes unmodified game geometry/pose code with explicit arithmetic adapters.
It is not Unity compilation, animation import, live AI or gameplay validation.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'
HARNESS = ROOT / 'tools/living_territories_04b_harness'


def export(requests, output, dotnet):
    output.mkdir(parents=True, exist_ok=True)
    sources = []
    for folder, patterns in [('Characters', ['BodyBuilder*.cs', 'HumanoidPose*.cs', 'Appearance.cs', 'RigLayout.cs', 'CharacterPaletteData.cs', 'WeaponGrip.Table.cs', 'HoldType.cs', 'TravelTypes.cs']),
                             ('Creatures', ['CreatureBuilder*.cs', 'CreaturePose*.cs', 'CreatureTypes.cs', 'CreatureLooks.cs']),
                             ('Core', ['QMath.cs']), ('Combat', ['MeleeAnim.cs'])]:
        for pattern in patterns:
            sources.extend(sorted((RUNTIME / folder).glob(pattern)))
    source_hashes = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
    drake = RUNTIME / 'Creatures/CrystalDrakeSurface.cs'
    source_hashes[str(drake.relative_to(ROOT))] = hashlib.sha256(drake.read_bytes()).hexdigest()
    text = drake.read_text()
    begin = text.index('            // Palette indices do not change')
    end = text.index('            _palette = PaletteTexture', begin)
    drake_cells = text[begin:end]
    ecology = RUNTIME / 'Gameplay/Territories/TerritoryEcologyRules04B.cs'
    source_hashes[str(ecology.relative_to(ROOT))] = hashlib.sha256(ecology.read_bytes()).hexdigest()
    ecology_text = ecology.read_text()

    def method(marker):
        start = ecology_text.index(marker); opening = ecology_text.index('{', start)
        depth = 1; end = opening + 1
        while depth:
            depth += (ecology_text[end] == '{') - (ecology_text[end] == '}'); end += 1
        return ecology_text[start:end]

    helpers = 'using UnityEngine; namespace NeonSyndicate { public static class TerritoryEcologyRules04B {\n' + '\n'.join(method(marker) for marker in ('public static int Seed(', 'public static Appearance StaffLook(', 'public static Vector3 StaffOffset(', 'public static float StaffYaw(')) + '\n}}'
    with tempfile.TemporaryDirectory(prefix='living04b-actors-') as directory:
        scratch = Path(directory)
        program = (HARNESS / 'ActorExportProgram.cs').read_text().replace('DRAKE_CELLS', drake_cells)
        (scratch / 'Program.cs').write_text(program)
        (scratch / 'EcologyAppearance.cs').write_text(helpers)
        assembly = scratch / 'Actors.dll'
        refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
        compiler = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
        args = ['-nologo', '-target:exe', '-langversion:9', '-out:' + str(assembly)]
        args += ['-r:' + str(p) for p in refs]
        args += [str(p) for p in sources + [HARNESS / 'ActorMathAdapter.cs', scratch / 'Program.cs', scratch / 'EcologyAppearance.cs']]
        response = scratch / 'compile.rsp'; response.write_text('\n'.join('"' + a + '"' for a in args))
        subprocess.run([str(dotnet), str(compiler), '@' + str(response)], check=True)
        (scratch / 'Actors.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        subprocess.run([str(dotnet), str(assembly), str(requests.resolve()), str(output.resolve())], check=True)
    recorder_sources = [HARNESS / 'ActorMathAdapter.cs', HARNESS / 'ActorExportProgram.cs', Path(__file__).resolve()]
    source_hashes.update({str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in recorder_sources})
    provenance = {'method': 'Actual existing builders and pose solvers executed with arithmetic adapters. Static source poses, not Unity or live AI.',
                  'source_sha256': source_hashes, 'requests_sha256': hashlib.sha256(requests.read_bytes()).hexdigest()}
    (output / 'source_provenance.json').write_text(json.dumps(provenance, indent=2) + '\n')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--requests', type=Path, required=True)
    p.add_argument('--output', type=Path, default=ROOT / 'Docs/LivingTerritories04B/Previews/ActorSource')
    p.add_argument('--dotnet', type=Path, default=ROOT.parent / '.tools/dotnet/dotnet')
    a = p.parse_args(); export(a.requests, a.output, a.dotnet.resolve())


if __name__ == '__main__':
    main()
