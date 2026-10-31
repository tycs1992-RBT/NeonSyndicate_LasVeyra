"""Source contracts and an optional execution of the actual engine-free Forge plan.

The optional .NET harness is not a Unity import, physics test, or playtest.
"""
import argparse
import hashlib
import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from xml.sax.saxutils import escape

from wr02c3_lineage import ADDITIONS, BASE, EDITS, original_view

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/NeonSyndicate'
LAYOUT = ASSETS / 'Resources/InhabitedCity04C/Layouts/forge.json'
PLAN = ASSETS / 'Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeWR02C3.cs'
HARNESS = ROOT / 'tools/forge_workroom_wr02c3_harness.cs'


def read(path):
    return (ROOT / path).read_text(encoding='utf-8')


class ForgeAndPreparationContracts(unittest.TestCase):
    def test_preservation_normalizer_refuses_unknown_changes(self):
        from wr02h_route_lineage import original_view as route_original_view
        from wr02i_lineage import original_view as forge_original_view
        for path, (before_hash, after_hash) in EDITS.items():
            current = route_original_view(ROOT, path, forge_original_view(ROOT, path, (ROOT / path).read_bytes()))
            self.assertEqual(hashlib.sha256(current).hexdigest(), after_hash, path)
            original = original_view(ROOT, path, current)
            self.assertEqual(hashlib.sha256(original).hexdigest(), before_hash, path)
            changed = current + b'\n// unexpected mutation\n'
            self.assertEqual(original_view(ROOT, path, changed), changed, path)
        for path, expected in ADDITIONS.items():
            content = (ROOT / path).read_bytes()
            self.assertEqual(hashlib.sha256(content).hexdigest(), expected, path)
            self.assertNotEqual(hashlib.sha256(content + b'\n// unexpected mutation\n').hexdigest(), expected, path)

    def test_real_service_and_staff_remain_in_the_original_layout(self):
        layout = json.loads(LAYOUT.read_text(encoding='utf-8'))
        self.assertEqual(layout['id'], 'forge')
        anchors = {a['id']: a['position'] for a in layout['anchors']}
        self.assertEqual(anchors['service'], [-4.5, 0.012, 8.2])
        self.assertEqual(anchors['staff'], [5.3, 0.012, 10])
        original = subprocess.check_output(['git', 'show', BASE + ':' + str(LAYOUT.relative_to(ROOT))], cwd=ROOT)
        self.assertEqual(LAYOUT.read_bytes(), original)

    def test_original_collision_is_outside_replacement_and_no_new_collider_is_added(self):
        venue = read('Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.cs')
        self.assertIn('BoxCollider(node.id, at, size, rotation)', venue)
        for path in ADDITIONS:
            code = read(path)
            self.assertNotIn('AddComponent<BoxCollider>', code)
            self.assertNotIn('AddComponent<MeshCollider>', code)
            self.assertNotIn('AddComponent<CapsuleCollider>', code)
        replacement = read('Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs')
        self.assertIn('candidate.Transform.gameObject.SetActive(false)', replacement)
        self.assertIn('main.PartMeshes(source.Mesh, source.Mesh, source.Material', replacement)
        self.assertIn('heart.RegisterRewriteVisual(', replacement)

    def test_unusable_cached_prefabs_cannot_pass_preparation(self):
        setup = read('Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs')
        self.assertIn('filters.Length==0 || renderers.Length==0', setup)
        self.assertIn('filter.sharedMesh==null || filter.sharedMesh.vertexCount<3', setup)
        self.assertIn('renderer.sharedMaterials.Length==0', setup)
        self.assertIn('!material.shader.isSupported', setup)
        self.assertIn('importer.addCollider=false', setup)
        for path in ('Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.cs',
                     'Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.LivingStreets.cs'):
            code = read(path)
            self.assertIn('ModelImporterMaterialImportMode.ImportViaMaterialDescription', code)
            self.assertIn('slots.Length==0', code)
            self.assertIn('GetComponentsInChildren<MeshRenderer>(true).Length==0', code)
            self.assertIn('catalog.Modules=modules.ToArray();if(!PreparedSurfacesReady(catalog))throw', code)

    def test_quest_service_and_save_authorities_remain_untouched(self):
        paths = subprocess.check_output(['git', 'diff', '--name-only', BASE], cwd=ROOT, text=True).splitlines()
        for path in paths:
            self.assertNotIn('/SaveSystem', path)
            self.assertNotIn('/QuestRunner', path)
            self.assertNotIn('/ForgeStations', path)
            self.assertNotEqual(path, str(LAYOUT.relative_to(ROOT)))


def run_dotnet(dotnet):
    with tempfile.TemporaryDirectory(prefix='wr02c3-forge-') as temp:
        project = Path(temp) / 'ForgePlan.csproj'
        project.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
            '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
            '</PropertyGroup><ItemGroup><Compile Include="' + escape(str(PLAN)) + '" />'
            '<Compile Include="' + escape(str(HARNESS)) + '" /></ItemGroup></Project>', encoding='utf-8')
        subprocess.run([dotnet, 'run', '--project', str(project), '--', str(LAYOUT)], cwd=temp, check=True, timeout=120)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet', help='also compile and execute the real Forge plan with minimal non-Unity stubs')
    args, remaining = parser.parse_known_args()
    result = unittest.main(argv=[__file__] + remaining, exit=False)
    if not result.result.wasSuccessful():
        raise SystemExit(1)
    if args.dotnet:
        run_dotnet(args.dotnet)
    else:
        print('Forge plan executable harness: NOT RUN (no --dotnet). Unity: NOT RUN.')
