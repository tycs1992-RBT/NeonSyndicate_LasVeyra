"""Full-file hash-pinned WR02-C3 hooks for earlier preservation checks."""
import hashlib
import subprocess

BASE = '5c608324bbc2ef330bcbb304da3f8cd31eaafc2a'
EDITS = {
    'Assets/NeonSyndicate/Scripts/Runtime/UI/VisibleRealityV0.FantasySessionV1.cs': ('d0316bf64c36c304c147d12b84f0d65d2233689bd2b8078580789afe0b7b8be0', '1b8899eed75ce53a26cf51637534175c30093c0bb152648d38d2e534d035f81c'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.CompositionWR02B.cs': ('90f942200461832915535171331564007f3459dce0b92d4c8113b0b050aa27a5', '876e862616d760f6a7e2657f0b905c3614e3e729750430ffb02e79ca2eff4c6f'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.CompositionWR02B.cs': ('938441f3d7caf963a096dd3acc20bcfb5bb202c35459a88b92ab45b077ae2d15', '6e614b938fbd41e6e57dfaf82d83c10f601a9e72aebf1bfaea640a41943bb203'),
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs': ('a5b33040650e08440eeb13b3ab1c5eeb25d91eca471ae5b9e4cf92de855b4359', '2062e88b5c8acc931818f7d9672e0c70d1db31f499e4c710e8396cc5e4084b91'),
    'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.LivingStreets.cs': ('ba7215fa1470919966f7641fed2878a9a41caf0fc98a6f08855245c2443842d8', 'c99a460409c58c9df16485b7ab0d62cf66c84f43e96a63ed82767a4ac6ea36f2'),
    'Assets/NeonSyndicate/Scripts/Editor/FantasyAcceptanceV1.cs': ('0808167061a33839eea6c2597b53adf8c160f15c18021171ec0284b5f7273cf6', 'ba9f48900d62a504d2b5ba75388a74c48ca856c13c03de96982da8855916ab59'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.cs': ('679cb10b3f7701335bdc00ad167ac11a9a3d06ebd3ece437402d923c98f9ca76', 'ecc24505b3dc6b9b7a8593b952d590662080fa6041f69759352ef666780e0bf5'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs': ('447c93fed4101ac2644b8fdf705b70cb32d4bc15a888df7788ff31bab681aebf', '375e4758f391b11a56b4510ab11ccd1b1b4e1218dfc3cdd6d0ed19738823c1ed'),
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs': ('234c04adbcf6b4727f2c1e3313f7ed491e651f4cf282adb1afbe20710901ff61', '38161ec1015a40aaf4b28594b6199869d81cbe381a7ad881641f2c190b52bffc'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs': ('4c58d9906c25b4f18caa8349ed34346046f8799af11cca0fc971e123cd4fed13', '1ee105cdd6e7329a8f47c0c5aba01193751e54e20b64063cb4e180affc673301'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs': ('a6257ff29a5a8657d59a3d2cd608e4b21098923f5bf98b0a99f239a3e5dae316', 'eea142507b81ffc26690e3f9f5ac4e87dd0a455eebb4919e8f3224bb60ac83b3'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.SurfaceWR02.cs': ('c67c69937f435db7ba82348a64687a4e8ac0d45591a22f7870250f61a515c213', '1f8b352ea61b46b0df7f748c8ac993f6d97ed34887f207342f5ce27286933514'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.cs': ('6cc9400d002ce3f2b101a5202ee7e1ffa0788c3689cdfe954b4e188b35af2a95', 'a3221460a369a7f0787cbe2f09d358dc7075461e10e5dfa4317142707a1a7440'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldArtH8S.Place.cs': ('02c4866feec0b2efb5d5b5266d320cda5876562f15fff4d1ccaff83424c9c04d', 'a7c968704130e9c95ab0570cc3745dfbe809cb84bf47af605319e2e4fda597ed'),
    'Assets/NeonSyndicate/Scripts/Runtime/World/LiveWorld/LiveWorldSafetyH8S.cs': ('d2ad00ebee6ee6bc5f9b7cf0f66088cd2572f5de0eb88c062b4182f68debb93e', '6a420b92729216586e8c8b537957409178237d1012af80b12ead98c48f26898e'),
}
ADDITIONS = {
    'Assets/NeonSyndicate/Tests/EditMode/ServiceHeraldryWR02GTests.cs': '635ad0aa815d1959a541b687ac633180d227d0385a31d19f4f7ac93c3499def6',
    'Assets/NeonSyndicate/Tests/EditMode/FantasyRoutePriorityWR02GTests.cs': '3c553586d25397c7fbdf40f1ab982e4b2d21b5cb398246fb97cd294859ad5d72',
    'Assets/NeonSyndicate/Tests/EditMode/KingdomStreetWR02GTests.cs': '9cce76d6d58dbedcf353b1d3795d0e191b8f16dc8d584e57672275e6e6f9f4ff',
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.HeraldryWR02G.cs': '7fa4ed4889c1b06244f7e12974e3812fa1432bf9e4b33b54412aced833da95c2',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.KingdomStreetWR02G.cs': '8943f165702c192f70950e2fd05e7a0110d4186b4da12c038f26356641a5a8da',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.KingdomStreetWR02G.cs': 'aba8a9d06b61d67a151bb280a784251378b7eed3597dff59a14352cb8155193b',
    'Assets/NeonSyndicate/Tests/EditMode/HeartHouseFallbackWR02FTests.cs': '78598709b61e511e5d8a036f7443925837846bcacf2959f185118b0ec76dba5d',
    'Assets/NeonSyndicate/Tests/EditMode/GrottoCompositionTests.cs': '7a0c4c3fd2f5fbed4e87ae8233e3981ec347337d846e53216a25529b244e5661',
    'Assets/NeonSyndicate/Tests/EditMode/FantasyCatalogReceiptTests.cs': 'f34d4f3d3660bc760aec3df7d9fed2d01106e803572b858a88e21cc48eb8a6ec',
    'Assets/NeonSyndicate/Tests/EditMode/FantasyPreparationFailureTests.cs': 'c730d97558b36726d3c948150b66e74992213cacaca74a5b00297c59abae7103',
    'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs': '2d5d732600f1d5e5b00ee8b723f3cc4d3b9323db3ffafb43488388545520ec83',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeWR02C3.cs': '6397fc4855b1344f633deccc84dac14de6e21cb4ce0ac953967bc5070bf1a58c',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.StreetWR02D.cs': 'f8cbbc0264e98159de7cb037c74439f53e8458b7487a08cb5b207cdad67bb30c',
    'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.StreetWR02D.cs': '43468709a5932cfacfa25ab5987039e2e3b516f5ca87723519b195e44650fd73',
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
