"""Complete pinned WR02-B hook replay. Extra, partial or unknown edits stay visible to every inherited guard."""
from pathlib import Path
import hashlib,json,subprocess
BASELINE='38ec685deafa3cf6f20a013f7c9556d8e02897f6'
CONTRACT_SHA256='c2b841eb85b23da3c1d9e57de21e77803376c0b9735b676459b1f123ddc0b5f3'
def load_contract(path=None):
 p=Path(path)if path else Path(__file__).with_name('wr02b_owner_hooks.json');raw=p.read_bytes()
 if hashlib.sha256(raw).hexdigest()!=CONTRACT_SHA256:raise ValueError('Exact reviewed WR02-B hook contract changed.')
 data=json.loads(raw)
 if data['baseline']!=BASELINE:raise ValueError('Unexpected WR02-B baseline.')
 return data
def reviewed_view(path,before,current,contract=None):
 data=load_contract()if contract is None else contract;row=data['paths'].get(path)
 if row is None or hashlib.sha256(before).hexdigest()!=row['before_sha256']:return current
 expected=before
 for edit in row['edits']:
  old,new=edit['before'].encode(),edit['after'].encode()
  if not old or expected.count(old)!=1:return current
  expected=expected.replace(old,new,1)
 if hashlib.sha256(expected).hexdigest()!=row['after_sha256']:return current
 return before if current==expected else current

def canonical_composition_bytes(root,path,current):
 contract=load_contract();row=contract['paths'].get(path)
 if row is None or hashlib.sha256(current).hexdigest()!=row['after_sha256']:return current
 try:before=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=root,stderr=subprocess.DEVNULL)
 except subprocess.CalledProcessError:return current
 return reviewed_view(path,before,current,contract)
