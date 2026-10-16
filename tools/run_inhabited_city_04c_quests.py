#!/usr/bin/env python3
"""Execute actual strict parser, extracted quest validator/graph methods and NUnit chapter tests offline.

The extraction retains the entire ValidateQuests/ValidateQuest/ValidateStep methods
and dependencies. Contract validation is a deliberate throwing guard: the selected
chapter has no contracts. POI, material, faction and species IDs come from source.
This does not run JsonUtility, quest coroutines, actors, room physics or native Unity.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
from run_living_territories_04b_events import block

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'

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
    species, bosses = [], []
    for part in species_source.split('new CreatureSpecies')[1:]:
        name = re.search(r'Id\s*=\s*"([^"]+)"', part)
        if name:
            species.append(name[1])
            if re.search(r'\bBoss\s*=\s*true', part): bosses.append(name[1])
    materials = re.findall(r'\bM\("([^"]+)"', read(RUNTIME / 'Gameplay/MaterialStore.cs'))
    factions = re.findall(r'new FactionDef\s*\{\s*Id\s*=\s*"([^"]+)"', read(RUNTIME / 'Gameplay/Factions.cs'))
    pois = re.findall(r'new PoiRequest\s*\{[^\n]*Name\s*=\s*"([^"]+)"', read(RUNTIME / 'World/CityLayout.Graph.cs'))
    references = dict(species=species, bosses=bosses, materials=materials, factions=factions, pois=pois)
    with tempfile.TemporaryDirectory(prefix='inhabited04c-quests-') as directory:
        scratch = Path(directory)
        sources = []
        def generated(name, content):
            path = scratch / name; path.write_text(content); sources.append(path)
        for relative in ['Gameplay/Quests/QuestData.cs', 'Gameplay/Quests/QuestJson.cs', 'Gameplay/Quests/QuestCatalog.cs',
                         'Gameplay/InhabitedCity/QuietSignal04C.cs']:
            path = RUNTIME / relative; read(path); sources.append(path)
        for relative in ['Assets/NeonSyndicate/Tests/EditMode/QuietSignal04CTests.cs',
                         'tools/inhabited_city_04c_harness/QuestValidationProgram.cs']:
            path = ROOT / relative; read(path); sources.append(path)
        retained = read(ROOT / 'Assets/NeonSyndicate/Tests/EditMode/QuestDataTests.cs')
        generated('RetainedGuildChainTests.cs', 'using System; using System.Collections.Generic; using NUnit.Framework; namespace NeonSyndicate.Tests { public sealed class RetainedGuildChainTests {\n'
                  + re.search(r'private static readonly string\[\] Guilds = [^;]+;', retained)[0] + '\n'
                  + block(retained, 'private static List<KeyValuePair<string, QuestFile>> ShippedQuests(') + '\n[Test]\n'
                  + block(retained, 'public void EveryGuildHasAnArcThatOpensAfterTheFirstStoryMission(') + '\n}}')
        validator = read(RUNTIME / 'Gameplay/Quests/QuestValidator.cs')
        methods = ['public static void ValidateQuests(', 'private static void ValidateQuest(', 'private static void ValidateStep(',
                   'private static void CheckPrerequisiteCycles(', 'private static bool Cycle(', 'private static void CheckArcs(',
                   'private static void CheckReward(', 'public static void CheckText(', 'private static void CheckUnusedFields(',
                   'public static bool IsSet(', 'private static void Range(string w, string label, float',
                   'private static void Range(string w, string label, int', 'private static void CheckOffset(',
                   'private static void CheckWorldPoint(', 'private static bool PlaceOk(', 'private static bool NamedPlace(',
                   'private static IEnumerable<string> AllPlaces(', 'private static bool IsId(', 'private static string At(',
                   'private static string Hint(', 'private static List<string> Sorted(']
        fields = validator[validator.index('public sealed class QuestRefs'):validator.index('        /// <summary>Builds the reference sets')]
        generated('QuestValidatorExtract.cs', 'using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text;\nnamespace NeonSyndicate {\n'
                  + fields + '}\n' + block(validator, 'public sealed class QuestReport')
                  + '\npublic static class QuestValidator { private const int MaxObjective=110; private const int MaxLine=260;\n'
                  + '\n'.join(block(validator, marker) for marker in methods)
                  + '\nprivate static void ValidateContract(string file, ContractDef c, Dictionary<string,QuestDef> quests, HashSet<string> givers, QuestRefs refs, QuestReport rep, QuestJsonTrace trace) { throw new NotSupportedException("This chapter-only harness does not validate contracts."); }\n}}')
        logic = read(RUNTIME / 'Gameplay/Quests/QuestLogic.cs')
        graph = ['public static int PhaseIndex(', 'public static int NextPhase(', 'public static List<int> Successors(',
                 'public static bool[] Reachable(', 'public static bool CanFinishFrom(']
        generated('QuestGraphExtract.cs', 'using System; using System.Collections.Generic; namespace NeonSyndicate { public static class QuestLogic {\n'
                  + '\n'.join(block(logic, marker) for marker in graph) + '\n}}')
        city = read(RUNTIME / 'World/CityLayout.cs'); frost = read(RUNTIME / 'World/CityLayout.Frost.cs')
        types = read(RUNTIME / 'World/CityTypes.cs'); places = read(RUNTIME / 'Gameplay/Quests/QuestPlaces.cs')
        atmo = read(RUNTIME / 'World/Atmosphere.cs'); gear = read(RUNTIME / 'Gameplay/Gear.cs')
        loot = read(RUNTIME / 'Gameplay/LootTypes.cs'); lootrules = read(RUNTIME / 'Gameplay/LootWorldRules.cs')
        catalog = 'using System; using System.Collections.Generic; namespace NeonSyndicate {\n'
        catalog += block(types, 'public enum District') + '\n' + block(atmo, 'public enum Weather') + '\n' + block(species_source, 'public static class Habitat')
        catalog += '\npublic sealed class CityLayout { ' + re.search(r'public const float MapHalf = [^;]+;', city)[0] + '\n' + block(city, 'public static string DistrictName') + '\n' + block(frost, 'public static bool IsWilderness') + '}\n'
        catalog += 'public static class QuestPlaces { public const string SitePrefix="Site:"; public const string DistrictPrefix="District:"; ' + block(places, 'public static bool TryDistrict') + '}\n'
        for marker in ['public enum GearSlot', 'public enum Rarity', 'public enum Element']: catalog += block(gear, marker) + '\n'
        for marker in ['public enum LootArchetype', 'public enum LootMaker', 'public enum LootTrait', 'public sealed class LootUniqueDef', 'public static class LootUniques']: catalog += block(loot, marker) + '\n'
        catalog += 'public static class LootWorldRules { ' + block(lootrules, 'public static int UniqueByName') + '}\n}'
        # These selected validator methods do not invoke Unity. The original using directive needs the namespace only.
        generated('ReferenceCatalogExtract.cs', catalog + '\nnamespace UnityEngine { }\n')
        refpath = scratch / 'references.json'; refpath.write_text(json.dumps(references))
        dll = scratch / 'Chapter.dll'; rsp = scratch / 'compile.rsp'
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
        report['data_sha256'] = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT / 'Assets/NeonSyndicate/Resources/Quests').glob('*.json'))}
        report['scope'] = 'Actual strict QuestJson and exact QuestValidator quest-method/QuestLogic phase-graph extraction; chapter-only validation; NUnit chapter tests. Contract validation deliberately throws if reached. Native Unity, JsonUtility, rooms, coroutines and combat NOT RUN.'
        report['reference_counts'] = {key: len(value) for key, value in references.items()}
        args.report.parent.mkdir(parents=True, exist_ok=True); args.report.write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({key: report[key] for key in ['quests','phases','steps','strict_errors','warnings','nunit_passed','nunit_failures']}, indent=2))
        return run.returncode

if __name__ == '__main__':
    raise SystemExit(main())
