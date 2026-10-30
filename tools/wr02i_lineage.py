"""Replay only the three complete Forge visual integration hooks. Unrecognized edits remain visible."""
import hashlib
import subprocess

BASE = 'a910c2ae86f1f3de4aee575af88fb38db5b816b1'
EDITS = {
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs': ('38161ec1015a40aaf4b28594b6199869d81cbe381a7ad881641f2c190b52bffc', 'bd18d33c872212abfc33e5639624e1c1513827297fd1c2ea1cd1b48dd4657f62'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('375e4758f391b11a56b4510ab11ccd1b1b4e1218dfc3cdd6d0ed19738823c1ed', '5ef912f2665d7224df0466f4edee32ddade1127437cef5ecd8fecb25914275ea'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('1ee105cdd6e7329a8f47c0c5aba01193751e54e20b64063cb4e180affc673301', '8d8d1ea64229d7100d26e618b4a2228aefa607c5c24257022d62bbde6d6e2e6f'),
}
ADDITIONS = {
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeCourtWR02I.cs': '790de78a2cdfa7516f11f8a5df2790a918edb3191fd335681cbe66eef8874032',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeCourtWR02I.cs': '0e5feeaa08f24c388d0792aea537dfc9bab4ffc713b9bd0937e9fc87af75ff22',
    'Assets/NeonSyndicate/Tests/EditMode/ForgeCourtWR02ITests.cs': 'ebf77bb1e9e074e13a28c4fd2f1a01e9fc6f834359c0a453d149a96c986ac050',
}


def original_view(root, path, current):
    pair = EDITS.get(path)
    if pair is None or hashlib.sha256(current).hexdigest() != pair[1]:
        return current
    try:
        original = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    return original if hashlib.sha256(original).hexdigest() == pair[0] else current
