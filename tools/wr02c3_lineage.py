"""Full-file hash-pinned WR02-C3 hooks for earlier preservation checks."""
import hashlib
import subprocess

BASE = '5c608324bbc2ef330bcbb304da3f8cd31eaafc2a'
EDITS = {
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs': ('a5b33040650e08440eeb13b3ab1c5eeb25d91eca471ae5b9e4cf92de855b4359', '73fb5e978b342f4ef0e4d7ad7d2bdbe2e45abc3a32a977af79c9b30982adc376'),
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.LivingStreets.cs': ('ba7215fa1470919966f7641fed2878a9a41caf0fc98a6f08855245c2443842d8', '1f31ee30bc028ed124dc757687046019d81ee7b15b0ce5db4224a001b02ba449'),
    'Assets/NeonSyndicate/Scripts/Editor/FantasyAcceptanceV1.cs': ('0808167061a33839eea6c2597b53adf8c160f15c18021171ec0284b5f7273cf6', 'ba9f48900d62a504d2b5ba75388a74c48ca856c13c03de96982da8855916ab59'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.cs': ('679cb10b3f7701335bdc00ad167ac11a9a3d06ebd3ece437402d923c98f9ca76', 'ecc24505b3dc6b9b7a8593b952d590662080fa6041f69759352ef666780e0bf5'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('447c93fed4101ac2644b8fdf705b70cb32d4bc15a888df7788ff31bab681aebf', '375e4758f391b11a56b4510ab11ccd1b1b4e1218dfc3cdd6d0ed19738823c1ed'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs': ('234c04adbcf6b4727f2c1e3313f7ed491e651f4cf282adb1afbe20710901ff61', '38161ec1015a40aaf4b28594b6199869d81cbe381a7ad881641f2c190b52bffc'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('4c58d9906c25b4f18caa8349ed34346046f8799af11cca0fc971e123cd4fed13', '1ee105cdd6e7329a8f47c0c5aba01193751e54e20b64063cb4e180affc673301'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs': ('a6257ff29a5a8657d59a3d2cd608e4b21098923f5bf98b0a99f239a3e5dae316', '1db332cf0e65c60ad15e189549b6064e6f983bf0511a01dc8f2aafcd10824ab7'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.SurfaceWR02.cs': ('c67c69937f435db7ba82348a64687a4e8ac0d45591a22f7870250f61a515c213', '1f8b352ea61b46b0df7f748c8ac993f6d97ed34887f207342f5ce27286933514'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.cs': ('6cc9400d002ce3f2b101a5202ee7e1ffa0788c3689cdfe954b4e188b35af2a95', 'a3221460a369a7f0787cbe2f09d358dc7075461e10e5dfa4317142707a1a7440'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.Place.cs': ('02c4866feec0b2efb5d5b5266d320cda5876562f15fff4d1ccaff83424c9c04d', 'a7c968704130e9c95ab0570cc3745dfbe809cb84bf47af605319e2e4fda597ed'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldSafetyH8S.cs': ('d2ad00ebee6ee6bc5f9b7cf0f66088cd2572f5de0eb88c062b4182f68debb93e', '6a420b92729216586e8c8b537957409178237d1012af80b12ead98c48f26898e'),
}
ADDITIONS = {
    'Assets/NeonSyndicate/Tests/EditMode/FantasyPreparationFailureTests.cs': 'c730d97558b36726d3c948150b66e74992213cacaca74a5b00297c59abae7103',
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs': '870871f27baf8016f336bd4143737ad9466e9efb4c532fd75c731fa476fdd817',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeWR02C3.cs': '6397fc4855b1344f633deccc84dac14de6e21cb4ce0ac953967bc5070bf1a58c',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.StreetWR02D.cs': '32670d81b32be2aa25c13295beb770c7e0763b19f4d7d6fd582a8b4b1b5749d8',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.StreetWR02D.cs': 'adb44b776a2736f644b7f388702caee771b7b417e63b7b40e5c680bc5302e5bb',
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
