#!/usr/bin/env python3
"""Rebuild every H9 Malik source and export from the H8S source, in order (plain Python driver).

Steps: bake the skin UV position map; copy unchanged H8S textures; author MalikVeyrH9.blend (groom shells and cards,
eyes, garment, hidden skin removal); paint the skin and groom textures; export three FBX LODs; export the motion
JSON and supplied clips; check the motion contract; validate the FBX files by re-import; seal the receipt.
Usage: python3 tools/build_malik_h9.py [--blender-python PATH] [--wrap "nice -n 10 flock LOCK"] [--work DIR]
"""
import argparse, shlex, shutil, subprocess, sys
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--blender-python', default='/home/claude/bpy52/bin/python')
p.add_argument('--wrap', default='', help='prefix for Blender jobs, for example "nice -n 10 flock /path/lock"')
p.add_argument('--work', type=Path, default=None, help='scratch folder for intermediate files (not committed)')
p.add_argument('--skip', default='', help='comma list of step names to skip')
a = p.parse_args()
root = a.root.resolve()
work = (a.work or root / 'Temp/h9-build').resolve()
work.mkdir(parents=True, exist_ok=True)
tex = root / 'Assets/NeonSyndicate/Art/ProductionH9/Textures'
tex.mkdir(parents=True, exist_ok=True)
h8s_tex = root / 'Assets/NeonSyndicate/Art/ProductionH8S/Textures'
skip = set(x for x in a.skip.split(',') if x)


def run(name, cmd, blender=False):
    if name in skip:
        print('skip', name); return
    full = (shlex.split(a.wrap) if blender and a.wrap else []) + cmd
    print('==', name, flush=True)
    r = subprocess.run(full, cwd=root, capture_output=True, text=True)
    tail = [ln for ln in (r.stdout + r.stderr).splitlines() if not ln.startswith(('00:', 'EGL'))][-12:]
    print('\n'.join(tail), flush=True)
    if r.returncode != 0:
        sys.exit('step failed: ' + name)


bpy_py = a.blender_python
run('posmap', [bpy_py, 'tools/bake_uv_position_h9.py', '--', '--blend', 'ArtSource/AscensionH8S/MalikVeyrH8S.blend', '--out', str(work / 'uvpos.npz')], True)
for name in ('Cloth_Albedo.png', 'Cloth_Normal.png', 'Leather_Albedo.png', 'Leather_Normal.png', 'Brows_Albedo.png', 'Eyes_Albedo.png'):
    shutil.copyfile(h8s_tex / name, tex / name)
run('author', [bpy_py, 'tools/author_malik_h9.py', '--', '--strands', str(work / 'groom-strands-h9.npz')], True)
run('skin', [sys.executable, 'tools/make_skin_textures_h9.py', '--src', str(h8s_tex), '--posmap', str(work / 'uvpos.npz'),
             '--out', str(tex), '--margins', str(work / 'lid-margins-h9.json')])
run('groom', [sys.executable, 'tools/make_groom_textures_h9.py', '--out', str(tex), '--strands', str(work / 'groom-strands-h9.npz')])
run('export', [bpy_py, 'tools/export_selected_malik_h9.py'], True)
run('motion', [bpy_py, 'tools/export_motion_h9.py'], True)
run('motion-check', [sys.executable, 'tools/check_motion_h9.py', '--json', 'Assets/NeonSyndicate/Art/ProductionH9/Animations/MalikFullBodyH9.json',
                     '--blend', 'ArtSource/AscensionH9/MalikVeyrH9.blend', '--reference', 'Assets/NeonSyndicate/Art/ProductionH8S/Animations/MalikFullBodyH8S.json'])
models = root / 'Assets/NeonSyndicate/Art/ProductionH9/Models'
materials = ','.join(['H6 Aged bronze', 'H6 Brows', 'H6 Cornea', 'H6 Worn leather', 'H6 Blackened steel', 'H6 Charcoal wool',
                      'H6 Restrained amethyst', 'H8R Textured groom', 'H6 Eyes', 'H8R Skin', 'H6 Pupils', 'H6 Sclera'])
run('validate', [bpy_py, 'tools/validate_fbx_h9.py', '--', '--source', 'ArtSource/AscensionH9/MalikVeyrH9.blend', '--rig', 'MalikRig',
                 '--fbx', str(models / 'MalikVeyrH9.fbx'), str(models / 'MalikVeyrH9_LOD1.fbx'), str(models / 'MalikVeyrH9_LOD2.fbx'),
                 '--stats', str(models / 'export-stats.json'), '--materials', materials, '--semantic', '--skins', '12'], True)
run('seal', [sys.executable, 'tools/seal_candidates_h9.py', '--which', 'malik'])
print('H9 Malik build complete. Native Unity import remains NOT RUN.')
