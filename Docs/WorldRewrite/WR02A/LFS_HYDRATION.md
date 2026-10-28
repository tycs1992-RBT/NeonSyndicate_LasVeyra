# WR02-A LFS publication and fresh hydration

Source/art checkpoint: fafe816fc6dbcd6086941f31af7c2386c8253dda. Forward-only LFS storage commit: e7b046f61ad9d29464a308f0c8c49c1ed0a31d82. The existing authorized publisher uploaded and normalized 27 new binary payloads: four editable Blender sources, two GLB inputs, six FBX LOD exports, six PBR maps, two original-context OBJs and seven labelled DCC PNGs. Every new pointer OID and size matches the exact published source payload manifest. All other source paths remain equivalent.

After fetch completed, fresh empty task-specific LFS storage held all 2,338 tracked entries, 1,614 unique SHA-256 objects and 747,659,307 unique bytes. Every object hash and size passed; no missing or corrupt object remained. git lfs fsck returned Git LFS fsck OK. An early scan made while download was still running saw 114 not-yet-downloaded objects; it is not counted as a pass. The subsequent complete-download scan is the verification receipt in VERIFICATION/lfs-object-verification.json.

The final text/metadata receipt does not change any LFS pointer or binary payload. Existing source objects and legitimate owner assets are preserved. LFS success does not prove Unity import, materials, visible difference or gameplay.
