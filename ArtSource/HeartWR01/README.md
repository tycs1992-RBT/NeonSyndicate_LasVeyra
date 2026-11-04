# WR01 editable architecture

`GothicCivicCrownsWR01.blend` is a natively compressed, lossless Blender 4.5.3 source file. It contains all five families and their fifteen explicit mesh LODs. It was regenerated after external workspace pruning; use the accompanying fresh SOURCE_RECEIPT.json rather than any lost scratch hash.

Run `blender -b --python tools/build_heart_wr01.py` from this repository to rebuild. The fifteen original FR01 maps must be hydrated first. No texture is copied or embedded. Origins retain the exact service frame or original footprint centre; export scale stays 1.

Models are hidden separately to keep overlapping LODs out of the viewport. In the Outliner, show one `_LOD0` object at a time to inspect it. UVs tile at four metres and material slots reuse the original shared surfaces.

Archive and Office art begins above the existing gallery volumes. The three skyline families retain the original four-pier ground openings and leave the original rooftop standing plane free. New upper belfry surfaces are visual-only. Runtime keeps the original physical source visible as a witness. Native collision, flight, character appearance, gameplay and owner acceptance are NOT RUN.

DCC renders in Docs/WorldRewrite/WR01/DCC are labeled source reviews, never Unity screenshots. They include actual retained source geometry and explicitly named approximations. They do not prove that a native ordinary-play world meets the owner's artistic acceptance gate.
