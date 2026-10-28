# Publication and hydration

All source .blend/.glb, imported .fbx and DCC image payloads use existing Git LFS rules. The authorized existing publisher creates recoverable forward pointer commits when needed; it never rewrites history. A pointer file is not a hydrated asset.

Final LFS OID/size/SHA-256 checks, fsck and branch publication receipt will be recorded after actual fetch. Do not open Unity with missing LFS payloads. Existing owner assets/GUIDs and older binary objects remain protected; no asset is deleted to make a guard pass.
