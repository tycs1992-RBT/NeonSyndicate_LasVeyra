# H2 owner-reported Safe Mode correction

The owner supplied an actual Unity 6000.3.16f1 Console screenshot showing CS0117 at CinematicRebirth04OFTests.cs:122: NUnit Is has no AnyOf definition. The project pins com.unity.ext.nunit 2.0.5, based on NUnit 3.5. The previous external verifier used NUnit 3.14, which accepted that newer method. Prior managed passes therefore missed this native compile incompatibility.

The assertion now uses the equivalent three EqualTo / Or constraints, retaining the exact allowed route IDs. No test requirement, failure or exclusion is removed. Both screenshot QA counters now use FindObjectsByType with no sorting, retaining the same active-object counts. UIKit's unused shadow-style field/reset are removed.

Native reimport on the owner's Windows Editor is still required to establish that Safe Mode is cleared. This host's native import remains NOT RUN. H2 consolidated verification includes the actual bundled Custom NUnit assembly separately from the external runner. The package manifest/lockfile, project renderer, game behavior and saved data are unchanged by this correction.
