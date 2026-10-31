# Character material recovery

The supplied owner screenshots show the earlier fallback warning, a later blue/white suited Malik and a later untextured-looking body. These images have no exact current SHA or native logs attached. They demonstrate the owner-visible problem, not its final native cause.

Actual Blender import of all five LiveCast FBXs confirms meshes, UV layers and material names remain in the owner assets. Malik still has separate Skin, Suit, Hair, Beard, Mask, Eyes, Teeth and Tongue meshes. No owner FBX, image, GUID, identity or rights file was deleted or regenerated.

Source defects repaired: the livecast-8 cache could accept materials whose shaders survived but whose albedo references were lost after source metadata restoration. A drop-in whose imported material name did not match a map could silently build a neutral gray material. An importer could keep a broken null material remap overriding embedded FBX names. Recipe9 rebuilds existing generation, removes only broken material remaps, requires a matched map or explicit solid color for opaque drop-ins, tags required maps and refuses a cache/runtime binding that loses one. Legitimate material references remain.

This closes demonstrated source failure paths. It is not proof that the owner-reported white-body problem is visually cured. Native import, actual prepared maps, all costumes, shaders and gameplay material behavior remain NOT RUN. Rebuild live characters before the ordinary review and keep the first refusal, including the exact material or texture name.
