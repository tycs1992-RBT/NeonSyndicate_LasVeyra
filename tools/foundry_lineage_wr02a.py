"""Complete exact WR02-A integration replay. All other inherited source bytes remain visible to guards."""
from pathlib import Path
import hashlib,json,subprocess
from composition_lineage_wr02b import canonical_composition_bytes
BASELINE='ed483fc720ee1493ab0e2f783c643ea0e9ae6397'
CONTRACT_SHA256='8d1eb4ea9bb19851206ef6fd7cbba84c00c4ce34f19d45d7fa42197cb8127866'
def load_contract(path=None):
 path=Path(path)if path else Path(__file__).with_name('wr02a_owner_hooks.json');raw=path.read_bytes()
 if hashlib.sha256(raw).hexdigest()!=CONTRACT_SHA256:raise ValueError('Reviewed WR02-A hook contract changed.')
 data=json.loads(raw)
 if data['baseline']!=BASELINE:raise ValueError('Unexpected foundry baseline.')
 return data
def canonical_foundry_bytes(root,path,current):
 current=canonical_composition_bytes(root,path,current)
 contract=load_contract();row=contract['paths'].get(path)
 if row is None or hashlib.sha256(current).hexdigest()!=row['after_sha256']:return current
 try:before=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=root,stderr=subprocess.DEVNULL)
 except subprocess.CalledProcessError:return current
 return reviewed_view(path,before,current,contract)
def reviewed_view(path,before,current,contract=None):
 data=load_contract()if contract is None else contract;row=data['paths'].get(path)
 if row is None or hashlib.sha256(before).hexdigest()!=row['before_sha256']:return current
 expected=before
 for edit in row['edits']:
  old=edit['before'].encode();new=edit['after'].encode()
  if not old or expected.count(old)!=1:return current
  expected=expected.replace(old,new,1)
 if hashlib.sha256(expected).hexdigest()!=row['after_sha256']:return current
 return before if current==expected else current
