# H8 recovery completed for source and actual motion assets

Workspace maintenance removed local files during final publication. The first source checkpoint 445f5bc and two uploaded real art Git objects were recovered. The complete C# source was restored byte-for-byte: SHA256 5b7b32da37b6d924d7f275ea9513e04b540833799f04347dcf5c4631063534ac, exactly the source tested before maintenance. Actual H8 artist source and fourteen-action FBX were retained, not recreated. Samples were recovered from that exact source with the same Blender 4.2.23 exporter.

The temporary recovery source name MalikActionsH8.blend is renamed MalikVeyrH8.blend, retaining identical binary content and the original review path. Original H7 art and old branches are preserved.

Before maintenance both 12-mode matrices, 3 assemblies, 12 syntax modes and 34 H8 tests passed. Full NUnit: 1,853 passed, 46 exact unchanged failures, 58 exclusions. See EvidenceH8/execution-receipt.json. Raw unpublished final logs and some unpublished DCC renders were lost; they are not fabricated or labeled recovered. Native Unity remains NOT RUN. Production and owner appearance acceptance are unfinished.
