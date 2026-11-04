"""Exact archive frame-probe change, replayed before predecessor preservation checks."""
import hashlib
import subprocess

BASE = '911c141fda7e982063c27cf07cf8e3b8d9ec1c55'
TARGET = 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs'
BEFORE = 'fbba13121d6b0eba19474a3adb603203bb2b8cd007ba94ff755281acb78a4bf4'
AFTER = '48071c48ed7512ae3b99cfd2c3cf8528e2b0f00794db68a3d97d0e7bb4a795b1'
MARKER = b'                if (valid)\n                {\n                    record.assetVariant = "Foundry WR02A / SanctuaryArchive / "'
CALL = b'                if (valid) valid = ProbeFoundryFrameWR02H(plan, out reason);\n'
METHOD_MARKER = b'        private void FillFoundryProofWR02A(FantasyProofSnapshotV1 snapshot)\n'

def original_view(root, path, current):
    if path != TARGET or hashlib.sha256(current).hexdigest() != AFTER:
        return current
    try:
        original = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    if hashlib.sha256(original).hexdigest() != BEFORE or original.count(MARKER) != 1 or original.count(METHOD_MARKER) != 1:
        return current
    # The hash and both insertion points must agree with the reviewed candidate. Extra edits stay visible.
    if current.count(CALL) != 1 or current.count(b'private bool ProbeFoundryFrameWR02H(') != 1:
        return current
    return original
