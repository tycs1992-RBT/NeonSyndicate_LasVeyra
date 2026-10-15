#!/usr/bin/env python3
"""Execute actual ecology scheduling and teardown methods with recording host doubles.

Actor allocation is suppressed here. These checks prove branch/timer/cleanup routing,
not native Unity destruction, scene lifetimes, creature AI, collisions or body workers.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryEcology04B.cs'
RULES = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryEcologyRules04B.cs'


def block(source, marker):
    start = source.index(marker); end = source.index('{', start) + 1; depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}'); end += 1
    return source[start:end]


HOST = r'''
using System;
using System.Collections.Generic;
using System.Text.Json;
public struct Vector3 {
 public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
 public static float Distance(Vector3 a,Vector3 b) { return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z)); }
}
public static class Mathf { public static float Min(float a,float b){return Math.Min(a,b);} }
public static class Time { public static float time; }
public sealed class GameObject { public bool activeInHierarchy=true,Destroyed; public void SetActive(bool active){activeInHierarchy=active;} }
public sealed class Transform { public GameObject gameObject=new GameObject(); }
public sealed class Creature { public bool Ready=true,IsDead; public float SpawnTime; public GameObject gameObject=new GameObject(); }
public sealed class NpcController { public bool IsDead; public Action<NpcController> OnScriptArrived; public GameObject gameObject=new GameObject(); }
public sealed class TerritoryHabitat04B { public string id="test"; public object[] slots=new object[3]; }
public enum GameState { Loading,Title,Playing,Paused,Wasted,Busted,Cutscene }
public static class GameTime { public static bool Paused; }
public sealed class Player { public bool IsDead; }
public sealed class Inner { public bool IsInner; }
public sealed class Missions { public object Active; }
public sealed class Peds { public bool Suppressed; }
public sealed class Director { public int Removed; public void DespawnTag(string tag){Removed++;} }
public static class Game {
 public static GameState State=GameState.Playing; public static Player Player=new Player();
 public static Inner Inner=new Inner(); public static Missions Missions=new Missions(); public static Peds Peds=new Peds();
 public static Director Creatures=new Director(); public static Vector3 PlayerPosition;
 public static bool IsPlaying {get{return State==GameState.Playing;}}
}
public static class LivingTerritories04BQa { public static bool FocusOverride; public static Vector3 FocusPoint; }
public static class Log { public static int Warnings; public static void Warn(string text){Warnings++;} }
public static class TerritoryEcologyRules04B { RULES }
public sealed class Probe {
 HABITAT_CLASS
 STAFF_CLASS
 private readonly List<Habitat> _habitats=new List<Habitat>();
 private readonly List<StaffSite> _staff=new List<StaffSite>();
 private float _clock=10f,_slice,_careCooldown=20f; private int _cursor; private bool _initialised=true;
 private int _allocations; public static int Destroyed;
 private static void Destroy(GameObject gameObject){gameObject.Destroyed=true;Destroyed++;}
 // Deliberate boundary: this test does not emulate the spawn director or native safe-position physics.
 private bool Fill(Habitat habitat,Vector3 focus){_allocations++;return false;}
 private bool Fill(StaffSite site,Vector3 focus){_allocations++;return false;}
 private void MaintainStaff(StaffSite site){}
 ACTUAL_METHODS
 private void Seed(bool ready=true) {
  _habitats.Add(new Habitat{Data=new TerritoryHabitat04B(),Owner=new Transform(),Tag="04b/habitat/test",Loaded=true,NextSlot=3});
  _habitats[0].Bodies.Add(new Creature{Ready=ready,SpawnTime=0});
  _staff.Add(new StaffSite{Owner=new Transform(),Loaded=true,NextSlot=1});
  _staff[0].Bodies.Add(new NpcController{OnScriptArrived=delegate{}});
 }
 private static void ResetHost() {
  Game.State=GameState.Playing;Game.Player=new Player();Game.Inner=new Inner();Game.Missions=new Missions();
  Game.Peds=new Peds();Game.Creatures=new Director();Game.PlayerPosition=new Vector3();GameTime.Paused=false;
  Time.time=0;LivingTerritories04BQa.FocusOverride=false;Log.Warnings=0;Destroyed=0;
 }
 private static void Check(List<object> results,string name,bool value) {results.Add(new{name,passed=value});}
 public static void Main() {
  var results=new List<object>();
  ResetHost();var p=new Probe();p.Seed();Game.State=GameState.Paused;GameTime.Paused=true;p.Tick(3);
  Check(results,"pause freezes timers and retains actors",p._clock==10&&p._habitats[0].Bodies.Count==1&&p._staff[0].Bodies.Count==1&&p._allocations==0);
  Game.State=GameState.Playing;GameTime.Paused=false;p.Tick(.5f);
  Check(results,"resume retains the same population",p._clock==10.5f&&p._habitats[0].Bodies.Count==1&&Game.Creatures.Removed==0);
  ResetHost();p=new Probe();p.Seed();Game.State=GameState.Title;p.Tick(.5f);
  Check(results,"title removes owned actors without erasing registrations",p._habitats.Count==1&&p._staff.Count==1&&p._habitats[0].Bodies.Count==0&&p._staff[0].Bodies.Count==0&&Game.Creatures.Removed==1&&Destroyed==1);
  p.Tick(.5f);p.ClearPopulation();
  Check(results,"repeated title and cleanup do not despawn twice",Game.Creatures.Removed==1&&Destroyed==1);
  ResetHost();p=new Probe();p.Seed();Game.Player.IsDead=true;p.Tick(.5f);
  Check(results,"death removes actors",Game.Creatures.Removed==1&&p._staff[0].Bodies.Count==0);
  ResetHost();p=new Probe();p.Seed();Game.Inner.IsInner=true;p.Tick(.5f);
  Check(results,"Veil transition removes outer residents",Game.Creatures.Removed==1&&!p._habitats[0].Loaded);
  ResetHost();p=new Probe();p.Seed();Game.PlayerPosition=new Vector3(256,0,0);p.Tick(.5f);
  Check(results,"leaving beyond hysteresis unloads with cooldown",Game.Creatures.Removed==1&&p._habitats[0].Retry==p._clock+75&&!p._habitats[0].Loaded);
  ResetHost();p=new Probe();p.Seed();Game.PlayerPosition=new Vector3(200,0,0);p.Tick(.5f);
  Check(results,"inside hysteresis retains actors",Game.Creatures.Removed==0&&p._habitats[0].Loaded);
  ResetHost();p=new Probe();p.Seed();p._habitats[0].Owner.gameObject.activeInHierarchy=false;p.Tick(.5f);
  Check(results,"geometry owner disabled unloads its residents",Game.Creatures.Removed==1&&!p._habitats[0].Loaded);
  ResetHost();p=new Probe();p.Seed(false);Time.time=26;p.CheckPendingBodies(p._habitats[0]);
  Check(results,"failed body removes population and blocks retry this visit",Game.Creatures.Removed==1&&p._habitats[0].Bodies.Count==0&&p._habitats[0].Loaded&&p._habitats[0].NextSlot==3&&Log.Warnings==1);
  p.CheckPendingBodies(p._habitats[0]);
  Check(results,"failed body cleanup is idempotent",Game.Creatures.Removed==1&&Log.Warnings==1);
  ResetHost();p=new Probe();p.Seed();Game.Creatures=null;p.ClearSites();
  Check(results,"missing director destroys bodies directly and clears registrations",Destroyed==2&&p._habitats.Count==0&&p._staff.Count==0&&p._clock==0);
  ResetHost();p=new Probe();p.Seed();var n=p._staff[0].Bodies[0];p.OnDisable();p.OnDestroy();
  Check(results,"disable then destroy clears callback and performs single cleanup",n.OnScriptArrived==null&&n.gameObject.Destroyed&&Game.Creatures.Removed==1&&p._habitats.Count==0);
  ResetHost();p=new Probe();p.Seed();Game.Missions.Active=new object();p.Tick(.5f);
  Check(results,"active story mission suppresses new allocations",p._allocations==0&&p._habitats[0].Bodies.Count==1);
  ResetHost();p=new Probe();p.Seed();Game.Peds.Suppressed=true;p.Tick(.5f);
  Check(results,"original pedestrian suppression blocks new allocations",p._allocations==0);
  ResetHost();p=new Probe();p.Seed();Game.PlayerPosition=new Vector3(400,0,0);LivingTerritories04BQa.FocusOverride=true;LivingTerritories04BQa.FocusPoint=new Vector3();p.Tick(.5f);
  Check(results,"QA focus retains noncombat population without moving player",Game.Creatures.Removed==0&&Game.PlayerPosition.x==400);
  int failures=0;foreach(var result in results) { var json=JsonSerializer.Serialize(result);if(json.Contains("false"))failures++;}
  Console.Write(JsonSerializer.Serialize(new{cases=results,passed=results.Count-failures,failed=failures}));
 }
}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', type=Path, default=ROOT.parent / '.tools/dotnet/dotnet')
    parser.add_argument('--report', type=Path, default=ROOT / 'Docs/LivingTerritories04B/Validation/ecology_lifecycle_checks.json')
    args = parser.parse_args(); dotnet = args.dotnet.resolve(); source = SOURCE.read_text(); rules = RULES.read_text()
    markers = ['public void Tick(', 'private void CheckPendingBodies(', 'private void Unload(Habitat', 'private void Unload(StaffSite', 'public void ClearPopulation(', 'public void ClearSites(', 'private void OnDisable(', 'private void OnDestroy(']
    methods = '\n'.join(block(source, marker) for marker in markers)
    rule_methods = '\n'.join(block(rules, marker) for marker in ['public static bool ShouldLoad('])
    constants = '\n'.join(line.strip() for line in rules.splitlines() if line.strip().startswith('public const '))
    host = HOST.replace('RULES', constants+'\n'+rule_methods).replace('HABITAT_CLASS', block(source,'private sealed class Habitat')).replace('STAFF_CLASS',block(source,'private sealed class StaffSite')).replace('ACTUAL_METHODS',methods)
    compiler = sorted(dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted(dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    with tempfile.TemporaryDirectory(prefix='04b-ecology-lifetime-') as directory:
        scratch = Path(directory); code=scratch/'Program.cs';code.write_text(host);dll=scratch/'Lifetime.dll'
        options = ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)] + ['-r:'+str(p) for p in refs] + [str(code)]
        rsp=scratch/'compile.rsp';rsp.write_text('\n'.join('"'+s+'"' for s in options))
        result=subprocess.run([str(dotnet),str(compiler),'@'+str(rsp)],capture_output=True,text=True)
        if result.returncode: raise RuntimeError(result.stdout+result.stderr)
        dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        report=json.loads(subprocess.check_output([str(dotnet),str(dll)],text=True))
    report.update(scope=__doc__, native_unity='NOT RUN', source_sha256=hashlib.sha256(source.encode()).hexdigest(), methods=markers)
    args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({key:report[key] for key in ('passed','failed')}));return bool(report['failed'])


if __name__ == '__main__':
    raise SystemExit(main())
