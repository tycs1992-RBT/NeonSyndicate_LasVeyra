# Build 03A2: Citadel Renaissance

This is the Git-delivered visual module built on the working Strongholds commit 4a4c26443b8fda3dbfd30f09e612989f9bbd9f5e.
It is NOT the earlier unfinished 03A ZIP or a new Unity project.

## Update and play

1. Close the Unity Editor. In GitHub Desktop stay on art/crystal-fantasy-block-proof, then Fetch origin and Pull origin.
2. Open the same NeonSyndicate_LasVeyra project in Unity 6000.3.16f1. Wait for compilation and use Play, Free Story Mode.
3. Use the existing STRONGHOLDS tab and its waypoint to visit Hearthspire, Galehook or Sunscar.

No installer, new scene, Art menu, manual placement, new save or repeated master prompt is required.

## What should visibly change

Hearthspire's default Amethyst appearance gains a much taller layered gateway, pale carved columns, rear starcourt arches and a suspended-looking crystal centerpiece.
Galehook's default Verdant appearance gains a large branching tree at the rear, overlapping canopies, plant beds, ferns and crystal flowers.
Uncaptured Sunscar gains a dark Riftforge silhouette with jagged stone/crystal fins and rust-red banners. This is an art treatment, not a new gameplay faction.
After capture, a fort still follows its saved Amethyst, Dawnstone or Verdant theme. Dawnstone has warm stone and gold trim. Existing ownership and theme choices are not reset.

Your six existing module positions also gain matching details. Empty sockets stay empty; conservatories, watchtowers, bulwarks and flight platforms retain their original function and collision.
Original fort stone and cloth materials receive new subdued surface textures. Most of the environment stays non-emissive, with glow reserved for crystals and flowers. No fullscreen filter or change to the weather or sun is included.

## Check that the module actually loaded

At each successfully decorated fort, the Console logs a line beginning `Citadel Renaissance 03A2 |` and the existing nearby core sign adds `CITADEL RENAISSANCE 03A2`.
Older 02D/02I boot messages can still appear because those systems are deliberately retained. No new menu tab is required.
An optional art warning means the old fort was retained instead. Send the first warning or red error rather than changing import settings or deleting files.

## Limits

This pass changes the EXISTING THREE FORTS, not every city building. It does not add new outposts, networking, unrestricted construction, NPC populations, new flight bonuses or quests.
The new high arches and tree silhouettes are decorative. They are not additional landable floors or fully collidable interiors. The previous fort's landable decks and collision remain unchanged.
The kit consists of twelve original parametric mesh definitions with detail variants and generated surface patterns. It is not a photoreal scanned asset pack, and matching cinematic reference images has not been demonstrated.

## Testing status

Offline source/preservation checks passed. Native Unity compilation, EditMode tests, rendered appearance, frame rate and live gameplay have NOT been run here. The provided EditMode test fixture is for local execution; it has not been reported as passed.

First test: visit the same fort as before, verify the new gate and skyline, use a construction socket, and make sure flight/landing and the existing capture loop still behave normally. Save through your existing SAVE / LOAD menu before ending Play Mode.

This update changes no save schema, packages, engine settings, .gitattributes or player controls. The previous uploaded build is retained in Git history. If a rollback is needed, preserve local changes and revert only the 03A2 commit; do not discard the working project.
