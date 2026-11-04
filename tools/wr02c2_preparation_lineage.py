"""Replay the exact WR02-C2 preparation and fallback repairs for predecessor guards."""
import hashlib
import subprocess

BASE = 'e5378e8b2e59347c88a903b9e07c2440f8c2cef1'
TARGET = 'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.FoundryWR02A.cs'
BEFORE_SHA256 = 'ecb7ea4f98ff094554918aed0dde2b77667166e199f9f57d51055b69c778893b'
AFTER_SHA256 = '36f7a79f97e67c66c6068a051a7e793da403cf1b837975438b44b0601f3f750b'
BEFORE = b'''                Directory.CreateDirectory(FoundryPreparedWR02A + "/Materials"); AssetDatabase.Refresh();
                Dictionary<string, Material> surfaces = new Dictionary<string, Material>();
                foreach (string name in new[] { "Masonry", "CarvedTrim", "SlateTimber", "MossRoot", "AmethystBronze" })
                {
                    Material shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/NeonSyndicate/Art/HeartFR01/Materials/" + name + ".mat");
                    if (shared == null) throw new InvalidDataException("Prepare original Heart shared surface first: " + name);
                    surfaces.Add(name, shared);
                }
'''
AFTER = b'''                Directory.CreateDirectory(FoundryPreparedWR02A + "/Materials"); AssetDatabase.Refresh();
                HeartCatalogFR01 foundation = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(Prepared + "/Catalog.asset");
                if (foundation == null || !foundation.Ready) throw new InvalidDataException("Prepare original Gothic Heart shared surfaces first.");
                Dictionary<string, Material> surfaces = new Dictionary<string, Material>();
                foreach (string name in new[] { "Masonry", "CarvedTrim", "SlateTimber", "MossRoot", "AmethystBronze" })
                {
                    Material shared = AssetDatabase.LoadAssetAtPath<Material>(Prepared + "/" + name + ".mat");
                    if (shared == null || shared.shader == null || !shared.shader.isSupported || shared.mainTexture == null ||
                        shared.GetTexture("_BumpMap") == null || shared.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidDataException("Prepare original Heart shared surface first: " + name);
                    surfaces.Add(name, shared);
                }
'''
RUNTIME_TARGET = 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs'
RUNTIME_BEFORE_SHA256 = 'd6a1f68826cf20a4713e05231f91e9ce22a89ac39c9b3b0928c9fa01750db6a2'
RUNTIME_AFTER_SHA256 = 'fbba13121d6b0eba19474a3adb603203bb2b8cd007ba94ff755281acb78a4bf4'
RUNTIME_BEFORE = b'''                record.assetVariant = "Foundry WR02A / SanctuaryArchive / " + _foundryCatalogWR02A.Fingerprint;
                return _foundryCatalogWR02A;
'''
RUNTIME_AFTER = b'''                string reason = "Foundry source fingerprint missing or incomplete.";
                bool valid = _foundryCatalogWR02A.Fingerprint != null && _foundryCatalogWR02A.Fingerprint.Length == 64;
                if (valid) valid = HeartPlanFR01.RewriteArtBoundsWR01(plan.Family, new Bounds(module.Center, module.Size), out reason);
                if (valid) foreach (GameObject prefab in module.Lods)
                {
                    if (!Passive(prefab)) { reason = "A foundry crown LOD is missing or not passive."; valid = false; break; }
                    if (!ValidPreparedModelV1(prefab, out reason)) { valid = false; break; }
                }
                if (valid)
                {
                    record.assetVariant = "Foundry WR02A / SanctuaryArchive / " + _foundryCatalogWR02A.Fingerprint;
                    return _foundryCatalogWR02A;
                }
                record.assetVariant = "Original WR01 fallback. WR02A crown refused: " + FantasyProofRulesV1.SafeText(reason);
                return _rewriteCatalog;
'''
PROOF_TARGET = 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.ProofV1.cs'
PROOF_BEFORE_SHA256 = 'dbc1da9564fedb6058e405bd953a89d72e85918021592cbe0b2ed8a415840bf5'
PROOF_AFTER_SHA256 = 'e63d26355a4b93211a4007990bc90b1d8839439a25b0f85f96cc65c57d1de1e5'
PROOF_REPAIRS = (
    (b'if (_firstFr01RefusalV1.Length > 0) proof.fr01.firstRefusal', b'if (_firstFr01RefusalV1.Length > 0 && (proof.fr01.prepared || proof.fr01.firstRefusal.Length == 0)) proof.fr01.firstRefusal'),
    (b'if (_firstLivingRefusalV1.Length > 0) proof.fr02a.firstRefusal', b'if (_firstLivingRefusalV1.Length > 0 && (proof.fr02a.prepared || proof.fr02a.firstRefusal.Length == 0)) proof.fr02a.firstRefusal'),
    (b'''            int imported = 0, valid = 0;
            if (catalog != null && catalog.Modules != null)
                foreach (HeartModuleFR01 module in catalog.Modules)
                    if (module != null && module.Lods != null) foreach (GameObject prefab in module.Lods)
                    { imported++; string problem; if (ValidPreparedModelV1(prefab, out problem)) valid++; else if (layer.firstRefusal.Length == 0) layer.firstRefusal = FantasyProofRulesV1.SafeText(problem); }
            layer.preparedModels = valid;
            layer.prepared = catalog != null && (name == "FR01" ? catalog.Ready : catalog.LivingReady) && imported > 0 && valid == imported;
''', b'''            int imported = 0, valid = 0;
            bool ready = catalog != null && (name == "FR01" ? catalog.Ready : catalog.LivingReady);
            if (catalog == null) layer.firstRefusal = name + " prepared catalog missing. Stop Play and run Verify Fantasy World.";
            else if (!ready) layer.firstRefusal = name + " prepared catalog incomplete: " + FantasyProofRulesV1.SafeText(catalog.Reason);
            if (catalog != null && catalog.Modules != null)
                foreach (HeartModuleFR01 module in catalog.Modules)
                    if (module != null && module.Lods != null) for (int lod = 0; lod < module.Lods.Length; lod++)
                    { imported++; string problem; if (ValidPreparedModelV1(module.Lods[lod], out problem)) valid++; else if (layer.firstRefusal.Length == 0) layer.firstRefusal = module.Id + " LOD" + lod + ": " + FantasyProofRulesV1.SafeText(problem); }
            layer.preparedModels = valid;
            layer.prepared = ready && imported > 0 && valid == imported;
'''),
)

CONTRACTS = {
    TARGET: (BEFORE_SHA256, AFTER_SHA256, ((BEFORE, AFTER),)),
    RUNTIME_TARGET: (RUNTIME_BEFORE_SHA256, RUNTIME_AFTER_SHA256, ((RUNTIME_BEFORE, RUNTIME_AFTER),)),
    PROOF_TARGET: (PROOF_BEFORE_SHA256, PROOF_AFTER_SHA256, PROOF_REPAIRS),
}


def original_view(root, path, current):
    contract = CONTRACTS.get(path)
    if contract is None:
        return current
    before_hash, after_hash, repairs = contract
    if hashlib.sha256(current).hexdigest() != after_hash:
        return current
    try:
        original = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    if hashlib.sha256(original).hexdigest() != before_hash:
        return current
    replayed = original
    for before, after in repairs:
        if replayed.count(before) != 1:
            return current
        replayed = replayed.replace(before, after, 1)
    return original if replayed == current else current
