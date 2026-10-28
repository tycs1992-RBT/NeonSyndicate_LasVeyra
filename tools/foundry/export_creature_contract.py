"""Export original creature skeleton, hit volumes and actual existing-pose samples. Not native animation acceptance."""
import argparse,contextlib,hashlib,io,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import measure_city_rebirth_04o as compiler
from measure_world_rewrite_wr01 import source_digest,commit_source_digest
CODE=r'''
using System;using System.Collections.Generic;using System.Linq;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public static class Probe {
static float[] P(Vector3 v){return new[]{v.x,v.y,v.z};}static float[] Q(Quaternion q){return new[]{q.x,q.y,q.z,q.w};}
public static void Main(){CreatureLook look=CreatureLooks.For("crystal_basilisk",61724);CreatureBody body=CreatureBuilder.Build(look,2);CreatureSkeleton sk=body.Skeleton;List<object> clips=new List<object>();
foreach(string id in new[]{"Idle","Walk","Attack","Impact","Death"}){CreaturePose pose=new CreaturePose(body);pose.Reset(Vector3.zero,0);List<object> frames=new List<object>();int count=id=="Impact"?30:60;for(int frame=0;frame<count;frame++){float t=frame/30f;CreaturePoseInput input=new CreaturePoseInput{Time=t,Grounded=true,GroundHeight=(x,z)=>0,RootPosition=id=="Walk"?Vector3.forward*t*1.8f:Vector3.zero,Velocity=id=="Walk"?Vector3.forward*1.8f:Vector3.zero,Action=id=="Attack"?CreatureAction.Bite:id=="Impact"?CreatureAction.Stagger:id=="Death"?CreatureAction.Death:CreatureAction.None,ActionT=frame/(float)(count-1),Windup=.4f,Strike=.18f,Hurt=id=="Impact"?1-frame/(float)count:0,HurtDirection=Vector3.back};pose.Solve(ref input,1f/30);frames.Add(new{frame=frame+1,positions=pose.LocalPositions.Select(P).ToArray(),rotations=pose.LocalRotations.Select(Q).ToArray(),footPlantMask=pose.FootPlantMask});}clips.Add(new{id,fps=30,frames});}
Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled existing skeleton, physics declarations and procedural pose replay. Not Unity motion, ground contact or combat acceptance.",species="crystal_basilisk",seed=61724,geometry=new object[0],bones=Enumerable.Range(0,sk.Count).Select(i=>new{name=sk.Names[i],parent=sk.Parents[i],local=P(sk.LocalPositions[i]),model=P(sk.ModelPosition(i))}),legs=sk.Legs.Select(l=>new{l.Upper,l.Lower,l.Foot,l.Toe,l.Front,l.Side}),bounds=new{min=P(body.Bounds.min),max=P(body.Bounds.max)},body.Height,body.Radius,head=P(body.HeadPoint),mouth=P(body.MouthPoint),aim=P(body.AimPoint),hits=body.HitVolumes.Select(h=>new{h.Bone,center=P(h.Center),axis=P(h.Axis),h.Radius,h.Length,h.WeakPoint}),clips,nativeUnity="NOT RUN"}));}}
'''
def main():
 p=argparse.ArgumentParser()
 for key in ['root','dotnet','assembly','unity-references','output','matrix-report']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--source-commit',required=True);a=p.parse_args();root=a.root.resolve();fingerprint=source_digest(root);matrix=json.loads(a.matrix_report.read_text())
 if fingerprint!=matrix['source_sha256']or fingerprint!=commit_source_digest(root,a.source_commit):raise SystemExit('Stale assembly/source identity rejected.')
 compiler.CODE=CODE;old=sys.argv;sys.argv=[__file__,'--root',str(root),'--dotnet',str(a.dotnet.resolve()),'--assembly',str(a.assembly.resolve()),'--unity-references',str(a.unity_references.resolve()),'--output',str(a.output.resolve())]
 try:
  with contextlib.redirect_stdout(io.StringIO()):compiler.main()
 finally:sys.argv=old
 d=json.loads((a.output/'world-manifest.json').read_text());d.update(sourceCommit=a.source_commit,sourceCSharpSha256=fingerprint);(a.output/'creature-contract.json').write_text(json.dumps(d,indent=2)+'\n');print('Original skeleton bones',len(d['bones']),'actual-pose clips',len(d['clips']))
if __name__=='__main__':main()
