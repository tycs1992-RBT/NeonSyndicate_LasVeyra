#!/usr/bin/env python3
"""Plan small approved batches; never submit paid generation from this queue."""
import argparse,hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def release_gate(acceptance,assets):
 for asset in assets:
  row=acceptance.get('assets',{}).get(asset['assetId'],{})
  if not all(row.get(key)is True for key in ['ownerAccepted','nativeImported','nativeMotionOrCollisionAccepted','ordinaryGameplayAccepted']):raise ValueError('First demonstration assets have not passed native/owner acceptance.')
  if row.get('inputSha256')!=asset['input_sha256']:raise ValueError('Approval belongs to different source bytes.')
 return True
def main():
 p=argparse.ArgumentParser();p.add_argument('mode',choices=['plan','process-approved']);p.add_argument('--queue',type=Path,default=ROOT/'ArtSource/FoundryWR02A/CATALOG_QUEUE.json');p.add_argument('--acceptance',type=Path);p.add_argument('--blender',type=Path);p.add_argument('--inspector',type=Path);a=p.parse_args();queue=json.loads(a.queue.read_text())
 if a.mode=='plan':print(json.dumps({'paidSubmission':False,'firstAssets':queue['firstAssets'],'proposals':queue['proposals'],'gate':'Both first assets need native and owner acceptance before batch processing.'},indent=2));return 0
 if a.acceptance is None or a.blender is None or a.inspector is None:raise SystemExit('Explicit acceptance receipt and local Blender executable required.')
 assets=[json.loads((ROOT/row['processedManifest']).read_text())for row in queue['firstAssets']];release_gate(json.loads(a.acceptance.read_text()),assets)
 for job in queue.get('approvedLocalJobs',[]):
  if job.get('ownerApproved')is not True:raise SystemExit('Unapproved job in batch.')
  brief=(ROOT/job['brief']).resolve()
  if not brief.is_relative_to(ROOT/'ArtSource/FoundryWR02A'):raise SystemExit('Unexpected brief path.')
  subprocess.run([str(a.blender),'-b','-Y','--python-exit-code','2','--python',str(ROOT/'tools/foundry/process_asset.py'),'--','--brief',str(brief),'--inspector',str(a.inspector.resolve())],cwd=ROOT,check=True)
 return 0
if __name__=='__main__':raise SystemExit(main())
