"""Exact proof of the two previously shipped append-only save blocks. No permission for future schema edits."""
import hashlib
import subprocess

BASE='a6c86a574cd82e3fee0f4b2f48f922a81127ea24'
REVIEWED='89644e85aa9b3270d2755ceab5f12a1ebe88e49c'
ANCHOR='a8f21d507876b36630f4d0421c2e55401e36ab15'
PATH='Assets/NeonSyndicate/Scripts/Runtime/Gameplay/SaveSystem.cs'
DIGEST='ddb71120265b13e537bb41bc3fe280b797cd0b6bcbb813b7d7ec2b96f39ebcaf'
APPENDS=(b'        // Generated city interiors (CityInteriors): visited doors and the all-visited reward. Old saves start with nothing visited.\n        public InteriorSave Interiors = new InteriorSave();\n',b'        // The wilds (WildsSystem): named places found and the all-found bonus. Old saves start with nothing found.\n        public WildsSave Wilds = new WildsSave();\n')
def prefix(data):return data.split(b'public static class SaveSystem')[0]
def exact_append_only(original,reviewed,current):
    if hashlib.sha256(reviewed).hexdigest()!=DIGEST or current!=reviewed:return False
    normalized=reviewed
    for block in APPENDS:
        if normalized.count(block)!=1:return False
        normalized=normalized.replace(block,b'',1)
    return normalized==original and reviewed.index(APPENDS[0])<reviewed.index(APPENDS[1])
def verify(root):
    def old(commit):return prefix(subprocess.check_output(['git','show',commit+':'+PATH],cwd=root))
    if subprocess.run(['git','merge-base','--is-ancestor',REVIEWED,ANCHOR],cwd=root,capture_output=True).returncode:return False
    return exact_append_only(old(BASE),old(REVIEWED),prefix((root/PATH).read_bytes()))
