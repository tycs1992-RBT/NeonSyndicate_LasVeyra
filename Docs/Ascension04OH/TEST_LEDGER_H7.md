# H7 executed verification ledger

Runtime integration source: `dd0848844c579adcf082b2bf10602de5676d8ad6`. The final source/art sealing commit is identified in the delivery receipt and PR. C# snapshot SHA256: `5f84048d05e2096f05f71150e850b9ee8a7cb862c3273939f4c55e62b62aff11`. Both matrices captured the same immutable source; the final working C# bytes match that digest. Native Unity is **NOT RUN** throughout.

| Executed check | Actual result | Evidence |
|---|---|---|
| Genuine 6000.3.16f1 + external NUnit API matrix | 12/12 passed, including one sequential retry | external-nunit-api.json, compile-consolidated.json, raw logs |
| Genuine 6000.3.16f1 + bundled Unity NUnit API matrix | 12/12 passed, including one sequential retry | bundled-nunit-api.json, compile-consolidated.json, raw logs |
| Runtime / Editor / EditMode assembly boundaries | 3/3 passed | assemblies.json and logs |
| Real Roslyn C# 9 syntax configurations | 12/12, zero syntax errors | syntax.json |
| External full NUnit suite | **1,819 passed, 46 failed, 58 excluded**; raw outcome Failed, exit 46 | external-tests.json, external-results.xml.gz, external-nunit.log.gz |
| Exact H6 failure comparison | Zero new failures, zero changed failure messages, zero resolved failures | external-tests.json comparison |
| New H7 tests | **29/29 passed**, 1,067 assertions | focused-tests.json, focused-results.xml.gz |
| Strict actual model FBX parsing | Three real geometry files passed, all vertices weighted and normalized | MalikVeyrH7*-strict.json |
| Full-influence independent comparison | 2,870,232 samples; maximum error 8.95197994781e-16 m | skin-proof.json, complete proofs |
| Four-weight alternative | **REJECTED**, maximum error 0.00632845259882 m exceeds unchanged 0.005 m limit | four-weight proofs and derivation receipts |
| Actual editable DCC actions | **23/23 passed after actual walk/run closing-key repair** | editable-actions.json; original 21/23 failure retained in Development |
| Actual rig-only animation FBX | Five named clips, 53 bones, 13,939 bone samples, zero root travel | actual-fbx-actions.json |
| H0 / H2 / H3 / gameplay preservation | 17/17, 110/110, 78/78, 15/15 | versioned preservation reports |
| H6 original art and owners | 53/53 | h6-preservation.json |
| H7 assets, rights and exact owner-hook audit | See final h7-assets-and-owners.json | actual fingerprints, no approval or ownership grants |
| Unity metadata/GUID/reference validation | 2,019 sidecars, no invalid metadata or unresolved references, 13 regression cases passed | metadata.json |
| Intake helper tests / skin-tool tests | 6/6 and 7/7 | intake.log, skin-tool-tests.log |
| Git LFS / whitespace / final preservation | Final recorded results in release-checks.json | release-checks.json |

The two first-attempt matrix failures were process exit **-9** with no C# diagnostic, one external editor-both-URP and one bundled editor-classic-URP. Each exact captured response file passed when retried sequentially. The initial reports and logs remain; they are not rewritten as initial passes. No unchanged matrix was rerun merely to improve its count.

The H2 frozen importer audit initially reported 109/110 because it did not recognize the new literal H7 hooks. Its before result is retained. The guard now reverses only the three exact reviewed H7 additions, then performs the original byte comparison. A separate H7 check requires that reversal to reproduce the exact H6 importer body, and checks the one final rotation writer, original positions, existing cleanup and absence of damage/input/save authority. No broad runtime exception was added.

Both source suites used official exact-version managed Unity modules, compiled pinned Input System 1.17.0 and supplied optional package metadata. They did not execute the Editor importer, Unity assembly graph, physics, animation playback, skin shader, gameplay, save engine or Windows player. The restored archive emitted ownership warnings; hashes in the reports identify the actual reference bytes.

The H6 comparison remains 1,790 pass / 46 engine-dependent failures / 58 exclusions. The 29 additional passes are executed H7 tests. Failure names and exact messages were compared; no tests, failure expectations or exclusions were weakened. The raw full-suite failure is retained.

Unity import, native EditMode/PlayMode, actual bank/save reload, controls, screenshots, lighting, deformation/contact acceptance, profiler and Windows build/launch are **NOT RUN**. DCC ankle range and root tests do not establish planted soles, foot sliding, sword contact or believable cloth. Native and owner acceptance remain separate open gates.
