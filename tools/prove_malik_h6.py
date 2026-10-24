#!/usr/bin/env python3
"""Independent actual FBX/skin proof. Full source influence policy and failed alternatives remain separate."""
import argparse,hashlib,json,subprocess,tempfile
from pathlib import Path
from fbx_skin_h5 import derive
ROOT=Path(__file__).resolve().parents[1]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--validator',type=Path,required=True);p.add_argument('--inspector',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True);rows=[]
 with tempfile.TemporaryDirectory(prefix='h6-skin-')as td:
  for path in sorted((ROOT/'Assets/NeonSyndicate/Art/ProductionH6/Models').glob('*.fbx')):
   raw=path.read_bytes();prefix=path.stem;selected=subprocess.run([str(a.validator),str(path)],capture_output=True,text=True);strict=json.loads(selected.stdout);(a.output/(prefix+'-strict.json')).write_text(selected.stdout)
   if selected.returncode:raise RuntimeError('Actual FBX validation failed: '+prefix)
   full,full_report=derive(raw,8);four,four_report=derive(raw,4);complete=Path(td)/(prefix+'-full.fbx');reduced=Path(td)/(prefix+'-four.fbx');complete.write_bytes(full);reduced.write_bytes(four)
   results=[]
   for name,source,target in [('complete',path,complete),('four-weight',complete,reduced)]:
    run=subprocess.run([str(a.inspector),str(source),str(target)],capture_output=True,text=True);d=json.loads(run.stdout);d['exit_code']=run.returncode;d['selected_source_sha256']=hashlib.sha256(raw).hexdigest();(a.output/(prefix+'-'+name+'.json')).write_text(json.dumps(d,indent=2)+'\n');results.append(d)
   if not results[0]['passed']or results[0]['exit_code']!=0:raise RuntimeError('Complete source preservation failed: '+prefix)
   if results[1]['passed']or results[1]['exit_code']==0:raise RuntimeError('The known rejected four-weight outcome changed; review it explicitly.')
   (a.output/(prefix+'-four-derivation.json')).write_text(json.dumps(four_report,indent=2)+'\n')
   rows.append({'file':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(raw).hexdigest(),'strict':strict,'complete':results[0],'four_weight_rejected':results[1]})
 aggregate={'scope':__doc__,'files':rows,'selected_policy':'Complete original influences, never the rejected four-weight files','strict_passed':all(x['strict']['passed']for x in rows),'complete_passed':all(x['complete']['passed']for x in rows),'four_weight_passed':False,'samples':sum(x['complete']['samples']for x in rows),'maximum_complete_error_metres':max(x['complete']['maximum_error_metres']for x in rows),'maximum_four_weight_error_metres':max(x['four_weight_rejected']['maximum_error_metres']for x in rows),'native_unity':'NOT RUN'}
 (a.output/'skin-proof.json').write_text(json.dumps(aggregate,indent=2)+'\n');print(json.dumps({k:v for k,v in aggregate.items()if k!='files'}));return 0
if __name__=='__main__':raise SystemExit(main())
