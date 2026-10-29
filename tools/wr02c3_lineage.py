"""Full-file hash-pinned WR02-C3 hooks for earlier preservation checks."""
import hashlib
import subprocess

BASE = '5c608324bbc2ef330bcbb304da3f8cd31eaafc2a'
EDITS = {
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs': ('a5b33040650e08440eeb13b3ab1c5eeb25d91eca471ae5b9e4cf92de855b4359', 'ed8c3720d0ea48f0236689b93c37e8823a95411e8b624032150cba95e7f0c936'),
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.LivingStreets.cs': ('ba7215fa1470919966f7641fed2878a9a41caf0fc98a6f08855245c2443842d8', 'ffbdd37cc23444304fe997dded7b4710020a4fcf3d78b42ec599e0019e571321'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.cs': ('679cb10b3f7701335bdc00ad167ac11a9a3d06ebd3ece437402d923c98f9ca76', 'ecc24505b3dc6b9b7a8593b952d590662080fa6041f69759352ef666780e0bf5'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('447c93fed4101ac2644b8fdf705b70cb32d4bc15a888df7788ff31bab681aebf', '743f187a75d83f1f981ca1d7ac92bac3010d37971233c99e82554597cc7b4493'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('4c58d9906c25b4f18caa8349ed34346046f8799af11cca0fc971e123cd4fed13', '4663de33d7ea52309ca4240138c048c40f416f033478896e84a15165fa193b28'),
}
ADDITIONS = {
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs': 'd5e389b6969864eb4c5164054d6114fe14797b0d7ac1531cdccbaddc04f1fca1',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeWR02C3.cs': '6397fc4855b1344f633deccc84dac14de6e21cb4ce0ac953967bc5070bf1a58c',
}


def original_view(root, path, current):
    hashes = EDITS.get(path)
    if hashes is None or hashlib.sha256(current).hexdigest() != hashes[1]:
        return current
    try:
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    return before if hashlib.sha256(before).hexdigest() == hashes[0] else current
