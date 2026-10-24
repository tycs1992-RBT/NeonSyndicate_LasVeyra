#!/usr/bin/env python3
"""Convert only the new H7 delivery paths to LFS in a forward commit on the art branch."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def main():
 if os.environ.get('GITHUB_ACTIONS')!='true' or os.environ.get('GITHUB_REF')!='refs/heads/feature/04oh-ascension-art-foundation':raise SystemExit('Only the authorized art-branch Actions environment may publish.')
 prefixes=('Assets/NeonSyndicate/Art/LiveCast/','ArtSource/AscensionH9/','Assets/NeonSyndicate/Art/ProductionH9/','Docs/Ascension04OH/ArtReviewH9/','Docs/Ascension04OH/EvidenceH9/','ArtSource/AscensionH8S/','Assets/NeonSyndicate/Art/ProductionH8S/','Docs/Ascension04OH/ArtReviewH8S/','Docs/Ascension04OH/EvidenceH8S/','ArtSource/WorldFoundationH8R/','Assets/NeonSyndicate/Art/WorldFoundationH8R/','ArtSource/AscensionH8R/','Assets/NeonSyndicate/Art/ProductionH8R/','Docs/Ascension04OH/ArtReviewH8R/','Docs/Ascension04OH/EvidenceH8R/','ArtSource/AscensionH8/','Assets/NeonSyndicate/Art/ProductionH8/','Docs/Ascension04OH/ArtReviewH8/','Docs/Ascension04OH/EvidenceH8/','ArtSource/AscensionH7/','Assets/NeonSyndicate/Art/ProductionH7/','Docs/Ascension04OH/ArtReviewH7/','Docs/Ascension04OH/EvidenceH7/')
 extensions={'.blend','.fbx','.png','.jpg','.mp4','.gz','.obj'};paths=[p for p in git('ls-files','-z').split('\0')if p.startswith(prefixes)and Path(p).suffix.lower()in extensions]
 if not paths:print('No H7 binary delivery yet.');return
 # Checkout without smudge preserves bootstrap bytes; existing pointer entries are skipped.
 changed=[]
 for p in paths:
  content=subprocess.check_output(['git','show','HEAD:'+p],cwd=ROOT)
  if not content.startswith(b'version https://git-lfs.github.com/spec/v1'):
   if (ROOT/p).read_bytes()!=content:raise SystemExit('Unexpected H7 checkout contents: '+p)
   changed.append(p)
 if not changed:print('All H7 binaries already use LFS.');return
 subprocess.run(['git','add','--renormalize','--',*changed],cwd=ROOT,check=True)
 staged=git('diff','--cached','--name-only').splitlines()
 if set(staged)!=set(changed):raise SystemExit('Refuse unexpected staged paths.')
 oids=[]
 for p in staged:
  pointer=git('show',':'+p);expected=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
  if 'oid sha256:'+expected not in pointer or not pointer.startswith('version https://git-lfs.github.com/spec/v1'):raise SystemExit('Actual bytes were not converted to a matching pointer: '+p)
  oids.append(expected)
 subprocess.run(['git','config','user.name','github-actions[bot]'],cwd=ROOT,check=True);subprocess.run(['git','config','user.email','41898282+github-actions[bot]@users.noreply.github.com'],cwd=ROOT,check=True)
 subprocess.run(['git','commit','-m','04OH H7: store new art delivery in Git LFS without rewriting history'],cwd=ROOT,check=True)
 subprocess.run(['git','lfs','push','--object-id','origin',*sorted(set(oids))],cwd=ROOT,check=True)
 subprocess.run(['git','push','origin','HEAD:refs/heads/feature/04oh-ascension-art-foundation'],cwd=ROOT,check=True)
 print(json.dumps({'published':git('rev-parse','HEAD'),'paths':staged,'history_rewritten':False}))
if __name__=='__main__':main()
