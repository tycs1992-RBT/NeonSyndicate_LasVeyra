"""Compare independently parsed FBX bone motion with the actual existing CreaturePose samples. No Unity contact claim."""
import argparse,json,math
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
def multiply(a,b):
 ax,ay,az,aw=a;bx,by,bz,bw=b
 return np.array([aw*bx+ax*bw+ay*bz-az*by,aw*by-ax*bz+ay*bw+az*bx,aw*bz+ax*by-ay*bx+az*bw,aw*bw-ax*bx-ay*by-az*bz])
def rotate(q,v):return np.asarray(v)+2*np.cross(q[:3],np.cross(q[:3],v)+q[3]*np.asarray(v))
def main():
 p=argparse.ArgumentParser();p.add_argument('--report',type=Path,required=True);p.add_argument('--inspection-dir',type=Path);a=p.parse_args();contract=json.loads((ROOT/'ArtSource/FoundryWR02A/CrystalBasilisk.original-contract.json').read_text());processed=json.loads((ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/CrystalBasiliskCandidate/processed.json').read_text());rows=[]
 for output in processed['outputs']:
  data=json.loads((a.inspection_dir/(Path(output['path']).name+'.json')).read_text())if a.inspection_dir else output['independentInspection'];clips=data['clips'];seen=set()
  for clip in clips:
   source=next((c for c in contract['clips']if'Foundry_'+c['id']in clip['name']),None)
   if source is None:raise ValueError('Unexpected authored clip.')
   seen.add(source['id']);error=0;count=0
   for sample in clip['samples']:
    frame=source['frames'][min(sample['frame']-1,len(source['frames'])-1)];positions=[];rotations=[]
    for i,bone in enumerate(contract['bones']):
     parent=bone['parent'];q=np.array(frame['rotations'][i]);v=np.array(frame['positions'][i]);positions.append(v if parent<0 else positions[parent]+rotate(rotations[parent],v));rotations.append(q if parent<0 else multiply(rotations[parent],q))
    error=max(error,float(np.max(np.abs(np.array(positions)-np.array(sample['positions'])))));count+=len(positions)
   rows.append({'lod':output['lod'],'clip':source['id'],'globalBonePositionsChecked':count,'maximumPositionErrorMetres':error,'passed':error<.0002 and len(clip['samples'])==len(source['frames'])})
  if seen!={'Idle','Walk','Attack','Impact','Death'}:raise ValueError('Incomplete actual clip coverage.')
 report={'scope':'Full 30 Hz independently decoded FBX global bone positions against original compiled CreaturePose. This is DCC source replay, not native Unity motion, foot/collision contact, AI, damage or telegraph acceptance.','rows':rows,'passed':sum(r['passed']for r in rows),'total':len(rows),'nativeUnity':'NOT RUN'};a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'total':report['total'],'maxError':max(r['maximumPositionErrorMetres']for r in rows)}));return int(report['passed']!=report['total'])
if __name__=='__main__':raise SystemExit(main())
