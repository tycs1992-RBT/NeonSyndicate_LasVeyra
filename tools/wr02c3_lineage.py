"""Full-file hash-pinned WR02-C3 hooks for earlier preservation checks."""
import hashlib
import subprocess

BASE = '5c608324bbc2ef330bcbb304da3f8cd31eaafc2a'
EDITS = {
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs': ('a5b33040650e08440eeb13b3ab1c5eeb25d91eca471ae5b9e4cf92de855b4359', '67e865a153a25171c989a77023bb27bcc12ca9c902c655ab6eea983b27b607b1'),
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.LivingStreets.cs': ('ba7215fa1470919966f7641fed2878a9a41caf0fc98a6f08855245c2443842d8', 'ffbdd37cc23444304fe997dded7b4710020a4fcf3d78b42ec599e0019e571321'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.cs': ('679cb10b3f7701335bdc00ad167ac11a9a3d06ebd3ece437402d923c98f9ca76', 'ecc24505b3dc6b9b7a8593b952d590662080fa6041f69759352ef666780e0bf5'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('447c93fed4101ac2644b8fdf705b70cb32d4bc15a888df7788ff31bab681aebf', '375e4758f391b11a56b4510ab11ccd1b1b4e1218dfc3cdd6d0ed19738823c1ed'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs': ('234c04adbcf6b4727f2c1e3313f7ed491e651f4cf282adb1afbe20710901ff61', '38161ec1015a40aaf4b28594b6199869d81cbe381a7ad881641f2c190b52bffc'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('4c58d9906c25b4f18caa8349ed34346046f8799af11cca0fc971e123cd4fed13', '1ee105cdd6e7329a8f47c0c5aba01193751e54e20b64063cb4e180affc673301'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs': ('a6257ff29a5a8657d59a3d2cd608e4b21098923f5bf98b0a99f239a3e5dae316', '1db332cf0e65c60ad15e189549b6064e6f983bf0511a01dc8f2aafcd10824ab7'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.SurfaceWR02.cs': ('c67c69937f435db7ba82348a64687a4e8ac0d45591a22f7870250f61a515c213', '1f8b352ea61b46b0df7f748c8ac993f6d97ed34887f207342f5ce27286933514'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.cs': ('6cc9400d002ce3f2b101a5202ee7e1ffa0788c3689cdfe954b4e188b35af2a95', 'a3221460a369a7f0787cbe2f09d358dc7075461e10e5dfa4317142707a1a7440'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldSafetyH8S.cs': ('d2ad00ebee6ee6bc5f9b7cf0f66088cd2572f5de0eb88c062b4182f68debb93e', '6a420b92729216586e8c8b537957409178237d1012af80b12ead98c48f26898e'),
}
ADDITIONS = {
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs': '870871f27baf8016f336bd4143737ad9466e9efb4c532fd75c731fa476fdd817',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeWR02C3.cs': '6397fc4855b1344f633deccc84dac14de6e21cb4ce0ac953967bc5070bf1a58c',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.StreetWR02D.cs': '32670d81b32be2aa25c13295beb770c7e0763b19f4d7d6fd582a8b4b1b5749d8',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.StreetWR02D.cs': '1a2b9729f2ff2193195869d1cc63c0ef8c6b7a610102b0690dd53a36804e5ea7',
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
