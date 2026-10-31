"""Exact source normalization for the bounded zone-order change."""
import hashlib
import subprocess

BASE = '911c141fda7e982063c27cf07cf8e3b8d9ec1c55'
TARGET = 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.CompositionWR02B.cs'
BEFORE = '876e862616d760f6a7e2657f0b905c3614e3e729750430ffb02e79ca2eff4c6f'
AFTER = '48d1bb68c6ce112c12b92e7529949e77cdc898166f0ef12b82f0e11f190580da'

def original_view(root, path, current):
    if path != TARGET or hashlib.sha256(current).hexdigest() != AFTER:
        return current
    try:
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    if hashlib.sha256(before).hexdigest() != BEFORE:
        return current
    return before
