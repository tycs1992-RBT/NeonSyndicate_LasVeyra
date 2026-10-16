#!/usr/bin/env python3
"""Record actual 04C resident appearance and existing body/idle-pose builders offline.

This records static source poses at authored stations, not spawned Unity residents
or proof of admission, physics, animation playback or gameplay.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import numpy as np
from PIL import Image
from check_inhabited_residents_04c import block

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'
OUT = ROOT / 'Docs/InhabitedCity04C/Previews/ActorSource'


def main():
    dotnet = ROOT.parent / '.tools/dotnet/dotnet'
    old = ROOT / 'tools/living_territories_04b_harness'
    OUT.mkdir(parents=True, exist_ok=True)
    sources = []
    for folder, patterns in [('Characters', ['BodyBuilder*.cs', 'HumanoidPose*.cs', 'Appearance.cs', 'RigLayout.cs', 'CharacterPaletteData.cs', 'WeaponGrip.Table.cs', 'HoldType.cs', 'TravelTypes.cs']),
                             ('Creatures', ['CreatureBuilder*.cs', 'CreaturePose*.cs', 'CreatureTypes.cs', 'CreatureLooks.cs']),
                             ('Core', ['QMath.cs']), ('Combat', ['MeleeAnim.cs'])]:
        for pattern in patterns: sources.extend(sorted((RUNTIME / folder).glob(pattern)))
    resident = RUNTIME / 'Gameplay/InhabitedCity/InhabitedResidents04C.cs'
    rules = RUNTIME / 'Gameplay/InhabitedCity/InhabitedResidentRules04C.cs'
    requests = [{'id': kind+'_'+str(slot), 'kind': 'human', 'siteId': kind, 'region': kind,
                 'worldSeed': 61724, 'slotIndex': slot, 'staffIndex': slot, 'detail': 1, 'time': 2}
                for kind in ['lofts', 'clinic', 'forge', 'inn', 'archive', 'observatory', 'garage'] for slot in [0, 1]]
    request_path = OUT / 'requests.json'; request_path.write_text(json.dumps(requests, indent=2)+'\n')
    original = (old / 'ActorExportProgram.cs').read_text()
    seed = 'TerritoryEcologyRules04B.Seed(request.worldSeed, request.siteId, request.slotIndex)'
    look = 'string.IsNullOrEmpty(request.region) ? Appearance.Random(new System.Random(request.seed)) : TerritoryEcologyRules04B.StaffLook(request.region, request.seed)'
    if seed not in original or look not in original: raise ValueError('Retained actor exporter changed; review adapters.')
    program = original.replace(seed, 'InhabitedResidentRules04C.Seed(request.worldSeed, request.siteId, request.slotIndex)')
    program = program.replace(look, 'InhabitedResidents04C.Look(request.region, request.seed, request.staffIndex)')
    program = program.replace('TerritoryEcologyRules04B.StaffOffset(request.staffIndex, request.hearth)', 'Vector3.zero')
    program = program.replace('TerritoryEcologyRules04B.StaffYaw(request.staffIndex)', '0f').replace('DRAKE_CELLS', '')
    # Creature branches remain compilable but this export requests only human residents.
    helper = 'using UnityEngine; namespace NeonSyndicate { public static class InhabitedResidents04C {\n'+block(resident.read_text(), 'public static Appearance Look(')+'\n}}'
    with tempfile.TemporaryDirectory(prefix='04c-resident-poses-') as directory:
        scratch = Path(directory)
        p = scratch / 'Program.cs'; p.write_text(program)
        h = scratch / 'Appearance.cs'; h.write_text(helper)
        assembly = scratch / 'Actors.dll'
        refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
        compiler = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
        args = ['-nologo', '-target:exe', '-langversion:9', '-out:'+str(assembly)]
        args += ['-r:'+str(p) for p in refs] + [str(p) for p in sources+[old/'ActorMathAdapter.cs', p, h, rules]]
        response = scratch / 'compile.rsp'; response.write_text('\n'.join('"'+arg+'"' for arg in args))
        subprocess.run([str(dotnet), str(compiler), '@'+str(response)], check=True)
        assembly.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        subprocess.run([str(dotnet), str(assembly), str(request_path), str(OUT)], check=True)
    data = json.loads((OUT/'character_palette.json').read_text())
    for channel in ['albedo', 'properties', 'emission']:
        pixels = np.clip(np.array(data[channel]).reshape(16,16,4)*255,0,255).astype('uint8')
        Image.fromarray(pixels[::-1]).save(OUT/('character_'+channel+'.png'))
    inputs = sources+[resident, rules, old/'ActorMathAdapter.cs', old/'ActorExportProgram.cs', Path(__file__).resolve()]
    report = {'scope': __doc__.strip(), 'source_sha256': {str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs},
              'requests': requests, 'detail': 'Actual ambient standard detail 1 used by Medium/High/Ultra. Static idle frame only.',
              'adapter_changes': ['Existing recorder uses actual 04C Seed and Look instead of 04B staff appearance.', 'Unused 04B staff offsets/yaw replaced by zero; rendering uses authored 04C anchors.', 'No creature request; unused special drake palette edit omitted.'],
              'native_residents': 'NOT RUN'}
    (OUT/'source_provenance.json').write_text(json.dumps(report,indent=2)+'\n')


if __name__ == '__main__': main()
