#!/usr/bin/env python3
"""Run real strict event validation and pure NUnit event tests without native Unity.

Compiles unmodified QuestJson, QuestData, QuestCatalog, EventSelector, rules and
NUnit test files, plus exact extracted event-validator methods. Reference IDs
come from the checked-in species/material/faction definitions. This does not
execute event coroutines, physics, JsonUtility, native Unity or whole-quest QA.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'

def block(source, marker):
    start = source.index(marker)
    begin = source.index('{', start)
    depth = 1
    i = begin + 1
    # Selected C# blocks contain no braces in strings. Explicitly scoped extraction.
    while depth:
        depth += (source[i] == '{') - (source[i] == '}')
        i += 1
    return source[start:i]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', type=Path, required=True)
    parser.add_argument('--nunit', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    hashes = {}
    def read(path):
        hashes[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
        return path.read_text()
    species_source = read(RUNTIME / 'Creatures/CreatureSpecies.cs')
    species = []
    bosses = []
    for part in species_source.split('new CreatureSpecies')[1:]:
        name = re.search(r'Id\s*=\s*"([^"]+)"', part)
        if name:
            species.append(name[1])
            if re.search(r'\bBoss\s*=\s*true', part): bosses.append(name[1])
    material_source = read(RUNTIME / 'Gameplay/MaterialStore.cs')
    materials = re.findall(r'\bM\("([^"]+)"', material_source)
    faction_source = read(RUNTIME / 'Gameplay/Factions.cs')
    factions = re.findall(r'new FactionDef\s*\{\s*Id\s*=\s*"([^"]+)"', faction_source)
    references = {'species': species, 'bosses': bosses, 'materials': materials, 'factions': factions}
    with tempfile.TemporaryDirectory(prefix='territory04b-events-') as directory:
        scratch = Path(directory)
        sources = []
        def generated(name, text):
            target = scratch / name
            target.write_text(text)
            sources.append(target)
        paths = [RUNTIME / 'Gameplay/Quests/QuestData.cs', RUNTIME / 'Gameplay/Quests/QuestJson.cs',
                 RUNTIME / 'Gameplay/Quests/QuestCatalog.cs', RUNTIME / 'Gameplay/Events/EventSelector.cs',
                 RUNTIME / 'Gameplay/Events/TerritoryEventRules.cs',
                 ROOT / 'Assets/NeonSyndicate/Tests/EditMode/TerritoryEventTests.cs',
                 ROOT / 'Assets/NeonSyndicate/Tests/EditMode/WorldEventTests.cs',
                 ROOT / 'tools/visual_rebirth_04a_harness/MathAdapter.cs',
                 ROOT / 'tools/living_territories_04b_harness/EventValidationProgram.cs']
        for path in paths:
            read(path)
            sources.append(path)
        coverage_source = read(ROOT / 'Assets/NeonSyndicate/Tests/EditMode/QuestDataTests.cs')
        generated('RetainedEventCoverageTests.cs', 'using System; using System.Collections.Generic; using NUnit.Framework; namespace NeonSyndicate.Tests { public sealed class RetainedEventCoverageTests { [Test]\n' + block(coverage_source, 'public void EventTemplatesCoverEveryDistrictAndTheInnerWorld()') + '}}')
        validator = read(RUNTIME / 'Gameplay/Quests/QuestValidator.cs')
        methods = ['public static void ValidateEvents(', 'private static void ValidateEvent(',
                   'private static void CheckEventSpecies(', 'private static void CheckReward(',
                   'public static void CheckText(', 'private static void CheckUnusedFields(',
                   'public static bool IsSet(', 'private static void Range(string w, string label, float',
                   'private static void Range(string w, string label, int', 'private static bool NamedPlace(',
                   'private static IEnumerable<string> AllPlaces(', 'private static bool IsId(',
                   'private static string At(', 'private static string Hint(', 'private static List<string> Sorted(']
        extracted = '\n'.join(block(validator, marker) for marker in methods)
        fields = validator[validator.index('public sealed class QuestRefs'):validator.index('        /// <summary>Builds the reference sets')]
        report_class = block(validator, 'public sealed class QuestReport')
        generated('EventValidatorExtract.cs', 'using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text;\nnamespace NeonSyndicate {\n' + fields + '}\n' + report_class + '\npublic static class QuestValidator { private const int MaxObjective=110; private const int MaxLine=260;\n' + extracted + '\n}}')
        city_source = read(RUNTIME / 'World/CityLayout.cs')
        frost_source = read(RUNTIME / 'World/CityLayout.Frost.cs')
        types_source = read(RUNTIME / 'World/CityTypes.cs')
        atmosphere_source = read(RUNTIME / 'World/Atmosphere.cs')
        places_source = read(RUNTIME / 'Gameplay/Quests/QuestPlaces.cs')
        util_source = read(RUNTIME / 'Core/Util.cs')
        generated('CatalogExtract.cs', 'using System; using System.Collections.Generic; using UnityEngine;\nnamespace NeonSyndicate {\n' +
                  block(types_source, 'public enum District') + '\n' + block(atmosphere_source, 'public enum Weather') + '\n' +
                  block(species_source, 'public static class Habitat') + '\n' + block(util_source, 'public sealed class GRand') + '\n' +
                  'public sealed class CityLayout { ' + block(city_source, 'public static string DistrictName') + '\n' +
                  block(frost_source, 'public static bool IsWilderness') + '}\n' +
                  'public static class QuestPlaces { public const string SitePrefix="Site:"; ' + block(places_source, 'public static bool TryDistrict') + '}\n}')
        refpath = scratch / 'references.json'
        refpath.write_text(json.dumps(references))
        dll = scratch / 'Events.dll'
        rsp = scratch / 'compile.rsp'
        options = ['-nologo', '-target:exe', '-langversion:9', '-out:' + str(dll), '-r:' + str(args.nunit.resolve())]
        options += ['-r:' + str(path) for path in refs] + [str(path) for path in sources]
        rsp.write_text('\n'.join('"' + option + '"' for option in options))
        subprocess.run([str(dotnet), str(csc), '@' + str(rsp)], check=True)
        shutil.copy2(args.nunit, scratch / 'nunit.framework.dll')
        dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        run = subprocess.run([str(dotnet), str(dll), str(ROOT / 'Assets/NeonSyndicate/Resources'), str(refpath)], capture_output=True, text=True)
        if run.returncode not in (0, 1): raise RuntimeError(run.stderr)
        report = json.loads(run.stdout)
        report['source_sha256'] = hashes
        report['data_sha256'] = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT / 'Assets/NeonSyndicate/Resources/Events').glob('*.json'))}
        report['scope'] = 'Actual QuestJson plus exact QuestValidator event-method extraction; six new data files; actual NUnit pure rule/selection tests; native event execution NOT RUN.'
        report['reference_counts'] = {key: len(value) for key, value in references.items()}
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({key: report[key] for key in ['events', 'strict_errors', 'warnings', 'nunit_passed', 'nunit_failures']}, indent=2))
        return run.returncode

if __name__ == '__main__':
    raise SystemExit(main())
