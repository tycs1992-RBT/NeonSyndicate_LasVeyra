#!/usr/bin/env python3
"""Execute actual 04C resident rules and lifecycle methods with explicit managed host doubles.

This checks production branch routing and cleanup ownership. It is not Unity
compilation, actor navigation, physics, animation, audio playback or performance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/Gameplay/InhabitedCity'


def block(source, marker):
    start = source.index(marker)
    end = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


HOST = r'''
using System;
using System.Collections.Generic;
using NeonSyndicate;
public struct Vector3 {
 public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
 public static Vector3 up {get{return new Vector3(0,1,0);}}
 public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
 public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
 public static float Distance(Vector3 a,Vector3 b){return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
}
public static class Mathf {public static float Min(float a,float b){return Math.Min(a,b);}}
public sealed class GameObject {public bool activeInHierarchy=true,Destroyed;public void SetActive(bool value){activeInHierarchy=value;}}
public sealed class Transform {public Vector3 position; public GameObject gameObject=new GameObject();}
public sealed class MeshBody {public object sharedMesh=new object();}
public sealed class Rig {public MeshBody Body=new MeshBody();}
public sealed class HealthHost {public float HealthValue=100,MaxHealth=100;public float Health {get{return HealthValue;}}}
public sealed class NpcController {
 public bool IsDead,IsRagdolled;public float DeathTime;public Rig Rig=new Rig();public HealthHost Health=new HealthHost();
 public Transform transform=new Transform();public GameObject gameObject=new GameObject();public Action<NpcController> OnKilled;
 public int Cleared;public void ClearResidence04C(){Cleared++;}
}
public sealed class AudioSource {public GameObject gameObject=new GameObject();public bool Stopped; public void Stop(){Stopped=true;}}
public sealed class AudioClip {public bool Destroyed;}
public sealed class VenueDefinition04C {public string Id="home",Kind="home";}
public sealed class VenueView04C {public GameObject gameObject=new GameObject();public bool Contains(Vector3 p){return p.y>=0;}}
public sealed class VenueBinding04C {public VenueDefinition04C Definition=new VenueDefinition04C();public Vector3 Entrance;public VenueView04C View=new VenueView04C();public bool Available {get{return View!=null;}}}
public sealed class InhabitedCitySystem {public bool isActiveAndEnabled=true,EnabledAtBoot=true,IsOperational=true,ResidentsAtBoot=true,AmbienceAtBoot=true;public string OccupiedId="";public List<VenueBinding04C> Venues=new List<VenueBinding04C>();}
public enum GameState {Loading,Title,Playing,Paused,Wasted,Busted,Cutscene}
public static class Settings {public static int Graphics=1;}
public static class LivingCity04CQa {public static bool FocusOverride; public static Vector3 FocusPoint;}
public static class GameTime {public static bool Paused;}
public static class Time {public static float time;}
public sealed class Player {public bool IsDead;}
public sealed class Inner {public bool IsInner;}
public sealed class Peds {public bool Suppressed;}
public sealed class MissionDef {public string Id;}
public sealed class MissionContext {public MissionDef Def;}
public sealed class Missions {public MissionContext Active;}
public sealed class QuestDef {public string Id;}
public sealed class QuestRun {public QuestDef Quest;}
public sealed class QuestBook {public QuestRun Running;}
public static class Game {
 public static GameState State=GameState.Playing;public static Player Player=new Player();public static Inner Inner=new Inner();
 public static Peds Peds=new Peds();public static Missions Missions=new Missions();public static QuestBook Quests=new QuestBook();public static Vector3 PlayerPosition;
 public static bool IsPlaying {get{return State==GameState.Playing;}}
}
public sealed class ResidenceReleaseProbe {
 private Func<Vector3,bool> _residenceContains=delegate{return true;};
 private Transform _residenceOwner=new Transform();private bool _residenceRoute=true;
 private Action OnScriptArrived=delegate{};
 CLEAR_RESIDENCE
 public bool Run(){ClearResidence04C();return _residenceContains==null&&_residenceOwner==null&&!_residenceRoute&&OnScriptArrived!=null;}
}
public sealed class Probe {
 SITE
 private readonly List<Site> _sites=new List<Site>(),_selected=new List<Site>();
 private InhabitedCitySystem _owner=new InhabitedCitySystem();private float _clock=10f,_slice;
 private readonly List<string> Attempts=new List<string>();
 private static int Destroyed;
 private static void Destroy(GameObject obj){obj.Destroyed=true;Destroyed++;}
 private static void Destroy(AudioClip obj){obj.Destroyed=true;Destroyed++;}
 private bool TrySpawn(Site site,int slot,int preset){Attempts.Add(site.Venue.Definition.Id+":"+slot);return false;}
 private void UpdateAudio(Site site){}
 METHODS
 private Site Seed(string id="home",float distance=0,bool loaded=true) {
  var venue=new VenueBinding04C{Definition=new VenueDefinition04C{Id=id},Entrance=new Vector3(distance,0,0)};
  _owner.Venues.Add(venue);var site=new Site{Venue=venue,Loaded=loaded};site.Bodies[0]=new NpcController();site.Assigned[0]=true;_sites.Add(site);return site;
 }
 private static void Reset() {Game.State=GameState.Playing;Game.Player=new Player();Game.Inner=new Inner();Game.Peds=new Peds();Game.Missions=new Missions();Game.Quests=new QuestBook();GameTime.Paused=false;Game.PlayerPosition=new Vector3();Time.time=0;Settings.Graphics=1;LivingCity04CQa.FocusOverride=false;Destroyed=0;}
 private static readonly List<object> Results=new List<object>();
 private static void Check(string name,bool value){Results.Add(new{name,passed=value});}
 public static void Main() {
  Check("opt-in delegate release preserves unrelated scripted arrival callback",new ResidenceReleaseProbe().Run());
  Reset();var p=new Probe();var site=p.Seed();GameTime.Paused=true;p.Tick(3);Check("pause freezes timers and retains body",p._clock==10&&p.ResidentCount==1&&p.Attempts.Count==0);
  Reset();p=new Probe();site=p.Seed();var npc=site.Bodies[0];Game.State=GameState.Title;p.Tick(1);p.Tick(1);Check("title cleanup idempotent and releases delegate",p.ResidentCount==0&&Destroyed==1&&npc.Cleared==1&&!npc.gameObject.activeInHierarchy);
  foreach(string condition in new[]{"dead","veil","disabled","feature-off","broken-owner","loading"}) {
   Reset();p=new Probe();p.Seed();if(condition=="dead")Game.Player.IsDead=true;if(condition=="veil")Game.Inner.IsInner=true;if(condition=="disabled")p._owner.isActiveAndEnabled=false;if(condition=="feature-off")p._owner.EnabledAtBoot=false;if(condition=="broken-owner")p._owner.IsOperational=false;if(condition=="loading")Game.State=GameState.Loading;p.Tick(1);Check(condition+" releases owned bodies",p.ResidentCount==0&&Destroyed==1);
  }
  Reset();p=new Probe();p.Seed();Game.Missions.Active=new MissionContext();p.Tick(1);Check("mission preserves existing residents and defers new bodies",p.ResidentCount==1&&p.Attempts.Count==0);
  foreach(string questId in QuietSignal04C.QuestIds) {
   Reset();p=new Probe();p.Seed();Game.Quests.Running=new QuestRun{Quest=new QuestDef{Id=questId}};Game.Missions.Active=new MissionContext{Def=new MissionDef{Id="quest_"+questId}};Game.Peds.Suppressed=true;p.Tick(1);Check("matching chapter admits indoor hosts: "+questId,p.Attempts.Count>0);
   p.Attempts.Clear();Game.Missions.Active.Def.Id="unrelated_story";p.Tick(1);Check("chapter cannot bypass unrelated mission: "+questId,p.Attempts.Count==0);
  }
  Reset();p=new Probe();p.Seed();Game.Peds.Suppressed=true;p.Tick(1);Check("ped suppression preserves ownership and defers admission",p.ResidentCount==1&&p.Attempts.Count==0);
  Reset();p=new Probe();p.Seed("far",92);p.Seed("near",10);p.Seed("middle",20);p.Tick(1);Check("nearest venues respect preset cap",p.LoadedCount==2&&p.ResidentCount==2);Check("hosts considered before visitors",string.Join(",",p.Attempts)=="near:0,middle:0,near:1,middle:1");
  Reset();p=new Probe();p.Seed("occupied",500);p._owner.OccupiedId="occupied";p.Tick(1);Check("occupied venue retained beyond distance threshold",p.ResidentCount==1&&p.LoadedCount==1);
  Reset();p=new Probe();p.Seed("edge",95);p.Tick(1);Check("unoccupied venue unloads at hysteresis edge",p.ResidentCount==0&&p.LoadedCount==0);
  Reset();p=new Probe();site=p.Seed();site.Bodies[0].IsDead=true;Time.time=26;p.Tick(1);Check("dead resident retires without immediate replacement",p.ResidentCount==0&&site.Retry[0]==191);
  Reset();p=new Probe();site=p.Seed();site.Bodies[0]=null;p.Tick(1);Check("unexpected destroyed body receives cooldown",!site.Assigned[0]&&site.Retry[0]==191);
  Reset();p=new Probe();site=p.Seed();site.Bodies[0].Rig.Body.sharedMesh=null;p.Tick(1);Check("broken synchronous mesh releases owned body",p.ResidentCount==0&&site.Retry[0]==191);
  Reset();p=new Probe();site=p.Seed();var audio=new AudioSource();var clip=new AudioClip();site.Ambience=audio;site.Clip=clip;p.ClearPopulation();Check("room unload stops audio and destroys source and clip",audio.Stopped&&audio.gameObject.Destroyed&&clip.Destroyed&&site.Clip==null);
  Reset();p=new Probe();site=p.Seed();site.Bodies[0].Health.HealthValue=50;p.ClearPopulation();Check("injury cooldown survives a room unload",site.Retry[0]==190);
  Reset();p=new Probe();site=p.Seed();p._owner.ResidentsAtBoot=false;p.Tick(1);Check("resident toggle clears only actors while venue remains selected",p.ResidentCount==0&&p.LoadedCount==1&&p.Attempts.Count==0);
  Reset();p=new Probe();p.Seed("capture",500);LivingCity04CQa.FocusOverride=true;LivingCity04CQa.FocusPoint=new Vector3(500,0,0);p.Tick(1);Check("QA focus admits real distant residents without moving player",p.ResidentCount==1&&Game.PlayerPosition.x==0);
  Reset();p=new Probe();for(int i=0;i<3;i++){site=p.Seed("room"+i,i*10);site.Bodies[1]=new NpcController();site.Bodies[2]=new NpcController();}Settings.Graphics=2;p.Tick(1);Check("lowered preset retires owned excess visitors",p.ResidentCount==8);
  for(int preset=0;preset<4;preset++) {
   Check("admission preset "+preset,InhabitedResidentRules04C.CanAllocate(preset,0,0,0)&&!InhabitedResidentRules04C.CanAllocate(preset,InhabitedResidentRules04C.ResidentCap(preset),0,0)&&!InhabitedResidentRules04C.CanAllocate(preset,0,InhabitedResidentRules04C.HumanoidCap(preset),0)&&!InhabitedResidentRules04C.CanAllocate(preset,0,1,InhabitedResidentRules04C.CombinedCap(preset)-1));
  }
  Check("allocation prevents integer overflow",!InhabitedResidentRules04C.CanAllocate(3,0,1,int.MaxValue));
  Check("floor tolerance rejects another storey and nonfinite heights",InhabitedResidentRules04C.FloorMatches(2,2.025f)&&!InhabitedResidentRules04C.FloorMatches(2,4)&&!InhabitedResidentRules04C.FloorMatches(float.NaN,0)&&!InhabitedResidentRules04C.FloorMatches(float.PositiveInfinity,float.PositiveInfinity));
  Check("load hysteresis and invalid distance",!InhabitedResidentRules04C.ShouldLoad(false,false,70)&&InhabitedResidentRules04C.ShouldLoad(true,false,70)&&!InhabitedResidentRules04C.ShouldLoad(false,false,float.NaN));
  var seeds=new HashSet<int>();foreach(string id in new[]{"lofts","clinic","forge","inn","archive","observatory","garage","depot"})for(int slot=0;slot<3;slot++){int seed=InhabitedResidentRules04C.Seed(61724,id,slot);Check("stable unique seed "+id+slot,seed>0&&seeds.Add(seed)&&seed==InhabitedResidentRules04C.Seed(61724,id,slot)&&seed!=InhabitedResidentRules04C.Seed(61725,id,slot));}
  foreach(string kind in new[]{"lofts","clinic","forge","inn","archive","observatory","garage","depot"}){
   float[] samples=InhabitedResidentRules04C.RoomTone(kind,22050);double energy=0;bool valid=samples.Length==88200;
   foreach(float sample in samples){valid&=!float.IsNaN(sample)&&!float.IsInfinity(sample)&&Math.Abs(sample)<=.3f;energy+=sample*sample;}
   Check("bounded periodic original tone "+kind,valid&&energy/samples.Length>.001&&Math.Abs(samples[0]-samples[samples.Length-1])<.03f);
  }
  System.Console.Write(System.Text.Json.JsonSerializer.Serialize(Results));
 }
}
'''


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet', type=Path, default=Path('/workspace/.tools/dotnet/dotnet'))
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    rules = RUNTIME / 'InhabitedResidentRules04C.cs'
    runtime = RUNTIME / 'InhabitedResidents04C.cs'
    source = runtime.read_text()
    npc = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/AI/NpcController.Residence04C.cs'
    markers = ['public int ResidentCount', 'public int LoadedCount', 'public void Tick(', 'private bool CanAdmitResidents(',
               'private void DiscoverSites(', 'private void Maintain(', 'private void ReleaseBody(',
               'private void Unload(', 'private void ReleaseAudio(', 'public void ClearPopulation(']
    code = HOST.replace(' SITE', ' ' + block(source, 'private sealed class Site')).replace(' METHODS', '\n'.join(block(source, marker) for marker in markers))
    code = code.replace(' CLEAR_RESIDENCE', block(npc.read_text(), 'public void ClearResidence04C('))
    compiler = sorted(args.dotnet.parent.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    refs = sorted(args.dotnet.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    with tempfile.TemporaryDirectory(prefix='04c-residents-') as temp:
        scratch = Path(temp)
        program = scratch / 'Program.cs'
        program.write_text(code)
        output = scratch / 'Residents.dll'
        response = scratch / 'compile.rsp'
        compile_args = ['-nologo', '-target:exe', '-langversion:9', '-out:' + str(output)] + ['-r:' + str(ref) for ref in refs] + [str(program), str(rules), str(RUNTIME / 'QuietSignal04C.cs')]
        response.write_text('\n'.join('"' + arg + '"' for arg in compile_args))
        run = subprocess.run([str(args.dotnet), str(compiler), '@' + str(response)], capture_output=True, text=True)
        if run.returncode:
            raise RuntimeError(run.stdout + run.stderr)
        (scratch / 'Residents.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        results = json.loads(subprocess.check_output([str(args.dotnet), str(output)], text=True))
    report = {'scope': 'Actual production rules and extracted lifecycle methods, with managed host doubles for engine objects. No native Unity compile, physics, navigation, animation, audio playback or performance.',
              'passed': sum(row['passed'] for row in results), 'failed': sum(not row['passed'] for row in results),
              'production_methods': markers + ['NpcController.ClearResidence04C'], 'source_sha256': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in (rules, runtime, npc)}, 'results': results}
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))
    raise SystemExit(1 if report['failed'] else 0)


if __name__ == '__main__':
    main()
