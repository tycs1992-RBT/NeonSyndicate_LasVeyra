#!/usr/bin/env python3
"""WR01 inherited payload, test and source protection. Source checks are never native acceptance."""
import argparse
import hashlib
import json
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path
from surface_lineage_wr02 import inherited_source_bytes

ROOT = Path(__file__).resolve().parents[1]
BASELINE = 'b1ecc142b9587093cd57cbdfe1414ba468b5c5ed'
RUNTIME = 'Assets/NeonSyndicate/Scripts/Runtime/'
EDITOR = 'Assets/NeonSyndicate/Scripts/Editor/'
UNDERHEART_BUILDER = RUNTIME + 'World/Underheart/UnderheartBuilder.cs'
ART_ADDITIONS = ('Assets/NeonSyndicate/Art/HeartWR01/',
                 'Assets/NeonSyndicate/Art/HeartWR01.meta', 'ArtSource/HeartWR01/',
                 'Assets/NeonSyndicate/Art/HeartWR02/',
                 'Assets/NeonSyndicate/Art/HeartWR02.meta', 'ArtSource/HeartWR02/',
                 'Assets/NeonSyndicate/Art/FoundryWR02A/', 'Assets/NeonSyndicate/Art/FoundryWR02A.meta', 'ArtSource/FoundryWR02A/')
SOURCE_EDITS = frozenset(
    [EDITOR + p for p in ('FantasyAcceptanceV1.cs', 'FantasyAcceptanceV1.Source.cs',
                         'FantasyAcceptanceV1.Capture.cs', 'LiveWorldArtSetupH8S.cs')]
    + [RUNTIME + p for p in (
        'QA/FantasyProofV1.cs', 'UI/VisibleRealityV0.FantasyProof.cs',
        'UI/VisibleRealityV0.FantasySessionV1.cs', 'UI/VisibleRealityV0.HeartFR01.cs',
        'World/HeartFR01/HeartCatalogFR01.cs', 'World/HeartFR01/HeartPresentationFR01.cs',
        'World/HeartFR01/HeartPresentationFR01.ProofV1.cs',
        'ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs',
        'World/Underheart/UnderheartBuilder.cs')])
NEW_SOURCES = frozenset(
    [EDITOR + 'HeartSetupFR01.WorldRewrite.cs']
    + [RUNTIME + p for p in ('ArtInhabitedCity04C/VenueView04C.WorldRewrite.cs',
        'UI/VisibleRealityV0.WorldRewrite.cs', 'World/HeartFR01/HeartPlanFR01.WorldRewrite.cs',
        'World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs',
        'World/HeartFR01/HeartPresentationFR01.SkylineWR01.cs',
        'World/Underheart/UnderheartBuilder.WorldRewrite.cs')]
    + ['Assets/NeonSyndicate/Tests/EditMode/' + p for p in
       ('UpperUnderheartRewriteTests.cs', 'WorldRewriteWR01Tests.cs', 'WorldRewriteProofWR01Tests.cs',
        'WorldRewriteTransactionWR01Tests.cs')])
NEW_SOURCES = NEW_SOURCES | frozenset([
    EDITOR + 'HeartSetupFR01.SurfaceWR02.cs',
    RUNTIME + 'World/CityBuilder.SurfaceWR02.cs',
    RUNTIME + 'World/HeartFR01/HeartPlanFR01.SurfaceWR02.cs',
    RUNTIME + 'World/HeartFR01/HeartPresentationFR01.SurfaceWR02.cs',
    'Assets/NeonSyndicate/Tests/EditMode/SurfaceWorldWR02Tests.cs',
    'Assets/NeonSyndicate/Tests/EditMode/SurfaceNativeWR02Tests.cs'])
NEW_SOURCES = NEW_SOURCES | frozenset([EDITOR + 'HeartSetupFR01.FoundryWR02A.cs', RUNTIME + 'World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs', 'Assets/NeonSyndicate/Tests/EditMode/FoundryWR02ATests.cs'])
POINTER = re.compile(rb'version https://git-lfs.github.com/spec/v1\r?\n'
                     rb'(?:[^\r\n]+\r?\n)*?oid sha256:([0-9a-f]{64})\r?\n'
                     rb'size ([0-9]+)(?:\r?\n)?\Z')


@dataclass(frozen=True)
class PayloadIdentity:
    sha256: str
    size: int
    lfs: bool = False


def pointer_identity(data):
    """Read an LFS identity without confusing pointer bytes with an asset payload."""
    match = POINTER.fullmatch(data)
    return PayloadIdentity(match[1].decode('ascii'), int(match[2]), True) if match else None


def file_identity(path):
    digest = hashlib.sha256()
    size = 0
    prefix = bytearray()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
            size += len(chunk)
            if len(prefix) < 4096:
                prefix.extend(chunk[:4096 - len(prefix)])
    pointer = pointer_identity(bytes(prefix)) if size <= 4096 else None
    return pointer or PayloadIdentity(digest.hexdigest(), size)


def compare_payload(expected, path):
    """Return preserved plus hydration facts; an edited LFS pointer can never pass."""
    if not path.is_file() or path.is_symlink():
        return 'missing'
    actual = file_identity(path)
    if (actual.sha256, actual.size) != (expected.sha256, expected.size):
        # Complete exact WR02 visual hooks may be replayed. Other bytes stay visible.
        try:
            relative = path.resolve().relative_to(ROOT.resolve()).as_posix()
            if path.suffix == '.cs':
                canonical = inherited_source_bytes(ROOT, relative)
                if (hashlib.sha256(canonical).hexdigest(), len(canonical)) == (expected.sha256, expected.size):
                    return 'preserved'
        except ValueError:
            pass
        return 'changed'
    if expected.lfs and actual.lfs:
        return 'unhydrated'
    # Replacing an ordinary Git asset by an unresolved pointer is not preservation.
    if actual.lfs and not expected.lfs:
        return 'changed'
    return 'preserved'


def git(root, *args):
    return subprocess.check_output(['git', *args], cwd=root)


def baseline_path_inventory(root, baseline, trees):
    """Equivalent to baseline cat-file existence, including trees, without fetching each blob."""
    return {path.decode('utf-8', 'surrogateescape') for path in
            git(root, 'ls-tree', '-r', '-t', '--name-only', '-z', baseline, '--', *trees).split(b'\0') if path}


def underheart_render_hook_bytes(original):
    """Apply only the reviewed render interception and failure-restoration literals.

    No whitespace normalization or syntax-level omission is permitted. The whole result
    must match, including the untouched collider, light, release and shell call bytes.
    """
    edits = (
        (b'        public IEnumerator Build(Action<string> progress)\n        {\n            foreach (UhCell c in L.Cells)',
         b'        public IEnumerator Build(Action<string> progress)\n        {\n            PrepareUnderheartRewrite();\n            foreach (UhCell c in L.Cells)'),
        (b'                try\n                {\n                    BuildShell(c);',
         b'                try\n                {\n                    BeginUnderheartRewriteCell(c);\n                    BuildShell(c);'),
        (b'                catch (Exception e)\n                {\n                    Problems.Add("shell "',
         b'                catch (Exception e)\n                {\n                    FailUnderheartRewriteCell(c);\n                    Problems.Add("shell "'),
        (b'                    Log.Exception("Underheart shell " + c.Id, e);\n                }\n                if (i % 6 == 5)',
         b'                    Log.Exception("Underheart shell " + c.Id, e);\n                }\n                finally { EndUnderheartRewriteCell(); }\n                if (i % 6 == 5)'),
        (b'                try { Dress(c); GothicCavern04OB(c); }',
         b'                try { BeginUnderheartRewriteCell(c); Dress(c); GothicCavern04OB(c); }'),
        (b'                catch (Exception e)\n                {\n                    Problems.Add("dressing "',
         b'                catch (Exception e)\n                {\n                    FailUnderheartRewriteCell(c);\n                    Problems.Add("dressing "'),
        (b'                    Log.Exception("Underheart dressing " + c.Id, e);\n                }\n                if (i % 8 == 7)',
         b'                    Log.Exception("Underheart dressing " + c.Id, e);\n                }\n                finally { EndUnderheartRewriteCell(); }\n                if (i % 8 == 7)'),
        (b'            Step("lamp shafts", BuildLampShafts);\n            yield return null;\n            FlushAll();\n        }',
         b'            Step("lamp shafts", BuildLampShafts);\n            yield return null;\n            FlushAll();\n            FinishUnderheartRewrite();\n        }'),
        (b'            string key = _group + "|" + m.GetInstanceID();',
         b'            string key = _group + "|" + m.GetInstanceID() + UnderheartRewriteBucketSuffix();'),
        (b'            b.Tile = tile;\n            return b;',
         b'            b.Tile = tile;\n            RememberUnderheartRewriteBucket(key);\n            return b;'),
        (b'            go.isStatic = true;\n            RendererCount++;',
         b'            go.isStatic = true;\n            CaptureUnderheartRewriteBucket(key, m, mesh, go);\n            RendererCount++;'),
    )
    expected = original
    for index, (before, after) in enumerate(edits):
        if expected.count(before) != 1:
            raise ValueError('Immutable Underheart render-hook context is not unique: ' + str(index))
        expected = expected.replace(before, after, 1)
    return expected


def verify_underheart_render_hooks(root):
    """Accept only unchanged immutable b1 source or the complete literal hook transform."""
    row = {'path': UNDERHEART_BUILDER, 'source_commit': BASELINE, 'passed': False,
           'purpose': 'Exact WR01 render hooks only; every other inherited source byte is frozen.'}
    try:
        original = git(root, 'show', BASELINE + ':' + UNDERHEART_BUILDER)
        current = (Path(root) / UNDERHEART_BUILDER).read_bytes()
        expected = underheart_render_hook_bytes(original)
        row.update({'baseline_sha256': hashlib.sha256(original).hexdigest(),
                    'expected_sha256': hashlib.sha256(expected).hexdigest(),
                    'actual_sha256': hashlib.sha256(current).hexdigest(),
                    'unchanged': current == original, 'passed': current in (original, expected)})
        row['reason'] = ('Unchanged immutable baseline.' if current == original else
                         'Only the complete exact reviewed render-hook transformation.' if current == expected else
                         'Underheart owner differs outside the complete exact reviewed render hooks.')
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        row['reason'] = 'Cannot prove exact Underheart owner transformation: ' + str(error)
    return row


def baseline_entries(root, baseline, tree):
    entries = {}
    for entry in git(root, 'ls-tree', '-r', '-z', baseline, '--', tree).split(b'\0'):
        if not entry:
            continue
        meta, path = entry.split(b'\t', 1)
        mode, kind, oid = meta.split()
        if kind != b'blob' or mode == b'120000':
            raise ValueError('Unsupported inherited asset entry: ' + path.decode())
        entries[path.decode('utf-8', 'surrogateescape')] = oid.decode('ascii')
    return entries


class BaselinePayloads:
    """One Git batch reader, hashing real ordinary blobs and reading LFS OID/size."""
    def __init__(self, root):
        self.process = subprocess.Popen(['git', 'cat-file', '--batch'], cwd=root,
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE)
        self.cache = {}

    def identity(self, oid):
        if oid in self.cache:
            return self.cache[oid]
        self.process.stdin.write(oid.encode('ascii') + b'\n')
        self.process.stdin.flush()
        header = self.process.stdout.readline().split()
        if len(header) != 3 or header[1] != b'blob':
            raise ValueError('Cannot read immutable baseline blob: ' + oid)
        remaining = size = int(header[2])
        digest = hashlib.sha256()
        prefix = bytearray()
        while remaining:
            chunk = self.process.stdout.read(min(remaining, 1024 * 1024))
            if not chunk:
                raise ValueError('Truncated immutable baseline blob: ' + oid)
            remaining -= len(chunk)
            digest.update(chunk)
            if size <= 4096:
                prefix.extend(chunk)
        if self.process.stdout.read(1) != b'\n':
            raise ValueError('Invalid immutable baseline blob delimiter.')
        value = pointer_identity(bytes(prefix)) if size <= 4096 else None
        self.cache[oid] = value or PayloadIdentity(digest.hexdigest(), size)
        return self.cache[oid]

    def close(self):
        self.process.stdin.close()
        self.process.stdout.close()
        self.process.wait()


def addition_allowed(path, allowed):
    return any(path.startswith(prefix) if prefix.endswith('/') else path == prefix
               for prefix in allowed)


def check_frozen_tree(root, baseline, tree, allowed_additions=(), predicate=None, reader=None):
    """Append-only tree guard including nonignored untracked additions, with exact LFS payload identity.

    An identical unresolved pointer preserves the inherited identity but is reported separately
    as unhydrated. The caller must not promote that identity-only result to hydrated delivery.
    """
    owned_reader = reader is None
    reader = reader or BaselinePayloads(root)
    result = {'tree': tree, 'preserved': 0, 'changed': [], 'missing': [],
              'unhydrated': [], 'allowed_additions': [], 'unexpected_additions': []}
    try:
        entries = baseline_entries(root, baseline, tree)
        for path, oid in entries.items():
            if predicate is not None and not predicate(path):
                continue
            status = compare_payload(reader.identity(oid), root / path)
            if status == 'preserved':
                result['preserved'] += 1
            else:
                result[status].append(path)
        current = git(root, 'ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', tree)
        for raw in sorted(set(current.split(b'\0')) - {b''}):
            path = raw.decode('utf-8', 'surrogateescape')
            if path in entries or (predicate is not None and not predicate(path)):
                continue
            result['allowed_additions' if addition_allowed(path, allowed_additions)
                   else 'unexpected_additions'].append(path)
    finally:
        if owned_reader:
            reader.close()
    result['passed'] = not (result['changed'] or result['missing'] or result['unexpected_additions'])
    result['hydrated'] = not result['unhydrated']
    return result


def new_source_allowed(path):
    return path in NEW_SOURCES


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    rows = []
    details = []

    def check(name, passed, detail=None):
        row = {'name': name, 'passed': bool(passed)}
        if detail is not None:
            row['detail'] = detail
        rows.append(row)

    def frozen(tree, allowed=(), predicate=None):
        result = check_frozen_tree(ROOT, BASELINE, tree, allowed, predicate, reader)
        details.append(result)
        check('Inherited payloads preserved and additions bounded: ' + tree, result['passed'])
        check('Inherited LFS payloads hydrated: ' + tree, result['hydrated'])

    reader = BaselinePayloads(ROOT)
    try:
        # All inherited asset types, import metadata, GUIDs, source art and data stay exact.
        new_metadata = tuple(path + '.meta' for path in NEW_SOURCES)
        frozen('Assets', ART_ADDITIONS + new_metadata, lambda p: not p.endswith('.cs'))
        frozen('ArtSource', ART_ADDITIONS)
        frozen('Packages')
        frozen('ProjectSettings')
        frozen('Assets/NeonSyndicate/Tests', tuple(NEW_SOURCES) + new_metadata)
        source = baseline_entries(ROOT, BASELINE, 'Assets')
        violations = []
        for path, oid in source.items():
            if path.endswith('.cs') and path not in SOURCE_EDITS and compare_payload(reader.identity(oid), ROOT / path) != 'preserved':
                violations.append(path)
        check('Every inherited C# owner outside explicit integration scope preserved', not violations, violations)
        underheart = verify_underheart_render_hooks(ROOT)
        check('Underheart owner allows only exact reviewed render hooks', underheart['passed'], underheart)
        current = git(ROOT, 'ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', 'Assets')
        unexpected = [p.decode() for p in set(current.split(b'\0')) - {b''}
                      if p.decode() not in source and p.decode().endswith('.cs') and not new_source_allowed(p.decode())]
        check('New C# sources use exact authorized WR01 paths', not unexpected, sorted(unexpected))
        for path in ['.gitattributes', 'tools/check_real_malik_h5.py', 'tools/measure_civic_h5.py']:
            entries = baseline_entries(ROOT, BASELINE, path)
            if entries:
                check('Inherited policy preserved: ' + path,
                      all(compare_payload(reader.identity(oid), ROOT / name) == 'preserved' for name, oid in entries.items()))
    finally:
        reader.close()

    rewrite = ROOT / (RUNTIME + 'World/HeartFR01/HeartPresentationFR01.WorldRewrite.cs')
    under = ROOT / (RUNTIME + 'World/Underheart/UnderheartBuilder.WorldRewrite.cs')
    text = '\n'.join(p.read_text() for p in (rewrite, under) if p.is_file())
    check('New visual partials contain no direct save or preference writes',
          all(token not in text for token in ('SaveSystem.Save(', 'PlayerPrefs.Set', 'File.Write', 'new SaveData')))
    check('New visual partials do not create gameplay controllers, actors or colliders',
          all(token not in text for token in ('AddComponent<BoxCollider>', 'AddComponent<MeshCollider>',
              'AddComponent<CharacterController>', 'AddComponent<PlayerController>', 'AddComponent<PlayerCamera>',
              'AddComponent<NpcController>', 'AddComponent<QuestRunner>')))
    for path in ['UserCaptures/FantasyWorldProof/example.png', 'UserBackups/FantasyWorldProof/example.json',
                 'Assets/NeonSyndicate/Resources/HeartPreparedWR01/example.prefab']:
        check('Private or generated local output remains ignored: ' + path,
              subprocess.run(['git', 'check-ignore', '-q', path], cwd=ROOT).returncode == 0)
    check('No private native evidence or save backups tracked', not git(ROOT, 'ls-files', '--', 'UserCaptures', 'UserBackups').strip())
    dirty = bool(git(ROOT, 'status', '--porcelain', '--untracked-files=all').strip())
    result = {'scope': __doc__, 'baseline': BASELINE, 'source_head': git(ROOT, 'rev-parse', 'HEAD').decode().strip(),
              'worktree_dirty': dirty, 'scope_includes_current_worktree_and_untracked_files': True,
              'passed': sum(row['passed'] for row in rows), 'total': len(rows),
              'checks': rows, 'payload_details': details, 'native_unity': 'NOT RUN',
              'notes': ['LFS hydration is checked separately from inherited OID/size preservation.',
                        'Inherited asset bytes and GUID metadata are immutable; only new HeartWR01 and owner-authorized HeartWR02 art is authorized.',
                        'source_head identifies Git HEAD. When dirty, it is not an exact tested source commit.',
                        'Source token checks are bounded wiring checks, not physics, visibility or gameplay acceptance.']}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'passed': result['passed'], 'total': result['total'],
                      'failed': [row['name'] for row in rows if not row['passed']]}))
    return int(result['passed'] != result['total'])


if __name__ == '__main__':
    raise SystemExit(main())
