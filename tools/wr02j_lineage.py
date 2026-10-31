"""Only complete reviewed supported-district hooks normalize to the preserved starting source."""
import hashlib
import subprocess

BASE = '70e5277e3de0c969bc97d534d9965653af189129'
EDITS = {'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs': ('eea142507b81ffc26690e3f9f5ac4e87dd0a455eebb4919e8f3224bb60ac83b3', '4b6a0e39c08199ba8dec3f8d5b061cc7259db4edd90cf177b911c3060fc2463a'), 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('8d8d1ea64229d7100d26e618b4a2228aefa607c5c24257022d62bbde6d6e2e6f', '4c74c97da91cea1a95d238f56c1e30555710777142141db1bfd7e1e862cc182f'), 'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('5ef912f2665d7224df0466f4edee32ddade1127437cef5ecd8fecb25914275ea', 'dcb702c961eea47dc27f2b5771b521a2fc62aca4628254890c872ae906f67852')}
ADDITIONS = {'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.SupportedArchiveWR02J.cs': 'd0ac1f9de3d237caaa1a96d460f3838bddcdcef4e1bd72cb8d3cb0c8474aae57', 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.SupportedDistrictWR02J.cs': '9332d6e0874ef2fb0e8935af1ee6e6b3f9ed49d3dcd3dd616972521722287207', 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.SupportedDistrictWR02J.cs': 'd6bc94dbc8f65d4dfd67a1cd9e75a206c8e2e2751cbd0670f06045fd422fa8c6', 'Assets/NeonSyndicate/Tests/EditMode/SupportedDistrictWR02JTests.cs': '6efa7bb463c59409de4ca9381d38c73f5e59da9f72dc6852310306631c6c5758', 'Assets/NeonSyndicate/Tests/EditMode/SupportedDistrictNativeWR02JTests.cs': '733ebfd820ccae13781a02cc94d30532e7c49a1467e6dbe21b964cf19725b973'}

def original_view(root, path, current):
    pair = EDITS.get(path)
    if pair is None or hashlib.sha256(current).hexdigest() != pair[1]:
        return current
    try:
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    return before if hashlib.sha256(before).hexdigest() == pair[0] else current
