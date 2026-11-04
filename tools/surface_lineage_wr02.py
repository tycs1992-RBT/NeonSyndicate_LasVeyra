"""Replay only complete exact reviewed WR02 hook transformations for inherited owner checks."""
from pathlib import Path
import hashlib,json,subprocess
from foundry_lineage_wr02a import canonical_foundry_bytes

CONTRACT_SHA256 = '7535971036fe980fb0744874e28e31af89ec1d1d0faf82c8b551fe18d4e051cd'
BASELINE = '99072f180f11da7edf168602b6fc603d5eb71daf'

def load_contract(path=None):
    path=Path(path) if path else Path(__file__).with_name('wr02_owner_hooks.json')
    data=path.read_bytes()
    if hashlib.sha256(data).hexdigest()!=CONTRACT_SHA256:raise ValueError('WR02 reviewed hook contract changed without an exact review.')
    value=json.loads(data)
    if value['baseline']!=BASELINE:raise ValueError('Unexpected WR02 baseline.')
    return value

def reviewed_view(path,original,current,contract=None):
    """Return canonical inherited bytes only for the complete pinned transformation. Any other edit remains visible."""
    current=canonical_foundry_bytes(Path(__file__).resolve().parents[1],path,current)
    contract=load_contract() if contract is None else contract;record=contract['paths'].get(path)
    if record is None:return current
    if hashlib.sha256(original).hexdigest()!=record['before_sha256']:return current
    expected=original
    for edit in record['edits']:
        before=edit['before'].encode();after=edit['after'].encode()
        if not before or expected.count(before)!=1:return current
        expected=expected.replace(before,after,1)
    if hashlib.sha256(expected).hexdigest()!=record['after_sha256']:return current
    return original if current==expected else current

def inherited_source_bytes(root,path):
    root=Path(root);current=canonical_foundry_bytes(root,path,(root/path).read_bytes());contract=load_contract()
    if path not in contract['paths']:return current
    if hashlib.sha256(current).hexdigest()!=contract['paths'][path]['after_sha256']:return current
    try:original=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=root,stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:return current
    return reviewed_view(path,original,current,contract)

def matches_original(root,path,original):
    return inherited_source_bytes(root,path)==original
