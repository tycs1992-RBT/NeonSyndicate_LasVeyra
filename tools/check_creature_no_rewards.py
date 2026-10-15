#!/usr/bin/env python3
"""Execute the actual ReportDeath method against explicit recording host doubles.

This isolates routing logic, not Unity objects/AI. No production source is changed.
The same cases must also fail against the unrepaired 04A method, proving regression
sensitivity. Native CreatureNoRewardsTests remain a separate Unity requirement.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = 'Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.cs'
BASE = '1afbaa9b7e311f977219412a1fb345bf6857c5a6'
HOST = r'''
using System;
using System.Collections.Generic;
using System.Text.Json;
public struct Vector3 { }
public sealed class Transform { public Vector3 position; }
public class CharacterBase { public Vector3 ChestPosition; public Transform transform = new Transform(); }
public sealed class Creature : CharacterBase { public CreatureSpawnOptions Options; }
public sealed class NpcController : CharacterBase { public CreatureSpawnOptions CreatureOptions; }
public sealed class CreatureSpawnOptions { public bool NoRewards; }
public sealed class CreatureSpecies { public string Id = "test"; public bool GreatBeast, Boss; }
public struct DamageInfo { public bool Headshot; }
public struct CreatureKill { public CharacterBase Body; public CreatureSpecies Species; public bool ByPlayer; public Vector3 Position; public string Tag; }
public enum StatId { Kills, Headshots }
public static class Log { public static void Exception(string label, Exception error) { throw error; } }
public sealed class RecordingServices
{
    public void RecordKill(string id) { Game.Rewards++; }
    public void Add(StatId id, int value) { Game.Rewards++; }
    public void OnKill() { Game.Rewards++; }
    public void OnCreatureKilled(CreatureSpecies species, bool elite, Vector3 at) { Game.Rewards++; }
}
public static class Game
{
    public static int Rewards;
    public static RecordingServices Compendium = new RecordingServices(), Stats = new RecordingServices(),
        Abilities = new RecordingServices(), Gear = new RecordingServices(), Loot = new RecordingServices();
}
public static class CreatureParts { public static void OnOwnerKilled(CharacterBase body, CreatureSpecies species, Vector3 at) { Game.Rewards++; } }
public sealed class CreatureDirector
{
    public int Notices, BossBookkeeping;
    private static string TagOf(CharacterBase body) { return "tag"; }
    private static bool IsEliteBody(CharacterBase body) { return false; }
    private static void GiveLoot(CreatureSpecies sp, bool elite, Vector3 at) { Game.Rewards++; }
    private static void GiveLumen(CreatureSpecies sp, bool elite, Vector3 at) { Game.Rewards++; }
    private static void GearDrop(Vector3 at, float chance, bool boss) { Game.Rewards++; }
    private void OnBossDied(CharacterBase body, CreatureSpecies sp, bool byPlayer) { BossBookkeeping++; }
    private void RaiseKilled(CreatureKill notice) { Notices++; }
    ACTUAL_REPORT_DEATH
}
public static class Program
{
    public static int Main()
    {
        List<object> rows = new List<object>();
        int failures = 0;
        foreach (bool humanoid in new[] { false, true })
        foreach (int option in new[] { -1, 0, 1 })
        foreach (bool byPlayer in new[] { false, true })
        {
            CreatureSpawnOptions options = option < 0 ? null : new CreatureSpawnOptions { NoRewards = option == 1 };
            CharacterBase body = humanoid ? (CharacterBase)new NpcController { CreatureOptions = options } :
                new Creature { Options = options };
            Game.Rewards = 0;
            CreatureDirector director = new CreatureDirector();
            director.ReportDeath(body, new CreatureSpecies(), byPlayer, default(DamageInfo));
            bool suppressed = option == 1;
            bool pass = director.Notices == (suppressed ? 0 : 1) && director.BossBookkeeping == (suppressed ? 0 : 1) &&
                (Game.Rewards > 0) == (!suppressed && byPlayer);
            if (!pass) failures++;
            rows.Add(new { humanoid, option, byPlayer, rewards = Game.Rewards, notices = director.Notices,
                bossBookkeeping = director.BossBookkeeping, passed = pass });
        }
        Console.Write(JsonSerializer.Serialize(new { cases = rows, failures }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
'''


def method(text):
    begin = text.index('        public void ReportDeath(')
    end = text.index('        private void GiveLoot(', begin)
    return text[begin:end]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    texts = {'baseline': subprocess.check_output(['git','show',BASE+':'+SOURCE],cwd=ROOT,text=True),
             'working': (ROOT/SOURCE).read_text()}
    results = {}
    with tempfile.TemporaryDirectory(prefix='04b-norewards-') as directory:
        scratch = Path(directory)
        for name, source in texts.items():
            actual = method(source)
            cs = scratch/(name+'.cs'); dll = scratch/(name+'.dll')
            cs.write_text(HOST.replace('ACTUAL_REPORT_DEATH', actual))
            options = ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]
            options += ['-r:'+str(p) for p in refs] + [str(cs)]
            rsp = scratch/(name+'.rsp'); rsp.write_text('\n'.join('"'+s+'"' for s in options))
            subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],check=True,capture_output=True,text=True)
            dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0',
                'framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
            run = subprocess.run([str(dotnet),str(dll)],check=True,capture_output=True,text=True)
            results[name] = json.loads(run.stdout)
            results[name]['method_sha256'] = hashlib.sha256(actual.encode()).hexdigest()
    report = {'scope':'Actual unmodified ReportDeath method executed with explicit recording doubles for native hosts and reward services. No Unity objects, native runtime, physics or gameplay tested.',
              'baseline_sha':BASE,'source':SOURCE,'results':results,'unity_regression_tests':'NOT RUN'}
    args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'baseline_failures':results['baseline']['failures'],'working_failures':results['working']['failures'],
        'working_cases':len(results['working']['cases'])},indent=2))
    return 0 if results['baseline']['failures'] == 2 and results['working']['failures'] == 0 else 1


if __name__ == '__main__':
    raise SystemExit(main())
