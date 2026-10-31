# Verified LFS publication

Implementation `20d8ddcaae3e7025f24bcf899ff6608deadb0738` was normalized by the existing authorized LFS publisher into forward commit `cedb86c41aeae5c3672fa273c649626db5aaa1fd`. Publication run 40788965999 succeeded. No history rewrite occurred.

All 2,382 current entries correspond to 1,658 unique objects, 841,719,167 unique bytes. Every current object size/SHA-256 was recomputed. All 44 newly introduced .blend/.glb/.fbx/DCC image payloads were independently fetched into an initially empty object cache and verified. Existing objects were retained and rehashed; this is not a claim of a second full download of every historical object. Full current `git lfs fsck` PASS.

The partial Git checkout initially lacked new promised pointer blobs. Verifying/staging the exact published pointer tree completed those objects without discarding any work; the final fetch/hash/fsck passed. No failure is hidden as a successful first attempt. See VERIFICATION/lfs-verification.json and lfs-fsck.txt.

Prepared Unity materials/prefabs/catalogs and local credentials are ignored, not committed. Owner asset GUIDs and payloads remain preserved. LFS hydration does not establish native Unity import.
