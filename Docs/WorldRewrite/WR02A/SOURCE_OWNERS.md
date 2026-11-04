# Source ownership

Existing WR01 civic staging chooses the prepared foundry Archive module, then runs the same bounds/material/passive-root checks, inactive staging, activation-before-suppression and rollback. Original CityBuilder, H5 structure, collision, portals, services and identities are unchanged. The old fallback remains available when the foundry catalog is absent.

Three new C# files: HeartPresentationFR01.FoundryWR02A.cs, HeartSetupFR01.FoundryWR02A.cs and FoundryWR02ATests.cs. Nine existing integration files receive complete exact pinned transformations recorded in tools/wr02a_owner_hooks.json. No physics, creature rig/AI, damage, quest, save, controller, existing test or character-asset owner changes.

The preservation layer replays only the complete SHA-pinned foundry transformation, then the previously pinned WR02 transformation. Arbitrary, partial or extra edits remain visible and fail. The historical WR02 mutation tests use their exact published WR02 fixture bytes; their assertions were not relaxed. New foundry mutation tests cover every integration path. H5/H6/H7/H8 preservation assertions and all inherited C# test bytes remain unchanged.

The external tools live under tools/foundry, outside Unity player code. Tripo credentials, task reservations and quarantine downloads live under ignored .foundry-local. Asset prompts and rights are public source records; credentials and signed URLs are not.
