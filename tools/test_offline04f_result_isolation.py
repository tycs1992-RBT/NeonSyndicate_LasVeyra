"""A failed fresh runner must never inherit a successful or failed older XML receipt."""
from pathlib import Path
import tempfile
import unittest
from offline04f_tests import isolate_previous_results


class ResultIsolationTests(unittest.TestCase):
    def test_prior_xml_and_receipts_are_recoverable_but_not_fresh(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            old = {'nunit-results.xml': b'<test-run passed="99"/>', 'test-report.json': b'{"old":true}', 'nunit.log': b'old log'}
            for name, content in old.items():
                (root / name).write_bytes(content)
            isolate_previous_results(root)
            self.assertFalse(any((root / name).exists() for name in old))
            archives = list((root / 'previous-results').iterdir())
            self.assertEqual(1, len(archives))
            for name, content in old.items():
                self.assertEqual(content, (archives[0] / name).read_bytes())

    def test_repeated_runs_preserve_separate_evidence(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for value in [b'first', b'second']:
                (root / 'nunit-results.xml').write_bytes(value)
                isolate_previous_results(root)
            self.assertEqual([b'first', b'second'], sorted(p.read_bytes() for p in (root / 'previous-results').glob('*/nunit-results.xml')))

    def test_unrelated_output_and_fresh_directory_are_untouched(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'owner-note.txt').write_text('keep')
            isolate_previous_results(root)
            self.assertEqual('keep', (root / 'owner-note.txt').read_text())
            self.assertFalse((root / 'previous-results').exists())


if __name__ == '__main__':
    unittest.main()
