"""Authorized physical preview contracts plus mutation tests. Not Unity execution.

Historical hashes are unchanged. Separate live checks require the new paired
collision policy, actual geometry, retained service sources and explicit activation.
"""
import argparse
import tempfile
import json
import subprocess
import unittest
from pathlib import Path
from science_district_lineage import BASE, ADDITIONS, contract, original_view
from h5_historical_continuations import original_view as historical_view, inherited_tests_preserved

ROOT = Path(__file__).resolve().parents[1]
R = 'Assets/NeonSyndicate/Scripts/Runtime/'

def read(tail):
    return (ROOT / (R + tail)).read_text()

def paired_contract(text):
    required = (
        'originalCollision,true)', 'art.Solid(),false)',
        'ScienceChangedZones(originalCollision,art.Solid())',
        'ScienceSelection(requested,valid,current,',
        'selection == ScienceDistrictSelection.WaitForClearance',
        'next = selection == ScienceDistrictSelection.Prepared;',
        'visual.Candidate.SetActive(next)', 'pair.CandidateSolid.SetActive(next)',
        'visual.Legacy.SetActive(!next)', 'pair.OriginalSolid.SetActive(!next)',
        'WorldRewriteSelectedWR01 && !_compare && !_suspended',
        'if (current != next) Physics.SyncTransforms()',
        'ScienceSolidValid(pair.OriginalSolid) && ScienceSolidValid(pair.CandidateSolid)',
        'collider.enabled && !collider.isTrigger && collider.sharedMesh != null',
        'material.shader.isSupported', 'material.mainTexture == null',
        'GetTexture("_BumpMap") == null', 'GetTexture("_MetallicGlossMap") == null',
    )
    return all(token in text for token in required) and text.index('WaitForClearance') < text.index('visual.Candidate.SetActive(next)')

class ScienceDistrictContracts(unittest.TestCase):
    def test_literal_hooks_preserve_every_other_byte_and_reject_mutations(self):
        for path in contract()['paths']:
            with self.subTest(path=path):
                current = (ROOT / path).read_bytes()
                before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)
                self.assertEqual(original_view(ROOT, path, current), before)
                for bad in (current + b'\n// unreviewed\n', current[:-1], current.replace(b'return', b'throw', 1)):
                    if bad != current:
                        self.assertEqual(original_view(ROOT, path, bad), bad)

    def test_mutations_cannot_drop_or_invert_one_physical_owner(self):
        source = read('World/HeartFR01/HeartPresentationFR01.ScienceDistrict.cs')
        self.assertTrue(paired_contract(source))
        for call in ['pair.CandidateSolid.SetActive(next)', 'pair.OriginalSolid.SetActive(!next)',
                     'ScienceChangedZones(originalCollision,art.Solid())', 'Physics.SyncTransforms()']:
            self.assertFalse(paired_contract(source.replace(call, 'RemovedCall()')))
        self.assertFalse(paired_contract(source.replace('pair.OriginalSolid.SetActive(!next)', 'pair.OriginalSolid.SetActive(next)')))

    def test_physical_service_capture_retains_original_interior_and_minimum_guards(self):
        source = read('ArtInhabitedCity04C/VenueView04C.ScienceDistrict.cs')
        for token in ['!Game.Layout.GothicCivicH5', 'ScienceServiceFrame(', 'GothicCivicH5.Structure(structure,binding,mineral)',
                      'GothicHeartH3.Structure(structure,binding)', 'collision.Append(structure);collision.Append(mineral)',
                      'civic?GothicCivicH5.Plan(binding):GothicHeartH3.ServicePlan(binding)',
                      'near.Add("amethyst",new MeshBuilder(1));far.Add("amethyst",new MeshBuilder(1))',
                      'art.Surface("amethyst").Append(mineral)', 'catch(Exception error)', 'return false;']:
            self.assertIn(token, source)
        self.assertNotIn('near.Add("amethyst",mineral)', source, 'Retained core must not alias the mutable old exterior.')

    def test_saves_services_quests_layout_and_actor_sources_remain_exact(self):
        paths = ['Assets/NeonSyndicate/Resources', 'Assets/NeonSyndicate/Art', 'ArtSource', 'Packages', 'ProjectSettings']
        paths += [R + p for p in ['Gameplay', 'Characters', 'Vehicles', 'Creatures', 'Combat', 'World/CityLayout.Gothic04OB.cs',
                  'World/GothicCivicH5.cs', 'World/GothicHeartH3.cs', 'World/CityBuilder.Gothic04OB.cs', 'World/GothicRealmGeometry04OB.cs']]
        self.assertEqual(subprocess.check_output(['git', 'diff', BASE, '--name-only', '--'] + paths, cwd=ROOT, text=True), '')

    def test_actual_play_entry_and_save_restoration_stay_authoritative(self):
        for path in ['UI/MenuSystem.cs', 'UI/VisibleRealityV0.FantasySessionV1.cs', 'World/HeartFR01/HeartPresentationFR01.ProofV1.cs']:
            self.assertEqual((ROOT/(R+path)).read_bytes(),subprocess.check_output(['git','show',BASE+':'+R+path],cwd=ROOT))
        self.assertIn('"PLAY FANTASY WORLD"',read('UI/MenuSystem.cs'))
        session=read('UI/VisibleRealityV0.FantasySessionV1.cs')
        self.assertIn('HeartFR01.BeginFantasyPreviewV1()',session)
        self.assertIn('_fantasyHeartV1.EndFantasyPreviewV1()',session)
        self.assertIn('public void Capture(SaveData data) { }',session)

    def test_exact_addition_paths_have_metadata_and_no_new_gameplay_state(self):
        for path in ADDITIONS:
            self.assertTrue((ROOT/path).is_file(),path)
            self.assertTrue((ROOT/(path+'.meta')).is_file(),path)
            source=(ROOT/path).read_text()
            for token in ['PlayerPrefs.Set', 'SaveSystem.Save(', 'new QuestDef', 'RewardLedger', 'Physics.IgnoreCollision', 'Physics.gravity =']:
                self.assertNotIn(token,source,path)

    def test_geometry_and_route_checks_remain_in_the_executable_fixture(self):
        source=(ROOT/'Assets/NeonSyndicate/Tests/EditMode/ScienceDistrictTests.cs').read_text()
        for test in ['ActualMeshesAreFiniteBoundedAndHaveNoDegenerateTriangles', 'EveryVisibleSolidTriangleHasTheSamePhysicalTriangle',
                     'DoorAndExistingServiceApproachRemainOpen', 'ArchiveRetainsActualDaisGalleryAndRoofReceiptWithoutDisplacement',
                     'ClearanceExcludesRetainedRoomsAndIncludesOldTallRoofs','ApproachGeometryKeepsTheExistingConnectedRouteWalkingWidths',
                     'CanonicalDistrictAdmitsConnectedApproachSites', 'OrdinaryNightIsSeparateFromEarnedResonance']:
            self.assertIn(test,source)

    def test_historical_h5_changes_are_exact_and_keep_schema_guarantees(self):
        self.assertTrue(inherited_tests_preserved(ROOT))
        from h5_historical_continuations import BASE as old_base
        for path in json.loads((ROOT/'tools/h5_historical_continuations.json').read_text())['paths']:
            current=(ROOT/path).read_bytes().replace(b'            AuthoredBodyH8(ref p,ref b,dt);\n',b'')
            expected=subprocess.check_output(['git','show',old_base+':'+path],cwd=ROOT)
            self.assertEqual(historical_view(ROOT,path,current),expected)
            bad=current+b'\n// unreviewed\n'
            self.assertEqual(historical_view(ROOT,path,bad),bad)

def run_selection(dotnet):
    runtime=Path(dotnet).resolve()
    compilers=sorted(runtime.parent.glob('sdk/8.*/Roslyn/bincore/csc.dll'))
    refs=sorted(runtime.parent.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))
    if not compilers or not refs:raise RuntimeError('The selection harness requires a .NET 8 SDK.')
    with tempfile.TemporaryDirectory(prefix='science-clearance-') as folder:
        directory=Path(folder);assembly=directory/'Science.dll'
        files=[ROOT/'tools/science_clearance_harness.cs',ROOT/(R+'World/HeartFR01/HeartPlanFR01.ScienceClearance.cs')]
        arguments=['-nologo','-target:exe','-langversion:9','-out:'+str(assembly)]+['-r:'+str(p) for p in refs]+[str(p) for p in files]
        response=directory/'compile.rsp';response.write_text('\n'.join('"'+argument+'"' for argument in arguments))
        subprocess.run([str(runtime),str(compilers[-1]),'@'+str(response)],check=True,cwd=ROOT)
        (directory/'Science.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
        subprocess.run([str(runtime),str(assembly)],check=True,cwd=ROOT)

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet')
    args,remaining=parser.parse_known_args()
    result=unittest.main(argv=[__file__]+remaining,exit=False)
    if not result.result.wasSuccessful():raise SystemExit(1)
    if args.dotnet:run_selection(args.dotnet)

