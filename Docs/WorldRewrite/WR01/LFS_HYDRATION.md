# Published LFS verification

Tested source/art commit:7b7716f16417607391b3b3c097b8c6c95fb2cc92. Forward normalization commit:185bb0b567fee621071def0bc583b5be02814c90.

The existing repository LFS publication workflow succeeded at run40576896522. It converted the new editable source,15 FBX files and seven DCC PNGs to matching pointers without rewriting history.

An initially empty remote verification cache fetched the exact normalized commit. All2,273 pointer paths /1,549 unique objects /662,928,876 unique bytes were checked by size and SHA256; zero errors. git lfs fsck passed and the hydrated published worktree was clean. See VERIFICATION/LFS.json. The original2,250 pointer paths are preserved; this checkpoint adds23 binary paths.

The verification used the exact commit SHA, not an ambiguous local branch name. A preliminary fetch resolved the older local branch and could not certify the new objects; it was corrected before the pass above. No missing-object result is relabelled success.

The source Blender container is genuinely compressed and editable. Its fresh hash is216551e49f3b5671dfd9163b29113a44dafe725d90e48e1bdcc964584fdeb491. Its geometry and15exports were freshly inspected/reimported after recovery. No Prepared, material-copy, save or capture folder is published. Native Unity import remains NOT RUN.
