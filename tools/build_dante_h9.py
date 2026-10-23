#!/usr/bin/env python3
"""Rebuild the H9 Dante source and exports from the H8S study, in order (plain Python driver).

Steps: copy the H8S NPC textures into ProductionH9/NPC; author DanteRookH9.blend (refit toward the gameplay rig,
sockets, boots, short sleeves, apron, tool belt, groom colour, hidden skin removal); export three FBX LODs; validate
them by re-import; seal the receipt. The gameplay rig JSON comes from the integration owner (CastRoster04E look).
Usage: python3 tools/build_dante_h9.py [--blender-python PATH] [--wrap "nice -n 10 flock LOCK"] [--rig-json FILE]
"""
import argparse, shlex, shutil, subprocess, sys
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--blender-python', default='/home/claude/bpy52/bin/python')
p.add_argument('--wrap', default='')
p.add_argument('--rig-json', default='/home/claude/work/dante_gameplay_rig.json')
a = p.parse_args()
root = a.root.resolve()
npc = root / 'Assets/NeonSyndicate/Art/ProductionH9/NPC'
npc.mkdir(parents=True, exist_ok=True)
for name in ('DanteAlbedo.png', 'T_Eye_Brown.png', 'T_Eye_Normal.png', 'T_Superhero_Male_Normal.png'):
    shutil.copyfile(root / 'Assets/NeonSyndicate/Art/ProductionH8S/NPC' / name, npc / name)


def run(name, cmd, blender=False):
    full = (shlex.split(a.wrap) if blender and a.wrap else []) + cmd
    print('==', name, flush=True)
    r = subprocess.run(full, cwd=root, capture_output=True, text=True)
    tail = [ln for ln in (r.stdout + r.stderr).splitlines() if not ln.startswith(('00:', 'EGL'))][-8:]
    print('\n'.join(tail), flush=True)
    if r.returncode != 0:
        sys.exit('step failed: ' + name)


bpy_py = a.blender_python
run('author', [bpy_py, 'tools/author_dante_h9.py', '--', '--rig-json', a.rig_json], True)
run('export', [bpy_py, 'tools/export_forge_cast_h9.py'], True)
fbx = [str(npc / f) for f in ('DanteRookH9.fbx', 'DanteRookH9_LOD1.fbx', 'DanteRookH9_LOD2.fbx')]
run('validate', [bpy_py, 'tools/validate_fbx_h9.py', '--', '--source', 'ArtSource/AscensionH9/DanteRookH9.blend', '--rig', 'DanteRig',
                 '--fbx'] + fbx + ['--stats', str(npc / 'export-stats.json')], True)
run('seal', [sys.executable, 'tools/seal_candidates_h9.py', '--which', 'dante'])
print('H9 Dante build complete. Native Unity import remains NOT RUN.')
