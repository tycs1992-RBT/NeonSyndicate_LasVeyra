#!/usr/bin/env python3
"""Python tests of package integrity and a reference implementation of installer policy.
Does NOT execute PowerShell, C#, Unity or Windows. Run: python this.py --package PATH --baseline PATH
"""
from pathlib import Path
import argparse, copy, hashlib, json, ntpath, re, shutil, tempfile, unittest


def digest(path, mode):
    if not path.is_file(): return None
    data = path.read_bytes()
    if mode == 'text': data = data.decode('utf-8-sig').replace('\r\n', '\n').replace('\r', '\n').encode('utf-8')
    elif mode != 'binary': raise ValueError('bad hash mode')
    return hashlib.sha256(data).hexdigest()


def inside(root, rel):
    if not isinstance(rel, str) or not rel or ntpath.isabs(rel) or ':' in rel:
        raise ValueError('unsafe path')
    parts = re.split(r'[\\/]', rel)
    if any(p in ('', '.', '..') or p.rstrip(' .') != p for p in parts): raise ValueError('unsafe segment')
    path = root
    for p in parts:
        path = path/p
        if path.is_symlink(): raise ValueError('linked path')
    if root.resolve() not in path.resolve().parents: raise ValueError('outside root')
    return path


def validate(package, project, manifest):
    if manifest['build'] != '02I': raise ValueError('wrong manifest')
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        if not (project/folder).is_dir(): raise ValueError('wrong root')
    if (project/'.git/HEAD').read_text().strip() != 'ref: refs/heads/'+manifest['branch']: raise ValueError('wrong branch')
    version = re.search(r'^m_EditorVersion:\s*([^\r\n]+)', (project/'ProjectSettings/ProjectVersion.txt').read_text(), re.M)
    if version is None or version.group(1).strip() != manifest['expected_editor']: raise ValueError('wrong editor')
    for d in manifest['dependencies']:
        if digest(inside(project,d['path']),d['mode']) != d['sha256']: raise ValueError('dependency')
    seen=set(); pending=[]
    for f in manifest['files']:
        if f['path'].casefold() in seen: raise ValueError('duplicate')
        seen.add(f['path'].casefold())
        source=inside(package/'Payload',f['path']); dest=inside(project,f['path'])
        if digest(source,f['mode']) != f['new_sha256']: raise ValueError('payload integrity')
        if dest.is_dir(): raise ValueError('directory collision')
        current=digest(dest,f['mode'])
        if current == f['new_sha256']: continue
        if f['old_sha256'] is None:
            if dest.exists(): raise ValueError('existing new file')
        elif current != f['old_sha256']: raise ValueError('local edit')
        pending.append(f)
    return pending


def apply_reference(package,project,manifest,fail_after=None):
    pending=validate(package,project,manifest); originals={};written=[]
    for f in pending:
        dest=inside(project,f['path']); originals[f['path']]=dest.read_bytes() if dest.is_file() else None
    try:
        for i,f in enumerate(pending):
            if fail_after is not None and i == fail_after: raise OSError('injected copy failure')
            dest=inside(project,f['path']);dest.parent.mkdir(parents=True,exist_ok=True)
            written.append(f)
            shutil.copyfile(inside(package/'Payload',f['path']),dest)
            if digest(dest,f['mode']) != f['new_sha256']: raise ValueError('bad copied bytes')
    except Exception:
        for f in reversed(written):
            dest=inside(project,f['path']);original=originals[f['path']]
            if original is None:dest.unlink(missing_ok=True)
            else:dest.write_bytes(original)
        raise
    return originals,pending


def restore_reference(project,originals,entries):
    # Validation of EVERY destination happens before the first restore write.
    for f in entries:
        if digest(inside(project,f['path']),f['mode']) != f['new_sha256']:raise ValueError('post-install edit')
    for f in entries:
        dest=inside(project,f['path']); original=originals[f['path']]
        if original is None:dest.unlink()
        else:dest.write_bytes(original)


class PolicyTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        self.project=self.root/'Project';self.package=PACKAGE
        self.manifest=copy.deepcopy(MANIFEST)
        for d in ('Assets','Packages','ProjectSettings','.git'):(self.project/d).mkdir(parents=True,exist_ok=True)
        (self.project/'.git/HEAD').write_text('ref: refs/heads/'+self.manifest['branch']+'\n')
        paths={'ProjectSettings/ProjectVersion.txt'} | set(d['path'] for d in self.manifest['dependencies'])
        paths.update(f['path'] for f in self.manifest['files'] if f['old_sha256'] is not None)
        for rel in paths:
            dest=inside(self.project,rel);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(inside(BASELINE,rel),dest)
    def tearDown(self):self.temp.cleanup()
    def new_entry(self):return next(f for f in self.manifest['files'] if f['old_sha256'] is None)
    def old_entry(self):return next(f for f in self.manifest['files'] if f['old_sha256'] is not None)
    def put(self,entry,data):
        dest=inside(self.project,entry['path']);dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data);return dest
    def snapshot(self):return {p.relative_to(self.project).as_posix():p.read_bytes() for p in self.project.rglob('*') if p.is_file()}
    def assert_no_write_reject(self):
        before=self.snapshot()
        with self.assertRaises((ValueError,FileNotFoundError)):validate(self.package,self.project,self.manifest)
        self.assertEqual(before,self.snapshot())
    def test_manifest_lists_exact_payload(self):
        actual={p.relative_to(PACKAGE/'Payload').as_posix() for p in (PACKAGE/'Payload').rglob('*') if p.is_file()}
        self.assertEqual(actual,{f['path'] for f in self.manifest['files']})
    def test_approved_baseline_validates(self):self.assertEqual(len(validate(self.package,self.project,self.manifest)),len(self.manifest['files']))
    def test_every_payload_hash_matches(self):
        for f in self.manifest['files']:self.assertEqual(digest(inside(PACKAGE/'Payload',f['path']),f['mode']),f['new_sha256'])
    def test_second_apply_is_noop(self):
        apply_reference(self.package,self.project,self.manifest)
        before=self.snapshot();self.assertEqual(validate(self.package,self.project,self.manifest),[]);self.assertEqual(before,self.snapshot())
    def test_partial_previous_apply_is_supported(self):
        f=self.new_entry();self.put(f,inside(PACKAGE/'Payload',f['path']).read_bytes())
        self.assertEqual(len(validate(self.package,self.project,self.manifest)),len(self.manifest['files'])-1)
    def test_modified_existing_code_refused(self):
        f=self.old_entry();self.put(f,inside(self.project,f['path']).read_bytes()+b'// user work\n');self.assert_no_write_reject()
    def test_conflicting_new_file_refused(self):self.put(self.new_entry(),b'user work');self.assert_no_write_reject()
    def test_missing_existing_code_refused(self):inside(self.project,self.old_entry()['path']).unlink();self.assert_no_write_reject()
    def test_changed_dependency_refused(self):
        d=self.manifest['dependencies'][-1];p=inside(self.project,d['path']);p.write_bytes(p.read_bytes()+b'// changed\n');self.assert_no_write_reject()
    def test_missing_dependency_refused(self):inside(self.project,self.manifest['dependencies'][-1]['path']).unlink();self.assert_no_write_reject()
    def test_wrong_branch_refused(self):(self.project/'.git/HEAD').write_text('ref: refs/heads/main\n');self.assert_no_write_reject()
    def test_detached_head_refused(self):(self.project/'.git/HEAD').write_text('a'*40);self.assert_no_write_reject()
    def test_wrong_editor_refused(self):(self.project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.0.1f1\n');self.assert_no_write_reject()
    def test_text_crlf_bom_canonicalization(self):
        f=self.old_entry();original=inside(self.project,f['path']).read_text(encoding='utf-8-sig')
        self.put(f,b'\xef\xbb\xbf'+original.replace('\r\n','\n').replace('\n','\r\n').encode())
        self.assertEqual(len(validate(self.package,self.project,self.manifest)),len(self.manifest['files']))
    def test_binary_bytes_not_normalized(self):
        p=self.root/'bytes.png';p.write_bytes(b'abc\r\ndef');before=digest(p,'binary');p.write_bytes(b'abc\ndef');self.assertNotEqual(before,digest(p,'binary'))
    def test_duplicate_case_path_refused(self):
        f=copy.deepcopy(self.manifest['files'][0]);f['path']=f['path'].upper();self.manifest['files'].append(f);self.assert_no_write_reject()
    def test_traversal_path_refused(self):self.manifest['files'][0]['path']='../save.json';self.assert_no_write_reject()
    def test_absolute_windows_path_refused(self):self.manifest['files'][0]['path']='C:\\games\\save.json';self.assert_no_write_reject()
    def test_alternate_data_stream_refused(self):self.manifest['files'][0]['path']='Assets/a.cs:stream';self.assert_no_write_reject()
    def test_trailing_dot_refused(self):self.manifest['files'][0]['path']='Assets/data./a.cs';self.assert_no_write_reject()
    def test_empty_path_segment_refused(self):self.manifest['files'][0]['path']='Assets//data.cs';self.assert_no_write_reject()
    def test_symlink_refused(self):
        external=self.root/'outside';external.mkdir();(self.project/'Assets/link').symlink_to(external,target_is_directory=True)
        with self.assertRaises(ValueError):inside(self.project,'Assets/link/file.cs')
    def test_directory_at_file_destination_refused(self):
        inside(self.project,self.new_entry()['path']).mkdir(parents=True);self.assert_no_write_reject()
    def test_wrong_manifest_refused(self):self.manifest['build']='wrong';self.assert_no_write_reject()
    def test_corrupt_package_hash_refused(self):self.manifest['files'][0]['new_sha256']='0'*64;self.assert_no_write_reject()
    def test_failure_after_half_copy_restores_original_bytes(self):
        before=self.snapshot()
        with self.assertRaises(OSError):apply_reference(self.package,self.project,self.manifest,fail_after=len(self.manifest['files'])//2)
        self.assertEqual(before,self.snapshot())
    def test_full_restore_retains_unrelated_files(self):
        unrelated=self.project/'Assets/_Recovery/keep.unity';unrelated.parent.mkdir(parents=True);unrelated.write_text('saved user scene')
        before=self.snapshot();originals,entries=apply_reference(self.package,self.project,self.manifest)
        restore_reference(self.project,originals,entries);self.assertEqual(before,self.snapshot())
    def test_restore_refuses_postinstall_user_edits(self):
        originals,entries=apply_reference(self.package,self.project,self.manifest)
        p=inside(self.project,self.old_entry()['path']);p.write_bytes(p.read_bytes()+b'// important work\n')
        before=self.snapshot()
        with self.assertRaises(ValueError):restore_reference(self.project,originals,entries)
        self.assertEqual(before,self.snapshot())
    def test_install_retains_original_meta_guid(self):
        rel='Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/CrystalGardenDressing.cs.meta'
        d=self.project/rel;d.parent.mkdir(parents=True,exist_ok=True);d.write_text('fileFormatVersion: 2\nguid: my-preserved-guid\n')
        apply_reference(self.package,self.project,self.manifest);self.assertIn('my-preserved-guid',d.read_text())
    def test_provided_scripts_have_critical_stop_guards(self):
        text=(PACKAGE/'Apply02I.ps1').read_text()
        for token in ('Get-Process -Name Unity','ReparsePoint','old_sha256','expected_editor','ConvertFrom-Json','rollbackErrors','NonInteractive','No files changed.'):
            self.assertIn(token,text)
        restore=(PACKAGE/'Restore02I.ps1').read_text();self.assertIn('Post-install changes detected',restore);self.assertIn('ReparsePoint',restore)
    def test_version_revision_and_lineendings_not_fingerprinted(self):
        p=self.project/'ProjectSettings/ProjectVersion.txt';p.write_bytes(b'm_EditorVersion: 6000.3.16f1\r\nm_EditorVersionWithRevision: 6000.3.16f1 (local-record)\r\n')
        self.assertEqual(len(validate(self.package,self.project,self.manifest)),len(self.manifest['files']))
    def test_version_pin_is_semantic_only(self):
        self.assertNotIn('ProjectSettings/ProjectVersion.txt',[d['path'] for d in self.manifest['dependencies']])
    def test_installer_checks_source_again_before_write(self):
        text=(PACKAGE/'Apply02I.ps1').read_text();self.assertIn('Package changed during installation',text)
    def test_no_git_push_or_network_or_cache_deletion(self):
        scripts='\n'.join((PACKAGE/f).read_text() for f in ('Apply02I.ps1','Restore02I.ps1','APPLY_02I.cmd'))
        for token in ('git push','git reset','Invoke-WebRequest','Remove-Item -Recurse','Set-ExecutionPolicy','Library\\'):
            self.assertNotIn(token,scripts)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package',type=Path,required=True);parser.add_argument('--baseline',type=Path,required=True)
    parser.add_argument('--report',type=Path)
    args=parser.parse_args();PACKAGE=args.package.resolve();BASELINE=args.baseline.resolve()
    MANIFEST=json.loads((PACKAGE/'manifest.json').read_text())
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(PolicyTests)
    result=unittest.TextTestRunner(verbosity=2).run(suite)
    report={'scope':'Actual package validation and a Python reference simulation of installer policy, NOT PowerShell execution',
            'tests_run':result.testsRun,'failures':len(result.failures),'errors':len(result.errors),
            'passed':result.wasSuccessful(),'windows_powershell_execution':'NOT RUN','unity_compile':'NOT RUN',
            'tests':[n for n in dir(PolicyTests) if n.startswith('test_')]}
    if args.report:args.report.write_text(json.dumps(report,indent=2)+'\n')
    raise SystemExit(0 if result.wasSuccessful() else 1)
